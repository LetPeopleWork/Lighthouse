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
| D4 | Per-team / per-portfolio percentile **configuration is deferred** to a post-MVP follow-up (provisionally `aging-pace-percentile-config`). No ADO story exists for it yet — configurable percentiles are simply out of scope for the MVP, which ships fixed 50/70/85/95 defaults. | Locked (deferred). |
| D5 | Edge cases: **empty distribution** (no completed items in window for a state) → that state's filled band is omitted (the existing overall cycle-time lines + chips are unaffected). **Small samples** → the band still renders from whatever data exists; **no low-sample messaging in MVP** (dropped to keep the feature visual-only — see Out of scope). **Bimodal distributions** → out of scope; the percentile math is still mathematically defined and the user can read the colors. | Locked (2026-05-25 simplification). |
| D6 | Visual treatment: per-state bands render as **filled colored background zones** spanning each state column's full width, drawn **behind the dots** — green below the 50th percentile, grading through yellow / orange to red above the 95th, so the column background itself signals how an item's age compares to history for that state. NOT dashed line segments. The existing overall cycle-time percentile lines (50/70/85/95) and the SLE line are **unchanged** and keep their own chips. A **single new legend chip** toggles the entire per-state band overlay on/off; it is **OFF by default**. No per-percentile sub-toggles and no second chip *group* — one chip for the whole overlay. | Locked (2026-05-25 simplification — supersedes the earlier "short dashed segments + two chip groups" design). |
| D7 | Filter composition: per-state bands respect the same filters that the existing chart respects — work-item type chips, team/portfolio scope, date range (which IS the history window per D3). No new filter primitives. | Locked. |
| D8 | Scope: **team-level and portfolio-level chart simultaneously** (the chart component `WorkItemAgingChart` is rendered in both `BaseMetricsView` instances via team and portfolio routes — `BaseMetricsView.tsx:789`). Shipping per-state bands for only one scope would create a confusing parity hole. | Locked. |
| D9 | **Data foundation reuse, not rebuild**: this feature consumes `WorkItemStateTransition` rows shipped by `time-in-state-and-staleness` slice 01 + 02. From transitions, derive `ageAtStateExit[state] = transition.timestamp - currentStateEnteredAt(state)` for completed items in the window. No new sync-side capture; no new persistence beyond a per-team materialised cache if profiling demands it (deferred to DESIGN). | Locked. |
| D10 | **WS strategy: Type A (additive)**. No contract change to existing endpoints. A new endpoint `GET /api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` (and the matching portfolio route) returns `[{ state: string, percentiles: PercentileValue[] }]`. The chart component takes a new optional prop `perStatePercentileValues`; if absent or empty, behaviour is identical to today. | Locked. |
| D11 | **Sibling-data dependency on `WorkItemStateTransition`**: this DISCUSS does NOT request any schema addition to the transition table. The four fields shipped in sibling slice 01 (`workItemId`, `fromState`, `toState`, `transitionedAt`) are sufficient. The "done date" required for "completed in window" is derivable from the existing Started/Closed dates already on the `WorkItem` entity (unchanged). | Locked — explicit no-impact on sibling DESIGN. |
| D12 | **Band metric = cumulative total age at state exit** (NOT time-spent-in-that-state). For each completed item and each `Doing`-category state it passed through, the observation is the item's **total work-item age at the moment it left that state** = `exitTransition.TransitionedAt − StartedDate` (the same age units the chart's Y axis already uses for the in-flight dots). Percentiles are computed over those total-age-at-exit observations, bucketed by state. Because total age accumulates as work moves right through the workflow, the bands **rise monotonically left→right** — matching ActionableAgile and making each band directly comparable to the dots plotted above its column. | Locked (2026-05-25) — corrects the original `exit − entry` per-state-duration framing, which was not comparable to the chart's total-age Y axis. |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Overlay per-state pace-percentile bands on the Work Item Aging chart (team + portfolio)

**Story**: As a `flow-coach`, I want per-state pace-percentile bands (50th / 70th / 85th /
95th of historical total-age-at-state-exit, per workflow state) rendered as colored
background zones on the Work Item Aging chart — in both team and portfolio scope — so I can
see at a glance which in-flight items are aging past where most historically-finished items
had reached by the time they left the same state.

**Job-id**: `job-flow-coach-spot-pace-outliers`

### Elevator Pitch
Before: I can only see the chart's existing end-to-end cycle-time lines — they tell me "this item is past the 85th percentile of overall cycle time" but not "this item, currently in Review, is older than 85% of items ever were by the time they left Review". For that I would mentally compare each dot's age against my memory of how far items historically got before leaving that state.
After: open `/teams/{teamId}` (or `/portfolios/{portfolioId}`) → scroll to the Work Item Aging chart → toggle on the new **Pace percentiles** chip → each state column's background fills with colored zones (green below the 50th, grading to red above the 95th, at the **cumulative total-age-at-exit** percentiles for that state, per D12). A dot sitting in the red zone of the `Review` column is visibly aging past where 95% of finished items had reached when they left Review.
Decision enabled: which in-flight items are aging slower than history for the state they're currently in — the colored column background names the concern without comparing numbers in your head, and works identically on the portfolio chart.

