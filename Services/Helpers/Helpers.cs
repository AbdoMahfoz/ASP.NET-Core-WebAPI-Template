using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Linq.Expressions;
using Services.DTOs;

namespace Services
{
    public static class Helpers
    {
        public static bool HasNullOrEmptyStrings<T>(T obj)
        {
            if (obj == null) return true;
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    string val = (string)propertyInfo.GetValue(obj);
                    return string.IsNullOrWhiteSpace(val);
                }
            }
            return false;
        }
        public static void UpdateObjects<T>(T oldobj, T newobj)
        {
            if (oldobj == null || newobj == null) throw new ArgumentNullException();
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;
                object val = propertyInfo.GetValue(newobj);
                if (val != null)
                {
                    propertyInfo.SetValue(oldobj, val);
                }
            }
        }
        public static T MapTo<T>(object obj) where T : new()
        {
            T res = new T();
            Dictionary<string, PropertyInfo> OutProps = new Dictionary<string, PropertyInfo>();
            foreach(var property in typeof(T).GetProperties())
            {
                if(property.CanRead)
                {
                    OutProps.Add(property.Name, property);
                }
            }
            foreach(var InProp in obj.GetType().GetProperties())
            {
                PropertyInfo OutProp;
                if(InProp.CanRead && OutProps.TryGetValue(InProp.Name, out OutProp))
                {
                    OutProp.SetValue(res, InProp.GetValue(obj));
                }
            }
            return res;
        }
        public static PagedResult<T> Pagination<T, OT>(int PageIndex, int PerPage, IQueryable<T> source,
            Expression<Func<T, OT>> orderByColumn, bool IsAscending = true)
        {
            IOrderedQueryable<T> Data;
            if (IsAscending) Data = source.OrderBy(orderByColumn);
            else Data = source.OrderByDescending(orderByColumn);
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int)Math.Ceiling((double)source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
        public static PagedResult<T> Pagination<T, OT>(int PageIndex, int PerPage, IEnumerable<T> source,
            Func<T, OT> orderByColumn, bool IsAscending = true)
        {
            IOrderedEnumerable<T> Data;
            if (IsAscending) Data = source.OrderBy(orderByColumn);
            else Data = source.OrderByDescending(orderByColumn);
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int)Math.Ceiling((double)source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
        public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IQueryable<T> source, string orderByColumn, bool IsAscending = true)
        {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter);
            string orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            Type orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            IOrderedQueryable<T> Data = (IOrderedQueryable<T>)(from method in typeof(Queryable).GetMethods()
                                                               where method.Name == orderMethod && method.GetParameters().Length == 2
                                                               select method).Single().MakeGenericMethod(typeof(T), orderByType)
                                                                                      .Invoke(null, new object[] { source, lambda });
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int)Math.Ceiling((double)source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
        public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IEnumerable<T> source, string orderByColumn, bool IsAscending = true)
        {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter).Compile();
            string orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            Type orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            IOrderedEnumerable<T> Data = (IOrderedEnumerable<T>)(from method in typeof(Enumerable).GetMethods()
                                                                 where method.Name == orderMethod && method.GetParameters().Length == 2
                                                                 select method).Single().MakeGenericMethod(typeof(T), orderByType)
                                                                                        .Invoke(null, new object[] { source, lambda });
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int)Math.Ceiling((double)source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
    }
}