using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace Repository.Tenant.Implementations.TenantStore;

public class ReadOnlyMemoryTenantStore : ITenantStore
{
    private static readonly FrozenDictionary<int, TenantEntry> Store = new KeyValuePair<int, TenantEntry>[]
    {
        new(1, new TenantEntry { TenantId = 1, ConnectionString = null })
    }.ToFrozenDictionary();

    public IEnumerable<TenantEntry> GetAllTenants()
    {
        return Store.Values;
    }

    public Task<TenantEntry> GetTenant(int tenantId)
    {
        return Task.FromResult(Store.GetValueOrDefault(tenantId));
    }

    public Task AddTenant(TenantEntry tenant)
    {
        throw new ReadOnlyException();
    }

    public Task UpdateTenantConnectionString(int tenantId, string connectionString)
    {
        throw new ReadOnlyException();
    }

    public Task RemoveTenant(TenantEntry tenant)
    {
        throw new ReadOnlyException();
    }

    public Task RemoveTenant(int tenantId)
    {
        throw new ReadOnlyException();
    }
}