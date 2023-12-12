using Microsoft.AspNetCore.Mvc.Filters;
using Repository.Tenant.Interfaces;

namespace WebAPI.Filters;

public class TenantActionFilter(ITenantManager tenantManager) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        tenantManager.ResolveTenant(context.HttpContext);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}