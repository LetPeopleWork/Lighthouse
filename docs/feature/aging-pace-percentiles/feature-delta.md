# Feature: aging-pace-percentiles

Epic 4144 (More Detailed State Info) — second MVP-bundle feature, covers slice F from the Epic catalog.

ADO Epic: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144
ADO Story (already created): https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5075

## Wave: DISCUSS / [REF] Pre-DISCUSS code reality check

Before drafting this delta, the existing Work Item Aging chart code was inspected
(`Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx`
+ `Lighthouse.Backend/.../TeamMetricsController.cs:cycleTimePercentiles`
+ `TeamMetricsService.cs:GetCycleTimePercentilesForTeam`).

**Finding**: the chart **already overlays 50/70/85/95 percentile reference lines** today.
The source is `GetCycleTimePercentilesForTeam(team, startDate, endDate)` — percentiles
of the **end-to-end cycle time** of completed items in the window, drawn as dashed
horizontal reference lines across the entire chart.

This means the carry-over README's framing — *"ActionableAgile shows pace percentiles
overlaid on the aging chart; Lighthouse currently does not"* — is **factually inaccurate
for the team-scope chart**. The bands exist. The competitive-parity gap is finer-grained:

- Today's bands are **one horizontal line per percentile, drawn across all states**, derived
  from completed items' total cycle time.
- ActionableAgile's modern aging chart shows **per-state percentile lines** (each workflow
  state has its own band heights), derived from the historical distribution of
  *age-at-state-exit per state* — not just end-to-end cycle time.

The actionable insight for a flow coach is different: *"this item is at the 85th percentile
**for this state**"* is a per-state pace call ("the work this item is doing right now is
slower than 85% of similar work historically did"), whereas *"this item is at the 85th
percentile of cycle time"* is an end-to-end pace call against the existing CT distribution.
Both are useful; the per-state pace is what Lighthouse is missing and what the
`WorkItemStateTransition` data foundation (built in `time-in-state-and-staleness`) unlocks.

This finding reshapes the feature: it is NOT "add percentile bands to the chart". It IS
"add **per-state** age-at-state-exit percentile bands to the chart, alongside the existing
end-to-end cycle-time bands". The existing bands stay (no regression to current users).

This finding is locked into D1 below and surfaced as a contradiction to be confirmed with
the user at next review touchpoint.

## Wave: DISCUSS / [REF] Persona ID

**Primary**: `flow-coach` — team lead, agile coach, scrum master, or RTE running a team's
flow review or standup. Same persona as `time-in-state-and-staleness`, different decision
shape: per-item triage (sibling) vs chart-glance pace recognition (this feature).

**Secondary**: `delivery-forecaster` / Product Manager — uses the same chart to reason about
"is what's in flight pacing similarly to what we historically finished, or is it slower?"
before committing to a forecast date. Persona file exists at
`docs/product/personas/delivery-forecaster.yaml`.

## Wave: DISCUSS / [REF] JTBD one-liner

When I open my team's Work Item Aging chart, I want to see at a glance which in-flight items
are pacing slower than most historically-finished items in the **same workflow state**, so I
can spot pace outliers without comparing per-state ages and historical distributions in my
head.

