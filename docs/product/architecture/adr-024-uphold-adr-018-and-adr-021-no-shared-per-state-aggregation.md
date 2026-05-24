# ADR-024: Uphold ADR-018 + ADR-021 — Compute Cumulative State-Time Independently inside `TeamMetricsService` / `PortfolioMetricsService` via a Sibling `protected` Helper in `BaseMetricsService` (no shared `IPerStateAggregationService`)

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-022/023/025; third and final sibling re-litigation of ADR-018)
**Date**: 2026-05-24
**Feature**: state-time-cumulative-view (Epic 4144 MVP bundle, slice B3)
**Decider**: Morgan (Solution Architect)

---

## Context

ADR-018 (sibling 1, `time-in-state-and-staleness` DESIGN) deferred whether to introduce a shared `IPerStateAggregationService` across the two MVP-bundle consumers of `WorkItemStateTransition`. ADR-021 (sibling F, `aging-pace-percentiles` DESIGN) re-litigated and upheld the deferral as **Path A** — independent computation via a `protected` helper inside `BaseMetricsService` named `ComputeAgeInStatePercentiles`. ADR-021's Reviewer-note (cross-MVP) explicitly flagged the third opportunity to re-litigate:

> "The third sibling (B3 — `state-time-cumulative-view`) has not yet DESIGNed. The state-time-cumulative-view DISCUSS document explicitly references ADR-018 in its Cross-MVP coordination section; that sibling's DESIGN will be the third (and final) opportunity to re-litigate."

ADR-021's enforcement table further pre-empts the consolidation question by naming what sibling B3's helper should be called:

> "Sibling B3 (state-time-cumulative-view) introduces its OWN service-layer method (e.g. `GetCumulativeStateTimeForTeam`); it does NOT call `BaseMetricsService.ComputeAgeInStatePercentiles`."

This DESIGN is the third (and last) sibling to encounter the question. The query shapes are now fully concrete across all three siblings — ADR-022 makes this feature's algorithm precise:

| Dimension | Sibling F (ADR-019) | This feature (ADR-022) |
|---|---|---|
| Item-membership rule | `W.ClosedDate ∈ [windowStart, windowEnd]` — completed-items-only | UNION: (a) any `(stateEnter, stateExit)` interval intersects window OR (b) currently in-flight AND `currentStateEnteredAt ≤ windowEnd` — INCLUDES in-flight |
| Per-visit attribution | Visit-level: each completed visit through state `S` contributes one observation | Visit-level: each completed visit through state `S` contributes `visitDuration` to `completedContribution[S]` |
| In-flight attribution | EXCLUDED — only completed visits | INCLUDED — current-state `now - currentStateEnteredAt` contributes to `ongoingContribution[S]` |
| Duration clipping | Unclipped (full visit duration) | Unclipped (full visit duration AND full in-flight duration) |
| Aggregation | Percentile distribution per state (50/70/85/95) via `PercentileCalculator` | Sum per state per segment; counts; mean; median |
| Cache key | `AgeInStatePercentiles_{startDate}_{endDate}` | `CumulativeStateTime_{startDate}_{endDate}` AND `CumulativeStateTime_Items_{state}_{startDate}_{endDate}` |

The two endpoints SHARE only: (i) the `IWorkItemStateTransitionRepository` data seam (ADR-015), (ii) the day-counting convention (`WorkItemBase.GetDateDifference`), (iii) the percentile primitive `PercentileCalculator.CalculatePercentile` (this feature uses it for the median per state — ADR-022 §7), (iv) the cache infrastructure (`BaseMetricsService.GetFromCacheIfExists`), (v) the `WorkItem.CurrentStateEnteredAt` field (sibling 1, ADR-016 — read-only for both).

They DO NOT SHARE: item-membership rule, in-flight attribution rule, aggregation function, response shape, cache key namespace, or endpoint route.

The question this ADR settles: **given the concrete divergence, does this DESIGN supersede ADR-018+021 with `IPerStateAggregationService` (Path B), or follow the same `protected` helper pattern in `BaseMetricsService` (Path A-prime, parallel to sibling F)?**

