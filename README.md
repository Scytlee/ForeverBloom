# ForeverBloom

> **Production snapshot:** 2025-10-26

> There is an ongoing refactor of this system into a pragmatic Clean Architecture solution with rich domain. It also comes with an upgrade to .NET 10 and Aspire 13. The snapshot can be found on the `refactor/clean-architecture` branch.

Production snapshot of Phase 1 (catalog showcase) of a modern e-commerce platform built with .NET 9 that demonstrates pragmatic architecture, solid testing practices, and a production-minded DevOps setup. Live, running system is available at [foreverbloomstudio.pl](https://foreverbloomstudio.pl/).

Note: This is a portfolio/showcase project intended for evaluation of engineering skills. The source is published for viewing only. See the [LICENSE](LICENSE) for terms.

## Overview

ForeverBloom models an e-commerce site for a dried flower studio. The solution favors a clean, vertical-slice API and a simple frontend, backed by PostgreSQL and automated database setup. It focuses on maintainable patterns over ceremony: clear contracts, predictable persistence, and integration-heavy tests that reflect real behavior. Deployments are containerized and automated through GitHub Actions.

## Technology Stack

### Backend
- .NET 9 targeting ASP.NET Core minimal APIs
- Entity Framework Core 9 with PostgreSQL 17 (`ltree` for hierarchical data)
- .NET Aspire for local orchestration and OpenTelemetry plumbing

### Frontend
- ASP.NET Core Razor Pages for a server-rendered UI
- Bootstrap 5 for layout and components
- A typed `ApiClient` for calling the backend

### Testing
- xUnit for unit and integration tests
- Testcontainers for realistic database-backed tests
- FluentAssertions for expressive assertions
- NSubstitute for lightweight test doubles

### DevOps & Infrastructure
- Docker and Docker Compose for packaging and environments
- GitHub Actions with self-hosted runners for CI and promotions
- EF Core migrations applied via a dedicated DatabaseManager utility

## Architecture Highlights

### Pragmatic hybrid architecture

The solution uses two main layers and organizes code by feature. The API project hosts presentation and application logic in vertical slices, while the persistence project handles EF Core configuration and repositories for command paths alongside direct query providers. Shared projects hold domain contracts and invariants. The emphasis is on development velocity, vertical cohesion, and testability, with a CQRS‑lite split where it adds value.

See the [Architecture Overview](docs/architecture/architecture-overview.md) and [ADR‑002](docs/adr/002-pragmatic-hybrid-architecture.md) for rationale.

### Key Technical Features

Hierarchical Category System
- Uses PostgreSQL `ltree` for efficient tree operations and controlled recursion
- Stores materialized paths for simple reads

Slug Management System
- Central registry to enforce unique, SEO‑friendly slugs across entities
- Multi‑entity support (categories, products) with conflict handling

Advanced Querying
- Common infrastructure for filtering and sorting
- Offset‑based pagination with total counts

PATCH Operations
- `Optional<T>` semantics for partial updates and precise null handling
- Custom JSON converters and validation‑aware update flow

Authentication & Authorization
- API key authentication
- Frontend/Admin scopes enforced at endpoints

## Project Structure

```
ForeverBloom/
├── aspire/                          # .NET Aspire orchestration
│   ├── ForeverBloom.Aspire.AppHost/
│   └── ForeverBloom.Aspire.ServiceDefaults/
├── src/
│   ├── backend/
│   │   ├── ForeverBloom.Api/         # ASP.NET Core Web API
│   │   └── ForeverBloom.Persistence/ # EF Core + PostgreSQL
│   ├── frontend/
│   │   ├── ForeverBloom.ApiClient/   # Typed HTTP client
│   │   └── ForeverBloom.Frontend.RazorPages/
│   └── shared/
│       ├── ForeverBloom.Api.Contracts/  # DTOs
│       └── ForeverBloom.Domain.Shared/  # Domain types and validation
├── tests/
│   ├── backend/
│   │   ├── ForeverBloom.Api.Tests.Unit/
│   │   ├── ForeverBloom.Api.Tests.Integration/
│   │   ├── ForeverBloom.Persistence.Tests.Unit/
│   │   └── ForeverBloom.Persistence.Tests.Integration/
│   └── shared/
│       └── ForeverBloom.Testing.Common/
├── tools/
│   └── ForeverBloom.DatabaseManager/   # Migration/seed utility
├── deploy/                          # Dockerfiles and compose
├── docs/                            # Architecture, reference, testing, ADRs
└── .github/workflows/               # CI and promotion workflows
```

## Documentation

### Architecture (Current System State)
- [Architecture Overview](docs/architecture/architecture-overview.md)
- [Local Development](docs/architecture/local-development.md)

### Reference (Technical Guides)
- [Domain Model](docs/reference/domain-model.md)
- [Frontend Architecture](docs/reference/frontend-design-document.md)
- [Authentication](docs/reference/authentication.md)
- [Slug Management](docs/reference/slug-management.md)
- [Filtering, Sorting & Pagination](docs/reference/filtering-sorting-pagination.md)
- [URL Design](docs/reference/url-design-and-routing-strategy.md)

### Testing
- [Testing](docs/testing/testing.md)

### Architecture Decision Records (ADRs)
- [ADR‑001](docs/adr/001-use-postgresql-over-sql-server.md)
- [ADR‑002](docs/adr/002-pragmatic-hybrid-architecture.md)
- [ADR‑003](docs/adr/003-choose-minimal-apis.md)
- [ADR‑004](docs/adr/004-ephemeral-local-databases-with-databasemanager.md)
- [ADR‑005](docs/adr/005-extract-endpoint-specific-queries-into-query-providers.md)
- [ADR‑006](docs/adr/006-unit-of-work-as-endpoint-filter.md)
- [ADR‑007](docs/adr/007-api-key-authentication-for-phase-1.md)

### Examples
- [API Scenarios](docs/examples/api-scenarios/) with httpyac

## Development Setup

### Prerequisites
- .NET 9 SDK
- Docker Desktop
- PostgreSQL 17 (or run it via Docker)
- Visual Studio, Rider, or VS Code

### Quick Start with .NET Aspire

There are two easy ways to run locally.

1) Run services individually (simple and explicit):

