# Feature Delta — delivery-joint-likelihood

**ADO**: User Story **#5587** "Delivery likelihood reflects all features, not just the governing one"
(parent Epic **#5459** "Multi Team Forecasts")
**Waves**: DISCUSS ✅ · DESIGN ⬜ · DISTILL ⬜ · DELIVER ⬜
**Density**: lean (Tier-1 [REF] only) · `expansion_prompt = ask-intelligent`
**Feature type**: cross-cutting (backend forecast maths · frontend copy · concept docs · release-notes positioning)

---

## Wave: DISCUSS / [REF] Evidence provenance

Everything below is read from the repository, not inferred, **except** ADO #5587's
`System.Description`. This agent has no shell tool in this session (`ctx_shell` is not granted), so
`az boards work-item show --id 5587` could not be run and the work item's body could not be fetched
first-hand. The analysis in #5587 reached this wave through the maintainer's briefing, and every
factual claim it makes has been re-verified against the code below. **DESIGN should re-read #5587
directly** and flag any divergence.

Code re-verified this wave:

| Claim | Verified at |
|---|---|
| Trials are grouped by **team**, not by feature | `Services/Implementation/Forecast/ForecastService.cs` — `RunMonteCarloSimulation`: `simulationResults.GroupBy(s => s.Team)`, one `Task.Run` per team |
| Intra-team features contend and share draws | `SimulateIndividualDayForFeatureForecast` → `GetSimulationResultsOfFeatureToUpdate` picks a feature at random under `FeatureWIP` from **that team's** remaining rows |
| Rows are genuinely sparse | `InitializeSimulationResults` emits one `SimulationResult` per `FeatureWork` with `RemainingWorkItems > 0` — never a cartesian product |
| The header rollup | `Models/Delivery.cs:51` `CalculateMetrics` → `GetGoverningFeature` (likelihood **and** the 70/85/95 chips) |
| The second rollup | `API/DTO/DeliveryWithLikelihoodDto.cs:66,125` `GetLeastLikelyFeature` — filters `>= 0`, orders, takes min; since ADR-112 D8 it drives only `HasSufficientData` |
| Lighthouse persists no person data | `ctx_search "Assignee" --include=*.cs` over `Lighthouse.Backend` → **0 matches in 3 files** (grounds D4) |
| Terminology keys exist | `src/models/TerminologyKeys.ts:8,9,25` — `FEATURE`, `FEATURES`, `DELIVERY` |
| Concept page already teaches the coin analogy | `docs/concepts/howlighthouseforecasts.md` → "2 Teams - 1 Feature" + "Doing it by hand" + "When a Team Cannot Be Forecast" |

---

## Wave: DISCUSS / [REF] Persona ID

`delivery-forecaster` (`docs/product/personas/delivery-forecaster.yaml`) — the person who takes a
delivery's header number into a steering or commitment conversation and is accountable for the date.

Secondary reader, not a distinct persona here: `product-owner`
(`docs/product/personas/product-owner.yaml`), who reads the same breakdown grid to decide a scope cut
(`job-po-scope-cut-from-delivery-trend`). The scope-cut decision gets **better**, not different: the
row-vs-header gap now tells the PO which feature is actually governing.

---

## Wave: DISCUSS / [REF] JTBD

**New job**: `job-delivery-likelihood-covers-every-feature` — added to `docs/product/jobs.yaml`,
added to `delivery-forecaster.primary_jobs`.

> *When I read a delivery's likelihood and its per-feature breakdown, I want the headline number to be
> the chance that **all** of its features land by the date — not the chance that the single governing
> one does — so the number I commit to in front of leadership is not optimistic by construction, and
> so the difference between the header and the rows is something I can explain rather than something I
> have to explain away.*

Opportunity score: **importance 5 / satisfaction 1 / gap 4**. Same trust surface and same defect class
as `job-forecast-multi-team-joint-probability` (Epic 5459), one level up — that epic fixed the
feature; the delivery rollup still selects a single representative.

**Four forces** (full text in `jobs.yaml`):

- **Push** — `GetGoverningFeature` picks one feature and reports its number as the delivery's. Every
  other feature in the delivery is treated as a certainty. Invisible on screen; no workaround a user
  could construct short of redoing the maths by hand from the per-team rows.
- **Pull** — one read-side change at `Delivery.CalculateMetrics` makes the badge **and** the 70/85/95
  chips honest at once, with no schema change and no new endpoint.
- **Anxiety** — the number drops and the dates move out at the release boundary, and the recorded
  trend shows a one-time step that cannot be backfilled. A forecaster mid-commitment may read that as
  the delivery getting worse rather than the tool getting honest. Mitigated by D1 (the label says what
  the number means) and D3 (release notes + concept docs), exactly as ADR-110 D5 handled 5459's own
  step.
- **Habit** — forecasters read a delivery percentage as if it were one feature's. The new number needs
  no new ritual, but the first release will prompt "why is my delivery number lower than every row?" —
  the relabelled chip is what converts that from alarm to comprehension at a glance.

**JTBD-to-story bridge**

| Story | `job_id` | Why this job |
|---|---|---|
| US-01 joint delivery rollup | `job-delivery-likelihood-covers-every-feature` | The spine — the number itself |
| US-02 sufficiency at row grain | `job-forecast-only-with-enough-data` | Literally that job's functional dimension ("every forecast surface… portfolio delivery likelihood") one grain down |
| US-03 relabel both surfaces | `job-delivery-likelihood-covers-every-feature` | The job's social/emotional dimension — being able to explain the header-vs-row gap |
| US-04 concept docs + release notes | `job-delivery-likelihood-covers-every-feature` | The job's `habit` force names the concept-page explanation as the mitigation |

---

## Wave: DISCUSS / [REF] Evidence — two rollups, one grain error

**There are two delivery-level rollups, and the ADO note names only one.**

**Rollup 1 — `Delivery.CalculateMetrics` → `GetGoverningFeature`** (`Models/Delivery.cs:51-107`).
Selects one feature by `OrderByDescending(forecast.GetProbability(85)).ThenBy(likelihood)` and reports
*that feature's* likelihood **and** its histogram's 70/85/95 percentile dates as the delivery's. This
is the ADR-110 worst-team defect one level up: a representative stands in for the whole, and every
other feature is silently a certainty. Two features each 85 % ⇒ true ≈ 72 %, reported 85 %.

**Rollup 2 — `DeliveryWithLikelihoodDto.GetLeastLikelyFeature`** (`API/DTO/…:66,125`). Filters
`LikelihoodPercentage >= 0`, orders, takes the minimum. Since ADR-112 D8 it no longer drives the
likelihood — but it **still** drives `HasSufficientData` via
`leastLikelyFeature?.HasSufficientData ?? featureLikelihoods.All(fl => fl.HasSufficientData)`. So the
delivery's "not enough data" warning is read off **one** feature. Same defect, third instance. See D6.

**The grain that makes it correct is `(team, feature)`, not `feature`.**
`ForecastService.RunMonteCarloSimulation` groups trials by team. Two features worked by the same team
share that team's throughput draws and contend for its `FeatureWIP` — they are positively correlated.
Two features on different teams draw from independent streams. So:

- **Within a team's bucket** — comonotonic. The honest, mildly-optimistic proxy for "that team's last
  row finishes" is the elementwise **min** of the bucket's CDFs (a valid CDF, and the correct upper
  bound under perfect positive dependence).
