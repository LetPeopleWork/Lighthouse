<!-- markdownlint-disable MD024 -->
# Feature Delta: Cumulative State-Time Completion Filter

ADO Story #5144 — "Allow to hide Completed/Ongoing Items in Cumulative Time in State Chart".
Tier-1 [REF] DISCUSS output. Brownfield additive refinement to the SHIPPED `state-time-cumulative-view` feature.
Density: lean + ask-intelligent. No expansion triggers fired (see Wave-Decisions) — no expansion menu emitted.

---

## Wave: DISCUSS / [REF] Personas & Jobs

Single primary persona; no new job discovered (Decision D4-locked). This story REFINES two existing,
already-validated jobs in `docs/product/jobs.yaml` (both `feature_context: state-time-cumulative-view`).
A `refined_by: [cumulative-state-time-completion-filter]` trace was added to each; the new
`feature_context` entry was added to the top list. No new job entry was created.

- **Primary persona**: `delivery-lead-rte` (delivery lead / Release Train Engineer).
  - **Job (primary)**: `job-delivery-lead-spot-workflow-constraint` — *"…see which workflow state has consumed the most of our collective time… so I can name the constraint with evidence."* This refinement lets the lead isolate the **in-flight-only** picture ("where is time piling up RIGHT NOW?") or the **completed-only** picture ("where did finished work historically spend its time?") by toggling one segment off — sharpening the constraint read without exporting to a spreadsheet.
- **Secondary persona**: `product-owner`.
  - **Job (secondary)**: `job-po-deep-dive-item-state-time` — single-item / one-feature deep-dive via the item picker. This refinement deliberately **does not change** that job: when an item is picked, the completion legend toggle is suppressed (Decision D6), so the narrowed deep-dive view always shows both segments and stays exactly as shipped.

---

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | Feature type | User-facing UI refinement: a toggle on an existing chart. |
| D2 | Walking skeleton | **NO** — brownfield additive extension to a shipped chart; nothing new to wire end-to-end. |
| D3 | UX research depth | Lightweight — one small interaction on one chart. |
| D4 | JTBD | Do **not** discover a new job. Trace to the two existing jobs above (N:1). jobs.yaml edited minimally: top `feature_context` + a `refined_by` line per job. |
| D5 | Control mechanism | Click-to-toggle **Completed** and **Ongoing** chips, **reusing the existing `LegendChip` + `useTypesVisibility` mechanic** (the same show/hide chips the cycle-time scatterplot uses for work-item types). Both shown by default; click dims/hides that series, click again restores. |
| D6 | Suppression rule | The completion toggle is available **only when no work item is selected** (`selectedItemIds.length === 0`). When an item is picked, the chips are suppressed AND any prior hide-state is reset so the narrowed view always shows both segments. |
| D7 | Semantics | **Pure client-side segment hide.** No backend recompute, no new API param, no DTO change. Hiding a class = hiding that stacked-bar segment; the y-axis rescales to the remaining series. Rationale: the per-state Mean/Median/Items aggregates live in a hidden `<Box sx={{display:"none"}}>` (test-only DOM) and are never user-visible, so there is no honesty obligation to keep them in sync. |
| D8 | Reuse decision (was a DESIGN flag) | MUI X Charts v9 (`@mui/x-charts@9.0.1`) has no native legend-click series toggle — but a proven in-house mechanic already exists and is **reused verbatim**: `LegendChip` (`components/Common/Charts/LegendChip.tsx`) for the clickable chip and `useTypesVisibility<T>` (`hooks/useChartVisibility.ts`) for the visibility state. Call `useTypesVisibility(["Completed","Ongoing"])`; render two `LegendChip`s wired to `visibleTypes`/`toggleTypeVisibility`; pass only the visible series to `<BarChart>`. The hook's built-in **"can't hide the last visible type" guard** makes "hide both" impossible by construction (prevents an empty chart). This supersedes the earlier "build a custom legend" flag and removes the open DESIGN risk. |

---

## Wave: DISCUSS / [REF] User Stories

One primary value story. The suppression interaction (D6) is folded into this story's ACs rather than
split out — it is a guard condition on the same single interaction, not an independent user outcome.

### US-5144-01: Isolate completed-only or ongoing-only time in the Cumulative Time per State chart

**job_id**: `job-delivery-lead-spot-workflow-constraint` (refined). Secondary trace: `job-po-deep-dive-item-state-time`.

