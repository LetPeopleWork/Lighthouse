# Slice 04 — Time-in-state on ServiceNow work (CONDITIONAL)

**Status**: **conditional** — build only if SPIKE Q6 finds a transition-history source that is
readable with a non-admin role, at acceptable cost, without instance-side configuration by the
customer. Slice 02 already ships the honest downgrade (D6), so "no" here is cheap and visible.

> **Scope grew 2026-07-29 (ADR-117).** Q6 answered: `metric_instance` is the source, and it is 403 for
> every read-only role — so the "readable with a non-admin role" condition above is **not** met at
> read-only grade, only at `itil`. Slice 02 therefore ships request-to-resolution measured from
> `opened_at`, and **true time-in-Doing is now this slice's job**, not just cumulative state time and
> staleness. That makes slice 04 the one that turns an inflated span into an accurate one, which is a
> stronger reason to build it than the original brief assumed — and it carries a role-escalation
> adoption cost that belongs in the build/cancel decision.

**Goal**: Cumulative State Time, per-state percentiles and staleness work on ServiceNow teams.

**Stories**: US-04 (value).

## IN scope
- Reading state transitions from whichever source Q6 identifies.
- Mapping through the existing `WorkItemStateTransitionMapper` — the same path every other system uses.
- `SupportsTransitionHistory => true` for configurations that support it.
- **Runtime** downgrade when history turns out to be unreadable for a given instance (the Linear `DowngradeHistorySupport()` precedent) — a per-instance answer, not a per-system one.
- Refresh-duration measurement; opt-in team setting if the cost is material (AC5).

## OUT of scope
- Blocked rules, wait states and named cycle times on ServiceNow — they build on this, but each is its own downstream feature.
- Backfilling history for items synced before this slice.

## Learning hypothesis
**Disproves** "ServiceNow state history is affordably readable by a normal integration account"
**if** the only viable source needs an elevated role, needs the customer to configure Metric
Definitions on their instance (which crosses this epic's no-instance-side-setup line), or costs one
call per work item at a volume that breaks refresh.
**Confirms** that ServiceNow teams reach full flow-diagnosis parity, not just throughput parity.

## Acceptance criteria
See US-04 AC1–AC5 in `feature-delta.md`.

## Dependencies
- Slice 02.
- **SPIKE Q6** — hard gate. Note the coupling flagged in Q4: if there is no native *started*
  timestamp, this slice becomes a **prerequisite of slice 02**, not a successor. Re-plan if that fires.

## Effort / reference class
≤1 day if the source is a single queryable table; **>1 day and needs re-slicing** if it is per-item.
Reference class: Linear's `HistoryConnectionFragment` + `MapSyncedTransitions` — history fetched
alongside items and mapped through the shared mapper.

## Pre-slice SPIKE
**Mandatory** — Q6, with a measured cost for ~500 items, not an estimate.

## Dogfood moment
Same day: move items through states in the dev instance, refresh, and confirm Cumulative State Time
shows the real durations — then revoke the history-source right and confirm the runtime downgrade
(AC4) rather than an error.
