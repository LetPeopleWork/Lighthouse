# Mutation testing — 6458 (a) (category lists hold raw states; a mapping is a display grouping)

Run 2026-11-03 against branch `bug-6458-typed-category-entries` @ `140e43073` (frozen code; the kill tests
landed afterwards, backend in `688f69382`, frontend in `e445aa768`; no production code changed). Gate is 80 %
kill rate on both stacks: met by the first run on each, before any kill test.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend, full run (Stryker.NET 5.0.0) | **87.20 %** | 481 | 436 | 45 | 19 | 0 | 17 m 05 s |
| Backend, kill round (4 files) | **95.02 %** | 277 | 267 | 10 | 4 | 0 | 11 m 06 s |
| Frontend, full run (StrykerJS 10.1.1) | **80.10 %** | 591 | 483 | 108 | 12 | 0 | 71 m 07 s |
| Frontend, kill round (incremental) | **89.22 %** | 103 re-tested, 500 reused | 538 | 62 | 3 | 0 | 17 m 48 s |

Configs: `stryker.6458.backend.json`, `stryker.6458.frontend.json`, `vitest.stryker.mutation.ts`.
The score counts no-coverage mutants as not detected (`killed / (killed + survived + no coverage)`).

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```bash
dotnet stryker --config-file ../../docs/feature/bug-6458-typed-category-entries/mutation/stryker.6458.backend.json
```

The test-case filter takes the unit suites only: everything under `Tests.API.Helpers`, `Tests.API.DTO`, the
`*SyncTest` classes, `Tests.Models`, the seeder's unit tests, the ServiceNow span mapper, the metrics-as-of-group
test, the `*RawLists*` tests, the CSV demo journey with a mapping and the state-transition mapper. It leaves out
`API.Integration`, `Integration.Containers` (Postgres containers) and every `*IntegrationTest`, which would turn
minutes into the better part of an hour.

### Per file — full run

| file | valid | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `Models/StateCategoryLists.cs` | 76 | 70 | 5 | 1 | 92.11 % |
| `Models/WorkTrackingSystemOptionsOwner.cs` | 24 | 24 | 0 | 0 | 100 % |
| `Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs` | 63 | 50 | 10 | 3 | 79.37 % |
| `API/Helpers/StateMappingValidator.cs` | 105 | 98 | 3 | 4 | 93.33 % |
| `API/Helpers/CycleTimeDefinitionValidator.cs` | 37 | 30 | 7 | 0 | 81.08 % |
| `API/Helpers/StateListEntries.cs` | 4 | 4 | 0 | 0 | 100 % |
| `API/Helpers/RefinementSettingsValidator.cs` | 75 | 75 | 0 | 0 | 100 % |
| `API/Helpers/TeamExtensions.cs` | 60 | 39 | 12 | 9 | 65.00 % |
| `API/Helpers/PortfolioExtensions.cs` | 34 | 25 | 7 | 2 | 73.53 % |
| `API/DTO/SettingsOwnerDtoBase.cs` | 5 | 5 | 0 | 0 | 100 % |
| `Services/.../ServiceNow/ServiceNowStateSpanMapper.cs` | 17 | 16 | 1 | 0 | 94.12 % |

### Per file — kill round

The kill round narrowed `mutate` to the four files below and the filter to `Tests.API.Helpers`, `Tests.Models`
and the seeder's unit tests. Its config is the full one with those two keys changed; it is not committed.

| file | valid | killed | survived | no coverage | score | full run |
| --- | --- | --- | --- | --- | --- | --- |
| `CycleTimeDefinitionValidator.cs` | 37 | 36 | 1 | 0 | 97.30 % | 81.08 % |
| `StateMappingValidator.cs` | 105 | 103 | 1 | 1 | 98.10 % | 93.33 % |
| `StateCategoryLists.cs` | 76 | 72 | 4 | 0 | 94.74 % | 92.11 % |
| `CategoryListNormalisationSeeder.cs` | 63 | 56 | 4 | 3 | 88.89 % | 79.37 % |

### Closed by this pass (`688f69382`)

- **Cycle-time ordering at the edges** (`CycleTimeDefinitionValidator` line 46 `>= 0` to `> 0` twice, `&&` to
  `||`; line 63 `-1` to `+1`): a definition starting and ending on the first state of the workflow is refused; one
  whose end state, or whose start state, is missing from the workflow is not refused at save, also when the other
  boundary is the first state.
- **A blank boundary** (`StateCategoryLists` line 75, no coverage): a definition with no start state is not refused
  and does not throw.
