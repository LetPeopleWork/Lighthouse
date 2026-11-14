# Acceptance brief: story\-6502\-hide\-outliers

## Slices

None.

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
