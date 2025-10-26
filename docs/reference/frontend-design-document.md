# Frontend Architecture

## Overview

The frontend is a server‑side rendered ASP.NET Core Razor Pages application that follows a Backend‑for‑Frontend (BFF) approach. Pages render on the server for predictable performance and SEO, and the site is built and localized in Polish.

## Technology Stack

The application uses ASP.NET Core Razor Pages with Bootstrap 5 for responsive styling. Client‑side code is intentionally minimal and relies on Bootstrap’s components and small vanilla JavaScript helpers; Alpine.js is present in the repository via LibMan but is not currently used in any page. The site is served in Polish (pl‑PL) with culture configured at startup.

## Architecture Pattern (BFF)

The frontend is the only client of the internal Web API. A centralized `ApiClient` handles all calls to the backend so that pages do not make direct browser‑to‑API requests. API keys are added to the `HttpClient` on the server and never exposed to the browser. This keeps secrets out of client code and gives the frontend a single place to manage error handling, redirects, and logging for backend communication.

## URL Structure & SEO

The site uses Polish canonical URLs:
- Product pages: `/produkt/{slug}`
- Category pages: `/kategoria/{slug}`

When a user visits a historical slug, the backend indicates the current slug and the frontend issues a `301` redirect to the canonical URL. Pages set a dynamic `<title>` and meta description. The layout sends a strong `noindex, nofollow` signal on non‑production environments. A Razor Pages endpoint serves a dynamic sitemap at `/sitemap.xml`, and `robots.txt` is environment‑aware (production allows indexing; non‑production disallows it). The HTML root sets `lang="pl"` for correct language signals.

## Asset Management

Static assets are served from `wwwroot`. Product and category images live under `/images/uploads`.

- Deployment volumes: in Docker Compose environments the container mounts a host directory to `/app/wwwroot/images/uploads` (read‑only) so images persist across deployments. In local Aspire runs there is no container volume; images are served directly from the project’s `wwwroot`.
- Formats and MIME types: AVIF images are supported and explicitly mapped so they are served with `image/avif`.
- Vendor assets: libraries are managed with LibMan. Minified CSS is used from vendors; application JavaScript is served unbundled. For production, using the minified Bootstrap bundle or adding a bundling step is recommended but not required by the current setup.
