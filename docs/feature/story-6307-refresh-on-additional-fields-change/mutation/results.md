# Mutation testing — 6307 (Refresh on Additional Fields change)

Run 2026-10-16 against `main` @ `1c4062d66` (local commits, not yet pushed). Gate is 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 66.52 % | 230 | 153 | 77 | 5 m 07 s |
| Backend, whole files, after the kill round | 69.13 % | 230 | 159 | 71 | 3 m 39 s (narrowed) |
| **Backend, this story's changed lines, after the kill round** | **100 %** | 26 | 26 | 0 | — |
| Frontend (StrykerJS 10.0.0), first run | 86.52 % | 89 | 77 | 12 | 8 m 31 s |
| **Frontend, after the kill round** | **89.89 %** | 89 | 80 | 9 | 2 m 41 s (incremental) |

Configs: `stryker.6307.backend.json` (the full run), `stryker.6307.backend.kill.json` (the narrowed
re-check of two files), `stryker.6307.frontend.json`, `vitest.stryker.6307.config.ts`.

## Backend

Stryker.NET mutates whole files, and most of the files this story touched are older controllers and the
PostHog publisher, where the story added a line or two. The whole-file score is dominated by survivors
in code the story did not change, so the gate is judged on the story's changed lines: every mutant
whose span touches a line added or changed in `git diff 74d9bd6a9..HEAD`, and every mutant of a file
the story created. Before the kill round that was 20 of 26 (76.92 %); after it, 26 of 26.

| file | whole-file score (first run → after kill round) | story-line mutants | story-line survivors (first run → after) |
| --- | --- | --- | --- |
| `API/WorkTrackingSystemConnectionController.cs` | 59.55 % → 64.04 % | 15 | 4 → 0 |
| `Services/Implementation/WorkTrackingSystemConnectionUsage.cs` (new) | 0 % → 100 % | 2 | 2 → 0 |
| `API/WorkTrackingSystemConnectionsController.cs` | 75.51 % | 2 | 0 |
| `API/DTO/WorkTrackingSystemConnectionDto.cs` | 62.50 % | 1 | 0 |
| `Models/UsageData/UsageDataEventShapes.cs` | 100 % | 2 | 0 |
| `Services/Implementation/BackgroundServices/Update/ConnectionFetchShapeChangedUpdateTriggerHandler.cs` (new) | 100 % | 2 | 0 |
| `Services/Implementation/WorkItems/FetchFingerprint.cs` | 100 % | 2 | 0 |
| `API/UsageDataController.cs` | 40.00 % | 0 testable | — |
| `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs` | 70.00 % | 0 testable | — |

Unit tests only. The filter leaves out both of this story's acceptance suites
(`API.Integration.ConnectionFieldChange` and `Integration.UsageData.WorkTrackingSystemFieldsChangedUsageEventTests`),
which boot the whole app. The first-run survivors that only those suites reached are closed below with
unit tests. The full run took 5 minutes for 191 tested mutants.

The 71 survivors and no-coverage mutants left in the whole-file figures all sit on lines this story did
not change: the predefined-field and option merging in `WorkTrackingSystemConnectionController`, the
secret-option handling in `WorkTrackingSystemConnectionsController`, the collector-key fallback and the
send-failure logging in `PostHogUsageDataPublisher`, the consent-token checks in `UsageDataController`
and three display strings in the connection DTO. They predate the story and are not triaged here.

### Closed by this pass

- `WorkTrackingSystemConnectionController.cs:159` and `:161`: `||` to `&&` in the "nothing to start"
  check, and removing its `return`. Two new controller tests: a field added with "leave for later"
  publishes nothing, and a save that only renames the connection publishes nothing.
- `WorkTrackingSystemConnectionController.cs:184` and `:185` (no coverage): `==` to `!=` in the
  Team and Portfolio look-ups. The existing tests stubbed the repositories without applying the
  predicate. The new test "field added publishes the Teams and Portfolios on this connection only" gives
  the repositories owners on this connection and on another one, and pins the published ids.
- `WorkTrackingSystemConnectionUsage.cs:14` and `:21` (no coverage): `Union` to `Intersect`, and the
  `_ => true` predicate to `false`. The new `WorkTrackingSystemConnectionUsageTest` counts Teams and
  Portfolios per connection, including one connection with only Teams and one with only Portfolios.

All six were confirmed killed by the narrowed run (`stryker.6307.backend.kill.json`, 3 m 39 s).

### Accepted survivors (this story's lines)

