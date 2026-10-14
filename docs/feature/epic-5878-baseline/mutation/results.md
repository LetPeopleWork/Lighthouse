# Mutation testing — 6159 / 6160 (Then & Now, slices 01 and 02)

Run 2026-10-13 against `main` @ `41c0a13fd` (feature work @ `69b693597` plus the test commits below). Gate is
80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **89.09 %** | 165 | 147 | 13 | 0 | 5 | 17 m 36 s |
| Frontend (StrykerJS 10.0.0) | **89.06 %** | 905 | 806 | 98 | 0 | 1 | 32 m 10 s |

First runs: backend 72.12 % (119 / 37 / 9 no coverage, 17 m 44 s); frontend 73.70 % (667 / 218 / 20 no
coverage, 33 m 49 s), then 84.42 % and 88.40 % on intermediate passes.

Configs: `stryker.6159.backend.json`, `stryker.6159.frontend.json`, `vitest.stryker.6159.config.ts`.
Backend runs from `Lighthouse.Backend.Tests/`; frontend from `Lighthouse.Frontend/` with
`pnpm exec stryker run ../docs/feature/epic-5878-baseline/mutation/stryker.6159.frontend.json` after copying
`vitest.stryker.6159.config.ts` into `Lighthouse.Frontend/` (that root pattern is gitignored).

Two traps found on the way, both fixed in the committed config:

- **`ignorePatterns: ["reports"]` (copied from the reference pair) silently drops every `Reports/` directory**
  — the match is case-insensitive, so `src/pages/Common/Reports/**` and `src/models/Reports/Report.ts`
  reported "did not result in any files". Root-anchored patterns (`/reports`, `/dist`, …) fix it.
- The backend scope is only reachable through the `ReportsAcceptanceTest` WebApplicationFactory suites, so
  the filter keeps them (they run in ~20 s here, not 80 min) and drops only the hand-run
  `ReportCreationTimingProbe` and the live-connector categories.

## Backend

| file | tested | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| API/TeamReportsController.cs | 26 | 22 | 3 | 1 |
| Services/Implementation/Reports/ReportCommands.cs | 29 | 28 | 1 | 0 |
| Services/Implementation/Reports/ThenAndNowTemplate.cs | 19 | 16 | 1 | 2 |
| Services/Implementation/Reports/ReportWindowPolicy.cs | 17 | 16 | 1 | 0 |
| Services/Implementation/Reports/ReportQueries.cs | 14 | 13 | 1 | 0 |
| Models/Reports/Report.cs | 12 | 8 | 4 | 0 |
| Models/UsageData/UsageDataEventShapes.cs | 12 | 12 | 0 | 0 |
| Models/Reports/ThenAndNowPayload.cs | 9 | 9 | 0 | 0 |
| Services/Implementation/Repositories/ReportRepository.cs | 8 | 8 | 0 | 0 |
| Services/Implementation/Reports/ReportOwnerLookup.cs | 6 | 6 | 0 | 0 |
| Services/Implementation/Reports/Metrics/CycleTimeReportMetric.cs | 6 | 6 | 0 | 0 |
| Services/Implementation/Reports/TeamReportMetricSeries.cs | 6 | 3 | 2 | 1 |
| Services/Implementation/Reports/Metrics/ReportMetricCatalog.cs | 1 | 0 | 0 | 1 |

`ReportReadModels.cs`, `ReportRefusalCodes.cs`, `NewReportRequest.cs` and `ReportOwner.cs` are in `mutate` and
produced no mutants (records and constants).

### Closed by this pass

Commit `69a6743a7` — unit tests at each port:

- Portfolio ownership (`Report.For`/`OwnedBy`, `ReportOwnerLookup`, `ReportRepository`): the acceptance suites
  only create Team reports, so a Portfolio report landing in the Team column, being found among the Teams,
  listed under the Team sharing its id, or reading back as Portfolio 0 all survived.
- `ReportRepository` lists oldest first (OrderBy → OrderByDescending survived).
- `ReportQueries.List`: the summary's `thenAndNow.thenRebuildable` (true / false / absent with no Then window)
  and an unreadable row still listed and logged as a warning.
- `ThenAndNowTemplate.Read`: the shown metric keys a row names, and `"null"` read as none.
- `ThenAndNowTemplate.Freeze`: a Now of exactly one day is accepted.
- `ReportWindowPolicy.ThenRebuildable` at the oldest kept day; `NowWindow(today, 1)` is today alone
  (`ReportWindow.EndingOn` `<=` → `<`).
