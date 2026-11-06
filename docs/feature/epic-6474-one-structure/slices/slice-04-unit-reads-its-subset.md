# Slice 04 — A unit reads its level's types, narrowed to its subset

> **Amended 2026-11-06.** Item types moved to the connection's mapping. The last section of `../design/upstream-changes.md` wins over this file; apply it at DISTILL.

Epic #6474 · invisible on screen, **breaking for API clients** (M1) · `job_id: infrastructure-only` · ≤ 1 day · depends on: 03

`infrastructure_rationale`: D8/D12 make item types the level's; a unit only narrows them. This slice switches
the fetch to that rule, with the migration ticking exactly today's list, before any screen shows it. Prepares
`job-config-admin-say-item-types-once`.

## Problem

Priya Raman keeps 12 Teams' type lists in step by hand. Each Team holds its own free-text list of types. Once types belong to the level, a unit's list must be a
subset of it, or the level does not own anything.

## Learning hypothesis

With each unit's subset equal to its old list, the fetch reads exactly the same records. **Disproved if** any
unit's fetch fingerprint or pinned numbers change (then the subset is not equal and the migration is wrong).

## IN

- Each unit's subset = its pre-upgrade `WorkItemTypes`, matched case-insensitively to its level's list; an
  empty list stays empty (Linear, ServiceNow by choice).
- The fetch reads the unit's subset; `/teams` and `/portfolios` keep returning it as `workItemTypes`.
- The invariant "subset ⊆ level's types" holds on every write.
- A `/teams` or `/portfolios` write carrying a type the level does not list is **refused with 400 naming the
  type and the level, for everyone, System Admins included** (M1). This is a deliberate **breaking API change**
  for the Lighthouse-Clients CLI/MCP: major version bump and breaking-change note (slice 14; confirm at DESIGN).
- State mapping stays the unit's own, with no organisation default (D13).

## OUT

- Any screen (08, 09). Ticking new level types for existing units (never, M2; slice 08).

## Domain examples

1. "Payments Crew" read {Story, Bug}; after upgrade it ticks Story and Bug of level 1's {Story, Bug, Task,
   User Story}.
2. Linear Team "Mobile Squad" read nothing; it ticks nothing and still reads its Linear team.
3. A CLI script PUTs Team "Search" with types {Story, Spike}; "Spike" is not on level 1: refused, "Spike is not
   on the Team level", nothing saved (M1).

## UAT scenarios

### Scenario: Each unit reads exactly its old types

Given "Payments Crew" read Story and Bug before the upgrade
When the install is upgraded and refreshes
Then "Payments Crew" ticks Story and Bug
And its pinned numbers are unchanged

### Scenario: A unit that read no types still reads its items

Given Linear Team "Mobile Squad" with no types
When the install is upgraded and refreshes
Then "Mobile Squad" ticks no types and reads the same 14 finished items in 90 days

### Scenario: An API client's settings round-trip unchanged

Given the CLI reads Team "Payments Crew"
When it writes the same settings back
Then the Team's types and numbers are unchanged

### Scenario: A type the level does not list is refused, even for a System Admin

Given level 1 does not list "Spike"
When Priya, a System Admin, saves Team "Search" with Story and Spike through the API
Then the save is refused with "Spike is not on the Team level"
And "Search" and level 1 are unchanged

## Acceptance criteria

- [ ] Each unit's subset equals its old list after upgrade; fetch fingerprints unchanged.
- [ ] Empty subsets are legal and read as today.
- [ ] `/teams` and `/portfolios` payloads round-trip unchanged.
- [ ] An unknown type is refused for everyone, naming the type and the level; nothing is saved (M1).
- [ ] Each unit keeps its own state mapping; no level or organisation default is introduced (D13).
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1.

## Technical notes

- `SettingsOwnerDtoRoundTripTest` and pinned payload-shape tests (ci-learnings "settings field through the
  whole path").
- Unchanged fingerprints also avoid one full refetch per unit after the upgrade.
