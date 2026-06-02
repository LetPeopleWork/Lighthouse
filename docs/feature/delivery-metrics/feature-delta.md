<!-- markdownlint-disable MD024 -->
# Feature: delivery-metrics

Epic 3993 (Delivery Metrics) — historical/over-time metrics for a Portfolio's Deliveries: backlog-and-done burnup, forecast-over-time, likelihood/predictability trend, and a fever-chart stretch. User-facing charting on the Portfolio → Delivery detail surface.

ADO Epic: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993>

## Wave: DISCUSS / [REF] Pre-DISCUSS code reality check

The premise that Lighthouse has NO historical/time-series persistence for delivery metrics today was verified in code before drafting this delta.

**Files inspected**:

- `Lighthouse.Backend/Lighthouse.Backend/Data/LighthouseAppContext.cs` — the DbSets persist only CURRENT state: `Deliveries`, `WorkItems`, `WorkItemStateTransitions`, `FeatureStateTransitions`, `Features`, `Portfolios`, `RefreshLogs`. There is NO `DeliveryMetricSnapshot` / metrics-history / time-series table.
- `Lighthouse.Backend/Lighthouse.Backend/Models/Delivery.cs` — a `Delivery` is a child of a `Portfolio` with `Name`, a future `Date`, a feature set (`SelectionMode` Manual or rule-based via `RuleDefinitionJson`). No historical fields.
- `Lighthouse.Frontend/src/models/Delivery.ts` — the FE-computed delivery metrics (`likelihoodPercentage`, `progress`, `remainingWork`, `totalWork`, `featureLikelihoods`, Monte-Carlo `completionDates`) are all CURRENT-SNAPSHOT, point-in-time only.
- `WorkItemBase.cs` — every `WorkItem` carries `CreatedDate`, `StartedDate`, `ClosedDate`; `WorkItemStateTransition` and `FeatureStateTransition` are persisted.

**Finding — one forward-recorded store (PIVOTAL; reframed by the backfill-drop decision, 2026-06-02)**:

- Backlog-size-over-time and done-over-time COULD in principle be reconstructed retroactively from `CreatedDate` / `ClosedDate` / feature-transition timing already in the DB. **The product decision (user, 2026-06-02) is NOT to do that.** All series are forward-only.
- A Monte-Carlo forecast (depends on throughput as-of-that-day, not in the DB historically), the likelihood/when-distribution trend, AND the inferred estimate of not-yet-broken-down features were never persisted on a past date and could never be reconstructed anyway — they accrue forward only by necessity.
- **Architectural decision**: build ONE `DeliveryMetricSnapshot` store as the single time-series source of truth, fed by ONE feed — a **forward recorder** that records every series (backlog, done, inferred estimate, forecast, likelihood/when-distribution) for the then-current day. All charts read from the one store; the chart starts empty at launch and fills one day at a time, exactly like the forecast/likelihood trends. There is no backfill and no reconstruction path.

The store is unified because all series share the same `(delivery, day)` grain and the same forward-recorded lifecycle — not because a backfill path is reconciled with a forward path. That single forward-recorded store is the spine of the 5-slice carpaccio below. Trade-off accepted: no immediate history; value accrues over weeks. See `## Changed Assumptions`.

## Wave: DISCUSS / [REF] Persona ID

**Primary**: `delivery-forecaster` — Delivery Forecaster / RTE who owns the conversation with leadership about WHEN deliveries will ship and HOW MUCH the team will deliver, using Lighthouse Monte-Carlo forecasts as the evidence base. The forecaster's trend-honesty job is the spine of this feature.

**Secondary (supporting)**: `delivery-lead-rte` — the same person in a release-train framing; reuses the over-time charts in retro / improvement-planning contexts.

**Secondary (scope-cut)**: `product-owner` — supported on the SAME charts for a scope-cut decision (which features to drop when a delivery is at risk). Not the spine; no bespoke chart.

## Wave: DISCUSS / [REF] JTBD one-liner

When I am tracking a delivery toward its target date and preparing to report status to leadership, I want to see how the delivery's backlog, completion, and forecast have moved OVER TIME — not just today's snapshot — so I can tell an honest trend story ("we were on track until scope was added in week 3; the forecast has held since") instead of defending a single point-in-time number that leaders read as a guarantee.

**Job-id (lead)**: `job-forecast-delivery-trend-over-time` (NEW — added to `docs/product/jobs.yaml` in this DISCUSS run; persona `delivery-forecaster`).

