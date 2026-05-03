---
inclusion: always
---

# Task Section Completion Checklist

When completing any section of tasks in `.kiro/specs/location-management/tasks.md`, follow this checklist to ensure quality and prevent regressions.

## Pre-Completion Verification

Before marking a task section as complete, verify:

### 1. All Sub-tasks Marked Complete
- [ ] All checkboxes in the section are marked `[x]`
- [ ] No optional tasks (`*` suffix) are blocking completion
- [ ] Parent task checkbox reflects completion of all required sub-tasks

### 2. Backend Tests (if applicable)
- [ ] Run: `dotnet test src/Api.Tests/LocationManagement.Api.Tests.csproj`
- [ ] Result: **All tests pass** (0 failures)
- [ ] Coverage: Verify ≥ 80% line coverage for new code
- [ ] No regressions in existing tests

### 3. Frontend Tests (if applicable)
- [ ] Run: `npm run test -- --run` (from `src/client/`)
- [ ] Result: **All tests pass** (0 failures)
- [ ] Coverage: Verify ≥ 80% line coverage for new code

### 4. Code Quality
- [ ] Run: `dotnet format LocationManagement.slnx --verify-no-changes`
- [ ] Result: **No formatting issues** (exit code 0)
- [ ] Run: `dotnet build LocationManagement.slnx`
- [ ] Result: **Build succeeds** with zero errors (warnings are acceptable if documented)

### 5. Security Scanning
- [ ] Run: `semgrep --config=p/owasp-top-ten src/`
- [ ] Result: **Zero findings** (no OWASP Top 10 violations)
- [ ] Run: `dotnet list package --vulnerable --include-transitive`
- [ ] Result: **No high/critical vulnerabilities**
- [ ] Run: `npm audit` (from `src/client/` if frontend changes)
- [ ] Result: **No high/critical vulnerabilities**

### 6. Secrets Scan
- [ ] Run: `dotnet husky run --group pre-commit` (or `pre-commit run --all-files`)
- [ ] Result: **Zero detected secrets** (gitleaks passes)

### 7. Documentation
- [ ] All new public methods have XML doc comments (`///`)
- [ ] Complex logic has inline comments explaining the "why"
- [ ] Any architectural decisions are documented in `docs/adrs/` or `docs/rfcs/`
- [ ] ADRs/RFCs created for significant design choices, library selections, or patterns introduced

## Automation

A hook (`task-section-completion-check.kiro.hook`) automatically triggers after each task completes to remind you of this checklist. The hook will ask you to verify all items above before proceeding to the next section.

## Quick Command Reference

```bash
# Run all backend tests
dotnet test src/Api.Tests/LocationManagement.Api.Tests.csproj

# Run all frontend tests
cd src/client && npm run test -- --run

# Format check
dotnet format LocationManagement.slnx --verify-no-changes

# Build check
dotnet build LocationManagement.slnx

# SAST scan
semgrep --config=p/owasp-top-ten src/

# SCA scan (backend)
dotnet list package --vulnerable --include-transitive

# SCA scan (frontend)
cd src/client && npm audit

# Secrets scan
dotnet husky run --group pre-commit
```

## Section-Specific Notes

### Backend Sections (3, 4, 5, 6, 9, 10, 14, 15, 16, 18)
- Always run full backend test suite
- SAST and SCA scans are mandatory
- Mutation testing (`dotnet stryker`) should be run before final merge

### Frontend Sections (7, 8, 11, 12, 13, 17)
- Always run full frontend test suite
- Axe-core accessibility assertions must pass (zero critical/serious violations)
- Playwright E2E tests must cover happy paths

### Mixed Sections (1, 2)
- Run both backend and frontend test suites
- Verify infrastructure changes don't break existing functionality

## Definition of Done Gate

A section is NOT complete until:
1. ✅ All required tests pass
2. ✅ Code formatting is correct
3. ✅ Build succeeds with zero errors
4. ✅ SAST scan returns zero findings
5. ✅ SCA scan shows no high/critical vulnerabilities
6. ✅ Secrets scan passes
7. ✅ No regressions in existing tests

Only after all items above are verified should you mark the section complete and move to the next.
