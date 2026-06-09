# Feature Delta — work-item-age-percentiles

**ADO:** Story #5257 "Work Item Age Percentiles" (User Story, was New) · **Feature type:** user-facing · **Premium:** No
**Wave status:** DISCUSS complete 2026-06-09 · DESIGN next

---

## Wave: DISCUSS / [REF] Persona ID

**flow-coach** (Flow Coach — runs standups, flow reviews, ops reviews for one team or release train; uses Lighthouse as a flow-health diagnostic, not just a forecast). Secondary: **delivery-forecaster** (forecast-honesty conversations). Same persona as the sibling `aging-pace-percentiles`.

## Wave: DISCUSS / [REF] JTBD one-liner

When I review my team's current in-progress work, I want a percentile readout of how old my WIP is *right now* (not how long finished work took), so I can judge at a glance whether my current work is aging healthily or piling up — and contrast "current WIP age" against "historical cycle time" on the one aging chart.

Job: `job-flow-coach-gauge-wip-age-spread` (opportunity importance 4 / satisfaction 2 / gap 2).

## Wave: DISCUSS / [REF] The conceptual distinction (why this is not a duplicate)

Lighthouse already has **two** percentile surfaces, both derived from *completed* items:
- **Cycle Time percentiles** (`cycleTimePercentiles`) — how long finished work took end-to-end. Shown as the small "Percentiles" card in OverviewCategory **and** as full-width horizontal reference lines on the Work Item Aging chart (an SLE-style benchmark: "this dot is older than 85% of completed items took").
- **Age-in-state pace bands** (`ageInStatePercentiles`, the `aging-pace-percentiles` feature) — per-state historical age-at-exit, drawn as filled background zones.

**Work Item Age percentiles are different**: they summarise the **current in-progress population's own ages** — a snapshot of live WIP, not a history of completions. "85% of my in-progress items are younger than X days." Today the aging chart *plots* those items as dots but gives no percentile readout of their distribution, and the overview has no WIP-age summary. This story fills that gap.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | Feature type | User-facing (chart overlay + overview card). No backend-only/infra escape valve. |
| **D2** | Aging-chart toggle behaviour | A switch **swaps** the chart's horizontal reference lines between Cycle Time percentiles (default, current behaviour preserved) and Work Item Age percentiles — **mutually exclusive**, one set at a time. The independent pace-band overlay chip (`aging-pace-percentiles`) is untouched and orthogonal. |
| **D3** | Premium / RBAC | **Not premium.** No `useRbac`/license gate — matches the existing CT percentiles card and aging overlay. Read rides the existing metrics path already guarded by `[RbacGuard(TeamRead/PortfolioRead)]`. No new authorization surface. |
| **D4** | Percentile population & set | Percentiles over **every current in-progress item's total work-item-age** (the `workItemAge` already carried on in-progress items), using the same **50 / 70 / 85 / 95** set as cycle-time percentiles. Snapshot of live WIP — **not** windowed. Per-state breakdown is explicitly out (already covered by pace bands). |
| **D5** | Scope & placement | **Team + Portfolio.** New small **"Work Item Age Percentiles"** card in `OverviewCategory`, mirroring the `CycleTimePercentiles` card layout (50/70/85/95 rows, descending, ForecastLevel colouring). |
| **D6** | Empty / low-WIP path | Zero in-progress items → card renders a graceful empty state ("—" / "no work in progress"), chart WIA lines simply absent; never crash. A single in-progress item still yields percentiles (D9-equivalent: behaves like the data it has, no special low-sample gate). |
| **D7** | Walking skeleton | **No** (brownfield extension). Slice 01 is the thinnest end-to-end slice (Team: compute → read → overview card), not a greenfield skeleton. |
| **D8** | Compute location | **LOCKED (user, 2026-06-09): backend.** A new backend `workItemAgePercentiles` read (Team + Portfolio) mirroring `cycleTimePercentiles`, reusing `PercentileCalculator` + `PercentileValue`. Card + chart consume server-computed `PercentileValue[]` like every other percentile surface — keep production work out of the frontend. The Lighthouse-Clients packages (CLI + MCP) extend to wrap the new endpoint, **version-gated** (a new endpoint 404s on an old server). Population = current in-progress items' `workItemAge` (snapshot; date-range params, if present for signature parity, do **not** filter the population). FE-derivation explicitly rejected. |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — WIP-age percentile summary on the Team overview
As a flow coach, I want a "Work Item Age Percentiles" card on my team's metrics overview, so I can read the 50/70/85/95 age of my current WIP without eyeballing every dot.
`job_id: job-flow-coach-gauge-wip-age-spread`

#### Elevator Pitch
Before: I can see in-progress dots on the aging chart but have no at-a-glance summary of how old my current WIP is.
After: open the Team **Metrics → overview** → see a **Work Item Age Percentiles** card reading e.g. `85th: 18 days · 70th: 11 · 50th: 6`.
Decision enabled: I decide whether my current WIP is aging healthily or piling up, and which percentile band to interrogate in standup.

