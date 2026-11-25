# Mutation testing — Story 6504 (a Feature without child Work Items shows 0 of 0)

Run 2026-11-25 on HEAD `9e3e3f6f6` (base `019a3bf36`) plus the kill-round tests listed below, uncommitted.
Gate is 80 % kill rate per stack. Both stacks pass.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.1.0), final run | **92.82 %** | 181 | 168 | 13 | 0 | 12 m 04 s |
| Frontend (StrykerJS), final incremental run | **83.87 %** | 62 | 52 | 10 | 0 | 1 m 14 s |

Configs: `stryker.6504.backend.json`, `stryker.6504.frontend.json`, `vitest.stryker.mutation.ts`
(plus `stryker.6504.backend.round2.json`, the narrowed diagnostic round).
Backend runs from `Lighthouse.Backend.Tests/` (`dotnet stryker --config-file ../../docs/feature/story-6504-childless-feature-size/mutation/stryker.6504.backend.json`).
Frontend runs from `Lighthouse.Frontend/` after copying the two frontend files into that folder (the vitest
config path is resolved against the working directory); delete the copies afterwards.

## How the score moved

| run | score | killed / survived / no coverage | note |
| --- | --- | --- | --- |
| backend 1, as first written | 69.61 % | 126 / 38 / 17 | 15 m 10 s. Every survivor sat in code the story did not touch. |
| backend 2, narrowed filter | 64.64 % | 117 / 33 / 31 | 3 m 15 s. Diagnostic only: the narrow filter drops the `Tests.Models.*` forecast tests, so Feature.cs shows no coverage. Not a result. |
| backend 3, run 1's filter plus the new tests | 92.82 % | 168 / 13 / 0 | the final run |
| frontend 1 | 72.58 % | 45 / 17 | 5 m 42 s |
| frontend 2, incremental, after 3 tests | 83.87 % | 52 / 10 | 1 m 14 s |

## The feature's own lines

Every mutant on a changed line was killed in the final backend run (none survived, none uncovered):
`Feature.HasNoChildWorkItemsYet`, `Feature.IsOpenWithoutChildWorkItems`, `Portfolio.InfersDefaultFeatureSize`,
both guards in `GetFeaturesToExtrapolate` / `GetFeaturesToOverrideWithDefaultSize`, `FeatureDto:27`,
`FeatureListDto:55`, `PortfolioSettingDto:16`, `PortfolioExtensions:31`.
Frontend: all mutants of the changed lines in `FeatureSizeComponent` (44-45, 104-128 apart from styling, 152, 174, 200, 222, 243),
`featureWarningSentences`, `WarningsIndicator`, `FeatureProgressIndicator`, `PortfolioSettings`, `Feature.ts`,
`CreatePortfolioWizard`, `ConnectionDefaultFieldSelect` and the changed `ItemListManager` / `ProgressIndicator` logic were killed.
The gate therefore holds on the whole-file score and on the feature's own lines.

## Backend

Whole-file mutate of the six files. Mutants ignored by Stryker (`Ignored`, `CompileError`) are not counted.

| file | tested | killed | survived | changed members' survivors |
| --- | --- | --- | --- | --- |
| API/DTO/FeatureDto.cs | 20 | 17 | 3 | none |
| API/DTO/FeatureListDto.cs | 12 | 11 | 1 | none |
| API/DTO/PortfolioSettingDto.cs | 9 | 8 | 1 | none |
| API/Helpers/PortfolioExtensions.cs | 36 | 35 | 1 | none |
| Models/Feature.cs | 93 | 88 | 5 | none |
| Models/Portfolio.cs | 11 | 9 | 2 | none |

### Left out of `mutate`

- `Data/LighthouseAppContext.cs`: EF mapping of the new `InfersDefaultFeatureSize` column. Stryker has no unit test that reads a column through a
  model builder, and the integration suite that does is kept out of the mutant loop. The migrations and the model snapshot are generated.
