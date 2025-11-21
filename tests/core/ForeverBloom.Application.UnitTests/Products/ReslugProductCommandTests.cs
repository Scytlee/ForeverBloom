using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.ReslugProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class ReslugProductCommandTests
{
    private static Result<ReslugProductCommand> CreateCommandWith(
        long productId = 1,
        uint rowVersion = 1,
        string newSlug = "new-product-slug")
    {
        return ReslugProductCommand.Create(
            productId,
            rowVersion,
            newSlug);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValid()
    {
        const long productId = 42;
        const uint rowVersion = 3;
        const string newSlug = "updated-product-slug";

        var createResult = ReslugProductCommand.Create(
            productId,
            rowVersion,
            newSlug);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ProductId.Should().Be(productId);
        command.RowVersion.Should().Be(rowVersion);
        command.NewSlug.Should().HaveValue(newSlug);
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
    public void Create_ShouldReturnFailure_WhenNewSlugIsInvalid()
    {
        const string invalidSlug = "Invalid Slug";

        var createResult = CreateCommandWith(newSlug: invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidProductId = 0;
        const uint invalidRowVersion = 0;
        const string invalidSlug = "Invalid Slug";

        var createResult = CreateCommandWith(
            productId: invalidProductId,
            rowVersion: invalidRowVersion,
            newSlug: invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ProductIdInvalid>();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }
}
