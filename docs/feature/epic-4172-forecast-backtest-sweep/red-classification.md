# RED classification: epic-4172-forecast-backtest-sweep (Forecast Reality Check)

Every pending scenario was un-ignored or un-skipped, run against unmodified production code, and then
ignored or skipped again; the files were restored byte for byte from a copy. Backend: one build, one run
of the four new fixtures together. Frontend: one run of the two new spec files together. Run on
2026-09-26 in the main checkout.

Classes: `MISSING_FUNCTIONALITY` is the correct RED - the assertion fires because the behaviour does not
exist yet. `BROKEN` is a setup, import or build failure. **There are none.** Every one of the 110 pending
cases reached its first assertion and failed on it; none failed on a parse, a null or a missing type,
because no scenario names a type this feature is about to add.

The one green scenario, `The_single_back_test_beside_the_check_still_answers_as_it_did`, passed in the
same run. It exercises the harness end to end - seeding, the pinned clock, the scripted forecast - so the
REDs below are REDs of the feature, not of the harness.

## Backend

### `API/Integration/ForecastRealityCheck/Slice01OneSentenceAboutYourSamplingWindowScenarios.cs`

Every scenario reads the answer through `ReadTheAnswer`, which asserts the check answered before it parses
anything. Before DELIVER the route does not exist, so each one fails there: `The reality check did not
answer ... Expected: OK But was: NotFound`. That refusal is the missing behaviour, not a harness fault -
the green back-test scenario in the same fixture proves the host, the seeding and the forecast script work.

| Scenario | Cases | Class | Fails on |
|---|---|---|---|
| Maria_runs_the_reality_check_on_Ocean_Explorer_and_gets_an_answer_without_giving_a_date | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| The_same_check_answers_on_the_versioned_route_as_well | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| The_check_answers_whether_or_not_the_forecast_filter_choice_is_given | 4 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_request_whose_filter_choice_is_not_yes_no_or_unset_is_refused_and_nothing_is_checked | 3 (a word / a number / cut-off JSON) | MISSING_FUNCTIONALITY | the presence leg - a well-formed request for the same Team - got NotFound; without it a missing route would pass as a refusal |
| Dates_sent_with_the_request_are_ignored_and_every_check_still_ends_today | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Tom_who_can_read_the_Team_but_not_change_it_gets_the_whole_answer | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Somebody_who_cannot_read_the_Team_is_refused_and_learns_nothing_about_it | 1 | MISSING_FUNCTIONALITY | expects NotFound (404) with no cells in the body — the single back-test's own refusal, so the answer never reveals that the Team exists. *(Corrected 2026-09-26: this row said Forbidden (403), which the scenario does not assert.)* A missing route also answers 404, so this scenario has no presence leg of its own; the route is proved by Tom's scenario above |
| A_Team_that_does_not_exist_is_answered_as_not_found | 1 | MISSING_FUNCTIONALITY | the presence leg - the check answering for a Team that does exist - got NotFound; without that leg this scenario would pass today |
| Every_check_ends_today_and_reaches_back_by_its_own_length | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| The_answer_states_exactly_what_it_checked_and_the_bar_each_check_had_to_clear | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_whose_window_is_on_the_standard_ladder_is_checked_sixteen_times | 4 (14 / 30 / 60 / 90) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_whose_window_is_off_the_ladder_has_it_checked_as_a_fifth_window | 3 (45 / 7 / 120) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| No_part_of_the_answer_can_rank_one_sampling_window_above_another | 2 (30 / 45) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Every_check_that_was_run_is_in_the_answer_including_the_ones_that_could_not_be_evaluated | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Every_window_behaving_alike_is_an_answer_that_says_the_setting_is_fine | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Deep_Currents_fourteen_day_window_over_forecast_three_times_in_four_and_sits_outside_the_region | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| The_windows_either_side_of_an_off_ladder_setting_can_hold_up_when_the_setting_itself_does_not | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| An_off_ladder_setting_can_hold_up_when_the_windows_either_side_of_it_do_not | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_window_that_fell_short_of_its_most_cautious_forecast_in_two_of_four_checks_is_outside_the_region | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Delivering_more_than_forecast_never_counts_against_a_window_only_falling_short_does | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_window_only_some_of_whose_checks_could_run_is_judged_on_the_ones_that_did | 4 (2/0, 2/1, 1/0, 1/1) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_window_that_could_not_be_checked_in_the_middle_of_the_ladder_is_a_gap_in_the_region | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| When_no_window_held_up_the_answer_says_so_rather_than_naming_the_least_bad | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_whose_history_supports_no_check_at_all_is_told_nothing_could_be_concluded | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| When_the_Teams_own_window_could_not_be_evaluated_its_standing_is_not_determined | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_level_is_expected_to_hold_as_often_as_its_own_percentage_of_the_checks | 4 (50 / 70 / 85 / 95) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Only_the_checks_that_could_run_count_towards_how_often_a_level_should_have_held | 4 (50 / 70 / 85 / 95) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_that_never_reached_even_its_most_cautious_forecast_is_told_no_level_ever_held | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_that_always_beat_its_most_optimistic_forecast_is_told_which_levels_always_held_when_a_miss_was_expected | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Each_check_says_where_the_Teams_actual_landed_against_its_forecast | 5 (NoLevel ... EveryLevel) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Coastal_Surveys_fourteen_day_checks_hold_too_few_days_of_finished_work_and_are_named_as_unable_to_run | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Five_days_with_finished_work_is_enough_to_check_and_four_is_not | 2 (5 / 4) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_check_whose_forecast_could_not_be_worked_out_is_named_for_that_reason_and_counts_for_nothing | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_Team_forecasting_from_fixed_dates_is_checked_at_the_standard_windows_and_told_its_own_setting_was_not_tested | 2 (stored 45 / 0) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| A_stored_window_that_is_not_a_length_of_time_adds_nothing_to_the_check | 2 (0 / -7) | MISSING_FUNCTIONALITY | expected OK, got NotFound |
| Running_the_check_changes_nothing_about_the_Team_or_its_Work_Items | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound; the "nothing changed" leg is behind it, so it cannot pass against a product that has no check at all |
| Running_the_check_twice_gives_the_same_answer_twice | 1 | MISSING_FUNCTIONALITY | expected OK, got NotFound |

