# Slice 04: Backtest — Per-Run Filter Toggle

**Feature**: filter-forecast-throughput
**Stories shipped**: US-06, US-03 chip (extended to the Backtest result surface)
**Estimate**: ~0.5 crafter day
**Reference class**: identical pattern to Slice 02 (Team Forecast toggle).

## Goal
Make backtests honest: a backtest that uses unfiltered throughput while the corresponding forward forecast uses filtered throughput is comparing apples to oranges. Give the user a per-run toggle so they can validate the forecast they actually consume, OR validate the raw model for comparison.

## IN scope
- Backtest input form (existing route(s) for `POST /api/forecast/backtest/{teamId}` in the frontend): new toggle "Apply forecast-throughput filter", defaulted ON when the team has a non-empty filter configured; hidden otherwise (no filter, or non-premium).
- `POST /api/forecast/backtest/{teamId}` accepts optional `applyFilterOverride: bool`; same default semantics as Slice 02 (omitted on premium tenant with filter → treated as `true`).
- Backend backtest computation: when override is `false`, samples historical throughput unfiltered; when `true`, applies the team's rule set to the historical window.
- `BacktestResultDto` extended with `filterApplied: bool` and `excludedSummary: string`.
- US-03 chip rendered on the Backtest result view when `filterApplied: true`.

## OUT scope
- Comparing both backtests (filtered + unfiltered) side-by-side in a single screen → out of feature (potential future).
- Filtering the historical window's dimensions other than the team's throughput → out of scope (the filter applies ONLY to which historical items count toward throughput; the throughput-window dates are unchanged).

## Learning hypothesis
**Confirms if it succeeds**: at least one customer reports using the toggle to validate filtered forecasts against historical results ("the filter improves backtest accuracy" or "the filter has no detectable effect on backtest accuracy — both are useful findings").
**Disproves if it fails**: zero usage → either backtest isn't being run on filtered teams (in which case the Slice 02 learning hypothesis is the bigger issue) or backtest's value-prop doesn't motivate the toggle. Cheap rollback (UI-only).

## Acceptance criteria
See US-06 in `../feature-delta.md` and the chip ACs in US-03.

## Dependencies
**Slice 01** — depends on the persisted rule set, the `IForecastFilterRuleService`, the chip pattern.
Independent of Slices 02 and 03.

## Production data requirement
**Required.** Smoke against the project's own Lighthouse Backtest tool on a team configured with a non-empty filter; both toggle positions; observable difference in backtest result.

## Dogfood moment
Configure a rule set, open Backtest with a representative historical window, run with toggle ON → observe filtered backtest; run again with toggle OFF → observe raw backtest. Verify the filtered run's `excludedSummary` matches the persisted rule set.

## Pre-slice spike candidates
None. Pattern is a direct copy of Slice 02 against the backtest endpoint.
