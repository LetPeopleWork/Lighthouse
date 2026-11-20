# Acceptance brief: story\-6650\-tables\-fit\-and\-show\-overflow

## Slices

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
