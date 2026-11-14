# Acceptance brief: story\-6502\-hide\-outliers

## Slices

### S1

##### Covers

P1

```text
A reader views a Cycle Time chart where no closed Work Item is longer than 5 times the median, including one exactly at 5 times
```

```text
Every item is drawn, the axis top is as before (1.1 times the largest value), and the header has no outlier chip
```

```text
A chart with no extreme item looks exactly as it did before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:leaves the chart as it was when no closed Work Item is longer than 5 times the median

```text
A rule using at-least instead of more-than at 5 times would hide the 30-day item; that wrong rule is rejected here
```

###### Obligations


##### Covers

P1

P2

P5

```text
A reader opens a Team chart where one closed Work Item took 58 days against a median of 6, with percentile and SLE lines, then clicks the header chip
```

```text
The 58-day item is not drawn, the axis top is 1.1 times the largest drawn value or line, the 50%, 85%, 95% and SLE lines keep their given values 6, 8, 15 and 7, the header chip reads 1 outlier, the popover says 'Not drawn: longer than 30 days (5 times the median, 6 days).' and 'Still counted in the percentiles, SLE and View Data.', and lists the item with closed date, 58 and 9.7x (9.7 times the median)
```

```text
The extreme item is left off the plot and disclosed one click away, while the chart's percentile and SLE lines keep their values
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:shows the one 58\-day Work Item as an outlier in the header while the statistics stay the same

```text
Percentile values recomputed from drawn items, median taken from drawn items only, chip left in the legend row, item missing from the popover, or 9.67 rounded down would each fail an assertion
```

###### Obligations


##### Covers

P1

```text
A closed Work Item one day over 5 times the median (31 days against 6)
```

```text
It is not drawn and the header chip reads 1 outlier
```

```text
The cut is exactly more than 5 times the median, not a looser multiple
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:flags a Work Item one day over 5 times the median

```text
A rule using a multiple of 6 or more would accept the 31-day item as drawn; that is rejected here
```

###### Obligations


##### Covers

P1

P2

```text
The 95% and SLE lines lie above the cut (33 and 35 days) while a 58-day item is left out; the reader looks at the chart and opens the chip
```

```text
The axis top follows the typical items (1.1 times 8), the two lines are not drawn in place, each appears as a labelled marker ('95%: 33 days', 'SLE: 85% @ 35 days or less'), and the popover says 'Above the axis: 95% at 33 days, SLE at 35 days.'
```

```text
A line that no longer fits on the axis is still shown at the top edge and announced in words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:tells the reader about a percentile and an SLE line that lie above the axis

```text
Removing the markers, an axis stretched to the 58-day item, or off-axis lines dropped without mention fails; marker geometry is not asserted in jsdom
```

###### Obligations


##### Covers

P1

```text
The 95% line lies exactly at the cut (30 days) while a 58-day item is left out
```

```text
The axis stops at 30, the line stays in place and no above-the-axis sentence appears
```

```text
A line that exactly reaches the top of the plot is still drawn, not turned into a marker
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:keeps a line that sits exactly at the top of the axis in place

```text
A split that treats equal as above would move the line into the sentence; rejected here
```

###### Obligations



##### Covers

P1

```text
A Team chart with an outlier and no percentile or SLE lines
```

```text
The axis is 1.1 times the largest typical item and no above-the-axis sentence appears
```

```text
A chart without lines scales to normal work
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:scales the axis to the typical items when there are no percentile or SLE lines

```text
An axis that still includes the outlier or fails without lines is rejected
```

###### Obligations


##### Covers

P1

```text
A chart with a single closed Work Item, with items that all took the same time, or with no closed Work Items
```

```text
Everything is drawn and the header has no outlier chip (empty set reads 'No data available')
```

