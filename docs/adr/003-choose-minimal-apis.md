# ADR-003: Choose Minimal APIs

- **Status:** Accepted
- **Decided:** 2025-04
- **Recorded:** 2025-10-04

## Context

The project required selecting an API implementation approach for all HTTP endpoints. ASP.NET Core offers two primary patterns: traditional Controllers and the newer Minimal APIs approach. This decision needed to consider development velocity, code organization, and maintainability for the project's vertical slice architecture.

The FastEndpoints library's "one endpoint per file" philosophy was evaluated as a potential organizational model. The concept was extended to "one endpoint per folder" to accommodate endpoint-specific validators, query providers, and related files while maintaining clear boundaries.

## Decision

Use Minimal APIs for all endpoints with one-endpoint-per-folder organization.

## Alternatives considered

- **Controllers**: Traditional approach with endpoints grouped in controller classes. Evaluated but Controllers group multiple related endpoints in single files, which conflicts with the vertical slice organization goal.
- **Minimal APIs** (chosen): Endpoint-focused approach with explicit route configuration and reduced ceremony.

## Rationale (decision drivers)

- **Reduced ceremony:** Minimal APIs eliminate controller class boilerplate and attribute-based routing complexity
- **Explicit control:** Direct control over route paths, grouping, OpenAPI tags, and filters
- **Vertical slice alignment:** One-endpoint-per-folder organization keeps all endpoint concerns together (handler, validator, query provider)
- **Easy toggling:** Commenting two lines in Program.cs can include/exclude an endpoint
- **Modern approach:** Aligns with .NET's direction for cloud-native and microservice architectures

## Consequences

**Benefits:**
- Less ceremony compared to Controllers (no base classes, simplified routing)
- Explicit control over paths, grouping, and middleware/filter application
- Natural fit with one-folder-per-endpoint organization
- Easy endpoint inclusion/exclusion during development
- Clear, focused endpoint definitions

**Trade-offs:**
- Requires explicit registration of each endpoint in Program.cs
- No controller-level filters or conventions (handled via endpoint groups)
- Less familiar pattern for developers from traditional MVC/Web API backgrounds

## Notes

The one-endpoint-per-folder pattern successfully scaled across Phase 1's catalog API endpoints, maintaining clear organization and easy navigation. Each endpoint folder contains its handler, request validator, query provider interface/implementation, and any endpoint-specific helpers.
