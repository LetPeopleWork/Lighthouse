# ADR-164: "An archived Delivery refuses writes" is an aggregate invariant surfaced as 409, not a check on each endpoint

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 05)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

AC-05.5 and AC-05.8 require that an archived Delivery refuses edits — its name, date, Feature
selection, rules and notes are frozen — and DISCUSS was explicit that the rule must not be bypassable
"by a different endpoint". D6 freezes notes on archive by the same logic.

Two facts make an endpoint-by-endpoint check the wrong shape:

- **`Delivery.Features` is written from three places**, one of which
  (`DeliveryRuleService.RecomputeRuleBasedDeliveries`, reached from `PortfolioUpdater`) is a
  background service with no HTTP surface at all. A controller-level check cannot see it.
- **This epic adds six more `{deliveryId}`-rooted write endpoints** (four for notes, archive,
  un-archive). Every one is another place to remember.

Two writes must remain *allowed* on an archived Delivery: `DELETE` (AC-04.9 — archive and delete both
stay) and un-archive itself.

## Decision

**The aggregate refuses. Controllers translate the refusal.**

Four points:

1. **Mutation goes through methods, not setters.** `Delivery` exposes `Rename`, `Reschedule`,
   `ReplaceFeatures`, `ApplyRuleSet`; `Features` becomes `IReadOnlyList<Feature>`. Each refuses when
   `ArchivedOn is not null` by throwing `DeliveryArchivedException`. `Archive` and `Unarchive` are
   themselves methods on the aggregate, so the state transition and its guard sit together.

2. **Notes are refused by the same invariant.** `Delivery.AddNote` / the note's own `Edit` and
   `Withdraw` consult the owning Delivery's `ArchivedOn`, so D6 is the same rule rather than a second
   one that happens to agree.

3. **One translation point.** An exception filter maps `DeliveryArchivedException` to **409 Conflict**
   with a machine-readable reason, so no controller action carries the check and every action gets it.
   409 rather than 403 because the refusal is about the resource's state, not the caller's rights —
   the same caller succeeds after un-archiving.

4. **Delete and un-archive are exempt by construction**, because they are not among the guarded
   mutators. The exemption is the absence of a guard on two named methods, not a special case inside
   a shared check.

## Alternatives considered

- **A check at the top of each controller action.** The obvious move and the smallest first diff.
  **Rejected** — it cannot cover `DeliveryRuleService`, so AC-04.7 and AC-05.8 would be enforced by
  two different mechanisms with different coverage, and the six new endpoints each become a place to
  forget. DISCUSS's "cannot be bypassed by a different endpoint" is precisely the requirement this
  option fails.

- **An action-filter attribute, `[RefuseWhenDeliveryArchived]`.** Declarative, and pinnable by the
  reflection-over-routes test the codebase already uses in `Lighthouse.Backend.Tests/API/Security/`.
  **Rejected as the enforcement**, kept as inspiration for the test: an attribute still does not see
  the background write path, and it costs an extra load of the Delivery per request. The reflection
  test it suggested is adopted below as the *structural* layer.

- **A soft-delete-style interceptor in `SaveChanges`** refusing to persist changes to an archived
  Delivery. **Rejected** — it refuses far too late to give a useful message, it would have to special-
  case the un-archive and archive writes themselves, and a `SaveChanges` interceptor that throws is a
  cross-cutting hazard for every unrelated save in the same unit of work.

## Concurrency — the stale-aggregate race

The guard reads `ArchivedOn` from the **in-memory** aggregate. A caller that loaded a Delivery
*before* it was archived therefore holds a stale `null`, and its `ReplaceFeatures` would be permitted
by an otherwise-correct guard. This is not hypothetical: the realistic interleaving is a Portfolio
refresh already in flight — `PortfolioUpdater` loads the Deliveries, a user archives one, and
`DeliveryRuleService.RecomputeRuleBasedDeliveries` then mutates the copy it already holds.

`Delivery` carries an optimistic-concurrency token (it is one of the config aggregate roots that
does), so the archive write changes the token and the stale `SaveChanges` fails. **That is only a
guarantee if the failure is allowed to surface.** The blanket reload-retry in the save path must not
quietly reload the Delivery and replay the Feature mutation onto the now-archived row — replaying is
precisely the corruption this invariant exists to prevent.

Two rules follow, and both are part of this decision:

- **The archive and un-archive writes bump the concurrency token**, so any in-flight holder of the
  aggregate loses its save.
- **The reload-retry path must re-evaluate `ArchivedOn` after reloading and drop the mutation rather
  than replay it.** A background recompute that loses this race is a no-op, not a retry — the
  Delivery it was recomputing no longer wants recomputing.

Verified by an integration test that interleaves the two explicitly: load a Delivery for recompute,
archive it, then attempt the recompute save, and assert the Feature set is unchanged and no exception
escapes to the background service's caller.

## Consequences

- **Positive**: one invariant, one place, covering HTTP and background callers alike. A seventh write
  endpoint added next year inherits it without anyone reading this ADR.
- **Positive**: the guard is a compile-time-shaped restriction as much as a runtime one — with
  `Features` read-only and setters replaced by methods, the common bypass (assigning a property) does
  not compile.
- **Negative**: encapsulating `Delivery` is a wider diff than a controller `if`. It touches three
  Feature write sites, the property setters used by `UpdateDelivery`, and their tests. The compiler
  finds all of them, and the epic already had to modify `UpdateDelivery` for AC-05.8.
- **Negative**: an exception used for expected control flow. Contained by being one exception type
  with one filter, and it is the mechanism the codebase already uses for optimistic-concurrency 409s.
- **Enforcement — three layers, each answering a different question**:
  1. *Compile*: `IReadOnlyList<Feature>` plus method-only mutation — a direct write does not build.
  2. *Structural*: an NUnit reflection test over every `[HttpPost]`/`[HttpPut]`/`[HttpDelete]` action
     on the Delivery-scoped controllers, asserting each is either in the archived-refusal integration
     matrix or on an explicit exemption list (`DeleteDelivery`, `Unarchive`). A new endpoint fails the
     test until it is classified. Modelled on the existing
     `AppSettingsControllerTest.EverySettingsRouteExceptTheOrderingPolicyReadRequiresSystemAdmin` and
     `API/Security/S4_DeliveriesDeleteGuardInversionTests`.
  3. *Behavioural*: integration tests hitting every mutating endpoint against an archived Delivery and
     asserting 409, plus one asserting a Portfolio refresh leaves its Features untouched.
- **Reuse verdict**: `Delivery` → **EXTEND** (encapsulate mutation). `DeliveriesController` →
  **EXTEND** (call the new methods; no per-action check added). `DeliveryRuleService` → **UNCHANGED**.
  Exception-to-status filter → **EXTEND** the existing mapping. No new component.
- Cross-refs [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) (the same aggregate
  guard covering the recorder and rule re-matching),
  [ADR-165](./adr-165-delivery-note-authorship-and-the-absent-profile.md) (the note-level rule this
  composes with),
  [ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md) (`ArchivedOn`, the column
  every guard reads).
