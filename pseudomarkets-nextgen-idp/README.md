# Pseudo Markets NextGen Identity Server

`pseudomarkets-nextgen-idp` is the identity provider service for the Pseudo Markets platform. It exposes HTTP endpoints for account creation, authentication, and authorization, and stores identity data in Aerospike.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- C# class library for the identity domain and data access
- Aerospike Community Edition as the backing data store
- JWT bearer token generation and validation
- Swagger UI / OpenAPI for local API exploration
- Docker and Docker Compose for local containerized development

## Architecture

The project is split into two main application layers:

- `src/PseudoMarkets.Security.IdentityServer.Web`
  Exposes the HTTP API, Swagger UI, exception handling, request contracts, and environment-specific behavior.
- `src/PseudoMarkets.Security.IdentityServer.Core`
  Contains the identity domain logic, Aerospike repository, account provisioning, authentication, authorization, configuration objects, and constants.

At runtime, the flow looks like this:

1. Requests enter the ASP.NET Core web app.
2. Controllers call core managers for account provisioning, authentication, or authorization.
3. Core managers use the Aerospike-backed repository for persistence and lookup.
4. Authentication returns signed JWTs.
5. Authorization validates JWTs and checks the `roles` claim.

Aerospike uses the namespace `nsPseudoMarkets` and persists data to disk via the local bind-mounted data directory when running in Docker.

## Project Layout

```text
pseudomarkets-nextgen-idp/
├── compose.yaml
├── infrastructure/
│   └── aerospike/
│       └── aerospike.conf
├── src/
│   ├── PseudoMarkets.Security.IdentityServer.Core/
│   └── PseudoMarkets.Security.IdentityServer.Web/
└── PseudoMarkets.Security.IdentityServer.sln
```

## Running Without Docker

### Prerequisites

- .NET 10 SDK
- Docker Desktop on Windows or macOS, or Docker Engine with Compose on Linux
- A trusted ASP.NET Core development certificate for HTTPS
- A shell such as PowerShell, Command Prompt, Bash, or Zsh

### 1. Start Aerospike

The simplest local option is to use only the Aerospike service from Compose:

```bash
docker compose -f compose.yaml up -d aerospike
```

This exposes Aerospike on `localhost:3000`, which matches `appsettings.Development.json`.

### 2. Trust the ASP.NET Core HTTPS development certificate

Windows, macOS, and Linux can all use the same .NET command:

```bash
dotnet dev-certs https --trust
```

Depending on your OS, you may be prompted to approve certificate trust through the local certificate store or keychain UI.

### 3. Run the web project

From the `pseudomarkets-nextgen-idp` folder:

```bash
dotnet run --project src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj
```

By default, the launch settings use:

- `https://localhost:7092`
- `http://localhost:5051`

Swagger UI is available at:

- [https://localhost:7092/swagger](https://localhost:7092/swagger)

## Running With Docker Compose

### What Compose Starts

The Compose stack brings up:

- `aerospike`
  Aerospike CE with disk-backed persistence
- `pseudomarkets.security.identityserver.web`
  The ASP.NET Core identity server configured to connect to the Aerospike container

### Start the full stack

```bash
docker compose -f compose.yaml up --build
```

### Run detached

```bash
docker compose -f compose.yaml up -d --build
```

### Stop the stack

```bash
docker compose -f compose.yaml down
```

### Service endpoints

- Identity server: [http://localhost:8080](http://localhost:8080)
- Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- Aerospike: `localhost:3000`

### Notes about the Docker setup

- The Compose file waits for Aerospike to become healthy before starting the identity server.
- The web container uses `Aerospike__Host=aerospike`, so it talks to the database over the Compose network instead of `localhost`.
- Aerospike data is persisted in `./.docker-data/aerospike`.
- The Compose stack runs the identity server in `Development` mode so Swagger UI and development-only flows are available locally.
- The Compose file does not hardcode a container CPU architecture, so Docker can pull the appropriate image variant for Windows, Linux, Intel/AMD, and Apple Silicon environments where the upstream image supports it.

## Configuration

### Local development configuration

`src/PseudoMarkets.Security.IdentityServer.Web/appsettings.Development.json` contains the default local development values for:

- Aerospike host/port
- JWT issuer
- JWT audience
- JWT signing key

### Container configuration

When running in Docker Compose, the web container overrides configuration with environment variables:

- `Aerospike__Host`
- `Aerospike__Port`
- `JwtConfiguration__Issuer`
- `JwtConfiguration__Audience`
- `JwtConfiguration__Key`

If you want different values, update `compose.yaml`.

## API Overview

Current primary endpoints include:

- `POST /api/identity/create`
  Creates a `USER` account by default. `SYSTEM` account creation is allowed in Development, or outside Development when `X-PseudoMarkets-System-Key` matches the configured JWT key.
- `POST /api/identity/authenticate`
  Validates credentials and returns a JWT.
- `POST /api/identity/authorize`
  Validates a JWT and checks whether the requested action is present in the `roles` claim.

Use Swagger UI to inspect request and response schemas interactively.

## Build

From the monorepo root:

```bash
dotnet build PseudoMarkets.Security.IdentityServer.sln
```

## Troubleshooting

### Swagger is not available

- Non-Docker local runs expose Swagger at `https://localhost:7092/swagger`.
- Docker Compose exposes Swagger at `http://localhost:8080/swagger`.
- Swagger is enabled only in Development mode.

### The app cannot connect to Aerospike

- Verify Aerospike is running on `localhost:3000` for non-Docker runs.
- In Docker Compose, verify both containers are up:

```bash
docker compose -f compose.yaml ps
```

### HTTPS certificate warnings locally

Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

### Reset local Aerospike data

Stop the Compose stack and remove the local data directory:

macOS/Linux:

```bash
docker compose -f compose.yaml down
rm -rf ./.docker-data/aerospike
```

Windows PowerShell:

```powershell
docker compose -f compose.yaml down
Remove-Item -Recurse -Force .\.docker-data\aerospike
```
