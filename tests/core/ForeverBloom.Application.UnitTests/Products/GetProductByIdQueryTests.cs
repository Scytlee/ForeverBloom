using FluentAssertions;
using ForeverBloom.Application.Products.Queries.GetProductById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class GetProductByIdQueryTests
{
    [Fact]
    public void Create_ShouldCorrectlyConstructQuery_WhenIdIsValid()
    {
        const long validId = 42;

        var createResult = GetProductByIdQuery.Create(validId);

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
        var createResult = GetProductByIdQuery.Create(invalidId);

        createResult.Should().BeFailure();
        var error = createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
        error.Id.Should().Be(invalidId);
    }
}