```text
Degenerate data never invents an outlier
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:shows no outlier chip for a single closed Work Item

```text
A median taken as zero for one item, or a strict-above test failing on equal values, would flag an item and fail here
```

###### Obligations




##### Covers

P1

P2

```text
The reader hides and shows Work Item types on a chart with one outlier Bug and typical Stories
```

```text
Hiding Stories leaves 'All visible Work Items are outliers.' and the chip; hiding the Bug removes the chip with the axis unchanged; hiding every type reads 'All types are hidden.' with no chip and a type can be shown again
```

```text
The chip and plot follow the visible types while the cut and axis do not move
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:keeps the cut and the axis still while the reader hides and shows Work Item types

```text
A cut computed from visible items only would move the axis or the chip on each toggle; rejected here
```

###### Obligations




##### Covers

P2

```text
The reader opens the outlier popover, then the widget starts reloading
```

```text
Opening reports the outlier-popover usage event once; on reload the popover closes, the chip is disabled and neither its text nor its accessible name shows a count
```

```text
A loading widget never shows an outlier count from the previous answer
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:reports one usage event per opening and shows no count while the widget reloads

```text
A chip that stays enabled or keeps the old count while loading, or a usage report on close or forced close, fails here
```

###### Obligations

None.

##### Covers

P1

```text
Generated closed sets and line sets checked against the pure rule (median convention, strict cut, partition, axis top, line split)
```

```text
The pure rule matches an independent oracle for every generated input
```

```text
The same cut and axis hold for any real data, not only the chosen examples
```

##### Disposition

NotTested

```text
Needs the property-test library fast-check and the new pure modules, neither installed or declared as a writable support; chart-level examples above cover the same laws for chosen inputs
```

crafter

##### Covers

P2

```text
A Team chart has two outliers, 58 and 40 days against a median of 6; the reader opens the chip
```

```text
The chip reads 2 outliers and the popover lists the 58-day item (12 Mar 2026, 58, 9.7x) then the 40-day item (11 Mar 2026, 40, 6.7x)
```

```text
Every outlier is listed, longest first, with date, days and multiple
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:lists two outliers longest first with closed date\, days and multiple of the median

```text
A chip fixed at 1 outlier or a popover listing only the first outlier fails
```

###### Obligations

None.

##### Covers

P2

```text
A Team chart has eleven outliers (31 to 41 days); the reader opens the chip
```

```text
The chip reads 11 outliers; the popover lists the ten longest first and then 'and 1 more'
```

```text
A long list stays short and says how many are not shown
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:lists the ten longest of eleven outliers and says how many more there are

```text
A list of all eleven, ten shortest, or no overflow line fails
```

###### Obligations

None.

##### Covers

P2

```text
The widget fails to load after the reader opened the chip
```

```text
The could-not-load message is shown, the chip is absent from the header, no digit shows in the header and the popover is gone
```

```text
A failed widget never keeps an outlier chip or count
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:replaces the plot and the chip with the could\-not\-load message when the data fails

```text
A chip that stays or keeps its count after failure fails
```

###### Obligations

None.

##### Covers

P2

```text
The widget is loading for the first time with nothing on screen
```

```text
No outlier count and no popover appear in the header
```

```text
A first load never shows an outlier count
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:shows no outlier count the first time the widget is loading

```text
A count shown before any answer exists fails; this case already holds today
```

###### Obligations

None.

##### Covers

P5

```text
A chart with an outlier is open; the reader opens View Data or clicks a dot to see its Work Items
```

```text
View Data and the dot dialog still list every closed Work Item including the outlier
```

```text
No number or list outside the chart's drawing changes
```

##### Disposition

NotTested

```text
View Data and the dot dialog are built outside the chart's outlier filter (the data list for View Data is the full list, unchanged by this slice); the oracle stubs the dialog, so the final review and EXAMINE verify it
```

examiner

### S3

##### Covers

P4

```text
Reader opens the outlier popover of a chart with eleven outliers and activates Open in a list, then closes the list
```

```text
The popover closes, the Work Items list opens titled with the reader's Work Items and Cycle Time terms and holds all eleven outliers, focus moves into the list and returns to the outlier chip on close
```

```text
The reader sees every outlier in the list and keyboard focus is not lost
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

