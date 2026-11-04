# Slice 10 - ModifyProjectSettings: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with a parallel session (load
average about 4 on 4 cores during the runs).

## Pre-slice commit

`6a789ebefa3b8f32e896fb0b479ed0a3336206a6` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. The slice is behaviour-free: no production behaviour changes, no
assertion is weakened or dropped without a row in the table at the end, no surviving test id changes.

### Search result: what is real logic inside the component

`ModifyProjectSettings.tsx` (443 lines) is the Portfolio twin of `ModifyTeamSettings.tsx`. Read first:

- Autosave debounce decision: already in the shared hook `src/hooks/useModifySettings.ts` (`DEBOUNCE_MS = 300`) and
  already tested with a fake clock in `useModifySettings.autosave.test.ts`. No extraction. The page tests only wait
  past it.
- State-mapping rules (`validateStateMappings`, `mappingAutoSaveBlockers`, `storedStateSettingsOf`): already pure in
  `src/utils/stateMappingValidation.ts` with their own tests. No extraction.
- Real logic still inside the component file: `portfolioAutoSaveBlockers` (lines ~63-121), the function that says
  which fields refuse an autosave and the sentence for each (name, default number of Work Items, percentile and
  history, Work Item Types unless the schema says they are not required, the three state lists, mapping blockers,
  and for a non-default Portfolio the Work Tracking System and the query/value). It is already pure (inputs: the
  settings, the selected system, "is default", `getTerm`, the stored state settings), only private to the component
  file. This is the same finding as slice 4's `teamAutoSaveBlockers`.
- Not extracted: `PortfolioSizeNote.tsx` already has its own unit test (`PortfolioSizeNote.test.tsx`, 4 tests);
  the "can save" decision is one line (`disableSave === undefined ? canUpdatePortfolioData : !disableSave`) and is
  asserted by the two "a caller that ..." page tests, which are wiring and stay.

### Slice 4's helpers this slice reuses

- `src/tests/waitPastTheDebounce.ts` is a real wall-clock wait (500 ms). In slice 4 the Team page tests moved off
  it onto a fake clock: `vi.useFakeTimers({ shouldAdvanceTime: true })` in `beforeEach`/`afterEach` plus
  `await vi.advanceTimersByTimeAsync(DEBOUNCE_MS + 200)` (see `ModifyTeamSettings.test.tsx` lines ~215 and ~291,
  `ModifyTeamSettings.queryCommit.test.tsx` lines ~43 and ~104). This slice copies that pattern into the Project
  page files; no new shared helper is needed beyond that pattern.
- The shape of slice 4's `teamAutoSaveBlockers.ts` / `teamAutoSaveBlockers.test.ts`: a plain pure function beside
  the component, a plain `.test.ts` driving it with settings objects.

### Proposed extraction (file beside the component)

| Extraction | New file | Unit-test file |
|---|---|---|
| `portfolioAutoSaveBlockers` moved out as an exported pure function (component imports it) | `src/components/Common/ProjectSettings/portfolioAutoSaveBlockers.ts` | `src/components/Common/ProjectSettings/portfolioAutoSaveBlockers.test.ts` |

That is the only extraction. The function body moves verbatim; its behaviour does not change. The test file drives
one case per blocker sentence, the Linear schema cases (`isWorkItemTypesRequired === false`, `isRequired === false`,
the display-label fallback), the default-settings case (no system/value blockers) and the percentile boundary
(`> 0`, `>= 30`), then the page tests that today cover the same rules through the full form are reduced to wiring
in the rewrite step (see the moved-assertions table, filled at close).

### Production files that may change

- `src/components/Common/ProjectSettings/ModifyProjectSettings.tsx` (import `portfolioAutoSaveBlockers`; the
  function and its now-unused imports leave the file)
- the one new production file above

No other production file changes (not `useModifySettings.ts`, not `PortfolioSizeNote.tsx`, not the Advanced
components, not `waitPastTheDebounce.ts`, which `GeneralSettingsComponent.test.tsx` still uses).

### Test files edited (existing)

- `src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx`
- `src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx`

Added: `src/components/Common/ProjectSettings/portfolioAutoSaveBlockers.test.ts` (new unit-test file).

Preserved untouched (ids must be unchanged; no real wait found in them): `ModifyProjectSettings.sizeNote.test.tsx`
(8 tests, no sleep), `PortfolioSizeNote.test.tsx`, and the files under `Advanced/`.

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

One entry, keyed by file and name:

