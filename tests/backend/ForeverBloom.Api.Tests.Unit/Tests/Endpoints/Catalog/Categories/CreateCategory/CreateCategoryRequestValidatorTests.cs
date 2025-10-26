using FluentValidation.TestHelper;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Testing.Common.BaseTestClasses;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryRequestValidatorTests : TestClassBase
{
    private readonly CreateCategoryRequestValidator _sut;

    private static CreateCategoryRequest CreateValidRequest()
    {
        return new CreateCategoryRequest
        {
            Name = "Valid Category",
            Description = "Valid category description",
            Slug = "valid-category",
            ParentCategoryId = 1,
            DisplayOrder = 0,
            IsActive = true,
            ImagePath = "/valid-image.jpg"
        };
    }

    public CreateCategoryRequestValidatorTests()
    {
        _sut = new CreateCategoryRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOverallRequestIsValid()
    {
        var request = CreateValidRequest();

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(CategoryValidationData.NameValidationData), MemberType = typeof(CategoryValidationData))]
    public void Validate_ShouldCorrectlyValidateName(string? name, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Name = name! };

        var result = _sut.TestValidate(request);

        if (expectedErrorCode is null)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode(expectedErrorCode);
        }
    }

    [Theory]
    [MemberData(nameof(CategoryValidationData.DescriptionValidationData), MemberType = typeof(CategoryValidationData))]
    public void Validate_ShouldCorrectlyValidateDescription(string? description, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Description = description! };

        var result = _sut.TestValidate(request);

        if (expectedErrorCode == null)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorCode(expectedErrorCode);
        }
    }

    [Theory]
    [MemberData(nameof(CategoryValidationData.SlugValidationData), MemberType = typeof(CategoryValidationData))]
    public void Validate_ShouldCorrectlyValidateSlug(string? slug, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Slug = slug! };

        var result = _sut.TestValidate(request);

        if (expectedErrorCode == null)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Slug);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorCode(expectedErrorCode);
        }
    }

    [Theory]
    [MemberData(nameof(CategoryValidationData.ImagePathValidationData), MemberType = typeof(CategoryValidationData))]
    public void Validate_ShouldCorrectlyValidateImagePath(string? imagePath, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { ImagePath = imagePath! };

        var result = _sut.TestValidate(request);

        if (expectedErrorCode == null)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.ImagePath);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorCode(expectedErrorCode);
        }
    }

    [Theory]
    [MemberData(nameof(CategoryValidationData.ParentCategoryIdValidationData), MemberType = typeof(CategoryValidationData))]
    public void Validate_ShouldCorrectlyValidateParentCategoryId(int? parentCategoryId, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { ParentCategoryId = parentCategoryId };

        var result = _sut.TestValidate(request);

        if (expectedErrorCode == null)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.ParentCategoryId);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.ParentCategoryId)
                .WithErrorCode(expectedErrorCode);
        }
    }
}