```text
The popover stays open behind the list, the list holds only the ten shown rows or all items, or focus falls to the page body after closing
```

###### Obligations




##### Covers

P4

```text
Reader renames Work Items and Cycle Time, or hides a Work Item type, then opens the outliers in a list
```

```text
The list title uses the renamed terms and holds only the outliers of the types still shown
```

```text
The list matches what the chip counts, in the reader's words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

```text
The title keeps the default words or the list includes outliers of hidden types
```

###### Obligations




##### Covers

P4

```text
Read the Cycle Time scatterplot help for a Team and a Portfolio with renamed terms
```

```text
The help states that an outlier is a closed item whose Cycle Time is more than 5 times the median of all closed items in the date range, in the configured terms, with outlier fixed and no default Work Item or Cycle Time words
```

```text
The reader learns the rule in their own vocabulary
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

```text
Help keeps default words or omits the 5 times the median rule for portfolios
```

###### Obligations





### S4

##### Covers

P2

P4

```text
A reader clicks the amber chip on Team Zenith's closed set (median 6 days, one 58-day item)
```

```text
The Work Items dialog opens at once, titled with the more-than-5-times-the-median wording, listing only TZ-301, receiving no props beyond its list, title and existing context; no popover, no extra step; OutlierListOpened is sent once
```

```text
One click shows the outlier list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

```text
A popover with an Open in a list button first, extra dialog content, an event with the old name, or an event sent twice
```

###### Obligations


##### Covers

P4

```text
A keyboard reader presses Enter on the chip, closes the list, and opens it again
```

```text
The list opens on Enter, focus returns to the chip on close, and each opening is reported once
```

```text
The keyboard reader reaches the list and keeps their place
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

```text
Focus lost to the page body after close, or Enter doing nothing
```

###### Obligations


##### Covers

P2

```text
The chip is read ready, at phone width, and while loading
```

```text
Border, icon and text use the app's amber warning colour in all three, each in its own scenario; loading shows a disabled Outliers chip with no count that opens and reports nothing when clicked
```

```text
The reader notices the amber chip at any width and never sees a stale count
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

```text
A plain grey chip or default MUI orange at some width or while loading, or a click on the loading chip opening the list
```

###### Obligations


##### Covers

P2

```text
The chip name and tooltip are read with no line above the axis, and with the 95% line and the SLE above it
```

```text
Name equals tooltip: n outlier(s) not drawn on the chart, then the Above the axis sentence only when a line is above, then Show the list
```

```text
A screen-reader user learns the count and the lines above the axis
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

```text
A name without the Above the axis sentence when a line is above, or with it when none is, or a tooltip that differs from the name
```

###### Obligations



##### Covers

P2

```text
The accessible-name wording is built for one outlier with no line above, three with the 95% line, one with the SLE, and two with both
```

```text
Each exact sentence, singular or plural, with the Above the axis phrase only when given
```

```text
Wording is right for every line-above shape
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.mutation\.test\.ts

```text
Show details still ending the name, or the sentence always present
```

###### Obligations




##### Covers

P4

```text
The chip is clicked with a named Cycle Time chosen, after a date-range change, with a type hidden, with 11 outliers, and with renamed terms
```

```text
The dialog lists exactly the visible-type outliers of the current view, all eleven, titled in the reader's terms, with focus back on the chip
```

```text
The list always matches the chip's view
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

```text
A list of the ten popover rows only, drawn items, or a stale view
```

###### Obligations


##### Covers

P2

```text
Revised earlier chart scenarios: line above the axis, line exactly at the top, no lines, counts, usage event, error and loading frames
```

```text
Chip name carries the Above the axis sentence only when a line is above; the usage event is OutlierListOpened once per open; error and loading frames show no chip count
```

