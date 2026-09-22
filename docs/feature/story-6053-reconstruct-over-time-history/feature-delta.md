# story-6053-reconstruct-over-time-history

**ADO**: User Story [6053](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6053) — "Back propagate missing PBC and Percentiles over Time values"
**Reported by**: Steve Pereira (community)
**Tags**: Release Notes
**Waves**: DISCUSS (2026-09-22)

---

## Wave: DISCUSS / [REF] Persona

**Primary**: `flow-coach` — opens the Percentiles Over Time widget in a team's Predictability tab during a flow review.
**Secondary**: `delivery-lead-rte` — opens the PBC Over Time widget to judge whether natural process limits have shifted.

Both personas already exist in `docs/product/personas/`. No persona change; this story removes a barrier that
stops both of them reaching a capability that already shipped.

---

## Wave: DISCUSS / [REF] JTBD

**New job**: `job-flow-coach-see-the-trend-my-data-already-supports`

> When I open an over-time chart on an instance that holds months of work-item history but only a handful of
> recorded snapshots, I want the chart to show the trend my data already supports, so I can read predictability
> from day one instead of waiting weeks for the recorder to accrue what could have been computed all along.

**Jobs whose satisfaction this raises** (both from Epic 5427, unchanged in shape):

- `job-flow-coach-see-predictability-trend` — currently unsatisfiable on a young or intermittently-run instance.
- `job-delivery-lead-see-process-stability-trend` — same barrier, PBC branch.

Epic 5427 built the capability and scored both jobs at `current_satisfaction: 1`. The observed satisfaction on a
real instance is still ~1, because the chart has almost nothing in it. This story closes the gap between the
capability and its reachability.

### Evidence (measured, dev instance backup, 2026-09-22)

The dev database reproduces the reported defect exactly:

```
PercentilesOverTimeSnapshots:  36 rows, 4 distinct days, 2026-09-05 .. 2026-09-21
ProcessBehaviorSnapshots:      42 rows, 4 distinct days, 2026-09-05 .. 2026-09-21

recorded days:  2026-09-05 (Sat)   2026-09-19 (Sat)  2026-09-20 (Sun)  2026-09-21 (Mon)
                |________ 13-day interior gap ________|

Portfolio 1: exactly 3 recorded days  <- Steve's "only 3 entries", literally
```

Every recorded day is a weekend or a Monday: the recorder only ran when the instance happened to be up. On a
standalone install this is the normal case, not an edge case.

Against that, the reconstructible span:

```
WorkItems with ClosedDate:   615 rows, 2025-09-22 .. 2026-09-21  (a full year)
WorkItems with StartedDate:  616 rows, 2025-09-22 .. 2026-09-21
DoneItemsCutoffDays:         365 (both owners, the default)
Team 1 / Portfolio 1 UpdateTime: 2026-09-21 - no trailing gap on this instance
```

**4 recorded days against roughly 365 reconstructible ones.** That ratio is the whole opportunity.

### Four forces

| Force | Statement |
|---|---|
| **Push** | An instance carrying a year of item history renders 3-4 points. Users read that as a broken chart, not as a design property. Steve asked directly: *"Any ideas why the PBC would only have 3 entries? Same thing for the 'Percentiles Over Time' chart"*. The forward-only design is correct and invisible; the confusion it causes is not. |
| **Pull** | Open the widget and see the trend the stored data already supports - no waiting, no explanation needed. |
| **Anxiety** | Is a reconstructed number as trustworthy as a recorded one? Will the first load be slow? (Answered by D3 and SPIKE-01 respectively.) |
| **Habit** | Users currently wait weeks, or conclude the widget is broken and stop opening it. The second is the expensive one - a feature that shipped and is presumed broken. |

**Opportunity score**: importance 4, current_satisfaction 1, **gap 3**.

---

## Wave: DISCUSS / [REF] Locked decisions

### D1 - Reconstruct from stored history; never synthesize. **(amends ADR-109)**

ADR-109 rejected "backfill real tenants too" on the grounds that it *"would fabricate historical percentiles
that were never actually measured - dishonest"*. That rejection was aimed at `DemoPercentilesBackfillHandler`,
which invents values from a deterministic wave (`4 + (dayIndex % 5) + horizon / 30`). It is correct, and it
stands, **for synthesis**.

Recomputation is a different act. Both recorders already take a window:

```csharp
teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate)
teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate)
```

A missing day `D` is the identical call with the window shifted to `(D - horizon, D)`, stamped
`RecordedAt = D`. Work-item age reconstructs too: `GetWorkItemAgePercentilesForTeam(team, endDate)` is genuinely
as-of-`endDate` since the D15 fix (`GetWipSnapshotForTeam` filters through `WasItemProgressOnDay`, projected via
`AgeOnDay`).

A reconstructed point is therefore the value the recorder *would have written that day*, not a value invented to
fill a hole. **ADR-109 needs an amendment recording this distinction** - its Decision section currently reads as
though all real-tenant backfill is refused.

