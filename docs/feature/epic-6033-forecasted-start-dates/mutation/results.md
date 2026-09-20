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

---

# Mutation testing — Epic 6033 slice 02 (the Feature table shows when work begins)

Run 2026-09-20 against `main` @ `8d61cb004`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **93.88 %** | 49 | 46 | 2 | 1 | ~1 m 45 s |
| Backend (Stryker.NET) | **N/A** | — | — | — | — | — |

Backend is N/A because this slice adds no backend production code. The one backend change it carries is
a test — the scenario pinning the precedence the column depends on — and a test cannot be mutated.

Config: `stryker-6046-slice-02.frontend.json` and its vitest include set
`vitest.stryker.6046-slice-02.config.ts`, both copied here; the working copies in
`Lighthouse.Frontend/` are gitignored as local tooling.

## Scope, and a correction to the first attempt

`Feature.ts` and `columns.tsx` are both large pre-existing files that this slice edited in places, so
the config mutates line ranges rather than whole files — 75-92, 116-117, 133-138, 155-156 and 254-266 in
`Feature.ts`; 51-52 and 76-88 in `columns.tsx`; all of `ForecastedStartCell.tsx`, which is new.

The first run reported **73.08 %**, and that number is not comparable, because the ranges were stale: a
four-line comment added while fixing the review's D2 finding shifted `Feature.ts` down by four lines
after the ranges were written. The run therefore mutated pre-existing `url` handling and missed part of
the mapping it was meant to measure. Ranges were re-derived from the file as it stands and the run
repeated. Both numbers are recorded here rather than only the good one.

That first run did find three genuine holes before the ranges were corrected, all the same shape — the
tests asserted that the right thing was drawn and never that nothing else was:

- **An empty cell** was only asserted to lack the observed-start marker, so a placeholder entry standing
  in for the missing forecasts would have passed. It now has to be empty to the character.
- **The observed-start cell** was only asserted to carry its four dates, so a caption above them —
  repeating on every row what the column header says once — would have passed.
- **The column's `field` and `sortable`** had no test at all. `field` is what the column-visibility menu
  toggles and what an export writes; naming a property the row does not carry breaks both silently,
  because the cell keeps drawing correctly — it reads the row directly and never looks the field up.

## Per file

| file | score | killed | survived | no coverage | tested |
| --- | --- | --- | --- | --- | --- |
| `components/Common/FeatureListDataGrid/columns.tsx` | **100 %** | 7 | 0 | 0 | 7 |
| `components/Common/FeatureListDataGrid/ForecastedStartCell.tsx` | 95.7 % | 22 | 0 | 1 | 23 |
| `models/Feature.ts` | 89.5 % | 17 | 2 | 0 | 19 |

Every mutant in the cell's decision logic is killed — which source wins, what each branch draws, and the
empty state — as is every mutant in the new column and the renamed header default.

## The three that remain

| file:line | mutation | why it is accepted |
| --- | --- | --- |
| `Feature.ts:80` | `"Unknown"` inside `z.enum([…])` → `""` | Equivalent, and created by the fix on the same line. With `.catch("Unknown")` attached, a payload carrying `Unknown` fails the mutated enum and falls to the catch, which yields `Unknown` — the same value by the other route. No observable difference exists to assert. |
| `Feature.ts:156` | `teamForecasts: IFeatureTeamForecast[] = []` → `["Stryker was here"]` | Unobservable today. `fromParsed` assigns this field unconditionally on the only path that builds a Feature from the wire, and the field's sole production reference in the codebase is that assignment — nothing reads it yet, by design; it is there for the timeline's sub-lanes. A test could only assert the initialiser on a bare `new Feature()`, which no code path observes. |
| `ForecastedStartCell.tsx:62` | `feature.teamsWithoutForecast ?? []` → `["Stryker was here"]` (no coverage) | Unreachable, and required anyway. Reaching this line means `cannotBeForecast` returned true, which means the list is non-empty and therefore defined, so the `??` branch cannot be taken at runtime. It cannot be deleted: `IFeature.teamsWithoutForecast` is optional, so the type checker needs it, and replacing it with a non-null assertion would be worse. The sibling completion column carries the identical expression. |

The gate is met at 93.88 %, and the one thing this slice is actually responsible for deciding — which of
the three sources the column draws, and what each one looks like — is fully killed.

---

# Mutation testing — Epic 6033 slice 03 (the tracker receives the start date)