**Acceptance criteria**
- AC1: Given a team with in-progress items aged d₁…dₙ, when I open the metrics overview, then a "Work Item Age Percentiles" card shows the 50/70/85/95 percentiles of those ages, in descending order with ForecastLevel colouring (mirrors the CT percentiles card).
- AC2: Percentiles are computed over the **current** in-progress population's `workItemAge`, independent of the selected date range (WIP snapshot, per D4).
- AC3: Given a team with zero in-progress items, the card renders a graceful empty state, not an error or blank crash (D6).

### US-02 — Toggle the aging chart between Cycle Time and Work Item Age percentiles
As a flow coach, I want a switch on the Work Item Aging chart that flips its reference lines between Cycle Time percentiles and Work Item Age percentiles, so I can compare my live WIP against historical completion times on the same chart.
`job_id: job-flow-coach-gauge-wip-age-spread`

#### Elevator Pitch
Before: the aging chart only shows full-width Cycle Time percentile lines (historical completions) as the benchmark.
After: on the **Work Item Aging chart**, flip the **CT ↔ WIA** switch → the reference lines redraw at the 50/70/85/95 of current WIP age instead.
Decision enabled: I decide whether my current WIP's own spread is tighter or wider than what finished work historically took — naming pile-up risk precisely.

**Acceptance criteria**
- AC1: Given the aging chart with the toggle in its default (CT) position, when I flip it to WIA, then the horizontal reference lines redraw at the 50/70/85/95 of the current in-progress items' ages, and the CT lines are removed (mutually exclusive, D2).
- AC2: Flipping back restores the Cycle Time lines; the toggle round-trips without a page reload.
- AC3: The pace-band overlay chip (if present) is unaffected by the toggle — the two controls are orthogonal (D2).
- AC4: Given zero in-progress items, flipping to WIA shows no reference lines (and no crash); the dots area renders normally (D6).

### US-03 — Same summary + toggle at Portfolio scope
As a flow coach for a release train, I want the WIA percentile card and the chart toggle on the Portfolio metrics view too, so I get the same WIP-age signal across the train.
`job_id: job-flow-coach-gauge-wip-age-spread`

#### Elevator Pitch
Before: WIA percentiles exist only at Team scope.
After: open the **Portfolio Metrics → overview** → the **Work Item Age Percentiles** card and the aging-chart **CT ↔ WIA** toggle behave exactly as at Team scope.
Decision enabled: I judge WIP-age health for the whole train, not one team at a time.

**Acceptance criteria**
- AC1: The Portfolio metrics overview shows the WIA percentile card computed over the portfolio's current in-progress features/items.
- AC2: The Portfolio aging chart offers the same CT↔WIA toggle with identical semantics (D2).
- AC3: Empty-WIP behaviour matches Team (D6).

## Wave: DISCUSS / [REF] Acceptance criteria (cross-story invariants)

- The WIA percentile set is exactly 50/70/85/95 wherever it appears (card + chart), matching the CT percentile set (D4).
- WIA percentiles never use a date-range filter; CT percentiles continue to honour the selected range. The two populations are visibly distinct in the UI label so users don't conflate them.
- No premium/RBAC gate is added anywhere in this feature (D3).

## Wave: DISCUSS / [REF] Out-of-scope

- Per-state WIA percentiles (already served by `aging-pace-percentiles`).
- Any change to how `workItemAge` itself is computed.
- Premium gating, new permissions, telemetry/phone-home (Lighthouse self-hosted instances do not report centrally — Epic 5015 blocker; KPIs are validated by dogfood/demo, not central analytics).
- A configurable percentile set (50/70/85/95 is fixed, as elsewhere).

## Wave: DISCUSS / [REF] Definition of Done (9-item)

1. US-01/02/03 ACs all green (backend integration + FE component tests). 2. WIA card renders Team + Portfolio. 3. Aging-chart CT↔WIA toggle round-trips. 4. Empty-WIP path graceful on card + chart. 5. `pnpm build`/`pnpm test` + `dotnet build`/`dotnet test` green, zero warnings. 6. SonarCloud new_violations = 0. 7. Mutation ≥80% on the new compute + toggle logic. 8. `docs/metrics/` updated + per-theme `@screenshot` test run live (card + toggle). 9. ADO #5257 transitioned; release-notes tag honoured (story already tagged "Documentation; Release Notes").

## Wave: DISCUSS / [REF] WS strategy

**Strategy C** (extend existing surface — no walking skeleton). Brownfield: the feature extends the existing percentile/aging machinery and the OverviewCategory card grid. Slice 01 is the thinnest end-to-end Team slice.

## Wave: DISCUSS / [REF] Driving ports

