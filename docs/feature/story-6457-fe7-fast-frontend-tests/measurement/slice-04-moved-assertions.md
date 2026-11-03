# Slice 04 - ModifyTeamSettings family: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the rewrite slices start before the
configuration spike (maintainer decision 2026-11-03).

## Pre-slice commit

`61b44b6ecf4f6b55480c112d5d828ae020bf4d1a` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice.

### Search result: what is real logic inside the component

- Autosave debounce decision: already lives in the shared hook `src/hooks/useModifySettings.ts`
  (`DEBOUNCE_MS = 300`, `autoSaveDebounceMs`), and is already unit-tested with a fake clock in
  `src/hooks/useModifySettings.autosave.test.ts`. No extraction. The component tests only wait past it.
- Rule-gating, already pure and exported next to the components:
  `refinementSettingsBlockers` (RefinementSettingsSection.tsx), `readinessErrors` / `hasReadinessErrors`
  (ReadinessSettings.tsx), `hasIncompleteStageRule` (StageRulesSettings.tsx), `bandErrors` / `hasBandErrors`
  (refinementBand.ts, tested in refinementBand.test.ts), `hasCadenceErrors` (RefinementCadenceSettings.tsx,
  tested in RefinementCadenceSettings.test.ts). `readinessErrors`, `hasIncompleteStageRule` and
  `refinementSettingsBlockers` have no direct unit test today (covered only through the rendered form).
- Real logic still inside a component, to extract:
  1. `teamAutoSaveBlockers` (the function at the top of ModifyTeamSettings.tsx): which required fields
     refuse an autosave and the sentence for each. Pure already, only private to the component file.
  2. The "remember the threshold of a switched-off discussion rule" decision in `ReadinessSettings.tsx`
     (`rememberedThresholds` state, `toggleRule`, `changeDiscussWhen`, lines ~167-190): which threshold a rule
     returns to when switched on again, and when a switch-off is refused. Today it is state inside the
     component.

### Proposed extractions (files beside the component)

| Extraction | New file | Unit-test file |
|---|---|---|
| `teamAutoSaveBlockers` moved out as an exported pure function (component imports it) | `src/components/Common/Team/teamAutoSaveBlockers.ts` | `src/components/Common/Team/teamAutoSaveBlockers.test.ts` |
| Discussion-rule toggle/threshold-memory decision as pure functions (e.g. `nextDiscussWhen`, `rememberThreshold`), with `ReadinessSettings` keeping only the `useState` wrapper | `src/components/Common/Team/discussionRules.ts` | `src/components/Common/Team/discussionRules.test.ts` |
| Direct unit tests for the already-pure `readinessErrors`, `hasIncompleteStageRule`, `refinementSettingsBlockers` (no production move) | none | `src/components/Common/Team/readinessErrors.test.ts`, `src/components/Common/Team/refinementSettingsBlockers.test.ts` (stage-rule case goes in the latter; or one file per function if the slice prefers) |

The exact function split of the second extraction is decided in the extraction step after reading
ReadinessSettings.tsx closely; the file names above are the commitment.

### Production files that may change

- `src/components/Common/Team/ModifyTeamSettings.tsx` (import `teamAutoSaveBlockers`)
- `src/components/Common/Team/ReadinessSettings.tsx` (use `discussionRules.ts`)
- the two new production files above

### Test files edited (existing)

- `src/components/Common/Team/ModifyTeamSettings.test.tsx`
- `src/components/Common/Team/ModifyTeamSettings.refinement.test.tsx`
- `src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx`
- `src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx`
- `src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx`

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

Seven entries, keyed by file and name. May be removed or lowered only once the test runs under budget.

queryCommit (`src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx`):
1. `ModifyTeamSettings query field saves a changed name after the debounce without anyone leaving a field`
2. `ModifyTeamSettings query field saves the edited query once, after the field is left`
3. `ModifyTeamSettings query field saves the latest edit after the save already on its way, when the page is left meanwhile`

