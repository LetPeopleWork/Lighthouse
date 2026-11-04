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

## Delta (fill at close)

| Measure | Before | After |
|---|---|---|
| Test count (three files) | 57 | |
| Preserved ids missing | n/a | |
| Line coverage, ProjectSettings directory | 89.07 | |
| Branch coverage, ProjectSettings directory | 76.19 | |
| File time, three files (indicative, not quiet; summed test seconds, mean of 5 alternating pairs) | | |
| Allow-list entries removed / lowered | 1 | |

## Moved assertions

Filled at close, one row per removed or shrunk `expect(` against
`git diff -U0 6a789ebef -- <file> | grep -c '^-.*expect('`.

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
