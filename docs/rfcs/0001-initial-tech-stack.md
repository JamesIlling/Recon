# RFC-0001: Initial Technology Stack

- **Status**: Accepted
- **Date**: 2026-05-02
- **Author(s)**: Project Team

## Summary

Establish the foundational technology stack for the project: a C# ASP.NET Core Web API backend, a React/TypeScript frontend, and a SQL Server database with GIS capabilities.

## Motivation

A clear, documented technology decision ensures all contributors and AI tooling work within the same constraints and conventions from the start.

## Proposal

### Backend
- C# on .NET 10+
- ASP.NET Core Web API
- Entity Framework Core with NetTopologySuite for spatial/GIS support

### Frontend
- React 18+ with TypeScript
- Vite as the build tool

### Database
- SQL Server 2019+ with `GEOGRAPHY` / `GEOMETRY` types
- Spatial indexes on geometry columns
- EF Core migrations for schema management

### Testing
- xUnit + Moq (unit/integration tests)
- Stryker.NET (mutation testing)
- Vitest + React Testing Library (frontend)

## Alternatives Considered

- **PostgreSQL + PostGIS**: Strong GIS support, but SQL Server was chosen for organisational familiarity and licensing alignment.
- **Next.js**: Considered for the frontend, but plain React + Vite was preferred to keep the frontend decoupled from SSR concerns.

## Consequences

- All backend code must target .NET 10+.
- Spatial queries must use NetTopologySuite types (`Point`, `Polygon`, etc.) rather than raw SQL geometry strings.
- Frontend build output is a static bundle served separately from the API.


