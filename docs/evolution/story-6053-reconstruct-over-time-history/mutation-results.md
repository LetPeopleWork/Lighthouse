# Mutation testing — 6053 (Reconstruct over-time history)

Run 2026-09-24 against branch `story-6053-reconstruct-over-time-history` @ `751302c0d` (production
source frozen; this pass adds tests only). Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), final run | **89.82 %** | 383 | 343 | 36 | 1 | 3 | 5 m 35 s |
| Backend, first run (before this pass's tests) | 59.27 % | 383 | 227 | 85 | 0 | 71 | 4 m 41 s |
| Frontend (StrykerJS 9.6.1) | **100 %** | 1 | 1 | 0 | 0 | 0 | 18 s |

Configs: `stryker.6053.backend.json`, `stryker.6053.frontend.json`, `vitest.stryker.mutation.ts`.
The frontend run needs `vitest.stryker.mutation.ts` copied into `Lighthouse.Frontend/` first (it
resolves `vitest/config` from its own directory); the copy there is gitignored.

Both runs report "total mutants will be tested" in line with the file set (312 before the new tests,
380 after — the difference is mutants that had no covering test at all in the first run). The scores
below were grouped per file from `reports/mutation-report.json`, not read off the headline.

## Pending-work check

`[Ignore(` count across the story's acceptance fixtures in
`Lighthouse.Backend.Tests/API/Integration/PercentilesOverTime/`: **zero** in every file.

| file | `[Ignore(` |
| --- | --- |
| Slice05ReconstructCycleTimeHistoryScenarios.cs / Specifications.cs | 0 / 0 |
| Slice06EveryPercentileTabSpansTheSamePeriodScenarios.cs / Specifications.cs | 0 / 0 |
| Slice07WhereTheLimitsActuallyMovedScenarios.cs / Specifications.cs | 0 / 0 |
| Slice08NothingToShowScenarios.cs / Specifications.cs | 0 / 0 |
| Slice09TheFillShipsOptInScenarios.cs / Specifications.cs | 0 / 0 |
| Slice10OnlyWhatTheTeamStillKeepsScenarios.cs / Specifications.cs | 0 / 0 |

The one skipped test in the whole backend suite is
`TaskManager/Story6055ActivityNamesTheWorkScenarios.cs:126` (`[Ignore(ProductionData)]`), which
belongs to Story 6055, not this one.

## Backend

`test-case-filter` covers the unit classes only and excludes `API.Integration` and
`Integration.Containers`. After this pass every core file sits at or above 87 % on unit tests alone,
so the optional second run adding the `PercentilesOverTime` acceptance scenarios was not needed and
was not made. (For scale: that acceptance class is 151 scenarios and 2 minutes as a plain
`dotnet test`, before any per-mutant multiplication.)

| file | tested | killed | survived | timeout | no cov | score |
| --- | --- | --- | --- | --- | --- | --- |
| BackgroundServices/OverTimeHistoryFiller.cs | 94 | 82 | 12 | 0 | 0 | 87.23 % |
| BackgroundServices/OverTimeFillTarget.cs | 16 | 16 | 0 | 0 | 0 | 100 % |
| BackgroundServices/RetentionEdge.cs | 22 | 21 | 1 | 0 | 0 | 95.45 % |
| BackgroundServices/ReconstructionMemo.cs | 20 | 20 | 0 | 0 | 0 | 100 % |
| OverTimeGapReconciler.cs | 17 | 16 | 1 | 0 | 0 | 94.12 % |
| OverTimeHistoryFillSwitch.cs | 3 | 3 | 0 | 0 | 0 | 100 % |
| PercentileSnapshotWriter.cs | 38 | 37 | 0 | 0 | 1 | 97.37 % |
| ProcessBehaviorSnapshotWriter.cs | 34 | 34 | 0 | 0 | 0 | 100 % |
| LostRaceTolerantSave.cs | 13 | 12 | 0 | 1 | 0 | 100 % |
| PercentileFamilies.cs | 2 | 2 | 0 | 0 | 0 | 100 % |
| GapAskingPercentilesOverTimeSeriesQuery.cs | 1 | 1 | 0 | 0 | 0 | 100 % |
| GapAskingProcessBehaviorSeriesQuery.cs | 1 | 1 | 0 | 0 | 0 | 100 % |
| DatabaseManagement/DatabaseMaintenanceGate.cs | 32 | 31 | 1 | 0 | 0 | 96.88 % |
| DomainEvents/PercentilesOverTimeRecordingHandler.cs | 15 | 15 | 0 | 0 | 0 | 100 % |
| DomainEvents/ProcessBehaviorRecordingHandler.cs | 15 | 15 | 0 | 0 | 0 | 100 % |
| API/OptionalFeaturesController.cs | 14 | 10 | 2 | 0 | 2 | 71.43 % |
| Seeding/OptionalFeatureSeeder.cs | 46 | 27 | 19 | 0 | 0 | 58.70 % |

The source files carry their own narrow `// Stryker disable` comments with reasons (mostly log
wording); those mutants report as Ignored and are not in the table. `Program.cs` shows only Ignored
mutants and is not in scope.

### Closed by this pass

The first run's survivors and no-coverage mutants were almost all behaviour the acceptance
scenarios exercise but no unit test did. Each new test pins one behaviour:

- `RetentionEdgeTest` (new) — an owner that keeps finished work for ever has no edge; older finished
  work moves the first fillable day up to the edge, newer work leaves it; nothing finished stays
  null; a percentile window reaching past the edge comes back empty without being read, one starting
  on the edge is read; limits whose window, or whose pinned stretch, reaches past the edge come back
  NotReady, and limits inside it are read as of the day asked.
- `ReconstructionMemoTest` (new) — days before the first finished item are ruled out and that day
  is not; nothing finished rules out every day; a worked-out day is ruled out and its neighbour is
  not; team and portfolio with the same id are kept apart; a team or portfolio refresh forgets only
  that owner; both bounds (256 owners, 512 days per owner) drop exactly at the bound, and dropping
  the days keeps the owner's first finished day.
- `LostRaceTolerantSaveTest` (new) — a refusal of rows that are all now stored is absorbed, the rows
  are detached and the save is made again; a row not stored, a partly-stored refusal, a refused row
  being modified rather than added, and a refusal naming no row are all thrown on, and a refused row
  leaves the staging area either way.
- `PercentileSnapshotWriterTest` (new) — a missing day is read at 30/60/90 over windows ending on the
  day; a horizon already stored keeps its observed value while the others fill; a day where nothing
  finished gets no row; any single non-zero percentile is worth a row; a refused row is absorbed only
  when that exact horizon is stored.
- `ProcessBehaviorSnapshotWriterTest` (new) — a missing day is read over the family's span ending on
  the day, as of that day; stored limits are kept; a refused row is absorbed only when that family's
  day is stored.
- `OverTimeHistoryFillSwitchTest` (new) — on only when the fill feature itself is on; no stored row,
  or another feature being on, leaves it off.
- `GapAskingSeriesQueryTest` (new) — both decorators serve the stored series unchanged and hand the
  reconciler the requested period and the days the series holds.
- `OverTimeGapReconcilerTest` — a period with no start, a period already held in full, and the fill
  switched off ask for nothing; held days and days no pass can write are left out; a centuries-wide
  period hands over exactly ten years of days.
- `OverTimeFillTargetTest` — a deleted team or portfolio has nothing to fill; a team's history begins
  at its own earliest finished item (not another team's, not an open one); a team that finished
  nothing has none; a portfolio's begins at its earliest delivery, counting one it shares with
  another portfolio and ignoring one only another portfolio holds.
- `OverTimeHistoryFillerTest` — two asks before a pass run one pass; an owner is taken again once its
  pass is over; a switched-off fill loads no owner; a failed pass is logged with its cause and the
  next owner is still taken; a running backup stops a pass before it loads the owner; a deleted owner
  is let go with nothing at Warning or above; after a pass the days before the first finished item
  are known to be unwritable and a fully written day is not asked for again; the first and last
  supported days are worked out and the days either side are not; limits are staged too; a day whose
  limits failed is reported and left to be tried again; a day where both halves failed reports the
  percentile failure once; one failed day gives no tally and two do; a pass out of time still works
  out its first day and gives the rest back; a pass works out at most 90 days; stopping the filler
  drains the queue and stops its reader, and a started filler takes an ask on its own.
- `PercentilesOverTimeRecordingHandlerTests` — a refresh for a deleted team or portfolio reads no
  metrics and leaves the cache alone; a failing save is logged under the Percentiles family.
- `DatabaseMaintenanceGateTest` — a restore refused behind another restore is not reported as pending
  behind a backup (pre-existing logic, cheap to pin).

### Accepted survivors

`OverTimeHistoryFiller.cs` (12):

- L66 `FullMode = Wait` removed — equivalent: `Wait` is `BoundedChannelOptions`' default.
- L153, L154, L170, L287, L288, L322, L323, L428 — log statements and log wording. The behaviour
  around each (drop while switched off, catch-and-continue, give the window back, stand down, report
  a failed day) is asserted by the tests above; only the sentence is not.
- L205, L271 — removing the `ReportStandingDownForMaintenance` call removes a log line and nothing
  else; the stand-down itself (`return` / `break`) is killed.
- L285 `>=` to `>` on the elapsed-time budget — equivalent in practice: the stopwatch reading
  exactly equal to the budget is not a state a test can reach, and at any real budget the two
  differ by one tick.

`RetentionEdge.cs` L60 `>` to `>=` — equivalent: when the two days are equal both branches return
the same day.

`OverTimeGapReconciler.cs` L44 `<` to `<=` — equivalent: when the picked end is today both branches
return today.

`DatabaseMaintenanceGate.cs` L150 `&&` to `||` — equivalent: the active operation's id and type are
only ever set together and cleared together, so one is null exactly when the other is.

`PercentileSnapshotWriter.cs` L114 (no coverage) — message text of an `ArgumentOutOfRangeException`
for a metric type with no horizon list. Every percentile family is CycleTime or WorkItemAge, so the
branch is an unreachable defensive guard.

`LostRaceTolerantSave.cs` — the one Timeout counts as detected: the mutant turns the retry loop into
one that never ends.

`OptionalFeaturesController.cs` (2 survived, 2 no coverage) — the story's only change to this file
is one Information log line (L66), and both its mutants are log wording / removal. The two
no-coverage mutants (L30, L47) are the pre-existing get-by-key lookup, untouched by this story.

`OptionalFeatureSeeder.cs` (19 survived) — every survivor sits on pre-existing lines (logging at
L15/L25/L45/L127/L137, the removal guard at L42, and the usage-data seed rows at L68 and L81-L89).
The story's own addition, the fill feature's seed row at L91-L104, has 8 mutants and all 8 are
killed (the object-initializer mutant is a compile error).

### Not mutated

- `BaseMetricsService.cs` (1385 lines, 28 changed), `TeamMetricsService.cs` (964, 36 changed),
  `PortfolioMetricsService.cs` (805, 34 changed) — the change threads an optional `asOf` day into
  the process-behaviour chart builders and their cache keys. Mutating the whole files would score
  the pre-existing metrics suite, not this change. The change is covered by
  `TeamMetricsServiceTests.GetThroughputProcessBehaviourChart_SameWindowAskedAboutTwoDifferentDays_DoesNotHandBackOneAnswerForTheOther`
  (cache key), by `ProcessBehaviorSnapshotWriterTest` (the as-of day reaches the reader), and by the
  slice 07 acceptance scenarios, whose fixture is built so that a day judged as of today and the
  same day judged as of itself give different limits.
- `OptionalFeatureKeys.cs` — the change is one `const string`, which Stryker.NET leaves alone (a
  mutated initialiser would no longer be a compile-time constant). `OptionalFeatureSeederTests` pins
  the key against the literal `"OverTimeHistoryFill"`.
- `Program.cs` (DI registration) and the Migrations — excluded by convention; wiring is exercised by
  every acceptance scenario.
- Interfaces (`IOverTimeHistoryFiller.cs`, `IOverTimeGapReconciler.cs`, `IOverTimeHistoryFillSwitch.cs`,
  `IPercentileSnapshotWriter.cs`, `IProcessBehaviorSnapshotWriter.cs`, and the `asOf` parameter on
  `ITeamMetricsService.cs` / `IPortfolioMetricsService.cs`) — declarations only, nothing to mutate.

## Frontend

`mutate` is scoped to the lines this story changed in the current files:
`overTimeEmptyState.ts:17-18`, `PbcOverTimeWidget.tsx:256-256`,
`PercentilesOverTimeWidget.tsx:208-208`.

| file | tested | killed | survived | score |
| --- | --- | --- | --- | --- |
| overTimeEmptyState.ts | 1 | 1 | 0 | 100 % |

The frontend change is almost entirely non-mutable: the two-sentence empty copy collapsed to one
exported string constant, and both widgets now import and render that constant. Import and re-export
lines and a bare `{OVER_TIME_EMPTY_COPY}` JSX expression produce no mutants, so the two widget files
are absent from the table — that is expected, not a scoping error (the log reports 3 files
instrumented, 1 mutant). The single mutant, blanking the constant, is killed by the specs that pin
the sentence against its literal. `git status` was clean after the run.
