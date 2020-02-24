using BusinessLogic.Interfaces;

using Models.DataModels;
using Models.GenericControllerDTOs;

using Repository;

using Services.Helpers;

using System.Collections.Generic;
using System.Linq;

namespace BusinessLogic.Implementations
{
    public class GenericLogic<T, DIn, DOut> : IGenericLogic<T, DIn, DOut> where T : BaseModel, new()
                                                                          where DOut : BaseDTO, new()
    {
        private readonly IRepository<T> _genericRepository;
        public GenericLogic(IRepository<T> genericRepository)
        {
            _genericRepository = genericRepository;
        }

        public IQueryable<DOut> GetAll()
        {
            return _genericRepository.GetAll().Select(u => ObjectHelpers.MapTo<DOut>(u));
        }

        public DOut Get(int id)
        {
            return ObjectHelpers.MapTo<DOut>(_genericRepository.Get(id));
        }

        public int Insert(DIn entity)
        {
            var res = ObjectHelpers.MapTo<T>(entity);
            _genericRepository.Insert(res).Wait();
            return res.Id;
        }

        public IEnumerable<int> InsertRange(IEnumerable<DIn> entities)
        {
            var res = entities.Select(u => ObjectHelpers.MapTo<T>(u));
            _genericRepository.InsertRange(res).Wait();
            return res.Select(u => u.Id);
        }

        public void Update(DIn entity)
        {
            _genericRepository.Update(ObjectHelpers.MapTo<T>(entity));
        }

        public void UpdateRange(IEnumerable<DIn> entities)
        {
            _genericRepository.UpdateRange(entities.Select(u => ObjectHelpers.MapTo<T>(u)));
        }

        public void SoftDelete(int id)
        {
            var x = _genericRepository.Get(id);
            _genericRepository.SoftDelete(entity: x ?? throw new KeyNotFoundException($"{id} is not found in the database"));
        }

        public void SoftDeleteRange(IEnumerable<int> ids)
        {
            List<T> entities = new List<T>();
            foreach (var id in ids)
            {
                var x =_genericRepository.Get(id);
                if (x == null)
                    throw new KeyNotFoundException($"{id} is not found in the database");
                else
                    entities.Add(x);
            }
            _genericRepository.SoftDeleteRange(entities);
        }

        public void SaveChanges()
        {
            _genericRepository.SaveChanges();
        }
    }
}
