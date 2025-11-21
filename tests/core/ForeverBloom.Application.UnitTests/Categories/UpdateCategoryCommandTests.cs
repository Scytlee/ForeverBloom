using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Optional;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class UpdateCategoryCommandTests
{
    private static Result<UpdateCategoryCommand> CreateCommandWith(
        long categoryId = 1,
        uint rowVersion = 1,
        Optional<string> name = default,
        Optional<string?> description = default,
        Optional<string?> imagePath = default,
        Optional<string?> imageAltText = default,
        Optional<int> displayOrder = default,
        Optional<string> publishStatus = default)
    {
        return UpdateCategoryCommand.Create(categoryId, rowVersion, name,
            description, imagePath, imageAltText, displayOrder, publishStatus);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreProvided()
    {
        const long categoryId = 1;
        const uint rowVersion = 1;
        const string name = "Test Category";
        const string description = "Test description";
        const string imagePath = "/image/image.jpg";
        const string imageAltText = "Test image";
        const int displayOrder = 10;
        const string publishStatus = "draft";

        var createResult = UpdateCategoryCommand.Create(categoryId, rowVersion, name,
            description, imagePath, imageAltText, displayOrder, publishStatus);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
        command.Name.Value.Should().HaveValue(name);
        command.Description.Value.Should().HaveValue(description);
        command.Image.Value.Should().Match(imagePath, imageAltText);
        command.DisplayOrder.Value.Should().Be(displayOrder);
        command.PublishStatus.Should().BeSetTo(PublishStatus.Draft);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreOmitted()
    {
        const long categoryId = 1;
        const uint rowVersion = 1;

        var createResult = UpdateCategoryCommand.Create(categoryId, rowVersion);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
        command.Name.Should().BeUnset();
        command.Description.Should().BeUnset();
        command.Image.Should().BeUnset();
        command.DisplayOrder.Should().BeUnset();
        command.PublishStatus.Should().BeUnset();
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreSetToNull()
    {
        const long categoryId = 1;
        const uint rowVersion = 1;

        var createResult = UpdateCategoryCommand.Create(categoryId, rowVersion,
            description: (string?)null,
            imagePath: (string?)null);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
        command.Description.Should().BeSetToNull();
        command.Image.Should().BeSetToNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenCategoryIdIsInvalid(long invalidCategoryId)
    {
        var createResult = CreateCommandWith(categoryId: invalidCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenRowVersionIsInvalid()
    {
        const uint invalidRowVersion = 0;

        var createResult = CreateCommandWith(rowVersion: invalidRowVersion);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenNameIsInvalid()
    {
        var invalidName = new string('a', SeoTitle.MaxLength + 1);

        var createResult = CreateCommandWith(name: invalidName);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SeoTitleErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenDescriptionIsInvalid()
    {
        var invalidDescription = new string('a', MetaDescription.MaxLength + 1);

        var createResult = CreateCommandWith(description: invalidDescription);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<MetaDescriptionErrors.TooLong>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImagePathIsInvalid()
    {
        const string invalidImagePath = "/image/image_with_no_extension";
        const string imageAltText = "Test image";

        var createResult = CreateCommandWith(
            imagePath: invalidImagePath,
            imageAltText: imageAltText);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImagePathIsProvidedButAltTextIsNot()
    {
        const string imagePath = "/image/image.jpg";

        var createResult = CreateCommandWith(imagePath: imagePath);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.CannotUpdatePartially>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImageAltTextIsProvidedButPathIsNot()
    {
        const string altText = "Some alt text";

        var createResult = CreateCommandWith(imageAltText: altText);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.CannotUpdatePartially>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenImagePathIsProvidedAndAltTextIsInvalid()
    {
        const string imagePath = "/image/image.jpg";
        var invalidAltText = new string('a', Image.AltTextMaxLength + 1);

        var createResult = CreateCommandWith(
            imagePath: imagePath,
            imageAltText: invalidAltText);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.AltTextTooLong>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("InvalidStatus")]
    public void Create_ShouldReturnFailure_WhenPublishStatusIsInvalid(string invalidPublishStatus)
    {
        var createResult = CreateCommandWith(publishStatus: invalidPublishStatus);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<PublishStatusErrors.InvalidName>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidCategoryId = 0;
        var invalidName = new string('a', SeoTitle.MaxLength + 1);

        var createResult = CreateCommandWith(
            categoryId: invalidCategoryId,
            name: invalidName);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        createResult.Should().HaveError<SeoTitleErrors.TooLong>();
    }
}
