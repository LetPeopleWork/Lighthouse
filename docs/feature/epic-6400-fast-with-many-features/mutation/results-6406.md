# Mutation testing — 6406 (Epic 6400 steps 02-01 / 02-02: names and counts read no forecast)

Run 2026-10-26 against local `main` @ `f210018de` (tree clean, code frozen). Backend only; no frontend change.
Gate: 80 % kill rate on `PortfolioRepository.cs`. `PortfolioController.cs` is reported but triaged only for what
the steps changed (`ReadPortfolio`, `Get`, `GetPortfolioSettings`).

| file | run | scored | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- | --- |
| `Repositories/PortfolioRepository.cs` | 1: narrow filter | 34 | 8 | 3 | 23 | 23.53 % |
| | 2: filter widened (same code and tests) | 34 | 24 | 10 | 0 | 70.59 % |
| | 3: after the kill round | 34 | **28** | 6 | 0 | **82.35 %** |
| `API/PortfolioController.cs` | 1: narrow filter | 38 | 20 | 8 | 10 | 52.63 % |
| | 2: filter widened | 38 | 20 | 8 | 10 | 52.63 % |
| | 3: after the kill round | 38 | 24 | 4 | 10 | 63.16 % |

Scores count no-coverage mutants as not killed, as Stryker does. Compile-error and ignored mutants are not scored.
Stryker's "will be tested" line: 39 in run 1, 62 in runs 2 and 3 (of 22 742 created for the whole backend). The
`Program.cs` rows in the JSON report are ignored mutants and not part of the scope.

## Why the filter was widened

Run 1 used the filter asked for (`PortfolioRepositoryTest`, `PortfolioCatalogueReaderRelationalTest`,
`API.PortfolioControllerTest`). Most of `PortfolioRepository.cs` is older code (compare-and-set of the forecast inputs,
forget, `Remove`) whose unit tests live elsewhere, so 23 mutants showed as no coverage. Runs 2 and 3 add two more unit
classes, nothing from `API.Integration`: `ForecastConfirmationRepositoryRelationalTest` (the compare-and-set and
forget writes on SQLite) and `Repositories.DeletionTests` (`Remove`). Config: `stryker.6406.backend.wide.json`;
`stryker.6406.backend.json` is the narrow first run. Both end with the usual exclusions (`API.Integration`,
`Integration.Containers`, Dogfood, the live-connector categories).

## Run commands

From `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `StrykerOutput` removed before run 1, `nohup` from a foreground
shell, coverage analysis `perTestInIsolation`, concurrency 4:

```
dotnet stryker --config-file ../../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6406.backend.json
dotnet stryker --config-file ../../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6406.backend.wide.json
```

Durations: run 1 2 m 37 s, run 2 3 m 08 s, run 3 3 m 14 s (after the kill round, whole widened config re-run; with
runs this short a narrowed config bought nothing). `--since` / `--with-baseline` not used.

## Kill round

New tests, all green with the classes they sit in (70 tests across the four classes, build with 0 warnings):

- `PortfolioControllerTest.GetPortfolioSettings_NamesTheBlockedRuleSetThePortfolioIsEffectivelyUnder`: pins the
  `BlockedRuleSetJson` initializer of the settings read.
- `PortfolioControllerTest.GetPortfolioSettings_AsksWhichTeamsAreReadableForTheInvolvedTeamsThenTheOwningTeamUnderTheRequestsAbortToken`
  and `GetPortfolio_AsksWhichTeamsAreReadableUnderTheRequestsAbortToken`: the request's abort token reaches the
  readable-Teams question (the `?? default` survivors), and the involved Teams come before the owning Team.
- `PortfolioRepositoryTest.GetById_NoPortfolioHasThatId_ReturnsNothing` (`SingleOrDefault` to `Single` in `GetById`) and
  `Remove_NoPortfolioHasThatId_RemovesNothingAndDoesNotFail`.
- `ForecastConfirmationRepositoryRelationalTest`: a forgotten Portfolio and a Portfolio marked running record
  `forgotten:` / `computing:` plus 32 hex characters without hyphens (the prefix constants and the `"N"` format).

## Survivors left, by line

`PortfolioRepository.cs` (6, gate holds at 82.35 %):

- `:76` statement and string (2), `:94` (2), `:116` (2): `logger.LogDebug` / `LogInformation` calls and their message
  templates. Equivalent for behaviour: no test asserts on log output. `:76` is in the changed `GetById`.

`PortfolioController.cs`, in what the steps changed (2):

- `:50` and `:185` `ConfigureAwait(false)` to `ConfigureAwait(true)`. Equivalent: ASP.NET Core and the test host have no
  synchronization context.

`PortfolioController.cs`, killed in this pass: `:49` and `:184` (`?? default`), `:178` (`Append` to `Prepend`), `:189`
(object initializer).

`PortfolioController.cs`, outside the steps' change, not triaged (12): `:73`-`:90` (`DeletePortfolio`, 7 no coverage
here), `:118` / `:123` (staleness messages, no coverage), `:144` / `:149` / `:159` (`UpdatePortfolio`). Older code
whose scenarios live in the excluded `API.Integration` suite or are simply unpinned; not part of this slice.

No survivor was justified as "covered only by the acceptance scenarios", so no hand mutation was needed.

## Not mutated

`PortfoliosController.cs`, `TeamController.cs`, `TeamsController.cs`: one-line swaps of the reader they call.
`IPortfolioRepository.cs`: an interface.
