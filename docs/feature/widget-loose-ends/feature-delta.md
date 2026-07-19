# Feature Delta — widget-loose-ends

**ADO:** Story #5508 "Cleanup Widget lose Ends" (User Story, New, tagged `Release Notes`) · **Feature type:** user-facing · **Premium:** No
**Wave status:** DISCUSS complete 2026-07-18 · DESIGN complete 2026-07-18 · DISTILL next (DEVOPS takes outcome-kpis only)

---

## Wave: DISCUSS / [REF] Persona ID

**flow-coach** (Flow Coach — runs standups, flow reviews and ops reviews for a team or release train; reads the metrics dashboard as a flow-health diagnostic). Secondary: **delivery-lead-rte** (reads the same overview to judge whether a period got better or worse).

Both personas already exist in `docs/product/personas/`. No new persona.

## Wave: DISCUSS / [REF] JTBD one-liner

When I scan the Flow Overview dashboard, I want every widget to answer the same three questions — *what is the status, how does it compare to last period, and which items is it about* — so I can read the whole board at one glance instead of learning which widgets are trustworthy and which quietly leave a question unanswered.

Primary job: `job-flow-coach-read-every-widget-the-same-way` (importance 4 / satisfaction 2 / gap 2).
Supporting jobs: `job-flow-coach-drill-into-throughput-and-arrivals`, `job-flow-coach-see-age-as-of-selected-date`, and the existing `job-delivery-lead-tell-blocked-trend-vs-last-period`.

## Wave: DISCUSS / [REF] Two of the six gaps are correctness defects, not chrome

Four of the six reported gaps are missing chrome — a widget that never registered its RAG footer, `viewData` payload, or trend policy in `BaseMetricsView`. The chrome itself (`WidgetShell`) already supports all three; the widgets simply do not feed it. Those are wiring.

Two are not. One is the Work-Item-Age today-anchoring defect below. The other was found on 2026-07-19 by the DISTILL review gate and is written up as UPSTREAM-4: the Blocked trend's baseline lookup sits one day outside the window its own history fetch requests, so the trend has never rendered a comparison on any instance. That one re-opens D2 and is not yet resolved.

Taking the age defect first. `WorkItemBase.WorkItemAge` (`Models/WorkItemBase.cs:71`) computes `GetDateDifference(referencedDate, DateTime.UtcNow)` **and** returns `0` unless `StateCategory == Doing` *right now*. Every Work-Item-Age surface therefore reports age **as of today**, no matter which date range is selected:

- `GetWorkItemAgePercentilesForTeam(team, endDate)` (`TeamMetricsService.cs:319`) correctly takes the WIP snapshot as of `endDate` — then ages every item to today, and drops any item that has closed since (`age > 0` filter removes it).
- The Work Item Aging chart plots `item.workItemAge` (`WorkItemAgingChart.tsx:212`) — same today-anchored value.

Select last month and the ages are wrong and the population is incomplete.

**Correction 2026-07-19 (review gate).** An earlier draft claimed this defect was "the root cause behind 'Work Item Percentiles has no trend with the previous period'". That overclaims. The widget has no trend because `trendPolicies.workItemAgePercentiles` is set to `"none"` in `categoryMetadata.ts:113` — the same registration gap as the other chrome stories. What the age defect actually does is make a trend **impossible to register honestly**: with both periods anchored to today, any previous-period comparison would read flat regardless of what happened. So US-04 is a genuine correctness fix, and US-05's trend is absent by registration and would have been *wrong* without US-04. The dependency is real; the causal claim was not, and it should not be used to relabel the feature.

