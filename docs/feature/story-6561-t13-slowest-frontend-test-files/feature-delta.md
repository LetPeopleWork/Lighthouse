# Feature: story-6561-t13-slowest-frontend-test-files

Story #6561 · T13 The slowest frontend test files run faster, and the frontend budget comes down with them · part of
Epic #6506 "Fast, reliable test suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-46](../epic-6264-modular-monolith/slices/slice-46-t13-slowest-frontend-test-files.md).

Created 2026-11-06 on the maintainer's word, as a separate Story after T4 #6462 pushed. DISCOVER and DIVERGE have no
artifacts and were not run: T4 measured the problem and named the files, and the lever is FE7's, so no solution direction
is open. DISCUSS is the brief plus the DISCUSS sections of this file, because the Story applies an existing contract to
more files. That is a shortcut taken in AFK and recorded here, not a skip the maintainer asked for in words; a fuller
DISCUSS pass can still be asked for. Size M.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md` and
`docs/feature/story-6457-fe7-fast-frontend-tests/feature-delta.md` (the behaviour-free contract for test rewrites);
T4's derivation in `docs/feature/story-6462-t4-test-budgets-fail-the-build/feature-delta.md`.

- job_id: infrastructure-only. infrastructure_rationale: the budget cannot fall while seven files take 17 to 27 s.
- Size M. Contexts touched: none (test files and one number in the budget file; ADR-270 and `ARCHITECTURE.md` section 4
  need no update).
- Slice-composition gate: knowingly waived for the whole Epic (D-6264-5 in the Epic workspace,
  `docs/feature/epic-6264-modular-monolith/feature-delta.md`): a behaviour-free Story has no user-visible slice by
  definition. This Story is `@infrastructure` and stands under that waiver; it is not a gap found here.
- Tests change by design; production behaviour and pixels do not. Production code changes only if the maintainer says yes
  to one named `refactor(reports)` commit (Q7, below); without the yes, no production file changes.
- Cross-cutting: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, Terminology: N/A, because only test
  files and the budget file change (rewritten tests keep asserting the seeded default terms). Context map: no change.
  Release notes: none, because nothing a user sees changes.

## Wave: DISCUSS / [REF] Acceptance criteria and dependencies

Restated from the brief, each observed through the CI step "Check test-time budget" (`check_budget.py --stack frontend`,
reading `Scripts/test-timings/test-time-budget.json`) and the Vitest run:

1. Each of the seven files' largest time on plain CI runs is under 17.5 s, or the reason it is not is recorded.
2. Coverage of the touched directories does not drop, and production behaviour and pixels are unchanged. Stryker kill
   rate, floor 80 %, on any production file the one allowed refactor commit touches; with no production file changed,
   mutation testing is N/A and recorded as a maintainer-visible exception (DEVOPS table).
3. The frontend **per-file budget** (`budget_seconds` of `frontend`, 56 s today) is lowered in the same Story, as its last
   commit, derived on at least 5 complete plain `main` runs after the rewrites. A slow-runner run among them is optional:
   when none is slow, L is multiplied by 1.62. If the derived B is not below the 56 s in force, no commit is made.

**Plain run:** a complete `ci.yml` run on `main` whose frontend test step ran without coverage, which is every run since
T7's commit `638a9bae9` (2026-11-04); see "The earliest runs are plain" in
[T4's workspace](../story-6462-t4-test-budgets-fail-the-build/feature-delta.md).

**Slow and fast runs:** the same tests take about 610-830 s of Vitest `Duration` on a fast runner and about 1,070-1,160 s on
a slow one. One boundary: a run of **950 s or more is slow, anything under 950 s is fast**. A slow-runner run among the five
is optional, because the 1.62 scaling covers its absence (see "Budget step").

**Dependencies (as the brief):** hard, T4 #6462, pushed 2026-11-06, which made the per-file budget fail the build and set
it at 56 s; soft, T12 #6539, which touches some of the same files for order and load.

## Baseline re-count

Measured 2026-11-06 on the 7 plain runs of T4 (see the brief's table). Frontend budget 56 s, empty allow-list. The brief's
figures, copied here so the target arithmetic below stands on this page:

| File | Max over 7 plain runs (s) | Median (s) |
|---|---|---|
| `ModifyTeamSettings.refinementNeed.test.tsx` | 26.5 | 20.5 |
| `ModifyTeamSettings.readiness.test.tsx` | 22.9 | 17.7 |
| `OverviewDashboard.test.tsx` | 22.0 | 14.3 |
| `BaseMetricsView.loading.test.tsx` | 21.7 | 16.3 |
| `ReportsPage.combinedPdf.test.tsx` | 21.0 | 14.5 |
| `ReportsPage.removal.test.tsx` | 20.2 | 14.4 |
| `ModifyTeamSettings.test.tsx` | 17.6 | 12.8 |

The next file is `ReportsTab.createWizard.test.tsx` at 17.5 s.

## Checklist

- [ ] Behaviour-free: production behaviour and pixels unchanged; no production file changes unless the one Q7 refactor
      commit is taken
- [ ] Each file's time profile recorded before its rewrite (step 1 of its slice)
- [ ] Every removed `expect(` has a row saying where it lives on (`check-behaviour-free.sh` exits 0) and the id union diff
      is clean (`run-touched-files.sh` ends with OK)
- [ ] Coverage not lower (`pnpm test:coverage`, opt-in for exactly this reason)
- [ ] Each call site proven by a deliberate break
- [ ] The FE7 guards hold: the per-test time guard (2 s, empty `slowTests.ts`) names no test of the seven files, and Biome
      `noSkippedTests` and `noFocusedTests` are clean
- [ ] Ratchet: the budget falls in this Story, as its last commit, derived on at least 5 complete plain `main` runs after
      the push (a slow-runner run among them is optional; else L x 1.62); the Story stays open until that commit lands.
      Status: open. Slice A alone is this run and nothing is pushed, so the budget stays 56 s.
- [ ] Mutation last, on frozen code: Stryker kill rate, floor 80 %, on a production file the refactor commit touched; N/A
      with the reason when none. Status: N/A for slice A, because no production file changes; an exception to the
      per-feature strategy that the maintainer sees at the hold.
- [ ] `pnpm build` and `pnpm test` clean before review

## Wave: DESIGN / [REF] Context-map placement and cross-cutting

Propose mode, maintainer AFK: the recommended option was taken for every engineering choice (except Q7, whose default was flipped to skip after the review gate, D-6561-17) and is recorded in
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
- Improvement backlog (`/continuous-improvement before`): does not apply. The command is for a new feature Epic, and #6506
  is itself an improvement Epic. FE5 #6393 is the one open item known to matter (Q1), checked at pick-up.

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
   file** from the budget arithmetic (below) and stops a file early when the step that puts its moves in measures a cut below it (D-6561-10, D-6561-20).
2. Per-slice figures were taken on a shared machine (load 10-14 on 4 cores) and called indicative; the final wall median
   and the five-run CI read were dropped, so FE7 proved no payoff on CI. T13's proof is the budget step itself (the AC is
   a CI reading), plus local alternating pairs with load recorded (D-6561-11).
3. CI-second ceilings were first read as measurements. Here, the brief's table mixes runner speeds (next section).
4. Slice 11 was judged on extraction alone. The profile (render 27 %, MUI 13 %) said the cost is mounting the dashboard,
   which a smaller mounted unit answers; T13 tests that as a hypothesis with a spike before any rewrite.

## Wave: DESIGN / [REF] The target, as arithmetic

The budget rule (ADR-237 as T4 applied it): B is at least 2.1 x L, L the largest file time over the runs among files not on
the allow-list. 2.1 x 17.5 (`ReportsTab.createWizard.test.tsx`) gives 36.75 and 2.1 x 17.6 (`ModifyTeamSettings.test.tsx`,
one of the seven, which needs only a trim) gives 36.96. Both round up to B = 37, so 37 is the floor whichever of the two
names L. **Making the seven files faster than that buys nothing**: the budget stops at that file. The aim is therefore
"each of the seven under the line, with margin", not "as fast as possible".

The brief's table gives each file's max and median over 7 runs. T4 recorded that the frontend runner is bimodal
(whole-run totals 649-793 s and 995-1054 s, a 1.62 ratio) and that the 26.5 s maximum is a **slow-runner reading**; the
largest file in fast-runner runs was 16.7 s. So the maxima below are slow-runner readings, and the medians mix both modes.
The required cut is a ratio, the same in both modes. **Cut = 1 - after/before**, on a file's summed per-test seconds (20 s
down to 13 s is a cut of 35 %); every later use of "cut" means this. Each file has two figures. The **required cut** (to
17.5 s, no margin) stops the file when the projected cut is less than it. The **design target** (to 16.5 s, with 1 s margin)
is what the rewrite aims at and is reported beside it. A file between the two passes and is recorded as passing with no
margin:

| File | Max (s) | Required cut, to 17.5 s (stops the file) | Design target, to 16.5 s (reported) | Local summed s (FE7, quiet-ish) |
|---|---|---|---|---|
| `ModifyTeamSettings.refinementNeed` | 26.5 | 34 % | 38 % | 14.8 (47 tests) |
| `ModifyTeamSettings.readiness` | 22.9 | 24 % | 28 % | about 14 (24 tests) |
| `OverviewDashboard` | 22.0 | 20 % | 25 % | 9.2 (51 tests) |
| `BaseMetricsView.loading` | 21.7 | 19 % | 24 % | 9.3 (133 tests) |
| `ReportsPage.combinedPdf` | 21.0 | 17 % | 21 % | not measured by FE7 (18 tests) |
| `ReportsPage.removal` | 20.2 | 13 % | 18 % | 9.0 (21 tests) |
| `ModifyTeamSettings.test` | 17.6 | 1 % | 6 % | not measured (35 tests) |

Local seconds are FE7's, from before its slices where they were touched, so they are only a scale: CI/local is about 1.6
to 1.75 for the two files with both numbers (Overview 14.3 / 9.2, `BaseMetricsView.loading` 16.3 / 9.3, both CI medians).
Step 1 of each slice recomputes the ratio per file from the saved CSVs of T4's runs and states the local target in
seconds. **The budget can only fall as far as the worst file left**: if `BaseMetricsView.loading` stayed at 21.7 s the
budget would be 46 s, not 37 (Q3).

**Expected budget outcome.** B = ceil(2.1 x L), L the largest file time left. Per worst file left:

| Worst file left | L (s) | B (s) |
|---|---|---|
| none: all seven reach their cut | 17.6 (`ModifyTeamSettings.test`, untrimmed) or 17.5 (`ReportsTab.createWizard`) | 37 |
| `ReportsPage.removal` | 20.2 | 43 |
| `ReportsPage.combinedPdf` | 21.0 | 45 |
| `BaseMetricsView.loading` | 21.7 | 46 |
| `OverviewDashboard` | 22.0 | 47 |
| `ModifyTeamSettings.readiness` | 22.9 | 49 |
| `ModifyTeamSettings.refinementNeed` | 26.5 | 56 (no change) |

37 s is the outcome **only if all seven reach their cut**. Without the `useReportsPdfExport` extraction (the default,
Q7), `ReportsPage.combinedPdf`'s cut rests on the cheaper `rowOf`, one dialog test and one merge, and probably misses
17 %: **45 s** is the likely outcome. `BaseMetricsView.loading` needs 19 % and the design's own estimate there is about
15 % (11 % by test count: 15 of 133), so it probably misses too, which gives **46 s**. `OverviewDashboard` may miss as well (47 s, D-6561-18). Plan on 45 to 47 s and treat 37 s
as the prize. A production fallback (the Overview extraction, Q6, or any other) stays the maintainer's call and is not
taken by default.

## Wave: DESIGN / [REF] Per-file reading (hypotheses, each measured at its slice's first step)

Nothing here is measured. "Likely cost" comes from reading the test and the component; the profile method of FE7 slice 11
(`--pool=forks --execArgv=--cpu-prof`, summarised by `measurement/profile-2026-11-04/summarize.py`) or one single-test
timing decides each before any edit.

### ModifyTeamSettings.refinementNeed.test.tsx (1,117 lines, 47 tests)

- **Likely cost.** 31 tests render the whole `ModifyTeamSettings` form to check one Refinement section: 6 stage rule
  tests, 7 cadence, 17 band (aria-invalid, descriptions, tooltips) and 1 refinement picker (the other 16 ids already render one unit or call
  a function). The form mounts `ForecastSettingsComponent`,
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
- **Risk to the target.** The cut is 34-38 %, the largest of the seven. Moving 23 of the 31 form tests to a unit render
  reaches 34 % only if one test costs at least 1.85 times as much in the form as in the section (2.05 times for 38 %; the
  formula is in "Slices"), and that is a hypothesis: the A-1 gate times one band test both ways before anything moves. If it
  does not reach, flag it (D-6561-10); do not split the file (D-6561-3).

### ModifyTeamSettings.readiness.test.tsx (561 lines, 24 tests counting `it.each` rows: 16 `it` and 8 rows)

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

### ModifyTeamSettings.test.tsx (971 lines, 35 tests)

- **Likely cost.** The whole form per test with the cheap mocks of General, States, Tags and WorkItemTypes (buttons), but
  `StateMappingsEditor` real. The least slow of the seven: 17.6 s max against the 17.5 line, so it needs a trim, not a rewrite.
- **FE7 pattern.** Remove duplicate wirings. Four "holds back autosave while a mapping is named after ..." tests (Doing, To
  Do, Done, another mapping) each assert a warning sentence that `stateMappingValidation.rawLists.test.ts` already pins at
  function level with the same text, then the same wiring "held back, saved once renamed". One of the four stays as the
  wiring; the other three go with rows naming that test and the function test (verify at A-1 that every sentence is
  asserted there), and one new form test names every warning kind at once so the form's feeding of each kind stays proved
  ("The mapping-warning seam" in "Slices"). The 7-scenario loop "does not auto-save when X is invalid" proves which field blocks; if the 18 tests of
  `teamAutoSaveBlockers.test.ts` already cover each of the seven, the loop shrinks to two rows.
- **Pins a rule:** the four warning sentences (function tests exist); the invalid-field loop (function tests exist, check).
  **Wiring:** held back / released on rename, auto-save after the debounce, no Save button, schema-driven work item types,
  staleness opt-in seed of 5, the refresh-failure "Reload now".
- **Production change needed:** none.

### OverviewDashboard.test.tsx (1,454 lines, 51 tests)

- **Likely cost.** FE7's profile (render 27 %, jsdom 11 %, MUI 15 %, no real wait, median 158 ms, 34 tests over 100 ms)
  says the cost is mounting the page, which holds two `DataOverviewTable`s (each a DataGrid with toolbar and chips), an
  `OnboardingStepper`, `FilterBar` and the providers, under `TerminologyProvider` and a query client, with six mock services
  built per render. Most tests (28 of 51, counted in DISTILL) read header buttons, alerts, the stepper
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
  needed is 20-25 %. The stub move is 28 of 51 tests (55 %), not about 35 (69 %), so its saving is 0.55 x the share of a
  mount that the two tables account for (equal weight per test assumed; the 23 tests that stay open real rows and may be
  heavier). It reaches 20 % only if the tables are at least 36 % of a mount, and 25 % only at 45 %. FE7's profile gives
  render 27 %, MUI 15 % and jsdom 11 % for the whole file (53 %), so the tables would have to be about two thirds of
  everything React, MUI and jsdom cost, which is doubtful. The stub spike decides (C-1); if it saves less than 20 % of the
  file, say so and stop. The extraction fallback (D-6561-9c, Q6) is a production change and stays the maintainer's call;
  it is not taken by default.

### BaseMetricsView.loading.test.tsx (1,967 lines, 133 tests)

- **Likely cost.** Already cheap per test: charts, `Dashboard`, `DashboardHeader` and `WorkItemsDialog` are stubbed, buttons
  are found by text, a held metrics service answers on command. What remains is mounting `BaseMetricsView` (2,074 lines,
  untouched, FE5 #6393 would split it) and its widget shells, **133 times at a median of 59 ms** (FE7 profile: 27 % React
  and React DOM, 13 % MUI, 34 % import and setup). The count is the cost.
- **Where the count comes from (counted from the baseline ids).** Three `it.each` families run one render for every owner x
  category row, 2 owners x 4 categories = 8 rows each, 24 of the 133 tests: "opens with every chart's frame in its place,
  each with a spinner, before any data arrives" (8), "adds and removes no chart once its data has arrived" (8; two
  `it.each` calls in the source, one for the three categories that are not Portfolio & Features and one for it, same
  title) and "is asked for without retrying, and kept for no other window" (8). The source lines 1313-1323 that DESIGN
  first read as a fourth family are the two calls of the "adds and removes" family. The other 109 tests are single tests
  or smaller `it.each` over other things (arrival orders, owners, widgets). The rows differ only in owner and category,
  and the three families render and load the same dashboard to look at different things.
- **FE7 pattern (new combination).** (a) **One render per owner x category for read-only checks**: the 8 "adds and removes"
  rows merge into the 8 "opens with every chart's frame" rows. One test opens the dashboard, asserts frames and spinners at
  first sight, loads everything, then asserts no chart was added or removed: 8 renders go, those 8 tests gain an
  assertion, and every old test name gets a row. (b) **The query options rule belongs on a hook**: `metricsQueryOptions`
  (staleTime 0, gcTime 0, retry false, no refetch) is one exported constant in `useMetricsData.ts`; "asked for without
  retrying, and kept for no other window" is therefore a hook-level `renderHook(useMetricsData)` test with the app's
  default query client over every fetch key (the hook's own fallback `allMetricsFetchKeys` covers every widget), one per
  owner. Seven of the 8 page rows are replaced by those two hook tests (7 renders go); one stays as the seam row below.
- **The kept seam row:** `every chart request on the dashboard on a team's Flow Metrics is asked for without retrying, and
  kept for no other window`. A correct hook with a widget that fetches without it would pass the hook test, and four places
  pass `metricsQueryOptions`: the hook (`useMetricsData.ts`), the drill-down query in `BaseMetricsView.tsx`,
  `useCumulativeStateTimeChoices.ts` and `useFilteredView.ts` (used by `ThroughputRunChartCard` and
  `PredictabilityScoreDetailsWidget`). C-1 lists, from `categoryMetadata.ts`, every widget that calls any of the four, with
  its category. The kept row stands alone only if it renders every widget on that list; for each category whose callers it
  does not render, the team's row of that category is kept too (one seam row per category, at most four rows, so at most 3
  more than planned). The deliberate break at C-3 bypasses `metricsQueryOptions` in a widget the kept row covers.
- **Counts.** 15 ids leave the file (8 merged away + 7 replaced) and 2 are added in `useMetricsData.test.ts`, so the file
  keeps 133 - 15 = 118 ids: 110 unmodified (the 109 outside the three families plus the kept seam row) and 8 that gain an
  assertion. With extra seam rows it keeps up to 121. The two files together hold 120 ids after the plan.
- **Pins a rule:** the query options (above); the window-picking arithmetic is already in `dateWindow`. **Proves wiring,
  and pins the loading and failure looks (every one stays, named by FE7's slice 11 table):** a dashboard as it opens, the
  dim under a spinner while the window changes, answers out of order, one failed request marks only its chart and comes
  back on retry, the aging chart's missing Doing columns.
- **Production change needed:** none, and `BaseMetricsView.tsx` stays untouched while FE5 is open (the rule FE7 set).
- **Risk to the target.** This is the file most likely to miss. The plan removes 15 of 133 renders, 11 % by count. DESIGN
  estimated about 15 % on the reading that the removed rows load every chart and so weigh more than the median test; the
  cut needed is 19-24 %. C-1's profile says what those 15 weigh. If the measurement cannot reach, record the reason (the AC
  allows it) and the budget floor moves to 2.1 x that file (Q3): 46 s. Do not change what the 133 tests pin (FE7's finding:
  mounting one category instead of all changes the pinned looks).

### ReportsPage.removal.test.tsx (593 lines, 21 tests)

- **Likely cost.** `renderTheReportsPage` mounts the page with the query client, router, rbac, licensing, terminology and
  the DataGrid; every test then picks rows through `selectTheRowOf`, which finds a row with `rowOf`. **`rowOf` runs
  `getAllByRole("row")` and, for each row, two `queryByRole("link", { name })` calls**: a role query inside a per-row loop,
  the pattern `docs/ci-learnings.md` ("Use the cheap query unless the role is what you are testing") says never to write,
  and the cost grows with the rows. `pickTheThree` calls it three times, and nine tests call `pickTheThree` or the same
  picks. Then tooltips, dialogs and `findByRole("dialog")`.
- **FE7 pattern.** Slice 5 already moved the pick limit to functions. Two new levers: (a) **a cheaper `rowOf`** (EXTEND the
  shared kit: find the row by its text and read the checkbox with a cheap query), which helps all six files that import it;
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

### ReportsPage.combinedPdf.test.tsx (612 lines, 18 tests)

- **Likely cost.** The same page mount and `selectTheRowOf` per test, with two to five picks each; the real `PrintFrameHost`
  then **draws the actual reports into a hidden frame** (`ReportPrintBundle`, a style cache into the frame, `CssBaseline`),
  for every test that lets reads answer: about 12 of the 18. Reading a report document is probably the larger share of these
  tests; the grid and `rowOf` the smaller. (Hypothesis: a CPU profile of two tests at B-1 splits them.)
- **FE7 pattern.** (a) The cheaper `rowOf` of the removal file (shared). (b) **Rules on pure functions**:
  `useReportsPdfExport` holds two private pure functions, `failedAmong` (which listed reports a failure names, matched by
  id and owner) and the decision inside `leaveOutTheFailedOf` (offered only when something failed and something would be
  left; leaves out only the failed, not another report of the same Team). Five page tests pin them by drawing real
  reports (the `Leave it out` tests and the `Retry` test). Extracting them is a **production change** (D-6561-4) and the only one in this Story that was
  recommended, held back until the maintainer says yes (Q7). (c) **`NoPdfDialog` rendered alone** for the button sets: Cancel, Leave it out, Retry against Cancel, Retry.
- **Pins a rule:** "Leave it out unpicks every report that failed", "is not offered when it would leave nothing",
  "leaves out only the report that failed, not the other report of the same Team" (3 tests, move to the function tests and the
  dialog); the buttons of the failure dialog (dialog alone). **Proves wiring (page, stays):** two Teams as one PDF each on a
  page of its own; the table's order as sorted; once the dialog closes the frame goes; nothing prints while a report is on its
  way; picks changed while preparing leave the PDF as pressed; the row's PDF icon and Export PDF hold each other back (2 tests);
  preparing look; a reader who is Admin of none may still export; a failing report named; Leave it out unpicks and prints the
  rest (one); Retry; part of the page failed; nothing picked disables with a tooltip.
- **Production change needed:** none by default. The one pure extraction (D-6561-4) is held back until the maintainer says
  yes (Q7). On the default path the three rule tests stay as page tests and this file's cut rests on the cheaper `rowOf`,
  on "Cancel closes the word that no PDF was made" moving to `NoPdfDialog` alone, and on merging the two "held back"
  tests (all three are in the change table; the plan is in "Slice B").

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-06)

| # | Decision | Options | Chosen (recommended) | Why / trade-off |
|---|---|---|---|---|
| D-6561-1 | What "done" means per file | (a) the brief's line exactly: max over >= 5 plain `main` runs under 17.5 s. (b) the line with 1 s margin (16.5 s). (c) as fast as possible | (b) as the design target, reported beside (a); (a) as the AC and the figure that stops a file | The no-margin cut (a) stops a file; a file between (a) and (b) passes and is recorded as passing with no margin. The budget floor is `ReportsTab.createWizard` at 17.5 s, so past that nothing is gained; a file at 17.4 s would set the floor with no margin and any slow runner would redden the new budget. The table in "The target" gives the cut each file needs |
| D-6561-2 | How the maxima are read | (a) as plain numbers. (b) as slow-runner readings | (b) | T4 measured the runner as bimodal (1.62) and the 26.5 s as the slow reading. A slow-runner run among the budget step's >= 5 is optional: when none is slow (Duration under 950 s), L is multiplied by 1.62 (D-6561-13) |
| D-6561-3 | The lever | (a) render the smallest unit that owns the behaviour (section, dialog, hook), keep one wiring per flow. (b) fake timers only. (c) configuration. (d) split a slow file into two to halve each file's sum. (e) mount fewer widgets in the metrics tests | (a), with FE7's extraction where a rule is still in a component | (b) is what slice 4 did and "did not shorten". (c) FE7 measured nine candidates and none passed the A/B rule. (d) moves seconds between files and keeps the wall time and the worker occupancy, so the Story would meet the letter of the AC and not its title; it stays an open fallback for the maintainer (Q4), not a default. (e) changes what the 133 loading tests pin, so it is not behaviour-free (FE7's finding) |
| D-6561-4 | Production code | (a) none anywhere. (b) one pure extraction in `useReportsPdfExport` (`failedAmong`, `leaveOutPlanOf`) into `printFailures.ts`, only on the maintainer's yes | **(a) by default; (b) only if the maintainer says yes (Q7)** | Revised by D-6561-17: the AFK pass took (b) as recommended, but a production change should not rest on an unanswered veto point. The rule "what a failed export may leave out" is private inside a hook and today only provable by drawing reports; moving two pure functions into `src/pages/Reports/` is a `refactor(reports)` commit with no test change and identical behaviour, then tested in milliseconds and covered by one Stryker run (D-6561-15). Without it, three tests keep drawing real reports and `combinedPdf`'s plan is the skip path in "Slice B". |
| D-6561-5 | Moves and ids | (a) keep every surviving id. (b) allow a test to move to a new file when the unit it renders changes | (b), with rows | A test that now renders a section or dialog belongs beside it. FE7's contract already allows a moved id with a row; the pure move is its own commit before any edit |
| D-6561-6 | One harness for both Refinement files | (a) a helper in `src/tests/`: a stateful host that renders a section with a value and hands back its changes. (b) copy a host into each file. (c) render the form with more stubs | (a) | Two files and about 40 tests need the same host. (c) leaves the form's own hooks and three async loads in every test. The helper is test-only, named after what it does (`renderRefinementSection`), and carries one plain comment on why it keeps the value in state |
| D-6561-7 | The `rowOf` of `BulkReportsTestKit` | (a) leave. (b) find the row by text and read the checkbox with a cheap query | (b), **only if the spike in B-1 shows a saving**; the **six** importing files (`removal`, `combinedPdf`, `reading`, `ReportsPage.test`, `ReportsPage.usageData.test`, `BulkReportWizard.test`) rerun and pass unchanged | It breaks the "never a role query inside a per-row loop" rule today. It is shared, so the universe of the change is the kit's one exported helper and those six files; none of their assertions changes |
| D-6561-8 | `BaseMetricsView.loading` | (a) leave and record. (b) merge the read-only per-owner x category families and move the query-options rule to a hook test, measured first | (b), with a stop rule | FE7 dropped the file on an extraction-only ceiling; a merge and a hook test were not weighed. If C-1's spike projects a cut under 19 %, the file is left alone, the reason is recorded, and the budget floor follows it (Q3) |
| D-6561-9 | `OverviewDashboard` | (a) leave. (b) a second test file with `DataOverviewTable` stubbed for the tests that read no row. (c) extract the header-action rules into a pure function | (b) first; (c) only if the spike shows (b) saves under 20 % | (b) changes no production code and keeps wiring proved by the real-table tests that stay; (c) is the textbook "rule on a function" but edits an RBAC-gated page of 725 lines. (b)'s risk is a stub hiding a table error in the moved tests; the remaining real-table tests render the same table |
| D-6561-10 | Stop rule per slice | (a) finish every slice. (b) at the step that puts a file's moves in, compare the projected cut with the file's required cut; below it, stop that file, record the number and flag | (b), steps named per slice in "Stop rule" (A-2/A-3/A-4, B-3/B-4, C-2/C-3) and measured as D-6561-20 says | FE7's slice 11 was dropped at its profile, which was right; but six of eight rewrite slices missed their estimate and ran on. A stop that records "target not met, because ..." is allowed by the AC and cheaper than a rewrite that does not reach. The earlier wording ("after step 2") named a step that, in two slices, has none of the file's moves in |
| D-6561-11 | How a slice measures | (a) FE7's: summed per-test seconds of the touched files, alternating before and after, 5 pairs, on a shared machine. (b) the same, plus the load average sampled at the start and end of each run, plus user+sys CPU seconds of the run, a pair valid only when all four samples (the 1-minute load average at the start and at the end of both its runs) are at or under half the cores | (b), with the minimum of D-6561-20 | FE7's figures were "indicative" because load swung; CPU seconds react less to a shared machine. Every slice also saves the before id list from a results file, the coverage of the touched directory, and the CI/local ratio of the file (from T4's seven CSVs) so its local target in seconds is stated before the first edit. The decision metric is summed per-test seconds; CPU seconds are a cross-check |
| D-6561-12 | Slices and order | one slice per file / by what files share / one slice | By what they share, in the order A ModifyTeamSettings x3, B ReportsPage x2, C OverviewDashboard and `BaseMetricsView.loading` | A holds the two largest maxima and one new harness; B carries the shared kit and, only on the maintainer's yes, the one production extraction; C has the lowest confidence and goes last, with its own stop rule. Each slice is at most one day and ends with its own before and after |
| D-6561-13 | The budget step | (a) a roadmap step. (b) a follow-up commit after the push, once >= 5 `main` runs are in, with the slow-runner rule | (b) | It needs five CI runs after a push, so it cannot be a step of this run (the maintainer pushes nothing in AFK). Rule: B = ceil(2.1 x L) per ADR-237 and T4's recipe; the >= 5 runs are plain, complete (a run that stopped at its first failure is excluded, as T4 did), and a slow-runner run (Duration of 950 s or more) among them is optional: when none is slow, L is multiplied by 1.62. If the derived B is not below the budget in force, no commit is made, the value stays and the derivation is recorded. Provisional value 37 if all seven reach their cut (L = 17.6 or 17.5, both give 37; with slice A alone L = 22.0 and B = 47; see "Expected budget outcome"). The first push is judged against the 56 s in force, and the Story stays open until this commit lands |
| D-6561-14 | T12 #6539 | (a) ignore. (b) re-read each file at slice start; on a conflict with trunk stop and report | (b) | T12 touches some of these files for order and load. Nothing of T12 exists in the repository yet, so which files is unknown (Q2). Never rebase autonomously |
| D-6561-15 | Mutation | (a) Stryker over all touched areas. (b) N/A for tests; Stryker once, last, over `printFailures.ts` only if the maintainer says yes to D-6561-4's extraction | (b) | Stryker mutates production code and no production file changes otherwise. If the extraction happens: at least 80 % on the new file, on direct tests, after the code is frozen. Without the extraction no production file changes and mutation testing is N/A: a maintainer-visible exception to the per-feature strategy (DEVOPS table) |
| D-6561-16 | Rigor profile (proposal) | `standard` for all three slices; `thorough` for none | `standard` | Mechanical rewrites under a settled contract; the guard and configuration (the thorough cases in FE7) are not touched. DELIVER asks anyway, and passes `model` explicitly on every dispatch |
| D-6561-17 | Q7 default, and the scope text | (a) keep the AFK default (take the extraction). (b) default to skip until the maintainer answers | (b) | Revised after the review gate. Q7 is a veto point on a production change and has no answer. Made consistent here: D-6561-4, Q7, the pre-requisites, the change table, the wiring count, the conditional `printFailures` tests, slice B. The brief's IN list allows one named `refactor(reports)` commit if the maintainer says yes, so the documents agree. Without it, `combinedPdf`'s cut rests on the cheaper `rowOf`, one dialog test and one merge, which probably misses 17 % (D-6561-18) |
| D-6561-18 | The expected budget outcome | (a) 37 s as the expectation. (b) a band, with each case named | (b) | 37 s only if all seven reach their cut. 45 s if `combinedPdf` stays at 21.0 (the likely case without the extraction), 46 s if `BaseMetricsView.loading` stays at 21.7 (the design's own estimate there is about 15 %, 11 % by test count, against 19 % needed, so it probably does), 47 s if Overview stays at 22.0. Plan on 45 to 47 s. Overview's saving restated: 28 of 51 tests leave the real table, so it reaches 20 % only if the tables are at least 36 % of a mount. A production fallback (Overview extraction, Q6) stays the maintainer's call and is not taken by default |
| D-6561-19 | The guarantee, made mechanical | (a) a script that prints counts for a person to read. (b) scripts that fail | (b) | `check-behaviour-free.sh` exits 1 on a removed `expect(` with no row, compared per file against the moved-assertions file, with one counter (`\bexpect\(`) for removed lines and rows, `--no-renames` on every diff, untracked new test files read as added, removed `.not.` compared with added, a longer weaker-matcher list (partial, and said so). Since the second revision pass the rows must also equal the removals per file in both directions, each row's text must be a removed line of its file, and `check_ids.py` fails on a row whose old id is still present unless the row is marked `kept:`. `run-touched-files.sh` runs every baseline, changed and new test file and diffs the union of ids, refusing a stale report. The report is not controllable by `--outputFile` here, so it is deleted before the run and checked after. The baseline was not stale (regenerated, byte-identical). The id diff pins names, not assertions; the removed-`expect(` rows catch assertion loss inside a surviving test. Biome's `noSkippedTests` does not flag `skipIf`/`runIf`/`todo` (tested) |
| D-6561-20 | Stop-check step and measurement | (a) "step 2" of a slice, any number of pairs. (b) the step that puts the file's moves in, 5 alternating pairs, at least 3 valid | (b) | Amends D-6561-10 and D-6561-11. A pair is valid when all four samples (the 1-minute load average at the start and at the end of both its runs) are at or under half the cores. The summed per-test seconds are `assertionResults[].duration` (milliseconds) of the file's suite in `Lighthouse.Frontend/test-results.json`. Below 3 valid pairs of 5 the slice records "unmeasured, machine loaded" and takes the stop; there is no retake loop, because a loop on a shared machine can run all day and the maintainer can order a quiet-machine run |
| D-6561-21 | Seams, backstops and breaks | (a) trust the removal. (b) name each seam, backstop and break | (b) | Mapping warnings: one form test names every kind at once (or the three tests stay if the form lists only the first reason); sentence text lives in `stateMappingValidation.rawLists.test.ts`, rendering in the `blocked autosave feedback` block. Loading seam: the kept row is the team's Flow Metrics; it stands alone only if it renders every caller of `metricsQueryOptions`, else one row per category. Deliberate breaks added: `refinementSettingsBlockers` and `teamAutoSaveBlockers` returning `[]`, per section kind; `metricsQueryOptions` bypassed in a widget the kept row covers. Backstop rule: a wiring test is not removed until its backstop is green; the list is finished at A-1, B-1 and C-1. The two harness-log tests of `renderRefinementSection` are named for what a test author reads (the value now; every change in order) |
| D-6561-22 | DEVOPS conditions | (a) as written in AFK. (b) rollback order, first-push judgement, mutation exception, bimodal CI | (b) | Rollback: budget commit first, then the rewrite commits, then the harness commit last, because `readiness` and `refinementNeed` both depend on it; the refactor commit (if taken) separately. The first push is judged against 56 s and the Story stays open until the budget commit lands. A slow-runner run among the five is optional; without one L is multiplied by 1.62 (CI is bimodal). With no production file changed, mutation is N/A: a maintainer-visible exception to the per-feature strategy. `/continuous-improvement before` does not apply (feature Epics only; #6506 is an improvement Epic) |

## Wave: DESIGN / [REF] Slices (carpaccio, at most one day each)

Every slice's first step declares its universe, saves the **before** ids from a results file, the before seconds (D-6561-11,
D-6561-20), the coverage of its directory, and states the file's local target. Its last step saves the after figures. No
slice writes a `@`-tag into code, and the moved-assertions list lives in the feature workspace
(`measurement/slice-X-moved-assertions.md`).

**Backstop rule, every slice.** A wiring test is not removed until the test that replaces it (its backstop) exists and is
green, and the removal is a later commit than the backstop's. Where a backstop already exists, the slice file names its
title beside the wiring test it replaces (slice A's known pairs are in the table below). Where one has to be written, the
list of new backstops is **finished at the slice's measuring step** (A-1, B-1, C-1), because only the measurement shows
which sentences and rows no function test pins.

### Slice A: ModifyTeamSettings x3 (`refinementNeed`, `readiness`, `test`)

Shares: `renderGravitysSettingsForm`, `replaceTheNumberIn`, the form, the fixtures. Directory: `src/components/Common/Team`.

| Step | What | Measures |
|---|---|---|
| A-1 | Declare and measure. Profile one band test and one readiness test; run the A-1 gate (below: one band test, form against section); count the form tests per section; check that every mapping-warning sentence and each of the seven invalid scenarios is pinned by a function test (the pairs below, by the criterion under them), and **fix the "up to 5" loop rows of `ModifyTeamSettings.test` to the number that check allows**, editing the change table in the same commit | Local summed seconds of the three files, CI/local ratio, local target, go or no-go for A-2 |
| A-2 | The harness (own commit, test-only) and `readiness`: display and validation tests to the section render, wiring tests stay | `readiness` against 24 % (stop check, below) and moved-assertions rows |
| A-3 | `refinementNeed` the same way for band, cadence and stage rules, one "saves nothing while invalid" wiring per section kind | `refinementNeed` against 34 % (stop check) and rows |
| A-4 | `ModifyTeamSettings.test.tsx`: keep the Doing mapping-name wiring, replace the other three by one form test that names every warning kind at once (seam below), shrink the invalid-scenario loop to what the function tests do not cover | `ModifyTeamSettings.test`: stop check at 1 % (rows match the change table, seconds not higher than before) and rows |
| A-5 | Close: after seconds, id union diff, `check-behaviour-free.sh`, coverage of the directory (FE7 left it at 96.26 / 86.23), the deliberate breaks, `pnpm build` and `pnpm test` | Per-file cut against the table |

**Stop rule.** Cut = 1 - after/before, as defined in "The target, as arithmetic". The step that has a file's moves in is
the stop check for that file: A-2 for `readiness`, A-3 for `refinementNeed`, A-4 for `ModifyTeamSettings.test`. It uses the
measurement of D-6561-20 (summed per-test seconds of that file, 5 alternating pairs, at least 3 valid). **A file stops when
its projected cut is less than its required cut**, the no-margin figure of the table in "The target" (24 % for `readiness`,
34 % for `refinementNeed`). The margin figures (28 %, 38 %) are the design target and are reported beside it; a file between
the two passes and is recorded as passing with no margin. Fewer than 3 valid pairs is "unmeasured, machine loaded" and also
stops the file.

`ModifyTeamSettings.test` has a required cut of 1 %, which five pairs cannot resolve. Its stop check is instead: the rows
removed match the change table, and the summed seconds are not higher than before. Anything finer is recorded as not
resolvable.

**What a stop does.** A stopped file keeps the moves already committed, since they are behaviour-free and not slower. It gets
no further work in this run and is recorded (the number, and "target not met, because ..."), and the budget floor follows it
(Q3). The harness commit stays. The other files of the slice carry on.

**A-1 gate (one measure).** The per-test seconds of one band test of `refinementNeed`, including its typing, in the form
against the same test in the section render, 5 alternating pairs under D-6561-20 (at least 3 valid). The ratio r is the
form's seconds over the section's. The gate tests cost only: that the host shows what the form shows is proved at A-2, by the
helper's own tests and the deliberate break, not here. What r has to be follows from the arithmetic. Assume the 31 form tests
carry the file's time at the same cost c each, 23 of them move to the section at c/r, and 8 stay on the form at c:

    after/before = (8 + 23/r) / 31, so the cut = 1 - (8 + 23/r) / 31

A cut of 34 % (the required cut) needs 23/r = 0.66 x 31 - 8 = 12.46, so r = 1.85. A cut of 38 % (the design target) needs
23/r = 0.62 x 31 - 8 = 11.22, so r = 2.05. **Stop at A-1 when the measured r is below 1.85**: nothing moves, and the run goes
to the hold. At 1.85 or more the slice goes on, and an r between 1.85 and 2.05 may end as "passing, no margin" at A-3. For
`readiness` (18 of 24 move, 6 stay) the same arithmetic, after/before = (6 + 18/r) / 24, needs r = 1.47 for 24 % and 1.60 for
28 %, so the figure for `refinementNeed` is the stricter one and the gate rests on it.

**Backstops known today (titles of existing tests):**

| Wiring test that goes | Its backstop |
|---|---|
| `holds back autosave while a mapping's name hides one of its states in To Do, and saves once it is renamed` | `names the To Do list when the shadowing sits there` (`stateMappingValidation.rawLists.test.ts`) |
| `holds back autosave while a mapping's name hides one of its states in Done, and saves once it is renamed` | `names the Done list when the shadowing sits there` (same file) |
| `holds back autosave while a mapping is named after a state another mapping groups, and saves once it is renamed` | `refuses a mapping that groups another mapping's name, with the sentence the server sends` (same file) |
| Kept: `holds back autosave while a mapping is named after a Doing state it does not group, and saves once it is renamed` | `refuses a name that sits beside one of its mapping's states, with the sentence the server sends` and `holds back the autosave for that name with the same sentence` (same file) |
| Rows of the loop `does not auto-save when X is invalid` that go | the `it.each` at the top of `teamAutoSaveBlockers.test.ts`, one row per field (titles copied at A-1, matched by the criterion below), and the rows of the loop that stay |

**When "a function test already pins it" is true (the criterion for the A-1 rows).** A loop row of `does not auto-save when
X is invalid`, or a mapping-warning sentence, goes only when a row of the `it.each` in `teamAutoSaveBlockers.test.ts` (or the
test in `stateMappingValidation.rawLists.test.ts` named in the table) builds the same invalid input and asserts the same text.
That means (a) it sets the same field to the same invalid value as the loop row's `scenario` entry (an empty list and
`undefined` are not the same value), and (b) it asserts that the blockers name that field or, for a warning, the same sentence
the form shows. Compare field, value and sentence row by row, and write the two titles beside the loop row in the table. A
row with no such pair stays, or its function row is written first and is green before the loop row goes (the backstop rule).

**The mapping-warning seam.** Where each lives: the sentence of every warning kind is built by
`stateMappingValidation` and pinned there, at function level, in `stateMappingValidation.rawLists.test.ts`; the form shows
it through the blocked-save message (`teamAutoSaveBlockers`), and the form test of that rendering is the
`blocked autosave feedback` block of `ModifyTeamSettings.test.tsx`. Removing three of the four form tests would leave the
form's feeding of the To Do, Done and other-mapping cases unproved, so A-4 adds **one** form test, "says what holds back the
save for a mapping of every kind of mistake at once", that renders one form with a mapping of each of the four kinds and
asserts the rendered sentence of each kind. If A-1 shows the form lists only the first reason, a one-render test cannot
show all four; then the three tests stay (one rendered sentence per warning kind), this file's cut comes from the loop
rows alone, and the change table ("Changes by design") and the list of tests that stay on the form are edited in the same
commit to say so. Reading the form today, it lists every reason (`formInvalidReasons.map`), so the one-render test is
expected to work.

### Slice B: ReportsPage x2 (`removal`, `combinedPdf`)

Shares: `BulkReportsTestKit` (`renderTheReportsPage`, `rowOf`, `selectTheRowOf`), the page, the grid. Directory: `src/pages/Reports`.

| Step | What | Measures |
|---|---|---|
| B-1 | Declare and measure. Profile one `pickTheThree` test and one export test; spike a cheap `rowOf` against three tests; spike a render of `RemoveSelectedDialog` alone against its page test; split the export tests' cost into grid, `rowOf` and drawing the reports | Seconds of both files, ratio, local target, whether D-6561-7 is worth taking, and the list of new backstops |
| B-2 | `rowOf` and `selectTheRowOf` with cheap queries in the kit (own commit); rerun the six importing files unchanged | Seconds of all six files |
| B-3 | `removal`: dialog and hook renders for the confirmation, warnings, results, Escape and cache tests; page wiring stays | `removal` against 13 % (stop check) and rows |
| B-4 | `combinedPdf`, default path (Q7 not yet a yes): `NoPdfDialog` alone for "Cancel closes the word that no PDF was made ...", the two "held back" tests merged into one, the effect of the cheaper `rowOf` read. **Only on a yes to Q7:** first the `refactor(reports)` commit extracting `failedAmong` and `leaveOutPlanOf` into `printFailures.ts` (no test change, existing tests pass), then `printFailures.test.ts` and the three "Leave it out" rule tests move, with rows | `combinedPdf` against 17 % (stop check), rows, coverage |
| B-5 | Close as A-5; Stryker over `printFailures.ts` only if the extraction was taken | Per-file cut against the table |

**Skip-path plan for `combinedPdf` (the default).** Cuts that still apply: (1) the cheaper `rowOf` (B-2, if B-1 shows a
saving; shared with `removal`); (2) "Cancel closes the word that no PDF was made, prints nothing and keeps what was picked"
moves to `NoPdfDialog` alone; (3) the two "held back" tests (`while a PDF of several is prepared, a row's PDF icon is held
back and starts no second` and `while a row's PDF is prepared, Export PDF is held back and starts no second`) merge into
one new test, and **that merge is in the change table** (2 ids gone, 1 added, every assertion kept, a row for each
removed `expect(`). Cuts that do not apply: the three "Leave it out" rule tests stay as page tests and `printFailures.ts`
is not created. Three of 18 tests change (two renders go, 11 % by count) against 17 % needed, so a miss and a 45 s budget
are the likely result unless `rowOf` carries the rest. A later yes to Q7 adds the extraction to B-4 as its own commit.

**Stop rule.** `removal`'s moves are in at B-3 and `combinedPdf`'s at B-4; each is measured there as D-6561-20 says. If B-1
shows the export tests spend their time drawing reports, B-2 is skipped for `combinedPdf` and only B-4's cuts count; if
its cut is then under 17 %, record it. Fewer than 3 valid pairs of 5 is recorded as "unmeasured, machine loaded" and takes
the stop.

### Slice C: OverviewDashboard and `BaseMetricsView.loading`

Shares: nothing in code; both are page-level mounts of a large view, both dropped by FE7 on an extraction-only ceiling, and both
pin loading or failure looks. Directories: `src/pages/Overview`, `src/pages/Common/MetricsView`. The lowest-confidence slice.

| Step | What | Measures |
|---|---|---|
| C-1 | Declare and measure. Spike: three header-gating tests with `DataOverviewTable` stubbed against the real one. Count `everyOwnerAndCategory` rows and the renders per family in `BaseMetricsView.loading`; spike: the query-options rule as a `renderHook` test; list the callers of `metricsQueryOptions` per category (seam row) | Seconds of both files, projected cut per file, go or no-go per file, list of new backstops |
| C-2 | Overview: pure move of the 28 non-row tests to `OverviewDashboard.gating.test.tsx` (own commit), then the stub | `OverviewDashboard` against 20 % (stop check), rows (moves) |
| C-3 | `BaseMetricsView.loading`: the hook test for query options, merge the open and the settled families, the seam row(s) kept; the deliberate break through a widget the kept row covers | `BaseMetricsView.loading` against 19 % (stop check), rows |
| C-4 | Close as A-5. Every test listed in FE7's "pin the loading and failure looks" table still exists and asserts what it did | Per-file cut against the table |

**Stop rule.** Overview's moves are in at C-2 and `BaseMetricsView.loading`'s at C-3; each file is judged on its own at
that step by D-6561-20's measurement, and either may stop at C-1 with a recorded number. If Overview's spike saves under
20 %, the fallback (D-6561-9c) is a question for the maintainer, not a default (Q6).

### After the slices (not a roadmap step in this run)

See "Budget step". Everything above is local and committed per step; nothing is pushed in AFK.

## Wave: DESIGN / [REF] Reuse Analysis

Contract shapes as `nw-solution-architect` asks for them: **pure function** (return-only), **bounded change** (declared
mutation set, aggregate-bounded universe, asserted by the mechanism named), **unbounded preservation** (none here: nothing in
this Story previews or validates by side effect).

| Need | Existing thing | Decision / contract shape / assertion mechanism |
|---|---|---|
| Rules as functions | `refinementBand`, `RefinementCadenceSettings` exports, `readinessErrors`, `discussionRules`, `refinementSettingsBlockers`, `stateMappingValidation`, `pickLimit`, `pickedReports`, `dateWindow`, `categoryMetadata`, `teamAutoSaveBlockers` | REUSE as destinations of moved assertions. **Pure function.** Assertion: the function's own tests |
| Render a section with state | `RefinementBandSettings`, `RefinementCadenceSettings`, `StageRulesSettings`, `ReadinessSettings` (props: value and `onChange`); the last blocks of `refinementNeed` already render two of them | EXTEND with one test helper, `renderRefinementSection` (CREATE NEW; the search is recorded under the table). The name stays because all five units it renders are parts of the Refinement section: `ReadinessSettings` is rendered by `RefinementSettingsSection` and its value is `refinement.readiness`. **Bounded change**: universe = the new helper `src/tests/renderRefinementSection.tsx`, its own test file, and every importer, named: `ModifyTeamSettings.refinementNeed.test.tsx`, `ModifyTeamSettings.readiness.test.tsx` and the section test files slice A creates (`RefinementBandSettings.test.tsx`, `StageRulesSettings.test.tsx`, `ReadinessSettings.test.tsx`, and a `.tsx` beside `RefinementCadenceSettings.test.ts` if the moved cadence tests need a render; names confirmed at A-2), and nothing else; assertion: the surviving form wiring tests still go through the real form |
| Query client and providers for tests | `renderWithQueryClient`, `createMockApiServiceContext` | REUSE unchanged. **Pure function** |
| Reports page test kit | `BulkReportsTestKit` (`renderTheReportsPage`, `rowOf`, `selectTheRowOf`), `ReportsTestKit`, `ReportPrintTestKit` | EXTEND `rowOf` only (D-6561-7). **Bounded change**: universe = one exported helper and its six importing test files (`removal`, `combinedPdf`, `reading`, `ReportsPage.test`, `ReportsPage.usageData.test`, `BulkReportWizard.test`); assertion: those six files pass unmodified with the same sorted id list |
| Dialog and hook units | `RemoveSelectedDialog` (props: `reports`, `onCancel`, `onDone`), `NoPdfDialog`, `useSelectionRemoval`, `useBulkReportRemoval`, `useReportsPdfExport` | REUSE by rendering them alone (`renderHook` for the hooks). No change to them. **Bounded change** as hooks keep their effects |
| The failure rule of a combined PDF | private `failedAmong`, `isTheSameReport`, and the decision in `leaveOutTheFailedOf` inside `useReportsPdfExport.ts` | CREATE NEW `src/pages/Reports/printFailures.ts`, **only on the maintainer's yes (Q7); not taken by default**, justified: no existing module owns it and the rule is only reachable today by drawing real reports. **Pure function** (return-only: reports and failed list in, plan out). Assertion: planted lists in a new `printFailures.test.ts`, then one Stryker run |
| Query options of metrics | `metricsQueryOptions` in `useMetricsData.ts`, `useMetricsData.test.ts` | REUSE: the hook test goes beside the existing one. **Pure function** (a constant) plus a hook that spreads it; assertion: the cache of an app-default client after `renderHook` |
| Overview with stubbed tables | `OverviewDashboard.test.tsx` fixtures (`renderWithProviders`) | EXTEND by a second file; the fixture moves with the tests or is exported once (decided at C-2, a pure move). **Bounded change** |
| Budget derivation | `Scripts/test-timings/check_budget.py`, `test-time-budget.json`, T4's recipe in `docs/ci-learnings.md` | REUSE unchanged; the budget step edits one number. **Bounded change**: universe = `budget_seconds` of `frontend`; assertion: the script on the saved CSVs |
| Time budget guard and allow-list | `src/tests/timeBudget/`, `slowTests.ts` (empty list, per-test limit 2 s) | REUSE. Per-test entries are not expected to appear; if a rewritten wiring test exceeds 2 s it is a defect of the rewrite, not an entry |

**Search record (2026-11-06, `Lighthouse.Frontend/src`, second revision pass).**

- A stateful host. `grep -rln 'useState' src/tests` found nothing: no shared helper keeps a controlled value in state.
  `grep -rln 'useState' --include='*.test.tsx' src` found hosts private to 10 test files, each written for its own component
  (`DataGridBase`, `WipSettingDialog`, `DashboardHeader`, `WidgetShell`, the Portfolio and Team detail pages, the two
  forecasters, and `PortfolioSettingsInState` in `DependenciesComponent.directionTip.test.tsx`, which holds a Portfolio's
  settings and applies every change, as the edit page does). None is exported and none renders a Refinement unit. So the
  helper is new, and `PortfolioSettingsInState` is its nearest model.
- Existing section test files. `find src -name 'RefinementBandSettings*' -o -name 'StageRulesSettings*' -o -name
  'ReadinessSettings*' -o -name 'RefinementSettingsSection*' -o -name 'RefinementCadenceSettings*'` found the five components
  and one test file, `RefinementCadenceSettings.test.ts`, which holds function tests and no JSX. There is no test file for the
  band, the stage rules, the readiness or the section, so the moved tests create `RefinementBandSettings.test.tsx`,
  `StageRulesSettings.test.tsx` and `ReadinessSettings.test.tsx`. The moved cadence tests extend what
  `RefinementCadenceSettings.test.ts` covers in a `.tsx` file beside it: a `.ts` file cannot hold JSX, and renaming it would
  move its ids, which no baseline list pins.

## Wave: DESIGN / [REF] Budget step (follow-up commit, after a push)

Not a step of this run, because it needs at least 5 `main` runs after a push and the maintainer pushes nothing in AFK. The
**first push is judged against the 56 s budget still in force**, and the Story **stays open until this commit lands**.

1. The maintainer (or the next session) pushes the slices; the orchestrator waits for >= 5 complete plain `ci.yml` runs on
   `main` ("plain" and "complete" as defined in "Acceptance criteria and dependencies"; the `test-timings-frontend`
   artifacts; a run that stopped at its first failure is partial and excluded).
2. For each, record the Vitest `Duration` and class the run by one boundary: **950 s or more is slow, anything under 950 s
   is fast** (fast runs have taken about 610-830 s, slow runs about 1,070-1,160 s). Never average across the classes; keep
   the maximum per file.
3. L is the largest per-file test time over those runs among files not on the allow-list. Rule as T4 applied it:
   B = ceil(2.1 x L). **Name L in the derivation**: today it is `ModifyTeamSettings.test` at 17.6 if that file is only
   trimmed, or `ReportsTab.createWizard` at 17.5; both give 37; with slice A alone it is `OverviewDashboard` at 22.0, which gives 47. **If none of the runs
   is slow, multiply L by 1.62** (T4's measured ratio): a budget derived on fast-runner runs alone fails on the first slow
   runner. A slow run among the five is optional, because this scaling covers its absence; there is no waiting for one.
4. If the derived B is not below the budget in force, make no commit: keep the current value and record the derivation
   (runs, files, L, B) in this file. Otherwise edit `budget_seconds` of `frontend` in `Scripts/test-timings/test-time-budget.json`, replay `check_budget.py --stack
   frontend` on the saved CSVs, and write the derivation (runs, files, L, B) in this file and in the `docs/ci-learnings.md`
   test-time-budget bullet if its figures name the old value. Commit as `ci(test-infra): ...`; the budget only tightens.
5. If a file of the seven is still over the line, the derivation names it and B follows it (Q3, "Expected budget outcome":
   45 to 47 s is the likely result); do not raise any ceiling.

## Wave: DESIGN / [REF] Risks and open questions

| # | Question or risk | Recommended answer taken in AFK | Needs the maintainer |
|---|---|---|---|
| Q1 | FE5 #6393 (the `BaseMetricsView` split) was open on 2026-11-04. If it landed, the metrics tests can mount one widget and C changes | Check the ADO state at pick-up; as written, `BaseMetricsView.tsx` stays untouched | Only if it landed during this work |
| Q2 | T12 #6539 touches some of these files for order and load; it has no workspace in the repository yet | Re-read each file at slice start; a conflict with trunk stops the slice (no autonomous rebase) | Which files T12 touches, and which Story goes first |
| Q3 | If `BaseMetricsView.loading` (or any file) cannot reach the line, the budget stops at 2.1 x that file. At 21.7 s that is 46 s, not 37; at `combinedPdf`'s 21.0 s it is 45 s; at Overview's 22.0 s it is 47 s | The AC allows "or the reason is recorded"; record it | **Yes**: the maintainer should know the headline 37 s depends on all seven, and on the metrics file and `combinedPdf` in particular; the likely outcome is 45 to 47 s |
| Q4 | Splitting a slow file in two would meet the per-file letter of the AC without running anything faster | Rejected as a default (D-6561-3d) | Only as a deliberate fallback, if a file misses and the maintainer wants the budget anyway |
| Q5 | The 5-run read may contain no slow-runner run, giving an optimistic L and a budget that fails on the first slow runner | Multiply L by 1.62 when none of the runs is slow (Duration of 950 s or more); a slow run is optional (D-6561-13) | No |
| Q6 | Overview fallback extraction (D-6561-9c) is a production change in an RBAC-gated page | Not taken by default; asked about only if the stub spike saves under 20 %, and then it is the maintainer's call | **Yes, if it comes to that** |
| Q7 | The `useReportsPdfExport` extraction (D-6561-4) is a production change | **Skipped until the maintainer says yes** (D-6561-17): slice B runs its default path and the `printFailures` tests are not written | **Veto point, now an opt-in**: a yes adds the extraction to B-4 as its own commit first |
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
   |       failedAmong, leaveOutPlanOf  (NEW, moved out of useReportsPdfExport; production, only on the maintainer's yes, Q7)
   '-- drives ApiServiceContext fakes (driven side, unchanged)       BulkReportsTestKit.rowOf (MODIFIED, cheap query)
 Scripts/test-timings/test-time-budget.json   frontend budget_seconds 56 -> 37 only if all seven reach their cut; 45-47 likely (follow-up, after 5 main runs)
```

New: one test helper, (only on a yes to Q7) one production module (`printFailures.ts`) and one test file for it, a handful of new unit-level
test files and one split test file (`OverviewDashboard.gating.test.tsx`). Modified: seven test files, `BulkReportsTestKit.rowOf`,
`useReportsPdfExport.ts` (imports the extracted functions, only on a yes to Q7), the budget number. Unchanged: all production behaviour, pixels,
E2E, `setupTests.ts`, the reporter and allow-list, every external call (this work adds none).

**Expected budget outcome.** 37 s only if all seven files reach their cut. Without the extraction `combinedPdf` probably misses (45 s) and `BaseMetricsView.loading` probably misses (46 s), and `OverviewDashboard` may miss (47 s), so plan on 45 to 47 s ("The target, as arithmetic"). Production fallbacks stay the maintainer's call.

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
| Extract a pure function from a hook (Strategy-free, Value-in/Value-out) | Weighed for the export failure rule and held back until the maintainer says yes (Q7); fallback for Overview's header actions, also the maintainer's call (Q6) | Moves the rule where it is cheap to test; behaviour identical |
| Domain events, outbox, unit of work, optimistic concurrency, JSON column versus child table | Not applicable | No product behaviour or storage changes |

Enforcement tooling for the styles above: the existing Biome and `tsc` gates, the time-budget reporter (per-test) and
`check_budget.py` (per-file, this Story's own target), and `reportsPage.enforcement.test.ts` and
`layerBoundaries.enforcement.test.ts` for module boundaries. No new tool.

## Wave: DEVOPS / [REF] Platform readiness

Existing platform; nothing new is deployed and no workflow changes in this run. Written 2026-11-06 under `/afk`, revised
after the review gate.

| Topic | Answer |
|---|---|
| Environment | Local Vitest and the existing "Verify Frontend" job. |
| CI/CD | Unchanged. The per-test guard and the per-file budget from T4 judge the rewritten files on the first push. The **first push is judged against the 56 s budget still in force**, so a green push proves only that no file got slower than that; the **Story stays open until the budget commit lands**. CI is bimodal (about 610-830 s and 1,070-1,160 s for the same tests), so one run says little. |
| Rollback | Revert the budget commit first, then the rewrite commits (each file's rewrite is its own commit and reverts alone), then the harness commit last, because `readiness` and `refinementNeed` both depend on it. The `refactor(reports)` commit, if taken, is its own unit and is reverted separately, after any rewrite commit that depends on it (the direct tests of its functions go first). |
| Local measurement | `LIGHTHOUSE_TEST_WORKERS=2` on named files, never the full suite while another session is mutating. The load average is sampled at the start and at the end of each run; a pair counts only if all four samples (the 1-minute load average at the start and end of both its runs) are at or under half the cores. Decision metric: summed per-test seconds of the touched files, alternating before and after, 5 pairs, **at least 3 valid pairs**; with fewer the slice records "unmeasured, machine loaded" and takes the stop, with no retake loop. User+sys CPU seconds are recorded beside it as a cross-check. |
| Monitoring | `test-timings-frontend` artifacts, read by hand for the budget step after at least 5 plain `main` runs (a slow-runner run among them, 950 s or more, is optional; else L multiplied by 1.62). No dashboard, no alert. |
| Mutation | N/A when no production file changes (the default), because Stryker mutates production code and this Story edits test files only. That departs from the project's per-feature strategy, so it is a **maintainer-visible exception**, stated again when the rigor profile is agreed. If the maintainer says yes to Q7, Stryker runs once, last, on `printFailures.ts`, kill rate floor 80 %, after the other session's run has finished. |
| Usage-data event | N/A, because no user of the product reaches test files, so no event could show use. |
| Terminology and release notes | N/A, because no screen or word a user sees changes: the rewritten tests keep asserting the seeded default terms, and release notes are written at `/release`, with nothing to say for a test-only Story. |
| RBAC, Clients, Website, UI sketch | N/A, because no route, DTO, page or marketing claim changes. |
| Improvement backlog (`/continuous-improvement before`) | Does not apply: the command is for a new feature Epic, and #6506 is an improvement Epic. FE5 #6393 is checked at pick-up (Q1). |

## Wave: DISTILL / [REF] Scope, reconciliation and baseline (2026-11-06, maintainer AFK)

- Shape: behaviour-free rewrites of seven test files under FE7's contract. No product behaviour is specified, so there is no
  new acceptance scenario to write: the acceptance criterion of every slice is that the existing behaviour stays pinned, and
  the checks below pin that.
- Reconciliation: DISCUSS (brief and Checklist), DESIGN and DEVOPS read; **0 hard contradictions**. Two soft points, both
  resolved in favour of DESIGN and recorded: (1) the DISCUSS checklist says "the budget falls in this Story" while D-6561-13
  makes it a follow-up commit after five `main` runs; read as "in this Story, as its last commit", so the checklist item stays
  open until that commit. (2) DESIGN's counts differ from a run (next table); they are corrections of figures, not of
  decisions, and the run's numbers win. (3) The revision pass after the review gate is D-6561-17 to D-6561-22; the second revision pass changes D-6561-1, -2, -11, -13, -18, -20 and -22 in their rows (the figure that stops a file, an optional slow run, pair validity, rollback order) and decides nothing new.
- UI sketches: N/A, because nothing a user sees changes. No nWave scenario tag in any test; an untagged test is correct.
- Mechanism: Vitest and React Testing Library, run per named file with `LIGHTHOUSE_TEST_WORKERS=2` from `Lighthouse.Frontend`.
  The full suite and Stryker are not run in this wave (another session is mutating on the machine).

### Baseline (run of 2026-11-06 on the pre-slice commit `d45a63265`, 329 of 329 passed, none skipped or todo)

Saved in `baseline/`: `<file>.ids.txt` (sorted `file :: fullName`, from a run, never from source), `<file>.expects.txt`
(count of `\bexpect\(` in the file's source, helpers inside the file included), `summary.md`. `baseline/outside-seven/`
holds the same for the eight files outside the seven that share a helper this Story edits (215 tests, 401 `expect(`),
so "unmodified" is pinned for them by an id list too, not by a sentence.

| File | Tests | `expect(` | DESIGN said | Note |
|---|---|---|---|---|
| `ModifyTeamSettings.refinementNeed.test.tsx` | 47 | 77 | 47 | same |
| `ModifyTeamSettings.readiness.test.tsx` | 24 | 44 | 47 | DESIGN's figure was wrong (16 `it` and 8 `it.each` rows) |
| `ModifyTeamSettings.test.tsx` | 35 | 59 | 33 | includes the nested describes |
| `OverviewDashboard.test.tsx` | 51 | 113 | 51 | only 28 read no row, not "about 35" (counted below) |
| `BaseMetricsView.loading.test.tsx` | 133 | 150 | 133 | same |
| `ReportsPage.removal.test.tsx` | 21 | 37 | 21 | same |
| `ReportsPage.combinedPdf.test.tsx` | 18 | 44 | 19 | DESIGN's figure was one too many |
| total | 329 | 524 | | |

DESIGN's headings and tables now carry these run figures (24, 35 and 18), as the run's numbers win.

**Slice A's before-commit is `d148c2d14`.** `git diff --stat d45a63265 d148c2d14 -- Lighthouse.Frontend` prints nothing, so no
test file or production file under `Lighthouse.Frontend` changed between the commit the baseline was run on and the
before-commit (only documents did). The lists in `baseline/` therefore describe slice A's before state.

**Was the baseline stale? No** (checked in the revision pass, 2026-11-06). The saved lists were made from
`Lighthouse.Frontend/test-results.json`, which the project's reporter writes on every run and which a run that dies early
leaves behind from the run before. The check: delete the report, run the seven files again (329 of 329 passed), regenerate
into a scratch directory, compare. All 14 files (seven id lists, seven `expect(` counts) are byte-identical to the
committed ones.

**What the reporter does here (tested, revision pass).** `--reporter=json` on the command line does not stop the
configured report: `test-results.json` was rewritten (its time stamp moved and the run printed "JSON report written to
..."). An explicit `--outputFile=<path>` on the command line is **not honoured**: no file appeared at the path and the
report went to `test-results.json` as before. So the fix is not an explicit output path but two guards: the report is
deleted before the run, and `baseline6561.py` and `check_ids.py` refuse (exit 2, nothing written) a report older than the
run (`--started`, taken before the run) or with a different test count than the run printed (`--expected-tests`).

Produce an "after" set, and check it, with one script from the repository root (it builds the list of files to run, runs
them with `LIGHTHOUSE_TEST_WORKERS=2`, and diffs the id union):

```
bash docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/run-touched-files.sh <before-commit> \
  docs/feature/story-6561-t13-slowest-frontend-test-files/measurement/slice-X-moved-assertions.md
```

The after-run must contain **every touched file**: the script runs every file that has a baseline list, every test file
changed since `<before-commit>` and every new test file that is not tracked yet, builds the union of the baseline ids over
all of them, and fails when
a touched file is not in the after-run, a test did not pass, a baseline id is gone without a row in the moved-assertions
file, or an id in the after-run is not in the baseline and not listed under "Added ids". Vitest's own exit status is
printed beside the result: it also goes non-zero when the per-test time guard names a test, which on a loaded machine is a
timing reading and is judged by the slice's measurement. To regenerate the baseline itself (only if the pre-slice commit
changes): `rm -f test-results.json`, note `started=$(date +%s)`, run the seven files from `Lighthouse.Frontend`, then
`python3 -I ../docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/baseline6561.py --started "$started"
--expected-tests 329 test-results.json "$PWD" <out-dir>`.

## Wave: DISTILL / [REF] The guarantee

**What the checks pin.** The id diff pins test **names**, and that every test passes. It does not pin assertions: an
assertion lost inside a test that keeps its name leaves the id list unchanged. That loss is caught by the removed-`expect(`
rows (`check-behaviour-free.sh`, below), and only there; the review and the deliberate breaks cover whether a moved
assertion still asserts the same thing.

**Stays green and unmodified (pinned by the id lists in `baseline/` and `baseline/outside-seven/`):** every id not named in
"changes by design" below, in all seven files; and, outside the seven, the files that share a helper this Story edits:
`ReportsPage.reading.test.tsx`, `ReportsPage.test.tsx`, `ReportsPage.usageData.test.tsx` and `BulkReportWizard.test.tsx`
(with `removal` and `combinedPdf`, the six importers of `rowOf`), `ModifyTeamSettings.refinement.test.tsx`,
`ModifyTeamSettings.queryCommit.test.tsx`, `OverviewDashboard.licenseCheck.test.tsx`, `useMetricsData.test.ts` (which only
gains the two hook tests). After each commit that is not a rewrite step (a pure move, the `rowOf` commit, the
`printFailures` extraction if taken) the unmodified tests pass with no test edit in that commit.

**Changes by design (the only ids that may leave their file or shrink; each needs a row):**

| File | Ids that may change | By how many (hypothesis, confirmed at the slice's step 1) |
|---|---|---|
| `refinementNeed` | stage rule display (4), cadence display and refusal (4), band display, marks, explanations and the two `it.each` families (15) move to a section render | 23 of 47 (of the 31 tests that render the whole form; 7 wiring tests and the refinement picker test stay on it, and 16 already render one unit or call a function) |
| `readiness` | defaults, refusals, `aria-invalid`, `min`/`step`, remembered-threshold toggles (all but 6) move to a section render | 18 of 24 |
| `ModifyTeamSettings.test` | three of the four "holds back autosave while a mapping ..." tests removed and replaced by one new form test that names every warning kind at once (backstops in "Slices"); the seven-row invalid-field loop shrinks to the rows the function tests do not cover | 3 removed + 1 added + up to 5 loop rows removed, of 35. **The "up to 5" is fixed at A-1**, where each row is checked against `teamAutoSaveBlockers.test.ts`, and this table is edited in that step's commit |
| `OverviewDashboard` | the 28 tests that read no row move, titles unchanged, to `OverviewDashboard.gating.test.tsx` and render the page with `DataOverviewTable` stubbed | 28 of 51 move; 0 removed |
| `BaseMetricsView.loading` | the 8 "adds and removes no chart once its data has arrived" rows merge into the matching "opens with every chart's frame" rows; 7 of the 8 "is asked for without retrying" rows become two hook tests; the team's Flow Metrics row stays as the seam row | 15 removed (118 left: 110 unmodified, 8 gain assertions), 2 added in `useMetricsData.test.ts`; C-1 may keep up to 3 more seam rows |
| `ReportsPage.removal` | the confirmation, warning, reason, result, Escape, in-flight and cache tests move to dialog and hook renders | 15 of 21 |
| `ReportsPage.combinedPdf` | **default (Q7 not yet a yes):** "Cancel closes the word that no PDF was made ..." moves to `NoPdfDialog` alone; the two "held back" tests merge into one new test (both old ids gone, one id added). **Only on a yes:** the three "Leave it out ..." rule tests also move to `printFailures.test.ts` | 3 of 18 change by default (1 moved, 2 merged into 1 added); 6 of 18 on a yes |

A change beyond these rows is allowed only when a slice's step 1 measures something different, and then the table above is
edited in the same commit with the reason.

**Loading and failure looks stay, every one, asserting what they did.** In `BaseMetricsView.loading` these are the 109
untouched ids plus the merged opening tests and the kept seam row: a dashboard as it opens, the dim under a spinner while
the window changes, answers out of order, one failed request marks only its chart and comes back on retry, the aging
chart's missing Doing columns. In `OverviewDashboard` they are "renders loading state initially" and the five "when the
connection list is refused" tests.

**No-new-behaviour checks:** no production file changes, except, only on a yes to Q7, `printFailures.ts` (new) and
`useReportsPdfExport.ts` (importing it); no Playwright spec or `@screenshot` file in the diff; production behaviour and
pixels are unchanged.

## Wave: DISTILL / [REF] Per-file: wiring tests that stay on the whole form or page

Named from DESIGN, titles as they are today. Each is the one proof of a seam that a unit render cannot give. **Total 211
ids stay in their file, unmodified** (208 on a yes to Q7): 24 + 6 + 27 + 23 + 110 + 6 + 15. Of these, 195 render the whole
form, page or dashboard (8 + 6 + 27 + 23 + 110 + 6 + 15) and 16 already render one unit or call a function. The counts for
`ModifyTeamSettings.test` and `BaseMetricsView.loading` are the unmodified remainder, because those files change by removal
or merge, not by moving the rest; the first is a floor until A-1 fixes its "up to" figure.

**`refinementNeed` (24: 8 on the form, 16 already unit or function tests):** the 7 wiring tests: saves a changed Ready rule
and leaves the Being refined rule as it was; saves nothing while a new rule is still incomplete; saves Thursdays every
week; asks for the starting week once Refinements are more than a week apart, and saves it; saves nothing while every
second week names no starting week, and marks the field; saves a changed high end with the low end as it was; refuses a
decimal likelihood instead of saving it rounded, and saves nothing. (One "saves nothing while invalid" per section kind,
because `teamAutoSaveBlockers` through `refinementSettingsBlockers` is only proven by a rendered form.) The eighth form
test is `the refinement picker offers a mapping's name, not its states`, which renders the whole form and is not in the
move list. The other 16 are the ids of the blocks `What holds back the save of the Refinement settings` (2 calls of
`refinementSettingsBlockers`), `The stage rules of a single Team` (7, render `StageRulesSettings` alone), `The Refinement
states when the To Do and Doing states change` (1, renders `RefinementSettingsSection` alone) and `The Refinement cadence
fields on their own` (6, render `RefinementCadenceSettings` alone); they are not in the move list and stay as they are.

**`readiness` (6):** saves a changed number of Yes votes with the rest of readiness as it was; starts a Team that has never
chosen refinement at the default readiness, and saves it with no states; keeps the chosen refinement states when readiness
changes; names readiness as what keeps the settings from being saved; saves nothing just because the form was opened; and one
of the "switches a rule back on ..." tests, `switches a rule back on at the threshold it had before`, as the single proof that
the toggle memory reaches the saved value.

**`ModifyTeamSettings.test` (27 or more, the form stays whole):** every test except three of the four mapping-name wirings
and up to five rows of the invalid-field loop; one mapping-name wiring stays (`holds back autosave while a mapping is named
after a Doing state it does not group, and saves once it is renamed`), and the two loop rows the function tests do not cover.

**`OverviewDashboard` (23, real tables):** the 4 "Row action gating via DataOverviewTable predicates" tests; the 12 deletion
tests ("what a deletion is reported as" 2 and "deleting a Team or Portfolio that has reports" 10, which open a real row's
Delete); `renders loading state initially`; `renders portfolios section after loading`; the 5 "when the connection list is
refused" tests. `OverviewDashboard.licenseCheck.test.tsx` is another file and is not touched.

**`BaseMetricsView.loading` (110 unmodified, and 8 that gain an assertion):** everything except the 24 rows of the three
`everyOwnerAndCategory` families (24 of 133), plus the kept seam row, `every chart request on the dashboard on a team's Flow
Metrics is asked for without retrying, and kept for no other window` (a correct hook with a widget that bypasses it would
pass the hook test). The 8 merged "opens with every chart's frame" rows are in the file and modified.

**`ReportsPage.removal` (6):** a coach removes three trial reports on two Teams and a Portfolio, and they leave the list; Cancel
removes nothing; when her permissions could not be loaded, pressing Remove asks nothing; a report the server did not remove is
named with its reason and stays listed; changing the filter clears what was picked; sorting keeps what was picked.

**`ReportsPage.combinedPdf` (15 on the default path, 12 on a yes to Q7, plus the merged test):** a coach exports the reports of
two Teams as one PDF, each on a page of its own; once the print dialog closes the frame goes ...; the reports print in the
table's order, as the reader sorted it; nothing prints while one of the reports is still on its way ...; picks and sorting
changed while the PDF is prepared leave it as it was ...; while the reports load, Export PDF shows it is preparing ...; a
reader who is Admin of none of them may still export them; a report that cannot be loaded stops the PDF and is named; Leave
it out unpicks the report that failed and prints the rest; Retry asks for the report again and prints once it arrives; when
part of the page cannot be loaded no PDF is made and it says so; with nothing picked Export PDF is disabled and says why
(12); and, on the default path only, the three "Leave it out ..." rule tests (unpicks every report that failed; is not
offered when it would leave nothing; leaves out only the report that failed, not the other report of the same Team). The
two "held back" tests (a row's PDF icon while a PDF of several is prepared; Export PDF while a row's PDF is prepared) are
merged into one new test and are not in this list.

## Wave: DISTILL / [REF] New direct tests, per slice, named as behaviours

Moved tests keep their titles; the rows of the moved-assertions file name them. The tests below are the ones that do not exist
today. All render the smallest unit that owns the behaviour, or call a function; none mounts the whole form or page, except
the two new form tests marked.

**Slice A**
- `renderRefinementSection.test.tsx` (the helper's own tests, so a wrong host cannot hide a wrong section):
  "the section shows the value the Team was stored with until the admin edits it", "a change the admin makes shows in the
  section as its new value", "a second edit builds on the first edit's result, as in the form" and "a test can read the value the
  admin has now" (the last is named for what a test author relies on, not for how the host records it). A fifth test, for
  `changes()`, is written only if a moved test needs that function.
- The moved Refinement tests land in `RefinementBandSettings.test.tsx`, `RefinementCadenceSettings.test.tsx` (existing last
  block of `refinementNeed` may stay or move), `StageRulesSettings.test.tsx`, `ReadinessSettings.test.tsx` (names chosen at A-2
  after the existing files are checked, so no duplicate file is made).
- Form test (whole form, `ModifyTeamSettings.test.tsx`): "says what holds back the save for a mapping of every kind of
  mistake at once" (A-4; replaces three removed wiring tests).
- If A-1 shows a mapping-warning sentence or invalid-field scenario that no function test pins, the function test is added
  before the wiring test is removed ("names the mapping that hides a Doing state" style, one per unpinned sentence). The list
  of such backstops is finished at A-1; a wiring test is not removed until its backstop is green.

**Slice B**
- `BulkReportsTestKit` self-test, only if the kit has none: "finds the row of a report on its owner", "tells two reports of the
  same name on different owners apart", "fails naming the report and the owner when there is no such row".
- `NoPdfDialog.test.tsx`: "offers Cancel, Leave it out and Retry when leaving out is possible", "offers Cancel and Retry when
  it would leave nothing", "each button reports its own choice once". Moved: "Cancel closes the word that no PDF was made".
- Merged form test (whole page, `ReportsPage.combinedPdf.test.tsx`): one test for the two "held back" behaviours, titled at B-4
  from the two old titles.
- **Only on a yes to Q7:** `printFailures.test.ts`: "names the listed reports a failure belongs to, by id and owner",
  "does not name another report of the same Team", "names nothing for an empty failure list", "offers leaving out when something
  failed and something would be left", "does not offer it when every report failed", "leaves out exactly the failed ones and no
  other". Without the yes this file does not exist.
- `RemoveSelectedDialog.test.tsx` and `useSelectionRemoval.test.ts`: the moved removal tests; new only where the page test
  proved a reason no unit test can reach (decided at B-3, stated in the rows; the list is finished at B-1).

**Slice C**
- `OverviewDashboard.gating.test.tsx`: the 28 moved tests, titles unchanged; no new test. A test that passes against the stub
  and fails against the real table (or the reverse) is a finding, written in the slice file.
- `useMetricsData.test.ts`: "a team's dashboard asks for every chart's data without retrying and keeps none of it for another
  window", "a portfolio's dashboard asks ... the same" (app default query client, every key of `allMetricsFetchKeys`).
- `BaseMetricsView.loading`: the 8 merged rows keep the title of the "opens with every chart's frame in its place, each with a
  spinner, before any data arrives" rows and also assert that no chart is added or removed once the data has arrived. The kept
  seam row of the team's Flow Metrics, and any seam row C-1 adds, stay unmodified.

## Wave: DISTILL / [REF] Shared helper contracts (written by DELIVER in the step that first needs them)

**`renderRefinementSection` (slice A, step A-2, in `src/tests/`, test-only).** Renders one controlled unit of the Refinement section, the section itself or one of its parts
(`ReadinessSettings`, `RefinementBandSettings`, `RefinementCadenceSettings`, `StageRulesSettings`, `RefinementSettingsSection`)
inside the providers the form's tests give it (the Team's terminology, the seeded default words unless the test passes its own)
and a host that keeps the value in `useState`.
- Input: the initial value and a function from `(value, onChange)` to the section element.
- Returns the `user` from `userEvent.setup()` and `latest()` (the value the host holds); the usual RTL queries are used from
  `screen`. There is no `changes()`: no moved test reads the sequence of changes, only the value the host holds now. A-2 adds
  it, with its own helper test in the same commit, only if a moved test turns out to need it.
- `onChange` of `ReadinessSettings`, `RefinementBandSettings`, `RefinementCadenceSettings` and `StageRulesSettings` takes the
  complete next value and the host replaces its value with it; `RefinementSettingsSection` hands only the part that changed,
  and the host spreads it over its value, as `changeRefinement` does in the form. The state updates synchronously; no
  query client, no fake clock, no debounce, no service. It never calls anything on its own, so a section that needs a service
  is a wiring test and stays on the form.
- It does not hide an error: a throwing section fails the test; no `try`, no `act` advance of timers.
- One plain comment on why the value lives in state (a controlled section shows nothing new unless handed its change back).
- Universe of the helper: `src/tests/renderRefinementSection.tsx` (new), its own test file `renderRefinementSection.test.tsx` (new), the importers `ModifyTeamSettings.refinementNeed.test.tsx` and `ModifyTeamSettings.readiness.test.tsx`, and the section test files slice A creates (`RefinementBandSettings.test.tsx`, `StageRulesSettings.test.tsx`, `ReadinessSettings.test.tsx`, a `.tsx` beside `RefinementCadenceSettings.test.ts` if needed; names confirmed at A-2). Nothing else imports it.
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

**`printFailures.ts` (slice B, step B-4, only on the maintainer's yes to Q7; not created by default).** Pure, return-only. `failedAmong(listed, failed)`
returns the listed reports a failure names, matched by id **and** owner; `leaveOutPlanOf(listed, failed)` returns either "not
offered" (nothing failed, or leaving the failed out would leave nothing) or the reports that remain. No hook, no React import.

## Wave: DISTILL / [REF] How "no `.skip`, `.only`, `.todo`, looser matcher or longer timeout" is checked

1. **Biome** fails `pnpm build` on `noSkippedTests` and `noFocusedTests` (FE7 slice 1a). Tested in the revision pass with a
   probe file: Biome 2.6 flags `it.skip` and nothing else of these. **`it.skipIf(...)`, `it.runIf(...)`, `test.skipIf(...)`
   and `it.todo(...)` pass Biome untouched**, so Biome alone does not cover them; the script below does.
2. **`baseline/check-behaviour-free.sh <before-commit> <moved-assertions-file> [--allow-extraction]`**, run from the
   repository root at every slice close and before review. It reads the diff of test files and `src/tests` since
   `<before-commit>`, taken with `--no-renames` (a moved file is a deletion and an addition, so its `expect(` lines need
   rows) and reads untracked new test files as all-added lines. It **exits 1** when:
   - an `expect(` was removed and the moved-assertions file has no row for it, or the rows of a file account for more
     `expect(` than were removed. The counter is one regular expression, `\bexpect\(`, applied to removed lines and to the
     "Assertion text" cell of every row; per file the two totals must be equal. The counter does not see `expect.soft(`,
     `expect.poll(` or `assert.`; none of them occurs in the three slice A files today (searched, no hits), and a file that
     uses one needs the counter extended before it is edited;
   - a row's assertion text, stripped of surrounding whitespace, is not one of the removed lines of its file, or a removed
     line is cited by more rows than it was removed (the phantom row that would otherwise hide an unrecorded removal);
   - more `.not.` assertions were removed than added, beyond those the rows quote;
   - an added line matches the weaker-matcher list: `.skip`, `.only`, `.todo`, `skipIf`, `runIf`, `it.fails`, `xit`,
     `xdescribe`, `toMatchObject`, `objectContaining`, `toBeGreaterThan`, `toBeLessThan`, `toContain(`, `toMatch(`,
     `toBeTruthy`, `toBeDefined`, a `{ timeout: ... }` option, `testTimeout`, `setConfig`, a 4-digit number as a last
     argument; or `toHaveBeenCalled()` was added while `toHaveBeenCalledWith(` fell (a weaker matcher in place of a stronger
     one). A hit that is not a weakening (a `toContain(` on a string that was already one) is pasted under "Accepted matcher
     hits" in the moved-assertions file, with a reason, and then passes;
   - a production file changed (none are allowed; `printFailures.ts` and `useReportsPdfExport.ts` only with
     `--allow-extraction`, which is a yes to Q7), or an E2E or screenshot file changed.
   **That matcher list is partial.** There are always more ways to say less than a grep list holds; the removed-`expect(`
   row check is the control that does not depend on knowing the way. Proved in the revision pass against planted changes
   (see the record at the end of this section).
3. **Rows** (FE7 rule 1) are checked by the script above, per file, from `measurement/slice-X-moved-assertions.md` (built
   from `measurement/moved-assertions-template.md`). The "Assertion text" column holds the removed `expect(` line verbatim.
   The `expect(` totals in `baseline/` are the sum to explain.
4. **Id union diff:** `baseline/run-touched-files.sh <before> <moved-assertions-file>` runs every baseline file, every changed
   test file and every new test file, deletes the old report first, refuses a stale report or a count that differs from the
   run's, and fails on a missing id without a row, a new id not under "Added ids", a touched file that was not in the run,
   a test that did not pass, a row naming an id that is not in the baseline, or a row naming an id that is still in the
   after-run unless its last cell starts with `kept:` (an accepted note, defined in the template). It pins names, not
   assertions (see "The guarantee").
5. **Deliberate break** for each new call site and each surviving wiring test: break the production seam it claims to prove,
   see the named test fail, restore, record both in the slice file. The list:
   - drop the value handed from the form to the section; pass the stub the wrong prop;
   - **make `refinementSettingsBlockers` return `[]`, and make `teamAutoSaveBlockers` return `[]`**, and see, one per section
     kind, the form's "saves nothing" tests fail: stage rule, `saves nothing while a new rule is still incomplete`; cadence,
     `saves nothing while every second week names no starting week, and marks the field`; band, `refuses a decimal
     likelihood instead of saving it rounded, and saves nothing`; readiness, `names readiness as what keeps the settings
     from being saved`; and, for `teamAutoSaveBlockers`, the kept rows of `does not auto-save when X is invalid`;
   - **bypass `metricsQueryOptions` in a widget the kept seam row covers** (remove the spread in `useFilteredView.ts`, in
     `useCumulativeStateTimeChoices.ts` and in the drill-down query of `BaseMetricsView.tsx`, one at a time) and name the
     test that fails. If none fails, that is a finding: the seam is untested today and a seam row must cover it before the
     family rows go;
   - change `retry: false` or `gcTime: 0` in `metricsQueryOptions` and see the hook tests fail.

   **Slice A rows, one per new call site and per named surviving seam.** Paths are under
   `Lighthouse.Frontend/src/components/Common/Team/` unless given. Each break is made one at a time, run at DELIVER (not
   now), the named test is seen to fail, the break is restored, and both are recorded in the slice file. A row where no test
   fails is a finding: the seam is untested today, and a test is added before the rewrite commit.

   | Site or seam | Production seam to break | Test expected to fail |
   |---|---|---|
   | `renderRefinementSection` host (new call site of every moved test) | none in production: make the host keep its first value and ignore `onChange` | the helper's own `a change the admin makes shows in the section as its new value` and `a second edit builds on the first edit's result, as in the form` |
   | A-4 form test `says what holds back the save for a mapping of every kind of mistake at once` | `teamAutoSaveBlockers.ts`: delete `...mappingAutoSaveBlockers(s.stateMappings, s, stored)` | that test, and the kept `holds back autosave while a mapping is named after a Doing state it does not group, and saves once it is renamed` |
   | the same, the form listing every reason | `ModifyTeamSettings.tsx`: render `formInvalidReasons.slice(0, 1).map(...)` instead of `formInvalidReasons.map(...)` | that test only (it names four kinds in one render); the kept Doing test still passes |
   | the same, each kind fed through the form | `src/utils/stateMappingValidation.ts`, `stateNameClashErrors`: remove the `listClashError` call for To Do, then for Done, then `groupedElsewhereError`, one at a time | that test each time, and the function test for that kind (`names the To Do list when the shadowing sits there`, `names the Done list when the shadowing sits there`, `refuses a mapping that groups another mapping's name, with the sentence the server sends`) |
   | readiness toggle wiring: the kept `switches a rule back on at the threshold it had before` | `ReadinessSettings.tsx`: `toggleRule` stops calling `setRememberedThresholds(rememberOnSwitchOff(...))` | that form test, and the moved `switches a rule back on at the threshold it was changed to before it was switched off` and `switches a rule that was refused when switched off back on at its last allowed threshold` in `ReadinessSettings.test.tsx` |
   | readiness handed from the form to the section: the kept `saves a changed number of Yes votes with the rest of readiness as it was` | `RefinementSettingsSection.tsx`: `onChange={(readiness) => onChange({ readiness })}` becomes `onChange={() => onChange({})}` | that test and the kept toggle test; the tests of `ReadinessSettings.test.tsx` stay green, because they own only the section |
   | band, stage rules and cadence handed up, one each | `RefinementSettingsSection.tsx`: `onChange({ band })`, then `onChange({ stageRules })`, then `onChange({ cadence })` becomes `onChange({})` | `saves a changed high end with the low end as it was`; `saves a changed Ready rule and leaves the Being refined rule as it was`; `saves Thursdays every week` |
   | the form keeps what a section did not change | `ModifyTeamSettings.tsx`, `changeRefinement`: replace its body with `updateSettings("refinement", { states: [], ...change })` | `keeps the chosen refinement states when readiness changes` (it asserts the whole saved refinement value) |
   | refinement picker: the kept `the refinement picker offers a mapping's name, not its states` | `ModifyTeamSettings.tsx`: hand `stateMappings={[]}` to `RefinementSettingsSection` | that test |
   | section marks and sentences moved to `ReadinessSettings.test.tsx`, `RefinementBandSettings.test.tsx`, `RefinementCadenceSettings` and `StageRulesSettings.test.tsx` | `ReadinessSettings.tsx`: `error={errors.minYes !== null}` becomes `error={false}`; `RefinementBandSettings.tsx`: `error={errors[end] !== null \|\| inverted}` becomes `error={false}`; `RefinementCadenceSettings.tsx`: `MOST_WEEKS_APART` 52 becomes 520; `StageRulesSettings.tsx`: `onChange({ ...stageRules, [stage]: rule })` loses its spread | `marks Yes votes needed as refused and leaves Voters needed unmarked`; `marks only the low end when it is out of range above the high end, since that is not an inverted band`; `saves nothing for more than 52 weeks between Refinements, and says why`; `saves a changed Ready rule and leaves the Being refined rule as it was` |
   | `ModifyTeamSettings.test` survivor "held back": the kept Doing wiring | `src/hooks/useModifySettings.ts`: `formValid` becomes `settings !== null`, ignoring `formInvalidReasons` | that test, and the kept rows of `does not auto-save when X is invalid` |
   | the same survivor, "released on rename" | `src/hooks/useModifySettings.ts`: delete `dispatchSave(settingsToSave)` inside the debounce timer | the "saves once it is renamed" half of that test |
   | `ModifyTeamSettings.test` survivor "auto-save after the debounce" | the same deletion | `auto-saves a valid change after the debounce window` |
6. **Coverage** (opt-in for exactly this reason): per slice, `--coverage` over the named files of the touched directory only,
   line and branch not lower than the slice's first-step reading (FE7 left `src/components/Common/Team` at 96.26 / 86.23).

**Proof for items 2 and 4, run in the revision pass (2026-11-06, planted changes, all undone).** Item 2, against `HEAD`:
no change exits 0; deleting one `expect(` line of `ownerLimitOfWindow.test.ts` with no row exits 1 ("removed 1 rows 0,
MISSING 1 row(s)") and with a row exits 0; `git mv` of that file with no rows exits 1 (15 rows missing); deleting a
`.not.` line with a row that does not quote it exits 1, with a row that does, 0; changing `toHaveBeenCalledWith(...)` to
`toHaveBeenCalled()` exits 1 even with a row; a new untracked test file holding `it.skip`, `toBeTruthy()` and `it.skipIf(...)`
exits 1 with all three printed. Item 4: renaming one test and adding a new untracked test file, with no rows, ended
`FAIL` naming the missing id and both new ids (544 baseline ids, 545 after, 16 files in the run); with the row and the two
"Added ids" it ended `OK`; a second untracked file left out of the same report was named as "not in the after-run"; a
report started before the run and a count that differs from the run's were refused with exit 2.

**Proof for the second revision pass (2026-11-06, planted changes, all undone with `git checkout --`).** Test file:
`ModifyTeamSettings.readiness.test.tsx`; two real lines were deleted in turn (`expect(screen.getByText("Send to discussion
when")).toBeVisible();` and `expect(screen.getByText("or more No votes")).toBeVisible();`) and a moved-assertions file was
written for each case. The old script is the version at `d148c2d14`. For `check_ids.py` a report was built from the baseline
id lists (every test passed), so no suite was run.

| Case | `check_behaviour_free.py` | Old script |
|---|---|---|
| nothing changed, no rows | exit 0 | exit 0 |
| two removals, one real row, one phantom row (text of a line that was never there), one removal unrecorded | exit 1, "not a removed line of this file" | exit 0 (counts equal, hidden) |
| one removal, its row | exit 0 | exit 0 |
| one removal, its row cited twice | exit 1, "SURPLUS 1 occurrence(s)" and "cited by more rows than it was removed" | exit 0 |
| one removal, a row naming another line | exit 1, "not a removed line of this file" | exit 0 |
| two removals, both rows | exit 0 | exit 0 |
| two removals, one row | exit 1, "MISSING 1 row(s)" | exit 1 |
| one removal, row text with spaces around it | exit 0 | exit 0 |

| Case | `check_ids.py` |
|---|---|
| nothing changed, no rows | exit 0 |
| a row names an id that is still present | exit 1, "still present and is not marked kept" |
| the same row with `kept:` in its last cell | exit 0 |
| a row names an id that is not in the baseline | exit 1, "not in the baseline" |

The first run of these cases found a defect (the table's header row was read as a row); it was fixed and all cases were
rerun with the results above. `git status` afterwards showed only the intended edits.

## Wave: DISTILL / [REF] Stop rule

Inherited from D-6561-10 and made checkable by D-6561-20. **The stop check for a file is the step that puts its moves in,
measured by one method:** summed per-test seconds of that file (`assertionResults[].duration` of its suite in
`Lighthouse.Frontend/test-results.json`), 5 alternating before and after pairs on `LIGHTHOUSE_TEST_WORKERS=2`, a pair valid
only if all four samples (the 1-minute load average at the start and end of both its runs) are at or under half the cores,
user+sys CPU seconds recorded beside it. Cut = 1 - after/before. The slice file states the projected cut next to two figures
of DESIGN's table: the **required cut**, which stops the file when the projection is less than it (34 %, 24 %, 20 %, 19 %,
17 %, 13 %, 1 % for the seven), and the **design target** with margin, which is reported (38 %, 28 %, 25 %, 24 %, 21 %, 18 %,
6 %). A file between the two passes and is recorded as passing with no margin.

| Slice | File | Stop-check step |
|---|---|---|
| A | `readiness` / `refinementNeed` / `ModifyTeamSettings.test` | A-2 / A-3 / A-4 |
| B | `removal` / `combinedPdf` | B-3 / B-4 |
| C | `OverviewDashboard` / `BaseMetricsView.loading` | C-2 / C-3 |

- **Projected cut below the required cut:** stop that file, record the number and the reason ("target not met, because ...",
  which the AC allows), flag that the budget floor follows that file (Q3), and do not split the file (Q4 stays a maintainer
  decision). A stopped file keeps the moves already committed, which are behaviour-free and not slower, gets no further work
  in this run and is recorded; the harness commit stays and the other files of the slice carry on.
- **Fewer than 3 valid pairs of 5:** the slice records "unmeasured, machine loaded" for that file and takes the stop, the same
  as a miss. There is no retake loop: the maintainer decides whether to measure again on a quiet machine.
- **`ModifyTeamSettings.test` (required cut 1 %):** five pairs cannot resolve 1 %, so its check is that the rows removed
  match the change table and the summed seconds are not higher than before; anything finer is recorded as not resolvable.
- **Other stops:** A-1's gate measures the ratio r of one band test, form against section; below 1.85, the figure the
  arithmetic needs for the required cut of `refinementNeed` (see "Slices"), nothing moves and the run goes to the hold. B-1 shows
  the export tests spend their time drawing reports (skip B-2 for `combinedPdf`; if the cut is then under 17 %, record it);
  C-1's Overview spike saves under 20 % (ask the maintainer about the extraction, Q6, never default to it). A conflict with
  trunk (T12 #6539) stops the slice and reports; nothing is rebased.

## Wave: DISTILL / [REF] No scaffolds, no skipped tests, and why

There is no new production behaviour, so no module is imported that does not exist, and a scaffold would only duplicate a test
that already passes. Skipped tests are not written either: the project forbids `.skip` in committed tests (Biome), and the 329
existing tests are the executable specification; they are green now (329 of 329) and that is the correct starting state.
The "red" of this Story is the deliberate break of check 5 above, made per call site by DELIVER. The one optional production
file (`printFailures.ts`, only on a yes to Q7) is created in its own `refactor(reports)` commit with no test change, not as a scaffold. Tier B,
the domain-types module and the state-delta port: N/A, because the project has no Gherkin, state machine or generator at this
layer. Self-completeness audit: C2 and C5 to C7 N/A (no state machine, mode flag, error contract or concurrency matrix is added).

## Wave: DISTILL / [REF] Pre-requisites

- **Done in this wave:** the worktree's `node_modules` (`pnpm install --frozen-lockfile`), the baseline run and files
  (seven files and the eight outside them), the check scripts, the moved-assertions template.
- **Before slice A:** nothing external. **Before slice B:** nothing is needed to start; the `useReportsPdfExport` extraction
  (Q7) is **not taken until the maintainer says yes**, so B-4 runs its default path (no production file) and the
  `printFailures` tests are not written. A yes adds the extraction to B-4. **Before slice C:** the ADO state of FE5 #6393
  (Q1); if it landed, C changes and the slice is redesigned with the maintainer.
- **At every slice start:** re-read the slice's files against trunk for T12 #6539 (Q2); agree the rigor profile (proposed
  `standard`, D-6561-16) and pass `model` on every dispatch, stating again that mutation testing is N/A while no production
  file changes; record the load average at the start and end of every run, run the alternating pairs with
  `LIGHTHOUSE_TEST_WORKERS=2` on named files, count a pair only when all four of its samples (1-minute load average, start
  and end of both runs) are at or under half the cores, record
  "unmeasured, machine loaded" below 3 valid pairs of 5, and never run the full suite or Stryker while another session is
  mutating. `/continuous-improvement before` does not apply (it is for feature Epics; #6506 is an improvement Epic).
- **After the slices:** the budget step (DESIGN, "Budget step"), once at least 5 complete plain `main` runs exist (a slow-runner
  run among them, 950 s or more, is optional). The first push is judged against 56 s and the Story stays open until that commit lands.

## Wave: DESIGN / [REF] Scope of this run (maintainer, 2026-11-06)

After seeing the expected budget band, the maintainer asked for **slice A alone**, to reassess on 2026-11-07. Slices B and
C are not dropped; they do not run now. The extraction question (Q7) and the Overview fallback (Q6) wait with them, so no
production code changes in this run.

- **Budget.** Slice A alone, if all three files reach their cuts, leaves `OverviewDashboard` as the worst file: L = 22.0, so
  B = ceil(2.1 x 22.0) = 47. The budget stays 56 s until the budget step, which needs at least 5 complete plain `main` runs
  after a push (see "Budget step"), and nothing is pushed in this run. If a file of slice A stops, the budget follows that
  file (Q3).
- **Mutation testing is N/A for slice A**, because no production file changes. This is an exception to the project's
  per-feature mutation strategy, and the maintainer sees it at the hold.
- **Before-commit.** `d148c2d14` is slice A's before-commit; the check that no test file changed since the baseline is
  recorded under "Baseline".
