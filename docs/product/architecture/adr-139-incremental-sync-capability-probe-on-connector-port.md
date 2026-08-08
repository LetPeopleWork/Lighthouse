# ADR-139: Incremental sync is a per-connection capability on `IWorkTrackingConnector`, not a connector type

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `epic-5687-faster-updates` (ADO Epic #5687 "Faster Updates")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

ADR-138 makes a refresh two-phase: sweep identity plus a remote-changed timestamp, then fetch payloads
for the changed only. Both phases are connector work, so the driven port `IWorkTrackingConnector` has to
grow. Three things constrain how.

**Not every connector can sweep.** The CSV connector's "fetch" is a file the user already uploaded.
There is no remote call to save and no timestamp to compare, so it must stay on the full path
permanently.

**Jira Cloud and Jira Data Center are one class.** `JiraWorkTrackingConnector` decides deployment at
runtime (`GetDeploymentType`, `:1363`) and branches into `GetIssuesByQueryFromCloud` or
`…FromDataCenter`. Cloud is safe to sweep immediately; DC is not, until its offset pagination is shown
to return a *stable* id set — DC pagination over an unordered JQL is the documented source of duplicate
reference ids (`docs/ci-learnings.md`, 2026-05-25, and the reason `DeduplicateByReferenceId` exists).
A sweep that can *lose* an id must never drive `removed = stored − swept`, because that failure looks
like data loss rather than slowness.

**The port already has a precedent for exactly this shape.**
`bool SupportsTransitionHistory(WorkTrackingSystemConnection connection)` is a per-connection capability
probe on the main interface, consumed by `WorkItemService.WithSyncDeltaTransition` to decide whether to
synthesise a transition. It is the established idiom for "this connector, against this connection, can
or cannot do X".

## Decision

**Incremental sync is a capability the connector declares per connection, on the existing port.**

```csharp
public interface IWorkTrackingConnector
{
    bool SupportsTransitionHistory(WorkTrackingSystemConnection connection);   // existing
    bool SupportsIncrementalSync(WorkTrackingSystemConnection connection);     // new — same shape

    Task<IReadOnlyList<RemoteRecordStamp>> SweepWorkItemsForTeam(Team team);
    Task<IReadOnlyList<RemoteRecordStamp>> SweepFeaturesForPortfolio(Portfolio portfolio);

    Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team);                              // existing
    Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, IReadOnlyCollection<string> referenceIds);
    Task<List<Feature>> GetFeaturesForProject(Portfolio portfolio);                          // existing
    Task<List<Feature>> GetFeaturesForProject(Portfolio portfolio, IReadOnlyCollection<string> referenceIds);
}
```

`RemoteRecordStamp` is a `sealed record (string ReferenceId, DateTime ChangedAt)` — no behaviour.

**Phase 2 names behaviour that already exists.** Azure DevOps has `GetAdoWorkItemsById` (`:657`) and Jira
builds a `key = "X" OR key = "Y"` query inside `GetParentFeaturesDetails` (`:150`). The by-reference-id
overloads surface those on the port instead of adding a new capability; `GetParentFeaturesDetails`
becomes a caller of the extracted query rather than its only owner.

**A connector that cannot sweep declares `false` and is never asked.** CSV returns `false`; its sweep
methods throw `NotSupportedException` as an assertion that the caller honoured the probe, not as a
control-flow path.

**The Jira probe is the slice gate.** `JiraWorkTrackingConnector.SupportsIncrementalSync` returns
`true` for a Cloud connection from the first Jira slice, and `false` for Data Center until the DC
pagination probe passes — at which point it becomes a one-line predicate change. The rollout order is
expressed as data, not as a feature branch.

**The diff stays out of the connector.** Connectors answer "what exists and when did each last change"
and "give me these ones". `WorkItemService` owns the comparison, the changed set and the removal set.

## Consequences

**Positive**

- Per-connection capability is the only form that can answer differently for Jira Cloud and Jira DC
  while they remain one class — which is the concrete requirement, not a hypothetical.
- The rollout across six connectors is a sequence of probe flips against one contract, so slices 03-08
  are transport work with no further design owed.
- The safety property (`removed = stored − swept`) is computed in exactly one place, so it can only be
  wrong once.
- The idiom is already in the codebase, so nothing new has to be learned to read it.

**Negative / accepted**

- Six implementations must exist even though one of them (CSV) only ever says "no". The alternative —
  an opt-in interface — trades that for a worse problem (below).
- Four new members on a driven port consumed by six adapters is a shared-contract change: usages get
  grepped and the connector test doubles extended before the first implementation lands.
- A connector could lie — declare `true` and return an incomplete sweep. Mitigated by the same rule the
  Jira DC gate enforces: a sweep is only trusted to drive deletion once it has been shown to enumerate
  the full set.

## Alternatives Considered

**A separate opt-in interface** — `IIncrementalWorkTrackingConnector`, with
`if (connector is IIncrementalWorkTrackingConnector inc)` at the call site. Superficially cleaner: CSV
simply does not implement it, so there is no `false` branch and no throwing member. **Rejected because a
type test cannot express per-connection variance.** Jira Cloud and Jira DC are one class and must answer
differently, so this shape would force either a Cloud/DC class split — a large refactor of an
1400-line connector, justified by nothing else — or a lie, where the type claims a capability it only
sometimes has and the real gate hides inside the sweep. The existing `SupportsTransitionHistory`
precedent exists precisely because this codebase already met this problem.

**Widen the existing fetch method** — `GetWorkItemsForTeam(Team, SyncScope scope)` where the scope
carries the stored stamps and the connector decides what to return. No new port members. **Rejected**
because it pushes the diff into every adapter: the comparison gets written five times, and the removal
set is computed inside the connector where `WorkItemService` cannot see how it was derived. The one
property that must not be got wrong would become five implementations of itself.

**One method for both phases** — `SweepAndFetchChanged(team, storedStamps)` returning the changed
payloads plus the full id set. One port call instead of two. **Rejected** for the same reason: it moves
the diff into the adapter. The round-trip saving is nil, since the two phases are two remote calls
either way.

## Cross-reference

- [ADR-138](./adr-138-two-phase-incremental-work-tracking-sync.md) — the two-phase contract this port
  serves, and why the removal rule is the binding constraint.
- [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md) — the other input to mode
  resolution.
- Extends the `SupportsTransitionHistory` idiom introduced alongside
  [ADR-015](./adr-015-work-item-state-transition-placement.md).
- Per-connector capability matrix (Jira / ADO / ServiceNow / Linear / CSV):
  `docs/feature/epic-5687-faster-updates/feature-delta.md` → Connector Capability Matrix.
