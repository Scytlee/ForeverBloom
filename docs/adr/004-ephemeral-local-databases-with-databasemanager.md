# ADR-004: Ephemeral Local Databases with DatabaseManager

- **Status:** Accepted
- **Decided:** 2025-04
- **Recorded:** 2025-10-04

## Context

Professional experience had always used persistent local development databases. This project presented an opportunity to experiment with ephemeral databases (fresh database recreated on every run). The challenge was how to handle database initialization, migrations, and seeding for local development. Aspire orchestration enabled automated database setup workflows.

## Decision

Use ephemeral local databases (recreated on every stack startup) with a custom DatabaseManager console application to automate database initialization, EF Core migrations, and test data seeding. For development and integration environments, DatabaseManager runs automatically on startup. For production, manual migrations are used (DatabaseManager is available but not used).

## Alternatives considered

- **Persistent local database**: Traditional approach from professional experience. Requires manual migration management and cleanup of stale data.
- **Ephemeral databases with initialization in Persistence project**: Fresh database each run, but initialization logic embedded in the application layer, mixing concerns.
- **Ephemeral databases with DatabaseManager tool** (chosen): Fresh database each run with dedicated console application for initialization and migrations.

## Rationale (decision drivers)

- The ephemeral approach provides a clean slate for every run, ensuring each startup begins with a known good state and consistent test data
- This eliminates the entire class of stale data issues common with persistent databases
- The developer experience is significantly improved by removing manual database setup and cleanup tasks
- Creating a dedicated DatabaseManager tool maintains separation of concerns by keeping database initialization outside the application runtime

## Consequences

**Benefits (realized during implementation):**
- Clean slate eliminates stale data issues entirely
- Consistent test data across runs
- Superior developer experience (no manual setup)
- Significant investment justified by workflow improvement
- Pattern validated: would use for any future project

**Trade-offs:**
- ~5 second startup delay (acceptable)
- Manual test data lost between runs (mitigated by updating DataSeeder as needed)
- Requires custom tooling (ephemeral approach not easily achievable without DatabaseManager)

**Follow-ups:**
- DatabaseManager tool evolved over time, proving extensible for additional initialization tasks
- Multi-branch development workflow benefited significantly from ephemeral approach (later discovery)

## Notes

This was the first project using ephemeral databases, departing from persistent database practices used in professional experience. The decision was made during early development (April 2025) when setting up the Aspire orchestration. The DatabaseManager tool proved extensible, handling various initialization tasks beyond the original scope.
