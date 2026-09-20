# ADR-200: Start forecasts live in their own collection on Feature, not alongside completion forecasts behind a discriminator

- **Status**: Accepted
- **Date**: 2026-09-20
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, Story #6045)
- **Deciders**: Benjamin Huser-Berta (maintainer)

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

**A second collection, `Feature.StartForecasts`, holding `WhenForecast` rows of the same shape.**

1. **Per-team rows carry their `TeamId`. The Feature-grain row carries `TeamId == null`**, exactly as
   ADR-111 defines for an aggregate: no single team owns it, null is the honest answer, and the
   relationship is already nullable with `OnDelete(SetNull)`.

2. **`Feature.Forecast` is not touched.** AC-1.5 — the completion forecast is unchanged before and
   after this Epic — becomes a structural property rather than a test. `AggregatedWhenForecast` cannot
   see a start row, because start rows are not in the collection it reads. A filter would make the same
   guarantee conditional on nobody later removing it.

3. **`WhenForecast` is reused as-is, not subclassed or forked.** It carries the histogram through
   `ForecastBase`, whose `GetProbability` and ascending `KeyOrder` are exactly what a start percentile
   needs. `NumberOfItems` reads as ADR-111's sum and is meaningless for a start; that is tolerable for
   the same reason ADR-111 tolerated it on the aggregate — nothing reads it, and forking the type
   hierarchy for one unused field is disproportionate.

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
- **Reuse verdict**: `WhenForecast` → **REUSED AS IS** (no new fields, no subclass).
  `ForecastBase` → **REUSED AS IS**. `Feature` → **EXTEND** (one collection, one setter mirroring
  `SetFeatureForecasts`). `LighthouseAppContext` → **EXTEND** (one `HasMany`, one `HasOne` for the
  nullable team relationship, mirroring the existing pair). `AggregatedWhenForecast` → **NO CHANGE**,
  and that is the point. **No new type.**
- Cross-refs [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) (what is stored),
  [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (the null-team aggregate shape this
  adopts), [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the declared future of
  `Feature.Forecasts`, reconciled here),
  [ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) (what is read back).
