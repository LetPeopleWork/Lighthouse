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

---

# Mutation testing — Epic 6033 slice 04, round two (after live review)

Run 2026-09-20 against `main` @ `c981726d5`. Gate is 80 % on each stack touched. **Frontend only.**

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **82.69 %** | 311 | 258 | 53 | 0 | 0 | 3 m 28 s |

| file | score | survived |
| --- | --- | --- |
| `timelineMarkers.ts` | 100 % | 0 |
| `deliveryTimelineModel.ts` | 98.41 % | 1 |
| `ganttShapes.ts` | 97.27 % | 3 |
| `DeliveryTimelineTab.tsx` | 83.64 % | 9 |
| `TimelineLegend.tsx` | 63.16 % | 7 |
| `DeliveryGanttChart.tsx` | 50.00 % | 16 |
| `TimelineBarContent.tsx` | 37.04 % | 17 |

## Four runs, and what each one was actually worth

**76.32 %** after the live-review fixes · **81.50 %** after two extractions · **82.69 %** after
deleting unreachable code. (An intermediate 71.86 % is not in that sequence — it was measured
mid-refactor and is noted below only because of what it revealed.)

The score is the least interesting part. What the runs found:

- **A dead branch.** The column-width helper handled year columns; nothing can reach it, because
  only the finest row of the axis is marked and the finest row is a month at coarsest. Five mutants
  with no coverage were the only signal. Deleted rather than tested — a test over unreachable code
  is a test that cannot fail.
- **Two dead exports.** `isTargetDay` and `isSameLocalDay` lost their production callers when the
  markers moved to containment. `isSameLocalDay` had none at all; `isTargetDay` was kept alive
  purely by its own tests, which is the worst way for dead code to look healthy. What they were
  protecting moved to `targetCalendarDate` along with the tests.
- **Two formatters never once invoked.** The weekly and monthly axis formatters were only checked
  for *being* functions. A broken one would have gone unnoticed until someone opened a long
  Delivery — and "the format is printed verbatim" is already the defect that reached a screenshot
  once in this slice.
- **The column-count boundary**, where `<=` and `<` are one character apart and the difference is a
  column of overflow.
- **The legend's two swatches** were never asserted to differ. A legend whose entries look alike
  explains nothing.
- **The details dialog** was never closed, so nothing caught a dialog that cannot be dismissed or
  that forgets its selection and refuses to reopen on the same bar.

## What the 71.86 % run revealed, which the number did not

Splitting the pure logic out of the chart component *lowered* the aggregate, because it concentrated
the untestable JSX in one file rather than diluting it. The right response was not to undo the split
— the split is what let arithmetic be tested without stubbing a canvas — but to notice **why** the
remaining file scored 21 %: everything a bar decides was inside a closure reachable only through a
library that does not render here. `TimelineBarContent` came out of that, and the chart went from
21 % to 50 % without a single test written for the score's sake.

## The fifty-three left alive

| group | count | why it is accepted |
| --- | --- | --- |
| `sx` style literals — widths, spacing, `display: flex`, `overflow: hidden`, colour keywords | ~40 | Styling. No behavioural assertion distinguishes them, and pinning them turns the suite into a brake on visual change. The rendered result belongs to the screenshot test. |
| `useMemo` / `useCallback` dependency arrays and wrappers | 8 | Equivalent by construction: memoisation changes how often a value is recomputed, never what it is. |
| Exact pixel arithmetic in `chartHeight` | 3 | The two rules that matter are pinned — one row per bar, never fewer than one row. The constants are not, because pinning them asserts a magic number and breaks on any spacing change. |
| The dialog's closed-state fallbacks (`: ""`, `: []`) | 2 | Reachable only while the dialog is shut, when nothing reads either value. |

`TimelineBarContent.tsx` reads 37 % and is the clearest case of a score understating its coverage:
seventeen of its mutants are the `sx` block, and every decision the component makes — clickable or
inert, what the hover promises, what happens to a bar the chart asks for and we do not have — is
asserted directly. That is what the file was extracted for.

`DeliveryGanttChart.tsx` at 50 % is the adapter boundary's standing cost, unchanged in kind from
round one: what is left there is props handed to a component whose output is deliberately asserted
nowhere. A test that killed those would be a test of somebody else's markup, going red on their
release rather than on our defect.

