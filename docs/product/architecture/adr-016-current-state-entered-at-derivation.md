# ADR-016: `currentStateEnteredAt` — Sync-Time Derived, Persisted on `WorkItem` (not query-time computed)

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-015/017/018)
**Date**: 2026-05-24
**Feature**: time-in-state-and-staleness (Epic 4144 MVP bundle, slice A+B1+D)
**Decider**: Morgan (Solution Architect)

---

## Context

DISCUSS US-01 mandates that the work-item table on `/teams/{teamId}` shows `<integer>d in <currentStateName>` for every row, where the integer matches the source-system history within 1 day (Jira/ADO) or is "Approximate — based on sync cadence" (CSV / no-history connectors). The badge renders for every visible row, sortable, and must render quickly.

US-02 highlights items where days-in-state ≥ `Team.StalenessThresholdDays + 1`. The threshold takes effect on next page render — no sync required (DISCUSS US-02 AC line 3). This means the chip-red computation runs in the API response per render, not in the sync path.

DISCUSS feature-delta driving-ports table specifies adding `currentStateEnteredAt: ISO8601` to the work-item-list API response. That is the contract the FE consumes; the question DESIGN must settle is **whether `currentStateEnteredAt` is a column on the `WorkItem` table or derived per-request from the transitions table**.

The numerical badge value `daysInState = floor((now - currentStateEnteredAt).TotalDays)` is then computed client-side or in `WorkItemDto`'s constructor. The threshold comparison `daysInState > Team.StalenessThresholdDays` is also rendered client-side from `currentStateEnteredAt` + the team's threshold (so a threshold change takes effect on next render — US-02 AC line 3).

Two plausible derivations of `currentStateEnteredAt`:

- **A. Sync-time, persisted column on `WorkItem`** — `WorkItem.CurrentStateEnteredAt: DateTime?`. Updated by the sync code path when (and only when) a new state is observed for that item, in the same `SaveChangesAsync` that appends the new `WorkItemStateTransition` row. Read path is `wi.CurrentStateEnteredAt` — zero subqueries, zero joins.
- **B. Query-time, computed from transitions** — `WorkItemDto` constructor (or a service helper) runs `_transitionRepo.GetAllByPredicate(t => t.WorkItemId == wi.Id && t.ToState == wi.State).Max(t => t.TransitionedAt)` per item. Computed for every render.

The work-item table is one of the more frequently-loaded surfaces in Lighthouse (team detail page, default landing for the team view). A typical team has 20–200 in-flight items; queries that fan out to N+1 sub-queries are precisely the pattern `bug-5016-cache-thread-safety` (the pre-req) was working around.

---

## Decision

**Option A — Sync-time, persisted column on `WorkItem`.**

```
public class WorkItemBase : IEntity
{
    // … existing fields …
    public DateTime? CurrentStateEnteredAt { get; set; }
}
```

EF migration adds the column as nullable on `WorkItems` (allowing existing rows pre-migration to have `null` until the next sync establishes a baseline — matches DISCUSS US-01 AC line 4: "For items whose current state was first observed in this sync (no prior data), the badge renders `—` until the next sync establishes a baseline").

Sync-side update rule (applied in `WorkItemService.RefreshWorkItems`, per ADR-017):

```
1. For each work item returned by the connector:
   a. Compare incoming.State to storedItem.State.
   b. If they differ (or storedItem is null = first observation):
      - Append a WorkItemStateTransition row { WorkItemId, FromState = storedItem?.State ?? "", ToState = incoming.State, TransitionedAt = transitionTimestamp }
      - Set storedItem.CurrentStateEnteredAt = transitionTimestamp
   c. Else: do nothing (state unchanged → currentStateEnteredAt unchanged).
2. All transitions + currentStateEnteredAt updates flush in a single SaveChangesAsync.
```

Where `transitionTimestamp` comes from:

- **Jira / ADO (source-of-truth path)**: the transition's actual `changedDate` from the source-system changelog/revision (extracted by extending the existing `IssueFactory.GetTransitionDate` / `AzureDevOpsWorkTrackingConnector.GetStateTransitionDateThrottled` — see ADR-017). For ALL transitions captured retroactively in one sync (e.g. an item that changed state 3 times since last sync), each transition gets its real timestamp; `CurrentStateEnteredAt` ends up at the timestamp of the LAST transition that ended in `incoming.State`.
- **CSV / Linear-without-history (sync-delta path)**: `DateTime.UtcNow` at the sync moment. Documented as approximate via the `"Approximate — based on sync cadence"` tooltip (DISCUSS US-01 AC line 3).

`WorkItemDto` exposes `CurrentStateEnteredAt` to the FE; FE computes `daysInState` and the red-threshold check on render. The badge `—` for first-observation items follows from `CurrentStateEnteredAt == null`.

---

## Alternatives Considered

**Option B — Query-time, computed from transitions.**

