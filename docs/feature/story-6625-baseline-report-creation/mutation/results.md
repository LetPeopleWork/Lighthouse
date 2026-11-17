# Mutation testing, Story 6625 (Then and Now: a Now always as long as Then)

Gate is 80 % per stack. Scope is the production changed since `3d6e179ab`.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **87.93 %** (gate met) | 53 (+5 no coverage) | 51 | 2 | 5 | 0 | 4 m 25 s |
| Frontend (StrykerJS, concurrency 1) | **85.38 %** (gate met) | 130 | 111 | 18 | 1 | 0 | 24 m 10 s |

Configs: `stryker.6625.backend.json`, `stryker.6625.frontend.json`, `vitest.stryker.mutation.ts`.

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests`:
`dotnet stryker --config-file ../../docs/feature/story-6625-baseline-report-creation/mutation/stryker.6625.backend.json`

| File | Result |
| --- | --- |
| `Models/Reports/ThenAndNowPayload.cs` | 9 killed, 1 ignored |
| `Services/Implementation/Reports/ReportWindowPolicy.cs` | 17 killed, 1 survived, 1 ignored |
| `Services/Implementation/Reports/ThenAndNowTemplate.cs` | 25 killed, 1 survived, 5 no coverage, 12 ignored |

Test selection: `Tests.Acceptance.BaselineReportCreation` (service-port scenarios) plus the unit tests under
`Tests.Services.Implementation.Reports` and `Tests.Models.Reports`; `API.Integration`, `Integration.Containers`,
`Dogfood`, `Tests.Contracts` and the live-connector categories are left out.

### Closed by this pass

- `ThenAndNowTemplate.cs` L137, `shown.All(known.Contains)` mutated to `Any`: a new row on
  `An_edit_showing_metrics_a_new_report_could_not_show_is_refused_and_changes_nothing` in
  `Services/Implementation/Reports/ThenAndNowTemplateEditTest.cs` (one known key plus `velocity` must be refused as
  `unknown-metric`). Checked by the deliberate break: with `Any` the row went red, with `All` back all 15 pass.

### Accepted survivors

- `ReportWindowPolicy.cs` L24, `doneItemsCutoffDays < today.DayNumber` mutated to `<=`: equivalent. At equality,
  `today.AddDays(-DayNumber)` is `DateOnly.MinValue`, the same value the other branch returns.
- `ThenAndNowTemplate.cs` L79, L81, L94, L96, L159 (no coverage, `$""` string mutations): the text of
  `JsonException`s thrown for a stored payload that a report cannot have through the public API. The message is
  diagnostic and no caller reads it.

### Not mutated

`ReportRefusalCodes.cs` (constants), `IReportCommands.cs` (interface) and `ReportEditRequest.cs` (record): no logic.

## Frontend

From `Lighthouse.Frontend`, with `vitest.stryker.mutation.ts` copied there (removed afterwards):
`pnpm exec stryker run ../docs/feature/story-6625-baseline-report-creation/mutation/stryker.6625.frontend.json`
(perTest coverage, incremental, concurrency 1). The first attempt, at concurrency 2 with every report spec in the
include list, was stopped by the system for low memory in the dry run. The include list was then narrowed to the wizard
and panel folders, the four Reports tab and view specs, the bulk wizard specs, `bulkWizardSteps.test.ts` and
`ownerLimitOfWindow.test.ts`. `git status` showed `src/` clean afterwards.

| File | Score | Survived |
| --- | --- | --- |
| `thenNowComparison.ts` | 92.86 % | 1 |
| `ThenNowLineParts.tsx` | 62.50 % | 3 |
| `ThroughputPanel.tsx` | 100 % | 0 |
| `thenAndNowDraft.ts` | 83.33 % | 8 |
| `ThenStep.tsx` | 87.50 % | 4 |
| `reportRefusalCopy.ts` | 85.71 % | 1 (+1 no coverage) |
| `reportTypes.ts`, `reportWindowCheck.ts`, `ReportWizard.tsx`, `ReportsTab.tsx` | 100 % | 0 |
| `ownerLimitOfWindow.ts` | 75.00 % | 1 |

`reportWizardRules.ts` produced no mutant.

### Closed by the kill round (not re-measured; a second run costs 24 minutes)

New tests in `src/pages/Common/Reports/wizard/thenAndNow/thenAndNowDraft.test.ts`, aimed at the `thenAndNowDraft.ts`
guards (L65 and the null checks of `windowsOf`):

- `thenEndOf`: `is the day before Now starts when the draft names no end`, `is the entered end when the draft names one`,
  and `falls back to today for ...` (a length that is no whole number, a today that is no day, a length that reaches
  before the calendar).
- `windowsOf without a usable today or a reachable length`: `names no windows for ...` (a today that is no day, a length
  that reaches before the calendar).

### Accepted survivors

Not killed in this round, by cost: `ThenNowLineParts.tsx` L81 (an object and a string in a label) and L119
(`line.showPercent && ...`), `ThenStep.tsx` L29, L32, L93 (helper text and render conditions), `reportRefusalCopy.ts`
L91-92 (the fallback wording), `thenNowComparison.ts` L154, `ownerLimitOfWindow.ts` L29 (a window label), and the
`thenAndNowDraft.ts` L144/L148 operands that only matter when both windows are unreachable at once. The gate holds
without them.

