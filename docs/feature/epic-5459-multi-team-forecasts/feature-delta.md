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

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDR | Impact |
|--------|------------|-----|--------|
| DESIGN#DDD-1 | The joint maths lives in the pure `JointCompletionDistribution`, not in the `AggregatedWhenForecast` ctor | DDR-1 | Tier-1 and Tier-2 tests address the collaborator directly with hand-built histograms — no EF entity, no simulation, no sampling error in the arithmetic tests |
| DESIGN#DDD-2 | Largest-remainder residue rule lives in the collaborator | DDR-2 | One exact test pins the tie-break (earlier day wins), because a tie-break left implicit is a mutation-testing hole |
| DESIGN#DDD-3 | Zero-trial contributors are filtered out of the product in this slice | DDR-3 | Two tests: filtered from the histogram, still counted for flags and provenance. Slice-02 replaces the filter |
| DESIGN#DDD-4 | Provenance: `Team`/`TeamId` null, `NumberOfItems` summed, `CreationTime` oldest | DDR-4 | Three provenance tests; the sum and the oldest date are taken over **all** contributors, including zero-trial ones |
| DESIGN#DDD-5 | Test seam = `internal` ctor on `WhenForecast` | DDR-5 | The `BindingFlags.NonPublic` reflection is gone from `AggregatedWhenForecastTest`; ~a dozen hand-built histograms now read as data |
| DISCUSS#AC-01.4 | Single-contributor output identical to today | n/a | Green before the change and after it — a regression guard, deliberately not a RED test |
| SPIKE-00#Finding-6 | Constant-throughput fixtures cannot discriminate old from new | n/a | Every discriminating fixture is multi-valued; the one constant-throughput test is labelled a plumbing anchor in its own comment |

## Wave: DISTILL / [REF] Scenario list with tags

Executable SSOT: the NUnit test files listed under *Test placement*. This repository has no Gherkin
layer — `Given/When/Then` lives in the Arrange/Act/Assert shape of the test bodies, and the tags below
are documentation, not attributes.

| Scenario (test) | Tags | Tier |
|---|---|---|
| `Combine_TwoTeamsWithIdenticalTwoValueHistograms_ProducesTheExactJointHistogram` | `@AC-01.1` `@AC-01.5` `@in-memory` | 1 |
| `Combine_SingleContributor_ReturnsThatHistogramUnchanged` | `@AC-01.4` `@in-memory` | 1 |
| `Combine_ContributorFinishedBeforeTheUnionMaximum_KeepsItsProbabilityAtOneBeyondItsLastDay` | `@AC-01.1` `@in-memory` `@edge` | 1 |
| `Combine_ScaledBucketsLeaveAResidue_AssignsItByLargestRemainderPreferringTheEarlierDay` | `@AC-01.5` `@in-memory` `@edge` | 1 |
| `Combine_ContributorWithoutTrials_IsExcludedFromTheProduct` | `@DDD-3` `@in-memory` `@edge` | 1 |
| `Combine_EveryContributorWithoutTrials_ReturnsAnEmptyHistogram` | `@DDD-3` `@in-memory` `@error` | 1 |
| `Combine_NoContributors_ReturnsAnEmptyHistogram` | `@in-memory` `@error` | 1 |
| `Combine_CrossingContributors_IsIndependentOfInputOrder` | `@AC-01.6` `@in-memory` | 1 |
| `Combine_RandomContributors_SumsToThePreservedTotalTrials` | `@AC-01.5` `@property` `@in-memory` | 2 |
| `Combine_RandomContributors_JointProbabilityNeverExceedsAnyContributorProbability` | `@AC-01.2` `@property` `@in-memory` | 2 |
| `Combine_RepeatedInvocationOnTheSameInput_ProducesTheSameHistogram` | `@AC-01.5` `@property` `@in-memory` | 2 |
| `GetProbability_TwoTeamsWithMassAtTheSelectedTeamsDate_IsStrictlyLaterThanThatTeam` | `@AC-01.3` `@in-memory` | 1 |
| `GetProbability_CrossingContributors_IsAtLeastEveryContributorsSamePercentile` | `@AC-01.2` `@in-memory` | 1 |
| `SingleContributor_HistogramAndPercentilesAreIdenticalToThatContributor` | `@AC-01.4` `@regression-guard` | 1 |
| `InputOrder_CrossingContributors_DoesNotChangeTheResult` | `@AC-01.6` `@in-memory` | 1 |
| `Provenance_AggregateOfMultipleTeams_CarriesNoTeamIdentity` | `@ADR-111` `@in-memory` | 1 |
| `Provenance_NumberOfItems_IsTheSumOfAllContributors` | `@ADR-111` `@in-memory` | 1 |
| `Provenance_CreationTime_IsTheOldestContributor` | `@ADR-111` `@in-memory` | 1 |
| `NoContributors_ProducesAnEmptyForecast` | `@in-memory` `@error` | 1 |
| `ContributorWithoutTrials_IsExcludedFromTheMathsButStillCountsForProvenance` | `@DDD-3` `@ADR-111` `@edge` | 1 |
| `HasSufficientData_*`, `FilterApplied_*`, `ExcludedSummary_*` (5, unchanged) | `@AC-01.7` `@regression-guard` | 1 |
| `FeatureForecast_TwoTeamsWithTwoValueThroughput_IsLaterThanEveryContributingTeam` | `@AC-01.2` `@AC-01.3` `@real-io` | 3 |
| `FeatureForecast_ConstantThroughputTeams_MatchesTheSlowestTeam` | `@plumbing-anchor` `@real-io` | 4 |
| `GetDelivery_MultiTeamFeature_LikelihoodIsTheJointProbabilityNotTheWorstTeams` | `@AC-01.8` `@walking_skeleton` `@driving_adapter` `@real-io` | 3 |

