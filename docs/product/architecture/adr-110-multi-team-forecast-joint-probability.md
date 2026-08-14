# ADR-110: A multi-team feature forecast is the product of its teams' completion CDFs, not the slowest team's distribution

- **Status**: Accepted, and unchanged by epic-4365-dependencies. A successor
  ([ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md)) was drafted and **deferred**: a
  Feature that waits on another Feature does break the independence assumed below, but the resulting
  bias is *conservative* — the true joint CDF is at least the product, so the product reports dates
  slightly **later** than the truth, only for Features that are both multi-team and dependent. The
  safe direction, and not worth a second change to the core forecast in one release. The "per-trial
  max inside the Monte Carlo" door this ADR left open under *Alternatives considered* stays open.
- **Date**: 2026-07-27
- **Feature**: epic-5459-multi-team-forecasts (ADO Epic 5459, Story 5569)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

`AggregatedWhenForecast` currently derives a feature's forecast by selecting one contributing team and
copying its entire simulation histogram:

```csharp
var worstCase = materialized.MaxBy(f => f.GetProbability(85));
SetSimulationResult(new Dictionary<int, int>(worstCase.SimulationResult));
```

A feature is complete only when **every** contributing team is complete, so the honest distribution is
that of `max(D₁ … Dₙ)`. Under the independence the simulation already assumes — `ForecastService`
runs one `Task.Run` per team with independent draws from each team's own throughput — that is
`∏ᵢ CDFᵢ(d)`, which is strictly below `CDF_worst(d)` whenever a second team has probability mass at
`d`. The reported figure is therefore optimistic by construction, and invisibly so: nothing on screen
indicates that the other teams' probabilities were discarded.

