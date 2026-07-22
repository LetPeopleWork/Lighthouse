# Feature: Percentiles Over Time (Epic 5427)

**Epic**: ADO 5427 — "Show Percentiles over Time Charts" (Community / Productboard)
**Feature id**: `epic-5427-percentiles-over-time`
**Wave**: DISCUSS (complete) → DESIGN next
**Density**: lean (Tier-1 [REF] only; expansions on demand via `--expand <id>`)

---

## Wave: DISCUSS / [REF] Pre-requisites

- **Snapshot precedent exists**: `DeliveryMetricSnapshot` (Epic 3993) and `BlockedCountSnapshot` (Epic 5074)
  are forward-only, one-row-per-day metric snapshot entities written on a refresh/domain event. This
  epic reuses that exact pattern — no new persistence paradigm.
- **Point-in-time metrics already computed**: CT percentiles (`percentiles` widget), WIA percentiles
  (`workItemAgePercentiles`), and PBC NPLs (`throughputPbc`/`cycleTimePbc`/… in the Predictability
  category) all exist as *current-window* computations. Their only "trend" today is `previous-period`
  (a single up/down arrow comparing two equal windows — `getTrendPolicy` in `categoryMetadata.ts`).
  **No daily percentile/PBC persistence exists** (verified: only two `*Snapshot` DbSets).
- **Domain-event bus** (Epic 5121) is the recording trigger: metric refresh raises an event, a handler
  records the fresh numbers, latest-write-wins per calendar day. Mind dispatch-freshness/ordering
  (known gotcha: dispatcher swallows handler errors — recording must be resilient and idempotent-per-day).
- **Multiple-cycle-times & RBAC** are untouched: this feature is **free-tier, ungated** (Decision D3).

## Wave: DISCUSS / [REF] Personas

| Persona ID | One-line identifier |
|---|---|
| `flow-coach` | Runs periodic flow/ops reviews for a team or train; asks "is our predictability firming up or slipping?" — **primary**. |
| `delivery-lead-rte` | Cumulative/systemic lens; reads process-behaviour (NPL) stability over time across the portfolio — **secondary (PBC-over-time)**. |
| `product-owner` | Consumes the same charts in reviews; no bespoke view — tertiary, no dedicated story. |

## Wave: DISCUSS / [REF] JTBD one-liners

- **job-flow-coach-see-predictability-trend** — *When I run a periodic flow/ops review, I want to see how
  our cycle-time and work-item-age percentiles have moved over the last N days, so I can tell whether our
  predictability is firming up or degrading — and show it, not assert it.*
- **job-delivery-lead-see-process-stability-trend** — *When I review a team/portfolio's process health, I
  want to see how the PBC natural process limits (UNPL/Average/LNPL) have moved day by day, so I can spot
  a genuine process shift over time rather than reacting to a single out-of-limit point.*

Both are the flow-metric siblings of `job-forecast-delivery-trend-over-time` (delivery-metrics): honest
*trend* story over a forward-only snapshot, not a point-in-time number.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict | Source |
|---|---|---|---|
| D1 | Widget UX | **Combined CT+WIA "Percentiles Over Time" widget** with toggle rows `[ WIA \| CT-30 \| CT-60 \| CT-90 ]`; PBC-over-time is a **separate** widget with a metric-type toggle. | User (AskUserQuestion) |
| D2 | Walking skeleton / first slice | **Cycle Time percentiles over time** — proves the daily-snapshot pipeline **and** the 30/60/90 horizon dimension **and** the combined-widget shell in one thin slice. | User |
| D3 | License gating | **Free-tier, ungated** — same as the sibling point-in-time PBC/percentile widgets. No premium/RBAC change. | User |
| D4 | CT lookback horizons | **30/60/90 snapshotted daily**, widget toggles between them (mirrors the WIA↔CT toggle in the Aging chart). | User |
| D5 | Persistence model | **Forward-only, latest-write-wins per calendar day**, one snapshot row per (metric, horizon, day). Event-driven on refresh. Reuses `DeliveryMetricSnapshot`/`BlockedCountSnapshot` shape. | Epic body + precedent |
| D6 | Empty-state honesty | Over-time charts are forward-only; on a fresh team they read *"builds forward from today — no snapshots recorded yet"*, never a broken/empty chart. Same contract as delivery-metrics forecast trend. | Precedent (job-forecast-delivery-trend) |
| D7 | Colouring | CT/WIA percentile lines keep the existing **red→green** percentile colour ramp (50/70/85/95). PBC keeps UNPL/Average/LNPL styling. Consistency, no new visual language. | Epic body |
| D8 | Placement | Combined percentiles-over-time widget and PBC-over-time widget both live in the **Predictability** category (`categoryMetadata.ts`), team **and** portfolio scope. Feature-Size variants stay `portfolio-only`. | Epic body |

