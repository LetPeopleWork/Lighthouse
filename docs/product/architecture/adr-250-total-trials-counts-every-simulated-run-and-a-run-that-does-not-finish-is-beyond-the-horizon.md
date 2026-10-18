# ADR-250: TotalTrials counts every simulated run, and a run that does not finish is beyond the horizon

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken). Revised at the review gate for the
multi-Team aggregate, dependency circles (NMD-13) and old binaries (NMD-16).
**Feature**: `epic-6311-responsive-during-updates` (Story #6309, slice 03)
**Keeps**: [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) and
[ADR-113](./adr-113-delivery-grain-joint-completion.md)'s combine maths, unchanged.

## Context

A simulated run that does not finish within the horizon records no day, and `TotalTrials` is set to the sum of what was
recorded (`ForecastBase.SetSimulationResult`). Every percentile and likelihood is then read over the runs that finished.
They are biased early, and the bias grows as the horizon shrinks to 5 years (ADR-251). Two related facts:
- A row where no run finished has `TotalTrials = 0`, which the product reads as "this Team has no throughput" ("Cannot
  forecast"), while the raw `ExpectedDate` on the wire reads today.
- Runs that end in a dependency circle are dropped the same way.

`TotalTrials` is a persisted column. Rows whose Team has no throughput never enter the simulation plan
(`ForecastRunPlan.For`).

A Feature's own forecast and a Delivery's forecast are not stored rows but aggregates (`Feature.Forecast`,
`DeliveryCompletionForecast.Build`). `AggregatedWhenForecast` builds them, and today it calls `SetSimulationResult(joint)`,
which sets `TotalTrials` to the joint histogram's sum. Fixing the rows alone would leave every multi-Team Feature and
every Delivery reporting nothing beyond the horizon.

## Decision

1. **`TotalTrials` is the number of runs simulated for that row**, every run, for each completion and start forecast
   the simulation produces. The histogram keeps finished days only. A row with no throughput keeps `TotalTrials = 0` and
   still reads "Cannot forecast".
2. **Beyond the horizon = `TotalTrials − Σ histogram`**, derived and not stored. Percentile p is beyond the horizon
   exactly when fewer than p% of runs finished.
   - Runs that ended because Features wait on each other in a circle count as beyond too (NMD-13). The error log that
     names the circle stays; its sentence calling those dates "not a forecast" is corrected.
   - The "ran out of days" error log goes: reaching the horizon is now counted, not an error. A Debug line with the
     count replaces it.
3. **One read for one percentile** returns `ForecastPercentile`: a day, beyond the horizon, or no answer. When/Start
   callers use it, so `-1` no longer means two things. `HowManyForecast` keeps `GetProbability`.
4. **Likelihood** divides by `TotalTrials`, so it is the share of *all* runs that finished by the date. Past the horizon
   it does not count unfinished runs as finished.
5. **Multi-Team aggregates carry the unfinished runs.** The combinators (`JointCompletionDistribution.Combine`,
   `ComonotonicCompletionDistribution.Min`) stay unchanged.
   - **On the way in:** each contributor is handed to them with one **virtual bucket at key `int.MaxValue`** holding
     `TotalTrials − Σ histogram`, when that is greater than 0. The key sorts last in their ascending day order, so its
     mass reads as "later than any day".
   - **Carriers keep it:** the per-Team carriers `DeliveryCompletionForecast.CarrierFor` builds keep the bucket in their
     histogram (`TotalTrials = Σ`, bucket included), so it survives into the cross-Team `Combine`. A carrier needs no
     second bucket.
   - **Only the aggregate output strips it:** `AggregatedWhenForecast` removes the `int.MaxValue` key and sets
     `TotalTrials` to the **carried total N** (the joint's full sum), not to the sum of the stripped histogram. The
     joint unfinished share is then `1 − Π` of the contributors' finished shares.
   - No histogram a reader projects ever contains `int.MaxValue`.
6. **Write-back** resolves no value for a beyond percentile (NMD-2), through the same read.
7. No migration. Rows written before this change have `TotalTrials = Σ` and read as "nothing beyond" until the next
   forecast rewrites them.

## Alternatives considered

- **A sentinel day (horizon + 1) stored in the histogram**:
  - every reader would need the horizon to recognise it;
  - a likelihood for a date past the horizon would count unfinished runs as finished by then;
  - write-back would project it into a date.
  The virtual bucket above is the same idea, confined to the combinators' inputs; it never leaves the aggregate.
- **A sentinel of `int.MaxValue` stored and read anywhere**: it would hang the working-day projection and overflow the
  window sizing.
- **A new `RunsBeyondHorizon` column**: an expand-only migration on both providers for information `TotalTrials`
  already holds.
- **Teach the combinators a total of their own**: changes the ADR-110/113 maths and their bit-identity proofs.
- **Lower the horizon without honest counting**: makes forecasts more optimistic.

## Consequences

**Positive**
- No schema change. Likelihood and write-back behave correctly through existing code paths.
- A Feature no run can finish reads "More than 5 years", never today and never "Cannot forecast".

**Negative / accepted**
- Features and Deliveries whose forecasts were computed from finished runs only move later; the before/after list is
  read by the maintainer (AC-03.8).
- A Delivery whose rows all have unfinished runs now has a forecast (beyond the horizon, likelihood 0) where it showed
  "cannot forecast".
- **Old binaries** (a rollback, or a replica not yet upgraded) read a beyond percentile as today's date until the
  Portfolio's next forecast rewrites it (NMD-16).
  - The release and rollback notes say to run a forecast update for every Portfolio after a rollback.
  - Until every contributing row is rewritten, an aggregate mixes rows that count unfinished runs with rows that do not.

## Enforcement

- Invariant pinned in unit tests: every forecast `ForecastService` builds has `TotalTrials ≥ Σ histogram`, and
  `TotalTrials = 0` only for rows outside the plan.
- Multi-Team tests: a two-Team Feature, and a Delivery over two Teams, with one Team having unfinished runs, report the
  joint beyond share and the right percentiles. An aggregate's `TotalTrials` is N, and no aggregate histogram holds
  `int.MaxValue`.
- Combinator tests: a single contributor round-trips bit-identically.
- A circle-ended run counts as beyond and still logs the circle error.
- The demo-data E2E that expects "Cannot forecast" for a Team that has closed nothing stays green.
