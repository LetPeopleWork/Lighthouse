# ADR-015: WorkItemStateTransition — Standalone Entity with FK → WorkItem (not owned-collection)

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-016/017/018 as the four DESIGN decisions for `time-in-state-and-staleness`)
**Date**: 2026-05-24
**Feature**: time-in-state-and-staleness (Epic 4144 MVP bundle, slice A+B1+D)
**Decider**: Morgan (Solution Architect)

---

## Context

DISCUSS locked four `WorkItemStateTransition` fields — `workItemId`, `fromState`, `toState`, `transitionedAt` — and explicitly forbade additional fields. Two sibling MVP-bundle features (`aging-pace-percentiles`, `state-time-cumulative-view`) ALSO locked DISCUSS-side "no schema additions required" with the same 4 fields. DESIGN must decide HOW that table lives inside the EF model.

The Lighthouse `WorkItem` is already an aggregate root in the codebase sense: it has a `TeamId` FK, an `AdditionalFieldValues` JSON-serialised dictionary, and a `WorkItems` collection on `Team`. Cycle Time and Work Item Age are computed properties on `WorkItemBase` (not persisted, not transition-derived).

Three plausible placements:

- **A. Owned-collection inside the `WorkItem` aggregate** — `WorkItem.StateTransitions: List<WorkItemStateTransition>` mapped as an EF owned type with shadow FK. Every WorkItem load brings transitions; transitions are guaranteed consistent with their owner.
- **B. JSON column on `WorkItem`** — `WorkItem.StateTransitionsJson: string` serialised list, similar to `AdditionalFieldValues`. Single round-trip, zero new tables, zero new migrations beyond a column add.
- **C. Standalone entity with FK → WorkItem** — `WorkItemStateTransition { Id, WorkItemId (FK, cascade delete), FromState, ToState, TransitionedAt }` as its own `DbSet<WorkItemStateTransition>`. Indexed on `(WorkItemId, TransitionedAt)`. Queryable independently of `WorkItem`.

The two sibling MVP features both consume the transition data via **GROUP BY state with aggregations** on **filtered subsets of items**:

- `aging-pace-percentiles` D9: `ageAtStateExit[state] = transition.timestamp - currentStateEnteredAt(state)` for COMPLETED items in a window → percentile distribution per state.
- `state-time-cumulative-view` D5/D12: full-duration `(stateExit - stateEnter)` summed per state across all included items, plus in-flight `now - currentStateEnteredAt` → bar chart.

Neither consumer wants to load every WorkItem-with-transitions into memory to compute per-state aggregates over hundreds of items × dozens of transitions each. Both want SQL-friendly aggregate queries.

The metrics cache (made thread-safe by `bug-5016-cache-thread-safety`, a stated pre-req) holds aggregated metrics keyed on (scope, window, …) for downstream re-use. Transition rows themselves are NOT cached — only the aggregates derived from them.

CLAUDE.md hard constraints: OOP, ports-and-adapters, immutable updates preferred, EF migrations generated via `Create-Migration.ps1` (lockstep Sqlite + Postgres), no warnings.

---

## Decision

**Option C — Standalone entity with FK → WorkItem.**

```
public class WorkItemStateTransition : IEntity
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public DateTime TransitionedAt { get; set; }
}
```

EF model:

- New `DbSet<WorkItemStateTransition> WorkItemStateTransitions` on `LighthouseAppContext`.
- FK relationship configured with `OnDelete(DeleteBehavior.Cascade)` keyed on `WorkItemId` — deleting a WorkItem deletes its transitions, matching the existing `Team.WorkItems` lifecycle.
- Composite index on `(WorkItemId, TransitionedAt)` to support both per-item ordered reads (badge: `MAX(TransitionedAt) WHERE ToState = WorkItem.State`) and consumer aggregate scans grouped by `ToState`.
- `UtcDateTimeConverter` applied automatically by the existing `ConfigureConventions` hook.

