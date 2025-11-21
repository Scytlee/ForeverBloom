using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

public static class BrowseCatalogCategoryTreeErrors
{
    public sealed record LevelsOutOfRange([AttemptedValue] int Levels) : ApplicationError
    {
        public override string Code => "BrowseCatalogCategoryTree.LevelsOutOfRange";
        public override string Message => $"Levels must be greater than or equal to 0, but was {Levels}";
    }
}
