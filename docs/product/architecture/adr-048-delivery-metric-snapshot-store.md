# ADR-048: Unified DeliveryMetricSnapshot Store Fed by a Forward Recorder

**Status**: Proposed (2026-06-02 — Morgan, interaction mode PROPOSE; pending user confirmation)
**Date**: 2026-06-02
**Feature**: delivery-metrics (Epic 3993) — over-time delivery metrics on the Portfolio → Delivery detail surface
**Decider**: Morgan (Solution Architect)
**Relates to**: DISCUSS decisions D1, D11, D12 in `docs/feature/delivery-metrics/feature-delta.md`; the forward-only change in `docs/feature/delivery-metrics/design/upstream-changes.md`

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993

---

## Context

Lighthouse persists only CURRENT delivery state today. `LighthouseAppContext` (verified) has no
time-series table: `Deliveries`, `WorkItems`, `WorkItemStateTransitions`, `FeatureStateTransitions`,
`Features`, `Portfolios`, `RefreshLogs` all carry point-in-time data. `DeliveryWithLikelihoodDto.FromDelivery`
computes likelihood/progress/remaining/total at request time off `Feature.FeatureWork` and
`Feature.Forecasts` — none of it is stored over time.

The feature needs five over-time series across three charts (D12): actual-item backlog, done,
inferred-estimate of not-yet-broken-down features, forecast-how-many-by-target-date, and
likelihood/when-distribution.

A prior framing tried to slice the feature on whether a series could be *reconstructed* from item
dates already in the DB. The product decision (2026-06-02, locked by the user) is that ALL series —
including backlog and done — are **forward-only**: the store accrues daily from the day recording
begins, exactly like the forecast and likelihood trends. There is no retroactive reconstruction of
history. The chart starts empty at launch and fills one day at a time. This makes the store unified
for a simpler reason than before: every series shares the same `(deliveryId, recordedAt.Date)` grain
and the same forward-recorded lifecycle, so they belong in one wide row in one store.

## Decision

Build **ONE** `DeliveryMetricSnapshot` store as the single time-series source of truth, fed by **ONE**
feed — the **forward recorder**, implemented as a domain-event handler
(`DeliveryMetricSnapshotRecordingHandler : IDomainEventHandler<PortfolioForecastsUpdated>`) reacting
to the new `PortfolioForecastsUpdated` event dispatched after the portfolio forecast update completes
(ADR-049).

The forward recorder runs on the existing forecast-update cadence (at most one row per delivery per
day) and records that day's then-current figures into the snapshot row:

- `totalWork` / `doneWork` / `remainingWork` — the delivery's current actual-item backlog and done
  counts as of that day (re-opens are handled naturally — each day snapshots the then-current count,
  so an item leaving Done simply lowers the next day's `doneWork`).
- `estimatedTotalWork` (US-01b), `forecastHowMany` (US-03), `likelihoodPercentage` + serialized
  `whenDistribution` (US-04) — the forward-only forecasting figures.

All charts read from the one store via the metrics-history endpoint. There is no live-query read path
and no historical reconstruction path. The store is the shared abstraction the whole feature stands
on (ports-and-adapters: a new driven port `IDeliveryMetricSnapshotRepository : IRepository<DeliveryMetricSnapshot>`
over EF, plus a `DeliveryMetricSnapshot` DbSet and EF migration generated via the `CreateMigration`
PowerShell script across all providers).

### Snapshot lifecycle on delivery delete — EF FK cascade

`DeliveryMetricSnapshot.DeliveryId` is an FK to `Delivery` with **`ON DELETE CASCADE`**. When a
delivery is deleted (`DeliveriesController.DeleteDelivery`), its snapshot rows are removed by the
database, so they never orphan. This is the simplest correct option and matches the existing cascade
idiom on the `Deliveries` table. **No `DeliveryDeleted` domain event is introduced for this** — an
event would only be warranted if a cross-aggregate reaction emerged; FK cascade suffices for in-row
cleanup. (`DeliveryMetricSnapshot` is owned by, and cascade-deleted with, its `Delivery` — it is not
a new aggregate root.)

### Event scope for Epic 3993

The ONLY new domain event 3993 introduces is `PortfolioForecastsUpdated` (the recorder's trigger,
ADR-049). Retrofitting domain events onto existing delivery CRUD (create/update/delete) is **out of
scope** — that is Epic 5121's remit, and 3993's jobs have no consumer for such events (snapshot
cleanup on delete is handled by the FK cascade above, not by an event).

Trade-off explicitly accepted: there is **no immediate history** — value accrues over weeks as rows
accumulate. In exchange, Slice 1 is lighter (no backfill component, no reconstruction trustworthiness
risk) and the data model is honest by construction (the chart never claims history it never recorded).

## Alternatives Considered

### Alternative A — Backfill actual-item backlog/done from item dates (the superseded plan)
- Reconstruct `totalWork`/`doneWork` history into snapshot rows on day one from `WorkItem.CreatedDate`/
  `ClosedDate` + `FeatureStateTransition`, so the burnup shows full history on first open; forward-record
  only the forecasting series.
- **Rejected (user decision, 2026-06-02)**: the reconstruction is trustworthy only for the count of
  actual items and is fragile against re-opened items, bulk-import `CreatedDate` skew, and mid-flight
  feature re-scoping — a divergence from team memory that would have been the Slice-1 learning risk.
  It also has no in-repo precedent (a non-trivial new component). The product accepts delayed history
  in exchange for a lighter Slice 1 and a uniformly forward-only, honest model. See
  `design/upstream-changes.md` for the full change record.

### Alternative B — Per-metric tables (one table per series)
- `DeliveryBacklogSnapshot`, `DeliveryForecastSnapshot`, `DeliveryLikelihoodSnapshot`, …
- **Rejected**: 4-5 EF migrations and 4-5 joins-by-date to assemble one chart payload; the series
  share the same grain (`deliveryId`, `recordedAt`) so the split buys nothing but `Maintainability`
  cost. Forward columns accrue at different times, which a single wide row models cleanly with
  nullables.

### Alternative C — Narrow key-value metric-sample table (`metricKey`, `value`)
- `(deliveryId, recordedAt, metricKey, value)` long-format.
- **Rejected**: loses type safety (everything `double`, the when-distribution is JSON anyway so it
  doesn't fit a scalar `value`); every read pivots rows→columns; SonarCloud-hostile stringly-typed
  metric keys; the grain is fixed (one delivery per day) so wide rows are simpler and cheaper to
  query. See ADR-050 for the schema-granularity decision in detail.

## Consequences

**Positive**:
- One read path AND one write path; charts assemble from one ordered row set fed by one recorder.
- Slice 1 is lighter (~1-1.5 days): store + migration + the `PortfolioForecastsUpdated` event + its
  `DeliveryMetricSnapshotRecordingHandler` + first chart, with no backfill component and no
  reconstruction-trustworthiness risk to carry.
- The model is honest by construction — every series builds forward from the first recorded day; the
  chart never shows reconstructed history it cannot stand behind.
- Forward columns accrue independently as nullables; no schema churn per slice (Slices 2-4 only start
  populating a column — see ADR-050).
- Testable: the recorder unit-tests against EF InMemory fixtures; the migration test runs on a real
  provider (the persisted-model EF-migration trap that InMemory misses).

**Negative**:
- **No immediate history**: the burnup starts empty at launch and fills one day at a time; the value
  accrues over weeks. Accepted as the product trade-off for a lighter, honest Slice 1.
- Per-day-per-delivery row volume grows over a delivery's life. At MVP scale (one row per delivery
  per day) this is small; documented as a guardrail (KPI 4) with a retention follow-up if needed.

## Earned Trust — probing the dependencies this store relies on

This store depends on the EF migration applying on a real provider (not just InMemory) and on the
recorder reading the delivery's current counts and fresh forecasts honestly.

- **Migration probe**: the migration test applies the generated migration on a real (SQLite +
  Postgres) provider — not InMemory — per the persisted-model EF-migration trap. A migration that
  fails to apply refuses the build, not production.
- **Recorder-correctness probe**: the Slice-1 integration test runs the recorder against a fixture
  delivery with known current backlog/done counts and asserts the day's row carries exactly those
  counts; a re-open in the fixture is asserted to lower the next recorded `doneWork`.
- **Idempotency probe**: re-running the recorder the same day is asserted a no-op that overwrites
  today's row in place (guard on `(deliveryId, recordedAt)`, NOT a `=true` sentinel — the
  forecast-minimum-data-guard non-idempotency lesson). See ADR-049.
