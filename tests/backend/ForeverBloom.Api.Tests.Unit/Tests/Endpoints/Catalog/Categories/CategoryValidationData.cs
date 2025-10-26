using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class CategoryValidationData
{
    public static TheoryData<string?, string?> NameValidationData = new()
    {
        { "Valid Category Name", null },
        { "A", null }, // Minimum valid length
        { new string('a', CategoryValidation.Constants.NameMaxLength), null }, // Maximum valid length
        { null, CategoryValidation.ErrorCodes.NameRequired },
        { "", CategoryValidation.ErrorCodes.NameRequired },
        { "   ", CategoryValidation.ErrorCodes.NameRequired },
        { new string('a', CategoryValidation.Constants.NameMaxLength + 1), CategoryValidation.ErrorCodes.NameTooLong }
    };

    public static TheoryData<string?, string?> DescriptionValidationData = new()
    {
        { null, null }, // Optional field
        { "", null }, // Empty is valid for optional field
        { "Valid description", null },
        { new string('a', CategoryValidation.Constants.DescriptionMaxLength), null }, // Maximum valid length
        { new string('a', CategoryValidation.Constants.DescriptionMaxLength + 1), CategoryValidation.ErrorCodes.DescriptionTooLong }
    };

    public static TheoryData<string?, string?> SlugValidationData = new()
    {
        { "valid-slug", null },
        { "valid-slug-123", null },
        { "123-valid-slug", null },
        { "a", null }, // Minimum valid length
        { new string('a', SlugValidation.Constants.MaxLength), null }, // Maximum valid length
        { null, CategoryValidation.ErrorCodes.SlugRequired },
        { "", CategoryValidation.ErrorCodes.SlugRequired },
        { "   ", CategoryValidation.ErrorCodes.SlugRequired },
        { new string('a', SlugValidation.Constants.MaxLength + 1), CategoryValidation.ErrorCodes.SlugTooLong },
        { "Invalid Slug With Spaces", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid_slug_with_underscores", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "InvalidSlugWithCapitals", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid-slug-with-special-chars!", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid.slug.with.dots", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "-invalid-slug-starting-with-dash", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid-slug-ending-with-dash-", CategoryValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid--double--dash--slug", CategoryValidation.ErrorCodes.SlugInvalidFormat }
    };

    public static TheoryData<string?, string?> ImagePathValidationData = new()
    {
        { null, null }, // Optional field
        { "", null }, // Empty is valid for optional field
        { "/valid-image.jpg", null },
        { "/no-extension-image", null },
        { "/valid-image.jpeg", null },
        { "/valid-image.png", null },
        { "/path/to/valid-image.jpg", null },
        { "relative/path/image.png", null },
        { new string('a', CategoryValidation.Constants.ImagePathMaxLength - 4) + ".jpg", null }, // Maximum valid length
        { new string('a', CategoryValidation.Constants.ImagePathMaxLength + 1), CategoryValidation.ErrorCodes.ImagePathTooLong }
    };

    public static TheoryData<int?, string?> ParentCategoryIdValidationData = new()
    {
        { null, null }, // Optional field
        { 1, null }, // Valid ID
        { 999, null }, // Valid ID
        { 0, CategoryValidation.ErrorCodes.ParentCategoryIdInvalid }, // Zero is invalid
        { -1, CategoryValidation.ErrorCodes.ParentCategoryIdInvalid }, // Negative is invalid
        { -999, CategoryValidation.ErrorCodes.ParentCategoryIdInvalid } // Negative is invalid
    };
}