None.

### No testable mutant

- `UsageDataController.cs:200`, the refusal of an `updates` value outside the list. Every mutant of
  the validation condition is a compile error, because its later `is not { } offset` patterns must be
  definitely assigned, so Stryker cannot test any part of it. This holds for the older enum checks beside
  it as well. The acceptance suite's unknown name (`"Soon"`) is refused by the JSON reader before this
  check runs, so an out-of-range number (`"updates":7`) was added to the same refusal test after the run:
  with the `updates` check deleted that case fails (1 of 12), with it restored all 12 pass.
- `PostHogUsageDataPublisher.cs:208` and `:276`: passing `Updates?.ToString()` along and declaring the
  `updates` property. Stryker has no mutator for either.
- The new log call in `ConnectionFetchShapeChangedUpdateTriggerHandler` and the `Count` arguments it
  logs compile to errors when mutated.

### Not mutated

- `Program.cs`: one DI registration.
- `UsageDataEventBatchDto.cs`, `UsageDataEventReported.cs` and `ConnectionFetchShapeChanged.cs`: records
  with no logic. `UsageDataEventName.cs` and `UsageDataUpdates.cs`: enums.
  `IWorkTrackingSystemConnectionUsage.cs`: an interface plus a record with one static instance.

## Frontend

Line ranges of the story's change only (`git diff -U0 74d9bd6a9..HEAD`), `coverageAnalysis: perTest`
with `--incremental`. Copy `vitest.stryker.6307.config.ts` into `Lighthouse.Frontend/` before a run,
then from there: `pnpm exec stryker run ../docs/feature/story-6307-refresh-on-additional-fields-change/mutation/stryker.6307.frontend.json --incremental`.

| file | mutants | killed | survived | no cov. | score |
| --- | --- | --- | --- | --- | --- |
| `components/Common/Connection/ModifyConnectionSettings.tsx` | 71 | 62 | 7 | 2 | 87.32 % |
| `components/Common/ActionButton/ActionButton.tsx` | 6 | 6 | 0 | 0 | 100 % |
| `services/Api/WorkTrackingSystemService.ts` | 8 | 8 | 0 | 0 | 100 % |
| `pages/Connections/Edit/EditConnection.tsx` | 4 | 4 | 0 | 0 | 100 % |

No mutant on the changed lines of `ValidationActions.tsx` (the new `await onSave()` sits inside a block
that also spans an unchanged line), `UsageData.ts` and `UsageDataService.ts` (`as const` literals,
which StrykerJS does not mutate), or `WorkTrackingSystemConnection.ts` (a plain assignment).
`usageDataBuffer.ts` and `usageDataReporter.ts` changed types only, and `AdditionalFieldsEditor.tsx`
only lost lines and had a comment reworded; none of them was put in `mutate`.

### Closed by this pass

- `ModifyConnectionSettings.tsx:593-594`, three mutants of the conflict check in `save` (whole
  condition `true`, `&&` to `||`, code check `true`). The new spec "does not blame someone else when
  the save fails for another reason" fails a save with a non-conflict `ApiError` and pins that the
  "changed by someone else" copy does not appear.

### Accepted survivors

- `:116`, `:120`, `:121` in `whatTheFieldsLoad`: dropping the `sort`, its comparator, or the `"\n"`
  join. The Additional Fields editor edits a field in place and appends new ones with a fresh id, so
  the loaded and current lists can never hold the same ids in a different order; and two JSON-encoded
  triples cannot run together into a different list. Equivalent through the UI.
- `:128`, `NOBODY` as `{}`: `isUsed({})` is `NaN > 0`, which is false like `0 > 0`, and the question
  is never opened for an unused connection. Equivalent.
- `:256`, the initial `loadedFields` as a non-empty list: only read in edit mode, where loading the
  connection overwrites it before Save can be pressed. Equivalent.
- `:370` (no coverage), `additionalFieldDefinitions ?? []`: the field is required on the type and the
  model and the service both default it to `[]`, so the fallback is unreachable.
- `:597`, dropping `setValidationTechnicalDetails(null)` on a conflict: `handleSave` already clears
  the technical details before validating, and nothing sets them again before the save. Equivalent.
- `:605` (one survivor, one no coverage), the `!connectionAwaitingAnswer` guard in `saveAnswering`:
  the answers are only reachable while the question is open, which is exactly while a connection is
  awaiting an answer, and both are disabled while an answer is saving. Defensive, unreachable.

`git status` was clean of mutants after both runs.