## Wave: DISCUSS / [REF] Scope Assessment: SPLIT (user-approved)

Oversized signals fired (≥2): three metric families (CT, WIA, PBC×6 types) · new persistence + event
recording · multiple widgets · >1 week effort. **Split into 4 elephant-carpaccio slices** (below). Each
ships end-to-end in ≤1 day, each with a named learning hypothesis, each with a value-bearing story.
The shared snapshot-recording pipeline is a new abstraction all slices need — per the taste test it is
**not** shipped as an @infrastructure-only slice (that would trip the hard gate); it lands **inside**
Slice 1 alongside the user-visible CT chart.

## Wave: DISCUSS / [REF] Story Map + WS strategy

**Backbone**: Record daily metric snapshot → Serve over-time series → Render over-time chart → Read the trend.

**WS strategy = A (thinnest end-to-end vertical)**: Slice 1 (CT percentiles over time) walks the full
backbone with the least incidental complexity beyond the horizon dimension.

| Slice | Story | Type | Ships |
|---|---|---|---|
| 01 | US-01 CT percentiles over time (widget shell + 30/60/90 toggle) | value | combined widget (CT tabs only), snapshot entity, event recorder, HTTP series endpoint |
| 01 | US-02 forward-only daily snapshot recording | `@infrastructure` (lands within slice 01) | event handler, latest-per-day write, migration |
| 02 | US-03 WIA percentiles over time (WIA tab) | value | WIA snapshot + WIA tab on the combined widget |
| 03 | US-04 Throughput PBC NPLs over time | value | PBC-over-time widget shell (Throughput active) |
| 04 | US-05 PBC over time — remaining type toggles | value | WIA/WIP/CT/Arrivals/Feature-Size(portfolio) toggle options |

Slice briefs: `docs/feature/epic-5427-percentiles-over-time/slices/slice-0{1..4}-*.md`.

## Wave: DISCUSS / [REF] User Stories

### US-01 — Cycle Time percentiles over time
`job_id: job-flow-coach-see-predictability-trend` · slice 01 · **value**

As a flow coach, I want a chart of my team's cycle-time percentiles (50/70/85/95) plotted day by day for
a chosen lookback horizon, so I can see whether cycle-time predictability is tightening or drifting.

#### Elevator Pitch
Before: I can see today's CT percentiles as four numbers, but not whether they're getting better or worse over time.
After: open **Team → Metrics → Predictability → "Percentiles Over Time"**, keep the default `CT-30` toggle → see four dated lines (50/70/85/95, red→green) trending across the range.
Decision enabled: decide whether last month's process change actually tightened cycle time, or just moved a single number.

#### Acceptance Criteria
- AC1: The combined "Percentiles Over Time" widget appears in the **Predictability** category for team and portfolio scope, with a toggle row `[ CT-30 | CT-60 | CT-90 ]` (WIA tab arrives in US-03).
- AC2: Selecting a CT horizon renders 50/70/85/95 percentile lines, one point per calendar day in the selected date range, using the existing red→green percentile colour ramp (D7).
- AC3: Each day's value is the CT percentile computed over that horizon **as of that day** — read from the persisted daily snapshot, not recomputed live for historical days.
- AC4: On a team with no snapshots yet, the widget shows the honest empty state *"builds forward from today — no snapshots recorded yet"* (D6), never a broken axis.
- AC5: Switching `30 ↔ 60 ↔ 90` re-plots from already-persisted horizon series without a backend recompute of history.

### US-02 — Forward-only daily snapshot recording `@infrastructure`
`job_id: job-flow-coach-see-predictability-trend` (enabler) · slice 01 · lands **within** slice 01

As the system, on each metrics refresh I record the fresh CT-percentile numbers for today, keeping only
the latest write per calendar day, so the over-time chart has one honest point per day going forward.

