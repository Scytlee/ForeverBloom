using System.Linq.Expressions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;
using ForeverBloom.Api.Extensions;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Models;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ListAdminProducts;

public sealed class ListAdminProductsEndpointQueryProvider : IListAdminProductsEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    private static readonly Dictionary<string, Expression<Func<Product, object>>> PropertyMapping =
        SortingHelper.CreatePropertyMapping<Product>(
            (nameof(ProductListItem.CategoryName), p => p.Category.Name)
        );

    public ListAdminProductsEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<ProductListItem>> GetProductsAsync(ListAdminProductsRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default)
    {
        var query = GetBaseQuery(request);

        query = sortColumns.Any()
            ? query.ApplySort(sortColumns, propertyMapping: PropertyMapping)
            : query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name);

        return query
            .Skip((request.PageNumber!.Value - 1) * request.PageSize!.Value)
            .Take(request.PageSize.Value)
            .Select(p => new ProductListItem
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.CurrentSlug,
                Price = p.Price,
                MetaDescription = p.MetaDescription,
                DisplayOrder = p.DisplayOrder,
                IsFeatured = p.IsFeatured,
                PublishStatus = p.PublishStatus,
                Availability = p.Availability,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CategoryIsActive = p.Category.IsActive,
                PrimaryImagePath = p.Images
                    .Where(i => i.IsPrimary)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .FirstOrDefault(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetProductsCountAsync(ListAdminProductsRequest request, CancellationToken cancellationToken = default)
    {
        return GetBaseQuery(request).CountAsync(cancellationToken);
    }

    private IQueryable<Product> GetBaseQuery(ListAdminProductsRequest request)
    {
        var query = _dbContext.Products.AsNoTracking();
        var includeArchived = request.IncludeArchived.HasValue && request.IncludeArchived.Value;

        if (includeArchived)
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            // Force an inner join with Categories so counting works correctly
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            query = query.Where(p => p.Category != null);
        }

        // Filter by publish status if specified (ProductActive maps to PublishStatus for backward compatibility)
        if (request.ProductActive.HasValue && request.ProductActive.Value)
        {
            query = query.Where(p => p.PublishStatus == PublishStatus.Published);
        }

        // Filter by category active status if specified
        if (request.CategoryActive.HasValue)
        {
            query = query.Where(p => p.Category.IsActive == request.CategoryActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                   (p.SeoTitle != null && p.SeoTitle.ToLower().Contains(searchTerm)) ||
                                   (p.FullDescription != null && p.FullDescription.ToLower().Contains(searchTerm)) ||
                                   (p.MetaDescription != null && p.MetaDescription.ToLower().Contains(searchTerm)));
        }

        if (request.CategoryId.HasValue)
        {
            if (request.IncludeSubcategories.HasValue && request.IncludeSubcategories.Value)
            {
                // Use ltree path matching to include subcategories - find products where category is descendant or equal
                var categoriesQuery = _dbContext.Categories.Where(c => c.Id == request.CategoryId.Value);
                if (includeArchived)
                {
                    categoriesQuery = categoriesQuery.IgnoreQueryFilters();
                }
                query = query.Where(p => categoriesQuery
                    .Any(c => p.Category.Path.IsDescendantOf(c.Path) || p.Category.Path == c.Path));
            }
            else
            {
                // Direct category relationship only
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price.HasValue && p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price.HasValue && p.Price <= request.MaxPrice.Value);
        }

        return query
            .Include(p => p.Category)
            .Include(p => p.Images);
    }
}