**Job-id (secondary, scope-cut lens)**: `job-po-scope-cut-from-delivery-trend` (NEW — persona `product-owner`; the PO uses the same over-time charts to decide which features to cut when the forecast band won't meet the backlog by the target date).

Differentiated from the forecaster's existing jobs:

- `job-forecast-no-false-certainty` / `job-forecast-only-with-enough-data`: about the HONESTY of TODAY's single forecast number. THIS job is about the TREND of that number over the delivery's life — a different artifact (a time series, not a capped percentage).
- `job-delivery-lead-spot-workflow-constraint` (sibling `state-time-cumulative-view`): per-STATE cumulative time across items. THIS job is per-DELIVERY backlog/done/forecast over calendar time. Different unit, different question, same flow-data foundation.

## Wave: DISCUSS / [REF] Corroborating evidence (customer Excel)

A customer who explicitly requested this capability already tracks it MANUALLY in Excel (example provided 2026-06-02), with three charts that map one-to-one onto this design — strong validation that the three-chart model (D12) and the geometric on-track read (D8) match how the job is actually done:

- **"How Many Items done by the delivery date (to 85%)"** — Done items, Total Backlog Size, and a forecast "how-many-by-the-target-date @85%" line, all on a count axis over time. This IS the Delivery Burnup (Slices 1+2+3): the on-track signal is the forecast line climbing to meet the backlog line by the date.
- **"How Likely?"** — % of simulations delivering the backlog by the required date, tracked over time. This IS the likelihood-over-time view of the Predictability chart (Slice 4 / US-04).
- **"When will it be done?"** — forecasted completion date(s) at one or more percentiles (e.g. 70%) plotted over time against a dashed target-delivery-date reference line. This IS the when-distribution view of the Predictability chart (Slice 4 / US-04); the customer treats it as a first-class chart, which sharpens its spec (date y-axis + target-date reference line + percentile spread).

Two design refinements taken from the example: forecast series are pinned to the existing Monte-Carlo percentiles (default 85% for how-many, 70% for the when-date); the when view is a fully-specified first-class view (date axis, dashed target-date line, narrowing/widening percentile spread = firming/slipping predictability), not a vague toggle.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | **One forward-recorded store; every series is forward-only.** Backlog+Done COULD be reconstructed from `WorkItem` dates + `FeatureStateTransition`, but the product decision (2026-06-02) is forward-only: a forward recorder records backlog, done, inferred estimate (Slice 2), forecast (Slice 3), and likelihood (Slice 4) for the then-current day. The chart starts empty and accrues. **Originally framed as a reconstructable-by-backfill vs forward-only seam (and revised by D11 to a dual-feed store); both are now superseded** — there is ONE `DeliveryMetricSnapshot` store fed by ONE forward recorder, no backfill, no reconstruction. See `## Changed Assumptions`. | Locked (backfill dropped 2026-06-02). |
| D2 | **Chart placement**: the Portfolio → Delivery detail surface (`Lighthouse.Frontend/src/pages/Portfolios/Detail/`, `PortfolioDeliveryView.tsx` / `DeliverySection.tsx`). The over-time charts live in a "Metrics" tab inside the per-delivery `DeliverySection` accordion (the existing feature grid becomes a "Work Items" tab); the Metrics tab is the lazy fetch trigger for `metrics-history`. No new top-level route — keeps the user's mental model intact (delivery tracking is already done from the portfolio delivery view). | Locked (tabs-in-accordion refinement 2026-06-02). |
| D3 | **Walking skeleton = Slice 1** (snapshot store + forward recorder + backlog+done burnup, forward-only). There is NO separate WS slice; Slice 1 IS the thinnest end-to-end user-visible slice that ships the store + the forward-recorder pipeline. Lighter than the prior backfill WS (~1-1.5 days) since there is no backfill component. | Locked (backfill dropped 2026-06-02). |
| D4 | **Premium gating inherited.** Deliveries are already a premium capability — verified: `Lighthouse.Frontend/src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.tsx` gates on `licenseStatus?.canUsePremiumFeatures` via `useLicenseRestrictions()`. The over-time metrics views inherit that gating: a non-premium instance never sees these charts. No new premium gate is invented — the charts sit behind the existing delivery surface's gate. | Locked — verified in code. |
| D5 | **No infra-only slice exists anymore (slice-composition gate satisfied structurally).** The `DeliveryMetricSnapshot` store + forward recorder is folded INTO Slice 1 alongside the user-visible burnup chart, so there is no standalone `@infrastructure` foundation slice that could fail the gate. The store IS the shared abstraction, shipped first, in the same slice as a user-visible consumer. The former US-02 `@infrastructure` story and the US-02b "captured today" marker are dissolved (US-02 folded into Slice 1; the marker is no longer needed because the burnup itself proves the store works). Every slice is user-visible by construction. | Locked. |
| D6 | **All history is forward-only and the empty state says so (now universal).** Because the store is forward-recorded with no backfill, ALL charts — the burnup included, not just forecast/likelihood — start empty and build forward from the day recording begins. The empty state reads "builds forward from today — no snapshots recorded yet" rather than implying data is missing. This honesty is inherited from the forecaster persona's existing jobs. | Locked (extended to all series 2026-06-02). |
| D7 | **Estimated size for not-yet-broken-down features is owned by Slice 2 (US-01b), IN MVP.** A feature in a delivery with zero child work items contributes its configured default/estimated size to the backlog line, annotated as estimated. Without this the backlog burnup reads artificially low for early-stage deliveries (the common case the epic calls out). It is forward-fed by the recorder (a feature's inferred size/breakdown-state on a past date was never persisted), like every other series. | Locked. |
| D8 | **On-track read is geometric, not a new RAG endpoint (MVP).** Slice 3 stacks the daily forecast on Done; "done + forecast ≥ backlog at the target date ⇒ on track" is read off chart geometry. A dedicated on-track RAG/badge endpoint is a follow-up only if the geometric read tests poorly (Slice 3 learning hypothesis). | Locked. |
| D9 | **Fever chart is STRETCH, gated behind Slices 2-4.** Slice 5 is explicitly out of committed MVP; it ships only on an explicit greenlight after Slice 4, and is the only slice with no internal reference class (requires a pre-slice SPIKE if greenlit). | Locked. |
| D10 | **No change to current-snapshot delivery metrics.** `likelihoodPercentage`, `progress`, `remainingWork`, `totalWork`, `completionDates` on `Delivery.ts` are unchanged; the over-time charts are purely additive. Type-A walking skeleton — absent endpoints leave an empty chart slot, no regression. | Locked. |
| D11 | **One store, forward-recorded only, keep 5 slices (2026-06-02, backfill dropped).** Build ONE `DeliveryMetricSnapshot` store as the single time-series source of truth, fed by ONE feed — a **forward recorder** (daily/on-refresh) that records every series for the then-current day: backlog/done counts, inferred estimate (US-01b), forecast how-many (US-03), likelihood/when-distribution (US-04). All charts read from the one store; the chart starts empty at launch and fills one day at a time. The store is unified because all series share the same `(delivery, day)` grain and the same forward-recorded lifecycle. **This dissolves the standalone `@infrastructure` snapshot-foundation slice (former Slice 2 / US-02) by folding the store into Slice 1, and supersedes the earlier dual-feed (backfill + forward recorder) revision of this decision — there is no backfill.** Cost accepted: no immediate history; value accrues over weeks. Benefit: a lighter Slice 1 (~1-1.5 days: schema + migration + recorder hook + first chart, no backfill component) and a uniformly forward-only, honest model with no reconstruction-trustworthiness risk. See `## Changed Assumptions`. | Locked. |
| D12 | **Chart consolidation (2026-06-02): 3 charts total, NOT one per slice.** The five slices map to series on THREE visual charts, not five charts. (1) **Delivery Burnup** — one chart shared by Slices 1-3: Done line + actual-backlog line (US-01) + inferred-estimate line (US-01b) + forward forecast band stacked on Done (US-03), all on a single count axis against a delivery-date marker; "on track ⇔ forecast band reaches the scope line before the marker" is read off this one chart. Each of Slices 1-3 ADDS A SERIES to this chart rather than shipping a new component. (2) **Predictability trend** (Slice 4 / US-04) — a separate chart because likelihood is a probability axis (0-100%), not a count; the when-distribution (forecast completion-date percentiles) is a TOGGLE/view on this same chart, not a fourth chart. RAG bands reuse the existing `getLikelihoodLevel` thresholds (<50 risky / <70 realistic / <85 likely / ≥85 certain). The predictability chart has two first-class views — a likelihood view (% axis) and a when view (date axis, dashed target-date reference line, percentile spread) — matching the customer Excel's "How Likely?" and "When will it be done?" charts. (3) **Fever chart** (Slice 5 / US-05, stretch) — a distinct 2-D idiom (buffer-consumed vs schedule-consumed). DESIGN resolved placement: a "Metrics" tab inside the per-delivery `DeliverySection` accordion (D2/D5); the when-distribution is a view of the predictability chart, not a separate component. | Locked. |

## Wave: DISCUSS / [REF] Scope Assessment

`## Scope Assessment: split-approved`

The full epic (4 charts + fever-chart stretch) is oversized for a single story (multiple independent user outcomes; >3 days; a persistence foundation). The orchestrator's interactive gate approved the **5-slice elephant-carpaccio split** below, sequenced on the single forward-recorded store (D1/D11): Slice 1 stands up the store + recorder + first series, Slices 2-4 each add a forward-recorded series. Each slice is independently demonstrable and user-visible — there is no `@infrastructure`-only slice (the store folds into Slice 1 per D11).

### Carpaccio taste tests per slice

| Slice | Vertical (DB→UI)? | Demoable in one session? | User-visible value? | Independently shippable? | Verdict |
|---|---|---|---|---|---|
| 1 Snapshot store + forward recorder + burnup (WS) | PASS (schema + migration + recorder → endpoint → chart) | PASS | PASS (chart is the visible value, store not shipped naked; forward-only empty state at first) | PASS (no deps) | **PASS — ships the store as the shared abstraction first, WITH a user-visible chart in the same slice (not abstraction-only)** |
| 2 Inferred-estimate enrichment (forward-fed) | PASS (forward recorder writes column → endpoint → inferred line) | PASS | PASS (projected-total backlog lens, US-01b) | PASS (deps Slice 1) | **PASS** |
| 3 Forecast-over-time | PASS (forward forecast field → endpoint → stacked chart) | PASS | PASS (on-track read) | PASS (deps Slice 1 store + elapsed days) | **PASS** |
| 4 Likelihood trend | PASS (forward likelihood field → endpoint → trend chart) | PASS | PASS (predictability read) | PASS (deps Slice 1 store + elapsed days) | **PASS** |
| 5 Fever chart (stretch) | PASS (snapshots → fever widget) | PASS | PASS (leadership glance) | PASS (deps Slices 2-4 + greenlight) | **PASS — but STRETCH, out of committed MVP (D9)** |

**Slice-1 abstraction-first justification**: Slice 1 ships >1 new component (table + EF migration + forward recorder + endpoint + chart) plus an abstraction (the snapshot store). The carpaccio "ship the abstraction first / not abstraction-only" tests both PASS because the store is the shared abstraction the whole feature stands on AND it ships in the same slice as a user-visible burnup chart that consumes it. With the store folded in, there is no infra-only slice to fail the slice-composition gate.

## Wave: DISCUSS / [REF] Story map

**Persona / goal**: Delivery Forecaster / RTE — tell an honest over-time trend story for a delivery toward its target date.

**Backbone (user activities, left→right)**: Open a delivery → Read its history (where have we been?) → Read its forecast trend (where are we headed?) → Read its predictability (is the forecast stabilising?) → Glance the risk signal (how worried should we be?).

| Open delivery | Read history | Read projected total | Read forecast trend | Read predictability | Glance risk |
|---|---|---|---|---|---|
| US-01 burnup (backlog+done) | US-01 backlog/done over time (forward-recorded) | US-01b inferred-estimate line | US-03 forecast stacked on Done | US-04 likelihood trend | US-05 fever (stretch) |
| (store + forward recorder, Slice 1) | — | US-01b estimated-portion annotation | US-03 on-track read | US-04 spread band | — |

**Walking skeleton** = US-01 (Slice 1): open a delivery, see its backlog+done burnup read from the snapshot store as the forward recorder accrues it (forward-only empty state at first). Touches the full schema→recorder→endpoint→chart path. The store ships here as the single source of truth all later slices read from.

**Release slices** (outcome-sequenced, NOT feature-grouped): see the 5 slice briefs in `slices/`. Priority order and rationale below.

### Priority rationale

1. **Slice 1 (WS)** — foundation-first: it ships the snapshot store (the single source of truth all later slices read from) and the forward-recorder hook (the sole feed) plus the first user-visible chart. Lighter (~1-1.5 days) now that there is no backfill component, and unavoidable since the store is needed for 3/4/5 regardless (D11).
2. **Slice 2** — first forward-fed lens (inferred-estimate); a distinct, valued backlog view (US-01b) that keeps early-stage deliveries from reading artificially small. Depends on the Slice-1 store + chart.
3. **Slice 3** — highest-value forward outcome (the on-track read is the forecaster's core question); forward-fed forecast field on the same store.
4. **Slice 4** — complementary predictability lens; lower value than the on-track read, same store.
5. **Slice 5** — stretch; gated behind 2-4; highest UI uncertainty, lowest priority.

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Snapshot store + forward recorder + Backlog/Done burnup (walking skeleton)

**Story**: As a `delivery-forecaster`, I want a burnup chart on the delivery detail view showing total backlog and items done as they accrue over time — recorded forward into a snapshot store from the day recording begins — so that from then on I can see where the delivery has been, not just today's snapshot, when I report status.

**Job-id**: `job-forecast-delivery-trend-over-time`

#### Elevator Pitch

Before: the delivery detail view shows only TODAY — likelihood, progress, remaining/total work. To answer "how did we get here? when did scope jump?" I export work-item history to a spreadsheet and rebuild the burnup by hand, every status meeting.
After: open `/portfolios/{portfolioId}` → the delivery's detail → the "Metrics" tab shows a "Burnup" chart with the backlog line and the done line, recorded forward into a snapshot store each day from the day recording begins. The chart starts empty and fills one day at a time (exactly like the forecast trends); after a few weeks a scope jump shows as a step up in the backlog line. No more rebuilding it in a spreadsheet.
Decision enabled: once history accrues, how to frame the status story to leadership — "we were on track until scope was added here (point to the step), and we've burned steadily since" — instead of defending a bare snapshot number.

**AC**:

- Given a `DeliveryMetricSnapshot` store exists (DbSet + EF migration via the `CreateMigration` script across all providers) and the forward recorder has run for one or more days, when I open a delivery's "Metrics" tab, then a burnup chart renders with a daily backlog line (`totalWork`) and a done line (`doneWork`) for each recorded day, read from the store (not a live request-time computation).
- Given the forward recorder runs, then each daily snapshot row records the delivery's then-current counts: `totalWork`, `doneWork`, `remainingWork = totalWork - doneWork`; re-running the recorder the same day does not duplicate rows (idempotent on `(deliveryId, recordedAt)`); an item re-opening (leaving Done) lowers the next day's `doneWork`.
- Given scope is added to or removed from the delivery between recordings, then the backlog line steps up/down on the next recorded day rather than appearing flat.
- Given a delivery with no recorded snapshots yet (recording just began, or no items), then the chart renders the forward-only empty state "builds forward from today — no snapshots recorded yet" in the tone of existing Lighthouse empty charts (D6).
- Given the delivery surface is gated by `canUsePremiumFeatures`, then a non-premium instance does not render the chart (inherits the existing gate — D4).
- Given the EF migration is generated, then it applies cleanly on a real (non-InMemory) provider in the migration test.
- No regression to the existing current-snapshot delivery metrics on the same view.

### US-01b — Inferred-estimate line for not-yet-broken-down features (Slice 2, forward-fed)

**Story**: As a `delivery-forecaster` tracking an early-stage delivery, I want features that haven't been broken down into child items yet to still contribute an estimated size to the backlog — recorded forward into the snapshot store — so the burnup doesn't read artificially low and mislead me into thinking the delivery is smaller than it is.

**Job-id**: `job-forecast-delivery-trend-over-time`

#### Elevator Pitch

Before: a delivery full of features that aren't broken down yet shows a near-zero backlog line — the burnup looks almost done before work has started, which is the opposite of honest.
After: open the delivery's Burnup chart → features with zero child items contribute their configured default/estimated size to a "total backlog including inferred estimate" line, recorded forward into the snapshot store, and the chart annotates "{N} of backlog is estimated (features not yet broken down)" so the size isn't silently inflated.
Decision enabled: whether the delivery's apparent size can be trusted yet, or whether breaking down features must come before any forecast conversation.

**AC**:

- Given a feature in the delivery with zero child work items, when the forward recorder writes a snapshot, then that feature contributes its configured default/estimated size to the inferred-estimate column rather than zero; re-running the same day is a no-op.
- The chart draws the inferred-estimate line alongside the actual-item backlog line and annotates that a portion of the backlog is estimated (not counted from real items), distinguishing it from broken-down work.
- Given all features are broken down, then no estimated portion is shown and the inferred line collapses onto the actual-item backlog.
- Given no forward snapshot has yet carried an inferred estimate, then only the actual-item backlog line shows, with the honest "estimated total builds forward from today" note (forward-only — D6/D11).

*(US-02 and US-02b removed per D11: the `DeliveryMetricSnapshot` store + forward recorder is folded into Slice 1 alongside the user-visible burnup, so there is no standalone `@infrastructure` foundation story and no "forecast captured today" marker — the burnup chart itself proves the store works. The forward recorder introduced by Slice 1 is the sole feed, used by Slices 2-4 to append the inferred estimate / forecast / likelihood columns.)*

### US-03 — Forecast-over-time stacked on Done (the on-track read)

**Story**: As a `delivery-forecaster`, I want the daily "how many will we complete by the target date" forecast stacked on top of the Done line against the backlog line, so I can read directly off the chart whether the delivery is on track — done plus projected forecast meeting or exceeding the backlog at the target date.

**Job-id**: `job-forecast-delivery-trend-over-time`

#### Elevator Pitch

Before: today's snapshot gives one likelihood number; to judge "are we on track?" I have to hold the backlog, the done count, and the forecast in my head and reason about whether they converge by the date.
After: open the delivery's metrics → a forecast band stacks on the Done area, drawn against the backlog line; where done + forecast meets or exceeds backlog at the target date, the delivery reads on track at a glance; where it falls short, the gap is visible.
Decision enabled: whether to raise this delivery as at-risk now (the forecast band won't meet the backlog) or report it on track — and, for a Product Owner, whether scope must be cut (`job-po-scope-cut-from-delivery-trend`).

**AC**:

- Given accumulated `DeliveryMetricSnapshot` rows carrying the forward forecast field, when I open the delivery's metrics, then a forecast segment stacks on the Done area showing the recorded projection per date, drawn against the backlog line.
- Given an on-track delivery (done + forecast ≥ backlog at target date), then the chart geometry reads visibly differently from an at-risk delivery (done + forecast < backlog at target date).
- Given few snapshots, then the forecast band is short and annotated "forecast trend builds forward from {first snapshot date}."
- Given no snapshots, then the forecast area shows the D6 empty state.
- No new RAG endpoint in MVP — the on-track read is geometric (D8).
- The forecast how-many series is pinned to an existing Monte-Carlo percentile — default **85%** ("how many done by the target date at 85% probability", matching the customer Excel example); the recorded `forecastHowMany` reuses the existing `WhenForecast` percentile output rather than a new computation.

### US-04 — Likelihood / when-distribution over time (predictability trend)

**Story**: As a `delivery-forecaster`, I want the delivery's likelihood and forecast-spread plotted over time, so I can tell whether our predictability for this delivery is improving (spread narrowing, likelihood stabilising) or degrading (spread widening) — a different question from "are we on track today?".

**Job-id**: `job-forecast-delivery-trend-over-time`

#### Elevator Pitch

Before: I can see today's likelihood, but not whether it's been climbing, flat, or sliding — so I can't tell leadership "our forecast has held steady for three weeks" with evidence.
After: open the delivery's metrics → a likelihood-over-time line and a forecast-spread band show whether predictability is stabilising (line flattening high, band narrowing) or slipping (band widening).
Decision enabled: whether to trust the current forecast as stable enough to commit to, or to flag that predictability is degrading and the date needs re-examination.

**AC**:

- Given accumulated snapshots with varying `likelihoodPercentage` and when-distribution spread, when I open the **likelihood view**, then a likelihood-over-time line renders per snapshot date, banded with the existing `getLikelihoodLevel` RAG thresholds (<50 / <70 / <85 / ≥85) — corresponds to the customer's "How Likely?" chart.
- Given the predictability chart, when I switch to the **when view**, then it re-renders with a DATE y-axis: one or more forecast completion-date percentile lines (default 70%; the 50/70/85/95 spread available) plotted per snapshot date against a dashed **target-delivery-date reference line** — corresponds to the customer's "When will it be done?" chart. A narrowing spread converging on/under the target reads as firming predictability; a widening spread or lines crossing above the target reads as slipping. The when-distribution is a first-class view of this one chart (per D12), not a separate chart component.
- Given an improving fixture (likelihood rising, spread narrowing) vs a degrading fixture (spread widening), then the two read visibly differently.
- Given sparse or no snapshots, then the sparse-data / empty-history states render consistently with US-03.

### US-05 — Fever chart (STRETCH)

**Story**: As a `delivery-forecaster` (or a `delivery-lead-rte` in a leadership review), I want a TameFlow-style fever chart plotting buffer-consumption against schedule progress over the recorded snapshots, so leadership gets a single at-a-glance "how worried should we be?" signal per delivery.

**Job-id**: `job-forecast-delivery-trend-over-time`

#### Elevator Pitch

Before: the line charts (US-03/US-04) answer the forecaster's questions but a leadership audience wants one glance — green/amber/red — not three lines to interpret.
After: open the delivery's metrics → a fever chart shows a bubble that has moved through green/amber/red zones over time as buffer was consumed against schedule; a red bubble is the one-glance "this delivery is in trouble" signal.
Decision enabled: which deliveries leadership should focus the review on — the red and amber ones — without reading every line chart.

**AC**:

- Given accumulated snapshots, when I open the fever chart, then a bubble plots buffer-consumed (y) against schedule-consumed (x) with a trail across snapshot dates through green/amber/red zones.
- Given an on-track delivery, the trail stays in green/amber; given an at-risk one, it enters red.
- Given no/sparse snapshots, then no bubble renders and an empty-state message shows.
- **STRETCH**: this story ships only on explicit greenlight after Slice 4 (D9); it is OUT of committed MVP.

## Wave: DISCUSS / [REF] Definition of Done

1. All committed stories (US-01, US-01b, US-03, US-04) pass their ACs via integration tests (NUnit + EF InMemory + WebApplicationFactory for endpoints; Vitest + RTL for charts). US-05 only if greenlit.
2. Backlog/done forward recording (US-01) verified against a fixture delivery with known current counts; the recorder writes the day's exact counts, a re-open lowers the next day's `doneWork`, and the recorder is idempotent on `(deliveryId, recordedAt)`.
3. The `DeliveryMetricSnapshot` store + EF migration (US-01, Slice 1) verified: migration applies on a real (non-InMemory) provider; the forward recorder (the sole feed) appends at most one row per delivery per day, idempotent re-run (verified per forward column in US-01b/US-03/US-04). The inferred-estimate forward recording (US-01b) is verified against a not-broken-down feature.
4. Forecast-over-time on-track read (US-03) verified against on-track and at-risk fixtures.
5. Likelihood/predictability trend (US-04) verified against improving and degrading fixtures.
6. Empty-state, sparse-data, single-day-history, and forward-only-empty edge cases render gracefully across all charts.
7. Premium gating (D4) verified: a non-premium license does not render the charts.
8. No regression to the existing current-snapshot delivery metrics or the rest of the portfolio delivery view.
9. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
10. SonarCloud quality gate passes on PR.
11. Mutation testing (Stryker.NET backend; Stryker frontend): ≥80% kill rate for new code.
12. Docs updated: screenshots of the burnup (with estimated-portion annotation) and the forecast-over-time chart; website marketing surface updated for the launch (per cross-cutting checklist).

## Wave: DISCUSS / [REF] Out of scope

- **Any retroactive / reconstructed history for any series** — by product decision (2026-06-02) ALL series are forward-only: the store accrues from the day recording begins, with no backfill from item dates and no reconstruction of a past Monte-Carlo forecast (which depends on throughput-as-of-that-day, not in the DB). Trends and the burnup alike build forward only (D6). See `## Changed Assumptions`.
- **Configurable fever-chart zone thresholds** — baseline defaults only; tunability is a follow-up.
- **Cross-delivery comparison / multi-delivery board** — one delivery at a time in MVP.
- **A dedicated on-track RAG/badge endpoint** — MVP reads on-track geometrically (D8); a badge is a follow-up if the geometric read tests poorly.
- **Changes to current-snapshot delivery metrics** — `Delivery.ts` fields unchanged (D10).
- **Business-hours / working-calendar time** — wall-clock, matching existing charts.
- **Any backfill at all** — there is no backfill component (user decision, 2026-06-02). Every series, including backlog/done, is forward-recorded into the store from the day recording begins; all lines are sparse for the first weeks and the chart starts empty (D6/D11). See `## Changed Assumptions`.
- **US-05 fever chart** — STRETCH, out of committed MVP unless greenlit after Slice 4 (D9).

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract change to existing endpoints. Slice 1 (US-01) IS the walking skeleton — the `DeliveryMetricSnapshot` store + EF migration + the forward-recorder hook (the sole feed) + a new history endpoint reading the store + a new burnup widget in the delivery's "Metrics" tab, with empty/edge states handled. If the endpoint is absent the chart slot is empty — no regression; until recording accrues the chart shows the forward-only empty state. Slice 2 (US-01b) forward-feeds the inferred-estimate column; Slices 3-4 forward-feed the forecast and likelihood columns; all read from the one store. There is no `@infrastructure`-only slice (the store folds into Slice 1 per D11).

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/latest/deliveries/{deliveryId}/metrics-history` (delivery-scoped per ADR-050; `api/v1` + `api/latest`) | `[RbacGuard(PortfolioRead)]` | **New (Slice 1)** | Returns `{ points: [{ date, totalWork, doneWork, remainingWork }] }` read FROM the `DeliveryMetricSnapshot` store (forward-recorded series). Slice 2 adds `estimatedTotal`; Slices 3-4 add the forecast + likelihood/when series — all as additional (initially-null) series on THIS one endpoint (NO separate `/forecast` endpoint — ADR-050). Empty `points` when no snapshots recorded yet or the delivery has no items. **Version-gating obligation applies from Slice 1.** DESIGN (ADR-050) supersedes the earlier portfolio-nested route shorthand. |
| — (background) | DeliveryMetricSnapshot forward recorder (on refresh) | Server-side, no user auth | **New (Slice 1)** | Slice 1 lands the store and the forward-recorder hook (the sole feed): it records each delivery's then-current backlog/done counts per day, idempotent on `(deliveryId, recordedAt)`. Slices 2-4 append forward columns (inferred estimate, forecast, likelihood/when-distribution) per delivery per day on refresh; idempotent on date. No backfill. |
| GET | existing current-snapshot delivery metrics | Authenticated | Existing | **Unchanged** (D10). |

UI surfaces touched: `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioDeliveryView.tsx` / `DeliverySection.tsx` (a "Work Items" tab for the existing grid + a "Metrics" tab for the charts, inside the accordion). Three chart components total (D12), not five: a new `DeliveryBurnupChart.tsx` (Done + actual-backlog + inferred-estimate + forecast band — enriched across Slices 1-3), a new `DeliveryPredictabilityChart.tsx` (Slice 4 — two views: a likelihood-% view and a when view with a date axis + dashed target-date line + percentile spread), and a stretch fever-chart widget (Slice 5). The existing premium gate (`useLicenseRestrictions` / `canUsePremiumFeatures`) and RBAC gate (`useRbac`) wrap them unchanged.

## Wave: DISCUSS / [REF] Cross-cutting impact checklist (DoR item 7 hard gate)

- **RBAC** — The delivery-metrics charts are READ views of Portfolio/Delivery data. They gate through the existing portfolio read-access path: authorization flows through `IRbacAdministrationService`, and UI gating derives from the `useRbac()` hook (already used on `PortfolioDetail.tsx`, line 97; `canEditDeliveries = rbac.isPortfolioAdmin(portfolio.id)` at line 103). Viewers see the charts read-only; there is NO new write surface (the recording job is a server-side background process, not a user action). No component fetches `/api/latest/authorization/my-summary` directly — all gating derives from `useRbac()`. **Confirmed: no new authorization effect beyond inheriting the existing portfolio read gate.**
- **Lighthouse-Clients (CLI + MCP)** — There is ONE new endpoint (`GET .../deliveries/{deliveryId}/metrics-history`, per ADR-050 — forecast/likelihood are additional series on it, not separate endpoints), so there is ONE version-gate to register: an old Lighthouse server returns an opaque 404, so the wrapping client method must pre-check the server version against `FEATURE_REQUIRES_SERVER_NEWER_THAN`, pinned strictly newer than the LAST released Lighthouse version (bump that registry to the current latest release when wrapping). **The endpoint ships in Slice 1, so the version-gating obligation applies from Slice 1** — the single registry entry covers Slices 2-4 (they only add series to the same response). Per-slice: Slice 1 — endpoint ships; version-gate it; a CLI/MCP delivery-history command follows only if wanted (UI-first). Slices 2-4 — N/A (extra series on the already-gated Slice-1 response). Slice 5 — N/A (visualization-only). Dev/unparseable server versions must never be blocked.
- **Website** — Delivery tracking over time is a flagship forecasting capability. N/A for early slices (1-2). LIKELY YES at Slice 3 launch — surface/market the delivery forecast-over-time view on the public website. Recorded as a launch-checklist item in the Slice 3 brief.

## Wave: DISCUSS / [REF] Outcome KPIs

### Objective

Within 8 weeks of launch, delivery forecasters and POs report delivery status to leadership using the over-time trend charts as the evidence base, instead of defending point-in-time snapshots or hand-built spreadsheets.

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|---|---|---|---|---|---|
| 1 | Forecasters/POs on premium instances with ≥1 delivery | Open the delivery burnup/trend charts when reviewing a delivery | ≥40% of delivery-detail sessions include a view of an over-time chart within 8 weeks | 0 (charts don't exist) | Frontend usage telemetry (gated on the self-hosted telemetry caveat below) | Leading |
| 2 | Forecasters tracking a delivery | Stop exporting work-item history to a spreadsheet to build a burnup | ≥50% reduction in self-reported spreadsheet-burnup workarounds among interviewed users | "every status meeting" (persona frustration) | User interviews / qualitative follow-up | Leading (secondary) |
| 3 | POs facing an at-risk delivery | Make a scope-cut decision citing the forecast-vs-backlog trend | ≥1 in 3 at-risk-delivery reviews references the over-time chart for the cut decision | 0 | User interviews | Leading |
| 4 | Snapshot store (forward recorder, the sole feed) | Capture a forward snapshot per delivery per day | 100% of active deliveries have a daily snapshot after recording starts; 0 duplicate rows on `(deliveryId, recordedAt)`; a same-day re-run overwrites in place | n/a | Backend integration metric / row-count assertion | Guardrail |

### Metric Hierarchy

- **North Star**: % of delivery-detail review sessions that consult an over-time trend chart (KPI 1).
- **Leading indicators**: reduction in spreadsheet workarounds (KPI 2); scope-cut decisions citing the trend (KPI 3).
- **Guardrail**: snapshot recording correctness/idempotency (KPI 4); no regression in delivery-detail page load time.

### Measurement caveat

Cross-instance behavioral telemetry is blocked on Epic 5015 (opt-in telemetry; no timeline) — see MEMORY `project_self_hosted_telemetry_gap`. KPIs 1-3 are measured on dogfood/dev instances and via user interviews until 5015 lands; KPI 4 is measurable today via backend integration assertions. **Additional forward-only timing caveat**: because the store is forward-recorded with no backfill, the charts are empty at launch and only become useful as snapshots accrue, so KPIs 1-3 (chart consultation, spreadsheet-workaround reduction, scope-cut citations) can only be measured after enough forward history has accumulated per delivery (on the order of weeks) — the 8-week window starts from when a delivery first has a usable trend, not from launch day. This caveat is inherited by the DEVOPS handoff.

## Wave: DISCUSS / [REF] DoR validation (9-item)

### Story: US-01 (Snapshot store + forward recorder + Backlog/Done burnup — walking skeleton)

| DoR Item | Status | Evidence |
|---|---|---|
| 1 Problem statement (domain language) | PASS | "delivery detail shows only TODAY; I rebuild the burnup in a spreadsheet every status meeting" — persona frustration, domain terms. |
| 2 Persona with characteristics | PASS | `delivery-forecaster` — owns the leadership WHEN/HOW-MUCH conversation; persona file verified. |
| 3 3+ domain examples (real data) | PASS | Scope-added-between-recordings example; recorder writes the day's current counts; re-open lowers next day's done; empty/forward-only state; idempotent same-day re-run; migration-on-real-provider. |
| 4 UAT in G/W/T (3-7) | PASS | 7 AC in Given/When/Then form. |
| 5 AC derived from UAT | PASS | AC trace to the store/recorder, the burnup outcome, the migration test, and the premium/empty/regression scenarios. |
| 6 Right-sized (1-3 days, 3-7 scenarios) | PASS | Slice 1 = ~1-1.5 days (schema + migration + recorder hook + first chart, lighter now that there is no backfill — D11); 7 AC; single demoable burnup. |
| 7 Technical notes / cross-cutting | PASS | EF migration via `CreateMigration` script; forward-recorder + idempotency-not-a-sentinel constraints; RBAC/Clients (version-gate from Slice 1)/Website answered; premium gate verified in code. |
| 8 Dependencies resolved/tracked | PASS | EF migration tooling (`CreateMigration` script) tracked; reads current `Feature.FeatureWork`/forecast data into the new store via the recorder; tracked in the slice brief. |
| 9 Outcome KPIs (measurable) | PASS | KPIs 1-2 above with targets, baseline, method, telemetry + forward-only-timing caveat; KPI 4 guardrail (forward-recorder correctness) now lands here. |

**DoR Status: PASSED**

### Story: US-01b (Inferred-estimate line — Slice 2, forward-fed)

| DoR Item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Early-stage delivery's backlog reads artificially low when features aren't broken down; the inferred size on a past date was never persisted so it must be forward-fed (D11, code-verified premise). |
| 2 Persona | PASS | `delivery-forecaster` tracking an early-stage delivery. |
| 3 3+ examples | PASS | Not-broken-down feature contributes configured size; all-broken-down collapses the line; no-forward-snapshot-yet shows only actual-item line; idempotent re-run. |
| 4 UAT G/W/T | PASS | 4 AC in Given/When/Then form. |
| 5 AC from UAT | PASS | Trace to the forward-fed inferred column, the chart line + annotation, and the forward-only empty state. |
| 6 Right-sized | PASS | Slice 2 = 1-2 days; its own user-facing story (no `@infrastructure`-only content), slice-composition gate satisfied structurally (D11). |
| 7 Technical notes / cross-cutting | PASS | Forward recorder reuses the Slice-1 hook; idempotency-not-a-sentinel; no new endpoint (extra field on the Slice-1 response); RBAC/Clients/Website answered. |
| 8 Dependencies | PASS | Slice 1 (store + chart + forward recorder) tracked. |
| 9 Outcome KPIs | PASS | KPIs 1-2 (the projected-total lens supports the trend-story outcome). |

**DoR Status: PASSED** — US-01b is a fully user-facing story (the inferred-estimate backlog lens); with the store folded into Slice 1, there is no `@infrastructure`-only story left in the feature, so the slice-composition gate is satisfied by construction.

### Story: US-03 (Forecast-over-time)

| DoR Item | Status | Evidence |
|---|---|---|
| 1-2 Problem/Persona | PASS | "judge on-track in my head" → geometric read; `delivery-forecaster`. |
| 3 Examples | PASS | On-track fixture; at-risk fixture; sparse-data; empty. |
| 4-5 UAT/AC | PASS | 5 AC in G/W/T, derived. |
| 6 Right-sized | PASS | Slice 3 = 2 days; 5 AC. |
| 7 Cross-cutting | PASS | Clients version-gate flagged; Website launch flagged; RBAC inherited. |
| 8 Dependencies | PASS | Slice 1 store + forward recorder + elapsed days tracked. |
| 9 KPIs | PASS | KPIs 1, 3. |

**DoR Status: PASSED**

### Story: US-04 (Likelihood trend)

| DoR Item | Status | Evidence |
|---|---|---|
| 1-9 | PASS | Distinct predictability question; improving/degrading fixtures; G/W/T AC; Slice 4 = 1-2 days; cross-cutting inherited from Slice 3; depends on the Slice-1 store + forward recorder; KPI 1. |

**DoR Status: PASSED**

### Story: US-05 (Fever chart — STRETCH)

| DoR Item | Status | Evidence |
|---|---|---|
| 1-9 | PASS-as-stretch | Full AC + cross-cutting in the slice brief; explicitly OUT of committed MVP (D9), gated behind Slice 4 greenlight; pre-slice SPIKE required (only slice with no internal reference class). Not blocking MVP DoR. |

**DoR Status: PASSED (as stretch — not part of the committed MVP gate)**

## Wave: DISCUSS / [REF] Risks

| Risk | Prob | Impact | Mitigation |
|---|---|---|---|
| No immediate history — the burnup is empty at launch and only useful after weeks of forward recording, so early users see little value | Medium | Medium | Accepted product trade-off (user decision 2026-06-02) for a lighter, honest Slice 1; the forward-only empty state sets the expectation honestly (D6); KPI timing caveat documents the measurement shift. |
| Forward recorder records wrong current counts or misses a day (active deliveries lack a daily snapshot) | Low | Medium | KPI-4 row-count guardrail + idempotency tests; the recorder rides the existing forecast-update cadence that already runs per portfolio. |
| Forward-fed data (inferred estimate / forecast / likelihood) too sparse or noisy for a useful trend | Medium | Medium | Slices 2-4 learning hypotheses; optional cadence SPIKE; charts annotate sparse data honestly and read "builds forward from today" (D6). |
| Cross-instance KPI measurement blocked on Epic 5015 telemetry gap | High | Medium | KPI 4 measurable today; KPIs 1-3 via dogfood + interviews until 5015; caveat carried to DEVOPS handoff. |
| DIVERGE wave artifacts absent — no `recommendation.md` / `job-analysis.md` for this epic | High | Low | Job grounding done from verified personas + code instead; noted here as the upstream-DIVERGE gap. |
| Fever chart (Slice 5) net-new visualization overruns | Low (gated) | Low | STRETCH-gated (D9); pre-slice SPIKE required if greenlit; droppable without affecting MVP. |

## Wave: DISCUSS / [REF] Wave Decisions summary

- **Feature type**: user-facing charting on Portfolio → Delivery detail (locked).
- **JTBD**: mandatory; lead job `job-forecast-delivery-trend-over-time` (forecaster trend-honesty), secondary `job-po-scope-cut-from-delivery-trend` (PO scope-cut). Both added to `jobs.yaml`.
- **Single forward-recorded store**: ONE `DeliveryMetricSnapshot` store, all series forward-only (D1 + D11). Originally framed as a reconstructable-by-backfill vs forward-only seam, then a dual-feed store; both superseded by the backfill drop (2026-06-02). See `## Changed Assumptions`.
- **Unified store (D11, 2026-06-02, backfill dropped)**: ONE `DeliveryMetricSnapshot` store fed by ONE feed — the forward recorder (backlog/done + inferred estimate + forecast + likelihood). All charts read from it; no backfill, no live-query read path. Store folds into Slice 1 alongside the burnup.
- **Walking skeleton**: Slice 1 (store + forward recorder + burnup), lighter (~1-1.5 days, no backfill) but foundation-first, no separate WS slice (D3 + D11).
- **Slice-composition gate**: satisfied structurally — the store folds into Slice 1 with a user-visible chart, so there is NO `@infrastructure`-only slice (D5/D11). The old US-02 foundation story and US-02b marker are dissolved.
- **Estimated-size requirement**: owned by Slice 2 (US-01b), forward-fed like every series, IN MVP (D7 + D11).
- **Premium gating**: inherited from the existing delivery surface's `canUsePremiumFeatures` gate (D4, code-verified).
- **Fever chart**: STRETCH, gated behind Slices 2-4 (D9).
- **DIVERGE absent**: noted as a risk; job grounding done from verified personas + code.
- Skipped per-wave peer review per the orchestrator (consolidated review fires at end of DISTILL).

## Changed Assumptions

Back-propagated change (2026-06-02, user decision) — the backfill is dropped. Recorded here per the DISCUSS→DESIGN back-propagation contract so DISTILL/DELIVER inherit a consistent truth.

**Original DISCUSS assumption (D1 + D11, verbatim)**:

> D1: *"The slicing seam is reconstructable-by-backfill vs forward-only-recorded. Backlog+Done reconstruct retroactively from `WorkItem` dates + `FeatureStateTransition`."*
>
> D11: *"build ONE `DeliveryMetricSnapshot` store as the single time-series source of truth, fed two ways — (a) a one-time **backfill** that reconstructs actual-item backlog & done history into snapshot rows (full populated history on day one, NO separate live-query read path), and (b) a **forward recorder** … that appends what can't be reconstructed."*

**New assumption (forward-only, no backfill)**: the `DeliveryMetricSnapshot` store is fed by the forward recorder ONLY. Every series — backlog, done, inferred estimate, forecast, likelihood/when-distribution — accrues daily from the day recording begins. There is no retroactive reconstruction of history from `WorkItem.CreatedDate`/`ClosedDate`. The chart starts empty at launch and fills one day at a time, exactly like the forecast/likelihood trends. Re-opens are handled naturally because each day snapshots the then-current count. The store stays unified — now for a simpler reason: all series share the same `(delivery, day)` grain and the same forward-recorded lifecycle.

**Rationale**: the user accepts delayed history (value accrues over weeks) in exchange for (a) a lighter Slice 1 (~1-1.5 days: store + migration + recorder hook + first chart, with no backfill component), (b) no reconstruction-trustworthiness risk (re-opened items, bulk-import `CreatedDate` skew, mid-flight feature re-scoping would all have made a reconstructed line diverge from team memory), and (c) a uniformly forward-only, honest model with no no-precedent backfill component. The KPIs that depend on chart usage (1-3) can only be measured once enough forward history has accrued.

**Ripple** (all applied in this delta and the DESIGN artifacts): D6's forward-only empty state is now universal (the burnup too, not just forecast/likelihood); former DESIGN Decisions 4 (backfill execution model) and 6 (backfill done-source / re-open cross-check) are removed as moot; Slice 1 is rescoped lighter; ADR-048 rewritten to single-feed (`adr-048-delivery-metric-snapshot-store.md`, superseding the dual-feed version); the placement refinement D2/D5 moves the charts into a "Metrics" tab inside the per-delivery `DeliverySection` accordion.

## Wave: DESIGN / [REF] Architect + status

Architect: Morgan (Solution Architect), interaction mode = PROPOSE. Date: 2026-06-02. Scope: Application/components.
Status: the six forking decisions below are **PROPOSED** (pending user confirmation — see the "Proposed Decisions — CONFIRM" block in the wave summary and `design/wave-decisions.md`). The DISCUSS decisions D1-D12 are inherited and not re-litigated. ADRs 048/049/050 carry the architecturally significant decisions. C4 in `docs/product/architecture/c4-diagrams.md` → "delivery-metrics"; brief section `## Application Architecture — delivery-metrics`.

## Wave: DESIGN / [REF] DDD

- **Bounded context**: this feature lives in the existing *Delivery / Portfolio Forecasting* context — no new context. `Delivery` is the aggregate root for membership; `Feature` (with `FeatureWork`, `Forecasts`, `EstimatedSize`) supplies the metric source.
- **New value/entity**: `DeliveryMetricSnapshot` is an immutable-by-convention time-series record keyed `(deliveryId, recordedAt.Date)` — a per-day projection, not a new aggregate root (it is owned by, and cascade-deleted with, its `Delivery`).
- **Ubiquitous language**: "backlog line" = `totalWork` forward-recorded over time; "done line" = `doneWork`; "inferred estimate" = forward-recorded size of not-yet-broken-down features; "forecast band" = `forecastHowMany` stacked on done; "predictability trend" = `likelihoodPercentage` + when-distribution spread over time; "on-track read" = geometric (done+forecast ≥ backlog at the delivery-date marker). All series are forward-recorded into the snapshot — no new domain term invented beyond the snapshot.
- **No DDD subdomain reshaping** — this is a projection/read-model addition over the existing forecasting domain.

## Wave: DESIGN / [REF] Component decomposition

| Component | Path | EXTEND / CREATE NEW | Notes |
|---|---|---|---|
| `DeliveryMetricSnapshot` model | `Lighthouse.Backend/Lighthouse.Backend/Models/DeliveryMetricSnapshot.cs` | CREATE NEW | Wide row, nullable forward columns, `(DeliveryId, RecordedAt)` unique; `WhenDistributionJson` value-converted (ADR-050) |
| DbSet + model config | `Data/LighthouseAppContext.cs` | EXTEND | DbSet, cascade-delete FK `DeliveryMetricSnapshot.DeliveryId` → `Delivery` (`ON DELETE CASCADE`, ADR-048), unique index, JSON converter (mirror `AdditionalFieldValues`/`StateMappings`) |
| EF migration | `Lighthouse.Migrations.Sqlite` + `Lighthouse.Migrations.Postgres` | CREATE NEW | Generated via the `CreateMigration` PowerShell script across all providers (do NOT call `dotnet ef migrations add`; do NOT run the script in DESIGN) |
| `IDeliveryMetricSnapshotRepository` + impl | `Services/Interfaces/Repositories/` + `Services/Implementation/Repositories/` | CREATE NEW | `IRepository<DeliveryMetricSnapshot>`; get-or-create by `(deliveryId, recordedAt.Date)` |
| `PortfolioForecastsUpdated(int PortfolioId) : IDomainEvent` | `Models/Events/PortfolioForecastsUpdated.cs` | CREATE NEW | The recorder's trigger; record, mirrors `PortfolioFeaturesRefreshed`'s shape; the ONLY new event Epic 3993 introduces (ADR-049) |
| `DeliveryMetricSnapshotRecordingHandler` | `Services/Implementation/DomainEvents/` | CREATE NEW | `IDomainEventHandler<PortfolioForecastsUpdated>`; forward-record projection — the sole feed (reuses `DeliveryWithLikelihoodDto.FromDelivery`); modeled on `PortfolioFeaturesRefreshedMetricsInvalidationHandler` |
| `PortfolioUpdater.Update` + `ForecastUpdater.Update` | `Services/Implementation/BackgroundServices/Update/` | EXTEND | Dispatch `PortfolioForecastsUpdated` via `IDomainEventDispatcher.PublishAsync` AFTER `UpdateForecastsForPortfolio` + forecast write-back (after ~line 84 in `PortfolioUpdater`); once per portfolio-forecast-completion on each path (ADR-049). NOT the stale pre-forecast `PortfolioFeaturesRefreshed` at line 73 |
| `DeliveryMetricsHistoryDto` (+ point record) | `API/DTO/` | CREATE NEW | ADR-050 response shape |
| metrics-history endpoint | `API/DeliveriesController.cs` (extend) or new `DeliveryMetricsController.cs` | CREATE NEW (endpoint) | `[RbacGuard(PortfolioRead)]`; `api/v1` + `api/latest` |
| `DeliveryBurnupChart.tsx` | `Lighthouse.Frontend/src/components/Common/Charts/` | CREATE NEW | MUI-X `LineChart`, area+line, time axis, delivery-date marker |
| `DeliveryPredictabilityChart.tsx` | same dir | CREATE NEW (Slice 4) | likelihood line + RAG bands + when-distribution toggle |
| `DeliveryFeverChart.tsx` | same dir | CREATE NEW (Slice 5 stretch) | buffer-vs-schedule bubble + trail |
| `deliveryMetricsHistorySchema` + model | `Lighthouse.Frontend/src/models/Delivery/` | CREATE NEW | Zod schema + `z.infer` type (ADR-050) |
| delivery metrics history fetch | `Lighthouse.Frontend/src/services/Api/` | EXTEND or CREATE NEW | one GET, parsed by the schema |
| `DeliverySection.tsx` | `src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx` | EXTEND | split `AccordionDetails` into a "Work Items" tab (existing grid) and a "Metrics" tab (charts) behind the inherited premium gate; the Metrics tab lazily fetches `metrics-history` on first open (Decision 5) |

## Wave: DESIGN / [REF] Driving ports (DESIGN — supersedes the DISCUSS two-endpoint listing)

ONE endpoint (ADR-050, supersedes the DISCUSS table that listed both `metrics-history` and `metrics-history/forecast`):

| Method | Route | Auth | Status |
|---|---|---|---|
| GET | `/api/v1/deliveries/{deliveryId:int}/metrics-history` (+ `api/latest/…`) | `[RbacGuard(PortfolioRead)]` | NEW (Slice 1; Slices 2-4 add nullable series, no new route) |

The DISCUSS `metrics-history/forecast` sibling is **folded into** this endpoint as additional (initially-null) series — see Decision 3 and `design/upstream-changes.md`.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IDeliveryMetricSnapshotRepository : IRepository<DeliveryMetricSnapshot>` | `DeliveryMetricSnapshotRepository` (EF) | CREATE NEW |
| `IForecastService` / fresh `Feature.Forecasts` (recorder source) | `ForecastService` | REUSE AS-IS |
| `Feature.FeatureWork` / `Feature.EstimatedSize` (recorder's current count + inferred-estimate source) | existing model / repositories | REUSE AS-IS (read-only) |
| `IDomainEventDispatcher.PublishAsync` (dispatch `PortfolioForecastsUpdated`) / `IDomainEventHandler<TEvent>` (the recorder reacts) | `DomainEventDispatcher` + the new `DeliveryMetricSnapshotRecordingHandler` | EXTEND (existing Epic 5121 / ADR-027 bus) |

No external integration. No contract-test recommendation at the platform-architect handoff (FE↔BE contract probed by the Zod schema).

## Wave: DESIGN / [REF] Technology choices (pinned)

ASP.NET Core .NET 8 (MIT); EF Core 8.x (MIT, migration via `CreateMigration` across Sqlite/Postgres); NUnit 4.6 + Moq + EF InMemory + `Microsoft.AspNetCore.Mvc.Testing` (migration test on a REAL provider); Stryker.NET ≥80%; React 18 (MIT) + TypeScript 5.x strict; MUI + MUI-X-charts `LineChart` (MIT, reuse the `StackedAreaChart` area+line idiom); Zod (MIT) at the trust boundary; Vitest + RTL; Stryker TS ≥80%; Playwright POM. NO new dependency.

## Wave: DESIGN / [REF] Decisions table (PROPOSED)

| # | Decision | Recommendation | ADR |
|---|---|---|---|
| 1 | Recorder trigger | Event-driven: NEW `PortfolioForecastsUpdated` event dispatched after `UpdateForecastsForPortfolio` + write-back in `PortfolioUpdater`/`ForecastUpdater`; recorder = `DeliveryMetricSnapshotRecordingHandler : IDomainEventHandler<…>` (the sole feed). NOT an inline updater step; NOT the pre-forecast `PortfolioFeaturesRefreshed` (stale) | ADR-049 |
| 2 | Snapshot schema granularity | One wide row per (delivery, day), nullable forward columns, JSON when-distribution | ADR-048 / ADR-050 |
| 3 | Endpoint shape | ONE `metrics-history` endpoint, all series, forward-only forecasting fields null until accrued | ADR-050 |
| 5 | Chart placement | TABS inside the per-delivery `DeliverySection` accordion — a "Work Items" tab (existing grid) and a "Metrics" tab (charts, the lazy fetch trigger); when-distribution is a toggle on the predictability chart | brief / D12 |

*(Former Decisions 4 "Backfill execution model" and 6 "Backfill done source" are removed: the user dropped the backfill on 2026-06-02 — the store is fed by the forward recorder only, so there is no backfill execution model and no historical-done reconstruction question. Re-opens are handled naturally because each day records the then-current count. See `## Changed Assumptions` and `design/upstream-changes.md`.)*

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | Verdict | Evidence / justification |
|---|---|---|
| EF persistence (`IRepository<T>` + EF adapter, `RefreshLog`/`Delivery` DbSet pattern) | EXTEND | New `DeliveryMetricSnapshot` DbSet + repository follow the exact existing pattern; no new ORM |
| `ForecastUpdater` / `PortfolioUpdater` / `UpdateServiceBase<Portfolio>` cadence | EXTEND | Both dispatch the new `PortfolioForecastsUpdated` event after the forecast update + write-back (ADR-049); no new background service, no second cadence |
| Domain-event infra (`IDomainEvent`, `IDomainEventDispatcher`, `IDomainEventHandler<TEvent>`, `DomainEventDispatcher`) + the `IDomainEventHandler` reaction pattern | EXTEND | Recorder is a handler on the existing Epic 5121 / ADR-027 bus, modeled on the precedent `PortfolioFeaturesRefreshedMetricsInvalidationHandler` (metrics invalidation) and `TeamDeletedRefreshLogCleanupHandler` (cleanup-on-delete) |
| `PortfolioForecastsUpdated` event + `DeliveryMetricSnapshotRecordingHandler` | CREATE NEW | No existing "forecasts updated" event — `PortfolioFeaturesRefreshed` fires pre-forecast (line 73, before recompute/forecast at 76/82/84) so reusing it would record STALE forecast/likelihood. The new event fires post-forecast; justified by this freshness gap (ADR-049) |
| `DeliveryWithLikelihoodDto.FromDelivery` projection | EXTEND (reuse) | Recorder reuses this projection for forward figures — DRY of the metric KNOWLEDGE |
| `IForecastService` + `Feature.Forecasts` / `Feature.EstimatedSize` / `Feature.FeatureWork` | REUSE AS-IS | Recorder reads fresh forecasts + current counts + inferred size; no change to forecasting |
| `LighthouseAppContext` JSON value-converter pattern | EXTEND | `WhenDistributionJson` reuses the `AdditionalFieldValues`/`StateMappings` converter idiom |
| MUI-X `LineChart` / `StackedAreaChart` area+line idiom | EXTEND (pattern) | Burnup/predictability reuse the area+line, `scaleType:"time"` shape |
| `getLikelihoodLevel` / `ForecastLevel` RAG thresholds | REUSE AS-IS | Predictability RAG bands |
| `useRbac()` + `useLicenseRestrictions`/`canUsePremiumFeatures` gate | REUSE AS-IS | Read gating + premium gating inherited; no new auth/premium surface |
| `DeliverySection` accordion surface | EXTEND | Charts render in a "Metrics" tab inside `AccordionDetails` (Decision 5) |
| `DeliveryMetricSnapshot` store + forward recorder | CREATE NEW | No delivery time-series persistence exists (verified `LighthouseAppContext`); justified vs the `RefreshLog`/`UpdateServiceBase` persisted-recorder analog. Forward-only — no backfill component |
| `DeliveryBurnupChart` / `DeliveryPredictabilityChart` / `DeliveryFeverChart` | CREATE NEW | Existing charts answer different questions over different units (run-charts/scatter/aging); delivery-count-over-calendar-time against a target-date marker is new — justified vs the `state-time-cumulative-view` new-chart precedent |
| `metrics-history` endpoint | CREATE NEW | No existing delivery time-series endpoint; mirrors `DeliveriesController` + metrics-endpoint precedent |

Net: EXTEND-heavy persistence/recorder/chart-idiom reuse; the two unavoidable CREATE-NEWs (store/recorder, charts) are justified against the closest in-repo analogs.

## Wave: DESIGN / [REF] Open questions (for DISTILL / DELIVER)

- **Per-day row volume** — Slice-1 SPIKE (optional) to confirm the (active-deliveries × elapsed-days) row count stays acceptable as snapshots accrue; retention follow-up if not.
- **Endpoint host** — extend `DeliveriesController` vs a new `DeliveryMetricsController`; DELIVER decides on controller cohesion (no architectural impact).
- **`PortfolioForecastsUpdated` dispatch-once invariant** — DELIVER must verify the event dispatches exactly once per portfolio-forecast-completion on BOTH `PortfolioUpdater.Update` and `ForecastUpdater.Update` (no missing dispatch, no double dispatch). Covered by the dispatch probe in ADR-049; flagged here because the two update paths must stay in sync.

## Wave: DISTILL / [REF] Acceptance designer + status

Acceptance Designer: Sentinel (nw-acceptance-designer), C#/.NET + React + Playwright polyglot rows of the ATDD Infrastructure Policy. Date: 2026-06-02. This is NOT the Python pilot: NO pytest-bdd / Hypothesis / `assert_state_delta` / `__SCAFFOLD__` stubs. Backend ATs are black-box example-based via `WebApplicationFactory<Program>` (skip = `[Ignore("pending — DELIVER (delivery-metrics)")]`); E2E is Playwright POM (skip = `test.fixme`); FE Vitest+RTL component tests are DELIVER's job. The `.feature` files under `docs/feature/delivery-metrics/acceptance/` are the scenario SSOT. The outcomes registry is skipped (no `nwave-ai` CLI in this repo).

Prior-wave reading confirmation:

- `+ docs/architecture/atdd-infrastructure-policy.md`
- `+ docs/feature/delivery-metrics/feature-delta.md` (DISCUSS + DESIGN, D1-D12, Changed Assumptions, US-01/01b/03/04/05)
- `+ docs/product/journeys/delivery-metrics.yaml` (steps + error_paths_summary + the two predictability views)
- `+ docs/feature/delivery-metrics/design/wave-decisions.md`
- `+ docs/product/architecture/adr-048..050`
- `+ docs/feature/delivery-metrics/slices/slice-01..05`
- `+ docs/product/kpi-contracts.yaml` (soft — no delivery-metrics rows; `@kpi` tagged against KPI 4 recorder-correctness, the only backend-measurable one)
- `- docs/feature/delivery-metrics/discuss/wave-decisions.md` (not found — single-file model; DISCUSS decisions live in the feature-delta DISCUSS sections + journey)
- `- docs/feature/delivery-metrics/devops/` (not found — no DEVOPS wave; using the ATDD policy defaults, no env matrix)

WARN: no `discuss/wave-decisions.md` and no `devops/` directory — single-file model. Reconciliation ran against the feature-delta DISCUSS sections + DESIGN wave-decisions + journey instead.

## Wave: DISTILL / [REF] Reconciliation result

Reconciliation passed — 0 NEW contradictions. The four big upstream changes (backfill DROPPED → forward-only every series; ONE consolidated `metrics-history` endpoint; event-driven recorder via the NEW `PortfolioForecastsUpdated` event + `DeliveryMetricSnapshotRecordingHandler`; charts in a "Metrics" tab inside the `DeliverySection` accordion; forecast pinned to Monte-Carlo percentiles 85% how-many / 70% when) are ALL already resolved and documented in `## Changed Assumptions` + `design/upstream-changes.md`. Scenarios were written against these resolved decisions, not blocked on them.

## Wave: DISTILL / [REF] Scenario list with tags

Spec SSOT = the `.feature` files under `docs/feature/delivery-metrics/acceptance/`. 28 scenarios across 7 files.

| File | Scenario | Tags |
|---|---|---|
| walking-skeleton.feature | Forecaster opens a delivery's Metrics tab and sees the backlog and done lines | `@walking_skeleton @driving_adapter @US-01 @premium @real-io` |
| milestone-1-metrics-history-and-recorder.feature | Endpoint returns the recorded backlog and done series in date order | `@US-01 @real-io @driving_adapter` |
| | Empty store yields an honest empty series, not an error | `@US-01 @real-io @driving_adapter @error` |
| | Recorder upserts one row per delivery per day | `@US-01 @real-io @kpi` |
| | Re-handling the same day is idempotent on delivery and date | `@US-01 @real-io @kpi` |
| | A re-opened item lowers the next recorded done count | `@US-01 @real-io` |
| | Deleting a delivery cascades away its snapshot rows | `@US-01 @real-io @error` |
| | A non-premium instance does not expose the history | `@US-01 @real-io @error @premium` |
| | A portfolio viewer can read the history | `@US-01 @real-io @rbac @driving_adapter` |
| milestone-2-inferred-estimate.feature | Not-yet-broken-down feature contributes its estimated size | `@US-01b @real-io @kpi` |
| | A fully broken-down delivery records no estimated portion | `@US-01b @real-io` |
| | Before any inferred estimate only the actual-item series is present | `@US-01b @real-io @error` |
| milestone-3-forecast-over-time.feature | Endpoint returns the recorded forecast-how-many series | `@US-03 @real-io @driving_adapter` |
| | On-track delivery reads differently from at-risk | `@US-03 @real-io` |
| | Sparse forecast series is annotated as building forward | `@US-03 @real-io @error` |
| | A delivery with no forecast yet exposes a null forecast series | `@US-03 @real-io @error` |
| milestone-4-predictability-trend.feature | Endpoint returns the likelihood-over-time series | `@US-04 @real-io @driving_adapter` |
| | When view returns completion-date percentiles vs the target date | `@US-04 @real-io @driving_adapter` |
| | Improving delivery reads differently from a degrading one | `@US-04 @real-io` |
| | Sparse/no predictability data renders consistently with the forecast view | `@US-04 @real-io @error` |
| milestone-5-fever-chart.feature | Bubble trails through risk zones over snapshots | `@US-05 @stretch @real-io` |
| | At-risk delivery's trail enters the red zone | `@US-05 @stretch @real-io` |
| | No/sparse snapshots shows no bubble | `@US-05 @stretch @real-io @error` |
| integration-checkpoints.feature | Recorder records fresh post-forecast figures, not stale | `@integration @US-01 @real-io @kpi` |
| | Forecasts-updated event dispatched once per completion | `@integration @US-01 @real-io @kpi` |
| | One endpoint carries every series | `@integration @US-01 @real-io @driving_adapter` |
| | Read gated by premium and portfolio read access | `@integration @US-01 @real-io @premium @rbac @error` |

Error/edge ratio: 9 of 28 carry `@error` (32%); counting the gated read + premium + RBAC negative-access checks (3 more) lifts the negative/edge surface to 12 of 28 (43%) — above the 40% bar. Excluding the 3 stretch Slice-5 scenarios (out of committed MVP), the committed surface is 25 scenarios with 11 negative/edge = 44%.

## Wave: DISTILL / [REF] WS strategy

Architecture-of-Reference treatment (NOT the retired A/B/C/D choice): driving adapters use the real adapter (E2E = Playwright against the real React app; backend endpoint = `WebApplicationFactory<Program>`); the driven-internal EF store uses the real EF context per the Project Infrastructure Policy; the driven-external `ILicenseService` is faked (`Mock<ILicenseService>`). This matches the existing policy rows exactly — no new policy rows needed.

ONE `@walking_skeleton @driving_adapter` E2E scenario (`walking-skeleton.feature` / `DeliveryMetrics.spec.ts`): open portfolio → delivery → "Metrics" tab → burnup renders backlog + done over time → switch back to "Work Items". Demo-data driven (`loadDemoScenario`), premium instance. Demoable to a stakeholder ("can a forecaster open a delivery and see its history over time?"). Forward-only honesty: a fresh demo instance has no snapshot history — see Pre-requisites.

## Wave: DISTILL / [REF] Adapter coverage table

Every driven adapter mapped to at least one `@real-io` acceptance test.

| Driven adapter | `@real-io` scenario | Covered by |
|---|---|---|
| `DeliveryMetricSnapshot` EF repository (`IDeliveryMetricSnapshotRepository`) | YES | `DeliveryMetricsHistoryReadApiIntegrationTest` (seeds rows via real EF context, reads via endpoint); `DeliveryMetricSnapshotCascadeDeleteIntegrationTest` (FK ON DELETE CASCADE on a real provider) |
| `DeliveryMetricSnapshotRecordingHandler` (reacts to `PortfolioForecastsUpdated`) | YES | `DeliveryMetricSnapshotRecordingHandlerTest` (upsert, idempotency-on-date, re-open lowers done, post-forecast freshness, inferred estimate) — real EF context via `TestWebApplicationFactory` scope |
| metrics-history endpoint (driving adapter, `GET /api/latest/deliveries/{id}/metrics-history`) | YES | `DeliveryMetricsHistoryReadApiIntegrationTest` (series shape, ordering, empty, RBAC viewer, premium gate, consolidated series) via `WebApplicationFactory<Program>` |
| `PortfolioForecastsUpdated` dispatch sites (`PortfolioUpdater`/`ForecastUpdater`) | YES | integration-checkpoints.feature dispatch-once + freshness scenarios (the dispatch probe ADR-049 flags for DELIVER) |
| `ILicenseService` (driven external — faked, not real) | n/a (fake by policy) | `Mock<ILicenseService>` premium gate scenarios |

## Wave: DISTILL / [REF] Scaffolds

NO production scaffolds are needed. Backend ATs hit HTTP routes through the client, so they COMPILE even though the endpoint does not exist yet (they would get 404) — and they are `[Ignore]`d, so the suite stays GREEN/compiles, not BROKEN. The C#/TS rows of the polyglot matrix use `[Ignore]` / `test.fixme` as skip markers; there is no `__SCAFFOLD__` mechanism here (sibling `state-time-cumulative-view` precedent). The few private fixture/parse helpers in the NUnit ATs throw `AssertionException("pending — …")` so that if a test were un-ignored prematurely it fails RED (assertion), never BROKEN (compile/infra).

Skip-marked files (RED-not-BROKEN by design):

- `Lighthouse.Backend.Tests/API/Integration/DeliveryMetricsHistoryReadApiIntegrationTest.cs` — `[Ignore("pending — DELIVER (delivery-metrics)")]`
- `Lighthouse.Backend.Tests/API/Integration/DeliveryMetricSnapshotCascadeDeleteIntegrationTest.cs` — `[Ignore(...)]`
- `Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandlerTest.cs` — `[Ignore(...)]`
- `Lighthouse.EndToEndTests/tests/specs/portfolios/DeliveryMetrics.spec.ts` — `test.fixme`

## Wave: DISTILL / [REF] Test placement

| Test | Path | Precedent |
|---|---|---|
| metrics-history read endpoint AT | `Lighthouse.Backend.Tests/API/Integration/DeliveryMetricsHistoryReadApiIntegrationTest.cs` | mirrors `CumulativeStateTimeReadApiIntegrationTest` (sibling read-API AT, `WebApplicationFactory` + `EnsureDeleted/EnsureCreated` + `As*` auth + `Mock<ILicenseService>`) |
| cascade-delete AT | `Lighthouse.Backend.Tests/API/Integration/DeliveryMetricSnapshotCascadeDeleteIntegrationTest.cs` | mirrors `DeliveriesControllerIntegrationTest` (`IntegrationTestBase`, delivery lifecycle through the controller) |
| recorder / event-handler AT | `Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandlerTest.cs` | sits beside `PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest` + `TeamDeletedRefreshLogCleanupHandlerTest` (the precedent domain-event handler tests, Epic 5121 / ADR-027) |
| E2E walking skeleton spec + POM | `Lighthouse.EndToEndTests/tests/specs/portfolios/DeliveryMetrics.spec.ts` + `tests/models/portfolios/Deliveries/DeliveryMetricsTab.ts` | mirrors `CumulativeStateTime.spec.ts` (demo-data, POM-only) + the existing `Deliveries/` POM family (`DeliveriesPage`, `DeliveryItem`) |

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving adapter | Exercised by |
|---|---|
| `GET /api/latest/deliveries/{deliveryId}/metrics-history` (HTTP) | `DeliveryMetricsHistoryReadApiIntegrationTest` via `WebApplicationFactory<Program>` — status, body shape, ordering, empty series, RBAC (`AsPortfolioViewer`), premium gate (`Mock<ILicenseService>`) |
| React Metrics tab / burnup chart (production app) | `DeliveryMetrics.spec.ts` via Playwright POM against the real app, demo data |

## Wave: DISTILL / [REF] Pre-requisites

- **DELIVER must extend the demo-data seeding with `DeliveryMetricSnapshot` rows** so the E2E walking skeleton renders a POPULATED burnup. A fresh demo instance is forward-only and has ZERO snapshot history (D6/D11) — the chart would otherwise show only the empty state. Reuse the demo-data-time-in-state CSV-column precedent (MEMORY `project_demo_data_time_in_state_via_csv_column`) for seeding deterministic snapshot history. This is the single hard pre-requisite for the WS to be green-able.
- DESIGN driving port: ONE `metrics-history` endpoint (ADR-050), `[RbacGuard(PortfolioRead)]`, `api/v1` + `api/latest`.
- EF migration generated via the `CreateMigration` PowerShell script across Sqlite + Postgres (the migration applies-on-real-provider check is DELIVER's, per the persisted-model EF-migration trap).
- No DEVOPS environment matrix (no DEVOPS wave) — the ATDD policy defaults govern (Sqlite + Postgres lockstep in CI for the EF ATs).

## Wave: DISTILL / [REF] Consolidated review gate — DELIVER action items

The mandatory 4-reviewer gate (2026-06-02) returned: DISCUSS approved, DISTILL approved, DESIGN + ops-readiness conditionally-approved. Zero hard blockers; all conditions are DELIVER-scope action items, carried here so DELIVER inherits them:

1. **Dispatch-once invariant (high)** — `ForecastUpdater` does NOT currently depend on `IDomainEventDispatcher`. DELIVER must add it and dispatch `PortfolioForecastsUpdated` after the forecast write-back on BOTH `PortfolioUpdater.Update` AND `ForecastUpdater.Update`, with a gated integration test asserting exactly one dispatch per portfolio-forecast-completion (no missing dispatch → KPI-4 silent gap; no double dispatch). Covered by the `integration-checkpoints` dispatch scenario — un-ignore + implement.
2. **Unique index (medium)** — the Slice-1 EF migration MUST create a unique index on `(DeliveryId, RecordedAt)`; a test triggers the constraint violation to prove the idempotency backstop exists at the DB.
3. **Demo-snapshot seeding (medium)** — extend demo data with `DeliveryMetricSnapshot` rows so the E2E WS renders a populated burnup (the single WS green-able pre-req above).
4. **EF migration via `CreateMigration` (high)** — add to the pre-push checklist: regenerate Sqlite + Postgres migrations after model changes (persisted-model EF-migration trap; InMemory tests miss it).
5. **Recorder observability (the one net-new finding, right-sized)** — instrument `DeliveryMetricSnapshotRecordingHandler` with structured logging (duration + per-portfolio daily snapshot count + exception logging) so KPI-4 coverage/idempotency is inspectable. Full dashboards/alerts are deferred — cross-instance telemetry is blocked on Epic 5015 (`project_self_hosted_telemetry_gap`); the KPI-4 backend integration assertions cover correctness today.
6. **Row-volume / retention (low)** — one row per delivery per day grows unbounded; quantify in the Slice-1 SPIKE and note a retention follow-up (post-MVP). Not an MVP blocker.
7. **Launch runbook note (low)** — forward-only means an empty burnup on day 1; the KPI 1-3 window starts once usable history accrues (~2-4 weeks), not at launch. Empty state is expected, not a defect.

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#D1/D11 | Every series is forward-recorded into ONE `DeliveryMetricSnapshot` store; no backfill, no reconstruction | n/a | ATs seed snapshots directly for read tests and publish `PortfolioForecastsUpdated` for recorder tests; the E2E needs demo snapshot seeding because a fresh instance has no history |
| DISCUSS#D4 | Premium gating inherited from the existing delivery surface | n/a | A `@premium` AT asserts a non-premium instance does not expose the metrics history (`Mock<ILicenseService>` returns false) |
| DISCUSS#D6 | Forward-only empty state is universal — the burnup too | n/a | An `@error` AT asserts an empty store yields an honest empty series (no points, no first-snapshot date), never an error or zero |
| DESIGN#ADR-049 | Recorder is event-driven on `PortfolioForecastsUpdated`, idempotent on `(deliveryId, recordedAt.Date)`, records post-forecast fresh figures | ADR-049 | Recorder ATs cover upsert, same-day idempotency, re-open lowering done, post-forecast freshness, and dispatch-once on both update paths |
| DESIGN#ADR-050 | ONE consolidated `metrics-history` endpoint carries every series; forward fields null until accrued | ADR-050 | One read AT asserts the consolidated response carries backlog/done/estimated/forecast/likelihood/when-distribution; `@error` ATs assert null forward series before accrual |
| DESIGN#ADR-048 | `DeliveryMetricSnapshot` FK to `Delivery` is `ON DELETE CASCADE` | ADR-048 | A cascade-delete AT asserts no snapshot rows remain after a delivery is deleted |
| DISCUSS#D9 | Fever chart is STRETCH, out of committed MVP | n/a | Milestone-5 scenarios are `@stretch` and excluded from the committed DELIVER scope (kept `[Ignore]`/`test.fixme`) |


## Wave: DELIVER / [WHY] Upstream Issues

### UI-1 (DESIGN gap, surfaced Slice 1 / step 01-03) — service-layer recorder cannot reuse `API.DTO.DeliveryWithLikelihoodDto.FromDelivery`

**Origin**: DESIGN Reuse Analysis + ADR-049 both direct the `DeliveryMetricSnapshotRecordingHandler` (a service-layer domain-event handler) to reuse `API.DTO.DeliveryWithLikelihoodDto.FromDelivery` for "DRY of the metric KNOWLEDGE."

**Issue**: that projection lives in `API.DTO`, and the service layer importing it trips the `ServiceLayer_DoesNotDependOnApiLayer` ArchUnitNET boundary gate (the core must not depend on the API layer; DTOs the core returns belong in `Models.*`). The reuse recommendation is architecturally infeasible as written.

**Slice-1 resolution (applied)**: the recorder computes the two Slice-1 counts (`TotalWork`/`RemainingWork` summed over `Delivery.Features`'s `FeatureWork`, `DoneWork = TotalWork - RemainingWork`) directly from the `Delivery` domain model — boundary-respecting, and the only figures Slice 1 records.

**Carried risk for Slices 3-4 (RESOLVE in DESIGN before those slices)**: the forecast-how-many (US-03) and likelihood/when-distribution (US-04) figures the recorder must record forward ARE computed inside `DeliveryWithLikelihoodDto.FromDelivery`. The same boundary will block reusing them. DESIGN must decide where the shared forecast/likelihood projection lives so both the API DTO and the service-layer recorder can call it without the service→API dependency — e.g. extract the projection into a `Models.*`/domain or a service-layer projection that `DeliveryWithLikelihoodDto` also delegates to.

**RESOLUTION (user decision, 2026-06-02 — code-verified)**: NOT a real architectural wall — `DeliveryWithLikelihoodDto.FromDelivery` is only an API-shaping orchestrator. The metric KNOWLEDGE already lives on the domain models: work counts are pure iteration over `Delivery.Features[].FeatureWork`, per-feature likelihood is `Feature.GetLikelhoodForDate(date)` (Models/Feature.cs), and forecast percentiles live on `Models/Forecast/*` (`HowManyForecast`/`WhenForecast`); `CreateForecastDtos` merely shapes those into API DTOs. The only genuinely-shared business rule in the DTO is `GetLeastLikelyFeature` (delivery likelihood = its least-likely feature's). **Chosen fix = extract a shared domain projection at Slice 3** (Option A): pull the delivery-level derivation (work sums + least-likely-feature likelihood + forecast-percentile selection) into a `Models.*` domain method/service (e.g. `Delivery.CalculateMetrics()` or a `DeliveryMetricsCalculator`); both `DeliveryWithLikelihoodDto.FromDelivery` AND the recorder delegate to it — true single source of truth, ArchUnit-clean (service→Models is allowed). Done lazily when Slice 3 first records `ForecastHowMany`/`LikelihoodPercentage` (Slice 1 records only counts, so the current ~6-line count duplication is acceptable until then). ADR-049's "reuse `DeliveryWithLikelihoodDto.FromDelivery`" wording is superseded by this (reuse the underlying domain projection, not the API DTO).

**Item 2 — `WhenDistributionJson` storage (user decision, 2026-06-02)**: keep the raw `string?` column for Slice 1. Switching to a typed value-converted property (`IReadOnlyList<WhenDistributionPoint>?` mirroring the `AdditionalFieldValues`/`StateMappings` converter idiom) later is a model-only change with NO migration (the column stays `TEXT`/`nvarchar(max)` either way), so there is no lock-in. Decide in Slice 4 when the recorder first populates and the endpoint first reads it.

### UI-2 (DESIGN/ADR wording vs codebase reality, surfaced Slice 1 / step 03-01) — no Zod on the frontend

**Origin**: roadmap step 03-01 criteria + the ADR-050 wording specify a `deliveryMetricsHistorySchema` with a `z.infer`-derived type ("schema-first at the FE/BE trust boundary using Zod").

**Issue**: Zod is NOT a frontend dependency — zero imports anywhere in `Lighthouse.Frontend/src`, absent from `package.json`. Introducing it for one model would add an unapproved dependency and break the "match the surrounding code" rule. The CLAUDE.md "use Zod" guidance is aspirational and does not reflect this codebase.

**Resolution (applied)**: `src/models/Delivery/DeliveryMetricsHistory.ts` uses the project's native runtime-parse idiom — `parseDeliveryMetricsHistory(value: unknown)` narrowing through typed `asObject`/`asNumber`/`asDate` guards that throw `BoundaryError` on contract violation, with the type derived from the parsed shape. This satisfies the schema-first-at-boundary INTENT (validated parse, type derived) without Zod, and was proven against the real wire format by the green live E2E. Do not "fix" this to Zod. Future FE waves on this feature should follow the same native-parser idiom unless Zod is adopted project-wide.

### UI-3 (DESIGN gap, surfaced Slice 1 / step 03-02) — demo data seeds no deliveries

**Origin**: roadmap step 03-02 pointed demo-snapshot seeding at `Services/Implementation/DemoData/` and assumed a demo delivery exists to attach snapshots to.

**Issue**: (a) the demo-data code lives at `Factories/DemoData` (CSVs) + `Services/Implementation/DemoDataService.cs`, not the cited path; (b) `DemoDataService` seeds only portfolios + teams — it creates NO deliveries at all (`new Delivery(...)` existed only in `DeliveriesController`). A forward-only burnup needs a delivery AND a multi-day snapshot series, and the recorder only produces one row/day, so a populated demo burnup is impossible without deterministic seeding.

**Resolution (applied)**: `DemoDataService` now seeds a rule-based demo delivery **"Apollo Release"** in portfolio **"Project Apollo"** (scenario 0 = `EpicForecast`) — rule `feature.name isnotempty` so `PortfolioUpdater` auto-links the synced demo features — plus 14 deterministic past-day `DeliveryMetricSnapshot` rows so the walking-skeleton burnup renders populated. Ctor gained `IDeliveryRepository` + `IDeliveryMetricSnapshotRepository` (both already DI-registered; 6 params, S107-safe).

### UI-4 (test-tooling note, surfaced Slice 1 / step 03-02) — MUI x-charts 9.0.1 line-series selector

**Origin**: the pre-scaffolded `DeliveryMetricsTab` POM targeted `path.MuiLineElement-root` for burnup series lines.

**Issue**: in MUI x-charts 9.0.1 the rendered line-series path class is `path.MuiLineChart-line`, not `MuiLineElement-root` — the original selector matched nothing. Also two "Metrics" tabs coexist (portfolio-level + delivery-level), so POM tab lookups must be scoped to the `delivery view tabs` tablist to avoid a strict-mode violation. Both fixed in the POM; recorded here so sibling chart E2Es reuse the correct selector.
