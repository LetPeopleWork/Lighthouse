# ADR-101: Named Cycle Time Trend — `cycleTimePercentilesInfo` Gains an Additive Optional `definitionId`; Cache Key MUST Segment by Definition; Invalid Definition Returns the Empty-Series Info (Sibling Parity), NOT a Default Fallback

**Status**: Accepted (2026-07-17 — Morgan, interaction mode PROPOSE; user-confirmed)
**Date**: 2026-07-17
**Feature**: flow-overview-named-cycle-time (ADO Story #5509)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: extends the read contract of **ADR-062** (which added optional `definitionId` to `cycleTimePercentiles` and argued additive-param ⇒ no client gate) to `cycleTimePercentilesInfo`, and **amends ADR-062's field-vs-param graceful-degradation reasoning** (see §4). Consumes ADR-061 (`NamedCycleTimeDays`) and ADR-063 (validity SSOT). Resolves DISCUSS **D12**. Sibling to ADR-100 (this feature's RAG neutrality).

---

## Context

The Cycle Time Percentiles widget's Trend footer is fed by `cycleTimePercentilesInfo?.comparison` (`BaseMetricsView.tsx:1619`), served by `GET …/metrics/cycleTimePercentilesInfo?startDate&endDate` (`MetricsService.ts:711`, `TeamMetricsController.cs:362`). Story 5509 makes the widget's Trend follow the named selection (D12): "is our `Lead Time (End to End)` improving vs last period?" is the delivery lead's core question and the reason Trend cannot simply be hidden for named.

Code reality (verified):

- `cycleTimePercentilesInfo` takes **no** `definitionId` — it only knows the default window. Its sibling `cycleTimePercentiles` already takes an optional `definitionId` (ADR-062), routed through `IsNamedRequest(definitionId)` (`TeamMetricsController.cs:140-151`).
- `GetCycleTimePercentilesInfoForTeam` (`TeamMetricsService.cs:687`) computes the info by calling the percentile method **twice** — current period + previous period (`:693-697`) — then `BuildCycleTimePercentilesInfoDto`. The named percentile computation for one period already exists: `GetNamedCycleTimePercentilesForTeam` → `ComputeNamedDurations` (`TeamMetricsService.cs:340, 361`).
- **Two invalid-definition conventions already ship in this service**, and they differ:
  - `ComputeNamedDurations` (percentiles path) returns **`[]`** for a missing/invalid definition (`:363-366`).
  - The cumulative scoped path silently falls back to the **default unscoped** result when the definition is invalid (`scopeSuffix` empty, `TeamMetricsService.cs:401-404`).
- Cache key today is `CycleTimePercentilesInfo_{start}_{end}` (`TeamMetricsService.cs:691`) — **no definition segment**. The named percentile method already segments correctly: `NamedCycleTimePercentiles_{start}_{end}_Def_{definitionId}` (`:344`); the cumulative path uses a `scopeSuffix` = `_Def_{id}` (`:404`).

---

## Decision

### 1. Additive optional `definitionId` on `cycleTimePercentilesInfo` (Team + Portfolio)

```
GET …/metrics/cycleTimePercentilesInfo?startDate&endDate[&definitionId=<int>]   [RbacGuard(TeamRead/PortfolioRead)]
```

- `definitionId` absent/0/null ⇒ today's default info, **byte-identical**.
- `definitionId` present ⇒ current-vs-previous comparison computed over that definition's named series. Routed through the same `IsNamedRequest(definitionId)` idiom the sibling `cycleTimePercentiles` uses — no new routing concept.

### 2. Named info reuses the named percentile computation, called twice

The named path mirrors the default path structurally: call `GetNamedCycleTimePercentilesForTeam(team, start, end, definitionId)` for the current period and `(previousStart, previousEnd, definitionId)` for the previous, then feed the existing `BuildCycleTimePercentilesInfoDto`. **No new DTO, no new builder** — the shape is identical; only the two percentile inputs change source. (Portfolio twin identical against `GetNamedCycleTimePercentilesForPortfolio`.)

### 3. Cache key MUST gain a `_Def_{definitionId}` segment (hard correctness rule)

```
default : CycleTimePercentilesInfo_{start}_{end}
named   : CycleTimePercentilesInfo_{start}_{end}_Def_{definitionId}
```

Use the SAME `_Def_{id}` convention already shipped by the named percentile method and the cumulative `scopeSuffix`. **Rationale — this is not cosmetic**: without the segment, a default-window request and a named request for the same entity+range collide on one cache entry; whichever populates first wins and the other silently receives the wrong window's comparison. The segment MUST be present per-definition (not merely "named vs default") so that two different named definitions for the same range also do not collide. This is the single most likely defect in the slice and is covered by an explicit AC, not left to review.

### 4. Invalid definition ⇒ empty-series info (sibling parity), NOT a default fallback

When `definitionId` names a missing or invalid definition (ADR-063), the named path returns the info built over an **empty** series (mirroring `ComputeNamedDurations` returning `[]`), never a 500 and **never a silent fall-through to the default window's comparison**.

This deliberately follows the **percentiles** sibling (`[]`), NOT the cumulative scoped path (which falls back to default). Reason: `cycleTimePercentilesInfo` is the Info-twin of `cycleTimePercentiles` and feeds the SAME widget's footer as the percentile body; the two MUST agree, or the widget would show named percentiles over an empty series while its trend silently compared the *default* window — a within-widget contradiction. The cumulative path's fallback is a different surface with a different (already-shipped) choice and is out of scope to change here.

### 5. Client version-gate consequence — no new gate, but the field-vs-param asymmetry is recorded

Additive optional query param on an existing endpoint ⇒ **no `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry** (consistent with ADR-062 and ADR-055's rule for additive changes). The clients do not currently wrap `cycleTimePercentilesInfo` at all, so there is nothing to gate today.

**Amendment to ADR-062's reasoning (recorded, not a behaviour change):** ADR-062 §4 stated additive `definitionId` "degrades gracefully on old servers — no opaque 404." That is true for an additive *response field* (an old server omits it; the caller sees absence and copes). It is **weaker for an additive query *param***: an old server *ignores* the unknown param and returns the **default** percentiles/info under a *named* request — a silent wrong answer (HTTP 200), not a detectable 404. The graceful-degradation guarantee therefore does not fully transfer from field to param. This does not change the no-gate decision **for the Lighthouse frontend** (it always talks to its own matched-version backend). It does mean: **if/when the Lighthouse-Clients wrap `definitionId`-bearing metric reads**, they SHOULD add a version gate despite the "additive param" classification, because the failure mode is a silent wrong answer rather than a clear error. Recorded here so the clients-repo decision at wrap-time is made with eyes open. (There is a separately-tracked pre-existing instance: the clients already forward `definitionId` on `getTeamCycleTimePercentiles` with no gate — see the feature-delta cross-cutting note.)

---

## Alternatives Considered

**Chosen: additive `definitionId` + `_Def_{id}` cache segment + empty-series-on-invalid (sibling parity).**

- Pros: mirrors the shipped sibling `cycleTimePercentiles` exactly (same routing, same DTO, same invalid convention); the named computation is already built and merely called twice; zero new contract shape; the cache segment reuses an existing idiom. Trend answers the persona's "is it improving?" on the widget they already read.
- Cons: two extra percentile computations per named info request (current + previous). Contained — same cost profile as the default info, reusing the cached named percentile method per period.

**Rejected: hide Trend for named (no backend change).**

- Cheapest, but drops the persona's core question ("is our custom window improving?") precisely when they've switched to the custom window. The backend change is small and the value is high. Rejected.

**Rejected: new `namedCycleTimePercentilesInfo` endpoint.**

- A new route ⇒ a real client version-gate + opaque-404 surface + duplicated DTO/builder scaffolding, to serve a shape identical to the existing info. Additive param on the existing endpoint is strictly simpler. Rejected (same reasoning as ADR-062 rejecting a by-id endpoint).

**Rejected: invalid definition falls back to the default comparison (matching the cumulative path).**

- Would make the trend "work" for an invalid definition, but by silently showing the *default* window's trend under a *named* request — the exact silent-wrong-answer failure this ADR is trying to prevent, and it would contradict the widget's own percentile body (which shows the empty named series). Sibling parity with `cycleTimePercentiles` (`[]`) is the correct convention here. Rejected.

---

## Consequences

**Positive**:
- Trend follows the named selection with no new contract shape; the default trend is byte-identical.
- The cache segment closes the highest-probability defect in the slice by construction + AC.
- Invalid-definition behaviour is consistent *within the widget* (percentile body and trend both reflect the empty named series).

**Negative**:
- Two percentile computations per named info request. Accepted (parity with default info; cached per period + definition).

**Neutral**:
- The field-vs-param asymmetry is now documented; it constrains a *future* clients decision but changes nothing for the frontend today.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `definitionId` absent ⇒ default info byte-identical | Integration golden-equality test (Team + Portfolio) |
| `definitionId` present ⇒ comparison over the named series; PHX-style fixture matches `GetNamedCycleTimePercentiles` per period | `Team/PortfolioMetricsControllerTests` / service tests |
| Cache key segments by definition; default and named for same range DO NOT collide; two named defs DO NOT collide | Service test: populate default, then request named, assert values differ (not merely non-null); repeat for two named ids |
| Invalid/missing definition ⇒ info over empty series, never 500, never default fallback | Service test: removed boundary state ⇒ empty-series info, not the default comparison |
| No new endpoint route; additive param only | Route inventory / integration test |
| No new client version-gate for the FE; asymmetry recorded for clients wrap-time | This ADR §5 + clients-repo handoff note |

---

## Cross-feature impact

- **Default `cycleTimePercentilesInfo` callers** (existing Flow Overview default trend, existing clients if any): UNCHANGED (no `definitionId` ⇒ default).
- **ADR-062**: its additive-param no-gate reasoning is amended (field-vs-param asymmetry) without changing the FE no-gate outcome; §5 is the durable record.
- **`state-time-cumulative-view`**: unaffected — its scoped cumulative path keeps its own (different) invalid-definition fallback; this ADR deliberately does not unify the two conventions (out of scope; would touch a shipped surface).
- **Lighthouse-Clients**: no new gate now; a documented SHOULD-gate at future wrap-time for `definitionId`-bearing metric reads.
