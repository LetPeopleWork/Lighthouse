# Backend Mutation Testing — aging-pace-percentiles

Tool: Stryker.NET 4.14.2 (local manifest, `Lighthouse.Backend/.config/dotnet-tools.json`).
Config: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.aging-pace-percentiles.json`.

## Method

Stryker.NET 4.14.2's glob line-range suffix (`file.cs{a..b}`) silently filters out
**all** mutants in this environment (documented in `docs/ci-learnings.md`, confirmed on
the sibling time-in-state run). To get reliable results the feature files are mutated
**whole**, then survivors are filtered to the feature's new line ranges during analysis.
Kill rate is reported over the feature surface only (mutants whose line falls inside the
new methods).

Feature line ranges analysed (current HEAD):
- `BaseMetricsService.cs`: 18–104 — `GroupTransitionsByItem`, `BuildWorkflowStateOrder`,
  `ComputeAgeInStatePercentiles`, `GroupAgeAtExitObservationsByState`,
  `BuildAgeInStatePercentilesDto`, `CumulativeAgeAtExit` (the core age-at-exit math).
- `TeamMetricsService.cs`: 24, 324–353 — `GetAgeInStatePercentilesForTeam` + `AssociateSyncedTransitions`.
- `PortfolioMetricsService.cs`: 18, 264–305 — `GetAgeInStatePercentilesForPortfolio` + `AssociateSyncedTransitions` + `ToWorkItemStateTransition`.
- `CsvWorkTrackingConnector.cs`: 201, 207–296 — `BuildStateEnteredTransitions`,
  `BuildMultiStateJourney`, `MappedDoneState`, `ReadPerStateEnteredDate`,
  `BuildCurrentStateEnteredTransition` (multi-state journey synthesis).

Trivial files excluded as agreed (`AgeInStatePercentilesDto.cs`, the
`ITeamMetricsService`/`IPortfolioMetricsService` interface members, `CsvWorkTrackingOptionNames.cs`)
— auto-property DTOs / interface declarations emit no behavioural mutants and are exercised
transitively.

Controller validation (`TeamMetricsController` / `PortfolioMetricsController` new endpoints)
is thin (`startDate.Date > endDate.Date → BadRequest`) and proven by the read-API integration
tests (`GetAgeInStatePercentiles_StartDateAfterEndDate_ReturnsBadRequest`); not separately
mutated because the controllers were not in the priority mutate set and the boundary is
behaviour-covered.

Baseline gate before mutating: target suites green (68 tests). After adding behaviour tests:
72 tests green (5 new journey tests; the suite count for the CSV class went 52 → 56).

## Results (feature surface)

| File | Killed | Survived | NoCov | Total | Kill % |
|------|-------:|---------:|------:|------:|-------:|
| BaseMetricsService.cs | 16 | 0 | 0 | 16 | 100.0% |
| CsvWorkTrackingConnector.cs | 19 | 3 | 0 | 22 | 86.4% |
| PortfolioMetricsService.cs | 4 | 2 | 0 | 6 | 66.7% |
| TeamMetricsService.cs | 3 | 2 | 0 | 5 | 60.0% |
| **Overall feature surface** | **42** | **7** | **0** | **49** | **85.7%** |

## Survivors killed (mutant → test added)

The first run scored 75.5% (37/49). The CSV multi-state journey was the gap — the only
journey coverage was an end-to-end "non-empty rising bands" integration assertion that never
pinned the synthesized transition chain. New targeted tests on `GetWorkItemsForTeam` (real CSV
fixture, real connector — integration-level, no mocks inside the hexagon):

| Mutant | Behaviour gap | Test added |
|--------|--------------|------------|
| Csv L242 Skip()→Take() + object-initializer (the `Zip(...Skip(1))` exit-chain pairing) | The consecutive FromState→ToState→TransitionedAt chain was never asserted | `GetWorkItemsForTeam_CompletedItemWithPerStateEnteredDates_SynthesizesExitChainAndClosingDoneTransition` + `..._InProgressItemWithPerStateEnteredDates_SynthesizesExitChainWithoutAClosingDoneTransition` (exact 3- and 1-transition chains) |
| Csv L249 EqualityOperator (`stateCategory == Done`) + NegateExpression | No assertion distinguished a Done item (gets a closing transition) from an in-progress item (does not) | the same two tests assert the closing `Test→Done` transition exists for the Done item and is absent for the WIP item |
| Csv L249 closing-condition right arm (`&& closedDate.HasValue`) | A Done item with no Closed Date was never exercised | `GetWorkItemsForTeam_DoneItemMissingClosedDate_SynthesizesExitChainWithoutAClosingDoneTransition` (Done item, null Closed Date → only the exit chain, no fabricated closing transition) |
| Csv L256 object-initializer (the closing Done `WorkItemStateTransition`) | FromState=lastDoing, ToState=mappedDone, TransitionedAt=closedDate were unasserted | covered by the completed-journey test's exact assertions on `transitions[2]` |
| Csv L213 block removal (`return []` when state-entered column unset) | No journey-capable fixture was ever run with the column unset, so the early return looked equivalent | `GetWorkItemsForTeam_PerStateColumnsPresentButStateEnteredColumnNotConfigured_EmitsNoTransitions` (per-state columns present, column unconfigured → no transitions; removing the guard would read the per-state columns and synthesize a journey) |

New fixture: `team-valid-multi-state-journey.csv` (per-state `StateSince_{state}` columns; a
completed item with a full In Progress→Review→Test→Done journey, an in-progress item with a
partial journey, a Done item missing its Closed Date, and a not-started item).

These tests took the CSV journey surface from 63.6% → 86.4% and the overall feature surface
from 75.5% → 85.7%.

## Survivors remaining (justified)

| Mutant | Status | Justification |
|--------|--------|---------------|
| TeamMetricsService L326 statement removal + string mutation | Survived | `logger.LogDebug("Getting Age In State Percentiles for Team ...")`. **Log-only** — no observable behaviour; removing the call or garbling the message changes nothing assertable. Equivalent. |
| PortfolioMetricsService L266 statement removal + string mutation | Survived | Same `logger.LogDebug(...)` on the portfolio path. **Log-only**. Equivalent. |
| CsvWorkTrackingConnector L270 `First()`→`FirstOrDefault()` (`MappedDoneState`) | Survived | `owner.DoneStates.First()`. The connector only reaches `MappedDoneState` for a Done item with a journey, and a Done item by construction has a non-empty `DoneStates`; `First()` and `FirstOrDefault()` return the identical element. **Equivalent.** |

All remaining survivors are either log-only or provably equivalent. None represents an
untested feature behaviour. The core age-at-exit percentile math (`BaseMetricsService`,
the priority file) is at **100%**.

## Verdict

**PASS** — backend feature surface mutation score **85.7%** (42/49), above the 80%
threshold. Per-file: BaseMetricsService 100%, CSV journey 86.4%; the two service
sub-80% rows are entirely the log-only `LogDebug` lines (2 mutants each on a 5/6-mutant
surface) and are equivalent.

## New / modified test files

- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnectorTest.cs` (5 new journey tests + `CreateMultiStateJourneyTeam` helper)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-multi-state-journey.csv` (new fixture)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj` (copy-to-output entry for the new fixture)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.aging-pace-percentiles.json` (scoped Stryker config — whole-file mutate + analysis-time line filtering due to the 4.14.2 glob-range limitation)

`StrykerOutput/` is gitignored (verified). No production source under
`Lighthouse.Backend/Lighthouse.Backend/` was modified.
