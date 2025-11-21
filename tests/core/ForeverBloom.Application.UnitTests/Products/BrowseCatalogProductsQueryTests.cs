using FluentAssertions;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;
using ForeverBloom.Application.Sorting;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class BrowseCatalogProductsQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreValid()
    {
        const int pageNumber = 2;
        const int pageSize = 40;
        const string sortStrategy = "price_desc";
        const long categoryId = 14;
        const bool featured = true;

        var createResult = BrowseCatalogProductsQuery.Create(
            pageNumber,
            pageSize,
            sortStrategy,
            categoryId,
            featured);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(pageNumber);
        query.PageSize.Should().Be(pageSize);
        query.SortStrategy.Id.Should().Be(sortStrategy);
        query.CategoryId.Should().Be(categoryId);
        query.Featured.Should().Be(featured);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreNotProvided()
    {
        var createResult = BrowseCatalogProductsQuery.Create();

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        query.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
        query.SortStrategy.Id.Should().Be("relevance");
        query.CategoryId.Should().BeNull();
        query.Featured.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenPageNumberIsNotPositive(int invalidPageNumber)
    {
        var createResult = BrowseCatalogProductsQuery.Create(pageNumber: invalidPageNumber);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageNumber>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PaginationConstants.MaximumPageSize + 1)]
    public void Create_ShouldReturnFailure_WhenPageSizeIsOutsideAllowedRange(int invalidPageSize)
    {
        var createResult = BrowseCatalogProductsQuery.Create(pageSize: invalidPageSize);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortStrategyIsInvalid()
    {
        var createResult = BrowseCatalogProductsQuery.Create(sortStrategy: "price_low");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortStrategy>();
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenSortStrategyIsMissingOrWhitespace()
    {
        var createResult = BrowseCatalogProductsQuery.Create(sortStrategy: " ");

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.SortStrategy.Id.Should().Be("relevance");
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenSortStrategyHasDifferentCasing()
    {
        var createResult = BrowseCatalogProductsQuery.Create(sortStrategy: "PRICE_ASC");

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.SortStrategy.Id.Should().Be("PRICE_ASC");
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const string invalidSortStrategy = "tolerance";
        const int invalidPageSize = PaginationConstants.MaximumPageSize + 1;

        var createResult = BrowseCatalogProductsQuery.Create(
            pageSize: invalidPageSize,
            sortStrategy: invalidSortStrategy);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortStrategy>();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
    }
}
