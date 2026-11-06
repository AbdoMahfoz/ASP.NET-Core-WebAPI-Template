using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Services.DTOs;

namespace Services.Helpers;

/// <summary>
///     Turns a query or a sequence into one page of results. 
///     PageIndex is 1-based, matching Services/DTOs/BaseDTO.cs PagedRequestBase: page 1 is the first slice of the source. Out-of-range paging values are clamped here (PageIndex >= 1, PerPage >= 1) so callers that never went through request validation still page sanely instead of skipping items or dividing by zero. 
/// </summary> 
public static class PagingService 
{ 
    /// <summary>Clamps paging arguments to this template's contract: a 1-based page index and a page size of at least one.</summary> 
    private static (int PageIndex, int PerPage) NormalizePaging(int pageIndex, int perPage) 
    { 
        return (Math.Max(pageIndex, PagedRequestBase.MinPageIndex), Math.Max(perPage, 1)); 
    } 

    /// <summary>How many items a 1-based page index skips.</summary> 
    private static int OffsetOf(int pageIndex, int perPage) 
    { 
        return (pageIndex - 1) * perPage; 
    } 

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")] 
    private static PagedResult<T> MakePagedResult<T>(IEnumerable<T> data, int pageIndex, int perPage, 
        int? totalCount = null) 
    { 
        (pageIndex, perPage) = NormalizePaging(pageIndex, perPage); 

        var materializedData = data as T[] ?? data.ToArray(); 
        totalCount ??= materializedData.Length; 

        return new PagedResult<T> 
        { 
            PageIndex = pageIndex, 
            PerPage = perPage, 
            TotalCount = totalCount.Value, 
            TotalPageCount = (int)Math.Ceiling((double)totalCount.Value / perPage), 
            Data = materializedData.Skip(OffsetOf(pageIndex, perPage)).Take(perPage).ToArray() 
        }; 
    } 

    /// <summary>Pages over a query and maps only the fetched page, so the transform never sees more rows than were requested.</summary> 
    private static PagedResult<TRes> MakePagedResult<T, TRes>(IQueryable<T> data, int pageIndex, int perPage, 
        Func<IQueryable<T>, IEnumerable<TRes>> transform) 
    { 
        (pageIndex, perPage) = NormalizePaging(pageIndex, perPage); 

        var totalCount = data.Count(); 
        var page = data.Skip(OffsetOf(pageIndex, perPage)).Take(perPage).ToArray(); 

        return new PagedResult<TRes> 
        { 
            PageIndex = pageIndex, 
            PerPage = perPage, 
            TotalCount = totalCount, 
            TotalPageCount = (int)Math.Ceiling((double)totalCount / perPage),             
            Data = transform(page.AsQueryable()).ToArray()         
        };     
    }     

    private static object OrderByExpressionHelper<T>(IEnumerable<T> source, string orderByColumn, bool isAscending,         
        Type orderableType)     
    {         
        var parameter = Expression.Parameter(typeof(T), "u");         
        var member = Expression.Property(parameter, orderByColumn);         
        var lambda = Expression.Lambda(member, parameter);

        // Queryable.OrderBy wants the expression tree; Enumerable.OrderBy wants the compiled delegate.
        var keySelector = orderableType == typeof(Enumerable) ? (object)lambda.Compile() : lambda;         

        var orderMethod = isAscending ? "OrderBy" : "OrderByDescending";         
        var propertyType = typeof(T).GetProperty(orderByColumn)!.PropertyType;         

        return orderableType.GetMethods()             
            .Single(method => method.Name == orderMethod && method.GetParameters().Length == 2)             
            .MakeGenericMethod(typeof(T), propertyType)             
            .Invoke(null, [source, keySelector])!;     
    }     

    /// <summary>Order a query by a property named at runtime.</summary>     
    public static IOrderedQueryable<T> MakeOrderByExpression<T>(IQueryable<T> source, string orderByColumn,         
        bool isAscending = true)     
    {         
        return (IOrderedQueryable<T>)OrderByExpressionHelper(source, orderByColumn, isAscending, typeof(Queryable));     
    }     