```text
The earlier behaviours still hold without the popover
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx

```text
Old Show details name or old event name
```

###### Obligations


##### Covers

P2

```text
On Team Zenith demo data the reader clicks the header chip
```

```text
The Work Items dialog is visible straight after the click with row TZ-301
```

```text
One click from the header lists TZ-301
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/metrics\/OutlierChip\.spec\.ts

```text
A popover table that also has a TZ-301 row
```

###### Obligations


##### Covers

P2

```text
The usage event member is named
```

```text
OutlierListOpened keeps number 27 after the last named event and carries only its name
```

```text
Usage counts follow the new name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/UsageData\/OutlierListOpenedEventNameTests\.cs

```text
Old name kept
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Cycle Time scatterplot of a Team or Portfolio does not draw a closed Work Item whose Cycle Time exceeds 5 times the 50th percentile; the y-axis rescales to the remaining items; percentile and SLE lines stay visible in place or as labelled markers at the top edge; with no such item the chart is unchanged.
```

#### E2

##### Promises

P2

```text
Widget header chip reads 1 outlier or N outliers and is absent when there are none; it opens a popover saying the items are not drawn, stating the cut and the median in days, and listing the outliers longest first, up to ten rows followed by and k more, each row with closed date, Cycle Time in days and multiple of the median. While the widget loads the chip is disabled and shows no count; when the data cannot be loaded the could-not-load message replaces the plot and the chip; the chip never shows a count from a previous answer.
```

#### E3

##### Promises

P3

```text
Chip and list count only outliers of visible Work Item types and follow the selected named Cycle Time and date range; toggling a type does not move the cut or the axis; when every visible item is an outlier or every type is hidden the plot says so instead of No data available.
```

#### E4

##### Promises

P4

```text
From the popover the reader opens the outliers in the existing Work Items list dialog with sort, export and tracker links working; the widget help explains the outlier rule in the reader's configured terminology.
```

#### E5

##### Promises

P5

```text
Percentiles, SLE share and RAG, tiles, View Data and forecasts are unchanged by the outlier feature and still include the outliers.
```

## Charter

None.

## Examination

### E1

unmet

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlotChart.outlier,widgetInfoMetadata: exit 0, 4 files, 36 of 36 passed. Passing test names only, not a user-facing capture by the examiner: 58-day item shown as header outlier with statistics unchanged; flags an item one day over 5 times the median; tells the reader about a percentile and an SLE line above the axis; keeps a line exactly at the top of the axis in place; lists the ten longest of eleven outliers and says how many more; replaces plot and chip with could-not-load; shows no outlier count while loading; keeps cut and axis still while types are hidden and shown; lists only outliers of visible types; ignores a late answer for a named Cycle Time; help names the rule on a team and a portfolio; titles the list in the reader's own words. Names alone do not establish the asserted behaviour.
```

```text
Non-probe gap: no declared public probe renders CycleTimeScatterPlotChart, clicks the chip, toggles Work Item types, throttles loading or failure, or activates Open in a list. Not observed by examiner: axis top value, top-edge marker placement, popover text and row values, and k more text, dialog contents, sort, export and tracker links, focus return, help text under configured terms, loading and failed visuals, numbers unchanged on a live view. Evidence-capture owner: the candidate's declared probe set (nwdes host registration); examiner cannot add a probe.
```

observation_gap

### E2

