# Product Requirements Document

## Feature Name
Pseudo Markets Platform - Batch Processing Framework

## Description
The Pseudo Markets platform needs a reusable batch processing framework that can schedule, coordinate, and execute various background batch jobs. This framework should provide the core infrastructure for defining, configuring, triggering, and monitoring batch processes without implementing the business logic of any specific batch job yet. One expected future use case is executing queued orders, but the framework must be generic enough to support additional batch workloads as the platform grows. The framework should be based on Hangfire and use the Hangfire.PostgreSql storage provider against the platform's shared PostgreSQL server.

## Problem Statement
The platform is beginning to introduce workflows that cannot or should not be completed synchronously during API request handling. Examples include processing queued orders, performing end-of-day routines, reconciling data, and running scheduled maintenance or platform jobs. Without a shared batch-processing framework, each future background process would need to invent its own scheduling, execution, status tracking, and configuration model, leading to duplicated logic and inconsistent operational behavior.

## Why
This feature creates the operational foundation for scalable deferred processing across the Pseudo Markets platform. A common framework will:

- reduce duplicated implementation effort
- standardize scheduling and execution behavior
- make future batch jobs easier to add and maintain
- improve observability and operational control
- provide a durable platform pattern for work that should run outside synchronous API calls

This is especially important now that queued orders exist and will require a future background execution flow.

## Audience
This feature is being built for:

- Platform developers who will add future batch jobs
- Backend services that need deferred or scheduled processing
- Operators and developers who need visibility into batch execution state
- Future platform components such as queued order execution, reconciliation, maintenance jobs, and end-of-day processing

## What
The platform should provide a reusable batch-processing framework that supports the following capabilities:

- Defining a batch job with a unique name or identifier
- Registering one or more batch job implementations in a configurable way
- Scheduling jobs to run on a recurring basis or on-demand
- Preventing overlapping execution for the same job when configured to run as a singleton
- Recording execution metadata such as start time, end time, status, and error information
- Supporting enablement or disablement of specific batch jobs through configuration
- Supporting future growth for job-specific configuration such as cadence, throttling, retry behavior, and execution windows
- Leveraging Hangfire job primitives such as recurring and delayed execution where appropriate for future jobs

The framework should separate the infrastructure for batch execution from the business logic of individual jobs.

For this PRD, the framework should focus on:

- batch job registration
- scheduling model
- runtime execution model
- persistence model for execution tracking
- configuration model
- extensibility points for future jobs

This PRD should not implement the business logic for any specific batch process yet.

The framework should be suitable for future use cases such as:

- executing queued orders
- nightly or scheduled maintenance routines
- reconciliation or cleanup jobs
- time-based platform workflows

## How
The framework should be implemented as a platform-level reusable component in the monorepo rather than being tightly coupled to a single service.

The framework should be based on:

- [Hangfire](https://www.hangfire.io/) for background job scheduling and execution
- [Hangfire.PostgreSql](https://github.com/hangfire-postgres/Hangfire.PostgreSql) for persistent job storage in the shared PostgreSQL environment

At a high level, the architecture should include:

- a shared batch-processing abstraction for defining jobs
- a host process or service capable of running scheduled jobs
- a configuration model for enabling, disabling, and scheduling jobs
- a relational persistence model for batch job definitions and execution history
- a coordination model that avoids duplicate job execution for singleton jobs
- Hangfire server and storage configuration integrated into the platform in a reusable way

The framework should be designed so future batch processes can be added by:

- implementing a standard batch job interface
- registering the job in dependency injection
- providing configuration for cadence and behavior

The framework should persist execution records in PostgreSQL using Hangfire.PostgreSql against the shared PostgreSQL server, while also supporting any additional platform-owned execution metadata tables needed for operational visibility.

At minimum, the persistence model should be able to capture:

- job name
- execution identifier
- execution status
- scheduled time
- actual start time
- completion time
- error details when execution fails

The execution host should be configurable enough to support both:

- scheduled recurring jobs
- manual or ad hoc invocation patterns later

The design should account for Hangfire-specific platform decisions such as:

- where the Hangfire server will run
- how recurring jobs will be registered
- how job storage will be configured against the shared PostgreSQL server
- how future services will register jobs without tightly coupling themselves to the host runtime

The design should allow future evolution toward:

- more advanced scheduling policies
- retry strategies
- concurrency controls
- partitioned or sharded execution
- multiple job hosts if the platform grows

The framework should also align with the platform's current operational standards, including standardized `/info` and `/health` behavior if a dedicated batch host service is introduced.

## Acceptance Criteria

- [ ] The platform has a documented and implementable framework design for registering and executing batch jobs without embedding business logic for specific jobs.
- [ ] The framework defines a standard abstraction or interface for batch job implementations.
- [ ] The framework supports configuration-driven enablement, disablement, and scheduling of batch jobs.
- [ ] The framework is explicitly based on Hangfire for scheduling and execution and Hangfire.PostgreSql for persistent storage.
- [ ] The framework includes a persistence model for tracking batch job executions in PostgreSQL.
- [ ] The framework supports singleton-style protection so the same batch job does not overlap with itself when configured not to do so.
- [ ] The framework is designed so future jobs such as queued-order execution can plug into it without reworking the core scheduling infrastructure.
- [ ] The framework can support future expansion for retries, throttling, execution windows, and additional batch job types.
- [ ] The framework design clearly separates batch infrastructure concerns from individual job business logic.

## Out Of Scope

- Implementing the queued-order execution batch process itself
- Implementing any other concrete business batch job
- Building a full operational UI for batch job administration
- Distributed multi-node coordination beyond what is needed for an initial framework design
- Advanced retry orchestration, dead-lettering, or workflow-engine behavior unless needed later
- Notifications or alerting for batch job outcomes

## Notes

- This PRD is intended to provide the framework that future queued-order execution will plug into after the queue component introduced in [Order_Execution_Order_Queue_PRD.md](Order_Execution_Order_Queue_PRD.md).
- The framework should be designed for platform-wide reuse rather than being owned solely by the Order Execution Service.
- Future implementation planning should decide whether the first runtime host is a dedicated batch service, a worker project, or another platform-hosted process.
- Hangfire describes itself as a background processing framework for .NET with persistent storage support and recurring or delayed job patterns. Hangfire.PostgreSql is the community PostgreSQL storage provider that should be used with the platform's shared PostgreSQL server.