---

## Decision

**Uphold ADR-018 and ADR-021. Path A-prime — independent computation inside `BaseMetricsService` via a sibling `protected` helper named `ComputeCumulativeStateTime`. Do NOT introduce `IPerStateAggregationService`.**

Concrete service-layer shape introduced by this feature:

```
public interface ITeamMetricsService
{
    // ... existing methods ...
    CumulativeStateTimeDto GetCumulativeStateTimeForTeam(
        Team team, DateTime startDate, DateTime endDate);

    CumulativeStateTimeItemsDto GetCumulativeStateTimeItemsForTeam(
        Team team, string state, DateTime startDate, DateTime endDate);
}

public interface IPortfolioMetricsService
{
    // ... existing methods ...
    CumulativeStateTimeDto GetCumulativeStateTimeForPortfolio(
        Portfolio portfolio, DateTime startDate, DateTime endDate);

    CumulativeStateTimeItemsDto GetCumulativeStateTimeItemsForPortfolio(
        Portfolio portfolio, string state, DateTime startDate, DateTime endDate);
}
```

Both pairs delegate to a sibling `protected` helper colocated in `BaseMetricsService`, alongside sibling F's `ComputeAgeInStatePercentiles`:

```
// Inside BaseMetricsService:
protected CumulativeStateTimeDto ComputeCumulativeStateTime(
    IEnumerable<WorkItem> includedItems,
    Func<int, IEnumerable<WorkItemStateTransition>> getTransitionsForItem,
    IEnumerable<string> workflowStatesInOrder,
    DateTime nowSnapshot);

protected CumulativeStateTimeItemsDto ComputeCumulativeStateTimeItems(
    IEnumerable<WorkItem> includedItems,
    Func<int, IEnumerable<WorkItemStateTransition>> getTransitionsForItem,
    string selectedState,
    DateTime nowSnapshot);
```

The helpers are **scope-agnostic and inclusion-rule-agnostic**: they take the *result* of the scope's D12-inclusion query plus a transition-fetcher delegate plus the workflow ordering plus the `now` snapshot. The scope-specific D12-inclusion query stays in each derived class (`TeamMetricsService` / `PortfolioMetricsService`) and exercises `IWorkItemStateTransitionRepository` + `IWorkItemRepository` per ADR-022 §1. The derived class also resolves the workflow order from the team's / portfolio's existing `WorkflowStates` configuration (same source the chart's bar ordering uses).

This shape parallels ADR-021's `ComputeAgeInStatePercentiles`: same access modifier (`protected`), same colocation (`BaseMetricsService`), same intra-inheritance access (NOT exposed via an interface). The two helpers live alongside each other; they share zero CALL SITES (sibling F's controllers call only `ComputeAgeInStatePercentiles`; this feature's controllers call only `ComputeCumulativeStateTime` + `ComputeCumulativeStateTimeItems`). The class-level co-location is purely organisational; there is no shared LOC across the two helpers.

