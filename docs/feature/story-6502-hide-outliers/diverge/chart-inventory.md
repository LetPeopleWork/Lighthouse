# Chart inventory — can outliers distort it, and should it hide them?

Basis: `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` (`buildWidgetNodes`, the widget keys),
`components/Common/Charts/*`, `widgetInfoMetadata.ts`. Teams and Portfolios share `BaseMetricsView`, so a change in a
chart component lands on both. Charts were inspected for how their value axis is sized; a chart not opened line by line
is marked "not inspected".

How the scatterplot distorts today: `getMaxYAxisHeight` (utils/charts/chartAxisUtils.ts) sets the axis maximum to
1.1 x the largest of the data points, the percentile values and the SLE. One 600-day item therefore sets the axis for
everything. The same helper is used by the Work Item Aging, Feature Size and Estimation vs Cycle Time charts; the
Process Behaviour Chart does the same on its own (`yAxisMax = 1.1 x max(values, upper limit, average)`).

Verdict key: **HIDE** = clip by default with a disclosure. **SHOW** = leave as is. **OPT** = candidate for a later
slice, default show. **N/A** = no outlier can distort it (no value axis, fixed axis, or a single number).

## Dots-per-item charts (where an outlier is a mark)

| Widget key | Chart | Axis driven by | Can an outlier distort? | Verdict | Why |
|---|---|---|---|---|---|
| `cycleScatter` | Cycle Time scatterplot (CycleTimeScatterPlotChart) | max of data | Yes, strongly | **HIDE** (maintainer decided) | One old item squeezes every other dot and the 50/70/85/95 lines. The story's motivating case. |
| `aging` | Work Item Aging (WorkItemAgingChart) | max of data, minimum 5 | Yes | **SHOW** (maintainer decided) | The chart exists to find the stuck item. Hiding it defeats `job-flow-coach-spot-pace-outliers`. |
| `featureSize` | Feature Size scatterplot (Portfolios) | max of size / cycle time / age | Yes | **OPT**, default show | A very large Feature is what the chart is for (its RAG says "split the ones above the 70th / 85th percentile"). Reconsider after the scatterplot ships, maybe only on the cycle-time axis. |
| `estimationVsCycleTime` | Estimation vs Cycle Time | max of data | Yes | **OPT**, default show | Same shape as the scatterplot. The correlation it reports is rank-based (Spearman), so clipping the view does not touch the number. Cheapest second adopter. |
| `cycleTimePbc` and the other five PBCs (`throughputPbc`, `wipPbc`, `totalWorkItemAgePbc`, `featureSizePbc`, `arrivalsPbc`) | Process Behaviour Charts | max of values and upper limit | Yes | **SHOW, never hide** | A point beyond the natural process limit is the signal the chart exists to show. Clipping it would hide the very thing a PBC detects. |

## Counts and totals over time (an outlier is a tall bar or a spike)

| Widget key | Chart | Can an outlier distort? | Verdict | Why |
|---|---|---|---|---|
| `throughput`, `arrivals` | Run charts (BarRunChart) | Yes, a bulk-close day | **SHOW** for now | Bars start at zero; clipping a bar is the "broken bar" the dataviz sources warn against. A burst day is information, and the PBC reads it. Outlier here would mean something else (a count, not a duration) and needs its own definition. |
| `wipOverTime`, `blockedCountHistory` | Line / bar over time | Rarely | **N/A** | Counts in a narrow band. |
| `totalWorkItemAgeOverTime` | Line over time | Yes in principle (one ancient item lifts the sum) | **SHOW** | It is a sum; one item cannot be cut out of it without changing the number. |
| `stacked` | Simplified CFD | No | **N/A** | Cumulative areas. |
| `stateTimeCumulative` | Cumulative state time (bars per state) | A heavy item inflates a bar | **SHOW** | Bars start at zero and the chart already has an item picker and scope control to narrow it. |
| `percentilesOverTime` | Percentiles over time (lines) | The p95 line can jump | **SHOW** | The line is the statistic; an outlier moving it is the information. Nothing to clip. |

## Single numbers, matrices and part-of-whole

| Widget key | Chart | Verdict | Why |
|---|---|---|---|
| `percentiles`, `workItemAgePercentiles`, `featureSizePercentiles`, `totalThroughput`, `totalArrivals` and the other tiles | Numbers | **N/A** | No axis. These must keep including outliers (they are statistics). |
| `loadBalanceMatrix` | Matrix with today point | **N/A** | One point against a baseline. |
| `workDistribution` | Pie by parent Feature | **N/A** | Part of whole. |
| `predictabilityScore`, `predictabilityScoreDetails` | Score and spread | **N/A** | Derived from forecast percentiles. |

## Delivery charts (Portfolio, Delivery metrics tab)

| Chart | Verdict | Why |
|---|---|---|
| DeliveryFeverChart | **N/A** | Both axes fixed at 0-100. |
| DeliveryBurnupChart | **N/A** | Scope and done lines; scope is the point. |
| DeliveryPredictabilityChart | **N/A** | Likelihood axis fixed at 100; the far-future case already has its own "beyond the horizon" handling. |
| DeliveryEpicSizeChart (bars per Feature) | **SHOW** | Bar length is scope; a very big Feature is the news. Not inspected beyond its band axis. |

## Not inspected, outside this story

`BacktestResultDisplay` (Team forecast backtest), `RefreshHistoryChart` (System settings, not a metrics view).
DISCUSS should glance at the backtest chart.

## Summary

- HIDE: **1** (cycle time scatterplot). SHOW (decided): Work Item Aging. OPT later: Estimation vs Cycle Time, Feature
  Size. Never: the six PBCs. Everything else N/A or SHOW because the chart *is* the statistic or starts at zero.
- So the story is mostly one chart plus a reusable mechanism for two more. Build the mechanism in a shared place
  (the axis helper and one disclosure component), not inside the scatterplot file, so the OPT charts adopt it with
  a prop. That is a DESIGN call; flagged here only so DISCUSS does not size it as "scatterplot only".
