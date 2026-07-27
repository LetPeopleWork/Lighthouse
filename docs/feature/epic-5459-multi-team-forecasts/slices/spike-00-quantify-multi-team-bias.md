# SPIKE-00 — Quantify the multi-team forecast bias

**Type**: timeboxed SPIKE / hard gate (D8). Nothing ships. No production code is written until the
maintainer has read and accepted the output of this spike.
**Effort**: ≤ ½ day.

## Goal

Measure, on real portfolio data, how far the current worst-team forecast sits from the joint
product-of-CDFs forecast — so the decision to change the core forecasting maths rests on numbers,
not on the algebra alone.

## IN scope

- A throwaway harness (test-project or scratch console, **not** shipped) that, for each existing
  multi-team feature:
  - reads the per-team `WhenForecast` histograms produced by a normal forecast run,
  - computes today's aggregate (worst team at p85) and the D1 aggregate (product-of-CDFs),
  - emits a table: feature, team count, old vs new p50/p70/p85/p95, Δ in days.
- Bucketing of the Δ by contributing-team count (2, 3, 4, 5+).
- A check for **Defect B**: count how many features have at least one percentile whose worst team
  differs from the p85 worst team (i.e. crossing distributions).
- **K5 benchmark**: time to build one aggregate at 5 teams × ~500 distinct day keys, p95 over
  repeated runs — the input to the D7 memoisation decision.
- Largest-remainder residue rule (D6) exercised: confirm the emitted histogram sums exactly to
  `TotalTrials` across every feature in the sample.

## OUT of scope

- Any change to `AggregatedWhenForecast`, `ForecastService`, DTOs, or the frontend.
- Any commit to a production path. The harness is deleted or left uncommitted after the gate.
- Correlation modelling, per-trial max (deferred per D1).

## Learning hypothesis

**Disproves** "the joint-probability correction is worth making" if the p85 Δ is under one day for
essentially every real multi-team feature — in which case the epic is a correctness cleanup with no
user impact, and its priority drops sharply.

**Confirms** the epic as stated if the Δ grows visibly with team count (expected: two teams shift
days, 4-5 teams shift materially more), and/or if Defect B fires on a non-trivial share of features —
which would mean today's numbers are not just optimistic but internally inconsistent across
percentiles.

**Also settles** (D7): whether `Feature.Forecast` needs memoising before slice-01 ships, or whether
the product is cheap enough to leave the computed property as it is.

## Acceptance criteria

- **AC-S0.1** Output table produced from **real portfolio data**, not demo/synthetic data — a
  synthetic run proves the arithmetic, not the magnitude.
- **AC-S0.2** Sample covers at least one feature at each of 2, 3, and 4+ contributing teams, or the
  brief records that the data set contains no such feature.
- **AC-S0.3** Every emitted aggregate histogram sums exactly to `TotalTrials` (D6 residue rule holds).
- **AC-S0.4** Defect-B crossing count reported.
- **AC-S0.5** K5 timing reported with the team-count and day-key-count it was measured at.
- **AC-S0.6** Findings written to
  `docs/feature/epic-5459-multi-team-forecasts/spike-00-findings.md` and **explicitly accepted by the
  maintainer** before slice-01 begins.

## Dependencies

- A Lighthouse instance with real multi-team portfolio data and a completed forecast run.

## Reference class

`docs/feature/test-speed-improvements/spike-be-parallelism-findings.md` — same shape: measure first
on real data, report a number, let the number authorise or kill the follow-on slice.
