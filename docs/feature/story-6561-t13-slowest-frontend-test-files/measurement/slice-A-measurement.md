# Slice A, step 01-01: declare, measure, decide (2026-11-07)

Code before-commit `1ffe2be77`, documents `b17d5bec9`. Everything below was measured locally, 12 cores, 2 workers
(`LIGHTHOUSE_TEST_WORKERS=2`), `TZ=Europe/Zurich`, from `Lighthouse.Frontend`. No test file and no production file under
`Lighthouse.Frontend` is changed by this step.

## Result: the gate stops slice A

The A-1 gate measured r = **1.52** over 5 of 5 valid pairs. The stop line is 1.85. So nothing moves, steps 01-02 to 01-05
are not started, and the run goes to the hold. Details under "The A-1 gate".

## Baseline regenerated from HEAD

Recipe of `feature-delta.md` ("Baseline"): report deleted, start time noted, files run, `baseline6561.py --started
--expected-tests`. The seven files ran 330 of 330 passed; the outside-seven files plus the new one ran 228 of 228 passed.

| File | Tests old / new | `expect(` old / new | Reason |
|---|---|---|---|
| `ModifyTeamSettings.refinementNeed` | 47 / 47 | 77 / 77 | unchanged |
| `ModifyTeamSettings.readiness` | 24 / 24 | 44 / 44 | unchanged in ids and `expect(` (commit 6be1e621e changed how one test waits, not what it asserts) |
| `ModifyTeamSettings.test` | 35 / **36** | 59 / **62** | Story 6501: +1 test (`a Team on Jira saves its Parent sources without the old field`, 3 `expect(`), 1 test renamed (`the Parent Override Field reads ...` became `the Parent sources read the Teams' default of the connection now chosen`) |
| `OverviewDashboard` | 51 / 51 | 113 / 113 | unchanged |
| `BaseMetricsView.loading` | 133 / 133 | 150 / 150 | unchanged |
| `ReportsPage.removal` | 21 / 21 | 37 / 37 | unchanged |
| `ReportsPage.combinedPdf` | 18 / 18 | 44 / 44 | unchanged |
| **seven files** | **329 / 330** | **524 / 527** | |
| outside-seven (8 files) | 215 / 215 | 401 / 401 | unchanged |
| `ModifyTeamSettings.usageData` (new, now in `outside-seven/`) | 0 / 13 | 0 / 10 | new file of Story 6501 |
| **outside-seven total** | **215 / 228** | **401 / 411** | |

`ModifyTeamSettings.usageData.test.tsx` (335 lines, 13 tests, 10 `expect(`) renders `ModifyTeamSettings` and
`ModifyProjectSettings` and uses `pastTheAutosaveDelay`. It is on the same form but tests the usage-data event of saving
several parent sources, not any Refinement section or mapping rule, so no move, removal or merge of slice A touches it. It
is recorded in `baseline/outside-seven/` so "unmodified" is pinned for it too; it is not rewritten.

## Before figures (summed per-test seconds, one run on a quiet machine, load 0.7 to 2 during the runs)

Source: `assertionResults[].duration` of the 330-test run.

| File | Tests | Summed s (local, today) | FE7 figure in DESIGN |
|---|---|---|---|
| `ModifyTeamSettings.refinementNeed` | 47 | 14.57 | 14.8 |
| `ModifyTeamSettings.readiness` | 24 | 12.08 | about 14 |
| `ModifyTeamSettings.test` | 36 | 10.78 | not measured |

Slowest tests today. `refinementNeed`: 1.14 s (keeps an edited Ready rule), 1.00 s (saves a changed Ready rule), 0.73 s
(starting week), 0.72 s and 0.67 s (band explanations), then everything under 0.66 s. `readiness`: 1.40 s (switches a rule
back on ... changed to before), 1.00 s (... it had before), 0.86 s (... refused ... last allowed threshold, down from
2.02 s on CI before 6be1e621e), then everything under 0.70 s.

### Coverage of the Team directory

`LIGHTHOUSE_TIME_BUDGET=off pnpm exec vitest run src/components/Common/Team --coverage
--coverage.include='src/components/Common/Team/**'` (12 files, 244 tests, all passed):

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| FE7 close, reference | n/a | 86.23 | n/a | 96.26 |
| Today, before slice A | 96.32 | 86.72 | 89.58 | 96.16 |

