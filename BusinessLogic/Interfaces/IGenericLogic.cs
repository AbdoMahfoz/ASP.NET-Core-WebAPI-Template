using System.Collections.Generic;
using Models.DataModels;
using Models.GenericControllerDTOs;

namespace BusinessLogic.Interfaces
{
    public interface IGenericLogic<T, DIn, DOut> where T : BaseModel, new()
        where DOut : BaseDTO, new()
    {
        IEnumerable<DOut> GetAll(IDictionary<string, object> relationalIds = null);
        DOut Get(int id);
        int Insert(DIn entity);
        IEnumerable<int> InsertRange(IEnumerable<DIn> entities);
        void Update(int Id, DIn entity);
        void SoftDelete(int id);
        void SoftDeleteRange(IEnumerable<int> ids);
        void HardDelete(int id);
        void HardDeleteRange(IEnumerable<int> ids);
        void SaveChanges();
    }
}