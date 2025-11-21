using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Products.Commands.UpdateProductImages;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Products;

public sealed class UpdateProductImagesCommandTests
{
    private static Result<UpdateProductImagesCommand> CreateCommandWith(
        long productId = 1,
        uint rowVersion = 1,
        IReadOnlyList<UpdateProductImagesCommand.CreateImageOperation>? imagesToCreate = null,
        IReadOnlyList<UpdateProductImagesCommand.UpdateImageOperation>? imagesToUpdate = null,
        IReadOnlyList<long>? imagesToDelete = null)
    {
        return UpdateProductImagesCommand.Create(
            productId,
            rowVersion,
            imagesToCreate,
            imagesToUpdate,
            imagesToDelete);
    }

    private static UpdateProductImagesCommand.CreateImageOperation CreateImage(
        string source = "/images/product-image.jpg",
        string? altText = "Product image",
        bool isPrimary = false,
        int displayOrder = 0)
    {
        return new UpdateProductImagesCommand.CreateImageOperation(source, altText, isPrimary, displayOrder);
    }

    private static UpdateProductImagesCommand.UpdateImageOperation UpdateImage(
        long id = 1,
        Optional<string?> altText = default,
        Optional<bool> isPrimary = default,
        Optional<int> displayOrder = default)
    {
        return new UpdateProductImagesCommand.UpdateImageOperation(id, altText, isPrimary, displayOrder);
    }

    [Fact]
    public void Create_ShouldConstructCommand_WhenInputIsValid()
    {
        var imagesToCreate =
            new[]
            {
                CreateImage("/images/primary.jpg", "Primary image", true, 1),
                CreateImage("/images/secondary.webp", "Secondary", false, 2)
            };
        var imagesToUpdate =
            new[]
            {
                UpdateImage(10, "Updated alt text", false, 3)
            };
        var imagesToDelete = new long[] { 20 };

        var createResult = CreateCommandWith(
            productId: 5,
            rowVersion: 2,
            imagesToCreate: imagesToCreate,
            imagesToUpdate: imagesToUpdate,
            imagesToDelete: imagesToDelete);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ProductId.Should().Be(5);
        command.RowVersion.Should().Be(2);
        command.ImagesToCreate.Should().BeEquivalentTo(imagesToCreate);
        command.ImagesToUpdate.Should().BeEquivalentTo(imagesToUpdate);
        command.ImagesToDelete.Should().BeEquivalentTo(imagesToDelete);
    }

    [Fact]
    public void Create_ShouldReturnSuccess_WhenOptionalCollectionsAreNotProvided()
    {
        var createResult = CreateCommandWith();

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.ImagesToCreate.Should().BeEmpty();
        command.ImagesToUpdate.Should().BeEmpty();
        command.ImagesToDelete.Should().BeEmpty();
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
    public void Create_ShouldReturnFailure_WhenImageToCreateIsInvalid()
    {
        var imagesToCreate = new[]
        {
            CreateImage("/images/invalid-path-without-extension", "Invalid")
        };

        var createResult = CreateCommandWith(imagesToCreate: imagesToCreate);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenUpdateImageIdIsInvalid()
    {
        var imagesToUpdate = new[]
        {
            UpdateImage(id: 0)
        };

        var createResult = CreateCommandWith(imagesToUpdate: imagesToUpdate);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ImageIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenUpdateImageAltTextIsTooLong()
    {
        var longAltText = new string('a', Image.AltTextMaxLength + 1);
        var imagesToUpdate = new[]
        {
            UpdateImage(id: 10, altText: longAltText)
        };

        var createResult = CreateCommandWith(imagesToUpdate: imagesToUpdate);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.AltTextTooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImagesToDeleteContainInvalidId()
    {
        var imagesToDelete = new long[] { -10 };

        var createResult = CreateCommandWith(imagesToDelete: imagesToDelete);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.ImageIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImageIdsAreDuplicatedAcrossOperations()
    {
        var imagesToUpdate = new[]
        {
            UpdateImage(id: 4, altText: "Alt")
        };
        var imagesToDelete = new long[] { 4 };

        var createResult = CreateCommandWith(
            imagesToUpdate: imagesToUpdate,
            imagesToDelete: imagesToDelete);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.DuplicateImageIds>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenTotalOperationsExceedMaximum()
    {
        var imagesToCreate = Enumerable.Range(1, Product.MaxImageCount)
            .Select(i => CreateImage($"/images/create-{i}.jpg", $"Alt {i}", i == 1, i))
            .ToArray();
        var imagesToUpdate = new[]
        {
            UpdateImage(id: 100 + Product.MaxImageCount)
        };

        var createResult = CreateCommandWith(
            imagesToCreate: imagesToCreate,
            imagesToUpdate: imagesToUpdate);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ProductErrors.TooManyImages>();
    }
}
