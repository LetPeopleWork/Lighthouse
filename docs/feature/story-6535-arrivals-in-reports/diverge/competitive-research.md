# Competitive research: arrivals vs throughput, Then vs Now (Story 6535)

**Date**: 2026-11-15 | **Researcher**: nw-researcher | **Depth**: short, web-search level (pages mostly not fetched in full; snippets noted as such) | **Confidence**: Medium-Low overall

## Verdict
- No product found shows "arrivals vs throughput, Then vs Now" as a before/after comparison. Before/after of the balance is a **gap** in every product checked; you pick the date ranges yourself and compare by eye.
- The dominant representations are: (a) CFD, top-line slope vs bottom-line slope (Nave, ActionableAgile, Businessmap); (b) a per-period created/resolved bar or difference chart (Jira, JSM, Zendesk); (c) a single ratio or net number (Allstacks, xprocess "net flow", queueing utilisation).
- "Good" is nearly always stated as **balance (arrivals ~ departures)**, not as "lower arrivals". Arrivals > throughput = bad sign only when sustained.

## Findings

### Nave (flow metrics, CFD-based)
- Arrival rate = slope of CFD top line; throughput = slope of Done line; widening gap = arrivals outpacing delivery. Unit: items/day (example "0.24 tickets per day"). Source: https://getnave.com/blog/?p=6263 (snippet-level; its two worked examples use the same 87/363 figure for both metrics, so treat as illustrative, likely a copy error). Before/after: not shown (unverified; no period-compare feature found). Little's Law linked on the flow-metrics page.

### ActionableAgile (55 Degrees)
- CFD with "rate lines": top = arrival rate, bottom = departure rate. Parallel = stable, diverging = fewer finished than started, converging = more finished than started. Source: https://55degrees.atlassian.net/wiki/spaces/AAS/pages/2543550465/Cumulative+Flow+Diagram (via search snippet; official exact rate calculation not found). Chart only, no ratio/verdict number found. Before/after: user picks the window; no side-by-side (unverified).
- Third-party worked example (Scott Logic): 3.16 arrivals/day vs 3.63 departures/day, called "converging". https://blog.scottlogic.com/2020/02/29/agile-progress-and-quality-reporting.html

### Businessmap (Kanbanize)
- CFD "Rate Lines" give Arrival Rate (items/day) and Throughput (items/day) for the selected period; since Analytics v11 arrival = all started items (entered In Progress), independent of the selected workflow stages. Source: https://knowledgebase.businessmap.io/hc/en-us/articles/360015034420 and https://businessmap.io/kanban-resources/kanban-analytics/cumulative-flow-diagram . Widening bands = bottleneck. Before/after: only by changing the period (unverified beyond that).
- Note: Businessmap defines arrival as **started**, not **created**. This is a real definitional fork for Lighthouse (created vs started).

### Jira (Created vs Resolved Issues report) and JSM
- Difference chart of created vs resolved per period; red area where created > resolved, green where resolved > created; option "Cumulative Totals?" only accumulates each series, it does not net them. Sources: https://confluence.atlassian.com/spaces/JIRA050/pages/284367567/Created+vs+Resolved+Issues+Report ; community confusion on cumulative: https://community.atlassian.com/t5/Jira-questions/JSD-report-to-the-subtraction-between-number-of-resolved-issues/qaq-p/1238187
- Colour = fixed direction (resolved > created good). Before/after: pick date range; no comparison of two ranges (unverified).
- Closest to the customer's wording: "if created > resolved, bad sign".

### Azure DevOps
- No built-in created-vs-closed or arrival widget found. CFD widget (counts per board column over time), Lead Time / Cycle Time widgets (scatter). Microsoft states CFD gives no discrete lead/cycle values. Source: https://learn.microsoft.com/en-us/azure/devops/report/dashboards/cumulative-flow-cycle-lead-time-guidance?view=azure-devops . Arrivals are only readable from the CFD top band. Before/after: none found.

