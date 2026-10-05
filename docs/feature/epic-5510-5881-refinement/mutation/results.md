# Mutation testing — 6136 (the Refinement tab)

Run 2026-10-03 against `main` @ `1e4cb29e3`, with the production code frozen at that commit; only tests
were added. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), the feature's code | **90.00 %** | 40 | 36 | 4 | 0 | 0 | 7 m 19 s |
| Backend, every mutated file whole | 70.76 % | 171 | 121 | 34 | 16 | 0 | (same run) |
| Frontend (StrykerJS 10.0.0) | **91.41 %** | 128 | 117 | 11 | 0 | 0 | 3 m 49 s |

Configs: `stryker.6136.backend.json`, `stryker.6136.frontend.json`, `vitest.stryker.6136.ts`.
Backend runs from `Lighthouse.Backend/Lighthouse.Backend.Tests/`; frontend from `Lighthouse.Frontend/`,
with `vitest.stryker.6136.ts` copied there first (Node resolves `vitest/config` from the config's own
directory, so the `docs/` copy cannot be pointed at directly).

First runs, before this pass added tests: backend 87.50 % on the feature's code (35 / 40), frontend
82.03 % (105 / 128, 20 survived, 3 no coverage).

**Why two backend rows.** Stryker.NET ignores line ranges, so `TeamExtensions.cs` (148 lines, of which
`SyncRefinement` and its call are ours) and `DemoDataFactory.cs` (219 lines, of which the demo
refinement helper is ours) were mutated whole and triaged by line. Their pre-existing code — cycle-time
definitions, ADO option lookups, business-day arithmetic — is what pulls the whole-file figure to 70.76 %;
none of its survivors sit on a line this epic wrote. The gate is judged on the first row: every mutant on
a line the epic added.

## Backend

Scope sanity check: `155 total mutants will be tested` (plus 16 with no covering test), consistent with
the nine mutated files. The `test-case-filter` selects 213 tests: every test whose name says
`Refinement` (the unit tests, the ArchUnit rules and the ~50 `API.Integration.Refinement` acceptance
scenarios on the real host), the four `TeamExtensions` sync suites, `DemoDataFactoryTest`,
`TeamControllerTest` and the `TeamsController` tests. The acceptance scenarios are where most of the
controller, DTO and query behaviour is pinned, and with them in the loop a run still takes seven
minutes, so they stayed.

| file | in score | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `API/Helpers/RefinementSettingsValidator.cs` | 8 | 8 | 0 | 0 | 100 % |
| `API/RefinementController.cs` | 2 | 2 | 0 | 0 | 100 % |
| `API/DTO/RefinementViewDto.cs` | 0 | — | — | — | no mutants (property initialisers only) |
| `API/DTO/RefinementSettingsDto.cs` | 3 | 2 | 1 | 0 | 66.67 % |
| `Services/Implementation/Refinement/RefinementList.cs` | 5 | 5 | 0 | 0 | 100 % |
| `Services/Implementation/Refinement/RefinementViewQuery.cs` | 2 | 2 | 0 | 0 | 100 % |
| `Models/Refinement/RefinementSettings.cs` | 1 | 0 | 1 | 0 | 0 % |
| `API/Helpers/TeamExtensions.cs` — our lines (64, 118–142) | 10 | 8 | 2 | 0 | 80 % |
| `Factories/DemoDataFactory.cs` — our lines (58, 66–84) | 9 | 9 | 0 | 0 | 100 % |
| **feature's code** | **40** | **36** | **4** | **0** | **90.00 %** |
| `API/Helpers/TeamExtensions.cs` — whole file | 48 | 30 | 9 | 9 | 62.50 % |
| `Factories/DemoDataFactory.cs` — whole file | 102 | 72 | 23 | 7 | 70.59 % |

Six mutants in `RefinementList.cs` (the `OrderBy` / `ThenBy` / `AsEnumerable` swaps on lines 23 and 27,
and the two on the `states.Count == 0` guard) are `CompileError`: Stryker.NET cannot place its mutant
switch inside the collection-expression spread. They are out of the score. Backlog order is pinned
directly by the seven `RefinementListTest.ListsWorkItemsInBacklogOrder` cases.

### Closed by this pass

- **`RefinementList.cs:17`** — removing the early `return []` for a Team without refinement states
  survived, because the filter that follows also lists nothing. What it lost is that every Team page
  without refinement set up would read every Work Item the Team holds. Pinned by
  `RefinementListTest.ATeamWithoutRefinementStatesListsNothingWithoutReadingItsWorkItems`.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `TeamExtensions.cs:131` | `group.First()` → `FirstOrDefault()` | equivalent: a `GroupBy` group always has at least one element |
| `TeamExtensions.cs:133` | `team.RefinementSettings ?? new RefinementSettings()` → always new | equivalent today: `States` is the settings' only member and the next line replaces it. It stops being equivalent the day a second member is added, and that change will need its own test |
| `RefinementSettingsDto.cs:32` | `State = string.Empty` → `"Stryker was here!"` | the default is only reached by a save whose state entry omits `state`; both values are refused by the validator, only the wording of the refusal differs |
| `RefinementSettings.cs:15` | `State = string.Empty` → `"Stryker was here!"` | the default is only reached by JSON this code never writes — every stored entry carries its state |

## Frontend

Scoped to the files the epic added, whole, plus the one line it added to `usageDataRouteKeys.ts`
(`:20-20`, the `refinement` tab key).

| file | in score | killed | survived | score |
| --- | --- | --- | --- | --- |
| `components/Common/Team/RefinementSettingsSection.tsx` | 51 | 45 | 6 | 88.24 % |
| `pages/Teams/Detail/Refinement/RefinementView.tsx` | 51 | 47 | 4 | 92.16 % |
| `hooks/useRefinementSetUpReporter.ts` | 21 | 20 | 1 | 95.24 % |
| `services/Api/RefinementService.ts` | 3 | 3 | 0 | 100 % |
| `services/UsageData/usageDataRouteKeys.ts:20` | 2 | 2 | 0 | 100 % |

Specs in the run (`vitest.stryker.6136.ts`, written from `ls`): `ModifyTeamSettings.refinement.test.tsx`,
`RefinementView.test.tsx`, `TeamDetail.refinementTab.test.tsx`, `EditTeam.test.tsx`,
`usageDataRouteKeys.refinement.test.ts`, `usageDataRouteKeys.test.ts`, `RefinementService.test.ts`,
`useRefinementSetUpReporter.test.ts` —
122 tests, proven live by a standalone `vitest run --config` before Stryker.

### Closed by this pass

- **`RefinementService.ts:12-15`** (3 no coverage) — nothing called the real service; every view spec
  stands it in. `RefinementService.test.ts` pins the address (`/teams/7/refinement`), that the answer is
  passed through, and that a failed read reaches the caller.
- **`RefinementSettingsSection.tsx:63`** (2) — the only removal case had a single chip, so a `remove`
  that cleared every state passed. `keeps the other chosen states when one chip is removed`.
- **`RefinementSettingsSection.tsx:76`** — `isLoading={true}` left a spinner in the state input forever.
  `shows no loading indicator in the state input`.
- **`RefinementView.tsx:67, 72, 77, 78, 80`** (5) — nothing moved the tab from one Team to another, so
  the effect's dependencies and its stale-answer guard were unpinned: under the mutants the tab either
  never asked for the new Team or let the previous Team's late answer, or late failure, overwrite it.
  `The Refinement tab follows the Team it is showing` — `shows the new Team's refinement, not the
  previous Team's answer that arrived late` and `says nothing about the previous Team's read failing
  once it shows another Team`.
- **`useRefinementSetUpReporter.ts:34`** — `settingsSaved`'s dependency on `reportUsage` emptied. The
  reporter is handed out anew once the browser's usage-data answer arrives, which can be after the
  settings page opened; with the dependency gone the save reports through the stale reporter, which
  still holds "no answer yet" and drops the event. `useRefinementSetUpReporter.test.ts` —
  `reports through the reporter the page holds when the save is answered, not the one it opened with`.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `RefinementSettingsSection.tsx:39` | `join("\n")` → `join("")` | the key only detects that the offered set changed; `""` collides only for state lists like `ab`+`c` vs `a`+`bc` changing into each other in one edit |
