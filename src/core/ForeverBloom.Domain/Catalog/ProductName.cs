using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Value object representing a product name.
/// </summary>
public sealed record ProductName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private ProductName(string value) => Value = value;

    /// <summary>
    /// Creates a new ProductName with validation.
    /// </summary>
    public static Result<ProductName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ProductName>.Failure(new ProductNameErrors.Required());
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Result<ProductName>.Failure(new ProductNameErrors.TooLong(trimmed));
        }

        return Result<ProductName>.Success(new ProductName(trimmed));
    }

    public override string ToString() => Value;
}

public static class ProductNameErrors
{
    public sealed record Required : IError
    {
        public string Code => "ProductName.Required";
        public string Message => "Product name is required";
    }

    public sealed record TooLong(string AttemptedValue) : IError
    {
        public string Code => "ProductName.TooLong";
        public string Message => $"Product name cannot exceed {MaxLength} characters";
        public int MaxLength => ProductName.MaxLength;
    }
}