readiness (`src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx`):
4. `Readiness in the Refinement section of a Team's settings switches a rule back on at the threshold it was changed to before it was switched off`
5. `Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold`

refinementNeed (`src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx`):
6. `Stage rules in the Refinement section of a Team's settings keeps an edited Ready rule when the Yes votes needed change before it is saved`
7. `Stage rules in the Refinement section of a Team's settings saves a changed Ready rule and leaves the Being refined rule as it was`

Entries 1-3 come from the shared consts `teamQueryCommitFile`, `teamReadinessFile`, `teamRefinementNeedFile`
at the top of slowTests.ts; those consts go with the entries that use them.

## Targets of the fake clock

Real (wall-clock) waits, to be moved onto a fake clock:

| File:line | What | Real time |
|---|---|---|
| `ModifyTeamSettings.refinement.test.tsx:177-178` | `pastTheAutosaveDelay` = `new Promise(r => setTimeout(r, 600))`, used once at line 298 | 600 ms |
| `ModifyTeamSettings.test.tsx:436` | `await new Promise(r => setTimeout(r, 500))` in "does not auto-save while no field has been edited" | 500 ms |
| `ModifyTeamSettings.test.tsx:471` | same sleep inside the loop over 7 `scenarios` (lines ~447-475, "does not auto-save when X is invalid"), so it runs once per scenario | 500 ms x 7 |
| `ModifyTeamSettings.test.tsx:269` | `setTimeout(resolve, 0)` flush after a 3000 ms waitFor at :265 | 0 ms (not a sleep to fake; the waitFor timeout is the generic ceiling) |

Waits that already use a fake clock (`vi.useFakeTimers({ shouldAdvanceTime: true })` +
`advanceTimersByTimeAsync(600)`) and are not sleeps: `readiness.test.tsx:172-184` (used at 219, 241, 253, 358,
574) and `refinementNeed.test.tsx:174-184` (used at 222, 330, 479, 496, 513, 573, 603, 624, 637). These tests
are slow for other reasons (full form render plus `userEvent` typing under `shouldAdvanceTime`), not for
sleeping, so the fake clock alone does not shorten them; the lever is testing the logic directly.

Debounce waits that rely on the real 300 ms debounce through `waitFor` polling:
`queryCommit.test.tsx` lines 102 (changed name after the debounce), 138 and 202 (`toHaveBeenCalledTimes(1)`),
line 36 comment says each test waits past the debounce in real time. In `ModifyTeamSettings.test.tsx` the
`waitFor(() => expect(mockSaveTeamSettings).toHaveBeenCalled())` calls (e.g. line ~427) and in
`refinement.test.tsx` lines 258, 273, 317 do the same. No explicit long timeout other than `{ timeout: 3000 }`
at `ModifyTeamSettings.test.tsx:265`.

## Preserved set (before)

`measurement/slice-04-ids-before.txt`: 153 ids, one per line, sorted, as `file :: fullName`, taken from a
run of the five files (153 passed, 0 skipped, 0 failed; `LIGHTHOUSE_TIME_BUDGET=warn`, vitest json
reporter). Surviving ids must be unchanged at close.

## Before coverage

Run apart from timing: `LIGHTHOUSE_TIME_BUDGET=off pnpm exec vitest run src/components/Common/Team --coverage
--coverage.include='src/components/Common/Team/**'` (7 test files, 186 tests, all passed; the five plus
`refinementBand.test.ts` and `RefinementCadenceSettings.test.ts`).

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (`src/components/Common/Team/**`) | 95.16 | 83.73 | 89.13 | 94.96 |
| `ModifyTeamSettings.tsx` | 77.58 | 65.13 | 50 | 77.19 |

Totals line: `All files | 95.16 | 83.73 | 89.13 | 94.96`. Line coverage (94.96) and branch coverage (83.73)
must not be lower at close.

## Delta (fill at close)

