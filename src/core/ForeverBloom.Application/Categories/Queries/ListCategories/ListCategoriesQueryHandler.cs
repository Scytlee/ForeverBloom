using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.ListCategories;

internal sealed class ListCategoriesQueryHandler
    : IQueryHandler<ListCategoriesQuery, ListCategoriesResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ListCategoriesQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<ListCategoriesResult>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var (query, parameters) = BuildQuery(request);

        var items = await connection.QueryAsync<CategoryListItemWithCount>(
            new CommandDefinition(query, parameters, cancellationToken: cancellationToken));

        var itemsList = items.ToList();
        var totalCount = itemsList.FirstOrDefault()?.TotalCount ?? 0;

        var result = new ListCategoriesResult
        {
            Items = itemsList.Select(item => new CategoryListItem
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                Description = item.Description,
                ParentCategoryId = item.ParentCategoryId,
                Path = item.Path,
                DisplayOrder = item.DisplayOrder,
                PublishStatus = item.PublishStatus,
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

        return Result<ListCategoriesResult>.Success(result);
    }

    private static (string Query, DynamicParameters Parameters) BuildQuery(ListCategoriesQuery request)
    {
        var parameters = new DynamicParameters();
        // Always exclude soft-deleted categories by default
        var whereConditions = new List<string> { "c.deleted_at IS NULL" };

        // Search term filter using PostgreSQL ILIKE for case-insensitive search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            whereConditions.Add("""
                (
                    c.name ILIKE @SearchPattern
                    OR c.description ILIKE @SearchPattern
                )
                """);
            parameters.Add("SearchPattern", $"%{request.SearchTerm}%");
        }

        // PublishStatus filter
        if (request.PublishStatus.HasValue)
        {
            whereConditions.Add("c.publish_status = @PublishStatus");
            parameters.Add("PublishStatus", request.PublishStatus.Value);
        }

        // Root category filter with optional subcategory inclusion
        if (request.RootCategoryId.HasValue)
        {
            if (request.IncludeSubcategories is true)
            {
                // Use ltree path matching to include categories that are descendants or equal
                whereConditions.Add("""
                    EXISTS (
                        SELECT 1
                        FROM categories rc
                        WHERE rc.id = @RootCategoryId
                          AND rc.path @> c.path
                    )
                    """);
            }
            else
            {
                // Direct parent relationship only
                whereConditions.Add("c.parent_category_id = @RootCategoryId");
            }
            parameters.Add("RootCategoryId", request.RootCategoryId.Value);
        }

        var whereClause = whereConditions.Count > 0
            ? $"WHERE {string.Join("\n  AND ", whereConditions)}"
            : "";

        var orderByClause = BuildOrderByClause(request.SortBy);

        var query = $"""
            WITH filtered_categories AS (
                SELECT
                    c.id,
                    c.name,
                    c.current_slug AS slug,
                    c.description,
                    c.parent_category_id,
                    c.path,
                    c.display_order,
                    c.publish_status,
                    c.image_path AS image_source,
                    c.image_alt_text,
                    c.created_at,
                    c.updated_at,
                    c.deleted_at
                FROM categories c
                {whereClause}
            ),
            page AS (
                SELECT *
                FROM filtered_categories
                {orderByClause}
                LIMIT @PageSize OFFSET @Offset
            ),
            total AS (
                SELECT COUNT(*) AS total_count
                FROM filtered_categories
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
            // Default sorting: created_at descending, then display_order, then id for stable ordering
            return "ORDER BY created_at DESC, id";
        }

        var orderByParts = new List<string>();

        foreach (var sortProperty in sortBy)
        {
            var direction = sortProperty.Direction?.ToSqlKeyword() ?? "ASC";
            var column = sortProperty.Name.ToLowerInvariant() switch
            {
                "name" => $"lower(name) {direction}",
                "created_at" => $"created_at {direction}",
                "updated_at" => $"updated_at {direction}",
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortProperty.Name, $"Unknown sort property: {sortProperty.Name}")
            };

            orderByParts.Add(column);
        }

        // Always add id as final tiebreaker for stable ordering
        orderByParts.Add("id");

        return $"ORDER BY {string.Join(", ", orderByParts)}";
    }

    private sealed class CategoryListItemWithCount
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public long? ParentCategoryId { get; set; }
        public string? Path { get; set; }
        public int DisplayOrder { get; set; }
        public int PublishStatus { get; set; }
        public string? ImageSource { get; set; }
        public string? ImageAltText { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int TotalCount { get; set; }
    }
}
