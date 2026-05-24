# ADR-017: Transition Capture — Source-of-Truth-First in Connectors, Sync-Delta Fallback in `WorkItemService`

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-015/016/018)
**Date**: 2026-05-24
**Feature**: time-in-state-and-staleness (Epic 4144 MVP bundle, slice A+B1+D)
**Decider**: Morgan (Solution Architect)

---

## Context

DISCUSS D1 locks the dispatch principle: *"State transitions are pulled from source-of-truth where available (Jira, ADO confirmed; Linear investigated at DESIGN). For CSV or any connector that can't expose history, fall back to sync-side delta capture (compare current State to last-known on every sync)."* DESIGN must locate WHERE that branch logic lives so that it does not duplicate per-connector and so that connectors stay technology-specialised.

Today's connector landscape:

- **Jira** (`JiraWorkTrackingConnector` + `IssueFactory.GetTransitionDate`): already walks the issue's `changelog.histories` array, extracts only the LAST transition into each target state, and discards everything else. The walker function `ExtractDateOfStateTransitionFromHistory` already sees every `(fromString, toString, createdDate)` triplet on each history entry where `field == "status"`. **Source-of-truth available; current code discards 95% of what it reads.**
- **Azure DevOps** (`AzureDevOpsWorkTrackingConnector.GetStateTransitionDateThrottled`): same shape — calls `witClient.GetRevisionsAsync(workItemId)`, iterates revisions, detects state changes via `RevisionWasChangingState`, currently keeps only the LAST transition matching the target state predicate. **Source-of-truth available; current code discards 95% of what it reads.**
- **Linear** (`LinearWorkTrackingConnector` + `LinearResponses.IssueNode`): the GraphQL query today does NOT request the `history` connection on `IssueNode`. Linear's GraphQL API exposes `Issue.history` with `IssueHistory.fromState`/`toState`/`createdAt` per their public schema, but it is not free — it adds nodes to the GraphQL request and to pagination state. **Source-of-truth investigation outcome: AVAILABLE via `history` field on `Issue`; needs explicit query addition.**
- **CSV** (`CsvWorkTrackingConnector`): reads one snapshot per file load. No history available at the source. **Sync-delta fallback required.**

The current `WorkItemService.RefreshWorkItems` (line 51–86 of `WorkItemService.cs`) already does the diff between `storedWorkItems` and `actualWorkItems` returned by the connector — it knows whether an item is new, updated, or removed. The State-change detection (`storedItem.State != incoming.State`) is a one-line check inside that same loop. That existing diff is the natural seam for the sync-delta fallback.

Three plausible dispatch architectures:

- **A. Connector-owned full responsibility** — each connector returns `(WorkItem, IEnumerable<WorkItemStateTransition>)` and is responsible for emitting transitions however it can. CSV / Linear-without-history return an empty transitions list, then `WorkItemService` separately runs sync-delta logic on top. Risk: two code paths in `WorkItemService` (the connector-provided list + the sync-delta supplement) and silent missed-transition bugs when both fire or neither does.
- **B. Connector returns transitions, `WorkItemService` does sync-delta ONLY for connectors that opt in via a capability flag** — `IWorkTrackingConnector.SupportsTransitionHistory: bool`. If `true`, connector emits real transitions; if `false`, `WorkItemService` runs the sync-delta logic.
- **C. Two-tier capture — connector ALWAYS returns `transitions` (possibly empty), `WorkItemService.RefreshWorkItems` ALWAYS runs sync-delta as a safety net to catch state changes the connector did not report** — guarantees coverage but risks double-counting if the connector returns the same transition that sync-delta would synthesise.

---

## Decision

**Option B — Capability-flagged dispatch with explicit single-source-of-emission per item per sync.**

```
public interface IWorkTrackingConnector
{
    // … existing methods …

    /// <summary>
    /// True if this connector emits real state-transition history alongside each WorkItem
    /// (via WorkItem.SyncedTransitions populated in GetWorkItemsForTeam).
    /// False = WorkItemService synthesises sync-delta transitions in its RefreshWorkItems loop.
    /// </summary>
    bool SupportsTransitionHistory { get; }
}
```

Concrete dispatch:

