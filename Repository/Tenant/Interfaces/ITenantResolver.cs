using Microsoft.AspNetCore.Http;

namespace Repository.Tenant.Interfaces;

public interface ITenantResolver
{
    int ResolveTenant(HttpContext context);
}