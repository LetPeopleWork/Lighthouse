# Mutation testing — 6300 (Jira dependencies from a named link, slices 01 and 02)

Run 2026-10-17 against `main` @ `d23fe484d`, production code frozen; only tests were added. Gate is 80 %
kill rate on each stack, judged on the code this story changed.

| stack | score | scored | killed + timeout | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 58.03 % | 1408 | 817 | 591 | 12 m 15 s |
| Backend, this story's lines, first run | 77.88 % | 104 | 81 | 23 | — |
| **Backend, this story's lines, after the kill round** | **95.19 %** | 104 | 99 | 5 | 6 m 04 s |
| Frontend (StrykerJS 10.0.0), first run | 67.39 % | 92 | 62 | 30 | 2 m 17 s |
| **Frontend, after the kill round (forced full run)** | **94.57 %** | 92 | 87 | 5 | 1 m 29 s |

Configs: `stryker.6300.backend.json` (the full run), `stryker.6300.backend.kill.json` (the narrowed
kill round), `stryker.6300.frontend.json`, `vitest.stryker.6300.config.ts` (copied into
`Lighthouse.Frontend/` for the run, because its `setupFiles` path is relative to the config).

## Backend

### How the story's lines were found

A line counts as this story's when `git blame HEAD` attributes it to a commit in `9e813dc7a..HEAD`
other than the unrelated Jira-POST and hosting commits (`cc864bc53 abf57cda7 5776973e7 5c642f03c
80adaaf6a 201f8c939 1ed3c76bf 39e9f3eb6`). Blame rather than the raw `git diff -U0` hunks, because
`cc864bc53` and `abf57cda7` edited `JiraWorkTrackingConnector.cs` inside the same range, and blame
gives each surviving line to the commit that last wrote it. A mutant is on a story line when any line
of its span is one. Scores come from `StrykerOutput/<run>/reports/mutation-report.json`, never the
console: (Killed + Timeout) / (Killed + Timeout + Survived + NoCoverage); Ignored and CompileError
are left out.

The story's lines per file: `DependencySourceSelector.cs` 11–13, 42–54, 61–65; `RefreshLogService.cs`
22, 25–32, 34; `RefreshLogDto.cs` all 80 (new file); `Feature.cs` 39–43, 123–129; `PortfolioUpdater.cs`
169; `IssueExtensions.cs` 87–103, 169, 171–183; `WorkItemService.cs` 985, 987–1055, 1058–1061;
`JiraWorkTrackingConnector.cs` 108–114, 333, 335, 1235–1237, 1241–1242, 1245–1250, 1259, 1263, 1269,
1272–1358, 1369–1372, 1374–1386, 1388–1402, 1404–1409, 1411–1413, 1421–1422, 1428–1432, 1442–1449,
1456–1463, 1472–1477, 1815–1816, 1841–1844, 1846–1847, 1849, 1856–1859, 1977, 1979–1982, 2193–2214,
2254–2257, 2259–2260, 2262–2264, 2266–2268, 2270, 2272–2273.

The score after the kill round keeps every story-line mutant's first-run status, except that a
first-run survivor takes the kill round's status. Mutants are matched on file, span, mutator and
replacement. Between the two runs tests were only added, so a mutant the first run killed stays killed.

The log read `21632 mutants created` (the whole backend, before `mutate` applies) and `1172 total
mutants will be tested`. The report scores 1408: those 1172 plus 236 with no coverage. The kill round
tested 744.

### Per file

| file | whole file, first run | whole file, after kill | story-line mutants | story-line survivors, first run | after kill |
| --- | --- | --- | --- | --- | --- |
| `API/DTO/RefreshLogDto.cs` | 66.67 % (2/3) | 66.67 % | 3 | 1 | 1 |
| `Models/Feature.cs` | 28.41 % (25/88) | 29.55 % | 1 | 1 | 0 |
| `Services/.../Update/PortfolioUpdater.cs` | 68.29 % (28/41) | 68.29 % | 5 | 0 | 0 |
| `Services/.../Dependencies/DependencySourceSelector.cs` | 100 % (6/6) | 100 % | 2 | 0 | 0 |
| `Services/Implementation/RefreshLogService.cs` | 70.00 % (21/30) | 73.33 % | 5 | 1 | 0 |
| `Services/.../WorkItems/WorkItemService.cs` | 63.64 % (217/341) | 65.10 % | 28 | 5 | 0 |
| `Services/.../Jira/IssueExtensions.cs` | 72.46 % (50/69) | 73.91 % | 4 | 1 | 0 |
| `Services/.../Jira/JiraWorkTrackingConnector.cs` | 56.39 % (468/830) | 57.95 % | 56 | 14 | 4 |

