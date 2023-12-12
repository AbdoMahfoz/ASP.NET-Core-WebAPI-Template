using System;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DataModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Repository.Tenant.Interfaces;

namespace Repository;

public class Repository<T>(ITenantManager tenantManager)
    : IRepository<T>
    where T : BaseModel
{
    private ApplicationDbContext Context => tenantManager.GetDbContext();
    private DbSet<T> EntitiesSet => tenantManager.GetDbContext().Set<T>();
    private readonly Semaphore _manipulationQueue = new(1, int.MaxValue);

    private class ManipulationWait(Semaphore manipulationQueue) : IDisposable
    {
        public static async Task<ManipulationWait> Wait(Semaphore manipulationQueue)
        {
            await Task.Run(manipulationQueue.WaitOne);
            return new ManipulationWait(manipulationQueue);
        }

        public void Dispose()
        {
            manipulationQueue.Release();
        }
    }

    public virtual IQueryable<T> GetAll()
    {
        using var _ = ManipulationWait.Wait(_manipulationQueue).Result;
        return EntitiesSet;
    }

    public virtual async Task<T> Get(int id)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        var result = await GetAll().FirstOrDefaultAsync(e => e.Id == id);
        return result;
    }

    public virtual async Task Insert(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        await EntitiesSet.AddAsync(entity);
        await SaveChangesHelper();
    }

    public virtual async Task InsertRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        await this.EntitiesSet.AddRangeAsync(Entities);
        await SaveChangesHelper();
    }

    public virtual async Task Update(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        EntitiesSet.Update(entity);
        await SaveChangesHelper();
    }

    public virtual async Task UpdateRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        this.EntitiesSet.UpdateRange(Entities.ToArray());
        await SaveChangesHelper();
    }

    public virtual async Task SoftDelete(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        entity.IsDeleted = true;
        EntitiesSet.Update(entity);
        await SaveChangesHelper();
    }

    public virtual async Task SoftDeleteRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        foreach (var item in Entities)
        {
            item.IsDeleted = true;
            this.EntitiesSet.Update(item);
        }

        await SaveChangesHelper();
    }

    public virtual async Task HardDelete(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        EntitiesSet.Remove(entity);
        await SaveChangesHelper();
    }

    public virtual async Task HardDeleteRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        this.EntitiesSet.RemoveRange(Entities);
        await SaveChangesHelper();
    }

    private Task SaveChangesHelper()
    {
        return Context.SaveChangesAsync();
    }

    public async Task SaveChanges()
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        await SaveChangesHelper();
    }
}