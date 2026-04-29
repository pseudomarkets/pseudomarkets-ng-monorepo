# Product Requirements Document

## Feature Name
Pseudo Markets - Settled and Unsettled Balances and Positions

## Description
Extend the transaction-processing balance and position data model to represent settled and unsettled cash balances and security positions. The platform currently stores aggregate cash balance and aggregate position quantity. This feature will make settlement state explicit so downstream services can distinguish settled funds and holdings from activity that has been posted but has not yet reached its settlement date.

## Problem Statement
The transaction-processing service already calculates trade dates and settlement dates for trade executions, but the write-side balance and position projections do not reflect whether the related cash or position quantity is settled. This makes it difficult for future read APIs, buying power checks, withdrawal rules, portfolio views, and compliance-oriented business rules to answer basic questions such as:

- how much cash is settled and available for withdrawal
- how much cash is unsettled due to pending trade settlement
- how many shares are settled versus unsettled
- which posted trades still need to be promoted into settled state

Without this distinction, future services may incorrectly treat unsettled proceeds or holdings as fully settled account value.

## Why
Settled and unsettled state is a core accounting concept for an equities trading platform. Adding this model now creates a more accurate foundation for order validation, withdrawal eligibility, customer-facing portfolio views, future read models, and settlement processing. It also keeps the platform's internal projections aligned with the existing trade settlement-date calculations in the transaction-processing domain.

Settled balances will be enforced at order entry so a user must have sufficient settled funds to cover a buy order. Settled positions will also be enforced at order entry for sell orders, because a user cannot sell shares that have not completed T+1 settlement.

## Audience
This feature is primarily for backend platform services and future account/portfolio read APIs. It will also support future frontend account views that display settled and unsettled cash, settled and unsettled positions, and pending settlement activity to end users. Developers working on transaction processing, shared entities, migrations, and future order-management workflows are also direct consumers of this feature.

## What
The system should extend the existing `account_balances` and `positions` tables so they can represent both settled and unsettled state.

For balances:

1. Track settled cash separately from unsettled cash.
2. Preserve `cash_balance` as a physically stored aggregate cash balance.
3. Ensure deposits, withdrawals, adjustments, trade buys, trade sells, and voids update the correct settled and unsettled cash fields.
4. Prevent withdrawals from using unsettled cash unless a future requirement explicitly allows it.
5. Treat non-trade cash movements as settled immediately.

For positions:

1. Track settled quantity separately from unsettled quantity for each user and symbol.
2. Preserve `quantity` and `cost_basis_total` as physically stored aggregate position values.
3. Track cost basis consistently across settled and unsettled quantities.
4. Ensure buys debit settled cash immediately and create unsettled position quantity.
5. Ensure sells are only allowed against settled position quantity, require a lot-level settled-share availability check, create unsettled cash proceeds, and are blocked when the user does not have enough settled shares.
6. Ensure voids correctly reverse settled or unsettled effects based on the stored state created by the original transaction.

The system should only mark trade-related balance and position effects as unsettled during trade execution. A future batch process will promote eligible unsettled cash and position quantities into settled state when settlement dates are reached.

The data written by this feature must support that future batch workflow. The future batch process is expected to run daily at the start of day, before market hours, find transactions where the processing date equals the transaction settlement date, and settle each impacted account's balances and positions. This feature must therefore persist unsettled trade effects in a way that can be traced back to the originating trade execution, account, symbol, amount, quantity, cost basis, and `settlement_date`.

## How
High level implementation should follow the existing transaction-processing and shared-entities architecture:

1. Update the shared Entity Framework Core entities and `PseudoMarketsDbContext` mappings for `AccountBalanceEntity` and `PositionEntity`.
2. Add an EF Core migration under `pseudomarkets-nextgen-shared-entities` to extend the PostgreSQL schema.
3. Add settled and unsettled balance columns to `account_balances`. Proposed fields:
   - `settled_cash_balance` `numeric(18, 4)` not null
   - `unsettled_cash_balance` `numeric(18, 4)` not null
   - keep `cash_balance` as a physically stored aggregate total cash value
