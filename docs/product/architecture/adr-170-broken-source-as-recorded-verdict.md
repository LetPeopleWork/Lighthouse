# ADR-170: A broken source is a verdict recorded by the sync pass from a total result type, not a staleness derived at read time

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slice 03)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D6 keeps a Delivery whose bound Release has vanished, freezes its last known values, and flags the
binding as broken — never auto-unbinding, never deleting, because #5698 established that a Delivery is a
durable record.

**AC-04.5 is the binding requirement**: a transient read failure must **not** raise the broken-source
state; only a resolved "this Release does not exist" does. Getting this wrong makes every network blip
look like a deleted Release, which is precisely the alarm fatigue `config-admin` is documented as
caring about.

That single criterion rules out deriving the state at read time. Stored state alone cannot tell "the
read failed" from "the read succeeded and returned nothing" — by the time anything reads the Delivery,
both look like "not synced lately". Only the pass that made the remote call knows which happened, so
the verdict has to be recorded by that pass.

D12 adds a second resolved-but-broken case: a Release that still exists but whose `releaseDate` was
cleared in Jira. Its behaviour is identical to a deletion — freeze, flag, offer Unbind — but its message
must differ, because "this Release no longer has a date" sends the reader somewhere entirely different
from "this Release is gone". D11 makes this likely rather than exotic: two of the three Releases on the
demo instance carry no date, and D11's remedy is to tell users to go and set one in Jira, so the feature
actively teaches the gesture that produces this state.

## Decision

**A closed result type from the handler; two columns written only by the sync service.**

1. **`IDeliverySourceHandler.Resolve` returns a total result type** — never a nullable, never an
   exception for an expected case. `DeliverySourceResolution` is a closed set of four:

   - `Resolved(DeliverySourceSnapshot snapshot)` — name, date and Features.
   - `NotFound` — the remote answered, and the source is not there.
   - `NoDate` — the source is there and carries no date (D12).
   - `Unavailable(string reason)` — the read did not complete.

   **`Unavailable` being a member of the result rather than a thrown exception is the whole design.** It
   makes "a network blip raised the broken-source flag" non-representable rather than tested-around: the
   sync service switches over a closed set, and the transient arm is a named case a reader cannot skip
   past.

2. **Two columns carry the state**, added by [ADR-167](./adr-167-source-binding-as-nullable-columns-behind-a-paired-mutator.md)'s
   migration: `SourceLastSyncedOn` (`DateOnly?`) and `SourceUnavailableReason` (nullable enum). The
   reason has three members — `SourceNotFound`, `SourceHasNoDate`, `CapabilityWithdrawn` — int-persisted
   and **append-only**, carrying the same warning `WorkTrackingSystems` and `DeliverySelectionMode`
   carry, because EF stores it as an int with no `HasConversion` and inserting a member above the end
   silently repoints every stored row.

3. **The transition table is exhaustive. `DeliverySourceSyncService` is its only writer while the
   Delivery is bound, and `Unbind()` clears all four columns (ADR-167 point 3):**

   | Resolution | `SourceLastSyncedOn` | `SourceUnavailableReason` | Name / date / Features |
   |---|---|---|---|
   | `Resolved` | set to today | cleared | applied |
   | `NotFound` | unchanged | `SourceNotFound` | frozen |
   | `NoDate` | unchanged | `SourceHasNoDate` | frozen |
   | `Unavailable` | unchanged | **unchanged** | frozen |

   The `Unavailable` row is AC-04.5 and AC-03.6 in a single line. A Delivery already flagged stays
   flagged; one that is not does not become flagged.

4. **`CapabilityWithdrawn` is decided by the registry, not by a handler** (AC-04.4). If
   `registry.Find(connection, delivery.SourceBinding.Key)` returns nothing — the credential was
   downgraded, the connection repointed, the handler removed — the sync service records
   `CapabilityWithdrawn` **without making any remote call**. It does not error and it does not silently
   unbind.

