<!-- markdownlint-disable MD024 -->
# Feature Delta - flow-overview-named-cycle-time (ADO Story 5509 "Alternative Cycle Time also visible in Flow Overview")

DISCUSS wave output. Density: lean + ask-intelligent, Tier-1 [REF] only - no Tier-2 expansion triggered
(see Wave-decisions). UX research depth: Lightweight (extends the shipped `multiple-cycle-times`
journey rather than re-mapping it). Premium feature. Feature-id: `flow-overview-named-cycle-time`.

## [REF] Summary

Epic 5251 (`multiple-cycle-times`, SHIPPED) let users define named cycle times - e.g. "Concept to
Cash" = Planned->Done - and read them on the **Cycle Time Scatterplot** (`flow-metrics` category) and
the cumulative-time-per-state chart. Story 5509 extends that read to the **Flow Overview**
(`flow-overview` category), where the `percentiles` widget (`CycleTimePercentiles`) today renders the
default started->finished window ONLY, with no selector.

The delivery lead lands on Flow Overview first ("How is my flow performing right now?"). Today, to ask
"where does our time really go?" she must leave that page for the Flow Metrics tab. This story brings
the named-cycle-time switch to the widget she already looks at, and makes the widget's three
companions - **RAG**, **View Data**, and **Trend** - follow the selection so the widget never
contradicts itself.

Analysis only. No new config surface (definitions are already defined per Epic 5251); no forecasting.

## [REF] Personas (SSOT)

- **delivery-lead-rte** (`docs/product/personas/delivery-lead-rte.yaml`) - PRIMARY and only. Same
  persona, same job as the shipped scatterplot read; a new surface for it.
- **config-admin** - referenced only as a PRE-REQUISITE (someone must have defined a definition via
  Epic 5251). No config change in this story.

No new personas invented.

## [REF] JTBD one-liner (SSOT: `docs/product/jobs.yaml`)

- **job-delivery-lead-see-time-over-custom-window** (delivery-lead-rte) - "See where time really goes
  over a custom start->end window, not just started->finished." Opportunity: importance 4 /
  satisfaction 1 / **gap 3** (as scored 2026-06-08).

**No new job.** Story 5509 is the SAME job on a NEW surface: same persona, same situation (retro /
leadership review), same motivation, same outcome. Creating a second job would duplicate knowledge and
inflate the backlog with a tech-surface entry - the exact anti-pattern the STANDING rule
"Tech-surface vs value-outcome backlog" warns about. The existing job gains a `surfaces` amendment
recording Flow Overview, and its `satisfaction` is re-scored **after** delivery, not now.

Every story below traces to `job-delivery-lead-see-time-over-custom-window`.

## [REF] Locked decisions (D11-D16)

Continues the D-series of `docs/product/journeys/multiple-cycle-times.yaml` (D1-D10 already locked
there; D1/D2/D5/D9/D10 are load-bearing here and are NOT re-litigated).