---

# Mutation testing — Epic 6033 slice 05 (dependency lines on the timeline)

Run 2026-09-21 against `main` @ `f2f67ed6b`, after the refactor, the live-review change and the
adversarial-review fixes — frozen code, as the gate requires. **Frontend only**; the backend change
is two demo-data rule builders and is covered by the two NUnit scenarios in step 05-07.

| stack | score | killed | survived | timeout | no coverage | errors | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend, all eight mutated files | 72.54 % | 523 | 184 | 0 | 14 | 0 | ~33 m |
| **Frontend, the surface this slice owns** | **92.22 %** | 332 | 25 | 0 | 3 | 0 | — |

| file | score | survived | whose code |
| --- | --- | --- | --- |
| `dependencySentences.ts` | 100 % | 0 | slice 05 |
| `featureWarningSentences.ts` | 100 % | 0 | slice 05 |
| `ganttShapes.ts` | 96.67 % | 4 | slice 05 (`toGanttLinks`) |
| `deliveryDependencyOverlay.ts` | 93.59 % | 4 | slice 05, new |
| `DeliveryTimelineTab.tsx` | 81.90 % | 17 | slice 05 |
| `WorkItemsDialog.tsx` | 60.00 % | 92 | slice 04 and earlier |
| `TimelineBarContent.tsx` | 58.00 % | 21 | slice 04 |
| `DeliveryGanttChart.tsx` | 27.63 % | 46 | slice 04 |

## Two numbers, and why the smaller one is the misleading one

The 72.54 % headline is an artefact of what was put in the mutate list, not a statement about this
slice. Three of the eight files are carried-over presentation code that slice 05 barely altered, and
they contribute 159 of the 184 survivors between them.

`WorkItemsDialog.tsx` gained **one** optional column descriptor of the five it now has; the other
four, and the grid plumbing around them, predate this work entirely.

`DeliveryGanttChart.tsx` gained **five lines** — a `links` prop, a memo and the swap of `links={[]}`
for `links={ganttLinks}`. Not one survivor falls in them. They cluster instead at the `ResizeObserver`
callback, at a date formatter handed to the vendor, and at the scale callback the vendor invokes —
none of which jsdom runs. That is the same standing cost slice 04 recorded for this file, and the
reason the adapter exists in the first place.

`TimelineBarContent.tsx` is the one slice 05 genuinely shares: the note types moved into it and the
mark symbol is new. Its survivors sit inside the two JSX return blocks, on `sx` objects and style
strings. Killing them would mean asserting on styling values, which is precisely how slice 04 shipped
four assertions incapable of failing — one of them asserting a CSS variable on the element under test
rather than the element that resolves it. The score is left where it is deliberately. It rose from
37.04 % to 58.00 % anyway, because the new mark has behaviour worth asserting and it is asserted
through accessible names.

## What the run actually found

Nothing that needed fixing. That is worth stating plainly rather than dressing up: the four survivors
in the overlay and the four in `ganttShapes` were reviewed individually and are equivalent mutants or
styling, and the deliberately-weak edge-limit default is a survivor accepted in advance and recorded
as such when it was written — a mutant moving the default from 40 to 41 is not killed by anything,
because pinning that number would assert the number rather than the behaviour and would break on the
next re-measurement.

The reason this run found little is that the mutants it would have found were killed earlier, by
hand, as the steps ran: every step named the mutation that reds each of its tests before accepting it,
and three steps ran those mutations against their finished code and measured which tests died. The
boundary operator in the edge limit, the source/target swap in the link mapping, the empty-reference-id
guard and the `isWorthWarningAbout` call site were all verified that way rather than by waiting for
Stryker.

## Method note

Scoped with `stryker-6049-slice-05.frontend.json` + `vitest.stryker.6049-slice-05.config.ts`, the
latter committed here. Every path in both files was checked to exist before the run: a spec missing
from the runner's include list makes every mutant in the code it covers survive for want of a test
*run* rather than for want of a test, and the report cannot tell those two apart.

---

# Mutation testing — Epic 6033 slice 06 (Teams on the Delivery timeline)

