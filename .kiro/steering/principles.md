---
inclusion: always
---

# Engineering Principles

## Common Pitfalls — Mistakes to Prevent

- Don't over-engineer: match complexity to the problem.
- Don't add error handling or validation that wasn't asked for.
- Don't use `Any` or `any` types. Define types explicitly.
- When modifying existing code, match the existing style.

## Code Clarity Rules

- Functions should be ≤ 25 lines. If longer, decompose.
- Maximum 3 parameters per function. Use objects/dataclasses for more.
- No magic numbers or strings. Use constants with descriptive names.
- Every public function must have a docstring.
