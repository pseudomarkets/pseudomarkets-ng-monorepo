# Pseudo Markets NextGen Transaction Processing

This document defines the concrete scaffold spec for the next-generation transaction processing service before implementation begins.

## Purpose

`pseudomarkets-nextgen-transaction-processing` will be the write-side financial posting engine for the Pseudo Markets platform.

It is responsible for:

- posting trade-generated financial transactions
- posting cash movements
- maintaining internal account cash state
- maintaining internal position state
- maintaining internal position lots for cost basis and inventory tracking
- voiding previously posted transactions by creating offsetting transactions

It is not responsible for:

- querying balances for end users
- querying positions for end users
- fetching market data
- order routing
- authentication or token issuance

Those concerns will live in other services.

Initial scope note:

- short-sale transaction posting and short-position handling are intentionally out of scope for the first implementation

## Proposed Folder Structure

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

## Project Definitions

### `PseudoMarkets.TransactionProcessing.Service`

Purpose:

- ASP.NET Core Web API host
- Swagger/OpenAPI
- configuration binding
- health checks
- shared authorization wiring
- controller layer only

References:

- `PseudoMarkets.TransactionProcessing.Contracts`
- `PseudoMarkets.TransactionProcessing.Core`
- `PseudoMarkets.TransactionProcessing.Persistence`
- `PseudoMarkets.Shared.Authorization`

### `PseudoMarkets.TransactionProcessing.Contracts`

Purpose:

- request and response DTOs
- public enum contracts that are safe to share between layers

No persistence logic should live here.

### `PseudoMarkets.TransactionProcessing.Core`

Purpose:

- posting orchestration
- transaction validation
- voiding logic
- balance mutation rules
- position mutation rules
- lot mutation rules
- domain interfaces
- domain exceptions

References:

- `PseudoMarkets.TransactionProcessing.Contracts`

### `PseudoMarkets.TransactionProcessing.Persistence`

Purpose:

- EF Core DbContext
- entity classes
- fluent mappings
- repositories
- migrations
- database transactions

References:

- `PseudoMarkets.TransactionProcessing.Core`

### `PseudoMarkets.TransactionProcessing.Tests`

Purpose:

- unit tests for posting flows
- unit tests for void flows
- unit tests for validation and authorization behavior

Frameworks:

- NUnit
- Moq
- Shouldly

## Service Authorization

The service will reuse `PseudoMarkets.Shared.Authorization`.

Required actions:

- `UPDATE_TRANSACTIONS`
- `VIEW_TRANSACTIONS`

Usage:

- all write endpoints, including void, should require `UPDATE_TRANSACTIONS`
- any future read-only endpoints should require `VIEW_TRANSACTIONS`

## API Surface

This service is intentionally write-focused.

### `POST /api/transactions/trades`

Posts a completed trade execution.

### `POST /api/transactions/cash/deposit`

Posts a deposit.

### `POST /api/transactions/cash/withdrawal`

Posts a withdrawal.

### `POST /api/transactions/cash/adjustment`

Posts an internal/admin balance correction.

### `POST /api/transactions/{transactionId:guid}/void`

Voids a previously posted transaction by creating offsetting entries.

### `GET /health`

Standard service health endpoint.

## Endpoint Contracts

### Trade Posting Request

File:

- `Contracts/Transactions/PostTradeTransactionRequest.cs`

Proposed shape:

```csharp
public sealed class PostTradeTransactionRequest
{
    public required string IdempotencyKey { get; init; }
    public required long AccountId { get; init; }
    public required string Symbol { get; init; }
    public required TradeSide TradeSide { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal ExecutionPrice { get; init; }
    public required decimal GrossAmount { get; init; }
    public required decimal Fees { get; init; }
    public required decimal NetAmount { get; init; }
    public required DateTime ExecutedAtUtc { get; init; }
    public required string ExternalExecutionId { get; init; }
}
```

Notes:

- `NetAmount` is provided by the upstream order execution service.
- The transaction processor does not call Market Data.
- initial scope supports `TradeSide.Buy` and `TradeSide.Sell` only

### Cash Deposit Request

File:

- `Contracts/Transactions/PostCashDepositRequest.cs`

```csharp
public sealed class PostCashDepositRequest
{
    public required string IdempotencyKey { get; init; }
    public required long AccountId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required string ExternalReferenceId { get; init; }
}
```

### Cash Withdrawal Request

File:

- `Contracts/Transactions/PostCashWithdrawalRequest.cs`