The two big files are scored on their story lines only: `JiraWorkTrackingConnector.cs` **92.86 %
(52/56)**, `WorkItemService.cs` **100 % (28/28)**. Their whole-file numbers describe about 4,500 lines
of older code and are not triaged here. Survivors were triaged only on story lines, in every file.

The whole-file figures of the other files are dominated by older code too (`Feature.cs` is mostly
forecasting, `IssueExtensions.cs` mostly rank, labels and blocked-by parsing). Judged instead with the
small story files whole and only the big two and the single-line files (`PortfolioUpdater.cs:169`,
`Feature.cs`) on their story lines, the stack scores 84.34 % (167/198), so the gate holds either way.

The first run's filter took every Jira unit test, `WorkItemServiceTest`, `DependencySourceSelectorTest`,
`RefreshLogServiceTest`, `PortfolioUpdaterTest`, `SystemInfoControllerTest` and `FeatureTest` (590 tests,
green before the run), and left out the live connector categories, `*IntegrationTest`,
`API.Integration` and `Dogfood`. `WorkItemService.cs` stayed in: the whole run took 12 minutes.

### Closed by this pass

Commit `a59211a66`:

- `WorkItemService.cs:1047`: a named link that matched nothing writes only its own warning, not also the
  catch-all line.
- `WorkItemService.cs:1050–1055`: a finding of a kind with no sentence of its own is still warned,
  naming the setting, what was typed and what was seen, quoted.
- `JiraWorkTrackingConnector.cs:1247`, `:1346`, `:1347`: a link type list that is refused or cannot be
  read stops the refresh of a Portfolio whose Dependency Field names no field, and says how Jira
  answered (`503 (ServiceUnavailable)`, `a reply Lighthouse could not read`).
- `:1319`, `:1321`: a Portfolio naming no Dependency Field is still refreshed when another setting
  names a link and the link types are listed. Also killed by the field-naming test below.
- `:1326` (two mutants): a phrase two link types share is named in the log by the first refresh that
  meets it. The existing test refreshed twice and counted one line, which a mutant that warns on the
  second refresh also passes.
- `:1354`: a finding names what was typed into the Dependency Field.
- `:1461`: a named link that matched nothing is not also told the older "none of those links is called
  'is blocked by'" line.
- `:2264`: a Dependency Field naming a field Jira defines is read from that field.
- `IssueExtensions.cs:183`: a link whose end reads no phrase is not named.
- `RefreshLogService.cs:22`: of refreshes recorded at the same instant, the one written first is pruned.
- `Feature.cs:41`: a Feature rebuilt from another, to give it its links, keeps what reading it found.

### Accepted survivors (this story's lines)

- `RefreshLogDto.cs:20`, the `EntityName = string.Empty` default to another string. Equivalent: the
  only way a DTO is built is `From`, which always sets the name.
- `JiraWorkTrackingConnector.cs:1326`, `TryAdd(portfolio.Id, true)` to `false`. Equivalent: the
  dictionary is used as a set and its values are never read.
- `:1354`, the `?? string.Empty` fallback to another string. Not reachable: a Dependency Field finding
  is only made after the definition it names was found, so the left side is never null.
- `:2213`, `label.Length > 0` to `>= 0` in `ReadsExactly`. Equivalent: it is only asked about a type
  that already answered the reference, and answering needs a non-empty label equal to it, so the
  reference is never empty and an empty label never equals it.
- `:2268`, a link type's empty `FieldId` to another string. Equivalent: the field id is read only behind
  `NamesALinkType`, and `ResolvedToNothing` is already false through the link type's name.

### Not mutated

- `Models/RefreshFinding.cs` (a record and two enums), `Models/RefreshLog.cs` and
  `Models/SyncOutcome.cs` (one property each). No logic to mutate.
- `Data/LighthouseAppContext.cs`, the `ConfigureRefreshFindings` value converter. A model-building
  method in a large context file; it is pinned by `RefreshLogFindingsPersistenceTest`, which writes and
  reads the column through the real SQLite migrations.
- `API/SystemInfoController.cs`, one changed call (`RefreshLogDto.ListFor(...)`), pinned by
  `SystemInfoControllerTest`.
- `JiraRestRoutes.cs` and `Program.cs` were changed only by the unrelated Jira-POST and hosting
  commits; the EF migrations are generated.

## Frontend

`refreshFindingSentences.ts` whole (new file), and `RefreshHistorySection.tsx:263-267` and `:281-290`,
the only story lines there that are code (line 20 is the import). Specs in the narrowed vitest config:
`refreshFindingSentences.test.ts`, `RefreshHistorySection.findings.test.tsx`,
`RefreshHistorySection.test.tsx`.

| file | first run | after the kill round |
| --- | --- | --- |
| `refreshFindingSentences.ts` | 67.82 % (59/87) | **96.55 % (84/87)** |
| `RefreshHistorySection.tsx` (story lines) | 60.00 % (3/5) | 60.00 % (3/5) |

