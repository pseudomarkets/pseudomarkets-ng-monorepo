# Product Requirements Document

## Feature Name
Pseudo Markets Batch Processing - Queued Order Execution Job

## Description
The Pseudo Markets platform needs its first concrete batch job implementation: a queued-order execution job. This job will run at NYSE market open, read pending orders from the queued-orders table, generate a `SYSTEM` token for service-to-service authentication and authorization, and submit each queued order to the Order Execution Service for normal execution handling. Even though the batch job authenticates with a `SYSTEM` account, each order must still execute on behalf of the original account that placed it.

## Problem Statement
The Order Execution Service can now persist orders that are submitted outside market hours, but those queued orders are not yet processed. Without a batch job to pick them up at market open, queued orders will remain stuck in the database and never transition into actual execution. The platform needs a durable, scheduled mechanism to resume those deferred orders automatically when the market opens.

## Why
This feature is the first real use of the new batch-processing framework and completes the initial queued-order workflow. It matters because it:

- turns the queued-order model into an end-to-end platform capability
- makes after-hours order entry meaningful instead of purely durable storage
- validates the Hangfire-based batch infrastructure with a real production-style job
- preserves correct user ownership by ensuring queued orders execute for the original account rather than the `SYSTEM` service account

## Audience
This feature is being built for:

- End users who submit orders outside market hours and expect them to execute automatically at market open
- The Pseudo Markets Order Execution Service, which needs a batch-driven path for processing deferred orders
- Platform developers maintaining the batch-processing and order-execution components
- Operators and developers who will monitor scheduled job behavior through the Hangfire dashboard

## What
The platform should add a recurring batch job that runs at NYSE market open and processes queued orders that are ready to execute.

At a high level, the job should:

- run right at market open
- read pending queued orders from the relational queued-orders table
- process orders in a deterministic, repeatable order
- authenticate to the IDP using a configured `SYSTEM` account
- obtain a JWT for downstream authorization
- call the Order Execution Service to submit each queued order
- pass the original queued order account number / user ID in the execution request
- avoid substituting the `SYSTEM` account number / user ID for the original order owner

The batch job should treat the `SYSTEM` token only as an authorization mechanism that gives the batch process permission to submit orders on behalf of users. The business identity of the order must remain the original account that created the queued order.

The job should only consider queued orders that are still pending and eligible for execution. It should not reprocess already completed, failed, canceled, or otherwise terminal queue records.

The job should execute through the existing Order Execution Service API path rather than duplicating execution logic inside the batch host. The Order Execution Service should remain the owner of pricing, validation, balance checks, position checks, transaction posting, and order execution persistence.

The job should be scheduled to run at market open. For this initial scope, “market open” should mean `9:30 AM America/New_York` on a valid market day.

This PRD is focused on the first queued-order execution job itself. It is not meant to redesign the overall batch framework, which already exists separately.

## How
Implementation should plug into the existing Hangfire-based batch-processing host introduced in [Platform_Batch_Processing_Framework_PRD.md](Platform_Batch_Processing_Framework_PRD.md).

At a high level, the solution should include:

- a concrete batch job implementation registered with the batch framework
- a repository or service that reads pending queued orders from PostgreSQL
- a client or service that authenticates against the IDP using a configured `SYSTEM` account
- a client that calls the Order Execution Service with the original queued-order user ID / account number
- queue-record state transitions so the same queued order is not processed repeatedly as if it were still untouched

The job should be implemented as an application-level orchestrator, not as a reimplementation of the execution engine.

The job should:

- use the shared PostgreSQL database to read queued orders
- authenticate through the IDP using the existing authentication flow
- call the Order Execution Service over its HTTP API
- rely on Order Execution to enforce current business rules when the market is open

The schedule should be registered as a recurring Hangfire job configured for NYSE market open using the `America/New_York` time zone.

The job should be designed so future enhancements can expand it with:

- batching or throttling
- retry handling
- partial-failure recovery
- better queue status transitions
- market-day guards at the scheduler or job level
- more advanced observability

For this initial implementation, it is acceptable for the job to focus on:

- reading pending queued orders
- submitting them through the existing Order Execution API
- recording enough queue-state changes to prevent obvious duplicate processing during the same execution window

## Acceptance Criteria

- [ ] A concrete queued-order execution batch job exists in the batch-processing host and is registered with the shared Hangfire-based batch framework.
- [ ] The job is scheduled to run at `9:30 AM` in the `America/New_York` time zone.
- [ ] The job reads pending queued orders from the relational queued-orders table in PostgreSQL.
- [ ] The job authenticates with the IDP using a configured `SYSTEM` account and obtains a JWT for downstream service calls.
- [ ] The job submits queued orders to the Order Execution Service through its existing API rather than duplicating order-execution business logic in the batch host.
- [ ] Each submitted order uses the original queued-order user ID / account number from the queue record, not the account number / user ID associated with the `SYSTEM` token.
- [ ] The Order Execution Service is able to authorize and accept the batch-submitted order under the `SYSTEM` token while still executing it for the original account owner.
- [ ] The queued-order job processes only queue records that are still pending and avoids treating already terminal queue records as new work.
- [ ] The queued-order job updates queue state in a way that supports preventing obvious duplicate processing of the same queued order.
- [ ] Unit tests cover schedule registration, queue selection, token acquisition, downstream order submission using the original account number / user ID, and queue-state transition behavior.

## Out Of Scope

- Redesigning the shared Hangfire-based batch framework
- Building a generic admin UI for queued-order operations
- Canceling, editing, reprioritizing, or manually releasing queued orders
- Multi-node distributed coordination beyond the current batch-host design
- Advanced dead-lettering or workflow-orchestration behavior
- Notification or alerting flows for queued-order execution outcomes
- Any changes to how queued orders are admitted into the queue outside market hours

## Notes

- This PRD builds directly on [Order_Execution_Order_Queue_PRD.md](Order_Execution_Order_Queue_PRD.md) and [Platform_Batch_Processing_Framework_PRD.md](Platform_Batch_Processing_Framework_PRD.md).
- The Order Execution Service already supports `SYSTEM` token behavior for submitting orders on behalf of another user, and that capability should be reused rather than bypassed.
- Future planning should decide the final queue-status progression for records as they move from pending to in-flight, succeeded, or failed.
- Future planning should also decide whether the job itself needs a market-day guard in addition to its schedule, especially for market holidays that may still coincide with a recurring cron registration.