**Coverage of US-01**: AC-01.1 ✓, AC-01.2 ✓, AC-01.3 ✓, AC-01.4 ✓, AC-01.5 ✓, AC-01.6 ✓, AC-01.7 ✓
(pre-existing, unchanged), AC-01.8 ✓. US-02 is slice-02 and is deliberately untouched here.

## Wave: DISTILL / [REF] Test placement

| File | Change | Precedent |
|---|---|---|
| `Lighthouse.Backend.Tests/Models/Forecast/JointCompletionDistributionTest.cs` | NEW | sits beside `WhenForecastTest` / `HowManyForecastTest`, mirroring `Models/Forecast/` |
| `Lighthouse.Backend.Tests/Models/Forecast/AggregatedWhenForecastTest.cs` | EXTEND | the five existing flag tests stay byte-identical in behaviour; only the reflection helper changed |
| `Lighthouse.Backend.Tests/Services/Implementation/Forecast/MultiTeamJointForecastTest.cs` | NEW | same mock shape as `ForecastServiceTest` (`NotSoRandomNumberService` / `RandomNumberService`, `ITeamMetricsService` mock); kept separate rather than growing that 610-line file |
| `Lighthouse.Backend.Tests/API/Integration/MultiTeamJointForecastDeliveryIntegrationTest.cs` | NEW | modelled on `BlackoutForecastShiftDeliveryIntegrationTest`, including its `SeedPortfolioWithMultiTeamForecastedFeature` shape (DESIGN open question 3) |

## Wave: DISTILL / [REF] Scaffolds

| File | Marker | State |
|---|---|---|
| `Lighthouse.Backend/Models/Forecast/JointCompletionDistribution.cs` | `// __SCAFFOLD__` | `internal static Combine(IEnumerable<IReadOnlyDictionary<int,int>>)` throwing `InvalidOperationException` — DELIVER replaces the body |
| `Lighthouse.Backend/Models/Forecast/WhenForecast.cs` | none | the DDD-5 `internal` ctor is real, not a scaffold: it is the test seam and has no behaviour to implement |

`InvalidOperationException` rather than `NotImplementedException` — the latter is a SonarQube smell
(S3717) and would fail the quality gate on a new file. Either way NUnit reports a failing test, never
a broken suite. Detection: `grep -rn "__SCAFFOLD__" Lighthouse.Backend/Lighthouse.Backend/` must return
zero hits when DELIVER is done.

`AggregatedWhenForecast` itself was **not** touched — its public API already carries every observable
the tests assert on, so DELIVER changes only the ctor body.

## Wave: DISTILL / [REF] Driving adapter + adapter coverage

| Driven adapter | `@real-io` scenario | Covered by |
|---|---|---|
| *(none added)* | n/a | Per DESIGN, this slice adds no driven port and no persistence — `AggregatedWhenForecast` is computed on read |
| EF-persisted `Feature.Forecasts` (existing) | YES | `MultiTeamJointForecastDeliveryIntegrationTest` round-trips through the repository and `WebApplicationFactory` |