- `Models/UsageData/UsageDataEventName.cs`: one enum member with a number; no logic to mutate. The reporter that emits it is mutated on the frontend.

### Accepted survivors (13), all in members the story did not change

| file:line | mutant | why it stays |
| --- | --- | --- |
| FeatureDto.cs:23 (x2) | `namedCycleTimes ?? []` remove left / remove right | Old code, not the story. The base class takes either value for what these tests read; the aging surfaces that read it sit in integration tests outside the loop. |
| FeatureDto.cs:37, FeatureListDto.cs:63 | `Forecast?.CreateForecastDtos(...) ?? []` remove right | Equivalent. `Feature.Forecast` is never null: a new Feature answers with four default forecasts (a probe test said so and was dropped), so the fallback cannot run. |
| PortfolioSettingDto.cs:44 | block removal of the `DataRetrievalSchema` assignment | Old code. It needs a Portfolio with a connection; only the integration suite builds one. |
| PortfolioExtensions.cs:89 | `dto.Id > 0` → `>= 0` | Equivalent. Id 0 is never in `existingIds`, so both give the same id. |
| Feature.cs:37 | statement removal in the links constructor | Old code, outside the story's lines. |
| Feature.cs:158 | `StartedDate ?? CreatedDate` swapped | Old code. A kill needs `FeatureStart` equality across both dates; left for the owner of the start forecast. |
| Feature.cs:181 (x2) | `(Team?.Id ?? TeamId)` operand swap / remove left | Old code. Equivalent whenever `Team` is set, which every fixture does. |
| Feature.cs:340 | statement removal in `ReplaceDependsOnReferences` | Old code, `internal`; covered through the reconciler tests in the integration suite. |
| Portfolio.cs:9 | `["Epic"]` default → `""` | Old code. Every caller sets the types. |
| Portfolio.cs:190 | statement removal in `UpdateFeatures` | Old code. Covered by service integration tests outside the loop. |

## Frontend

Line ranges taken from `git diff -U0 019a3bf36 HEAD`. `useDefaultFeatureSizingSavedReporter.ts` is whole-file.
`deliveryExportTable.ts:137` and `deliveryBarMarks.ts:49` each hold one object property and produced no mutant.

| file | tested | killed | survived |
| --- | --- | --- | --- |
| ProjectSettings/useDefaultFeatureSizingSavedReporter.ts | 13 | 10 | 3 |
| ProgressIndicator/ProgressIndicator.tsx | 14 | 11 | 3 |
| ProjectSettings/Advanced/FeatureSizeComponent.tsx | 8 | 5 | 3 |
| ProjectSettings/ModifyProjectSettings.tsx | 3 | 2 | 1 |
| FeatureListDataGrid/FeatureProgressIndicator.tsx | 4 | 4 | 0 |
| FeatureListDataGrid/columns.tsx | 3 | 3 | 0 |
| FeatureListDataGrid/WarningsIndicator.tsx | 2 | 2 | 0 |
| ItemListManager/ItemListManager.tsx | 4 | 4 | 0 |
| models/Portfolio/PortfolioSettings.ts | 4 | 4 | 0 |
| utils/features/featureWarningSentences.ts | 4 | 4 | 0 |
| models/Feature.ts | 1 | 1 | 0 |
| CreateWizards/CreatePortfolioWizard.tsx | 1 | 1 | 0 |
| ConnectionDefault/ConnectionDefaultFieldSelect.tsx | 1 | 1 | 0 |

### Accepted survivors (10)

| file:line | mutant | why it stays |
| --- | --- | --- |
| useDefaultFeatureSizingSavedReporter.ts:17 | `useRef(true)` → `false` | Equivalent in use. `settingsLoaded` always sets the ref before any save, so the starting value is never read. |
| useDefaultFeatureSizingSavedReporter.ts:24, :37 | `useCallback` dependency arrays | Equivalent. The dependencies only change what React keeps between renders; no observable output differs. |
| ModifyProjectSettings.tsx:128 | `parentSourcesSaved(settings)` removed | Not the story's line; it is the neighbour of the new `sizingSaved` call. The parent-source bookkeeping has its own spec outside this loop. |
| ProgressIndicator.tsx:61-63 (3) | IconButton `sx` margin, padding, colour | Presentation only. Pinning pixel values of an icon button in a test would break on any theme change and prove nothing about the "0 of 0" behaviour. |
| FeatureSizeComponent.tsx:118 (2), :124 | `sx` of the switch row and its grid cell | Presentation only, same reason. |

