# Mutation testing: 6435 (Epic 6350 slice 07: several reports in one PDF)

Run 2026-10-31 against local `main` @ `89692e5b0` (production code frozen), frontend only. The slice changed no
backend code. Gate: 80 % kill rate on the lines slice 07 changed.

The changed lines come from `git diff -U0 9385b8f85..HEAD` over `Lighthouse.Frontend/src`, test files left out. The
slice 06 commit in that range (`2f6bebb3e`) touched only docs. `2e168ac19` (ActionButton leaves its state alone once
it is gone) is a standalone fix that landed before slice 06's mutation commit, so it is left out. It also could not
be run: see the dry run below.

## Frontend (StrykerJS 10.0.0)

Config `stryker.6435.frontend.json` and `vitest.stryker.6435.mutation.ts`. As in 6433, the vitest config is committed
here and copied into `Lighthouse.Frontend/`, where it is gitignored. The Stryker config runs from where it sits here.
Stryker 10.1.1 is on `origin/main` but not on local `main`, so this ran on the installed 10.0.0.

Settings:
- `perTest`.
- `--incremental`, with the file `reports/mutation/stryker-incremental-6435.json` (gitignored).
- Concurrency 2.
- `inPlace`.
- `disableTypeChecks: false`.
- `TZ=Europe/Zurich`.

`mutate`:
- New files, whole: `bundleState.ts`, `ReportPrintBundle.tsx`, `ReportsPrintPage.tsx`, `printedReportsQuery.ts`,
  `CreditFooter.tsx`, `usePrintFrameAttempts.ts`, `preparingLook.tsx`, `NoPdfDialog.tsx`, `useReportsPdfExport.ts`,
  `useRowPicks.ts`, `useSelectionActions.tsx`, `useBulkCreation.tsx`. A refactor moved `useBulkCreation` and
  `useRowPicks` out of `ReportsPage.tsx` unchanged, but they are included anyway.
- Edited files, one `start-end` entry per added hunk: `DataGridBase.tsx`, `ReportList.tsx`, `ThenAndNowHeader.tsx`,
  `PrintFrameHost.tsx`, `ReportPrintDocument.tsx`, `ReportPrintPage.tsx`, `reportPrintRoutes.tsx`, `usePrintOnce.ts`,
  `useReportPdfExport.ts`, `ReportsPage.tsx`.

So every score here is a changed-line score.

Skipped:
- `types.ts`: two type-only lines on `RowPicking`.
- `UsageDataService.ts`: one entry, `ReportCombinedPdfExportStarted`, in an `as const` object. The usage-data test
  that expects the event by name would kill it.
- `ThenAndNowHeader.tsx:50` and `ReportPrintPage.tsx:44` produced no mutant. One spreads props into a button, and the
  other calls `withTheCredit`.

The vitest include:
- `src/pages/Reports/ReportsPage*.test.tsx`
- `src/pages/Reports/BulkReportWizard*.test.tsx` (these open the wizard from the Reports page, through
  `useBulkCreation`)
- `src/pages/Common/Reports/print/*.test.{ts,tsx}`
- `ReportList.test.tsx`, `ReportsTab.exportPdf.test.tsx`, `ReportView.exportPdf*.test.tsx`
- `src/components/Common/DataGrid/DataGridBase*.test.tsx`

`*.enforcement.test.ts` is excluded. Those suites read source text off disk, which in-place mutation rewrites.

### Dry run, and run times

