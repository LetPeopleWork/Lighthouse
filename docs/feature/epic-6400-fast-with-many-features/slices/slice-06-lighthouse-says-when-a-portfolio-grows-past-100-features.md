# Slice 06: Lighthouse says when a Portfolio grows past 100 Features (US-06)

**Goal:** whoever edits a Portfolio holding more than 100 Features reads, above its query, what that size costs,
without being blocked; each update of such a Portfolio writes one Warning log line with the count.

**Job:** `job-reach-and-read-what-i-came-for-on-a-large-instance` (outcome OS6). **Moves:** OS6, a nudge (V7: it
does not replace the speed work).

## IN scope

- Threshold: strictly more than 100 stored Features; one constant for note and log (D23).
- Non-blocking info note on the Portfolio edit page, above the query. Copy (maintainer decision 8, 2026-10-24, the
  DESIGN DDD-19 draft): "This {Portfolio} holds {N} {Features} (as of its last update). Above 100, its pages and
  forecasts get slower. If you don't plan with all of them, narrow the query."
- One log line at **Warning** at the end of each successful Features update of such a Portfolio (decision 10): "Portfolio
  '{name}' ({id}) holds {N} Features after its update, more than 100. Its pages and forecasts get slower; narrowing its
  query keeps them fast." It also lands in Recent Problems, accepted (decision 14).
- When the count could not be loaded, the same info note reads "Couldn't check this {Portfolio}'s size." (decision 9;
  *supersedes* "No note while the count … could not be read"). No note while the count loads, at or under 100, or when
  the Portfolio was never updated.

## OUT of scope

- A Team nudge; a note on the Features tab (Q3 default); blocking Save or an update; a configurable threshold.
- Counting during validation (`POST /portfolios/validate` answers a boolean only, S11).

## Learning hypothesis

- **Disproves** "a note at 100 leads some admins to narrow their query" (A10) if, months after release, the share of
  Portfolio tab openings in the largest size bucket (D24) does not fall. Not testable inside the Epic; stated so it
  is read later.
- **Confirms** the copy is understood if the maintainer, shown the note cold, can say what to do about it.

## Acceptance criteria

US-06 AC-6.1 to AC-6.6 in `feature-delta.md`.

## Production data (D25)

Dev DB backup (largest Portfolio 86): no note, no log line anywhere — the negative case on real history. The positive
case uses the scaled set (Northwind Platform 920, Northwind Mobile exactly 100).

## Looks and sketch

- Changes a fetch: **yes** — DESIGN gives the count its own read (`GET /portfolios/{id}/size`, ADR-262). Loading: no
  note. Failure: "Couldn't check this {Portfolio}'s size." in the same info note (maintainer decision 9; this replaces
  the earlier "no note on failure", because a silent failure reads as "under 100").
- UI sketch at DISTILL: **yes** — the note's place and copy on the edit page. *Done*: decisions 8–10 (2026-10-24).

## Dependencies

Last (maintainer). Independent of 02–05 in code.

## Effort

About 1 day. Reference class: Story 6348's always-visible line under a setting.

## Dogfood

Scaled instance: open Northwind Platform's and Northwind Mobile's edit pages; run an update and read the log.
