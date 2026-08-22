# ADR-169: A source-bound Delivery's name, date and Features refuse hand mutation, and the future-date rule leaves the constructor

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slices 01b-02)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D4 makes the remote system the single source of truth for a bound Delivery's date **and** its
membership; both render read-only, and editing by hand means unbinding first. D5 says a date arriving
from the remote is accepted whatever it says — including the past — while hand entry still rejects a
past date, and that the future-date invariant therefore "moves off the constructor onto the hand-entry
path".

Two verified facts reshape what that move actually is.

**The future-date rule is already duplicated.** It sits in `Delivery`'s constructor (`Delivery.cs:16-19`)
*and*, independently, in `DeliveriesController.CreateDelivery` (`:86-89`) and
`DeliveriesController.UpdateDelivery` (`:144-147`). Moving the invariant is therefore deleting one of
three copies, not relocating a rule that lives in one place.

**It was never an aggregate invariant.** `Delivery` has a second, parameterless constructor for EF
(`:26-29`). Every Delivery ever loaded from the database has skipped the check. A rule that every
persisted instance bypasses is input validation that happens to sit in a constructor — which is also
why the ADR-027 analysis in `brief.md` calling this "the one model with real invariant enforcement
today" overstates it.

## Decision

**The remote owns three fields; the aggregate refuses hand-writes to them; the sync path writes all
three at once and applies no date policy.**

1. **The future-date check is deleted from the `Delivery` constructor.** The two controller checks stay
   and are the hand-entry boundary, which is what they already were. They become conditional on the
   selection mode: a create or update whose mode is `SourceBound` does not run them, because the date on
   that request is not the user's.

2. **`Rename`, `Reschedule`, `ReplaceFeatures` and `ApplyRuleSet` each refuse when the Delivery is
   source-bound**, alongside the archived refusal
   [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md) gives them. D4 stops being a
   UI convention and becomes a property of the aggregate: there is no call path by which a human edit
   reaches a bound Delivery's name, date or membership.

3. **The sync path uses one mutator, not four.**
   `Delivery.ApplySourceSnapshot(DeliverySourceSnapshot snapshot)`, where the snapshot is
   `sealed record DeliverySourceSnapshot(string Name, DateTime Date, IReadOnlyList<Feature> Features)`.
   It refuses when archived, refuses when **not** source-bound, and bumps `ConcurrencyToken` once. A
   handler owns membership and date together (D1), so they move together or not at all — "the sync
   updated the date but not the membership" is a state this design cannot reach.

4. **`ApplySourceSnapshot` applies no date policy whatsoever.** That is D5: a Release six months past
   its date is a normal state, Jira renders it overdue itself, and Lighthouse agreeing with Jira is the
   entire point of the Epic. The asymmetry between the two paths lives in the **method names, at the
   call sites, where a reader sees it** — not in a flag passed to a shared method.

5. **Neither constructor re-runs anything.** The EF constructor is untouched, and no factory
   reconstitutes a `Delivery` from storage through the public constructor. This is the class of bug
   already recorded against work-item sync, where a copy-constructor silently dropped an init-only
   property.

6. **`VerifyDeliveryRequest`'s `if/else` becomes a `switch` over `DeliverySelectionMode` with a
   throwing `default`.** This closes a licence-gate hole, and the shape change is the point rather
   than a flourish.

   **The hole**: D10 makes source-bound selection Premium, and
   `DeliveriesController.VerifyDeliveryRequest` (`:285-300`) reads
   `if (request.SelectionMode == RuleBased) { premium check } else { if (CanUsePremiumFeatures())
   return null; …free-tier delivery-count limit… }`. A `SourceBound` request falls into the `else`,
   where the premium check **short-circuits to allow**. A Community instance could create a
   source-bound Delivery by posting directly, bypassing the gate the UI faithfully renders.

   **Why the shape, not just a branch.** An `else` that means "everything that is not rule-based"
   silently absorbs every future enum member and applies Manual's rules to it. That is precisely how a
   third selection mode arrived through the same door and inherited the wrong licence check. A `switch`
   with an explicit arm per member and a `default` that throws `NotSupportedException` makes the next
   member a **compile-and-test failure rather than a silent mis-gate**.

   This is not a new pattern being introduced by this Epic — it is **making one method consistent with
   its two neighbours**. The same controller already switches over `DeliverySelectionMode` with a
   throwing `default` in `CreateDelivery` (`:112-126`) and `UpdateDelivery` (`:181-195`);
   `VerifyDeliveryRequest` is the odd one out, and that is why it was the one that broke.

## Alternatives considered

