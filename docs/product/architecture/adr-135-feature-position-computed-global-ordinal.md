# ADR-135: Position is a computed global ordinal from a narrow projection, never a SQL window function

**Status**: Accepted
**Date**: 2026-08-06
**Feature**: `epic-5375-manual-sorting` (ADO Epic #5375 "Manual Sorting")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

AC-1.5 asks for something sharper than it looks: the `#` column must show a Feature's rank **across the
whole instance**, so two rows displayed consecutively inside one Portfolio may legitimately read `4`
and `17`. AC-1.6 adds that sorting the grid by another column must leave every position value
unchanged, and AC-1.7 that hiding Done Features must not renumber the rest.

Together those three make the position a **data field computed over the full ordered set**, not a row
index and not a client-side counter. [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md)
INV-O2/INV-O3 sharpen it further: contiguity is not a contract, so the position is also **not** the
stored `ManualRank` — with gaps the two diverge, and nothing may read a rank value.

Two endpoints must carry it, and one of them is a subset read:

- `GET /features` (new) — the whole visible set for the Features view.
- `GET /features/ids` (existing) — what `PortfolioFeatureList.tsx:66-70` calls with a handful of ids to
  render the Portfolio surface, which D10 says gets the same column from the same factory.

DISCUSS did not anticipate this constraint. It is the feature's sharpest technical requirement, because
the obvious implementations are each wrong in a different way: a row index violates AC-1.5, the stored
rank violates INV-O2, and a per-row lookup over an already-heavy read path is an N+1 waiting to happen.

## Decision

**Position is computed by one dedicated read-side port from a single narrow projection query over the
whole `Features` table, ordered by the same comparison [ADR-134](./adr-134-ordering-policy-appsetting-enum-single-selection-point.md)
selects, and materialised as a `featureId -> ordinal` map. It is computed *before* any filtering and
shipped as an additive `Position` field on `FeatureDto`.**

```
IFeaturePositionMap.GetAsync(CancellationToken) -> IReadOnlyDictionary<int, int>
```

Implementation shape: `Context.Features.AsNoTracking().Select(f => new FeatureOrderKey(f.Id, f.Order,
f.ManualRank))` — one query, three columns, **no `Include` graph** — then `IFeatureOrdering`'s
comparison in memory, then `Select((key, i) => (key.Id, i + 1))`.

Three properties follow, and each maps to an AC rather than to taste:

- **Global, because the map is built over the whole table before the RBAC filter runs.** A user who can
  read one Portfolio sees the same numbers as an admin. AC-1.5, and ADR-132's constraint that the filter
  must not change position values.
- **Stable under display state, because it is a value on the row.** MUI-X sorting and the
  `hideCompleted` toggle are client-side operations on rows that already carry their position. AC-1.6,
  AC-1.7.
- **Never blank.** A Feature with an empty `Order` and a null `ManualRank` still has an ordinal, because
  INV-O1 is total. AC-1.8 — no `NaN`, no empty cell.

The DTO field is named `Position`, **not** `rank`. `slices/slice-01-…md` proposes "an additive `rank`
integer on `FeatureDto`"; that wording is corrected here rather than followed, because INV-O2 forbids
any consumer reading a rank value and the two numbers differ the moment gaps exist.

## Alternatives Considered

### A. A SQL window function — `ROW_NUMBER() OVER (ORDER BY …)` — rejected on a hard blocker

The textbook answer, and it would push the whole computation into the database.

It cannot work here, and not for a performance reason. Under `SourceOrder` the comparison is
`int.TryParse`, then `double.TryParse` **with the sign inverted for Linear**, then `string.Compare`
(`FeatureComparer.cs:10-42`). That is not expressible as a SQL `ORDER BY` in any provider, and
Lighthouse ships SQLite, PostgreSQL and SQL Server. A window function could therefore serve only the
`ManualOrder` half of the policy — producing **two ordinal implementations that must agree**, which is
precisely the failure mode ADR-134 exists to remove. Rejected as structurally incompatible with K4,
not as an optimisation not worth taking.

### B. A cached ordinal map, invalidated on change — rejected as premature

Would avoid re-sorting per request.

Rejected because the invalidation surface is larger than the thing it saves: every move, every sync
tail-append, every policy flip, and every seed. At the target size the projection query is the cheap
part of a request that already loads three `Include` graphs. A cache here would buy microseconds and
sell a staleness bug — and a stale ordinal is invisible, because a wrong number still renders.

### C. Reuse `FeatureRepository.GetAll()` and number its result — rejected on cost, not correctness

Correct, and tempting because ADR-132 INV-O3 observes the sort is already paid for. But `GetAll()`
loads `Portfolios`, `FeatureWork.Team` and `Forecasts.SimulationResults` for every Feature in the
instance (`FeatureRepository.cs:38-46`). `GET /features/ids` — called by the Portfolio surface with a
handful of ids — would pay a whole-instance graph load to number a few rows. The projection in the
decision is the same sort over ~1/20th of the bytes.

### D. Compute the ordinal client-side from the returned array index — rejected

The cheapest thing, and wrong: it is the row index. It fails AC-1.5 on any filtered view, fails AC-1.6
the moment a user clicks a column header, and fails AC-1.7 when Done rows are hidden. It is named here
because it is what the requirement looks like from a distance.

## Consequences

**Positive**

- One code path produces positions for both endpoints, in both policy states, for every connector's
  `Order` shape. There is no second numbering to keep in step.
- The RBAC filter and the position computation are independent: neither can silently change the other's
  result, which is the property ADR-132 asked for.
- No cache, no projection table, no invalidation, no background job. The read model is a `Select`.

**Negative / cost**

- **The whole-table sort happens twice per `GET /features` request** — once inside
  `FeatureRepository.GetAllByPredicate`, once for the position map. At 500 Features that is two sorts
  of a 500-element list, microseconds, but it is genuinely redundant work and is stated rather than
  hidden. The obvious fusion — have the repository return positions alongside entities — is declined
  because it would push a read-model concern into `RepositoryBase<Feature>`'s override, which every
  unrelated caller (`ForecastService`, sync, metrics) would then pay for.
- One extra database round trip on the two endpoints that render positions. Not on any other path.
- The number a user reads is not the number in the database once gaps exist. ADR-132 already recorded
  this as the ergonomics cost of relaxing contiguity; this ADR is where it becomes visible to support.
- At an order of magnitude beyond the K6 target the in-memory sort becomes the bottleneck rather than
  the query. The named revisit trigger is K6's 500 ms p95 failing at the measured instance size — at
  which point the honest fix is sparse keys (ADR-132 alternative D) plus a `ManualOrder`-only window
  function, accepting the two-path cost with eyes open.

**Quality attribute impact**

- Correctness: the position is total, global and filter-independent by construction rather than by test.
- Performance: one extra narrow query per position-rendering request; unchanged elsewhere.
- Portability: no provider-specific SQL is introduced, which is what alternative A would have cost.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Positions are global, not per-result-set | Integration test: two users with disjoint `PortfolioRead` scopes request `GET /features`; a Feature visible to both reports the same position |
| Filtering does not renumber | Integration test: `GET /features/ids` for a non-contiguous subset returns non-consecutive positions (the literal AC-1.5 `4` and `17` case) |
| Hiding Done rows does not renumber | Vitest on `FeatureListDataGrid` — toggle `hideCompleted`, assert remaining rows' position cells are unchanged (AC-1.7) |
| Column sorting does not renumber | Vitest — sort by Name, assert position cell values are unchanged (AC-1.6) |
| Every Feature has a position | Integration test over a set containing empty `Order`, null `ManualRank`, duplicate ranks and gaps; assert `1..N` with no holes and no nulls (AC-1.8) |
| The position map uses no `Include` | Unit test asserting the projection type is `FeatureOrderKey`, plus code review; there is no ArchUnit rule that can see a LINQ shape, and that limit is stated rather than papered over |
| 500 Features stay interactive | Vitest render assertion at 500 rows (AC-1.9); `DataGridBase` already virtualises (`DataGridBase.tsx:28`) |

## Cross-reference

- [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) — INV-O2 (nothing
  reads a rank value) and INV-O3 (position is a computed ordinal) are what this ADR implements.
- [ADR-134](./adr-134-ordering-policy-appsetting-enum-single-selection-point.md) — the position map
  reuses the comparison selected there. That reuse is the reason alternative A is not available.
- [ADR-136](./adr-136-feature-move-authorization-and-non-disclosing-block-reason.md) — the RBAC filter
  that runs *after* this computation.
- Corrects `docs/feature/epic-5375-manual-sorting/slices/slice-01-features-view-and-position-column.md`
  on the DTO field name (`position`, not `rank`).
- **Clients consistency**: `Position` is an additive field on `FeatureDto`, which
  `docs/concepts/api-versioning.md` makes a non-breaking change. No Lighthouse-Clients version bump is
  owed. DISCUSS asked that this be said out loud at the point it became real; this is that point.
