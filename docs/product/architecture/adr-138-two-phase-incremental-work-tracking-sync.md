# ADR-138: A refresh sweeps every record's identity, then downloads only the ones that moved

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `epic-5687-faster-updates` (ADO Epic #5687 "Faster Updates")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

Every refresh cycle refetches the entire query result. `WorkItemService.RefreshWorkItems` asks the
connector for all work items matching `(user query) AND types AND states AND resolved-cutoff`, and
`RefreshFeatures` / `RefreshParentFeatures` do the same for a portfolio's Features and their parents.

The cost is dominated by what comes *with* each record rather than by the record itself:

- **Jira** returns the issue payload with `expand=changelog`, and any issue with more than 30 changelog
  entries triggers additional paged requests at 100 per page (`JiraWorkTrackingConnector:1120`, `:776`).
  The oldest, longest-lived issues — the ones least likely to have changed — are the most expensive to
  re-read, so the cost scales with the organisation's history rather than with its activity.
- **Azure DevOps** already runs two-phase (`QueryByWiqlAsync` returns id references, then
  `GetWorkItemsInChunks`), but then pays a `GetRevisionsAsync` round trip **per work item** to rebuild
  state transitions (`:811`).
- **ServiceNow** reads state spans per record; **Linear** requests a history connection per issue.

The epic was filed against an on-premise Jira Data Center instance with 25 years of history, where a
single cycle takes minutes. Operators compensate by widening the refresh interval, which trades data
freshness for load — a product decision being made by a cost constraint.

**The constraint that shapes the design is not the cost, it is the deletion rule.** `RefreshWorkItems`
removes any stored item the fetch did not return:

```
foreach (var itemToRemove in storedWorkItems)   // whatever the fetch did not claim
    workItemRepository.Remove(itemToRemove.Id);
```

That single rule is how an item deleted in the tracker, re-typed out of the query, or moved into an
unmapped state disappears from Lighthouse. A naive incremental query — `updated >= lastSync` — returns
only changed records, so the "not returned" set becomes "everything that did not change", and the rule
would either delete the whole backlog or have to be abandoned. Abandoning it makes every stored item
immortal.

## Decision

**Each cycle runs two phases against the same query.**

**Phase 1 — identity sweep.** Issue the *same* query the full path issues, requesting only identity and
the remote system's own last-changed timestamp: `(ReferenceId, ChangedAt)` for every record in the
result set. Modelled as `RemoteRecordStamp`.

**Phase 2 — payload for the changed only.** Compare each swept stamp against the stored
`LastChangedRemote` for that reference id, and fetch the full payload — fields, changelog, revisions,
spans, history — only for records whose timestamp differs.

**The removal rule does not change.** Because phase 1 enumerates the full result set every cycle,
`removed = stored − swept` keeps exactly the meaning it has today. No item can outlive its query, and
the safety property needs no new argument.

**Comparison is per record, never against a global watermark.** Since the sweep returns a timestamp for
every id, the test is `swept.ChangedAt != stored.LastChangedRemote`, item by item. This keeps clock skew
between Lighthouse and the tracker, the "does `lastSync` mean sweep-start or sweep-end" ambiguity, and
the "does a failed cycle advance the watermark" question entirely out of the design.

The residual — a second change landing inside the same timestamp granularity as the fetch — is bounded
by treating any record whose stamp falls inside the current sweep's uncertainty window as changed on the
following cycle too. That costs a handful of extra payload fetches; it never costs a missed change.

**Only the remote fetch is incremental.** Remaining-work rollup, feature extrapolation, the percentile
default size and forecast triggering continue to recompute on every cycle. They are functions of
wall-clock and of *other* teams' data, so "this entity's records did not move" says nothing about
whether their outputs changed.

**A mode is `full` or `delta`, never partial.** An update is full when the entity has never been swept,
when any stored record lacks a stamp, when the fetch fingerprint changed (ADR-140), when the connector
does not support incremental sync for this connection (ADR-139), or when the sweep itself failed.
Anything ambiguous resolves to full — the expensive answer is always the safe one.

## Consequences

**Positive**

- The saving lands where the cost is: payload, changelog, revisions and spans are paid for on change,
  not on existence. A low-churn entity's cycle collapses to one cheap scan.
- Correctness needs no new reasoning. The one property that could cause data loss — removal — is
  computed from the same full id set it is computed from today.
- Every connector can express the contract (ADR-139's matrix), so this is one design rather than five.
- The refresh interval becomes a freshness decision again.

**Negative / accepted**

- Two round trips per cycle instead of one for entities that have not changed at all. At the observed
  scale this is far cheaper than the payload it replaces, but it is not free, and an instance with 100%
  churn gets nothing from this ADR and pays the sweep on top.
- `WorkItemService` grows a second path. Mitigated by keeping the diff, the removal set and the event
  collection in one method (see the Reuse Analysis in the feature delta) rather than extracting an
  orchestrator that would duplicate them.
- The stored stamp is a new piece of sync-owned state that can be lost — and losing it degrades delta to
  "always full" *silently*, with every other test green. This is why the stamp surviving the copy path
  is its own acceptance criterion.

**Neutral**

- No new substrate, package or external dependency. The saving comes from not asking, not from
  remembering.
- Multi-replica behaviour is untouched: ADR-076's per-entity advisory lock is an *admission* boundary,
  and this ADR operates inside a single admitted execution. INV-1..4 are unaffected.

## Alternatives Considered

**Incremental query plus a periodic full reconcile.** Make the query `updated >= lastSync` and run a full
sweep every Nth cycle (or nightly) to catch removals. Rejected: it turns correctness into a scheduling
parameter. An item that leaves the query lingers for up to N cycles, and every forecast built on it is
wrong for that window — invisibly, because the sync reports success. The reconcile interval then becomes
a knob nobody can set correctly: short enough to be safe defeats the saving, long enough to be cheap is
unsafe. It would win only if the identity sweep turned out to cost nearly as much as the full fetch,
which is the hypothesis the first Jira slice is written to test.

**Incremental query plus a manual "Full refresh" action.** Rejected: it makes correctness a user chore,
and the button's existence advertises that the automatic path is not trusted. It also fails in the common
case, because nobody clicks a button for a problem they cannot see.

**A global watermark per entity.** The conventional shape — store `lastSweptAt`, ask the tracker for
everything newer. Rejected in favour of per-record comparison, which costs nothing extra given that the
sweep already returns a stamp per record, and which removes clock skew, watermark semantics and the
failed-cycle question from the design rather than mitigating them.

**Push / webhooks from the tracker.** Out of scope: a different epic and a different trust model. Nothing
in this ADR forecloses it — a webhook would become another trigger into the same two-phase execution.

## Cross-reference

- [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md) — where the sweep lives on
  the connector port, and how per-connection capability is expressed.
- [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md) — what makes a cycle resolve to
  `full` after a configuration change.
- [ADR-141](./adr-141-time-driven-derivations-over-the-stored-set.md) — the staleness trap this ADR
  creates and how it is closed.
- AMENDS nothing in [ADR-076](./adr-076-cluster-aware-update-queue.md): admission is untouched.
- Realises [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) D2 — the
  re-sync still re-derives every signal, it merely stops re-downloading what it already holds.
- Full analysis: `docs/feature/epic-5687-faster-updates/feature-delta.md`.
