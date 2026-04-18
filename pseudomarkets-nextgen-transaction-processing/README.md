# Pseudo Markets NextGen Transaction Processing

`pseudomarkets-nextgen-transaction-processing` is the write-side financial posting service for the Pseudo Markets platform. It is responsible for posting trade transactions, cash movements, and compensating void transactions while maintaining internal balance and position state in PostgreSQL.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core with `Npgsql`
- Shared IDP-backed authorization via `PseudoMarkets.Shared.Authorization`
- Swagger UI / OpenAPI
- Docker and Docker Compose
- NUnit, Moq, and Shouldly

## Current Scaffold State

The service is scaffolded with:

- write endpoints for trades, deposits, withdrawals, adjustments, and voids
- PostgreSQL-backed EF Core DbContext and entities
- shared authorization wiring using `UPDATE_TRANSACTIONS`
- Dockerfile and service-local Compose file
- a first-pass domain/service structure ready for implementation

The posting services currently return scaffold placeholder responses while the real persistence and mutation logic is implemented in the next phase.

## Project Layout

```text
pseudomarkets-nextgen-transaction-processing/
├── compose.yaml
├── README.md
├── src/
│   ├── PseudoMarkets.TransactionProcessing.Contracts/
│   ├── PseudoMarkets.TransactionProcessing.Core/
│   ├── PseudoMarkets.TransactionProcessing.Persistence/
│   └── PseudoMarkets.TransactionProcessing.Service/
├── tests/
│   └── PseudoMarkets.TransactionProcessing.Tests/
└── PseudoMarkets.TransactionProcessing.sln
```

## Running Without Docker

### Prerequisites

- .NET 10 SDK
- PostgreSQL 17 or compatible local instance
- the identity server running locally for authorization
- shared repo-root `.env` file for any local secrets

### 1. Create the shared local env file

From the repository root:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

### 2. Start PostgreSQL

Use your local PostgreSQL instance, or start the service-local Compose stack’s database only:

```bash
docker compose -f compose.yaml up -d postgres
```

### 3. Start the identity server

All write endpoints are protected, so local non-Docker development also requires the IDP to be running. From the repository root:

```bash
dotnet run --project pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj
```

By default, the transaction processing service will call the IDP at `http://localhost:5051/api/identity/authorize`.

### 4. Run the service

From the service folder:

```bash
dotnet run --project src/PseudoMarkets.TransactionProcessing.Service/PseudoMarkets.TransactionProcessing.Service.csproj
```

Swagger UI will be available from the local launch profile once the app is running. Use the Swagger `Authorize` button with a JWT issued by the IDP before calling the write endpoints.

## Running With Docker Compose

The service-local Compose stack brings up:

- `aerospike`
  Aerospike CE with disk-backed persistence for the identity service
- `pseudomarkets.security.identityserver.web`
  The ASP.NET Core identity server used as the authorization source
- `postgres`
  PostgreSQL 17 with a local bind-mounted data directory
- `pseudomarkets.transactionprocessing.service`
  The ASP.NET Core transaction processing service configured to connect to PostgreSQL and call the IDP authorization endpoint

```bash
docker compose -f compose.yaml up --build
```

Detached:

```bash
docker compose -f compose.yaml up -d --build
```

Endpoints:

- Identity server Swagger UI: [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)
- Transaction processing Swagger UI: [http://localhost:8082/swagger/index.html](http://localhost:8082/swagger/index.html)
- PostgreSQL: `localhost:5432`
- Aerospike: `localhost:3000`

Use the IDP Swagger UI to authenticate first, then paste the returned JWT into the transaction processing Swagger UI `Authorize` dialog before calling the write endpoints.

## Authorization

All write endpoints are protected using the shared authorization library and require the `UPDATE_TRANSACTIONS` action from the IDP.

For the scaffold phase, the service exposes only write endpoints and health checks. Balance and position reads are intentionally out of scope for this service and will live in a future read-oriented service.

## API Surface

- `POST /api/transactions/trades`
- `POST /api/transactions/cash/deposit`
- `POST /api/transactions/cash/withdrawal`
- `POST /api/transactions/cash/adjustment`
- `POST /api/transactions/{transactionId}/void`
- `GET /health`

## Build

From the repository root:

```bash
dotnet build pseudomarkets-nextgen-transaction-processing/PseudoMarkets.TransactionProcessing.sln
```

## Test

From the repository root:

```bash
dotnet test pseudomarkets-nextgen-transaction-processing/PseudoMarkets.TransactionProcessing.sln -m:1
```