#### Acceptance Criteria
- AC1: A metrics-refresh domain event triggers a handler that records CT percentiles for horizons 30/60/90 for the current day.
- AC2: Re-running the refresh N times the same day overwrites (latest-write-wins) — exactly one row per (team, metric, horizon, day). *(Blast radius: verify with a same-day double-refresh test.)*
- AC3: Recording is forward-only — no historical backfill of real data; the series starts the day recording begins.
- AC4: A handler failure does not break the refresh path and is observable (recall: the dispatcher swallows handler errors — do not rely on it surfacing).
- AC5: EF migration is expand-only/additive (new table, no destructive change), generated via the `CreateMigration` script across all providers.

> Slice-composition gate: Slice 01 contains US-01 (value) + US-02 (`@infrastructure`) → passes (≥1 value story).

### US-03 — Work Item Age percentiles over time
`job_id: job-flow-coach-see-predictability-trend` · slice 02 · **value**

As a flow coach, I want a **WIA** tab on the same widget showing work-item-age percentiles (50/70/85/95)
day by day, so I read age and cycle-time predictability trends from one surface.

#### Elevator Pitch
Before: the over-time widget only shows cycle-time horizons; work-item-age trend isn't there.
After: click the **WIA** toggle on "Percentiles Over Time" → see 50/70/85/95 age percentiles trending day by day.
Decision enabled: decide whether in-progress work is ageing worse over time even when finished-item cycle time looks stable.

#### Acceptance Criteria
- AC1: The widget's toggle row becomes `[ WIA | CT-30 | CT-60 | CT-90 ]`; WIA has no horizon dimension (age is as-of-today).
- AC2: The WIA tab renders 50/70/85/95 daily lines from a persisted WIA-percentile daily snapshot, reusing the US-02 recording pipeline (no second bespoke pipeline).
- AC3: Empty-state and red→green colouring behave identically to the CT tabs (D6/D7).

### US-04 — Throughput PBC natural process limits over time
`job_id: job-delivery-lead-see-process-stability-trend` · slice 03 · **value**

As a delivery lead, I want a chart of the **Throughput** PBC's UNPL/Average/LNPL plotted day by day, so I
can see whether the natural process limits are shifting — a real process change vs a one-off signal.

#### Elevator Pitch
Before: the Throughput PBC shows one current set of limits; I can't see when the limits actually moved.
After: open **Predictability → "PBC Over Time"** (Throughput selected) → see UNPL / Average / LNPL as three dated lines.
Decision enabled: decide whether a process change genuinely shifted the limits, or the chart just caught a single special-cause point.

#### Acceptance Criteria
- AC1: A separate "PBC Over Time" widget appears in the Predictability category with a metric-type toggle showing **Throughput** (further types in US-05).
- AC2: Renders UNPL, Average, LNPL as three daily lines from a persisted PBC-NPL daily snapshot, reusing the US-02 recording pipeline.
- AC3: NPL styling matches the existing point-in-time PBC widgets (D7); empty state per D6.

### US-05 — PBC over time: remaining metric-type toggles
`job_id: job-delivery-lead-see-process-stability-trend` · slice 04 · **value**

As a delivery lead, I want the PBC-over-time widget to toggle across all PBC metric types, so I can read
limit-stability for whichever behaviour I'm reviewing.

#### Elevator Pitch
Before: PBC-over-time only covers Throughput.
After: use the type toggle to switch to **WIA / WIP / CT / Arrivals / Feature Size** → each shows its own UNPL/Average/LNPL over time.
Decision enabled: decide which behaviour's process limits are actually drifting, across the full PBC set.

#### Acceptance Criteria
- AC1: Toggle exposes Throughput, WIA, WIP, Cycle Time, Arrivals, and (portfolio-only) Feature Size.
- AC2: Each type reads its own persisted NPL daily series; Feature Size stays `portfolio-only` (D8).
- AC3: Adding a type does not alter the US-04 Throughput behaviour (regression-guarded).

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Recording correctness | Exactly 1 snapshot row per (team, metric, horizon, calendar day) under repeated same-day refresh | Backend integration test asserting row count after N refreshes = 1 |
| Pipeline reuse | ≥2 metric families (CT, WIA) + PBC share **one** recording pipeline (no per-metric bespoke recorder) | Code review + AT: single handler/table family drives all series |
| Trend readability | Flow coach identifies "firming vs drifting" from the chart in a 5-min review without export | Dogfood on a real Lighthouse team; qualitative confirm |
| Empty-state honesty | 0 charts render a broken/empty axis on a fresh team | E2E on a zero-snapshot team asserts the honest empty-state copy |
| Mutation kill rate | ≥80% backend + frontend on new snapshot/recording/series code | Stryker.NET + Stryker per feature (per-feature mandate) |