### D2 - The trigger is the series read detecting a gap in its requested range. **(amends ADR-108)**

What the work item proposes literally: *"if we lack any snapshot before date x, and we request 'x - yyy days',
generate it for each missing day"*.

This overturns a property ADR-108's slice-03b amendment locked verbatim:

> **Still read-only.** The window is a filter on persisted rows, never a recompute trigger... the widget re-plots
> the days the pipeline recorded, it never triggers a recompute.

That sentence is no longer true after this story. **ADR-108 needs an amendment.** The endpoints stay read-only
*in their response contract* - they still return a dated series and nothing else - but they acquire a side effect.
DESIGN must decide whether that side effect belongs on the controller, the query port, or a seam behind both.

### D3 - The read never blocks on reconstruction.

A request that finds 47 missing days does **not** compute 47 days inline. It enqueues the work and returns the
series as it stands now. The chart fills on a subsequent load. No refresh and no chart request ever gets slower
than it is today.

This reconciles the two answers given in DISCUSS: the *trigger* is lazy and per-range (D2), the *work* is
background. The user-visible consequence - the chart is not complete on the very first load after upgrade -
is accepted, and is the subject of US-04's honesty copy.

### D4 - Depth: walk back to the data floor, capped.

Reconstruction walks backwards day by day and stops at the first day the stored data cannot honestly support.
The floor is bounded by `DoneItemsCutoffDays` (default 365) and by the earliest stored item. A fixed ceiling
bounds first-run cost; **the ceiling's value is an open question for DESIGN**, informed by SPIKE-01's cost
measurement.

Reconstruction cannot reach "pre-Lighthouse" time by construction: there is nothing to recompute from, so the
walk terminates. No extra guard is needed for that case.

### D5 - Fill leading and interior gaps; floor at the owner's last successful fetch.

| Gap | Cause | Filled? |
|---|---|---|
| **Leading** | recording began later than the data | **Yes** - the reported complaint |
| **Interior** | instance off, weekend, no refresh ran | **Yes** - the dominant case on a standalone install |
| **Trailing** | owner stopped refreshing (revoked token, deleted project, team dropped) | **No** |

Leading and interior gaps take one code path with no gap taxonomy in the implementation - the rule is simply
"no row for day D, and day D is computable".

The trailing gap is refused because it is where recomputation stops being recomputation. If an owner's connection
broke 60 days ago its work items are frozen at the break; reconstructing the days since would compute steadily
rising work-item age and WIP off items Lighthouse only *believes* are still open, and a flattening cycle time -
a confident, plausible trend for a period in which nothing was observed. ADR-108's slice-03b amendment already
names this owner as a live state, not a hypothetical. The floor is
`WorkTrackingSystemOptionsOwner.UpdateTime`.

### D6 - No distinction between a reconstructed point and a recorded one. **(contingent on SPIKE-01)**

No flag is persisted, no dashed segment, no tooltip difference. The justification is D1: a reconstructed value
*is* the value that day's computation would have produced, so drawing a distinction would assert a difference
that does not exist.

**This decision is contingent and must be re-confirmed by SPIKE-01's fidelity measurement.** The residual risk is
real and named: reconstruction runs against *today's* configuration and *today's* item set. Where state mappings,
blocked rules, cycle-time definitions, blackout configuration or team membership have changed since day D - or
where items have been deleted or re-parented - a reconstructed point may differ from what the recorder would
actually have written on that day. With no flag, that difference is invisible and unrecoverable after the fact.

SPIKE-01 measures exactly this against the four days the dev instance genuinely recorded. If reconstructed and
recorded diverge materially, **D6 is withdrawn** and the marking question reopens before any story is built.

### D7 - Never write an all-zero percentile row; absence is the honest answer.

`BuildPercentiles` on an empty list yields four zeros, and `ValueFor(percentiles, n) ?? 0` writes them. A day with
no closed items therefore records `P50 = P70 = P85 = P95 = 0` rather than no row at all. Reconstructed across a
thin stretch of history, that paints a floor of zeros - replacing "only 3 entries" with a worse and more
confident lie.

Reconstruction must refuse to write such a row. This is the percentile equivalent of the gate
`ProcessBehaviorRecordingHandler` already applies to its own family:

```csharp
if (chart.Status != BaselineStatus.Ready) return;
if (chart.Average == 0 && chart.UpperNaturalProcessLimit == 0) return;
```

Scope note: the same zero-write path exists in the *forward* recorder today. This story gates the reconstruction
path. Whether to gate the forward recorder too is left to DESIGN as an adjacent question, not assumed.

### D8 - The forward-only empty-state copy becomes false and must be revised.

`"builds forward from today - no snapshots recorded yet"` (Epic 5427 D6) is honest only while the series is
forward-only. Once reconstruction exists, an owner can legitimately have no data for a reason the copy does not
name - the range predates the data floor, or reconstruction has been enqueued and has not finished. US-04 owns
the revised copy.

