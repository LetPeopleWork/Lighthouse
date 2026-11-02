# ADR-276: One refresh per unit, with steps chosen by level and one rule for when a unit is not refreshed

**Status**: Proposed (Epic #6474 DESIGN, application part, 2026-11-02; maintainer decisions MA3, MA6, MA8 applied).
Promoted to `docs/product/architecture/` with slice 12.
**Context map**: Work Items (owner of the refresh), Forecasting (forecast after a Feature-level refresh), Organisation
set-up (levels, units). Related: ADR-027, ADR-273, ADR-274, ADR-275, maintainer decisions M13, MD5, MD7.

## Context

`TeamUpdater` and `PortfolioUpdater` are two `UpdateServiceBase<T>` background services, each with its own schedule
loop, refresh settings (`GetTeamDataRefreshSettings`, the Feature refresh settings), queue key (`UpdateType.Team`,
`UpdateType.Features`, `UpdateType.Forecasts`) and free-tier check (`TeamUpdater` skips when more than 3 Teams exist
without Premium). The Portfolio pipeline calls the forecast directly. Every refresh change is made twice.

This Epic adds two reasons not to refresh a unit: its level is switched off (M13) and the unit needs item types
(MD7). Without one place to decide, each would be checked in two updaters, the manual "Update" endpoints and the
forecast triggers.

## Decision

1. **One `UnitUpdater` hosted service** replaces the two schedule loops. It walks the units of every switched-on
   level and runs the **steps of the unit's level**: the leaf's (fetch Work Items, write-back staging, refresh log,
   `TeamDataRefreshed`) or the Feature level's (fetch Features, roll-up and default sizing, deliveries, refresh log,
   `PortfolioFeaturesRefreshed`, then the forecast). Steps are strategy objects chosen by `Level` role, not by
   `is Team`.
2. **Queue keys, refresh settings and event names stay as they are**: the leaf uses `UpdateType.Team` and the Team
   refresh settings, the Feature level `UpdateType.Features` and the Feature refresh settings; the forecast stays on
   `UpdateType.Forecasts`. Coalescing (park and run once more) is the queue's, unchanged.
3. **One pure rule decides whether a unit refreshes**: `UnitRefreshEligibility.For(unit, level, licence, counts)`
   returns `Refresh` or `Skip(reason)` with reasons `LevelSwitchedOff`, `NeedsItemTypes`, `FreeTierExceeded`. The
   free-tier limits are a table per level (leaf 3, Feature level 1, each `count > limit`), today's two rules unchanged. The
   schedule loop, the manual update endpoints (which answer 409 with the reason), the forecast triggers and the
   catch-up after `LevelSwitchedOn` all ask it. A skipped unit keeps its data and its `UpdateTime`.
4. **`ITeamUpdater` and `IPortfolioUpdater` stay as thin facades** over `IUnitRefresh.TriggerUpdate(UnitId)` for
   their many callers in this Epic; a ratchet counts their callers so later work removes them.
5. **The forecast after a Feature-level refresh moves to a handler of `PortfolioFeaturesRefreshed`** only if slice
   12b shows the same event order; otherwise the direct call stays and is recorded as left (DISCUSS slice 12).
6. **Delivered as one slice (12), kept whole by the maintainer and flagged as over a day.** It is gated by slice 01's
   pinned numbers and an event-order test. Every step of both pipelines is pinned (leaf: fetch, write-back staging,
   item count, refresh log, summary, connection health, `TeamDataRefreshed`; Feature level: free-tier check, fetch,
   roll-up and default sizing, delivery recomputation, large-portfolio warning, orphaned-Feature clean-up, refresh
   log, summary, connection health, `PortfolioFeaturesRefreshed`, forecast with its fingerprint skip,
   `PortfolioForecastsUpdated`).

## Consequences

- #6455 adds a level by adding its steps, not a third updater.
- The free-tier skip, the switched-off level and the paused unit are tested once, as a pure function, plus one
  wiring test per caller.
- Units on a switched-off level still count towards the free tier (MD5) because the rule counts units, not refreshed
  units.
- One hosted service instead of two changes start-up timing slightly; the demo-data E2E, the only check that sees
  some refresh races, runs on 12a and 12b.

## Alternatives considered

- **Two updaters behind one entry point.** The fallback if 12a or 12b cannot reproduce event order; rejected as the
  target because every change would still be made twice.
- **A configurable pipe-and-filter engine.** Rejected: two fixed step lists do not need a runtime-configured
  pipeline.
- **One `UnitDataRefreshed` event now.** Rejected for this Epic (domain part's recommendation): six handlers would
  move for no behaviour gain; #6455 takes it when a level-3 refresh needs it.
- **Checking the switch and the flag inside each step.** Rejected: four callers would each have to remember them.
