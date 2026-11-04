# Slice 8 - Connection forms: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with a parallel session (load
average 3 to 5 on 4 cores during the runs).

## Pre-slice commit

`608f3017f046ac8846650d8b97ff4d5d5225d6c1` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. The slice is behaviour-free: no production behaviour changes, no
assertion is weakened or dropped without a row in the table at the end, no surviving test id changes, no added
skip/todo/only/fails, no looser matcher, no longer timeout.

### Search result: what is real logic, and where it sits

The two Edit page test files (`EditConnection.fieldChange.test.tsx`, `EditConnection.defaults.test.tsx`) render
`EditConnectionPage`, but the page (`EditConnection.tsx`, 209 lines) is thin: RBAC gate, the connection query, the
usage-data reports and the navigation. The logic the 80 tests ask about is private to
`src/components/Common/Connection/ModifyConnectionSettings.tsx` (1212 lines), which the page renders for an existing
connection. Read first, in that file:

- **Which question a save asks** (lines ~96-300): `fieldsChangedSince` (an Additional Field set changed, ids
  included, order ignored), `followersOfDefault`, `changedDefaultsToUpdateFollowersOf` (only defaults read while
  updating, only with followers, `null` and `undefined` equal), `changedEstimationDefaultsWithFollowers`,
  `followersIn` (an owner following several changed defaults is counted once), `whyUpdate` (fields beat defaults,
  Estimation-only asks just whether to save, nothing changed asks nothing) and the table `WhereEachDefaultSits`
  (group, read-when, what changes, label). All already pure, only private to the component file. The sentences
  built from the result live in `UpdateQuestion.tsx`, which has its own 19 tests (title and description per case,
  renamed terms, answers); no extraction there.
- **Which defaults a field removal clears** (lines ~905-925, inside `handleAdditionalFieldsChange`): the
  `Object.fromEntries` over the defaults that nulls every default whose field (for an Estimation default, the field
  inside the bundle, via `fieldOfDefault`) is no longer in the list. Pure over `(defaults, fields)`, inline in a
  handler.
- Not extracted: `handleSave` (validate, then ask or save), `save` (409 mapping), `saveAnswering` (choice carries
  `additionalFieldsChanged`): they sequence the injected service calls and the dialog, which is wiring.
  `EditConnection.tsx`'s two usage-data reports are wiring too. `CreateConnectionWizard.tsx` (649 lines): see below.

### Proposed extractions (files beside the component)

| Extraction | New file | Unit-test file |
|---|---|---|
| `WhereEachDefaultSits`, `fieldsChangedSince`, `changedDefaultsToUpdateFollowersOf`, `changedEstimationDefaultsWithFollowers`, `whyUpdate` and their private helpers (`followerGroupsOf`, `followersOfDefault`, `followersIn`, `sameEstimation`, `isUsed`) moved out verbatim as exported pure functions; the component imports them (the defaults section still uses `WhereEachDefaultSits`) | `src/components/Common/Connection/whatASaveAsks.ts` | `src/components/Common/Connection/whatASaveAsks.test.ts` |
| `fieldOfDefault` stays with the table; the field-removal clearing becomes `defaultsKeepingOnlyFields(defaults, fields)` | `src/components/Common/Connection/defaultsKeepingOnlyFields.ts` | `src/components/Common/Connection/defaultsKeepingOnlyFields.test.ts` |

Behaviour is unchanged; bodies move verbatim (a split for the cognitive-complexity limit is allowed, as in slice 10,
with the same conditions and order). `ModifyConnectionSettings.tsx` is the only production file that changes besides
the two new ones; `EditConnection.tsx`, `UpdateQuestion.tsx`, `CreateConnectionWizard.tsx` and the models do not.

### The wizard: nothing moves

`CreateConnectionWizard.test.tsx` (42 tests, 7.0 s, no real wait): stepper and step changes, auth options and
Next gating, validation verdicts and ApiError text, OAuth popup outcomes and draft cleanup, step 3 Create,
DTO and save failure copy, Cancel, no standalone Validate button, the secret-handling notice. Every one proves
wiring between steps, injected services and what a person sees. The only pure rule is `configInputsValid` (two
tests, "disables/enables Next"), worth under 0.3 s and a render away from the field being typed; extracting it would
trade a real wiring test for a function test. Decision: no extraction, no wizard test changes. The file is declared
for completeness, its 42 ids are preserved untouched.

### `EditConnection.test.tsx` (15 tests, 0.42 s)

