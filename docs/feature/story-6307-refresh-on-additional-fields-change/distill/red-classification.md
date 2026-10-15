# RED classification — story-6307-refresh-on-additional-fields-change

Run 2026-10-15 against unmodified production code, re-run the same day after the review-gate revision. There are no
DISTILL scaffolds: every backend scenario goes through HTTP and every frontend test through the rendered page or the
service, so nothing new had to exist for the tests to compile. Every new `[Ignore(Pending)]` and `it.skip` was
stripped, the suites were run, and the markers were put back from a backup. Build: `dotnet build Lighthouse.sln`,
0 errors; the only warning is NU1903 on `Microsoft.AspNetCore.SignalR.Common` 10.0.12, a package-audit advisory that
predates this story (no project file was touched).

**Every pending test fails on an assertion. None fails on an error.** Backend: 30 of 30 are NUnit Failures; 0 Errors.
Frontend: 22 of 22 fail on `AssertionError` or `TestingLibraryElementError` (the question dialog is not there); 0
`TypeError`, import or setup failures. All are MISSING_FUNCTIONALITY.

## The harness can tell a full download from a partial one

The fake tracker holds one item for each Team and Portfolio, stamped with when it last changed. An update with
nothing new to ask for therefore fetches only what changed, and "downloaded everything" in the refresh history means
the update had a reason to. An earlier version of the harness had the tracker answer with nothing, so every update
was a full download and the "downloads everything" assertions could never fail. A throwaway probe found this and was
deleted afterwards. With the probe, on today's code:

| Probe | Result | What it shows |
|-------|--------|---------------|
| A scheduled update of Atlas with no field change | `Atlas Delta finished` | "downloads everything" is not true by default |
| Name-only save, then every owner started by hand | 6 Task Manager rows, 6 history entries | `ThenNoTeamOrPortfolioIsUpdated` fails when anything runs |
| Field added, every owner started by hand, Teams first | passed the walking skeleton's Then steps, "downloads everything once" and Teams-first | the scenarios can go green |
| The same, Portfolios first | `lastTeam 15` not less than `firstPortfolio 10` | the Teams-first check fails on the wrong order |
| Atlas running and Cobalt waiting at the save, then every owner started by hand | `Atlas Delta, Cobalt Full, Borealis Full, Delta Ops Full, Fleet Platform Full, Customer Apps Full, Atlas Full, Cobalt Delta` | the queue behaves as DESIGN's UC-1 says: Atlas's running update predates the save and its follow-up downloads everything, Cobalt's waiting run downloads everything and its follow-up only fetches what changed |

The "same fields in another order" guard used to reverse a list of one field, which changes nothing, so it could
not fail. It now stores a second field first, and its When step asserts that the order it sends differs from the
one it read.

## Backend

Filter `FullyQualifiedName~ConnectionFieldChange|FullyQualifiedName~FetchFingerprintPinnedDigestTest|FullyQualifiedName~WorkTrackingSystemFieldsChangedUsageEventTests`
with the markers stripped and the run limited to the 30 pending tests (their `[Ignore]` replaced by a temporary `TestCategory=unskipped-for-red-run`, filtered on that category): 30 Failures, 0 Errors, 5 min 12 s. Many
of the failing cases wait out the 30-second refresh-history poll, so the RED run is slow and the green run will not
be.