| `RefinementSettingsSection.tsx:41` (×2), `:47` | `anyNoLongerOffered` forced true / `<` → `<=` | equivalent through the form: the effect only runs when the admin changed the To Do or Doing states, which is itself an edit that saves; re-sending the unchanged refinement list in the same render adds nothing to that save |
| `RefinementSettingsSection.tsx:55` | `typed.trim()` → `typed` | equivalent through the public surface: `ItemListManager` trims (and matches the suggestion) before it calls `onAddItem` |
| `RefinementSettingsSection.tsx:69` | `size={{ xs: 12 }}` → `{}` | layout only |
| `useRefinementSetUpReporter.ts:23` | `settingsLoaded`'s empty dependency array gains a constant | equivalent: a constant dependency never changes, so the callback is the same one |
| `RefinementView.tsx:25` | `NO_ROWS` gains an element | only used while `refinement` is null, when the component renders nothing; at most the parent lookup is asked about a reference that is not there |
| `RefinementView.tsx:33` | parent column `field` → `""` | the cell renders from `row.parentReferenceId` directly; the field names the column, it does not feed it |
| `RefinementView.tsx:36` | parent column `sortable: false` → `true` | presentation: a sort arrow on the Parent header, which would order by parent id. Not asserted |
| `RefinementView.tsx:125` | `storageKey` → `""` | where the grid remembers column widths per Team; every spec clears `localStorage`, and nothing the coach reads depends on it |

## Not mutated

Backend:

- `Models/Team.cs` (`RefinementSettings`, `HasRefinementStates`), `API/DTO/TeamDto.cs`,
  `API/DTO/TeamSettingDto.cs`, `API/TeamController.cs`, `API/TeamsController.cs` — one to nine added
  lines each in large pre-existing files; mutated whole they would bury the change under unrelated
  code. Each added line is exercised end to end by the `API.Integration.Refinement` acceptance
  scenarios on the real host: the tab switching on (`RefinementConfigured`), the settings read
  round-tripping the chosen states, a stale chosen state not refusing the save (`TeamController`
  passes the stored states to the validator), and creation refusing a state that is neither To Do nor
  Doing (`TeamsController`).
- `Data/LighthouseAppContext.cs` (the JSON column) and the two `AddRefinementSettingsToTeams`
  migrations — mapping, pinned by the acceptance scenarios' save-and-read round trip on SQLite and by
  `HistoricalSchemaPatch`.
- `Services/Implementation/Seeding/TerminologySeeder.cs`, `Models/UsageData/*`, `Program.cs`,
  `Services/Interfaces/Refinement/IRefinementViewQuery.cs` — seed rows, enum members, DI registration
  and an interface; no behaviour of their own beyond what `TerminologySeederTests`,
  `UsageDataRoutePatternsTests` and the acceptance host already pin.

Frontend:

- `pages/Teams/Detail/TeamDetail.tsx` (the tab, its switched-off tooltip and the save-accepted switch-on),
  `pages/Teams/Edit/EditTeam.tsx`, `components/Common/Team/ModifyTeamSettings.tsx` — the epic's lines
  sit inside large pre-existing pages and were outside this pass's scope. They are covered by
  `TeamDetail.refinementTab.test.tsx` (15 cases) and the refinement block of `EditTeam.test.tsx`, which
  were in the run's spec list and kill the mutants in `useRefinementSetUpReporter.ts` that those pages
  drive.
- `components/Common/FeatureListDataGrid/columns.tsx`, `hooks/useParentWorkItems.ts` — the shared name column and parent lookup
  were widened to accept Work Item rows (generic types and renamed parameters); no runtime logic changed.
- `models/Refinement/Refinement.ts`, `models/Team/Team.ts`, `models/TerminologyKeys.ts`,
  `models/UsageData/UsageData.ts`, `services/Api/UsageDataService.ts`,
  `services/TerminologyContext.tsx` — types, enum members and terminology keys.

---

# Mutation testing — 6148 (the SLE yardstick, E3 slice 10)

Run 2026-10-03 against `main` @ `15dc4580a`, with the production code frozen; the only commit after the
first run added tests. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | timeout |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **100.00 %** | 20 | 20 | 0 | 0 | 0 |
| Frontend (StrykerJS 10.0.0) | **94.23 %** | 52 | 49 | 3 | 0 | 0 |

Configs: `stryker.6148.backend.json`, `stryker.6148.frontend.json`, `vitest.stryker.6148.ts`.

## Backend

| file | tested | killed |
| --- | --- | --- |
| `SleYardstickResolver.cs` | 15 | 15 |
| `RefinementViewQuery.cs` | 5 | 5 |

The filter keeps to the unit namespace `Tests.Services.Implementation.Refinement`; the acceptance suite
boots a host per test and would make the gate too slow to run per slice.

### Closed by this pass

The first run scored 85.00 % (17 of 20): `RefinementViewQuery` was only exercised through the acceptance
suite, so the unknown-Team null, and the configured flag being `false` / `true`, survived. Three unit
cases in `RefinementViewQueryTest` now pin them (commit `15dc4580a`).

### Not mutated

`RefinementViewDto.cs` (the `yardstick` mapping) is plain property copying covered only by the
acceptance scenarios that read `source`, `days` and `probability` off the wire; mutating it would
need the acceptance suite in the filter. `Program.cs` carries one registration line, proven by every
acceptance scenario that reads the tab.

## Frontend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `YardstickQuestion.tsx` | 49 | 46 | 3 |
| `RefinementView.tsx` (lines 121-123, the yardstick guard) | 3 | 3 | 0 |

### Closed by this pass

The first run scored 88.46 %: the `source is Sle or CycleTimeFallback` condition forced to `true`
survived, because no case sent an unknown source with a positive number of days. A row in the
"treats %s as having no number" table now does.

### Accepted survivors

- `YardstickQuestion.tsx:18` `yardstick.days !== null` → `true`: equivalent. The next condition is
  `yardstick.days > 0`, and `null > 0` is already `false`, so dropping the null check changes nothing.
- `YardstickQuestion.tsx:57` the `sx={{ alignItems: "center" }}` object and its string: vertical
  alignment of the question and the icon only, which jsdom does not lay out and no behaviour depends on.

---

# Mutation testing — 6149 (casting a sizing vote, E3 slice 11)

Run 2026-10-04 against `main` @ `7cadf3a62`, with the production code frozen at that commit; only tests
were added. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **97.73 %** | 132 | 129 | 1 | 2 | 0 | 4 m 20 s |
| Frontend (StrykerJS 10.0.0) | **94.59 %** | 314 | 297 | 17 | 0 | 0 | 12 m 18 s |

Configs: `stryker.6149.backend.json`, `stryker.6149.frontend.json`, `vitest.stryker.6149.ts`.
Backend runs from `Lighthouse.Backend/Lighthouse.Backend.Tests/`; frontend from `Lighthouse.Frontend/`,
with `vitest.stryker.6149.ts` copied there first.

First runs, before this pass added tests: backend **67.42 %** (89 / 132: 12 survived, 31 with no
covering test), frontend **79.30 %** (249 / 314: 52 survived, 13 with no covering test). Both were
below the gate.

## Backend

Scope sanity check: `130 total mutants will be tested` (plus 2 with no covering test) over the eleven
mutated files. The `test-case-filter` keeps to the unit tests that cover them — the
`Tests.Services.Implementation.Refinement` namespace and the classes named after the controller, the
repository, the vote's body, the event shapes and the guard attribute — and excludes the acceptance
suite (`API.Integration`, `Integration.Containers`), which boots a host per scenario.

| file | first run (killed / tested) | final (killed / tested) |
| --- | --- | --- |
| `API/RefinementVotesController.cs` | 0 / 24 (24 no coverage) | 22 / 24 |
| `API/DTO/SizingVoteDto.cs` | 6 / 9 | 8 / 9 |
| `Models/UsageData/UsageDataEventShapes.cs` | 5 / 10 | 10 / 10 |
| `Services/Implementation/Authorization/RbacGuardAttribute.cs` | 31 / 35 | 35 / 35 |
| `Services/Implementation/Refinement/RefinementResolution.cs` | 10 / 10 | 10 / 10 |
| `Services/Implementation/Refinement/RefinementViewQuery.cs` | 5 / 6 | 6 / 6 |
| `Services/Implementation/Refinement/SizingLogCommands.cs` | 4 / 5 | 5 / 5 |
| `Services/Implementation/Refinement/SizingRefusal.cs` | 1 / 1 | 1 / 1 |
| `Services/Implementation/Refinement/VoterIdentityResolver.cs` | 27 / 27 | 27 / 27 |
| `Services/Implementation/Repositories/SizingLogRepository.cs` | 0 / 5 (5 no coverage) | 5 / 5 |