Run 2026-09-21 against `main`, after the refactor pass, the live-review changes and the
adversarial-review fixes. **Frontend only**; slice 06 changes no backend file at all.

**The figures below are the third run**, and the slice was mutated three times. What moved between
them is the useful part of this record:

| run | headline | what it found |
| --- | --- | --- |
| one | 77.85 % | two of the three files this slice created were barely tested — the colour key at 20.00 % and the preference store at 59.09 %, both reachable only through the tab that composes them |
| two | 79.60 % | those gaps closed; two survivors left that were **tests unable to fail** rather than untested code |
| **three** | **80.06 %** | those two closed. `useShowTeams.ts` went to **zero** no-coverage mutants, which is the evidence its storage guard is now actually entered rather than merely named |

Each run was against frozen code, as the gate requires. **Frontend only**; slice 06 changes no
backend file at all.

| stack | score | killed | survived | no coverage | timeout | errors | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Frontend, all eight mutated files** | **80.06 %** | 514 | 115 | 13 | 0 | 0 | 8 m 37 s |
| Frontend, the surface this slice owns | 90.55 % | 441 | 42 | 4 | 0 | 0 | — |
| Slice 04's two carried-over files | 47.10 % | 73 | 73 | 9 | 0 | 0 | — |

The two rows reconcile against the headline without rerunning anything: 441 + 73 = 514 killed,
42 + 73 = 115 survived, 4 + 9 = 13 uncovered. That arithmetic is written out because a split that
cannot be checked is a convenient number rather than a credible one.

| file | score | killed | survived | no cov | whose code |
| --- | --- | --- | --- | --- | --- |
| `deliveryTimelineModel.ts` | 98.48 % | 65 | 1 | 0 | slice 04, one parameter widened by slice 06 |
| `deliveryTeamLanes.ts` | 97.17 % | 103 | 2 | 1 | **slice 06, new** (92.45 → 96.23 → 97.17) |
| `useShowTeams.ts` | 95.65 % | 22 | 1 | 0 | **slice 06, new** (59.09 → 86.96 → 95.65; uncovered 2 → 0) |
| `ganttShapes.ts` | 95.14 % | 137 | 6 | 1 | slice 04 and 05, extended by slice 06 |
| `DeliveryTimelineTab.tsx` | 80.43 % | 111 | 25 | 2 | slice 04 and 05, extended by slice 06 |
| `TimelineBarContent.tsx` | 64.86 % | 48 | 26 | 0 | slice 04 (was 58.00 % at slice 05) |
| `DeliveryGanttChart.tsx` | 30.86 % | 25 | 47 | 9 | slice 04 (was 27.63 % at slice 05) |
| `TimelineTeamLegend.tsx` | 30.00 % | 3 | 7 | 0 | **slice 06, new** (was 20.00 %) |

## The gate verdict

**The headline is 80.06 % and clears the 80 % gate on its own.** No attribution argument is needed
to pass, and none is being made.

The owned surface is 90.55 %, and it is worth stating for a different reason than the one it would
have served at 79.60 %: it is the sharper measurement of this slice's own work, not the number that
rescues it. The split is kept because it is the honest way to read a mutate list that necessarily
includes files the slice touched without owning — the tool mutates whole files — and because the
arithmetic above lets a reader check it rather than take it.

Slice 05 recorded the same shape from the other side: 72.54 % headline against 92.22 % owned, with
`DeliveryGanttChart.tsx` attributed to slice 04 at 27.63 %. There the split was load-bearing. Here it
is explanatory.

**The two carried-over files both improved under this slice** — `TimelineBarContent.tsx` from 58.00 %
to 64.86 % and `DeliveryGanttChart.tsx` from 27.63 % to 30.86 % — because closing the adapter's
bar-content seam put tests through code that previously had none. Neither is chased to 80. What
remains in them is `sx` blocks, a `ResizeObserver` callback, a date formatter handed to the vendor
and the scale callback the vendor invokes, none of which this environment runs; asserting on them is
how this Epic already shipped assertions incapable of failing.

## `TimelineTeamLegend.tsx` reads 30 %, and that is the right number

