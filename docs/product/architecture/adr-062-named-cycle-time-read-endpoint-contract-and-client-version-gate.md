# ADR-062: Named Cycle Time Read Endpoint — Extend the EXISTING `cycleTimeData` / `cycleTimePercentiles` Endpoints with an Optional `definitionId` (Same `WorkItemDto` Contract), and the Lighthouse-Clients Version-Gate Consequence

**Status**: Accepted (2026-06-08 — Morgan; Fork 2 confirmed by user)
**Date**: 2026-06-08
**Feature**: multiple-cycle-times (Epic 5251)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: pairs with ADR-061 (computation). Follows ADR-055's client version-gate pattern (strictly-newer-than-last-release, `FEATURE_REQUIRES_SERVER_NEWER_THAN`). Resolves DISCUSS **D8** (NEW read endpoint vs reuse) and the cross-cutting Lighthouse-Clients gate.

---

## Context

DISCUSS locked (D8) that config writes ride the EXISTING settings endpoint and a NEW read serves per-definition scatter/percentile data; the cross-cutting checklist requires any NEW endpoint wrapped by the CLI/MCP clients to be version-gated.

Code reality (verified `API/TeamMetricsController.cs`):

- `GET …/metrics/cycleTimeData?startDate&endDate` returns `IEnumerable<WorkItemDto>` from `GetClosedItemsForTeam` — each `WorkItemDto` carries `CycleTime` (int, the default duration). The FE scatterplot (`CycleTimeScatterPlotChart.tsx`) plots `item.cycleTime` directly (`IGroupedWorkItem.cycleTime`, grouping key `${closedDateTimestamp}-${item.cycleTime}`).
- `GET …/metrics/cycleTimePercentiles?startDate&endDate` returns `IEnumerable<PercentileValue>` (50/70/85/95).
- Both are class-level `RbacGuard(TeamRead)` (PortfolioRead twin), share `startDate.Date > endDate.Date ⇒ 400`, and cache per entity by a date-keyed string.

The fork: (a) a definition-by-id endpoint that looks the saved definition up server-side; (b) an inline-boundaries endpoint taking `startState`/`endState`; (c) extend the EXISTING `cycleTimeData`/`cycleTimePercentiles` with an optional `definitionId`. Constraints: premium gating, client version-gate (ADR-055), cache keys, and that the scatter ALREADY calls the existing endpoints and plots `WorkItemDto.cycleTime`.

---

## Decision

### 1. Extend the existing endpoints with an optional `definitionId` (Option c)

```
GET …/metrics/cycleTimeData?startDate&endDate[&definitionId=<int>]          [RbacGuard(TeamRead/PortfolioRead)]
GET …/metrics/cycleTimePercentiles?startDate&endDate[&definitionId=<int>]   [RbacGuard(TeamRead/PortfolioRead)]
```

- **`definitionId` absent (or 0/null)** ⇒ today's behaviour, byte-for-byte (default cycle time). No change for any existing caller, including the FE default selection and the CLI/MCP if they call it.
- **`definitionId` present** ⇒ the controller looks the saved `CycleTimeDefinition` up on the owner's settings aggregate server-side, resolves its boundaries, and returns the SAME `IEnumerable<WorkItemDto>` shape with each item's `CycleTime` carrying the **named** ordered-boundary duration (ADR-061 §4). Percentiles endpoint returns the SAME `PercentileValue` shape over the named series.
- **Premium gate**: when `definitionId` is present, the feature is premium-gated server-side (the optional-feature key, `OptionalFeatureKeys`); a non-premium caller passing a `definitionId` is refused (403/feature-disabled) — UI never offers the selector to non-premium (`useRbac()`), so this is defence-in-depth, not the primary gate.
- **Invalid definition** (D5 — boundary state removed) ⇒ the endpoint returns a structured "definition invalid" signal (empty series + an `IsValid:false`-style flag carried alongside, NOT a 500), so the chart degrades to the disabled-with-warning state rather than crashing (D5/US-03). The validity verdict comes from the single source of truth (ADR-063), not a re-implementation here.

Because the **same `WorkItemDto` contract** is returned, the FE scatterplot's existing render path (`item.cycleTime`) works unchanged — the selector simply re-fetches the existing endpoint with `definitionId` and re-plots. This is the decisive reuse: option (a)/(b) would also return `WorkItemDto[]`, but a NEW route forces a NEW client method + a NEW version-gate touch-point, whereas extending an existing route with an additive optional query param does NOT.

### 2. Cache key includes `definitionId`

The metrics-service cache key gains a definition suffix when present (e.g. `CycleTimeData_{start}_{end}` → `CycleTimeData_{start}_{end}_Def_{definitionId}`), mirroring the `SelectionCacheSuffix` idiom already in `BaseMetricsService`. The default key is unchanged (no suffix when absent), so the default cache entries are untouched.

### 3. Lighthouse-Clients version-gate consequence — NO new gate required

