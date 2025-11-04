using FluentValidation;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .MustBeValidCategoryId();
    }
}
