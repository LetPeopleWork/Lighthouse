# Slice 04: The Features list opens fast and says when it cannot load (US-04)

**Goal:** a Portfolio's or Team's Features tab shows its first row in under 2 s at 920 Features on 4 cores, idle and
during a round, from a lean list keyed by the Portfolio or Team; a failed, empty or all-hidden list says so instead of
loading forever.

**Job:** `job-reach-and-read-what-i-came-for-on-a-large-instance`. **Moves:** **K2**.

## IN scope

- A new, additive list endpoint per Portfolio and per Team: only the shown columns, parents folded in, in-progress as
  ids (shape is DESIGN's). Guarded by Portfolio / Team read as today.
- Both Features tabs read it (D21); hide-completed stays client-side (maintainer).
- Looks (D22): loading placeholders until the first answer; after a completed update the rows stay, dimmed under a
  small spinner, and an older answer never lands (keyed query); failure "The {Features} couldn't be loaded." with
  Retry; empty "This {Portfolio} has no {Features} yet." (or {Team}); all hidden "All {N} {Features} are completed.
  Turn off Hide completed to see them."
- The E2E page object waits for the list's ready state.

## OUT of scope

- Removing or changing `GET /features`, `features/ids`, `FeatureDto` (D20; the clients stay on them).
- Paging, server-side sort or filter (later Epic).
- Client caching across visits.

## Learning hypothesis

- **Disproves** "a lean payload brings the first row under 2 s" if K2 is missed at 920 idle after this slice.
- **Confirms** it if K2 holds idle and during a round, on SQLite and PostgreSQL.

## Acceptance criteria

US-04 AC-4.1 to AC-4.7 in `feature-delta.md`.

## Production data (D25)

Dev DB backup: every column of every row identical to today's list on all three Portfolios and Teams.

## Looks and sketch

- Changes a fetch: **yes**. DESIGN pins loading, failure, empty and all-hidden (reuse `WidgetShell` status and a keyed
  TanStack query). Check by hand with the browser throttled.
- UI sketch at DISTILL: **yes** — the four looks and their copy, one decision at a time, reusing the existing
  `FeatureListDataGrid` / `DataGridBase` grid.

## Dependencies

Slice 03 (finished): it consumes the light verdicts and the in-progress ids. If the falsifier fired in 01a, this
slice moves to just before 06 (D18).

## Effort

About 2 days: one endpoint, one hook, four looks of one grid. Reference class: Bug 5571's widget fetch by category
and `WidgetShell` status adoption.

## Dogfood

Scaled instance: open Northwind Platform and Team Borealis with and without a round; stop the backend and press Retry;
create an empty Portfolio; hide completed on a fully completed one.
