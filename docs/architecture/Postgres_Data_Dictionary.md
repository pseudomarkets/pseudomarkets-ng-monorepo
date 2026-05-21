# Pseudo Markets PostgreSQL Data Dictionary

## Overview

Pseudo Markets uses PostgreSQL as the relational system of record for platform data that needs transactional consistency, relational constraints, historical auditability, and Entity Framework Core migrations.

The shared database is named `pseudomarkets_db`. Entity Framework models and migrations are owned by the `PseudoMarkets.Shared.Entities` project and exposed through `PseudoMarketsDbContext`.

## Conventions

- Monetary values use `numeric(18, 4)` unless otherwise noted.
- Share quantities and prices use `numeric(18, 6)` unless otherwise noted.
- Timestamps ending in `_utc` are UTC timestamps.
- User identifiers use the 10-digit numeric user ID issued by the IDP account store.
- Primary keys named `id` are database-generated integer/long identifiers unless otherwise noted.
- Public service APIs may use PascalCase DTOs, but database columns use snake_case.

## Platform Tables

### `market_holidays`

Stores market holidays used by services and batch jobs to determine market days and settlement dates.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `holiday_date` | `date` | Yes | Primary key | Market holiday date. |
| `holiday_name` | `varchar(100)` | Yes |  | Human-readable holiday name. |

Seed data currently includes the 2026 NYSE market holidays.

## Reference Data Tables

### `trading_instruments`

Stores instruments that can be traded on the platform.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `symbol` | `varchar(32)` | Yes | Primary key | Instrument symbol. |
| `description` | `varchar(512)` | Yes |  | Human-readable instrument description. |
| `trading_status` | `boolean` | Yes | Indexed | Indicates whether the instrument is currently tradable. Defaults to `true`. |
| `primary_instrument_type` | `varchar(50)` | Yes |  | Broad instrument type. |
| `secondary_instrument_type` | `varchar(50)` | Yes | Indexed | More specific instrument classification. |
| `closing_price` | `double precision` | Yes |  | Last stored closing price. |
| `closing_price_date` | `date` | Yes |  | Date associated with the stored closing price. |
| `source` | `varchar(100)` | Yes |  | Source of the instrument or price data. |

## Transaction Processing Tables

### `posting_batches`

Tracks idempotent transaction-processing requests.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Posting batch identifier. |
| `idempotency_key` | `varchar(100)` | Yes | Unique index | Idempotency key supplied by the caller. |
| `user_id` | `bigint` | Yes |  | User associated with the request. |
| `request_type` | `varchar(50)` | Yes |  | Type of posting request. |
| `status` | `varchar(20)` | Yes |  | Processing status. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Batch creation timestamp. |
| `completed_at_utc` | `timestamp with time zone` | No |  | Completion timestamp. |
| `error_message` | `text` | No |  | Error details when processing fails. |

### `ledger_transactions`

Stores the transaction ledger. Each row represents a posted transaction or compensating void transaction.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Ledger row identifier. |
| `transaction_id` | `uuid` | Yes | Unique index | Platform transaction identifier used for voiding and history lookups. |
| `posting_batch_id` | `bigint` | Yes | Indexed, FK | Owning posting batch. |
| `user_id` | `bigint` | Yes | Indexed | User affected by the transaction. |
| `transaction_kind` | `varchar(50)` | Yes |  | Transaction category, such as trade or cash movement. |
| `direction` | `varchar(20)` | Yes |  | Debit or credit direction. |
| `amount` | `numeric(18, 4)` | Yes |  | Transaction amount. |
| `transaction_description` | `varchar(200)` | Yes |  | Service-generated description. |
| `status` | `varchar(20)` | Yes |  | Transaction status. |
| `occurred_at_utc` | `timestamp with time zone` | Yes |  | Business event timestamp. |
| `voids_transaction_id` | `uuid` | No | Indexed | Transaction ID voided by this row, when applicable. |
| `external_reference_id` | `varchar(100)` | No |  | External reference supplied by caller or upstream system. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |

### `trade_executions`