#### Elevator Pitch

- **Before**: Priya Raman (delivery lead, Team Aurora) opens *Cumulative Time per State* and sees each state's bar split into a solid "Completed" segment and a hatched "Ongoing" segment stacked together. To answer "where is our in-flight work piling up right now?" she has to mentally subtract the completed portion from every bar — error-prone and not screenshot-able for her retro deck.
- **After**: Priya clicks the **"Completed"** legend entry on the chart. The solid segments vanish, every bar shrinks to its ongoing-only height, and the y-axis rescales so the in-flight distribution fills the view — a clean, screenshot-able "where in-flight time sits today" chart. Clicking **"Completed"** again restores both segments.
- **Decision enabled**: Priya decides *which workflow state to target for improvement effort this quarter* by reading the constraint from the isolated in-flight view (or the completed-only history view) instead of guessing across stacked segments.

#### Problem

Priya Raman is a delivery lead who, in retro and improvement-planning meetings, needs to read the
systemic constraint from the *Cumulative Time per State* chart. She finds it tedious to separate
"time finished work spent per state" from "time in-flight work is still consuming" because both are
stacked in every bar — she ends up eyeballing the hatched vs solid portions or rebuilding the split
in a spreadsheet.

#### Who

- `delivery-lead-rte` | preparing a retro / improvement-planning / leadership review | wants to name the constraint with evidence, not anecdote.
- `product-owner` (secondary) | doing a single-item or one-feature deep-dive via the picker | this story must leave that flow unchanged.

#### Solution

Reuse the existing chart show/hide mechanic — `LegendChip` + `useTypesVisibility` — to add two
togglable chips, "Completed" and "Ongoing", to `CumulativeStateTimeChart` (the identical pattern the
cycle-time scatterplot uses for work-item-type filtering). Both active by default. Clicking a chip
hides that stacked series client-side; the chart rescales to the remaining series. The chips are
rendered (and hide-state honoured) **only when no work item is selected** in the item picker; when a
selection exists the chips are suppressed and any prior hide-state is reset. The hook's "can't hide
the last visible type" guard prevents hiding both. No backend or API change. Applies on both team and
portfolio detail.

#### Domain Examples

##### 1. Happy path — isolate ongoing-only (Priya Raman, Team Aurora)

Priya opens Team Aurora's flow metrics for Q2. The chart shows "Doing" with a tall stacked bar
(Completed 12d solid + Ongoing 20d hatched) and "Review" (Completed 6d + Ongoing 4d). She clicks
**"Completed"** in the legend. The solid segments disappear; "Doing" now reads 20d, "Review" 4d, and
the y-axis top rescales from 32d to 20d. She screenshots the in-flight-only chart for her retro.

##### 2. Edge / restore + completed-only (Priya Raman)

From the same view Priya clicks **"Completed"** again — both segments return. She then clicks
**"Ongoing"**: the hatched segments vanish, "Doing" reads 12d completed-only, "Review" 6d, y-axis
rescales to the largest completed value. She has flipped between "where in-flight time sits" and
"where finished work historically spent time" with two clicks.

##### 3. Boundary — suppression when an item is picked (Marco Bianchi, product owner)

Marco is deep-diving feature ABC-204's slow story via the item picker (`selectedItemIds = [88412]`).
Because a work item is selected, the **Completed/Ongoing legend toggle is not shown** — the picker
selection is the only filter and the chart shows both segments for that single item. Marco then
clears the picker; the legend reappears with both entries active (no stale hide carried over).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Both completion classes shown by default
  Given Priya opens Team Aurora's Cumulative Time per State chart with no work item selected
  Then each state bar shows its Completed segment and its Ongoing segment stacked together
  And both the "Completed" and "Ongoing" legend entries appear active

Scenario: Hiding completed isolates the ongoing-only picture
  Given Priya is viewing the chart with both segments shown and no work item selected
  When Priya clicks the "Completed" legend entry
  Then the Completed segment is removed from every state bar
  And each bar's height reflects ongoing time only
  And the chart's value axis rescales to the largest remaining ongoing value

Scenario: Hiding ongoing isolates the completed-only picture
  Given Priya is viewing the chart with both segments shown and no work item selected
  When Priya clicks the "Ongoing" legend entry
  Then the Ongoing segment is removed from every state bar
  And each bar's height reflects completed time only
  And the chart's value axis rescales to the largest remaining completed value

