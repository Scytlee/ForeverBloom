using System.Data;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProductByIdQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetProductByIdResult>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Fetch product by ID without any visibility filtering
        var product = await FetchProductAsync(connection, query.Id, cancellationToken);
        if (product is null)
        {
            return Result<GetProductByIdResult>.Failure(new ProductErrors.NotFoundById(query.Id));
        }

        // Fetch all product images
        product.Images = await FetchProductImagesAsync(connection, query.Id, cancellationToken);

        return Result<GetProductByIdResult>.Success(product);
    }

    private static async Task<GetProductByIdResult?> FetchProductAsync(
        IDbConnection connection,
        long productId,
        CancellationToken cancellationToken)
    {
        // Product must be visible, which means that:
        // - it cannot be soft-deleted
        // - its category cannot have a soft-deleted ancestor, including itself
        const string productSql = """
            SELECT
                p.id, p.name, p.seo_title, p.full_description, p.meta_description,
                p.current_slug AS slug, p.price, p.category_id, p.display_order,
                p.is_featured, p.publish_status AS publish_status_code,
                p.availability AS availability_status_code, p.created_at,
                p.updated_at, p.deleted_at, p.xmin AS row_version
            FROM products p
            JOIN categories c ON p.category_id = c.id
            WHERE p.deleted_at IS NULL
              AND p.id = @ProductId
              AND NOT EXISTS (
                  SELECT 1
                  FROM categories ancestor
                  WHERE ancestor.path @> c.path
                    AND ancestor.deleted_at IS NOT NULL
              )
            """;

        return await connection.QuerySingleOrDefaultAsync<GetProductByIdResult>(
            new CommandDefinition(productSql, new { ProductId = productId }, cancellationToken: cancellationToken));
    }

    private static async Task<IReadOnlyList<GetProductByIdImageItem>> FetchProductImagesAsync(
        IDbConnection connection,
        long productId,
        CancellationToken cancellationToken)
    {
        // This query assumes that the product is visible,
        // therefore this query does not check it again.
        const string imagesSql = """
            SELECT id, image_path, is_primary, display_order, image_alt_text AS alt_text
            FROM product_images
            WHERE product_id = @ProductId
            ORDER BY display_order
            """;

        var images = await connection.QueryAsync<GetProductByIdImageItem>(
            new CommandDefinition(imagesSql, new { ProductId = productId }, cancellationToken: cancellationToken));

        return images.ToList();
    }
}
