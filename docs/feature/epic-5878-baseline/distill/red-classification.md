# RED classification — epic-5878-baseline DISTILL (2026-10-13)

Every pending case was un-skipped once, run, classified and skipped again (`[Ignore(PendingSliceNN)]` /
`Ignore = PendingSliceNN` removed; `it.skip` → `it`). Copies of the files were restored afterwards, and the suites
re-run skipped (backend 127 skipped, frontend 93 skipped, both green).

**Result: 220 of 220 runnable cases fail, every one for missing behaviour. None broken, none vacuously green.**
The E2E walking skeleton is type-checked and collected by Playwright (`--list`), not run live: it is `test.fixme`,
no instance was running, and its first step (the Reports tab) does not exist yet.

## Backend — 127 cases

Run: `dotnet test Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --no-build --filter
"FullyQualifiedName~API.Integration.Reports|FullyQualifiedName~ReportUsageEventsTests"` → Failed 127, Passed 0.

| Class | Cases | Failure | Classification |
|---|---|---|---|
| `Slice01TeamReportScenarios` | 19 | `…/teams/1/reports` and `…/reports/templates` answer 404; "The report was not created: expected Created but was NotFound" | MISSING_FUNCTIONALITY (no endpoint) |
| `Slice02ThenInThePastScenarios` | 18 | same; refusals expected 400, got 404 | MISSING_FUNCTIONALITY |
| ~~`Slice03CycleTimeBeyondThenLimitsScenarios`~~ | ~~7~~ | ~~create answers 404~~ | *deleted 2026-10-14 (D49); see the re-run below* |
| `Slice04ThroughputAndShownMetricsScenarios` | 7 | same | MISSING_FUNCTIONALITY |
| `Slice05WipAndWorkItemAgeScenarios` | 7 | same | MISSING_FUNCTIONALITY |
| `Slice06PortfolioReportScenarios` | 7 | `…/portfolios/1/reports…` answers 404 | MISSING_FUNCTIONALITY |
| `Slice07NowLengthScenarios` | 12 | same as slice 01 | MISSING_FUNCTIONALITY |
| `Slice08DeleteAndCommunityCapScenarios` | 12 | same | MISSING_FUNCTIONALITY |
| `Slice10EditReportScenarios` | 12 | same | MISSING_FUNCTIONALITY |
| `ReportsWithoutSignInScenarios` | 3 | same | MISSING_FUNCTIONALITY |
| `ReportUsageEventsTests` | 23 | `ReportCreated` / `ReportOpened` / `ReportDeleted` refused with 400 (name not on the list); `TeamDetail_Reports` / `PortfolioDetail_Reports` refused; `UsageDataReportTemplate` and the three names absent from the enums; `docs/settings/usagedata.md` lacks the rows | MISSING_FUNCTIONALITY |

The scenarios expecting 404 or 403 cannot be vacuously green on a missing route: each first creates a report, or
reads the list, through an endpoint that has to answer 2xx, so a 404 from a missing route fails before the refusal
is asserted. Validation refusals expect 400 with the reason code, which a missing route does not give.
The one refusal that was accepted today, a tab opening carrying `reportTemplate`, fails because the pipe ignores the
unknown member; it goes green only once `reportTemplate` is known and limited to the two report events.

## Frontend — 93 cases

Run: `pnpm exec vitest run` on the eight files below, un-skipped. (The 35 existing cases in
`OverviewDashboard.test.tsx` passed throughout.)

| File | Cases | Failure | Classification |
|---|---|---|---|
| `pages/Common/Reports/ReportsTab.test.tsx` | 19 | `Not yet implemented -- RED scaffold: the Reports tab of team 1` | MISSING_FUNCTIONALITY (scaffold throws) |
| `pages/Common/Reports/ReportsTab.createWizard.test.tsx` | 24 | same | MISSING_FUNCTIONALITY |
| `pages/Common/Reports/ReportView.test.tsx` | 26 | `Not yet implemented -- RED scaffold: report 42 of team 1` | MISSING_FUNCTIONALITY |
| `pages/Common/Reports/ReportView.editWizard.test.tsx` | 8 | same | MISSING_FUNCTIONALITY |
| `pages/Teams/Detail/TeamDetail.reportsTab.test.tsx` | 5 | no tab named Reports; no Reports view or report view rendered | MISSING_FUNCTIONALITY |
| `pages/Portfolios/Detail/PortfolioDetail.reportsTab.test.tsx` | 3 | same | MISSING_FUNCTIONALITY |
| `services/UsageData/usageDataRouteKeys.reports.test.ts` | 4 | `/teams/7/reports` answers no tab opening | MISSING_FUNCTIONALITY |
| `pages/Overview/OverviewDashboard.test.tsx` (new block) | 4 | the delete dialog does not name the reports, nor ask for them | MISSING_FUNCTIONALITY |

