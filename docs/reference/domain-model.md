# Domain Model

ForeverBloom uses deliberately simple (anemic) entities. Cross-cutting behavior such as timestamps, optimistic concurrency, and soft deletion is applied through marker interfaces, while business rules live in endpoint handlers and validators. This keeps persistence models predictable and pushes workflows and validations to the API layer.

## Entity Overview

| Entity | Purpose | Key Relationships |
|--------|---------|-------------------|
| **Category** | Organizes products in a hierarchy | Self-referencing tree; owns many Products |
| **Product** | Catalog item (e.g., dried flowers, kits, vouchers) | Belongs to Category; owns many ProductImages |
| **ProductImage** | Image attached to a product with ordering | Belongs to Product; ordered by DisplayOrder |
| **SlugRegistryEntry** | Central ledger for URL slugs and their history | Tracks current and historical slugs for SEO and redirects |

## Cross-Cutting Interfaces

### IAuditable

Categories and products record `CreatedAt` and `UpdatedAt` automatically during `SaveChanges`. Both also expose a `RowVersion` mapped to PostgreSQL’s native `xmin` column, which EF Core treats as a concurrency token. When two updates race, EF Core raises `DbUpdateConcurrencyException`, allowing the API to return a conflict response instead of silently overwriting data.

### ISoftDeleteable

Categories and products support soft deletion via a nullable `DeletedAt`. A global query filter includes only records where `DeletedAt` is null, so archived rows are excluded by default. When needed (for admin or recovery flows), queries can opt into archived data using `.IgnoreQueryFilters()`.

ProductImage does not implement either interface.

## Key Entity Details

### Category

Categories form a tree using PostgreSQL’s ltree type. The `Path` column encodes the full route (for example, `obrazy-botaniczne.obrazy-plaskie`) and enables efficient ancestor/descendant queries without recursive CTEs. The model is self‑referencing through `ParentCategoryId`/`ParentCategory` and exposes `ChildCategories` and `Products` collections. Two operational fields, `IsActive` and `DisplayOrder`, control visibility and ordering and are indexed for common admin and browse scenarios. Names are unique within the same parent, enforced via a unique index on `(Name, ParentCategoryId)`.

Hierarchy rules are enforced in the API: there is a maximum depth, and changes that would implicitly cascade (for example, moving a node with children or changing its slug) are blocked. When a leaf category changes its slug or parent, the API updates `Path` accordingly. These rules complement the ltree storage and ensure the tree remains consistent under write operations.

### Product

Products belong to a single category and carry both content and merchandising fields: `Name`, `SeoTitle`, `FullDescription`, and `MetaDescription`, along with a denormalized `CurrentSlug`. `Price` is nullable to support “made to order” or “inquire” scenarios. `IsFeatured` and `DisplayOrder` are available for presentation. Publishing is controlled by `PublishStatus` (Draft, Published, Unpublished). Availability is expressed separately and can be one of: Available, OutOfStock, MadeToOrder, InquireForPrice, Discontinued, or ComingSoon. Precision for prices is configured as NUMERIC(12,2).

Each product exposes an `Images` collection. Validations and indexes favor common queries such as browsing within a category by publish status and display order.

### ProductImage

Product images store `ImagePath`, `DisplayOrder`, `IsPrimary`, and optional `AltText`. The product relationship is required, and images are ordered per product. API validators ensure exactly one primary image and that display orders do not collide within a product’s gallery.

### SlugRegistryEntry

The slug registry is the source of truth for URLs. Every slug is globally unique across entity types, and each entity has at most one active slug at a time. Parent tables carry a denormalized `CurrentSlug` for fast lookup, and the API updates both the parent and the registry in the same unit of work. Historical entries remain for redirect handling, enabling 301s from old slugs to the current one.

See [Slug Management](slug-management.md) for workflow details and design rationale.

## Validation Strategy

Validation rules are centralized in `ForeverBloom.Domain.Shared` to keep limits and messages consistent across API validators, EF model configuration, and test data. Notable classes include `ProductValidation`, `CategoryValidation`, `ProductImageValidation`, and `SlugValidation` (with a shared slug regex and maximum length). Error codes are standardized so clients can localize validation feedback reliably.

## Database Schema

All business tables live in the `business` schema. Conventions are consistent across entities:

- `DateTimeOffset` properties map to `timestamp with time zone` with microsecond precision.
- String lengths are explicit and aligned with the shared validation constants.
- Delete behaviors match domain expectations: Category→Parent uses Restrict; Product→Category uses Restrict; ProductImage→Product uses Cascade.

Key constraints and indexes reinforce domain rules and performance characteristics:

- Global uniqueness of slugs in the registry, plus a partial unique index so only one active slug exists per entity.
- Category name uniqueness within the same parent.
- Indexed fields for common filters and sorts, such as activity flags and display ordering.

Together with ltree for category paths and the API’s write-time checks, the schema supports fast reads and safe, predictable updates.
