using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.GetProductsSitemapData;

internal sealed class GetProductsSitemapDataQueryHandler
    : IQueryHandler<GetProductsSitemapDataQuery, GetProductsSitemapDataResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProductsSitemapDataQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetProductsSitemapDataResult>> Handle(
        GetProductsSitemapDataQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                p.current_slug AS slug,
                p.updated_at
            FROM products p
            INNER JOIN categories c ON p.category_id = c.id
            WHERE p.publish_status = 2
              AND p.deleted_at IS NULL
              AND c.publish_status = 2
              AND c.deleted_at IS NULL
              AND NOT EXISTS (
                SELECT 1
                FROM categories ancestor
                WHERE c.path <@ ancestor.path
                  AND ancestor.id != c.id
                  AND (ancestor.publish_status != 2 OR ancestor.deleted_at IS NOT NULL)
              )
            ORDER BY p.updated_at DESC
            """;

        var items = await connection.QueryAsync<ProductSitemapDataItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        var result = new GetProductsSitemapDataResult
        {
            Items = items.ToList()
        };

        return Result<GetProductsSitemapDataResult>.Success(result);
    }
}