Stores trade execution details and settlement dates.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Trade execution row identifier. |
| `transaction_id` | `uuid` | Yes | Unique index | Related ledger transaction ID. |
| `user_id` | `bigint` | Yes | Indexed | User affected by the trade. |
| `external_execution_id` | `varchar(100)` | Yes | Unique index | External execution identifier. |
| `symbol` | `varchar(32)` | Yes | Indexed | Executed symbol. |
| `trade_side` | `varchar(20)` | Yes | Composite index with `settlement_date` | Buy or sell side. |
| `quantity` | `numeric(18, 6)` | Yes |  | Executed quantity. |
| `execution_price` | `numeric(18, 6)` | Yes |  | Execution price per share. |
| `gross_amount` | `numeric(18, 4)` | Yes |  | Gross trade amount before fees. |
| `fees` | `numeric(18, 4)` | Yes |  | Fees applied to the execution. |
| `net_amount` | `numeric(18, 4)` | Yes |  | Net cash effect. |
| `executed_at_utc` | `timestamp with time zone` | Yes |  | Execution timestamp. |
| `trade_date` | `date` | Yes | Indexed | Trade date. |
| `settlement_date` | `date` | Yes | Indexed, composite index with `trade_side` | T+1 settlement date adjusted for weekends and market holidays. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |

### `cash_movements`

Stores cash movement details for deposits, withdrawals, and adjustments.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Cash movement row identifier. |
| `transaction_id` | `uuid` | Yes | Unique index | Related ledger transaction ID. |
| `user_id` | `bigint` | Yes | Indexed | User affected by the movement. |
| `movement_type` | `varchar(30)` | Yes |  | Deposit, withdrawal, or adjustment. |
| `external_reference_id` | `varchar(100)` | No |  | External reference. |
| `reason_code` | `varchar(50)` | No |  | Optional reason code for adjustments. |
| `occurred_at_utc` | `timestamp with time zone` | Yes |  | Business event timestamp. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |

### `account_balances`

Stores aggregate, settled, and unsettled cash balances per user.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `user_id` | `bigint` | Yes | Primary key | User ID from IDP. |
| `cash_balance` | `numeric(18, 4)` | Yes |  | Aggregate cash balance. |
| `settled_cash_balance` | `numeric(18, 4)` | Yes |  | Settled cash available for settled-only flows. |
| `unsettled_cash_balance` | `numeric(18, 4)` | Yes |  | Cash pending settlement. |
| `updated_at_utc` | `timestamp with time zone` | Yes |  | Last update timestamp. |

### `positions`

Stores aggregate, settled, and unsettled position state per user and symbol.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Position row identifier. |
| `user_id` | `bigint` | Yes | Unique composite index with `symbol` | User ID from IDP. |
| `symbol` | `varchar(32)` | Yes | Unique composite index with `user_id` | Position symbol. |
| `position_side` | `varchar(20)` | Yes |  | Position side. Short-sale behavior is currently out of scope. |
| `quantity` | `numeric(18, 6)` | Yes |  | Aggregate position quantity. |
| `settled_quantity` | `numeric(18, 6)` | Yes |  | Settled position quantity. |
| `unsettled_quantity` | `numeric(18, 6)` | Yes |  | Position quantity pending settlement. |
| `cost_basis_total` | `numeric(18, 4)` | Yes |  | Aggregate cost basis. |
| `settled_cost_basis_total` | `numeric(18, 4)` | Yes |  | Settled cost basis. |
| `unsettled_cost_basis_total` | `numeric(18, 4)` | Yes |  | Cost basis pending settlement. |
| `updated_at_utc` | `timestamp with time zone` | Yes |  | Last update timestamp. |

### `position_lots`

Stores lot-level inventory used for settled-share validation and cost basis tracking.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Lot identifier. |
| `user_id` | `bigint` | Yes | Composite index with `symbol` | User ID from IDP. |
| `symbol` | `varchar(32)` | Yes | Composite index with `user_id` | Lot symbol. |
| `opening_transaction_id` | `uuid` | Yes | Indexed | Transaction that opened the lot. |
| `closing_transaction_id` | `uuid` | No | Indexed | Transaction that closed the lot when fully closed. |
| `lot_entry_type` | `varchar(20)` | Yes |  | Type/source of lot entry. |
| `quantity_opened` | `numeric(18, 6)` | Yes |  | Original lot quantity. |
| `quantity_remaining` | `numeric(18, 6)` | Yes |  | Aggregate remaining quantity. |
| `settled_quantity_remaining` | `numeric(18, 6)` | Yes |  | Settled remaining quantity. |
| `unsettled_quantity_remaining` | `numeric(18, 6)` | Yes |  | Remaining quantity pending settlement. |
| `price` | `numeric(18, 6)` | Yes |  | Lot opening price. |
| `opened_at_utc` | `timestamp with time zone` | Yes |  | Lot opening timestamp. |
| `updated_at_utc` | `timestamp with time zone` | Yes |  | Last update timestamp. |

