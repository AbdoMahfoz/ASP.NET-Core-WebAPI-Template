using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Models;
using Repository.Tenant.Interfaces;

namespace Repository.Tenant.Implementations;

public class TenantManager : ITenantManager, IDisposable, IAsyncDisposable
{
    private ApplicationDbContext _dbContext;
    private readonly ApplicationDbContext _tenantContext = new();
    private readonly ITenantResolver _tenantResolver;
    private static readonly object TenantLock = new();

    public TenantManager(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
        lock (TenantLock)
        {
            if (!_tenantContext.Set<Models.DataModels.Tenant>().Any())
            {
                _tenantContext.Set<Models.DataModels.Tenant>().AddRange(
                    new Models.DataModels.Tenant
                    {
                        TenantId = 1,
                        ConnectionString = null
                    },
                    new Models.DataModels.Tenant
                    {
                        TenantId = 2,
                        ConnectionString = null
                    }
                );
                _tenantContext.SaveChanges();
            }
        }
    }

    public int TenantId { get; private set; } = 1;
    public Semaphore ManipulationQueue { get; } = new(1, int.MaxValue);

    public ApplicationDbContext GetDbContext()
    {
        return _dbContext ??= new ApplicationDbContext();
    }

    public void SwitchTenant(int tenantId)
    {
        if (tenantId == TenantId) return;
        var tenant = _tenantContext.Set<Models.DataModels.Tenant>().SingleOrDefault(u => u.TenantId == tenantId);
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
        SwitchTenant(_tenantResolver.ResolveTenant(context));
    }

    public IEnumerable<int> GetAllTenants()
    {
        return _tenantContext.Set<Models.DataModels.Tenant>().Select(u => u.TenantId).ToArray();
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