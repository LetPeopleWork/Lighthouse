# Mutation testing — Epic 6298 "Report PDF Export", Lighthouse frontend

> **Gate met after one kill round: 83.87 %** (759 killed / 140 survived / 6 no coverage / 0 timeout of 905), up from
> 79.01 % on the first run. The kill round added tests only; production code is unchanged since `f7b67946b`.

- Production code frozen at `f7b67946b` (`origin/main` at `12d51d141` is its ancestor; 85 Epic commits between).
- Tool: StrykerJS `@stryker-mutator/core@10` with `@stryker-mutator/vitest-runner`, from `Lighthouse.Frontend/`,
  `coverageAnalysis: "perTest"`, `inPlace: true`, `disableTypeChecks: false`, concurrency 8, incremental file on.
  Narrowed vitest config `vitest.stryker.6298.config.ts` (the story-6307 one with the include list swapped) runs 43
  specs: all 30 under `pages/Common/Reports/`, plus AuthPageLayout, Header, Footer, LighthouseLogo, LighthouseVersion,
  SnackbarErrorHandler, UsageDataService, usageDataEvents, usageDataRouteKeys.print/.reports, the Team and Portfolio
  `reportsTab` specs and `bundledTypeface.enforcement`.
- First run (at `bb0404856`): **79.01 %** (715 killed / 184 survived / 6 no coverage / 0 timeout of 905), 54 min 55 s
  (dry run 1 min 56 s, 597 tests; 54 tests per mutant on average).
- Kill round (2026-10-21, at `9110003c2`, production code identical to `f7b67946b`): **83.87 %** (759 killed /
  140 survived / 6 no coverage / 0 timeout of 905). **Gate ≥ 80 % met.** Measured by two incremental runs on the
  first run's incremental file: 24 min 45 s re-testing the 534 mutants the changed specs cover, then 16 min 26 s
  re-testing the 56 static survivors (see the trap under "Kill round").

## Scope

New files whole, pre-existing files by the new-side spans of `git diff -U0 origin/main..f7b67946b` (pure deletions
dropped). The config's `mutate` list is generated from the diff; 32 files matched and 905 mutants were instrumented.
Three scoped files produced no mutants: `ReportsTab.tsx` and `printReadPort.ts` (types, imports and JSX wiring only)
and `services/Api/UsageDataService.ts` (one enum-mirror member).

Left out, on purpose:

- `src/main.tsx`: the change is one font import plus moving the two themes out to `appThemes.ts` (import-only hunks).
- `src/App.tsx`: the shell is moved verbatim into an `AppShell` layout route and the existing route table re-nested
  under it; the only new behaviour is the `{reportPrintRoutes()}` call, which carries no mutant. Its string and
  object literals are pre-existing code that only moved.
- `utils/theme/appThemes.ts` lines 1-19 and 32-153: a verbatim move of the light and dark themes out of `main.tsx`.
  Only the shared `TYPOGRAPHY` (lines 20-31, the font family and heading weights that changed) is in scope.
- Test files, `src/tests/*` kits.

## Scores per file (after the kill round)

