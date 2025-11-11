using System.Data;
using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

public sealed class GetCategoryBySlugQueryHandler(IDbConnectionFactory connectionFactory)
    : IQueryHandler<GetCategoryBySlugQuery, GetCategoryBySlugResult>
{
    public async Task<Result<GetCategoryBySlugResult>> Handle(
        GetCategoryBySlugQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // Slug must exist in the registry
        var slugLookup = await LookupSlugAsync(connection, query.Slug, cancellationToken);
        if (slugLookup is null)
        {
            return Result<GetCategoryBySlugResult>.Failure(new CategoryErrors.NotFoundBySlug(query.Slug));
        }

        // Category must be accessible - all ancestors not deleted and published, including itself
        var categoryDto = await FetchCategoryAsync(connection, slugLookup.CategoryId, cancellationToken);
        if (categoryDto is null)
        {
            return Result<GetCategoryBySlugResult>.Failure(new CategoryErrors.NotFoundBySlug(query.Slug));
        }

        // Provided slug must be the current slug of the entity
        if (!slugLookup.IsCurrent)
        {
            return Result<GetCategoryBySlugResult>.Failure(
                new CategoryErrors.SlugChanged(query.Slug, slugLookup.CurrentSlug));
        }

        // Category is accessible - fetch breadcrumbs and return full result
        var breadcrumbs = await FetchBreadcrumbsAsync(connection, categoryDto.Path, cancellationToken);
        var result = new GetCategoryBySlugResult
        {
            Id = categoryDto.Id,
            Name = categoryDto.Name,
            Description = categoryDto.Description,
            Slug = categoryDto.Slug,
            ImageSource = categoryDto.ImageSource,
            ImageAltText = categoryDto.ImageAltText,
            ParentCategoryId = categoryDto.ParentCategoryId,
            Breadcrumbs = breadcrumbs
        };
        return Result<GetCategoryBySlugResult>.Success(result);
    }

    private static async Task<SlugLookupResult?> LookupSlugAsync(
        IDbConnection connection,
        string slug,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entity_id, is_active
            FROM slug_registry
            WHERE slug = @Slug AND entity_type = 2 -- Category
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
                CategoryId = slugEntry.EntityId,
                CurrentSlug = slug,
                IsCurrent = true
            };
        }

        // If the provided slug is inactive, find the current active slug for this category
        const string activeSlugSql = """
            SELECT slug
            FROM slug_registry
            WHERE entity_id = @EntityId
              AND entity_type = 2 -- Category
              AND is_active = true
            """;

        var currentSlug = await connection.QuerySingleAsync<string>(
            new CommandDefinition(activeSlugSql, new { slugEntry.EntityId }, cancellationToken: cancellationToken));

        return new SlugLookupResult
        {
            CategoryId = slugEntry.EntityId,
            CurrentSlug = currentSlug,
            IsCurrent = false
        };
    }

    private static async Task<CategoryDto?> FetchCategoryAsync(
        IDbConnection connection,
        long categoryId,
        CancellationToken cancellationToken)
    {
        // Category must be publicly visible, which means that:
        // - it cannot be soft-deleted
        // - it has to be published
        // - it cannot have a soft-deleted ancestor
        // - all of its ancestors must be published
        const string sql = """
            SELECT
                c.id, c.name, c.description, c.current_slug AS slug,
                c.image_path AS image_source, c.image_alt_text,
                c.parent_category_id, c.path
            FROM categories c
            WHERE c.id = @CategoryId
              AND c.publish_status = 2  -- Published
              AND c.deleted_at IS NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM categories ancestor
                  WHERE ancestor.path @> c.path
                    AND (ancestor.deleted_at IS NOT NULL OR ancestor.publish_status != 2)
              )
            """;

        return await connection.QuerySingleOrDefaultAsync<CategoryDto>(
            new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
    }

    private static async Task<IReadOnlyList<BreadcrumbItem>> FetchBreadcrumbsAsync(
        IDbConnection connection,
        string path,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, current_slug AS slug
            FROM categories
            WHERE path @> @CategoryPath::ltree
              AND deleted_at IS NULL
              AND publish_status = 2  -- Published
            ORDER BY path
            """;

        var breadcrumbs = await connection.QueryAsync<BreadcrumbItem>(
            new CommandDefinition(sql, new { CategoryPath = path }, cancellationToken: cancellationToken));

        return breadcrumbs.ToList();
    }

    private sealed record SlugRegistryRow
    {
        public long EntityId { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class SlugLookupResult
    {
        public long CategoryId { get; init; }
        public string CurrentSlug { get; init; } = null!;
        public bool IsCurrent { get; init; }
    }

    private sealed class CategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Slug { get; set; } = null!;
        public string? ImageSource { get; set; }
        public string? ImageAltText { get; set; }
        public long? ParentCategoryId { get; set; }
        public string Path { get; set; } = null!;
    }
}
