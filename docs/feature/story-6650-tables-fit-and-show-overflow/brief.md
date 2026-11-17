# Product brief: story\-6650\-tables\-fit\-and\-show\-overflow

## Request

```text
ADO Story #6650, split out of Story #6625. Feedback from a community user: he could not find Delete on the report list; it sits in the last column and scrolls out of view. The cause is in the shared DataGridBase, used by the report list, the Overview tables (DataOverviewTable) and other tables (FeatureGrid, FeatureListDataGrid, WorkItemsDialog, ArchivedFeatureGrid, ConnectionsSettings, ThirdPartyPackagesSection, RefinementGrid). The maintainer sees the same problem on the Overview. Maintainer decision (2026-11-15): both. 1) Columns fit the available width better: they shrink, and long names truncate with a tooltip. 2) When columns still do not fit, a visible cue (shadow or fade on the edge) shows there is more to scroll to.
```

## Work

M

```text
Every column of a table, including its actions, can be found: tables fit their width where they can, and say so when they cannot.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A person looking at any table that uses the shared grid can find every column it offers, including its row actions such as Delete on the report list: the columns fit the available width where they can, and when they cannot, the table visibly shows that more columns wait off screen.
```

### P2

Scenario

```text
Report list shows Delete without scrolling
```

```text
a Team with several reports and a desktop-width window
```

```text
the person opens its Reports tab
```

```text
the Name column takes the remaining width and shrinks, and the actions column with Delete is inside the visible table without horizontal scrolling. Falsifier: Delete sits beyond the right edge at a width where the other columns could have shrunk.
```

### P3

Scenario

```text
Long text truncates and the tooltip gives the full text
```

```text
a report whose name is longer than its column
```

```text
the person looks at the row and hovers or focuses the name
```

```text
the name ends with an ellipsis and a tooltip shows the full name; text columns shrink first. Falsifier: the full text is lost with no way to read it, or the cell wraps and hides the row's other cells.
```

### P4

Scenario

```text
Numbers, dates and actions are never cut
```

```text
a table whose columns do not all fit
```

```text
the grid shrinks columns
```

```text
number, date and row-action columns keep at least the width that shows their whole content (a number is never ellipsised); only text columns shrink below their preferred width. Falsifier: a number or date shows an ellipsis, or an action button is clipped.
```

### P5

Scenario

```text
A width the person set stays theirs
```

```text
a person has resized the Name column of a grid, so the width is stored for that grid
```

```text
they reopen the grid
```

```text
the stored width is used for that column as set, and the other columns fit around it; Reset in the toolbar clears stored widths and returns to the fitted layout. Falsifier: reopening replaces their width with the fitted one, or Reset leaves their widths in place.
```

### P6

Scenario

```text
An edge cue shows more columns off screen
```

```text
a table whose columns still do not fit at the current width (for example a narrow window)
```

```text
the person views the table scrolled to the left
```

```text
a shadow or fade on the right edge shows that more columns can be scrolled to. Falsifier: columns are cut off at the edge with no cue.
```

### P7

Scenario

```text
The cue follows the scroll position and disappears when nothing is hidden
```

```text
a table with a cue on the right edge
```

```text
the person scrolls to the end, or the window is widened until everything fits
```

```text
the right cue goes, a left cue shows while columns are hidden on the left, and no cue shows once all columns are visible. Falsifier: a cue stays at the end of the scroll range or on a table that fits.
```

### P8

Scenario

```text
The cue is not the only way to know
```

```text
a table with columns off screen and a keyboard or screen-reader user
```

```text
they move through the grid with the keyboard
```

```text
focus moves to off-screen cells and scrolls them into view, and the table exposes to assistive technology that it scrolls horizontally with more columns than shown (the cue is also a text-readable state, not colour or shadow alone). Falsifier: a keyboard user cannot reach the actions column, or only a visual effect marks the overflow.
```

### P9

Scenario

```text
Overview tables fit and cue like the report list
```

```text
the Overview with a table whose columns do not fit
```

```text
the person opens it
```