`SizingLogEntry.cs` was in `mutate` and produced no testable mutant: its only mutant (the string
concatenation of the hashed voter key) does not compile. The hash itself is pinned by
`VoterIdentityResolverTest` and the new controller test, which compare against its output.
`RbacGuardAttribute.cs` was mutated whole (108 lines, two of them this slice's); its four survivors
were on pre-existing lines and are closed below rather than left to drag the file.

### Closed by this pass

- **`RefinementVotesController.cs`** (24, all no coverage) — the controller was only exercised by the
  acceptance scenarios. `RefinementVotesControllerTest` now pins every answer it gives: the 400 for a
  missing answer or channel; each voter refusal's status, title, whether it names its `code` (only the
  name and key refusals do), and the reason and level it logs (Warning only for a credential nobody
  stands behind); the row as it now stands after a recorded vote, including a reference whose slash
  arrived as `%2F` or `%2f` and one holding `%25` that must not be unescaped twice; 404 for an unknown
  Team and for a row gone by the time it is read; 409 with `work-item-not-in-refinement`.
- **`SizingLogRepository.cs`** (5, all no coverage) — `SizingLogRepositoryTest` appends and reads on a
  real database: an appended entry is stored at once, and a read returns only the asked Team's entries
  on the asked Work Items, oldest first.
- **`SizingVoteDto.cs:36`** (2) — the refusal's wording was unpinned. `SizingVoteDtoTest` —
  `A_refused_answer_names_the_answers_there_are`.
- **`UsageDataEventShapes.cs:31, 32, 64`** (5) — the page prefixes and three of the `&&` links between
  the parts were only checked from the controller's tests, outside this filter. `UsageDataEventShapesTests`
  — `An_event_missing_or_misplacing_one_of_its_other_parts_does_not_fit`: a Team tab naming a Portfolio
  page and the reverse, a Team tab naming none, a connection naming no system, a switch missing either
  half.
- **`RbacGuardAttribute.cs:52, 86, 89, 106`** (4) — a blank route key, a route value that is null or not
  a number (each must answer 500, not fall through to the check with no scope), and the request's
  cancellation reaching the check. Four cases in `RbacGuardAttributeTest`.
- **`RefinementViewQuery.cs:39`** — the votes-per-row lookup was never reached by a unit test, since no
  case had Work Items in refinement. `RefinementViewQueryTest` —
  `EachRowCarriesItsOwnVotesAndARowNobodyVotedOnCarriesNone`, which also proves the reader's own vote is
  recognised from the browser key.
- **`SizingLogCommands.cs:25`** `Any` → `All` — the fixture had only one Work Item in refinement, where
  the two agree. `SizingLogCommandsTest` — `AVoteOnAnyOfSeveralWorkItemsInRefinementIsRecordedAgainstThatWorkItem`.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `SizingVoteDto.cs:30` | `TokenType == String ? GetString() : null` → always `GetString()` | equivalent through the serializer: on a number or a boolean `GetString()` throws `InvalidOperationException`, which System.Text.Json rethrows as a `JsonException`, so the body is refused the same way either way |
| `RefinementVotesController.cs:54`, `:75` | the `UnreachableException` messages blanked | the discard arms of two total switches over closed enums; no value reaches them through the API |

## Frontend

`mutate` takes the slice's new files whole, and line ranges for the three it changed:
`RefinementView.tsx` 31-48, 62-95, 108-116, 134-136 and 156-178, `SizingLogService.ts` 10-18 and 57-71,
`RefinementService.ts` 15-26. The initial test run ran 119 tests from the 15 specs in
`vitest.stryker.6149.ts`, written from `ls`.

| file | first run (killed / tested) | final (killed / tested) |
| --- | --- | --- |
| `hooks/useVoterIdentity.ts` | 21 / 34 | 34 / 34 |
| `services/Refinement/voterStore.ts` | 32 / 46 | 39 / 46 |
| `Refinement/voteWording.ts` | 42 / 55 | 54 / 55 |
| `Refinement/useVoteCasting.ts` | 33 / 38 | 37 / 38 |
| `Refinement/RefinementView.tsx` (ranges above) | 43 / 50 | 46 / 50 |
| `Refinement/refinementColumns.tsx` | 20 / 25 | 25 / 25 |
| `Refinement/VoteControl.tsx` | 17 / 19 | 19 / 19 |
| `Refinement/VoterNamePrompt.tsx` | 17 / 17 | 17 / 17 |
| `Refinement/VotesAndCommentsDialog.tsx` | 8 / 13 | 10 / 13 |
| `services/Api/SizingLogService.ts` (ranges above) | 11 / 12 | 11 / 12 |
| `services/Api/RefinementService.ts` (range above) | 5 / 5 | 5 / 5 |

`VotesAndCommentsDialog.tsx` stays below 80 % on its own: its three survivors are one `sx` object and
its strings (see below).

### Closed by this pass

- **`voterStore.ts`** — nothing tested the store directly. `voterStore.test.ts` reads a stored voter
  back, counts ten unreadable shapes as no voter (nothing, not JSON, `null`, a number, a string, a name
  or key missing or not text, an empty key), and storage that refuses to be read; and keeps the stored
  key over the page's on a rename, keeps it when the page holds none, takes the page's when storage has
  none, and mints a fresh 64-hex key otherwise.
- **`useVoterIdentity.ts`** — `useVoterIdentity.test.ts`: a signed-in voter's ballot carries no name and
  no key, also once the tab learns the instance signs voters in after the first render (the
  `[isAccount]` dependency); a ballot with nobody declared carries neither; the name is trimmed; and a
  rename in a browser that refuses to store keeps the key the page holds (the `voter?.key ?? null`
  hand-over and the `[voter]` dependency).
- **`voteWording.ts`** — `voteWording.test.ts`, `why a vote was refused`: each refusal's words,
  including another server code and another field of a 400 (which must not be read as a name refusal),
  the name field on a non-400, a plain `Error`, and a thrown value that is not an error at all.
- **`useVoteCasting.ts`** — `useVoteCasting.test.tsx`: two votes on one row in the same tick send one;
  a vote after the Team changed goes to the new Team (the `castVote` dependency list); a name given with
  no vote waiting casts nothing.
- **`refinementColumns.tsx`** — `refinementColumns.test.tsx` pins which fields the grid can sort by:
  state and vote count, not parent or the reader's own vote.
- **`VoteControl.tsx`** — `VoteControl.test.tsx`: each label casts its answer (`Yes, if…` as `YesBut`),
  and clicking the answer already given casts nothing.
- **`VotesAndCommentsDialog.tsx:14, 34`** — `VotesAndCommentsDialog.test.tsx`: a row with no split reads
  as zero votes; a reader with no name is offered no name to change.
- **`RefinementView.tsx:86, 175`** — `RefinementView.votes.test.tsx`: `keeps the name as it was when
  changing it is cancelled`, and `words a refusal in the terms the instance uses by the time the vote is
  refused`. The file's terminology mock now hands out a lookup over the words as they stood at each
  render, as the real provider does; with the old live lookup a stale callback could never be told
  apart.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `voterStore.ts:13` (×5) | the not-an-object guard forced false, `&&`, or emptied | equivalent: the guard only runs inside `readStoredVoter`'s `try`. Destructuring `null` throws and is caught as no voter; destructuring any other primitive yields no name and no key, which the next line refuses |
| `voterStore.ts:28` (×2) | `stored === null` forced false or emptied | equivalent: `JSON.parse(null)` is `null`, which the shape check refuses |
| `voteWording.ts:22` | `problemCode !== undefined` → `true` | equivalent: `NAME_REFUSALS.has(undefined)` is already false |
| `SizingLogService.ts:14` | `voterKey === null` forced false | a null key only reaches `withVoterKey` from a signed-in voter, which is slice 15; its scaffold `sends no voter key header when the browser holds none` pins this and is skipped until then |
| `RefinementView.tsx:47` | `showFailure`'s dependency on `showError` emptied | equivalent: `showError` is a `useCallback` with no dependencies inside the snackbar provider, so it never changes |
| `RefinementView.tsx:73` | `current === null` → `false` | a row's answer only arrives for a vote cast from a rendered row, and rows render only once `refinement` is set; nothing sets it back to null |
| `RefinementView.tsx:82`, `useVoteCasting.ts:49` | empty dependency array gains a constant | equivalent: a constant dependency never changes |
| `RefinementView.tsx:171` | `changeableName !== null` → `true` | the name prompt only opens from "Change your name", which the dialog offers only when there is a name |
| `VotesAndCommentsDialog.tsx:35` (×3) | the `sx` object and its strings | layout of the "Voting as" line only |

