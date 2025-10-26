namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public interface IDeleteCategoryEndpointQueryProvider
{
    Task<CategoryDeletionValidationResult> ValidateCategoryForDeletionAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed record CategoryDeletionValidationResult
{
    public bool Exists { get; init; }
    public bool IsArchived { get; init; }
    public bool HasChildCategories { get; init; }
    public bool HasProducts { get; init; }
}
