using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ListAdminCategories;
using ForeverBloom.Api.Extensions;
using ForeverBloom.Api.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ListAdminCategories;

public sealed class ListAdminCategoriesEndpointQueryProvider : IListAdminCategoriesEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public ListAdminCategoriesEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<AdminCategoryListItem>> GetCategoriesAsync(ListAdminCategoriesRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default)
    {
        var query = GetBaseQuery(request);

        query = sortColumns.Any()
            ? query.ApplySort(sortColumns, propertyMapping: null)
            : query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

        return query
            .Skip((request.PageNumber!.Value - 1) * request.PageSize!.Value)
            .Take(request.PageSize.Value)
            .Select(c => new AdminCategoryListItem
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Slug = c.CurrentSlug,
                ImagePath = c.ImagePath,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                Path = c.Path.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                DeletedAt = c.DeletedAt,
                RowVersion = c.RowVersion
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCategoriesCountAsync(ListAdminCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        return GetBaseQuery(request).CountAsync(cancellationToken);
    }

    private IQueryable<Category> GetBaseQuery(ListAdminCategoriesRequest request)
    {
        var query = _dbContext.Categories.AsNoTracking();

        // Handle archived items
        if (request.IncludeArchived.HasValue && request.IncludeArchived.Value)
        {
            query = query.IgnoreQueryFilters();
        }

        if (request.Active.HasValue)
        {
            query = query.Where(c => c.IsActive == request.Active.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(c => c.Name.ToLower().Contains(request.SearchTerm.ToLower()));
        }

        if (request.ParentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == request.ParentCategoryId.Value);
        }

        return query;
    }
}
