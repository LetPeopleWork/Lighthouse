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


## Amendment (slice-02, 2026-07-25) — metric-family selection is explicit via `metricType`, not implicit via an omitted horizon

**Status**: Accepted. Clarifies Decision item 1; the endpoint count, shapes and rejected alternatives
are unchanged.

Decision item 1 above says the percentiles endpoint serves WIA when it is "requested without a
horizon". Slice-02 made the selection **explicit** instead:

```
GET .../metrics/percentiles-over-time?horizon={30|60|90}          -> CycleTime  (unchanged)
GET .../metrics/percentiles-over-time?metricType=WorkItemAge      -> WorkItemAge
```

The controller signature is additive with a default:

```csharp
GetPercentilesOverTime(int teamId, [FromQuery] int? horizon, [FromQuery] MetricType metricType = MetricType.CycleTime)
```

**Why implicit selection could not work.** Slice-01 had already shipped `[FromQuery] int? horizon`.
An omitted horizon is therefore a **legal cycle-time request** on the shipped contract (it means "all
CT horizons"), so "no horizon ⇒ WIA" would have re-interpreted an existing, already-valid request
shape — a silent breaking change dressed as a default. With an explicit `metricType`, the two
meanings stay separable and the pre-existing shape keeps its pre-existing meaning.

**Horizon is ignored, not matched, for WIA.** A caller switching tabs may carry `?horizon=30` over
from a cycle-time tab. `PercentilesOverTimeSeriesQuery.ResolveHorizon` discards it and substitutes
the horizon-less sentinel (ADR-106 amendment), because matching it literally would return an empty
series — the honest-empty-state path — for data that exists. Being lenient here is deliberate: the
horizon is meaningless for a family measured as-of-today, so rejecting the request with a 400 would
punish a caller for sending an irrelevant parameter.

**Compatibility.** Purely additive: one optional query parameter with a default equal to the previous
hard-coded behaviour. No existing request changes on the wire, no response DTO changes shape (the
`metricType` field was already in `PercentilesOverTimeSnapshotDto` from slice-01). Therefore:
**no CLI/MCP client version gate**, no RBAC change — same conclusion as the original Consequences.

Frontend counterpart: `PercentilesSelection = "age" | 30 | 60 | 90`, with the service building
`metricType=WorkItemAge` for `"age"` and `horizon={n}` otherwise.

## Amendment (slice-03b, 2026-07-26) — both series endpoints take an optional `startDate`/`endDate` window

**Status**: Accepted. Extends the two endpoints' request shape; the endpoint count, the response shapes,
the read-only property and the rejected alternatives are all unchanged. This is an **amendment, not a
supersession** (ADO #5564, DISCUSS D9).

Both endpoints gain two optional query parameters, on team and portfolio scope alike:

```
GET .../metrics/percentiles-over-time?horizon=30&startDate=2026-07-01&endDate=2026-07-26
GET .../metrics/process-behavior-over-time?type=Throughput&startDate=2026-07-01&endDate=2026-07-26
```

```csharp
GetPercentilesOverTime(int teamId, [FromQuery] int? horizon,
    [FromQuery] MetricType metricType = MetricType.CycleTime,
    [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)

GetProcessBehaviorOverTime(int teamId,
    [FromQuery] ProcessBehaviorMetricType type = ProcessBehaviorMetricType.Throughput,
    [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
```

**Semantics.** Filtering is on `RecordedAt` and **inclusive at both ends**. Either parameter may be
omitted independently: no `startDate` means "from the first recorded day", no `endDate` means "to the
last recorded day", neither means the full history — so every request shape that was legal before this
amendment keeps its previous meaning byte-for-byte on the wire. The parameter **names and types match
the sibling date-ranged actions on the same controllers** (`blockedCountHistory`,
`estimationVsCycleTime`, the PBC and percentile point-in-time reads) — `startDate`/`endDate` as
`DateTime`, converted with `DateOnly.FromDateTime(x.Date)` at the controller boundary because
`RecordedAt` is a `DateOnly`.

**Still read-only.** The window is a filter on persisted rows, never a recompute trigger: a narrowed
range plots fewer of the days the recording pipeline already judged honest. The ADR-108 property that
"the widget re-plots the days the pipeline recorded, it never triggers a recompute" is preserved
verbatim, which is why this is not a new endpoint and not a new port.

**Filtering stays server-side.** `RepositoryBase.GetAllByPredicate` returns `IQueryable<T>`, so the
date predicate composes into the same SQL as the owner/type predicate. No path materialises the full
series and filters in memory.

**Inverted range is rejected with 400**, using the controllers' existing
`StartDateMustBeBeforeEndDateErrorMessage` — the same guard `estimationVsCycleTime` and
`blockedCountHistory` already apply, and consistent with this endpoint pair's existing choice to 400 an
unknown `type` rather than answer it with an empty 200. The reason is the empty-state contract below: a
silently-swapped window returns an empty series, which the widget would then label "no data recorded in
the selected range" — an honest-*looking* message for what is actually a caller error. The guard only
fires when **both** parameters are present; a lone `startDate` or `endDate` has nothing to invert
against. (Note: this reverses the slice brief's original "stay lenient, no 400" out-of-scope line. The
leniency precedent in the slice-02 amendment above is about *ignoring an irrelevant* parameter, not
about accepting a self-contradictory one.)

**Empty-state disambiguation stays client-side (DISCUSS D10) — and the discriminator is the range's
end, not "narrowed vs default".** No envelope, no discriminator field, no second unfiltered request;
the original ADR-108 rejection of a discriminated envelope stands. The widget decides from the range it
asked for:

| Series empty and… | Copy | Why it is true |
|---|---|---|
| the range **ends today or later** | `builds forward from today — no snapshots recorded yet` (unchanged D6 copy) | recording is forward-only and per-day, so a window that includes today would contain a point if recording had run |
| the range **ends before today** | `no data recorded in the selected range` | the forward-only sentence would be a lie about a past window on an owner that may well have history |

The DISCUSS brief proposed "narrowed range ⇒ in-range copy, default range ⇒ forward-only copy". That
predicate is **not implementable**: there is no unfiltered state in the UI. `BaseMetricsView`'s date
pickers default to `getDefaultStartDate(defaultDateRange)`..today — a bounded window, 30 days for teams
(user-configurable via the team's `dateRange` setting) and 90 for portfolios — so *every* request the
dashboard makes is a narrowed one. The range-end predicate is equivalent for the two states that
matter, and unlike the original it is decidable from what the widget holds.

**Known defect, not an accepted edge** (corrected 2026-07-26 after adversarial review; the first
version of this paragraph claimed the state was unreachable and the message merely imprecise — both
claims were wrong). An owner whose snapshots ALL predate the selected window, where the window still
ends today, reads the forward-only copy. That reads as "no snapshots recorded yet" for an owner that
may have months of history, which is **false**, not imprecise — and it is exactly the second clause of
`OUT-5427-empty-state-honesty` ("0 charts claim 'no snapshots recorded yet' when the only reason the
series is empty is a narrowed range").

It is also an ordinary state, not an unreachable one: a team whose work-tracking connection broke more
than `defaultDateRange` days ago (revoked token, deleted project, team dropped from the refresh) stops
recording while keeping every snapshot it already has. The original reasoning — "recording runs on
every refresh, so this cannot happen for a refreshing instance" — only covers instances that are still
refreshing *that owner*.

The predicate stays as DDD-13 locked it, because the alternative needs something the widget does not
have: knowledge of whether the owner has any snapshots at all outside the window. Closing it honestly
means either a cheap "has any history" signal on the response (which reopens the envelope question
ADR-108 rejected) or a second unfiltered request (which D10 rejected). Tracked as a follow-up rather
than solved here, and no longer described as acceptable.

**User-visible consequence**: the two over-time widgets now show at most the dashboard's default window
(30 days team / 90 portfolio) instead of all recorded history, until the user widens the pickers. That
is the requested behaviour — it makes these two widgets agree with every sibling widget on the same
dashboard — and `docs/metrics/predictability.md`'s two **Affected by Filtering** rows change from *No*
to *Yes* accordingly.

**Compatibility.** Additive: two optional query parameters whose omission reproduces the previous
behaviour exactly. No response DTO changes shape. Frontend counterpart: `IMetricsService`'s two methods
take `startDate`/`endDate` as **required** `Date`s (the dashboard always has a range, and every sibling
service method is shaped that way), serialised through the existing `getDateFormatString` helper. Both
hooks' caches re-key from selection-alone to selection-plus-range. Therefore: **no CLI/MCP client
version gate** (no client consumes these two endpoints), no RBAC change, no migration — same conclusion
as the original Consequences.