4. Add settled and unsettled position columns to `positions`. Proposed fields:
   - `settled_quantity` `numeric(18, 6)` not null
   - `unsettled_quantity` `numeric(18, 6)` not null
   - `settled_cost_basis_total` `numeric(18, 4)` not null
   - `unsettled_cost_basis_total` `numeric(18, 4)` not null
   - keep `quantity` and `cost_basis_total` as physically stored aggregate values
5. Extend `position_lots` as needed so buy-created lot inventory can distinguish settled quantity from unsettled quantity and can be traced to its opening trade execution through the existing transaction relationship.
6. Preserve enough trade execution and ledger linkage for sell proceeds and buy-created positions to be discoverable by a future settlement batch using `settlement_date`.
7. Backfill existing rows so current aggregate values are treated as settled by default. Existing `cash_balance`, `quantity`, and `cost_basis_total` values should remain unchanged during the backfill.
8. Update trade posting so trade-related effects are assigned to unsettled cash and unsettled position fields while preserving the existing `trade_date` and `settlement_date` values for the future settlement batch process.
9. Update non-trade cash movement posting so deposits, withdrawals, and adjustments affect settled cash immediately.
10. Update transaction-processing unit tests for deposits, withdrawals, adjustments, buys, sells, and voids.
11. Update service and root README files if commands, behavior, schema ownership notes, or service responsibilities change during implementation.

## Acceptance Criteria

- [ ] `account_balances` can store settled cash and unsettled cash separately while retaining `cash_balance` as a physically stored aggregate cash value.
- [ ] `positions` can store settled quantity, unsettled quantity, settled cost basis, and unsettled cost basis separately while retaining `quantity` and `cost_basis_total` as physically stored aggregate values.
- [ ] Existing account balance and position rows are backfilled as settled state during migration.
- [ ] Trade buy posting debits settled cash immediately and records newly purchased quantity and related cost basis as unsettled.
- [ ] Trade sell posting records sale proceeds as unsettled cash.
- [ ] Trade sell posting validates settled share availability at the lot level and blocks the transaction when settled lots do not contain enough quantity for the sell.
- [ ] Unsettled trade effects remain traceable to the originating trade execution, account, symbol, amount, quantity, cost basis, and `settlement_date` so a future start-of-day batch can settle transactions where the processing date equals `settlement_date`.
- [ ] Non-trade cash movements, including deposits, withdrawals, and adjustments, update settled cash immediately.
- [ ] Withdrawal validation uses settled cash and does not allow unsettled cash to be withdrawn.
- [ ] Void logic correctly reverses the original transaction effects for trade-created unsettled balances and positions, and for instantly settled cash movements.
- [ ] Unit tests cover schema-sensitive posting behavior, unsettled trade posting behavior, instantly settled cash movement behavior, and void behavior.
- [ ] The full solution builds and `dotnet test PseudoMarkets.NextGen.Platform.sln -m:1` passes.

## Out Of Scope

- Building a user-facing portfolio or account balance read API.
- Building frontend UI for displaying settled and unsettled state.
- Building the batch process that promotes unsettled balances or positions into settled state.
- Implementing settlement promotion, settlement retry, or settlement idempotency workflows.
- Implementing margin, short selling, options, crypto settlement rules, or non-U.S. market settlement rules.
- Changing the existing authorization model.
- Replacing the transaction ledger model.

## Notes
The current transaction-processing service already calculates trade dates and settlement dates for trade executions. This PRD only covers marking trade-related effects as unsettled at execution time and preserving the data needed by a future settlement batch. Settlement promotion will be handled by a future batch process.

Future settlement batch expectation: run once per trading day at start of day before market hours, find trade executions where the processing date equals `settlement_date`, and update each impacted account's settled and unsettled balances, positions, and lot state.

Open questions to resolve during implementation planning:

- None
