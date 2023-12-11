using System.Collections.Generic;
using Models.DataModels;
using Models.GenericControllerDTOs;

namespace BusinessLogic.Interfaces;

public interface IGenericLogic<T, TDIn, TDOut> where T : BaseModel, new()
    where TDOut : BaseDto, new()
{
    IEnumerable<TDOut> GetAll(IDictionary<string, object> relationalIds = null);
    TDOut Get(int id);
    int Insert(TDIn entity);
    IEnumerable<int> InsertRange(IEnumerable<TDIn> entities);
    void Update(int Id, TDIn entity);
    void SoftDelete(int id);
    void SoftDeleteRange(IEnumerable<int> ids);
    void HardDelete(int id);
    void HardDeleteRange(IEnumerable<int> ids);
    void SaveChanges();
}

public interface IUserGenericLogic<T, TDIn, TDOut> where T : BaseUserModel, new()
    where TDOut : BaseDto, new()
{
    IEnumerable<TDOut> GetAll(int userId, IDictionary<string, object> relationalIds = null);
    TDOut Get(int userId, int id);
    int Insert(int userId, TDIn entity);
    IEnumerable<int> InsertRange(int userId, IEnumerable<TDIn> entities);
    bool Update(int userId, int Id, TDIn entity);
    bool SoftDelete(int userId, int id);
    bool SoftDeleteRange(int userId, IEnumerable<int> ids);
    bool HardDelete(int userId, int id);
    bool HardDeleteRange(int userId, IEnumerable<int> ids);
    void SaveChanges();
}