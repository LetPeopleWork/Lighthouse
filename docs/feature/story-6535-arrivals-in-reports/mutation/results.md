# Mutation testing, Story 6535 (Arrivals in reports)

The gate is an 80 % kill rate per stack. Scope is the production changed since `2420e90f5`. The configs follow the
shape of Story 6655's (`/mutation-testing`, `.claude/commands/mutation-testing.md`).

## Backend

Run 2026-11-17, Stryker.NET, worktree at `18b53cf59` (production frozen since `40ed685a5`).
Run from `Lighthouse.Backend/Lighthouse.Backend.Tests`:
`dotnet stryker --config-file ../../docs/feature/story-6535-arrivals-in-reports/mutation/stryker.6535.backend.json`

| run | score | tested | killed | survived |
| --- | --- | --- | --- | --- |
| first | **72.73 %** (gate failed) | 22 | 16 | 6 |
| after the kill round | **100 %** | 22 | 22 | 0 |

| File | First run | After kill round |
| --- | --- | --- |
| `Metrics/ArrivalsReportMetric.cs` | 2/2, 100 % | 100 % |
| `Metrics/ThroughputReportMetric.cs` (one-line change, added to scope) | 1/1, 100 % | 100 % |
| `Metrics/WindowedReportMetric.cs` | 4/4, 100 % | 100 % |
| `Reports/TeamReportMetricSeries.cs` | 3/6, 50 % | 100 % |
| `Reports/PortfolioReportMetricSeries.cs` | 3/6, 50 % | 100 % |
| `Reports/ReportMetricSeriesByOwner.cs` | 3/3, 100 % | 100 % |

Mutants inside the changed lines (the `DailyArrivals` members, the `ByOwner` and `Windowed` additions): all killed in the
first run. The six survivors were in lines this Story did not change (the finished-item count `CycleTime(...) > 0`, 2 per
series file, and the refusal message text, 1 per series file).

Test selection: the unit tests under `Tests.Services.Implementation.Reports` plus, by name and ahead of the
`API.Integration` exclusion, `ArrivalsInReportsScenarios` (in `API/Integration/Reports/ReportsAtThePortTest.cs`; it uses
`ServicePortHarness`, no web host). Live-connector, container, Dogfood and Contracts exclusions are kept. Not mutated:
`Program.cs` (one registration line).

### Closed by the kill round (changed test files, no new files)

- `Services/Implementation/Reports/TeamReportMetricSeriesTest.cs`: `Only_the_closed_items_with_a_Cycle_Time_count_as_finished`
  (a Done item and a Doing item give 1 finished) and `A_Portfolio_asking_the_Team_series_is_told_what_it_asked_for_and_what_it_is`
  (message pinned).
- `Services/Implementation/Reports/PortfolioReportMetricSeriesTest.cs`: the same two rows for Features and the Portfolio series.

## Frontend

Run twice, perTest coverage and incremental, 90 mutants. First run 78.89 % (19 survived, gate failed), after the kill
round **96.67 %** (3 survived).

First run: `pnpm exec stryker run ../docs/feature/story-6535-arrivals-in-reports/mutation/stryker.6535.frontend.json`
from `Lighthouse.Frontend` with `vitest.stryker.mutation.ts` copied there (removed afterwards).

| run | score | survived |
| --- | --- | --- |
| first | **78.89 %** (gate failed) | 19 |

| File | First run |
| --- | --- |
| `ArrivalsPanel.tsx` | 81.67 % |
| `reportPanelRegistry.ts` | 100 % |
| `thenNowComparison.ts` | 100 % |
| `ThenNowLineParts.tsx` | 100 % |
| `ThenNowPanelFrame.tsx` | 20 % |
| `ReportPanels.tsx`, `ThroughputPanel.tsx`, `MetricsAndNameStep.tsx`, `Report.ts` | no mutants |

| File | After kill round |
| --- | --- |
| `ArrivalsPanel.tsx` | 96.67 % (81.67 % first) |
| `ThenNowPanelFrame.tsx` | 90 % (20 % first) |
| `reportPanelRegistry.ts`, `thenNowComparison.ts`, `ThenNowLineParts.tsx` | 100 % |

### Closed by the kill round

New file `src/pages/Common/Reports/panels/ArrivalsPanel.sides.test.tsx` (5 tests): a missing total reads a dash, not
`null`; a missing gap reads a dash, not "as many started"; the reason the server gave is preferred over "Not captured";
three spacer rows above the change; the info button is named "About Arrivals"; the gap sentence is bold; the frame's
heading gap and info row.

### Accepted survivors

| Mutant | Reason |
| --- | --- |
| `ArrivalsPanel.tsx:118` (2: `{}` and `() => undefined` in `SPACER_ROWS`) | Module-level constant, a static mutant Stryker reports as surviving. Applying the `{}` mutant by hand turns the new "three empty rows" test red, so the test pins it. Not a test gap. |
| `ThenNowPanelFrame.tsx:121` (`info === undefined` to `false`) | After the last run I added a check on the heading's parent; applying the mutant by hand turns the frame test red. Stryker's last run predates that line. Not re-run (one kill round). |

Not mutated: `Report.ts` (types only), `ReportPanels.tsx`, `ThroughputPanel.tsx`, `MetricsAndNameStep.tsx` (no mutants
in the changed lines), imports, `Program.cs`.

Final runs: `vitest run src/pages/Common/Reports` 37 files, 672 tests green; `pnpm --dir Lighthouse.Frontend build` clean;
`dotnet build Lighthouse.Backend` 0 warnings, 0 errors; the two backend series test classes 13 tests green.
`vitest.stryker.mutation.ts` was removed from `Lighthouse.Frontend` after the run; `git status` shows no mutant in `src/`.
