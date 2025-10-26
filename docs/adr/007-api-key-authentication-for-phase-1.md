# ADR-007: API Key Authentication for Phase 1

- **Status:** Accepted
- **Decided:** 2025-09-05
- **Recorded:** 2025-10-04

## Context

Phase 1 scope was defined as a product catalog showcase without user accounts, shopping cart, or orders. The frontend uses a Backend-for-Frontend (BFF) pattern, meaning API keys would never be exposed in the browser—all API communication occurs server-side.

The authentication system needed to:
1. Protect the internal API from unauthorized access
2. Distinguish between public catalog reads and admin catalog management
3. Align with Phase 1's catalog showcase scope
4. Support clean migration to full authentication in Phase 2

## Decision

Implement API key authentication with two-tier access control:
- **Frontend keys:** Read-only catalog access (public endpoints)
- **Admin key:** Full CRUD operations on catalog (admin endpoints)

Authentication uses a custom `ApiKeyAuthenticationHandler` with scope-based claims ("frontend" and "admin" scopes). Keys are configured via strongly-typed options and validated at startup.

## Alternatives considered

- **ASP.NET Core Identity:** Full authentication system with user accounts, passwords, and role management. Deferred to Phase 2 when user accounts are required for e-commerce features (cart, orders, payments).
- **API key authentication** (chosen): Simple authentication sufficient for BFF pattern and Phase 1 scope.

## Rationale (decision drivers)

- **Scope alignment:** Phase 1 has no user accounts, shopping cart, or orders—API keys are sufficient for catalog showcase
- **BFF security:** Keys remain server-side only in the BFF layer, never exposed to browser or client applications
- **Two-tier access:** Frontend scope for catalog reads, admin scope for catalog management
- **Simplicity:** Minimal implementation complexity appropriate for Phase 1 scope
- **Migration path:** Architecture designed for clean transition to ASP.NET Identity in Phase 2

## Consequences

**Benefits:**
- Minimal implementation time compared to full Identity integration
- Appropriate security model for Phase 1 BFF pattern (keys never exposed in browser)
- Clear frontend/admin separation via scope-based claims
- Validates BFF architecture before adding Identity complexity

**Trade-offs:**
- Phase 2 will require migration to ASP.NET Identity for user account features
- Single admin key appropriate for solo developer; Phase 2 will introduce role-based authorization
- Admin actions tracked by timestamp but not individual user (acceptable for single-developer Phase 1)

**Migration to Phase 2:**
- Replace `ApiKeyAuthenticationHandler` with `IdentityAuthenticationHandler`
- Implement ASP.NET Identity endpoints (registration, login, logout, profile management)
- Migrate from scope claims to role-based authorization
- Add user audit trails for admin actions

## Notes

The BFF pattern provides defense in depth—even with simple API key authentication, keys are protected server-side. This approach validated the BFF architecture before introducing the complexity of Identity integration, cookie management, and CSRF protection required for user-facing authentication.
