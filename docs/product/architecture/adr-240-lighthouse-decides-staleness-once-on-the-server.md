# ADR-240: Lighthouse decides staleness once, on the server, and says why

**Status**: Proposed (DESIGN, 2026-10-16; taken in AFK mode on the architect's recommendation — maintainer to confirm)
**Feature**: `story-6247-server-marks-stale` (ADO User Story #6247)
**Decider**: Morgan (Solution Architect)
**Supersedes**: ADR-026's placement of the staleness rule in the browser. **Amends**: ADR-070 (the blocked-duration
trigger keeps its semantics and moves with the rule). ADR-026's DDD-8 (a threshold edit takes effect without a
re-sync) still holds: the rule runs when a read is served, not when items are synced.

---

## Context

Staleness is decided in two places that disagree:

- The browser (`deriveStaleness.ts`): Blocked takes precedence, a blocked-duration trigger (`≥`), a threshold of `0`
  turns its trigger off, whole days.
- The sync pipeline (`WorkItemService.IsStale`): no Blocked precedence, no blocked-duration trigger, `0` not treated
  as off, fractional days from `DateTime.UtcNow`. It feeds `WorkItemBecameStale`, which nothing in production
  subscribes to.

`lh`, the MCP tools and the Daily Flow Review skill cannot see either. Copying the rule into the clients would make a
third.

## Decision

1. **One pure domain policy**, `Models/Staleness/StalenessPolicy`, built from the owner's two thresholds
   (`StalenessPolicy.For(WorkTrackingSystemOptionsOwner)`). `Evaluate(candidate, zone, asOfDay)` returns a
   `StalenessVerdict(IsStale, Reasons)`. The candidate carries state category, state name, entered-at, is-blocked and
   blocked-since. The policy takes the zone and the day as values and never depends on `ILighthouseClock`, like the
   other Models helpers.
2. **The semantics are the browser's**: Doing only; Blocked takes precedence; time in state stale when days `>`
   threshold and not Blocked; blocked duration stale when blocked days `≥` blocked threshold; `0` turns a trigger off;
   a Blocked item past the time-in-state threshold carries a `TimeInStateWhileBlocked` reason that does not make it
   stale.
3. **Days are counted the way Work Item Age counts them**: the entered day in the instance's zone
   (`InstanceCalendar.DayOf`), `asOfDay − enteredDay + 1`, at least 1. The as-of day is the read's `asOfDate`.
4. **Two reads carry the verdict**: `GET teams/{id}/metrics/wip` (Team thresholds) and
   `GET portfolios/{id}/metrics/wip` (Portfolio thresholds, Features). They evaluate the DTO's own resolved fields,
   after the as-of projection, so the badge and the verdict read the same values. Every Stale surface in the UI
   reads these two.
5. **Wire**: `isStale: bool?` and `staleReasons: [{ kind, days, stateName? }]?`, both omitted when null, so a read
   that does not evaluate staleness (or an older server) says nothing rather than "not stale". `kind` is
   `StaleReasonKind { TimeInState, BlockedDuration, TimeInStateWhileBlocked }`, serialised as its name by the global
   string-enum converter and mirrored in TypeScript as a string union.
6. **The sync-time event uses the same policy**, with `ILighthouseClock.Today` and the blocked decision from
   `IBlockedItemService`. `WorkItemBecameStale` and `WasStaleAtLastSync` stay. Removing the code would leave a dead
   column, and dropping the column is not expand-only.
7. **The frontend and the clients never decide staleness.** The browser's rule is deleted; the clients pass the
   fields through and fall back on their absence.

## Alternatives

- **A method on `WorkItemBase`.** Rejected: the Portfolio read cannot project a Feature into a past state and passes
  that state separately, so an entity method would judge today's state on a past read.
- **An `IStalenessService` port.** Rejected: dependency-injection ceremony around pure arithmetic, with the same
  domain core still needed underneath.
- **A nested `staleness` object on the wire.** Rejected: the clients read the flat `isBlocked`/`blockedSince` pair,
  and the new fields sit next to it.
- **Always-present `isStale: false`.** Rejected: on reads that never evaluate, it would be a wrong answer instead of
  no answer.
- **Remove `WorkItemBecameStale`.** Rejected for now: see decision 6.

## Consequences

- One rule, covered by backend unit tests at its boundaries; the browser's copy and its tests go.
- A badge's "N days in State" label is still computed in the browser, counted to the dialog column's `now`. DELIVER
  passes the range end as that `now`, so on a past range the label and the server's reason count to the same day;
  the residual difference is one day near midnight, where the browser's zone and the instance's zone disagree.
- Past ranges now judge staleness as of the range's end on every surface, including the dialog badges and the
  aging-pace data, which measured against today before.
- The Blocked RAG and the "is a threshold set" check still read the thresholds from settings; they are not the
  staleness rule.
- A threshold edit shows on the next WIP read; the WIP query must be refetched after a settings save.