Job-id: `job-flow-coach-spot-pace-outliers` (NEW — added to `docs/product/jobs.yaml` in this
DISCUSS run). Differentiated from sibling's `job-flow-coach-spot-stuck-items` (per-item time
the badge tells me "X days in state Y"; this job tells me "X days is the 90th percentile for
state Y — most items leave Y by day 7").

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | The "ActionableAgile parity" gap is **per-state** percentile bands, not the presence of bands. Existing end-to-end cycle-time bands stay; new per-state age-at-state-exit bands are added alongside, distinguished visually (see D6). | Locked — confirmed via code inspection of `WorkItemAgingChart.tsx` and `TeamMetricsController.cs:cycleTimePercentiles` (see Pre-DISCUSS Code Reality Check above). |
| D2 | Default percentiles: **50, 70, 85, 95**. Matches the existing Lighthouse percentile choice (`TeamMetricsService.cs:314-317`) and the ActionableAgile convention. No new defaults set means there is one less thing for the user to learn. | Locked. |
| D3 | Distribution window for the historical age-at-state-exit calculation: **matches the existing throughput / cycle-time history window already configured per team** (i.e. reuse the same `startDate`/`endDate` parameters the existing `cycleTimePercentiles` endpoint takes — `History` setting). One window for all pace measures keeps the mental model coherent. | Locked. |
| D4 | Per-team / per-portfolio percentile **configuration is deferred** to a post-MVP follow-up (provisionally `aging-pace-percentile-config` — already represented in ADO as Story #5076, which the user explicitly deferred from this DISCUSS). MVP ships fixed defaults. Confirmed by user instructions (`#5076 ... was deferred from this DISCUSS`). | Locked (deferred). |
| D5 | Edge cases: **empty distribution** (no completed items in window for a state) → that state's per-state bands are omitted (the existing end-to-end cycle-time bands continue to show). **Small samples** (n<10 for a state) → bands are computed but the legend chip for the per-state series shows a "low-sample" tooltip ("based on N completed items"). **Bimodal distributions** → out of scope; percentile math is still mathematically defined and the user can read it. | Locked. |
| D6 | Visual treatment: per-state bands render as **short horizontal segments anchored to each state column** (not lines across the whole chart), using the same dashed style as today's full-width bands but **only spanning the width of the state column**. The existing full-width end-to-end CT lines remain unchanged. Legend distinguishes "Cycle Time %iles (overall)" vs "Age-in-State %iles (per state)" via two separate chip groups in `PercentileLegend`. | Locked. |
| D7 | Filter composition: per-state bands respect the same filters that the existing chart respects — work-item type chips, team/portfolio scope, date range (which IS the history window per D3). No new filter primitives. | Locked. |
| D8 | Scope: **team-level and portfolio-level chart simultaneously** (the chart component `WorkItemAgingChart` is rendered in both `BaseMetricsView` instances via team and portfolio routes — `BaseMetricsView.tsx:789`). Shipping per-state bands for only one scope would create a confusing parity hole. | Locked. |
| D9 | **Data foundation reuse, not rebuild**: this feature consumes `WorkItemStateTransition` rows shipped by `time-in-state-and-staleness` slice 01 + 02. From transitions, derive `ageAtStateExit[state] = transition.timestamp - currentStateEnteredAt(state)` for completed items in the window. No new sync-side capture; no new persistence beyond a per-team materialised cache if profiling demands it (deferred to DESIGN). | Locked. |
| D10 | **WS strategy: Type A (additive)**. No contract change to existing endpoints. A new endpoint `GET /api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` (and the matching portfolio route) returns `[{ state: string, percentiles: PercentileValue[] }]`. The chart component takes a new optional prop `perStatePercentileValues`; if absent or empty, behaviour is identical to today. | Locked. |
| D11 | **Sibling-data dependency on `WorkItemStateTransition`**: this DISCUSS does NOT request any schema addition to the transition table. The four fields shipped in sibling slice 01 (`workItemId`, `fromState`, `toState`, `transitionedAt`) are sufficient. The "done date" required for "completed in window" is derivable from the existing Started/Closed dates already on the `WorkItem` entity (unchanged). | Locked — explicit no-impact on sibling DESIGN. |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Overlay per-state age-in-state percentile bands on the Work Item Aging chart (team scope)

**Story**: As a `flow-coach`, I want per-state percentile bands (50th / 70th / 85th / 95th of
historical age-at-state-exit, per workflow state) overlaid on my team's Work Item Aging
chart, so I can see at a glance which in-flight items are pacing slower than most
historically-finished items did *while in that same state*.

**Job-id**: `job-flow-coach-spot-pace-outliers`

### Elevator Pitch
Before: I can only see the chart's existing horizontal end-to-end cycle-time lines — they tell me "this item is past 85th percentile of overall cycle time" but not "the work this item is doing IN THIS STATE is slower than 85% of similar work". For that I would mentally compare each item's days-in-current-state against my memory of how long items historically stayed in that state.
After: open `/teams/{teamId}` → scroll to the Work Item Aging chart → see short dashed horizontal segments above each state column at 50/70/85/95 of historical age-at-state-exit for that state, alongside today's full-width end-to-end cycle-time bands. A dot above the per-state 85% segment for state `Review` is visibly past the band.
Decision enabled: which in-flight items are pacing slower than the team historically does *for the work they're currently doing* — names the conversation more precisely than the existing end-to-end view ("this PR is past the 85th age-in-Review percentile — it's not the whole cycle that's slow, it's specifically the review step").

**AC**:
- Given a team with completed items in the configured history window AND `WorkItemStateTransition` data present, when I open the team metrics view containing the Work Item Aging chart, then the chart renders per-state percentile bands as short dashed horizontal segments anchored above each state column, AT the heights of the 50th / 70th / 85th / 95th percentile of age-at-state-exit for that state, computed over the same history window the existing `cycleTimePercentiles` endpoint uses.
- Given today's full-width end-to-end cycle-time percentile lines, when the new per-state bands render, then the existing full-width lines continue to render unchanged (no regression). Legend distinguishes the two series.
- Given a state with fewer than 10 completed items in the window, when the chart renders, then per-state bands for that state still render and the legend chip for that series tooltip reads `Based on N completed items in <state> (low sample)` where N is the actual count.
- Given a state with zero completed items in the window, when the chart renders, then no per-state bands render for that state; the existing full-width cycle-time bands still render so the chart is never empty of guidance.
- Given a portfolio (not a team), the same behaviour holds for the portfolio's Work Item Aging chart, derived from completed work items in the portfolio's scope (D8).
- Existing chart filters (work-item type chips, date range) continue to work; toggling a type off recomputes both the in-flight dots AND the per-state bands' historical sample (no stale band heights).

### US-02 — Legend control to toggle per-state bands independently of existing cycle-time bands

**Story**: As a `flow-coach`, I want to toggle the new per-state bands on/off independently
of the existing full-width cycle-time bands, so I can choose which pace story I am telling
in this view without losing the other.

**Job-id**: `job-flow-coach-spot-pace-outliers`

### Elevator Pitch
Before: even if the new bands ship, on a busy chart I can be looking at 8 dashed lines at once (4 full-width CT lines + 4 short segments per state, multiplied across N states). I want to thin the visual to whichever story I am telling at this moment.
After: in the `PercentileLegend` strip above the chart, two chip groups — `Cycle Time %iles (overall): 50 70 85 95` (existing) and `Age-in-State %iles (per state): 50 70 85 95` (new) — each percentile chip toggles independently. Switching off a chip hides only that series; switching off the whole `Age-in-State` group hides per-state bands entirely and the chart falls back to today's exact look.
Decision enabled: which pace lens to read the chart through this minute (end-to-end CT for forecast conversations; per-state age for "what is this team's specific bottleneck?" conversations).

**AC**:
- Given the legend renders, when I click an `Age-in-State` percentile chip, then only that percentile's per-state segments hide/show across all states; existing CT bands and other percentile chips are unaffected.
- Given the legend renders, when I click a `Cycle Time %iles` chip, then today's behaviour is unchanged (full-width line for that percentile hides/shows).
- Given both groups exist, the chip group labels are visually distinct (different sub-header text within the legend strip) so a user does not confuse the two series.
- Toggle state persists for the current session but is not persisted server-side (out of scope for MVP — matches existing chip toggle behaviour).

### US-03 — In-flight item tooltip surfaces "above 85th age-in-state" annotation

**Story**: As a `flow-coach`, when I hover a chart dot for an in-flight item, I want the
tooltip to tell me whether the item is past the 85th percentile age-in-state for its
current state, so I can identify the specific pace outliers without manually comparing
the dot's Y position to each state column's bands.

**Job-id**: `job-flow-coach-spot-pace-outliers`

### Elevator Pitch
Before: even with the bands visible, identifying which dots cross which per-state band requires eye-balling Y position against the band heights — easy to miss on a crowded chart.
After: hover any in-flight item dot → existing tooltip text plus one new line `Pace: above 85th percentile for <stateName>` (or `at 50–70th`, `at 70–85th`, `at 85–95th`, `above 95th`, `below 50th`, `no historical data`). The annotation answers the chart's question in words.
Decision enabled: which dots warrant a "what is keeping this item in this state?" conversation, without staring at the chart geometry.

**AC**:
- Given an in-flight item in state `Review` with `daysInState = 12` and the team's historical 85th age-in-Review percentile is `10`, when I hover that dot, then the tooltip contains a line `Pace: above 85th percentile for Review`.
- Given an in-flight item whose current state has zero completed-item historical data in the window, when I hover that dot, then the tooltip line reads `Pace: no historical data for <stateName>`.
- The annotation uses the percentile choices from D2 (50/70/85/95) — buckets are `below 50`, `at 50-70`, `at 70-85`, `at 85-95`, `above 95`.
- The annotation is computed client-side from the per-state percentile values and the item's current `daysInState` (no extra round-trip).

## Wave: DISCUSS / [REF] Definition of Done

1. All 3 stories pass their ACs via integration tests (NUnit + EF InMemory + WebApplicationFactory for the new endpoint; Vitest + React Testing Library for chart rendering and tooltip).
2. Per-state percentile math verified against a fixture: a known set of completed items with known per-state durations, asserted exact percentile values for each state.
3. No regression in existing `WorkItemAgingChart` tests (`WorkItemAgingChart.test.tsx`) — existing full-width CT lines render exactly as before when per-state bands are absent.
4. Per-state band rendering tolerates: empty distribution per state, low-sample per state, single-completed-item per state, all gracefully (per AC).
5. Portfolio-scope parity: both team and portfolio Work Item Aging charts show per-state bands (D8).
6. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
7. SonarCloud quality gate passes on PR.
8. Mutation testing (Stryker.NET for Backend; Stryker for Frontend): ≥80% kill rate for new code.
9. Docs updated: screenshot of the chart with both band series visible, with a callout for the new per-state bands.

## Wave: DISCUSS / [REF] Out of scope

- **Configurable percentiles per team/portfolio** — deferred (ADO Story #5076, locked D4). MVP ships fixed 50/70/85/95.
- **Per-state distribution window override** — out of scope; uses the team/portfolio history window per D3.
- **Bimodal-distribution-aware visual treatment** — out of scope (D5).
- **Per-state historical drill-down chart** — that is feature B2 (`work-item-state-history-view`), post-MVP. This feature only renders aggregate per-state percentile heights, not per-item history.
- **Cumulative time-per-state across timeframe** — that is sibling MVP feature `state-time-cumulative-view`. Different question; different chart.
- **Changes to existing `cycleTimePercentiles` endpoint or `WorkItemStateTransition` schema** — D11 explicitly preserves both. Sibling feature DESIGN is unaffected.
- **Backfill of historical transitions before sibling slice 01 shipped** — sibling locked this as forward-only; this feature inherits the same constraint. Therefore the per-state bands will be empty / low-sample for the first weeks post-release until enough completed items have transitions captured.
- **In-flight item pace annotation in the chart dot ITSELF** (e.g. dot colour-coded by percentile bucket) — only the tooltip is in scope per US-03. Chart-symbol changes would conflict with existing colour semantics (type chips, blocked emphasis).

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract change to existing endpoints. One new
endpoint and one new chart prop. Walking skeleton = US-01 against the team scope only, with
the existing PercentileLegend extended to show the new chip group (US-02). US-03 piggy-backs
on the same data — adds tooltip annotation in the same UI component.

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | **New** | Returns `[{ state: string, sampleSize: int, percentiles: [{ percentile: int, value: double }] }]` for the team's completed-in-window items, derived from `WorkItemStateTransition`. Empty array for states with zero completed items. |
| GET | `/api/portfolios/{portfolioId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | **New** | Same shape as the team route, scoped to the portfolio (D8). |
| GET | `/api/teams/{teamId}/metrics/cycleTimePercentiles` | Authenticated | Existing | **Unchanged** (D11). |

No new top-level routes; no schema additions. UI surfaces: `WorkItemAgingChart` (extended
with `perStatePercentileValues` prop), `PercentileLegend` (extended with second chip group),
`BaseMetricsView` (passes the new metric ctx slot through).

## Wave: DISCUSS / [REF] Pre-requisites

- **HARD**: `time-in-state-and-staleness` slice 01 must be **merged and shipped** so `WorkItemStateTransition` exists and has been accumulating data for completed items. (Sibling DISCUSS is complete; DESIGN/DELIVER not yet started — see "MVP bundle dependency note" below.)
- **HARD**: Existing Work Item Aging chart code path (`Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` + `Lighthouse.Backend/.../TeamMetricsController.cs:cycleTimePercentiles`) — confirmed present in code reality check above.

**MVP bundle dependency note**: this feature cannot enter DELIVER until the sibling
`time-in-state-and-staleness` is at least merged to main (the transitions table must exist
and the connector capture must be running, even if `WorkItemStateTransition` rows have only
been accumulating for a short time). DESIGN of this feature CAN run in parallel with the
sibling's DELIVER. This sequencing is consistent with locked D3 of the sibling
(`Slice ordering across Epic 4144: A+B1+D → F → B2 → B3 → C`).

No DISCOVER or DIVERGE artifacts exist for Epic 4144; this DISCUSS run extends the same
"community-validated via Productboard + Community tags on ADO #4144" reasoning the sibling
used. JTBD differentiation from sibling is the substantive new content of this feature.

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Scope | Measurement |
|---|---|---|---|
| `OUT-aging-pace-bands-rendered` | ≥30% of teams have per-state bands rendering (i.e. ≥1 state has ≥1 completed item in the window) within 6 weeks of sibling slice 01 shipping (giving 6 weeks of transition accumulation) | per_instance | Backend counter on the new endpoint: increment when ≥1 state returns a non-empty band array; sample at week 6 |
| `OUT-aging-pace-legend-toggled` | ≥15% of users who view a team detail page interact with the new `Age-in-State` legend chip at least once within 4 weeks of release | per_instance | Existing page-view telemetry on the team detail route + a new event `aging.pace.legend.toggled` emitted on chip click (instrumentation requirement carries to DEVOPS handoff) |
| `OUT-aging-pace-parity-claim` | Zero customer reports within 8 weeks of release of "Lighthouse aging chart is missing ActionableAgile-style per-state bands" — verifies the parity claim is now defensible | vendor_demo_only + community reports | Issue tracker label `feature-request-aging-pace`; community Slack mentions |

KPIs will be appended to `docs/product/kpi-contracts.yaml` at the DEVOPS handoff.

## Wave: DISCUSS / [REF] Definition of Ready — validation

| # | DoR item | Verdict | Evidence |
|---|---|---|---|
| 1 | Every story traces to a `job_id` | Pass | US-01, US-02, US-03 → `job-flow-coach-spot-pace-outliers` (new entry added to `docs/product/jobs.yaml` in this run). |
| 2 | Persona named & scoped | Pass | `flow-coach` primary (existing persona, reused — different decision shape from sibling, explicitly differentiated in JTBD one-liner); `delivery-forecaster` secondary (existing persona). |
| 3 | Elevator pitch per non-`@infrastructure` story | Pass | Each US-NN has Before/After/Decision triplet referencing real entry points (`/teams/{teamId}` route + `WorkItemAgingChart` interactions) and concrete observable output (dashed segments above state columns, tooltip line text). |
| 4 | AC testable, no ambiguous outcomes | Pass | Quantified percentiles (50/70/85/95); explicit empty-state and low-sample behaviour; explicit no-regression on existing endpoint and chart. |
| 5 | Out-of-scope explicit | Pass | 7 items listed (configurable percentiles, distribution-window override, bimodal handling, per-item drill-down, cumulative view, existing endpoint changes, in-flight dot recolour). |
| 6 | Outcome KPIs measurable with targets | Pass | 3 KPIs, each with numeric target, scope, and measurement method. |
| 7 | Pre-requisites resolved | Pass (with sequencing note) | Sibling DISCUSS complete; existing chart confirmed present in code. The sibling-slice-01-shipped pre-req is sequencing, not a blocker for THIS wave — it gates DELIVER, not DISCUSS or DESIGN. |
| 8 | Slice composition: each slice contains ≥1 user-visible story | Pass | Slice 01 ships US-01 (per-state bands visible — value-bearing). Slice 02 ships US-02 + US-03 (legend toggle + tooltip annotation — both value-bearing). No `@infrastructure`-only slices. |
| 9 | Handoff target identified | Pass | nw-solution-architect (DESIGN, full artifacts); nw-platform-architect (DEVOPS, outcome-kpis only). |

**DoR overall verdict: PASSED.**

## Wave: DISCUSS / [REF] Wave decisions summary

**Primary user need**: enable flow coaches to spot pace outliers at chart-glance — items
whose in-flight age in their current state is past the historical 85th percentile of
age-at-state-exit for that same state, computed from completed-items history. Differentiated
from sibling's per-item time-in-state badge (sibling answers "is THIS item stuck?"; this
feature answers "is this team's collective in-flight work pacing in line with what they
historically finished?").

**Foundation investment**: zero new persistence; reuses `WorkItemStateTransition` (sibling).
One new endpoint per scope (team + portfolio), one chart prop extension. Backwards-compatible
across the board.

**Walking skeleton scope**: slice 01 (US-01) — team scope, ADO connector reusing sibling's
transition data, the new endpoint, the chart extension. Proves the data-foundation→endpoint→
chart path in one slice.

**Feature type**: user-facing (chart UI extension).

**Upstream changes**: **NONE for sibling DESIGN**. D11 explicitly preserves
`WorkItemStateTransition` schema as shipped by sibling slice 01. The done-date for the
"completed in window" filter is derived from existing `WorkItem.StartedDate` / `ClosedDate`
fields, unchanged.

**Downstream coordination**: `state-time-cumulative-view` (next DISCUSS, sibling MVP) will
also consume `WorkItemStateTransition` and may want to share aggregation infrastructure with
this feature's new endpoint. Flag for DESIGN cross-feature alignment, not a DISCUSS blocker.

## Wave: DISCUSS / [REF] Contradiction with carry-over README

Per the `Document Update (Back-Propagation)` contract in `nw-discuss/SKILL.md`, contradictions
between DISCUSS findings and prior artifacts must be flagged in a `Changed Assumptions`
section. The READ artifact predates DISCUSS; this is the first artifact in the feature
lifecycle that can correct it.

### Changed Assumptions

**Source**: `docs/feature/aging-pace-percentiles/README.md` (Strategic framing section).

**Original assumption (verbatim)**:
> "ActionableAgile shows pace percentiles overlaid on the aging chart; Lighthouse currently
> does not. Closing this gap is the explicit value prop"

**New assumption** (locked as D1 above):
The Work Item Aging chart **already overlays** 50/70/85/95 percentile reference lines
(verified by inspecting `WorkItemAgingChart.tsx:349-364` and
`TeamMetricsService.cs:303-318`). Those lines are computed from **end-to-end cycle time**
of completed items and drawn full-width. The competitive-parity gap is **per-state**
age-at-state-exit percentiles, drawn as short segments anchored per state column, alongside
the existing full-width CT lines. The existing bands stay; the new per-state bands are added.

**Rationale**: code inspection contradicts the README's premise. Treating this feature as
"add bands where none exist" would either (a) waste effort re-implementing what is already
there, or (b) ship a degraded duplicate. The reframing preserves the user's stated intent
("close the ActionableAgile gap") while correcting the technical premise. The reframing
ALSO justifies the feature's stated reliance on `WorkItemStateTransition` (which the
existing CT-based bands do NOT need — they use Started/Closed dates only) — making the
"data foundation reuse" claim in the README accurate after correction.

The README itself is NOT modified (per the contract: "Do NOT modify DISCOVER documents
directly"); this Changed Assumptions section IS the modification record.

## Wave: DESIGN / [REF] DDD list

| ID | Decision | Verdict | One-line rationale |
|---|---|---|---|
| DDD-1 | Per-state percentile algorithm: visit-level (not item-level) observations of `ageAtStateExit = exitTransition.TransitionedAt - entryTransition.TransitionedAt` per completed visit, bucketed by state | Locked | Matches the user-facing chart-glance question ("the work this dot is doing IS slower than 85% of historical visits"); see ADR-019 |
| DDD-2 | Item-membership rule: `W.ClosedDate ∈ [startDate, endDate]` — mirrors `cycleTimePercentiles` exactly. EXPLICITLY DIFFERENT from sibling B3's frame-intersection rule | Locked | Keeps band heights comparable to the existing CT bands shown on the same chart; semantic divergence vs B3 is the deliberate distinction; ADR-019 |
| DDD-3 | Percentile function: reuse existing `PercentileCalculator.CalculatePercentile` (nearest-rank with clamp). Defaults 50/70/85/95 per DISCUSS D2 | Locked | Algorithmic parity with `cycleTimePercentiles` — user reads both percentile families on the same chart | ADR-019 |
| DDD-4 | Empty / single / low-sample at the API: emit dto when ≥1 sample, omit state entry when 0; FE handles presentation (per DISCUSS D5) | Locked | No backend threshold — presentational decisions stay in the FE; ADR-019 |
| DDD-5 | Cache via existing `BaseMetricsService.GetFromCacheIfExists` with key `AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}`; inherits existing post-sync invalidation hook | Locked | Zero new cache infrastructure; matches existing `cycleTimePercentiles` cache pattern | ADR-019 |
| DDD-6 | Per-state band rendering: custom SVG `<line>` overlay rendered as a child of the existing `<ChartsContainer>`, anchored to each state column index. NOT `ChartsReferenceLine` (no X-range support); NOT a sibling widget (breaks D6 "alongside" semantics); NOT a chart replacement (discards investment) | Locked | Resolves the slice-01 spike candidate at DESIGN time so DELIVER does not stall; ADR-020 |
| DDD-7 | Chart prop interface: `WorkItemAgingChart` accepts new optional `perStatePercentileValues?: IPerStatePercentileValues[]`. Absent / empty → renders today-identical (guarded by snapshot test) | Locked | Backwards-compatible; matches DISCUSS WS Type A | ADR-020 |
| DDD-8 | Legend chip-group wiring: two independent invocations of `useChartVisibility` (or one invocation with two parameters — implementation shape, software-crafter chooses at GREEN). Two parallel chip groups in `PercentileLegend` with distinct sub-headers | Locked | DISCUSS US-02 requires independent toggles; the existing hook supports both wiring shapes | ADR-020 |
| DDD-9 | Backend computation lives as a `protected` helper inside `BaseMetricsService` (`ComputeAgeInStatePercentiles`); both `TeamMetricsService.GetAgeInStatePercentilesForTeam` and `PortfolioMetricsService.GetAgeInStatePercentilesForPortfolio` delegate to it. Helper takes the result of the scope-specific "completed items in window" query plus workflow state order plus requested percentiles | Locked | Mirrors the existing inheritance pattern for shared metrics work (throughput, total work item age); intra-service helper, NOT an interface | ADR-021 |
| DDD-10 | ADR-018 disposition: UPHELD. No shared `IPerStateAggregationService`. The repository (`IWorkItemStateTransitionRepository`) IS the shared primitive across siblings. Sibling B3 writes its own service-layer method when it DESIGNs | Locked | Concrete query shapes (DDD-2 vs sibling B3 D12) make the semantic divergence visible; consolidation would conflate; ADR-021 |
| DDD-11 | US-03 tooltip annotation: computed client-side using the dot's existing `daysInState` (derived from `currentStateEnteredAt` per sibling 1's `WorkItemDto`) and the per-state percentile values already in chart state. No extra round-trip. Bucket: `below 50` / `at 50–70` / `at 70–85` / `at 85–95` / `above 95` / `no historical data` (when the dot's state has no entry in `perStatePercentileValues`) | Locked | DISCUSS US-03 AC line 4 mandates client-side; no API change | DESIGN, no ADR (DISCUSS-mechanical) |
| DDD-12 | Endpoint route shape: `GET /api/teams/{teamId:int}/metrics/ageInStatePercentiles?startDate&endDate` and `GET /api/portfolios/{portfolioId:int}/metrics/ageInStatePercentiles?startDate&endDate`. Auth: existing `[RbacGuard(TeamRead)]` / `[RbacGuard(PortfolioRead)]`. Validation: `startDate.Date <= endDate.Date` (HTTP 400 otherwise), matching the existing `cycleTimePercentiles` validation | Locked | Mirrors the existing route convention precisely; no new top-level routes | DESIGN, no ADR |

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change Type | Change Summary |
|---|---|---|---|
| `AgeInStatePercentilesDto` (NEW DTO) | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/AgeInStatePercentilesDto.cs` | NEW | `record AgeInStatePercentilesDto(string State, int SampleSize, IReadOnlyList<PercentileValue> Percentiles)`. Returned by the new endpoint. |
| `ITeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ITeamMetricsService.cs` | EXTEND | Add `IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)`. |
| `IPortfolioMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IPortfolioMetricsService.cs` | EXTEND | Add `IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)`. |
| `BaseMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BaseMetricsService.cs` | EXTEND | Add `protected IEnumerable<AgeInStatePercentilesDto> ComputeAgeInStatePercentiles(IEnumerable<WorkItem> completedItemsInWindow, IEnumerable<string> doingStatesInWorkflowOrder, IReadOnlyList<int> requestedPercentiles)`. Per ADR-019 algorithm: walks transitions via `IWorkItemStateTransitionRepository.GetAllByPredicate(t => completedItemIds.Contains(t.WorkItemId))` (or equivalent SQL-friendly composition), pairs entry→next-exit per state, buckets, computes percentiles via `PercentileCalculator`. |
| `TeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` | EXTEND | Implement `GetAgeInStatePercentilesForTeam` by: (1) `GetWorkItemsClosedInDateRange(team, startDate, endDate)` (existing); (2) call `ComputeAgeInStatePercentiles(...)`; (3) wrap in `GetFromCacheIfExists(team, $"AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () => …, logger)`. Default percentiles `[50, 70, 85, 95]` per DDD-3. Workflow state order from the team's existing `doingStates` (same source the chart's X axis uses). |
| `PortfolioMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PortfolioMetricsService.cs` | EXTEND | Same pattern as Team. The portfolio's "completed items in window" comes from the existing per-portfolio equivalent of `GetWorkItemsClosedInDateRange`. |
| `TeamMetricsController` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` | EXTEND | Add `[HttpGet("ageInStatePercentiles")] ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)`. Validation: `startDate.Date <= endDate.Date` (HTTP 400 with `StartDateMustBeBeforeEndDateErrorMessage`); auth via existing `[RbacGuard(TeamRead, ScopeIdRouteKey="teamId")]` if present at the class level (verify at GREEN). Implementation: `GetEntityByIdAnExecuteAction(teamRepository, teamId, team => teamMetricsService.GetAgeInStatePercentilesForTeam(team, startDate, endDate))`. |
| `PortfolioMetricsController` | `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioMetricsController.cs` | EXTEND | Mirror endpoint: `[HttpGet("ageInStatePercentiles")] ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentilesForPortfolio(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)`. |
| `IPercentileValue` (TS model) | `Lighthouse.Frontend/src/models/PercentileValue.ts` | NO CHANGE | Existing model is reused. The new model below composes it. |
| `IPerStatePercentileValues` (TS model — NEW) | `Lighthouse.Frontend/src/models/PerStatePercentileValues.ts` | NEW | `interface IPerStatePercentileValues { state: string; sampleSize: number; percentiles: IPercentileValue[]; }`. Mirrors backend DTO. |
| `MetricsService` (TS) | `Lighthouse.Frontend/src/services/Api/MetricsService.ts` | EXTEND | Add `getAgeInStatePercentiles(id: number, startDate: Date, endDate: Date): Promise<IPerStatePercentileValues[]>` to both the `IMetricsService` interface and the `MetricsService` class. Implementation mirrors `getCycleTimePercentiles` (line 247) — GET `/${this.api}/${id}/metrics/ageInStatePercentiles?${this.getDateFormatString(startDate, endDate)}`. |
| `useMetricsData` (hook) | `Lighthouse.Frontend/src/hooks/useMetricsData.ts` | EXTEND | Add `perStatePercentileValues: IPerStatePercentileValues[]` to the returned ctx (default `[]`). In the same `useEffect` block that calls `metricsService.getCycleTimePercentiles` (~line 211), add a parallel call to `metricsService.getAgeInStatePercentiles(entity.id, startDate, endDate)`. Both calls share the same window state; both can run in parallel via `Promise.all` (software-crafter chooses shape at GREEN). |
| `BaseMetricsView` | `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` | EXTEND | Pass `perStatePercentileValues={ctx.perStatePercentileValues}` to the `<WorkItemAgingChart>` instance (currently line 788-794). |
| `WorkItemAgingChart` | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` | EXTEND | Add optional `perStatePercentileValues?: IPerStatePercentileValues[]` prop. Add a custom SVG `<line>` overlay inside `<ChartsContainer>` per ADR-020 — one `<line>` per visible `(state, percentile)` pair, anchored to `x = stateIndex ± 0.4`, `y = percentileValue`, `strokeDasharray = "5 5"`, `stroke = ForecastLevel(percentile).color`. Extend the `useChartVisibility` wiring to manage a parallel `visiblePerStatePercentiles` map. Extend the `<PercentileLegend>` invocation with the new chip-group props. Per-segment hover tooltip (slice 02): `<percentile>th %ile for <state>: <value>d (n=<sampleSize>)`. Per-dot tooltip annotation (US-03, slice 02): one new line `Pace: above 85th percentile for <state>` (or appropriate bucket). |
| `PercentileLegend` | `Lighthouse.Frontend/src/components/Common/Charts/PercentileLegend.tsx` | EXTEND | Add optional `perStatePercentiles?: IPercentileValue[]`, `visiblePerStatePercentiles?: Record<number, boolean>`, `onTogglePerStatePercentile?: (percentile: number) => void`, `perStateGroupLabel?: string`, `perStateLowSampleNotice?: string` props. When `perStatePercentiles` is non-empty, render a second chip group below the existing one, with a sub-header `Age-in-State %iles (per state)` and per-chip styling consistent with the existing chips. Low-sample notice (per slice 02): append `Note: some states have low sample sizes — hover the chart bands for per-state counts.` to the group's tooltip when any state in `perStatePercentileValues` has `sampleSize < 10` (the WAC passes this via `perStateLowSampleNotice` derived from the data). |
| `useChartVisibility` (hook) | `Lighthouse.Frontend/src/hooks/useChartVisibility.ts` | EXTEND or INVOKE-TWICE (DDD-8) | Either extend the hook signature to accept a second percentile-set parameter, or invoke the hook twice from `WorkItemAgingChart` with the two sets. Software-crafter chooses at GREEN. Both shapes meet the independent-toggle requirement (DISCUSS US-02 AC). |
| Vitest + RTL tests for the new chart behaviour | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.test.tsx` + `PercentileLegend.test.tsx` | EXTEND | (a) No-regression test: render with `perStatePercentileValues` undefined → DOM matches existing snapshot; (b) per-state band rendering test: render with a 2-state, 2-percentile fixture → assert 4 `<line>` elements with expected x1/x2/y1/y2 inside `<ChartsContainer>`; (c) legend chip-group test: toggle one group, assert only that group's bands hide; (d) per-dot tooltip annotation test: hover a dot whose state has a percentile entry, assert the tooltip text matches the spec bucket. |
| Vitest tests for `MetricsService` and `useMetricsData` extension | colocated `.test.ts` files | EXTEND | Mock `getAgeInStatePercentiles` resolves; assert `ctx.perStatePercentileValues` populated; assert parallel-fetch shape. |
| NUnit tests for `BaseMetricsService.ComputeAgeInStatePercentiles` | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceTests.cs` (or alongside `TeamMetricsServiceTests`) | NEW / EXTEND | Fixture with 20 completed items having known per-state durations across 3 states (`In Progress`, `Review`, `Test`); assert exact 50/70/85/95 percentile values per state matching hand-computed expectations. Boundary tests: empty state, single-sample state, multi-visit item (re-work). Visit-level (not item-level) sampling test per ADR-019 enforcement table. Cache-key shape test per ADR-019. |
| NUnit tests for `TeamMetricsController.GetAgeInStatePercentilesForTeam` | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/TeamMetricsControllerTest.cs` | EXTEND | HTTP 400 on inverted date range; HTTP 200 with empty body on team with no completed items; HTTP 200 with expected dto shape on populated fixture; `[RbacGuard]` test asserts unauthenticated returns 401/403 (matches the pattern used by `cycleTimePercentiles` tests at the same controller). |
| NUnit tests for `PortfolioMetricsController.GetAgeInStatePercentilesForPortfolio` | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/PortfolioMetricsControllerTests.cs` | EXTEND | Same coverage as Team. |
| ArchUnitNET test (NEW rule) | `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/` (existing suite) | EXTEND | No class or interface named `*PerStateAggregation*` exists in the feature commit set (ADR-021). The metrics-service classes read transitions only via `IWorkItemStateTransitionRepository`, never via `DbSet<WorkItemStateTransition>` (extends the ADR-015 rule). The `BaseMetricsService.ComputeAgeInStatePercentiles` helper is `protected` (intra-inheritance only). |
| E2E spec | `Lighthouse.EndToEndTests/tests/specs/flow/AgingPacePercentiles.spec.ts` (new file) | NEW | 1 happy-path scenario per skill DoD: open team detail → see aging chart with both band families → toggle off `Age-in-State 85` → assert that percentile's per-state segments hide while CT bands remain. Per-state algorithm correctness lives in NUnit (faster, deterministic) per the established sibling-1 pattern. |

