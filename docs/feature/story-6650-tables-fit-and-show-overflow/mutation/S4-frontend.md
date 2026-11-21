# Story 6650, S4 frontend mutation record

Code at 38bbee713 (plus one added test file change). StrykerJS 10.1.1, vitest runner, `coverageAnalysis: perTest`, incremental, concurrency 4, default sandbox (not inPlace).
Config: `stryker.6650.s4.frontend.json` (run from `Lighthouse.Frontend`; the git-ignored `vitest.stryker.6650.ts` has `include` set to the five S4 test folders: FeatureListDataGrid, ParentWorkItemCell, WorkItemsDialog, Settings/Connections, Teams/Detail/Refinement).
`tsconfigFile` points at a file that does not exist, the same bypass as the S3 record (Stryker's tsconfig rewrite crashes under this TypeScript).
Mutated: the lines S4 changed in production (`git diff 3533c18cb HEAD -U0`): columns.tsx 25-27, 117, 261, 263-264; ParentWorkItemCell.tsx 11-24, 30, 44; WorkItemsDialog.tsx 253; ConnectionsSettings.tsx 136; refinementColumns.tsx 8-10, 120, 122-123.

## Score

| File | Mutants | Round 1 | Round 2 (final) |
|---|---|---|---|
| columns.tsx | 5 | 100.00 | 100.00 |
| ParentWorkItemCell.tsx | 7 | 57.14 (3 survived) | 100.00 |
| WorkItemsDialog.tsx | 2 | 100.00 | 100.00 |
| ConnectionsSettings.tsx | 2 | 100.00 | 100.00 |
| refinementColumns.tsx | 3 | 100.00 | 100.00 |
| All | 19 | 84.21 | 100.00 |

## Survivors of round 1 and disposition

| Mutant (ParentWorkItemCell.tsx) | Disposition |
|---|---|
| `if (!parentReference)` -> `if (false)` in `parentWorkItemText` | Real. The component guards the same case first, so only a direct call of the exported helper reaches it. Killed by a new test of `parentWorkItemText` with null, undefined and "" (map holding a "" key). |
| `sx={{ color: "text.secondary" }}` -> `sx={{}}` | Real. Killed by a new test: "No Parent" renders with `color: rgba(0, 0, 0, 0.6)`. |
| `"text.secondary"` -> `""` | Real. Same test. |

No equivalent mutants. Tests added: two, in `ParentWorkItemCell.test.tsx`. Production code and existing assertions unchanged.
Checks before commit: the five folders' vitest green (57 files, 923 tests), `pnpm build` exit 0.
