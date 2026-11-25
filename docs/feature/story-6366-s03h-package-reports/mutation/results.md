# Mutation testing — 6366 (S03h Package Reports)

Run 2026-11-25 in a detached worktree at `ce6d79266` (Reports production code frozen since `7da60809d`; feature C is
test-only), Stryker.NET 5.1.0 on Linux x64, in Stryker's own sandbox (never in place). Bar (D-11): at least 80 % of the
moved code, **and** no survivor or no-coverage mutant added by the move. The stricter of the two holds.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, `Modules/Reports/**` | **90.58 %** | 223 | 202 | 10 | 11 | 7 m |
| Frontend | N/A — no frontend file changed | | | | | |

"Tested" counts killed + survived + no coverage over the 31 files under `Domain` and `Application` (the 11 `I*.cs`
interface files, `WipReportMetric.cs`, `WorkItemAgeReportMetric.cs` and `ReportSeriesReading.cs` yield no testable
mutant). Stryker also marked 112 mutants as compile errors (Safe Mode) and ignored 44 as covered by a block mutant; the
rest of the 24,160 mutants created sit outside `Modules/Reports` and were skipped by the `mutate` globs (212 tested in
all, confirmed in `mutation-report.json`).

Per file (killed / survived / no coverage): `ReportCommands` 40/6/1, `ReportQueries` 23/3/2, `ThenAndNowTemplate` 28/0/5,
`ReportOwnerLookup` 6/0/2, `ReportWindowPolicy` 19/1/0, `Metrics/ReportMetricCatalog` 0/0/1, every other file 100 %.

Config: `stryker.6366.backend.json` (the two `mutate` globs `Modules/Reports/{Domain,Application}`; Metrics sits inside
`Application`). Run from `Lighthouse.Backend.Tests`, with coverage analysis `perTestInIsolation`, concurrency 6. Test
filter: the Reports unit tests (`Tests.Modules.Reports`), the Reports scenarios at the port
(`ReportWindowBoundaryScenarios`, `ArrivalsInReportsScenarios`, the `BaselineReportCreation` scenarios), `ReportsControllerTest`,
`TeamReportsControllerTest`, `ReportOwnerWriteAccessTest` and `ReportRepositoryTest`. Left out, because they start a web
host through `ReportsAcceptanceTest`: `Slice01`..`Slice10` scenarios, `ReportsWithoutSignInScenarios`,
`ReportCreationTimingProbe` and everything under `BulkReports`; no dogfood, no write-back, no live connector category
(251 tests, 5 s on their own). There is no portfolio-reports controller test.

## Survivors

The third column is a reading of the code, not checked by a test. Several are paths that only the excluded host
scenarios observe (saving, concurrency token), so they are likely killed there.

| File:line | Mutation | Likely reason it survives |
| --- | --- | --- |
| `Application/ReportCommands.cs:82` | `writer.ExpectUnchangedSince(...)` removed | The concurrency token is checked by the host scenarios (`Slice10EditReportScenarios`), which the filter leaves out. |
| `Application/ReportCommands.cs:86`, `:99` | `await writer.Save()` removed | Unit tests use a fake writer that is not asked whether `Save` ran; the host scenarios read the stored row back. |
| `Application/ReportCommands.cs:127` (no coverage) | `NameRefusal` early-return block removed | The missing-name branch is reached only through the host scenarios. |
| `Application/ReportCommands.cs:131` | `> Report.LongestName` → `>=` | Boundary: no unit test sends a name of exactly the longest length. |
| `Application/ReportCommands.cs:138` (×2) | `request.ThenAndNow?.NowLengthDays ?? thenLengthDays` → left-to-right / left removed | The tests that set a Now length also set an equal Then length, so the fallback value is indistinguishable. |
| `Application/ReportQueries.cs:44` (×2) | `!MayCreate(...)` un-negated; `TemplateKey == template.Key` → `!=` | Per-template cap counting is asserted at the host in the cap scenarios (`Slice08`), not in the unit test. |
| `Application/ReportQueries.cs:117`, `:127` (no coverage) | `LogUnreadable(...)` calls removed | No unit test feeds an unreadable stored report through the list path; `ReportLoggingTest` covers another entry. |
| `Application/ReportQueries.cs:165` | log message string → `""` | Log wording; no test asserts the message text. |
| `Application/ReportOwnerLookup.cs:17`, `:21` (no coverage) | `team => true` / `portfolio => true` → `false` | The lookup's predicate is only evaluated by an in-memory repository fake that ignores it. |
| `Application/ThenAndNowTemplate.cs:79, 81, 94, 96, 159` (no coverage) | exception message strings → `$""` | Message wording of `JsonException` on a payload that cannot be read; no test asserts the text. |
| `Application/Metrics/ReportMetricCatalog.cs:13` (no coverage) | `metric.Key == key` → `!=` | `Find` is called only from the host scenarios. |
| `Domain/ReportWindowPolicy.cs:24` | `doneItemsCutoffDays < today.DayNumber` → `<=` | Equivalent: when the two are equal, `today.AddDays(-n)` is `DateOnly.MinValue`, the same value as the other branch. |

No kill round: the score is above the bar, as in 6365, and the survivors are either equivalent, log or message wording,
or paths the excluded host scenarios observe. They are recorded here rather than killed. A few (`ReportCommands.cs:131`
boundary, `:138` fallback, `ReportQueries.cs:44` cap count) are real unit-level gaps worth a test if the maintainer
wants a kill round.

## Against the D-11 bar

**Kill rate: met at 90.58 %.**

**No survivor or no-coverage mutant added by the move: met, shown mechanically.** For each of the 31 production `.cs` files
that moved into `Modules/Reports` (git renames between `75807027a` and `997cedf12`, all 31 under `Modules/Reports`), the
content without `using` and `namespace` lines, blank lines and carriage returns is **identical to the pre-move file in 31
of 31** (compared at `HEAD`, so it includes the using edits of `7da60809d`). The move changed only namespaces and usings,
so no mutant can be new, and each survivor above is one the pre-move file had. A mutant-by-mutant comparison against a
run on the base was therefore not made.

Command used for the proof, per pair: `git show 75807027a:<old> | tr -d '\r' | grep -vE '^\s*(using |namespace |$)' | md5sum`
against the same filter on `git show HEAD:<new>`.
