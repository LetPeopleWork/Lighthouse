# ADR-161: The archived read path is a sibling projection that cannot reach live Features, not a branch inside `FromDelivery`

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 05)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

`DeliveryWithLikelihoodDto.FromDelivery(Delivery delivery, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)`
is the one projection that turns a `Delivery` into a row the Portfolio surface renders. It reaches
live Features three ways — `CalculateDeliveryWork`, `CalculateFeatureLikelihoods`, and through them
`Delivery.CalculateMetrics` — all of which read `delivery.Features` as it stands right now.

AC-05.3 requires that an archived Delivery's numbers are **identical across a Portfolio refresh**. A
refresh is precisely the event that moves Features in and out of a rule-based Delivery and re-runs
every forecast. So any archived read path that *can* still reach `Features` will drift the first time
someone edits the live path and forgets the archived case. DISCUSS asked for this to hold by
construction rather than by a branch someone can later forget.

## Decision

**A sibling projection on its own type, whose signature withholds the inputs the live path needs.**

```
ArchivedDeliveryProjection.ToDto(ArchivedDeliveryIdentity identity, DeliveryClosureRecord pin)
    -> DeliveryWithLikelihoodDto
```

Four points that are part of the decision:

1. **It takes no `DateOnly today` and no `IReadOnlyList<BlackoutPeriod>`.** Those are exactly the two
   arguments `Delivery.CalculateMetrics` requires. Without them in scope the forecast call does not
   compile. The failure mode AC-05.3 names is not guarded against — it is unavailable.

2. **It takes no `Delivery`.** `ArchivedDeliveryIdentity` is a small record carrying only what an
   archived row still legitimately shows: `Id`, `Name`, `Date`, `PortfolioId`, `SelectionMode`,
   `ConcurrencyToken`, `ArchivedOn`, `MetricSnapshotCount`. `Features` is not a field on it, so the
   live collection is not reachable at all — this is capability restriction at the boundary rather
   than a god-object handed over with a note asking callers to be careful.

3. **It lives on a new type, in its own namespace, so the rule is expressible as an architecture
   test.** Putting the factory as a second static method on `DeliveryWithLikelihoodDto` would make it
   unenforceable: that class legitimately depends on `Feature` and `BlackoutPeriod` for the live
   path, so a type-granularity ArchUnitNET rule could say nothing about it. As its own type,
   `ArchivedDeliveryProjection` can be pinned by a rule asserting it depends on **neither** `Feature`,
   `BlackoutPeriod`, `Delivery`, nor `IForecastService`.

4. **A distinct DTO out, carrying the Feature rows inline.** *(Corrected 2026-08-21. This point
   originally read "same DTO out", on the belief that archived and active rows render through the
   same grid. That was factually wrong and is the most serious defect this ADR shipped.)*

   The Delivery Feature grid is **not** rendered from `DeliveryWithLikelihoodDto`.
   `useDeliveryManagement.ts` reads `delivery.features` — a `number[]` of live Feature ids — and
   issues a **separate live GET** of Feature entities, which it hands to `FeatureListDataGrid`. So
   everything above protected a payload the grid never consumed, and the archived grid had two
   possible outcomes, both wrong: render empty (the early return when `features` is empty), or carry
   the live ids and re-fetch live Features — reintroducing precisely the drift this ADR exists to
   prevent, through a seam it never examined.

   The archived read therefore returns **`ArchivedDeliveryDto`**, which carries its Feature rows
   inline (from `FeatureBreakdownJson`) and **has no `features: number[]`**. The omission is the
   structural part: with no ids on the wire, the client has nothing to re-fetch *by*. A new
   `ArchivedFeatureGrid` consumes `DeliveryFeatureMetricDto` rows directly, keyed on `ReferenceId`.

   **No `FeatureId` is carried, deliberately** — an archived row must not offer navigation to a live
   entity that may since have moved or been deleted.

   **Columns an archived grid does not have**, because the pin does not hold them: work-item state,
   type, owning team(s), per-team remaining/total work, per-Feature forecast completion dates, and
   blocked status. It has Reference, Name, Completion %, Likelihood, Total Items and the default-size
   flag. D8's "export reads what is on screen" inherits exactly this set.

