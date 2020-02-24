using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Services.DTOs;

namespace Services.Helpers
{
    public class PagingService
    {
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
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
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
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }

        public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IQueryable<T> source,
            string orderByColumn, bool IsAscending = true)
        {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter);
            string orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            Type orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            IOrderedQueryable<T> Data = (IOrderedQueryable<T>) (from method in typeof(Queryable).GetMethods()
                    where method.Name == orderMethod && method.GetParameters().Length == 2
                    select method).Single().MakeGenericMethod(typeof(T), orderByType)
                .Invoke(null, new object[] {source, lambda});
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }

        public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IEnumerable<T> source,
            string orderByColumn, bool IsAscending = true)
        {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter).Compile();
            string orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            Type orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            IOrderedEnumerable<T> Data = (IOrderedEnumerable<T>) (from method in typeof(Enumerable).GetMethods()
                    where method.Name == orderMethod && method.GetParameters().Length == 2
                    select method).Single().MakeGenericMethod(typeof(T), orderByType)
                .Invoke(null, new object[] {source, lambda});
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = Data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
    }
}