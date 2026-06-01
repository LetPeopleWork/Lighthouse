# Wave Decisions — aging-pace-percentiles / DESIGN (bug #5145 metric redesign)

Feature: aging-pace-percentiles (Epic 4144 MVP bundle, slice F)
Wave: DESIGN (revision)
Date: 2026-06-01
Architect: Morgan (Solution Architect), interaction mode = PROPOSE
ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5145

This records the DESIGN decisions for the **bug #5145 metric redesign** of the shipped
aging-pace feature. It supersedes the metric of D12 / DDD-1; all other DISCUSS / DESIGN
decisions for the feature stand.

## Key Decisions

| ID | Decision |
|---|---|
| D12 (revised) | Band metric = **cumulative reached-at-least-this-state population** over the team's mapped `Doing` states. Per mapped state `S`: population = every completed-in-window item that reached `S` or any state at-or-after `S` (mapped order); observation = last-exit cumulative age `(last exit from S).TransitionedAt − StartedDate`; skip / never-exited items imputed from their earliest at-or-after exit. Percentiles, window membership, day-counting, caching unchanged from ADR-019. Superset population on one monotonic clock ⇒ monotonicity + ≈ cycle-time-line alignment **by construction**. |
| DDD-1 (revised) | Implements the revised D12. Computation lives on the pace path only (`BuildWorkflowStateOrder` + the `BaseMetricsService` cumulative population helper + the two leaf `GetAgeInStatePercentilesFor*` methods). Operates only on normalized `SyncedTransitions` + mapped `DoingStates`; no per-connector branching. |
| D-5145-A | Option A locked. Option B (mapped-state filter only) folded in; Option C (FE `cummax` clamp) declined (semantically dishonest — manufactures a non-real percentile). |
| D-5145-CA | **Connector-agnostic constraint** (user-locked): correct for Jira, ADO, Linear, CSV, and any future source, with NO `switch (WorkTrackingSystem)`. The non-linear flow shape is source-independent. |
| D-5145-V | Verification: replace the masking synthetic perfectly-linear integration fixture with non-linear-flow fixtures asserting on the normalized `SyncedTransitions` model; live walking-skeleton uses the CSV / demo-data path carrying non-linear flows (skips / backward / unmapped), plus a real connector if available. |

## Architecture Summary

