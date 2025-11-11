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
        bool isFeatured = false,
        ProductAvailabilityStatus? availability = null,
        string? seoTitle = null,
        string? fullDescription = null,
        string? metaDescription = null,
        decimal? price = null,
        ICollection<ProductImage>? images = null,
        PublishStatus? publishStatus = null)
    {
        var productResult = Product.Create(
            ProductNameFactory.Create(name),
            seoTitle is null ? null : SeoTitleFactory.Create(seoTitle),
            fullDescription is null ? null : HtmlFragmentFactory.Create(fullDescription),
            metaDescription is null ? null : MetaDescriptionFactory.Create(metaDescription),
            SlugFactory.Create(slug),
            categoryId,
            price is null ? null : MoneyFactory.Create(price.Value),
            isFeatured,
            availability ?? ProductAvailabilityStatus.ComingSoon,
            timestamp,
            images);

        productResult.Should().BeSuccess();
        var product = productResult.Value!;

        if (publishStatus is not null && publishStatus != PublishStatus.Draft)
        {
            var updateResult = product.Update(
                name: default,
                seoTitle: default,
                fullDescription: default,
                metaDescription: default,
                categoryId: default,
                price: default,
                isFeatured: default,
                availability: default,
                publishStatus: publishStatus,
                timestamp: timestamp);
            updateResult.Should().BeSuccess();
        }

        return product;
    }
}
