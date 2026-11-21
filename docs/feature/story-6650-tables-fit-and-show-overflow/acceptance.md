# Acceptance brief: story\-6650\-tables\-fit\-and\-show\-overflow

## Slices

### S1

##### Covers

P1

P2

```text
A Team has two reports; the person opens its Reports tab in a window 1280 px wide.
```

```text
The table is at its left edge and the Delete button of every report lies inside the table box.
```

```text
The person sees Delete on every row without scrolling.
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
Delete is inside the table box but hidden under another element or not clickable.
```

###### Obligations

None.

##### Covers

P3

```text
One report has a name longer than its column; the person looks at it and hovers it, and a keyboard user focuses a cut name drawn by its own renderer.
```

```text
The name ends with an ellipsis, a tooltip shows the full name on hover and on keyboard focus, the full text stays in the cell, and text shown whole gets no tooltip.
```

```text
The person reads the full report name without widening the column.
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach \(tooltip cases\)

```text
The tooltip opens on focus of the link but not when the grid moves cell focus onto the cell itself.
```

###### Obligations

None.

##### Covers

P4

```text
The table shrinks its columns; a number, date or actions column has any stored width, including one below its declared width.
```

```text
The Created date shows whole; a content or actions column never gets a width or minimum below its declared width; the actions column ignores a stored width and cannot be resized.
```

```text
Dates and the row action buttons are never cut.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A number\, date or action column is never cut \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
A content column whose declared width is already smaller than its real content (a long localised date) is cut; the design leaves that per table.
```

###### Obligations



##### Covers

P5

```text
The person resizes Name, then Owner; or a Name width of 900 px is stored and the page reloads; then the person presses Reset.
```

```text
Only the resized widths are stored, the Name width stays when Owner is resized, a stored Name width is used as set after reload, a text column keeps flex and its minimum when nothing is stored, and Reset clears the stored widths and returns Name to its fitted width.
```

```text
A width the person set stays theirs, and Reset gives back the fitted table.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A text column shrinks to its minimum unless the person set its width \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach \(resize and Reset cases\) \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
The stored width is kept but differs by up to one pixel from the one set.
```

###### Obligations






##### Covers

P12

```text
The columns overflow, with a stored 900 px Name at 1280 px and at 375 px; the person scrolls the table to its end; in the grid the saved column order puts the actions first, leaves them out, or there is none.
```

```text
Delete stays inside the table box with the table at its left edge and at its end, Name stays at least 160 px at 375 px, and the actions column is always ordered last.
```

```text
Delete never scrolls out of view at any width.
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach \(actions\-last order cases\)

```text
The pinned actions column covers part of the last scrolling column's content.
```

###### Obligations




### S2

##### Covers

P6

```text
A report list whose columns do not fit its box is shown scrolled to its left edge, once with the grid box stated in a test and once in the browser at 1280 px with a wide stored Name.
```

```text
The table is marked as hiding columns on the right and one decorative shadow shows, on the right side only, each shadow naming its side; in the browser the shadow sits on the right edge and ends where the pinned actions column starts.
```

```text
The person sees a shadow on the right edge, over the columns that scroll and not over Delete, and knows there are more columns to scroll to.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx\:\:A table that has more columns than shown says so shows a cue on the side with hidden columns while the person scrolls\, and none once the table fits \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
A shadow of another width or colour passes, as does other screen reader wording that names the side. A shadow over the Delete button or at the wrapper edge fails the browser check; a left and right swap fails both checks.
```

###### Obligations

None.

##### Covers

P7

```text
The person scrolls the table to the middle, then to its end, then the window is widened until everything fits; in the browser the table is scrolled to its end.
```

```text
The mark and the shadows go right, then both, then left, then none; at the end of the range only the left shadow shows; the edge rule gives the listed sides for worked scroll positions including each one-pixel boundary, never names a side for a table that fits, never a left side at the start or a right side at the end, names both when clear of both ends, swaps sides for mirrored positions, and never brings the left cue back once scrolled past it.
```

