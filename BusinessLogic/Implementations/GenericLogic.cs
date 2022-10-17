using System;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Models.GenericControllerDTOs;
using Repository;
using Services.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

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

        public IEnumerable<DOut> GetAll(IDictionary<string, object> relationalIds = null)
        {
            var baseQuery = _genericRepository.GetAll();
            if (relationalIds != null)
            {
                foreach (var (key, id) in relationalIds)
                {
                    var s = char.ToUpper(key[0]) + key[1..];
                    var parameter = Expression.Parameter(typeof(T));
                    var member = Expression.PropertyOrField(parameter, s);
                    Expression<Func<T, bool>> lambda;
                    switch (id)
                    {
                        case long:
                        {
                            var val = Convert.ToInt32(id);
                            var equals = Expression.MakeBinary(ExpressionType.Equal, member, Expression.Constant(val));
                            lambda = Expression.Lambda<Func<T, bool>>(equals, true, parameter);
                            break;
                        }
                        case JArray arr:
                        {
                            var values = arr.Select(u => u.Value<int>()).ToArray();
                            var equals = Expression.Call(null,
                                typeof(Enumerable).GetMethods().First(u => u.Name == "Contains")
                                    .MakeGenericMethod(typeof(int)), Expression.Constant(values),
                                member);
                            lambda = Expression.Lambda<Func<T, bool>>(equals, true, parameter);
                            break;
                        }
                        default:
                            return null;
                    }
                    baseQuery = baseQuery.Where(lambda);
                }
            }

            return baseQuery.ToArray().Select(ObjectHelpers.MapTo<DOut>);
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
            var res = entities.Select(u => ObjectHelpers.MapTo<T>(u)).ToArray();
            _genericRepository.InsertRange(res).Wait();
            return res.Select(u => u.Id);
        }

        public void Update(int Id, DIn entity)
        {
            var obj = ObjectHelpers.MapTo<T>(entity);
            obj.Id = Id;
            _genericRepository.Update(obj);
        }

        public void SoftDelete(int id)
        {
            var x = _genericRepository.Get(id);
            _genericRepository.SoftDelete(
                entity: x ?? throw new KeyNotFoundException($"{id} is not found in the database"));
        }

        public void HardDelete(int id)
        {
            var x = _genericRepository.Get(id);
            _genericRepository.HardDelete(
                entity: x ?? throw new KeyNotFoundException($"{id} is not found in the database"));
        }

        public void SoftDeleteRange(IEnumerable<int> ids)
        {
            List<T> entities = new List<T>();
            foreach (var id in ids)
            {
                var x = _genericRepository.Get(id);
                if (x == null)
                    throw new KeyNotFoundException($"{id} is not found in the database");
                else
                    entities.Add(x);
            }

            _genericRepository.SoftDeleteRange(entities);
        }
        
        public void HardDeleteRange(IEnumerable<int> ids)
        {
            List<T> entities = new List<T>();
            foreach (var id in ids)
            {
                var x = _genericRepository.Get(id);
                if (x == null)
                    throw new KeyNotFoundException($"{id} is not found in the database");
                entities.Add(x);
            }

            _genericRepository.HardDeleteRange(entities);
        }

        public void SaveChanges()
        {
            _genericRepository.SaveChanges();
        }
    }
}