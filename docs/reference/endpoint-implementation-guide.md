# Endpoint Implementation Guide

This guide provides a concise reference for implementing Web API endpoints in the Clean Architecture refactor (`src` folder). Endpoints serve as the thin HTTP layer that connects consumers to application use cases.

---

## Quick Reference Checklist

When creating a new endpoint:

- [ ] Create endpoint folder: `Endpoints/{Feature}/{Operation}/`
- [ ] Create `{Operation}Endpoint.cs` with mapping and handler methods
- [ ] Create `{Operation}Request.cs` with `ToCommand()` (for POST/PATCH/PUT)
- [ ] Create `{Operation}Response.cs` with `FromResult()`
- [ ] Add endpoint name to `{Feature}EndpointsModule.Names`
- [ ] Add mapping call to `{Feature}EndpointsModule.Map{Feature}Endpoints()`
- [ ] Define typed `Results<...>` return type (all possible responses)
- [ ] Map application errors to HTTP status codes
- [ ] Use `ApiResults` factory methods for all responses
- [ ] Add HTTP-specific validation (enum parsing, etc.) in endpoint

---

## File Structure

### Directory Layout

```
src/web/ForeverBloom.WebApi/
├── Endpoints/
│   ├── Categories/
│   │   ├── CreateCategory/
│   │   │   ├── CreateCategoryEndpoint.cs
│   │   │   ├── CreateCategoryRequest.cs
│   │   │   └── CreateCategoryResponse.cs
│   │   ├── GetCategoryById/
│   │   │   ├── GetCategoryByIdEndpoint.cs
│   │   │   └── GetCategoryByIdResponse.cs
│   │   ├── ReparentCategory/
│   │   │   ├── ReparentCategoryEndpoint.cs
│   │   │   ├── ReparentCategoryRequest.cs
│   │   │   └── ReparentCategoryResponse.cs
│   │   └── CategoryEndpointsModule.cs
│   └── Products/
│       └── ProductEndpointsModule.cs
├── Results/
│   ├── ApiResults.cs
│   └── {ResultType}.cs
├── Mapping/
│   └── {Entity}Mapper.cs
└── Program.cs
```

### Naming Conventions

- **Endpoint Folders**: `{Operation}` (e.g., `CreateCategory`, `ReparentCategory`, `GetProductById`)
- **Endpoint Files**: `{Operation}Endpoint.cs`
- **Request DTOs**: `{Operation}Request.cs`
- **Response DTOs**: `{Operation}Response.cs`
- **Module Files**: `{Feature}EndpointsModule.cs`

---

## Module Pattern

Each feature area has a **Module** class that centralizes endpoint registration. This structure is **mandatory**.

**File**: `Endpoints/{Feature}/{Feature}EndpointsModule.cs`

```csharp
public static class CategoryEndpointsModule
{
    // Endpoint name constants (for routing/OpenAPI)
    public static class Names
    {
        public const string GetCategoryById = "GetCategoryById";
        public const string CreateCategory = "CreateCategory";
        public const string UpdateCategory = "UpdateCategory";
        public const string ReparentCategory = "ReparentCategory";
    }

    // Tag constants (for OpenAPI grouping)
    public static class Tags
    {
        public const string Categories = "Categories";
        public const string Admin = "Admin";
    }

    // Service registration (even if empty)
    public static IServiceCollection AddCategoryEndpoints(this IServiceCollection services)
    {
        // Register endpoint-specific services (validators, presenters, etc.)
        return services;
    }

    // Route mapping
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var adminEndpointsGroup = app.MapGroup("/admin/categories")
            .WithTags(Tags.Categories, Tags.Admin)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);

        adminEndpointsGroup.MapGetCategoryByIdEndpoint();
        adminEndpointsGroup.MapCreateCategoryEndpoint();
        adminEndpointsGroup.MapUpdateCategoryEndpoint();
        adminEndpointsGroup.MapReparentCategoryEndpoint();

        return app;
    }
}
```

### Registration in `Program.cs`

```csharp
// Register endpoint modules
builder.Services.AddCategoryEndpoints();
builder.Services.AddProductEndpoints();

// Map endpoints to routes
var apiV1Group = app.MapGroup("/api/v1").EnrichProblemDetails();
apiV1Group.MapCategoryEndpoints();
apiV1Group.MapProductEndpoints();
```

