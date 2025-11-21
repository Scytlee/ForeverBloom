using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class ProductFactory
{
    public static Product Create(
        DateTimeOffset timestamp,
        string name = "Test Product",
        string slug = "test-product",
        long categoryId = 1,
        string? seoTitle = null,
        string? fullDescription = null,
        string? metaDescription = null,
        decimal? price = null,
        bool isFeatured = false,
        ProductAvailabilityStatus? availability = null,
        ICollection<ProductImage>? images = null,
        PublishStatus? publishStatus = null)
    {
        var productResult = Product.Create(
            ProductNameFactory.Create(name),
            SlugFactory.Create(slug),
            categoryId,
            timestamp,
            seoTitle is null ? null : SeoTitleFactory.Create(seoTitle),
            fullDescription is null ? null : HtmlFragmentFactory.Create(fullDescription),
            metaDescription is null ? null : MetaDescriptionFactory.Create(metaDescription),
            price is null ? null : MoneyFactory.Create(price.Value),
            isFeatured,
            availability,
            images);

        productResult.Should().BeSuccess();
        var product = productResult.Value!;

        if (publishStatus is not null && publishStatus != PublishStatus.Draft)
        {
            var updateResult = product.Update(
                timestamp,
                publishStatus: publishStatus);
            updateResult.Should().BeSuccess();
        }

        return product;
    }
}