- **One `Reschedule(DateTime date, bool isFromSource)`.** **Rejected** — the two behaviours differ in
  *whether they reject*, which is the definition of a distinction a boolean parameter should not carry.
  A reader at the call site would have to know what `true` means before they could tell whether a past
  date was about to be accepted.

- **Keep the constructor check and have the sync path construct through EF only.** **Rejected** — it
  makes correctness depend on which constructor a future call site happens to pick. The sync path
  updates an existing Delivery rather than constructing one, so the check would never be reached while
  remaining as a trap for the next feature that does construct one.

- **Three source mutators — `ApplySourceName`, `ApplySourceDate`, `ApplySourceFeatures`.** **Rejected**
  — three token bumps, three chances to apply two of three and leave a Delivery half-synced, and no
  gain, because nothing ever wants to apply one without the others.

- **Enforce read-only in the UI only.** **Rejected** — `DeliveryRuleService` is the standing proof of
  the failure class: a background writer with no HTTP surface already mutates `Delivery.Features`, and a
  UI rule cannot see it. This is ADR-164's argument, applied to a second reason for refusing.

## Consequences

**Positive**

- D4's conflict class is removed by construction rather than managed. There is no last-synced value to
  store, no divergence state, no "which side changed more recently", and no conflict UI.
- AC-02.4's unbind is trivially correct. `Unbind()` clears the binding; the last synced name, date and
  Features are already the entity's own fields, so "retained and editable" requires no copying and no
  second store.

**Negative / accepted**

- `UpdateDelivery` must branch on selection mode before setting `Name` and `Date`, which it currently
  does unconditionally (`:177-179`). That branch is the visible cost of point 2, and it is exactly where
  a crafter will be tempted to assign the property rather than call the aggregate — which is why
  `Rename` and `Reschedule` must exist as methods first, and why this Epic is sequenced behind ADR-164's
  encapsulation.
- Deleting a constructor check is a behaviour change for any caller that relied on it. Verified: the
  only production caller is `CreateDelivery`, which checks first anyway. **Tests asserting the
  constructor throws must be moved to the controller, not deleted** — deleting them would remove the
  hand-entry rule's only coverage while appearing to be a tidy-up.
- The aggregate now carries two orthogonal refusal reasons (archived, source-bound) on the same four
  methods. Two conditions in one guard is the ceiling; a third would mean the refusals want their own
  policy object.

**Reuse verdict**: `Delivery` → **EXTEND** (one new mutator; four existing mutators gain a second
refusal; one constructor check deleted). `DeliveriesController` → **EXTEND** (conditional date
validation, selection-mode branch in `UpdateDelivery`). `DeliverySourceSnapshot` → **CREATE NEW** — a
three-field record with no behaviour, existing so point 3's atomicity is a signature rather than a
convention a future edit can drop.

**Enforcement**

| Rule | Mechanism |
|---|---|
| The sync cannot partially apply a snapshot | Compile: `ApplySourceSnapshot` takes the whole record; there is no partial overload |
| A bound Delivery refuses hand edits from every path | NUnit: `Rename`, `Reschedule`, `ReplaceFeatures` and `ApplyRuleSet` each throw on a source-bound Delivery |
| The sync mutator cannot be used on an unbound Delivery | NUnit: `ApplySourceSnapshot` throws when `SourceBinding` is null |
| The hand-entry rule survives leaving the constructor | NUnit: `new Delivery(name, yesterday, portfolioId, today)` no longer throws, **and** `POST`/`PUT` with a hand-entered past date still return `400` |
| A remote past date is accepted end to end | Integration: a source-bound create with a past remote date succeeds and the Delivery renders overdue (AC-03.2) |
| A new mutating route is classified rather than forgotten | The reflection test ADR-164 introduces over Delivery-scoped `[HttpPost]`/`[HttpPut]`/`[HttpDelete]` actions must classify the source-bound branch, not only the archived one |
| Source-bound selection is Premium on the write path, not only in the UI | Integration: a Community-licensed `POST` with `selectionMode: SourceBound` returns `403`, and does **not** fall through to the free-tier one-delivery-per-portfolio rule |
| A future selection mode cannot inherit Manual's licence rules by falling through | NUnit parameterised over every `DeliverySelectionMode` member: each has an explicit arm in `VerifyDeliveryRequest`; a new member hits the throwing `default` and fails until classified |

Cross-refs [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md) (the mutator set
this extends, and the token-bump rule), [ADR-167](./adr-167-source-binding-as-nullable-columns-behind-a-paired-mutator.md)
(the binding whose presence triggers the refusal),
[ADR-168](./adr-168-inbound-resync-sibling-service-at-the-portfolio-updater-seam.md) (the only caller of
`ApplySourceSnapshot`).
