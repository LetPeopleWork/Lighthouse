# Evolution: wait-states-flow-efficiency

- **Date finalized**: 2026-06-05
- **ADO**: Story #5173 ("Allow to specify Wait States", reported by Chris). No ADO changes made at finalize per user hold.
- **Status**: Delivered on `main` (DISCUSS → DESIGN → DISTILL → DELIVER all done; all gates green; mutation ≥ 80% on core logic). Push held by user.
- **Workspace (history)**: `docs/feature/wait-states-flow-efficiency/`
- **Builds on**: [state-time-cumulative-view](./../feature/state-time-cumulative-view/feature-delta.md) (Epic 4144 — the Cumulative Time per State chart, its per-state day totals, the US-05 item picker, and the completed/ongoing segment split this feature extends) plus the Flow Overview KPI-tile family.

## What shipped

Lighthouse now tells **idle time apart from active time**. Previously every Doing-state minute
counted as work, so a team whose lead time was two-thirds queueing still looked 100% productive on
paper. The feature lets a config-admin mark which Doing-states are idle "wait" states, then surfaces
**flow efficiency = active-Doing-time / total-Doing-time** on three surfaces, additively, over data
the client already round-trips.

- **US-01 (config + computation foundation)** — a new **mapping-aware** `WaitStates`
  (`List<string>`, default empty) field on `WorkTrackingSystemOptionsOwner` (next to `BlockedStates`
  / `StateMappings`), persisted via the existing settings endpoint plus one EF migration across both
  providers. Entries are raw Doing-states OR a `StateMapping.Name`, resolved through the EXISTING
  `GetRawStatesForCategory(WaitStates)` expansion — marking a whole mapping as "wait" in one click
  counts all its underlying raw states as wait time, no enumeration. The backend computes efficiency
  as a pure `protected` fold `BaseMetricsService.ComputeFlowEfficiency` over the per-state day totals
  the cumulative computation already produces (no new per-state aggregation pass — ADR-024 upheld a
  fifth time). The config UI is a NEW sibling `WaitStatesEditor` immediately after
  `StateMappingsEditor` in both the Team and Project settings forms; the existing mappings editor was
  NOT relocated, and Blocked States was left untouched (it is evolving independently).
- **US-02 (chart efficiency number)** — a flow-efficiency figure on the Cumulative Time per State
  chart, FE-derived (`flowEfficiency.ts`: `resolveWaitRawStates` + the fold), aggregate for the
  in-scope set and recomputed per-item when the existing US-05 picker narrows to one item.
- **US-03 (overview tile)** — a `flowEfficiency` widget in the `flow-overview` category (small KPI
  tile, the established `wipOverviewInfo` / `totalWorkItemAgeInfo` shape), BE-computed over the WHOLE
  in-scope set via a small dedicated `flowEfficiencyInfo` endpoint per scope (team + portfolio). The
  endpoint takes no `itemIds`, so the tile never follows the chart picker. "Not configured" (never a
  misleading 100%) and "no data in scope" (no division) are distinct contract booleans on
  `FlowEfficiencyInfoDto { IsConfigured, HasDataInScope, EfficiencyPercent, … }`, not magic sentinels.
  RAG is **inverted** (`computeFlowEfficiencyRag`: red < 40 / amber 40–60 / green ≥ 60 — efficiency is
  higher-is-better, the opposite polarity of the cumulative-chart RAG).
- **US-04 (wait-bar highlight)** — wait-state bars on the cumulative chart rendered in a distinct,
  red-ish colour family, composing with the existing completed/ongoing segment split, with an
  interactive legend and a 2-row tooltip.

A forecaster can now say "two of our five Doing-states are idle queues, and that is why we are 34%
efficient" — naming idle time as the problem with evidence rather than arguing about effort.

## Key decisions and ADRs

Full decision log (D1–D12 with verbatim framing and the 2026-06-05 DISCUSS revision) lives in the
workspace `feature-delta.md`. Load-bearing ones:

- **D1/D11/D12 — mapping-aware config in the state cluster** — `WaitStates` resolved via the existing
  `GetRawStatesForCategory`, edited next to `StateMappingsEditor`, decoupled from Blocked States. The
  2026-06-05 DISCUSS review superseded the original "structural twin of BlockedStates" framing.