```text
its columns fit and truncate as on the report list, and a cue shows when columns still overflow. Falsifier: the Overview table still hides its last column with no cue.
```

### P10

Scenario

```text
Remaining tables behave the same
```

```text
the Feature, Work Item, archived Feature, Connections, third-party packages and Refinement tables
```

```text
each is opened at a narrow width
```

```text
each fits where it can and shows the cue where it cannot, with no per-table behaviour that contradicts the report list. Falsifier: one of these tables still scrolls its last column out of view with no cue.
```

### P11

Scenario

```text
Phone width
```

```text
a window around 375 px wide
```

```text
a person opens a table that cannot fit
```

```text
text columns are at their minimum width, the table scrolls horizontally, the cue shows, and the actions remain reachable by scrolling. Falsifier: the page scrolls sideways instead of the table, or the actions cannot be reached.
```

### P12

Scenario

```text
The actions column stays pinned at the right edge
```

```text
a shared-grid table with an actions column (Delete, Edit and so on), and columns that overflow the width
```

```text
the person scrolls the table sideways
```

```text
the actions column stays at the right edge and never scrolls out of view, while the fit and the edge cue still apply to the other columns. Falsifier: Delete scrolls out of view at any width, or the pinned column hides the cue or covers another column's content.
```

## Scope

```text
Shared DataGridBase behaviour: columns fit the available width (text columns shrink and truncate with a full-text tooltip; number, date and action columns are protected), a horizontal-overflow edge cue, keyboard and screen-reader exposure of the overflow, and the interaction with stored widths, applied to the report list first, then the Overview and the other DataGridBase tables.
```

```text
The actions column pinned to the right edge on every shared-grid table that has one (walking skeleton on the report list, carried to the other tables by the later slices).
```

```text
New columns, new row actions, moving Delete to another place (such as a row menu), changes to what a table lists, the report list's content, a card-per-row layout at phone width; also hiding or reordering columns, which exist today and stay as they are.
```

## Decisions

```text
Direction is Confirmed by the maintainer (2026-11-15): both fit (columns shrink, long names truncate with a tooltip) and a visible edge cue when columns still do not fit.
```

#### Status

Confirmed


```text
Proposed: text columns (names, titles, owner) shrink and truncate; number, date, state and row-action columns never go below the width that shows their whole content, so a number is never ellipsised and an action button is never clipped (visualization: truncation must not hide meaning).
```

#### Status

Proposed


```text
Proposed: the tooltip shows the full text, on hover and on keyboard focus of the cell; it is not the only place the text lives, so the cell carries the full text for assistive technology.
```

#### Status

Proposed


```text
Proposed: a width the person set by resizing is kept as they set it, and the fit fills the remaining columns around it; Reset in the toolbar clears the stored widths and returns to the fitted layout. If their stored widths together exceed the available width, the table scrolls and shows the cue; their choice is not overridden.
```

#### Status

Proposed


```text
Proposed: shadow or fade on the right edge while columns lie to the right, on the left edge while columns lie to the left, each disappearing when that side has nothing hidden; the cue is decorative and does not capture clicks.
```

#### Status

Proposed


```text
Proposed: the cue is not the only signal: keyboard focus scrolls cells into view, and the grid exposes its horizontal overflow to assistive technology (existing MUI DataGrid roles retained).
```

#### Status

Proposed


```text
Proposed: the behaviour applies to every DataGridBase user at once, because the cause is in the shared component; each table can mark which of its columns are protected. Delivery is staged by slice (report list, Overview, the rest) so each table is observed on its own.
```

#### Status

Proposed


```text
DISCUSS checklist - RBAC impact: N/A, because no action, page or data is added; Delete and the other actions keep their existing gating through useRbac().
```

#### Status

Confirmed


```text
DISCUSS checklist - Lighthouse-Clients CLI / MCP: N/A, because no API route or DTO changes; this is front-end rendering only.
```

#### Status

Confirmed


```text
DISCUSS checklist - Website marketing surface: N/A, because it fixes table usability and is not a capability worth a line; no docs/assets file is expected to change (screenshots are a finalize decision).
```

