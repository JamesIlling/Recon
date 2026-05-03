---
inclusion: always
---

# Testing Automation & Quality Gates

This document describes the automated testing and quality gates that ensure every section of the Location Management spec is completed to the highest standards.

## Automated Hooks

Three hooks work together to enforce quality at every stage of development:

### 1. Pre-Task Test Validation (`pre-task-test-validation.kiro.hook`)
**Trigger**: Before a spec task starts (`preTaskExecution`)

**Purpose**: Verify the codebase is in a healthy state before starting new work.

**Action**: Runs `dotnet test src/Api.Tests/LocationManagement.Api.Tests.csproj` and reports pass/fail summary.

**Benefit**: Catches regressions from previous work early, before new changes are made.

---

### 2. Build Validation on Code Write (`build-validation-on-write.kiro.hook`)
**Trigger**: After any code file is written (`postToolUse` with `toolTypes: write`)

**Purpose**: Catch compilation errors immediately after code changes.

**Action**: Runs `dotnet build LocationManagement.slnx --no-restore` and reports last 5 lines of output.

**Benefit**: Prevents broken code from being committed; provides immediate feedback during development.

---

### 3. Task Section Completion Check (`task-section-completion-check.kiro.hook`)
**Trigger**: After a spec task completes (`postTaskExecution`)

**Purpose**: Enforce the full Definition of Done before marking a section complete.

**Action**: Asks the agent to verify all 7 steps of the Task Section Completion Checklist:
1. All sub-tasks marked complete
2. Backend tests pass (if applicable)
3. Frontend tests pass (if applicable)
4. Code quality checks pass (formatting, build)
5. Security scans pass (SAST, SCA)
6. Secrets scan passes
7. Documentation is complete

**Benefit**: Prevents incomplete or low-quality work from being marked done; ensures consistent standards across all sections.

---

## Manual Verification Checklist

While hooks provide automation, the following manual checks are still required before marking a section complete:

### For Backend Sections (3, 4, 5, 6, 9, 10, 14, 15, 16, 18)

```bash
# 1. Run all backend tests
dotnet test src/Api.Tests/LocationManagement.Api.Tests.csproj

# 2. Verify code formatting
dotnet format LocationManagement.slnx --verify-no-changes

# 3. Build the solution
dotnet build LocationManagement.slnx

# 4. Run SAST scan
semgrep --config=p/owasp-top-ten src/

# 5. Check for vulnerable dependencies
dotnet list package --vulnerable --include-transitive

# 6. Run secrets scan
dotnet husky run --group pre-commit

# 7. (Optional) Run mutation tests
dotnet stryker --config-file src/Api.Tests/stryker-config.json
```

### For Frontend Sections (7, 8, 11, 12, 13, 17)

```bash
# 1. Run all frontend tests
cd src/client && npm run test -- --run

# 2. Check for vulnerable dependencies
npm audit

# 3. Run Playwright E2E tests (if applicable)
npx playwright test

# 4. Run accessibility checks
npx playwright test --grep @accessibility
```

### For Mixed Sections (1, 2)

Run both backend and frontend checks above.

---

## Quality Gates

A section is **NOT** complete until ALL of the following pass:

| Gate | Command | Expected Result |
|------|---------|-----------------|
| **Unit Tests** | `dotnet test src/Api.Tests/...` | 0 failures |
| **Code Format** | `dotnet format ... --verify-no-changes` | Exit code 0 |
| **Build** | `dotnet build LocationManagement.slnx` | 0 errors |
| **SAST** | `semgrep --config=p/owasp-top-ten src/` | 0 findings |
| **SCA (Backend)** | `dotnet list package --vulnerable` | No high/critical |
| **SCA (Frontend)** | `npm audit` | No high/critical |
| **Secrets** | `dotnet husky run --group pre-commit` | 0 detected |
| **Mutation** | `dotnet stryker` | ≥ 80% score |

---

## Workflow

### Starting a New Section

1. **Pre-task hook runs** → Validates existing tests pass
2. **Begin implementation** → Write code, tests, documentation
3. **Build hook runs** → After each code write, verify compilation
4. **Complete section** → Mark all tasks complete

### Completing a Section

1. **Post-task hook triggers** → Asks for completion checklist verification
2. **Run manual checks** → Execute all quality gate commands
3. **Report results** → Confirm all gates pass
4. **Mark section complete** → Update task checkboxes
5. **Move to next section** → Begin new work

---

## Continuous Integration

In CI/CD pipelines, all hooks should run automatically:

```yaml
# Example CI stage
test-and-validate:
  script:
    - dotnet test src/Api.Tests/LocationManagement.Api.Tests.csproj
    - dotnet format LocationManagement.slnx --verify-no-changes
    - dotnet build LocationManagement.slnx
    - semgrep --config=p/owasp-top-ten src/
    - dotnet list package --vulnerable --include-transitive
    - npm audit --prefix src/client
    - dotnet husky run --group pre-commit
    - dotnet stryker --config-file src/Api.Tests/stryker-config.json
```

---

## Troubleshooting

### Tests Fail After Code Changes
1. Check the test output for specific failures
2. Review the code changes that triggered the failure
3. Fix the code or update tests as needed
4. Re-run tests to verify the fix

### Build Fails
1. Check the compiler error messages
2. Verify all required dependencies are installed
3. Run `dotnet restore` if needed
4. Fix the compilation error and rebuild

### SAST Scan Finds Issues
1. Review the semgrep findings
2. Determine if the issue is a real security concern or a false positive
3. Fix the issue or document the false positive
4. Re-run the scan to verify

### SCA Scan Finds Vulnerabilities
1. Identify the vulnerable package
2. Check if an updated version is available
3. Update the package to a patched version
4. Re-run the scan to verify

### Secrets Detected
1. Identify what was detected
2. Remove the secret from the code
3. If accidentally committed, rotate the secret immediately
4. Re-run the scan to verify

---

## Best Practices

1. **Run hooks locally before pushing** — Don't rely solely on CI to catch issues
2. **Fix failures immediately** — Don't accumulate technical debt
3. **Document exceptions** — If a gate must be skipped, document why
4. **Review hook output** — Don't ignore warnings or informational messages
5. **Keep dependencies updated** — Run SCA scans regularly, not just at section completion
6. **Commit frequently** — Small, focused commits are easier to debug than large batches

