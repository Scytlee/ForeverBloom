using FluentAssertions;
using ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class GetCategoryBySlugQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenSlugIsValid()
    {
        const string validSlug = "orchids";

        var createResult = GetCategoryBySlugQuery.Create(validSlug);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.Slug.Value.Should().Be(validSlug);
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSlugHasInvalidFormat()
    {
        const string invalidSlug = "Obviously.Invalid_Slug";

        var createResult = GetCategoryBySlugQuery.Create(invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }
}
