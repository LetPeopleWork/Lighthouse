# ADR-021: Uphold ADR-018 — Compute Per-State Percentiles Independently inside `TeamMetricsService` / `PortfolioMetricsService` (no shared aggregation service)

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-019/020)
**Date**: 2026-05-24
**Feature**: aging-pace-percentiles (Epic 4144 MVP bundle, slice F)
**Decider**: Morgan (Solution Architect)

---

## Amendment (2026-05-25) — feature simplification

The **decision below stands unchanged**: per-state percentiles are computed independently inside `TeamMetricsService` / `PortfolioMetricsService`; no shared `IPerStateAggregationService`. Only one incidental detail is stale: the Context's "duration = `exitTransition − entryTransition` per visit" was amended (ADR-019, D12) to **cumulative total age at exit** = `exitTransition − StartedDate`. This does not change the divergence argument vs sibling B3 (the membership rules still differ; the duration formulas still differ), so Path A still holds.

---

## Context

Sibling ADR-018 (`time-in-state-and-staleness` DESIGN, 2026-05-24) deferred the question of whether to introduce a shared `IPerStateAggregationService` for the two MVP consumers of `WorkItemStateTransition` (`aging-pace-percentiles` and `state-time-cumulative-view`). The sibling architect's verbatim rationale: *"Helper would conflate inclusion-rule semantics that sibling DISCUSSes deliberately distinguished. Defer the abstraction further; ship the primitive (repository); siblings consume independently."* The decision was deliberately punted to each sibling's DESIGN to be re-litigated when the queries are concrete.

This DESIGN is the first sibling to re-encounter the question. The query shapes are now concrete (ADR-019). The decision arrives:

**Path A (uphold ADR-018)**: Compute per-state percentiles independently inside `TeamMetricsService.GetAgeInStatePercentilesForTeam` / `PortfolioMetricsService.GetAgeInStatePercentilesForPortfolio`, each method walking the `IWorkItemStateTransitionRepository` directly with its own filters. Sibling B3 (`state-time-cumulative-view`) writes its own similar walk in its own service when it DESIGNs.

**Path B (supersede ADR-018)**: Introduce `IPerStateAggregationService` NOW with one method `GetPerStatePercentiles(scope, window, percentiles)`. Sibling B3 adds its method `GetPerStateCumulativeTime(scope, window)` when it DESIGNs.

ADR-019 just concretised the two key reasons the rules diverge:

1. **Item-membership rule diverges**: this feature filters by `WorkItem.ClosedDate ∈ [startDate, endDate]` (mirrors `cycleTimePercentiles`); sibling B3 filters by frame-based intersection of any `(stateEnter, stateExit)` interval with the window (D12 of sibling B3 DISCUSS). The two predicates select **different sets of items** for the same window.
2. **Duration-counting rule diverges**: this feature counts the full `(exitTransition - entryTransition)` per completed visit (unclipped, visit-level); sibling B3 counts the full state durations for every item in the included set (unclipped, item-level, INCLUDING in-flight `now - currentStateEnteredAt` contributions per D11 of sibling B3 DISCUSS). The two duration formulas attribute time **differently** to the same state.

The two endpoints are answering materially different questions; the surface similarity ("aggregate per-state from `WorkItemStateTransition`") hides this divergence.

---

## Decision

**Uphold ADR-018. Path A — independent computation per feature. Do NOT introduce `IPerStateAggregationService` in this DESIGN.**

Concrete service-layer shape introduced by this feature:

```
public interface ITeamMetricsService
{
    // … existing methods …
    IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForTeam(
        Team team, DateTime startDate, DateTime endDate);
}

public interface IPortfolioMetricsService
{
    // … existing methods …
    IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForPortfolio(
        Portfolio portfolio, DateTime startDate, DateTime endDate);
}
```

Both methods are implemented on the corresponding existing service classes (`TeamMetricsService`, `PortfolioMetricsService`). Both delegate to a shared **private** helper colocated in `BaseMetricsService` (where appropriate — the helper is intra-service, NOT a new abstraction):

```
// Inside BaseMetricsService — a protected helper, not a new interface:
protected IEnumerable<AgeInStatePercentilesDto> ComputeAgeInStatePercentiles(
    IEnumerable<WorkItem> completedItemsInWindow,
    IEnumerable<string> doingStatesInWorkflowOrder,
    IReadOnlyList<int> requestedPercentiles)
{
    // Walk transitions per ADR-019; bucket by state; CalculatePercentile via PercentileCalculator.
}
```

