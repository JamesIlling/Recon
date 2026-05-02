---
inclusion: always
---

# Decision Records

## RFCs (Request for Comments)

**Location**: `docs/rfcs/` | **Naming**: `XXXX-short-description.md` | **Template**: `docs/rfcs/_template.md`

Write an RFC WHEN: introducing a new library or major dependency, changing the API contract, establishing a new architectural pattern, or any change requiring team alignment before work starts.

**Statuses**: Draft → Under Review → Accepted / Rejected / Superseded

## ADRs (Architecture Decision Records)

**Location**: `docs/adrs/` | **Naming**: `XXXX-short-description.md` | **Template**: `docs/adrs/_template.md`

Write an ADR WHEN: choosing a library or tool over alternatives, deciding on a data storage strategy, establishing a coding convention, or any decision a future developer would ask "why did we do it this way?"

**Statuses**: Proposed → Accepted / Deprecated / Superseded

## Rules

- WHEN proposing a significant architectural or library change, suggest creating an RFC or ADR.
- WHEN implementing a change preceded by an RFC, reference the RFC number in the ADR.
- NEVER modify an accepted ADR — create a new one superseding it instead.
- WHEN adding a new dependency or changing a core pattern, check `docs/adrs/` first.