- Pros: "single source of truth" — `currentStateEnteredAt` is always exactly `MAX(transitions WHERE ToState = current)` with no possibility of skew between the WorkItem row and the transitions table. No new column. Zero possibility of a sync code path forgetting to update one of the two.
- Cons: every work-item-table render does N sub-queries against `WorkItemStateTransitions` (one per visible item), or one large windowed query that the EF/Linq translator may or may not optimise well across Sqlite + Postgres. For a 200-item team table this is N+1 territory; even with eager loading via `Include`, the row volume balloons because each work item joins all its transitions just to extract the MAX of a filtered subset. Cache the result inside `BaseMetricsService`? Now we have a cache-invalidation problem on every sync — which is precisely the problem `bug-5016-cache-thread-safety` shipped to mitigate (mitigation, not removal).
- **Rejected** because the read path frequency (every team detail render) dominates the write path frequency (per-sync, every few hours), AND because the consistency-by-construction win of Option B is replicated in Option A by colocating the two writes in one `SaveChangesAsync` plus an integration test that asserts the invariant after every sync (ADR-015 already specifies the invariant test).

**Option B' — Query-time but precomputed materialised view / Postgres-only computed column.**

- Pros: read path is cheap.
- Cons: breaks the Sqlite-+Postgres-lockstep policy (CLAUDE.md, ADR-014 precedent, sibling features all live in lockstep). Sqlite computed columns are limited; emulating Postgres materialised views in Sqlite is not free.
- **Rejected** as it tilts the persistence layer asymmetrically per provider for a change that Option A solves with a plain column.

**Option C — In-memory cache populated lazily, keyed on (teamId, workItemId)**.

- Pros: avoids the WorkItem-row column.
- Cons: cache miss on first render is slow; cache populates from the transitions table (same N+1 problem as Option B at miss time); cache invalidation on sync is non-trivial and `bug-5016` is precisely about cache thread-safety bugs in this area. Adding more cached metric flavours invites more bug-5016-class bugs.
- **Rejected** in favour of "make the read path cheap by writing the field once at sync time."

---

## Consequences

**Positive**:

- Read path for the work-item table is a single `SELECT … FROM WorkItems WHERE TeamId = X` — same query the table renders today, plus one extra nullable column. Zero N+1, zero subqueries, zero EF projection complexity.
- The badge value computation in `WorkItemDto` is one line: `DaysInState = CurrentStateEnteredAt.HasValue ? (int)(DateTime.UtcNow.Date - CurrentStateEnteredAt.Value.Date).TotalDays : (int?)null`. (Implementation detail noted only because the boundary computation is a place where the existing `WorkItemBase.GetDateDifference` rounding convention applies — the actual implementation belongs to software-crafter.)
- Threshold-change-on-render (US-02 AC line 3) works trivially: the FE has `currentStateEnteredAt` and the team's `stalenessThresholdDays`; the comparison is reactive to threshold edits without any sync.
- The "first-observation" UX (US-01 AC line 4 — render `—`) maps cleanly to `currentStateEnteredAt == null`, which is the state of every existing row at migration time AND the state of newly-discovered items mid-sync.

**Negative**:

- One extra nullable column on the `WorkItems` table. Trivial migration cost.
- The invariant `WorkItem.CurrentStateEnteredAt == MAX(transitions.TransitionedAt WHERE ToState = WorkItem.State)` is enforced by the sync code rather than by the type system. Mitigated by:
  - Colocating both writes in one `SaveChangesAsync` in `WorkItemService.RefreshWorkItems` (ADR-017).
  - An NUnit integration test (one per connector × representative fixture) that runs a sync against a canned fixture and asserts the invariant.
  - A startup self-check that runs in DEBUG / dev builds only (optional, can be skipped — the integration test already catches it).

**Neutral**:

- `WorkItemBase.CycleTime` and `WorkItemBase.WorkItemAge` are unchanged (they're computed from `StartedDate` / `ClosedDate` per `WorkItemBase` today, sibling D9 lock). `CurrentStateEnteredAt` is a NEW derived-but-persisted field that coexists with them — DISCUSS D9 explicitly locks "no changes to existing Cycle Time or Work Item Age semantics".

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `WorkItem.CurrentStateEnteredAt` is updated ONLY by `WorkItemService.RefreshWorkItems` (not by any controller, not by any sibling-consumer service) | ArchUnitNET test: classes outside `Services.Implementation.WorkItems.WorkItemService` may not assign `CurrentStateEnteredAt` |
| The invariant `CurrentStateEnteredAt == MAX(transitions.TransitionedAt WHERE ToState = State)` holds after every full sync | Integration test in `WorkItemServiceTest.cs` parameterised over connectors |
| The FE-side computation `daysInState = floor((now - currentStateEnteredAt).Days)` matches the source-system "how many days in this state" within 1 day for Jira / ADO (per DISCUSS US-01 AC line 2) | Integration test against recorded Jira + ADO API-response fixtures asserts exact day count |

---

## Cross-feature impact

- `aging-pace-percentiles` D11: explicitly locks `WorkItem.currentStateEnteredAt` as available. ADR-016 confirms it as a persisted column — sibling consumes it via `wi.CurrentStateEnteredAt`. No upstream change required.
- `state-time-cumulative-view` D9 and D11: same — sibling consumes via the persisted column; the "in-flight contribution" computation `now - currentStateEnteredAt` becomes a direct field access. No upstream change required.
- `epic-5074-blocked-items` (post-MVP): unaffected (separate mechanism).