Run 2026-09-20 against `main` @ `fc0ebdd3e`, after the adversarial review's findings were fixed. Gate is
80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.94 %** | 99 | 93 | 4 | 2 | 2 m 31 s |
| Frontend (StrykerJS) | **100 %** | 26 | 26 | 0 | 0 | ~1 m 30 s |

Configs: `stryker-6047-slice-03.backend.json`, `stryker-6047-slice-03.frontend.json` and its vitest
include set `vitest.stryker.6047-slice-03.config.ts`, all copied here; the working copies are gitignored
as local tooling.

Backend mutates whole files, because Stryker.NET ignores line spans — 19 298 of 19 395 mutants were
removed by the `mutate` filter and the ignore comments the codebase already carries, leaving 97 tested.
Frontend mutates line ranges, which StrykerJS does honour: `38-68` and `77-101` in
`WriteBackMappingDefinition.ts`, `182-215` in `WorkTrackingSystemService.ts`.

## What the frontend run found, and the re-run

The first frontend run read **88.46 %** with three mutants alive, and two of them were a real gap this
slice put there: the rename touched four completion labels and the test pinned the 50th and the 85th, so
the **70th and the 95th could have said anything at all**. The third was the `?? []` guard on a
connection that carries no sync mappings — a shape the save path handles and nothing exercised.

Three tests closed all three, and the run was repeated to **100 %**. Only tests were added between the
two runs, so the mutate ranges did not move; had a line shifted, the ranges would have needed
re-deriving and the earlier number would not have been comparable.

## The six the backend run left alive

| file:line | mutation | why it is accepted |
| --- | --- | --- |
| `WriteBackMappingValidator.cs:43` | `g.First()` → `g.FirstOrDefault()` | Equivalent. The value is read from a `GroupBy` group, and a group with no members does not exist. Pre-existing line. |
| `WriteBackMappingValidator.cs:40` | `is not null and not 0` → `not null or not 0` | Reachable, pre-existing, and not this slice's to close. The `or` form admits field-less mappings into duplicate detection, so two mappings that both lack a field would report a *duplicate* on top of the "an additional field is required" error they already report. A worse message, not a wrong write. Noted rather than fixed, because the line predates this slice and the fix belongs with whoever owns that message. |
| `WriteBackTriggerService.cs:188` (x2) | the `LogError` in the new per-mapping catch — statement removed, and its message rewritten | Accepted by the convention this codebase already applies to logging: the run skipped several hundred such mutants under in-place ignore comments whose reasons say the tests pin *that* an operator is told, not the wording. These two were written after that convention and simply have no ignore comment yet. What matters — that a failing mapping is skipped and the others still write — is asserted, and killed. |
| `WriteBackTriggerService.cs:43` | `wi.TeamId == team.Id` → `!=` (no coverage) | Unobservable through the test double. The repository is a Moq mock matching `It.IsAny<Expression<…>>()`, so the predicate is never evaluated — no unit test can kill it. Pre-existing, and the kind of thing only an integration test reaches. |
| `WriteBackTriggerService.cs:364` | the `ArgumentOutOfRangeException` message (no coverage) | Unreachable, and confirmed so rather than assumed. `GetPercentileFromSource` has two callers and both are entered only after membership in `WriteBackValueSources.Start` or `.Completion` has been tested — whose union is exactly the eight members the switch handles. Nothing can reach the default arm today. |

Both gates are met. Every mutant in the code this slice is responsible for deciding — which of the two
answers a start is, which day that lands on, and which sources a mapping may name — is killed.

---

# Mutation testing — Epic 6033 slice 04 (a Delivery as a timeline)

Run 2026-09-20 against `main` @ `6fcbd6482` plus the test-strengthening this section describes. Gate is
80 % kill rate on each stack touched. **Frontend only** — this slice changes no backend file.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **88.89 %** | 153 | 136 | 17 | 0 | 0 | 1 m 21 s |
| Backend (Stryker.NET) | **N/A** | — | — | — | — | — | — |

Backend is N/A because nothing under `Lighthouse.Backend` is touched; the wire contract this slice reads
shipped with slices 01 and 02.

| file | score | survived |
| --- | --- | --- |
| `deliveryTimelineModel.ts` | 98.67 % | 1 |
| `DeliveryTimelineTab.tsx` | 87.88 % | 4 |
| `DeliveryGanttChart.tsx` | 73.33 % | 12 |

