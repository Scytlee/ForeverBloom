# Local Development Setup

This document describes how local development is orchestrated for ForeverBloom. The project uses .NET Aspire to coordinate PostgreSQL, a one‑shot DatabaseManager, and the application services so that the stack comes up coherently with minimal local configuration.

## Overview

With .NET Aspire, the local environment behaves like a small composed system. Aspire is responsible for starting the database, running the DatabaseManager to prepare it, and then bringing up the backend and frontend in the right order. The result is a predictable developer experience without ad‑hoc scripts or manual database work.

Key benefits:
- No manual database provisioning; migrations and seeding are automated.
- The entire stack is orchestrated together.
- Configuration flows through .NET User Secrets rather than local `.env` files.

## Aspire Orchestration

The service topology is defined in the AppHost project (`aspire/ForeverBloom.Aspire.AppHost/Program.cs`). Aspire coordinates startup so that components come online only when their dependencies are ready:
1. PostgreSQL starts and executes its init scripts.
2. DatabaseManager waits for the database, initializes schemas and roles, applies migrations, seeds data, and then exits.
3. The backend waits for the DatabaseManager to complete successfully before starting.
4. The frontend waits for the backend to become ready before it starts.

For the rationale behind the ephemeral database approach, see [ADR‑004](../../adr/004-ephemeral-local-databases-with-databasemanager.md).

## DatabaseManager: Automated Setup

DatabaseManager is a dedicated, short‑running process that prepares the database for the application. It creates the `business` schema and the application role and user with the appropriate permissions, applies any pending EF Core migrations so the schema matches the current model, and seeds initial data used for development and testing. After it completes its work, the process sets the exit code and stops; the backend only starts after a successful completion (exit code 0).

## Environment Configuration Strategy

Configuration follows the usual layered approach, with later sources overriding earlier ones:
1. `appsettings.json` embedded in the application.
2. `appsettings.{Environment}.json` embedded in the application.
3. Environment variables injected by .NET Aspire, sourced from AppHost parameters backed by .NET User Secrets.

Aspire reads secrets from the AppHost’s User Secrets store and injects them as environment variables for each service. This keeps sensitive values out of source control and ensures they override any appsettings when the local stack is orchestrated.

## API Documentation (OpenAPI/Scalar)

During development and integration testing, the API exposes two developer‑facing endpoints: the OpenAPI document at `/openapi/v1.json` and an interactive reference via Scalar at `/scalar/v1`. These endpoints are enabled only for Development and Integration environments and are disabled in production. The configuration is wired in `src/backend/ForeverBloom.Api/Program.cs` with environment checks around the OpenAPI and Scalar mappings.