- `ReportCommands`: exactly 10 s is not slow; a create without a Now length logs Now as long as Then.
- `TeamReportsController`: the `Created` address, a missing name / metric list reaching the command as
  `""` / `[]`, the problem body's `status`, and `earliestStart` added only to cutoff refusals.

### Accepted survivors

- **Exception and validation wording** (`TeamReportsController.cs:36`, `:62` Title, `TeamReportMetricSeries.cs:16`,
  `:19` no coverage, `ThenAndNowTemplate.cs:66`/`:69`/`:100`): message text only; the tests pin the status, the
  code and the exception type, which is what callers act on.
- **`TeamReportsController.cs:39` TemplateKey `?? ""` → "Stryker was here!"** (no coverage): an absent key and an
  unknown one are both refused as `template-not-supported`. Equivalent.
- **`TeamReportsController.cs:69` `{ StatusCode = status }` removed**: ASP.NET Core copies `ProblemDetails.Status`
  onto an `ObjectResult` without a status code, so the response is the same 400/422. Equivalent over HTTP.
- **`Report.cs:20/22/26/28` property defaults**: `Report.For` and EF materialisation always overwrite them, and
  `ReportCommands.Create` writes both JSON columns before saving. Equivalent.
- **`ReportCommands.cs:40` `||` → `&&`**: needs a template returning `Done` with a null payload; the only template
  serialises a payload record, which is never null. Unreachable.
- **`ReportQueries.cs:42` `CreationBlockedByCap` false → true**: a placeholder until the Community cap (slice 08),
  whose pending acceptance scenarios pin it.
- **`ReportWindowPolicy.cs:24` `<` → `<=`**: at `cutoff == today.DayNumber`, `today.AddDays(-cutoff)` *is*
  `DateOnly.MinValue`. Equivalent.
- **`TeamReportMetricSeries.cs:24` `> 0` → `>= 0`**: `GetClosedItemsForTeam` returns Done items only, whose
  cycle time is at least 1 (inclusive count, fallback 1). Equivalent.
- **`ReportMetricCatalog.cs:12` `Find`** (no coverage): no caller in this version.

### Not mutated

- `API/UsageDataController.cs` (221 lines, 2 changed) and `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs`
  (306 lines, 1 changed): Stryker.NET cannot scope to lines. The changed lines are pinned by
  `ReportUsageEventsTests` (an undefined report type refused; the type travels as `report_template`).
- `Models/UsageData/UsageDataRoutePatterns.cs` (one dictionary entry), `UsageDataEventReported.cs`,
  `UsageDataEventBatchDto.cs`, `UsageDataReportTemplate.cs` (record/enum additions): pinned by
  `UsageDataRoutePatternsTests` and `ReportUsageEventsTests`.
- `Data/LighthouseAppContext.cs` (mapping), `Program.cs` (registration), migrations: wiring, exercised by every
  acceptance scenario.
- Interfaces under `Services/Interfaces/Reports/`: no executable code.

## Frontend

Ranges as in `stryker.6159.frontend.json`: whole files for `src/pages/Common/Reports/**`, `ReportService.ts`
and `WizardStepperFrame.tsx`; changed lines only for `Report.ts`, `WidgetShell.tsx`, `widgetStatus.ts`,
`ApiError.ts`, `BaseApiService.ts`, `usageDataReporter.ts`, `usageDataRouteKeys.ts`, `UsageData.ts`,
`UsageDataService.ts` and `TeamDetail.tsx` (the last four ranges, and `ApiError.ts:31`, produce no mutants).