### EXTEND / NEW summary

**EXTEND: 16** (existing classes / files / hooks / components / test suites that gain additive code paths)
**NEW: 5** (`AgeInStatePercentilesDto`, `IPerStatePercentileValues` TS model, the new E2E spec file, the NUnit test additions for `ComputeAgeInStatePercentiles` as new methods on existing test classes, the new ArchUnitNET rule additions in the existing suite — every NEW item has zero existing overlap)
**REUSE AS-IS: 4** (`PercentileCalculator`, `PercentileValue`, `IPercentileValue` TS model, `ForecastLevel` color palette — no change to any of them; all four are consumed at the established API)

## Wave: DESIGN / [REF] Driving ports

| Method | Route | Auth | Status | Implementation pointer |
|---|---|---|---|---|
| GET | `/api/teams/{teamId:int}/metrics/ageInStatePercentiles?startDate&endDate` | `[RbacGuard(TeamRead)]` (per the existing `TeamMetricsController` class-level guard pattern; verify at GREEN) | NEW | `TeamMetricsController.GetAgeInStatePercentilesForTeam` — mirrors `cycleTimePercentiles` shape and validation (line 119) |
| GET | `/api/portfolios/{portfolioId:int}/metrics/ageInStatePercentiles?startDate&endDate` | `[RbacGuard(PortfolioRead)]` | NEW | `PortfolioMetricsController.GetAgeInStatePercentilesForPortfolio` — mirrors `cycleTimePercentiles` shape (line 100) |
| GET | `/api/teams/{teamId:int}/metrics/cycleTimePercentiles` | Existing | NO CHANGE (D11 of DISCUSS) | `TeamMetricsController.GetCycleTimePercentilesForTeam` — unchanged |
| GET | `/api/portfolios/{portfolioId:int}/metrics/cycleTimePercentiles` | Existing | NO CHANGE | `PortfolioMetricsController.GetCycleTimePercentiles` — unchanged |
| GET | `/api/teams/{teamId:int}/metrics/wip` (and other endpoints carrying `WorkItemDto`) | Existing | NO CHANGE | Sibling 1's `WorkItemDto.CurrentStateEnteredAt` and `Approximate` additions are inherited from sibling DESIGN; this feature does not modify the DTO further |

