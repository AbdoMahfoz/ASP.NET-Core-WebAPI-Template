using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Models;
using Repository.Tenant.Interfaces;

namespace Repository.Tenant.Implementations;

public class TenantManager(ITenantResolver tenantResolver) : ITenantManager, IDisposable, IAsyncDisposable
{
    private readonly FrozenDictionary<int, string> _tenants = new KeyValuePair<int, string>[]
    {
        new(0, null)
    }.ToFrozenDictionary();

    private ApplicationDbContext _dbContext;

    public int TenantId { get; private set; }

    public ApplicationDbContext GetDbContext()
    {
        return _dbContext ??= new ApplicationDbContext();
    }

    public void ResolveTenant(HttpContext context)
    {
        var tenantId = tenantResolver.ResolveTenant(context);
        TenantId = tenantId;
        if (_dbContext != null)
        {
            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }

        _dbContext = new ApplicationDbContext(tenantId, _tenants[tenantId]);
    }


    public void Dispose()
    {
        if (_dbContext != null)
        {
            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.SaveChangesAsync();
            await _dbContext.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}