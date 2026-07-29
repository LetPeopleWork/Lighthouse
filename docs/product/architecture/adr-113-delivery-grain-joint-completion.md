# ADR-113: A delivery's completion forecast is the joint distribution over its `(team, feature)` rows — comonotonic within a team, independent across teams

- **Status**: Accepted (2026-07-29, DESIGN wave for ADO #5587). Interaction mode = **guide**; every
  decision point below was answered by the maintainer in session and is recorded, not proposed.
- **Date**: 2026-07-29
- **Feature**: delivery-joint-likelihood (ADO User Story **#5587**, parent Epic **#5459**)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

[ADR-110](./adr-110-multi-team-forecast-joint-probability.md) removed the "one representative stands
for the whole" defect at **feature** grain: a feature's forecast is now the product of its teams'
CDFs rather than the worst team's histogram. The same defect survives one level up, twice over.

**Rollup 1 — `Delivery.CalculateMetrics` → `GetGoverningFeature`** (`Models/Delivery.cs:51-107`).
Selects one feature by `OrderByDescending(forecast.GetProbability(85)).ThenBy(likelihood)` and reports
*that feature's* likelihood **and** its 70/85/95 percentile dates as the delivery's. Every other
feature in the delivery is silently treated as a certainty. Two features each at 85 % report 85 %,
where the honest joint answer is ≈ 72 %.

**Rollup 2 — `DeliveryWithLikelihoodDto.GetLeastLikelyFeature`** (`API/DTO/…:66,125`). Since
[ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) D8 it no longer drives
the likelihood, but it still drives `HasSufficientData`: the delivery's "not enough data" warning is
read off a single feature.

**The grain that makes the rollup correct is `(team, feature)`, not `feature`.**
`ForecastService.RunMonteCarloSimulation` groups trials by **team**
(`simulationResults.GroupBy(s => s.Team)`, one `Task.Run` per team). Two features worked by the same
team share that team's throughput draws and contend for its `FeatureWIP` — they are positively
correlated. Two features on different teams draw from independent streams. So the delivery CDF is

```
rows          = the delivery's (team, feature) work pairs that still have remaining work
bucket(t)     = { r ∈ rows : r.team == t }
teamCdf(t)(d) = min over r ∈ bucket(t) of CDF_r(d)      # comonotonic within a team
deliveryCdf(d)= ∏ over t of teamCdf(t)(d)               # independent across teams
```

Two wrong shapes are readily reachable and must be excluded by construction, not by review:
multiplying *feature* CDFs double-penalises same-team features, and building a team term out of
`feature.Forecast` (the `AggregatedWhenForecast`) folds team B into team A's term and then multiplies
B again. The DISCUSS wave pinned a fixture that separates all three at once — `A/F1 = 0.90`,
`B/F1 = 0.80`, `B/F2 = 0.95` yields **0.720** correct, 0.684 for the feature-CDF product, 0.518 for
the aggregate-derived team term.

## Decision

**Compute the delivery's completion distribution as the cross-bucket product of per-bucket
comonotonic minima, enumerated from the authoritative pair set, and delete both representative
selectors.** Seven points are part of the decision, not implementation detail.

### 1. Two combinators, two types, behind one composing builder

`min` operates only *within* a bucket; the product operates only *across* buckets; the two never touch
the same pair. That invariant is load-bearing — it is what stops a shared feature being penalised
twice.

| Type | Location | Role |
|---|---|---|
| `JointCompletionDistribution` | `Models/Forecast/JointCompletionDistribution.cs` | **unchanged** — product ACROSS buckets (ADR-110) |
| `ComonotonicCompletionDistribution` | `Models/Forecast/ComonotonicCompletionDistribution.cs` | **NEW** — elementwise `min` WITHIN a bucket |
| `CompletionHistogram` | `Models/Forecast/CompletionHistogram.cs` | **NEW** — shared primitives: `TrialsIn`, `CumulativeProbabilities`, `DistributeByLargestRemainder` |
| `DeliveryCompletionForecast` | `Models/Forecast/DeliveryCompletionForecast.cs` | **NEW** — the composing builder: pairs → bucket → `Min` → carrier → `AggregatedWhenForecast`. Reimplements no maths |

The two combinators are distinct types on **cohesion** grounds — two domain operations with two
different justifications (comonotonicity vs independence), one pure type each, exactly as ADR-110
already shapes this corner of the model. **The type split alone does not make the grain invariant
machine-checkable**, and any claim that it does should be treated as withdrawn: the invariant is a
property of the *call site*, which must depend on both combinators. What makes it checkable is the
builder — with the composition behind one collaborator, the enforceable rule becomes `Models.Delivery`
must not depend on either combinator (see Consequences), which forbids precisely the mistake.

`CompletionHistogram` is extracted from `JointCompletionDistribution`'s existing private helpers
**behaviour-preservingly, in its own `refactor(forecast):` commit, separate from the feature commit**
(project convention: refactor commits are never mixed with feature commits).

### 2. `Min` short-circuits at `count == 1`, verbatim

`ComonotonicCompletionDistribution.Min(histograms)`:

- filter to contributors with trials (`TrialsIn > 0`); **zero contributors ⇒ empty result**, and the
  caller (point 4) treats an empty bucket as CDF ≡ 1 rather than as a distribution;
- **exactly one contributor ⇒ return that histogram verbatim.** This is not an optimisation. Round-
  tripping a single histogram through cumulative → differentiate → largest-remainder can shift one
  trial between adjacent days, because `(a/T − b/T) × T` is not bit-identical to the original count in
  IEEE 754. That shift would break the AC-01.5 bit-identity requirement (point 6);
- two or more contributors ⇒ elementwise minimum of the contributors' cumulative probabilities over
  the ascending day-key union, differentiate, then `DistributeByLargestRemainder` with the **same**
  tie-break as `JointCompletionDistribution` (`OrderByDescending(remainder).ThenBy(day)`), preserving
  `totalTrials = contributors.Max(TrialsIn)`.

The result is a valid CDF: each contributor's cumulative series is non-decreasing and reaches 1 at the
last key of the union, and a pointwise minimum of such series is non-decreasing and reaches 1 there
too — so the differentiated mass sums to exactly `totalTrials` before rounding.

### 3. `Min` must **not** sort its inputs — and the absence of the sort is deliberate

`JointCompletionDistribution` sorts the contributor probabilities before multiplying, with a comment
explaining why: IEEE 754 multiplication is not associative, so a caller-determined order can differ in
the last bit and tip a rounding decision. **Minimum has no such hazard** — it returns one of its
inputs unchanged, performs no arithmetic and therefore no rounding, and is invariant under permutation
of finite inputs. Adding a mirrored `.Order()` to `Min` would be dead code that the next reader takes
for a load-bearing invariant. The absence carries a one-line comment pointing at this ADR so it is not
"fixed" later.

### 4. The delivery distribution is built by **reuse**, not by a new distribution type

No new carrier is introduced for the header dates:

```
DeliveryCompletionForecast (NEW — a composing builder, reimplementing no maths):
  pairs      = FeatureWork with remaining work, LEFT JOIN Forecasts   (point 5)
  per team t: a WhenForecast whose histogram = ComonotonicCompletionDistribution.Min(bucket(t))
  delivery:   new AggregatedWhenForecast(those per-team forecasts)
```

`AggregatedWhenForecast` already performs the cross-bucket product through
`JointCompletionDistribution` (which satisfies the reuse requirement by construction), ORs
`FilterApplied`, distinct-joins `ExcludedSummary`, and ANDs `HasSufficientData`. **Likelihood and all
three percentile dates therefore come off one object, with no representative feature anywhere in the
path.** That is the direct answer to "where do the header dates come from once the governing feature
is gone".

The composition lives in `DeliveryCompletionForecast` rather than inline in `Delivery.CalculateMetrics`
for three reasons: it keeps ~40 lines of non-trivial combination logic out of an EF-mapped entity that
also holds `Name`, `Date`, `Portfolio` and the rule-set JSON; it gives the ≥ 80 % mutation gate a pure
target that needs no `Delivery` graph to construct; and it creates the boundary that makes the grain
rule **machine-checkable** (`Models.Delivery` must not depend on either combinator). This is ADR-110
point 1's own reasoning — "a dedicated collaborator, not constructor logic" — applied one grain up. The
**guards stay on `Delivery`**: they are delivery policy, not combination.

The per-team carrier applies [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) provenance
one grain down: `NumberOfItems` summed over the bucket, `CreationTime` the **oldest** row in the
bucket, `FilterApplied` any, `ExcludedSummary` distinct-joined, `HasSufficientData` all.

**`Team`/`TeamId` and `Feature`/`FeatureId` are left null on the carrier.** Nothing downstream reads
them, and leaving the navigations unset is what keeps these transient read-path instances structurally
unable to be fixed up by EF onto a tracked `Feature` or `Team`. ADR-110 point 4 already records that
the transient `IndividualSimulationResult` objects the aggregate allocates are never attached to a
context; this decision extends that guarantee to the per-team carriers, and point 7 makes it a test
rather than a promise.

### 5. Enumerate from `FeatureWork`, LEFT JOIN `Forecasts` — the direction is the decision

**Rows are enumerated FROM `feature.FeatureWork.Where(w => w.RemainingWorkItems > 0)`, LEFT JOINed to
`feature.Forecasts`.** `FeatureWork` is the authoritative set of contributing pairs; `Forecasts` is a
derived, *lagging* projection of it, rewritten only by `SetFeatureForecasts` on a forecast run. The
reverse direction is unsafe and is rejected in Alternatives: a pair with remaining work and no forecast
row would emit nothing, land in no bucket, and contribute **CDF ≡ 1** — a silent certainty, which is
this ADR's own defect one grain lower.

A pair that fails the remaining-work predicate contributes CDF ≡ 1 and is **not enumerated**; a bucket
left with no pairs is **absent from the product**. Both are the identity element of their operator
(`min(x, 1) = x`, `1 × x = x`), so no degenerate empty CDF is ever constructed and no bucket silently
vanishes with the wrong value. This is only sound because the *only* pairs that resolve to 1 are pairs
with no remaining work — which is exactly what the join direction guarantees.

The predicate reads `FeatureWork`, never the emptiness of the forecast and never who owns it. Because
`Forecasts` is EF-persisted and lags, **four** shapes are reachable, and this direction handles all
four:

| Shape | Why it occurs | Handled by |
|---|---|---|
| Pair **absent** from `Forecasts`, remaining work `0` | `InitializeSimulationResults` filters `RemainingWorkItems > 0` — the normal case | not enumerated |
| Pair **present with full trials**, remaining work now `0` | the common stale shape: work finished after the last forecast run, so the row keeps all 10 000 trials | not enumerated |
| Pair **present with zero trials** | the team lost its throughput and was dropped from the simulation (`ForecastService.cs:112-119`, `:126`) — the **only** way a row reaches zero trials | not enumerated if remaining work is 0; if remaining work is `> 0`, guard 2 fires |
| Pair **has remaining work but NO row at all** | `AddOrUpdateWorkForTeam` ran during work-item sync after the last forecast run (`WorkItemService.cs:332`, `:412`) | **cannot forecast**, team named — never a silent 1 |

The second and third shapes are **different fixtures**; a row stale from finished work keeps its full
trials, and only a throughput-less team yields a zero-trial row. Conflating them yields an
unconstructible fixture and a false belief that the completed-row case is covered.

**Detecting the fourth shape requires `Feature.TeamsWithoutForecast` to grow a second clause** — pairs
with remaining work and no `Forecasts` row — because that property is the only path that *names* the
team, and the resolution requires the team to be named. Its completed-feature exemption is unchanged.
Consequence, flagged rather than assumed: this also moves the **feature** surface (a feature whose
newly-synced team has no forecast row yet reads "cannot forecast" on the Team and Portfolio grids),
which is arguably a latent-defect fix at feature grain — *no row at all* is strictly worse than zero
trials, which ADR-112 already treats as un-forecastable — but is outside this story's stated
delivery-only scope.

The whole-feature `{0: 0}` sentinel (`Team == null`, emitted by `ForecastService` for a feature with no
rows) matches no `FeatureWork` pair and is unreachable from this direction, so a null-keyed bucket is
structurally unrepresentable. The remaining-work exemption is the same rule as ADR-112's
completed-feature exemption, one grain down.

### 6. The two "no rows" cases are distinguished explicitly, and neither reaches `GetLikelihood`

An empty delivery and an all-done delivery both produce zero contributing pairs and must report
opposite numbers. Both are answered by explicit guards in `Delivery.CalculateMetrics`:

1. **No features at all ⇒ `0 %`, empty completion dates.** Preserves today's behaviour.
2. **Any feature with `CanBeForecast == false` ⇒ unknown (`null`), empty dates, teams named.**
   ADR-112 D8, unchanged in substance, and — with point 5's extension — the guard that also covers the
   missing-pair case. Its position relative to guard 1 is unobservable (the two are disjoint); what
   matters is that it precedes the joint computation.
3. **Features present, total remaining work `<= 0` ⇒ `100 %`**, with percentile dates taken from a
   single `{0: 0}` day-0 marker, byte-for-byte the shape `ForecastService` already emits for a finished
   feature. This mirrors `Feature.GetLikelhoodForDate`'s `RemainingWorkItems == 0` short-circuit one
   grain up. **The dates are unchanged only if the delivery was already complete at the last forecast
   run**; if it finished *between* runs the persisted rows still carry full trials, so today's path
   shows future dates against a likelihood of 100 and this guard moves them to `today`. Better, but a
   visible delta in its own right.
4. **Backstop, at pair grain**: any contributing pair still lacking a `Forecasts` row ⇒ unknown. Should
   be unreachable once guard 2 covers it; retained because it re-derives the predicate from the pair set
   the maths actually consumes, so the two cannot drift apart silently.
5. Otherwise: build the buckets and read likelihood and dates off the aggregate.

Guards 3 and 4 exist for the same reason: `ForecastBase.GetLikelihood` used to end `return 100` when
`trialCounter == 0`, so an absence of evidence read as certainty. ADO Bug **#5586** fixed that — the
method now returns 0 on no evidence and cumulates to `CDF(threshold)` rather than to the next bucket
at or after it. The guards stay: **the delivery rollup must never depend on that branch for its
100 %**, exactly as ADR-112 point 3 requires of the feature path. A 100 % that is *meant* is returned
by an explicit rule; a 100 % that falls out of an empty histogram is the defect this ADR family
exists to remove. Guard 3 carried a `// Stryker disable once all` annotation that was only an
equivalent mutant while #5586 stood; the fix made it observable and the annotation is gone.

### 7. `GetGoverningFeature` and `GetLeastLikelyFeature` are both deleted

`GetGoverningFeature` currently carries the ADO **#5435** tie-break fix (ranking by likelihood alone
saturates for large deliveries and the tie-break then falls back to arbitrary collection order,
surfacing delivery dates earlier than an individual feature's). That fix is **structurally superseded,
not dropped**: there is no selection step left to tie-break, and the delivery CDF is pointwise ≤ every
feature's CDF.

**Stated precisely, because the strong form is false.** The inequality is exact **on the CDFs**. It
does **not** survive `DistributeByLargestRemainder`, which floors per day and hands the residue to the
largest fractional remainders — an allocation that is not monotone across two different day-key grids.
Since D5 makes near-equality the *common* case, a one-trial residue difference can still put a delivery
percentile **day** one earlier than a feature's. So the invariant is: **exact on the cumulative series,
±1 trial on the emitted histograms.** Assert it on the pre-rounding series, or on days with an explicit
one-day tolerance that names the residue as the reason — never as an unqualified strict inequality over
demo data, and never via the deleted tie-break.

`GetLeastLikelyFeature` is deleted with it. `HasSufficientData` becomes the AND across the delivery's
features **that have remaining work** (empty set ⇒ `true`), computed on `Delivery` and returned on
`DeliveryMetricsProjection`; `DeliveryWithLikelihoodDto` copies it. **No wire-contract change**: no new
field on `FeatureLikelihoodDto`, nothing new on the DTO surface, so the CLI/MCP payloads are untouched.

**One consequence, recorded because it is a visible behaviour delta — and the mechanism is the SPLIT,
not an ordering.** Today `CalculateMetrics` returns `0.0` keyed on `governingFeature == null`
(`Delivery.cs:56-59`), one condition standing for **two different things**: "the delivery has no
features" *or* "the `likelihood >= 0` filter rejected every candidate" (`null >= 0` is `false` in C#,
so an un-forecastable feature drops out of the ranking). A delivery in which **every** feature is
un-forecastable therefore reports **0 %** today instead of "cannot forecast", contradicting D8.
Deleting the selector **splits that condition in two**: guard 1 narrows to `Features.Count == 0`, and
the all-un-forecastable case falls through to guard 2. That split is the fix. An earlier draft claimed
the fix was placing guard 2 *above* guard 1 — that is wrong, since `Features.Count == 0` and
`Features.Any(…)` are disjoint and no ordering between them is observable. What D2/D8 actually require,
and what holds, is that guard 2 precedes the **joint computation**.

## Alternatives considered

- **A `Min` overload on `JointCompletionDistribution`.** Smallest diff, one file. **Rejected on
  cohesion**: `min`-within-a-bucket and product-across-buckets are two domain operations with two
  different justifications (comonotonicity vs independence), and ADR-110 already shapes this corner of
  the model as one pure type per operation. **Note the justification that was withdrawn**: an earlier
  draft claimed the split made D5 machine-checkable via "neither combinator may depend on the other".
  It does not — the grain invariant is a property of the **call site**, which must depend on both, so
  that rule is satisfied by a caller that applies `Min` across teams. It forbids only what nobody would
  write. The enforceable rule is the one on `DeliveryCompletionForecast` below.
- **Keep the composition inline in `Delivery.CalculateMetrics`** (no builder). This is what the first
  draft of the design did. **Rejected** — it grows an EF-mapped entity that already holds `Name`,
  `Date`, `Portfolio`, `SelectionMode` and the rule-set JSON by ~40 lines of pair enumeration,
  bucketing and carrier construction; it leaves the grain rule unenforceable (there is no boundary to
  put a rule on); and it makes the ≥ 80 % mutation target reachable only by constructing a full
  `Delivery` graph. This is precisely **ADR-110 point 1's own reasoning** ("a dedicated collaborator,
  not constructor logic … touches no EF-mapped state, so it is directly unit-testable and gives
  mutation testing a real target"), applied one grain up.
- **Enumerate rows from `Forecasts`, left-joining `FeatureWork` for remaining work.** The obvious
  reading, and what the first draft specified. **Rejected — this is the C1 defect.** A `FeatureWork`
  with remaining work and no matching `Forecasts` row emits no row, lands in no bucket, and therefore
  contributes CDF ≡ 1: a silent certainty, this feature's own defect one grain lower. Reachable via
  `WorkItemService.cs:332`/`:412` → `AddOrUpdateWorkForTeam` during work-item sync, which is not a
  forecast run. `Feature.TeamsWithoutForecast` iterates `Forecasts` and cannot see it. Enumerating from
  `FeatureWork` makes the missing row a *detected absence* rather than an *undetectable one*.
- **Multiply the delivery's *feature* CDFs** (`∏_f CDF_f`). Simplest possible reading of "all features
  must land". **Rejected** — features sharing a team are positively correlated by construction of the
  simulation, so this double-penalises them: 0.684 against the correct 0.720 on the pinned fixture.
- **Build each team term from `feature.Forecast`** and then take a min per team. **Rejected** — the
  aggregate has already folded every other team into that feature's number, which is then multiplied
  again across buckets: 0.518 on the same fixture.
- **A new `DeliveryCompletionDistribution` type carrying the delivery's histogram.** **Rejected** — it
  would have to reimplement the cross-bucket product, and a parallel implementation cannot satisfy the
  bit-identity requirement (a delivery holding one feature shared by two teams must equal that
  feature's own forecast exactly) other than by accident. Reusing `AggregatedWhenForecast` satisfies it
  by construction and inherits the flag aggregation for free.
- **Per-trial `max` within a team's bucket** — the exact intra-team combination rather than the
  comonotonic `min` proxy. **Rejected for now, deferred not refused**, on the same terms and for the
  same reason as ADR-110's cross-team per-trial max: it needs trial-level storage and touches the hot
  loop of `ForecastService`. `min` is the comonotonic upper bound — mildly optimistic, bounded, and far
  closer to honest than today's single-feature marginal.
- **Amend ADR-110 instead of filing a new ADR.** **Rejected** — ADR-110 is Accepted and shipped; its
  scope is the feature grain and its measured evidence (SPIKE-00) is about teams within a feature. A
  new grain with a new combinator and two deleted selectors is a decision in its own right. ADR-110 is
  referenced, not edited.
- **Enumerate rows as the cartesian product of the delivery's teams × features.** **Rejected** —
  `AddOrUpdateWorkForTeam` / `RemoveTeamFromFeature` make the row set genuinely sparse; a cartesian
  product injects degenerate empty CDFs for teams that do no work on a feature.

## Consequences

- **Positive**: the delivery badge and the 70/85/95 chips become honest through one seam, with no
  schema change, no DTO field, no endpoint and no migration. The last two "a representative stands for
  the whole" paths in the read model are gone, and the #5435 defect class is removed at the source —
  the delivery CDF is pointwise ≤ every feature's CDF by construction, so the only residual way a
  delivery date can land a day early is the ±1-trial rounding residue, not a mis-ranked representative.
- **The number drops and the dates move outward on upgrade**, and the recorded `DeliveryMetricSnapshot`
  trend shows a one-time step that cannot be backfilled (forward-only, ADR-048/049 — percentile *dates*
  are stored, not per-team histograms). Handled with release notes and concept docs only, exactly as
  ADR-110's own step was.
- **`FilterApplied` / `ExcludedSummary` on the delivery chips now cover every contributing team**, not
  just the governing feature's. A delivery whose *non*-governing feature had a throughput filter applied
  now surfaces that filter. Correct, and a visible delta worth naming. One cosmetic wrinkle: the
  distinct-join composes twice — within the bucket, then across buckets. Two *single-row* buckets both
  reading `"X"` collapse correctly, because `AggregatedWhenForecast` already applies `.Distinct()`; the
  case that survives needs a **multi-row** bucket, where bucket A yields `"X; Y"`, bucket B yields
  `"X"`, and the outer `Distinct()` sees two different strings, giving `"X; Y; X"`. Cosmetic, out of
  scope, named rather than silently accepted.
- **The delivery figure is an upper bound twice over.** `min` is optimistic within a team (perfect
  positive dependence is the best case for the team's last row) and cross-team independence is
  optimistic where teams share people. Both err in the same direction, so the shipped number is a
  ceiling, never a floor. Independence stays a docs-level statement: Lighthouse persists no
  person/assignee data (`Assignee`: zero hits across the backend), so shared-people correlation is not
  merely undetected — it is underivable from what is stored.
- **A delivery can now flip to "Cannot forecast" transiently after a work-item sync** (point 5). Adding
  a team to an already-forecast feature via `AddOrUpdateWorkForTeam` creates a contributing pair with no
  forecast row; the delivery says so until the next forecast run, then self-heals. Accepted cost — the
  alternative is a number that quietly assumes the new team's work is already done.
- **Cost falls by roughly half, and will be measured before the slice-01 commit.** The delivery *header*
  stops reading `feature.Forecast` — a computed property that rebuilds an entire
  `AggregatedWhenForecast` on every get, which `GetGoverningFeature` calls once per candidate feature
  and `ToWhenPercentile` re-evaluates once per percentile — and instead reads the raw persisted
  `FeatureWork` + `Forecasts` once, building one aggregate for the whole delivery. Counted inside
  `CalculateMetrics`: **≈ 2N + percentiles.Length + 1 → ≈ N + 1**, not → 1 — `CalculateFeatureBreakdown`
  still rebuilds one aggregate per feature for the breakdown rows and is deliberately untouched. No
  memoisation unless the measurement contradicts the expectation (ADR-110 point 4 declined it at
  0.113 ms p95 for 5 teams × 500 day keys).
- **Reuse verdict**: `JointCompletionDistribution` → **REUSE UNCHANGED**; `AggregatedWhenForecast` →
  **REUSE UNCHANGED** (a second call site, no edit); `WhenForecast` → **REUSE** (the `internal`
  histogram constructor added by ADR-110's story is the carrier seam — its "test seam" comment must be
  updated in the same commit, since this promotes it to a production seam);
  `Delivery.CalculateMetrics` → **EXTEND** (keeps the guards, delegates the combination);
  `Feature.TeamsWithoutForecast` → **EXTEND** (point 5 — the missing-pair clause, which also moves the
  *feature* surface and was ratified by the maintainer on that basis, 2026-07-29);
  `DeliveryMetricsProjection` → **EXTEND** (one field on a `public sealed record` that is never
  serialised); `DeliveryWithLikelihoodDto` → **EXTEND** (one selector deleted, one assignment rewired);
  `ComonotonicCompletionDistribution`, `CompletionHistogram` and `DeliveryCompletionForecast` →
  **CREATE NEW**, justified above.
- **Enforcement** (architecture rules erode without it):

  | Rule | Mechanism |
  |---|---|
  | Only the builder may reach a combinator — the wrong grain is unreachable from the entity | ArchUnitNET: `Models.Delivery` must not depend on `ComonotonicCompletionDistribution` or `JointCompletionDistribution`; only `DeliveryCompletionForecast` may. The weaker "neither combinator depends on the other" is **not** used — the grain invariant is a property of the call site, so that rule forbids only what nobody would write |
  | The delivery read path allocates no tracked entities | Integration test: `ChangeTracker.Entries<WhenForecast>()` and `<IndividualSimulationResult>()` are unchanged across a `DeliveryWithLikelihoodDto.FromDelivery` call |
  | No representative-selection helper survives in the delivery read path | Deletion is proven by compilation; the behaviour is proven by the invariant below |
  | `delivery ≤ every breakdown row` — exact on the CDFs, ±1 trial on the emitted histograms | Assert on the pre-rounding cumulative series, or on percentile days with an explicit one-day tolerance naming the largest-remainder residue. A strict day-level assertion over demo data on every CI build would flake: the residue allocation is not monotone across two different day-key grids, and near-equality is the common case |
  | Every contributing pair has a forecast row, or the delivery says so | Unit test: a `FeatureWork` with remaining work and no matching `Forecasts` row ⇒ `null` + that team named |
  | `Delivery` stays clock-free | **`CalendarDayAnchorSeamArchUnitTest`** — a plain **source scanner**, not ArchUnitNET (`:20-23` says why: `DateTime.UtcNow` is a property access on a universally-referenced type, which dependency rules cannot express). `CalculateMetrics` keeps taking `DateOnly today` as a parameter |
  | `Delivery` stays repository-free | **`BlackoutForecastShiftSeamArchUnitTest.FeatureAndDeliveryModels_DoNotDependOnRepositories`**. A `Models ↛ Services` rule *does* exist in this codebase (`RecurringBlackoutEventsSeamArchUnitTest.cs:35-37`, ADR-060) but **cannot** cover `Delivery`, which already imports `Services.Implementation` / `Services.Interfaces` and calls `InstanceCalendar.AsUtcMidnight` |

- Cross-refs [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (same reasoning, one grain
  up — the product, the largest-remainder residue and the canonical multiplication order are inherited
  verbatim), [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (provenance, applied to the
  per-team carrier), [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (D8
  preserved unchanged in substance; the `GetLikelihood` 100 % trap this must not reach),
  [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md) (the AND-across-teams sufficiency
  rule this extends across features),
  [ADR-058](./adr-058-blackout-forecast-date-shift-translation-placement.md) (day → date translation
  still runs after aggregation, unaffected), ADR-048/049 (forward-only snapshots).