- **Across buckets** — independent, by construction of the simulation. **Product**, exactly as
  ADR-110 already does for a feature's teams.

Multiplying *feature* CDFs is wrong and double-penalises same-team features. Building a team term out
of `feature.Forecast` (the `AggregatedWhenForecast`) is wrong in the other direction — it folds team B
into team A's term and then multiplies B again.

**Row states, enumerated from the code** (this is what any implementation must handle):

| `(team, feature)` row | In `feature.Forecasts`? | `TotalTrials` | Meaning |
|---|---|---|---|
| remaining > 0, team has throughput | yes | 10 000 | normal contributor |
| remaining > 0, team has **no** throughput | yes | **0** | cannot forecast (ADR-112) |
| remaining == 0 | **absent** — `InitializeSimulationResults` filters `RemainingWorkItems > 0` | — | done; contributes CDF ≡ 1 |
| **all** of a feature's rows remaining == 0 | one sentinel row, `Team == null`, `{0: 0}` | **0** | done; `Feature.GetLikelhoodForDate` short-circuits to 100 |

**Finding (new this wave, sharpens trap 4).** A finished row inside an unfinished shared feature is
normally **absent** from `Forecasts`, not present-with-zero-trials — so the naive failure mode is a
*missing* bucket rather than a degenerate one. But `Forecasts` is EF-persisted and only rewritten by
`SetFeatureForecasts` on the next forecast run, so a stale zero-trial row for a team whose remaining
work has since dropped to 0 **is** reachable between runs. Both shapes must resolve to CDF ≡ 1. The
exemption therefore keys off `FeatureWork.RemainingWorkItems` for that pair — **not** off the
emptiness of the forecast, and **not** off who owns it. Same rule as ADR-112's completed-feature
exemption, applied one grain down.

**Finding (new this wave, blocks a naive D6).** The whole-feature sentinel row carries
`HasSufficientData == false`: `CreateWhenForecastForSimulationResult` only copies the sufficiency flag
`if (simulationResult.Team is { } team && …)`, and the sentinel's `Team` is null, so the `bool` stays
at its default. `AggregatedWhenForecast` then ANDs it to `false`. Today `GetLeastLikelyFeature` masks
this — a finished feature sorts to likelihood 100 and is never selected unless it is the only one.
**Switching to a plain `All(…)` across features would make every delivery containing a completed
feature report "not enough data".** D6 carries the exemption to prevent exactly that.

---

## Wave: DISCUSS / [REF] Locked decisions

D1–D5 are the maintainer's, recorded verbatim in substance. D6–D11 were derived this wave; **D6 and
D12 were put back to the maintainer at wave end and ratified** (see their verdicts). D7–D11 stand as
derived.

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | **UX framing: relabel both surfaces.** The header chip becomes **"All {features} by {date}: NN%"** with an info tooltip **"P(ALL of these land by the date)"**. The breakdown grid's Likelihood column gets **"each on its own"** framing with a tooltip **"P(this one lands), ignoring the others"**. | LOCKED (maintainer, this session). Rejected: showing joint **and** marginal on the badge (two competing numbers); tooltip-only with no label change (leaves the row-vs-header mismatch unexplained at a glance). **Constraint A**: all new copy routes through `useTerminology()` / `getTerm(TERMINOLOGY_KEYS.DELIVERY \| FEATURE \| FEATURES)` — "Delivery" and "Feature" are user-renamable; never hardcode. **Constraint B**: the copy must **not** promise the header is always lower than every row — equality is legitimate (see D5). |
| **D2** | **ADR-112 D8 stands unchanged.** A delivery containing a feature that cannot be forecast is itself un-forecastable and names the teams. | LOCKED (maintainer). Rejected: exposing a forecastable-subset upper bound alongside it; replacing D8 with a subset-plus-warning. |
| **D3** | **Upgrade shock is handled with release notes + docs only.** A release-notes lead item written as positioning (customer pain → outcome win), plus extending `docs/concepts/howlighthouseforecasts.md` — which already teaches the multi-team coin analogy — one level up to deliveries. **No** in-app banner, **no** dismissible notice, **no** trend-chart annotation. | LOCKED (maintainer). Rationale: matches how ADR-110 D5 handled 5459's own one-time step; no dismissible-notice mechanism exists today and building one for a single-release message is disproportionate. |
| **D4** | **Team independence: docs only.** The assumption is already stated plainly in the concept docs post-5459 ("shared people or a hand-off make reality worse than the maths suggests"). No in-product caveat, no detector. | LOCKED (maintainer). Grounding: `ctx_search "Assignee" --include=*.cs` over the backend returns **zero hits** — Lighthouse stores no person/assignee data at all, so shared-people correlation is not merely undetected, it is **underivable from what is persisted**. Recorded here so it is not re-litigated. |
| **D5** | **Shared features decompose per team, never at feature grain.** A feature worked by teams A and B contributes one row to A's bucket and one to B's. Every `(team, feature)` row lands in exactly one bucket, so the sharing is expressed once and never re-applied. `min` operates only *within* a bucket; the product operates only *across* buckets; the two operators never touch the same pair. | LOCKED (maintainer — the correctness backbone). **Invariant**: `delivery ≤ every breakdown row`, **equality possible**. Proof in one line: team *t*'s min ≤ any row in bucket(*t*), and every other team's term ≤ 1. Equality occurs when one feature governs entirely and the other teams' rows carry slack. |
| **D6** | **`GetLeastLikelyFeature` is deleted. `HasSufficientData` becomes an AND across the delivery's contributing features — excluding features with no remaining work.** | **RATIFIED (maintainer, wave end).** Derived this wave as the answer to the brief's open question, then put back to the maintainer and confirmed. The exemption chain was re-verified independently against the code before ratification: `ForecastService.cs:141-144` builds the no-rows sentinel with the parameterless `SimulationResult()` ctor, so `Team` is null; `CreateWhenForecastForSimulationResult:156` guards on `simulationResult.Team is { } team` and therefore never assigns `HasSufficientData`, leaving the `bool` at its `false` default (`WhenForecast.cs:36`, no initializer); `AggregatedWhenForecast.cs:26` ANDs over that single row. A plain `All(…)` would regress every delivery containing a completed feature to "not enough data". See the dedicated section below. |
| **D7** | **`GetGoverningFeature` is deleted.** The 70/85/95 chips come from the joint delivery histogram, not the governing feature's. | DERIVED — a consequence of D5. The joint rollup produces a full histogram, so there is no selection step left to keep. Note it currently carries the ADO **#5435** tie-break fix; that fix is superseded structurally, not dropped — see AC-01.9. |
| **D8** | **The ADR-112 D8 un-forecastable check runs *before* the joint computation**, unchanged. | DERIVED — preserves D2 exactly. `Delivery.CalculateMetrics` already short-circuits on `Features.Any(f => !f.CanBeForecast)` before touching the governing feature; that ordering stays. |
| **D9** | **Slices 01–04 ship as one release unit.** Do not release the maths (01/02) without the relabel (03) and the docs/notes (04). | DERIVED. Slice order is a build order, not a release order. Releasing 01 alone ships a number that dropped with nothing on screen or in the notes explaining why — the precise failure D1 and D3 exist to prevent. |
| **D10** | **The row set is derived from the persisted per-team forecasts plus `FeatureWork` remaining work — never from a cartesian product of the delivery's teams × features.** | DERIVED (trap 3). `AddOrUpdateWorkForTeam` / `RemoveTeamFromFeature` make the row set genuinely sparse; a cartesian product injects degenerate empty CDFs for teams with no work on a feature. |
| **D11** | **The delivery rollup reuses `JointCompletionDistribution` for the cross-bucket product** rather than reimplementing it. | DERIVED. Forced by the canonical consistency fixture (AC-01.5): a delivery holding exactly one feature shared by two teams must be **bit-identical** to that feature's `AggregatedWhenForecast`, which pins the largest-remainder allocation and the canonical multiplication order. A parallel implementation cannot satisfy it by accident. The per-bucket `min` combinator is new and is DESIGN's to place. |
| **D12** | **The per-bucket elementwise `min` ships as the intra-team proxy. The exact form is deferred, not refused.** | **RATIFIED (maintainer, wave end).** `min` is the comonotonic upper bound on a team's own completion — mildly optimistic, bounded, and far closer to honest than today's single-feature marginal. The exact form is a per-trial max within the bucket, which needs trial-level storage and touches `ForecastService`'s hot loop; deferred for the same reason and on the same terms as ADR-110's cross-team per-trial max ("deferred, not refused: if cross-team correlation ever needs modelling, this is the door"). Consequence to state wherever the approximation is documented: **the delivery number is an upper bound twice over** — `min` is optimistic within a team, and cross-team independence (D4) is optimistic where teams share people. Both err in the same direction, so the shipped figure is a ceiling, never a floor. |

