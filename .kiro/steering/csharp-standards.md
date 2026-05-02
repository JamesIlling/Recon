---
inclusion: always
---

# C# Coding Standards

Based on [Microsoft official C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes, structs, records, delegates | PascalCase | DataService |
| Interfaces | PascalCase prefixed with I | IWorkerQueue |
| Public methods, properties, events, fields | PascalCase | StartProcessing() |
| Local variables, method parameters | camelCase | workerQueue |
| Private / internal instance fields | _camelCase | _workerQueue |
| Private / internal static fields | s_camelCase | s_instance |
| Thread-static fields | t_camelCase | t_timeSpan |
| Constants | PascalCase | MaxRetryCount |
| Generic type parameters | T + PascalCase | TSession |
| Enum types | Singular (non-flags), plural (flags) | Status, Permissions |
| Attribute types | PascalCase + Attribute suffix | RequiredAttribute |

- Prefer clarity over brevity. Avoid abbreviations unless widely accepted (Id, Http, Url).
- Avoid single-letter names except simple loop counters (i, j).
- No two consecutive underscores.

## Layout and Formatting

- 4 spaces indentation, no tabs.
- Allman braces: opening and closing brace each on their own line.
- One statement per line. One declaration per line.
- Blank line between method and property definitions.
- using directives outside namespace declarations.
- File-scoped namespaces for single-namespace files.

## Code Safety

- Prefer managed C# code over unsafe or native code.
- Do not use top-level statements.
- Avoid primary constructors unless required.
- Avoid extension members.
- Do not use union types.

## Language Features

- Use var only when the type is obvious from the right-hand side (new, explicit cast, literal).
- Use var in for loops; use explicit types in foreach loops.
- Use collection expressions to initialise collections.
- Use required properties to enforce initialisation where appropriate.
- Use Func<> and Action<> instead of custom delegate types.
- Use async/await for all I/O-bound operations. Use ConfigureAwait(false) in library code.
- Use LINQ for collection manipulation.
- Use string interpolation for short strings; StringBuilder in loops.
- Prefer raw string literals over escape sequences for multi-line strings.
- Call static members via the class name, not a derived class or instance.

## Exception Handling

- Catch specific exception types only, do not catch System.Exception without a filter.
- Use using declarations for IDisposable resources, not try/finally.
- Never swallow exceptions silently, always log at Error level or rethrow.

## Comments and Documentation

- XML doc comments (///) on all public types and members.
- Single-line comments (//) for brief inline explanations, on their own line, uppercase, ending with a period.
- No block comments (/* */).

## Code Analysis

- Enable Roslyn analyzers in all projects.
- Use .editorconfig at the repository root to enforce style automatically.
- Treat warnings as errors in CI builds.
- Use SecurityCodeScan for security-specific rules.
- All generated code MUST pass analysis with zero errors and zero unjustified suppressions.
