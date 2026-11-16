# Slice 22 — S07b `BaseMetricsService` inheritance → composition

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Team and Portfolio metrics share behaviour through composed collaborators, not a base class.

## IN

- Extract the shared parts of `BaseMetricsService` into named collaborators (calculators, queries, cache) and delete the base class.
- Remove `TeamMetricsService`'s S107 suppression if the split allows it, or leave that to S07d.

## OUT

- Controller logic (S12a). Read ports (S05f).

## Hypothesis

The base class holds a few cohesive concerns. **Disproved if** Team and Portfolio diverge so much that sharing is wrong. Then duplicate deliberately ("don't repeat knowledge, not code").

## Acceptance criteria

- [ ] `BaseMetricsService` no longer exists. Team and Portfolio services compose collaborators.
- [ ] Metrics responses unchanged. Cache invalidation via the domain-event handlers is unchanged.
- [ ] Mutation score held.

## Dependencies

- Soft: S02, S05f.

## Notes for DESIGN

The frontend mirror (FE4's `BaseMetricsService` split) should follow the same shape.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

## Update after S03b (#6360, 2026-11-16)

A Team's metrics are now updated by the Flow metrics reaction `TeamDataRefreshedTeamMetricsUpdateHandler`, registered first among the `TeamDataRefreshed` handlers; `TeamDataService` no longer calls `ITeamMetricsService`. `UpdateTeamMetrics` still saves through `IWorkItemRepository.Save` (left for this Story). Since the update became a reaction, a failed metrics save is caught and logged by the dispatcher: the refresh succeeds, the Team's `UpdateTime` and `FeatureWIP` keep their old values so the Team is due again, and the other reactions run. The maintainer accepted this. Any change to how metrics are composed or saved must keep `TeamMetricsFollowTheTeamRefreshTest` (including its failing-save scenario) and `DomainEventHandlerOrderPinTest` green.
