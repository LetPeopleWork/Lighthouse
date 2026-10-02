# Slice 01 — Name the refinement states; the Refinement tab switches on

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E1 Refinement tab (new) · **Story**: US-01 ·
**Estimate**: ~1d (includes a migration) · **Tier**: Community · **Walking skeleton**: yes (1 of 2)

## Goal

A Team admin names which of the Team's mapped states mean refinement. Until at least one is named, the Refinement tab
is visible but disabled, with a tooltip that matches the reader's role.

## IN

- New **Refinement** section in Team → Settings (Team admins only, DD-14). It offers only To Do and Doing mapped states,
  each labelled with its category (C8). A Doing state gets the note "already counts in {WIP} and {cycle time}".
- An additive, persisted Team setting, created with `CreateMigration` (expand-only). DESIGN picks a shape that slice 03
  can extend with a stage per state without a destructive change.
- A Refinement tab between Metrics and Settings (DD-15). Disabled with a tooltip until a state is named, reusing the
  Features-tab pattern (`TeamDetail.tsx:491-509`). The tooltip text depends on the role: editors are pointed to
  Settings, readers are told a Team admin has to act.
- A warning in the section when a named state is no longer mapped (AC-1.4).
- Server-side guard: only Team admins can save. The UI derives the gating from `useRbac()`.

## OUT

The list (02), stages (03), cadence (04), Terminology key (02; until then the tab label is the seeded default via the
same key path — DESIGN may move the key into this slice if cheaper).

## Learning hypothesis

**This disproves "Teams can name their refinement states from their mapped states"** if, on the dev instance
(`:5169`, real history) or the demo Teams, the states that mean refinement turn out to be unmapped or shared with
delivery work. In that case rules (slice 08) are needed from day one, and 08 moves up to follow 02.

## Data and dogfood moment

- Demo Team Gravity maps `Backlog` (To Do) and `Analysing` / `Next` (Doing). Seed those three as refinement states.
  Leave one demo Team unconfigured, for the disabled-tab E2E.
- Dogfood, same day: on the dev instance, name the refinement states for its busiest Team. Write down whether any
  state was missing or ambiguous, and how long it took.

## Acceptance criteria

AC-1.1 … AC-1.6 in `feature-delta.md` (US-01).

## Dependencies

None.