**AC**:
- Given a team (or portfolio) with completed items in the configured history window AND `WorkItemStateTransition` data present, when I open the metrics view containing the Work Item Aging chart and toggle the **Pace percentiles** chip on, then each state column renders a filled colored background zone whose 50/70/85/95 boundaries are at the cumulative total-age-at-state-exit percentiles for that state (per D12), computed over the same history window the existing `cycleTimePercentiles` endpoint uses, colored green (below 50th) → red (above 95th), drawn behind the dots.
- Given the chart loads, when I have not toggled the chip, then **no per-state bands render** (off by default) and the chart looks exactly as it does today — the existing overall cycle-time lines and SLE line are unchanged whether the chip is on or off (no regression).
- Given a state with zero completed items in the window, when the overlay is on, then no band renders for that state's column; bands for other states still render.
- Given a portfolio (not a team), the same behaviour holds for the portfolio's Work Item Aging chart, derived from completed work items in the portfolio's scope (D8) — the Work Item Aging chart is not a team-only concern.
- Existing chart filters (work-item type chips, date range) continue to work; toggling a type off recomputes both the in-flight dots AND the per-state bands' historical sample (no stale band heights).

### US-02 — Single legend chip toggles the pace-percentile band overlay on/off

**Story**: As a `flow-coach`, I want one chip on the Work Item Aging chart that turns the
per-state pace-percentile background bands on and off, defaulting to off, so the chart stays
clean until I deliberately ask for the pace lens — without adding any new controls to the
existing percentile / SLE chips, which keep working exactly as they do today.

**Job-id**: `job-flow-coach-spot-pace-outliers`

### Elevator Pitch
Before: the chart already has chips for the overall percentile lines (50/70/85/95) and the SLE line; those stay. I just want a way to switch the new colored background on when I want it and have it gone otherwise.
After: a single **Pace percentiles** chip sits alongside the existing chips. Off by default → no colored background, chart looks like today. Click it on → every state column's background fills with its colored pace zones. Click again → gone. No per-percentile sub-toggles, no second chip group — one switch for the whole overlay.
Decision enabled: opt into the pace lens for a flow conversation, then drop back to the clean chart — one click each way.

**AC**:
- Given the chart renders, when the page first loads, then the **Pace percentiles** chip is present but **off**, and no colored background bands are drawn.
- Given the chip is off, when I click it, then all per-state colored background bands appear; when I click it again, they all disappear. The existing overall-percentile chips and the SLE chip are unaffected either way.
- Given the existing percentile / SLE chips, when I toggle any of them, then today's behaviour is unchanged — the new chip is purely additive and independent.
- Toggle state persists for the current session but is not persisted server-side (matches existing chip toggle behaviour).

