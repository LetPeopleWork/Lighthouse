# ADR-163: The recorder stops at an archived Delivery because its port cannot yield one, not because a global query filter hides it

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 04)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D3 says archiving stops the machinery rather than merely hiding the row. Two consequences are in
scope: the forward recorder must not write another `DeliveryMetricSnapshot` for an archived Delivery
(AC-04.6), and rule re-matching must not move its Features (AC-04.7).

`IDeliveryRepository.GetByPortfolioAsync(int portfolioId)` is the shared read. Its consumers want
different things:

- `DeliveryMetricSnapshotRecordingHandler` — must **exclude** archived (AC-04.6).
- `DeliveryRuleService.RecomputeRuleBasedDeliveries` — must **exclude** archived (AC-04.7).
- `DeliveriesController.GetByPortfolio` — must **include** archived, because Slice 05 renders them in
  their own section.

A global EF `HasQueryFilter` on `Delivery` would serve the first two and break the third, and the
break would be silent: the archived section would simply render empty, with `IgnoreQueryFilters()`
as an easily-forgotten opt-out on exactly the read that must not forget it. The codebase also uses
`HasQueryFilter` **nowhere today**, so this would introduce a repository-wide mechanism to solve one
feature's problem.

## Decision

**No global query filter. The recorder is handed a port that cannot return an archived Delivery, and
Feature mutation is refused by the aggregate as a backstop.**

Four points:

1. **A narrow read port for the recorder.** `IDeliveryRepository` gains
   `RecordableDeliveries GetRecordableByPortfolio(int portfolioId)` — deliveries with
   `ArchivedOn is null`. The recording handler depends on that method and never on
   `GetByPortfolioAsync`. This is capability restriction rather than discipline: the handler is not
   trusted to remember a `where` clause, it is given a source that cannot produce the wrong rows.

2. **Rule re-matching is narrowed by the same port, through a type.**
   `GetRecordableByPortfolio` returns a sealed `RecordableDeliveries : IReadOnlyList<Delivery>` that
   only the repository can construct, and
   `DeliveryRuleService.RecomputeRuleBasedDeliveries(Portfolio, RecordableDeliveries)` takes that type
   rather than `IEnumerable<Delivery>`. One narrowing serves both background consumers instead of
   protecting the recorder and leaving rule re-matching to remember a filter.

   **What this does and does not buy, stated precisely.** `RecordableDeliveries` and
   `DeliveryRepository` live in the same assembly, so `internal` is reachable from anywhere in
   `Lighthouse.Backend` — and the element type is still `Delivery`, so this is a **nominal marker,
   not a refinement type**. Nothing in the type system prevents it holding an archived row. An
   earlier draft of this ADR claimed passing an archived Delivery "does not compile"; that was an
   overclaim and is withdrawn. What the type actually buys is worth keeping: **one construction site**
   for the filter instead of one per caller, a name at every call site that says which set this is,
   and a **constructor-level assertion** that no element has `ArchivedOn` set — so a violation fails
   loudly at the single place that can create one, rather than silently at the mutation.

   This replaces an earlier draft of this ADR in which rule re-matching relied on the aggregate
   throwing. That draft made `DeliveryArchivedException` the **normal** case on the Portfolio-refresh
   hot path — raised and swallowed once per archived rule-based Delivery per refresh — and it
   destroyed the signal [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md)'s
   concurrency rule depends on: if the exception is expected, a lost stale-aggregate race is
   indistinguishable from routine operation at the catch site. With both consumers narrowed, the
   guard firing on a background path means precisely one thing — the Delivery was archived after the
   collection was read.

   **This changes a signature pinned by [ADR-012](./adr-012-rule-engine-generalisation.md)'s
   reflection test**, which asserts `RecomputeRuleBasedDeliveries` still exists with its original
   signature. That guard exists to stop the rule-engine generalisation *silently* altering the public
   surface; this change is deliberate and recorded, so the guard is updated in the same commit rather
   than circumvented.

