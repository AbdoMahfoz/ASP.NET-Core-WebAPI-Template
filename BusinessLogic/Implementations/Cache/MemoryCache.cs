using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BusinessLogic.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Repository.Tenant.Interfaces;


namespace BusinessLogic.Implementations.Cache;


/// <summary>
///     In-process cache behind the single ICache contract. Values expire on read, string sets are stored as a keyed
///     concurrent set, and an invalidated key is dropped, rebuilt in a background DI scope, and any reader of that key waits for the rebuild rather than serving the stale value or racing it. Invalidation is serialized per key with its own wait handle, so two callers invalidating the same key do not double-refresh.
/// </summary>
public class MemoryCache(
    IServiceScopeFactory serviceScopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<MemoryCache> logger) : ICache

{

    private static readonly TimeSpan InvalidationWaitTimeout = TimeSpan.FromSeconds(50);

    protected static ConcurrentDictionary<string, object> Cache = new();

    protected static ConcurrentDictionary<string, DateTimeOffset> Expirations = new();

    private static readonly ConcurrentDictionary<string, ManualResetEvent> InvalidationLocks = new();

    private static readonly ConcurrentDictionary<string, Func<IServiceProvider, Task<object>>> Invalidators = new();


    /// <summary>Drops every cached value and invalidation state - the reset used when the host or a test harness rebuilds its world.</summary>
    public void ClearEntries()

    {

        Cache.Clear();

        Expirations.Clear();


        foreach (var invalidationLock in InvalidationLocks.Values)

        {

            try { invalidationLock.Set(); } catch { /* best-effort reset */ }


            try { invalidationLock.Dispose(); } catch { /* best-effort reset */ }

        }


        InvalidationLocks.Clear();

        Invalidators.Clear();

    }


    public Task SetValue(string key, object value)

    {

        Cache[key] = value;


        return Task.CompletedTask;

    }


    public async Task SetValue(string key, object value, TimeSpan expiry)

    {

        await SetValue(key, value);


        await SetExpiration(key, expiry);

    }


    public async Task<T> GetValue<T>(string key)

    {

        var val = await GetValue(key);


        if (val == null) return default;


        if (val.GetType().IsAssignableTo(typeof(T))) return (T)val;


        var nullableType = Nullable.GetUnderlyingType(typeof(T));

        if (nullableType != null) return (T)Convert.ChangeType(val, nullableType);


        return (T)val;

    }


    public Task<object> GetValue(string key)

    {

        if (RemoveIfExpired(key)) return Task.FromResult<object>(null);


        if (InvalidationLocks.TryGetValue(key, out var invalidationSignal))

        {

            var released = false;


            try { released = invalidationSignal.WaitOne(InvalidationWaitTimeout); }
            catch (ObjectDisposedException ex)

            {

                released = true;


                logger.LogWarning(ex, "Cache invalidation lock was disposed while waiting for key {CacheKey}", key);

            }
            catch (Exception ex)

            {
                logger.LogError(ex, "Failed while waiting for cache invalidation lock for key {CacheKey}", key);
            }



            if (!released) logger.LogWarning("Timed out waiting for cache invalidation lock for key {CacheKey}", key);
        }



        var found = Cache.TryGetValue(key, out var val);



        return Task.FromResult(found ? val : null);
    }


    public Task Remove(string key)

    {

        Cache.TryRemove(key, out _);
        
        Expirations.TryRemove(key, out _);


        return Task.CompletedTask;

    }


    public Task InvalidateEntry(string key)

    {
        if (!Invalidators.TryGetValue(key, out var invalidator))
        {
            logger.LogInformation("Invalidating cache entry {CacheKey} without registered refresh", key);
            
            return Remove(key);
        }



        var invalidationSignal = new ManualResetEvent(false);



        if (!InvalidationLocks.TryAdd(key, invalidationSignal))
        {
            invalidationSignal.Dispose();



            logger.LogInformation("Cache invalidation is already running for key {CacheKey}", key);



            return Task.CompletedTask;
        }



        Cache.TryRemove(key, out _);
        
        Expirations.TryRemove(key, out _);



        logger.LogInformation("Invalidating cache entry {CacheKey} and scheduling refresh", key);



        IServiceScope refreshScope = null;



        try
        {
            refreshScope = serviceScopeFactory.CreateScope();



            var httpContext = httpContextAccessor.HttpContext;



            if (httpContext != null)
            {
                refreshScope.ServiceProvider.GetRequiredService<ITenantManager>().ResolveTenant(httpContext);
            }

            else
            {
                logger.LogWarning("No HttpContext was available while invalidating cache entry {CacheKey}", key);
            }
        }

        catch (Exception ex)

        {

            logger.LogError(ex, "Failed to prepare cache invalidation refresh for key {CacheKey}", key);


            refreshScope?.Dispose();


            ReleaseInvalidationLock(key, invalidationSignal, "setup_failed");


            return Task.CompletedTask;

        }



        var scope = refreshScope;



        _ = Task.Run(async () =>

        {

            try

            {

                logger.LogInformation("Refreshing invalidated cache entry {CacheKey}", key);


                var res = await invalidator(scope.ServiceProvider);


                await SetValue(key, res);


                logger.LogInformation("Refreshed invalidated cache entry {CacheKey}", key);

            }

            catch (Exception ex)

            {

                logger.LogError(ex, "Failed to refresh invalidated cache entry {CacheKey}", key);

            }


            finally

            {

                try { scope.Dispose(); }
                catch (Exception ex)

                {
                    logger.LogError(ex, "Failed to dispose cache invalidation scope for key {CacheKey}", key);
                }



                ReleaseInvalidationLock(key, invalidationSignal, "refresh_finished");

            }

        });


        return Task.CompletedTask;
    }


    public Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator)
        where TService : notnull

    {

        Invalidators[key] = provider => invalidator(provider.GetRequiredService<TService>());


        return Task.CompletedTask;

    }


    public async Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator, object initialValue)
        where TService : notnull

    {

        await RegisterInvalidator(key, invalidator);


        await SetValue(key, initialValue);

    }


    public Task AddSetEntry(string key, string value, TimeSpan? expiry = null)

    {
        RemoveIfExpired(key);



        var values = (ConcurrentDictionary<string, byte>)Cache.GetOrAdd(key, _ => new ConcurrentDictionary<string, byte>());



        values[value] = 0;



        if (expiry.HasValue) Expirations[key] = DateTimeOffset.UtcNow.Add(expiry.Value);



        return Task.CompletedTask;
    }


    public Task RemoveSetEntry(string key, string value)

    {
        RemoveIfExpired(key);



        if (Cache.TryGetValue(key, out var cachedValue) && cachedValue is ConcurrentDictionary<string, byte> values)
        {
            values.TryRemove(value, out _);
        }



        return Task.CompletedTask;
    }


    public Task<IReadOnlyCollection<string>> GetSetEntries(string key)

    {
        RemoveIfExpired(key);



        if (!Cache.TryGetValue(key, out var cachedValue) || cachedValue is not ConcurrentDictionary<string, byte> values)
        {
            return Task.FromResult((IReadOnlyCollection<string>)Array.Empty<string>());
        }



        return Task.FromResult((IReadOnlyCollection<string>)values.Keys.ToArray());
    }


    public Task SetExpiration(string key, TimeSpan expiry)

    {

        if (Cache.ContainsKey(key)) Expirations[key] = DateTimeOffset.UtcNow.Add(expiry);


        return Task.CompletedTask;

    }


    private bool RemoveIfExpired(string key)

    {

        if (!Expirations.TryGetValue(key, out var expiresAt) || expiresAt > DateTimeOffset.UtcNow) return false;


        Cache.TryRemove(key, out _);
        
        Expirations.TryRemove(key, out _);


        return true;

    }


    private void ReleaseInvalidationLock(string key, ManualResetEvent invalidationSignal, string reason)

    {

        try { invalidationSignal.Set(); }
        catch (Exception ex)

        {
            logger.LogError(ex, "Failed to signal cache invalidation lock for key {CacheKey}", key);
        
            }


        try

        {

            var removed = InvalidationLocks.TryRemove(key, out var removedSignal);


            if (removed) removedSignal?.Dispose(); else invalidationSignal.Dispose();


            logger.LogInformation("Released cache invalidation lock for key {CacheKey}. Reason: {Reason}", key, reason);

        }

        catch (Exception ex)

        {
            logger.LogError(ex, "Failed to release cache invalidation lock for key {CacheKey}", key);
        
            }
    }
}