5. **The message is chosen on the frontend from the reason, never stored.** Storing prose would put
   user-facing copy in the database and defeat the configurable Terminology rule.

6. **One resolver class, two call sites — create and re-sync.**

   > **Revised 2026-08-22 (maintainer ruling, Fork 2).** An earlier draft said the sync service was
   > "the only writer". That phrasing is what made create-time resolution look like an unresolved fork:
   > US-02 requires the grid to show the Release's name and date the moment it is saved, but the sync
   > service is slice 02 and hard-blocked, so the create path appeared to have nowhere to get them.
   > The claim is restated: `IDeliverySourceResolver` is the **only *implementation*** of the switch
   > below, and it has **two call sites**. That is not duplication — the four arms live in exactly one
   > class, and both callers depend on it.

   `IDeliverySourceResolver.Resolve(Portfolio, sourceKey, IReadOnlyList<string> references)` wraps
   [ADR-171](./adr-171-release-membership-by-jql-reference-ids.md)'s `ResolveMany`, intersects the
   returned reference ids with `portfolio.Features`, and yields a `DeliverySourceResolution` per
   reference. `DeliveriesController` calls it with one reference; `DeliverySourceSyncService` calls it
   with the pass's whole set.

   **At create, only `Resolved` is a success.** The other three arms are refusals, not states — you
   cannot *bind* to something that is not there, and creating a Delivery that is broken on arrival
   would be a worse outcome than a failed save:

   | Resolution at create | Response |
   |---|---|
   | `Resolved`, and bindable | `200` — the Delivery is created with the resolved name, date and Features, and `SourceLastSyncedOn` set to today |
   | `Resolved`, but **not bindable** | `400` — the source resolved fine but the picker would not have offered it (retired at source). See below |
   | `NotFound` | `400` — the chosen Release does not exist |
   | `NoDate` | `400` — D11 already makes a dateless Release unselectable in the picker; this is the server-side half of that rule |
   | `Unavailable` | `502` — the save is refused and may be retried. Nothing is persisted |

   **Bindability is checked at create only, and it is deliberately *not* an arm of the resolution.**
   D13 rules that a source's retired/released flags gate **binding**, never the lifecycle of a Delivery
   already bound — an existing binding keeps syncing and its date simply stops moving. Adding a
   `Retired` arm to `DeliverySourceResolution` would force the re-sync switch to carry a case it must
   always ignore, and a case that must always be ignored is a case someone will eventually handle. So
   **the resolution type stays lifecycle-blind**: it answers only "is it there, and what does it say".
   The create path applies `DeliverySourceBindability.For(hasDate, isRetired)` — the same predicate
   `GetOptions` uses to set `IsSelectable` (ADR-166 point 5) — on top of a successful resolution. One
   predicate, one construction site, two callers; without that sharing, a direct `POST` could bind
   something the picker calls unselectable.

   **`SourceLastSyncedOn` is set at create.** An earlier draft left it unset until the first successful
   refresh, so a Delivery that broke before its first refresh would render AC-04.2's "showing last
   synced values from ___" with a blank. A Delivery is never bound without having resolved at least
   once, so the column is non-null for the whole life of a binding.

## Alternatives considered

- **A single `IsSourceBroken` boolean.** **Rejected** — D12 needs two messages, and a bool forces the
  message either to be re-derived from something else or hard-coded to the deletion case. The deletion
  wording is the one that sends a reader hunting for a Release that is sitting right in front of them.

- **Derive the state at read time from `SourceLastSyncedOn`'s age.** **Rejected** by AC-04.5, above. It
  would also invent a staleness threshold that has to track the Portfolio refresh interval, so a slower
  refresh silently starts reporting healthy sources as broken.

