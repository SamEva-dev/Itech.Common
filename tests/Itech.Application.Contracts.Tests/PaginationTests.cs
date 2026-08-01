using Itech.Application.Contracts.Pagination;

namespace Itech.Application.Contracts.Tests;

public sealed class PaginationTests
{
    [Fact]
    public void PageRequest_ComputesSkip()
    {
        var request = new PageRequest(page: 3, pageSize: 20);

        Assert.Equal(40, request.Skip);
    }

    [Fact]
    public void PageRequest_RejectsPageSizeAboveMaximum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PageRequest(pageSize: PaginationParameters.MaximumPageSize + 1));
    }

    [Fact]
    public void PagedResult_ComputesNavigation()
    {
        var result = new PagedResult<int>([1, 2], page: 2, pageSize: 2, totalCount: 5);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
