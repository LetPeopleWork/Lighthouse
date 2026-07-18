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

## Wave: DISCUSS / [REF] The finding that changes this from polish to a bug fix

Five of the six reported gaps are missing chrome — a widget that never registered its RAG footer, `viewData` payload, or trend policy in `BaseMetricsView`. The chrome itself (`WidgetShell`) already supports all three; the widgets simply do not feed it. Those are wiring.

The sixth is different. `WorkItemBase.WorkItemAge` (`Models/WorkItemBase.cs:71`) computes `GetDateDifference(referencedDate, DateTime.UtcNow)` **and** returns `0` unless `StateCategory == Doing` *right now*. Every Work-Item-Age surface therefore reports age **as of today**, no matter which date range is selected:

- `GetWorkItemAgePercentilesForTeam(team, endDate)` (`TeamMetricsService.cs:319`) correctly takes the WIP snapshot as of `endDate` — then ages every item to today, and drops any item that has closed since (`age > 0` filter removes it).
- The Work Item Aging chart plots `item.workItemAge` (`WorkItemAgingChart.tsx:212`) — same today-anchored value.

Select last month and the ages are wrong and the population is incomplete. This is the root cause behind "Work Item Percentiles has no trend with the previous period": there is no honest previous-period value to compare against until age becomes a function of a date.

The primitive to fix it already exists and is already trusted: `BaseMetricsService.GenerateTotalWorkItemAgeByDay` (`BaseMetricsService.cs:808`) computes, for any given day, `items.Where(i => WasItemProgressOnDay(currentDate, i))` and `age = (currentDate - (StartedDate ?? CreatedDate)) + 1`. No new persistence, no new snapshot table.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | Feature type | **User-facing.** Six visible dashboard gaps. No infrastructure-only escape valve. |
| **D2** | Blocked-Items trend baseline | **LOCKED (user, 2026-07-18): keep the boundary, treat a missing baseline as 0.** Baseline stays "the latest `BlockedCountSnapshot` at or before `startDate − 1 day`". The only change: when no such snapshot exists, assume `blockedCount = 0` rather than returning the `noBaseline` marker. A fresh instance therefore reads "+N since we started recording" instead of a neutral "—". The `noBaseline` field on `TrendPayload` stays for other widgets. |
| **D3** | Work Item Age is a function of the selected date | **LOCKED (user, 2026-07-18): every Work-Item-Age surface reports age as of the LAST DAY of the selected range (`endDate`), not as of now.** Population = items in progress *on that day* (`WasItemProgressOnDay`), age = `(endDate − started) + 1`. Applies to the Work Item Age Percentiles card, the Total Work Item Age overview widget, and the dot heights on the Work Item Aging chart. When `endDate` is today the rendered numbers are unchanged — this is a correctness fix for historical ranges, not a redefinition of the live view. |
| **D4** | Where D3 is implemented | **Backend, and NOT by changing the `WorkItemAge` property.** `WorkItemAge` is also consumed by write-back (`WriteBackValueSource.cs`); repointing it at an arbitrary date would change what Lighthouse writes into Jira/ADO. DESIGN picks the mechanism (an `AgeOnDay(date)` helper on `WorkItemBase` reusing the `GenerateTotalWorkItemAgeByDay` arithmetic is the obvious candidate) under the hard constraint: **write-back semantics unchanged**. |
| **D5** | Work Item Age Percentiles trend | **Previous-period, derived from D3** — the same as-of-date computation evaluated at `startDate − 1 day`, compared against the `endDate` value. This is why D3 must land first; it is the enabling correctness fix, not a separate feature. Trend policy for `workItemAgePercentiles` moves `"none"` → `"previous-period"`. |
| **D6** | Work Item Age Percentiles RAG | **CLARIFIED 2026-07-18.** New rule in `ragRules.ts` reusing the existing `calculateSLEStats` shape **verbatim**: compute the share of in-progress items whose age (as-of-`endDate`, per D3) falls within `sle.value`, compare that share against the `sle.percentile` target, and apply the shipped bands from `computeCycleTimePercentilesRag` — green when the target is met, red when more than 20pp short, amber otherwise, and red with the "define an SLE" tip when no SLE is configured. Only the population changes (in-progress ages instead of completed cycle times). **No invented amber band, no new threshold, no new setting.** Known bias, accepted: WIP-within-SLE flatters a young WIP profile, because an item at 2 days counts as "within 14" though it may still breach. Directionally sound — an in-progress item already past the SLE *will* breach — and validated in slice 04 rather than blocked on. |
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
Decision enabled: I read the band a dot has crossed at a glance, without parsing a label that repeats the mode I just selected.

**Acceptance criteria**
- AC1: Given the aging chart with percentile source `workItemAge`, each reference line's label is exactly `{percentile}%` with no term prefix.
- AC2: Given the percentile source `cycleTime`, the label is unchanged from today's behaviour.

### US-03 — Blocked trend that reads from day one
As a delivery lead, I want the Blocked Items trend to show a real comparison even before a full previous period of snapshots exists, so the widget tells me something on the instance I just set up.
`job_id: job-delivery-lead-tell-blocked-trend-vs-last-period`

