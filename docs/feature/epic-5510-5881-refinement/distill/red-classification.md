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


---

# RED classification — Epic #5510 (E3), slices 10–17b

**Wave**: DISTILL · **Date**: 2026-10-03 · **Scope**: slices 10 (US-10, #6148), 11 (US-11, #6149), 12 (US-12,
#6150), 13 (US-13, #6151), 14 (US-14, #6152), 15 (US-15, #6153), 16 (US-16, #6154), 17a (US-17a, #6155) and 17b
(US-17b, #6156) — the Lighthouse half of 17a/17b only.

Every case below was un-skipped once on `main` at `bc151d88d` plus this commit's test files and scaffolds, run,
classified, and skipped again. **196 of 196 runnable cases fail, every one of them on missing behaviour
(`MISSING_FUNCTIONALITY`). None is `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE` or `WRONG_ASSERTION`.** The
E2E walking skeleton was type-checked and linted, not run live.

Three cases came back **green** on the first run and were rewritten before this record, because a green case before
DELIVER proves nothing:

- *Changing readiness keeps every Work Item the Team holds* (backend, slice 13): today's settings write ignores a
  `readiness` member, so "the Work Items are kept" held trivially. It now also reads the readiness back, so it can
  only pass once readiness is stored.
- *Offers no way to change a name, because the account is the name* (frontend, slice 15): "no such button" held on
  a tab with no vote controls at all. It now first finds the row's Yes button.
- *Asks nothing once nothing is in refinement any more* (frontend, slice 10): "no question" held on a tab that asks
  none. It now first shows the question on a list, then the empty state without it.

Refusal scenarios whose status a missing route would also give (404) first prove the route answers a permitted
caller: the vote for a Team that does not exist, the log of a Work Item outside refinement, and every "somebody
without a role" case start from a vote, log read or take-back that succeeds.

## Backend — `Lighthouse.Backend.Tests` (NUnit, `WebApplicationFactory`, real EF)

Run: `dotnet test --filter "FullyQualifiedName~API.Integration.Refinement.Slice1|FullyQualifiedName~TeamSizingUsageEventsTests"`
with every `[Ignore(PendingSlice…)]` and `IgnoreReason = PendingSlice…` removed → **Failed 132, Passed 0**.

| Fixture (slice) | Cases | Fails at | Why it fails today | Class |
|---|---|---|---|---|
| `Slice10SleYardstickTest` (10) | 9 | Then: `yardstick` on the tab's read | The tab's answer carries no yardstick; the Givens (Team with or without an SLE, finished Work Items, the admin's settings save) all complete | MISSING_FUNCTIONALITY |
| `Slice11CastAVoteTest` (11) | 26 | When: `POST …/work-items/{ref}/votes`, or Given `HasVoted` | The vote route does not exist (404); refusals expect 400/409/429 and get 404; the tab's rows carry no `voteCount`/`myVote`; the tab carries no `voterIdentity` | MISSING_FUNCTIONALITY |
| `Slice12CommentsTest` (12) | 19 | Given `HasVoted` / `HasCommented`, or Then: refusal status | Vote and comment routes do not exist (404); refusals expect 400/409 and get 404 | MISSING_FUNCTIONALITY |
| `Slice13ReadinessTest` (13) | 22 | Then: `readiness` in the settings read, or Given `HasVoted`, or Then: row readiness | Settings carry no readiness; a save with readiness answers 200 and stores nothing; rows carry no `readiness`/`missingVotes`; the vote route is missing | MISSING_FUNCTIONALITY |
| ~~`Slice14HiddenSplitTest` + `Slice14HiddenFromTeamAdminsTest` (14)~~ | ~~10~~ | — | Deleted 2026-10-04: slice 14 dropped by the maintainer, see the DISTILL wave decisions' amendment | — |
| `Slice15VotesWithAnAccountTest` + `Slice15VotesWithoutRolesTest` (15) | 9 | Given `HasVoted`, When: vote, or Then: `voterIdentity` | The vote route does not exist; the tab says nothing about how voters are known. The sign-in-without-roles host itself works: its tab read answers 200 | MISSING_FUNCTIONALITY |
| `Slice16TakeBackTest` + `Slice16TakeBackWithAnAccountTest` (16) | 11 | Given `HasVoted` | No vote route, no take-back route | MISSING_FUNCTIONALITY |
| `Slice17ClientVotesTest` + `Slice17ClientVotesWithAnApiKeyTest` (17a, 17b) | 13 | Given `HasVoted`, or Then: 403 refusal | No vote, comment or take-back route; an unowned API key's write gets 404 instead of 403 `vote-needs-a-person`. The key itself authenticates: the same key's tab read answers 200 | MISSING_FUNCTIONALITY |
| `TeamSizingUsageEventsTests` (11, 13) | 13 | Then: 204, or the plain-event precondition | `TeamSizingVoteCast` / `TeamSizingReadinessReached` and `sizingMoment` are unknown, so the server refuses them (400); neither name is on `UsageDataEventName` | MISSING_FUNCTIONALITY |

## Frontend — `Lighthouse.Frontend` (Vitest + RTL)

Run: `vitest run` over the seven files with every `it.skip(` / `it.skip.each(` turned into `it(` / `it.each(` →
**64 failed, 0 passed** (after the two rewrites above).

| File | Cases | Fails because | Class |
|---|---|---|---|
| `pages/Teams/Detail/Refinement/RefinementView.yardstick.test.tsx` | 11 | no "Doable within …?" line, no info icon | MISSING_FUNCTIONALITY |
| `pages/Teams/Detail/Refinement/RefinementView.votes.test.tsx` | 18 | no Yes / Yes, but… / No buttons, no vote count, no Take back, no Change your name | MISSING_FUNCTIONALITY |
| `pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx` | 11 | no Yes, but… / Ask a question / Votes and comments buttons, no Open question marker | MISSING_FUNCTIONALITY |
| `pages/Teams/Detail/Refinement/RefinementView.readiness.test.tsx` | 8 | rows say nothing about readiness; the heading has no "ready by votes"; no vote buttons | MISSING_FUNCTIONALITY |
| `components/Common/Team/ModifyTeamSettings.readiness.test.tsx` | 7 | the Refinement section has no readiness fields | MISSING_FUNCTIONALITY |
| `services/Api/SizingLogService.test.ts` | 7 | the `SizingLogService` scaffold throws `Not yet implemented -- RED scaffold` | MISSING_FUNCTIONALITY (scaffold) |
| `services/Api/RefinementService.voterKey.test.ts` | 2 | the tab's read sends no voter key header | MISSING_FUNCTIONALITY |

## E2E — `Lighthouse.EndToEndTests/tests/specs/teams/Refinement.spec.ts`

Second walking skeleton, `testWithDemo.fixme`: *a voter gives a name, says Yes on a Work Item in refinement and the
votes make it Ready*. **Not run live**: it needs DELIVER's vote controls and demo votes (GR-059 with two Yes votes,
from Jonas Weber and Mo Okafor). Type-checked (`tsc --noEmit`, 0 errors) and Biome-clean. Expected RED when
un-fixme'd today: the row has no "1 more Yes needed" and no Yes button. **DELIVER slice 13 runs it against a live
instance before committing it un-fixme'd.**
