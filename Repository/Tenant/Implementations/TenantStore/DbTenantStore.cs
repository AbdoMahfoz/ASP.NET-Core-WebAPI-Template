using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace Repository.Tenant.Implementations.TenantStore;

public class DbTenantStore : ITenantStore
{
    private readonly ApplicationDbContext _tenantContext = new();
    private static readonly object TenantLock = new();

    public DbTenantStore()
    {
        lock (TenantLock)
        {
            if (!_tenantContext.Set<TenantEntry>().Any())
            {
                _tenantContext.Set<TenantEntry>().AddRange(
                    new TenantEntry
                    {
                        TenantId = 1,
                        ConnectionString = null
                    }
                );
                _tenantContext.SaveChanges();
            }
        }
    }

    public IEnumerable<TenantEntry> GetAllTenants()
    {
        return _tenantContext.Set<TenantEntry>().ToArray();
    }

    public Task<TenantEntry> GetTenant(int tenantId)
    {
        return _tenantContext.Set<TenantEntry>()
            .Where(u => u.TenantId == tenantId).SingleOrDefaultAsync();
    }

    public Task AddTenant(TenantEntry tenant)
    {
        return _tenantContext.Set<TenantEntry>().AddAsync(tenant).AsTask();
    }

    public async Task UpdateTenantConnectionString(int tenantId, string connectionString)
    {
        var tenant = await GetTenant(tenantId);
        tenant.ConnectionString = connectionString;
        _tenantContext.Set<TenantEntry>().Update(tenant);
        await _tenantContext.SaveChangesAsync();
    }

    public Task RemoveTenant(TenantEntry tenant)
    {
        _tenantContext.Remove(tenant);
        return _tenantContext.SaveChangesAsync();
    }

    public Task RemoveTenant(int tenantId)
    {
        return _tenantContext.Set<TenantEntry>()
            .Where(u => u.TenantId == tenantId).ExecuteDeleteAsync();
    }
}