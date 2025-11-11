using System.Text;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

internal sealed class BrowseCatalogCategoryTreeQueryHandler
    : IQueryHandler<BrowseCatalogCategoryTreeQuery, BrowseCatalogCategoryTreeResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BrowseCatalogCategoryTreeQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<BrowseCatalogCategoryTreeResult>> Handle(
        BrowseCatalogCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var (sql, parameters) = BuildQuery(request);

        var categories = await connection.QueryAsync<CategoryRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var categoryList = categories.ToList();
        var lookup = categoryList.ToLookup(category => category.ParentCategoryId);

        var startingCategories = request.RootCategoryId.HasValue
            ? categoryList.Where(category => category.Id == request.RootCategoryId.Value)
            : lookup[null];

        var result = new BrowseCatalogCategoryTreeResult
        {
            Categories = BuildTree(startingCategories, lookup)
        };

        return Result<BrowseCatalogCategoryTreeResult>.Success(result);
    }

    private static IReadOnlyList<BrowseCatalogCategoryTreeResultItem> BuildTree(
        IEnumerable<CategoryRow> categories,
        ILookup<long?, CategoryRow> lookup)
    {
        return categories
            .Select(category => new BrowseCatalogCategoryTreeResultItem
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ImageSource = category.ImageSource,
                ImageAltText = category.ImageAltText,
                Children = BuildTree(lookup[category.Id], lookup)
            })
            .ToList();
    }

    private static (string Sql, DynamicParameters Parameters) BuildQuery(BrowseCatalogCategoryTreeQuery request)
    {
        var parameters = new DynamicParameters();
        var sqlBuilder = new StringBuilder();

        if (request.RootCategoryId.HasValue)
        {
            sqlBuilder.AppendLine("""
                WITH root_categories AS (
                    SELECT c.id, c.path, nlevel(c.path) AS root_level
                    FROM categories c
                    WHERE c.id = @RootCategoryId
                      AND c.publish_status = 2
                      AND c.deleted_at IS NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM categories ancestor
                          WHERE ancestor.path @> c.path
                            AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
                      )
                )
                """);

            parameters.Add("RootCategoryId", request.RootCategoryId.Value);
        }

        sqlBuilder.AppendLine("""
            SELECT
                c.id,
                c.name,
                c.current_slug AS slug,
                c.image_path AS image_source,
                c.image_alt_text,
                c.parent_category_id,
                c.display_order
            FROM categories c
            """);

        if (request.RootCategoryId.HasValue)
        {
            sqlBuilder.AppendLine("CROSS JOIN root_categories root");
        }

        sqlBuilder.AppendLine("""
            WHERE c.publish_status = 2
              AND c.deleted_at IS NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM categories ancestor
                  WHERE ancestor.path @> c.path
                    AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
              )
            """);

        if (request.RootCategoryId.HasValue)
        {
            sqlBuilder.AppendLine("  AND root.path @> c.path");

            if (request.Depth.HasValue)
            {
                sqlBuilder.AppendLine("  AND nlevel(c.path) <= root.root_level + @Depth");
                parameters.Add("Depth", request.Depth.Value);
            }
        }
        else if (request.Depth.HasValue)
        {
            sqlBuilder.AppendLine("  AND nlevel(c.path) <= @Depth");
            parameters.Add("Depth", request.Depth.Value);
        }

        sqlBuilder.AppendLine("ORDER BY c.path, c.display_order;");

        return (sqlBuilder.ToString(), parameters);
    }

    private sealed class CategoryRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? ImageSource { get; set; }
        public string? ImageAltText { get; set; }
        public long? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