- HTTP read: `GET /api/teams/{teamId}/metrics/workItemAgePercentiles` + portfolio sibling (IF the DESIGN backend-compute fork is chosen, D8); else no new port (FE derives from already-loaded in-progress items).
- UI: OverviewCategory "Work Item Age Percentiles" card; `WorkItemAgingChart` CT↔WIA toggle control.

## Wave: DISCUSS / [REF] Pre-requisites

- Existing `cycleTimePercentiles` machinery (`PercentileCalculator`, `PercentileValue`, `CycleTimePercentiles.tsx`) — the WIA card and compute mirror it.
- Existing `WorkItemAgingChart.tsx` with its `percentileValues` reference lines and `useMetricsData` plumbing — the toggle extends it.
- In-progress items already carry `workItemAge` (FE `IWorkItem`, backend metrics path).

## Wave: DISCUSS / [REF] Cross-cutting impact (DoR Item 7 hard gate)

- **RBAC** — **N/A (no new authorization), because** D3 makes this non-premium and the read rides the existing `TeamMetricsController` / `PortfolioMetricsController` paths already guarded by `[RbacGuard(TeamRead/PortfolioRead)]`. No `useRbac()` UI gating change; no `IRbacAdministrationService` interaction.
- **Lighthouse-Clients (CLI + MCP)** — **AFFECTED (version-gated).** Resolved by the D8 backend override (2026-06-09): the new `workItemAgePercentiles` endpoint × 2 scopes means the wrapping CLI + MCP client method MUST be version-gated — pre-check the server version and fail with a clear "upgrade Lighthouse" error (an old server 404s opaquely), pinned strictly-newer-than the last released Lighthouse version, recorded in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (dev/unparseable versions never blocked). Separate repo; tracked as a DELIVER cross-cutting deliverable (see DELIVER action items). Precedent: `cycleTimePercentiles`/`ageInStatePercentiles` + ADR-055/062.
- **Website** — **Marketing N/A, because** this enhances an existing *free* metric surface rather than introducing a new premium feature. **Docs are NOT N/A**: `docs/metrics/` MUST gain the WIA card + chart-toggle description with a per-theme `@screenshot` at finalization (per the DELIVER docs discipline).

## Wave: DISCUSS / [REF] Story map & slices

Backbone: **See current WIP age** → **Benchmark it on the chart** → **Do it across the train**.

| Slice | Stories | Ships end-to-end | Learning hypothesis |
|-------|---------|------------------|---------------------|
| **01** Team WIP-age summary (thin e2e) | US-01 | Team overview WIA percentile card, real data | Disproves "a WIP-age percentile readout is a meaningful at-a-glance signal" if coaches ignore the card / find it redundant with the dots. |
| **02** Aging-chart CT↔WIA toggle (Team) | US-02 | Toggle flips reference lines on the Team aging chart | Disproves "coaches want to contrast live-WIP percentiles against the same dots" if the toggle is never flipped or confuses CT-vs-WIA. |
| **03** Portfolio scope | US-03 | Card + toggle on Portfolio metrics | Disproves "the WIP-age signal carries to release-train scope" if portfolio aggregation makes it noise. |

Briefs: `docs/feature/work-item-age-percentiles/slices/slice-01..03-*.md`.

## Wave: DISCUSS / [REF] Outcome KPIs