| file | mutants | killed | survived | no coverage | timeout | score | first run |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `components/App/Auth/AuthPageLayout.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % | 0.00 % |
| `components/App/LetPeopleWork/LighthouseLogo.tsx` | 14 | 8 | 6 | 0 | 0 | 57.14 % | 57.14 % |
| `components/App/LetPeopleWork/LighthouseVersion.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % | 0.00 % |
| `components/Common/SnackbarErrorHandler/SnackbarErrorHandler.tsx` | 35 | 29 | 6 | 0 | 0 | 82.86 % | 82.86 % |
| `pages/Common/Reports/panels/ChangeMarkIcon.tsx` | 12 | 10 | 2 | 0 | 0 | 83.33 % | 83.33 % |
| `pages/Common/Reports/panels/ReportPanels.tsx` | 7 | 7 | 0 | 0 | 0 | 100.00 % | 100.00 % |
| `pages/Common/Reports/panels/thenNowComparison.ts` | 11 | 11 | 0 | 0 | 0 | 100.00 % | 100.00 % |
| `pages/Common/Reports/panels/ThenNowLineParts.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % | 0.00 % |
| `pages/Common/Reports/panels/ThenNowPanelFrame.tsx` | 17 | 9 | 8 | 0 | 0 | 52.94 % | 52.94 % |
| `pages/Common/Reports/print/browserPrintEffects.ts` | 101 | 85 | 14 | 2 | 0 | 84.16 % | 76.24 % |
| `pages/Common/Reports/print/PrintFrameHost.tsx` | 93 | 74 | 17 | 2 | 0 | 79.57 % | 74.19 % |
| `pages/Common/Reports/print/printPageState.ts` | 82 | 81 | 1 | 0 | 0 | 98.78 % | 98.78 % |
| `pages/Common/Reports/print/PrintSheet.tsx` | 30 | 19 | 11 | 0 | 0 | 63.33 % | 53.33 % |
| `pages/Common/Reports/print/ReportPrintContent.tsx` | 139 | 123 | 16 | 0 | 0 | 88.49 % | 86.33 % |
| `pages/Common/Reports/print/ReportPrintDocument.tsx` | 58 | 49 | 9 | 0 | 0 | 84.48 % | 46.55 % |
| `pages/Common/Reports/print/ReportPrintPage.tsx` | 7 | 7 | 0 | 0 | 0 | 100.00 % | 100.00 % |
| `pages/Common/Reports/print/reportPrintRoutes.tsx` | 25 | 19 | 6 | 0 | 0 | 76.00 % | 76.00 % |
| `pages/Common/Reports/print/usePrintOnce.ts` | 51 | 46 | 5 | 0 | 0 | 90.20 % | 90.20 % |
| `pages/Common/Reports/ReportList.tsx` | 37 | 32 | 5 | 0 | 0 | 86.49 % | 83.78 % |
| `pages/Common/Reports/reportReadFailures.ts` | 22 | 20 | 2 | 0 | 0 | 90.91 % | 90.91 % |
| `pages/Common/Reports/ReportView.tsx` | 28 | 24 | 4 | 0 | 0 | 85.71 % | 85.71 % |
| `pages/Common/Reports/ThenAndNowHeader.tsx` | 14 | 8 | 4 | 2 | 0 | 57.14 % | 57.14 % |
| `pages/Common/Reports/ThenAndNowSummary.tsx` | 11 | 10 | 1 | 0 | 0 | 90.91 % | 90.91 % |
| `pages/Common/Reports/useReportOpenedReporter.ts` | 15 | 12 | 3 | 0 | 0 | 80.00 % | 80.00 % |
| `pages/Common/Reports/useReportPdfExport.ts` | 61 | 50 | 11 | 0 | 0 | 81.97 % | 78.69 % |
| `pages/Common/Reports/useReportRead.ts` | 3 | 2 | 1 | 0 | 0 | 66.67 % | 66.67 % |
| `pages/Common/Reports/wizard/reportRefusalCopy.ts` | 20 | 20 | 0 | 0 | 0 | 100.00 % | 100.00 % |
| `pages/Common/Reports/wizard/reportTypes.ts` | 4 | 4 | 0 | 0 | 0 | 100.00 % | 100.00 % |
| `utils/theme/appThemes.ts` | 5 | 0 | 5 | 0 | 0 | 0.00 % | 0.00 % |
| **all** | **905** | **759** | **140** | **6** | **0** | **83.87 %** | **79.01 %** |

## Kill round (2026-10-21)

Tests only, in four existing specs (so the narrowed vitest config's `include` list did not change). Every new
assertion was first checked by hand: the matching change made to the production line, the spec run red, the line
restored.

- `print/browserPrintEffects.test.tsx`
  - fonts answer failed when one face is found and another is not (`every` → `some`);
  - the frame's document keeps a page language holding `"`, `&` and `<` intact (the three escapes);
  - a style sheet that cannot be loaded is named in the refusal, `The style sheet …/index-3f9a1c.css could not be
    loaded` (both message templates);
  - a frame the browser cannot load is refused as `The print frame could not be loaded` and taken away;
  - a caller who gives up before the frame loads gets `The print frame was given up`, and the frame goes.
- `print/ReportPrintPage.runningHeader.test.tsx`
  - the running line stays one row: owner and report name cut with an ellipsis (`nowrap`, `hidden`, `ellipsis`),
    the windows and date never shrink, the parts sit in a flex row;
  - on paper the report is a full-width table with its body as a row group; the repeated room groups are hidden
    on screen, may not break, keep a gap (in mm) to the report; line and footer heights are real lengths in mm
    (blanking one turned it into `0%`, which the old `/\d/` check let through).
- `ReportView.exportPdf.test.tsx`
  - the frame's paper says `data-render-ready="false"` while preparing and `"true"` once printed;
  - the frame holds no link at all: no "Back to Reports", and the footer's Lighthouse mark is not a link home;
  - an export with the terms already in hand does not read them from the server again.
