using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Models;
using Models.DataModels;

namespace Repository.Tenant.Interfaces;

public interface ITenantManager
{
    ApplicationDbContext GetDbContext();
    void ResolveTenant(HttpContext context);
    IEnumerable<TenantEntry> GetAllTenants();
    void SwitchTenant(int tenantId);
    int TenantId { get; }
    Semaphore ManipulationQueue { get; }
}