using System.Data;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler
    : IQueryHandler<GetProductBySlugQuery, GetProductBySlugResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProductBySlugQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetProductBySlugResult>> Handle(
        GetProductBySlugQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Slug must exist in the registry
        var slugLookup = await LookupSlugAsync(connection, query.Slug, cancellationToken);
        if (slugLookup is null)
        {
            return Result<GetProductBySlugResult>.Failure(new ProductErrors.NotFoundBySlug(query.Slug));
        }

        // Product must be accessible - not deleted, published, category active, etc.
        var product = await FetchProductAsync(connection, slugLookup.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<GetProductBySlugResult>.Failure(new ProductErrors.NotFoundBySlug(query.Slug));
        }

        // Provided slug must be the current slug of the entity
        if (!slugLookup.IsCurrent)
        {
            return Result<GetProductBySlugResult>.Failure(new ProductErrors.SlugChanged(query.Slug, slugLookup.CurrentSlug));
        }

        // Product is accessible - fetch images and return full result
        product.Images = await FetchProductImagesAsync(connection, slugLookup.ProductId, cancellationToken);
        return Result<GetProductBySlugResult>.Success(product);
    }

    private static async Task<SlugLookupResult?> LookupSlugAsync(
        IDbConnection connection,
        string slug,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entity_id, is_active
            FROM slug_registry
            WHERE slug = @Slug AND entity_type = 1 -- Product
            """;

        var slugEntry = await connection.QuerySingleOrDefaultAsync<SlugRegistryRow>(
            new CommandDefinition(sql, new { Slug = slug }, cancellationToken: cancellationToken));
        if (slugEntry is null)
        {
            return null;
        }

        // If the provided slug is active, we already have the current slug
        if (slugEntry.IsActive)
        {
            return new SlugLookupResult
            {
                ProductId = slugEntry.EntityId,
                CurrentSlug = slug,
                IsCurrent = true
            };
        }

        // If the provided slug is inactive, find the current active slug for this product
        const string activeSlugSql = """
            SELECT slug
            FROM slug_registry
            WHERE entity_id = @EntityId
              AND entity_type = 1 -- Product
              AND is_active = true
            """;

        var currentSlug = await connection.QuerySingleAsync<string>(
            new CommandDefinition(activeSlugSql, new { slugEntry.EntityId }, cancellationToken: cancellationToken));

        return new SlugLookupResult
        {
            ProductId = slugEntry.EntityId,
            CurrentSlug = currentSlug,
            IsCurrent = false
        };
    }

    private static async Task<GetProductBySlugResult?> FetchProductAsync(
        IDbConnection connection,
        long productId,
        CancellationToken cancellationToken)
    {
        // Product must be publicly visible, which means that:
        // - it cannot be soft-deleted
        // - it has to be published
        // - its category cannot have a soft-deleted ancestor, including itself
        // - all of its category's ancestors must be published, including itself
        const string productSql = """
            SELECT
                p.id, p.name, p.seo_title, p.full_description, p.meta_description,
                p.current_slug AS slug, p.price, p.category_id, p.availability AS availability_status_code, p.is_featured,
                c.name AS category_name
            FROM products p
            INNER JOIN categories c ON p.category_id = c.id
            WHERE p.id = @ProductId
              AND p.publish_status = 2
              AND p.deleted_at IS NULL
              AND NOT EXISTS (
                SELECT 1
                FROM categories ancestor
                WHERE ancestor.path @> c.path
                  AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
              )
            """;

        return await connection.QuerySingleOrDefaultAsync<GetProductBySlugResult>(
            new CommandDefinition(productSql, new { ProductId = productId }, cancellationToken: cancellationToken));
    }

    private static async Task<IReadOnlyList<GetProductBySlugImageItem>> FetchProductImagesAsync(
        IDbConnection connection,
        long productId,
        CancellationToken cancellationToken)
    {
        // This query assumes that the product is publicly visible,
        // therefore this query does not check it again.
        const string imagesSql = """
            SELECT image_path, is_primary, display_order, image_alt_text AS alt_text
            FROM product_images
            WHERE product_id = @ProductId
            ORDER BY display_order
            """;

        var images = await connection.QueryAsync<GetProductBySlugImageItem>(
            new CommandDefinition(imagesSql, new { ProductId = productId }, cancellationToken: cancellationToken));

        return images.ToList();
    }

    private sealed class SlugRegistryRow
    {
        public long EntityId { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class SlugLookupResult
    {
        public long ProductId { get; set; }
        public string CurrentSlug { get; set; } = null!;
        public bool IsCurrent { get; set; }
    }
}
