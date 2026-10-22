# Slice 24 — S07d Remove the remaining S107 suppressions on services

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

No non-API production type suppresses S107 (too many constructor parameters). Each one is removed by splitting, never by bundling.

## IN

The verified list (2026-10-22, re-check at start), 9 types:

- WorkItemService (S07a)
- TeamMetricsService (S07b)
- ForecastRunPlan
- OAuthService
- ConnectionHealthService
- DemoDataService
- WorkTrackingAuthStrategyFactory
- BlockedCountSnapshotRecordingHandler
- DemoBlockedHistoryBackfillHandler

**Correction to the brief**: StubOAuthProvider suppresses S1075 (a hard-coded URI), not S107, so it is not in scope.

## OUT

- API controllers and DTOs (S12a/b/c).

## Hypothesis

Each class does more than one job. **Disproved for a class** if every parameter is a genuinely distinct seam of one cohesive job (e.g. `ForecastRunPlan`, an immutable plan). Then record why and keep a narrow suppression with an inline plain-language justification.

## Acceptance criteria

- [ ] `grep -rln "disable S107" --include=*.cs Lighthouse.Backend/Lighthouse.Backend` lists only API types, or justified exceptions recorded in this Story.
- [ ] No parameter objects, no `[FromServices]`. Behaviour-free, mutation score held.

## Dependencies

- Soft: S07a, S07b (they remove two).

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one type per slice).
