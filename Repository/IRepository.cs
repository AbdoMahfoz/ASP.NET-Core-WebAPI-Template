using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.DataModels;

namespace Repository;

public interface IRepository<T> where T : BaseModel
{
    IQueryable<T> GetAll();
    Task<T> Get(int id);
    Task Insert(T entity);
    Task InsertRange(IEnumerable<T> entities);
    Task Update(T entity);
    Task UpdateRange(IEnumerable<T> entities);
    Task SoftDelete(T entity);
    Task SoftDeleteRange(IEnumerable<T> entities);
    Task HardDelete(T entity);
    Task HardDeleteRange(IEnumerable<T> entities);
    Task SaveChanges();
}