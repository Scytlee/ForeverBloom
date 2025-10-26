using System.Linq.Expressions;
using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.Api.Extensions;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Models;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.ListProducts;

public sealed class ListProductsEndpointQueryProvider : IListProductsEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    private static readonly Dictionary<string, Expression<Func<Product, object>>> PropertyMapping =
        SortingHelper.CreatePropertyMapping<Product>(
            (nameof(PublicProductListItem.CategoryName), p => p.Category.Name)
        );

    public ListProductsEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<PublicProductListItem>> GetProductsAsync(ListProductsRequest request, SortCriterion[] sortColumns, CancellationToken cancellationToken = default)
    {
        var query = GetBaseQuery(request);

        // Custom handling for price sorting: place null prices last and sort nulls by display order
        if (sortColumns.Length == 1 && sortColumns[0].PropertyName.Equals(nameof(PublicProductListItem.Price), StringComparison.OrdinalIgnoreCase))
        {
            var asc = sortColumns[0].Direction.Equals("asc", StringComparison.OrdinalIgnoreCase);
            if (asc)
            {
                query = query
                    .OrderBy(p => p.Price == null)       // priced first (false < true)
                    .ThenBy(p => p.Price)                // ascending price
                    .ThenBy(p => p.DisplayOrder)         // within nulls, stable ordering
                    .ThenBy(p => p.Name);
            }
            else
            {
                query = query
                    .OrderBy(p => p.Price == null)       // priced first (false < true)
                    .ThenByDescending(p => p.Price)      // descending price
                    .ThenBy(p => p.DisplayOrder)
                    .ThenBy(p => p.Name);
            }
        }
        else
        {
            query = sortColumns.Any()
                ? query.ApplySort(sortColumns, propertyMapping: PropertyMapping)
                : query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name);
        }

        return query
            .Skip((request.PageNumber!.Value - 1) * request.PageSize!.Value)
            .Take(request.PageSize.Value)
            .Select(p => new PublicProductListItem
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.CurrentSlug,
                Price = p.Price,
                MetaDescription = p.MetaDescription,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Availability = p.Availability,
                IsFeatured = p.IsFeatured,
                PrimaryImagePath = p.Images
                    .Where(i => i.IsPrimary)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetProductsCountAsync(ListProductsRequest request, CancellationToken cancellationToken = default)
    {
        return GetBaseQuery(request).CountAsync(cancellationToken);
    }

    private IQueryable<Product> GetBaseQuery(ListProductsRequest request)
    {
        var query = _dbContext.Products.AsNoTracking();

        // Only show published items
        query = query.Where(p => p.PublishStatus == PublishStatus.Published);
        query = query.Where(p => p.Category.IsActive == true);

        // Ensure no ancestor categories are inactive - entire branch must be visible
        query = query.Where(p => !_dbContext.Categories.Any(ancestor =>
            p.Category.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive));

        // Search functionality
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                   (p.SeoTitle != null && p.SeoTitle.ToLower().Contains(searchTerm)) ||
                                   (p.FullDescription != null && p.FullDescription.ToLower().Contains(searchTerm)) ||
                                   (p.MetaDescription != null && p.MetaDescription.ToLower().Contains(searchTerm)));
        }

        // Category filtering
        if (request.CategoryId.HasValue)
        {
            if (request.IncludeSubcategories.HasValue && request.IncludeSubcategories.Value)
            {
                // Use ltree path matching to include subcategories - find products where category is descendant or equal to target category
                query = query.Where(p => _dbContext.Categories
                    .Where(c => c.Id == request.CategoryId.Value)
                    .Any(c => c.Path.IsAncestorOf(p.Category.Path) || c.Path == p.Category.Path));
            }
            else
            {
                // Direct category relationship only
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }
        }

        // Featured filtering
        if (request.Featured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.Featured.Value);
        }

        return query
            .Include(p => p.Category)
            .Include(p => p.Images);
    }
}
