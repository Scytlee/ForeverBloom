using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed record ProductAvailabilityStatus
{
    /// <summary>
    /// The product is available for immediate purchase.
    /// </summary>
    public static readonly ProductAvailabilityStatus Available = new(1);

    /// <summary>
    /// The product is temporarily out of stock.
    /// </summary>
    public static readonly ProductAvailabilityStatus OutOfStock = new(2);

    /// <summary>
    /// The product is custom-made upon request.
    /// </summary>
    public static readonly ProductAvailabilityStatus MadeToOrder = new(3);

    /// <summary>
    /// The product has been discontinued and is no longer available.
    /// </summary>
    public static readonly ProductAvailabilityStatus Discontinued = new(5);

    /// <summary>
    /// The product is not yet released but is being previewed.
    /// </summary>
    public static readonly ProductAvailabilityStatus ComingSoon = new(6);

    private ProductAvailabilityStatus(int code) => Code = code;

    public int Code { get; init; }

    public static Result<ProductAvailabilityStatus> FromCode(int code)
    {
        return All.FirstOrDefault(s => s.Code == code) is { } status
            ? Result<ProductAvailabilityStatus>.Success(status)
            : Result<ProductAvailabilityStatus>.Failure(new ProductAvailabilityStatusErrors.InvalidCode(code));
    }

    public static readonly IReadOnlyCollection<ProductAvailabilityStatus> All =
    [
        Available,
        OutOfStock,
        MadeToOrder,
        Discontinued,
        ComingSoon
    ];
}

public static class ProductAvailabilityStatusErrors
{
    public sealed record InvalidCode(int AttemptedCode) : IError
    {
        public string Code => "ProductAvailabilityStatus.InvalidCode";
        public string Message => $"The product availability status code '{AttemptedCode}' is invalid";
        public int[] ValidCodes => ProductAvailabilityStatus.All.Select(s => s.Code).ToArray();
    }
}
