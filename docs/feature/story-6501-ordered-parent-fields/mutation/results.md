# Mutation testing: 6501 (Ordered parent sources)

Run 2026-11-06 against `main` @ `857c21a37`, production code frozen. New tests only. The gate is an 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **96.32 %** | 489 | 471 | 14 | 4 | 11 m 41 s + 18 m 56 s, kill round 25 m 17 s |
| Frontend (StrykerJS 10.1.1) | **92.67 %** | 464 | 430 | 34 | 0 | 53 m 18 s, kill round 8 m 55 s |

First runs, before any new test: backend 76.69 % (375 of 489), frontend 87.28 % (405 of 464). No timeouts on either
stack.

**183 further backend mutants are `CompileError`: they were never tested and are in none of the figures above**, 129 of
them on lines this feature changed. Read as survivors, the backend would be 70.09 % (471 of 672). See
[Never tested](#never-tested).

Configs: `stryker.6501.backend.json` (the feature's own files), `stryker.6501.backend.large.json` (large files, changed
lines counted), `stryker.6501.backend.killround.json`, `stryker.6501.frontend.json`, `vitest.stryker.mutation.ts`.
Backend runs from `Lighthouse.Backend.Tests/`, frontend from `Lighthouse.Frontend/`.

## Backend

The score adds two sets: every mutant of the 20 files below, and only the changed lines' mutants of the large files.
Changed lines are those `git blame` gives to a `(parents)` commit since `87ac968e7`. A mutant counts as killed if the
first run or the kill round killed it. The kill round's filter is narrower, so it shows two mutants at
`ConnectionDefaultsDto.cs:154` as survivors that the first run killed through
`WorkTrackingSystemConnectionsControllerTest`. `API.Integration`, `Dogfood`, `Integration.Containers` and the
live-connector categories stayed out of every filter.

Whole files:

| file | tested | first run | final | survived | no coverage | never tested |
| --- | --- | --- | --- | --- | --- | --- |
| ParentSources.cs | 20 | 85.00 % | 100 % | 0 | 0 | 12 |
| ParentSourceEntry.cs | 6 | 83.33 % | 100 % | 0 | 0 | 0 |
| ParentSourceEntryDto.cs | 7 | 14.29 % | 100 % | 0 | 0 | 0 |
| ParentSourceSelector.cs | 19 | 100 % | 100 % | 0 | 0 | 9 |
| ParentReadings.cs | 3 | 0 % | 0 % | 3 | 0 | 0 |
| ParentChoice.cs | 2 | 100 % | 100 % | 0 | 0 | 0 |
| ParentResolution.cs | 35 | 94.29 % | 94.29 % | 2 | 0 | 12 |
| ParentPlacement.cs | 4 | 100 % | 100 % | 0 | 0 | 2 |
| ParentPlacementReport.cs | 41 | 73.17 % | 95.12 % | 0 | 2 | 13 |
| ConnectionFieldDefaults.cs | 9 | 100 % | 100 % | 0 | 0 | 6 |
| ConnectionDefaultsDto.cs | 18 | 66.67 % | 100 % | 0 | 0 | 13 |
| SettingsOwnerDtoBase.cs | 6 | 83.33 % | 100 % | 0 | 0 | 3 |
| OwnerSettingRefusals.cs | 42 | 64.29 % | 95.24 % | 1 | 1 | 23 |
| WorkTrackingSystemOptionsOwnerExtensions.cs | 15 | 66.67 % | 86.67 % | 1 | 1 | 7 |
| WorkTrackingSystemOptionsOwner.cs | 33 | 96.97 % | 100 % | 0 | 0 | 7 |
| WorkTrackingSystemConnection.cs | 23 | 91.30 % | 91.30 % | 2 | 0 | 2 |
| FetchFingerprint.cs | 15 | 100 % | 100 % | 0 | 0 | 3 |
| ChangedConnectionDefault.cs | 6 | 100 % | 100 % | 0 | 0 | 0 |
| AdditionalFieldDefinition.cs | 4 | 25.00 % | 50.00 % | 2 | 0 | 0 |
| AdditionalFieldInUse.cs | 23 | 100 % | 100 % | 0 | 0 | 15 |
| **Subtotal** | **331** | 82.18 % | **95.47 %** | 11 | 4 | 127 |

Changed lines only (Stryker.NET mutates whole files, so the whole-file figures describe code this feature did not touch):

| file | tested | first run | final | survived | never tested | whole file, final (killed / survived / no coverage) |
| --- | --- | --- | --- | --- | --- | --- |
| JiraWorkTrackingConnector.cs | 29 | 72.41 % | 89.66 % | 3 | 2 | 628 / 217 / 90 |
| AzureDevOpsWorkTrackingConnector.cs | 34 | 52.94 % | 100 % | 0 | 8 | 329 / 113 / 91 |
| WorkItemService.cs | 34 | 76.47 % | 100 % | 0 | 12 | 264 / 90 / 25 |
| ConnectionSaveService.cs | 39 | 89.74 % | 100 % | 0 | 20 | 87 / 1 / 2 |
| WorkTrackingSystemConnectionController.cs | 22 | 13.64 % | 100 % | 0 | 14 | 76 / 10 / 26 |
| **Subtotal** | **158** | 65.19 % | **98.10 %** | 3 | 56 | |

`Portfolio.cs`, `Team.cs` and `WorkItemBase.cs` were in the large run and produced no mutant on a changed line.

### Closed by this pass

- **`ParentSourceEntryDto.cs:23`, `:35`**: nothing outside the acceptance suite told an echo of the old field from an
  edit of it. `ParentSourceEntryDtoTest` pins the three combinations of field and list.
- **`OwnerSettingRefusals.cs`, 13 without coverage, `:49`**: `OwnerSettingRefusalsTest` pins code, title and detail of
  every refusal of a list, which connection names a repeated field, and that one lacking field refuses a list whose other
  fields are held. The last one also closes `AdditionalFieldDefinition.cs:19`.
- **`ConnectionDefaultsDto.cs:27`, `:113`, `:126`, `:131`**: reading a connection's lists, the fields of a sent list, an
  explicit null, and the old field beside the list read versus beside a new one.
- **`WorkTrackingSystemOptionsOwnerExtensions.cs:39`, `:42`, `:46`, `SettingsOwnerDtoBase.cs:108`**:
  `WorkTrackingSystemOptionsOwnerExtensionsParentSourcesTest` pins what a save means by a list, the old field, or both.
- **`ParentSources.cs:58`, `:61`, `ParentSourceEntry.cs:33`**: stored text of `null`, and the words of a refused read.
- **`WorkTrackingSystemOptionsOwner.cs:184`**: a list left behind on a follower by a build one release back is not read.
  Nothing in this build can write that row, so the test sets it by reflection.
- **`ParentPlacementReport.cs`, 9 without coverage**: the owner names and the per-position fetch line.
- **`WorkTrackingSystemConnectionController.cs`, 19 without coverage**: the refusals of a default's list, one field of
  no connection in a list, and the old column kept in step when a list is held.
- **`ConnectionSaveService.cs:231`, `:233`, `:235`**: how the log names a default's list.
- **`WorkItemService.cs:1069`, `:1070`, `:1115` to `:1124`**: the warning for a source that matched nothing, with and
  without a next source.
- **`AzureDevOpsWorkTrackingConnector.cs`, 16 mutants at `:820` to `:829`, `:1577` to `:1589`, `:1854`**: findings per
  entry, the relation types that could not be listed, the own parent with and without one, and an empty field against a
  relation type.
- **`JiraWorkTrackingConnector.cs:335`, `:1919`, `:1963`, `:1973`**: the winning reading hangs the Work Item, and an
  entry that matched nothing is reported with whether a source came after it.

### Accepted survivors

On changed lines:

- `OwnerSettingRefusals.cs:74`, the setting's name: equivalent. Both strings built from it are overridden on that row.
- `OwnerSettingRefusals.cs:124`, `First` to `FirstOrDefault`: equivalent. The line is reached only for a list refused as
  a duplicate, so a repeated entry always exists.
- `ParentPlacementReport.cs:78`, `:97`, no coverage: the message of a throw no placed record can reach.
- `JiraWorkTrackingConnector.cs:1964`, removing the early return after a refused link type list: pinned by
  `ParentSourcesRefreshScenarios.A_refused_link_type_list_does_not_claim_that_an_entry_matched_nothing`, outside the
  filter. Named from the scenario, not re-run against this mutant.
- `JiraWorkTrackingConnector.cs:2033`, the empty field name: equivalent. That name is read only for a list holding the
  own parent, which takes the other branch.
- `JiraWorkTrackingConnector.cs:2031`, always looking the field name up: **not killed, and no test is known to pin it.**
  It differs only on an instance whose field map lacks that name, where the lookup would throw.

`ParentReadings.cs:15`, `:17`, `:23` (3): `string.Empty` as the key of a reading that has none. Equivalent, because
only a field value or the tracker's own reading has its key read. `git blame` credits no line of this file to a
`(parents)` commit, but the file is the feature's, so it stays in the score.

Outside the changed lines, in code this feature did not touch, left as they are (8):
`WorkTrackingSystemOptionsOwnerExtensions.cs:16`, `:64`; `AdditionalFieldDefinition.cs:7`, `:9`;
`ParentResolution.cs:22`, `:128`; `WorkTrackingSystemConnection.cs:125`, `:150`.

### Never tested

183 `CompileError` mutants across the scored set: 127 in the whole files and 56 on the large files' changed lines; 129
of the 183 are on changed lines. There are two main causes:

- `x is { } y` patterns, where `is not`, a forced conditional or a logical swap breaks definite assignment, for example
  `ParentSources.cs:73`, `FetchFingerprint.cs:163`, `ConnectionDefaultsDto.cs:28`, `:111`, `:116`.
- `Count` to `Sum` on a list, which does not compile. Stryker then also rolls back the real mutants sharing that
  expression, such as `entries.Count > Maximum` at `ParentSources.cs:84` and `choice.WinnerPosition + 1` at
  `ParentSourceSelector.cs:61`. `ParentSourceSelector.HangUnder`'s loop (`:97` to `:100`) went the same way.

So the 100 % of `ParentSourceSelector.cs` covers 19 of its 28 mutants. The other nine depend on what
`ParentSourceSelectorTest` asserts, with no mutation evidence behind them. The kill round tried `"ignore-mutations": ["linq.Count"]`
to free the rolled-back ones. It changed nothing: the same mutants came back as `CompileError`. The setting is left in
`stryker.6501.backend.killround.json` because that is what ran. Freeing them means rewriting the production guards in
plain steps, which the freeze rules out.

### Not mutated

- The six migration files, the two `AddParentSources` migrations with their designers and both model snapshots: generated.
- `LighthouseAppContext.cs`: 36 changed lines of EF configuration in a 965-line file, pinned by
  `ParentSourcesColumnConversionTests` under `Integration/Containers`, which needs a real Postgres.
- `IWorkItemQueryOwner.cs`, `IConnectionSaveService.cs`: interfaces. `UsageDataEventName.cs`: an enum. No mutants.
- `PortfolioController.cs`, `PortfoliosController.cs`: one changed line each, a comment.
- `Scripts/test-selection/*.sh`: shell, no mutation tool.

## Frontend

Line ranges on the changed lines, with `parentSources.ts`, `ParentSourcesList.tsx`, `useParentSourcesSavedReporter.ts`
and `ParentSourcesSetting.tsx` whole.

| file | tested | first run | final | survived |
| --- | --- | --- | --- | --- |
| parentSources.ts | 148 | 90.54 % | 98.65 % | 2 |
| ParentSourcesList.tsx | 109 | 76.15 % | 83.49 % | 18 |
| ParentSourcesSetting.tsx | 52 | 92.31 % | 98.08 % | 1 |
| refreshFindingSentences.ts | 29 | 100 % | 100 % | 0 |
| ModifyConnectionSettings.tsx | 25 | 96.00 % | 96.00 % | 1 |
| useModifySettings.ts | 23 | 95.65 % | 95.65 % | 1 |
| useParentSourcesSavedReporter.ts | 19 | 73.68 % | 73.68 % | 5 |
| EditConnection.tsx | 14 | 78.57 % | 78.57 % | 3 |
| whatASaveAsks.ts | 13 | 84.62 % | 100 % | 0 |
| fieldDirectionTips.ts | 10 | 90.00 % | 90.00 % | 1 |
| AdvancedInputs.tsx | 6 | 83.33 % | 83.33 % | 1 |
| WorkTrackingSystemService.ts | 4 | 75.00 % | 75.00 % | 1 |
| defaultsKeepingOnlyFields.ts | 4 | 100 % | 100 % | 0 |
| ModifyProjectSettings.tsx | 3 | 100 % | 100 % | 0 |
| ModifyTeamSettings.tsx | 3 | 100 % | 100 % | 0 |
| FormSelectField.tsx | 1 | 100 % | 100 % | 0 |
| WorkTrackingSystemConnection.ts | 1 | 100 % | 100 % | 0 |

### Closed by this pass

- **`ParentSourcesList.tsx:143`**: nothing pressed Remove on a source that can go. Also closed: the row numbers (`:108`),
  the picker's help text (`:200`), no warning when the own parent is listed (`:240`, four mutants) and focus after a move
  up (`:102`).
- **`parentSources.ts:41`, `:54`, `:170`, `:171`**: `listsTheSameSources`, `ownerOfParentSourcesDefault` and
  `namesAnUnknownField` had no direct test.
- **`whatASaveAsks.ts:127`**: a single-field default that stayed or moved.
- **`ParentSourcesSetting.tsx:50`, `:75`, `:105`**: the section before the settings arrive.

### Accepted survivors

- **Styling (14)**: `ParentSourcesList.tsx:65` to `:68`, `:107`, `:110`, `:111`, `:245` to `:248`, and
  `AdvancedInputs.tsx:47`. `sx`, colours, layout.
- **`useParentSourcesSavedReporter.ts:11`, `:12`, `:22`, `EditConnection.tsx:35` (two)**: equivalent. One made-up entry
  in place of an empty list differs from a list of several in the same way an empty list does, and a list of one never
  counts.
- **`useParentSourcesSavedReporter.ts:29`, `:42`**: hook dependency arrays, whose values do not change.
- **`ParentSourcesList.tsx:98`**: `before === position` to `false`. The effect runs only when its dependencies change, so
  a press that moved nothing never gets there.
- **`ParentSourcesList.tsx:102`** (`<` to `<=`): equivalent, the positions differ by then. **`:103`**, **`EditConnection.tsx:129`**,
  **`useModifySettings.ts:538`**, **`ParentSourcesSetting.tsx:52`**: optional chaining on a value present wherever the
  line is reached.
- **`ParentSourcesList.tsx:168`**: `allowNone` is false, so the picker never hands back null. **`:178`**: a React key.
- **`fieldDirectionTips.ts:125`**: the own parent's name reads no definitions.
- **`parentSources.ts:26`**: two kinds with the same id cannot exist.
- **`parentSources.ts:166`, `WorkTrackingSystemService.ts:51`**: reported survived, and very likely stale. Both are built
  when the module loads, and the incremental run reused the first run's verdict for them. The new tests `namesAnUnknownField`
  and "keeps a default's list of parent sources" assert against exactly these, but this was reasoned, not hand-checked.
- **`ModifyConnectionSettings.tsx:206`**: **a real gap, left open.** Dropping the filter would offer a field added in
  this same save in the defaults' list, and no spec adds one, because the main spec mocks the fields editor. The server
  refuses such a save, as
  `UpdateWorkTrackingSystemConnection_AParentSourcesDefaultWithOneFieldOfNoConnection_IsRefused` pins.

### Not mutated

- `ParentSourceEntry.ts`, `BaseSettings.ts`, `RefreshLog.ts`: types only.
- `UsageDataService.ts:50-50`: in `mutate`, produced no mutant.

## Configuration notes

- `vitest.stryker.mutation.ts` needs `onUnhandledError` from `vitest.config.ts`. Without it the deliberate
  `ExpectedTestFailure` rejections crash the dry run with `TypeError: Cannot convert object to primitive value`, which
  says nothing about the cause. Run the narrowed config through plain `vitest run` first to see the real errors. The
  reference config at `servicenow-multi-table-work-item-types` predates this.
- The frontend's first run took 53 minutes, over the usual budget. The incremental rerun took 9.
