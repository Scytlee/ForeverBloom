# Use Case Implementation Guide

**Purpose**: This guide is the authoritative reference for implementing use cases (commands and queries) in the ForeverBloom application layer. Use this as your primary guide for all future use case implementations to ensure consistency with the established Clean Architecture patterns.

**Scope**: Covers the `src/core/ForeverBloom.Application/` layer structure, patterns, and conventions.

---

## Quick Reference Checklist

When implementing a new use case, follow this checklist:

### For Commands:
- [ ] Create feature folder if it doesn't exist: `{Feature}/Commands/{UseCaseName}/`
- [ ] Create Command DTO (`{UseCase}Command.cs`) implementing `ICommand<TResult>`
- [ ] Create Handler (`{UseCase}CommandHandler.cs`) implementing `ICommandHandler<TCommand, TResult>`
- [ ] Create Validator (`{UseCase}CommandValidator.cs`) extending `AbstractValidator<TCommand>`
- [ ] Create Result DTO (`{UseCase}Result.cs`)
- [ ] Create Value Object Assembler if needed (`{UseCase}ValueObjectsAssembler.cs`)
- [ ] Add feature-specific errors to `{Feature}Errors.cs` if needed
- [ ] Register dependencies (auto-registered via assembly scanning)

### For Queries:
- [ ] Create feature folder if it doesn't exist: `{Feature}/Queries/{UseCaseName}/`
- [ ] Create Query DTO (`{UseCase}Query.cs`) implementing `IQuery<TResult>`
- [ ] Create Handler (`{UseCase}QueryHandler.cs`) implementing `IQueryHandler<TQuery, TResult>`
- [ ] Create Validator (`{UseCase}QueryValidator.cs`) extending `AbstractValidator<TQuery>`
- [ ] Create Result DTO as mutable class (`{UseCase}Result.cs`)
- [ ] Write SQL query using Dapper

---

## Folder Structure & Organization

### Location
All use cases live in: `src/core/ForeverBloom.Application/`

### Feature Folder Pattern
```
ForeverBloom.Application/
├── {Feature}/                                    # e.g., Products, Categories
│   ├── Commands/
│   │   ├── {UseCaseName}/                        # e.g., CreateProduct
│   │   │   ├── {UseCase}Command.cs
│   │   │   ├── {UseCase}CommandHandler.cs
│   │   │   ├── {UseCase}CommandValidator.cs
│   │   │   ├── {UseCase}Result.cs
│   │   │   └── {UseCase}ValueObjectsAssembler.cs # Optional
│   │   └── {AnotherUseCase}/
│   ├── Queries/
│   │   ├── {UseCaseName}/                        # e.g., GetProductById
│   │   │   ├── {UseCase}Query.cs
│   │   │   ├── {UseCase}QueryHandler.cs
│   │   │   ├── {UseCase}QueryValidator.cs
│   │   │   └── {UseCase}Result.cs
│   │   └── {AnotherUseCase}/
│   ├── {Feature}Errors.cs                        # Application-layer errors
│   └── {Feature}ValidationExtensions.cs          # Optional: feature-specific validation
├── Abstractions/
│   ├── Requests/                                 # ICommand, IQuery base interfaces
│   ├── Behaviors/                                # MediatR pipeline behaviors
│   ├── Data/                                     # IUnitOfWork, IDbConnectionFactory
│   └── Validation/                               # Shared validation extensions
└── DependencyInjection.cs                        # Service registration
```

### Folder Rules
- **Strict**: Each use case gets its own dedicated folder
- **One folder = one use case** (e.g., `CreateProduct/`, `UpdateProduct/`)
- Never group multiple use cases in the same folder, even if related

---

## Naming Conventions

All files in a use case folder share the same base name prefix:

| File Type | Pattern | Example |
|-----------|---------|---------|
| Command | `{Action}{Entity}Command` | `CreateProductCommand` |
| Query | `Get{Entity}By{Criteria}Query` | `GetProductByIdQuery`, `GetProductBySlugQuery` |
| Handler | `{UseCase}Handler` | `CreateProductCommandHandler` |
| Validator | `{UseCase}Validator` | `CreateProductCommandValidator` |
| Result | `{UseCase}Result` | `CreateProductResult` |
| Assembler | `{UseCase}ValueObjectsAssembler` | `CreateProductValueObjectsAssembler` |

---

## Command Implementation

### 1. Command DTO

**File**: `{UseCase}Command.cs`

```csharp
// Example: CreateProduct/CreateProductCommand.cs
public sealed record CreateProductCommand(
    string Name,
    string? SeoTitle,
    string? MetaDescription,
    string Slug,
    long CategoryId,
    decimal Price,
    string? Description,
    IReadOnlyCollection<CreateProductCommandImage>? Images
) : ICommand<CreateProductResult>;

// Nested DTOs: Always in the same file as parent
public sealed record CreateProductCommandImage(
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder
);
```

