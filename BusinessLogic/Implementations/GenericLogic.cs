using System;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Models.GenericControllerDTOs;
using Repository;
using Services.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BusinessLogic.Implementations;

public class GenericLogic<T, TDIn, TDOut>(IRepository<T> genericRepository) : IGenericLogic<T, TDIn, TDOut>
    where T : BaseModel, new()
    where TDOut : BaseDto, new()
{
    public virtual IEnumerable<TDOut> GetAll(IDictionary<string, object> relationalIds = null)
    {
        var baseQuery = genericRepository.GetAll();
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

        return baseQuery.ToArray().Select(ObjectHelpers.MapTo<TDOut>);
    }

    public virtual TDOut Get(int id)
    {
        return ObjectHelpers.MapTo<TDOut>(genericRepository.Get(id).Result);
    }

    public virtual int Insert(TDIn entity)
    {
        var res = ObjectHelpers.MapTo<T>(entity);
        genericRepository.Insert(res).Wait();
        return res.Id;
    }

    public virtual IEnumerable<int> InsertRange(IEnumerable<TDIn> entities)
    {
        var res = entities.Select(u => ObjectHelpers.MapTo<T>(u)).ToArray();
        genericRepository.InsertRange(res).Wait();
        return res.Select(u => u.Id);
    }

    public virtual void Update(int Id, TDIn entity)
    {
        var obj = ObjectHelpers.MapTo<T>(entity);
        obj.Id = Id;
        genericRepository.Update(obj).Wait();
    }

    public virtual void SoftDelete(int id)
    {
        var x = genericRepository.Get(id).Result;
        if (x == null) return;
        genericRepository.SoftDelete(x).Wait();
    }

    public virtual void HardDelete(int id)
    {
        genericRepository.GetAll().Where(u => u.Id == id).ExecuteDelete();
    }

    public virtual void SoftDeleteRange(IEnumerable<int> ids)
    {
        genericRepository.GetAll().Where(u => ids.Contains(u.Id))
            .ExecuteUpdate(u => u.SetProperty(x => x.IsDeleted, x => true));
    }

    public virtual void HardDeleteRange(IEnumerable<int> ids)
    {
        genericRepository.GetAll().Where(u => ids.Contains(u.Id)).ExecuteDelete();
    }

    public virtual void SaveChanges()
    {
        genericRepository.SaveChanges().Wait();
    }
}

public class UserGenericLogic<T, TDIn, TDOut>(IRepository<T> genericRepository) : IUserGenericLogic<T, TDIn, TDOut>
    where T : BaseUserModel, new()
    where TDOut : BaseDto, new()
{
    public virtual IEnumerable<TDOut> GetAll(int userId, IDictionary<string, object> relationalIds = null)
    {
        var baseQuery = genericRepository.GetAll();
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
                    case bool:
                    {
                        var equals = Expression.MakeBinary(ExpressionType.Equal, member, Expression.Constant(id));
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

        return baseQuery.Where(u => u.UserId == userId).ToArray().Select(ObjectHelpers.MapTo<TDOut>);
    }

    public virtual TDOut Get(int userId, int id)
    {
        var res = genericRepository.Get(id).Result;
        if (res != null && res.UserId == userId)
        {
            return ObjectHelpers.MapTo<TDOut>(res);
        }

        return null;
    }

    public virtual int Insert(int userId, TDIn entity)
    {
        var res = ObjectHelpers.MapTo<T>(entity);
        res.UserId = userId;
        genericRepository.Insert(res).Wait();
        return res.Id;
    }

    public virtual IEnumerable<int> InsertRange(int userId, IEnumerable<TDIn> entities)
    {
        var res = entities.Select(u =>
        {
            var x = ObjectHelpers.MapTo<T>(u);
            x.UserId = userId;
            return x;
        }).ToArray();
        genericRepository.InsertRange(res).Wait();
        return res.Select(u => u.Id);
    }

    public virtual bool Update(int userId, int Id, TDIn entity)
    {
        if (!genericRepository.GetAll().Any(u => u.Id == Id && u.UserId == userId))
        {
            return false;
        }

        var obj = ObjectHelpers.MapTo<T>(entity);

        obj.Id = Id;
        obj.UserId = userId;
        genericRepository.Update(obj).Wait();
        return true;
    }

    public virtual bool SoftDelete(int userId, int id)
    {
        var x = genericRepository.Get(id).Result;
        if (x == null || x.UserId != userId) return false;
        genericRepository.SoftDelete(x).Wait();
        return true;
    }

    public virtual bool HardDelete(int userId, int id)
    {
        genericRepository.GetAll().Where(u => u.Id == id && u.UserId == userId).ExecuteDelete();
        return true;
    }

    public virtual bool SoftDeleteRange(int userId, IEnumerable<int> ids)
    {
        genericRepository.GetAll().Where(u => ids.Contains(u.Id) && u.UserId == userId)
            .ExecuteUpdate(u => u.SetProperty(x => x.IsDeleted, x => true));
        return true;
    }

    public virtual bool HardDeleteRange(int userId, IEnumerable<int> ids)
    {
        genericRepository.GetAll().Where(u => ids.Contains(u.Id) && u.UserId == userId).ExecuteDelete();
        return true;
    }

    public virtual void SaveChanges()
    {
        genericRepository.SaveChanges().Wait();
    }
}