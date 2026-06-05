<!-- markdownlint-disable MD024 -->
# Feature: wait-states-flow-efficiency

Additive, brownfield extension of the shipped `state-time-cumulative-view` chart (Epic 4144) plus
a new Flow Overview tile. Lets admins mark idle "wait" Doing-states, then surfaces flow efficiency
(active-Doing-time / total-Doing-time) three ways: an overview tile, a number on the cumulative
chart (aggregate or per-item via the existing picker), and a colour-highlight of wait-state bars.

ADO Story: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5173> ("Allow to specify Wait States", reported by Chris, state New, "Next Release" column)

Verbatim ask: define "Wait" states (no active work being done); if defined, add (1) a Flow Efficiency
widget in the overview, (2) a colour highlight for wait-state bars in the Cumulative Time in State
chart, (3) the flow-efficiency number on that chart (enabling per-item efficiency when filtered).

## Wave: DISCUSS / [REF] Revision (2026-06-05)

User review of the delivered DISCUSS changed the config design on two points. The original
assumptions are superseded but kept readable below and marked at their source.

- **Superseded (D1, original)**: "WaitStates = structural twin of `BlockedStates`, a flat
  `List<string>` of raw Doing-states, edited via a Wait States `InputGroup` inside
  `FlowMetricsConfigurationComponent` mirroring the Blocked States sub-section."
- **New (D1 revised + D11)**: `WaitStates` is **mapping-aware** — entries are raw Doing-states OR
  `StateMapping.Name`, resolved via the SAME `GetRawStatesForCategory(WaitStates)` expansion the
  state categories use. A mapped state is markable as a wait state in ONE click; all its underlying
  raw states then count as wait time. The efficiency `waitTime` (D2) is defined over that EXPANDED
  raw set, not a literal string match on `WaitStates`.
  - *User rationale*: mapped states must be markable as wait states without enumerating their raw
    states; wait-state resolution must match how categories already resolve mappings.
- **Superseded (UI placement, original D1/D8)**: "Wait States config lives in the Blocked States
  `InputGroup` / `FlowMetricsConfigurationComponent`, as a structural twin of Blocked States."
- **New (UI placement revised + D12)**: Wait States is state-classification config and lives in the
  **state-config cluster** next to `StateMappingsEditor` (`StatesList` + `StateMappingsEditor`),
  **decoupled from Blocked States**. The DISCUSS-preferred direction is an "Advanced State config"
  grouping that houses State Mappings + Wait States together; the exact container shape
  (new wrapper that relocates the existing `StateMappingsEditor`, vs a sibling Wait States
  `InputGroup` next to it without regrouping) is a DESIGN-level choice. Touching Blocked States is
  explicitly out of scope.
  - *User rationale*: Blocked config is changing soon, so Wait States must NOT be anchored to it;
    Wait States belongs with the other state-classification controls.

DoR remains **PASSED** — the changes touch DoR item 5 (AC, US-01 mapping AC added), item 7
(technical notes / cross-cutting RBAC re-confirmed: still no new permission), and the decisions
table; all re-confirmed in place below.

## Wave: DISCUSS / [REF] Pre-DISCUSS code reality check

Verified against the live codebase (not re-derived from scratch — the brief supplied the anchors,
this confirms them):

- **State config model** — `Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs`
  has `ToDoStates`, `DoingStates`, `DoneStates`, `public List<string> BlockedStates { get; set; } = [];`
  (line 41), and `public List<StateMapping> StateMappings { get; set; } = [];` (line 45), plus
  `MapStateToStateCategory`, `GetRawStatesForCategory` (resolves each entry: a `StateMapping.Name`
  expands to its raw `States`, otherwise the entry is a raw state), `MapRawStateToMappedName`,
  `OpenStates`, `AllStates`. `WaitStates` is a NEW `List<string>` on the abstract owner — but it is
  **mapping-aware** (resolved via `GetRawStatesForCategory`), NOT a flat raw-state mirror of
  `BlockedStates` (see Revision above, D11).
- **State config UI** — the settings form renders THREE sibling components (`ModifyTeamSettings.tsx`
  ~lines 153–196 and the Project equivalent): `StatesList` (ToDo/Doing/Done editor, mapping-name
  aware) → `StateMappingsEditor` (define logical mappings from Doing-states) →
  `FlowMetricsConfigurationComponent` (WIP/SLE/Blocked States/staleness/PBC). Wait States config
  lives in the **state-config cluster** next to `StateMappingsEditor` (D12), decoupled from the
  Blocked States `InputGroup` in `FlowMetricsConfigurationComponent` (which is evolving
  independently — out of scope to touch). The wait-state selector's suggestions include BOTH raw
  Doing-states AND `StateMappings` names. The wrapper-vs-sibling container shape is a DESIGN choice.
- **Cumulative chart this extends** — feature `state-time-cumulative-view`, widget key `stateTimeCumulative`,
  in the `flow-metrics` category on BOTH team and portfolio detail pages (`categoryMetadata.ts` line 79).
  Renders one bar per **Doing-category** state (D19 — ToDo/Done excluded), completed/ongoing segments
  (solid base + hatched top, D6), adaptive display unit (D16), RAG via `computeCumulativeStateTimeRag`,
  drill-down via shared `WorkItemsDialog` (D22), and a US-05 multi-select **item picker** that recomputes
  bars over a selected subset (D14/D21) — the exact "individual item if we filter" hook the story references.
- **Widget catalog** — `widgetInfoMetadata.ts` has the `stateTimeCumulative` entry (lines 331–340) with
  description, RAG `statusGuidance`, and learn-more URL. `categoryMetadata.ts` enumerates four categories:
  `flow-overview` (all `small` KPI tiles), `flow-metrics` (large charts), `predictability`, `portfolio`.
- **No existing `flowEfficiency` / `waitState` concept** anywhere in backend or frontend — confirmed fresh
  build (zero matches in `Lighthouse.Backend` or `Lighthouse.Frontend/src`).

**Finding**: the brief's code reality is accurate. The feature is an additive new `WaitStates` field
(config) + three thin presentation surfaces on top of an existing chart and the overview tile family.
Per the 2026-06-05 revision, `WaitStates` is mapping-aware (resolved via `GetRawStatesForCategory`)
and its config lives in the state-config cluster next to `StateMappingsEditor`, not as a twin of the
Blocked States sub-section.

## Wave: DISCUSS / [REF] Persona ID

- **`config-admin`** (Configuration Administrator) — owns the config action: marking which Doing-states
  are wait states. Existing persona (`docs/product/personas/config-admin.yaml`); new primary job
  `job-config-admin-define-wait-states` added. Concrete: Carlos Mendez, Team Phoenix admin.
- **`delivery-lead-rte`** (Delivery Lead / RTE) — primary viewer of flow efficiency for systemic
  improvement decisions. Existing persona; new primary job `job-spot-flow-efficiency-waste` added.
  Concrete: Priya Nair, Team Phoenix delivery lead.
