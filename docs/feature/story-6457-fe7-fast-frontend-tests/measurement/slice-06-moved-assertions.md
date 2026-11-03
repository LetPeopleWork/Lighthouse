# Slice 06 - Reports tab and wizards: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the rewrite slices start before the
configuration spike (maintainer decision 2026-11-03). The machine was shared with a parallel session.

## Pre-slice commit

`5003575e55b2612349a6871ad8ecc7a9328b372e` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. This slice is behaviour-free: no production behaviour changes,
no assertion is weakened or dropped.

### Search result: what is real logic inside the components

The planned production files are thinner than the roadmap assumed. The logic that the five test files drive lives
mostly in `BulkReportWizard.tsx` and in `ReportWizard.tsx` (the single wizard, used by ReportsTab for create and by
ReportView for edit); both are named below. Step "is it complete" rules and refusals are already pure and unit
tested (`wizard/reportTypes.ts`, `wizard/reportWindowCheck.ts`, `wizard/thenAndNow/thenAndNowDraft.ts`,
`wizard/reportRefusalCopy.ts`); they are not extracted again.

`src/pages/Reports/BulkReportWizard.tsx` (461 lines), module-private and untested without a render:

- `limitOfFor(draft, templates)`: what stops an owner from holding the window (Then and Now lengths, the furthest
  back window wins, formatted dates). Pure over a draft, templates and an owner.
- `typeStepAt(activeStep)`, `nextActionOf(activeStep, draft, templates, onNext)`, `createActionOf(activeStep,
  pickedCount, ownersStatus, onCreate)`: which step is shown, whether Next is disabled, the Create label and its
  disabled and `describedBy` state (owners not ready, nothing picked, over the ceiling).
- The pick rules inside `ReadyWizard`: `changeDraft` keeps only the picks the new window can still hold (a dropped
  pick stays dropped), `selectAll` adds only owners that can hold the window, `pickedOwners` is the picked owners
  that can hold it, `pickedNames`. All are closures over state today.
- `runOf(creation)`: mutation state to the result shape (pending, error, success).
- `createLabelOf(count)`, `byName`, the owner filter in `useOwnersToCreateOn`.

`src/pages/Common/Reports/wizard/ReportWizard.tsx` (257 lines):

- `canMoveOn(step, isFrozen, draft, templates)`: whether Next or Create is enabled (bounds not read yet holds it).
- `saveFailureCopyFor(error, words)`, `isStaleSave`, `isGone`, `isThenBeforeCutoff`: which sentence an API error
  gets, and whether the bounds are asked for again.
- `firstStepOf(isEditing)` and the `isLastStep` / `activeStep` arithmetic.

`src/pages/Common/Reports/ReportsTab.tsx` (141 lines):

- The Create Report button state (`data === undefined || isAtTheCap`) and the empty sentence
  (`isEditor || rolesAreLoading`, else "A Team Admin can create one").

`src/pages/Common/Reports/ReportView.tsx` (133 lines):

- `mayChange` and `mayExport` (`exportWaiting || (report !== undefined && !isError && isShownByThisVersion(...))`).

Already pure, no extraction: `reportTypes`, `ownerCannotHold`, `thenEndOf`, `lengthOf`, `nowLengthOf`,
`thenRefusalOf`, `nowRefusalOf`, `refusalCopyFor`, `mayEditReportsOf`.

### Proposed extractions (files beside the component)

The exact function split is decided in 07-02 after reading the files closely; the file names are the commitment.

| Extraction | From | New file | Unit-test file |
|---|---|---|---|
| `limitOfFor` plus the pick rules over a draft: `picksThatCanStillHold`, `holdableOwners`, `pickedOwnersOf` (pure over owners, picked keys, a limit function) | `BulkReportWizard.tsx` | `src/pages/Reports/ownerLimitOfWindow.ts` | `src/pages/Reports/ownerLimitOfWindow.test.ts` |
| `typeStepAt`, `nextActionOf`, `createActionOf`, `createLabelOf`, `runOf` as exported pure functions over the step number | `BulkReportWizard.tsx` | `src/pages/Reports/bulkWizardSteps.ts` | `src/pages/Reports/bulkWizardSteps.test.ts` |
| `canMoveOn`, `firstStepOf`, `saveFailureCopyFor`, `isStaleSave`, `isGone`, `isThenBeforeCutoff` | `wizard/ReportWizard.tsx` | `src/pages/Common/Reports/wizard/reportWizardRules.ts` | `src/pages/Common/Reports/wizard/reportWizardRules.test.ts` |
| Create Report button state and empty sentence of the tab, `mayExport` of the view (only if 07-02 finds them worth a function; otherwise left, and the page tests stay) | `ReportsTab.tsx`, `ReportView.tsx` | `src/pages/Common/Reports/reportsTabState.ts` | `src/pages/Common/Reports/reportsTabState.test.ts` |

### Production files that may change

- `src/pages/Reports/BulkReportWizard.tsx` (imports the two new files)
- `src/pages/Common/Reports/wizard/ReportWizard.tsx` (imports `reportWizardRules.ts`). The roadmap did not name it;
  it holds the wizard step and validation logic the ReportsTab create tests drive, and the ReportView edit wizard.
