# ADR-002: Pragmatic Hybrid Architecture

- **Status:** Accepted
- **Decided:** 2025-04
- **Recorded:** 2025-10-04

## Context

At the start of the ForeverBloom project, an architectural approach was needed for building the e-commerce application. The project required a structure that would enable rapid development while maintaining clear separation of concerns and testability.

Two established architectural patterns were evaluated: Clean Architecture with full layering (Application, Domain, Infrastructure separation) and Vertical Slice Architecture with feature-based organization. The goal was to select an approach that balanced development velocity with maintainability for Phase 1's catalog showcase scope.

## Decision

Adopt a pragmatic two-layer architecture combining elements from multiple patterns:
- **Two-layer structure:** API + Persistence layers
- **Vertical slice organization:** One folder per endpoint, self-contained
- **Business logic:** Embedded in endpoint handlers
- **Clear dependency flow:** API layer depends on Persistence layer only

## Alternatives considered

- **Clean Architecture**: Full layering with Application, Domain, and Infrastructure separation. Evaluated as having more ceremony than needed for Phase 1 scope.
- **Vertical Slice Architecture**: Pure feature-based slicing. Considered but deferred due to unclear domain boundaries at project start.
- **Pragmatic hybrid architecture** (chosen): Two-layer approach with vertical slice organization within the API layer.

## Rationale (decision drivers)

- **Development velocity:** Streamlined structure enables rapid feature implementation
- **Simple mental model:** Two-layer architecture is easy to understand and navigate
- **Vertical organization:** One-endpoint-per-folder keeps related code together
- **Clear dependencies:** API → Persistence dependency flow prevents circular references
- **Testability:** API as single entry point simplifies integration testing strategy
- **Appropriate scope:** Architecture complexity matched to Phase 1 catalog showcase requirements

## Consequences

**Benefits:**
- Fast development velocity for Phase 1 implementation
- Simple, navigable codebase structure with consistent organization
- API as single source of truth simplified testing strategy
- Vertical slice organization keeps endpoint logic self-contained
- Clear separation between presentation (API) and data access (Persistence)

**Trade-offs:**
- Business logic in endpoint handlers couples presentation and domain concerns
- Two-layer structure may require evolution for more complex infrastructure requirements
- Architecture optimized for Phase 1 scope; may be revisited as application complexity grows

## Notes

This architectural decision prioritized delivery velocity and simplicity for Phase 1's catalog showcase scope. The structure successfully supported rapid development while maintaining testability and code organization through vertical slice patterns.
