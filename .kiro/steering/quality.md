---
inclusion: always
---

# Quality Rules

## Definition of Done

A feature is NOT complete until ALL of the following pass with no regressions:

1. **Unit tests** — `dotnet test`
2. **Mutation tests** — `dotnet stryker` at ≥ 80% score
3. **UI automation tests** — Playwright happy path and critical journeys
4. **Accessibility** — `@axe-core/playwright` passes with zero critical or serious violations on all affected pages; keyboard navigation and screen reader smoke test completed manually (see `accessibility.md`)
5. **SAST scan** — zero findings (`semgrep --config=p/owasp-top-ten`)
6. **SCA scan** — no high/critical vulnerabilities (`dotnet list package --vulnerable` + `npm audit`)
7. **Secrets scan** — zero detected secrets (`pre-commit run --all-files`)

---

## Testing Pyramid

Follow the pyramid — many unit tests, fewer integration tests, minimal E2E tests.

| Layer | Target share | Tool |
|---|---|---|
| Unit | ~70% | xUnit + Moq (backend), Vitest + RTL (frontend) |
| Integration | ~20% | xUnit with real DB / HTTP |
| E2E | ~10% | Playwright |

---

## Unit Testing Standards

- Every public method on a service, repository, or controller MUST have unit tests.
- Follow **Arrange / Act / Assert** — three sections separated by blank lines, no `// arrange` comments.
- One behaviour per test. One method call in the Act section.
- Test names describe the scenario and expected outcome: `GetUser_WhenNotFound_Returns404`.
- Use Moq to isolate the unit — assert on outcomes, not just that mocks were called.
- Minimum **≥ 80% line coverage**; coverage alone is not sufficient — tests must verify real behaviour.

### AAA Anti-patterns to avoid
- Multiple acts in one test — split them
- Shared mutable state between tests — each test arranges its own world
- Assert sections longer than 5 lines — you're testing too much at once

---

## Mutation Testing (Stryker.NET)

- Run on demand or in a dedicated CI stage — not on every build.
- Minimum score: **80%** (set in `stryker-config.json`).
- Surviving mutants must be killed with additional tests or explicitly excluded with justification.

---

## UI Automation (Playwright)

- Every user-facing feature needs at least one E2E test covering the happy path.
- Critical journeys (login, data submission, map interaction) need full scenario coverage.
- Tests run against a real running instance — not mocked endpoints.
- All Playwright tests must pass before a feature branch is merged.

---

## Test File Conventions

- Backend: mirror source structure, suffix `Tests` — e.g. `UserServiceTests.cs`
- Frontend: co-located with component, suffix `.test.tsx` or `.spec.tsx`
- E2E: `tests/e2e/`, named by feature and scenario

---

## QA Review Triggers

Stop and ask before proceeding WHEN:
- A new feature has no corresponding test plan
- Business logic changes without test updates
- A change would reduce coverage or mutation score below threshold
- UI behaviour changes without Playwright test updates
