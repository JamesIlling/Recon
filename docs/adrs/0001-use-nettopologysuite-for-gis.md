# ADR-0001: Use NetTopologySuite for GIS Support in the API

- **Status**: Accepted
- **Date**: 2026-05-02
- **Deciders**: Project Team

## Context

The application requires spatial/GIS capabilities — storing, querying, and returning geographic data (points, polygons, routes, etc.). SQL Server supports `GEOGRAPHY` and `GEOMETRY` column types natively, but EF Core needs a spatial provider to map .NET types to those columns.

## Decision

Use **NetTopologySuite (NTS)** via the `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` NuGet package as the spatial provider for EF Core.

## Rationale

- NTS is the de-facto standard geometry library for .NET.
- The EF Core SQL Server provider has first-class NTS integration.
- NTS types (`Point`, `LineString`, `Polygon`, `Geometry`) map directly to SQL Server spatial types.
- Spatial LINQ queries (e.g., `.Where(x => x.Location.Distance(point) < radius)`) are translated to native SQL spatial functions.

## Consequences

- All spatial properties on EF Core entities must use NTS types (e.g., `NetTopologySuite.Geometries.Point`).
- The `DbContext` must call `.UseNetTopologySuite()` when configuring the SQL Server provider.
- Coordinate reference system (CRS/SRID) must be set consistently — default to **SRID 4326** (WGS 84) for geographic data.
- Raw SQL spatial queries should be avoided in favour of NTS LINQ expressions.
