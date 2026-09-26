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
| Somebody_who_cannot_read_the_Team_is_refused_and_learns_nothing_about_it | 1 | MISSING_FUNCTIONALITY | expected Forbidden, got NotFound |
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
