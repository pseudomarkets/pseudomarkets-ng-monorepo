---
name: trading-instrument-business-rules
description: Defines business rules for trading instruments as they are stored in the Trading Instrument Database
  building new endpoints, designing APIs, or implementing CRUD operations.
version: 1.0.0
author: Shravan Jambukesan
tags:
  - instruments
  - securities
  - stocks
  - etfs
---

# Trading Instrument Business Rules and Context

## Overview

This skill helps define the business context for a instrument that can be traded within the Pseudo Markets platform

## When to Use

- Developing new code related to querying or updating the Trading Instrument Database
- Seeding the Trading Instrument Database for the first time

## Rules

### What is an instrument?
A financial instrument in this context is a security that can be traded on the Pseudo Markets platform. This includes common stocks, exchange traded funds (ETFs), derivatives (options contracts), and cryptocurrencies (BTH/USD). Not all types of instruments may be supported when the platform initially launches, but the platform will be designed to support future growth. 

### Instrument attributes
Instruments must have the following basic attributes:
1. Symbol - The primary identifier of the instrument
2. Description - Description of the instrument, which can be the company name or fund name for stocks or ETFs
3. Trading Status - Indicator to determine if a specific instrument can be traded on the platform, set to true to enable and false to disable
4. Primary Instrument Type - Top level classification for an instrument, see possible classifications below
5. Secondary Instrument Type - Secondary classification for a given primary type, see possible classifications below
6. Closing price - The last trade price at the end of a trading day
7. Closing price date - The date on which the closing price was recorded
8. Source - Indicates how the instrument was loaded into the database, from a reference seed file or manual entry

### Primary Instrument Type
The following table describes the possible primary instrument types:
| Primary Instrument Type | Definition | Example |
|-----------|--------|-------------|
| Equity | Shares of a public traded company or fund | AAPL |
| Derivative | Financial contracted derived from an underlying asset, such as an equity | AAPL 4/22/2026 270.00 Call |
| Cryptocurrency | A digital asset, traded on a blockchain network | BTC/USD |

### Secondary Instrument Type
The following table describes the possible secondary instrument types for a given primary instrument type:
| Primary Instrument Type | Secondary Instrument Type | Definition | Example |
|-----------|--------|-------------|-------------
| Equity | Common Stock | Common stock for a corporation | AAPL |
| Equity | ETF | An exchange traded fund | SPY |
| Derivative | Call Option | Call side options contract | AAPL 4/22/2026 270.00 Call |
| Derivative | Put Option | Put side options contract | AAPL 4/22/2026 270.00 Put |

### Seeding the Trading Intruments Database
Seeding the database will require using the reference data files stored under reference-data folder under the root directory of the monorepo. These CSV files should be converted to SQL scripts so they can be inserted into the PostgreSQL database. The following steps should be followed for each file, in this specific order. The database should have a unique constraint on the primary key (symbol), so the seed scripts should be able to handle a rejected insert gracefully if attempting to insert a duplicate row. Create scripts for each of the following files using the following instructions:

1. nasdaqlisted.csv - Prepare a SQL script that will insert all the rows in this pipe delimited file. Map the symbol to the symbol column, security name to description column, primary instrument type to Equity for all rows, and the secondary instrument type to Common Stock if the ETF indicator is set to N, else set it to ETF. Set the closing price to 0.00 and the closing date to the current date for all rows. Set the source to "NASDAQ Reference Data".

2. NYSE_Arca_ETFs.csv - Prepare a SQL script that will insert all the rows in this comma seperated file. Map the symbol to the symbol column, the ETF name to the description column, primary instrument type to Equity for all rows, secondary instrument type to ETF for all rows, closing price to 0.00 for all rows, closing date to the current date for all rows, and set the source to "NYSE Arca ETF Reference Data". 

3. nyse-listed.csv - Prepare a SQL script that will insert all the rows in this comma seperated file. Map the ACT Symbol to symbol column, Company Name to description column, and set the primary instrument type to Equity for all rows and secondary instrument type to Common Stock for all rows, closing price to 0.00 for all rows, and closing date to the current date for all rows. Set the source to "NYSE Reference Data" for all rows. 

When executing the seed scripts against the database, the same ordering should be followed for execution. This is because we want to prefer the ETF data sourced from NASDAQ and NYSE Arca over the consolidated NYSE file. 