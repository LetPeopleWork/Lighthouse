# DESIGN Wave Decisions — delivery-metrics

Feature: delivery-metrics (Epic 3993)
Architect: Morgan (Solution Architect), interaction mode = PROPOSE
Date: 2026-06-02
Status: the six forking decisions are PROPOSED — pending user confirmation. Locked DISCUSS decisions D1-D12 inherited.

## Key Decisions (PROPOSED — pending confirmation)

| # | Decision | Recommendation | ADR / source |
|---|---|---|---|
| 1 | Forward-recorder trigger | Event-driven: NEW `PortfolioForecastsUpdated` event dispatched after `UpdateForecastsForPortfolio` + write-back in `PortfolioUpdater`/`ForecastUpdater`; recorder = `DeliveryMetricSnapshotRecordingHandler : IDomainEventHandler<PortfolioForecastsUpdated>` (the sole feed). NOT an inline updater step; NOT the pre-forecast `PortfolioFeaturesRefreshed` (stale) | ADR-049 |
| 2 | Snapshot schema granularity | ONE wide row per (delivery, day), nullable forward columns, JSON when-distribution | ADR-048 / ADR-050 |
| 3 | Endpoint shape | ONE `metrics-history` endpoint, all series, forward-only forecasting fields null until accrued | ADR-050 |
| 5 | Chart placement | TABS inside the per-delivery `DeliverySection` accordion — a "Work Items" tab (existing grid) and a "Metrics" tab (charts, the lazy fetch trigger); when-distribution is a toggle on the predictability chart | brief / D12 |

*(Former Decisions 4 "Backfill execution model" and 6 "Backfill done source" removed — the backfill was dropped by the user on 2026-06-02; the store is forward-recorded only. See `design/upstream-changes.md`.)*

## Architecture Summary

Ports-and-adapters, unchanged. This feature adds the FIRST delivery time-series persistence: ONE
`DeliveryMetricSnapshot` store (driven port `IDeliveryMetricSnapshotRepository`) fed by ONE feed — a
forward recorder implemented as a domain-event handler (`DeliveryMetricSnapshotRecordingHandler :
IDomainEventHandler<PortfolioForecastsUpdated>`) on the existing Epic 5121 / ADR-027 bus, reacting to a
NEW `PortfolioForecastsUpdated` event dispatched after the portfolio forecast update + write-back in
`PortfolioUpdater`/`ForecastUpdater`. It reuses the `DeliveryWithLikelihoodDto.FromDelivery` projection
and records every series (backlog/done counts + forward-only inferred-estimate/forecast/likelihood) for
the then-current day; the store accrues from the day recording begins, with no backfill and no
reconstruction. The event fires post-forecast precisely because the existing `PortfolioFeaturesRefreshed`
fires pre-forecast (line 73, stale). Snapshot rows are FK cascade-deleted with their `Delivery`
(`ON DELETE CASCADE`); no `DeliveryDeleted` event. ONE new driving port
(`GET .../deliveries/{deliveryId}/metrics-history`, `[RbacGuard(PortfolioRead)]`). Up to three new
chart components in a "Metrics" tab on the existing per-delivery `DeliverySection`. Idempotency is
date-keyed `(deliveryId, recordedAt.Date)` — NOT a `=true` sentinel. Premium + RBAC gating inherited.
No new route, external integration, or library.

C4: `docs/product/architecture/c4-diagrams.md` → "delivery-metrics" (L1 no-delta, L2 container, L3
component). Brief: `## Application Architecture — delivery-metrics`. ADRs: 048, 049, 050.

## Reuse Analysis (verdicts)

