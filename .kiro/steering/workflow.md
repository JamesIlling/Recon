---
inclusion: always
---

# Workflow Rules

## Spec Review Gates

1. NEVER assume user preferences or requirements — always ask explicitly.
2. Present each spec document (requirements, design, tasks) for user review before proceeding to the next.
3. Do NOT proceed until the user provides a clear "yes" or "approved" response.

**Order**: `requirements.md` → `design.md` → `tasks.md`

## Clarification Before Action

- Act as an expert software architect and security analyst on every request.
- Assume the user has NOT provided all information needed — always check before acting.
- Before generating any code or files, evaluate confidence (0–10). If below 8, stop and ask 3–5 clarifying questions as a bulleted list.
- Do not proceed until the user has answered or context is fully defined.
- Do not infer intent when the user's preference could reasonably go either way.

## No Fabrication

- If the exact configuration, API, or best practice is not known with certainty, ask rather than guess.
- Do not fabricate configuration settings, library APIs, or version-specific behaviour.
- If something needs verification, say so explicitly before using it in generated code.

## Package Validation

Before suggesting, adding, or referencing ANY library, package, or tool, you MUST verify it exists in its package manager registry and confirm the version is real and current.

| Ecosystem | Registry to check | How to verify |
|---|---|---|
| .NET / NuGet | https://www.nuget.org/packages/{id} | Check package page exists and version is listed |
| npm / Node | https://www.npmjs.com/package/{id} | Check package page exists and version is listed |
| .NET tools | https://www.nuget.org/packages/{id} | Same as NuGet — dotnet tools are NuGet packages |
| Docker images | https://hub.docker.com or ghcr.io | Confirm image and tag exist |

### Rules

- NEVER suggest a package without first confirming it exists in its registry.
- NEVER hardcode a version number without confirming that exact version exists.
- ALWAYS use the latest stable (non-preview) version unless the project explicitly requires otherwise.
- WHEN adding a package to `.config/dotnet-tools.json`, `package.json`, or any config file, verify the version against the registry before writing it.
- IF a package cannot be found or the version does not exist, say so explicitly and ask the user how to proceed.
- DO NOT assume a package name is correct based on similarity to another — verify the exact identifier.

## Pre-commit Hooks (Husky.Net)

All commits MUST pass the Husky.Net hook suite defined in `.husky/task-runner.json`. Hooks are managed via the .NET tool manifest (`.config/dotnet-tools.json`) — no Python required.

| Task | Checks |
|---|---|
| `gitleaks-secrets-scan` | No secrets or credentials in staged files |
| `dotnet-format` | C# formatting (`dotnet format --verify-no-changes`) |
| `dotnet-build` | Backend builds with zero warnings-as-errors |
| `dotnet-test` | All backend unit tests pass |
| `eslint` | Frontend ESLint rules |
| `tsc` | TypeScript compiles with no type errors |
| `vitest` | All frontend unit tests pass |

```powershell
dotnet tool restore                    # installs Husky.Net (once per clone)
dotnet husky install                   # registers git hooks (once per clone)
dotnet husky run --group pre-commit    # run all hooks manually
```

- NEVER use `git commit --no-verify` without explicit user approval.
- All generated code MUST pass all hooks before being presented as complete.
