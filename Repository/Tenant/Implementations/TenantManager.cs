using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Models;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace Repository.Tenant.Implementations;

public class TenantManager(ITenantResolver tenantResolver, ITenantStore tenantStore)
    : ITenantManager, IDisposable, IAsyncDisposable
{
    private ApplicationDbContext _dbContext;
    public int TenantId { get; private set; } = 1;
    public Semaphore ManipulationQueue { get; } = new(1, int.MaxValue);

    public ApplicationDbContext GetDbContext()
    {
        return _dbContext ??= new ApplicationDbContext();
    }

    public void SwitchTenant(int tenantId)
    {
        if (tenantId == TenantId) return;
        var tenant = tenantStore.GetTenant(tenantId).Result;
        if (tenant == null)
        {
            throw new Exception($"Couldn't find tenant Id {tenantId}");
        }

        TenantId = tenantId;
        if (_dbContext != null)
        {
            ManipulationQueue.WaitOne();
            _dbContext.SaveChanges();
            _dbContext.Dispose();
            _dbContext = new ApplicationDbContext(tenantId, tenant.ConnectionString);
            ManipulationQueue.Release();
        }
        else
        {
            _dbContext = new ApplicationDbContext(tenantId, tenant.ConnectionString);
        }
    }

    public void ResolveTenant(HttpContext context)
    {
        SwitchTenant(tenantResolver.ResolveTenant(context));
    }

    public IEnumerable<TenantEntry> GetAllTenants()
    {
        return tenantStore.GetAllTenants().ToArray();
    }

    public void Dispose()
    {
        if (_dbContext != null)
        {
            ManipulationQueue.WaitOne();
            _dbContext.SaveChanges();
            _dbContext.Dispose();
            ManipulationQueue.Release();
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            ManipulationQueue.WaitOne();
            await _dbContext.SaveChangesAsync();
            await _dbContext.DisposeAsync();
            ManipulationQueue.Release();
        }

        GC.SuppressFinalize(this);
    }
}