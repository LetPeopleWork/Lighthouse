# ADR-167: The source binding is nullable columns on `Delivery` written only by `BindToSource`/`Unbind` — not an owned type, not a table

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slice 01b)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

S1 is the gap this Epic exists to close: `Delivery` carries Name, Date, PortfolioId, Features,
SelectionMode, RuleDefinitionJson and RuleSchemaVersion, and **no remote identity of any kind**. There
is nothing to sync against.

Four facts constrain how the identity is stored:

- The reference must be the Jira Version **id**, never its name, so a rename in Jira leaves the binding
  resolvable (D3.3, AC-02.1, AC-02.5).
- `DeliverySelectionMode` is int-persisted with **no `HasConversion`** anywhere in
  `LighthouseAppContext` (S3). Inserting a member above the end silently repoints every stored Delivery.
- `Delivery` is read through `DeliveryRepository.GetAllDeliveriesWithIncludes`, which already includes
  five levels of navigation under a globally-configured split-query setting.
- [ADR-170](./adr-170-broken-source-as-recorded-verdict.md) needs two further facts stored on the same
  row: when the source last resolved successfully, and why it is currently unavailable.

## Decision

**Four nullable columns on `Delivery`, read through one computed value object, written through two
mutators.**

1. **The columns.**

   | Column | Type | Meaning |
   |---|---|---|
   | `DeliverySourceKey` | `string?` | The registry key, e.g. `"jira.release"` |
   | `DeliverySourceReference` | `string?` | The Jira Version **id** |
   | `SourceLastSyncedOn` | `DateOnly?` | The day the source last resolved (ADR-170) |
   | `SourceUnavailableReason` | `DeliverySourceUnavailableReason?` | Why it is broken (ADR-170) |

   `SourceLastSyncedOn` is `DateOnly`, sourced from `ILighthouseClock.Today`, because it is a **day a
   reader is shown** — "showing last synced values from 2026-08-20". This follows
   [ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md)'s `ArchivedOn` rather than
   [ADR-165](./adr-165-delivery-note-authorship-and-the-absent-profile.md)'s note instants. Both
   precedents exist and the choice is deliberate: a `DateTime` column is in reach of the global
   `Properties<DateTime>()` UTC converter, which shifts a local-kind midnight onto the previous day on
   write.

2. **The pair is read directly, behind `bool IsSourceBound => DeliverySourceKey is not null`.**

   > **Revised 2026-08-22 (review finding).** An earlier draft introduced a computed
   > `DeliverySourceBinding(Key, Reference)` record "so the half-bound state is unrepresentable". It is
   > withdrawn: point 4's single asserting mutator already makes that state unreachable, so the record
   > added a type without adding a guarantee. See Alternatives.

3. **`Unbind()` clears all four columns, not just the binding pair.** Ownership stated once, because an
   earlier draft split it and left a hole: ADR-170 says the sync service is the only writer of
   `SourceLastSyncedOn` and `SourceUnavailableReason`, which is true **only while the Delivery is
   bound**. Without this, AC-04.3's whole point — a *broken* Delivery unbound back to Manual — would
   leave `SourceUnavailableReason` set on a Manual Delivery, and the banner is keyed on the reason.

4. **The columns are written only by `Delivery.BindToSource(key, reference)` and `Delivery.Unbind()`.**
   Both bump `ConcurrencyToken`, per [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md)'s
   rule that a mutation which does not bump is not protected at all. `BindToSource` asserts both
   arguments are non-empty — the single construction site the pair's invariant rests on, the same shape
   [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) gives `RecordableDeliveries`.

5. **`DeliverySelectionMode` gains `SourceBound = 2`, appended.** One member, not one per source: the
   handler is a column, so the enum does not grow with concepts. The append-only rule (S3) is the same
   one already pinned on `WorkTrackingSystems`.

6. **The migration is additive and expand-only**, generated with the `CreateMigration` PowerShell script
   across all providers — never `dotnet ef migrations add`. The migration DLLs are `HintPath`
   references and must be built before the tooling runs, or it reports pending model changes that are
   not real.

## Alternatives considered

- **An EF owned type**, `OwnsOne(d => d.SourceBinding)`, mapping to the same table. This is the shape
  that buys the invariant in the mapping rather than in a computed property. **Rejected** — there is no
  verified *optional* owned-type precedent in this codebase.
  [ADR-064](./adr-064-cycle-time-definitions-storage-as-owned-collection-on-settings-aggregate.md) is an
  owned **collection**, which is a different mapping, and an optional owned entity whose properties are
  all null is the EF shape most likely to surprise. The invariant it would buy is bought instead by
  points 2 and 3 at no mapping risk. **If a crafter finds an existing optional `OwnsOne` in
  `LighthouseAppContext`, this decision should be revisited — it would then be the better shape.**

