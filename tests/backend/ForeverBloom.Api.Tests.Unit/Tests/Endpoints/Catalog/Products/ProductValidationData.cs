using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class ProductValidationData
{
    public static TheoryData<string?, string?> NameValidationData = new()
    {
        { "Valid Product Name", null },
        { "A", null }, // Minimum valid length
        { new string('a', ProductValidation.Constants.NameMaxLength), null }, // Maximum valid length
        { null, ProductValidation.ErrorCodes.NameRequired },
        { "", ProductValidation.ErrorCodes.NameRequired },
        { "   ", ProductValidation.ErrorCodes.NameRequired },
        { new string('a', ProductValidation.Constants.NameMaxLength + 1), ProductValidation.ErrorCodes.NameTooLong }
    };

    public static TheoryData<string?, string?> SeoTitleValidationData = new()
    {
        { null, null }, // Optional field
        { "", null }, // Empty is valid for optional field
        { "Valid SEO Title", null },
        { new string('a', ProductValidation.Constants.SeoTitleMaxLength), null }, // Maximum valid length
        { new string('a', ProductValidation.Constants.SeoTitleMaxLength + 1), ProductValidation.ErrorCodes.SeoTitleTooLong }
    };

    public static TheoryData<string?, string?> FullDescriptionValidationData = new()
    {
        { null, null }, // Optional field
        { "", null }, // Empty is valid for optional field
        { "Valid full description", null },
        { new string('a', ProductValidation.Constants.FullDescriptionMaxLength), null }, // Maximum valid length
        { new string('a', ProductValidation.Constants.FullDescriptionMaxLength + 1), ProductValidation.ErrorCodes.FullDescriptionTooLong }
    };

    public static TheoryData<string?, string?> MetaDescriptionValidationData = new()
    {
        { null, null }, // Optional field
        { "", null }, // Empty is valid for optional field
        { "Valid meta description", null },
        { new string('a', ProductValidation.Constants.MetaDescriptionMaxLength), null }, // Maximum valid length
        { new string('a', ProductValidation.Constants.MetaDescriptionMaxLength + 1), ProductValidation.ErrorCodes.MetaDescriptionTooLong }
    };

    public static TheoryData<string?, string?> SlugValidationData = new()
    {
        { "valid-slug", null },
        { "valid-slug-123", null },
        { "123-valid-slug", null },
        { "a", null }, // Minimum valid length
        { new string('a', SlugValidation.Constants.MaxLength), null }, // Maximum valid length
        { null, ProductValidation.ErrorCodes.SlugRequired },
        { "", ProductValidation.ErrorCodes.SlugRequired },
        { "   ", ProductValidation.ErrorCodes.SlugRequired },
        { new string('a', SlugValidation.Constants.MaxLength + 1), ProductValidation.ErrorCodes.SlugTooLong },
        { "Invalid Slug With Spaces", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid_slug_with_underscores", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "InvalidSlugWithCapitals", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid-slug-with-special-chars!", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid.slug.with.dots", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "-invalid-slug-starting-with-dash", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid-slug-ending-with-dash-", ProductValidation.ErrorCodes.SlugInvalidFormat },
        { "invalid--double--dash--slug", ProductValidation.ErrorCodes.SlugInvalidFormat }
    };

    public static TheoryData<decimal?, string?> PriceValidationData = new()
    {
        { null, null }, // Optional field
        { ProductValidation.Constants.MinPrice, null }, // Minimum valid price
        { ProductValidation.Constants.MaxPrice, null }, // Maximum valid price
        { 99.99m, null }, // Valid price
        { ProductValidation.Constants.MinPrice - 0.01m, ProductValidation.ErrorCodes.PriceOutOfRange },
        { ProductValidation.Constants.MaxPrice + 0.01m, ProductValidation.ErrorCodes.PriceOutOfRange },
        { 0m, ProductValidation.ErrorCodes.PriceOutOfRange },
        { -1m, ProductValidation.ErrorCodes.PriceOutOfRange }
    };

    public static TheoryData<int, string?> CategoryIdValidationData = new()
    {
        { 1, null }, // Valid ID
        { 999, null }, // Valid ID
        { int.MaxValue, null }, // Valid large ID
        { 0, ProductValidation.ErrorCodes.CategoryIdRequired }, // Zero is invalid
        { -1, ProductValidation.ErrorCodes.CategoryIdRequired }, // Negative is invalid
        { -999, ProductValidation.ErrorCodes.CategoryIdRequired } // Negative is invalid
    };

    public static TheoryData<PublishStatus, string?> PublishStatusValidationData = new()
    {
        { PublishStatus.Draft, null },
        { PublishStatus.Published, null },
        { (PublishStatus)999, ProductValidation.ErrorCodes.PublishStatusInvalid } // Invalid enum value
    };

    public static TheoryData<ProductAvailabilityStatus, string?> AvailabilityValidationData = new()
    {
        { ProductAvailabilityStatus.Available, null },
        { ProductAvailabilityStatus.OutOfStock, null },
        { ProductAvailabilityStatus.Discontinued, null },
        { (ProductAvailabilityStatus)999, ProductValidation.ErrorCodes.AvailabilityStatusInvalid } // Invalid enum value
    };
}