#### Status

Confirmed


```text
DISCUSS checklist - Context map: cross-cutting UI infrastructure; no bounded context of ADR-270 is changed and no domain event is published or reacted to. It touches the surfaces that show tables: the Reports tab of a Team and of a Portfolio, the Overview, Feature and Work Item lists, and Settings > System (Connections, System Info). No new glossary term is introduced; no new concept, context or edge, so ADR-270 and ARCHITECTURE.md section 4 stay unchanged. Per ADR-271 no new UI surface is added and no surface moves; the terms Feature, Work Item, Team, Portfolio are written as the configurable defaults.
```

#### Status

Confirmed


```text
Usage data is a DEVOPS question, not asked here.
```

#### Status

Confirmed


```text
Loading and failure: the grid already shows its own loading state; this feature adds no data fetch, so no new loading or failure look is needed (DESIGN confirms). The sketch review at the start of DELIVER must cover the cue, the truncated cell and the phone width.
```

#### Status

Proposed


```text
Confirmed by the maintainer (2026-11-17): the actions column (Delete, Edit and so on) is pinned to the right edge on every shared-grid table that has one, so it never scrolls out of view; the fit and the edge cue stay.
```

#### Status

Confirmed


```text
Open (maintainer, with the DELIVER sketch): minimum widths and which columns are protected per table; for example the report list's Name shrinking to a minimum of about 160 px. Exact numbers are not yet supplied.
```

#### Status

Open

Design

```text
Open (maintainer, at the DELIVER sketch review): shadow or fade as the cue style, and whether a text hint is added.
```

#### Status

Open

Design

```text
Confirmed by the maintainer (2026-11-17): at phone width the table scrolls inside its box with the cue; a card-per-row layout is out of scope.
```

#### Status

Confirmed


```text
Open (DESIGN decides): how to pin the actions column. The app uses the community @mui/x-data-grid, which has no built-in column pinning, so DESIGN chooses between a custom sticky column and the Pro grid's pinning, and must check the choice with the edge cue, user-set widths and phone width.
```

#### Status

Open

Design

## Journey

```text
A person opens a table (for example a Team's Reports tab) and looks for Delete; the columns fit the width, so Delete is in view.
```

```text
Not supplied; the community user reported that he could not find Delete.
```

```text
In a narrow window the table cannot fit; the person sees the edge cue, scrolls or tabs to the actions column and uses it.
```

```text
Not supplied.
```

## Slices

### S1

```text
On the report list, Delete and the other row actions are visible without scrolling at desktop width and stay pinned at the right edge when the table scrolls; the Name column shrinks and truncates with a full-text tooltip; number, date and action columns are never cut; a width the person stored is kept. Establishes the shared fit and pinning mechanism in the grid (walking skeleton).
```

#### Promises

P1

P2

P3

P4

P5

P12

#### Depends on

None.

#### Slice work

M

```text
Delete is found on the report list
```

##### Rework

None.

### S2

```text
On the report list at a width where columns still do not fit, a right-edge cue shows more columns, follows scroll position, disappears when nothing is hidden, and the grid announces horizontal overflow to keyboard and screen-reader users; at phone width the table (not the page) scrolls.
```

#### Promises

P6

P7

P8

P11

#### Depends on

S1

#### Slice work

S

```text
A person knows when a table has more columns than shown
```

##### Rework

None.

### S3

```text
On the Overview, the DataOverviewTable fits and truncates like the report list and shows the cue when columns still overflow. Its actions column, if any, stays pinned.
```

#### Promises

P9

P12

#### Depends on

S1

S2

#### Slice work

S

```text
The Overview no longer hides its last column
```

##### Rework

None.

### S4

```text
The Feature, Work Item, archived Feature, Connections, third-party packages and Refinement tables each fit, pin their actions column and show the cue at a narrow width, with columns marked truncatable or protected per table.
```

#### Promises

P10

P12

#### Depends on

S3

#### Slice work

M

```text
Every shared-grid table can be read to its last column
```

##### Rework

None.
