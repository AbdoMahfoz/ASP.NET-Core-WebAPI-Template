using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.Interfaces;

namespace BusinessLogic.Implementations.Cache;

public class MemoryCache : ICache
{
    private static readonly Dictionary<string, object> Cache = new();

    public Task SetValue(string key, object value)
    {
        lock (Cache)
        {
            if (!Cache.TryAdd(key, value))
            {
                Cache[key] = value;
            }
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetValue<T>(string key)
    {
        var val = await GetValue(key);
        return val == null ? default : (T)val;
    }

    public Task<object> GetValue(string key)
    {
        lock (Cache)
        {
            return Task.FromResult(Cache.TryGetValue(key, out var val) ? val : null);
        }
    }
}