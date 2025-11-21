using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.ListCategories;

public sealed record ListCategoriesQuery : IQuery<ListCategoriesResult>
{
    public static readonly HashSet<string> AllowedSortProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "created_at", "updated_at"
    };

    public int PageNumber { get; private init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; private init; } = PaginationConstants.DefaultPageSize;
    public SortProperty[] SortBy { get; private init; } = [];
    public string? SearchTerm { get; private init; }
    public long? RootCategoryId { get; private init; }
    public bool? IncludeSubcategories { get; private init; }
    public int? PublishStatus { get; private init; }

    public static Result<ListCategoriesQuery> Create(
        int pageNumber = PaginationConstants.DefaultPageNumber,
        int pageSize = PaginationConstants.DefaultPageSize,
        string? sortBy = null,
        string? searchTerm = null,
        long? rootCategoryId = null,
        bool? includeSubcategories = null,
        string? publishStatus = null)
    {
        var errors = new List<IError>();

        // Validate PageNumber
        if (pageNumber <= 0)
        {
            errors.Add(new PaginationErrors.InvalidPageNumber(pageNumber));
        }

        // Validate PageSize
        if (pageSize is < PaginationConstants.MinimumPageSize or > PaginationConstants.MaximumPageSize)
        {
            errors.Add(new PaginationErrors.InvalidPageSize(pageSize));
        }

        // Parse and validate SortBy
        var parseSortResult = SortPropertyParser.Parse(sortBy);
        if (parseSortResult.IsFailure)
        {
            errors.Add(parseSortResult.Error);
        }

        // Normalize: always use an array (empty or with values), never null
        var sortProperties = parseSortResult.IsSuccess ? parseSortResult.Value : [];

        if (sortProperties.Length > 0)
        {
            // Validate each property name
            foreach (var property in sortProperties)
            {
                if (!AllowedSortProperties.Contains(property.Name))
                {
                    errors.Add(new SortingErrors.InvalidSortProperty(
                        property.Name,
                        AllowedSortProperties.ToArray()));
                }
            }

            // Validate no duplicates
            var duplicates = sortProperties
                .Select((property, index) => (property.Name, Index: index))
                .GroupBy(p => p.Name)
                .Where(g => g.Count() > 1)
                .Select(g => (Name: g.Key, Indices: g.Select(x => x.Index).ToArray()))
                .ToArray();

            foreach (var duplicate in duplicates)
            {
                errors.Add(new SortingErrors.DuplicateSortProperty(
                    duplicate.Name,
                    duplicate.Indices));
            }
        }

        // Parse and validate PublishStatus
        int? publishStatusCode = null;
        if (!string.IsNullOrWhiteSpace(publishStatus))
        {
            var publishStatusResult = Domain.Catalog.PublishStatus.FromName(publishStatus);
            if (publishStatusResult.IsFailure)
            {
                errors.Add(publishStatusResult.Error);
            }
            else
            {
                publishStatusCode = publishStatusResult.Value.Code;
            }
        }

        return Result<ListCategoriesQuery>.FromValidation(
            errors,
            () => new ListCategoriesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortProperties,
                SearchTerm = searchTerm,
                RootCategoryId = rootCategoryId,
                IncludeSubcategories = includeSubcategories,
                PublishStatus = publishStatusCode
            });
    }
}
