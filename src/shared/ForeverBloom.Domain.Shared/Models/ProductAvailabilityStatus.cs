namespace ForeverBloom.Domain.Shared.Models;

public enum ProductAvailabilityStatus
{
    /// <summary>
    /// The product is available for immediate purchase. The "Add to Cart" button should be shown.
    /// </summary>
    Available = 1,

    /// <summary>
    /// The product is temporarily out of stock. It is visible, but cannot be purchased.
    /// </summary>
    OutOfStock = 2,

    /// <summary>
    /// The product is custom-made upon request. An "Inquire" or "Order Now" button should be shown.
    /// </summary>
    MadeToOrder = 3,

    /// <summary>
    /// The product requires a custom quote. The price is likely null. An "Inquire for Price" button should be shown.
    /// </summary>
    InquireForPrice = 4,

    /// <summary>
    /// The product has been discontinued and is no longer available.
    /// </summary>
    Discontinued = 5,

    /// <summary>
    /// The product is not yet released but is being previewed.
    /// </summary>
    ComingSoon = 6
}