---

## Endpoint Components

### 1. Endpoint Class

**File**: `Endpoints/{Feature}/{Operation}/{Operation}Endpoint.cs`

**Structure**:
- Static class
- Internal mapping method (registers route)
- Private handler method (processes request)

**Example: Simple Query**

```csharp
// File: GetCategoryById/GetCategoryByIdEndpoint.cs
public static class GetCategoryByIdEndpoint
{
    // Mapping method (internal)
    internal static IEndpointRouteBuilder MapGetCategoryByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoryById);

        return app;
    }

    // Handler method (private)
    private static async Task<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult>> HandleAsync(
        long id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult>>(
            onSuccess: payload => ApiResults.Ok(GetCategoryByIdResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                _ => throw new UnreachableException()
            });
    }
}
```

**Example: Command with Request Body**

```csharp
// File: CreateCategory/CreateCategoryEndpoint.cs
public static class CreateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.CreateCategory);

        return app;
    }

    private static async Task<Results<
        CreatedResult<CreateCategoryResponse>,
        ValidationProblemResult,
        ConflictResult,
        BadRequestResult>> HandleAsync(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.Match<Results<
            CreatedResult<CreateCategoryResponse>,
            ValidationProblemResult,
            ConflictResult,
            BadRequestResult>>(
            onSuccess: payload => ApiResults.Created(
                $"/api/v1/admin/categories/{payload.CategoryId}",
                CreateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.SlugAlreadyInUse => ApiResults.Conflict(error.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
```

**Example: Update with Enum Parsing**

```csharp
// File: UpdateCategory/UpdateCategoryEndpoint.cs
public static class UpdateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{categoryId:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.UpdateCategory);

        return app;
    }

    private static async Task<Results<
        OkResult<UpdateCategoryResponse>,
        ValidationProblemResult,
        NotFoundResult,
        ConflictResult,
        BadRequestResult>> HandleAsync(
        long categoryId,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // HTTP-specific validation: parse enum string
        PublishStatus? publishStatus = null;
        if (request.PublishStatus.IsSet)
        {
            if (!PublishStatusMapper.TryParse(request.PublishStatus.Value, out var parsedStatus))
            {
                return ApiResults.ValidationProblem(
                    nameof(request.PublishStatus),
                    new ValidationErrorDetail(
                        code: "PublishStatus.InvalidValue",
                        message: "The provided publish status is not defined.",
                        attemptedValue: request.PublishStatus.Value,
                        customState: new { PublishStatusMapper.ValidValues }));
            }
            publishStatus = parsedStatus;
        }

        var command = request.ToCommand(categoryId, publishStatus);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<
            OkResult<UpdateCategoryResponse>,
            ValidationProblemResult,
            NotFoundResult,
            ConflictResult,
            BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                CategoryErrors.SlugAlreadyInUse => ApiResults.Conflict(error.Code),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
```

### 2. Request DTO

**File**: `Endpoints/{Feature}/{Operation}/{Operation}Request.cs`

**Rules**:
- `internal sealed record`
- Includes `ToCommand()` method to map to application layer
- Route parameters passed as method arguments to `ToCommand()`
- Optional&lt;T&gt; for PATCH operations

**Example: Simple POST Request**

```csharp
// File: CreateCategory/CreateCategoryRequest.cs
internal sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    long? ParentCategoryId,
    string PublishStatus)
{
    internal CreateCategoryCommand ToCommand() => new(
        Name: Name,
        Description: Description,
        ParentCategoryId: ParentCategoryId,
        PublishStatus: PublishStatus);
}
```

**Example: POST with Route Parameters**

```csharp
// File: ReparentCategory/ReparentCategoryRequest.cs
internal sealed record ReparentCategoryRequest(
    long? NewParentCategoryId,
    uint RowVersion)
{
    internal ReparentCategoryCommand ToCommand(long categoryId)
    {
        return new ReparentCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion,
            NewParentCategoryId: NewParentCategoryId);
    }
}
```

**Example: PATCH with Optional&lt;T&gt;**