- **KPI-1 (functional completeness):** WIA card renders correct 50/70/85/95 for any team/portfolio with ≥1 in-progress item. Target 100%. Measure: integration + component tests.
- **KPI-2 (interaction cost):** CT↔WIA toggle re-renders client-side in <200 ms with no network round-trip when data is already loaded. Measure: component test / manual profile.
- **KPI-3 (job validation, qualitative):** A flow coach can answer "is my current WIP aging healthily?" from one screen without source-system spelunking. Measure: dogfood/demo walkthrough (central telemetry unavailable — self-hosted instances don't phone home).

## Wave: DISCUSS / [REF] DoR validation (9/9)

1. Business value — ✓ JTBD job + opportunity 4/2/gap 2. 2. User/persona — ✓ flow-coach. 3. AC testable — ✓ per-story, port-observable. 4. Dependencies — ✓ all existing (percentile machinery, aging chart). 5. Sized — ✓ 3 thin slices ≤1 day. 6. Job traceability — ✓ every story → `job-flow-coach-gauge-wip-age-spread`. 7. Technical notes / cross-cutting — ✓ RBAC/Clients/Website all answered above. 8. No blocking unknowns — ✓ only the D8 compute-location fork, which DESIGN owns and which doesn't block AC definition. 9. Demoable — ✓ each slice demoable on demo data.

## Changed Assumptions

- **DISCUSS Cross-cutting "Lighthouse-Clients — Conditional, resolved at DESIGN" → RESOLVED to AFFECTED (version-gated).** *Supersedes the prior DESIGN pass's conclusion, quoted verbatim:* ~~"RESOLVED to UNAFFECTED. The DESIGN D8 verdict (ADR-065) computes the WIA percentiles client-side from already-loaded `inProgressItems` — no new endpoint exists, so the opaque-404-on-old-server problem never arises and there is nothing to wrap or version-gate. No `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry."~~ The user overrode D8 to **backend compute** (2026-06-09), which adds a NEW endpoint × 2 scopes (`workItemAgePercentiles`, Team + Portfolio). A new endpoint 404s opaquely on an old server, so the CLI + MCP clients MUST add a **version-gated** `getWorkItemAgePercentiles` wrapper that pre-checks the server version and fails with a clear "upgrade Lighthouse" error, pinned **strictly newer than the last released Lighthouse version**, recorded in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (dev/unparseable versions never blocked). The clients live in a **separate repo** — this work is tracked there, not in this repo's slices, but is a cross-cutting deliverable DELIVER/finalize must honour. No story or AC changes (both compute paths satisfy the ACs; only the compute location moved), so no `design/upstream-changes.md` is needed.
- No other DISCUSS assumption contradicted. No DISCOVER/DIVERGE artifacts existed; DISCUSS bootstrapped the feature workspace.

---

## Wave: DESIGN / [REF] D8 verdict (the one real decision)

**Compute the WIA percentiles SERVER-SIDE on a new read endpoint per scope** (ADR-065 — user-confirmed 2026-06-09, overriding the prior pass's client-side recommendation). User directive: *"WIA percentiles should be calculated in the BACKEND, with an extension to the API (and thus also the client packages). We want to do as little production work in the frontend."* `GET …/metrics/workItemAgePercentiles` (Team + Portfolio) mirrors `cycleTimePercentiles`: existing class-level `[RbacGuard(TeamRead/PortfolioRead)]`, returns `IEnumerable<PercentileValue>` (flat 50/70/85/95, **reuse** `PercentileValue` — NOT per-state). The service method composes existing primitives — the current in-progress selection (`GetWipSnapshotForTeam` / `GetInProgressFeaturesForPortfolio`) → each item's `WorkItemBase.WorkItemAge` → `BaseMetricsService.BuildPercentiles` → `PercentileCalculator`. This restores percentile uniformity (one server-side algorithm) and keeps production logic out of the FE. Client-side derivation REJECTED (documented in ADR-065). Consequence: **Clients AFFECTED** — a new endpoint needs version-gated CLI + MCP wrappers.

## Wave: DESIGN / [REF] DDD context

Single existing bounded context (Flow Metrics). No new aggregate, entity, value object, or domain service. The WIA percentile is a **server-side read-model projection** over the existing in-progress selection, computed by the existing percentile primitive. No ubiquitous-language addition beyond the UI label "Work Item Age Percentiles" (distinct from "Cycle Time Percentiles" — the two populations must never be conflated, a cross-story invariant). No bounded-context boundary moves.

## Wave: DESIGN / [REF] Component decomposition

| # | Component | Layer | Action | Notes |
|---|-----------|-------|--------|-------|
| 1 | `TeamMetricsController.GetWorkItemAgePercentilesForTeam` + Portfolio sibling | BE controller | CREATE NEW | `GET …/metrics/workItemAgePercentiles?startDate&endDate`; existing `[RbacGuard]`; `startDate>endDate ⇒ 400`; returns `IEnumerable<PercentileValue>`; mirrors `cycleTimePercentiles` action |
| 2 | `TeamMetricsService.GetWorkItemAgePercentilesForTeam` + `PortfolioMetricsService.GetWorkItemAgePercentilesForPortfolio` | BE service | CREATE NEW | `<in-progress selection>(entity, endDate).Select(i => i.WorkItemAge).Where(a => a > 0).ToList()` → `BuildPercentiles(...)`; cached `WorkItemAgePercentiles_{endDate:yyyy-MM-dd}` |
| 3 | `ITeamMetricsService` / `IPortfolioMetricsService` | BE port | EXTEND | New method signatures |
| 4 | `WorkItemAgePercentiles.tsx` | FE card | CREATE NEW | Clone of `CycleTimePercentiles.tsx`: 50/70/85/95 descending, `ForecastLevel` colour, graceful empty state, distinct WIA title |
| 5 | `MetricsService.getWorkItemAgePercentiles` + `IMetricsService` | FE service | CREATE NEW | Fetches the new endpoint → `IPercentileValue[]`; Zod: reuse the existing percentile shape (`PercentileValue[]`) |
| 6 | `MetricsData` ctx field `workItemAgePercentilesValues` + `useMetricsData` parallel-fetch | FE hook | EXTEND | Parallel-fetch alongside `percentileValues` |
| 7 | `categoryMetadata.ts` entry `workItemAgePercentiles` | FE registry | CREATE NEW | `flow-overview`, size `small`, both scopes |
| 8 | `WorkItemAgingChart.tsx` | FE chart | EXTEND | Optional `workItemAgePercentileValues` prop + local `percentileSource` state + CT↔WIA toggle control; `activePercentiles` feeds the existing single `ChartsReferenceLine` block |
| 9 | `BaseMetricsView.tsx` | FE view | EXTEND | Render new card via the widget key; pass `workItemAgePercentilesValues` to the `aging` widget |
| 10 | Lighthouse-Clients `getWorkItemAgePercentiles` (CLI + MCP) | clients (separate repo) | CREATE NEW | Version-gated wrapper; `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry |
| 11 | `PercentileValue` / `IPercentileValue` | BE+FE model | REUSE AS-IS | Response + card/chart shape — no new DTO |
| 12 | `BaseMetricsService.BuildPercentiles` / `PercentileCalculator` | BE | REUSE AS-IS | The 50/70/85/95 algorithm |
| 13 | `GetWipSnapshotForTeam` / `GetInProgressFeaturesForPortfolio` | BE | REUSE AS-IS | The current in-progress selection (feeds the aging-chart dots) |
| 14 | `BaseMetricsService.GetFromCacheIfExists` | BE | REUSE AS-IS | New `WorkItemAgePercentiles_…` key namespace |
| 15 | `WorkItemBase.WorkItemAge` | BE | REUSE AS-IS | The age value, already computed |
| 16 | `ForecastLevel` | FE | REUSE AS-IS | Palette + icons for the card rows + chart lines |
| 17 | `ChartsReferenceLine` / `<ChartsContainer>` / `useChartVisibility` | FE (MUI-X) | REUSE AS-IS | Existing single reference-line block renders `activePercentiles`; single-`percentiles` contract unchanged |
| 18 | `CycleTimePercentiles.tsx` | FE | TEMPLATE | Cloned → new card; original unmodified |

## Wave: DESIGN / [REF] Driving ports

**2 NEW HTTP endpoints.** `GET /api/teams/{teamId:int}/metrics/workItemAgePercentiles?startDate&endDate` `[RbacGuard(TeamRead)]` and `GET /api/portfolios/{portfolioId:int}/metrics/workItemAgePercentiles?startDate&endDate` `[RbacGuard(PortfolioRead)]`, each returning `IEnumerable<PercentileValue>`. Mirror the existing `cycleTimePercentiles` action (same guard, same 400-guard, same date-keyed cache idiom). **Snapshot semantics:** `startDate`/`endDate` exist for signature parity + the 400-guard + cache key, but `startDate` does NOT filter the population — only `endDate` is passed to the in-progress selection as `asOfDate` and only `endDate` participates in the cache key. FE driving surface: the OverviewCategory WIA card + the `WorkItemAgingChart` CT↔WIA toggle.

## Wave: DESIGN / [REF] Driven ports

**NONE NEW.** The service methods read through the existing `workItemRepository` / `featureRepository` via the existing in-progress selections and cache through the existing `GetFromCacheIfExists`. No new repository, no persistence, no external integration, no new driven adapter. **No probe contract / no contract tests owed** at the platform-architect handoff — the new endpoints read existing repositories already under integration coverage; there is no new foreign-substrate boundary.

## Wave: DESIGN / [REF] Technology choices

NO new technology, library, or third-party service. Stack unchanged: ASP.NET Core 8 + EF (BE, no migration); NUnit + Moq + EF InMemory + WebApplicationFactory (BE test); React 18 + TS (strict) + MUI-X-charts (FE); Vitest + RTL (FE test); Stryker (mutation ≥80%); Biome (lint); Playwright (E2E screenshot at finalization). All MIT/Apache-2.0, all already in the project.

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | ADR | Status |
|----|----------|-----|--------|
| D8 | WIA percentiles computed **server-side** on `GET …/metrics/workItemAgePercentiles` (Team + Portfolio); reuse in-progress selection + `BuildPercentiles`/`PercentileCalculator` + `PercentileValue` | ADR-065 | ACCEPTED (user-confirmed) |
| — | Aging chart swaps the line *source* between two **server-fetched** arrays (one `activePercentiles` feeds the existing single reference-line block), not a second line set | ADR-066 | ACCEPTED |
| — | Clients: **AFFECTED** — version-gated `getWorkItemAgePercentiles` wrapper + `FEATURE_REQUIRES_SERVER_NEWER_THAN` (consequence of the new endpoint) | ADR-065 | RESOLVED |
| — | RBAC: N/A, rides existing guards; no `ILicenseService` on the read path (D3) | — | RESOLVED |

## Wave: DESIGN / [REF] Reuse Analysis

| Overlapping surface | Classification | Justification |
|---------------------|----------------|---------------|
| `PercentileCalculator` / `BaseMetricsService.BuildPercentiles` (BE) | REUSE AS-IS | The 50/70/85/95 algorithm — computed server-side verbatim; no re-implementation |
| `PercentileValue` / `IPercentileValue` | REUSE AS-IS | Endpoint response + card/chart shape — **no new DTO** (flat list, not per-state) |
| `GetWipSnapshotForTeam` (`TeamMetricsService:563`) | REUSE AS-IS | Team current in-progress selection (same set as `/metrics/wip`, feeds the aging-chart dots) |
| `GetInProgressFeaturesForPortfolio` (`PortfolioMetricsService:224`) | REUSE AS-IS | Portfolio current in-progress selection |
| `BaseMetricsService.GetFromCacheIfExists` | REUSE AS-IS | New `WorkItemAgePercentiles_{endDate}` key namespace; `endDate`-only |
| `WorkItemBase.WorkItemAge` (`:73`) | REUSE AS-IS | The age value, already computed per item |
| `cycleTimePercentiles` controller action | TEMPLATE | New action mirrors its guard/400/cache idiom |
| `CycleTimePercentiles.tsx` | TEMPLATE (clone → new card) | New `WorkItemAgePercentiles.tsx` mirrors its layout; original unmodified |
| `ForecastLevel` palette | REUSE AS-IS | Row/line colouring |
| `WorkItemAgingChart.tsx` | EXTEND | Optional WIA prop + local toggle + `activePercentiles` source swap |
| `useMetricsData` / `MetricsData` / `BaseMetricsView` plumbing | EXTEND | Parallel-fetch WIA into new ctx field; render card + pass prop |
| OverviewCategory card grid + dispatch / `categoryMetadata.ts` | EXTEND (one registry entry) | New `workItemAgePercentiles` widget key |
| `useChartVisibility` | REUSE AS-IS | Single-`percentiles` contract unchanged |
| **2 endpoints + 2 service methods** (BE) | CREATE NEW | Genuinely new contract — thin compositions of the reused primitives above |
| `WorkItemAgePercentiles.tsx` card + `MetricsService.getWorkItemAgePercentiles` + ctx field | CREATE NEW | Card cloned from template; one service method + one ctx field |
| Lighthouse-Clients `getWorkItemAgePercentiles` wrapper (CLI + MCP) | CREATE NEW (separate repo) | Version-gated; `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry |

The only CREATE-NEW backend artifacts are 2 thin service methods + 2 controller actions; every primitive they compose already exists and is reused verbatim (no new DTO, no new algorithm, no new selection, no persistence, no EF migration).

## Wave: DESIGN / [REF] Open questions (deferred)

- **DISTILL**: per-story Gherkin for the date-range-invariance of the endpoint (D4 — same `endDate`, differing `startDate` ⇒ identical percentiles), the CT↔WIA toggle round-trip + mutual exclusivity (D2/US-02), and the empty/single-item WIP path (D6). Confirm the consolidated 4-reviewer gate.
- **DELIVER**: exact placement/affordance of the CT↔WIA toggle control on the chart (switch vs chip-pair) — a UX detail, AC-neutral, decided live against the rendered chart per the run-Playwright-before-commit discipline. Per-theme `@screenshot` (card + toggle) + `docs/metrics/` prose at finalization.
- **DELIVER (Lighthouse-Clients — separate repo)**: add the version-gated `getWorkItemAgePercentiles` wrapper to CLI + MCP; record/bump the `FEATURE_REQUIRES_SERVER_NEWER_THAN` baseline to the current latest release; ensure dev/unparseable versions are never blocked. Tracked in the clients repo, not this repo's slices.
- **DELIVER (mutation)**: ensure the new service methods (`GetWorkItemAgePercentilesForTeam`/`…ForPortfolio`) are mutation-hardened — the `> 0` age filter, the `endDate`-only cache key (not `startDate`), and the empty-population path. Mirror the `GetCycleTimePercentilesForTeam` test cases for the `BuildPercentiles` boundary mutants. ≥80% Stryker BE gate.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DESIGN#row1 | 2 NEW HTTP endpoints `GET …/metrics/workItemAgePercentiles` (Team + Portfolio) returning flat `IEnumerable<PercentileValue>` | ADR-065 | DISTILL authors black-box WebApplicationFactory acceptance tests at the HTTP port; both endpoints RED-scaffolded |
| DESIGN#row2 | `startDate` does NOT filter the population; only `endDate`/asOf matters (snapshot semantics) | ADR-065 | Date-range-invariance is the key new invariant under test (same `endDate`, differing `startDate` ⇒ identical percentiles) |
| DISCUSS#D3 | Non-premium; no `ILicenseService` gate on the read path | n/a | A non-premium caller still receives percentiles — the inverse of a premium-gated endpoint; asserted explicitly |
| DISCUSS#D6 | Empty / single-item WIP graceful, never crash | n/a | Empty WIP ⇒ four-entry all-zero set (confirmed `BuildPercentiles([])` contract); single-item ⇒ that one value at every percentile |
| DESIGN#row3 | NO new driven adapter — reads existing repos via existing selections + cache | ADR-065 | No new `@real-io` adapter scenario owed; existing repos already under integration coverage |

## Wave: DISTILL / [REF] Scenario list with tags

Backend acceptance, black-box at HTTP port via `WebApplicationFactory<Program>` (Tier A only — see WS strategy). Skip marker: NUnit `[Ignore]` at class level. All 13 scaffolds RED (clean-assertion, verified by un-skip-and-run).

**Team — `WorkItemAgePercentilesReadApiIntegrationTest` (US-01, 9 scenarios)**

| # | Scenario (test method) | Tags |
|---|------------------------|------|
| 1 | `…TeamWithInProgressItemsOfKnownAges_ReturnsExactPercentilesOfThoseAges` | `@US-01 @real-io @driving_port @golden` |
| 2 | `…PopulationIsTheWipSet_ClosedItemsExcluded` | `@US-01 @real-io @golden` |
| 3 | `…ResponseShapeIsByteCompatibleWithCycleTimePercentiles` | `@US-01 @real-io @contract` |
| 4 | `…SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles` | `@US-01 @real-io @invariant` (D4) |
| 5 | `…TeamWithNoInProgressItems_ReturnsGracefulZeroValuedSet` | `@US-01 @real-io @error @edge` (D6) |
| 6 | `…TeamWithSingleInProgressItem_ComputesOverThatOneValue` | `@US-01 @real-io @edge` (D6) |
| 7 | `…NonPremiumCaller_StillReceivesPercentiles` | `@US-01 @real-io @rbac` (D3 inverse) |
| 8 | `…AnonymousCaller_IsRejected` | `@US-01 @real-io @error @rbac` |
| 9 | `…StartDateAfterEndDate_ReturnsBadRequest` | `@US-01 @real-io @error` |

**Portfolio — `WorkItemAgePercentilesPortfolioReadApiIntegrationTest` (US-03, 4 scenarios)**

| # | Scenario (test method) | Tags |
|---|------------------------|------|
| 1 | `…PortfolioWithInProgressFeaturesOfKnownAges_ReturnsExactPercentilesOfThoseAges` | `@US-03 @real-io @driving_port @golden` |
| 2 | `…SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles` | `@US-03 @real-io @invariant` (D4) |
| 3 | `…PortfolioWithNoInProgressFeatures_ReturnsGracefulZeroValuedSet` | `@US-03 @real-io @error @edge` (D6) |
| 4 | `…AnonymousCaller_IsRejected` | `@US-03 @real-io @error @rbac` |

Error/edge ratio: 6 of 13 = 46% (≥40% target met).

## Wave: DISTILL / [REF] WS strategy

Per the Architecture of Reference + Project Infrastructure Policy (`docs/architecture/atdd-infrastructure-policy.md`): **driving port = real `WebApplicationFactory<Program>`** (`TestWebApplicationFactory` + `WithTestAuthentication`, real EF `LighthouseAppContext` via `EnsureDeleted`/`EnsureCreated`). No walking skeleton (D7 — brownfield extension); Slice 01 is the thinnest end-to-end Team slice. No new driven adapter ⇒ no fake added beyond the policy's existing `ILicenseService` mock (not even exercised on this non-premium read path). Tier B (state-machine PBT) NOT applied: this is a config/read-shaped feature (single read, no ≥3-scenario chained journey with rich input space) and the project is C#/NUnit, not the Python/Hypothesis pilot.

## Wave: DISTILL / [REF] Adapter coverage table

| Driven adapter | New? | @real-io scenario | Covered by |
|----------------|------|-------------------|------------|
| EF `LighthouseAppContext` + `IWorkItemRepository` / `IRepository<Feature>` | NO (existing) | YES | Every test seeds real EF rows and reads through the endpoint; existing repos already under integration coverage |
| `ILicenseService` (fake) | NO (existing) | n/a | Non-premium read path — no license gate (D3); `NonPremiumCaller` test proves a standard caller is served |

**No new driven adapter** ⇒ no new `@real-io @adapter-integration` scenario is owed (DESIGN driven-ports = NONE NEW; new endpoints read existing repositories already under integration coverage).

## Wave: DISTILL / [REF] Scaffolds (RED-ready)

| File | Tests | RED mechanism |
|------|-------|---------------|
| `Lighthouse.Backend.Tests/API/Integration/WorkItemAgePercentilesReadApiIntegrationTest.cs` | 9 (US-01) | Class-level `[Ignore]`; real assertions fail because the route is unmatched (SPA fallback ⇒ HTML body) — a JSON-array guard converts that to a clean `MISSING_FUNCTIONALITY` assertion, never a raw parse exception |
| `Lighthouse.Backend.Tests/API/Integration/WorkItemAgePercentilesPortfolioReadApiIntegrationTest.cs` | 4 (US-03) | Same |

No `__SCAFFOLD__` production stubs (C#/.NET project — the Python-pilot scaffold convention does not apply per the Infrastructure Policy). The endpoints/service methods are authored from scratch in DELIVER; the tests are RED against their absence (the missing route yields the SPA fallback, asserted as a non-JSON body).

## Wave: DISTILL / [REF] Test placement + precedent

Both files live in `Lighthouse.Backend.Tests/API/Integration/`, the canonical home of real-EF + WebApplicationFactory percentile read-endpoint acceptance tests. Direct precedent cloned: `AgeInStatePercentilesReadApiIntegrationTest.cs` + `AgeInStatePercentilesPortfolioReadApiIntegrationTest.cs` (sibling percentile read endpoints, Team + Portfolio) and the `NamedCycleTimeReadApiIntegrationTest` / `CumulativeStateTimeReadApiIntegrationTest` family. Same `[SetUp]`/`[TearDown]`, `testDateOffset` interlock, `client.AsTeamAdmin`/`AsPortfolioAdmin`/`AsTeamViewer`/`AsAnonymous` auth helpers, and `[NonParallelizable]` (process-wide static `BaseMetricsService.MetricsCache` + SQLite file-DB race, per ci-learnings).

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving port (DESIGN) | Protocol scenario |
|-----------------------|-------------------|
| `GET /api/teams/{teamId:int}/metrics/workItemAgePercentiles` `[RbacGuard(TeamRead)]` | 9 real-HTTP tests (golden, shape, invariance, empty, single, non-premium, anon, 400) |
| `GET /api/portfolios/{portfolioId:int}/metrics/workItemAgePercentiles` `[RbacGuard(PortfolioRead)]` | 4 real-HTTP tests (golden, invariance, empty, anon) |
| FE WIA card + `WorkItemAgingChart` CT↔WIA toggle (US-02) | DEFERRED to DELIVER (see deferrals) |

## Wave: DISTILL / [REF] Pre-requisites

- `WebApplicationFactory<Program>` test host (`TestWebApplicationFactory` + `WithTestAuthentication`), `AuthenticatedHttpClientExtensions`, real EF `LighthouseAppContext` — all existing.
- Reused production contracts the tests pin: `PercentileValue` (flat `{percentile,value}`), `BaseMetricsService.BuildPercentiles` (always 4 entries 50/70/85/95; `[]`⇒all-zero), `WorkItemBase.WorkItemAge` (computed against `DateTime.UtcNow`, so golden ages are anchored to today; `endDate` only selects the snapshot), `GetWipSnapshotForTeam`/`GetInProgressFeaturesForPortfolio`.
- DEVOPS: no feature-specific environment matrix; default C# integration env (SQLite + Postgres lockstep in CI per policy).

## Wave: DISTILL / [REF] Deferred to DELIVER

- **US-02 chart toggle (FE) + the live E2E walking-skeleton** — DEFERRED to **DELIVER Slice 02** per the run-Playwright-before-commit discipline (a Playwright spec/POM is never authored unrun). No live spec authored now.
- **FE Vitest component RED scaffold for the CT↔WIA swap** — NOT scaffolded. The repo's FE toggle tests are authored alongside the component in DELIVER (Vitest + RTL), and the existing FE test idiom co-locates them with the new `WorkItemAgePercentiles.tsx` / extended `WorkItemAgingChart.tsx`; authoring a standalone RED component test now (against components that don't exist) would be BROKEN, not RED, under Vitest module resolution. Deferred fully to DELIVER Slice 01/02 with an explicit note here.
- **Lighthouse-Clients** version-gated `getWorkItemAgePercentiles` wrapper (separate repo) — per DESIGN.
- **Mutation hardening** of the new service methods (`> 0` age filter, `endDate`-only cache key, empty-population path) — DELIVER.

### Review-gate action items (4-reviewer consolidated, 2026-06-09 — all DELIVER-scope, 0 blockers)

- **[Forge HIGH] Clients release-gate, not just deferral.** The separate-repo version-gated wrapper must be confirmed merged + `FEATURE_REQUIRES_SERVER_NEWER_THAN` pinned to the last released version BEFORE this feature releases — otherwise old CLI/MCP callers get an opaque 404 instead of the "upgrade Lighthouse" error. Add an explicit confirmation step to the feature's finalization/release checklist (don't mark released until clients work is verified). Ensure dev/unparseable versions are never blocked.
- **[Forge MEDIUM] Cache-isolation test.** Add a cheap defensive test that `WorkItemAgePercentiles_{endDate}` and `CycleTimePercentiles_{startDate}_{endDate}` produce distinct cache entries for the same entity+endDate and never cross-contaminate (guards against a metric-identifier typo silently serving the wrong percentile lines).
- **[Forge LOW] KPI-3 finalization criterion.** Define a concrete dogfood/demo pass for KPI-3 (flow coach answers "is my WIP aging healthily?" from one screen). If the card is ignored in the walkthrough, record as an exploratory finding (not a release blocker), since central telemetry is unavailable (self-hosted; Epic 5015).
- **[Eclipse MEDIUM] (DONE)** Stale DISCUSS Clients "Conditional" line refreshed to "AFFECTED (version-gated)" — fixed in this pass.