---

## Wave: DISCUSS / [REF] Pre-requisites

- Epic 5427 shipped in full: `PercentilesOverTimeSnapshot` + `ProcessBehaviorSnapshot` tables (ADR-106), both
  recording handlers (ADR-107), both series endpoints (ADR-108), both widgets.
- No migration expected - reconstruction writes rows into existing tables in the existing shape.
- No RBAC change; reads inherit the existing `MetricsController` gate (Epic 5427 D3, free-tier/ungated).
- No CLI/MCP client version gate - no client consumes these two endpoints (ADR-108 Consequences).

---

## Wave: DISCUSS / [REF] Driving ports

Both existing, both extended in behaviour rather than in shape:

```
GET .../metrics/percentiles-over-time?horizon={30|60|90}&metricType={CycleTime|WorkItemAge}&startDate=&endDate=
GET .../metrics/process-behavior-over-time?type={Throughput|WorkItemAge|Wip|CycleTime|Arrivals|FeatureSize}&startDate=&endDate=
```

No new route, no new response DTO, no request-shape change. The UI surfaces are the two existing widgets
(`PercentilesOverTimeWidget.tsx`, `PbcOverTimeWidget.tsx`) on team and portfolio Predictability tabs.

---

## Wave: DISCUSS / [REF] Walking-skeleton strategy

**Strategy B - extend the existing vertical.** Brownfield: every layer this story touches already exists and is
under test. There is no new port, no new table, no new widget. Slice 01 is the thinnest end-to-end proof
(one metric family, one scope), not a skeleton.

---

## Wave: DISCUSS / [REF] Scope assessment

**PASS - right-sized.** Against the oversize heuristics: 4 user stories (< 10); one bounded context (Metrics);
no new integration points; estimated ~4 days (< 2 weeks). The two user outcomes (percentile trend, PBC trend)
are not independently shippable in the user's mind - Steve reported them as one complaint - so they stay one
story with separate slices.

---

## Wave: DISCUSS / [REF] User stories

### US-01 - See the cycle-time percentile trend my data already supports

> As a **flow coach**, I want the Percentiles Over Time widget to fill in the days it can compute from stored
> history, so that I can read a cycle-time trend on the instance I have rather than the instance I will have in
> two months.

`job_id: job-flow-coach-see-the-trend-my-data-already-supports`

#### Elevator Pitch
Before: the widget shows 4 points spanning 17 days on an instance holding a year of work-item history.
After: open **Team -> Metrics -> Predictability -> Percentiles Over Time** -> sees a continuous dated line across
the capped window, with no holes at weekends and no empty run before the first recorded day.
Decision enabled: whether cycle-time spread has tightened or widened over the review period - answerable in the
first week of using Lighthouse, not the ninth.

**Acceptance criteria**

1. Requesting a range containing days that have no `PercentilesOverTimeSnapshot` row, on an owner whose stored
   items support those days, results in those days being reconstructed and subsequently plotted.
2. A reconstructed day's `P50/P70/P85/P95` equal what `GetCycleTimePercentilesForTeam(team, D - horizon, D)`
   returns for that day - verified by reconstructing a day the recorder genuinely recorded and asserting equality
   against the persisted row (the dev instance supplies four such days).
3. The request that discovers the gap returns within the same latency envelope as today's request, and returns
   the series as it currently stands. Reconstruction happens off the request path (D3).
4. Interior gaps are filled on the same path as leading gaps - a series with days recorded either side of a
   missing day gains that day (D5).
5. No day after the owner's `UpdateTime` is reconstructed (D5, trailing-gap floor).
6. The walk back terminates at the data floor and never writes a row for a day the stored items cannot support
   (D4).
7. No reconstructed row is written with `P50 = P70 = P85 = P95 = 0`; such a day is left absent (D7).
8. Reconstruction is idempotent: a day already carrying a row - recorded or reconstructed - is not rewritten.

### US-02 - The same trend on every percentile tab and at portfolio scope

> As a **flow coach**, I want work-item-age and every cycle-time horizon to fill in the same way at team and
> portfolio scope, so that no tab of the widget contradicts another about how much history exists.

`job_id: job-flow-coach-see-the-trend-my-data-already-supports`

#### Elevator Pitch
Before: one tab of the widget has a populated trend and the other three are nearly empty.
After: toggle **[ WIA | CT-30 | CT-60 | CT-90 ]** on team and on portfolio -> sees every tab covering the same
dated span.
Decision enabled: whether age is climbing while finished-item cycle time looks stable - the cross-tab comparison
the combined widget was built for, which only works when both tabs span the same period.

**Acceptance criteria**

