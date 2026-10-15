# Slice 01 — The over-time history fill always runs

**Story**: US-01 | **ADO**: 6084 | **Estimate**: ≤ 1 day | **Reference class**: #6083 (seeder-driven change to one optional feature), and DeltaSync's retirement to the deprecated-keys list

## Goal

Remove the `OverTimeHistoryFill` switch so that every instance fills in missing over-time days, and remove
#6083's one-time machinery along with it.

## IN scope

- Delete `IOverTimeHistoryFillSwitch`, `OverTimeHistoryFillSwitch` and their DI registration. Drop the check in `OverTimeGapReconciler` and `OverTimeHistoryFiller` (D5).
- `OverTimeHistoryFillKey` moves from `GetOptionalFeatures()` to `RemoveDeprecatedFeatures` (D2).
- Delete the marker-writing code, `SettleTheHistoryFillAfterARestore` and its `RestoreBackup` call, and `AppSettingKeys.HistoryFillSwitchedOnByDefault` (D3).
- `AppSettingSeeder` gains a key-based obsolete list holding `"OptionalFeatures:HistoryFillSwitchedOnByDefault"` (D4).
- Tests: retire or rewrite #6083's suite, #6053's opt-in suite and the switch's unit tests. Update the ArchUnit seam confinement. Drop `switchHistoryFill` from E2E.
- Docs (`configuration.md`, `predictability.md`, the `releasenotes.md` link target, `ARCHITECTURE.md`, the ADR-207 status note), the `overTimeEmptyState.ts` doc comment, and the Lighthouse-Clients copy plus a patch release.

## OUT of scope

- Any change to the fill's bounds, budget, memo, maintenance gate or queueing.
- Any opt-out path. Frontend UI copy, of which there is none to change.

## Learning hypothesis

- **Disproves** "the fill is safe to run everywhere" if, within 30 days of the release, an instance that had it off reports wrong or slow over-time charts that trace back to the fill.
- **Confirms** it if no such report arrives. The fill is then an ordinary part of the charts, and the Preview bookkeeping is gone.

## Acceptance criteria

AC-1.1 … AC-1.6 in `../feature-delta.md` (US-01).

## Dependencies

#6083 released (v26.10.3.6). The maintainer confirmed the trigger on 2026-10-15.

## Dogfood moment

Upgrade the dev instance (`:5169`, which ran #6083), check that the row and the marker are gone, then open
Percentiles Over Time for a Team with gaps and watch the days arrive on the next open.
