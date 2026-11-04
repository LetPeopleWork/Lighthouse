# Mutation testing — 6457 (fast frontend tests)

Run 2026-11-04 against `main` @ `ff9992298` (the first run was at `e85f2ebc5`). Gate is 80 % kill rate on each stack
touched. Backend: N/A, because this Story changes no backend file. Frontend only.

| run | score | tested | killed | timeout | survived | no coverage | error |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First, `e85f2ebc5`, direct tests only | **95.27 %** | 804 | 764 | 1 | 33 | 5 | 1 |
| Final, `ff9992298`, full re-run (`--force`) | **97.76 %** | 804 | 784 | 1 | 18 | 0 | 1 |

The score is (killed + timeout) / (killed + timeout + survived + no coverage); the one `error` mutant (a mutated
module constant that fails to load) is left out of the score by Stryker.

Configs: `stryker.6457.frontend.json`, `vitest.stryker.mutation.ts` (same folder). StrykerJS, concurrency 2 next to
another session, per-test timeout 30 s, dry-run limit raised to 30 min, `TZ=Europe/Zurich`, run in the separate
worktree `Lighthouse-fe7-arms/candidate` so the in-place mutants never touched the main checkout. Wall clock of
the final full run: 7 min 38 s.

## What the run covers

- **Mutate list checked before the run.** The 15 files below, every production file this Story moved logic out of
  into plain functions, plus the three time-budget guard files. Stryker reported 804 mutants, which is the size
  these 15 files should give; the project-wide count did not leak in.
- **Runner include checked before the run.** `vitest.stryker.mutation.ts` includes only the direct function tests of
  those folders and `src/tests/timeBudget/*.test.ts`. Page and component tests are left out (see "Not mutated").
- **The time-budget guard is off during the run.** The guard turns itself off when `STRYKER_MUTATOR_WORKER` is set,
  which StrykerJS sets in its workers; the test "is off under mutation testing, whatever the switch says" in
  `timeBudgetOptions.test.ts` pins it.
- **Earlier than the first complete run, three attempts failed before any mutant:** the 5-minute dry-run limit, a
  5 s timeout of the restored readiness page test under instrumentation, and a crash inside the Stryker runner
  (`TypeError: Cannot convert object to primitive value` in `errorToString`) after about twenty minutes with the
  page tests in. The cause of the crash is unknown; leaving the page tests out avoided it.
- **Kill rate before the Story:** not measured. These functions were inlined in components and hooks before the
  Story, tested only through page tests, and no earlier Stryker run exists to compare with. What can be said is that
  the extracted logic now scores 97.76 % by direct tests alone, so the page tests only add to it. New code is above
  80 % in every file.

## Per file

| file | first run | final | survivors left |
| --- | --- | --- | --- |
| `components/Common/Connection/defaultsKeepingOnlyFields.ts` | 90.00 % | 100.00 % | 0 |
| `components/Common/Connection/whatASaveAsks.ts` | 94.00 % | 98.00 % | 3 |
| `components/Common/ProjectSettings/portfolioAutoSaveBlockers.ts` | 100.00 % | 100.00 % | 0 |
| `components/Common/Team/discussionRules.ts` | 93.75 % | 93.75 % | 1 |
| `components/Common/Team/teamAutoSaveBlockers.ts` | 98.59 % | 100.00 % | 0 |
| `pages/Common/Reports/wizard/reportWizardRules.ts` | 97.37 % | 100.00 % | 0 |
| `pages/Reports/bulkWizardSteps.ts` | 89.66 % | 89.66 % | 6 (+1 error) |
| `pages/Reports/ownerLimitOfWindow.ts` | 80.00 % | 87.27 % | 7 |
| `pages/Reports/pickLimit.ts` | 100.00 % | 100.00 % | 0 |
| `pages/Reports/pickedReports.ts` | 100.00 % | 100.00 % | 0 |
| `pages/Teams/Detail/Refinement/refinementHeading.ts` | 100.00 % | 100.00 % | 0 |
| `pages/Teams/Detail/Refinement/refinementPaging.ts` | 95.24 % | 95.24 % | 1 |
| `tests/timeBudget/compareToAllowList.ts` | 97.14 % | 100.00 % | 0 |
| `tests/timeBudget/observedRunFromVitest.ts` | 98.04 % | 100.00 % | 0 |
| `tests/timeBudget/timeBudgetOptions.ts` | 96.67 % | 100.00 % | 0 |

## Closed by this pass

Each of these gets a direct test in the existing test file. Before committing, the mutation was applied by hand to
the production line and the test file run: every one of them failed, and the production files were restored.

- **`ownerLimitOfWindow.ts` 46 (sort removed, comparator returning nothing, `-` turned into `+`):** when both windows
  reach back past the owner's kept history, the one that starts furthest back is named, even when Then is listed
  first. Pinned with Now reaching back to 5 Jul 2026 and Then to 18 Sep 2026.
