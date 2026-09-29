# Competitive Research: Strategy / Initiative-Level (FL3) Views on Top of Jira & ADO

**Date**: 2026-09-29 | **Researcher**: nw-researcher (Nova) | **Epic**: 5806 Initiatives (DIVERGE input)
**Confidence**: Medium overall (vendor pages are marketing; pricing often "contact sales")

Legend: **[V]** = verified on vendor page/docs during this session. **[I]** = inferred (from search snippets,
third-party pages, or our own reading); treat as lower confidence.

## Executive Summary

21 tools were examined (17 in depth). The strategy layer above the Epic — cross-team, hierarchy-aware — is
the most consistently monetised tier in this market: Jira Plans, JPD Premium, Linear Enterprise, Productboard
Enterprise, BigPicture Enterprise and Nooga Portfolio all gate it, so a Premium placement for Lighthouse follows
the norm. But what those layers show is thin. Forecasts are deterministic (planned dates, velocity, PI points),
and health is typed by a human (Linear, Focus). Where a tool does compute health, it uses people activity or
opaque ML (Swarmia, Allstacks).

Probabilistic forecasting exists, but either below the initiative level (55 Degrees: epics/versions; Nave and
Broken Build: any JQL scope with *pooled* throughput) or paced by FTE rather than item flow (Swarmia, June 2026:
the closest threat). No tool found rolls per-team feature Monte Carlo into an initiative likelihood. None
computes initiative health from flow metrics, or measures Flight Level 3 WIP. Lighthouse already computes the
hard part: per-team feature forecasts and Delivery likelihoods. Its best angles are a correct initiative
likelihood, computed flow health, FL3 metrics, a one-glance heatmap, and push delivery to execs. Priced flat, not
per developer.

## Method

Web search plus vendor-page fetches on 2026-09-29, using the queries requested ("flight level 3 tool jira",
"initiative forecasting monte carlo portfolio", "executive portfolio dashboard jira") and per-vendor queries.
Vendor pages are primary but commercially biased: claims are marketing, and an "absence" means the fetched page
did not mention it, not proof the product lacks it. Four pages were blocked (Businessmap KB 403, Jira Align help
403, appfire.com 429, BigPicture redirect); those findings are marked [I]. No trials or hands-on use.

## Tool-by-Tool

### 1. Nooga (Portfolio / Scale) — Azure DevOps extensions — user-named

