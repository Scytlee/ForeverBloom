# URL Design and Routing Strategy

This document defines the user-facing URL structure, how slugs are resolved, and how redirects are handled. It reflects what is implemented today and notes planned extensions where relevant.

## 1. Guiding Principles

URLs should be easy to read and clearly indicate the content they address. Each piece of content has a single canonical address to avoid duplicates and preserve SEO equity. Slugs may change over time; the system keeps their history and issues permanent redirects to the current canonical address. Marketing aliases are planned as a separate layer and will not interfere with canonicalization.

## 2. Core Concepts

Canonical URLs are the public, browser-visible addresses. The site uses language-specific prefixes for entity pages. A slug is a globally unique, SEO‑friendly identifier stored in a central registry. Exactly one slug is active per entity; older slugs remain as history and are used for 301 redirects.

- URL prefixes (user‑facing): `/produkt/` for products, `/kategoria/` for categories.
- Static root pages are not slug‑based (e.g., `/o-nas`, `/kontakt`, `/polityka-prywatnosci`, `/sitemap.xml`).
- Slug constraints: lowercase letters and digits with hyphens (`^[a-z0-9]+(?:-[a-z0-9]+)*$`), up to 255 characters, globally unique across entity types.
- The `SlugRegistry` stores all slugs and enforces global uniqueness; it guarantees at most one active slug per entity at a time. Historical rows enable permanent redirects from old slugs.
- Aliases (managed redirects) are planned as a separate system for vanity URLs and campaign links.

## 3. The Canonical URL Structure

Canonical paths are prefixed and language‑aware:

- Product: `/produkt/{slug}`
- Category: `/kategoria/{slug}`
- Static pages: `/o-nas`, `/kontakt`, `/polityka-prywatnosci`, `/sitemap.xml`

The Web API is internal to the frontend (BFF pattern) and is not canonical. It exposes entity routes under `/api/v1/...`, for example:

- `GET /api/v1/products/{slug}`
- `GET /api/v1/categories/{slug}`

Future entities such as blog posts or courses would follow the same approach with clear prefixes (for example, `/blog/{slug}`) when they are introduced.

## 4. The Slug Resolution Waterfall

Requests for entity pages arrive at Razor Pages using the canonical prefixes (`/produkt/{slug}` or `/kategoria/{slug}`). The page calls the internal API to resolve the slug and decide on the outcome.

1. Static pages and fixed routes are handled directly by Razor Pages and do not use slug resolution.
2. The page calls `GET /api/v1/products/{slug}` or `GET /api/v1/categories/{slug}`.
3. The API validates the slug format and looks it up in the `SlugRegistry` to identify the target entity and its current slug.
4. Outcomes:
   - Current slug and entity is accessible: API returns 200; the page renders normally.
   - Historical slug: API returns 301 with a `Location` header that points to the same API route with the current slug. The frontend extracts the current slug and issues a permanent redirect to the canonical user‑facing path (`/produkt/{slug}` or `/kategoria/{slug}`).
   - Not found or not accessible: API returns 404; the page returns 404.

This sequence guarantees a single visible URL per entity while preserving old links.

## 5. API-Driven Slug Resolution

Slug resolution is handled by entity‑specific GET endpoints; there is no standalone “resolve‑slug” endpoint.

- Endpoints: `GET /api/v1/products/{slug}`, `GET /api/v1/categories/{slug}`.
- Possible responses:
  - 200 OK — the provided slug is current; the response body contains the entity data used by the page.
  - 301 Moved Permanently — the provided slug is historical; the response has an empty body and a `Location` header pointing to `/api/v1/{entity}/{currentSlug}`. The frontend does not auto‑follow redirects; it reads the header and redirects the browser to the canonical user‑facing URL.
  - 404 Not Found — the slug does not exist or the entity is not accessible.

## 6. The Redirect Management System (Aliases)

Aliases are planned as a separate feature for vanity links, campaign URLs, and repairing broken inbound links. This system is distinct from slug history and does not replace canonicalization. It is not implemented yet; the outline below captures the intended design.

- Purpose: create human‑friendly shortcuts (for example, `/swieta` → `/kategoria/suszone-kwiaty`), run temporary campaigns, and maintain legacy links.
- Separation: managed independently from `SlugRegistry`; it can point to any destination path.
- Proposed data model:
  - `Id` (integer): primary key
  - `SourcePath` (string): unique incoming path (e.g., `/prezent-last-minute`)
  - `DestinationPath` (string): internal destination (e.g., `/produkt/the-aurora-kit`)
  - `RedirectType` (integer): 301 (permanent) or 302 (temporary)
  - `IsEnabled` (boolean): activation flag
  - `Notes` (string): optional admin comments
- Scope: single language; internationalization is out of scope for this system.

Until aliases are added, only slug‑history redirects are supported, and they always issue a permanent (301) redirect to the current slug.