### Allstacks / Swarmia / LinearB / Jellyfish
- Allstacks "Issues Created vs. Completed": compares creation rate with resolution in a period; **ratio = created / completed**; backlog grows when completed < created. https://allstacks.atlassian.net/wiki/spaces/APG/pages/3408756799/Issue+Created+vs.+Completed (snippet-level; before/after and colouring unverified).
- Swarmia: completed-issue count/throughput and average WIP per bucket; no created series found. https://help.swarmia.com/features/metrics/issue-cycle-time . LinearB throughput = completed in timeframe: https://linearb.helpdocs.io/article/tpmxyz5ei7-throughput-metrics . Jellyfish advises open-vs-closed per week as practice, not a named chart: https://jellyfish.co/library/jira-metrics/ . All three: no arrival metric found (absence of evidence from search only).

### Practitioners (Vacanti, Little's Law)
- Little's Law needs average arrival = average departure over the window; if arrivals exceed departures WIP grows and the system is not in steady state. Sources: https://capgemini.github.io/agile/Littles-Law-and-KanBan ; Croker on Vacanti's assumptions https://medium.com/@matthewcroker/uncovering-littles-law-exploring-assumptions-and-violations-2-in-series-dbf82878c666 ; "Little's Inequality" and **net flow** (delivery bias) over time: https://xprocess.blogspot.com/2015/07/littles-inequality.html
- Vacanti's own text was not fetched; claims are via secondary sources. Prateek Singh: **no source found** (gap).
- StoryPointLab: qualitative only, "sustained" arrivals above throughput, balance is healthy, no formula, no before/after. https://www.storypointlab.com/blog/flow-metrics/arrival-rate-vs-throughput
- Per-step view: one step's throughput is the next step's arrival rate (https://kanbantool.com/kanban-guide/advanced-kanban-metrics, snippet-level).

### Non-obvious alternatives
- **Zendesk Backlog Evolution**: backlog(t) = backlog(t-1) + new tickets - solved tickets. Net-flow accumulated into a backlog line. https://support.zendesk.com/hc/en-us/articles/4409155859610 (URL from search; page not confirmed to be this report, verify).
- **eazyBI "Open issues"** = created and still unresolved at period end, i.e. running created - resolved. https://docs.eazybi.com/eazybi/learn-more/learn-eazybi-through-sample-reports/samples-jira-issues/created-vs-resolved-issues-over-time
- **Queueing utilisation** rho = arrival rate / service rate (= the Allstacks ratio). rho < 1 stable, rho >= 1 queue grows without bound. Standard queueing theory (e.g. https://classes.engineering.wustl.edu/cse538-online/images/f/f4/Chapter0.pdf, found by search, not read). Ratio is scale-free, so Then vs Now of rho is directly comparable across windows of different length.
- **Net flow** (arrivals - departures per period) is the signed version; the accumulated net flow equals the WIP change (xprocess link above).

## Knowledge Gaps
- Before/after (period vs period) of arrivals vs throughput: searched Nave, AA, Businessmap, Jira, ADO, Allstacks; found none. Recommendation: treat as differentiator, validate with the customer.
- Prateek Singh and Vacanti primary texts: not found/not fetched.
- ServiceNow built-in inflow vs closed report: not found.
- Nave/AA/Businessmap behaviour inferred from docs snippets; no product trial.

## Comparison table
| Product | Values | Visual | Before/after | Good direction | Source |
|---|---|---|---|---|---|
| Nave | arrival and throughput rate, items/day | CFD slopes | no (unverified) | balanced; gap widening bad | getnave.com/blog/?p=6263 |
| ActionableAgile | arrival and departure rate | CFD rate lines | window only | parallel = stable | 55degrees CFD page |
| Businessmap | arrival (started) and throughput, items/day | CFD rate lines | period only | balanced, bands widen bad | knowledgebase 360015034420 |
| Jira / JSM | created and resolved count per period (optional cumulative) | difference chart, red/green | no | resolved >= created | confluence JIRA050 |
| Azure DevOps | none direct | CFD, scatter | no | n/a | learn.microsoft.com CFD guidance |
| Allstacks | created/completed ratio | metric | unverified | ratio < 1 (backlog shrinks) | allstacks APG page |
| Zendesk / eazyBI | backlog = prior + new - solved | backlog line | no | flat or falling | zendesk 4409155859610, eazyBI docs |
| Little's Law / queueing | net flow, utilisation rho | none (theory) | n/a | arrival ~ departure, rho < 1 | capgemini, xprocess, wustl |
