# Job Analysis — story-6535-arrivals-in-reports (DIVERGE)

ADO: User Story #6535 "Show Arrivals in reports" (no parent Epic). "Reports" here means the **Then & Now** report
(Epic 5878): a frozen Then window against a rolling Now window, one panel per ticked metric.

## 1. Evidence (from DISCOVER, recorded in `docs/product/jobs.yaml`)

| # | Source | Says | Kind |
|---|---|---|---|
| E1 | Requester, Productboard ideas board, 2026-11-03 (relayed by a partner) | "Have the option to show Arrivals in the reports as well." | Request, second-hand |
| E2 | Requester, direct answer to the maintainer, ~2026-11-03 | "Number of arrivals in the given timeframe. What I do want to compare is arrivals vs. throughput. If arrivals is bigger than throughput, bad sign." | First-hand, n=1 |
| E3 | Maintainer, 2026-11-15 | The comparison that matters is **Then against Now**, not arrivals Now against Throughput Now. Show total and average per day; max and median per day may add nothing. A comparison could be average arrivals against average Throughput, and whether the gap gets closer to 0. | Product owner |
| E4 | Epic 5878 DIVERGE/DISCUSS | Arrivals and "arrivals balanced with Throughput" were parked as nice-to-haves, "one catalog registration later, no model change" (`epic-5878-baseline/recommendation.md:104,132`; `feature-delta.md` D39). | Prior desk work |

## 2. The job

No new job. This serves **`job-flow-coach-show-whether-flow-changed`** ("set today's flow against a before that
cannot move … so I can say with evidence whether flow changed"). The request adds one dimension of "flow": is
work coming in faster than it goes out, and has that changed since the intervention?

Tactical ask (E1): "show arrivals". Physical job (E2, E3): **show whether the balance between work arriving
and work finishing got better since Then.** Arrivals alone are not good or bad ("it depends", maintainer);
only their relation to Throughput carries a direction.

- **Functional**: for the Then window and the Now window, how many Work Items arrived and finished per day, and
  whether arrivals outrun Throughput by less (or not at all) Now than Then.
- **Emotional**: the coach can explain a growing WIP or Work Item Age without blaming the Team: "more arrived
  than you could finish; that gap halved".
- **Social**: a sponsor sees that demand is part of the flow story, not only the Team's speed.

## 3. Outcome statements (ODI)

1. Minimise the time it takes a reader to tell whether more Work Items arrived than finished, in each window.
2. Minimise the likelihood that a reader compares totals from windows of different length (Then 90 days, Now
   30 days) and draws a wrong conclusion.
3. Minimise the likelihood that a reader reads "more arrivals" alone as good or bad news.
4. Minimise the effort to see whether the arrivals-to-Throughput balance improved from Then to Now.
5. Minimise the number of extra steps to include it when creating a report.

## 4. Constraints from the current report (checked in code)

- Panels compare **per day** because the windows can differ in length; the total is context
  (`ThroughputPanel.tsx`: change line `perDay` only; `ThroughputReportMetric.cs`: total and perDay).
- A change has a direction of good of `"lower" | "higher"` only (`thenNowComparison.ts:8`). "Closer to zero"
  does not exist yet.
- Metrics are chosen by checkbox in `MetricsAndNameStep.tsx`; a panel draws only from its own props
  (`reportPanelRegistry.ts`), so one panel cannot depend on another metric being ticked.
- Then is frozen at creation: existing reports will show arrivals as "not captured".
- No charts, no verdicts on the report (Epic 5878 D39).