- **`ownerLimitOfWindow.ts` 25 (Then's end day is no calendar day):** an owner is told no limit while the day Then
  ends is not a date, instead of being blamed for Now alone.
- **`whatASaveAsks.ts` 143 (the filter to Estimation defaults removed):** a followed default that is no Estimation
  default and was cleared is not listed among the changed Estimation defaults.
- **`whatASaveAsks.ts` 55, 84, 208:** the Estimation Field label of both Estimation defaults is named once, and the
  defaults list nothing they change.
- **`defaultsKeepingOnlyFields.ts` 23:** an Estimation default whose bundle names no field is kept as it is.
- **`teamAutoSaveBlockers.ts` 64:** settings with no query at all (not an empty one) still name "Enter a JQL Query".
- **`observedRunFromVitest.ts` 84:** a suite without a title counts as an empty title in the full test name.
- **`reportWizardRules.ts` 12:** the stale-save sentence itself is pinned; the existing test compared the constant
  with itself.
- **`compareToAllowList.ts` 90:** an allow-list entry is stale when only another file has a test of that name.
- **`compareToAllowList.ts` 130:** a skipped test is not judged against the budget, however long it took.
- **`timeBudgetOptions.ts` 47:** `--watch` next to a `run` command still turns the guard off.

## Accepted survivors

All 18 are equivalent: no input can make the mutated program behave differently from the original.

- **`whatASaveAsks.ts` 21 (`join("\n")` becomes `join("")`):** the strings joined are `JSON.stringify` outputs, each
  a self-delimiting array, so two different field lists cannot join to the same text with or without a separator.
- **`whatASaveAsks.ts` 54, 83 (`readWhen: "pageLoads"` becomes `""`):** the only reader is `readWhen === "updating"`
  (line 110), which is false for `""` as it is for `"pageLoads"`.
- **`discussionRules.ts` 30 (`current !== null` always true):** `isUsableThreshold(null)` is already false, because
  `null` is not a whole number, so the guard repeats what the second operand decides.
- **`refinementPaging.ts` 36 (`high !== undefined` always true):** `isNumbered(undefined, index)` is
  `index < undefined`, which is false, so the result is `null` either way.
- **`bulkWizardSteps.ts` 34 (six mutants of `activeStep > TYPE_STEP && activeStep < OWNER_STEP`):** the condition only
  guards an array read, `type.steps[activeStep - 1]`. For every step outside the range (`-1`, `0`, the owner step,
  beyond it) that index is out of bounds and reads `undefined`, which is what the other branch returns. Every
  bound mutation, and the condition being always true, therefore give the same result. The existing tests already
  pin the Type step, the owner step and the three steps between.
- **`bulkWizardSteps.ts` 16 (error, not a survivor):** `chosen = ""` makes the module fail to load; it counts as
  neither killed nor survived.
- **`ownerLimitOfWindow.ts` 24 and 25 (`earliestStart === null` and `keptSince === null` forced false):**
  `parseLocalDate(null)` is `null` anyway, and an owner that keeps no history, or an unreadable one, cannot fail
  `ownerCannotHold`, so the list of too-long windows stays empty and the result is `null` either way.
- **`ownerLimitOfWindow.ts` 41, 42 (`nowLength !== null`, `today !== null`, their conjunction and `||`):** without a
  Now length the window check is called with `null`, which counts as shorter than the minimum and refuses for
  "too short", not for the kept history, so `ownerCannotHold` is false; without a readable today the day counts
  are `NaN` and every comparison is false. The guards are there for the types.
- **`ownerLimitOfWindow.ts` 43 (`side: "now"` becomes `""`):** the window check tests `side === "then"` and treats
  every other value as Now.

## Not mutated

- **Page and component tests are not in the runner.** With them the initial run took twenty minutes and then
  crashed the Stryker runner (`TypeError: Cannot convert object to primitive value` in `errorToString`; cause
  unknown). So a mutant that only a page test catches would show as a survivor; none of the 18 above is of that
  kind, each being equivalent.
- **Only the logic this Story moved out is mutated.** The components, hooks and wizards that call these functions
  are not in the `mutate` list: the Story moved lines, it did not change what they do.
- **Incremental runs go stale for module-level mutants.** The incremental re-run (`--incremental`, 91 mutants,
  2 min 21 s, 97.38 %) kept the old result for five mutants that run when the module loads (the four label
  arrows in `whatASaveAsks.ts` and the `STALE_SAVE` string), though the new tests kill them. A forced run of
  `whatASaveAsks.ts` alone, then the full forced run above, show the true result. Use `--force` after adding a
  test aimed at a module-level constant.
