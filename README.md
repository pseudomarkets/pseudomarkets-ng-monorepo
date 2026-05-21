# Pseudo Markets NextGen Platform

Pseudo Markets NextGen is a .NET-based stock trading simulation platform. This monorepo contains the core backend services, shared libraries, database model, and local infrastructure needed to run the platform services together.

## Tech Stack

- .NET 10 and ASP.NET Core Web API
- C# class libraries for shared platform concerns
- Aerospike for identity-server account storage and market-data caching
- PostgreSQL for relational platform data
- Entity Framework Core with Npgsql for PostgreSQL access and migrations
- Docker and Docker Compose for local orchestration
- Scalar / OpenAPI for browser-based API exploration
- NUnit, Moq, and Shouldly for tests

## Architecture

The platform is split into focused services and shared libraries:

- `pseudomarkets-nextgen-idp`
  Identity provider for account creation, authentication, JWT generation, and centralized authorization checks.
- `pseudomarkets-nextgen-marketdata`
  Market data API for quotes, detailed quotes, and indices. It uses Finnhub for quote data, Yahoo Finance for U.S. indices, and Aerospike for caching.
- `pseudomarkets-nextgen-transaction-processing`
  Write-side transaction processor for cash movements, trade postings, voids, settled/unsettled balances, settled/unsettled positions, lots, and settlement-date calculation.
- `pseudomarkets-nextgen-instrument-db`
  Trading instrument reference-data API for creating tradable instruments, retrieving instruments by symbol, and updating closing prices.
- `pseudomarkets-nextgen-order-execution`
  Order entry and simulated market-order execution service. It validates tradable symbols, executes market-hour submissions immediately, queues after-hours submissions for later processing, posts fills to Transaction Processing, and persists order state in PostgreSQL.
- `pseudomarkets-nextgen-batch-processing`
  Shared Hangfire-based batch-processing host for scheduling and running recurring platform jobs against the shared PostgreSQL server, including the Hangfire dashboard UI and the market-open queued-order execution job.
- `pseudomarkets-nextgen-shared-auth`
  Shared authorization client and filters used by services that delegate authorization to the IDP, including propagation of authorized user ID and token type metadata.
- `pseudomarkets-nextgen-shared-entities`
  Shared EF Core entity model, `PseudoMarketsDbContext`, migrations, and platform reference data for `pseudomarkets_db`.
- `pseudomarkets-nextgen-shared-servicehelpers`
  Shared operational endpoint and health-check helpers used by the platform APIs for standardized `/info` and `/health` behavior.
- `infrastructure/aerospike`
  Shared Aerospike configuration.
- `infrastructure/postgres`
  Shared PostgreSQL scripts and operational assets, including committed trading-instrument seed SQL.

## Service Ports

These are the default HTTP ports exposed by Docker Compose. For local non-Docker runs, the same HTTP ports are used for every service except the IDP, which keeps its local HTTP endpoint on `http://localhost:5051` so dependent services can use the default development configuration without overrides.