This is the **key consequence of choosing (c) over (a)/(b)**. Per ADR-055's rule: a NEW endpoint wrapped by the clients MUST be version-gated; an **additive field/param on an EXISTING contract needs no gate**.

- `definitionId` is an **optional additive query param on the existing `cycleTimeData`/`cycleTimePercentiles` endpoints** ⇒ **NO version gate**. An old server simply ignores an unknown query param and returns the default series (graceful degrade), rather than an opaque 404. If the clients ever expose named-cycle-time reads, they pass `definitionId` to the existing wrapped method — no new `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry.
- The settings **write** (`CycleTimeDefinitions`) rides the existing settings endpoint as an additive field ⇒ **NO version gate** (D8, same as ADR-056 `waitStates`).
- **Net**: this feature introduces **zero new version-gate touch-points** — a concrete advantage of (c). (Contrast ADR-055, where a NEW `flowEfficiencyInfo` endpoint DID create one.) Recorded for the clients-repo handoff: clients pass `definitionId` to the existing method; no gate, no registry bump.

---

## Alternatives Considered

**Option C (chosen): extend existing endpoints with optional `definitionId`.**

- Pros: zero new routes ⇒ **zero new client version-gate touch-points** (additive query param degrades gracefully on old servers); the existing FE scatter render path (`WorkItemDto.cycleTime`) and the existing percentile path work unchanged — the selector just re-fetches; cache reuses the `SelectionCacheSuffix` idiom; server-side definition lookup keeps boundaries off the wire (no boundary tampering); one premium gate location.
- Cons: the existing endpoints gain a conditional branch (default vs named) — modest added responsibility on a shipped endpoint. Accepted: the branch is a thin "if definitionId, compute named series; else today's path" with the default path untouched.

**Option A: NEW definition-by-id endpoint `…/cycleTimeData/named?definitionId=`.**

- Pros: explicit, single-purpose route; the default endpoint stays single-responsibility.
- Cons: a NEW endpoint ⇒ a NEW client version-gate touch-point (ADR-055 rule) and an opaque 404 on old servers; duplicates the `WorkItemDto`-projection + RBAC + date-validation scaffolding of the existing endpoint for the same return shape. Rejected — the gate + duplication cost buys only nominal single-responsibility.

**Option B: inline-boundaries endpoint `…/cycleTimeData?startState=&endState=`.**

- Pros: no server-side definition lookup; stateless.
- Cons: puts boundary states on the wire (tampering surface, bypasses the saved-definition validity check D5), can't carry the definition's identity/name for telemetry (KPI 2 "selector-change to a non-Default value"), and is STILL a new query-shape the clients must gate/handle. The definition must be saved server-side anyway (US-02), so looking it up server-side by id is strictly better. Rejected.

---

## Consequences

**Positive**:

- Zero new endpoints ⇒ zero new client version-gate touch-points; old servers degrade gracefully (unknown param ignored), no opaque 404.
- The FE scatter + percentile render paths are unchanged — the named series reuses the `WorkItemDto.cycleTime` contract; the selector is the only new FE behaviour for the read.
- Server-side definition lookup keeps boundaries off the wire and routes the validity verdict (D5) through the single source of truth (ADR-063).

**Negative**:

- The existing `cycleTimeData`/`cycleTimePercentiles` endpoints carry a default-vs-named branch. Contained and additive; default path untouched.

**Neutral**:

- Premium gate on the named branch is defence-in-depth behind the `useRbac()` UI gate; consistent with the feature being premium-only.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `definitionId` absent ⇒ byte-identical default behaviour | Integration test: no-param request equals pre-feature golden response |
| `definitionId` present ⇒ same `WorkItemDto` shape, `CycleTime` = named duration | `TeamMetricsControllerTests`/`PortfolioMetricsControllerTests`: PHX-204 ⇒ `cycleTime == 47` for "Concept to Cash" |
| Named branch is premium-gated server-side | NUnit: non-premium + `definitionId` ⇒ refused; premium ⇒ series |
| Invalid definition ⇒ empty series + invalid flag, never 500 (D5) | Integration test: removed boundary state ⇒ no exception, invalid signal |
| Cache key includes `definitionId`; default key unchanged | NUnit cache-key assertion (mirrors `SelectionCacheSuffix`) |
| NO new client version-gate (additive param) | Clients-repo handoff note: pass `definitionId` to existing method, no `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry |

---

## Cross-feature impact

- Lighthouse-Clients (CLI + MCP): **no new gate** — additive optional param; the only feature surface to the clients is "pass `definitionId` to the existing cycle-time read if/when they expose named reads." Contrast ADR-055 (new endpoint → gate).
- Default scatter/percentile callers (FE Default selection, any existing client/MCP usage): UNCHANGED.
- `state-time-cumulative-view`: US-04's scoped cumulative read is handled in ADR-063 (extends `cumulativeStateTime`), parallel to this decision.