| Measure | Before | After |
|---|---|---|
| Test count (five files) | 153 | 123 (plus 68 in four new unit-test files) |
| Preserved ids missing | n/a | 0 of the surviving; 30 gone, all in the table below |
| Line coverage, Team directory | 94.96 | 96.26 |
| Branch coverage, Team directory | 83.73 | 86.23 |
| Per-test time, allow-listed entries (indicative, not quiet; mean of 5 alternating runs, before -> after) | | name change after debounce 1.56 -> 2.18 s; query saved once 2.09 -> 0.99 s; latest edit while save in flight 2.00 -> 1.36 s; Ready rule with Yes votes 1.85 -> 2.71 s; changed Ready rule 1.76 -> 1.69 s; the entry of the removed "refused" readiness test is gone (2.38 s before); the entry of the restored readiness test is gone too, as the test now runs under budget. The machine was loaded and the spread between runs is large (summed 61-104 s before, 58-80 s after), so single-test figures say little. |
| File time, five files (indicative, not quiet; summed test seconds, mean of 5 alternating pairs, 6 workers, budget off) | 82.31 s (104.4 88.3 88.2 61.4 69.2) | 71.44 s (79.8 70.1 72.0 58.5 76.8), -13.2 % |
| Allow-list entries removed / lowered | 7 | 2 removed, 0 lowered, 5 kept |

## Moved assertions

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| ModifyTeamSettings.refinementNeed.test.tsx :: What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished | `expect(stageRuleOf(conditions, "or")).toEqual(rule);` | refinementSettingsBlockers.test.ts | What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished |
| ModifyTeamSettings.refinementNeed.test.tsx :: What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished | `expect(hasIncompleteStageRule({ ready: null, beingRefined: rule })).toBe(` | refinementSettingsBlockers.test.ts | What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished |
| ModifyTeamSettings.refinementNeed.test.tsx :: What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished | `expect(hasIncompleteStageRule({ ready: rule, beingRefined: null })).toBe(` | refinementSettingsBlockers.test.ts | What a stage rule saves as %s saves as the rule shown and blocks the save only while unfinished |
| ModifyTeamSettings.refinementNeed.test.tsx :: What a stage rule saves as holds nothing back for a Team without stage rules | `expect(hasIncompleteStageRule(undefined)).toBe(false);` | refinementSettingsBlockers.test.ts | What a stage rule saves as holds nothing back for a Team without stage rules |
| ModifyTeamSettings.refinementNeed.test.tsx :: What a stage rule saves as holds nothing back for a Team without stage rules | `expect(hasIncompleteStageRule(null)).toBe(false);` | refinementSettingsBlockers.test.ts | What a stage rule saves as holds nothing back for a Team without stage rules |
| ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | `expect(theReadinessLastSaved()?.discussWhen).toEqual({` | discussionRules.test.ts | the threshold a discussion rule returns to keeps the last allowed threshold when the rule is switched off at one that is refused |
| ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | `expect(theReadinessLastSaved()?.discussWhen).toEqual({` | discussionRules.test.ts | the threshold a discussion rule returns to keeps the last allowed threshold when the rule is switched off at one that is refused |
| ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | `expect(theReadinessLastSaved()?.discussWhen).toEqual({` | discussionRules.test.ts | the threshold a discussion rule returns to keeps the last allowed threshold when the rule is switched off at one that is refused |
| ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | `expect(theReadinessLastSaved()?.discussWhen).toEqual({` | discussionRules.test.ts | the threshold a discussion rule returns to keeps the last allowed threshold when the rule is switched off at one that is refused |
| ModifyTeamSettings.readiness.test.tsx :: the readiness rule the form shares with the server $minYes Yes from $minVoters voters → $minYesError / $minVotersError | `expect(` | readinessErrors.test.ts | the readiness rule the form shares with the server $minYes Yes from $minVoters voters → $minYesError / $minVotersError |
| ModifyTeamSettings.readiness.test.tsx :: the discussion rules the form shares with the server No at $no, “Yes, if…” at $yesIf → $noError / $yesIfError | `expect(` | readinessErrors.test.ts | the discussion rules the form shares with the server No at $no, “Yes, if…” at $yesIf → $noError / $yesIfError |
| ModifyTeamSettings.readiness.test.tsx :: the discussion rules the form shares with the server checks a readiness stored without discussion rules as if it had the defaults | `expect(readinessErrors({ minYes: 3, minVoters: 3 })).toEqual({` | readinessErrors.test.ts | the discussion rules the form shares with the server checks a readiness stored without discussion rules as if it had the defaults |

