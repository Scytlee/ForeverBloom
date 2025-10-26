# API Scenarios (httpyac)

This document describes seven narrative scenarios that capture the Phase 1 API endpoints and workflows. Scalar/OpenAPI is the source of truth for request and response shapes and status codes (the application exposes OpenAPI/Scalar in Development). The scenarios reflect the system at Phase 1 completion.

These scenarios document API behavior used during development. They are descriptive, not a runnable guide or a supported test harness. The accompanying `.http` files are formatted for httpyac and served as a development aid.

Notes
- Do not commit real keys. Placeholders are present only to illustrate headers; if experimenting locally, keep any real values in a private, git‑ignored override.
- The scenarios are curated, not exhaustive. Use Scalar/OpenAPI for the full surface area.
- Public reads require the `X-Api-Key` header (frontend scope). Admin writes require `X-Api-Key` (admin scope).

Included scenarios
- `S01-category-page-setup.http` — Sets up a minimal catalog and exercises the endpoints a Category page uses.
- `S02-catalog-lifecycle.http` — Walks a product through create → public read → slug update (with concurrency) → listing → delete, and covers category archive/restore.
- `S03-product-management.http` — Updates product details and replaces images with concurrency.
- `S04-auth-demo.http` — Shows 401 vs 403 vs 200 across public and admin endpoints.
- `S05-product-browsing.http` — Demonstrates paging, `searchTerm`, category/child aggregation, featured filter, sorting, and an invalid sort example.
- `S06-slug-management.http` — Updates a product slug and verifies canonical redirects (categories behave the same).
- `S07-sitemap-data.http` — Checks category sitemap data before and after creating a category.
