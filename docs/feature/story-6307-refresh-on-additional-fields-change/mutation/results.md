# Mutation testing — 6307 (Refresh on Additional Fields change)

Run 2026-10-16 against `main` @ `1c4062d66` (local commits, not yet pushed). Gate is 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | _pending_ | | | | |
| Frontend (StrykerJS 10.0.0), first run | 86.52 % | 89 | 77 | 12 | 8 m 31 s |
| **Frontend, after the kill round** | **89.89 %** | 89 | 80 | 9 | 2 m 41 s (incremental) |

Configs: `stryker.6307.backend.json`, `stryker.6307.frontend.json`, `vitest.stryker.6307.config.ts`.

## Backend

_Pending._

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