```bash
git clone https://github.com/Scytlee/ForeverBloom.git
cd ForeverBloom
dotnet restore

# Start the API (HTTPS)
dotnet run --project src/backend/ForeverBloom.Api
# API: https://localhost:7299/api/v1

# In a second terminal, start the frontend (HTTPS)
dotnet run --project src/frontend/ForeverBloom.Frontend.RazorPages
# Frontend: https://localhost:7069
```

The frontend’s development settings point to the API at `https://localhost:7299/api/v1/`, so the two projects work together out of the box.

2) Orchestrate the stack with .NET Aspire:

```bash
dotnet run --project aspire/ForeverBloom.Aspire.AppHost
```

Aspire brings up PostgreSQL, runs the DatabaseManager once to initialize/migrate/seed, then starts the API and frontend. By default the frontend is exposed externally; the API runs internally. To call the API through the frontend under Aspire, either expose the API externally in the AppHost or set the frontend’s `ApiClient__BasePath` to the API URL for that session. See [Local Development](docs/architecture/local-development.md) for details.

### Running Tests

```bash
dotnet test                     # all tests
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

## CI/CD Pipeline

CI restores, builds, and tests on every branch. On `main`, images are published to GHCR and then promoted:
- Integration deploy is automated from the main pipeline.
- Staging and Production promotions are triggered via dedicated workflows with SQL script generation/verification steps.

See `.github/workflows/` for the pipeline and promotion workflows.

## Key Features Implemented

- Category management with `ltree` and tree queries
- Product catalog CRUD with images and pricing
- Slug system for unique URLs across entities
- API key authentication with role‑based access control
- Search and filtering across product attributes
- Offset‑based pagination with total counts
- FluentValidation with detailed problem responses
- Consistent error handling via `ProblemDetails`
- Structured logging with Serilog
- Health endpoints for aliveness and readiness
- Seed data for development and tests
- Static Razor Pages for common site content

## Testing Strategy

Tests emphasize realistic behavior with integration coverage around API contracts and persistence, complemented by unit tests for focused business rules. Shared test infrastructure, data builders, and category-based filtering keep the suite fast and easy to navigate. Coverage is high across critical paths.

## Security

This repository contains source code for a live production system. If you discover a security vulnerability, please report it responsibly by emailing [ops@foreverbloomstudio.pl](mailto:ops@foreverbloomstudio.pl) with details. Your assistance in keeping the platform secure is appreciated.

## License

All rights reserved — portfolio evaluation only. The code is available to view and assess; it is not licensed for use, modification, or redistribution. See [LICENSE](LICENSE) for full terms.

## Contact

Questions or feedback are welcome by contacting the repository owner via email.