### `API/Integration/ForecastRealityCheck/RealityCheckQueryCountTest.cs`

| Scenario | Cases | Class | Fails on |
|---|---|---|---|
| One_check_on_a_cold_cache_reads_the_Teams_finished_work_once_per_window_it_asks_about | 2 (30 days -> 20, 45 days -> 24) | MISSING_FUNCTIONALITY | the test-side seam `TheProductionSweepRunsOnce` throws an `AssertionException` naming the missing service and the shape it needs; the SQLite file, the seeding and the query counter all ran before it |

### `Architecture/RealityCheckReadOnlyArchUnitTest.cs`

| Scenario | Class | Fails on |
|---|---|---|
| The_sweep_and_its_verdict_rules_exist_where_these_rules_look_for_them | MISSING_FUNCTIONALITY | `RealityCheckVerdictPolicy` not found (expected not null) and no `ForecastRealityCheckService` |
| Nothing_in_the_reality_check_can_reach_a_repository | MISSING_FUNCTIONALITY | ArchUnitNET: "The rule requires positive evaluation" - no type named `*RealityCheck*` exists to judge |
| Nothing_in_the_reality_check_can_reach_the_database_directly | MISSING_FUNCTIONALITY | same |
| The_verdict_rules_depend_on_no_service | MISSING_FUNCTIONALITY | same |

These three dependency rules would be vacuous without that ArchUnitNET behaviour; with it, they fail
until the types exist and then judge them. Run them in **Release** before the first push that turns them
on (see `docs/ci-learnings.md`, 2026-08-22): an edge that exists only inside an `async` method is visible
in Debug and gone in Release.

### `Integration/UsageData/TeamForecastRealityCheckRunEventTests.cs`

Before DELIVER the name `TeamForecastRealityCheckRun` does not bind, so every message under it gets a 400
and nothing reaches the collector. That refusal is the missing behaviour.

| Scenario | Cases | Class | Fails on |
|---|---|---|---|
| A_browser_that_agreed_reports_a_reality_check_as_one_event_carrying_only_its_name | 1 | MISSING_FUNCTIONALITY | expected NoContent, got BadRequest; 0 messages at the collector |
| Nothing_leaves_a_browser_that_did_not_agree_when_it_runs_a_reality_check | 2 (declined / never asked) | MISSING_FUNCTIONALITY | the agreed-browser control sent 0 messages, expected 1 - without that control the scenario would pass today |
| Nothing_is_forwarded_while_the_administrator_has_stopped_usage_data | 1 | MISSING_FUNCTIONALITY | the before-the-veto control sent 0 messages, expected 1 |
| A_reality_check_event_carrying_anything_but_its_name_is_refused | 3 (route / kind of system / setting) | MISSING_FUNCTIONALITY | the plain control event got BadRequest, expected NoContent |
| The_event_is_the_twelfth_on_the_list_and_the_usage_data_page_describes_it | 1 | MISSING_FUNCTIONALITY | the name is not on the enum; the page has no line for it |