Transition reads use `IWorkItemStateTransitionRepository.GetAllByPredicate(...)` exclusively per ADR-015 (extended by ADR-021's enforcement rule). The repository is the architectural seam shared between all three sibling features.

**The ADR-018 + ADR-021 + ADR-024 chain converges on a single across-MVP rule**: per-state aggregation does NOT cross feature boundaries via a shared service interface. Three independent DESIGN re-litigations arrived at the same conclusion under three different starting framings (foundation, percentile consumer, sum consumer).

---

## Alternatives Considered

**Path B — Introduce `IPerStateAggregationService` now, superseding ADR-018 and ADR-021.**

The proposed interface (drafted in ADR-018 Alternatives, re-evaluated in ADR-021 Alternatives, and re-evaluated again here with all three concrete query shapes in hand):

```
public interface IPerStateAggregationService
{
    IEnumerable<AgeInStatePercentilesDto> GetPerStatePercentiles(
        IMetricsScope scope, DateTime windowStart, DateTime windowEnd,
        IReadOnlyList<int> percentiles);

    CumulativeStateTimeDto GetPerStateCumulativeTime(
        IMetricsScope scope, DateTime windowStart, DateTime windowEnd);

    CumulativeStateTimeItemsDto GetPerStateCumulativeTimeItems(
        IMetricsScope scope, string state, DateTime windowStart, DateTime windowEnd);
}
```

- Pros:
  1. Three methods on one interface; a future maintainer searching for "per-state aggregation" finds them together.
  2. Eventual cache-sharing infrastructure has one home.
  3. Symmetric across siblings; sibling F's existing `ComputeAgeInStatePercentiles` becomes the first method of the new service.
- Cons:
  1. **Three-way conflation risk now explicit.** With three concrete query shapes documented in three ADRs, the divergence is no longer speculative. The three methods carry DIFFERENT inclusion rules:
     - `GetPerStatePercentiles`: `ClosedDate ∈ window`
     - `GetPerStateCumulativeTime`: transition-intersection OR in-flight-at-windowEnd
     - `GetPerStateCumulativeTimeItems`: same as `GetPerStateCumulativeTime`, plus a state filter
     A future maintainer reading the three signatures side-by-side will reasonably assume they apply the same window-membership rule. They do not. Symmetric signatures hide asymmetric semantics; that is precisely the conflation risk ADR-018 spent an entire ADR documenting away.
  2. **Symmetric interface naming creates pressure to symmetric behaviour.** Once `IPerStateAggregationService` exists, future per-state features will be tempted to land their methods on it ("if it's per-state, it goes there"). Each new method either (a) re-litigates its inclusion rule against the existing two, or (b) silently adopts whichever rule looks most similar to the new feature's framing. (b) is the failure mode.
  3. **Premature abstraction surface.** The three methods' bodies share at most the `IWorkItemStateTransitionRepository.GetAllByPredicate(...)` call and the `GetDateDifference` day-counting helper — both already shared via their existing surfaces (sibling 1's repository, `WorkItemBase`'s convention). The shared service adds an interface layer without reducing duplicated LOC.
  4. **No cache-sharing win at MVP scale.** The two endpoints' cache keys are distinct (`AgeInStatePercentiles_…` vs `CumulativeStateTime_…`); each scope/window/state combination caches independently. Cross-method cache sharing would require a unified cache key strategy that respects all three inclusion rules — significantly more complex than the current per-key approach, and bug-5016's recent thread-safety fix was non-trivial work this DESIGN should not multiply.
  5. **Supersession breaks two stable ADRs across three feature boundaries.** ADR-018 (Accepted), ADR-021 (Accepted) — both feature deltas reference these ADRs in their handoff documents and SSOT brief.md sections. Superseding now means rewriting cross-references in `aging-pace-percentiles/feature-delta.md`, in the SSOT brief.md aging-pace-percentiles section, and in three C4 diagrams.
- **Rejected** for: three-way conflation risk made concrete by ADR-022's documented divergence (cons 1 + 2); premature abstraction surface (cons 3); zero cache-sharing win at MVP scale (cons 4); cost of superseding stable cross-MVP decisions (cons 5).

**Path B' — Introduce `IPerStateAggregationService` later in a post-MVP refactor, after profiling demonstrates a measurable win.**

- Pros: timing inverts so the abstraction is shaped by three concrete callers, not zero.
- Cons:
  1. Still creates the symmetric-naming conflation risk above; the cons 1+2 from Path B don't go away because the rename is later.
  2. The refactor commit's rationale would have to explain why three independent DESIGN decisions all chose Path A-prime, then a single later refactor chose Path B. The asymmetry is uncomfortable; the abstraction would have to be designed to PRESERVE the documented semantic divergence (e.g. `IPerStateAggregationService` with three methods named to embed the inclusion rule in the method name: `GetClosedInWindowPerStatePercentiles`, `GetFrameIntersectionPerStateCumulativeTime`, `GetFrameIntersectionPerStateCumulativeTimeItems`). At that point the shared interface is purely organisational — zero behavioural value — and the colocation could be achieved equally well by colocating the three protected helpers inside `BaseMetricsService` (which is what Path A-prime ALREADY DOES).
- **Rejected** at this DESIGN; the refactor is left available as a documented FUTURE option in this ADR's Consequences. Sibling B3's DESIGN author (me) is the third independent decision-maker to choose Path A-prime, completing the three-way convergence.

**Path C — Inline the cumulative-state-time computation directly into the controllers (skip the service layer).**

- Pros: minimal indirection; matches the "endpoint → repository" pattern of simple CRUD endpoints.
- Cons: violates the established ports-and-adapters pattern (controllers depend on `I…MetricsService` interfaces, never on repositories or DbSets directly). Breaks the caching pattern (`GetFromCacheIfExists` lives on `BaseMetricsService`). Breaks ArchUnitNET rules.
- **Rejected** for violating established architecture.

**Path D — A NEW shared helper outside `BaseMetricsService`** (e.g. a static class `PerStateAggregationHelpers` with three methods).

- Pros: extraction without an interface; helpers callable from both services.
- Cons: a static helper with three methods that have different inclusion-rule contracts is the same conflation risk as Path B, just without the interface header. The naming pressure to symmetry persists (the static class's name suggests "this is THE place for per-state aggregation"); the three methods' semantic divergence is no more visible in a static class than in an interface.
- **Rejected** as equivalent-in-risk to Path B without the discoverability benefit.

---

## Consequences

**Positive**:

- ADR-018 stays Accepted. ADR-021 stays Accepted. This ADR upholds both. The cross-MVP coordination ends in three-way convergence: three architects (the same one, three times across the three sibling DESIGNs) independently reached the same conclusion under three different starting framings (foundation, percentile consumer, sum-and-counts consumer).
- The three consumer services compute their own per-state aggregates independently. Each can be optimised, cached, profiled, or restructured without affecting the others.
- The semantic divergence across the three features remains visible at the type level: each service has its own method (`GetCycleTimePercentilesForTeam` existing + `GetAgeInStatePercentilesForTeam` from sibling F + `GetCumulativeStateTimeForTeam` from this feature) with its own return shape and its own implementation comment in three separate ADRs.
- The three feature-deltas' Cross-MVP coordination sections all converge on the same conclusion, documented consistently across SSOT.
- If profiling later demands consolidation, the refactor commits to ALL THREE features will be honest about the supersession; the migration path is straightforward (the three protected helpers move to a new service; the three controllers' calls update to the new interface).

**Negative**:

- The third instance of "compute per-state aggregate" code colocated in `BaseMetricsService`. Quantification: ADR-022's algorithm uses ~30-40 LOC for the protected helper (item-inclusion query, per-visit walk, segment-split, counts, mean/median). Sibling F's helper is similar size. The third instance is parallel to the second — colocation is `BaseMetricsService`, access is `protected`, naming is parallel (`ComputeCumulativeStateTime` vs `ComputeAgeInStatePercentiles`). Net duplicated LOC across the three features: ~10-15 lines per service of structural similarity (the per-state walk pattern), each diverging on the duration formula and the aggregation function. Trivially refactorable later if profiling demands consolidation.
- A future reader looking for "where is per-state aggregation done?" must look at TWO protected helpers in `BaseMetricsService` (`ComputeAgeInStatePercentiles` + `ComputeCumulativeStateTime` + `ComputeCumulativeStateTimeItems` — actually three). Mitigated by:
  1. All three are named with `ComputeXxx` parallel naming (precisely searchable).
  2. All three live in `BaseMetricsService.cs` (precisely greppable).
  3. ADR-018 + ADR-021 + ADR-024 chain explicitly names this location across the SSOT.

**Neutral**:

- `IWorkItemStateTransitionRepository` (sibling 1, ADR-015) IS the shared primitive across all three sibling features — and that is sufficient. The repository is what was always going to be shared; the question was only whether to ALSO share a higher-level service. Three independent DESIGN re-litigations answer: "no, not yet."
- If profiling demands consolidation post-MVP, the refactor extracts the three protected helpers into a new `IPerStateAggregationService` with three semantically-named methods (`GetCompletedInWindowPerStatePercentiles`, `GetFrameIntersectionPerStateCumulativeTime`, `GetFrameIntersectionPerStateCumulativeTimeItems`) that preserve the semantic gap by NAMING IT. The refactor commit names the conflation risk in its rationale and discharges it via the explicit method names.
- The ArchUnitNET enforcement rule (no class/interface named `*PerStateAggregation*`) introduced by ADR-021 is EXTENDED by this ADR — it now covers all three sibling features. The rule remains a single ArchUnitNET predicate; this ADR adds the third feature's commit-set as a covered scope.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| No class or interface named `*PerStateAggregation*` is introduced by this feature's commit set (extends ADR-021's rule across the three MVP-bundle features) | Existing ArchUnitNET test; canonical reference ADR-018 + ADR-021 + ADR-024 |
| `TeamMetricsService.GetCumulativeStateTimeForTeam` and `GetCumulativeStateTimeItemsForTeam` read transitions only via `IWorkItemStateTransitionRepository` (not via raw `LighthouseAppContext.WorkItemStateTransitions`) | ArchUnitNET test (extending the ADR-015 rule); the metrics-service classes may not reference `DbSet<WorkItemStateTransition>` directly |
| The `BaseMetricsService` helpers (`ComputeCumulativeStateTime`, `ComputeCumulativeStateTimeItems`) are `protected` (intra-inheritance), NOT `public` and NOT exposed via any interface | NUnit reflection test asserting the helper methods' access modifiers; ArchUnitNET test asserting no interface in `Services.Interfaces` exposes the helper signatures |
| This feature's services (TeamMetricsService, PortfolioMetricsService) do NOT call `ComputeAgeInStatePercentiles` (sibling F's helper); sibling F's services do not call this feature's helpers | NUnit reflection test asserting no cross-feature helper invocation; canonical reference ADR-021 + ADR-024 |
| If a future feature reopens this question, it does so by adding an ADR that supersedes ALL THREE (ADR-018, ADR-021, ADR-024) — not by silent introduction of the helper | Project convention; the chain of three ADRs makes the supersession cost explicit |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1): UNCHANGED. ADR-018 stays Accepted; this ADR refers to it and upholds its decision for the third time.
- `aging-pace-percentiles` (sibling F): UNCHANGED. ADR-021 stays Accepted; this ADR refers to it and upholds its decision. Sibling F's `ComputeAgeInStatePercentiles` protected helper lives alongside this feature's `ComputeCumulativeStateTime` / `ComputeCumulativeStateTimeItems` helpers inside the same `BaseMetricsService` class. The two sets of helpers are parallel-named, parallel-located, semantically distinct, and never call each other.
- Future per-state aggregation feature (post-MVP, hypothetical): the established pattern is "scope-specific item-resolution query lives in the derived service; per-state computation lives as a `protected` helper in `BaseMetricsService`; ADR documents the inclusion-rule and duration-formula divergence vs siblings; ArchUnitNET rule extends to forbid silent consolidation." Future features fold into this pattern by adding a fourth helper, OR they explicitly supersede ADR-018+021+024 with a new ADR justifying consolidation.
- The two new endpoints from this feature (`/metrics/cumulativeStateTime` + `/metrics/cumulativeStateTime/items`) are introduced ONLY in this feature's controllers. No cross-feature controller modification.

---

## Reviewer note (cross-MVP, final)

Atlas / future reviewer: if you read this ADR alongside ADR-018 and ADR-021, the three say the same thing three times. That is deliberate — three independent DESIGN waves across three sibling features in the same MVP bundle, all encountering the same "should we share?" question, all answering "no, not yet" with three different concrete reasons. The triplication is intentional; the three ADRs together form a single coherent record across three feature boundaries. ADR-024 closes the loop: the third (and final) MVP-bundle DESIGN has now made its choice.

If future profiling, future similar features, or future maintainer cognitive load demonstrates a real cost from the triplication, the supersession path is documented in this ADR's Path B/B' Alternatives — extracted as a new `IPerStateAggregationService` with semantically-named methods that preserve the divergence by embedding it in the names. Until that evidence exists, the three protected helpers in `BaseMetricsService` are the documented home.

