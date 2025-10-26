namespace ForeverBloom.Domain.Shared.Models;

/// <summary>
/// Controls the visibility of a product on the public website.
/// </summary>
public enum PublishStatus
{
    /// <summary>
    /// The product is a work-in-progress and is not visible on the public website.
    /// This is the default state for all new products.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// The product is complete and is visible on the public website, provided it is not archived.
    /// </summary>
    Published = 2,

    /// <summary>
    /// The product was previously published but is now temporarily hidden from the public website.
    /// </summary>
    Unpublished = 3
}
