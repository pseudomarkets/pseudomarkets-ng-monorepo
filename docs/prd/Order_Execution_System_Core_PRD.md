# Product Requirements Document

## Feature Name
Pseudo Markets Order Execution System - Core Foundation

## Description
The order execution system is a critical component of the Pseudo Markets trading platform. It will simulate a combined order entry, order execution, and FIX system. 

## Problem Statement
In order to provide an accurate paper trading experience to users, we need an order execution system to simulate placing trades and having them filled at an exchange or market maker. The order execution system will serve to simulate that experience, where users can submit buy or sell side trades. 

## Why
The Order Execution System is a backend platform used to facilitate the paper trading experience. Without a proper system to handle order entry and simulated execution, the user cannot hold a simulated position in a particular security. This is integral to the concept of paper trading, users must be able to buy and sell stocks easily just as they would through an actual broker. 

## Audience
The system is a backend service that will be exposed via REST API. It will be called via frontend systems that users will interact with, such as a web app. 

## What
The Core Foundation of the Order Execution System will need to be able to accept the following parameters for order entry: a user ID to execute the trade against, symbol for the security being traded, the side of the trade (buy or sell), and quantity. Initial scope will only support market orders. Limit orders, stop orders, stop limit orders, and other contingent order types will be added later as a dedicated submodule within the Order Execution Service. After a market order is received, the system needs to perform basic validation checks before accepting and filling the trade. These checks include ensuring the user ID is valid, the requested symbol is supported for trading on the platform, the user has sufficient settled cash balance to cover buy orders, and the user has sufficient settled position quantity to cover sell orders.

The Order Execution Service must authenticate and authorize the incoming order submission request before performing business validation. Unsupported-symbol validation should happen after the caller has been authorized to submit an order. If the incoming token is a system token, the service may accept any user ID in the order payload. If the incoming token is a user token, the service must validate that the token's `id` claim matches the user ID in the order payload before accepting the order. This prevents one user from submitting trades against another user's account.

Initial symbol support should be limited to symbols that contain only letters and numbers. Symbols with special characters, separators, class-share delimiters, exchange suffixes, whitespace, or other non-alphanumeric characters should be rejected during order-entry validation. The service should trim and uppercase submitted symbols before validation, then apply the alphanumeric-only rule before calling dependent services or reading positions.

The Order Execution System should call the Market Data Service to retrieve the current quote for the requested symbol. The `Price` field from `QuoteResponse` should be used as the immediate fill price for the market order and to calculate the projected value of the trade.

For buy orders, the projected trade amount should be calculated as quantity * `QuoteResponse.Price`. The buy order should only be accepted if the user's settled cash balance can cover the projected trade amount. Unsettled cash should not count as available buying power for this core foundation unless a later PRD introduces margin or other expanded buying power rules.

For sell orders, the Order Execution System must validate that the user has enough settled position quantity for the submitted symbol before accepting the order. Unsettled position quantity should not count as sellable quantity. Sell orders that exceed settled position availability should be rejected before execution-related processing. This validation should be performed at the symbol-level using the aggregate settled position quantity. Lot-level settled-share enforcement should remain owned by the Transaction Processing Service when the filled trade is posted. This core foundation should not allow short selling.

The Order Execution Service should read account balances and positions directly from the shared PostgreSQL database for order-entry validation. The submitted user ID should be used to look up the user's `account_balances` row for settled cash validation and the user's `positions` row for the submitted symbol for settled position validation. These reads should use the shared Entity Framework Core model through `PseudoMarketsDbContext`. Order Execution should only read these records for validation; balance and position mutations remain owned by the Transaction Processing Service after a trade is filled.

The Order Execution System must use the Trading Instruments Service as the authoritative source for determining whether a requested symbol is supported. A trade should only be accepted if the Trading Instruments Service returns an instrument for the submitted symbol and the instrument is currently enabled for trading. If the symbol is not found, disabled, or otherwise not supported, the Order Execution System must reject the order before performing execution-related processing.

The Order Execution Service must authenticate to dependent platform services using a configured system account. The service configuration should include the IDP base URL, system account login ID, and system account password. At runtime, the Order Execution Service should call the IDP `POST /api/identity/authenticate` endpoint with those credentials, obtain a JWT, and send that token as the Bearer token when calling dependent services such as Trading Instruments Service, Market Data Service, and Transaction Processing Service. The configured system account must have the roles required by those downstream services, including `VIEW_MARKET_DATA` for Trading Instruments and Market Data, and `UPDATE_TRANSACTIONS` for posting trade results to Transaction Processing.

## How

High level implementation should include the following:

