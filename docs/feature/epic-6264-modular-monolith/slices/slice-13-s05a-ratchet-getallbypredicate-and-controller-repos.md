# Slice 13 — S05a Ratchet guard: `GetAllByPredicate` callers + controllers on repositories

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S** · Pick-up group G0 (independent, can go first)

## Goal

Stop the generic-repository leak from growing, and turn its removal into a list that only shrinks.

## IN

- An ArchUnitNET guard listing, **as an inline file list (not a count)**, today's callers of `GetAllByPredicate` outside the persistence adapters. It fails when a new file calls it, and when a listed file no longer does. The failure names the file.
- Baseline, verified 2026-10-22: **14 files, 53 calls**. The brief said 12, missing ConnectionSaveService and WorkTrackingSystemConnectionUsage.
  - TeamMetricsService (14), PortfolioMetricsService (13), ConnectionSaveService (5), WorkItemService (4), FeaturesController (3), WorkTrackingSystemConnectionUsage (3), OverTimeFillTarget (2), DemoPercentilesBackfillHandler (2), RefreshLogService (2)
  - TeamMetricsController, BlockedCountSeriesBuilder, DemoBlockedHistoryBackfillHandler, RefinementList, WriteBackTriggerService (1 each)
- Sibling rule in the same guard: **controllers may not depend on `IRepository<>` / `I*Repository`**, baselined the same way. The brief said 5. A single-line constructor-parameter search on 2026-10-22 found about 20: Deliveries, DeliveryNotes, DeliveryRules, DeliverySources, Demo, Features, Forecast, ForecastRealityCheck, OptionalFeatures, Portfolio, PortfolioMetrics, Portfolios, Suggestions, Team, TeamFeatureSizeMetrics, TeamMetrics, Teams, Wizards, WorkTrackingSystemConnection, WorkTrackingSystemConnections. DESIGN re-counts exactly.

## OUT

- Migrating any caller (S05b–g, S12*).

## Hypothesis

A file-naming ratchet is enough to stop growth without slowing feature work. **Disproved if** contributors routinely have to add files to the list. That would mean a missing port, and the port gets created instead.

## Acceptance criteria

- [ ] Given a file outside the persistence adapters that is not on the list calls `GetAllByPredicate`, when the architecture tests run, then they fail naming the file.
- [ ] Given a listed file stops calling it, then the guard fails until the file is removed from the list.
- [ ] The same two behaviours for the controller-on-repository rule.
- [ ] The guard's lists match the verified baselines at the time of the PR.

## Dependencies

None.

## Notes for DESIGN

- "Persistence adapters" means `Services/Implementation/Repositories/**`, and later the S01 persistence namespace.
- The rule matches by member call, so overrides inside repositories (`FeatureRepository`, `WorkItemRepository`) are excluded by namespace.

## Slicing

Carpaccio slicing happens at this Story's DESIGN. Likely one slice per rule.