- **Which stored definition is let through** (`CycleTimeDefinitionValidator` line 56, both `&&` to `||`): a new
  definition copying a stored one inside one group is refused, and so is the stored one when this save moves its
  start state.
- **Shadowing an owner already stored** (`StateMappingValidator` line 81 `||` to `&&` and `!` removed; line 85
  `Any` to `All` and `?? false` dropped): a name stored beside one of its states is let through when it comes back
  unchanged, and refused when this save lists it there anew, even though the mapping is unchanged.
- **A chain where only one side changed** (`StateMappingValidator` line 103 `||` to `&&`): a new mapping named like
  a state that an unchanged stored mapping groups is refused with the chain sentence.
- **Shadowing by a mapping that is not the first** (`StateCategoryLists` line 210 `< 0` to `> 0`): the second of two
  mappings shadowing a Doing state is refused with its own name.
- **The start-up normalisation's skips** (`CategoryListNormalisationSeeder` lines 81, 83, 96, 98): an owner with a
  mapping that has no states is left whole, its other groups included, and counted as skipped; a failed save
  counts the owner as skipped and not as rewritten.

### Accepted survivors

- `StateCategoryLists` lines 26, 30, 33 (`??=` to `=`): the three lookups are caches; rebuilding them on every read
  gives the same answers, only slower. Equivalent.
- `StateCategoryLists` line 210 (`group < 0 ? null : …` to always the mapping): `GroupNamed` is only ever asked
  about an entry already classified as shadowing, which always names a group, so the null branch is unreachable
  through the public API.
- `CycleTimeDefinitionValidator` line 63 (`entry == null ? -1 : …` to always `IndexOf`): `IndexOf(null)` on a list
  of strings already returns -1. Equivalent.
- `StateMappingValidator` line 25 (`storedOwner?.WaitStates ?? []` without `?? []`): the stored wait states are only
  read for a mapping that is unchanged, and a mapping can only be unchanged when there is a stored owner, whose
  wait states are never null. Equivalent.
- `StateMappingValidator` line 85 (`?? false` to `?? true`, no coverage): the fallback is used only without a stored
  owner, and then every mapping counts as changed, so the `||` never reaches it. Unreachable.
- `CategoryListNormalisationSeeder` line 102: the Information line for a rewritten owner (statement and text). Log
  text only; the counts in the summary line are pinned.
- `CategoryListNormalisationSeeder` lines 125, 141 (catch block removed): Stryker.NET fills the removed block with
  `return default`, which is the `null` the block returned. Equivalent.
- `CategoryListNormalisationSeeder` line 88 (no coverage, the early return for an owner whose lists are already raw)
  and lines 159, 163 (no coverage, the refused-write path that detaches the row): reached only by the Postgres and
  SQLite container suites, `CategoryListNormalisationSeederOnBothProvidersTests.EveryStoredShape_IsRewrittenOnceAndLeftAloneTheSecondTime`
  and `CategoryListNormalisationSeederIsolationTests.AnOwnerWhoseWriteIsRefused_IsSkipped_AndTheOwnersAfterItAreStillWritten`,
  which the filter leaves out.
- `TeamExtensions` and `PortfolioExtensions` (19 survived, 11 no coverage in the full run): all in code this change
  did not touch. They are the cycle-time id assignment (`existingIds` / `nextId`, lines 83/88 and 106/111),
  `CreateTeamDto` (lines 15–29, covered by the API integration suites), the throughput-history start date (line 43)
  and the refinement sync outside the candidate list (lines 124, 132, 144). The lines this change rewrote
  (`SyncStates` and the refinement candidates) have no survivors.
- `ServiceNowStateSpanMapper` line 98 (`string.Empty` to `"Stryker was here!"`): the earliest arrival's from-state is
  compared only against the team's lists, and neither the empty string nor the mutant's text is a state in any
  of them, so the crossing check reads both alike. Equivalent, and in a line this change did not touch.

### Not mutated

- `Models/ListEntryKind.cs`, `Models/LegacyListShape.cs`, `Models/IWorkItemQueryOwner.cs`: enums and an interface;
  nothing to mutate.
- `Services/Implementation/BaseMetricsService.cs` (~1370 lines, 70 changed), `TeamMetricsService.cs`,
  `PortfolioMetricsService.cs`: the change swaps how a category's states are read (through the owner's category
  lists) in a few places of large services. Whole-file mutation would bury those lines under untouched metrics code.
  Covered by `MetricsAsOfGroupNameTest`, `TeamMetricsServiceTests`, `PortfolioMetricsServiceTests` and the API
  integration suites `CycleTimeDefinitionOverAMappingIntegrationTest`, `FlowEfficiencyReadApiIntegrationTest` and
  `FlowEfficiencyPortfolioReadApiIntegrationTest`.
