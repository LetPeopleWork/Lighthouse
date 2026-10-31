# Mutation testing — 6458 (b) (an unfinished state mapping is never saved)

Run 2026-10-31 against the worktree at `18f5c26eb` (fix + review revision). Gate is 80 % kill rate on each
stack touched. **Backend: N/A**, no backend file changed.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **88.66 %** | 97 | 86 | 9 | 2 | 19 m 57 s |

Configs: `stryker.6458.frontend.json`, `vitest.stryker.mutation.ts` (copy it to `Lighthouse.Frontend/`
before running; vitest resolves `setupTests.ts` against the config's own folder). `mutate` is scoped to
the changed line ranges. Run with `concurrency: 1`: a first run at 2 was stopped by the host for low
memory at 65/97 and left instrumented sources behind, which were restored from git.

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| `utils/stateMappingValidation.ts` | 39 | 0 | 0 |
| `hooks/useModifySettings.ts` | 40 | 9 | 0 |
| `components/Common/Team/ModifyTeamSettings.tsx` | 2 | 0 | 1 |
| `components/Common/ProjectSettings/ModifyProjectSettings.tsx` | 3 | 0 | 1 |
| `components/Common/StateMappings/CycleTimesEditor.tsx` | 1 | 0 | 0 |
| `components/Common/StateMappings/WaitStatesEditor.tsx` | 1 | 0 | 0 |

## Closed by this pass

- `useModifySettings.ts:317` (`system?.id ?? 0` in the remembered form after a conflict reload; `&& 0` and
  `system.id`) and `:406` (the same on first load; `system.id`). Closed by tests in the kill-round commit
  `6e493d0a5` (three tests in `useModifySettings.conflict.test.ts`), each shown to fail with its mutant
  applied by hand: the revert-after-reload test fails `&& 0` with an extra save, and the two
  no-matching-connection tests fail `system.id` with a TypeError on reload and with no settings on load.

## Accepted survivors

All in lines this fix did not write; the line-range scope takes in their neighbours.

- `:270` `hasInteractedRef.current = false` after a successful save, `:306` `requestSeqRef.current += 1`
  and `:309` `hasInteractedRef.current = false` in the conflict reload: unchanged lines inside the scoped
  ranges, outside this fix's behaviour.
- `:460`/`:461` `setValidationError(null)` / `setValidationTechnicalDetails(null)` at the start of a
  manual save: unchanged lines.
- `:464` `selectedWorkTrackingSystem?.id ?? 0`: an unchanged expression that the fix only wrapped in
  `withoutDraftMappings(...)`.

## No coverage

- `ModifyTeamSettings.tsx:242` and `ModifyProjectSettings.tsx:250`, `stateMappings ?? []`: a defensive
  fallback for a field the loaded settings always carry.