The `BaseMetricsService` placement matches the existing inheritance pattern (both `TeamMetricsService` and `PortfolioMetricsService` extend `BaseMetricsService` today and share other helpers — e.g. throughput aggregation, total work-item-age — via that base class). The helper is **scope-agnostic**: it takes the *result* of the scope's "what items are completed in window" query and the *workflow state order*. The scope-specific completed-items query stays in each derived class (the team uses `GetWorkItemsClosedInDateRange(team, …)`; the portfolio uses its existing equivalent).

**Crucially, this helper is INSIDE the existing service classes' inheritance.** It is not exposed via an interface; it is not a port; sibling B3 cannot consume it. The DDD-4 (`time-in-state-and-staleness`) commitment — "no shared service across consumers" — is upheld.

Transition reads use `IWorkItemStateTransitionRepository.GetAllByPredicate(...)` per ADR-018. The repository is the architectural seam shared between the two sibling features; each consumer composes its own filter.

---

## Alternatives Considered

**Path B — Introduce `IPerStateAggregationService` now, with one method `GetPerStatePercentiles(scope, window, percentiles)`.**

- Pros:
  1. When sibling B3 DESIGNs, it can add its method to the same interface; eventual cache-sharing infrastructure has one home.
  2. Consolidates the transition-walking code path; one place to optimise if profiling demands.
  3. Sibling B3 has fewer "where does this code live?" decisions to make.
- Cons:
  1. **Re-introduces the conflation risk that ADR-018 spent an entire ADR documenting away.** A future maintainer reading `GetPerStatePercentiles(team, start, end, [50,70,85,95])` next to `GetPerStateCumulativeTime(team, start, end)` will reasonably assume both methods apply the same window-membership rule and the same duration formula. They do not. The endpoint names hide a substantive semantic gap. Routing through one service makes the names look related; they are not.
  2. The "shared transition walking" win is small in practice. Each method's body filters items, walks transitions, computes its own aggregation. The only honestly-shared work is the `GetAllByPredicate(...)` call against the repository — and that call is already deduplicated by the EF query plan when adjacent requests run against the same predicate. Profile first; abstract later.
  3. ADR-019 has now made the divergence concrete in writing. Introducing the helper after writing down "these two methods reason about completely different inclusion rules and duration formulas" is intellectually inconsistent.
  4. Sibling B3 has not yet DESIGNed. Its actual query shape is not yet written. Designing a generic helper against an unwritten sibling's query produces a helper shaped by guesswork.
- **Rejected** because (1) the conflation risk is the explicit reason ADR-018 exists, (2) ADR-019's documented divergence reinforces rather than weakens the conflation risk, and (3) the cost of "ship independent, refactor later" is at most one refactor commit in a later sibling DESIGN — at which point the abstraction is shaped by two concrete callers, not one concrete and one speculated.

**Path B' — Introduce `IPerStateAggregationService` later, in sibling B3's DESIGN, with both methods (this feature's and B3's) added in the same commit.**

- Pros: timing inverts so the helper is shaped by two concrete callers, not one.
- Cons: still creates the conflation surface. The shared interface name suggests common semantics that do not exist. Naming the methods differently inside the same interface (`GetPerStatePercentiles_ClosedInWindow(...)` vs `GetPerStateCumulativeTime_FrameIntersection(...)`) is more honest, but at that point the shared interface is purely a code-organisation convenience with zero semantic value.
- **Rejected (for now)** with an explicit note for sibling B3's DESIGN author: if you arrive at the same conclusion (independent computation is correct), no ADR is needed; if you arrive at a different conclusion (a shared helper IS warranted), this ADR can be superseded by a new ADR-NNN that documents the new framing.

**Path C — Inline the per-state percentile computation directly into `TeamMetricsController.GetAgeInStatePercentilesForTeam` (skip the service layer).**

- Pros: minimal indirection.
- Cons: violates the established ports-and-adapters pattern (controllers depend on `I…MetricsService`, never on repositories). Breaks the caching pattern (`GetFromCacheIfExists` lives on `BaseMetricsService`). Breaks ArchUnitNET rules.
- **Rejected** for violating established architecture.

---

## Consequences

**Positive**:

