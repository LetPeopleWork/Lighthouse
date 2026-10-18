# Slice 01 — The forecast leaves cores free for pages

**Story** Bug #6308 / US-01 · **Job** `job-operator-keep-lighthouse-usable-while-it-forecasts`

## Goal

Pages keep answering while a forecast runs, because the simulation never uses more than half the cores.

## IN scope

- A bounded degree of parallelism for the forecast simulation: max(1, ⌊cores ÷ 2⌋) (D3), **per simulation**
  (NMD-14). Applied in the one method every When/Start simulation goes through (S2), so background, manual
  and multi-team forecasts are all bounded; reality checks and backtests use a separate single-threaded loop
  and are not touched (DESIGN changed assumption 1).
- A seam for the core count, so tests can pin it.

## OUT of scope

- Cancellation (slice 02). Horizon (slices 03–04). A configurable bound. Moving the simulation off the
  thread pool, unless slice 01's production AC shows it is needed (DESIGN Q2).

## Learning hypothesis

**Disproves that CPU and thread-pool starvation from the simulation is why pages hung.** If pages still
hang with the bound in place, the incident has a second cause (for example a database lock held across
the forecast's save), and slices 02–05 are planned knowing that. If it succeeds, the main symptom of the
report is gone.

## Acceptance criteria

AC-01.1 to AC-01.5 in `feature-delta.md`. The ones carrying the risk:

- **AC-01.1** — while a Portfolio forecast is still running, a request for the Portfolios list is answered.
  Pass when the answer arrives before the forecast has finished, never on elapsed time (D13).
- **AC-01.4** — seeded percentiles are identical bounded and unbounded.
- **AC-01.5 (production data, manual gate)** — the dev instance restored from the production backup, Docker
  capped to 4 CPUs, slowest Portfolio forecast running: the pages load; pass when the p95 of 20 reads is
  under 1 s (KPI-1). KPI-3's baseline is recorded at the start of this slice.

## Dependencies

None.

## Effort / reference class

About 3h. Reference class: the `BenchmarkPortfolio` and wall-clock probe fixtures that already pin
`ForecastSimulationLimits`.

## Pre-slice SPIKE

None. The production AC answers the uncertainty.
