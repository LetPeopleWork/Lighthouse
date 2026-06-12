# ADR-068: Blocked-Time Capture — `WorkItemBlockedTransition` Owned Entity, Enter via the Existing `WorkItemBlocked` Event, Leave via a New `WorkItemUnblocked` Domain Event

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (Slice 02 — per-item blocked duration)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: follows ADR-015/016/017 (`WorkItemStateTransition` placement + capture-dispatch) as the transition-capture precedent and ADR-027 / Epic 5121 (domain-event bus — the memory note "prefer the domain-event bus on changes"). README L1 (Lighthouse-side per-sync capture) governs. Resolves DISCUSS D-CAPTURE, D5, D6.

---

## Context

Slice 02 must surface "blocked \<N>d" per item, derived from when the item's CURRENT blocked spell began (D6 — current spell, not cumulative). Resolution is sync cadence (L1, approximate — disclosed in a tooltip). The capture must be a NEW entity, not `WorkItemStateTransition` (README L1 data-foundation note: blocked is a Lighthouse-derived concept orthogonal to state).

Verified code reality:
- The edge-triggered `WorkItemBlocked(int WorkItemId, string Reason) : IDomainEvent` already fires on the false→true transition in `WorkItemService` (L121: `if (!syncedItem.WasBlockedBeforeSync && workItem.IsBlocked)`), dispatched via `IDomainEventDispatcher.PublishAsync` (L182). This is the natural ENTER seam.
- There is **no** `WorkItemUnblocked` event. Leave-detection (`WasBlocked && !IsBlocked`) does not exist today.
- `WasBlocked` is computed at sync (L103) from the prior persisted item; after ADR-067 it routes through `IBlockedItemService.IsBlocked`.
- Transition entities precedent: `WorkItemStateTransition` is an owned collection on the work item, captured per-sync and dispatched via the capture-dispatch pattern (ADR-017).

The question: (1) the capture entity's shape and placement, (2) how leave is detected and whether it becomes a domain event, (3) how "blocked Nd" (current spell) and first-observation "—" derive.

---

## Decision

### 1. `WorkItemBlockedTransition` is an owned collection on the work item

```csharp
public class WorkItemBlockedTransition
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? LeftAt { get; set; }   // null = open (current) spell
}
```

Persisted as an owned collection on the work item (mirroring `WorkItemStateTransition`'s placement + EF mapping + provider coverage; migration via the `CreateMigration` PowerShell script across all providers). NOT reusing `WorkItemStateTransition` — blocked is orthogonal to state (an item can be blocked in any state). At most one OPEN spell per item (`LeftAt == null`) at a time — enforced by the handler (close-before-open).

### 2. Enter reuses `WorkItemBlocked`; leave is a NEW `WorkItemUnblocked` event (domain-event bus, not inline)

- **Enter**: a handler `WorkItemBlockedTransitionRecordingHandler : IDomainEventHandler<WorkItemBlocked>` opens a spell — creates a `WorkItemBlockedTransition { EnteredAt = sync timestamp, LeftAt = null }` if no open spell exists (idempotent: a second `WorkItemBlocked` with an already-open spell is a no-op, not a duplicate).
- **Leave**: introduce `WorkItemUnblocked(int WorkItemId) : IDomainEvent`, dispatched at the symmetric seam in `WorkItemService` — the existing enter edge is at `WorkItemService.cs` ~L121 (`if (!syncedItem.WasBlockedBeforeSync && workItem.IsBlocked)`, the `events.Add(new WorkItemBlocked(...))` site), collected into the per-sync `events` list and published via `IDomainEventDispatcher.PublishAsync` in the `PublishDomainEvent` switch (~L182). The leave branch is added IMMEDIATELY adjacent: `if (syncedItem.WasBlockedBeforeSync && !workItem.IsBlocked) events.Add(new WorkItemUnblocked(workItem.Id));`, with a matching `case WorkItemUnblocked unblocked:` arm in the publish switch (~L182, parallel to the `case WorkItemBlocked` arm). `WasBlockedBeforeSync`/`WasBlocked` (L74/L103) route through `IBlockedItemService.IsBlocked` after ADR-067. A handler `IDomainEventHandler<WorkItemUnblocked>` closes the open spell (`LeftAt = sync timestamp`). (Line numbers are pre-ADR-067 anchors; DELIVER confirms exact lines after the `IsBlocked` refactor lands.)

Leave is a domain EVENT (not inline detection) because: (a) the codebase is standardizing cross-aggregate reactions on the bus (ADR-027/5121, memory "prefer the domain-event bus"); (b) it makes enter and leave symmetric and independently testable; (c) `WorkItemUnblocked` is a genuinely useful domain signal future features (notifications, the over-time recorder cross-check) can consume — it is not speculative, it pairs an event that already half-exists. The edge-trigger seam already computes `WasBlockedBeforeSync` — adding the symmetric branch is one condition.

### 3. "blocked \<N>d" (current spell) and first-observation "—" derivation

- Per-item blocked duration = `now − EnteredAt` of the item's OPEN spell (`LeftAt == null`). Surfaced on `WorkItemDto` as a new field `blockedSince : DateTime?` (the open spell's `EnteredAt`, or null) — the FE computes the day count with the SAME day convention the time-in-state badge uses (ADR-026 day-count consistency), and renders "blocked Nd" + an "Approximate — based on sync cadence" tooltip.
- **First-observation "—"**: an item already blocked at release has NO captured open spell (the `WorkItemBlocked` enter event only fires on a false→true edge, which never occurred for an already-blocked item). `blockedSince` is null ⇒ the FE renders "—" until the next edge-transition establishes a baseline (twin of the time-in-state first-observation behaviour). The handler does NOT fabricate an `EnteredAt` for items blocked before capture began (honest, forward-only — consistent with the snapshot model in ADR-069).
- On unblock the open spell closes; `blockedSince` becomes null ⇒ the badge clears.

