using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery : IQuery<GetProductBySlugResult>
{
    public string Slug { get; private init; } = null!;

    private GetProductBySlugQuery() { }

    public static Result<GetProductBySlugQuery> Create(string slug)
    {
        var errors = new List<IError>();

        var slugResult = Domain.Shared.Slug.Create(slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        return Result<GetProductBySlugQuery>.FromValidation(
            errors,
            () => new GetProductBySlugQuery
            {
                Slug = slug
            });
    }
}