> **US-03 (in-flight dot tooltip pace annotation) — REMOVED 2026-05-25.** The original third
> story appended a `Pace: above 85th percentile for <state>` line to each dot's tooltip. The
> user scoped the feature down to "the coloring with the toggle, nothing more" — the colored
> column background carries the signal on its own, so the dot annotation, the per-band hover
> tooltip, and all low-sample messaging are cut. See **Out of scope**. (ADO #5080 to be
> removed; ADO #5079 folds into #5075.)

## Wave: DISCUSS / [REF] Definition of Done

1. Both stories (US-01, US-02) pass their ACs via integration tests (NUnit + EF InMemory + WebApplicationFactory for the new endpoint; Vitest + React Testing Library for chart band rendering and the toggle chip).
2. Per-state percentile math verified against a fixture: a known set of completed items with known per-state transition timestamps, asserting the exact 50/70/85/95 **cumulative total-age-at-state-exit** values (per D12) for each state.
3. No regression in existing `WorkItemAgingChart` tests (`WorkItemAgingChart.test.tsx`) — with the overlay off (default), the chart renders exactly as before; the existing overall cycle-time lines and SLE line are unaffected with the overlay on or off.
4. Per-state band rendering tolerates empty distribution per state (band omitted) and single-completed-item per state, gracefully (per AC). No low-sample messaging (cut).
5. Portfolio-scope parity: both team and portfolio Work Item Aging charts show the bands (D8) — built once via the shared `BaseMetricsView` / `WorkItemAgingChart`.
6. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
7. SonarCloud quality gate passes on PR.
8. Mutation testing (Stryker.NET for Backend; Stryker for Frontend): ≥80% kill rate for new code.
9. Docs updated: screenshot of the chart with the **Pace percentiles** overlay toggled on, with a callout for the colored per-state bands.

## Wave: DISCUSS / [REF] Out of scope

- **In-flight dot tooltip pace annotation** (the old US-03 `Pace: above 85th percentile for <state>` line) — **cut 2026-05-25**. The colored column background is the whole signal; no per-dot text. (ADO #5080 to be removed.)
- **Per-band hover tooltip** (e.g. `85th %ile for Review: 10d (n=38)`) — cut. The bands are read by color, not by hovering for numbers.
- **Low-sample messaging** (the old "Based on N completed items (low sample)" tooltip + legend notice) — cut. Bands render from whatever data exists; states with zero data simply show no band. Consequently the API returns **no `sampleSize` field**.
- **Per-percentile sub-toggles / a second chip *group*** — cut. One single chip toggles the entire overlay (US-02). The existing overall-percentile and SLE chips are untouched.
- **Configurable percentiles per team/portfolio** — deferred (locked D4). No ADO story exists yet; MVP ships fixed 50/70/85/95.
- **Per-state distribution window override** — out of scope; uses the team/portfolio history window per D3.
- **Bimodal-distribution-aware visual treatment** — out of scope (D5).
- **Per-state historical drill-down chart** — that is feature B2 (`work-item-state-history-view`), post-MVP. This feature only renders aggregate per-state percentile heights, not per-item history.
- **Cumulative time-per-state across timeframe** — that is sibling MVP feature `state-time-cumulative-view`. Different question; different chart.
- **Changes to existing `cycleTimePercentiles` endpoint or `WorkItemStateTransition` schema** — D11 explicitly preserves both. Sibling feature DESIGN is unaffected.
- **Backfill of historical transitions before sibling slice 01 shipped** — sibling locked this as forward-only; this feature inherits the same constraint. Therefore the per-state bands will be empty for some states for the first weeks post-release until enough completed items have transitions captured.

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract change to existing endpoints. One new
endpoint per scope and one new chart prop. Walking skeleton = US-01 across team + portfolio,
with `PercentileLegend` extended to show one new **Pace percentiles** toggle chip (US-02).

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | **New** | Returns `[{ state: string, percentiles: [{ percentile: int, value: double }] }]` for the team's completed-in-window items, where each `value` is the cumulative total-age-at-state-exit percentile (D12) derived from `WorkItemStateTransition`. States with zero completed-item observations are omitted. No `sampleSize` field (low-sample messaging cut). |
| GET | `/api/portfolios/{portfolioId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | **New** | Same shape as the team route, scoped to the portfolio (D8). |
| GET | `/api/teams/{teamId}/metrics/cycleTimePercentiles` | Authenticated | Existing | **Unchanged** (D11). |

No new top-level routes; no schema additions. UI surfaces: `WorkItemAgingChart` (extended
with `perStatePercentileValues` prop + filled-band SVG overlay), `PercentileLegend` (extended
with one **Pace percentiles** toggle chip), `BaseMetricsView` (passes the new metric ctx slot
through, for both team and portfolio).

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
| `OUT-aging-pace-legend-toggled` | ≥15% of users who view a team or portfolio detail page interact with the new **Pace percentiles** toggle chip at least once within 4 weeks of release | per_instance | Existing page-view telemetry on the detail route + a new event `aging.pace.bands.toggled` emitted on chip click (instrumentation requirement carries to DEVOPS handoff) |
| `OUT-aging-pace-parity-claim` | Zero customer reports within 8 weeks of release of "Lighthouse aging chart is missing ActionableAgile-style per-state bands" — verifies the parity claim is now defensible | vendor_demo_only + community reports | Issue tracker label `feature-request-aging-pace`; community Slack mentions |

KPIs will be appended to `docs/product/kpi-contracts.yaml` at the DEVOPS handoff.

## Wave: DISCUSS / [REF] Definition of Ready — validation

| # | DoR item | Verdict | Evidence |
|---|---|---|---|
| 1 | Every story traces to a `job_id` | Pass | US-01, US-02 → `job-flow-coach-spot-pace-outliers` (new entry added to `docs/product/jobs.yaml` in this run). US-03 removed 2026-05-25. |
| 2 | Persona named & scoped | Pass | `flow-coach` primary (existing persona, reused — different decision shape from sibling, explicitly differentiated in JTBD one-liner); `delivery-forecaster` secondary (existing persona). |
| 3 | Elevator pitch per non-`@infrastructure` story | Pass | Each US-NN has Before/After/Decision triplet referencing real entry points (`/teams/{teamId}` + `/portfolios/{portfolioId}` routes + the **Pace percentiles** chip) and concrete observable output (filled green→red band zones per state column). |
| 4 | AC testable, no ambiguous outcomes | Pass | Quantified percentiles (50/70/85/95); explicit off-by-default and empty-state behaviour; explicit no-regression on existing endpoint and chart. |
| 5 | Out-of-scope explicit | Pass | Items listed include the 2026-05-25 cuts (dot tooltip annotation, per-band hover tooltip, low-sample messaging, per-percentile sub-toggles) plus configurable percentiles, distribution-window override, bimodal handling, per-item drill-down, cumulative view, existing endpoint changes. |
| 6 | Outcome KPIs measurable with targets | Pass | 3 KPIs, each with numeric target, scope, and measurement method. |
| 7 | Pre-requisites resolved | Pass (with sequencing note) | Sibling DISCUSS complete; existing chart confirmed present in code. The sibling-slice-01-shipped pre-req is sequencing, not a blocker for THIS wave — it gates DELIVER, not DISCUSS or DESIGN. |
| 8 | Slice composition: each slice contains ≥1 user-visible story | Pass | Post-simplification the feature is a **single slice** shipping US-01 + US-02 (colored per-state bands + one toggle chip, team + portfolio — all value-bearing). No `@infrastructure`-only slices. |
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
cumulative total-age-at-state-exit percentiles (D12), drawn as filled colored background
zones per state column (green→red), toggled by one chip and off by default. The existing
full-width CT lines and SLE line stay unchanged; the new per-state bands are added.

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
| DDD-1 | Per-state percentile algorithm: visit-level (not item-level) observations of **cumulative total age at state exit** `ageAtStateExit = exitTransition.TransitionedAt − StartedDate` per completed visit, bucketed by state (per D12) | Locked (2026-05-25 correction) | The chart's Y axis is total work-item age, so the band must be in total-age units for a dot to be directly comparable to the band above its column; bands rise left→right. **Supersedes the earlier `exit − entry` per-state-duration formula**, which was not comparable to the chart's Y axis. See ADR-019 |
| DDD-2 | Item-membership rule: `W.ClosedDate ∈ [startDate, endDate]` — mirrors `cycleTimePercentiles` exactly. EXPLICITLY DIFFERENT from sibling B3's frame-intersection rule | Locked | Keeps band heights comparable to the existing CT bands shown on the same chart; semantic divergence vs B3 is the deliberate distinction; ADR-019 |
| DDD-3 | Percentile function: reuse existing `PercentileCalculator.CalculatePercentile` (nearest-rank with clamp). Defaults 50/70/85/95 per DISCUSS D2 | Locked | Algorithmic parity with `cycleTimePercentiles` — user reads both percentile families on the same chart | ADR-019 |
| DDD-4 | Empty / single at the API: emit dto when ≥1 observation, omit the state entry when 0. **No `sampleSize` field and no low-sample messaging** (cut 2026-05-25, per DISCUSS D5) | Locked | Bands are visual-only; a state with no data simply renders no band; ADR-019 |
| DDD-5 | Cache via existing `BaseMetricsService.GetFromCacheIfExists` with key `AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}`; inherits existing post-sync invalidation hook | Locked | Zero new cache infrastructure; matches existing `cycleTimePercentiles` cache pattern | ADR-019 |
| DDD-6 | Per-state band rendering: custom SVG **`<rect>` filled-zone** overlay rendered as a child of the existing `<ChartsContainer>`, **before (behind) the dots**. One stacked set of rects per state column, anchored to that column's index span on the existing **linear** X axis (the chart is a `ScatterPlot` with a linear, NOT band, scale — column half-width ≈ `0.4` in data units, so a band spans `x ∈ [stateIndex−0.4, stateIndex+0.4]`); each rect spans one percentile boundary to the next in Y, filled green→red. NOT dashed `<line>` segments; NOT `ChartsReferenceLine` (no X-range support); NOT a sibling widget; NOT a chart replacement | Locked (2026-05-25 — filled zones supersede the earlier short-line-segment design) | Matches D6; dot z-order preserved by drawing the overlay first; ADR-020 |
| DDD-7 | Chart prop interface: `WorkItemAgingChart` accepts new optional `perStatePercentileValues?: IPerStatePercentileValues[]`. Absent / empty → renders today-identical (guarded by snapshot test) | Locked | Backwards-compatible; matches DISCUSS WS Type A | ADR-020 |
| DDD-8 | Legend wiring: a **single boolean toggle** for the whole band overlay (`showPaceBands` + `onTogglePaceBands`), surfaced as one new chip in `PercentileLegend`, **off by default**. No per-percentile visibility map for the bands; the existing `useChartVisibility` percentile/SLE wiring is untouched | Locked (2026-05-25 — single chip supersedes the two-chip-group design) | DISCUSS US-02 now asks for one on/off switch — the simplest possible state | ADR-020 |
| DDD-9 | Backend computation lives as a `protected` helper inside `BaseMetricsService` (`ComputeAgeInStatePercentiles`); both `TeamMetricsService.GetAgeInStatePercentilesForTeam` and `PortfolioMetricsService.GetAgeInStatePercentilesForPortfolio` delegate to it. Helper takes the result of the scope-specific "completed items in window" query plus workflow state order plus requested percentiles | Locked | Mirrors the existing inheritance pattern for shared metrics work (throughput, total work item age); intra-service helper, NOT an interface | ADR-021 |
| DDD-10 | ADR-018 disposition: UPHELD. No shared `IPerStateAggregationService`. The repository (`IWorkItemStateTransitionRepository`) IS the shared primitive across siblings. Sibling B3 writes its own service-layer method when it DESIGNs | Locked | Concrete query shapes (DDD-2 vs sibling B3 D12) make the semantic divergence visible; consolidation would conflate; ADR-021 |
| DDD-11 | ~~US-03 tooltip annotation~~ — **REMOVED 2026-05-25**. US-03 (in-flight dot pace annotation) was cut from scope. No tooltip changes, no client-side bucket computation, no use of `daysInState` for this feature | Removed | Feature scoped to colored bands + one toggle only; the colored column background carries the signal | — |
| DDD-12 | Endpoint route shape: `GET /api/teams/{teamId:int}/metrics/ageInStatePercentiles?startDate&endDate` and `GET /api/portfolios/{portfolioId:int}/metrics/ageInStatePercentiles?startDate&endDate`. Auth: existing `[RbacGuard(TeamRead)]` / `[RbacGuard(PortfolioRead)]`. Validation: `startDate.Date <= endDate.Date` (HTTP 400 otherwise), matching the existing `cycleTimePercentiles` validation | Locked | Mirrors the existing route convention precisely; no new top-level routes | DESIGN, no ADR |

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change Type | Change Summary |
|---|---|---|---|
| `AgeInStatePercentilesDto` (NEW DTO) | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/AgeInStatePercentilesDto.cs` | NEW | `record AgeInStatePercentilesDto(string State, IReadOnlyList<PercentileValue> Percentiles)`. Returned by the new endpoint. No `SampleSize` (low-sample messaging cut, DDD-4). |
| `ITeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ITeamMetricsService.cs` | EXTEND | Add `IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)`. |
| `IPortfolioMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IPortfolioMetricsService.cs` | EXTEND | Add `IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)`. |
| `BaseMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BaseMetricsService.cs` | EXTEND | Add `protected IEnumerable<AgeInStatePercentilesDto> ComputeAgeInStatePercentiles(IEnumerable<WorkItem> completedItemsInWindow, IEnumerable<string> doingStatesInWorkflowOrder, IReadOnlyList<int> requestedPercentiles)`. Per ADR-019 + D12 algorithm: walks transitions via `IWorkItemStateTransitionRepository.GetAllByPredicate(t => completedItemIds.Contains(t.WorkItemId))` (or equivalent SQL-friendly composition); for each completed item and each `Doing` state it exited, records the **cumulative total age at exit** `exitTransition.TransitionedAt − item.StartedDate`; buckets by state; computes percentiles via `PercentileCalculator`. |
| `TeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` | EXTEND | Implement `GetAgeInStatePercentilesForTeam` by: (1) `GetWorkItemsClosedInDateRange(team, startDate, endDate)` (existing); (2) call `ComputeAgeInStatePercentiles(...)`; (3) wrap in `GetFromCacheIfExists(team, $"AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () => …, logger)`. Default percentiles `[50, 70, 85, 95]` per DDD-3. Workflow state order from the team's existing `doingStates` (same source the chart's X axis uses). |
| `PortfolioMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PortfolioMetricsService.cs` | EXTEND | Same pattern as Team. The portfolio's "completed items in window" comes from the existing per-portfolio equivalent of `GetWorkItemsClosedInDateRange`. |
| `TeamMetricsController` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` | EXTEND | Add `[HttpGet("ageInStatePercentiles")] ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)`. Validation: `startDate.Date <= endDate.Date` (HTTP 400 with `StartDateMustBeBeforeEndDateErrorMessage`); auth via existing `[RbacGuard(TeamRead, ScopeIdRouteKey="teamId")]` if present at the class level (verify at GREEN). Implementation: `GetEntityByIdAnExecuteAction(teamRepository, teamId, team => teamMetricsService.GetAgeInStatePercentilesForTeam(team, startDate, endDate))`. |
| `PortfolioMetricsController` | `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioMetricsController.cs` | EXTEND | Mirror endpoint: `[HttpGet("ageInStatePercentiles")] ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentilesForPortfolio(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)`. |
| `IPercentileValue` (TS model) | `Lighthouse.Frontend/src/models/PercentileValue.ts` | NO CHANGE | Existing model is reused. The new model below composes it. |
| `IPerStatePercentileValues` (TS model — NEW) | `Lighthouse.Frontend/src/models/PerStatePercentileValues.ts` | NEW | `interface IPerStatePercentileValues { state: string; percentiles: IPercentileValue[]; }`. Mirrors backend DTO. No `sampleSize` (DDD-4). |
| `MetricsService` (TS) | `Lighthouse.Frontend/src/services/Api/MetricsService.ts` | EXTEND | Add `getAgeInStatePercentiles(id: number, startDate: Date, endDate: Date): Promise<IPerStatePercentileValues[]>` to both the `IMetricsService` interface and the `MetricsService` class. Implementation mirrors `getCycleTimePercentiles` (line 247) — GET `/${this.api}/${id}/metrics/ageInStatePercentiles?${this.getDateFormatString(startDate, endDate)}`. |
| `useMetricsData` (hook) | `Lighthouse.Frontend/src/hooks/useMetricsData.ts` | EXTEND | Add `perStatePercentileValues: IPerStatePercentileValues[]` to the returned ctx (default `[]`). In the same `useEffect` block that calls `metricsService.getCycleTimePercentiles` (~line 211), add a parallel call to `metricsService.getAgeInStatePercentiles(entity.id, startDate, endDate)`. Both calls share the same window state; both can run in parallel via `Promise.all` (software-crafter chooses shape at GREEN). |
| `BaseMetricsView` | `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` | EXTEND | Pass `perStatePercentileValues={ctx.perStatePercentileValues}` to the `<WorkItemAgingChart>` instance (currently line 788-794). |
| `WorkItemAgingChart` | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` | EXTEND | Add optional `perStatePercentileValues?: IPerStatePercentileValues[]` prop. Add a custom SVG **`<rect>` filled-zone** overlay inside `<ChartsContainer>` per ADR-020 + DDD-6 — rendered **before the dots** so it sits behind them; for each state with data, one stacked set of rects spanning `x ∈ [stateIndex−0.4, stateIndex+0.4]` and the consecutive percentile boundaries in Y (0→p50→p70→p85→p95→top), filled from a green→red scale keyed off `ForecastLevel`. Gate the whole overlay on a single `showPaceBands` boolean (off by default, DDD-8). No per-percentile visibility map, no tooltip annotation, no per-band hover tooltip (US-03 and per-band hover cut). |
| `PercentileLegend` | `Lighthouse.Frontend/src/components/Common/Charts/PercentileLegend.tsx` | EXTEND | Add a single optional chip: props `showPaceBands?: boolean`, `onTogglePaceBands?: () => void`, and a `paceBandsAvailable?: boolean` (render the chip only when at least one state has band data). One chip labeled **Pace percentiles**, off by default, styled consistently with the existing chips. No second chip group, no sub-headers, no low-sample notice (all cut). |
| `useChartVisibility` (hook) | `Lighthouse.Frontend/src/hooks/useChartVisibility.ts` | NO CHANGE (or trivial) | The single `showPaceBands` boolean is the simplest possible local `useState` in `WorkItemAgingChart`; it does not need the percentile-map machinery. Existing percentile/SLE visibility wiring is untouched. |
| Vitest + RTL tests for the new chart behaviour | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.test.tsx` + `PercentileLegend.test.tsx` | EXTEND | (a) No-regression / default-off test: render with `perStatePercentileValues` present but overlay not toggled → no band `<rect>`s, DOM matches existing snapshot; (b) band rendering test: render with a 2-state fixture and the overlay on → assert the expected `<rect>` elements at the expected x-span/y-boundaries with green→red fills, behind the dots; (c) toggle-chip test: chip off by default (no rects) → click → rects appear → click → gone; assert the existing percentile/SLE chips are unaffected throughout. |
| Vitest tests for `MetricsService` and `useMetricsData` extension | colocated `.test.ts` files | EXTEND | Mock `getAgeInStatePercentiles` resolves; assert `ctx.perStatePercentileValues` populated; assert parallel-fetch shape. |
| NUnit tests for `BaseMetricsService.ComputeAgeInStatePercentiles` | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceTests.cs` (or alongside `TeamMetricsServiceTests`) | NEW / EXTEND | Fixture with 20 completed items having known `StartedDate` + per-state transition timestamps across 3 states (`In Progress`, `Review`, `Test`); assert exact 50/70/85/95 **cumulative total-age-at-state-exit** values per state matching hand-computed expectations, and that band values rise left→right (D12). Boundary tests: empty state (omitted), single-observation state, multi-visit item (re-work). Visit-level (not item-level) sampling test per ADR-019 enforcement table. Cache-key shape test per ADR-019. |
| NUnit tests for `TeamMetricsController.GetAgeInStatePercentilesForTeam` | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/TeamMetricsControllerTest.cs` | EXTEND | HTTP 400 on inverted date range; HTTP 200 with empty body on team with no completed items; HTTP 200 with expected dto shape on populated fixture; `[RbacGuard]` test asserts unauthenticated returns 401/403 (matches the pattern used by `cycleTimePercentiles` tests at the same controller). |
| NUnit tests for `PortfolioMetricsController.GetAgeInStatePercentilesForPortfolio` | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/PortfolioMetricsControllerTests.cs` | EXTEND | Same coverage as Team. |
| ArchUnitNET test (NEW rule) | `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/` (existing suite) | EXTEND | No class or interface named `*PerStateAggregation*` exists in the feature commit set (ADR-021). The metrics-service classes read transitions only via `IWorkItemStateTransitionRepository`, never via `DbSet<WorkItemStateTransition>` (extends the ADR-015 rule). The `BaseMetricsService.ComputeAgeInStatePercentiles` helper is `protected` (intra-inheritance only). |
| E2E spec | `Lighthouse.EndToEndTests/tests/specs/flow/AgingPacePercentiles.spec.ts` (new file) | NEW | 1 happy-path scenario per skill DoD: open team detail → aging chart shows no bands by default → toggle the **Pace percentiles** chip on → colored per-state band rects appear behind the dots → toggle off → bands gone, existing percentile/SLE lines unchanged throughout. Per-state algorithm correctness lives in NUnit (faster, deterministic) per the established sibling-1 pattern. |

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
| GET | `/api/teams/{teamId:int}/metrics/wip` (and other endpoints carrying `WorkItemDto`) | Existing | NO CHANGE | This feature neither reads nor modifies `WorkItemDto` — the dropped US-03 dot annotation was its only would-be consumer (`CurrentStateEnteredAt` / `daysInState`). Bands are computed entirely server-side from transitions |