- **A separate one-row-per-Delivery table**, mirroring ADR-160's `DeliveryClosureRecord`. **Rejected** —
  ADR-160's reasoning does not transfer. A closure record mirrors around ten value columns written once,
  at archive, with their own lifecycle. A binding is four scalars rewritten on every refresh, and a
  table adds a join and a cascade FK to a read that already includes five levels.

- **Store the Version name rather than its id.** **Rejected** outright by AC-02.1 and AC-02.5. Named
  here because it is the cheap mistake: the name is what the picker shows, what the Delivery displays,
  and what a developer reaches for.

- **One column holding `"jira.release:10042"`.** **Rejected** — it makes the registry lookup a string
  split, and it turns a handler key containing a colon into a data-corruption bug.

- **No `DeliverySourceBinding` record at all — just the columns plus
  `bool IsSourceBound => DeliverySourceKey is not null`.** Raised at review and **it is the better
  answer for point 2.** Point 4's single asserting mutator already makes the half-bound state
  unreachable; a computed record over two columns that only `BindToSource` can write adds a type
  without adding a guarantee. **Point 2 is therefore withdrawn**: the value object is dropped, the two
  columns are read directly, and `IsSourceBound` is the predicate. Kept in this list rather than
  deleted silently, because the original argument for the record — "it makes the half-bound state
  unrepresentable" — is the kind of reasoning that sounds structural and is merely duplicative.

## Consequences

**Positive**

- No join, no cascade FK, no second table, no change to `GetAllDeliveriesWithIncludes`.
- The wire contract grows by additive nullable fields on `DeliveryWithLikelihoodDto`. A stale frontend
  reads them as absent and renders today's behaviour, so the deployment is backwards-compatible by
  construction.
- **`IDeliveryRepository` gains no method.** This is deliberate: that file is being edited concurrently
  by #5698 phase 4, and this Epic taking no dependency on it removes the parallel-edit collision
  entirely.

**Negative / accepted**

- The columns permit `key set, reference null`. Unreachable through the mutator and never read except
  through `SourceBinding`, but a hand-written SQL repair could produce it. Accepted; the alternative is
  a table.
- `Delivery` grows four columns, on top of ADR-160's `ArchivedOn`. It is becoming a wide row. The
  trigger to revisit is a fifth source-related column, which would mean the binding has acquired a
  lifecycle of its own and earned its table.

**Reuse verdict**: `Delivery` → **EXTEND** (four columns, two mutators, one computed property).
`DeliverySelectionMode` → **EXTEND** (one appended member). `UpdateDeliveryRequest` → **EXTEND** (two
nullable fields; `Name`, `Date` and `FeatureIds` keep their `[JsonRequired]`, so a source-bound payload
still sends them and the server ignores them). `DeliveryWithLikelihoodDto` → **EXTEND** (additive
nullable fields). `LighthouseAppContext` → **EXTEND**. `IDeliveryRepository` / `DeliveryRepository` →
**UNCHANGED**. `DeliverySourceBinding` → **CREATE NEW** — a two-field record with no behaviour, existing
so the half-bound state is unrepresentable at every read.

**Enforcement**

| Rule | Mechanism |
|---|---|
| A Delivery is source-bound iff it has a binding | NUnit over a real provider round-trip: `SourceBinding is not null` ⟺ `SelectionMode == SourceBound` |
| The enum's ordinals never move | NUnit reflection pinning `Manual == 0`, `RuleBased == 1`, `SourceBound == 2`, modelled on `ServiceNowConnectionConfigurationTest` |
| The migration applies on a real provider | Migration test on Sqlite and Postgres — InMemory skips migrations |
| Binding and unbinding are protected against a concurrent edit | NUnit: `BindToSource` and `Unbind` each change `ConcurrencyToken`. Without this assertion, optimistic concurrency on this aggregate is decorative (ADR-164) |
| The stored reference survives a remote rename | Integration: bind, rename the source remotely, refresh, assert the binding still resolves and only the displayed name changed (AC-02.5, AC-03.8) |

Cross-refs [ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md) (the `DateOnly`
reasoning and the table this deliberately is not),
[ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md) (the token-bump rule every
mutator obeys), [ADR-170](./adr-170-broken-source-as-recorded-verdict.md) (the two state columns),
[ADR-169](./adr-169-remote-owned-fields-refuse-hand-mutation.md) (what the binding's presence forbids).
