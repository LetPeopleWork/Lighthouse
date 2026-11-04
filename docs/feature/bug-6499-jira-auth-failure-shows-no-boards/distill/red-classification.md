# Bug 6499: DISTILL pre-DELIVER RED classification

Feature-id: `bug-6499-jira-auth-failure-shows-no-boards` | Wave: DISTILL | Date: 2026-11-04 | Scope: slices 01 and 02.

Copy ids (F1, F2, E1, T1, D1) are the ones in "Maintainer decisions: simplified copy" in `feature-delta.md`.

## Procedure and evidence

Each test was un-skipped once on a copy, run against today's production code, classified, and re-skipped. The
runs were then repeated against a throwaway fix in the production files, restored afterwards, to prove each test
can go green and reaches the failure path it names. A test failing with "no exception thrown" alone would not
prove that.

| Check | Result |
|---|---|
| Backend, 2 classes un-skipped, today's code | 66 cases: 58 failed, 8 passed |
| Backend, same, throwaway fix | 66 of 66 passed |
| Frontend, new tests un-skipped, today's code | 26 cases: 19 failed, 7 passed |
| Frontend, same, throwaway fix | 26 of 26 passed |
| Existing Jira, Azure DevOps, wizard and health tests, throwaway fix | 861 passed, 22 failed: 8 pin the quiet degrade, 14 fail through one fixture line. With that line changed, the two classes concerned fail on exactly the 8. See "Tests DELIVER changes" in `feature-delta.md` |
| Hand-off state, backend | `dotnet build` 0 warnings, 0 errors. New classes, `WizardsControllerTest` and `Architecture`: 349 passed, 66 skipped, 0 failed |
| Hand-off state, frontend | `pnpm biome check ./src` clean, `pnpm tsc -b` clean. The 4 touched files: 61 passed, 26 skipped, 0 failed, 0 time-budget offenders |

Legend: **RED** fails today because the behaviour is missing, which is the correct signal.
**PASS-WHEN-ENABLED** passes today: a guard on behaviour the fix must keep, skipped only for one-at-a-time
enabling, and classified honestly. No test is BROKEN (setup, import or fixture failure).

## Backend: `JiraBoardReadFailureTest` (slice 01), 43 cases

| Test | Cases | Class | Reason |
|---|---|---|---|
| `An_administrator_asking_for_the_boards_of_a_Jira_that_refuses_them_is_told_what_Jira_said` | 1 | RED | controller answers 200 with an empty list, not a 400 carrying F1 |
| `An_administrator_asking_for_a_board_of_a_Jira_that_refuses_it_is_told_what_Jira_said` | 1 | RED | controller answers 200 with an empty board, not a 400 carrying F2 |
| `A_board_list_Jira_answers_with_a_failure_carries_that_answer` (401, 403, 503) | 3 | RED | non-success returns `[]`, no `JiraReadException` |
| `A_board_list_from_a_Jira_that_cannot_be_reached_reads_as_no_answer` | 1 | RED | bare `catch` returns `[]` |
| `A_board_list_that_times_out_reads_as_no_answer` | 1 | RED | bare `catch` returns `[]` |
| `A_board_list_Lighthouse_cannot_read_says_so` (page of HTML, entry with no id) | 2 | RED | bare `catch` returns `[]` |
| `A_failure_on_the_second_page_fails_the_whole_board_list` | 1 | RED | non-success on page 2 returns `[]` |
| `A_board_list_over_two_pages_still_offers_every_board` | 1 | PASS-WHEN-ENABLED | paging guard; shows the second page is really requested |
| `A_Jira_that_really_has_no_boards_for_this_account_stays_an_empty_list` | 2 | PASS-WHEN-ENABLED | an empty list must stay empty |
| `A_failed_board_list_is_logged_with_what_Jira_answered_and_not_as_a_bad_credential` | 1 | RED | no exception; the log line still says "Authentication is not valid" |
| `A_board_Jira_answers_with_a_failure_carries_that_answer` (401, 403, 404, 503) | 4 | RED | non-success `/configuration` returns an empty `BoardInformation` |
| `A_board_from_a_Jira_that_cannot_be_reached_reads_as_no_answer` | 1 | RED | generic `catch` returns an empty `BoardInformation` |
| `A_board_read_that_times_out_reads_as_no_answer` | 1 | RED | generic `catch` returns an empty `BoardInformation` |
| `A_board_Lighthouse_cannot_read_says_so` (page of HTML, `{}`) | 2 | RED | generic `catch` returns an empty `BoardInformation` |
| `A_board_Jira_hands_over_only_in_part_carries_the_answer_of_the_part_it_refused` (status list, work items, work item type list; 403, 503) | 6 | RED | the part is logged and the board comes back without its states or types, no exception |
| `A_part_of_a_board_Lighthouse_cannot_read_says_so` (the same three parts; page of HTML, `{}`) | 6 | RED | no exception: a status or type list is logged and left out, unusable work items end in the generic `catch` |
| `A_part_of_a_board_that_cannot_be_reached_reads_as_no_answer` (the same three parts) | 3 | RED | no exception, as above |
| `A_part_of_a_board_that_times_out_reads_as_no_answer` (the same three parts) | 3 | RED | no exception, as above |
| `A_board_Jira_hands_over_whole_still_gives_its_states_and_types` | 1 | PASS-WHEN-ENABLED | healthy guard; also proves the fixture answers every part, so the failures above are the named part's |
| `A_board_that_holds_no_work_items_yet_is_a_real_answer_and_not_a_failure` | 1 | PASS-WHEN-ENABLED | an empty board must not become a failure when the quiet returns go |
| `Test_connection_on_a_refused_credential_says_to_check_all_the_sign_in_details` | 1 | RED | message is still "Authentication failed for Jira." |

