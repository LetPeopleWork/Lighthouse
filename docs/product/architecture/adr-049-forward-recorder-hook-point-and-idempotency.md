# ADR-049: Event-Driven Forward Recorder (PortfolioForecastsUpdated) and Date-Keyed Idempotency

**Status**: Proposed (2026-06-02 — Morgan, interaction mode PROPOSE; revised 2026-06-02 to event-driven per user decision, superseding the inline-step approach below; pending user confirmation)
**Date**: 2026-06-02
**Feature**: delivery-metrics (Epic 3993)
**Decider**: Morgan (Solution Architect)
**Relates to**: ADR-048 (the store this recorder writes); ADR-027 / Epic 5121 (the domain-event bus this recorder hooks); DISCUSS D11, KPI 4

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993

---

## Context

The forward recorder is the **sole feed** of `DeliveryMetricSnapshot` (ADR-048): every series —
the actual-item backlog/done counts AND the forward-only inferred estimate, forecast-how-many, and
likelihood/when-distribution — is appended once per delivery per day from the day recording begins.
The question is WHERE this recorder runs.

Verified code reality: deliveries have **no** periodic refresh service of their own. The delivery
likelihood is recomputed indirectly — `ForecastUpdater` (a `UpdateServiceBase<Portfolio>`, update
type `Forecasts`) calls `IForecastService.UpdateForecastsForPortfolio`, which recomputes
`Feature.Forecasts` for every feature touching the portfolio's teams and saves them. Delivery
likelihood/progress/forecast are then derived at read time in `DeliveryWithLikelihoodDto.FromDelivery`
off those refreshed `Feature.Forecasts` + `FeatureWork`. So the moment "fresh forecast data exists
for a delivery's features" is exactly the completion of the portfolio forecast update.

`RefreshLog` already records each Team/Portfolio refresh with `ExecutedAt` — the cadence anchor.

**Domain-event bus already exists** (Epic 5121 / ADR-027): `IDomainEvent`,
`IDomainEventDispatcher.PublishAsync<TEvent>`, `IDomainEventHandler<in TEvent>.HandleAsync`, and the
`DomainEventDispatcher`. The codebase is standardizing cross-aggregate reactions onto this bus.
Precedent handlers to model the recorder on: `PortfolioFeaturesRefreshedMetricsInvalidationHandler`
(`IDomainEventHandler<PortfolioFeaturesRefreshed>` → invalidates portfolio metrics) and
`TeamDeletedRefreshLogCleanupHandler` (cleanup-on-delete reaction).

**Ordering finding (the crux)**: in `PortfolioUpdater.Update`, `PortfolioFeaturesRefreshed` is
dispatched at line 73 — BEFORE `deliveryRuleService.RecomputeRuleBasedDeliveries` (line 76) and
BEFORE `forecastUpdateService.UpdateForecastsForPortfolio` (line 82) and its forecast write-back
(line 84). So the existing `PortfolioFeaturesRefreshed` event fires with STALE forecasts/likelihood
and pre-membership-recompute. There is NO existing "forecasts updated" event. The genuinely-fresh
moment a recorder needs is AFTER line 84, with no event currently marking it.

## Decision

Make the forward recorder **event-driven via the domain-event bus**, not an inline step in the
updater.

1. **New domain event** `PortfolioForecastsUpdated(int PortfolioId) : IDomainEvent` — a record
   mirroring `PortfolioFeaturesRefreshed`'s shape. It is dispatched at the genuinely-fresh moment:
   AFTER `UpdateForecastsForPortfolio` + the forecast write-back in `PortfolioUpdater.Update`
   (after ~line 84), AND at the equivalent end-of-update point in the separate `ForecastUpdater.Update`
   path (after its `UpdateForecastsForPortfolio` + `TriggerForecastWriteBackForPortfolio`). DELIVER
   must ensure it dispatches **once per portfolio-forecast-completion on both paths**. This is the
   ONLY new event Epic 3993 introduces.

2. **Recorder = a domain-event handler** `DeliveryMetricSnapshotRecordingHandler :
   IDomainEventHandler<PortfolioForecastsUpdated>` (named consistently with the precedent handlers).
   On `HandleAsync`: load the portfolio's deliveries, project each delivery's then-current figures
   reusing the `DeliveryWithLikelihoodDto.FromDelivery` projection (DRY of the metric KNOWLEDGE, not
   duplicated arithmetic) — the current backlog/done counts AND the forward-only forecasting figures —
   and upsert today's `DeliveryMetricSnapshot` row per delivery through `IDeliveryMetricSnapshotRepository`.
   It is the SOLE feed (ADR-048): forward-only, no backfill, no reconstruction path.

