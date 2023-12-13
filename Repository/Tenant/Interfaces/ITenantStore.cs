using System.Collections.Generic;
using System.Threading.Tasks;
using Models.DataModels;

namespace Repository.Tenant.Interfaces;

public interface ITenantStore
{
    IEnumerable<TenantEntry> GetAllTenants();
    Task<TenantEntry> GetTenant(int tenantId);
    Task AddTenant(TenantEntry tenant);
    Task UpdateTenantConnectionString(int tenantId, string connectionString);
    Task RemoveTenant(TenantEntry tenant);
    Task RemoveTenant(int tenantId);
}