## Frontend

### `src/pages/Teams/Detail/TeamForecastView.realityCheck.test.tsx`

The Team Forecast tab renders and the Forecast Backtesting group is found (`getByRole("region", { name:
"Forecast Backtesting" })` succeeds in every case), then each spec fails looking for the check's button:
`Unable to find an accessible element with the role "button" and name /^run reality check$/i`. The group's
only accessible roles today are its heading, which is exactly what "the check is not there" looks like.

| Spec | Cases | Class |
|---|---|---|
| slice 01 - all 17 specs (walking skeleton, whole range, Deep Current, hole in the region, a window not checked mid-ladder, no window held up, denominator at 16 and at 20, levels against nominal rate, never held, unevaluable named, nothing concluded, fixed dates, two findings, no write control, terminology, failed request, second press) | 18 | MISSING_FUNCTIONALITY |
| slice 02 - all 8 specs (four panels in order, fifth panel, band and actual, thin-history row, no-reading row, all-unevaluable panel, nominal-rate lines, denominator stays) | 8 | MISSING_FUNCTIONALITY |

### `src/pages/Teams/Detail/TeamForecastView.realityCheck.usageData.test.tsx`

| Spec | Class | Fails on |
|---|---|---|
| is on the list of names the browser may send, as the word itself | MISSING_FUNCTIONALITY | `expected undefined to be 'TeamForecastRealityCheckRun'` |
| reports a run once, after the answer came back and not when the button was pressed | MISSING_FUNCTIONALITY | no Run reality check button |
| reports a run whose every check was too thin to evaluate | MISSING_FUNCTIONALITY | no Run reality check button |
| reports a run for a Team that forecasts from fixed dates | MISSING_FUNCTIONALITY | no Run reality check button |
| reports nothing for a request that failed | MISSING_FUNCTIONALITY | no Run reality check button - the "nothing reported" leg sits behind the press, so it cannot pass against a tab with no check |
| never reports a reality check as a forecast run by hand | MISSING_FUNCTIONALITY | no Run reality check button |

## Re-run after the DESIGN amendments (2026-09-26)

Re-run the same way after DES-14..DES-18 and the maintainer's two answers (rule A; `AboutRight` renamed
`SometimesHeld`). No classification changed: every scenario, including the four new region boundary
scenarios, the second fixed-dates case and the new frontend mid-ladder spec, still fails first on the
check not answering (backend: expected OK, got NotFound) or on the missing Run reality check button
(frontend). The assertions the amendments changed - the 95% readings, `NotTested` with its reason,
`NotEvaluated`, `unevaluatedWindowDays`, `SomeWindowsSound` - sit behind that first assertion, so they
will be reached, and judged, only once the endpoint answers. Totals: backend 78 pending of 79 cases, the
one green unchanged; frontend 32 pending.

## Not yet written, and why

- **Slice 03 (the Copy as Markdown one-pager)** was deferred by the maintainer on 2026-09-26 while the
  reporting format is re-evaluated. It has no scenarios; the six drafted earlier in this wave were
  deleted before this classification was final.

- **The Playwright walking skeleton** is listed in the feature delta as pending and not written: the card
  does not exist, and a Page Object locator written against markup nobody has rendered is exactly the
  unrun spec the project rules forbid. It is owed at DELIVER of slice 01, on seeded demo data.

---

# Story #6094 — the graded results dialog (slices 04 and 05)

Run on 2026-09-26 in the main checkout, against unmodified production code plus the DISTILL scaffolds.
Every pending case was un-skipped or un-ignored by a script that copied each file first, ran it once, and
copied it back byte for byte (`cmp` confirmed each restore). Frontend: one Vitest run of the six touched
spec files together. Backend: one build and one run of the slice 04 fixture.

**99 new cases: 98 run, 98 RED, 0 BROKEN, 0 passing.** The one case not run is the Playwright walking
skeleton, which cannot be run before the dialog exists (see its row). No case failed on an import, a type,
a parse or a harness fault: the scaffolds load, the fixture builds the answer, the tab renders and the
**Run reality check** button is pressed in every frontend case before the first failure.

