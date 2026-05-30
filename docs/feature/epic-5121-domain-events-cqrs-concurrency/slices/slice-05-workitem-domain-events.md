# Slice 05: Emit work-item domain events on state transitions

**Feature**: epic-5121-domain-events-cqrs-concurrency
**ADO child**: #5122
**Story shipped**: US-5122 — **USER-VISIBLE** (the value is reliable event emission that unlocks proactive reactions)
**Job-id**: `job-react-proactively-to-workitem-change`
**Persona**: `flow-coach` (eventual beneficiary; reactions are downstream)
**Estimate**: ~1.5 crafter days
**ADR**: D1, D2; event family D (addendum). **GUARDRAIL: D7 — transport only, NOT Event Sourcing.**

## Goal

Off the `WorkItemService.SyncStateTransitions` sync delta, publish in-process domain events when a work item changes state, crosses its staleness threshold, or becomes blocked — `WorkItemTransitioned`, `WorkItemBecameStale`, `WorkItemBlocked`. Today these signals exist ONLY as read-time projections; emitting them as events makes proactive reactions (SignalR push, notifications #4754/#4755, webhooks) an additive "add-a-handler" step. **This slice delivers the emission + the seam contract only; the reactions are downstream and OUT of scope.**

## IN scope

- Three event records as POCO `record`s under `Models/Events/`: `WorkItemTransitioned` (from/to state), `WorkItemBecameStale` (referencing the configured threshold), `WorkItemBlocked` (blocked reason).
- Publication off the `SyncStateTransitions` sync DELTA (a genuine change), after-commit via the #5098 dispatcher onto the existing `UpdateQueueService` channel (D2).
- Edge discipline: `WorkItemBecameStale` fires on the threshold CROSSING (not every sync while it stays stale); `WorkItemBlocked` fires on the block TRANSITION (not every sync while blocked); unchanged items in a re-sync publish no event.

## OUT scope (GUARDRAIL)

- **NOT Event Sourcing (D7)**: the dispatcher is transport only — transient, in-process, recovered via next re-sync, no outbox, no persisted log. `WorkItemStateTransition` history stays a read-time projection of the EXTERNAL changelog, NOT a system of record. Do NOT generalise it.
- Any persisted event history is a SEPARATE subscriber/sink (#5017-style) with its own retention/PII rules — never the dispatcher. NOT built here.
- The reactions themselves — SignalR push, in-app/external notifications (#4754/#4755), webhooks — downstream, OUT of scope.
- Concurrency tokens (#5100), reaction migration (#5099), module rules (#5101).

## Learning hypothesis

**Confirms if it succeeds**: the sync delta is a clean source for the three events; emitting them after-commit via the #5098 seam adds no behaviour regression to sync, fires handlers reliably, survives a throwing handler (recovers on re-sync), and does NOT double-fire on unchanged items — proving the rail is trustworthy enough to build #4754/#4755 reactions on later.
**Disproves if it fails**: either (a) the sync delta does not cleanly distinguish a genuine threshold-crossing / block-transition from "still stale / still blocked", so events spam on every sync and the edge discipline cannot be met without persisting prior-state (which would drift toward the ES the guardrail forbids), or (b) after-commit emission off the sync path cannot guarantee the committed work-item fact survives a throwing handler without an outbox (reopens D2).

## Acceptance criteria

See US-5122 in `../feature-delta.md`. Slice specifics:

- A state change in the delta publishes one `WorkItemTransitioned` with correct from/to; an unchanged item publishes none.
- A staleness-threshold crossing publishes one `WorkItemBecameStale` referencing the threshold; a later sync where it remains stale publishes none.
- A block transition publishes one `WorkItemBlocked`; later syncs while still blocked publish none.
- Events publish AFTER the sync data is committed (D2); a throwing handler does not lose the committed fact and recovers on next re-sync (inherits the #5098 gold-test contract).
- Guardrail test: the dispatcher persists nothing for these events; the `WorkItemStateTransition` table is unchanged by this slice (still a projection, not a sink).

## Dependencies

**Hard**: slice 01 / #5098 (the dispatcher seam — events publish through it). Independent of #5099, #5100, #5101.

## Production data requirement

**Required (light).** On the project's own Lighthouse instance, after a real sync that moves / stales / blocks an item, confirm (via a test/diagnostic handler or log) that the corresponding event published exactly once and did not re-fire on the next unchanged sync. No user-facing surface yet (reactions are downstream).

## Carpaccio taste tests

- **Independently shippable?** YES — reliable emission is verifiable value on its own (a test handler proves it), and it unblocks #4754/#4755.
- **One day or less?** SLIGHTLY OVER — ~1.5 days (three events + edge discipline + guardrail test). Could split into "transitioned" (1) then "stale + blocked" (0.5) if needed, but they share the same delta-derivation plumbing so shipping together is cleaner; flagged, not forced.
- **End-to-end?** YES — sync delta → publish → handler fires (test handler), with throwing-handler recovery.
- **`@infrastructure`?** NO — user-visible value (proactive-reaction capability for `flow-coach`), though the reactions land downstream. The "After → sees" is concrete event emission proven by a test handler; carries the epic's second user-visible value.
