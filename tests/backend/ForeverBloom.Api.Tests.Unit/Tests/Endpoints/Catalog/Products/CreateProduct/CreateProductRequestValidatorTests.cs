using FluentValidation.TestHelper;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.BaseTests;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.CreateProduct;

public sealed class CreateProductRequestValidatorTests : TestClassBase
{
    private readonly CreateProductRequestValidator _sut;

    private static CreateProductRequest CreateValidRequest()
    {
        return new CreateProductRequest
        {
            Name = "Valid Product",
            SeoTitle = "Valid SEO Title",
            FullDescription = "Valid full description of the product",
            MetaDescription = "Valid meta description",
            Slug = "valid-product-slug",
            Price = 99.99m,
            DisplayOrder = 1,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.Available,
            CategoryId = 1
        };
    }

    public CreateProductRequestValidatorTests()
    {
        _sut = new CreateProductRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOverallRequestIsValid()
    {
        var request = CreateValidRequest();

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.NameValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateName(string? name, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Name = name! };

        BaseTests.BaseValidationTest(_sut, request, r => r.Name, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.SeoTitleValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateSeoTitle(string? seoTitle, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { SeoTitle = seoTitle };

        BaseTests.BaseValidationTest(_sut, request, r => r.SeoTitle, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.FullDescriptionValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateFullDescription(string? fullDescription, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { FullDescription = fullDescription };

        BaseTests.BaseValidationTest(_sut, request, r => r.FullDescription, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.MetaDescriptionValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateMetaDescription(string? metaDescription, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { MetaDescription = metaDescription };

        BaseTests.BaseValidationTest(_sut, request, r => r.MetaDescription, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.SlugValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateSlug(string? slug, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Slug = slug! };

        BaseTests.BaseValidationTest(_sut, request, r => r.Slug, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.PriceValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidatePrice(decimal? price, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Price = price };

        BaseTests.BaseValidationTest(_sut, request, r => r.Price, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.PublishStatusValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidatePublishStatus(PublishStatus publishStatus, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { PublishStatus = publishStatus };

        BaseTests.BaseValidationTest(_sut, request, r => r.PublishStatus, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.AvailabilityValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateAvailability(ProductAvailabilityStatus availability, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { Availability = availability };

        BaseTests.BaseValidationTest(_sut, request, r => r.Availability, expectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(ProductValidationData.CategoryIdValidationData), MemberType = typeof(ProductValidationData))]
    public void Validate_ShouldCorrectlyValidateCategoryId(int categoryId, string? expectedErrorCode)
    {
        var request = CreateValidRequest() with { CategoryId = categoryId };

        BaseTests.BaseValidationTest(_sut, request, r => r.CategoryId, expectedErrorCode);
    }
}
