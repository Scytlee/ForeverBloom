using FluentAssertions;
using ForeverBloom.Application.Pagination;

namespace ForeverBloom.Application.UnitTests.Pagination;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(99, 10, 10)]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(50, 25, 2)]
    [InlineData(51, 25, 3)]
    [InlineData(15, 1, 15)]
    [InlineData(1001, 100, 11)]
    public void TotalPages_ShouldCalculateCorrectly_OnVariousScenarios(
        int totalCount,
        int pageSize,
        int expectedTotalPages)
    {
        var result = new TestPagedResult
        {
            Items = new List<string>().AsReadOnly(),
            PageNumber = 1,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        result.TotalPages.Should().Be(expectedTotalPages);
    }

    private sealed class TestPagedResult : PagedResult<string>;
}