| Test (cases) | Classification | Failure observed |
|--------------|----------------|------------------|
| `Slice01FieldChangeUpdatesItsOwnersTest.Adding_a_field_lists_an_update_for_every_Team_and_Portfolio_on_the_connection_in_the_Task_Manager` (walking skeleton) | MISSING_FUNCTIONALITY | `The Task Manager should list one update for each Team and Portfolio the save started, within 5 seconds of the save. It listed: (nothing)` |
| `…Every_Team_and_Portfolio_on_the_connection_downloads_everything_once_after_a_field_is_added` | MISSING_FUNCTIONALITY | `Exactly one update each … The refresh history after the save: (nothing)` |
| `…The_Teams_are_updated_before_any_Portfolio` | MISSING_FUNCTIONALITY | refresh history after the save `(nothing)`; `lastTeam` `int.MaxValue` not less than `firstPortfolio` `int.MinValue` |
| `…Any_other_field_edit_also_makes_every_Team_and_Portfolio_on_the_connection_download_everything` (3: rename, new reference, removal) | MISSING_FUNCTIONALITY | refresh history after the save `(nothing)` |
| `…A_Team_already_updating_or_waiting_is_not_downloaded_in_full_twice` | MISSING_FUNCTIONALITY | the Task Manager listed only `Atlas, Cobalt`, the two the scenario started itself |
| `…An_update_that_fails_leaves_the_field_saved_and_the_others_updating` | MISSING_FUNCTIONALITY | `One update of Borealis after the save: (nothing)` |
| `…The_Team_whose_update_failed_downloads_everything_at_its_next_update` | MISSING_FUNCTIONALITY | fails at its Given `GivenTheUpdateOfHasFailed(Borealis)`: `One update of Borealis after the save: (nothing)` |
| `…Adding_a_field_to_a_connection_only_Portfolios_read_from_lists_their_updates_in_the_Task_Manager` (new) | MISSING_FUNCTIONALITY | `It listed: (nothing)` |
| `…Adding_a_field_on_the_community_licence_updates_every_Team_and_Portfolio_on_the_connection` | MISSING_FUNCTIONALITY | refresh history after the save `(nothing)` |
| `…A_save_that_asks_to_update_now_updates_every_Team_and_Portfolio_on_the_connection` (`?startUpdates=true`) | MISSING_FUNCTIONALITY | the save is accepted; `Exactly one update each … (nothing)` |
| `…A_save_that_asks_for_neither_now_nor_later_is_refused_and_stores_nothing` (2: `maybe`, `later`) | MISSING_FUNCTIONALITY | `The save answered 200`: the unknown query value is ignored today |
| `Slice02ConnectionsSayWhoUsesThemTest.The_connection_list_says_how_many_Teams_and_Portfolios_use_each_connection` (4, the Portfolio-only connection new) | MISSING_FUNCTIONALITY | `How many Teams read through it`: the list carries no `usedBy` |
| `WorkTrackingSystemFieldsChangedUsageEventTests.A_change_to_a_connections_fields_travels_with_when_the_updates_run_and_nothing_else` (2: `Now`, `Later`) | MISSING_FUNCTIONALITY | 400, `One or more validation errors occurred`: the event name cannot be read |
| `…without_a_choice_from_the_list_or_carrying_anything_more_is_refused` (6) | MISSING_FUNCTIONALITY | fails at the precondition `the complete event is accepted`: `Expected: NoContent, But was: BadRequest`, because the event does not exist yet. The refusal itself is compared only once the complete event is accepted |
| `…Any_other_event_carrying_the_choice_is_refused` (2: `TeamCreated`, `WorkTrackingSystemConnected`) | MISSING_FUNCTIONALITY | `Expected: BadRequest, But was: NoContent`: the extra `updates` is dropped today rather than refused |
| `…The_event_is_named_and_every_event_named_before_it_keeps_its_number` (replaces the "straight after ReportDeleted" test) | MISSING_FUNCTIONALITY | `Expected: some item equal to "WorkTrackingSystemFieldsChanged"`. The golden-list half holds today: all 19 existing names keep their numbers |
| `…The_usage_data_page_has_a_row_for_a_change_to_a_connections_fields_naming_both_choices` | MISSING_FUNCTIONALITY | `the page has one row for the event`: `Expected: 1, But was: 0` |
| Guards (17): `Slice01SavesThatStartNothingTest`, 15 cases (five edits that change nothing fetched, the same fields in another order, another connection's Team, an unused connection, three refused saves, Save only, the next scheduled update after Save only, a lookup that registers the tracker's own field, and the save after it); `FetchFingerprintPinnedDigestTest` (2) | (green) | pass today and must pass after |

`Saving_only_stores_the_field_and_starts_no_update` and `After_saving_only_the_next_scheduled_update_downloads_everything`
pass today because nothing starts updates on save and `startUpdates` is ignored. They are the regression guard for
slice 01: once the save starts updates, `?startUpdates=false` is what keeps them green.

## Frontend

`pnpm exec cross-env TZ=Europe/Zurich vitest run src/pages/Connections/Edit/EditConnection.fieldChange.test.tsx src/services/Api/WorkTrackingSystemService.test.ts`
with every `it.skip` made `it`: 22 failed, 24 passed (8 page guards, 16 existing service tests).

| Test (cases) | Classification | Failure observed |
|--------------|----------------|------------------|
| WS `adding a field and pressing Save asks whether to update the 4 Teams and 2 Portfolios on the connection` | MISSING_FUNCTIONALITY | `Unable to find role="dialog" and name "Update 4 Teams and 2 Portfolios?"` |
| `Save and update saves the new field without holding the updates back …`, `the question is asked whenever Priya %s` (3: adds, renames, removes), `Cancel saves nothing …`, `when someone else saved the connection first …` | MISSING_FUNCTIONALITY | the same: no question dialog. The field edits themselves ran (each reached Save), so the editor steps are sound |
| `with %s the question reads as such` (5: one of each, Squads and Programs, one Squad and one Program, no Portfolio, no Team), `the explanation names the Teams and Portfolios in the words this instance uses` | MISSING_FUNCTIONALITY | no dialog named `Update 1 Team and 1 Portfolio?`, `Update 4 Squads and 2 Programs?`, `Update 1 Squad and 1 Program?`, `Update 3 Teams and 0 Portfolios?`, `Update 0 Teams and 2 Portfolios?` |
| `changing a field no longer promises it will load with the next refresh` | MISSING_FUNCTIONALITY | `expect(element).not.toBeInTheDocument()`: the alert is there |
| Slice 03: `the question offers to save only …`, `Save only saves the new field, holds the updates back …`, `answering %s tells usage data the updates run %s and nothing else` (2), `Cancel tells usage data nothing`, `a save refused because someone else saved first tells usage data nothing` | MISSING_FUNCTIONALITY | no question dialog |
| Service: `keeps how many Teams and Portfolios use each connection from the list of connections` | MISSING_FUNCTIONALITY | `expected undefined to deeply equal { teams: 4, portfolios: 2 }`: the deserializer drops `usedBy` |
| Service: `saving only asks the server to leave the updates to their next scheduled run` | MISSING_FUNCTIONALITY | the PUT carries no `startUpdates` |
| Guards (8): no question when setting up a new connection, for a connection nobody uses, for a new name, for a new API token, for a field added and cancelled, for a field added and removed, for a field change the tracker refuses; no usage data for a save without a question | (green) | pass today and must pass after |
