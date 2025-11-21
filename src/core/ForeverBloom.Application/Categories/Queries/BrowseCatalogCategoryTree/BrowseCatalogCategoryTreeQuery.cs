using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

/// <summary>
/// Query to browse the catalog category tree.
/// </summary>
public sealed record BrowseCatalogCategoryTreeQuery : IQuery<BrowseCatalogCategoryTreeResult>
{
    public long? RootCategoryId { get; private init; }
    public int? Levels { get; private init; }

    private BrowseCatalogCategoryTreeQuery() { }

    public static Result<BrowseCatalogCategoryTreeQuery> Create(
        long? rootCategoryId = null,
        int? levels = null)
    {
        var errors = new List<IError>();

        // RootCategoryId (optional)
        if (rootCategoryId is <= 0)
        {
            errors.Add(new CategoryErrors.CategoryIdInvalid(rootCategoryId.Value));
        }

        // Levels (optional)
        if (levels is < 0)
        {
            errors.Add(new BrowseCatalogCategoryTreeErrors.LevelsOutOfRange(levels.Value));
        }

        return Result<BrowseCatalogCategoryTreeQuery>.FromValidation(
            errors,
            () => new BrowseCatalogCategoryTreeQuery
            {
                RootCategoryId = rootCategoryId,
                Levels = levels
            });
    }
}