The first attempt also mutated `ActionButton.tsx` and included `ActionButton.test.tsx`. The dry run crashed after
9 m 36 s with `TypeError: Cannot convert object to primitive value` inside Stryker's `errorToString`. A plain vitest
run of the same include named the cause: `ActionButton.test.tsx` ("is ready to be pressed again after its handler
fails") leaves an `ExpectedTestFailure` rejection unhandled. That is the same reason 6431, 6432 and 6433 left it out.
It was dropped, along with the two `ActionButton.tsx` entries.

The second run took **87 m 41 s**. The initial test run took 4 m 09 s (434 tests). The run was far over the
~40-minute budget because another session's vitest run held about ten cores for most of it. Without coverage the same
include runs in 30 s. No Stryker re-run was made after the kill round. Every kill below was checked instead by
applying the mutant by hand (`sed`) and running the killing test: it fails with the mutant and passes without it.

### Scores

| file | first run | after kill round (hand-verified) |
| --- | --- | --- |
| `pages/Common/Reports/print/bundleState.ts` (new) | 41 / 41 | 41 / 41 |
| `pages/Reports/useReportsPdfExport.ts` (new) | 76 / 92 | **84 / 92 (91.30 %)** |
| `pages/Reports/useBulkCreation.tsx` (new, moved) | 69 / 75 | 69 / 75 (92.00 %) |
| `pages/Common/Reports/print/ReportPrintBundle.tsx` (new) | 56 / 71 | **61 / 71 (85.92 %)** |
| `pages/Reports/useSelectionActions.tsx` (new) | 62 / 65 | **63 / 65 (96.92 %)** |
| `pages/Reports/useRowPicks.ts` (new, moved) | 25 / 31 | **26 / 31 (83.87 %)** |
| `pages/Common/Reports/print/printedReportsQuery.ts` (new) | 22 / 27 | **25 / 27 (92.59 %)** |
| `pages/Common/Reports/print/ReportsPrintPage.tsx` (new) | 12 / 16 | **13 / 16 (81.25 %)** |
| `pages/Reports/NoPdfDialog.tsx` (new) | 14 / 15 | 14 / 15 (93.33 %) |
| `pages/Common/Reports/usePrintFrameAttempts.ts` (new) | 8 / 15 | 8 / 15 (53.33 %) |
| `pages/Common/Reports/print/CreditFooter.tsx` (new) | 7 / 8 | **8 / 8** |
| `pages/Common/Reports/preparingLook.tsx` (new) | 2 / 6 | 2 / 6 (33.33 %) |
| `pages/Common/Reports/useReportPdfExport.ts` | 15 / 16 | 15 / 16 (93.75 %) |
| `pages/Common/Reports/print/ReportPrintDocument.tsx` | 13 / 16 | 13 / 16 (81.25 %) |
| `components/Common/DataGrid/DataGridBase.tsx` | 8 / 12 | 8 / 12 (66.67 %) |
| `pages/Common/Reports/ReportList.tsx` | 8 / 8 | 8 / 8 |
| `pages/Common/Reports/print/PrintFrameHost.tsx` | 7 / 8 | 7 / 8 (87.50 %) |
| `pages/Common/Reports/print/usePrintOnce.ts` | 4 / 5 | 4 / 5 (80.00 %) |
| `pages/Reports/ReportsPage.tsx` | 2 / 2 | 2 / 2 |
| `pages/Common/Reports/print/reportPrintRoutes.tsx` | 1 / 1 | 1 / 1 |
| **total** | 452 / 530 = **85.28 %** | **472 / 530 = 89.06 %** |

The first run already met the gate. The denominators include the one `NoCoverage` mutant (`DataGridBase.tsx:159`).

`ReportsPrintPage.tsx:11` (`NOTHING_NAMED` blanked) is a **false survivor**. Applied by hand, it fails the three
"says so when … names no report it can read" cases. It still counts as a survivor in the totals above. Counting it
as killed gives 473 / 530 = 89.25 %.

### Kill round (nine tests, 20 mutants)

- `ReportsPage.combinedPdf.test.tsx`:
  - Leave it out leaves out only the report that failed, not the other report of the same Team. Kills four
    `useReportsPdfExport.ts:48` mutants: the report-id half of `isTheSameReport` turned into `true`, the whole match
    into `true`, and either `&&` into `||`. Every earlier test failed reports on Teams of their own, so matching by
    owner alone looked the same.
  - Cancel closes the word that no PDF was made, prints nothing and keeps what was picked. Kills `:127`, `onCancel`
    emptied, which no test pressed before.
  - Once the print dialog closes the frame goes, Export PDF is ready again, and nothing says a PDF failed. Kills
    `:101` (`setPreparing(false)` into `true`), `:103` (the frame is never removed) and `:104` (a closed dialog read
    as a failure).
  - Exactly 50 picked may still be exported as one PDF. Kills `useSelectionActions.tsx:96`, `>` into `>=`. The
    existing test picked 51.
  - Leave it out unpicks every report that failed (two of three fail). Kills `useRowPicks.ts:25`, `some` into
    `every`, which unpicks only when a single report failed.
- `ReportsPrintPage.test.tsx`:
  - Starts every report after the first on a page of its own. It reads `break-before` through `whatPrintAsksOf`.
    Kills the three `ReportPrintBundle.tsx:126` mutants (`index === 0` into `true`, `false` and `!==`) and both
    `:28` mutants (`ON_A_PAGE_OF_ITS_OWN` emptied, and `"page"` blanked). The test that said "each on a page of its
    own" checked only the titles.
  - Is never ready to print when it names no report. Kills `ReportsPrintPage.tsx:35`, `ready={false}` into `true`.
    That mutant would let a headless renderer print the "nothing named" sheet.
  - Reads ids of several digits, and leaves out a part with anything before or after its report. Kills the three
    `printedReportsQuery.ts:3` regex mutants (`^` dropped, `$` dropped, `\d+` into `\d` for the owner id). Every
    earlier address used one-digit owner ids.
- `ReportPrintPage.lighthouseMark.test.tsx`: leaves the credit off a page that could not be made. Kills
  `CreditFooter.tsx:50`, `startsWith("failed")` into `endsWith("failed")`. No print state ends in "failed"
  (`failedRetryable`, `failedPart`, `failedFinal`), so that mutant would show the credit on every failed page.

### Accepted survivors (58)

Layout that jsdom does not compute (9):
- `preparingLook.tsx:5`–`:8` (four): the `PREPARING_LOOK` colours and cursor. What preparing does is pinned
  (`aria-disabled`, `aria-busy`, the spinner, the ignored click).
- `useSelectionActions.tsx:22` (two): the `HELD_BACK_LOOK` colour and cursor. Same reasoning.
- `ReportPrintDocument.tsx:35` (`boxSizing`), `:64` (`flexDirection`) and `:133` (a `Divider`'s `mt`).

Equivalent (45):
- `ReportPrintBundle.tsx:23` (two) and `:26` (two): the `failure: { retry: false }` objects of the preparing and
  ready looks, emptied or set to `true`. A look's failure is read only when its state is a failed one.
- `useReportsPdfExport.ts:49`, `:50`: the owner-kind and owner-id halves of `isTheSameReport` turned into `true`.
  Report ids come from one `Reports` table with one identity column, so no two reports of different owners share an
  id. The id half alone decides.
- `useReportsPdfExport.ts:84`, `:89` (four): the `isPreparing` and nothing-picked guards in `exportOne` and
  `exportPdf`. Through the page, Export PDF and a row's icon carry no `onClick` while a PDF is prepared
  (`exportPdfButtonProps`), and Export PDF is disabled with nothing picked. The guards cannot be reached; they stay as
  defence.
- `useReportsPdfExport.ts:107`: `outcome.failedReports ?? []` turned into a one-element array. A failure without
  `failedReports` is a part failure. The stray element matches no report, because `report.id === undefined` is false
  first, so `failed` is empty either way.
- `useReportsPdfExport.ts:132` and `useReportPdfExport.ts:135`: the `[]` used when no export is under way. It is read
  only when a frame is drawn, which needs an export.
- `ReportPrintBundle.tsx:71`: `failedReports.length === 0` turned into `false`. The look then carries
  `failedReports: []` instead of none, and both sides read that as no report failed (`?? []`).
- `ReportPrintBundle.tsx:109`: the several-report `setTitle` stand-in returns `undefined` instead of a no-op
  clean-up. React accepts either.
- `ReportPrintBundle.tsx:110`, `:115`, `:120`, `ReportsPrintPage.tsx:31`, `PrintFrameHost.tsx:143` and
  `DataGridBase.tsx:163`: hook dependency arrays emptied or padded. Each lists values that do not change while the
  component is mounted (a report list fixed by its address, a constant budget, `apiRef` and a ref object).
- `ReportsPrintPage.tsx:12`: `REPORTS_PATH` blanked. It is the back link of a page no reader sees: only a headless
  renderer opens it.
- `printedReportsQuery.ts:24`: `query ?? ""` turned into another string. It holds no report, so it is left out.
- `printedReportsQuery.ts:28`: `!named.has(key)` turned into `true`. A repeat overwrites an equal value, and a `Map`
  keeps the first insertion's place, so the result is the same.
- `usePrintOnce.ts:117`: always spreading `failedReports`, possibly as `undefined`. Every reader treats the
  `undefined` property as absent.
- `NoPdfDialog.tsx:56`: the list item's `key` blanked. React warns, and still draws every item.
- `usePrintFrameAttempts.ts` (seven):
  - `:20` (three): the clear-on-unmount and its dependencies. A timer that outlives the page sets state on an
    unmounted component, which React ignores.
  - `:23`: `+= 1` into `-= 1`. Attempts only need to be distinct.
  - `:30` (three): the "unless a newer attempt has taken its place" guard (`true`, the arrow emptied, `?.` dropped).
    The removal fires a turn after the outcome, and a fresh attempt needs a click, which comes later. The guard is
    defence against a race no reader can produce.
- `DataGridBase.tsx:159` (two, one `NoCoverage`) and `:160`: `apiRef.current?.` and its `?? []`, and the reader
  cleared on unmount. The reader is called only when Export PDF is pressed, with the grid mounted, and the grid
  unmounts with the page.
- `useRowPicks.ts` (four):
  - `:9` and `:22`: picks started or cleared as `["Stryker was here"]`. An id no row has finds no report, so it reads
    as nothing picked. This is 6433's reasoning, which moved here with the hook.
  - `:12`: `reports?.find` into `reports.find`. Nobody can pick before the list has loaded.
  - `:15`: `tableOrder.current?.()` into `.current()`. The grid sets the reader whenever it is mounted.
- `useBulkCreation.tsx` (five), moved unchanged from `ReportsPage.tsx`:
  - `:21`, `:22`: `?.length` into `.length`. The RBAC summary always carries both lists.
  - `:35`, `:47`: `retrying` started, or left after close, as `true`. `open()` sets it back to `false` before the
    wizard can show.
  - `:43`: `+= 1` into `-= 1`. Openings only need to be distinct and non-null.

Left open (4):
- `ReportPrintBundle.tsx:100`: `timedOut = false` into `true`. `ReportsPrintPage` never passes `timedOut`, so the
  default decides. With `true`, a report still on its way would count as timed out. Every test's report arrives at
  once. A `getReport` that never answers on the address-opened page would kill it.
- `useBulkCreation.tsx:58`: one operand of `opening !== null && (arrived || options.isError || retrying)` turned into
  `true`. It probably shows the wizard before its options arrive, and no test pins the wait before the wizard
  through the page. Moved unchanged from `ReportsPage.tsx`.
- `useRowPicks.ts:12`: `?? []` turned into a one-element array. That counts a pick whose report has left the list.
  6433 left this one open too (`ReportsPage.tsx:172` then).
- `ReportsPrintPage.tsx:11`: the false survivor above. The existing suite kills it.

No survivor revealed a bug.

## Commands

From `Lighthouse.Frontend/`, after
`cp ../docs/feature/epic-6350-bulk-reports/mutation/vitest.stryker.6435.mutation.ts .`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6435.frontend.json --incremental
```
