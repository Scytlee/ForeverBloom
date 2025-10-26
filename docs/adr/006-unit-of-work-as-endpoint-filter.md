# ADR-006: Unit of Work as Endpoint Filter

- **Status:** Accepted
- **Decided:** 2025-05-10
- **Recorded:** 2025-10-04

## Context

Initially, transaction management was implemented manually in every command endpoint with the same repeated code: transaction begin, try/catch blocks, commit/rollback logic, and error handling. This code duplication existed across all mutating endpoints. The Unit of Work pattern was learned and implemented as an injectable service, which improved the abstraction but still required manual wrapping in every endpoint handler. Endpoint filters were discovered as a mechanism for handling cross-cutting concerns automatically.

## Decision

Extract transaction management to an endpoint filter that automatically begins transactions before handler execution, commits on 2XX status codes, and rolls back on non-2XX status codes or exceptions. The filter is applied via `.AddEndpointFilter<UnitOfWorkEndpointFilter>()` on command endpoints.

## Alternatives considered

- **Manual transaction management in every endpoint**: Initial approach. Rejected due to code duplication and inconsistency risk.
- **Unit of Work as injectable service**: Improved abstraction but still required manual wrapping in every endpoint handler. Rejected as not DRY enough.
- **Unit of Work as endpoint filter** (chosen): Automatic transaction management applied via endpoint filter.

## Rationale (decision drivers)

- Eliminate duplicated transaction boilerplate across all command endpoints (DRY principle)
- Guarantee transactional behavior for all command endpoints automatically (consistency)
- Move error handling and transaction management to filter (simplified handlers)
- Explicit opt-in via `.AddEndpointFilter<UnitOfWorkEndpointFilter>()` makes transaction boundaries visible (clear intent)

## Consequences

**Benefits (realized during implementation):**
- Eliminated transaction boilerplate from endpoint handlers
- Guaranteed consistent transactional behavior across all command endpoints
- Simplified error handling in endpoints (moved to filter)
- Clear separation of concerns (business logic vs infrastructure)
- Dramatic reduction in boilerplate code

**Downsides/Risks (discovered during implementation):**
- Status-code-based commits require escape hatch for edge cases
  - Example: Login attempts must commit on 401 (for lockout tracking)
  - Solution: `CommitsOnStatusCodeMetadata` extension created (discovered during Identity implementation, not in Phase 1)
- Filter must be manually applied to each endpoint (could be forgotten), though this was deemed a lesser risk than incorrect implementation of Unit of Work handling
- Tight coupling between HTTP status codes and database transactions

**Follow-ups:**
- Edge case discovered during Identity implementation (not in Phase 1 scope)
- CommitsOnStatusCodeMetadata allows specific status codes to commit instead of rollback
- Phase 1 has no edge cases requiring this escape hatch
- Edge case discovery validated the importance of escape hatch design

## Notes

The evolution timeline was: (1) Manual transactions everywhere → code duplication, (2) Learned Unit of Work pattern → extracted to service, (3) Still manual wrapping in handlers → not DRY enough, (4) Learned about endpoint filters → "aha moment", (5) Created UnitOfWorkEndpointFilter → automated everything.
