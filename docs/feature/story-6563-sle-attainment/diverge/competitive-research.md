# Competitive scan — story-6563-sle-attainment (DIVERGE)

Desk research with WebSearch and WebFetch, run directly (no researcher dispatch). No product was tried. A row says
"read" when the page text was fetched, and "search summary only" when only a search result described it. Searches ran
2026-11-27.

## Prior art in this repo

- Percentiles Over Time (`PercentileSnapshotWriter`, `OverTimeHistoryFiller`, `GapAskingPercentilesOverTimeSeriesQuery`,
  controllers for Team and Portfolio): a daily snapshot, three Cycle Time horizons (30/60/90 days), past days filled in
  from stored Work Items against today's configuration, no point on a day with nothing to measure.
- The status tooltip of Cycle Time Percentiles carries the share today (`computeCycleTimePercentilesRag`).
- `SleRiskWidget`: a count tile with a dash instead of zero when no target exists.
- Trend indicator on Cycle Time Percentiles: compares the selected window with the same-length preceding period
  (`docs/metrics/flow-overview.md`).
- UI surface (ADR-271): Team and Portfolio metrics are tabs of the unit's page. Nothing here belongs in Organisation,
  Access or System. The SLE is set on the Team or Portfolio edit page.

## Products

| Product | How it shows the SLE and whether it is kept | Does well | Fails the job | Assumption about the reader |
|---|---|---|---|---|
| ActionableAgile (via its docs mirrored on a [partner wiki](https://55degrees.atlassian.net/wiki/spaces/AAS/pages/2324987905/Cycle+Time+Scatterplot); page fetch returned only navigation, so search summary only; vendor help domain not reachable) | A percentile line on the Cycle Time Scatterplot is highlighted as the SLE; "NO SLE" clears it; trend lines for the average and the 50/70/85/95 lines. | The SLE is one chosen line on the picture the coach already reads. | No share of items within the SLE and no attainment over time found in the summary. | The coach reads dots against the line. |
| Nave (read: [Cycle Time Scatterplot page](https://getnave.com/cycle-time-scatterplot)) | Dotted percentile lines: "This line marks the cycle time that 85 of every 100 items came in under." A trendline and a Percentiles widget show "how your cycle time percentiles change over time." The page calls the lines a basis for "service level agreements", not SLEs. | Plain-language reading of a line. | "No met/not-met indicator or percentage of items within a target is described." | The coach reads dots against lines. |
| Businessmap / Kanbanize (read: [SLE blog](https://businessmap.io/blog/kanban-service-level-expectations); search summary only: [Exceeded SLE Scatter Plot](https://knowledgebase.businessmap.io/hc/en-us/articles/4407574637074), [Exceeded SLE Heat Map](https://knowledgebase.businessmap.io/hc/en-us/articles/4408911381778), which returned 403 on fetch) | A progress bar on each card against its SLE: "a part of the progress bar will turn red, showing you how much the SLE has been surpassed". Separate charts show how often and by how much SLEs are exceeded; the heat map is described as giving the "percentage this number represents out of all cards". Their SLEs are per column or swimlane, not one for the whole item. | The only product found with a chart built for breaches. Wording "10 days or less" is in its blog example. | The blog says to refine the SLE "by monitoring the Scatterplot chart"; it describes no share and no trend over time. | The coach watches single cards or breaches. |
| Jira Control Chart (read: [Atlassian docs](https://confluence.atlassian.com/jirasoftware/control-chart-1688899022.html)) | A rolling average line and a shaded band. The page read at the cited URL says the window is "20% of the total issues displayed (always an odd number and a minimum of 5 issues)". A search summary of the Jira Cloud page (not read) gives a different window: the issue plus four before and four after. | The rolling window is counted in issues, with a minimum of 5, so it is built for few items. | The page names no target and no share within a target. | The coach reads a smoothed mean. |
| Non-obvious: SRE SLO dashboards (read: [Grafana SLO dashboard](https://grafana.com/docs/grafana-cloud/observe-and-act/alert-and-measure-reliability/slo/dashboard.md)) | "The SLI panel shows how the SLI value changes over time for the selected time range." "When the SLI drops below the SLO objective, the chart highlights these intervals in red." A second panel, "Error Budget Burndown", shows the remaining budget. | The same shape as this job: a share of good events against an objective, as a line over time, with the objective drawn and the breach marked. | It assumes very many events per window. Lighthouse has about a dozen closed Work Items. | The reader trusts the share because the sample is large. |
| Non-obvious: Google SRE Workbook (read: [Implementing SLOs](https://sre.google/workbook/implementing-slos/)) | SLOs "can use either a rolling window or a calendar-aligned window". "We have found a four-week rolling window to be a good general-purpose interval." "Reports and dashboards that provide in-time snapshots of the SLO compliance"; a compliance report shows whether SLIs trend up or down against the previous quarter. | Documents both ways of doing "over time": a rolling window and a calendar period, and a period-against-period comparison. | On few events: a customer with very few requests can show "either 100% availability or very low availability, since a single failure can be a large share"; it advises watching aggregates. No minimum count is given. | Aggregate over many units to avoid noise. |
| Practice, not a product (read: [Scrum.org, SLE and Scrum](https://www.scrum.org/resources/blog/kanban-service-level-expectations-and-how-use-them-scrum)) | The SLE is stated "85% of work items will be finished in 8 days or less". It is reviewed in the Sprint Retrospective: "the 'surprise' or 'anomaly' of missing your SLE can drive an improvement discussion." | Shows the reader who needs this job (the retro) and the "or less" wording. | The article "doesn't describe a formal attainment calculation". | The team judges misses one by one. |

## What this means for the options

1. Three flow-analytics products were read (Nave, the Businessmap blog, Jira); ActionableAgile by search summary only.
   They draw the SLE or a percentile as a line against dots. None of the pages read states the share within the target
   as a number. One page (Nave) says so. Not verified absent elsewhere.
2. Only Businessmap has a chart for breaches, and only as a count of exceedances per workflow stage.
3. The SRE sources document two ways to show "over time": a rolling window (daily value) and a calendar period.
   The Workbook compares a period with the previous one.
4. The SRE examples assume many events. The only small-sample statement found is a warning, with no threshold. Jira's
   control chart is the one source that sets a minimum count (5 issues).
5. Wording: the "or less" form appears in two texts read (Scrum.org, Businessmap).
