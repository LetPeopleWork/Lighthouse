# Mutation testing — 6354 (Updates that change nothing skip the forecast)

Run 2026-10-23 against `main` @ `ead31f93f` (the Story's last commit is `683716b43`). Gate is 80 % kill rate
on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), Story lines | **82.61 %** | 69 | 57 | 12 | 0 | 18 m 10 s |
| Backend (Stryker.NET 5.0.0), whole files | 71.37 % | 524 | 367 | 150 | 7 | 18 m 10 s |
| Frontend (StrykerJS 10.0.0) | **96.77 %** | 62 | 60 | 2 | 0 | 4 m 32 s |

Configs: `stryker.6354.backend.json`, `stryker.6354.backend.kill.json` (the narrowed kill round),
`stryker.6354.frontend.json`, `vitest.stryker.mutation.ts`. Backend runs from `Lighthouse.Backend.Tests/`;
frontend from `Lighthouse.Frontend/`, with `vitest.stryker.mutation.ts` copied there and `TZ=Europe/Zurich`.

The first backend run scored 70.99 % whole-file (372 of 524) and 81.16 % on Story lines (56 of 69). The kill
round below added two kills, and its narrowed re-run (2 files, 36 mutants, 5 m 36 s) confirmed both. The first
frontend run scored 95.16 % (59 of 62); the incremental re-run after the kill round took 2 m 28 s.

## Backend

`mutate` names every production file the Story changed, whole. Stryker.NET has no reliable line scoping, so
the files the Story touched in a few lines (the five controllers, `UpdateServiceBase`, `RefreshLogDto`) were
mutated whole as well, rather than left out: their unit tests are fast, and mutating them is the only direct
evidence that the new forget-before-queue calls are pinned. The cost is that their untouched code sets the
whole-file number. The gate is therefore read off the **Story lines**: mutants on lines that `git blame` at
`ead31f93f` attributes to the Story's 13 non-merge commits (`3c00a9998..683716b43`). Lines that a later commit
rewrote count against that later commit, not the Story.

`test-case-filter` covers the unit tests of every mutated file (forecast service and updater, the update
services, both repositories plus the relational SQLite suite, the round summary, the seeder, the five
controller suites, the DTO), and excludes `API.Integration`, `Integration.Containers`, Dogfood and the live
connector categories: 729 tests, 3 s as a plain run.

Per file, after the kill round:

| file | tested | survived | Story lines tested | Story lines survived |
| --- | --- | --- | --- | --- |
| ForecastService.cs | 89 | 25 | 12 | 3 |
| UpdateServiceBase.cs | 75 | 21 | 0 | 0 |
| TeamController.cs | 62 | 15 | 1 | 0 |
| ForecastController.cs | 53 | 12 | 2 | 0 |
| ForecastUpdater.cs | 52 | 14 | 19 | 8 |
| TeamsController.cs | 44 | 16 | 1 | 0 |
| PortfolioController.cs | 36 | 18 | 1 | 0 |
| PortfolioRepository.cs | 27 | 17 | 5 | 1 |
| PortfoliosController.cs | 23 | 6 | 1 | 0 |
| WriteBackRound.cs | 22 | 1 | 7 | 0 |
| ForecastInputFingerprint.cs | 15 | 0 | 15 | 0 |
| FeatureRepository.cs | 14 | 2 | 2 | 0 |
| RefreshRoundSummaryLine.cs | 7 | 0 | 2 | 0 |
| RefreshLogDto.cs | 3 | 3 | 0 | 0 |
| RefreshRoundSummary.cs | 1 | 0 | 0 | 0 |
| ForecastInputsForgottenAtStartSeeder.cs | 1 | 0 | 1 | 0 |

