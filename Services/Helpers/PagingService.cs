using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Services.DTOs;

namespace Services.Helpers;

public class PagingService
{
    public static PagedResult<T> Pagination<T, TOt>(int PageIndex, int PerPage, IQueryable<T> source,
        Expression<Func<T, TOt>> orderByColumn, bool IsAscending = true)
    {
            IOrderedQueryable<T> data;
            if (IsAscending) data = source.OrderBy(orderByColumn);
            else data = source.OrderByDescending(orderByColumn);
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }

    public static PagedResult<T> Pagination<T, TOt>(int PageIndex, int PerPage, IEnumerable<T> source,
        Func<T, TOt> orderByColumn, bool IsAscending = true)
    {
            IOrderedEnumerable<T> data;
            if (IsAscending) data = source.OrderBy(orderByColumn);
            else data = source.OrderByDescending(orderByColumn);
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }

    public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IQueryable<T> source,
        string orderByColumn, bool IsAscending = true)
    {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter);
            var orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            var orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            var data = (IOrderedQueryable<T>) (from method in typeof(Queryable).GetMethods()
                    where method.Name == orderMethod && method.GetParameters().Length == 2
                    select method).Single().MakeGenericMethod(typeof(T), orderByType)
                .Invoke(null, new object[] {source, lambda});
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }

    public static PagedResult<T> Pagination<T>(int PageIndex, int PerPage, IEnumerable<T> source,
        string orderByColumn, bool IsAscending = true)
    {
            var parameter = Expression.Parameter(typeof(T), "u");
            var val = Expression.Property(parameter, orderByColumn);
            var lambda = Expression.Lambda(val, parameter).Compile();
            var orderMethod = "OrderBy";
            if (!IsAscending) orderMethod += "Descending";
            var orderByType = typeof(T).GetProperty(orderByColumn).PropertyType;
            var data = (IOrderedEnumerable<T>) (from method in typeof(Enumerable).GetMethods()
                    where method.Name == orderMethod && method.GetParameters().Length == 2
                    select method).Single().MakeGenericMethod(typeof(T), orderByType)
                .Invoke(null, new object[] {source, lambda});
            return new PagedResult<T>
            {
                PageIndex = PageIndex,
                PerPage = PerPage,
                TotalPageCount = (int) Math.Ceiling((double) source.Count() / PerPage),
                Data = data.Skip(PageIndex * PerPage).Take(PerPage)
            };
        }
}