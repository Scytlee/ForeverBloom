using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal sealed class ProductImageFactory
{
    public static ProductImage Create(
        string imageSource = "/images/test.jpg",
        string? imageAltText = "Test image",
        bool isPrimary = false,
        int displayOrder = 1)
    {
        var image = ImageFactory.Create(imageSource, imageAltText);
        return ProductImage.Create(image, isPrimary, displayOrder);
    }
}