- `Factories/IssueFactory.cs`, `AzureDevOpsWorkTrackingConnector.cs`, `CsvWorkTrackingConnector.cs`: a few lines each
  that read the raw lists instead of expanding group names, in large connector files. Covered by
  `IssueFactoryRawListsTest`, `AzureDevOpsRawListsTest` and `CsvDemoJourneyWithStateMappingTest`.
- `API/SuggestionsController.cs`, `API/TeamController.cs`, `API/PortfolioController.cs`, `Program.cs`: wiring (the
  stored owner handed to the validators, the seeder registered). Covered by
  `CategoryListsHoldRawStatesSettingsIntegrationTest` and the seeder container suites.

## Frontend

Run from `Lighthouse.Frontend/`, with `vitest.stryker.mutation.ts` copied from this folder into
`Lighthouse.Frontend/` first (that path is gitignored there, so this copy is the source of truth):

```bash
pnpm exec stryker run ../docs/feature/bug-6458-typed-category-entries/mutation/stryker.6458.frontend.json --incremental
```

`mutate` holds the changed line ranges of every production file under `src/` (from
`git diff -U0 803b73acb..HEAD`), 107 ranges over 18 files; `models/Common/BaseSettings.ts` (type only) and
`utils/stateMappingRuleCases.json` (data) are left out. Coverage is `perTest` and the incremental file lives in
`reports/mutation/`, so the kill round re-tested only the 103 mutants whose covering specs changed. The vitest
config narrows the run to the 34 specs covering those files. `git status` was clean after both runs.

### Per file

| file | valid | killed | survived | no coverage | full run | kill round |
| --- | --- | --- | --- | --- | --- | --- |
| `utils/stateMappingValidation.ts` | 281 | 227 → 262 | 45 → 19 | 9 → 0 | 80.78 % | 93.24 % |
| `utils/stateMappingView.ts` | 61 | 60 | 1 | 0 | 98.36 % | 98.36 % |
| `utils/isCycleTimeDefinitionValid.ts` | 18 | 15 → 16 | 3 → 2 | 0 | 83.33 % | 88.89 % |
| `utils/flowEfficiency.ts` | 9 | 9 | 0 | 0 | 100 % | 100 % |
| `components/Common/StatesList/StatesList.tsx` | 50 | 31 → 41 | 19 → 9 | 0 | 62.00 % | 82.00 % |
| `components/Common/ItemListManager/ItemListManager.tsx` | 43 | 24 → 33 | 19 → 10 | 0 | 55.81 % | 76.74 % |
| `components/Common/StateMappings/StateMappingsEditor.tsx` | 8 | 8 | 0 | 0 | 100 % | 100 % |
| `components/Common/Team/RefinementSettingsSection.tsx` | 18 | 18 | 0 | 0 | 100 % | 100 % |
| `components/Common/Team/ModifyTeamSettings.tsx` | 12 | 9 | 1 | 2 | 75.00 % | 75.00 % |
| `components/Common/ProjectSettings/ModifyProjectSettings.tsx` | 9 | 6 | 2 | 1 | 66.67 % | 66.67 % |
| `components/Common/Charts/CumulativeStateTimeChart.tsx` | 2 | 1 | 1 | 0 | 50.00 % | 50.00 % |
| `hooks/useModifySettings.ts` | 9 | 6 | 3 | 0 | 66.67 % | 66.67 % |
| `pages/Common/MetricsView/useOwnerMetricsSettings.ts` | 52 | 43 | 9 | 0 | 82.69 % | 82.69 % |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 22 | 21 | 1 | 0 | 95.45 % | 95.45 % |
| `pages/Teams/Detail/TeamMetricsView.tsx` | 6 | 4 | 2 | 0 | 66.67 % | 66.67 % |
| `pages/Portfolios/Detail/PortfolioMetricsView.tsx` | 3 | 1 | 2 | 0 | 33.33 % | 33.33 % |

`CycleTimesEditor.tsx` and `WaitStatesEditor.tsx` changed only imports and deletions, so their ranges hold no
mutants.

### Closed by this pass (`e445aa768`)

- **A mapping's name read as its states** (`stateMappingValidation` lines 156–177, 127, 159): a name standing in
  Doing with one of its states in Done is refused; a name that is one of its own states counts as that state plus
  the states not listed beside it; a name another mapping groups counts as a plain state.