| ID | Decision |
|----|----------|
| D11 | **RAG under a named selection is NEUTRAL + explained.** `computeCycleTimePercentilesRag` (`ragRules.ts:174`) is SLE-anchored, and the SLE is ONE `(ServiceLevelExpectationProbability, ServiceLevelExpectationRange)` pair per owner (`WorkTrackingSystemOptionsOwner.cs:33-35`) defined against the DEFAULT started->finished window. A named window is legitimately wider, so comparing it to the default SLE renders a FALSE red (e.g. Concept-to-Cash P85 47d vs SLE "85% @ 12d"). Therefore: named selected -> `ragStatus: "none"` + tip "SLE applies to the Default cycle time. Named cycle times have no SLE target." Default selected -> today's RAG unchanged. Rejected: per-definition SLE (reopens Epic 5251's config surface + EF migration - a later story if demand appears); reusing the default SLE (knowingly ships a misleading red). |
| D12 | **The Trend endpoint gains `definitionId`.** `GET /{teams\|portfolios}/{id}/metrics/cycleTimePercentilesInfo` takes no `definitionId` (`MetricsService.ts:711`, `TeamMetricsController.cs:362`) - it only knows the default window. Add `[FromQuery] int? definitionId = null`, mirroring `cycleTimePercentiles` (`TeamMetricsController.cs:140`). The service change is small: `GetCycleTimePercentilesInfoForTeam` already calls the percentile method twice (current + previous period, `TeamMetricsService.cs:693-697`); the named path calls the EXISTING `GetNamedCycleTimePercentilesForTeam` twice instead. Rejected: hiding Trend for named (cheaper, but "is our Concept-to-Cash improving?" is the persona's core question - see `delivery-lead-rte.yaml` goals). |
| D13 | **Selection state lives at `BaseMetricsView`, consumed ONLY by the percentiles widget.** Intent: no cross-tab coupling; the scatterplot keeps its own local `useState` (`CycleTimeScatterPlotChart.tsx:169`) and is NOT touched (no Epic 5251 regression surface). Mechanism: the ViewData payload is assembled at `BaseMetricsView` level by `buildViewData()` (`BaseMetricsView.tsx:466`) and handed to `WidgetShell` as a prop - so widget-local state could not reach it and D15 would be impossible. Lifting one level up resolves that. This is NOT a new pattern: `cumulativeScopeDefinitionId` + `onCumulativeScopeChange` (`BaseMetricsView.tsx:1191, 1462`) already does exactly this for `CumulativeStateTimeScopeControl`, including the invalid-definition self-reset (D5). Follow that component's shape. Rejected: shared-with-scatterplot (changes shipped behaviour); shared + persisted (needs a persistence ADR - a later story). |
| D14 | **Scope: the `flow-overview` `percentiles` widget, Team AND Portfolio.** Both `TeamMetricsView` and `PortfolioMetricsView` already thread `cycleTimeDefinitions` through `BaseMetricsView` (`BaseMetricsView.tsx:136, 1071`), so Portfolio is near-free and matches D7. OUT: `workItemAgePercentiles` (measures age of IN-PROGRESS items - a named cycle time is a closed-item start->end window; conceptually N/A); `cycleTimePbc` (Predictability tab - not named in 5509). |
| D15 | **View Data follows the selection.** The highlight column becomes the named duration: title = definition name, value = `item.namedCycleTimes.find(v => v.definitionId === id)?.days` (`WorkItem.ts:16` - already on the item, already loaded, no new fetch). Default selected -> `item.cycleTime` as today (`BaseMetricsView.tsx:471-475`). Rationale: 5509's text names View Data explicitly, and without this the widget contradicts itself on screen (P85 reads 47d while the table behind it lists 12d values) - the exact incoherence the `cycleTimeDefinitions` shared-artifact registry entry flags as HIGH risk. Cost is ~one conditional `valueGetter` given D13. |
| D16 | **View Data rows are filtered to the named population.** D9 excludes items that never cross BOTH boundaries from a named series, so the percentiles are computed over fewer items than `cycleTimeData` holds. The table lists exactly those items - table population == percentile population, so "see the data behind them" stays literally true. Default selected -> unfiltered, as today. Rejected: listing all closed items with blank cells (a superset of the population the P85 came from). |

## [REF] Technical grounding (verified in code, not assumed)

| Fact | Evidence |
|------|----------|
| `flow-overview` category holds `{ widgetKey: "percentiles", size: "small" }` | `categoryMetadata.ts:62` |
| Widget is `<CycleTimePercentiles percentileValues={ctx.percentileValues} />` - no selector, no definitions prop | `BaseMetricsView.tsx:882`; `CycleTimePercentiles.tsx` |
| Scatterplot ALREADY has the selector + `onFetchNamedCycleTimePercentiles` | `BaseMetricsView.tsx:916`; `CycleTimeScatterPlotChart.tsx:299-328` |
| `getCycleTimePercentiles(id, start, end, definitionId?)` exists and is wired | `MetricsService.ts:332`; `TeamMetricsController.cs:140` |
| `GetNamedCycleTimePercentilesForTeam` exists, cache key `NamedCycleTimePercentiles_{start}_{end}_Def_{id}` | `TeamMetricsService.cs:340-346` |
| Trend source `cycleTimePercentilesInfo?.comparison`; endpoint has NO definitionId | `BaseMetricsView.tsx:1619`; `MetricsService.ts:711` |
| Trend cache key is `CycleTimePercentilesInfo_{start}_{end}` - **no definition segment** | `TeamMetricsService.cs:691` |
| RAG is SLE-anchored; SLE is one pair per owner | `ragRules.ts:174`; `WorkTrackingSystemOptionsOwner.cs:33-35` |
| `trendPolicies.percentiles = "previous-period"` | `categoryMetadata.ts:112` |
| ViewData payload built at BaseMetricsView, passed to WidgetShell as a prop | `BaseMetricsView.tsx:466`; `WidgetShell.tsx:40-56` |
| Lifted-scope precedent (controlled selector + invalid self-reset) | `CumulativeStateTimeScopeControl.tsx`; `BaseMetricsView.tsx:1191, 1462` |
| `namedCycleTimes?: INamedCycleTimeValue[]` already on the work item | `WorkItem.ts:16`; `NamedCycleTime.ts` |
| Premium gate is free - `namedCycleTimeDefinitions` is `[]` when `!isPremium` | `BaseMetricsView.tsx:1143-1152` |

**Trap flagged for DESIGN (D12):** the Trend cache key `CycleTimePercentilesInfo_{start}_{end}`
(`TeamMetricsService.cs:691`) has no definition segment. Adding `definitionId` WITHOUT extending the
key makes a named trend collide with the default trend in cache - first caller wins, second gets the
other's answer. The fix mirrors the existing named key (`..._Def_{definitionId}`), which already gets
this right. Same applies to the Portfolio twin.

## [REF] User stories

### US-01 - Read a named cycle time's percentiles on Flow Overview

`job_id: job-delivery-lead-see-time-over-custom-window`

Priya (delivery-lead-rte) opens Team Phoenix and lands on Flow Overview - the page that answers "how
is my flow performing right now?". The Cycle Time Percentiles widget tells her P85 is 12 days, and it
looks fine. But features keep landing late. She already knows (from the scatterplot, Epic 5251) that
"Concept to Cash" tells a different story - but that lives on another tab, and Overview is where she
and her leadership actually look.

### Elevator Pitch
Before: The Flow Overview Cycle Time Percentiles widget only ever shows the default started->finished window; seeing a named window means leaving Overview for the Flow Metrics scatterplot.
After: run `open /teams/1 -> Flow Overview -> Cycle Time Percentiles -> select "Concept to Cash"` → sees `95th 62 days / 85th 47 days / 70th 34 days / 50th 21 days`, RAG footer neutral with "SLE applies to the Default cycle time. Named cycle times have no SLE target."
Decision enabled: whether the constraint to attack this quarter is upstream of "started" (the validation queue) or inside the build - read on the page she already lands on.

**Acceptance criteria**

1. Given Team Phoenix is premium and has a named cycle time "Concept to Cash" (Planned->Done), when Priya opens Flow Overview, then the Cycle Time Percentiles widget shows a selector defaulting to "Default" and listing "Concept to Cash".
2. Given the selector is on "Default", when Priya reads the widget, then the percentiles, RAG, View Data and Trend are byte-identical to today's behaviour (no regression).
3. Given Priya selects "Concept to Cash", when the widget re-renders, then the 50/70/85/95 values are that definition's percentiles, fetched via `getCycleTimePercentiles(entityId, start, end, definitionId)` - the endpoint that already exists.
4. Given "Concept to Cash" is selected, when Priya reads the RAG footer, then RAG is neutral (`none`) with the tip "SLE applies to the Default cycle time. Named cycle times have no SLE target." - never a red derived from the default SLE (D11).
5. Given Team Phoenix has NO named definitions, when Priya opens Flow Overview, then no selector renders and the widget is exactly as today (D13 - mirrors `CumulativeStateTimeScopeControl`'s `length === 0` early return).
6. Given the instance is NOT premium, when Priya opens Flow Overview, then no selector renders (`namedCycleTimeDefinitions` is `[]`) and the default widget is unaffected.
7. Given "Concept to Cash" is selected and an admin then removes the "Planned" state from the team config (D5), when the definitions reload, then the selection resets to "Default" and the invalid definition is not selectable - never a crash (D13 - mirrors the shipped self-reset `useEffect`).
8. Given Portfolio Atlas is premium with "Idea to Live", when Priya opens its Flow Overview, then the selector behaves identically to Team scope (D14).
9. Given "Concept to Cash" is selected on Flow Overview, when Priya switches to the Flow Metrics tab, then the scatterplot's own selector is unaffected and still reads "Default" (D13 - no cross-tab coupling, no Epic 5251 regression).

### US-02 - See the items behind a named percentile

`job_id: job-delivery-lead-see-time-over-custom-window`

Priya sees Concept-to-Cash P85 = 47 days and needs to bring specifics to the retro: which items
actually dragged it, not just the aggregate. 5509's text: "to also have the option to see the data
'behind them'".

### Elevator Pitch
Before: View Data behind the percentiles widget always lists `item.cycleTime` (the default window), so with a named window selected the table contradicts the percentiles above it - P85 says 47 days, the table lists 12-day values.
After: run `Flow Overview -> Cycle Time Percentiles -> select "Concept to Cash" -> View Data` → sees a dialog listing the `34` items the percentiles were computed from, with the highlight column titled `Concept to Cash (days)` showing `47, 39, 52 …`
Decision enabled: which specific work items to pull into the retro as evidence of the upstream wait.

**Acceptance criteria**

1. Given "Default" is selected, when Priya opens View Data, then the highlight column is titled with the Cycle Time term and shows `item.cycleTime`, over all closed items in range - unchanged from today.
2. Given "Concept to Cash" is selected, when Priya opens View Data, then the highlight column is titled "Concept to Cash" and each row shows that item's named duration from `item.namedCycleTimes` (D15) - no new fetch is issued.
3. Given "Concept to Cash" is selected and 51 items closed in range but only 34 crossed both boundaries, when Priya opens View Data, then exactly the 34 items are listed (D16) and the count matches the population the percentiles were computed over (D9).
4. Given "Concept to Cash" is selected, when Priya reads the dialog, then no SLE line is drawn (the SLE does not apply to a named window - consistent with D11).

### US-03 - Tell whether a named cycle time is improving

`job_id: job-delivery-lead-see-time-over-custom-window`

Priya's team spent last quarter attacking the validation queue. She needs to answer "did it work?" for
the leadership review - a question the default window cannot answer, because the default window never
saw the queue in the first place.

### Elevator Pitch
Before: The Trend footer only ever compares the default started->finished window against the previous period; there is no way to see whether a named window is improving.
After: run `Flow Overview -> Cycle Time Percentiles -> select "Concept to Cash"` → sees the trend footer read `▼ 5 days vs previous period` computed over the Concept-to-Cash window
Decision enabled: whether last quarter's intervention on the validation queue actually moved the number, or whether the improvement effort should be redirected.

**Acceptance criteria**

1. Given "Default" is selected, when Priya reads the trend footer, then it is byte-identical to today (`cycleTimePercentilesInfo` with no `definitionId`).
2. Given "Concept to Cash" is selected, when the widget loads, then the trend footer compares that definition's percentiles for the current period against the same definition's percentiles for the previous period, via `GET /{teams|portfolios}/{id}/metrics/cycleTimePercentilesInfo?startDate=..&endDate=..&definitionId={id}` (D12).
3. Given a named trend and a default trend are both requested for the same date range, when both are served, then they return their own answers - the cache key includes a definition segment and they do not collide (the `CycleTimePercentilesInfo_{start}_{end}` trap above).
4. Given `definitionId` names a definition that does not exist or is invalid, when the endpoint is called, then it behaves exactly as the existing `cycleTimePercentiles` named path does for the same input - no new error contract is invented.
5. Given the Portfolio twin, when a named trend is requested, then it behaves identically to Team scope.

## [REF] Out of scope

- **Per-definition SLE** - D11 rejects it for now; a named window has no SLE target. Revisit only if
  users ask to RAG a named window.
- **`workItemAgePercentiles` widget** - measures in-progress age, not a closed start->end window (D14).
- **`cycleTimePbc`** (Predictability tab) - not named in 5509 (D14).
- **Sharing / persisting the selection** across tabs or reloads - D13; needs a persistence ADR.
- **Any config change** - definitions are defined by Epic 5251's shipped settings surface.
- **Forecasting** - inherited from the Epic 5251 journey; this is analysis.
- **Fixing the pre-existing clients version-gate gap** (see Cross-cutting) - a separate bug.

## [REF] WS strategy

**Strategy B (brownfield extension)** - no walking skeleton. The end-to-end path (definitions ->
premium gate -> named percentile computation -> read endpoint -> selector -> chart) was proven by
Epic 5251's slice-01 and is SHIPPED. This story reuses that spine on a new surface. Slice 01 below is
the thinnest end-to-end vertical, not a skeleton.

## [REF] Driving ports (inbound surfaces)

| Port | Change |
|------|--------|
| UI - `flow-overview` category, `percentiles` widget (Team + Portfolio) | NEW selector; RAG/ViewData/Trend follow it |
| HTTP - `GET /{teams\|portfolios}/{id}/metrics/cycleTimePercentiles?definitionId=` | EXISTS, reused unchanged |
| HTTP - `GET /{teams\|portfolios}/{id}/metrics/cycleTimePercentilesInfo?definitionId=` | EXTENDED with an optional param (D12) |
| CLI / MCP | No change - see Cross-cutting |

## [REF] Pre-requisites

- Epic 5251 `multiple-cycle-times` SHIPPED (definitions, named computation, premium gate, read
  endpoint) - **satisfied**.
- Premium license for the viewing instance; `@premium` E2E seed per
  `reference_premium_license_dev_seed` - **satisfied**.
- A named definition on the demo data for `@screenshot` / E2E - **satisfied, verified 2026-07-17**.
  `DemoDataFactory.CreateDemoCycleTimeDefinitions()` (`DemoDataFactory.cs:74`) already seeds TWO
  definitions onto BOTH `CreateDemoTeam` (`:49`) and `CreateDemoPortfolio` (`:30`):

  | Id | Name | Window |
  |----|------|--------|
  | 1 | `Lead Time (End to End)` | Backlog -> Done |
  | 2 | `Analysis to Done` | Analysing -> Done |

  No demo-data work is in scope. Tests and screenshots use these REAL names - "Concept to Cash" is
  narrative shorthand inherited from the Epic 5251 journey and must NOT leak into a fixture.
  `Lead Time (End to End)` (Backlog->Done) is materially wider than the default started->finished
  window, so it exercises D11's false-red case with real demo data rather than a contrived one.

## [REF] Scope Assessment: PASS

3 stories | 1 bounded context (metrics; settings untouched) | 2 technologies (C# + React) | ~2 days |
2 slices. Zero oversized signals fired. No split needed.

## [REF] Story map + slices

Backbone: *Land on Flow Overview -> switch the window -> read the number -> inspect the items -> tell
whether it's moving.*

| Slice | Ships | Stories | Est | Brief |
|-------|-------|---------|-----|-------|
| 01 | Selector + named percentiles + neutral RAG + View Data follows selection (Team + Portfolio) | US-01, US-02 | ~1 day | `slices/slice-01-flow-overview-cycle-time-selector.md` |
| 02 | Trend follows the selection (backend `definitionId` + cache key + FE wiring) | US-03 | ~1 day | `slices/slice-02-named-cycle-time-trend.md` |

**Why US-02 rides slice 01 rather than its own slice:** shipping the selector without View Data would
put a self-contradicting widget in front of users (P85 47d over a table of 12d values) for the length
of a slice. Given D13 lifts the state anyway, the View Data conditional is ~10 lines - the incoherence
costs more than the code. If slice 01 measures over ~1 day at roadmap time, the View Data conditional
is the pre-identified split seam.

**Prioritisation (learning leverage first):**

- **Slice 01 first** - highest uncertainty is layout: the `percentiles` widget is `size: "small"`
  (3 cols x 2 rows at xl, `Dashboard.tsx`), and a MUI `Select` (`minWidth: 200` in the shipped scope
  control) must fit above a 4-row table without forcing a scroll. That risk gates the whole story and
  is cheapest to disprove first.
- **Slice 02 second** - lower uncertainty (the cache-key trap is known and has a shipped fix to
  mirror), and it depends on slice 01's selector existing to be demoable.

## [REF] Outcome KPIs

### Objective

Within one reporting cycle of release, a delivery lead can answer "where does our time really go, and
is it improving?" without leaving the Flow Overview.

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|-----|-----------|-------------|----------|-------------|------|
| 1 | Delivery leads on premium owners with >=1 named definition | Switch the Flow Overview percentiles selector to a named definition | >=1 switch per owner per reporting cycle, >=50% of such owners, within 60 days | 0 (surface absent) | Frontend interaction telemetry: selector-change to a non-Default value, `flow-overview` scope | Leading (North Star) |
| 2 | Same | Open View Data with a named definition selected | >=25% of the owners in KPI 1, within 60 days | 0 | Frontend interaction telemetry: View Data open while `definitionId != null` | Leading (depth-of-use) |
| 3 | Delivery leads | Stop tab-hopping to the scatterplot to answer the Overview question | Qualitative: named in >=2 customer interviews / community reports within 90 days | tab-hop workaround | Interviews + community feedback | Lagging (impact) |

- **North Star**: KPI 1 - the switch on Overview IS the activation moment.
- **Guardrails**: Default-selection render path must not regress (US-01 AC2); scatterplot behaviour
  unchanged (US-01 AC9); named and default trends must not collide in cache (US-03 AC3);
  SonarCloud `new_violations = 0`; mutation >=80% BE and FE.

> **Telemetry note (unchanged from Epic 5251, still true):** self-hosted instances do not phone home
> (Epic 5015 blocker). KPIs 1-2 are measurable only on the hosted/demo instance and via opt-in; for
> self-hosted, KPI 3 is the only signal. Flagged for DEVOPS. This is a known, accepted gap - NOT a
> reason to weaken the KPI targets.

## [REF] DoR validation (9-item hard gate)

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1 Problem statement clear, domain language | PASS | Each story opens from Priya's pain (Overview says 12d and looks fine; features still land late; the answer lives on another tab). |
| 2 User/persona specific | PASS | delivery-lead-rte (Priya Nair, Team Phoenix); SSOT persona, no invention. |
| 3 3+ domain examples real data | PASS | "Concept to Cash" (Planned->Done) P85 47d vs default 12d; Portfolio Atlas "Idea to Live"; 51 closed / 34 crossing both boundaries. |
| 4 UAT Given/When/Then 3-7 | PASS | US-01 has 9 AC (6 happy/scope + 3 error/regression), US-02 4, US-03 5. US-01 exceeds 7 - accepted: AC 5-9 are premium/empty/invalid/portfolio/no-regression guards, not additional scenarios; they split with the slice seam if needed. |
| 5 AC derived from UAT | PASS | AC trace to the journey steps appended to `multiple-cycle-times.yaml` (journey `read-named-cycle-time-on-flow-overview`). |
| 6 Right-sized (1-3 days, 3-7 scenarios) | PASS | 2 slices, ~1 day each, 4-5 scenarios each. |
| 7 Technical notes: constraints/cross-cutting | PASS | Technical grounding table (12 verified facts + the cache-key trap) + cross-cutting checklist below, all answered explicitly. |
| 8 Dependencies resolved/tracked | PASS | Epic 5251 SHIPPED; premium seed exists; demo-data named definitions verified present (`DemoDataFactory.cs:74`, Team + Portfolio) - Risk (a) closed, nothing outstanding. |
| 9 Outcome KPIs measurable | PASS | 3 KPIs with numeric targets, baselines, measurement method; telemetry gap flagged for DEVOPS. |

**DoR Status: PASSED (9/9).**

## [REF] Cross-cutting impact checklist (CLAUDE.md DoR item 7 hard gate)

*No silent N/A - every item answered.*

- **RBAC**: **No new authz surface.** This is a READ on a surface the user can already see; viewing
  requires only existing view rights on the team/portfolio PLUS premium. The premium gate is already
  applied where the definitions are derived (`BaseMetricsView.tsx:1143-1152` - `[]` when not premium),
  so the selector simply never renders. All UI gating continues to derive from `useRbac()`; no
  component fetches `/api/latest/authorization/my-summary` directly. No `IRbacAdministrationService`
  change.
- **Lighthouse-Clients (CLI + MCP)**: **N/A for this story, because** the clients do not wrap
  `cycleTimePercentilesInfo` at all (verified: zero non-`dist` matches in
  `/storage/repos/lighthouse-clients/packages/`). D12's new optional param is therefore invisible to
  them, and no `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry is needed. **However, a PRE-EXISTING gap was
  found and is NOT fixed here:** `getTeamCycleTimePercentiles(teamId, range, definitionId)`
  (`packages/client/src/index.ts:1959`, plus the Portfolio twin at `:2087`) forwards `definitionId`
  with NO version gate, and no `namedCycleTimes` key exists in the registry (`:1665`). Against an
  older server the unknown query param is silently ignored and **DEFAULT** percentiles are returned -
  a silent wrong answer, not a 404, so the existing gate mechanism would not even fire. This is Epic
  5251 debt. Raise as its own bug; explicitly out of scope for 5509.
- **Website (marketing surface)**: **N/A, because** named cycle times are already marketed as a
  premium feature from Epic 5251; 5509 adds a surface to an existing feature, not a new sellable
  capability. No new premium-features entry. Re-assess at DELIVER if the Overview surface turns out to
  be the better demo.
- **Docs prose + screenshots**: **REQUIRED at finalization, not N/A.** The Flow Overview docs page
  showing the Cycle Time Percentiles widget needs prose for the selector + the neutral-RAG rule (D11),
  and a per-theme `@screenshot` test per `feedback_screenshot_tests_per_theme`. Per
  `feedback_docs_in_feature_finalization`, this happens at feature finalization, not deferred to
  `/release`. Note `project_screenshot_regen_pixel_threshold_trap`: `rm` the old PNG first, or a
  <0.5% diff silently keeps the stale image.
- **EF migration**: **N/A, because** no schema change - D11 rejects per-definition SLE, and everything
  else is read-path only.
- **Demo data**: see Risk (a).

## [REF] Wave-decisions summary

### Key decisions

- [D11] RAG neutral + explained under a named selection: the single per-owner SLE is default-anchored,
  so RAG-ing a named window ships a false red (`ragRules.ts:174`, `WorkTrackingSystemOptionsOwner.cs:33-35`).
- [D12] `cycleTimePercentilesInfo` gains optional `definitionId`; the named service path reuses the
  shipped `GetNamedCycleTimePercentilesForTeam` twice (`TeamMetricsService.cs:340, 687`).
- [D13] Selection lifted to `BaseMetricsView`, consumed only by the percentiles widget - forced by
  ViewData being assembled there (`BaseMetricsView.tsx:466`); mirrors the shipped
  `cumulativeScopeDefinitionId` pattern; scatterplot untouched.
- [D14] Scope = `flow-overview` `percentiles` widget, Team + Portfolio.
- [D15] View Data highlight column follows the selection (`WorkItem.ts:16`, no new fetch).
- [D16] View Data rows filtered to the named population, matching D9.

### Requirements summary

- Primary job: `job-delivery-lead-see-time-over-custom-window` on a NEW surface (Flow Overview). Same
  persona, same motivation - no new job created.
- Walking skeleton: none (Strategy B - Epic 5251 proved the spine).
- Feature type: user-facing.

### Constraints established

- The SLE is default-cycle-time-anchored and single per owner - any named-window RAG is meaningless
  until a per-definition SLE exists.
- ViewData payloads are built at `BaseMetricsView`, so any widget-local selection that must drive
  ViewData has to be lifted. Structural, applies to future widget selectors too.
- The scatterplot's local selector is shipped behaviour; changing it is a regression surface.

### Upstream changes

- **None.** No DISCOVER/DIVERGE artifacts exist for this feature (`docs/feature/flow-overview-named-cycle-time/{discover,diverge}/` absent), and no D1-D10 decision in
  `multiple-cycle-times.yaml` is contradicted. D9 and D5 are consumed as-is (D16 and US-01 AC7).
  D6 said the MVP surfaces were the scatterplot + cumulative chart; 5509 ADDS a surface rather than
  changing that - recorded as an amendment in the journey YAML, not an override.

### Contradiction check vs source evidence

ADO 5509's text - "For sure the widgets RAG, View Data, and Trend should adjust based on the
selection" - initially conflicted with a narrower scope answer that omitted View Data. **Raised with
the user rather than silently resolved; resolved in favour of the story text** (D15 + D16), once the
code showed the lifted-state precedent makes it ~10 lines. 5509's "Idea: ... add a combobox to switch
in the existing widget. Proposal, open to hear other ideas." is honoured as-is: combobox in the
existing widget, matching the shipped scatterplot idiom.

### Anti-patterns checked

- Tech-surface backlog entry: caught - no new job invented for a new surface of the same job.
- Implement-X stories: none - all three open from persona pain.
- Synthetic data: AC use real shapes (PHX items, Concept to Cash, 51/34 split).
- Infrastructure-only slice: none - both slices carry user-visible value.
- Elevator pitch: all 3 stories have Before/After/Decision-enabled with a real user entry point.

### Risks

- **(a) Demo data named definition - RESOLVED 2026-07-17, no work needed.** Verified rather than
  assumed: `DemoDataFactory.CreateDemoCycleTimeDefinitions()` (`DemoDataFactory.cs:74`) already seeds
  `Lead Time (End to End)` (Backlog->Done) and `Analysis to Done` (Analysing->Done) onto BOTH the demo
  Team (`:49`) and the demo Portfolio (`:30`). So `@screenshot` and E2E are demo-drivable per
  `feedback_e2e_use_demo_data`, and the `project_estimation_chart_uses_additional_fields`
  static-image fallback does NOT apply. Two consequences for DESIGN/DISTILL: (1) tests and screenshots
  must use the REAL demo names - "Concept to Cash" is Epic 5251 narrative shorthand and must not leak
  into a fixture; (2) `Lead Time (End to End)` spans Backlog->Done, materially wider than the default
  started->finished window, so it exercises D11's false-red case with real data - use it as the
  E2E's named selection rather than `Analysis to Done`.
- **(b) Cache-key collision on the named trend** (D12) - concrete, has a shipped fix to mirror
  (`..._Def_{definitionId}`). Covered by US-03 AC3.
- **(c) Layout in a `size: "small"` widget** - a `minWidth: 200` Select above a 4-row percentile table
  at 3 cols. Highest-uncertainty item; slice 01 disproves it first.
- **(d) KPI telemetry gap on self-hosted** (Epic 5015) - accepted, flagged for DEVOPS.

### Density / expansion

`resolve_density` -> `mode: lean`, `expansion_prompt: ask-intelligent` (from
`~/.nwave/global-config.json`). Trigger evaluation: AC ambiguity - NO (AC name exact endpoints,
values, counts); cross-context complexity - NO (1 context, 2 technologies); multi-stakeholder - NO (1
persona); compliance/regulatory - NO; WS strategy = D - NO (strategy B). **No trigger fired -> strict
lean, no expansion menu.** One telemetry skip event (`expansion_id: "*"`) records the silent-lean
opportunity.

## [REF] Handoff

**To**: `nw-solution-architect` (DESIGN - full artifact set) + `nw-platform-architect` (DEVOPS - the
Outcome KPIs section only; note the Epic 5015 telemetry gap).

**DESIGN must resolve**: (1) the trend cache-key segment (D12/Risk b); (2) whether the Trend service
change warrants an ADR or rides ADR-062's contract precedent; (3) the `size: "small"` layout approach
(Risk c). Risk (a) (demo data) is CLOSED - verified present, no work.