Title, which service is called, RBAC guard, reconnect banner, navigation. All wiring, all fast. Untouched.

### Production files that may change

- `src/components/Common/Connection/ModifyConnectionSettings.tsx` (imports; the moved functions and now-unused
  imports leave the file; `handleAdditionalFieldsChange` calls `defaultsKeepingOnlyFields`)
- the two new production files above

### Test files edited (existing)

- `src/pages/Connections/Edit/EditConnection.fieldChange.test.tsx` (10 ids leave, see the table)
- `src/pages/Connections/Edit/EditConnection.defaults.test.tsx` (15 ids leave)
- `src/components/Common/Connection/UpdateQuestion.test.tsx` only if one of the question-wording rows below is not
  already asserted there (a case added, none removed); checked row by row at 09-02.

Added: `whatASaveAsks.test.ts`, `defaultsKeepingOnlyFields.test.ts`.

Untouched (ids unchanged): `EditConnection.test.tsx`, `CreateConnectionWizard.test.tsx`, and every other file under the
two directories (`ModifyConnectionSettings.test.tsx`, `ModifyConnectionSettings.directionTips.test.tsx`,
`EstimationDefaultEditor.test.tsx`, `SecretHandlingNotice.test.tsx`, `UpdateQuestion.test.tsx` apart from the
condition above).

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

None (grep for the four file names and the directories: no entry). Nothing to delete or lower. No real wait in the four
files either (one zero-delay `setTimeout` flush, `theNextTick`, in `EditConnection.fieldChange.test.tsx`, stays).

## Classification of every test of the two Edit page files

Rule: a test **moves** (id leaves, a function test takes its logic, a row below) when it only asks "given these
usage counts, defaults and fields, which question / which clearing", and the wording is already pinned per case in
`UpdateQuestion.test.tsx`. A test **stays** when it proves wiring (the editor, dialog and form produce the inputs, the
save and navigation receive the outputs, usage data is reported) or what a person sees (loading look, failure look,
section layout, words of this instance in the section, focus, busy state).

### `EditConnection.defaults.test.tsx` (42 ids): 15 move, 27 stay

Move (ids as `describe path :: name` suffix):

| # | Test (suffix) | Question it answers | Goes to |
|---|---|---|---|
| 1-4 | `a changed {parentOverrideForPortfolios, dependencyFieldForPortfolios, sizeEstimateFieldForPortfolios, featureOwnerFieldForPortfolios} default 2 Portfolios follow asks whether to update them` | label and "what changes" per default in `whyUpdate` | `whatASaveAsks.test.ts` (one case per default, same expected strings as `labels`/`changes`) plus the existing `UpdateQuestion.test.tsx` cases for the sentences |
| 5-7 | `3 Portfolios following both the A and B defaults, both changed, are counted once` (three pairs) | `followersIn` counts a shared group once | `whatASaveAsks.test.ts` |
| 8 | `a changed Dependency default says its followers' dependencies change once updated` | `followersOfDefault` ignores a group that follows another default | `whatASaveAsks.test.ts` |
| 9 | `Portfolios following a changed default and another one left as it was are all asked about` | followers of a changed default, whole group counted | `whatASaveAsks.test.ts` |
| 10 | `a changed default nobody follows saves without asking, though other defaults have followers` | no followers of the changed name means no question | `whatASaveAsks.test.ts` |
| 11 | `a followed Estimation default left as it was asks nothing when another default changes` | `sameEstimation` on an unchanged bundle | `whatASaveAsks.test.ts` |
| 12 | `a changed Estimation default nobody follows saves without asking anything` | no followers means no question | `whatASaveAsks.test.ts` |
| 13 | `a changed Portfolios' Estimation default asks about the Portfolios that follow it` | `because: "estimation"`, Portfolios count | `whatASaveAsks.test.ts` |
| 14 | `changing only the unit of a followed Estimation default asks before saving` | `sameEstimation` sees a unit change | `whatASaveAsks.test.ts` |
| 15 | `removing the field the Teams' Estimation default reads clears that default in the save` | the bundle's field decides the clearing | `defaultsKeepingOnlyFields.test.ts` |