```csharp
// File: UpdateCategory/UpdateCategoryRequest.cs
internal sealed record UpdateCategoryRequest(
    uint RowVersion,  // Always required for updates
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string> PublishStatus)  // String for enum conversion
{
    internal UpdateCategoryCommand ToCommand(
        long categoryId,
        PublishStatus? publishStatus)
    {
        return new UpdateCategoryCommand(
            CategoryId: categoryId,
            RowVersion: RowVersion,
            Name: Name,
            Description: Description,
            PublishStatus: publishStatus is not null
                ? Optional<PublishStatus>.FromValue(publishStatus.Value)
                : Optional<PublishStatus>.Unset);
    }
}
```

### 3. Response DTO

**File**: `Endpoints/{Feature}/{Operation}/{Operation}Response.cs`

**Rules**:
- `internal sealed record`
- Includes static `FromResult()` method to map from application layer result
- Mappers used for domain enums → strings

**Example: Simple Response**

```csharp
// File: CreateCategory/CreateCategoryResponse.cs
internal sealed record CreateCategoryResponse(long Id)
{
    internal static CreateCategoryResponse FromResult(CreateCategoryResult result)
        => new(result.CategoryId);
}
```

**Example: Complex Response with Enum Mapping**

```csharp
// File: GetCategoryById/GetCategoryByIdResponse.cs
internal sealed record GetCategoryByIdResponse(
    long Id,
    string Name,
    string? Description,
    long? ParentCategoryId,
    string PublishStatus,  // Mapped from code to string
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    uint RowVersion)
{
    internal static GetCategoryByIdResponse FromResult(GetCategoryByIdResult result)
    {
        return new GetCategoryByIdResponse(
            Id: result.Id,
            Name: result.Name,
            Description: result.Description,
            ParentCategoryId: result.ParentCategoryId,
            PublishStatus: PublishStatusMapper.ToString(result.PublishStatusCode),
            CreatedAt: result.CreatedAt,
            UpdatedAt: result.UpdatedAt,
            RowVersion: result.RowVersion);
    }
}
```

**Example: Response with Nested Objects**

```csharp
// File: GetProductById/GetProductByIdResponse.cs
internal sealed record GetProductByIdResponse(
    long Id,
    string Name,
    string Slug,
    decimal Price,
    IReadOnlyList<GetProductByIdResponseImage> Images,
    uint RowVersion)
{
    internal static GetProductByIdResponse FromResult(GetProductByIdResult result)
    {
        var images = result.Images
            .Select(image => new GetProductByIdResponseImage(
                Id: image.Id,
                ImagePath: image.ImagePath,
                AltText: image.AltText,
                DisplayOrder: image.DisplayOrder))
            .ToArray();

        return new GetProductByIdResponse(
            Id: result.Id,
            Name: result.Name,
            Slug: result.Slug,
            Price: result.Price,
            Images: images,
            RowVersion: result.RowVersion);
    }
}

internal sealed record GetProductByIdResponseImage(
    long Id,
    string ImagePath,
    string? AltText,
    int DisplayOrder);
```

---

## Request Processing Flow

The endpoint acts as a thin layer connecting HTTP to the application:

```
HTTP Request → Endpoint Handler
            ↓
Request DTO → .ToCommand() → Command/Query
            ↓
ISender.Send() → Command/Query Handler (Application Layer)
            ↓
Result<TResult> ← Handler
            ↓
.Match() → Success or Failure path
            ↓
Response DTO ← .FromResult() ← Application Result
            ↓
ApiResults.{Method}() → HTTP Response
```

### MediatR Integration

All endpoints use **MediatR's `ISender`** to dispatch commands/queries:

```csharp
private static async Task<Results<...>> HandleAsync(
    ISender sender,  // Injected by Minimal APIs
    CancellationToken cancellationToken)
{
    var result = await sender.Send(command, cancellationToken);
    // ...
}
```

### Railway-Oriented Programming

All handlers use `Result<T>.Match()` for error handling:

```csharp
return result.Match<Results<OkResult<Response>, NotFoundResult, BadRequestResult>>(
    onSuccess: payload => ApiResults.Ok(Response.FromResult(payload)),
    onFailure: error => error switch
    {
        SpecificError.NotFound => ApiResults.NotFound(),
        ValidationError validation => ApiResults.ValidationProblem(validation),
        _ => ApiResults.BadRequest(error)
    });
```

---

## Response Handling

### Typed Results (Compiler-Enforced)

Endpoints use **typed union results** (`Results<T1, T2, T3>`) to specify all possible responses. This enables OpenAPI generation and compile-time safety.

