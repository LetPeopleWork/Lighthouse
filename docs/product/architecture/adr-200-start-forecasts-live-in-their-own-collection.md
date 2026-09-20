# ADR-200: Start forecasts live in their own collection on Feature, not alongside completion forecasts behind a discriminator

- **Status**: Accepted, amended 2026-09-20
- **Date**: 2026-09-20
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, Story #6045)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Amendment, 2026-09-20 — the row type

Amended the same day it was written, before any code was, during the DISTILL review gate.

The decision below originally said `Feature.StartForecasts` would hold **`WhenForecast`** rows, reusing
that type as-is. **That cannot be mapped, and the reason matters more than the inconvenience.**
`WhenForecast` has exactly one `FeatureId`/`Feature` pair, and `LighthouseAppContext` already binds it to
`Feature.Forecasts`. EF Core cannot carry two collections of one entity type over one foreign key, and
adding a second foreign key does not rescue it: `FeatureId` is a **required** `int`, so a start row would
still have to carry a valid one — and would then be loaded into `Feature.Forecasts`, which is the exact
silent corruption this ADR exists to prevent. The mapping would have reintroduced the trap through the
back door.

The forecast hierarchy is already table-per-hierarchy: `ForecastBase` is the table, with a
`Discriminator` column and `WhenForecast` as one of its values (`IndividualSimulationResult.Forecast` is
typed `ForecastBase`). So the row type is:

> **`StartForecast : ForecastBase`** — a TPH **sibling** of `WhenForecast`, not a derived type, carrying
> its own `FeatureId` and nullable `TeamId`. `Feature.StartForecasts` is `List<StartForecast>`.

Three consequences, all of which make this ADR's own argument stronger rather than weaker:

1. **The AC-1.5 guarantee gets harder, not softer.** `Feature.Forecasts` is `List<WhenForecast>`, and a
   sibling type cannot appear in it **by CLR type** — not by filter, not by convention, not by anyone
   remembering. Decision point 2 below is now enforced by the type system rather than by the shape of
   the collection.
2. **Decision point 3 is superseded, and its one blemish disappears with it.** That point excused
   `NumberOfItems` — meaningless for a start — as a tolerable wart. `NumberOfItems` is declared on
   `WhenForecast`, not on `ForecastBase`, so a sibling never inherits it. The new type carries no unused
   field and nothing has to be excused.
3. **The "new lean `StartForecast` type" alternative below is now the decision** — but not for the reason
   it was argued on. It was rejected as disproportionate domain tidying; it is adopted as the only shape
   the persistence layer can express. It also costs less than that entry assumed: deriving from
   `ForecastBase` duplicates nothing, because the percentile reads and the `SimulationResults`
   relationship are on the base.

Still additive and expand-only, as decision point 5 requires: one new discriminator value and two new
nullable columns in an existing table, with no existing column altered.

**One mechanical trap, named so DELIVER does not diagnose it from scratch.** `StartForecast.FeatureId`
and `StartForecast.TeamId` share their names with `WhenForecast.FeatureId` and `WhenForecast.TeamId`, and
both types live in the same physical `ForecastBase` table. EF Core does not merge same-named properties
from sibling types onto one column: left to convention it either uniquifies them into something like
`FeatureId1` or refuses, and neither is what anyone intended. Map them explicitly with `HasColumnName`
(`StartFeatureId`, `StartTeamId`) when the mapping is written, and read the generated migration before
accepting it. This surfaces at migration-generation time rather than silently — but only if someone looks
at the column list.

The reuse verdict at the foot of this document is corrected accordingly: `WhenForecast` → **NOT REUSED**;
`ForecastBase` → **REUSED AS IS, by inheritance**; **one new type**, `StartForecast`.

## Context

[ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) produces two kinds of start
histogram per Feature: one per contributing `(Feature, Team)` row, and one at Feature grain. Both have
to be persisted — they are observed inside the run and there is no arithmetic to recompute them from
afterwards, which is the same property that forces
[ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) to persist its aggregate.

`Feature` already owns `List<WhenForecast> Forecasts`, one row per `(Feature, Team)`, mapped by
`LighthouseAppContext` with cascade delete and rewritten wholesale by `SetFeatureForecasts` on every
refresh. It is the obvious place to put them.

**It is also a trap.** `Feature.Forecast` is:

```csharp
public WhenForecast Forecast => new AggregatedWhenForecast(Forecasts);
```

`AggregatedWhenForecast` aggregates **every** entry of that collection, unconditionally. A start row
landing in `Forecasts` would be folded into the completion forecast on every Feature, silently, on the
single most load-bearing number the product emits. Nothing would throw and nothing would look wrong.

There is a second consideration pulling the other way. ADR-156 declares that when the completion
aggregate is eventually observed per trial, it **joins `Feature.Forecasts` as the row whose `TeamId` is
null** — the shape `AggregatedWhenForecast` already declares under
[ADR-111](./adr-111-aggregate-forecast-field-provenance.md). So that collection's intended future
already contains a Feature-grain row, and a discriminator would be needed there regardless.

## Decision

