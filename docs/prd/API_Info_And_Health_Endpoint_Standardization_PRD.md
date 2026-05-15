# Product Requirements Document

## Feature Name

Standardized API Info And Health Endpoints

## Description

Standardize the `/info` and `/health` endpoints across every API in the Pseudo Markets platform. Each API should expose the same endpoint paths and follow the same response conventions so the platform has a consistent operational surface for local development, Docker, and future Kubernetes deployment.

## Problem Statement

The platform currently has inconsistent operational endpoints across services. Some APIs already expose `/health`, some do not, and there is no standardized `/info` endpoint. This makes it harder to validate service startup, troubleshoot deployments, configure container orchestration probes, and quickly identify which build/version of a service is running.

## Why

This feature matters because the platform is growing into a multi-service architecture and needs a predictable operational contract across all APIs. Standardized info and health endpoints improve local debugging, Docker Compose verification, Kubernetes readiness/liveness configuration, and future CI/CD deployment validation. They also reduce friction when onboarding additional services into the monorepo.

## Audience

- Platform developers working locally
- Internal services and infrastructure components
- Docker and Kubernetes deployment workflows
- Future CI/CD automation and operational tooling

## What

Every API in the platform should expose:

- `GET /info`
- `GET /health`

The `/info` endpoint should return:

- application name
- application version
- build timestamp

The `/health` endpoint should:

- use `Microsoft.Extensions.Diagnostics.HealthChecks`
- replace any existing non-standard health endpoint implementation
- return a standardized health check response based on `HealthCheckResult`
- be available at the exact route `/health`
- include lightweight dependency checks for directly connected infrastructure where applicable

This standard should apply to all current API services in the monorepo, including:

- Pseudo Markets Identity Server
- Pseudo Markets Market Data Service
- Pseudo Markets Transaction Processing Service
- Pseudo Markets Trading Instruments Service
- Pseudo Markets Order Execution Service

The `/info` endpoint should be unauthenticated and intended for operational visibility only. The `/health` endpoint should also be unauthenticated so it can be used by local tooling, Docker health checks, and future Kubernetes probes.

For services that connect directly to Aerospike, the health check should validate the connection using the `IsConnected` property on `AerospikeClient`.

For services that connect directly to PostgreSQL, the health check should include a similar lightweight database connectivity check suitable for request-time health evaluation, without turning the endpoint into a heavyweight diagnostic probe.

## How

Implementation should introduce a consistent pattern across all service projects rather than allowing each API to define its own approach.

At a high level:

- Add a shared convention for registering and mapping info and health endpoints in each service `Program.cs`
- Use `Microsoft.Extensions.Diagnostics.HealthChecks` for health registration and execution
- Replace any existing custom or ad hoc health endpoint behavior with the new standard implementation
- Add a lightweight info response model or minimal API response payload that includes:
  - app name
  - version
  - build timestamp
- Source app name and version from the running application metadata
- Source build timestamp from application/build metadata so it reflects the built artifact rather than current request time
- Update each API project's `.csproj` as needed so version and build metadata are emitted consistently into the built assembly or package metadata
- Standardize how build timestamp metadata is produced so every API returns the same kind of value from `/info`
- Ensure local builds, Docker builds, and future CI/CD builds all preserve the metadata required by the `/info` endpoint
- Register lightweight dependency checks only for infrastructure each service directly owns a connection to
- For Aerospike-connected services, implement the health check using `AerospikeClient.IsConnected`
- For PostgreSQL-connected services, implement a lightweight connectivity validation appropriate for EF Core/Npgsql-backed services
- Ensure Swagger and README documentation reflect the presence of both endpoints
- Ensure Docker health checks and any service-specific documentation reference the standardized `/health` route

If a service already has a `/health` endpoint, it should be updated to follow the standardized implementation rather than keeping a service-specific variation.

The implementation should explicitly evaluate whether each API service project needs `.csproj` changes such as assembly metadata, version metadata, informational version metadata, or generated build properties in order to provide reliable `/info` responses.

Dependency health checks should remain intentionally lightweight. They should confirm that the application can still reach its directly connected persistence layer, but they should not expand into deep dependency traversal across downstream HTTP services.

## Acceptance Criteria

- [ ] Every current API service exposes `GET /info` and `GET /health`
- [ ] `GET /info` returns the application name, version, and build timestamp for the running service
- [ ] `GET /health` uses `Microsoft.Extensions.Diagnostics.HealthChecks`
- [ ] Any existing health endpoint implementation is replaced with the standardized one
- [ ] Both endpoints use the exact routes `/info` and `/health`
- [ ] Both endpoints are unauthenticated
- [ ] Services that directly use Aerospike report dependency health using `AerospikeClient.IsConnected`
- [ ] Services that directly use PostgreSQL report dependency health using a lightweight database connectivity check
- [ ] Each API project includes whatever `.csproj` metadata changes are required for `/info` to return consistent application version and build timestamp values
- [ ] Service documentation is updated anywhere endpoint behavior or operational URLs are described
- [ ] Docker and local verification flows continue to work with the standardized health endpoint

## Out Of Scope

- Adding deep dependency-specific health probes for every downstream service or database
- Adding authentication or authorization to info or health endpoints
- Building a centralized shared library for operational endpoints unless it becomes necessary during implementation
- Standardizing non-operational diagnostic endpoints beyond `/info` and `/health`
- Deep health probing of downstream HTTP dependencies

## Notes

- Current repo context indicates that Market Data, Transaction Processing, Trading Instruments, and Order Execution already expose `/health`, while Identity Server does not yet expose the standardized health endpoint.
- This PRD focuses on endpoint standardization only. More advanced readiness/liveness or dependency health strategies can be layered in later if needed.
- The `/info` endpoint should return build metadata from the built artifact itself, not from a runtime-generated current timestamp.
