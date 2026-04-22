# Product Requirements Document

## Feature Name
Pseudo Markets Order Execution System - Core Foundation

## Description
The order execution system is a critical component of the Pseudo Markets trading platform. It will simulate a combined order entry, order execution, and FIX system. 

## Problem Statement
In order to provide an accurate paper trading experience to users, we need an order execution system to simulate placing trades and having them filled at an exchange or market maker. The order execution system will serve to simulate that experience, where users can submit buy or sell side trades. 

## Why
The Order Execution System is a backend platform used to facilitate the paper trading experience. Without a proper system to handle order entry and simulated execution, the user can not hold a simulated position in a particular security. This is integral to the concept of paper trading, users must be able to buy and sell stocks easily just as they would through an actual broker. 

## Audience
The system is a beckend service that will be exposed via REST API. It will be called via frontend systems that users will interact with, such as a web app. 

## What
The Core Foundation of the Order Execution System will need to be able to accept the following parameters for order entry: a user ID to execute the trade against, symbol for the security being traded, the side of the trade (buy or sell), and order pricing constraints (market, limit, stop limit). After the order is recieved, it needs to perform basic validation checks, such as ensuring the user ID is valid, and that the valid has sufficient cash balance to cover the trade amount (defined as quantity * price of the security). If the order is a market order, the Order Execution System should call the Market Data Service to retrieve the current price of the security. This value should be used to calculate the projected value of the trade.

## How

<!-- How should this be implemented at a high level? Include architecture notes, dependencies, services, data stores, APIs, or integration points. -->

## Acceptance Criteria

<!-- Define the conditions that must be true for this feature to be accepted. -->

- [ ] 
- [ ] 
- [ ] 

## Out Of Scope

<!-- Clarify what will not be included in this feature. -->

- 

## Open Questions

<!-- Capture decisions, unknowns, or follow-ups before implementation starts. -->

- 

## Notes

<!-- Add supporting context, links, diagrams, examples, or references. -->