## Backend: `AzureDevOpsBoardReadFailureTest` (slice 02), 23 cases

| Test | Cases | Class | Reason |
|---|---|---|---|
| `An_administrator_asking_for_the_boards_of_an_Azure_DevOps_that_refuses_them_is_told_what_it_said` | 1 | RED | controller answers 200 with an empty list |
| `An_administrator_asking_for_a_board_of_an_Azure_DevOps_that_refuses_it_is_told_what_it_said` | 1 | RED | controller answers 200 with an empty board |
| `A_rejected_token_on_the_only_project_fails_the_list_and_names_the_project` | 1 | RED | per-project `catch` returns `[]` |
| `One_project_failing_among_others_fails_the_whole_list_and_names_only_that_project` | 1 | RED | the working project's boards are returned as if complete |
| `Two_projects_failing_are_both_named` | 1 | RED | per-project `catch` returns `[]` |
| `A_status_Azure_DevOps_sent_without_a_sentence_of_its_own_still_reaches_the_picker` | 1 | RED | per-project `catch` returns `[]` |
| `Each_failing_project_keeps_its_line_in_the_error_log` | 1 | RED | no exception yet; the log half already holds and must keep holding |
| `A_project_list_nobody_answered_reads_as_no_answer` (unreachable, 2 timeouts) | 3 | RED | outer `catch` returns `[]` |
| `A_project_nobody_answered_for_reads_as_no_answer_and_is_named` (unreachable, 2 timeouts) | 3 | RED | per-project `catch` returns `[]` |
| `A_board_nobody_answered_for_reads_as_no_answer` (unreachable, 2 timeouts) | 3 | RED | `catch` returns an empty `BoardInformation` |
| `A_malformed_board_id_is_a_failed_read_and_never_an_empty_board` (4 ids) | 4 | RED | `Length != 2` guard returns an empty `BoardInformation` |
| `An_organisation_with_no_projects_stays_an_empty_list` | 1 | PASS-WHEN-ENABLED | an empty list must stay empty |
| `Projects_without_boards_stay_an_empty_list` | 1 | PASS-WHEN-ENABLED | an empty list must stay empty |
| `Boards_of_every_project_that_answers_are_offered` | 1 | PASS-WHEN-ENABLED | healthy guard; also proves the fake clients are wired |

## Frontend (slice 01), 26 cases

| File | Test | Cases | Class | Reason |
|---|---|---|---|---|
| `BoardWizard.test.tsx` | says the same thing about an empty list on a %s picker (Board, Team) | 2 | RED | `NO_BOARDS` still names Visual Task Boards |
| `BoardWizard.test.tsx` | shows what the tracker said about a board and cannot be confirmed | 1 | PASS-WHEN-ENABLED | the frontend half of F2 already works: `refusalOr` shows the text and null keeps Confirm disabled |
| `BoardWizard.test.tsx` | takes away the boards of the last answer when the list is refused | 1 | RED | the field stays enabled on the previous connection's boards |
| `BoardWizard.test.tsx` | keeps the newer board when the older one answers last | 1 | RED | no selection guard: the older answer overwrites the newer |
| `BoardWizard.test.tsx` | says nothing about the older board being refused after the newer one answered | 1 | RED | the older refusal is shown |
| `BoardWizard.test.tsx` | keeps waiting for the newer board when the older one arrives first / is refused first | 2 | RED | the older answer clears the newer spinner |
| `BoardWizard.test.tsx` | ignores an answer that arrives after the selection was cleared | 1 | RED | the late answer is shown for no selection |
| `GeneralSettingsComponent.test.tsx` | shows the picker disabled and says why (scoped token, OAuth; team, portfolio) | 4 | RED | the button is enabled |
| `GeneralSettingsComponent.test.tsx` | leaves the picker usable (`jira.cloud`, `jira.datacenter`) | 2 | PASS-WHEN-ENABLED | other sign-ins must be unaffected |
| `CreateWizard.boardPicker.test.tsx` | goes straight to Configure, with the picker disabled and the reason on it (Team, Portfolio; scoped token, OAuth) | 4 | RED | the flow stops on Load Data |
| `CreateWizard.boardPicker.test.tsx` | going back from Configure returns to choosing the connection (Team, Portfolio) | 2 | RED | the flow never reaches Configure |
| `CreateWizard.boardPicker.test.tsx` | still offers the picker on the Load Data step (Team, Portfolio; cloud, data center) | 4 | PASS-WHEN-ENABLED | other sign-ins must be unaffected |
| `WorkTrackingSystemConnection.authenticationMethodKeys.test.ts` | includes every Jira sign-in, so the picker rule can name them | 1 | RED | `JiraOAuth` is missing from the list |

The "going back" test first passed for the wrong reason: today it starts on Load Data, so Back reached the
connection list without ever being on Configure. It now waits for Configure first and is a genuine RED.

## Summary

92 cases: 77 RED, 15 PASS-WHEN-ENABLED, 0 BROKEN.
