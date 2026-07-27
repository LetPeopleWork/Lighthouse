# Feature Delta — epic-5459-multi-team-forecasts

**ADO**: Epic #5459 "Multi Team Forecasts" (state `Active`, tag `Community`) — stories #5568 (spike,
Resolved), #5569 (joint probability), #5570 (unknown forecast)
**Waves**: DISCUSS ✅ · SPIKE-00 ✅ · DESIGN ✅ · DEVOPS ⬜ · DISTILL ⬜ · DELIVER ⬜
**Density**: lean (Tier-1 [REF] only) · `expansion_prompt = ask-intelligent`

---

## Wave: DISCUSS / [REF] Persona ID

`delivery-forecaster` (`docs/product/personas/delivery-forecaster.yaml`) — the person who takes a
portfolio forecast into a steering or commitment conversation and is accountable for the date.

Secondary reader, not a distinct persona for this epic: `product-owner` reading the same feature
columns to decide scope cuts.

---

## Wave: DISCUSS / [REF] JTBD one-liner

`job-forecast-multi-team-joint-probability` — *When I forecast a feature that several teams must each
finish before it is done, I want the date and likelihood to reflect the chance that ALL of those
teams are finished — not the chance that the single slowest one is — so the date I commit to is not
optimistic by construction.*

Opportunity score: importance 5 / satisfaction 1 / **gap 4** — the highest-gap job currently open in
the forecasting domain.

---

## Wave: DISCUSS / [REF] Evidence — the two defects

Read directly from the code, not inferred.