```text
The shadow follows the scroll position and disappears when nothing is hidden.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx\:\:A table that has more columns than shown says so shows a cue on the side with hidden columns while the person scrolls\, and none once the table fits \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns scrolled \%i px in a \%i px viewport over \%i px of content\, hidden columns are\: \%s \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns never names a side for a table that fits\, wherever the scroll position was left \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns shows no left cue at the start and no right cue at the end of the range \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns names both sides when the table is scrolled clear of both ends \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns treats the two edges alike\: scrolling to the mirror position swaps the sides \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts\:\:Which side of a table has hidden columns shows a left cue once it has started and keeps it\, and a right cue until it ends and never brings it back \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
A rule that is wrong only at a position no worked row or generated property picks. A tolerance of 2 px instead of 1 fails the one-pixel boundary rows.
```

###### Obligations

None.

##### Covers

P8

```text
A screen reader user reads the grid while columns are hidden on one side, on both sides and on none; a keyboard user lands on a Name cell in view, arrows to an Owner cell that ends under the pinned actions column, then arrows to the actions cell; in the browser at 375 px a keyboard user arrows from the Created date to the actions cell.
```

```text
The grid description reads: This table scrolls sideways. More columns to the right. (or to the left, or on both sides) and is absent when nothing is hidden; a cell in view makes the table stay put; the Owner cell is scrolled clear of the pinned actions column without its start leaving the view; the actions cell takes the focus; in the browser every focused cell comes to rest clear of the pinned actions column and the walk ends on the actions cell.
```

```text
A keyboard or screen reader user learns of the hidden columns as text and can reach every cell, the actions included.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx\:\:A table that has more columns than shown says so shows a cue on the side with hidden columns while the person scrolls\, and none once the table fits \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx\:\:A person using the keyboard reaches columns that are off screen scrolls a focused cell clear of the pinned actions column\, leaves a cell that is in view where it is\, and lets the keyboard reach the actions column \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
A focus scroll that always runs to the end of the table fails the Owner start check. The actions cell geometry is not checked in jsdom, where the layout is stated by the test; the browser walk measures it.
```

###### Obligations

None.

##### Covers

P11

```text
The Reports tab is opened at 375 px with the fitted layout and again with a stored 900 px Name; the person looks at the table and scrolls it to its end.
```

```text
The page does not scroll sideways and the table does; Delete stays in the table; the right shadow shows at the left edge, ends where the pinned actions column starts, and the left shadow shows at the end of the range, read from where each shadow sits.
```

```text
At phone width the table scrolls inside its box with the cue and the page stays put.
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

```text
A page that scrolls sideways by less than the browser reports as scrollable; the check compares the scrolling element widths only.
```

###### Obligations

None.

##### Covers

P4

```text
Unchanged from the earlier slice: columns are fitted with any stored width, saved column orders put the actions column anywhere, and the person resizes and resets.
```

```text
Number, date and action columns never go below their declared width, a text column keeps its flex or its stored width, the actions column is ordered last, and a resize stores only the resized width.
```

```text
Dates and the row action buttons are never cut and the actions stay at the right edge while the new cue is added.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A number\, date or action column is never cut \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A text column shrinks to its minimum unless the person set its width \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach orders the actions column last when no order is saved \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach stores only the width of the column the person resized

```text
A cue that changes a column width is caught by these unchanged tests.
```

###### Obligations











### S3

##### Covers

P9

```text
The Overview shows Teams and Portfolios whose names are longer than their column, in a box too narrow for every column; the person hovers a long name, reaches it with the keyboard, and looks at a table that fits.
```

```text
The name and tags give way and end with an ellipsis while numbers and dates stay whole; the full name appears in a tooltip on hover and on keyboard focus; a table that overflows on its right says so with a right-edge shadow and a screen-reader description 'This table scrolls sideways. More columns to the right.'; a table that fits shows neither.
```