The re-run after adding tests was first tried with `--incremental` alone. It reported 78.26 %,
because it kept the first run's verdict on the static mutants (the `SETTING_NAMES` and `MONTHS`
literals at module level) even though new tests now cover them. The recorded score comes from a
`--incremental --force` run, which re-tests every mutant.

### Closed by this pass

In `refreshFindingSentences.test.ts`: every setting named as the screen labels it, and a setting this
browser does not know reads as nothing; a link type offering one or three phrases is not advised as two
ends; a named link nothing carries is worded in the default term, and lists no links when none were
seen; every month's label; a time that cannot be read gives no label; the findings come from the first
row that carries any; a finding this browser cannot word is left out, and if it is the only one there
is no label either.

### Accepted survivors

- `refreshFindingSentences.ts:84`, `fullRunAt === undefined ? null : …` to always `new Date(fullRunAt)`.
  Equivalent: `new Date(undefined)` is an invalid date, which the next line turns into no label.
- `:101`, the `?? []` fallback to a non-empty array. Equivalent: its element is not a finding, so
  `sentenceFor` words it as nothing and the filter drops it.
- `:107`, `latestFullRun?.` to `latestFullRun.`. Equivalent: that line is reached only with at least one
  sentence, which needs a row carrying findings.
- `RefreshHistorySection.tsx:282`, the sentence's `sx={{ mt: 2 }}` emptied. Spacing only.
- `RefreshHistorySection.tsx:286`, `asOfLabel && <Typography …>` to `||`. The label's text is pinned
  present and absent; the mutant only renders it without its caption wrapper, or renders an empty one.
  Presentation only.

`git status` was clean apart from the added spec after every frontend run.

## Story #6312

Azure DevOps dependencies from a named relation type. Run 2026-10-17 against `main` @ `de10289e1`,
production code frozen; only tests were added. Gate is 80 % kill rate on each stack, judged on the lines
this story changed.

| stack | score | scored | killed + timeout | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 53.07 % | 1091 | 579 | 512 | 10 m 32 s |
| Backend, this story's lines, first run | 48.63 % | 183 | 89 | 94 | — |
| Backend, this story's lines, after the kill round (unit tests only) | 90.16 % | 183 | 165 | 18 | 7 m 23 s |
| **Backend, this story's lines, final** (connector run with the two acceptance suites) | **93.99 %** | 183 | 172 | 11 | 29 m 39 s |
| **Frontend (StrykerJS 10.0.0), this story's lines** | **100 %** | 17 | 17 | 0 | 40 s |

Configs: `stryker.6312.backend.json` (the full first run), `stryker.6312.backend.kill.json` (the
narrowed kill round), `stryker.6312.backend.connector.json` (the connector and `RelationTypeListing.cs`
with the acceptance suites), `stryker.6312.frontend.json`, `vitest.stryker.6312.config.ts` (copied into
`Lighthouse.Frontend/` for the run, because its `setupFiles` path is relative to the config).

### How the story's lines were found

A line counts as this story's when it is on the new side of a `git diff -U0 d247d6d62..HEAD` hunk. A
mutant is on a story line when any line of its span is one. Scores come from
`StrykerOutput/<run>/reports/mutation-report.json`: (Killed + Timeout) / (Killed + Timeout + Survived +
NoCoverage). After the kill round, a first-run story-line survivor takes the kill round's status, matched
on span, mutator and replacement; the final row takes the connector run's figures for
`AzureDevOpsWorkTrackingConnector.cs` and `RelationTypeListing.cs` and the kill round's for the rest.

The first run's log read `21846 mutants created` and `870 total mutants will be tested`; the kill round
tested 664, the connector run 380. The first run's filter took every Azure DevOps unit test,
`WorkItemServiceTest`, `PortfolioUpdaterTest`, `TeamUpdaterTest`, `UpdateServiceBaseTest`, `WorkItemBase`,
`WorkItemTest`, `FeatureTest` and the remote-change-stamp model tests (319 tests), and left out the live
connector categories, `*IntegrationTest`, `API.Integration` and `Dogfood`.

### Why the connector run includes two acceptance suites

The named-end and relation-type behaviour of the connector is pinned, before this pass, only by
`Slice03AzureDevOpsRelationTypeAcceptedScenarios` and `Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios`
under `API/Integration/ConfigureOncePerConnection/`. They drive the real connector over a fake Azure
DevOps client, so they make no network call, but they boot the application, which is why the run took
30 minutes for 380 mutants. Without them the first run scored the connector's story lines 44.09 %. The
live categories and `Dogfood` stay out.

### Per file

