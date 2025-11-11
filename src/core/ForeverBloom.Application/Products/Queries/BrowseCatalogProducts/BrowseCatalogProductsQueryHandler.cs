using System.Text;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

internal sealed class BrowseCatalogProductsQueryHandler
    : IQueryHandler<BrowseCatalogProductsQuery, BrowseCatalogProductsResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BrowseCatalogProductsQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<BrowseCatalogProductsResult>> Handle(
        BrowseCatalogProductsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var (query, parameters) = BuildQuery(request);

        var items = await connection.QueryAsync<ProductListItemDtoWithCount>(
            new CommandDefinition(query, parameters, cancellationToken: cancellationToken));

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
                ImageSource = item.ImageSource,
                ImageAltText = item.ImageAltText,
                AvailabilityStatusCode = item.AvailabilityStatusCode,
                IsFeatured = item.IsFeatured
            }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<BrowseCatalogProductsResult>.Success(result);
    }

    private static (string Query, DynamicParameters Parameters) BuildQuery(BrowseCatalogProductsQuery request)
    {
        var parameters = new DynamicParameters();

        // Base query with mandatory public visibility rules and image selection
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
              p.created_at,
              pi.image_path AS image_source,
              pi.image_alt_text
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
            WHERE p.publish_status = 2
              AND p.deleted_at IS NULL
              AND NOT EXISTS (
                SELECT 1
                FROM categories ancestor
                WHERE ancestor.path @> c.path
                  AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
              )
            """;

        var productCteBuilder = new StringBuilder(baseQuery);

        // Category filter - filter only products whose category is a descendant of the provided category
        if (request.CategoryId.HasValue)
        {
            productCteBuilder.AppendLine();
            productCteBuilder.Append("""
                  AND EXISTS (
                    SELECT 1
                    FROM categories pc
                    WHERE pc.id = @CategoryId
                      AND pc.path @> c.path
                )
                """);
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        // Featured filter - filter only products with provided featured state
        if (request.Featured.HasValue)
        {
            productCteBuilder.AppendLine();
            productCteBuilder.Append("  AND p.is_featured = @Featured");
            parameters.Add("Featured", request.Featured.Value);
        }

        var productCte = productCteBuilder.ToString();
        var orderByClause = GetOrderByClause(request.SortStrategy);

        var query = $"""
            WITH filtered_products AS (
                {productCte}
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

    private static string GetOrderByClause(SortStrategy sortStrategy)
    {
        return sortStrategy.Id.ToLowerInvariant() switch
        {
            "relevance" => "ORDER BY is_featured DESC, created_at DESC, id",
            "name_asc" => "ORDER BY lower(name), created_at DESC, id",
            "name_desc" => "ORDER BY lower(name) DESC, created_at DESC, id",
            "price_asc" => "ORDER BY price NULLS LAST, lower(name), created_at DESC, id",
            "price_desc" => "ORDER BY price DESC NULLS LAST, lower(name), created_at DESC, id",
            _ => throw new ArgumentOutOfRangeException(nameof(sortStrategy), sortStrategy, null)
        };
    }

    private sealed class ProductListItemDtoWithCount
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal? Price { get; set; }
        public string? MetaDescription { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? ImageSource { get; set; }
        public string? ImageAltText { get; set; }
        public int AvailabilityStatusCode { get; set; }
        public bool IsFeatured { get; set; }
        public int TotalCount { get; set; }
    }
}
