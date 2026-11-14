# Acceptance brief: story\-6502\-hide\-outliers

## Slices

### S1

##### Covers

P1

P5

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
The 58-day item is not drawn, the axis top is 1.1 times the largest drawn value or line, the header chip reads 1 outlier, the popover says 'Not drawn: longer than 30 days (5 times the median, 6 days).' and 'Still counted in the percentiles, SLE and View Data.', lists the item with closed date, 58 and 9.7x (9.7 times the median), and the percentile lines stay
```

```text
Typical work is readable, the extreme item is disclosed one click away and no statistic changes
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:shows the one 58\-day Work Item as an outlier in the header while the statistics stay the same

```text
Median taken from drawn items only, chip left in the legend row, item missing from the popover, or 9.67 rounded down to 9.6 would each fail an assertion here
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
The 95% and SLE lines lie above the cut (33 and 35 days) while a 58-day item is left out; the reader opens the chip
```

```text
The axis top follows the typical items (1.1 times 8), the two lines are not drawn in place, and the popover says 'Above the axis: 95% at 33 days, SLE at 35 days.'
```

```text
A line that no longer fits on the axis is still announced in words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:tells the reader about a percentile and an SLE line that lie above the axis

```text
An axis stretched to the 58-day item, or off-axis lines dropped without mention, fails here; marker geometry is not asserted in jsdom
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
Widget header chip reads 1 outlier or N outliers and is absent when there are none; it opens a popover saying the items are not drawn, stating the cut and the median in days, and listing each outlier with closed date, Cycle Time in days and multiple of the median; while loading or failed the chip is disabled and never shows a count from a previous answer.
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

None.
