using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.GetCategoriesSitemapData;

internal sealed class GetCategoriesSitemapDataQueryHandler
    : IQueryHandler<GetCategoriesSitemapDataQuery, GetCategoriesSitemapDataResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCategoriesSitemapDataQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetCategoriesSitemapDataResult>> Handle(
        GetCategoriesSitemapDataQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                c.current_slug AS slug,
                c.updated_at
            FROM categories c
            WHERE c.publish_status = 2
              AND c.deleted_at IS NULL
              AND NOT EXISTS (
                SELECT 1
                FROM categories ancestor
                WHERE c.path <@ ancestor.path
                  AND ancestor.id != c.id
                  AND (ancestor.publish_status != 2 OR ancestor.deleted_at IS NOT NULL)
              )
            ORDER BY c.updated_at DESC
            """;

        var items = await connection.QueryAsync<CategorySitemapDataItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        var result = new GetCategoriesSitemapDataResult
        {
            Items = items.ToList()
        };

        return Result<GetCategoriesSitemapDataResult>.Success(result);
    }
}
