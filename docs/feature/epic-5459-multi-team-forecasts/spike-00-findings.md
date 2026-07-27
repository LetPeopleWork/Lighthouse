# SPIKE-00 findings — multi-team forecast bias

**ADO**: #5568 (spike) under Epic #5459 · **Run**: 2026-07-27 · **Status**: accepted by maintainer

**Verdict in one line**: the correction is real and material — today's "85 % confident" date is a
**54–80 %** date once every contributing team is accounted for, and the gap widens monotonically with
team count. Proceed with #5569.

---

## Data used — and its limitation, stated up front

**AC-S0.2 outcome: the live instance has exactly ONE team** (`Lighthouse Stories`, portfolio
`Lighthouse Epics`). There is no real multi-team feature in the data set, so the spike could not
measure "the bias on our actual portfolio". The brief anticipated this outcome and requires it be
recorded rather than papered over.

What was used instead is the strongest available real input: **that team's actual 90-day throughput
run chart** (2026-04-29 → 2026-07-27, 165 items, 60 active days, max 9/day, pulled live via MCP),
fed through the **real `ForecastService` Monte Carlo** (10 000 trials, `RandomNumberService`) to
produce genuine per-team completion histograms. Multi-team features are then composed from teams
drawing on that real throughput.

So: **real throughput shape and real simulation, synthetic team composition.** The magnitudes below
are what Lighthouse-like teams produce; they are not a measurement of a specific customer portfolio.
Read the *direction and scale* as solid, the exact day counts as indicative.

---

## Finding 1 — Percentile shift (the headline)

Each team carries the same number of remaining items; all draw on the real throughput series.

| teams | items/team | p50 old→new (Δd) | p70 old→new (Δd) | p85 old→new (Δd) | p95 old→new (Δd) |
|---|---|---|---|---|---|
| 1 | 5 | 3→3 (0) | 5→5 (0) | 6→6 (0) | 8→8 (0) |
| 1 | 15 | 9→9 (0) | 11→11 (0) | 13→13 (0) | 16→16 (0) |
| 2 | 5 | 3→5 (+2) | 5→6 (+1) | 6→7 (+1) | 8→9 (+1) |
| 2 | 15 | 9→11 (+2) | 11→12 (+1) | 13→14 (+1) | 15→17 (+2) |
| 3 | 5 | 3→5 (+2) | 5→6 (+1) | 6→8 (+2) | 8→9 (+1) |
| 3 | 15 | 9→12 (+3) | 11→14 (+3) | 13→15 (+2) | 16→18 (+2) |
| 4 | 5 | 3→6 (+3) | 5→7 (+2) | 6→8 (+2) | 8→10 (+2) |
| 4 | 15 | 9→13 (+4) | 11→14 (+3) | 13→16 (+3) | 16→19 (+3) |
| 5 | 5 | 3→6 (+3) | 4→7 (+3) | 6→8 (+2) | 8→10 (+2) |
| 5 | 15 | 9→13 (+4) | 11→15 (+4) | 13→17 (+4) | 15→19 (+4) |

**Reading it:**

- **Single-team rows are Δ0 at every percentile.** D4's regression guard holds empirically, not just
  algebraically — the overwhelming majority of features will not move at all. This is the single most
  important row in the table for release risk.
- **Every multi-team row moves later, never earlier.** Consistent with AC-01.2.
- **The shift scales with team count**, roughly +1 day per additional team at p50 on a 15-item slice
  — a 9-day p50 becomes 13 days at five teams (+44 %).
- **p50 moves more than p95 in absolute terms.** Worth noting for the release-notes framing: the
  "middle" of the forecast moves most, so users watching a median date will see the biggest jump.

## Finding 2 — What today's "85 %" actually is

Take the date today's forecast labels p85, and ask the joint distribution what that same date is
really worth.

| teams | items/team | today's p85 date (day) | joint likelihood on that day |
|---|---|---|---|
| 2 | 5 | 6 | **80.3 %** |
| 2 | 15 | 13 | **77.9 %** |
| 3 | 5 | 6 | **72.3 %** |
| 3 | 15 | 13 | **69.3 %** |
| 4 | 5 | 6 | **64.7 %** |
| 4 | 15 | 13 | **61.8 %** |
| 5 | 5 | 6 | **57.0 %** |
| 5 | 15 | 13 | **54.4 %** |

