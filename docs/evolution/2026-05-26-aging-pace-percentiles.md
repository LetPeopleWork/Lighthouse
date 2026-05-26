# Evolution — aging-pace-percentiles (Epic ADO #4144, Story #5075)

Date: 2026-05-26
Branch: `main` (trunk-based; shipped across many small commits)
Wave path: single canonical artifact (`docs/feature/aging-pace-percentiles/feature-delta.md`) covering DISCUSS → DESIGN → DISTILL → DELIVER, plus per-slice plans under `slices/`.
Outcome: shipped. The Work Item Aging chart (team **and** portfolio) now overlays per-state **pace-percentile bands** — filled green→red background zones per workflow-state column, off by default, toggled by a small top-right icon. The bands show the historical distribution of *cumulative total age at the moment items left each state* (50/70/85/95), so an in-flight dot sitting in a column's red zone is aging past where 95% of completed items had reached when they left that state. ActionableAgile parity for the per-state pace lens.

---

## Feature goal and user intent

The chart already drew end-to-end cycle-time percentile lines (full-width dashed). The competitive gap vs ActionableAgile was **per-state** granularity: "this item, in Review, is older than 85% of items ever were when they left Review." That per-state pace call is what `WorkItemStateTransition` (shipped by sibling `time-in-state-and-staleness`) unlocked. The feature is a pure downstream reader — no new persistence, no migration, no external integration, no premium gate.

A 2026-05-25 scope simplification (with the product owner) cut the original design down to "the coloring with the toggle, nothing more": filled colored zones (not dashed segments), one toggle (not two chip groups), and removed the per-dot tooltip annotation (old US-03), per-band hover, low-sample messaging, and the `sampleSize` field.

---

## Business context

| Item | Value |
|---|---|
| Epic | ADO #4144 — *More Detailed State Info* (MVP bundle) |
| Story | ADO #5075 — set to **Resolved** on push (awaiting CI green → Done at release) |
| Metric (locked) | Cumulative total age at state exit = `exitTransition.TransitionedAt − StartedDate`, bucketed by the exited (`FromState`) state, percentiles 50/70/85/95 via the existing `PercentileCalculator`. Last (final Doing) state's bands equal the cycle-time percentiles exactly (same population + same inclusive day-count). |
| Scope | Team + portfolio (shared `BaseMetricsView`/`WorkItemAgingChart`), single slice |
| Outcome KPIs | `OUT-aging-pace-*` registered in `kpi-contracts.yaml`; admin-visible only (per-instance) — cross-instance measurement blocked on Epic 5015 opt-in telemetry |

---

## What shipped (architecture)

- **Backend**: `AgeInStatePercentilesDto`; a `protected static BaseMetricsService.ComputeAgeInStatePercentiles` helper (no `IPerStateAggregationService` — ADR-021 upheld); `GetAgeInStatePercentilesForTeam`/`…ForPortfolio` service methods; two new `GET …/metrics/ageInStatePercentiles` endpoints mirroring `cycleTimePercentiles`. Team reads transitions via `IWorkItemStateTransitionRepository`; **portfolio via `IFeatureStateTransitionRepository`** (Features have their own transition type — a divergence from the DESIGN's single-repo idealization). New ArchUnit rules lock the seam.
- **Frontend**: `IPerStatePercentileValues` model + `MetricsService.getAgeInStatePercentiles`; parallel fetch in `useMetricsData`; a filled `<rect>` overlay (`computePaceBandRects` + `PaceBandOverlay`) drawn behind the dots inside `<ChartsContainer>` via MUI-X `useXScale`/`useYScale`; a top-right icon toggle backed by `useShowPaceBands` (localStorage-persisted global preference, off by default).

---

## Decisions / notable trajectory

| ID | Decision | Status at close |
|---|---|---|
| D12 / DDD-1 | Band metric = cumulative total-age-at-exit (not time-in-state) | **Held.** Bands rise left→right, comparable to the chart's total-age Y axis. |
| D6 / DDD-6 | Filled green→red zones behind the dots (not dashed segments) | **Held**, refined post-review: green at the floor (axis min = 1) → red at the top, full-height contiguous columns (half-width 0.5). |
| DDD-8 | Single off-by-default toggle | **Evolved.** Shipped as a legend chip, then changed (review feedback) to a small top-right icon so it stops shifting the Work Item Types chips; persisted in localStorage as a global binary. |
| DDD-4 | Empty state omitted | **Reversed (review).** An empty state now repeats its nearest populated left neighbour; leading-empty states render bare. |
| Demo data | Per-state CSV columns (slice 04-04) | **Replaced (review).** Demo-only interpolation: every demo Done item gets an evenly-interpolated journey ending at ClosedDate, so the last column equals the cycle-time lines exactly. Real CSV imports never get synthetic journeys (opt-in connection option). |

---

## DELIVER trajectory + what bit us

- The live Playwright walking skeleton (05-01) **failed first** — CSV demo data carried no multi-state journey, so the bands were empty. Caught a real demo-data gap that unit/integration tests could not. Resolved by demo journey synthesis (04-04, later simplified to interpolation).
- Post-merge review found the band **colors were inverted** (reused `ForecastLevel`, calibrated for forecast certainty = backwards for aging). Fixed to position-based green→red; the tests had asserted the palette generically, not the direction — lesson logged.
- The first CI run failed on 12 backend Sonar `new_violations` + an E2E baseline-before-load race on slow Postgres. **Five of the 12 Sonar rules were already in `docs/ci-learnings.md`** — they recurred because the DELIVER crafters didn't consult the ledger. Logged as recurrences.
- A GitHub Actions major outage blocked CI validation of the fixes for an extended window; work continued locally (all gates green) and CI re-validation was deferred to recovery.

---

## Quality at close

Backend ~2787 tests green (0 warnings); frontend ~3000+ tests green; both builds + Biome clean. Mutation (original feature): backend 85.7%, frontend 89.58%. Adversarial review of the original feature + the post-delivery review rounds: APPROVED. E2E walking skeleton (team + portfolio toggle) green live on demo scenario 0.