The primitive to fix it already exists and is already trusted: `BaseMetricsService.GenerateTotalWorkItemAgeByDay` (`BaseMetricsService.cs:808`) computes, for any given day, `items.Where(i => WasItemProgressOnDay(currentDate, i))` and `age = (currentDate - (StartedDate ?? CreatedDate)) + 1`. No new persistence, no new snapshot table.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | Feature type | **User-facing.** Six visible dashboard gaps. No infrastructure-only escape valve. |
| **D2** | Blocked-Items trend baseline | **RE-DECIDED (user, 2026-07-19) after UPSTREAM-4 — two changes, in order.** The diagnostic slice 02 promised was run and disproved the original premise: the missing baseline is not a young-instance condition. `useMetricsData` fetched the history with the dashboard's own `[startDate, endDate]`, the controller filters `RecordedAt >= startDate`, and the trend looks for its baseline at `startDate − 1 day` — one day outside the fetched window, always — so `noBaselineTrend()` fired on **every instance, every range** and the widget has never rendered a comparison. **(1) Widen the fetch** to start at `startDate − 1 day`, so a real baseline is reachable. One extra day per request; no new endpoint, no contract change. **(2) Keep the original locked intent** — treat a still-absent baseline as `blockedCount = 0` — but now it applies only to the residual genuinely-young-instance case, where it means what `noBaselineTrend`'s docstring always claimed. A fresh instance still reads "+N since we started recording"; an established one now reads the truth. Shipping (2) without (1) would have made every instance read "+N" forever and hidden the real comparison — a visibly broken widget traded for an invisibly wrong one. The `noBaseline` field on `TrendPayload` stays for other widgets. |
| **D3** | Work Item Age is a function of the selected date | **LOCKED (user, 2026-07-18): every Work-Item-Age surface reports age as of the LAST DAY of the selected range (`endDate`), not as of now.** Population = items in progress *on that day* (`WasItemProgressOnDay`), age = `(endDate − started) + 1`. Applies to the Work Item Age Percentiles card, the Total Work Item Age overview widget, and the dot heights on the Work Item Aging chart. When `endDate` is today the rendered numbers are unchanged — this is a correctness fix for historical ranges, not a redefinition of the live view. |
| **D4** | Where D3 is implemented | **Backend, and NOT by changing the `WorkItemAge` property.** `WorkItemAge` is also consumed by write-back (`WriteBackValueSource.cs`); repointing it at an arbitrary date would change what Lighthouse writes into Jira/ADO. DESIGN picks the mechanism (an `AgeOnDay(date)` helper on `WorkItemBase` reusing the `GenerateTotalWorkItemAgeByDay` arithmetic is the obvious candidate) under the hard constraint: **write-back semantics unchanged**. |
| **D5** | Work Item Age Percentiles trend | **Previous-period, derived from D3** — the same as-of-date computation evaluated at `startDate − 1 day`, compared against the `endDate` value. This is why D3 must land first; it is the enabling correctness fix, not a separate feature. Trend policy for `workItemAgePercentiles` moves `"none"` → `"previous-period"`. |
| **D6** | Work Item Age Percentiles RAG | **CLARIFIED 2026-07-18.** New rule in `ragRules.ts` reusing the existing `calculateSLEStats` shape **verbatim**: compute the share of in-progress items whose age (as-of-`endDate`, per D3) falls within `sle.value`, compare that share against the `sle.percentile` target, and apply the shipped bands from `computeCycleTimePercentilesRag` — green when the target is met, red when more than 20pp short, amber otherwise, and red with the "define an SLE" tip when no SLE is configured. Only the population changes (in-progress ages instead of completed cycle times). **No invented amber band, no new threshold, no new setting.** Known bias, accepted: WIP-within-SLE flatters a young WIP profile, because an item at 2 days counts as "within 14" though it may still breach. Directionally sound — an in-progress item already past the SLE *will* breach — and validated in slice 04 rather than blocked on. **Validation made falsifiable 2026-07-19 (review gate):** "validated in slice 04" was unowned and had no abort criterion, which under delivery pressure means skipped. It is now a gate at the *start* of slice 04, not a check inside it. Before any slice-04 code: name the reviewing coach and pick **two** historical periods on a real instance that the coach independently judges to have deteriorated. Run the rule over both. **Abort criterion: if the chip reads GREEN on either period, stop and re-open D6** — a status that reassures during a decline is worse than the absent chip it replaces. Record coach, periods and results in the slice brief before proceeding. |
| **D7** | Flow Efficiency RAG | **LOCKED (user, 2026-07-18): lift the data into `BaseMetricsView`.** `FlowEfficiencyOverviewWidget` currently self-fetches via `metricsService.getFlowEfficiencyInfoFor*` and colours its own number; it is the only overview widget off the shared data path, which is exactly why it has no RAG chip. Move the fetch into the `BaseMetricsView` data layer so `buildWidgetFooters` can emit `flowEfficiency: computeFlowEfficiencyRag(...)` like every sibling. The existing `computeFlowEfficiencyRag` rule is reused as-is — no new thresholds. |
| **D8** | Throughput / Arrivals "View Data" | Add `totalThroughput` and `totalArrivals` keys to `buildViewData`, reusing the item sources the neighbouring chart widgets already extract (`throughputItems`, and `extractWorkItems(arrivalsData.workItemsPerUnitOfTime)` — the `arrivals` widget's existing payload at `BaseMetricsView.tsx:643`). No new endpoint, no new query. |
| **D9** | Aging-chart percentile line label | Drop the term prefix. `WorkItemAgingChart.tsx:653-654` renders `` `${workItemAgeTerm} ${p.percentile}%` `` when the source is `workItemAge`; render `` `${p.percentile}%` `` so both percentile sources label identically. The Cycle Time / Work Item Age toggle already states which population is active — the per-line prefix is redundant. |
| **D10** | Premium / RBAC | **Not premium, no new authorization surface.** Every read rides existing metrics paths already guarded by `[RbacGuard(TeamRead/PortfolioRead)]`. No `useRbac` gating, no license check, no `IRbacAdministrationService` change. |
| **D11** | Walking skeleton | **No** (brownfield, WS strategy C). Slice 01 is the thinnest existing-path slice, not a greenfield skeleton. |
| **D12** | Slice shape | **LOCKED (user, 2026-07-18): three groups by shape** — (A) pure chrome wiring, (B) blocked-trend fix, (C) the RAG/as-of-date data work. Group C exceeds the ≤1-day carpaccio gate as one unit, so it ships as **three sub-slices** (03, 04, 05). Five shippable slices, three conceptual groups. |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Drill into Total Throughput and Total Arrivals
As a flow coach, I want the "View Data" table icon on the Total Throughput and Total Arrivals widgets, so I can see which items make up the number without leaving the dashboard.
`job_id: job-flow-coach-drill-into-throughput-and-arrivals`

#### Elevator Pitch
Before: I see "Total Throughput 23" and have no way to ask which 23 — every neighbouring widget offers that, these two do not.
After: open **Team → Metrics → Flow Overview** → click the table icon on **Total Throughput** → the `WorkItemsDialog` lists the completed items for the selected range, with cycle time highlighted.
Decision enabled: I decide whether a good throughput number came from real work or from a batch of trivial items, before I quote it in a review.

**Acceptance criteria**
- AC1: Given a team with completed items in the selected range, when I click the View Data icon on Total Throughput, then `WorkItemsDialog` opens listing exactly those items with the cycle-time highlight column — the same payload shape the `throughput` chart widget already supplies.
- AC2: Given a team with items that arrived in the selected range, when I click the View Data icon on Total Arrivals, then the dialog lists exactly those arrival items — the same set the `arrivals` chart widget already exposes.
- AC3: Given zero items in scope, the icon still renders and the dialog opens with an empty table — no crash, consistent with the existing empty behaviour of the sibling widgets.

### US-02 — Read the aging chart's percentile lines without a redundant prefix
As a flow coach, I want the Work Item Age percentile reference lines labelled with just the percentage, so the chart reads the same whichever percentile source is selected.
`job_id: job-flow-coach-read-every-widget-the-same-way`

#### Elevator Pitch
Before: switching the aging chart to Work Item Age relabels every reference line "Work Item Age 85%", repeating on four lines what the toggle above already says.
After: open **Team → Metrics → Flow Metrics → Work Item Aging** → switch the percentile source to **Work Item Age** → the lines read `95%`, `85%`, `70%`, `50%`.
Readability tidy-up traceable to the user report. (Amended 2026-07-19: this previously claimed a decision — "I read the band a dot has crossed at a glance" — which asserts a comprehension gain from removing eleven characters with no evidence behind it. The change is defensible on taste and consistency; it does not enable a decision and carries no KPI of its own.)

**Acceptance criteria**
- AC1: Given the aging chart with percentile source `workItemAge`, each reference line's label is exactly `{percentile}%` with no term prefix.
- AC2: Given the percentile source `cycleTime`, the label is unchanged from today's behaviour.

### US-03 — Blocked trend that reads from day one
As a delivery lead, I want the Blocked Items trend to show a real comparison even before a full previous period of snapshots exists, so the widget tells me something on the instance I just set up.
`job_id: job-delivery-lead-tell-blocked-trend-vs-last-period`

#### Elevator Pitch
Before: the Blocked widget's trend shows a neutral "—" with a hint about waiting for a baseline, so it reads as broken rather than as informative. (Corrected 2026-07-19: this was written as a *young-instance* symptom. Per UPSTREAM-4 it happens on every instance and every range — the hint about waiting for a baseline is simply never true.)
After: open **Team → Metrics → Flow Overview** → the **Blocked Items** widget shows an arrow and delta against the blocked count at `startDate − 1 day`, counting an absent snapshot as 0.
Decision enabled: I decide whether blocking is growing or shrinking in this period, on day one, without waiting a period for the record to fill.

**Acceptance criteria** — settled 2026-07-19 (UPSTREAM-4 resolved; D2 re-decided). AC0 must land before
AC1-AC4 are meaningful: until the fetch window includes the boundary day, AC1 describes behaviour that has
never been reachable in the shipped app.

- AC0 (**new, from UPSTREAM-4**): The blocked-count history available to the trend includes the day
  `startDate − 1`, so a baseline recorded on that day is found. Asserted at the `useMetricsData` seam —
  the selector alone cannot catch this, which is why the defect survived a green selector suite.
- AC1: Given a `BlockedCountSnapshot` exists at or before `startDate − 1 day`, the trend compares the current count against it — unchanged from today (`blockedTrend.ts` boundary logic preserved).
- AC2: Given **no** snapshot exists at or before `startDate − 1 day`, the baseline is `0`; a current count of N renders direction `up` rather than the `noBaseline` placeholder.
- AC3: Given no snapshot exists and the current count is also 0, the trend renders `flat` — not an arrow implying change.
- AC2b (**added 2026-07-19, second-pass gate**): Given the history holds no snapshot at or before `endDate` — a range selected entirely before recording began — no direction is rendered. The zero-baseline assumption of AC2 is defensible for a day-one instance; with no measurement at *either* end there is nothing to compare and an arrow would be fabrication. This is the third `noBaselineTrend()` return site, which UPSTREAM-3 counted but no scenario covered.
- AC4: The percentage delta is omitted when the baseline is 0 (division by zero), matching the existing `formatDelta` guard; the arrow and the absolute current/previous values still render.
- AC5b (**added 2026-07-19, second-pass gate**): The synthetic zero baseline is identifiable as assumed — the payload carries a signal the measured case does not, so "+4" is never read as four items having become blocked when the truth is "no record before this". D2 and the pitch above both promise this in prose; without an AC, DELIVER could ship a bare arrow and stay green.

### US-04 — Work Item Age that respects the selected date range
As a flow coach, I want every Work-Item-Age readout to describe the last day of the range I selected, so looking at a past period tells me what was true then rather than what is true now.
`job_id: job-flow-coach-see-age-as-of-selected-date`

#### Elevator Pitch
Before: I select last month and the Work Item Age Percentiles card ages every item to today and silently omits everything that has closed since — the number is not about last month at all.
After: open **Team → Metrics**, set the range to a past month → the **Work Item Age Percentiles** card, the **Total Work Item Age** widget and the **Work Item Aging** chart all report ages as they stood on the last day of that range.
Decision enabled: I can review a past period honestly in a retro — "our WIP was ageing to 30 days back then" — instead of unknowingly reading today's numbers under last month's heading.

**Acceptance criteria**
- AC1: Given an item started on day D and closed on day D+5, when I select a range ending on day D+3, then the item is included in the Work Item Age population with age 4 — it is neither aged to today nor dropped for being closed now.
- AC2: Given the selected range ends today, every Work-Item-Age number is identical to today's behaviour (regression guard: this is a fix for historical ranges only).
- AC3: The Work Item Aging chart's dot heights use the same as-of-`endDate` age as the percentile card — the two surfaces never disagree.
- AC4: Work-tracking write-back values are unchanged by this story — `WriteBackValueSource` continues to emit today-anchored age (D4 constraint).

### US-05 — Status and previous-period trend on Work Item Age Percentiles
As a flow coach, I want the Work Item Age Percentiles widget to carry a RAG status and a previous-period trend, so it answers the same two questions every other overview widget answers.
`job_id: job-flow-coach-read-every-widget-the-same-way`

#### Elevator Pitch
Before: the Work Item Age Percentiles card is the only overview widget with neither a status colour nor a trend — I have to judge for myself whether "85th: 18 days" is good.
After: open **Team → Metrics → Flow Overview** → the card shows an **Act/Observe/Sustain** chip against the SLE plus a trend arrow versus the previous period.
Decision enabled: I decide whether ageing WIP is a problem *and* whether it is getting worse, without holding last period's numbers in my head.

**Acceptance criteria**
- AC1: Given an SLE of `{percentile: P, value: V}` and a share of in-progress items within V that is more than 20pp below P, the widget header renders RED ("Act") with a tip stating the achieved share against the target — same wording shape as the cycle-time rule.
- AC2: Given the share within V meets or exceeds P, the header renders GREEN ("Sustain"); given it is short by 20pp or less, AMBER ("Observe").
- AC3: Given **no SLE is configured**, the widget renders RED with the "define an SLE in settings" tip — matching `computeCycleTimePercentilesRag`'s existing handling, not a bespoke neutral state.
- AC3b (**split out 2026-07-19, review gate**): Given an SLE **is** configured but **zero items are in the population**, the widget renders no Act status and a body reading "no work in progress in this range". Previously AC3 collapsed the two cases into one RED + "define an SLE" answer, which tells a team that has already configured an SLE to go and configure one — a user-visible falsehood — and reads a team with zero WIP as needing to act. Consistency with the sibling cycle-time rule was being used to license it: there, an empty population means no completions and therefore no signal at all, which is not the same situation. This introduces no new band and no new threshold, so it stays inside the out-of-scope line.
- AC4: The trend compares the as-of-`endDate` percentile against the same percentile computed at `startDate − 1 day` (D5), rendered through the existing `WidgetShell` trend chrome — no new UI component.

### US-06 — Status on Flow Efficiency
As a flow coach, I want the Flow Efficiency widget to carry the standard RAG chip, so I read its status the same way I read every other widget's.
`job_id: job-flow-coach-read-every-widget-the-same-way`

#### Elevator Pitch
Before: Flow Efficiency colours its own big number but has no Act/Observe/Sustain chip, so it is the one widget whose status I read by a different convention.
After: open **Team → Metrics → Flow Overview** → the **Flow Efficiency** widget shows the same status chip and hover tip as its neighbours.
Decision enabled: I scan the row of widgets for red chips and act on what I find, without a blind spot.

**Acceptance criteria**
- AC1: Given a configured flow efficiency of X%, the widget header renders the RAG status returned by the existing `computeFlowEfficiencyRag(X, terms)` — the rule itself is unchanged.
- AC2: Given wait states are not configured, the widget keeps its current "Not configured" body and renders no misleading status colour.
- AC3: Given no data in scope, the widget keeps its current "No data in scope" body and renders no misleading status colour.
**Design constraint (not an AC — relabelled 2026-07-19):** flow efficiency data is fetched once through the `BaseMetricsView` data layer, not by the widget. This is an architecture rule, not an observable user outcome; it stays enforced by scenario 46 but no longer occupies an AC slot.

## Wave: DISCUSS / [REF] Acceptance criteria (cross-story invariants)

- **CI1** — No new endpoint requires a new RBAC guard; every read is already covered by `[RbacGuard(TeamRead/PortfolioRead)]` (D10).
- **CI2** — Every story is Team **and** Portfolio scope. A widget fixed for teams and left broken for portfolios is not done.
- **CI3** — RAG colour is never the only signal; the `WidgetShell` chip carries the Act/Observe/Sustain label and tip text, as today.
- **CI4** — When the selected range ends today, all rendered numbers are byte-identical to pre-change behaviour (the D3 regression guard).
- **CI5** — Write-back output is untouched by this feature (D4).

## Wave: DISCUSS / [REF] Out-of-scope

- Any new persisted snapshot table for Work Item Age. D3 is computed from existing transition/started/closed dates via the `WasItemProgressOnDay` primitive.
- New user-configurable thresholds, and new RAG bands of any kind. D6 reuses the shipped `calculateSLEStats` bands verbatim; a WIP-specific SLE distinct from the cycle-time SLE is explicitly not in this story (it would re-open D6 — see slice 04's learning hypothesis).
- Widgets outside Flow Overview and the Work Item Aging chart. The Predictability and Portfolio **widget categories** are not audited here — note this is a widget grouping in `categoryMetadata.ts`; portfolio *scope* is in scope for every story per CI2. (Disambiguated 2026-07-19; the `computeFlowEfficiencyRag`-thresholds bullet that sat here was deleted as a restatement of D7.)
- Retro-filling `BlockedCountSnapshot` history. D2 assumes 0, it does not backfill.
- ~~Lighthouse-Clients (CLI/MCP) changes~~ — **no longer out of scope, 2026-07-19.** No endpoint *contract* changes, but D16 changes the age *values* the client's percentile and total-age methods return for historical ranges. Slice 03 now owes an MCP tool-description reword plus a Changeset. See the cross-cutting table row.

## Wave: DISCUSS / [REF] Definition of Done (12-item)

1. All six reported gaps closed, Team and Portfolio scope (CI2).
2. `pnpm test` green; `pnpm build` zero errors/warnings; Biome clean.
3. `dotnet build` zero warnings; `dotnet test` green.
4. SonarCloud: no new issues of any severity.
5. Vitest coverage for each new `ragRules` rule and the changed `blockedTrend` baseline path, including the 0-baseline and both-zero cases.
6. Backend tests pin the as-of-date age arithmetic against the `GenerateTotalWorkItemAgeByDay` reference, plus a write-back-unchanged regression test (CI5).
7. E2E: one `@screenshot` spec per changed theme, driven by demo data, POM-mediated; run locally before commit.
8. Docs + screenshots refreshed at feature finalization (per-feature, not batched).
9. Mutation testing ≥80% kill rate on the changed FE and BE units.
10. Chrome parity asserted as a test, not a manual audit: widget keys registered in `buildWidgetFooters` / `buildViewData` cover every key from `getWidgetsForCategory("flow-overview")` (was an "outcome KPI"; moved here 2026-07-19).
11. Zero divergence between the WIA percentile card and `GenerateTotalWorkItemAgeByDay` at the same date (CI6) (was an "outcome KPI").
12. Zero changed values on any Work-Item-Age surface when `endDate` = today (CI4), and zero change in emitted write-back age values (CI5) (were "outcome KPIs").

## Wave: DISCUSS / [REF] WS strategy

**C — brownfield extension.** No walking skeleton. Every slice rides an existing end-to-end path (metrics service → controller → `useMetricsData` → `BaseMetricsView` → `WidgetShell`). Slice 01 is the thinnest such slice and ships the same day.

## Wave: DISCUSS / [REF] Driving ports

- HTTP: `GET /api/{teams|portfolios}/{id}/metrics/workItemAgePercentiles` (existing; population/age semantics change per D3)
- HTTP: `GET /api/{teams|portfolios}/{id}/metrics/flowEfficiency` (existing; call site moves from widget to `BaseMetricsView` data layer per D7)
- UI: Team/Portfolio **Metrics → Flow Overview** dashboard
- UI: Team/Portfolio **Metrics → Flow Metrics → Work Item Aging** chart

## Wave: DISCUSS / [REF] Pre-requisites

- `WidgetShell` already supports `header` (RAG), `viewData`, and `trend` — all three chrome slots exist and need only registration in `BaseMetricsView`. No new UI primitive.
- `BlockedCountSnapshot` history is already loaded into `BaseMetricsView` and threaded into `computeBlockedTrend`.
- `BaseMetricsService.GenerateTotalWorkItemAgeByDay` / `WasItemProgressOnDay` supply the as-of-date arithmetic D3 needs.

## Wave: DISCUSS / [REF] Cross-cutting impact (DoR Item 7 hard gate)

| Surface | Impact | Verdict |
|---|---|---|
| RBAC / `IRbacAdministrationService` | None — no new endpoint, no new gate | N/A, because every read rides existing `RbacGuard`-protected metrics paths (D10) |
| Premium licensing | None | N/A, because no story is gated; these are parity fixes to already-free widgets |
| EF migrations | None | N/A, because D3 computes from existing dates; no new persisted column or table |
| Work-tracking write-back | **Must stay unchanged** | Explicit constraint D4 + CI5 + US-04 AC4 — `WriteBackValueSource` keeps today-anchored age |
| Lighthouse-Clients (CLI/MCP) | **Yes — re-opened and re-answered 2026-07-19** | The row's own re-open trigger fired: D16 (as amended by UPSTREAM-2) threads `asOf` through `WorkItemDto` + `FeatureDto`, changing the `workItemAge` **values** the team and portfolio metrics endpoints emit for historical ranges. Verified against `lighthouse-clients`: `packages/client/src/index.ts` exposes `getTeam/PortfolioWorkItemAgePercentiles` and `getTeam/PortfolioTotalWorkItemAge`, and `packages/mcp-core/src/index.ts:701,715` registers the matching MCP tools. **No shape change, so no breaking bump** — the response types are untouched. Two things are still owed: (a) the MCP tool descriptions read "optionally filtered by start and end dates", which after D3 understates the semantics and would let an LLM consumer read a historical result as current — reword to "ages reported as of the last day of the selected range"; (b) a Changeset + patch/minor release recording the value-semantics change. Both land with slice 03, per the per-feature client-versioning rule. |
| Website marketing surface | None | N/A, because these are dashboard parity fixes, not a new capability to market |
| Docs + screenshots | **Yes** | Flow Overview and Work Item Aging screenshots change; refresh at finalization (DoD 8) |
| Demo data | **None — verified 2026-07-18** | N/A, because `DemoDataFactory.ReplaceDatePlaceholders` (`DemoDataFactory.cs:102-116`) resolves `{w-N}` placeholders to N business days before today at seed time. Team Zenith alone carries 201 closed items spanning ~`{w-145}` to recent plus 4 open, so items closed well before today are abundant and slice 03's historical assertion is satisfiable as-is. Because seeding is relative to "today", an E2E can select a historical range deterministically. No extension needed. |

## Wave: DISCUSS / [REF] Story map & slices

**Backbone:** Scan the overview → read a widget's status → compare it to last period → drill into its items → investigate a metric over time.

| # | Slice | Group | Stories | Est. | Learning hypothesis |
|---|-------|-------|---------|------|---------------------|
| 01 | Throughput/Arrivals View Data + aging label | A (chrome) | US-01, US-02 | ~0.5d | Disproves that the missing chrome is pure registration if wiring `buildViewData` needs a new item source |
| 02 | Blocked trend zero-baseline | B (trend fix) | US-03 | ~0.5d | **HYPOTHESIS FIRED 2026-07-19 — see UPSTREAM-4.** The `noBaseline` path is what renders, but the cause is upstream: the baseline day is outside the fetched window. Slice is **BLOCKED** pending the user's choice of correction; scope becomes fetch-window fix + selector change (still ~0.5d) |
| 03 | Work Item Age as of selected date | C (data) | US-04 | ~1d | Disproves that as-of-date age is derivable from existing dates if `WasItemProgressOnDay` cannot reconstruct the population without a persisted snapshot |
| 04 | WIA Percentiles RAG + previous-period trend | C (data) | US-05 | ~0.5–1d | Disproves that the SLE is the right benchmark for *in-progress* age if the resulting RAG sits red permanently for healthy teams |
| 05 | Flow Efficiency RAG | C (data) | US-06 | ~0.5–1d | Disproves that lifting the fetch is mechanical if `BaseMetricsView`'s data layer cannot absorb the call without a render-loop or an extra round trip |

**Execution order: 01 → 03 → 04 → 05, with 02 re-inserted once UPSTREAM-4 is decided.** Rationale, rewritten 2026-07-19: 03 precedes 04 because 04 hard-depends on it (D5) — that is the one real ordering constraint, and it is sufficient on its own. The earlier rationale ("03 is pulled ahead despite being the largest because it carries the most uncertainty") is retired: DESIGN's revision cut 03 to ~0.5d on the finding that the population was already date-correct at both call sites and `GetTotalWorkItemAge` was already correct, so 03 is neither the largest nor the most uncertain. 01 stays first as a same-day win that costs nothing if wrong. 05 is last because it is independent of everything else and can absorb slack. 02 was second; it is now blocked on UPSTREAM-4 and slots back in wherever the correction lands.

**Carpaccio taste tests**
- *4+ new components?* No — zero new components across all five slices; every change registers into existing chrome.
- *Every slice depends on a new abstraction?* No. Only 04 depends on 03's as-of-date helper, and 03 ships that helper end-to-end with its own user-visible outcome.
- *Does any slice disprove a pre-commitment?* Yes — 03 tests "as-of-date age is derivable without new persistence" and 04 tests "SLE is the right benchmark for in-progress age". Both are real, falsifiable bets.
- *Synthetic-only data?* No — every slice is acceptance-tested against seeded demo data; 03 additionally needs demo items closed before today (flagged above).
- *Two slices identical except for scale?* No.
- **Documented deviation:** the user selected a 3-slice shape (D12). Group C at ~2.5 days fails the ≤1-day gate as one unit, so it is split into 03/04/05. The three conceptual groups are preserved.

## Wave: DISCUSS / [REF] Outcome KPIs

**Rewritten 2026-07-19 (review gate).** The previous table listed five "KPIs" that were all test assertions
— "count widget keys registered…", "backend test comparing…", "Vitest on `computeBlockedTrend`". Every one
measures that the code was written, and every one is trivially 100% the moment the slices merge. They are
Definition-of-Done items wearing a KPI hat. They have been **moved to the DoD** (items 10-12 below); the two
entries here are the only outcome measures obtainable in this product.

Lighthouse has no cross-instance telemetry phone-home, so fleet-wide behavioural measurement is genuinely
impossible. Saying so and choosing a local proxy is the honest response; relabelling the test suite is not.

| KPI | Target | Measurement |
|-----|--------|-------------|
| A past period reads true to someone who lived it | A named design partner reviews a period on their own instance and confirms the Work-Item-Age numbers match what they remember being true then — the retro use case US-04 exists for | Qualitative, one partner, recorded in the slice-03 brief. Same coach as the D6 gate, so it is one conversation, not two. |
| Questions the dashboard can now answer without leaving it | Count the Flow Overview questions that previously required a manual query or a DB look-up and no longer do — baseline the list before slice 01, re-count after slice 05 | Manual count against a written list. Small n, honestly reported; the point is the direction, not the number. |

## Wave: DISCUSS / [REF] DoR validation (9/9 — re-audited and repaired 2026-07-19)

**Two items failed the re-audit and both are now closed.** The original 9/9 was recorded before the DISTILL
review gate; items 3 and 7 did not survive it. Each failure is kept visible below with what was wrong and
what fixed it, rather than being quietly overwritten — the audit trail is the point.

1. **Business value articulated** — PASS. Six named gaps degrade the dashboard's scan-in-one-glance promise; one is a correctness bug on historical ranges.
2. **Job traceability** — PASS. Every story carries a `job_id`; three new jobs added to `docs/product/jobs.yaml`, one existing job reused.
3. **Acceptance criteria testable** — **FAIL on 2026-07-19, now PASS (closed same day).** Four defects were found and all four are fixed. (a) US-03's ACs described behaviour that had never been reachable (UPSTREAM-4) — testable in isolation, false against the shipped wiring, which is the worst combination; resolved by D2's re-decision plus AC0 and scenario 16b. (b) CI6 was a tautological oracle: it asserted the new projection against `GenerateTotalWorkItemAgeByDay`, so two definitions wrong in the same way would still agree; a second, hand-computed fixture (`GetWorkItemAgePercentiles_MatchHandComputedAgesOnTheSelectedDay`, expected ages `{1,4,4}`, total 9) now pins the arithmetic independently of the reference. (c) US-06 AC4 and slice-04 AC6 were architecture rules in AC slots and are relabelled as design constraints — still enforced by scenarios 46 and 33, no longer counted as acceptance criteria. (d) US-04 AC2 / CI4 was queried for naming no baseline-capture mechanism; on inspection it needs none — it compares the new as-of-date age against the shipped `WorkItemAge` property *in the same run*, and D4/CI5 keep that property untouched, so it is a live oracle rather than a remembered one. No golden file required.
4. **Dependencies identified** — PASS. Only 04 → 03. Documented in the slice table and slice briefs.
5. **Scope bounded** — PASS. Explicit out-of-scope list; six gaps, no adjacent-widget drift.
6. **Sized** — PASS, at the boundary. Five slices, each ≤1 day, with the group-C split documented. Noted 2026-07-19: slices 04 and 05 are estimated "~0.5–1d", so the gate is met only at the optimistic end of both. Slice 01 also bundles US-01 and US-02, which share no code path, no job and no risk — they are paired only because both are small. US-02 rides along as a rider with no dependency; it does not muddy slice 01's learning hypothesis, which covers View Data only.
7. **Cross-cutting impact assessed** — **FAIL (2026-07-19).** The Lighthouse-Clients row read "N/A, because no endpoint contract changes; re-opens if DESIGN adds one". DESIGN then added one (D16, as amended by UPSTREAM-2). The row named its own re-open trigger, the trigger fired, and the row was never revisited — so the "every row answered" claim rested on a stale answer. Now re-answered (see the table). The other five N/A-because rows were re-audited at the same time and stand.
8. **Elevator pitch per story** — PASS. All six stories carry Before/After/Decision-enabled; none is `@infrastructure`; every slice contains at least one user-visible value story.
9. **Handoff target identified** — PASS. `nw-solution-architect` (DESIGN). Two open forks remain after the 2026-07-18 clarification round: **(a)** the D4 mechanism for as-of-date age that leaves write-back untouched, and **(b)** whether `BaseMetricsView`'s data layer can absorb the flow-efficiency fetch without adding a sequential round trip. D6's band design and the demo-data question are both closed — see D6 and the cross-cutting table.

## Changed Assumptions

**Source: `docs/product/journeys/work-item-age-percentiles.yaml`** (feature `work-item-age-percentiles`, Story #5257, 2026-06-09), decision D4:

> "Every current in-progress item's total workItemAge (snapshot, not windowed); 50/70/85/95 to match cycle-time percentiles."

and its `shared_artifacts` entry:

> "Never windowed by date range (WIP is 'now')."

**New assumption (this feature, D3):** Work Item Age percentiles are computed over the population in progress on the **last day of the selected range**, aged to that day. When the range ends today the result is identical to the previous behaviour, so the original "live snapshot" reading is preserved as the default case rather than contradicted.

**Rationale:** the original assumption was consistent while the dashboard only ever showed "now", but it makes the widget silently wrong the moment a user selects a historical range — items that closed since disappear from the population and survivors are aged to today. The user established on 2026-07-18 that the age readout should follow the selected period. The prior decision is superseded, not reversed: "snapshot of live WIP" becomes "snapshot as of the selected day", of which live WIP is the today case.

**Same change applies to** `docs/product/jobs.yaml` job `job-flow-coach-gauge-wip-age-spread`, whose functional dimension reads "how old is my work RIGHT NOW". That job is annotated rather than rewritten — its emotional and social dimensions are unaffected.

DISCOVER/prior-wave documents are not modified.

---

## Wave: DESIGN / [REF] Both DISCUSS forks resolved

**Fork A (D4 — as-of-date age mechanism): resolved, and the blast radius is far smaller than DISCUSS assumed.**

DISCUSS asserted that `GetWorkItemAgePercentilesForTeam` "drops any item that has closed since". That is the observed effect but not the mechanism, and the distinction matters because it halves the work:

- `GetWipSnapshotForTeam` (`TeamMetricsService.cs:575`) queries items in `Doing OR Done` and filters them through `GenerateWorkInProgressByDay(endDate, endDate, items)[0]` — i.e. `WasItemProgressOnDay`. **The population is already date-correct**, including items that have closed since.
- `GetInProgressFeaturesForPortfolio` (`PortfolioMetricsService.cs:224`) does the identical thing for features. Also already correct.
- The sole defect is the projection: `.Select(i => i.WorkItemAge).Where(age => age > 0)`. `WorkItemAge` returns `0` for anything not `Doing` *right now*, so correctly-selected historical items are zeroed and then filtered out by the `age > 0` guard; survivors are aged to `DateTime.UtcNow`.

So no population query changes anywhere. Two projection sites and one DTO field.

**Further scope reduction:** `GetTotalWorkItemAge` (`TeamMetricsService.cs:556`) already computes via `GenerateTotalWorkItemAgeByDay(endDate, endDate, items)` and is **already date-correct**. US-04 AC4 is therefore satisfied by existing behaviour — it converts from new work into a regression assertion. Slice 03 shrinks accordingly (see revised estimate below).

**Fork B (D7 — can the data layer absorb the flow-efficiency fetch): yes, with one caveat.**

`useMetricsData.ts:230-260` already runs a `Promise.all` batch — `getCycleTimePercentiles`, `getWorkItemAgePercentiles`, `getAgeInStatePercentiles`, `getCumulativeStateTimeForTeam` — inside an effect keyed on `[entity, metricsService, startDate, endDate, cycleTimeTerm]`. Flow efficiency has an identical dependency signature and becomes a fifth parallel call. **No additional round trip.**

Caveat: that effect `await`s `getCycleTimeData` *sequentially* before reaching the `Promise.all`, so joining this batch gates flow efficiency behind cycle-time data. Today the widget fetches independently and paints early. See D18 for the resolution.

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Rationale |
|----|----------|-----------|
| **D13** | Add `int AgeOnDay(DateTime asOf)` to `WorkItemBase`. Leave the `WorkItemAge` property **byte-for-byte untouched**. | Satisfies D4's hard constraint structurally rather than by discipline: `WriteBackTriggerService.cs:188-205` reads the `WorkItemAge` property, so an unmodified property is an unmodified write-back. Note the two are *not* the same function with a different date — `WorkItemAge` carries a `StateCategory == Doing` guard that `AgeOnDay` must **not** have, because callers establish the population via `WasItemProgressOnDay` before projecting. Do not refactor `WorkItemAge` to delegate to `AgeOnDay`; they encode different questions. |
| **D14** | No changes to `GetWipSnapshotForTeam` or `GetInProgressFeaturesForPortfolio`. | Both already select the historically-correct population via `GenerateWorkInProgressByDay`. Changing them would be churn without behaviour change, and would risk the one thing that currently works. |
| **D15** | Change exactly two projections: `TeamMetricsService.cs:326` and `PortfolioMetricsService.cs:284`, from `.Select(i => i.WorkItemAge)` to `.Select(i => i.AgeOnDay(endDate))`. Drop the `.Where(age => age > 0)` guard in favour of an explicit `> 0` inside `AgeOnDay`'s contract, or retain it — but document which, because it currently doubles as the "not Doing" filter that D13 removes. | The guard's present meaning is accidental. Once ages are date-correct, a `0` can only mean "started on the day itself" (which `+1` arithmetic makes impossible) or bad data. Retaining it unexamined would silently drop legitimate items. |
| **D16** | `WorkItemDto` gains an optional `DateTime? asOf = null` constructor parameter; `WorkItemAge` becomes `asOf.HasValue ? workItem.AgeOnDay(asOf.Value) : workItem.WorkItemAge`. Only the `/wip` endpoints pass it — the other construction sites are unchanged by omission. **Extended 2026-07-18 (DISTILL, user-confirmed):** the same optional `asOf` threads through `FeatureDto`'s constructor to the `WorkItemDto` base, and `PortfolioMetricsController` `/wip` (`PortfolioMetricsController.cs:95-105`) passes it too. Without that half, the portfolio aging chart would stay today-anchored while the portfolio percentiles card moved — the two surfaces disagreeing for the same range, which US-04 AC3 and CI2 forbid. | The aging chart consumes `/wip?asOfDate=`, which *already* carries the date; the DTO simply discards it. Defaulting to the existing property keeps all other callers (FeaturesController, blocked/stale item lists) on today-anchored age with no edit, bounding the blast radius to one call site. |
| **D17** | No cache-key changes. | Every affected cache entry is already date-stamped: `WorkItemAgePercentiles_{endDate:yyyy-MM-dd}`, `WipSnapshot_{endDate:yyyy-MM-dd}`, `TotalWorkItemAge_{endDate:yyyy-MM-dd}`. Correcting the value behind an already-correct key needs no key change. **Watch-item:** deployments carrying a warm cache will serve stale pre-fix values until the date rolls or the cache is invalidated — confirm the cache does not survive a restart, and if it does, note it in the release. |
| **D18** | Flow efficiency joins the existing `Promise.all` in `useMetricsData.ts`, but **hoist it above the sequential `getCycleTimeData` await** — either by moving `getCycleTimeData` into the same `Promise.all` (it shares the dependency signature) or by placing flow efficiency in its own concurrent effect. | Answers Fork B as "yes, no extra round trip" without regressing paint time. Joining the batch naively would make Flow Efficiency wait on cycle-time data it does not need. Prefer folding `getCycleTimeData` into the batch: it removes an existing sequential await and speeds up the whole view. |
| **D19** | Extract the shared band logic from `computeCycleTimePercentilesRag` (`ragRules.ts:174-218`) into a private helper taking `(sle, values, terms, copy)`; both the cycle-time rule and the new WIA rule call it. | Per CLAUDE.md, DRY applies to *knowledge*, not shape. The 20pp red boundary and the "meet the target = green" rule are one piece of knowledge — an SLE-attainment band — deliberately shared, so a future change to the boundary lands once. The populations differ; the banding does not. Contrast with the deliberately-unshared `validatePaymentAmount`/`validateTransferAmount` case: those evolve independently, this must not. |
| **D20** | `AgeOnDay` is unit-tested on `WorkItemBase` directly; the two projection sites get NUnit service tests with EF InMemory asserting parity against `GenerateTotalWorkItemAgeByDay` at the same date; the write-back regression is an NUnit test on `WriteBackTriggerService`. Frontend rules are Vitest; E2E is Playwright via POM on demo data. | Matches the project stack (NUnit 4.6 + Moq + EF InMemory, **not** xUnit/NSubstitute). The parity assertion (CI6 in slice 03) is what prevents the two age computations drifting apart later. |

## Wave: DESIGN / [REF] DDD context

No new bounded context, no new aggregate, no domain event. This feature is entirely within the existing **Metrics** context and is best read as three separate concerns that happen to share a release:

- **Domain-model correction** (slices 03-04): `WorkItemBase` gains a query method. The ubiquitous language gains a distinction it was previously conflating — *"work item age"* (a property of an item, always meaning "now") versus *"age on a day"* (a function of an item and a date). The absence of that distinction is precisely what caused the defect; naming it is the fix.
- **Read-model presentation** (slices 01, 05): registration of already-computed values into existing view chrome. No domain involvement.
- **Pure calculation policy** (slice 04's RAG rule): a stateless banding function in the frontend presentation layer, consistent with where every other RAG rule already lives.

Ports-and-adapters position: all changes sit behind existing driving adapters (`TeamMetricsController`, `PortfolioMetricsController`) and touch no driven port. No repository interface changes — `GetAllByPredicate` usage is unchanged.

## Wave: DESIGN / [REF] Component decomposition

| Slice | Backend | Frontend | New files |
|---|---|---|---|
| 01 | — | `BaseMetricsView.buildViewData` (+2 keys); `WorkItemAgingChart.tsx:653` label | none |
| 02 | — | `blockedTrend.ts` (baseline policy) | none |
| 03 | `WorkItemBase.AgeOnDay`; `TeamMetricsService.cs:326`; `PortfolioMetricsService.cs:284`; `WorkItemDto` (+`asOf`); `TeamMetricsController` `/wip` | — (DTO field already consumed) | none |
| 04 | second call into the slice-03 computation at `startDate − 1 day` | `ragRules.ts` (+ shared band helper, + WIA rule); `buildWidgetFooters`; `categoryMetadata.trendPolicies`; `widgetInfoMetadata` | none |
| 05 | — | `useMetricsData.ts` (batch); `FlowEfficiencyOverviewWidget` → presentational; `buildWidgetFooters` | none |

**Zero new files, zero new components, zero new endpoints across the whole feature.** That is the strongest available confirmation of the DISCUSS reading that these are registration gaps rather than missing capability.

## Wave: DESIGN / [REF] Driving ports

Unchanged in count and shape. Two behaviour changes behind existing signatures:

- `GET /api/{teams|portfolios}/{id}/metrics/workItemAgePercentiles?startDate&endDate` — same contract, corrected values (D15).
- `GET /api/teams/{id}/metrics/wip?asOfDate=` — same contract, `workItemAge` in the response now honours the `asOfDate` the caller already sends (D16).
- `GET /api/{teams|portfolios}/{id}/metrics/flowEfficiency` — unchanged; only the frontend call site moves (D18).

No new route, no version bump, therefore **no Lighthouse-Clients (CLI/MCP) change** — the DISCUSS cross-cutting entry stands.

## Wave: DESIGN / [REF] Driven ports

None affected. No repository interface change, no new persistence, no EF migration, no change to any work-tracking connector. Write-back is explicitly and structurally unchanged (D13).

## Wave: DESIGN / [REF] Technology choices

Nothing new introduced. The feature is implemented entirely in the existing stack: C# .NET 10 services behind the existing controllers, React 18 + TypeScript presentation, MUI-X for the chart already in place. No new package on either side.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing asset | Reused for | Why not new code |
|---|---|---|
| `GenerateTotalWorkItemAgeByDay` arithmetic (`BaseMetricsService.cs:808`) | `AgeOnDay`'s day-arithmetic contract | Already the trusted definition of "age on a day" and already drives the over-time chart. Slice 03 asserts parity against it (CI6) so a second definition cannot drift into existence. |
| `WasItemProgressOnDay` | population selection | Already in use at both call sites; D14 changes nothing. |
| `calculateSLEStats` + band logic (`ragRules.ts:174-218`) | slice 04's WIA RAG | One knowledge, two populations — extracted per D19. |
| `WidgetShell` `header` / `viewData` / `trend` slots | all chrome work | Every slot already exists and is already rendered; five of six gaps are registration. |
| `WorkItemsDialog` + existing `throughputItems` / arrivals extraction | slice 01 | No new query; the sibling chart widgets already build these sets. |
| `computeFlowEfficiencyRag` | slice 05 | Reused verbatim; only its call site moves. |
| `Promise.all` batch in `useMetricsData` | slice 05 | Existing concurrency structure absorbs the fifth call (D18). |

## Wave: DESIGN / [REF] Revised slice estimates

DESIGN's investigation moves work off slice 03 and leaves the rest as DISCUSS estimated.

| # | Slice | DISCUSS est. | DESIGN est. | Change |
|---|---|---|---|---|
| 01 | View Data + aging label | ~0.5d | ~0.5d | — |
| 02 | Blocked trend zero-baseline | ~0.5d | ~0.5d | — |
| 03 | WIA as of selected date | ~1d | **~0.5d** | Population already correct (D14); Total Work Item Age already correct; reduces to one method, two projections, one DTO param |
| 04 | WIA RAG + trend | ~0.5–1d | ~0.5–1d | — |
| 05 | Flow Efficiency RAG | ~0.5–1d | ~0.5–1d | — |

Execution order stands: 01 → 02 → 03 → 04 → 05. Slice 03 was ordered early for its uncertainty; that uncertainty is now largely retired, but the order is still right because slice 04 depends on it.

## Wave: DESIGN / [REF] Test placement + precedent

- `AgeOnDay` — `Lighthouse.Backend.Tests/Models/` alongside existing `WorkItemBase` tests. Cover: item open on the day; item closed after the day; item closed before the day; item started after the day; `StartedDate` null falling back to `CreatedDate`.
- Projection parity — `Lighthouse.Backend.Tests/Services/Implementation/` for both Team and Portfolio metrics services, EF InMemory, asserting the percentile population and ages match `GenerateTotalWorkItemAgeByDay` at the same date (CI6).
- Live-view regression (CI4) — same test classes, `endDate = today`, asserting byte-identical output to the pre-change expectation.
- Write-back regression (CI5) — `WriteBackTriggerServiceTest`, asserting emitted age for `WorkItemAgeCycleTime` is unchanged.
- RAG rules — Vitest beside `ragRules.test.ts`; include the shared-band extraction so both rules exercise it.
- Chrome registration — Vitest in `BaseMetricsView.test.tsx`, including the parity assertion that every `flow-overview` widget key has a `buildWidgetFooters` entry (slice 05 AC7).
- E2E — Playwright POM on demo data, one `@screenshot` spec per changed theme. Demo-data adequacy confirmed (see cross-cutting table).

## Wave: DESIGN / [REF] Open questions (deferred to DELIVER)

1. **`age > 0` guard disposition** (D15) — retain or remove. Decide with the code in front of you; whichever is chosen must be commented, because the guard's current meaning is an accident of the `StateCategory` check that D13 removes.
2. **Warm-cache staleness on deploy** (D17) — confirm whether the metrics cache survives a process restart. If it does, the corrected values will not appear until the cache turns over, which is a release note, not a code change.
3. **Flow-efficiency batch placement** (D18) — folding `getCycleTimeData` into the `Promise.all` is the preferred option and speeds up the existing view, but it touches a load path outside this feature's stated scope. If that feels like scope creep at implementation time, fall back to a separate concurrent effect for flow efficiency and leave the existing sequential await alone.
4. **Optimistic bias of WIP-SLE attainment** (D6, unchanged) — slice 04's learning hypothesis. Not a design question; a validation step during that slice.

---

## Wave: DISTILL / [REF] Reconciliation + degradation log

**Wave-decision reconciliation: PASSED — 0 contradictions.** DISCUSS D1-D12, DESIGN D13-D20 and
`docs/product/journeys/widget-loose-ends.yaml` were cross-checked decision by decision. DESIGN corrects
DISCUSS's *mechanism* reading in two places (the population is already date-correct; `GetTotalWorkItemAge`
is already correct) but both are documented supersessions with rationale, not contradictions.

**Graceful degradation:** no `devops/` artifacts for this feature — **WARN, not block**. The default
environment matrix applies; no infrastructure constraint affects these tests (no new endpoint, no new
persistence, no migration). DEVOPS takes outcome-KPIs only, exactly as the wave-status line said.

**Three upstream issues raised, all three now resolved** — see `distill/upstream-issues.md`.
**UPSTREAM-1** (a slice-03 domain example contradicted `WasItemProgressOnDay`): resolved 2026-07-18 — the
example was wrong and has been corrected in the slice brief; an item closing on the selected day is
excluded. **UPSTREAM-2** (D16 omitted the portfolio half): resolved 2026-07-18 — D16 is extended to
`FeatureDto` + `PortfolioMetricsController./wip`, so slice 03's file list grows by those two.
**UPSTREAM-3** was a count error already absorbed by the ATs.

## Wave: DISTILL / [REF] Scenario list with tags

Project convention wins over the skill's Gherkin default: this repo has no `.feature` files. Acceptance
tests are NUnit (backend) and Vitest (frontend), per `docs/architecture/atdd-infrastructure-policy.md`,
which records that the Python/Hypothesis pilot artifacts do not apply here. Skip markers are NUnit
`[Ignore]` and Vitest `describe.skip`, as the polyglot matrix prescribes for C#/TS.

| # | Scenario | Slice / AC | Layer | Tags |
|---|---|---|---|---|
| 1 | Age on a day for an open item is the inclusive day count | 03 / US-04 AC1 | unit | `@in-memory` `@US-04` |
| 2 | An item that has closed since still ages to the requested day | 03 / US-04 AC1 | unit | `@in-memory` `@US-04` |
| 3 | `StartedDate` falls back to `CreatedDate`, and is preferred when both exist | 03 | unit | `@in-memory` `@edge` |
| 4 | Started on the day itself reads 1 | 03 | unit | `@in-memory` `@boundary` |
| 5 | Started after the requested day reads 0 | 03 | unit | `@in-memory` `@boundary` `@error` |
| 6 | Neither started nor created date reads 0 | 03 | unit | `@in-memory` `@error` |
| 7 | `AgeOnDay` does not disturb the `WorkItemAge` property | 03 / CI5 | unit | `@in-memory` `@regression` |
| 8 | Percentiles include an item closed after the selected day, aged to it | 03 / US-04 AC1 | service | `@real-io` `@US-04` |
| 9 | Percentiles exclude an item closed before the selected day | 03 / US-04 AC1 | service | `@real-io` `@US-04` |
| 10 | Percentiles exclude an item closed **on** the selected day | 03 / UPSTREAM-1 | service | `@real-io` `@boundary` |
| 11 | Percentiles exclude an item started after the selected day | 03 | service | `@real-io` `@error` |
| 12 | Percentile ages agree with the `GenerateTotalWorkItemAgeByDay` reference | 03 / CI6 | service | `@real-io` `@anti-drift` |
| 12b | Percentile ages match hand-computed values `{1,4,4}` on the selected day | 03 / CI6 | service | `@real-io` `@independent-oracle` |
| 13 | With `endDate` = today, every age matches the today-anchored property | 03 / CI4 | service | `@real-io` `@regression` |
| 14 | Portfolio: feature closed after the selected day is included and aged to it | 03 / CI2 | service | `@real-io` `@portfolio` |
| 15 | Portfolio: with `endDate` = today, ages match the today-anchored property | 03 / CI2 CI4 | service | `@real-io` `@portfolio` `@regression` |
| 16 | Write-back keeps emitting today-anchored age (literal, not property-derived) | 03 / CI5 | service | `@real-io` `@regression` |
| 16b | **Blocked-count history is fetched from `startDate − 1 day`, so the baseline is inside the window** | 02 / US-03 AC0 | hook | `@in-memory` `@US-03` `@wiring` |
| 17 | Blocked trend still compares against a real boundary snapshot | 02 / US-03 AC1 | unit | `@in-memory` `@regression` |
| 18 | Blocked trend still picks the latest snapshot at or before the boundary | 02 / US-03 AC1 | unit | `@in-memory` `@regression` |
| 19 | Missing boundary snapshot → baseline 0, direction rendered | 02 / US-03 AC2 | unit | `@in-memory` `@US-03` |
| 20 | Baseline 0 and current 0 → flat, never a false arrow | 02 / US-03 AC3 | unit | `@in-memory` `@boundary` |
| 21 | Entirely empty history → flat, not a no-baseline placeholder | 02 / US-03 AC3 | unit | `@in-memory` `@error` |
| 22 | Baseline 0 → percentage delta omitted, absolute values kept | 02 / US-03 AC4 | unit | `@in-memory` `@error` |
| 23 | The synthetic baseline label is present but never a fabricated `recordedAt` | 02 / US-03 AC5 | unit | `@in-memory` `@US-03` |
| 23b | Synthetic baseline is marked assumed, so an arrow is never read as measured change | 02 / US-03 AC5b | unit | `@in-memory` `@error` `@honesty` |
| 23c | Nothing at or before `endDate` → no fabricated direction (3rd `noBaselineTrend` site) | 02 / US-03 AC2b | unit | `@in-memory` `@error` `@boundary` |
| 24 | WIA RAG: amber at ≤20pp short of the SLE target | 04 / US-05 AC2 | unit | `@in-memory` `@US-05` |
| 25 | WIA RAG: red at >20pp short | 04 / US-05 AC1 | unit | `@in-memory` `@US-05` |
| 26 | WIA RAG: green at or above target | 04 / US-05 AC2 | unit | `@in-memory` `@US-05` |
| 27 | WIA RAG: exactly-met target is green, not amber | 04 | unit | `@in-memory` `@boundary` |
| 28 | WIA RAG: exactly-20pp shortfall is amber, not red | 04 | unit | `@in-memory` `@boundary` |
| 29 | WIA RAG: an age equal to the SLE value counts as within it | 04 | unit | `@in-memory` `@boundary` |
| 30 | WIA RAG: no SLE → red with the define-an-SLE tip | 04 / US-05 AC3 | unit | `@in-memory` `@error` |
| 31 | WIA RAG: empty population → **no Act status**, never the define-an-SLE tip, no divide-by-zero | 04 / US-05 AC3b | unit | `@in-memory` `@error` |
| 31b | WIA RAG: no-SLE and empty-population stay distinct answers | 04 / US-05 AC3 vs AC3b | unit | `@in-memory` `@error` `@regression` |
| 32 | WIA RAG: the tip always carries the signal, colour is never alone | 04 / US-05 AC5, CI3 | unit | `@in-memory` `@a11y` |
| 33 | WIA RAG bands identically to the cycle-time rule on the same numbers | 04 / US-05 AC6 | unit | `@in-memory` `@anti-duplication` |
| 34 | Total Throughput exposes its completed items with the cycle-time highlight | 01 / US-01 AC1 | component | `@in-memory` `@US-01` |
| 35 | Total Arrivals exposes its arrival items | 01 / US-01 AC2 | component | `@in-memory` `@US-01` |
| 36 | Empty range still offers the drill-through, with an empty set | 01 / US-01 AC3 | component | `@in-memory` `@error` |
| 37 | Aging chart labels WIA reference lines with the percentage alone | 01 / US-02 AC1 | component | `@in-memory` `@US-02` |
| 38 | Aging chart leaves the cycle-time labels unchanged | 01 / US-02 AC2 | component | `@in-memory` `@regression` |
| 39 | Both percentile sources label identically | 01 / US-02 AC1+AC2 | component | `@in-memory` `@US-02` |
| 40 | WIA Percentiles widget renders a RAG chip | 04 / US-05 AC1-3 | component | `@in-memory` `@US-05` |
| 41 | WIA Percentiles chip carries a non-empty tip | 04 / US-05 AC5, CI3 | component | `@in-memory` `@a11y` |
| 41b | WIA Percentiles: nothing in progress → no Act chip, never the define-an-SLE tip | 04 / US-05 AC3b | component | `@in-memory` `@error` |
| 42 | WIA Percentiles renders a previous-period trend via existing chrome | 04 / US-05 AC4 | component | `@in-memory` `@US-05` |
| 43 | Flow Efficiency renders the unchanged rule's status | 05 / US-06 AC1 | component | `@in-memory` `@US-06` |
| 44 | Wait states unconfigured → body kept, no misleading colour | 05 / US-06 AC2 | component | `@in-memory` `@error` |
| 45 | No data in scope → body kept, no misleading colour | 05 / US-06 AC3 | component | `@in-memory` `@error` |
| 46 | Flow efficiency is fetched exactly once, via the shared data layer | 05 / US-06 AC4 | component | `@in-memory` `@US-06` |
| 47 | **Every** Flow Overview widget has a registered RAG footer | 05 / US-06 AC7 | component | `@in-memory` `@kpi` `@structural` |

**52 scenarios. Error/edge/boundary-tagged: 21 (40%)** — the target is met.

The count history is worth keeping, because it is the more useful artifact. The first pass claimed
19/47 = 40%; the review gate measured the tags and found 17/47 = **36%**. That was corrected to 36% and
left there rather than re-tagged, and the shortfall then did its job: it prompted a hunt for *genuinely*
uncovered error paths, which found four real ones — the third `noBaselineTrend()` return site that
UPSTREAM-3 had counted but no scenario covered (23c), the honest-labelling guard on the synthetic
baseline (23b), the AC3/AC3b split at rule level (31b), and AC3b at the rendered widget where the
rule-to-chip mapping actually lives (41b). Padding with contrived cases would have hit 40% faster and
been worth nothing.

## Wave: DISTILL / [REF] Architecture-of-reference treatment (replaces per-feature WS strategy)

DISCUSS D11 recorded "WS strategy C, no walking skeleton" — kept as historical record. Under the current
model the choice is structural, not per-feature, and is already fixed by
`docs/architecture/atdd-infrastructure-policy.md`. No new port is introduced by this feature, so **no new
policy row is needed and the file is unchanged** (`--policy=inherit`).

Applied treatments:

| Port in scope | Class | Treatment (from the policy) |
|---|---|---|
| `GET /metrics/workItemAgePercentiles` (team + portfolio) | driving | exercised at the service seam with real EF-shaped repositories via Moq-backed `GetAllByPredicate` — the shipped precedent for every `*MetricsServiceTests` class |
| `GET /metrics/wip` (team + portfolio) | driving | same |
| `GET /metrics/flowEfficiency` | driving | driven from the React data layer with a mocked `IMetricsService`, matching every other widget test |
| Flow Overview / Work Item Aging UI | driving | React Testing Library against the real `BaseMetricsView` with the shipped `WidgetShell` mock |
| `IWorkItemRepository`, `IRepository<Feature>` | driven internal | real query semantics via predicate compilation, per policy |
| `IWriteBackService` | driven external | `Mock<IWriteBackService>` with output capture, per policy |

**No walking-skeleton scenario is added.** This is a brownfield feature that introduces zero new files,
zero new components and zero new endpoints (DESIGN component-decomposition table); every path it touches is
already covered end to end by shipped E2E specs. Adding a skeleton would prove wiring that is not in
question. Scenario 47 is the structural stand-in that actually matters here: it fails the moment a Flow
Overview widget ships without a status.

## Wave: DISTILL / [REF] Adapter coverage (Mandate 6)

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| `IWorkItemRepository` (team WIP + percentiles) | YES | 8-13, 16 |
| `IRepository<Feature>` (portfolio WIP + percentiles) | YES | 14, 15 |
| `IWriteBackService` (write-back emission) | YES | 16 |
| `IMetricsService` frontend adapter (flow efficiency) | YES | 43-46 |
| `IBlockedItemService` | N/A | not touched — slice 02 changes a pure selector over already-loaded history, no adapter involved |

Zero `NO — MISSING` rows.

## Wave: DISTILL / [REF] Scaffolds (Mandate 7)

Two production symbols do not exist yet, so the ATs would not compile. Both are scaffolded to keep the
suite **RED, not BROKEN**, and both carry a `__SCAFFOLD__` marker for machine detection:

| Scaffold | File | Shape |
|---|---|---|
| `WorkItemBase.AgeOnDay(DateTime)` | `Lighthouse.Backend/Models/WorkItemBase.cs` | real not-started guard (settled behaviour), arithmetic returns 0 |
| `computeWorkItemAgePercentilesRag(sle, ages, terms)` | `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.ts` | returns `{ ragStatus: "red", tipText: "" }` |

Neither throws. `TreatWarningsAsErrors` plus Sonar's S3717 make a `NotImplementedException` scaffold a
build failure here, and a throwing scaffold is a live hazard if anything reaches it before DELIVER. A
returning stub is the safer C#/TS equivalent of the "assertion failure, not infrastructure error"
principle — the skipped ATs fail on the assertion the moment they are un-skipped, which is exactly the
signal Mandate 7 asks for. `grep -rn "__SCAFFOLD__"` must return zero once slices 03 and 04 land.

## Wave: DISTILL / [REF] Test placement

| Tests | File | Precedent |
|---|---|---|
| 1-7 | `Lighthouse.Backend.Tests/Models/WorkItemBaseAgeOnDayTest.cs` (new) | `FeatureTest.cs`, `BlockedCountSnapshotTests.cs` in the same folder |
| 8-15 | `Lighthouse.Backend.Tests/Services/Implementation/WorkItemAgeAsOfDateTest.cs` (new) | `TeamMetricsServiceSnapshotTests.cs` — feature-suffixed fixture, same folder |
| 16 | `WriteBackTriggerServiceTest.cs` (appended) | sits beside the shipped `WorkItemAgeCycleTime` cases |
| 16b | `useMetricsData.test.ts` (appended to the blocked-count describe) | the file's own `getBlockedCountHistory` cases — the only layer that can catch the UPSTREAM-4 window defect |
| 17-23 | `blockedTrend.test.ts` (appended describe) | the file's own existing B3 describe |
| 24-33 | `ragRules.test.ts` (appended describe) | one describe per rule, as throughout |
| 34-36, 40-47 | `BaseMetricsView.test.tsx` (appended describes) | the shipped `M3`–`M6 RAG Footers` describes |
| 37-39 | `WorkItemAgingChart.test.tsx` (nested describe) | inside the existing percentile-source-selector describe |

## Wave: DISTILL / [REF] RED classification (pre-DELIVER fail-for-the-right-reason gate)

Both suites were run with the skip markers stripped, then restored.

**Backend** — 12 failed, 4 passed of 16. Every failure is an NUnit assertion failure
(`MISSING_FUNCTIONALITY`). Zero compile, fixture, or setup errors. The 4 passes are scenarios 5, 6, 10 and
11, which assert behaviour that is *already* correct — they are regression guards, and passing green from
the start is the right outcome for them.

**Frontend** — 14 failed of the 24 un-skipped, all `AssertionError` on the expected value
(`expected 'none' to be 'up'`, `expected 'red' to be 'amber'`, `expected '' to contain 'SLE'`). Zero import
or fixture failures. The other 10 are the unchanged-behaviour regression guards.

**One test was rejected by this gate and rewritten.** Scenario 23 originally asserted only
`expect(recordedAts).not.toContain(previousLabel)`, which passes **vacuously** against today's `undefined`
label — a `WRONG_ASSERTION` that would have gone green in DELIVER without the behaviour existing. It now
asserts the label is defined first. Two backend scenarios (10, 11) were also corrected: they asserted an
empty percentile list, but `BuildPercentiles` always emits the four 50/70/85/95 entries and represents an
empty population as zero *values*.

Full suites green with markers restored: backend **3454 passed / 20 skipped**, frontend **3576 passed /
31 skipped**, `tsc -b` clean, Biome clean.

## Wave: DISTILL / [REF] E2E — deliberately deferred to each slice's DELIVER step

**Not an N/A.** DoD item 7 and every slice's AC8 require Playwright coverage, and it is still required.
It is not authored here, for two compounding reasons:

1. A `@screenshot` spec asserting a status chip that does not render yet cannot be run, and the standing
   rule in this project is that a spec or POM locator is never committed unrun.
2. The POM additions the specs need (status-chip accessors on the Flow Overview page object, a View Data
   opener for the two new widgets) are only writable against real rendered markup.

**Contract for DELIVER:** each slice adds its own E2E before that slice is called done — slice 01 the View
Data drill-through, slice 02 the directional arrow on Blocked, slice 03 a historical range showing non-zero
WIA percentiles, slices 04 and 05 the status chips (asserting the status attribute, never a pixel). All
POM-mediated, driven by seeded demo data, run locally before commit. Demo-data adequacy is already
confirmed (DISCUSS cross-cutting table).

## Wave: DISTILL / [REF] Outcomes registry

**Skipped, with reason.** The registry tracks new typed contract surfaces. This feature introduces no new
rule module, CLI subcommand, endpoint, or system-wide invariant — DESIGN's component table records zero new
files, components and endpoints across all five slices. `AgeOnDay` and `computeWorkItemAgePercentilesRag`
are internal helpers behind existing contracts, not new promises about what the system does. The one
genuinely new system-wide claim — *every Flow Overview widget exposes a status* — is registered where it
can actually fail: scenario 47.

## Wave: DISTILL / [REF] Pre-requisites for DELIVER

- **No blockers.** UPSTREAM-4 was raised and resolved on 2026-07-19: the final-wave review gate ran the
  diagnostic slice 02 had promised, found the blocked-trend baseline lookup sits one day outside its own
  fetch window, and the user re-decided D2 the same day. Slice 02 now carries **two** changes in order —
  widen the fetch (AC0, scenario 16b at the `useMetricsData` seam), then the zero-baseline fallback
  (AC2-AC4, scenarios 17-23) — and DELIVER must do them in that order. Still ~0.5d.
- UPSTREAM-1 and UPSTREAM-2 were both answered on 2026-07-18 and folded back into the slice brief and D16
  respectively. Slice 03's file list now includes `FeatureDto` and `PortfolioMetricsController`, and — new
  on 2026-07-19 — an MCP tool-description reword plus a Changeset in `lighthouse-clients` (the cross-cutting
  row that was answered "N/A" had a re-open trigger that had already fired).
- Slice 04 now opens with the D6 validation gate (named coach, two deteriorating periods, GREEN on either →
  stop and re-open D6) before any code, and its AC3 is split into AC3/AC3b so an empty population no longer
  borrows the unconfigured-SLE answer.
- Execution order: 01 → 02 → 03 → 04 → 05, restored now that UPSTREAM-4 is resolved. Slice 04's ATs must not be un-skipped before slice
  03 lands — pre-fix, both periods age to today, so the trend reads flat and scenario 42 would pass for the
  wrong reason.
- Three shipped expectations are superseded by design and must be **deleted**, not repaired: the
  `noBaseline` case in `blockedTrend.test.ts` (slice 02) and the prefixed-label expectations in
  `WorkItemAgingChart.test.tsx` (slice 01) — **seven tests there, not two**; the corrected count and
  the full list are in that file's inline marker. Each is flagged inline at the point of change.
- DESIGN open question 1 (the `age > 0` guard) is **answered** by scenario 5: `AgeOnDay` returns 0 for a
  not-yet-started item, so the guard keeps a real meaning and is retained. Open questions 2 (warm-cache
  staleness) and 3 (batch placement) remain live for DELIVER.

---

## Wave: DELIVER / [REF] Implementation summary

**PARTIAL — slices 01, 02, 03 and 05 shipped; slice 04 held; E2E outstanding.** Run of 2026-07-19.

Four of the six reported Flow Overview gaps are closed. Slice 01 registered the two missing `viewData`
payloads and dropped the redundant term prefix from the aging chart's percentile labels — confirming the
DISCUSS reading that these are registration gaps, since no new item source or fetch was needed. Slice 02
fixed the blocked-trend defect in the two ordered steps D2 requires: widen the fetch to reach the baseline
day (Bug #5521 — the trend had never rendered on any instance since it shipped), then treat a still-absent
baseline as zero, marked as assumed so an arrow is never read as measured change. Slice 03 made every
Work-Item-Age surface report as of the selected range end via a new `WorkItemBase.AgeOnDay`, leaving the
`WorkItemAge` property byte-for-byte untouched so write-back semantics are structurally unchanged. Slice 05
lifted the flow-efficiency fetch into the shared `useMetricsData` batch, made the widget presentational, and
registered its RAG footer — taking DESIGN open question 3's preferred option, folding `getCycleTimeData` into
the same `Promise.all` and removing a pre-existing sequential await.

**Slice 04 (WIA Percentiles RAG + previous-period trend) was not started.** It opens with the D6 validation
gate, which requires a named human coach to judge two deteriorated periods on a real instance. The user
elected on 2026-07-19 to stop before it rather than waive or fake that gate. Its two `__SCAFFOLD__` markers
(`ragRules.ts:924`, and the companion type) therefore remain in the tree, so the feature is **not** finalizable.

## Wave: DELIVER / [REF] Files modified

**Production — backend**
- `Models/WorkItemBase.cs` — `AgeOnDay(DateTime)` replaces the scaffold; `WorkItemAge` untouched (D13/CI5).
- `Services/Implementation/TeamMetricsService.cs`, `PortfolioMetricsService.cs` — the two projections (D15).
- `API/DTO/WorkItemDto.cs`, `FeatureDto.cs` — optional `asOf` (D16 + UPSTREAM-2).
- `API/TeamMetricsController.cs`, `PortfolioMetricsController.cs` — both `/wip` endpoints pass `asOfDate`.

**Production — frontend**
- `MetricsView/BaseMetricsView.tsx` — `buildViewData` +`totalThroughput`/+`totalArrivals`; `buildWidgetFooters` +`flowEfficiency`; arrivals extraction hoisted to one call.
- `Charts/WorkItemAgingChart.tsx` — unprefixed percentile labels (D9).
- `hooks/useMetricsData.ts` — blocked-history fetch widened by one day (AC0); flow efficiency joins the batch with `getCycleTimeData` folded in (D18).
- `MetricsView/blockedTrend.ts` — zero-baseline policy, assumed-baseline hint, third no-baseline site retained.
- `MetricsView/FlowEfficiencyOverviewWidget.tsx` — now presentational.

**Tests** — `WorkItemBaseAgeOnDayTest.cs`, `WorkItemAgeAsOfDateTest.cs`, `WriteBackTriggerServiceTest.cs`, `BaseMetricsView.test.tsx`, `WorkItemAgingChart.test.tsx`, `blockedTrend.test.ts`, `useMetricsData.test.ts`, `FlowEfficiencyOverviewWidget.test.tsx`.

**Docs** — `distill/upstream-issues.md` (UPSTREAM-5, UPSTREAM-6), `deliver/roadmap.json`, this file.

## Wave: DELIVER / [REF] Scenarios green count

**42 of 53** as of 2026-07-19. (The DISTILL header says 52; its table has 53 rows — the sub-lettered
`12b/16b/23b/23c/31b/41b` are easy to miscount. Logged in the roadmap's validation notes.)

- Slice 01 — 6/6 (34-39) · Slice 02 — 10/10 (16b, 17-23c) · Slice 03 — 17/17 (1-16, 12b) · Slice 05 — 4/5 (43-46; **47 skipped**, see UPSTREAM-6)
- Slice 04 — 0/15 (24-33, 40-42, 41b, 31b) — not started, held on the D6 gate.

## Wave: DELIVER / [REF] Quality gates

| Gate | Result |
|---|---|
| `pnpm test` | 3594 passed / 16 skipped / **0 failed** |
| `pnpm build` | exit 0, zero errors and warnings (Biome clean via prebuild) |
| `dotnet build` | succeeded, no new warnings |
| `dotnet test` | 3472 passed / 3 skipped / **0 failed** |
| E2E (Playwright) | **NOT RUN — outstanding**, see below |
| Mutation (per-feature ≥80%) | not run — deferred to feature completion |
| `des-verify-integrity` | **exit 1** — see the integrity note below |
| `grep -rn __SCAFFOLD__` | **2 hits remain** (slice 04) — feature not finalizable |

## Wave: DELIVER / [REF] DoD check (12-item)

1. Six gaps closed, Team + Portfolio — **PARTIAL.** Four closed at both scopes; the two slice-04 gaps (WIA RAG, WIA trend) remain open.
2. `pnpm test` green, `pnpm build` clean, Biome clean — **PASS.**
3. `dotnet build` zero warnings, `dotnet test` green — **PASS.**
4. SonarCloud no new issues — **NOT VERIFIED.** Nothing pushed, so no PR analysis has run.
5. Vitest coverage for each new rule and the changed `blockedTrend` path — **PARTIAL.** `blockedTrend` fully covered incl. 0-baseline and both-zero; the WIA `ragRules` rule is slice 04.
6. Backend pins as-of-date arithmetic against the reference + write-back regression — **PASS.** Two independent oracles (reference parity and hand-computed) plus the CI5 guard.
7. E2E `@screenshot` per changed theme, demo-data driven, POM-mediated, run locally — **NOT DONE.**
8. Docs + screenshots refreshed at finalization — **NOT DONE** (finalization not reached).
9. Mutation ≥80% — **NOT RUN.**
10. Chrome parity asserted as a test — **BLOCKED.** Scenario 47 is the assertion; skipped pending slice 04 (UPSTREAM-6).
11. Zero divergence vs `GenerateTotalWorkItemAgeByDay` — **PASS** (scenario 12, plus 12b independently).
12. Zero changed values when `endDate` = today; zero write-back change — **PASS** (scenarios 13, 15, 16).

## Wave: DELIVER / [REF] Demo evidence

**N/A, because this feature ships no CLI surface.** Every US-01..US-06 Elevator Pitch is a UI path
("open Team → Metrics → Flow Overview → …"), not a `run X → see Y` command, so the Phase-3.5 demo-command
gate has nothing to execute. The equivalent evidence for a dashboard feature is the Playwright E2E contract
DISTILL deferred to each slice — which is **still outstanding** and is the honest gap here, not a passed gate.

## Wave: DELIVER / [WHY] DES integrity — orchestrator-implemented steps

`des-verify-integrity` exits **1**. This is a known, deliberate state, recorded rather than worked around.

Only step **05-01** carries a genuine execution-log trace. Five implemented steps — `01-01`, `02-01`,
`02-02`, `03-01`, `03-02` — have **no DES record**, because they were written by the orchestrator in the main
conversation rather than by a DES-monitored crafter subagent.

**Root cause:** `~/.claude/settings.json` carried `permissions.deny: ["Read", "Grep", "Glob"]`. Deny beats
allow in Claude Code, and subagents inherit the deny but *not* the `mcp__lean-ctx__*` allows, since MCP
servers are not granted to subagents. Every dispatched subagent therefore had zero file access — the
`nw-solution-architect` dispatched for the roadmap aborted for exactly this reason and correctly refused to
fabricate one. With delegation impossible, the user authorised main-thread implementation on 2026-07-19,
accepting that finalize would be blocked. Those edits went through `ctx_patch` (MCP), which the DES
enforcement hook does not intercept — it gates only native `Write`/`Edit`. The bypass was incidental, not
deliberate, and once the hook surfaced it the orchestrator stopped rather than continue routing around it.
The deny list was then cleared and slice 05 ran properly as a monitored crafter Task.

**Decision (user, 2026-07-19):** leave the five steps unlogged. Re-dispatching crafters to "log" them now
would assert a RED→GREEN cycle that did not happen in that order, which is precisely the fraud the DES
anti-fraud rules exist to prevent. A false-but-passing log is worse than an honest failing one. The code
itself is not in question: all four slices are committed, fully green, and each carries the acceptance tests
DISTILL authored.

**Consequence:** finalize stays blocked. That is correct and costs nothing here, because slice 04 must land
before the feature can finalize regardless.

## Wave: DELIVER / [WHY] Upstream issues

**UPSTREAM-5 (RESOLVED in place)** — three slice-01 ATs carried oracles the render cannot satisfy: two
compared against sibling chart widgets absent from the flow-overview screen, one asserted list equality the
fixtures make impossible. Also, the `ChartsReferenceLine` mock keyed its test-id off the label, so once both
sources labelled identically several *surviving* tests would have gone silently vacuous. Fixed by exposing
`data-y` and asserting values. Lesson recorded for RED classification: a scenario failing because the queried
element does not exist is a different signal from one failing on an expected value, and the two should be
reported separately rather than counted together.

**UPSTREAM-6 (OPEN)** — scenario 47 asserts a category-wide invariant from inside slice 05 but iterates a
widget set including slice 04's deliverable, so no correct slice-05 implementation can satisfy it. Interim
`it.skip` with an inline un-skip trigger; permanent re-siting to a standalone guard step deferred to slice 04.

**Marker undercount, twice.** DISTILL's supersession markers named one superseded test where two existed
(slice 02), having already been corrected once from two to seven (slice 01). Worth a habit change: state
superseded tests as an enumerated list, not a count.

## Wave: DELIVER / [REF] D6 gate RESULT and D6-REVISED — the rule changed

**The gate fired. D6 as designed is withdrawn and replaced.** Run 2026-07-19, coach: the user (Benjamin),
on their own instance with real data. Periods nominated BEFORE any output was computed, per the gate's
falsifiability requirement.

**Result — team 34 "Lighthouse Dev Team", SLE = 90% within 2 days:**

| Period | In progress on end date | Ages | Share within SLE | Old rule verdict |
|---|---|---|---|---|
| A — 2026-07-11 … 07-17 | 1 item | `[1]` | 100% | **GREEN** |
| B — 2026-06-21 … 06-28 | 0 items | — | undefined | empty |

Period A read GREEN on a period the coach had nominated as deteriorated. That is the abort criterion, so
D6 was re-opened rather than shipped.

**What the gate actually revealed, which is more interesting than a failed threshold.** The coach noted the
period had many days with **0 throughput AND 0 WIP**. The deterioration was *throughput-shaped* — no work
flowing — not *age-shaped*. A widget answering "how old is my in-progress work" genuinely cannot detect
"there is almost no in-progress work", and reporting green there is arguably correct: WIP was not ageing
because there was barely any. So D6's original fear (optimistic bias flattering young WIP during a decline)
was **neither confirmed nor refuted** by this data — these periods are not the test case for it. What the run
*did* prove is that a **percentage over a population of 1 is noise**: "100% attainment" from a single item
carries no signal at all. That is what killed the share-based design.

**Period B's empty population is legitimate, not UPSTREAM-7.** 0 WIP means the empty read is correct.
UPSTREAM-7 remains open on the demo-data evidence (where the WIP-over-time chart *did* show items); this run
is not further evidence for it.

### D6-REVISED (user decision, 2026-07-19) — absolute-count bands, not share bands

Evaluated over in-progress items as of `endDate` (per D3), where `V` = the SLE's **day value** only:

| Condition | Status |
|---|---|
| No SLE configured | **RED** (with the define-an-SLE tip) |
| More than one item with age **> V** | **RED** (Act) |
| Exactly one item with age **> V**, or one or more items with age **== V** | **AMBER** (Observe) |
| All in-progress ages **< V** | **GREEN** (Sustain) |
| **Empty population** (nothing in progress) | **GREEN** |

Three consequences the user explicitly confirmed:

1. **The SLE percentile is discarded.** Only the day value is used. The chip is deliberately independent of
   the configured probability, because a percentage is meaningless at the WIP sizes this widget sees.
2. **Absolute counts do not scale, and that is accepted.** Two breaching items reads RED whether the WIP is
   3 or 40. Noted risk: on a portfolio view carrying many in-progress features, RED could become permanent
   and stop carrying information. Accepted as-is for now.
3. **Empty population is GREEN, not "no status".** This **supersedes US-05 AC3b**, which required no Act
   status plus a "no work in progress in this range" body. Rationale: nothing in progress is not a bad state,
   and the **WIP RAG already signals it** — no need to say the same thing twice on the same dashboard.

### Consequences for the shipped acceptance tests — DISTILL scenarios 24-33 are now largely invalid

This is not a tweak; it replaces the rule's entire band structure, so the ATs authored against the old design
no longer describe intended behaviour:

- **Scenarios 24, 25, 26, 27, 28** (amber at ≤20pp short, red at >20pp, green at/above target, exactly-met is
  green, exactly-20pp is amber) — all encode share-based banding. **Invalid; must be re-authored.**
- **Scenario 29** (an age equal to the SLE value counts as within it) — **inverted**. Under D6-REVISED an age
  exactly at `V` is now an AMBER trigger, not a pass.
- **Scenario 31 / 31b** (empty population → no Act status, distinct from no-SLE) — **superseded** by the
  GREEN decision above.
- **Scenario 33** (WIA RAG bands identically to the cycle-time rule) — **invalid**, and with it **D19**.
  The two rules no longer share band logic, so there is no longer one piece of knowledge to extract. The
  planned shared-helper refactor is **withdrawn**: extracting a helper now would couple two rules that have
  deliberately diverged, which is the opposite of what DRY asks for.
- **Scenario 30** (no SLE → red with the define-an-SLE tip) — **still valid, unchanged.**
- **Scenario 32** (the tip always carries the signal, colour never alone, CI3) — **still valid.**

DELIVER does not normally re-author ATs (that is DISTILL's job), but the acceptance criteria themselves were
changed by the product owner, so re-authoring is the correct response rather than a boundary violation. It
must be recorded as such — which is what this section is.

## Wave: DELIVER / [REF] Outstanding work

1. **E2E specs** — `01-02`, `02-03`, `03-04`, `05-02`. DISTILL deferred these to DELIVER deliberately (a spec asserting a chip that does not render yet cannot be run, and this project never commits an unrun spec).
2. **`03-03`** — lighthouse-clients MCP tool-description reword + Changeset, in the separate repo.
3. **Slice 04** — behind the D6 human validation gate. Un-skips scenario 47 and clears the last `__SCAFFOLD__`.
4. **Then** — mutation testing, adversarial review, integrity, docs + screenshots, finalize.