Idempotency is **date-keyed and UNCHANGED**: the row identity is `(deliveryId, recordedAt.Date)`. The
handler does a get-or-create on that key and overwrites the forward columns for today (a same-day
re-run recomputes today's row in place — at most one row per delivery per day). It is NOT guarded by
a `=true` "already recorded" boolean sentinel (the forecast-minimum-data-guard non-idempotency lesson).

## Alternatives Considered

### Alternative A0 — Inline step inside `ForecastUpdater.Update` (the superseded original decision)
- Run the recorder as a direct method call inside the forecast-update pipeline, after
  `UpdateForecastsForPortfolio` and before/with the write-back trigger — no event involved.
- **Rejected (user decision, 2026-06-02; supersedes the original ADR-049 decision)**: it accretes a
  second responsibility (snapshot recording) onto an updater whose job is forecasting, and it bypasses
  the domain-event bus the codebase is standardizing on (Epic 5121 / ADR-027). An event-driven handler
  keeps the recorder a separate, independently-testable reaction, consistent with
  `PortfolioFeaturesRefreshedMetricsInvalidationHandler`. The freshness guarantee is preserved by
  dispatching the new event only after the forecast write-back, so the inline approach buys nothing the
  event approach lacks while coupling two concerns.

### Alternative A1 — Reuse the existing `PortfolioFeaturesRefreshed` event
- Make the recorder an `IDomainEventHandler<PortfolioFeaturesRefreshed>` rather than introducing a new
  event — no new event type at all.
- **Rejected — freshness**: `PortfolioFeaturesRefreshed` is dispatched at `PortfolioUpdater.Update`
  line 73, BEFORE `RecomputeRuleBasedDeliveries` (line 76) and BEFORE `UpdateForecastsForPortfolio`
  (line 82) + write-back (line 84). A handler on it would record STALE forecast/likelihood and
  pre-membership-recompute deliveries, so Slices 3-4 would persist wrong forward figures. The new
  `PortfolioForecastsUpdated` event fires post-forecast, at the only moment the data is fresh. This is
  the decisive rationale for a new event over reuse.

### Alternative A — Separate scheduled job (own `IHostedService` / timer)
- A new background service that wakes daily and records every active delivery.
- **Rejected**: introduces a second cadence to reason about and a second place forecasts could be
  stale relative to (the job could fire between forecast updates and record a stale forecast). The
  forecast-update pipeline already runs on the right cadence and guarantees fresh `Feature.Forecasts`
  at the moment of recording — colocating avoids the staleness race and reuses the existing
  `UpdateServiceBase` infrastructure, scoping, and `RefreshLog`-adjacent observability. Adds
  operational surface (`Resume-Driven Development` smell) for no benefit.

### Alternative B — On-demand at read time (record when a chart is opened)
- The metrics-history endpoint records today's forward row as a side effect of being read.
- **Rejected**: a GET with a write side effect violates REST semantics and `IRbacAdministrationService`
  read/write separation; deliveries nobody opens get no snapshots (KPI 4 — "100% of active
  deliveries have a daily forward snapshot" — fails); and the read path would block on a Monte-Carlo
  recompute. The recorder must be server-side and decoupled from reads.

### Alternative C — `=true` "captured today" sentinel flag on the delivery
- A boolean the recorder sets and checks to avoid double-recording.
- **Rejected explicitly**: this is the exact non-idempotency trap from the forecast-minimum-data-guard
  feature (`project_forecast_minimum_data_guard`) — a boolean sentinel is non-idempotent across
  restarts / clock changes / multi-row days and silently drifts. The natural key
  `(deliveryId, recordedAt.Date)` is the correct idempotency mechanism.

## Consequences

**Positive**:
- Fresh-by-construction forecast data at record time: the handler runs only on
  `PortfolioForecastsUpdated`, dispatched after the forecast write-back — never on the stale
  pre-forecast `PortfolioFeaturesRefreshed`.
- Recording is a separate, independently-testable reaction on the domain-event bus, consistent with
  Epic 5121 / ADR-027 and the precedent handlers — the updater keeps a single responsibility.
- Reuses the `DeliveryWithLikelihoodDto.FromDelivery` projection (DRY of the metric KNOWLEDGE, not
  just code) and rides the existing forecast-update cadence — no new background service, no new
  schedule, no new operational surface.
- Date-keyed upsert is idempotent under re-run, restart, and concurrent triggers (the unique index
  on `(deliveryId, recordedAt)` from ADR-048 enforces it at the DB).

**Negative**:
- Couples snapshot recording to the forecast-update lifecycle (via the event): a portfolio whose
  forecast update is disabled/failing dispatches no `PortfolioForecastsUpdated` and records no forward
  snapshots (acceptable — its forecast would be stale anyway, and the chart honestly shows the
  forward-only empty/sparse state, D6).
- The handler adds a small amount of work per portfolio forecast completion (one projection + one
  upsert per delivery) — bounded and well under the Monte-Carlo cost that precedes the event.
- A new event type and two dispatch sites (`PortfolioUpdater` + `ForecastUpdater`) must stay in sync;
  DELIVER must guarantee exactly-once-per-completion dispatch on both paths (see the dispatch probe).

## Earned Trust — probing the recorder

- **Freshness probe (the decisive one)**: the handler reacts to `PortfolioForecastsUpdated`, which is
  dispatched AFTER `UpdateForecastsForPortfolio` has saved fresh `Feature.Forecasts`; an integration
  test asserts the recorded `forecastHowMany`/`likelihoodPercentage` match the just-computed forecast
  for the fixture (no stale read), and a regression test asserts a handler on `PortfolioFeaturesRefreshed`
  WOULD have recorded the stale pre-forecast values — proving why the new event exists.
- **Dispatch probe**: an integration test asserts `PortfolioForecastsUpdated` is dispatched exactly
  once per portfolio-forecast-completion on BOTH the `PortfolioUpdater.Update` and `ForecastUpdater.Update`
  paths (no missing dispatch, no double dispatch).
- **Idempotency probe**: the handler runs twice for the same day and asserts a single row with the
  latest forward values (no duplicate, no append). A unique-index violation on `(deliveryId, recordedAt)`
  is the DB-level backstop the test also exercises.
- **Cadence guardrail (KPI 4)**: a backend integration metric asserts every active delivery has a row
  for the run's date after the event is handled, with zero duplicates.
