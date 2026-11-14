# Product brief: story\-6502\-hide\-outliers

## Request

```text
ADO Story 6502 Hide Outliers: on the Cycle Time scatterplot, hide exceptionally long closed Work Items so typical work stays readable, while the outliers stay known, one click away, and included in every statistic.
```

## Work

M

```text
A flow coach reads the picture of normal work on the Cycle Time scatterplot without one extreme item squashing it, and still knows which items were left out and why.
```

### Rework

None.

## Promises

### P1

Outcome

```text
On the Cycle Time scatterplot of a Team or a Portfolio, a closed Work Item whose Cycle Time is more than 5 times the chart's 50th percentile is not drawn; the y-axis scales to the remaining items, and every percentile and SLE line stays visible, either in place or as a labelled marker at the top edge of the plot. With no such item the chart is exactly as before.
```

### P2

Outcome

```text
A chip in the widget header reads '1 outlier' or 'N outliers' (absent when there are none) and opens a popover that says the items are not drawn, states the cut and the median in days, and lists the outliers longest first, up to ten rows followed by 'and k more', each row with its closed date, its Cycle Time in days and how many times the median it is. While the widget loads the chip is disabled and shows no count; when the data cannot be loaded the could-not-load message replaces the plot and the chip; it never shows a count from a previous answer.
```

### P3

Outcome

```text
The chip and its list stay truthful while the reader changes the view: they count only outliers of visible Work Item types, follow a selected named Cycle Time and the date range, and the cut and axis do not move when a type is toggled; when every visible item is an outlier or every type is hidden the plot says so instead of 'No data available'.
```

### P4

Outcome

```text
From the popover the reader opens the outliers in the existing Work Items list dialog (sort, export, tracker links), and the widget's help explains the outlier rule in the reader's configured terms.
```

### P5

Outcome

```text
No number changes: the percentiles, the SLE share and RAG, the tiles, View Data and the forecasts still include the outliers.
```

## Scope

None.

```text
Other charts (Estimation vs Cycle Time, Feature Size, Work Item Aging, process behaviour charts, throughput), a change to View Data, a change to the backend percentile convention, and showing outliers in place on the chart.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Slice 01: on Team Zenith's demo data the scatterplot leaves out TZ-301, scales its y-axis to the other items, shows lines above the axis as top-edge markers, and a header chip '1 outlier' opens a popover listing TZ-301 with 58 days and 9.7x the median; the statistics are unchanged.
```

#### Promises

P1

P2

P3

P5

#### Depends on

None.

#### Slice work

M

```text
Walking skeleton: typical work readable, outliers disclosed in the header chip and popover.
```

##### Rework

None.

### S2

```text
Slice 02: the outlier chip follows the type chips, a named Cycle Time and the date range, the cut and axis stay put when a type is toggled, and an all-outlier or all-hidden view says so instead of 'No data available'.
```

#### Promises

P3

#### Depends on

S1

#### Slice work

S

```text
The chip stays truthful whatever the reader filters.
```

##### Rework

None.

### S3

```text
Slice 03: 'Open in a list' in the popover opens the Work Items dialog with exactly the chip's outliers, focus returns to the chip, and the widget's help names the outlier rule in the configured terms.
```

#### Promises

P4

#### Depends on

S1

#### Slice work

S

```text
Sorting, export and the full list of outliers, and the rule explained in the help.
```

##### Rework

None.