## Wave: DISCUSS / [REF] Definition of Done

1. All 5 user stories' ACs pass (US-02 within slice 01).
2. `dotnet build` zero warnings; `dotnet test` green; `pnpm test`/`pnpm build`/Biome clean.
3. New EF migration additive/expand-only, generated via `CreateMigration` across all providers.
4. Mutation testing ≥80% BE + FE on new code (per-feature).
5. Forward-only recording is idempotent-per-day and resilient to handler failure.
6. Empty-state honesty verified by E2E on a zero-snapshot team.
7. Demo data: a backfill handler backdates snapshots so demo/screenshot E2Es show populated charts
   (precedent: `DemoBlockedHistoryBackfillHandler`) — **not** shipped to real tenants.
8. Docs + per-feature screenshots at feature finalization (one `@screenshot` per theme; `rm` old PNG first).
9. SonarCloud gate: no new issues. ADO 5427 children mirrored + state-transitioned.

## Wave: DISCUSS / [REF] Out of scope

- Premium gating / RBAC changes (D3: free-tier).
- Backfilling **real** historical percentiles (forward-only by design, D5; only demo data is backdated).
- Overlaying percentile-over-time on the existing scatter/aging charts (dedicated widgets only, D1).
- Configurable per-team horizons beyond the fixed 30/60/90 set (D4).
- Alerting/thresholds on trend direction (read-only charts this epic).
- New export/CSV of the series.

## Wave: DISCUSS / [REF] Driving Ports (inbound surfaces)

- **HTTP**: `GET` team & portfolio metrics endpoints returning the over-time series (CT-by-horizon,
  WIA, PBC-NPL-by-type) — extend the existing MetricsController surface, do not fetch a new bespoke route from a component.
- **Domain event (inbound)**: metrics-refresh event → snapshot-recording handler (US-02).
- **UI actions**: "Percentiles Over Time" widget toggle (`WIA | CT-30 | CT-60 | CT-90`); "PBC Over Time"
  widget metric-type toggle — both via the existing MetricsView widget/hook plumbing.

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status |
|---|---|---|
| 1 | Job traceability | ✓ every story → real `job_id` (2 jobs added to `jobs.yaml`) |
| 2 | Elevator pitch per value story | ✓ US-01/03/04/05 (US-02 is `@infrastructure`, gated within a value slice) |
| 3 | Testable ACs | ✓ each AC verifiable end-to-end |
| 4 | Personas defined | ✓ flow-coach (primary), delivery-lead-rte (secondary) |
| 5 | Journey mapped | ✓ `docs/product/journeys/epic-5427-percentiles-over-time.yaml` |
| 6 | Slices ≤1 day, learning hypothesis each | ✓ 4 slice briefs |
| 7 | Outcome KPIs numeric | ✓ 5 KPIs with targets |
| 8 | Out-of-scope explicit | ✓ |
| 9 | No silent N/A | ✓ premium/RBAC/CLI-MCP-versioning explicitly N/A (free-tier, no client surface) |

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Primary need**: honest *trend* of flow-metric percentiles/NPLs over time, not a point-in-time number
  — the flow-metric sibling of the delivery-metrics over-time trend.
- **Feature type**: user-facing (new charts) + backend (forward-only snapshot persistence + event recorder).
- **Walking skeleton**: CT percentiles over time (D2) — full backbone, least incidental complexity.
- **Constraints**: forward-only latest-per-day persistence; free-tier; reuse `DeliveryMetricSnapshot`
  pattern + Epic 5121 event bus; empty-state honesty mandatory; expand-only migrations.
- **Upstream changes**: none — no DISCOVER/DIVERGE artifacts for this epic; SSOT extended additively.

## Next Wave

**Handoff → DESIGN** (`nw-solution-architect`) with full artifact set + **DEVOPS** (`nw-platform-architect`,
KPIs only). Key DESIGN questions: snapshot table shape (one wide table with a metric+horizon+type
discriminator vs per-family tables), the recording handler's placement on the refresh path, and the
series HTTP contract shared by both widgets.
