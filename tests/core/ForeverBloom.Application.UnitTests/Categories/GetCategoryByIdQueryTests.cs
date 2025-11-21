using FluentAssertions;
using ForeverBloom.Application.Categories.Queries.GetCategoryById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class GetCategoryByIdQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenIdIsValid()
    {
        const long validId = 42;

        var createResult = GetCategoryByIdQuery.Create(validId);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.Id.Should().Be(validId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldReturnFailure_WhenIdIsNotPositive(long invalidId)
    {
        var createResult = GetCategoryByIdQuery.Create(invalidId);

        createResult.Should().BeFailure();
        var error = createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        error.Id.Should().Be(invalidId);
    }
}
