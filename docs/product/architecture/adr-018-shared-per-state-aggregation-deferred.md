# ADR-018: Shared `IPerStateAggregationService` — Deferred to Sibling Consumers' DESIGNs

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-015/016/017)
**Date**: 2026-05-24
**Feature**: time-in-state-and-staleness (Epic 4144 MVP bundle, slice A+B1+D)
**Decider**: Morgan (Solution Architect)

---

## Context

Both sibling MVP-bundle features (`aging-pace-percentiles`, `state-time-cumulative-view`) flagged at DISCUSS time the possibility of a SHARED `IPerStateAggregationService` to deduplicate per-state aggregate computation across `WorkItemStateTransition` rows:

- `aging-pace-percentiles` DISCUSS → "Wave decisions summary / Downstream coordination": *"`state-time-cumulative-view` will also consume `WorkItemStateTransition` and may want to share aggregation infrastructure with this feature's new endpoint. Flag for DESIGN cross-feature alignment, not a DISCUSS blocker."*
- `state-time-cumulative-view` DISCUSS D10: *"A shared per-state aggregation service / materialised cache is a candidate DESIGN-time optimization but NOT required for MVP correctness. MVP proceeds assuming independent computation per feature; DESIGN may consolidate if profiling demands it."*
- `state-time-cumulative-view` DISCUSS "Cross-MVP DESIGN coordination notes / 1": *"A shared `IPerStateAggregationService` with two methods (`GetPerStatePercentiles(team, window)` and `GetPerStateCumulativeTime(team, window)`) would deduplicate the per-state loop and could share a materialised cache. NOT required for MVP correctness; flagged for DESIGN consideration."*

Crucially, the same sibling's "Cross-MVP DESIGN coordination notes / 3 and 4" emphasise that the two consumers have **materially different semantics**:

- `aging-pace-percentiles` uses a "history window" with COMPLETED items only — the distribution is over the age-at-state-exit of items that finished WITHIN the window.
- `state-time-cumulative-view` uses "full-duration attribution with frame-based item selection" (D5 revised 2026-05-24, D12 inclusion rule) — items that touched the window during their life are included and their FULL state-durations are counted regardless of whether those durations fall inside or outside the window.

The sibling explicitly warns: *"DESIGN must NOT share a helper that conflates the two; the two endpoints should be named and documented to make the distinction unmistakable."*

This DESIGN (`time-in-state-and-staleness`) is the FOUNDATION for both consumers but does not itself need any per-state aggregation. The question this ADR settles is: **Should this DESIGN define and ship `IPerStateAggregationService` as part of the foundation, or leave the decision to the consumer DESIGNs?**

---

## Decision

**Defer `IPerStateAggregationService` to the consumer DESIGNs. This DESIGN does NOT define it.**

The data foundation shipped by this feature consists of:

1. `WorkItemStateTransition` entity with `IRepository<WorkItemStateTransition>` (concretely `IWorkItemStateTransitionRepository` extending the base for the same reason `IWorkItemRepository` exists — to express the `WorkItemId`-scoped query patterns).
2. `WorkItem.CurrentStateEnteredAt` persisted column (ADR-016).

Both consumers consume these primitives directly. If, during their own DESIGN or DELIVER, profiling shows duplicated per-state aggregation walks dominating latency, an extracted `IPerStateAggregationService` can ship as a refactor — at that point the two consumers' EXACT semantic needs are known concretely, and the abstraction can be designed against real usage rather than speculated usage.

The repository interface IS the consumer-facing surface this DESIGN commits to:

```
public interface IWorkItemStateTransitionRepository : IRepository<WorkItemStateTransition>
{
    // Repository base provides GetAll, GetAllByPredicate, Add, Update, Remove, Save.
    // No new methods needed for the foundation; consumer DESIGNs may extend if profiling demands.
}
```

That is the contract sibling DESIGNs build on top of. Sibling DESIGNs may freely extract a shared helper later — including potentially in the same iteration where their two endpoints land if their DESIGN authors decide to coordinate.

---

## Alternatives Considered

**Option A — Define `IPerStateAggregationService` now, in this DESIGN.**

The proposed interface:

```
public interface IPerStateAggregationService
{
    // For aging-pace-percentiles:
    IReadOnlyDictionary<string, IReadOnlyList<PercentileValue>> GetPerStatePercentiles(
        Team team, DateTime windowStart, DateTime windowEnd, IReadOnlyList<int> percentiles);

    // For state-time-cumulative-view:
    IReadOnlyDictionary<string, CumulativeStateTime> GetPerStateCumulativeTime(
        Team team, DateTime windowStart, DateTime windowEnd);
}
```

