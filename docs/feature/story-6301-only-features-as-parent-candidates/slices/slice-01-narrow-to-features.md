# Slice 01 — A Work Item linked to one Feature and to incidents counts toward that Feature

**Feature**: story-6301-only-features-as-parent-candidates · **ADO**: #6301 · **Story**: US-01 · **Estimate**: ~1 day
**Reference class**: #6028 slice 03 (`docs/feature/parent-from-issue-links/slices/slice-03-ambiguity-says-so.md`),
which made the refusal and its count. This slice turns most of those refusals into placements and splits the
count in two.

## Goal

At Team grain, when a Work Item's links of the configured type name two or more issues, only the stored
Features stay candidates. Exactly one left → it is the parent. None left, or several (until slice 02) → not
placed, reported. Refresh History shows placed and not placed as separate figures.

## IN scope

- Candidates travel on the record from the connector (keys only here; link dates arrive in slice 02).
- One shared placing step in `WorkItemService`, called from both Team fetches (full and delta), before
  `SyncWorkItem`; the stored-Feature key set is loaded once per fetch (AC-1.1).
- No additional Jira request per refresh (AC-1.9).
- The result written in both places: `ParentReferenceId` and the override's Additional Field value.
- Lone candidate stored as-is, counted in neither figure (AC-1.3).
- Not placed: today's reading stands (Jira's own `parent` field or nothing), never a preserved old parent
  (AC-1.2, D11).
- Split count on the Team `RefreshLog` row; one expand-only column via `CreateMigration` (AC-1.5).
- Refresh History: two figures, max per window, shown only above zero, the configured Work Items term on
  Team rows and Features term on Portfolio rows (AC-1.6, S-3), plus the loading and failure looks.
- At most one placed line and one not-placed line per fetch, moved out of the connector; no line says "left
  where it was" (AC-1.7, D12, D13).

## OUT of scope

- Any pick between two or more Features (slice 02). They stay not placed here: a refusal, never a guess.
- Portfolio grain (slice 03). A Portfolio's ambiguous Features still refuse and count as "not placed"; its
  Refresh History row shows the new captions with the placed figure hidden at 0.
- Re-resolving after a Portfolio refresh (D6). A late Feature is picked up when the item is next read (AC-1.8).
- Native-parent and override-field sources (D4).

## Learning hypothesis

**Disproves, if it fails**: A2 — that after narrowing to stored Features, almost every ambiguous Work Item has
exactly one Feature left.

**The measurement**: on the first full Team refresh against Steve's customer's data (or a replay of the
warning line from his refresh log, P1), placed / (placed + not placed). Pre-registered: **≥ 80% placed
confirms**; below 50% means most items name several Features, so slice 02 is the real value and moves up
in priority ahead of any polish. Record the number and the denominator here.

**Confirms, if it succeeds**: the customer's symptom is mostly fixed by narrowing alone; slices 02-03 are
refinement.

On our own data the number is directional only: the dogfood instance has 8 link-carrying issues in 4843
(#6028 slice 03, "Measurement result").

## Acceptance criteria

AC-1.1 through AC-1.9 in `feature-delta.md` (US-01).

## UI (approved at the start of DISTILL)

The existing per-entity stat row in `RefreshHistorySection` (caption over value), no new control. The one
figure `Max Work Items Fetched With Links Naming More Than One Parent` becomes two, as the maintainer
approved (copy B): `Max Work Items Placed From Links Naming More Than One Parent` and `Max Work Items Not
Placed: Links Name More Than One Parent`, with the Feature term in place of Work Items on a Portfolio row.

The section also gains a loading look and a failure look, which it lacked: a centred spinner while the
refresh history loads, and `Could not load refresh history.` when the load fails.
`No refresh history available yet.` now shows only for a successful, empty answer. See `feature-delta.md`,
"Wave: DESIGN / [REF] Loading and failure look" and "Wave: DISTILL / [REF] Maintainer decisions: UI sketch
review (2026-10-15)".

Slice 01 removes the old caption for every entity. A Portfolio row therefore shows the new layout from this
slice: its placed figure stays 0 (hidden) until slice 03, and everything ambiguous at Portfolio grain shows
under the not-placed figure.

## Data

- **Dogfood**: `letpeoplework.atlassian.net`, project `LGHTHSDMO`, link type **"Blocks"** (the instance has
  no "caused by" pair). `LGHTHSDMO-7` and `LGHTHSDMO-9` already carry two counterparts each; make one
  counterpart of `LGHTHSDMO-7` a Feature on the demo Portfolio and leave `LGHTHSDMO-9`'s both non-Features.
  Expected: `-7` placed, `-9` not placed and named.
- **Production**: Steve's customer, after upgrade (KPI `OUT-6301-ambiguous-items-placed`).

## Dependencies

#6028 and #6203 shipped. No dependency on Spike 00.

## Pre-slice SPIKE

No. Nothing here reads new tracker data.

## Dogfood moment

Same day: refresh the demo Team pointed at "Blocks", open Settings → System Info → Refresh History → that
Team, and see one placed and one not placed; open the Task Manager and read both lines.

## Docs at finalize

`docs/teams/edit.md:403` (drop "narrow those links down to one"; say what is placed and what is reported),
`docs/settings/systeminfo.md` (the two figures; the timing gap).
