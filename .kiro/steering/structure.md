---
inclusion: always
---

# Project Structure

```
.kiro/
  steering/         # AI assistant steering documents
docs/
  adrs/             # Architecture Decision Records
  rfcs/             # Requests for Comments
  security/         # Pen test and dependency-check reports
  runbooks/         # Operational runbooks
infra/
  aws/              # AWS Terraform (networking, compute, database, cdn, iam)
  azure/            # Azure Terraform (networking, compute, database, cdn, identity)
  shared/           # Cloud-agnostic Terraform modules
src/
  AppHost/          # .NET Aspire AppHost — local dev orchestration
  ServiceDefaults/  # Shared OTel, health checks, resilience
  Api/              # ASP.NET Core Web API (C#)
  Api.Tests/        # xUnit tests — mirrors Api/ structure
  client/           # React + TypeScript frontend (Vite)
tests/
  e2e/              # Playwright end-to-end tests
```

> `src/` and `infra/` do not exist yet — this is the intended layout.

## Conventions

- **C# files**: PascalCase — `UserController.cs`, `SpatialQueryService.cs`
- **React components**: PascalCase — `MapView.tsx`, `UserCard.tsx`
- **React hooks**: camelCase prefixed with `use` — `useMapData.ts`
- **Test files**: suffix `Tests.cs` (backend) or `.test.tsx` / `.spec.tsx` (frontend)
- **ADR / RFC files**: `XXXX-kebab-description.md`
- `stryker-config.json` lives in `src/Api.Tests/`
- EF Core `DbContext` and migrations live in `src/Api/`