| Class | Meaning here |
|---|---|
| `MISSING_FUNCTIONALITY` (dialog) | The press succeeds; the next step, `findByRole("dialog", { name: /reality check/i })`, finds nothing, because the answer still renders inline. That is the absent dialog, not a harness fault: the tab, the button and the stand-in service all worked |
| `MISSING_FUNCTIONALITY` (scaffold) | The call reaches `realityCheckGrading.ts` and its scaffold throws `Not yet implemented -- RED scaffold: …`, naming the question and the facts it was asked about |
| `MISSING_FUNCTIONALITY` (assertion) | An assertion fires on a value the production code does not produce yet |

## Backend - `API/Integration/ForecastRealityCheck/Slice04TheAnswerOpensInADialogScenarios.cs`

Every scenario gets an answer (the check exists and answers 200), then fails on the harness's field
lookup: `The answer carries no 'scoredPeriods' where the contract puts one`. The reader then treats the
periods as none, so the scenario's other legs fail as assertions too ("one period per horizon …"), never
as an exception. The legs that do not read `scoredPeriods` - no check carries an actual, twenty checks
were run - pass, which shows the harness and seeding are sound.

| Scenario | Class | Fails on |
|---|---|---|
| Every_period_the_check_scored_is_in_the_answer_with_its_days_and_what_the_Team_delivered | MISSING_FUNCTIONALITY | no `scoredPeriods` on the answer |
| Every_check_that_could_be_evaluated_carries_the_same_actual_as_its_period | MISSING_FUNCTIONALITY | no `scoredPeriods` on the answer |
| A_Team_whose_history_supports_no_check_still_gets_four_periods_each_with_what_it_delivered | MISSING_FUNCTIONALITY | no `scoredPeriods`; "every period says what the Team delivered in it …" |
| A_week_in_which_the_Team_delivered_nothing_is_a_period_whose_actual_is_zero_not_missing | MISSING_FUNCTIONALITY | no `scoredPeriods`; "every period says what the Team delivered in it, zero included …" |
| A_Team_whose_own_window_is_off_the_ladder_gets_one_period_per_horizon_never_one_per_window | MISSING_FUNCTIONALITY | no `scoredPeriods`; "one period per horizon …" (its twenty-checks leg passes) |

## Frontend - `src/pages/Teams/Detail/TeamForecastView.realityCheck.dialog.test.tsx` (slice 04)

All 31 cases: `MISSING_FUNCTIONALITY` - `Unable to find role="dialog" and name /reality check/i`.

| Spec | Cases |
|---|---|
| opens the dialog at once, saying the check is running, and fills in without asking for a date | 1 |
| opens on one line per confidence level, then the window sentence, the findings, the denominator, above the table | 1 |
| @error never held = over-forecasting, always held = under-forecasting | 1 |
| @error a Team whose history supports no check: no level tested, the table still stands | 1 |
| keeps nothing behind a toggle, a tooltip or a disclosure | 1 |
| one row per check, grouped by period in horizon order, windows in ladder order (16 / 20) | 2 |
| each period's actual printed once, in its header, with its first and last day | 1 |
| every forecast with its value, its miss in Work Items and whether it held (Ocean Explorer, 8 weeks) | 1 |
| @boundary delivered exactly: a miss of 0, held | 1 |
| @error a check that could not run: its reason across all four columns | 1 |
| @error a forecast that could not be worked out: its own reason | 1 |
| @error a period in which no window could be checked still shows its actual | 1 |
| the Team's own window is "your setting" in every period | 1 |
| @error a Team whose setting was not tested has no "your setting" row | 1 |
| keeps the run order when the checks arrive shuffled | 1 |
| the four level columns with their confidence names | 1 |
| a real table: caption, row-group and row headers | 1 |
| nothing tallies, orders or picks out a window; no sorting | 1 |
| no control that could change a Team setting | 1 |
| the instance's own words, and no tracker's | 1 |
| the Backtesting group keeps only the button once the dialog is closed | 1 |
| Run again asks again and fills in afresh | 1 |
| closing and reopening asks again | 1 |
| @error while running, neither Run again nor reopening starts a second check | 1 |
| @error a failed check: plain message and Run again in the dialog, no answer | 1 |
| @error Run again after a failure can bring the answer | 1 |
| Escape closes and puts focus back on Run reality check | 1 |
| the visible close control closes it too | 1 |
| focus moves into the dialog; the answer arriving moves it nowhere | 1 |
| on a narrow screen: full screen, nothing dropped | 1 |

## Frontend - `src/pages/Teams/Detail/TeamForecastView.realityCheck.grading.test.tsx` (slice 05)

