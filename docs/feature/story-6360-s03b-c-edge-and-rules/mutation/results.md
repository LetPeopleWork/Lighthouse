# Mutation — Story #6360 S03b feature C (Work Items edge and rules), final gate

Run on frozen code, after the whole-candidate review of feature C (HEAD 25471dbe8, 2026-11-16). Stryker.NET 5.0.0.

## Bar

Gate from `CLAUDE.md`: 80 % or more on the production code feature C changed.

## Scope

| File | Change in feature C |
|---|---|
| `Modules/FlowMetrics/Application/Policies/TeamDataRefreshedTeamMetricsUpdateHandler.cs` | new: loads the Team of a `TeamDataRefreshed` event and updates its metrics |
| `Modules/WorkItems/Application/TeamDataService.cs` | changed: no longer calls `ITeamMetricsService`; saves the items and publishes `TeamDataRefreshed` |

Not mutated: `Program.cs`, which gained one handler registration line. Mutating the whole start-up file for one line
would bury the score under untouched code; the registration and its order are pinned by
`DomainEventHandlerOrderPinTest` and by the port scenario `Tests.Acceptance.WorkItems.TeamMetricsFollowTheTeamRefreshTest`.

C2 and C3 changed no production code (C2: module rules, C3: test moves).

## First run — `stryker.story6360c.json`

Whole-file `mutate` for both files, `perTest`, the Flow metrics and Work Items unit tests
(`Tests.Modules.FlowMetrics`, `Tests.Modules.WorkItems`), with `API.Integration`, `Integration.Containers` and
`Acceptance` left out. Report: `mutation-report.first-run.json`. Scope line: `1 total mutants will be tested`, after
`3 mutants got status NoCoverage`. Wall clock 2 m 38 s.

| File | Tested | Killed | Survived | No coverage | Timeout | Score |
|---|---|---|---|---|---|---|
| `TeamDataRefreshedTeamMetricsUpdateHandler.cs` | 3 | 0 | 0 | 3 | 0 | 0 % |
| `TeamDataService.cs` | 1 | 1 | 0 | 0 | 0 | 100 % |
| **Total** | 4 | 1 | 0 | 3 | 0 | **25 %** |

The handler had no unit test: only the port scenario under `Tests.Acceptance.WorkItems` reached it, and the config
leaves `Acceptance` out.

`TeamDataService.cs`: the one mutant is the removal of the `PublishAsync` call, killed by
`TeamDataServiceTest.ExecuteAsync_PublishesTeamDataRefreshedForTeam`. Its two log lines carry
`Stryker disable once all` and report as Ignored; Stryker reports the method-body block removal as Ignored too.

## Closed by this pass — kill round, `stryker.story6360c.killround.json`

Narrowed to the handler and to `Tests.Modules.FlowMetrics.Application.Policies`. Report:
`mutation-report.kill-round.json`. Scope line: `3 total mutants will be tested`. Wall clock 2 m 4 s.

New test class `Tests/Modules/FlowMetrics/Application/Policies/TeamDataRefreshedTeamMetricsUpdateHandlerTest.cs`,
shaped like its sibling `PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest`, and listed in
`FlowMetricsUnitTestPlacementTest` under `Application/Policies`:

- `HandleAsync_UpdatesMetricsForTheTeamCarriedByTheEvent` kills line 16 `team == null` → `team != null` and line 21,
  the removal of `UpdateTeamMetrics(team)`.
- `HandleAsync_TeamNoLongerExists_DoesNotUpdateMetrics` kills line 18, the removal of the early `return`.

| File | Tested | Killed | Survived | No coverage | Timeout | Score |
|---|---|---|---|---|---|---|
| `TeamDataRefreshedTeamMetricsUpdateHandler.cs` | 3 | 3 | 0 | 0 | 0 | 100 % |

## Final per-file figures

| File | Tested | Killed | Survived | No coverage | Timeout | Score |
|---|---|---|---|---|---|---|
| `TeamDataRefreshedTeamMetricsUpdateHandler.cs` | 3 | 3 | 0 | 0 | 0 | 100 % (kill round) |
| `TeamDataService.cs` | 1 | 1 | 0 | 0 | 0 | 100 % (first run) |
| **Total** | 4 | 4 | 0 | 0 | 0 | **100 %** (first run 25 %) |

## Accepted survivors

None.

## Verdict

Bar met: 100 % on the code feature C changed, no survivor left.