SPIKE-00 (ADO #5568) quantified this against the real 90-day throughput of the Lighthouse Stories team
run through the real Monte Carlo. A date presented as **85 % confident is worth 77.9 % at two teams
and 54.4 % at five**. Percentile dates move later by roughly a day per additional team, and p50 moves
more than p95 in absolute terms. Single-team features move by **zero days at every percentile**, which
matters because they are the overwhelming majority.

A second defect rides along: the worst team is selected at p85 *only*, so when team distributions
cross, the reported p50/p70/p95 can be read off a team that is not worst at that percentile. SPIKE-00
could not produce a crossing pair from real throughput (0/40 samples, including an adversarial
steady/bursty/real configuration at matched means), so this is latent correctness rather than an
observed user-facing bug — but it is constructible by hand and disappears for free under the decision
below, because there is no selection step left.

## Decision

**Compute the joint completion distribution as a product of the contributors' empirical CDFs**, in a
dedicated pure collaborator, and delete the worst-team selection entirely.

```
contributors = forecasts.Where(f => f.TotalTrials > 0)
days         = union of contributors' day keys, ascending
CDF_f(d)     = ∏ᵢ CDFᵢ(d)
PMF_f(d)     = CDF_f(d) − CDF_f(d−1)
histogram(d) = integer allocation of PMF_f × TotalTrials
```

Four points that are part of the decision, not implementation detail:

1. **A dedicated collaborator, not constructor logic.** The maths lives in a pure type
   (`JointCompletionDistribution`) that takes histograms and returns a histogram. It touches no
   EF-mapped state, so it is directly unit-testable and gives mutation testing a real target — which
   matters against the ≥ 80 % gate. `AggregatedWhenForecast` keeps only its flag aggregation
   (`FilterApplied` Any / `HasSufficientData` All / `ExcludedSummary` distinct-join, all unchanged)
   and calls the collaborator.

2. **Largest-remainder allocation for the residue**, inside that collaborator.
   `ForecastBase.GetProbability` and `GetLikelihood` both divide by `TotalTrials`, so the emitted
   histogram must sum to exactly the preserved trial count. Floor every scaled bucket, then hand the
   remaining units to the buckets with the largest fractional parts. Deterministic — no RNG, no drift
   across repeated aggregation. SPIKE-00 verified 50/50 histograms sum exactly.

3. **Zero-trial contributors are filtered out of the product** for this story. This is
   behaviour-preserving: today a contributor with no usable throughput never simulates, gets an empty
   histogram, and loses the `MaxBy` selection anyway (`GetProbability` returns `-1`). Filtering keeps
   the visible result identical while `MaxBy` is removed, so Story 5569 can be reviewed as a pure
   maths change. **Story 5570 replaces this filter with an explicit unknown state** — see ADR-112.

4. **No memoisation of `Feature.Forecast`.** It stays a computed property rebuilt on every get.
   SPIKE-00 measured the product-of-CDFs build at **0.113 ms p95** for the deliberately hostile shape
   of 5 teams × 500 distinct day keys — 44× under the 5 ms budget, and ~16× the cost of today's
   dictionary copy in relative terms but negligible in absolute ones. The aggregate also materialises
   one transient `IndividualSimulationResult` per day key via `SetSimulationResult`; these are never
   attached to a context, and the measurement above already includes them. Optimising either would be
   speculative.

## Alternatives considered

- **Per-trial max inside the Monte Carlo.** Align trial indices across teams, record each team's
  per-trial completion day, take `max` per trial. Produces an identical distribution under
  independence, and would leave room to model *correlated* teams later. **Rejected for now** — it
  rewrites the hot loop of the core forecasting path and needs trial-level storage (10 000 ints ×
  teams per feature) to buy nothing today. Deferred, not refused: if cross-team correlation ever needs
  modelling, this is the door.
- **Re-rank the worst team per percentile.** Keep the selection but evaluate it separately at
  50/70/85/95 to fix the crossing defect. **Rejected** — it addresses only the latent defect and
  leaves the optimistic bias, which is the actual problem.
- **Keep the maths in the `AggregatedWhenForecast` constructor.** Smallest diff. **Rejected** — a
  constructor doing both distribution maths and flag aggregation is reachable in tests only by
  constructing the EF-mapped entity, which drags persistence concerns into every arithmetic test and
  blunts mutation testing.
- **Weight teams, or make the aggregation strategy configurable.** **Rejected** — no evidence of
  demand, and a configurable forecast semantics surface is a trust liability on the core feature.

## Consequences

- **Positive**: every multi-team forecast surface becomes honest at once — portfolio feature columns,
  delivery likelihood, per-feature likelihood, MCP/CLI — through a single seam, with no DTO or
  endpoint change. The percentile-crossing defect is eliminated as a side effect.
- **Dates move outward on upgrade**, for multi-team features only. Single-team features are unchanged
  (SPIKE-00: Δ0 at every percentile), which bounds the blast radius to the minority case.
- **Recorded `DeliveryMetricSnapshot` history shows a one-time step** at the release boundary.
  Snapshots are forward-only (ADR-048/ADR-049) and store percentile *dates*, not per-team histograms,
  so the history cannot be recomputed — re-simulating would need per-snapshot historical throughput
  that is not retained. Documented in the release notes and the forecasting concept page rather than
  backfilled.
- **Independence is now explicit.** `∏ᵢ CDFᵢ(d)` is exact only if team completion times are
  independent. The simulation already models them that way, so this change makes the *reported number*
  consistent with the *model*; it does not make the model match reality where teams share people.
  Stated openly in the concept docs.
- **Reuse verdict**: `AggregatedWhenForecast` → **EXTEND** (same seam, selection replaced by
  aggregation); `JointCompletionDistribution` → **CREATE NEW** (no existing type combines
  distributions; `ForecastBase` reads a single histogram and cannot host a multi-histogram
  combination without becoming a second responsibility on a persisted base class).
- Cross-refs [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (what the aggregate's
  team/size/timestamp fields mean once no single team owns it),
  [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (the unknown state that
  replaces the zero-trial filter), [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md)
  (the `HasSufficientData` AND-across-teams signal this composes with),
  [ADR-038](./adr-038-forecast-confidence-cap-display-formatter.md) (confidence cap),
  [ADR-058](./adr-058-blackout-forecast-date-shift-translation-placement.md) (day→date translation
  runs after aggregation and is unaffected).