| file | tested | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| Reports/wizard/ReportWizard.tsx | 93 | 83 | 10 | 0 |
| Reports/panels/thenNowComparison.ts | 83 | 82 | 1 | 0 |
| Reports/wizard/thenAndNow/thenAndNowDraft.ts | 76 | 75 | 1 | 0 |
| Reports/panels/CycleTimePanel.tsx | 75 | 66 | 9 | 0 |
| Reports/ReportView.tsx | 74 | 65 | 9 | 0 |
| Reports/wizard/reportRefusalCopy.ts | 58 | 56 | 2 | 0 |
| Reports/ReportsTab.tsx | 57 | 47 | 10 | 0 |
| Reports/wizard/reportWindowCheck.ts | 44 | 42 | 1 | 1 |
| Reports/wizard/reportTypes.ts | 39 | 39 | 0 | 0 |
| Reports/wizard/thenAndNow/ThenStep.tsx | 36 | 30 | 6 | 0 |
| models/Reports/Report.ts | 30 | 30 | 0 | 0 |
| CreateWizards/WizardStepperFrame.tsx | 30 | 20 | 10 | 0 |
| Reports/panels/ThenNowPanelFrame.tsx | 24 | 5 | 19 | 0 |
| services/Api/ReportService.ts | 24 | 24 | 0 | 0 |
| Reports/wizard/thenAndNow/MetricsAndNameStep.tsx | 23 | 23 | 0 | 0 |
| Reports/ReportList.tsx | 21 | 14 | 7 | 0 |
| Reports/wizard/TypeStep.tsx | 18 | 13 | 5 | 0 |
| MetricsView/WidgetShell.tsx | 17 | 11 | 6 | 0 |
| Reports/useReportOpenedReporter.ts | 16 | 16 | 0 | 0 |
| Teams/Detail/TeamDetail.tsx | 10 | 10 | 0 | 0 |
| Reports/reportOwner.ts | 9 | 9 | 0 | 0 |
| services/Api/BaseApiService.ts | 9 | 9 | 0 | 0 |
| services/UsageData/usageDataReporter.ts | 9 | 9 | 0 | 0 |
| Reports/ThenAndNowHeader.tsx | 6 | 6 | 0 | 0 |
| MetricsView/widgetStatus.ts | 6 | 6 | 0 | 0 |
| Reports/panels/reportPanelRegistry.ts | 4 | 4 | 0 | 0 |
| Reports/panels/reportWindowPresets.ts | 4 | 4 | 0 | 0 |
| services/UsageData/usageDataRouteKeys.ts | 2 | 2 | 0 | 0 |

### Closed by this pass

Commits `581628212`, `cb9eadeb8`, `d04272cff`, `41c0a13fd`:

- `reportRefusalCopy.ts`: every refusal sentence (nine had no coverage), the day a refusal names over the
  wizard's, `null` as "the oldest kept day", and the plain retry for non-refusals, refusal-shaped objects and
  unknown codes.
- `thenAndNowDraft.ts`: blank, fractional, zero and negative lengths; `resolvedThenDays` / `defaultNameOf`
  without a day or a length; the Then & Now metrics found among other Types.
- `reportTypes.ts`: step labels, each step's completion rule, a Type's title.
- `Report.ts` schemas: lower-case owner kinds, the three absent reasons, a metric key that is not a string.
- `reportWindowCheck.ts`: the first day the calendar can name (739 891 / 739 892 days back from 2 Oct 2026).
- Owner term and window presets per owner kind; `usageDataReportTemplateOf` for an unknown Type;
  `BaseApiService` drops an `earliestStart` that is not a day.
- `MetricsAndNameStep`: ticking, unticking, the ask for a metric, a metric with no panel named by its key.
- `ReportView`: which failures are retried and how often, Retry versus the way back, `aria-busy`, one crumb
  while the name is on its way.
