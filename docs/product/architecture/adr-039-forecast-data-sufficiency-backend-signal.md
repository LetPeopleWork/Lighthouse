# ADR-039: Forecast data-sufficiency guard is a backend-computed signal on the forecast DTOs, not a frontend heuristic

## Status

Accepted — 2026-05-31 (DESIGN wave, feature `forecast-minimum-data-guard`, ADO #5125)

## Context

DISCUSS locked the rule for "Don't Forecast with too little Data" (feature `forecast-minimum-data-guard`):

- **D1** — sufficiency = the count of **distinct days with ≥1 completed item** in the team's throughput window must be **≥ 5**. The datapoint is a day; only days that had a completion count.
- **D2** — below the threshold the forecast output is **suppressed** and an explicit "not enough throughput data yet (need ≥5 days with completed items)" state is shown.
- **D3** — applies to **all** forecast surfaces (manual When + How-Many, portfolio delivery likelihood, per-feature likelihood).
- **D4** — genuinely completed items (no remaining work) are **exempt** — they still read `100%`/Done; the guard fires only where a probabilistic forecast is computed from throughput.
- **D5** — the threshold is a **fixed constant (5)**, not per-team configurable.
- **D6** — sufficiency is evaluated on the throughput series the forecast **actually samples** (after the empty-filtered-sample fallback of `filter-forecast-throughput`).
- **D7** (this ADR) — **where does the sufficiency signal live** so every forecast surface suppresses consistently and the CLI/MCP clients do not drift?

### Decisive grounding read (signal availability per surface)

| Concern | Source of truth | Available at the FE call site today? |
|---|---|---|
| Likelihood number | `ManualForecastDto.Likelihood` / `DeliveryWithLikelihoodDto.LikelihoodPercentage` / `FeatureLikelihoodDto.LikelihoodPercentage` | Yes (props/models) |
| Remaining-work (for D4) | `remainingItems` / `delivery.remainingWork` / `row.getRemainingWorkForFeature()` | **Yes** — same signal ADR-038's cap already sources locally |
| **Active-days count (for D1)** | `RunChartData.WorkItemsPerUnitOfTime` (backend only) | **No** — the per-day throughput buckets are never sent to the frontend |

**The critical finding — and the sharp contrast with ADR-038:** the confidence cap could live entirely in the frontend because its D4 signal (`hasRemainingWork`) was already at every call site. This feature's D1 signal (days-with-throughput) is **not** on the wire and is expensive to put there (it is per-day throughput detail). The natural owner of the sufficiency decision is therefore the **backend**, where the throughput already lives.

### The single choke point

Every forecast path traverses one method — `ITeamMetricsService.GetForecastThroughputStatus(team, mode)`:

- Manual When → `ForecastService.InitializeThroughputPerTeam` (line 98)
- Manual How-Many → `ForecastController.RunManualForecastAsync` (line 101)
- Portfolio deliveries → `ForecastService.UpdateForecastsForPortfolio` → `ForecastFeatures` → `InitializeThroughputPerTeam`

`GetForecastThroughputStatus` already returns forecast-suitability signals (`FilterApplied`, `ExcludedSummary`) and already carries the resolved post-fallback throughput. It is the correct, single home for the sufficiency decision (honours D6 for free).

### The existing carrier chain

A forecast-quality signal already flows `ForecastThroughputStatus → WhenForecast.{FilterApplied,ExcludedSummary} → ManualForecastDto / (feature.Forecast →) DeliveryWithLikelihoodDto`. The sufficiency flag rides the same rails — no new transport, just an additive field on each carrier.

## Decision

**Option A — a backend-computed boolean signal carried on the existing forecast DTOs; the frontend branches to a suppressed-state indicator.**

1. **`RunChartData.DaysWithThroughput`** (new pure accessor): `WorkItemsPerUnitOfTime.Count(kvp => kvp.Value.Count > 0)`. (Note: `RunChartData.History` is the *total* window length — all day-indices the Monte Carlo samples — **not** the active-day count; the new accessor is required.)
2. **`ForecastDataSufficiencyPolicy.HasEnoughData(RunChartData)`** (new pure static, single rule SSOT): holds `const int MinimumActiveDays = 5` and returns `throughput.DaysWithThroughput >= MinimumActiveDays`.
3. **`ForecastThroughputStatus.HasSufficientData`** (new record member, default `true`) — stamped once in `GetForecastThroughputStatus` against the resolved throughput.
4. **`WhenForecast.HasSufficientData`** (new field) — copied from the status in `ForecastService.CreateWhenForecastForSimulationResult`, beside the existing `FilterApplied`/`ExcludedSummary` copy.
5. **Simulation gate** — `InitializeThroughputPerTeam`'s `if (status.Throughput.Total > 0)` becomes `if (status.HasSufficientData)` (which subsumes `Total > 0`, since ≥5 active days ⇒ `Total ≥ 5`). An insufficient team is excluded from the simulation, so no misleading number is computed.
6. **DTO fields** — additive `bool HasSufficientData` on `ManualForecastDto`, `FeatureLikelihoodDto`, and `DeliveryWithLikelihoodDto`. The controller stamps the manual DTO (both When and How-Many paths, skipping the computation when insufficient); `DeliveryWithLikelihoodDto.FromDelivery` reads it from `feature.Forecast.HasSufficientData`.
7. **Frontend** — a pure predicate `isForecastDataInsufficient({ hasRemainingWork, hasSufficientData })` (= `hasRemainingWork && !hasSufficientData`) and a shared presentational `InsufficientForecastDataIndicator`. Each of the four surfaces (`ForecastLikelihood`, `DeliveriesChips`, `DeliverySection` header chip + per-feature column) branches: insufficient → render the indicator; otherwise render the likelihood exactly as today (`formatLikelihood` + `ForecastLevel`, so the ADR-038 cap still applies).

**D4 composition:** the suppression predicate gates on `hasRemainingWork`, the *same* local signal the cap uses — so a completed item (`hasRemainingWork === false`) is never suppressed and still reads `100%`/Done, even if its team is data-thin.

**Message ownership:** the boolean is the contract; the frontend owns the user-facing copy (and the tooltip naming the ≥5-day rule). The threshold number lives in the backend constant (policy SSOT) and, deliberately, in the FE copy (presentation) — the same "rule expressed twice on purpose" stance as ADR-038, guarded by a test on each side and de-risked by D5 (the constant is fixed).

## Alternatives Considered

### Option B — reuse the existing `ExcludedSummary` string channel

Set `ExcludedSummary = "Not enough throughput data…"` and have the frontend detect the insufficiency by inspecting the string.

- **Rejected.** It overloads one channel with two orthogonal concepts — *filter excluded all throughput* (a warning that still shows a number) versus *too little data* (suppress the number). Both can be true at once. Driving a behavioural switch (warn-vs-suppress) off a human-readable string is a fragile contract that couples the FE to backend copy. A typed boolean states the intent unambiguously.

### Option C — frontend-only heuristic

Send the per-day throughput buckets to every forecast surface and compute days-with-throughput in the browser.

- **Rejected.** The buckets are not on the wire today; putting them there is a *larger* contract change than one boolean and ships per-day throughput detail to surfaces that only render a chip. It also duplicates a backend-owned forecast policy in a presentation layer that has no other reason to know throughput internals. (This is the mirror image of ADR-038, where the FE-only choice was correct precisely because its signal was already local.)

## Consequences

**Positive**

- One pure rule (`ForecastDataSufficiencyPolicy`) owns the whole D1 decision — boundary tests (4 / 5 / 6 active days; 0) live in one place; strong for the ≥80% mutation gate.
- The decision is computed once at the single choke point (`GetForecastThroughputStatus`), so all three forecast paths inherit it; D6 (post-fallback throughput) holds automatically.
- Additive contract: a new boolean on existing endpoints — no new route, **no `FEATURE_REQUIRES_SERVER_NEWER_THAN` gate**; an old server omits the field, a new client reads `false`-default? **No** — the FE default for a missing field must be `hasSufficientData = true` (treat as today: show the number), so an old server degrades gracefully to current behaviour.
- No EF migration: the threshold is a constant (D5), not a persisted `Team` column — contrast with `filter-forecast-throughput`, which added a column.
- `ForecastLevel`, the numeric likelihood DTOs, and the ADR-038 cap are untouched; suppression composes in front of them.

**Negative / accepted**

- A new boolean is added to three DTOs (a real, if additive, contract surface) — unavoidable: the D1 signal genuinely is not available frontend-side (the whole point of this ADR vs. ADR-038).
- The threshold number `5` appears in two places (backend constant + FE copy). Deliberate and test-guarded; de-risked by D5 (fixed). If a per-team threshold is ever introduced, the FE copy interpolates a wire value and the duplication disappears.
- Multi-team features: a feature's forecast `HasSufficientData` is the **AND** across its contributing teams that have remaining work (any data-thin contributing team makes the feature forecast untrustworthy). The common single-team case is just that team's flag. DELIVER verifies the aggregation; the delivery-level chip mirrors the governing (least-likely) feature it already derives its likelihood from.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The sufficiency rule exists in exactly one place (`ForecastDataSufficiencyPolicy`) — no inline `DaysWithThroughput >= 5` elsewhere | NUnit/grep test asserting the constant and predicate are referenced only from `ForecastDataSufficiencyPolicy` and `GetForecastThroughputStatus` |
| `ForecastDataSufficiencyPolicy` is pure (no I/O) | NUnit constructor/static-inspection test (no `IRepository<>`, `DbContext`, `HttpClient`, `ILogger`) |
| Every likelihood-rendering FE surface routes through `isForecastDataInsufficient` before showing a number | Vitest structural test asserting the four call sites branch on the predicate; no surface renders a likelihood without the suppression check |
| D1 boundary behaviour | NUnit unit tests on `ForecastDataSufficiencyPolicy` at 4 / 5 / 6 active days and 0; Vitest unit tests on `isForecastDataInsufficient` for the `hasRemainingWork × hasSufficientData` matrix |
| D4 exemption preserved | Tests asserting `hasRemainingWork === false` is never suppressed (composes with ADR-038 D4) |
| Numeric likelihood DTO fields unchanged | NUnit reflection test asserting `Likelihood`/`LikelihoodPercentage` remain `double` (additive boolean only) |

## Clients consistency verdict

The sufficiency boolean rides existing DTOs on existing endpoints; **no new endpoint → no `FEATURE_REQUIRES_SERVER_NEWER_THAN` version gate**. Clients that render a likelihood to a human SHOULD honour the suppression (print the "not enough data" message instead of a number) — a non-blocking follow-up in the clients repo. A client that does nothing still receives the (now simulation-excluded) likelihood and the new boolean it can ignore; the worst case is it keeps showing the old number, an honesty miss, not a breakage. Record the current latest release as the baseline should a client choose to gate the suppressed-state rendering.