Stay (27): the section tests (`the section on the connection page`, 13: layout per tracker, words, None display,
loading look, failure look, late answer dropped, Try again); one dialog test per wiring chain (`asks whether to
update the 3 Teams ...`, `Save only sends ...`, `Cancel saves nothing ...`, `the dialog never names who follows`,
`changing a default clears the refusal the last save got`, `a default nobody follows saves without asking anything`
(also asserts the save carries no choice), `an Additional Field and a followed default changed in one save ...`,
`answering the question about a default does not report that Additional Fields changed`); `saving a changed default`
(`neither a default nor a field`, `removing the field a default names clears that default in the save`, `renaming the
connection sends ...`); the Estimation chain (`a changed Estimation default 4 Teams follow asks only to save`, `a save
that also changes a default read when updating ...`, `stays collapsed`).

### `EditConnection.fieldChange.test.tsx` (38 ids): 10 move, 28 stay

Move:

| # | Test (suffix) | Question it answers | Goes to |
|---|---|---|---|
| 1-5 | `with {one of each, Squads and Programs, one Squad and one Program, no Portfolio, no Team} the question reads as such` | title wording per count and terms | `UpdateQuestion.test.tsx` (existing title cases; `no Portfolio`, `no Team`, `one of each` and `Squads and Programs` added there if absent); the counts come from `usage` straight into `whyUpdate`, asserted in `whatASaveAsks.test.ts` |
| 6 | `the explanation names the Teams and Portfolios in the words this instance uses` | description wording with renamed terms | `UpdateQuestion.test.tsx` |
| 7 | `a field Priya started to add and then cancelled asks no question` | `fieldsChangedSince` of identical sets | `whatASaveAsks.test.ts` |
| 8 | `a field Priya added and removed again before saving asks no question` | same | `whatASaveAsks.test.ts` |
| 9 | `swapping the names and references of two fields asks the question` | the id is in the compared key | `whatASaveAsks.test.ts` |
| 10 | `a field change on a connection no Team or Portfolio uses is saved straight away without a question` | `isUsed` of zero usage | `whatASaveAsks.test.ts` |

Stay (28): `adding a field and pressing Save asks ...`, `Save and update ...`, `the question is asked whenever Priya
{adds, renames, removes} a field` (editor to question wiring), `Cancel ...`, `changing a field no longer promises ...`,
`when someone else saved the connection first ...`, `setting up a new connection ...`, `a save with only {a new name, a
new API token}` (what reaches the save), `a field Priya removed and added back with the same name and reference asks the
question` (the editor gives the re-added field a new id), `a field change the tracker refuses ...`, the six
`save now, update later` tests (usage data), the six `while the answer is being saved` tests (busy state, Esc, 409), the
two `a save that removes a field still in use` tests.

## Tracker-specific field sets the tests rely on (none may be lost when logic moves)

Source: `WhatEachTrackerOffers` in `EditConnection.defaults.test.tsx` (what `supportedDefaults` the connection
carries), read by the section and the page; `whyUpdate` and its siblings do **not** consult it: they iterate every
name of `WhereEachDefaultSits`, so the function tests need no tracker.

| Tracker | Defaults offered | Tests that rely on it |
|---|---|---|
| Jira | all seven: `parentOverrideForTeams`, `estimationForTeams`, `parentOverrideForPortfolios`, `dependencyFieldForPortfolios`, `sizeEstimateFieldForPortfolios`, `featureOwnerFieldForPortfolios`, `estimationForPortfolios` | the whole file's default connection (`jiraDcPayments`, tracker `Jira`); the moved rows 1-14 pick their rows from it, so the function tests cover all seven names (one case per name; the table stays complete) |
| AzureDevOps | the same seven | the section's `Jira connection shows ...` test reads the same set via `EveryDefault`; stays |
| Csv | `estimationForTeams`, `featureOwnerFieldForPortfolios`, `estimationForPortfolios` | `a CSV connection offers only the Feature Owner and Estimation defaults`, `a connection that offers only Teams defaults ...`; stay |
| Linear, ServiceNow | none | the two `shows no defaults section` tests; stay |

Follower fixtures the moved rows rely on: `jiraDcPayments` usage `{ teams: 4, portfolios: 2, followers, followerGroups }`
(defaults file) and `nordwindJiraDc` usage `{ teams: 4, portfolios: 2 }` (field-change file); `followerGroups` absent
means each default in `followers` is its own group of that size (`followerGroupsOf`), which the function tests also
cover (groups given, groups absent, a count of zero).

## Preserved set (before)