**Defect A — optimistic bias (this is the epic's reported problem).**
`Lighthouse.Backend/Models/Forecast/AggregatedWhenForecast.cs:12-20`:

```csharp
var worstCase = materialized.MaxBy(f => f.GetProbability(85));
if (worstCase != null)
{
    SetSimulationResult(new Dictionary<int, int>(worstCase.SimulationResult));
    ...
}
```

The feature's forecast IS one team's histogram. A feature is complete only when every contributing
team is complete, so the honest CDF is `P(max(D₁..Dₙ) ≤ d)`. Under the independence the simulation
already assumes, that is `∏ᵢ CDFᵢ(d)`, which is **strictly ≤** the reported `CDF_worst(d)` whenever a
second team has probability mass below `d`. Two teams each 85 % by day 30 ⇒ true ≈ 72 %, reported
85 %. The bias compounds with team count and is invisible on screen.

**Defect B — percentile crossing (not in the ADO note; found in DISCUSS).**
The "worst" team is selected by `GetProbability(85)` *only*. Team distributions cross — a
high-variance team can be worst at p95 but not at p85 — so the reported p50 / p70 / p95 may be read
off a team that is not the worst at that percentile. The single-team-copy design is wrong even
before the joint-probability fix. **D1 removes the selection step entirely, so A and B are fixed by
the same change.**

**Hazard C — the 100 % trap.** `ForecastBase.GetLikelihood` (`:70-95`) returns `100` when
`trialCounter == 0`. A contributing team with no usable throughput never simulates
(`ForecastService.cs:112` filters on `throughputByTeam.ContainsKey`), yet `UpdateFeatureForecasts`
(`:130-146`) still constructs a `WhenForecast` for it — empty histogram, `TotalTrials == 0`. Under
today's `MaxBy` that loses harmlessly (`GetProbability` returns `-1`). Under D3's unknown-forecast
rule, an unhandled empty aggregate would render as **"100 % likely to hit the date"** — the exact
inverse of "unknown". Slice-02 exists to close this.

**Hazard D — trial normalisation.** `GetProbability` and `GetLikelihood` both divide by
`TotalTrials`. A product-of-CDFs yields a real-valued PMF; it must be re-emitted as an integer
histogram summing to the preserved trial count, with a deterministic residue rule (D6).

---

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | Joint distribution = **product-of-CDFs**, computed post-hoc in `AggregatedWhenForecast`. `CDF_f(d) = ∏ᵢ CDFᵢ(d)`; `PMF_f(d) = CDF_f(d) − CDF_f(d−1)`. | LOCKED (user, 2026-07-27). Chosen over rewriting the Monte Carlo to a per-trial max: identical result under the independence `ForecastService` already assumes (one `Task.Run` per team, independent draws), at a fraction of the risk to the core hot loop. Per-trial max deferred, not rejected. |
| **D2** | The `MaxBy(p85)` worst-team selection is **removed**, not re-ranked. | LOCKED — consequence of D1; also fixes Defect B. |
| **D3** | A contributing team with no usable throughput ⇒ the feature forecast is **UNKNOWN**: no dates, no likelihood. Not "exclude the team and show a partial forecast". | LOCKED (user, 2026-07-27). Companion rule: the likelihood path must be suppressed **explicitly**, because `GetLikelihood` falls through to `100` on an empty histogram (Hazard C). |
| **D4** | Single-contributor features must produce **byte-identical** output to today. | LOCKED — explicit regression guard, not an incidental property. Most features are single-team. |
| **D5** | Recorded `DeliveryMetricSnapshot` history: **accept the one-time step, document it.** | LOCKED (user, 2026-07-27). Snapshots are forward-only (ADR-048/049) and store percentile *dates*, not per-team histograms — recomputation would need per-snapshot historical throughput that is not retained. |
| **D6** | Trial count preserved; fractional PMF mass resolved by **largest-remainder allocation** (deterministic, no RNG). | LOCKED — required for `GetProbability` / `GetLikelihood` to keep working. DESIGN owns exact placement. |
| **D7** | Computation lives in `AggregatedWhenForecast`. `ForecastService` and the Monte Carlo loop are **untouched**. | LOCKED. DESIGN note: `Feature.Forecast` is a computed property rebuilt on every get, read 2-3× per feature per request (`FeatureDto.cs:17,22`; `DeliveryWithLikelihoodDto.cs:151,153,160`). Copying a dict was cheap; a product over the union of day keys is not. DESIGN decides memoisation. |
| **D8** | **Two gates before commit**: a numeric SPIKE quantifying the change on real portfolio data, and a line-by-line diff review. | LOCKED (user, 2026-07-27) — forecasting is the core functionality. No production code is written before the spike numbers are read and accepted. |

---

## Wave: DISCUSS / [REF] User stories

### US-01 — Multi-team feature forecast reflects every contributing team

`job_id: job-forecast-multi-team-joint-probability`

As a **delivery-forecaster**, when a feature needs work from more than one team, I want its
percentile dates and target-date likelihood computed from the joint probability that all of those
teams are finished, so that the date I commit to is not optimistic by construction.

#### Elevator Pitch
Before: a feature needing three teams shows the slowest team's dates, as though the other two teams
were certainties.
After: open `GET /api/latest/portfolios/{id}` (portfolio detail → feature forecast columns) → the
feature's 50/70/85/95 dates are the joint distribution across all contributing teams, and land on or
after today's values.
Decision enabled: whether to commit to that date in the steering meeting, or pull scope now.

**Acceptance criteria**

- **AC-01.1** For a feature with ≥2 contributing teams that each have usable throughput, the
  aggregate CDF at every day `d` equals `∏ᵢ CDFᵢ(d)` within the tolerance implied by the integer
  trial count.
- **AC-01.2** The aggregate p50/p70/p85/p95 are each **≥** the corresponding percentile of every
  contributing team (the joint date can never be earlier than any single team's).
- **AC-01.3** Strictness: when at least two contributing teams have non-zero probability mass at the
  old reported p85, the new p85 is **strictly later** than the old one. (Distinguishes the fix from a
  no-op.)
- **AC-01.4** *(D4 regression guard)* For a feature with exactly one contributing team, the
  aggregate histogram, `TotalTrials`, and all four percentile dates are **identical** to that team's
  `WhenForecast`.
- **AC-01.5** *(D6)* The aggregate histogram sums to exactly the preserved `TotalTrials`, and the
  same input produces the same output on repeated aggregation (no RNG, no drift).
- **AC-01.6** No percentile is read from a selected team: given two teams whose CDFs cross between
  p50 and p95, the aggregate result is unchanged by the order of the input collection.
- **AC-01.7** `FilterApplied`, `ExcludedSummary`, and `HasSufficientData` aggregate exactly as today
  (Any / distinct-join / All) — regression-covered by the existing `AggregatedWhenForecastTest`.
- **AC-01.8** `GET /api/latest/deliveries/portfolio/{portfolioId}` returns a per-feature
  `LikelihoodPercentage` for a multi-team feature that is ≤ the likelihood computed from any single
  contributing team.

---

### US-02 — A feature that cannot be honestly forecast says so, instead of saying 100 %

`job_id: job-forecast-multi-team-joint-probability`

As a **delivery-forecaster**, when one of the teams a feature depends on has no usable throughput
history, I want Lighthouse to tell me the feature cannot be forecast, so that I go and fix the data
gap instead of anchoring on a number that silently ignored that team.

#### Elevator Pitch
Before: a feature whose second team has no throughput still shows dates from the first team — and its
delivery likelihood reads **100 %**, because the empty-histogram path falls through to `return 100`.
After: open the portfolio's delivery section → that feature's likelihood renders as an explicit
"cannot forecast — no throughput history for {team}" state and its date columns are empty.
Decision enabled: chase the missing throughput for that team before committing to any date.

**Acceptance criteria**

- **AC-02.1** When any contributing team's `WhenForecast` has `TotalTrials == 0`, the aggregate
  reports **no** percentile dates (`Forecasts` empty on `FeatureDto`).
- **AC-02.2** *(Hazard C — the sharp edge)* In that same case `GetLikelhoodForDate` /
  `FeatureLikelihoodDto.LikelihoodPercentage` must **not** be 100. The unknown state is carried
  explicitly; falling through to `ForecastBase.GetLikelihood`'s `trialCounter == 0 → return 100`
  branch is a test failure.
- **AC-02.3** A feature with **no remaining work** is unaffected — it is a fact, not a forecast, and
  still reads 100 % / Done (mirrors the `forecast-minimum-data-guard` D4 exemption).
- **AC-02.4** `HasSufficientData` continues to report `false` in this case; the unknown state
  composes with, and does not replace, the existing insufficient-data signal.
- **AC-02.5** The frontend renders the unknown state without error where `forecasts` is empty
  (`ForecastInfoList.tsx:25` already tolerates `forecasts ?? []`), and the likelihood cell shows the
  unknown state rather than a numeric percentage.
- **AC-02.6** The message names the team(s) that could not be forecast.

---

## Wave: DISCUSS / [REF] Out of scope

- Rewriting the Monte Carlo to take a per-trial max across teams (D1 — deferred, not rejected).
- Modelling correlation **between** teams (shared people, shared dependencies). Not modelled today;
  this epic does not introduce it. The independence assumption is inherited, and stated openly.
- Team-level manual When / How-Many forecasts — single-team by definition, unaffected.
- Throughput sampling, filter modes, and the blackout day-shift translation — unchanged.
- Backfilling or recomputing recorded delivery-metric history (D5 — impossible from stored data).
- Per-team weighting, or a user-configurable aggregation strategy.
- A new DTO contract or endpoint. Slice-01 changes values on existing fields only.

---

## Wave: DISCUSS / [REF] WS strategy

**Strategy B — brownfield extension of an existing end-to-end path. No new walking skeleton.**

The full path already runs in production: throughput → `ForecastService` Monte Carlo →
`AggregatedWhenForecast` → `FeatureDto` / `FeatureLikelihoodDto` → portfolio UI → MCP/CLI. Slice-01
replaces exactly one node in that path (`AggregatedWhenForecast`), so end-to-end value is reachable
in a single slice without scaffolding. The risk is *numerical*, not structural — which is why the
gate in front of it is a numeric spike (D8), not a skeleton.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Port | Change |
|---------|------|--------|
| Portfolio detail — feature forecast columns | `GET /api/latest/portfolios/{portfolioId}` → `FeatureDto.Forecasts` (50/70/85/95) | Values only (US-01); may be empty (US-02) |
| Portfolio delivery section — likelihood + completion dates | `GET /api/latest/deliveries/portfolio/{portfolioId}` → `FeatureLikelihoodDto` | Values only (US-01); unknown state (US-02) |
| Delivery trend over time | `GET /api/latest/deliveries/{deliveryId}/metrics-history` | One-time step at release boundary (D5) |
| MCP / CLI | `lighthouse_delivery_list`, `lighthouse_portfolio_get` | Same DTO fields, more conservative values |

No new endpoint. No new field in slice-01.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| K1 — Reported likelihood never exceeds the joint probability | 100 % of multi-team cases | Property test over generated team-histogram sets: `aggregate.GetLikelihood(d) ≤ min over teams` for all `d` |
| K2 — Single-team regression | 0 features change | Backend test asserting identical histogram + all four percentiles for one-contributor features |
| K3 — No false 100 % | 0 occurrences | Test: contributing team with `TotalTrials == 0` ⇒ likelihood is the unknown state, never `100` |
| K4 — Bias magnitude, quantified | Reported, not thresholded | SPIKE-00 output: per-feature Δdays at p85, old vs new, on real portfolio data, bucketed by team count |
| K5 — Aggregate build cost | p95 ≤ 5 ms per `Feature.Forecast` get at 5 teams × 500 distinct days | Benchmark in the spike; informs D7 memoisation call |

---

## Wave: DISCUSS / [REF] Pre-requisites

- No dependency on an unshipped wave or feature.
- Composes with `forecast-minimum-data-guard` (`HasSufficientData`, AND-across-teams) and
  `forecast-confidence-cap` — both already shipped; neither is modified.
- Composes with `blackout-day-forecast-shift` — the day→date translation runs **after** aggregation
  (`WhenForecastDto`), so it is unaffected by the change of which histogram it reads.
- Real portfolio data with multi-team features is required for SPIKE-00 (demo data alone would
  prove plumbing, not magnitude).

---

## Wave: DISCUSS / [REF] Slices

| # | Slice | Type | Value | Est. |
|---|-------|------|-------|------|
| 00 | `spike-00-quantify-multi-team-bias` | SPIKE (gate) | none shipped — produces the numbers that authorise slice-01 | ≤ ½ d |
| 01 | `slice-01-joint-probability-aggregation` | value | US-01 — honest multi-team dates and likelihood | ≤ 1 d |
| 02 | `slice-02-unknown-forecast-not-hundred` | value | US-02 — unknown instead of a false 100 % | ≤ 1 d |

Briefs: `docs/feature/epic-5459-multi-team-forecasts/slices/`.

**Carpaccio taste tests** — all pass:
- No slice ships 4+ new components (slice-01 changes one class; slice-02 adds one carried state).
- No slice depends on a new abstraction.
- Each slice disproves a pre-commitment: slice-01 disproves "the bias is negligible"; slice-02
  disproves "an empty forecast degrades safely".
- Neither value slice runs on synthetic data only — SPIKE-00 and slice-01's acceptance both require
  real portfolio data.
- No two slices differ only by scale.
- Every slice contains a user-visible value story (slice-composition hard gate satisfied; SPIKE-00 is
  a gate, not a shipped slice).

**Execution order**: 00 → 01 → 02. SPIKE-00 first by learning leverage (it can invalidate the whole
approach at ½ day's cost). Slice-02 after 01 because the unknown-forecast rule is only reachable once
the aggregate stops selecting a single team.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Product-of-CDFs aggregation implemented in `AggregatedWhenForecast`; `MaxBy(p85)` selection gone.
2. Unknown-forecast state carried explicitly; no path reaches `GetLikelihood`'s `return 100` on an
   empty histogram.
3. Backend `dotnet build` zero warnings; `dotnet test` green.
4. Frontend `pnpm test` green; `pnpm build` zero errors/warnings; Biome clean.
5. Mutation testing ≥ 80 % on the changed backend surface (`AggregatedWhenForecast`, and any touched
   part of `ForecastBase`).
6. Playwright E2E run locally before commit — portfolio detail + delivery section with a multi-team
   feature.
7. SonarCloud: no new issues.
8. Docs updated per-feature: forecasting concept page explains joint-probability aggregation; the
   D5 trend step is documented; screenshots regenerated if the delivery/feature surfaces changed.
9. **D8 gates honoured**: SPIKE-00 numbers reviewed and accepted by the maintainer, and the
   implementation diff reviewed line-by-line, before any commit.

---

## Wave: DISCUSS / [REF] Definition of Ready — validation

| # | Item | Verdict | Evidence |
|---|------|---------|----------|
| 1 | Job traceability | PASS | Both stories carry `job_id: job-forecast-multi-team-joint-probability`, added to `docs/product/jobs.yaml` |
| 2 | Persona identified | PASS | `delivery-forecaster` (existing SSOT persona) |
| 3 | Journey mapped | PASS | `docs/product/journeys/epic-5459-multi-team-forecasts.yaml` |
| 4 | Acceptance criteria testable | PASS | 8 ACs on US-01, 6 on US-02; each is a mechanical assertion over a histogram, a DTO field, or a rendered state |
| 5 | Elevator pitch per value story | PASS | Both stories; both name a real endpoint and a concrete observable output |
| 6 | Slices ≤ 1 day, end-to-end | PASS | 2 value slices + 1 spike gate; taste tests recorded above |
| 7 | Outcome KPIs with numeric targets | PASS | K1-K5; K4 is deliberately a *reported* number, not a threshold — its purpose is the D8 gate |
| 8 | Out-of-scope explicit | PASS | 7 items, including the deferred per-trial-max approach |
| 9 | Cross-cutting checklist answered, no silent N/A | PASS | RBAC = N/A with reason; clients = impacted (behavioural, no gate needed in slice-01; DESIGN confirms human-rendered likelihood for slice-02); website marketing = N/A, `docs/` concept page IN scope. See journey `cross_cutting`. |

**Scope assessment: PASS.** 2 user stories, 1 bounded context (forecasting), 1 seam, ≤ 2 days of
crafter work. No oversized signal fires.

---

## Wave: DISCUSS / [REF] Risks carried into DESIGN

1. **Independence is assumed, not verified.** `∏ᵢ CDFᵢ(d)` is exact only if team completion times are
   independent. The simulation already models them that way, so the change makes the *reported*
   number consistent with the *model*. It does not make the model match reality where teams share
   people. Stated openly in the concept docs; not solved here.
2. **`Feature.Forecast` recompute cost** (D7) — measured in SPIKE-00 (K5), decided in DESIGN.
3. **Dates move out on upgrade.** Every multi-team forecast becomes more conservative in one release.
   Release-notes framing is a deliverable, not an afterthought.

---

## Wave: DESIGN / [REF] DDD list

Scope: **application / components** (`@nw-solution-architect` remit). Mode: **guide** — decisions
taken with the maintainer on 2026-07-27, not proposed unilaterally.

| ID | Decision | Verdict |
|----|----------|---------|
| **DDD-1** | Joint distribution computed as product-of-CDFs in a **dedicated pure collaborator**, `JointCompletionDistribution` (histograms in, histogram out) — not inline in the `AggregatedWhenForecast` constructor. | LOCKED. A ctor doing both distribution maths and flag aggregation is reachable in tests only by constructing the EF-mapped entity, which drags persistence into every arithmetic test and blunts mutation testing against the ≥ 80 % gate. |
| **DDD-2** | The **largest-remainder residue rule lives in that collaborator** (answers the DESIGN handoff's open question 3). | LOCKED. Floor each scaled bucket, hand remaining units to the largest fractional parts. Deterministic, no RNG. SPIKE-00 verified 50/50 histograms sum exactly to `TotalTrials`. |
| **DDD-3** | Story 5569 **filters zero-trial contributors out of the product**; Story 5570 replaces that filter with the explicit unknown state. | LOCKED. Behaviour-preserving — today `MaxBy` already discards a zero-trial forecast (`GetProbability` returns `-1`), so 5569 stays a pure maths change and each story is reviewable in isolation. Closes a genuine gap in the original slice boundary, which said "keep today's handling unchanged" without a mechanism to do so once `MaxBy` was gone. |
| **DDD-4** | Aggregate provenance: `Team`/`TeamId` = **null**, `NumberOfItems` = **sum** of contributors, `CreationTime` = **oldest** contributor. | LOCKED (ADR-111). Consumer check found the first three are write-only on the aggregate; `CreationTime` is not — it surfaces as `FeatureDto.LastUpdated`, and oldest errs conservative so a fresh team cannot mask a stale one. |
| **DDD-5** | Test seam = **`internal` constructor** on `WhenForecast` taking a histogram. | LOCKED. `InternalsVisibleTo("Lighthouse.Backend.Tests")` already exists in `Lighthouse.Backend.csproj:64`, so this costs no plumbing; production never needs it (`AggregatedWhenForecast` is a subclass and calls `protected SetSimulationResult` directly), so the seam stays test-scoped and the public API is unchanged. Replaces the `BindingFlags.NonPublic` reflection call in `AggregatedWhenForecastTest`. |
| **DDD-6** | **No memoisation** of `Feature.Forecast`; **no bypass** of the `IndividualSimulationResult` allocation. | LOCKED. SPIKE-00 K5: 0.113 ms p95 at 5 teams × 500 day keys, 44× under budget, allocation included. Both alternatives would be speculative optimisation on the core forecasting path. |
| **DDD-7** | `ForecastBase.GetLikelihood`'s `return 100` branch is **left alone** in this epic. | LOCKED. It is reachable from single-team paths too, so changing it here would alter behaviour outside the epic without its own tests. The aggregate must not *reach* it (Story 5570 / ADR-112). Separate ticket. |

## Wave: DESIGN / [REF] Component decomposition

| Component | Path | Change |
|---|---|---|
| `JointCompletionDistribution` | `Models/Forecast/` | **CREATE NEW** — pure, no EF state |
| `AggregatedWhenForecast` | `Models/Forecast/AggregatedWhenForecast.cs` | **EXTEND** — `MaxBy` deleted; flags unchanged; ADR-111 provenance applied |
| `WhenForecast` | `Models/Forecast/WhenForecast.cs` | **EXTEND** — `internal` histogram ctor (DDD-5) |
| `AggregatedWhenForecastTest` | `Lighthouse.Backend.Tests/Models/Forecast/` | **EXTEND** — reflection replaced; four existing flag tests keep passing unchanged |
| `Feature.GetLikelhoodForDate` | `Models/Feature.cs` | **EXTEND** — Story 5570 only |
| `ForecastService`, `ForecastBase` | — | **UNCHANGED**, deliberately (DDD-6, DDD-7) |

## Wave: DESIGN / [REF] Driving ports

Unchanged from DISCUSS — no new endpoint, no new field in Story 5569. `GET /api/latest/portfolios/{id}`
(`FeatureDto.Forecasts`), `GET /api/latest/deliveries/portfolio/{portfolioId}` (`FeatureLikelihoodDto`),
`GET /api/latest/deliveries/{deliveryId}/metrics-history` (one-time step, D5), and the MCP/CLI tools
that read those DTOs. Values move; shapes do not.

## Wave: DESIGN / [REF] Driven ports + adapters

None added. The change is a pure in-memory computation between the persisted per-team `Feature.Forecasts`
(EF, `LighthouseAppContext.cs:185-189`) and the DTO assembly. `AggregatedWhenForecast` is never
persisted — it is constructed on read by the computed `Feature.Forecast` property — so no migration,
no repository change, no new adapter.

## Wave: DESIGN / [REF] Technology choices

No new dependency. C# .NET 10, existing NUnit 4.6 + Moq test stack. The property tests in Tier 2 of the
test strategy use the repository's existing patterns; no property-testing library is introduced unless
DISTILL finds the invariants awkward to express by hand — flagged as an open question rather than
pre-decided.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `AggregatedWhenForecast` | `Models/Forecast/AggregatedWhenForecast.cs` | Combines per-team forecasts into a feature forecast — exactly this feature's job | **EXTEND** | The seam already exists and already owns flag aggregation. Replacing the selection with an aggregation is ~40 LOC in place vs. a parallel type nothing would call. |
| `ForecastBase` | `Models/Forecast/ForecastBase.cs` | Holds a histogram, derives percentiles and likelihood from it | **EXTEND (no change)** | Reads a *single* histogram. Hosting a multi-histogram combination would add a second responsibility to a persisted base class shared by every per-team forecast. The new collaborator takes that instead. |
| `WhenForecast` | `Models/Forecast/WhenForecast.cs` | Already the type the aggregate subclasses | **EXTEND** | Only an `internal` test ctor is added (DDD-5). No shape change. |
| `ForecastService` | `Services/Implementation/Forecast/ForecastService.cs` | Produces the per-team histograms | **EXTEND (no change)** | Its output is already the correct input; only the combination was wrong. Touching the hot loop would be risk without benefit (ADR-110 rejected alternative). |
| `JointCompletionDistribution` | *(new)* | — | **CREATE NEW** | No existing type combines probability distributions. Justified by DDD-1: purity is what makes the maths unit- and mutation-testable, which the ≥ 80 % gate requires. |

**Outcome collision check**: skipped — `docs/product/outcomes/registry.yaml` does not exist in this
repository, so there is no contract registry to collide against.

## Wave: DESIGN / [REF] Open questions

1. **ADR-112's DTO carrier shape** (Story 5570): nullable `LikelihoodPercentage` vs a companion
   `CanBeForecast` flag. Deliberately left open — it depends on a CLI/MCP client check that belongs to
   5570, not 5569. ADR-112 is filed as **Proposed** for this reason.
2. **Property-test expression** (DISTILL): whether the Tier-2 invariants are clearer hand-rolled or
   warrant a property-testing library. No library is introduced pre-emptively.
3. **Multi-team test data for AC-01.8** (DELIVER): the live instance has one team (SPIKE-00 AC-S0.2),
   so the end-to-end likelihood check needs seeded or demo multi-team data.
   `BlackoutForecastShiftDeliveryIntegrationTest.SeedPortfolioWithMultiTeamForecastedFeature` already
   builds a two-team forecasted feature and is the obvious starting point.
4. **`ForecastBase.GetLikelihood`'s `return 100`** — out of scope here (DDD-7), wants its own ticket.