1. Create a new Order Execution Service as a .NET Web API microservice following the existing project and solution structure used by the monorepo.
2. Reference the shared entities project and configure `PseudoMarketsDbContext` so the Order Execution Service can read from the shared PostgreSQL database.
3. Add configuration for the shared PostgreSQL connection string, dependent service base URLs, IDP base URL, and the Order Execution system account credentials. Real system account passwords and connection-string secrets must be supplied through environment variables or secret-backed deployment configuration, not committed source-controlled settings.
4. Implement a service-to-service token provider that calls `POST /api/identity/authenticate` using the configured system account login ID and password, caches the returned JWT until shortly before expiration, and refreshes it when needed.
5. Use the system account Bearer token for downstream calls to Trading Instruments Service, Market Data Service, and Transaction Processing Service.
6. Expose an authenticated REST API endpoint for submitting market orders. The request should include user ID, symbol, side, and quantity. The core foundation should not accept client-submitted limit, stop, or stop limit price fields.
7. Authenticate and authorize the incoming order submission request before performing business validation. The endpoint should require the caller to be allowed to execute trades.
8. Inspect the validated incoming token to determine whether it is a system token or user token. With the current IDP token shape, a system token should be identified by the `roles` claim containing one or more roles that are only assigned to system accounts, such as `UPDATE_TRANSACTIONS`, `UPDATE_BALANCES`, or `UPDATE_INSTRUMENTS`. A system token may submit an order for any payload user ID. A user token must contain an `id` claim that matches the payload user ID.
9. Reject the order when a user token is missing an `id` claim, has a malformed `id` claim, or has an `id` claim that does not match the payload user ID.
10. Normalize the submitted symbol by trimming whitespace and converting it to uppercase.
11. Validate that the normalized symbol contains only letters and numbers. Reject symbols that contain special characters, separators, class-share delimiters, exchange suffixes, whitespace, or other non-alphanumeric characters before calling dependent services or reading positions.
12. Call the Trading Instruments Service during order validation using `GET /api/trading-instruments/{symbol}` to retrieve the submitted symbol.
13. Validate the returned `TradingInstrumentResponse` fields needed for order entry: `symbol`, `tradingStatus`, `primaryInstrumentType`, and `secondaryInstrumentType`.
14. Reject the order if the Trading Instruments Service does not return a matching instrument, returns an instrument with trading disabled, returns an instrument type that is not supported by this initial release, or cannot be reached successfully during validation.
15. Continue validation only after the instrument has been confirmed as tradable. Remaining validation includes user validation, quantity validation, settled cash validation for buy orders, and settled position validation for sell orders.
16. Validate that quantity is greater than zero.
17. Call the Market Data Service using `GET /api/MarketData/quote/{symbol}` to retrieve `QuoteResponse` and use `QuoteResponse.Price` to calculate projected trade value. Reject the order if a quote cannot be retrieved or if the returned price is not greater than zero.
18. For buy orders, use the submitted user ID to read `AccountBalanceEntity` from the shared PostgreSQL `account_balances` table and validate that `SettledCashBalance` can cover the projected trade amount. `CashBalance` and `UnsettledCashBalance` must not be included in this validation.
19. For sell orders, use the submitted user ID and normalized symbol to read `PositionEntity` from the shared PostgreSQL `positions` table and validate at the symbol level that `SettledQuantity` can cover the submitted quantity. `Quantity` and `UnsettledQuantity` must not be included in this validation.
20. Reject buy orders when no `account_balances` row exists for the submitted user ID, and reject sell orders when no `positions` row exists for the submitted user ID and symbol.
21. Fill accepted market orders immediately at `QuoteResponse.Price`.
22. Post the completed trade execution to the Transaction Processing Service using its protected trade-posting API so settled/unsettled balance and position effects are recorded by the transaction-processing domain. Transaction Processing remains responsible for lot-level settled-share validation during sell-side trade posting.
23. Persist the accepted order, immediate simulated execution, `QuoteResponse.Price` used for the fill, downstream transaction reference, and any resulting execution state in the appropriate persistence layer for the Order Execution Service.
24. Return a clear validation failure response for unsupported symbols, unsupported symbol formats, invalid users, user ID ownership violations, missing balances or positions, insufficient settled cash, insufficient settled position quantity, unsupported order types, authentication/token acquisition failures, downstream authorization failures, and market data failures so callers can distinguish these failures from authorization or malformed request failures.

Initial symbol support should align to the Trading Instruments Service data model. For the core foundation, supported instruments should be limited to instruments whose primary instrument type is `Equity` and whose trading status is enabled, unless a later PRD explicitly expands support to derivatives or cryptocurrencies.

## Acceptance Criteria

