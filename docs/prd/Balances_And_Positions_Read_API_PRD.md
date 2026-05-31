# Product Requirements Document

## Feature Name
Pseudo Markets Balances and Positions Read API

## Description
Add read-only API support for retrieving balances and positions through a dedicated Balances and Positions microservice. The API accepts a `userId` in the request body, enforces that user-scoped tokens can only request their own `userId`, allows `SYSTEM` tokens to request any user's data, returns both settled and unsettled balances and positions, supports filtering settled and unsettled values, calculates current market value for positions by calling the Market Data Service, and calculates unrealized gain/loss values in the service response.

## Problem Statement
The platform already writes aggregate, settled, and unsettled balances and positions into the shared relational model, but there was no dedicated user-facing or service-facing API for reading that state back. Without a dedicated read API, consumers could not retrieve account cash, holdings, or current position value in a safe and user-scoped way.

This is especially limiting because:

- end users need a way to view their current balances and holdings
- future UI and account services need a stable API for account-state display
- settled and unsettled values need to be visible independently
- current market value cannot be derived without joining current holdings with live market data

## Why
Balances and positions are foundational account views for a trading platform. This feature turns the existing shared projections into a usable read surface and creates the contract that future portfolio, dashboard, and account-summary features can depend on.

This matters because it:

- exposes settled and unsettled cash in a user-safe way
- exposes settled and unsettled position quantity and cost basis
- provides current market value for each open position using the Market Data Service
- avoids trusting caller-submitted user IDs for account reads
- reuses the existing transaction-processing data model instead of creating another projection prematurely

## Audience
This feature is being built for:

- authenticated end users who need to view their balances and positions
- future frontend applications that will show account cash and holdings
- platform developers building account and portfolio experiences
- internal services that need a trusted read-only account-state contract

## What
A dedicated Balances and Positions Service should expose read-only endpoints for balances and positions.

The API should:

- accept a `userId` request property in the JSON request body
- derive the caller identity from the validated authentication token
- reject requests when a user-scoped token does not contain a usable user ID claim
- reject requests when a user-scoped token requests a different `userId` than the one in the token
- allow `SYSTEM` tokens to bypass the user-ID ownership check and request any user's balances or positions
- require read-only authorization using the existing `VIEW_TRANSACTIONS` action

The balances response should include:

- aggregate cash balance
- settled cash balance
- unsettled cash balance
- the requested user ID
- a way to filter the response so callers can request:
  - all balance values
  - settled-only balance values
  - unsettled-only balance values

The positions response should include, for each symbol:

- symbol
- aggregate quantity
- settled quantity
- unsettled quantity
- aggregate cost basis
- settled cost basis
- unsettled cost basis
- current market price from the Market Data Service
- current aggregate market value
- current settled market value
- current unsettled market value
- aggregate unrealized gain/loss
- settled unrealized gain/loss
- unsettled unrealized gain/loss
- a way to filter the response so callers can request:
  - all position values
  - settled-only position values
  - unsettled-only position values

The service should not return trade lots or lot-level inventory details as part of this API.

The positions response should support returning only positions that currently exist for the user. Closed positions with zero quantity should not be returned unless a future requirement explicitly adds historical position retrieval.

Market value should be calculated using the current quote price from the Market Data Service:

- aggregate market value = aggregate quantity * current quote price
- settled market value = settled quantity * current quote price
- unsettled market value = unsettled quantity * current quote price

Unrealized gain/loss should be calculated by the service using returned market value and stored cost basis:

- aggregate unrealized gain/loss = aggregate market value - aggregate cost basis
- settled unrealized gain/loss = settled market value - settled cost basis
- unsettled unrealized gain/loss = unsettled market value - unsettled cost basis

If a market quote cannot be retrieved for a position, the API should return partial success:

- the overall response remains `200 OK`
- the affected position returns `isQuoteAvailable = false`
- quote-derived fields return `null`
- the response includes warning records identifying the affected symbols

## How
Implementation should live in a dedicated Balances and Positions Service and reuse the existing shared relational model through `PseudoMarketsDbContext`.

At a high level, the solution should include:

- one read-only balances endpoint in the Balances and Positions Service
- one read-only positions endpoint in the Balances and Positions Service
- token claim extraction logic that derives the caller user ID and account type from the request context
- user-ownership validation logic that compares request `userId` against the token for non-`SYSTEM` tokens
- authorization using the shared authorization library and the existing `VIEW_TRANSACTIONS` action
- read-side services or query handlers in Balances and Positions Core
- a Market Data client in Balances and Positions Core for retrieving current quotes
- DTOs for balance and position responses, including settled/unsettled filtering behavior
- unit tests for authorization, claim extraction, filtering, market-value calculation, and unrealized gain/loss calculation