All 26 cases: `MISSING_FUNCTIONALITY` - `Unable to find role="dialog" and name /reality check/i`. The
grading sits behind the dialog, so these reach their own assertions only once slice 04 is delivered; they
were written so that nothing in them depends on slice 05 existing before that point.

| Spec | Cases |
|---|---|
| a forecast that held is shaded by how close it landed (26% / 14% / 5%) | 1 |
| a forecast that did not hold is graded by how far it fell short (14%) | 1 |
| @error a one-Work-Item miss beside 33% | 1 |
| @error nothing delivered: no percentage; 0 held exactly, 2 did not hold by the most | 1 |
| @boundary 10% / 11% / 25% / 26% / 1% at the band edges | 5 |
| every graded cell says in words whether it held and by how much | 1 |
| @error a check that could not run takes no grade colour | 1 |
| Maria's 85th: held 15 of 16, within 10% in 3, usually low by more than a quarter | 1 |
| misses within 10% count as within 10% | 1 |
| @boundary no "usually" when within-10% is split between held and not held | 1 |
| @boundary no "usually" at exactly half | 1 |
| the "usually" words of each of the six grades | 6 |
| @error checks that could not run are left out of the counts | 1 |
| @error a level no check could test: no count, no "usually" | 1 |
| no level line names, counts or ranks a window | 1 |
| the legend names six grades and "Not checked" | 1 |
| Nick Brown credited, this product's additions named | 1 |

## Frontend - `src/pages/Teams/Detail/realityCheckGrading.test.ts` (slices 04 and 05)

All 26 cases: `MISSING_FUNCTIONALITY` (scaffold) - `Not yet implemented -- RED scaffold: the miss in
Work Items …` (4), `… the grade of one check …` (17, including the exhaustive 1..200 x 0..400 property,
which fails on its first call), `… how close one level landed …` (5).

## Frontend - other files

| File | Spec | Class | Fails on |
|---|---|---|---|
| `TeamForecastView.realityCheck.usageData.test.tsx` | all 7 dialog cases (first open, Run again, reopen after an answer, @error reopen adopting the running check, @error dropped answer, @error failed Run again, never a manual forecast run) | MISSING_FUNCTIONALITY | no dialog |
| `utils/theme/colors.test.ts` | every grade's text reads at 4.5 : 1; no grade in a level colour, no two grades alike | MISSING_FUNCTIONALITY | `appColors has no forecastGrade fills: expected undefined to be defined` |
| `services/Api/ForecastService.test.ts` | keeps every period the check scored as it travels | MISSING_FUNCTIONALITY | `expected undefined to deeply equal [ { horizonDays: 7, … } ]` - the parse drops the field today |

## E2E - `Lighthouse.EndToEndTests/tests/specs/teams/TeamsDetail.spec.ts`

| Step | Class | Why it was not run |
|---|---|---|
| @walking_skeleton "Forecast reality check opens in a dialog" (`test.step.skip`) | not run - pending | It needs the dialog in a running Lighthouse; against today's build it could only fail on the missing dialog, which the Vitest cases above already show. It is un-skipped and **run locally before commit at DELIVER of slice 04**, in the same commit that deletes the step above it and the card-scoped locators. The rest of the shared visit ran as before (`playwright test --list` loads the spec; `tsc` over the E2E project is clean) |

## Existing tests DELIVER retires or rewrites (not run here, still green)

These stay green today and are **not** touched by DISTILL. They go in the slice 04 commit that adds the
dialog (6094-DES-9), except where noted: every slice 01 spec in `TeamForecastView.realityCheck.test.tsx`
that finds the answer inside the Backtesting group (rewritten through the dialog or deleted where a
dialog spec above replaces it); all eight slice 02 specs in that file (deleted); `RealityCheckBandRow.test.ts`
(deleted); the `bandDescription` and `actualDescription` cases in `realityCheckCopy.test.ts` (deleted) and
its `levelLine` cases (rewritten to the "should be about" form); the five pre-dialog specs in
`TeamForecastView.realityCheck.usageData.test.tsx` (moved into the dialog; the failed-request spec stops
looking for a snackbar); the existing "Forecast reality check" Playwright step and the `realityCheckVerdict`
/ `realityCheckDenominator` locators (deleted). At slice 05, the slice 04 level-line and cell-name pins in
`TeamForecastView.realityCheck.dialog.test.tsx` are rewritten to the graded form.

# Story #6094, US-06 — the reality check reads at a glance (slices 06a and 06b)

