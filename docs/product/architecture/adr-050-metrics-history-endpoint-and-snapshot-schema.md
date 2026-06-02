# ADR-050: Single metrics-history Endpoint and Wide Nullable-Column Snapshot Schema

**Status**: Proposed (2026-06-02 — Morgan, interaction mode PROPOSE; pending user confirmation)
**Date**: 2026-06-02
**Feature**: delivery-metrics (Epic 3993)
**Decider**: Morgan (Solution Architect)
**Relates to**: ADR-048 (store), ADR-049 (recorder); DISCUSS driving-ports table (lists both a
`metrics-history` and a `metrics-history/forecast` endpoint and flags the choice to DESIGN);
cross-cutting Lighthouse-Clients version-gating obligation

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993

---

## Context

Two coupled shape questions DISCUSS deferred to DESIGN:

1. **Endpoint shape** — the driving-ports table lists `GET .../deliveries/{deliveryId}/metrics-history`
   (Slice 1, forward-recorded actual-item series) AND a sibling `GET .../deliveries/{deliveryId}/metrics-history/forecast`
   (Slices 3/4, forward forecast + likelihood series), and explicitly says the latter "may be folded
   into the history endpoint as additional series — a DESIGN choice."
2. **Snapshot schema granularity** — wide row vs narrow key-value vs per-metric tables (ADR-048
   chose wide; this ADR pins the exact shape and the FE contract).

Each NEW endpoint carries a Lighthouse-Clients version-gating obligation (one registry entry in
`FEATURE_REQUIRES_SERVER_NEWER_THAN`, pinned strictly newer than the last released Lighthouse).
Fewer endpoints = fewer registry entries and one opaque-404 pre-check.

The DeliveriesController routes are `api/v1/[controller]` + `api/latest/[controller]` with
`[RbacGuard(PortfolioRead)]` per existing precedent.

## Decision

**One endpoint, all series.**

`GET /api/v1/deliveries/{deliveryId}/metrics-history` (and `api/latest/…`), `[RbacGuard(PortfolioRead, ScopeIdRouteKey resolved from the delivery's portfolio)]`,
returns the full ordered series set from the one store:

```
{
  deliveryDate: string,            // the delivery target date (for the on-track marker, D8)
  firstSnapshotDate: string | null,// for the "builds forward from {date}" annotations (D6)
  points: [{
    date: string,
    totalWork: number,             // actual-item backlog (forward-recorded, Slice 1)
    doneWork: number,
    remainingWork: number,
    estimatedTotalWork: number | null,   // inferred estimate (forward, Slice 2 / US-01b)
    forecastHowMany: number | null,      // forecast-to-target-date (forward, Slice 3 / US-03)
    likelihoodPercentage: number | null, // (forward, Slice 4 / US-04)
    whenDistribution: { probability: number, expectedDate: string }[] | null // (forward, Slice 4)
  }]
}
```

The forward-only forecasting fields are `null` on days before the forward recorder recorded them —
the chart renders them as absent series / sparse bands with the D6 honest annotation, never as
zero. The whole series is empty until recording begins (no reconstructed history). Empty
`points: []` when the delivery has no items or no snapshots have been recorded yet.

**Schema** (`DeliveryMetricSnapshot`, one wide row per `(deliveryId, recordedAt.Date)`):

| Column | Type | Feed | Nullable |
|---|---|---|---|
| `Id` | int (PK) | — | no |
| `DeliveryId` | int (FK → Delivery, cascade) | both | no |
| `RecordedAt` | DateTime (UTC, date grain) | both | no |
| `TotalWork` | int | recorder | no |
| `DoneWork` | int | recorder | no |
| `RemainingWork` | int | recorder | no |
| `EstimatedTotalWork` | int? | recorder (Slice 2) | yes |
| `ForecastHowMany` | int? | recorder (Slice 3) | yes |
| `LikelihoodPercentage` | double? | recorder (Slice 4) | yes |
| `WhenDistributionJson` | string? (serialized percentiles) | recorder (Slice 4) | yes |

Unique index on `(DeliveryId, RecordedAt)`. All nullable forward columns are provisioned in the
**Slice-1 migration** so Slices 2-4 add no further migrations (they only start populating columns).
`WhenDistribution` is stored as JSON (a value-converted column, the established
`AdditionalFieldValues` / `StateMappings` JSON-column pattern in `LighthouseAppContext`) — it is a
percentile list, not a scalar.

FE contract is **schema-first / Zod** at the trust boundary: a `deliveryMetricsHistorySchema`
(Zod) parses the response and `z.infer` derives the TS type; forward fields are `.nullable()`.

## Alternatives Considered

### Endpoint — Alternative A: two endpoints (`metrics-history` + `metrics-history/forecast`)
- **Rejected**: two NEW endpoints = two Lighthouse-Clients registry entries and two opaque-404
  pre-checks; the FE would fire two requests and merge them by date for one chart; the forecast
  series share the exact grain and source table as the backlog series. One endpoint is simpler for
  the consumer and the client wrapper. The forward fields being null until they accrue gives the
  same "empty until recorded" behaviour without a second route.

### Schema — Alternative B: narrow key-value sample table
- `(deliveryId, recordedAt, metricKey, value)`.
- **Rejected (also in ADR-048)**: stringly-typed keys (SonarCloud-hostile), `whenDistribution`
  doesn't fit a scalar `value`, every read pivots long→wide. The grain is fixed at one delivery per
  day, so a wide row is the natural, type-safe shape.

### Schema — Alternative C: per-metric tables
- **Rejected (also in ADR-048)**: 4-5 migrations + 4-5 date-joins to assemble one payload for no
  modelling benefit at a shared grain.

## Consequences

**Positive**:
- One endpoint → one client registry entry (gated from Slice 1, covers Slices 2-4's added series
  with no new gate); one FE fetch; one Zod schema.
- Wide nullable schema models "forward columns accrue later" directly; no per-slice migration churn.
- Type-safe columns (no stringly-typed metric keys); JSON only where the data is genuinely a list.

**Negative**:
- The Slice-1 response shape carries forward fields that are always-null until Slices 2-4 ship —
  documented as intentional (Type-A additive walking skeleton); the FE tolerates nulls from day one.
- A future non-delivery-per-day grain (e.g. multiple snapshots per day) would need a schema rethink
  — out of scope (MVP is one row per delivery per day, D11/KPI 4).

## Earned Trust — probing the contract boundary

- **Schema-drift probe**: the FE Zod schema is the runtime probe that the server response matches
  the expected shape; a contract mismatch fails parse loudly at the boundary rather than rendering a
  silently-wrong chart. (No external integration here — this is an internal FE↔BE contract, so no
  Pact/consumer-driven contract test is recommended at the platform-architect handoff.)
- **Null-forward probe**: a Vitest test asserts the chart renders the actual-item series and the D6
  forward-only annotation when every forward field is null (the Slice-1-only / new-delivery case).
- **Version-gate probe**: the Lighthouse-Clients wrapper pre-checks server version against the
  registry and fails with a clear "upgrade Lighthouse" error instead of surfacing the opaque 404 an
  old server returns for this endpoint; dev/unparseable versions are never blocked.