---

## Wave: DISCUSS / [REF] The `GetLeastLikelyFeature` / `HasSufficientData` question — answered

The brief asked this as a real open question. **Verdict: delete `GetLeastLikelyFeature`; make
`HasSufficientData` an AND across the delivery's contributing features, with the no-remaining-work
exemption.**

Concretely — replace

```csharp
HasSufficientData = leastLikelyFeature?.HasSufficientData ?? featureLikelihoods.All(fl => fl.HasSufficientData),
```

with an AND over the features that actually carry a forecast, i.e. those with remaining work; empty
set ⇒ `true`.

**Why AND, not least-likely-feature-derived:**

1. **Grain consistency with the change itself.** After US-01 the delivery number rests on *every*
   `(team, feature)` row. A sufficiency flag read off one representative feature is the same
   "representative stands for the whole" defect the story exists to remove — it would be the last one
   left in the delivery rollup.
2. **ADR-039's rule already says AND.** `AggregatedWhenForecast.HasSufficientData = materialized.All(…)`
   ANDs across a feature's team rows *today*. The delivery is the same aggregation one level up;
   least-likely-feature is the odd one out, not the precedent.
3. **`FeatureLikelihoodDto.HasSufficientData` is already the All-across-teams aggregate**
   (`feature.Forecast.HasSufficientData`). So All-across-features ≡ All across every `(team, feature)`
   row — exactly the row set the likelihood now uses. One grain, two signals.
4. **Direction of change is conservative.** AND can only flip `true → false`, never the reverse. It
   never newly hides a warning; it can only surface one that was previously masked.
5. **Nothing else calls it.** `ctx_search` finds `GetLeastLikelyFeature` at exactly two sites — its
   own definition and the single `FromDelivery` call. Leaving a dead "pick a representative" helper in
   a file whose whole point is that representatives are wrong is an invitation to reuse it.

**Why the exemption is not optional.** A feature with no remaining work carries the `{0: 0}` sentinel,
whose `Team` is null, so `CreateWhenForecastForSimulationResult` never copies the sufficiency flag and
the `bool` stays at its `false` default. A plain `All(…)` would therefore make **any** delivery
containing a completed feature report "not enough data" — a visible regression that today's
least-likely path masks by accident. The exemption keys off remaining work, which is the same rule as
ADR-112's completed-feature exemption and the same rule as trap 4. One rule, three applications.

**Visible behaviour delta to put in the release notes**: today, a delivery whose *least likely* feature
has sufficient data but where some *other* feature rests on thin history reports `hasSufficientData:
true` and shows no warning. After: `false`, and the "not enough data" indicator appears. That is the
right direction — the joint number genuinely rests on that thin history now — but it is a change users
will see. Covered by AC-02.4 and US-04.

**Implementation note for DESIGN (not a DISCUSS decision)**: `FeatureLikelihoodDto` does not carry
remaining work, so the exemption needs either a new row-level signal or evaluation against
`delivery.Features` rather than against the DTO list. DESIGN's call.

---

## Wave: DISCUSS / [REF] Scope assessment (Elephant Carpaccio gate)

**PASS — 4 stories, 2 bounded contexts (forecast domain + delivery presentation) plus a docs surface,
estimated 3–4 days.**

Oversized signals checked: >10 stories — no (4). >3 contexts — no (2 + docs). Walking skeleton needs
>5 integration points — N/A, no skeleton (Decision 2: brownfield, read-side change on an existing
seam; the path `throughput → ForecastService → Feature.Forecasts → Delivery.CalculateMetrics →
DeliveryWithLikelihoodDto → DeliverySection` already runs in production, and this replaces one node in
it). Effort >2 weeks — no. Multiple independent shippable outcomes — no; D9 binds all four into one
release unit.

---

## Wave: DISCUSS / [REF] Journey

**Mental model the forecaster brings.** "The delivery is done when the last feature is done, so the
delivery percentage should be the worst feature's percentage." That model is *almost* right and is
exactly why the current number passes casual inspection: the worst feature **is** a bound — it is just
an upper bound, not the answer. The forecaster's model has no slot for "and the others each have to
land too". The relabel (D1) is what installs that slot: "All {features} by {date}" names the
conjunction the model is missing.

**Vocabulary** — the delivery surface must speak one language. Today it says "Likelihood" in two
places meaning two different things. After: "All {features} by {date}" (joint, header) and
"Likelihood (each on its own)" (marginal, row). Both terms route through `useTerminology()`.

**Happy path.**

```
Portfolio → Deliveries → expand a delivery
  ┌─────────────────────────────────────────────────────────────────────┐
  │ Q3 Launch      Delivery Date: Oct 14, 2026                          │
  │ [ All features by Oct 14, 2026: 72% ]  ⓘ                            │
  │   ⓘ  P(ALL of these land by the date)                               │
  │ Forecast:  [70%: Oct 12]  [85%: Oct 21]  [95%: Nov 03]              │
  │            ^ joint histogram, not the governing feature's           │
  ├─────────────────────────────────────────────────────────────────────┤
  │ Feature            Team        Progress   Forecast   Likelihood ⓘ   │
  │                                                      (each on its own)│
  │                                            ⓘ P(this one lands), ignoring the others
  │ FTR-1 Checkout     Alpha,Beta  ██████░░    Oct 20     [ 72% ]        │
  │ FTR-2 Reporting    Beta        ████████    Oct 09     [ 95% ]        │
  └─────────────────────────────────────────────────────────────────────┘
```

