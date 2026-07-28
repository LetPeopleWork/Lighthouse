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

## What DESIGN settled (2026-07-27, guide mode — ADR-110/111)

- **DDD-1: the maths lives in a new pure collaborator**, `JointCompletionDistribution` (histograms in,
  histogram out). `AggregatedWhenForecast` keeps only its flag aggregation and calls it. Purity is what
  makes the arithmetic unit- and mutation-testable without constructing an EF-mapped entity.
- **DDD-2: the largest-remainder residue rule lives in that collaborator.**
- **DDD-3: zero-trial contributors are FILTERED OUT of the product in this slice.** This closes a real
  gap in the earlier brief, which said "keep today's handling unchanged" without saying how — once
  `MaxBy` is gone there is no mechanism that discards them. Filtering is behaviour-preserving (today a
  zero-trial forecast loses the selection anyway, `GetProbability` returning `-1`), so this slice stays
  a pure maths change and slice-02 replaces the filter with the explicit unknown state.
- **DDD-4: aggregate provenance** — `Team`/`TeamId` = `null`, `NumberOfItems` = sum of contributors,
  `CreationTime` = **oldest** contributor. A consumer check found the first three are write-only on the
  aggregate; `CreationTime` is not — it surfaces as `FeatureDto.LastUpdated`, so oldest is chosen to
  stop a freshly-forecast team masking a stale one. Expect `LastUpdated` to move earlier for
  multi-team features.
- **DDD-5: test seam = `internal` ctor** on `WhenForecast` taking a histogram.
  `InternalsVisibleTo("Lighthouse.Backend.Tests")` already exists at `Lighthouse.Backend.csproj:64`, so
  this needs no plumbing and leaves the public API untouched.
- **DDD-6/7: no memoisation, and `ForecastBase.GetLikelihood`'s `return 100` is left alone here** —
  it is reachable from single-team paths, so it wants its own ticket.

## IN scope

- `Lighthouse.Backend/Models/Forecast/JointCompletionDistribution.cs` (**NEW**, DDD-1/DDD-2) — pure:
  takes the contributors' histograms, returns a histogram.
  - `CDF_f(d) = ∏ᵢ CDFᵢ(d)` over the union of day keys; `PMF_f(d) = CDF_f(d) − CDF_f(d−1)`,
  - integer histogram summing to the preserved `TotalTrials`, residue by largest-remainder (D6),
  - contributors filtered to `TotalTrials > 0` (DDD-3).
- `Lighthouse.Backend/Models/Forecast/AggregatedWhenForecast.cs`:
  - remove the `MaxBy(f => f.GetProbability(85))` selection (D2), call the collaborator instead,
  - apply ADR-111 provenance: `Team`/`TeamId` = `null`, `NumberOfItems` = sum of contributors,
    `CreationTime` = oldest contributor (DDD-4).
- `FilterApplied` / `HasSufficientData` / `ExcludedSummary` aggregation kept exactly as today
  (Any / All / distinct-join) — regression-covered by the existing `AggregatedWhenForecastTest`.
- **Test seam** (DDD-5): replace the `typeof(WhenForecast).GetMethod(..., BindingFlags.NonPublic)`
  reflection call in `AggregatedWhenForecastTest` with an `internal` constructor on `WhenForecast`
  taking a histogram. `InternalsVisibleTo("Lighthouse.Backend.Tests")` already exists at
  `Lighthouse.Backend.csproj:64`. Lands as a precursor commit inside this slice, since Tier-1 below
  needs ~a dozen hand-built histograms.
- Tests per the four-tier strategy below.

## OUT of scope

- `ForecastService` and the Monte Carlo loop — untouched (D7).
- The unknown-forecast rule for teams with no throughput — **slice-02** (ADR-112). In this slice
  zero-trial contributors are filtered out of the product (DDD-3), which reproduces today's visible
  behaviour, so the two changes land and review separately.
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

## Docs impact — rewrite `docs/concepts/howlighthouseforecasts.md` (at finalization)

The user doc does not merely omit this behaviour — it **documents the bug as an open problem and asks
readers for ideas**. Section `### 2 Teams - 1 Feature` (`docs/concepts/howlighthouseforecasts.md:204-213`)
currently says:

> Lighthouse is then presenting you the forecast that is predicting to be done later. […] What Lighthouse
> does in such cases is not ideal. It's suggesting that there is a 95/85/70/50% probability for the
> forecast that predicts to be done later. However, as it's two forecasts that both need to happen, the
> real probabilities would be 90/72/49/25%. […] While we know it's not properly done at the moment, we
> are not sure what's the best way to handle this. We're open for ideas.

Every sentence of that passage is obsolete once this slice ships. The rewrite must:

