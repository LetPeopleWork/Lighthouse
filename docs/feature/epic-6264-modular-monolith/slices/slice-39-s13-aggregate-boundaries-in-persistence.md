# Slice 39 — S13 Aggregate boundaries in persistence

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L** · Pick-up group G4b (after S08's catalogue; before or alongside S10)

## Goal

S08 decides the aggregates, and S13 makes persistence follow them. An aggregate load pulls in only its own data, never another bounded context's. Forecasts on Feature is the pilot. This is the one Story in the Epic with a measurable performance payoff.

## Problem (verified in code, 2026-10-22)

- Forecasts already have their own tables. `ForecastBase` holds both `WhenForecast` and `StartForecast` rows, and `IndividualSimulationResult` holds one row per outcome bucket.
- But they are modelled as child collections of the Feature aggregate. `Feature.Forecasts` and `Feature.StartForecasts` are cascade FKs configured in `Data/LighthouseAppContext.cs` (≈ l.241–281). Feature also carries forecast logic (`GetLikelhoodForDate`, `AggregatedWhenForecast`).
- So every Feature load drags in its whole forecast history. `Services/Implementation/Repositories/FeatureRepository.cs:59-66` includes Portfolios, FeatureWork→Team, Forecasts→SimulationResults, StartForecasts→SimulationResults and DependsOnReferences. `PortfolioRepository` and `DeliveryRepository` include Forecasts too.

## IN

1. **Catalogue**: every aggregate whose load pulls in data owned by another bounded context.
   - Forecasts on Feature (the pilot).
   - FeatureWork→Team.
   - The Portfolio and Delivery includes.
   - Whatever S08's catalogue finds.
2. **Pilot**:
   - Forecast becomes its own aggregate in the Forecasting context, referencing `FeatureId`.
   - Feature stops navigating to it.
   - Forecast logic (`GetLikelhoodForDate`, `AggregatedWhenForecast`) moves to the Forecasting side.
   - Readers go through a forecast read port.
   - The tables already exist, so this is mostly a code change. Any schema change is expand-only (additive per release, never destructive) via `CreateMigration`.
3. **Measure each candidate before and after**: rows loaded and query time for a Feature load and a Portfolio load. Use the dev database restored from a production-like backup (`Restore-DbBackup.ps1`) and EF query logging plus timing. The KPI baseline is measured at this Story's DESIGN, and the target is set there.

## OUT

- **Removing FKs or cascades.** They stay (D-6264-13): deleting a Feature still deletes its forecasts.
- Ownership documentation and the owner-only-writes rule (S10, which builds on this Story).
- Any change to forecast computation (Monte-Carlo stays as is).

## Hypothesis

Forecast rows dominate the cost of a Feature or Portfolio load, so cutting the navigation measurably reduces rows loaded and query time with identical results. **Disproved if** the before/after measurement shows no meaningful difference. Then record that, keep the boundary change for modelling reasons only if it is cheap, and do not pursue further candidates for performance.

## Acceptance criteria

- [ ] Catalogue of cross-context includes, each with a before measurement.
- [ ] Given the pilot is done, when a Feature or Portfolio is loaded, then no forecast or simulation-result rows are loaded unless a forecast reader asks for them.
- [ ] `Feature` has no forecast navigation and no forecast logic. Forecasts are read via the forecast read port.
- [ ] Forecast results byte-identical. API, DTO shapes and E2E unchanged. Mutation score held.
- [ ] After measurements recorded next to the before, and the KPI updated.
- [ ] FKs and cascades unchanged. Any schema change is expand-only.

## Dependencies

- Hard: S08 (aggregate catalogue).
- Soft: S03c (the Forecast aggregate's home in the Forecasting context), S05d (Portfolio ports).
- Related: S10 (before or alongside it). S13 removes the cross-context navigation that S10's rule then forbids.

## Notes for DESIGN

- Find **every reader** of `Feature.Forecasts` / `StartForecasts` before removing the navigation. A caller that relied on the include would otherwise silently see an empty list. Candidates include DTO mapping (`FeatureDto`, so coordinate with S12c), Delivery forecast publishing, Portfolio and Delivery views, and the demo data.
- Measure on the restored dev instance (real history), not on demo or InMemory data, because InMemory translates nothing.
- Forecast writes happen in `ForecastUpdater`. Keep the write and the after-commit domain events (`PortfolioForecastsUpdated`) unchanged.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN, e.g.:

1. Baseline measurement + catalogue.
2. Forecast read port beside the navigation.
3. Readers moved.
4. Navigation removed.
5. After measurement.
6. Further candidates as their own slices.