`measurement/slice-08-ids-before.txt`: 137 ids, one per line, sorted, as `file :: fullName` (file paths relative to
`Lighthouse.Frontend/`), from a run of the four files (137 passed, 0 skipped, 0 failed;
`LIGHTHOUSE_TIME_BUDGET=warn`, `LIGHTHOUSE_TEST_WORKERS=2`, vitest json reporter). Per file:
`EditConnection.test.tsx` 15, `EditConnection.fieldChange.test.tsx` 38, `EditConnection.defaults.test.tsx` 42,
`CreateConnectionWizard.test.tsx` 42. Surviving ids must be unchanged at close; the 25 ids above leave and are the
only ones that may be missing (the rows of this report).

Command (in `Lighthouse.Frontend`):

```
LIGHTHOUSE_TEST_WORKERS=2 LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  src/pages/Connections/Edit/EditConnection.test.tsx \
  src/pages/Connections/Edit/EditConnection.fieldChange.test.tsx \
  src/pages/Connections/Edit/EditConnection.defaults.test.tsx \
  src/components/Common/Connection/CreateConnectionWizard.test.tsx --reporter=json
```

## Before figures (indicative, not quiet)

Summed per-test durations of the touched files (the figure the close-time pairs compare), one run:

| File | Tests | Summed seconds |
|---|---|---|
| `EditConnection.test.tsx` | 15 | 0.42 |
| `EditConnection.fieldChange.test.tsx` | 38 | 11.33 |
| `EditConnection.defaults.test.tsx` | 42 | 7.58 |
| `CreateConnectionWizard.test.tsx` | 42 | 7.00 |
| Sum | 137 | 26.32 |

(The orchestrator's earlier quick measure at the same commit read 25.6 s; the spread is the load.)

## Coverage (before)

`src/pages/Connections/**` and `src/components/Common/Connection/**`, from a run of both directories' tests
(15 files incl. the neighbouring `Connections/` component tests that drive their own sources, 275 tests, all passed):

```
LIGHTHOUSE_TEST_WORKERS=2 LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run \
  src/pages/Connections src/components/Common/Connection \
  --coverage.enabled --coverage.include='src/pages/Connections/**' \
  --coverage.include='src/components/Common/Connection/**' --coverage.reporter=text
```

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (both directories) | 89.59 | 82.50 | 87.50 | 90.51 |
| `src/components/Common/Connection` | 89.22 | 81.90 | 87.79 | 90.06 |
| `ModifyConnectionSettings.tsx` | 87.46 | 82.86 | 83.50 | 87.89 |
| `CreateConnectionWizard.tsx` | 88.64 | 76.22 | 90.38 | 90.32 |
| `src/pages/Connections/Edit` (`EditConnection.tsx`) | 94.11 | 90.62 | 83.33 | 95.91 |

Line coverage (90.51) and branch coverage (82.50) of the two directories must not be lower at close. The moved
functions stay covered by their own files; the component file's own percentage may fall (the covered functions left
it), judged by the directory total as in slice 10.

## Expected saving (honest figure)

Time is per-test, not per-file: the page tests have no real wait, so a removed test saves its render, its dialog and
its MUI selects, and nothing else.

| File | Ids leaving | Mean seconds per id today | Expected saving |
|---|---|---|---|
| `EditConnection.defaults.test.tsx` | 15 of 42 | 0.18 (these pick a default and open a dialog, so nearer 0.22) | about 3.0 to 3.3 s of 7.58 |
| `EditConnection.fieldChange.test.tsx` | 10 of 38 | 0.30 | about 3.0 s of 11.33 |
| `EditConnection.test.tsx`, `CreateConnectionWizard.test.tsx` | 0 | | 0 |
| Added function tests | about 40 cases in two files | well under 5 ms each | about +0.1 s |

**About 6 s of 26.3 s (roughly 23 %)**, indicative (not quiet) and from an average, not from the individual
durations; the close-time alternating pairs give the measured figure. On a whole-suite wall clock at the default
worker count that is a few seconds at most. There is no allow-list entry and no real wait to remove, and 112 of the 137
tests, including the whole wizard (7 s), must stay as renders because they prove wiring.

Recommendation: step 09-02 is worth doing at this size (25 ids, two small pure files, one production file touched,
behaviour-free), and it is not worth widening: the remaining 20 s is wiring that only a render can prove. If the
maintainer wants only measurable wall-clock returns, this is the least of the rewrite slices; dropping 09-02 would
leave about 6 s of summed test time on the table and the question logic still private to a 1212-line component.

## Delta

