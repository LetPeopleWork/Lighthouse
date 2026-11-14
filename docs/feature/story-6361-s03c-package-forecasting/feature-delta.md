# Feature: story-6361-s03c-package-forecasting

Story #6361 · S03c Package Forecasting · Epic #6510 (Modules: package the bounded contexts)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
- Brief: [`slices/slice-05-s03c-package-forecasting.md`](../epic-6264-modular-monolith/slices/slice-05-s03c-package-forecasting.md)
- ADR: [`adr-270-bounded-contexts-context-map-and-module-layout.md`](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
- Prerequisite of Epic #6474: [`epic-6474-one-structure/feature-delta.md`](../epic-6474-one-structure/feature-delta.md),
  section "Plan update: improvement Stories become prerequisites (maintainer, 2026-11-14)". S03c is the first of three.

DISCUSS: covered at Epic level (see the Epic's Wave Decisions D-6264-3, D-6264-4); waves start at DESIGN.

Picked up 2026-11-14. ADO #6361 was already Active (set by the maintainer at 13:50 that day).

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: DDD vocabulary · Story Map (dependency graph, priority rationale) · How to pick up a Story · Stories →
  S03a–S03h (the "Notes from S02" list applies to every S03*), S12c, S13 · Cross-Cutting Impact Checklist · Outcome
  KPIs + guardrails · Wave Decisions (D-6264-13 logical data ownership, D-6264-16 eleven contexts) · Risks 1, 2, 4,
  5 · Improvement log (S02: gate go-with-changes and its lessons; S05f: mutation bar for a move, own oracle file per
  nwdes feature, EXAMINE needs the `DB_Backup` database).
- ADR-270: the Forecasting context, the context map, the glossary, `Modules/<Context>/{Domain, Application,
  Adapters}`, "Lessons from the Flow metrics pilot" and "Gate decision" (changes 1–5).
- ADR-268 (ratchet convention: inline file lists, never loosened).
- Epic #6474: its delta, ADR-274 (draft, in that workspace) and the open mockup feedback (the "[SUPER] Epic" point).

## Wave: DESIGN / [REF] DoR re-run (2026-11-14)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Forecasting is spread over `Services/{Interfaces,Implementation}/Forecast`, `Models/Forecast`, update handlers and loose files, with no namespace rule (re-count below). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files below; the Flow metrics rule lists that name Forecasting types. |
| 4 UAT | PASS (catalogue) | Four testable AC in the brief, CI-observable; Given/When/Then scenarios are written by the acceptance designer in DELIVER. |
| 5 AC from UAT | PASS | AC map to the S03* Elevator Pitch "After". |
| 6 Right-sized | PASS by waiver, **size to re-check** | Brief says M. Today's file count (below) makes M–L likely; DESIGN cuts slices of a day or less, at most 5 per nwdes feature (D-6264-4). |
| 7 Technical notes and cross-cutting | PASS | Catalogue checklist; re-answered for this Story at DESIGN. |
| 8 Dependencies | PASS | Hard: S01 #6357 Closed; S02 #6358 Closed, gate go-with-changes (2026-11-09). Soft: S03b #6360 Next, **not done**; the maintainer put S03c first on 2026-11-14 (6474 plan). Cross-Epic hard: S12c #6388 New, so Forecast DTOs in `API/DTO` and `Models/Forecast/*Dto` stay where they are. |
| 9 Outcome KPIs | PASS | KPIs 1, 2, 3, 12 from the catalogue. |

## Wave: DESIGN / [REF] Baseline re-count (old → new, 2026-11-14)

| Measure | Brief / catalogue | Today |
|---|---|---|
| Loose files in `Services/Interfaces` + `Services/Implementation`, top level (KPI 1) | 34 + 44 (after S02) | **34 + 44** |
| Files under `Services/Interfaces` + `Services/Implementation`, total (KPI 1) | 485 (after S02) | **486** |
| `Models` files (KPI 3) | 261 (S02 pick-up) | **267** |
| Contexts packaged with namespace rules (KPI 2) | 1 of 11 (after S02) | **1 of 11** (`Modules/FlowMetrics` only) |
| Hypothesis "nearly self-contained, edits beyond namespaces in at most a handful of files" | — | To test at DESIGN. Core: `Services/Implementation/Forecast` **18** files, `Services/Interfaces/Forecast` **6**, `Models/Forecast` **19**. Around it, membership to decide against ADR-270: `ForecastUpdater` + `IForecastUpdater`, four `*ForecastTriggerHandler`/`*RetriggerHandler` in `BackgroundServices/Update`, `DeliveryForecastPublishingService`/`…BlockRenderer`/`…PublishingHandler`/`IDeliveryForecastPublisher` (Promises or Tracker integration?), `WhatTheForecastWaitsFor` (Dependencies), `ForecastInputsForgottenAtStartSeeder`, the backtest (`BacktestResultDto`). |
| Flow metrics → Forecasting edges (S02 R5 list) | 3 | **3**: `BaseMetricsService → IForecastService`, `TeamMetricsService → ForecastDataSufficiencyPolicy`, `TeamMetricsService → IForecastFilterRuleService` (`FlowMetricsApplicationOutboundArchUnitTest.cs:58-60`) |
| Forecasting → Flow metrics Application (S02 R4 list) | 5 | **5**: `ForecastInputFingerprint`, `ForecastInputs`, `ForecastRealityCheckService`, `ForecastService`, `ForecastService+GatheredInputs` (`FlowMetricsInboundApplicationArchUnitTest.cs:61-65`). With the R5 edges this is the Flow metrics ↔ Forecasting cycle S03c's DESIGN decides. |
| Forecasting-specific ArchUnit seams keyed on today's namespaces | — | At least `ForecastFilterSeam`, `DependencyAwareForecastSeam`, `SimulatedRunIsolation`, `UpdateHoldsAreForecastOnly`, `BlackoutForecastShiftSeam`, `FastWithManyFeatures`, `DeliveryGrainSeam` (`Lighthouse.Backend.Tests/Architecture/`). Rules-first slice widens them (S02 lesson). |
| S107 suppressions in Forecasting (KPI 6) | `ForecastRunPlan` (brief) | **2**: `ForecastRunPlan`, `ForecastService` (S07d's; out of scope) |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. **Membership** of every file in the "around it" list above, against ADR-270's context map. The brief puts the
   `*Updater` pipelines out (S03d); `ForecastUpdater` and the trigger handlers need an explicit call.
2. **The Flow metrics ↔ Forecasting cycle**: which direction stays, and how the other one goes (port, event or move),
   still behaviour-free. ADR-270 gate change 5.
3. **Shared module rules**: S03c is the first Story after S02 to touch the module rules, so it lifts the Application
   inbound and outbound rules into a module-neutral class first (ADR-270 gate change 3).
4. **Feature WIP and the throughput window.** The brief's 2026-11-01 update says they move into a Forecasting table
   keyed by unit id (a schema change). Epic #6474's plan says its slice 07 makes that move into a finished
   `Modules/Forecasting`. A schema move is not behaviour-free packaging. **Decided (maintainer, 2026-11-14): 6474
   slice 07 makes the move; S03c changes no schema.** The brief's 2026-11-01 update is corrected at close-out.
5. **6474 conditions**: read the 6474 delta, ADR-274 and the "[SUPER] Epic" feedback; fix no shape that 6474 reverses.
   Name the Forecasting seam slice 07 will write into (the leaf capacity inputs keyed by unit id).
6. **Size and split**: M or L; more than 5 slices means more than one nwdes feature, one oracle file each.
7. **EXAMINE**: forecast results identical for seeded draws, plus an answer comparison for the forecast endpoints
   (reuse `Scripts/examine`; the `DB_Backup` database gap from S05f).

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies are unchanged. The existing API, E2E and response tests stay green unmodified, apart from tests that move along with their code. Forecast results identical for seeded draws.
- [ ] **Ratchet in the same change**: the namespace-based Forecasting rules land in the commit range that makes them pass; baselines are inline file lists; no rule loosened.
- [ ] **Moves separate from edits**: `refactor(forecasting): move …` commits apart from edit commits.
- [ ] **Re-count before designing**: done above; DESIGN confirms the membership against ADR-270 and measures edges from the IL.
- [ ] **Mutation last, on frozen code**: no survivor or no-coverage mutant added in the moved files (S05f bar, maintainer to confirm).
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4 Forecasting row, anchors and diagram; ADR-270 where a decision changes (the cycle direction).
- [ ] **One context at a time**: old and new layouts coexist until Forecasting is fully moved.
- [ ] **Terminology**: names follow ADR-270's glossary (`Modules/Forecasting`).
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered with no silent N/A.
- [ ] **Rules first**: every rule keyed on Forecasting's namespaces or folders widened before any type moves, proven with a made-up type under `Modules`.
- [ ] **Configuration checked for namespace strings** (log categories change with the namespace).
- [ ] **`Program.cs` edits**: the push runs the full CI backend suite, live tracker tests included.
- [ ] **No shape that Epic #6474 reverses** (condition from the 6474 plan).
