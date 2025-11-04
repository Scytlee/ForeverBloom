using System.Data;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler
    : IQueryHandler<GetCategoryByIdQuery, GetCategoryByIdResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCategoryByIdQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetCategoryByIdResult>> Handle(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Fetch category by ID without any filtering
        var category = await FetchCategoryAsync(connection, query.Id, cancellationToken);
        if (category is null)
        {
            return Result<GetCategoryByIdResult>.Failure(new CategoryErrors.NotFoundById(query.Id));
        }

        return Result<GetCategoryByIdResult>.Success(category);
    }

    private static async Task<GetCategoryByIdResult?> FetchCategoryAsync(
        IDbConnection connection,
        long categoryId,
        CancellationToken cancellationToken)
    {
        // Category must be visible, which means that:
        // - it cannot be soft-deleted
        // - it cannot have a soft-deleted ancestor, including itself
        const string categorySql = """
            SELECT
                c.id, c.name, c.description, c.current_slug AS slug,
                c.image_path, c.image_alt_text, c.parent_category_id,
                c.display_order, c.publish_status AS publish_status_code,
                c.path, c.created_at, c.updated_at, c.deleted_at,
                c.xmin AS row_version
            FROM categories c
            WHERE c.id = @CategoryId
              AND c.deleted_at IS NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM categories ancestor
                  WHERE ancestor.path @> c.path
                    AND ancestor.deleted_at IS NOT NULL
              )
            """;

        return await connection.QuerySingleOrDefaultAsync<GetCategoryByIdResult>(
            new CommandDefinition(categorySql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
    }
}
