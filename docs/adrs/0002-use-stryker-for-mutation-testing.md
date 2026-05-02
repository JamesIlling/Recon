# ADR-0002: Use Stryker.NET for Mutation Testing

- **Status**: Accepted
- **Date**: 2026-05-02
- **Deciders**: Project Team

## Context

Unit tests can pass while still leaving logic gaps — tests may exist but not actually verify the correct behaviour. Mutation testing provides a quantitative measure of test suite effectiveness by introducing small code changes (mutants) and checking whether tests catch them.

## Decision

Use **Stryker.NET** (`dotnet-stryker`) as the mutation testing tool for the API test project.

## Rationale

- Stryker.NET is the leading mutation testing tool for .NET.
- Integrates with xUnit (the chosen test framework) without additional configuration.
- Produces an HTML report with mutation score and surviving mutants.
- Configurable thresholds allow CI gates on mutation score.

## Consequences

- `dotnet-stryker` must be installed as a .NET global or local tool.
- A `stryker-config.json` file should be maintained in the test project to configure thresholds, excluded files, and reporters.
- Mutation tests are not run on every build — they are run on demand or in a dedicated CI stage due to execution time.
- Target mutation score threshold: **≥ 80%** (adjust in `stryker-config.json` as the project matures).
