# Feature: story-6561-t13-slowest-frontend-test-files

Story #6561 · T13 The slowest frontend test files run faster, and the frontend budget comes down with them · part of
Epic #6506 "Fast, reliable test suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-46](../epic-6264-modular-monolith/slices/slice-46-t13-slowest-frontend-test-files.md).

Created 2026-11-06 on the maintainer's word, as a separate Story after T4 #6462 pushed. DISCUSS is the brief plus this
file, following FE7's framing, because the Story applies an existing contract to more files. Waves start at DESIGN.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md` and
`docs/feature/story-6457-fe7-fast-frontend-tests/feature-delta.md` (the behaviour-free contract for test rewrites);
T4's derivation in `docs/feature/story-6462-t4-test-budgets-fail-the-build/feature-delta.md`.

- job_id: infrastructure-only. infrastructure_rationale: the budget cannot fall while seven files take 17 to 27 s.
- Tests change by design; production code, behaviour and pixels do not.
- Cross-cutting: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, Terminology: N/A, because only test
  files and the budget file change. Context map: no change. Release notes: none.

## Baseline re-count

Measured 2026-11-06 on the 7 plain runs of T4 (see the brief's table). Frontend budget 56 s, empty allow-list.

## Checklist

- [ ] Behaviour-free: production code and pixels unchanged
- [ ] Every removed assertion has a row saying where it lives on
- [ ] Coverage not lower (`pnpm test:coverage`, opt-in for exactly this reason)
- [ ] Each call site proven by a deliberate break
- [ ] Ratchet: the budget falls in this Story
- [ ] Mutation last, on frozen code; the touched area's score does not drop
- [ ] `pnpm build` and `pnpm test` clean before review

## Wave: DESIGN / [REF] Context-map placement and cross-cutting

Propose mode, maintainer AFK: the recommended option was taken for every engineering choice and is recorded in
"Decisions". Read-only design: nothing was run, so every timing statement below is a hypothesis read from code and
from FE7's and T4's measurements, and the first step of each slice measures it.

- Contexts touched: none. Test files and, in one optional case, one pure function moved out of a hook. No concept, term,
  edge or domain event is added or changed. ADR-270 and `ARCHITECTURE.md` section 4: no update owed.
- UI surface and the loading and failure look: N/A, because no screen changes. The tests that pin those looks in
  `BaseMetricsView.loading.test.tsx` and `OverviewDashboard.test.tsx` keep every assertion (named in the per-file reading).
- RBAC: N/A, because no rule changes; `useRbac()` stays the only gate. Overview's fallback extraction (D-6561-9) would
  take values from `useRbac()` and fetch nothing.
- Usage-data event, Clients, Website, Terminology, release notes: N/A, as in DISCUSS. Rewritten tests keep asserting the
  seeded default terms.
- Improvement backlog (`/continuous-improvement before 6561`): not run in this read-only pass. The orchestrator runs it at
  DELIVER pick-up; FE5 #6393 is the one item known to matter (Q1).

## Wave: DESIGN / [REF] What FE7 teaches, read for this Story

From `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md` and the FE7 workspace, by what this Story reuses.

**The contract (inherited, linked not copied):** behaviour-free; a moved-assertions row for every removed or shrunk
`expect(`; test-id diff before and after; no `.skip/.only/.todo`, no looser matcher, no longer timeout; line and branch
coverage of the touched directory not lower; the extraction is its own `refactor(...)` commit with no test change;
mutation once, last, on frozen code, over production files an extraction touched.

| FE7 slice | Lever | Measured change | Reading for T13 |
|---|---|---|---|
| 9 WorkItemsDialog | 65 of 108 rows moved to 88 direct tests of an already-exported function | -63.3 % | Best result: replace many whole-component renders by function calls |
| 10 ModifyProjectSettings | all real waits to a fake clock, a blocker function extracted | -49.0 % | Fake clock paid only next to an extraction |
| 7 RefinementView | paging arithmetic extracted, one 143-row render left | -34.5 % | The same shape |
| 8 EditConnection | two functions extracted, 20 form tests removed | -21.3 % | The same shape, smaller |
| 5 Reports pages | pick limit and select-all as functions | -22.7 % | This is the family `ReportsPage.removal` belongs to; what is left is the dialog and the page wiring |
| 6 Reports tab and wizards | fake clock, three extraction files | -12.4 % (-4.4 % under equal load) | Little payoff where no rule sat in the component |
| 4 ModifyTeamSettings family | fake clock, three extractions, 30 tests moved | -13.2 % | **The three T13 files are what slice 4 left behind**: its own record says the remaining tests "are slow for other reasons (full form render plus `userEvent` typing)" and "the lever is testing the logic directly" |
| 11 OverviewDashboard, BaseMetricsView | none: dropped on measured payoff | n/a | Profile: no real wait, flat tail, 34-47 % of CPU in import and setup, under 2 s reachable by extraction; **it did not weigh a cheaper render unit** (D-6561-8, D-6561-9) |

What saved time: moving a rule onto a function that already existed or was cheap to extract, and rendering one wiring per
flow. What did not: fake timers alone (slice 4's sleeps were about 5.5 s across the whole suite), configuration (nothing
passed the A/B rule), and any slice with no rule inside the component.

What FE7 measured wrong, and what T13 does differently:

1. Its estimate of "about half of the summed seconds" was reached for 2 of 7 families. T13 states a **required cut per
   file** from the budget arithmetic (below) and stops a slice early when step 2 projects a cut below it (D-6561-10).
2. Per-slice figures were taken on a shared machine (load 10-14 on 4 cores) and called indicative; the final wall median
   and the five-run CI read were dropped, so FE7 proved no payoff on CI. T13's proof is the budget step itself (the AC is
   a CI reading), plus local alternating pairs with load recorded (D-6561-11).
3. CI-second ceilings were first read as measurements. Here, the brief's table mixes runner speeds (next section).
4. Slice 11 was judged on extraction alone. The profile (render 27 %, MUI 13 %) said the cost is mounting the dashboard,
   which a smaller mounted unit answers; T13 tests that as a hypothesis with a spike before any rewrite.

## Wave: DESIGN / [REF] The target, as arithmetic

The budget rule (ADR-237 as T4 applied it): B is at least 2.1 x L, L the largest file time over the runs. 2.1 x 17.5 gives
36.75, so B = 37 is the floor while `ReportsTab.createWizard.test.tsx` sits at 17.5 s. **Making the seven files faster than
that buys nothing**: the budget stops at that file. The aim is therefore "each of the seven under the line, with margin",
not "as fast as possible".

The brief's table gives each file's max and median over 7 runs. T4 recorded that the frontend runner is bimodal
(whole-run totals 649-793 s and 995-1054 s, a 1.62 ratio) and that the 26.5 s maximum is a **slow-runner reading**; the
largest file in fast-runner runs was 16.7 s. So the maxima below are slow-runner readings, and the medians mix both modes.
The required cut is a ratio, the same in both modes:

| File | Max (s) | Cut to reach 17.5 | Cut with 1 s margin (16.5) | Local summed s (FE7, quiet-ish) |
|---|---|---|---|---|
| `ModifyTeamSettings.refinementNeed` | 26.5 | 34 % | 38 % | 14.8 (47 tests) |
| `ModifyTeamSettings.readiness` | 22.9 | 24 % | 28 % | about 14 (47 tests) |
| `OverviewDashboard` | 22.0 | 20 % | 25 % | 9.2 (51 tests) |
| `BaseMetricsView.loading` | 21.7 | 19 % | 24 % | 9.3 (133 tests) |
| `ReportsPage.combinedPdf` | 21.0 | 17 % | 21 % | not measured by FE7 (19 tests) |
| `ReportsPage.removal` | 20.2 | 13 % | 18 % | 9.0 (21 tests) |
| `ModifyTeamSettings.test` | 17.6 | 1 % | 6 % | not measured (33 tests) |

Local seconds are FE7's, from before its slices where they were touched, so they are only a scale: CI/local is about 1.6
to 1.75 for the two files with both numbers (Overview 14.3 / 9.2, `BaseMetricsView.loading` 16.3 / 9.3, both CI medians).
Step 1 of each slice recomputes the ratio per file from the saved CSVs of T4's runs and states the local target in
seconds. **The budget can only fall as far as the worst file left**: if `BaseMetricsView.loading` stayed at 21.7 s the
budget would be 46 s, not 37 (Q3).

## Wave: DESIGN / [REF] Per-file reading (hypotheses, each measured at its slice's first step)