1. CT-60 and CT-90 reconstruct on the same path as CT-30, each with its own window.
2. Work-item-age reconstructs at the `NoHorizon` sentinel via the as-of-`endDate` path
   (`GetWipSnapshotForTeam` -> `AgeOnDay`), and a reconstructed WIA day equals what that path returns for it.
3. Portfolio scope reconstructs through `IPortfolioMetricsService` with the portfolio family set.
4. Every AC of US-01 holds for each family and scope added here - in particular the absence gate (D7) and the
   `UpdateTime` floor (D5).
5. Switching tabs does not itself trigger a second reconstruction of a span already reconstructed.

### US-03 - See where the process limits actually moved

> As a **delivery lead**, I want the PBC Over Time widget to fill in the days it can compute, so that I can tell
> a genuine process shift from a single out-of-limit point without waiting for months of recording.

`job_id: job-delivery-lead-see-process-stability-trend`

#### Elevator Pitch
Before: the PBC Over Time widget shows 3 points, so "have the limits moved?" is unanswerable.
After: open **Predictability -> PBC Over Time**, toggle across Throughput / WIA / WIP / Cycle Time / Arrivals
(and Feature Size at portfolio scope) -> sees UNPL, Average and LNPL as three dated lines across the capped
window.
Decision enabled: whether to treat a recent out-of-limit point as noise or as evidence the process itself changed.

**Acceptance criteria**

1. All five team families and all six portfolio families reconstruct, matching
   `ProcessBehaviorRecordingHandler`'s `TeamReaders` / `PortfolioReaders` sets exactly.
2. The existing honesty gates are applied to reconstructed days unchanged: a day whose chart is not
   `BaselineStatus.Ready`, or whose `Average == 0 && UpperNaturalProcessLimit == 0`, yields **no row**.
3. **Baseline validation is evaluated correctly for the reconstructed day.** `BaselineValidationService.Validate(
   baselineStart, baselineEnd, DoneItemsCutoffDays, Clock.Today)` is anchored to *today*; a reconstruction for
   day D must not be silently invalidated (or silently validated) by today's anchor. The chosen behaviour is
   asserted explicitly rather than inherited.
4. An owner with a pinned `ProcessBehaviourChartBaselineStartDate`/`EndDate` produces a defensible series - flat
   limits if the baseline is fixed is a correct reading, an empty series is not, and the test says which is
   expected and why.
5. Feature Size remains portfolio-only.

### US-04 - Be told the truth when there is still nothing to show

> As a **flow coach**, I want the widget to say something true when it is empty, so that I can tell "still
> filling in" from "your history does not reach back that far" from "nothing was ever recorded".

`job_id: job-flow-coach-see-the-trend-my-data-already-supports`

#### Elevator Pitch
Before: an empty chart says *"builds forward from today - no snapshots recorded yet"*, which after this story can
be false in two new ways.
After: open an over-time widget on a fresh owner, or select a range that predates the data floor -> sees copy that
names the actual reason the chart is empty.
Decision enabled: whether to wait, to widen the range, or to stop expecting data that cannot exist.

**Acceptance criteria**

1. An owner with no stored items and no snapshots reads copy that does not promise a forward-only fill that has
   already been superseded.
2. A range entirely before the data floor reads copy naming that, distinct from "nothing recorded yet".
3. A range whose reconstruction has been enqueued but not completed is distinguishable from one that has nothing
   to reconstruct.
4. The existing slice-03b range-end predicate (range ends today or later -> forward-only copy; ends before today
   -> in-range copy) is revisited against the new states rather than left to drift.
5. `docs/metrics/predictability.md` is updated: the forward-only note and the demo-backfill-is-Throughput-only
   note both change meaning under this story.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All four user stories' acceptance criteria pass.
2. SPIKE-01's fidelity finding is recorded, and D6 is either confirmed or withdrawn on that evidence.
3. Backend `dotnet build` zero warnings; `dotnet test` green with the connector categories excluded.
4. Frontend `pnpm test` green; `pnpm build` zero errors and zero warnings.
5. SonarQube Cloud introduces no new issues of any severity.
6. ADR-108 and ADR-109 each carry an amendment recording what this story changed about them (D1, D2).
7. Mutation testing >= 80% kill rate on the changed backend surface, run last on frozen code.
8. `docs/metrics/predictability.md` updated (US-04 AC5); screenshots regenerated if the empty-state copy is
   visible in any documented shot.
9. Demo data still renders populated over-time charts, and `DemoPercentilesBackfillHandler` is reconciled with
   reconstruction rather than left to double-write (it backdates synthetic values into the same tables
   reconstruction now writes to - the interaction is decided, not discovered).

---

## Wave: DISCUSS / [REF] Out of scope

- **Marking reconstructed points** - D6 says no distinction. Reopens only if SPIKE-01 withdraws D6.
- **Trailing-gap fill** for owners that have stopped refreshing (D5).
- **Orphaned snapshots from deleted owners.** The dev DB carries 7 all-zero percentile rows and 4 PBC rows for
  owner id 2, which exists in neither `Teams` nor `Portfolios`. Snapshot rows survive owner deletion. A real
  defect, found while measuring this story; belongs in its own bug, not here.
