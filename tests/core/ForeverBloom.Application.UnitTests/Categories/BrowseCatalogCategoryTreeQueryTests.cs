using FluentAssertions;
using ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class BrowseCatalogCategoryTreeQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenParametersAreValid()
    {
        const long rootCategoryId = 42;
        const int levels = 3;

        var createResult = BrowseCatalogCategoryTreeQuery.Create(rootCategoryId, levels);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.RootCategoryId.Should().Be(rootCategoryId);
        query.Levels.Should().Be(levels);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenNoParametersWereProvided()
    {
        var createResult = BrowseCatalogCategoryTreeQuery.Create();

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.RootCategoryId.Should().BeNull();
        query.Levels.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenRootCategoryIdIsInvalid(long invalidRootCategoryId)
    {
        var createResult = BrowseCatalogCategoryTreeQuery.Create(rootCategoryId: invalidRootCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Create_ShouldReturnFailure_WhenLevelsIsNegative(int invalidLevels)
    {
        var createResult = BrowseCatalogCategoryTreeQuery.Create(levels: invalidLevels);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<BrowseCatalogCategoryTreeErrors.LevelsOutOfRange>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidRootCategoryId = 0;
        const int invalidLevels = -1;

        var createResult = BrowseCatalogCategoryTreeQuery.Create(
            rootCategoryId: invalidRootCategoryId,
            levels: invalidLevels);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        createResult.Should().HaveError<BrowseCatalogCategoryTreeErrors.LevelsOutOfRange>();
    }
}
