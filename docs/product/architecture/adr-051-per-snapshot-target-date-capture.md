# ADR-051: Per-Snapshot Target-Date Capture (`TargetDateAtSnapshot`, forward-only)

**Status**: Proposed (2026-06-04 — Morgan, interaction mode PROPOSE; pending user confirmation)
**Date**: 2026-06-04
**Feature**: delivery-target-date-tracking (Epic 3993 follow-up)
**Decider**: Morgan (Solution Architect)
**Relates to**: ADR-048 (snapshot store), ADR-049 (forward recorder + idempotency), ADR-050
(metrics-history endpoint + wide nullable schema). Extends all three.

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993 (stories #5174, #5175)

---

## Context

Every target-relative delivery metric — `LikelihoodPercentage`, the per-feature chance-of-being-late
(`100 − likelihood`) on the fever chart, and the burnup's delivery-date marker — is computed against
`Delivery.Date` (verified: `Delivery.CalculateMetrics` / `Feature.GetLikelhoodForDate(Date)`). The
`DeliveryMetricSnapshot` store records those computed values per day but **not** the target date they
were computed against, and `DeliveryMetricsHistoryDto` exposes only a single top-level `DeliveryDate`
(today's `Delivery.Date`).

Consequence: when the target moves, every recorded point is silently re-referenced against the new
target. A +2-week replan makes the recorded likelihood line step up — it reads as progress but is
goalpost-moving. To make the trend honest we must know the target **as it stood on each recorded
day**.

Constraint (epic-wide, ADR-048/049): the store is forward-recorded only. There is no reconstruction
of past state; values accrue from the day recording began.

## Decision

Add one nullable column to the existing wide snapshot row and capture it on the existing daily
recorder run.

- **Model** (`DeliveryMetricSnapshot`): `public DateTime? TargetDateAtSnapshot { get; set; }`.
- **Recorder** (`DeliveryMetricSnapshotRecordingHandler`): inside the existing per-delivery loop,
  `snapshot.TargetDateAtSnapshot = delivery.Date;` — captured at record time, same idempotent
  get-or-create-by-`(deliveryId, recordedAt.Date)` row (ADR-049). No new event, no new cadence, no
  immediate snapshot on a target-date edit (a move is captured at the next daily run).
- **Schema**: nullable; pre-existing rows stay `null` (forward-only — consistent with ADR-050's
  forward forecasting columns). One EF migration per provider via `Create-Migration.ps1`
  (Sqlite + Postgres); **never** `dotnet ef migrations add` directly.
- **DTO** (`DeliveryMetricsHistoryPointDto`): add `DateTime? TargetDateAtSnapshot`, mapped from the
  snapshot in `ToPoint`. Additive nullable field on the **existing** metrics-history response — no
  new endpoint, so no Lighthouse-Clients `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry and no version
  gate (that obligation guards NEW endpoints that old servers 404). `[RbacGuard(PortfolioRead)]` and
  the premium gate are unchanged.

## Alternatives Considered

### A — Reconstruct past targets from a Delivery audit/history table
- **Rejected**: no such history exists (`LighthouseAppContext` persists current `Delivery.Date`
  only), and reconstruction violates the epic's forward-only principle (ADR-048). Forward-recording
  the target is uniform with how every other series already accrues.

### B — Capture an immediate snapshot when the target is edited
- **Rejected**: requires a new domain event on the delivery-edit path and a second write trigger for
  one column; daily cadence is sufficient for a trend chart and keeps the single-feed model (ADR-049).
  Accepted cost: a same-day edit-and-revert is invisible.

### C — A separate `DeliveryTargetChange` table (event log of moves)
- **Rejected**: a new table + join for a value that fits one nullable column at the existing
  (delivery, day) grain; the wide-row precedent (ADR-048/050) explicitly favours columns over side
  tables at this grain. The per-day target is all the charts need; an exact move-timestamp log is not
  required for a daily trend.

## Consequences

**Positive**: one column, one assignment, one migration; reuses the recorder, the endpoint, the DTO
projection, and the FE `asNullableDate` boundary parser. No new contract surface; clients and RBAC
untouched.

**Negative**: forward-only — the stepped target line and change markers only become meaningful once a
delivery has accrued snapshots across at least one target change (weeks). Pre-migration rows are
`null`; the FE must fall back gracefully (covered by ADR-052).

## Earned Trust — probing the contract boundary

- **Migration-on-real-provider probe**: the new column is verified against Sqlite/Postgres, not just
  EF InMemory (which silently skips migrations — the recurring Lighthouse trap).
- **Recorder probe**: an integration assertion that a recorded snapshot has
  `TargetDateAtSnapshot == delivery.Date`, and that a same-day re-handle overwrites the row in place
  (idempotency preserved, ADR-049).
- **Null-forward probe**: the FE tolerates `null` `targetDateAtSnapshot` across the whole window
  (pre-migration history) without crashing — the fallback paths in ADR-052.