## Not mutated

Backend:

- `API/RefinementController.cs` (the voter key header), `API/DTO/RefinementViewDto.cs` (the row's votes),
  `Data/LighthouseAppContext.cs` and the two `AddSizingLogEntries` migrations, `Program.cs`,
  `Configuration/RateLimitingConfiguration.cs`, `Services/Implementation/DemoDataService.cs` — wiring,
  mapping and seed data, pinned by the `Slice11CastAVote` acceptance scenarios, `S6_RateLimitingTests`
  and `DemoDataServiceTest`.
- `Services/Implementation/Authorization/RbacAdministrationService.cs` — two added lines in a 1400-line
  class (`TeamContribute` is satisfied by reading that Team); mutated whole it would bury them. No
  acceptance scenario signs in, so nothing exercised them: this pass added
  `CanSatisfyRequirementAsync_TeamContribute_FollowsReadingThatTeam` (the reader's own Team yes, another
  Team no) and `CanSatisfyRequirementAsync_TeamContribute_WithoutScopeId_ReturnsFalse` to
  `RbacAdministrationServiceTest`, and checked by hand that each fails when the scope is ignored or a
  missing scope is let through.
- `API/UsageDataController.cs`, `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs`,
  `Models/UsageData/*` (the new `SizingMoment` part) — one line each in large files, covered by
  `TeamSizingUsageEventsTests` and `UsageDataPublishedMessageTests`; the rule they rely on,
  `UsageDataEventShapes.cs`, is mutated above.
- `Models/Refinement/{SizingAnswer,SizingChannel,SizingEntryKind,YardstickSource}.cs`,
  `Services/Interfaces/**` — enums, records and interfaces.

Frontend:

- `models/UsageData/UsageData.ts`, `services/Api/UsageDataService.ts`,
  `services/UsageData/{usageDataBuffer,usageDataReporter}.ts` — an enum member and the new event part
  passed through; covered by `reports each vote the server took to usage data` in the votes spec.
- `setupTests.ts` — test setup.

# Mutation testing — 6151 (readiness by votes, E3 slice 13)

Run 2026-10-04 against `main` @ `62ff787b2`, with the production code frozen at that commit; only tests
were added. Gate is 80 % kill rate on both stacks, judged on mutants on the lines slice 13 added or
changed (`git diff eb6fb05dd^..62ff787b2`).

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), slice-13 lines | **100.00 %** | 80 | 80 | 0 | 0 | 0 | 3 m 13 s |
| Backend, every mutated file whole | 80.53 % | 190 | 153 | 18 | 19 | 0 | (same run) |
| Frontend (StrykerJS 10.0.0) | **92.75 %** | 207 | 192 | 15 | 0 | 0 | 13 m 45 s |

Configs: `stryker.6151.backend.json`, `stryker.6151.frontend.json`, `vitest.stryker.6151.ts`.
Backend runs from `Lighthouse.Backend/Lighthouse.Backend.Tests/`
(`dotnet stryker -f ../../docs/feature/epic-5510-5881-refinement/mutation/stryker.6151.backend.json`);
frontend from `Lighthouse.Frontend/`, with `vitest.stryker.6151.ts` copied there first
(`pnpm exec stryker run ../docs/feature/epic-5510-5881-refinement/mutation/stryker.6151.frontend.json`).

First runs, before this pass added tests: backend **76.25 %** on the slice-13 lines (61 / 80: 8
survived, 11 with no covering test; 68.42 % whole-file), frontend **76.33 %** (158 / 207: 47 survived,
2 with no covering test). Both were below the gate.

## Backend