- `src/pages/Common/Reports/ReportsTab.tsx` and `ReportView.tsx` (only if the fourth extraction is done)
- the new production files above

### Test files edited (existing)

- `src/pages/Common/Reports/ReportsTab.createWizard.test.tsx`
- `src/pages/Common/Reports/ReportsTab.test.tsx`
- `src/pages/Common/Reports/ReportView.test.tsx`
- `src/pages/Reports/BulkReportWizard.test.tsx`
- `src/pages/Reports/BulkReportWizard.openings.test.tsx`

Not edited, and must stay green as they are (they hold wiring named below):
`ReportView.editWizard.test.tsx`, `BulkReportWizard.ownerLimits.test.tsx`, `BulkReportWizard.portfolios.test.tsx`,
`BulkReportWizard.ownerStepLooks.test.tsx`.

### Fake-clock targets

Two real-time waits, found in the code:

1. `src/pages/Reports/BulkReportWizard.openings.test.tsx`, line 55 (inside `theLastPressSettles`, run in `afterEach`
   for all 3 tests of the file):
   ```ts
   /**
    * A wizard button keeps its spinner for at least 300 ms after a press, and its Next is gone the moment the
    * step changes, so nothing on the page shows when that wait is over. Each test sits it out before it ends;
    * otherwise the last press comes due after the page has been taken down and fails the whole run.
    */
   const theLastPressSettles = () =>
   	act(() => new Promise((resolve) => setTimeout(resolve, 350)));

   afterEach(theLastPressSettles);
   ```
2. `src/pages/Reports/BulkReportWizard.test.tsx`, line 919 (test `Who may press Create reports for an Admin on
   Premium it carries no tooltip`):
   ```ts
   await user.hover(button.parentElement ?? button);
   await act(() => new Promise((resolve) => setTimeout(resolve, 300)));

   expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
   ```
   The 300 ms is the tooltip's enter delay: the test waits past it to prove no tooltip appears.

Both become fake timers (advance by the same milliseconds) in 07-02, with the same assertions. Not in the plan, noted
for 07-02 to decide: the three `ReportView.test.tsx` tests under "A report on a slow or failing connection" take
about 1.0 to 1.1 s each, which is the query client's retry delay in real time (the slowest tests of the five files
after the two Bulk ones).

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

None. The allow-list has 23 entries; only two touch Reports (`reportsRemovalFile` entries and
`ReportsPage.usageData.test.tsx`), both outside this slice. The slowest test of the five files is 1.85 s
(`BulkReportWizard.test.tsx` :: a coach creates the same comparison for Zenith and Gravity), under the 2 s budget.
There is nothing to delete or lower, and 07-02 adds none.

## Preserved set (before)

`measurement/slice-06-ids-before.txt`: 246 ids, one per line, sorted, as `file :: fullName`, taken from a run of
the five test files (246 passed, 0 skipped, 0 failed; `LIGHTHOUSE_TIME_BUDGET=off`, vitest json reporter, file paths
relative to `Lighthouse.Frontend/`). Surviving ids must be unchanged at close. A test that moves keeps its file and
name, or is listed in the Moved assertions table below.

Per file: `ReportsTab.createWizard.test.tsx`, `ReportsTab.test.tsx`, `ReportView.test.tsx`,
`BulkReportWizard.test.tsx`, `BulkReportWizard.openings.test.tsx` (3 tests).

## Wiring to keep

For each behaviour a page or wizard test proves through the UI, one page test must still prove, after the rewrite,
that the component wires it at its call site. Unit tests over the extracted functions come in addition; they do not
replace these. A reason string asserted is not button state asserted: where the behaviour is "disabled", the page
test asserts `toBeDisabled()` / `toBeEnabled()` on the button itself.