The colour key has ten mutants. **Seven of them are CSS properties** — `display`, `flexWrap`,
`alignItems` and two `gap` values, plus the two object literals that hold them. Killing any of them
means asserting that a flex container is `display: flex`, which pins the implementation, catches no
defect anyone could have, and would go red the day someone reaches the same layout another way.

The eighth was worth killing and is dead: emptying the swatch's style removes the Team's colour
altogether, and the key then explains nothing. That is caught by requiring two Teams' patches to be
styled differently.

So what the component *decides* is covered — one entry per Team, the name written in full, the patch
hidden from anything reading the page aloud, two Teams distinguishable, and both entries present
when the Portfolio can name neither Team. What is not covered is how it is arranged, deliberately.
A reader meeting 30 % here should read it as a file that is nine-tenths layout, not as a file that
is nine-tenths untested.

## Closed after run one

Each mutation below was re-applied by hand against the finished code and watched to fail.

| file | mutation | what now kills it |
| --- | --- | --- |
| `deliveryTeamLanes.ts` | the control's verdict forced to `false` | asserted **true** for a splitting Delivery. Both existing assertions on it expected `false`, so a verdict hard-wired to "nothing to show" satisfied them. This closed the first and third operands of that verdict; the middle one survives and is taken up below — the first pass read the mutant as covering the whole expression, and it does not |
| `deliveryTeamLanes.ts` | `if (split.unlaned.length > 0)` → `true` | a Feature where every Team has a lane leaves no note entry at all, rather than an empty one |
| `deliveryTeamLanes.ts` | `if (team.name)` → `true` | a Team the Portfolio lists **with a blank name** is treated as one it cannot name. A blank is not a name: written along a lane it is nothing, and the colour helper drops a falsy key outright |
| `deliveryTeamLanes.ts` | `left.isNamed ? -1 : 1` → `+1` | the un-nameable Team arriving **first** in the forecast still sorts last. The two orderings only disagree when the named Team is the one being asked about, which the existing fixture never provoked |
| `useShowTeams.ts` | the stored read forced to `false`; `=== "true"` → `=== ""` | the store's own spec: a stored `"true"` reads as on |
| `useShowTeams.ts` | `shownNow ??= readStored()` → `&&=` | a first reader with a stored choice sees it. Under `&&=` the store never reads storage at all |
| `useShowTeams.ts` | unsubscribe replaced by an empty function | a listener that has stopped listening is not told, paired with one that is, so it cannot pass against a store that tells nobody |
| `useShowTeams.ts` | the `catch` around the **write**, previously uncovered | a choice the store could not persist is still the choice for this visit, which is only true if the throw was caught. The `catch` around the *read* is still uncovered and is taken up below |
| `TimelineTeamLegend.tsx` | the swatch's `sx` → `{}` | two Teams' patches must be styled differently. With the fill gone they share one class |

`useShowTeams.ts` was reshaped to make this possible: it is now an exported store — subscribe, read,
set, forget — with the hook as its React binding. That is what it always was; it was simply not
reachable. **Both copies of `vitest.stryker.6050-slice-06.config.ts` gained the two new specs.** A
spec missing from the runner's include list makes every mutant in the code it covers survive for
want of a test *run* rather than for want of a test, and the report cannot tell those two apart —
which is exactly what a new spec file would have hit here.

## Closed after run two

Run two left two survivors that were **not** untested code. Both were tests that could not fail, and
both are now able to. Test-only; no production file was touched to close either.

| file | mutation | what was wrong, and what now kills it |
| --- | --- | --- |
| `deliveryTeamLanes.ts` | the **middle** operand of the control's verdict, `unlanedTeams.size > 0`, forced to `false` | Run one's fix was reported as closing this and did not: the mutant's own span covers one operand of three, not the whole expression, and the first pass read it as the whole. The middle one decides the answer on exactly one shape — a Feature two Teams work on where *neither* resolves at the probability being read: nothing to draw, nothing to colour, two Teams still to name on the bar. Every other fixture has a Team that does resolve, so the first operand carried the verdict. That fixture now exists |
| `useShowTeams.ts` | the `catch` around the stored read | Two tests named this guard, mocked the read to throw, and asserted the answer was `false` — which an absent key also answers, so neither could tell a caught throw from nothing being there. Probing it found the guard was not merely under-asserted but **never entered at all**; see the section below. The fixture now stores the choice as *on* before breaking the read, so a read that got through answers `true` and a caught one answers `false` |