    /// <summary>Order a sequence by a property named at runtime.</summary>     
    public static IOrderedEnumerable<T> MakeOrderByExpression<T>(IEnumerable<T> source, string orderByColumn,         
        bool isAscending = true)     
    {         
        return (IOrderedEnumerable<T>)OrderByExpressionHelper(source, orderByColumn, isAscending, typeof(Enumerable));     
    }     

    /// <summary>Unordered pagination over a query; the source keeps whatever order it already has.</summary> 
    public static PagedResult<T> Pagination<T>(int pageIndex, int perPage, IQueryable<T> source) 
    { 
        return MakePagedResult(source, pageIndex, perPage); 
    } 

    /// <summary>Paginates a query ordered by a compile-time property selector.</summary> 
    public static PagedResult<T> Pagination<T, TOrderBy>(int pageIndex, int perPage, IQueryable<T> source, 
        Expression<Func<T, TOrderBy>> orderByColumn, bool isAscending = true) 
    { 
        return MakePagedResult(isAscending ? source.OrderBy(orderByColumn) : source.OrderByDescending(orderByColumn), 
            pageIndex, perPage); 
    } 

    /// <summary>Paginates a query ordered by a compile-time property selector and maps only the fetched page.</summary> 
    public static PagedResult<TResult> Pagination<T, TOrderBy, TResult>(int pageIndex, int perPage, IQueryable<T> source, 
        Expression<Func<T, TOrderBy>> orderByColumn, Func<IQueryable<T>, IEnumerable<TResult>> mapToPageResultType, 
        bool isAscending = true) 
    { 
        return MakePagedResult(isAscending ? source.OrderBy(orderByColumn) : source.OrderByDescending(orderByColumn), 
            pageIndex, perPage, mapToPageResultType); 
    } 

    /// <summary>Paginates a query ordered by a property named at runtime.</summary> 
    public static PagedResult<T> Pagination<T>(int pageIndex, int perPage, IQueryable<T> source, string orderByColumn, 
        bool isAscending = true) 
    { 
        return MakePagedResult(MakeOrderByExpression(source, orderByColumn, isAscending), pageIndex, perPage); 
    } 

    /// <summary>Paginates a query ordered by a runtime property name and maps only the fetched page.</summary> 
    public static PagedResult<TResult> Pagination<T, TResult>(int pageIndex, int perPage, IQueryable<T> source, 
        string orderByColumn, Func<IQueryable<T>, IEnumerable<TResult>> mapToPageResultType, bool isAscending = true) 
    { 
        return MakePagedResult(MakeOrderByExpression(source, orderByColumn, isAscending), pageIndex, perPage,
            mapToPageResultType);
    }

    /// <summary>Paginates an in-memory sequence ordered by a property named at runtime.</summary>
    public static PagedResult<T> Pagination<T>(int pageIndex, int perPage, IEnumerable<T> source,
        string orderByColumn,
        bool isAscending = true)
    {
        var data = MakeOrderByExpression(source ?? throw new ArgumentNullException(nameof(source)), orderByColumn,
            isAscending).ToArray();
        return MakePagedResult(data.AsQueryable(), pageIndex, perPage);
    }

    /// <summary>Paginates an in-memory sequence ordered by a runtime property name and maps the page.</summary>
    public static PagedResult<TResult> Pagination<T, TResult>(int pageIndex, int perPage, IEnumerable<T> source,
        string orderByColumn, Func<IEnumerable<T>, IEnumerable<TResult>> mapToPageResultType,
        bool isAscending = true)
    {
        var data = MakeOrderByExpression(source ?? throw new ArgumentNullException(nameof(source)), orderByColumn,
            isAscending).ToArray();
        return MakePagedResult(data.AsQueryable(), pageIndex, perPage,
            query => mapToPageResultType(query.ToArray()));
    }
} 