| file | whole file, first run | story-line mutants | story-line survivors, first run | final |
| --- | --- | --- | --- | --- |
| `.../AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` | 41.07 % (200/487); 54.00 % (263/487) with the acceptance suites | 127 | 71 | 11 |
| `.../AzureDevOps/RelationTypeListing.cs` | 27.27 % (6/22); 100 % with the acceptance suites | 22 | 16 | 0 |
| `.../AzureDevOps/WorkItemExtensions.cs` | 40.00 % (16/40) | 1 | 0 | 0 |
| `.../WorkItems/WorkItemService.cs` | 65.36 % (234/358) | 25 | 5 | 0 |
| `.../Update/UpdateServiceBase.cs` | 59.21 % (45/76) | 1 | 0 | 0 |
| `.../Update/PortfolioUpdater.cs` | 66.67 % (28/42) | 1 | 1 | 0 |
| `.../Update/TeamUpdater.cs` | 66.67 % (20/30) | 6 | 1 | 0 |
| `Models/WorkItemBase.cs` | 83.33 % (30/36) | 0 | 0 | 0 |

`AzureDevOpsWorkTrackingConnector.cs` was mutated whole (1,793 lines, about 470 of them this story's); its
story lines score **91.34 % (116/127)** in the connector run, and its whole-file figure describes older
code that the live `AdoIntegration` suites pin and this run does not. `WorkItemBase.cs`'s two story lines
(a copied property and a `[NotMapped]` auto-property) carry no mutant.

### Closed by this pass

New fixture `AzureDevOpsNamedRelationEndTest` (unit, over a mocked Azure DevOps client):

- Validate: references that are all fields never list the relation types; an end, an end in another
  case, a base name with both ends listed, a type without direction and an end listed without attributes
  are accepted; a base name with only one end listed, an attachment type and an unknown name are refused;
  the refusal lists every reference and the relation types that do exist, or says none were listed; a
  refused or timed-out listing reads as `relation_types_unreadable` with its full sentence, hint and field;
  an unexpected failure is not passed off as a refusal.
- Refresh: a carried named end is read alone, as a tracker link, and the Predecessor link is ignored; no
  finding while one Feature carries it; a named end nobody carries is reported naming only the work-item
  links seen (not attachments, hyperlinks, artifact links, the hierarchy, or a relation with no type, and
  each type once in any case); nothing is said when the Features carry no relations, or when no end is
  named; a base name and a type without direction read nothing and say why; a Parent Override Field naming
  a base name is reported; relations are not read when the parent comes from a field and the Dependency
  Field reads nothing; settings that read a field and name a relation type are each reported, settings
  naming fields are not, and the listing is asked for once; a stale definition no setting names does not
  cause a listing; a Dependency Field that cannot be looked up stops the refresh with its full verdict.

`WorkItemServiceTest`: each worded finding kind writes only its own line, not also the catch-all; a Team's
full refresh reports a finding once, under the Team's name. `TeamUpdaterTest`: a read failure still stops
the Team's refresh.

`UpdateServiceTestBase` now clears `WhatTheRefreshThrew` at the start of each enqueued update. It kept the
previous test's exception, because NUnit reuses one fixture instance, so the existing "attaching a reason
must not swallow the failure" assertion in `PortfolioUpdaterTest` passed on a mutant that swallowed it
(`PortfolioUpdater.cs:155`, `throw;` removed): the test before it in the class had thrown the same type.

### Accepted survivors

- `AzureDevOpsWorkTrackingConnector.cs:745–748`, `:876` (two), `:972`, `:973`, `:975`: log calls removed
  or their templates emptied. Log-only; the refresh's outcome, findings and verdicts are pinned.
- `:1763`, `return true` to `false` for a base name with both ends listed. The definition has already been
  recorded as a relation type and as reading nothing, so the only difference is the extra "nothing will be
  read for it" warning. Log-only.
- `:725`, the `?? string.Empty` fallback to another string. Not reachable: a finding is only made for a
  definition the setting names, so the lookup never misses.

### Not mutated

- `Data/LighthouseAppContext.cs`: only `ReadFindings` and `AFindingThisReleaseKnows` (the findings value
  converter) changed, in an 894-line context file whose model-building mutants would bury them and slow
  every mutant through the DbContext. Pinned by `RefreshLogFindingsPersistenceTest`, which writes and reads
  the column through the real SQLite migrations.
- `Models/Feature.cs` and `Models/RefreshFinding.cs` only lost lines, so there is nothing of this story's
  to mutate. `RefreshLog.ts` on the frontend likewise only lost a line.

## Frontend (Story #6312)

`refreshFindingSentences.ts:7-15`, `:48-56`, `:60-62` and `:76`, the hunks of `git diff -U0
d247d6d62..HEAD`. Specs in the narrowed vitest config: `refreshFindingSentences.test.ts` and
`RefreshHistorySection.findings.test.tsx`. `coverageAnalysis: perTest`, incremental file under
`reports/mutation/` (not committed).

| file | score |
| --- | --- |
| `refreshFindingSentences.ts` (story lines) | **100 % (17/17)** |

No survivors, so no kill round. `git status` showed no change under `Lighthouse.Frontend/src` after the run.

## Story 6304

Teams' Parent Override Field as a connection default. Run 2026-10-18 against `main` @ `37949a071`,
production code frozen; only tests were added. Gate is 80 % kill rate, judged on the lines this story
changed.

| stack | score | scored | killed + timeout | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 61.85 % | 561 | 347 | 214 | 5 m 29 s |
| Backend, this story's lines, first run | 68.78 % | 189 | 130 | 59 | — |
| Backend, whole files, after the kill round | 71.48 % | 561 | 401 | 160 | 4 m 37 s |
| **Backend, this story's lines, after the kill round** | **95.77 %** | 189 | 181 | 8 | — |

| stack | score | scored | killed + timeout | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 10.0.0), this story's lines, first run | 83.17 % | 309 | 257 | 52 | 21 m 34 s |
| **Frontend, this story's lines, after the kill round** (full re-run) | **93.53 %** | 309 | 289 | 20 | 16 m 56 s |