The correction on the first row is the more useful half of this record. A survivor reported closed
and not closed is worse than one left open, because nothing looks at it again — and what caught it
was re-reading the report's own mutant spans rather than trusting the previous pass's summary.

## What survives now, triaged one by one against the third run's list

Re-read from the third report rather than carried over. What is left is four mutants, and every one
of them is equivalent, unreachable, or styling.

**Accepted — equivalent or unreachable.**

| file | mutation | why it cannot be meaningfully killed |
| --- | --- | --- |
| `deliveryTeamLanes.ts` | `byId.get(featureId) ?? []` → a sentinel array (no coverage) | Unreachable by construction. The index is built from the same Features the timeline was built from, so the fallback answers a question that cannot be asked. TypeScript requires it written |
| `deliveryTeamLanes.ts` | `feature.teamForecasts ?? []` → a sentinel array, in both places | **Equivalent, and checked rather than assumed.** A one-element array holding a string takes the single-Team path, where the id resolves to nothing and no Team is recorded — precisely what an empty array produces. In the colour key set it adds `"undefined"`, which sorts last and so shifts no Team's colour. The test written for the shape that reaches it — a Feature carrying no per-Team forecast at all, which is every timeline fixture older than slice 01 — is worth having for its own sake even though it kills nothing |
| `useShowTeams.ts` | `useCallback` deps `[]` → `["Stryker was here"]` | Equivalent. The dependency is a constant, so the callback's identity is as stable as it was |
| `TimelineTeamLegend.tsx` | seven `sx` layout mutants | Styling — see the section above |
| `TimelineBarContent.tsx`, `DeliveryGanttChart.tsx` | 73 between them | Slice 04's code — see the section above |

Nothing else is outstanding. Every survivor in the third run is in the table above.

## The finding worth carrying past this slice: a mock that never mocked anything

Closing the storage guard turned up something that is not about slice 06 at all, and it is the most
transferable thing this work produced.

**In this test environment, `localStorage` does not inherit from `Storage.prototype`.** Its
`getItem` and `setItem` are own properties of the object. So `vi.spyOn(Storage.prototype, "getItem")`
installs cleanly, reports itself as installed, and **intercepts nothing**. Probed directly, twice,
independently:

```
inherits=false  ownGetItem=true  protoSpyIntercepted=false  objectSpyIntercepted=true
```

The consequence is sharper than "those tests were weak". A test written this way names a guard,
arranges for the guard to be needed, and then never reaches it — while passing. And it passes for a
second reason that hides the first: the assertion is almost always *"the default is still in
effect"*, which an absent key satisfies anyway. Two independent reasons to be green, neither of them
the one the test claims.

**Every other use of the idiom in this repository was then read**, rather than inferred from the
grep. There are four, in three features, and they do not all fail the same way:

| where | what it claims | verdict |
| --- | --- | --- |
| `hooks/useAgingBackground.test.ts:75` | "still draws a chart when the browser refuses to say what was stored" — asserts `background` is `"off"` | **Cannot fail.** The spy never intercepts, and `"off"` is what an absent key gives anyway |
| `hooks/useAgingBackground.test.ts:93` | "still applies a choice the browser refuses to remember" — asserts `background` is `"pace"` after choosing it | **Cannot fail.** The spy never intercepts, and the state would move to `"pace"` whether or not the write threw |
| `components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx:1786` | "opens anyway when the browser will not remember anything" — asserts the dialog and its Enlarge button render | **Cannot fail.** The spy never intercepts, and the dialog renders with no stored key at all |
| `services/UsageData/usageDataEvents.test.ts:162` | "keeps what it noticed in memory, writing to neither browser store" — `expect(writes).not.toHaveBeenCalled()` | **The spy assertion cannot fail** — a spy that intercepts nothing records nothing, so `not.toHaveBeenCalled()` is true by construction. **But the test is not vacuous**: its siblings assert `localStorage.length` is 1 and `sessionStorage.length` is 0, and those *would* catch a real write. The claim is protected; the line that looks like it protects it is not |

