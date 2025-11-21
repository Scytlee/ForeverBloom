using FluentAssertions;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.ListProducts;
using ForeverBloom.Application.Sorting;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class ListProductsQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreValid()
    {
        const int pageNumber = 2;
        const int pageSize = 50;
        const string sortBy = "name:asc,price:desc";
        const string searchTerm = "rose";
        const long categoryId = 8;
        const bool includeSubcategories = true;

        var createResult = ListProductsQuery.Create(
            pageNumber,
            pageSize,
            sortBy,
            searchTerm,
            categoryId,
            includeSubcategories);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(pageNumber);
        query.PageSize.Should().Be(pageSize);
        query.SortBy.Should().BeEquivalentTo([
            new SortProperty("name", SortDirection.Ascending),
            new SortProperty("price", SortDirection.Descending)
        ]);
        query.SearchTerm.Should().Be(searchTerm);
        query.CategoryId.Should().Be(categoryId);
        query.IncludeSubcategories.Should().Be(includeSubcategories);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreNotProvided()
    {
        var createResult = ListProductsQuery.Create();

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        query.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
        query.SortBy.Should().BeEmpty();
        query.SearchTerm.Should().BeNull();
        query.CategoryId.Should().BeNull();
        query.IncludeSubcategories.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenPageNumberIsNotPositive(int invalidPageNumber)
    {
        var createResult = ListProductsQuery.Create(pageNumber: invalidPageNumber);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageNumber>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PaginationConstants.MaximumPageSize + 1)]
    public void Create_ShouldReturnFailure_WhenPageSizeIsOutsideAllowedRange(int invalidPageSize)
    {
        var createResult = ListProductsQuery.Create(pageSize: invalidPageSize);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortByHasInvalidFormat()
    {
        var createResult = ListProductsQuery.Create(sortBy: "name");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortFormat>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortByHasInvalidDirection()
    {
        var createResult = ListProductsQuery.Create(sortBy: "name:ascending");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortDirection>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortByContainsUnsupportedProperty()
    {
        const string invalidProperty = "rating";

        var createResult = ListProductsQuery.Create(sortBy: $"{invalidProperty}:asc");

        createResult.Should().BeFailure();
        var error = createResult.Should().HaveError<SortingErrors.InvalidSortProperty>();
        error.PropertyName.Should().Be(invalidProperty);
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortByContainsDuplicateProperties()
    {
        var createResult = ListProductsQuery.Create(sortBy: "price:asc,PRICE:desc");

        createResult.Should().BeFailure();
        var error = createResult.Should().HaveError<SortingErrors.DuplicateSortProperty>();
        error.PropertyName.Should().Be("price");
        error.PropertyIndices.Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const int invalidPageSize = PaginationConstants.MaximumPageSize + 1;
        const string invalidSortBy = "invalid";

        var createResult = ListProductsQuery.Create(
            pageSize: invalidPageSize,
            sortBy: invalidSortBy);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
        createResult.Should().HaveError<SortingErrors.InvalidSortFormat>();
    }
}