- **`flow-coach`** (secondary) — uses the per-item efficiency number in the picker-narrowed view for
  per-item triage / outlier post-mortems. Existing persona, reused (no new job — shares
  `job-spot-flow-efficiency-waste` in its per-item lens).

No new personas invented — all three already exist in SSOT and fit cleanly.

## Wave: DISCUSS / [REF] JTBD one-liners

- **Config** (`job-config-admin-define-wait-states`, config-admin): *When I am configuring a Team/Portfolio
  and I know some Doing-states (or mapped states) are idle queues, I want to mark exactly those as "wait"
  states alongside my other state config — picking a raw Doing-state or a whole State Mapping in one click —
  so the tool can tell idle time from active time and report flow efficiency.*
- **View** (`job-spot-flow-efficiency-waste`, delivery-lead-rte / flow-coach): *When the question is "how
  much of our lead time is active work vs waiting?", I want a single flow-efficiency percentage (and the
  wait-states called out on the cumulative chart, and per-item efficiency when I filter), so I can name idle
  time as the problem with evidence rather than arguing about effort.*

Both added to `docs/product/jobs.yaml` in this DISCUSS run.

## Wave: DISCUSS / [REF] Scope assessment (Elephant Carpaccio gate)

Run BEFORE journey visualization investment.

| Signal | Reading | Oversized? |
|---|---|---|
| User stories | 4 (one per slice) | No (≤10) |
| Bounded contexts / modules touched | 2 (backend settings+metrics; frontend config+chart+overview) | No (≤3) |
| Walking skeleton integration points | N/A — additive brownfield, no walking skeleton | No |
| Estimated effort | ~3.5 days total, each slice ≤1 day | No (<2 weeks) |
| Independent user outcomes | 1 cohesive outcome (measure flow efficiency), sliced for incremental delivery | No |

**Scope Assessment: PASS — 4 stories, 2 contexts, estimated ~3.5 days (≤1 day per slice).** Right-sized;
no split needed. Per the locked decision this is a focused additive extension, not a greenfield epic.

## Wave: DISCUSS / [REF] Feature type & walking skeleton

- **Feature type**: Cross-cutting (backend config model + backend metric computation + frontend chart/
  widget). Confirmed per locked decision 4.
- **Walking skeleton**: **No** — additive brownfield. Each slice ships end-to-end against existing,
  working surfaces; there is no new end-to-end skeleton to stand up. Slice 1 (config + computation) is the
  foundation the later slices read from, but it is itself shippable and verifiable (the field persists and
  the efficiency value is computable/inspectable), not a non-functional skeleton.
- **JTBD**: Yes — every story traces to a `job_id` (default per decision 4).

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | **WaitStates = a new mapping-aware `List<string>` on `WorkTrackingSystemOptionsOwner`.** `public List<string> WaitStates { get; set; } = [];`; entries are raw Doing-states OR `StateMapping.Name`, resolved via `GetRawStatesForCategory(WaitStates)` (see D11). Edited in the state-config cluster next to `StateMappingsEditor` (see D12), with a selector whose suggestions include raw Doing-states AND mapping names. A wait state is "in-flow but idle". ~~Originally framed as a structural twin of `BlockedStates` edited in a `FlowMetricsConfigurationComponent` `InputGroup`~~ — **superseded 2026-06-05** (mapping-aware; state-config cluster; decoupled from Blocked States). | Locked (revised 2026-06-05). |
| D2 | **Flow efficiency formula**: `efficiency = activeDoingTime / totalDoingTime`, where `totalDoingTime` = sum of time in ALL Doing-category states and `activeDoingTime` = `totalDoingTime − waitTime`. **`waitTime` = sum of time spent in any raw state in the EXPANDED set `GetRawStatesForCategory(WaitStates)`** (mapping-aware, per D11) — NOT a literal string match on `WaitStates`. Computed over the SAME D12-included item set and full-duration attribution (D5) the cumulative chart uses. ToDo/Done time is excluded from the denominator (only Doing-time counts, matching cumulative D19). Aggregate sums across all in-scope items; per-item uses one item's durations. ~~waitTime originally = sum of time in states literally marked `WaitStates`~~ — **superseded 2026-06-05** (expanded raw set). | Locked (revised 2026-06-05). |
| D3 | **No wait states configured → "not configured", never 100%.** Overview tile: "Flow Efficiency — not configured" with a learn-more pointer to define wait states. Chart number: suppressed (cumulative bars/segments unchanged). Wait-bar highlight: nothing highlighted. Rationale: a 100% reading would falsely claim perfect efficiency for an unconfigured team. | Locked (user-confirmed). |
| D4 | **Zero total Doing-time in scope → "no data in scope", never divide-by-zero.** Distinct wording from D3's "not configured". Zero-denominator and unconfigured are different conditions and read differently. | Locked (user-confirmed). |
| D5 | **Chart flow-efficiency number recomputes with the US-05 picker** (per-item when narrowed to one item) — this is the "efficiency for an individual item if we filter" the work item asks for. The overview tile and the cumulative chart RAG do NOT follow the picker (they stay on the whole in-scope set, matching cumulative D18). The chart number is a view-level read of the displayed bars; the tile/RAG are systemic. | Locked. |
| D6 | **Wait-state bars colour-highlighted** on the cumulative chart, distinct from active-state bars, composing with the existing completed/ongoing segment split (still solid-base/hatched-top, but a wait colour family). Colour-blind-safe: the distinction is not colour-alone (legend entry and/or label/icon marks wait bars), matching the cumulative chart's existing colour-blind care. | Locked (user-confirmed via verbatim ask point 2). |
| D7 | **Overview Flow Efficiency widget placement**: `flow-overview` category, size `small` (the single-number-KPI tile shape — same family as `wipOverview`, `predictabilityScore`, `totalWorkItemAge`). Widget key: `flowEfficiency`. Confirmed against `categoryMetadata.ts`: every `flow-overview` entry is a small KPI tile; the efficiency percentage is exactly that shape. NOT `flow-metrics` (large charts). | Locked. |
| D8 | **No new write endpoint; additive read contract.** WaitStates rides the EXISTING team/portfolio settings DTO/endpoint (additive field, same mechanism as when `BlockedStates`/`StateMappings` were added). The cumulative chart's efficiency number and the wait-bar flag are either an additive extension of the existing `cumulativeStateTime` response OR derived client-side from the existing per-state bars + the settings' WaitStates list (expanded via `GetRawStatesForCategory`) — DESIGN picks the split. The overview tile is served by a new small endpoint or folded into the overview payload — DESIGN picks. No breaking change to any existing contract. ~~"twin of BlockedStates" framing~~ — **superseded 2026-06-05** (UI placement now the state-config cluster per D12; the additive-contract substance is unchanged). | Locked (revised 2026-06-05; DESIGN-flagged split). |
| D9 | **Wait states are a labelling overlay only.** They do NOT change throughput, forecasts, cycle time, aging, or the existing cumulative bars/RAG. The only consumers are the new flow-efficiency computation and the chart colour-highlight. | Locked. |
| D10 | **RAG thresholds for Flow Efficiency** (overview tile): baseline `act` < 40%, `observe` 40–60%, `sustain` ≥ 60% efficiency. These are starting defaults aligned with common flow-efficiency guidance and the cumulative chart's 40/60 threshold family; tunability is out of scope for MVP (fold into the existing per-widget RAG-config work if customers ask). | Locked (DESIGN may refine numeric values). |
| D11 | **Mapped states as wait states (mapping-aware WaitStates).** A wait-state entry may be EITHER a raw Doing-state OR a `StateMapping.Name`. The flow-efficiency computation expands `WaitStates` through `GetRawStatesForCategory(WaitStates)` (the SAME resolution the categories use), so a raw state counts as "wait" iff it is in that expanded set. The wait-state selector's suggestions include BOTH raw Doing-states AND mapping names; marking a mapped state marks ALL its underlying raw states as wait in one click (no enumeration). Validation keeps wait states within the Doing category (mappings are Doing-sourced); a state outside the expanded Doing set contributes nothing to the denominator. | Locked (user-confirmed 2026-06-05). |
| D12 | **Advanced State config grouping (UI placement).** Wait States is state-classification config and lives in the **state-config cluster** next to `StateMappingsEditor` (`StatesList` + `StateMappingsEditor`), **decoupled from Blocked States** (which is evolving independently — explicitly out of scope to touch). DISCUSS-preferred direction: an "Advanced State config" grouping that houses State Mappings + Wait States together. **DISCUSS hard requirement** = state cluster placement + decoupled-from-Blocked + mapping-aware selector. **DESIGN-level**: the exact container shape — (a) a new "Advanced State config" wrapper that relocates the existing `StateMappingsEditor` into it, vs (b) a Wait States `InputGroup` sibling next to `StateMappingsEditor` without regrouping. Relocating the existing mappings editor is a brownfield refactor whose scope DESIGN must size. | Locked (user-confirmed 2026-06-05; container shape DESIGN-flagged). |

