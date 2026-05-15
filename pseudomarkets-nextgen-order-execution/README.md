# Pseudo Markets NextGen Order Execution

`pseudomarkets-nextgen-order-execution` is the order-entry and immediate simulated execution service for the Pseudo Markets platform. The core foundation supports market orders for equities, validates settled buying power or settled sellable quantity, fills accepted orders at the latest quote price, posts completed fills to Transaction Processing, and persists order execution state in PostgreSQL.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- PostgreSQL with EF Core and Npgsql
- Shared `PseudoMarketsDbContext` from `pseudomarkets-nextgen-shared-entities`
- Shared IDP-backed authorization from `pseudomarkets-nextgen-shared-auth`
- Downstream calls to Trading Instruments, Market Data, and Transaction Processing
- Swagger / OpenAPI
- Docker and Docker Compose
- NUnit, Moq, and Shouldly

## API

- `GET /info`
  Returns the service name, version, and build timestamp.
- `GET /health`
  Returns the standardized JSON health payload and includes a lightweight PostgreSQL connectivity check through the shared EF Core DbContext.
- `POST /api/orders`
  Requires `EXECUTE_TRADES`.

Example request:

```json
{
  "userId": 1000000001,
  "symbol": "aapl",
  "side": "Buy",
  "quantity": 5,
  "orderType": "Market"
}
```

The service rejects unmapped order fields such as `limitPrice`, `stopPrice`, and `stopLimitPrice`. Only `Market` order type is supported in this foundation.

## Runtime Behavior

Order submission is authorized before business validation. The service reads the authorized `userId` and `tokenType` from the shared authorization context populated by the IDP authorization endpoint. `USER` tokens may submit orders only for the token's authorized user id. `SYSTEM` tokens may submit orders for any payload user id.

The service trims and uppercases symbols, rejects non-alphanumeric symbols before downstream calls, validates tradability through Trading Instruments, reads quotes from Market Data, validates buy orders against `SettledCashBalance`, validates sell orders against symbol-level `SettledQuantity`, and posts completed trade executions to Transaction Processing. It does not directly mutate `account_balances` or `positions`.

For downstream service-to-service calls, Order Execution authenticates with a configured system account, caches the access token and refresh token returned by the IDP, and automatically refreshes the system token before expiration when calling Trading Instruments, Market Data, or Transaction Processing.

## Configuration

Secrets are centralized in the repository-root `.env` file or deployment secrets:

- `ConnectionStrings__PseudoMarketsDb`
- `IdentityAuthorization__IdentityServerBaseUrl`
- `OrderExecution__SystemAccountLoginId`
- `OrderExecution__SystemAccountPassword`
- `OrderExecution__TradingInstrumentsBaseUrl`
- `OrderExecution__MarketDataBaseUrl`
- `OrderExecution__TransactionProcessingBaseUrl`

The configured system account must have downstream roles including `VIEW_MARKET_DATA` and `UPDATE_TRANSACTIONS`.

## Run With Docker

From the repository root:

```bash
docker compose -f compose.yaml up -d --build pseudomarkets.orderexecution.service
```

Or run the full platform:

```bash
docker compose -f compose.yaml up -d --build
```

Swagger is available at [http://localhost:8084/swagger/index.html](http://localhost:8084/swagger/index.html).

## Run Without Docker

Start Aerospike, PostgreSQL, IDP, Market Data, Transaction Processing, and Trading Instruments first. Then run:

```bash
dotnet run --project src/PseudoMarkets.OrderExecution.Service/PseudoMarkets.OrderExecution.Service.csproj
```

The service loads the root `.env` file for local development secrets.

By default, the launch settings use:

- `https://localhost:7284`
- `http://localhost:8084`

Swagger UI is available at [https://localhost:7284/swagger/index.html](https://localhost:7284/swagger/index.html) for non-Docker local runs, or [http://localhost:8084/swagger/index.html](http://localhost:8084/swagger/index.html) through Docker Compose.

## Build And Test

From the repository root:

```bash
dotnet build pseudomarkets-nextgen-order-execution/PseudoMarkets.OrderExecution.sln
dotnet test pseudomarkets-nextgen-order-execution/PseudoMarkets.OrderExecution.sln -m:1
```