| Behaviour | Page test that must still prove the wiring |
|---|---|
| Next is disabled while Then's length is no number of days | `BulkReportWizard.test.tsx` :: Moving through the wizard Next waits while Then's length is no number of days (button state) |
| Single wizard: Next disabled and nothing further for a Then/Now that is refused (too short, past the kept day, ends in the future, not a whole number) | `ReportsTab.createWizard.test.tsx` :: Step two / Step three `... and goes no further` tests (Next button state, not only the sentence) |
| Back leads to the earlier step; no Back on the first step | `BulkReportWizard.test.tsx` :: Back leads from Then to the Type step, which offers no Back of its own; `ReportsTab.createWizard.test.tsx` :: The wizard's steps offers no way back from the first step |
| The owner step offers Create, not Next, and Create is disabled before a Team is picked | `BulkReportWizard.test.tsx` :: the owner step offers Create, not Next, and nothing can be created before a Team is picked |
| Create is enabled at exactly 50 picked, disabled at 51 with the ceiling sentence as its description | `BulkReportWizard.test.tsx` :: At most 50 ... (30 and 20 make 50; 30 and 21 make 51; Select all picks all 60) - button state asserted |
| Create is disabled while the owners' roles are on their way or could not be read | `BulkReportWizard.test.tsx` :: When Create reports can be pressed (while permissions are on their way, permissions cannot be loaded) |
| A pick the new window cannot hold is dropped for good; Select all leaves out owners that cannot hold it | `BulkReportWizard.ownerLimits.test.tsx` :: a pick made under a short window is dropped when going back...; a dropped pick stays dropped...; Select all picks every Team that can hold the window (file not edited) |
| The created report asks for what each picked owner needs: the picked owners that can hold the window are the ones sent | `BulkReportWizard.test.tsx` :: every Team picked gets the same comparison, in the order the step lists them |
| Next and Create stay held until today's bounds have been read again; no Create while bounds are re-read after a refusal | `ReportsTab.createWizard.test.tsx` :: The wizard while its bounds are read (holds Next on reopening...; offers no Create while the bounds are read again...) |
| Create disabled and the wizard held (Cancel and Back disabled) while saving; a name or no metric blocks Create | `ReportsTab.createWizard.test.tsx` :: Creating the report holds the wizard open...; Step four cannot create a report without a name / showing no metric |
| Which sentence a save failure gets (someone else changed it, it no longer exists, could not be saved) | `ReportView.editWizard.test.tsx` :: says the report was changed by someone else...; says the report no longer exists...; couldn't be saved (file not edited) |
| Create Report is disabled at the Community cap and while the list is on its way; the cap sentence names the owner in the instance's word | `ReportsTab.test.tsx` :: The Community cap on the Reports tab (disables Create Report at the cap and says why...; enables Create Report again once the report is deleted); The Reports list on a slow or failing connection |
| Create Report, Edit and Delete only for an Admin; a reader gets the empty sentence that says an Admin can create one | `ReportsTab.test.tsx` :: The Reports tab lists a Team's reports (shows a reader the reports but nothing to create or delete them with; tells a reader of a Team / of a Portfolio without reports that an Admin can create one); `ReportView.test.tsx` :: Who may change a report from its page (offers an admin Edit report and Delete; offers a reader neither) |
| Export is offered only for a report this version can show | `ReportsTab.test.tsx` :: lists a report of a type this version cannot show as not supported, and still lets an admin delete it. The view's own `mayExport` has no page test among the five files; 07-02 either leaves `ReportView.tsx` alone or finds the test in `ReportView.exportPdf*.test.tsx` before moving it |

If 07-02 removes or merges a page test, it names in the Moved assertions table which row of this list it still
leaves proven.

## Delta and moved assertions

Empty until 07-02.

| Measure | Before | After |
|---|---|---|
| Test count (five files) | 246 | |
| Tests added (new unit files) | n/a | |
| Tests removed | n/a | |
| Preserved ids missing | n/a | |
| Line coverage, `src/pages/Reports` and `src/pages/Common/Reports` | 99.39 | |
| Branch coverage, same directories | 96.94 | |
| Allow-list length | 23 | |

### Moved assertions

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| | | | |

## Before figures

Summed test durations of the five touched files, 3 runs (`LIGHTHOUSE_TIME_BUDGET=off`, default local worker cap,
12 cores so 6 workers): 62.57 s, 61.28 s, 60.58 s; mean **61.48 s**. Indicative, shared machine (load average
about 3 at the start).

Mean per file over the 3 runs (summed test durations): `ReportsTab.createWizard.test.tsx` 17.03 s,
`BulkReportWizard.test.tsx` 16.08 s, `ReportsTab.test.tsx` 15.02 s, `ReportView.test.tsx` 10.02 s,
`BulkReportWizard.openings.test.tsx` 3.32 s.

Coverage: `LIGHTHOUSE_TIME_BUDGET=off pnpm exec vitest run src/pages/Reports src/pages/Common/Reports --coverage
--coverage.include='src/pages/Reports/**' --coverage.include='src/pages/Common/Reports/**'` (51 test files, 830
tests, all passed).

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (both directories) | 99.1 | 96.94 | 98.8 | 99.39 |
| `Common/Reports` (top level) | 98.65 | 95.17 | 99.12 | 99.14 |
| `Common/Reports/wizard` | 98.09 | 99.12 | 94.8 | 98.5 |
| `Common/Reports/wizard/thenAndNow` | 100 | 97.47 | 100 | 100 |
| `Reports` | 99.32 | 96.35 | 99.31 | 99.75 |

Totals line: `All files | 99.1 | 96.94 | 98.8 | 99.39`. Line coverage (99.39) and branch coverage (96.94) of the two
directories must not be lower at close. (`Common/Reports/print` and `panels` sit inside the second directory and are
part of that total.)

## Counting rule

- Test count: the vitest json reporter's `assertionResults` for the five files; ids as `file :: fullName`.
- Seconds: the summed `duration` of those assertion results, per run, as the mean of 3 runs (before) and of 5
  alternating before/after pairs at close, same minute, same worker count, `LIGHTHOUSE_TIME_BUDGET=off`. Always
  labelled indicative while the machine is shared.
- Coverage: the same command as above, lines and branches of the combined directories.
- A surviving id keeps file and name; an id that changes file only is listed as moved; an id that disappears is a
  removed test and needs a row in Moved assertions naming where the assertion lives now.
