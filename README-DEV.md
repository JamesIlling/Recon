# Developer Setup Guide

This guide covers everything you need to get the project running locally, including all tools, security scanning, testing, observability, and the AI assistant (Kiro) setup.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Clone and Initial Setup](#2-clone-and-initial-setup)
3. [Git Hooks — Husky.Net](#3-git-hooks--huskynet)
4. [Backend — .NET 10 API](#4-backend--net-10-api)
5. [Frontend — React + TypeScript](#5-frontend--react--typescript)
6. [Database — SQL Server with GIS](#6-database--sql-server-with-gis)
7. [Local Orchestration — .NET Aspire](#7-local-orchestration--net-aspire)
8. [Testing](#8-testing)
9. [Security Scanning — SAST and SCA](#9-security-scanning--sast-and-sca)
10. [OWASP ZAP — Pre-deployment Pen Testing](#10-owasp-zap--pre-deployment-pen-testing)
11. [Kiro AI Assistant — MCP Servers](#11-kiro-ai-assistant--mcp-servers)
12. [Decision Records — RFCs and ADRs](#12-decision-records--rfcs-and-adrs)
13. [Pre-deployment Checklist](#13-pre-deployment-checklist)

---

## 1. Prerequisites

Install the following before cloning the repository.

### Required

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 20 LTS+ | https://nodejs.org |
| Docker Desktop | 20.10+ | https://www.docker.com/products/docker-desktop |
| Git | Any | https://git-scm.com |

### Verify installs

```powershell
dotnet --version        # should be 10.x
node --version          # should be 20.x or higher
docker --version        # should be 20.10 or higher
git --version
```

### Optional but recommended

| Tool | Purpose | Install |
|---|---|---|
| Semgrep | SAST scanning | https://semgrep.dev/docs/getting-started (standalone binary) |
| Gitleaks | Secret scanning CLI | https://github.com/gitleaks/gitleaks/releases |
| OWASP Dependency-Check | Deep SCA | https://owasp.org/www-project-dependency-check |
| openssl | Generating API keys | Included in Git Bash / WSL |

> No Python required. All hook tooling runs via .NET and Node.js.

---

## 2. Clone and Initial Setup

```powershell
git clone <repository-url>
cd <repository-name>
```

After cloning, restore all .NET tools (including Husky.Net) and install git hooks:

```powershell
dotnet tool restore
dotnet husky install
```

That is all the setup required for hooks — no Python, no pip.

---

## 3. Git Hooks — Husky.Net

Pre-commit hooks are managed by [Husky.Net](https://alirezanet.github.io/Husky.Net/) — a .NET global tool that requires no Python or npm for the hook runner itself.

Hooks are defined in `.husky/task-runner.json` and registered via `.husky/pre-commit`.

### What the hooks check

| Task | What it enforces |
|---|---|
| `gitleaks-secrets-scan` | No secrets or credentials in staged files |
| `dotnet-format` | C# code formatting (Allman braces, 4-space indent) |
| `dotnet-build` | Backend builds with zero warnings-as-errors |
| `dotnet-test` | All backend unit tests pass |
| `eslint` | Frontend ESLint rules pass |
| `tsc` | TypeScript compiles with no type errors |
| `vitest` | All frontend unit tests pass |

### Run hooks manually

```powershell
dotnet husky run --group pre-commit
```

### Test a single task

```powershell
dotnet husky run --name dotnet-format
dotnet husky run --name gitleaks-secrets-scan
```

> Never use `git commit --no-verify` without explicit approval. If a hook fails, fix the issue before committing.

---

## 4. Backend — .NET 10 API

### Restore tools and dependencies

```powershell
dotnet tool restore    # installs Husky.Net, Stryker.NET, EF Core tools
dotnet restore
dotnet build
```

### Run the API directly

```powershell
dotnet run --project src/Api
```

API will be available at `https://localhost:5001` by default.

### EF Core migrations

```powershell
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/Api

# Apply migrations to the database
dotnet ef database update --project src/Api
```

---

## 5. Frontend — React + TypeScript

```powershell
cd src/client

# Install dependencies
npm install

# Start development server (Vite)
npm run dev
```

Frontend will be available at `http://localhost:5173` by default.

```powershell
# Build for production
npm run build

# Run unit tests (single pass)
npm run test -- --run
```

---

## 6. Database — SQL Server with GIS

The project uses SQL Server with spatial extensions (`GEOGRAPHY` / `GEOMETRY` types).

### Option A — Docker (recommended for local dev)

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" `
  -p 1433:1433 --name sqlserver `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Option B — SQL Server Developer Edition

Download from https://www.microsoft.com/en-us/sql-server/sql-server-downloads

### Apply migrations

Once SQL Server is running:

```powershell
dotnet ef database update --project src/Api
```

---

## 7. Local Orchestration — .NET Aspire

.NET Aspire orchestrates all services (API, frontend, database) for local development and provides a built-in observability dashboard.

### Install Aspire workload (once per machine)

```powershell
dotnet workload install aspire
```

### Run everything via Aspire

```powershell
dotnet run --project src/AppHost
```

This starts:
- The ASP.NET Core API
- The React frontend (via npm)
- SQL Server (via Docker)
- The Aspire Dashboard at `http://localhost:15888`

### Aspire Dashboard

Open `http://localhost:15888` to view:
- **Structured logs** — all services, filterable by level and service
- **Distributed traces** — end-to-end request traces across services
- **Metrics** — HTTP request rates, durations, EF Core query times, GC metrics

No additional observability tooling is needed for local development.

---

## 8. Testing

### Unit tests (backend)

```powershell
dotnet test
```

### Unit tests (frontend)

```powershell
cd src/client
npm run test -- --run
```

### Mutation testing — Stryker.NET

Mutation testing verifies that your tests actually catch bugs. Run on demand, not on every build.

```powershell
# Run from the test project directory
cd src/Api.Tests
dotnet stryker

# Run with config file
dotnet stryker --config-file stryker-config.json
```

Minimum acceptable mutation score: **80%**. Results are in `StrykerOutput/reports/`.

### E2E tests — Playwright

```powershell
# Install Playwright browsers (first time)
npx playwright install

# Run all E2E tests
npx playwright test

# Run in headed mode (visible browser)
npx playwright test --headed

# Run a specific test file
npx playwright test tests/e2e/login.spec.ts

# Open Playwright UI mode
npx playwright test --ui

# View last test report
npx playwright show-report
```

E2E tests run against a real running instance. Start the app via Aspire before running Playwright.

---

## 9. Security Scanning — SAST and SCA

Run these before every pull request and before deployment.

### SCA — Check for vulnerable dependencies

```powershell
# NuGet (backend)
dotnet list package --vulnerable --include-transitive

# npm (frontend)
cd src/client
npm audit --audit-level=high

# Deep transitive scan (requires OWASP Dependency-Check installed)
dependency-check --project "MyProject" --scan ./src --format HTML --out docs/security/
```

### SAST — Static code analysis

Semgrep is available as a standalone binary — no Python required.
Download from https://semgrep.dev/docs/getting-started or install via winget:

```powershell
winget install Semgrep.Semgrep

# Run with OWASP Top 10 ruleset
semgrep --config=p/owasp-top-ten src/

# Run with default ruleset
semgrep --config=auto src/
```

Zero findings required before merging or deploying.

### Secrets scanning

```powershell
# Scan recent commits (requires Gitleaks installed)
gitleaks detect --source . --log-opts="HEAD~1..HEAD"

# Run all Husky.Net hooks (includes secrets scan)
dotnet husky run --group pre-commit
```

Install Gitleaks from https://github.com/gitleaks/gitleaks/releases (single binary, no Python).

---

## 10. OWASP ZAP — Pre-deployment Pen Testing

OWASP ZAP performs dynamic application security testing (DAST) against a running instance. Run against staging only — never production.

### Setup (first time)

1. Generate API keys (Git Bash or WSL):

```bash
openssl rand -hex 32   # copy output -> MCP_API_KEY
openssl rand -hex 32   # copy output -> ZAP_API_KEY
```

2. Configure the environment:

```powershell
cd tools/zap
Copy-Item .env.example .env
# Edit .env and replace REPLACE_WITH_GENERATED_KEY with your generated keys
```

3. Start the ZAP stack:

```powershell
docker compose up -d
```

4. Set the Kiro environment variable (PowerShell):

```powershell
# Current session only
$env:MCP_ZAP_API_KEY = "your-mcp-api-key-from-.env"

# Permanent
[System.Environment]::SetEnvironmentVariable("MCP_ZAP_API_KEY", "your-key", "User")
```

5. Enable the ZAP MCP server in Kiro:
   - Open `~/.kiro/settings/mcp.json`
   - Set `"disabled": false` on the `owasp-zap` entry
   - Reconnect MCP servers via the Kiro MCP Server panel

### Run a pen test

In Kiro, trigger the **Pre-Deployment Pen Test** hook (Agent Hooks panel), or ask:

> "Run a pre-deployment pen test against https://staging.myapp.com"

Kiro will run reconnaissance, DAST scan, vulnerability scanning, and generate a report saved to `docs/security/pentest-YYYY-MM-DD.md`.

### Stop ZAP

```powershell
cd tools/zap
docker compose down
```

---

## 11. Kiro AI Assistant — MCP Servers

Kiro uses MCP (Model Context Protocol) servers to access external tools and documentation. Configured in `~/.kiro/settings/mcp.json`.

### Microsoft Learn (always active)

Provides real-time access to official Microsoft and Azure documentation.

- **Status**: Enabled by default — no setup required
- **Usage**: Kiro automatically queries this when answering .NET, Azure, or C# questions

### OWASP ZAP (requires Docker)

Provides DAST security scanning tools.

- **Status**: Disabled by default — enable when Docker is running (see Section 10)
- **Requires**: `MCP_ZAP_API_KEY` environment variable matching `MCP_API_KEY` in `tools/zap/.env`

### fetch (disabled)

General web fetch tool — disabled by default.

### Reconnecting MCP servers

After changing `mcp.json` or restarting Docker:
1. Open the Kiro feature panel
2. Navigate to **MCP Servers**
3. Click **Reconnect** on any server showing as disconnected

---

## 12. Decision Records — RFCs and ADRs

All significant technical decisions are tracked in `docs/`.

### Write an RFC (before starting work)

Use the template at `docs/rfcs/_template.md`. Name files `XXXX-short-description.md`.

RFCs are for: new libraries, breaking API changes, new architectural patterns, anything needing team alignment.

### Write an ADR (to record a decision)

Use the template at `docs/adrs/_template.md`. Name files `XXXX-short-description.md`.

ADRs are immutable — never edit an accepted ADR. Create a new one that supersedes it.

### Existing records

| # | Type | Title |
|---|---|---|
| 0001 | RFC | Initial Technology Stack |
| 0001 | ADR | Use NetTopologySuite for GIS |
| 0002 | ADR | Use Stryker.NET for Mutation Testing |

---

## 13. Pre-deployment Checklist

Run all of the following before deploying to production. All must pass.

```
[ ] dotnet test                                          — unit tests pass
[ ] dotnet stryker                                       — mutation score >= 80%
[ ] npx playwright test                                  — E2E tests pass
[ ] semgrep --config=p/owasp-top-ten src/               — zero SAST findings
[ ] dotnet list package --vulnerable --include-transitive — zero high/critical NuGet CVEs
[ ] npm audit --audit-level=high                         — zero high/critical npm CVEs
[ ] dependency-check --scan ./src                        — OWASP deep scan report saved
[ ] gitleaks detect --source .                           — zero secrets in history
[ ] dotnet husky run --group pre-commit                  — all hooks pass
[ ] ZAP pen test against staging                         — report saved to docs/security/
[ ] No Critical findings in pen test report
[ ] No unresolved High findings (or ADR with accepted risk)
[ ] Pen test report referenced in deployment PR
```

---

## Quick Reference

| Task | Command |
|---|---|
| First-time setup | `dotnet tool restore && dotnet husky install` |
| Start everything (Aspire) | `dotnet run --project src/AppHost` |
| Run backend tests | `dotnet test` |
| Run frontend tests | `cd src/client && npm run test -- --run` |
| Run mutation tests | `cd src/Api.Tests && dotnet stryker` |
| Run E2E tests | `npx playwright test` |
| Run all git hooks | `dotnet husky run --group pre-commit` |
| Run SAST | `semgrep --config=p/owasp-top-ten src/` |
| Run SCA (backend) | `dotnet list package --vulnerable --include-transitive` |
| Run SCA (frontend) | `cd src/client && npm audit` |
| Start ZAP | `cd tools/zap && docker compose up -d` |
| Stop ZAP | `cd tools/zap && docker compose down` |
| Add DB migration | `dotnet ef migrations add <Name> --project src/Api` |
| Apply DB migrations | `dotnet ef database update --project src/Api` |
