using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery : IQuery<GetProductByIdResult>
{
    public long Id { get; private init; }

    private GetProductByIdQuery() { }

    public static Result<GetProductByIdQuery> Create(long id)
    {
        var errors = new List<IError>();

        if (id <= 0)
        {
            errors.Add(new ProductErrors.ProductIdInvalid(id));
        }

        return Result<GetProductByIdQuery>.FromValidation(
            errors,
            () => new GetProductByIdQuery
            {
                Id = id
            });
    }
}