- **D2/D8 — derivation as a fold, no new write/cumulative field** — efficiency is a pure fold over the
  per-state totals already produced; the chart number is FE-derived; the config field rides the
  existing settings DTO additively.
- **D3 vs D4 — "not configured" ≠ "no data in scope"** — distinct contract flags, never 100%, never a
  divide-by-zero.
- **D5/D18 — tile is BE-computed over the whole set, never the picker** — only the chart number follows
  the picker; the tile and chart RAG stay systemic.
- **D10 — inverted RAG** — 40/60 thresholds, higher-is-better polarity.

The architecture delta is captured in four ADRs (all already in their permanent home):

- **ADR-054** — `docs/product/architecture/adr-054-flow-efficiency-derivation-and-contract.md` —
  derivation from existing per-state day totals; FE-computed chart number + wait-bar flag (no new
  cumulative field); BE-computed tile value.
- **ADR-055** — `docs/product/architecture/adr-055-flow-efficiency-tile-transport-and-client-version-gate.md`
  — small dedicated `flowEfficiencyInfo` endpoint per scope (established tile pattern), `trendPolicy:
  none`, and the Lighthouse-Clients version-gate consequence (any wrapping client method must pin
  `FEATURE_REQUIRES_SERVER_NEWER_THAN`; N/A while product-UI-only).
- **ADR-056** — `docs/product/architecture/adr-056-wait-states-config-placement-and-mapping-aware-resolution.md`
  — mapping-aware `WaitStates` + sibling `WaitStatesEditor` (no `StateMappingsEditor` relocation),
  resolved via `GetRawStatesForCategory`.
- **ADR-057** — `docs/product/architecture/adr-057-wait-bar-highlight-and-flow-efficiency-rag.md` —
  wait-bar colour-highlight (FE-derived, composing with segments) + `computeFlowEfficiencyRag`
  (inverted 40/60 thresholds).

Cross-cutting: RBAC N/A (no new permission — `WaitStates` rides the already-gated settings form; the
read surfaces inherit team/portfolio read-gating); clients version-gate flagged for the ONE new
`flowEfficiencyInfo` endpoint if/when wrapped (ADR-055); website N/A (standard flow metric, not a
premium capability).

## The two rounds of user-review UX revision (DELIVER)

The wait-bar highlight (US-04) went through two rounds of user-review revision after the first
delivery, both shipped:

- **Round 1** (`b63a6bb6`) — wait bars became **colour-only** (dropping the pattern/icon idea), an
  **interactive legend** was added, and the chart's flow-efficiency number was moved to sit **below
  the chart title**.
- **Round 2** (`332f7952`) — the wait colour was changed to a **red-ish** family (waste reads as red),
  and the chart tooltip became a **2-row** layout.

Note the **ADR-057 deviation**: the DISCUSS D6 plan called for a colour-blind-safe distinction
conveyed by legend label *and/or pattern/icon, not colour alone*. Per explicit user choice during
review, the wait distinction shipped as **colour-only** (red-ish bars) plus the interactive legend —
the pattern/icon reinforcement was deliberately dropped. Recorded here so the deviation from the
written D6 is not mistaken for an oversight.

## Durable lessons

- **The xAxis-ordinal `colorMap` breaks stacked series** — driving the wait/active bar colour through
  an ordinal `colorMap` on the chart's x-axis broke the existing stacked completed/ongoing segment
  series (the colorMap took over per-band colouring and clobbered the segment split). The highlight
  had to be expressed without the ordinal colorMap to preserve the stacked-series rendering. A trap to
  watch on any future per-bar colouring of this chart.
- **The TBU wiring gap, caught by the walking-skeleton E2E** — the chart's flow-efficiency props were
  not actually threaded through `BaseMetricsView` to the chart component until the live walking
  skeleton (`1393cda0`) exercised it end-to-end; unit tests passed against the components in isolation
  but the number rendered as "TBU"/absent live. The live Playwright run is what surfaced the
  unconnected prop — the same "live-E2E-catches-wiring-gaps-mocks-miss" lesson from prior features.