**Rules**:
- Use `sealed record` with primary constructor
- Immutable properties
- Implement `ICommand<TResult>` where `TResult` is your result DTO
- Nullable types for optional fields
- **Nested DTOs**: Always define in the same file as the parent command
- Collections use `IReadOnlyCollection<T>`

### 2. Command Handler

**File**: `{UseCase}CommandHandler.cs`

```csharp
// Example: CreateProduct/CreateProductCommandHandler.cs
internal sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISlugRegistrationService slugRegistrationService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _slugRegistrationService = slugRegistrationService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateProductResult>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Convert primitives to value objects
        var valueObjectsResult = command.AssembleValueObjects();
        if (valueObjectsResult.IsFailure)
            return Result<CreateProductResult>.Failure(valueObjectsResult.Error);

        var (name, seoTitle, metaDescription, slug, price, description) =
            valueObjectsResult.Value;

        // 2. Validate business rules requiring external data
        var slugAvailable = await _slugRegistrationService
            .IsSlugAvailableAsync(slug, cancellationToken);
        if (!slugAvailable)
            return Result<CreateProductResult>.Failure(
                new ProductErrors.SlugNotAvailable(slug));

        var category = await _categoryRepository
            .GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return Result<CreateProductResult>.Failure(
                new ProductErrors.CategoryNotFound(command.CategoryId));

        // 3. Assemble images (if provided)
        List<ProductImage>? images = null;
        if (command.Images is not null)
        {
            var imagesResult = command.Images.AssembleImages();
            if (imagesResult.IsFailure)
                return Result<CreateProductResult>.Failure(imagesResult.Error);
            images = imagesResult.Value;
        }

        // 4. Call domain entity factory
        var timestamp = _timeProvider.GetUtcNow();
        var productResult = Product.Create(
            name,
            seoTitle,
            metaDescription,
            slug,
            command.CategoryId,
            price,
            description,
            timestamp,
            images);

        if (productResult.IsFailure)
            return Result<CreateProductResult>.Failure(productResult.Error);

        var product = productResult.Value;

        // 5. Register slug
        _slugRegistrationService.RegisterSlug(slug, product.Id, SlugEntityType.Product);

        // 6. Persist via repository
        _productRepository.Add(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Return result
        return Result<CreateProductResult>.Success(
            new CreateProductResult(product.Id));
    }
}
```

**Handler Rules**:
- Mark as `internal sealed class`
- Implement `ICommandHandler<TCommand, TResult>`
- Return `Result<T>` for all paths (success and failure)
- Handler flow:
  1. Convert primitives → value objects (via Assembler)
  2. Validate business rules requiring external data (repositories, services)
  3. Call domain entity factory/methods (`Product.Create()`, `product.Update()`)
  4. Persist via repository
  5. Call `IUnitOfWork.SaveChangesAsync()`
  6. Return result DTO

**Dependencies** (inject as needed):
- `IUnitOfWork` - Required for persistence
- `I{Entity}Repository` - For aggregate roots being modified
- Domain services - For complex business logic coordination
- `ITimeProvider` - For timestamps (never use `DateTime.UtcNow` directly)
- Infrastructure services - Slug registry, email, etc.

### 3. Value Object Assembler

**File**: `{UseCase}ValueObjectsAssembler.cs` (optional)

**When to create**:
- When there are **multiple value objects** to convert
- When conversion logic **spans multiple lines**
- **Typically**: Only `Create` and `Update` use cases need assemblers
- **Queries**: Don't use assemblers (they work with primitives via Dapper)

```csharp
// Example: CreateProduct/CreateProductValueObjectsAssembler.cs
internal static class CreateProductValueObjectsAssembler
{
    public static Result<(
        ProductName Name,
        SeoTitle? SeoTitle,
        MetaDescription? MetaDescription,
        Slug Slug,
        Money Price,
        ProductDescription? Description
    )> AssembleValueObjects(this CreateProductCommand command)
    {
        var errors = new List<IError>();

        var nameResult = ProductName.Create(command.Name);
        if (nameResult.IsFailure) errors.Add(nameResult.Error);

        var seoTitleResult = SeoTitle.Create(command.SeoTitle);
        if (seoTitleResult.IsFailure) errors.Add(seoTitleResult.Error);

        var metaDescriptionResult = MetaDescription.Create(command.MetaDescription);
        if (metaDescriptionResult.IsFailure) errors.Add(metaDescriptionResult.Error);

        var slugResult = Slug.Create(command.Slug);
        if (slugResult.IsFailure) errors.Add(slugResult.Error);

        var priceResult = Money.Create(command.Price);
        if (priceResult.IsFailure) errors.Add(priceResult.Error);

        var descriptionResult = ProductDescription.Create(command.Description);
        if (descriptionResult.IsFailure) errors.Add(descriptionResult.Error);

        return Result<(ProductName, SeoTitle?, MetaDescription?, Slug, Money, ProductDescription?)>
            .FromValidation(errors, () => (
                nameResult.Value,
                seoTitleResult.Value,
                metaDescriptionResult.Value,
                slugResult.Value,
                priceResult.Value,
                descriptionResult.Value
            ));
    }
}
```

