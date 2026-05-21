# Product Requirements Document

## Feature Name
Pseudo Markets Batch Processing - Trade Settlement Job

## Description
Add a scheduled batch job that performs settlement for trade-related unsettled cash, positions, lots, and cost basis on each trade's settlement date. The heavy settlement business logic should live in the Transaction Processing project as reusable core logic. The Hangfire-based batch job should reference that project, create the required services through dependency injection, and call the Transaction Processing settlement logic directly while using the shared PostgreSQL database.

## Problem Statement
The platform currently records trade executions with a trade date and settlement date, and trade posting creates unsettled cash or unsettled position effects. Those effects remain unsettled indefinitely because there is no batch process that promotes them when the settlement date arrives.

Without a settlement job, downstream services and future account views cannot reliably determine settled cash, settled holdings, available funds for withdrawal, or settled inventory for sell validation. The system needs a durable, repeatable process that applies settlement effects once the settlement date is reached.

## Why
Trade settlement is core platform accounting behavior. This job closes the lifecycle for posted trades by moving eligible unsettled balances and positions into settled state at the right time.

This matters because it:

- keeps account balances and positions accurate after trade settlement
- allows future read services to distinguish settled and unsettled account value
- supports future buying power, withdrawal, portfolio, and compliance rules
- keeps cost basis and lot-level state aligned with settlement
- validates the batch-processing framework with another production-style financial workflow

## Audience
This feature is being built for:

- Backend platform services that depend on settled balances, positions, lots, and cost basis
- Future account, portfolio, and reporting services that will query settled and unsettled projections
- Platform developers maintaining transaction processing, shared entities, and batch workflows
- Operators and developers monitoring scheduled settlement behavior through the Hangfire dashboard

## What
The platform should add a recurring batch job that settles eligible trade executions for the date on which the job runs.

At a high level, the job should:

- run once per market day as the first morning batch job
- determine the processing date in the configured market time zone
- resolve and call the Transaction Processing settlement service directly
- interact with PostgreSQL through shared dependencies used by the Transaction Processing core logic
- avoid duplicating settlement business logic inside the batch-processing host
- use the existing Hangfire-based batch-processing framework

At a high level, the Transaction Processing project should:

- expose reusable settlement business logic from the Transaction Processing core layer
- query the `trade_executions` table for trades with `settlement_date` equal to the requested processing date
- process only trades that still have unsettled effects to settle
- apply settlement effects to `account_balances`, `positions`, `position_lots`, and cost basis fields
- persist enough state to avoid settling the same trade more than once
- process records in deterministic order
- operate against the shared PostgreSQL database through Entity Framework

Buy trade settlement should:

- reduce unsettled position quantity for the user and symbol
- increase settled position quantity for the user and symbol
- reduce unsettled cost basis for the user and symbol
- increase settled cost basis for the user and symbol
- move the related buy-created lot quantity from unsettled remaining quantity to settled remaining quantity
- preserve aggregate quantity and aggregate cost basis totals

Sell trade settlement should:

- reduce unsettled cash by the sale proceeds amount
- increase settled cash by the sale proceeds amount
- preserve aggregate cash balance
- leave already-reduced positions and closed lots unchanged, because sell posting already consumed settled inventory at execution time
- keep lot closure / realized cost basis data consistent with the already-posted sell transaction

Settlement should be idempotent. If the batch job is retried by Hangfire or manually run again for the same processing date, the Transaction Processing settlement logic must not settle already-settled trades a second time.

The Transaction Processing settlement logic should return a settlement summary that includes the processing date, number of candidate trades, number of settled trades, number of skipped trades, and any failed trade IDs. The batch job should log this response.

## How
Implementation should plug into the existing Hangfire-based batch-processing host introduced in [Platform_Batch_Processing_Framework_PRD.md](Platform_Batch_Processing_Framework_PRD.md), but settlement business rules should remain in the Transaction Processing project.

At a high level, the solution should include:

- a concrete trade settlement batch job registered with the shared batch framework
- a project reference from the batch-processing host/core to the Transaction Processing project that contains settlement business logic
- dependency injection wiring that allows the batch host to resolve the Transaction Processing settlement service
- a settlement service in Transaction Processing that accepts a processing date and uses `PseudoMarketsDbContext` from the shared entities project
- a Transaction Processing query that selects eligible trade executions by `settlement_date`
- transaction-scoped updates in Transaction Processing so each trade settlement is atomic
- settlement state tracking in Transaction Processing so each trade execution is settled once
- unit tests covering batch orchestration, dependency injection wiring, buy settlement, sell settlement, idempotency, and partial-failure behavior

The implementation will likely need a small schema update in the shared entities project so settlement completion can be tracked safely. Proposed fields on `trade_executions`:

- `settled_at_utc` nullable timestamp
- `settlement_status` text or enum-backed string, with values such as `Pending`, `Settled`, and `Failed`

The Transaction Processing settlement service should query for trade executions where:

- `settlement_date` equals the processing date
- `settlement_status` indicates pending settlement, or the equivalent tracking field is null
- the parent transaction has not been voided or otherwise reversed

The exact void/reversal filter should be confirmed during implementation planning based on the current transaction schema. Voided transactions should not create duplicate or incorrect settlement effects.

The Transaction Processing settlement service should use a database transaction for each trade or bounded batch of trades. A single failed trade should be recorded and returned in the settlement summary without necessarily preventing unrelated trades from settling.

The initial schedule should run as the first morning batch job on valid market days. Default schedule:

- `12:00 AM America/New_York`

The batch job should include a market-day guard that skips settlement processing on weekends and configured market holidays. The Transaction Processing settlement service should also validate the processing date as a market day so manual or test invocations cannot accidentally settle on non-market days.

## Acceptance Criteria

- [ ] A concrete trade settlement batch job exists in the batch-processing host and is registered with the shared Hangfire-based batch framework.
- [ ] The job is scheduled to run as the first morning batch job at `12:00 AM America/New_York`.
- [ ] The job skips processing on weekends and configured market holidays.
- [ ] The batch-processing project references the Transaction Processing project that contains settlement business logic.
- [ ] The batch job resolves and calls the Transaction Processing settlement service directly instead of using an HTTP endpoint.
- [ ] Settlement business logic is not duplicated inside the batch-processing host.
- [ ] The Transaction Processing settlement service accepts a processing date.
- [ ] The Transaction Processing settlement service validates that the processing date is a market day.
- [ ] The Transaction Processing settlement service queries PostgreSQL for trade executions whose `settlement_date` equals the processing date.
- [ ] The Transaction Processing settlement service excludes already-settled trades from processing.
- [ ] The Transaction Processing settlement service excludes voided or reversed trades from settlement processing.
- [ ] Buy trade settlement moves unsettled position quantity to settled position quantity.
- [ ] Buy trade settlement moves unsettled cost basis to settled cost basis.
- [ ] Buy trade settlement moves related lot quantity from unsettled remaining quantity to settled remaining quantity.
- [ ] Sell trade settlement moves unsettled cash proceeds to settled cash.
- [ ] Sell trade settlement preserves aggregate cash balance, aggregate position quantity, and aggregate cost basis totals.
- [ ] Settlement updates are atomic for each processed trade.
- [ ] The job is idempotent and does not apply duplicate balance, position, lot, or cost basis effects when retried.
- [ ] Settlement status or equivalent tracking data is persisted for each settled trade by Transaction Processing.
- [ ] The Transaction Processing settlement service returns processing date, candidate count, settled count, skipped count, and failed trade identifiers.
- [ ] The batch job logs the settlement service response.
- [ ] Unit tests cover Transaction Processing settlement behavior, batch service invocation, market-day skipping, idempotency, voided trade exclusion, and failure handling.
- [ ] The full solution builds and `dotnet test PseudoMarkets.NextGen.Platform.sln -m:1` passes.

## Out Of Scope

- Building a user-facing account, portfolio, or settlement history API.
- Building frontend UI for settled and unsettled balances or positions.
- Changing trade date or settlement date calculation rules.
- Changing order execution behavior.
- Changing trade posting behavior except where needed to support safe settlement tracking.
- Duplicating balance, position, lot, or cost basis settlement logic inside the batch-processing host.
- Adding a long-running Transaction Processing API endpoint for settlement.
- Implementing margin, short selling, options, crypto settlement, or non-U.S. market settlement rules.
- Implementing tax reporting or realized gain/loss reporting beyond preserving cost basis state already captured by existing lot and closure tables.
- Adding alerting, notifications, or operator workflows for failed settlement records.

## Notes
This PRD builds on:

- [Settled_Unsettled_Balances_Positions_PRD.md](Settled_Unsettled_Balances_Positions_PRD.md)
- [Platform_Batch_Processing_Framework_PRD.md](Platform_Batch_Processing_Framework_PRD.md)

Current related shared tables include:

- `trade_executions`
- `ledger_transactions`
- `account_balances`
- `positions`
- `position_lots`
- `position_lot_closures`
- `market_holidays`

Open questions to resolve during implementation planning:

- Should the job process each trade independently, or group by user and symbol for fewer database writes while preserving idempotency?
- Should settlement failures be stored only on `trade_executions`, or should the batch framework also persist a settlement-specific audit table?
- What is the precise schema relationship that identifies a voided or reversed trade today, and does it need to be made more explicit before settlement is implemented?
- Should the reusable settlement service live in the existing Transaction Processing Core project, or should it be split into a smaller application/service project if dependency direction becomes too broad?
