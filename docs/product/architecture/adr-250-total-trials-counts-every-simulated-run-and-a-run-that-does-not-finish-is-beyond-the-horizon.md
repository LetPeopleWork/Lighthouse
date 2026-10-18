# ADR-250: TotalTrials counts every simulated run, and a run that does not finish is beyond the horizon

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken).
**Feature**: `epic-6311-responsive-during-updates` (Story #6309, slice 03)
**Keeps**: [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) and
[ADR-113](./adr-113-delivery-grain-joint-completion.md)'s combine maths, unchanged.

## Context

A simulated run that does not finish within the horizon records no day, and `TotalTrials` is set to the sum of what was
recorded (`ForecastBase.SetSimulationResult`). Every percentile and likelihood is then read over the runs that finished.
They are biased early, and the bias grows as the horizon shrinks to 5 years (ADR-251). A row where no run finished has
`TotalTrials = 0`, which the product reads as "this Team has no throughput" ("Cannot forecast"). The raw `ExpectedDate`
on the wire reads today. Runs that end in a dependency circle are dropped the same way.

`TotalTrials` is a persisted column. Rows whose Team has no throughput never enter the simulation plan
(`ForecastRunPlan.For`).

## Decision

1. **`TotalTrials` is the number of runs simulated for that row**, every run, for each completion and start forecast
   the simulation produces. The histogram keeps finished days only. A row with no throughput keeps `TotalTrials = 0` and
   still reads "Cannot forecast".
2. **Beyond the horizon = `TotalTrials − Σ histogram`**, derived and not stored. Percentile p is beyond the horizon
   exactly when fewer than p% of runs finished. Runs that ended because nothing could be started (a circle) count as
   beyond too.
3. **One read for one percentile** returns a value object: a day, beyond the horizon, or no answer. When/Start callers
   use it, so `-1` no longer means two things. `HowManyForecast` keeps `GetProbability`.
4. **Likelihood** divides by `TotalTrials`, so it is the share of *all* runs that finished by the date. Past the horizon
   it does not count unfinished runs as finished.
5. **Combinators stay closed.** `AggregatedWhenForecast` and `DeliveryCompletionForecast` hand each contributor in with
   its unfinished runs as one virtual last bucket, call `JointCompletionDistribution` / `ComonotonicCompletionDistribution`
   unchanged, then strip that bucket and carry the total. The joint unfinished share is then `1 − Π` of the contributors'
   finished shares: later than any day.
6. **Write-back** resolves no value for a beyond percentile (NMD-2), through the same read.
7. No migration. Rows written before this change have `TotalTrials = Σ` and read as "nothing beyond" until the next
   forecast rewrites them.

## Alternatives considered

- **A sentinel day (horizon + 1) in the histogram**: the combinators would need no change. But every reader would need
  the horizon to recognise the sentinel, a likelihood for a date past the horizon would count unfinished runs as finished
  by then, and write-back would project it into a date. A sentinel of `int.MaxValue` would hang the working-day
  projection and overflow the window sizing.
- **A new `RunsBeyondHorizon` column**: an expand-only migration on both providers for information `TotalTrials`
  already holds.
- **Lower the horizon without honest counting**: makes forecasts more optimistic.

## Consequences

**Positive**
- No schema change. Likelihood and write-back behave correctly through existing code paths.
- A Feature no run can finish reads "more than 5 years", never today and never "Cannot forecast".

**Negative / accepted**
- Features and Deliveries whose forecasts were computed from finished runs only move later; the before/after list is
  read by the maintainer (AC-03.8).
- A Delivery whose rows all have unfinished runs now has a forecast (beyond the horizon, likelihood 0) where it showed
  "cannot forecast".

## Enforcement

- Invariant pinned in unit tests: every forecast `ForecastService` builds has `TotalTrials ≥ Σ histogram`, and
  `TotalTrials = 0` only for rows outside the plan.
- Combinator tests: a single contributor round-trips bit-identically; any contributor with unfinished runs makes the
  joint carry them.
- The demo-data E2E that expects "Cannot forecast" for a Team that has closed nothing stays green.