**Assembler Rules**:
- `internal static` extension class
- Extension method on the command type
- Returns `Result<(...)>` tuple with all value objects
- Collect all errors before returning (don't short-circuit)
- Use `Result.FromValidation()` to return success or aggregated errors

### 4. Command Validator

**File**: `{UseCase}CommandValidator.cs`

```csharp
// Example: CreateProduct/CreateProductCommandValidator.cs
public sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValidProductName();

        RuleFor(x => x.SeoTitle)
            .MustBeValidSeoTitle()
            .When(x => !string.IsNullOrWhiteSpace(x.SeoTitle));

        RuleFor(x => x.MetaDescription)
            .MustBeValidMetaDescription()
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        RuleFor(x => x.Slug)
            .MustBeValidSlug();

        RuleFor(x => x.CategoryId)
            .MustBeValidId();

        RuleFor(x => x.Price)
            .MustBeValidMoney();

        RuleFor(x => x.Description)
            .MustBeValidProductDescription()
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleForEach(x => x.Images)
            .ChildRules(image =>
            {
                image.RuleFor(i => i)
                    .MustHaveValidImage(i => i.Source, i => i.AltText);

                image.RuleFor(i => i.DisplayOrder)
                    .MustBeValidDisplayOrder();
            })
            .When(x => x.Images is not null);
    }
}
```

**Validator Rules**:
- Extend `AbstractValidator<TCommand>`
- Use validation extensions (see [Validation section](#validation-rules))
- Use `.When()` for conditional validation
- Use `RuleForEach()` for collections
- **Don't duplicate domain validation** - validators delegate to domain value object factories

**For Update Commands with Optional<T>**:

```csharp
// Example: UpdateProduct/UpdateProductCommandValidator.cs
public sealed class UpdateProductCommandValidator
    : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).MustBeValidId();
        RuleFor(x => x.RowVersion).MustBeValidRowVersion();

        // Use RuleForOptional for Optional<T> properties
        this.RuleForOptional(command => command.Name, name =>
        {
            name.MustBeValidProductName();
        });

        this.RuleForOptional(command => command.SeoTitle, seoTitle =>
        {
            seoTitle.MustBeValidSeoTitle();
        });

        // ... other optional fields
    }
}
```

### 5. Command Result DTO

**File**: `{UseCase}Result.cs`

**Pattern**: Return what's been changed in the subject aggregate, enabling optimistic client-side updates without re-fetching.

```csharp
// Creation: Return ID only
public sealed record CreateProductResult(long Id);

// Update: Return all modified fields + computed values (like RowVersion)
public sealed record UpdateProductResult(
    string Name,
    string? SeoTitle,
    string? MetaDescription,
    string Slug,
    long CategoryId,
    decimal Price,
    string? Description,
    IReadOnlyCollection<ProductImageDto>? Images,
    uint RowVersion
);

// Specialized operations: Return relevant changed data
public sealed record ChangeProductSlugResult(
    string Slug,
    uint RowVersion
);
```

**Result DTO Rules**:
- Use `sealed record` (immutable)
- **Create operations**: Return `Id` only
- **Update operations**: Return all fields that were modified + `RowVersion`
- **Specialized operations**: Return what changed (e.g., slug change returns slug + row version)
- Purpose: Allow client to optimistically update without re-fetching the entire entity

### 6. Transaction Management

**Default Behavior**: All commands are automatically wrapped in transactions via `TransactionalCommandBehavior`:
- Isolation Level: `READ COMMITTED`
- Lock Timeout: None (waits indefinitely)
- Statement Timeout: None

**Custom Transaction Settings**:

Implement `IWithTransactionOverrides` when you need:
1. **Stricter isolation** for hierarchical data operations (e.g., category reparenting)
2. **Custom isolation** for multiple aggregate updates

**Example**:

```csharp
// Example: ReparentCategory/ReparentCategoryCommand.cs
public sealed record ReparentCategoryCommand(
    long CategoryId,
    long? NewParentCategoryId,
    uint RowVersion
) : ICommand<ReparentCategoryResult>, IWithTransactionOverrides
{
    public TransactionSettings TransactionSettings => new()
    {
        Isolation = IsolationLevel.Serializable,  // Prevent phantom reads in hierarchy
        LockTimeout = TimeSpan.FromSeconds(2),    // Bonus: fail fast on contention
        StatementTimeout = TimeSpan.FromSeconds(30) // Bonus: prevent hanging
    };
}
```

**When to use custom transaction settings**:
- **Hierarchical data operations** - Use `Serializable` isolation to prevent phantom reads
- **Multiple aggregate updates** - Consider stricter isolation to ensure consistency
- **Lock/Statement timeouts** - Bonus optimization, but focus on isolation level first

---

## Query Implementation

### 1. Query DTO

**File**: `{UseCase}Query.cs`

```csharp
// Example: GetProductById/GetProductByIdQuery.cs
public sealed record GetProductByIdQuery(long ProductId)
    : IQuery<GetProductByIdResult>;
```

**Rules**:
- Use `sealed record` with primary constructor
- Implement `IQuery<TResult>`
- Simple, focused query parameters

### 2. Query Handler

**File**: `{UseCase}QueryHandler.cs`

```csharp
// Example: GetProductById/GetProductByIdQueryHandler.cs
internal sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProductByIdQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetProductByIdResult>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                p.id AS Id,
                p.name AS Name,
                p.seo_title AS SeoTitle,
                p.meta_description AS MetaDescription,
                p.slug AS Slug,
                p.category_id AS CategoryId,
                p.price AS Price,
                p.description AS Description,
                p.created_at AS CreatedAt,
                p.updated_at AS UpdatedAt,
                p.xmin AS RowVersion
            FROM products p
            WHERE p.id = @ProductId
            """;

        var product = await connection.QuerySingleOrDefaultAsync<GetProductByIdResult>(
            sql,
            new { query.ProductId },
            cancellationToken);

        return product is not null
            ? Result<GetProductByIdResult>.Success(product)
            : Result<GetProductByIdResult>.Failure(
                new ProductErrors.NotFoundById(query.ProductId));
    }
}
```

**Query Handler Rules**:
- Mark as `internal sealed class`
- Implement `IQueryHandler<TQuery, TResult>`
- **Only dependency**: `IDbConnectionFactory`
- Use **Dapper** for queries (preferred)
- Write **raw SQL** for explicit control and performance
- No transactions (queries are read-only)
- Return `Result<T>` with appropriate not-found errors

**Dapper vs EF Core**:
- **Default**: Use Dapper with raw SQL
- **EF Core acceptable if**: There's a compelling reason (complex projections, etc.)
- **Reasoning**: Explicit SQL control, better read performance, clear separation from write model

### 3. Query Result DTO

**File**: `{UseCase}Result.cs`

```csharp
// Example: GetProductById/GetProductByIdResult.cs
public sealed class GetProductByIdResult
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string Slug { get; set; } = null!;
    public long CategoryId { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}
```

**Query Result Rules**:
- Use **mutable class** (preferred convention for Dapper)
- Properties with `get; set;`
- Non-nullable reference types get `= null!` or default initialization
- **Why mutable**: Dapper convention, though records with `init` also work

### 4. Query Validator

**File**: `{UseCase}QueryValidator.cs`

```csharp
// Example: GetProductById/GetProductByIdQueryValidator.cs
public sealed class GetProductByIdQueryValidator
    : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .MustBeValidId();
    }
}
```

**Rules**: Same validation patterns as commands, but typically simpler (e.g., just ID validation).

---

## Validation Rules

### Validation Extension Locations

Validation extensions follow a **three-tier pattern** based on what's being validated:

| What's Being Validated | Location | Example |
|------------------------|----------|---------|
| **Value Objects** | `/Abstractions/Validation/{ValueObject}ValidationExtensions.cs` | `ProductNameValidationExtensions.cs`, `SlugValidationExtensions.cs` |
| **Entity Input (non-VO)** | `/Abstractions/Validation/{Entity}ValidationExtensions.cs` | `EntityValidationExtensions.cs` (for ID, RowVersion) |
| **Use-Case-Specific Rules** | `/{Feature}/{UseCase}/{UseCase}ValidationExtensions.cs` | Rules like "max 20 image operations" stay in the use case folder |

### Validation Extension Pattern

Validation extensions **delegate to domain value object factories** to preserve domain rules:

```csharp
// File: Abstractions/Validation/ProductNameValidationExtensions.cs
internal static class ProductNameValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, string> MustBeValidProductName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            // Delegate to domain value object factory
            var result = ProductName.Create(value);

            if (result.IsSuccess)
                return;

            // Map domain errors to FluentValidation failures
            switch (result.Error)
            {
                case ProductNameErrors.Required e:
                    context.AddNameRequiredFailure(e, value);
                    break;

                case ProductNameErrors.TooLong e:
                    context.AddNameTooLongFailure(e, value);
                    break;
            }
        });
    }

    private static void AddNameRequiredFailure(
        this CustomContext context,
        ProductNameErrors.Required error,
        string attemptedValue)
    {
        context.AddFailure(new ValidationFailure
        {
            PropertyName = context.PropertyPath,
            ErrorMessage = error.Message,
            ErrorCode = error.Code,
            AttemptedValue = attemptedValue
        });
    }

    // ... other helper methods
}
```

**Key Points**:
- Extensions are `internal static` classes
- Use `.Custom()` to call domain value object factories
- Map domain errors to `ValidationFailure`
- Preserve domain error codes and messages
- **Never duplicate domain validation logic** - always delegate to domain

### Optional<T> Validation

For PATCH operations using `Optional<T>`:

```csharp
// Use RuleForOptional to only validate if the Optional<T> is set
this.RuleForOptional(command => command.Name, name =>
{
    name.MustBeValidProductName();
});
```

---

## Error Handling

### Error Organization

Errors are organized in **two layers**:

#### 1. Domain Layer Errors
**Location**: `src/core/ForeverBloom.Domain/Catalog/Products/Product.cs` (inside entity file)

**Purpose**: Entity invariant violations

```csharp
// Inside Product.cs
public static class ProductErrors
{
    public sealed record CategoryIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Product.CategoryIdInvalid";
        public string Message => $"Category ID '{AttemptedId}' is invalid.";
    }

    public sealed record NoPrimaryImage : IError
    {
        public string Code => "Product.NoPrimaryImage";
        public string Message => "Product must have exactly one primary image when images are provided.";
    }

    public sealed record MultiplePrimaryImages(int[] PrimaryIndices) : IError
    {
        public string Code => "Product.MultiplePrimaryImages";
        public string Message => $"Product cannot have multiple primary images. Found at indices: {string.Join(", ", PrimaryIndices)}.";
    }
}
```

#### 2. Application Layer Errors
**Location**: `src/core/ForeverBloom.Application/Products/ProductErrors.cs`

**Purpose**: Use case-specific errors (not found, conflicts, etc.)

```csharp
// File: ForeverBloom.Application/Products/ProductErrors.cs
public static class ProductErrors
{
    public sealed record NotFound : IError
    {
        public string Code => "Product.NotFound";
        public string Message => "Product was not found.";
    }

    public sealed record NotFoundById(long AttemptedId) : NotFound;

    public sealed record NotFoundBySlug(string AttemptedSlug) : NotFound;

    public sealed record SlugNotAvailable(string Slug) : IError
    {
        public string Code => "Product.SlugNotAvailable";
        public string Message => $"Slug '{Slug}' is not available.";
    }

    public sealed record CategoryNotFound(long CategoryId) : IError
    {
        public string Code => "Product.CategoryNotFound";
        public string Message => $"Category with ID '{CategoryId}' was not found.";
    }
}
```

### Decision Criteria

| Error Type | Layer | Examples |
|------------|-------|----------|
| **Entity invariant violations** | Domain | Invalid state, business rule violations within entity |
| **Not found** | Application | Entity/aggregate not found by ID/slug |
| **Conflicts** | Application | Slug unavailable, uniqueness violations |
| **External validation** | Application | Related entity doesn't exist (category not found) |

### Result<T> Pattern

All use cases return `Result<T>`:

```csharp
// Failure
return Result<CreateProductResult>.Failure(
    new ProductErrors.SlugNotAvailable(slug));

// Success
return Result<CreateProductResult>.Success(
    new CreateProductResult(product.Id));
```

**Key Points**:
- Railway-Oriented Programming pattern
- No exceptions for business logic failures
- Errors are strongly-typed records
- Error codes follow convention: `{Feature}.{ErrorType}`

---

## Domain Interaction

### Entity Methods vs Domain Services

**Use Entity Methods** when:
- Simple entity creation, updates, state transitions
- Logic operates on a single entity's state
- No coordination across multiple entities needed

**Examples**:

```csharp
// Product.cs - Factory method with validation
public static Result<Product> Create(
    ProductName name,
    SeoTitle? seoTitle,
    HtmlFragment? fullDescription,
    MetaDescription? metaDescription,
    Slug slug,
    long categoryId,
    Money? price,
    int displayOrder,
    bool isFeatured,
    ProductAvailabilityStatus availabilityStatus,
    DateTimeOffset timestamp,
    ICollection<ProductImage>? images = null)
{
    var errors = new List<IError>();

    // Validate single-entity invariants
    if (categoryId <= 0)
        errors.Add(new ProductErrors.CategoryIdInvalid(categoryId));

    if (images is not null && images.Count > 0)
    {
        var imageValidationResult = ValidateImageCollection(images);
        if (imageValidationResult.IsFailure)
            errors.Add(imageValidationResult.Error);
    }

    return Result<Product>.FromValidation(
        errors,
        () => new Product(name, seoTitle, fullDescription, metaDescription,
                         slug, categoryId, price, displayOrder, isFeatured,
                         availabilityStatus, images, timestamp));
}

// Product.cs - Update method with Optional<T> for PATCH semantics
public Result<bool> Update(
    Optional<ProductName> name,
    Optional<SeoTitle?> seoTitle,
    Optional<HtmlFragment?> fullDescription,
    Optional<MetaDescription?> metaDescription,
    Optional<long> categoryId,
    Optional<Money?> price,
    Optional<int> displayOrder,
    Optional<bool> isFeatured,
    Optional<ProductAvailabilityStatus> availability,
    Optional<PublishStatus> publishStatus,
    DateTimeOffset timestamp)
{
    // No-op detection
    var hasChanges = (name.IsSet && Name != name.Value) ||
                     (seoTitle.IsSet && SeoTitle != seoTitle.Value) ||
                     /* ... other fields ... */;

    if (!hasChanges)
        return Result<bool>.Success(false); // No changes, don't persist

    var errors = new List<IError>();

    // Validate state transitions
    if (publishStatus.IsSet && PublishStatus != publishStatus.Value)
    {
        if (!PublishStatus.CanTransitionTo(publishStatus.Value))
            errors.Add(new ProductErrors.PublishStatusTransitionNotAllowed(
                PublishStatus, publishStatus.Value));
    }

    if (errors.Count > 0)
        return Result<bool>.Failure(new CompositeError(errors));

    // Apply changes
    if (name.IsSet) Name = name.Value;
    if (seoTitle.IsSet) SeoTitle = seoTitle.Value;
    // ... other fields

    UpdatedAt = timestamp;
    return Result<bool>.Success(true); // Changed, caller should persist
}

// Category.cs - Specialized operation on single entity
public Result<bool> ChangeSlug(Slug newSlug, DateTimeOffset timestamp)
{
    if (CurrentSlug == newSlug)
        return Result<bool>.Success(false); // No-op

    var newPathResult = Path.WithSlug(newSlug);
    if (newPathResult.IsFailure)
        return Result<bool>.Failure(newPathResult.Error);

    CurrentSlug = newSlug;
    Path = newPathResult.Value;
    UpdatedAt = timestamp;

    return Result<bool>.Success(true);
}
```

**Use Domain Services** when:
- Logic requires **coordination across multiple entities or aggregates**
- **Complex algorithms** involving multiple entities (e.g., hierarchy operations)
- Operations that affect an entity and its related entities simultaneously

**Key Point**: Domain services both validate AND execute the operation. They don't just validate - they orchestrate the entire multi-entity operation.

**Example - CategoryHierarchyService**:

```csharp
// Domain Service: CategoryHierarchyService.cs
public sealed class CategoryHierarchyService
{
    /// <summary>
    /// Reparents a category to a new parent and rebases all its descendants.
    /// This method validates AND performs the operation atomically.
    /// </summary>
    public Result<bool> ReparentCategoryAndRebaseDescendants(
        Category category,
        long? newParentId,
        HierarchicalPath? newParentPath,
        IReadOnlyList<Category> descendants,
        DateTimeOffset timestamp)
    {
        var oldBase = category.Path;

        // Domain validation: prevent circular dependencies
        if (newParentPath is not null &&
            newParentPath.IsDescendantOf(oldBase, includeSelf: true))
        {
            return Result<bool>.Failure(
                new CategoryErrors.CircularDependency(category.Id, newParentId!.Value));
        }

        // Execute the reparent on the subject
        var reparentResult = category.Reparent(newParentId, newParentPath, timestamp);
        if (reparentResult.IsFailure)
            return Result<bool>.Failure(reparentResult.Error);

        if (reparentResult.Value is false)
            return Result<bool>.Success(false); // No-op

        var newBase = category.Path;

        // Rebase all descendants to reflect the new hierarchy
        return RebaseCategories(oldBase, newBase, descendants, timestamp);
    }

    /// <summary>
    /// Changes a category's slug and rebases all its descendants.
    /// This method validates AND performs the operation atomically.
    /// </summary>
    public Result<bool> ChangeCategorySlugAndRebaseDescendants(
        Category category,
        Slug newSlug,
        IReadOnlyList<Category> descendants,
        DateTimeOffset timestamp)
    {
        if (category.CurrentSlug == newSlug)
            return Result<bool>.Success(false); // No-op

        var oldBase = category.Path;

        // Execute slug change on subject
        var changeSlugResult = category.ChangeSlug(newSlug, timestamp);
        if (changeSlugResult.IsFailure)
            return Result<bool>.Failure(changeSlugResult.Error);

        if (changeSlugResult.Value is false)
            return Result<bool>.Success(false);

        var newBase = category.Path;

        // Rebase all descendants
        return RebaseCategories(oldBase, newBase, descendants, timestamp);
    }

    private Result<bool> RebaseCategories(
        HierarchicalPath oldBase,
        HierarchicalPath newBase,
        IReadOnlyList<Category> categories,
        DateTimeOffset timestamp)
    {
        if (oldBase.Value == newBase.Value || categories.Count == 0)
            return Result<bool>.Success(false);

        // Validate max depth for all descendants
        var depthChange = newBase.Depth - oldBase.Depth;
        var currentMaxDepth = categories.Max(d => d.Path.Depth);
        var newMaxDepth = currentMaxDepth + depthChange;

        if (newMaxDepth > HierarchicalPath.MaxDepth)
            return Result<bool>.Failure(
                new HierarchicalPathErrors.TooDeep(currentMaxDepth + depthChange));

        // Rebase all descendants
        foreach (var descendant in categories)
        {
            var result = descendant.RebasePath(oldBase, newBase, timestamp);
            if (result.IsFailure)
                return Result<bool>.Failure(result.Error);
        }

        return Result<bool>.Success(true);
    }
}
```

**Usage in Handler - Application Layer Orchestration**:

```csharp
// ReparentCategoryCommandHandler.cs
internal sealed class ReparentCategoryCommandHandler
    : ICommandHandler<ReparentCategoryCommand, ReparentCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryHierarchyService _hierarchyService;
    private readonly ITimeProvider _timeProvider;

    public async Task<Result<ReparentCategoryResult>> Handle(
        ReparentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Handler validates application-layer concerns
        var category = await _categoryRepository.GetByIdAsync(
            command.CategoryId, cancellationToken);
        if (category is null)
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));

        // Check concurrency
        if (category.RowVersion != command.RowVersion)
            return Result<ReparentCategoryResult>.Failure(
                new ApplicationErrors.ConcurrencyConflict());

        // Validate new parent exists (if provided)
        HierarchicalPath? newParentPath = null;
        if (command.NewParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(
                command.NewParentCategoryId.Value, cancellationToken);
            if (parent is null)
                return Result<ReparentCategoryResult>.Failure(
                    new CategoryErrors.ParentNotFound(command.NewParentCategoryId.Value));

            newParentPath = parent.Path;
        }

        // Validate business rule: name uniqueness within parent
        var nameExists = await _categoryRepository.NameExistsWithinParentAsync(
            category.Name, command.NewParentCategoryId, cancellationToken);
        if (nameExists)
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.NameNotUniqueWithinParent(
                    category.Name.Value, command.NewParentCategoryId));

        // Fetch descendants
        var descendants = await _categoryRepository.GetDescendantsAsync(
            category.Path, category.Id, MaxDescendantsToMove, cancellationToken);

        if (descendants.Count > MaxDescendantsToMove)
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.TooManyDescendantsToMove(
                    command.CategoryId, MaxDescendantsToMove));

        // 2. Domain service handles the coordinated operation (validation + execution)
        var reparentResult = _hierarchyService.ReparentCategoryAndRebaseDescendants(
            category,
            command.NewParentCategoryId,
            newParentPath,
            descendants,
            _timeProvider.UtcNow);

        if (reparentResult.IsFailure)
            return Result<ReparentCategoryResult>.Failure(reparentResult.Error);

        // 3. Persist if changes were made
        if (reparentResult.Value)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReparentCategoryResult>.Success(
            new ReparentCategoryResult(
                category.Path.Value,
                category.ParentCategoryId,
                category.UpdatedAt,
                category.RowVersion));
    }
}
```

**Key Patterns**:

1. **Entity Methods**:
   - Return `Result<T>` or `Result<bool>` (bool indicates whether changes were made)
   - Validate single-entity invariants
   - Perform state transitions on the entity
   - No-op detection (return false if no changes)

2. **Domain Services**:
   - Orchestrate multi-entity operations
   - Both validate AND execute the operation
   - Work with collections of related entities (subject + descendants)
   - Delegate to entity methods for individual entity operations
   - Return `Result<bool>` indicating whether changes were made

3. **Handler Responsibilities**:
   - **Application-layer validation**: Entity existence, concurrency, business rules requiring repositories
   - **Data fetching**: Load aggregates and related entities from repositories
   - **Domain service invocation**: Delegate coordinated operations to domain services
   - **Persistence**: Call `SaveChangesAsync()` if domain operation returned true
   - **Result mapping**: Convert domain results to application result DTOs

---

## Handler Dependencies

### Command Handler Dependencies

Inject as needed:

| Dependency | Purpose | Required? |
|------------|---------|-----------|
| `IUnitOfWork` | Persistence and transaction management | ✅ Always |
| `I{Entity}Repository` | Access aggregate roots being modified | ✅ Usually |
| Domain services | Complex business logic coordination | ⚠️ As needed |
| `ITimeProvider` | Timestamps for entity operations | ✅ Always use (never `DateTime.UtcNow`) |
| Infrastructure services | Slug registry, email, etc. | ⚠️ As needed |

**Example**:
```csharp
internal sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISlugRegistrationService slugRegistrationService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _slugRegistrationService = slugRegistrationService;
        _timeProvider = timeProvider;
    }

    // ...
}
```

### Query Handler Dependencies

**Only inject**: `IDbConnectionFactory`

```csharp
internal sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProductByIdQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ...
}
```

**No repositories, no UnitOfWork, no domain services in queries** - queries use Dapper for read-optimized data access.

---

## Common Patterns

### Result<T> - Railway-Oriented Programming

```csharp
// Check and return failure early
var slugAvailable = await _slugRegistrationService
    .IsSlugAvailableAsync(slug, cancellationToken);
if (!slugAvailable)
    return Result<CreateProductResult>.Failure(
        new ProductErrors.SlugNotAvailable(slug));

// Success path
return Result<CreateProductResult>.Success(
    new CreateProductResult(product.Id));
```

### Optional<T> - PATCH Operations

For partial updates (PATCH endpoints):

```csharp
// Command with optional fields
public sealed record UpdateProductCommand(
    long Id,
    uint RowVersion,
    Optional<string> Name,
    Optional<string?> SeoTitle,
    Optional<string?> MetaDescription
    // ...
) : ICommand<UpdateProductResult>;

// Validation - only validate if set
this.RuleForOptional(command => command.Name, name =>
{
    name.MustBeValidProductName();
});

// Usage in handler
if (command.Name.IsSet)
{
    var nameResult = ProductName.Create(command.Name.Value);
    if (nameResult.IsFailure)
        errors.Add(nameResult.Error);
}
```

### Nested DTOs

Always define nested DTOs in the same file as the parent:

```csharp
// Parent command
public sealed record CreateProductCommand(
    string Name,
    IReadOnlyCollection<CreateProductCommandImage>? Images
) : ICommand<CreateProductResult>;

// Nested DTO - same file
public sealed record CreateProductCommandImage(
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder
);
```

---

## Testing

**IMPORTANT**: Automated testing for the new CA architecture is **not yet defined**.

**Do not create automated tests** until testing strategy is established.

When testing strategy is defined in the future, it will be documented separately.

---

## Registration & Dependency Injection

**Good news**: Use cases are auto-registered via assembly scanning.

**Location**: `src/core/ForeverBloom.Application/DependencyInjection.cs`

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    // Auto-register all validators
    services.AddValidatorsFromAssembly(ApplicationAssembly);

    // Auto-register all handlers + pipeline behaviors
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(ApplicationAssembly);

        // Pipeline order matters!
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));           // 1st: Validation
        cfg.AddOpenBehavior(typeof(TransactionalCommandBehavior<,>)); // 2nd: Transactions
    });

    return services;
}
```

**Key Points**:
- No manual registration needed for handlers/validators
- Convention-based discovery
- Pipeline behaviors execute in order: Validation → Transactions → Handler
- Validation failures short-circuit before handler execution
- Transactions wrap command handlers automatically

---

## MediatR Pipeline

Request flow through MediatR pipeline:

```
Request → ValidationBehavior → TransactionalCommandBehavior → Handler → Response
```

### 1. ValidationBehavior
- Runs all `AbstractValidator<T>` validators
- Returns `ValidationError` with all failures if validation fails
- Short-circuits before handler if validation fails

### 2. TransactionalCommandBehavior
- **Only runs for commands** (not queries)
- Wraps handler in database transaction
- Applies transaction settings (isolation, timeouts)
- Catches `DbUpdateConcurrencyException` → returns `ConcurrencyConflict` error
- Auto-rollback on failure

### 3. Handler
- Your use case handler implementation
- Executes business logic
- Returns `Result<T>`

---

## Code Examples

Refer to these existing use cases as canonical examples:

### Commands
- **Create**: `src/core/ForeverBloom.Application/Products/Commands/CreateProduct/`
- **Update**: `src/core/ForeverBloom.Application/Products/Commands/UpdateProduct/`
- **Specialized Operation**: `src/core/ForeverBloom.Application/Products/Commands/ChangeProductSlug/`
- **Complex Transaction**: `src/core/ForeverBloom.Application/Categories/Commands/ReparentCategory/`

### Queries
- **Get by ID**: `src/core/ForeverBloom.Application/Products/Queries/GetProductById/`
- **Get by Slug**: `src/core/ForeverBloom.Application/Products/Queries/GetProductBySlug/`

---

## Summary: Key Principles

1. **One folder per use case** - Never group multiple use cases
2. **CQRS separation** - Commands use EF Core + repositories, queries use Dapper
3. **Value Object Assemblers** - Use for multi-value-object commands (typically Create/Update)
4. **Domain-driven validation** - Validators delegate to domain value object factories
5. **Result<T> everywhere** - No exceptions for business logic
6. **Immutability** - Commands and results use `record`; queries use mutable classes
7. **Clear error boundaries** - Domain errors for invariants, application errors for use cases
8. **Entity methods vs services** - Services for multi-entity coordination and complex algorithms
9. **Transaction awareness** - Override settings for hierarchical operations
10. **Testability** - Use `ITimeProvider`, clear dependencies, no static/global state

---

## Checklist Before Submitting a Use Case

- [ ] Follows folder structure convention
- [ ] All files follow naming conventions
- [ ] Command/Query implements correct interface
- [ ] Handler returns `Result<T>`
- [ ] Validator uses validation extensions (delegates to domain)
- [ ] Value Object Assembler created if needed
- [ ] Errors added to appropriate layer (domain vs application)
- [ ] Handler dependencies are appropriate (commands vs queries)
- [ ] Transaction settings customized if needed
- [ ] Result DTO returns what changed (for optimistic updates)
- [ ] Uses `ITimeProvider` instead of `DateTime.UtcNow`
- [ ] Nested DTOs defined in same file as parent
- [ ] No automated tests created (not yet defined)

---

**This guide is the single source of truth for use case implementation. Refer to it for every new use case to maintain consistency across the codebase.**