Rows per file, against `git diff -U0 686f22bf5 -- <file> | grep -c '^-.*expect('`: refinementNeed 5 / 5,
readiness 7 / 7, test.tsx 0 / 0, refinement 0 / 0, queryCommit 0 / 0. The `expect(` rows of the two
`it.each` readiness blocks are one line each in the diff (the call is on the next line); the other rows
quote the removed line as it was. Every moved block was moved verbatim except the removed readiness
page test ("refused"), whose rule logic is covered by `discussionRules.test.ts`; the page file keeps
"switches a rule back on at the threshold it had before" and "switches a rule back on at the threshold it was changed to before it was switched off" as the wiring tests for the same rule.

### Preserved ids

Before 153 ids, after 123 (the five files). Missing: 30, all listed above by their old test (1 removed readiness
test, 23 readiness-rule cases moved to `readinessErrors.test.ts`, 6 stage-rule cases moved to
`refinementSettingsBlockers.test.ts`, kept with the same test names). Added to the five files: 0. Surviving
ids are unchanged.

### New unit tests

`teamAutoSaveBlockers.test.ts` 18, `discussionRules.test.ts` 14, `refinementSettingsBlockers.test.ts` 13 (6 moved,
7 new), `readinessErrors.test.ts` 23 (all moved). Net new tests: 39. Moved: 29. Removed outright: 1.

### Fake clock

`ModifyTeamSettings.refinement.test.tsx` `pastTheAutosaveDelay` (600 ms), `ModifyTeamSettings.test.tsx` the 500 ms sleeps
(one test and the 7-scenario loop) and every `waitPastTheDebounce()` in `ModifyTeamSettings.test.tsx` and
`ModifyTeamSettings.queryCommit.test.tsx` now move a fake clock (`vi.useFakeTimers({ shouldAdvanceTime: true })` plus
`advanceTimersByTimeAsync`). Left as it was: the `setTimeout(resolve, 0)` flush at `ModifyTeamSettings.test.tsx:269`
and the 3000 ms `waitFor` ceiling.

### Allow-list

Seven entries before, five after. Removed: the entry of the removed readiness test and the entry of the restored readiness test (it runs under budget). Kept: the three
queryCommit entries and the two refinementNeed entries; each still renders the whole form and none runs under
0.5 s, so none is stale (a time-budget run reports 0 offenders).

## Review revision

The review found that the page test "switches a rule back on at the threshold it was changed to before it was
switched off" had been removed as if `discussionRules.test.ts` covered it. It does not: the unit tests check the
helper, not that `ReadinessSettings` hands the helper the threshold currently shown. The one remaining page test
starts from a stored 4 and ends at 4, so passing the remembered threshold instead of the shown one would have
gone unnoticed. The test is back, unchanged, as a page test; it fails when `ReadinessSettings` passes the
remembered threshold instead of the shown one, and passes with the real code. Only the "refused" page test stays
removed, because its extra case (a threshold of 0) is asserted in `discussionRules.test.ts` and its wiring is the
same. Counts above are corrected (removed 1, not 2; 123 ids after, 30 gone). The restored test ran in about 1.5 s
(limit 2 s), so no allow-list entry came back. The three autosave test files now take their wait from
`DEBOUNCE_MS` plus a margin instead of a hard-coded 600. All timing figures remain indicative: the machine was
loaded throughout.