`ForecastRunOutcome.cs` is two record declarations and produced no mutants. `ForecastInputFingerprint.cs`
yields only 17 mutants for 129 lines: most of its rendering is `const` separators passed to `string.Join`,
and Stryker.NET has no operator for those. All 15 tested mutants are killed. `RefreshRoundSummaryLine.cs`
produced 21 compile errors: every interpolated-string mutant inside `string.Create` fails to build.
The Story's line in `UpdateServiceBase.cs` (`ForecastSkipped = skipped` in an object initialiser) produced no
mutant of its own; the initialiser mutant on that object is killed.

### Closed by this pass

- **`WriteBackRound.cs:79`** — `forecastSummary?.ForecastSkipped ?? false` survived `?? true`. A round that
  only refreshed would then report a skipped forecast it never ran. The line it prints hides that, because a
  round with no forecast time prints no forecast at all, but the merged summary is what the round hands over.
  `A_round_that_only_refreshed_reports_the_refresh_with_no_forecast_in_it` now asserts the flag is false.
- **`PortfolioRepository.cs:47`** — `GetPortfolioIdsForTeam` survived `Any` → `All`, because every existing
  case had a Feature worked on by one Team only. The line predates the Story, but the Story's refresh buttons
  (Team, all Teams, forecast-for-team) now forget the recorded inputs of exactly the Portfolios it returns, so
  a Team sharing a Feature would have left that Portfolio's skip in place.
  `GetPortfolioIdsForTeam_TeamSharesAFeatureWithAnotherTeam_ReturnsPortfolio` pins it.

### Accepted survivors on Story lines (12)

