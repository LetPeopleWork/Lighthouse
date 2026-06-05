# ADR-054: Flow Efficiency — Derivation From Existing Per-State Day Totals, FE-Computed Chart Number + Wait-Bar Flag (No New Cumulative Field), BE-Computed Tile Value

**Status**: Accepted (2026-06-05 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-05
**Feature**: wait-states-flow-efficiency (Story #5173, additive extension of `state-time-cumulative-view` Epic 4144)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: ADDS to (does not supersede) ADR-022 (cumulative algorithm), ADR-024/ADR-028 (no shared per-state aggregation; `itemIds` subset). Those remain Accepted and authoritative. This ADR resolves DISCUSS-deferred decision **D8(a)**.

---

## Context

DISCUSS locked the flow-efficiency formula (D2, mapping-aware via D11) but deferred to DESIGN **how the chart's efficiency number and the wait-bar flag are transported** (D8a):

> additive field(s) on the existing `cumulativeStateTime` response, **vs** derived purely client-side from the existing per-state bars + the settings' `WaitStates` list.

The formula (D2/D11):

```
efficiency      = activeDoingTime / totalDoingTime
totalDoingTime  = Σ over all Doing-category states S of totalDays[S]
waitTime        = Σ over states S ∈ GetRawStatesForCategory(WaitStates) of totalDays[S]
activeDoingTime = totalDoingTime − waitTime
```

Code reality (verified against `Lighthouse.Backend/.../Services/Implementation/BaseMetricsService.cs` and `Models/Metrics/CumulativeStateTimeDto.cs`):

- The existing `cumulativeStateTime` response is `{ states: [{ state, workflowOrder, totalDays, completedContributionDays, ongoingContributionDays, … }] }` — **one row per Doing-category workflow state, already carrying `totalDays` per state**, ordered by `workflowOrder`, computed over the D12-included set (and narrowed by `itemIds` per ADR-028).
- `WorkTrackingSystemOptionsOwner` already exposes `GetRawStatesForCategory(List<string>)` and `DoingStates`. The frontend already round-trips `stateMappings` and (after slice 01, ADR-056) `waitStates` on the settings object.
- The chart number must follow the US-05 picker per D5 (per-item when narrowed); the bars it reads are ALREADY narrowed by `itemIds`. The overview tile must NOT follow the picker (D5/D18) — it is always the whole in-scope set.

The efficiency value is therefore **a pure fold over the per-state `totalDays` the chart endpoint already returns**, partitioned by a list the client already holds. No new per-state aggregation pass is required (honours ADR-024/ADR-028 — no `IPerStateAggregationService`).

---

## Decision

### 1. Chart efficiency number (US-02) — FE-derived, NO new cumulative field

The chart's flow-efficiency number is computed **client-side** by a NEW pure util `flowEfficiency.ts` from:

- the per-state `totalDays` already present in the `cumulativeStateTime` response the chart is rendering (already narrowed by the active `itemIds` picker selection — so per-item efficiency falls out for free as the n=1 case, mirroring ADR-028 §7's B2-distribution absorption), and
- the `waitStates` list + `stateMappings` already on the team/portfolio settings object the chart's parent holds, resolved through the SAME expansion `GetRawStatesForCategory` performs (a pure TS twin `resolveWaitRawStates(waitStates, stateMappings, doingStates)` co-located in `flowEfficiency.ts`).

```
flowEfficiency(states, waitRawStates) -> { efficiencyPercent, totalDoingDays, waitDays, status } | null
  totalDoingDays = Σ states[*].totalDays                       // all Doing rows in the response
  waitDays       = Σ states[s].totalDays where s ∈ waitRawStates
  if waitStates empty           -> null  (D3 "not configured", caller renders the not-configured copy)
  if totalDoingDays <= 0        -> { status: "no-data" }  (D4, no division)
  else efficiencyPercent = round((totalDoingDays - waitDays) / totalDoingDays * 100)
```

The `cumulativeStateTime` response shape is **UNCHANGED**. No `efficiency` field is added to it; no `isWaitState` flag is added to its rows. The chart number and the wait-bar flag are both presentation reads over data the client already has.

**Rationale**: the contract stays minimal and numerically comparable (same principle as ADR-028 §4's FE-owned `formatDuration` — heights/units are an FE concern, and so is this fold). Adding an `efficiency` field would (a) bloat every cumulative response with a value the client can trivially derive, (b) require the BE to re-decide it on every `itemIds`-narrowed fetch, and (c) risk two formula sources drifting. One formula source for the chart (`flowEfficiency.ts`), reused by the wait-bar flag (ADR-057), keeps the shared-artifact (registry HIGH-risk) single-sourced.

### 2. Wait-bar flag (US-04) — FE-derived from the SAME resolution

The wait-bar colour-highlight (ADR-057) reads `isWait(state) = waitRawStates.has(state)` from the SAME `resolveWaitRawStates(...)` used by the number. No `isWaitState` boolean is added to the cumulative row. Single source: both the number and the highlight partition the same per-state rows by the same expanded raw-state set (closes the registry HIGH-risk divergence item — neither surface re-implements the expansion).

### 3. Overview tile value (US-03) — BE-computed via a small `…Info` endpoint (ADR-055)

The overview tile is the ONE surface that must NOT follow the picker and is NOT rendered alongside a cumulative response (it lives in the `flow-overview` category, a different view from the chart). For it, re-using the chart's client-side fold would force the overview to fetch the full `cumulativeStateTime` bar payload just to sum it. Instead the tile value is computed **server-side** over the whole in-scope set and served by a small dedicated endpoint following the established `wipOverviewInfo` / `totalWorkItemAgeInfo` tile pattern — see **ADR-055** (transport) for the endpoint-vs-fold decision and its Lighthouse-Clients version-gate consequence. The server-side computation reuses the SAME per-state `totalDays` fold (a new `protected` helper `ComputeFlowEfficiency` on `BaseMetricsService`, a thin fold over the existing per-state computation — ADR-024 upheld: no new shared service, no new interface).

### 4. Formula lives in exactly two places, by design

- **Frontend**: `flowEfficiency.ts` (chart number + wait-bar partition).
- **Backend**: `BaseMetricsService.ComputeFlowEfficiency` (overview tile).

This is a deliberate, enforced duplication of a 3-line fold across the FE/BE boundary — NOT shared knowledge that should be DRY'd (the FE computes over a response-in-hand for a picker-narrowed view; the BE computes over the whole set for a tile that never narrows). A cross-surface integration test (picker cleared ⇒ chart number == tile value) pins the two to agree, so drift is caught by behaviour, not prevented by a shared module that would couple two different scopes. This mirrors the project's "DRY = don't repeat knowledge, not code" rule (CLAUDE.md) and ADR-024's same-arithmetic-different-aggregation precedent.

---

## Alternatives Considered

**Option A (chosen): chart number + wait-flag FE-derived; tile BE-computed via small endpoint.**

- Pros: zero change to the `cumulativeStateTime` contract; per-item efficiency is the free n=1 case of the already-narrowed bars; one FE formula source for number + highlight; tile uses the established `…Info` tile pattern; no Clients version-gate for the chart/highlight (they ride the existing settings + cumulative round-trips).
- Cons: the formula exists on both sides of the FE/BE boundary (3-line fold) — accepted, pinned by a cross-surface equality test.

**Option B: add `efficiency` + per-row `isWaitState` to the `cumulativeStateTime` response.**

- Pros: single BE formula source; FE renders a value it is handed.
- Cons: bloats every cumulative response (incl. every picker-narrowed fetch) with a value the client can derive in 3 lines; the BE must re-resolve `WaitStates` expansion on every narrowed fetch; **breaks the contract's numeric minimality** (ADR-028 §4 keeps the contract numeric and FE owns presentation folds); the wait-flag still has to be mirrored on the FE for the highlight anyway. Rejected — more contract surface for no behavioural gain.

**Option C: a single new combined `flowEfficiency`-on-cumulative endpoint serving number + bars + tile.**

- Pros: one endpoint.
- Cons: conflates the picker-following chart scope with the never-narrowing tile scope into one payload, exactly the asymmetry ADR-028 §6/§7 warns against; the tile would then wrongly inherit the picker. Rejected.

---

## Consequences

**Positive**:

- The `cumulativeStateTime` contract is untouched — no breaking change, no client version-gate for the chart number or wait-bar highlight (they derive from existing round-trips). Confirmed against the Lighthouse-Clients cross-cutting note: chart/highlight are N/A for a version gate.
- Per-item efficiency (D5) is free: the chart already fetches `itemIds`-narrowed bars; the fold over those bars IS the per-item number.
- One FE formula source (`flowEfficiency.ts`) feeds both the number and the wait-bar partition — the registry HIGH-risk "two surfaces read different lists" item is closed structurally.
- The tile's small endpoint matches the existing overview-tile idiom (`wipOverviewInfo`, `totalWorkItemAgeInfo`) — no new architectural shape.

**Negative**:

- The 3-line fold is duplicated FE/BE. Mitigated: a cross-surface integration test asserts equality (picker-cleared chart number == tile value); both sides are mutation-tested ≥80%.

**Neutral**:

- The FE `resolveWaitRawStates` is a pure twin of `GetRawStatesForCategory`. It is exercised by Vitest at the mapping-name and raw-state cases; the BE side is exercised by NUnit. Cross-surface parity is asserted by the equality test.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The `cumulativeStateTime` response shape is UNCHANGED — no `efficiency` field, no `isWaitState` row flag | NUnit contract test on `CumulativeStateTimeDto` (row record unchanged); review |
| Chart number + wait-bar highlight both read `resolveWaitRawStates(...)` (single FE source) | Vitest: the number util and the highlight predicate import the SAME resolver; no second expansion implementation exists (grep-style test) |
| FE fold uses the per-state `totalDays` already in the response (no extra fetch for the number) | Vitest on `flowEfficiency.ts`: given a `states[]` + waitRawStates, returns the expected percent; n=1 (per-item) case covered |
| Tile value (BE) and chart number (FE, picker cleared) agree | Integration + Vitest cross-surface test: BE `ComputeFlowEfficiency` over the whole set == FE fold over the no-`itemIds` cumulative response |
| `ComputeFlowEfficiency` is a `protected` helper on `BaseMetricsService`, NOT a new interface/service (ADR-024 upheld) | NUnit reflection test + ArchUnitNET (extends ADR-024 rule) |
| Mapping-name wait state expands to all raw states on BOTH sides | NUnit (BE `GetRawStatesForCategory`) + Vitest (FE `resolveWaitRawStates`): fixture "Waiting" → ["Waiting for Review", "Blocked - External"] |

---

## Cross-feature impact

- `state-time-cumulative-view` (Epic 4144): UNCHANGED contract. This feature reads its response and its settings round-trip; it adds no field to the cumulative DTOs and no `itemIds` semantics change. ADR-022/024/028 all hold.
- `aging-pace-percentiles` (sibling F): UNCHANGED. No shared per-state aggregation introduced (ADR-024/ADR-018/ADR-021 upheld for the fifth time across this lineage).
- Future blocked-time epic (#5074): orthogonal — `BlockedStates` and `WaitStates` are independent overlays (D9; out-of-scope to reconcile).
