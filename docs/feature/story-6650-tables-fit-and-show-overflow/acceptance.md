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

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A number\, date or action column is never cut \(L1\) \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

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

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts\:\:A text column shrinks to its minimum unless the person set its width \(L2\) \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach \(L5 cases\) \| Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full

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

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts\:\:a coach finds Delete on every report of a Team\, at desktop and at phone width\, and reads a long name in full \| Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx\:\:Columns fit the width and keep the row actions in reach \(L4 cases\)

```text
The pinned actions column covers part of the last scrolling column's content.
```

###### Obligations




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
