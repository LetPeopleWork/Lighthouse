# Slice 09 — A unit narrows its level's types

> **Amended 2026-11-06.** Item types moved to the connection's mapping. The last section of `../design/upstream-changes.md` wins over this file; apply it at DISTILL.

Epic #6474 · **visible** · `job_id: job-config-admin-say-item-types-once` (proposed) · ≤ 1 day · depends on: 08

## Elevator Pitch

- **Before**: creating Team "Search", Tom types or loads "Story, Bug" again, and may mistype "Storey".
- **After**: he ticks from the Team level's list; Load from board ticks what it finds and names any type the
  level does not have, pointing him to a System Admin.
- **Decision enabled**: Tom picks what his Team reads in seconds, and can only pick types the organisation counts.

## Problem

Tom Becker, Team Admin, sets up a new Team every few weeks. The Work Item Types field is free text with board
import, so every Team's list is retyped, and nothing stops a type nobody else counts.

## Learning hypothesis

Checkboxes from the level replace typing without slowing board-based set-up. **Disproved if** board-created
Teams regularly stall on "ask a System Admin" (then the levels need better seeding from boards, a later
question, not a button in the unit form: maintainer decision M4).

## IN

- Team and Portfolio settings and the create wizards: the types field becomes the level's list as checkboxes
  (sketch in `feature-delta.md`); a unit may untick, never add (D12).
- **No "Add to the level" in unit forms, for anyone** (M4). Load from board ticks the board's types the level
  lists and names the others: "Sub-task is not on the Team level. A System Admin can add it in Organisation."
- Linear units: "Item types are not used by Linear" instead of the list (M11).
- Loading and failure looks as sketched.

## OUT

- Adding types to a level (slice 08, Organisation only). The one unit page (#6476). Org-default control (U2).

## Domain examples

1. Tom creates "Search" from a Jira board with Story, Bug, Sub-task: Story and Bug ticked; Sub-task named with
   the pointer to a System Admin.
2. Priya, System Admin, creates the same Team: same form, no add button; she adds Sub-task in Organisation, then
   ticks it on "Search".
3. Portfolio "Checkout Revamp" unticks Change: it reads Epic only from its next refresh.

## UAT scenarios

### Scenario: A new Team picks its types from the level

Given level 1 lists Story, Bug, Task and User Story
When Tom creates Team "Search" and ticks Story and Bug
Then "Search" reads Story and Bug

### Scenario: Board import names a type the level lacks and says who can add it

Given the board of "Search" uses Sub-task
When Tom loads from the board
Then Story and Bug are ticked
And he is told Sub-task is not on the Team level and that a System Admin can add it in Organisation

### Scenario: Even a System Admin adds types only in Organisation

Given Priya is a System Admin
When she edits Team "Search"
Then she can tick only the Team level's types and finds no way to add one there

### Scenario: The types stay safe while the level's list cannot load

Given the level's types cannot be loaded
When Tom opens "Payments Crew" settings
Then he sees its ticked types read-only and can still save other settings

## Acceptance criteria

- [ ] Units tick only from their level; no free-text types and no add-to-level control in unit forms.
- [ ] Load from board ticks known types and names unknown ones with the System Admin pointer.
- [ ] Linear wording; loading and failure looks.

## Outcome KPI

OUT-2.

## Technical notes

- Types match ignoring case, as the migration's union does (M6); the subset ⊆ level rule is enforced on the
  server, where an unknown type is a 400 for everyone (M1, slice 04).
- Docs at finalisation: `docs/teams/edit.md`, `docs/portfolios/edit.md`, connector pages' "Work Item Types".
