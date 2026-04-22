## Project Overview
This is a monorepo for an end-to-end stock trading simulation platform, also known as a paper trading platform. It is built with .NET, uses Aerospike and PostgreSQL as persistence layers, and is designed to be packaged to run in Docker or Kubernetes. It contains all microsevices needed to run the entire platform. 

## Build and Test Commands
The root level README.md file contains the latest commands to build, run, and test the application. Each microservice/domain component also has its own README with service specific information. Any time code changes are performed, check to ensure that these README files are still up-to-date. Update them as needed. Use the root level README.md as a reference for creating new service specific README files, as well as updating existing ones. 

## Code Style Guidelines
Follow the Microsoft standards for modern C# and .NET development as closely as possible. The current code base is closely aligned to these standards already, so follow it as reference. Overrides may be provided by the user, or by a PRD during the planning phase. 

## Testing Instructions
Any time new features are added, unit tests must be added. After tests are created, run dotnet test against the entire solution based on the instructions defined in the README. 

## Building New Features and Components
Before implementing any code, use plan mode to plan the implementation. The user will provide a PRD (product requirements document, found under docs/prd) which will be used to drive the planning process. Do NOT use PRDs with the word 'draft' in their file name. The planning process is iterative, only start implementation after the user has accepted the plan. Do NOT make assumptions, always ask the user to verify if something in the PRD or follow up prompts are not clear. This procedure should be followed when adding a new feature or component to the platform, or modifying an existing one. During implementation, take a step by step approach, do not try to build the entire component in one shot. This will allow the user to review and streer the session. Before closing the loop on completing a new feature or component, make sure the entire solution builds and all tests pass. 
