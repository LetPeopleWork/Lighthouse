# ADR-111: An aggregated feature forecast carries no owning team, sums its contributors' items, and reports its oldest contributor's timestamp

- **Status**: Accepted
- **Date**: 2026-07-27
- **Feature**: epic-5459-multi-team-forecasts (ADO Epic 5459, Story 5569)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

`WhenForecast` carries four fields that describe the *provenance* of a forecast rather than its
distribution: `Team`, `TeamId`, `NumberOfItems`, `CreationTime`. For a per-team forecast these are
unambiguous — they come from the `SimulationResult` that produced it.

For the **aggregate**, today's code copies all four from whichever team won the `MaxBy(GetProbability(85))`
selection. [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) deletes that selection, so
there is no longer any team to copy from. The fields need a defined meaning rather than an accidental
one.

A consumer check across the backend and frontend was run before deciding:

- `WhenForecastDto` reads only `GetProbability`, `FilterApplied`, `ExcludedSummary` — none of the four.
- The only production references to `NumberOfItems` are its own definition in `WhenForecast.cs` and the
  copy in `AggregatedWhenForecast.cs`; the rest are EF migration column definitions.
- No production code reads `Team` or `TeamId` off `Feature.Forecast`. The frontend has no
  corresponding field (the `numberOfItems` hits in `LineRunChart.tsx` / `WorkItemAgingChart.tsx` are
  unrelated chart-local variables).
- `AggregatedWhenForecast` is **never persisted**. EF maps the per-team `Feature.Forecasts` collection
  (`LighthouseAppContext.cs:185-189`) and the `WhenForecast.Team` relationship
  (`:314-318`); the aggregate is constructed on read by the computed `Feature.Forecast` property.

So three of the four fields are effectively write-only on the aggregate. **`CreationTime` is the
exception** — `FeatureDto.cs:17` surfaces it as `LastUpdated`, which is user-visible.

## Decision

| Field | Value on the aggregate | Why |
|---|---|---|
| `TeamId` | `null` | No single team owns a joint forecast. Null is the honest answer; the relationship is `OnDelete(SetNull)` and nullable already. |
| `Team` | `null` | As above. |
| `NumberOfItems` | **sum** of contributors' `NumberOfItems` | The feature's actual remaining work across all contributing teams — a meaningful feature-level quantity, unlike one team's slice of it. |
| `CreationTime` | **oldest** contributor's `CreationTime` | Feeds `FeatureDto.LastUpdated`. Taking the newest would let a freshly-forecast team mask a stale one; the oldest errs conservative, which is the correct direction for a freshness signal. |

Contributors here means the same filtered set ADR-110 multiplies over.

## Alternatives considered

- **Keep copying all four from one contributor.** Smallest diff. **Rejected** — the selection being
  copied *from* is exactly what ADR-110 deletes, so this would require inventing a fresh arbitrary
  rule ("the first one", "the latest-finishing one") and every field would silently misattribute the
  forecast to one team.
- **Newest `CreationTime`.** Keeps `LastUpdated` closest to today's values and avoids a visible
  change. **Rejected** — it understates staleness in exactly the case that matters, where one team's
  forecast is old. In practice all contributors are forecast together in a single `ForecastFeatures`
  pass, so the two rules rarely differ; when they do differ, the difference is a real signal.
- **Drop the fields from the aggregate's contract entirely** (e.g. a separate read model without
  provenance fields). **Rejected as disproportionate** — `AggregatedWhenForecast` inherits them from
  `WhenForecast`, and removing them would fork the type hierarchy for no consumer benefit.

## Consequences

- **`FeatureDto.LastUpdated` may report an earlier timestamp** than today for multi-team features —
  by design. Single-contributor features are unaffected (min of one).
- **`NumberOfItems` becomes meaningful at feature level** for the first time; nothing reads it today,
  so this is latent value rather than a behaviour change.
- **A null `Team`/`TeamId` on a feature-level forecast is now a documented invariant**, not an
  accident. Any future consumer that wants "which team drove this date" must ask the per-team
  `Feature.Forecasts` collection, which still carries full provenance.
- **Reuse verdict**: `WhenForecast` → **EXTEND** (fields unchanged in shape; only the aggregate's
  population rule is defined). No new type.
- Cross-refs [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (the aggregation this
  accompanies), [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (the
  unknown state, which must also decide what these fields read as).