- IDP Scalar: [http://localhost:8080/scalar](http://localhost:8080/scalar)
- Market Data Scalar: [http://localhost:8081/scalar](http://localhost:8081/scalar)
- Transaction Processing Scalar: [http://localhost:8082/scalar](http://localhost:8082/scalar)
- Trading Instruments Scalar: [http://localhost:8083/scalar](http://localhost:8083/scalar)
- Order Execution Scalar: [http://localhost:8084/scalar](http://localhost:8084/scalar)
- Batch Processing Dashboard: [http://localhost:8085/hangfire](http://localhost:8085/hangfire)
- Aerospike: `localhost:3000`
- PostgreSQL: `localhost:5432`

## Operational Endpoints

Each HTTP host now exposes unauthenticated operational routes:

- `GET /info`
  Returns the application name, version, and build timestamp from the built service artifact.
- `GET /health`
  Returns a JSON-serialized health payload based on ASP.NET Core health checks.

Dependency health is lightweight and service-specific:

- IDP and Market Data report Aerospike connectivity from the shared `AerospikeClient`
- Transaction Processing, Trading Instruments, Order Execution, and Batch Processing report PostgreSQL connectivity from the shared EF Core DbContext

## Configuration

Create a root `.env` file before running the Docker stack:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Set these values in `.env`:

- `JwtConfiguration__Key`
- `IdentitySecurity__SystemAccountBypassKey`
- `FinnHub__ApiKey`
- `Postgres__Password`
- `OrderExecution__SystemAccountLoginId`
- `OrderExecution__SystemAccountPassword`
- `QueuedOrderExecution__SystemAccountLoginId`
- `QueuedOrderExecution__SystemAccountPassword`

The Docker stack loads this shared `.env` file into all services. PostgreSQL uses the database name `pseudomarkets_db`.

The batch host uses the queued-order execution system credentials to authenticate with the IDP and submit queued orders back through Order Execution at market open while preserving the original order `userId`.

## Run With Docker

From the repository root:

```bash
docker compose -f compose.yaml up -d --build
```

Stop the stack:

```bash
docker compose -f compose.yaml down
```

Docker data is persisted under:

- `./.docker-data/aerospike`
- `./.docker-data/postgres`

If PostgreSQL was initialized before the database was renamed to `pseudomarkets_db`, recreate `./.docker-data/postgres` or manually create `pseudomarkets_db` in the existing local PostgreSQL instance.

## Auth Flow For Browser API Testing

1. Open the IDP Scalar UI.
2. Create or authenticate an account.
3. If you create a `USER` account, note the returned password reset key. The IDP only shows it at sign-up time or immediately after a successful password reset.
4. Copy the returned JWT. The IDP also returns a refresh token that frontend or service clients can use to renew access tokens before the one-hour access-token lifetime expires.
5. If needed, use the IDP `POST /api/identity/reset-password` endpoint with `loginId`, `passwordResetKey`, and `newPassword` to rotate the password and receive a new one-time reset key.
6. Open the Scalar UI for the service you want to test.
7. Use Scalar authentication with the Bearer security scheme and paste the JWT.
8. Call protected endpoints.

Market Data and trading-instrument lookup require `VIEW_MARKET_DATA`. Transaction posting and void operations require `UPDATE_TRANSACTIONS`. Trading-instrument create and closing-price update operations require `UPDATE_INSTRUMENTS`. Order submission requires `EXECUTE_TRADES`.

## Run Without Docker

Start dependencies first:

- Aerospike on `localhost:3000`
- PostgreSQL on `localhost:5432` with database `pseudomarkets_db`

Then run services from the repository root:

```bash
dotnet run --project pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj
dotnet run --project pseudomarkets-nextgen-marketdata/src/PseudoMarkets.MarketData.Service/PseudoMarkets.MarketData.Service.csproj
dotnet run --project pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service/PseudoMarkets.TransactionProcessing.Service.csproj
dotnet run --project pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Service/PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj
dotnet run --project pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Service/PseudoMarkets.OrderExecution.Service.csproj
dotnet run --project pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host/PseudoMarkets.Platform.Batch.Host.csproj
```

The services load the root `.env` file for local development secrets.

Default local non-Docker URLs:

- IDP: `http://localhost:5051` or `https://localhost:7092`
- Market Data: `http://localhost:8081` or `https://localhost:7228`
- Transaction Processing: `http://localhost:8082` or `https://localhost:7282`
- Trading Instruments: `http://localhost:8083` or `https://localhost:7183`
- Order Execution: `http://localhost:8084` or `https://localhost:7284`
- Batch Processing: `http://localhost:8085` or `https://localhost:7285`

To seed trading instruments without Docker, apply shared EF migrations by starting the Trading Instruments service, then run the SQL scripts in this order:

```bash
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f infrastructure/postgres/trading-instruments/001_seed_nasdaq_listed.sql
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f infrastructure/postgres/trading-instruments/002_seed_nyse_arca_etfs.sql
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f infrastructure/postgres/trading-instruments/003_seed_nyse_listed.sql
```

## Build And Test

Build everything:

```bash
dotnet build PseudoMarkets.NextGen.Platform.sln
```

Run all tests:

```bash
dotnet test PseudoMarkets.NextGen.Platform.sln -m:1
```

Validate Compose configuration:

```bash
docker compose -f compose.yaml config
```

## Database Migrations

The shared EF Core model and migrations live in:

```text
pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities
```

`PseudoMarketsDbContext` is applied at transaction-processing, trading-instruments, and order-execution startup. The batch-processing host uses the same database connection for Hangfire PostgreSQL storage and health reporting. Current relational tables include transaction posting tables, settled/unsettled balance and position projection tables, trade lots, market holidays, trading instruments, order executions, queued orders, Hangfire storage tables, and EF migration history.