Today's figures, not the FE7 ones, are the bar for A-5: Story 6501 added production code to the directory, so line
coverage moved from 96.26 to 96.16 and branch from 86.23 to 86.72 with no test of this Story involved. "Not lower" at close
means not lower than 96.16 lines and 86.72 branch.

### CI over local

Six saved `test-timings-frontend` artifacts of plain `main` runs (CI runs 41923442666, 41953376165, 42232786032,
42276978526, 42293873647, 42296704538; each 9,438 rows), summed per file by `file` and `duration_ms`. None of them contains
commit 6be1e621e or the Story 6501 commits (checked by ancestry), so every CI figure is from before them.

| File | CI per run (s), 6 runs | CI median | CI max (slow runner) | Local today | median / local | max / local |
|---|---|---|---|---|---|---|
| `refinementNeed` | 19.8, 26.5, 16.7, 24.9, 24.9, 15.4 | 22.3 | 26.5 | 14.57 | 1.53 | 1.82 |
| `readiness` | 17.2, 22.9, 14.9, 21.5, 20.7, 13.8 | 19.0 | 22.9 | 12.08 | 1.57 (not comparable: CI is before 6be1e621e) | see below |
| `ModifyTeamSettings.test` | 12.8, 17.6, 11.9, 16.4, 16.4, 9.9 | 14.6 | 17.6 | 10.78 | 1.35 (CI is before Story 6501) | 1.63 |

The runner is bimodal (about 15 to 17 s against 25 to 26.5 s for `refinementNeed`), as T4 recorded. The budget is set by
the slow-runner reading, so the required cut is read on the slow reading: **cut = 1 - 17.5 / slow reading**.

## Required cut, re-read against today's time

Commit 6be1e621e replaced four real autosave waits (about 2 s of wall clock, which a slow runner does not stretch) with
`pastTheAutosaveDelay` in `switches a rule that was refused ... last allowed threshold`: 2.02 s on a fast CI runner, 0.86 s
here today, so about 1.2 s less. The slow CI reading of `readiness` is therefore estimated as 22.9 - 1.2 = **21.7 s**
(an estimate; no CI run contains the change yet). `readiness` is already faster by that much, so its required cut is smaller.

| File | Slow reading, today (s) | Required cut, to 17.5 s | Design target, to 16.5 s | Local target in seconds (required / design) | Before this re-read |
|---|---|---|---|---|---|
| `refinementNeed` | 26.5 (measured) | **34.0 %** | 37.8 % | 9.6 / 9.1 (from 14.57) | 34 % / 38 % |
| `readiness` | 21.7 (estimate) | **19.4 %** | 24.0 % | 9.7 / 9.2 (from 12.08) | 24 % / 28 % |
| `ModifyTeamSettings.test` | 17.6 (measured, before 6501) | **0.6 %**, unresolvable | 6.3 % | no higher than 10.78 | 1 % / 6 % |

Local target = today's local seconds x (1 - cut). The ratio used for the slow reading is max / local (1.82 for
`refinementNeed`, 1.80 for `readiness`, 1.63 for `ModifyTeamSettings.test`).

`readiness` required r (same arithmetic as `feature-delta.md`, 18 of 24 move, 6 stay, after/before = (6 + 18/r) / 24): for
19.4 % r = 1.35, for 24.0 % r = 1.47. For `refinementNeed` (23 of 31 move) 34.0 % needs r = 1.85 and 37.8 % needs r = 2.04.

## The A-1 gate

One band test of `refinementNeed`, typing included: `marks the high end being typed in when it falls below the low end, and
explains why there` (clears and types "55" into the high end, then asserts `aria-invalid` and the description of both ends
and that the sentence appears once). Form side: `ModifyTeamSettings` with the same mocks and fake clock as the file. Section
side: a throwaway spike file, `RefinementSettingsSection` inside a minimal `useState` host (`onChange` merges the change, as
the form does), the same assertions. The spike file was deleted before this commit; `git status` shows no test file changed.
Each side has a warm-up test in front of the measured one (the first test of a file pays the cold render; the other 46 do
not), and the warm-up figure is shown beside it. Five alternating pairs (order form-section, section-form, ...); a pair is
valid when the four 1-minute load-average samples (start and end of both runs) are all at or under 6 (half of 12 cores) and
every test passed. CPU is user plus system seconds of the whole vitest process, start-up included.