This is the number to put in the release notes. At five teams, a date Lighthouse presents as **85 %
confident is a coin flip**. The theoretical prediction for identical independent teams is
0.85ⁿ (72 % at n=3, 44 % at n=5); the measured values sit above that because the discrete day grid
means each team's actual CDF at the p85 day exceeds 0.85. Direction and magnitude both confirmed.

## Finding 3 — Defect B (percentile crossing): **not observed in real throughput**

**0/40** sampled 3-team features showed a percentile where the p85-worst team was not also the worst
team — including a deliberately adversarial configuration (steady 2/day, bursty 18-every-10-days, and
the real series, all at the same mean of ~1.8/day, so the distributions differ in spread rather than
scale).

**Honest reading**: Defect B is real in principle — the code genuinely selects on p85 alone and
genuinely copies one team's whole histogram — but it did not manifest in 80 sampled configurations.
The first probe (varying only item counts and FeatureWIP) was a weak test and was replaced; the
second, varying throughput *shape*, still found nothing. In the shapes tested, the worst team
dominated at every percentile.

**But it is constructible.** See Finding 6(d) — a hand-built pair whose worst team genuinely flips
between p70 and p85. So AC-01.6 is testable; it just needs a constructed fixture rather than a
sampled one.

**Consequence for #5569**: do **not** use Defect B to justify the change. Finding 1 and Finding 2
carry the case on their own. Defect B is removed for free by the product-of-CDFs form (there is no
selection step left), and AC-01.6 keeps it honest — it should stay as an acceptance criterion, but it
is a latent-correctness item, not an observed user-facing bug.

## Finding 4 — D6 residue rule works

**50/50** generated histograms sum exactly to the preserved `TotalTrials` under largest-remainder
allocation. The rule is deterministic (no RNG) and needs no special-casing. `GetProbability` and
`GetLikelihood` keep working unchanged.

## Finding 5 — K5 cost: **no memoisation needed**

| aggregate | p50 | p95 |
|---|---|---|
| product-of-CDFs | 0.107 ms | 0.113 ms |
| today's worst-team copy | 0.007 ms | 0.008 ms |

Measured at the deliberately hostile shape from the brief: 5 teams × 500 distinct day keys.

**~16× slower, but 0.113 ms absolute — 44× under the 5 ms K5 target.** D7 is answered: leave
`Feature.Forecast` as a computed property; do **not** add memoisation in slice-01.

One caveat for DESIGN to hold rather than act on: the property is rebuilt on every get, and DTO
assembly reads it 2–3× per feature. A portfolio page over the live instance's 86 features implies
roughly 200–260 rebuilds ≈ **25 ms** added per portfolio request in the worst-case shape — and far
less in practice, since real features have far fewer than 500 distinct day keys. Acceptable. Revisit
only if a portfolio-load regression actually shows up.

## Finding 6 — Test-data recipe (added 2026-07-27 on maintainer's prompt)

The maintainer proposed building test teams with *known* throughput (one team at 1/day, another at
2/day) so the expected result is computable, plus random teams to check the weaker invariant that the
joint probability is below every individual one. The direction is right. The specific recipe has one
hole, measured rather than argued:

### (a) Constant throughput does NOT distinguish the bug from the fix

| teams | histograms | old p50/p85 | new p50/p85 | distinguishes? |
|---|---|---|---|---|
| TP=1 & TP=2, 6 items each | `{6:10000}` + `{3:10000}` | 6/6 | 6/6 | **NO** |

A team with constant throughput `c` and `N` items finishes on day `ceil(N/c)` with probability 1 —
a **point mass**. And the product of point-mass CDFs *is* the max, which is exactly what today's
worst-team copy already returns. Such a test passes identically against buggy and fixed code.

Keep **one** constant-throughput case as a plumbing anchor, explicitly commented as such. Relying on
it for correctness would buy false confidence.

### (b) Two-value throughput gives a closed form that IS non-degenerate

History `[1, 3]`, 3 remaining items. By hand: day 1 draws 3 (p=.5, done); else draws 1, then day 2
draws 3 (p=.25, done); else day 3 always finishes (p=.25) ⇒ `{1: .50, 2: .25, 3: .25}`.

Measured against the real `ForecastService`:

| day | closed form | simulated | delta |
|---|---|---|---|
| 1 | 50.0 % | 49.6 % | 0.44 % |
| 2 | 25.0 % | 25.4 % | 0.44 % |
| 3 | 25.0 % | 25.0 % | 0.00 % |

Zero trials landed outside `{1,2,3}`.

### (c) Two such teams aggregated — the discriminating case, exact on paper

