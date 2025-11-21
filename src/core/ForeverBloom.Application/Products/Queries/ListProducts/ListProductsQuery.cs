using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Queries.ListProducts;

public sealed record ListProductsQuery : IQuery<ListProductsResult>
{
    public static readonly HashSet<string> AllowedSortProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "price",
        "created_at",
        "updated_at",
        "category_name"
    };

    public int PageNumber { get; private init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; private init; } = PaginationConstants.DefaultPageSize;
    public SortProperty[] SortBy { get; private init; } = [];
    public string? SearchTerm { get; private init; }
    public long? CategoryId { get; private init; }
    public bool? IncludeSubcategories { get; private init; }

    public static Result<ListProductsQuery> Create(
        int pageNumber = PaginationConstants.DefaultPageNumber,
        int pageSize = PaginationConstants.DefaultPageSize,
        string? sortBy = null,
        string? searchTerm = null,
        long? categoryId = null,
        bool? includeSubcategories = null)
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
        var parseResult = SortPropertyParser.Parse(sortBy);
        SortProperty[] sortProperties = [];

        if (parseResult.IsFailure)
        {
            errors.Add(parseResult.Error);
        }
        else
        {
            sortProperties = parseResult.Value;

            // Validate each property name against allowed properties
            foreach (var property in sortProperties)
            {
                if (!AllowedSortProperties.Contains(property.Name))
                {
                    errors.Add(new SortingErrors.InvalidSortProperty(
                        property.Name,
                        AllowedSortProperties.ToArray()));
                }
            }

            // Check for duplicate properties
            var propertyGroups = sortProperties
                .Select((prop, index) => new { prop.Name, Index = index })
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var group in propertyGroups)
            {
                errors.Add(new SortingErrors.DuplicateSortProperty(
                    group.Key,
                    group.Select(x => x.Index).ToArray()));
            }
        }

        return Result<ListProductsQuery>.FromValidation(
            errors,
            () => new ListProductsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortProperties,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                IncludeSubcategories = includeSubcategories
            });
    }
}