| Pair | Order | Form (ms, warm-up) | Section (ms, warm-up) | r | Load samples | CPU form / section (s) | Valid |
|---|---|---|---|---|---|---|---|
| 1 | form, section | 415 (802) | 260 (544) | 1.60 | 1.81 1.74 1.74 1.74 | 4.3 / 3.9 | yes |
| 2 | section, form | 415 (774) | 273 (544) | 1.52 | 1.76 1.76 1.74 1.76 | 4.3 / 3.7 | yes |
| 3 | form, section | 421 (773) | 260 (597) | 1.62 | 1.76 1.70 1.70 1.73 | 4.3 / 3.8 | yes |
| 4 | section, form | 402 (759) | 281 (537) | 1.43 | 1.73 1.67 1.73 1.73 | 4.2 / 3.7 | yes |
| 5 | form, section | 399 (759) | 262 (535) | 1.52 | 1.67 1.67 1.67 1.70 | 4.2 / 3.6 | yes |

**r = 1.52 (median of 5 valid pairs), below the 1.85 floor.** The spread is narrow (1.43 to 1.62), so this is not a noisy
reading; even the best pair misses. A trial pair before the five read 1.56. The section render is not free: the Section mounts
the state list, readiness, stage rules (which fetch their schema) and cadence as well as the band, so a section render of
one band test still costs 0.26 s against 0.41 s in the form.

What r = 1.52 projects, by the same arithmetic as the design (the 31 form tests at one cost each, 23 moving at c/r):

- `refinementNeed`: cut = 1 - (8 + 23/1.52) / 31 = **25.4 %** against a required 34.0 %: **stops**.
- `readiness`, if the same r held for its tests (an assumption; the gate measured a `refinementNeed` band test only):
  cut = 1 - (6 + 18/1.52) / 24 = **25.7 %** against a required 19.4 % (design target 24.0 %): would pass, with margin.
- `ModifyTeamSettings.test`: stop check of 1 %, which five pairs cannot resolve; see the removal estimate below.

Per the stop rule of this step nothing moves; the readiness projection is a figure for the maintainer at the hold, not a
decision of this step.

## Profile of one band test and one readiness test

`--pool=forks --execArgv=--cpu-prof`, one worker, summarised with FE7's `summarize.py`, one test of each file picked with
`-t`:

- band test (`marks the high end being typed in ...`): test body 0.84 s, whole process sampled 2.4 s. 18.6 % idle (waiting
  in `findBy*` polling and the fake clock), 26 % node core (module compile), 12 % jsdom, 4.3 % react-dom, 2.5 % MUI, 0.7 %
  Lighthouse source.
- readiness test (`saves a changed number of Yes votes ...`): test body 1.03 s, whole process sampled 2.6 s. 27.3 % idle,
  23.6 % node core, 10.7 % jsdom, 3.9 % react-dom, 2.0 % MUI, 0.7 % Lighthouse source.

Both are dominated by DOM work (jsdom, react-dom, MUI) and waiting, not by Lighthouse code, which fits a cost that scales
with how many components mount and how many times they re-render. The process profile includes start-up, so it cannot say
how the 0.84 s splits between mounting and typing; the gate above is what measured that, and it says the section render
saves a third, not half.

## Backstops and the loop rows

### The seven loop rows of `does not auto-save when X is invalid` (`ModifyTeamSettings.test`)

Function side: the `it.each` titled `names %s` in `teamAutoSaveBlockers.test.ts` (describe `what keeps a Team's settings from
being auto-saved`), each row asserting `blockersOf(change)` equals exactly one sentence. The loop's other fields come from
`createMockTeamSettings` with id 1 and name `Test Team`, the function test's from `createMockTeamSettings` alone, so both
start from the same valid settings.