Run on 2026-09-27 in the main checkout, against unmodified production code plus one DISTILL scaffold
(`heldShare` in `realityCheckGrading.ts`). Every pending case was un-skipped or un-ignored in a copy - the
frontend files as `*.unskipped.test.ts(x)` beside the originals, the backend scenarios file by deleting its
`[Ignore(Pending)]` lines after saving a copy - run once, and the copy deleted or the original restored
(`cmp` confirmed the backend restore). Frontend: one Vitest run per spec file. Backend: one build and one
run of the new fixture.

**84 new runnable cases: 84 RED, 0 BROKEN, 0 passing.** The Playwright step is the one pending case not
run (see its row). No case failed on an import, a type, a parse or a harness fault. Two new cases are
green on purpose and are not pending: the trigger file's back-test harness check and the compact file's
unevaluable-row guard.

| Class | Meaning here |
|---|---|
| `MISSING_FUNCTIONALITY` (words) | The dialog renders the answer as shipped; an assertion on the new words, glyph-only cells, headline, badge or level row fires on the old text instead |
| `MISSING_FUNCTIONALITY` (element) | A lookup the new markup satisfies finds nothing today: the "What does the reality check do?" icon, the "About these numbers" icon, the named scroll region, a level bar `img`, a tooltip. Testing Library reports it as an element-not-found error thrown from the assertion, the same class the slice 04 specs failed with on the missing dialog |
| `MISSING_FUNCTIONALITY` (scaffold) | The call reaches `heldShare` and its scaffold throws `Not yet implemented -- RED scaffold: the share of … checks a level held in, … of them held` |
| `MISSING_FUNCTIONALITY` (log) | The check answers 200, and the log holds no line of a reality check: `Expected … with 1 elements, actual … with 0 elements` |

Harness proof, run in the same session: the reality-check acceptance host's new log capture saw 13 lines
during start-up and seeding (a one-off probe, deleted after the run), so the backend REDs are the missing
line and not an inert capture. The trigger file's green harness case brings a real back-test result into
the group, so its REDs are placement and not a harness that cannot render the back-test forecaster.

## Backend - `API/Integration/ForecastRealityCheck/Slice06AnOperatorSeesEveryRealityCheckScenarios.cs` (06a)

| Scenario | Cases | Class | Fails on |
|---|---|---|---|
| Every_check_run_writes_one_line_an_operator_sees_naming_the_Team_and_the_filter_choice_asked_for | 6 (latest: unset, null, on, off; versioned: unset, on) | MISSING_FUNCTIONALITY (log) | no line of a reality check at Information, after an answer of 200 |
| A_check_asked_for_a_Team_that_does_not_exist_writes_no_line | 1 | MISSING_FUNCTIONALITY (log) | its presence leg: the check for the Team that exists wrote no line |
| A_request_whose_filter_choice_cannot_be_read_is_refused_and_writes_no_line | 2 (a word / cut-off JSON) | MISSING_FUNCTIONALITY (log) | its presence leg, as above |
| A_check_refused_to_somebody_who_cannot_read_the_Team_writes_no_line | 1 | MISSING_FUNCTIONALITY (log) | its presence leg, as above |

Each negative scenario runs a logged check first, so it cannot pass on a capture that sees nothing, and
never calls `Clear()` between that check and its "nothing logged" assertion.

## Frontend - `src/pages/Teams/Detail/TeamForecastView.realityCheck.trigger.test.tsx` (06a)

| Spec | Cases | Class | Fails on |
|---|---|---|---|
| "Run reality check" sits after the back-test inputs, its explanation just before it, above a single back-test result | 1 | MISSING_FUNCTIONALITY (words) | `expected false to be true` - the button sits before the inputs |
| with the forecast filter switch shown, the trigger comes after the switch too | 1 | MISSING_FUNCTIONALITY (words) | `expected false to be true` |
| on keyboard focus / on hover the info icon explains what the check does | 2 | MISSING_FUNCTIONALITY (element) | no button named "What does the reality check do?" |
| @error pressing the info icon opens nothing and runs no check | 1 | MISSING_FUNCTIONALITY (element) | same |
| pressing "Run reality check" from its new place still opens the dialog | 1 | MISSING_FUNCTIONALITY (element) | same |
| the explanation speaks the instance's own words | 1 | MISSING_FUNCTIONALITY (element) | same |
| *(green)* the single back-test beside the check still shows its result once an input changes | 1 | passes | - |

## Frontend - `src/pages/Teams/Detail/TeamForecastView.realityCheck.compact.test.tsx` (06a)