Frontend configs: `stryker.6304.frontend.json` and `vitest.stryker.6304.config.ts` (copied into
`Lighthouse.Frontend/` for the run). See "Frontend (Story 6304)" below.

Configs: `stryker.6304.backend.json` (the full first run) and `stryker.6304.backend.kill.json` (the
narrowed kill round: the eleven files that had survivors on story lines, and only their unit tests).

### How the story's lines were found

A line counts as this story's when it is on the new side of a `git diff -U0 60d0edfae~1..HEAD` hunk. A
mutant is on a story line when any line of its span is one. Scores come from
`StrykerOutput/<run>/reports/mutation-report.json`: (Killed + Timeout) / (Killed + Timeout + Survived +
NoCoverage); Ignored and CompileError are left out. After the kill round, a first-run survivor takes the
kill round's status, matched on file, span, mutator and replacement; between the runs tests were only
added, so a mutant the first run killed stays killed. No mutant timed out in either run.

The first run's log read `22095 mutants created` and `464 total mutants will be tested`; the report
scores 561, those plus 97 with no coverage. The kill round tested 395. The first run's filter took 380
unit tests: the Team, Teams, connection and connections controller tests (with the write-back and licence
gate ones), `ConnectionSaveServiceTest`, `WorkTrackingSystemConnectionUsageTest`, `TeamExtensionsTest`,
the five settings-sync tests, the Portfolio controller tests, and the model tests of every mutated model
file. It left out the live connector categories, `*IntegrationTest`, `API.Integration` and `Dogfood`.

`TeamController.cs:195-253` counts as story lines because the story moved the existing save validation
into `RefusalOf` to add the Parent Override check; the moved checks were pinned only by tests that a
later check also refused, so they were closed rather than set aside.

### Per file

| file | whole file, first run | whole file, after kill | story-line mutants | story-line survivors, first run | after kill |
| --- | --- | --- | --- | --- | --- |
| `API/APIHelpers.cs` | 100 % (6/6) | 100 % | 2 | 0 | 0 |
| `API/DTO/ConnectionDefaultsDto.cs` | 83.33 % (5/6) | 100 % | 6 | 1 | 0 |
| `API/DTO/SettingsOwnerDtoBase.cs` | 50.00 % (1/2) | 50.00 % | 1 | 0 | 0 |
| `API/DTO/WorkTrackingSystemConnectionDto.cs` | 50.00 % (4/8) | 50.00 % | 1 | 0 | 0 |
| `API/Helpers/NewAdditionalFieldTargets.cs` | 62.50 % (10/16) | 75.00 % | 3 | 3 | 1 |
| `API/Helpers/PortfolioExtensions.cs` | 39.39 % (13/33) | 42.42 % | 1 | 1 | 0 |
| `API/Helpers/TeamExtensions.cs` | 63.49 % (40/63) | 65.08 % | 3 | 1 | 0 |
| `API/Helpers/TeamParentOverrideRefusals.cs` | 22.22 % (2/9) | 100 % | 9 | 7 | 0 |
| `API/TeamController.cs` | 63.93 % (39/61) | 75.41 % | 16 | 6 | 0 |
| `API/TeamsController.cs` | 60.47 % (26/43) | 60.47 % | 0 | 0 | 0 |
| `API/WorkTrackingSystemConnectionController.cs` | 52.27 % (46/88) | 68.18 % | 32 | 12 | 0 |
| `API/WorkTrackingSystemConnectionsController.cs` | 71.70 % (38/53) | 77.36 % | 5 | 3 | 0 |
| `Models/AdditionalFieldInUse.cs` | 100 % (23/23) | 100 % | 23 | 0 | 0 |
| `Models/IncomingSetting.cs` | 100 % (3/3) | 100 % | 3 | 0 | 0 |
| `Models/OwnersWhoseFetchMoved.cs` | 100 % (2/2) | 100 % | 2 | 0 | 0 |
| `Models/Team.cs` | 54.55 % (6/11) | 54.55 % | 0 | 0 | 0 |
| `Models/WorkTrackingSystemConnection.cs` | 72.22 % (13/18) | 77.78 % | 6 | 1 | 0 |
| `Models/WorkTrackingSystemOptionsOwner.cs` | 46.15 % (24/52) | 55.77 % | 14 | 6 | 1 |
| `Services/.../Connections/ConnectionSaveService.cs` | 70.18 % (40/57) | 91.23 % | 57 | 17 | 5 |
| `Services/.../WorkTrackingSystemConnectionUsage.cs` | 75.00 % (3/4) | 75.00 % | 2 | 1 | 1 |
| `Services/Interfaces/IConnectionSaveService.cs` | 100 % (3/3) | 100 % | 3 | 0 | 0 |

