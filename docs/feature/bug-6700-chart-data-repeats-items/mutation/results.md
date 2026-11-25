# Mutation testing — 6700 (several charts show the same Work Item many times)

Run 2026-11-25 against `main` @ `6240b7c9a`. Gate is 80 % kill rate on each stack touched.

| stack | scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | `extractWorkItems`, the lines the fix changed (6–16) | **100 %** | 7 | 7 | 0 | 0 | 4 s |
| Backend | — | N/A | — | — | — | — | — |

Backend: N/A, the fix changed no backend file.

Configs: `Lighthouse.Frontend/stryker.6700.frontend.json`, `Lighthouse.Frontend/vitest.mutation.6700.config.ts`
(only `workItemLookup.test.ts`, `widgetViewData.test.ts` and the oracle `widgetViewData.repeatedItems.test.ts` run).
Raw report: `stryker-6700-frontend.json`.

## Mutants

All seven were killed: the empty-input guard (condition forced true or false, its return emptied or replaced), the
whole body emptied, the de-duplication call removed, and the returned list emptied. Removing the de-duplication call
is the mutant that brings the defect back; the oracle kills it.

## Not mutated

`addItemsWithoutDuplicates`, the helper the fix now reuses, is unchanged code and was left out of `mutate`. The oracle
covers it through `extractWorkItems`.