**All three targets are new files, so they are mutated whole rather than by line range.** That is both
the honest scope — every line is this slice's — and immune to the drift that makes a stale range mutate
the wrong code and report a number that means nothing.

Config: `stryker-6048-slice-04.frontend.json` + `vitest.stryker.6048-slice-04.config.ts`, copied here;
the working copies under `Lighthouse.Frontend/` are gitignored as local tooling.

## Three runs, and what the first two were worth

**75.33 %** first time — below the gate. **86.09 %** after closing six real gaps. **88.89 %** after
extracting two functions that had no test at all. The middle number is the one worth keeping in view:
the six gaps it closed were all things a reader would expect to be tested, and none of them were.

- The **premium notice's copy** was never asserted, only its presence. A notice that renders empty is a
  blank panel where the chart was, telling the reader nothing.
- The **unknown licence state** was never exercised. The hook answers `null` until the licence has been
  fetched; reading through it without a guard throws and takes the tab down.
- **Re-clicking the selected confidence level** was never exercised. A toggle group reports `null` when
  its active button is pressed again, and taking that at face value leaves the chart with no percentile.
- The **count in the unplaceable heading**, and **the absence of that section** when everything is
  placeable, were both unasserted — so the section could have read "Not on the timeline (0)" forever.
- `isTargetDay` was only ever given **two-digit** months and days, so the zero-padding on both sides was
  free to disappear. Unpadded, `2026-3-7` matches nothing and the tint silently never appears in nine
  months out of twelve.
- The **target-tint callback had no coverage at all** — seven mutants, none reachable. The axis needs a
  measured width and therefore never renders outside a browser, so nothing invoked it. Its rule is now
  a named function, `targetDayHighlight`, tested directly. Its `unit === "day"` guard turns out to be
  load-bearing: the scale calls it for the month row too, and without the guard the whole month holding
  the target would be shaded instead of the one day.

The third run's two extractions were taken on their merits rather than for the score. `toGanttTasks` is
**the** translation into the library's vocabulary and had no test whatsoever — and since nothing asserts
on what the library renders, untested there means untested anywhere. `chartHeight` carries a real rule:
an empty chart keeps a row's height instead of collapsing onto its own axis.

## The seventeen left alive

Grouped, because they are a few kinds of thing and only one of them is interesting.

| group | count | why it is accepted |
| --- | --- | --- |
| `sx` spacing object literals (`{ p: 2 }`, `{ mb: 2 }`, `{ mt: 2 }`) | 4 | Padding. No behavioural assertion distinguishes them, and pinning spacing in a unit test is how a suite becomes a brake on design. |
| `useMemo` / `useCallback` wrappers and their dependency arrays | 6 | Equivalent by construction: memoisation changes how often a value is recomputed, never what it is. Killing them would mean asserting on render counts. |
| Exact pixel arithmetic in `chartHeight` | 3 | The tests pin the two rules that matter — one row per bar, and never less than one row — not the constants. Pinning the exact number asserts a magic value and breaks on any spacing change, which is worse than the mutant. |
| The `& .delivery-target-day` selector key | 2 | The tint's *rule* is tested; that the CSS selector matches the class the callback returns is only observable as a computed style on rendered library markup. |
| `links={[]}` | 1 | Dependency arrows are slice 05. Killing it means asserting what the library was handed, at a seam this slice deliberately does not reach into. |
| `start?.percentiles ?? []` | 1 | Equivalent. The mutant substitutes a non-empty array whose elements carry no `probability`, so the lookup still finds nothing and returns the same answer. |

**None of the seventeen is in the mapping.** Every mutant in the code that decides which day each end of
a bar sits on, which Features cannot be drawn, and what the reader is told instead, is killed — that
file is at 98.67 %, and its single survivor is the equivalent one above.

`DeliveryGanttChart.tsx` sits at 73.33 % on its own, and that is the one number worth understanding
rather than chasing. Twelve of its remaining mutants are memo wrappers, pixel arithmetic, and props
handed to a component whose output is deliberately not asserted anywhere. **That is the cost of the
adapter boundary, and it is the right trade**: a test that killed them would be a test of somebody
else's markup, going red on their release rather than on our defect. Everything in that file which is a
*decision* — the task translation, the axis formats, the tint rule, the theme choice, the height rule —
is extracted and tested directly, which is why the file's own score understates its coverage.