| Driving port (DESIGN) | Scenario |
|---|---|
| `GET /api/latest/deliveries/portfolio/{portfolioId}` | `GetDelivery_MultiTeamFeature_LikelihoodIsTheJointProbabilityNotTheWorstTeams` (real HTTP, real status check) |
| `GET /api/latest/portfolios/{id}` (`FeatureDto.Forecasts`) | NOT covered end-to-end — the percentile-date surface is asserted at `Feature.Forecast` in Tier 3 instead. Adding a second HTTP scenario for the same computed property would duplicate the walking skeleton; flagged for DELIVER to reconsider only if the DTO assembly is touched |
| `GET /api/latest/deliveries/{deliveryId}/metrics-history` | N/A — one-time step (DISCUSS D5), no forecast maths in the path |
| MCP / CLI tools | N/A — they read the DTOs above, shapes unchanged |

## Wave: DISTILL / [REF] Pre-requisites

- SPIKE-00 accepted (gate D8/1) — done 2026-07-27.
- `InternalsVisibleTo("Lighthouse.Backend.Tests")` at `Lighthouse.Backend.csproj:64` — already present, DDD-5 costs no plumbing.
- No DEVOPS artefacts for this feature: default environment applies (in-process `WebApplicationFactory` + EF InMemory, as every other `API/Integration` test). Logged as a warning, not a block.
- Multi-team data for AC-01.8 (DESIGN open question 3): resolved inside the test by seeding two teams with identical two-value histograms, so no demo-data dependency remains for the backend suite. The Playwright check in gate 3 still needs seeded multi-team data.

## Wave: DISTILL / [REF] Decisions taken in DISTILL

| ID | Decision | Rationale |
|----|----------|-----------|
| DT-1 | `Combine` takes `IEnumerable<IReadOnlyDictionary<int,int>>` and returns `Dictionary<int,int>` | Keeps the collaborator decoupled from `WhenForecast` per DDD-1; `SortedDictionary` already satisfies the input type |
| DT-2 | The preserved `TotalTrials` is the **maximum** over usable contributors | Loses no precision when contributors were simulated with different trial counts, and is exact for the single-contributor identity |
| DT-3 | Residue tie-break: equal fractional parts resolve to the **earlier day** | Deterministic and testable; an unspecified tie-break survives mutation |
| DT-4 | Zero-count buckets are omitted from the output histogram | Otherwise the joint histogram carries days with no mass; AC-01.4 identity is asserted against the contributor's own non-zero buckets |
| DT-5 | Provenance sums and the oldest `CreationTime` are taken over **all** contributors, zero-trial ones included | DDD-3 filters the *product*, not the bookkeeping; a team with no throughput still contributes items and staleness |
| DT-6 | No property-testing library introduced (DESIGN open question 2) | Three seeded loops over `Random(5569)` express the invariants; adding FsCheck for three tests would be a dependency for its own sake |
| DT-7 | Tier-3 asserts percentile **days**, never probabilities | 10 000 trials give σ ≈ 0.5 %; the chosen fixture keeps every asserted day ≥ 10σ from its boundary |

## Wave: DISTILL / [REF] Upstream notes (back-propagation)

1. **AC-01.3 wording** — "non-zero probability mass at the old reported p85" is not the precise
   condition; the aggregate p85 moves when the *other* contributors' CDFs at that day are below 1, so
   the product falls under 0.85. The test uses the exact fixture ({1:5000, 2:2500, 3:2500} twice, p50
   `1` → `2`) and asserts at p50, where the shift is exact on paper. No change requested to the AC —
   noted so nobody reads the strictness claim as universal.
2. **AC-01.2 is not a discriminator** — it is satisfied by today's `MaxBy` code and stays green
   throughout. It constrains the new maths; it does not prove it. AC-01.1/01.3/01.8 are the tests that
   fail today.
3. **`FeatureDto.LastUpdated` moves earlier** for multi-team features (DDD-4). No test asserts the DTO
   field itself — `Provenance_CreationTime_IsTheOldestContributor` covers the source. If DELIVER finds
   a consumer that treats `LastUpdated` as a staleness alarm, that belongs to a follow-up, not here.

## Wave: DISTILL / [REF] Review gate

The four-reviewer Final Wave Review Gate was **not** dispatched. DISCUSS and DESIGN were reviewed and
accepted with the maintainer on 2026-07-27 and are pushed; this epic runs under an explicit maintainer
gate (D8/2 — line-by-line diff review before every commit), which supersedes agent review for the
production diff. Not a silent skip: re-run `/nw-review` against `feature-delta.md` if a second opinion
on the DISTILL sections is wanted before DELIVER.

RED classification for the hand-off: `distill-red-classification.md` (18 RED, all
`MISSING_FUNCTIONALITY`, 0 broken; 10 regression guards green).
