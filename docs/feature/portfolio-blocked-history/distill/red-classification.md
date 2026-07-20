# DISTILL Red Classification — portfolio-blocked-history

**Feature**: portfolio-blocked-history (ADO #5524)
**Date**: 2026-07-20
**Status**: PENDING — `dotnet test` not run (no runtime available in DISTILL subagent environment)

---

## Pre-DELIVER fail-for-the-right-reason gate

Per ADR-025 D2, DISTILL produces scaffolded RED acceptance tests with `[Ignore]` markers. DELIVER's RED phase unskips the scaffolds and classifies each failure as `MISSING_FUNCTIONALITY` (valid RED) or `INFRA_BROKEN` (wrong RED — fix before proceeding).

### Classification procedure (to be executed by DELIVER at RED phase)

1. Run `dotnet test --filter "Category=acceptance&Category=portfolio-blocked-history"` in `Lighthouse.Backend/Lighthouse.Backend.Tests/`
2. For each FAIL, classify:
   - `MISSING_FUNCTIONALITY`: assertion fires because implementation is absent (correct RED)
   - `INFRA_BROKEN`: test fails on setup error, import error, or fixture bug (wrong RED)
   - `WRONG_ASSERTION`: assertion couples to internal struct (fix Universe/assertion)

### Expected classification per slice

| Slice | Scaffold file | Test count | Expected RED class | Notes |
|---|---|---|---|---|
| 01 | `Slice01DemoBackfillTeamHistoryScenarios.cs` | 5 | 3× MISSING_FUNCTIONALITY, 1× REGRESSION_GUARD, 1× REGRESSION_GUARD | The regression-guard tests (`The_demo_portfolio_blocked_trend_still_renders_its_backdated_history`) should be GREEN at authoring — they assert existing working behavior. The `Backdated_portfolio_snapshots_land...` test was previously red (FK-bite branch) and may now be green if the backfill was already fixed. |
| 02 | `Slice02FeatureBlockedCaptureScenarios.cs` | 5 | 4× MISSING_FUNCTIONALITY, 1× REGRESSION_GUARD | `The_scope_free_team_feature_list...` is a regression guard (GREEN at authoring). All others are RED — capture seam is unimplemented. |
| 03 | `Slice03HistoricPortfolioBlockedCountScenarios.cs` | 6 | 5× MISSING_FUNCTIONALITY, 1× REGRESSION_GUARD | The parity-matrix test (`The_same_spell_shape_answers_identically...`) may be a regression guard. All others assert the new historic read branch. |
| 04 | `Slice04PortfolioBlockedDrillThroughScenarios.cs` | 5 | 5× MISSING_FUNCTIONALITY | All assert new reconstruction logic. `The_latest_bar_reconstructs_from_the_live_blocked_set` relies on live branch which exists today. |
| 05 | `Slice05DemoPortfolioBlockedHistoryScenarios.cs` | 7 | 5× MISSING_FUNCTIONALITY, 2× REGRESSION_GUARD | `The_demo_backfill_writes_nothing_into_the_team_keyspace` (invariant guard, may be GREEN) and `A_non_demo_portfolio_gains_no_backdated_spells` (edge guard). |

### Known pre-conditions for PASSING classification

1. **SQLite must be available**: These tests use real SQLite via `EnsureDeleted()` / `EnsureCreated()`. If the test runner cannot create/delete a SQLite database, ALL tests will fail with `INFRA_BROKEN` — SQLite is mandatory per ADR-102 / DST-1.
2. **`FeatureBlockedTransition` entity must exist**: Slice 02+ tests reference `FeatureBlockedTransitions` DbSet. If the migration hasn't run, the database schema will be missing the table → `INFRA_BROKEN`. This is expected — the entity lives in slice 02's precursor commit.
3. **`IBlockedItemService` must resolve**: The refresh path calls `blockedItemService.IsBlocked(feature, portfolio)` — already implemented and tested in epic-5074.
4. **`PortfolioBlockedHistoryAcceptanceTest` base class must compile**: The base class references `FeatureBlockedTransition` (for `SeedFeatureBlockedSpell` and `ReadFeatureSpells`). If the entity doesn't exist yet, the base class won't compile → `INFRA_BROKEN`. The entity + migration should land as a **precursor commit** before any slice 02 test is unskipped.

### RED validation protocol (for DELIVER)

1. Unskip ONLY the first scenario in the slice (the `@walking_skeleton`).
2. Run `dotnet test --filter "FullyQualifiedName~Slice02FeatureBlockedCaptureTest.A_feature_that_becomes_blocked_shows_how_long_it_has_been_blocked"`.
3. Verify: **exactly one failure**, and the failure message indicates the assertion (`blockedSince` is null / spell row absent) rather than a setup error (SQLite connection, DI resolution, type load).
4. If `MISSING_FUNCTIONALITY` → proceed to GREEN.
5. If `INFRA_BROKEN` → fix the test infrastructure before starting implementation.

---

## Test file inventory

| File | Tests | `[Ignore]` state |
|---|---|---|
| `Slice01DemoBackfillTeamHistoryScenarios.cs` | 5 | `[Ignore]` commented-out (tests would run) |
| `Slice01DemoBackfillTeamHistorySpecifications.cs` | 12 methods | — |
| `Slice02FeatureBlockedCaptureScenarios.cs` | 5 | `[Ignore]` commented-out |
| `Slice02FeatureBlockedCaptureSpecifications.cs` | 13 methods | — |
| `Slice03HistoricPortfolioBlockedCountScenarios.cs` | 6 | `[Ignore]` commented-out |
| `Slice03HistoricPortfolioBlockedCountSpecifications.cs` | 14 methods | — |
| `Slice04PortfolioBlockedDrillThroughScenarios.cs` | 5 | `[Ignore]` commented-out |
| `Slice04PortfolioBlockedDrillThroughSpecifications.cs` | 11 methods | — |
| `Slice05DemoPortfolioBlockedHistoryScenarios.cs` | 7 | `[Ignore]` commented-out |
| `Slice05DemoPortfolioBlockedHistorySpecifications.cs` | 15 methods | — |
| `PortfolioBlockedHistoryAcceptanceTest.cs` | base class | — |

**Note on `[Ignore]` state**: The scenario files currently have `[Ignore]` attributes **commented out** (e.g., `// [Ignore("DISTILL scaffold — RED pending DELIVER...")]`). This means running `dotnet test` against these files would attempt to execute all 28 tests. DELIVER should:
1. Comment IN the `[Ignore]` on ALL scenarios except the first walking skeleton.
2. Unskip one at a time as implementation proceeds.
3. Re-comment-out `[Ignore]` on regression-guard tests that were GREEN at authoring — these should run as regression checks, not as RED→GREEN drivers.

---

## Placeholder — actual run results

*This section to be filled by DELIVER at RED phase entry.*

```
### Run: YYYY-MM-DD HH:MM
### Command: dotnet test --filter "Category=acceptance&Category=portfolio-blocked-history"

| Scenario | Status | Classification | Notes |
|---|---|---|---|
| ... | ... | ... | ... |
```
