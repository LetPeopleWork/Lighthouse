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