```csharp
public sealed class PostCashWithdrawalRequest
{
    public required string IdempotencyKey { get; init; }
    public required long AccountId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required string ExternalReferenceId { get; init; }
}
```

### Cash Adjustment Request

File:

- `Contracts/Transactions/PostCashAdjustmentRequest.cs`

```csharp
public sealed class PostCashAdjustmentRequest
{
    public required string IdempotencyKey { get; init; }
    public required long AccountId { get; init; }
    public required decimal Amount { get; init; }
    public required CashAdjustmentDirection Direction { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required string ReasonCode { get; init; }
}
```

### Void Transaction Request

File:

- `Contracts/Transactions/VoidTransactionRequest.cs`

```csharp
public sealed class VoidTransactionRequest
{
    public required string IdempotencyKey { get; init; }
    public required DateTime VoidedAtUtc { get; init; }
    public required string ReasonCode { get; init; }
}
```

### Common Success Response

File:

- `Contracts/Transactions/TransactionCommandResponse.cs`

```csharp
public sealed class TransactionCommandResponse
{
    public required Guid PostingBatchId { get; init; }
    public required Guid TransactionId { get; init; }
    public required string Status { get; init; }
    public required string TransactionDescription { get; init; }
    public string? Message { get; init; }
}
```

For voids, `TransactionId` should be the new reversing transaction ID.

## Domain Enums

Recommended enums:

- `TransactionKind`
  - `TradeBuy`
  - `TradeSell`
  - `CashDeposit`
  - `CashWithdrawal`
  - `CashAdjustment`
  - `Void`
- `LedgerDirection`
  - `Debit`
  - `Credit`
- `PostingBatchStatus`
  - `Pending`
  - `Posted`
  - `Rejected`
  - `Voided`
- `TransactionStatus`
  - `Posted`
  - `Voided`
- `TradeSide`
  - `Buy`
  - `Sell`
- `PositionSide`
  - `Long`
- `LotEntryType`
  - `Open`
  - `Close`
  - `Void`
- `CashAdjustmentDirection`
  - `Credit`
  - `Debit`

## Persistence Model

PostgreSQL will be the source of truth.

### Table: `posting_batches`

Purpose:

- tracks one inbound command
- provides idempotency anchor
- groups related writes

Columns:

- `id` `uuid` PK
- `idempotency_key` `varchar(100)` unique not null
- `account_id` `bigint` not null
- `request_type` `varchar(50)` not null
- `status` `varchar(20)` not null
- `created_at_utc` `timestamp with time zone` not null
- `completed_at_utc` `timestamp with time zone` null
- `error_message` `text` null

### Table: `ledger_transactions`

Purpose:

- immutable financial ledger entries

Columns:

- `id` `bigserial` PK
- `transaction_id` `uuid` unique not null
- `posting_batch_id` `uuid` not null FK
- `account_id` `bigint` not null
- `transaction_kind` `varchar(50)` not null
- `direction` `varchar(20)` not null
- `amount` `numeric(18, 4)` not null
- `transaction_description` `varchar(200)` not null
- `status` `varchar(20)` not null
- `occurred_at_utc` `timestamp with time zone` not null
- `voids_transaction_id` `uuid` null
- `external_reference_id` `varchar(100)` null
- `created_at_utc` `timestamp with time zone` not null

Constraints:

- unique index on `transaction_id`
- index on `account_id`
- index on `posting_batch_id`
- index on `voids_transaction_id`

### Table: `trade_executions`

Purpose:

- stores trade-specific execution details for trade ledger transactions

Columns:

- `id` `bigserial` PK
- `transaction_id` `uuid` not null FK to `ledger_transactions.transaction_id`
- `account_id` `bigint` not null
- `external_execution_id` `varchar(100)` not null
- `symbol` `varchar(32)` not null
- `trade_side` `varchar(20)` not null
- `quantity` `numeric(18, 6)` not null
- `execution_price` `numeric(18, 6)` not null
- `gross_amount` `numeric(18, 4)` not null
- `fees` `numeric(18, 4)` not null
- `net_amount` `numeric(18, 4)` not null
- `executed_at_utc` `timestamp with time zone` not null
- `created_at_utc` `timestamp with time zone` not null

Constraints:

- unique index on `external_execution_id`
- index on `account_id`
- index on `symbol`

### Table: `cash_movements`

Purpose:

- stores cash-specific details for non-trade movements

Columns:

- `id` `bigserial` PK
- `transaction_id` `uuid` not null FK to `ledger_transactions.transaction_id`
- `account_id` `bigint` not null
- `movement_type` `varchar(30)` not null
- `external_reference_id` `varchar(100)` null
- `reason_code` `varchar(50)` null
- `occurred_at_utc` `timestamp with time zone` not null
- `created_at_utc` `timestamp with time zone` not null

### Table: `account_balances`

Purpose:

- internal write-side balance state
- not exposed from this service

Columns:

- `account_id` `bigint` PK
- `cash_balance` `numeric(18, 4)` not null
- `updated_at_utc` `timestamp with time zone` not null

### Table: `positions`

Purpose:

- internal write-side current holdings
- not exposed from this service

Columns:

- `id` `bigserial` PK
- `account_id` `bigint` not null
- `symbol` `varchar(32)` not null
- `position_side` `varchar(20)` not null
- `quantity` `numeric(18, 6)` not null
- `cost_basis_total` `numeric(18, 4)` not null
- `updated_at_utc` `timestamp with time zone` not null

Constraints:

- unique index on `(account_id, symbol)`

### Table: `position_lots`

Purpose:

- internal inventory tracking for FIFO and void safety

Columns:

- `id` `bigserial` PK
- `account_id` `bigint` not null
- `symbol` `varchar(32)` not null
- `opening_transaction_id` `uuid` not null
- `closing_transaction_id` `uuid` null
- `lot_entry_type` `varchar(20)` not null
- `quantity_opened` `numeric(18, 6)` not null
- `quantity_remaining` `numeric(18, 6)` not null
- `price` `numeric(18, 6)` not null
- `opened_at_utc` `timestamp with time zone` not null
- `updated_at_utc` `timestamp with time zone` not null

Indexes:

- `(account_id, symbol)`
- `opening_transaction_id`
- `closing_transaction_id`

## First Migration Scope

The initial EF migration should create:

- `posting_batches`
- `ledger_transactions`
- `trade_executions`
- `cash_movements`
- `account_balances`
- `positions`
- `position_lots`

It should also include:

- all primary keys
- all foreign keys
- unique constraints on:
  - `posting_batches.idempotency_key`
  - `ledger_transactions.transaction_id`
  - `trade_executions.external_execution_id`
  - `positions(account_id, symbol)`

## Write-Side Processing Rules

### Trade Posting

Input assumptions:

- upstream order execution already computed `grossAmount`, `fees`, and `netAmount`
- no market data call is needed

Flow:

1. Validate request and idempotency key.
2. Reject duplicate idempotency key.
3. Create `posting_batch`.
4. Create `ledger_transaction` with new GUID `transaction_id`.
5. Create `trade_execution`.
6. Mutate `account_balances`.
7. Mutate `positions`.
8. Mutate `position_lots`.
9. Mark batch `Posted`.
10. Commit in one database transaction.

The service should auto-generate `transaction_description`.

Examples:

- `TRADE BUY AAPL $100.00`
- `TRADE SELL AAPL $125.25`

Balance behavior:

- `Buy`: debit cash by `netAmount`
- `Sell`: credit cash by `netAmount`

### Deposit Posting

Flow:

1. Validate request.
2. Create `posting_batch`.
3. Create `ledger_transaction` with GUID.
4. Create `cash_movement`.
5. Credit `account_balances`.
6. Mark batch `Posted`.
7. Commit.

Generated description example:

- `CASH DEPOSIT $100.00`

### Withdrawal Posting

Flow:

1. Validate request.
2. Validate sufficient funds.
3. Create `posting_batch`.
4. Create `ledger_transaction` with GUID.
5. Create `cash_movement`.
6. Debit `account_balances`.
7. Mark batch `Posted`.
8. Commit.

Generated description example:

- `CASH WITHDRAWAL $100.00`

### Adjustment Posting

Flow mirrors deposit or withdrawal depending on direction.

Generated description examples:

- `CASH ADJUSTMENT CREDIT $50.00`
- `CASH ADJUSTMENT DEBIT $50.00`

## Voiding Rules

Void is implemented as compensation, never deletion.

### Endpoint

- `POST /api/transactions/{transactionId}/void`

### Void Flow

1. Load original `ledger_transaction` by `transactionId`.
2. Reject if not found.
3. Reject if original transaction is already voided.
4. Reject if a reversing transaction already references it.
5. Create a new `posting_batch` using the void request idempotency key.
6. Create a new reversing `ledger_transaction` with a new GUID.
7. Set the reversing transaction `voids_transaction_id` to the original GUID.
8. Update internal balance state with the opposite direction and same amount.
9. If original was trade-based, reverse the related position and lot mutations.
10. Mark original transaction status `Voided`.
11. Mark new batch `Posted`.
12. Commit atomically.