Note the header (72 %) **equals** FTR-1's row here. That is the three-way fixture rendered, and it is
why D1's copy may not promise "always lower".

**Emotional arc.**

- *Start* — unsettled: "The delivery says 85 % and so does the top feature. Are the other four
  features free?"
- *Middle* — oriented: "The header says **All** features. The rows say **each on its own**. Of course
  the header is the lower one — that is what 'all' means. And when they match, I can see which single
  feature is carrying the delivery."
- *End* — defensible: "I can reproduce this by hand from the per-team numbers, the docs show me how,
  and I can say out loud what the number means without a caveat speech."

No jarring transition: the badge keeps its position, size and colour scale. Only the label, the
tooltip and the value change.

**Shared artifacts.** Every `${variable}` above has a single source:

| Variable | Source of truth |
|---|---|
| `{features}` / `{feature}` / `{delivery}` | `getTerm(TERMINOLOGY_KEYS.FEATURES \| FEATURE \| DELIVERY)` — user-renamable, never hardcoded |
| `{date}` (header chip) | `delivery.getFormattedDate()` — the same formatter as the "Delivery Date:" text beside it |
| header `NN%` | `DeliveryWithLikelihoodDto.LikelihoodPercentage` ← `Delivery.CalculateMetrics` joint histogram (US-01) |
| header 70/85/95 chips | `DeliveryWithLikelihoodDto.CompletionDates` ← the **same** joint histogram (US-01, D7) |
| row `NN%` | `FeatureLikelihoodDto.LikelihoodPercentage` ← `feature.GetLikelhoodForDate` — **unchanged** |
| "Cannot forecast" + team names | `TeamsWithoutForecast` (ADR-112) — **unchanged** (D2) |
| "Not enough data" | `DeliveryWithLikelihoodDto.HasSufficientData` — changes rule (US-02, D6) |
| recorded trend points | `DeliveryMetricSnapshot` — forward-only, one-time step (US-04) |

**Error and edge paths — all four must be answered on screen.**

| Path | Behaviour |
|---|---|
| Any feature cannot be forecast | "Cannot forecast", names the teams. **Unchanged** (D2). The joint maths is never reached. |
| All features finished | 100 % / Done, dates as today. The joint rollup must not turn "done" into "cannot forecast" — this is the ADR-110 zero-trial replay one grain down (trap 4). |
| Delivery with no features | 0 %, empty chips — as today. |
| Some features finished, others not | The finished rows drop out (CDF ≡ 1); the delivery reflects only the rows with remaining work. |

**Integration checkpoints.** (a) US-01 → US-03: the relabel is only truthful once the header is joint —
D9 binds them into one release. (b) US-02 → the existing `INSUFFICIENT_FORECAST_DATA_SHORT` rendering:
composes, does not replace (ADR-039/ADR-112 D4). (c) US-01 → `DeliveryMetricSnapshot`: the recorder
writes the new percentile dates from the day of release; no backfill (ADR-048/049).

---

## Wave: DISCUSS / [REF] Story map

**Backbone (user activities, left to right):**

`Open a delivery` → `Read the headline number` → `Compare it against the per-feature rows` →
`Explain the gap to leadership` → `Reproduce it / understand why it moved`

| Activity | Story | Slice |
|---|---|---|
| Read the headline number | US-01 — the number and the dates are the joint across all features' team rows | 01 |
| Read the headline number | US-02 — the "not enough data" warning covers every contributing feature | 02 |
| Compare against the rows · Explain the gap | US-03 — both surfaces say which probability they are | 03 |
| Understand why it moved · Reproduce it | US-04 — release notes + concept docs one level up | 04 |

**No walking skeleton** (Decision 2, brownfield). Slice 01 is itself end-to-end through an existing
path.

**Release slicing** — one release unit (D9), built in the order 01 → 02 → 03 → 04. Every slice is
≤ 1 day and carries at least one user-visible value story.

**Priority rationale.** 01 first because everything else describes its output; releasing the relabel
against the old number would be actively false. 02 second because it is the last representative-based
signal in the same file and is cheapest to fix while that code is open. 03 third because the copy must
describe the shipped number. 04 last because the release notes must describe what actually shipped,
including US-02's visible delta.

---

## Wave: DISCUSS / [REF] User stories

<!-- markdownlint-disable MD024 -->

### US-01 — A delivery's likelihood and dates reflect every feature, not the governing one

`job_id: job-delivery-likelihood-covers-every-feature`

As a **delivery-forecaster**, when a delivery holds several features, I want its headline likelihood
and its 70/85/95 dates computed from the joint distribution over every contributing `(team, feature)`
row, so the number I commit to is not optimistic by construction.

#### Elevator Pitch

**Before**: a delivery with five features shows the governing feature's 85 % and the governing
feature's dates, as though the other four were certainties.
**After**: open Portfolio → Deliveries and expand a delivery (`GET
/api/latest/deliveries/portfolio/{portfolioId}`) → the badge reads the joint probability across all
features and the 70/85/95 chips are the joint histogram's dates. In the Dependencies demo scenario the
badge drops and the chips move outward by days, and no delivery's number exceeds any of its own rows.
**Decision enabled**: whether to commit to the delivery date in the steering meeting, or cut scope now.

#### Domain examples

1. **Happy path — Maria Santos, RTE for the Q3 Launch delivery (14 Oct 2026).** Two features:
   *Checkout* (teams Alpha + Beta) and *Reporting* (Beta alone). Rows at 14 Oct:
   `Alpha/Checkout = 0.90`, `Beta/Checkout = 0.80`, `Beta/Reporting = 0.95`.
   bucket(Alpha) = min(0.90) = 0.90; bucket(Beta) = min(0.80, 0.95) = 0.80; delivery = 0.90 × 0.80 =
   **0.720**. Breakdown rows: Checkout **0.72**, Reporting **0.95**. The header equals Checkout's row.
   Maria reports 72 % and can point at Checkout as the governing feature.
2. **Edge — equality with slack.** Same delivery, but Beta's *Reporting* work closes and its row drops
   out. bucket(Beta) = min(0.80) = 0.80; delivery = 0.90 × 0.80 = 0.720, still. The finished feature
   changed nothing, which is correct — it is a fact, not a forecast.
3. **Error/boundary — Team Meridian has zero throughput** on *Checkout*. `Checkout.CanBeForecast` is
   false, so the delivery short-circuits to "Cannot forecast — no throughput history for Team
   Meridian" (ADR-112 D8, D2). The joint maths is never reached and Maria goes and fixes the data gap.

#### UAT scenarios

