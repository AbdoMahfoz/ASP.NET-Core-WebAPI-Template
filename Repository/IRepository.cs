using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Models.DataModels;

namespace Repository;

public interface IRepository<T> where T : BaseModel
{
    IQueryable<T> GetAll();
    Task<T> Get(int id);
    Task Insert(T entity);

    /// <summary>Inserts and hands back the very instance the context is tracking, with its generated id already assigned.</summary>
    Task<T> InsertAndReturnTrackedEntity(T entity);
    Task InsertRange(IEnumerable<T> entities);
    Task Update(T entity);
    Task UpdateRange(IEnumerable<T> entities);
    Task SoftDelete(T entity);
    Task SoftDeleteRange(IEnumerable<T> entities);
    Task HardDelete(T entity);
    Task HardDeleteRange(IEnumerable<T> entities);
    /// <summary>
    ///     Flags exactly one property of an attached instance as modified, so SaveChanges writes that column only.
    ///     Use it when a caller holds a detached copy and wants to persist a single field without overwriting the rest.
    /// </summary>
    void MarkModified<TP>(T instance, Expression<Func<T, TP>> property);

    Task SaveChanges();
}