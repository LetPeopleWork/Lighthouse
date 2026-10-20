# Mutation testing — Epic 6298 "Report PDF Export", Lighthouse frontend

> **First run only — kill round not yet done (session ended).** The score below is the untouched first run.
> No killing tests have been written yet; the survivor list at the end is the work list for the next session.

- Production code frozen at `f7b67946b` (`origin/main` at `12d51d141` is its ancestor; 85 Epic commits between).
- Tool: StrykerJS `@stryker-mutator/core@10` with `@stryker-mutator/vitest-runner`, from `Lighthouse.Frontend/`,
  `coverageAnalysis: "perTest"`, `inPlace: true`, `disableTypeChecks: false`, concurrency 8, incremental file on.
  Narrowed vitest config `vitest.stryker.6298.config.ts` (the story-6307 one with the include list swapped) runs 43
  specs: all 30 under `pages/Common/Reports/`, plus AuthPageLayout, Header, Footer, LighthouseLogo, LighthouseVersion,
  SnackbarErrorHandler, UsageDataService, usageDataEvents, usageDataRouteKeys.print/.reports, the Team and Portfolio
  `reportsTab` specs and `bundledTypeface.enforcement`.
- **Gate ≥ 80 %: first run 79.01 % (715 killed / 184 survived / 6 no coverage / 0 timeout of 905). Not yet met.**
- Wall clock: 54 min 55 s (dry run 1 min 56 s, 597 tests; 54 tests per mutant on average).

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

## Scores per file (first run)

| file | mutants | killed | survived | no coverage | timeout | score |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `components/App/Auth/AuthPageLayout.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % |
| `components/App/LetPeopleWork/LighthouseLogo.tsx` | 14 | 8 | 6 | 0 | 0 | 57.14 % |
| `components/App/LetPeopleWork/LighthouseVersion.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % |
| `components/Common/SnackbarErrorHandler/SnackbarErrorHandler.tsx` | 35 | 29 | 6 | 0 | 0 | 82.86 % |
| `pages/Common/Reports/ReportList.tsx` | 37 | 31 | 6 | 0 | 0 | 83.78 % |
| `pages/Common/Reports/ReportView.tsx` | 28 | 24 | 4 | 0 | 0 | 85.71 % |
| `pages/Common/Reports/ThenAndNowHeader.tsx` | 14 | 8 | 4 | 2 | 0 | 57.14 % |
| `pages/Common/Reports/ThenAndNowSummary.tsx` | 11 | 10 | 1 | 0 | 0 | 90.91 % |
| `pages/Common/Reports/panels/ChangeMarkIcon.tsx` | 12 | 10 | 2 | 0 | 0 | 83.33 % |
| `pages/Common/Reports/panels/ReportPanels.tsx` | 7 | 7 | 0 | 0 | 0 | 100.00 % |
| `pages/Common/Reports/panels/ThenNowLineParts.tsx` | 1 | 0 | 1 | 0 | 0 | 0.00 % |
| `pages/Common/Reports/panels/ThenNowPanelFrame.tsx` | 17 | 9 | 8 | 0 | 0 | 52.94 % |
| `pages/Common/Reports/panels/thenNowComparison.ts` | 11 | 11 | 0 | 0 | 0 | 100.00 % |
| `pages/Common/Reports/print/PrintFrameHost.tsx` | 93 | 69 | 22 | 2 | 0 | 74.19 % |
| `pages/Common/Reports/print/PrintSheet.tsx` | 30 | 16 | 14 | 0 | 0 | 53.33 % |
| `pages/Common/Reports/print/ReportPrintContent.tsx` | 139 | 120 | 19 | 0 | 0 | 86.33 % |
| `pages/Common/Reports/print/ReportPrintDocument.tsx` | 58 | 27 | 31 | 0 | 0 | 46.55 % |
| `pages/Common/Reports/print/ReportPrintPage.tsx` | 7 | 7 | 0 | 0 | 0 | 100.00 % |
| `pages/Common/Reports/print/browserPrintEffects.ts` | 101 | 77 | 22 | 2 | 0 | 76.24 % |
| `pages/Common/Reports/print/printPageState.ts` | 82 | 81 | 1 | 0 | 0 | 98.78 % |
| `pages/Common/Reports/print/reportPrintRoutes.tsx` | 25 | 19 | 6 | 0 | 0 | 76.00 % |
| `pages/Common/Reports/print/usePrintOnce.ts` | 51 | 46 | 5 | 0 | 0 | 90.20 % |
| `pages/Common/Reports/reportReadFailures.ts` | 22 | 20 | 2 | 0 | 0 | 90.91 % |
| `pages/Common/Reports/useReportOpenedReporter.ts` | 15 | 12 | 3 | 0 | 0 | 80.00 % |
| `pages/Common/Reports/useReportPdfExport.ts` | 61 | 48 | 13 | 0 | 0 | 78.69 % |
| `pages/Common/Reports/useReportRead.ts` | 3 | 2 | 1 | 0 | 0 | 66.67 % |
| `pages/Common/Reports/wizard/reportRefusalCopy.ts` | 20 | 20 | 0 | 0 | 0 | 100.00 % |
| `pages/Common/Reports/wizard/reportTypes.ts` | 4 | 4 | 0 | 0 | 0 | 100.00 % |
| `utils/theme/appThemes.ts` | 5 | 0 | 5 | 0 | 0 | 0.00 % |
| **all** | **905** | **715** | **184** | **6** | **0** | **79.01 %** |

