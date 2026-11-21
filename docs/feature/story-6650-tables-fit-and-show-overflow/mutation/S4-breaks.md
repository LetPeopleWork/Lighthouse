# S4 deliberate breaks

Each break was applied alone, only the named tests were run, and the file was restored afterwards (the file matched its pre-break content again).

## (a) TruncatedCell: remove `cut &&` from the tooltip `open` condition

| Break | Tests run | Result |
|---|---|---|
| `open={cut && (hovered \|\| focusedWithin \|\| hasFocus)}` becomes `open={(hovered \|\| focusedWithin \|\| hasFocus)}` | every "opens no tooltip" check, all 7 files (`vitest -t "opens no tooltip"`) | **NOT caught: 9 passed.** Every check stayed green. |
| Control: `open={true}` | Connections "opens no tooltip over a connection name that fits its column" | fails, so the check can see a tooltip that is open at mount |

Diagnosis: with `cut &&` removed the hover still reaches `onOpen` (a probe logged it), but the `setHovered(true)` re-render
is not flushed before the check reads `queryByRole("tooltip")` in `thenHoveringOverItOpensNoTooltip`
(`src/tests/SharedGridFitKit.tsx`; fake timers advance 300 ms but React's scheduler flush runs on a real macrotask). The negative
checks therefore cannot see a tooltip that opens on hover for text that fits. Owner of the defect: oracle (the kit helper must wait
for the re-render, for example by awaiting a real macrotask or using `act`, before asserting absence).

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
