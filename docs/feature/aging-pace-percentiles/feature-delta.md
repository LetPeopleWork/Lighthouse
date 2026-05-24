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
