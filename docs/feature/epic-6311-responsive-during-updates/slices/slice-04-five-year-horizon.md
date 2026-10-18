# Slice 04 — Five-year horizon

**Story** #6309 / US-05 · **Job** `job-forecast-no-false-certainty` · **Released** together with slice 03
(US-03) under Story #6309 (NMD-5)

## Goal

No simulated run covers more than 5 years. A Feature too far out reads "More than 5 years", and the
slowest forecasts get much shorter.

## IN scope

- `ForecastSimulationLimits.Default` horizon from 100,000 days to 5 years: **1,827 simulated days**
  (5 × 365 + 2, so it spans 5 calendar years even with two leap days, DDD-6).
- The "5 years" copy reads from the `horizonYears` the forecast carries, on live forecasts and Delivery
  history points alike, never a second literal (AC-04.2, NMD-12).
- Fixture baselines (S18) re-checked; any that move are explained.

## OUT of scope

- A configurable horizon. Changing the number of runs.

## Learning hypothesis

**Disproves that few real Features sit beyond 5 years.** If the restored production data shows many
Features flipping to "more than 5 years", the horizon is cutting into answers users rely on, and the
maintainer decides whether 5 is the right number before release. It also measures whether the horizon is
what made the long forecasts long (KPI-3).

## Acceptance criteria

AC-04.1 to AC-04.4 (US-05). **AC-04.4 (production data, manual gate)**: the slowest Portfolio's forecast
`RefreshLog.DurationMs` on the restored dev instance, before slice 01 and after this slice; pass when it is
at least 50% lower, fail otherwise (KPI-3).

## Dependencies

**Slice 03** (D7). Without honest counting, a lower horizon makes forecasts more optimistic (S9). Released
together with slice 03 (NMD-5).

## Effort / reference class

About 3h. Mostly fixture review.

## Pre-slice SPIKE

None.
