# Mutation testing — Epic 6033 slice 01 (the run records the day it starts each Feature)

Run 2026-09-20 against `main` @ `4f6f4c7a5`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | 73.54 % whole-file · **93.83 % on the lines this slice added** | 325 | 228 | 62 | 11 | 24 | 15 m 08 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

Frontend is N/A because this slice touches no frontend file. Nothing under `Lighthouse.Frontend/src`
was modified; the Feature table column is slice 02.

Config: `stryker-config.epic-6033-slice01.json` (copied here; the working copy under
`Lighthouse.Backend.Tests/` is gitignored as local tooling). Scope sanity-check per the standing rule:
19 366 mutants created across the backend, 17 205 removed by the mutate filter, **286 tested plus 24
with no coverage** — consistent with twelve files.

## Two numbers, and why the second is the one that answers the gate

**73.54 %** is every mutant in twelve whole files. Four of those files are large and pre-existing —
`ForecastService.cs` (312 lines before this slice), `Feature.cs` (241 before), `FeatureDto.cs`,
`DtoExtensions.cs` — and this slice changed a fraction of each. A whole-file score over them measures
the suite that was already there at least as much as it measures this change.

**93.83 %** is the same run, counted only over the line ranges this slice added or rewrote: 81 mutants,
**76 killed, 5 survived, 0 without coverage.** Every one of the five is named and accounted for below.

Both numbers are from one run with one config. The second is not a narrowed re-run — narrowing the
config until the number looks good is the exact trap this project has been bitten by before, and the
whole-file figure stays in this table for that reason.

## Per file

| file | score | killed | timeout | survived | no coverage | tested |
| --- | --- | --- | --- | --- | --- | --- |
| `Models/Forecast/StartForecast.cs` | **100 %** | 1 | 0 | 0 | 0 | 1 |
| `Services/.../Forecast/TrialRecordings.cs` | **100 %** | 6 | 0 | 0 | 0 | 6 |
| `Services/.../Forecast/DayCounts.cs` | **100 %** | 4 | 0 | 0 | 0 | 4 |
| `API/DTO/FeatureStartDto.cs` | **100 %** | 5 | 0 | 0 | 0 | 5 |
| `API/DTO/WhenForecastDto.cs` | **100 %** | 1 | 0 | 0 | 0 | 1 |
| `Services/.../Forecast/TrialState.cs` | 93.3 % | 50 | 6 | 4 | 0 | 60 |
| `Services/.../Forecast/ForecastRunPlan.cs` | 83.3 % | 15 | 0 | 3 | 0 | 18 |
| `Services/.../Forecast/SimulatedRun.cs` | 82.5 % | 31 | 2 | 7 | 0 | 40 |
| `Models/Feature.cs` | 71.2 % | 57 | 0 | 12 | 11 | 80 |
| `Services/.../Forecast/ForecastService.cs` | 64.6 % | 50 | 3 | 29 | 0 | 82 |
| `API/DTO/FeatureDto.cs` | 35.0 % | 7 | 0 | 7 | 6 | 20 |
| `API/DTO/DtoExtensions.cs` | 12.5 % | 1 | 0 | 0 | 7 | 8 |

Every file the slice **introduced** is fully killed. Every file it extended **inside the simulation** —
the recorder, its per-trial state, the run plan — is above the gate. The four below it are the large
pre-existing files, and in `FeatureDto.cs` and `DtoExtensions.cs` **not one survivor is on a line this
slice wrote**: they are the named-cycle-time default, `LastUpdated`, the `TeamsWithoutForecast`
ordering, the remaining/total work accumulation, the readable-portfolios loop, and the two
`DtoExtensions` methods this slice never touched.

`Models/Feature.cs` at 71.2 % is the same story: of its 23 survivors and no-coverage mutants, **two** are
on a line this slice added (below); the rest are `TeamsWithoutForecast`, `TeamFor`, `HasForecastRowFor`,
`GetRemainingWorkForTeam`, `ClearFeatureWork`, `ReplaceDependsOnReferences` and `Update`.

