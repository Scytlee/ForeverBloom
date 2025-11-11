using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

public sealed record GetCategoryBySlugQuery(string Slug)
    : IQuery<GetCategoryBySlugResult>;
