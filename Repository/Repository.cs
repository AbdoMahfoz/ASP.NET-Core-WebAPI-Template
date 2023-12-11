using System;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.DataModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Repository;

public class Repository<T>(ApplicationDbContext context)
    : IRepository<T>
    where T : BaseModel
{
    private readonly DbSet<T> _entities = context.Set<T>();
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
        return _entities;
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
        await _entities.AddAsync(entity);
        await SaveChanges();
    }

    public virtual async Task InsertRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        await _entities.AddRangeAsync(Entities);
        await SaveChanges();
    }

    public virtual async Task Update(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        _entities.Update(entity);
        await SaveChanges();
    }

    public virtual async Task UpdateRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        _entities.UpdateRange(Entities.ToArray());
        await SaveChanges();
    }

    public virtual async Task SoftDelete(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        entity.IsDeleted = true;
        _entities.Update(entity);
        await SaveChanges();
    }

    public virtual async Task SoftDeleteRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        foreach (var item in Entities)
        {
            item.IsDeleted = true;
            _entities.Update(item);
        }

        await SaveChanges();
    }

    public virtual async Task HardDelete(T entity)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        _entities.Remove(entity);
        await SaveChanges();
    }

    public virtual async Task HardDeleteRange(IEnumerable<T> Entities)
    {
        using var _ = await ManipulationWait.Wait(_manipulationQueue);
        _entities.RemoveRange(Entities);
        await SaveChanges();
    }

    public Task SaveChanges()
    {
        return context.SaveChangesAsync();
    }
}