No new top-level routes. No premium gate. WS strategy Type A: additive throughout.

Response shape (both endpoints):

```
[
  { "state": "In Progress", "sampleSize": 42, "percentiles": [
      { "percentile": 50, "value": 3 },
      { "percentile": 70, "value": 5 },
      { "percentile": 85, "value": 8 },
      { "percentile": 95, "value": 14 } ] },
  { "state": "Review",      "sampleSize": 38, "percentiles": [ … ] },
  { "state": "Test",        "sampleSize": 35, "percentiles": [ … ] }
]
```

States are returned in workflow order (matching the chart's X-axis order). States with zero observations are omitted; the FE renders no per-state bands above them.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IWorkItemStateTransitionRepository` (extends `IRepository<WorkItemStateTransition>`) | `WorkItemStateTransitionRepository` (shipped by sibling 1, ADR-015) | REUSE AS-IS — sibling 1's repository is consumed via `GetAllByPredicate` |
| `WorkItem` read access via `IWorkItemRepository` / existing `GetWorkItemsClosedInDateRange` | `WorkItemRepository` (existing) | REUSE AS-IS |
| `WorkItem.CurrentStateEnteredAt` read access | Direct property on `WorkItem` (shipped by sibling 1, ADR-016) | REUSE AS-IS — read-only for this feature (sibling 1's `WorkItemService.RefreshWorkItems` is the only mutator) |
| Cache: existing `BaseMetricsService.GetFromCacheIfExists` | Existing in-process cache (post-`bug-5016-cache-thread-safety` thread-safe) | REUSE AS-IS — new cache key namespace `AgeInStatePercentiles_...` slots into the same scope-keyed cache |

External integrations: NONE introduced by this feature. The endpoint reads only Lighthouse-internal persisted data (transitions table + work items table). The sibling 1 connector capture machinery is what populates the transitions table — this feature is purely a downstream reader. **No contract tests recommended** because no external integration is introduced.

## Wave: DESIGN / [REF] Technology choices

| Component | Pin |
|---|---|
| Backend | C# .NET 8, ASP.NET Core, EF Core 8.x (existing) |
| Backend tests | NUnit 4.6, Moq, EF InMemory, `Microsoft.AspNetCore.Mvc.Testing` (project_test_stack memory: project actually uses NUnit, not CLAUDE.md's xUnit claim) |
| Backend mutation | Stryker.NET (≥80% kill rate gate per CLAUDE.md) |
| Backend ArchUnit | ArchUnitNET (existing suite extended per ADR-021 rules) |
| Frontend | React 18 + TypeScript 5.x (strict), MUI 5.x, MUI-X-charts (existing chart library — no version change) |
| Frontend tests | Vitest + React Testing Library |
| Frontend mutation | Stryker (TS) (≥80% kill rate gate) |
| Frontend linter | Biome (zero errors / zero warnings on `./src` per CLAUDE.md) |
| E2E | Playwright with Page Object Model |

NO new technology introduced. NO new library dependency. NO new third-party service.

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Source / ADR |
|---|---|---|
| DDD-1 | Visit-level (not item-level) percentile sampling | ADR-019 |
| DDD-2 | Item-membership rule: `ClosedDate ∈ window` (mirrors `cycleTimePercentiles`) | ADR-019 |
| DDD-3 | Percentile function: existing `PercentileCalculator` with defaults 50/70/85/95 | ADR-019 |
| DDD-4 | Empty / low-sample at API: emit when ≥1, omit when 0; FE handles presentation | ADR-019 |
| DDD-5 | Cache key shape and invalidation reuse the `cycleTimePercentiles` pattern | ADR-019 |
| DDD-6 | Per-state band rendering: custom SVG `<line>` overlay inside `<ChartsContainer>` | ADR-020 |
| DDD-7 | Chart prop interface: optional `perStatePercentileValues` → backwards-compatible | ADR-020 |
| DDD-8 | Legend chip groups: independent visibility wiring via `useChartVisibility` | ADR-020 |
| DDD-9 | Backend computation: `protected` helper inside `BaseMetricsService` | ADR-021 |
| DDD-10 | ADR-018 disposition: UPHELD — no `IPerStateAggregationService` | ADR-021 |
| DDD-11 | US-03 tooltip: client-side bucket computation from `daysInState` + per-state percentiles | DESIGN, no ADR (mechanical) |
| DDD-12 | Endpoint route shape mirrors `cycleTimePercentiles` precisely | DESIGN, no ADR (mechanical) |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WorkItemAgingChart` | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` | Already renders the dot scatter, the existing CT band lines (line 349-364), the X-axis state columns (line 287-298), the SLE line, the per-type filter chips, and integrates with `WorkItemsDialog` on dot click | EXTEND | The new per-state bands are conceptually "alongside the existing CT bands"; per DISCUSS D6 + ADR-020, they share the same chart, the same coordinate system, the same legend strip, the same tooltip surface. A new sibling widget would duplicate every one of these and lose the "alongside" semantics. Net add: ~30 LOC for SVG overlay + ~10 LOC for prop + ~10 LOC for legend wiring. |
| `PercentileLegend` | `Lighthouse.Frontend/src/components/Common/Charts/PercentileLegend.tsx` | Already renders a chip group with `(percentiles, visiblePercentiles, onTogglePercentile)` for the existing CT bands; supports an optional SLE chip | EXTEND | Adding a SECOND chip group with parallel props is structurally consistent with the existing component shape. A new sibling legend would split the legend strip and complicate alignment. Net add: ~20 LOC. |
| `useChartVisibility` | `Lighthouse.Frontend/src/hooks/useChartVisibility.ts` | Already manages `visiblePercentiles: Record<number, boolean>` and `visibleTypes`; supports SLE visibility toggle | EXTEND (per DDD-8 — software-crafter chooses extend signature OR invoke twice at GREEN) | Either shape meets the independent-toggle requirement. CREATE NEW hook would duplicate state machinery. |
| `MetricsService` / `IMetricsService` (TS) | `Lighthouse.Frontend/src/services/Api/MetricsService.ts` | Already exposes `getCycleTimePercentiles(id, startDate, endDate)` with identical shape to what `getAgeInStatePercentiles` needs (line 247-260) | EXTEND | One new method following the exact same shape. CREATE NEW service would duplicate the HTTP plumbing, the date-format string helper, the per-entity scoping pattern. |
| `useMetricsData` | `Lighthouse.Frontend/src/hooks/useMetricsData.ts` | Already fetches `percentileValues` via `getCycleTimePercentiles` (~line 211); already manages the window state shared across all metrics fetches | EXTEND | Add one parallel call in the same effect; add one returned ctx field. CREATE NEW hook would duplicate the entire window-state management. |
| `BaseMetricsView` | `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` | Already renders `<WorkItemAgingChart>` from `ctx.percentileValues` (line 788-794); shared across team and portfolio routes | EXTEND | Pass one more prop. CREATE NEW view would split team and portfolio metrics rendering. |
| `TeamMetricsController.GetCycleTimePercentilesForTeam` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs:119-129` | Already does: route attribute, query-string date validation, log-and-dispatch via `GetEntityByIdAnExecuteAction(teamRepository, teamId, …)` | EXTEND (pattern, not the method itself) | The new endpoint mirrors this exact shape — same validation, same error message, same dispatch pattern. CREATE NEW controller would duplicate scaffolding. |
| `PortfolioMetricsController.GetCycleTimePercentiles` | `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioMetricsController.cs:100-114` | Same shape as TeamMetricsController equivalent | EXTEND (pattern) | Same reasoning. |
| `TeamMetricsService.GetCycleTimePercentilesForTeam` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs:303-320` | Already: GetWorkItemsClosedInDateRange + map to cycle times + `PercentileCalculator.CalculatePercentile` per 50/70/85/95 + `GetFromCacheIfExists` wrap | EXTEND (pattern) + new method | New `GetAgeInStatePercentilesForTeam` reuses the SAME `GetWorkItemsClosedInDateRange` predicate (per ADR-019 DDD-2) and the same `GetFromCacheIfExists` mechanism with the same date-stamped key namespace. The per-state algorithm itself is new (lives in `BaseMetricsService` protected helper per DDD-9). |
| `PortfolioMetricsService.GetCycleTimePercentilesForPortfolio` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PortfolioMetricsService.cs` (mirror of Team equivalent) | Same shape | EXTEND (pattern) + new method | Same reasoning. |
| `BaseMetricsService.GetFromCacheIfExists` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BaseMetricsService.cs` | Already provides the cache + per-entity invalidation hook; thread-safe after `bug-5016-cache-thread-safety` | REUSE AS-IS | The new cache key namespace `AgeInStatePercentiles_...` slots in alongside the existing `CycleTimePercentiles_...`. No mechanism change. |
| `PercentileCalculator.CalculatePercentile` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PercentileCalculator.cs` | Already implements the nearest-rank algorithm Lighthouse uses for `cycleTimePercentiles` | REUSE AS-IS | Algorithmic parity per ADR-019 DDD-3. |
| `PercentileValue` (C# model) | `Lighthouse.Backend/Lighthouse.Backend/Models/Metrics/PercentileValue.cs` | Already used by `cycleTimePercentiles` response | REUSE AS-IS | The new `AgeInStatePercentilesDto.Percentiles` is `IReadOnlyList<PercentileValue>` — same type. |
| `IWorkItemStateTransitionRepository` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Repositories/IWorkItemStateTransitionRepository.cs` (shipped by sibling 1, ADR-015) | Already exposes `GetAllByPredicate(Expression<Func<WorkItemStateTransition, bool>>)` via `IRepository<T>` | REUSE AS-IS | The per-state walk in `ComputeAgeInStatePercentiles` calls `GetAllByPredicate(t => completedItemIds.Contains(t.WorkItemId))` (or an equivalent EF-friendly composition; software-crafter chooses at GREEN). Adding methods to the repository was explicitly deferred to consumer DESIGNs per sibling ADR-015; this DESIGN does NOT add methods — the base `GetAllByPredicate` is sufficient. |
| `WorkItem.CurrentStateEnteredAt` | sibling 1 ADR-016 | Persisted column on `WorkItem` | REUSE AS-IS | Read-only access; used by the chart's `daysInState` derivation feeding US-03 client-side bucket. No mutation. |
| `WorkItem.ClosedDate`, `WorkItem.StartedDate`, `GetWorkItemsClosedInDateRange` | `WorkItemBase` + `BaseMetricsService` | Existing persistence + existing predicate | REUSE AS-IS | Item-membership rule per ADR-019 DDD-2 reuses this predicate exactly. |
| `ForecastLevel(percentile).color` palette | `Lighthouse.Frontend/src/components/Common/Forecasts/ForecastLevel.ts` (already imported by `WorkItemAgingChart`) | Already provides the per-percentile color for the existing CT bands | REUSE AS-IS | The new per-state bands use the SAME color per percentile so 85th-CT and 85th-per-state look visually related but differ only in span. |
| MUI-X `<ChartsContainer>` | `@mui/x-charts` (existing dependency) | Provides the chart coordinate system; supports arbitrary SVG children | REUSE AS-IS | The custom `<line>` overlay (ADR-020) is rendered as a child of the existing `<ChartsContainer>`. No new chart library; no new chart container. |

**Totals: 17 rows. 7 EXTEND + 10 REUSE-AS-IS + 0 CREATE-NEW at the OVERLAP level.** (The NEW entries — `AgeInStatePercentilesDto`, `IPerStatePercentileValues` TS model, the new E2E spec, the new test methods, the new ArchUnit rules — appear in the Component decomposition; they have ZERO existing overlap per the grep below.)

CODEBASE GREP FOR OVERLAP CANDIDATES THAT MIGHT JUSTIFY CREATE-NEW BUT WERE REJECTED:

- Searched for `AgeInState|ageInState|PerStatePercentile|perStatePercentile` across `Lighthouse.Backend` + `Lighthouse.Frontend/src` — zero production matches. No existing DTO, service, hook, or component duplicates the per-state percentile concern.
- Searched for `PerState|StateAggregation|PerStateMetric` — zero production matches. Confirms ADR-018 + ADR-021's "no existing service to extend" position.
- Searched for `BandSegment|ChartBand|PercentileBand` — zero production matches. The new SVG `<line>` overlay (ADR-020) is the first per-state band rendering in the codebase.

## Wave: DESIGN / [REF] Open questions

- **MUI-X coordinate-system access from custom SVG children**: ADR-020 assumes that an SVG `<line>` rendered as a child of `<ChartsContainer>` receives the same coordinate system as `<ChartsReferenceLine>` (i.e. `x` and `y` are in data space, not pixel space). This is consistent with MUI-X's documented API but is a small interop assumption. **Validation deferred to DELIVER spike (slice 01, ~30 min per slice-01 spec)**: confirm at the start of slice 01 implementation. If false, ADR-020's alternative would be to use an `<svg>` overlay positioned via `getBoundingClientRect()` math on the chart container — heavier, but still feasible. No DESIGN blocker.
- **Endpoint profiling at scale**: ADR-019's algorithm is `O(transitions_per_item × completed_items_in_window)`. For a team with 200 completed items × 12 transitions each over a 6-month window, ~2400 row-level operations per request. Expected to be sub-100ms uncached. **Validation deferred to DELIVER spike (slice 01, ~30 min per slice-01 spec)**: profile against a team with 6 months of transition data on the project's own ADO instance. If profiling shows materially worse performance, the existing `GetFromCacheIfExists` already handles repeat requests; a one-step materialisation (e.g. precompute per (team, window) at sync time) is a follow-up — NOT in scope for MVP.
- **`useChartVisibility` shape (extend signature vs. invoke twice)**: DDD-8 leaves this to software-crafter at GREEN. Both shapes meet the architectural requirement (independent toggle state). Software-crafter picks the cleaner shape after seeing the call site.
- **Linear connector transition timestamps**: depend on sibling 1's per-connection runtime downgrade behaviour (ADR-017). When Linear's `history` field is unavailable, the transitions for that team are sync-cadence approximations — the per-state bands for that team will be band-heights derived from approximate timestamps. **Resolution**: no change required by this DESIGN. The sibling's `WorkItemDto.Approximate` flag already indicates the badge tooltip on the in-flight side. For the historical band side, the approximation is inherited from the transition data; no separate annotation needed (a band derived from 10 approximate-timestamp items is not meaningfully more wrong than the same band derived from 10 exact items). Documented as an inherited characteristic, not a feature gap.
- **Backfill of pre-feature transitions**: explicitly OUT OF SCOPE per DISCUSS (forward-only constraint inherited from sibling 1). First weeks post-release will have low / empty per-state bands for most states. The legend low-sample tooltip (slice 02) surfaces this honestly. **Resolved by DISCUSS**; no DESIGN action.

## Wave: DESIGN / [REF] Cross-MVP coordination outcomes

- **ADR-018 (sibling 1) disposition: UPHELD.** This DESIGN does NOT introduce `IPerStateAggregationService`. Per-state percentile computation lives as a `protected` helper inside `BaseMetricsService`, consumed only by `TeamMetricsService` / `PortfolioMetricsService` via inheritance. Repository-level sharing (sibling 1's `IWorkItemStateTransitionRepository`) IS the architectural seam across siblings. Rationale captured in ADR-021.
- **No new flag-ups for sibling B3 (`state-time-cumulative-view`) DESIGN.** B3's DESIGN author retains full freedom to either (a) follow the same Path A pattern this feature establishes (independent computation), or (b) supersede both ADR-018 and ADR-021 with a new ADR that argues for consolidation in light of B3's concrete query shape. ADR-021's "Reviewer note (cross-MVP)" section flags both options for B3's DESIGN author.
- **No upstream impact on sibling 1 DESIGN.** D11 of DISCUSS held: no schema additions to `WorkItemStateTransition`. ADR-019's algorithm reads only the 4 fields shipped by sibling 1 plus `WorkItem.CurrentStateEnteredAt` (read-only) plus `WorkItem.ClosedDate` (read-only, unchanged). Zero feedback to sibling 1.
- **Semantic divergence vs sibling B3 made permanent in writing.** ADR-019 explicitly contrasts this feature's "ClosedDate-in-window + unclipped visit-level duration" rule with sibling B3's "frame-intersection + unclipped item-level duration including in-flight" rule. The two endpoints are intentionally named differently (`ageInStatePercentiles` vs B3's `cumulativeStateTime`) and ADR-021's enforcement rules prevent silent helper consolidation.

## Wave: DESIGN / [REF] Wave decisions summary

**Primary architectural commitment**: introduce one new endpoint per scope (team + portfolio), one chart prop extension, one custom SVG overlay inside the existing chart's `<ChartsContainer>`, and one `protected` helper inside `BaseMetricsService`. Every other touchpoint is an additive extension of an existing class / hook / component / DTO / cache key namespace. NO new top-level routes. NO new external integration. NO new external library. NO new persistence. NO premium gate. NO breaking change.

**Architecture pattern**: ports-and-adapters / hexagonal (unchanged). Reads transitions via the sibling 1 repository port; no new port introduced.

**Walking skeleton path** (validates the architecture in one slice end-to-end): existing sibling-1 `WorkItemStateTransition` rows → new `BaseMetricsService.ComputeAgeInStatePercentiles` helper → new `TeamMetricsService.GetAgeInStatePercentilesForTeam` method → new `TeamMetricsController.GetAgeInStatePercentilesForTeam` endpoint → new `MetricsService.getAgeInStatePercentiles` TS method → extended `useMetricsData` ctx → extended `WorkItemAgingChart` prop → custom SVG overlay rendering on the team detail page. One slice (slice 01) exercises every architectural seam end-to-end.

**Sibling MVP coordination**: ADR-018 upheld (ADR-021); semantic divergence vs sibling B3 documented permanently in ADR-019; sibling 1's DESIGN unchanged (no schema feedback, no port feedback).

**Downstream changes propagated upstream**: NONE. Sibling 1 DESIGN unchanged. Sibling B3 DESIGN unaffected.

**Handoff readiness**: ready for nw-acceptance-designer (DISTILL) handoff. The 3 user stories with their AC are already locked at DISCUSS; this DESIGN adds the architectural decomposition the acceptance designer needs to write executable specs against. Subsequent nw-platform-architect (DEVOPS) handoff: no CI workflow changes required (the existing test gates cover the new code; the existing `ci_verifysqlite.yml` + `ci_verifypostgres.yml` cover the no-new-migration case).

## Wave: DESIGN / [REF] Outcome Collision Check

`nwave-ai outcomes check-delta docs/feature/aging-pace-percentiles/feature-delta.md` was NOT executed: the tool (`nwave-ai`) is not installed in this repository and the canonical outcomes registry at `docs/product/outcomes/registry.yaml` does not exist. Per skill ("skip-and-document"): documented here, deferred to DEVOPS handoff for KPI / outcomes registration alongside the DISCUSS-defined `OUT-aging-pace-bands-rendered`, `OUT-aging-pace-legend-toggled`, `OUT-aging-pace-parity-claim`.

