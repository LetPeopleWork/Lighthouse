# Slice 01 — Joint-probability aggregation for multi-team features

**Story**: US-01 · **Job**: `job-forecast-multi-team-joint-probability` · **Effort**: ≤ 1 day
**Blocked by**: SPIKE-00 accepted by the maintainer (D8).

## Goal

Replace the worst-team histogram copy in `AggregatedWhenForecast` with the joint completion
distribution across all contributing teams, so every multi-team forecast surface reports the
probability that **all** teams are finished.

## IN scope

- `Lighthouse.Backend/Models/Forecast/AggregatedWhenForecast.cs`:
  - remove the `MaxBy(f => f.GetProbability(85))` selection (D2),
  - build each contributor's empirical CDF over its day keys,
  - `CDF_f(d) = ∏ᵢ CDFᵢ(d)` over the union of day keys; `PMF_f(d) = CDF_f(d) − CDF_f(d−1)`,
  - re-emit an integer histogram summing to the preserved `TotalTrials`, residue by
    largest-remainder (D6),
  - carry `Team` / `TeamId` / `NumberOfItems` / `CreationTime` per DESIGN's call — a joint forecast
    has no single owning team, so what those fields mean now is a DESIGN question, not a free choice
    at implementation time.
- `FilterApplied` / `HasSufficientData` / `ExcludedSummary` aggregation kept exactly as today
  (Any / All / distinct-join) — regression-covered by the existing `AggregatedWhenForecastTest`.
- Memoisation of `Feature.Forecast` **only if** SPIKE-00's K5 number says it is needed (D7). If it is
  needed it lands inside this slice as a precursor commit, not as its own slice.
- Backend tests: unit tests for the aggregation maths, plus a property test for AC-01.1/01.2.

## OUT of scope

- `ForecastService` and the Monte Carlo loop — untouched (D7).
- The unknown-forecast rule for teams with no throughput — **slice-02**. In this slice, keep today's
  handling of `TotalTrials == 0` contributors unchanged so the two behaviours land separately and can
  be reviewed separately.
- Any DTO or endpoint change. Values move; shapes do not.
- Frontend changes. The FE renders whatever dates it is given.

## Learning hypothesis

**Disproves** "this is a one-line change at one seam" (the ADO note's own assumption) if the residue
rule, the crossing-distribution cases, or the `Team`/`TeamId` semantics turn out to need decisions
that ripple past `AggregatedWhenForecast` — e.g. if any consumer depends on the aggregate carrying a
real team identity.

**Confirms** the seam choice if the diff stays inside one class plus its tests, and the existing
`AggregatedWhenForecastTest` passes unmodified for the `FilterApplied`/`HasSufficientData`/
`ExcludedSummary` behaviours.

## Acceptance criteria

Full text in `feature-delta.md` US-01. Summary:

- AC-01.1 aggregate CDF = product of contributor CDFs.
- AC-01.2 every aggregate percentile ≥ every contributor's same percentile.
- AC-01.3 strictly later p85 when ≥2 teams have mass at the old p85 (proves it is not a no-op).
- AC-01.4 **single-contributor output identical to today** — the regression guard that matters most.
- AC-01.5 histogram sums to `TotalTrials`; deterministic on repeat.
- AC-01.6 result independent of input collection order.
- AC-01.7 flag aggregation unchanged.
- AC-01.8 delivery endpoint likelihood ≤ any single contributor's likelihood.

## Dependencies

- SPIKE-00 findings accepted (D8 gate 1).
- Real multi-team portfolio data for the AC-01.3 / AC-01.8 end-to-end check (per the carpaccio
  production-data rule).

## Gates before commit

1. `dotnet build` zero warnings, `dotnet test` green.
2. Mutation testing ≥ 80 % on `AggregatedWhenForecast`.
3. Playwright run locally against a portfolio with a multi-team feature.
4. **Maintainer diff review** (D8 gate 2) — no commit before it.