1. `src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx` :: `ModifyProjectSettings query
   field saves the edited query once, after the field is left` (ceiling 3.0 s; 1.84 s in the not-quiet before run).

**Not declared for deletion.** Its stated reason is that it renders the whole project settings form and types a
query before leaving the field; the rewrite moves its debounce wait onto a fake clock but still renders the whole
form and types, which is the stated reason. It is deleted only if a quiet run at close shows it well under the
default budget; otherwise it stays. No other entry belongs to this slice.

## Targets of the fake clock

Real (wall-clock) waits, to be moved onto a fake clock. Line numbers are those at the pre-slice commit; the task
brief's 590, 617 and 722 had moved to 585, 612 and 717.

| File:line | What | Real time |
|---|---|---|
| `ModifyProjectSettings.test.tsx:585` | `await new Promise(r => setTimeout(r, 500))` in "suppresses auto-save when portfolio data updates are not permitted" | 500 ms |
| `ModifyProjectSettings.test.tsx:612` | same sleep in "a caller that switches saving off says why, whatever the license allows" | 500 ms |
| `ModifyProjectSettings.test.tsx:717` | same sleep inside the loop over 9 `scenarios` (lines ~665-720, "does not auto-save when X is invalid"), so it runs once per scenario | 500 ms x 9 |
| `ModifyProjectSettings.test.tsx:945, 1021, 1055, 1091` | `waitPastTheDebounce()` (real, 300 + 200 ms) in four "blocked autosave feedback" tests | 500 ms x 4 |
| `ModifyProjectSettings.queryCommit.test.tsx:122, 132, 142, 155, 166, 178, 194` | `waitPastTheDebounce()` seven calls across the six tests | 500 ms x 7 |

That is 11 + 4 + 7 = 22 real half-seconds, about 11 s of sleeping of the 24.6 s summed. The sleeps and the
`waitPastTheDebounce()` calls at the pre-slice commit are all real time; there is no `useFakeTimers` in either file
today. Not a sleep to fake: the `waitFor` polls (`mockSaveProjectSettings` toHaveBeenCalled) that wait for the real
300 ms debounce to fire, which become fast once the fake clock advances the debounce.

## Preserved set (before)

`measurement/slice-10-ids-before.txt`: 57 ids, one per line, sorted, as `file :: fullName` (file paths relative to
`Lighthouse.Frontend/`), taken from a run of the three files (57 passed, 0 skipped, 0 failed;
`LIGHTHOUSE_TIME_BUDGET=warn`, vitest json reporter). Per file: `ModifyProjectSettings.test.tsx` 43,
`ModifyProjectSettings.queryCommit.test.tsx` 6, `ModifyProjectSettings.sizeNote.test.tsx` 8. Surviving ids must be
unchanged at close. A test that moves keeps its file and name, or is listed in the table below.

Command (in `Lighthouse.Frontend`; the config pins the json report to the gitignored `test-results.json`):

```
LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx \
  src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx \
  src/components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx \
  --reporter=json
```

## Before figures (indicative, not quiet)

Summed per-test durations of the touched files (the figure the close-time pairs compare), one run:

| File | Tests | Summed seconds |
|---|---|---|
| `ModifyProjectSettings.test.tsx` | 43 | 17.02 |
| `ModifyProjectSettings.queryCommit.test.tsx` | 6 | 6.17 |
| `ModifyProjectSettings.sizeNote.test.tsx` | 8 | 1.41 |
| Sum | 57 | 24.61 |

Wall clock of the three files together (vitest "Duration"): 22.73 s.

## Coverage (before)

