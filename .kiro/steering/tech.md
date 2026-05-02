---
inclusion: always
---

# Tech Stack

## Languages & Runtimes

- **Backend**: C# (.NET 10+)
- **Frontend**: TypeScript / JavaScript (Node.js for tooling)
- **Database**: SQL Server with spatial extensions (SQL Server 2019+ with geography/geometry types)

## Frameworks & Libraries

### Backend
- ASP.NET Core Web API
- Entity Framework Core (with NetTopologySuite for GIS support)
- NetTopologySuite â€” geometry types and spatial operations
- Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite â€” EF Core spatial provider

### Observability & Orchestration
- .NET Aspire (`Aspire.Hosting`, `Aspire.Dashboard`) â€” local dev orchestration and observability dashboard
- `Aspire.ServiceDefaults` â€” shared OTel, health checks, and resilience configuration applied to all services
- `OpenTelemetry.Extensions.Hosting` â€” OTel SDK host integration
- `OpenTelemetry.Instrumentation.AspNetCore` â€” automatic HTTP request tracing
- `OpenTelemetry.Instrumentation.Http` â€” outbound `HttpClient` tracing
- `OpenTelemetry.Instrumentation.EntityFrameworkCore` â€” EF Core query tracing
- `OpenTelemetry.Instrumentation.Runtime` â€” GC and runtime metrics
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` â€” OTLP exporter (cloud-agnostic sink)

### Frontend
- React 18+
- TypeScript
- Vite (build tool)
- React Router (client-side routing)
- Axios or Fetch API (HTTP client)

### GIS / Mapping
- Leaflet â€” interactive map rendering
- Proj4js â€” coordinate projection support (if needed)

## Build Tools & Package Managers

- **Backend**: `dotnet` CLI, NuGet
- **Frontend**: `npm` or `yarn`, Vite

## Test Frameworks

- **Backend**: xUnit, Moq
- **Mutation Testing**: Stryker.NET (`dotnet-stryker`)
- **Frontend**: Vitest, React Testing Library
- **UI Automation (E2E)**: Playwright

## Security Analysis Tools

### SAST (Static Application Security Testing)
- **Backend**: Roslyn Analyzers (built-in), `SecurityCodeScan.VS2019` NuGet package
- **All code**: `semgrep` (CLI, cross-language rules)

### SCA (Software Composition Analysis)
- **Backend**: `dotnet list package --vulnerable` (built-in NuGet audit)
- **Frontend**: `npm audit`
- **Advanced**: OWASP Dependency-Check (`dependency-check` CLI)

## Database

- SQL Server with `GEOGRAPHY` and `GEOMETRY` data types enabled
- Spatial indexes on geometry/geography columns
- Migrations managed via EF Core

## Common Commands

### Backend

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run via Aspire AppHost (starts API, dashboard, and dependencies)
dotnet run --project src/AppHost

# Run API directly (without Aspire orchestration)
dotnet run --project src/Api

# Run tests
dotnet test

# Run Stryker.NET mutation tests (from test project directory)
dotnet stryker

# Run Stryker.NET with a config file
dotnet stryker --config-file stryker-config.json

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project src/Api

# Apply migrations
dotnet ef database update --project src/Api
```

### Frontend

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Run unit tests
npm run test
```

### Security Scanning (SAST / SCA)

```bash
# SCA â€” check for vulnerable NuGet packages
dotnet list package --vulnerable --include-transitive

# SCA â€” audit frontend dependencies
npm audit

# SCA â€” OWASP Dependency-Check (if installed)
dependency-check --project "MyProject" --scan ./src --format HTML --out reports/

# SAST â€” run Semgrep with default ruleset
semgrep --config=auto src/

# SAST â€” run Semgrep with OWASP ruleset
semgrep --config=p/owasp-top-ten src/
```

### E2E / UI Automation (Playwright)

```bash
# Install Playwright and browsers (first time)
npx playwright install

# Run all E2E tests
npx playwright test

# Run tests in headed mode (visible browser)
npx playwright test --headed

# Run a specific test file
npx playwright test tests/e2e/login.spec.ts

# Open Playwright UI mode
npx playwright test --ui

# Show last test report
npx playwright show-report
```

