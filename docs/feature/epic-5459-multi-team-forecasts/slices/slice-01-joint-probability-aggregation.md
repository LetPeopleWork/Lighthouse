# Slice 01 — Joint-probability aggregation for multi-team features

**Story**: US-01 (ADO #5569) · **Job**: `job-forecast-multi-team-joint-probability` · **Effort**: ≤ 1 day
**Blocked by**: SPIKE-00 — **accepted 2026-07-27**, see `../spike-00-findings.md`.

## Goal

Replace the worst-team histogram copy in `AggregatedWhenForecast` with the joint completion
distribution across all contributing teams, so every multi-team forecast surface reports the
probability that **all** teams are finished.

## What the spike settled

- **Proceed.** Today's p85 date is worth 54–80 % once every team is counted (Finding 2).
- **D7 memoisation is NOT needed** — 0.113 ms p95 at 5 teams × 500 day keys, 44× under target.
  Leave `Feature.Forecast` as a computed property.
- **D6 largest-remainder residue rule validated** — 50/50 histograms sum exactly to `TotalTrials`.
- **Defect B demoted** to latent correctness (0/40 in real throughput). Keep AC-01.6, use a
  constructed fixture, keep it out of the release narrative.

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
- **Test seam**: replace the `typeof(WhenForecast).GetMethod(..., BindingFlags.NonPublic)` reflection
  call in `AggregatedWhenForecastTest` with a real seam (`internal` constructor +
  `InternalsVisibleTo`, or a public histogram constructor — DESIGN picks). Approved by the maintainer
  on 2026-07-27. Lands as a precursor commit inside this slice, since Tier-1 below needs ~a dozen
  hand-built histograms.
- Tests per the four-tier strategy below.

## OUT of scope

- `ForecastService` and the Monte Carlo loop — untouched (D7).
- The unknown-forecast rule for teams with no throughput — **slice-02**. In this slice, keep today's
  handling of `TotalTrials == 0` contributors unchanged so the two behaviours land and review
  separately.
- Any DTO or endpoint change. Values move; shapes do not.
- Frontend changes. The FE renders whatever dates it is given.
- Memoisation of `Feature.Forecast` (spike Finding 5 closed this as unnecessary).

## Test data — four tiers (spike Finding 6)

**Tier 1 — exact unit tests on hand-built histograms.** Aggregation is a pure function of histograms,
so no simulation and no sampling error are involved. The discriminating fixture, exact on paper:

```
two teams, each {1:5000, 2:2500, 3:2500}      // = throughput [1,3], 3 items
CDF each:  d1=.50   d2=.75    d3=1.00
joint:     d1=.25   d2=.5625  d3=1.00
expected:  {1:2500, 2:3125, 3:4375}           // sums to 10000
old p50 = 1   new p50 = 2                     // FAILS against current code
```

Covers the product maths, D6 residue, AC-01.4 single-contributor identity, AC-01.6 order
independence, and the zero-trial contributor.

**Tier 2 — property tests over random histograms.** Invariants: joint CDF ≤ every individual CDF at
every day; every aggregate percentile ≥ every contributor's; histogram sums to `TotalTrials`; result
independent of input order; a single contributor round-trips to itself.

**Tier 3 — integration with closed-form throughput.** History `[1,3]` with 3 items through the real
`ForecastService` yields `{1:.50, 2:.25, 3:.25}` — measured within 0.44 % of closed form. Proves the
whole pipeline, not just the aggregation. **Budget the sampling error**: at 10 000 trials σ ≈ 0.5 %
for p≈0.5, so use ±1.5 % (3σ) bands, or assert percentile *days* (integers), which is more robust.

**Tier 4 — one constant-throughput anchor, commented as plumbing-only.** A team at TP=1/day with 6
items finishes on day 6 with probability 1 — a point mass. The product of point masses *is* the max,
which is what the buggy code already returns, so `TP=1 & TP=2` yields `6/6` under **both** old and new
code. Useful as an obvious-correctness anchor; useless as proof of the fix. The comment must say so.

**AC-01.6 crossing fixture** (constructed — real throughput would not produce one):
tight-late `{8:500, 9:9000, 10:500}` vs wide-early `{2:4000, 9:3000, 20:3000}` — worst team is
tight-late at p50/p70, wide-early at p85/p95.

## Learning hypothesis

**Disproves** "this is a one-line change at one seam" (the ADO note's own assumption) if the
`Team`/`TeamId` semantics turn out to need decisions that ripple past `AggregatedWhenForecast` — e.g.
if any consumer depends on the aggregate carrying a real team identity.

**Confirms** the seam choice if the diff stays inside one class plus its tests (plus the test-seam
precursor), and the existing `AggregatedWhenForecastTest` behaviours for
`FilterApplied`/`HasSufficientData`/`ExcludedSummary` pass unchanged.

## Acceptance criteria

Full text in `../feature-delta.md` US-01. Summary:

- AC-01.1 aggregate CDF = product of contributor CDFs.
- AC-01.2 every aggregate percentile ≥ every contributor's same percentile.
- AC-01.3 strictly later p85 when ≥2 teams have mass at the old p85 (proves it is not a no-op) —
  spike confirmed every multi-team configuration moved.
- AC-01.4 **single-contributor output identical to today** — the regression guard that matters most;
  spike measured Δ0 at every percentile.
- AC-01.5 histogram sums to `TotalTrials`; deterministic on repeat.
- AC-01.6 result independent of input collection order — use the constructed crossing fixture.
- AC-01.7 flag aggregation unchanged.
- AC-01.8 delivery endpoint likelihood ≤ any single contributor's likelihood.

## Dependencies

- SPIKE-00 findings accepted (D8 gate 1) — **done**.
- Real multi-team portfolio data for the AC-01.8 end-to-end check. Note the spike's AC-S0.2 finding:
  the live instance has only one team, so this needs seeded or demo multi-team data.

## Gates before commit

1. `dotnet build` zero warnings, `dotnet test` green.
2. Mutation testing ≥ 80 % on `AggregatedWhenForecast`.
3. Playwright run locally against a portfolio with a multi-team feature.
4. **Maintainer diff review** (D8 gate 2) — no commit before it.