```csharp
private static async Task<Results<
    OkResult<UpdateCategoryResponse>,      // 200
    ValidationProblemResult,                // 400 with validation errors
    NotFoundResult,                         // 404
    ConflictResult,                         // 409
    BadRequestResult                        // 400 generic
>> HandleAsync(...)
```

**All possible response types must be listed** - the compiler enforces this when using `ApiResults` factory methods.

### ApiResults Factory (Mandatory)

**Always use `ApiResults` factory methods** to create responses. Direct instantiation of result types is not allowed.

**Location**: `src/web/ForeverBloom.WebApi/Results/ApiResults.cs`

### Error Mapping Examples

Map application errors to appropriate HTTP status codes:

```csharp
onFailure: error => error switch
{
    // Not Found (404)
    CategoryErrors.NotFoundById => ApiResults.NotFound(),
    ProductErrors.NotFoundById => ApiResults.NotFound(),

    // Conflict (409)
    CategoryErrors.SlugAlreadyInUse => ApiResults.Conflict(error.Code),
    ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),

    // Validation Problem (400 with details)
    ValidationError validation => ApiResults.ValidationProblem(validation),

    // Generic Bad Request (400)
    _ => ApiResults.BadRequest(error)
}
```

---

## Validation Boundary (Strict Rule)

**Endpoints handle HTTP-specific validation. Application handlers handle business validation.**

### Endpoint Layer Responsibilities

- Enum string parsing and validation
- Request structure validation
- Route parameter validation
- HTTP-specific constraints

**Example: Enum Parsing in Endpoint**

```csharp
PublishStatus? publishStatus = null;
if (request.PublishStatus.IsSet)
{
    if (!PublishStatusMapper.TryParse(request.PublishStatus.Value, out var parsedStatus))
    {
        return ApiResults.ValidationProblem(
            nameof(request.PublishStatus),
            new ValidationErrorDetail(
                code: "PublishStatus.InvalidValue",
                message: "The provided publish status is not defined.",
                attemptedValue: request.PublishStatus.Value,
                customState: new { PublishStatusMapper.ValidValues }));
    }
    publishStatus = parsedStatus;
}
```

### Application Layer Responsibilities

- Business rule validation (via FluentValidation)
- Domain logic validation
- Data consistency validation
- Cross-aggregate validation

---

## Optional&lt;T&gt; for PATCH Operations

PATCH operations use `Optional<T>` to distinguish between:
- **Unset**: Property not included in request (no change)
- **Null**: Explicitly set to null (clear value)
- **Value**: Set to a specific value (update)

**Example Usage**:

```csharp
internal sealed record UpdateCategoryRequest(
    uint RowVersion,              // Required
    Optional<string> Name,        // Can be unset or value
    Optional<string?> Description // Can be unset, null, or value
)
```

**Checking Optional Values**:

```csharp
if (request.Name.IsSet)
{
    // Process the value (request.Name.Value)
}
```

**Mapping to Application Layer**:

```csharp
internal UpdateCategoryCommand ToCommand(long categoryId, PublishStatus? publishStatus)
{
    return new UpdateCategoryCommand(
        CategoryId: categoryId,
        RowVersion: RowVersion,
        Name: Name,  // Pass Optional<T> directly
        Description: Description,
        PublishStatus: publishStatus is not null
            ? Optional<PublishStatus>.FromValue(publishStatus.Value)
            : Optional<PublishStatus>.Unset);
}
```

---

## Route Design

### Standard REST Patterns

Use standard HTTP methods and resource paths:

