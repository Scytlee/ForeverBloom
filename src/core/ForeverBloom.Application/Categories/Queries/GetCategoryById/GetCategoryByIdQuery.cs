using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(long Id)
    : IQuery<GetCategoryByIdResult>;
