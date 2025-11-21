using FluentAssertions;
using ForeverBloom.Application.Products.Queries.GetProductBySlug;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class GetProductBySlugQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenSlugIsValid()
    {
        const string validSlug = "red-rose";

        var createResult = GetProductBySlugQuery.Create(validSlug);

        createResult.Should().BeSuccess();
        var query = createResult.Value!;
        query.Slug.Should().Be(validSlug);
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenSlugHasInvalidFormat()
    {
        const string invalidSlug = "Obviously.Invalid_Slug";

        var createResult = GetProductBySlugQuery.Create(invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }
}
