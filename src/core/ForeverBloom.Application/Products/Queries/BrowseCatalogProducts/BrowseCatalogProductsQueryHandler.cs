using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

internal sealed class BrowseCatalogProductsQueryHandler
    : IQueryHandler<BrowseCatalogProductsQuery, PagedResult<BrowseCatalogProductsResultItem>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BrowseCatalogProductsQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<PagedResult<BrowseCatalogProductsResultItem>>> Handle(
        BrowseCatalogProductsQuery query,
        CancellationToken cancellationToken)
    {
        // TODO: Review this
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = BuildQuery(query, out var parameters);

        var items = await connection.QueryAsync<ProductListItemDtoWithCount>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var itemsList = items.ToList();
        var totalCount = itemsList.FirstOrDefault()?.TotalCount ?? 0;

        var result = new BrowseCatalogProductsResult
        {
            Items = itemsList.Select(item => new BrowseCatalogProductsResultItem
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                Price = item.Price,
                MetaDescription = item.MetaDescription,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                PrimaryImagePath = item.PrimaryImagePath,
                AvailabilityStatusCode = item.AvailabilityStatusCode,
                IsFeatured = item.IsFeatured
            }).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        return Result<PagedResult<BrowseCatalogProductsResultItem>>.Success(result);
    }

    private static string BuildQuery(BrowseCatalogProductsQuery query, out DynamicParameters parameters)
    {
        parameters = new DynamicParameters();
        var conditions = new List<string>();

        // Base query with mandatory public visibility rules
        const string baseQuery = """
            SELECT
                p.id,
                p.name,
                p.current_slug AS slug,
                p.price,
                p.meta_description,
                p.category_id,
                c.name AS category_name,
                p.availability AS availability_status_code,
                p.is_featured,
                p.display_order,
                (SELECT pi.image_path
                 FROM product_images pi
                 WHERE pi.product_id = p.id
                 ORDER BY pi.display_order
                 LIMIT 1) AS primary_image_path
            FROM products p
            INNER JOIN categories c ON p.category_id = c.id
            WHERE p.publish_status = 2
              AND p.deleted_at IS NULL
              AND NOT EXISTS (
                SELECT 1
                FROM categories ancestor
                WHERE ancestor.path @> c.path
                  AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
              )
            """;

        // Optional filters - add conditions based on active parameters

        if (query.CategoryId.HasValue)
        {
            conditions.Add("""
                EXISTS (
                    SELECT 1
                    FROM categories pc
                    WHERE pc.id = @CategoryId
                      AND (pc.path @> c.path OR pc.path = c.path)
                )
                """);
            parameters.Add("CategoryId", query.CategoryId.Value);
        }

        if (query.Featured.HasValue)
        {
            conditions.Add("p.is_featured = @Featured");
            parameters.Add("Featured", query.Featured.Value);
        }

        // Build complete WHERE clause
        var whereClause = baseQuery;
        if (conditions.Count > 0)
        {
            whereClause += " AND " + string.Join(" AND ", conditions);
        }

        // Build ORDER BY clause
        var orderByClause = BuildOrderByClause(query.SortBy);

        // Build final SQL with CTE and pagination
        var sql = $"""
            WITH filtered_products AS (
                {whereClause}
            )
            SELECT *, COUNT(*) OVER() AS total_count
            FROM filtered_products
            {orderByClause}
            LIMIT @PageSize OFFSET @Offset
            """;

        // Add pagination parameters
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", (query.PageNumber - 1) * query.PageSize);

        return sql;
    }

    private static string BuildOrderByClause(SortCriterion[]? sortCriteria)
    {
        if (sortCriteria is null || sortCriteria.Length == 0)
        {
            // Default sorting
            return "ORDER BY display_order, name";
        }

        var orderByParts = new List<string>();

        foreach (var criterion in sortCriteria)
        {
            var direction = criterion.Direction == SortDirection.Descending ? "DESC" : "ASC";

            var columnName = criterion.PropertyName.ToLowerInvariant() switch
            {
                "name" => "name",
                "price" => "price",
                _ => "name" // Fallback (shouldn't happen due to validation)
            };

            // Special handling for price sorting with NULLs
            if (columnName == "price")
            {
                // Place non-null prices first, then sort by display_order for nulls
                orderByParts.Add("price IS NULL");  // false (priced) comes first
                orderByParts.Add($"price {direction}");
                orderByParts.Add("display_order");
                orderByParts.Add("name");
            }
            else
            {
                orderByParts.Add($"{columnName} {direction}");
            }
        }

        return "ORDER BY " + string.Join(", ", orderByParts);
    }

    /// <summary>
    /// Extended DTO that includes the total count from the window function.
    /// </summary>
    private sealed class ProductListItemDtoWithCount
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal? Price { get; set; }
        public string? MetaDescription { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? PrimaryImagePath { get; set; }
        public int AvailabilityStatusCode { get; set; }
        public bool IsFeatured { get; set; }
        public int TotalCount { get; set; }
    }
}
