using Models.DataModels;

namespace Repository
{
    public interface ICachedRepository<T> : IRepository<T> where T : BaseModel
    {
    }
}