`Models/EffectiveSetting.cs` (a two-arm switch over the enum) produced no mutant, and
`Models/SupportedConnectionDefaults.cs` only one, which did not compile. The story lines of `Team.cs` and
`TeamsController.cs` carry no scored mutant: Stryker makes none of `Team.cs`'s expression-bodied members,
and the four of `TeamsController.cs:126-135` did not compile. Whole-file figures of the controllers, the
Team and Portfolio extensions and the options owner describe older code that this pass did not triage.

### Closed by this pass

`ConnectionSaveServiceTest`:

- `Commit_RemovingAFieldTeamsOnThisConnectionChoseThemselves_IsRefusedNamingThoseTeamsInTheOrderTheyWereCreated`:
  a Team that chose the field as its Parent Override or its estimation field blocks the removal, a Team
  on another connection or one owning nothing does not, they are named in id order, and nothing is
  stored (`ConnectionSaveService.cs:44-47`, `:81-84`).
- `Commit_RemovingAFieldPortfoliosOnThisConnectionChoseForAnySetting_IsRefusedNamingThosePortfoliosInTheOrderTheyWereCreated`:
  each of the five Portfolio settings on its own blocks the removal (`:90-96`).

`WorkTrackingSystemConnectionControllerTest`:

- `UpdateWorkTrackingSystemConnection_ACopyReadBeforeSomeoneElsesSave_IsRefusedAsStaleAndNothingIsStored`
  (`WorkTrackingSystemConnectionController.cs:104`).
- `UpdateWorkTrackingSystemConnection_RemovingTheFieldTheTeamsDefaultNames_IsRefusedAsInUseAndNothingIsStored`:
  the 409 with its code, title and full sentence (`:138-140`, `:248-249`).
- `UpdateWorkTrackingSystemConnection_TheTeamsDefaultMovedToAnotherStoredField_StoresTheDefault` (`:221-222`).
- `UpdateWorkTrackingSystemConnection_TheTeamsDefaultNamesAFieldAddedInThisSave_StoresTheFieldFirstAndThenPointsTheDefaultAtIt`:
  the first save stores no default and the second names the stored id (`:221`,
  `NewAdditionalFieldTargets.cs:30`).
- The two existing refusal tests now pin the title and the whole detail, the unsupported one with two
  defaults so the separator counts (`:235-236`, `:242`). `GetWorkTrackingSystemConnection_ConnectionExists_ReturnsConnection`
  now reads the Teams' default back (`ConnectionDefaultsDto.cs:15-17`).

`WorkTrackingSystemConnectionsControllerTest`: `CreateNewWorkTrackingSystemConnection_NamingSeveralDefaults_IsRefusedListingEveryOne`
(`WorkTrackingSystemConnectionsController.cs:222-223`).

`TeamControllerTest`:

- `UpdateTeam_OneSettingOfAnOtherwiseValidSaveRefused_IsRefusedAndNothingIsSaved`, one case each for the
  baseline, state mappings, refinement, staleness threshold and cycle time (`TeamController.cs:203-249`).
  The older baseline and state-mapping tests sent no throughput history, so the throughput check refused
  them too and a mutant that dropped their own check still passed.
- `UpdateTeam_ACycleTimeEndingBeforeItStartsWithoutPremium_IsNotJudgedAndTheSaveGoesThrough` (`:243`).
- `UpdateTeam_NamingAParentOverrideSourceThisVersionDoesNotKnow_IsRefusedNamingTheSourcesItKnows` and
  `UpdateTeam_OwningAParentOverrideFieldItsConnectionDoesNotHave_IsRefused`: code, title and detail
  (`TeamParentOverrideRefusals.cs:18-29`, `TeamExtensions.cs:40`).

