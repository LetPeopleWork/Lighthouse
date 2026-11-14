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

## Wave: DESIGN / [REF] Context-map placement (2026-11-14)

- **Context touched**: Forecasting (packaged from today's code into `Modules/Forecasting`).
- **Neighbours whose edge becomes visible** (no behaviour change): Flow metrics (the cycle, D-3), Work Items
  (`TeamDataRefreshed`, dependencies through `IWhatTheForecastWaitsFor`, repositories), Refinement (reads the manual
  forecast and the filter rules), Promises (the Portfolio refresh triggers the forecast updater; Delivery forecast
  publishing stays in Promises), Licensing (`ForecastUpdater`, `ForecastFilterRuleService` read the licence), the
  update queue (platform lane).
- **New terms / concepts**: none. Folder `Modules/Forecasting` is the ADR-270 glossary name. One new port name is
  proposed by D-3 (`IThroughputFilter`, `IForecastDataSufficiency`, `IHowManyForecaster`, all in Flow metrics'
  Application); they name what Flow metrics asks, in Flow metrics' words.
- **Domain events**: publishes `PortfolioForecastsUpdated` (unchanged, the event type stays in `Models/Events` until
  the kernel shrinks, S04b). Reacts to `TeamDataRefreshed`, `TeamDeleted`, `FeatureRankChanged`,
  `FeatureOrderingPolicyChanged` (the four policies, unchanged, registration order unchanged).
- **UI surface**: none.
- **ADR-270 / `ARCHITECTURE.md` §4 edits**: "SSOT edits pending" below; written in the finalize checklist, not in a
  slice (S02 lesson 3).
- **Improvement backlog check**: S03c is itself a #6510 item, run as its own improvement item; N/A here, stated.

## Wave: DESIGN / [REF] Evidence from the design-time measurements (2026-11-14, `main` at `870be56fb`)

All edge facts below come from the compiled IL of a Debug build of `Lighthouse.dll`, read with the same walk as
`IlDependencies` (fields, properties, signatures, locals, every IL operand, compiler-made nested types folded into the
type that wrote them, generic arguments unwrapped). The scanner is a throw-away console in the session scratchpad,
not committed. Text searches were used only for path and namespace strings.

| # | Finding | Consequence for the design |
|---|---|---|
| E1 | **Flow metrics → Forecasting, 3 edges** (equal to the R5 list): `BaseMetricsService → IForecastService`, `TeamMetricsService → IForecastFilterRuleService`, `TeamMetricsService → ForecastDataSufficiencyPolicy`. **Forecasting → Flow metrics, 6 edges from 5 types** (equal to the R4 list): `ForecastService → ITeamMetricsService, ForecastThroughputStatus`; `ForecastService+GatheredInputs`, `ForecastInputs`, `ForecastInputFingerprint → ForecastThroughputStatus`; `ForecastRealityCheckService → ITeamMetricsService`. | The baselines are current. D-3 decides the direction. |
| E2 | Flow metrics' Application also reaches **`Models.Forecast`** types: `ForecastPredictabilityScore` (both service ports and both services), `HowManyForecast` (`BaseMetricsService`), `ForecastInputCandidatesDto` and `FeatureCandidateDto` (`ITeamMetricsService`, `TeamMetricsService`). Today they sit in the kernel, so R5 does not see them. | If S03c moved them into `Modules/Forecasting`, R5 would gain 4 to 6 edges: the cycle would grow. They stay in `Models` (D-2). |
| E3 | Four `Models/Forecast` types are **EF entities named in the model snapshots of both migration assemblies**: `ForecastBase` (10 hits), `WhenForecast`, `StartForecast`, `IndividualSimulationResult` (4 each). The kernel entities `Feature` and `Delivery` reach `AggregatedWhenForecast`, `DeliveryCompletionForecast`, `FeatureStart`, `ForecastPercentile`, and through them `CompletionHistogram`, `ComonotonicCompletionDistribution`, `JointCompletionDistribution`, `DeliveryForecastRow`. | All of these stay in `Models`: moving an entity changes the EF model (S02 E1), and a Models type may not reach a module (`KernelRules.ModelsDoNotDependOnServices` checks `Services` **or** `Modules`). |
| E4 | Wire types: `RealityCheckResultDto` and its 8 nested DTOs and 6 verdict enums (returned by `ForecastRealityCheckController`), `ForecastInputCandidatesDto` (returned by `TeamMetricsController`), `ForecastPredictabilityScore` (both metrics controllers), and `API/DTO` `BacktestInputDto`, `BacktestResultDto`, `ForecastDto`, `ManualForecastDto`, `WhenForecastDto`, `FeatureStartDto`. | Stay where they are until S12c #6388 (New). |
| E5 | `ForecastSimulationLimits` and `ForecastHorizon` are used only by Forecasting, `Program` and the start-up banner (`Startup.StartupBanner`, `StartupBannerFacts`); `ForecastHorizon.Days` is a constant the compiler copies into its callers, so the IL shows no reader at all. `FeatureForecastWindow` (internal) is read by 7 controllers, `DeliveryMetricValuesProjector` and `WriteBackTriggerService`, and by no Forecasting type. | `ForecastSimulationLimits` and `ForecastHorizon` could move, but would add an inbound edge from the start-up banner; kept in `Models` for this Story (D-2), listed as a later candidate. `FeatureForecastWindow` is not Forecasting's code at all (a display window); it stays. |
| E6 | Inside `Services/{Implementation,Interfaces}/Forecast` (24 files), only 5 types reach outside the kernel and their own namespace: `ForecastService` (Flow metrics, `Services.Interfaces`, `Dependencies`, `Repositories`), `ForecastRealityCheckService` (Flow metrics, `Services.Interfaces`), `ForecastInputFingerprint` and `ForecastInputs` (Flow metrics), `ForecastFilterRuleService` (`Licensing`, `WorkItemRules`). The other 19 reach only `Models` and their own namespace. | A real Domain exists: the simulation, draw streams, run plan, sufficiency and verdict policies (D-4). |
| E7 | Outside callers of the Forecasting core (IL): `API.ForecastController` (`IForecastService`, `IForecastUpdater`), `API.ForecastRealityCheckController`, `API.TeamController`, `API.TeamsController`, `API.Helpers.RuleSetValidation` (`IForecastFilterRuleService`); Refinement `RefinementNeedCalculator` (`IForecastService`), `StageRuleMatcher`, `StageRuleHealing` (`IForecastFilterRuleService`); `PortfolioUpdater` (`IForecastUpdater`); Flow metrics (E1); `Program`. | API, Refinement (legal per ADR-270), Promises' `PortfolioUpdater` (legal: Promises depends on Forecasting) and Flow metrics (removed by D-3). The inbound rule's list after D-3 is **empty**: every caller is legal by namespace. |
| E8 | `ForecastUpdater` (an `UpdateServiceBase<Portfolio>`) and the four trigger/retrigger policies reach the update queue (`Services.Implementation.BackgroundServices.Update`, `Services.Interfaces.Update`), `DomainEvents`, `Repositories`, `Licensing` (`ForecastUpdater` only, ADR-270 ratchet candidate 2) and Forecasting. Nothing but `Program`, `ForecastController` and `PortfolioUpdater` reaches them. | They move with the module (D-1). Their reach into the update queue is a platform-lane edge, legal; the licence read stays a named outbound edge (R-F5 list, 1 entry). |
| E9 | Rules keyed on today's Forecasting namespaces, folders or names (text search over `Lighthouse.Backend.Tests/Architecture` and committed configs): see "Ratchet design", table "Rules first". 11 files. No `appsettings*.json`, script, workflow or Serilog set-up names a Forecasting namespace; one committed Stryker config (`Lighthouse.Backend.Tests/stryker.6094.backend.json`) names a Forecast path. | Rules first (slice A1). Log categories change from `Lighthouse.Backend.Services.Implementation.Forecast.*` and `…BackgroundServices.Update.ForecastUpdater` to `Lighthouse.Backend.Modules.Forecasting.*`; nothing keys on them. |
| E10 | `ForecastUpdater` and `TeamDataRefreshedForecastTriggerHandler` are pinned by name in `DomainEventHandlerOrderPinTest` (short names) and by type in `UpdateHoldsAreForecastOnlyArchUnitTest`, `DependencyAwareForecastSeamArchUnitTest`. `DependencyAwareForecastSeamArchUnitTest` takes its subjects from the namespace `…BackgroundServices.Update`: once `ForecastUpdater` leaves it, the rule still holds for the components that stay, and the exemption "contains ForecastUpdater" becomes dead but harmless. | Widen the subject to "the update components or the Forecasting module's background adapters" in A1, so the rule keeps meaning "only the forecast updater starts a forecast". |

### Hypothesis measurement ("edits beyond namespaces in at most a handful of files")

| Edit kind | Files | Note |
|---|---|---|
| Moved production files | **31**: 18 in `Services/Implementation/Forecast`, 6 in `Services/Interfaces/Forecast`, `ForecastUpdater`, `IForecastUpdater`, 4 policies, `ForecastInputsForgottenAtStartSeeder` | namespace line plus `using`s |
| Production files outside the module needing only a `using` | about **12**: `Program.cs`, 6 in `API` (Forecast, ForecastRealityCheck, Team, Teams controllers, `RuleSetValidation`, and `DtoExtensions` if it names a moved type), 3 in Refinement, `PortfolioUpdater`, `Startup` banner (only if `ForecastSimulationLimits` moves; it does not) | mechanical; counted from E7 |
| Rule, path and config files (not a `using`) | **11 test files + 1 Stryker config** (table "Rules first") | the ratchet owns them |
| Production edits that are not a `using`, for the **move** | **0 expected**; the move slices confirm with `dotnet build` (a qualified name through a parent namespace, like S02's `OverTimeFillTarget`, would show there) | |
| Production edits for the **cycle** (D-3) | **7 files**: 3 new port files in Flow metrics' Application, `BaseMetricsService`, `TeamMetricsService`, `ForecastFilterRuleService` (implements a port), one new adapter for sufficiency and how-many in Forecasting, plus their DI lines in `Program.cs` | a deliberate edit, its own nwdes feature, not packaging |
| Behavioural assertions edited | **0 expected** | pass criterion |

**Reading.** For the packaging itself the hypothesis **holds**: outside the moved set, every production edit is a
`using`, and the other edits are rule and path files that the ratchet owns (the S02 gate read the same shape as
"go"). It holds only for the move: removing the cycle (D-3) adds 7 production files of real edits. That is a
decision the gate asked S03c to make, not an effect of the folder move, and it ships as its own nwdes feature so the
two can be told apart.

## Wave: DESIGN / [REF] Decisions

Status: **Decided (maintainer, 2026-11-14)** = D-0. **Recommended** = D-1 to D-11, for the DESIGN hold. Option
tables are the record of what was weighed.

### D-0 No schema change — **DECIDED (maintainer, 2026-11-14)**

Feature WIP, the throughput window and the forecast filter rules stay on `Team`. Epic #6474 slice 07 moves them into
the Forecasting extension `LeafCapacity` keyed by unit id. S03c touches no entity, no migration, no `DbSet`.

### D-1 Membership — **RECOMMENDED**

One line of why per candidate. "Move" means a pure-move commit, namespace `Lighthouse.Backend.Modules.Forecasting.<Layer>`.

| Candidate | Decision | Why |
|---|---|---|
| `Services/Implementation/Forecast/*` (18) and `Services/Interfaces/Forecast/*` (6) | **Move** (layers in D-4) | ADR-270 Decision 1: the forecasts, the round and its fingerprint, the reality check and the filter rules are Forecasting's. |
| `ForecastUpdater` | **Move**, `Adapters/Background` | ADR-270 gives "the forecast half of Portfolio/Delivery (the forecast updater)" to Forecasting; it is a hosted background service, like S02's history filler. ADR-276 keeps the forecast on `UpdateType.Forecasts` and leaves the updater alone, so 6474 does not reverse this. |
| `IForecastUpdater` | **Move**, `Application` | The port of the use case "run a forecast round", next to it (ADR-270 Decision 6). Callers: `ForecastController`, `PortfolioUpdater`, the policies. |
| `TeamDataRefreshedForecastTriggerHandler`, `TeamDeletedForecastRetriggerHandler`, `FeatureRankChangedForecastTriggerHandler`, `FeatureOrderingPolicyChangedForecastTriggerHandler` | **Move**, `Application/Policies` | Each is "whenever X, then forecast": a Forecasting policy (ADR-270's own example of the role is the first one). 6474 slice 12 makes the trigger ask the eligibility rule and slice 03 re-subscribes the `TeamDeleted` handler to `UnitDeleted`; both edit these files in place, so the move does not fix a shape 6474 reverses. Registration order stays (D-8). |
| `DeliveryForecastPublishingService`, `DeliveryForecastBlockRenderer`, `DeliveryForecastPublishingHandler`, `IDeliveryForecastPublishingService`, `IDeliveryForecastBlockRenderer`, `IDeliveryForecastPublisher`, `Models/DeliverySources/DeliveryForecast*` | **Stay** (Promises, S03d) | They publish a Delivery's joint answer back to its tracker source. The Delivery is Promises'; ADR-270 ratchet candidate 3 already places the connector edge in Promises. Forecasting only supplies the dates. |
| `WhatTheForecastWaitsFor`, `IWhatTheForecastWaitsFor`, `Models/Dependencies/ForecastWaits` | **Stay** (Work Items, S03b) | Dependencies are Work Items' facts (ADR-270 Decision 1); this is the single dependency decision that `DependencySingleDecisionArchUnitTest` keeps in one folder. Forecasting reads it through its port, a legal Forecasting → Work Items edge. |
| `ForecastInputsForgottenAtStartSeeder` | **Move**, `Adapters/Background` | It clears Forecasting's own data (the forecast-input fingerprint) at start; ADR-270 Decision 8 says only the owner writes it. A start-up job is a host-driven adapter. It keeps its `ISeeder` registration and order. |
| Backtest | **Stay**: `BacktestInputDto`, `BacktestResultDto` are wire DTOs (S12c); the backtest runs in `ForecastController` and `IForecastService`, which already move or stay with the API | No separate backtest service exists to move. |
| `ForecastController`, `ForecastRealityCheckController` | **Stay** in `API` | As S02: controllers move with S12a / S12c, not with packaging. |
| `Models/Forecast/*` (19 files) | **Stay** in `Models` | E2 to E5: entities, types the kernel entities reach, wire DTOs, and types Flow metrics reaches. `ForecastSimulationLimits` and `ForecastHorizon` are the only Forecasting-only ones; left for S04b (E5). |
| `PortfolioForecastsUpdated` (event type) | **Stay** in `Models/Events` | ADR-270 Decision 3 moves event types with their publisher, but every event type still sits in the kernel; S04b moves them together. |

Totals: **31 files move**, 0 entities, 0 DTOs. KPI 1: the top-level loose files stay 34 + 44 (Forecasting already
had its own sub-folders); the files under `Services/Interfaces` + `Services/Implementation` go **486 → 455**. KPI 2:
**2 of 11** contexts packaged.

### D-2 What stays in the kernel — **RECOMMENDED**

All 19 `Models/Forecast` files and the 6 `API/DTO` forecast DTOs stay (E2–E5). Alternative: move
`ForecastSimulationLimits` and `ForecastHorizon` to `Modules/Forecasting/Domain`. Rejected for now: it adds an
inbound edge from the start-up banner (Platform) to the module's Domain, for 2 files; S04b takes them with the rest.

### D-3 The Flow metrics ↔ Forecasting cycle — **RECOMMENDED: Forecasting depends on Flow metrics; Flow metrics' 3 edges back are inverted through ports Flow metrics owns**

Which direction stays: **Forecasting → Flow metrics** (Forecasting reads throughput, `ForecastThroughputStatus`,
from Flow metrics' contract). Throughput is a flow metric; a forecast is made from throughput. ADR-270 draws the edge
as "through Work Items' contract", but no code does that: every throughput read goes to `ITeamMetricsService`.
Moving 6 edges onto Work Items would move Flow metrics' calculation into Work Items, against Decision 1. So the map
changes, not the code: the edge becomes legal (customer/supplier, Forecasting the customer).

How the other direction goes, per edge, behaviour-free:

| Edge (Flow metrics → Forecasting) | What Flow metrics needs | Removal |
|---|---|---|
| `TeamMetricsService → IForecastFilterRuleService` | Filter a Team's closed items by the Team's forecast filter rules before counting throughput | Port `IThroughputFilter` in `Modules/FlowMetrics/Application` (one method, the filter call `TeamMetricsService` makes today). `ForecastFilterRuleService` implements it (it already holds that method). `Program.cs` registers the same instance for both interfaces. |
| `TeamMetricsService → ForecastDataSufficiencyPolicy` | Ask whether a throughput window holds enough finished days | Port `IForecastDataSufficiency` in Flow metrics' Application, implemented by a 3-line adapter in `Modules/Forecasting/Application` that calls the policy. The policy stays Forecasting's rule (the reality check uses it too). |
| `BaseMetricsService → IForecastService` (service locator) | Run a How Many forecast for the predictability score | Port `IHowManyForecaster` in Flow metrics' Application, implemented by the same adapter (or by `ForecastService` directly); `BaseMetricsService` resolves the port instead of `IForecastService`. The service-locator call stays a service-locator call (S07b removes it). |

After D-3: R5's Forecasting list is **empty** and stays as a canary that fails on a newcomer; the new module's
outbound rule lists Flow metrics' Application as a **legal** target by namespace. `ForecastFilterSeamArchUnitTest`
("only `TeamMetricsService` and `ForecastFilterRuleService` call `Filter`") is reworded to the port.

| Option | What | Cost | Risk |
|---|---|---|---|
| **a (recommended)** Invert through Flow metrics' ports | As above | about 1 day, 7 production files, one nwdes feature (C) | low: same objects answer, only the interface the caller names changes; the handler order and DI lifetimes stay. Proven by the metrics and forecast answer comparisons. |
| b Domain event | Forecasting publishes a fact Flow metrics stores | — | Rejected: all 3 are synchronous queries inside one request. ADR-270 Decision 3 uses events for reactions, contracts for queries; an event would add stored state and eventual consistency to a chart. |
| c Move the code | Put the filter rules or the sufficiency rule into Flow metrics | — | Rejected: ADR-270 Decision 8 and Epic 6474 slice 07 give the filter rules to Forecasting's `LeafCapacity`; moving them away now is a shape 6474 reverses. |
| d Reverse: Flow metrics → Forecasting stays, Forecasting stops reading Flow metrics | Forecasting gets throughput from a port it owns, implemented in Flow metrics | similar size, 6 edges instead of 3 | Rejected: the port would be implemented by the module that calculates throughput anyway, so it only renames the same dependency, and twice as many call sites change. |
| e Ratchet only | Keep both lists, rewrite their 8 strings to the new namespaces | 0 extra days | Leaves the cycle for S09b, which the gate said must be decided before then. Fallback if the maintainer wants the smallest Story. |

### D-4 Layers inside `Modules/Forecasting` — **RECOMMENDED**

| Layer | Types | Why |
|---|---|---|
| `Domain` | `ForecastSimulation`, `SimulatedRun`, `TrialState`, `TrialRecordings`, `HowTheRunEnded`, `WhatTheRunsCouldNotFinish`, `OneWorkersShareOfTheRuns`, `ForecastRunPlan`, `DayCounts`, `AddressableDrawStream`, `DrawStreamFactory`, `IDrawStream`, `IDrawStreamFactory`, `ForecastDataSufficiencyPolicy`, `RealityCheckVerdictPolicy`, `ForecastRunOutcome` | E6: they reach only `Models` and each other. The Domain rule (R-F2) proves it in the move slice; a type it rejects goes to `Application` in that slice, recorded, not argued. |
| `Application` | `IForecastService`, `ForecastService` (+ `GatheredInputs`), `ForecastInputs`, `ForecastInputFingerprint`, `IForecastRealityCheckService`, `ForecastRealityCheckService`, `IForecastFilterRuleService`, `ForecastFilterRuleService`, `ForecastLane`, `IForecastUpdater`; after D-3 the sufficiency/how-many adapter | Use cases and their ports; `ForecastLane` schedules threads (an effect), so not Domain. |
| `Application/Policies` | the four trigger/retrigger handlers | S02's settled sub-folder for domain-event handlers. |
| `Adapters/Background` | `ForecastUpdater`, `ForecastInputsForgottenAtStartSeeder` | Host-driven entry points. |
| `Adapters/Persistence` | none | Forecasting has no repository of its own today (forecasts are written through `FeatureRepository`, S13 later). |

### D-5 The seam Epic 6474 slice 07 writes into — **RECOMMENDED (named, not built)**

Slice 07 adds `LeafCapacity` (Feature WIP, auto-adjust, throughput window, filter rules; keyed by unit id):

- entity: `Modules/Forecasting/Domain/LeafCapacity.cs` would need an EF model change, so slice 07 decides whether it
  lives in `Models` (as today's entities, E3) or in the module; S03c fixes nothing there;
- store port: `ILeafCapacityStore` in `Modules/Forecasting/Application`, implemented in
  `Modules/Forecasting/Adapters/Persistence` (the sub-folder S02 settled; `RepositoryRecognition` already accepts it);
- readers: `ForecastService.GatheredInputs` / `ForecastInputs` (Feature WIP, window), `ForecastFilterRuleService`
  (filter rules), `ForecastInputFingerprint`, and, through D-3's `IThroughputFilter`, Flow metrics' throughput. Today
  they read `Team`; slice 07 points them at the port. With D-3 a, Flow metrics never names `LeafCapacity`.

### D-6 Shared module rules — **RECOMMENDED**

First step of the rules-first feature (gate change 3): rename `FlowMetricsModuleRules` to `ModuleRules` (module
namespaces as parameters, as today) and lift into it the two private shapes `ApplicationIsReachedFromOutsideOnlyBy`
(from `FlowMetricsInboundApplicationArchUnitTest`) and `OutboundEdges` with `OutboundVerdict` (from
`FlowMetricsApplicationOutboundArchUnitTest`). Flow metrics' tests call the lifted methods with their own constants;
their made-up-type tests stay green unchanged in meaning. A move-only commit (rename) and an edit commit (lift).

### D-7 Rules first — **RECOMMENDED**

Every rule in "Ratchet design / Rules first" is widened before any type moves, and a made-up type under
`Lighthouse.Backend.Modules.ForecastingFixtures` proves each widened rule still sees a type in the new place.

### D-8 Registration order — **RECOMMENDED**

As S02 D-8 a: Program.cs lines stay where they are, only `using`s change; `DomainEventHandlerOrderPinTest` already
pins `TeamDataRefreshedForecastTriggerHandler`'s place among the refresh handlers and must stay green unmodified. No
`AddForecasting` extension (S09a).

### D-9 Unit-test placement — **RECOMMENDED**

As S02 D-9 a: unit tests of moved types relocate to `Lighthouse.Backend.Tests/Modules/Forecasting/{Domain,
Application, Adapters}` as a pure-move slice; a placement test like `FlowMetricsUnitTestPlacementTest` (generalised
in D-6's class if cheap, else one more fixture) keeps them there. Integration tests under `API/Integration` stay.

### D-10 Behaviour-free proof and EXAMINE — **RECOMMENDED**

1. Existing tests green unmodified, apart from moved tests, `using`s and the rule files.
2. **Seeded forecast identity**: a committed port-level oracle in each move feature runs `ForecastSimulation` and
   `ForecastService` (How Many and When) with `DrawsFromAPinnedStartingNumber` and the same inputs, and compares the
   percentiles to values written out in the test (not computed by the code under test). Run before the move (green
   on the old namespaces) and after.
3. **Forecast answer comparison**: a new `Scripts/examine/bin/forecast-answers-compare`, built like
   `metrics-answers-compare`: on the demo data, capture before and after the bodies of `GET` forecast endpoints
   (Team manual How Many/When, Feature When/start forecasts in the feature list, the reality check, the backtest)
   with forecasts pinned by a fixed seed, and `diff` them. Declared in `.nwave/examine.json` before the first EXAMINE.
4. Reuse: `architecture-rules`, `ef-model-unchanged` (no EF change expected; it proves D-0), `metrics-answers-compare`
   (D-3 touches the metrics services), `backend-tests`.
5. **The `DB_Backup` gap (S05f)**: both answer comparisons need a production-like database that the examiner host
   does not have. Run them on the seeded **demo** data, which every host can build, and record the production-like
   comparison as a maintainer-run step, not an EXAMINE expectation. Otherwise the gap returns as "indeterminate".

### D-11 Mutation bar — **RECOMMENDED**

Last, on frozen code, Stryker.NET with whole-file `mutate` entries, the same test filter old and new paths
(`Tests.Modules.Forecasting` named, per the test-writing skill rule 6). Bar: **no survivor or no-coverage mutant
added in a moved file** (S05f's proposal; the maintainer is asked to confirm it, see Open questions). D-3's edited
files: the new ports and adapter at 80 % or more as new code.

## Wave: DESIGN / [REF] Slices

Every slice is a day or less, has **its own oracle file** (S02 lesson 2, S05f), and keeps move commits
(`refactor(forecasting): move …`) apart from edit commits. Three nwdes features, at most 5 slices each, run in this
order. ADR-270, `ARCHITECTURE.md`, brief and catalogue edits, and the mutation run, are in the finalize checklist,
not in a slice (S02 lesson 3). Size: **L, about 7 days** (the brief's M does not hold: rules first, the cycle and the
test move add 4 days to the 2-day move). With D-3 option e instead of a, feature C loses C1 (about 6 days).

| Id | nwdes feature | Delivers, in plain words | Oracle file (new, one per slice) | Size |
|---|---|---|---|---|
| A1 | A `story-6361-s03c-a-rules-first` | The module rules become one shared class any module can use; Flow metrics' rules call it and still catch the same made-up offenders | `Architecture/ModuleRulesCatchANewOffenderArchUnitTest.cs` | 0.5 d |
| A2 | A | Every rule that names Forecasting's namespaces, folders or files also sees the future `Modules/Forecasting`; a made-up type there is caught by each | `Architecture/ForecastingRulesSeeTheModuleArchUnitTest.cs` | 1 d |
| A3 | A | Safety net: a pinned-seed check that forecasts give the same percentiles, and a public command that captures the forecast answers on demo data before the move | `Tests/Acceptance/Forecasting/SeededForecastsStayTheSameTest.cs` (port level, `ServicePortHarness`) + `Scripts/examine/bin/forecast-answers-compare` | 1 d |
| B1 | B `story-6361-s03c-b-move` | The simulation, draw streams, run plan and the two forecast policies live in `Modules/Forecasting/Domain`, which reaches only the kernel | `Architecture/ForecastingDomainLayoutArchUnitTest.cs` | 0.5 d |
| B2 | B | The forecast, reality-check and filter-rule services and their ports live in `Modules/Forecasting/Application`; the Flow metrics lists name them in the new place | `Architecture/ForecastingApplicationLayoutArchUnitTest.cs` | 1 d |
| B3 | B | The forecast updater and the start-up seeder live in `Adapters/Background`, the four forecast triggers in `Application/Policies`; handler order unchanged | `Architecture/ForecastingAdaptersAndPoliciesLayoutArchUnitTest.cs` | 0.5 d |
| C1 | C `story-6361-s03c-c-cycle-and-rules` | Flow metrics no longer depends on Forecasting: it asks through three ports of its own, which Forecasting answers; metrics and forecast answers unchanged | `Architecture/FlowMetricsDoesNotReachForecastingArchUnitTest.cs` | 1 d |
| C2 | C | Forecasting has its own module rules (Domain, Adapters, Application in and out) with inline lists; Flow metrics' inbound list loses the five Forecasting entries | `Architecture/ForecastingModuleBoundariesArchUnitTest.cs` | 1 d |
| C3 | C | Forecasting's unit tests sit under `Tests/Modules/Forecasting` and a placement test keeps them there | `Architecture/ForecastingUnitTestPlacementTest.cs` | 0.5 d |

Finalize (not nwdes slices): mutation on frozen code (D-11, baseline started in A3 on old paths); ADR-270,
`ARCHITECTURE.md` §4, brief, catalogue Improvement log, the brief's 2026-11-01 update corrected (D-0); the Lighthouse
finalize checklist.

Why three features: each is a coherent review unit (rules only; pure moves; edits), each under 5 slices, and C can
be dropped to "ratchet only" (D-3 e) without touching A or B.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list is inline, compared both ways (ADR-268), never loosened.

### Rules first (A2): every rule keyed on Forecasting's namespaces, folders or files

| File | Today | Widening |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | `ForecastingPattern` = `Services.(Implementation\|Interfaces).Forecast` | add `\|^Lighthouse\.Backend\.Modules\.Forecasting(\.\|$)`; `NamedModules_AreNonEmpty` gains the Forecasting module namespaces (after B) |
| `FlowMetricsApplicationOutboundArchUnitTest` | reads `ForecastingPattern` by reflection; 3 full-name strings | widened through the pattern; strings rewritten in B2 (edit commit), emptied in C1 |
| `FlowMetricsInboundApplicationArchUnitTest` | 5 Forecasting full-name strings | rewritten in B2; removed in C2 when Forecasting becomes a legal caller by namespace |
| `SimulatedRunIsolationArchUnitTest` | `DoNotResideInNamespace("…Services.Implementation.Forecast")` | namespace **pattern** old-or-module; otherwise the move turns it red (`ForecastSimulation` would look like an outsider holding `TrialState`) |
| `RealityCheckReadOnlyArchUnitTest` | full names of the verdict policy and the sufficiency policy; `ImplementationPattern` = `Services.Implementation` as forbidden target | match by type (`typeof`) or old-or-new pattern; forbidden target becomes `ApplicationCore` (`Services` or `Modules`) minus the two policies, so a moved service stays forbidden |
| `DependencyAwareForecastSeamArchUnitTest` | subject = namespace `…BackgroundServices.Update` | subject = the update components **or** `Modules.Forecasting.Adapters.Background`, exemption by `typeof(ForecastUpdater)` |
| `FastWithManyFeaturesArchUnitTest` | two source paths (`Services/Implementation/Forecast/ForecastService.cs`, `…/Update/ForecastUpdater.cs`) | locate each file by name under the production source (as it already does for `ForecastLane.cs`) |
| `RefinementModuleArchUnitTest` | "Refinement reaches no background update" forbids `PortfolioDeliveryPattern` | add `Modules.Forecasting.Adapters`, so `ForecastUpdater` stays forbidden to Refinement after it leaves `BackgroundServices` |
| `UpdateHoldsAreForecastOnlyArchUnitTest`, `ForecastFilterSeamArchUnitTest`, `DomainEventHandlerOrderPinTest` | `typeof` / short names | `using` only; listed so the reviewer sees they were checked |
| `RecurringBlackoutEventsSeamArchUnitTest`, `StalenessPolicyPurityArchUnitTest` | namespace constants `Services.Implementation` / `Services` | A2 checks whether a Forecasting type is in their subject or target set; widen to `ApplicationCore` if so |
| `Lighthouse.Backend.Tests/stryker.6094.backend.json` | one Forecast path | path updated in B (config, not a rule) |

Proof (A2's oracle): made-up types in `Lighthouse.Backend.Modules.ForecastingFixtures.*` are seen by each widened rule
(subject and target), the S02 way.

### New and changed rules (C2), measured from the IL on 2026-11-14

| # | Rule | Baseline after S03c |
|---|---|---|
| R-F1 | Each of the 31 types lives in its layer; none left in `Services` (`ModuleRules.TypesLiveInTheirLayer`) | the type lists of D-4 |
| R-F2 | `Modules.Forecasting.Domain` reaches only `Models` and the BCL | empty |
| R-F3 | Outside → Domain only from the list | `Program` (registers `DrawStreamFactory`, `ForecastSimulation`); confirmed from the IL in C2 |
| R-F4 | Outside → Adapters only from the composition root | `Program` |
| R-F5 | Outside → Application only from `API`, the composition root, Refinement (legal by namespace) and the list | 1: `Services.Implementation.BackgroundServices.Update.PortfolioUpdater` (Promises' refresh, ratchet candidate 4) |
| R-F6 | Application → no EF Core, `Data`, `API`, module `Adapters`; Flow metrics' Application legal; Licensing only through the list | 1: `ForecastFilterRuleService → ILicenseService` (ratchet candidate 2; `ForecastUpdater`'s licence read sits in Adapters and is named in prose) |
| R4 (Flow metrics) | Forecasting becomes a legal caller by namespace | 10 → **5** |
| R5 (Flow metrics) | Forecasting edges | 3 → **0** (D-3 a); stays as an empty, two-way list |

## Wave: DESIGN / [REF] Component decomposition

Root `Lighthouse.Backend/Lighthouse.Backend/Modules/Forecasting/`; namespace = folder. MOVE = pure-move commit; EDIT =
separate commit; STAY.

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| 16 Domain types (D-4) | `Services/Implementation/Forecast`, `Services/Interfaces/Forecast` (`IDrawStream`, `IDrawStreamFactory`, `ForecastRunOutcome`) | `Domain/` | MOVE | B1 |
| `IForecastService`, `ForecastService`, `ForecastInputs`, `ForecastInputFingerprint`, `IForecastRealityCheckService`, `ForecastRealityCheckService`, `IForecastFilterRuleService`, `ForecastFilterRuleService`, `ForecastLane` | same | `Application/` | MOVE | B2 |
| `IForecastUpdater` | `Services/Interfaces/Update` | `Application/` | MOVE | B2 |
| four trigger/retrigger handlers | `Services/Implementation/BackgroundServices/Update` | `Application/Policies/` | MOVE | B3 |
| `ForecastUpdater` | same | `Adapters/Background/` | MOVE | B3 |
| `ForecastInputsForgottenAtStartSeeder` | `Services/Implementation/Seeding` | `Adapters/Background/` | MOVE | B3 |
| `IThroughputFilter`, `IForecastDataSufficiency`, `IHowManyForecaster` | — | `Modules/FlowMetrics/Application/` | NEW (EDIT) | C1 |
| adapter answering sufficiency and how-many | — | `Application/` | NEW (EDIT) | C1 |
| `BaseMetricsService`, `TeamMetricsService`, `ForecastFilterRuleService`, `Program.cs` DI lines | | | EDIT | C1 |
| `Program.cs` registrations | | same lines | EDIT (`using`s only, D-8) | B1–B3 |
| `Models/Forecast/*`, `API/DTO` forecast DTOs, controllers, Delivery forecast publishing, `WhatTheForecastWaitsFor`, `PortfolioForecastsUpdated` | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: `ForecastController`, `ForecastRealityCheckController`, the Team/Teams controllers for filter rules
(routes, `RbacGuard`s and response shapes untouched; they stay in `API`). Events: `TeamDataRefreshed`, `TeamDeleted`,
`FeatureRankChanged`, `FeatureOrderingPolicyChanged` reach the four policies in the pinned order. Host:
`ForecastUpdater` as an update service, the seeder at start. In-process: `PortfolioUpdater` → `IForecastUpdater`;
Refinement → `IForecastService`, `IForecastFilterRuleService`. New (C1): Flow metrics → its own three ports,
implemented in Forecasting.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `ITeamMetricsService` (throughput, `ForecastThroughputStatus`) | Flow metrics | unchanged; edge made legal (D-3) |
| `IFeatureRepository`, `ITeamRepository`, `IPortfolioRepository` (forecast rows still on `Feature`, S13) | Platform repositories | unchanged |
| `IWhatTheForecastWaitsFor` | Work Items (dependencies) | unchanged |
| `ILicenseService` | Licensing | unchanged; R-F6 list + prose |
| `IUpdateQueueService`, `IDomainEventDispatcher`, `ILighthouseClock`, `IBlackoutPeriodService` | platform lane, Organisation set-up | unchanged |
| No tracker call, no new external call. Earned trust: no new adapter or dependency, so no new probe. | | |

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET and `IlDependencies` over Mono.Cecil (already in the test project), NUnit, Stryker.NET. The
answer comparison is a shell script like `metrics-answers-compare`.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `FlowMetricsModuleRules` + `InboundVerdict` | module rules | **EXTEND**: rename to `ModuleRules`, lift the two private Application shapes into it (D-6). No second rules class. |
| `ApplicationCoreRules.ApplicationCore`, `RepositoryRecognition` | already `Services` or `Modules` | **REUSE** unchanged |
| `ModuleBoundariesArchUnitTest.ForecastingPattern` | the one Forecasting pattern | **EXTEND** (widen); Flow metrics' outbound rule keeps reading it |
| `DomainEventHandlerOrderPinTest` | handler order | **REUSE** unchanged |
| `FlowMetricsUnitTestPlacementTest` | test placement | **EXTEND** (parameterise by module) or one sibling fixture, decided in C3 by size |
| `Scripts/examine/bin/metrics-answers-compare`, `ef-model-unchanged`, `architecture-rules`, `backend-tests` | EXAMINE | **REUSE**; **CREATE** `forecast-answers-compare` from the metrics one (no forecast comparison exists) |
| `ServicePortHarness`, `DrawsFromAPinnedStartingNumber` | seeded forecast at the port | **REUSE** for A3 |
| `ForecastFilterRuleService.Filter` | the throughput filter | **REUSE** as the implementation of `IThroughputFilter` (no second filter) |
| `ForecastDataSufficiencyPolicy` | sufficiency rule | **REUSE**; the C1 adapter calls it, nothing copies it |
| Registration extension `AddForecasting` | none | **not created** (D-8) |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, UI, topologies unchanged (D-10). No schema change (D-0). |
| RBAC | N/A, because no endpoint, guard or authorisation call changes; the controllers and their `RbacGuard`s stay in `API`; nothing fetches `/api/latest/authorization/my-summary`. |
| Lighthouse-Clients (CLI, MCP) | N/A, because no route, DTO or enum changes; no client release. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because no feature becomes usable or used differently; nothing appended to `UsageDataEventName` or `docs/settings/usagedata.md`. |
| UI sketch | N/A, because no view is added or changed. |
| Loading and failure look | N/A, because no screen fetches differently. |
| Terminology | No user-facing text. Code names: `Forecasting` (ADR-270). The three new ports use Flow metrics' words (throughput filter, data sufficiency, how-many forecast). Comments touched lose internal references. |
| Log categories | Change with the namespace (E9); nothing keys on them. Recorded in the evolution file. |
| `Program.cs` | Edited in B and C: the push runs the full CI backend suite, live tracker tests included. Push the Story's features together. |
| Epic 6474 | No shape reversed: the updater, the policies and the filter rules move as they are; slice 07 gets a named seam (D-5); slice 12 edits the triggers in their new place; the "[SUPER] Epic" feedback concerns the connection mapping (ADR-274), not Forecasting. |
| Mutation | D-11. |
| Test-writing skill | A3's seeded check sits at the application-service port under `Tests/Acceptance/Forecasting/`; its mutation config names `Tests.Acceptance.Forecasting` (rule 6). Rule tests have no clock. |

## Wave: DESIGN / [REF] Formal verification

Not applicable, with the reason: S03c changes no state, transition, protocol, concurrency or recovery behaviour. The
claims are (1) "the same types exist, in other namespaces" and (2) "the same objects answer the same calls, through
another interface name" (D-3). Neither is a state-machine property for Quint or TLA+, nor a proof obligation for
Agda: (1) is decided exactly by the compiled IL (R-F1 to R-F6 and the widened rules, each with a made-up offender as
its falsifier), and (2) by observation of the real answers (seeded forecast identity at the port, and the forecast
and metrics answer comparisons). The one law that is broad, "for every seed and input the forecast is the same
before and after", is a refactoring-preservation law whose two sides are the same code; a property test over seeds
would compare the code with itself and could not fail. So the pinned-seed check with values written out is the
honest falsifier. No checker was probed or installed; none is needed. If a later Story changes the forecast round's
scheduling (6474 slice 12), that Story owns a state model.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature C.

**ADR-270**, proposed text:

- Decision 1, Forecasting row, last column: "**Packaged** in `Modules/Forecasting` by Story 6361 (date). Its entities,
  the types the kernel entities reach and its wire DTOs still sit in `Models` and `API/DTO`. The leaf unit's capacity
  inputs move with Epic 6474 slice 07, not with the packaging."
- Decision 1, Forecasting responsibility: replace "(today both live on `Team`; they move when Forecasting is packaged,
  an expand-only schema change)" with "(today both live on `Team`; Epic 6474 slice 07 moves them, an expand-only
  schema change)". Same correction in the Feature WIP glossary row and the Decision 8 row.
- "Every edge": replace the two Derived rows with: "Flow metrics → Forecasting (Forecasting calls Flow metrics) |
  customer/supplier | Forecasting reads throughput and `ForecastThroughputStatus` through Flow metrics' Application |
  **Yes** (decided by Story 6361: a forecast is made from throughput, a flow metric) | Derived" and "Forecasting →
  Flow metrics | none | Removed by Story 6361: Flow metrics asks three ports of its own (`IThroughputFilter`,
  `IForecastDataSufficiency`, `IHowManyForecaster`), which Forecasting implements | **Not legal**, list empty".
  Change the Work Items → Forecasting row's text "reads Features and throughput through Work Items' contract" to
  "reads Features through Work Items' contract".
- New block "Edges measured by packaging Forecasting (date)" with R-F2 to R-F6 and the new R4/R5 counts.
- Decision 6, settled folder names: add "Forecasting: `Domain` (16), `Application` (10 + adapter),
  `Application/Policies` (4), `Adapters/Background` (2)".
- Gate decision change 5: "*Done (Story 6361):* the cycle is broken as above."

**ARCHITECTURE.md** §4: Forecasting row anchors become `Modules.Forecasting.*`; the mermaid line Flow metrics ↔
Forecasting becomes one arrow (Forecasting depends on Flow metrics); the enforced-rules list gains R-F1 to R-F6 and
the shared `ModuleRules`.

**brief.md**: a new section `## Application Architecture — story-6361 s03c package forecasting` with the module layout,
the cycle decision and the port names, without touching earlier sections.

**Epic catalogue**: Improvement log entry; brief slice-05 "Update after S01" corrected per D-0; S03* notes: the
shared rules class exists.

## Wave: DESIGN / [REF] Open questions

1. **D-3, the cycle**: option a (invert, about 1 more day) or e (ratchet only). See the report's maintainer question.
2. **D-11, the mutation bar** for a move: "no survivor or no-coverage mutant added" (S05f's proposal) is not yet
   confirmed by the maintainer.
3. **D-10, `DB_Backup`**: accept demo-data answer comparisons for EXAMINE and a maintainer-run production-like
   comparison, or provide the database to the examiner host?
4. Size: L (about 7 days) against the brief's M. The catalogue's S03c size is corrected at close-out.
5. Later, not this Story: `ForecastSimulationLimits` and `ForecastHorizon` to the module (S04b); the licence reads of
   `ForecastUpdater` and `ForecastFilterRuleService` (ratchet candidate 2).

## Wave: DESIGN / [REF] Architecture summary

A learning aid for the maintainer, not a replacement for `brief.md` or the ADRs.

**1. The shape in hexagonal terms (N = new, M = moved, U = unchanged).**

- Driving adapters: `ForecastController`, `ForecastRealityCheckController`, the Team/Teams controllers (U, in `API`);
  `ForecastUpdater` (M, `Adapters/Background`); the start-up seeder (M, `Adapters/Background`); the four event
  policies (M, `Application/Policies`, order U).
- Application: forecast, reality-check and filter-rule services with their ports, `ForecastLane`, `IForecastUpdater`
  (M); the adapter that answers Flow metrics' two ports (N).
- Domain: Monte Carlo simulation, draw streams, run plan, sufficiency and verdict policies (M, `Domain`); entities and
  DTOs (U, `Models`, `API/DTO`).
- Driven: Flow metrics' throughput contract (U, now a legal edge); repositories, Work Items' dependency port,
  Licensing, the update queue (U). No new external call.
- In Flow metrics: three small ports (N) replace three direct reaches into Forecasting.

```text
 TODAY                                         AFTER S03c
 API controllers ─┐                            API controllers (U) ─┐
                  v                                                 v
 Services/…/Forecast  <────────┐               Modules/Forecasting
   ForecastService ──reads──>  │                 Adapters/Background (updater, seeder)
   RealityCheck ────reads──> Modules/FlowMetrics     │ drives
 BackgroundServices/Update     │   Application        v
   ForecastUpdater, 4 triggers │   (Base/TeamMetrics  Application (+Policies) ──reads──> FlowMetrics.Application
 Seeding/ForecastInputs…       │    ──uses──> IForecastService,      │            ^           │ declares
                               └──── IForecastFilterRuleService,     v            │ implements v
                                     ForecastDataSufficiencyPolicy) Domain (pure)  └── IThroughputFilter,
                                                                                       IForecastDataSufficiency,
   (a cycle: each side reaches the other)                                              IHowManyForecaster
                                                                    (one direction: Forecasting -> Flow metrics)
```

**2. Patterns weighed for the cycle (and around it).**

- Dependency inversion through ports owned by Flow metrics: **chosen**; Flow metrics names what it needs in its own
  words, Forecasting implements it, the arrow points one way and the same objects keep answering.
- Domain event: **rejected**; all three are synchronous queries, and ADR-270 keeps events for reactions.
- Move the code into Flow metrics: **rejected**; the filter rules are Forecasting's data (6474 slice 07 builds on that).
- Reverse direction (Forecasting gets throughput through its own port): **rejected**; same dependency renamed, twice
  as many call sites.
- Ratchet only: **fallback**; cheapest, but leaves the cycle the gate asked S03c to decide.
- Shared rules class (`ModuleRules`) instead of copies: **chosen**; the next eight contexts reuse it (open/closed by
  parameters).
- Namespace ratchet from the IL with inline two-way lists: **chosen**, as S02.
- Entity and DTO relocation: **deferred** (S12c, S04b, S13); migration snapshots carry entity names.
- DI extension `AddForecasting`: **deferred** (S09a); handler order is behaviour.
- Not needed: outbox, unit of work, value objects, optimistic concurrency, strategy (no data or variation changes).
