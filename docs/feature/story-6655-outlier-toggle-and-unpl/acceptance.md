# Acceptance brief: story\-6655\-outlier\-toggle\-and\-unpl

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A reader can switch outlier handling off and then sees every closed Work Item drawn on the Cycle Time scatterplot.
```

#### E2

##### Promises

P1

P4

```text
With outlier handling on, the Work Items set apart as outliers are exactly those the Cycle Time process behaviour chart of the same Team and date range shows above its UNPL; none above it is drawn, none at or below it is set apart.
```

#### E3

##### Promises

P2

```text
Every time the chart opens, or the Metrics tab is reloaded after switching outlier handling off, outlier handling is on: Work Items above the UNPL are not drawn and the outlier chip counts them.
```

#### E4

##### Promises

P3

```text
Switching outlier handling off draws all closed Work Items including the former outliers, raises the y-axis to fit the tallest, and removes the outlier chip; switching it on again restores the chip and the shorter axis.
```

#### E5

##### Promises

P4

```text
With an UNPL of 30 days and Cycle Times of 12, 30, 31 and 90 days, the 31 and 90 day items are outliers and the 12 and 30 day items are drawn; the outlier list and help call the limit the upper natural process limit, never 5 times the median.
```

#### E6

##### Promises

P5

```text
When the process behaviour chart has no limits (invalid baseline such as under 14 days, or fewer than 2 closed Work Items), every closed Work Item is drawn, no outlier count shows, and the reader can see that outliers need the process behaviour chart's limits.
```

## Charter

None.

## Examination

None.