## Kill round (one, per stack)

New tests, all passing, none in an oracle file:

| stack | file | test |
| --- | --- | --- |
| backend | `API/DTO/FeatureDtoTest.cs` | `TheFeaturePayloadAndTheListRow_ReportEachTeamsRemainingAndTotalWork` |
| backend | `API/DTO/FeatureDtoTest.cs` | `TheFeaturePayloadAndTheListRow_NameTheTeamsThatCannotBeForecastInAlphabeticalOrderAndShowNoForecast` |
| backend | `API/DTO/FeatureDtoTest.cs` | `TheListRow_OfAForecastableFeature_ReportsTheUsualFourPercentiles` |
| backend | `API/DTO/FeatureDtoTest.cs` | `TheListRow_GivenForecastCreationTime_ReturnsDateAsUTC` |
| backend | `API/DTO/FeatureDtoTest.cs` | `TheFeaturePayloadAndTheListRow_ListOnlyThePortfoliosTheCallerMayRead` |
| backend | `Models/FeatureTest.cs` | `GetRemainingWorkForTeam_TeamHasNoWorkOnTheFeature_ReturnsMinusOne` |
| backend | `Models/FeatureTest.cs` | `ANewFeature_IsNotAParentFeatureAndNamesNoOwningTeam` |
| backend | `API/Helpers/PortfolioExtensionsTest.cs` | `Saving_settings_stores_the_baseline_dates_as_UTC_and_the_state_lists_trimmed` |
| backend | `API/Helpers/PortfolioExtensionsTest.cs` | `Saving_settings_without_baseline_dates_leaves_them_unset` |
| backend | `API/Helpers/PortfolioExtensionsTest.cs` | `Saving_cycle_time_definitions_keeps_known_ids_and_numbers_the_rest_after_the_highest` |
| backend | `API/Helpers/PortfolioExtensionsTest.cs` | `Saving_cycle_time_definitions_to_a_portfolio_with_none_numbers_them_from_one` |
| frontend | `FeatureListDataGrid/columns.warnings.test.tsx` | `sorts a Feature with no child Work Items yet as warned` |
| frontend | `ItemListManager/ItemListManager.test.tsx` | `leaves the input and the chips enabled unless the list is disabled` |
| frontend | `ProgressIndicator/ProgressIndicator.test.tsx` | `explains on its info button why the work could not be determined, beside the label` |

The backend tests close pre-existing weak spots in the DTO mapping and the settings sync that the oracle files do not
cover (the oracles pin `hasNoChildWorkItemsYet` and `infersDefaultFeatureSize` themselves). The three frontend tests
close the survivors that sat on lines the story changed (`columns.tsx:153`, `ItemListManager.tsx:38`, `ProgressIndicator.tsx:53,56`).

## Configuration notes

- **Coverage capture dominates the backend wall clock.** Of 12 to 15 minutes, the mutation itself took about one minute.
  The mutate filter does not shrink the number of tests captured; the test-case filter does. Narrowing it below the
  `Tests.Models.*` namespace dropped the Feature.cs forecast tests and produced false "no coverage" survivors.
- **Stryker.NET ignores line spans, so the six backend files run whole-file.** StrykerJS honours them.
- The backend filter excludes `Integration`, `Tests.Acceptance` and `Tests.Contracts`. The DefaultFeatureSizing and DTO oracles are
  service-level and sit inside it.
- The mutation-testing project skill is not present in this checkout (`.claude/skills/mutation-testing/`); the config shapes and this
  format follow the ServiceNow reference pair.