- EF persistence pattern / `IRepository<T>` — EXTEND.
- `ForecastUpdater` / `PortfolioUpdater` / `UpdateServiceBase<Portfolio>` cadence — EXTEND (dispatch the new `PortfolioForecastsUpdated` event after the forecast update + write-back).
- Domain-event infra + `IDomainEventHandler` pattern (Epic 5121 / ADR-027) — EXTEND (recorder is a handler, modeled on `PortfolioFeaturesRefreshedMetricsInvalidationHandler`).
- `PortfolioForecastsUpdated` event + `DeliveryMetricSnapshotRecordingHandler` — CREATE NEW (no existing post-forecast event; `PortfolioFeaturesRefreshed` fires pre-forecast/stale).
- `DeliveryWithLikelihoodDto.FromDelivery` projection — EXTEND (reuse for forward figures).
- `IForecastService` + `Feature.Forecasts` / `Feature.FeatureWork` / `Feature.EstimatedSize` — REUSE AS-IS (recorder source: current counts + forecasts + inferred size).
- `LighthouseAppContext` JSON value-converter — EXTEND (`WhenDistributionJson`).
- MUI-X `LineChart` / `StackedAreaChart` area+line idiom — EXTEND (pattern).
- `getLikelihoodLevel` / `ForecastLevel` RAG — REUSE AS-IS.
- `useRbac()` + `canUsePremiumFeatures` gate — REUSE AS-IS.
- `DeliverySection` accordion — EXTEND ("Work Items" + "Metrics" tabs).
- `DeliveryMetricSnapshot` store + forward recorder — CREATE NEW (no delivery time-series persistence exists; justified vs `RefreshLog`/`UpdateServiceBase`; forward-only, no backfill component).
- `DeliveryBurnupChart` / `DeliveryPredictabilityChart` / `DeliveryFeverChart` — CREATE NEW (different unit/question; justified vs `state-time-cumulative-view` new-chart precedent).
- `metrics-history` endpoint — CREATE NEW (mirrors `DeliveriesController` precedent).

## Tech Stack

ASP.NET Core .NET 8; EF Core 8.x (migration via `CreateMigration` across Sqlite/Postgres); NUnit 4.6
+ Moq + EF InMemory + WebApplicationFactory (migration test on a REAL provider); Stryker.NET ≥80%;
React 18 + TS 5.x strict; MUI + MUI-X-charts `LineChart`; Zod at the trust boundary; Vitest + RTL;
Stryker TS ≥80%; Playwright POM. All MIT/Apache-2.0. No new dependency.

## Constraints

- EF migrations generated ONLY via the `CreateMigration` PowerShell script across all providers; not
  run during DESIGN. Migration test must run on a real provider (InMemory misses the migration trap).
- Idempotency MUST be the natural key `(deliveryId, recordedAt.Date)`, never a `=true` sentinel
  (forecast-minimum-data-guard lesson).
- Immutability (records / `with` / immutable collections), nullable enable, `TreatWarningsAsErrors`,
  no comments/decorative dividers, options-objects to cap params, early returns — per CLAUDE.md and
  ci-learnings (avoid the recurring S107/S2325/CA1859/etc. foot-guns in new code).
- Forward-only series start empty and accrue (D6) — charts must render the honest forward-only state,
  never zero.

## Upstream Changes

Two:
1. Endpoint consolidation (two DISCUSS endpoints → one `metrics-history` endpoint, a DISCUSS-delegated
   choice) plus route alignment to the `DeliveriesController` convention. No story/AC/KPI dropped.
2. **Backfill dropped (user decision, 2026-06-02)** — the store is fed by the forward recorder ONLY;
   every series is forward-only. Slice 1 is rescoped lighter; D6's empty state is now universal;
   former Decisions 4/6 are removed; ADR-048 rewritten to single-feed. No story/AC/KPI dropped (US-01
   reframed to forward-only burnup).

See `docs/feature/delivery-metrics/design/upstream-changes.md` for both.

## Outcome collision check

`nwave-ai outcomes check-delta` was not run — the CLI/registry is not available in this environment;
step skipped per the best-effort instruction. (KPIs are recorded in the DISCUSS feature-delta; KPI 4
guardrail is enforced by backend integration assertions.)
