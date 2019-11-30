using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.DataModels;
using Models.GenericControllerDTOs;

namespace BusinessLogic.Interfaces
{
    public interface IGenericLogic<T, DIn, DOut> where T : BaseModel, new()
                                                 where DOut : BaseDTO, new()
    {
        IQueryable<DOut> GetAll();
        DOut Get(int id);
        int Insert(DIn entity);
        IEnumerable<int> InsertRange(IEnumerable<DIn> entities);
        void Update(DIn entity);
        void UpdateRange(IEnumerable<DIn> entities);
        void SoftDelete(int id);
        void SoftDeleteRange(IEnumerable<int> ids);
        void SaveChanges();
    }
}