#### Elevator Pitch
Before: on a young instance the Blocked widget's trend shows a neutral "—" with a hint about waiting for a baseline, so it reads as broken rather than as informative.
After: open **Team → Metrics → Flow Overview** → the **Blocked Items** widget shows an arrow and delta against the blocked count at `startDate − 1 day`, counting an absent snapshot as 0.
Decision enabled: I decide whether blocking is growing or shrinking in this period, on day one, without waiting a period for the record to fill.

**Acceptance criteria**
- AC1: Given a `BlockedCountSnapshot` exists at or before `startDate − 1 day`, the trend compares the current count against it — unchanged from today (`blockedTrend.ts` boundary logic preserved).
- AC2: Given **no** snapshot exists at or before `startDate − 1 day`, the baseline is `0`; a current count of N renders direction `up` rather than the `noBaseline` placeholder.
- AC3: Given no snapshot exists and the current count is also 0, the trend renders `flat` — not an arrow implying change.
- AC4: The percentage delta is omitted when the baseline is 0 (division by zero), matching the existing `formatDelta` guard; the arrow and the absolute current/previous values still render.

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
- AC3: Given no SLE is configured, or zero items are in the population, the widget renders RED with the "define an SLE in settings" tip — matching `computeCycleTimePercentilesRag`'s existing handling, not a bespoke neutral state.
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
- AC4: Flow efficiency data is fetched once through the `BaseMetricsView` data layer, not by the widget — asserted by the widget rendering from props with no service call of its own.

## Wave: DISCUSS / [REF] Acceptance criteria (cross-story invariants)

- **CI1** — No new endpoint requires a new RBAC guard; every read is already covered by `[RbacGuard(TeamRead/PortfolioRead)]` (D10).
- **CI2** — Every story is Team **and** Portfolio scope. A widget fixed for teams and left broken for portfolios is not done.
- **CI3** — RAG colour is never the only signal; the `WidgetShell` chip carries the Act/Observe/Sustain label and tip text, as today.
- **CI4** — When the selected range ends today, all rendered numbers are byte-identical to pre-change behaviour (the D3 regression guard).
- **CI5** — Write-back output is untouched by this feature (D4).

## Wave: DISCUSS / [REF] Out-of-scope

