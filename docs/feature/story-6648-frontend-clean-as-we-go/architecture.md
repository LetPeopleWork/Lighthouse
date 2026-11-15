# Architecture brief: story\-6648\-frontend\-clean\-as\-we\-go

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Formal verification: no Quint, TLA+ or Agda model applies. The change is a relocation of existing test rules with no state machine, no concurrency and no production behaviour change, so there is no transition system or theorem to check. The one law (every removed or merged page test keeps a test that fails when its production line is broken) is finite and is checked by executable scripts (id diff, removed expect( rows, deliberate-break runs), not by a model. No checker was probed or run; the claim is not stated as proven. If the maintainer wants a model anyway, that is a separate ask.
```

#### D2

```text
Paradigm is object_oriented (project authority). Rules move onto the unit that owns them: a rule of one dialog onto that dialog rendered alone with its props, a rule of one hook onto renderHook, a pure rule onto the function. The page keeps one render per wiring. No class or production file is introduced by default.
```

#### D3

```text
Oracle and RED. These slices change no behaviour, so RED is not a missing behaviour. Each slice has a public oracle chain: stimulus = run its measure script from the repository root; expected = the named test files all pass, the slice file-alone summed per-test seconds fall to at most the slice target ratio of the before-commit file, and the slice files plus the new direct tests together fall to at most the slice combined ratio; falsifier = a before-commit file that is unchanged (ratio 1.0) must make the script exit non-zero. RED is observed on today code: the page files are unshrunk and the new direct tests do not exist, so the ratio is 1.0 and the script reports assertions_failed=1 for the time assertion with observed equal to the tests run. GREEN is the same script after the rewrite.
```

#### D4

```text
How a local time reading is judged. The measure script alternates 5 pairs: the before side is an untracked copy of the before-commit test file next to the real one (deleted by the script on exit), the after side is the rewritten file. A pair is valid only if all four 1-minute load-average samples (start and end of both runs) are at or under half the cores and every test passed. Fewer than 3 valid pairs prints unmeasured, machine loaded, exits non-zero, and is never retried in a loop. The ratio is the median over valid pairs of after/before summed per-test seconds (assertionResults duration in ms from the JSON report; the script deletes the report first and refuses a stale one). The ratio, not the seconds, is judged, so machine speed cancels. CI judgement (median of at least 4 main runs of test-timings-frontend CSVs, as the baseline) happens after the push and is not a slice gate.
```

#### D6

```text
What Story 6561 slice B still holds, verified against today code: RemoveSelectedDialog(reports,onCancel,onDone), useSelectionRemoval(picks,rbac), useBulkReportRemoval, NoPdfDialog(failedReports,onLeaveItOut,onRetry,onCancel), the private failedAmong and leaveOutTheFailedOf in useReportsPdfExport.ts, removeBlockedBecause already function-tested in pickLimit.test.ts; the per-file test lists, the wiring-stays lists, the guarantee scripts. What changed: (1) the cheaper rowOf is dropped: a spike of render plus 3 picks showed the same time with the current rowOf and a cheap lookup within noise, the cost is the page mount and userEvent clicks, so the lever is fewer page renders and fewer picks, and the kit is now imported by 15 files not 6, which makes a kit change a larger blast for no gain; (2) combinedPdf default path is stronger than slice B said: the three Leave it out rule tests merge into one 4-pick page test and Cancel merges into the failure-named test, not only two merges; (3) the T13 budget arithmetic (required cut to a 17.5 s line, B=37) is gone: P1 here is each file less per-test time by CI median, and the budget is 54 s already; (4) slice A measurement scripts are reused, copied into this workspace because they hard-code the T13 file list.
```

#### D8

```text
Order inside every slice, backstop first: (1) acceptance designer adds the new direct tests, which pass at once against unchanged production (characterisation, GREEN on creation); (2) the measure script is observed RED; (3) a pure move commit if a test only changes file; (4) the crafter then removes or merges the page tests the direct tests replace, one rule family per commit, each with its moved-assertions rows; (5) the measure, check and break scripts are run GREEN; (6) pnpm test --changed and the named files in the loop, pnpm test and pnpm build once at the end. A page test is never removed before its backstop exists and passes.
```

#### D9

```text
Loading and failure looks stay pinned: S1 keeps the permissions-could-not-load tooltip and the disabled Remove on the page, and the under-way dialog (progressbar, cannot close), cannot-confirm and not-removed result move to the dialog alone with the same assertions; S2 keeps the preparing look and the part-of-the-page-failed dialog on the page, plus the dialog alone; S3 keeps one log-cannot-be-read row on the page (the renamed-terms row) and the reading state of the dialog is already pinned in VotesAndCommentsDialog.test.tsx. Nothing visible changes: no UI sketch, no usage-data event.
```

#### D10

```text
No nWave scenario tags in any test; plain words, Feature and Work Item wording as in the existing tests. Fake clock never real waits: no new setTimeout wait; the existing zero-delay settle stays. No testTimeout, no retry, no raised ceiling: slowTests.ts and the 54 s frontend budget are not touched. Per-test budget stays 2 s for .test.tsx and 0.7 s for .test.ts, no derogation requested; the feature share of the default suite only falls.
```

#### D13

```text
Q-6648-2 closed by the maintainer, 2026-11-15: YES, extract. One refactor(reports) commit moves failedAmong (with isTheSameReport) and the leave-out decision out of useReportsPdfExport.ts into a new pure printFailures.ts, behaviour unchanged, no test changed in it. The three Leave it out rule tests then move off the page onto printFailures.test.ts. Stryker runs once on printFailures.ts, last, on frozen code, kill rate at least 80 percent. The guarantee scripts allow exactly pages/Reports/printFailures.ts and useReportsPdfExport.ts as production changes.
```

#### D14

```text
Q-6648-3 closed by the maintainer, 2026-11-15: slice order is S1, then S3, then S2. S2 depends on nothing in S3 (distinct files, unchanged kit); the order is the maintainer choice. Rigor profile stays standard unless the maintainer says otherwise at DELIVER start.
```

#### D15

```text
Order S1 then S3 then S2 (maintainer). S1 is the walking slice that proves the measure script; S3 has no production change; S2 carries the only production commit and the Stryker run, last.
```

#### D16

```text
Targets, from the local baseline of 2026-11-15 (load about 7, so indicative only; the slice first step re-measures at load at or under 6). removal 11.22 s over 21 tests (CI median 16.6); combinedPdf 10.70 s over 18 (CI 15.2); comments 11.04 s over 24 (CI 15.5). File-alone target ratios: S1 at most 0.60, S2 at most 0.75, S3 at most 0.60. Combined (file plus its new direct tests) at most 0.80 for S1, S2 and S3. Design expectation from per-test times: S1 about 0.4, S2 about 0.70 (extraction path), S3 about 0.5. These are projections to be confirmed by the first measure, not facts. P1 has no numeric floor in the brief; the ratios are this design own margin. A file that misses its ratio is recorded as target not met with the number, not stretched with a timeout.
```

#### D17

```text
Guarantee mechanics, reused from Story 6561 (copied, adapted): baseline ids and expect( counts regenerated by a fresh run at slice start into this workspace; check-behaviour-free (one \bexpect\( counter, no weaker matcher, no skip/only/todo/timeout, every removed expect( has a row in the slice moved-assertions file, rows equal removals per file); id diff (a baseline id missing needs a row, a surviving id with a row needs a kept: note, an added id must be listed). Adaptation: file lists and baseline folder are this feature own. The production allow-list of the guarantee scripts is exactly src/pages/Reports/printFailures.ts and src/pages/Reports/useReportsPdfExport.ts, in S2 only (maintainer, 2026-11-15); it is empty in S1 and S3. A deliberate-break script applies each line of slice-SN-breaks.tsv (production file, a sed expression, the direct test that must go red), runs that test expecting assertion failures, restores with git checkout, and ends with git diff --exit-code on the production files, so the can-fail proof is a command not a claim.
```

### Constraints

#### K2

```text
Input bounds: the brief states none beyond the scope list; fixtures are as small as the rule allows (a removal dialog needs 3 reports at most, the page tests at most 2 picks except the one 4-pick leave-out test, a log of at most 4 entries). No bound is widened beyond the existing tests.
```

#### K3

```text
Every removed or shrunk expect( has a moved-assertions row; test ids lost have a row; deleting a test without mapping its rule is refused by check-slice. A merged test keeps the union of the assertions of the tests it replaces.
```

#### K4

```text
Direct tests drive the public port of the unit: the dialog by its props, the hook by renderHook with its real collaborators (QueryClient, ApiServiceContext with a mocked service), the function by call. They never import a private symbol; lostForGoodWarningOf stays private and is reached by rendering the dialog.
```

#### K5

```text
Trunk safety: Story 6535 changes report panels (not these files) and T12 may touch test order; re-read each file at slice start, stop on a conflict, never rebase autonomously.
```

#### K6

```text
Behaviour-free: no production file changes except, in S2 only, one refactor(reports) commit: pages/Reports/printFailures.ts (create) and useReportsPdfExport.ts (extend), a pure move with no test change in it. BulkReportsTestKit.tsx, RefinementTabTestKit.tsx, slowTests.ts and test-time-budget.json are not changed.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract is touched: no schema, wire format or config shape changes, and the shared Reports and Refinement test kits (BulkReportsTestKit.tsx, RefinementTabTestKit.tsx) are left unchanged, so their importers (15 files for the Reports kit) need no migration.
```

architectural_tests

```text
No dependency, boundary or owner law is reachable by a test-only change with no production file in any target; the existing law in reportsPage.enforcement.test.ts (only useBulkReportRemoval calls removeReportsInBulk) keeps running in pnpm test and is not changed.
```

contract_tests

```text
Producer and consumer do not change apart: no wire or persisted data and no second deployable is involved; the real unit rendered by each direct test is the same unit the page renders, so the page tests that stay are the integration.
```

### Boundaries

None.

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

None.

### Models

None.

### Closure

```text
story-6648 test rewrite S1 S2 S3
```

Safety

Pass

DesignedNotBuilt

```text
No production file is in any target by default, so production behaviour cannot change; git diff of production paths is empty at close, measured by check-slice.
```

##### Evidence

None.

```text
story-6648 test rewrite S1 S2 S3
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each direct test builds its input from existing kit constructors (aReportOn, aLog, anEntry, aRow) and renders the unit with props; the same constructors feed the page tests today, so valid inputs exist.
```

##### Evidence

None.

```text
story-6648 test rewrite S1 S2 S3
```

PublicReachability

Pass

DesignedNotBuilt

```text
Input -> kit constructor -> unit public props or hook -> vitest run -> printed assertion result; the chain ends at the vitest CLI, which the measure script drives.
```

##### Evidence

None.

```text
story-6648 test rewrite S1 S2 S3
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no temporal or fairness claim; the rewritten tests are finite and each run terminates under the vitest per-test budget.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: a moved rule is still proven because the direct test renders the same production unit. Negated: the direct test passes while the production line is broken (a fixture hides the rule).
```

```text
Every moved rule fails a test when its production line is broken
```

```text
A rule proven on a narrower fixture
```

```text
A direct test that stays green after the break
```

```text
A break not listed in slice-SN-breaks.tsv is not proven; the list is closed at the slice first step
```

acceptance designer

#### Y2

Stressor

```text
Assumption: the local runner measures the file fairly. Negated: load or a bimodal runner distorts one side of a pair.
```

```text
No PASS from an invalid pair
```

```text
unmeasured, machine loaded, with the slice recorded as target not met or not measured
```

```text
A retake loop or a ratio from fewer than 3 valid pairs
```

```text
Local ratio is indicative; CI median of at least 4 main runs after the push decides P1
```

maintainer

#### Y3

SinglePointOfFailure

```text
The before-commit baseline (ids, expect( counts, before copies) is the reference of every guarantee script
```

```text
The baseline equals a fresh run of the unmodified files
```

```text
Regeneration at slice start if trunk moved the file
```

```text
A stale or hand-edited baseline
```

```text
Scripts refuse a report older than the run or with a count that differs from the run summary
```

crafter

#### Y4

Bypass

```text
Deleting a page test or weakening a matcher without a row, or relabelling it skip/only/todo
```

```text
No rule loses its test
```

```text
Merging tests when the union of assertions is kept
```

```text
A removed expect( with no row, a skip/only/todo, a looser matcher, a longer timeout
```

```text
The weaker-matcher list is partial; the removed-expect( row check is the control that is not bypassable
```

reviewer

#### Y5

Stressor

```text
Second set, walked against the finished design and not executed: a wiring that only the removed page test covered (onCancel clears the confirmation and keeps picks, onDone clears picks, onCancel of the failure dialog keeps picks, closure over the Work Item opened at click time, readerKey reaching the log read). Negated: nothing remains that exercises it.
```

```text
One page render per wiring stays
```

```text
Wiring proven inside a merged page test
```

```text
A wiring with no remaining test
```

```text
Named per slice in its decision; the reviewer checks each named wiring has a surviving or merged test
```

reviewer

#### Y6

Stressor

```text
Assumption: the extraction is a pure move. Negated: the move changes behaviour, for example matching failed reports by id alone.
```

```text
The existing combinedPdf tests pass unmodified before and after the move
```

```text
Renamed internal parameters
```

```text
A test edited in the refactor commit, or a changed matching rule
```

```text
Stryker on printFailures.ts at least 80 percent, run last on frozen code; owner crafter
```

crafter

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.removal\.test\.tsx

Extend

```text
Remove or merge the page tests the direct tests replace; keep six wiring tests.
```

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh

#### Verification

```text
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S1
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run src/pages/Reports/ReportsPage.removal.test.tsx src/pages/Reports/RemoveSelectedDialog.test.tsx src/pages/Reports/useBulkReportRemoval.test.tsx src/pages/Reports/useSelectionActions.test.tsx
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S1
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S1
```

#### Supports

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/deliberate\-breaks\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S1\-breaks\.tsv

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S1\-moved\-assertions\.md

Lighthouse\.Frontend\/src\/pages\/Reports\/RemoveSelectedDialog\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/useBulkReportRemoval\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/useSelectionActions\.test\.tsx

#### Public oracle

```text
Run bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S1 from the repository root: it alternates 5 before/after pairs of ReportsPage.removal.test.tsx and runs the new direct tests.
```

```text
Exit 0 and a gate line with assertions_failed=0, the file-alone summed per-test seconds ratio at most 0.60 and the combined ratio at most 0.80 over at least 3 valid pairs, derived from the JSON report durations and not from any figure in this design.
```

```text
On the before-commit file with no direct tests the ratio is 1.0 and the script exits non-zero with assertions_failed=1; and with any line of slice-S1-breaks.tsv applied its named direct test goes red.
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
S1 removal, 21 tests become 6 page tests plus direct tests. Page stays (one render per wiring): pick three on two Teams and a Portfolio, confirm, they leave the list, and after Done the picks are cleared (selectedCount 0 added here); Cancel merged with Escape at the confirmation (dialog closes, nothing removed, three stay picked, list as before); a report the server did not remove is named and stays listed; permissions could not load, merged (tooltip says to reload, pressing Remove asks nothing); changing the filter clears picks (2 picks); sorting keeps picks (2 picks). RemoveSelectedDialog.test.tsx (rendered alone under QueryClientProvider and ApiServiceContext with a mocked removeReportsInBulk): lists every report in order; warning for one, plural for two, none when all rebuildable, no alert without a Then and Now and the button label; Remove sends the ids in order and shows the headline; already-gone counts as removed; cannot confirm; under way shows the progressbar and Escape does not close; result counts only removed and names only the failed with its reason; Escape on confirmation calls onCancel only; Escape on result and Done call onDone. useBulkReportRemoval.test.tsx (renderHook): forgets each removed and already-gone report, keeps one not removed, forgets the one removed not one picked before it, reads every report list again also after a failure. useSelectionActions.test.tsx (renderHook of useSelectionRemoval plus its button): disabled with the reason for one and for several owners she only reads, disabled with the reload reason when permissions failed, disabled while loading, pressing an enabled Remove opens the dialog, pressing a disabled one opens none. The reason wording itself stays proven by pickLimit.test.ts.
```

##### Constraints

None.

##### Reuse

RemoveSelectedDialog

Lighthouse\.Frontend\/src\/pages\/Reports\/RemoveSelectedDialog\.tsx\:136

Reuse

```text
Owns the confirmation, the warning, the headline and the close rule; rendered alone with its three props, no new type.
```

useBulkReportRemoval

Lighthouse\.Frontend\/src\/pages\/Reports\/useBulkReportRemoval\.ts\:18

Reuse

```text
Owns forgetting removed reports and re-reading the lists; tested by renderHook, no extraction needed.
```

useSelectionRemoval

Lighthouse\.Frontend\/src\/pages\/Reports\/useSelectionActions\.tsx\:22

Reuse

```text
Owns the not-admin count and when Remove is enabled; takes plain values (picks, RbacState) so it needs neither page nor grid.
```

removeBlockedBecause

Lighthouse\.Frontend\/src\/pages\/Reports\/pickLimit\.ts\:10

Reuse

```text
The reason wording is already proven on the function by pickLimit.test.ts; the three reason page tests add nothing for it.
```

rowOf

Lighthouse\.Frontend\/src\/tests\/BulkReportsTestKit\.tsx\:504

Reuse

```text
A cheaper lookup was spiked and showed no gain within noise (render plus 3 picks about the same); the kit has 15 importers, so it is left unchanged.
```

pickTheThree

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.removal\.test\.tsx\:62

Extend

```text
The page file shrinks in place to its wiring tests; pickTheThree stays for them (fewer picks where two suffice). The new direct tests are separate files beside the units.
```

##### Impact

test\-file

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.removal\.test\.tsx\:1

Changed

```text
15 tests removed or merged into 6, each with moved-assertions rows.
```

test\-support

Lighthouse\.Frontend\/src\/tests\/BulkReportsTestKit\.tsx\:504

UnchangedCompatible

```text
Not edited; all 15 importers run unchanged.
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/RemoveSelectedDialog\.tsx\:136

UnchangedCompatible

```text
No production edit.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No production move: the unit under test is already testable alone through its props or hook. The test-level move is a pure move commit, then edit commits, per the shared order decision.
```

##### Boundaries

```text
The vitest CLI run from the repository root through measure-slice.sh S1, and for the rules the public props of RemoveSelectedDialog and the public return of useBulkReportRemoval and useSelectionRemoval
```

###### Driven ports

```text
ApiServiceContext reportService (mocked removeReportsInBulk and getAllReports)
```

```text
Tests depend on production units and on BulkReportsTestKit and ReportsTestKit; production never depends on tests; no production file changes.
```

###### Failures

None.

##### Observations

```text
The measure script prints one gate line for the slice files: observed (tests run), assertions_failed (1 when the time ratio exceeds its limit or a test fails), setup_failed, the file-alone ratio, the combined ratio and the number of valid pairs; exit 0 only when all hold.
```

gate\-line

```text
A slower machine from slower tests when a pair is invalid, which is why invalid pairs are never counted and fewer than 3 valid pairs is unmeasured, not pass.
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
Every rule the 15 page tests of ReportsPage.removal that leave the page proved is proven by a direct test that goes red when its production line is broken
```

Invariant

Application

RemoveSelectedDialog\, useBulkReportRemoval\, useSelectionRemoval

###### Treatment

Finite

###### Cases

```text
lostForGoodWarningOf returning a warning when lost is 0 -> warning and no-warning tests
```

```text
lostForGoodWarningOf singular and plural branches swapped -> singular and plural tests
```

```text
headlineOf counting every outcome not only removed -> headline and not-removed result tests
```

```text
close() without the isPending guard -> under-way cannot close test
```

```text
close() calling onCancel after an answer or onDone before one -> two Escape tests
```

```text
useBulkReportRemoval onSuccess forgetting a notRemoved report -> forgets-only-removed test
```

```text
useBulkReportRemoval matching the report by position not by id -> forgotten-is-the-one-removed test
```

```text
useBulkReportRemoval without the invalidate in onSettled -> lists-read-again test
```

```text
useSelectionRemoval notAdmin counting owners she can edit -> reason tests
```

```text
useSelectionRemoval enabling Remove when blockedBecause is not empty -> disabled and press-asks-nothing tests
```

```text
The list is closed: the rules are the ones the 15 moved page tests name, one break per rule, recorded in slice-S1-breaks.tsv.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx

Extend

```text
Merge and drop the page tests the dialog tests and the merges replace; keep 13 wiring tests.
```

Lighthouse\.Frontend\/src\/pages\/Reports\/printFailures\.ts

Create

```text
Pure module of the two moved rules, CreateNew.
```

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts

Extend

```text
Calls printFailures; behaviour unchanged.
```

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh

#### Verification

```text
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S2
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run src/pages/Reports/ReportsPage.combinedPdf.test.tsx src/pages/Reports/NoPdfDialog.test.tsx src/pages/Reports/printFailures.test.ts
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S2
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S2
pnpm --dir Lighthouse.Frontend exec stryker run stryker.6648.frontend.json
```

#### Supports

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/deliberate\-breaks\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S2\-breaks\.tsv

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S2\-moved\-assertions\.md

Lighthouse\.Frontend\/src\/pages\/Reports\/NoPdfDialog\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/printFailures\.test\.ts

Lighthouse\.Frontend\/stryker\.6648\.frontend\.json

#### Public oracle

```text
Run bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S2 from the repository root: 5 alternating before/after pairs of ReportsPage.combinedPdf.test.tsx plus the new NoPdfDialog and printFailures tests; then, last on frozen code, Stryker on printFailures.ts.
```

```text
Exit 0 and a gate line with assertions_failed=0, file-alone ratio at most 0.75 and combined ratio at most 0.80 over at least 3 valid pairs, read from the JSON report durations; Stryker kill rate on printFailures.ts at least 80 percent.
```

```text
The before-commit file alone has ratio 1.0 and the script exits non-zero with assertions_failed=1; with any line of slice-S2-breaks.tsv applied its named test goes red; a Stryker kill rate under 80 percent fails the slice.
```

##### Promises

P1

P2

P3

#### Prefactoring

```text
L3 move: extract failedAmong, isTheSameReport and the leave-out decision from useReportsPdfExport.ts into pure printFailures.ts; makes the follow-up change (the Leave it out rule tests moving off the page) possible
```

```text
No behaviour change: the existing ReportsPage.combinedPdf.test.tsx passes unmodified before the move and after the move, before any test edit, with the same file content and the same argv
```

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run src/pages/Reports/ReportsPage.combinedPdf.test.tsx
```

#### Records

##### Decisions

```text
S2 combinedPdf, 18 tests become 12 page tests plus direct tests, extraction path. First a refactor(reports) commit moves failedAmong, isTheSameReport and the leave-out decision (offered only when something failed and something would be left; leaves out only the failed) into pure printFailures.ts; useReportsPdfExport.ts calls it. No test changes in that commit. Page stays: a reader who is Admin of none exports the reports of two Teams as one PDF each on a page of its own (merges the coach and Admin-of-none tests); once the dialog closes the frame goes; the table order as sorted; nothing prints while a report is on its way; picks and sorting changed while preparing; one held-back test (merges the two held-back tests); the preparing look; a failed report is named, nothing printed, then Cancel closes the dialog, prints nothing and keeps the picks (merges two tests); one Leave it out wiring test (Voyager fails, Leave it out unpicks it and prints the rest in table order, Export PDF count drops); Retry; part of the page failed (message and nothing printed); nothing picked disables with a tooltip. Moved off the page onto printFailures.test.ts: Leave it out unpicks every failed report, is not offered when nothing would be left, leaves out only the failed report not the other report of the same Team, and matches by id and owner. NoPdfDialog.test.tsx (alone, props only): names each failed report in order with its owner kind word, accessible description equals the list, the part-of-the-page message when failedReports is empty, Cancel, Leave it out, Retry when onLeaveItOut is given and Cancel, Retry when it is undefined, each button calls its own handler once and no other, Escape calls onCancel.
```

##### Constraints

```text
Stryker config: Lighthouse.Frontend has no default Stryker config and a bare StrykerJS run with default disableTypeChecks once rewrote 661 files, so the S2 run names stryker.6648.frontend.json, written by the crafter in S2 modelled on stryker.6536.frontend.json: a whole-file mutate entry for src/pages/Reports/printFailures.ts (single-line entries are dropped), Vitest limited to printFailures.test.ts and ReportsPage.combinedPdf.test.tsx, disableTypeChecks off, report under docs/feature/story-6648-frontend-clean-as-we-go/mutation/.
```

##### Reuse

NoPdfDialog

Lighthouse\.Frontend\/src\/pages\/Reports\/NoPdfDialog\.tsx\:30

Reuse

```text
Owns the failure lines and the button set; rendered alone with its four props.
```

useReportsPdfExport

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:66

Extend

```text
Keeps the frame, the preparing state and the unpick wiring; calls printFailures for the two rules. Behaviour unchanged.
```

failedAmong

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:52

Replace

```text
Moves to the new pure printFailures.ts with isTheSameReport and the leave-out decision, so the rules are tested in milliseconds and mutation-tested. No other owner of these rules exists (searched: only useReportsPdfExport.ts mentions failedAmong or the leave-out).
```

eachReportReads

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx\:75

Extend

```text
The page file merges and drops tests in place; its read fakes stay for the wirings that remain. NoPdfDialog.test.tsx is a new file beside the dialog.
```

printFailures

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:52

CreateNew

```text
New pure module pages/Reports/printFailures.ts (does not exist yet): no existing unit can own a decision now private to a hook; it holds failedAmong and the leave-out decision as plain functions over reports.
```

failedAmong

Lighthouse\.Frontend\/src\/pages\/Reports\/printFailures\.ts\:1

CreateNew

```text
New pure module printFailures.ts; no existing unit can own these rules, which are private to a hook today.
```

##### Impact

test\-file

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx\:1

Changed

```text
18 tests become 13 with moved-assertions rows for every removed expect(.
```

test\-support

Lighthouse\.Frontend\/src\/tests\/ReportPrintTestKit\.tsx\:1

UnchangedCompatible

```text
Not edited.
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:66

Changed

```text
Pure move of two rules to printFailures.ts in one refactor(reports) commit; callers of useReportsPdfExport (useSelectionActions, ReportsPage) see the same interface.
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:52

Migrated

```text
failedAmong and isTheSameReport migrate to printFailures.ts; no other consumer.
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/printFailures\.ts\:1

Changed

```text
New file holding the moved rules; only useReportsPdfExport.ts imports it.
```

##### Agreement

None.

##### Not applicable

None.

##### Boundaries

```text
The vitest CLI through measure-slice.sh S2, the public props of NoPdfDialog, and the rendered Reports page for the wirings that stay
```

###### Driven ports

```text
ApiServiceContext reportService (getAllReports, getReport), the print frame browser fake aPrintFrameBrowser and the fonts fake in ReportPrintTestKit
```

```text
Tests depend on production and on the kits; production never depends on tests; no production file changes on the default path.
```

###### Failures

None.

##### Observations

```text
The measure script prints one gate line for the slice files: observed, assertions_failed, setup_failed, file-alone ratio, combined ratio, valid pairs; exit 0 only when all hold.
```

gate\-line

```text
Time spent drawing reports in the hidden frame from time spent mounting the page; only the ratio of the whole file is judged.
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
Every rule the page tests of ReportsPage.combinedPdf that leave or merge proved is proven by a test that goes red when its production line is broken
```

Invariant

Application

NoPdfDialog\, printFailures

###### Treatment

Finite

###### Cases

```text
NoPdfDialog showing Leave it out although onLeaveItOut is undefined -> dialog button-set test
```

```text
NoPdfDialog empty-list branch inverted -> part-of-the-page message test
```

```text
NoPdfDialog Cancel and Retry handlers swapped -> each-button test
```

```text
failedAmong returning every report -> printFailures test and the Leave it out wiring page test
```

```text
failedAmong matching by id only or by owner only -> printFailures id-and-owner test
```

```text
leave-out decision offering Leave it out when nothing would be left -> printFailures not-offered test
```

```text
leave-out decision leaving out the other report of the same Team -> printFailures only-the-failed test
```

```text
useReportsPdfExport unpick(failed) dropped -> Leave it out wiring page test (Export PDF count)
```

```text
useReportsPdfExport onCancel replaced by a no-op -> failure-named-and-Cancel page test
```

```text
The list is closed and recorded in slice-S2-breaks.tsv.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.comments\.test\.tsx

Extend

```text
Remove or merge the page tests the direct tests replace; keep seven wiring tests.
```

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh

#### Verification

```text
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S3
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S3
bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S3
```

#### Supports

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/deliberate\-breaks\.sh

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S3\-breaks\.tsv

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/slice\-S3\-moved\-assertions\.md

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/ConditionPrompt\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/VotesAndCommentsDialog\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useSizingLog\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useCommentAdding\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useVoteCasting\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/sizingLogWording\.test\.ts

#### Public oracle

```text
Run bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S3 from the repository root: 5 alternating before/after pairs of RefinementView.comments.test.tsx plus the new and extended direct test files.
```

```text
Exit 0 and a gate line with assertions_failed=0, file-alone ratio at most 0.60 and combined ratio at most 0.80 over at least 3 valid pairs, read from the JSON report durations.
```

```text
The before-commit file alone has ratio 1.0 and the script exits non-zero with assertions_failed=1; and with any line of slice-S3-breaks.tsv applied its named test goes red.
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
S3 comments, 24 tests become 7 page tests plus direct tests, no production change. Page stays: a browser without a name is asked who votes, then the condition of its Yes, if, and one castVote carries answer, channel, name, trimmed condition and key; a comment sent from the votes and comments is answered by the Warnings cell naming the open question, addComment gets comment, channel, name and key, the log is read again so the comment shows, and the first log read carried the browser key (merges two tests); a comment sent on one Work Item and answered late leaves the log of the Work Item now open (the closure over the Work Item opened at click time); a refused comment shows its error, keeps the text and Send is enabled again; the log is read without the key for somebody signed in; the Votes cell and Warnings cell marks; one log-cannot-be-read row on the page (the renamed-terms row, 404). ConditionPrompt.test.tsx (new, alone): title, Work Item line, placeholder, maxlength 2000, a padded pasted condition is confirmed trimmed once, blank or spaces keep Vote disabled and confirm nothing, Cancel calls onCancel only. useVoteCasting.test.tsx (extend, renderHook): Yes, if asks for its condition and casts nothing until voteWithCondition, which casts once with the condition as comment; cancelCondition casts nothing; a browser without a name is asked the name first and the condition second. useSizingLog.test.tsx (extend): readAgain for a Work Item no longer open reads nothing; a new reader key alone does not read again and the next read carries it; the open read carries the key, or null for an account. useCommentAdding.test.tsx (extend): a refused comment calls onFailure, never onSent, and clears commentsBeingSent; a browser without a name asks the name and sends under it once. VotesAndCommentsDialog.test.tsx (extend, alone): lists only entries with words in log order with who, day and condition; open question marked only where the entry says so, also between people sharing a name; no channel wording; markup shown as text; a revocation adds nothing; counts come with a tooltip naming the people on hover and focus, none for a count of 0; No comments yet when only votes exist; a reader who never voted sees split and comments; a failed read shows the alert with the describeFailure text; Send disabled for blank text and while sending (box disabled too), maxlength 2000, trimmed text sent, draft kept when onSent is not called and cleared when it is. sizingLogWording.test.ts (extend): describeLogFailure for a 404 names the renamed terms and for any other error returns its message.
```

##### Constraints

None.

##### Reuse

useSizingLog

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useSizingLog\.ts\:68

Reuse

```text
Owns which Work Item log is shown and when it is read again; already hook-tested for stale answers, the readAgain guard and key rule are the gaps and are added there.
```

VotesAndCommentsDialog

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/VotesAndCommentsDialog\.tsx\:221

Extend

```text
VotesAndCommentsDialog.test.tsx already renders it alone with props; the log display tests go there.
```

ConditionPrompt

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/ConditionPrompt\.tsx\:24

Reuse

```text
No test file exists; a new ConditionPrompt.test.tsx renders it alone.
```

useVoteCasting

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useVoteCasting\.ts\:59

Reuse

```text
Owns holding a Yes, if for its condition; test file exists and is extended.
```

useCommentAdding

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useCommentAdding\.ts\:23

Reuse

```text
Owns send-once, failure and name-first for comments; test file exists and is extended.
```

describeLogFailure

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/sizingLogWording\.ts\:43

Reuse

```text
Pure wording function with no direct test today; its two page rows move to sizingLogWording.test.ts.
```

startACommentOn

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.comments\.test\.tsx\:104

Extend

```text
The page file shrinks in place to the wiring tests; the comment helper stays for them.
```

##### Impact

test\-file

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.comments\.test\.tsx\:1

Changed

```text
24 tests become 7, with moved-assertions rows.
```

test\-support

Lighthouse\.Frontend\/src\/tests\/RefinementTabTestKit\.tsx\:1

UnchangedCompatible

```text
Not edited.
```

production

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.tsx\:1

UnchangedCompatible

```text
No production edit; no extraction is needed because every rule already lives in an exported unit.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No production move: every moved rule already lives in an exported unit (hook, component, function) that is testable alone.
```

##### Boundaries

```text
The vitest CLI through measure-slice.sh S3, the public props of ConditionPrompt and VotesAndCommentsDialog, the public return of useVoteCasting, useSizingLog and useCommentAdding, and describeLogFailure; the rendered Refinement tab for the wirings that stay
```

###### Driven ports

```text
ApiServiceContext sizingLogService (castVote, addComment, getLog)
```

```text
Tests depend on production and on RefinementTabTestKit; production never depends on tests; no production file changes.
```

###### Failures

None.

##### Observations

```text
The measure script prints one gate line for the slice files: observed, assertions_failed, setup_failed, file-alone ratio, combined ratio, valid pairs; exit 0 only when all hold.
```

gate\-line

```text
Typing cost from mount cost inside one test; the ratio of the whole file is judged.
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
Every rule the 17 page tests of RefinementView.comments that leave or merge proved is proven by a test that goes red when its production line is broken
```

Invariant

Application

ConditionPrompt\, useVoteCasting\, useSizingLog\, useCommentAdding\, VotesAndCommentsDialog\, describeLogFailure

###### Treatment

Finite

###### Cases

```text
ConditionPrompt without the blank check on the trimmed draft -> blank test
```

```text
ConditionPrompt confirming the draft not the trimmed condition -> trimmed test
```

```text
useVoteCasting awaitsCondition ignoring YesBut -> condition-asked test
```

```text
useSizingLog.readAgain without the open-reference guard -> late-answer hook test and the page late-answer test
```

```text
useSizingLog read depending on readerKey so a new key re-reads -> key-arrives hook test
```

```text
useCommentAdding without catch(onFailure) -> refused-comment hook test
```

```text
VotesAndCommentsDialog without the isWritten filter -> words-only and revocation tests
```

```text
VotesAndCommentsDialog open-question marker inverted -> open-question test
```

```text
VotesAndCommentsDialog counts from the row instead of the log voters -> tooltip and split tests
```

```text
VotesAndCommentsDialog Send enabled while sending -> sending test
```

```text
describeLogFailure 404 branch removed -> function test and the page row
```

```text
Closed list, recorded in slice-S3-breaks.tsv; hook tests exist for double-send, team switch and stale answers and stay as they are.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
