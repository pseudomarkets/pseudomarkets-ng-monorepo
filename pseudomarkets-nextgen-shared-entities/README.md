# Pseudo Markets Shared Entities

`pseudomarkets-nextgen-shared-entities` contains the shared Entity Framework Core model for the Pseudo Markets platform PostgreSQL database.

## Current Contents

- `PseudoMarkets.Shared.Entities`
  .NET 10 class library containing `PseudoMarketsDbContext` and shared entity classes.
- `PseudoMarketsDbContext`
  Generic platform DbContext for the shared `pseudomarkets_db` database.
- transaction processing entities
  Posting batches, ledger transactions, trade executions, cash movements, balances, positions, lots, and lot closures.
- platform reference entities
  Market holidays, including seeded 2026 NYSE full-day market holidays, and trading instruments.

## Migration Ownership

This project owns the EF Core migrations for `PseudoMarketsDbContext`. The transaction processing and trading instruments services apply those migrations at startup.

If multiple services begin contributing migrations, we should introduce a dedicated platform database migrations project instead of letting multiple apps independently own schema changes.
