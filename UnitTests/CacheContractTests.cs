using System;
using System.Linq;
using System.Threading.Tasks;


using BusinessLogic.Implementations.Cache;
using BusinessLogic.Interfaces;


using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;


using Models.Helpers;


using Repository.Tenant.Interfaces;


using StackExchange.Redis;


using WebAPI.Extensions;


using Xunit;


namespace UnitTests;


/// <summary>
///     Cache contract evidence: entries expire on access instead of serving stale data, string sets add and drop members, an invalidated key is rebuilt in the background by the invalidator registered for it (and a plain invalidate without an invalidator is just a remove), and configuration decides whether the application runs on the shared Redis backplane or the in-process cache - asserted from the registered descriptors so no Redis server has to be reachable. The behaviour tests run against the cache the booted application actually registers, which is MemoryCache because AppSettings.Redis.Use is false in configuration. Each test uses its own keys so the process-wide cache state cannot leak between them.
/// </summary>
public class CacheContractTests : MainWebApplicationFactory<Program>

{

    public CacheContractTests() => Setup();


    private static string UniqueKey(string purpose) => $"cache-contract:{purpose}:{Guid.NewGuid():N}";


    private ICache ResolveCache()

    {

        using var scope = Scope();


        return scope.ServiceProvider.GetRequiredService<ICache>();

    }


    [Fact]
    public void TheApplicationRegistersTheConfiguredCacheImplementation()

    {

        Assert.IsType<MemoryCache>(ResolveCache());

    }


    [Fact]
    public async Task CachedValuesExpireOnAccess()

    {

        var cache = ResolveCache();



        var expiringKey = UniqueKey("expiry");



        await cache.SetValue(expiringKey, "still-here", TimeSpan.FromMilliseconds(120));



        Assert.Equal("still-here", await cache.GetValue<string>(expiringKey));



        await Task.Delay(320);



        Assert.Null(await cache.GetValue<string>(expiringKey));



        var givenExpiryLate = UniqueKey("late-expiry");



        await cache.SetValue(givenExpiryLate, "kept-until-told");



        Assert.Equal("kept-until-told", await cache.GetValue<string>(givenExpiryLate));



        await cache.SetExpiration(givenExpiryLate, TimeSpan.FromMilliseconds(120));



        await Task.Delay(320);



        Assert.Null(await cache.GetValue<string>(givenExpiryLate));



        var missingKey = UniqueKey("missing");



        await cache.SetExpiration(missingKey, TimeSpan.FromDays(1));



        Assert.Null(await cache.GetValue<string>(missingKey));
    }


    [Fact]
    public async Task StringSetsAddReadAndDropMembers()

    {

        var cache = ResolveCache();



        var setKey = UniqueKey("set");



        await cache.AddSetEntry(setKey, "member-one");
        
        await cache.AddSetEntry(setKey, "member-two");



        var entries = await cache.GetSetEntries(setKey);



        Assert.Contains("member-one", entries);
        
        Assert.Contains("member-two", entries);



        await cache.RemoveSetEntry(setKey, "member-one");



        entries = await cache.GetSetEntries(setKey);



        Assert.DoesNotContain("member-one", entries);
        
        Assert.Contains("member-two", entries);



        var expiringSet = UniqueKey("expiring-set");



        await cache.AddSetEntry(expiringSet, "temporary", TimeSpan.FromMilliseconds(120));



        Assert.NotEmpty(await cache.GetSetEntries(expiringSet));



        await Task.Delay(320);



        Assert.Empty(await cache.GetSetEntries(expiringSet));
    }


    [Fact]
    public async Task InvalidatedEntriesAreRebuiltByTheirRegisteredInvalidator()

    {

        var cache = ResolveCache();


        ITenantManager tenants;


        
 using (var scope = Scope())

        {

            tenants = scope.ServiceProvider.GetRequiredService<ITenantManager>();

        
 }


        
 var rebuiltValue = "rebuilt-by-tenant-" + tenants.TenantId;


        
 var key = UniqueKey("invalidator");


        
 await cache.RegisterInvalidator<ITenantManager>(key, manager => Task.FromResult<object>("rebuilt-by-tenant-" + manager.TenantId), "seeded-before-invalidation");


        
 Assert.Equal("seeded-before-invalidation", await cache.GetValue<string>(key));


        
 await cache.InvalidateEntry(key);


        
 Assert.Equal(rebuiltValue, await cache.GetValue<string>(key));
    }


    [Fact]
    public async Task InvalidationWithoutARegisteredInvalidatorIsAPlainRemove()

    {

        var cache = ResolveCache();


        
 var key = UniqueKey("plain-invalidation");


        
 await cache.SetValue(key, "plain-value");


        
 Assert.Equal("plain-value", await cache.GetValue<string>(key));


        
 await cache.InvalidateEntry(key);


        
 Assert.Null(await cache.GetValue<string>(key));
    }


    [Fact]
    public void RedisConfigurationSelectsTheRedisBackplaneWithoutConnecting()

    {

        var services = new ServiceCollection();



        services.AddConfiguredCache(new AppSettings
        {
            Redis = new AppSettings.RedisObject
            {
                Use = true,
                ConnectionString = "replace-with-your-redis-endpoint:6379,password=replac…word",
                ValidateCertificate = false
            }
        });



        var cacheDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(ICache));



        Assert.Equal(typeof(RedisCache), cacheDescriptor.ImplementationType);



        var multiplexerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer));



        Assert.True(multiplexerDescriptor.ImplementationFactory != null);



        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor));
    }


    [Fact]
    public void DisabledOrMissingRedisConfigurationSelectsTheInProcessCache()

    {

        var withRedisDisabled = new ServiceCollection();



        withRedisDisabled.AddConfiguredCache(new AppSettings
        {
            Redis = new AppSettings.RedisObject { Use = false, ConnectionString = "replace-with-your-redis-endpoint:6379" }
        });



        Assert.Equal(typeof(MemoryCache), withRedisDisabled.Single(descriptor => descriptor.ServiceType == typeof(ICache)).ImplementationType);



        Assert.DoesNotContain(withRedisDisabled, descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer));



        var withoutAnyRedisSection = new ServiceCollection();



        withoutAnyRedisSection.AddConfiguredCache(new AppSettings());



        Assert.Equal(typeof(MemoryCache), withoutAnyRedisSection.Single(descriptor => descriptor.ServiceType == typeof(ICache)).ImplementationType);
    }

}
