# S4 deliberate breaks

Each break was applied alone, only the named tests were run, and the file was restored afterwards (the file matched its pre-break content again).

## (a) TruncatedCell: remove `cut &&` from the tooltip `open` condition

Break: `open={cut && (hovered || focusedWithin || hasFocus)}` becomes `open={hovered || focusedWithin || hasFocus}`.

Command: `pnpm exec vitest run <the 7 S4 folders> -t "opens no tooltip"` (from Lighthouse.Frontend).

Result with the break: **8 failed, 1 passed** (7 test files failed). Message of every failure: "a tooltip must open only for text the column actually cuts, never for text that fits or for a number, date or state: expected <div role="tooltip" ...> to be null".

| Failing test (file) |
|---|
| opens no tooltip over a Feature name that fits its column (FeatureGrid) |
| opens no tooltip over a Feature name that fits its column (FeatureListDataGrid) |
| opens no tooltip over a dependency list that fits its column (FeatureListDataGrid) |
| opens no tooltip over a Work Item name that fits its column (WorkItemsDialog.layout) |
| opens no tooltip over a Feature name that fits its column (ArchivedFeatureGrid) |
| opens no tooltip over a connection name that fits its column (ConnectionsSettings.list) |
| opens no tooltip over a package name that fits its column (ThirdPartyPackagesSection) |
| opens no tooltip over a Work Item name that fits its column (RefinementView) |

The one that stays green is "opens no tooltip over a state, which is never cut" (FeatureListDataGrid): the state negative is governed by the column's fit kind (a state is not a text column, so it never gets a tooltip), not by `cut`, so this break cannot reach it.

Restored with `git checkout`; `git diff --stat` shows no change under src/components/Common/DataGrid. Rerun after the restore: 9 passed, 0 failed.

## (b) Feature list Dependencies

| Break | Tests run | Failing message |
|---|---|---|
| drop `fit: { kind: "text" }` on `dependsOn` | `-t "dependency list"` in FeatureListDataGrid.test.tsx | 3 fail: "a long dependsOn text must end with an ellipsis instead of wrapping the row or being lost; mark the column as text ..."; two tooltip checks "a cut text must show its full text in a tooltip ... mark the column as text ..." |
| `valueFormatter` returns the dependency count | same | 2 fail (hover and keyboard tooltip): "the tooltip of a cut text must carry its full text, not a shortened or different text"; the cut check stays green, as it should |

## (c) Cut checks of the unchanged tables: name column set to `fit: { kind: "content" }`

Run with `-t "cuts a long"`. Message for each: "a long name text must end with an ellipsis instead of wrapping the row or being lost; mark the column as text so it shrinks and cuts".

| Table (column) | Test that failed |
|---|---|
| Feature picker (name) | FeatureGrid.test.tsx "cuts a long Feature name to its column, shows the state whole and says columns are out of sight" |
| Feature list (name) | FeatureListDataGrid.test.tsx "cuts a long Feature name and a long parent name to their columns and shows the position and the state whole" |
| Refinement (name, shares `createNameColumn`) | RefinementView.test.tsx "cuts a long Work Item name and a long parent name to their columns and shows the state and the votes whole" |
| Work Item dialog (name) | WorkItemsDialog.layout.test.tsx "cuts a long Work Item name and a long owning Team to their columns and shows the type, state and closed date whole" |
| Archived Feature (name) | ArchivedFeatureGrid.test.tsx "cuts a long Feature name to its column and says columns are out of sight" |
| Connections (name) | ConnectionsSettings.list.test.tsx "cuts a long connection name to its column and shows the type whole" |
| Third-party packages (name) | ThirdPartyPackagesSection.test.tsx "cuts a long package name to its column, shows the version whole and says columns are out of sight" |

## (d) Shown-whole checks: column set to `fit: { kind: "text", minWidth: 120 }`

Run with `-t "cuts a long"`; each failed the same test that also holds the cut check of its table (the mark touched only the named column).