**Scenario: A delivery's headline number accounts for every feature**
Given Maria's Q3 Launch delivery holds Checkout (Alpha + Beta) and Reporting (Beta), with the row
probabilities 0.90 / 0.80 / 0.95 at the delivery date
When Maria opens the delivery on the Portfolio page
Then the headline number reads 72 %, and it is not greater than either the Checkout row (72 %) or the
Reporting row (95 %)

**Scenario: A feature shared by two teams is counted once**
Given a delivery holding exactly one feature that both Alpha and Beta work on
When Maria opens the delivery
Then the delivery's likelihood and its 70/85/95 dates are identical to that feature's own likelihood
and dates

**Scenario: The delivery's dates are never earlier than any feature's**
Given a delivery whose features have different 85 % dates
When Maria reads the delivery's 85 % chip
Then that date is on or after every feature's own 85 % date

**Scenario: Work a team has already finished does not drag the delivery down**
Given Alpha has finished all of its work on Checkout while Beta still has work left
When Maria opens the delivery
Then the delivery still reports a forecast (Alpha's finished work counts as certain), and it does not
read "cannot forecast"

**Scenario: A delivery whose work is entirely finished reads as done**
Given every feature in the delivery has no remaining work
When Maria opens the delivery
Then it reads 100 % / Done, exactly as it does today

**Acceptance criteria**

- **AC-01.1** *(D5 — row grain)* The delivery CDF at every day `d` is `∏_t min_{r ∈ bucket(t)} CDF_r(d)`,
  where `bucket(t)` is the set of `(t, feature)` rows in the delivery. Rows are per-team, per-feature.
- **AC-01.2** *(trap 1)* No team term is derived from `feature.Forecast` / `AggregatedWhenForecast`.
  **Discriminating fixture**: F1 shared by A+B, F2 owned by B alone; at some day
  `A/F1 = 0.90`, `B/F1 = 0.80`, `B/F2 = 0.95` ⇒ the delivery is **0.720**. A team term built from
  `feature.Forecast` gives `(0.90×0.80) × min(0.72, 0.95) = 0.518`; multiplying feature CDFs gives
  `(0.90×0.80) × 0.95 = 0.684`. All three values are distinct — the fixture kills all three grain
  traps at once.
- **AC-01.3** *(trap 2)* `min` is taken over the individual per-team rows in a bucket, never over
  feature aggregates. Covered by the same fixture (a min over feature aggregates yields 0.518).
- **AC-01.4** *(D5 invariant)* `delivery ≤ every breakdown row`, and **equality is permitted**. The
  fixture in AC-01.2 must render as delivery 0.720 with rows F1 = 0.72, F2 = 0.95 — a passing test
  must accept the equality, not assert strict inequality.
- **AC-01.5** *(canonical consistency, D11)* A delivery holding exactly **one feature shared by two
  teams** produces a likelihood, histogram and 70/85/95 dates **bit-identical** to that feature's
  `AggregatedWhenForecast`. The single-**team** version of this check is trivially true and proves
  nothing; the shared-feature version is the required fixture.
- **AC-01.6** *(trap 3, D10)* The row set is enumerated from the actual `(team, feature)` work pairs —
  never a cartesian product of the delivery's teams × features. A team with no work on a feature
  contributes no row and injects no degenerate empty CDF. Fixture: a delivery with 2 teams × 2
  features where team A works only F1 must not produce 4 rows.
- **AC-01.7** *(trap 4a)* A row whose `(team, feature)` pair has `RemainingWorkItems == 0` contributes
  CDF ≡ 1 — whether that row is **absent** from `Forecasts` (the normal case) or **present with zero
  trials** (a stale persisted row between forecast runs). The exemption keys off remaining work, not
  off the emptiness of the forecast and not off who owns it.
- **AC-01.8** *(trap 4b)* If such a row is a team's **only** row in the delivery, `bucket(team)`
  resolves to **1** — not to "cannot forecast", and not to a degenerate empty CDF. A whole-feature
  `{0: 0}` sentinel row (`Team == null`) is excluded and must never create a null-keyed bucket.
- **AC-01.9** *(D7 / ADO #5435 regression)* `GetGoverningFeature` is removed and the 70/85/95 chips
  come from the joint histogram. The #5435 symptom — a delivery's forecast dates earlier than an
  individual feature's — becomes structurally impossible, because the delivery CDF is pointwise ≤ every
  feature's CDF. Assert it directly, do not rely on the deleted tie-break.
- **AC-01.10** *(D2 preserved)* The ADR-112 D8 check runs **before** the joint computation. A delivery
  containing any feature with `CanBeForecast == false` still reports "cannot forecast" and names the
  teams; no forecastable-subset number is produced.
- **AC-01.11** A delivery with no features still reports 0 % and empty completion dates, as today.
- **AC-01.12** No schema change, no new DTO field, no new endpoint. `LikelihoodPercentage` and
  `CompletionDates` carry different values on the same contract.

---

### US-02 — The "not enough data" warning covers every contributing feature

`job_id: job-forecast-only-with-enough-data`

As a **delivery-forecaster**, when any feature in a delivery rests on thin throughput history, I want
the delivery to say so, so I do not present a joint number whose weakest input is invisible.

#### Elevator Pitch

**Before**: `HasSufficientData` is read off the single least-likely feature. A delivery whose *other*
features rest on three days of history shows no warning at all.
**After**: open Portfolio → Deliveries → a delivery containing Team Equinox's thin-data feature shows
the "not enough data" indicator on the header, regardless of which feature happens to be least likely.
In the Dependencies demo scenario this makes the thin-data case visible on the delivery, not only on
the feature row.
**Decision enabled**: wait for more history before quoting the delivery number, or quote it with the
caveat the tool is now making for you.

#### Domain examples

1. **Happy path — Maria's Q3 Launch.** Checkout (60 %, 90 days of history) and Reporting (95 %, Team
   Equinox, three active days). Today: least-likely is Checkout, which has sufficient data ⇒ **no
   warning**. After: AND across both ⇒ **warning shown**.
2. **Edge — a completed feature must not trigger it.** Q3 Launch also holds *Legacy Migration*, fully
   done. Its `{0: 0}` sentinel carries `HasSufficientData == false` by default. The exemption keys off
   remaining work, so the delivery is judged only on Checkout and Reporting.
3. **Error/boundary — every feature is done.** All rows exempt, the AND is over an empty set ⇒
   `true`. The delivery reads 100 % / Done with no spurious data warning.

#### UAT scenarios

**Scenario: A thin-data feature raises the warning even when it is not the least likely one**
Given Maria's delivery holds Checkout at 60 % on 90 days of history and Reporting at 95 % on three
days of history
When Maria opens the delivery
Then the delivery shows the "not enough data" indicator

**Scenario: A finished feature does not raise a false warning**
Given Maria's delivery holds one finished feature and one feature with ample history
When Maria opens the delivery
Then no "not enough data" indicator is shown

**Scenario: A delivery whose work is entirely finished shows no data warning**
Given every feature in the delivery has no remaining work
When Maria opens the delivery
Then it reads 100 % / Done with no "not enough data" indicator

**Acceptance criteria**

- **AC-02.1** *(D6)* `DeliveryWithLikelihoodDto.HasSufficientData` is the logical AND of
  `HasSufficientData` across the delivery's features **that have remaining work**. An empty set yields
  `true`.
- **AC-02.2** *(D6 exemption)* A feature with no remaining work is excluded from the AND. Regression
  fixture: a delivery holding one completed feature and one well-supported feature reports
  `hasSufficientData: true`. Without the exemption this returns `false`, because the whole-feature
  `{0: 0}` sentinel carries the `bool` default.
- **AC-02.3** *(D6)* `GetLeastLikelyFeature` is deleted; no delivery-level signal is derived from a
  single representative feature.
- **AC-02.4** *(visible delta, must reach the release notes)* A delivery whose least-likely feature has
  sufficient data but where another feature does not now reports `false` where it previously reported
  `true`. This is intentional.
- **AC-02.5** The unknown-forecast state (ADR-112) and the sufficiency signal continue to **compose**:
  a delivery can be both un-forecastable and insufficient; the "cannot forecast" label wins on screen,
  as today.
- **AC-02.6** The existing `INSUFFICIENT_FORECAST_DATA_SHORT` rendering on `DeliverySection` is reused
  unchanged — no new indicator, no new colour.

---

### US-03 — Both surfaces say which probability they are showing

`job_id: job-delivery-likelihood-covers-every-feature`

As a **delivery-forecaster**, when the delivery's headline number is lower than every feature row
beneath it, I want each surface to state which probability it is, so I can explain the gap at a glance
instead of being asked to justify an apparent inconsistency.

#### Elevator Pitch

**Before**: the header chip reads `Likelihood: 72%` and the grid column reads `Likelihood`, showing
95 % on a row — two numbers with the same word and no way to tell they answer different questions.
**After**: open Portfolio → Deliveries → the header chip reads **"All features by Oct 14, 2026: 72%"**
with an info tooltip **"P(ALL of these land by the date)"**, and the grid column reads **"Likelihood
(each on its own)"** with a tooltip **"P(this one lands), ignoring the others"**.
**Decision enabled**: answer "why is the delivery lower than every feature?" in the meeting itself,
without opening the docs.

#### Domain examples

1. **Happy path — Maria in a steering review.** Header "All features by Oct 14, 2026: 72%"; a row
   shows 95 %. A director asks about the gap; Maria hovers the header tooltip and reads it aloud.
2. **Edge — a renamed vocabulary.** An org has renamed "Feature" to "Epic" and "Delivery" to "Release
   Train". The header must read "All epics by Oct 14, 2026: 72%" — the copy is built from
   `getTerm(TERMINOLOGY_KEYS.FEATURES)`, never a literal.
3. **Error/boundary — equality.** The three-way fixture renders header 72 % with rows 72 % and 95 %.
   The copy must remain true when the header equals a row — it says "All", it does not say "lower than
   any of these".

#### UAT scenarios

**Scenario: The header states that it covers all features**
Given Maria opens a delivery whose headline number is 72 %
When she reads the delivery header
Then it reads "All features by {the delivery date}: 72%" and an info tooltip explains "P(ALL of these
land by the date)"

