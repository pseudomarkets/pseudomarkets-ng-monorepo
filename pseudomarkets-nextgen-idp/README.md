# Pseudo Markets NextGen Identity Server

`pseudomarkets-nextgen-idp` is the identity provider service for the Pseudo Markets platform. It exposes HTTP endpoints for account creation, authentication, and authorization, and stores identity data in Aerospike.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- C# class library for the identity domain and data access
- Aerospike Community Edition as the backing data store
- JWT bearer token generation and validation
- Scalar / OpenAPI for local API exploration
- Docker and Docker Compose for local containerized development
- NUnit, Moq, and Shouldly for unit testing

## Architecture

The project is split into two main application layers:

- `src/PseudoMarkets.Security.IdentityServer.Web`
  Exposes the HTTP API, Scalar API reference UI, exception handling, request contracts, and environment-specific behavior.
- `src/PseudoMarkets.Security.IdentityServer.Core`
  Contains the identity domain logic, Aerospike repository, account provisioning, authentication, authorization, configuration objects, and constants.
- `tests/`
  Contains standalone NUnit test projects for the web and core layers.

At runtime, the flow looks like this:

1. Requests enter the ASP.NET Core web app.
2. Controllers call core managers for account provisioning, authentication, or authorization.
3. Core managers use the Aerospike-backed repository for persistence and lookup.
4. User account creation returns a one-time password reset key while storing only its hashed value in Aerospike.
5. Authentication returns signed JWTs plus opaque refresh tokens.
6. Refresh requests rotate opaque refresh tokens stored in Aerospike with hashed token material at rest.
7. Password reset requests validate a user-provided reset key, rotate it after success, and clear any login lockout state.
8. Authorization validates JWTs, rechecks the current account in Aerospike, and returns the authorized `userId` plus token type from the JWT `account_type` claim.
9. Downstream services can call `POST /api/identity/authorize` through the shared authorization library to centralize access checks.

Aerospike uses the namespace `nsPseudoMarkets` and persists data to disk via the local bind-mounted data directory when running in Docker.

## Project Layout

```text
pseudomarkets-nextgen-idp/
├── compose.yaml
├── src/
│   ├── PseudoMarkets.Security.IdentityServer.Core/
│   └── PseudoMarkets.Security.IdentityServer.Web/
├── tests/
│   ├── PseudoMarkets.Security.IdentityServer.Core.Tests/
│   └── PseudoMarkets.Security.IdentityServer.Web.Tests/
└── PseudoMarkets.Security.IdentityServer.sln
```

Shared Aerospike infrastructure now lives at the repository root:

- `../infrastructure/aerospike/aerospike.conf`

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

### 3. Create the shared local secrets file

From the repository root:

```bash
cp .env.example .env
```

Then set at least:

- `JwtConfiguration__Key`
- `IdentitySecurity__SystemAccountBypassKey`

The identity server now loads the shared root `.env` file automatically for local non-Docker runs.

### 4. Run the web project

From the `pseudomarkets-nextgen-idp` folder:

```bash
dotnet run --project src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj
```

By default, the launch settings use:

- `https://localhost:7092`
- `http://localhost:5051`

Scalar API reference UI is available at:

- [https://localhost:7092/scalar](https://localhost:7092/scalar)

The OpenAPI document is available at:

- [https://localhost:7092/openapi/v1.json](https://localhost:7092/openapi/v1.json)

The market data service expects to call the IDP authorization endpoint at `http://localhost:5051/api/identity/authorize` during local non-Docker development.

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
- Scalar API reference UI: [http://localhost:8080/scalar](http://localhost:8080/scalar)
- OpenAPI document: [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json)
- Aerospike: `localhost:3000`

### Notes about the Docker setup

- The Compose file waits for Aerospike to become healthy before starting the identity server.
- The web container uses `Aerospike__Host=aerospike`, so it talks to the database over the Compose network instead of `localhost`.
- Aerospike data is persisted in the shared repo-root directory `../.docker-data/aerospike`.
- The Compose stack runs the identity server in `Development` mode so Scalar and development-only flows are available locally.
- The service-local Compose file pins Aerospike to `linux/arm64`, which keeps it aligned with Apple Silicon / M-series development machines.
- The JWT signing key is read from the shared repo-root `.env` file through `../.env`.

## Configuration

### Local development configuration

`src/PseudoMarkets.Security.IdentityServer.Web/appsettings.Development.json` contains the default local development values for:

- Aerospike host/port
- JWT issuer
- JWT audience

Secrets are centralized in the repository-root `.env` file instead of committed appsettings files.

### Container configuration

When running in Docker Compose, the web container overrides configuration with environment variables:

- `Aerospike__Host`
- `Aerospike__Port`
- `JwtConfiguration__Issuer`
- `JwtConfiguration__Audience`
- `JwtConfiguration__Key`
- `IdentitySecurity__SystemAccountBypassKey`

The Compose files load `JwtConfiguration__Key` from the shared repo-root `.env` file.

## API Overview

Current primary endpoints include:

- `GET /info`
  Returns the service name, version, and build timestamp.
- `GET /health`
  Returns the standardized JSON health payload for the identity server, including Aerospike connectivity from the shared Aerospike client.
- `POST /api/identity/create`
  Creates a `USER` account by default and returns a one-time password reset key for user accounts. `SYSTEM` account creation is allowed in Development, or outside Development when `X-PseudoMarkets-System-Key` matches the configured dedicated system-account bypass key. SYSTEM accounts do not receive a password reset key.
- `POST /api/identity/authenticate`
  Validates credentials and returns an access token, access-token expiration, refresh token, and refresh-token expiration.
- `POST /api/identity/refresh`
  Accepts a refresh token, validates and rotates it, and returns a new access token plus a replacement refresh token.
- `POST /api/identity/reset-password`
  Accepts a `loginId`, one-time password reset key, and `newPassword`. On success, it updates the password, clears any lockout state, rotates the reset key, and returns the new one-time password reset key.
- `POST /api/identity/authorize`
  Validates a JWT, rechecks current account status and roles, and returns the authorized `userId` and `tokenType`. This is the endpoint consumed by the shared authorization library and downstream platform services.

## Security Notes

- Authentication and refresh endpoints are rate limited.
- Repeated failed login attempts trigger a temporary account lockout.
- Password reset keys are hashed at rest and rotated after every successful password reset.
- Refresh-token consumption is atomic so one refresh token cannot be rotated successfully more than once.
- The system-account bypass secret is separate from the JWT signing key.

Use Scalar to inspect request and response schemas interactively.

## Build

From the monorepo root:

```bash
dotnet build pseudomarkets-nextgen-idp/PseudoMarkets.Security.IdentityServer.sln
```

## Test

From the monorepo root:

```bash
dotnet test pseudomarkets-nextgen-idp/PseudoMarkets.Security.IdentityServer.sln -m:1
```

## Troubleshooting

### Scalar is not available

- Non-Docker local runs expose Scalar at `https://localhost:7092/scalar`.
- Docker Compose exposes Scalar at `http://localhost:8080/scalar`.
- Scalar is enabled only in Development mode.

### The app cannot connect to Aerospike

- Verify Aerospike is running on `localhost:3000` for non-Docker runs.
- Check `GET /health` to confirm whether the shared Aerospike client reports `Healthy` or `Unhealthy`.
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
