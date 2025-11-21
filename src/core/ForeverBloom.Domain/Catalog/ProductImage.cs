using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed class ProductImage
{
    public long Id { get; private set; }
    public Image Image { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }

    // Navigation
    public Product Product { get; private set; } = null!;

    private ProductImage() { }

    private ProductImage(
        Image image,
        bool isPrimary = false,
        int displayOrder = 0)
    {
        Image = image;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Creates a new ProductImage.
    /// </summary>
    public static ProductImage Create(
        Image image,
        bool isPrimary = false,
        int displayOrder = 0)
    {
        return new ProductImage(image, isPrimary, displayOrder);
    }

    /// <summary>
    /// Updates mutable properties of a product image.
    /// </summary>
    public Result Update(
        Optional<string?> altText = default,
        Optional<bool> isPrimary = default,
        Optional<int> displayOrder = default)
    {
        if (altText.IsSet)
        {
            var imageResult = Image.Create(Image.Source.Value, altText.Value);
            if (imageResult.IsFailure)
            {
                return Result.Failure(imageResult.Error);
            }

            Image = imageResult.Value;
        }

        if (isPrimary.IsSet)
        {
            IsPrimary = isPrimary.Value;
        }

        if (displayOrder.IsSet)
        {
            DisplayOrder = displayOrder.Value;
        }

        return Result.Success();
    }
}
