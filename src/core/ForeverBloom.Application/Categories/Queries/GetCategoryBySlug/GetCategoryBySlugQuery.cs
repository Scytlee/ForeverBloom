using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

public sealed record GetCategoryBySlugQuery : IQuery<GetCategoryBySlugResult>
{
    public Slug Slug { get; private init; } = null!;

    private GetCategoryBySlugQuery() { }

    public static Result<GetCategoryBySlugQuery> Create(string slug)
    {
        var errors = new List<IError>();

        // Slug (required)
        var slugResult = Slug.Create(slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        return Result<GetCategoryBySlugQuery>.FromValidation(
            errors,
            () => new GetCategoryBySlugQuery { Slug = slugResult.Value! });
    }
}