Recommended request shape:

- balances endpoint with required `userId` and optional `view` in the request body
- positions endpoint with required `userId` and optional `view` in the request body

The service should read:

- `account_balances` by requested `user_id` after authorization and ownership validation
- `positions` by requested `user_id` after authorization and ownership validation

The service should call the existing Market Data Service quote endpoint for each returned position symbol, using the existing system-to-system authorization pattern already used elsewhere in the platform. The initial implementation performs quote lookups one symbol at a time.

The feature should not mutate balances, positions, lots, or transactions. It is a read-only surface over the existing write-side projections.

## API Spec

The API should expose dedicated account-state routes and should not be nested under a transaction-history route segment.

### Authentication and authorization

- All read endpoints require a Bearer token issued by the IDP.
- The caller user ID and account type must be derived from the authenticated token claims.
- The endpoints must require the existing `VIEW_TRANSACTIONS` action.
- The endpoints accept a request `userId`.
- For non-`SYSTEM` tokens, the request `userId` must match the `userId` in the token.
- `SYSTEM` tokens may request balances or positions for any `userId`.

### Common request body

All read endpoints should support:

- `userId: <10-digit user id>`
- `view: all|settled|unsettled`

Behavior:

- `userId` is required.
- `all` returns aggregate, settled, and unsettled values.
- `settled` returns only settled-specific values.
- `unsettled` returns only unsettled-specific values.
- If omitted, default to `all`.
- Invalid or missing `userId` should return `400 Bad Request`.
- Invalid `view` values should return `400 Bad Request`.

### `POST /balances`

Returns the requested user's balance view from the `account_balances` table after authorization and ownership validation.

Relevant backing schema:

- `account_balances.user_id`
- `account_balances.cash_balance`
- `account_balances.settled_cash_balance`
- `account_balances.unsettled_cash_balance`

#### Response shape

```json
{
  "requestedUserId": 1000000001,
  "view": "all",
  "aggregateCashBalance": 1500.25,
  "settledCashBalance": 1200.25,
  "unsettledCashBalance": 300.00
}
```

#### View-specific response expectations

- `view=all`
  - return `aggregateCashBalance`, `settledCashBalance`, and `unsettledCashBalance`
- `view=settled`
  - return `settledCashBalance`
  - `aggregateCashBalance` and `unsettledCashBalance` return `null`
- `view=unsettled`
  - return `unsettledCashBalance`
  - `aggregateCashBalance` and `settledCashBalance` return `null`

#### Status behavior

- `200 OK` when a balance row exists
- `404 Not Found` when the requested user has no `account_balances` row
- `400 Bad Request` for invalid or missing `userId`, or invalid `view`
- `401 Unauthorized` or `403 Forbidden` for missing/invalid token or failed authorization
- `403 Forbidden` when a non-`SYSTEM` token requests a different `userId` than the one in the token

### `POST /positions`

Returns the requested user's open positions from the `positions` table after authorization and ownership validation, enriched with current quote data from the Market Data Service.

Relevant backing schema:

- `positions.user_id`
- `positions.symbol`
- `positions.quantity`
- `positions.settled_quantity`
- `positions.unsettled_quantity`
- `positions.cost_basis_total`
- `positions.settled_cost_basis_total`
- `positions.unsettled_cost_basis_total`

Relevant Market Data dependency:

- current quote price by symbol from the Market Data Service

#### Response shape

```json
{
  "requestedUserId": 1000000001,
  "view": "all",
  "positions": [
    {
      "symbol": "AAPL",
      "aggregateQuantity": 10.000000,
      "settledQuantity": 8.000000,
      "unsettledQuantity": 2.000000,
      "aggregateCostBasis": 1950.00,
      "settledCostBasis": 1560.00,
      "unsettledCostBasis": 390.00,
      "currentMarketPrice": 210.50,
      "aggregateMarketValue": 2105.00,
      "settledMarketValue": 1684.00,
      "unsettledMarketValue": 421.00,
      "aggregateUnrealizedGainLoss": 155.00,
      "settledUnrealizedGainLoss": 124.00,
      "unsettledUnrealizedGainLoss": 31.00,
      "isQuoteAvailable": true,
      "quoteWarningMessage": null
    }
  ],
  "warnings": []
}
```

#### View-specific response expectations

- `view=all`
  - return aggregate, settled, and unsettled quantity, cost basis, market value, and unrealized gain/loss fields
- `view=settled`
  - return `settledQuantity`, `settledCostBasis`, `currentMarketPrice`, `settledMarketValue`, and `settledUnrealizedGainLoss`
  - aggregate and unsettled fields return `null`
