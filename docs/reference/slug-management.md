# Slug Management Design

This document describes how the application manages URL slugs across their lifecycle and how those slugs drive routing. It outlines the storage model, the endpoint flows that change or read slugs, and the guarantees the system provides.

## 1. Core Concept

A single, centralized mechanism governs slugs for all content types (Products and Categories) and serves as the source of truth for URL routing. Once a slug is used, it is permanently reserved and can be reused only if it is explicitly freed during a permanent deletion.

## 2. Key Components

The `SlugRegistry` table is the ledger of every slug and enforces global uniqueness. For fast reads, each parent entity also stores its current slug in a denormalized `CurrentSlug` column.

`SlugRegistry` schema (polymorphic association to link entities):

| Column Name | Data Type | Constraints / Notes |
| :--- | :--- | :--- |
| `Id` | `int` | Primary Key |
| `Slug` | `varchar` | **`UNIQUE`**. Enforces global uniqueness. |
| `EntityType`| `varchar` | Discriminator (e.g., "Product", "Category", "Course"). |
| `EntityId` | `int` | ID of the parent entity. **No foreign key constraint.** |
| `IsActive` | `boolean` | Marks the entity’s current slug. |

Parent entity columns (denormalized for performance):

| Table | Column Name | Notes |
| :--- | :--- | :--- |
| `Products` | `CurrentSlug` | The active slug; updated in the same transaction as `SlugRegistry`. |
| `Categories` | `CurrentSlug` | The active slug; updated in the same transaction as `SlugRegistry`. |

Guarantees provided by the model:
1. Global uniqueness via a `UNIQUE` constraint on `Slug`.
2. Exactly one active slug per entity, enforced by a composite `UNIQUE` constraint on `(EntityType, EntityId, IsActive)` where `IsActive = true`.

Referential integrity is enforced at the application level inside transactions, which allows the registry to stay globally unique across all entity types.

Example `SlugRegistry` rows:

| Id | Slug | EntityType | EntityId | IsActive |
| :--| :--- | :--- | :--- | :--- |
| 1 | `bouquets` | `Category` | 1 | true |
| 2 | `diy-kits` | `Category` | 2 | true |
| 3 | `the-aurora-kit` | `Product` | 101 | true |
| 4 | `aurora-flower-kit` | `Product` | 101 | false |
| 5 | `spring-collection` | `Course` | 42 | true |

## 3. API Endpoint Workflows

### 3.1. CREATE Content

On create, the API inserts an active entry into `SlugRegistry` in the same transaction as the parent entity insert. The database constraint rejects duplicate slugs.

### 3.2. UPDATE Slug

Within a single transaction the API:
1. Marks the current active slug as inactive.
2. Reactivates a historical entry if the same entity used the slug before; otherwise creates a new entry.
3. Updates the parent entity’s `CurrentSlug`.

### 3.3. DELETE Content

- **Soft delete:** No changes to `SlugRegistry`; the slug stays reserved.
- **Hard delete:** All historical entries are removed so the slug can be reused.

### 3.4. FETCH by Slug

Resolution returns one of the following:
1. **200 OK:** The slug is active and the entity is public (`IsActive = true AND DeletedAtUtc IS NULL`).
2. **301 Redirect:** The slug is historical; redirect to the current active slug.
3. **404 Not Found:** The slug does not exist or the entity is archived/inactive.

## 4. Design Rationale

**Single polymorphic table approach**

- Guarantees uniqueness: the `UNIQUE` index on `Slug` prevents duplicates atomically.
- Extensible: new entity types don’t require schema changes—add a new `EntityType` value.
- Resilient to failures: orphaned entries, if they occur, simply resolve to 404 and don’t affect users.

**Rejected alternative (separate tables per entity)**
- Application‑level uniqueness checks are fragile and prone to races.
- Duplicated slugs risk breaking URLs, SEO, and marketing campaigns.
- Higher maintenance: adding entities forces changes across validation and checks.
