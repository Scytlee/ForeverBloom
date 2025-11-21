using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Controls the visibility of a product on the public website.
/// </summary>
public sealed record PublishStatus
{
    /// <summary>
    /// The product is incomplete and not ready for publication.
    /// This is the default state for all new products.
    /// </summary>
    public static readonly PublishStatus Draft = new(1, "draft");

    /// <summary>
    /// The product is complete and visible on the public website.
    /// </summary>
    public static readonly PublishStatus Published = new(2, "published");

    /// <summary>
    /// The product is complete but hidden from the public website.
    /// </summary>
    public static readonly PublishStatus Hidden = new(3, "hidden");

    private PublishStatus(int code, string name)
    {
        Code = code;
        Name = name;
    }

    public int Code { get; init; }
    public string Name { get; init; }

    public static Result<PublishStatus> FromCode(int code)
    {
        return All.FirstOrDefault(s => s.Code == code) is { } status
            ? Result<PublishStatus>.Success(status)
            : Result<PublishStatus>.Failure(new PublishStatusErrors.InvalidCode(code));
    }

    public static Result<PublishStatus> FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<PublishStatus>.Failure(new PublishStatusErrors.InvalidName(name));
        }

        return All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } status
            ? Result<PublishStatus>.Success(status)
            : Result<PublishStatus>.Failure(new PublishStatusErrors.InvalidName(name));
    }

    public bool CanTransitionTo(PublishStatus target)
    {
        if (Equals(Draft))
        {
            return target.Equals(Published) || target.Equals(Hidden);
        }

        if (Equals(Published))
        {
            return target.Equals(Hidden);
        }

        if (Equals(Hidden))
        {
            return target.Equals(Published);
        }

        return false;
    }

    public static readonly IReadOnlyCollection<PublishStatus> All =
    [
        Draft,
        Published,
        Hidden
    ];
}

public static class PublishStatusErrors
{
    public sealed record InvalidCode([AttemptedValue] int Value) : DomainError
    {
        public override string Code => "PublishStatus.InvalidCode";
        public override string Message => $"The publish status code '{Value}' is invalid. Valid codes are: {string.Join(", ", ValidCodes)}.";
        public static readonly int[] ValidCodes = PublishStatus.All.Select(s => s.Code).ToArray();
    }

    public sealed record InvalidName([AttemptedValue] string Name) : DomainError
    {
        public override string Code => "PublishStatus.InvalidName";
        public override string Message => $"The publish status '{Name}' is invalid. Valid names are: {string.Join(", ", ValidNames)}.";
        public static readonly string[] ValidNames = PublishStatus.All.Select(s => s.Name).ToArray();
    }
}