### `position_lot_closures`

Stores lot closure records created when sell trades consume lot inventory.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `id` | `bigint` | Yes | Primary key | Lot closure identifier. |
| `position_lot_id` | `bigint` | Yes | Indexed, FK | Lot being closed. |
| `opening_transaction_id` | `uuid` | Yes |  | Transaction that opened the lot. |
| `closing_transaction_id` | `uuid` | Yes | Indexed | Transaction that closed lot quantity. |
| `user_id` | `bigint` | Yes | Composite index with `symbol` | User ID from IDP. |
| `symbol` | `varchar(32)` | Yes | Composite index with `user_id` | Closed symbol. |
| `quantity_closed` | `numeric(18, 6)` | Yes |  | Quantity closed from the lot. |
| `cost_basis_amount` | `numeric(18, 4)` | Yes |  | Cost basis associated with the closed quantity. |
| `closed_at_utc` | `timestamp with time zone` | Yes |  | Closure timestamp. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |

## Order Execution Tables

### `order_executions`

Stores submitted order execution outcomes.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `order_id` | `uuid` | Yes | Primary key | Order identifier. |
| `execution_id` | `uuid` | Yes | Unique index | Execution identifier. |
| `user_id` | `bigint` | Yes | Indexed | User that owns the order. |
| `symbol` | `varchar(32)` | Yes | Indexed | Order symbol. |
| `order_side` | `varchar(20)` | Yes |  | Buy or sell. |
| `order_type` | `varchar(20)` | Yes |  | Order type. |
| `quantity` | `numeric(18, 6)` | Yes |  | Requested quantity. |
| `fill_price` | `numeric(18, 6)` | Yes |  | Fill price. |
| `gross_amount` | `numeric(18, 4)` | Yes |  | Gross execution amount. |
| `fees` | `numeric(18, 4)` | Yes |  | Fees applied. |
| `net_amount` | `numeric(18, 4)` | Yes |  | Net execution amount. |
| `status` | `varchar(40)` | Yes | Indexed | Execution status. |
| `transaction_id` | `uuid` | No | Indexed | Transaction generated by transaction processing. |
| `posting_batch_id` | `bigint` | No |  | Posting batch generated by transaction processing. |
| `failure_code` | `varchar(80)` | No |  | Failure code for rejected/failed executions. |
| `failure_message` | `varchar(512)` | No |  | Failure details. |
| `submitted_at_utc` | `timestamp with time zone` | Yes |  | Order submission timestamp. |
| `executed_at_utc` | `timestamp with time zone` | No |  | Execution timestamp. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |
| `updated_at_utc` | `timestamp with time zone` | Yes |  | Last update timestamp. |

### `queued_orders`

Stores orders submitted outside market hours that should be processed by a later batch job.

| Column | Type | Required | Key / Index | Description |
| --- | --- | --- | --- | --- |
| `order_id` | `uuid` | Yes | Primary key | Queued order identifier. |
| `user_id` | `bigint` | Yes | Indexed | User that placed the order. |
| `symbol` | `varchar(32)` | Yes | Indexed | Order symbol. |
| `order_side` | `varchar(20)` | Yes |  | Buy or sell. |
| `order_type` | `varchar(20)` | Yes |  | Order type. |
| `quantity` | `numeric(18, 6)` | Yes |  | Requested quantity. |
| `status` | `varchar(40)` | Yes | Indexed | Queue status. |
| `queue_reason` | `varchar(40)` | Yes |  | Reason the order was queued. |
| `submitted_at_utc` | `timestamp with time zone` | Yes | Indexed | Original submission timestamp. |
| `last_attempted_at_utc` | `timestamp with time zone` | No |  | Last batch processing attempt timestamp. |
| `processed_at_utc` | `timestamp with time zone` | No |  | Completion timestamp. |
| `failure_message` | `varchar(512)` | No |  | Failure details. |
| `created_at_utc` | `timestamp with time zone` | Yes |  | Row creation timestamp. |
| `updated_at_utc` | `timestamp with time zone` | Yes |  | Last update timestamp. |