Scenario: Re-clicking a hidden class restores it
  Given Priya has hidden the Completed segment
  When Priya clicks the "Completed" legend entry again
  Then the Completed segment reappears in every state bar
  And both segments are shown stacked as before

Scenario: The completion toggle is suppressed while a work item is selected
  Given Marco has selected work item 88412 in the item picker
  When Marco views the Cumulative Time per State chart
  Then no Completed/Ongoing legend toggle is offered
  And both segments are shown for the selected item

Scenario: Selecting a work item resets any prior hide-state
  Given Priya has hidden the Completed segment with no work item selected
  When Priya selects a work item in the item picker
  Then the chart shows both segments for the narrowed selection
  And clearing the selection restores the legend with both entries active

Scenario: The toggle behaves identically on portfolio detail
  Given Priya opens a portfolio's Cumulative Time per State chart with no work item selected
  When Priya clicks the "Ongoing" legend entry
  Then the Ongoing segment is removed from every state bar
  And the value axis rescales to the remaining completed values
```

#### Acceptance Criteria

- [ ] (a) With no item selected, both Completed and Ongoing segments render by default on every bar, and both legend entries are active. *(Scenario 1)*
- [ ] (b) Clicking "Completed" removes the completed segment everywhere; bars and value axis reflect ongoing-only. *(Scenario 2)*
- [ ] (c) Clicking "Ongoing" removes the ongoing segment everywhere; bars and value axis reflect completed-only. *(Scenario 3)*
- [ ] (d) Re-clicking a hidden entry restores that segment. *(Scenario 4)*
- [ ] (e) When `selectedItemIds.length > 0`, the legend toggle is absent/disabled and both segments show. *(Scenario 5)*
- [ ] (f) Making a selection resets any prior hide-state; clearing the selection restores the legend with both entries active. *(Scenario 6)*
- [ ] (g) Behaviour is identical on team detail and portfolio detail. *(Scenario 7)*
- [ ] Guardrail: no network request is issued when toggling a legend entry (pure client-side). *(verifies D7)*

#### Outcome KPIs

- **Who**: delivery leads / RTEs and product owners viewing the Cumulative Time per State chart.
- **Does what**: use the completion-class toggle to read an isolated (ongoing-only or completed-only) constraint view at least once per chart session where they engage the chart.
- **By how much**: ≥ 25% of chart-engaged sessions exercise the toggle within 60 days of release (adoption signal; honest target for a small refinement).
- **Measured by**: existing frontend analytics funnel events (the project already emits funnel/analytics events — see `adr-037-analytics-funnel-events.md`); add a single `cumulative_state_time_legend_toggle` event. If instrumentation is not free, fall back to qualitative confirmation from 3 delivery leads in the next retro cycle.
- **Baseline**: 0% (toggle does not exist today).

#### Technical Notes (constraints / dependencies)

- **Reuse (D8)**: MUI X Charts v9 has no native legend-click series toggle, so reuse the in-house mechanic instead of building one — `LegendChip` (`components/Common/Charts/LegendChip.tsx`) + `useTypesVisibility(["Completed","Ongoing"])` (`hooks/useChartVisibility.ts`), exactly as `CycleTimeScatterPlotChart.tsx` (lines 268-273) does for work-item types. Pass only the series whose `visibleTypes[label] !== false` to `<BarChart>`. The hook prevents hiding the last visible type (no empty chart).
- Host wiring (verified): hide-state lives in `BaseMetricsView.tsx` alongside `selectedItemIds` (declared line 1049); it must reset when `setSelectedItemIds` makes a non-empty selection. `displayedCumulativeStateTime = narrowedCumulativeStateTime ?? cumulativeStateTime` (lines 1280-1281) is unchanged; the toggle filters the *series passed to the chart*, not the data fetched.
- Chart component to extend: `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.tsx` (two stacked series: `completedContributionDays` "Completed (<unit>)" solid `theme.palette.primary.main`; `ongoingContributionDays` "Ongoing (<unit>)" hatch `url(#cumulative-state-time-ongoing-hatch)`; stack `"stateTime"`).
- No change to `ICumulativeStateTimeStateRow` / response model (`models/Metrics/CumulativeStateTime.ts`).
- The hidden `display:none` tooltip Box is test-only and never user-visible — out of scope to change (D7).
- **RBAC**: no authorization effect (see cross-cutting section).

---

## Wave: DISCUSS / [REF] Definition of Done (9-item)

1. All 7 UAT scenarios pass as automated tests (Vitest + React Testing Library; live behaviour verified on team and portfolio detail).
2. Both default-shown, hide, restore, and rescale behaviours work for Completed and Ongoing.
3. Suppression + hide-state reset on item selection verified.
4. Code refactored, no debt; Biome clean; `pnpm build` zero warnings.
5. Frontend mutation testing (Stryker) ≥ 80% kill on the new toggle/legend surface.
6. Reviewed and approved; merged to `main`; CI green.
7. Demoable: toggle Completed → ongoing-only chart, restore, toggle Ongoing, then pick an item to show suppression.
8. No new/changed API contract; confirmed no network call on toggle.
9. Outcome KPI instrumentation (`cumulative_state_time_legend_toggle` event) added or the qualitative fallback explicitly chosen.

### Out of Scope (explicit)

- No backend changes; no new/changed API param; no DTO change.
- No change to the (non-visible) per-state Mean/Median/Items aggregates — there is nothing visible to keep honest (D7).
- No persistence of the toggle across page reloads (session-local only) unless trivially free; persistence is **not** required.
- The item picker's own behaviour is unchanged — this story only *reads* `selectedItemIds` to gate the legend.
- No third "Total"/combined toggle, no per-state hiding, no chronology/timeline lens.

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**N/A — brownfield.** The `state-time-cumulative-view` feature is already shipped end-to-end (chart,
data fetch, item picker, RAG, drill-down all live on team + portfolio detail). This story adds a single
client-side interaction to an existing, working flow; there is no new end-to-end path to skeleton.

---

## Wave: DISCUSS / [REF] Driving Ports

**None new — frontend-only.** Existing endpoints are reused unchanged:

- `getCumulativeStateTimeForTeam(teamId, [itemIds])` and the portfolio equivalent — already called by
  `BaseMetricsView.tsx`. The toggle does **not** call them; it filters which series the already-fetched
  response renders. No new driving port, no new adapter, no contract change.

---

## Wave: DISCUSS / [REF] Pre-requisites

- `state-time-cumulative-view` feature is **SHIPPED** (chart, series, item picker, host wiring all in
  `main`). This story builds directly on it.
- No data migration, no config, no feature flag required.

---

## Wave: DISCUSS / [REF] Cross-Cutting Impact (mandatory — all three answered)

- **RBAC — N/A (no new authorization).** The toggle is a client-side view preference on an existing
  chart whose visibility already follows team/portfolio metrics gating and premium gating (`isPremium`
  in `BaseMetricsView`). It changes no data access, fetches nothing new, and never touches
  `IRbacAdministrationService`. No `useRbac()` gating change. Reason: hiding a rendered series is a
  pure presentation concern over data the user is already authorized to see.
- **Lighthouse-Clients (CLI + MCP) — N/A.** No API contract change: pure frontend, no new/changed
  endpoint, no DTO change, no server-version dependency. The CLI/MCP clients are unaffected. **Revisit
  this verdict only if** DESIGN deviates from the locked client-side semantics (D7) and introduces a
  backend param — it should not, by design.
- **Website — N/A.** A minor UX refinement to an existing chart, not a new marketable premium
  capability. Nothing to surface or market on the public website. (The chart itself is already
  premium-gated; that positioning is unchanged.)

---

## Wave: DISCUSS / [REF] Definition of Ready Validation

### Story: US-5144-01

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1. Problem statement clear, domain language | PASS | Priya Raman wastes effort mentally subtracting completed from ongoing segments in retro prep. |
| 2. Persona with specific characteristics | PASS | `delivery-lead-rte` preparing a retro; secondary `product-owner` on item deep-dive. |
| 3. 3+ domain examples with real data | PASS | Priya/Team Aurora (Doing 12d+20d, Review 6d+4d); restore + completed-only; Marco/feature ABC-204/item 88412. |
| 4. UAT in Given/When/Then (3-7) | PASS | 7 scenarios covering default, hide-completed, hide-ongoing, restore, suppression, reset, portfolio. |
| 5. AC derived from UAT | PASS | AC (a)-(g) + guardrail map 1:1 to scenarios. |
| 6. Right-sized (1-3 days, 3-7 scenarios) | PASS | 7 scenarios, ≤1 day, frontend-only single interaction. |
| 7. Technical notes: constraints/deps | PASS | D8 MUI-v9 custom-legend flag; host wiring lines cited; cross-cutting RBAC/Clients/Website all answered. |
| 8. Dependencies resolved or tracked | PASS | Depends on shipped `state-time-cumulative-view`; no open dependency. |
| 9. Outcome KPIs with measurable target | PASS | ≥25% toggle adoption in chart-engaged sessions / 60 days, via funnel event (qualitative fallback). |

### DoR Status: PASSED

---

## Wave: DISCUSS / [REF] Wave-Decisions Summary

- **Scope Assessment (Elephant Carpaccio): PASS** — 1 story, 1 bounded context (frontend chart component + host), estimated ≤1 day. Right-sized; no split needed.
- **Expansion triggers evaluated — NONE fired.** Single story, single context, single primary persona, frontend-only refinement. No expansion menu emitted (per ask-intelligent rules).
- **JTBD**: no new job (D4); traced N:1 to two existing jobs; jobs.yaml edited minimally (top `feature_context` + `refined_by` per job).
- **WS**: N/A (brownfield). **Driving ports**: none new. **Cross-cutting**: RBAC / Clients / Website all N/A with stated reasons.
- **Key reuse decision (D8)**: MUI X Charts v9 has no native legend-click toggle → reuse the in-house `LegendChip` + `useTypesVisibility(["Completed","Ongoing"])` mechanic (same as the cycle-time scatterplot), not a bespoke control. Removes the open DESIGN risk; the hook's "can't hide the last visible type" guard prevents an empty chart.
- **Risk (low)**: if a backend param is ever introduced, the client-side semantics (D7) and the Lighthouse-Clients N/A verdict both break — flagged to keep the toggle client-side.
- Peer review (`nw-product-owner-reviewer`) pending before `*handoff-design`.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#D5 | Click-to-toggle Completed/Ongoing via reused `LegendChip` chips, both shown by default | D8 | Acceptance tests assert chips render + toggle filters the series passed to `<BarChart>` |
| DISCUSS#D6 | Toggle available only when no work item selected; hide-state resets on selection | n/a | Scenarios gate chips on the `completionFilterEnabled` prop and assert reset across enable/disable |
| DISCUSS#D7 | Pure client-side segment hide; no backend, no network call on toggle | n/a | Guardrail: tests assert the chart filters already-fetched series; no fetch is triggered |

---

## Wave: DISTILL / [REF] Test approach (repo idiom, not pytest-bdd)

This is the Lighthouse product repo (React + TS). Acceptance tests are **Vitest + React Testing Library**, co-located as `*.test.tsx` per the existing convention — NOT pytest-bdd `.feature` files. The driving surface is the `CumulativeStateTimeChart` React component (and its `BaseMetricsView` host); there is no backend, CLI, HTTP endpoint, or new driven adapter, so the Python DISTILL machinery (`nwave-ai` outcomes registry, `__SCAFFOLD__` `src/` stubs, `assert_state_delta`, polyglot state-delta port) does not apply. The methodology that DOES apply — acceptance tests authored RED before DELIVER, business-behaviour assertions through the public component surface, error/guard coverage, and the fail-for-right-reason gate — is honoured below.

---

## Wave: DISTILL / [REF] Scenario list with tags

SSOT for executable scenarios: the `describe.skip("…US-5144-01")` block in
`Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.test.tsx`.

| # | Scenario | Maps to AC | Tags | State |
|---|----------|-----------|------|-------|
| 1 | Both chips active + both segments by default (filter enabled) | a | `@US-5144` `@in-memory` | skip (RED-pending) |
| 2 | Click Completed → completed segment removed, ongoing-only series | b | `@US-5144` `@in-memory` | skip (RED-pending) |
| 3 | Click Ongoing → ongoing segment removed, completed-only series | c | `@US-5144` `@in-memory` | skip (RED-pending) |
| 4 | Re-click restores the hidden segment | d | `@US-5144` `@in-memory` | skip (RED-pending) |
| 5 | Keeps at least one segment visible when the user tries to hide both | — (D8 guard) | `@US-5144` `@in-memory` `@error` | skip (RED-pending) |
| 6 | Filter disabled (item picked) → no chips, both segments | e | `@US-5144` `@in-memory` | skip (passes today as invariant) |
| 7 | Disable→enable resets prior hide-state | f | `@US-5144` `@in-memory` | skip (RED-pending) |

AC (g) team/portfolio parity is **structural** — both pages render the same `CumulativeStateTimeChart`, so the component-level scenarios cover both surfaces by construction. Verified live in DELIVER per DoD item 1 (no separate Playwright scenario added, per E2E-minimalism).

---

## Wave: DISTILL / [REF] WS strategy & adapter coverage

- **Walking Skeleton: N/A** — brownfield; the end-to-end chart path is already shipped and green. No `@walking_skeleton` scenario required.
- **Driven adapters: none** — frontend-only; no new I/O. The toggle filters an already-fetched response in-memory. No `@real-io` adapter scenario is applicable (nothing new crosses a process/network boundary). Guardrail scenario asserts the toggle issues **no** network request (D7).
- **Driving surface**: `CumulativeStateTimeChart` rendered via RTL with the existing `@mui/x-charts` `BarChart` mock that exposes `data-series`; chips exercised via `@testing-library/user-event`.

---

## Wave: DISTILL / [REF] Scaffolds & test placement

- **Production scaffold (minimal, RED-ready)**: added optional `completionFilterEnabled?: boolean` to `CumulativeStateTimeChartProps` in `CumulativeStateTimeChart.tsx` — accepted but not yet wired, so the test file type-checks and the scenarios fail at runtime for the right reason (chips/filtering missing). DELIVER wires it (render `LegendChip`s + `useTypesVisibility`, filter `series`). No `src/` stub module is created (the component already exists).
- **Test placement**: `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.test.tsx` (co-located beside the component, matching every sibling chart test). New `describe.skip` block appended; `userEvent` import added.

---

## Wave: DISTILL / [REF] Fail-for-the-right-reason gate

Ran the new block un-skipped once before handoff:

- **6 / 7 scenarios → `MISSING_FUNCTIONALITY`** (correct RED): every failure is *"Unable to find button 'Completed visibility toggle'"* — the `LegendChip` toggle + series filtering are unimplemented. No `IMPORT_ERROR` / `FIXTURE_BROKEN` / `SETUP_FAILURE`; no assertion couples to internal state (series asserted via the public `BarChart` `data-series`, the same observable the existing tests use).
- **1 / 7 (scenario 6)** passes today as a guard invariant (filter disabled → no chips, both series) — acceptable; it protects against accidentally rendering chips when an item is picked.
- Block then set to `describe.skip`; full file green (9 passed, 7 skipped), Biome clean, `tsc` clean for the touched files. DELIVER un-skips one scenario per RED→GREEN cycle.

---

## Wave: DISTILL / [REF] Pre-requisites

- Reuse targets exist in `main`: `LegendChip` (`components/Common/Charts/LegendChip.tsx`), `useTypesVisibility` (`hooks/useChartVisibility.ts`), the `BarChart` test mock + `getMockStateRow` factory in the chart test file.
- Shipped `state-time-cumulative-view` chart, series, item picker, and `BaseMetricsView` host wiring (`selectedItemIds`, `displayedCumulativeStateTime`).
- No DEVOPS environment matrix needed (no infra change). No DESIGN driving-port doc needed (no backend boundary; component surface is the port).

---

## Wave: DISTILL / [REF] DELIVER handoff notes

1. Add `useTypesVisibility(["Completed","Ongoing"])` inside `CumulativeStateTimeChart`; render two `LegendChip`s in the header `Stack` (beside `pickerSlot`) only when `completionFilterEnabled`.
2. Pass only the visible series to `<BarChart>` (`visibleTypes["Completed"] !== false` → include completed series; same for ongoing). Chip colours: Completed = `theme.palette.primary.main`; Ongoing = `theme.palette.primary.light` (representative of the hatch).
3. While `completionFilterEnabled` is false: render no chips, always pass both series, and ensure visibility resets to both-on for the next enable (scenario 7).
4. Wire in `BaseMetricsView.tsx`: pass `completionFilterEnabled={ctx.cumulativeStateTimeSelectedItemIds.length === 0}` to the chart.
5. Un-skip scenarios one at a time (Outside-In). Then: Stryker ≥ 80% on the new surface (DoD 5), optional `cumulative_state_time_legend_toggle` analytics event (DoD 9), live-verify on team + portfolio detail (DoD 1).