- **The close-out ArchUnit guard caught a hexagonal-seam leak** — running the Architecture suite during
  close-out surfaced a pre-existing RED: `FlowEfficiencyInfoDto` had been placed under
  `Lighthouse.Backend.API.DTO` (the driving-adapter namespace) yet was returned by the Services core,
  violating the ADR-027 hexagonal seam (core DTOs belong in `Models.*`). It was relocated to
  `Models/Metrics/InfoWidgetDtos.cs` alongside every sibling Info DTO, dropping the now-unused
  `using Lighthouse.Backend.API.DTO;` from the five Services files —
  `ModuleBoundariesArchUnitTest.ServiceLayer_DoesNotDependOnApiLayer` is green again. Shipping a
  close-out that pins the `ComputeFlowEfficiency` seam while that seam was actively violated would have
  been incoherent.
- **EF stale-migration-DLL trap (again)** — the `WaitStates` column needed `dotnet build
  --no-incremental` on both migration projects before `dotnet test`, even for a fresh migration; an
  incremental build can keep a stale migration DLL so the new column is silently absent at test time
  (the Epic-4144 slice-04 / forecast-minimum-data-guard family). InMemory unit tests never catch it;
  the live SQLite round-trip in `FlowEfficiencyReadApiIntegrationTest` does.

## Quality

- **Mutation testing** — Backend core-logic **86.2%** (`ComputeFlowEfficiency` fold 100% (13/13); both
  controllers 100% (4/4); the 4 survivors are all `logger.LogDebug` logging-only equivalents). Frontend
  core logic **89.0%** conservative / **99.1%** excluding equivalents: `flowEfficiency.ts` 100%
  (38/38), `computeFlowEfficiencyRag` 100% (20/20 after amber/green tip-text tests),
  `FlowEfficiencyOverviewWidget.tsx` 100% logic, `WaitStatesEditor.tsx` 80.49% (survivors
  presentational/equivalent). The shared `CumulativeStateTimeChart.tsx` aggregate (58.25%) is
  presentational-bound and carries its own `state-time-cumulative-view` baseline — not a gap introduced
  here. Report: `docs/feature/wait-states-flow-efficiency/deliver/mutation/mutation-baseline.md`.
- **Close-out guardrails** — `FlowEfficiencySeamArchUnitTest` (`ComputeFlowEfficiency` is `protected`,
  no `IFlowEfficiencyService` introduced — ADR-024) and the D9 overlay-only acceptance test
  (`GetFlowEfficiency_WithWaitStatesDefined_DoesNotChangeAnyOtherMetric`: throughput / cycle-time /
  aging endpoint bodies byte-identical before/after defining wait states) both green and falsifiable.
- **Tests / build** — all DELIVER steps EXECUTED / PASS (RED → GREEN → COMMIT for slices 01–05); builds
  clean (0 warnings under `TreatWarningsAsErrors`); Biome clean; the live Playwright walking skeleton
  observed the tile reading 75% with 2 highlighted wait bars against demo data.

## Permanent artifacts

- **ADR-054 / ADR-055 / ADR-056 / ADR-057** — `docs/product/architecture/adr-05{4,5,6,7}-*.md`
- **Application architecture delta** — `docs/product/architecture/brief.md`
  (`## Application Architecture — wait-states-flow-efficiency (Story #5173)`)
- **Jobs** — `job-config-admin-define-wait-states`, `job-spot-flow-efficiency-waste` in
  `docs/product/jobs.yaml`
- **Mutation report** — `docs/feature/wait-states-flow-efficiency/deliver/mutation/mutation-baseline.md`
- **Close-out** — `docs/feature/wait-states-flow-efficiency/deliver/close-out.md`

## Commits

`4fc47b84` docs(DISCUSS) · `976a9e0c` docs(DESIGN: architecture + ADRs) · `f96168da` test(DISTILL:
scaffolded-RED ATs) · `b7573642`/`cf0506ca`/`0801dd95` feat S1 (WaitStates field + tile endpoint
team+portfolio + settings DTO threading) · `cdea26d4` feat S1 (WaitStatesEditor config UI) ·
`409799d7` feat S2 (chart number + resolveWaitRawStates) · `43da20fe` feat S3 (overview tile +
inverted RAG) · `0a1492c8` feat S4 (wait-bar highlight) · `1393cda0` feat (chart props wiring + green
walking-skeleton E2E) · `18e5ed78` test (D9 overlay guard + ComputeFlowEfficiency seam ArchUnit
close-out) · `b63a6bb6` fix (colour-only bars + interactive legend + efficiency below title, user
review) · `332f7952` fix (red-ish wait colour + 2-row tooltip, user review) · `4ba71e99`
test(scoped mutation BE+FE).