### Void Examples

If original:

- `TradeBuy`, `Debit`, `100.00`

then void creates:

- `Void`, `Credit`, `100.00`

If original:

- `CashDeposit`, `Credit`, `500.00`

then void creates:

- `Void`, `Debit`, `500.00`

Generated description examples:

- `VOID CASH DEPOSIT $500.00`
- `VOID TRADE BUY AAPL $100.00`

### Trade Void Safety

Initial implementation should use strict reversal rules.

Trade void should be rejected if:

- later lots have consumed the original lot in a way that cannot be safely reversed
- later transactions depend on the original position state

This keeps the first version safe and auditable.

## Core Service Interfaces

Recommended interfaces:

- `ITradeTransactionPostingService`
- `ICashMovementPostingService`
- `IVoidTransactionService`
- `ITransactionDescriptionService`
- `IBalanceMutationService`
- `IPositionMutationService`
- `ILotMutationService`
- `ITransactionRepository`
- `IPostingBatchRepository`
- `IBalanceRepository`
- `IPositionRepository`
- `ILotRepository`

## Controller Layout

Recommended controllers:

- `TransactionsController`
  - `POST /api/transactions/trades`
  - `POST /api/transactions/{transactionId}/void`
- `CashTransactionsController`
  - `POST /api/transactions/cash/deposit`
  - `POST /api/transactions/cash/withdrawal`
  - `POST /api/transactions/cash/adjustment`
- `HealthController`
  - optional only if not using mapped health checks directly

## Configuration Model

Configuration sections:

- `ConnectionStrings:PseudoMarketsDb`
- `IdentityAuthorization`
- `Swagger`
- `Logging`

Example `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "PseudoMarketsDb": "Host=localhost;Port=5432;Database=pseudomarkets_db;Username=postgres;Password=postgres"
  },
  "IdentityAuthorization": {
    "IdentityServerBaseUrl": "http://localhost:5051",
    "AuthorizeEndpointPath": "/api/identity/authorize",
    "TimeoutSeconds": 10
  }
}
```

Secrets should continue to come from the repo-root `.env` file for Docker and local development.

Suggested `.env` additions:

- `ConnectionStrings__PseudoMarketsDb=Host=postgres;Port=5432;Database=pseudomarkets_db;Username=postgres;Password=postgres`
- `Postgres__Password=postgres`

## Docker Compose Layout

Service-local `compose.yaml` should include:

- `postgres`
- `pseudomarkets.transactionprocessing.service`

Initial proposal:

```yaml
services:
  postgres:
    image: postgres:17
    container_name: pseudomarkets-nextgen-postgres
    environment:
      POSTGRES_DB: pseudomarkets_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${Postgres__Password}
    ports:
      - "5432:5432"
    volumes:
      - ../.docker-data/postgres:/var/lib/postgresql/data

  pseudomarkets.transactionprocessing.service:
    build:
      context: ..
      dockerfile: pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service/Dockerfile
    env_file:
      - ../.env
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8082
      ConnectionStrings__PseudoMarketsDb: Host=postgres;Port=5432;Database=pseudomarkets_db;Username=postgres;Password=${Postgres__Password}
      IdentityAuthorization__IdentityServerBaseUrl: http://pseudomarkets-nextgen-idp-web:8080
    depends_on:
      - postgres
    ports:
      - "8082:8082"
```

Future root platform compose integration should expose:

- transaction processing Swagger UI at `http://localhost:8082/swagger/index.html`

## First Scaffolding Deliverable

The initial scaffold should include:

1. service folder and solution
2. all four source projects
3. test project
4. root solution inclusion
5. minimal controllers with request contracts
6. shared authorization wiring
7. shared EF Core `PseudoMarketsDbContext` and entities
8. initial migration
9. Dockerfile and service-local Compose file
10. README

## First Implementation Order

Recommended implementation sequence after scaffold:

1. post cash deposit
2. post cash withdrawal
3. post cash adjustment
4. post trade transaction
5. void cash transaction
6. void trade transaction using strict reversal rules

## Open Decisions For Scaffolding

These do not block scaffolding, but should be confirmed during implementation:

1. Should `account_id` remain numeric to align with the legacy world, or move to GUID later?
2. Do we want an outbox table in migration one, or add it later?
3. Should adjustment posting be available only in Development/admin flows at first?

## Recommendation

Proceed to scaffolding once this document is approved, using the exact project and table names above unless a naming adjustment is requested.