`WorkItem` does NOT hold a `List<WorkItemStateTransition>` navigation. Consumers that need transitions for an item query the repository directly (`IWorkItemStateTransitionRepository.GetAllByPredicate(t => t.WorkItemId == id)`). This keeps the default `WorkItem` load cheap (US-01's badge needs only the `currentStateEnteredAt` derived field on `WorkItem` per ADR-016; no transitions need to load for the work-item table to render).

ALSO derived from this decision: a single migration file generated via the existing `Create-Migration.ps1` PowerShell script (CLAUDE.md hard rule) — name `AddWorkItemStateTransitions` — produces matching Sqlite + Postgres migrations in lockstep.

---

## Alternatives Considered

**Option A — Owned collection inside the `WorkItem` aggregate.**

- Pros: aggregate consistency at the type level; transitions naturally cascade-delete with WorkItem (EF owned types behave this way by default).
- Cons: EVERY `workItemRepository.GetAll()` and `GetAllByPredicate(...)` call would either eager-load transitions (N+M rows across hundreds of items, expensive on the work-item table render) or use explicit `Include(...)` / `AsSplitQuery()` to avoid pulling them — adding load-path discipline at every consumer site. Worse, the sibling-consumer aggregates (`GROUP BY ToState SUM(duration)`) become awkward: you would need to flatten back out of the owned collection into a SQL projection, defeating the encapsulation benefit. The aggregate-consistency win is mostly philosophical — practical consistency is enforced by the sync code path that writes to both `WorkItem` and `WorkItemStateTransition` in the same `DbContext` save.
- **Rejected** because the consumer use-cases dominate the consumer count (3 features × 2+ aggregate queries each vs. 1 badge query) and SQL projections over a flat table are materially simpler than projections over a child collection.

**Option B — JSON column on `WorkItem`.**

- Pros: zero new tables, zero new migrations beyond an additive column; matches the existing `AdditionalFieldValues` pattern; single round-trip on WorkItem load.
- Cons: aggregate queries (`GROUP BY ToState`) would require either Postgres-specific JSON operators (breaks Sqlite lockstep migration policy) OR loading every WorkItem into memory and aggregating in C# (which is precisely what `aging-pace-percentiles` and `state-time-cumulative-view` are trying to avoid for performance reasons in their D10 cross-coordination note — they explicitly flagged a SHARED aggregation primitive as a profiling-driven optimization). The unbounded growth of the JSON list per item (months of transitions for long-lived epics) would also bloat the WorkItem row and slow EVERY work-item read.
- **Rejected** because the consumer aggregate semantics are incompatible with JSON column storage in a Sqlite-+Postgres-lockstep world, and the bloat penalty is paid on the read-heavy WorkItem table.

---

## Consequences

**Positive**:

- Consumer aggregations (`aging-pace-percentiles` percentile-per-state, `state-time-cumulative-view` SUM-per-state) become straightforward EF/LINQ queries: `_transitionRepo.GetAllByPredicate(t => t.WorkItemId == itemId && t.ToState == s)`, or for cross-team aggregation, `_transitionRepo.GetAll().Where(...).GroupBy(t => t.ToState).Select(g => …)`. Both translate to single SQL queries.
- The work-item table render path (US-01's `Time in State` column) loads zero transition rows — it reads `WorkItem.CurrentStateEnteredAt` (derived field per ADR-016) and computes `now - that` in the existing `WorkItemDto` projection.
- New table is independently indexable / queryable / archivable. If retention becomes a concern post-MVP (e.g. > 1 year of transitions per long-lived epic), a separate cleanup policy can ship without touching `WorkItem`.
- Sibling DESIGNs (`aging-pace-percentiles`, `state-time-cumulative-view`) get exactly the schema they assumed; their D11/D9 "no schema additions required" claims hold without revision.

**Negative**:

- One additional EF table to manage and migrate. Mitigated by single migration in lockstep, generated via the existing `Create-Migration.ps1`.
- Aggregate-consistency between `WorkItem.CurrentStateEnteredAt` (ADR-016) and the latest `WorkItemStateTransition` for that item is enforced by the SYNC CODE rather than by the type system. Mitigated by colocating both writes in one `DbContext.SaveChangesAsync()` call inside `WorkItemService.RefreshWorkItems` (ADR-017), and by an NUnit integration test that, for every connector × every fixture, asserts the invariant `currentStateEnteredAt == transitions.Where(t => t.ToState == State).Max(t => t.TransitionedAt)` after a full sync.

**Neutral**:

- Cascade delete behaviour matches the existing `Team.WorkItems` → `WorkItem` deletion lifecycle. No new lifecycle rules.
- No new repository abstraction is created beyond `IWorkItemStateTransitionRepository : IRepository<WorkItemStateTransition>` — follows the established `RepositoryBase<T>` pattern in `Services/Implementation/Repositories/`.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `WorkItem` MUST NOT hold a navigation collection of `WorkItemStateTransition` (no eager-load surprise on the read-heavy path) | NUnit reflection test asserting `typeof(WorkItem).GetProperty("StateTransitions")` returns null |
| `WorkItem.CurrentStateEnteredAt` must equal `MAX(WorkItemStateTransitions.TransitionedAt WHERE WorkItemId = X AND ToState = WorkItem.State)` after every full sync | Integration test in `WorkItemServiceTest.cs` per connector fixture |
| The two sibling consumers MUST query transitions via `IWorkItemStateTransitionRepository`, never via raw SQL or `LighthouseAppContext.WorkItemStateTransitions` directly | ArchUnitNET test extending the existing suite: classes outside `Services.Implementation.Repositories` and `Services.Implementation.Metrics.*` may not reference `DbSet<WorkItemStateTransition>` |

---

## Cross-feature impact

- `aging-pace-percentiles` DESIGN: consume `IWorkItemStateTransitionRepository` directly; the per-state percentile query is `GetAllByPredicate(t => t.WorkItemId IN closedItemIds && t.ToState != t.FromState).GroupBy(t => t.FromState)…` (each transition's `(fromState, transitionedAt)` paired with the next transition's `transitionedAt` gives age-at-state-exit; the prior-transition lookup is a windowed query). No upstream schema change required.
- `state-time-cumulative-view` DESIGN: same repository; queries are SUM-per-state. No upstream schema change required.
- `epic-5074-blocked-items` (post-MVP, separate epic, different mechanism): unaffected. That epic uses its own capture mechanism (per the Epic 4144 README cross-cutting note).
