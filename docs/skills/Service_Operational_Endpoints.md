---
name: service-operational-endpoints
description: Standard for adding /info and /health endpoints to new Pseudo Markets API services
  using the shared PseudoMarkets.Shared.ServiceHelpers library.
version: 1.0.0
author: Shravan Jambukesan
tags:
  - services
  - health-checks
  - info-endpoint
  - shared-library
  - operational-endpoints
---

# Service Operational Endpoints

## Overview

Use this skill whenever a new API service is added to the Pseudo Markets platform. Every API must expose standardized `GET /info` and `GET /health` endpoints through the shared `PseudoMarkets.Shared.ServiceHelpers` library.

## When to Use

- Creating a brand new API service
- Refactoring an existing API to align with the platform operational endpoint standard
- Replacing a custom or legacy health endpoint with the shared implementation

## Requirements

Every API service must:

1. Reference `PseudoMarkets.Shared.ServiceHelpers`
2. Expose unauthenticated `GET /info`
3. Expose unauthenticated `GET /health`
4. Use `AddHealthChecks()` from `Microsoft.Extensions.Diagnostics.HealthChecks`
5. Map the shared endpoints with `app.MapPseudoMarketsOperationalEndpoints<Program>()`
6. Include assembly metadata so `/info` returns the app name, version, and build timestamp

## Shared Library Reference

Add a project reference to:

```xml
<ProjectReference Include="..\..\..\pseudomarkets-nextgen-shared-servicehelpers\src\PseudoMarkets.Shared.ServiceHelpers\PseudoMarkets.Shared.ServiceHelpers.csproj" />
```

Also add this namespace in `Program.cs`:

```csharp
using PseudoMarkets.Shared.ServiceHelpers;
```

## Required .csproj Metadata

Each API `.csproj` should include these properties:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <Version>1.0.0</Version>
  <InformationalVersion>$(Version)</InformationalVersion>
  <Product>Pseudo Markets My Service Name</Product>
  <BuildTimestamp>$([System.DateTime]::UtcNow.ToString("O"))</BuildTimestamp>
</PropertyGroup>
```

And this assembly metadata entry:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
    <_Parameter1>BuildTimestamp</_Parameter1>
    <_Parameter2>$(BuildTimestamp)</_Parameter2>
  </AssemblyAttribute>
</ItemGroup>
```

`Product` is what the shared `/info` endpoint uses as the application name. Set it to the human-readable Pseudo Markets service name.

## Required Program.cs Wiring

Every API should register health checks and map the shared endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var healthChecks = builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapPseudoMarketsOperationalEndpoints<Program>();
app.MapControllers();
```

Do not create custom `/info` or `/health` route handlers unless the shared library is being enhanced for the whole platform.

## Dependency-Specific Health Checks

### Aerospike-backed services

If the service connects directly to Aerospike:

1. Register a shared `IAerospikeClient` in DI
2. Reuse that same client across repositories, caches, and health checks
3. Add the shared `AerospikeClientHealthCheck`

Example:

```csharp
builder.Services.AddSingleton<IAerospikeClient>(sp =>
{
    var configuration = sp.GetRequiredService<AerospikeConfiguration>();
    return new AerospikeClient(configuration.Host, configuration.Port);
});

healthChecks.AddCheck<AerospikeClientHealthCheck>("aerospike");
```

The shared Aerospike health check uses `IAerospikeClient.Connected`. It must not create a new client connection just for health checks.

### PostgreSQL-backed services

If the service connects directly to PostgreSQL through `PseudoMarketsDbContext`, add:

```csharp
healthChecks.AddCheck<DbContextConnectivityHealthCheck<PseudoMarketsDbContext>>("postgres");
```

This uses a lightweight `CanConnectAsync()` check and should remain the standard approach for Postgres-backed APIs.

## Endpoint Behavior

### `/info`

`GET /info` should return:

- Application name
- Version
- Build timestamp

These values come from assembly metadata through `ApplicationInfoProvider`.

### `/health`

`GET /health` should return the JSON-serialized health response produced by the shared library. It should reflect the registered dependency checks and remain lightweight.

## Authentication and Accessibility

- `/info` must remain unauthenticated
- `/health` must remain unauthenticated
- Do not decorate these endpoints with authorization requirements

These endpoints are intended for local development, Docker Compose, orchestration tooling, and uptime monitoring.

## Documentation Follow-up

After adding a new service:

1. Update the service README to mention `/info` and `/health`
2. Update the root README if the service is user-facing or changes platform run instructions
3. Add any new docs under `docs/` to the root solution so they appear in Rider

## Validation Checklist

Before closing out a new service implementation, verify:

1. The service references `PseudoMarkets.Shared.ServiceHelpers`
2. `Product`, `Version`, and `BuildTimestamp` are present in the API `.csproj`
3. `Program.cs` calls `AddHealthChecks()`
4. `Program.cs` calls `app.MapPseudoMarketsOperationalEndpoints<Program>()`
5. Aerospike services use a shared `IAerospikeClient`
6. Postgres services register `DbContextConnectivityHealthCheck<PseudoMarketsDbContext>`
7. `GET /info` and `GET /health` both respond successfully
8. The relevant README files are updated