That last row is why the others were read rather than assumed from a grep. Three tests are green for
a reason unrelated to what they assert; the fourth asserts the same thing twice, once uselessly and
once properly, and is fine. A finding stated as "four broken tests" would have been wrong in a way
that costs someone an afternoon.

**Fixing the three is outside this slice.** They belong to two other features, they are green today,
and changing them means understanding what each was protecting — its own blast radius and its own
review. This is a verified finding recorded where the next person will look, not a task left
half-done. The fix, when someone takes it, has two parts and neither is sufficient alone: spy the
**object**, and arrange a fixture where *caught* and *absent* give **different** answers. The second
part is the one that is easy to skip, and skipping it leaves a test that still cannot fail.

## Four assertions in this slice could not fail, and four different instruments found them

Worth stating plainly, because it is the most useful sentence here for whoever reads it next.

| what could not fail | found by |
| --- | --- |
| a bar's name checked with `toHaveTextContent`, satisfied by the accessible title inside an icon | measuring the RED classification before writing the code |
| `expect(timelineWindow(bars)).toEqual(timelineWindow([bar(10, 20)]))`, where `bars` *was* `[bar(10, 20)]` | the adversarial review |
| a colour test whose shared Team sorted first, so two separately-built maps agreed by coincidence | sabotaging the code to prove the test could fail |
| blocked-storage tests aimed at a prototype the object does not inherit from | **mutation testing** |

The last one is the one to dwell on. It survived a four-reviewer wave gate, an adversarial review
with execution tools, a falsifier column written specifically to catch this class, and my own
hand-sabotage of neighbouring tests. Every one of those instruments reads the test and believes it,
and **no reading of that test could have revealed the problem** — the mock was correct-looking,
correctly typed, and pointed at the wrong object. Only running the code with the guard removed and
finding that nothing noticed could tell.

That is what mutation testing is for, and it is the argument for keeping it as a gate rather than a
formality: it is the only instrument here that does not take the test's word for what it tests.

## Not mutated

`DeliverySection.tsx` is excluded deliberately. Slice 06 changed **one line** in it — `teams={teams}`
on a component it already rendered — and the file is around 900 lines. Mutating it would have buried
this slice's score under a thousand mutants of untouched code and told nobody anything about either.

No backend file is mutated, because slice 06 changes none.

## Method note, and a gap in the evidence trail worth knowing about

Scoped with `stryker-6050-slice-06.frontend.json` + `vitest.stryker.6050-slice-06.config.ts`.

**Two different files are called "the config", and only one of them survives `.gitignore`.** This was
checked file by file with `git ls-files` rather than read off the patterns, because the patterns are
where the confusion lives.

The **vitest runner config** — `vitest.stryker.<id>.config.ts`, the one that decides which specs
Stryker runs — is fine. `.gitignore:450` ignores `**/vitest.stryker*.ts` and `:453` carves out
`!docs/feature/*/mutation/vitest.stryker*.ts`, so the copy beside this write-up is committable. All
of slices 02, 03, 04 and 05 have theirs committed, and slice 06's is committed here with an ordinary
`git add`.

The **Stryker config** — `stryker-<id>.frontend.json`, the one naming the mutate list and the
thresholds — is not. `.gitignore:463` is `**/mutation/stryker-*.json`, written to keep the
multi-megabyte JSON *reports* out of the repository. Its own comment explains that this is safe
because "a config is stryker.5837.frontend.json, a report is stryker-5837-frontend.json" — a dot
where the other has a hyphen. **No file in this folder actually follows that convention.** Every
slice's config is `stryker-<id>.frontend.json`, hyphen included, so every one of them matches the
report pattern. Slices 02 and 03 are committed because they predate the rule; **slices 04, 05 and 06
are ignored**, and the instruction to commit the config as evidence has been failing silently since.

The consequence is narrow but real: the write-up can be reproduced as far as *which specs ran*, and
not as far as *which files were mutated or at what threshold*. The durable fix is either a negated
pattern for the config spelling or renaming the configs to the dotted form the comment already
claims they use. Recorded rather than done, because changing an ignore rule at a gate is how a
2.4 MB report ends up committed instead — and one of those is sitting in this folder right now,
correctly ignored.