- `ReportsTab` / `ReportList`: `aria-busy`, the Type and Created cells (the old assertion matched "Then & Now"
  inside the report's *name*), "Not supported in this version".
- `ReportWizard`: closes and refreshes the list after creating, re-asks for bounds only after a cutoff refusal,
  clears an old refusal when creating again, its accessible name, no Back or Create on the first step, fresh
  bounds on every opening, the 90-day preset pressed and kept when clicked again.
- `WidgetBody` loading message: said only while there is nothing to dim.
- `CycleTimePanel`: "Not captured for this report", the Work Item count from the first percentile that has one
  (even when the first is missing), no count for a side of dashes, no change beside a Now dash.

### Accepted survivors

- **Styling only — 64 mutants**: `sx` objects and their strings, `slotProps.inputLabel.shrink`, and
  `ThenNowPanelFrame.tsx:41` (`first` only feeds the divider's `sx`) in `WizardStepperFrame` (9),
  `WidgetShell` 177–182 (6), `ReportView` 97–99 (3), `ReportsTab` 65, 83–85 (6), `CycleTimePanel` 31–34, 86, 91,
  120 (9), `ThenNowPanelFrame` (19), `TypeStep` (5), `NowStep:26`, `ThenStep` 49, 53, 79 (6). jsdom does no layout;
  the screenshot and E2E suites are where these show.
- **Equivalent — 13 mutants**: `thenNowComparison.ts:32` (a zero delta returns "neutral" first);
  `thenAndNowDraft.ts:44` first operand (`null > 0` is false); `reportRefusalCopy.ts:48` (`parseLocalDate(null)`
  returns null) and `:95` first operand (`isRefusalCode(undefined)` is false); `ReportView.tsx:42`, `:44`, `:73`
  (the app's QueryClient sets `queries.retry: 2`, a number, so the `true`/non-number branch and the optional
  chain never matter); `ReportView.tsx:81`, `ReportWizard.tsx:50` ×2 (query-key spelling; nothing else shares
  the key); `ReportWizard.tsx:94`/`:95` (an empty filter invalidates every query, including the list);
  `ReportView.tsx:120` (the newer-version error is only seen in the error status — no Retry is offered).
- **Not observable in jsdom — 8 mutants**: `ReportList.tsx:26/35/42` `field`, `:29` `hideable`, `:56`
  `storageKey` (column identity for sorting, the column menu and saved column state); `ReportList.tsx:46`
  (the schema guarantees a valid day); `ReportWizard.tsx:54` `refetchOnWindowFocus` (needs a window focus
  event); `WizardStepperFrame.tsx:77` (no caller in this feature passes
  `details`; an empty caption renders no text).
- **Reachable only during a background refetch over loaded data — 6 mutants**: `ReportView.tsx:106`,
  `ReportsTab.tsx:92` ×3, `ReportWizard.tsx:58`, `:143`. Nothing in slices 01–02 refetches a loaded report or
  types; the list's refetch after create happens while the page navigates to the new report.
- **Pending slices — 6 mutants**: `ReportsTab.tsx:51` (Portfolio admin, slice 06), `NowStep.tsx:29` (Now's
  sentence, slice 07, pinned by a skipped scenario), `reportWindowCheck.ts:72` and `:76` no coverage (the Now
  check, slice 07), `ReportWizard.tsx:60` (`typeStep >= 0` → true reads `steps[-1]`, undefined either way), `ReportList.tsx:49` (`useMemo` deps; the list unmounts when the owner changes).
- **Known gap left open — 2 mutants**: `ReportWizard.tsx:133` (`isLastStep && templates` → true survived the
  "no Create on the first step" assertion; why was not investigated) and
  `CycleTimePanel.tsx:48` one operand. Iteration stopped here at the coordinator's request with the gate met.

### Not mutated

- `src/App.tsx:230-233`: route registration, covered by the walking-skeleton E2E.
- `src/components/Common/CreateWizards/CreateWizardShell.tsx`: a pure extraction refactor; the logic it lost
  moved into `WizardStepperFrame.tsx`, which is mutated whole.
- `usageDataBuffer.ts` and the interface lines of `UsageDataService.ts` / `usageDataReporter.ts`: type-only.

## Found along the way

`reportWindowCheck.ts` counts days with `Date.UTC(year, …)`, which maps years 0–99 to 1900–1999. A Then
ending in the year 1–99 is therefore checked against the wrong calendar day in the wizard (the server is
right and still refuses). Not changed here — production behaviour is out of scope for a mutation pass.

---

# Mutation testing — 6161 / 6162 / 6163 (Then & Now, slices 03, 04, 05 and 05b)

Run 2026-10-14 on `main` @ `9ae277961`, once, on frozen code after the final refactor and adversarial review.
Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **86.79 %** | 53 | 46 | 4 | 3 | 9 m |
| Frontend (StrykerJS) | **88.21 %** | 619 | 546 | 73 | 0 | 14 m |

Configs: `stryker.6161-6163.backend.json` (run from `Lighthouse.Backend.Tests/` with `--config-file`),
`stryker.6161-6163.frontend.json` + `vitest.stryker.6161-6163.config.ts` (copy the vitest config into
`Lighthouse.Frontend/`; `perTest` coverage and incremental mode, so a re-run only tests what changed).

Both runs are above the gate, so no further iteration; the survivors are recorded below.

## Backend

| file | tested | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| Reports/ThenAndNowTemplate.cs | 33 | 29 | 2 | 2 |
| Reports/TeamReportMetricSeries.cs | 7 | 4 | 2 | 1 |
| Reports/Metrics/CycleTimeReportMetric.cs | 5 | 5 | 0 | 0 |
| Reports/Metrics/DailySeriesSummary.cs | 3 | 3 | 0 | 0 |
| Reports/Metrics/WindowedReportMetric.cs | 3 | 3 | 0 | 0 |
| Reports/Metrics/ThroughputReportMetric.cs | 2 | 2 | 0 | 0 |

`WipReportMetric.cs`, `WorkItemAgeReportMetric.cs` and `ReportRefusalCodes.cs` produced no mutants. The small
`TeamMetricsService` / `BaseMetricsService` hunks (the cached daily total age) were left out: whole-file
mutation would score those services' older code, not this change; their caching is covered by unit tests.

Survivors, all accepted:
- Exception and log message strings (`ThenAndNowTemplate` L73, L76, L139; `TeamReportMetricSeries` L46, L49).
- `?? []` on the stored captures' keys, inside the unknown-key log call only (`ThenAndNowTemplate` L80).
- `CycleTime > 0` → `>= 0` when counting finished items (`TeamReportMetricSeries` L17): a finished item's cycle
  time is at least one day, so the two are equivalent.

## Frontend

| file | tested | killed | survived | score |
| --- | --- | --- | --- | --- |
| panels/thenNowComparison.ts | 116 | 115 | 1 | 99.1 % |
| ThenAndNowSummary.tsx | 111 | 98 | 13 | 88.3 % |
| wizard/thenAndNow/thenAndNowDraft.ts | 90 | 84 | 6 | 93.3 % |
| ReportView.tsx | 73 | 64 | 9 | 87.7 % |
| panels/changeSummary.ts | 45 | 45 | 0 | 100 % |
| panels/reportPanelRegistry.ts | 45 | 42 | 3 | 93.3 % |
| panels/ThenNowLineParts.tsx | 35 | 25 | 10 | 71.4 % |
| panels/ThroughputPanel.tsx | 29 | 18 | 11 | 62.1 % |
| panels/ThenNowPanelFrame.tsx | 23 | 6 | 17 | 26.1 % |
| wizard/thenAndNow/MetricsAndNameStep.tsx | 21 | 21 | 0 | 100 % |
| models/Reports/Report.ts (174–191) | 12 | 12 | 0 | 100 % |
| others (CycleTime, DailySpread, Wip, WorkItemAge, Unsupported panels, presets) | 19 | 16 | 3 | — |

Survivor kinds: 23 string literals and 18 object literals (MUI `sx` props and layout copy — the known
equivalent class here, `ThenNowPanelFrame` is almost all of them), 15 conditional expressions, the rest
optional chaining / boolean / array literals.

Worth a test later (not chased, the gate was met):
- `ThenAndNowSummary` L51/L65–66: the closed count's fallback to the Cycle Time sample size when Throughput was
  never captured, and the all-`no-finished-items` branch.
- `ThroughputPanel` L37–54: which absence reason the panel shows when only one side is absent.
- `thenAndNowDraft` L92: a window with neither day entered.
- `ReportView` L55/L57/L127/L145: retry/loading branches already covered by E2E-shaped tests elsewhere;
  hand-check before chasing (StrykerJS reports false survivors on this repo).

---

# Mutation testing — 6164 (Reports on Portfolios, slice 06), backend only

Run 2026-10-14 on `main` @ `5c922a2de`, once, on frozen code after the slice's review fixes and refactor.
Config `stryker.6164.backend.json`. Frontend run deferred (maintainer: no long CPU jobs before the meeting);
survivors recorded, not chased.

| file | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| API/ReportsControllerBase.cs | 26 | 22 | 3 | 1 | 84.6 % |
| Models/UsageData/UsageDataRoutePatterns.cs | 13 | 13 | 0 | 0 | 100 % |
| Services/Implementation/Reports/PortfolioReportMetricSeries.cs | 7 | 4 | 2 | 1 | 57.1 % |
| Services/Implementation/Reports/ReportMetricSeriesByOwner.cs | 3 | 3 | 0 | 0 | 100 % |
| **Total** | **49** | **42** | **5** | **2** | **85.71 %** |

`PortfolioReportsController.cs`, `TeamReportsController.cs` and `ReportSeriesReading.cs` produced no mutants of
their own (routing attributes; the shared code sits in the base class).

Survivors: problem-detail and exception message strings (`ReportsControllerBase` L26, L29, L51;
`PortfolioReportMetricSeries` L45, L48); the problem result's object initializer emptied (`ReportsControllerBase`
L58, worth a status-code assertion later); `CycleTime > 0` → `>= 0` when counting finished Features
(`PortfolioReportMetricSeries` L18), equivalent because a finished Feature's cycle time is at least one day.
