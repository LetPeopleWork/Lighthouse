# ADR-055: Flow Efficiency Overview Tile — Transport via a Small Dedicated `flowEfficiencyInfo` Endpoint (Established Tile Pattern), `trendPolicy: none`, and the Lighthouse-Clients Version-Gate Consequence

**Status**: Accepted (2026-06-05 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-05
**Feature**: wait-states-flow-efficiency (Story #5173)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: pairs with ADR-054 (derivation). Resolves DISCUSS-deferred decisions **D8(b)** (tile transport) and the **`trendPolicy`** part of US-03's technical notes.

---

## Context

DISCUSS deferred (D8b):

> the overview tile is served by a new small `flowEfficiency` endpoint per scope, **vs** folding the value into an existing overview/metrics payload — and the consequent Lighthouse-Clients version-gate decision.

The cross-cutting checklist (CLAUDE.md DISCUSS gate) makes the consequence explicit: a **NEW endpoint** wrapped by the CLI/MCP clients MUST be version-gated (`FEATURE_REQUIRES_SERVER_NEWER_THAN`, strictly newer than the last released Lighthouse version) so an old server's opaque 404 surfaces as a clear "upgrade Lighthouse" error; an **additive field on an existing response** needs no gate.

Code reality (verified against `API/TeamMetricsController.cs`): the existing `flow-overview` small KPI tiles are each served by a small, dedicated `…Info` endpoint — `wipOverviewInfo`, `featuresWorkedOnInfo`, `totalWorkItemAgeInfo`, `predictabilityScoreInfo`, `cycleTimePercentilesInfo`, `throughputInfo`, `arrivalsInfo`. There is **no single combined "overview payload"** the tiles share; each tile fetches its own small `…Info` DTO. The `flowEfficiency` tile (D7: `flow-overview`, `small`) is structurally one of this family.

The tile value must be the **whole in-scope set, never the picker** (D5/D18), and is computed server-side (ADR-054 §3).

---

## Decision

### 1. Tile served by a NEW small `flowEfficiencyInfo` endpoint per scope (the established tile pattern)

```
GET /api/teams/{teamId:int}/metrics/flowEfficiencyInfo?startDate&endDate            [RbacGuard(TeamRead)]
GET /api/portfolios/{portfolioId:int}/metrics/flowEfficiencyInfo?startDate&endDate  [RbacGuard(PortfolioRead)]
```

Response `FlowEfficiencyInfoDto`:

```csharp
public sealed record FlowEfficiencyInfoDto(
    bool   IsConfigured,        // false ⇒ "not configured" (D3), never 100%
    bool   HasDataInScope,      // false ⇒ "no data in scope" (D4)
    double EfficiencyPercent,   // meaningful only when IsConfigured && HasDataInScope
    double TotalDoingDays,
    double WaitDays);
```

It reuses the exact controller scaffolding of `wipOverviewInfo` / `totalWorkItemAgeInfo`: same route prefix, same `[FromQuery] DateTime startDate, endDate`, same `startDate.Date > endDate.Date ⇒ 400` validation, same class-level `RbacGuard`. The value is `BaseMetricsService.ComputeFlowEfficiency` (ADR-054 §3) over the whole D12-included set — NO `itemIds` parameter (the tile never narrows). Cache key `FlowEfficiency_{startDate}_{endDate}` via the existing `GetFromCacheIfExists`, scoped per entity (parallel to the other `…Info` keys).

**`IsConfigured` / `HasDataInScope` are explicit booleans, not magic sentinel values** — D3 ("not configured") and D4 ("no data in scope") are semantically different reads (DISCUSS distinguishes them), so the contract carries two distinct flags rather than overloading `-1` / `NaN`. The FE renders the three states (configured-with-value / not-configured / no-data) off these flags.

### 2. `trendPolicy: none` for `flowEfficiency`

The `flowEfficiency` entry in `categoryMetadata.ts` `trendPolicies` is `"none"` (no period-over-period delta arrow). Rationale: a meaningful efficiency trend needs a stored history, which is explicitly out of scope for MVP (DISCUSS "trend-over-time of flow efficiency — out of scope; could fold into the delivery-metrics over-time work later"). `"none"` matches the other "point-in-window" reads (`aging`, `blockedOverview`, `staleOverview`). A future history view can upgrade this to `previous-period` or a snapshot store without a contract break.

### 3. Lighthouse-Clients version-gate consequence (explicit)

Because `flowEfficiencyInfo` is a **NEW endpoint**, the cross-cutting rule applies **IF and only if the CLI/MCP clients wrap it**:

- **Config write (`waitStates`)**: rides the EXISTING settings endpoint as an additive field — **NO version gate** (additive field on an existing contract).
- **Chart efficiency number + wait-bar highlight**: FE-derived from the existing `cumulativeStateTime` + settings round-trips (ADR-054) — **NO new endpoint, NO version gate**.
- **Overview tile (`flowEfficiencyInfo`)**: NEW endpoint. **IF the clients wrap it**, the wrapping client method MUST be version-gated: pin the feature to **strictly newer than the last released Lighthouse version**, recording/bumping that baseline in the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry to the current latest release at wrap time, so an old server's opaque 404 becomes a clear "upgrade Lighthouse" error. Dev/unparseable versions must never be blocked. **IF the clients do NOT wrap the tile endpoint** (the tile is a product-UI surface; the CLI/MCP may have no need for a flow-efficiency read), the gate is N/A — record that decision in the clients repo at wrap-or-skip time.

This is the **only** new-endpoint surface in the feature, so it is the only version-gate touch-point. Flagged for the DELIVER/clients-repo handoff.

---

## Alternatives Considered

**Option A (chosen): small dedicated `flowEfficiencyInfo` endpoint per scope.**

- Pros: matches the established `…Info` tile idiom exactly (zero new architectural shape); tile value is server-computed over the whole set (never the picker — D5/D18 enforced by construction, no `itemIds` param exists); small payload; cacheable GET; the version-gate consequence is contained to one endpoint and is the standard new-endpoint rule.
- Cons: one new endpoint per scope (two total) ⇒ the version-gate touch-point IF clients wrap it. Accepted and flagged.

**Option B: fold `flowEfficiencyPercent` into an existing overview payload.**

- Pros: additive field ⇒ no client version gate.
- Cons: **there is no shared overview payload** — each tile has its own `…Info` endpoint; "folding" would mean overloading an unrelated tile's DTO (e.g. `wipOverviewInfo`) with a flow-efficiency value, coupling two unrelated tiles and breaking the one-tile-one-endpoint convention. The "no shared overview payload" code reality makes this option structurally unavailable without inventing the coupling it pretends to avoid. Rejected.

**Option C: derive the tile value client-side from a `cumulativeStateTime` fetch (reuse ADR-054 §1 fold).**

- Pros: no new endpoint, no version gate.
- Cons: forces the `flow-overview` tile to fetch the full `cumulativeStateTime` bar payload (a `flow-metrics` chart endpoint) purely to sum it — a cross-category data dependency the overview tiles deliberately avoid (each tile fetches its own small read). It would also have to strip any picker influence. Heavier and category-crossing for a single number. Rejected — but noted as the fallback if a future "minimise endpoints" constraint ever outweighs the category-cleanliness.

---

## Consequences

**Positive**:

- The tile uses the exact existing `…Info` pattern — predictable to implement, cache, test, and gate.
- `IsConfigured` / `HasDataInScope` flags make D3 vs D4 a contract-level distinction, eliminating the "not configured reads 100%" failure mode at the source (registry/risk item).
- The version-gate consequence is contained to one endpoint and is the standard rule, fully spelled out for the clients repo.

**Negative**:

- One new endpoint per scope. The version gate (if clients wrap it) is a small, well-understood follow-up in the clients repo.

**Neutral**:

- `trendPolicy: none` is the MVP choice; upgradable later without a contract break when an efficiency-history store lands.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `flowEfficiencyInfo` endpoints mirror the `wipOverviewInfo` shape (route prefix, `startDate/endDate`, `startDate.Date > endDate.Date ⇒ 400`, class-level `RbacGuard`) | Integration test in `TeamMetricsControllerTests` / `PortfolioMetricsControllerTests` |
| The tile endpoint takes NO `itemIds` (never follows the picker — D5/D18) | NUnit reflection / signature test; integration test asserts value is whole-set |
| D3 "not configured" returns `IsConfigured: false` (never `EfficiencyPercent: 100`) | NUnit: empty `WaitStates` ⇒ `IsConfigured == false`; FE Vitest renders "not configured", asserts NOT "100%" |
| D4 "no data in scope" returns `HasDataInScope: false`, no division | NUnit: zero Doing-time fixture ⇒ `HasDataInScope == false`, no exception |
| `flowEfficiency` `trendPolicy == "none"` | Vitest `categoryMetadata.test.ts` |
| Clients version-gate decision recorded (gate IF wrapped, else N/A) | DELIVER checklist item; clients-repo `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry or explicit N/A note |

---

## Cross-feature impact

- `state-time-cumulative-view`: the tile endpoint reuses `ComputeFlowEfficiency` (ADR-054 §3), itself a fold over the existing cumulative per-state computation. No change to the cumulative endpoints/DTOs.
- Lighthouse-Clients (CLI + MCP): the ONE version-gate touch-point in this feature. Config write + chart number + highlight are gate-free; only the tile endpoint (if wrapped) gates.
- Future flow-efficiency history view: would upgrade `trendPolicy` and add a snapshot store; this endpoint's `FlowEfficiencyInfoDto` is forward-compatible (additive fields only).