`ToDto` is a pure function of its two arguments — no clock, no repository, no forecast service. The
per-Feature rows come from `DeliveryMetricsHistoryDto.ParseFeatureBreakdown(pin.FeatureBreakdownJson)`,
reused as-is, which is what makes the "one encoding" claim in ADR-160 true rather than aspirational.

## Alternatives considered

- **A branch inside `FromDelivery`** (`if (delivery.ArchivedOn is not null) { … }`). The smallest
  diff, and the obvious first move. **Rejected** — the branch keeps `delivery`, `today` and
  `blackoutPeriods` in scope for the archived case, so the exact defect AC-05.3 exists to prevent
  stays one line away, permanently, and every future edit to the live path is another chance to
  reintroduce it. It also cannot be pinned by an architecture test, because the enclosing type must
  keep its live-path dependencies.

- **A separate archived DTO type on the wire** (`ArchivedDeliveryDto`). Originally **rejected** on the
  grounds that "the two rows render in the same grid with the same columns". **That premise was
  false, and this alternative is now the decision** — see point 4. The two rows do not render in the
  same grid: the live grid is fed by a separate fetch of `IFeature` entities, and the pinned row is a
  strictly narrower shape. Forking the client model is the correct cost, because the distinction is
  one the reader *does* care about — it is the difference between the record and the present.

- **Recompute from the pin at read time** rather than storing the projected values. **Rejected** —
  the pin already holds the computed figures (ADR-160 point 3); recomputing would reintroduce a
  forecast call on the archived path, which is the thing being designed out.

## Consequences

- **Positive**: AC-05.3 holds because the archived projection has no access to the inputs that could
  make it drift, not because a branch is correct today.
- **Positive**: the archived path needs no blackout periods and no clock, so it is trivially testable
  as a pure function and cannot be affected by the `DateTime.UtcNow` / calendar-day class of defect
  the ledger tracks.
- **Negative**: one more type, and the caller must choose which projection to use. That choice lives
  in exactly one place — the assembler in `DeliveriesController.GetByPortfolio` — and the choice is
  driven by `ArchivedOn is not null`, which is the same predicate that decides everything else about
  an archived Delivery.
- **Negative**: `ArchivedDeliveryIdentity` duplicates a handful of `Delivery`'s fields. That is the
  price of the restriction and is the point rather than an accident; a test asserts the DTO's
  identity fields are equal whichever projection produced them.
- **Enforcement**: ArchUnitNET rule in `Lighthouse.Backend.Tests/Architecture/`, modelled on the
  existing `DeliveryGrainSeamArchUnitTest`, asserting `ArchivedDeliveryProjection` depends on none of
  `Feature`, `Delivery`, `BlackoutPeriod`, `IForecastService`. Plus an integration test that reads an
  archived Delivery, runs a Portfolio refresh that provably moves its Features, reads again, and
  asserts the two payloads are byte-identical.
- **Reuse verdict**: `DeliveryWithLikelihoodDto` (the type) → **REUSED AS IS** as the output contract.
  `DeliveryWithLikelihoodDto.FromDelivery` → **UNCHANGED** — the live path is not touched.
  `DeliveryMetricsHistoryDto.ParseFeatureBreakdown` → **REUSED AS IS**.
  `ArchivedDeliveryProjection` + `ArchivedDeliveryIdentity` → **CREATE NEW**, justified because the
  enforcement in point 3 is not expressible if the code lives on the existing type.
- Cross-refs [ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md) (the row this
  reads), [ADR-050](./adr-050-metrics-history-endpoint-and-snapshot-schema.md) (the reused parser),
  [ADR-121](./adr-121-delivery-metrics-history-client-projection.md) (the client-side projection of
  the same payload), [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) (how the
  recorder stops feeding an archived Delivery in the first place).
