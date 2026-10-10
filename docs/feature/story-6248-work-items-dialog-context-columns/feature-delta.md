# Feature Delta — story-6248-work-items-dialog-context-columns

> ADO User Story #6248 "Additional Column(s) in Work Items Dialog based on Context". Active, no parent Epic,
> tagged `Release Notes`. Steve (community) clicked a bubble on the Estimation vs. Cycle Time chart and got a
> table without the estimate: "this datatable doesn't really provide enough detail to be more informative than
> the chart or help with a drill-down". The maintainer's assessment: rework the Work Items dialog so it always
> brings the columns that matter where it was opened; estimation is one case, others exist too.

Lean DISCUSS pass (2026-10-10). **The maintainer explicitly skipped DISCOVER and DIVERGE** (answered "Skip
DISCOVER+DIVERGE → DISCUSS" when asked where to start). Decisions D1–D5 were taken by the maintainer on
2026-10-10; their answers are quoted in the Verdict column. Grounded in a code read of `WorkItemsDialog.tsx`, its
16 call sites, `DataGridBase.tsx` / `DataGridToolbar.tsx`, `models/WorkItem.ts`, `models/Feature.ts`,
`BaseMetricsView.buildViewData`, `EstimationVsCycleTimeChart.tsx`, `WorkItemDto.cs`, `FeatureDto.cs`,
`RunChartDataDto.cs` and `BaseMetricsService.BuildEstimationVsCycleTimeResponse`.

---

## Wave: DISCUSS / [REF] Persona IDs

- **flow-coach**: clicks a point on a chart in a retro or standup and asks "which items are these, and why are
  they here?"
- **delivery-lead-rte** (secondary): opens a Feature's child items, or the items behind a Portfolio chart, to
  explain a number in a review.

## Wave: DISCUSS / [REF] JTBD One-Liners

New job **`job-flow-coach-see-why-each-item-sits-behind-the-point`** (added to `docs/product/jobs.yaml`):
*When I click a point or bar on a chart to see the items behind it, I want the list to show the values that
put each item there (its estimate, its dates, its age), so I can explain or question the point without going
back to the chart or the work tracking tool.*

It extends `job-flow-coach-drill-into-throughput-and-arrivals` and
`job-flow-coach-drill-into-blocked-trend-point`. Those made the dialog reachable from almost every chart; this
makes what it shows worth reaching.

## Wave: DISCUSS / [REF] Code reality (what the story walks into)

- **One dialog, 16 call sites.** Ten charts open it on click, `WidgetShell` opens it from every widget's
  *View Data*, `BaseMetricsView` from a Cumulative State Time bar, and the Delivery section, Delivery timeline and
  both feature lists from a Feature's progress cell or timeline bar.
- **One context column, reused for four meanings.** Fixed columns are ID, Name, Type, State, and *Owned by* when
  rows are Features. Each context adds at most one `highlightColumn` (field `additionalColumn`). It holds Cycle
  Time, Age, Size or Days Contributed depending on the caller, and is the default sort and the SLE colouring.
  Optional descriptors add Age band, SLE risk, Time in State (aging chart, WIP and stale widgets) and Warnings
  (Delivery timeline).
- **The estimate never reaches a row.** `IWorkItem` has no estimate. The value lives in
  `WorkItemBase.AdditionalFieldValues[owner.EstimationAdditionalFieldDefinitionId]` and is normalised by
  `EstimateNormalizer` only inside `BuildEstimationVsCycleTimeResponse`, which hands the frontend
  `{workItemIds[], estimationDisplayValue}` per chart point. `WorkItemDto` / `FeatureDto` don't carry it.
- **Rows already carry more than the dialog shows:** started/closed dates, cycle time, age, parent reference,
  blocked-since, time in state, named cycle times; Features add size, owning team and forecasts.
- **One layout for every context.** `DataGridBase` persists visibility, order and widths under
  `lighthouse:datagrid:{storageKey}:state`, and every dialog uses `work-items-dialog`. Hiding a column in one
  chart's dialog hides it in all of them, Team and Portfolio, and `additionalColumn`'s width carries across its
  four meanings.
- **No hidden-by-default columns.** The visibility model starts empty; `useColumnVisibility(initialHiddenColumns)`
  in `DataGrid/hooks.ts` exists but is unused. Users hide columns through MUI's column menu (*Manage columns*).
- **Feature Size *View Data* highlights Age/Cycle Time, not Size**, though its chart is about size.
- **Cumulative State Time rows are synthetic**: built on the frontend with ages 0 and dates at the epoch.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **A full column catalogue; the context picks the defaults.** The dialog knows every column a row can have. Each opening context names which are visible by default and which one sorts; the rest are hidden but reachable through *Manage columns*. | Maintainer, 2026-10-10: "Full catalogue, context picks defaults" |
| D2 | **Catalogue = what rows already carry, plus the Estimate from the backend.** Started, Closed, Cycle Time, Age, Parent, Blocked since, Time in State, named cycle times; for Features also Size, Owned by and the forecast. The normalised Estimate is added to the work-item payload, so it is offered in every context where estimation is configured for the Team or Portfolio. Tags are not added. | Maintainer, 2026-10-10: "Frontend fields + Estimate from backend" |
| D3 | **Layout persists per context.** Each context remembers its own visibility, order and widths; a change in one dialog never leaks into another. | Maintainer, 2026-10-10: "Per context" |
| D4 | **The per-context defaults table below is locked**, and it is declared as configuration in one place, so changing a context's defaults is a one-line edit. The maintainer expects their manual review to find rows to adjust. | Maintainer, 2026-10-10: "yes lock … the config should allow to simply adjust this in future" |
| D5 | **Clients (CLI + MCP): N/A for this story.** The Estimate is an additive field the clients ignore. | Maintainer, 2026-10-10 |
| D6 | **No Estimate column where estimation isn't configured.** It is not offered, not offered-and-empty. A configured estimate that is missing on an item shows an empty cell. | Locked in DISCUSS, from D2 |
| D7 | **Terminology.** Column headers for configurable terms (Cycle Time, Work Item Age, Blocked, Feature, Team) use the instance's terms, as the dialog's other copy already does. "Estimate" is not a configurable term; the column header carries the estimation unit when one is set (`Estimate (Story Points)`), as the chart's axis does. | Locked |

### Per-context defaults (D4)

ID, Name, Type and State are always shown. `*` marks the sort column (descending, as today).

| Context (what was clicked) | Default columns besides ID / Name / Type / State |
|---|---|
| Throughput bar; Cycle Time scatter dot; Cycle Time percentiles, Throughput and Cycle Time PBC *View Data* | Closed, Cycle Time* (SLE colouring where passed today) |
| **Estimation vs. Cycle Time** bubble / *View Data* | Estimate, Cycle Time*, Closed |
| Arrivals bar / *View Data* | Started, Age / Cycle Time* |
| WIP over time; WIP / in-progress widgets; Total Work Item Age (run chart, PBC) | Started, Age*, plus Time in State / SLE risk where shown today |
| Work Item Aging chart dot | Age*, Age band, SLE risk, Time in State (unchanged) |
| Blocked over time; Blocked overview | Age*, Blocked since |
| Stale overview | Age*, Time in State |
| Work Distribution slice | Parent, Age / Cycle Time* |
| Feature Size dot / PBC (rows are Features) | Size*, Cycle Time / Age, Closed (fixes today's Age highlight) |
| A Feature's child items (Delivery section, Team / Portfolio feature lists) | Started, Closed, Age / Cycle Time*, Estimate |
| Delivery timeline bar | Warnings (unchanged) |
| Cumulative State Time bar | Days Contributed* only; no catalogue (synthetic rows) |

"Age / Cycle Time" is today's combined value: cycle time for a closed item, age for an open one.

## Wave: DISCUSS / [REF] User Stories

### US-01: The Estimation dialog shows each item's estimate

As a **flow coach** looking at Estimation vs. Cycle Time, I want the items behind a bubble listed with their
estimate, cycle time and closed date, so I can say which items broke the pattern without opening the tracker.

`job_id: job-flow-coach-see-why-each-item-sits-behind-the-point`

#### Elevator Pitch
Before: click a bubble on a Team's Estimation vs. Cycle Time chart → a table of ID, Name, Type, State and Cycle Time; the estimate that put the items there is only in the dialog title, and *View Data* across all bubbles shows no estimate at all.
After: click a bubble (or *View Data*) → the table shows Estimate, Cycle Time and Closed for every row, and *Manage columns* offers Started, Age, Parent and the rest.
Decision enabled: the coach picks out the 3-point items that took 20 days and asks about them, instead of only seeing that some did.

#### Acceptance Criteria
- **AC-1.1** From a bubble or *View Data* on Estimation vs. Cycle Time, Team or Portfolio, the dialog shows Estimate, Cycle Time and Closed by default, sorted by Cycle Time descending (D4).
- **AC-1.2** Each row's Estimate is that item's normalised display value, the same value the chart plots it at (non-numeric estimates show their category name). The header carries the unit when one is set (D7).
- **AC-1.3** *Manage columns* lists every catalogue column the rows can fill (D1, D2); turning one on shows it, and none of them is visible until turned on.
- **AC-1.4** A layout change (hide, show, reorder, resize) in this context is remembered for this context on the next open, and no other context's dialog changes (D3). *Reset layout* returns this context to its defaults.
- **AC-1.5** Where the Team or Portfolio has no estimation field configured, Estimate is not offered in any context (D6).
- **AC-1.6** Every other context looks exactly as today until it adopts its defaults (slices 02 and 03).

### US-02: Every chart's dialog brings the columns that explain its point

As a **flow coach**, I want each chart's and widget's dialog to open with the columns that explain why its items
are there, so the table tells me more than the chart did.

`job_id: job-flow-coach-see-why-each-item-sits-behind-the-point`

#### Elevator Pitch
Before: click a bar on Blocked over time → ID, Name, Type, State and nothing about how long each item has been blocked; click a Feature Size dot → the list highlights age, not size.
After: click a bar on Blocked over time → Age and Blocked since per row; click a Feature Size dot → Size, Cycle Time / Age and Closed, sorted by Size.
Decision enabled: the coach sees which blocked item has waited longest and raises it first.

#### Acceptance Criteria
- **AC-2.1** Each chart and *View Data* context in the defaults table (all rows except a Feature's child items, the Delivery timeline and Cumulative State Time) opens with exactly its default columns and sort.
- **AC-2.2** Feature Size, from a dot or *View Data*, sorts by Size.
- **AC-2.3** Existing judgements keep their look: SLE colouring on Cycle Time where `sle` is passed today, Age band, SLE risk and Time in State as today.
- **AC-2.4** Cumulative State Time keeps Days Contributed only and offers no catalogue columns.
- **AC-2.5** AC-1.3 to AC-1.5 hold in every context.

### US-03: A Feature's child items show their progress at a glance

As a **delivery lead**, I want a Feature's child items listed with their dates, age and estimate, so I can see
what is finished, what is moving and what is big without leaving the Delivery or Feature list.

`job_id: job-flow-coach-see-why-each-item-sits-behind-the-point`

#### Elevator Pitch
Before: on a Portfolio's Deliveries, click a Feature's progress → ID, Name, Type, State; nothing says when anything started or finished.
After: click the progress → Started, Closed, Age / Cycle Time and Estimate per child item.
Decision enabled: the lead sees the one large child item still not started and raises it before the date slips.

#### Acceptance Criteria
- **AC-3.1** The child-items dialog from the Delivery section and from the Team and Portfolio feature lists opens with Started, Closed, Age / Cycle Time* and Estimate (D4, D6).
- **AC-3.2** The Delivery timeline keeps Warnings as its only extra column; the catalogue is offered there too.
- **AC-3.3** AC-1.3 to AC-1.5 hold in these contexts.

## Wave: DISCUSS / [REF] Out of Scope

- Tags, assignee, arbitrary additional fields as columns (D2). A user-configurable column set per instance.
- Showing the Estimate in the CLI or MCP (D5).
- Any change to which items a dialog lists, or to the charts themselves.
- Other grids (feature lists, Refinement, Delivery grid).
- Migrating the old shared `work-items-dialog` layout into the per-context ones; every context starts from its defaults.

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC**: **N/A, because** the Estimate comes from the same Team or Portfolio the user can already read; no
  endpoint, permission or `/authorization/my-summary` read is added.
- **Lighthouse-Clients (CLI + MCP)**: **N/A, because** the new `estimate` field is additive and the clients ignore
  unknown fields (D5). The clients' contract tests may need the field tolerated; DESIGN checks.
- **Website**: **N/A, because** the website shows charts, not the drill-down table. Re-check at finalize.
- **Premium**: **N/A, because** the dialog is free; CSV export stays premium and exports the visible columns, as today.
- **Docs**: owed at finalize. `docs/metrics/widgets.md:55` describes *View Data*; it gains a line on context
  columns and *Manage columns*. `docs/metrics/flow-metrics.md` (Estimation vs. Cycle Time) mentions the Estimate
  column.
- **Screenshots**: check at finalize whether any `@screenshot` captures an open dialog; regenerate if so.
- **E2E**: low risk. Specs that read dialog cells by column must not assume today's column order; DISTILL checks
  the dialog POM. Walking skeleton: demo data, Estimation chart bubble, Estimate column present.
- **Usage data**: for **DEVOPS** to answer. Candidate: a name-only event when a user shows a catalogue column
  that was hidden by default, as evidence the catalogue is used (and as the signal for the D4 table review).

## Wave: DISCUSS / [REF] WS Strategy

Strategy **B (extend existing)**, no skeleton. Three slices, each about a day and releasable on its own:

1. **slice-01-estimation-dialog**: US-01. The Estimate in the payload, the catalogue, hidden-by-default columns,
   the per-context layout and the one-place defaults config; only the Estimation context adopts it.
2. **slice-02-chart-contexts**: US-02. Every chart and *View Data* context adopts its defaults.
3. **slice-03-feature-children**: US-03. The child-items contexts and the Delivery timeline.

## Wave: DISCUSS / [REF] Driving Ports

- UI: the Work Items dialog, opened from chart clicks, *View Data*, a Feature's progress cell and the Delivery timeline.
- HTTP: the work-item and Feature payloads (`WorkItemDto`, `FeatureDto`) gain the normalised estimate.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

Three stories, three slices. One frontend component plus its call sites, one additive backend DTO field. Each slice
≤ 1 day and releasable alone. The largest unknown is how the backend gets the owner's estimation config to every
place a `WorkItemDto` is built (a question for DESIGN). No oversized signals.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-6248-estimate-in-the-drill-down` | 100 % of Estimation vs. Cycle Time dialogs show an Estimate per row when estimation is configured | Component test + E2E walking skeleton on demo data |
| `OUT-6248-no-layout-leak` | 0 contexts change when another context's layout changes | Component test across two contexts |
| `OUT-6248-catalogue-used` | Catalogue columns turned on in ≥ 10 % of opted-in instances within 60 days of release | Usage-data event, if DEVOPS adopts one; owner: whoever runs `/release` |
| `OUT-6248-no-repeat-request` | No new "column X missing in the dialog" request in 60 days for a column already in the catalogue | ADO / community channels |

## Wave: DISCUSS / [REF] Definition of Done

1. Every context in the defaults table opens with its locked defaults and sort.
2. The Estimate is in the work-item and Feature payloads, normalised exactly as the chart normalises it.
3. Catalogue columns are hidden by default and reachable through *Manage columns*; Estimate is offered only where configured.
4. Layout persists per context; *Reset layout* restores that context's defaults.
5. The defaults live in one declared map; changing a context's defaults touches one entry.
6. `pnpm test`, `pnpm build` (Biome clean), `dotnet build` (0 warnings), filtered `dotnet test` and the E2E walking skeleton are green.
7. No new SonarCloud issues. StrykerJS and Stryker.NET ≥ 80 % on changed files.
8. Docs updated at finalize (`widgets.md`, `flow-metrics.md`).
9. A release-notes line is drafted on #6248 (`Release Notes` tag, already set), crediting Steve.

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | Problem statement clear, in domain language | ✅ | Header quote; job `job-flow-coach-see-why-each-item-sits-behind-the-point` (new) |
| 2 | User / persona identified | ✅ | flow-coach, delivery-lead-rte |
| 3 | At least 3 domain examples with real data | ✅ | (a) Estimation vs. Cycle Time bubble, demo data (US-01); (b) Blocked over time bar (US-02); (c) a Feature's child items on a Portfolio's Deliveries (US-03); (d) the dev instance `:5169` with an estimation field configured (slice-01 dogfood) |
| 4 | UAT scenarios | ✅ | ACs are observable outcomes; DISTILL writes them as scenarios |
| 5 | AC derived from UAT | ✅ | AC-1.1–1.6, AC-2.1–2.5, AC-3.1–3.3, each traced to D1–D7 |
| 6 | Story right-sized | ✅ | Three slices, each ≤ 1 day and releasable alone |
| 7 | Technical notes | ✅ | Code reality; Out of Scope; one additive backend field |
| 8 | Dependencies | ✅ | None outside the story. 02 and 03 build on 01's catalogue and config |
| 9 | Outcome KPIs | ✅ | Four KPIs with targets |

Cross-cutting checklist (RBAC, Clients, Website, Premium, Docs, Screenshots, E2E, Usage data) answered above.

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions:** D1 full catalogue, context picks defaults; D2 catalogue = row fields + Estimate from
  the backend, no tags; D3 layout per context; D4 locked defaults table, declared in one place; D5 clients N/A;
  D6 no Estimate column without estimation configured.
- **Feature type:** user-facing; frontend plus one additive backend field.
- **Constraints:** reuse `DataGridBase` and its *Manage columns*; keep the existing judgement cells; the
  estimate is normalised by the same code as the chart.
- **Upstream changes:** none. DISCOVER and DIVERGE were skipped by the maintainer.

## Wave: DISCUSS / [REF] Open questions for DESIGN

- How does `WorkItemDto` / `FeatureDto` get the owner's estimation config to normalise the value? Every place
  that builds one would need it; is a per-response enrichment (owner known at the controller) cheaper than
  threading it through the DTO constructors? Does `EstimateNormalizer` normalise one value without its batch?
- Where does the defaults config live, and what shape does a context key take (one per row of the table, or per
  call site)? It must be the single edit point (D4).
- Hidden-by-default in `DataGridBase`: revive `useColumnVisibility(initialHiddenColumns)` or seed the visibility
  model from a new prop? How does it combine with a persisted per-context state and *Reset layout*?
- The per-context storage key: should the old shared `work-items-dialog` key be removed from localStorage?
- Synthetic Cumulative State Time rows: a context flag "no catalogue", or rows typed so the catalogue cannot apply?
- The Feature zod schema drops `blockedSince`, `currentStateEnteredAt` and `namedCycleTimes`; do Feature rows need them for the catalogue?

## Wave: DISCUSS / [REF] Open checks for DISTILL

- UI sketch walk-through with the maintainer at the start of DISTILL: the Estimation dialog, the *Manage
  columns* list, and the Estimate header with and without a unit.
- The dialog POM reads cells by column header, not position.
