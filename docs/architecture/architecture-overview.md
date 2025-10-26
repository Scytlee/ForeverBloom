# System Architecture & Technology Stack

## Architectural Approach

ForeverBloom follows a pragmatic hybrid architecture: a two-layer backend organized into vertical slices, with shared libraries for contracts and domain primitives. The approach keeps business logic close to each endpoint while isolating persistence concerns, which balances development speed with maintainability. See [ADR‑002](../adr/002-pragmatic-hybrid-architecture.md) for the rationale.

### Architecture Layers

- API layer ([`src/backend/ForeverBloom.Api`](../../src/backend/ForeverBloom.Api)) uses ASP.NET Core Minimal APIs ([ADR‑003](../adr/003-choose-minimal-apis.md)). Endpoints are grouped by feature in one‑folder‑per‑endpoint slices, and handlers encapsulate the necessary business rules. This layer depends on the persistence layer.
- Persistence layer ([`src/backend/ForeverBloom.Persistence`](../../src/backend/ForeverBloom.Persistence)) is built on EF Core 9 with PostgreSQL. Entities are intentionally anemic; write operations go through repositories, and the DbContext and migrations live here.
- Shared libraries contain cross‑cutting primitives: `ForeverBloom.Api.Contracts` holds request/response DTOs, and `ForeverBloom.Domain.Shared` provides validation constants and enums.

## Technology Stack

### Core Framework

The code targets .NET 9. The backend is implemented with ASP.NET Core Minimal APIs ([ADR‑003](../adr/003-choose-minimal-apis.md)), and the frontend is a server‑rendered Razor Pages site that acts as a BFF. Key dependencies include:
- Entity Framework Core 9
- Npgsql
- FluentValidation

### Database

The system uses PostgreSQL 17 ([ADR‑001](../adr/001-use-postgresql-over-sql-server.md)). Database design emphasizes correctness and observability:
- ltree extension for hierarchical category queries
- xmin row versioning for optimistic concurrency with no extra storage
- `business` schema to isolate application tables
- microsecond timestamp precision across all audit columns

### Development Infrastructure

Local development is orchestrated with .NET Aspire, which starts PostgreSQL, waits for a one‑shot DatabaseManager to initialize and migrate the database, and then brings up the backend and frontend. Test infrastructure relies on Testcontainers for real PostgreSQL instances, and the DatabaseManager enables ephemeral local databases ([ADR‑004](../adr/004-ephemeral-local-databases-with-databasemanager.md)).

## Solution Structure

Key projects:
- Backend: Minimal APIs (API) and EF Core + PostgreSQL (Persistence)
- Frontend: Razor Pages with an internal ApiClient (BFF pattern)
- Shared: Api.Contracts (DTOs) and Domain.Shared (validation constants and enums)
- Infrastructure: Aspire orchestration and the DatabaseManager tool
- Testing: four projects covering Unit and Integration tests for API and Persistence

## API Design Patterns

### Minimal APIs & Vertical Slices

Endpoints are self‑contained. Each slice keeps the handler, validator, and (when needed) a query provider together. This organization shortens the path from request to data and makes navigation straightforward.

### Request/Response Contracts & Typed Results

Contracts are immutable records (init‑only) to stabilize the API surface. Responses use envelopes rather than bare arrays or primitives, and endpoint handlers return typed results so the compiler enforces declared return shapes.

## CQRS Pattern: Command‑Query Separation

The backend separates write operations from reads ([ADR‑005](../adr/005-extract-endpoint-specific-queries-into-query-providers.md)).

### Commands: Repository Pattern

Repositories are dedicated to writes. Command endpoints call repositories and persist via the Unit of Work.

### Queries: Endpoint‑Specific Providers

Reads are implemented by endpoint‑specific query providers that access the DbContext directly. This avoids generic repository bloat and allows tuned projections and tracking choices per endpoint.

### Unit of Work Pattern

Transaction management is applied with an endpoint filter ([ADR‑006](../adr/006-unit-of-work-as-endpoint-filter.md)). A transaction begins before the handler, commits on 2xx responses, and rolls back on non‑2xx results or exceptions. The filter is attached explicitly on mutating endpoints.

## Cross‑Cutting Concerns

### Validation

FluentValidation validators use shared domain constants from `ForeverBloom.Domain.Shared`. A validation endpoint filter runs before handlers and short‑circuits on errors, returning RFC 9110‑style problem details. See the [Domain Model](../reference/domain-model.md) for the validation constants.

### Authentication & Authorization

Phase 1 uses API key authentication with two scopes—frontend and admin—applied via authorization policies ([ADR‑007](../adr/007-api-key-authentication-for-phase-1.md)). This fits the BFF setup: keys are added server‑side in the frontend and never exposed to the browser. Implementation details are covered in [Authentication](../reference/authentication.md).

### Error Handling

Problem detail responses are enriched with request context (requestId, traceId, instance) by a dedicated endpoint filter. Responses intentionally omit stack traces and internal paths to avoid leaking implementation details.

## Deployment & CI/CD

Services are packaged into Docker images (Alpine‑based .NET 9 runtime) and deployed on a VPS with Docker Compose. Traffic is served through an Nginx reverse proxy with Let’s Encrypt TLS, managed outside this repository. GitHub Actions runs on a self‑hosted runner: Integration deploys are automated on `main`, and Staging/Production promotions are manual.

## Testing Strategy

Testing favors integration coverage for API contracts, with unit tests focused on the core business logic. Infrastructure includes Testcontainers, a template‑database pattern for fast isolation, and WebApplicationFactory for end‑to‑end HTTP tests. See the [Testing](../testing/testing.md) document for metrics and patterns.

