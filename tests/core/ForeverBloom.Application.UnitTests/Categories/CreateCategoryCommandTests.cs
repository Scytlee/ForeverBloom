using FluentAssertions;
using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class CreateCategoryCommandTests
{
    private static Result<CreateCategoryCommand> CreateCommandWith(
        string name = "Test Category",
        string slug = "test-category",
        string? description = null,
        string? imagePath = null,
        string? imageAltText = null,
        long? parentCategoryId = null,
        int displayOrder = 0)
    {
        return CreateCategoryCommand.Create(name, slug, description,
            imagePath, imageAltText, parentCategoryId, displayOrder);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreProvided()
    {
        const string name = "Test Category";
        const string slug = "test-category";
        const string description = "Test description";
        const string imagePath = "/image/image.jpg";
        const string imageAltText = "Test image";
        const long parentCategoryId = 1;
        const int displayOrder = 10;

        var createResult = CreateCategoryCommand.Create(name, slug, description,
            imagePath, imageAltText, parentCategoryId, displayOrder);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Name.Should().HaveValue(name);
        command.Slug.Should().HaveValue(slug);
        command.Description.Should().HaveValue(description);
        command.Image.Should().Match(imagePath, imageAltText);
        command.ParentCategoryId.Should().Be(parentCategoryId);
        command.DisplayOrder.Should().Be(displayOrder);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndOptionalParametersAreOmitted()
    {
        const string name = "Test Category";
        const string slug = "test-category";

        var createResult = CreateCategoryCommand.Create(name, slug);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.Name.Should().HaveValue(name);
        command.Slug.Should().HaveValue(slug);
        command.Description.Should().BeNull();
        command.Image.Should().BeNull();
        command.ParentCategoryId.Should().BeNull();
        command.DisplayOrder.Should().Be(0);
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
    public void Create_ShouldReturnFailure_WhenSlugIsInvalid()
    {
        const string invalidSlug = "Obviously.Invalid_Slug";

        var createResult = CreateCommandWith(slug: invalidSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
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

        var createResult = CreateCommandWith(imagePath: invalidImagePath);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }

    [Fact]
    public void Create_ShouldIgnoreInvalidAltTextAndReturnSuccess_WhenImagePathIsNotProvided()
    {
        var invalidAltText = new string('a', Image.AltTextMaxLength + 1);

        var createResult = CreateCommandWith(
            imagePath: null,
            imageAltText: invalidAltText);

        createResult.Should().BeSuccess();
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
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenParentCategoryIdIsInvalid(long invalidParentCategoryId)
    {
        var createResult = CreateCommandWith(parentCategoryId: invalidParentCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.ParentCategoryIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const string invalidSlug = "Obviously.Invalid_Slug";
        const string invalidImagePath = "/image/image_with_no_extension";

        var createResult = CreateCommandWith(
            slug: invalidSlug,
            imagePath: invalidImagePath);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
        createResult.Should().HaveError<ImageErrors.InvalidExtension>();
    }
}
