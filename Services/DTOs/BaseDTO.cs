using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Services.DTOs;

public class BaseDto
{
    public string Id { get; set; }
}

/// <summary>
///     One page of results. PageIndex is 1-based (see Services.DTOs.PagedRequestBase), PerPage is the page size actually applied,
///     TotalCount is every matching item and TotalPageCount is how many pages that many items produce.
/// </summary>
public class PagedResult<T>
{
    public int PageIndex { get; set; }
    public int PerPage { get; set; }
    public int TotalCount { get; set; }
    public int TotalPageCount { get; set; }
    public IEnumerable<T> Data { get; set; }
}

public class EnumResult
{
    public int Id { get; set; }
    public string Name { get; set; }
}

/// <summary>
///     Machine-readable pagination failure codes. The message of every paging ValidationResult is exactly one of these
///     names, so clients can switch on a stable token instead of on human-readable text.
/// </summary>
public enum PaginationError
{
    /// <summary>Paging is 1-based in this template, so PageIndex must be 1 or greater.</summary> 
    PageIndexMustBeAtLeastOne,

    /// <summary>A page must contain at least one item.</summary> 
    PerPageMustBeAtLeastOne,

    /// <summary>PerPage is capped so a single request cannot ask for the whole collection.</summary> 
    PerPageExceedsMaximum,

    /// <summary>Fewer items exist than the requested page size.</summary> 
    PerPageExceedsTotalItems,

    /// <summary>The requested ordering column is not valid for this request.</summary> 
    InvalidOrderBy
}

/// <summary>Machine-readable codes for free-text lookup rules.</summary> 
public enum LookupError
{
    QueryTooShort,
    QueryTooLong
}

/// <summary>
///     Base class for every paged request: 1-based PageIndex and a PerPage ceiling of <see cref="MaxPerPage" />.
///     Validation results carry the <see cref="PaginationError" /> name as their message so the response body stays
///     machine-readable all the way from the model to the client. Rules are executed by MVC's DataAnnotations pipeline
///     and re-checked by WebAPI's ValidatorActionFilter. Override <see cref="GetTotalItems" /> when the request knows how many items it pages over, which enables the PerPage &lt;= TotalItems rule. 
/// </summary> 
public class PagedRequestBase : IValidatableObject 
{ 
    public const int MinPageIndex = 1; 
    public const int DefaultPerPage = 20; 
    public const int MaxPerPage = 500; 

    /// <summary>1-based page index.</summary> 
    public int PageIndex { get; set; } = MinPageIndex; 

    /// <summary>Items per page, between 1 and 500.</summary> 
    public int PerPage { get; set; } = DefaultPerPage; 

    /// <summary>Name of the property to order by, when the endpoint supports ordering.</summary> 
    public string OrderBy { get; set; } 

    /// <summary>True when OrderBy should be applied in descending order.</summary> 
    public bool Descending { get; set; } 

    /// <returns>Number of items this request pages over, or null when unknown (the TotalItems rule is then skipped).</returns> 
    protected virtual int? GetTotalItems(ValidationContext validationContext) 
    { 
        return null; 
    } 

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) 
    { 
        if (PageIndex < MinPageIndex) 
            yield return new ValidationResult(nameof(PaginationError.PageIndexMustBeAtLeastOne), new[] { nameof(PageIndex) }); 

        if (PerPage < 1) yield return new ValidationResult(nameof(PaginationError.PerPageMustBeAtLeastOne), new[] { nameof(PerPage) }); 
        else if (PerPage > MaxPerPage) yield return new ValidationResult(nameof(PaginationError.PerPageExceedsMaximum), new[] { nameof(PerPage) }); 

        var totalItems = GetTotalItems(validationContext); 
        if (totalItems.HasValue && PerPage > totalItems.Value && totalItems.Value > 0) 
            yield return new ValidationResult(nameof(PaginationError.PerPageExceedsTotalItems), new[] { nameof(PerPage) }); 

        if (!string.IsNullOrWhiteSpace(OrderBy) && !IsOrderable(OrderBy)) 
            yield return new ValidationResult(nameof(PaginationError.InvalidOrderBy), new[] { nameof(OrderBy) }); 
    } 

    /// <summary>Override to restrict ordering to the columns the endpoint actually supports.</summary> 
    protected virtual bool IsOrderable(string orderBy) 
    { 
        return true; 
    } 
} 

/// <summary>
///     Paged lookup request with an optional free-text Query (2..100 characters).
/// </summary>
public class LookupRequestBase : PagedRequestBase
{
    public const int MinQueryLength = 2;
    public const int MaxQueryLength = 100;

    /// <summary>Optional free-text filter, between 2 and 100 characters when present.</summary>
    public string Query { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext)) yield return result;

        if (string.IsNullOrWhiteSpace(Query)) yield break;

        if (Query.Length < MinQueryLength)
            yield return new ValidationResult(nameof(LookupError.QueryTooShort), new[] { nameof(Query) });
        else if (Query.Length > MaxQueryLength)
            yield return new ValidationResult(nameof(LookupError.QueryTooLong), new[] { nameof(Query) });
    }

    /// <summary>True when the request carries a usable query term.</summary>
    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
}