```text
The person reads long Team and Portfolio names in full, never meets a cut number or date, and is told when columns are out of sight.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:The Overview table fits its columns and says when some are out of sight \| Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on every Team of the Overview\, at desktop and at phone width\, and reads a long name in full

```text
The name wraps instead of being cut, the tooltip opens on hover only, the grid never marks the overflow state, or a shadow shows on a table that fits (the box is stated 900 wide in 400, the fitting case 1000, so no check passes through a zero-width jsdom).
```

###### Obligations







##### Covers

P12

```text
A Teams table and a Portfolios table of the Overview whose columns overflow the width, with a saved column order that puts Actions first or leaves it out.
```

```text
Actions is the last column, its header and cells carry the pinned class and it has one width at phone and desktop size, whatever order the person saved.
```

```text
Delete, Edit and Clone stay at the right edge and never scroll out of view.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:The Overview table fits its columns and says when some are out of sight

```text
The actions column is last but not pinned, or is moved by a saved order (both orders are tried), or keeps a phone-specific width. The ordering rule is proven through two renders because its function is not exported.
```

###### Obligations





##### Covers

P9

P12

```text
A coach opens the Overview at 1280 px and at 375 px with a renamed Team whose name is very long, every Team of the Overview, and a Portfolio; scrolls, walks the row with the arrow keys and focuses the cut name.
```

```text
Delete of every Team and of the Portfolio is inside the table at both widths and at the end of the range; the name is really cut with a tooltip on hover and on focus; the Name column keeps 160 px; the page never scrolls sideways at 375 px; the keyboard reaches Actions clear of the pinned column; a right shadow ends at the pinned column and moves to the left at the end; a 1280 px table shows no shadow and its last column ends inside the box.
```

```text
The coach finds Delete on every Overview table at any width and sees when more columns wait.
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on every Team of the Overview\, at desktop and at phone width\, and reads a long name in full \| Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on the Portfolios table of the Overview at phone width\, and the table says it scrolls

```text
Only the first or renamed Team keeps Delete in view, the shadow covers Delete, or the page scrolls instead of the table (every Team and the Portfolios table are checked and every read is polled).
```

###### Obligations





##### Covers

P9

P12

```text
A person opens the Overview of Teams or Portfolios on a phone, where four action icons would crowd out the Name, then opens a row's menu with a click, with Enter or Space, and with Enter and the arrow keys, and chooses Details, Edit, Clone or Delete; with the menu closed and the keyboard on the menu button the person presses the left arrow; at desktop width the same table is opened.
```

```text
On a phone each row has one pinned menu button named Actions for the row name, with the tooltip Actions, and no separate icons; the actions column is no wider than the button; opening the menu puts the keyboard focus inside it; the menu lists exactly the actions the person may use, in the order Details, Edit, Clone, Delete; each item does what its icon did and Delete asks the same confirmation; after Escape the focus is on the menu button and the button is in view; with the menu closed the left arrow moves the focus to the cell before the actions cell; at desktop width the four icons are back and there is no menu button.
```

```text
The person always finds the actions of a row in one pinned button that never scrolls away, and can use every action and move along the row with mouse or keyboard.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:on a phone\, where four icons do not fit beside a readable Name \| Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:with the menu closed and the keyboard on a row\'s menu button \| Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:keeps the four action icons and no menu button on a desktop \| Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on every Team of the Overview\, at desktop and at phone width\, and reads a long name in full \| Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on the Portfolios table of the Overview at phone width\, and the table says it scrolls

```text
The icons stay at phone width and only the column narrows; the menu button is not pinned or scrolls away; the menu offers actions the person may not use; Delete from the menu skips the confirmation or targets another row; the menu opens by mouse only; opening the menu leaves the focus outside it; the menu button swallows the arrow keys so the person cannot leave the actions cell; the menu button also shows at desktop width; the actions column stays 200 px wide on a phone. Known limitation (maintainer decision 8): at 375 px, opening the menu of the second Overview table can make the browser scroll that table to its end, and the browser check does not hide or undo that jump; it requires only that after Escape the focus is on the menu button and the button is in view. A follow-up Bug is to be raised.
```

###### Obligations





##### Covers

P12

```text
On a phone-width Overview, the person opens a row's actions menu with the keyboard and closes it with Escape
```

```text
the menu closes, keyboard focus is back on that row's actions button, and the table has neither scrolled nor changed its edge shadows
```