- `view=unsettled`
  - return `unsettledQuantity`, `unsettledCostBasis`, `currentMarketPrice`, `unsettledMarketValue`, and `unsettledUnrealizedGainLoss`
  - aggregate and settled fields return `null`

#### Position filtering behavior

- only rows with an existing `positions` record for the requested user should be considered
- rows where aggregate quantity is `0` should not be returned by default
- trade lots and lot-level inventory must not be returned

#### Calculation rules

- `aggregateMarketValue = aggregateQuantity * currentMarketPrice`
- `settledMarketValue = settledQuantity * currentMarketPrice`
- `unsettledMarketValue = unsettledQuantity * currentMarketPrice`
- `aggregateUnrealizedGainLoss = aggregateMarketValue - aggregateCostBasis`
- `settledUnrealizedGainLoss = settledMarketValue - settledCostBasis`
- `unsettledUnrealizedGainLoss = unsettledMarketValue - unsettledCostBasis`

#### Status behavior

- `200 OK` when the request succeeds, including when the user has no open positions
- `400 Bad Request` for invalid or missing `userId`, or invalid `view`
- `401 Unauthorized` or `403 Forbidden` for missing/invalid token or failed authorization
- `403 Forbidden` when a non-`SYSTEM` token requests a different `userId` than the one in the token
- quote dependency failure does not fail the full request
- affected positions return `isQuoteAvailable = false`
- quote-derived fields return `null` for affected positions
- top-level `warnings` identify affected symbols

## Acceptance Criteria

- [x] A dedicated Balances and Positions Service exposes a read-only balances endpoint for a requested `userId`.
- [x] A dedicated Balances and Positions Service exposes a read-only positions endpoint for a requested `userId`.
- [x] The API accepts a caller-supplied `userId` request property in the JSON body.
- [x] For non-`SYSTEM` tokens, the request `userId` must match the user ID in the token.
- [x] `SYSTEM` tokens can request balances and positions for any `userId`.
- [x] Requests require the existing `VIEW_TRANSACTIONS` authorization action.
- [x] The balances response includes aggregate, settled, and unsettled cash values.
- [x] The balances endpoint supports filtering for `all`, `settled`, or `unsettled`.
- [x] The positions response includes aggregate, settled, and unsettled quantity values.
- [x] The positions response includes aggregate, settled, and unsettled cost basis values.
- [x] The positions endpoint supports filtering for `all`, `settled`, or `unsettled`.
- [x] The positions response includes current market price per symbol from the Market Data Service.
- [x] The positions response includes aggregate, settled, and unsettled market value calculations derived from the current quote price.
- [x] The positions response includes aggregate, settled, and unsettled unrealized gain/loss values calculated by the service.
- [x] The positions response does not expose trade lots or lot-level inventory details.
- [x] Unrealized gain/loss values are derived from returned market value and stored cost basis rather than calculated by the consumer.
- [x] Closed positions with zero quantity are not returned by default.
- [x] The API returns an appropriate unauthorized, forbidden, or invalid-token response when the token is missing a usable user ID claim or the request `userId` fails ownership validation.
- [x] The Market Data call uses the existing service-to-service authorization pattern rather than caller-provided market data credentials.
- [x] Quote failures return partial success with warning records and null quote-derived fields for affected positions.
- [x] Unit tests cover authorization, user-ID claim extraction, balance filtering, position filtering, market-value calculation, missing balance rows, empty position sets, and Market Data dependency behavior.
- [x] The full solution builds and `dotnet test PseudoMarkets.NextGen.Platform.sln -m:1` passes.

## Out Of Scope

- Historical balance snapshots.
- Historical position history or closed-position history retrieval.
- Realized P&L or broader performance analytics beyond the unrealized gain/loss values returned for current positions.
- Tax-lot reporting or lot-level inventory retrieval beyond the settled/unsettled aggregate cost basis fields already stored.
- Balance or position mutation behavior.
- Margin, options, short selling, or non-equity portfolio calculations.
- A new aggregated portfolio service outside the dedicated Balances and Positions Service.
- Replacing the Market Data Service quote API.

## Notes
- This PRD builds on [Settled_Unsettled_Balances_Positions_PRD.md](Settled_Unsettled_Balances_Positions_PRD.md) and the existing Transaction Processing write model.
- The current transaction-processing data model already stores `cash_balance`, `settled_cash_balance`, `unsettled_cash_balance`, `quantity`, `settled_quantity`, `unsettled_quantity`, `cost_basis_total`, `settled_cost_basis_total`, and `unsettled_cost_basis_total`.
- The shared authorization constants already define `VIEW_TRANSACTIONS` for read-only transaction-processing access and `VIEW_MARKET_DATA` for Market Data access.
- The implementation plan should explicitly decide how the API behaves when one or more market quotes cannot be retrieved, because that choice materially affects the response contract.
