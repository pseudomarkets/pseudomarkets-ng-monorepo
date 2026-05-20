# Product Requirements Document

## Feature Name

Swagger to Scalar API Exploration Migration

## Description

Migrate Pseudo Markets API browser exploration from Swagger UI to Scalar. The platform should continue generating OpenAPI documents with ASP.NET Core OpenAPI support, but the interactive browser UI should move from `/swagger` to `/scalar`.

## Problem Statement

The platform currently uses Swagger UI through Swashbuckle for local and Docker-based API exploration. As the number of API services grows, API exploration should be standardized on a modern OpenAPI UI that aligns cleanly with ASP.NET Core's built-in OpenAPI document generation model.

Swagger-specific package references, middleware, launch settings, and documentation are now spread across multiple services. This creates repeated configuration and outdated browser URLs that need to be migrated consistently.

## Why

Scalar provides a modern OpenAPI exploration experience while integrating with ASP.NET Core's generated OpenAPI document endpoint. Migrating to Scalar keeps the developer workflow simple, preserves browser-based API testing, and removes the platform's dependency on Swagger UI/Swashbuckle for API exploration.

This also aligns the platform with current Microsoft guidance for ASP.NET Core OpenAPI document generation, where the framework generates OpenAPI documents and interactive UIs such as Scalar are added separately.

## Audience

This is being built for the primary Pseudo Markets developer and future contributors who need to inspect and test API endpoints locally through a browser.

The affected services are:

- Identity Server
- Market Data Service
- Transaction Processing Service
- Trading Instruments Service
- Order Execution Service

The Batch Processing Host is not in scope unless it later exposes business API endpoints that require OpenAPI exploration. Its Hangfire dashboard remains separate from API exploration.

## What

Each browser-facing API service should expose:

- An OpenAPI JSON document at the ASP.NET Core default route, `/openapi/v1.json`
- A Scalar API reference UI at `/scalar`
- Development-only API exploration UI by default
- Existing API metadata, endpoint discovery, request schemas, response schemas, and authorization metadata
- A way to supply bearer tokens from the Scalar UI for protected endpoints

The migration should remove Swagger-specific user-facing behavior:

- `/swagger` should no longer be the documented API exploration route
- `Swashbuckle.AspNetCore` should be removed where it is no longer needed
- `AddSwaggerGen`, `UseSwagger`, and `UseSwaggerUI` should be removed from API startup code
- `launchSettings.json` files should launch `scalar` instead of `swagger`
- README and deployment documentation should refer to Scalar, not Swagger

## How

The implementation should follow the Microsoft Learn guidance for ASP.NET Core OpenAPI and Scalar:

- Keep or add `Microsoft.AspNetCore.OpenApi` in each API service project.
- Add the `Scalar.AspNetCore` package to each API service project.
- Register OpenAPI document generation with `builder.Services.AddOpenApi()`.
- In Development environments, map the OpenAPI document with `app.MapOpenApi()`.
- In Development environments, map Scalar with `app.MapScalarApiReference()`.
- Keep the default OpenAPI route shape `/openapi/{documentName}.json`, which results in `/openapi/v1.json` for the default document.
- Update local launch profiles so `launchBrowser` remains `true` and `launchUrl` is set to `scalar`.

The migration should be done service-by-service to reduce risk:

1. Update one API service as the reference implementation.
2. Verify `/openapi/v1.json` and `/scalar` work locally.
3. Verify protected endpoints can still be tested with bearer tokens from the Scalar UI.
4. Apply the same pattern to the remaining API services.
5. Update READMEs and deployment documentation after the endpoint behavior is validated.
6. Run the full solution build and test suite.

## Acceptance Criteria

- [ ] Identity Server exposes Scalar at `/scalar` and OpenAPI JSON at `/openapi/v1.json` in Development.
- [ ] Market Data Service exposes Scalar at `/scalar` and OpenAPI JSON at `/openapi/v1.json` in Development.
- [ ] Transaction Processing Service exposes Scalar at `/scalar` and OpenAPI JSON at `/openapi/v1.json` in Development.
- [ ] Trading Instruments Service exposes Scalar at `/scalar` and OpenAPI JSON at `/openapi/v1.json` in Development.
- [ ] Order Execution Service exposes Scalar at `/scalar` and OpenAPI JSON at `/openapi/v1.json` in Development.
- [ ] Swagger UI is no longer registered in any API service startup code.
- [ ] `Swashbuckle.AspNetCore` package references are removed from API service projects unless a specific project still requires them for a non-UI reason.
- [ ] `Scalar.AspNetCore` package references are added to each browser-facing API service project.
- [ ] Launch settings open `scalar` instead of `swagger` for local browser launches.
- [ ] Docker Compose service ports continue to expose each API service from the host machine.
- [ ] Existing protected endpoints can still be tested from the browser by supplying a JWT bearer token through Scalar.
- [ ] Root and service-level README files document Scalar URLs instead of Swagger URLs.
- [ ] Deployment documentation uses Scalar terminology and URLs instead of Swagger terminology and URLs.
- [ ] The full solution builds successfully.
- [ ] The full test suite passes successfully.

## Out Of Scope

- Replacing or redesigning API endpoints
- Changing authentication or authorization rules
- Publishing OpenAPI documents as static generated files
- Adding Spectral or another OpenAPI linting tool
- Exposing Scalar in production without a separate security review
- Replacing the Hangfire dashboard in the Batch Processing Host

## Notes

Microsoft Learn references:

- [Generate OpenAPI documents in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
- [Use OpenAPI documents in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0)

Microsoft guidance used for this PRD:

- ASP.NET Core OpenAPI document generation is provided by `Microsoft.AspNetCore.OpenApi`.
- `AddOpenApi()` registers OpenAPI document generation.
- `MapOpenApi()` exposes the generated OpenAPI document, defaulting to `/openapi/{documentName}.json`.
- Interactive UIs such as Swagger UI and Scalar are not included by default and must be added separately.
- Scalar integration uses the `Scalar.AspNetCore` package and `MapScalarApiReference()`.
- The default Scalar UI route is `/scalar`.
- OpenAPI exploration UIs should be enabled only in development environments unless explicitly secured.
