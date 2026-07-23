# ADR-108: Two typed series endpoints on MetricsController, not one polymorphic envelope

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: epic-5427-percentiles-over-time (ADO Epic 5427)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Two widgets read the over-time series (DISCUSS D1): a combined **Percentiles-Over-Time** widget with a `[ WIA | CT-30 | CT-60 | CT-90 ]` toggle, and a separate **PBC-Over-Time** widget with a metric-type toggle. Their value shapes differ — four percentiles (`P50/P70/P85/P95`) vs three NPLs (`UNPL/Average/LNPL`) — matching the two-table split (ADR-106). DISCUSS driving-ports constraint: **extend the existing `MetricsController` surface**, do not fetch a new bespoke route from a component; UI plumbing flows through the existing `MetricsView` widget/hook conventions.

## Decision

Two typed read endpoints on the existing team and portfolio `MetricsController` surface (both scopes per D8), each returning a tight DTO matching its widget's toggle:

1. **Percentiles-over-time** — `GET .../metrics/percentiles-over-time?horizon={30|60|90}` (WIA requested without a horizon), returning a dated series of `{ recordedAt, metricType, p50, p70, p85, p95 }`. Switching `30 ↔ 60 ↔ 90` re-plots from already-persisted horizon series — no backend recompute of history (US-01 AC5).
2. **Process-behaviour-over-time** — `GET .../metrics/process-behavior-over-time?type={Throughput|WorkItemAge|Wip|CycleTime|Arrivals|FeatureSize}` returning a dated series of `{ recordedAt, unpl, average, lnpl }`. Feature-Size stays portfolio-only (D8, US-05 AC2).

Both are **read-only driving ports** — query methods only, no write surface; recording writes flow through the ADR-107 handlers' driven ports. Each endpoint reads its snapshot table via a read-only query port (`IPercentilesOverTimeSeriesQuery` / `IProcessBehaviorSeriesQuery`) and returns the honest empty series on a zero-snapshot owner, which the frontend renders as the D6 empty-state ("builds forward from today — no snapshots recorded yet"), never a broken axis.

## Alternatives considered

- **One polymorphic series endpoint** with a discriminated envelope (`{ seriesType, values: percentiles | npl }`): forces the frontend to branch on `seriesType` at runtime and weakens the TypeScript contract into a union of unlike shapes, blurring the read-port surface. **Rejected.**
- **One endpoint returning all series for a scope**: over-fetches (Feature-Size PBC is portfolio-only, so team scope would carry dead payload) and couples the two widgets' lifecycles into one call. **Rejected.**

## Consequences

- **Positive**: each widget gets an honestly-typed DTO with no runtime branching; the read contract mirrors the two-table (ADR-106) and two-widget (D1) split; extends `MetricsController` as required, no bespoke route; empty-state honesty is a property of the query (empty series), not special-casing in the controller.
- **Accepted cost**: two endpoints instead of one — deliberate; the shapes are genuinely different and DRY here would couple distinct business concepts.
- **Contract change is additive** — two new GET actions + two new response DTOs; no existing endpoint or DTO changes shape. Free-tier/ungated (D3): reads inherit the existing `MetricsController` read gate; no RBAC change, no CLI/MCP client version gate.
- Cross-refs [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) (tables read), [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) (write side), [ADR-069](./adr-069-blocked-count-snapshot-and-over-time-endpoint.md) (over-time endpoint precedent), [ADR-001](./adr-001-rbac-ui-gating-strategy.md) (read-gate inheritance).
