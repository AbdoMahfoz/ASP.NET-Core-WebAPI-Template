using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Models;

namespace Repository.Tenant.Interfaces;

public interface ITenantManager
{
    ApplicationDbContext GetDbContext();
    void ResolveTenant(HttpContext context);
    IEnumerable<int> GetAllTenants();
    void SwitchTenant(int tenantId);
    int TenantId { get; }
    Semaphore ManipulationQueue { get; }
}