3. **The aggregate keeps refusing, as a backstop.** `Delivery.Features`
   is written from **three** places — `DeliveriesController.CreateManualFeatureSelectionDelivery`,
   `DeliveriesController.CreateRuleBasedDelivery`, and
   `DeliveryRuleService.RecomputeRuleBasedDeliveries`, the last of which is reached from
   `PortfolioUpdater` and is not an endpoint at all. Filtering at the caller would therefore have to
   be remembered in three places, one of which no controller test covers. Instead `Features` becomes
   `IReadOnlyList<Feature>` with a single mutator, `Delivery.ReplaceFeatures(...)`, which refuses when
   `ArchivedOn is not null`. All three paths are covered by one rule, and a fourth path added later
   inherits it.

4. **`GetByPortfolioAsync` keeps returning everything.** The controller splits active from archived
   for the two sections. Slice 04 wants archived Deliveries out of the *active list*, which is a
   presentation split, not a data-access one.

## Alternatives considered

- **A global `HasQueryFilter(d => d.ArchivedOn == null)` on `Delivery`.** **Rejected** — it changes
  every consumer of the entity, present and future, to serve two of them. The archived read (Slice 05)
  would need `IgnoreQueryFilters()`, and forgetting it yields an empty archived section rather than an
  error. It would also be the first query filter in the codebase, so nobody reading a `Delivery`
  query would expect one.

- **A `where d.ArchivedOn == null` clause added to each of the recorder and the rule service.**
  **Rejected** — a clause is a convention a future edit can drop with no signal, which is the failure
  this ADR exists to remove. A clause has to be re-remembered at every call site; the collection type
  has **one** construction site, and that site asserts. The clause also leaves the two controller write
  paths untouched, so it would satisfy AC-04.7 while leaving AC-05.8 to a separate mechanism.

- **Letting the aggregate's exception carry rule re-matching**, with no narrowing on that path. This
  was an earlier draft of this ADR. **Rejected** — it makes `DeliveryArchivedException` the expected
  case on the Portfolio-refresh hot path, and it collapses the stale-aggregate race into the same
  catch site as routine operation, so neither can be told from the other.

- **A `DeliveryArchived` domain event that unsubscribes the recorder.** **Rejected** — the recorder is
  stateless and per-portfolio; there is no subscription to withdraw, and an event would add a second
  source of truth for "is this archived" alongside the column.

## Consequences

- **Positive**: AC-04.6 and AC-04.7 hold across every caller, including the background one, and a new
  caller cannot regress them without deliberately reaching past the aggregate.
- **Positive**: no repository-wide mechanism is introduced, so no unrelated query changes meaning.
- **Negative**: `Delivery.Features` changing from `List<Feature>` to `IReadOnlyList<Feature>` touches
  the three write sites plus their tests. Reads are unaffected except that `Features.Exists(...)`
  inside `CalculateMetrics` becomes `Features.Any(...)`. Contained, and the compiler finds every site.
- **Negative**: `IDeliveryRepository` grows a fourth method, and the two "get deliveries" methods can
  be confused at a glance. The names carry the distinction, and the ArchUnitNET rule below makes the
  wrong choice fail rather than merely read oddly.
- **Enforcement**: an ArchUnitNET rule asserting `DeliveryMetricSnapshotRecordingHandler` does not
  depend on `GetByPortfolioAsync`, plus an integration test that archives a Delivery, runs a Portfolio
  refresh, and asserts both that no new snapshot row appeared and that its Feature set is unchanged.
- **Reuse verdict**: `IDeliveryRepository` / `DeliveryRepository` → **EXTEND** (one method).
  `DeliveryMetricSnapshotRecordingHandler` → **EXTEND** (swap its source). `DeliveryRuleService` →
  **UNCHANGED** — it keeps calling `ReplaceFeatures`, which now refuses on its own. `Delivery` →
  **EXTEND** (encapsulate the collection). No new component.
- Cross-refs [ADR-048](./adr-048-delivery-metric-snapshot-store.md) and
  [ADR-049](./adr-049-forward-recorder-hook-point-and-idempotency.md) (the recorder whose source this
  narrows), [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md) (the same
  aggregate guard applied to the rest of the writes),
  [ADR-012](./adr-012-rule-engine-generalisation.md) (the rule service whose public surface is
  preserved).