Commits (pre-slice `608f3017f`): `f959554ee` extraction (no test file changed; the 275 tests of the two directories passed
unmodified at it), `65b9bfd4b` test rewrite, `73b296192` first close report; the revision after the review (three renders restored,
function tests tightened, logic mutants, ids-after list, re-measured timing) is the commit that carries this text. Files changed since the pre-slice
commit (`git diff --name-only 608f3017f`, apart from this report, the declaration and `feature-delta.md`):
`whatASaveAsks.ts` (new), `defaultsKeepingOnlyFields.ts` (new), `ModifyConnectionSettings.tsx`, `whatASaveAsks.test.ts`
(new), `defaultsKeepingOnlyFields.test.ts` (new), `EditConnection.defaults.test.tsx`, `EditConnection.fieldChange.test.tsx`,
`UpdateQuestion.test.tsx`. `slowTests.ts`, `setupTests.ts`, `EditConnection.tsx`, `UpdateQuestion.tsx`,
`CreateConnectionWizard.tsx` and every other production file are untouched.

RED: skipped for the extraction (behaviour-free, the existing tests are the net). The bodies moved as written, with two
placement choices: `fieldOfDefault` sits with the table in `whatASaveAsks.ts` (the defaults section of the form and
`defaultsKeepingOnlyFields` both import it), and `NOBODY` stays in the component (it seeds state there). No helper split
was needed for Biome.

| Measure | Before | After |
|---|---|---|
| Test count, the five files (defaults, fieldChange, UpdateQuestion, plus the two new) | 99 | 164 (31 + 29 + 25 + 68 + 11) |
| Ids of the four-file preserved set | 137 | 117 (20 missing, all in the table below; 117 unchanged; list saved as `slice-08-ids-after.txt`, a set diff against `slice-08-ids-before.txt` shows only those 20 removed and none added) |
| Ids declared to leave | 25 | 20 (five stay, see "Deviations") |
| `expect(` removed / added, `EditConnection.defaults.test.tsx` | n/a | 8 / 0 |
| `expect(` removed / added, `EditConnection.fieldChange.test.tsx` | n/a | 2 / 0 |
| `expect(` removed / added, `UpdateQuestion.test.tsx` | n/a | 0 / 1 (the added "fields" case table has one `expect(`; the pin case for 3 Portfolios is a row of the existing table) |
| Line coverage, both directories | 90.51 | 90.54 |
| Branch coverage, both directories | 82.50 | 82.72 |
| Statements / functions, both directories | 89.59 / 87.50 | 89.62 / 87.56 |
| File time, the touched files (indicative (not quiet); summed test seconds, 5 alternating pairs) | 92.14 s | 72.49 s (-21.3 %) |
| Allow-list length (`slowTests.ts`) | 13 | 13 (no entry belongs to these files; file untouched) |

Time: five alternating before/after pairs back to back, `LIGHTHOUSE_TEST_WORKERS=2` both sides, only the touched test
files (before = `EditConnection.defaults`, `EditConnection.fieldChange` and `UpdateQuestion` tests from a temporary
worktree of `608f3017f`; after = the same three plus the two new files), the sum of per-test durations from the JSON report.
Indicative (not quiet): load average 3 to 6.5 over the ten runs (pair 1 before was the highest, 6.5); no pair ran under
clearly higher load than the rest, so all five are in the sum. Measured again after the review revision restored three
renders. The first measurement, before the restores (164 tests were then 160), is kept for the record: pairs 1 to 4 gave
71.76 s to 55.56 s (-22.6 %); its pair 5 "after" ran while another session pushed the load to 12 (25.49 s) and set the
sum to 90.32 s against 81.05 s (-10.3 %), which is why it was measured again.

| Pair | Before (s, 99 tests) | After (s, 164 tests) |
|---|---|---|
| 1 | 18.30 | 14.62 |
| 2 | 18.79 | 14.67 |
| 3 | 18.39 | 13.94 |
| 4 | 18.11 | 15.01 |
| 5 | 18.55 | 14.25 |
| Sum | 92.14 | 72.49 |

The saving is smaller than the 6 s of 26 s the declaration expected for the same reason as the spread: the two edited page
files carried 15 and 10 ids at 0.18 to 0.30 s, and a render per id was all they cost; the whole wizard (7 s) and the 31 + 27
wiring tests stay.