**Scenario: The breakdown column states that it ignores the other features**
Given Maria expands the delivery's feature grid
When she reads the Likelihood column header
Then it carries "each on its own" framing and a tooltip explaining "P(this one lands), ignoring the
others"

**Scenario: Renamed vocabulary flows into the new copy**
Given an administrator has renamed "Feature" to "Epic"
When Maria opens the delivery
Then the header reads "All epics by {the delivery date}: 72%"

**Scenario: The copy still reads correctly when the header equals a row**
Given a delivery whose headline number is 72 % and whose highest-risk feature row is also 72 %
When Maria reads both
Then neither the label nor the tooltip claims the header is lower than every row

**Scenario: The cannot-forecast state keeps its own message**
Given a delivery containing a feature that cannot be forecast
When Maria reads the delivery header
Then it reads "Cannot forecast" with the existing tooltip naming the teams — not the "All features by
…" framing

**Acceptance criteria**

- **AC-03.1** *(D1)* The header chip label in the numeric state is `All {featuresTerm} by {formatted
  delivery date}: NN%`, with an info affordance whose tooltip reads `P(ALL of these land by the date)`.
- **AC-03.2** *(D1)* The breakdown grid's Likelihood column header carries "each on its own" framing
  with a tooltip reading `P(this one lands), ignoring the others`.
- **AC-03.3** *(D1 Constraint A)* Every new user-facing string that names a domain object is built
  from `useTerminology()` / `getTerm(TERMINOLOGY_KEYS.DELIVERY | FEATURE | FEATURES)`. No new
  hardcoded "Delivery" / "Feature" / "Features" literal is introduced. Verified by the renamed-
  vocabulary scenario, not by inspection alone.
- **AC-03.4** *(D1 Constraint B)* Neither the label nor either tooltip asserts that the header is lower
  than every row. Equality is legitimate.
- **AC-03.5** The non-numeric header states are unaffected: `CANNOT_FORECAST_SHORT` keeps its label
  and its `cannotForecastReason(teamsWithoutForecast)` tooltip; `INSUFFICIENT_FORECAST_DATA_SHORT`
  keeps its label. The "All … by …" framing applies to the numeric state only.
- **AC-03.6** The existing per-row `FeatureLikelihoodChip` tooltip (cannot-forecast, naming teams) is
  not clobbered by the new column-header tooltip; the two coexist.
- **AC-03.7** The header chip's position, size and colour scale (`ForecastLevel`) are unchanged — only
  the label text, the tooltip and the value change.
- **AC-03.8** The date rendered inside the header label uses the same formatter as the "Delivery Date:"
  text beside it, so the two never disagree.

---

### US-04 — A reader can find out why the number moved, and reproduce it by hand

`job_id: job-delivery-likelihood-covers-every-feature`

As a **delivery-forecaster** upgrading Lighthouse, when my delivery numbers drop and my percentile
dates move outward on the same day, I want the release notes to tell me why and the concept page to
let me reproduce the new number from the per-team figures, so I read the change as the tool getting
honest rather than the delivery getting worse.

#### Elevator Pitch

**Before**: after upgrading, a delivery's badge drops, its 70/85/95 chips move outward, its recorded
trend shows a step, and nothing anywhere explains it — the reader's only hypothesis is "the delivery
got worse".
**After**: the release notes lead with the change written as positioning, and
`docs/concepts/howlighthouseforecasts.md` gains a delivery-level worked example alongside the existing
"2 Teams - 1 Feature / Doing it by hand" section, so a reader can reproduce the 0.90 × 0.80 = 72 %
from their own per-team rows in a spreadsheet.
**Decision enabled**: trust the new number and re-baseline the commitment, instead of opening a
support issue or reverting the upgrade.

#### Domain examples

1. **Happy path — Maria upgrades on a Monday.** Q3 Launch drops from 85 % to 72 %; the 85 % chip moves
   from 14 Oct to 21 Oct. She reads the release-notes lead item, follows the link to the concept page,
   reproduces 72 % from Alpha's 0.90 and Beta's 0.80, and re-baselines.
