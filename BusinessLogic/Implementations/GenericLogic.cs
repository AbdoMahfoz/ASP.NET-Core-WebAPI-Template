using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository;

namespace BusinessLogic.Implementations
{
    public class GenericLogic<T> : IGenericLogic<T> where T : BaseModel
    {
        private readonly Repository<T> _genericRepository;
        GenericLogic(Repository<T> genericRepository)
        {
            _genericRepository = genericRepository;
        }

        public IQueryable<T> GetAll()
        {
            return _genericRepository.GetAll();
        }

        public T Get(int id)
        {
            return _genericRepository.Get(id);
        }

        public Task Insert(T entity)
        {
            return _genericRepository.Insert(entity);
        }

        public Task InsertRange(IEnumerable<T> entities)
        {
            return _genericRepository.InsertRange(entities);
        }

        public void Update(T entity)
        {
            _genericRepository.Update(entity);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _genericRepository.UpdateRange(entities);
        }

        public void SoftDelete(int id)
        {
            var x = Get(id);
            _genericRepository.SoftDelete(entity: x == null
                ? throw new KeyNotFoundException($"{id} is not found in the database") : x);
        }

        public void SoftDeleteRange(IEnumerable<int> ids)
        {
            List<T> entities = new List<T>();
            foreach (var id in ids)
            {
                var x = Get(id);
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