- [ ] Order submission API accepts market orders with user ID, symbol, side, and quantity.
- [ ] Order Execution Service references the shared entities project and configures `PseudoMarketsDbContext` for shared PostgreSQL reads.
- [ ] Order Execution Service configuration supports shared PostgreSQL connection string, IDP base URL, system account login ID, system account password, and dependent service base URLs.
- [ ] System account passwords and connection-string secrets are supplied through environment variables or secret-backed deployment configuration and are not committed to source-controlled settings.
- [ ] Order Execution Service authenticates to the IDP using `POST /api/identity/authenticate` before calling protected dependent services.
- [ ] Order Execution Service caches and refreshes the system account JWT according to the token expiration returned by the IDP.
- [ ] Downstream calls include the IDP-issued JWT as a Bearer token.
- [ ] The configured system account has the roles required for dependent services, including `VIEW_MARKET_DATA` and `UPDATE_TRANSACTIONS`.
- [ ] Orders are rejected when the Order Execution Service cannot obtain a valid system account token.
- [ ] Orders are rejected when a dependent service rejects the system account token as unauthorized or forbidden.
- [ ] Incoming order submission requests are authenticated and authorized before unsupported-symbol validation or other business validation runs.
- [ ] Incoming order submission requests require the caller to be authorized to execute trades.
- [ ] System tokens, identified by system-only roles in the token `roles` claim, are allowed to submit orders for any user ID in the order payload.
- [ ] User tokens are allowed to submit orders only when the token `id` claim matches the user ID in the order payload.
- [ ] Orders submitted with user tokens are rejected when the token `id` claim is missing, malformed, or does not match the payload user ID.
- [ ] Orders are rejected when the user ID is missing, malformed, or does not resolve to a valid user.
- [ ] Orders are rejected when quantity is zero or negative.
- [ ] Orders are rejected when the order side is not buy or sell.
- [ ] Orders are rejected when the request attempts to submit limit, stop, stop limit, or other contingent order types.
- [ ] Orders are rejected when the request includes client-submitted limit, stop, or stop limit price fields.
- [ ] Submitted symbols are trimmed and uppercased before symbol validation.
- [ ] Orders are rejected when the normalized symbol contains special characters, separators, class-share delimiters, exchange suffixes, whitespace, or other non-alphanumeric characters.
- [ ] Trading Instruments Service, Market Data Service, and position lookups are not called when symbol format validation fails.
- [ ] Order validation calls `GET /api/trading-instruments/{symbol}` in the Trading Instruments Service before accepting or executing an order.
- [ ] The Order Execution Service is configured to call the Trading Instruments Service with authorization for the existing `VIEW_MARKET_DATA` protected endpoint.
- [ ] Orders for symbols not found in the Trading Instruments Service are rejected with a clear validation error.
- [ ] Orders for symbols found in the Trading Instruments Service but marked as not tradable are rejected with a clear validation error.
- [ ] Orders for instrument types outside the initial supported scope are rejected with a clear validation error.
- [ ] Orders are rejected when the Trading Instruments Service cannot be reached or returns an unexpected validation response.
- [ ] Orders only proceed to user, balance, market data, and execution processing after symbol support has been confirmed.
- [ ] Market orders use `GET /api/MarketData/quote/{symbol}` in the Market Data Service to retrieve `QuoteResponse` before calculating projected trade value.
- [ ] Market orders use `QuoteResponse.Price` as the projected-value price and immediate fill price.
- [ ] Market orders are rejected when the Market Data Service cannot provide `QuoteResponse` for the submitted symbol.
- [ ] Market orders are rejected when `QuoteResponse.Price` is less than or equal to zero.
- [ ] Accepted market orders are filled immediately using `QuoteResponse.Price`.
- [ ] The `QuoteResponse.Price` value used for the immediate fill is persisted with the order execution record.
- [ ] Completed market-order executions are posted to the Transaction Processing Service using the system account token.
- [ ] The resulting transaction reference from Transaction Processing is persisted with the order execution record when posting succeeds.
- [ ] Buy order validation calculates projected trade amount from quantity and `QuoteResponse.Price` before accepting the order.
- [ ] Buy order validation reads the submitted user's `account_balances` row directly from shared PostgreSQL using `PseudoMarketsDbContext`.
- [ ] Buy orders are accepted only when the user has enough settled cash to cover the projected trade amount.
- [ ] Buy orders are rejected when settled cash is insufficient, even if aggregate cash or unsettled cash would otherwise cover the projected trade amount.
- [ ] Buy orders are rejected when no `account_balances` row exists for the submitted user ID.
- [ ] Sell order validation reads the submitted user's `positions` row for the normalized symbol directly from shared PostgreSQL using `PseudoMarketsDbContext`.
- [ ] Sell orders are accepted only when the user has enough symbol-level settled position quantity for the submitted symbol.
- [ ] Sell orders are rejected when settled position quantity is insufficient, even if aggregate position quantity or unsettled position quantity would otherwise cover the submitted quantity.
- [ ] Sell orders are rejected when the user has no settled position for the submitted symbol.
- [ ] Order Execution Service does not perform lot-level settled-share validation for sell orders.
- [ ] Transaction Processing Service remains responsible for lot-level settled-share validation when sell-side trade executions are posted.
- [ ] Order Execution Service does not mutate `account_balances` or `positions` directly; trade-related writes remain owned by Transaction Processing after the market order is filled.
- [ ] Short selling is not allowed by the core foundation.
- [ ] Validation failure responses distinguish unsupported symbols, unsupported symbol formats, unsupported order types, invalid users, user ID ownership violations, market data failures, token acquisition failures, downstream authorization failures, missing balances, missing positions, insufficient settled cash, insufficient settled position quantity, authorization failures, and malformed requests.
- [ ] Unit tests cover successful token acquisition, token caching and refresh behavior, token acquisition failure, downstream unauthorized/forbidden responses, system-token submission for any user ID, user-token submission with matching payload user ID, user-token rejection for missing/malformed/mismatched `id` claims, successful symbol validation, unsupported symbols, unsupported symbol formats, disabled symbols, unsupported instrument types, downstream Trading Instruments Service failure behavior, unsupported order types, rejected client-submitted price fields, invalid users, invalid quantities, market data failures, invalid `QuoteResponse.Price`, immediate market-order fills, persisted fill quote price, transaction-processing posting, persisted transaction references, direct balance reads, missing balances, sufficient settled cash, insufficient settled cash with unsettled cash present, direct position reads, sufficient settled position quantity, insufficient settled position quantity with unsettled quantity present, missing positions, and short sell rejection.

