# Slice 01 — The run records the day it starts each Feature

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-01 · **Estimate**: ~7h
**Reference class**: `TrialCompletions` — a per-worker, lock-free `Dictionary<int,int>[]` summed once
after all runs, and `RecordTheDaysEachRowFinishedOn` which merges the shares. This slice adds a second
array alongside it, filled from a variable the loop already holds, and merged by the same pass.

## Goal

The portfolio API reports, for every Feature that has not started, four dated percentiles saying when
work on it is expected to begin — and for every Feature that has, the day it actually began.

## IN scope

- Record the first day each Feature is pulled, inside the trial. `SimulatedRun.WorkOneDayOf` already
  names the row in `worked`; the start is the first day that names a row of this Feature (D2).
- **Both grains** (D4). Per `(Feature, Team)` row, and per Feature as the earliest across that Feature's
  rows *within* each run. The Feature-level value is stored rather than derived: deriving it from the
  marginals would need `1 - Π(1 - Fi(t))`, which assumes the teams are independent, and dependency-aware
  forecasting exists to model the case where they are not. Inside the trial it assumes nothing.
- Per-team **completion** forecasts on the DTO as well (AC-1.10). They already exist in the domain and
  die at the DTO boundary today; the timeline's sub-lanes need them, and it is one mapping.
- Storage as a distribution in its own right, not inside `Feature.Forecasts` (D3). `Feature.Forecast`
  aggregates that collection unconditionally, so a start row in it silently corrupts the completion
  forecast.
- Expand-only EF migration via `CreateMigration`, all providers.
- `FeatureDto` gains the start percentiles, dated by the same working-day and blackout projection the
  completion percentiles use (`CreateForecastDtos`).
- Read-time override: `StateCategory == Doing` returns `StartedDate`, marked observed, and no percentiles
  (D5). Read-time only — the simulation is not told what is in flight.
- The before-and-after wall-clock measurement for AC-1.8.

## OUT of scope

- Any UI. Nothing renders this yet; slice 02 does.
- Any write-back source. Slice 03.
- Any change to completion forecasting, aggregation or presentation. AC-1.5 asserts there is none.
- Seeding in-flight Features into the WIP slots (D6). Documented residual, separate item.
- Snapshotting over time (D12). This slice only gives a later snapshotter something addressable.

## Learning hypothesis

**Disproves, if it fails**: that the start day is free — the premise the whole Epic was scoped on
("it really should be minimal to get this stored and have 10k results"). Three ways it could fail:

1. **Cost.** A second per-worker array and a second merge pass across 10,000 runs is cheap in
   principle; `Parallel.For` with per-worker state is already the shape, so there is no new
   contention. But this is the busiest loop in the product and the cheapness is asserted, not measured.
   AC-1.8 measures it.
2. **The Feature-grain recording is more intrusive than row-grain. — CONFIRMED at the review gate,
   before any code was written.** `TrialCompletions` is indexed by row and knows nothing about Features.
   `ForecastRunPlan` does group rows by `Feature.ReferenceId` when computing what waits on what, but
   that grouping is keyed by a string rather than a dense index **and `WhatEachRowWaitsFor` returns
   early with no grouping at all whenever nothing waits on anything** — which the class's own comment
   says is almost every forecast. So there is nothing to hoist in the ordinary case: a dense `int[]`
   row-to-Feature index has to be built unconditionally. The per-row half is still free; the Feature-level
   roll-up costs a little more than this slice was scoped on.
3. **Storage shape.** A separate collection off `Feature` is the clean answer, but `SetFeatureForecasts`
   clears and rewrites on every refresh, and a second collection has to follow the same lifecycle
   without a second round of cascade-delete surprises.

**Confirms, if it succeeds**: every other slice is presentation over a number that already exists.
Slices 02-05 add no forecasting logic whatsoever.

**Baseline, measured 2026-09-20 before any of this slice was written** (`StartForecastWallClockProbe`,
fifty Features across five Teams at the shipped ten thousand runs, on the development machine):
**313 ms, 335 ms, 365 ms — median 335 ms.** AC-1.8's 110% budget is a median of **369 ms** on that same
machine. The number is worth nothing on another one, which is why the probe is `[Explicit]` and not an
assertion; it is worth everything here, because it cannot be taken again once the recorder exists.

If (1) fails, the shape changes rather than the feature dying: record the start day for the top N rows
per team rather than for every Feature, since the rows deep in the order are the ones whose start dates
are least trustworthy anyway (D6). Worth knowing before slice 02 is built on it.

## Acceptance criteria

AC-1.1 through AC-1.10 in `feature-delta.md`.

AC-1.2 and AC-1.3 are the two that pin the mechanism to the model rather than to itself, and they are
the two most likely to be written to match whatever the code does. Write them from the reasoning in D7
and S2 **before** running them: at Feature WIP 1 the second Feature's P85 start equals the first's P85
completion exactly, same day; at Feature WIP 3 of five Features exactly three start on day 1.

## Dependencies

P1, P2, P4 — all confirmed by reading (S1, S2, S14). Nothing waits on anything.

## Pre-slice SPIKE

**No.** Every pre-requisite this slice rests on was confirmed by reading the code during DISCUSS, and
the surface inventory records the line numbers. The one measurable unknown is cost, and that is
AC-1.8 rather than a spike — it needs the implementation to exist before it can be measured.

## Effort

~7h. Recording both grains ~1.5h, storage and migration ~2h, DTO including per-team completion ~1.5h,
read-time override ~1h, tests ~2h — rounded down where they overlap. Two things found at the DISTILL
review gate push at the upper end rather than the lower: the row-to-Feature index is new work rather
than a hoist (the existing grouping is keyed by string and is skipped entirely when nothing waits on
anything), and the row type is a new `StartForecast` entity rather than a reused `WhenForecast`.

**The split is planned, not contingent.** It runs along D5, and it is planned because the over-run is
predicted rather than merely possible — ten acceptance scenarios against one slice is itself the signal.

| Part | Ships | Scenarios it turns green |
|---|---|---|
| **01a** | The run records the day, both grains stored, the read carries start percentiles and the per-team breakdown | 1, 2, 4, 5, 7, 8, 9, 10 |
| **01b** | The read-time observed-start override | 3 |

Scenario 6 is green throughout — it is the regression guard that says neither part moved the completion
forecast. 01b is read-time only and touches nothing 01a touches, so 01a is shippable on its own: a
Feature in flight simply reports a forecast start until 01b lands, which is wrong but not incoherent.

At the top of the ≤1-day band, and the likeliest of the six to run over.

## Dogfood moment

Same day: restore the dev instance database (real history, not the checked-in `.db`), refresh a
portfolio, and read the start percentiles out of `GET /api/latest/portfolios/{id}`. The check that
matters is not that numbers appear — it is whether the Features are ordered the way the board is, and
whether anything already in flight reports its real start date rather than "today".
