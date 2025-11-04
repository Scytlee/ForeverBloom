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
    public static readonly PublishStatus Draft = new(1);

    /// <summary>
    /// The product is complete and visible on the public website.
    /// </summary>
    public static readonly PublishStatus Published = new(2);

    /// <summary>
    /// The product is complete but hidden from the public website.
    /// Used for seasonal products, editing published products, or temporary unavailability.
    /// </summary>
    public static readonly PublishStatus Hidden = new(3);

    private PublishStatus(int code) => Code = code;

    public int Code { get; init; }

    public static Result<PublishStatus> FromCode(int code)
    {
        return All.FirstOrDefault(s => s.Code == code) is { } status
            ? Result<PublishStatus>.Success(status)
            : Result<PublishStatus>.Failure(new PublishStatusErrors.InvalidCode(code));
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
    public sealed record InvalidCode(int AttemptedCode) : IError
    {
        public string Code => "PublishStatus.InvalidCode";
        public string Message => $"The publish status code '{AttemptedCode}' is invalid";
    }
}