- **Gating the forward recorder's zero-write path** (D7 scope note) - adjacent, decided by DESIGN, not assumed.
- **Changing the chart x-axis from `scaleType: "point"` to a time scale.** Considered and set aside: it would
  make interior gaps render at true width instead of being silently compressed, which is an honest improvement,
  but it changes the shape of every existing over-time chart and answers a different question from this story's.
- **Reconstructing `BlockedCountSnapshot` or `DeliveryMetricSnapshot`** - the same argument may apply to both;
  neither was reported and neither is in this work item.
- **Backfilling metrics that are structurally forward-only**, e.g. Epic 5585's feature size fields.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Dated span rendered on the dev instance's team Percentiles Over Time widget | from 4 days to >= 90 days (or the chosen cap) | Direct observation on the restored dev DB, before and after |
| Reconstructed-vs-recorded divergence on days with both | 0 of 4 days diverge on any of P50/P70/P85/P95 | SPIKE-01 diff against the 4 genuinely recorded days |
| Added latency on a series request that discovers a gap | < 50 ms over today's p95 for the same request | Timed request against the dev DB, gap present vs gap absent |
| All-zero percentile rows written by reconstruction | exactly 0 | Assertion over the snapshot table after a full-window reconstruction |
| Rows written for days after an owner's `UpdateTime` | exactly 0 | Assertion after reconstruction on an owner with a stale `UpdateTime` |
| Recurrence of the reported confusion | no further "why only N entries" reports after release | Community channels; qualitative, no telemetry until Epic 5015 |

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Every story traces to a job | **PASS** | US-01/02/04 -> `job-flow-coach-see-the-trend-my-data-already-supports`; US-03 -> `job-delivery-lead-see-process-stability-trend` |
| 2 | Every story has a complete Elevator Pitch | **PASS** | Four pitches, each naming a real UI entry point and observable output |
| 3 | Every AC is testable without ambiguity | **PASS** | Each AC names the call, the table or the copy it asserts on |
| 4 | Slice briefs exist, <= 1 day each | **PASS** | `slices/slice-01..04`, each with a learning hypothesis |
| 5 | Outcome KPIs have numeric targets and a method | **PASS** | Six KPIs above; five measurable today on the dev DB |
| 6 | Dependencies identified | **PASS** | Epic 5427 shipped; ADR-106/107/108/109 read; no migration, no RBAC, no client gate |
| 7 | Out-of-scope explicit | **PASS** | Seven named exclusions, each with a reason |
| 8 | Prior-wave / architecture consultation done | **PASS** | ADR-106/107/108/109, both recorders, both services, both widgets, `jobs.yaml`, `journeys/epic-5427-*` |
| 9 | No unresolved blocking unknown | **PASS with a carried probe** | The one decisive unknown (reconstruction fidelity, which D6 rests on) is carried as SPIKE-01 inside slice 01 and is timeboxed, with ground truth already located |

**Requirements completeness**: 0.96 - the single incompleteness is D4's cap value, deliberately left for DESIGN
to set on SPIKE-01's cost measurement rather than guessed here.

---

## Wave: DISCUSS / [REF] Slice map and prioritisation

| Slice | Goal | Why here |
|---|---|---|
| **SPIKE-01** (inside slice 01) | Measure reconstruction fidelity, cost, and absence behaviour against the dev DB | Highest-uncertainty item, and it gates D6. Failing it costs hours, not a shipped slice |
| **01** | CT-30 percentiles, team scope, gap fill end to end | Thinnest slice that exercises every mechanism: trigger, background, floor, absence gate, idempotency |
| **02** | CT-60/90 + WIA + portfolio scope | WIA is a genuinely different reconstruction path (as-of, not windowed); portfolio is a different service |
| **03** | PBC families, both scopes | Different table, different honesty gate, and the today-anchored baseline hazard |
| **04** | Honest empty-state copy + docs | Depends on knowing which empty states actually exist, which only slices 01-03 settle |

**Order rationale**: learning leverage first (SPIKE-01 can withdraw D6 before anything is built), then the
dependency chain (01 establishes the path 02 and 03 reuse), then the copy that can only be written once the
states are known. Each slice has a dogfood moment on the restored dev DB the same day.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | **Pass** - no slice adds a component; all extend existing seams |
| Every slice depending on a new abstraction? | **Pass** - slice 01 establishes the reconstruction seam and ships value with it; 02-04 reuse it |
| Does any slice disprove a pre-commitment? | **Pass** - SPIKE-01 can withdraw D6; slice 03 can disprove "PBC reuses the percentile path unchanged" |
| Synthetic data only? | **Pass** - every slice is dogfooded against the restored dev DB, which carries a year of real items |
| 2+ slices identical except for scale? | **Flagged, not merged** - 01 and 02 look like scale, but WIA reconstructs through a different service method (`GetWorkItemAgePercentiles(owner, endDate)`, no window) and portfolio through a different service. Kept separate deliberately; if slice 02 turns out to be a pure parameter sweep, merge it into 01 at DELIVER |

