using Microsoft.AspNetCore.Http;
using Models;

namespace Repository.Tenant.Interfaces;

public interface ITenantManager
{
    ApplicationDbContext GetDbContext();
    void ResolveTenant(HttpContext context);
    int TenantId { get; }
}