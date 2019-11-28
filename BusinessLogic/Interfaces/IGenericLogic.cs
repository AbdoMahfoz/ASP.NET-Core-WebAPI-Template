using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.DataModels;

namespace BusinessLogic.Interfaces
{
    public interface IGenericLogic<T> where T : BaseModel
    {
        IQueryable<T> GetAll();
        T Get(int id);
        Task Insert(T entity);
        Task InsertRange(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void SoftDelete(int id);
        void SoftDeleteRange(IEnumerable<int> ids);
        void SaveChanges();
    }
}