- **Throw from `Resolve` for the not-found case.** **Rejected** — it routes the two outcomes that must be
  told apart (`NotFound` and `Unavailable`) into the same `catch`, which is exactly the failure AC-04.5
  names. It is also ADR-163's argument in miniature: an exception used for an expected outcome destroys
  the signal of the unexpected one.

- **Auto-unbind on `NotFound`.** **Rejected** by D6 outright: it silently converts a synced Delivery
  into a hand-maintained one whose date nobody is updating, and never tells anybody it happened.

- **Emit a domain event and notify someone.** Out of scope by D6 — the state is visible where the
  Delivery is read; no email, no alert.

## Consequences

**Positive**

- The four criteria most likely to be conflated — AC-04.1 (freeze), AC-04.5 (transient), AC-04.6
  (cleared date) and AC-03.6 (read failure) — become four arms of one switch over a closed type. A
  crafter cannot satisfy three and miss one without leaving an unhandled case.
- Nothing is deleted or cleared on any failure path, which is what #5698's durable-record decision
  requires.
- The handler stays pure with respect to Lighthouse state: it reads the remote and returns a verdict.
  Only the sync service mutates. "Did resolving write something?" is answerable from the signature.

**Negative / accepted**

- A fourth nullable column on `Delivery` and a fifth int-persisted enum in the model layer, with the
  append-only hazard every one of them carries.
- A Delivery can sit flagged indefinitely if nobody looks at it. That is the gap the feature delta names
  explicitly — there is no persona for "the person who set this up and left" — and instance-level health
  reporting is where it would be closed, not here.

**Reuse verdict**: `DeliveryWithLikelihoodDto` → **EXTEND** (two additive nullable fields beside
ADR-167's binding fields). `DeliverySection` / `DeliveryHeader` → **EXTEND** (broken-source banner and
Unbind action). `DeliverySourceResolution` and `DeliverySourceUnavailableReason` → **CREATE NEW** — a
closed result type and an enum, neither carrying behaviour. They exist so point 3's table is a
compiler-checked switch rather than four `if`s a future edit can leave incomplete.

**Enforcement**

| Rule | Mechanism |
|---|---|
| Every row of the transition table holds | NUnit, one test per row — the `Unavailable` row asserting **both** columns unchanged |
| A thrown handler is transient, never a deletion | NUnit: a handler that throws is treated as `Unavailable` by the sync service, never as `NotFound` |
| The reason enum's ordinals never move | NUnit reflection pinning all three members (the append-only rule) |
| A withdrawn capability degrades without unbinding | Integration: remove the handler's applicability for a connection, run a refresh, assert `CapabilityWithdrawn` and that the Delivery is **still bound** (AC-04.4) |
| Each reason reads as its own instruction | Vitest: each reason renders its own message, and an unmapped reason renders a neutral fallback rather than an empty banner |
| A cleared remote date is not reported as a deletion | Integration: clear the remote date, refresh, assert `SourceHasNoDate` and that the message differs from the deleted-Release one (AC-04.6) |
| A retired source cannot be bound through the API, only through the picker's absence | Integration: `POST` naming a retired-at-source Version returns `400`, even though it resolves cleanly |
| A source retired *after* binding changes nothing | Integration: bind, mark the Version archived remotely, refresh — assert the Delivery is still bound, still syncing, `SourceUnavailableReason` still null, and no snapshot pin was written (AC-01.8, D13) |
| The resolution type never learns about lifecycle flags | NUnit: `DeliverySourceResolution` has exactly four arms; a retired-but-present Version resolves to `Resolved`, never to a fifth state |

Cross-refs [ADR-167](./adr-167-source-binding-as-nullable-columns-behind-a-paired-mutator.md) (the
columns), [ADR-166](./adr-166-delivery-source-handler-registry-not-connector-port.md) (the registry that
decides `CapabilityWithdrawn`), [ADR-168](./adr-168-inbound-resync-sibling-service-at-the-portfolio-updater-seam.md)
(the only writer), [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) (the
exception-destroys-signal argument this reuses).