## Wave: DISCUSS / [REF] Cross-cutting impact checklist (DoR item 7 — hard gate)

### RBAC

**Defining wait states (config write)** is a settings mutation on the Team/Portfolio config aggregate. It
inherits the SAME settings-edit authorization as the rest of the team/portfolio settings form (the
state-config cluster — `StatesList`, `StateMappingsEditor` — and the Flow Metrics Configuration fields:
SLE, WIP limits, Blocked States). It is NOT gated by anything specific to the Blocked States sub-section
(which is evolving independently, D12). There is no separate permission for wait states; it inherits the
existing settings-edit permission, which flows through `IRbacAdministrationService` with UI gating derived
from `useRbac()` (per Architecture). Because `WaitStates` is just another field on the existing settings
DTO, no new permission, no new authorization path, and no new `useRbac()` gate is introduced — the form's
existing edit-gating covers it.

**Viewing flow efficiency** (overview tile, chart number, wait-bar highlight) is read-access, identical to
viewing any other metric widget on the team/portfolio detail pages. It inherits the existing read-gating for
those pages; no new read permission. N/A for any NEW authorization, because the feature adds no new protected
operation — a config field on an already-gated form and read surfaces on already-gated pages.

### Lighthouse-Clients (CLI + MCP)

**Config write**: `WaitStates` rides the existing team/portfolio settings endpoint/DTO (D8). If the CLI/MCP
clients expose team/portfolio settings get/update, the new `waitStates` array appears additively in that
existing contract — clients that round-trip the settings object will carry it through without change; a client
that wants to SET wait states needs the field added to its settings model (a non-breaking additive field, same
as when `blockedStates` was added). **No new endpoint, so no `FEATURE_REQUIRES_SERVER_NEWER_THAN` version gate
is required for the config write.**

**Read surfaces**: the overview efficiency value and the chart efficiency number/wait-flag are NEW data. If
DESIGN exposes them via a NEW endpoint (e.g. a `flowEfficiency` overview endpoint), and if the clients wrap it,
that wrapping client method MUST be version-gated: pin the feature to **strictly newer than the last released
Lighthouse version** (record/bump the baseline in the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry to
the current latest release when wrapping it), so an old server returning an opaque 404 surfaces as a clear
"upgrade Lighthouse" error. If instead the efficiency is folded additively into the EXISTING overview/cumulative
responses (D8 alternative) and derived client-side, no version gate is needed. **Action for DELIVER/DESIGN**:
once D8's read-contract split is decided, if a NEW read endpoint is introduced, add the version gate in the
clients repo; if purely additive on existing responses, mark clients N/A. Flagged here so it is not missed.

### Website

**N/A, because** flow efficiency is a standard flow metric surfaced inside the product, not a premium/paid
capability requiring marketing. It is an additive enhancement of an existing chart plus an overview tile — no
new pricing tier, no gated premium feature. If the public website maintains a feature/metrics list, a one-line
mention of "Flow Efficiency" could be added opportunistically, but no website change is required for this
feature to deliver its value. (Contrast: a new premium feature WOULD require website surfacing — this is not one.)

## Wave: DISCUSS / [REF] Slice breakdown (Elephant Carpaccio)

Order keeps config + computation first (locked). Each slice ships end-to-end ≤1 day with a named learning
hypothesis. Slice briefs: `docs/feature/wait-states-flow-efficiency/slices/slice-NN-*.md`.

| Slice | Story | One-line goal | Learning hypothesis |
|---|---|---|---|
| 01 | US-01 | Config + flow-efficiency computation foundation: mapping-aware `WaitStates` field editable in the state-config cluster (next to State Mappings), and the backend computes `activeDoingTime / totalDoingTime` over the in-scope set, with `waitTime` over `GetRawStatesForCategory(WaitStates)`. | Teams with idle queues WILL define wait states when the control sits with their other state config and lets them mark a whole mapping in one click, producing a non-100% efficiency value that matches their reality. |
| 02 | US-02 | Per-chart flow-efficiency NUMBER on the cumulative chart — aggregate for the in-scope set, and per-item when the US-05 picker narrows to one item. | Surfacing the number where per-state time already lives (and making it follow the picker) gives leads/coaches the "efficiency for this item" answer without a new screen. |
| 03 | US-03 | Overview Flow Efficiency tile (`flow-overview`, small) showing the aggregate percentage with RAG. | A single efficiency percentage in the overview KPI row is the glance-level signal that prompts leads to open the chart and investigate. |
| 04 | US-04 | Wait-bar colour-highlight on the cumulative chart, distinguishing wait-state bars from active-state bars (colour-blind-safe, composing with completed/ongoing segments). | Making wait-state bars visually pop turns "which states are the waste?" from a tooltip read into a glance, sharpening the constraint conversation. |

**Carpaccio taste tests** (applied; order holds):

- *Each slice independently valuable?* 01 yes (efficiency is computable/inspectable + config persists); 02
  yes (number visible on the chart); 03 yes (overview glance); 04 yes (visual highlight). PASS.