2. **Edge — the recorded trend step.** Maria opens the delivery's Metrics tab. The likelihood line
   steps down at the release boundary. The release notes say the recorded history is forward-only
   (ADR-048/049) and cannot be backfilled, so the step is a change of method, not of delivery health.
3. **Error/boundary — the sufficiency delta.** Maria's *other* delivery gains a "not enough data"
   indicator it never had, because its thin-data feature was never the least likely one. The release
   notes name this as its own bullet (AC-02.4), so it is not mistaken for a new defect.

#### UAT scenarios

**Scenario: The release notes lead with the customer pain and the outcome win**
Given a reader opens the release notes for the version carrying this change
When they read the lead item
Then it states that a delivery's likelihood and dates previously reflected only its governing feature,
and now reflect all of them, and names the three visible consequences: the number drops, the
percentile dates move outward, and the recorded trend shows a one-time step that cannot be backfilled

**Scenario: The sufficiency change is called out separately**
Given the same release notes
When the reader looks for why a delivery gained a "not enough data" indicator
Then a distinct bullet explains that the warning now covers every contributing feature rather than the
least likely one

**Scenario: A reader can reproduce a delivery number by hand**
Given a reader opens the forecasting concept page
When they follow the delivery-level worked example
Then they can compute the delivery percentage from their own per-team, per-feature figures and arrive
at the number Lighthouse displays, rounded

**Scenario: The independence assumption is stated at delivery grain**
Given a reader reaches the end of the delivery-level section
When they look for the caveat
Then the page states that teams are assumed independent and that shared people or a hand-off make
reality worse than the maths suggests

**Acceptance criteria**

- **AC-04.1** *(D3)* A release-notes lead item exists, written as positioning (customer pain → outcome
  win, per house style), covering: the delivery number drops; the 70/85/95 chips move outward; the
  recorded `DeliveryMetricSnapshot` trend shows a one-time step that **cannot** be backfilled
  (forward-only, ADR-048/049).
- **AC-04.2** *(D6, AC-02.4)* The sufficiency-signal change is a separate release-notes bullet.
- **AC-04.3** *(D3)* `docs/concepts/howlighthouseforecasts.md` gains a delivery-level scenario
  alongside the existing "2 Teams - 1 Feature" / "Doing it by hand" walkthrough, using the same coin
  framing, with a worked example a reader can reproduce in a spreadsheet from per-team rows.
- **AC-04.4** *(D5, teaching the grain)* The concept page explains that the decomposition is per team
  per feature, that a feature shared by two teams contributes one row to each, and that this is why a
  shared feature is not penalised twice.
- **AC-04.5** *(D4)* The independence assumption is restated at delivery grain, in the same plain terms
  the page already uses post-5459. **No in-product caveat and no detector** — the backend persists no
  person data (`Assignee`: 0 hits), so the correlation is underivable, not merely undetected.
- **AC-04.6** *(D1 Constraint B, in prose)* The docs must not claim the delivery number is always lower
  than every feature; they must show the equality case, which is the same three-way fixture.
- **AC-04.7** *(D3)* No in-app banner, no dismissible notice, no trend-chart annotation is added.

---

## Wave: DISCUSS / [REF] Trap → AC index

| # | Trap | Failure mode | Closed by |
|---|---|---|---|
| 1 | Team term built from `feature.Forecast` (`AggregatedWhenForecast`) instead of `feature.Forecasts.Where(team == t)` | Folds B's risk into A's term, then multiplies B again — 0.518 instead of 0.720 | **AC-01.2** |
| 2 | `min` taken over feature aggregates per team rather than over per-team rows | Same double-count in different clothing | **AC-01.3** (same fixture) |
| 3 | Row enumeration by cartesian product (teams × features) | Injects degenerate empty CDFs for teams with no work on a feature; `AddOrUpdateWorkForTeam` / `RemoveTeamFromFeature` make the row set genuinely sparse | **AC-01.6**, D10 |
| 4 | A finished row inside an unfinished shared feature | Row-level replay of the Epic 5459 zero-trial bug — "done" rendering as "cannot forecast" or as a degenerate CDF | **AC-01.7** (absent *or* stale-zero-trial row ⇒ CDF ≡ 1), **AC-01.8** (sole row ⇒ bucket = 1, never a null-keyed bucket) |

Cross-cutting kill shot: the three-way fixture in AC-01.2 produces **three distinct values** (0.720 /
0.684 / 0.518) from one input, so it discriminates traps 1 and 2 and the feature-grain product
simultaneously. The bit-identity fixture in AC-01.5 uses a **shared** feature deliberately — the
single-team version is trivially true and proves nothing.

---

## Wave: DISCUSS / [REF] Cross-cutting verdicts

No silent N/A — every item answered.

- **RBAC — N/A, because** the change alters *what number* an existing surface renders, not who may read
  it. No new operation, no `IRbacAdministrationService` interaction, no new UI gate, no change to
  `useRbac()`. Every caller who can read a delivery today continues to, and reads the same fields.
  The premium gate on the delivery Metrics tab is untouched.
- **Lighthouse-Clients (CLI/MCP) — no version gate required, because** the change is read-side and
  adds no DTO field. `lighthouse_delivery_list` forwards the delivery payload verbatim
  (`packages/mcp-core/src/index.ts` → `encodePayload`), and `listDeliveries` is typed
  `Promise<LighthouseApiResult<readonly unknown[]>>` — the clients do not deserialise or render a
  likelihood to a human. `LikelihoodPercentage`, `CompletionDates` and `HasSufficientData` all already
  exist and keep their types (`LikelihoodPercentage` is already nullable per ADR-112). No
  `FEATURE_REQUIRES_SERVER_NEWER_THAN` gate applies. **Verdict: no clients release needed for this
  feature.** Values change, contracts do not — the same conclusion ADR-112's client audit reached, and
  it should be re-confirmed rather than re-derived at DELIVER.
- **Website marketing surface — N/A, because** no new capability, screen or headline claim is
  introduced; this corrects an existing number. The marketing pages do not quote a delivery
  likelihood. **In scope for `docs/`**, which the website hot-links from `Lighthouse@main/docs/` via
  jsDelivr: the concept-page edit (AC-04.3–04.6) is live on letpeople.work as soon as it merges, so it
  must be complete and self-consistent at merge time, not "finished later".
- **Recorded history — one-time step, no backfill.** `DeliveryMetricSnapshot` is forward-only
  (ADR-048/049) and stores percentile *dates*; recomputation would need per-snapshot historical
  throughput that is not retained. Same situation as ADR-110 D5, one level up. **Must appear in the
  release notes** (AC-04.1). No schema change, no migration.
- **Percentile chips move too — stated explicitly.** `delivery.completionDates` currently comes from
  the governing feature's histogram and will come from the joint histogram (D7). Dates move outward,
  not just the badge. Recorded in the journey, in AC-01.9, and in the release notes (AC-04.1). This is
  the single most under-communicated consequence of the change: a reader watching only the badge will
  be surprised by the dates.