Coverage: directory run as in the "Coverage (before)" section, 17 files, 340 tests passed. `whatASaveAsks.ts` is at 96 %
statements (uncovered: lines 55 and 84, the `label` of `estimationForTeams` and `estimationForPortfolios`, which the
form's Estimation rows never ask for).

### Deviations

1. Five of the declared 25 ids stay as renders. Two because breaking the component's call of the extracted function left
   every other test green until they were put back (see the mutation table):
   - `... a followed Estimation default left as it was asks nothing when another default changes` (row 11): the only test
     that fails when `changedEstimationDefaultsWithFollowers` is called without the loaded defaults (mutation 6).
   - `... a changed featureOwnerFieldForPortfolios default 2 Portfolios follow asks whether to update them` (row 4): the
     `it.each` of four defaults shrank to its one `featureOwnerFieldForPortfolios` row, which is the only one that fails when
     `whyUpdate` is given another word for Feature (mutation 10). The expect line is unchanged; three of its four rows left.
   Three more after the review, with their original ids and bodies: the review found that the cancelled add and the
   added-and-removed field were the only tests that opened the Add Field dialog on that path, so no test covered a cancelled add
   or a deleted new row (the editor state, not an extracted function):
   - `... a field Priya started to add and then cancelled asks no question` and `... a field Priya added and removed again
     before saving asks no question` (rows 7 and 8). With a cancelled add that keeps the field (`handleCancelEdit` calling
     `onChange([...fields, editingField])`) the first fails and only it; with a delete that leaves the new row
     (`f.id !== fieldToDelete.id || f.id < 0`) the second fails and only it. The file was restored after each.
   - `... the explanation names the Teams and Portfolios in the words this instance uses` (row 6), restored with its
     `SquadsAndPrograms` words: it costs about 0.2 s, and it is the only page test with the renamed words; the same wording is
     also pinned in `UpdateQuestion.test.tsx` with a mocked terminology hook.
2. The other rows of the declaration hold unchanged. Function cases are more numerous than the "about 40" expected (68 + 11),
   because every default of the table is asserted by name; they add about 0.1 s.

### Mutation table (wiring)

Each call site in `ModifyConnectionSettings.tsx` was broken once (one replacement, in the committed file), the six
`ModifyConnectionSettings`/`EditConnection`/wizard test files run, the file restored with `git checkout`. After the last one
`git diff -- ModifyConnectionSettings.tsx` was empty. "Failed" is the count of failing tests; the first is named.

| # | Call site broken | Failed | First failing test (surviving id) |
|---|---|---|---|
| 0 | fields flag of `whyUpdate` always false | 23 | `defaults ... an Additional Field and a followed default changed in one save ask only the Additional Fields question` |
| 1 | fields flag always true | 12 | `defaults ... a save that changes neither a default nor a field sends the defaults as they were and asks nothing` |
| 2 | fields flag compares current with itself | 23 | same as 0 |
| 3 | `changedDefaultsToUpdateFollowersOf` without the loaded defaults | 1 | `... a save that changes neither a default nor a field ...` |
| 4 | same, current defaults replaced by the loaded | 6 | `... asks whether to update the 3 Teams that follow it, and Save and update says so` |
| 5 | same, usage replaced by nobody | 6 | same as 4 |
| 6 | `changedEstimationDefaultsWithFollowers` without the loaded defaults | 0 before row 11 was kept; 1 after | `... a followed Estimation default left as it was asks nothing when another default changes` |
| 7 | same, current defaults replaced by the loaded | 1 | `the Estimation default a changed Estimation default 4 Teams follow asks only to save ...` |
| 8 | same, usage replaced by nobody | 1 | same as 7 |
| 9 | `whyUpdate` given nobody as usage | 30 | `... asks whether to update the 3 Teams ...` |
| 10 | `whyUpdate` given another word for Feature | 0 before row 4 was kept; 1 after | `... a changed featureOwnerFieldForPortfolios default 2 Portfolios follow asks ...` |
| 11 | the two defaults arguments swapped (Estimation for the updating list) | 7 | `... asks whether to update the 3 Teams ...` |
| 12 | the question never asked | 31 | `saving a changed default removing the field a default names clears that default in the save ...` |
| 13 | answer carries `additionalFieldsChanged` always false | 4 | `... an Additional Field and a followed default changed ...` |
| 14 | answer carries it always true | 4 | `... asks whether to update the 3 Teams ...` |
| 15 | answer compares current with itself | 4 | `... an Additional Field and a followed default changed ...` |
| 16 | `defaultsKeepingOnlyFields` not called (defaults left) | 1 | `saving a changed default removing the field a default names clears that default in the save, and leaves the others as they were` |
| 17 | called with the stale field list | 1 | same as 16 |
| 18 | called with an empty field list | 1 | same as 16 |
| 19 | section row filter by group inverted | 20 | `ModifyConnectionSettings.directionTips ... on a Jira connection ...` |
| 20 | section row label with another word for Feature | 3 | `the section on the connection page a Jira connection shows a Defaults for Teams and Portfolios section ...` |
| 21 | section reads the chosen field without the Estimation rule | 2 | `the Estimation default a changed Estimation default 4 Teams follow asks only to save ...` |

Not run: swapping the two arguments of `fieldsChangedSince` (the comparison is symmetric, so the result is the same).
Every other call-site argument that matters is in the table; none is caught by nothing.

### Removed ids

| Removed id (file :: suffix) | Moved to | Assertion kept as |
|---|---|---|
| defaults :: ... a changed parentOverrideForPortfolios default 2 Portfolios follow asks whether to update them | `whatASaveAsks.test.ts` `whyUpdate ... %s names its label and what changes` | label `Parent Override Field`, change `parents`, 2 Portfolios; sentence in `UpdateQuestion.test.tsx` "the Teams' and the Portfolios' Parent Override defaults" |
| defaults :: ... a changed dependencyFieldForPortfolios default ... | same test, `dependencyFieldForPortfolios` | `Dependency Field`, `dependencies`, 2 Portfolios; sentence in the `UpdateQuestion` Dependency case |
| defaults :: ... a changed sizeEstimateFieldForPortfolios default ... | same test, `sizeEstimateFieldForPortfolios` | `Size Estimate Field`, `sizes`, 2 Portfolios; sentence in the `UpdateQuestion` Size Estimate case |
| defaults :: ... a changed featureOwnerFieldForPortfolios default ... | stays (row of the shrunk `it.each`) and in the same function test | see Deviations; also `Feature Owner Field`, `owners`, with the renamed-Feature label asserted |
| defaults :: ... 3 Portfolios following both the Parent Override Field and Dependency Field defaults, both changed, are counted once | `whatASaveAsks.test.ts` `counts 3 Portfolios following both %s and %s once` | portfolios 3, teams 0, from `followerGroups` |
| defaults :: ... both the Dependency Field and Size Estimate Field ... | same, second pair | same |
| defaults :: ... both the Size Estimate Field and Feature Owner Field ... | same, third pair | same; the joined sentence is in `UpdateQuestion.test.tsx` |
| defaults :: ... a changed Dependency default says its followers' dependencies change once updated | `whatASaveAsks.test.ts` `leaves out a group that follows another default only` | portfolios 2 (not 3), label and change from the table |
| defaults :: ... Portfolios following a changed default and another one left as it was are all asked about | `whatASaveAsks.test.ts` `counts a whole group when it follows a changed default and another one left as it was` | portfolios 3 |
| defaults :: ... a changed default nobody follows saves without asking, though other defaults have followers | `whatASaveAsks.test.ts` `changedDefaultsToUpdateFollowersOf ... leaves out a changed default nobody follows, though others have followers` | empty list, so `whyUpdate` is null; the wiring "default nobody follows saves without asking" stays as a render |
| defaults :: ... a followed Estimation default left as it was asks nothing when another default changes | stays (mutation 6); function side in `changedEstimationDefaultsWithFollowers ... leaves out a followed Estimation default that stayed as it was` | see Deviations |
| defaults :: the Estimation default a changed Estimation default nobody follows saves without asking anything | `whatASaveAsks.test.ts` `leaves out a changed Estimation default nobody follows` | empty list |
| defaults :: the Estimation default a changed Portfolios' Estimation default asks about the Portfolios that follow it | `whatASaveAsks.test.ts` `asks about the Portfolios that follow a changed Portfolios' Estimation default` | `because: estimation`, portfolios 2, defaults 1; the sentence is in `UpdateQuestion.test.tsx` "one Estimation default" |
| defaults :: the Estimation default changing only the unit of a followed Estimation default asks before saving | `whatASaveAsks.test.ts` `sees another unit as a change` (also field, non-numeric flag, category values, removed, null) | the Estimation default listed |
| defaults :: saving a changed default removing the field the Teams' Estimation default reads clears that default in the save | `defaultsKeepingOnlyFields.test.ts` `clears an Estimation default when the field inside its bundle is gone` (and `estimationForPortfolios`) | `estimationForTeams: null`, the others as they were; the same call is proven by the form test that removes `Caused By` |
| fieldChange :: ... with one of each the question reads as such | `UpdateQuestion.test.tsx` `when an Additional Field changed` case `one of each`; `whatASaveAsks.test.ts` `whyUpdate ... asks about everyone on a connection used by {teams: 1, portfolios: 1}` | title `Update 1 Team and 1 Portfolio?` with the default words |
| fieldChange :: ... with Squads and Programs ... | same file, case `Squads and Programs` | title `Update 4 Squads and 2 Programs?` and the description in the renamed words |
| fieldChange :: ... with one Squad and one Program ... | same, case `one Squad and one Program` | `Update 1 Squad and 1 Program?` |
| fieldChange :: ... with no Portfolio ... | same, case `no Portfolio`; usage `{3, 0}` in `whyUpdate` | `Update 3 Teams and 0 Portfolios?` |
| fieldChange :: ... with no Team ... | same, case `no Team`; usage `{0, 2}` in `whyUpdate` | `Update 0 Teams and 2 Portfolios?` |
| fieldChange :: ... swapping the names and references of two fields asks the question | `fieldsChangedSince ... the names and references of two fields swapped is a change` | true; the id is part of the compared key (`... back under a new id ...`) |
| fieldChange :: ... a field change on a connection no Team or Portfolio uses is saved straight away without a question | `whyUpdate ... asks nothing on a connection no Team or Portfolio uses` | null |

`expect(` lines: `EditConnection.defaults.test.tsx` lost 8 (the loop `expect` of the `it.each` of four stays, one row is left of the
four), `EditConnection.fieldChange.test.tsx` lost 2. The one test that left without an `expect(` line of its own used the helpers
`thenTheConnectionIsSavedWith` and `thenNoQuestionIsAsked`, which stay.

Notes on what the page still proves:
- The renamed words in the question (`Squad`, `Program`, `Epic`) are pinned in `UpdateQuestion.test.tsx` with a mocked
  terminology hook, and by the one restored page test for the Additional Fields explanation; no other page test covers them.
- The Estimation "nobody follows" page path (the removed `a changed Estimation default nobody follows saves without asking
  anything`) is proven by the direct test `leaves out a changed Estimation default nobody follows` plus the shared call
  wiring: the wiring test `a default nobody follows saves without asking anything` goes through the same `whyUpdate` call, and
  mutations 7 and 8 fail the Estimation render that stays.

### Logic mutants inside the extracted files

Broken once by hand, the three function test files run (`whatASaveAsks`, `defaultsKeepingOnlyFields`, `UpdateQuestion`), file
restored after each.

| File | Mutant | Failed | First failing test |
|---|---|---|---|
| `whatASaveAsks.ts` | `followersIn` counts per changed default instead of per group | 4 | `whyUpdate ... counts 3 Portfolios following both ... once` |
| `whatASaveAsks.ts` | `sameEstimation` ignores the unit | 1 | `changedEstimationDefaultsWithFollowers sees another unit as a change` |
| `whatASaveAsks.ts` | `sameEstimation` ignores the category values | 1 | `... sees other category values as a change` |
| `whatASaveAsks.ts` | null and undefined defaults compared as different | 2 | `... counts unset becoming null as no change` |
| `whatASaveAsks.ts` | a changed field asks even when nobody uses the connection | 2 | `whyUpdate ... asks nothing on a connection no Team or Portfolio uses` |
| `whatASaveAsks.ts` | the field key leaves the id out | 2 | `fieldsChangedSince ... back under a new id ...` |
| `whatASaveAsks.ts` | a default with no followers counted (`> 0` to `>= 0`) | 3 | `... leaves out a changed default nobody follows, though others have followers` |
| `defaultsKeepingOnlyFields.ts` | every default written as null | 4 | `clears a default whose field is no longer in the list and leaves the others as they were` |
| `defaultsKeepingOnlyFields.ts` | no default ever cleared | 8 | same |
| `defaultsKeepingOnlyFields.ts` | a default naming no field cleared too (`!kept.has(fieldId)` for a null) | 0 | equivalent: a null default is written back as null |

The test `leaves a default that names no field as it is` now also holds a default whose field is there, so the
"always null" mutant fails it too. Where the removed page tests asserted wording, the function tests compare the whole
result with `toEqual` (`because`, counts, `defaults`, `changes`); the single-default sentence `3 Portfolios follow the Parent
Override Field default.` is pinned in `UpdateQuestion.test.tsx`.
