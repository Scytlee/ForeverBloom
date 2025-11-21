using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery : IQuery<GetCategoryByIdResult>
{
    public long Id { get; private init; }

    private GetCategoryByIdQuery() { }

    public static Result<GetCategoryByIdQuery> Create(long id)
    {
        var errors = new List<IError>();

        // Id (required)
        if (id <= 0)
        {
            errors.Add(new CategoryErrors.CategoryIdInvalid(id));
        }

        return Result<GetCategoryByIdQuery>.FromValidation(
            errors,
            () => new GetCategoryByIdQuery { Id = id });
    }
}