| Spec | Cases | Class | Fails on |
|---|---|---|---|
| the first run shows a spinner and "Crunching the numbers…"; the answer moves no focus | 1 | MISSING_FUNCTIONALITY (words) | `expected 'Checking Ocean Explorer's forecasts …' to be 'Crunching the numbers…'` |
| Run again shows the same spinner and words | 1 | MISSING_FUNCTIONALITY (words) | same |
| @error a check that fails replaces the spinner with its plain message | 1 | MISSING_FUNCTIONALITY (words) | same, before the failure |
| every period header in de-CH / en-US | 2 | MISSING_FUNCTIONALITY (words) | `1 week, 16. Sept. 2026 to 22. Sept. 2026: …` - the pinned locale reaches the formatter, so the month name is the old format and not a harness fault |
| @boundary one Work Item completed, in the singular | 1 | MISSING_FUNCTIONALITY (words) | the old header |
| @error no scored period: the horizon alone | 1 | MISSING_FUNCTIONALITY (words) | `expected '1 week' to be 'Forecast Horizon: 1 week'` |
| Ocean Explorer's 30-day row over 8 weeks: ✗ 48, ✓ 40, ✓ 36, ✓ 31 and their names | 1 | MISSING_FUNCTIONALITY (words) | `'48 −6 14% ✗ did not hold'` |
| the seven cell wordings (more / fewer by one, exactly, exactly one, 0 vs 0, 0 vs 2, 0 vs 1) | 7 | MISSING_FUNCTIONALITY (words) | the old visible cell, e.g. `'14 +1 7% ✓ held'` |
| every graded cell is a keyboard stop showing only glyph and forecast | 1 | MISSING_FUNCTIONALITY (words) | no `tabindex` on the cell |
| on keyboard focus / on hover a cell shows its comparison in a tooltip | 2 | MISSING_FUNCTIONALITY (words / element) | the old visible cell; no tooltip |
| @error a level left out shows "—" and is a keyboard stop | 1 | MISSING_FUNCTIONALITY (words) | `expected '' to be '—'` |
| @error Escape dismisses a cell's tooltip and leaves the dialog open | 1 | MISSING_FUNCTIONALITY (words) | the keyboard never reaches the cell |
| the legend is two titled rows over the six fills | 1 | MISSING_FUNCTIONALITY (words) | no "Forecast held" |
| the credit line and "Not checked" are gone from under the legend | 1 | MISSING_FUNCTIONALITY (words) | the credit paragraph is still there |
| no caption; the scroll region is named for the Team | 1 | MISSING_FUNCTIONALITY (element) | no region named "Forecasts checked for Ocean Explorer, by forecast horizon and sampling window" |
| the Backtesting group holds the trigger and its explanation | 1 | MISSING_FUNCTIONALITY (words) | one button, not two |
| the headers, cells and region name in the instance's words | 1 | MISSING_FUNCTIONALITY (words) | the old header |
| *(green)* @error a check that could not run keeps its reason and is no keyboard stop | 1 | passes | - |

## Frontend - `src/pages/Teams/Detail/TeamForecastView.realityCheck.summary.test.tsx` (06b)

