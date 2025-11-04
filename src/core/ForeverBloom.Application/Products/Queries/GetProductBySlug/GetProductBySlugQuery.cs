using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug)
    : IQuery<GetProductBySlugResult>;
