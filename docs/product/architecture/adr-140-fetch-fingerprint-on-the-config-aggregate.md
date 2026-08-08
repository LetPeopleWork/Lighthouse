# ADR-140: A fetch fingerprint on the config aggregate decides when a cycle must be full

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `epic-5687-faster-updates` (ADO Epic #5687 "Faster Updates")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

Once a refresh is incremental (ADR-138), a settings change becomes a correctness problem rather than a
convenience one. Widening a Team's query, adding a work item type, or including another state changes
*which records the query returns* — but the stored stamps are unchanged, so a delta cycle would compare
against the old result set and serve the previous data indefinitely, with no error anywhere.

The inverse matters too, and the epic author flagged it before any code existed: *"if just add wait
states, that doesn't mean an update is needed from the source systems"*. Wait states, blocked rules,
staleness thresholds, named cycle times, ordering policy and terminology all change what Lighthouse
*makes of* data it already holds. Refetching a 25-year Jira history because someone marked a state as a
wait state is exactly the waste this epic exists to remove.

Two facts about the codebase constrain where the answer can live.

**A settings save does not fetch today.** `TriggerUpdate` is called only from the manual controller
endpoints and from the periodic loop (`UpdateServiceBase.UpdateAll`). So this decision is about what
*mode* the next scheduled cycle runs in, not about triggering an immediate refresh.

**`Team` and `Portfolio` are optimistic-concurrency-tokened config aggregates.** Both extend
`WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner, IConcurrencyTokenEntity`. The first instinct is
that machine-owned sync state must therefore live off them, to avoid the background sync causing a 409
for an administrator mid-edit. That instinct is wrong here, and checking it is what settles this ADR:

- `RegenerateConcurrencyTokens` rotates the token **only** for `EntityState.Added`
  (`LighthouseAppContext.cs:572-579`).
- The human edit path rotates it explicitly via `ApplyConcurrencyTokenForEdit` (`:551`).
- A background write does neither, so it cannot invalidate an admin's in-flight token.
- **`UpdateTime` is already a sync-owned field on that same tokened aggregate**
  (`WorkTrackingSystemOptionsOwner.cs:16`, written by `RefreshUpdateTime()` at `:136`). The precedent
  exists, is years old, and is safe.

## Decision

**Store a fetch fingerprint on `WorkTrackingSystemOptionsOwner`, beside `UpdateTime`.**

The fingerprint is a hash over exactly the properties that shape the remote query:

| Property | Why it shapes the fetch |
|---|---|
| `DataRetrievalValue` | the query text itself |
| `WorkItemTypes` | `PrepareQuery` argument |
| `AllStates` | `PrepareQuery` argument |
| `DoneItemsCutoffDays` | `PrepareQuery` argument |
| Additional field definitions | change the requested field set |
| Parent-override field | changes the parent resolution query |
| `WorkTrackingSystemConnectionId` | changes which system is asked |

Collections hash order-insensitively, so re-saving the same states in a different sequence is not a
change.

**A mismatch makes the next cycle `full`, and the summary log line names configuration as the reason.**
Everything outside the set leaves the fingerprint untouched and provokes no remote fetch at all.

**Computation is a pure static function**, `FetchFingerprint.For(IWorkItemQueryOwner)`. It is a total
function of data already in hand, and `WorkItemService` already carries twelve constructor dependencies
with `#pragma S107` suppressed — a thirteenth for a pure function would not earn its place.

**Completeness is enforced by a reflection test.** A test enumerates the properties reachable from
`PrepareQuery` and the connector call sites and fails when one is neither in the fingerprint nor on an
explicit, commented exclusion list. ArchUnitNET constrains types and dependencies, not property
membership, so this is a reflection assertion rather than an architecture rule.

**An instance upgrading into the feature has no stored fingerprint**, so its first cycle is full —
consistent with ADR-138's rule that ambiguity always resolves to the expensive answer.

## Consequences

**Positive**

- The administrator never has to know which settings are expensive; the system classifies the edit.
- The waste the epic names by name disappears: a wait-state or blocked-rule edit costs nothing.
- A query edit demonstrably takes effect, and the log says so — which is the difference between trusting
  the feature and hoping.
- One column on a shared base class, inherited by both Team and Portfolio; one migration, no join.

**Negative / accepted**

- The fingerprint is a duplicate encoding of "what shapes the query". If someone adds a connector option
  and forgets it, delta serves stale data with a green test suite. The reflection test is the entire
  mitigation and is therefore an acceptance criterion, not a nice-to-have.
- A sync write to `Team` participates in the EF concurrency check as the *victim*: if an admin edit
  commits between the sync's read and its write, the sync's UPDATE fails. This is pre-existing
  behaviour that `UpdateTime` already carries, and the recovery is the next cycle — but the fingerprint
  write makes it very slightly more frequent.
- The fingerprint is a boolean answer to a nuanced question. Narrowing a state list could in principle
  be served incrementally; it is not, and that is deliberate.

**Neutral**

- No change to the human edit path, the 409 semantics, or `SaveWithRetry`'s scoping.

## Alternatives Considered

**Any settings save invalidates.** Simple and always safe. **Rejected**: it wastes the win on precisely
the case the epic calls out, and it is self-defeating — an admin who learns that every save is expensive
stops tuning, which is worse than the original problem. It remains the honest fallback if the property
set proves un-enumerable, in which case the epic loses one promise rather than shipping silent staleness.

**Explicit admin choice on save** — a "refetch everything? [Y/N]" prompt. **Rejected**: it puts a
correctness decision in front of someone with no way to answer it. The right answer is derivable from
the diff; asking is an abdication dressed as control. It would only make sense if fetch-shaping-ness
were genuinely ambiguous — and if it were, the reflection test could not exist.

**Per-field granularity** — "only the states changed, so pull only the newly included states".
**Rejected** before it reached the option table: it multiplies the correctness surface by the number of
fetch-shaping fields for a saving that only appears on a rare edit.

**A side table** — `EntityFetchState(EntityType, EntityId, Fingerprint, LastSweptAt)`, keeping
machine-owned state physically off the human-edited aggregate. Genuinely cleaner on paper, and it would
survive a future tightening of the concurrency rules. **Rejected** because `UpdateTime` already proves
the problem it solves does not exist here, and it costs a second repository, a join, and an
orphan-cleanup path on entity deletion. Revisit if sync-owned state on these aggregates ever grows past
two fields or if the token rotation rule changes.

## Cross-reference

- [ADR-138](./adr-138-two-phase-incremental-work-tracking-sync.md) — mode resolution, of which the
  fingerprint is one input.
- [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md) — the other input.
- [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) — the config
  aggregate / sync entity split this ADR respects; the fingerprint is sync-owned state, and it is placed
  on a config aggregate only because that aggregate already hosts sync-owned state safely.
- Full analysis: `docs/feature/epic-5687-faster-updates/feature-delta.md`.