No new top-level routes. No premium gate. WS strategy Type A: additive throughout.

Response shape (both endpoints):

```
[
  { "state": "In Progress", "percentiles": [
      { "percentile": 50, "value": 3 },
      { "percentile": 70, "value": 5 },
      { "percentile": 85, "value": 8 },
      { "percentile": 95, "value": 14 } ] },
  { "state": "Review",      "percentiles": [
      { "percentile": 50, "value": 8 }, { "percentile": 70, "value": 13 },
      { "percentile": 85, "value": 18 }, { "percentile": 95, "value": 26 } ] },
  { "state": "Test",        "percentiles": [
      { "percentile": 50, "value": 12 }, { "percentile": 70, "value": 18 },
      { "percentile": 85, "value": 24 }, { "percentile": 95, "value": 33 } ] }
]
```

Each `value` is the **cumulative total age (in days) at the moment items left that state**
(D12), so values rise across states in workflow order — `Test`'s percentiles sit above
`Review`'s, which sit above `In Progress`'s — matching the chart's total-age Y axis. States
are returned in workflow order (matching the X-axis order). States with zero observations are
omitted; the FE renders no band for them. No `sampleSize` field (DDD-4).

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
| DDD-1 | Visit-level percentile sampling of **cumulative total-age-at-state-exit** (`exit − StartedDate`), per D12 | ADR-019 |
| DDD-2 | Item-membership rule: `ClosedDate ∈ window` (mirrors `cycleTimePercentiles`) | ADR-019 |
| DDD-3 | Percentile function: existing `PercentileCalculator` with defaults 50/70/85/95 | ADR-019 |
| DDD-4 | Empty at API: emit when ≥1, omit when 0; no `sampleSize`, no low-sample messaging | ADR-019 |
| DDD-5 | Cache key shape and invalidation reuse the `cycleTimePercentiles` pattern | ADR-019 |
| DDD-6 | Per-state band rendering: custom SVG `<rect>` filled-zone overlay (green→red) inside `<ChartsContainer>`, behind the dots | ADR-020 |
| DDD-7 | Chart prop interface: optional `perStatePercentileValues` → backwards-compatible | ADR-020 |
| DDD-8 | Legend: a single `showPaceBands` boolean toggle (one chip), off by default | ADR-020 |
| DDD-9 | Backend computation: `protected` helper inside `BaseMetricsService` | ADR-021 |
| DDD-10 | ADR-018 disposition: UPHELD — no `IPerStateAggregationService` | ADR-021 |
| DDD-11 | ~~US-03 tooltip~~ — REMOVED 2026-05-25 (dot annotation cut) | — |
| DDD-12 | Endpoint route shape mirrors `cycleTimePercentiles` precisely | DESIGN, no ADR (mechanical) |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WorkItemAgingChart` | `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` | Already renders the dot scatter, the existing CT band lines (line 349-364), the X-axis state columns (line 287-298), the SLE line, the per-type filter chips, and integrates with `WorkItemsDialog` on dot click | EXTEND | The new per-state bands are conceptually "alongside the existing CT bands"; per DISCUSS D6 + ADR-020, they share the same chart, the same coordinate system, the same legend strip, the same tooltip surface. A new sibling widget would duplicate every one of these and lose the "alongside" semantics. Net add: ~30 LOC for the filled-`<rect>` overlay + ~10 LOC for the prop + ~5 LOC for the single toggle chip + `showPaceBands` state. |
| `PercentileLegend` | `Lighthouse.Frontend/src/components/Common/Charts/PercentileLegend.tsx` | Already renders a chip group with `(percentiles, visiblePercentiles, onTogglePercentile)` for the existing CT bands; supports an optional SLE chip | EXTEND | Adding ONE new **Pace percentiles** toggle chip alongside the existing chips is structurally consistent with the existing component shape. No second chip group, no sub-headers. Net add: ~8 LOC. |
| `useChartVisibility` | `Lighthouse.Frontend/src/hooks/useChartVisibility.ts` | Already manages `visiblePercentiles: Record<number, boolean>` and `visibleTypes`; supports SLE visibility toggle | NO CHANGE (DDD-8) | The single `showPaceBands` boolean is a trivial local `useState` in `WorkItemAgingChart` — it does not need the percentile-map machinery, and the existing percentile/SLE wiring stays untouched. |
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
| `WorkItem.CurrentStateEnteredAt` | sibling 1 ADR-016 | Persisted column on `WorkItem` | NOT USED | Was only needed by the dropped US-03 dot annotation (`daysInState` bucket). With US-03 cut, this feature does not read it. |
| `WorkItem.ClosedDate`, `WorkItem.StartedDate`, `GetWorkItemsClosedInDateRange` | `WorkItemBase` + `BaseMetricsService` | Existing persistence + existing predicate | REUSE AS-IS | Item-membership rule per ADR-019 DDD-2 reuses this predicate exactly. |
| `ForecastLevel(percentile).color` palette | `Lighthouse.Frontend/src/components/Common/Forecasts/ForecastLevel.ts` (already imported by `WorkItemAgingChart`) | Already provides the per-percentile color for the existing CT bands (green→red across 50→95) | REUSE AS-IS | The filled band zones reuse this same green→red palette per percentile boundary, so the colored background reads consistently with the existing percentile lines (below-50 green … above-95 red). |
| MUI-X `<ChartsContainer>` | `@mui/x-charts` (existing dependency) | Provides the chart coordinate system; supports arbitrary SVG children | REUSE AS-IS | The custom `<line>` overlay (ADR-020) is rendered as a child of the existing `<ChartsContainer>`. No new chart library; no new chart container. |

**Totals: 17 rows. 6 EXTEND + 11 REUSE-AS-IS / NO-CHANGE + 0 CREATE-NEW at the OVERLAP level.** (The NEW entries — `AgeInStatePercentilesDto`, `IPerStatePercentileValues` TS model, the new E2E spec, the new test methods, the new ArchUnit rules — appear in the Component decomposition; they have ZERO existing overlap per the grep below.)

CODEBASE GREP FOR OVERLAP CANDIDATES THAT MIGHT JUSTIFY CREATE-NEW BUT WERE REJECTED:

- Searched for `AgeInState|ageInState|PerStatePercentile|perStatePercentile` across `Lighthouse.Backend` + `Lighthouse.Frontend/src` — zero production matches. No existing DTO, service, hook, or component duplicates the per-state percentile concern.
- Searched for `PerState|StateAggregation|PerStateMetric` — zero production matches. Confirms ADR-018 + ADR-021's "no existing service to extend" position.
- Searched for `BandSegment|ChartBand|PercentileBand` — zero production matches. The new SVG `<line>` overlay (ADR-020) is the first per-state band rendering in the codebase.

## Wave: DESIGN / [REF] Open questions

- **MUI-X coordinate-system access from custom SVG children**: ADR-020 assumes that an SVG `<rect>` rendered as a child of `<ChartsContainer>` receives the same coordinate system as `<ChartsReferenceLine>` (i.e. `x`/`y` are in data space, not pixel space) so the band can be placed at `x ∈ [stateIndex−0.4, stateIndex+0.4]` and between percentile-value Y boundaries. This is consistent with MUI-X's documented API but is a small interop assumption. **Validation deferred to DELIVER spike (~30 min)**: confirm at the start of implementation. If false, the alternative is an `<svg>` overlay positioned via `getBoundingClientRect()` math on the chart container — heavier, but still feasible. No DESIGN blocker.
- **Endpoint profiling at scale**: ADR-019's algorithm is `O(transitions_per_item × completed_items_in_window)`. For a team with 200 completed items × 12 transitions each over a 6-month window, ~2400 row-level operations per request. Expected to be sub-100ms uncached. **Validation deferred to DELIVER spike (slice 01, ~30 min per slice-01 spec)**: profile against a team with 6 months of transition data on the project's own ADO instance. If profiling shows materially worse performance, the existing `GetFromCacheIfExists` already handles repeat requests; a one-step materialisation (e.g. precompute per (team, window) at sync time) is a follow-up — NOT in scope for MVP.
- ~~`useChartVisibility` shape (extend signature vs. invoke twice)~~: **RESOLVED 2026-05-25.** The overlay is now a single `showPaceBands` boolean, so no `useChartVisibility` change is needed at all — a trivial local `useState` in `WorkItemAgingChart` suffices.
- **Linear connector transition timestamps**: depend on sibling 1's per-connection runtime downgrade behaviour (ADR-017). When Linear's `history` field is unavailable, the transitions for that team are sync-cadence approximations — the per-state bands for that team will be band-heights derived from approximate timestamps. **Resolution**: no change required by this DESIGN. The sibling's `WorkItemDto.Approximate` flag already indicates the badge tooltip on the in-flight side. For the historical band side, the approximation is inherited from the transition data; no separate annotation needed (a band derived from 10 approximate-timestamp items is not meaningfully more wrong than the same band derived from 10 exact items). Documented as an inherited characteristic, not a feature gap.
- **Backfill of pre-feature transitions**: explicitly OUT OF SCOPE per DISCUSS (forward-only constraint inherited from sibling 1). First weeks post-release will have empty bands for states that have no completed-item transitions yet — those columns simply render no band (no low-sample messaging; that was cut). **Resolved by DISCUSS**; no DESIGN action.

## Wave: DESIGN / [REF] Cross-MVP coordination outcomes

- **ADR-018 (sibling 1) disposition: UPHELD.** This DESIGN does NOT introduce `IPerStateAggregationService`. Per-state percentile computation lives as a `protected` helper inside `BaseMetricsService`, consumed only by `TeamMetricsService` / `PortfolioMetricsService` via inheritance. Repository-level sharing (sibling 1's `IWorkItemStateTransitionRepository`) IS the architectural seam across siblings. Rationale captured in ADR-021.
- **No new flag-ups for sibling B3 (`state-time-cumulative-view`) DESIGN.** B3's DESIGN author retains full freedom to either (a) follow the same Path A pattern this feature establishes (independent computation), or (b) supersede both ADR-018 and ADR-021 with a new ADR that argues for consolidation in light of B3's concrete query shape. ADR-021's "Reviewer note (cross-MVP)" section flags both options for B3's DESIGN author.
- **No upstream impact on sibling 1 DESIGN.** D11 of DISCUSS held: no schema additions to `WorkItemStateTransition`. ADR-019's algorithm reads only the 4 fields shipped by sibling 1 plus `WorkItem.CurrentStateEnteredAt` (read-only) plus `WorkItem.ClosedDate` (read-only, unchanged). Zero feedback to sibling 1.
- **Semantic divergence vs sibling B3 made permanent in writing.** ADR-019 explicitly contrasts this feature's "ClosedDate-in-window + unclipped visit-level duration" rule with sibling B3's "frame-intersection + unclipped item-level duration including in-flight" rule. The two endpoints are intentionally named differently (`ageInStatePercentiles` vs B3's `cumulativeStateTime`) and ADR-021's enforcement rules prevent silent helper consolidation.

## Wave: DESIGN / [REF] Wave decisions summary

**Primary architectural commitment**: introduce one new endpoint per scope (team + portfolio), one chart prop extension, one custom SVG overlay inside the existing chart's `<ChartsContainer>`, and one `protected` helper inside `BaseMetricsService`. Every other touchpoint is an additive extension of an existing class / hook / component / DTO / cache key namespace. NO new top-level routes. NO new external integration. NO new external library. NO new persistence. NO premium gate. NO breaking change.

**Architecture pattern**: ports-and-adapters / hexagonal (unchanged). Reads transitions via the sibling 1 repository port; no new port introduced.

**Walking skeleton path** (validates the architecture in one slice end-to-end): existing sibling-1 `WorkItemStateTransition` rows → new `BaseMetricsService.ComputeAgeInStatePercentiles` helper → new `TeamMetricsService.GetAgeInStatePercentilesForTeam` method → new `TeamMetricsController.GetAgeInStatePercentilesForTeam` endpoint → new `MetricsService.getAgeInStatePercentiles` TS method → extended `useMetricsData` ctx → extended `WorkItemAgingChart` prop → filled-`<rect>` overlay (gated by the **Pace percentiles** chip) rendering on both the team and portfolio detail pages. One slice exercises every architectural seam end-to-end.

**Sibling MVP coordination**: ADR-018 upheld (ADR-021); semantic divergence vs sibling B3 documented permanently in ADR-019; sibling 1's DESIGN unchanged (no schema feedback, no port feedback).

**Downstream changes propagated upstream**: NONE. Sibling 1 DESIGN unchanged. Sibling B3 DESIGN unaffected.

**Handoff readiness**: ready for nw-acceptance-designer (DISTILL) handoff. The 3 user stories with their AC are already locked at DISCUSS; this DESIGN adds the architectural decomposition the acceptance designer needs to write executable specs against. Subsequent nw-platform-architect (DEVOPS) handoff: no CI workflow changes required (the existing test gates cover the new code; the existing `ci_verifysqlite.yml` + `ci_verifypostgres.yml` cover the no-new-migration case).

## Wave: DESIGN / [REF] Outcome Collision Check

`nwave-ai outcomes check-delta docs/feature/aging-pace-percentiles/feature-delta.md` was NOT executed: the tool (`nwave-ai`) is not installed in this repository and the canonical outcomes registry at `docs/product/outcomes/registry.yaml` does not exist. Per skill ("skip-and-document"): documented here, deferred to DEVOPS handoff for KPI / outcomes registration alongside the DISCUSS-defined `OUT-aging-pace-bands-rendered`, `OUT-aging-pace-legend-toggled`, `OUT-aging-pace-parity-claim`.