```
CDF each:  d1=.50   d2=.75    d3=1.00
joint:     d1=.25   d2=.5625  d3=1.00
PMF:       .25 / .3125 / .4375   ->  {1:2500, 2:3125, 3:4375}
```

Verified exactly; sums to 10 000. **old p50 = 1, new p50 = 2.** Every number checkable by hand, and
it fails against the current code — which is the property the constant-throughput case lacks.

### (d) A constructed crossing pair for AC-01.6

Real throughput would not produce one (Finding 3), but a hand-built pair does:

- tight-late `{8:500, 9:9000, 10:500}` vs wide-early `{2:4000, 9:3000, 20:3000}`
- p50 → worst is tight-late (9d vs 9d), p70 → tight-late, **p85 → wide-early (20d)**, p95 → wide-early

The rank genuinely flips across percentiles, which is precisely the situation today's
`MaxBy(GetProbability(85))` mishandles.

### Recommended four-tier test strategy for #5569

1. **Exact unit tests on hand-built histograms** — the gold standard. Aggregation is a pure function
   of histograms, so no simulation and no sampling error are involved and the expected output is
   exact. Covers the product maths, D6 residue, single-contributor identity (AC-01.4), order
   independence (AC-01.6), and the zero-trial contributor.
2. **Property tests over random histograms** — the maintainer's second idea, and the right tool for
   random teams. Invariants: joint CDF ≤ every individual CDF at every day; every aggregate
   percentile ≥ every contributor's; histogram sums to `TotalTrials`; result independent of input
   order; a single contributor round-trips to itself.
3. **Integration tests with closed-form throughput** — `[1,3]`-style histories through the real Monte
   Carlo, proving the whole pipeline rather than just the aggregation. **Budget the sampling error**:
   at 10 000 trials σ ≈ 0.5 % for p≈0.5, so use ±1.5 % (3σ) bands — or, more robustly, assert
   percentile *days* (integers) instead of probabilities.
4. **One constant-throughput anchor**, commented as plumbing-only per (a).

### Test seam: replace the reflection call

`AggregatedWhenForecastTest` currently reaches `SetSimulationResult` through
`typeof(WhenForecast).GetMethod(..., BindingFlags.NonPublic | BindingFlags.Instance)`. Tier 1 needs
roughly a dozen hand-built histograms; a dozen reflection calls is a poor foundation, and this spike
hit the same wall and worked around it with a throwaway subclass.

**Decided (maintainer, 2026-07-27): adjust it.** Slice-01 introduces a proper seam — an `internal`
constructor plus `InternalsVisibleTo`, or a public histogram constructor — and the existing
reflection call in `AggregatedWhenForecastTest` is migrated onto it. DESIGN picks which of the two.

---

## What this changes in the plan

| Item | Before spike | After spike |
|---|---|---|
| Proceed with #5569? | Open | **Yes** — Finding 2 is the justification |
| D7 memoisation | Open, "DESIGN decides" | **Closed — not needed.** Leave the computed property alone |
| D6 residue rule | Proposed | **Validated**, largest-remainder as specified |
| Defect B weight | Co-headline with Defect A | **Demoted** to latent correctness; keep AC-01.6 with a constructed fixture, drop from the release narrative |
| Release-notes framing | "dates move out" | Lead with **"an 85 % that was really 54–80 %"**; note p50 moves most |
| AC-01.3 (strictly later p85) | Assumed testable | **Confirmed reachable** — every multi-team config moved |
| Test data | Unspecified | **Four-tier strategy, Finding 6.** Constant throughput demoted to anchor; `[1,3]` closed form is the discriminating fixture |
| Reflection test seam | Not noticed | **Fix it in slice-01** (maintainer decision) |

## Carried into DESIGN

1. The joint-CDF algorithm as prototyped is sound and small — the spike implementation is ~60 lines
   and can be read as a sketch of the production shape, though it is **not** production code and
   should be rewritten test-first under #5569.
2. **Open question the spike did NOT answer**: what `Team` / `TeamId` / `NumberOfItems` should mean on
   an aggregate that no longer belongs to a single team. The spike sidestepped it by only comparing
   histograms. DESIGN must decide, and must check whether any consumer reads those fields off a
   feature-level forecast.
3. Test seam shape — `internal` + `InternalsVisibleTo` vs a public histogram constructor (Finding 6).

## Harness

`Lighthouse.Backend.Tests/Spike5459/` (`MultiTeamBiasSpike.cs`, `TestDataRecipeSpike.cs`) —
throwaway, never committed, **deleted 2026-07-27** once these findings were accepted. The numbers
above are the durable output.
