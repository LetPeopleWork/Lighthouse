# Story 6650, final frontend mutation record

Code at c1af7db8c. StrykerJS, vitest runner, `coverageAnalysis: perTest`, incremental, concurrency 4, default sandbox. Config: `stryker.6650.final.frontend.json` (run from `Lighthouse.Frontend`; the git-ignored `vitest.stryker.6650.ts` includes DataGrid, DataOverviewTable, FeatureListDataGrid and pages/Common/Reports tests; `tsconfigFile` bypass as in S3/S4).

Mutated: code changed after the S4 run (`git diff -U0 19195d97d HEAD`), lines of the new side.
DataGridBase.tsx 275-356, 419-437, 447, 499, 561-564, 626, 685, 694; columnFit.ts 20-22 (no mutants: a constant); DataOverviewTable.tsx 22, 28, 41-42, 101-106, 220, 225-235, 254; RowActionsMenu.tsx and RowActionsCell.tsx whole files; columns.tsx 242-251, 258, 263; ReportList.tsx 14 (an import, no mutants). 190 mutants.

## Score

| File | Mutants | Round 1 | After kill round |
|---|---|---|---|
| DataGridBase.tsx | 66 | 42.42 | 78.79 |
| DataOverviewTable.tsx | 13 | 84.62 | 84.62 |
| RowActionsCell.tsx | 37 | 62.16 | 100.00 |
| RowActionsMenu.tsx | 64 | 71.88 | 85.94 |
| columns.tsx | 10 | 70.00 | 90.00 |
| All | 190 | 60.53 | 86.32 |

Overall passes 80. DataGridBase.tsx stays 1.2 points under 80 per file: a last test (the table's column order when the grid is gone) was tried and dropped, because nothing is persisted after unmount, so it could not be made to pass.

## Survivors (all kept, none killed further)

| Where | Mutant | Disposition |
|---|---|---|
| DataGridBase 281 | `if (!api)` to false, and its arrow (not covered) | Real but not reachable: the order callback only runs while the grid is mounted. Left. |
| DataGridBase 323, 327, 314 | `if (timeout.current)` forced true (resize save and unmount cleanup) | Equivalent: `clearTimeout(null)` does nothing. |
| DataGridBase 336, 338 | catch block of the delayed width save, its message (not covered) | Real: needs the store to throw inside the save; the store catches first. Left. |
| DataGridBase 316, 352, 347, 426, 437 | dependency arrays changed | Equivalent for behaviour: callbacks and effects do not read changing values. |
| DataGridBase 424, 431 | `if (pending.current)` forced true | Equivalent: `clearTimeout(null)` does nothing. |
| DataOverviewTable 103, 104 | clone allowed for any api; dependency array | Equivalent: `api` is typed to teams or portfolios; array is a stale-closure only mutant. |
| RowActionsMenu 29-32 (not covered) | `focusWithoutScrolling` body | Real: the mouse-down handler is not driven by a test. Left. |
| RowActionsMenu 35 | three strings of the arrow-key set | Real: survives only on a per-key basis; each key has a test, one mutant per key kept alive by the other three `it.each` rows sharing the set. Left. |
| RowActionsMenu 104 | `button.current?.focus` to `.current.focus` | Equivalent: the ref is set while the menu is open. |
| columns.tsx 263 | `?? []` array replaced (not covered) | Equivalent: `dependsOn` is always an array. |

## Tests added

columns.dependsOn.test.tsx: the tooltip text (2). DataGridBase.overflow.test.tsx: unmount before a drag is saved, a drag without a width, Reset layout after a drag, two focus scrolls, unmounting before the focus scroll, three column-order fallbacks (the file's grid mock now records the props passed to the grid, since the community grid raises no order event). DataOverviewTable.test.tsx: RowActionsMenu events and keys (9), RowActionsCell layout, links, sizes, hover (5). Production code and existing assertions unchanged. Checks: the four folders' vitest green (64 files, 1154 tests), `pnpm build` exit 0.
