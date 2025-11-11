using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

public sealed class GetCategoryBySlugQueryValidator : AbstractValidator<GetCategoryBySlugQuery>
{
    public GetCategoryBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeValidSlug();
    }
}