`src/components/Common/ProjectSettings/**`, from a run of the whole directory (8 test files, 126 tests, all
passed; a directory's coverage needs all of its tests, since the Advanced files drive their own sources):

```
LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run src/components/Common/ProjectSettings \
  --coverage.enabled --coverage.include='src/components/Common/ProjectSettings/**' --coverage.reporter=text
```

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (`src/components/Common/ProjectSettings/**`) | 89.25 | 76.19 | 68.42 | 89.07 |
| `ModifyProjectSettings.tsx` | 81.66 | 74.52 | 47.36 | 81.35 |

Line coverage (89.07) and branch coverage (76.19) must not be lower at close. The uncovered lines of
`ModifyProjectSettings.tsx` are 108, 278-354 and 433 (the callbacks of the state-mapping, wait-state, cycle-time,
feature-size, ownership and estimation components passed as props, and the reload action).

## Delta

Commits (pre-slice `6a789ebef`): `02995973f` extraction (no test file changed; the 126 tests of the directory passed
unmodified at it), `fb9c8c540` test rewrite; this report is the close-report commit. Files changed since the pre-slice
commit (`git diff --name-only 6a789ebef`, apart from this report and `feature-delta.md`): `portfolioAutoSaveBlockers.ts`
(new), `ModifyProjectSettings.tsx`, `portfolioAutoSaveBlockers.test.ts` (new), `ModifyProjectSettings.test.tsx`,
`ModifyProjectSettings.queryCommit.test.tsx`. `slowTests.ts`, `waitPastTheDebounce.ts`, `setupTests.ts`,
`biome.json` and every other production file are untouched.

RED: skipped for the extraction (behaviour-free, the existing tests are the net). The extraction differs from "verbatim"
in one place: the file's cognitive complexity came to 16 (limit 15; in the component file the limit is switched off by an
override in `biome.json`, which names the component file only), so the last block (the system and query check) became a
private function of the new file, `connectionBlockers`. Same conditions, same sentences, same order.

| Measure | Before | After |
|---|---|---|
| Test count (three files; four with the new file) | 57 | 50 (+ 24 in `portfolioAutoSaveBlockers.test.ts` = 74) |
| Preserved ids missing | n/a | 7, all listed in the table below; 50 unchanged |
| `expect(` removed / added, `ModifyProjectSettings.test.tsx` | n/a | 0 / 0 (the seven removed ids shared one loop body) |
| `expect(` removed / added, `ModifyProjectSettings.queryCommit.test.tsx` | n/a | 0 / 0 |
| Line coverage, ProjectSettings directory | 89.07 | 90.16 |
| Branch coverage, ProjectSettings directory | 76.19 | 76.87 |
| `ModifyProjectSettings.tsx` lines / branches | 81.35 / 74.52 | 70.58 / 66.21 (the covered function left the file, so the share of uncovered prop callbacks rises; uncovered lines are now 215-291 and 370; the moved function is covered in its own file, judged by the directory total) |
| File time, three files (indicative, not quiet; summed test seconds, 5 alternating pairs) | 141.58 s | 72.27 s (-49.0 %) |
| Allow-list length (`slowTests.ts`) | 13 | 13 |
| Allow-list entries removed / lowered | 1 | 0 / 0 |

Time: five alternating before/after pairs back to back, `LIGHTHOUSE_TEST_WORKERS=2` both sides, only the touched test
files (before = the three files from a temporary worktree of `6a789ebef`; after = the same three plus the new file), the
sum of per-test durations from the JSON report. Indicative (not quiet): load average 4 to 5 for pairs 1 to 4, 8 to 9 for
pair 5.

| Pair | Before (s, 57 tests) | After (s, 74 tests) |
|---|---|---|
| 1 | 25.14 | 12.12 |
| 2 | 24.59 | 12.88 |
| 3 | 26.23 | 12.41 |
| 4 | 24.58 | 17.44 |
| 5 | 41.04 | 17.42 |
| Sum | 141.58 | 72.27 |

The allow-list entry for the query field stays: the test still renders the whole form and types a query. Its wait is now
a fake clock, and it ran in under a second in the not-quiet runs, but the declaration says only a quiet run may delete it.

Coverage: directory run as in the "Coverage (before)" section, 9 files, 143 tests passed. The component file's own
percentages fall because its covered function moved out; the directory total, which is the judged figure, rises.

### Wiring proven by mutation

Production code changed temporarily (a line altered, the touched test files run: the two page files and the new file),
then restored with `git checkout`; `git status` after each batch showed only the test files. A "saves nothing" test is
credited with a wait only if the mutation lets a save start and the test fails at its final assertion.

