using System.Collections.Generic;
using System.Linq;
using Models.DataModels.SimpleModels;
using Services.DTOs;
using Services.Helpers;
using Xunit;

namespace UnitTests;

/// <summary>
/// Paging contract evidence against DemoModel rows: out-of-range page indices are clamped while totals survive,
/// ordering works over both IQueryable and IEnumerable sources in both directions, and the mapped overloads project
/// rows without losing the total counts the pagination contract promises.
/// </summary>
public class PagingContractTests
{
    private static List<DemoModel> Samples() => new()
    {
        new DemoModel { Name = "delta" },
        new DemoModel { Name = "alpha" },
        new DemoModel { Name = "charlie" },
        new DemoModel { Name = "bravo" }
    };

    private static string[] Names(PagedResult<DemoModel> page) => page.Data.Select(u => u.Name).ToArray();

    [Fact]
    public void OutOfRangePageIndicesAreClampedWhileTotalCountsAndPagesArePreserved()
    {
        var orderedFromZero = PagingService.Pagination(0, 2, Samples().AsQueryable(), nameof(DemoModel.Name));

        Assert.Equal(PagedRequestBase.MinPageIndex, orderedFromZero.PageIndex);
        Assert.Equal(new[] { "alpha", "bravo" }, Names(orderedFromZero));

        var unorderedFromNegative = PagingService.Pagination(-7, 2, Samples().AsQueryable());

        Assert.Equal(PagedRequestBase.MinPageIndex, unorderedFromNegative.PageIndex);
        Assert.Equal(2, unorderedFromNegative.Data.Count());

        var beyondLastPage = PagingService.Pagination(99, 2, Samples().AsQueryable(), nameof(DemoModel.Name));

        Assert.Empty(beyondLastPage.Data);
        Assert.Equal(4, beyondLastPage.TotalCount);
        Assert.Equal(2, beyondLastPage.TotalPageCount);
    }

    [Fact]
    public void OrderingHelpersSortQueryableAndEnumerableSourcesInBothDirections()
    {
        var expectedAscending = new[] { "alpha", "bravo", "charlie", "delta" };

        var expectedDescending = new[] { "delta", "charlie", "bravo", "alpha" };


        Assert.Equal(expectedAscending, PagingService.MakeOrderByExpression(Samples().AsQueryable(), nameof(DemoModel.Name)).Select(u => u.Name).ToArray());


        Assert.Equal(expectedDescending, PagingService.MakeOrderByExpression(Samples().AsQueryable(), nameof(DemoModel.Name), false).Select(u => u.Name).ToArray());


        Assert.Equal(expectedAscending, PagingService.MakeOrderByExpression(Samples(), nameof(DemoModel.Name)).Select(u => u.Name).ToArray());


        Assert.Equal(expectedDescending, PagingService.MakeOrderByExpression(Samples(), nameof(DemoModel.Name), false).Select(u => u.Name).ToArray());

    }

    [Fact]
    public void PaginationOverloadsOrderAndMapRowsWhileReportingTotals()
    {
        var source = Samples();


        var expressionOrdered = PagingService.Pagination<DemoModel, string>(1, 2, source.AsQueryable(), u => u.Name);


        Assert.Equal(new[] { "alpha", "bravo" }, Names(expressionOrdered));


        var columnOrdered = PagingService.Pagination(2, 2, source.AsQueryable(), nameof(DemoModel.Name), false);


        Assert.Equal(new[] { "bravo", "alpha" }, Names(columnOrdered));


        var expressionMapped = PagingService.Pagination<DemoModel, string, string>(1, 2, source.AsQueryable(), u => u.Name, q => q.Select(u => u.Name));


        Assert.Equal(new[] { "alpha", "bravo" }, expressionMapped.Data.ToArray());

        Assert.Equal(4, expressionMapped.TotalCount);

        Assert.Equal(2, expressionMapped.TotalPageCount);


        var enumerableMapped = PagingService.Pagination<DemoModel, string>(1, 3, source.AsEnumerable(), nameof(DemoModel.Name), e => e.Select(u => u.Name));


        Assert.Equal(new[] { "alpha", "bravo", "charlie" }, enumerableMapped.Data.ToArray());

        Assert.Equal(4, enumerableMapped.TotalCount);

    }
}
