# Slice 04 — Five-year horizon

**Story** #6309 / US-03 · **Job** `job-forecast-no-false-certainty`

## Goal

No simulated run covers more than 5 years. A Feature too far out reads "more than 5 years", and the
slowest forecasts get much shorter.

## IN scope

- `ForecastSimulationLimits.Default` horizon from 100,000 days to 5 years of simulated days (unit per
  DESIGN Q5).
- The "5 years" copy reads from the same constant (AC-04.2).
- Fixture baselines (S18) re-checked; any that move are explained.

## OUT of scope

- A configurable horizon. Changing the number of runs.

## Learning hypothesis

**Disproves that few real Features sit beyond 5 years.** If the restored production data shows many
Features flipping to "more than 5 years", the horizon is cutting into answers users rely on, and the
maintainer decides whether 5 is the right number before release. It also measures whether the horizon is
what made the long forecasts long (KPI-3).

## Acceptance criteria

AC-04.1 to AC-04.4. **AC-04.4 (production data)**: the slowest Portfolio's forecast `RefreshLog.DurationMs`
on the restored dev instance, before slice 01 and after this slice; target at least 50% lower (KPI-3).

## Dependencies

**Slice 03** (D7). Without honest counting, a lower horizon makes forecasts more optimistic (S9).

## Effort / reference class

About 3h. Mostly fixture review.

## Pre-slice SPIKE

None.
