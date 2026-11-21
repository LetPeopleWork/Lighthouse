# Story 6650, S2 frontend mutation record

Code frozen at 09c5db143. StrykerJS 10.1.1, vitest runner, `coverageAnalysis: perTest`, incremental, concurrency 4 (half the cores).
Config: `stryker.6650.s2.frontend.json` (run from `Lighthouse.Frontend`, with a copy of `vitest.stryker.mutation.ts` placed there as `vitest.stryker.6650.ts`, which git ignores). Tests run: `src/components/Common/DataGrid/**`.
Mutated: `horizontalOverflow.ts` (whole file) and `DataGridBase.tsx` at the line ranges of `git diff 448a743e6~1 HEAD -U0` (275-361, 401-402, 497-507, 515, 552-595, 597-598, 601, 604-614, 616-617, 619-623, 625-637, 639-642, 644-701). Ranges that merge S2 edits with reindented older code (column-order handler, resize handler, sx block) are in scope as instructed.

## Scores

| Run | File | Killed | Survived | No coverage | Timeout | Errors | Score |
|---|---|---|---|---|---|---|---|
| 1 (S2 oracle only) | DataGridBase.tsx | 72 | 65 | 19 | 0 | 4 | 46.15 |
| 1 | horizontalOverflow.ts | 40 | 6 | 0 | 0 | 0 | 86.96 |
| 1 | all | 112 | 71 | 19 | 0 | 4 | 55.45 |
| 2 (after kill round) | DataGridBase.tsx | 125 | 17 | 16 | 0 | 2 | 79.11 |
| 2 | horizontalOverflow.ts | 46 | 0 | 0 | 0 | 0 | 100.00 |
| 2 | all | 171 | 17 | 16 | 0 | 2 | 83.82 |

Target 80% met on the total; DataGridBase.tsx alone is 79.11, with the survivors below all justified as equivalent or unobservable.

## Tests added (kill round, existing files only)
- `DataGridBase.overflow.test.tsx`: shadow geometry and gradient direction, right shadow ends at the pinned column width and at 0 when that column is hidden, nothing beside the grid when the table fits, positioned box and dynamic row height, cell and pinned-column styles, initial sort and persisted sort, resize debounce and persistence, applied width, unreadable drag warning, focusing the actions cell does not scroll, a cell starting before the left edge scrolls back.
- `horizontalOverflow.test.ts`: hook measures on mount, "none" without a grid, unsubscribes on unmount and re-subscribes for a new grid, follows scroll and the three fit events.

## Remaining survivors and disposition

| Mutant | Disposition |
|---|---|
| 350 `parentElement?.` to `.` | Equivalent: a grid cell always has a parent row. |
| 356 `under > 0` to `>= 0`, 359 `before > 0` to `>= 0` | Equivalent: at 0 the scroll adjustment adds or subtracts 0. |
| 349 (2, RuntimeError) | Counted as errors, not scored. |
| 631:42 `typeof width === "number"` to true | Equivalent in practice: MUI always reports a numeric width on a resize. |
| 641 `if (resizeTimeoutRef.current)` to true | Equivalent: `clearTimeout(null)` does nothing. |
| 505 `useMemo` deps to `[]` | Not observable through the public port without re-rendering with a new column set; the unchanged-memo case is covered. Left as a survivor. |
| 575 `pageSizeOptions` to `[]` | Unobservable: the custom `RowRangeFooter` replaces the page-size selector. |
| 675, 678-685 (cell whiteSpace, focus outline, row hover cursor, row max-height string, empty objects), 691-694 (pinned background and border colour) | Styles jsdom resolves unreliably (`!important`, pseudo-classes, theme colours); visual-only, not asserted. Cell flex, alignment, word-wrap, row max-height object, sticky, right, z-index are asserted. |
| 16 no-coverage mutants (column-order handler 594-620, resize commit catch 654-662 inner, 359 `-=` swap now covered) | `onColumnOrderChange` is never fired by the community `DataGrid` (column reordering is a Pro feature; MUI wires no event to that prop), so the handler is unreachable code. The persist-failure catch is unreachable: `updateState` only schedules a state update and handles storage errors itself. Candidates for removal in a separate improvement item. |

Production code was not changed.
