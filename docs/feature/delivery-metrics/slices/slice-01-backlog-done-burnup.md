# Slice 01 — Snapshot store + forward recorder + backlog/done burnup (walking skeleton)

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-01 (backlog+done burnup), US-02 (`DeliveryMetricSnapshot` store — folded in, no longer a standalone slice)
**Effort estimate**: ~1-1.5 days (schema + EF migration + new `PortfolioForecastsUpdated` event + its dispatch + the `DeliveryMetricSnapshotRecordingHandler` + first chart — no backfill component; the event+handler is comparable effort to the former inline step)
**Reference class**: sibling charting epic `state-time-cumulative-view` slice-01 (new metrics endpoint + new chart widget) for the chart half; `RefreshLog` DbSet + its EF-migration shape for the persisted-table half; the precedent domain-event handlers `PortfolioFeaturesRefreshedMetricsInvalidationHandler` + `TeamDeletedRefreshLogCleanupHandler` (Epic 5121 / ADR-027) for the recording handler; `PortfolioUpdater`/`ForecastUpdater` + `IDomainEventDispatcher` for the event dispatch. No sub-step lacks an in-repo precedent — the recorder reacts to a new event dispatched on the existing forecast-update cadence.

## Goal (one sentence)

Stand up ONE `DeliveryMetricSnapshot` store as the single time-series source of truth, fed by a forward recorder hooked into the existing forecast-update pipeline that records each delivery's then-current backlog and done counts once per day, and surface a burnup chart in the delivery's "Metrics" tab that reads from that store — so from the day recording begins a Delivery Forecaster sees the delivery's backlog/done accrue over time (the chart starts empty and fills one day at a time, exactly like the forecast trends).

## IN scope

- New table `DeliveryMetricSnapshot` (DbSet + EF migration generated via the `CreateMigration` PowerShell script across all providers — do NOT call `dotnet ef migrations add` directly, do NOT run the script in this DISCUSS run; the brief only records that DESIGN/DELIVER uses it): `deliveryId`, `recordedAt`, `totalWork`, `doneWork`, `remainingWork`, plus nullable forward columns (`likelihoodPercentage`, serialized when-distribution, forecast projection, inferred-estimate) that Slices 2-4 populate. The `DeliveryId` FK to `Delivery` is `ON DELETE CASCADE` (ADR-048) so rows never orphan when a delivery is deleted — this FK is part of THIS slice's store schema.
- New domain event `PortfolioForecastsUpdated(int PortfolioId) : IDomainEvent` (record, mirrors `PortfolioFeaturesRefreshed`), dispatched via `IDomainEventDispatcher.PublishAsync` AFTER `UpdateForecastsForPortfolio` + the forecast write-back in BOTH `PortfolioUpdater.Update` (after ~line 84) and `ForecastUpdater.Update` — once per portfolio-forecast-completion on each path. NOT the existing `PortfolioFeaturesRefreshed` (it fires pre-forecast at line 73, before recompute/forecast at 76/82/84, so it carries stale forecast/likelihood).
- **Forward recorder** (the sole feed) as a domain-event handler `DeliveryMetricSnapshotRecordingHandler : IDomainEventHandler<PortfolioForecastsUpdated>` (modeled on `PortfolioFeaturesRefreshedMetricsInvalidationHandler`): on `HandleAsync`, for each delivery, record the then-current `totalWork`, `doneWork`, `remainingWork = totalWork - doneWork` into that day's snapshot row, reusing the `DeliveryWithLikelihoodDto.FromDelivery` projection. Idempotent — re-running the same day overwrites today's row in place, never duplicates (guard on `(deliveryId, recordedAt)`, not a `=true` sentinel; per the forecast-minimum-data-guard non-idempotency lesson). A re-open (item leaving Done) lowers the next day's `doneWork` naturally.
- New endpoint `GET /api/portfolios/{portfolioId}/deliveries/{deliveryId}/metrics-history` returning `{ points: [{ date, totalWork, doneWork, remainingWork }] }` read FROM the snapshot store.
- New chart widget `DeliveryBurnupChart.tsx` rendering the backlog line and the done line/area over time, in a new "Metrics" tab inside the per-delivery `DeliverySection` accordion (the existing feature grid becomes a "Work Items" tab; the Metrics tab is the lazy fetch trigger for the history endpoint).
- Forward-only empty state (no snapshots recorded yet / delivery with no items) and single-day-history edge cases — the chart reads "builds forward from today — no snapshots recorded yet" (D6), never zero.
- Premium gating: the tab inherits the existing `canUsePremiumFeatures` gate already on the Delivery surface.

## OUT scope (deferred)

- Estimated size for not-yet-broken-down features (the inferred-estimate backlog line) — Slice 2 (US-01b), a further forward-recorded series.
- Forecast-over-time and likelihood-over-time (forward-fed into the same store — Slices 3-4).
- Fever chart (Slice 5).
- Any backfill / retroactive reconstruction of history — dropped by product decision (2026-06-02). Every series is forward-recorded; the store accrues from the day recording begins. See the feature-delta `## Changed Assumptions`.
- Any change to the current-snapshot delivery metrics (`likelihoodPercentage`, `progress`) — unchanged.