- ADR-018 stays Accepted; the cross-MVP coordination ends in convergence (both architects independently reached the same conclusion under different starting framings).
- The two consumer services compute their own per-state aggregates independently. Each can be optimised, cached, profiled, or restructured without affecting the other.
- The semantic divergence between the two features remains visible at the type level: each service has its own method with its own return shape and its own implementation comment in the ADRs.
- Sibling B3's DESIGN author is free to choose Path A again (same conclusion) or Path B (superseding both ADR-018 AND this ADR-021 with a new ADR that argues for consolidation in light of concrete B3 query shapes).

**Negative**:

- Some implementation-level duplication: the helper inside `BaseMetricsService` introduced by this feature (a private/protected method) is shaped for the `aging-pace-percentiles` consumer; sibling B3 will write a similar-but-different helper for its own consumer. Marginal duplication: the protected helpers each amount to a transition-walk + a bucket + a domain-specific aggregation (percentile vs sum). Quantification: the two helpers will share the `IWorkItemStateTransitionRepository.GetAllByPredicate(...)` call signature and the `Doing`-state filter; they will diverge on the duration formula and the aggregation function. Net duplicated LOC across the two features: ~10-15 lines per service. Trivially refactorable if profiling later demands consolidation.
- A future reader looking for "where is per-state aggregation done?" must look in TWO places (the two service classes' protected helpers). Mitigated by both ADRs (018 and 021) explicitly naming the location and by the protected helper's method name (`ComputeAgeInStatePercentiles` is precisely searchable, parallel-named to sibling B3's eventual `ComputeCumulativeStateTime`).

**Neutral**:

- The `IWorkItemStateTransitionRepository` (shipped by ADR-015) IS the shared primitive across both consumers — and that is sufficient. The repository is what was always going to be shared; the question was only whether to ALSO share a higher-level service. The answer is "no, not yet."
- If profiling demands consolidation later, the refactor is straightforward: extract the two protected helpers into a new `IPerStateAggregationService` with two clearly-named methods (`GetCompletedInWindowPerStatePercentiles` and `GetFrameIntersectionPerStateCumulative` — distinct names that preserve the semantic gap). The refactor commit names the conflation risk in its rationale and discharges it via the explicit method names.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| No class or interface named `*PerStateAggregation*` is introduced by this feature's commit set | Code-review gate; canonical reference ADR-018 + ADR-021 |
| `TeamMetricsService.GetAgeInStatePercentilesForTeam` reads transitions only via `IWorkItemStateTransitionRepository` (not via raw `LighthouseAppContext.WorkItemStateTransitions`) | ArchUnitNET test (extending the ADR-015 rule): the metrics-service classes may not reference `DbSet<WorkItemStateTransition>` directly |
| The `BaseMetricsService` helper for per-state aggregation is `protected` (intra-inheritance), NOT `public` and NOT exposed via an interface | NUnit reflection test asserting the helper method's access modifier; ArchUnitNET test asserting no interface in `Services.Interfaces` exposes the helper signature |
| Sibling B3 (`state-time-cumulative-view`) introduces its OWN service-layer method (e.g. `GetCumulativeStateTimeForTeam`); it does NOT call `BaseMetricsService.ComputeAgeInStatePercentiles` | DESIGN-time review of sibling B3's eventual feature-delta; this ADR is the canonical reference if B3's DESIGN author considers calling across feature boundaries |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1): unchanged. ADR-018 stays Accepted; this ADR refers to it and upholds its decision.
- `state-time-cumulative-view` (sibling B3): sibling B3's DESIGN author has explicit freedom under ADR-018 + ADR-021 to either (a) write an independent computation following the same pattern this feature establishes, or (b) supersede both ADRs with a new ADR proposing consolidation in light of B3's concrete query shape. This DESIGN does NOT pre-decide B3's choice.
- The pattern established here ("scope-specific completed-items query lives in the derived service; per-state walk lives in a `protected` `BaseMetricsService` helper") is reusable by sibling B3 if it chooses Path A. If it chooses Path B, the pattern is superseded.

---

## Reviewer note (cross-MVP)

Atlas / future reviewer: if you read this ADR alongside ADR-018, the two say the same thing twice. That is deliberate — ADR-018 deferred the decision; this ADR makes the deferral explicit AGAIN in the context of the first concrete consumer. The duplication is intentional; the two ADRs together form a single coherent record across two feature boundaries.

The third sibling (B3 — `state-time-cumulative-view`) has not yet DESIGNed. The state-time-cumulative-view DISCUSS document explicitly references ADR-018 in its Cross-MVP coordination section; that sibling's DESIGN will be the third (and final) opportunity to re-litigate.