**A second collection, `Feature.StartForecasts`, holding rows of the same shape.** (Originally
`WhenForecast` rows; `StartForecast` rows per the amendment above.)

1. **Per-team rows carry their `TeamId`. The Feature-grain row carries `TeamId == null`**, exactly as
   ADR-111 defines for an aggregate: no single team owns it, null is the honest answer, and the
   relationship is already nullable with `OnDelete(SetNull)`.

2. **`Feature.Forecast` is not touched.** AC-1.5 — the completion forecast is unchanged before and
   after this Epic — becomes a structural property rather than a test. `AggregatedWhenForecast` cannot
   see a start row, because start rows are not in the collection it reads. A filter would make the same
   guarantee conditional on nobody later removing it.

3. ~~**`WhenForecast` is reused as-is, not subclassed or forked.**~~ **Superseded by the amendment
   above.** The histogram still comes through `ForecastBase`, whose `GetProbability` and ascending
   `KeyOrder` are exactly what a start percentile needs — but it arrives by inheriting `ForecastBase`
   directly rather than by reusing `WhenForecast`, because two collections of `WhenForecast` cannot be
   mapped onto one foreign key. `NumberOfItems` is not inherited and needs no excusing.

4. **Same lifecycle as `Forecasts`**: cleared and rewritten in full on every forecast run, cascade
   delete from `Feature`. A start forecast is current state, never history. The deferred over-time view
   (DISCUSS D12) is a snapshot concern and follows `DeliveryMetricSnapshot`'s pattern — a dedicated
   entity written by a handler on a domain event — not a retention policy on this collection.

5. **Additive, expand-only migration**, generated with the `CreateMigration` script across all
   providers.

## Alternatives considered

- **One collection with a `ForecastKind` discriminator.** Tidier as a schema, one table instead of two,
  and closer to where ADR-156 points. **Rejected** — it requires `Feature.Forecast` to filter, which
  puts a `WHERE` clause on the product's most load-bearing number for a storage-tidiness reason, and
  turns AC-1.5 from something the type system enforces into something a test has to catch. The cost is
  paid on the completion path, which gains nothing from the change.
- **A separate `FeatureStartForecast` entity with its own repository**, decoupled from `Feature`'s
  lifecycle. Most room for the deferred snapshot. **Rejected** — `SetFeatureForecasts` already owns a
  clear-and-rewrite lifecycle, and a second one managed by hand would need keeping in step with it
  forever. The snapshot does not need this: it needs its own forward-only table, which it would need
  either way.
- **A new lean `StartForecast` type** carrying only the histogram and `TeamId`. Cleaner domain
  modelling, no meaningless `NumberOfItems`. **Rejected** — it duplicates `ForecastBase`'s percentile
  and likelihood reads, or inherits from it and gains the same fields anyway. ADR-111 rejected the
  mirror of this ("drop the fields from the aggregate's contract entirely") as disproportionate.
- **Do not persist the Feature-grain row; recompute it from the per-team rows on read.** Half the
  storage. **Rejected** — it is precisely what ADR-199 decided against. Recomputing means
  `1 - Π(1 - Fᵢ(d))` over the marginals, which reintroduces the independence assumption the per-trial
  observation exists to avoid. Persisting is what makes the stored number the observed one.

## Consequences

- **Positive**: AC-1.5 holds by construction. No reviewer has to verify that a filter is still correct.
- **Positive**: the two collections are structurally identical, so if ADR-156 is ever un-deferred and
  `Forecasts` gains its own `TeamId == null` aggregate row, the two shapes match rather than diverging.
  Reconciled deliberately, not by coincidence.
- **A second collection to keep in step.** `SetFeatureForecasts` gains a sibling, and a refresh that
  wrote one without the other would leave a Feature with a start and a stale completion or vice versa.
  Both are written in the same `ForecastFeatures` pass; a test pins that they are cleared and rewritten
  together.
- **Row count roughly doubles** for the forecast tables: one start row per contributing team plus one
  per Feature, against one completion row per contributing team. Both are rewritten per refresh rather
  than accumulated, so this is a steady-state size, not growth.
- **Reuse verdict**, as amended: `WhenForecast` → **NOT REUSED** (its single required `FeatureId` is
  already bound to `Feature.Forecasts`). `ForecastBase` → **REUSED AS IS, by inheritance**.
  `StartForecast` → **NEW**, one type, a TPH sibling. `Feature` → **EXTEND** (one collection, one setter
  mirroring `SetFeatureForecasts`). `LighthouseAppContext` → **EXTEND** (one `HasMany`, one `HasOne` for
  the nullable team relationship, alongside the existing pair rather than duplicating it over the same
  foreign key). `FeatureRepository` → **EXTEND** (`GetFeatures()` eager-loads `Forecasts` and must load
  `StartForecasts` the same way, or the collection reads back empty everywhere).
  `AggregatedWhenForecast` → **NO CHANGE**, and that is the point.
- Cross-refs [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) (what is stored),
  [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (the null-team aggregate shape this
  adopts), [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the declared future of
  `Feature.Forecasts`, reconciled here),
  [ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) (what is read back).
