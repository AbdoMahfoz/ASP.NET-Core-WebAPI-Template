using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BusinessLogic.Interfaces;


/// <summary>
///     The application's one cache: values with optional expirations, string sets for grouped membership, and keyed
///     invalidation where an entry is dropped and rebuilt in the background by a registered invalidator. One contract on purpose - there is no local/global split, so a caller cannot accidentally write to a cache nobody reads.
/// </summary>
public interface ICache

{


    Task SetValue(string key, object value);


    /// <summary>Writes the value and gives it a lifetime in the same call.</summary>
    Task SetValue(string key, object value, TimeSpan expiry);


    Task<T> GetValue<T>(string key);


    Task<object> GetValue(string key);


    Task Remove(string key);


    /// <summary>
    ///     Drops the entry and rebuilds it in the background through the invalidator registered for that key. Reads of
    ///     the key wait for the refresh instead of racing it; without a registered invalidator this is a plain remove.
    /// </summary>
    Task InvalidateEntry(string key);


    /// <summary>Tells the cache how to rebuild this key, resolving TService from a fresh DI scope when it refreshes.</summary>
    Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator)
        where TService : notnull;


    /// <summary>Registers the rebuild path and seeds the key with an initial value.</summary>
    Task RegisterInvalidator<TService>(string key, Func<TService, Task<object>> invalidator, object initialValue)
        where TService : notnull;


    /// <summary>Adds a member to a string set keyed by <paramref name="key" />, optionally expiring the whole set.</summary>
    Task AddSetEntry(string key, string value, TimeSpan? expiry = null);


    Task RemoveSetEntry(string key, string value);


    Task<IReadOnlyCollection<string>> GetSetEntries(string key);


    /// <summary>Gives an existing entry a new lifetime; a missing entry is not created.</summary>
    Task SetExpiration(string key, TimeSpan expiry);

}
