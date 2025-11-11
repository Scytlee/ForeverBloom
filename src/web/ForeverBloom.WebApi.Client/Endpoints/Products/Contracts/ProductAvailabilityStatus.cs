namespace ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

/// <summary>
/// Represents the availability status of a product.
/// </summary>
public enum ProductAvailabilityStatus
{
    /// <summary>
    /// Product is currently available for purchase.
    /// </summary>
    Available = 1,

    /// <summary>
    /// Product is temporarily out of stock.
    /// </summary>
    OutOfStock = 2,

    /// <summary>
    /// Product is made to order.
    /// </summary>
    MadeToOrder = 3,

    /// <summary>
    /// Product has been discontinued.
    /// </summary>
    Discontinued = 5,

    /// <summary>
    /// Product is coming soon.
    /// </summary>
    ComingSoon = 6
}