1. **Delete the apology.** The `{: .important}` block and "we're open for ideas" go away — this is solved.
2. **State the new rule in one plain sentence**: a feature is done only when every contributing team is
   done, so Lighthouse now multiplies the teams' probabilities together instead of showing the slowest
   team's dates.
3. **Keep the worked numbers, flipped.** The doc's own 95/85/70/50 → 90/72/49/25 example is the clearest
   thing on the page: it now describes what Lighthouse *does*, not what it fails to do. Re-frame it as
   "you asked for 85 %, and with two teams you now get a date that really is 85 %."
4. **Explain it without the maths vocabulary.** No "CDF", no "product of distributions", no "joint
   probability" as a bare term. The two-coin framing works: each team finishing on time is like a coin
   landing heads; both teams landing heads at once is rarer than either one alone. Two teams at 85 %
   give ~72 % together, which is why the honest date moves later.
5. **Say what does NOT change**, because it is the first thing a reader will fear: single-team features
   move zero days, at every percentile (AC-01.4, measured in SPIKE-00).
6. **Note the direction of travel**: dates get later, never earlier. Nobody's forecast gets more
   optimistic because of this change.
7. Keep the existing pointer to [Dependencies](#dependencies) — the advice to avoid them stands, it is
   just no longer compensating for a known inaccuracy.

Also check on the same pass: `### 2 Teams - 2 Features` (line 201) claims independent teams are "the same
case as 1 Team - 1 Feature", which stays true, and the `# Conclusion` section, which should not still
imply the multi-team number is approximate.

Screenshots: no UI change in this slice, so no `@screenshot` regeneration is expected — confirm rather
than assume, since forecast dates appear in committed portfolio screenshots and the demo data may shift
them.

## Second docs deliverable — a hand-runnable step-by-step (maintainer request, 2026-07-28)

Beyond the conceptual rewrite above, Benjamin wants to be able to **run a two-team forecast by hand**
from the two individual team distributions, and to turn that into a blog post in the style of
[An Introduction and Step-by-Step Guide to Monte Carlo Simulations](https://blog.letpeople.work/p/an-introduction-and-step-by-step-guide-to-monte-carlo-simulations).

That post opens with motivation, carries **one** concrete scenario the whole way, hands the reader a
spreadsheet to follow along in, leans on run charts / histograms / tables, and deliberately avoids
formal notation — "randomly select values", "sum up", never `∏` or "CDF". Match that. The sequel
framing is natural: **its output is this post's input.** Where that post stops at one team's
histogram, this one starts there and asks "now do it for two teams."

**Use one worked example throughout** — the same fixture the tests use, because it is exact on paper
and needs no simulation to reproduce:

> Two teams, each with throughput history `[1, 3]` and 3 items left. Each team's Monte Carlo run gives
> **day 1: 5 000 runs · day 2: 2 500 · day 3: 2 500** (out of 10 000).

Five spreadsheet columns, one per step:

| Step | What the reader does | Team A | Team B | Joint |
|---|---|---|---|---|
| 1 | Take each team's simulation results (the "When" sheet of the earlier post) | 5000 / 2500 / 2500 | same | — |
| 2 | Running total ÷ 10 000 = "chance this team is done **by** day X" | .50 / .75 / 1.00 | .50 / .75 / 1.00 | — |
| 3 | **Multiply the two columns, row by row** — both teams must be done | | | .25 / .5625 / 1.00 |
| 4 | Subtract the row above to get "chance it finishes **on** day X" | | | .25 / .3125 / .4375 |
| 5 | Read the percentile off column 3: first day where the running chance passes 50 % / 85 % | p50 = **day 1** | p50 = **day 1** | p50 = **day 2** |

Step 3 is the whole insight and deserves the most words: *both* teams have to be finished, and two
coin-flips both landing heads is rarer than either one alone. Step 5 is the payoff — each team alone
says day 1, together they say day 2, and no team got slower.

Points the post must land, in plain language:

1. **Why multiply rather than take the worst team.** The worst-team answer silently assumes every other
   team is a certainty. Use the 85 % → 72 % framing from `howlighthouseforecasts.md`.
2. **Dates move later, never earlier**, and single-team features do not move at all.
3. **What "independent" assumes** — the multiplication treats the teams as unrelated. Shared people or
   a hand-off between them breaks that, and the honest answer is then *worse* than the maths says. Say
   so plainly; it is the post's main caveat and the natural bridge to the Dependencies section.
4. **Where the reader sees this in Lighthouse**: the portfolio feature forecast columns and the
   delivery likelihood.

Deliverables: the walkthrough lives in `docs/concepts/howlighthouseforecasts.md` (or a linked page) so
the product docs are self-contained; the blog post is Benjamin's, drafted from the same worked example.
A companion spreadsheet mirroring the five columns would match the earlier post's format — offer it,
do not assume it.