```text
A keyboard user keeps their place in the table and the pinned actions column stays reachable
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx\:\:returns the keyboard focus to the row\'s menu button when Escape closes the menu \(reads the table\'s overflow state before opening and after Escape\)

```text
focus lands on the body or on a grid cell after Escape, or the grid jumps to its end when the button gets focus, or Escape changes which side shows a shadow
```

###### Obligations

None.

##### Covers

P9

```text
The person had set a column width on the Overview before, reloads, or presses Reset layout
```

```text
a width the person set is kept, and Reset layout returns to the fitted layout; this is the shared grid's behaviour, proven on the report list and on the shared grid, not repeated per table
```

```text
A person's own column widths are respected, and one click puts the fitted layout back
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:stored 900 px Name block \(the wide stored Name keeps its width and the pinned actions keep Delete in view\) \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx\:\:stored widths and Reset block \(storedWidths\) \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.defaults\.test\.tsx\:\:Reset layout lands on the defaults\, not on every column shown

```text
the shared grid ignores a stored width, or Reset keeps the person's width; an Overview-only check is not added because DataOverviewTable passes the same stored widths to the shared grid and has no width rule of its own
```

###### Obligations

None.

##### Covers

P9

P12

```text
In a mid-width window of about 700 px the Overview shows a Team with a long name, the four action icons still show and the columns do not fit
```

```text
Delete is in view at the start and at the end of the sideways scroll, the actions column is last and sits at the right edge, and the shadow shows on the right at the start and on the left only at the end
```

```text
At mid width the person still finds Delete and sees there is more to scroll to
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/teams\/OverviewTableFit\.spec\.ts\:\:a coach finds Delete on a Team with a long name in a mid\-width window\, where the icons still show and the columns overflow

```text
the actions column is not pinned, so Delete is out of view at the start of the scroll; the shadow is missing or on the wrong side
```

###### Obligations

None.

### S4

##### Covers

P12

```text
The Feature list and the Connections table, which have an actions column, are shown at a narrow width
```

```text
The actions column is the last column and is pinned to the right edge while the other columns keep their fit and the cue
```

```text
Delete, Edit and the move menu never scroll out of view
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.list\.test\.tsx\:\:pins the actions column as the last column and says columns are out of sight

```text
The Refinement vote columns are not asserted as actions, so leaving them unpinned would still pass
```

###### Obligations



##### Covers

P10

```text
The Feature list shows a Feature that waits on two others, and the Dependencies column is narrower than the list
```

```text
Each dependency stays on its own line and is cut to the column with an ellipsis instead of wrapping; hovering or tabbing to the list opens a tooltip that names every dependency (for example DEP-1: Card payments gateway cutover, DEP-2: Ledger export to the data warehouse, joined by commas); a list that fits opens no tooltip
```

```text
A person with a long dependency list sees each dependency on its own line, cut to the column, and can read every dependency in full by hovering or keyboard focus
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:\:cuts a long dependency list to its column\; Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:\:names every dependency in a tooltip when the person hovers over a cut dependency list\; Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:\:names every dependency in a tooltip when the person reaches a cut dependency list with the keyboard\; Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:\:opens no tooltip over a dependency list that fits its column\; Lighthouse\.EndToEndTests\/tests\/specs\/features\/FeatureDependencies\.spec\.ts\:\:a product owner on a narrow window sees each dependency cut on its own line and reads them all in full on hover

```text
the first dependency line is cut but another line wraps or overflows, or the tooltip shows only the count or only the first dependency, or it opens even when the list fits
```

###### Obligations

None.

##### Covers

P10

```text
The Feature picker, Work Item dialog, archived Feature table, Connections, third-party packages and Refinement tables each show a name that fits its column, and the person hovers over it
```

```text
No tooltip opens over text that is not cut
```

```text
A person is not shown a tooltip that repeats text already shown in full
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.test\.tsx\:\:opens no tooltip over a Feature name that fits its column\; Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.layout\.test\.tsx\:\:opens no tooltip over a Work Item name that fits its column\; Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.test\.tsx\:\:opens no tooltip over a Feature name that fits its column\; Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.list\.test\.tsx\:\:opens no tooltip over a connection name that fits its column\; Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.test\.tsx\:\:opens no tooltip over a package name that fits its column\; Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.test\.tsx\:\:opens no tooltip over a Work Item name that fits its column