- **GET /{id}** - Get single resource by ID
- **GET /{slug}** - Get single resource by slug (public endpoints)
- **POST /** - Create new resource
- **PATCH /{id}** - Partial update
- **DELETE /{id}** - Delete resource

### Custom Actions (Colon Syntax)

Use `:action` syntax for operations that don't fit standard REST patterns:

- **POST /{id}:reparent** - Custom action on existing resource
- **POST /{id}:publish** - State transition
- **POST /{id}:changeSlug** - Specialized update

**When to Use Custom Actions**:

1. **Non-CRUD Operations**: Actions that don't map to standard CRUD (e.g., `reparent`, `publish`, `archive`)
2. **State Transitions**: Operations that represent state changes (e.g., `publish`, `approve`, `cancel`)
3. **Complex Operations**: Multi-step operations that warrant explicit naming (e.g., `clone`, `merge`)
4. **Semantic Clarity**: When the operation needs a clear, explicit name for API consumers

### Route Grouping

Group related endpoints with shared configuration:

```csharp
var adminEndpointsGroup = app.MapGroup("/admin/categories")
    .WithTags(Tags.Categories, Tags.Admin)
    .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);

var publicEndpointsGroup = app.MapGroup("/products")
    .WithTags(Tags.Products, Tags.Public);
```

### Versioning

- Base route: `/api/v1`
- Version in URL path
- Grouped at root level in `Program.cs`

---

## Mapper Classes for Database Enums

Database enums (stored as integer codes with string names) require mapper classes for HTTP layer conversion.

**Location**: `src/web/ForeverBloom.WebApi/Mapping/{Entity}Mapper.cs`

**Pattern**:

```csharp
public static class PublishStatusMapper
{
    private static readonly Dictionary<string, PublishStatus> StringToEnum = new(StringComparer.OrdinalIgnoreCase)
    {
        { "draft", PublishStatus.Draft },
        { "published", PublishStatus.Published },
        { "archived", PublishStatus.Archived }
    };

    private static readonly Dictionary<int, string> CodeToString = new()
    {
        { PublishStatus.Draft.Code, "draft" },
        { PublishStatus.Published.Code, "published" },
        { PublishStatus.Archived.Code, "archived" }
    };

    public static bool TryParse(string value, [NotNullWhen(true)] out PublishStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (StringToEnum.TryGetValue(value, out var result))
        {
            status = result;
            return true;
        }

        return false;
    }

    public static string ToString(PublishStatus status)
    {
        return CodeToString.TryGetValue(status.Code, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown publish status.");
    }

    public static string ToString(int code)
    {
        return CodeToString.TryGetValue(code, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown publish status code.");
    }

    public static IReadOnlyCollection<string> ValidValues => StringToEnum.Keys.ToArray();
}
```

**Usage in Endpoints**:

```csharp
// Parsing string to domain type
if (!PublishStatusMapper.TryParse(request.PublishStatus.Value, out var publishStatus))
{
    return ApiResults.ValidationProblem(
        nameof(request.PublishStatus),
        new ValidationErrorDetail(
            code: "PublishStatus.InvalidValue",
            message: "The provided publish status is not defined.",
            attemptedValue: request.PublishStatus.Value,
            customState: new { PublishStatusMapper.ValidValues }));
}

// Converting domain type to string for response
PublishStatus: PublishStatusMapper.ToString(result.PublishStatusCode)
```

---

## Strict Rules Summary

### Non-Negotiable Rules

1. ✅ **Always use `internal` visibility** for DTOs and endpoint methods
2. ✅ **Follow exact Module pattern** with Names and Tags classes
3. ✅ **Use mapper classes** for all database enum conversions
4. ✅ **List all possible responses** in `Results<...>` type signature
5. ✅ **Always use `ApiResults` factory methods** - never instantiate result types directly
6. ✅ **HTTP validation in endpoints, business validation in application layer** - strict boundary
7. ✅ **Use `ISender` for application layer calls** - never instantiate handlers directly
8. ✅ **Follow Railway-Oriented Programming** with `Result<T>.Match()` for error handling
9. ✅ **Use `Optional<T>` for all PATCH properties** (except required fields like `RowVersion`)
10. ✅ **Include `CancellationToken`** as last parameter in all handlers

### Recommended Patterns

1. 📌 **Use `ToCommand()` / `FromResult()`** naming for mapping methods (preferred but not strict)
2. 📌 **Route parameters before request body** in handler method signature
3. 📌 **Named routes for all endpoints** using module constants
4. 📌 **Group related endpoints** with `MapGroup()` for shared routes/metadata
5. 📌 **Custom actions use `:action` syntax** when operations don't fit standard REST

---

## Summary

Endpoints are the thin HTTP layer connecting consumers to application use cases. They:

- Accept HTTP requests and validate HTTP-specific concerns
- Transform requests into commands/queries
- Dispatch to application layer via MediatR
- Map application results to HTTP responses
- Handle errors with appropriate status codes

Follow the patterns and rules in this guide to maintain consistency and ensure proper separation of concerns between the HTTP layer and application layer.
