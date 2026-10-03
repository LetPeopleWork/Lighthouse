# RED classification — Epic #6136 (E1), slices 01 and 02

**Wave**: DISTILL · **Date**: 2026-10-03 · **Scope**: slice 01 (US-01, ADO #6139) and slice 02 (US-02, ADO #6140).

Every scenario below was un-skipped once on `main` at `fd9df2bca` plus this commit's test files and
scaffolds, run, classified, and skipped again. **66 of 66 runnable cases fail, every one of them on
missing behaviour (`MISSING_FUNCTIONALITY`). None is `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE`
or `WRONG_ASSERTION`.** The E2E walking skeleton was type-checked and linted, not run live (see the end).

Two backend scenarios first came back **green** against today's code, because a missing route answers
404 and both asserted a 404. Each now first proves the tab opens for a caller who may read the Team (or
for an existing Team) before asserting the refusal, so a 404 can only mean what the scenario says. Both
were re-run and are RED.

## Backend — `Lighthouse.Backend.Tests` (NUnit, `WebApplicationFactory`, real EF)

Run: `dotnet test --filter "FullyQualifiedName~API.Integration.Refinement|FullyQualifiedName~TeamRefinementUsageEventsTests"`
with every `[Ignore(PendingSlice0n)]` removed → **Failed 34, Passed 0**.

| Scenario | Fails at | Why it fails today | Class |
|---|---|---|---|
| `Slice01RefinementStatesTest.A_Team_admin_names_the_refinement_states_and_the_Team_says_it_has_them` | Then: chosen states read back | The settings write ignores `refinement`; nothing is stored | MISSING_FUNCTIONALITY |
| `…A_Team_nobody_has_set_up_says_it_has_no_refinement_states` | Then: Team read | `TeamDto` carries no `refinementConfigured` | MISSING_FUNCTIONALITY |
| `…Every_reader_of_the_Team_learns_that_it_has_refinement_states` | Then: Team read | as above | MISSING_FUNCTIONALITY |
| `…A_state_the_Team_maps_under_a_name_of_its_own_can_be_chosen_by_that_name` | Then: chosen states | nothing stored | MISSING_FUNCTIONALITY |
| `…A_state_that_is_neither_To_Do_nor_Doing_is_refused_and_nothing_is_saved` (Done, Icebox) | Then: refusal | save answers 200 — no validator | MISSING_FUNCTIONALITY |
| `…Only_a_Team_admin_can_change_the_refinement_states` | Then: states unchanged | the admin's own first choice was never stored (the 403 itself already holds) | MISSING_FUNCTIONALITY |
| `…A_save_that_says_nothing_about_refinement_leaves_the_chosen_states_as_they_were` | Then: chosen states | nothing stored | MISSING_FUNCTIONALITY |
| `…Clearing_every_refinement_state_turns_the_Team_back_to_having_none` | Then: Team read | no `refinementConfigured` | MISSING_FUNCTIONALITY |
| `…A_chosen_state_that_stops_being_mapped_is_kept_and_flagged_never_dropped` | Then: chosen states | nothing stored, no `isMapped` flag | MISSING_FUNCTIONALITY |
| `…Saving_again_with_the_flagged_state_still_chosen_keeps_it_flagged_instead_of_refusing_the_save` | Then: flag | nothing stored | MISSING_FUNCTIONALITY |
| `…A_newly_chosen_state_that_is_no_longer_mapped_is_refused` | Then: refusal | save answers 200 | MISSING_FUNCTIONALITY |
| `…Choosing_refinement_states_keeps_every_Work_Item_the_Team_already_holds` | Then: chosen states (the Work Item count already holds) | nothing stored | MISSING_FUNCTIONALITY |
| `…Refinement_is_a_word_every_instance_can_rename` | Then: terminology | no `refinement` / `refinements` seeded | MISSING_FUNCTIONALITY |
| `Slice02RefinementListTest.*` (14 scenarios) | When: `GET /teams/{id}/refinement` | the endpoint does not exist (404); the Given — choosing states through the real settings write — completes | MISSING_FUNCTIONALITY |
| `TeamRefinementUsageEventsTests.A_browser_that_agreed_reports_refinement_being_set_up…` | Then: 204 | the server refuses an unknown event name (400) | MISSING_FUNCTIONALITY |
| `…A_refinement_set_up_event_carrying_anything_but_its_name_is_refused` (2 cases) | Then: plain event accepted | as above | MISSING_FUNCTIONALITY |
| `…Refinement_being_set_up_is_appended_to_the_list_of_names_never_inserted` | Then: name on the list | `UsageDataEventName` has no such member | MISSING_FUNCTIONALITY |
| `…A_browser_that_agreed_reports_opening_the_Refinement_tab…` | Then: 204 | route key `TeamDetail_Refinement` unknown (400) | MISSING_FUNCTIONALITY |
| `…The_usage_data_page_lists_the_Refinement_tab…` | Then: page text | `docs/settings/usagedata.md` does not list `/teams/:id/refinement` | MISSING_FUNCTIONALITY |

## Frontend — `Lighthouse.Frontend` (Vitest + RTL)

Run: `vitest run` over the four files with every `it.skip(` turned into `it(` → **32 failed, 0 passed**.

| File | Cases | Fails because | Class |
|---|---|---|---|
| `pages/Teams/Detail/TeamDetail.refinementTab.test.tsx` | 13 | no tab named "Refinement" (11); an address naming `refinement` opens Features, not Refinement or Forecasts (2) | MISSING_FUNCTIONALITY |
| `pages/Teams/Detail/Refinement/RefinementView.test.tsx` | 9 | the `RefinementView` scaffold throws `Not yet implemented -- RED scaffold` | MISSING_FUNCTIONALITY (scaffold) |
| `components/Common/Team/ModifyTeamSettings.refinement.test.tsx` | 8 | the settings form has no Refinement section (no heading, no checkboxes, no flag) | MISSING_FUNCTIONALITY |
| `services/UsageData/usageDataRouteKeys.refinement.test.ts` | 2 | `/teams/7/refinement` maps to no page opening | MISSING_FUNCTIONALITY |

## E2E — `Lighthouse.EndToEndTests/tests/specs/teams/Refinement.spec.ts`

One walking skeleton, `test.fixme`. **Not run live**: it cannot pass before DELIVER (no tab, no demo
refinement states), and the local run needs a backend with a premium licence on a spare port. It is
type-checked (`tsc --noEmit`, 0 errors) and Biome-clean. Expected RED when un-fixme'd today: the
`refinementTab` locator finds no tab. **DELIVER slice 02 must run it against a live instance before
committing it un-fixme'd** (CI learning *never commit a Playwright spec you have not run*).