| line | mutant | why it is accepted |
| --- | --- | --- |
| ForecastUpdater.cs:216 (×4) | `WasSkipped(outcome) ? "skipped" : "computed"` forced either way, or either literal emptied | the name feeds only the debug log on the next line |
| ForecastUpdater.cs:217 (×2) | debug log removed, or its template emptied | log only |
| ForecastUpdater.cs:285 (×2) | the warning that forgetting failed removed, or its template emptied | log only; the behaviour around it (a failed forget never replaces the forecast's own failure) is pinned |
| ForecastService.cs:98 (×2) | the "inputs unchanged" info log removed, or its template emptied | log only; the skip itself is pinned |
| ForecastService.cs:133 | the "could not fingerprint" warning template emptied | log string; the fall-back to a full forecast is pinned |
| PortfolioRepository.cs:38 | `Guid.NewGuid().ToString("N")` → `ToString("")` | equivalent: the token is still prefixed and unique, and nothing reads its format |

### Accepted survivors on lines the Story did not change (138)

Triaged by file. None is on a line the Story wrote, so none was chased in this pass; the one real gap among
them is named below for whoever owns that code.

- **Controllers (67).** Settings validation messages and ranges, `ConfigureAwait(false)` and
  `RequestAborted ?? default` (equivalent under test), RBAC creator grants, CSV-licence checks, settings sync
  calls, and paths such as delete and the edit-concurrency token that no unit test in the filter reaches
  (19 `NoCoverage`).
- **`UpdateServiceBase.cs` (21).** Information, warning and error log calls (14), the start delay (2), and five
  branches of the round-summary hand-over that no unit test in the filter reaches.
- **`ForecastService.cs` (22 off Story lines).** Debug and information logs (16), the "abandoned runs" warning
  text (2), two `OrderBy` → `OrderByDescending` that only change the insertion order of a day-keyed dictionary,
  the early `return []` for a plan with no rows (the run that follows yields nothing either), and
  **one real gap: `:386`**. Removing the block that copies a Team's throughput-filter status onto its When
  forecast survives, so no unit test in the filter checks `FilterApplied`, `ExcludedSummary` or
  `HasSufficientData` on a forecast it produces. It predates this Story and belongs to the forecast-chip work.
- **`PortfolioRepository.cs` (16 off Story lines).** `Remove`, `RemoveOrphanedFeatures` and `GetAll`
  (13 `NoCoverage`; no unit test in the filter calls them), the `GetById` debug log (2), and `SingleOrDefault` →
  `Single` (1), which is equivalent wherever the id exists.
- **`ForecastUpdater.cs` (6 off Story lines).** Two `NotSupportedException` texts for the periodic refresh this
  updater never runs, a `stopwatch.Stop()` whose elapsed time is read straight after it, and three mutants in
  the "a forecast for this Portfolio is already owed" check from the responsive-during-updates work.
- **Rest (6).** `FeatureRepository.cs`: `SingleOrDefault` → `Single` and one uncovered block;
  `RefreshLogDto.cs`: a property default, the findings filter and the latest-findings pick, all pre-existing;
  `WriteBackRound.cs:116`: clearing the connection lookup, whose entries `Stage` rewrites before they are
  read again.

### Not mutated

- `Models/Portfolio.cs`, `Models/RefreshLog.cs`: the Story added one auto-property each, which yields no
  mutants.
- `Services/Interfaces/Forecast/IForecastService.cs`: an interface signature.
- `Data/LighthouseAppContext.cs`: one `SetAfterSaveBehavior(Ignore)` call in a 929-line model configuration.
  Stryker has no operator for it, and `ForecastConfirmationRepositoryRelationalTest` pins what it does: a
  Portfolio saved whole from an old copy never writes back a replaced fingerprint.
- `Program.cs`: one DI registration. `ForecastInputsForgottenAtStartSeederTests` and the acceptance suite cover
  the seeder it registers.
- The migrations and the model snapshot.

## Frontend

Scoped to the lines the Story changed: `RefreshHistoryChart.tsx:29` (the gap at a skipped forecast) and
`RefreshHistorySection.tsx:36-63` (the computed-only duration helpers), `:121-123`, `:137`, `:154`, `:160`,
`:219-221`, `:223-227`, `:229-234`, `:272-277` and `:299-303`. The vitest config runs the three Refresh
History specs (32 tests); `@mui/x-charts` is mocked in them, so the chart component itself renders.

| file | tested | survived |
| --- | --- | --- |
| RefreshHistorySection.tsx | 61 | 2 |
| RefreshHistoryChart.tsx | 1 | 0 |

### Closed by this pass

- **`RefreshHistorySection.tsx:231`** — the empty suffix of a computed last run survived becoming any text,
  because the only check was that the word "skipped" was absent. "says nothing about skipped runs when every
  forecast computed" now pins the Last Run figure to the bare time.

### Accepted survivors

- **`:229`** `lastRun?.skipped` → `lastRun.skipped`: equivalent. `lastRun` is null only when the selected
  entity has no runs, and the entity list is built from the loaded runs, so a selected entity always has one.
- **`:300`** `sx={{ mt: 2 }}` → `sx={{}}`: margin on the skipped-count line.

## After the run: the review fix round

The review fix round (`dadfe5a07..0e36fbcf9`, merged as `4de2a9c62`) changed `ForecastService.cs`,
`ForecastUpdater.cs`, `PortfolioRepository.cs`, `FeatureRepository.cs` and `TeamsController.cs` after this run,
and on the frontend one line of `RefreshHistorySection.tsx` (the All view's average is null, not 0, for an entity
whose every run was skipped). The figures above describe those files as they stood at `ead31f93f`. A narrow re-run
on them was skipped at the maintainer's request on 2026-10-23. The fixes are covered by their own new tests: the run
mark and its two-phase compare-and-set (`ForecastServiceTest`, `ForecastUpdaterTest`,
`ForecastConfirmationRepositoryRelationalTest` and the new `ForecastInputsCompareAndSetOnPostgresTests`), the
confirmation that never moves a forecast's time back (`ForecastConfirmationRepositoryRelationalTest`), the one-query
Portfolio read on refresh-all (`TeamsControllerTest`, `PortfolioRepositoryTest`), and the missing bar
(`RefreshHistorySection.skippedForecasts.test.tsx`).
