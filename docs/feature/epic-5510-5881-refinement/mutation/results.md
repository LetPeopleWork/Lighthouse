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
