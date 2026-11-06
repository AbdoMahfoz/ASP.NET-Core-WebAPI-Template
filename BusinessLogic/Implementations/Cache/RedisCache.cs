using System;

using System.Collections.Generic;

using System.Linq;

using System.Text.Json;

using System.Threading.Tasks;


using BusinessLogic.Interfaces;


using StackExchange.Redis;


namespace BusinessLogic.Implementations.Cache;


/// <summary>
///     The shared-backplane cache behind the same ICache contract, so a value cached by one instance is visible to every other instance and an invalidation is seen by all of them. Values are stored in an envelope that records the cached CLR type, so a plain GetValue can hand back the original object rather than a loosely typed token. Invalidation here is a delete: with a shared store there is no per-process refresh loop to run, and whoever owns the value rewrites it on the next read path.
/// </summary>
public class RedisCache(IConnectionMultiplexer connectionMultiplexer) : ICache

{

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();


    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions();


    private sealed class CacheEnvelope

    {

        public string TypeName { get; set; }


        public JsonElement Value { get; set; }

    }


    public Task SetValue(string key, object value)

    {

        return SetValueCore(key, value, null);

    }


    public Task SetValue(string key, object value, TimeSpan expiry)

    {

        return SetValueCore(key, value, expiry);

    }


    private async Task SetValueCore(string key, object value, TimeSpan? expiry)

    {

        var envelope = new CacheEnvelope
        {
            TypeName = value.GetType().AssemblyQualifiedName,
            Value = JsonSerializer.SerializeToElement(value, JsonOptions)
        };



        var payload = JsonSerializer.Serialize(envelope, JsonOptions);

        // StackExchange.Redis 3.x models a key's lifetime as an Expiration value.
        if (expiry.HasValue) await _database.StringSetAsync(key, payload, new Expiration(expiry.Value));

        else await _database.StringSetAsync(key, payload);
    }



    private async Task<CacheEnvelope> GetEnvelope(string key)
    {

        var cached = await _database.StringGetAsync(key);



        if (!cached.HasValue) return null;



        return JsonSerializer.Deserialize<CacheEnvelope>(cached.ToString(), JsonOptions);
    }


    public async Task<T> GetValue<T>(string key)

    {

        var envelope = await GetEnvelope(key);


        if (envelope == null) return default;


        return JsonSerializer.Deserialize<T>(envelope.Value.GetRawText(), JsonOptions);

    }


    public async Task<object> GetValue(string key)

    {

        var envelope = await GetEnvelope(key);


        if (envelope == null) return null;


        var type = Type.GetType(envelope.TypeName) ?? throw new InvalidOperationException($"Cached type '{envelope.TypeName}' could not be resolved.");


        return JsonSerializer.Deserialize(envelope.Value.GetRawText(), type, JsonOptions);

    }


    public async Task Remove(string key)

    {
    
        await _database.KeyDeleteAsync(key);
    
    }


    /// <summary>Every instance shares this store, so dropping the entry here is what invalidates it everywhere.</summary>
    public Task InvalidateEntry(string key)
    
    {
    
        return Remove(key);
    
    }


    /// <summary>No per-process refresh loop runs against a shared store; the value is rebuilt by whoever owns it.</summary>
    public Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator)
        where TService : notnull
    
    {
    
        return Task.CompletedTask;
    
    }


    public async Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator, object initialValue)
        where TService : notnull
    
    {
    
        await RegisterInvalidator(key, invalidator);
    
        await SetValue(key, initialValue);
    
    }


    public async Task AddSetEntry(string key, string value, TimeSpan? expiry = null)
    
    {
    
    
    
        if (expiry == null)
        {
            await _database.SetAddAsync(key, value);
    
    
    
            return;
        }
    
    
    
        var transaction = _database.CreateTransaction();
    
    
    
        _ = transaction.SetAddAsync(key, value);
    
        _ = transaction.KeyExpireAsync(key, expiry.Value);
    
    
    
        await transaction.ExecuteAsync();
    
    }


    public async Task RemoveSetEntry(string key, string value)
    
    {
    
    
    
        await _database.SetRemoveAsync(key, value);
    
    }


    public async Task<IReadOnlyCollection<string>> GetSetEntries(string key)
    
    {
    
    
    
        var values = await _database.SetMembersAsync(key);
    
    
    
        return values.Select(value => value.ToString()).ToArray();
    
    }


    public async Task SetExpiration(string key, TimeSpan expiry)

    {

        await _database.KeyExpireAsync(key, expiry);

    }

}