- **Three categories** (lines 241, 243): a state in To Do, Doing and Done is named that way.
- **A nameless mapping** (line 297, `&&` to `||` and the name check dropped): it only gets the missing-name error.
- **Wait states against a stored owner** (lines 309, 347): a wait state the edit adds inside an unchanged stored
  mapping is refused, and so is a stored one once the edit narrows its mapping.
- **The stored settings** (lines 35–41): nothing before loading; kept as the server holds them; missing fields
  read as empty.
- **A cycle time ending on the first state** (`isCycleTimeDefinitionValid` line 44 `>= 0` to `> 0`).
- **Suggestions** (`StatesList` lines 157–191): a mapping's name is not offered in any list, even where none of its
  states is listed (before, the only test listed the states, so the chip itself hid the name).
- **Chips** (`ItemListManager` lines 112–140): a blank item gets no chip; a grouped chip is filled, has the group
  icon and is a named group; a plain chip is none of that.

### Accepted survivors — equivalent

- `stateMappingValidation` lines 156, 158, 162 (the kind strings to `""`) and 174, 177 (which branch expands an
  entry): an empty kind falls into the branch the real one would, or into the plain "the mapping's states" branch.
  That branch differs from the own-state one only in keeping members already listed and in order, and the list is
  de-duplicated and then read for membership only. A collapsed name kept beside its states is not a member of any
  mapping, so no rule looks it up.
- `stateMappingValidation` lines 71 and 187 (blank states and entries not dropped), 206 and 334 (`?? []` fallbacks
  and the blank wait-state filter), 273 (`first?.` to `first.`): a blank is never a mapping member or a listed
  state, so keeping it changes no answer. The `?? []` fallbacks only differ for a missing list, and
  `first` is always set when the callback runs.
- `stateMappingView` line 31 (`join("")` to another separator): every key passes through the same fold, so two
  states still compare equal exactly when they did.
- `isCycleTimeDefinitionValid` line 27 (the blank-boundary guard): a blank boundary folds to an empty key, which no
  workflow state has, so the index is -1 either way.
- `useOwnerMetricsSettings` lines 36, 42, 47, 48 (the `isCurrentOwner` guard): a late answer for an earlier owner is
  also discarded by the `loaded.ownerId === ownerId` check, so dropping the guard changes nothing on screen.
- `ItemListManager` line 155 (a plain chip wrapped in a tooltip with no title): MUI shows nothing for an empty title.

### Accepted survivors — log text or styling

- `useOwnerMetricsSettings` line 41, `TeamMetricsView` line 51, `PortfolioMetricsView` line 32: the owner label is
  used only in the `console.error` text.
- `ItemListManager` lines 146–151: drag cursor and opacity of a dragged chip, unchanged lines that moved into the
  new map callback.

### Open — real gaps, not closed in the one kill round

These are missing tests rather than equivalent mutants. The gate holds without them; they are listed so a later
change can pick them up.

- **Memo and callback dependency lists set to `[]`** (`StatesList` lines 123–148 and 161, `CumulativeStateTimeChart`
  line 320, `ModifyProjectSettings` line 209, `TeamMetricsView` line 39, `PortfolioMetricsView` line 21): every test
  renders once with fixed props, so nothing notices a list or chart that would stop following its props. A re-render
  test per component would pin them.
- **`StatesList` line 72**: a list without a reorder callback would get a reorder handler, making its chips
  draggable. No test checks that To Do or Done stay undraggable when only Doing can be reordered.
- **`useOwnerMetricsSettings` lines 68, 69 (`??` to `&&`) and 11, 12**: the metrics views would ignore the loaded
  mappings and cycle-time definitions. The metrics-view tests do not check a chart that depends on a mapping.
- **`useModifySettings` lines 318, 506**: the stored snapshot the rules judge against is refreshed after a reload
  and after a save. No test makes a second edit after a save and checks it is judged against what was just saved.
- **`BaseMetricsView` line 1311**: a widget would dim its content while its column settings still load, rather than
  show a lone spinner. The loading tests have the columns ready.
- **`ModifyTeamSettings` lines 251, 289 and `ModifyProjectSettings` line 259** (`stateMappings || []`): the
  fallback for a settings object without mappings; every test passes them.
- **`stateMappingValidation` lines 143, 297 (`> 0` to `>= 0` or always true)**: a mapping whose states are all blank
  would count as a group. It differs only for such a mapping; the page refuses an empty mapping, but not one whose
  only state is blank.
