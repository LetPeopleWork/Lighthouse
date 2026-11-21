# Story 6650, S3 frontend mutation record and deliberate breaks

Code frozen at e6305dc11. StrykerJS 10.1.1, vitest runner, `coverageAnalysis: perTest`, incremental, concurrency 4, default sandbox (not inPlace).
Config: `stryker.6650.s3.frontend.json` (run from `Lighthouse.Frontend`; the git-ignored `vitest.stryker.6650.ts` copy of `vitest.stryker.mutation.ts` has `include` set to `src/components/Common/DataOverviewTable/**/*.test.{ts,tsx}`).
`tsconfigFile` points at a file that does not exist: Stryker's sandbox tsconfig rewrite crashes under this TypeScript (`ts.parseConfigFileTextToJson is not a function`), and earlier records use the same bypass.
Mutated: `DataOverviewTable.tsx` lines 45-46, 112, 131, 148, 177, 193, 209, 217, 220 (the S3 lines of `git diff da47e8eb2 HEAD -U0`).

## Score

| File | Mutants | Killed | Survived | No coverage | Timeout | Errors | Score |
|---|---|---|---|---|---|---|---|
| DataOverviewTable.tsx | 14 | 14 | 0 | 0 | 0 | 0 | 100.00 |

No survivors, so no kill round and no tests added. Production code and tests unchanged.

## Deliberate breaks

Each break was one edit to `DataOverviewTable.tsx`, one run of `npx vitest run src/components/Common/DataOverviewTable/DataOverviewTable.test.tsx -t "<test>"` (from `Lighthouse.Frontend`), then `git checkout --` of that file; `git status` afterwards showed no tracked change, and the same test passed again.

| Check | Break applied | Failing message | Restored |
|---|---|---|---|
| Actions pinned and last (line 840) | deleted `fit: { kind: "actions" }` | `the Teams Actions header must carry the pinned class, or Delete scrolls out of view: expected false to be true` | yes, green |
| Exposes overflow and description (892) | `DataGridBase` import replaced by MUI `DataGrid` (the Overview takes no prop that disables overflow, so it stops using the shared grid) | `the table must mark that columns are hidden on its right while it is scrolled to its left edge: expected undefined to be 'right'` | yes, green |
| Focus tooltip on a cut name (927) | name column `fit` changed to `{ kind: "content" }` | `Unable to find role="tooltip"` | yes, green |
| No cue when the table fits (909) | same as the overflow break (shared grid replaced) | `a table that fits must not claim hidden columns: expected undefined to be 'none'` | yes, green |
| Same actions width on phone and desktop (939) | `width: isMobile ? 160 : 200` | `the actions column keeps one width at every screen size: expected 160 to be 200` | yes, green |

Notes:
- The two overflow checks cannot be broken through a column definition. A first attempt for "fits" (name `minWidth` 5000) left that test green: jsdom has no layout, so the test's measured box, not the column widths, decides the cue. Column widths are therefore unproven for the cue at Overview level; the cue logic itself is owned and mutation-tested by the S2 record (`DataGridBase.overflow.test.tsx`, `horizontalOverflow.test.ts`, which assert `data-overflow` and the shadows).
- The Overview tests prove the wiring (the Overview renders through the shared grid with its pinned actions); the S2 tests prove the behaviour.

## Browser checks added after the slice review (OverviewTableFit.spec.ts, DataGridView.ts)

- Tags column minimum of 120 px, polled at 1280 px and at 375 px: green against current production. Break to run: set the Tags column to `fit: { kind: "text", minWidth: 40 }` in DataOverviewTable.tsx; the Tags poll must then fail. Not run in this pass.
- The cue reading also fails when an aria-hidden, positioned, text-free overlay of ours inside the overflow root lacks data-side (the grid's own scrollbar track is excluded). Green today; break to run: drop `data-side` from the left cue in DataGridBase.tsx. Not run in this pass.
- A Name focused at desktop width and the window narrowed to 375 px: RED, a real defect (see the report to the root); the Name cell (x 53..213) stays under the pinned actions column (starts at x 122) because no focus event follows the resize.

## Deliberate breaks, phone actions menu (decision 7)

Each break was one edit, one run of `pnpm exec vitest run src/components/Common/DataOverviewTable/DataOverviewTable.test.tsx -t "<test>"` from `Lighthouse.Frontend`, then the file was restored from a copy (`cmp` identical) and the desktop test passed again.

| Check | Break applied | Failing message | Restored |
|---|---|---|---|
| keeps the four action icons and no menu button on a desktop | the actions cell renders the menu whatever the width (`isMobile ?` became `true ?`) | `at desktop width the icons have room, so there is no menu button` | yes, green |
| the menu leaves out Clone when the person may edit and delete but not clone | dropped the `canCloneRow` filter in the menu | `the menu must list exactly the actions the person may use ... expected [ Array(4) ] to deeply equal [ 'Details', 'Edit', 'Delete' ]` | yes, green |
| opens the Delete confirmation for the row when Delete is chosen from the menu | Delete in the menu does nothing | `choosing Delete must ask for the confirmation of this very row, exactly once` | yes, green |