## Out Of Scope

<!-- Clarify what will not be included in this feature. -->

- Support for derivatives, options, cryptocurrencies, or other non-equity instruments.
- Limit orders, stop orders, stop limit orders, trailing stops, and other contingent order types.
- The future contingent-orders submodule that will manage trigger conditions, deferred execution, and order lifecycle for non-market orders.
- Management of trading instrument records. Instrument creation, updates, and enablement are owned by the Trading Instruments Service.
- Margin, unsettled-funds buying power, and short selling.
- Building a new OAuth client credentials flow. The core foundation will use the existing IDP username/password authentication endpoint with a system account.
- Direct balance or position writes from Order Execution. Balance and position mutations are owned by Transaction Processing.
- Symbols with special characters, separators, class-share delimiters, exchange suffixes, or other non-alphanumeric characters.
- Frontend order ticket behavior.

## Open Questions

<!-- Capture decisions, unknowns, or follow-ups before implementation starts. -->

- None

## Notes

<!-- Add supporting context, links, diagrams, examples, or references. -->

- The Trading Instrument Database PRD defines the Trading Instruments Service as the source of tradable instrument data.
- The Trading Instruments Service README and controller define `GET /api/trading-instruments/{symbol}` as the existing symbol lookup API. The endpoint returns `TradingInstrumentResponse` and requires `VIEW_MARKET_DATA`.
- The Trading Instrument Business Rules define trading status as the indicator that determines whether a specific instrument can be traded on the platform.
- The Settled and Unsettled Balances and Positions PRD defines settled cash as the source for buy-order buying power and settled position quantity as the source for sell-order availability.
- The shared entities project maps `AccountBalanceEntity` to `account_balances` and `PositionEntity` to `positions` through `PseudoMarketsDbContext`.
- Order Execution performs sell-side settled position validation at the symbol level. Transaction Processing owns lot-level settled-share validation during trade posting.
- Incoming order submission authorization happens before unsupported-symbol validation.
- IDP tokens include an `id` claim containing the authenticated account's user ID and a `roles` claim containing the account roles. In the current IDP model, system accounts receive system-only roles such as `UPDATE_TRANSACTIONS`, `UPDATE_BALANCES`, and `UPDATE_INSTRUMENTS`, while user accounts do not. Order Execution uses those claims to distinguish system-token behavior from user-token ownership checks.
- Initial symbol normalization trims and uppercases input, then accepts only alphanumeric symbols.
- The Market Data Service exposes `GET /api/MarketData/quote/{symbol}` and returns `QuoteResponse`; Order Execution uses `QuoteResponse.Price` for projected trade value and immediate market-order fills.
- The IDP README and controller define `POST /api/identity/authenticate` as the existing endpoint for exchanging `loginId` and `password` for a JWT.
- The Transaction Processing Service protects trade posting with `UPDATE_TRANSACTIONS`.
