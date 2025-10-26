# ADR-005: Extract Endpoint-Specific Queries into Query Providers

- **Status:** Accepted
- **Decided:** 2025-05-08
- **Recorded:** 2025-10-04

## Context

Initially, queries were implemented by injecting DbContext directly into endpoint handlers. This made handlers untestable since DbContext couldn't be easily mocked. Each endpoint needed different data shapes—public endpoints required a subset of fields, admin endpoints needed full data, and some endpoints needed validation-only queries. Some endpoints needed tracked entities for updates, while others needed untracked entities for read-only operations. Placing these queries in repositories was considered but would lead to repository bloat with endpoint-specific one-off query methods and would violate the one-endpoint-per-folder organizational principle.

## Decision

Separate commands and queries. Commands (write operations) remain in repositories. Queries are extracted to endpoint-specific query providers that live within each endpoint's folder and are injected into endpoint handlers, making them testable via mocking.

## Alternatives considered

- **Direct DbContext injection in handlers**: Initial approach. Queries implemented directly in endpoint handlers. Rejected due to making handlers untestable.
- **Queries in repositories**: Considered as a way to maintain testability. Rejected due to anticipated repository bloat with single-use query methods, and because it would violate the one-endpoint-per-folder organizational principle (queries specific to an endpoint would live outside the endpoint's folder).
- **Endpoint-specific query providers** (chosen): Dedicated query providers per endpoint, injected for testability, living within each endpoint's folder.

## Rationale (decision drivers)

- Enable unit testing of endpoint handlers through dependency injection and mocking (solving the DbContext testability problem)
- Avoid repository bloat by not adding single-use query methods that serve only one endpoint
- Maintain one-endpoint-per-folder organizational principle by keeping endpoint-specific queries within the endpoint's folder
- Allow endpoint-specific optimization (projections, tracking vs no-tracking queries)
- Each endpoint declares exactly what data it needs without over-fetching

## Consequences

**Benefits (realized during implementation):**
- Achieved testability through mockable query providers
- Endpoint-specific optimization possible (no over-fetching)
- Repositories stay focused on commands only
- Clear separation of concerns (write vs read)
- Organizational consistency maintained (queries live with their endpoints)

**Downsides/Risks (discovered during implementation):**
- DbContext accessed from both Persistence and API layers creates confusion
- Query provider proliferation (one per endpoint with queries)
- Good separation decision, but execution needs improvement

**Follow-ups:**
- Pattern later identified as "lightweight CQRS" during documentation
- Was not a conscious "we're implementing CQRS" decision—evolved organically from practical needs (testability, avoiding bloat, organizational consistency)
- Phase 2 will keep command/query separation but rethink execution to avoid layer confusion

## Notes

This decision emerged organically from practical evolution rather than conscious pattern adoption. The progression was: (1) DbContext in handlers (May 7, 2025) → not testable, (2) considered repositories → would violate folder organization, (3) query providers (May 8, 2025) → testability + organizational consistency.
