# Product Requirements Document

## Feature Name
Pseudo Markets Order Execution System - Order Queue Component

## Description
The Pseudo Markets Order Execution Service needs an order queue component so that orders submitted outside of normal NYSE trading hours can still be accepted and persisted for later execution. Orders submitted during market hours on valid market days should continue through the normal immediate execution flow. Orders submitted when the market is closed should be saved to a database table so a future batch process can read and execute them when the market is open.

## Problem Statement
The current order execution model is geared toward immediate processing. That works for market-day submissions during open trading hours, but it does not provide a proper experience for orders entered after hours, on weekends, or on market holidays. Users still need a way to place orders when the market is closed, and the platform needs a durable queue so those orders can be executed later instead of being rejected outright.

## Why
Supporting queued orders makes the trading simulation more realistic and more usable. Users should be able to submit orders outside of market hours just as they would through a real brokerage platform. Persisting queued orders also provides a clear handoff point for future batch-based execution logic and avoids losing user intent when the market is closed.

## Audience
This feature is being built for:

- End users submitting orders through client applications backed by the Pseudo Markets platform
- Frontend and API clients that need a consistent order submission experience regardless of market state
- Future backend batch-processing components that will read queued orders and execute them
- Platform developers maintaining the Order Execution Service and shared database model

## What
The Order Execution Service should determine whether the market is currently open before deciding how to handle an incoming order.

If an order is submitted on a valid market day during NYSE trading hours, the service should continue using the current immediate execution behavior.

If an order is submitted outside of this window, the service should accept the request but persist the order into a queue table instead of executing it immediately.

For this PRD, market-open eligibility should be defined as:

- The current date is a market day
- The current time is between `9:30 AM` and `4:00 PM` in the `America/New_York` time zone

A market day should be defined as:

- Monday through Friday
- Excluding dates stored in the shared `market_holidays` table

Queued orders should preserve the full order submission intent needed for later execution. At minimum, the queued record should capture:

- A queue record identifier
- The user ID
- The symbol
- The order side
- The quantity
- The order type
- The timestamp the order was submitted
- The market-status reason for queueing
- A queue status value

This feature should not execute queued orders. It should only decide whether an order should execute immediately or be written to the queue for later processing.

The order submission API surface should remain within the Order Execution Service. The same order submission endpoint should continue to accept requests, with the service deciding whether the request results in immediate execution or queue persistence.

The response contract should be expanded so callers can tell whether the order was:

- executed immediately, or
- accepted into the queue for later execution

## How
Implementation should remain inside the Pseudo Markets Order Execution Service and its existing relational persistence model.

The feature should be implemented using the shared PostgreSQL database rather than Aerospike. A new relational table should be added for queued orders, and the corresponding Entity Framework model should be added to the shared entities project so it can participate in the shared `PseudoMarketsDbContext`.

The Order Execution Service should add a market-hours evaluation component that:

- Converts the current instant into `America/New_York`
- Checks whether the local day is a weekday
- Checks whether the date exists in `market_holidays`
- Checks whether the local time falls within the NYSE trading session window

The order submission workflow should be updated so market-hours eligibility is determined after authentication and authorization, but before immediate execution-specific downstream calls are made.

If the market is open:

- Continue through the existing validation and immediate execution flow

If the market is closed:

- Persist a queued-order record
- Return a success response indicating the order was queued
- Do not call Market Data, Transaction Processing, or any immediate execution path

The queue record should be durable and should support future processing by a separate batch component. That future batch reader is expected to load pending queued orders from the database and execute them, but that processing flow is explicitly out of scope for this PRD.

The design should avoid duplicating future execution logic inside the queue component. The queue component is only responsible for admission-time decisioning and persistence.

## Acceptance Criteria

- [ ] The Order Execution Service evaluates whether an incoming order was submitted during NYSE market hours using the `America/New_York` time zone.
- [ ] Market-open evaluation treats Monday through Friday as eligible trading days, excluding dates present in the shared `market_holidays` table.
- [ ] Orders submitted during market hours on valid market days continue through the existing immediate execution path.
- [ ] Orders submitted outside market hours are accepted and persisted to a relational queued-orders table instead of being executed immediately.
- [ ] The queued-orders table is modeled through the shared entities project and the shared `PseudoMarketsDbContext`.
- [ ] Queued orders store the core order submission fields plus queue metadata such as submission timestamp, queue status, and queue reason.
- [ ] The order submission response clearly indicates whether the order was immediately executed or queued for later execution.
- [ ] When an order is queued, the Order Execution Service does not call downstream immediate-execution dependencies such as Market Data or Transaction Processing.
- [ ] Unit tests cover market-open submissions, after-hours submissions, weekend submissions, market-holiday submissions, queue persistence behavior, and response-shape differences between immediate execution and queue acceptance.

## Out Of Scope

- Building the batch process that reads queued orders and executes them
- Scheduling, hosting, or orchestrating the future batch processor
- Canceling, modifying, reprioritizing, or manually releasing queued orders
- Limit-order or contingent-order logic beyond the existing supported order-entry surface
- Changing the current immediate execution rules for orders submitted while the market is open
- Notifications, alerts, or frontend UX for queued-order lifecycle events after submission

## Notes

- This PRD extends the behavior defined in [Order_Execution_System_Core_PRD.md](Order_Execution_System_Core_PRD.md).
- The shared `market_holidays` table already exists in the shared entities project and should be reused for market-day evaluation.
- Future implementation planning should decide the final queue status values and exact queued-order response contract.