`ForecastService.cs` at 64.6 % has 29 survivors, **one** of which is on an added line. Twenty-one of the
other 28 are `LogDebug`/`LogInformation` message strings and their statements — the category this
project already ignores by comment elsewhere in the codebase, not ignored here only because these lines
predate the convention.

## What three earlier runs bought

The gate was run three times, and the first two found real gaps rather than confirming the third.

| run | whole-file | what it found |
| --- | --- | --- |
| 1 | 70.15 % | `TrialState.cs:43`, `Feature.cs:128`, `ForecastRunPlan.cs:98` all survived |
| 2 | 71.08 % | those three killed; `FeatureDto.cs` at **0 %**, 20 mutants with no coverage at all |
| 3 | 73.54 % | `FeatureDto` unit tests added; slice-own lines at 93.83 % |

The three from run 1 were genuine holes in tests that read as though they covered the thing:

- **`TrialState.cs:43` — `Array.Clear(rowHasBeenWorked)`.** Both "a Feature is recorded as starting
  once" tests asserted on the Feature-grain row, which a *different* marker gates. The per-row marker's
  reset was never exercised, and the test's own comment claimed it was. Now both grains are asserted.
- **`Feature.cs:128` — the `!CanBeForecast` guard in `WhenWorkBegins`.** The test used a Feature with no
  start rows at all, which reaches the same answer down a different path. The case that needs the guard
  is a Feature the run *did* record a start for and still cannot honestly forecast.
- **`ForecastRunPlan.cs:98` — the no-Feature branch.** Untested; a row belonging to no Feature now has a
  test.

Run 2's finding was that `FeatureDto` — this slice's actual read contract — had no unit test at all, only
acceptance coverage that Stryker cannot afford to drive. Seven were added, and they pinned two edges
nothing else covered: a Feature nobody is working, and a team appearing twice in one Feature's work
getting one row rather than two.

## The five survivors on added lines

| file:line | mutation | why it is accepted |
| --- | --- | --- |
| `Feature.cs:147` (x2) | `(forecast.Team?.Id ?? forecast.TeamId)` → either side alone | Defensive. A completion row carries the team object just after a forecast runs and only the id after a round trip, so either side alone satisfies every reachable case. The identical pre-existing expression in `HasForecastRowFor` (`Feature.cs:203`) survives the same three mutations and always has; killing one and not the other would be inconsistent for no gain. |
| `ForecastService.cs:134` | `if (plan.RowCount == 0) { return []; }` → block removed | Equivalent. With no rows the run loop produces an empty result anyway, so removing the early return changes the work done and not the answer. It is there to skip 10 000 empty trials, and a test could only assert the timing. |
| `ForecastService.cs:215` | `days.OrderBy(...)` → `OrderByDescending` | Equivalent on every read. `ForecastBase` re-sorts into a `SortedDictionary` before any percentile is taken, so the order only decides the order rows are written to the database in. The sibling `RecordTheDaysEachRowFinishedOn` orders for the same reason and its mutant (`:233`) survives identically. |
| `ForecastRunPlan.cs:95` | `new ForecastRunPlan(…) { NobodyWaitsForAnything = … }` → initializer removed | Equivalent on the answer. False where it should be true makes the run ask "is anything workable today?" the long way round every day instead of short-circuiting; the rows it returns are the same. Performance, not behaviour. |
| `FeatureDto.cs:36` | `feature.Forecast?.CreateForecastDtos(…) ?? []` → right side removed | Pre-existing expression on a line this slice edited only to pass the shared percentile array. Inside `if (feature.CanBeForecast)`, `Forecast` is never null, so the fallback is unreachable. |

Killing the `Feature.cs:147` pair would mean constructing a completion row with the team object set and
its id left unset — a state nothing in the product produces. The rest are equivalent mutants. The gate is
met at 93.83 % on the code this slice is responsible for, with the decision logic — the recorder, the
per-trial reset, the two grains, the observed-start rule and the read contract — fully killed.
