# Evolution: epic-5459-multi-team-forecasts (Slices 01 + 02 — COMPLETE)

- **Date finalized**: 2026-07-28
- **ADO**: Epic #5459 ("Multi Team Forecasts", Community / Release Notes). Stories **#5568 SPIKE — Closed**, **#5569 — Closed**, **#5570 — Closed**. The epic itself is still **Active** pending the maintainer's call on closing it.
- **Status**: **COMPLETE — both slices delivered on `main`** (`59e4a3b28..d2cbe8e7b`, 15 commits). DISCUSS → SPIKE → DESIGN → DISTILL → DELIVER complete for the whole epic. Backend 3906/3906; frontend 3772/3772; mutation slice-01 **100 %**, slice-02 backend **80.26 %** / frontend **88.10 %**; CI green after one Sonar cycle.
- **Workspace (history)**: `docs/feature/epic-5459-multi-team-forecasts/`
- **ADRs**: [ADR-110](../product/architecture/adr-110-multi-team-forecast-joint-probability.md), [ADR-111](../product/architecture/adr-111-aggregate-forecast-field-provenance.md), [ADR-112](../product/architecture/adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) — already in the permanent ADR home, no migration needed.

## The defect this epic existed to remove

`AggregatedWhenForecast` picked the single worst contributing team with `MaxBy(f => f.GetProbability(85))` and copied that team's entire histogram. A feature is done only when **every** contributing team is done, so the honest completion curve is the product of the teams' CDFs. The old answer silently assumed every other team was a certainty.

SPIKE-00 measured the cost on real throughput: **a date labelled 85 % confident was worth 77.9 % at two teams and 54.4 % at five.** Single-team features were unaffected at every percentile — measured, not assumed, which is what made the change safe to ship.

## What shipped

### Slice 01 — joint probability (#5569)

- **`JointCompletionDistribution`** (NEW, `Models/Forecast/`) — a pure collaborator, histograms in and histogram out, with no EF or persistence in reach. Builds each contributor's CDF over the union of day keys in one pass, multiplies them in a canonical order, differentiates back to a per-day histogram, and scales to the preserved trial count. The rounding residue is allocated by **largest remainder** with ties going to the earlier day, so the histogram sums exactly and the same input always produces the same output.
- **`AggregatedWhenForecast`** — `MaxBy` deleted; ADR-111 provenance applied (`Team`/`TeamId` null, `NumberOfItems` summed, `CreationTime` the **oldest** contributor, which surfaces as `FeatureDto.LastUpdated` and therefore moves earlier for multi-team features).
- **`WhenForecast`** — an `internal` histogram constructor as the test seam, replacing `BindingFlags.NonPublic` reflection in the tests.

### Slice 02 — "cannot forecast" instead of a false 100 % (#5570)

- **`Feature.TeamsWithoutForecast` / `CanBeForecast`** — a team that must still finish but has closed nothing leaves the feature with no honest distribution. Features with no remaining work are exempt.
- **`Feature.GetLikelhoodForDate` returns `double?`** — `null` rather than falling through to `ForecastBase.GetLikelihood`, whose `trialCounter == 0 → return 100` branch reported **maximum confidence on the one feature nobody could forecast**. That branch is deliberately untouched (DDD-7); the aggregate simply must not reach it.
- **Delivery rollup (ADR-112 D8)** — one un-forecastable feature makes the whole delivery un-forecastable.
- **Frontend** — "Cannot forecast" with a tooltip naming the teams, on the delivery chips *and* the Feature grids of both the Team and Portfolio pages. `ForecastLevel` gained an explicit **Unknown** level.
- **Demo data** — the premium **Dependencies** scenario now demonstrates all four states side by side: joint forecast, thin data (Team Equinox, three active days, below the five-day sufficiency threshold), cannot-forecast (Team Meridian, zero throughput), and a genuinely finished epic (OE-011).

## Key decisions

- **ADR-110** — product of CDFs in a dedicated pure collaborator, largest-remainder residue, **no memoisation** (SPIKE-00 measured 0.113 ms p95 at 5 teams × 500 day keys, 44× under budget).
- **ADR-111** — aggregate provenance; `CreationTime` takes the oldest contributor so a freshly-forecast team cannot mask a stale one.
- **ADR-112** — the unknown state is **carried explicitly**, never inferred by callers from an empty collection, because inference-from-silence is exactly what produced the 100 %.
- **ADR-112 carrier = nullable likelihood**, resolved with a client audit rather than a guess: the CLI/MCP clients type deliveries as `readonly unknown[]` and forward the payload verbatim, so there is no non-nullable `double` to break, and both `DeliveryMetricSnapshot` and `DeliveryMetricsHistoryDto` already used `null` for this exact meaning.
- **ADR-112 D8** (added during delivery) — a delivery containing an un-forecastable feature is itself un-forecastable. Found because `GetLeastLikelyFeature` filters `LikelihoodPercentage >= 0` and would have dropped the unknown feature **silently**, reproducing the worst-team defect one level up.
- **DISTILL decisions** (DT-1..7, in the feature-delta): preserved `TotalTrials` = max over usable contributors; tie-break to the earlier day; zero-count buckets omitted; provenance computed over **all** contributors including zero-trial ones; no property-testing library; Tier-3 asserts percentile **days**, never probabilities.

## What we learned

### Constant-throughput test data cannot prove this fix