- Any new persisted snapshot table for Work Item Age. D3 is computed from existing transition/started/closed dates via the `WasItemProgressOnDay` primitive.
- New user-configurable thresholds, and new RAG bands of any kind. D6 reuses the shipped `calculateSLEStats` bands verbatim; a WIP-specific SLE distinct from the cycle-time SLE is explicitly not in this story (it would re-open D6 — see slice 04's learning hypothesis).
- Changing `computeFlowEfficiencyRag`'s thresholds (D7 reuses the rule as-is).
- Widgets outside Flow Overview and the Work Item Aging chart. The Predictability and Portfolio categories are not audited here.
- Retro-filling `BlockedCountSnapshot` history. D2 assumes 0, it does not backfill.
- Lighthouse-Clients (CLI/MCP) changes — no new or changed endpoint contract is expected; if DESIGN introduces one, the client-versioning checklist re-opens.

## Wave: DISCUSS / [REF] Definition of Done (9-item)

1. All six reported gaps closed, Team and Portfolio scope (CI2).
2. `pnpm test` green; `pnpm build` zero errors/warnings; Biome clean.
3. `dotnet build` zero warnings; `dotnet test` green.
4. SonarCloud: no new issues of any severity.
5. Vitest coverage for each new `ragRules` rule and the changed `blockedTrend` baseline path, including the 0-baseline and both-zero cases.
6. Backend tests pin the as-of-date age arithmetic against the `GenerateTotalWorkItemAgeByDay` reference, plus a write-back-unchanged regression test (CI5).
7. E2E: one `@screenshot` spec per changed theme, driven by demo data, POM-mediated; run locally before commit.
8. Docs + screenshots refreshed at feature finalization (per-feature, not batched).
9. Mutation testing ≥80% kill rate on the changed FE and BE units.

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
| Lighthouse-Clients (CLI/MCP) | None expected | N/A, because no endpoint contract changes; re-opens if DESIGN adds one |
| Website marketing surface | None | N/A, because these are dashboard parity fixes, not a new capability to market |
| Docs + screenshots | **Yes** | Flow Overview and Work Item Aging screenshots change; refresh at finalization (DoD 8) |
| Demo data | **None — verified 2026-07-18** | N/A, because `DemoDataFactory.ReplaceDatePlaceholders` (`DemoDataFactory.cs:102-116`) resolves `{w-N}` placeholders to N business days before today at seed time. Team Zenith alone carries 201 closed items spanning ~`{w-145}` to recent plus 4 open, so items closed well before today are abundant and slice 03's historical assertion is satisfiable as-is. Because seeding is relative to "today", an E2E can select a historical range deterministically. No extension needed. |

## Wave: DISCUSS / [REF] Story map & slices

**Backbone:** Scan the overview → read a widget's status → compare it to last period → drill into its items → investigate a metric over time.

| # | Slice | Group | Stories | Est. | Learning hypothesis |
|---|-------|-------|---------|------|---------------------|
| 01 | Throughput/Arrivals View Data + aging label | A (chrome) | US-01, US-02 | ~0.5d | Disproves that the missing chrome is pure registration if wiring `buildViewData` needs a new item source |
| 02 | Blocked trend zero-baseline | B (trend fix) | US-03 | ~0.5d | Disproves that "trend doesn't work" is the `noBaseline` path if the trend stays blank after the change — then the history is not loading at all |
| 03 | Work Item Age as of selected date | C (data) | US-04 | ~1d | Disproves that as-of-date age is derivable from existing dates if `WasItemProgressOnDay` cannot reconstruct the population without a persisted snapshot |
| 04 | WIA Percentiles RAG + previous-period trend | C (data) | US-05 | ~0.5–1d | Disproves that the SLE is the right benchmark for *in-progress* age if the resulting RAG sits red permanently for healthy teams |
| 05 | Flow Efficiency RAG | C (data) | US-06 | ~0.5–1d | Disproves that lifting the fetch is mechanical if `BaseMetricsView`'s data layer cannot absorb the call without a render-loop or an extra round trip |

**Execution order: 01 → 02 → 03 → 04 → 05.** Rationale: 01 and 02 are same-day wins that put something dogfoodable on the dashboard immediately and cost nothing if wrong. 03 is pulled ahead of 04 and 05 despite being the largest because it carries the most uncertainty *and* 04 hard-depends on it (D5) — failing it late would strand 04. 05 is last only because it is independent of everything else and can absorb schedule slack.

**Carpaccio taste tests**
- *4+ new components?* No — zero new components across all five slices; every change registers into existing chrome.
- *Every slice depends on a new abstraction?* No. Only 04 depends on 03's as-of-date helper, and 03 ships that helper end-to-end with its own user-visible outcome.
- *Does any slice disprove a pre-commitment?* Yes — 03 tests "as-of-date age is derivable without new persistence" and 04 tests "SLE is the right benchmark for in-progress age". Both are real, falsifiable bets.
- *Synthetic-only data?* No — every slice is acceptance-tested against seeded demo data; 03 additionally needs demo items closed before today (flagged above).
- *Two slices identical except for scale?* No.
- **Documented deviation:** the user selected a 3-slice shape (D12). Group C at ~2.5 days fails the ≤1-day gate as one unit, so it is split into 03/04/05. The three conceptual groups are preserved.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| Overview widget chrome parity | 100% of Flow Overview widgets expose a RAG status, and every widget backed by an item set exposes View Data | Count widget keys registered in `buildWidgetFooters` / `buildViewData` vs `getWidgetsForCategory("flow-overview")` — assert as a test, not a manual audit |
| Work Item Age historical accuracy | 0 divergence between the WIA percentile card and the as-of-date reference computation for any selected range | Backend test comparing the percentile population/ages against `GenerateTotalWorkItemAgeByDay` at the same date |
| Blocked trend availability | Trend renders a direction on 100% of instances with ≥1 blocked count, including day-one instances with no snapshot history | Vitest on `computeBlockedTrend` covering the empty-history and both-zero paths |
| Live-view regression | 0 changed values on any Work-Item-Age surface when `endDate` = today | Regression assertion, CI4 |
| Write-back stability | 0 change in emitted write-back age values | Backend regression test, CI5 |

## Wave: DISCUSS / [REF] DoR validation (9/9)

1. **Business value articulated** — PASS. Six named gaps degrade the dashboard's scan-in-one-glance promise; one is a correctness bug on historical ranges.
2. **Job traceability** — PASS. Every story carries a `job_id`; three new jobs added to `docs/product/jobs.yaml`, one existing job reused.
3. **Acceptance criteria testable** — PASS. Every AC names an observable surface or a computed value; none reference internal state alone.
4. **Dependencies identified** — PASS. Only 04 → 03. Documented in the slice table and slice briefs.
5. **Scope bounded** — PASS. Explicit out-of-scope list; six gaps, no adjacent-widget drift.
6. **Sized** — PASS. Five slices, each ≤1 day, with the group-C split documented.
7. **Cross-cutting impact assessed** — PASS. Table above; every row answered, including five explicit N/A-because entries.
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
| **D16** | `WorkItemDto` gains an optional `DateTime? asOf = null` constructor parameter; `WorkItemAge` becomes `asOf.HasValue ? workItem.AgeOnDay(asOf.Value) : workItem.WorkItemAge`. Only the `/wip` endpoint (`TeamMetricsController.cs:117-130`) passes it — the other five construction sites are unchanged by omission. | The aging chart consumes `/wip?asOfDate=`, which *already* carries the date; the DTO simply discards it. Defaulting to the existing property keeps all other callers (FeaturesController, blocked/stale item lists) on today-anchored age with no edit, bounding the blast radius to one call site. |
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