- `ReportView.exportPdf.failures.test.tsx`
  - choosing Export PDF again after a failure takes the old snackbar message away once the new export prints.

**Trap found: incremental mode never re-tests a static survivor when only tests changed.** A mutant in a
module-level constant (the `sx` objects in `ReportPrintDocument.tsx`, `PrintSheet.tsx`) is "static": Stryker runs
every test against it and records no covering tests. The incremental differ reuses a result unless a *covering*
test changed, so the first incremental run reused all 56 static survivors untouched, and the 16 that the new
running-line tests kill by hand still read "Survived". Fix used: drop the static survivors from the incremental
file (`jq '.files |= with_entries(.value.mutants |= map(select((.static==true and .status=="Survived")|not)))'`)
and run `--incremental` again; only those 56 are re-tested (16 min). 24 of them were then killed.

## Surviving mutants after the kill round (146 = 140 survived + 6 no coverage)

Grouped by reason. The first three groups are accepted; the last is left open with a note on each.

**Styling that only a real browser or real paper shows (accepted).** jsdom applies no `@media print` to a computed
style, lays nothing out, and MUI's `sx` turns some blanked values into other valid CSS; the print layout itself is
pinned by the hand probe and the end-to-end check.

- `ReportPrintDocument.tsx` L47-49 (running line's print-only `flex` / `column` / `flex-end`), L53 `baseline`, L61
  print font size `8pt` (object and string), L66-67 and L70 (footer `flex` / `center` / `divider` colour), L148
  divider `mt`.
- `PrintSheet.tsx` L13-16 (Back to Reports hidden on paper; body `flex` / `column`), L38-40 (`@media print`
  white ground, `printColorAdjust`, no animation or transition).
- `ThenNowPanelFrame.tsx` L43-44, L74-76, L84, L106 (grid columns and set-off per breakpoint, line height, width).
- `ThenAndNowHeader.tsx` L30-33 (disabled button colours and cursor), `ReportList.tsx` L42 (disabled icon colour
  and cursor), `LighthouseLogo.tsx` L19, L34-35 (flex alignment, bold), `ChangeMarkIcon.tsx` L45 (baseline nudge),
  `SnackbarErrorHandler.tsx` L200 (width).
- The typeface family strings: `appThemes.ts` L20-28, `AuthPageLayout.tsx` L57, `LighthouseVersion.tsx` L230.
  jsdom loads no font; `bundledTypeface.enforcement` pins that the family is bundled, not how a theme spells it.

**Text spacing and labels with no reader-facing effect (accepted).** `ThenAndNowSummary.tsx` L121 and
`ThenNowLineParts.tsx` L81 (a `" "` between inline elements whose readings the tests join by element), and
`ChangeMarkIcon.tsx` L16 (`PlusMinus` icon name passed to an aria-hidden icon).

**Equivalent mutants (accepted).**

- Effect dependency lists that hold constants or a value that never changes per mount (`PrintFrameHost.tsx` L60,
  L86, L111, L138, L145; `useReportPdfExport.ts` L58, L89; `usePrintOnce.ts` L96, L123; `reportPrintRoutes.tsx`
  L56, L77; `SnackbarErrorHandler.tsx` L124), and the "latest tell" ref updates (`PrintFrameHost.tsx` L58,
  `usePrintOnce.ts` L94): every `tell` handed in calls the same host for the same attempt.
- `PrintFrameHost.tsx` L142 (5 mutants: the clock is not stopped once the dialog is asked for). Once the page is
  ready, `printPageStateOf` stays `ready` when the time runs out, the frame is already open and the print-once
  rule has settled, so the late time-out changes nothing.
- Cleanups that only cancel a timer or a message on unmount (`PrintFrameHost.tsx` L137; `useReportPdfExport.ts`
  L89; `useReportPdfExport.ts` L68, since the snackbar shows one message at a time and the next `showError`
  replaces it anyway).
- `useReportPdfExport.ts` L93 (`attempts -= 1` still gives each frame a distinct key), L99 (a second choice while
  preparing: the button is `aria-disabled` and swallows the click before the hook sees it), L108-109 (a removal
  timer of 0 ms that always fires before the reader can start another attempt), L25 (a Type without a usage-data
  word never offers Export PDF, so the guard cannot be reached from the page).
- `browserPrintEffects.ts` L104, L108, L134 (`{ once: true }`: a promise settles once however often its event
  fires), L138 (`givenUp.catch`: the frame's own `Promise.race` already handles that rejection), L94 / L97 / L98
  (`left: 0`, `border: 0`, `pointer-events: none` on a frame that sits a whole sheet above the screen).
- `reportPrintRoutes.tsx` L29-30 (the route's lazy-loading fallback spinner, never shown because the tests render
  after the chunk is in).

**Not killed in this round, and not proven equivalent (open).** Judged low-value or reachable only through timing
or a browser jsdom does not imitate; each is a candidate for a later round rather than an accepted gap.

- `PrintFrameHost.tsx` L68-69 (no coverage: a frame that finishes opening after its host unmounted; in practice
  the unmount aborts the open first), L170 / L173-174 (the drawing guard's state shape: a report that breaks is
  caught, told, and the frame removed in the same turn either way).
- `browserPrintEffects.ts` L169-170 (no coverage for the throw: jsdom always gives a loaded frame a window and a
  document).
- `ReportPrintContent.tsx` L73, L80-81 (owner deleted between opening the report and printing it), L108-111 (a
  load answered after unmount), L118, L135, L219, L226 (guards whose other branch is overruled by
  `printPageStateOf` before anything prints), L248-250 (Retry on the print page re-asks every failed part at
  once; the tests cover each part failing alone).
- `ReportView.tsx` L62-70, `ReportList.tsx` L128, `useReportOpenedReporter.ts` L53, `ThenAndNowHeader.tsx` L58-59
  (no coverage): loading-versus-loaded guards and the once-per-opening counter, whose other branch renders the
  same thing for the one frame jsdom draws, and a header prop no current caller passes.
- `usePrintOnce.ts` L72, L106 (the "dialog opened" answer and its reset after a throw): the existing close-the-dialog
  scenarios did not see them flipped; worth a scenario where the dialog opens, closes, and the frame must go.
- `printPageState.ts` L45, `reportReadFailures.ts` L15 / L17, `useReportRead.ts` L20 (`?.` on an object the
  query client always has; retry-option guards whose other branch gives the same retry count with the app's
  `retry: false` client).
- `SnackbarErrorHandler.tsx` L114, L119, L202, L224 (`?.` on a value checked just before, ids that only need to
  differ, an initial `null` branch MUI renders the same, an empty key fallback).

## Re-running

The incremental file and reports live outside the repository (the scratch directory is tmpfs):
`/home/benjamin/.cache/lighthouse-mutation/epic-6298/stryker-incremental-6298.json`, `stryker-6298-frontend.json`
and `.html`, `vitest.stryker.6298.config.ts`, `stryker.config.json`, `run-killround*.log`; the first run's files are kept in
`first-run/` beside them. To run again: copy the vitest
config into `Lighthouse.Frontend/`, fill `<scratch>` in `stryker.6298.frontend.json` with a directory holding the
incremental file (`tempDirName` is now `.stryker-tmp-6298`, **inside `Lighthouse.Frontend/`**), and run
`pnpm exec stryker run <config> --incremental` from `Lighthouse.Frontend/`. A `tempDirName` on `/tmp` puts the
backup on another filesystem; the restore `rename` then fails with `EXDEV` and leaves `src/` instrumented. After
any run, `git status` must show no production file changed.

## Backend

Not run through Stryker.NET: the Epic's only backend production change appends `ReportPdfExportStarted = 20` to
`UsageDataEventName` and adds it to the set of events that must carry `report_template` in `UsageDataEventShapes`
(commit `48852f11d`); the publisher already wrote `report_template` and did not change. Both are pinned by tests.
`Integration/UsageData/ReportPdfExportUsageEventTests` posts the event through the real usage-data pipe and asserts the
collector receives exactly one message named `ReportPdfExportStarted` carrying the standard properties plus
`report_template = ThenAndNow` and nothing else (forwarding); refuses it without a type, with an unknown or mis-cased type,
or with any extra property (shape allowlist); still refuses a type on `ReportDeleted`, `TeamCreated` and
`WorkTrackingSystemFieldsChanged` (the allowlist grew by exactly one); and pins that the name comes after
`WorkTrackingSystemFieldsChanged` while every earlier name keeps its number 0-19 (enum value, append-only). The unit test
`Models/UsageData/UsageDataEventShapesTests` puts `ReportPdfExportStarted` beside `ReportCreated` and
`ReportOpened` as a template-carrying shape. Removing the member, renumbering, or dropping it from the template set each
reds at least one of these.

