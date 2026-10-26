# Mutation testing — 6409 (Epic 6400 slice 05: lighter forecast rounds)

Run 2026-10-26 against local `main` @ `e2655781c` (tree clean, code frozen). Backend only; no frontend change.
Gate: 80 % kill rate on what the slice changed. `ForecastUpdater.cs` and `FeatureRepository.cs` whole-file carry much
older code, so their whole-file scores are reported but survivors are triaged only in the regions
`git diff d7595f8e2..e2655781c` shows.

Stryker's "will be tested" line: **137** (**139** after the refresh below) (of 22 768 created for the whole backend). One run was enough: the gate held
on the first run, so there was no kill round and no second run.

## Whole file

| file | scored | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `BackgroundServices/Update/ForecastUpdater.cs` | 74 | 67 (2 by timeout) | 5 | 2 | 90.54 % |
| `Repositories/FeatureRepository.cs` | 31 | 21 | 2 | 8 | 67.74 % |
| `Repositories/PortfolioRepository.cs` | 37 | 31 | 6 | 0 | 83.78 % |
| `BackgroundServices/Update/FeatureOrderingPolicyChangedForecastTriggerHandler.cs` | 1 | 1 | 0 | 0 | 100 % |
| `DomainEvents/DeliveryForecastPublishingHandler.cs` | 6 | 5 | 1 | 0 | 83.33 % |

Scores count no-coverage mutants as not killed, as Stryker does; timeouts count as killed. Compile-error and ignored
mutants are not scored. Final overall score printed by Stryker: 83.89 %.

## Changed region (what slice 05 changed)

| file | region (lines in `e2655781c`) | scored | killed | survived | score |
| --- | --- | --- | --- | --- | --- |
| `ForecastUpdater.cs` | catalogue read and `TeamIdsOf` trigger check (`:125-132`), `OneWithoutForecasts` (`:181`), `StageTheWriteBackAndAnnounce`, `ResolveTheForecastWriteBack` with its catch, `ReadAndResolveTheForecastWriteBack`, `MapsAForecast` (`:254-307`) | 20 | 18 | 2 | **90.00 %** |
| `FeatureRepository.cs` | `FeaturesWorkedOnBy` (`:86-102`) | 1 | 1 | 0 | **100 %** |
| `PortfolioRepository.cs` | `AllIds` and `TeamIdsOf` (`:58-72`) | 3 | 3 | 0 | **100 %** |
| `FeatureOrderingPolicyChangedForecastTriggerHandler.cs` | the loop over `AllIds()` | 1 | 1 | 0 | **100 %** |
| `DeliveryForecastPublishingHandler.cs` | the early return on no Delivery (`:33-41`) | 1 | 1 | 0 | **100 %** (plus the hand mutations below) |

Stryker produces few mutants in the two query bodies (the `Where`/`Include` chains and `Distinct` give it little to
change); the surviving risk there is covered by the relational tests, not by the count.

## Run command

From `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `StrykerOutput` removed before the run, `nohup` from a foreground
shell, coverage analysis `perTestInIsolation`, concurrency 4:

```
dotnet stryker --config-file ../../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6409.backend.json
```

Duration 5 m 25 s. `--since` / `--with-baseline` not used. Unit classes in the filter: `ForecastUpdaterTest`,
`ForecastGatherReaderRelationalTest`, `Repositories.FeatureRepositoryTest`, `FeatureGraphReaderRelationalTest`,
`FeaturesWithReferenceIdsRelationalTest`, `PortfolioRepositoryTest`, `PortfolioCatalogueReaderRelationalTest`,
`DeliveryForecastPublishingHandlerTest`, plus `ForecastConfirmationRepositoryRelationalTest` and
`Repositories.DeletionTests` for the older `PortfolioRepository` and `FeatureRepository` code. Nothing from
`API.Integration` (the slice's own acceptance scenarios, `Slice05LighterRoundsScenarios`, stay out), `Integration.Containers`
or the live-connector categories.

## Survivors, by line

Inside the slice's changed regions (2):

- `ForecastUpdater.cs:275` statement and string (2): the `Logger.LogError(...)` call and its message template in the
  catch of `ResolveTheForecastWriteBack`. Equivalent for behaviour: no test asserts on log output. That the catch
  stages nothing, still announces and does not swallow a cancellation is pinned (`:268` block, `:272` filter killed).

Outside the changed regions, older code, not triaged for this slice:

- `ForecastUpdater.cs:142`, `:147` (no coverage): the messages of two `NotSupportedException`s.
- `ForecastUpdater.cs:215`: `stopwatch.Stop()` in a `finally`, timing only.
- `ForecastUpdater.cs:368` statement and string (2): the `LogWarning` when forgetting forecast inputs fails.
- `FeatureRepository.cs:32`, `:36`: `CreationTime < confirmedAt` to `<=` in the confirm-forecast writes; no-coverage
  `:104` / `:107` (the Portfolio and Team feature reads), `:118` and `:129` (a forecast read with its
  histograms, `ParentsOf`) and `:140` (`ParentsOf` projection) whose tests are in classes
  outside this filter.
- `PortfolioRepository.cs:93`, `:111`, `:133`: `logger.LogDebug` / `LogInformation` calls and templates.
- `DeliveryForecastPublishingHandler.cs:70`: block removal in the catch around `logger.LogError`; the source carries a
  `Stryker disable` for it, and the round surviving a failed publish is asserted.

## Early return in the handler, by hand

Stryker marks the `deliveries.Count == 0` mutants as compile errors or "block already covered", so none of them was
scored. Mutated by hand and run against `DeliveryForecastPublishingHandlerTest`, then restored with `git checkout`:

- `Count == 0` to `Count != 0`: 3 tests fail (`A_finished_forecast_is_published_...`,
  `A_Portfolio_with_no_Delivery_is_not_read_and_publishes_nothing`, `What_publishing_found_out_...`).
- `return;` in the block replaced by a no-op: `A_Portfolio_with_no_Delivery_is_not_read_and_publishes_nothing` fails.
- Replacing the condition by `false` or `true` does not compile (warnings are errors), so those cannot survive.

No survivor was justified as "covered only by the acceptance scenarios", and no test was added: the gate held on the
first run and every in-scope survivor is a log line.

## Refresh after the ids-only trigger

Re-run 2026-10-26 on `55a6accc7`, which makes `FeatureOrderingPolicyChangedForecastTriggerHandler` read the Portfolios
through the new `IPortfolioRepository.AllIds()` instead of `GetAll()`. Same config, with the handler added to `mutate`
and `FeatureOrderingPolicyChangedForecastTriggerHandlerTest` added to the test-case filter (unit namespaces only);
`ForecastGatherReaderRelationalTest`, already in the filter, carries the relational test for `AllIds()`. 139 mutants
tested, 5 m 46 s, overall 83.89 %.

- `PortfolioRepository.cs` 83.33 % to 83.78 %: the one new mutant (in `AllIds`) is killed; the six survivors are the
  same log lines as before, only seven lines lower.
- `FeatureOrderingPolicyChangedForecastTriggerHandler.cs`: one scored mutant, killed; two more are ignored by Stryker.
  Nothing survives in new code, so there was no kill round.
- The other three files are unchanged (`ForecastUpdater.cs` 90.54 %, `FeatureRepository.cs` 67.74 %,
  `DeliveryForecastPublishingHandler.cs` 83.33 %).