## Resuming the kill round

The incremental file and the reports were written to the session scratch directory, which is on tmpfs and is gone after
a shutdown, so copies were kept outside the repository:

- `/home/benjamin/.cache/lighthouse-mutation/epic-6298/stryker-incremental-6298.json` (incremental file)
- `/home/benjamin/.cache/lighthouse-mutation/epic-6298/stryker-6298-frontend.json` and `.html` (mutation report)
- `/home/benjamin/.cache/lighthouse-mutation/epic-6298/vitest.stryker.6298.config.ts` (copy into `Lighthouse.Frontend/`)
- `/home/benjamin/.cache/lighthouse-mutation/epic-6298/stryker.config.json` and `run.log` (this run's exact config and log)

Original scratch paths: `/tmp/claude-1000/-storage-repos-Lighthouse/8cfcd098-4af0-43c3-b7ab-877262c28913/scratchpad/fe6298/stryker-incremental-6298.json`, `/tmp/claude-1000/-storage-repos-Lighthouse/8cfcd098-4af0-43c3-b7ab-877262c28913/scratchpad/fe6298/reports/stryker-6298-frontend.json`.

To resume: copy the vitest config into `Lighthouse.Frontend/`, set `<scratch>` in `stryker.6298.frontend.json` to a
directory holding the incremental file, write the killing tests, then
`pnpm exec stryker run <config> --incremental` from `Lighthouse.Frontend/`. Only mutants whose code or covering
tests changed are re-tested.

**Trap found in this run — fix before the next one.** With `inPlace: true`, Stryker backs the originals up under
`tempDirName` and restores them with a `rename`. A `tempDirName` on `/tmp` (tmpfs) sits on a different filesystem
from `/storage`, so the restore failed with `EXDEV: cross-device link not permitted`, the run exited 1 after writing
its reports, and all 32 files were left instrumented in `src/`. They were restored with
`git checkout -- Lighthouse.Frontend/src` (the code is frozen at HEAD, so nothing else was in them). Next run: point
`tempDirName` at `.stryker-tmp-6298` inside `Lighthouse.Frontend/` (gitignored, same filesystem), and keep only
the incremental and report files in the scratch directory.

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

## Surviving mutants (first run, 190 = 184 survived + 6 no coverage)

Not yet triaged. Grouped by file; `file:line mutator: original → replacement` (long fragments cut with …).

### `components/App/Auth/AuthPageLayout.tsx`

- L57 StringLiteral: `"'Quicksand Variable', sans-serif"` → `""`

### `components/App/LetPeopleWork/LighthouseLogo.tsx`

- L14 BooleanLiteral: `true` → `false`
- L19 StringLiteral: `"center"` → `""`
- L19 StringLiteral: `"flex"` → `""`
- L19 ObjectLiteral: `{ display: "flex", alignItems: "center" }` → `{}`
- L34 StringLiteral: `"bold"` → `""`
- L35 StringLiteral: `"flex"` → `""`

### `components/App/LetPeopleWork/LighthouseVersion.tsx`

- L230 StringLiteral: `"'Quicksand Variable', sans-serif"` → `""`

### `components/Common/SnackbarErrorHandler/SnackbarErrorHandler.tsx`

- L114 OptionalChaining: `current?.id` → `current.id`
- L119 AssignmentOperator: `lastId.current += 1` → `lastId.current -= 1`
- L124 ArrayDeclaration: `[closeIfShowing]` → `[]`
- L200 StringLiteral: `"100%"` → `""`
- L202 ConditionalExpression: `shown === null` → `false`
- L224 StringLiteral: `""` → `"Stryker was here!"`

### `pages/Common/Reports/ReportList.tsx`

- L128 EqualityOperator: `actions.editor === undefined` → `actions.editor !== undefined`
- L128 ConditionalExpression: `actions.editor === undefined` → `true`
- L128 ConditionalExpression: `actions.editor === undefined` → `false`
- L42 ObjectLiteral: `{ color: "action.disabled", cursor: "default" }` → `{}`
- L42 StringLiteral: `"default"` → `""`
- L42 StringLiteral: `"action.disabled"` → `""`

### `pages/Common/Reports/ReportView.tsx`

- L62 ConditionalExpression: `report === undefined` → `true`
- L70 ConditionalExpression: `report !== undefined` → `true`
- L65 LogicalOperator: `report !== undefined && …` → `report !== undefined \|\| !isError`
- L65 ConditionalExpression: `report !== undefined` → `true`

### `pages/Common/Reports/ThenAndNowHeader.tsx`

- L58 BooleanLiteral (no coverage): `false` → `true`
- L59 BooleanLiteral (no coverage): `false` → `true`
- L30 ObjectLiteral: `{ …` → `{}`
- L31 StringLiteral: `"action.disabled"` → `""`
- L32 StringLiteral: `"action.disabledBackground"` → `""`
- L33 StringLiteral: `"default"` → `""`

### `pages/Common/Reports/ThenAndNowSummary.tsx`

- L121 StringLiteral: `" "` → `""`

### `pages/Common/Reports/panels/ChangeMarkIcon.tsx`

- L45 StringLiteral: `"-0.125em"` → `""`
- L16 StringLiteral: `"PlusMinus"` → `""`

### `pages/Common/Reports/panels/ThenNowLineParts.tsx`

- L81 StringLiteral: `" "` → `""`

### `pages/Common/Reports/panels/ThenNowPanelFrame.tsx`

- L43 StringLiteral: `"grid"` → `""`
- L44 ObjectLiteral: `{ xs: "1fr", md: sideBySide }` → `{}`
- L44 StringLiteral: `"1fr"` → `""`
- L74 ObjectLiteral: `{ md: setOff.pl }` → `{}`
- L75 ObjectLiteral: `{ md: setOff.borderLeftWidth }` → `{}`
- L76 ObjectLiteral: `{ md: setOff.borderLeftStyle }` → `{}`
- L84 ObjectLiteral: `{ lineHeight: 1.5 }` → `{}`
- L106 StringLiteral: `"100%"` → `""`

### `pages/Common/Reports/print/PrintFrameHost.tsx`

- L68 BlockStatement (no coverage): `{ …` → `{}`
- L69 CallExpression (no coverage): `frame.close();` → `;`
- L58 BlockStatement: `{ …` → `{}`
- L60 ArrayDeclaration: `[tell]` → `[]`
- L68 ConditionalExpression: `gone` → `false`
- L86 ArrayDeclaration: `[]` → `["Stryker was here"]`
- L110 ConditionalExpression: `error !== null` → `true`
- L111 ArrayDeclaration: `[]` → `["Stryker was here"]`
- L137 ArrowFunction: `() => clearTimeout(clock.current)` → `() => undefined`
- L142 ConditionalExpression: `outcome.kind === "printing"` → `true`
- L138 ArrayDeclaration: `[]` → `["Stryker was here"]`
- L142 ConditionalExpression: `outcome.kind === "printing"` → `false`
- L142 EqualityOperator: `outcome.kind === "printing"` → `outcome.kind !== "printing"`
- L142 CallExpression: `clearTimeout(clock.current);` → `;`
- L142 StringLiteral: `"printing"` → `""`
- L173 BlockStatement: `{ …` → `{}`
- L174 ObjectLiteral: `{ broken: true }` → `{}`
- L174 BooleanLiteral: `true` → `false`
- L145 ArrayDeclaration: `[tell]` → `[]`
- L170 ObjectLiteral: `{ broken: false }` → `{}`
- L205 ConditionalExpression: `look.state === "ready"` → `true`
- L205 EqualityOperator: `look.state === "ready"` → `look.state !== "ready"`
- L205 ConditionalExpression: `look.state === "ready"` → `false`
- L205 StringLiteral: `"ready"` → `""`

### `pages/Common/Reports/print/PrintSheet.tsx`

- L62 ConditionalExpression: `reportsPath === undefined` → `false`
- L13 ObjectLiteral: `{ "@media print": { display: "none" } }` → `{}`
- L13 ObjectLiteral: `{ display: "none" }` → `{}`
- L13 StringLiteral: `"none"` → `""`
- L15 StringLiteral: `"flex"` → `""`
- L14 ObjectLiteral: `{ …` → `{}`
- L16 StringLiteral: `"column"` → `""`
- L39 StringLiteral: `"#fff"` → `""`
- L39 StringLiteral: `"exact"` → `""`
- L40 ObjectLiteral: `{ animation: "none", transition: "none" }` → `{}`
- L40 StringLiteral: `"none"` → `""`
- L40 StringLiteral: `"none"` → `""`
- L39 ObjectLiteral: `{ backgroundColor: "#fff", printColorAdjust: "exact" }` → `{}`
- L38 ObjectLiteral: `{ …` → `{}`

### `pages/Common/Reports/print/ReportPrintContent.tsx`

- L80 BlockStatement: `{ …` → `{}`
- L81 StringLiteral: `'The ${owner.kind} ${owner.id} was not found'` → `''`
- L73 ArrayDeclaration: `["reportPrintOwner", owner.kind, owner.id]` → `[]`
- L73 StringLiteral: `"reportPrintOwner"` → `""`
- L80 ConditionalExpression: `found === null \|\| found === undefined` → `false`
- L80 LogicalOperator: `found === null \|\| found === undefined` → `found === null && found === undefined`
- L80 ConditionalExpression: `found === null` → `false`
- L118 ConditionalExpression: `state === "failed"` → `true`
- L108 ConditionalExpression: `!gone` → `true`
- L110 BlockStatement: `{ …` → `{}`
- L111 BooleanLiteral: `true` → `false`
- L135 ConditionalExpression: `typeof ownerName !== "string"` → `false`
- L145 ConditionalExpression: `title === undefined` → `false`
- L248 ConditionalExpression: `ownerName.isError` → `true`
- L249 ConditionalExpression: `terms.error !== null` → `true`
- L226 ConditionalExpression: `fonts.state === "loaded"` → `true`
- L219 ConditionalExpression: `report.data === undefined` → `false`
- L250 CallExpression: `fonts.askAgainIfFailed();` → `;`
- L226 LogicalOperator: `drawn && fonts.state === "loaded"` → `drawn \|\| fonts.state === "loaded"`

### `pages/Common/Reports/print/ReportPrintDocument.tsx`

- L148 ObjectLiteral: `{ mt: 0.5 }` → `{}`
- L169 BooleanLiteral: `false` → `true`
- L25 StringLiteral: `"10mm"` → `""`
- L26 StringLiteral: `"16mm"` → `""`
- L27 StringLiteral: `"4mm"` → `""`
- L47 StringLiteral: `"flex"` → `""`
- L48 StringLiteral: `"column"` → `""`
- L49 StringLiteral: `"flex-end"` → `""`
- L53 ObjectLiteral: `{ display: "flex", alignItems: "baseline", gap: 3 }` → `{}`
- L53 StringLiteral: `"baseline"` → `""`
- L56 ObjectLiteral: `{ …` → `{}`
- L57 StringLiteral: `"nowrap"` → `""`
- L58 StringLiteral: `"hidden"` → `""`
- L59 StringLiteral: `"ellipsis"` → `""`
- L61 ObjectLiteral: `{ fontSize: "8pt" }` → `{}`
- L61 StringLiteral: `"8pt"` → `""`
- L63 ObjectLiteral: `{ ...NAME_PART, flexShrink: 0 }` → `{}`
- L66 StringLiteral: `"flex"` → `""`
- L67 StringLiteral: `"center"` → `""`
- L70 StringLiteral: `"divider"` → `""`
- L74 ObjectLiteral: `{ "@media print": { display: "table", width: "100%" } }` → `{}`
- L74 ObjectLiteral: `{ display: "table", width: "100%" }` → `{}`
- L74 StringLiteral: `"table"` → `""`
- L74 StringLiteral: `"100%"` → `""`
- L75 ObjectLiteral: `{ "@media print": { display: "table-row-group" } }` → `{}`
- L75 ObjectLiteral: `{ display: "table-row-group" }` → `{}`
- L75 StringLiteral: `"table-row-group"` → `""`
- L85 StringLiteral: `"none"` → `""`
- L86 StringLiteral: `"avoid"` → `""`
- L91 ObjectLiteral: `{ …` → `{}`
- L94 ObjectLiteral: `{ …` → `{}`

### `pages/Common/Reports/print/browserPrintEffects.ts`

- L169 BlockStatement (no coverage): `{ …` → `{}`
- L170 StringLiteral (no coverage): `"The print frame has no document"` → `""`
- L24 MethodExpression: `found.every((matches) => matches.length > 0)` → `found.some(matches => matches.length > 0)`
- L81 StringLiteral: `"&amp;"` → `""`
- L82 StringLiteral: `"&quot;"` → `""`
- L83 StringLiteral: `"&lt;"` → `""`
- L107 StringLiteral: `'${what} could not be loaded'` → `''`
- L94 StringLiteral: `"0"` → `""`
- L97 StringLiteral: `"0"` → `""`
- L98 StringLiteral: `"none"` → `""`
- L104 ObjectLiteral: `{ once: true }` → `{}`
- L104 BooleanLiteral: `true` → `false`
- L122 StringLiteral: `'The style sheet ${sheet.href}'` → `''`
- L108 ObjectLiteral: `{ once: true }` → `{}`
- L108 BooleanLiteral: `true` → `false`
- L133 StringLiteral: `"The print frame was given up"` → `""`
- L134 BooleanLiteral: `true` → `false`
- L134 ObjectLiteral: `{ once: true }` → `{}`
- L138 CallExpression: `givenUp.catch(() => undefined);` → `;`
- L163 StringLiteral: `"The print frame"` → `""`
- L169 ConditionalExpression: `frameWindow === null \|\| frameDocument === null` → `false`
- L169 LogicalOperator: `frameWindow === null \|\| frameDocument === null` → `frameWindow === null && frameDocument === null`
- L169 ConditionalExpression: `frameWindow === null` → `false`
- L169 ConditionalExpression: `frameDocument === null` → `false`

### `pages/Common/Reports/print/printPageState.ts`

- L45 ConditionalExpression: `error !== undefined` → `true`

### `pages/Common/Reports/print/reportPrintRoutes.tsx`

- L30 ObjectLiteral: `{ display: "flex", justifyContent: "center", py: 8 }` → `{}`
- L30 StringLiteral: `"flex"` → `""`
- L30 StringLiteral: `"center"` → `""`
- L77 ArrayDeclaration: `[loadFonts, loadPicture]` → `[]`
- L56 ArrayDeclaration: `[reportService, teamService, portfolioService]` → `[]`
- L29 ArrowFunction: `() => ( …` → `() => undefined`

### `pages/Common/Reports/print/usePrintOnce.ts`

- L72 BooleanLiteral: `false` → `true`
- L94 BlockStatement: `{ …` → `{}`
- L106 BooleanLiteral: `false` → `true`
- L96 ArrayDeclaration: `[tell]` → `[]`
- L123 ArrayDeclaration: `[port]` → `[]`

### `pages/Common/Reports/reportReadFailures.ts`

- L15 ConditionalExpression: `typeof usualRetry === "number"` → `true`
- L17 ConditionalExpression: `usualRetry === true` → `false`

### `pages/Common/Reports/useReportOpenedReporter.ts`

- L53 CallExpression: `counted.add(opening);` → `;`
- L53 ConditionalExpression: `sent` → `true`
- L53 ConditionalExpression: `sent` → `false`

### `pages/Common/Reports/useReportPdfExport.ts`

- L25 ConditionalExpression: `template !== undefined` → `true`
- L60 BlockStatement: `{ …` → `{}`
- L68 CallExpression: `takeBack();` → `;`
- L93 AssignmentOperator: `attempts.current += 1` → `attempts.current -= 1`
- L58 ArrayDeclaration: `[]` → `["Stryker was here"]`
- L99 ConditionalExpression: `preparingId !== undefined` → `false`
- L100 CallExpression: `message.takeBack();` → `;`
- L108 ArrowFunction: `(current) => …` → `() => undefined`
- L109 ConditionalExpression: `current?.attempt === attempt` → `true`
- L89 ArrowFunction: `() => () => clearTimeout(frameRemoval.current)` → `() => undefined`
- L109 OptionalChaining: `current?.attempt` → `current.attempt`
- L89 ArrowFunction: `() => clearTimeout(frameRemoval.current)` → `() => undefined`
- L89 ArrayDeclaration: `[]` → `["Stryker was here"]`

### `pages/Common/Reports/useReportRead.ts`

- L20 OptionalChaining: `useQueryClient().getDefaultOptions().queries?.retry` → `useQueryClient().getDefaultOptions().queries.retry`

### `utils/theme/appThemes.ts`

- L20 ObjectLiteral: `{ …` → `{}`
- L21 StringLiteral: `"'Quicksand Variable', 'Roboto', 'Arial', sans-serif"` → `""`
- L22 ObjectLiteral: `{ …` → `{}`
- L25 ObjectLiteral: `{ …` → `{}`
- L28 ObjectLiteral: `{ …` → `{}`