- Pros: Sibling DESIGNs converge on one helper; if both implementations end up walking the same transitions, the walk happens once. Could share a materialised cache.
- Cons:
  1. **Conflation risk explicitly flagged by sibling DISCUSS.** The two methods take superficially-identical signatures (`Team, DateTime, DateTime`) but reason about COMPLETELY different inclusion rules. A future maintainer reading `GetPerStatePercentiles(team, start, end, [50,70,85,95])` next to `GetPerStateCumulativeTime(team, start, end)` would reasonably assume both methods filter by the SAME window-overlap rule. They do not. The sibling DESIGNs deliberately name their endpoints differently (`ageInStatePercentiles` vs `cumulativeStateTime`) to communicate the distinction; folding both behind one service obscures that distinction precisely where it matters most.
  2. **Premature abstraction.** The shared signature exists; the shared IMPLEMENTATION does not. Each method's body would walk transitions via different filters, group by different aggregation predicates, and assemble different return shapes. The only ACTUAL shared work would be the `GetAllByPredicate` call against the transitions repository — and that is already deduplicated at the EF query plan level by Postgres / Sqlite when the same predicate runs twice in adjacent requests.
  3. **Cache sharing is not free.** A shared materialised cache requires a cache-key strategy that captures BOTH inclusion rules; cross-feature cache invalidation becomes a function of MAX(both rule sets' invalidation events). The existing `bug-5016-cache-thread-safety` was a non-trivial fix; piling another cache flavour onto the same infrastructure NOW (before profiling shows it's needed) increases the bug-5016-class surface area for hypothetical performance.
  4. **Surface DOES NOT YET EXIST.** At the moment this DESIGN ships, the sibling features have DISCUSS-locked DDDs but no DESIGN. Their actual queries — bind variables, exact predicates, exact returned tuples — are not yet written. Designing a generic helper against unwritten queries produces a helper shaped by what we GUESS the queries will need, not what they actually need.
- **Rejected** because conflation risk is explicit, premature abstraction is real, and the cost of NOT shipping the helper now is at most "one extra refactor commit" in a later sibling DESIGN.

**Option B — Define a narrower shared primitive: only the `IWorkItemStateTransitionRepository` query method.**

E.g. `_transitionsRepo.GetTransitionsForTeam(Team team, DateTime? windowStart, DateTime? windowEnd) → IQueryable<WorkItemStateTransition>` returning an unfiltered queryable that the consumer composes with its own LINQ filters.

- Pros: minimal commitment; consumer keeps its semantic-specific filter logic; the shared piece is genuinely shared.
- Cons: this is exactly what `IRepository<T>.GetAllByPredicate(Expression<Func<T, bool>>)` already provides. Adding a thin wrapper around it adds names without reducing duplication.
- **Rejected** as redundant; `GetAllByPredicate` is the shared primitive.

---

## Consequences

**Positive**:

- This DESIGN ships the smallest possible foundation: one table, one column, the existing repository pattern. Sibling DESIGNs have maximum freedom to choose their own DESIGN-time abstractions against the actual queries they need to write.
- The cross-MVP coordination note (sibling F's "Downstream coordination" and state-time-cumulative-view's D10) is RESOLVED rather than left open: the resolution is "ship independent, consolidate later if profiling demands."
- Sibling DESIGNs can be reviewed and signed off without contention over a speculative helper.
- If, after sibling MVP ships, an aggregation helper does emerge, it lands as a clean refactor commit with both call sites visible — exactly the conditions under which abstractions succeed.

**Negative**:

- Sibling F and sibling state-time-cumulative-view each ship their own per-state-aggregation code path. If their queries happen to be IDENTICAL on inspection (unlikely given the semantic divergence), some duplication is paid temporarily. Net cost: the marginal LOC of one extra `GroupBy(t => t.ToState)` LINQ chain duplicated across two files. Trivially refactorable later.
- The cross-MVP coordination conversation does NOT end with a single shared service — sibling DESIGN authors must coordinate via their endpoint naming conventions and via the explicit semantic-difference documentation in this DESIGN's `[REF] Open questions` section (where the OPEN-QUESTION resolution lives for downstream readers).

**Neutral**:

- The two sibling endpoints (`/metrics/ageInStatePercentiles` and `/metrics/cumulativeStateTime`) carry materially different routes precisely BECAUSE they answer materially different questions. That is correct API design and matches the sibling DISCUSS's locked endpoint names.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| No class named `*PerStateAggregation*` exists in this DESIGN's commit set | Code-review gate; this ADR is the canonical reason if a reviewer asks why |
| If a future feature reopens this question, it does so by adding an ADR that supersedes ADR-018 (not by silent introduction of the helper) | Project convention; ADR-018 status remains `Accepted` until explicitly superseded |

---

## Cross-feature impact

- `aging-pace-percentiles` DESIGN: free to implement its per-state percentile loop directly against `IWorkItemStateTransitionRepository.GetAllByPredicate(...)`. May choose to add private helper methods on its own service.
- `state-time-cumulative-view` DESIGN: same. May choose to add private helper methods on its own service.
- Sibling DESIGNs should each include an `[REF] Open questions` note that points back to ADR-018 and confirms the "ship independent" path; if EITHER sibling decides to extract a shared helper during its DESIGN, a new ADR-NNN may supersede this one.