- **`JiraWorkTrackingConnector.SupportsTransitionHistory = true`** — extend `IssueFactory.GetStartedAndClosedDate` (or add a sibling `GetAllStateTransitions` that walks the SAME changelog histories) to emit a `List<WorkItemStateTransition>` per issue. The existing changelog walker already iterates every history entry; the new method captures every `(fromString, toString, createdDate)` where `changedField == "status"` instead of only those that hit a category boundary. Emit those as transition records attached to the returned `WorkItemBase` via a new transient property `SyncedTransitions: IReadOnlyList<WorkItemStateTransition>` (NOT mapped to EF on `WorkItemBase` — it is sync-transport data, consumed and detached by `WorkItemService.RefreshWorkItems`).
- **`AzureDevOpsWorkTrackingConnector.SupportsTransitionHistory = true`** — same shape. Extend `GetStateTransitionDateThrottled` (or add a sibling `GetAllStateTransitions`) to capture every `(state, changedDate)` revision where `RevisionWasChangingState` returns true, then materialise the `(fromState, toState, changedDate)` sequence from the ordered revisions.
- **`LinearWorkTrackingConnector.SupportsTransitionHistory = true`** — extend the existing GraphQL query for `IssueNode` to also request the `history` connection with `nodes { fromState { name } toState { name } createdAt }`. Walk the result the same way as Jira/ADO. If a customer's Linear plan or API quota does not permit the `history` query (detected via a 403/graphql-error response on first sync), the connector logs a warning and downgrades to `SupportsTransitionHistory = false` at runtime (per-connection, not per-class). This downgrade is logged once per connection on first failure and surfaced as a tooltip on the badge: `"Approximate — based on sync cadence"` (per DISCUSS US-01 AC line 3).
- **`CsvWorkTrackingConnector.SupportsTransitionHistory = false`** — no history available at the source; `WorkItemService` runs sync-delta.

`WorkItemService.RefreshWorkItems` becomes:

```
For each incoming work item:
  Find stored item (if any).
  If new (no stored item):
    Add the WorkItem.
    If SupportsTransitionHistory:
      Persist all of incoming.SyncedTransitions.
      Set CurrentStateEnteredAt = SyncedTransitions.Where(t => t.ToState == State).Max(t => t.TransitionedAt)
        ?? incoming.CreatedDate.
    Else:
      Append one synthetic transition { FromState = "", ToState = State, TransitionedAt = DateTime.UtcNow }.
      Set CurrentStateEnteredAt = DateTime.UtcNow.
  Else (existing item):
    Update the WorkItem as today.
    If SupportsTransitionHistory:
      Persist NEW transitions: SyncedTransitions minus those already stored (dedup by (WorkItemId, ToState, TransitionedAt)).
      Set CurrentStateEnteredAt = SyncedTransitions.Where(t => t.ToState == new State).Max(t => t.TransitionedAt)
        ?? stored.CurrentStateEnteredAt.
    Else (sync-delta):
      If stored.State != incoming.State:
        Append a synthetic transition { FromState = stored.State, ToState = incoming.State, TransitionedAt = DateTime.UtcNow }.
        Set CurrentStateEnteredAt = DateTime.UtcNow.
      Else:
        No-op.

All writes flush in one SaveChangesAsync.
```

The transition-dedup-by-`(WorkItemId, ToState, TransitionedAt)` guards against the case where the same Jira/ADO sync re-fetches an issue whose transitions are already stored — the next sync only persists genuinely-new transitions. Index `(WorkItemId, TransitionedAt)` from ADR-015 makes the dedup probe O(log n) per transition.

---

## Alternatives Considered

**Option A — Connector-owned full responsibility, no capability flag.**

- Pros: cleaner connector interface (no flag); `WorkItemService` does not branch on connector capability.
- Cons: `WorkItemService` would still need to run sync-delta logic separately for connectors that returned empty transitions; without the capability flag, deciding "is the empty list because nothing changed or because the connector cannot tell us?" requires either a sentinel value (null vs empty) or per-connector special-casing. Both alternatives leak the same concept the flag exposes explicitly.
- **Rejected** because the explicit flag IS the architectural commitment; hiding it behind a sentinel value or per-connector switch loses the clarity.

**Option C — Two-tier capture (connector + always-on sync-delta safety net).**

- Pros: belt and braces; guaranteed coverage even if a connector's history query fails silently.
- Cons: double-counts in the common case. The Jira/ADO connectors produce timestamps from the source-system history; the sync-delta safety net would synthesise its own `DateTime.UtcNow` transition for the same observed state change. Dedup logic would need to span source-of-truth-timestamp transitions and sync-time transitions for the SAME state change — not a clean equality test. Furthermore, "guarantees coverage even if connector history fails silently" is not what we want: if the connector silently degrades, we want a logged warning AND a visible badge tooltip ("Approximate"), not a hidden synthesis of approximate transitions that the user cannot distinguish from real ones. The Linear runtime-downgrade path makes this explicit by design.
- **Rejected** for the silent-degradation reason; making the failure observable is more important than belt-and-braces redundancy.

