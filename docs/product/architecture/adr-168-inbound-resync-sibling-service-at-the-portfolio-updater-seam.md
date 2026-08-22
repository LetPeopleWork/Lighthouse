# ADR-168: Inbound re-sync is a sibling service at the `PortfolioUpdater` seam, narrowed by `RecordableDeliveries` — not the rule service, not an event handler

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slice 02)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D9 puts inbound sync on the existing Portfolio refresh, on the existing cadence: no new background
service, no new schedule, no new setting. S8 names the seam — `PortfolioUpdater.Update` reads
`deliveryRepository.GetByPortfolioAsync(project.Id)`, calls
`deliveryRuleService.RecomputeRuleBasedDeliveries(project, deliveries)`, then `Save()`. Verified at
`PortfolioUpdater.cs:72-80`; the feature delta's `:73-79` has drifted by a line.

**Ordering is a hard constraint, not a preference.** Re-sync must run *after*
`UpdateFeaturesForPortfolio` (`:75`), so the `fixVersions` on the Portfolio's Features are current, and
*before* `UpdateForecastsForPortfolio` (`:91`), so the forecast is computed against the new membership
and the new target date. Those two calls are sixteen lines apart in one method.

AC-03.7 requires that an archived Delivery is not re-synced — its pinned closure snapshot must stand.

## Decision

**A sibling service, called inline at the same seam, taking the same narrowed collection.**

1. **`IDeliverySourceSyncService.ResyncSourceBoundDeliveries(Portfolio portfolio, RecordableDeliveries
   deliveries)`** — a new service beside `IDeliveryRuleService`, called from `PortfolioUpdater.Update`
   immediately after `RecomputeRuleBasedDeliveries` and before the existing `Save()`.

   **It resolves the whole pass in one batch, then applies per Delivery.** It collects the distinct
   source references across the bound Deliveries, makes **one** call to `IDeliverySourceResolver`
   ([ADR-170](./adr-170-broken-source-as-recorded-verdict.md) point 6), and switches over the returned
   verdict for each Delivery. Two remote calls per refresh, constant in the number of bound Deliveries
   — see [ADR-171](./adr-171-release-membership-by-jql-reference-ids.md) for the cost analysis against
   the 5% KPI. It does **not** own the resolution logic; the create endpoint depends on the same
   resolver.

2. **It takes `RecordableDeliveries`, from the same narrowed port, not `GetByPortfolioAsync` plus a
   predicate.** AC-03.7 then holds because the collection has **one construction site that asserts** no
   element has `ArchivedOn` set — the filter is applied once rather than re-remembered per caller — not
   because somebody remembered a `where`.

   **`RecordableDeliveries` is a nominal marker, not a refinement type, and an earlier draft of this ADR
   claimed otherwise.** ADR-163 point 2 explicitly withdraws the "does not compile" overclaim: the
   element type is still `Delivery`, and nothing in the type system prevents the collection holding an
   archived row. What the type buys is one construction site, a name at every call site saying which set
   this is, and a constructor-level assertion. **The crafter's obligation is that assertion.** A reader
   who takes the compiler's word for it will not write it. Reaching for `GetByPortfolioAsync` and catching
   `DeliveryArchivedException` instead would be the design error
   [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) exists to prevent: it makes the
   guard fire on the hot path as the expected case, and destroys the one signal
   [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md)'s concurrency rule depends
   on.

3. **The unit of work is per Delivery**, matching what ADR-164 gives the rule recompute. A
   `DbUpdateConcurrencyException` on one Delivery skips that Delivery and the batch continues; a
   Delivery archived under us no longer wants re-syncing.

4. **`PortfolioUpdater` reads the collection once and passes it to both services.** One
   `GetRecordableByPortfolio` call, two consumers, one `Save()`.

5. **Not the rule service.** `DeliveryRuleService` is the *rule* service, and D3's whole argument is
   that a source handler is not a rule. Its public surface is pinned by
   [ADR-012](./adr-012-rule-engine-generalisation.md)'s reflection test, and ADR-163 already spends that
   pin once by narrowing the parameter. Spending it twice, from two epics in flight simultaneously, is
   how a guard gets disabled rather than updated.

6. **Not a domain-event handler.** Neither existing event sits at the right point:
   `PortfolioFeaturesRefreshed` is published at `:76`, *before* the delivery work, and
   `PortfolioForecastsUpdated` at `:97`, *after* the forecast. A new event placed between them would put
   the ordering contract into `Program.cs` registration order — which
   [ADR-144](./adr-144-writeback-collection-seam.md) point 6 and
   [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) D2 both reject,
   for exactly this reason, on exactly this method.

## Sequencing — the binding constraint on slice 02

`RecordableDeliveries`, `GetRecordableByPortfolio`, `DeliveryArchivedException`, `Delivery.ArchivedOn`
and the `Delivery` mutator encapsulation are **#5698 phase-4 work, and none of it is in the codebase
today.** Verified: `grep ArchivedOn` across the backend returns nothing, as do `RecordableDeliveries`
and `DeliveryArchivedException`; `Delivery.Features` is still `public List<Feature> { get; }`, mutated
by `.Clear()`/`.AddRange()` at the three sites ADR-163 names. #5698 has shipped through **notes**
(`DeliveryNote.cs`, `DeliveryNotesController.cs` both exist), not through archiving.

