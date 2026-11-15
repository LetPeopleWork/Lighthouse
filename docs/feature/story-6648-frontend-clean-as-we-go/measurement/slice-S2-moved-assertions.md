# Slice S2 - combined PDF of picked reports: moved assertions

The scripts `measure-slice.sh`, `check-slice.sh` and `deliberate-breaks.sh` read this file by slice id. Ids are
`file :: full test name`, files relative to `Lighthouse.Frontend`.

## Measure

- Before commit: f9cfb6b61936f70fb532b2432e1d80d839ad9b21
- Page test file: src/pages/Reports/ReportsPage.combinedPdf.test.tsx
- Direct test files: src/pages/Reports/NoPdfDialog.test.tsx src/pages/Reports/printFailures.test.ts
- File-alone ratio at most: 0.75
- Combined ratio at most: 0.80
- Production allow-list: none

One row per test of the page file at the before commit. A kept test keeps its id; a removed or merged test names the
tests that now prove its rule, separated by `;;`. The direct test files list every test they hold under "Added ids".
The two rules of the failure dialog's lists and the Leave it out decision moved into `printFailures.ts` before the
before commit, so no production file is allowed to change in this slice.

## Moved assertions

| Old id | Disposition | Rule proved | New tests |
|---|---|---|---|
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports a coach exports the reports of two Teams as one PDF, each on a page of its own | removed | two Teams' reports print as one PDF, one title per report, in the table's order (merged into the kept Admin-of-none test) | src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports a reader who is Admin of none of them may still export them |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports once the print dialog closes the frame goes, Export PDF is ready again, and nothing says a PDF failed | kept | the frame goes after the dialog closes, Export PDF is not busy and no failure dialog shows | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports the reports print in the table's order, as the reader sorted it | kept | the PDF follows the table's sort order | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports nothing prints while one of the reports is still on its way, and the PDF holds both once it arrives | kept | nothing prints before every report has arrived, then one print holds all | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports picks and sorting changed while the PDF is prepared leave it as it was when Export PDF was pressed | kept | the PDF keeps the picks and the order of the moment Export PDF was pressed | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports while a PDF of several is prepared, a row's PDF icon is held back and starts no second | kept | a second PDF starts neither from a row's icon while several are prepared nor from Export PDF while a row's PDF is prepared (the other held-back test merged in) | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports while a row's PDF is prepared, Export PDF is held back and starts no second | removed | Export PDF is busy and starts no second PDF while a row's PDF is prepared (merged into the kept held-back test) | src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports while a PDF of several is prepared, a row's PDF icon is held back and starts no second |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports while the reports load, Export PDF shows it is preparing and nothing prints yet | kept | Export PDF shows the preparing look and nothing prints while reports load | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports a reader who is Admin of none of them may still export them | kept | a reader who is Admin of none exports the reports of two Teams as one PDF, in the table's order (the coach test merged in) | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports a report that cannot be loaded stops the PDF and is named | kept | a failed report is named, the one that loaded is not, nothing prints, and Cancel closes the dialog, prints nothing and keeps the picks (the Cancel test merged in); the dialog's own list, description and buttons are proved on the dialog alone | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Leave it out unpicks the report that failed and prints the rest | removed | Leave it out unpicks the failed report and prints the rest in table order (merged into the kept Leave it out test, which fails two reports) | src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Leave it out unpicks every report that failed ;; src/pages/Reports/printFailures.test.ts :: what Leave it out prints prints the reports that did not fail, in their order, and leaves out every failed one |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Leave it out unpicks every report that failed | kept | Leave it out unpicks every report that failed, closes the dialog and prints only the rest in table order | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Leave it out is not offered when leaving the failed reports out would leave nothing to print | kept | when every picked report failed the dialog shows exactly Cancel and Retry, no Leave it out, and nothing prints; the decision itself is proved on printFailures, the buttons on the dialog alone | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Leave it out leaves out only the report that failed, not the other report of the same Team | removed | only the failed report is left out, not the other report of the same Team | src/pages/Reports/printFailures.test.ts :: what Leave it out prints leaves out only the report that failed, not the other report of the same Team ;; src/pages/Reports/printFailures.test.ts :: which of the picked reports failed matches a failed report by its id and by its owner together |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Cancel closes the word that no PDF was made, prints nothing and keeps what was picked | removed | Cancel closes the failure dialog, prints nothing and keeps the picks (merged into the kept failed-report test) | src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports a report that cannot be loaded stops the PDF and is named ;; src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made Cancel calls its own handler once and no other |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports Retry asks for the report again and prints once it arrives | kept | Retry reads the reports again and prints once they arrive | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports when part of the page cannot be loaded no PDF is made and it says so | kept | when part of the page failed the dialog says so, names no report, offers no Leave it out and nothing prints; its description and buttons are proved on the dialog alone | |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx :: One PDF of several reports with nothing picked Export PDF is disabled and says why | kept | with nothing picked Export PDF is disabled and its tooltip says why | |

## Added ids

- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made Cancel calls its own handler once and no other
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made Escape cancels, once, and calls no other handler
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made Leave it out calls its own handler once and no other
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made names each report that could not be read, in print order, with its owner's kind
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made offers Cancel, Leave it out and Retry when leaving the failed reports out is offered
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made offers only Cancel and Retry when leaving the failed reports out is not offered
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made Retry calls its own handler once and no other
- src/pages/Reports/NoPdfDialog.test.tsx :: the word that no PDF was made says that part of the page could not be loaded when no report is named
- src/pages/Reports/printFailures.test.ts :: what Leave it out prints is not offered when leaving the failed reports out would leave nothing to print
- src/pages/Reports/printFailures.test.ts :: what Leave it out prints is not offered when nothing failed
- src/pages/Reports/printFailures.test.ts :: what Leave it out prints leaves out only the report that failed, not the other report of the same Team
- src/pages/Reports/printFailures.test.ts :: what Leave it out prints prints the reports that did not fail, in their order, and leaves out every failed one
- src/pages/Reports/printFailures.test.ts :: which of the picked reports failed matches a failed report by its id and by its owner together
- src/pages/Reports/printFailures.test.ts :: which of the picked reports failed names none when nothing failed
- src/pages/Reports/printFailures.test.ts :: which of the picked reports failed names only the failed reports, in the order the reports are listed
- src/pages/Reports/printFailures.test.ts :: which of the picked reports failed takes a report for the same report only when its id, its owner kind and its owner id all agree

## Assertions the kept page tests must carry

The page file after the rewrite holds exactly the thirteen kept tests, each under the same full name, in the same file
order. Six of them absorb assertions of removed tests or are tightened; the crafter adds exactly these, and the
completion review reads them in the file because the scripts check names only. Every kept test keeps every assertion
it has today, except where this list says an assertion moved to the dialog or function tests. Anchor every name
matcher (`/^Cancel$/`), add no weaker matcher (`toContain`, `toMatch`, `objectContaining`).

- "a reader who is Admin of none of them may still export them" (absorbs the coach test): the reader is Admin of none
  (`permissions: { adminOf: [] }`); Zenith's Quarter report and Gravity's Month report are picked, Export PDF (2) is
  pressed, one print happens, and the printed titles are `[MONTH, QUARTER]`, one level-1 heading per report; that each report starts on a page of its own is proved by ReportPrintPage.pageBreaks.test.tsx, since a heading count is not a page break (Gravity
  before Zenith, as the table lists Teams by name, whichever was picked first). Both halves are asserted.
- "while a PDF of several is prepared, a row's PDF icon is held back and starts no second" (absorbs the Export PDF
  held-back test): `getReport` answers every report at once except Gravity's, which waits on a gate, and Voyager's,
  which is never answered. Phase one: Zenith's Quarter and Gravity's
  Month are picked and Export PDF (2) pressed; one print frame is up; Voyager's row icon is pressed and after a moment
  there is still one frame, `getReport` was called twice, nothing printed and the icon has `aria-disabled="true"`.
  Phase two: the gate is opened, one print happens, the dialog is closed, every frame is gone; Voyager's row icon is
  pressed; one frame is up for it, Export PDF (2) has `aria-busy="true"`, it is pressed, and after a moment there is
  still one frame, `getReport` was called three times in all and the print count is still one. Both phases are in this
  one test, one render.
- "a report that cannot be loaded stops the PDF and is named" (absorbs the Cancel test): Zenith's and Voyager's Quarter
  reports are picked and only Voyager's cannot be read. The dialog named "No PDF was made" shows exactly Voyager's line
  and not Zenith's line (both anchored exact texts), and nothing printed. Then `/^Cancel$/` is pressed: the dialog goes
  (waitFor), after a moment nothing printed, and Export PDF (2) is still there, the picks kept. The assertions on the
  dialog's accessible description and on its three buttons leave the page; the dialog tests hold them.
- "Leave it out unpicks every report that failed" (absorbs "Leave it out unpicks the report that failed and prints the
  rest"): four reports are picked: Zenith's Quarter, Gravity's Month, Voyager's Quarter, Pulsar's Quarter; Voyager's and
  Pulsar's cannot be read. Export PDF (4) is pressed and the dialog appears. `/^Leave it out$/` is pressed: one print
  happens, the titles are `[MONTH, QUARTER]` (Gravity before Zenith, the table's order), Export PDF (2) is there (both
  failed reports unpicked, none of the others), and the dialog is gone.
- "when part of the page cannot be loaded no PDF is made and it says so": the dialog is named "No PDF was made", it
  shows the anchored exact part-of-the-page message, it names no report (no line of a report), no `Leave it out`
  button is there, and nothing printed. Its accessible description and its Cancel and Retry buttons leave the page.
- "Leave it out is not offered when leaving the failed reports out would leave nothing to print": Zenith's and Voyager's Quarter reports are picked and both cannot be read; the dialog named "No PDF was made" shows exactly the buttons Cancel and Retry (in that order), no `Leave it out` button, and nothing printed. The test keeps its old assertions; the page wiring of the not-offered case is proved here.
- "Retry asks for the report again and prints once it arrives": unchanged.
- The other six kept tests stay as they are: closes the frame, table order, nothing prints while one is on its way,
  picks and sorting changed, the preparing look, nothing picked.

## Deliberate breaks that name a kept page test

Several break lines name a kept page test: the row icon not held back and the Export PDF button not busy while a PDF is
prepared, with the merged held-back test; the no-op Cancel with the failed-report test; the dropped or narrowed unpick,
the failed report reprinted and every report treated as failed with the Leave it out test; Leave it out offered when
every picked report failed with the not-offered test; only the first report printed with the Admin-of-none test; every
report named failed and Leave it out always offered with the part-of-the-page test. They go red today and must stay red
after the rewrite; the completion review reads the assertions above in the file, because no break notices a dropped
assertion that another assertion of the same test also catches.

The guards at the top of `exportOne` and `exportPdf` sit behind the disabled icon and the disabled Export PDF button, so
the page cannot reach them. No test covers them before or after this slice, and no break names them.

Every other break names a direct test and goes red today on the direct files this slice authors.
