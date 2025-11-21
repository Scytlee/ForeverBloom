using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed record ProductAvailabilityStatus
{
    /// <summary>
    /// The product is available for immediate purchase.
    /// </summary>
    public static readonly ProductAvailabilityStatus Available = new(1, "available");

    /// <summary>
    /// The product is temporarily out of stock.
    /// </summary>
    public static readonly ProductAvailabilityStatus OutOfStock = new(2, "out_of_stock");

    /// <summary>
    /// The product is custom-made upon request.
    /// </summary>
    public static readonly ProductAvailabilityStatus MadeToOrder = new(3, "made_to_order");

    /// <summary>
    /// The product has been discontinued and is no longer available.
    /// </summary>
    public static readonly ProductAvailabilityStatus Discontinued = new(5, "discontinued");

    /// <summary>
    /// The product is not yet released but is being previewed.
    /// </summary>
    public static readonly ProductAvailabilityStatus ComingSoon = new(6, "coming_soon");

    private ProductAvailabilityStatus(int code, string name)
    {
        Code = code;
        Name = name;
    }

    public int Code { get; init; }
    public string Name { get; init; }

    public static Result<ProductAvailabilityStatus> FromCode(int code)
    {
        return All.FirstOrDefault(s => s.Code == code) is { } status
            ? Result<ProductAvailabilityStatus>.Success(status)
            : Result<ProductAvailabilityStatus>.Failure(new ProductAvailabilityStatusErrors.InvalidCode(code));
    }

    public static Result<ProductAvailabilityStatus> FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ProductAvailabilityStatus>.Failure(new ProductAvailabilityStatusErrors.InvalidName(name));
        }

        return All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } status
            ? Result<ProductAvailabilityStatus>.Success(status)
            : Result<ProductAvailabilityStatus>.Failure(new ProductAvailabilityStatusErrors.InvalidName(name));
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
    public sealed record InvalidCode([AttemptedValue] int Value) : DomainError
    {
        public override string Code => "ProductAvailabilityStatus.InvalidCode";
        public override string Message => $"The product availability status code '{Value}' is invalid. Valid codes are: {string.Join(", ", ValidCodes)}.";
        public static readonly int[] ValidCodes = ProductAvailabilityStatus.All.Select(s => s.Code).ToArray();
    }

    public sealed record InvalidName([AttemptedValue] string Name) : DomainError
    {
        public override string Code => "ProductAvailabilityStatus.InvalidName";
        public override string Message => $"The product availability status '{Name}' is invalid. Valid names are: {string.Join(", ", ValidNames)}.";
        public static readonly string[] ValidNames = ProductAvailabilityStatus.All.Select(s => s.Name).ToArray();
    }
}
