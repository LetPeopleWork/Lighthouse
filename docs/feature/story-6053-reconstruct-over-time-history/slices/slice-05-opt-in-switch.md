# Slice 05 — Choose whether the over-time charts fill in the past

**Story**: US-05 | **ADO**: 6053 (no item of its own; follow-ups #6083 and #6084 are unparented) | **Estimate**: <= 1 day | **Reference class**: delta sync, an optional feature that shipped off and reads its switch each time it is used

## Goal

A System Admin decides whether this instance fills in past days. The switch is off on fresh and upgraded
instances alike. Switching it on takes effect at the next chart open, with no restart. Switching it off keeps
what was filled and starts nothing new. An empty chart's sentence is true in both positions.

## Why here: after 04-02, before 04-03 and 04-04

- **Before the docs (04-03)**, so `docs/metrics/predictability.md`, the ADR-207 amendment and the release notes
  describe the fill as opt-in from their first sentence instead of being rewritten.
- **Before mutation testing (04-04)**, because the gate is a one-line condition that a mutant can simply delete.
  It has to be inside the frozen code that gets mutated, with an off-state test that fails when it goes.

## IN scope

- A new key in `OptionalFeatureKeys` and a new seeded row: off, not premium, and "Preview" if the user agrees
  (US-05 open question 3). The default applier is enough, because switching it has no side effect.
- One gate at the one place fills start (`OverTimeGapReconciler`), covering UI and Lighthouse-Clients reads alike.
  It reads the switch each time it is used, so switching needs no restart.
- Both widgets read the mode and choose a sentence. The off sentence lives next to the existing one in
  `overTimeEmptyState.ts`. When the mode cannot be read, the widgets fall back to the off sentence.
- Off-state acceptance tests, backend and frontend. Every existing reconstruction fixture, E2E over-time spec and
  `@screenshot` shot that expects filled days switches the fill on in its own arrangement.
- The ADR-207 amendment: the switch, where the gate sits, and that switching off keeps what was filled.

## OUT of scope

- #6083 (default on) and #6084 (remove the switch).
- A switch per Team or Portfolio; a progress indicator; purging filled days when the switch goes off.
- The docs prose (04-03) and the Lighthouse-Clients copy (finalization, DoD 14).

## Learning hypothesis

**Disproves if it fails**: "the fill has exactly one entry point, so one gate turns it off completely." If an
off-state test catches a row written with the switch off, then something DESIGN did not name also starts fills:
a refresh-event path, the maintenance gate, or the demo loader. The gate then moves or multiplies before any
doc says "off means off".

**Confirms if it succeeds**: turning the fill off is a single condition. That condition is also exactly what
#6084 deletes.

## Acceptance criteria

1. A fresh instance and an upgraded instance both carry the switch off. An upgrade never overwrites a stored
   on/off value.
2. With the switch off, opening either chart for any range, at either scope, from the UI or a client, writes no
   row, then or later. This is asserted against an arrangement where the same open with the switch on does
   write rows, so the assertion cannot pass for free.
3. With the switch on, the existing reconstruction scenarios pass unchanged. Switching it on needs no restart.
4. After switching off, filled days still plot and no new fill starts. A pass already running when the switch
   goes off may finish; that is DESIGN's call, and the test states which way it went.
5. On, an empty chart shows today's sentence unchanged. Off, it shows *"Nothing to show for the selected range.
   Lighthouse adds a day here only when it is running on that day; this instance is not set to fill in past
   days."* When the mode cannot be read, it shows the off sentence.
6. Only a System Admin can change the switch, through the existing guard on the optional-features write. Reads
   stay ungated beyond sign-in. The switch is not premium.
7. Unaffected by the switch: the recorder writes no all-zero rows, past-day limits are judged as of that day, and
   demo loading still writes the synthesiser's rows.
8. Backend `dotnet build` has zero warnings and `dotnet test` is green (connector categories excluded). Frontend
   `pnpm test` is green and `pnpm build` is clean.

## Dependencies

Slices 01–04 (DELIVER through 04-02, done). The US-05 open questions each carry a recommended answer. Question 1
(how the widgets learn the mode) should be settled before the frontend half starts.

## Dogfood moment (same day, restored dev database)

Restore the backup with `Restore-DbBackup.ps1` onto a scratch instance, not the live :5169 data. The backup
predates the switch, so first start seeds it off, which is the upgraded-instance case in real conditions.

1. Switch off: open team 1 Percentiles Over Time for the last 90 days, and portfolio 1 PBC Over Time. Count
   `PercentilesOverTimeSnapshots` / `ProcessBehaviorSnapshots` before and after. Expect no change (36 / 42).
2. Select 2024-01-01..2024-06-30 and read the off sentence on both charts.
3. Switch it on under Settings -> Configuration -> Behaviour Settings, without restarting. Reopen the charts:
   the row counts grow and the line fills in on a later visit.
4. Switch it off. The filled days still plot, and a new range such as 2026-03-01..2026-05-31 writes nothing.
