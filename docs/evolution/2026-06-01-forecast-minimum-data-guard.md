# Evolution: forecast-minimum-data-guard

- **Date finalized**: 2026-06-01
- **ADO**: Story #5125 — "Dont Forecast with too little Data" (persona delivery-forecaster)
- **Status**: Shipped to `main`, CI-green (run 26743295499, attempt 2). #5125 Closed.
- **Workspace (history)**: `docs/feature/forecast-minimum-data-guard/`
- **Sibling**: [forecast-confidence-cap](./2026-05-31-forecast-confidence-cap.md) (#5126, ADR-038) — this is the data-sufficiency **gate** that the cap's D3 explicitly deferred.

## What shipped

A team with only a day or two of completed work no longer receives a
confident-looking forecast. When a team's throughput window holds **fewer than 5
distinct days that each had ≥1 completed item**, every forecast surface suppresses
the number and shows a plain "not enough data yet" message instead. At or above 5
active days the forecast renders exactly as before. A genuinely completed item
(no remaining work) is exempt — there is no forecast to suppress (D4).

Surfaces guarded:

- Manual forecast headline (`ForecastLikelihood`, via `ManualForecaster`)
- Portfolio delivery overview chips (`DeliveriesChips`)
- Portfolio delivery header chip + per-feature likelihood column (`DeliverySection`)

The intent mirrors the sibling cap: stop the forecaster (and leadership) from
anchoring on a misleadingly precise date — here, the "100% by a 2027 date from a
team with a throughput of one" failure the reporter (Liz) hit and was manually
working around by hiding the forecast on her dashboard.

## Key decisions (user-locked D1–D7)

Full decision log with verbatim framing lives in the workspace `feature-delta.md`.
Load-bearing ones:

- **D1 — sufficiency = ≥5 distinct days with ≥1 completed item** in the throughput
  window (a datapoint = a day; only days *with* a completion count). User rejected
  total-items and window-length bases.
- **D2 — suppress, don't warn-over-a-number**: below threshold the output is
  replaced by "not enough throughput data yet (need ≥5 days with completed items)".
- **D3 — all forecast surfaces** (manual When + How-Many, portfolio delivery,
  per-feature).
- **D4 — completed / no-remaining-work exempt** (still 100%/Done); composes with the
  cap's D4.
- **D5 — fixed threshold of 5**, not per-team configurable (keeps scope thin; no
  RBAC/settings surface).
- **D6 — evaluate on the post-fallback throughput series** the forecast actually
  samples (composes with filter-forecast-throughput's empty-filtered-sample fallback).
- **D7 (DESIGN, ADR-039) — backend-computed boolean** `HasSufficientData` on the
  existing forecast DTOs. Unlike the cap (FE-only), the active-day count is *not*
  otherwise on the wire, so the one signal the FE can't compute itself rides the
  backend.

## Architecture (ADR-039) — and the implementation pivot

ADR-039's original plan swapped the simulation gate
(`if (status.Throughput.Total > 0)` → `if (status.HasSufficientData)`). On the
re-attempt this was deliberately **dropped** in favour of **signal, don't gate**:

- The simulation still runs unchanged for thin-throughput teams — they keep
  producing a number. This left the existing `When_ThroughputOfOne` /
  `HowMany_ThroughputOfOne` math tests untouched (the gate swap was the entire
  source of the first attempt's churn — broken fixtures, multi-file ripple).
- The backend's only job became: compute `HasSufficientData` **once** at the
  `ITeamMetricsService.GetForecastThroughputStatus` choke point (a pure
  `ForecastDataSufficiencyPolicy.HasEnoughData`, `MinimumActiveDays = 5`, reading the
  new `RunChartData.DaysWithThroughput`), carry it on the existing
  `ForecastThroughputStatus → WhenForecast → DTO` rails, AND aggregate it as an
  **AND across contributing teams** in `AggregatedWhenForecast` (DES-8: a feature is
  insufficient if any contributing team with remaining work is).
- The frontend owns suppression: pure `isForecastDataInsufficient =
  hasRemainingWork && hasSufficientData === false` + `InsufficientForecastDataIndicator`.
  Graceful old-server degradation via FE/DTO default `?? true`.

`RunChartData.History` was confirmed to be *total* window length (zero-day buckets
included), so `DaysWithThroughput` is a genuinely new metric.

## Regression caught at the gate: the missing EF migration

`HasSufficientData` was added to the **persisted** `WhenForecast` model without a
migration. The backend unit suite (EF InMemory) stayed green, but the sqlite/postgres
CI jobs started the real app and died on `PendingModelChangesWarning`. Fixed by
generating the migration for both providers via `Create-Migration.ps1` and dropping
the `= true` sentinel to match the `FilterApplied` column (a non-type-default bool
initializer makes the EF model-differ non-idempotent). Verified by running the app
against sqlite to "Application started". Captured as durable rules in
`docs/ci-learnings.md` (## EF migrations).

## Quality

- **Mutation testing: 100% / 100%** (`docs/feature/forecast-minimum-data-guard/mutation-report.md`).
  Backend `ForecastDataSufficiencyPolicy` / `RunChartData` / `AggregatedWhenForecast`
  18/18; frontend `isForecastDataInsufficient` 7/7. The first runs (57.89% / 41.67%)
  surfaced (a) the DISTILL AI-1 multi-team `All`/`Any` gap — closed by
  `AggregatedWhenForecastTest`; (b) a dead `compact` variant on the indicator —
  removed.
- Backend 2903 tests / 0 warnings; frontend 3172 tests; both builds clean.

## Permanent artifacts

- **ADR-039** — `docs/product/architecture/adr-039-forecast-data-sufficiency-backend-signal.md`
- **Application architecture delta** — `docs/product/architecture/brief.md` (`## Application Architecture — forecast-minimum-data-guard`)
- **Journey** — `docs/product/journeys/forecast-minimum-data-guard.yaml`
- **Job** — `job-forecast-only-with-enough-data` in `docs/product/jobs.yaml`
- **Mutation report** — `docs/feature/forecast-minimum-data-guard/mutation-report.md`

## Commits

`e54c1547` feat · `fe3f063f` docs · `e61f4324` fix(migration) · `f6a8fd14` test/mutation · `5df5a392` refactor(S107) · `22291d60` ci-learnings.