**What slice 02 can build first**: `IDeliverySourceHandler.Resolve`, the resolution result type, the
Jira resolve adapter, and `DeliverySourceSyncService` itself — written against `RecordableDeliveries`
from its first commit — together with its whole unit suite against a hand-constructed collection.

**What must wait**: the `PortfolioUpdater` wiring line, and AC-03.7's integration test.

**If #5698 archiving slips, slice 02 is held, not softened.** Reaching for `GetByPortfolioAsync` with a
predicate to unblock it would ship the exact convention ADR-163 rejected, in the same method, weeks
later. The slice brief already says AC-03.7 must be held rather than skipped silently; this is the
architectural form of that instruction.

## Alternatives considered

- **Extend `DeliveryRuleService`.** **Rejected** — see Decision point 5. It also re-merges the two
  concepts D3 spent four arguments separating, one slice after separating them.

- **A domain-event handler on a new `PortfolioDeliveriesResyncing` event.** **Rejected** — see Decision
  point 6. ADR-144 met this exact choice on this exact method and chose the inline seam; the reasoning
  is unchanged and the precedent is one method away.

- **A new background service on its own cadence.** **Rejected** by D9, and independently by the ordering
  constraint: a second schedule cannot express "after the Feature fetch, before the forecast run".

- **Re-sync at read time**, when the Delivery grid is fetched. **Rejected** — it makes a GET perform a
  remote call and a write, it re-syncs N times for N readers and zero times for none, and the forecast
  would still have been computed against the previous target.

## Consequences

**Positive**

- Sync latency equals the Portfolio refresh interval, which is the latency every other number on the
  Delivery already has (D9) — so it needs no separate explanation to the user.
- One `Save()` covers the rule recompute and the source re-sync together.

  **AC-03.4 needs an explicit comparison, and an earlier draft of this ADR got that wrong.** It claimed
  the no-op fell out of EF's change tracker. It does not: ADR-164 requires every mutator to bump
  `ConcurrencyToken`, which makes the `Deliveries` row modified and emits an `UPDATE` even when every
  value is identical — that bump is the whole reason a Features-only write is protected at all. So
  `ApplySourceSnapshot` **compares first and reports whether it changed anything**, and the sync service
  applies it only when the resolution differs from what is stored. This is a hand-rolled comparison, and
  naming it is the point: the alternative is a design that writes on every refresh of every bound
  Delivery while claiming not to.
- `PortfolioUpdater.Update` still reads top-to-bottom as fetch → recompute → re-sync → forecast, which
  is the property ADR-144 point 5 explicitly preserved.

**Negative / accepted**

- `PortfolioUpdater.Update` gains a ninth service resolution, in a method ADR-027 already names as a
  service-locator smell. Accepted rather than hidden: the alternative trades a *visible* smell for an
  *invisible* ordering contract, and ADR-027's own migration stance is that this pipeline is peeled
  apart in dedicated slices, not opportunistically inside a feature.
- A Jira read failure must not fail the Portfolio refresh (AC-03.6). Because resolution is batched, a
  transport failure yields `Unavailable` for **every** Delivery in the pass rather than a mix — which
  is the more honest reading of one outage, and means the failure is logged once rather than N times.
  The refresh's `success` flag and its `RefreshLog` row are unaffected.
- The service is the **second** caller of the resolver, not its owner. That is deliberate: the create
  endpoint needs the same four-arm switch (ADR-170 point 6), and duplicating it is how the two paths
  would drift on which verdicts are recoverable.

**Reuse verdict**: `PortfolioUpdater` → **EXTEND** (one call, one shared collection read).
`IDeliveryRuleService` / `DeliveryRuleService` → **UNCHANGED** by this Epic — its signature change is
ADR-163's and must not be made twice. `IDeliveryRepository` / `DeliveryRepository` → **UNCHANGED** by
this Epic; it consumes #5698's method and adds none. `IDeliverySourceSyncService` and its implementation
→ **CREATE NEW**: no existing service resolves a remote object and applies it to a Delivery, and the one
component with overlapping responsibility (the rule service) is rejected above on a pinned-signature
argument, not on a complexity one.

**Enforcement**

| Rule | Mechanism |
|---|---|
| The sync service cannot see an archived Delivery | `RecordableDeliveries`' **constructor assertion** — one construction site, *not* a compile-time guarantee (ADR-163 point 2) — plus an ArchUnitNET rule that `DeliverySourceSyncService` does not depend on `GetByPortfolioAsync`, the same rule shape ADR-163 puts on the recording handler |
| An archived Delivery is not re-synced | Integration: archive a bound Delivery, run a Portfolio refresh, assert its date, name and Feature set are unchanged **and** that no remote call was made for it (AC-03.7) |
| A no-op refresh writes nothing | Integration: identical remote values across two refreshes issue no `UPDATE` and produce no new history entry (AC-03.4) |
| A read failure does not fail the refresh | Integration: the handler throws, last-known values survive, and the refresh still records success (AC-03.6) |
| The ordering is the reason this is not an event, so the ordering is asserted | NUnit on `PortfolioUpdater`: the source sync runs after `RecomputeRuleBasedDeliveries` and before `UpdateForecastsForPortfolio` |

Cross-refs [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) (the narrowed port this
consumes and must not bypass), [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md)
(the per-Delivery unit of work), [ADR-144](./adr-144-writeback-collection-seam.md) (the adjacent seam in
the same method, and the precedent for not using the dispatcher here),
[ADR-012](./adr-012-rule-engine-generalisation.md) (the pinned signature this Epic does not spend).
