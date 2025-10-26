# Security & Authentication

## Authentication Strategy

Phase 1 uses a simple API key approach tailored for the Backend‑for‑Frontend (BFF) setup. The frontend runs as a trusted server component and talks to the API with a dedicated key, so keys are never exposed to a browser. The rationale and trade‑offs are captured in [ADR‑007](../adr/007-api-key-authentication-for-phase-1.md).

Key characteristics:
- BFF pattern with a dedicated server‑side key
- Two access levels: frontend and admin
- Small surface area appropriate for the Phase 1 catalog scope
- Keys remain server‑side only

## API Key Authentication

### Authentication Flow

Requests are authenticated by `ApiKeyAuthenticationHandler`. The handler reads the API key from a configurable header (the default is `X-Api-Key`), compares it with the configured admin or frontend keys, issues a `Scope` claim of either `admin-api-access` or `frontend-api-access`, and returns a success or failure result accordingly.

Configuration is provided through environment variables:
- `ApiKeys__HeaderName` – header name for the API key (defaults to `X-Api-Key`)
- `ApiKeys__FrontendKeys__0`, `ApiKeys__FrontendKeys__1` – one or more frontend keys
- `ApiKeys__AdminKey` – admin key

The BFF client sends the same header when calling the API:
- `ApiClient__ApiKeyHeaderName` – header name used by the frontend HTTP client

On startup, options validation ensures required values are present and that at least one frontend key is configured.

## Authorization Policies

Authorization is policy‑based with two tiers.

FrontendAccess policy:
- Accepts requests with a `Scope` of `frontend-api-access` or `admin-api-access`
- Serves as the default for all endpoints
- Protects public catalog endpoints: `/api/v1/products`, `/api/v1/categories`

AdminAccess policy:
- Requires a `Scope` of `admin-api-access`
- Protects administrative endpoints: `/api/v1/admin/products`, `/api/v1/admin/categories`

Policies are applied to endpoint groups so related routes are consistently protected.

## Input Validation

Requests are validated with FluentValidation. Validators use shared constants from `ForeverBloom.Domain.Shared`, and return standardized error codes that clients can localize. A lightweight endpoint filter runs before the handler; if validation fails, the request short‑circuits and the API returns RFC 9110–style Problem Details that carry the error codes.

## Database Security

The database follows a small, explicit separation of duties and keeps application tables in their own schema. The `business` schema isolates application data, and access is granted through a constrained application role.

User separation:
1. Database superuser (`postgres`) – used only for initialization and emergency operations
2. Migration user (development/integration) – applies schema changes via DatabaseManager
3. Application user (`app_user`) – DML permissions only (SELECT, INSERT, UPDATE, DELETE)

In production, migrations are applied manually after an operator reviews the SQL (see [ADR‑004](../adr/004-ephemeral-local-databases-with-databasemanager.md)).

## Error Handling

Authentication challenges and authorization failures return RFC 9110‑compliant Problem Details. A `401 Unauthorized` indicates a missing or invalid key and includes the `WWW-Authenticate` header. A `403 Forbidden` means the key is valid but does not have the required scope.

Problem Details are enriched with helpful context: the `instance` field contains the HTTP method and path, and the payload also includes a `requestId` and `traceId` for diagnostics. Responses intentionally avoid exposing stack traces, internal paths, or database details, and the format is consistent across failure cases.

## Security Monitoring

Structured logging with Serilog provides request tracing and security signals. When available, logs include the resolved authentication scope (frontend or admin). The system records 401/403 responses, excludes query strings from logs, and attaches an `X-Request-ID` header for correlation across services.

Health check endpoints:
- `/alive` – liveness probe
- `/health` – readiness probe with dependency checks