| Spec | Cases | Class | Fails on |
|---|---|---|---|
| only the headline, badge and four level rows above the table, ≤ 50 words | 1 | MISSING_FUNCTIONALITY (words) | no line reads "Backtested 16 scenarios · 64 forecasts" |
| none of the replaced sentences is said anywhere | 1 | MISSING_FUNCTIONALITY (words) | "How often each confidence level held" is still there |
| the headline in its five forms (16, 20, 12 of 16, none of 16, one scenario at one level) | 5 | MISSING_FUNCTIONALITY (words) | no line reads the headline |
| Maria's 85th: bar at 94% against 85%, "94% (15 of 16) · 3 accurate" | 1 | MISSING_FUNCTIONALITY (element) | no `img` named by the bar's text alternative |
| four level rows in ascending order | 1 | MISSING_FUNCTIONALITY (words) | `expected [] to deeply equal [ …(4) ]` |
| @error never held / always held get no extra words | 1 | MISSING_FUNCTIONALITY (words) | no line reads "0% (0 of 16) · 0 accurate" |
| @error Kelp Farm: plain headline, could-not-be-checked badge, four levels not tested | 1 | MISSING_FUNCTIONALITY (words) | no headline |
| the badge in each of nine verdict states and its tone | 9 | MISSING_FUNCTIONALITY (words) | `no badge reads "…"` |
| the badge on keyboard focus: its name, what held up means, the window not checked | 1 | MISSING_FUNCTIONALITY (words) | no badge |
| @error the Team's own window not checked is named among those not counted | 1 | MISSING_FUNCTIONALITY (words) | no badge |
| @boundary every window checked: the tooltip only says what held up means | 1 | MISSING_FUNCTIONALITY (words) | no badge |
| nothing names, orders or scores a window - Deep Current | 1 | MISSING_FUNCTIONALITY (words) | no badge |
| hovering the info icon names it and opens nothing | 1 | MISSING_FUNCTIONALITY (element) | no button named "About these numbers" |
| a click / Enter / Space opens the explanation | 3 | MISSING_FUNCTIONALITY (element) | same |
| the article is the next Tab stop and opens in a new tab | 1 | MISSING_FUNCTIONALITY (element) | same |
| Escape closes the explanation first, then the dialog | 1 | MISSING_FUNCTIONALITY (element) | same |
| @error Kelp Farm's explanation: all 16 left out, and why | 1 | MISSING_FUNCTIONALITY (element) | same |
| @error Coastal Survey's explanation: how many and why | 1 | MISSING_FUNCTIONALITY (element) | same |
| the only control the summary adds is the explanation's icon | 1 | MISSING_FUNCTIONALITY (words) | `expected [] to have a length of 1` |
| fixed dates, in the instance's word for Team | 1 | MISSING_FUNCTIONALITY (words) | no badge |
| the explanation in the instance's words | 1 | MISSING_FUNCTIONALITY (element) | no "About these numbers" |

## Frontend - `src/pages/Teams/Detail/realityCheckGrading.test.ts` (06b)

All 7 cases: MISSING_FUNCTIONALITY (scaffold) - `@error` no share without a check (1), the five worked
shares (5), and the exhaustive 1..200 property, which fails on its first call (1).

## E2E - `Lighthouse.EndToEndTests/tests/specs/teams/TeamsDetail.spec.ts`

| Step | Class | Why it was not run |
|---|---|---|
| @walking_skeleton "Forecast reality check reads at a glance" (`test.step.skip`) | not run - pending | It needs the summary in a running Lighthouse; against today's build it could only fail on the missing headline, which the Vitest cases above already show. `tsc` over the E2E project is clean and `playwright test --list` loads the spec. DELIVER of 06b un-skips it, **runs it locally before commit**, and deletes the step above it with `realityCheckLevelLine` and `realityCheckDialogDenominator` in the same commit |

## Re-run after the four-reviewer gate (2026-09-27)

**The expected log line stays unquoted, and DESIGN now makes that true.** Serilog quotes a string value in
the rendered message, so the template as first designed would have written `(filter override: "on")` while
`Slice06AnOperatorSeesEveryRealityCheckScenarios.cs` and its specifications expect `(filter override: on)`.
DESIGN's template now reads `{FilterOverride:l}` (6094-DES-16, P-US06-10, `wave-decisions.md`), after the
`{EntityType:l}` precedent in `UpdateServiceBase`. The backend scenarios are unchanged, still pending, and
still RED on the missing line.

Every tightened frontend case was un-skipped in a copy and run once under `TZ=Europe/Zurich`, then the copy
deleted. **6 cases: 6 RED, 0 BROKEN.**

| Spec | Tightened by | Class | Fails on |
|---|---|---|---|
| `…compact` *in de-CH / en-US every period header reads …* (2) | the hour of every day formatted must be local midnight | MISSING_FUNCTIONALITY (words) | the old header, before the new assertion is reached. A day parsed through UTC is 02:00 in Zurich and still prints the same date, so only the hour tells the two apart |
| `…compact` *every graded cell is a keyboard stop …* | the more and fewer names split into two anchored patterns, `+` only with more, `−` only with fewer | MISSING_FUNCTIONALITY (words) | no `tabindex` on the cell |
| `…compact` *@error Escape dismisses a cell's tooltip …* | focus must still be on the cell afterwards; a closing dialog lingers through its exit animation, while closing it hands focus back to the trigger | MISSING_FUNCTIONALITY (words) | the keyboard never reaches the cell |
| `…trigger` *@error pressing the info icon opens nothing …* | no `.MuiPopover-root` in the document and no `aria-expanded="true"` on the icon | MISSING_FUNCTIONALITY (element) | no button named "What does the reality check do?" |
| `…summary` *@error the Team's own window not checked …* | the "held up" definition must be present, not only nothing else | MISSING_FUNCTIONALITY (words) | no badge reads "Your 30-day sampling window: could not be checked" |
