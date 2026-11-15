# Slice S1 - removal of picked reports: moved assertions

The scripts `measure-slice.sh`, `check-slice.sh` and `deliberate-breaks.sh` read this file by slice id. Ids are
`file :: full test name`, files relative to `Lighthouse.Frontend`.

## Measure

- Before commit: a0c6f17aa96229a5f79376d47b8da30c0548d2a0
- Page test file: src/pages/Reports/ReportsPage.removal.test.tsx
- Direct test files: src/pages/Reports/RemoveSelectedDialog.test.tsx src/pages/Reports/useBulkReportRemoval.test.tsx src/pages/Reports/useSelectionActions.test.tsx
- File-alone ratio at most: 0.60
- Combined ratio at most: 0.80
- Production allow-list: none

## Moved assertions

One row per test of the page file at the before commit. A kept test keeps its id; a removed or merged test names the
tests that now prove its rule, separated by `;;`.

| Old id | Disposition | Rule proved | New tests |
|---|---|---|---|
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once a coach removes three trial reports on two Teams and a Portfolio, and they leave the list | kept | pick three, confirm, the picks leave the list, and after Done the picks are cleared (selected count 0) | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once Escape at the confirmation closes it, removes nothing and keeps what was picked | kept | Escape and Cancel at the confirmation close the dialog, remove nothing, keep three picked and the list as before | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once a report the server did not remove is named with its reason and stays listed | kept | a report the server did not remove is named and stays listed | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once when her permissions could not be loaded, Remove is disabled and says to reload | kept | tooltip says to reload and pressing Remove asks nothing | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Picking reports changing the filter clears what was picked | kept | changing the filter clears the picks | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Picking reports sorting keeps what was picked | kept | sorting keeps the picks | |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once Cancel removes nothing | removed | Cancel removes nothing and the list is unchanged | src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once Escape at the confirmation closes it, removes nothing and keeps what was picked |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once the confirmation warns when one of them holds a Then that can no longer be rebuilt | removed | warning for one Then lost | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports warns when one of them holds a Then that can no longer be rebuilt |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once the confirmation warns in the plural when two Thens would be lost | removed | warning in the plural for two | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports warns in the plural when two Thens would be lost |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once with every Then still rebuildable the confirmation carries no warning | removed | no warning when every Then is rebuildable | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports carries no warning when every Then can still be rebuilt |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once a picked report without a Then & Now carries no warning | removed | no alert without a Then and Now, and the Remove 1 report button | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports carries no warning for a report without a Then & Now and offers to remove one report |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once with one picked report on an owner she only reads, Remove is disabled and says why | removed | disabled with the reason for one owner she only reads | src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar with one picked report on an owner she only reads, Remove is disabled and says why |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once with two picked reports on owners she only reads, the reason says they are | removed | disabled with the reason for several owners she only reads | src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar with two picked reports on owners she only reads, the reason says they are |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once when her permissions could not be loaded, pressing Remove asks nothing | removed | pressing a disabled Remove opens no dialog and sends nothing | src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar pressing a disabled Remove asks nothing ;; src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once when her permissions could not be loaded, Remove is disabled and says to reload |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once a report somebody else removed first counts as removed | removed | already-gone counts as removed in the headline | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports a report somebody else removed first counts as removed |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once when no answer arrives it says it cannot confirm what was removed | removed | cannot confirm what was removed | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports says it cannot confirm what was removed when no answer arrives |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once while the removal is under way the dialog cannot be closed | removed | progress shown and Escape does not close while under way | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports while the removal is under way it shows progress and cannot be closed |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once the result counts only the reports removed, and names only the one that was not | removed | headline counts only removed, only the failed is named with its reason | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports the result counts only the reports removed and names only the one that was not, with its reason |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once Escape on the result closes it and clears what was picked | removed | Escape on the result is done; the picks are cleared after done | src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports Escape on the result is the reader being done ;; src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once a coach removes three trial reports on two Teams and a Portfolio, and they leave the list |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once each report the removal took, or found already gone, is forgotten, and one it could not remove is kept | removed | removed and already-gone reports are forgotten, a not-removed one is kept | src/pages/Reports/useBulkReportRemoval.test.tsx :: Removing several reports in one request forgets each report removed or found already gone, and keeps one it could not remove |
| src/pages/Reports/ReportsPage.removal.test.tsx :: Remove several reports at once the report forgotten is the one removed, not one picked before it | removed | the forgotten report is the one removed | src/pages/Reports/useBulkReportRemoval.test.tsx :: Removing several reports in one request forgets the report that was removed, not one picked before it |

## Added ids

- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports lists every picked report with its owner, in the order picked
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports warns when one of them holds a Then that can no longer be rebuilt
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports warns in the plural when two Thens would be lost
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports carries no warning when every Then can still be rebuilt
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports carries no warning for a report without a Then & Now and offers to remove one report
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports Remove sends the picked ids in order and says how many reports were removed
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports a report somebody else removed first counts as removed
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports says it cannot confirm what was removed when no answer arrives
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports while the removal is under way it shows progress and cannot be closed
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports the result counts only the reports removed and names only the one that was not, with its reason
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports Escape at the confirmation asks to cancel and nothing else
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports Escape on the result is the reader being done
- src/pages/Reports/RemoveSelectedDialog.test.tsx :: Confirming the removal of picked reports Done on the result is the reader being done
- src/pages/Reports/useBulkReportRemoval.test.tsx :: Removing several reports in one request forgets each report removed or found already gone, and keeps one it could not remove
- src/pages/Reports/useBulkReportRemoval.test.tsx :: Removing several reports in one request forgets the report that was removed, not one picked before it
- src/pages/Reports/useBulkReportRemoval.test.tsx :: Removing several reports in one request reads every report list again, also when no answer arrives
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar with one picked report on an owner she only reads, Remove is disabled and says why
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar with two picked reports on owners she only reads, the reason says they are
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar when her permissions could not be loaded, Remove is disabled and says to reload
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar while her permissions are loading, Remove is disabled
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar pressing an enabled Remove opens the confirmation listing the picked reports
- src/pages/Reports/useSelectionActions.test.tsx :: Removing the picked reports from the toolbar pressing a disabled Remove asks nothing