---

## Consequences

**Positive**:

- Jira / ADO badge accuracy ≤ 1 day vs source-system (the existing walker is already proven accurate for Started/Closed in production; emitting all transitions captured by the same walker inherits the same accuracy).
- CSV / Linear-fallback badge accuracy bounded by sync cadence, surfaced honestly to the user via the tooltip per DISCUSS US-01 AC.
- Single seam (`WorkItemService.RefreshWorkItems`) holds the dispatch — extending to a new connector is a 2-line change (add `SupportsTransitionHistory` to the interface implementation; populate `SyncedTransitions` if `true`).
- The sync-delta path costs ZERO incremental work beyond the State-equality check that the existing diff loop ALREADY needs (today it just doesn't act on it). The capability flag and synthetic-transition emission are the new code.
- Sibling MVP-bundle features (`aging-pace-percentiles`, `state-time-cumulative-view`) consume `WorkItemStateTransition` rows that may have either real source-system timestamps or synthetic sync-cadence timestamps — both consumers reason about the rows the same way; only the badge tooltip distinguishes them at the UX layer. Sibling DESIGNs do NOT need to know which path produced any given transition.

**Negative**:

- Linear connector needs a GraphQL query extension. If Linear's `history` connection is paginated heavily, this could slow the Linear sync materially. Mitigation: the existing connector already uses `GetWithPagination<T>`; the `history` field per issue is bounded (typically <20 transitions per issue), so the additional payload is linear in transition count, not item count squared. If profiling shows the addition is excessive, a follow-up can flip the Linear default to `false` and rely on sync-delta — that follow-up is a one-line change.
- The transient `SyncedTransitions` property on `WorkItemBase` is a slight abstraction leak (it's sync-transport data, not domain state). Mitigation: marked `[NotMapped]` (or implemented as a separate "sync result" record returned alongside the work item from the connector, decided by software-crafter at GREEN). The architectural contract is: the field is consumed and cleared inside `WorkItemService.RefreshWorkItems`, never accessed by any controller or downstream consumer.

**Neutral**:

- Linear's potential runtime downgrade is an observable, log-once event — not a silent fall-through. UX surfaces honestly via the existing tooltip mechanism.
- The dedup-by-(WorkItemId, ToState, TransitionedAt) probe replaces the alternative of "always wipe and replace transitions per sync" — the latter would invalidate the immutable history claim sibling features rely on (since historical transitions never change in the source system, they should never change in Lighthouse either).

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Every `IWorkTrackingConnector` implementation declares `SupportsTransitionHistory` | C# interface forces it at compile time; ArchUnitNET test confirms no implementation overrides as protected/internal accidentally |
| `WorkItemService.RefreshWorkItems` is the ONLY caller that writes to `WorkItemStateTransitions` and the ONLY mutator of `WorkItem.CurrentStateEnteredAt` | ArchUnitNET test extends the existing suite: classes outside `Services.Implementation.WorkItems.WorkItemService` may not assign these |
| The Linear runtime-downgrade fires at most once per connection per process and is logged with the connection ID + failure reason | Unit test in `LinearWorkTrackingConnectorTests` asserts both behaviours against a mocked GraphQL transport |
| Per-connector integration test asserts: after a sync of a 3-transition fixture, exactly 3 rows exist in `WorkItemStateTransitions` for that item with the expected `(FromState, ToState, TransitionedAt)` triplets | Per-connector NUnit fixture in `Tests/Services/Implementation/WorkItems/WorkItemServiceTest.cs` (or per-connector file under the existing Connector test folders) |
| The integration test for the dedup invariant: running the same sync twice produces no duplicate transitions | Same test fixture asserts `transitionsRepo.GetAll().Count()` is identical after sync #1 and sync #2 |

---

## Cross-feature impact

- Sibling MVP DESIGNs receive a steady stream of transition rows whose semantics are clear (real timestamp vs sync-cadence timestamp is opaque at the row level — sibling consumers compute against the rows regardless). No upstream DISCUSS revisions required for either sibling.
- `epic-5074-blocked-items` (post-MVP, separate epic): unaffected — blocked-time history uses a different capture mechanism per the Epic 4144 cross-cutting note.
