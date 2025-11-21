using FluentAssertions;
using ForeverBloom.Application.Categories.Queries.ListCategories;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class ListCategoriesQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreValid()
    {
        const int pageNumber = 2;
        const int pageSize = 50;
        const string sortBy = "name:asc,updated_at:desc";
        const string searchTerm = "orchid";
        const long rootCategoryId = 42;
        const bool includeSubcategories = true;
        const string publishStatus = "published";

        var createResult = ListCategoriesQuery.Create(
            pageNumber,
            pageSize,
            sortBy,
            searchTerm,
            rootCategoryId,
            includeSubcategories,
            publishStatus);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(pageNumber);
        query.PageSize.Should().Be(pageSize);
        query.SearchTerm.Should().Be(searchTerm);
        query.RootCategoryId.Should().Be(rootCategoryId);
        query.IncludeSubcategories.Should().Be(includeSubcategories);
        query.PublishStatus.Should().Be(PublishStatus.Published.Code);
        query.SortBy.Should().BeEquivalentTo([
            new SortProperty("name", SortDirection.Ascending),
            new SortProperty("updated_at", SortDirection.Descending)
        ]);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenNoParametersWereProvided()
    {
        var createResult = ListCategoriesQuery.Create();

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.PageNumber.Should().Be(PaginationConstants.DefaultPageNumber);
        query.PageSize.Should().Be(PaginationConstants.DefaultPageSize);
        query.SearchTerm.Should().BeNull();
        query.RootCategoryId.Should().BeNull();
        query.IncludeSubcategories.Should().BeNull();
        query.PublishStatus.Should().BeNull();
        query.SortBy.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenPageNumberIsNotPositive(int invalidPageNumber)
    {
        var createResult = ListCategoriesQuery.Create(pageNumber: invalidPageNumber);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageNumber>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PaginationConstants.MaximumPageSize + 1)]
    public void Create_ShouldReturnFailure_WhenPageSizeIsOutsideAllowedRange(int invalidPageSize)
    {
        var createResult = ListCategoriesQuery.Create(pageSize: invalidPageSize);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortStringIsMalformed()
    {
        var createResult = ListCategoriesQuery.Create(sortBy: "nameasc");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortFormat>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortDirectionIsInvalid()
    {
        var createResult = ListCategoriesQuery.Create(sortBy: "name:ascending");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortDirection>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortPropertyIsNotAllowed()
    {
        var createResult = ListCategoriesQuery.Create(sortBy: "title:asc");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortProperty>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSortPropertiesContainDuplicates()
    {
        var createResult = ListCategoriesQuery.Create(sortBy: "name:asc,name:desc");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.DuplicateSortProperty>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenPublishStatusIsInvalid()
    {
        var createResult = ListCategoriesQuery.Create(publishStatus: "archived");

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PublishStatusErrors.InvalidName>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const string invalidSortBy = "name:ask";
        const int invalidPageSize = PaginationConstants.MaximumPageSize + 1;

        var createResult = ListCategoriesQuery.Create(
            pageSize: invalidPageSize,
            sortBy: invalidSortBy);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SortingErrors.InvalidSortDirection>();
        createResult.Should().HaveError<PaginationErrors.InvalidPageSize>();
    }
}
