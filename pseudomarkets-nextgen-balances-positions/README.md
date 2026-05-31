# Pseudo Markets NextGen Balances and Positions

`pseudomarkets-nextgen-balances-positions` is the read-side portfolio snapshot service for the Pseudo Markets platform. It reads settled and unsettled balances and positions directly from the shared PostgreSQL database, enforces IDP-backed authorization, and enriches open positions with current market prices from the Market Data Service.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- PostgreSQL with EF Core and `Npgsql`
- Shared `PseudoMarketsDbContext` from `pseudomarkets-nextgen-shared-entities`
- Shared IDP-backed authorization from `pseudomarkets-nextgen-shared-auth`
- Market Data integration for quote enrichment
- Scalar / OpenAPI
- NUnit, Moq, and Shouldly

## API

- `GET /info`
  Returns the service name, version, and build timestamp.
- `GET /health`
  Returns the standardized JSON health payload and includes a lightweight PostgreSQL connectivity check.
- `POST /balances`
  Requires `VIEW_TRANSACTIONS`. Returns aggregate, settled, and unsettled cash balances for the requested `userId`.
- `POST /positions`
  Requires `VIEW_TRANSACTIONS`. Returns aggregate, settled, and unsettled positions for the requested `userId`, including current market value and unrealized gain/loss where quotes are available.

`USER` tokens may only request their own `userId`. `SYSTEM` tokens may request any `userId`.

When quote retrieval fails for one or more symbols, `POST /positions` still returns `200 OK`, marks the affected positions with `isQuoteAvailable = false`, sets quote-derived fields to `null`, and returns warning records describing the affected symbols.

Scalar is available at [http://localhost:8086/scalar](http://localhost:8086/scalar) when running through Docker, or [https://localhost:7286/scalar](https://localhost:7286/scalar) for non-Docker local runs. The OpenAPI document is available at [http://localhost:8086/openapi/v1.json](http://localhost:8086/openapi/v1.json) or [https://localhost:7286/openapi/v1.json](https://localhost:7286/openapi/v1.json).

## Data Access

The service uses the shared PostgreSQL database `pseudomarkets_db` and reads from:

- `account_balances`
- `positions`

It does not own migrations and does not mutate database state. Shared schema setup is expected to be handled by the existing write-side services that apply `PseudoMarketsDbContext` migrations at startup.

## Run With Docker

From the repository root:

```bash
docker compose -f compose.yaml up -d --build pseudomarkets.balancesandpositions.service
```

Or run the full platform:

```bash
docker compose -f compose.yaml up -d --build
```

The service is exposed on `localhost:8086`.

## Run Without Docker

Prerequisites:

- PostgreSQL on `localhost:5432` with database `pseudomarkets_db`
- the Identity Server running locally for authorization
- the Market Data Service running locally for quote enrichment
- the repository-root `.env` file with system-account credentials

From the repository root:

```bash
dotnet run --project pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Service/PseudoMarkets.BalancesAndPositions.Service.csproj
```

By default, the launch settings use:

- `https://localhost:7286`
- `http://localhost:8086`

The default development configuration expects:

- Identity Server at `https://localhost:7092`
- Market Data Service at `https://localhost:7091`

If you run dependencies on different ports, override:

- `IdentityAuthorization__IdentityServerBaseUrl`
- `BalancesAndPositions__MarketDataBaseUrl`

## Configuration

Set these values in the shared repository-root `.env` file for Docker or local development:

- `BalancesAndPositions__SystemAccountLoginId`
- `BalancesAndPositions__SystemAccountPassword`
- `Postgres__Password`

The service also reads:

- `ConnectionStrings__PseudoMarketsDb`
- `IdentityAuthorization__IdentityServerBaseUrl`
- `BalancesAndPositions__MarketDataBaseUrl`
- `BalancesAndPositions__TimeoutSeconds`
- `BalancesAndPositions__TokenRefreshBufferSeconds`

The system account is used only for service-to-service quote retrieval from Market Data.

## Build And Test

```bash
dotnet build PseudoMarkets.BalancesAndPositions.sln
dotnet test PseudoMarkets.BalancesAndPositions.sln -m:1
```