| Wait (file:line before) | Mutation | Failing surviving test |
|---|---|---|
| `.test.tsx:585` | `canSave` forced to `true` | "suppresses auto-save when portfolio data updates are not permitted" (and the next one) |
| `.test.tsx:612` | `canSave` ignores `disableSave` | "a caller that switches saving off says why, whatever the license allows" |
| `.test.tsx:717` (Name, Work Item Query) | the blockers call replaced by `[]` | "does not auto-save when Name is invalid", "... Work Item Query is invalid" (plus the five blocker-warning tests) |
| same | `isDefault` passed as `true` | "does not auto-save when Work Item Query is invalid" only |
| same | the name rule disabled in the new file | "does not auto-save when Name is invalid" and three function tests |
| same | the autosave gate ignores the blockers (`!formValid` removed in `useModifySettings.ts`) | both loop tests |
| `.test.tsx:945, 1021, 1055, 1091` | the same `!formValid` mutation | "holds back autosave while a new mapping is named after a Doing state ...", "... a new mapping's state sits in two categories", "... a mapping's name hides one of its states in To Do", "... two complete mappings share a source state" (the warning text is still shown under it, so each fails at the saved assertion after the wait) |
| call site | the mapping rule removed from the new file | the same four tests and "names a state mapping that is named after a state it does not group" |
| call site | `system` passed as `null` | 12 tests (every save test) |
| call site | `storedStateSettingsOf(stored)` replaced by `undefined` | "saves an unrelated edit although a stored mapping is named after a state another mapping groups" |
| `queryCommit:122, 166, 194` | the unchanged-value guard removed in `GeneralSettingsComponent.tsx` and in `useModifySettings.ts` together (either one alone is not enough: each covers for the other) | "saves nothing when the query is focused and left without a change", "... edited back to what was saved ...", "saves nothing when the page is left without an edit" |
| `queryCommit:132, 142` | the query committed on every keystroke | "saves nothing while the query is being typed and the field is still focused", "saves the edited query once, after the field is left" |
| `queryCommit:155` | a second save sent 50 ms after the first | "saves the edited query once, after the field is left" |
| `queryCommit:178` | the unsent-save flush on unmount removed | "saves the edited query when the page is left straight after the field" |

A finding the mutation exposed in the old tests: the nine "does not auto-save when X is invalid" cases clicked "Remove
ToDo", which removes a state that is not there, so the settings did not change and the autosave would not have fired
whatever the blockers said. With the blockers call replaced by `[]` they still passed. The two cases that stay click
"Add ToDo" instead, a real edit that is no remedy for a missing name or query; the mutations above fail them.

### Moved assertions

No `expect(` line was removed or added in either page file (counts above), so the table is by test id. The seven removed
ids each ran the loop body (a wait for loading, a click, a sleep, `expect(mockSaveProjectSettings).not.toHaveBeenCalled()`)
and checked one rule of `portfolioAutoSaveBlockers`; as written they could not fail (see the finding), so their effective
assertion was nothing. The rule is now asserted on the function, which can fail. The component reaches every rule through
one call with the whole settings object, so no field has its own wiring to lose: the kept Name and Work Item Query loop tests
and the four "holds back autosave" tests prove that call.

Adversarial review (2026-11-04): approved, no blocker or major. This line closes its one recommended change. Three minors
stay as they are: the `as number` cast that lets the null-history case through, the `pastTheAutosaveDelay` helper copied
between page test files as in slice 4, and the reworded reason beside the unchanged 20 s timeout in the queryCommit file.

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| `ModifyProjectSettings.test.tsx` :: does not auto-save when Default Amount of Work Items is invalid | `mockSaveProjectSettings` not called | `portfolioAutoSaveBlockers.test.ts` | names no default number of work items |
| same :: ... Work Item Types is invalid | same | same | names no work item type; does not ask for work item types when the system does not need them |
| same :: ... To Do States is invalid | same | same | names no To Do state |
| same :: ... Doing States is invalid | same | same | names no Doing state |
| same :: ... Done States is invalid | same | same | names no Done state |
| same :: ... Missing Historical Days is invalid | same | same | a size percentile in use is named for a history that was never set / a history of 29 days; accepted with exactly 30 |
| same :: ... Missing Default Work Item Percentile is invalid | same | same | a size percentile in use is named for a percentile of 0; accepted just above 0; not asked for while the default number is fixed |

Kept as wiring (ids unchanged, each proven by a mutation above): "does not auto-save when Name is invalid", "does not
auto-save when Work Item Query is invalid", "names the To Do States field when no To Do state is configured" and the four
"holds back autosave" tests, "saves an unrelated edit although a stored mapping ...".

### Deviations and what is not proven

- The declaration said "the function body moves verbatim"; one block became a private function (complexity limit, above).
- The two kept loop cases changed their click from "Remove ToDo" to "Add ToDo" (above), a change of the test body made so
  that the test can fail. Their ids are unchanged.
- The full `pnpm test` run (6 workers, load 8 to 11) showed 2 failures in files this slice does not touch
  (`BulkReportWizard.test.tsx`, `ReportsPage.combinedPdf.test.tsx`) and 1 unhandled error; both files pass when run alone
  (58 tests). Not run again in full.