## Learning hypothesis

- **Disproves if it fails**: that a forward-recorded daily backlog/done burnup is the artifact forecasters want, and that the recorder captures the counts correctly. If forecasters don't find an accruing daily burnup useful for the leadership status story, or the recorder records wrong/missed counts, the forward-recorder + store + chart foundation is unsafe — a costly but EARLY finding before Slices 2-4 build on it.
- **Confirms if it succeeds**: that the forward-recorder + store + chart pipeline is the right foundation for Slices 2-5, and that a daily-accruing backlog/done burnup read from one store is a credible basis for the over-time trend story (honestly noting: no immediate history — value accrues over weeks).

## Acceptance criteria

- US-01 AC items from `feature-delta.md` apply unchanged.
- Integration test (NUnit + EF InMemory + WebApplicationFactory for the endpoint): the recording handler, invoked for a `PortfolioForecastsUpdated` event with a delivery of known current counts, writes the day's exact `totalWork`/`doneWork`/`remainingWork` snapshot row; a re-open in the fixture lowers the next recorded `doneWork`; re-handling the same event the same day is a no-op that overwrites in place (idempotency on `(deliveryId, recordedAt)`); the endpoint returns the recorded rows; `PortfolioForecastsUpdated` is dispatched once per portfolio-forecast-completion on both `PortfolioUpdater.Update` and `ForecastUpdater.Update` (and is dispatched AFTER, not before, the forecast — so the recorded values are fresh, not the pre-forecast stale ones).
- Integration test: deleting a delivery cascade-deletes its `DeliveryMetricSnapshot` rows (FK `ON DELETE CASCADE`) — no orphans remain.
- The EF migration applies on a real (non-InMemory) provider in the migration test (per the persisted-model EF-migration trap — InMemory misses it).
- Vitest + RTL: the burnup renders a backlog line and a done line over the date axis reading from the endpoint; the forward-only empty-state message renders when no snapshots exist yet.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud gate passes; mutation ≥80% on new code.

## Carpaccio taste tests (re-run for the unified Slice 1)

- **Vertical (DB→UI)?** PASS — schema + forward recorder (DB) → endpoint → burnup chart (UI).
- **Demoable in one session?** PASS — trigger a forecast update for a delivery, see today's point appear; the chart shows the forward-only empty state before that.
- **User-visible value?** PASS — the burnup chart IS the user-visible value; the store is not shipped naked.
- **Independently shippable?** PASS — no deps; the recorder rides the existing forecast-update cadence.
- **"Ship the abstraction first, not abstraction-only"?** PASS — this slice ships >1 new component (table + migration + forward recorder + endpoint + chart) AND an abstraction (the snapshot store). Justified: the store IS the shared abstraction the whole feature stands on, and it ships in the SAME slice as a user-visible chart that consumes it. There is no infra-only foundation slice to fail the slice-composition gate.
- **Verdict**: **PASS.**

## Dependencies

- **HARD**: EF migration generated via the `CreateMigration` PowerShell script across all supported providers (do not call `dotnet ef migrations add` directly).
- **SOFT**: the domain-event bus (Epic 5121 / ADR-027) is already in place — the recorder is a handler on it; no new infra, just a new event + handler + two dispatch sites.

## Production data requirement

Exercise the recorder against a REAL delivery on the dev/seed instance: trigger the forecast-update pipeline across two days (or simulate two recorded days), confirm the burnup accrues a point per day, the idempotency guard holds on a same-day re-run, and a re-open lowers the next day's done. Screenshot the burnup (and the forward-only empty state) in the PR.

## Cross-cutting (DoR item 7)

- **RBAC**: read view; gates through the existing portfolio read-access path (`IRbacAdministrationService`); UI gating via `useRbac()` — viewers see the chart read-only, no new write surface (the recorder is a server-side background step, not a user action). No component fetches `/my-summary` directly.
- **Lighthouse-Clients**: the store-backed `GET .../deliveries/{deliveryId}/metrics-history` endpoint ships in THIS slice, so the version-gating obligation applies from Slice 1 — version-gate it in the clients repo (`FEATURE_REQUIRES_SERVER_NEWER_THAN`, pinned strictly newer than the last released Lighthouse; bump that registry to the current latest release when wrapping). An old server returns an opaque 404; the wrapping client method must pre-check the server version and fail with a clear "upgrade Lighthouse" error. Dev/unparseable versions must never be blocked. CLI/MCP expose a delivery-history command only if wanted; the version-gate registry entry is required regardless once the endpoint is wrapped.
- **Website**: N/A for this slice — marketing surface waits for the full delivery-tracking launch (Slice 3+).

## Pre-slice SPIKE

OPTIONAL (~1h): confirm the exact dispatch points for `PortfolioForecastsUpdated` in `PortfolioUpdater.Update` (after ~line 84) and `ForecastUpdater.Update` so it fires once per portfolio-forecast-completion post-write-back on both paths, and confirm the store's per-day-per-delivery row volume stays acceptable as snapshots accrue. Skip if the precedent handlers + dispatch sites are clear.
