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

## Pre-commit Hooks

All commits MUST pass the hook suite in `.pre-commit-config.yaml`. Hooks enforce:

| Hook | Checks |
|---|---|
| `gitleaks`, `detect-secrets`, `detect-private-key` | No secrets or credentials |
| `dotnet-format` | C# formatting (`dotnet format --verify-no-changes`) |
| `dotnet-build` | Builds with zero warnings-as-errors |
| `dotnet-test` | All backend unit tests pass |
| `eslint` | Frontend ESLint rules |
| `tsc` | TypeScript compiles with no type errors |
| `vitest` | All frontend unit tests pass |

```bash
pip install pre-commit   # once per machine
pre-commit install        # once per clone
pre-commit run --all-files
```

- NEVER use `--no-verify` without explicit user approval.
- All generated code MUST pass all hooks before being presented as complete.