---

## Wave: DISCUSS / [REF] Upstream changes

No DISCOVER wave ran for this story - the evidence is the reported defect on the work item plus the measured dev
database, both recorded under **JTBD -> Evidence** above.

Two Epic 5427 decisions are changed rather than extended, and both are recorded as amendments rather than silent
overrides:

- **ADR-109 Decision** - "backfill real tenants too... **Rejected**" is narrowed to synthesis (D1).
- **ADR-108 slice-03b amendment** - "the window is a filter on persisted rows, **never a recompute trigger**" no
  longer holds (D2).

Epic 5427's D5 (forward-only recording) is **not** overturned: the recorder stays forward-only. Reconstruction is
a second, separate path.

---

## Wave: DISCUSS / [HOW] Gherkin scenarios

Rendered on request (expansion `gherkin-scenarios`), triggered by AC ambiguity: US-01 AC6 ("terminates at the
data floor") and US-03 AC4 ("produces a defensible series") are each readable more than one way. These pin them.

DISCUSS-level scenarios, not acceptance tests — DISTILL owns the executable specifications. The concrete dates
are the dev database's real values so the scenarios can be driven against it.

```gherkin
Feature: Reconstruct missing over-time history from stored work items

  Background:
    Given a Team whose stored work items span 2025-09-22 to 2026-09-21
    And the Team's DoneItemsCutoffDays is 365
    And the Team's last successful fetch was on 2026-09-21
    And today is 2026-09-22

  # ---------- D5: which gaps get filled ----------

  Scenario: A leading gap before the first recorded day is filled
    Given the earliest recorded snapshot for the Team is 2026-09-05
    And the requested range starts on 2026-08-01
    When the percentiles series is requested
    Then the days from 2026-08-01 to 2026-09-04 are reconstructed
    And each reconstructed day carries the value the recorder would have written that day

  Scenario: An interior gap left by an instance that was not running is filled
    Given the Team has recorded snapshots on 2026-09-05 and on 2026-09-19
    And no snapshot exists for any day between them
    When the percentiles series is requested for a range covering both
    Then the 13 days from 2026-09-06 to 2026-09-18 are reconstructed
    And they are filled by the same path that fills a leading gap

  Scenario: Days after the last successful fetch are never reconstructed
    Given the Team's last successful fetch was on 2026-07-24
    And no snapshot exists for any day after 2026-07-24
    When the percentiles series is requested for a range ending today
    Then no day after 2026-07-24 is reconstructed
    And the series ends on the last day the Team was actually observed

  # ---------- D4: how far back ----------

  Scenario: The walk back stops where the stored data stops
    Given the earliest stored work item closed on 2025-09-22
    And the requested range starts on 2024-01-01
    When the percentiles series is requested
    Then no day before 2025-09-22 is reconstructed
    And the walk terminates rather than emitting progressively thinner days

  Scenario: The walk back stops at the configured cap even when data reaches further
    Given the stored work items reach back 365 days
    And the reconstruction cap is <cap> days
    When the percentiles series is requested for the full history
    Then at most <cap> days are reconstructed

  # ---------- D7: absence beats a false zero ----------

  Scenario: A day with no closed items produces no row rather than four zeros
    Given no work item closed in the 30 days before 2026-03-14
    When 2026-03-14 is reconstructed for cycle time at horizon 30
    Then no PercentilesOverTimeSnapshot row is written for that day
    And the chart shows a gap there rather than a point at zero

  # ---------- idempotency ----------

  Scenario: A day the recorder genuinely wrote is never rewritten
    Given a recorded snapshot exists for 2026-09-20 with P85 of 2
    When the series is requested for a range containing 2026-09-20
    Then that row is left exactly as recorded
    And reconstruction does not overwrite it with its own computation

  Scenario: Requesting the same range twice reconstructs nothing the second time
    Given the range 2026-08-01 to 2026-09-22 has already been reconstructed
    When the same range is requested again
    Then no further reconstruction is enqueued

  # ---------- D2 + D3: lazy trigger, non-blocking ----------

  Scenario: The request that discovers a gap is not slowed by it
    Given 47 days inside the requested range have no snapshot
    When the percentiles series is requested
    Then the response returns within the same latency envelope as a request with no gap
    And the response carries only the days persisted at the moment of the request
    And reconstruction of the 47 days proceeds after the response

  # ---------- D6 (contingent on SPIKE-01) ----------

  Scenario: A reconstructed day matches what the recorder wrote for that same day
    Given the recorder wrote a snapshot for 2026-09-19
    When 2026-09-19 is reconstructed from stored work items
    Then the reconstructed P50, P70, P85 and P95 equal the recorded ones
    # If this fails, D6 is withdrawn and reconstructed points must be distinguishable.

  # ---------- US-02: the as-of path ----------

  Scenario: Work item age reconstructs as of the reconstructed day, not as of today
    Given a work item started on 2026-08-01 and closed on 2026-09-10
    When work item age percentiles are reconstructed for 2026-09-01
    Then that item is counted as in progress on 2026-09-01
    And its contributed age is its age on 2026-09-01, not zero and not its age today

  # ---------- US-03: PBC honesty gates and the baseline hazard ----------

  Scenario Outline: A chart that is not ready yields no row
    Given the process behaviour chart for <type> on 2026-05-10 has status <status>
    When 2026-05-10 is reconstructed for <type>
    Then no ProcessBehaviorSnapshot row is written

    Examples:
      | type       | status          |
      | Throughput | NotReady        |
      | CycleTime  | BaselineInvalid |

  Scenario: A collapsed limit band yields no row
    Given the process behaviour chart for Throughput on 2026-05-10 is Ready
    And its Average is 0 and its UpperNaturalProcessLimit is 0
    When 2026-05-10 is reconstructed for Throughput
    Then no ProcessBehaviorSnapshot row is written
    # LowerNaturalProcessLimit is deliberately not part of this predicate: a real, busy
    # process routinely reports Lnpl == 0 because the calculator clamps at zero.

  Scenario: An owner with a pinned baseline still produces a series
    Given the Portfolio has ProcessBehaviourChartBaselineStartDate 2026-01-01
    And ProcessBehaviourChartBaselineEndDate 2026-03-31
    When the range 2026-04-01 to 2026-06-30 is reconstructed for Throughput
    Then a row is written for each reconstructed day
    And the limits are identical across those days, because a pinned baseline does not move
    And an empty series is NOT an acceptable outcome here

  Scenario: An owner with no pinned baseline gets per-day limits
    Given the Team has no ProcessBehaviourChartBaselineStartDate
    When the range 2026-04-01 to 2026-06-30 is reconstructed for Throughput
    Then each day's limits are computed from that day's own lookback window
    And the limit lines are free to move across the range

  Scenario: Feature Size reconstructs only at Portfolio scope
    When a Team's process behaviour history is reconstructed
    Then no FeatureSize row is written
    But when a Portfolio's history is reconstructed
    Then FeatureSize rows are written

  # ---------- US-04: honest empty states ----------

  Scenario: A range entirely before the data floor says so
    Given the earliest stored work item closed on 2025-09-22
    When the range 2024-01-01 to 2024-06-30 is requested
    Then the widget states that this period cannot be reconstructed
    And it does NOT say "builds forward from today - no snapshots recorded yet"

  Scenario: A fresh owner is not promised a forward-only fill that no longer applies
    Given a Team with no stored work items and no snapshots
    When the percentiles series is requested
    Then the widget states there is nothing to show and why
    And the copy is true of an instance where reconstruction exists
```

---

## Wave: DISCUSS / [WHY] Alternatives considered

Rendered on request (expansion `alternatives-considered`), triggered by cross-context complexity: the story
spans C#/EF on the backend, React/TypeScript on the frontend, and the snapshot storage layer.

One entry per locked decision. Options are recorded with why they lost, so a later reader can tell a considered
rejection from an unexamined one.

### D1 — Reconstruct, not synthesize

| Option | Verdict |
|---|---|
| **Recompute from stored history** (chosen) | The recorder's own service call with a shifted window. Faithful by construction, and cheap because the computation already exists |
| Extend `DemoPercentilesBackfillHandler` to real tenants | **Rejected, and ADR-109 was right to.** It invents values from `4 + (dayIndex % 5) + horizon / 30`. Shipping that to a paying instance would put a fabricated trend in front of a delivery decision |
| Compute history on the fly and persist nothing | **Rejected.** Keeps the snapshot table meaning "what we observed", which is attractive. But it pays the full compute on every read of every range forever, and the two series endpoints are read on every dashboard load. The persisted form converges to zero cost; this one never does |

### D2 — Lazy trigger on the series read

| Option | Verdict |
|---|---|
| **Gap detected by the series read** (chosen) | What the work item proposes literally. Fills only what someone actually looks at, and needs no upgrade hook or migration |
| Once, on the first refresh after upgrade | Strong contender — off the read path entirely, cost paid once, mirrors the demo-backfill idiom including its per-family idempotency guard. Lost because it fills history nobody may ever request, and because the guard has a documented trap: ADR-109's slice-02 amendment records how an owner-scoped guard silently made a whole metric family a permanent no-op while fresh-owner unit tests stayed green |
| Explicit admin action per owner | **Rejected.** Steve's confusion persists until somebody finds the button. A fix that requires discovering the fix is not a fix for a first-impression problem |

### D3 — Non-blocking

| Option | Verdict |
|---|---|
| **Enqueue, return what exists** (chosen) | No request ever gets slower than today. Cost: the chart is incomplete on the first load, which US-04 has to explain honestly |
| Compute the gap inline | **Rejected.** 47 missing days across the full family set on the request thread would turn a dashboard load into a visible stall. Replacing "my chart is empty" with "my dashboard hangs" is not an improvement |
| Throttle: fill N days per refresh | Considered and set aside. No single operation is slow and no new job type is needed, but it ties fill rate to refresh cadence — and on the very instances with the worst gaps (a laptop run twice a week) the refresh cadence is exactly what is missing. It converges slowest where it is needed most |

### D4 — Depth

| Option | Verdict |
|---|---|
| **To the data floor, capped** (chosen) | Stops where truth stops; the cap bounds first-run cost. The cap's value is left to DESIGN on SPIKE-01's measurement rather than guessed |
| Fixed window regardless of data | **Rejected.** Emits progressively thinner and eventually wrong points at the old end, where items have aged past `DoneItemsCutoffDays`. Confidently wrong is worse than absent |
| Match the widest dashboard range | **Rejected.** Ties a data-quality boundary to a UI constant. If the pickers change, the honesty boundary moves with them for no reason connected to the data |
| Uncapped to the floor | **Rejected.** Unbounded first-run cost on an instance with years of history, for days nobody will scroll to |

### D5 — Gap scope

| Option | Verdict |
|---|---|
| **Leading + interior, floored at last fetch** (chosen) | One code path with no gap taxonomy in the implementation. The floor is the only special case, and it earns its place |
| Leading gap only | Literally what the work item asks for, and the cheapest check (one query for the earliest snapshot, no per-day scan). **Rejected** because the measured evidence contradicts the framing: the dev instance's dominant gap is interior, not leading — 4 recorded days across 17, every one a weekend or Monday. On a standalone install the interior gap *is* the problem |
| Fill everything including trailing | **Rejected.** A broken-connection owner's items are frozen at the break. Reconstructing the days since computes steadily rising work item age and WIP off items only *believed* open — a confident trend for a period in which nothing was observed. This is the one place reconstruction stops being recomputation |
| Leave interior gaps; switch the x-axis to a time scale instead | Genuinely attractive and much cheaper — no rows written, and `scaleType: "point"` currently compresses a weekend away so a gap is silently invisible rather than visibly honest. **Set aside, not dismissed**: it changes the shape of every existing over-time chart and answers a different question. Recorded in Out of scope so it is not re-derived from scratch later |

### D6 — No distinction between reconstructed and recorded

| Option | Verdict |
|---|---|
| **No flag, no visual difference** (chosen, contingent) | Follows from D1: a reconstructed value *is* the value that day's computation would have produced, so a distinction would assert a difference that does not exist |
| Persist a flag, render reconstructed days differently | The safer option, and the one consistent with the project's "say what we know" habit. Lost on the argument above — but it is the fallback if SPIKE-01 shows divergence, which is why the spike runs before anything is built |
| Persist the flag, surface only a caption | Considered: cheap, no chart-geometry change. Rejected as the worst of both — it pays the storage and migration cost of the flag while giving a reader scrubbing the line no per-point signal |

**Standing risk on the chosen option**: reconstruction reflects *today's* configuration and item set. Changed
state mappings, cycle-time definitions, blocked rules or blackout config, and deleted or re-parented items, all
make a reconstructed day potentially differ from what would have been written then. Choosing no flag makes that
difference invisible and unrecoverable after the fact. Accepted deliberately, measured by SPIKE-01 first.

### D7 — Absence gate

| Option | Verdict |
|---|---|
| **Refuse to write an all-zero percentile row** (chosen) | Mirrors the gate `ProcessBehaviorRecordingHandler` already applies to its own family. Absence is the honest empty state |
| Allow zero rows, as the forward recorder does today | **Rejected.** Reconstructed across a thin stretch of history this paints a floor of zeros — replacing "only 3 entries" with a more confident lie, which is a worse outcome than the bug being fixed |
| Also gate the forward recorder | Deliberately **not** decided here. The same zero-write path exists in `PercentilesOverTimeRecordingHandler` today and is a pre-existing issue, not one this story creates. Flagged for DESIGN as adjacent rather than folded in silently |

### D8 — Empty-state copy

| Option | Verdict |
|---|---|
| **Revise the copy in slice 04** (chosen) | The reachable empty states are not knowable until slices 01–03 exist, so the copy is written last, against the states that actually remain |
| Leave the forward-only copy | **Rejected.** It becomes false in two new ways once reconstruction exists. The current copy is itself an example of this failure — written when forward-only was the only possibility, and now outlived by it |
| Write the new copy first | **Rejected** for the reason the chosen option gives: it would be guessing at which states survive, which is how the current copy got stale |