- **Demo data — no change required, but verify.** The premium **Dependencies** scenario already
  demonstrates joint / thin-data / cannot-forecast / finished side by side (Epic 5459). It is the
  natural fixture for the visible-delta checks in US-01 and US-02; DELIVER should confirm it exercises
  a shared feature across two teams within one delivery, and add one if it does not.
- **EF migrations — N/A, because** the change is read-side only (S2). No entity, no column, no
  `CreateMigration` run.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Measurement honesty first: Lighthouse is self-hosted with **no phone-home telemetry** (#5015), so no
KPI here may depend on usage data from customer instances. Every measurement below is either a CI
assertion, a maintainer-run check, or public issue triage.

| Who | Does what | By how much | Measured by | Baseline |
|---|---|---|---|---|
| Every delivery rendered by Lighthouse | Reports a likelihood no greater than any of its own feature rows, and dates no earlier than any of its features' | **0 violations** | Backend integration assertion over the Dependencies demo scenario, run on every CI build | Unknown today — the invariant is not asserted anywhere, and `GetGoverningFeature` can violate it (ADO #5435 was that symptom) |
| A forecaster reading a multi-feature delivery | Can state what the headline number means without opening the docs | Both surfaces carry the framing and the tooltip; renamed-vocabulary scenario passes | Frontend test + maintainer walkthrough on the demo instance before release | 0 — today both surfaces say "Likelihood" and mean different things |
| A reader of the concept page | Reproduces a displayed delivery percentage by hand from per-team rows | Reproduces to the displayed rounded value | Maintainer walks the new worked example end to end against the running demo instance before release; a mismatch blocks the release | The page teaches this at *feature* grain only (post-5459); at delivery grain it is 0 |
| A forecaster upgrading | Attributes a dropped delivery number to the method change rather than to delivery health | Every post-release question about a moved delivery number is answerable by linking the release-notes item or the concept section, with no bespoke explanation needed | GitHub/community issue triage for 90 days after release; count of questions answered by link vs. by bespoke explanation | Epic 5459's equivalent step produced no such issues, which sets the bar rather than the baseline — the delivery surface is more leadership-facing, so expect more |
| A forecaster whose delivery rests on thin history | Sees the "not enough data" indicator on the delivery, not only on the feature row | Indicator present in 100 % of deliveries containing a thin-data feature with remaining work; **0** false positives on deliveries containing a completed feature | Backend + frontend tests (AC-02.1, AC-02.2), plus the Dependencies scenario | Today the indicator fires only when the *least likely* feature is the thin one — an unmeasured fraction, and provably 0 in the Dependencies scenario's shape |

---

## Wave: DISCUSS / [REF] Out of scope

- Changing the Monte Carlo, the throughput sampling, the filter modes, or the blackout day-shift
  translation.
- Per-trial max across teams, or modelling cross-team correlation (ADR-110's deferred door).
- Modelling **intra**-team correlation more exactly than the comonotonic `min` proxy. `min` is the
  honest upper bound under perfect positive dependence; a per-trial max within a team's bucket would
  be exact and is deferred for the same reason ADR-110 deferred it.
- Any schema change, new DTO field, new endpoint, or migration.
- Changing per-feature likelihood or per-feature dates — the breakdown rows are marginals and stay
  marginals (that is the point of D1).
- Changing ADR-112 D8 (D2), including any forecastable-subset upper bound.
- In-app upgrade messaging of any kind (D3).
- An in-product independence caveat or a shared-people detector (D4).
- Backfilling `DeliveryMetricSnapshot` history.
- Filing the standing `ForecastBase.GetLikelihood` `return 100` ticket — still open from Epic 5459,
  still deliberately untouched, still wants its own ticket.

---

## Wave: DISCUSS / [REF] DoR validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Problem statement clear, domain language | **PASS** | Two rollups documented from code with file:line; the grain error stated in forecasting vocabulary, not implementation terms |
| 2 | User/persona with specific characteristics | **PASS** | `delivery-forecaster` (existing SSOT persona); secondary `product-owner`; Maria Santos carries the examples |
| 3 | 3+ domain examples with real data | **PASS** | Each story carries 3 examples with named people, named teams (Alpha/Beta/Equinox/Meridian), named features and concrete probabilities (0.90 / 0.80 / 0.95 → 0.720) |
| 4 | UAT in Given/When/Then (3–7 scenarios) | **PASS** | US-01: 5 · US-02: 3 · US-03: 5 · US-04: 4. All titled by business outcome, no implementation titles |
| 5 | AC derived from UAT | **PASS** | 12 / 6 / 8 / 7 ACs, each traceable to a scenario or to a named trap; the four traps are indexed to ACs |
| 6 | Right-sized (1–3 days, 3–7 scenarios) | **PASS** | 4 stories, one slice each, ≤ 1 day each; 3–5 scenarios each. US-01 is the largest at 12 ACs but a single seam and a single day |
| 7 | Technical notes: constraints/dependencies | **PASS** | Row-state table; the `FeatureLikelihoodDto` remaining-work gap flagged for DESIGN; D11 bit-identity constrains the combinator; `min` combinator placement left to DESIGN |
| 8 | Dependencies resolved or tracked | **PASS with one tracked gap** | Slice order 01→02→03→04, release-bound by D9. **Tracked gap**: ADO #5587's `System.Description` could not be fetched (no shell tool this session) — recorded in the provenance section; DESIGN must re-read it and flag divergence. Not blocking: every claim was independently re-verified against code |
| 9 | Outcome KPIs with measurable targets | **PASS** | Five KPIs, each with a measurement method that exists without telemetry (CI assertion, maintainer walkthrough, issue triage), and an honest baseline including "unknown today" where that is the truth |

**Verdict: DoR PASS (9/9).** Ready for DESIGN.

---

## Wave: DISCUSS / [REF] Open questions for DESIGN

1. **Where does the per-bucket `min` combinator live?** `JointCompletionDistribution` is `internal
   static` and multiplies. A `Min` sibling there is the obvious home, but the two operators must stay
   visibly distinct (D5: they never touch the same pair). DESIGN owns the placement and the
   trial-count/rounding rule for the min result — constrained by AC-01.5's bit-identity requirement.
2. **How does the sufficiency exemption reach the DTO?** `FeatureLikelihoodDto` carries no remaining
   work. Either add a row-level signal or evaluate the AND against `delivery.Features`. DESIGN's call;
   AC-02.1/02.2 constrain the behaviour, not the shape.
3. **Cost.** ADR-110 D-point 4 declined memoisation on a measured 0.113 ms p95 for 5 teams × 500 day
   keys. The delivery rollup adds a second pass over the same day-key union per delivery, and
   `Feature.Forecast` is still a computed property rebuilt on every get. DESIGN should measure before
   deciding, and note that this change reads `feature.Forecasts` (the raw rows) rather than
   `feature.Forecast` (the aggregate) — which may make the delivery path *cheaper*, not dearer.
4. **Does an ADR get filed?** This extends ADR-110's reasoning to a new grain and supersedes
   `GetGoverningFeature`. DESIGN decides whether that is a new ADR or an amendment; DISCUSS does not
   write ADRs.