unmet

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlotChart.outlier,widgetInfoMetadata: exit 0, 4 files, 36 of 36 passed. Passing test names only, not a user-facing capture by the examiner: 58-day item shown as header outlier with statistics unchanged; flags an item one day over 5 times the median; tells the reader about a percentile and an SLE line above the axis; keeps a line exactly at the top of the axis in place; lists the ten longest of eleven outliers and says how many more; replaces plot and chip with could-not-load; shows no outlier count while loading; keeps cut and axis still while types are hidden and shown; lists only outliers of visible types; ignores a late answer for a named Cycle Time; help names the rule on a team and a portfolio; titles the list in the reader's own words. Names alone do not establish the asserted behaviour.
```

```text
Non-probe gap: no declared public probe renders CycleTimeScatterPlotChart, clicks the chip, toggles Work Item types, throttles loading or failure, or activates Open in a list. Not observed by examiner: axis top value, top-edge marker placement, popover text and row values, and k more text, dialog contents, sort, export and tracker links, focus return, help text under configured terms, loading and failed visuals, numbers unchanged on a live view. Evidence-capture owner: the candidate's declared probe set (nwdes host registration); examiner cannot add a probe.
```

observation_gap

### E3

unmet

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlotChart.outlier,widgetInfoMetadata: exit 0, 4 files, 36 of 36 passed. Passing test names only, not a user-facing capture by the examiner: 58-day item shown as header outlier with statistics unchanged; flags an item one day over 5 times the median; tells the reader about a percentile and an SLE line above the axis; keeps a line exactly at the top of the axis in place; lists the ten longest of eleven outliers and says how many more; replaces plot and chip with could-not-load; shows no outlier count while loading; keeps cut and axis still while types are hidden and shown; lists only outliers of visible types; ignores a late answer for a named Cycle Time; help names the rule on a team and a portfolio; titles the list in the reader's own words. Names alone do not establish the asserted behaviour.
```

```text
Non-probe gap: no declared public probe renders CycleTimeScatterPlotChart, clicks the chip, toggles Work Item types, throttles loading or failure, or activates Open in a list. Not observed by examiner: axis top value, top-edge marker placement, popover text and row values, and k more text, dialog contents, sort, export and tracker links, focus return, help text under configured terms, loading and failed visuals, numbers unchanged on a live view. Evidence-capture owner: the candidate's declared probe set (nwdes host registration); examiner cannot add a probe.
```

observation_gap

### E4

unmet

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlotChart.outlier,widgetInfoMetadata: exit 0, 4 files, 36 of 36 passed. Passing test names only, not a user-facing capture by the examiner: 58-day item shown as header outlier with statistics unchanged; flags an item one day over 5 times the median; tells the reader about a percentile and an SLE line above the axis; keeps a line exactly at the top of the axis in place; lists the ten longest of eleven outliers and says how many more; replaces plot and chip with could-not-load; shows no outlier count while loading; keeps cut and axis still while types are hidden and shown; lists only outliers of visible types; ignores a late answer for a named Cycle Time; help names the rule on a team and a portfolio; titles the list in the reader's own words. Names alone do not establish the asserted behaviour.
```

```text
Non-probe gap: no declared public probe renders CycleTimeScatterPlotChart, clicks the chip, toggles Work Item types, throttles loading or failure, or activates Open in a list. Not observed by examiner: axis top value, top-edge marker placement, popover text and row values, and k more text, dialog contents, sort, export and tracker links, focus return, help text under configured terms, loading and failed visuals, numbers unchanged on a live view. Evidence-capture owner: the candidate's declared probe set (nwdes host registration); examiner cannot add a probe.
```

observation_gap

### E5

unmet

#### Observations

```text
Probe backend-tests Percentile: exit 0, 427 of 427 passed. Visible names are percentile, forecast write-back and percentile-history tests (ResolveForecastWriteBackForPortfolio, UpdateFeaturesForProject_UseCalculatedDefault, Migration_OnPostgres_PersistsPercentilesOverTimeSnapshotTable). No outlier-named backend test appeared in the captured output tail. Supports only that percentile and forecast computations pass their named tests. Not a capture of tiles, SLE share, RAG, View Data or forecast values with outliers included.
```

```text
Non-probe gap: no declared public probe renders CycleTimeScatterPlotChart, clicks the chip, toggles Work Item types, throttles loading or failure, or activates Open in a list. Not observed by examiner: axis top value, top-edge marker placement, popover text and row values, and k more text, dialog contents, sort, export and tracker links, focus return, help text under configured terms, loading and failed visuals, numbers unchanged on a live view. Evidence-capture owner: the candidate's declared probe set (nwdes host registration); examiner cannot add a probe.
```

observation_gap
