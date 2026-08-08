# ADR-141: Time-driven derivations are evaluated over the stored set, not the fetched set

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `epic-5687-faster-updates` (ADO Epic #5687 "Faster Updates")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

`WorkItemService.RefreshWorkItems` raises four kinds of domain event as it syncs. Three of them are
change-driven: `WorkItemTransitioned` comes off a new transition, `WorkItemBlocked` and
`WorkItemUnblocked` off a rising or falling edge in the blocked verdict. All three are functions of the
record's own fields, so an unchanged record cannot produce one.

The fourth is not:

```csharp
private static bool IsStale(Team team, WorkItem workItem, DateTime syncTime)
{
    if (workItem.StateCategory != StateCategories.Doing || !workItem.CurrentStateEnteredAt.HasValue)
        return false;

    return (syncTime - workItem.CurrentStateEnteredAt.Value).TotalDays > team.StalenessThresholdDays;
}
```

Staleness is a function of **elapsed wall-clock time** against a stored timestamp. Nothing about the
record has to change for it to become true — that is the entire point of the signal.

Today `AddStalenessEventIfThresholdCrossed` is called from inside the loop over *fetched* items:

```
foreach (var item in actualWorkItems)   // everything the full fetch returned
    ...
    AddStalenessEventIfThresholdCrossed(team, workItem, syncTime, events);
```

Under a full fetch that loop covers every stored item, so the distinction never surfaced. Under ADR-138
it covers only the records that moved — and **the item that goes stale is precisely the item that stops
being fetched.** Left as-is, incremental sync would mean nothing ever goes stale again: no error, no
failed sync, no log line, and `WorkItemBecameStale` simply stops firing. Every other test stays green.

This also touches an ADR-027 guarantee. The domain-event dispatcher is transport-only, with recovery by
"the next re-sync re-derives it". That promise holds only if a re-sync still evaluates every signal over
every record.

## Decision

**Time-driven derivations are evaluated in their own pass over the stored set, after the sync commits.**

`AddStalenessEventIfThresholdCrossed` moves out of the `foreach (actualWorkItems)` body and into a
second loop over the team's stored work items. The rule itself is unchanged — only what it is called
over. The events it produces join the same collection and are published through the same
`PublishDomainEvents` call, so event ordering and the after-commit dispatch contract are untouched.

**The pass runs regardless of mode.** A `delta` cycle that fetched zero records still evaluates
staleness over every stored record, because that is the cycle most likely to have something newly stale.

**Change-driven signals stay on the fetched set.** Transitions and the blocked rising/falling edge are
functions of the record's own fields; evaluating them over unchanged records would be work that can only
produce nothing.

**This is the general rule, not a staleness special case.** Any future signal that is a function of
elapsed time rather than of a field change belongs in this pass. `BlockedStalenessThresholdDays` already
exists on the same aggregate and is the obvious next member.

**It stays inside `WorkItemService`.** All four event kinds are collected in one method, which is what
makes "an item untouched past the threshold still raises `WorkItemBecameStale` under delta" a readable
test rather than an integration exercise across a dispatcher.

## Consequences

**Positive**

- Closes the sharpest failure mode incremental sync creates, and closes it by construction rather than
  by a guard that could itself be skipped.
- Preserves ADR-027's "the next re-sync re-derives it" recovery guarantee.
- Gives a name to a category — time-driven versus change-driven — so the next signal of this kind lands
  in the right loop without rediscovering the trap.
- The staleness rule is untouched, so its existing tests keep their meaning.

**Negative / accepted**

- A second pass over the stored set each cycle. It is an in-memory loop over records the sync already
  loaded to compute the removal set, so the cost is a loop, not a query.
- `WorkItemService.RefreshWorkItems` gains a step, in a method that is already the longest in the class.
  Accepted deliberately: the alternative — moving it out — separates event collection across two places
  for a four-line rule.
- `WasStaleAtLastSync` is now written for records the cycle did not otherwise touch, so a delta cycle can
  produce a write even when no payload was fetched. That is correct (the flag is the edge detector) but
  it means "fetched = 0" does not imply "no writes".

## Alternatives Considered

**A handler on `TeamDataRefreshed`.** A `WorkItemStalenessHandler` subscribing to the refresh event,
mirroring [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md), which hangs
percentile recording off exactly this event. Genuinely decoupled, purely additive, and the precedent is
close. **Rejected** on two counts: ADR-107's handler *records* a projection, whereas this one would
*emit further domain events from inside a handler*, which is a shape the codebase does not currently
have; and the handler would re-query the team's work items that the sync had in memory moments earlier.
Worth revisiting if the time-driven category grows enough to justify its own subscriber.

**An extracted `IStalenessEvaluator`.** A dedicated collaborator with its own unit-test seam.
**Rejected**: it is a third object for a four-line rule, and `WorkItemService` already takes twelve
constructor dependencies with `#pragma S107` suppressed. A thirteenth needs to earn its place, and a
rule that is already directly testable through the service does not.

**Leave it on the fetched set and accept the gap.** Not seriously considered, recorded so it is not
reopened: it silently deletes a shipped signal, and staleness is a premium-visible capability
(`time-in-state-and-staleness`) that several charts and the aging view depend on.

## Cross-reference

- [ADR-138](./adr-138-two-phase-incremental-work-tracking-sync.md) — the incremental fetch that creates
  this hazard.
- [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) D2 — the
  transport-only dispatcher and the re-sync recovery guarantee this ADR keeps true.
- [ADR-016](./adr-016-current-state-entered-at-derivation.md) — where `CurrentStateEnteredAt`, the
  timestamp staleness measures against, comes from.
- Acceptance criterion: `docs/feature/epic-5687-faster-updates/feature-delta.md` → US-02 AC-2.5.