```text
a table opens a tooltip over every name whether or not it is cut
```

###### Obligations

None.

##### Covers

P10

```text
Each of the Feature picker, Feature list, Work Item dialog, archived Feature, Connections, third-party packages and Refinement tables is shown at a narrow width with a long text value, the Feature list and the Refinement table also with a long parent name
```

```text
Long text, including a parent name once its title has loaded, is cut to its column and its full text shows in a tooltip on hover and on keyboard focus; numbers, dates and states show whole and open no tooltip; text that fits opens no tooltip; the table says columns are out of sight on its right
```

```text
The person sees every table fit its columns, sees the cue where columns are hidden and can read a cut name or parent in full
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:\:cuts a long Feature name and a long parent name to their columns and shows the position and the state whole

```text
A long Dependencies column (content, declared width 260 by design) that wraps or overflows would still pass; a tooltip showing the parent reference instead of its title is refused by the Refinement and Feature list parent tooltip checks
```

###### Obligations




##### Covers

P12

```text
A person focuses a cell on the Feature list that sits under the pinned move menu column
```

```text
The table scrolls so the focused cell ends clear of the move menu column
```

```text
The keyboard user sees the cell they are on, not hidden under the move menu
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx\:681

```text
The scroll fix only looks for a column named actions, so the move menu column is not treated as pinned
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Any table on the shared grid lets me find every column it offers, including row actions like Delete: columns shrink to fit, and when they cannot fit, the table visibly shows more columns wait off screen.
```

#### E2

##### Promises

P2

```text
Opening a Team's Reports tab at desktop width with several reports shows the Name column shrinking and the actions column with Delete inside the visible table, with no horizontal scrolling needed.
```

#### E3

##### Promises

P3

```text
A report name longer than its column ends with an ellipsis, and hovering or focusing it shows a tooltip with the full name; the row's other cells stay visible and the row does not wrap.
```

#### E4

##### Promises

P4

```text
When columns do not all fit, number, date and row-action columns keep full width (no ellipsis on numbers or dates, no clipped action buttons); only text columns shrink below their preferred width.
```

#### E5

##### Promises

P5

```text
A column width I set by resizing is reused as set when I reopen the grid, with other columns fitting around it; Reset in the toolbar clears my stored widths and returns to the fitted layout.
```

#### E6

##### Promises

P6

```text
On a table whose columns do not fit (for example a narrow window), scrolled to the left, a shadow or fade on the right edge shows more columns can be scrolled to; columns are never cut off silently.
```

#### E7

##### Promises

P7

```text
The edge cue follows scroll position: at the scroll end the right cue goes and a left cue shows while columns are hidden on the left; once everything fits (window widened) no cue shows, and no cue sticks at the end of the range or on a fitting table.
```

#### E8

##### Promises

P8

```text
A keyboard user moving through a grid with off-screen columns has focus scroll those cells into view and can reach the actions column; assistive technology is told the table scrolls horizontally with more columns than shown, so overflow is not marked by a visual effect alone.
```

#### E9

##### Promises

P9

```text
The Overview table with columns that do not fit fits and truncates like the report list and shows an edge cue when columns still overflow; its last column is not hidden without a cue.
```

#### E10

##### Promises

P10

```text
At a narrow width, the Feature, Work Item, archived Feature, Connections, third-party packages and Refinement tables each fit where they can and show the cue where they cannot, consistent with the report list, and none scrolls its last column out of view without a cue.
```

#### E11

##### Promises

P11

```text
At about 375 px width, a table that cannot fit shows text columns at minimum width, scrolls horizontally inside the table (not the page), shows the cue, and keeps the actions reachable by scrolling.
```

#### E12

##### Promises

P12

```text
With overflowing columns, scrolling the table sideways keeps the actions column (Delete, Edit and so on) pinned at the right edge and always visible, while fit and edge cue still apply to other columns; the pinned column neither hides the cue nor covers another column's content.
```

## Charter

None.

## Examination

None.
