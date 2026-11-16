# Slice 21 — S07a Split `WorkItemService` along use cases

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Break `WorkItemService` (20+ dependencies, S107-suppressed) into use-case classes, each under the S107 threshold.

## IN

- Split along the use cases visible today: refresh Work Items, sync state transitions, refresh remaining work, sync Feature state transitions, and any other DESIGN finds.
- Remove its `#pragma warning disable S107`.
- Restate the seam rules that name it (e.g. "sole writer of transitions") for the successor type(s).

## OUT

- Moving it into the Sync folder (S03b). Persistence ports (S05*).

## Hypothesis

The dependencies cluster by use case. **Disproved if** most dependencies are needed by every use case. Then the real issue is a missing collaborator, which is extracted instead.

## Acceptance criteria

- [ ] No S107 suppression on the successors. No parameter objects, no `[FromServices]`.
- [ ] Seam ArchUnit rules preserved for the successor types.
- [ ] Behaviour-free (sync outcomes identical), domain events published identically. Mutation score held.

## Dependencies

- Soft: S05a, and ideally before S03b.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one use case per slice).

## Update after S03b (#6360, 2026-11-16)

`WorkItemService` keeps two query edges into Flow metrics for the default-size guess: `IPortfolioMetricsService` and the Flow metrics Domain's `PercentileCalculator`. They are ratcheted by name in Flow metrics' R3 and R4 lists and in Work Items' own outbound list (`WorkItemsModuleBoundariesArchUnitTest`). Splitting the service is the place to remove them (or Epic 6474 slice 10, whichever comes first); every list compares both ways, so each removed edge must leave its entry in the same commit.

## Picked up from S06 (#6376, 2026-11-16)

- `WorkItemService` still has private methods whose `Portfolio` parameter is named `project` (`RefreshRemainingWork`,
  `GetExtrapolatedRemainingWork`, `GetDefaultRemainingWork`, `RefreshParentFeatures`). S06 renamed the connector port
  only; rename these as the split moves them.
- Two of S06's capability oracles sit apart from their sibling: `FeatureSourceCapabilityArchUnitTest` and
  `WriteBackTargetCapabilityArchUnitTest` are under `Lighthouse.Backend.Tests/Architecture`, while
  `IncrementalSourceCapabilityTest` is under `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors`.
  Bring them into one folder as a plain refactor commit (`git mv` and the namespace line), no assertion changed.
