# Pagination, Sorting, and Filtering

This document explains how list endpoints in the ForeverBloom API handle pagination, sorting, and filtering. The goal is a predictable request/response shape across endpoints, with details that are easy for clients to follow.

## Core Pattern

All endpoints that return collections follow a consistent pattern. Requests accept common pagination fields and optional sort/filter inputs. Responses return items together with pagination metadata that UIs can use directly.

### Request Structure

List endpoints inherit from `PaginationQuery`:

```csharp
public class PaginationQuery
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}
```

Defaults are applied per endpoint, not on `PaginationQuery` itself. Current behavior:
- Admin Products and Admin Categories: page number 1, page size 10.
- Public Products: page number 1, page size 20, with a maximum page size of 100 (anything higher is capped).

Endpoint-specific requests extend this base with sorting and filtering. For example, `ListAdminProductsRequest` includes:

```csharp
public sealed class ListAdminProductsRequest : PaginationQuery
{
    public string? OrderBy { get; set; }                // e.g., "Name asc, Price desc"
    public bool? ProductActive { get; set; }
    public bool? CategoryActive { get; set; }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public bool? IncludeSubcategories { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IncludeArchived { get; set; }
}
```

### Response Structure

All list endpoints return `PaginatedList<T>`:

```csharp
public record PaginatedList<T>
{
    public IList<T> Items { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
```

This shape includes everything needed for basic pagination controls. For empty result sets, `TotalPages` is 0 and both `HasPreviousPage` and `HasNextPage` are false.

## Implementation Approach

### Query Execution Order

Database work happens in a specific order to keep results correct and efficient:
1. Filter the dataset (WHERE)
2. Sort the filtered data (ORDER BY)
3. Apply pagination (SKIP/TAKE)

Clients should pass `PageNumber >= 1` and `PageSize >= 1`. Endpoints fill in defaults when these values are null, and the Public Products endpoint caps `PageSize` at 100.

### Dynamic Sorting

Sorting accepts only whitelisted columns to keep things safe and predictable:

```csharp
private static readonly HashSet<string> AllowedSortColumns =
    SortingHelper.CreateAllowedSortColumns(
        // Example for products endpoints; actual sets vary per endpoint
        nameof(PublicProductListItem.Name),
        nameof(PublicProductListItem.Price),
        nameof(PublicProductListItem.CategoryName)
    );
```

The `ApplySort` extension builds the ordering dynamically while preventing invalid property access.

Sort strings are simple and predictable:
- Columns are comma‑separated, e.g., `"Name asc, Price desc"`.
- Direction can be `asc` or `desc` (case‑insensitive). If omitted, it defaults to `asc`.
- Unknown or duplicate properties result in a 400 Bad Request.

Allowed columns differ by endpoint. For example:
- Public Products: `Name`, `Price`, `CategoryName`.
- Admin Products: `Name`, `DisplayOrder`, `Price`, `CreatedAt`, `CategoryName`.
- Admin Categories: `Name`, `DisplayOrder`, `Id`.

Sometimes DTO property names differ from entity fields. Property mapping bridges that gap:

```csharp
private static readonly Dictionary<string, Expression<Func<Product, object>>> PropertyMapping = new()
{
    { nameof(ProductListItem.CategoryName), p => p.Category.Name }
};
```

Clients sort by `CategoryName`, which maps to the `Product.Category.Name` path in the data model.

When no `OrderBy` is provided, endpoints sort by `DisplayOrder`, then `Name`. The Public Products endpoint treats `Price` specially when it is the only sort: products with `null` prices appear last, and ties are broken by `DisplayOrder` and then `Name` for stability.

## Design Principles

- **DTO is the contract:** clients use DTO property names, not internal entity structure.
- **Security first:** only explicit columns are sortable.
- **Encapsulation:** property mapping lives in the query provider and isn’t exposed at the API layer.
- **Computed properties:** display‑only and not server‑sortable; let the client handle them if needed.
- **Fail fast:** invalid sort parameters return 400.
- **Endpoint‑specific defaults:** pagination defaults and caps are defined per endpoint; Public Products enforces a 100‑item page‑size cap.

For concrete examples, see the list endpoints under `src/backend/ForeverBloom.Api/Endpoints/`.
