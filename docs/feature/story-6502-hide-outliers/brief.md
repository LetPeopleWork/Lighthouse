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
An amber outlined chip in the widget header reads '1 outlier' or 'N outliers' ('(!) N' below 600 px; absent when there are none); its accessible name says the items are not drawn and names any percentile or SLE line above the axis; clicking it opens the outliers directly in the Work Items list dialog, with no popover in between.
```

### P3

Outcome

```text
The chip and its list stay truthful while the reader changes the view: they count only outliers of visible Work Item types, follow a selected named Cycle Time and the date range, and the cut and axis do not move when a type is toggled; when every visible item is an outlier or every type is hidden the plot says so instead of 'No data available'.
```

### P4

Outcome

```text
The chip opens every outlier of the visible types in the existing Work Items list dialog (sort, export, tracker links), titled in the reader's terms, with focus back on the chip when it closes; the widget's help explains the outlier rule in the reader's configured terms.
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

```text
D13 (maintainer guided review 2026-11-14): the outlier chip is amber outlined (MUI warning colour on border, icon and text) at every width and while loading.
```

#### Status

Confirmed


```text
D14 (maintainer guided review 2026-11-14): the chip opens the outlier list dialog directly; the popover is removed, with no extra lines in the dialog; the rule stays in the (i) help.
```

#### Status

Confirmed


```text
D15: the usage event keeps 27 and is renamed OutlierListOpened, sent when the chip opens the dialog; no release has carried OutlierPopoverOpened.
```

#### Status

Confirmed


```text
D16: the chip's accessible name carries the lines above the axis ('{n} outlier(s) not drawn on the chart. Above the axis: 95% at {p95} days. Show the list.'), the middle sentence only when a line is above the axis.
```

#### Status

Confirmed


```text
D17: the 'times the median' multiple lived only in the popover table and is dropped with it; the maintainer chose the Work Items dialog as it is, with its own columns and no extra lines (D14).
```

#### Status

Confirmed


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

### S4

```text
Slice 04: on Team Zenith's demo data the header chip '1 outlier' is amber outlined, and clicking it opens the Work Items dialog listing TZ-301 directly, with no popover; closing the dialog returns focus to the chip; the usage event OutlierListOpened (27) is sent on open.
```

#### Promises

P2

P4

#### Depends on

S1

S3

#### Slice work

S

```text
The reader notices the outliers and reaches the list in one click
```

##### Rework

None.
