---
inclusion: always
---

# Security Rules

## Analyst Mindset

- Act as an expert security analyst on every request that touches authentication, authorisation, data handling, APIs, or infrastructure.
- Assume the user has NOT provided all the information needed to make the best security decisions — always check before acting.
- Before generating any security-sensitive code or configuration, stop and ask 3–5 clarifying questions using a bulleted list if context is missing.
- Do not proceed until the user has answered, or until the security context is fully defined.

## Required Security Tests

Every feature that exposes an API endpoint, handles user data, or performs authorisation MUST include:

- **Authentication tests** — verify that unauthenticated requests are rejected (401)
- **Authorisation tests** — verify that unauthorised roles/users are denied (403)
- **Input validation tests** — verify that malformed, oversized, or malicious input is rejected
- **Injection tests** — verify that SQL, command, or query injection attempts are handled safely
- **Rate limiting / abuse tests** — verify that endpoints enforce request limits where applicable

## Secure Coding Defaults

- Always use parameterised queries or EF Core LINQ — never concatenate raw SQL with user input.
- Validate and sanitise all input at the API boundary before it reaches business logic or the database.
- Never log sensitive data (passwords, tokens, PII, spatial coordinates that could identify individuals).
- Use HTTPS-only; never transmit credentials or tokens over plain HTTP.
- Store secrets in environment variables or a secrets manager — never hardcode them in source files or commit them to version control.
- Apply the principle of least privilege to all database accounts, service identities, and API roles.

## GIS-Specific Security

- Validate coordinate ranges and geometry complexity before processing spatial input (guard against geometry bombs / oversized polygons).
- Do not expose raw database geometry column values in API responses without sanitisation — return only the fields the client needs.
- Apply authorisation checks before returning location data — spatial data can be sensitive PII.

## Secrets Detection

### Tools

| Tool | Purpose |
|---|---|
| **Gitleaks** | Scans git history and staged files for secrets/credentials |
| **detect-secrets** (Yelp) | Detects high-entropy strings and known secret patterns |
| **pre-commit `detect-private-key`** | Blocks commits containing private keys |

### Pre-commit Hook Setup

Pre-commit hooks are configured in `.pre-commit-config.yaml` at the repository root. Every developer and CI pipeline MUST have these hooks active.

```bash
# Install pre-commit (once per machine)
pip install pre-commit

# Install hooks into the local repo (once per clone)
pre-commit install

# Run all hooks manually against all files
pre-commit run --all-files

# Run gitleaks only
pre-commit run gitleaks --all-files

# Initialise detect-secrets baseline (first time)
detect-secrets scan > .secrets.baseline
```

### Rules

- Pre-commit hooks MUST be installed in every clone of this repository before any commits are made.
- All generated code MUST pass the pre-commit secret scan before being committed — zero detected secrets allowed.
- NEVER hardcode secrets, API keys, connection strings, tokens, or passwords in any source file, config file, or test fixture.
- Secrets MUST be stored in environment variables, `.env` files (git-ignored), or a secrets manager — never in tracked files.
- `.env` files MUST be listed in `.gitignore` — the AI assistant MUST verify this before generating any `.env` file.
- If a secret is accidentally committed, treat it as compromised immediately — rotate it before attempting to remove it from history.
- The `.secrets.baseline` file tracks known false positives — review it on every update and do not use it to suppress real secrets.
- CI pipelines MUST run `gitleaks detect --source . --log-opts="HEAD~1..HEAD"` on every pull request.

## Security Review Triggers

Raise a security question or suggest an ADR WHEN:
- A new authentication or authorisation mechanism is introduced
- A new external API or third-party service is integrated
- User-supplied data is used in a query, file path, or system command
- A new data type containing PII or location data is added to the schema
- CORS policy, CSP headers, or cookie settings are changed

## SAST & SCA Requirements

### Tools in Use

| Tool | Type | Scope |
|---|---|---|
| Roslyn Analyzers + SecurityCodeScan | SAST | C# / .NET backend |
| Semgrep (`p/owasp-top-ten`) | SAST | All source code |
| `dotnet list package --vulnerable` | SCA | NuGet dependencies |
| `npm audit` | SCA | npm dependencies |
| OWASP Dependency-Check | SCA | Deep transitive dependency scan |

### Rules

- All generated C# code MUST pass Roslyn security analyzer warnings with zero errors and zero suppressions (unless explicitly justified).
- SAST (`semgrep --config=p/owasp-top-ten`) MUST be run against any new code before it is considered complete — zero findings required.
- SCA checks (`dotnet list package --vulnerable --include-transitive` and `npm audit`) MUST return no high or critical severity vulnerabilities before a feature is merged.
- If a vulnerability is found in a dependency, it MUST be resolved (upgrade, patch, or replace) before proceeding — do not suppress without explicit user approval.
- SAST and SCA results MUST be reviewed as part of the Definition of Done alongside unit, mutation, and UI tests.
