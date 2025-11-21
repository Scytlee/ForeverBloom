using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.RestoreProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class RestoreProductCommandTests
{
    private static Result<RestoreProductCommand> CreateCommandWith(
        long productId = 1,
        uint rowVersion = 1)
    {
        return RestoreProductCommand.Create(
            productId,
            rowVersion);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValid()
    {
        const long productId = 42;
        const uint rowVersion = 3;

        var createResult = RestoreProductCommand.Create(
            productId,
            rowVersion);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ProductId.Should().Be(productId);
        command.RowVersion.Should().Be(rowVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenProductIdIsInvalid(long invalidProductId)
    {
        var createResult = CreateCommandWith(productId: invalidProductId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenRowVersionIsInvalid()
    {
        var createResult = CreateCommandWith(rowVersion: 0);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidProductId = 0;
        const uint invalidRowVersion = 0;

        var createResult = CreateCommandWith(
            productId: invalidProductId,
            rowVersion: invalidRowVersion);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
    }
}