| Loop row (title) | Field and value | Function row (title) | Field and value | Sentence asserted | Pair |
|---|---|---|---|---|---|
| `does not auto-save when Name is invalid` | `name: ""` | `names a missing name` | `name: ""` | "Enter a Name" | yes |
| `does not auto-save when Throughput History is invalid` | `throughputHistory: 0` | `names no throughput history` | `throughputHistory: 0` | "Set a Output History of at least one day" | yes |
| `does not auto-save when Feature WIP is invalid` | `featureWIP: undefined` | `names no feature WIP` | `featureWIP: undefined` | "Set a Deliverable Limit" | yes |
| `does not auto-save when To Do States is invalid` | `toDoStates: []` | `names no To Do state` | `toDoStates: []` | "Add at least one To Do State" | yes |
| `does not auto-save when Doing States is invalid` | `doingStates: []`, `doneStates: ["Done"]` | `names no Doing state` | `doingStates: []` (the default `doneStates` is `["Done"]`) | "Add at least one Doing State" | yes |
| `does not auto-save when Done States is invalid` | `doneStates: []` | `names no Done state` | `doneStates: []` | "Add at least one Done State" | yes |
| `does not auto-save when Work Item Types is invalid` | `workItemTypes: []` | `names no work item type` | `workItemTypes: []` | "Add at least one Task Type" | yes |

All seven pair, by field, value (an empty list against an empty list, `undefined` against `undefined`, `0` against `0`)
and sentence. The DESIGN's assumption that two rows are not covered by a function test does not hold: **none is uncovered**.
The check therefore allows up to **7** loop rows to go, not 5. The number removed is a design choice on top of that, kept
here at 5 with 2 rows left as the form's own proof that the autosave gate is fed by `teamAutoSaveBlockers` (Name, and Work
Item Types, whose result also depends on the form passing the connection's schema). That choice is for A-4, if it ever runs.

### The four mapping-warning sentences (`blocked autosave feedback` against `stateMappingValidation.rawLists.test.ts`)

| Form test (title) | Sentence the form shows | Function test (title) that builds the same input and asserts it | Pinned |
|---|---|---|---|
| `holds back autosave while a mapping is named after a Doing state it does not group, and saves once it is renamed` (kept) | `Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.` | `refuses a name that sits beside one of its mapping's states, with the sentence the server sends` and `holds back the autosave for that name with the same sentence`; the constant `SHADOWS_A_DOING_STATE` is byte-equal to the form's sentence | yes |
| `holds back autosave while a mapping's name hides one of its states in To Do, and saves once it is renamed` | `... has the same name as the To Do state "In Arbeit". Choose another name.` | `names the To Do list when the shadowing sits there`, same mapping name, same list, same sentence | yes |
| `holds back autosave while a mapping's name hides one of its states in Done, and saves once it is renamed` | `... has the same name as the Done state "In Arbeit". Choose another name.` | `names the Done list when the shadowing sits there`, same | yes |
| `holds back autosave while a mapping is named after a state another mapping groups, and saves once it is renamed` | `Mapping "In Arbeit" has the same name as a state grouped by mapping "Group". Choose another name.` | `refuses a mapping that groups another mapping's name, with the sentence the server sends`: same template with the mapping names `A` and `B` | yes, as a template: the two names in it are different, so a swap of the two slots would fail it |

Nothing is unpinned, so **the backstop list is empty and no backstop test is written** (no commit of type `test`).

### The one-render mapping test

The form lists every reason, not the first: `useModifySettings` passes the output of `teamAutoSaveBlockers` as
`formInvalidReasons`, the form renders `formInvalidReasons.map(...)` under `settings-blocking-warning`, and
`mappingRuleErrors` concatenates the four validators. A throwaway call of `mappingAutoSaveBlockers` with five mappings (one
of each kind: Doing, To Do, Done, and one named after a state another mapping groups) returned all four sentences in one
array; that file was deleted. So the one-render test of A-4 is possible and the three wiring tests may go if A-4 runs.

### What removing the mapping wirings and loop rows would save (for the maintainer; not done)

From the 330-test run: the three removable mapping tests 0.54 + 0.55 + 0.54 = 1.63 s; the five loop rows to go (Throughput,
Feature WIP, To Do, Doing, Done as the cheapest choice) about 0.82 s; the one new test about 0.55 s. Net about 1.9 s of 10.78
(17 %), well over the 1 % this file needs, but the file is not the one that blocks the budget.

## Form tests per section (counted from the files)

`refinementNeed`: 47 tests = 31 render the whole form + 16 already render one unit or call a function. The 31: 6 stage
rule, 7 cadence, 17 band (counting the 4 rows of the two `it.each`), 1 picker. The tests that stay on the form: stage 2
(`saves a changed Ready rule and leaves the Being refined rule as it was`, `saves nothing while a new rule is still
incomplete`), cadence 3 (`saves Thursdays every week`, `asks for the starting week ... and saves it`, `saves nothing while
every second week names no starting week, and marks the field`), band 2 (`saves a changed high end with the low end as it
was`, `refuses a decimal likelihood instead of saving it rounded, and saves nothing`), picker 1 = 8. So **23 of 47 move is
confirmed**: 4 stage + 4 cadence + 15 band.

