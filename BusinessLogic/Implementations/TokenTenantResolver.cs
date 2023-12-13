using System;
using Microsoft.AspNetCore.Http;
using Repository.Tenant.Interfaces;
using Services.Extensions;

namespace BusinessLogic.Implementations;

public class TokenTenantResolver : ITenantResolver
{
    public int ResolveTenant(HttpContext context)
    {
        try
        {
            return context.User.GetTenantId();
        }
        catch (Exception)
        {
            return 1;
        }
    }
}