`WorkTrackingSystemOptionsOwnerTest`: an owner stored with a field of its own before it chose a source owns
it (`WorkTrackingSystemOptionsOwner.cs:98`); a sent field of its own is the field the owner would hold, and
a Team that follows holds none even when its row still carries one (`:128`); taking a sent follow makes an
owner that owned a field follow (`:143`).

`WorkTrackingSystemConnectionTest.HoldsStoredField_AFieldNotStoredYet_IsNotHeldEvenThoughItsIdOfZeroMatches`
(`WorkTrackingSystemConnection.cs:34`). `EstimationFieldSyncTest.SyncWithPortfolioSettings_TakesTheParentOverrideFieldAsThePortfoliosOwn`
(`PortfolioExtensions.cs:33`).

### Accepted survivors (this story's lines)

- `NewAdditionalFieldTargets.cs:30`, `fieldId is < 0` to `<= 0`. Equivalent: only negative placeholder ids
  are ever put in the map, so asking it about 0 finds nothing either way.
- `WorkTrackingSystemOptionsOwner.cs:138`, the early `return` when the sent setting equals the stored one
  removed. Equivalent: the branch below then writes the same source and the same value again.
- `ConnectionSaveService.cs:190` (two), the error logged when the changed default cannot be logged:
  removed, or its template emptied. Log-only, on a path reached only when writing the first log entry
  throws; the save is already stored by then.
- `ConnectionSaveService.cs:199-200`, `:237-240`, `:244-247` and `WorkTrackingSystemConnectionUsage.cs:22`,
  `AsEnumerable()` to `Reverse()`. Equivalent under the unit tests' in-memory queries: each result is
  ordered afterwards, put in a dictionary by id, or only counted per connection. Against a real database
  EF refuses an unordered `Reverse()`, so the acceptance suites under `API/Integration/ConfigureOncePerConnection/`
  would fail on it.

### Not mutated

- `API/DTO/ConnectionUsageDto.cs` (a record), `Models/SettingSource.cs` (an enum),
  `Services/Interfaces/IWorkTrackingSystemConnectionUsage.cs` (an interface and two records) and
  `Models/IWorkItemQueryOwner.cs` (an interface that lost a setter). No logic to mutate.
- `Data/LighthouseAppContext.cs`, a four-line column mapping for the Team's stored source in an
  894-line context file whose model-building mutants would bury it. Pinned by
  `ConnectionDefaultsUpgradeMigrationTests`, which read and write the column through the real migrations.
- `Services/Implementation/WorkItems/FetchFingerprint.cs`: only a doc comment changed.
- The EF migrations are generated.

### Frontend (Story 6304)

Run 2026-10-18 against `main` from `37949a071`, production code frozen; only tests were added (the backend
commits that landed during the runs touched no frontend file).

The mutated lines are the new side of the `git diff -U0 60d0edfae~1..HEAD` hunks in
`ModifyConnectionSettings.tsx`, `FormSelectField.tsx`, `WorkTrackingSystemConnection.ts`,
`AdvancedInputs.tsx`, `EditConnection.tsx`, `BaseApiService.ts` and `WorkTrackingSystemService.ts`, plus
`UpdateQuestion.tsx` whole (a new file). Hunks that are only imports, interfaces or type aliases were
left out, and so was `BaseSettings.ts`, whose story lines are all types. Specs in the narrowed vitest
config: `ModifyConnectionSettings.test.tsx`, `UpdateQuestion.test.tsx`, `CreateConnectionWizard.test.tsx`,
`FormSelectField.test.tsx`, `WorkTrackingSystemConnection.serviceNow.test.ts`,
`AdvancedInputsComponent.test.tsx`, `AdvancedInputs.connectionDefault.test.tsx`, the three
`EditConnection*.test.tsx`, `WorkTrackingSystemService.test.ts` and `DeliveryService.test.ts`.
`coverageAnalysis: perTest`, incremental file under `reports/mutation/` (not committed).

The vitest config carries the main config's `onUnhandledError` filter for `ExpectedTestFailure`. Without
it the dry run dies on the save-failure tests' deliberate rejections ("Something went wrong in the
initial test run"). The first run was started without `--incremental`, so it wrote no incremental file
and the re-run after the kill round was a full run: no verdict was carried over.

