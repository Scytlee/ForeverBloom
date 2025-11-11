using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.ListProducts;

internal sealed class ListProductsQueryHandler
    : IQueryHandler<ListProductsQuery, ListProductsResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ListProductsQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<ListProductsResult>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var (query, parameters) = BuildQuery(request);

        var items = await connection.QueryAsync<ProductListItemWithCount>(
            new CommandDefinition(query, parameters, cancellationToken: cancellationToken));

        var itemsList = items.ToList();
        var totalCount = itemsList.FirstOrDefault()?.TotalCount ?? 0;

        var result = new ListProductsResult
        {
            Items = itemsList.Select(item => new ProductListItem
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                Price = item.Price,
                MetaDescription = item.MetaDescription,
                IsFeatured = item.IsFeatured,
                PublishStatus = item.PublishStatus,
                Availability = item.Availability,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                CategoryPublishStatus = item.CategoryPublishStatus,
                ImageSource = item.ImageSource,
                ImageAltText = item.ImageAltText,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                DeletedAt = item.DeletedAt
            }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<ListProductsResult>.Success(result);
    }

    private static (string Query, DynamicParameters Parameters) BuildQuery(ListProductsQuery request)
    {
        var parameters = new DynamicParameters();
        // Always exclude soft-deleted products by default
        var whereConditions = new List<string> { "p.deleted_at IS NULL" };

        // Search term filter using PostgreSQL ILIKE for case-insensitive search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            whereConditions.Add("""
                (
                    p.name ILIKE @SearchPattern
                    OR p.seo_title ILIKE @SearchPattern
                    OR p.full_description ILIKE @SearchPattern
                    OR p.meta_description ILIKE @SearchPattern
                )
                """);
            parameters.Add("SearchPattern", $"%{request.SearchTerm}%");
        }

        // Category filter with optional subcategory inclusion
        if (request.CategoryId.HasValue)
        {
            if (request.IncludeSubcategories is true)
            {
                // Use ltree path matching to include products whose category is a descendant or equal
                whereConditions.Add("""
                    EXISTS (
                        SELECT 1
                        FROM categories pc
                        WHERE pc.id = @CategoryId
                          AND pc.path @> c.path
                    )
                    """);
            }
            else
            {
                // Direct category relationship only
                whereConditions.Add("p.category_id = @CategoryId");
            }
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        var whereClause = whereConditions.Count > 0
            ? $"WHERE {string.Join("\n  AND ", whereConditions)}"
            : "";

        var orderByClause = BuildOrderByClause(request.SortBy);

        var query = $"""
            WITH filtered_products AS (
                SELECT
                    p.id,
                    p.name,
                    p.current_slug AS slug,
                    p.price,
                    p.meta_description,
                    p.is_featured,
                    p.publish_status,
                    p.availability,
                    p.category_id,
                    c.name AS category_name,
                    c.publish_status AS category_publish_status,
                    pi.image_path AS image_source,
                    pi.image_alt_text,
                    p.created_at,
                    p.updated_at,
                    p.deleted_at
                FROM products p
                INNER JOIN categories c ON p.category_id = c.id
                LEFT JOIN LATERAL (
                    SELECT pi.image_path, pi.image_alt_text
                    FROM product_images pi
                    WHERE pi.product_id = p.id
                    ORDER BY
                        pi.is_primary DESC,
                        pi.display_order
                    LIMIT 1
                ) pi ON true
                {whereClause}
            ),
            page AS (
                SELECT *
                FROM filtered_products
                {orderByClause}
                LIMIT @PageSize OFFSET @Offset
            ),
            total AS (
                SELECT COUNT(*) AS total_count
                FROM filtered_products
            )
            SELECT p.*, t.total_count
            FROM page AS p
            CROSS JOIN total AS t
            {orderByClause};
            """;

        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", (request.PageNumber - 1) * request.PageSize);

        return (query, parameters);
    }

    private static string BuildOrderByClause(SortProperty[]? sortBy)
    {
        if (sortBy is null || sortBy.Length == 0)
        {
            // Default sorting: created_at descending, then id for stable ordering
            return "ORDER BY created_at DESC, id";
        }

        var orderByParts = new List<string>();

        foreach (var sortProperty in sortBy)
        {
            var direction = sortProperty.Direction?.ToSqlKeyword() ?? "ASC";
            var column = sortProperty.Name.ToLowerInvariant() switch
            {
                "name" => $"lower(name) {direction}",
                "price" => $"price {direction} NULLS LAST",
                "created_at" => $"created_at {direction}",
                "updated_at" => $"updated_at {direction}",
                "category_name" => $"lower(category_name) {direction}",
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortProperty.Name, $"Unknown sort property: {sortProperty.Name}")
            };

            orderByParts.Add(column);
        }

        // Always add id as final tiebreaker for stable ordering
        orderByParts.Add("id");

        return $"ORDER BY {string.Join(", ", orderByParts)}";
    }

    private sealed class ProductListItemWithCount
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal? Price { get; set; }
        public string? MetaDescription { get; set; }
        public bool IsFeatured { get; set; }
        public int PublishStatus { get; set; }
        public int Availability { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int CategoryPublishStatus { get; set; }
        public string? ImageSource { get; set; }
        public string? ImageAltText { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int TotalCount { get; set; }
    }
}