Three cases were green on the first run and were rewritten before hand-off, because each asserted only something
already true: "says nothing about reports when there are none" now first proves the Overview asked for that Team's
reports; "reports the Team deleted and never a report deleted" now first proves the dialog named the 2 reports;
"names no tab for the address of one report" now first proves the Reports tab itself is named. All three fail on
the second run.

## Revision run (2026-10-13, after DESIGN's revision) — 260 cases

Same procedure (Ignores and `.Ignore(…)` stripped, `it.skip` / `it.skip.each` → `it` / `it.each`, run, files restored
from copies, suites re-run skipped: backend 144 skipped, frontend 116 skipped, both green).

**Result: 260 of 260 runnable cases fail, every one for missing behaviour. None broken.**

Backend — `dotnet test … --filter "FullyQualifiedName~API.Integration.Reports|FullyQualifiedName~ReportUsageEventsTests"`
→ Failed 144, Passed 0:

| Failure | Cases | Classification |
|---|---|---|
| "The report was not created" — create answers 404 | 78 | MISSING_FUNCTIONALITY (no endpoint) |
| `…/reports/templates answered 404` — includes all 12 rows of `ReportWindowBoundaryScenarios` | 19 | MISSING_FUNCTIONALITY |
| multiple-assert scopes over 404 answers (list, read, edit, delete) | 25 | MISSING_FUNCTIONALITY |
| usage events refused or not on the list; enum `UsageDataReportTemplate` absent | 13 | MISSING_FUNCTIONALITY |
| `docs/settings/usagedata.md` has no "A report was …" / "Which type of report" rows, the tab rows still say six Team / five Portfolio tabs, the addresses are not listed | 8 | MISSING_FUNCTIONALITY |
| without sign-in, create answers 404 | 1 | MISSING_FUNCTIONALITY |

Frontend — `pnpm exec vitest run` on the nine files → 116 failed (35 existing `OverviewDashboard` cases passed):

| Failure | Cases | Classification |
|---|---|---|
| `Not yet implemented -- RED scaffold: the Reports tab of team/portfolio N` | 50 | MISSING_FUNCTIONALITY (scaffold throws) |
| `Not yet implemented -- RED scaffold: report N of team N` | 36 | MISSING_FUNCTIONALITY |
| `Not yet implemented -- RED scaffold: checking a then/now window …` (`reportWindowCheck`, the shared table) | 12 | MISSING_FUNCTIONALITY |
| no Reports tab, no report view, no tab opening for `/teams/7/reports` | 12 | MISSING_FUNCTIONALITY |
| Overview delete dialog: no report count, no spinner, no "and any reports it has" | 6 | MISSING_FUNCTIONALITY |

## Re-run 2026-10-14 — the pending slice 04 and 05 cases after D49–D52

Slices 01–03 have shipped, so the endpoints exist and the failures are now about the missing metrics, not a 404.
Each file was un-skipped in a copy, run, and restored.

| Where | Cases | Failure | Classification |
|---|---|---|---|
| `Slice04ThroughputAndShownMetricsScenarios` | 7 | 6 fail: the templates read lists only `cycle-time`; no `throughput` capture; no `no-metric-shown` / `unknown-metric` refusal (a report is created). `An_older_report_did_not_capture_Throughput` passes already: nothing captures {Throughput} yet, so "not captured" holds vacuously until slice 04 starts capturing it — it is a preservation guard slice 04 must keep green | MISSING_FUNCTIONALITY (6); vacuously green before its slice (1) |
| `Slice05WipAndWorkItemAgeScenarios` | 7 | every line of `wip` / `work-item-age` reads as not in the report (`IsPresent` false, value null); templates list only `cycle-time` | MISSING_FUNCTIONALITY |
| `ReportView.test.tsx` (slices 04, 05, 08, 10) | 16 run, 15 fail | no Throughput / WIP / Total Work Item Age panel; the summary still counts from {Cycle Time} (38 / 41, expected 52 / 65); no unsupported-metric note; slice 08/10 cases unchanged. "offers a reader neither" passes vacuously, as before | MISSING_FUNCTIONALITY |
| `Report.test.ts` (slice 04) | 5 | the schema still accepts the four retired absence reasons and keeps `beyondThenLimits` | MISSING_FUNCTIONALITY |

The slice-07 placeholder "says how long ago Then ended…" is an `it.todo` until the maintainer has seen a sketch of
the line; it has no assertion to classify. Suites re-run skipped afterwards: backend Reports filter 366 passed, 69
skipped; frontend full suite green with skips.