`blockedSince` is an **additive** field on `WorkItemDto` (no client version gate per ADR-062's additive rule — see ADR-072).

---

## Alternatives Considered

**Leave detection — Option A (chosen): new `WorkItemUnblocked` domain event + handler.**
- Pros: symmetric with `WorkItemBlocked`; on the standardized bus; independently testable; a reusable domain signal; the seam already computes `WasBlockedBeforeSync`.
- Cons: a new event type + dispatch site + handler. Minimal; pairs an event that already half-exists.

**Leave detection — Option B: inline spell-close inside `WorkItemService` (no event).**
- Pros: no new event type.
- Cons: bypasses the bus the codebase is standardizing on; couples capture to the sync service; asymmetric with the event-based enter. Rejected — the memory note and ADR-027 direction favour the event; the cost delta is one event record.

**Capture entity — Option C: reuse `WorkItemStateTransition` with a synthetic "Blocked" pseudo-state.**
- Pros: no new entity.
- Cons: pollutes the state-transition stream with a non-state; blocked is orthogonal to state (blocked-in-Review ≠ a Review transition); README L1 explicitly forbids it. Rejected.

**Duration shape — Option D: cumulative across all spells.**
- Rejected by D6 — the badge answers "how long has THIS item been blocked right now?", matching the time-in-state badge shape. Cumulative is a different (future) metric.

---

## Consequences

**Positive**:
- Symmetric enter/leave capture on the bus; per-item duration derives from the single `IsBlocked` definition (ADR-067) via the captured open spell.
- `WorkItemUnblocked` is a reusable signal (future notifications, recorder cross-checks).
- First-observation "—" is honest (no fabricated history), matching the established time-in-state and forward-only snapshot conventions.

**Negative**:
- Sync-cadence resolution: a block+unblock within one cadence collapses to one (or zero) captured spell — a documented L1 limitation, disclosed in the tooltip (R2). Re-block after a closed spell opens a NEW spell (current-spell semantics preserved).
- A new entity + migration (real-provider test required; InMemory misses migrations).

**Neutral**:
- `blockedSince` additive on `WorkItemDto` ⇒ no client version gate (ADR-072).

---

## Earned Trust — probing the capture

- **Enter probe**: an integration test blocks a fixture item (rule match) on a sync, asserts a `WorkItemBlockedTransition` with `EnteredAt` = that sync and `LeftAt == null`, and `blockedSince` populated on the DTO.
- **Leave probe**: the item stops matching on the next sync; assert `WorkItemUnblocked` is dispatched exactly once and the open spell closes (`LeftAt` set); `blockedSince` becomes null.
- **First-observation probe**: an item already blocked when capture begins (no prior edge) has null `blockedSince` ⇒ "—"; the next genuine edge establishes the baseline.
- **Idempotency probe**: re-running the enter handler with an already-open spell is a no-op (no duplicate spell); the open-spell uniqueness is asserted.
- **Re-block probe**: block → unblock (spell closes) → block again opens a NEW open spell; the badge reads the new spell's duration, not the old one.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| At most one open spell (`LeftAt == null`) per item | NUnit on the enter handler: second enter with an open spell is a no-op |
| Enter via `WorkItemBlocked`, leave via `WorkItemUnblocked`, both on the dispatcher | NUnit dispatch test: false→true ⇒ one `WorkItemBlocked`; true→false ⇒ one `WorkItemUnblocked`; no inline spell mutation outside the handlers (ArchUnitNET) |
| `blockedSince` derives only from `IsBlocked` items (ADR-067) | Property test: an item not matching the rule set has no open spell |
| First-observation items show null `blockedSince` (no fabricated history) | NUnit: pre-capture-blocked item ⇒ null until the next edge |
| Capture entity is NOT `WorkItemStateTransition` | ArchUnitNET: `WorkItemBlockedTransition` is a distinct owned entity |

---

## Cross-feature impact

- ADR-015/016/017: parallel transition-capture entity; same owned-collection + capture-dispatch idiom.
- ADR-027 / Epic 5121: a new `WorkItemUnblocked` event on the shared bus; `WorkItemBlocked` reused.
- ADR-067: `blockedSince` derives from the single `IsBlocked`.
- ADR-070 (blocked→stale): consumes `blockedSince` as the blocked-duration source.
- Lighthouse-Clients: `blockedSince` additive on `WorkItemDto` ⇒ no version gate (ADR-072).