Nothing here is measured. "Likely cost" comes from reading the test and the component; the profile method of FE7 slice 11
(`--pool=forks --execArgv=--cpu-prof`, summarised by `measurement/profile-2026-11-04/summarize.py`) or one single-test
timing decides each before any edit.

### ModifyTeamSettings.refinementNeed.test.tsx (1,117 lines, 47 tests)

- **Likely cost.** About 31 tests render the whole `ModifyTeamSettings` form to check one Refinement section: 6 stage rule
  tests, 7 cadence, 17 band (aria-invalid, descriptions, tooltips). The form mounts `ForecastSettingsComponent`,
  `StateMappingsEditor`, `CycleTimesEditor`, `WaitStatesEditor` and `FlowMetricsConfigurationComponent` unstubbed (only
  General, WorkItemTypes, States, Tags and AdvancedInputs are mocked), behind three async loads and `useModifySettings`. Every
  typed character re-renders the whole form through `updateSettings`. `replaceTheNumberIn` and `replaceTheLikelihood` clear
  and type, so a two-digit number is three renders of the form. `pastTheAutosaveDelay` advances the fake clock 500 ms
  with `act`, in 14 tests.
- **FE7 pattern.** Slice 4's "test the logic directly" has been used up (`bandErrors`, `cadenceErrors`, `stageRuleOf`,
  `hasIncompleteStageRule`, `refinementSettingsBlockers` are tested as functions). What is left is a second pattern, new to
  this Story: **render the unit that owns the behaviour.** `RefinementBandSettings`, `RefinementCadenceSettings`,
  `StageRulesSettings` and `RefinementSettingsSection` take plain props (`band`, `cadence`, `stageRules`, `onChange`), and
  the file already renders `StageRulesSettings` and `RefinementCadenceSettings` directly in its last blocks.
- **Pins a rule (move to the section render, no form):** band field marks and descriptions (inverted, out of range, decimal,
  cleared shows empty), the explanation tooltip texts (last valid band), the heading in the Team's words, cadence field
  marks and the 52-week sentence, weekday order. These assert what the section shows for a value, not what the form saves.
- **Proves wiring (stays on the form, one per section):** "saves a changed Ready rule and leaves the Being refined rule as it
  was", "saves Thursdays every week", "asks for the starting week ... and saves it", "saves a changed high end with the low
  end as it was", plus one "saves nothing while a section is invalid" per section kind, because the form's autosave gate
  (`teamAutoSaveBlockers` through `refinementSettingsBlockers`) is only proven by a rendered form. "Saves nothing just because
  the form was opened" stays.
- **Production change needed:** none. The sections are exported with plain props. A test-only stateful host (a `useState`
  wrapper that hands the section back what it changed, as the form does) is the one new piece (D-6561-6).
- **Risk to the target.** The cut is 34-38 %, the largest of the seven. Moving about 25 of 31 form tests to a unit render at
  perhaps a third of the cost is the estimate that reaches it, and it is a hypothesis: the spike in A-1 times one band test
  both ways before anything moves. If it does not reach, flag it (D-6561-10); do not split the file (D-6561-3).

### ModifyTeamSettings.readiness.test.tsx (561 lines, 47 tests counting `it.each` rows)

- **Likely cost.** The same whole-form render and clear-and-type cycle. `renderGravitysSettingsForm` waits for
  `GeneralInputsComponent`, then each test types into number fields; the `it.each` rows (4 + 2 + 2) each pay a form render.
- **FE7 pattern.** Same as above; `ReadinessSettings` takes `readiness` and `onChange`, and the pure parts
  (`readinessErrors`, `discussionRules`) already have direct tests from slice 4.
- **Pins a rule (section render):** defaults shown, "refuses '0' / '' in a threshold, says why", marks `aria-invalid` on the
  refused field and leaves the other unmarked, describes the field by why, `min`/`step` of the numbers, the remembered-threshold
  round trips (the toggle memory is state in `ReadinessSettings`, so it needs the render but not the form).
- **Proves wiring (form, one each):** "saves a changed number of Yes votes with the rest of readiness as it was", "starts a
  Team that has never chosen refinement at the default readiness, and saves it with no states", "keeps the chosen refinement
  states when readiness changes", "names readiness as what keeps the settings from being saved", "saves nothing just because
  the form was opened". The three "switches a rule back on" tests assert the saved value after each toggle through `waitFor`
  and the debounce; their rule is in the section's toggle memory, so the wiring needs only one of them.
- **Production change needed:** none.

### ModifyTeamSettings.test.tsx (971 lines, 33 tests)

- **Likely cost.** The whole form per test with the cheap mocks of General, States, Tags and WorkItemTypes (buttons), but
  `StateMappingsEditor` real. The least slow of the seven: 17.6 s max against the 17.5 line, so it needs a trim, not a rewrite.
- **FE7 pattern.** Remove duplicate wirings. Four "holds back autosave while a mapping is named after ..." tests (Doing, To
  Do, Done, another mapping) each assert a warning sentence that `stateMappingValidation.rawLists.test.ts` already pins at
  function level with the same text, then the same wiring "held back, saved once renamed". One of the four stays as the
  wiring; the other three go with rows naming that test and the function test (verify at A-1 that every sentence is
  asserted there). The 7-scenario loop "does not auto-save when X is invalid" proves which field blocks; if the 18 tests of
  `teamAutoSaveBlockers.test.ts` already cover each of the seven, the loop shrinks to two rows.
- **Pins a rule:** the four warning sentences (function tests exist); the invalid-field loop (function tests exist, check).
  **Wiring:** held back / released on rename, auto-save after the debounce, no Save button, schema-driven work item types,
  staleness opt-in seed of 5, the refresh-failure "Reload now".
- **Production change needed:** none.

### OverviewDashboard.test.tsx (1,454 lines, 51 tests)

- **Likely cost.** FE7's profile (render 27 %, jsdom 11 %, MUI 15 %, no real wait, median 158 ms, 34 tests over 100 ms)
  says the cost is mounting the page, which holds two `DataOverviewTable`s (each a DataGrid with toolbar and chips), an
  `OnboardingStepper`, `FilterBar` and the providers, under `TerminologyProvider` and a query client, with six mock services
  built per render. Most tests (about 35 of 51, counted at C-1) read header buttons, alerts, the stepper
  and the connections section, and none of them reads a table row.
- **FE7 pattern.** Not an extraction first. The new lever is **a smaller mounted unit for the tests that do not read a
  row**: a second test file that stubs `DataOverviewTable` and renders the real page around it. This is a test-only
  change, but it moves tests between files, so ids change and each moved test gets a row (a pure move commit first).
- **Pins a rule that could be a function (extraction is the fallback, D-6561-9):** which header actions show and enable from
  `rbac`, the licence status and whether connections exist (lines 497-553 of the component, inline JSX conditions). Half a
  dozen tests pin them one case each.
- **Proves wiring (stays with the real tables):** row action gating via the `canEditRow/canDeleteRow/canCloneRow` predicates
  (4 tests), the deletion reporting and report-count tests (about 9, which open the real row's delete), "renders loading state
  initially", "renders portfolios section after loading", "when the connection list is refused" (pins a failure look), and the
  licence-check file is a different file (`OverviewDashboard.licenseCheck.test.tsx`, not in scope).
- **Production change needed:** none for the recommended option. The fallback is a pure function out of the page.
- **Risk to the target.** FE7 put the ceiling of an extraction-only rewrite under 2 s of 9 s (about 20 %), and the cut
  needed is 20-25 %: the stub spike decides (C-1); if it saves less than 20 % of the file, say so and stop.

### BaseMetricsView.loading.test.tsx (1,967 lines, 133 tests)

- **Likely cost.** Already cheap per test: charts, `Dashboard`, `DashboardHeader` and `WorkItemsDialog` are stubbed, buttons
  are found by text, a held metrics service answers on command. What remains is mounting `BaseMetricsView` (2,074 lines,
  untouched, FE5 #6393 would split it) and its widget shells, **133 times at a median of 59 ms** (FE7 profile: 27 % React
  and React DOM, 13 % MUI, 34 % import and setup). The count is the cost.
- **Where the count comes from (read, not measured).** Four `it.each` families run one render for every owner x category row
  (`everyOwnerAndCategory`: 2 owners x the categories): "opens with every chart's frame in its place" (about 2 x
  `getCategories().length` rows), "adds and removes no chart once its data has arrived" (the same rows, plus a second load),
  "on a team's/portfolio's X is asked for without retrying" (the same rows, plus a load), and, partly, the per-category
  rows at 1313-1323. The rows differ only in the category, and three of them render and load the same dashboard to look at
  different things.
- **FE7 pattern (new combination).** (a) **One render per owner x category for read-only checks**: one test opens the
  dashboard, asserts frames and spinners at first sight, loads everything, then asserts no chart was added or removed. That
  merges two families into one, so the render count of those rows halves; every old test name gets a row. (b) **The query
  options rule belongs on a hook**: `metricsQueryOptions` (staleTime 0, gcTime 0, retry false, no refetch) is one exported
  constant in `useMetricsData.ts`; "asked for without retrying, and kept for no other window" is therefore a hook-level
  `renderHook(useMetricsData)` test with the app's default query client over every fetch key (the hook's own fallback
  `allMetricsFetchKeys` covers every widget), for each owner, plus **one** page-level row to keep the seam proved (a hook
  that is correct and a widget that bypasses it would pass the hook test; see the memory note on untested seams). That
  removes about 2 x the category count of full renders and loads.
