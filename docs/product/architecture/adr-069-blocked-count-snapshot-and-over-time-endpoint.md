# ADR-069: Blocked-Over-Time Trend — New Sibling `BlockedCountSnapshot` Store Fed by a Forward Recorder on the Existing Refresh Cadence, New Read Endpoint with Client Version-Gate

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (Slice 03 — blocked-items over-time chart)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: reuses the forward-only snapshot PATTERN of ADR-048 (unified store), ADR-049 (event-driven forward recorder + date-keyed idempotency), ADR-050 (metrics-history endpoint + schema). Client version-gate follows ADR-055/ADR-062. Resolves DISCUSS D-CHART, D7, and OQ2 (chart lives in the Flow Metrics chart area).

---

## Context

Slice 03 plots the in-scope blocked COUNT as a forward-only daily time series in the **Flow Metrics chart area** (OQ2 — alongside the other flow-metrics charts, NOT folded into the overview widget). D7: forward-only daily snapshot of the blocked count per Team/Portfolio, the delivery-metrics history pattern, no per-item reconstruction.

Verified pattern (ADR-048/049/050): `DeliveryMetricSnapshot` is a wide row keyed `(deliveryId, recordedAt.Date)`, written by `DeliveryMetricSnapshotRecordingHandler : IDomainEventHandler<PortfolioForecastsUpdated>`, read via a metrics-history endpoint. Date-keyed upsert idempotency (NOT a `=true` sentinel). The store is scoped to deliveries — its grain is `deliveryId`, not `team/portfolio`.

Teams and portfolios refresh on their own cadence; `RefreshLog` records each refresh with `ExecutedAt`. The blocked count is `count(items where IsBlocked)` (ADR-067) in the owner's scope at refresh time.

The question: (1) extend `DeliveryMetricSnapshot` or a new store; (2) the recorder hook point; (3) the read endpoint shape + client gate.

---

## Decision

### 1. A NEW sibling store `BlockedCountSnapshot` — not an extension of `DeliveryMetricSnapshot`

```csharp
public class BlockedCountSnapshot
{
    public int Id { get; set; }
    public int OwnerId { get; set; }              // Team or Portfolio id
    public OwnerType OwnerType { get; set; }       // Team | Portfolio
    public DateOnly RecordedAt { get; set; }
    public int BlockedCount { get; set; }
}
```

Identity (unique index): `(OwnerId, OwnerType, RecordedAt)`. Persisted as a top-level DbSet with a migration via the `CreateMigration` PowerShell script (all providers). Driven port `IBlockedCountSnapshotRepository : IRepository<BlockedCountSnapshot>`.

**Why a new store, not extend `DeliveryMetricSnapshot`**: the grains differ. `DeliveryMetricSnapshot` is keyed by `deliveryId` (a delivery is a portfolio sub-entity); blocked count is keyed by team/portfolio OWNER. Bolting a nullable `blockedCount` onto the delivery-grained row would (a) leave it null for every delivery row and need a separate owner-grained row anyway, and (b) couple blocked recording to the `PortfolioForecastsUpdated` delivery-forecast lifecycle, which does not fire for teams. The forward-only PATTERN (forward recorder, date-keyed upsert, honest empty state) is reused; the wide-row STORE is not, because the grain is different.

### 2. Recorder hook point — forward recorder on the existing Team/Portfolio refresh, event-driven

A handler `BlockedCountSnapshotRecordingHandler` reacts to the existing refresh-completion signal for BOTH Team and Portfolio. Code reality: `PortfolioFeaturesRefreshed` exists; teams refresh through `UpdateServiceBase<Team>`. To avoid the ADR-049 staleness trap (recording before `IsBlocked` is fresh), the recorder fires AFTER the work-item/feature sync that recomputes blocked status:

- The recorder counts `items where IsBlocked` (via `IBlockedItemService`, ADR-067) in the owner's scope and upserts today's `BlockedCountSnapshot` row.
- Idempotency is date-keyed `(OwnerId, OwnerType, RecordedAt)` — same-day re-run overwrites today's row in place (NOT a `=true` sentinel; the forecast-minimum-data-guard lesson, ADR-049).
- Hook point: DELIVER selects the post-sync seam where blocked status is fresh (mirroring how `PortfolioFeaturesRefreshed`/`WorkItemBlocked` are dispatched after sync). If a clean post-blocked-recompute event does not exist for teams, a small `TeamWorkItemsRefreshed`-style event is added at the symmetric seam (consistent with ADR-049's "new event for the genuinely-fresh moment" rationale). Forward-only: no backfill, no reconstruction.

### 3. Read endpoint — NEW route, client version-gated

A new metrics-history read endpoint per owner (mirroring ADR-050's metrics-history shape):

```
GET .../metrics/blockedCountHistory?startDate&endDate   [RbacGuard(TeamRead / PortfolioRead)]
  → IEnumerable<BlockedCountSnapshotDto>   // { recordedAt, blockedCount }
```

- Returns the daily blocked count over the window; empty when no snapshots ⇒ the FE renders the honest forward-only empty state ("blocked trend builds forward from today — no snapshots yet"), never a flat zero (D7).
- The chart composes with the existing team/portfolio/type/date-range filter. **Type/scope filtering**: the snapshot stores the TOTAL in-scope blocked count per day; a per-type breakdown over time would require recording per-type counts. DECISION: record the total count only in slice 03 (the AC3 "respects the active filter" is satisfied for team/portfolio/date-range, which scope the OWNER and the window; work-item-type filtering of the historical series is deferred — a per-type snapshot column is an additive follow-up if demanded). This is recorded as an upstream clarification (see §AC note) — the slice-03 AC3 "filters to type Bug" is the one sub-case the forward total-count snapshot cannot serve without a per-type column; flagged to DISTILL.
- **Client version-gate (the decisive contrast with ADR-062)**: this is a NEW endpoint (new route), so an old server returns an opaque 404. Per the CLAUDE.md rule and ADR-055, the client wrapper MUST pre-check server version and fail with "upgrade Lighthouse", pinning `FEATURE_REQUIRES_SERVER_NEWER_THAN` strictly newer than the last released version (**v26.6.7.1** at design time — DELIVER bumps to the then-latest release). Unlike ADR-062's additive field (no gate), a new route DOES gate. Clients repo only — not edited here.

---

## Alternatives Considered

**Store — Option A (chosen): new sibling `BlockedCountSnapshot`, forward-only pattern reused.**
- Pros: correct grain (owner, not delivery); reuses the proven forward-recorder + date-keyed-upsert + honest-empty-state pattern; independent of the delivery-forecast lifecycle.
- Cons: a new table + migration + recorder. Idiomatic and small.

**Store — Option B: extend `DeliveryMetricSnapshot` with a `blockedCount` column.**
- Cons: grain mismatch (delivery vs owner) ⇒ nulls on every delivery row + a separate owner row anyway; couples team blocked-recording to a portfolio-delivery event that never fires for teams. Rejected.

**Recorder — Option C: reconstruct history from `WorkItemBlockedTransition` (slice 02 spells).**
- Pros: instant history.
- Cons: D7 + ADR-048 reject reconstruction (forward-only by product decision); spells are sync-cadence-approximate and pre-capture items have none ⇒ reconstructed counts would be wrong for the pre-capture window. Rejected — forward-only is the locked, honest model.

**Read — Option D: derive the count live at read time (no store).**
- Cons: a "trend" needs history; live read gives only today. Rejected — the whole job is the trend.

---

## Consequences

**Positive**:
- Reuses the forward-only pattern wholesale; the chart is honest by construction (empty until snapshots accrue).
- Correct owner grain; independent of the delivery lifecycle; counts derive from the single `IsBlocked` (ADR-067).

**Negative**:
- No immediate history (forward-only) — value accrues over weeks (accepted product trade-off, twin of ADR-048).
- New endpoint ⇒ client version-gate (unlike the additive fields in ADR-067/068).
- Historical per-TYPE breakdown not served by the total-count snapshot (slice-03 AC3 type sub-case) — flagged to DISTILL as an upstream clarification; per-type column is an additive follow-up.

---

## Earned Trust — probing the recorder

- **Freshness probe**: the recorder runs AFTER the sync that recomputes `IsBlocked`; an integration test asserts the recorded count equals `count(IsBlocked)` for a fixture owner at that moment (no stale read).
- **Idempotency probe**: same-day re-run overwrites today's row (one row per owner per day); the unique index `(OwnerId, OwnerType, RecordedAt)` is the DB backstop.
- **Single-definition probe**: the snapshot count equals the live `count(IsBlocked)` via `IBlockedItemService` (slice-03 @property AC4).
- **Empty-state probe**: an owner with no snapshots ⇒ the endpoint returns empty ⇒ the FE shows the forward-only empty state, not a zero line.
- **Migration probe**: real-provider (SQLite + Postgres) migration test; InMemory misses it.
- **Schema forward-compatibility probe (per-type deferral, UC-2)**: the unique index `(OwnerId, OwnerType, RecordedAt)` remains valid if a future per-type snapshot adds an `ItemType` discriminator — that enhancement adds a NEW row grain `(OwnerId, OwnerType, RecordedAt, ItemType)` as an additive sibling column + a widened index, NOT a migration of the existing total-count rows. The slice-03 total-count design does NOT foreclose the later per-type follow-up; the deferred per-type filtering is purely additive. (Recorded so UC-2's deferral carries an explicit no-rework guarantee.)

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| One row per owner per day (date-keyed upsert, no sentinel) | NUnit: re-run same day ⇒ one row; unique index `(OwnerId, OwnerType, RecordedAt)` |
| Snapshot count == live `count(IsBlocked)` (ADR-067) | Integration property test at record time |
| Forward-only: no reconstruction path | ArchUnitNET / code review: recorder has no backfill; empty owner ⇒ empty series |
| New endpoint ⇒ client version-gate `> v26.6.7.1` | Clients-repo handoff note; `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry entry (DELIVER) |
| Read is `RbacGuard(TeamRead/PortfolioRead)`, no write side effect | Controller attribute test; GET has no recorder call (ADR-049 Alternative B rejection) |

---

## Cross-feature impact

- ADR-048/049/050: pattern reused (forward recorder, date-keyed idempotency, history endpoint); store is a sibling, not an extension (grain differs).
- ADR-067: counts derive from the single `IsBlocked`.
- Lighthouse-Clients: NEW endpoint ⇒ version-gate (contrast ADR-067/068 additive ⇒ no gate). See ADR-072.
- DISTILL clarification: historical per-type filtering deferred (total-count snapshot).