- **What the strategy view shows** [V]: Nooga Portfolio "adds a strategic planning layer to Azure DevOps",
  "connecting your OKRs and epics to the teams executing them". It "implements OKRs as native Azure DevOps
  work items" linking enterprise OKRs → portfolio OKRs → epics → features; a "holistic overview" ("a single,
  real-time view of how strategy connects to execution"); a "dynamic, data-driven timeline that reflects the
  actual state of work in Azure DevOps ... no manual updates needed". [nooga.net/portfolio](https://www.nooga.net/portfolio),
  [VS Marketplace](https://marketplace.visualstudio.com/items?itemName=nooga.nooga-portfolio)
- Nooga Scale [V]: SAFe program board, dependency management (drag-and-drop), PI/team objectives with planned
  vs actual business value, ROAM risk board, "Flow Metrics, as recommended by SAFe 6.0". [nooga.net/scale](https://www.nooga.net/scale)
- Search-index descriptions also claim "shadow work detection" and "OKR pace tracking" [I] — not visible on the
  pages fetched; treat as roadmap/marketing copy.
- **Forecast**: none probabilistic found on any Nooga page [V: absence]. Timeline = planned dates from ADO.
- **Snappiness**: single roadmap/timeline + overview in ADO; data lives in ADO (no sync) [V].
- **Pricing** [V]: Scale €200/mo (Team, from 10 users), €550/mo (Business, 50), €1,300/mo (Enterprise, 100);
  **Portfolio is "Early Access", custom pricing** — i.e. the strategy layer is sold as a separate, higher product.
  [nooga.net/pricing](https://www.nooga.net/pricing)
- **Steal**: "no manual updates" — the roadmap is derived entirely from work-item state; OKRs as first-class
  items above epics. "Shadow work" (work not linked to any strategic item) is a strong exec hook.
- **Avoid**: SAFe-ceremony heaviness (PI overlays, ARTs) and ADO-only reach.

### 2. Businessmap (ex-Kanbanize) — Flight Levels originators' partner tool

- **Strategy view** [V]: FL3 is modelled as a "Management workspace" with a "Strategy board" split into
  "OKRs" and "Actions/Ideas"; an initiative can contain "outcomes" (objective ↔ key results). Portfolio boards
  link initiatives to strategic objectives and track "progress, blockers, and dependencies".
  [Strategy visualization](https://businessmap.io/flight-levels/strategy-visualization),
  [OKR visualization](https://businessmap.io/blog/okr-visualization)
- **Forecast** [V]: Monte Carlo ("What If Analysis"); "even a set of 20-30 completed items is sufficient";
  outputs like "85% probability that we will complete this project by November 20th".
  [Portfolio forecasting](https://businessmap.io/kanban-resources/portfolio-kanban/forecasting-portfolio-level)
  Whether the MC rolls up from child cards across multiple team boards into one initiative forecast could not
  be verified (knowledge-base article returned 403) [gap].
- **Snappiness**: card-on-board rollup (initiative card with child-progress bars), management dashboards [V: "Management Dashboards" listed].
- **Pricing** [V]: "Simple Pricing. No Tiers" — Standard vs Enterprise, both include OKRs/KPIs, Management
  Dashboards, "Project Forecasting"; prices via sales. Not a premium upsell — strategy is the core pitch.
  [Plans & pricing](https://businessmap.io/plans-pricing/)
- **Steal**: FL3 vocabulary (initiative = objective, outcomes = KRs) aligned with Flight Levels.
- **Avoid**: requires moving work into Businessmap boards; for Jira/ADO shops the strategy layer is a second system.

### 3. Jira Plans (ex-Advanced Roadmaps) — Atlassian

- **Strategy view** [V]: cross-team timeline; "add more hierarchy levels to your plan--like initiatives";
  roll-ups of estimates and dates; dependencies "even if they live outside your plan"; capacity "based on your
  team's capacity and velocity"; scenario sandbox. [Atlassian Support](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-roadmaps/)
- **Forecast**: deterministic (velocity/capacity auto-scheduling), no probabilities [V: absence on docs page].
- **Snappiness**: one Gantt-style timeline; not a one-glance exec page — it is a planner's tool [I].
- **Pricing** [V]: "Jira Cloud Premium and Enterprise" only — the classic tier upsell; hierarchy above Epic is
  the premium hook.
- **Steal**: the hierarchy-above-epic as a premium gate works commercially — Atlassian proved buyers pay for it.
- **Avoid**: plans rot because dates/estimates are manually maintained; a velocity-based schedule looks precise and is not.

### 4. Jira Align / Atlassian Focus / Strategy Collection — Atlassian enterprise

- **Jira Align** [V]: "a single view of progress, risks, and dependencies across portfolios and teams"; "Map
  capacity and investment to your highest-priority work"; "improved delivery health quarter over quarter".
  [atlassian.com/software/jira-align](https://www.atlassian.com/software/jira-align).
  Portfolio room = information radiator incl. whether "emerging spend is aligned with target funding levels";
  forecasted spend = PI forecasts in points × spend-per-point; investment guardrails per theme [I: search
  snippets of help.jiraalign.com + Atlassian Community; help page returned 403]
  ([Portfolio room](https://help.jiraalign.com/hc/en-us/articles/115000095234-Portfolio-room),
  [Financials in Jira Align](https://community.atlassian.com/forums/Jira-Align-articles/Financials-in-Jira-Align/ba-p/2213320)).
- **Focus** (GA 2025-02-25 [I]) [V]: "a central hub for leaders to map and track goals, work, teams, and funds";
  "focus area maps" = high-level view with drill-down; "AI-powered summaries highlighting at-risk items";
  Slack/Teams alerts; health via progress + qualitative team updates.
  [Focus announcement](https://www.atlassian.com/blog/focus/announcing-focus-atlassians-new-enterprise-strategic-portfolio-management-offering)
- Bundled as the **Strategy Collection** (Focus + Talent + Align) — "Enterprise-scale strategic planning"
  [V] [Strategy Collection](https://www.atlassian.com/collections/strategy). Pricing: contact sales [V].
- **Forecast**: story-point/PI-based deterministic forecasts; no Monte Carlo found [I].
- **Steal**: "focus area map" drill-down tree; AI summary of at-risk items; push alerts where execs live.
- **Avoid**: enterprise-sales weight, SAFe dependence, points × cost maths; health that is largely *self-reported*.

### 5. Azure DevOps Delivery Plans / portfolio backlogs — Microsoft (built-in)

- **Strategy view** [V]: Delivery Plans show multiple team backlogs on a calendar with "rollup progress" of
  Features/Epics — progress bars, counts, numeric sums.
  [Display rollup](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/display-rollup?view=azure-devops),
  [Delivery plans](https://learn.microsoft.com/en-us/azure/devops/boards/plans/review-team-plans?view=azure-devops)
- **Limits** [V]: "In Delivery Plans, child items from other projects aren't included in rollup calculations";
  rollup is Analytics-backed and "can cause temporary display latency". 20-team/backlog cap per plan [I: search snippet].
- **Forecast**: none (dates are planned iteration/target dates) [V: absence].
- **Pricing**: included with Basic access — no upsell; this is the "free baseline" Lighthouse must beat [I].
- **Steal**: nothing new; **exploit**: the cross-project rollup gap is exactly where multi-team Lighthouse Features live.

### 6. 55 Degrees — ActionableAgile + Portfolio Forecaster

- **Strategy view** [V]: Portfolio Forecaster forecasts "Jira epics and versions with confidence using
  probabilities" from "your team's actual delivery history"; "combine forecasts from multiple teams into one
  clear view"; Confluence embedding. [Portfolio Forecaster](https://www.55degrees.se/products/portfolioforecaster)
- Monte Carlo "taking into account how many are in progress at one time and your historical Throughput" [I: search snippet of same site].
- **Pricing** [V]: Jira Cloud app via Atlassian Marketplace; separate product from ActionableAgile (i.e. a second SKU).
- **Closest methodological competitor** to Lighthouse (same Vacanti-style flow-metrics lineage). Stops at
  Epic/Version level; no initiative (level above epic) rollup, no OKR/investment/health layer found [V: absence].
- **Steal**: Confluence embed of a forecast — execs read Confluence, not tools.
- **Avoid**: Jira-only for the portfolio product; ADO users get ActionableAgile only.

### 7. Planview AgilePlace (+ Planview Portfolios / Advisor)

- **Strategy view** [V]: native "connected Objectives and Key Results (OKRs)"; "Intelligent Dependency
  Management"; "Card and Board Health"; "rolling up data to team, program, and agile release train boards";
  Jira and Azure DevOps integrations. [AgilePlace](https://www.planview.com/products-solutions/products/agileplace/)
- **Forecast**: none probabilistic on the AgilePlace page [V: absence]; Monte Carlo lives in *Planview Advisor*,
  a separate R&D portfolio product [I: third-party snippet]. Planview Portfolios = cost/budget forecasting [I].
- **Pricing**: separate pricing page; enterprise tiers [I].
- **Steal**: "board health" as a first-class computed concept.
- **Avoid**: product sprawl — strategy, flow, and forecasting split across three SKUs.

### 8. IBM Targetprocess (Apptio)

- **Strategy view** [V]: "connect strategic priorities to initiatives, delivery teams, capacity, and investment";
  "visibility into dependencies, progress, capacity, and risk"; "flexible funding and allocation models";
  intake/prioritisation of epics and initiatives. [Targetprocess portfolio](https://www.apptio.com/products/targetprocess/portfolio-management/)
- Forecasts align "budgets and forecasts to live portfolio execution using real-time work, velocity, and
  capacity data"; SPM "Control Center" with priorities dashboard, funding requests, delivery alerts [I: search
  snippet of apptio.com]. Forecasting = velocity/cost, not probabilistic [I].
- **Pricing**: enterprise, contact sales [I]. Strategy/finance is the core product, not an add-on.
- **Steal**: "delivery alerts" — push, not pull. **Avoid**: finance-first framing that needs cost models.

### 9. Nave (Jira analytics)

- [V]: "$100 per board/month" / "$1,000 per board/year", "One price per board. Every feature included":
  Executive Dashboard, Continuous Forecasting Dashboard ("deadline probability assessment"), Monte Carlo, AI insights.
  [Nave pricing](https://getnave.com/premium)
- Monte Carlo can draw throughput from "another board, project, release, epic, initiative, or custom JQL" and
  aggregate multiple boards for portfolio-level forecasting [I: search snippet]. Exec dashboard contents not
  described on the page fetched [V: absence]. [Dashboard for Jira](https://getnave.com/dashboard-for-jira)
- **Steal**: per-board simple pricing; "continuous forecasting" (the forecast is always live, not a one-off run).
- **Avoid**: chart-catalogue UX — many charts, no single "is strategy on track?" answer.

### 10. Swarmia (engineering intelligence) — **the closest new threat**

- **Initiatives view** [V]: "all your initiatives at a glance", "see all the information you need for board
  updates", "spot the initiatives ... that need your attention", status "based on data from your issue tracker
  and source code hosting", "thousands of tickets in one single view". [Swarmia Initiatives](https://www.swarmia.com/product/initiatives/)
- **Forecast** [V]: since 2026-06-30 "runs a Monte Carlo simulation on your team's recent activity to estimate
  the most likely completion date and the remaining FTE for any in-progress issue or initiative"; p50/p80/p90
  for date *and* effort; adjust scope, focus allocation, sample window; "turn the forecast into a target date in
  one click". [Changelog 2026-06-30](https://www.swarmia.com/changelog/2026-06-30-initiative-forecast/).
  Earlier (2024) version used "total number of epics, average epic cycle time, and average number of epics in progress" [I: snippet of
  [2024 changelog](https://www.swarmia.com/changelog/2024-08-13-initiative-forecast/)].
- **Investment balance** [V: search snippet of swarmia.com]: roadmap vs maintenance vs unplanned; initiative A vs B;
  cost via loaded engineer cost. [Investment balance](https://www.swarmia.com/product/investment-balance/)
- **Pricing** [V]: Standard $45/dev/mo, Enterprise $55/dev/mo; Initiatives + Investment balance in paid plans.
  [Swarmia pricing](https://www.swarmia.com/pricing/)
- **Steal**: forecast → target date in one click; forecast of *effort* alongside date.
- **Avoid / exploit**: forecast is FTE/effort-paced (people-centric, needs git + contributor mapping) — not
  flow-of-items; priced per developer, so costly for large orgs whose execs only want the top view.

### 11. Jellyfish (engineering management platform)

- [V]: allocation "across teams, individuals, investment categories, epics, and initiatives"; "Executive-ready
  dashboards" tracking "progress against investment targets"; "patented multi-source data allocations model"
  from Jira, Azure DevOps, Linear, Git. [Business alignment](https://jellyfish.co/solutions/business-alignment/)
- Scenario Planner models trade-offs in "hours and days, not story points" [V]; the claim that it yields
  "probabilistic forecasts" appears only in third-party snippets [I]. [Scenario Planner](https://jellyfish.co/solutions/scenario-planner/)
- **Pricing**: demo/sales only [V: absence]; Scenario Planner described as "an addition" — likely add-on [I].
- **Steal**: "board-ready" as a design brief — one page a CTO can paste into a board deck.
- **Avoid**: surveillance-adjacent people metrics; allocation depends on inference from commits.

### 12. Linear Initiatives (native in a tracker Lighthouse connects to)

- [V]: initiatives group projects around company objectives; "Initiative Health" = "on track, at risk, or off
  track"; roadmap timeline colourable by health/priority/milestone progress; target dates.
  [Linear Initiatives](https://linear.app/docs/initiatives)
- **Health is manual** [V]: "Select a health indicator—On track, At risk, or Off track"; no computed prediction.
  [Initiative and project updates](https://linear.app/docs/initiative-and-project-updates)
- **Pricing** [V]: initiatives on all paid plans; "Initiative views ... Enterprise plan, only"; team
  initiatives Business+ — the filtered exec view is the upsell.
- **Steal**: weekly written update ritual + health colour on a timeline; auto-appended target-date changes.
- **Exploit**: Lighthouse can *compute* the health Linear asks humans to type — and read Linear initiatives as the FL3 parent.

### 13. Allstacks (engineering intelligence)

- **Portfolio Report** [V]: "summarized Milestone report" giving executives "a high-level look across key roadmap
  items and teams"; columns = forecast date vs target end date, progress bar split into completed /
  estimated-incomplete / unestimated, on-track/off-track risk; tabs for forecast, burndown, velocity; click-through
  to Milestone Report with "scope, contributor activity, velocity, and related alerts".
  [Allstacks portfolio report](https://www.allstacks.com/blog/executive-insights-portfolio-report)
- **Forecast** [V]: "proprietary algorithm", "AI/ML models of historical trends of team activity", processed daily —
  a black box; not explainable percentiles.
- **Pricing**: sales-led [I].
- **Steal**: forecast-vs-target *gap* as the headline column; progress bar that shows *unestimated* scope honestly.
- **Avoid**: opaque ML — execs cannot defend a date they cannot explain.

### 14. Broken Build — Agile Monte Carlo Charts (Jira app)

- [V]: forecasts "Projects, Releases, Epics, Initiatives" or JQL; pools sources into "a single Monte Carlo
  simulation ... across teams, programs, or the entire portfolio"; "When" and "How many" charts; P50/P70/P85/P95
  bands, RAG indicators, CSV/PNG/PDF export; free ≤10 users, seat-tiered above.
  [Agile Monte Carlo Charts](https://www.brokenbuild.net/apps/agile-monte-carlo-charts)
- **Weakness (interpretation)**: pooling throughput across teams into one simulation assumes any team can do any
  item — the classic multi-team MC error that Lighthouse's per-team feature forecasting avoids.
- **Steal**: RAG derived from percentile vs target (computed, not typed); PDF export for exec decks.

### 15. Kiplot (SPM on Jira/ADO)

- [V]: "Ready-built and fully configurable dashboards" per role incl. C-suite; "At risk" flags; AI "continuously
  monitors the portfolio for dependency conflicts, capacity overcommitment, and delivery drift"; budget vs actual,
  capacity, OKR alignment; what-if "across funding, prioritization, and resource allocation".
  [Kiplot SPM](https://www.kiplot.com/solutions/strategic-portfolio-management)
- Jira live pull; ADO "true bidirectional sync" [I: search snippets of kiplot.com].
- **Forecast**: scenario/what-if, not probabilistic [V: absence]. **Pricing**: demo only [V].
- **Steal**: "delivery drift" as a named, detected signal. **Avoid**: full-PPM scope (intake, benefits, lessons learned).

### 16. Easy Agile Programs (Jira app)

- [V]: program board, PI objectives (committed/uncommitted) linked to work, "instant insight into the health of
  dependencies" colour-coded (red conflict, orange at-risk timing, green healthy), confidence vote at PI close.
  Pricing via Marketplace. [Easy Agile Programs](https://www.easyagile.com/products/programs)
- **Forecast**: none [V: absence]. **Steal**: dependency health computed from *scheduled* dates (a cheap, legible
  signal). **Avoid**: PI-ceremony coupling.

### 17. BigPicture (Appfire) — PPM/SPM for Jira

- OKR module linking stories/epics/initiatives to outcomes; risk matrices + heatmaps across initiatives and
  portfolios; program board; RICE/WSJF/ICE prioritisation; separate "BigPicture Enterprise" edition for OKRs,
  budgets, risk [I: Marketplace + search snippets; appfire.com returned 429].
  [BigPicture Marketplace](https://marketplace.atlassian.com/apps/1212259/bigpicture-ppm-strategic-portfolio-management-for-jira)
- **Forecast**: none probabilistic found [I]. **Pricing pattern**: strategy features in the *Enterprise edition* —
  a premium upsell like the one Lighthouse is considering [I].
- **Steal**: heatmap as the one-glance exec device. **Avoid**: Gantt/PPM complexity.

### 18. Productboard

- Initiatives and objective roadmaps are Enterprise-only; Pro has a limited number of objectives; roadmaps sync
  with Jira status/dates [I: search snippets of support.productboard.com].
  [Productboard + Jira](https://support.productboard.com/hc/en-us/articles/11535151728275-Getting-started-with-Productboard-s-Jira-Integration)
- A snippet claims Productboard reorganised around an AI product in April 2026 with >30% layoffs [I — unverified].
- **Pattern**: initiative/objective layer = top-tier gate. Discovery-side, no delivery forecasting.

### 19. Screenful

- Aggregates sprint data across boards; pricing by imported boards; scheduled email/Slack reports and PDFs [V].
  No initiative/epic forecasting documented [V: absence]. [Screenful Jira guide](https://screenful.com/guide/jira)
- **Steal**: scheduled report delivered to Slack/email — the exec never logs in.

### 20. Structure PPM (Tempo) and Jira Product Discovery (Atlassian) — brief

- **Structure PPM** [V]: user-defined hierarchies, "powerful data aggregation and reporting with custom
  formulas", "granular view of progress across all epics". [Tempo Structure PPM](https://www.tempo.io/products/project-portfolio-management-software-ppm)
  Rolls up estimates/story points/logged time to initiative [I: tempo.io blog snippets]. No forecast [V: absence].
  Pattern: spreadsheet-over-Jira; powerful, but the exec needs someone to build the formula view.
- **Jira Product Discovery Premium** [V]: "aggregating roadmaps across different projects into a single view";
  idea hierarchy "between opportunities, solutions, and initiatives". [JPD Premium](https://www.atlassian.com/software/jira/product-discovery/premium)
  Delivery progress bar per idea from linked epics [I: Atlassian guides snippet]. Standard $10, Premium $25/user/mo [I: third-party snippet].
  Pattern again: **cross-team + hierarchy = Premium**.

### 21. Planview Viz (ex-Tasktop) — flow metrics at portfolio level

- [V]: Flow Time, Flow Efficiency, Flow Velocity, Flow Load, Flow Distribution (features/defects/risk/debt);
  "Portfolio Insights consolidates performance across product lines"; "AI-powered capacity forecasting" and
  "forward-looking simulations trained on your historical data". [Planview Viz](https://www.planview.com/products-solutions/products/viz/)
- Requires Planview Hub model-based integration [I]. Measures value *streams*, not strategic *initiatives*.
- **Steal**: Flow Distribution as an allocation view derived from item counts, not hours. **Avoid**: integration-project weight.

---

## Comparison Matrix (strategy level only)

Legend: P = probabilistic, D = deterministic/planned dates, M = manual status, C = computed signal, — = none found.

| Tool | Forecast @ initiative | Health | Allocation | Dependencies | OKRs | Trackers | Strategy layer gated? |
|---|---|---|---|---|---|---|---|
| Nooga Portfolio | D (timeline) | — | — | Scale: yes | yes | ADO only | separate product, custom price |
| Businessmap | P (MC, card-level) | C (blockers [I]) | — | yes | yes | own boards | no ("No Tiers") |
| Jira Plans | D (velocity) | — | capacity | yes | — | Jira | **Premium/Enterprise** |
| Jira Align / Focus | D (points, PI) | M + AI summary | funds/spend | yes | yes | Jira | enterprise SKU |
| ADO Delivery Plans | — | — | — | yes | — | ADO | free (same-project rollup only) |
| 55° Portfolio Forecaster | P (epics/versions) | — | — | — | — | Jira | separate app |
| Planview AgilePlace/Viz | — / AI "simulations" | C (board health) | Flow Distribution | yes | yes | Jira, ADO | multi-SKU |
| Targetprocess | D (velocity/cost) | alerts | funding | yes | yes | Jira, ADO [I] | core product |
| Nave | P (MC, any scope) | — | — | — | — | Jira | no (per board, all-in) |
| Swarmia | **P (MC, FTE-paced)** | C (at-risk) | investment balance | — | — | Jira, Linear, git | paid plans, per dev |
| Jellyfish | scenario (P? [I]) | C | **core** | — | — | Jira, ADO, Linear, git | sales-led |
| Linear | — | **M** | — | yes | — | Linear | views = Enterprise |
| Allstacks | ML (opaque) | C | — | — | — | Jira, git | sales-led |
| Broken Build MC | P (pooled) | C (RAG vs target) | — | — | — | Jira | seat tiers |
| Kiplot | what-if | C (AI drift) | budget | yes | yes | Jira, ADO | sales-led |
| BigPicture | D (Gantt) | risk heatmap | budget | yes | yes | Jira | Enterprise edition [I] |
| Productboard / JPD | — | — | — | — | yes | Jira | Enterprise / Premium |

---

## Synthesis

### (a) Recurring patterns

1. **"Hierarchy above Epic + cross-team view" is the universal premium gate.** Jira Plans (Premium/Enterprise),
   JPD Premium, Linear Enterprise initiative views, Productboard Enterprise initiatives, BigPicture Enterprise,
   Nooga Portfolio as a separate custom-priced product. Buyers are trained to pay for exactly this layer. (High: 5+ vendors.)
2. **Health is mostly typed, not computed.** Linear (explicitly manual), Atlassian Focus (qualitative updates +
   AI summaries), Jira Align/Nooga (status fields). Computed health exists only in the engineering-intelligence
   camp (Swarmia, Allstacks, Kiplot AI) and is based on activity/FTE or opaque ML. (Medium-High.)
3. **Forecasts at strategy level are deterministic** (planned dates, velocity, PI points) in every SPM/PPM tool.
   Probabilistic forecasting lives in flow-analytics apps that stop at epic/version (55°, Nave, Broken Build) or
   in Swarmia's new FTE-paced MC (June 2026). (High.)
4. **Allocation / investment is the exec's second question** after "when": Jira Align funds, Focus funds,
   Swarmia investment balance, Jellyfish allocations, Planview Flow Distribution, Nooga "shadow work". Most need
   cost models or hours; few derive it from item counts. (High.)
5. **Snappiness devices**: timeline coloured by health (Linear), heatmaps (BigPicture risk), drill-down maps
   (Focus), forecast-vs-target columns (Allstacks), push delivery via Slack/email/PDF/Confluence (Screenful,
   Focus, 55°, Broken Build). (Medium.)
6. **Weight scales with price**: SAFe/PPM tools (Align, Targetprocess, BigPicture, Kiplot, Nooga Scale) are
   ceremony-heavy and sales-led; lightweight tools (Nave, 55°, Broken Build) lack the strategy layer. (Interpretation.)

### (b) Gaps nobody fills well

- **G1 — Correct probabilistic rollup to initiative across teams.** No tool found that runs *per-team* MC for each
  feature and rolls the result into a joint "all features of this initiative by date X" likelihood. Broken Build
  pools throughput (ignores which team does what); 55° stops at epic/version; Swarmia paces by FTE, not item flow;
  Businessmap/Nave forecast per card/scope. (Medium: absence of evidence on 17 vendors, not proof.)
- **G2 — Flow-metric health at initiative level.** Nobody computes initiative health from *flow* signals: feature
  WIP under the initiative, feature age vs SLE, throughput share, likelihood vs target, scope growth. Health is
  typed (Linear/Focus) or people-activity-based (Swarmia/Allstacks).
- **G3 — Flight Level 3 WIP.** Flight Levels says limit strategic initiatives in progress; Businessmap shows the
  board but no tool found *measures* initiative WIP, initiative cycle time or "initiatives started vs finished". 
- **G4 — Cross-tracker, cross-project.** ADO Delivery Plans cannot roll up across projects [V]; Nooga is ADO-only;
  55° Portfolio Forecaster and Broken Build are Jira-only; Linear initiatives are Linear-only.
- **G5 — Explainable forecasts for execs.** The exec-facing tools either use deterministic dates or opaque ML
  (Allstacks). Explainable percentiles from the team's own history are rare above epic level.
- **G6 — Priced for the reader.** Per-developer pricing (Swarmia $45–55/dev/mo) or enterprise sales makes the exec
  view expensive for organisations that only want the top-level answer.

### (c) Differentiation angles for Lighthouse (interpretation, grounded in the gaps above)

1. **"Initiative likelihood" done right.** Roll per-team feature forecasts (already computed) into a joint
   likelihood per parent and a date percentile — explicitly *not* pooled throughput. Headline: "83% likely by
   30 Nov", plus the gap to the target date (Allstacks' column, with Lighthouse's maths). Closes G1, G5.
2. **Computed flow health, zero typing.** Initiative health = f(likelihood vs target, features in progress vs
   limit, oldest feature age vs SLE, scope growth since start). Show *why* it's amber in one line. Directly beats
   Linear/Focus manual RAG. Closes G2.
3. **Flight Level 3 metrics nobody measures.** Initiatives in progress (strategic WIP), initiative cycle time,
   initiatives started vs finished per month, and "how many teams each initiative spreads across" (fragmentation).
   Owns the Flight Levels narrative that Businessmap only visualises. Closes G3.
4. **One-glance heatmap.** Initiatives × months, cells coloured by likelihood of completion by that month;
   alternatively initiatives × teams coloured by remaining features (where is strategy stuck?). Borrowed device
   (BigPicture, Linear), new content (probabilities).
5. **Strategy share of flow ("shadow work" from counts).** Percentage of team throughput with no initiative parent,
   and distribution across initiatives — derived from item counts Lighthouse already has, no hours or cost model.
   Answers Nooga's "shadow work" and Swarmia's investment balance without git or timesheets.
6. **Push it to the exec.** Weekly "what changed" digest (likelihood deltas, new at-risk initiatives, target-date
   moves — Linear's auto-appended changes, but computed) as email/PDF/Slack; the exec never needs a login. Pairs
   with the server-side chart → PDF spike.
7. **Tracker-agnostic parent resolution.** Treat Jira Initiative/Epic, ADO Epic, Linear Initiative, ServiceNow
   parent as the same FL3 concept, across Portfolios and projects — the exact rollup ADO Delivery Plans refuses.
   Closes G4.
8. **Pricing story**: flat licence, not per developer — "the strategy view for the price of one licence" against
   Swarmia/Jellyfish per-seat and Align/Focus enterprise sales. (Note DISCOVER's warning: with a 1-Portfolio free cap
   the feature cannot sell itself via try-then-buy — use demo data and a fake-door preview.)

**Ideas to avoid**: SAFe ceremony (PI overlays, ROAM), cost/funds models, manual status fields, opaque ML,
Gantt editing. Lighthouse should stay read-only and computed.

---

## Knowledge Gaps

- **Businessmap initiative MC rollup**: whether "What If" forecasts an initiative from child cards on several team
  boards. KB article 403. *Next*: trial account or ask a Flight Levels trainer.
- **Nooga "shadow work detection" / "OKR pace tracking"**: only in search-index descriptions, not on the fetched
  pages. Could be roadmap copy. *Next*: Nooga Portfolio trial (28 days via VS Marketplace).
- **Pricing** for Jira Align/Focus, Targetprocess, Jellyfish, Allstacks, Kiplot, Portfolio Forecaster, BigPicture:
  sales-led or Marketplace calculators not fetched.
- **Swarmia multi-team rollup**: the 2026 changelog does not say how several teams' paces combine for one initiative.
- **Jellyfish probabilistic claim**: only in third-party text; the vendor page does not say "probabilistic".
- **Nave executive dashboard contents**: named, not described.
- No hands-on validation of any tool; absence-based claims (G1–G3) are "not found", not "does not exist".

## Conflicting Information

- **Swarmia forecast basis.** 2024 changelog snippet: forecast from "total number of epics, average epic cycle time,
  and average number of epics in progress" (a flow model). 2026 changelog [V]: Monte Carlo on "recent monthly pace"
  with FTE output, which "replaces the previous ... forecast tool". *Assessment*: the 2026 page is newer and primary.
  Swarmia moved from flow-based to effort-based. That leaves the flow-based initiative forecast open.
- **Businessmap pricing.** The page says both "No Tiers" and lists Standard vs Enterprise [V]. *Assessment*: two plans,
  and the strategy features sit in both, so strategy is not an upsell there.

## Source Notes

Vendor pages (primary, High for "what the vendor claims", Medium for "what the product does") make up most citations.
Microsoft Learn and Atlassian Support are authoritative for the Delivery Plans and Jira Plans facts. Search-engine
summaries are used only where marked [I]. Commercial bias applies throughout. No source was independent of its
vendor except the Microsoft/Atlassian documentation for their own products.

