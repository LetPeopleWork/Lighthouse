# Feature: story-6360-s03b-package-work-items

Story #6360 · S03b Package Work Items · Epic #6510 (Modules: package the bounded contexts)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
- Brief: [`slices/slice-04-s03b-package-workitems-sync.md`](../epic-6264-modular-monolith/slices/slice-04-s03b-package-workitems-sync.md)
- ADR: [`adr-270-bounded-contexts-context-map-and-module-layout.md`](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
- Prerequisite of Epic #6474: [`epic-6474-one-structure/feature-delta.md`](../epic-6474-one-structure/feature-delta.md),
  section "Plan update: improvement Stories become prerequisites (maintainer, 2026-11-14)". S03b is the second of three
  (S03c #6361 Closed 2026-11-15; S09a #6382 next).
- Previous packaging Story, same shape: [`story-6361-s03c-package-forecasting/feature-delta.md`](../story-6361-s03c-package-forecasting/feature-delta.md).

DISCUSS: covered at Epic level (Wave Decisions D-6264-3, D-6264-4, D-6264-16); waves start at DESIGN.

DESIGN run 2026-11-15. Every decision below is **RECOMMENDED** for the maintainer's DESIGN hold; none is decided.

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: Story Map, How to pick up a Story, Stories → S03a–S03h with the "Notes from S02" list (settled names,
  rules first, edges from the IL, one oracle file per slice, EXAMINE commands up front, mutation bar "no survivor or
  no-coverage mutant added in the moved code", `Program.cs` forces the full CI suite, reuse `ModuleRules`), S07a, S07b,
  S12c · Cross-Cutting Impact Checklist · Outcome KPIs · Improvement log (S02, S05f, S03c lessons).
- ADR-270: Work Items context (Decision 1), the map and "Every edge", Decision 3 (events for reactions, contracts for
  queries), Decision 6 (layout), Decision 8 (Work Items owns the mirrored items), ratchet candidates 1, 2, 4, 7,
  "Gate decision" change 5 (S03b decides the Work Items → Flow metrics edge).
- ADR-268 (ratchet convention: inline file lists, never loosened).
- Epic #6474: its delta, ADR-276 (one `UnitUpdater`, draft in that workspace), slices 04 and 12.
- S03c evolution `docs/evolution/2026-11-15-story-6361-s03c-package-forecasting/evolution.md` (lessons: logger
  categories change with the namespace; a public command for "the triggers still fire"; `IlDependencies` misses
  catch-clause types, events and named attribute arguments; a port can close a DI cycle).

## Wave: DESIGN / [REF] DoR re-run (2026-11-15)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Work Items is spread over `Services/{Interfaces,Implementation}/{WorkItems, TeamData, WorkItemRules, Dependencies, Parents}`, `DomainEvents` handlers, loose files and the update folder; no namespace rule names it whole (`WorkItemsSyncPattern` covers only `WorkItems|TeamData|WorkItemRules`). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files and IL edges below. |
| 4 UAT | PASS (catalogue) | Four AC in the brief, CI-observable; scenarios are written by the acceptance designer in DELIVER. |
| 5 AC from UAT | PASS | AC map to the S03* Elevator Pitch "After". |
| 6 Right-sized | PASS by waiver | Brief says L; DESIGN confirms L (about 8 days, 9 slices, 3 nwdes features). |
| 7 Technical notes | PASS | Re-answered below. |
| 8 Dependencies | PASS | Hard: S01 Closed; S02 gate go-with-changes (2026-11-09). S12c #6388 New, so `WorkItemDto` and every Work Items DTO stays. Soft: S07a #6377 New (split of `WorkItemService` stays OUT, it moves whole); S03a #6359 New; S06 #6376 New; S07b #6378 New; S05f #6374 Closed. |
| 9 Outcome KPIs | PASS | KPIs 1, 2, 3, 12. |

## Wave: DESIGN / [REF] Baseline re-count (2026-11-15, `main` at `1e39e17da`)

Counted with `git ls-files` (tracked files only).

| Measure | Catalogue | Today |
|---|---|---|
| Loose files at the top of `Services/Interfaces` + `Services/Implementation` (KPI 1) | 34 + 44 | **34 + 44** |
| Files under `Services/Interfaces` + `Services/Implementation`, total (KPI 1) | 456 (after S03c) | **456** |
| `Models` files (KPI 3) | 267 | **267** |
| Contexts packaged (KPI 2) | 2 of 11 | **2 of 11** (`FlowMetrics`, `Forecasting`) |
| Core folders | — | `Services/Interfaces/WorkItems` 2, `Services/Implementation/WorkItems` 4, `TeamData` 1 + 1, `WorkItemRules` 2 + 3, `Dependencies` 5 + 10, `Parents` 0 + 2 = **30 files** |
| Work Items handlers in `Services/Implementation/DomainEvents` | — | 5: `WorkItemBlockedTransitionCaptureHandler`, `WorkItemBlockedTransitionCloseHandler`, `FeatureBlockedTransitionCaptureHandler`, `FeatureBlockedTransitionCloseHandler`, `TeamDeletedRefreshLogCleanupHandler` |
| Work Items handler in the update folder | — | 1: `ConnectionFetchShapeChangedUpdateTriggerHandler` |
| Refresh pipelines | — | `TeamUpdater`, `PortfolioUpdater` (`Services/Implementation/BackgroundServices/Update`), `ITeamUpdater`, `IPortfolioUpdater` (`Services/Interfaces/Update`) |
| Work Items → Flow metrics (R3/R4 entries) | 3 | **3 edges** (IL): `TeamDataService → ITeamMetricsService`, `WorkItemService → IPortfolioMetricsService`, `WorkItemService → PercentileCalculator` (Domain) |
| Hypothesis "one folder that depends only on Tracker integration and the kernel; disproved if it pulls in Forecasting or Portfolio types" | — | **Partly disproved** (E2): no Forecasting edge, no Portfolio/Delivery service edge, but Flow metrics (3), Access (1), Licensing (1), the Feature ordering policy (1) and the update queue (1). The kernel entity `Portfolio` is used throughout; it is kernel, not a context edge. |
| S107 suppressions in the moved code | — | unknown — not measured (`WorkItemService` carries one, catalogue S07a) |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. **Membership**: which files move into `Modules/WorkItems/{Domain, Application, Adapters}`, which stay and to which
   Story; KPI before/after. → D-1, D-4.
2. **The Work Items → Flow metrics edge** (ratchet candidate 1, gate change 5; S02 open question 4). → D-2.
3. **The Portfolio refresh calling Promises and Forecasting** (`PortfolioUpdater.cs:101,106,124`). → D-3.
4. **Dependencies, Parents, blocked, `WhatTheForecastWaitsFor`, `SyncModeResolver`, `FetchFingerprint`,
   `BlockedItemService`, rule types, Team data**: where each lands. → D-1.
5. **Seam rules that name sync types**, restated by namespace; R3/R4 changes. → Ratchet design.
6. **Domain events**: stay in the kernel or move. → D-5.
7. **Registration order, unit-test placement, behaviour-free proof, mutation bar.** → D-8 to D-11.
8. **Size and split** into nwdes features. → Slices.
9. **6474 seams**: the Work Items seams 6474 slices 04 and 12 write into, and no shape 6474 reverses. → D-6.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies unchanged; existing API, E2E and response tests green unmodified, apart from tests that move with their code. Refresh results identical on demo data.
- [ ] **Ratchet in the same change**: namespace rules land in the commit range that makes them pass; inline lists; no rule loosened.
- [ ] **Moves separate from edits**: `refactor(work-items): move …` commits apart from edit commits.
- [ ] **Re-count before designing**: done above.
- [ ] **Mutation last, on frozen code**: no survivor or no-coverage mutant added in the moved code (maintainer-confirmed bar, 2026-11-14).
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4 Work Items row, anchors, diagram; ADR-270 where a decision changes.
- [ ] **One context at a time**: old and new layouts coexist until Work Items is fully moved.
- [ ] **Terminology**: `Modules/WorkItems` (ADR-270 glossary).
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered, no silent N/A.
- [ ] **Rules first**: every rule keyed on Work Items' namespaces, folders, paths or type names widened before any type moves, proven with a made-up type under `Modules`.
- [ ] **Configuration and strings checked for namespace and full type names** (log categories; S03c lesson: search full type names in strings, not only `using` lines).
- [ ] **`Program.cs` edits**: the push runs the full CI backend suite, live tracker tests included.
- [ ] **No shape that Epic #6474 reverses** (D-6).
- [ ] **The connector leaf rule holds**: no connector reaches `Modules.WorkItems` (D-1, the selectors stay).

## Wave: DESIGN / [REF] Context-map placement (2026-11-15)

- **Context touched**: Work Items (packaged from today's code into `Modules/WorkItems`).
- **Neighbours whose edge becomes visible** (no behaviour change): Tracker integration (connector port; the parent and
  dependency selectors the connectors share with the refresh), Flow metrics (D-2), Forecasting (reads
  `IWhatTheForecastWaitsFor`, the rule evaluator), Promises (Delivery rules use the rule evaluator and
  `IBlockedItemService`), Refinement (`StageRuleMatcher` uses the rule evaluator), Access (`FeatureDependencyRendering`
  → `IRbacAdministrationService`), Licensing (`DependencyDecision` → `ILicenseService`), Organisation set-up (Feature
  ordering policy, `FetchFingerprint` read by the connection save), the update queue (platform lane).
- **New terms / concepts**: none. Folder `Modules/WorkItems` is the ADR-270 glossary name.
- **Domain events**: publishes `TeamDataRefreshed`, `WorkItemTransitioned`, `WorkItemBlocked`, `WorkItemUnblocked`,
  `WorkItemBecameStale`, `FeatureBlocked`, `FeatureUnblocked` (all stay in `Models/Events`, D-5). Reacts to
  `ConnectionFetchShapeChanged`, `TeamDeleted`, and its own blocked events (transition capture/close handlers). With
  D-2 a, Flow metrics gains one reaction to `TeamDataRefreshed`.
- **UI surface**: none.
- **ADR-270 / `ARCHITECTURE.md` §4 edits**: "SSOT edits pending" below; written in the finalize checklist.
- **Improvement backlog check**: S03b is itself a #6510 item, run as its own improvement item; N/A, stated.

## Wave: DESIGN / [REF] Evidence from the design-time measurements (2026-11-15, `main` at `1e39e17da`)

Edges come from the compiled IL of a fresh Debug build of `Lighthouse.dll` (`dotnet build -c Debug`, 0 warnings),
read with the same walk as `IlDependencies` (base types, interfaces, fields, properties, signatures, locals, every IL
operand, attributes; compiler-made nested types folded into their writer; generic arguments unwrapped; generic
parameters skipped). The scanner is a throw-away console in the session scratchpad, not committed: it prints every
type-to-type edge inside the assembly (6,156 edges), and a small script groups them by namespace. Text searches were
used only for paths and strings. The same IL-reader gap applies (catch clauses, events, named attribute arguments).

Core set measured: namespaces `Services.(Implementation|Interfaces).(WorkItems|TeamData|WorkItemRules|Dependencies|Parents)`.

| # | Finding | Consequence |
|---|---|---|
| E1 | **Work Items → Flow metrics, 3 edges**: `TeamDataService → ITeamMetricsService` (`TeamDataService.cs:23`, `UpdateTeamMetrics` after `UpdateWorkItemsForTeam`, before `TeamDataRefreshed` is published), `WorkItemService → IPortfolioMetricsService`, `WorkItemService → PercentileCalculator` (Flow metrics Domain). `TeamMetricsService.UpdateTeamMetrics` saves through `IWorkItemRepository.Save()` (`TeamMetricsService.cs:868-875`). | D-2. |
| E2 | Other outbound edges of the core set, beyond the kernel and the repositories: `WorkItemService` → `IWorkTrackingConnectorFactory`, `IWorkTrackingConnector` (Tracker integration, legal), `IFeatureOrdering` (Organisation set-up), `ILighthouseClock` (platform), `UpdateCancellationContext` (update queue), `IDomainEventDispatcher`; `FeatureDependencyRendering → IRbacAdministrationService` (Access); `DependencyDecision → ILicenseService` (Licensing, ratchet candidate 2 family); `FeatureDependencyRendering → IFeatureListReader`. **No edge to Forecasting or Promises.** | Outbound list of the new module (R-W6), each entry named. |
| E3 | **Connectors reach three core types**: `DependencySourceSelector` (ADO, CSV, Jira, Linear connectors), `DependencyFieldReferences` (CSV), `ParentSourceSelector` (ADO, Jira). Nothing else outside reaches `DependencySourceSelector`/`DependencyFieldReferences`; `WorkItemService` also calls `ParentSourceSelector`. `ParentSourceSelector`'s doc comment says it sits outside the work item services because a connector must call it, and `WorkTrackingIntegration_IsALeafInputAdapter` forbids connectors → `WorkItemsSyncPattern`. | The three **stay** (D-1): moving them into `Modules/WorkItems` breaks the leaf rule once it is widened. Owner: Tracker integration (S03a), which applies the mapping. |
| E4 | **Inbound to the core set** (IL): `API` (13 edges: `IBlockedItemService` from 9 controllers and helpers, `DependenciesAsRead`, `FeatureDependencyRendering`, `IDependencyDecision`), `API.DTO.DependenciesAsReadExtensions`, `API.Helpers` (`RuleSetValidation`, `WorkTrackingSystemOptionsOwnerExtensions → FetchFingerprint`), Flow metrics policies `BlockedCountSnapshotRecordingHandler`, `DemoBlockedHistoryBackfillHandler` (→ `IBlockedItemService`), Forecasting `ForecastFilterRuleService` (→ `IRuleEvaluator`, `IRuleFieldProvider`), `ForecastService` (→ `IWhatTheForecastWaitsFor`), `DeliveryRuleService` (rule evaluator, `FeatureFieldProvider`), `FeatureListService` and `Services.Interfaces.FeatureListRead` (`DependenciesAsRead`, `FeatureDependencyRendering`), Refinement `StageRuleMatcher` (rule evaluator), `ConnectionSaveService → FetchFingerprint`, `TeamUpdater → ITeamDataService`, `PortfolioUpdater → IWorkItemService`, connectors (E3), `Program`. | Flow metrics, Forecasting, Promises and Refinement depend on Work Items in ADR-270: legal by namespace. `ConnectionSaveService` (Tracker integration / Organisation set-up) and `FeatureListService` (unassigned loose file) go on the inbound list. |
| E5 | `Models.WorkItemRules` is reached by `Models.Refinement` (kernel), connectors (`Factories`), Forecasting, Promises, API DTOs. `Models.Dependencies`, `Models.Parent*`, `SyncMode`, `SyncOutcome`, `RemoteRecordStamp`, `RefreshFinding`, `Models.Staleness` are reached by core types; who else reaches each: **unknown — not measured per type**. The entities (`WorkItem`, `Feature`, `WorkItemBase`, `FeatureWork`, both transition tables, both blocked-transition tables, `RefreshLog`, `FeatureDependencyReference`) are in the EF model. `ParentResolution`, `ParentCandidate`, `ParentPlacement`, `StoredFeatures` are pinned as kernel types by `ParentPlacementKernel_DependsOnNoServiceOrData`. | D-1: **no `Models` type moves** in S03b (same reasoning as S03c E3/E4: a kernel type may not reach a module; entities and DTO-reached types change the EF model or wait for S12c). A per-type follow-up list is S04b's. The brief's "context-owned `Models` types have moved" AC is corrected (D-7). |
| E6 | `TeamUpdater`/`PortfolioUpdater` outbound (IL, grouped): Promises (`Services.Interfaces.DeliverySources`, `IDeliveryRuleService` in `Services.Interfaces`, both resolved by the service locator, `PortfolioUpdater.cs:93-106`), Forecasting `IForecastUpdater` (`PortfolioUpdater.cs:24`, trigger at line 124), Licensing (2), WriteBack (`Models.WriteBack`), Encryption (2), the update queue, Work Items (`ITeamDataService`, `IWorkItemService`). Inbound to `ITeamUpdater`/`IPortfolioUpdater`: `API` (6), Forecasting Application (1), the update folder (4), `Program` (2). | D-3, D-4: moving the updaters into `Modules/WorkItems` would make Work Items reach Promises and Forecasting, both downstream of it: two cycles to list. They stay for 6474 slice 12. |
| E7 | Rule files keyed on Work Items' namespaces, paths or type names (text search over `Lighthouse.Backend.Tests/Architecture`): **26 files** (list in "Ratchet design / Rules first"). `appsettings*`, scripts, workflows, Stryker configs: **unknown — not measured**. | Rules first (feature A). |
| E8 | `ParentSourceSelector`, `DependencySourceSelector`, `DependencyFieldReferences` move nowhere; the Parents folder keeps 1 file, Dependencies keeps 2. | KPI 1 counts them as left, with their owner named. |
| E9 | `RefreshLogService`/`IRefreshLogService`, `OrphanedFeatureCleanupService`/`IOrphanedFeatureCleanupService`, `RefreshRoundSummary`, `RefreshRoundSummaryLine`, `FeatureListService`, `FeatureListActiveWork`, `EstimateNormalizer`, `InProgressFeatureResolution`, `LostRaceTolerantSave`: edges **unknown — not measured**. | D-1 marks the first four as move candidates (ADR-270: the refresh log is Work Items'), confirmed from the IL in slice A2 before B moves anything; the others stay unassigned. |

### Hypothesis measurement ("edits beyond namespaces in a handful of files")

| Edit kind | Files | Note |
|---|---|---|
| Moved production files | **33 confirmed** (27 core + 6 handlers) + up to **4 candidates** (E9) | namespace line plus `using`s |
| Production files outside the module needing only a `using` | about **25**: `Program.cs`, about 13 in `API`/`API.DTO`/`API.Helpers`, 2 Flow metrics policies, 2 Forecasting types, `DeliveryRuleService`, `FeatureListService`, `FeatureListRead`, `StageRuleMatcher`, `ConnectionSaveService`, the 2 updaters; the 3 selectors and the connectors need none (they stay) | counted from E4; exact list from `dotnet build` |
| Rule, path and config files | **26 test files** (E7) + configs unknown | the ratchet owns them |
| Production edits that are not a `using`, for the move | **0 expected**; confirmed by `dotnet build` in each move slice | |
| Production edits for the edge (D-2 a) | **3 to 4**: `TeamDataService` (drop the call and the dependency), one new handler in `Modules/FlowMetrics/Application/Policies`, `Program.cs` registration line; `DomainEventHandlerOrderPinTest` gains a pin | its own nwdes feature |
| Behavioural assertions edited | **0 expected** | pass criterion |

**Reading.** The packaging hypothesis holds for the move: every production edit outside the moved set is a `using`.
The context does not depend "only on Tracker integration and the kernel" (E2): 6 named outbound edges remain, each
listed. The Flow metrics edge is a deliberate edit, shipped as its own feature.

## Wave: DESIGN / [REF] Decisions

Status: **all DECIDED as recommended (maintainer, 2026-11-15)**: D-2 a, D-4 a and every other recommendation. The
headings keep the word RECOMMENDED; read it as decided. Option tables are the record of what was weighed.

### D-0 No schema change — RECOMMENDED

No entity, migration or `DbSet` changes. `UpdateTime` and `FetchFingerprint` on the unit row stay (6474 records them
as Work Items' data left in place). `DefaultSizeGuess` is 6474 slice 10's.

### D-1 Membership — RECOMMENDED

"Move" = pure-move commit to `Lighthouse.Backend.Modules.WorkItems.<Layer>`.

| Candidate | Decision | Why |
|---|---|---|
| `WorkItemService`, `IWorkItemService` | **Move whole**, `Application` | ADR-270: refresh, sync mode, transitions, roll-up are Work Items'. S07a (New) splits it later in place; the split is OUT. |
| `TeamDataService`, `ITeamDataService` | **Move**, `Application` | The Team refresh use case; publishes `TeamDataRefreshed`. |
| `BlockedItemService`, `IBlockedItemService` | **Move**, `Application` | Blocked is Work Items' (ADR-270). Many readers (E4), all legal downstreams or `API`. |
| `SyncModeResolver`, `FetchFingerprint` | **Move**, layer by the Domain rule in B1 | Sync mode and the fetch fingerprint are sync's. `FetchFingerprint` gains 2 inbound entries (`ConnectionSaveService`, `API.Helpers` extension). |
| `RuleEvaluator<T>`, `IRuleEvaluator<T>`, `IRuleFieldProvider<T>`, `WorkItemFieldProvider`, `FeatureFieldProvider` | **Move**, `Application` (evaluator may be `Domain`, B1 decides) | Rules over item fields. Callers in Forecasting, Promises, Refinement are downstream of Work Items: legal. Alternative "shared kernel": rejected, it is behaviour, not an id or value object (Decision 7). |
| `Models/WorkItemRules/*` (8) | **Stay** in `Models` | E5: reached by `Models.Refinement` (kernel) and by connectors. |
| `Dependencies/*` except the two selectors (13 files: `DependencyDecision`, `DependencyHonourPolicy`, `DependencyReconciler`, `DependencyRefreshReporter`, `DependencyCycleDetector`, `DependencyFacts`, `FeatureDependencyRendering`, `WhatTheForecastWaitsFor`, 5 interfaces) | **Move**; policies and detector to `Domain` if B1's rule admits them, the rest `Application` | Dependencies are Work Items' facts; S03c left `WhatTheForecastWaitsFor` here. `DependencySingleDecisionArchUnitTest` keeps "one decision, one folder", restated by namespace. |
| `DependencySourceSelector`, `DependencyFieldReferences`, `ParentSourceSelector` | **Stay** (Tracker integration, S03a) | E3: connectors call them; the leaf rule forbids connectors → Work Items. They apply the mapping during a fetch: ADR-270 gives translation to Tracker integration. |
| `ParentPlacementReport` (+ `RecordPlacement`) | **Move**, `Application` | Only `WorkItemService` reaches it; parent links are Work Items'. |
| 4 blocked-transition capture/close handlers, `TeamDeletedRefreshLogCleanupHandler`, `ConnectionFetchShapeChangedUpdateTriggerHandler` | **Move**, `Application/Policies` | "Whenever X, then Work Items does Y". Registration order unchanged (D-8). |
| `RefreshLogService`, `IRefreshLogService`, `OrphanedFeatureCleanupService`, `IOrphanedFeatureCleanupService` | **Move if A2's IL check finds no new cycle**, `Application` | ADR-270: the refresh log and the Portfolio refresh steps are Work Items'. Edges unknown (E9). |
| `TeamUpdater`, `PortfolioUpdater`, `ITeamUpdater`, `IPortfolioUpdater` | **Stay** in the update folder (D-4) | E6: they reach Promises and Forecasting; 6474 slice 12 replaces them with `UnitUpdater`. |
| `Models/*` Work Items types (entities, `Parent*`, `SyncMode`, `SyncOutcome`, `Dependencies`, `Staleness`, …) | **Stay** | E5; S04b, S12c, S13. |
| Work Items DTOs (`WorkItemDto`, …) | **Stay** | Hard dependency S12c (New). |
| Controllers | **Stay** in `API` | As S02/S03c: S12a/S12c. |
| Domain events | **Stay** in `Models/Events` | D-5. |

Totals: **33 files move** (+ up to 4, E9). KPI 1 total **456 → 423** (419 with the candidates); top-level loose files
**34 + 44** unchanged (32 + 42 with the candidates). KPI 2: **3 of 11**. KPI 3: 267 unchanged.

### D-2 The Work Items → Flow metrics edge — RECOMMENDED: a, the Team metrics update becomes a Flow metrics reaction to `TeamDataRefreshed`; the two `WorkItemService` query edges stay ratcheted

| Option | What | Cost | Risk |
|---|---|---|---|
| **a (recommended)** Event | Drop `teamMetricsService.UpdateTeamMetrics(team)` from `TeamDataService`; add `TeamDataRefreshedTeamMetricsUpdateHandler` in `Modules/FlowMetrics/Application/Policies`, registered **first** among `TeamDataRefreshed` handlers, so it runs where the inline call ran relative to the other reactions. The Portfolio side already works this way (ADR-270 candidate 1). | about 1 day, 3–4 files, feature C | medium: the dispatcher runs after commit; how it treats a handler failure, and whether the forecast trigger and snapshot handlers relied on the metrics already being updated, is **unknown — not measured**. C1 first pins the event order and the failure behaviour with an oracle, then makes the change. If C1 shows a difference, fall back to e. |
| b Port owned by Work Items | `ITeamRefreshFollowUp` in Work Items, implemented by Flow metrics | similar | Rejected: it is a command ("update your metrics") across contexts, which ADR-270 Decision 3 sends through events. |
| c Move `UpdateTeamMetrics` into Work Items | — | — | Rejected: the metrics calculation is Flow metrics' (Decision 1). |
| e Ratchet only | Keep the call; rename the R4 entry | 0 days | Leaves candidate 1 open; the gate asked S03b to decide. Fallback. |

The two `WorkItemService` edges (`IPortfolioMetricsService`, `PercentileCalculator`) are **queries** used by the
default-size guess (to verify in C1): Work Items asking Flow metrics, its downstream, is a cycle. They stay as named
entries (R3: `PercentileCalculator`; R4: `WorkItemService`), because 6474 slice 10 builds `DefaultSizeGuess` and S07a
splits `WorkItemService`; either is the natural place to remove them. **S02 open question 4** (`UpdateTeamMetrics`
writes through `IWorkItemRepository.Save`): with a, it remains a Flow metrics write through Work Items' repository;
recommended to stay with **S07b**, recorded in the Flow metrics outbound list as prose, not fixed here.

### D-3 The Portfolio refresh calling Promises and Forecasting — RECOMMENDED: leave it to Epic 6474 slice 12

| Option | What | Risk |
|---|---|---|
| **a (recommended)** Leave | `PortfolioUpdater` stays in the update folder; lines 101, 106, 124 unchanged; ratchet candidate 4 stays the one listed caller of Forecasting's Application | none for S03b. ADR-276 Decision 5 already decides the forecast trigger's move to a `PortfolioFeaturesRefreshed` handler, conditional on slice 12b's event-order test; doing it now fixes a shape 6474 owns. |
| b Turn the three calls into handlers of `PortfolioFeaturesRefreshed` now | about 1.5 days, Promises and Forecasting handlers | Rejected: not behaviour-free by construction (order between handlers of one event is not defined, ADR-270 Decision 3); duplicates ADR-276's test; 6474 rewrites the updater anyway. |

### D-4 The refresh pipelines stay where they are — RECOMMENDED

ADR-270 gives the Team and Portfolio refresh pipelines to Work Items. S03b does not move them: E6 shows that inside
`Modules/WorkItems` they would make Work Items reach Promises and Forecasting (two cycles), and 6474 slice 12 replaces
both with `UnitUpdater`. Recommendation to 6474 (D-6): create `UnitUpdater`, its steps and `UnitRefreshEligibility`
in `Modules/WorkItems/Adapters/Background` and `Modules/WorkItems/Application`, not in
`Services/Implementation/BackgroundServices/Update/` as its component table says, and do the cross-context steps as
events there. Alternative "move them now and list 3+ edges": rejected, it lists cycles that slice 12 removes a few
weeks later.

### D-5 Domain events stay in the kernel — RECOMMENDED

As S03c kept `PortfolioForecastsUpdated`: every event type still sits in `Models/Events`; ADR-270 moves each with its
publisher, but `Models` may not reach a module and several kernel/other-context types name these events. S04b moves
them together.

### D-6 The seams Epic 6474 writes into — RECOMMENDED (named, not built)

| 6474 slice | Seam after S03b | Note |
|---|---|---|
| 04 (a unit reads its subset of types) | The fetch shape inside `Modules.WorkItems.Application.WorkItemService` (`FetchShape` nested type, `IWorkItemQueryOwner`) and `FetchFingerprint` (unchanged fingerprints are 04's disproof test) | The type list itself is Organisation set-up's; Work Items reads the effective types. No port is invented now. |
| 12 (one refresh per unit) | `ITeamDataService`, `IWorkItemService` in `Modules.WorkItems.Application`, called by the updaters; `UnitUpdater` recommended in `Modules/WorkItems` (D-4) | ADR-276's facades `ITeamUpdater`/`IPortfolioUpdater` stay in the update folder. |
| 10 (`DefaultSizeGuess`) | `WorkItemService`'s default-size guess and its two Flow metrics queries (D-2) | extension keyed by unit id lives in Work Items. |
| 03 (`UnitDeleted`) | `TeamDeletedRefreshLogCleanupHandler` in `Application/Policies` re-subscribes in place | no shape reversed. |
| 08/15 (`LevelItemTypesChanged`, `LevelSwitched*`) | handlers next to `ConnectionFetchShapeChangedUpdateTriggerHandler` in `Application/Policies` | same pattern. |

### D-7 Brief correction — RECOMMENDED

AC "Context-owned `Models` types have moved" cannot hold before S12c/S04b (E5); rewrite it at close-out as "no
`Models` type moves; the list of Work Items-owned `Models` types is handed to S04b".

### D-8 Registration order — RECOMMENDED

`Program.cs` lines stay; only `using`s change. `DomainEventHandlerOrderPinTest` stays green unmodified in A and B; in C,
D-2 a adds one pin (the metrics handler first among `TeamDataRefreshed` handlers, proven to fail on a swapped
registration). No `AddWorkItems` extension (S09a).

### D-9 Unit-test placement — RECOMMENDED

As S03c: feature B adds only `using` lines to tests (waiver of "improve the tests you touch" for using-only edits, as
S03c recorded); C3 moves the unit tests to `Lighthouse.Backend.Tests/Modules/WorkItems/{Domain, Application,
Application/Policies}` and adds `WorkItemsUnitTestPlacementTest`. Integration tests under `API/Integration` stay.
Number of test files: **unknown — not measured**.

### D-10 Behaviour-free proof and EXAMINE — RECOMMENDED

1. Existing tests green unmodified, apart from moved tests, `using`s and the rule files.
2. **Refresh identity at the port**: A3's oracle refreshes a CSV-backed Team and Portfolio through
   `ITeamDataService`/`IWorkItemService` (`ServicePortHarness`) and compares items, transitions, blocked periods,
   parent placements and the published events with values written out in the test.
3. **`refresh-answers-compare`** (new, from `metrics-answers-compare`): on demo data, force a refresh of every Team
   and Portfolio, then capture the bodies of the Team/Portfolio Work Item, Feature list, dependency and blocked
   endpoints and `diff` them before and after. Declared in `.nwave/examine.json` before the first EXAMINE.
4. **`refresh-reactions-show`** (S03c lesson): a public command that shows the moved policies and, after D-2, the
   metrics reaction fire on a refresh.
5. Reuse `architecture-rules`, `ef-model-unchanged`, `metrics-answers-compare` (D-2 touches the metrics path),
   `forecast-answers-compare` (Forecasting reads the dependency port), `backend-tests`.
6. Demo data for EXAMINE; the maintainer runs one comparison on the production-like database by hand (as S03c).

### D-11 Mutation bar — RECOMMENDED

Last, on frozen code: "no survivor or no-coverage mutant added in the moved code" (confirmed 2026-11-14), whole-file
`mutate` entries, same filter old and new paths (`Tests.Modules.WorkItems` named). D-2's new handler at 80 % or more.

## Wave: DESIGN / [REF] Slices

Every slice is a day or less, has its own oracle file, and keeps move commits apart from edit commits. Three nwdes
features, in this order. ADR, `ARCHITECTURE.md`, brief and catalogue edits and the mutation run are in the finalize
checklist. Size: **L, about 8 days** (with D-2 e: about 7).

| Id | nwdes feature | Delivers, in plain words | Oracle file (new, one per slice) | Size |
|---|---|---|---|---|
| A1 | A `story-6360-s03b-a-rules-first` | Every rule that names Work Items' namespaces, folders, paths or types also sees the future `Modules/WorkItems`; a made-up type there is caught by each | `Architecture/WorkItemsRulesSeeTheModuleArchUnitTest.cs` | 1 d |
| A2 | A | The four unmeasured candidates (E9) and the per-type kernel reach (E5) are measured from the IL; the membership list is fixed in a test that fails when a listed type is missing | `Architecture/WorkItemsMembershipArchUnitTest.cs` | 0.5 d |
| A3 | A | Safety net: a refresh at the service port gives the same items, transitions, blocks and events; public commands capture refresh answers and show the reactions firing on demo data | `Tests/Acceptance/WorkItems/RefreshesStayTheSameTest.cs` + `Scripts/examine/bin/refresh-answers-compare`, `refresh-reactions-show` | 1 d |
| B1 | B `story-6360-s03b-b-move` | Sync mode, fetch fingerprint, the rule evaluator and the dependency policies live in `Modules/WorkItems/Domain`, which reaches only the kernel (rejects go to Application, recorded) | `Architecture/WorkItemsDomainLayoutArchUnitTest.cs` | 0.5 d |
| B2 | B | The Work Item, Team data, blocked, dependency and parent-placement services and their ports live in `Modules/WorkItems/Application`; Flow metrics' lists name them there | `Architecture/WorkItemsApplicationLayoutArchUnitTest.cs` | 1 d |
| B3 | B | The six Work Items reactions live in `Application/Policies` (plus the refresh log and orphan clean-up if A2 admitted them); handler order unchanged | `Architecture/WorkItemsPoliciesLayoutArchUnitTest.cs` | 0.5 d |
| C1 | C `story-6360-s03b-c-edge-and-rules` | Work Items no longer tells Flow metrics to update: Flow metrics updates a Team's metrics when it hears the Team was refreshed; metrics answers and event order unchanged | `Architecture/WorkItemsDoesNotCommandFlowMetricsArchUnitTest.cs` (+ order pin) | 1 d |
| C2 | C | Work Items has its own module rules (Domain, Application in and out, Policies) with inline lists; Flow metrics' R4 list loses `TeamDataService` | `Architecture/WorkItemsModuleBoundariesArchUnitTest.cs` | 1 d |
| C3 | C | Work Items' unit tests sit under `Tests/Modules/WorkItems` and a placement test keeps them there | `Architecture/WorkItemsUnitTestPlacementTest.cs` | 0.5 d |

Finalize (not nwdes slices): mutation on frozen code (D-11); ADR-270, `ARCHITECTURE.md` §4, brief (D-7), catalogue
Improvement log; the Lighthouse finalize checklist.

Why three features: rules only; pure moves; edits. Each under 5 slices; C1 can drop to D-2 e without touching A or B.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list inline, compared both ways (ADR-268), never loosened.

### Rules first (A1): files keyed on Work Items' namespaces, paths or types (E7, text search)

| File | What it keys on | Widening |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | `WorkItemsSyncPattern` = `Services.(Implementation\|Interfaces).(WorkItems\|TeamData\|WorkItemRules)` | add `\|^Lighthouse\.Backend\.Modules\.WorkItems(\.\|$)`; also add `Dependencies` and `Parents`? **No**: the selectors stay there and connectors call them (E3). `NamedModules_AreNonEmpty` gains the module namespaces after B |
| `DependencySingleDecisionArchUnitTest` | 6+ source paths under `Services/Implementation/Dependencies/`, `typeof` | locate files by name under the production source; "one folder" becomes old-or-module folder; the selector path stays |
| `BlockedItemSinglePathArchUnitTest` | `typeof`, `using` | `using` only |
| `CalendarDayAnchorSeamArchUnitTest` | path `Services/Implementation/WorkItems/WorkItemService.cs` (2) | locate by name |
| `FetchShapingPropertyGuardTest` | 8 hits, paths or names | to check in A1 |
| `InstantsUnaffectedByZoneTest` | 12 hits | to check in A1 |
| `TimeInStateSeamArchUnitTest` | 6 hits | to check in A1 |
| `RuleEvaluatorPurityTest` | 9 hits | to check in A1 |
| `ConnectionFetchShapeChangedSinglePublisherArchUnitTest`, `ConnectionSaveArchitectureTest`, `DeliveryRuleServiceApiPreservationTest`, `DomainEventHandlerOrderPinTest`, `UpdateHoldsAreForecastOnlyArchUnitTest`, `UpdateCancellationContextWriterArchUnitTest`, `DependencyAwareForecastSeamArchUnitTest` | `typeof`, short names, update-folder namespace | `using` only, or subject widened like S03c did for the forecast seam; listed so the reviewer sees they were checked |
| `FlowMetricsInboundApplicationArchUnitTest` (R4), `FlowMetricsInboundDomainAndAdaptersArchUnitTest` (R3) | full names `TeamData.TeamDataService`, `WorkItems.WorkItemService` | rewritten in B2 (edit commit); `TeamDataService` leaves R4 in C1 |
| `ForecastingModuleBoundariesArchUnitTest`, `ForecastingRulesSeeTheModuleArchUnitTest`, `ForecastingAdaptersAndPoliciesLayoutArchUnitTest`, `ForecastingUnitTestPlacementTest` | Work Items names in Forecasting's outbound or fixture lists | rewritten in B2 if they name a moved type |
| `KernelRules.cs`, `KernelRulesCatchANewOffenderArchUnitTest` | namespace constants | check whether `Modules` already counts (it does for `ModelsDoNotDependOnServices`) |
| `RepositoryLeakRatchetArchUnitTest`, `PortGivensGuardTest`, `TestHostUseGuardTest.HostUsersStillToMove` | file or type names | names rewritten where a listed test moves (C3) |

Exact hit content of the "to check in A1" rows: **unknown — not measured** (counts only). Configs: unknown.

### New rules (C2), baselines to measure from the IL in C2

| # | Rule | Expected baseline |
|---|---|---|
| R-W1 | Each moved type lives in its layer; none left in `Services` (`ModuleRules.TypesLiveInTheirLayer`) | D-1 lists |
| R-W2 | `Modules.WorkItems.Domain` reaches only `Models` and the BCL | empty |
| R-W3 | Outside → Domain only from the list | `Program` and the types B1 places (unknown) |
| R-W5 | Outside → Application from `API`, the composition root, Flow metrics, Forecasting, Promises, Refinement (legal by namespace) and the list | `ConnectionSaveService`, `FeatureListService`, `FeatureListRead`, `TeamUpdater`, `PortfolioUpdater`, `DeliveryRuleService` (if Promises is not yet a namespace) |
| R-W6 | Application → no EF Core, `Data`, `API`, module `Adapters`; Flow metrics only through the list; Access, Licensing, Organisation set-up, update queue only through the list | Flow metrics 2 (`IPortfolioMetricsService`, `PercentileCalculator`); `IRbacAdministrationService`, `ILicenseService`, `IFeatureOrdering`, `UpdateCancellationContext` (1 each) |
| R3 (Flow metrics) | `WorkItemService` entry renamed | 6 → 6 |
| R4 (Flow metrics) | `TeamDataService` leaves (D-2 a) | 5 → **4** |
| Leaf rule | connectors → `Modules.WorkItems` | empty |

## Wave: DESIGN / [REF] Component decomposition

Root `Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/`; namespace = folder.

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| Domain candidates (`SyncModeResolver`, `FetchFingerprint`, `RuleEvaluator<T>`, `DependencyHonourPolicy`, `DependencyCycleDetector`, `DependencyFacts`) | `Services/Implementation/{WorkItems,WorkItemRules,Dependencies}` | `Domain/` (or `Application/` if R-W2 rejects) | MOVE | B1 |
| `WorkItemService`, `TeamDataService`, `BlockedItemService`, rule ports and field providers, dependency services and ports, `ParentPlacementReport` | same | `Application/` | MOVE | B2 |
| 6 handlers (+ refresh log, orphan clean-up if admitted) | `Services/Implementation/DomainEvents`, `…/BackgroundServices/Update`, loose | `Application/Policies/` (services to `Application/`) | MOVE | B3 |
| `TeamDataRefreshedTeamMetricsUpdateHandler` | — | `Modules/FlowMetrics/Application/Policies/` | NEW (EDIT) | C1 |
| `TeamDataService`, `Program.cs` | | | EDIT | C1 |
| Selectors, updaters, `Models`, DTOs, events, controllers | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: Team, Portfolio, Features, Feature list, metrics, Delivery rule controllers (routes, `RbacGuard`s and
responses untouched). Host: `TeamUpdater`, `PortfolioUpdater` (stay) call `ITeamDataService`, `IWorkItemService`.
Events: `ConnectionFetchShapeChanged`, `TeamDeleted`, `WorkItemBlocked/Unblocked`, `FeatureBlocked/Unblocked` reach
the moved policies in the pinned order. In-process: Forecasting → `IWhatTheForecastWaitsFor`, rule evaluator; Promises,
Refinement → rule evaluator; Flow metrics → `IBlockedItemService`. New (C1): Flow metrics reacts to `TeamDataRefreshed`.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IWorkTrackingConnectorFactory`, `IWorkTrackingConnector` | Tracker integration | unchanged, legal |
| `IWorkItemRepository`, `IFeatureRepository`, transition and blocked repositories, `IPortfolioRepository`, `IFeatureListReader` | Platform repositories | unchanged |
| `IPortfolioMetricsService`, `PercentileCalculator` | Flow metrics | unchanged, listed (D-2) |
| `ITeamMetricsService` | Flow metrics | removed from Work Items in C1 (D-2 a) |
| `IRbacAdministrationService`, `ILicenseService`, `IFeatureOrdering`, `ILighthouseClock`, `IDomainEventDispatcher`, `UpdateCancellationContext` | Access, Licensing, Organisation set-up, platform lane | unchanged, listed |
| No new external call; no new adapter or dependency, so no new probe. | | |

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET and `IlDependencies` over Mono.Cecil, NUnit, Stryker.NET; shell scripts in `Scripts/examine/bin`.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `ModuleRules` + `ModuleWording` | module rules | **REUSE**; no rule copied |
| `ModuleBoundariesArchUnitTest.WorkItemsSyncPattern` | the Work Items pattern | **EXTEND** (widen) |
| `ApplicationCoreRules`, `RepositoryRecognition`, `KernelRules` | `Services` or `Modules` | **REUSE** |
| `DomainEventHandlerOrderPinTest` | handler order | **REUSE**; EXTEND in C1 with one pin |
| `ForecastingUnitTestPlacementTest` / `FlowMetricsUnitTestPlacementTest` | placement | **EXTEND** (parameterise) or sibling, decided in C3 |
| `Scripts/examine/bin/metrics-answers-compare`, `forecast-answers-compare`, `forecast-triggers-show`, `ef-model-unchanged`, `architecture-rules`, `backend-tests` | EXAMINE | **REUSE**; **CREATE** `refresh-answers-compare`, `refresh-reactions-show` from them |
| `ServicePortHarness`, CSV connector demo data | refresh at the port | **REUSE** for A3 |
| Existing `TeamDataRefreshed` handlers in Flow metrics `Application/Policies` (snapshot recording) | where the new handler goes | **REUSE** the folder and pattern; the new handler holds no rule of its own, it calls `UpdateTeamMetrics` |
| `ITeamMetricsService.UpdateTeamMetrics` | the metrics update | **REUSE**; no second update path |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, UI unchanged (D-10). No schema change (D-0). |
| RBAC | N/A, because no endpoint, guard or authorisation call changes; `FeatureDependencyRendering` keeps calling `IRbacAdministrationService`; nothing fetches `my-summary`. |
| Lighthouse-Clients | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing becomes usable or used differently. |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. Code: `Modules/WorkItems`. |
| Log categories | Change with the namespace; config check in A1 (unknown today). Search full type names in strings (S03c lesson). |
| `Program.cs` | Edited in B and C: the push runs the full CI backend suite. |
| Epic 6474 | No shape reversed: updaters and the Portfolio crossings stay for slice 12 (D-3, D-4); seams named (D-6). The "[SUPER] Epic" feedback concerns the connection mapping (ADR-274): the parent and dependency selectors that apply the mapping stay with Tracker integration (D-1), so nothing here fixes their shape. |
| Test-writing skill | A3 at the application-service port under `Tests/Acceptance/WorkItems/`; mutation config names `Tests.Acceptance.WorkItems`. |

## Wave: DESIGN / [REF] Formal verification

Not applicable, with the reason (same reasoning as S03c; the formal-verification skill was not re-read in this run, under the turn limit). S03b's claims are: (1) "the same types exist
in other namespaces", decided exactly by the compiled IL with a made-up offender per rule as falsifier; (2) "a
refresh gives the same items, transitions and events", decided by observation at the service port (A3) and the answer
comparison. Neither is a state-machine or proof obligation: both sides of (2) are the same code, so a property test
over generated refreshes would compare the code with itself and cannot fail. The one temporal claim is D-2 a: "the
metrics update still happens after the items are saved and before the other `TeamDataRefreshed` reactions, and a
failure still has the same effect". The dispatcher is in-process and sequential; the order is pinned by
`DomainEventHandlerOrderPinTest` with a swapped-registration falsifier, and the failure behaviour by C1's oracle.
**No checker (Quint, TLC, Agda) was probed or run.** If the maintainer wants a model of the handler order or the
failure path, that claim is **INDETERMINATE** until a checker is probed; 6474 slice 12 (ADR-276) is the Story that
changes refresh scheduling and owns a state model.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature C.

**ADR-270**, proposed text:

- Decision 1, Work Items row, last column: "**Packaged** in `Modules/WorkItems` by Story 6360 (date). Its entities,
  rule types, parent and dependency value types, events and DTOs still sit in `Models` and `API/DTO`. The Team and
  Portfolio refresh pipelines stay in the update code until Epic 6474 slice 12 replaces them with one `UnitUpdater`.
  The parent and dependency source selectors stay with Tracker integration: the connectors call them."
- "Every edge", row "Flow metrics → Work Items (Work Items calls Flow metrics, beyond the event)": "The Team metrics
  update is a Flow metrics reaction to `TeamDataRefreshed` since Story 6360. Left: `WorkItemService` queries
  `IPortfolioMetricsService` and `PercentileCalculator` for the default-size guess, listed until Epic 6474 slice 10
  or S07a removes them | **Not legal**, list of 2".
- Ratchet candidate 1: "*Done (Story 6360):* `TeamDataService` no longer calls `UpdateTeamMetrics`; it left the R4
  list. The write of `UpdateTeamMetrics` through `IWorkItemRepository.Save` stays with S07b."
- Ratchet candidate 4: "*Story 6360:* left to Epic 6474 slice 12 (ADR-276 Decision 5)."
- New block "Edges measured by packaging Work Items (date)" with R-W1 to R-W6 and the new R3/R4 counts.
- Decision 6: Work Items folder counts.
- Gate change 5: "*Done for Work Items (Story 6360).*"

**ARCHITECTURE.md** §4: Work Items row anchors become `Modules.WorkItems.*`; the Work Items → Metrics arrow becomes an
event; enforced-rules list gains R-W1 to R-W6.

**brief.md**: new section `## Application Architecture — story-6360 s03b package work items` with the layout, D-2 and
the 6474 seams, without touching earlier sections.

**Epic catalogue**: Improvement log entry; slice-04 brief AC corrected (D-7); S03* notes: S03b done; the 6474 delta
gets a note that `UnitUpdater` is recommended in `Modules/WorkItems` (D-4).

## Wave: DESIGN / [REF] Open questions

1. **D-2**: event (a) or ratchet only (e)? Owner: maintainer.
2. **D-4**: agree that the updaters stay, and that 6474 slice 12 builds `UnitUpdater` in `Modules/WorkItems`? Owner:
   maintainer (6474 DESIGN amendment).
3. **D-1 candidates** (E9): refresh log and orphan clean-up services; owner: A2 (IL), recorded in the oracle.
4. **S02 question 4** (`UpdateTeamMetrics` writes through `IWorkItemRepository.Save`): S07b, recommended. Owner:
   maintainer.
5. **D-7**: brief AC on `Models` types rewritten. Owner: maintainer at close-out.
6. **Size**: L, about 8 days, 9 slices. Owner: maintainer.
7. Later, not this Story: the selectors to Tracker integration (S03a); Work Items `Models` types (S04b); DTOs (S12c);
   `WorkItemService` split (S07a); its two Flow metrics queries (6474 slice 10 or S07a).

## Wave: DESIGN / [REF] Maintainer decisions at hold 1 (2026-11-15)

- **D-2:** option a. Flow metrics updates a Team's metrics in a new policy that reacts to `TeamDataRefreshed`;
  `TeamDataService` stops calling it. Ratchet only is not taken.
- **D-4:** option a. `TeamUpdater` and `PortfolioUpdater` stay in the update code. Epic 6474 slice 12 builds
  `UnitUpdater` in `Modules/WorkItems`; this is an amendment to 6474's DESIGN, recorded there at S03b's close-out.
- **S02 open question 4** (`UpdateTeamMetrics` writes through `IWorkItemRepository.Save`): goes to S07b.
- **D-7:** the brief's acceptance criterion on `Models` types is rewritten at close-out.
- **Size:** L, all 9 slices, 3 nwdes features.
- All other decisions (D-0, D-1, D-3, D-5, D-6, D-8 to D-11) as recommended.

## Wave: DESIGN / [REF] Architecture summary

A learning aid for the maintainer, not a replacement for `brief.md` or the ADRs.

**1. The shape in hexagonal terms (N = new, M = moved, U = unchanged).**

- Driving adapters: the Team, Portfolio, Features and Feature list controllers (U, in `API`); `TeamUpdater`,
  `PortfolioUpdater` (U, stay in the update code for 6474); the six event policies (M, `Application/Policies`).
- Application: `WorkItemService` (whole), `TeamDataService`, `BlockedItemService`, dependency decision, reconciler,
  reporter, rendering and `WhatTheForecastWaitsFor`, parent placement, rule evaluator and field providers (M).
- Domain: sync mode, fetch fingerprint, the dependency policies and cycle detector (M, if the Domain rule admits them);
  entities, rule types, events and DTOs (U, `Models`).
- Driven: connector port (U, legal); repositories (U); Flow metrics' two query ports (U, listed); Access, Licensing,
  ordering policy, update queue (U, listed). No new external call.
- In Flow metrics: one new policy (N) reacts to `TeamDataRefreshed` and updates the Team's metrics.

```text
 TODAY                                        AFTER S03b
 TeamUpdater ──> TeamDataService              TeamUpdater (U) ──> Modules/WorkItems/Application
                  ├─ UpdateWorkItemsForTeam                          TeamDataService ─ publishes ─┐
                  ├─ UpdateTeamMetrics ──> Flow metrics (command)     WorkItemService             │ TeamDataRefreshed
                  └─ publish TeamDataRefreshed                        Policies (6)                v
 Services/…/{WorkItems,TeamData,WorkItemRules,    Domain (pure)     Modules/FlowMetrics/Application/Policies
   Dependencies,Parents}, DomainEvents handlers                       TeamMetricsUpdate (N) ──> UpdateTeamMetrics
 Connectors ──> Parent/Dependency selectors     Connectors (U) ──> selectors (U, stay: Tracker integration)
 PortfolioUpdater ──> Promises, Forecasting     PortfolioUpdater (U) ──> Promises, Forecasting (left to 6474 slice 12)
```

**2. Patterns weighed.**

- Domain event for the metrics update: **chosen** (D-2 a); ADR-270 sends commands across contexts through events, and
  the Portfolio side already works this way.
- Port owned by Work Items for the metrics update: **rejected**; still a command, only renamed.
- Ratchet only: **fallback**; cheapest, leaves candidate 1 open.
- Events for the Portfolio refresh's Promises and Forecasting steps now: **rejected**; ADR-276 owns that change with an
  event-order test, and handler order inside one event is not defined.
- Move the updaters into the module: **rejected**; two cycles listed for weeks until `UnitUpdater` replaces them.
- Move the selectors into the module: **rejected**; connectors are a leaf and may not reach Work Items.
- Shared kernel for the rule evaluator: **rejected**; it is behaviour, not an id or value object.
- Move whole versus split `WorkItemService`: **move whole**; the split is S07a's, and moving first does not change it.
- Namespace ratchet from the IL with inline two-way lists, shared `ModuleRules`: **chosen**, as S02 and S03c.
- Entity, event and DTO relocation: **deferred** (S04b, S12c, S13); migration snapshots carry entity names.
- DI extension `AddWorkItems`: **deferred** (S09a); handler order is behaviour.
- Not needed: outbox, unit of work, value objects, optimistic concurrency, strategy (no data or variation changes).

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-15)

- **Platform risk: none material.** Nothing is deployed differently: no new env var, chart value, credential,
  workflow, container or migration (D-0). Standalone, Docker and Helm builds are unchanged.
- **CI:** no workflow change. Features B and C edit `Program.cs` (`using` lines; C1 adds one handler registration),
  so their pushes run the full CI backend suite, live tracker tests included. A red live-tracker test there is
  checked against the environmental signatures in CLAUDE.md before it is called a regression.
- **CI test selection (found at DEVOPS, measured 2026-11-15):** `Scripts/test-selection/path-classifier.sh` line 13
  selects the Jira tests by path, and one of its paths is
  `Services/Implementation/WorkItems/FetchFingerprint.cs`. `Scripts/test-selection/test-path-classifier.sh` lines
  135 to 146 pin that path. The slice that moves `FetchFingerprint` (B1) also updates both files to the new path,
  or the Jira tests stop running when the fingerprint changes. The `Parents/` entries stay valid: the parent
  selector stays (D-1).
- **Observability:** log categories change with the namespace (`Lighthouse.Backend.Services.Implementation.WorkItems`
  and the other moved folders → `Lighthouse.Backend.Modules.WorkItems.*`). No appsettings, workflow or Stryker config
  filters on the old names (checked 2026-11-15 with `git grep` over json, yml, ps1, sh, props and config files; the
  only hits are the test-selection scripts above). Moving a type renames its `ILogger<T>` category, so the move
  slices grep full type names in strings too. That is a log-category change, not user-facing behaviour; no release
  note.
- **Rollback:** revert the commit range; no data to undo.
- **Usage-data event: N/A, because the Story is behaviour-free.** It adds nothing a user can use, so there is nothing
  to count. No `UsageDataEventName` entry, no `docs/settings/usagedata.md` line.

## Wave: DELIVER / [REF] Start of DELIVER (maintainer, 2026-11-15)

- **UI sketches: N/A, because nothing a user sees changes** (behaviour-free; routes, DTOs and pixels unchanged).
- **Architecture review owed: none**; DESIGN was reviewed by the maintainer at hold 1.
- **AFK options:** slice wrap-up = refactor + review; push per slice with CI watched; no 5-minute review window
  (improvement work); rebase onto `origin/main` allowed for this session.
- **Rigor profile:**

| nwdes feature | Slices | Profile | Crafter, acceptance designer | Reviewers, EXAMINE |
|---|---|---|---|---|
| A rules first | A1–A3 | `thorough` | opus | sonnet |
| B the move | B1–B3 | `standard` | sonnet | haiku |
| C edge and rules | C1–C3 | `thorough` | opus | sonnet |
