using ForeverBloom.Api.Contracts.Common;

namespace ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;

public sealed record UpdateCategoryRequest
{
    public Optional<string> Name { get; init; }
    public Optional<string?> Description { get; init; }
    public Optional<string> Slug { get; init; }
    public Optional<string?> ImagePath { get; init; }
    public Optional<int?> ParentCategoryId { get; init; }
    public Optional<int> DisplayOrder { get; init; }
    public Optional<bool> IsActive { get; init; }
    public uint RowVersion { get; init; }
}