**Why two backend rows.** Stryker.NET ignores line ranges, so every file is mutated whole and the
survivors are triaged by line. `TeamExtensions.cs` (159 lines, of which `SyncReadiness` and its call are
this slice's) carries most of the difference: its 32 non-killed mutants sit in `CreateTeamDto`, the
throughput-date conversions, the cycle-time definitions and `SyncRefinement`'s state handling, all
written before this slice. None of the whole-file survivors sits on a slice-13 line.

Scope sanity check: `171 total mutants will be tested` over the eleven mutated files, 249 tests in the
filter. The `test-case-filter` keeps to the unit tests: the `Tests.Services.Implementation.Refinement`
namespace and the classes named after the vote's controller, the settings validator, the settings JSON,
the settings DTO, the Team-settings sync tests and the event shapes. It excludes the acceptance suite
(`API.Integration`, `Integration.Containers`). The `Slice13Readiness` acceptance scenarios were **not**
needed: after this pass the unit tests alone kill every mutant on the slice's lines.

| file | first run, slice-13 lines (killed / tested) | final, slice-13 lines | final, whole file |
| --- | --- | --- | --- |
| `API/DTO/RefinementSettingsDto.cs` | 9 / 13 (2 no coverage) | 13 / 13 | 14 / 16 |
| `API/Helpers/RefinementSettingsValidator.cs` | 27 / 27 | 27 / 27 | 33 / 33 |
| `API/Helpers/TeamExtensions.cs` | 0 / 3 (3 no coverage) | 3 / 3 | 19 / 51 |
| `API/RefinementVotesController.cs` | 3 / 5 (1 no coverage) | 5 / 5 | 24 / 26 |
| `Services/Implementation/Refinement/RefinementResolution.cs` | 20 / 26 (4 no coverage) | 26 / 26 | 36 / 36 |
| `Services/Implementation/Refinement/RefinementViewQuery.cs` | 0 / 1 | 1 / 1 | 7 / 7 |
| `Services/Implementation/Refinement/SizingLogCommands.cs` | 2 / 4 | 4 / 4 | 9 / 9 |
| `Services/Interfaces/Refinement/IRefinementViewQuery.cs` | 0 / 1 (1 no coverage) | 1 / 1 | 1 / 1 |
| `Models/UsageData/UsageDataEventShapes.cs` | no mutant on the slice's line | — | 10 / 10 |
| `Models/Refinement/RefinementSettings.cs` | no mutant on the slice's lines | — | 0 / 1 |
| `API/DTO/RefinementViewDto.cs` | no mutant | — | — |

`RefinementViewDto.cs` is property initialisers only, so Stryker.NET makes nothing of it; what it adds
(`Readiness`, `MissingVotes`, `MadeReady`) is pinned by the controller test below. The slice's one line
in `UsageDataEventShapes.cs` adds `TeamSizingReadinessReached` to a collection expression, which
Stryker.NET does not mutate; `UsageDataEventShapesTests` checks that event carries its sizing moment
and nothing else. `IRefinementViewQuery.cs` was added to `mutate` although it is an interface file,
because `ReadyByVotesCount` is a rule, not a declaration.

### Closed by this pass

- **`RefinementResolution.cs:49, 50, 53`** (6: four no coverage, two survived) — `MadeReady` had no unit
  test; the readiness of the log before and after the entry was never compared. `RefinementResolutionTest`
  — `Only_the_entry_that_moves_a_Work_Item_to_Ready_made_it_Ready`: the second Yes of two made it Ready,
  the first did not, a Yes on an already-Ready Work Item did not, the entry that made it Ready still did
  after a later No took it away, and that No did not.
- **`SizingLogCommands.cs:54, 60`** (2) — no test had a vote make a Work Item Ready, and none had a Team
  with its own readiness. `SizingLogCommandsTest` —
  `AVoteSaysWhetherItMadeTheWorkItemReadyUnderTheTeamsOwnReadiness`: under one Yes from one voter, a Yes
  or a "Yes, if…" is `RecordedAndMadeReady` and a No is `Recorded`.
- **`RefinementVotesController.cs:51, 52`** (2) — whether the answered row says it was made Ready.
  `RefinementVotesControllerTest` — `ARecordedVoteSaysWhetherItIsTheVoteThatMadeItsRowReady`.
- **`RefinementViewQuery.cs:31`, `IRefinementViewQuery.cs:15`** (2) — the view was only ever read under
  the default readiness, and nothing read `ReadyByVotesCount`. `RefinementViewQueryTest` —
  `EachRowStandsUnderTheTeamsOwnReadinessAndOnlyReadyRowsAreCounted`: three rows under one Yes from one
  voter, one of them Ready, counted as one.
- **`RefinementSettingsDto.cs:46, 90, 119, 120`** (4) — the readiness read out to the form, and a
  discussion rule left out of a save. New `RefinementSettingsDtoTest`: the readiness a Team has is read
  out whole; rules read out and sent back replace the stored ones, a rule that is off included; a save
  that sends only the No rule keeps the stored "Yes, if…" rule, and the reverse.
- **`TeamExtensions.cs:140, 147, 149`** (3, all no coverage) — `SyncReadiness` was reached only by the
  acceptance scenarios. New `RefinementReadinessSyncTest`: a saved readiness is stored with what it
  leaves out kept, and a save that says nothing about readiness leaves it as it was.

### Accepted survivors

None on a slice-13 line. The whole-file survivors are on code earlier slices wrote and gated:
`TeamExtensions.cs` (above), `RefinementSettingsDto.cs:32, 36` and `RefinementSettings.cs:17` (the
refinement-state setting's constructor and `State` defaults, slice 01), and the two
`UnreachableException` messages in `RefinementVotesController.cs:55, 76`, accepted in 6149.

## Frontend

`mutate` takes `ReadinessCell.tsx` whole and line ranges for every changed file:
`RefinementSettingsSection.tsx` 30-160, 212-246 and 260-300, `ModifyTeamSettings.tsx` 87-90 and
270-284, `RefinementView.tsx` 31-58, 102 and 161-169, `refinementColumns.tsx` 71-81, `useVoteCasting.ts`
70-75 and `voteWording.ts` 52-65. The ranges in `RefinementView.tsx` and `useVoteCasting.ts` leave out
the slice-11 lines gated in 6149. The initial test run ran 158 tests from the 9 specs in
`vitest.stryker.6151.ts`, written from `ls`.

| file | first run (killed / tested) | final (killed / tested) |
| --- | --- | --- |
| `Common/Team/RefinementSettingsSection.tsx` (ranges above) | 87 / 127 | 114 / 127 |
| `Common/Team/ModifyTeamSettings.tsx` (ranges above) | 13 / 17 | 17 / 17 |
| `Refinement/RefinementView.tsx` (ranges above) | 29 / 34 | 32 / 34 |
| `Refinement/ReadinessCell.tsx` | 5 / 5 | 5 / 5 |
| `Refinement/refinementColumns.tsx` (range above) | 4 / 4 | 4 / 4 |
| `Refinement/useVoteCasting.ts` (range above) | 4 / 4 | 4 / 4 |
| `Refinement/voteWording.ts` (range above) | 16 / 16 | 16 / 16 |

### Closed by this pass

All in `ModifyTeamSettings.readiness.test.tsx` unless named otherwise.

- **`RefinementSettingsSection.tsx:37`, `ModifyTeamSettings.tsx:281`** (4) — no spec opened a Team that had
  never chosen refinement, and none checked the states a readiness save carries. `starts a Team that has
  never chosen refinement at the default readiness, and saves it with no states`; `keeps the chosen
  refinement states when readiness changes`.
- **`ModifyTeamSettings.tsx:89`** — the blocker's wording. `names readiness as what keeps the settings
  from being saved`, against the literal.
- **`RefinementSettingsSection.tsx:140, 146, 270, 282`** (12) — the fields were checked for their helper
  text, never for being marked. `marks $refused as refused and leaves $accepted unmarked` (Yes votes and
  voters) and `marks $refused as refused, describes it by why, and leaves $accepted unmarked` (both
  discussion thresholds: `aria-invalid`, the accessible description, and no `aria-describedby` on the
  field that is fine).
- **`RefinementSettingsSection.tsx:272, 284`** (4) — `does not let any of its numbers step below one
  vote`: `min` and `step` of all four number fields.
- **`RefinementSettingsSection.tsx:230, 242`** (10) — remembering a threshold when a rule is switched
  off. The existing case switched back on at the stored threshold, which the first render already
  remembers. `switches a rule back on at the threshold it was changed to before it was switched off`;
  `switches a rule that was refused when switched off back on at its last allowed threshold` (which
  first switches the rule off and on once, so that switching on must not overwrite what is remembered);
  `switches on a No rule stored as off at its default threshold`. The last mutant of the ten, the
  `!on && current !== null` pair forced `true`, survived the second run and was killed by the off-and-on
  step added to the refused case, checked by hand before the final run.
- **`RefinementView.tsx:41, 45`** (3) — `RefinementView.readiness.test.tsx`: `leaves the next Team's count
  alone when a vote cast before switching Teams is answered after` (the answered row is not in the list
  any more, so the count must not move), and `names no ready count, also after a vote, when the server
  sends none`.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `RefinementSettingsSection.tsx:242` | `current !== null` forced `true` | equivalent: the branch only runs on switching a rule off, and a rule can only be switched off from a ticked box, which means its threshold is not null |
| `RefinementSettingsSection.tsx:242` | `!on && current !== null` → `!on \|\| current !== null` | equivalent: switching on, `current` is null and both read false; switching off, both read true |
| `RefinementSettingsSection.tsx:69` | the empty string shown for a cleared field → `"Stryker was here!"` | equivalent: a number input sanitises any value that is not a number to empty, in the browser and in jsdom alike |
| `RefinementSettingsSection.tsx:152` | `error !== null` forced `true` | renders an empty helper paragraph under a threshold that is fine; nothing points at it (`aria-describedby` is unset without an error), so it is a few pixels of layout |
| `RefinementSettingsSection.tsx:122, 123` (×3), `134`, `260`, `263`, `275`, `287` | Grid `size`, `Box` and `TextField` `sx` objects and their strings | layout only |
| `RefinementView.tsx:32` | `row?.readiness` → `row.readiness` | equivalent: both callers pass a defined row, since the shown row is checked for `undefined` first |
| `RefinementView.tsx:102` | `current === null` → `false` | a row's answer only arrives for a vote cast from a rendered row, and rows render only once `refinement` is set; nothing sets it back to null (accepted in 6149 for the same line) |

## Not mutated

Backend:

- `API/TeamController.cs`, `API/TeamsController.cs` — the slice changed one line in each, handing the
  validator the stored `RefinementSettings` (`storedTeam?.RefinementSettings`) or `null` for a new Team.
  Stryker.NET has no mutator for a `?.` member access or a `null` argument, so mutating the two
  controllers whole would only score their older code. The stored side is pinned through the update
  endpoint by the `Slice13Readiness` scenarios that refuse a partial save judged against it; the `null`
  a new Team passes is pinned at the validator by
  `A_readiness_save_for_a_Team_without_refinement_is_judged_with_the_default_side`.
- `Models/Refinement/RowReadiness.cs`, `Models/UsageData/UsageDataEventName.cs`,
  `Services/Interfaces/Refinement/ISizingLogCommands.cs` — enums and an enum member.

Frontend:

- `services/Api/SizingLogService.ts` — the change is the return type (`IVotedRow`), which has no runtime
  form.
- `models/Refinement/Refinement.ts` — types only.
- `services/Api/UsageDataService.ts` — an enum member, pinned against its literal by `reports the vote
  the server says made a Work Item Ready to usage data`.

# Mutation testing — 6141 + 6142 (stage rules and the Refinement cadence, E2 slices 03 and 04)

Run 2026-10-05 against `main` @ `99114ac7f`, one run per stack for both slices. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **88.05 %** | 307 | 280 | 17 | 10 | 75 m 33 s |
| Frontend (StrykerJS) | **83.60 %** | 634 | 530 | 94 | 10 | 66 m 27 s |

After this pass's tests, checked by applying each closed mutant by hand: backend 285 / 307 (**92.8 %**), frontend
560 / 634 (**88.3 %**).

Configs: `stryker.6141-6142.backend.json`, `stryker.6141-6142.frontend.json`, `vitest.stryker.6141-6142.ts`.

## Backend

| file | killed | survived / no coverage |
| --- | --- | --- |
| `API/DTO/RefinementSettingsDto.cs` | 29 | 2 |
| `API/DTO/RefinementViewDto.cs` | 1 | 0 |
| `API/DTO/SaveField.cs` | 4 | 0 |
| `API/Helpers/RefinementSettingsValidator.cs` | 54 | 0 |
| `API/Helpers/RuleSetValidation.cs` | 13 | 8 |
| `API/Helpers/TeamExtensions.cs` | 40 | 17 |
| `Models/Refinement/RefinementSettings.cs` | 16 | 1 |
| `Services/Implementation/RecurringBlackoutRuleExtensions.cs` | 14 | 4 |
| `Services/Implementation/Refinement/RefinementCadenceCalendar.cs` | 18 | 5 |
| `Services/Implementation/Refinement/RefinementCalendar.cs` | 3 | 1 |
| `RefinementResolution.cs`, `RefinementViewQuery.cs`, `StageRuleHealing.cs`, `StageRuleMatcher.cs`, `WeeklyRecurrence.cs`, `IRefinementViewQuery.cs` | 88 | 0 |

### Closed by this pass

- **`RuleSetValidation.cs:51`** (Ready and Being-refined swapped) — `RuleSetValidationTest`: `When_both_stage_rules_are_invalid_the_Ready_rule_is_named`, `An_invalid_Being_refined_rule_is_named_when_the_Ready_rule_is_fine`.
- **`RefinementCadenceCalendar.cs:51`** (`<` → `<=`, `- 1` → `+ 1`) — `A_search_that_ends_exactly_on_the_calendars_last_day_still_looks_a_whole_year`.
- **`RefinementCadenceCalendar.cs:46, 60`** (no-cadence guard, `IntervalWeeks > 0` → `>= 0`) — `A_cadence_with_no_weekday_or_no_interval_searches_no_days`.
- **`RecurringBlackoutRuleExtensions.cs:12`** (`>` → `>=`) — `ExpandToBlackoutDays_OneDayWindowOnAMatchingWeekday_MatchesThatDay`. The line predates the slice (the weekly rule was extracted from around it), but the gap was real: a one-day window lost its day.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `TeamExtensions.cs:15, 41, 42, 62, 63, 81, 105–131` (17) | various | code outside this change; Stryker.NET mutates the whole file. Every mutant in the lines the slices added (141–142, 157–176) is killed |
| `RuleSetValidation.cs:27, 35, 57, 58, 64` (7) | various | the blocked and forecast-filter validation the slices did not touch, outside the test filter |
| `RecurringBlackoutRuleExtensions.cs:9, 10` (3) | `>` → `>=`, forced `true` | equivalent: on the boundary both sides of the choice are the same day |
| `RefinementCadenceCalendar.cs:66` | `>` → `>=` | equivalent: on equality both sides are the same day |
| `RefinementCalendar.cs:13` | null-cadence guard removed | equivalent: a null cadence then searches no days and is no cadence day, which is the same `None` |
| `RefinementSettingsDto.cs:135` | `Version ?? SchemaVersion` → `SchemaVersion` | equivalent while one rule-set schema version exists |
| `RefinementSettingsDto.cs:153`, `RefinementSettings.cs:83` | `string.Empty` default → `"Stryker was here!"` | the default is always overwritten by binding or by the constructor |

### Not mutated

- `API/TeamController.cs`, `API/TeamsController.cs` — a handful of lines each (stage-rule healing on read, stage-rule validation on create and update) in large controllers. They are pinned end to end by the `Slice03StageRules` scenarios and `TeamControllerTest`; mutating the controllers whole would mostly score their older code.
- `Factories/DemoDataFactory.cs` (one line: demo Team Gravity refines on Thursdays, pinned by `DemoDataFactoryTest`), `Program.cs` (DI registration), `Models/UsageData/UsageDataSizingMoment.cs` and `Services/Interfaces/Refinement/IRefinementCalendar.cs` (an enum and an interface).

## Frontend

Per file (killed / survived + no coverage) before this pass: `ModifyTeamSettings` 13/4, `ReadinessSettings` 100/12,
`RefinementCadenceSettings` 51/12, `RefinementSettingsSection` 78/8, `StageRulesSettings` 51/25, `WeeklyRecurrenceFields`
10/4, `BlackoutSettings` 5/0, `InfoTooltip` 1/1, `NextRefinement` 8/5, `nextRefinementWording` 27/1,
`refinementColumns` 8/0, `RefinementGrid` 8/2, `RefinementView` 57/15, `stageBreakdown` 14/1, `StageCell` 14/7,
`stageWording` 4/0, `useRefinement` 48/3, `useVoteCasting` 15/1, `YardstickQuestion` 3/2, `numberField` 15/1.

### Closed by this pass (30 mutants, each checked by hand)

- `RefinementSettingsSection.tsx:35, 39` — the two blocker reasons, `names an unfinished stage rule and a cadence with a mistake, in the Team's word`.
- `RefinementSettingsSection.tsx:79, 85` — `leaves the chosen states alone while every one of them is still offered`.
- `RefinementCadenceSettings.tsx:15` — `is shown with no weekday, every week and no starting week`; `:83` `keeps the starting week when a weekday is ticked`; `:89` `says Refinements are at least one week apart for fewer than one week`; `:93` `drops a ticked weekday when it is ticked again and keeps the others`; `:126` `asks for a day in the week of a Refinement when every second week has no starting week`.
- `StageRulesSettings.tsx:60, 81` — `asks for no fields and offers no stages for a Team that is not saved yet`, `stops offering stages once the form is for a Team that is not saved yet`; `:67, 71–79` `offers no stages when the fields of the Team cannot be fetched`, `ignores the fields of a Team the form has already moved away from`; `:99` `saves the first condition of an empty stage as matching all conditions`; `:107` `saves a stage rule switched to match any condition`; `:46, 50, 136, 146` `says what each empty stage would do, in the Team's word for work items`.
- `WeeklyRecurrenceFields.tsx:59` — `does not mark a valid number of weeks as invalid`.
- `InfoTooltip.tsx:6` — `keeps a click on its info icon from reaching the header around it`.
- `RefinementGrid.tsx:45` — `links the parents of the new Team's Work Items once it shows another Team`.

### Accepted survivors

| line | mutant | why it survives |
| --- | --- | --- |
| `RefinementView.tsx:22–33` (14) | the tab's grid layout object and its strings | layout only; jsdom does no grid layout. The heading-row placement is pinned by the Refinement E2E (`pin the next Refinement to the heading's row`) |
| `ReadinessSettings.tsx`, `RefinementCadenceSettings.tsx:99, 129`, `RefinementSettingsSection.tsx:107`, `StageRulesSettings.tsx:102, 139`, `WeeklyRecurrenceFields.tsx:62–63`, `NextRefinement.tsx:43–44`, `StageCell.tsx:28, 37`, `YardstickQuestion.tsx:68`, `RefinementGrid.tsx:79` | `sx`, Grid `size`, `slotProps`, storage key | layout or column-width persistence only |
| `ReadinessSettings.tsx:131, 180` (3) | as accepted for 6151 | the same code, moved out of `RefinementSettingsSection` unchanged |
| `NextRefinement.tsx:34, 44` | the slot marker `true` → `false` | equivalent: the layout selector matches the attribute's presence, and `data-next-refinement="false"` is still present |
| `RefinementSettingsSection.tsx:93` | `typed.trim()` → `typed` | equivalent through the UI: `ItemListManager` only ever hands over a trimmed value or a suggestion's exact text |
| `RefinementCadenceSettings.tsx:115` | `?? ""` → `&& ""` / `"Stryker was here!"` | equivalent: a date input sanitises a value that is not a date to empty |
| `nextRefinementWording.ts:24` | `== null ? null :` forced `false` | equivalent: parsing a null or undefined date already yields null |
| `ModifyTeamSettings.tsx:176, 177` | `?.` → `.` | equivalent: the section renders only once the settings are loaded |
| `ModifyTeamSettings.tsx:277, 278` (no coverage) | `\|\| []` → `["Stryker was here"]` | unreachable: the form already reads both lists unguarded before rendering the section |
| `StageCell.tsx:21` | `!stage` guard forced `false` | renders an empty cell instead of none; nothing visible changes |
| `stageBreakdown.ts:22` | `if (row.stage)` forced `true` | equivalent: a row without a stage counts under a key nothing reads |
| `useRefinement.ts:13, 73, 75`, `useVoteCasting.ts:39` | `?.` → `.`, `current === null` → `false`, callback deps | as accepted for 6149 and 6151 (code moved into the hook), and `facts` is already known to be set on line 39 |
| `numberField.ts:3` | `""` → `"Stryker was here!"` | equivalent: a number input shows any non-number as empty |

### Not mutated

- `models/Refinement/Refinement.ts`, `models/UsageData/UsageData.ts` — types and an enum member.

---

# Mutation testing — 6143 (need number and verdict, E2 slice 05)

Run 2026-10-05 against `main` @ `f01e7ff80` (backend) and `73b2611a2` (frontend, after the kill tests). Production
code was frozen between the runs. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **92.73 %** | 55 | 51 | 4 | 0 | 4 m 30 s |
| Frontend (StrykerJS), first run | 86.62 % | 299 | 259 | 39 (+1 no coverage) | 0 | 13 m 8 s |
| Frontend (StrykerJS), after kill tests | **95.32 %** | 299 | 285 | 14 | 0 | 11 m 17 s |

Configs: `stryker.6143.backend.json`, `stryker.6143.frontend.json`, `vitest.stryker.6143.ts`.

## Backend

| file | mutants | killed | survived | score |
| --- | --- | --- | --- | --- |
| `Services/Implementation/Refinement/NeedBand.cs` | 9 | 9 | 0 | 100 % |
| `Services/Implementation/Refinement/RefinementNeedCalculator.cs` | 8 | 8 | 0 | 100 % |
| `Services/Implementation/Refinement/RefinementViewQuery.cs` | 8 | 8 | 0 | 100 % |
| `Services/Interfaces/Refinement/IRefinementViewQuery.cs` | 1 | 1 | 0 | 100 % |
| `API/DTO/RefinementViewDto.cs` | 1 | 1 | 0 | 100 % |
| `Models/UsageData/UsageDataEventShapes.cs` | 11 | 11 | 0 | 100 % |
| `Models/Refinement/RefinementSettings.cs` | 17 | 13 | 4 | 76.47 % |

### Accepted survivors

All four are in `RefinementSettings.cs` and outside this slice's change (which added only `Band` and its defaults):

| mutant | reason |
| --- | --- |
| `:62` `OrderBy` → `OrderByDescending` (cadence weekdays) | slice-04 code; its tests (`Slice04RefinementCadenceTest`, `WeeklyRecurrence…`) are outside this run's filter and the 6141 + 6142 run covered the file |
| `:64` anchor-week conditional → `false` | as above |
| `:64` `-DaysFromMonday` → `+DaysFromMonday` | as above |
| `:101` `State = string.Empty` → `"Stryker was here!"` | the same equivalent default the 6136 run accepted: only JSON this code never writes reaches it |

### Not mutated

- `API/UsageDataController.cs` (11 of 218 lines changed) and `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs`
  (18 of 298) — whole-file mutation would bury the change under unrelated code. The verdict part is pinned end to end by
  `TeamRefinementNeedUsageEventsTests` (accepted with each of the four values, refused without one, with an unknown one,
  with anything beside it, and on any other event) and by `UsageDataEventShapesTests` over every event name.
- Enums (`RefinementVerdict`, `NeedUnavailableReason`, `UsageDataRefinementVerdict`, `UsageDataEventName`), the
  `UsageDataEventReported` / batch DTO records and `Program.cs` (one registration) — no behaviour to mutate.

## Frontend

| file | mutants | killed | survived | score |
| --- | --- | --- | --- | --- |
| `needWording.ts` | 46 | 46 | 0 | 100 % |
| `RefinementView.tsx` (changed lines) | 3 | 3 | 0 | 100 % |
| `usageDataReporter.ts:63-91` | 15 | 15 | 0 | 100 % |
| `useVerdictShownReporter.ts` | 40 | 39 | 1 | 97.50 % |
| `useRefinement.ts` | 102 | 97 | 5 | 95.10 % |
| `NeedVerdict.tsx` | 80 | 76 | 4 | 95.00 % |
| `NextRefinement.tsx` | 13 | 9 | 4 | 69.23 % |

### Closed by this pass (26 mutants, each checked by hand, commit `73b2611a2`)

- The guard that decides a need is complete enough to judge: one case per missing fact (verdict, low, high, either
  percentile, horizon), a need the server did not send, a missing ready count, and a judged need with no next
  Refinement — each shows no verdict and keeps the next Refinement on the heading's row.
- Too little history names the next Refinement once, as the message's title.
- A vote answered for a Work Item the Refinement no longer lists leaves the Refinement as read; a read failing after
  the tab closed says nothing.
- Team moves: a vote that moved the last Team's message, answered after the move, does not bring that Team back; a
  vote on a Work Item still on screen from the last Team does not count as the next Team's verdict report.
- The verdict is reported once per Team the tab is opened on, however often that Team's Refinement is read again, and
  not at all on a Refinement day with no Work Items in Refinement.
- The next Refinement sits on the heading's row, beside the count, when there is no message to title.

### Accepted survivors

| mutant | reason |
| --- | --- |
| `useRefinement.ts:50` `shown?.need` → `shown.need` | only differs with nothing shown, and no row can be voted on before a Refinement is shown |
| `useRefinement.ts:58` condition → `true` | unreachable: the vote's callback is captured with the view that holds the voted row |
| `useRefinement.ts:92` read counter `+= 1` → `-= 1` | equivalent: a counter counting down is as unique and monotonic |
| `useRefinement.ts:137` `current?.teamId` → `current.teamId` | only differs with nothing shown, when nothing can be voted on |
| `useRefinement.ts:142` condition → `true` | equivalent: the read counter already drops a re-read once the Team moves |
| `NeedVerdict.tsx:47` date guard → `false` | equivalent: `parseLocalDate` returns null for a missing date anyway |
| `NeedVerdict.tsx:56` condition → `true` | with no cadence the heading shows the no-cadence hint whatever this says; without refinement states the empty message shows first |
| `NeedVerdict.tsx:61` condition → `true` | equivalent: no next Refinement means no title text, so the hint shows either way |
| `NeedVerdict.tsx:77` condition → `true` | unreachable: too little history without a cadence cannot happen — the calculator answers no cadence first |
| `NextRefinement.tsx:30`, `:40` `true` → `false` | equivalent: React writes `data-next-refinement="false"` and the layout selector only checks the attribute is present |
| `NextRefinement.tsx:39` `{ alignItems }` → `{}` / `""` | pure styling |
| `useVerdictShownReporter.ts:35` initial opening `{…}` → `{}` | equivalent: the first run resets an opening without a Team |

# Mutation testing — 6144 (the enough-for line, E2 slice 06)

Run 2026-10-05 against `main` @ `e2d4a3e73`, production code frozen between the runs. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend | N/A | — | — | — | — | — |
| Frontend (StrykerJS), first run | 87.10 % | 217 | 189 | 28 | 0 | 15 m 7 s |
| Frontend (StrykerJS), after kill tests, ranges narrowed | **92.93 %** | 198 | 184 | 14 | 0 | 11 m 45 s |

Backend: N/A, the slice changed no backend file. Configs: `stryker.6144.frontend.json`, `vitest.stryker.6144.ts`.

The first run's `DataGridBase.tsx:237-259` and `RefinementGrid.tsx:12-97` ranges also covered lines this slice did not
touch (the grid's `initialState`, `pageSizeOptions`, `getRowHeight`, the dialog's `setColumnOrder`, the grid's
`storageKey`); seven of its survivors sat there. The second run mutates only the changed lines.

## Frontend

| file | mutants | killed | survived | score |
| --- | --- | --- | --- | --- |
| `enoughForPlacement.ts` | 58 | 58 | 0 | 100 % |
| `NeedVerdict.tsx` (changed lines) | 24 | 24 | 0 | 100 % |
| `refinementColumns.tsx` (changed lines) | 13 | 13 | 0 | 100 % |
| `RefinementGrid.tsx` (changed lines) | 3 | 3 | 0 | 100 % |
| `RefinementView.tsx` (changed lines) | 6 | 6 | 0 | 100 % |
| `DataGridBase.tsx` (changed lines) | 24 | 23 | 1 | 95.83 % |
| `EnoughForLine.tsx` | 70 | 57 | 13 | 81.43 % |

### Closed by this pass (each checked by hand against the mutant)

- Two columns added since a column order was saved keep their declared order, not just the first of them.
- A Team with a cadence but no Refinement states names the next Refinement on the heading's row; there is no message
  for it to title.
- The # column is headed `#` and offers no sorting, filtering or column menu: it numbers the rows as shown, so there
  is nothing to sort or filter it by.
- The space the grid keeps for the line moves with the number needed when the Refinement is read again.

### Accepted survivors

| mutant | reason |
| --- | --- |
| `DataGridBase.tsx:204` `useMemo` deps → `[]` | the dialog's order is read when it opens; columns change only with the page, which remounts the grid |
| `EnoughForLine.tsx:42` default shown index `-1` → `+1` | equivalent: the number is only ever rendered inside a row, which provides its own index |
| `EnoughForLine.tsx:73-75`, `:84-92` (9) | pure styling of the line (negative margins, flex, padding, border, overflow); jsdom lays nothing out |
| `EnoughForLine.tsx:144` no-marking guard → `false` | equivalent: without a marking `lineBeside` places no line, so the spacing is `{}` either way |
| `EnoughForLine.tsx:154` no-marking guard → `false` | unreachable: the # column is only declared while rows are marked |

# Mutation testing — 6150 (comments, questions and a Work Item's log, E3 slice 12)

Run 2026-10-05 on `main`, production code frozen after the maintainer's review changes and the second review pass.
Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.43 %** | 133 | 124 | 5 | 4 | 23 m 19 s |
| Frontend (StrykerJS), first run | 83.17 % | 398 | 331 | 66 | 1 | 14 m 5 s |
| Frontend (StrykerJS), after kill tests | **89.20 %** | 398 | 355 | 43 | 0 | 12 m 53 s |

Configs: `stryker.6150.backend.json`, `stryker.6150.frontend.json`, `vitest.stryker.6150.ts`.

## Backend

| file | killed / tested |
| --- | --- |
| `RefinementResolution.cs` | 56 / 56 |
| `SizingLogCommands.cs` | 25 / 25 |
| `RefinementViewQuery.cs` | 18 / 18 |
| `RefinementVotesController.cs` | 22 / 27 |
| `RefinementController.cs` | 3 / 4 |
| `WorkItemRouteReference.cs`, `SizingLogDto.cs`, `SizingRefusal.cs` | 4 / 4 |
| `IRefinementViewQuery.cs` | 0 / 3 |

Not mutated: `DemoDataService.cs` (4 of 514 lines changed; the seeded condition is pinned by
`LoadScenarios_Gravity_AnaLimasYesButOnGr051CarriesItsCondition`); `SizingCommentDto.cs` / `SizingVoteDto.cs` are plain
records once their length attribute went.

### Accepted survivors

| mutant | reason |
| --- | --- |
| `RefinementVotesController.cs:62`, `:84`, `:85` problem titles → `""` | the title is not user copy (the browser words refusals from the `code`, which the scenarios assert) |
| `RefinementVotesController.cs:86`, `:99` unreachable-switch messages | the switches are total over their enums |
| `RefinementController.cs:23` view `NotFound` | slice-11 code; the unknown-Team case is covered by tests outside this run's filter |
| `IRefinementViewQuery.cs:54` calendar default | slice-04 code |
| `IRefinementViewQuery.cs:73` `RowConversation.None` → `true` (×2) | the default is never read: every row the query builds carries the conversation it worked out |

## Frontend

| file | killed / tested |
| --- | --- |
| `useSizingLog.ts`, `useNameFirst.ts`, `useVoterIdentity.ts` (changed lines), `ConditionPrompt.tsx`, `RefinementView.tsx` (changed lines), `refinementWarnings.ts`, `sizingLogWording.ts`, `stageWording.ts` (changed lines), `SizingLogService.ts` (changed lines) | all killed |
| `useVoteCasting.ts` (changed lines) | 38 / 39 |
| `useCommentAdding.ts` | 19 / 20 |
| `voteWording.ts` (changed lines) | 12 / 13 |
| `VotesAndCommentsDialog.tsx` (changed lines) | 71 / 97 |
| `refinementColumns.tsx` (changed lines) | 26 / 36 |
| `WarningsIcon.tsx` | 9 / 13 |

### Closed by this pass (commit `09e396480`, each checked by hand against its mutant)

- The log's read guards: an answer for the Work Item open before, an answer after the dialog closed, a failure of the
  earlier read, a re-read for the Team the tab moved to, and nothing of the previous Work Item shown while the next is read.
- A second comment on a Work Item waits for the first to be answered; a commented row is never counted as just made
  ready; comments go to the Team the tab shows now.
- A "Yes, if…" that already carries its condition is cast without asking; a condition prompt keeps naming its Work Item
  after the Refinement is read again without it.
- Counts read while the voters are still being read name nobody; a Work Item without a split reads as nobody voted.
- The Warnings column sorts clean rows first one way and warned rows first the other.

### Accepted survivors

| mutant | reason |
| --- | --- |
| `useCommentAdding.ts:38` `showSending` deps | equivalent: the callback reads only a ref and a state setter |
| `useVoteCasting.ts:92` no-condition branch | equivalent: `comment: undefined` is dropped from the JSON body |
| `voteWording.ts:41` `?? ""` | equivalent: neither lookup has the key |
| `VotesAndCommentsDialog.tsx:125` `count === 0` | equivalent: once read the count is the number of names; while reading there are none |
| `refinementColumns.tsx:87` `type: "boolean"` | the sort order is the same either way; it only swaps the column's yes/no filter for a text filter |
| `refinementColumns.tsx:26-32`, `:41`; `WarningsIcon.tsx:23`, `:29`, `:43`; `VotesAndCommentsDialog.tsx` 25 `sx` literals | pure styling (visually-hidden label, margins, colours, widths, `whiteSpace`); every accessible name and every piece of copy is asserted |

# Mutation testing — 6145 (the band's two likelihoods, E2 slice 07)

Run 2026-10-05 on `main`, production code frozen after the maintainer's layout review, the refactor and the review
fixes. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **80.59 %** | 170 | 137 | 22 | 11 | 4 m 57 s |
| Frontend (StrykerJS), first run | 84.35 % | 115 | 97 | 18 | 0 | 8 m 8 s |
| Frontend (StrykerJS), after kill tests | **91.30 %** | 115 | 105 | 10 | 0 | 7 m 43 s |

Configs: `stryker.6145.backend.json`, `stryker.6145.frontend.json`, `vitest.stryker.6145.ts`.

## Backend

Whole-file mutation (Stryker.NET ignores line ranges), so two of the three files carry earlier slices' code. The
filter is scoped to unit namespaces: an earlier attempt that included the acceptance suites ran 18 minutes without
finishing a single mutant batch.

| file | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- |
| `RefinementSettingsValidator.cs` | 75 | 0 | 0 | 100 % |
| `RefinementSettingsDto.cs` | 26 | 7 | 2 | 74.3 % |
| `TeamExtensions.cs` | 36 | 15 | 9 | 60.0 % |

### Survivors inside the slice's own lines — killed by the acceptance suite, proven by hand

The slice changed `RefinementSettingsDto.cs` lines 20 and 33–62 and `TeamExtensions.cs` lines 143 and 178–187. Two
survivors fall there, and both are lines only the host-level scenarios reach:

- `RefinementSettingsDto.cs:45` — the read mapping into `RefinementBandDto` (block removal). Probe: assigning `null`
  to both ends fails 15 of the 18 `Slice07BandPercentiles` scenarios.
- `TeamExtensions.cs:143` — the `SyncBand` call (statement removal). Probe: passing `null` instead of the saved band
  fails 16 of the 18.

### Accepted survivors — earlier slices' code in the same files

The other 20 survivors and all 11 no-coverage mutants sit in code this slice did not touch: Team throughput and
process-behaviour date conversion, cycle-time definition ids, refinement state merging, stage-rule and cadence DTO
mappings. Each is covered by its own slice's acceptance scenarios, which this unit-scoped run leaves out.

## Frontend

| file | killed | survived | score |
| --- | --- | --- | --- |
| `refinementBand.ts` | 39 | 0 | 100 % |
| `RefinementBandSettings.tsx` | 51 | 10 | 83.6 % |
| `RefinementSettingsSection.tsx` (band lines) | 10 | 0 | 100 % |
| `needWording.ts` (band lines) | 5 | 0 | 100 % |

### Closed by this pass (`e0b793702`)

- `isBandInverted`'s range guard (`true`, `&&` → `||`): low 96 against high 85 is out of range, not inverted — only
  the low field is marked.
- An emptied field read as 0: a cleared low end shows empty and says "Between 50% and 95%.".
- The band-equality check ignoring the low end: after a low-only change the tooltip follows the new low end.
- The forecast sentence of the tooltip, in the Team's own word for Refinement.
- `error={true}`: a valid band marks neither field.
- The `%` adornment.

### Accepted survivors

All 10 are layout styling jsdom does not observe: the `Grid size` objects, the heading row's and the fields row's
`sx` (flex, wrap, gap, alignment) and the fields' `width: 180`. The side-by-side layout was checked by the maintainer
in the browser.