`readiness`: all 24 tests (16 `it` and 8 `it.each` rows) render the whole form (24 calls of `renderGravitysSettingsForm`).
6 stay (the five wiring tests and `switches a rule back on at the threshold it had before`), so **18 of 24 move is
confirmed**.

## Backstop list

Empty. Every mapping-warning sentence and each of the seven invalid scenarios is pinned by a function test (tables above).

## Conclusion for the hold

- r = 1.52 < 1.85: slice A stops at A-1, nothing moves, no harness commit.
- `refinementNeed` cannot reach 34 % this way (projected 25.4 %).
- `readiness` has become easier: its required cut fell from 24 % to 19.4 % because 6be1e621e removed about 1.2 s of real
  waiting, and r = 1.52 would project to 25.7 %, which is over even the 24.0 % design target. That is a projection from a
  `refinementNeed` band test, not a measurement of `readiness`.
- `ModifyTeamSettings.test` has room (about 17 % by removing mapping wirings and loop rows) but needs only a trim.
- The maintainer's options at the hold are: (a) stop slice A here; (b) run `readiness` alone through A-2 as its own measured
  stop check, accepting that `refinementNeed` is recorded as target not met; (c) look for a cheaper lever for
  `refinementNeed` (the form's remaining cost is the whole-form mount and the typing, not the assertions).

# Step 01-02: harness, then readiness display and validation to the section render (2026-11-07)

Code before-commit `1ffe2be77`. The harness is commit `342915d95` (test-only, before any move). Measured from
`Lighthouse.Frontend`, `LIGHTHOUSE_TEST_WORKERS=2`, `TZ=Europe/Zurich`, `test-results.json` deleted before every run.

## Moves

`readiness` has 24 tests. 18 moved to `ReadinessSettings.test.tsx` with their titles unchanged (display, refusal messages,
`aria-invalid`, `min`/`step`, remembered thresholds, table-driven refusals). 6 stay on the form unmodified: saves a changed
number of Yes votes with the rest as it was, starts a Team that has never chosen refinement, keeps the chosen refinement
states, names readiness as what keeps the settings from being saved, the toggle-memory test (the one that proves the toggle
reaches the saved value), and saves nothing just because the form was opened. 34 `expect(` lines removed from the form file,
34 rows. `check-behaviour-free.sh` exits 0 (`.not.` removed 5, added 1, in rows 5). `run-touched-files.sh` ends OK: 18 files,
562 of 562 passed, baseline ids 558, after-run 562, missing 18 (all with rows), new 22 (all listed).

## Deliberate breaks (one at a time, restored each time, `git status` clean of production files after)

| Break | Test that failed |
|---|---|
| The host ignores `onChange` (helper) | helper: `shows a change the admin makes as its new value`, `builds a second edit on the first edit's result, as the form does`, `lets a test read the value the admin has now`; with `ReadinessSettings.test.tsx` the 16 moved tests that edit or read a value |
| The readiness toggle stops remembering thresholds (`rememberOnSwitchOff` replaced by the old map) | form: `switches a rule back on at the threshold it was changed to before it was switched off` |
| The readiness handoff from the form to the section (`RefinementSettingsSection` passes `DEFAULT_READINESS`) | form: `saves a changed number of Yes votes with the rest of readiness as it was`, `starts a Team that has never chosen refinement at the default readiness, and saves it with no states`, `keeps the chosen refinement states when readiness changes`, `switches a rule back on at the threshold it was changed to before it was switched off` |
| The error mark on Yes votes needed (`error={false}`) | section: `marks Yes votes needed as refused and leaves Voters needed unmarked` |

No break left without a failing test, so no test was added. Finding: the moved section tests start with the stored thresholds
remembered, so only the form's toggle-memory test fails when remembering is broken; that test is one of the six that stay.

## Stop check for `readiness`: passes with margin

Summed per-test seconds (`assertionResults[].duration`), 5 alternating pairs, 5 of 5 valid (every 1-minute load-average
sample between 0.89 and 2.19, limit 6). Before is the old file run from a copy; after is the form file (6 tests) plus the
section file (18 tests), 24 tests each side. The helper's 4 tests (0.4 s) are not counted on the after side; with them the
cut is about 52 %.

| Pair | Before s | After s | Before CPU user+sys | After CPU user+sys |
|---|---|---|---|---|
| 1 | 11.10 | 4.92 | 10.20 | 7.85 |
| 2 | 11.09 | 4.96 | 10.11 | 7.82 |
| 3 | 11.11 | 4.98 | 10.14 | 7.80 |
| 4 | 11.13 | 4.90 | 10.19 | 7.71 |
| 5 | 11.05 | 4.95 | 10.10 | 7.78 |

Median 11.10 to 4.95: cut **55.4 %** against the required 19.4 % and the design target of 24 %. The file does not stop.
Step 01-03 is not blocked.

# Step 01-03: refinementNeed stage, cadence and band tests to the section renders (2026-11-07)

Code before-commit `1ffe2be77`; the move is commit `9e3995dbe`. Measured from `Lighthouse.Frontend`,
`LIGHTHOUSE_TEST_WORKERS=2`, `TZ=Europe/Zurich`, `test-results.json` deleted before every run.

## Moves

`refinementNeed` had 47 tests. 23 moved with titles unchanged: 4 stage rule tests to `StageRulesSettings.test.tsx` (one of
them, `keeps an edited Ready rule when the Yes votes needed change before it is saved`, renders the whole Refinement section
because it edits two of its parts), 4 cadence tests to `RefinementCadenceSettings.test.tsx` (new, beside the existing
function test file) and 15 band tests to `RefinementBandSettings.test.tsx`. 8 wiring tests stay on the form unmodified: stage
`saves a changed Ready rule ...` and `saves nothing while a new rule is still incomplete`; cadence `saves Thursdays every
week`, `asks for the starting week ... and saves it`, `saves nothing while every second week names no starting week`; band
`saves a changed high end ...` and `refuses a decimal likelihood ... saves nothing`; the picker test. The 16 tests that
already rendered one unit or called a function are untouched. That makes one saves-nothing-while-invalid test per section
kind on the form: stage rule, cadence, band (and readiness from 01-02). 47 `expect(` lines removed, 47 rows;
`check-behaviour-free.sh` exits 0; the id check ends OK (23 moved ids have rows, 24 new ids listed: 23 moved plus the added
test below). Nine band-explanation lines (`toContain`) are listed under "Accepted matcher hits": moved verbatim.

## Deliberate breaks (one at a time, restored each time; production files clean after)

| Break | Tests that failed |
|---|---|
| `refinementSettingsBlockers` returns `[]` | form: `names readiness as what keeps the settings from being saved`, stage `saves nothing while a new rule is still incomplete`, band `refuses a decimal likelihood ...`, cadence `saves nothing while every second week names no starting week ...`; `refinementNeed`'s two `What holds back the save` function tests |
| `teamAutoSaveBlockers` returns `[]` | the same four saves-nothing tests, one per section kind (readiness, stage rule, band, cadence) |
| band handoff to the form replaced by `onChange({})` | form: `refuses a decimal likelihood ...`, `saves a changed high end ...` |
| stage rules handoff replaced by `onChange({})` | form: `saves a changed Ready rule ...`; section: `keeps an edited Ready rule when the Yes votes needed change ...` |
| cadence handoff replaced by `onChange({})` | form: `saves Thursdays every week`, `asks for the starting week ... and saves it`, `saves nothing while every second week names no starting week` |
| the form loses the unchanged part of the refinement value (`...teamSettings?.refinement` removed) | **none failed**: a finding |
| band error mark removed (`error={false}`) | section: `marks only the low end ...`, `marks the high end being typed in ...`, both `refuses ... outside 50 to 95`; form: `refuses a decimal likelihood ...` |
| cadence limit of 52 weeks raised to 520 | section: `saves nothing for more than 52 weeks between Refinements, and says why` |
| stage rule spread broken (`{ [stage]: rule }`) | section: `switches the Ready rule off ...`, `keeps an edited Ready rule ...`; form: `saves a changed Ready rule ...`; the two single-Team stage tests (`saves a stage rule switched to match any condition`, `saves the first condition of an empty stage ...`) |
| picker given no mappings | `the refinement picker offers a mapping's name, not its states` |

Finding: with the form losing the unchanged part of the refinement value, no test failed. Before the move,
`keeps an edited Ready rule when the Yes votes needed change before it is saved` caught it on the form (it saves two edits and
compares the whole saved refinement). On the section it cannot, because the section's host merges the change itself. A form
test was added before the commit: `keeps the stage rules, cadence and readiness as they were when the band changes`, which
changes the band on a Team with stage rules and a cadence and compares the whole saved value. With the break applied it
fails; the break row above is the run before the test existed.

## Stop check for refinementNeed: passes with margin

Summed per-test seconds (`assertionResults[].duration`), 5 alternating pairs, 5 of 5 valid (every 1-minute load-average
sample between 1.96 and 2.62, limit 6). Before is the old file run from a copy; after is the form file (25 tests: 8 wiring tests, the added one,
the 16 that already rendered one unit) and the three section files (23), 48 tests against 47.

| Pair | Order | Before s | After s | Before CPU user+sys | After CPU user+sys |
|---|---|---|---|---|---|
| 1 | before, after | 13.46 | 7.91 | 13.51 | 13.78 |
| 2 | after, before | 13.45 | 7.90 | 13.33 | 13.74 |
| 3 | before, after | 13.35 | 7.93 | 13.22 | 13.77 |
| 4 | after, before | 13.30 | 7.91 | 13.21 | 13.79 |
| 5 | before, after | 13.30 | 7.81 | 13.18 | 13.59 |

Median 13.35 to 7.91: cut **40.8 %** against the required 17 % (and the design target of 25 %). The file does not stop.
The summed seconds count tests only; the process CPU is not lower because four files start up where one did.

# Step 01-05: close slice A, after figures and every proof (2026-11-07)

Code before-commit `1ffe2be77`. Step 01-04 (`ModifyTeamSettings.test`) was dropped on the maintainer's word: that file is
unchanged in this Story, so its proofs and the "up to 7 of 36" loop-row figures of the change table do not apply.
Mutation testing is N/A: no production file changed.

## After figures (medians of the 5 alternating pairs of the stop checks, summed per-test seconds, local, 2 workers)

| File | Before s | After s | Cut | Required | Result |
|---|---|---|---|---|---|
| `ModifyTeamSettings.refinementNeed` | 13.35 | 7.91 | 40.8 % | 17 % | target met, no file stopped |
| `ModifyTeamSettings.readiness` | 11.10 | 4.95 | 55.4 % | 19.4 % | target met, no file stopped |
| `ModifyTeamSettings.test` | n/a | n/a | n/a | none | step dropped, file unchanged |

No file is named as "target not met". (The 01-01 before figures of 14.57 s and 12.08 s came from a single run; the
stop-check medians above are the like-for-like pairs the cuts are computed from.)

## Proofs against `1ffe2be77`

| Proof | Result |
|---|---|
| `check-behaviour-free.sh` | exit 0. No production, end-to-end or screenshot file in the diff; `.not.` removed 17, added 7, in rows 16; removed `expect(` 34 of 34 (readiness) and 47 of 47 (refinementNeed) have rows; row text matches. No skip, only, todo, skipIf, runIf, looser matcher or longer timeout added |
| `run-touched-files.sh` | ends OK: 21 files, 563 of 563 passed, vitest exit 0, baseline ids 558, after-run 563, missing 41 (all with rows), new 46 (all listed). No outside-seven id list changed |
| Coverage, Team directory (`--coverage.include='src/components/Common/Team/**'`, 16 files, 245 tests) | 96.32 stmts, 86.72 branch, 89.58 funcs, 96.16 lines: equal to the 01-01 reading (96.16 line, 86.72 branch), not lower |
| Deliberate breaks | all recorded above under steps 01-02 and 01-03, each with the failing test and the restore; the one break that no test caught (the form losing the unchanged refinement value) led to the added form test `keeps the stage rules, cadence and readiness as they were when the band changes`, which fails with the break applied |
| `pnpm build` | exit 0; Biome checked 1303 files, no fixes applied; `tsc -b` and `vite build` clean; no `*/docs` symlink under the project; `git status` clean afterwards (no rewritten files) |
| `pnpm test` (whole frontend, `LIGHTHOUSE_TIME_BUDGET=off`) | 618 of 618 files, 9498 of 9498 tests passed, 233.8 s |
| Mutation testing | N/A: no production file changed (accepted by the maintainer) |

The budget stays 56 s until the budget commit after the push. Slices B and C are not part of this Story.