| file | first run | after the kill round |
| --- | --- | --- |
| `ModifyConnectionSettings.tsx` (story lines) | 88.10 % (148/168) | **95.24 % (160/168)** |
| `UpdateQuestion.tsx` | 100 % (51/51) | **100 % (51/51)** |
| `FormSelectField.tsx` (story lines) | 46.67 % (7/15) | **80.00 % (12/15)** |
| `WorkTrackingSystemConnection.ts` (story lines) | 0 % (0/1) | **100 % (1/1)** |
| `AdvancedInputs.tsx` (story lines) | 78.95 % (30/38) | **94.74 % (36/38)** |
| `EditConnection.tsx` (story lines) | 67.86 % (19/28) | **75.00 % (21/28)** |
| `BaseApiService.ts` (story lines) | 100 % (1/1) | **100 % (1/1)** |
| `WorkTrackingSystemService.ts` (story lines) | 14.29 % (1/7) | **100 % (7/7)** |

#### Closed by this pass

- `WorkTrackingSystemService.test.ts`: "keeps the connection's defaults and the defaults it offers,
  leaving out one this page does not know" (`WorkTrackingSystemService.ts:20-22`, `:177-185`,
  `WorkTrackingSystemConnection.ts:61`).
- `EditConnection.defaults.test.tsx`: "a connection that offers only Teams defaults shows no Portfolios
  group" (`ModifyConnectionSettings.tsx:277`, `:284`); "a default naming no field / a field the
  connection does not list reads as None" (`:270`); "Try again loads the connection that could not be
  loaded, with its section" (`EditConnection.tsx:181-182`, no coverage before); "a changed
  `parentOverrideForPortfolios` / `dependencyFieldForPortfolios` / `sizeEstimateFieldForPortfolios` /
  `featureOwnerFieldForPortfolios` default 2 Portfolios follow asks whether to update them"
  (`ModifyConnectionSettings.tsx:160-175`, `:232`); "changing a default clears the refusal the last save
  got" (`:782`).
- `AdvancedInputs.connectionDefault.test.tsx`: "names the … connection the default is set on", now one
  case per tracker (`AdvancedInputs.tsx:26-28`); "before the settings arrive the dropdown offers no
  connection default and chooses nothing" (`:36`, `:39`, `:61`).
- `FormSelectField.test.tsx`: "without one, an empty field shows no None and keeps its label inside",
  "with one, a picked None stays readable and the label sits above it" (`FormSelectField.tsx:44`, `:53`);
  "shows the helper text beneath the field", "shows no helper line when there is no helper text" (`:78`).

#### Accepted survivors

- `EditConnection.tsx:42` and `:67`, the `useCallback` dependency lists emptied. Equivalent: the service
  and the query's `refetch` they close over do not change while the page is open.
- `EditConnection.tsx:50`, the query key's name emptied. Equivalent: the key still carries the
  connection's id, and no other query on the page shares it.
- `EditConnection.tsx:57`, `refetchOnWindowFocus` to `true`. The re-read fetches the connection the form
  already holds, and the form keeps its own state, so nothing on the page changes.
- `EditConnection.tsx:63`, `if (error)` to `false`, and its `throw` emptied (no coverage). Reached only
  when the form's own re-read of the connection fails after the page has loaded it. With the throw,
  `ModifyConnectionSettings` logs the error and leaves the form blank; with the mutant it shows the
  connection from the first read. Not pinned, because a test would pin the blank form, which is a latent
  issue rather than wanted behaviour.
- `EditConnection.tsx:174`, the error look's `maxWidth={false}` to `true`. Layout only.
- `ModifyConnectionSettings.tsx:155` and `:180`, `"pageLoads"` emptied. Equivalent: only `"updating"` is
  compared.
- `ModifyConnectionSettings.tsx:230`, `because: "defaults"` emptied. Equivalent: `UpdateQuestion` only
  tests for `"fields"`.
- `ModifyConnectionSettings.tsx:270` and `:799`, `fieldId != null` to `true`. Equivalent: a set of field
  ids never has `null` or `undefined`, so the other half of the condition gives the same answer.
- `ModifyConnectionSettings.tsx:775`, `selectedWorkTrackingSystem?.` to `.`. Unreachable: edit mode and the
  selected connection are set in the same update, so edit mode never renders without one.
- `ModifyConnectionSettings.tsx:296` (the fieldset's `sx`) and `:983` (the section's `Grid` size) emptied.
  Layout only.
- `FormSelectField.tsx:61` (three), `notched` forced on, off or to `undefined`. It draws only the gap the
  outline leaves for a raised label; the label's own raising is pinned. Presentation only.
- `AdvancedInputs.tsx:38`, `teamSettings?.` to `.`. Unreachable: the line is reached only after a default
  name was read, which needs settings.
- `AdvancedInputs.tsx:42`, the empty tracker name to any other text. Reached only by a Team that follows a
  default but carries no tracker, whose sentence would read "Set on the  connection."; not pinned, since
  that sentence is not one worth keeping.

`git status` showed no change under `Lighthouse.Frontend/src` other than the added specs after both runs.
