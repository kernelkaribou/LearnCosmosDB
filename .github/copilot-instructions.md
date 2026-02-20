# Copilot Instructions

## Project Overview
This is a learning platform for Azure Cosmos DB. It contains independent modules that demonstrate different Cosmos DB concepts (data modeling, indexing, change feed, etc.) through an interactive web application.

## Architecture
- **web/** — Blazor WebAssembly frontend (.NET 9), deployed to Azure Static Web Apps
- **api/** — ASP.NET Core minimal API (.NET 9), deployed to Azure Container Apps
- **processor/** — .NET 9 console app that reads from the Battle Cabbage Media API and writes Cosmos documents
- **shared/** — Shared class library for models used by web, api, and processor

## Tech Stack
- .NET 9 (C#)
- Blazor WebAssembly
- ASP.NET Core minimal APIs
- Azure Cosmos DB (NoSQL API)
- Docker Compose for local development (Cosmos emulator)

## Conventions
- Each learning module is self-contained: own pages in web/, own endpoints in api/, own processor in processor/
- Shared data models live in shared/DataModeling/
- API returns `DataModelingResponse` with `MediaResults` (documents) and `RequestDiagnostics` (RU cost, query text, etc.)
- Never commit secrets — use environment variables or appsettings.Development.json (gitignored)
- Prefer simplicity and clarity for learning purposes
- Use camelCase for JSON serialization (matches Cosmos SDK config)

## Module: Data Modeling
The first module demonstrates 4 document models (Single, Embedded, Reference, Hybrid) using a movie/media database. The Cosmos database is `DataModeling` with containers named after each model.