- **Pins a rule:** the query options (above); the window-picking arithmetic is already in `dateWindow`. **Proves wiring, and
  pins the loading and failure looks (every one stays, named by FE7's slice 11 table):** a dashboard as it opens, the dim
  under a spinner while the window changes, answers out of order, one failed request marks only its chart and comes back on
  retry, the aging chart's missing Doing columns.
- **Production change needed:** none, and `BaseMetricsView.tsx` stays untouched while FE5 is open (the rule FE7 set).
- **Risk to the target.** This is the file most likely to miss. Merging the two families and moving the options rule could
  remove perhaps 20 of the 45 rows that render per category, a cut of about 15 %; the cut needed is 19-24 %. If the
  measurement at C-1 says it cannot reach, record the reason (the AC allows it) and the budget floor moves to 2.1 x that file
  (Q3). Do not change what the 133 tests pin (FE7's finding: mounting one category instead of all changes the pinned looks).

### ReportsPage.removal.test.tsx (593 lines, 21 tests)

- **Likely cost.** `renderTheReportsPage` mounts the page with the query client, router, rbac, licensing, terminology and
  the DataGrid; every test then picks rows through `selectTheRowOf`, which finds a row with `rowOf`. **`rowOf` runs
  `getAllByRole("row")` and, for each row, two `queryByRole("link", { name })` calls**: a role query inside a per-row loop,
  the pattern `docs/ci-learnings.md` ("Use the cheap query unless the role is what you are testing") says never to write,
  and the cost grows with the rows. `pickTheThree` calls it three times, and nine tests call `pickTheThree` or the same
  picks. Then tooltips, dialogs and `findByRole("dialog")`.
- **FE7 pattern.** Slice 5 already moved the pick limit to functions. Two new levers: (a) **a cheaper `rowOf`** (EXTEND the
  shared kit: find the row by its text and read the checkbox with a cheap query), which helps all four files that import it;
  (b) **render the dialog or the hook that owns the behaviour**: `RemoveSelectedDialog` takes `reports`, `onCancel`, `onDone`,
  and `useSelectionRemoval(picks, rbac)` takes plain values, so most of this file needs neither the page nor the grid.
- **Pins a rule (move to the dialog or hook render):** the confirmation lists every report in order; the warning when one Then
  cannot be rebuilt, the plural, the no-warning cases (4 tests, `lostForGoodWarningOf` is private to the dialog but reached by
  rendering it); the reason Remove is disabled (3 tests: `removeBlockedBecause` is a tested function, the notAdmin count
  is in `useSelectionRemoval`); the result headline and the named failure; "somebody else removed first counts as removed";
  "cannot confirm what was removed"; the dialog cannot be closed while under way; Escape on confirmation and on result; "each
  report the removal took is forgotten" (the cache keys, `useBulkReportRemoval`).
- **Proves wiring (page, stays):** pick three on two Teams and a Portfolio, confirm, and they leave the list; Cancel removes
  nothing and keeps the list; pressing Remove asks nothing while permissions failed; changing the filter clears the picks; sorting
  keeps them; one "a report the server did not remove stays listed" through the page.
- **Production change needed:** none.

### ReportsPage.combinedPdf.test.tsx (612 lines, 19 tests)

- **Likely cost.** The same page mount and `selectTheRowOf` per test, with two to five picks each; the real `PrintFrameHost`
  then **draws the actual reports into a hidden frame** (`ReportPrintBundle`, a style cache into the frame, `CssBaseline`),
  for every test that lets reads answer: about 12 of the 19. Reading a report document is probably the larger share of these
  tests; the grid and `rowOf` the smaller. (Hypothesis: a CPU profile of two tests at B-1 splits them.)
- **FE7 pattern.** (a) The cheaper `rowOf` of the removal file (shared). (b) **Rules on pure functions**:
  `useReportsPdfExport` holds two private pure functions, `failedAmong` (which listed reports a failure names, matched by
  id and owner) and the decision inside `leaveOutTheFailedOf` (offered only when something failed and something would be
  left; leaves out only the failed, not another report of the same Team). Five page tests pin them by drawing real
  reports (lines 405-508). Extracting them is a **production change** (D-6561-4) and the only one in this Story that is
  recommended. (c) **`NoPdfDialog` rendered alone** for the button sets: Cancel, Leave it out, Retry against Cancel, Retry.
- **Pins a rule:** "Leave it out unpicks every report that failed", "is not offered when it would leave nothing",
  "leaves out only the report that failed, not the other report of the same Team" (3 tests, move to the function tests and the
  dialog); the buttons of the failure dialog (dialog alone). **Proves wiring (page, stays):** two Teams as one PDF each on a
  page of its own; the table's order as sorted; once the dialog closes the frame goes; nothing prints while a report is on its
  way; picks changed while preparing leave the PDF as pressed; the row's PDF icon and Export PDF hold each other back (2 tests);
  preparing look; a reader who is Admin of none may still export; a failing report named; Leave it out unpicks and prints the
  rest (one); Retry; part of the page failed; nothing picked disables with a tooltip.
- **Production change needed:** one recommended pure extraction (D-6561-4). If the maintainer declines it, the three rule
  tests stay as page tests and this file's cut rests on the cheaper `rowOf` and on merging the two "held back" tests.

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-06)

| # | Decision | Options | Chosen (recommended) | Why / trade-off |
|---|---|---|---|---|
| D-6561-1 | What "done" means per file | (a) the brief's line exactly: max over >= 5 plain `main` runs under 17.5 s. (b) the line with 1 s margin (16.5 s). (c) as fast as possible | (b) as the design target, (a) as the AC | The budget floor is `ReportsTab.createWizard` at 17.5 s, so past that nothing is gained; a file at 17.4 s would set the floor with no margin and any slow runner would redden the new budget. The table in "The target" gives the cut each file needs |
| D-6561-2 | How the maxima are read | (a) as plain numbers. (b) as slow-runner readings | (b) | T4 measured the runner as bimodal (1.62) and the 26.5 s as the slow reading. The budget step therefore needs at least one slow-runner run among its >= 5, or it scales the maximum by 1.62 (D-6561-13) |
| D-6561-3 | The lever | (a) render the smallest unit that owns the behaviour (section, dialog, hook), keep one wiring per flow. (b) fake timers only. (c) configuration. (d) split a slow file into two to halve each file's sum. (e) mount fewer widgets in the metrics tests | (a), with FE7's extraction where a rule is still in a component | (b) is what slice 4 did and "did not shorten". (c) FE7 measured nine candidates and none passed the A/B rule. (d) moves seconds between files and keeps the wall time and the worker occupancy, so the Story would meet the letter of the AC and not its title; it stays an open fallback for the maintainer (Q4), not a default. (e) changes what the 133 loading tests pin, so it is not behaviour-free (FE7's finding) |
| D-6561-4 | Production code | (a) none anywhere. (b) none by default, plus one pure extraction in `useReportsPdfExport` (`failedAmong`, `leaveOutPlanOf`) | (b), recommended | The rule "what a failed export may leave out" is private inside a hook and today only provable by drawing reports. Moving two pure functions into `src/pages/Reports/` is a `refactor(reports)` commit with no test change and identical behaviour, then tested in milliseconds and covered by one Stryker run (D-6561-15). (a) would leave three tests drawing real reports. **This is a production change; the maintainer can veto it, and the slice then skips it (Q7)** |
| D-6561-5 | Moves and ids | (a) keep every surviving id. (b) allow a test to move to a new file when the unit it renders changes | (b), with rows | A test that now renders a section or dialog belongs beside it. FE7's contract already allows a moved id with a row; the pure move is its own commit before any edit |
| D-6561-6 | One harness for both Refinement files | (a) a helper in `src/tests/`: a stateful host that renders a section with a value and hands back its changes. (b) copy a host into each file. (c) render the form with more stubs | (a) | Two files and about 40 tests need the same host. (c) leaves the form's own hooks and three async loads in every test. The helper is test-only, named after what it does (`renderRefinementSection`), and carries one plain comment on why it keeps the value in state |
| D-6561-7 | The `rowOf` of `BulkReportsTestKit` | (a) leave. (b) find the row by text and read the checkbox with a cheap query | (b), **only if the spike in B-1 shows a saving**; the four importing files (`removal`, `combinedPdf`, `reading`, `BulkReportWizard.test`) rerun and pass unchanged | It breaks the "never a role query inside a per-row loop" rule today. It is shared, so the universe of the change is those four files; none of their assertions changes |
| D-6561-8 | `BaseMetricsView.loading` | (a) leave and record. (b) merge the read-only per-owner x category families and move the query-options rule to a hook test, measured first | (b), with a stop rule | FE7 dropped the file on an extraction-only ceiling; a merge and a hook test were not weighed. If C-1's spike projects a cut under 19 %, the file is left alone, the reason is recorded, and the budget floor follows it (Q3) |
| D-6561-9 | `OverviewDashboard` | (a) leave. (b) a second test file with `DataOverviewTable` stubbed for the tests that read no row. (c) extract the header-action rules into a pure function | (b) first; (c) only if the spike shows (b) saves under 20 % | (b) changes no production code and keeps wiring proved by the real-table tests that stay; (c) is the textbook "rule on a function" but edits an RBAC-gated page of 725 lines. (b)'s risk is a stub hiding a table error in the moved tests; the remaining real-table tests render the same table |
| D-6561-10 | Stop rule per slice | (a) finish every slice. (b) after step 2 of a slice, compare the projected cut with the file's required cut; below it, stop, record the number and flag | (b) | FE7's slice 11 was dropped at its profile, which was right; but six of eight rewrite slices missed their estimate and ran on. A stop that records "target not met, because ..." is allowed by the AC and cheaper than a rewrite that does not reach |
| D-6561-11 | How a slice measures | (a) FE7's: summed per-test seconds of the touched files, alternating before and after, 5 pairs, on a shared machine. (b) the same, plus the load average before each pair, plus user+sys CPU seconds of the run, discarding a pair whose load exceeds half the cores | (b) | FE7's figures were "indicative" because load swung; CPU seconds react less to a shared machine. Every slice also saves the before id list from a results file, the coverage of the touched directory, and the CI/local ratio of the file (from T4's seven CSVs) so its local target in seconds is stated before the first edit |
| D-6561-12 | Slices and order | one slice per file / by what files share / one slice | By what they share, in the order A ModifyTeamSettings x3, B ReportsPage x2, C OverviewDashboard and `BaseMetricsView.loading` | A holds the two largest maxima and one new harness; B carries the shared kit and the one production extraction; C has the lowest confidence and goes last, with its own stop rule. Each slice is at most one day and ends with its own before and after |
| D-6561-13 | The budget step | (a) a roadmap step. (b) a follow-up commit after the push, once >= 5 `main` runs are in, with the slow-runner rule | (b) | It needs five CI runs after a push, so it cannot be a step of this run (the maintainer pushes nothing in AFK). Rule: B = ceil(2.1 x L) per ADR-237 and T4's recipe; the >= 5 runs are plain, complete (a run that stopped at its first failure is excluded, as T4 did), and include at least one slow-runner run, else L is scaled by 1.62. Provisional value 37 |
| D-6561-14 | T12 #6539 | (a) ignore. (b) re-read each file at slice start; on a conflict with trunk stop and report | (b) | T12 touches some of these files for order and load. Nothing of T12 exists in the repository yet, so which files is unknown (Q2). Never rebase autonomously |
| D-6561-15 | Mutation | (a) Stryker over all touched areas. (b) N/A for tests; Stryker once, last, over `printFailures.ts` if D-6561-4 is taken | (b) | Stryker mutates production code and no production file changes otherwise. If the extraction happens: at least 80 % on the new file, on direct tests, after the code is frozen |
| D-6561-16 | Rigor profile (proposal) | `standard` for all three slices; `thorough` for none | `standard` | Mechanical rewrites under a settled contract; the guard and configuration (the thorough cases in FE7) are not touched. DELIVER asks anyway, and passes `model` explicitly on every dispatch |

## Wave: DESIGN / [REF] Slices (carpaccio, at most one day each)

Every slice's first step declares its universe, saves the **before** ids from a results file, the before seconds (D-6561-11),
the coverage of its directory, and states the file's local target. Its last step saves the after figures. No slice writes a
`@`-tag into code, and the moved-assertions list lives in the feature workspace (`measurement/slice-X-moved-assertions.md`).

### Slice A: ModifyTeamSettings x3 (`refinementNeed`, `readiness`, `test`)

Shares: `renderGravitysSettingsForm`, `replaceTheNumberIn`, the form, the fixtures. Directory: `src/components/Common/Team`.

| Step | What | Measures |
|---|---|---|
| A-1 | Declare and measure. Profile one band test and one readiness test; time `renderGravitysSettingsForm` alone and `render(<RefinementSettingsSection ...>)` alone (the spike); count the form tests per section; check that every mapping-warning sentence and each of the seven invalid scenarios is pinned by a function test | Local summed seconds of the three files, CI/local ratio, local target, go or no-go for A-2 |
| A-2 | The harness (own commit, test-only) and `readiness`: display and validation tests to the section render, wiring tests stay | Seconds and moved-assertions rows |
| A-3 | `refinementNeed` the same way for band, cadence and stage rules, one "saves nothing while invalid" wiring per section kind | Same |
| A-4 | `ModifyTeamSettings.test.tsx`: keep one of the four mapping-name wirings, shrink the invalid-scenario loop to what the function tests do not cover | Same |
| A-5 | Close: after seconds in 5 pairs, id diff, E-weaken grep, coverage of the directory (FE7 left it at 96.26 / 86.23), `pnpm build` and `pnpm test` | Per-file cut against the table |

Stop rule: at A-3 if the projected cut of `refinementNeed` is under 34 %, record it. If A-1's spike shows the section render
is not at least about 2x cheaper than the form, stop at A-1.

### Slice B: ReportsPage x2 (`removal`, `combinedPdf`)

Shares: `BulkReportsTestKit` (`renderTheReportsPage`, `rowOf`, `selectTheRowOf`), the page, the grid. Directory: `src/pages/Reports`.

| Step | What | Measures |
|---|---|---|
| B-1 | Declare and measure. Profile one `pickTheThree` test and one export test; spike a cheap `rowOf` against three tests; spike a render of `RemoveSelectedDialog` alone against its page test; split the export tests' cost into grid, `rowOf` and drawing the reports | Seconds of both files, ratio, local target, which of D-6561-7 and D-6561-4 are worth taking |
| B-2 | `rowOf` and `selectTheRowOf` with cheap queries in the kit (own commit); rerun the four importing files unchanged | Seconds of all four files |
| B-3 | `removal`: dialog and hook renders for the confirmation, warnings, results, Escape and cache tests; page wiring stays | Seconds, rows |
| B-4 | `combinedPdf`: `refactor(reports)` extracting `failedAmong` and `leaveOutPlanOf` (no test change, existing tests pass), then direct tests, the failure dialog alone, three page tests removed with rows | Seconds, rows, coverage |
| B-5 | Close as A-5; Stryker over the extracted file if B-4 ran | Per-file cut against the table |

Stop rule: if B-1 shows the export tests spend their time drawing reports, B-2 is skipped for `combinedPdf` and only B-4's
three tests count; if the cut is then under 17 %, record it.

### Slice C: OverviewDashboard and `BaseMetricsView.loading`

Shares: nothing in code; both are page-level mounts of a large view, both dropped by FE7 on an extraction-only ceiling, and both
pin loading or failure looks. Directories: `src/pages/Overview`, `src/pages/Common/MetricsView`. The lowest-confidence slice.

| Step | What | Measures |
|---|---|---|
| C-1 | Declare and measure. Count Overview's tests by what they read; spike: three header-gating tests with `DataOverviewTable` stubbed against the real one. Count `everyOwnerAndCategory` rows and the renders per family in `BaseMetricsView.loading`; spike: the query-options rule as a `renderHook` test | Seconds of both files, projected cut per file, go or no-go per file |
| C-2 | Overview: pure move of the non-row tests to `OverviewDashboard.gating.test.tsx` (own commit), then the stub | Seconds, rows (moves) |
| C-3 | `BaseMetricsView.loading`: the hook test for query options, merge the open and the settled families, one page row left | Seconds, rows |
| C-4 | Close as A-5. Every test listed in FE7's "pin the loading and failure looks" table still exists and asserts what it did | Per-file cut against the table |

Stop rule: each file is judged on its own; either may stop at C-1 with a recorded number. If Overview's spike saves under 20 %,
the fallback (D-6561-9c) is a question for the maintainer, not a default.

### After the slices (not a roadmap step in this run)

See "Budget step". Everything above is local and committed per step; nothing is pushed in AFK.

## Wave: DESIGN / [REF] Reuse Analysis

Contract shapes as `nw-solution-architect` asks for them: **pure function** (return-only), **bounded change** (declared
mutation set, aggregate-bounded universe, asserted by the mechanism named), **unbounded preservation** (none here: nothing in
this Story previews or validates by side effect).

| Need | Existing thing | Decision / contract shape / assertion mechanism |
|---|---|---|
| Rules as functions | `refinementBand`, `RefinementCadenceSettings` exports, `readinessErrors`, `discussionRules`, `refinementSettingsBlockers`, `stateMappingValidation`, `pickLimit`, `pickedReports`, `dateWindow`, `categoryMetadata`, `teamAutoSaveBlockers` | REUSE as destinations of moved assertions. **Pure function.** Assertion: the function's own tests |
| Render a section with state | `RefinementBandSettings`, `RefinementCadenceSettings`, `StageRulesSettings`, `ReadinessSettings` (props: value and `onChange`); the last blocks of `refinementNeed` already render two of them | EXTEND with one test helper, `renderRefinementSection` (CREATE NEW, no existing alternative: grep found no host that keeps the value in state). **Bounded change**: universe = the new helper file and the two test files that import it; assertion: the surviving form wiring tests still go through the real form |
| Query client and providers for tests | `renderWithQueryClient`, `createMockApiServiceContext` | REUSE unchanged. **Pure function** |
| Reports page test kit | `BulkReportsTestKit` (`renderTheReportsPage`, `rowOf`, `selectTheRowOf`), `ReportsTestKit`, `ReportPrintTestKit` | EXTEND `rowOf` only (D-6561-7). **Bounded change**: universe = one exported helper, four importing test files; assertion: those four files pass unmodified and the same sorted id list |
| Dialog and hook units | `RemoveSelectedDialog` (props: `reports`, `onCancel`, `onDone`), `NoPdfDialog`, `useSelectionRemoval`, `useBulkReportRemoval`, `useReportsPdfExport` | REUSE by rendering them alone (`renderHook` for the hooks). No change to them. **Bounded change** as hooks keep their effects |
| The failure rule of a combined PDF | private `failedAmong`, `isTheSameReport`, and the decision in `leaveOutTheFailedOf` inside `useReportsPdfExport.ts` | CREATE NEW `src/pages/Reports/printFailures.ts`, justified: no existing module owns it and the rule is only reachable today by drawing real reports. **Pure function** (return-only: reports and failed list in, plan out). Assertion: planted lists in a new `printFailures.test.ts`, then one Stryker run |
| Query options of metrics | `metricsQueryOptions` in `useMetricsData.ts`, `useMetricsData.test.ts` | REUSE: the hook test goes beside the existing one. **Pure function** (a constant) plus a hook that spreads it; assertion: the cache of an app-default client after `renderHook` |
| Overview with stubbed tables | `OverviewDashboard.test.tsx` fixtures (`renderWithProviders`) | EXTEND by a second file; the fixture moves with the tests or is exported once (decided at C-2, a pure move). **Bounded change** |
| Budget derivation | `Scripts/test-timings/check_budget.py`, `test-time-budget.json`, T4's recipe in `docs/ci-learnings.md` | REUSE unchanged; the budget step edits one number. **Bounded change**: universe = `budget_seconds` of `frontend`; assertion: the script on the saved CSVs |
| Time budget guard and allow-list | `src/tests/timeBudget/`, `slowTests.ts` (empty list, per-test limit 2 s) | REUSE. Per-test entries are not expected to appear; if a rewritten wiring test exceeds 2 s it is a defect of the rewrite, not an entry |

## Wave: DESIGN / [REF] Budget step (follow-up commit, after a push)

Not a step of this run, because it needs at least 5 `main` runs after a push and the maintainer pushes nothing in AFK.

1. The maintainer (or the next session) pushes the slices; the orchestrator waits for >= 5 complete `ci.yml` runs on `main`
   (the `test-timings-frontend` artifacts; a run that stopped at its first failure is partial and excluded).
2. For each, record the Vitest `Duration`, read as the runner speed: the memory note `reference_ci_vitest_duration_is_bimodal`
   puts the groups at about 610-830 s and 1,070-1,160 s. Never average across the groups; keep the maximum per file.
3. L is the largest per-file test time over those runs among files not on the allow-list. Rule as T4 applied it: B = ceil(2.1 x L).
   If none of the runs is in the slow group, scale L by 1.62 (T4's measured ratio) or wait for one. Provisional: L = 17.5
   (`ReportsTab.createWizard`) gives 37.
4. Edit `budget_seconds` of `frontend` in `Scripts/test-timings/test-time-budget.json`, replay `check_budget.py --stack
   frontend` on the saved CSVs, and write the derivation (runs, files, L, B) in this file and in the `docs/ci-learnings.md`
   test-time-budget bullet if its figures name the old value. Commit as `ci(test-infra): ...`; the budget only tightens.
5. If a file of the seven is still over the line, the derivation names it and B follows it (Q3); do not raise any ceiling.

## Wave: DESIGN / [REF] Risks and open questions

| # | Question or risk | Recommended answer taken in AFK | Needs the maintainer |
|---|---|---|---|
| Q1 | FE5 #6393 (the `BaseMetricsView` split) was open on 2026-11-04. If it landed, the metrics tests can mount one widget and C changes | Check the ADO state at pick-up; as written, `BaseMetricsView.tsx` stays untouched | Only if it landed during this work |
| Q2 | T12 #6539 touches some of these files for order and load; it has no workspace in the repository yet | Re-read each file at slice start; a conflict with trunk stops the slice (no autonomous rebase) | Which files T12 touches, and which Story goes first |
| Q3 | If `BaseMetricsView.loading` (or any file) cannot reach the line, the budget stops at 2.1 x that file. At 21.7 s that is 46 s, not 37 | The AC allows "or the reason is recorded"; record it | **Yes**: the maintainer should know the headline 37 s depends on all seven, and on the metrics file in particular |
| Q4 | Splitting a slow file in two would meet the per-file letter of the AC without running anything faster | Rejected as a default (D-6561-3d) | Only as a deliberate fallback, if a file misses and the maintainer wants the budget anyway |
| Q5 | The 5-run read may contain no slow-runner run, giving an optimistic L and a budget that fails on the first slow runner | Require a slow-runner run or scale by 1.62 (D-6561-13) | No |
| Q6 | Overview fallback extraction (D-6561-9c) is a production change in an RBAC-gated page | Not taken unless the stub spike saves under 20 % | **Yes, if it comes to that** |
| Q7 | The `useReportsPdfExport` extraction (D-6561-4) is a production change | Taken as recommended (two pure functions, own commit, identical behaviour) | **Veto point**: say no and slice B drops step B-4 |
| Q8 | The machine is shared by parallel sessions (FE7 saw load 10-14 on 4 cores); local alternating pairs can be noise | Record load and CPU seconds per pair; discard loaded pairs (D-6561-11) | No |
| Q9 | The three Refinement tests moved to a section render stop proving that the form wires the section's value; one wiring test per section stays | Kept explicitly in the per-file reading | No |
| Q10 | `docs/ci-learnings.md` "Where to test" says to put logic on a function; this Story adds a second rule, "render the unit that owns the behaviour, not the form or page" | Add one sentence to that bullet at the last slice's close, not now | No |

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**The feature's shape in hexagonal terms.** There is no product hexagon to change. The "ports" of this work are the seams
tests drive: a component's props and callbacks (driving), a hook's arguments and return, and the services behind
`ApiServiceContext` (driven, always faked).

```
 test (Vitest, RTL)
   |-- renders a page ----------- ReportsPage / OverviewDashboard / BaseMetricsView   (wiring, one per flow; unchanged)
   |-- renders the unit that owns the behaviour   (NEW for T13)
   |       RefinementBandSettings / Readiness / Cadence / StageRules   <-- renderRefinementSection (NEW test helper)
   |       RemoveSelectedDialog / NoPdfDialog / useSelectionRemoval    (unchanged, rendered alone)
   |       useMetricsData                                              (unchanged, renderHook)
   |-- calls a function directly (ms)
   |       bandErrors, readinessErrors, stateMappingValidation, ... (unchanged)
   |       failedAmong, leaveOutPlanOf  (NEW, moved out of useReportsPdfExport; production, optional)
   '-- drives ApiServiceContext fakes (driven side, unchanged)       BulkReportsTestKit.rowOf (MODIFIED, cheap query)
 Scripts/test-timings/test-time-budget.json   frontend budget_seconds 56 -> about 37  (follow-up commit, after 5 main runs)
```

New: one test helper, one optional production module (`printFailures.ts`), one test file for it, a handful of new unit-level
test files and one split test file (`OverviewDashboard.gating.test.tsx`). Modified: seven test files, `BulkReportsTestKit.rowOf`,
`useReportsPdfExport.ts` (imports the extracted functions), the budget number. Unchanged: all production behaviour, pixels,
E2E, `setupTests.ts`, the reporter and allow-list, every external call (this work adds none).

**Design patterns weighed.**

| Pattern or principle | Verdict | One line of why |
|---|---|---|
| Single responsibility, applied to a test: one test renders the smallest unit that owns the behaviour | Chosen | The form tests were paying for five sections to check one |
| Functional core / imperative shell (rules as pure functions, wiring in the shell) | Chosen, already mostly done by FE7 | Remaining private rule in `useReportsPdfExport` follows it |
| One wiring test per flow (the test pyramid at file scale) | Chosen | Providers and routing are only proven by a rendered page |
| Stateful host as a test double for a controlled component | Chosen | The sections are controlled; a host gives them back what they changed, as the form does |
| Stub a heavy child (`DataOverviewTable`) | Chosen for Overview's non-row tests, behind a spike | Test-only and the real table stays covered by the tests that read rows |
| Merge read-only checks into one render | Chosen for two metrics families | Same dashboard opened to look at two things |
| Hook test with `renderHook` for a rule in a hook | Chosen for the metrics query options | The rule is one exported constant |
| Fake timers instead of real waits | Already applied by FE7; not a lever here | No real waits are left in these files |
| Split a slow file in two | Rejected, kept as a fallback | Moves seconds between files, runs nothing faster |
| Memoise or lazy-load in `BaseMetricsView` | Rejected | Production change in a 2,074-line file that FE5 will split |
| Mount fewer widgets per loading test | Rejected | Changes what the 133 tests pin |
| Configuration (projects, pool, happy-dom) | Rejected | FE7 measured nine candidates; none passed |
| Extract a pure function from a hook (Strategy-free, Value-in/Value-out) | Chosen for the export failure rule; fallback for Overview's header actions | Moves the rule where it is cheap to test; behaviour identical |
| Domain events, outbox, unit of work, optimistic concurrency, JSON column versus child table | Not applicable | No product behaviour or storage changes |

Enforcement tooling for the styles above: the existing Biome and `tsc` gates, the time-budget reporter (per-test) and
`check_budget.py` (per-file, this Story's own target), and `reportsPage.enforcement.test.ts` and
`layerBoundaries.enforcement.test.ts` for module boundaries. No new tool.

## Wave: DEVOPS / [REF] Platform readiness

Existing platform; nothing new is deployed and no workflow changes in this run. Written 2026-11-06 under `/afk`.

| Topic | Answer |
|---|---|
| Environment | Local Vitest and the existing "Verify Frontend" job. |
| CI/CD | Unchanged. The per-test guard and the per-file budget from T4 judge the rewritten files on the first push. |
| Rollback | Revert the rewrite commit of a file; each file's rewrite is its own commit. |
| Monitoring | `test-timings-frontend` artifacts, read by hand for the budget step after at least 5 `main` runs. No dashboard, no alert. |
| Local measurement | Another session may run mutation testing at the same time. Load is recorded with each reading, before and after run in alternating pairs under similar load, and a reading under high load is retaken, not used. |
| Mutation | Project strategy stays per-feature. A run applies only to a production file an extraction touched; none if the maintainer declines the extraction. It runs last, and after the other session's run has finished. |
| Usage-data event | N/A, because no user of the product reaches test files, so no event could show use. |
| RBAC, Clients, Website, UI sketch | N/A, because no route, DTO, page or marketing claim changes. |

## Wave: DISTILL / [REF] Scope, reconciliation and baseline (2026-11-06, maintainer AFK)

- Shape: behaviour-free rewrites of seven test files under FE7's contract. No product behaviour is specified, so there is no
  new acceptance scenario to write: the acceptance criterion of every slice is that the existing behaviour stays pinned, and
  the checks below pin that.
- Reconciliation: DISCUSS (brief and Checklist), DESIGN and DEVOPS read; **0 hard contradictions**. Two soft points, both
  resolved in favour of DESIGN and recorded: (1) the DISCUSS checklist says "the budget falls in this Story" while D-6561-13
  makes it a follow-up commit after five `main` runs; read as "in this Story, as its last commit", so the checklist item stays
  open until that commit. (2) DESIGN's counts differ from a run (next table); they are corrections of figures, not of
  decisions, and the run's numbers win.
- UI sketches: N/A, because nothing a user sees changes. No nWave scenario tag in any test; an untagged test is correct.
- Mechanism: Vitest and React Testing Library, run per named file with `LIGHTHOUSE_TEST_WORKERS=2` from `Lighthouse.Frontend`.
  The full suite and Stryker are not run in this wave (another session is mutating on the machine).

### Baseline (run of 2026-11-06 on the pre-slice commit `d45a63265`, 329 of 329 passed, none skipped or todo)

Saved in `baseline/`: `<file>.ids.txt` (sorted `file :: fullName`, from a run, never from source), `<file>.expects.txt`
(count of `expect(` in the file's source, helpers inside the file included), `summary.md`.

| File | Tests | `expect(` | DESIGN said | Note |
|---|---|---|---|---|
| `ModifyTeamSettings.refinementNeed.test.tsx` | 47 | 77 | 47 | same |
| `ModifyTeamSettings.readiness.test.tsx` | 24 | 44 | 47 | DESIGN's figure was wrong (16 `it` and 8 `it.each` rows) |
| `ModifyTeamSettings.test.tsx` | 35 | 59 | 33 | includes the nested describes |
| `OverviewDashboard.test.tsx` | 51 | 113 | 51 | only 28 read no row, not "about 35" (counted below) |
| `BaseMetricsView.loading.test.tsx` | 133 | 150 | 133 | same |
| `ReportsPage.removal.test.tsx` | 21 | 37 | 21 | same |
| `ReportsPage.combinedPdf.test.tsx` | 18 | 44 | 19 | 18 |
| total | 329 | 524 | | |

Regenerate (or produce the "after" set) from the frontend directory, then diff:

```
cd Lighthouse.Frontend
LIGHTHOUSE_TEST_WORKERS=2 pnpm exec cross-env TZ=Europe/Zurich vitest run --reporter=json \
  src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx \
  src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx \
  src/components/Common/Team/ModifyTeamSettings.test.tsx \
  src/pages/Overview/OverviewDashboard.test.tsx \
  src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx \
  src/pages/Reports/ReportsPage.removal.test.tsx \
  src/pages/Reports/ReportsPage.combinedPdf.test.tsx
python3 -I ../docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/baseline6561.py \
  test-results.json "$PWD" <out-dir>
diff ../docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/<file>.ids.txt <out-dir>/<file>.ids.txt
```

The project's JSON reporter writes `Lighthouse.Frontend/test-results.json` whatever `--outputFile` says, so the script reads
that file. `baseline6561.py` takes `<report.json> <frontend-root> <out-dir>`. When a test moves to a new file, diff the union of
the touched files' ids: an id may change its file part, never silently disappear.

## Wave: DISTILL / [REF] The guarantee

**Stays green and unmodified (pinned by the baseline id lists):** every id not named in "changes by design" below, in all
seven files; and, outside the seven, the files that share a helper this Story edits: `ReportsPage.reading.test.tsx`, `ReportsPage.test.tsx`, `ReportsPage.usageData.test.tsx` and
`BulkReportWizard.test.tsx` (importers of `rowOf`, six in all), `ModifyTeamSettings.refinement.test.tsx`, `ModifyTeamSettings.queryCommit.test.tsx`,
`OverviewDashboard.licenseCheck.test.tsx`, `useMetricsData.test.ts`. After each commit that is not a rewrite step (a pure move, the
`rowOf` commit, the `printFailures` extraction) the unmodified tests pass with no test edit in that commit.

**Changes by design (the only ids that may leave their file or shrink; each needs a row):**

| File | Ids that may change | By how many (hypothesis, confirmed at the slice's step 1) |
|---|---|---|
| `refinementNeed` | stage rule display (4), cadence display and refusal (4), band display, marks, explanations and the two `it.each` families (15) move to a section render | 23 of 47 |
| `readiness` | defaults, refusals, `aria-invalid`, `min`/`step`, remembered-threshold toggles (all but 6) move to a section render | 18 of 24 |
| `ModifyTeamSettings.test` | three of the four "holds back autosave while a mapping ..." tests removed; the seven-row invalid-field loop shrinks to the rows the function tests do not cover | 3 + up to 5 of 35 |
| `OverviewDashboard` | the 28 tests that read no row move, titles unchanged, to `OverviewDashboard.gating.test.tsx` and render the page with `DataOverviewTable` stubbed | 28 of 51 move; 0 removed |
| `BaseMetricsView.loading` | the 8 "adds and removes no chart once its data has arrived" rows merge into the matching "opens with every chart's frame" rows; 7 of the 8 "is asked for without retrying" rows become two hook tests | 15 removed, 8 gain assertions, 2 added |
| `ReportsPage.removal` | the confirmation, warning, reason, result, Escape, in-flight and cache tests move to dialog and hook renders | 15 of 21 |
| `ReportsPage.combinedPdf` | three "Leave it out ..." rule tests move to function tests; "Cancel closes the word that no PDF was made" to the dialog alone | 4 of 18 |

A change beyond these rows is allowed only when a slice's step 1 measures something different, and then the table above is
edited in the same commit with the reason.

**Loading and failure looks stay, every one, asserting what they did.** In `BaseMetricsView.loading` these are the 109
untouched ids plus the merged opening tests: a dashboard as it opens, the dim under a spinner while the window changes, answers
out of order, one failed request marks only its chart and comes back on retry, the aging chart's missing Doing columns. In
`OverviewDashboard` they are "renders loading state initially" and the five "when the connection list is refused" tests.

**No-new-behaviour checks:** no production file changes except the two optional ones (`printFailures.ts` new,
`useReportsPdfExport.ts` importing it); no Playwright spec or `@screenshot` file in the diff; production behaviour and pixels
are unchanged.

## Wave: DISTILL / [REF] Per-file: wiring tests that stay on the whole form or page

Named from DESIGN, titles as they are today. Each is the one proof of a seam that a unit render cannot give. **Total 193 ids
stay on the whole form or page, unmodified**: 7 + 6 + 27 + 23 + 110 + 6 + 14. Counts for `ModifyTeamSettings.test` and
`BaseMetricsView.loading` are the unmodified remainder, because those files change by removal or merge, not by moving the rest.

**`refinementNeed` (7):** saves a changed Ready rule and leaves the Being refined rule as it was; saves nothing while a new
rule is still incomplete; saves Thursdays every week; asks for the starting week once Refinements are more than a week apart,
and saves it; saves nothing while every second week names no starting week, and marks the field; saves a changed high end with
the low end as it was; refuses a decimal likelihood instead of saving it rounded, and saves nothing. (One "saves nothing while
invalid" per section kind, because `teamAutoSaveBlockers` through `refinementSettingsBlockers` is only proven by a rendered
form.) The blocks after line 693 (the blockers function tests, stage fields of a single Team, the states picker, the To Do and
Doing changes, the cadence fields on their own) are not in the move list and stay as they are.

**`readiness` (6):** saves a changed number of Yes votes with the rest of readiness as it was; starts a Team that has never
chosen refinement at the default readiness, and saves it with no states; keeps the chosen refinement states when readiness
changes; names readiness as what keeps the settings from being saved; saves nothing just because the form was opened; and one
of the "switches a rule back on ..." tests, `switches a rule back on at the threshold it had before`, as the single proof that
the toggle memory reaches the saved value.

**`ModifyTeamSettings.test` (27, the form stays whole):** every test except three of the four mapping-name wirings and up to
five rows of the invalid-field loop; one mapping-name wiring stays (`holds back autosave while a mapping is named after a
Doing state it does not group, and saves once it is renamed`), and the two loop rows the function tests do not cover.

**`OverviewDashboard` (23, real tables):** the 4 "Row action gating via DataOverviewTable predicates" tests; the 12 deletion
tests ("what a deletion is reported as" 2 and "deleting a Team or Portfolio that has reports" 10, which open a real row's
Delete); `renders loading state initially`; `renders portfolios section after loading`; the 5 "when the connection list is
refused" tests. `OverviewDashboard.licenseCheck.test.tsx` is another file and is not touched.

**`BaseMetricsView.loading` (110):** everything except the 24 rows of the three `everyOwnerAndCategory` families, plus **one
page-level "asked for without retrying, and kept for no other window" row** kept as the seam proof (a correct hook with a
widget that bypasses it would pass the hook test).

**`ReportsPage.removal` (6):** a coach removes three trial reports on two Teams and a Portfolio, and they leave the list; Cancel
removes nothing; when her permissions could not be loaded, pressing Remove asks nothing; a report the server did not remove is
named with its reason and stays listed; changing the filter clears what was picked; sorting keeps what was picked.

**`ReportsPage.combinedPdf` (14):** a coach exports the reports of two Teams as one PDF, each on a page of its own; once the
print dialog closes the frame goes ...; the reports print in the table's order, as the reader sorted it; nothing prints while
one of the reports is still on its way ...; picks and sorting changed while the PDF is prepared leave it as it was ...; while a PDF
of several is prepared, a row's PDF icon is held back ...; while a row's PDF is prepared, Export PDF is held back ...; while the
reports load, Export PDF shows it is preparing ...; a reader who is Admin of none of them may still export them; a report that
cannot be loaded stops the PDF and is named; Leave it out unpicks the report that failed and prints the rest; Retry asks for the
report again and prints once it arrives; when part of the page cannot be loaded no PDF is made and it says so; with nothing
picked Export PDF is disabled and says why.

## Wave: DISTILL / [REF] New direct tests, per slice, named as behaviours

Moved tests keep their titles; the rows of the moved-assertions file name them. The tests below are the ones that do not exist
today. All render the smallest unit that owns the behaviour, or call a function; none mounts the whole form or page.

**Slice A**
- `renderRefinementSection.test.tsx` (the helper's own tests, so a wrong host cannot hide a wrong section):
  "hands the section the value it was given and no more", "a change the admin makes reaches the section as its new value",
  "a second edit starts from the first edit's result, as in the form", "lists every value the section handed back, in order".
- The moved Refinement tests land in `RefinementBandSettings.test.tsx`, `RefinementCadenceSettings.test.tsx` (existing last
  block of `refinementNeed` may stay or move), `StageRulesSettings.test.tsx`, `ReadinessSettings.test.tsx` (names chosen at A-2
  after the existing files are checked, so no duplicate file is made).
- If A-1 shows a mapping-warning sentence or invalid-field scenario that no function test pins, the function test is added
  before the wiring test is removed: "names the mapping that hides a Doing state" style, one per unpinned sentence.

**Slice B**
- `BulkReportsTestKit` self-test, only if the kit has none: "finds the row of a report on its owner", "tells two reports of the
  same name on different owners apart", "fails naming the report and the owner when there is no such row".
- `printFailures.test.ts` (only if the extraction is taken): "names the listed reports a failure belongs to, by id and owner",
  "does not name another report of the same Team", "names nothing for an empty failure list", "offers leaving out when something
  failed and something would be left", "does not offer it when every report failed", "leaves out exactly the failed ones and no
  other".
- `NoPdfDialog.test.tsx`: "offers Cancel, Leave it out and Retry when leaving out is possible", "offers Cancel and Retry when
  it would leave nothing", "each button reports its own choice once". Moved: "Cancel closes the word that no PDF was made".
- `RemoveSelectedDialog.test.tsx` and `useSelectionRemoval.test.ts`: the moved removal tests; new only where the page test
  proved a reason no unit test can reach (decided at B-3, stated in the rows).

**Slice C**
- `OverviewDashboard.gating.test.tsx`: the 28 moved tests, titles unchanged; no new test. A test that passes against the stub
  and fails against the real table (or the reverse) is a finding, written in the slice file.
- `useMetricsData.test.ts`: "a team's dashboard asks for every chart's data without retrying and keeps none of it for another
  window", "a portfolio's dashboard asks ... the same" (app default query client, every key of `allMetricsFetchKeys`).
- `BaseMetricsView.loading`: the 8 merged rows keep the title of the "opens with every chart's frame in its place, each with a
  spinner, before any data arrives" rows and also assert that no chart is added or removed once the data has arrived.

## Wave: DISTILL / [REF] Shared helper contracts (written by DELIVER in the step that first needs them)

**`renderRefinementSection` (slice A, step A-2, in `src/tests/`, test-only).** Renders one controlled Refinement section
(`ReadinessSettings`, `RefinementBandSettings`, `RefinementCadenceSettings`, `StageRulesSettings`, `RefinementSettingsSection`)
inside the providers the form's tests give it (the Team's terminology, the seeded default words unless the test passes its own)
and a host that keeps the value in `useState`.
- Input: the initial value and a function from `(value, onChange)` to the section element.
- Returns the `user` from `userEvent.setup()`, `latest()` (the value the host holds), `changes()` (every value the section
  handed to `onChange`, in order, as copies), and the usual RTL queries are used from `screen`.
- `onChange` takes the complete next value, as `updateSettings` does in the form, and updates the state synchronously; no
  query client, no fake clock, no debounce, no service. It never calls anything on its own, so a section that needs a service
  is a wiring test and stays on the form.
- It does not hide an error: a throwing section fails the test; no `try`, no `act` advance of timers.
- One plain comment on why the value lives in state (a controlled section shows nothing new unless handed its change back).
- Proved by its own four tests (above) before any Refinement test moves; the surviving wiring tests prove the form hands the
  same value to the section.

**Cheaper `rowOf` (slice B, step B-2, in `BulkReportsTestKit.tsx`, only if B-1 shows a saving).**
- Same name, same arguments `(reportName, owner)`, same return (the one `row` element) and the same thrown message
  `no row for "<name>" on <owner link name>`, so no importing test changes.
- Finds the row by its text (the report name link) and confirms the owner with a cheap query; **no `*ByRole` call inside a
  per-row loop** (`docs/ci-learnings.md`, "Use the cheap query unless the role is what you are testing"). The checkbox read by
  `selectTheRowOf` may keep `getByRole("checkbox")` on the single row it returns.
- Picks the correct row when two reports share a name on different owners, and when the owner's name is also a report's name.
- Proved by: the six importing files (`removal`, `combinedPdf`, `reading`, `ReportsPage.test`, `ReportsPage.usageData.test`, `BulkReportWizard.test`; DESIGN named four) pass unmodified with the same sorted ids, and by
  the kit's self-tests above.

**`printFailures.ts` (slice B, step B-4, optional, veto point Q7).** Pure, return-only. `failedAmong(listed, failed)`
returns the listed reports a failure names, matched by id **and** owner; `leaveOutPlanOf(listed, failed)` returns either "not
offered" (nothing failed, or leaving the failed out would leave nothing) or the reports that remain. No hook, no React import.

## Wave: DISTILL / [REF] How "no `.skip`, `.only`, `.todo`, looser matcher or longer timeout" is checked

1. **Biome** already fails `pnpm build` on `noSkippedTests` and `noFocusedTests` (FE7 slice 1a), so `.skip` and `.only` cannot
   reach review.
2. **`baseline/check-behaviour-free.sh <before-commit>`**, run from the repository root at every slice close and before review.
   It greps the diff of test files and `src/tests` since `<before-commit>` for added `.skip`, `.only`, `.todo`, `it.fails`,
   `xit`, `xdescribe`, `toMatchObject`, `objectContaining`, `toBeGreaterThan`, `toBeLessThan`, `toContain(`, `toMatch(`, a
   `{ timeout: ... }` option, `testTimeout`, `setConfig` and any 4-digit number as a last argument; counts removed against added
   `not.`; prints the number of removed `expect(` lines per file; fails when a production file other than `printFailures.ts` and
   `useReportsPdfExport.ts` changed, or when an E2E or screenshot file changed. It was tried against a planted `it.skip` and a
   deleted `expect(` and reported both. Hits it cannot judge (an added `toContain(` on a string that was already one) are
   answered in the slice file, a row each, not ignored.
3. **Rule 1 counting** (FE7): per touched file, `git diff -U0 <before> -- <file> | grep -c '^-.*expect('` equals the rows for
   that file in `measurement/slice-X-moved-assertions.md`, built from `measurement/moved-assertions-template.md`. The
   "Assertion text" column holds the removed `expect(` line verbatim. The `expect(` totals in `baseline/` are the sum to explain.
4. **Id diff** against `baseline/*.ids.txt` (command above): every missing id is a row, every added id is listed.
5. **Deliberate break** for each new call site and each surviving wiring test: break the production seam it claims to prove
   (drop the value handed from the form to the section, pass the stub the wrong prop, bypass `metricsQueryOptions` in one
   widget), see the named test fail, restore, record both in the slice file.
6. **Coverage** (opt-in for exactly this reason): per slice, `--coverage` over the named files of the touched directory only,
   line and branch not lower than the slice's first-step reading (FE7 left `src/components/Common/Team` at 96.26 / 86.23).

## Wave: DISTILL / [REF] Stop rule

Inherited from D-6561-10 and made checkable: at the slice's step 2, the slice file states the projected cut per file (seconds
after the moves as measured on the touched files, over the file's summed seconds before) next to the required cut of DESIGN's
table (34 %, 24 %, 20 %, 19 %, 17 %, 13 %, 1 % for the seven; 38 to 6 % with margin). Below the required cut: stop that file,
record the number and the reason ("target not met, because ...", which the AC allows), flag that the budget floor follows that
file (Q3), and do not split the file (Q4 stays a maintainer decision). Other stops: A-1's spike shows the section render under
about 2x cheaper than the form (stop slice A at A-1); B-1 shows the export tests spend their time drawing reports (skip B-2 for
`combinedPdf`; if the cut is then under 17 %, record it); C-1's Overview spike saves under 20 % (ask the maintainer about the
extraction, Q6). A conflict with trunk (T12 #6539) stops the slice and reports; nothing is rebased.

## Wave: DISTILL / [REF] No scaffolds, no skipped tests, and why

There is no new production behaviour, so no module is imported that does not exist, and a scaffold would only duplicate a test
that already passes. Skipped tests are not written either: the project forbids `.skip` in committed tests (Biome), and the 329
existing tests are the executable specification; they are green now (329 of 329) and that is the correct starting state.
The "red" of this Story is the deliberate break of check 5 above, made per call site by DELIVER. The one optional production
file (`printFailures.ts`) is created in its own `refactor(reports)` commit with no test change, not as a scaffold. Tier B,
the domain-types module and the state-delta port: N/A, because the project has no Gherkin, state machine or generator at this
layer. Self-completeness audit: C2 and C5 to C7 N/A (no state machine, mode flag, error contract or concurrency matrix is added).

## Wave: DISTILL / [REF] Pre-requisites

- **Done in this wave:** the worktree's `node_modules` (`pnpm install --frozen-lockfile`), the baseline run and files, the
  check script, the moved-assertions template.
- **Before slice A:** nothing external. **Before slice B:** the maintainer's yes or veto on the `useReportsPdfExport`
  extraction (Q7); the default taken is yes. **Before slice C:** the ADO state of FE5 #6393 (Q1); if it landed, C changes and the
  slice is redesigned with the maintainer.
- **At every slice start:** re-read the slice's files against trunk for T12 #6539 (Q2); run `/continuous-improvement before 6561`
  once at pick-up; agree the rigor profile (proposed `standard`, D-6561-16) and pass `model` on every dispatch; record the load
  average, run the alternating pairs with `LIGHTHOUSE_TEST_WORKERS=2` on named files, discard a pair whose load exceeds half the
  cores, and never run the full suite or Stryker while another session is mutating.
- **After the slices:** the budget step (DESIGN, "Budget step"), once at least 5 complete `main` runs exist.
