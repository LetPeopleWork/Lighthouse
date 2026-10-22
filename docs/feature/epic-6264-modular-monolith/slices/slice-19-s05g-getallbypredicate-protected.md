# Slice 19 — S05g End state: `GetAllByPredicate` protected; ADR-163 generalised

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S** (plus remaining callers) · Pick-up group G4

## Goal

Let the compiler enforce what the S05a guard enforced, and write the narrowed-port rule down as the general rule.

## IN

- Migrate the remaining baseline callers (whatever is left after S05c–f), with the demo backfill handlers last. Possible residue: FeaturesController, TeamMetricsController, WorkItemService, OverTimeFillTarget, BlockedCountSeriesBuilder, RefreshLogService, WriteBackTriggerService, RefinementList, DemoPercentilesBackfillHandler, DemoBlockedHistoryBackfillHandler.
- Remove `GetAllByPredicate` from `IRepository<T>`. Make it `protected` on `RepositoryBase`.
- ADR-163 moves from Proposed to Accepted with a generalising note, or a new ADR.

## OUT

- Removing `IRepository<T>` for aggregates not covered by S05c–e. Record any residue as follow-ups.

## Hypothesis

Once the list is empty, protecting the method costs nothing. **Disproved if** tests rely on it. Then test builders get the named methods too.

## Acceptance criteria

- [ ] S05a `GetAllByPredicate` list empty. The guard is deleted, or kept as a belt-and-braces rule.
- [ ] Calling `GetAllByPredicate` outside a repository does not compile.
- [ ] ADR updated. New code uses intention-revealing methods (e.g. `FindDueForRefresh(today)`).

## Dependencies

- Hard: S05a–S05f and the remaining callers.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one caller group per slice).
