# Product Requirements Document

## Feature Name
Pseudo Markets - Trading Instrument Database

## Description
The instrument database will serve as an authoritative source for all securities that can be traded across the Pseudo Markets paper trading platform. 

## Problem Statement
To reduce the amount of outbound requests made out to market data providers such as Twelve Data (which are rate limited, as well as costly for paid plans), we need to develop a local database to store all securities that can be traded across the platform. It will use reference data from NASDAQ and NYSE, so it will cover a large group of common stocks and ETFs available in the U.S equities market. 

## Why
This database is needed to control which securities can be traded on the platform as additional features and capabilities are being built out, as well as keep costs down to avoid making too many calls to 3rd party market data providers for information such as descriptions and company names. 

## Audience
The Trading Instrument Database will be used to render stock/ETF descriptions (ex: AAPL is Apple Inc) in future frontend UIs, as well as various backend systems such as the Order Execution Service to validate if a given symbol can be traded in the platform. 

## What
Data will be stored in the shared Pseudo Markets PostgreSQL database under a new table. It will made accessible via REST with a .NET Web API that will allow backend and frontend systems to query it using a symbol. It should also allow adding new instruments, which will be done through a mangement UI that will be developed in the future. 

## How
High level implementation can be found below:
1. Create a new project folder at the root level of the monorepo called pseudomarkets-nextgen-instrument-db
2. Create a new solution under that folder called PseudoMarkets.ReferenceData.TradingInstruments. Follow the other microservices as reference to create the project structure and type (.NET 10 Web API). 
3. The new project should use Entity Framework Core to interface with the shared Pseudo Markets PostgreSQL database. Put all entities and migration related files in the Shared Entities project and reference that in the new project. Entities will be defined based on the database schema in step 4. 
4. A new database table will need to be created, call it trading_instruments. It should contain a primary key column with unique constraint for the symbol (string), and columns for description (string), trading status (bool), primary instrument type (string), secondary instrument type (string), closing price (double), closing price date (date), and source (string). Follow standard Entity Framework and PostgreSQL naming conventions. 
5. The database should be seeded using reference data values; create a SQL script for seeding the database instead of using Entity Framework migartions. Reference data can be found under the reference-data folder. Refer to the business rules for seeding this database under the docs/skills folder. Put the database scripts under the infrastructure/postgres folder. 
6. The API should have endpoints for updating the closing price of a security (which will be done on a daily basis by a future batch scheduling app), retrieving a security using its symbol, and adding a new security to the table. The API should be secured using the Identity Server for authorization, and should utilize the Shared Auth library. It should accept tokens with a new role that should be granted to system users called "UPDATE_INSTRUMENTS". 
7. Docker and infra related files should be updated to accomodate this new service, along with a step to run the SQL scripts for seeding the database. 


## Acceptance Criteria
1. New database table (trading_instruments) created under the shared Pseudo Markets PostgreSQL database to store tradable instrument data
2. Seed scripts created to seed the database with NYSE and NASDAQ listed ETF and common stock data
3. .NET 10 Web API created to support updating closing prices, adding new securities, and the primary use case of retrieving a security using its symbol. 

## Out Of Scope
- Integration with any other services, further integration will be done at a later time


## Notes
None