A team at throughput 1/day finishes on a single day with probability 1 — a point mass — and **the product of point masses is their maximum**, which is exactly what the buggy code returned. `TP=1 & TP=2` yields identical results under old and new code. The discriminating fixture is two-valued: history `[1,3]` with 3 items gives `{1:.50, 2:.25, 3:.25}`, and two such teams aggregate to `{1:2500, 2:3125, 3:4375}` where the old p50 is day 1 and the new one is day 2. One constant-throughput test survives as a labelled plumbing anchor that says so in its own comment.

### The zero-trial filter was not behaviour-preserving on its own

DDD-3 filtered zero-trial contributors out of the product and called it behaviour-preserving. The full suite disagreed: `ForecastService` emits a **`{0: 0}` day-0 sentinel** for a feature with no remaining work, the filter dropped it, and `GetProbability` returned `-1` — "done" rendering as "no forecast". The aggregate now keeps the union of contributor day keys when no contributor has trials. **The exemption keys off remaining work, not off who owns the empty forecast.** Worth knowing: the 100 % a finished feature reports never goes through the forecast at all — `Feature.GetLikelhoodForDate` short-circuits on `RemainingWorkItems == 0`.

### Adversarial review earned its keep on tests, not on code

Three reviewers ran pre-push. The architect approved against all three ADRs with zero critical/high/medium. The test reviewer found the thing that mattered: **none of the seeded-random tests could tell the new maths from the old worst-team copy.** "Sum is preserved" and "joint never exceeds a contributor" are both true of a plain copy, so the discrimination existed only in the hand-computed fixtures. Fixed by adding a property a copy cannot satisfy — *n* identical contributors must raise each probability to the *n*-th power — and then proving it by sabotage: replacing the product with an assignment makes it fail with `0.2218` against an expected `0.0109`, which is precisely the old behaviour.

Two reviewer findings were **rejected on inspection**: a claimed null-dereference in `TeamsWithoutForecast` (`FeatureWork.Team` is non-nullable and `Feature.Teams` is already dereferenced across existing code) and an integer overflow needing 2.1 billion trials against a `const 10_000`. One was accepted and fixed: `Delivery.GetGoverningFeature` rebuilt each feature's aggregate up to three times per call, which was free when the aggregate was a dictionary copy and is not free now.

### Mutation testing found real holes, and one of its own

Backend started at 61.84 % and reached 80.26 %; the survivors were genuine gaps — the remaining-work exemption could not distinguish `<= 0` from `< 0`, team resolution had no test for the id fallback or the precedence rule, and the delivery sufficiency fallback could not tell `All` from `Any` because every fixture had a single feature. Frontend went 80.46 % → 88.10 %, where the standout was that **nothing tested the Unknown forecast level at all** — the mutants that deleted the whole null branch and renamed the label both lived, meaning the "Risky" fallback the branch exists to prevent was unverified.

One "survivor" was the Stryker config rather than a missing test: a new test class matched none of the `test-case-filter` tokens, so its tests never ran during mutation and the score looked worse than reality. **Check the filter before believing a survivor list.**

### Two CI lessons, both silent locally

Both are in `docs/ci-learnings.md`. **CA1861** — eight inline `new[] {...}` arrays in NUnit assertions, caught pre-push by consulting the ledger, which records six prior recurrences of the same rule, two of them arriving exactly as these did (writing assertions to kill surviving mutants). **CS9236** — the one that got through: the same nested generic lambda appeared three times in one file, Roslyn reports the repeated binding cost at **INFO** severity, `TreatWarningsAsErrors` stays green locally, and the gate is `new_violations = 0`, so one INFO failed the build exactly like a bug. The sharpened rule: treat the backend Sonar gate as *zero new issues of any severity*.

## Docs

`docs/concepts/howlighthouseforecasts.md` previously documented this defect as an **open problem** and asked readers for ideas ("we are not sure what's the best way to handle this. We're open for ideas."). That passage is gone. The section now explains the combination with coins rather than notation, keeps the page's own 95/85/70/50 → 90/72/49/25 example — which now describes what Lighthouse *does* rather than what it fails to do — and adds a **"Doing it by hand"** walkthrough so a reader can reproduce a two-team forecast in a spreadsheet from the teams' own results. A new section covers what happens when a team cannot be forecast and how to close the data gap. The independence assumption is stated plainly: shared people or a hand-off make reality **worse** than the maths suggests.

## Open items carried out of the epic

- **`ForecastBase.GetLikelihood`'s `return 100` on `trialCounter == 0`** — still there, deliberately (DDD-7). It is reachable from single-team paths too, so changing it inside this epic would have altered behaviour outside the epic's scope without its own tests. **Wants its own ticket; not yet filed.**
- **Epic #5459 is still Active** — all three child stories are Closed.
- **Blog post** — the maintainer asked for a step-by-step guide in the style of the letpeople.work Monte Carlo introduction. The worked example, the five-column structure and the points it must land are captured in `slices/slice-01-joint-probability-aggregation.md`; the post itself is deferred.
- **Demo-data side effect worth knowing**: adding a throughput-less team to a portfolio makes every **default-sized** epic in that portfolio un-forecastable too, because default sizing allocates estimated work to every involved team. In the Dependencies scenario that is 4 of 13 epics rather than 1. This is correct behaviour, not a defect, but it is louder than a single instructive example.
