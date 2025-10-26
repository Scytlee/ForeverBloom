# ADR-001: Use PostgreSQL over SQL Server

- **Status:** Accepted
- **Decided:** 2025-04
- **Recorded:** 2025-10-04

## Context

At the start of the ForeverBloom project, a database platform was needed for the e-commerce application. The developer had 3 years of professional experience with SQL Server, making it the natural and familiar choice. However, this was a solo developer project where commercial licensing and learning opportunities were important considerations.

SQL Server would have provided a faster start given existing expertise, but required commercial licensing for production deployment. PostgreSQL offered enterprise-grade reliability and features with no licensing costs, but required accepting a learning curve despite SQL Server expertise.

## Decision

Use PostgreSQL 17 with Entity Framework Core, accepting the learning curve despite existing SQL Server expertise.

## Alternatives considered

- **SQL Server**: Familiar technology with 3 years of professional experience. Rejected due to commercial licensing requirements for production deployment.
- **PostgreSQL** (chosen): Open-source, enterprise-grade database with rich feature ecosystem. Requires learning curve but eliminates licensing costs.

## Rationale (decision drivers)

- Expand database skills beyond SQL Server and gain marketable PostgreSQL experience as a learning opportunity
- PostgreSQL's open-source model eliminates licensing costs compared to SQL Server's commercial licensing
- PostgreSQL provides enterprise-grade reliability and rich ecosystem (extensions, JSONB, full-text search, ltree)
- Database design knowledge from SQL Server experience would transfer to PostgreSQL

## Consequences

**Benefits:**
- Avoided SQL Server licensing costs for production deployment
- Gained PostgreSQL experience (marketable skill for career development)
- Access to PostgreSQL-specific features (ltree for hierarchical data, JSONB, extensions)
- Database design skills successfully transferred from SQL Server experience

**Downsides/Risks:**
- Learning curve: Different syntax, tooling, and performance characteristics to master
- Pragmatic tradeoff: Prioritized EF Core development velocity in Phase 1 over raw SQL optimization

**Follow-ups:**
- ltree extension was discovered later during Categories implementation (not a factor in this initial decision)
- Phase 2 goal: Deepen PostgreSQL SQL expertise beyond EF Core abstractions for query optimization

## Notes

The decision was made at "day zero" before implementation commenced. The learning approach differed from SQL Server: where SQL Server was learned SQL-first → EF Core, PostgreSQL was learned EF Core-first → SQL later (deferred to Phase 2). This pragmatic approach enabled rapid development velocity while establishing a foundation for deeper optimization work in Phase 2.
