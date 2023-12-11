using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository;

public class Repository<T>(ApplicationDbContext context, ILogger<Repository<T>> logger)
    : IRepository<T>
    where T : BaseModel
{
    private readonly ILogger _logger = logger;
    private readonly DbSet<T> _entities = context.Set<T>();

    public virtual IQueryable<T> GetAll()
    {
        return _entities;
    }

    public virtual async Task<T> Get(int id)
    {
        var result = await GetAll().FirstOrDefaultAsync(e => e.Id == id);
        return result;
    }

    public virtual async Task Insert(T entity)
    {
        await _entities.AddAsync(entity);
        await SaveChanges();
    }

    public virtual async Task InsertRange(IEnumerable<T> Entities)
    {
        await _entities.AddRangeAsync(Entities);
        await SaveChanges();
    }

    public virtual Task Update(T entity)
    {
        _entities.Update(entity);
        return SaveChanges();
    }

    public virtual Task UpdateRange(IEnumerable<T> Entities)
    {
        _entities.UpdateRange(Entities.ToArray());
        return SaveChanges();
    }

    public virtual Task SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        return Update(entity);
    }

    public virtual async Task SoftDeleteRange(IEnumerable<T> Entities)
    {
        foreach (var item in Entities)
        {
            item.IsDeleted = true;
            await Update(item);
        }
    }

    public virtual Task HardDelete(T entity)
    {
        _entities.Remove(entity);
        return SaveChanges();
    }

    public virtual Task HardDeleteRange(IEnumerable<T> Entities)
    {
        _entities.RemoveRange(Entities);
        return SaveChanges();
    }

    public Task SaveChanges()
    {
        return context.SaveChangesAsync();
    }
}