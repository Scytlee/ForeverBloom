using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(long Id)
    : IQuery<GetProductByIdResult>;