- *Each slice end-to-end (not a horizontal layer)?* 01 touches config model + UI + computation; 02 touches
  computation read + chart number; 03 touches tile + overview; 04 touches chart rendering. None is a pure
  backend-only or pure-UI-only layer. PASS.
- *Config + computation first?* Yes — slice 01 (locked). PASS.
- *Could any later slice ship before 01?* No — 02/03/04 all read the efficiency value and/or WaitStates that
  01 establishes. The order is dependency-correct. PASS.

Note: slices 02–04 could ship in any order after 01 (all depend only on 01). The proposed 02→03→04 leads with
the per-item number (the work item's most specific ask, "efficiency for an individual item if we filter"),
then the glance-level tile, then the visual polish. If a future taste test or user preference reorders 02–04,
that is acceptable; 01-first is the only hard constraint.

## Wave: DISCUSS / [REF] User stories with elevator pitches

Each story has `job_id`, an Elevator Pitch (Before / After referencing a real user-invocable surface /
Decision enabled), and embedded AC. Concrete data: Carlos Mendez (config-admin, Team Phoenix), Priya Nair
(delivery-lead-rte, Team Phoenix).

### US-01 — Define wait states and compute flow efficiency (foundation, slice 01)

**Story**: As a `config-admin` (Carlos), I want to mark which of my team's Doing-states (or mapped states)
are idle "wait" states alongside my other state config — picking a raw Doing-state or a whole State Mapping
in one click — and have Lighthouse compute our flow efficiency (active Doing-time / total Doing-time) from
that, so idle time is told apart from active time instead of every pipeline minute counting as work.

**Job-id**: `job-config-admin-define-wait-states`

#### Elevator Pitch

- Before: Lighthouse counts all Doing-state time as work. Carlos knows "Waiting for Review" and "Ready for
  Test" are idle queues, but there is no way to say so, so no efficiency number can exist — the team looks
  100% productive on paper while two thirds of its lead time is queueing.
- After: open `/teams/{teamId}` settings → the **state-config cluster** (next to State Mappings) → enable
  **Configure Wait States** → add "Waiting for Review" and "Ready for Test" (raw Doing-states), or pick the
  "Waiting" State Mapping to mark its whole group in one click → Save. The team's flow efficiency is now
  computable: `activeDoingTime / totalDoingTime` over the in-scope items, with `waitTime` taken over the
  expanded `GetRawStatesForCategory(WaitStates)` set.
- Decision enabled: whether the team's pipeline time is being measured honestly — Carlos sets up the team so
  the efficiency figure leadership later sees reflects real idle time, not a flattering 100%.

#### Domain Examples

1. **Happy path** — Carlos opens Team Phoenix settings, enables Configure Wait States, adds "Waiting for
   Review" and "Ready for Test" (both raw Doing-states, offered as suggestions), saves. The states
   persist; on reload the chips are still there. Backend now computes efficiency over Phoenix's in-scope
   items: total Doing-time 540d, wait-time 356d → 184/540 = 34% efficient.
2. **Mapping name as a wait state** — Team Phoenix has a State Mapping "Waiting" →
   ["Waiting for Review", "Blocked - External"]. Carlos marks "Waiting" (the mapping name) as a single wait
   state. `GetRawStatesForCategory(WaitStates)` expands it, so time in BOTH "Waiting for Review" and
   "Blocked - External" is attributed to wait — one click, both raw states counted.
3. **Edge case (state outside the Doing set)** — Carlos types "Closed" (a Done-state, not a mapping name)
   into Wait States. It is accepted into the list but contributes nothing to the efficiency denominator
   (only states inside `GetRawStatesForCategory(WaitStates)` within Doing count); the suggestions never
   offered it.
4. **Boundary (no wait states)** — Carlos leaves Configure Wait States off (the default). No efficiency is
   computed; downstream surfaces will read "not configured" (US-03), never a misleading 100%.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Admin marks idle states as wait states in the state-config cluster
  Given Carlos is editing Team Phoenix whose Doing-states include "Waiting for Review" and "Ready for Test"
  When he enables "Configure Wait States" (next to State Mappings) and adds "Waiting for Review" and "Ready for Test" and saves
  Then both states are saved as wait states on the team's settings
  And reopening the settings shows both as removable chips (read-your-writes)

Scenario: Wait-state suggestions offer both raw Doing-states and mapping names
  Given Carlos is editing the Wait States list for Team Phoenix which has a State Mapping "Waiting"
  When he focuses the add-state input
  Then the suggestions include the team's raw Doing-states AND its State Mapping names (e.g. "Waiting")

Scenario: A mapping name marked as a wait state counts all its underlying raw states as wait time
  Given Team Phoenix has a State Mapping "Waiting" grouping ["Waiting for Review", "Blocked - External"]
  And Carlos marks "Waiting" (the mapping name) as a single wait state and saves
  When flow efficiency is computed
  Then time spent in both "Waiting for Review" and "Blocked - External" is attributed to wait time
  And he did not have to enumerate the mapping's raw states

Scenario: Flow efficiency is computed from active vs total Doing-time
  Given Team Phoenix has wait states "Waiting for Review" and "Ready for Test" configured
  And the in-scope items spent 540 days total in Doing-states, of which 356 days were in those two wait states
  When flow efficiency is computed for the team over that in-scope set
  Then the flow efficiency is 34% (184 active days / 540 total Doing days)

Scenario: A state outside the Doing set marked as wait does not distort efficiency
  Given Carlos has added the Done-state "Closed" to the wait states list
  When flow efficiency is computed
  Then "Closed" contributes nothing to the denominator (it is outside GetRawStatesForCategory(WaitStates) within Doing)
  And the efficiency reflects only Doing-time
```

#### Acceptance Criteria

- [ ] A new `WaitStates` (`List<string>`, default empty) exists on the Team/Portfolio settings aggregate,
      persisted via the EXISTING settings endpoint (no new endpoint). It is mapping-aware (D11) — entries are
      raw Doing-states OR `StateMapping.Name`.
- [ ] A "Wait States" control in the state-config cluster next to `StateMappingsEditor` (D12), decoupled from
      the Blocked States sub-section, gated by a "Configure Wait States" toggle, lets the admin add/remove
      wait states via `ItemListManager` with suggestions that include BOTH raw Doing-states AND `StateMappings`
      names. Helper text explains "idle, not active" and the efficiency formula.
- [ ] A wait state defined as a MAPPING NAME counts every underlying raw state's time as wait time: fixture
      mapping "Waiting" → ["Waiting for Review", "Blocked - External"], marking "Waiting" attributes BOTH raw
      states' time to wait (one click, no enumeration), via `GetRawStatesForCategory(WaitStates)` (D11).
- [ ] Saving persists wait states; reloading shows them (read-your-writes).
- [ ] Backend computes flow efficiency = active Doing-time / total Doing-time over the D12-included item set,
      using full-duration attribution (D5), Doing-category time only (D19); `waitTime` = time spent in any raw
      state in the EXPANDED set `GetRawStatesForCategory(WaitStates)` (D2/D11), not a literal `WaitStates` match.
- [ ] A `WaitStates` entry that resolves outside the current Doing set contributes nothing to the denominator.
- [ ] No regression: throughput, forecasts, cycle time, aging, and the existing cumulative bars/RAG are
      unchanged by defining wait states (D9).

#### Outcome KPIs

- **Who**: Teams/portfolios with idle queues in their Doing pipeline (config-admins).
- **Does what**: Define at least one wait state, producing a non-100% flow-efficiency reading.
- **By how much**: ≥ 40% of active teams that have a recognisable wait-state pattern define wait states within
  6 weeks of release.
- **Measured by**: Count of teams/portfolios with non-empty `WaitStates` ÷ active teams (settings inspection).
- **Baseline**: 0% (no wait-state concept exists today).

#### Technical Notes

- New `List<string> WaitStates` on `WorkTrackingSystemOptionsOwner` (alongside `BlockedStates` line 41 and
  `StateMappings` line 45), resolved via the existing `GetRawStatesForCategory` (D11); EF migration adds the
  column (use the `CreateMigration` PowerShell script per CLAUDE.md; mind the stale-migration-DLL
  `--no-incremental` rebuild trap from prior memory). UI lives in the state-config cluster next to
  `StateMappingsEditor` (D12); container shape (wrapper-vs-sibling) is a DESIGN choice, and relocating the
  existing mappings editor would be a brownfield refactor for DESIGN to size. RBAC/Clients/Website resolved in
  the cross-cutting section. Concurrency inherited from the tokened config aggregate
  (`job-config-edit-no-silent-lost-update`, epic-5121) for free.

### US-02 — Flow-efficiency number on the cumulative chart, per-item via the picker (slice 02)

**Story**: As a `delivery-lead-rte` (Priya) or `flow-coach`, I want a flow-efficiency number shown on the
Cumulative Time per State chart — aggregate for the in-scope set, and recomputed for an individual item when
I narrow the existing item picker to one item — so I can read "how much of our (or this item's) time was
active work vs waiting" right where the per-state time already lives.

**Job-id**: `job-spot-flow-efficiency-waste`

#### Elevator Pitch

- Before: the cumulative chart shows WHERE time goes per state, but not what FRACTION of it is waste. To answer
  "how efficient were we?" or "how efficient was this one slow item?" Priya exports state history and computes
  it in a spreadsheet — the exact friction the chart was meant to remove.
- After: open `/teams/{teamId}` → Flow Metrics → the **Cumulative Time per State** chart now shows a
  flow-efficiency figure (e.g. "Flow Efficiency: 34%") for the in-scope set. Use the existing item picker to
  select one slow item → the number recomputes to that item's efficiency ("This item: 18% — 36 of its 44
  Doing-days were waiting").
- Decision enabled: whether the improvement target is idle time vs effort (aggregate), and which specific item
  to raise in a post-mortem (per-item) — "this item was 18% efficient, it sat in queues almost the whole time".

#### Domain Examples

1. **Happy path (aggregate)** — Priya opens the Phoenix cumulative chart with the picker cleared; it shows
   "Flow Efficiency: 34%" matching the overview tile's whole-set value.
2. **Per-item** — Priya picks item PHX-204 (a slipped story). The bars recompute to PHX-204 alone and the
   number recomputes to "18%" (PHX-204 spent 36 of its 44 Doing-days in wait states).
3. **No wait states** — On a team without wait states configured, the chart renders fully (bars/segments
   unchanged) but the efficiency number is suppressed — never a misleading 100%.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Aggregate flow efficiency appears on the cumulative chart
  Given Team Phoenix has wait states configured and items in scope
  When Priya opens the Cumulative Time per State chart with the item picker cleared
  Then a flow-efficiency figure is shown reflecting the whole in-scope set (e.g. 34%)
  And it equals the value on the Flow Efficiency overview tile

Scenario: Picking one item shows that item's flow efficiency
  Given the cumulative chart is open for Team Phoenix
  When Priya selects only item PHX-204 in the item picker
  Then the chart's flow-efficiency number recomputes to PHX-204's efficiency (e.g. 18%)
  And the overview tile and the chart RAG are unchanged (they reflect the whole in-scope set)

Scenario: Clearing the picker returns to the aggregate efficiency
  Given Priya has PHX-204 selected and the number shows 18%
  When she clears the item picker
  Then the flow-efficiency number returns to the aggregate value (34%)

Scenario: No wait states suppresses the number without faking 100%
  Given Team Atlas has no wait states configured
  When Priya opens its cumulative chart
  Then the bars and segments render unchanged
  And no flow-efficiency number is shown (suppressed, not 100%)
```

#### Acceptance Criteria

- [ ] A flow-efficiency figure is displayed on the cumulative chart for the current in-scope set (picker cleared).
- [ ] With the US-05 picker narrowed to a selection, the number recomputes over that selection (one item → that
      item's efficiency) — per D5.
- [ ] The overview tile (US-03) and the cumulative chart RAG do NOT change when the picker narrows (D5/D18).
- [ ] With picker cleared, the chart number equals the overview tile's value (same whole-set scope).
- [ ] With no wait states configured, the number is suppressed (D3); the rest of the chart is unchanged.
- [ ] With zero total Doing-time in the displayed scope, the number shows "no data in scope" (D4), no
      division error.

#### Outcome KPIs

- **Who**: Delivery leads / flow coaches viewing the cumulative chart on teams with wait states.
- **Does what**: Read the flow-efficiency number (aggregate or per-item) instead of exporting to a spreadsheet.
- **By how much**: ≥ 15% of cumulative-chart sessions on wait-state-configured teams exercise the per-item
  efficiency (picker narrowed) within 8 weeks.
- **Measured by**: Frontend usage telemetry on picker-narrowed chart views (blocked on Epic 5015 self-hosted
  telemetry; until then, qualitative via customer feedback / shared decks).
- **Baseline**: 0% (no efficiency number exists today).

#### Technical Notes

- Reuses the existing cumulative endpoints and US-05 picker. The efficiency value is either an additive field
  on the `cumulativeStateTime` response or derived client-side from the existing per-state bars + WaitStates
  (D8 — DESIGN picks). Telemetry KPI is currently un-instrumentable on self-hosted instances (memory:
  self-hosted telemetry gap, Epic 5015) — flagged as a guarded KPI.

### US-03 — Flow Efficiency overview tile (slice 03)

**Story**: As a `delivery-lead-rte` (Priya), I want a Flow Efficiency tile in the Flow Overview row showing the
team's aggregate efficiency percentage with a RAG colour, so I get a glance-level signal of how much of our lead
time is waste — alongside the WIP, Predictability, and Age tiles I already scan.

**Job-id**: `job-spot-flow-efficiency-waste`

#### Elevator Pitch

- Before: the Flow Overview answers "how is my flow performing right now?" with WIP, predictability, and age
  tiles — but nothing says what fraction of our time is idle. Priya has no glance-level efficiency signal; she'd
  have to open the chart and reason about per-state bars.
- After: open `/teams/{teamId}` → land on **Flow Overview** → a new **Flow Efficiency** tile reads "34%" in an
  `act`-coloured (red) chip, sitting beside the existing KPI tiles.
- Decision enabled: whether flow efficiency is worth a deeper look this period — a red 34% tile prompts Priya to
  open the cumulative chart and investigate which wait states are dragging it.

#### Domain Examples

1. **Happy path** — Team Phoenix has wait states; the tile reads "34%" with an `act` (red) RAG.
2. **Not configured** — Team Atlas has no wait states; the tile reads "Flow Efficiency — not configured" with a
   pointer to define wait states (D3), never "100%".
3. **No data in scope** — A brand-new team with no Doing-time in the window; the tile reads "no data in scope"
   (D4), distinct from "not configured".

#### UAT Scenarios (BDD)

```gherkin
Scenario: Flow Efficiency tile shows the aggregate percentage with RAG
  Given Team Phoenix has wait states configured and Doing-time in scope, computing to 34% efficiency
  When Priya opens the Flow Overview for Team Phoenix
  Then a Flow Efficiency tile shows "34%" with an "act" RAG colour (below 40%)

Scenario: Unconfigured team shows "not configured", never a misleading 100%
  Given Team Atlas has no wait states configured
  When Priya opens its Flow Overview
  Then the Flow Efficiency tile reads "not configured" with a pointer to define wait states
  And it does not read "100%"

Scenario: No Doing-time in scope reads "no data in scope"
  Given a team with wait states configured but no Doing-time in the selected window
  When Priya opens its Flow Overview
  Then the Flow Efficiency tile reads "no data in scope" and no division error occurs

Scenario: RAG thresholds classify efficiency
  Given a team computing to 72% efficiency
  When the Flow Efficiency tile renders
  Then it shows a "sustain" RAG colour (60% or above)
```

#### Acceptance Criteria

- [ ] A `flowEfficiency` widget renders in the `flow-overview` category, size `small`, on team and portfolio
      detail pages (D7).
- [ ] With wait states configured and Doing-time in scope, the tile shows the aggregate efficiency percentage
      (whole in-scope set; not affected by the chart picker — D5).
- [ ] RAG mapping (D10): `act` < 40%, `observe` 40–60%, `sustain` ≥ 60%; reflected in `widgetInfoMetadata.ts`
      `statusGuidance`.
- [ ] No wait states → "not configured" with a define-wait-states pointer (D3), never 100%.
- [ ] Zero Doing-time in scope → "no data in scope" (D4), no division error.
- [ ] No regression in the rest of the `flow-overview` category; existing tiles render unchanged.

#### Outcome KPIs

- **Who**: Delivery leads / RTEs scanning the Flow Overview on wait-state-configured teams.
- **Does what**: Read the aggregate flow-efficiency percentage at a glance.
- **By how much**: On teams with wait states, ≥ 30% of detail-page sessions include a view of the Flow Efficiency
  tile within 6 weeks (it sits in the default-landing Flow Overview row, so high exposure expected).
- **Measured by**: Overview-tile view telemetry (guarded by Epic 5015 self-hosted telemetry gap; qualitative
  until then).
- **Baseline**: 0% (no tile exists today).

#### Technical Notes

- New entry in `categoryMetadata.ts` (`flow-overview`, `small`), `widgetInfoMetadata.ts` (description, RAG
  guidance, learn-more URL), and `trendPolicies` (`none` or `previous-period` — DESIGN picks). Served via a small
  efficiency read (new endpoint or folded into overview payload, D8). Telemetry KPI guarded by Epic 5015.

### US-04 — Wait-bar colour-highlight on the cumulative chart (slice 04)

**Story**: As a `delivery-lead-rte` (Priya), I want the wait-state bars on the Cumulative Time per State chart
rendered in a visually distinct treatment from the active-state bars, so I can see at a glance which states are
idle waste — making the "where is our waste?" read instant instead of a tooltip hunt.

**Job-id**: `job-spot-flow-efficiency-waste`

#### Elevator Pitch

- Before: on the cumulative chart, every Doing-state bar looks the same; Priya can't tell idle "wait" states
  from active ones without checking the settings or hovering each bar. The waste is in the chart but not visible.
- After: open `/teams/{teamId}` → Flow Metrics → the cumulative chart now renders the **wait-state bars** ("Waiting
  for Review", "Ready for Test") in a distinct colour family with a legend entry, while still showing the
  completed/ongoing segment split. The tall bars that are ALSO wait-coloured jump out as the waste.
- Decision enabled: which specific wait states to attack — "the two tallest bars are both wait states; removing
  those queues is where the improvement budget goes".

#### Domain Examples

1. **Happy path** — Team Phoenix's "Waiting for Review" and "Ready for Test" bars render in the wait colour with
   a legend entry; "In Progress" and "In Review" (active) bars render in the active colour. Each bar still shows
   its solid/hatched completed/ongoing split.
2. **Colour-blind safety** — The wait/active distinction is reinforced by a legend label (and/or icon/pattern),
   not colour alone, so a colour-blind user can still tell wait bars apart.
3. **No wait states** — On a team without wait states, no bars are highlighted; the chart renders exactly as it
   does today (no regression).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Wait-state bars are visually distinct from active-state bars
  Given Team Phoenix has wait states "Waiting for Review" and "Ready for Test" configured
  When Priya opens the Cumulative Time per State chart
  Then the "Waiting for Review" and "Ready for Test" bars render in a distinct wait treatment
  And the active-state bars render in the active treatment
  And a legend distinguishes wait bars from active bars

Scenario: The wait highlight composes with the completed/ongoing segments
  Given the wait-state bars are highlighted
  When Priya inspects a wait-state bar
  Then it still shows a solid completed-contribution base and a hatched ongoing-contribution top
  And the wait colouring is legible together with the segment split

Scenario: Colour-blind users can still tell wait bars apart
  Given a colour-blind user views the chart
  When wait-state bars are highlighted
  Then the wait distinction is conveyed by a legend label or pattern, not colour alone

Scenario: No wait states means no highlight and no regression
  Given Team Atlas has no wait states configured
  When Priya opens its cumulative chart
  Then no bars are highlighted and the chart renders exactly as before
```

#### Acceptance Criteria

- [ ] Bars for states in `GetRawStatesForCategory(WaitStates)` (the expanded set, so a mapped wait state
      highlights all its underlying raw-state bars — D11) render in a treatment visually distinct from
      active-state bars on the cumulative chart (D6), with a legend entry.
- [ ] The wait highlight composes with the existing completed/ongoing segment split (solid base / hatched top)
      and remains legible together.
- [ ] The wait/active distinction is colour-blind-safe (legend label and/or pattern/icon, not colour alone).
- [ ] With no wait states configured, no bars are highlighted; the chart is unchanged (no regression).
- [ ] The highlight reads from the SAME `WaitStates` list — through the SAME `GetRawStatesForCategory`
      resolution — the efficiency computation uses (single source; both agree on a mapping's raw states —
      shared-artifact registry HIGH-risk item).

#### Outcome KPIs

- **Who**: Delivery leads / flow coaches viewing the cumulative chart on wait-state-configured teams.
- **Does what**: Identify which states are wait states at a glance (without hovering or checking settings).
- **By how much**: Qualitative — wait-highlighted charts appear in customer-shared retro/leadership decks within
  8 weeks of release (the "make the waste visible" bet).
- **Measured by**: Customer feedback / shared decks (telemetry guarded by Epic 5015).
- **Baseline**: 0 (no wait highlight exists today).

#### Technical Notes

- Frontend-only rendering change on the existing cumulative chart, driven by the `WaitStates` list (additive on
  the response or client-side from settings, per D8). Reuse the chart's existing colour-blind-safe conventions
  (the cumulative feature already handles colour-blind care for the completed/ongoing segments).

## Wave: DISCUSS / [REF] Shared artifacts registry

| Artifact | Source of truth | Consumers | Integration risk | Validation |
|---|---|---|---|---|
| `waitStates` | `WorkTrackingSystemOptionsOwner.WaitStates` (List<string>, mapping-aware — raw Doing-states OR `StateMapping` names, resolved via `GetRawStatesForCategory`) on Team/Portfolio settings | Config UI (Wait States in the state-config cluster); efficiency computation (waitTime over the expanded set); cumulative chart wait-bar highlight | **HIGH** — the SAME list (after `GetRawStatesForCategory` expansion) drives the efficiency math AND the chart highlight; divergent reads, or one surface skipping the mapping expansion, would confuse users | A state (raw OR mapping name) marked wait is both highlighted on the chart AND moved into the wait portion of the efficiency denominator — both surfaces must read the one `WaitStates` field through the SAME `GetRawStatesForCategory` resolution |
| `flowEfficiency` | Computed: active(non-wait) Doing-time / total Doing-time over the in-scope set (or picker selection), full-duration per cumulative D5 | Overview tile (aggregate, whole set); chart number (in-scope set; follows US-05 picker) | **MEDIUM** — tile is always whole-set; chart number follows the picker (D5) — intentional scope difference, must be documented so users don't read them as a contradiction | Picker cleared → chart number == tile; one item picked → chart number is that item's efficiency, tile unchanged |

## Wave: DISCUSS / [REF] Outcome KPIs (feature-level)

### Objective

Within 6–8 weeks of release, teams with idle queues can see and name what fraction of their lead time is waste
— turning "where does our time go?" into "two thirds of it is waiting", with a defensible number on the
overview, the chart, and (when filtered) per item.

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|-----|-----------|-------------|----------|-------------|------|
| 1 | Config-admins on teams with idle queues | Define ≥1 wait state (non-100% efficiency reading) | ≥40% of eligible teams in 6 weeks | 0% | Settings inspection (non-empty `WaitStates`) | Leading (secondary) |
| 2 | Delivery leads on wait-state teams | View the Flow Efficiency tile in a detail-page session | ≥30% of sessions in 6 weeks | 0% | Overview-tile view telemetry | Leading (primary) |
| 3 | Leads / coaches on wait-state teams | Exercise per-item efficiency (picker narrowed) on the chart | ≥15% of chart sessions in 8 weeks | 0% | Picker-narrowed chart-view telemetry | Leading (primary) |

### Metric Hierarchy

- **North Star**: fraction of wait-state-eligible teams that have defined wait states and regularly view flow
  efficiency (adoption of the honest-efficiency loop).
- **Leading indicators**: wait-state definition rate (KPI 1); efficiency-tile view rate (KPI 2); per-item
  efficiency usage (KPI 3).
- **Guardrail metrics** (must NOT degrade): existing cumulative-chart render correctness/RAG; throughput/
  forecast/cycle-time/aging numbers (D9 — must be byte-identical before and after defining wait states);
  detail-page load time.

### Measurement plan & telemetry caveat

All three KPIs require frontend/usage telemetry. Per the **self-hosted telemetry gap (Epic 5015 blocker,
memory)**, Lighthouse customer instances do not phone home; cross-instance KPI collection is blocked until
opt-in telemetry ships. Until then these KPIs are tracked **qualitatively** (customer feedback, shared decks,
support conversations) and instrumented for when telemetry lands. KPI 1 (settings inspection) is measurable on
any instance the team can inspect directly. Flagged for the DEVOPS/platform-architect handoff.

## Wave: DISCUSS / [REF] Out of scope

- **Wait time affecting throughput / forecasts / cycle time / aging** — out of scope (D9). Wait states are a
  labelling overlay for efficiency + highlight only.
- **Configurable RAG thresholds for Flow Efficiency** — out of scope (D10); 40/60 are baseline defaults. Fold
  into the existing per-widget RAG-config work if asked.
- **Per-item chronology / ordered wait timeline** — out of scope (the cumulative feature already dropped
  chronology, D15). Per-item efficiency is a single number, not a timeline.
- **Business-hours / working-calendar wait time** — out of scope; wait time is wall-clock/calendar, matching the
  cumulative chart (D16) and existing cycle-time/age charts.
- **A wait-vs-active toggle that recomputes throughput** — out of scope; efficiency is a read-only overlay.
- **Blocked-vs-wait reconciliation** — `BlockedStates` and `WaitStates` are independent overlays; a state could
  in principle be in both, but reconciling/merging them is out of scope. (Blocked-time attribution is Epic #5074,
  a separate mechanism.)
- **Touching the Blocked States config / sub-section** — explicitly out of scope (D12). Blocked config is
  evolving independently; Wait States is decoupled from it and lives in the state-config cluster.
- **Defining new State Mappings as part of this feature** — out of scope; Wait States REUSES the existing
  `StateMappings` (D11) and the existing `StateMappingsEditor`. This feature does not change how mappings are
  created, only lets a mapping name be selected as a wait state.
- **Trend-over-time of flow efficiency** (efficiency history chart) — out of scope for MVP; the tile and chart
  number are point-in-window reads. A history view could fold into the delivery-metrics over-time work later.

## Wave: DISCUSS / [REF] Definition of Done

1. All four stories (US-01..US-04) pass their ACs via integration tests (NUnit + Moq + EF InMemory +
   WebApplicationFactory for the backend settings field and efficiency computation; Vitest + RTL for the config
   sub-section, overview tile, chart number, and wait-bar highlight).
2. Flow-efficiency math verified against a fixture with known Doing-state durations and known wait states:
   aggregate efficiency = active/total Doing-time, full-duration (D5), Doing-only (D19); per-item efficiency for
   a single picked item; a wait state defined as a MAPPING NAME attributes all its underlying raw states' time
   to wait via `GetRawStatesForCategory(WaitStates)` (D11 — fixture "Waiting" → ["Waiting for Review",
   "Blocked - External"]); a wait entry resolving outside the Doing set contributes nothing to the denominator.
3. Edge cases verified: no wait states → "not configured" (never 100%, D3); zero Doing-time → "no data in scope"
   (no division error, D4); efficiency 0% (all Doing-time in wait states) renders with `act` RAG.
4. Picker interaction verified: chart number follows the picker (per-item when one item selected); tile and RAG
   stay on the whole in-scope set (D5/D18); picker cleared → chart number == tile.
5. Wait-bar highlight verified: wait-state bars distinct from active bars, composing with completed/ongoing
   segments, colour-blind-safe (legend/pattern not colour-alone, D6); reads from the same `WaitStates` source as
   the math.
6. No-regression verified (D9 guardrail): throughput, forecast, cycle time, aging, and the existing cumulative
   bars/RAG are unchanged when wait states are defined; existing `ragRules.test.ts` and metrics tests pass
   unchanged.
7. EF migration created via the `CreateMigration` script across providers (mind the `--no-incremental`
   stale-migration-DLL trap); read-your-writes verified on settings reload.
8. RBAC: wait-state edit inherits existing settings-edit gating (no new permission); read surfaces inherit page
   read-gating — verified no new authorization path introduced.
9. Clients (CLI + MCP): if a NEW read endpoint is introduced for efficiency (D8), the wrapping client method is
   version-gated (`FEATURE_REQUIRES_SERVER_NEWER_THAN`, strictly newer than the last released version); if purely
   additive on existing responses, clients are N/A — decision recorded in DESIGN.
10. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md); SonarCloud gate passes on PR.
11. Mutation testing (Stryker.NET backend; Stryker frontend): ≥80% kill rate for new code (efficiency
    computation, RAG mapping, config handlers).
12. Docs updated: `widgetInfoMetadata.ts` learn-more URLs (Flow Efficiency tile + the cumulative chart's wait
    highlight) point to a doc page added in the same wave; screenshot of the tile and the highlighted chart.

## Wave: DISCUSS / [REF] DoR validation (9-item hard gate)

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1. Problem statement clear, domain language | PASS | Two JTBD one-liners in domain language (idle vs active time, flow efficiency, wait states); per-story Problem framings with Carlos/Priya. |
| 2. User/persona with specific characteristics | PASS | `config-admin` (Carlos Mendez, Team Phoenix admin) and `delivery-lead-rte` (Priya Nair) + `flow-coach` secondary; all existing SSOT personas, jobs added. |
| 3. 3+ domain examples with real data | PASS | Each of US-01..US-04 has 3+ domain examples with real names/data (Team Phoenix/Atlas, PHX-204, 540d/356d/34%, 36-of-44 days); US-01 now has 4, incl. the mapping "Waiting" → ["Waiting for Review", "Blocked - External"] case. |
| 4. UAT in Given/When/Then (3–7 scenarios) | PASS | US-01: 5 (incl. mapping-name-as-wait + suggestions-include-mappings), US-02: 4, US-03: 4, US-04: 4 scenarios — all in the 3–7 range, business-outcome titles. |
| 5. AC derived from UAT | PASS | Each story's AC checklist maps to its scenarios + locked decisions; US-01 mapping-aware AC traces to the new mapping-name scenario (D11). |
| 6. Right-sized (1–3 days, 3–7 scenarios) | PASS | 4 stories, each ≤1 day, 4–5 scenarios each; scope assessment PASS (4 stories, 2 contexts, ~3.5 days). Mapping-aware reuse of existing `GetRawStatesForCategory`/`StateMappingsEditor` adds no new context. |
| 7. Technical notes: constraints/dependencies + cross-cutting (RBAC/Clients/Website) | PASS | Per-story technical notes; full RBAC/Clients/Website cross-cutting section with explicit resolutions (RBAC re-confirmed: inherits settings-edit gating, NOT Blocked-States-specific, no new permission; Clients version-gate IF new endpoint else N/A; Website N/A with reason). |
| 8. Dependencies resolved or tracked | PASS | Depends on shipped `state-time-cumulative-view` (Epic 4144, in main) for the chart + US-05 picker; settings concurrency inherited from epic-5121 (shipped). Slice 01 is the only intra-feature dependency for 02–04 (tracked in slice briefs). |
| 9. Outcome KPIs with measurable targets | PASS | Feature-level KPI table + per-story KPIs (who/does-what/by-how-much/baseline/measured-by); telemetry caveat (Epic 5015) flagged. |

**DoR Status: PASSED** — all 9 items pass with evidence. Note: KPIs 2 & 3 are guarded by the Epic 5015
self-hosted telemetry gap; they have targets and measurement methods defined, tracked qualitatively until
telemetry lands (this does not fail DoR item 9 — the KPIs are defined and measurable in principle; the gap is an
instrumentation dependency, recorded for DEVOPS).

## Wave: DISCUSS / [REF] Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Users confuse the tile (whole-set) and chart number (picker-following) as contradictory (D5) | Medium | Medium | Document the scope difference in learn-more text; tile labelled "team" / chart number labelled to its scope. |
| `WaitStates` and the highlight read different lists (divergence) | Low | High | Single-source shared-artifact (registry HIGH-risk item); integration test asserts both surfaces read the one field. |
| "Not configured" vs "100%" confusion if D3 is mis-implemented | Low | High | Explicit AC + UAT scenario asserting "not configured" never reads 100%; guardrail in DoD. |
| Defining wait states silently shifts throughput/forecast numbers | Low | High | D9 + DoD guardrail: numbers byte-identical before/after; regression test. |
| Telemetry-based KPIs un-measurable on self-hosted (Epic 5015) | High | Low | Track qualitatively; KPI 1 measurable via settings inspection; flagged for DEVOPS. |

## Wave: DISCUSS / [REF] Handoff package (to DESIGN — solution-architect)

- This `feature-delta.md` (narrative requirements, locked decisions, AC, KPIs, DoR PASSED).
- Slice briefs `slices/slice-01..04-*.md`.
- Journey SSOT `docs/product/journeys/wait-states-flow-efficiency.yaml` (two journeys, embedded design decisions,
  shared-artifact registry, error paths).
- Jobs SSOT additions in `docs/product/jobs.yaml` (`job-config-admin-define-wait-states`,
  `job-spot-flow-efficiency-waste`); persona updates (config-admin, delivery-lead-rte).
- **Open DESIGN decisions explicitly deferred** (D8): (a) whether the chart efficiency number / wait-flag is an
  additive field on the `cumulativeStateTime` response or derived client-side; (b) whether the overview tile is a
  new endpoint or folded into the overview payload — and the consequent Clients version-gate decision; (c) the
  exact `trendPolicy` for the `flowEfficiency` tile; (d) **(D12)** the state-config container shape — a new
  "Advanced State config" wrapper that relocates the existing `StateMappingsEditor`, vs a sibling Wait States
  `InputGroup` next to it without regrouping; DESIGN must size the brownfield refactor if it picks the wrapper.
