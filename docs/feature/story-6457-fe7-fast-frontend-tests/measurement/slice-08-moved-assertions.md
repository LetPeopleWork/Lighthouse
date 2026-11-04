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
files either (one zero-delay `setTimeout` flush, `theNextTick`, at `EditConnection.fieldChange.test.tsx:400`, stays).

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

Filled at close: commits, files changed since `608f3017f`, `expect(` removed/added per file, test counts, coverage
after, file time after (alternating pairs), allow-list length (unchanged at its current length: no entry belongs to
these files), and one row per removed id in the table:

| Removed id (file :: suffix) | Moved to | Assertion kept as |
|---|---|---|
| (filled at close from the 25 rows above) | | |
