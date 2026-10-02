# Competitive Research — epic-5878-baseline (DIVERGE, Phase 2)

**Depth**: lightweight (per dispatch). Done by Flux directly with WebSearch/WebFetch (no separate
`nw-researcher` run). **Job researched**: compare today's flow against a fixed "before", judged by rules that
resist noise (`job-analysis.md` §3).

Grading: **[V]** verified on the vendor's own page or docs during this wave · **[S]** secondary source or search
snippet, not opened on the vendor page · **[I]** internal Lighthouse evidence, not publicly verifiable ·
**[U]** unverified practice description.

## 1. Products and practices

### 1.1 ValueFlow baseline (Thrivve Partners) — prior art

- **What it does**: a baseline of flow metrics frozen at engagement start, shown against today. Field set
  (from the 2026-08-31 demo): SLE 85th percentile over 90 and 30 days, Cycle Time 50th/70th, Total Work Item
  Age + average, Throughput weekly median + breach count + stability, WIP range/average/streaks, days since
  intervention. **[I]** — Paul Brown demo, #5878. Not publicly verifiable: `valueflow.co/how-it-works` returned
  404 on 2026-10-02, and Thrivve's public writing does not describe the feature.
- **Does well**: anchors to the engagement date; the consultant owns the story; clients "react most" to it
  (Thrivve's own account) **[I]**.
- **Fails the job**: a wide field set with no stated rule of what counts as "better" per field, so the
  reader has to judge each row; stability is one field among many, not the judge of the others **[I, inferred
  from the field list]**.
- **Assumption about users**: the consultant presents and narrates; the artefact supports a conversation.
- Thrivve publicly advocates process behaviour charts for separating signal from noise ("Signal vs Noise: How
  Process Behaviour Charts can enable more effective Product Operations") **[S]** — the article itself
  returned 403, so only the title and the search snippet were seen.

### 1.2 Nave — Executive Report

- **What it does**: "For each team, the report displays their flow metrics and compares them to the previous
  month, three months, and six months. When there's a significant shift in the numbers, you'll see a red
  indicator next to the metric that exceeds your predefined threshold. If the team performance is improving,
  you will see green indicators." **[S]** (search snippet attributed to getnave.com). The home page states
  "Show whether delivery is improving across 1, 3 and 6-month periods" **[V]**, getnave.com, 2026-10-02.
- **Does well**: a per-metric verdict (green/red) on a user-defined threshold — the closest public match to
  D16's "item + rule". Executive audience by design.
- **Fails the job**: the "before" is **rolling** (previous 1/3/6 months), so it moves; the threshold is a
  percentage the user picks, so a noisy metric crosses it by chance (OS3); no engagement anchor.
- **Assumption**: leadership wants colour per Team, not method; a fixed % threshold is good enough.

### 1.3 ActionableAgile Analytics (55 Degrees)

- **What it does**: flow charts including a Process Behavior Chart: "process behavior charts characterize a
  process as predictable or unpredictable by identifying points that represent exceptional variation
  (signals) and the amount of routine variation (noise)"; Individuals + Moving Range on Cycle Time **[S]**
  (55degrees support space; the page body did not render for WebFetch). Date-range selection on charts
  ("drag to select a time period … average cycle time shown as a dotted line") **[S]** (SafetyCulture
  engineering blog on Medium). No frozen before/after and no per-metric verdict were found **[S]**; matches
  DISCOVER V4.
- **Does well**: rigorous signal/noise on Cycle Time; saved and shared chart configurations.
- **Fails the job**: the user has to read the chart and judge. No "before" pinned to a date, no statement of
  what counts as better.
- **Assumption**: the reader is a flow practitioner who can read a PBC.

### 1.4 Non-obvious alternatives (different category, same job)

| Alternative | Category | How it serves the job | Where it fails | Grade |
|---|---|---|---|---|
| **Consultant's hand-built spreadsheet + slide** ("before" column copied from an export at kick-off, "after" column at review, arrows or RAG by hand) | Manual practice | Truly frozen (a copied number cannot drift); the consultant decides what "better" means per row | Hours per review; "better" is decided after looking (OS5); no noise test (OS3); the before is lost if the file is. | **[U]** — DISCOVER R1 says this has to be confirmed in interviews (protocol Q2). |
| **BaselineX for Jira** (Optimizory) | Project-plan baselining | "A baseline represents a snapshot of selected Jira issues at a specific point in time"; current issue state is compared to it | Freezes **issue fields** (estimates, dates), not flow metrics; no verdict. It shows that "baseline" means *frozen snapshot* to Jira users. | **[V]** optimizory.atlassian.net, 2026-10-02 |
| **Google Analytics date comparison** | Web analytics | "Previous period", "Previous year" and custom comparison for any report | Comparison is between two *live* ranges; no rule of good; no noise test. Shows the market's default mental model is "pick two ranges, see the delta". | **[V]** support.google.com/analytics/answer/13412290 |
| **Jellyfish AI Impact** | Engineering intelligence | Before/after of adopting a tool (Cycle Time, Throughput), with users compared against non-users | Cohort comparison, not a frozen window; proprietary benchmark. | **[S]** gitkraken.com Jellyfish-alternatives blog |
| **Wheeler XmR practice: "compute limits on the baseline, extend them, look for signals in new data"** | Statistical method | Exactly the noise test OS3 asks for; the limits are frozen by construction | Requires the reader to accept "no signal" as an answer; percentile summaries are not XmR series. | **[U]** standard SPC practice (Wheeler, *Understanding Variation*); not re-sourced in this wave |

## 2. Local evidence (codebase, 2026-10-02)

| Fact | Where | Consequence for options |
|---|---|---|
| XmR process behaviour charts exist for Throughput, WIP, Total Work Item Age, Cycle Time, arrivals (Team and Portfolio) and Feature size (Portfolio), each taking `(startDate, endDate, asOf)` | `TeamMetricsService.cs:178-234`, `PortfolioMetricsService.cs:35-210` | A PBC rule can be computed for any past baseline window, retroactively (D12). |
| Special-cause classes: `LargeChange`, `ModerateChange`, `ModerateShift`, `SmallShift` | `Models/Metrics/SpecialCauseType.cs` | Rules can be phrased on existing classifications; "trending" can mean "shift" without a new statistic. |
| Charts expose `Average`, `UpperNaturalProcessLimit`, … and a `NotReady` state | `Models/Metrics/ProcessBehaviourChart.cs:38-54` | The values to freeze per item are small (average + limits); "not enough data" already exists as a state. |
| `XmRCalculator.Calculate(baselineValues, displayValues)` derives limits from one series and classifies another; the lower lines clamp at 0 and disable the rules that depend on them | `Services/Implementation/XmRCalculator.cs:15-53` | Judging Now against frozen Then limits needs no new statistics. |
| Count-based Community limit precedent: Community may hold fewer than 2 custom additional fields | `API/Helpers/AdditionalFieldsHelper.cs:11` | A per-owner report cap has a pattern to copy. |
| No user-facing "Report" wording in the frontend | search of `src/**/*.tsx`, 0 hits | "Reports" tab and "Create Report" collide with nothing. |
| "Set Baseline for Process Behaviour Chart" in Settings | `FlowMetricsConfigurationComponent.tsx:666` (DISCOVER C3) | "Baseline" in the new feature's UI copy would collide. |

## 3. Insights for brainstorming

- **I1.** Nobody researched both **freezes the before** and **judges per metric**. Nave judges per metric with
  a moving before; ValueFlow freezes without a stated rule; ActionableAgile judges signal without a before.
- **I2.** The market default for "is it better" is a **percentage threshold** (Nave, GA-style deltas). It is the
  cheapest mental model and the weakest against noise (OS3).
- **I3.** The rigorous default is **PBC limits frozen on the baseline** (the Wheeler practice). Lighthouse has
  every ingredient except "classify new points against limits from another window".
- **I4.** Percentile summaries (Cycle Time 85th) have no XmR series of their own. A rule on them is either a
  threshold or delegates to the Cycle Time item series.
- **I5.** "Baseline" means *frozen snapshot of issue fields* to some Jira users (BaselineX) and *PBC limits
  window* inside Lighthouse — two reasons to keep the word out of the template name.

## Gate G2

- [x] 3+ real products named: ValueFlow (prior art, internal evidence), Nave, ActionableAgile, plus BaselineX,
  Google Analytics, Jellyfish.
- [x] Non-obvious alternatives: consultant spreadsheet/slide (practice), BaselineX (project baselining), GA
  (web analytics), Wheeler XmR practice.
- [x] No generic market claims; every claim graded; unverified items marked [U]/[I].

**G2: PASS** (lightweight depth; ValueFlow evidence is internal only).

## Sources

- [Nave — Flow Analytics Platform](https://getnave.com/)
- [Nave — Mapping flow metrics (Executive Report snippet source)](https://getnave.com/blog/mapping-flow-metrics-4-step-process-with-qualitative-insights/)
- [ActionableAgile — Process Behavior Chart](https://support.55degrees.se/space/AAS/699236831/Process+Behavior+Chart)
- [ActionableAgile — product page](https://www.55degrees.se/products/actionableagileanalytics)
- [3 easy team delivery metrics with ActionableAgile](https://medium.com/safetycultureengineering/3-easy-team-delivery-metrics-with-actionableagile-ce02ef9c1209)
- [Thrivve Partners — Signal vs Noise (PBC)](https://medium.com/thrivve-partners/signal-vs-noise-how-process-behaviour-charts-can-enable-more-effective-product-operations-e360cf474941)
- [BaselineX for Jira Cloud — Introduction](https://optimizory.atlassian.net/wiki/spaces/BLJC/pages/979337225/Introduction)
- [Google Analytics — Change and compare date ranges](https://support.google.com/analytics/answer/13412290?hl=en&co=GENIE.Platform%3DDesktop)
- [7 Jellyfish alternatives (AI Impact description)](https://gitkraken.com/blog/7-jellyfish-alternatives-for-engineering-intelligence-in-2026)