- **Pattern**: ports-and-adapters / hexagonal (unchanged). No new port, no new adapter. Transitions still read only via `IWorkItemStateTransitionRepository` (team) / `IFeatureStateTransitionRepository` (portfolio); the `BaseMetricsService` helper stays repo-free, taking pre-loaded transitions.
- **Paradigm**: OOP (C# .NET 8 backend); functional-leaning React 18 + TypeScript frontend (no FE change in this redesign — the values change, the rendering does not).
- **Key components (pace path)**: `BuildWorkflowStateOrder` (pace-exclusive), the cumulative reached-at-least population walk + skip/never-exited imputation helper inside `BaseMetricsService`, and the two leaf `GetAgeInStatePercentilesForTeam` / `…ForPortfolio` methods that load transitions via their own repository and delegate to the helper.
- **Earned-Trust note**: the redesign exists because a dependency (the *shape* of real transition data) was assumed honest and never probed; the replacement verification exercises the non-linear lie directly on the normalized model.

## Reuse Analysis

0 CREATE-NEW. Every touchpoint is an in-place change to an already-shipped pace-path component.

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `BaseMetricsService` pace helper (`ComputeAgeInStatePercentiles`) | `Lighthouse.Backend/.../Services/Implementation/BaseMetricsService.cs` | Already computes per-state pace percentiles from pre-loaded transitions | MODIFY (in place) | Swap the per-state-exit population for the cumulative reached-at-least population + imputation; extract a named helper for the per-item observation to keep cognitive complexity under the Sonar `S3776` threshold. No signature change. |
| `BuildWorkflowStateOrder` | `Lighthouse.Backend/.../Services/Implementation/BaseMetricsService.cs` | Pace-exclusive mapped-state ordering | REUSE AS-IS | Already yields the mapped `DoingStates` order the cumulative walk needs; pace-exclusive so the change cannot leak into the sibling cumulative-state-time chart. |
| `GetAgeInStatePercentilesForTeam` / `…ForPortfolio` | `TeamMetricsService.cs` / `PortfolioMetricsService.cs` | Load transitions + delegate to the helper | REUSE AS-IS (delegation unchanged) | Each still loads via its own repository and delegates; only the helper's internal population logic changes. |
| `PercentileCalculator.CalculatePercentile` | `Lighthouse.Backend/.../Services/Implementation/PercentileCalculator.cs` | Nearest-rank percentile | REUSE AS-IS | Algorithmic parity with `cycleTimePercentiles` (ADR-019 §4) preserved. |
| `GetWorkItemsClosedInDateRange` membership predicate | `BaseMetricsService` | `ClosedDate ∈ window` | REUSE AS-IS | Membership rule (ADR-019 §2) unchanged. |
| `BaseMetricsService.GetFromCacheIfExists` + key `AgeInStatePercentiles_{start}_{end}` | `BaseMetricsService` | In-process static cache + post-sync invalidation | REUSE AS-IS | No cache-key bump (restart-on-deploy + post-sync `InvalidateMetrics` already drop stale values). |
| `WorkItem.SyncedTransitions` (normalized model) | sibling 1 (ADR-015) | Normalized `(FromState, ToState, TransitionedAt)` per connector | REUSE AS-IS | The single connector-agnostic data source for the cumulative walk. |
| `AgeInStatePercentilesDto`, endpoints, FE model, response shape | API/DTO + controllers + `PerStatePercentileValues.ts` | Contract surface | REUSE AS-IS | Values change, contract does not. No schema / contract change. |
| ArchUnit transition-repo + no-aggregation-service rules | `Architecture/MetricsArchitectureTests.cs` + `TimeInStateSeamArchUnitTest.cs` | Enforce repo-only transition reads + ADR-021 | REUSE AS-IS (+ add no-`WorkTrackingSystem`-switch guard) | Preserve the existing rules; add one guard that the pace walk contains no per-connector branch. |

## Technology Stack

Unchanged. C# .NET 8 / ASP.NET Core / EF Core 8 backend; React 18 + TypeScript 5 (strict) frontend; NUnit 4.6 + Moq + EF InMemory + `WebApplicationFactory` backend tests; Vitest + RTL frontend tests; Playwright (POM) E2E; ArchUnitNET; Stryker.NET / Stryker (TS) mutation (≥80%); Biome. No new technology, library, or third-party service.

## Constraints Established

- **Connector-agnostic** — correct for Jira, ADO, Linear, CSV, and any future source; NO `switch (WorkTrackingSystem)`; operates only on normalized `SyncedTransitions` + mapped `DoingStates`.
- **Pace-path-only blast radius** — `BuildWorkflowStateOrder` is pace-exclusive; the mapped-state filter and cumulative walk touch no other metric; the sibling cumulative-state-time chart is provably unaffected (ADR-024 cross-call rule preserved).
- **No schema / contract / cache-key change** — no EF migration; DTO / endpoints / FE model / response shape unchanged; cache key unchanged (restart-on-deploy + post-sync invalidate handle the value change).
- **Sonar new-violations = 0** — extract a named helper for the cumulative walk / imputation to keep cognitive complexity under `S3776`; pre-apply `docs/ci-learnings.md` rule families.

## Upstream Changes (back-propagation)

- **D12 (DISCUSS locked-decisions table) revised** — per-state-exit population → cumulative reached-at-least-this-state population; connector-agnostic constraint added. Recorded in the feature-delta `## Changed Assumptions` with the original wording quoted verbatim.
- **DDD-1 (DESIGN DDD list + Decisions table) revised** — to the cumulative population algorithm; ADR pointer changed from ADR-019 to ADR-047 (which supersedes ADR-019 §1/§3). DDD-2/3/4/5 unchanged.
- **ADR-047 created** — supersedes the ADR-019 §1/§3 metric; ADR-019's membership / percentile / caching sections retained.
- No downstream-to-upstream feedback to sibling features — sibling 1 (`time-in-state-and-staleness`) and sibling B3 (`state-time-cumulative-view`) DESIGNs are unaffected.