| Table (column) | Test that failed |
|---|---|
| Third-party (version) | ThirdPartyPackagesSection "cuts a long package name ... shows the version whole ..." |
| Feature list (position) | FeatureListDataGrid "cuts a long Feature name and a long parent name ... shows the position and the state whole" |
| Feature list (state) | same test |
| Refinement (state) | RefinementView "cuts a long Work Item name ... shows the state and the votes whole" |
| Refinement (votes, `voteCount`) | same test |
| Connections (type, `workTrackingSystem`) | ConnectionsSettings.list "cuts a long connection name to its column and shows the type whole" |
| Work Item dialog (type) | WorkItemsDialog.layout "cuts a long Work Item name ... shows the type, state and closed date whole" |
| Work Item dialog (state) | same test |
| Work Item dialog (closed date, `dayColumn` width 130) | same test |
| Feature picker (state) | FeatureGrid "cuts a long Feature name to its column, shows the state whole ..." |

## (e) Each dependency line cut

Break: in `TruncatedCell.tsx` the `"& *": ONE_LINE_WITH_ELLIPSIS,` rule is deleted, so only the cell's outer box cuts and the dependency lines inside it carry no ellipsis of their own.

| Layer | Command | Failing test | Message |
|---|---|---|---|
| jsdom | `pnpm exec vitest run src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx` (Lighthouse.Frontend) | "cuts a long dependency list to its column" (1 failed, 32 passed) | the dependency line "DEP-1: Card payments gateway cutover" itself must stay on one line and end with an ellipsis ...: expected [ 'clip', 'nowrap', 'visible' ] to deeply equal [ 'ellipsis', 'nowrap', 'hidden' ] |
| browser | `pnpm build` in Lighthouse.Frontend, then `pnpm exec playwright test tests/specs/features/FeatureDependencies.spec.ts --project=chromium -g "narrow window"` (Lighthouse.EndToEndTests, dev backend on 5169) | "a product owner on a narrow window sees each dependency cut on its own line and reads them all in full on hover" | dependency 1 must be cut to its column with an ellipsis, on one line, inside the cell: endsWithEllipsis expected true, received false (isCut, isOneLine, endsInsideCell stayed true) |

A first version of the browser check, without the ellipsis field, stayed green under this break (the cell's outer box already clips); the ellipsis field is what makes it fail.

Restored with `git checkout`; `git diff --stat` showed no change under src/components/Common/DataGrid. After the restore: the jsdom folder run was 13 files, 161 tests green; after a rebuild the browser spec was 2 passed.

## (e) Feature list Dependencies in the browser (FeatureDependencies.spec.ts, narrow-window test)

Each break was applied alone to production, the frontend rebuilt with `pnpm build`, the test run with `playwright test tests/specs/features/FeatureDependencies.spec.ts -g "narrow window"`, and the file restored with `git checkout` (then rebuilt, 3 specs green).

| Break | Failing check | Message and difference |
|---|---|---|
| line cell `white-space: normal` (lines wrap) in TruncatedCell | `isOneLine` (and `isCut`, which a wrapped line cannot be) | "dependency 1 must be cut to its column with an ellipsis, on one line, inside the cell": isCut true -> false, isOneLine true -> false |
| line cell `width: max-content` (never cut) | `isCut` and `endsInsideCell` | same message: isCut true -> false, endsInsideCell true -> false (the text edge now runs past the cell) |
| line cell `text-overflow: clip` | `endsWithEllipsis` | same message: endsWithEllipsis true -> false |
| render only the first dependency (`slice(0, 1)`) | the count of two | "one line per dependency": expected 2, received 1 |
| tooltip text joins only the first dependency | the hover tooltip naming both | getByRole('tooltip', { name: 'Asteroid Mining Program' }) not visible |

`endsInsideCell` now compares the right edge of the text the person sees (a Range over the line's text, clipped by the line's own box) with the cell's right edge, instead of the right edge of a block element; the `max-content` break is what turns it false.
