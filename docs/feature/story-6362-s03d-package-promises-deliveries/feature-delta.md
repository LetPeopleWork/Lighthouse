# Feature: story-6362-s03d-package-promises-deliveries

Story #6362 · S03d Package Promises (Deliveries) · Epic #6510 (Modules: package the bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03d entries, D-6264-16, "Notes from S02")
- Brief: [`slices/slice-06-s03d-package-portfolio-delivery.md`](../epic-6264-modular-monolith/slices/slice-06-s03d-package-portfolio-delivery.md)
  (both the body and the "Update after S01" block read: retitled Promises; Portfolios go to S03i)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) (Promises row,
  "Every edge", ratchet candidates 3 and 4, glossary, layout, "Left open"),
  [ADR-271](../../product/architecture/adr-271-ui-surfaces-follow-units-and-levels-and-settings-split-by-audience.md)
  (no UI surface here), [ADR-285](../../product/architecture/adr-285-connector-capabilities-are-interfaces-and-per-connection-variance-is-a-probe-on-the-capability.md)
  (capability interfaces: the Delivery ports belong to the connector port).
- Precedents, same shape: [S03a Tracker integration](../story-6359-s03a-package-tracker-integration/feature-delta.md)
  (its D-6 write-back and D-7 Promises edges hand work to this Story), [S03b Work Items](../story-6360-s03b-package-work-items/feature-delta.md),
  [S03c Forecasting](../story-6361-s03c-package-forecasting/feature-delta.md).

DISCUSS: covered at Epic level (D-6264-3, D-6264-4, D-6264-16); waves start at DESIGN.

DESIGN run 2026-11-17, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is recorded as
such. No choice changes what a user sees or is told, so no decision is held; the open questions at the end are
confirmations, not blockers.

**Start condition.** This Story starts only after S03a is closed (features A, B and C). S03a was still moving files when
this design was measured (`main` at `55010ee55`; re-checked at `79128f4b4`, where S03a B2 had already moved `IWorkTrackingConnectorFactory`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher` and the rest of the port into `Modules/TrackerIntegration/Application`; S03a B3, B4 and C were still to come). Every count below is **re-measured at the Story's start (A2)**. Two things depend on S03a: the connector port
and factory change namespace under the Deliveries services (a `using` edit made by S03a B2), and S03a's rule files list
the Jira → renderer edge by full type name (S03a D-7). See "Risks after S03a".

## Wave: DESIGN / [REF] Inherited

Linked, not copied.

- **ADR-270 Promises row**: Deliveries, their rules, sources (Jira Releases), notes, closure pins and metric
  snapshots. "Packaged (today's Deliveries)". The Portfolio as a unit goes to Organisation set-up (S03i); the Team and
  Portfolio refresh pipelines go to Work Items (Epic 6474 slice 12); the update queue's mechanics are platform lane;
  Feature ranking and write-back are "Left open" (decided here: D-5, D-6).
- **ADR-270 Decision 3**: "Promises recomputes rule-based Deliveries on 'items refreshed' and records its snapshot on
  'forecasts updated'". Today the recompute and the source resync are two inline calls in `PortfolioUpdater`
  (ratchet candidate 4, left to Epic 6474 slice 12 by S03b D-3). Only the snapshot handler is already an event handler.
- **ADR-270 "Every edge"**: Forecasting → Promises, Work Items → Promises (both legal: Promises is downstream);
  Tracker integration → Promises (the Jira renderer edge, S03a D-7); ratchet candidate 3 (answered by S03a D-7 b:
  "through the ACL's port since S03a").
- **S03a D-6**: the write-back **round** stays where it is and is placed by "S03d or a later Story". **S03a D-7**: the
  Jira connector → `IDeliveryForecastBlockRenderer` edge is listed, owner S03d; "the list only shrinks".
- **S03c**: `DeliveryForecastPublishingService`, `DeliveryForecastBlockRenderer`, `DeliveryForecastPublishingHandler`,
  their interfaces and `Models/DeliverySources/DeliveryForecast*` stay for this Story.
- **S03b**: the updaters stay in the update code; the `DeliveryRuleService` entry sits in Work Items' inbound list
  until Promises has a namespace (R-W5).
- **Packaging rules (catalogue "Notes from S02")**: rules first, proven with a made-up type under `Modules`; edges from
  the compiled IL (`IlDependencies`), inline lists compared both ways, never loosened (ADR-268); one oracle file per
  slice; moves apart from edits; `Program.cs` edits force the full CI suite; reuse `ModuleRules` + `ModuleWording`;
  where a context has no namespace yet, list its types by name; mutation bar "no survivor or no-coverage mutant added in
  the moved code"; EXAMINE commands declared up front.

## Wave: DESIGN / [REF] DoR re-run (2026-11-17)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Promises code sits loose in `Services/Implementation`, `Services/Implementation/DeliverySources`, `Services/Interfaces/DeliverySources`, `Services/Implementation/DomainEvents`; no rule names it, and the only "Portfolio/Delivery" rule pattern matches the update code, not Deliveries (E1). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files and edges below. |
| 4 UAT | PASS (catalogue) | Brief AC, CI-observable; the acceptance designer writes the oracles in DELIVER. |
| 5 AC from UAT | PASS, three ACs corrected | (a) "No file of this context remains in `Services`" holds for **Promises** only, with named exceptions (D-1, D-4, D-5, D-6); (b) "context-owned `Models` types have moved" holds only for the types that pass the four S03a conditions (D-2); (c) "`BackgroundServices/Update/**`, `WriteBack*`, `FeatureOrdering*`/`FeatureRank*`" are no longer in IN: ADR-270 gave them to other owners. |
| 6 Right-sized | PASS by re-sizing | Brief says L. The brief was written when the whole Portfolio/Delivery module was in scope. Without the update code (33 files), the write-back round (8), ordering and ranking (9) and the persistence ports, the Promises set is **13 production files + 1 admitted `Models` file + 9 unit-test files**. DESIGN sizes it **M, about 6.5 days, 9 slices, 3 nwdes features** (most of the cost is rules first and the one edit slice, not the move). |
| 7 Technical notes | PASS | Brief note on in-process and cluster update adapters: nothing moves there (D-1), so the composition root's adapter choice is untouched. |
| 8 Dependencies | PASS | Hard: S01 Closed, S02 gate go-with-changes. Soft: S05d (Delivery repositories stay, D-4), S07c, S12b (controllers and DTOs stay). **Added: S03a closed first.** |
| 9 Outcome KPIs | PASS | KPI 1 (files under `Services`), KPI 2 (contexts packaged), KPI 3 (`Models` files). |

Hypothesis in the brief ("Portfolio/Delivery can be one context; disproved if it is really two"): **already disproved by
S01**. ADR-270 split it into Promises, Organisation set-up and the refresh half of Work Items. This Story packages the
Promises half only. No split into two Stories is needed.

## Wave: DESIGN / [REF] Baseline re-count (2026-11-17, `main` at `55010ee55`; KPI and Services counts re-checked at `79128f4b4`)

Counted with `git ls-files`. Namespaces from each file's `namespace` line. Edges: `using` lines and `git grep -w` of type
names over `Lighthouse.Backend` and `Lighthouse.Backend.Tests`. **This is a text-search fallback, `noisy`**: it
misses same-namespace and fully qualified references, catch-clause types and generic `GetRequiredService<T>()` calls
are only seen when the name appears; it also matches names in comments (example: `BlockedItemService` names
`DeliveryRuleService` in a comment only). `code-fact` was not used (same tier for C#). The IL measurement is slice A2's.

| Measure | Today |
|---|---|
| Files under `Services` (KPI 1; all tracked files, as S03a counted) | **419** at `55010ee55`; **400** at `79128f4b4` (S03a B1/B2 moved files; re-measure at start) |
| Loose files at the top of `Services/Interfaces` + `Services/Implementation` | 33 + 43 = **76** |
| `Models` files (KPI 3) | **267** |
| Contexts packaged (KPI 2) | **3 of 11** (`FlowMetrics`, `Forecasting`, `WorkItems`); `TrackerIntegration` in progress (S03a B1 and B2 done) |

### The Promises set by ADR-270 (production)

| Group | From | Files |
|---|---|---|
| Rule service | `Services/Implementation/DeliveryRuleService`, `Services/Interfaces/IDeliveryRuleService` (ADR-012 pins its public signature) | 2 |
| Delivery sources | `Services/Implementation/DeliverySources/{DeliverySourceResolver, DeliverySourceSyncService, DeliveryForecastPublishingService, DeliveryForecastBlockRenderer}` | 4 |
| | `Services/Interfaces/DeliverySources/{IDeliverySourceResolver, IDeliverySourceSyncService, IDeliveryForecastPublishingService, IDeliveryForecastBlockRenderer}` | 4 |
| Metric values | `Services/Implementation/DeliveryMetricValuesProjector` (with the record `DeliveryMetricValues`; no interface) | 1 |
| Reactions to "forecasts updated" | `Services/Implementation/DomainEvents/{DeliveryMetricSnapshotRecordingHandler, DeliveryForecastPublishingHandler}` | 2 |
| **Total moving, production** | | **13** (all under `Services`) |
| Admitted `Models` type (D-2, to confirm in A2) | `Models/DeliverySources/DeliveryForecastBlock` | 1 |

**Touching the set but not moving** (D-1 says why):

| Group | Files | Owner / reason |
|---|---|---|
| Update code | `Services/Implementation/BackgroundServices/Update/*` 24 + `Services/Interfaces/Update/*` 9 (TeamUpdater, PortfolioUpdater, queue, status store, Redis and Postgres adapters, cancellation) | Platform lane (queue mechanics) and Epic 6474 slice 12 (updaters). S03b D-4 already decided. |
| Write-back round | `WriteBackService`, `WriteBackCollector`, `WriteBackRound`, `WriteBackRoundContext`, `WriteBackTriggerService` + 3 interfaces = 8; `Models/WriteBack/*` 9 | D-6 |
| Ordering and ranking | `FeatureOrdering`, `FeatureOrderingPolicyProvider`, `FeatureRankSeeder`, `FeatureRankingService`, `OptionalFeatures/FeatureOrderingApplier` + 4 interfaces = 9 | D-5 |
| Delivery persistence | `DeliveryRepository` (including `GetOrCreateClosureRecord`, the closure-pin write), `DeliveryMetricSnapshotRepository`, `IDeliveryRepository`, `IDeliveryMetricSnapshotRepository`, `RecordableDeliveries` (5). Delivery **notes** are read and written by `DeliveryNotesController` through `IDeliveryRepository`; there is no notes service. | D-4, S05d |
| Entities and kernel types | `Models/{Delivery, DeliveryClosureRecord, DeliveryMetricSnapshot, DeliveryNote, DeliveryMetricsProjection, DeliverySelectionMode, DeliveryArchivedException, DeliverySourceBoundException}`, `Models/Forecast/DeliveryCompletionForecast`, 10 more `Models/DeliverySources/*` | D-0, D-2 |
| EF guard | `Data/DeliveryMetricSnapshotDayCollisionGuard` (read by `DatabaseConfigurator`) | Platform lane |
| Controllers and DTOs | 4 Delivery controllers, `PortfolioController(s)`, `API/DTO/Delivery*`, `Archived/*`, `API/Filters/DeliveryArchivedExceptionFilter` | S12a/S12b/S12c |
| Portfolio entity, repository, settings | `Models/Portfolio`, `PortfolioRepository` | S03i, S05d |

### Tests of the set (path count, `noisy`)

Unit tests that move (9): `Services/Implementation/DeliveryRuleServiceTest`, `DeliveryMetricValuesProjectorTest`,
`DeliverySources/{DeliveryForecastBlockRendererTest, DeliveryForecastBlockRendererBeyondTheHorizonTest,
DeliveryForecastPublishingServiceTest, DeliverySourceResolverTest, DeliverySourceSyncServiceTest}`,
`DomainEvents/{DeliveryForecastPublishingHandlerTest, DeliveryMetricSnapshotRecordingHandlerTest}`.
`DeliverySourceBindabilityTest` tests a kernel type and stays. 29 test files `using` a `DeliverySources` namespace;
38 test files name a set type (includes comments and strings). 13 Jira test files build `new DeliveryForecastBlockRenderer()`
(a `using` edit after the move). None of the 9 moving tests carries a live-connector category (checked by name search).
`DeliveryMetricSnapshotRecordingHandlerTest` is in `TestHostUseGuardTest.HostUsersStillToMove` (it derives from `IntegrationFixture` and builds a `TestWebApplicationFactory`: D-9 gives its action).
Integration, acceptance and controller tests for Deliveries stay under `API/Integration` and `API`.

### Inbound (who calls the set; text search, `noisy`)

| Caller | Reaches | ADR-270 reading |
|---|---|---|
| `API/DeliveriesController` | `IDeliveryRuleService`, `IDeliverySourceResolver`, `DeliveryMetricValuesProjector` | `API` → Application: legal by namespace |
| `API/DeliveryRulesController` | `IDeliveryRuleService` | legal |
| `API/DeliverySourcesController` | `IDeliverySourceResolver` | legal |
| `Services/Implementation/BackgroundServices/Update/PortfolioUpdater` | `IDeliveryRuleService` (line 93), `IDeliverySourceSyncService` (line 98), both by `GetRequiredService<T>()` | ratchet candidate 4: **listed**, owner Epic 6474 slice 12 |
| `Modules/…` | nothing found | none |
| `JiraWorkTrackingConnector` | `IDeliveryForecastBlockRenderer.MergeInto` (line 3971) | **Tracker integration → Promises**: S03a D-7 entry; resolved in C2 (D-7) |
| `Program` | all registrations (lines 1470-1474, 1632-1634) | composition root |
| 13 Jira test files, `TestHelpers/JiraConnectorTestSetup` | `new DeliveryForecastBlockRenderer()` | tests, `using` edit |

### Outbound (what the set reaches beyond `Models`)

| From | To | Reading |
|---|---|---|
| `DeliveryRuleService` | `Modules.WorkItems.Application` (rule evaluator, field provider), `Services.Interfaces.Repositories` (`RecordableDeliveries`) | Promises → Work Items: legal. Repository port: platform persistence, listed. |
| `DeliverySourceResolver`, `DeliveryForecastPublishingService` | `IWorkTrackingConnectorFactory`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher`: since S03a B2 (checked at `79128f4b4`) they live in `Modules/TrackerIntegration/Application`; the `using` lines of these two files are S03a's edit, re-measured at start | Promises → Tracker integration through the port: legal, listed by namespace (D-7, candidate 3) |
| `DeliveryForecastPublishingService`, `DeliverySourceSyncService`, `DeliveryMetricSnapshotRecordingHandler` | `ILighthouseClock` (platform, top-level `Services.Interfaces`) | platform, listed |
| Services and handlers | `Services.Interfaces.Repositories` (`IDeliveryRepository`, `IDeliveryMetricSnapshotRepository`) | platform persistence, listed (D-4) |
| Handlers | `Services.Interfaces.DomainEvents` (dispatcher contract), `Models.Events.PortfolioForecastsUpdated` | platform lane; the event stays in `Models/Events` (S03a/b/c) |
| Renderer, projector | `Models`, `Models.Forecast`, `Models.DeliverySources` | kernel |
| Set → Forecasting or Flow metrics | none found | Promises is downstream of Forecasting: it may call it; it does not today. To confirm from the IL in A2. |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. Which of the brief's IN items really belong to Promises (update code, write-back, ordering)? → D-1, D-5, D-6.
2. Which `Models` types move? → D-2 (rule: S03a's amended four conditions).
3. Target path and sub-folders. → D-3.
4. Do the Delivery repositories move (S05d)? → D-4.
5. Where does Feature ranking live (ADR-270 "Left open")? → D-5.
6. Where does the write-back round live (S03a D-6)? → D-6.
7. The Jira → `IDeliveryForecastBlockRenderer` edge (S03a D-7)? → D-7.
8. Rule set and how it is written. → D-8, Ratchet design.
9. Tests, DI, CI selection, behaviour-free proof, mutation. → D-9 to D-13.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **S03a is closed** before the first slice starts.
- [ ] **Behaviour-free** for B slices and A, C1 and C3: routes, DTO shapes, serialized responses, UI pixels unchanged; existing tests green unmodified apart from moved tests, `using`s and rule files. C2 is the one edit slice: same text bytes written to Jira (D-7).
- [ ] **Rules first**: every rule keyed on the set's namespaces, types or paths widened before a type moves, proven with a made-up type under `Modules/Promises`.
- [ ] **Moves separate from edits**: `refactor(promises): move …` commits apart from edit commits.
- [ ] **Ratchet in the same change**; inline lists; no rule loosened.
- [ ] **No Promises file left in `Services/Interfaces|Implementation`**, with the named exceptions of D-1, D-4, D-5, D-6.
- [ ] **Strings and config checked** for namespaces and full type names (log categories, `TestHostUseGuardTest.HostUsersStillToMove`, S03a and S03b rule lists, Stryker globs).
- [ ] **`Program.cs` edits** (B1, B2, C2): the push runs the full CI backend suite and, through `SHARED_REGEX`, every live suite (D-11).
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4, ADR-270, brief ACs.
- [ ] **Cross-cutting re-check** answered (section below).

## Wave: DESIGN / [REF] Context-map placement (2026-11-17)

- **Context touched**: Promises, packaged into `Modules/Promises`.
- **Neighbours whose edge becomes visible** (no behaviour change): Work Items (rule evaluator, legal); Tracker
  integration (the port, legal; the Jira renderer edge, removed in C2); Forecasting (supplies the dates; Promises reacts to
  `PortfolioForecastsUpdated`); Organisation set-up and the platform lane (repositories, clock, dispatcher).
- **New terms / concepts**: none. Folder `Modules/Promises` is the ADR-270 glossary name; "Delivery" stays the code and
  user word. The C2 port is named `IDescriptionBlockMerger`, plain English, no new glossary word.
- **Domain events**: none published or newly reacted to. `PortfolioForecastsUpdated` stays in `Models/Events` with its
  publisher (Forecasting). The two reactions move into `Application/Policies` with registration order unchanged (D-10).
  "Promise made, kept, missed" stays target.
- **UI surface**: none (ADR-271). **Improvement backlog check**: S03d is itself a #6510 item; N/A, stated.

## Wave: DESIGN / [REF] Evidence (2026-11-17, `main` at `55010ee55`)

| # | Finding | Consequence |
|---|---|---|
| E1 | `ModuleBoundariesArchUnitTest.PortfolioDeliveryPattern` = `Services.(Implementation.BackgroundServices\|Interfaces.Update)`: it names the **update code only**. It is read by `RefinementModuleArchUnitTest`, `ForecastingRulesSeeTheModuleArchUnitTest` and `TrackerIntegrationRulesSeeTheModuleArchUnitTest` (by key). No rule names Deliveries. | The Promises namespaces are **new** keys, not a widening of that pattern. The old pattern keeps its update-code meaning; renaming it (`UpdateCodePattern`) is an edit left out (subtraction: three readers, no behaviour gain). A1 adds `PromisesPattern` beside it. |
| E2 | `TrackerIntegrationRulesSeeTheModuleArchUnitTest` has a fixture `AModuleConnectorReachingADeliverySource` that names `IDeliveryForecastBlockRenderer` by full type, and S03a's A1 text says "do not add the delivery sources to the Portfolio/Delivery side yet". | S03a's rule files hold the old namespace of the renderer. B1 renames the entries; C1/C2 remove them. |
| E3 | `WorkItemsModuleBoundariesArchUnitTest.OtherOutsideTypesReachingTheApplication` lists `Services.Implementation.DeliveryRuleService` by string; `WorkItemsLegalCallers` does not name Promises. | C1 adds the Promises namespaces to the legal callers (Promises is downstream of Work Items) and deletes the entry: the list shrinks. |
| E4 | `RepositoryLeakRatchetArchUnitTest` lists the four Delivery controllers; `TestHostUseGuardTest.HostUsersStillToMove` holds **25** lines whose test-class name contains `Deliver` or `WriteBack` (match rule: `grep -c -E '"Lighthouse.Backend.Tests[^"]*(Deliver\|WriteBack)'`, at `79128f4b4`); one of them, `DeliveryMetricSnapshotRecordingHandlerTest`, is a moving test. `DeliveryRuleServiceApiPreservationTest` holds `typeof(DeliveryRuleService)`. | `using`-only edits except the entries D-9 names. The pin keeps its assertions (brief AC). |
| E5 | `PortfolioForecastsUpdated` has two handlers registered in `Program.cs` at lines 1633-1634: snapshot recording, then publishing. `DomainEventHandlerOrderPinTest` has **no entry** for this event. | The move keeps the order (D-10). **B2 adds the order pin** for this event (its oracle reads the registration order). Whether the two handlers depend on each other: unknown, measured in A2; the pin holds today's order either way. |
| E6 | The Jira connector uses the renderer for one call, `MergeInto(existing, blockText)`. The Promises publishing service uses only `Render`. No other production code calls `MergeInto`. | `MergeInto` can leave `IDeliveryForecastBlockRenderer` without changing a caller except the connector (D-7). |
| E7 | 13 Jira tests construct the connector with `new DeliveryForecastBlockRenderer()`; `JiraDeliverySourceProviderTest` registers `IDeliveryForecastBlockRenderer` in a service collection. | After C2 the class implements the new port, so `new DeliveryForecastBlockRenderer()` still compiles. Only the one DI registration needs the new port. |
| E8 | `Scripts/test-selection/path-classifier.sh` line 28: `SHARED_REGEX` names `Program.cs` and `Modules/TrackerIntegration/(Application\|Domain)/`. `test-build-filter.sh` (lines 12-18): `CONNECTOR_SHARED=true` forces a full run; `dev-test.sh` (lines 60-65) adds the guarded integration category. So **a push that edits `Program.cs` or adds a file under `Modules/TrackerIntegration/Application` runs every live suite** (Jira, ADO, Linear, ServiceNow, GitHub), not only the Jira one. No regex names a Promises path. Stryker: no live config names the set. `test-time-budget.json`: no Delivery entry found. | B1, B2 and C2 each edit `Program.cs`, and C2 also adds a file under the Tracker integration `Application`: each of those pushes runs all live suites (D-11 prices it). A4 pins the classifier cases. |
| E9 | `forecast-answers-compare` already reads `GET /api/v1/deliveries/portfolio/{id}` (Delivery likelihoods). Nothing reads Delivery metrics history, rules schema or sources. | A3 adds one EXAMINE command on the same shared pieces (D-12). |
| E10 | The Delivery EF entities (`Delivery`, `DeliveryNote`, `DeliveryClosureRecord`, `DeliveryMetricSnapshot`) are mapped from `Models` in `LighthouseAppContext`; none moves. | No migration; `ef-model-unchanged` proves it. |
| E11 | Delivery source Models: `DeliverySourceDescriptor`, `Option`, `Resolution`, `Snapshot`, `SweepPurpose`, `UnavailableReason`, `Bindability`, `ForecastPublication`, `ForecastPublishResult` are read by the Jira connector or its port; `SourceOptionBlockReason` by an API DTO. `DeliveryForecastBlock` is read only by the renderer, the publishing service and the renderer interface (all moving). | Only `DeliveryForecastBlock` can pass the four conditions (D-2). |

## Wave: DESIGN / [REF] Decisions

All taken as recommended (AFK, 2026-11-17). Option tables are the record of what was weighed.

### D-0 No schema change — RECOMMENDED

No entity, `DbSet`, migration or table name changes. Entities stay in `Models` (same reasoning as S03a to S03c).
Checked by `ef-model-unchanged` in EXAMINE.

### D-1 Membership — RECOMMENDED: the 13 files of the Promises table move; the rest stays with a named owner

"Move" = a pure-move commit to `Lighthouse.Backend.Modules.Promises.<Layer>[.<Sub>]`, namespace = folder.

| Candidate | Decision | Why |
|---|---|---|
| `DeliveryForecastBlock` (admitted `Models` value, D-2) | **Move**, `Domain` | A pure value; reaches only the kernel. |
| `DeliveryForecastBlockRenderer` + `IDeliveryForecastBlockRenderer`; `DeliveryRuleService` + interface; `DeliverySourceResolver`, `DeliverySourceSyncService`, `DeliveryForecastPublishingService` + 3 interfaces; `DeliveryMetricValuesProjector` | **Move**, `Application` | Use cases and their ports (ports next to their use case, ADR-270 Decision 6). The renderer stays out of `Domain` because after C2 it implements Tracker integration's `IDescriptionBlockMerger`, and `Domain` may reach only the kernel (R-P2); `Render` and `MergeInto` stay in one class (D-7). The projector stays out of `Domain`: the API controller, the snapshot handler and the publishing service all call it. |
| `DeliveryMetricSnapshotRecordingHandler`, `DeliveryForecastPublishingHandler` | **Move**, `Application/Policies` | "Whenever forecasts are updated, then Promises does Y" (ADR-270 Update after S01). Registration order unchanged (D-10). |
| Update queue and updaters (`BackgroundServices/Update/*`, `Interfaces/Update/*`, 33 files) | **Stay** | The brief's IN item is superseded by ADR-270: queue mechanics are the platform lane (S03f); the Team and Portfolio updaters are replaced by Epic 6474 slice 12 (S03b D-4). Moving the Portfolio updater into Promises would be wrong: it also refreshes Work Items and triggers Forecasting. The brief's note on Redis and Postgres adapters is moot: nothing there moves. |
| Write-back round (8 files) | **Stay** | D-6. |
| `FeatureOrdering*`, `FeatureRank*` (9 files) | **Stay** | D-5. |
| `DeliveryRepository` (closure-pin persistence: `GetOrCreateClosureRecord`), `DeliveryMetricSnapshotRepository` + 2 interfaces + `RecordableDeliveries`; Delivery **notes** persistence (`DeliveryNotesController` → `IDeliveryRepository`, `DeliveryNote` entity) | **Stay** in `Services/*/Repositories` | D-4. Named exception to "no file left"; owner S05d (the repository split is S05d's). |
| Entities, kernel Delivery types, controllers, DTOs, `Data/DeliveryMetricSnapshotDayCollisionGuard`, Portfolio parts | **Stay** | D-0, D-2; S12; S03i. |
| `IDeliverySourceProvider`, `IDeliveryForecastPublisher` | already Tracker integration's port (S03a) | Not Promises' files. |

Totals: **13 production files move** (13 from `Services`) + 1 admitted `Models` file. Loose files at the top of
`Services/Interfaces` + `Services/Implementation`: 76 → **73**. KPI 1: **−13** against the tree at the time S03d starts
(400 at `79128f4b4`; re-measure at start). KPI 2: **+1** (5 of 11 once S03a has closed). KPI 3: 267 → **266** if `DeliveryForecastBlock` is admitted.

### D-2 Context-owned `Models` types — RECOMMENDED: S03a's amended rule, four conditions

A `Models` type moves only if it is (a) not an EF entity, (b) not on the wire, (c) not reached by a kernel type and (d)
not used by any code that stays outside the module. A2 lists every candidate from the IL and fixes the answer in a test.

| Option | Verdict |
|---|---|
| **a (recommended)** Rule-based, four conditions | Expected admitted: `DeliveryForecastBlock` only (E11). Stay: the four entities (a); `SourceOptionBlockReason` and `Delivery*Dto` inputs (b); `DeliveryMetricsProjection`, `DeliverySelectionMode`, `DeliveryArchivedException`, `DeliverySourceBoundException`, `DeliveryCompletionForecast` (c: `Delivery` reaches them); the nine source types the connector port or Jira connector read (d). |
| b Move all `Delivery*` Models | Rejected: changes the EF model, breaks the connector port's signatures (S06 D-8), waits for S12c for wire types. |
| c Move none | Rejected as a default; `DeliveryForecastBlock` is a free, honest move. |

The brief AC "context-owned `Models` types have moved" is rewritten at close-out: "types that are not entities, wire
types, port vocabulary or reached by the kernel have moved; the rest is listed for S04b/S12c" (as S03a/S03b).

### D-3 Target path and sub-folders — RECOMMENDED

`Lighthouse.Backend/Lighthouse.Backend/Modules/Promises/`:

```
Domain/             DeliveryForecastBlock (admitted by D-2); reaches the kernel only
Application/        rule service, source resolver, source sync, forecast publishing, forecast block renderer (+ their 5 interfaces), metric-values projector
Application/Policies/  snapshot-recording and forecast-publishing handlers of PortfolioForecastsUpdated
```

The renderer is in `Application`, not `Domain`: after C2 it implements `IDescriptionBlockMerger`, a port in
`Modules.TrackerIntegration.Application`, and the Domain rule (R-P2) allows only the kernel and the BCL. `Render` and
`MergeInto` stay in one class (the markers must be matched together). The marker rules are **not** extracted into a
separate Domain value: that is an edit with no second consumer (subtraction). If A2 finds that `DeliveryForecastBlock`
fails the four conditions, there is no `Domain` folder and R-P2/R-P3 are dropped (the shared Domain rule fails on an
empty Domain), recorded in A2.

No `Adapters` (D-4: repositories stay; no hosted service belongs to Promises). Tests mirror the layout under
`Lighthouse.Backend.Tests/Modules/Promises/…`.

### D-4 Delivery persistence — RECOMMENDED: repositories stay (S05d not taken)

The Delivery repositories carry more than Delivery lists: `DeliveryRepository` also writes the **closure pin**
(`GetOrCreateClosureRecord`) and is the only store for Delivery **notes** (`DeliveryNotesController` → `IDeliveryRepository`).
How the Portfolio-side repositories split and where their ports live is S05d's decision, not a packaging move.

| Option | Verdict |
|---|---|
| **a (recommended)** The two repositories, their interfaces, `RecordableDeliveries`, the closure-pin and notes persistence stay in `Services/*/Repositories`; the Application's use of them is listed in the outbound list | Same decision as S03a D-4. Keeps S03d a move. Named exceptions to "no file left", owner S05d. |
| b Move them to `Adapters/Persistence` (Flow metrics precedent) | Rejected now: the split of the Portfolio-side repositories is S05d's; `RecordableDeliveries` is also read by `PortfolioUpdater`; S05d would move them again. |
| c Take S05d inside S03d | Rejected: new ports and consumers rewired in a move Story. |

### D-5 Feature ordering and ranking (ADR-270 "Left open") — RECOMMENDED: not Promises; Organisation set-up, placed by S03i

| Option | Verdict |
|---|---|
| **a (recommended)** Stays in `Services`; owner Organisation set-up, placed by S03i | The ordering **policy** is Organisation set-up's by ADR-270 (cadence and priority rules). `FeatureRankChanged` and `FeatureOrderingPolicyChanged` are already handled by Forecasting's policies (S03c); Work Items calls `IFeatureOrdering` (S03b E2). Rank is a Feature order, not a promise. The ranking service and the policy are applied together (`FeatureOrderingApplier`), so one owner is cleaner. `FeatureOrderingSingleSourceArchUnitTest` stays as is. |
| b Move to Promises ("planning") | Rejected: Work Items and Forecasting would reach Promises (upstream → downstream, illegal). |
| c Move to Forecasting | Rejected: re-opens a closed module for a rule about order, not forecasts. |

One decision recorded in ADR-270 "Left open" at close-out: "Feature ranking: not Promises; Organisation set-up, placed by
S03i." `ManualRank` follows (same "Left open" line).

### D-6 Write-back round (S03a D-6) — RECOMMENDED: not Promises; stays, named exception; follow-up item proposed, not yet filed

| Option | Verdict |
|---|---|
| **a (recommended)** Round stays in `Services`; not placed in this Story | The round writes **forecast dates and Team or Portfolio fields** to a tracker per connection mapping. `ForecastUpdater` (Forecasting) calls `IWriteBackTriggerService`; moving the round into Promises would make Forecasting reach Promises, the reverse of the legal edge (Forecasting → Promises). The round reaches Flow metrics and Licensing too (`using` lines). It is not a Delivery rule. Delivery forecast publishing (a Delivery's joint answer to a Jira release) **is** Promises' and moves. |
| b Move the round to Forecasting `Application` | Candidate home (its trigger and dates are Forecasting's, and Forecasting depends on Work Items and Tracker integration). Rejected for this Story: re-opens a closed module, grows Forecasting's R-F5 inbound list (`TeamUpdater`, `PortfolioUpdater`, `UpdateQueueService`) and adds Licensing and Flow-metrics edges. Better as its own Story with its own measurement. |
| c Move to Promises (brief IN: `WriteBack*`) | Rejected: reverse edge above. |

Consequence: the brief's IN item `WriteBack*` is dropped. **No ADO item exists for the round's placement.** Proposed, **to be
filed after the maintainer confirms** (open question 2): title "Place the write-back round (candidate: Forecasting
`Application/WriteBack`)", owner candidate: the Forecasting module, under Epic #6510. ADR-270 "Left open" is updated at
close-out with "not Promises; owner proposed", never with "filed".

### D-7 The Promises edges from S03a D-7 — RECOMMENDED: the port edge is legal; the Jira edge is removed by inversion in C2

Location facts (checked at `79128f4b4`): `IWorkTrackingConnectorFactory`, `IDeliverySourceProvider` and
`IDeliveryForecastPublisher` already live in `Modules/TrackerIntegration/Application` (S03a B2). The Jira connector is
still under `Services/Implementation/WorkTrackingConnectors/Jira` until S03a B3; C2 runs after S03a closes.

| Edge | Options | Recommended | Why |
|---|---|---|---|
| Promises → Tracker integration (`DeliverySourceResolver`, `DeliveryForecastPublishingService` take the factory and type-test for `IDeliverySourceProvider`, `IDeliveryForecastPublisher`) | a leave listed by name; **b legal by namespace: Promises' `Application` may reach `Modules.TrackerIntegration.Application`, never `Adapters`** | **b** | Both callers move into `Modules.Promises.Application`; S03a's two list entries are deleted in C1. |
| Tracker integration → Promises (`JiraWorkTrackingConnector` → `IDeliveryForecastBlockRenderer.MergeInto`) | a keep the S03a list entry (type renamed) and stop; **b inversion: `Modules.TrackerIntegration.Application` defines a one-method port `IDescriptionBlockMerger.MergeInto(string? existing, string blockText)`; `DeliveryForecastBlockRenderer` (in `Promises/Application`, D-3) implements it; the connector takes the port**; c hand the merged text in as data (S03a "target a"); d move the whole renderer into Tracker integration | **b** | b makes the ACL a true leaf (S03a R-T7 list goes to empty) with no change to the bytes written. The merge needs the tracker's **current** description, which only the connector has, so c needs a new connector read capability. d moves tracker-text rules that Promises owns, and `Render` and `MergeInto` share the markers on purpose: they stay in one class. a leaves a known upward edge with no owner. `MergeInto` leaves `IDeliveryForecastBlockRenderer` (E6), so Promises' own callers do not change. The edge Promises → Tracker integration `Application` (implementing the port) is the legal direction. |

C2 is the **one edit slice** and the only one that changes a closed module's public surface (one new interface file in
the Tracker integration `Application` and one constructor parameter type in the Jira connector). It runs after S03a is
closed. Because it adds a file under `Modules/TrackerIntegration/Application` and edits `Program.cs`, its push runs
**every live suite** (E8, D-11). The renderer is registered twice (`IDeliveryForecastBlockRenderer`,
`IDescriptionBlockMerger`); the class is stateless and pure, so two scoped instances behave the same. Whether C2 stays in
this Story or becomes its own is open question 3; if C2 is dropped, S03a's R-T7 keeps one entry (renamed by B1).

### D-8 The rule set — RECOMMENDED: by namespace, through `ModuleRules`, one new boundaries file

Written in `Architecture/PromisesModuleBoundariesArchUnitTest.cs` with `ModuleRules` and a `ModuleWording` of its own
(REUSE; a `PromisesModule` namespace constant beside `FlowMetricsModule`/`ForecastingModule`). Rules and baselines in
"Ratchet design". Where a neighbour has no namespace yet (Organisation set-up, the update queue, the platform
repositories), its types are listed by name ("stricter than designed, never looser").

### D-9 Tests move with production — RECOMMENDED (S03a D-9), with the "improve the tests you touch" rule applied

Each B slice moves the unit tests of the files it moves, in a separate `refactor(promises): move … tests` commit in the
same slice, to `Lighthouse.Backend.Tests/Modules/Promises/<same sub-folder>`. Reason to move with production: one touch per
file, and the placement rule (C3) then has nothing left to find. The S03a reason (live-suite classifier on test paths) does
not apply (E8). Integration, acceptance and controller tests stay. `PromisesUnitTestPlacementTest` lands in C3.

CLAUDE.md says a change that touches a test class leaves it better. No waiver is taken. Action per class (measured at
`79128f4b4`; a host reference = `TestWebApplicationFactory`, `WebApplicationFactory`, `IntegrationFixture` or `TestHost`):

| Test class | Host references | In `HostUsersStillToMove` | Action |
|---|---|---|---|
| `DeliveryRuleServiceTest`, `DeliveryMetricValuesProjectorTest`, `DeliveryForecastBlockRendererTest`, `DeliveryForecastBlockRendererBeyondTheHorizonTest`, `DeliveryForecastPublishingServiceTest`, `DeliverySourceResolverTest`, `DeliverySourceSyncServiceTest`, `DeliveryForecastPublishingHandlerTest` (8) | 0 | no | Move only. They are already unit tests; the B slice's crafter reads each against the test-writing skill and fixes any row it touches; none is expected (not read here). |
| `DeliveryMetricSnapshotRecordingHandlerTest` | 4 (derives from `IntegrationFixture`; builds a `TestWebApplicationFactory`, lines 46 and 61; other rows already build the handler with mocks, lines 165 and 508-525) | **yes** (line 295) | Move (B2). Each row that starts the host moves down to the mock-built handler the class already uses. If no host reference remains, the `HostUsersStillToMove` line **comes off**; if a row must keep the host, the line is renamed to the new namespace. Which case: measured in B2. |
| The 13 Jira test files that write `new DeliveryForecastBlockRenderer()` (list in E7) | 0 | none of them | `using`-only edit in B1 (a `using` line is not a row; no row is touched). Stated, not waived: if the crafter touches a row of one of them, that row follows the skill. C2 edits only the `JiraDeliverySourceProviderTest` service registration. |
| `DeliveryRuleServiceApiPreservationTest`, `RepositoryLeakRatchetArchUnitTest`, Work Items and Tracker integration rule files | n/a | no | Rule files: edited in A1, B1, C1 as listed. |

### D-10 DI registration — RECOMMENDED

`Program.cs` lines and order stay; only `using`s and full type names in lines 1470-1474 and 1632-1634 change. The two
`PortfolioForecastsUpdated` registrations keep their order (E5). C2 adds one line (`IDescriptionBlockMerger`). No
`AddPromises` extension (S09a).

### D-11 CI selection and live suites — RECOMMENDED: no classifier widening; the cost of three pushes is priced

No regex names a Promises path (E8), so nothing silently stops selecting a live suite. But `SHARED_REGEX` names `Program.cs`
and `Modules/TrackerIntegration/(Application|Domain)/`, and `CONNECTOR_SHARED=true` forces a full run: **B1, B2 and C2 each
push every live suite** (Jira, ADO, Linear, ServiceNow, GitHub). The Linear key is shared with CI; repeated runs can
rate-limit it (429), and only one of the resulting failures names the 429.

| Option | Live runs | Verdict |
|---|---|---|
| a push every slice | B1, B2, C2 = 3 | the S03a maintainer choice; highest 429 risk |
| **b (recommended)** commits stay per slice; push B1 and B2 together once both are green locally; push C1, C2 and C3 together | **2** | one run for the move, one for the edit; A slices touch no production or `Program.cs` file, so they cost no live run (A3/A4 edit `Scripts/`: whether a path under `Scripts/` is in the truncated tail of `SHARED_REGEX` is checked by A4's cases) |
| c drop C2 (open question 3) | 1 | cheapest; keeps one R-T7 entry |

A4 adds classifier cases: a Promises path selects backend only; C2's two paths select all live suites. Push cadence is
open question 4.

### D-12 Behaviour-free proof and EXAMINE — RECOMMENDED

1. Existing tests green unmodified apart from moves, `using`s and rule files.
2. Reuse EXAMINE commands (`Scripts/examine`): `architecture-rules` (filter `Promises`), `ef-model-unchanged`,
   `backend-tests`, `forecast-answers-compare` (it already reads `GET /api/v1/deliveries/portfolio/{id}`).
3. **New command `deliveries-answers-compare`** (CREATE_NEW on the shared capture pieces of `build-examine-commands`): per
   demo scenario and portfolio it reads the Delivery list, `GET deliveries/{id}/metrics-history` (proves the snapshot
   handler still fires after a forecast update), `GET portfolios/{id}/delivery-rules/schema` and `GET …/delivery-sources`,
   on two revisions, and compares the bodies. Fails when nothing was compared. If demo data holds no Delivery, A3 reports it.
4. **Not observable by EXAMINE**: publishing a forecast to a real Jira release and syncing from a Jira release need a
   live tracker. They stay with the existing acceptance tests (`Slice02SourceSync…`, `Slice04ForecastPublishing…`), the
   moved unit tests and the Jira live suite in CI. Stated as a limit.
5. Declared in `.nwave/examine.json` before the first EXAMINE.

### D-13 Mutation bar — RECOMMENDED

Last, on frozen code, per CLAUDE.md "per-feature": **minimum kill rate 80 %** of the changed and moved code, **and** "no
survivor or no-coverage mutant added in the moved code" (S02 bar; the stricter of the two holds). Whole-file `mutate`
entries for the 13 files; same filter on old and new paths; acceptance suite excluded. C2's new code (the port, the
constructor change) is covered by the renderer's merge tests and the Jira connector tests; a survivor there is killed with a
unit test.

## Wave: DESIGN / [REF] Slices

Every slice ≤ 1 day, its own oracle file, moves apart from edits. Three nwdes features. Size **M, about 6.5 days** (the
brief's L is waived down: see DoR 6).

| Id | nwdes feature | Delivers, in plain words | Oracle (new file; how it is RED first) | Production targets | Size |
|---|---|---|---|---|---|
| A1 | A `story-6362-s03d-a-rules-first` | Every architecture rule that names a Promises type, namespace or folder also sees the future `Modules/Promises`; a made-up Delivery service there that reaches a repository, Forecasting or the update code is caught by the module rules; S03a/S03b rule files read the Promises names | `Architecture/PromisesRulesSeeTheModuleArchUnitTest.cs` (reuses the `ForecastingRulesSeeTheModule` fixture approach). RED: the made-up offender under `Modules.Promises` passes today's rules | none (test-side: `ModuleRules` constant, E1-E4 rule files) | 1 d |
| A2 | A | The membership list and the `Models` admissions (D-2) are measured from the IL and fixed in a test; E5 and the Forecasting/Flow-metrics edges confirmed | `Architecture/PromisesMembershipArchUnitTest.cs` (RED: fails when a listed type is missing or the list names a type that does not exist) | none | 0.5 d |
| A3 | A | A Delivery-answers EXAMINE command exists before any file moves, and it reports a changed Delivery answer | `Scripts/examine/test-deliveries-answers-compare.sh` (one file, **behavioural RED**): runs the command's compare stage on two fixture capture folders that differ in one Delivery answer, then on two identical folders. RED: the command does not exist; GREEN needs `DIFFERENT` (exit 1) for the first pair and `IDENTICAL` (exit 0) for the second, so a command that always prints `IDENTICAL` fails. Whether the compare stage can take capture folders: unknown, measured in A3; if not, A3 splits that stage out | `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | 0.5 d |
| A4 | A | CI selection is pinned for Promises paths and for C2's paths | extra cases in `Scripts/test-selection/test-path-classifier.sh` (its one oracle file): a `Modules/Promises/**` source and test path select backend only (RED: no case); `Modules/TrackerIntegration/Application/IDescriptionBlockMerger.cs` and `Program.cs` select all live suites | `Scripts/test-selection/path-classifier.sh` only if a case is red | 0.5 d |
| B1 | B `story-6362-s03d-b-move` | `DeliveryForecastBlock` lives in `Promises/Domain`; the renderer, rule service, source resolver, source sync, forecast publishing (+ their interfaces) and the projector live in `Promises/Application`; S03a/S03b rule-list strings follow | `Architecture/PromisesLayoutArchUnitTest.cs` (RED: types in old namespaces; Domain reaches only the kernel). Structural RED, acceptable for a layout move: behaviour is proved by the unchanged tests and EXAMINE | 11 files (+1 `Models`) + 7 tests, `Program.cs` usings, S03a/S03b list strings | 1 d |
| B2 | B | The two "forecasts updated" handlers live in `Promises/Application/Policies`; **the order pin for `PortfolioForecastsUpdated` is added** (snapshot recording, then publishing) | `Architecture/PromisesPoliciesLayoutArchUnitTest.cs` (RED: types under `Services.Implementation.DomainEvents`; the order of the two registrations is read from the composition root) + one new entry in `DomainEventHandlerOrderPinTest` (RED: no entry for the event today). Layout RED is structural, acceptable for a move | 2 files + 2 tests, `Program.cs`, `HostUsersStillToMove` line per D-9 | 0.5 d |
| C1 | C `story-6362-s03d-c-edges-and-rules` | Promises has its own module rules with inline lists; Work Items and Tracker integration lists shrink (Promises legal by namespace) | `Architecture/PromisesModuleBoundariesArchUnitTest.cs` (RED: a made-up offender per list position; falsified per rule with `ModuleRulesCatchANewOffender`) | rule files; entries removed from S03a/S03b lists | 1 d |
| C2 | C | The Jira connector reaches no Promises type: it takes the ACL's `IDescriptionBlockMerger`, which the renderer (in `Promises/Application`) implements; written bytes unchanged | `Architecture/TrackerIntegrationReachesNoPromisesArchUnitTest.cs` (RED: the S03a R-T7 list has one entry) + the existing renderer merge tests and Jira write-back tests green unchanged (same bytes) | `Modules/TrackerIntegration/Application/IDescriptionBlockMerger.cs` (new), `JiraWorkTrackingConnector` (ctor type), `IDeliveryForecastBlockRenderer` (drops `MergeInto`), renderer (implements port), `Program.cs`, `JiraDeliverySourceProviderTest` registration | 1 d |
| C3 | C | Every Promises unit test sits under `Tests/Modules/Promises`; no Promises file is left in `Services` apart from the named exceptions | `Architecture/PromisesUnitTestPlacementTest.cs` + "no file left" path check (RED: a made-up test or source file in an old folder is caught) | none | 0.5 d |

A2's and B1's REDs are structural (types are in the old place); that is the right RED for a pure layout move, and the
behavioural proof is the unchanged existing tests plus EXAMINE. A3 is the one slice with a behavioural RED.

Verification commands per slice (fast loop, then the full gate at the end of the slice):

```
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~<OracleClass>"
bash Scripts/test-selection/test-path-classifier.sh                      # A3
bash Scripts/examine/test-deliveries-answers-compare.sh                  # A3
bash Scripts/examine/build-examine-commands && bash Scripts/examine/bin/deliveries-answers-compare   # EXAMINE
dotnet build Lighthouse.Backend                                          # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

Why three features: rules only; pure moves; edges, the one edit and placement. B2 and C1 may swap order; C2 needs B1
(the renderer already in `Promises/Application`). Finalize (not nwdes slices): mutation on frozen code (D-13); ADR-270,
`ARCHITECTURE.md` §4, brief ACs (D-2, DoR 5), catalogue, ADR-270 "Left open" (D-5, D-6); the Lighthouse finalize checklist.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list inline, compared both ways, never loosened (ADR-268).

### Rules first (A1): what is widened

| File | Keys on | Widening |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | `PortfolioDeliveryPattern` (update code only, E1) | unchanged meaning; add `PromisesPattern` = `^Lighthouse\.Backend\.Modules\.Promises(\.\|$)` plus the old Delivery namespaces; S03a's "add the delivery sources to the Portfolio/Delivery side" happens here **only after** the renderer entry is listed (it is, in S03a C1) |
| `TrackerIntegrationRulesSeeTheModuleArchUnitTest` | fixture `AModuleConnectorReachingADeliverySource` names the old interface namespace | fixture reads the type through `typeof`; follows the move in B1 |
| `WorkItemsModuleBoundariesArchUnitTest` | list string `Services.Implementation.DeliveryRuleService`; `WorkItemsLegalCallers` | B1 renames the string; C1 adds Promises to the legal callers and deletes the entry |
| `TrackerIntegrationMembershipArchUnitTest` / S03a boundaries file | R-T4 entries `DeliverySourceResolver`, `DeliveryForecastPublishingService`; R-T7 entry `JiraWorkTrackingConnector -> IDeliveryForecastBlockRenderer` | B1 renames; C1 removes the R-T4 entries (legal by namespace); C2 removes R-T7's |
| `ForecastingModuleBoundariesArchUnitTest`, `RefinementModuleArchUnitTest`, `RepositoryLeakRatchetArchUnitTest`, `DeliveryRuleServiceApiPreservationTest`, `TestHostUseGuardTest.HostUsersStillToMove` | type names, `typeof`, strings | `using`-only; one string renamed in B1 |

### New rules (C1), baselines measured from the IL in A2 and C1

| # | Rule | Expected baseline |
|---|---|---|
| R-P1 | Each listed type lives in its layer; none left under `Services.*.DeliverySources`, `Services.Implementation` root for the set (`ModuleRules.TypesLiveInTheirLayer`) | D-1 lists |
| R-P2 | `Domain` reaches only `Models` and the BCL (one type, `DeliveryForecastBlock`; the rule is dropped if A2 does not admit it) | empty |
| R-P3 | Outside → `Domain` only from the list | empty (the renderer reaches the block, but it is inside the module) |
| R-P4 | Outside → `Application` from `API`, the composition root (legal by namespace), plus the list | `PortfolioUpdater` (rule service, sync service: candidate 4, Epic 6474 slice 12). Anything else the IL finds is added only with a reason. |
| R-P5 | No outside type reaches `Application/Policies` except the composition root | empty |
| R-P6 | `Application` → no EF Core, `Data`, `API`, any module's `Adapters`; Work Items and Tracker integration `Application` legal by namespace; Forecasting and Flow metrics `Application` legal if found; platform only through the list | `ILighthouseClock`, `IDeliveryRepository`, `IDeliveryMetricSnapshotRepository`, `RecordableDeliveries`, the dispatcher contract (all by name) |
| R-P7 (leaf toward upstream) | Promises reaches no `Adapters` of another module, no update code, no write-back round, no Access or Licensing | empty (to confirm from the IL; the text scan found none) |
| R-P8 | Tracker integration reaches no Promises type (restates S03a R-T7) | 1 entry until C2, then **empty** |
| Work Items / Tracker integration | Inbound from Promises | legal by namespace; their lists lose 3 entries (`DeliveryRuleService`, the two Delivery services) |

## Wave: DESIGN / [REF] Component decomposition

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `DeliveryForecastBlock` | `Models/DeliverySources` | `Domain/` | MOVE | B1 |
| Renderer, rule service, resolver, sync, publishing (+ 5 interfaces), projector | `Services/Implementation`, `…/DeliverySources`, `Services/Interfaces` | `Application/` | MOVE | B1 |
| 2 handlers | `Services/Implementation/DomainEvents` | `Application/Policies/` | MOVE | B2 |
| `IDescriptionBlockMerger` (new), Jira connector ctor, renderer interface | | `TrackerIntegration/Application`, `Promises/Application` | CREATE_NEW 1 file + EDIT | C2 |
| Rule files, scripts | tests, `Scripts` | same | EDIT | A1, A3, C1 |
| Update code, write-back round, ordering and ranking, repositories, entities, controllers, DTOs, events | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: Deliveries, Delivery notes, Delivery rules and Delivery sources controllers (routes, `RbacGuard`s,
responses untouched). Host: the Portfolio refresh (`PortfolioUpdater` calls the rule service and the sync service).
In-process: `PortfolioForecastsUpdated` handlers. Promises → connector port (D-7).

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IDeliveryRepository`, `IDeliveryMetricSnapshotRepository` | platform repositories | stay (D-4) |
| `IWorkTrackingConnectorFactory`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher` | Jira connector (S03a's module) | unchanged; the Promises edge is legal |
| `IDescriptionBlockMerger` (new, C2) | `DeliveryForecastBlockRenderer` | new in-process port, no external call |
| `ILighthouseClock`, domain-event dispatcher | platform | unchanged, listed |

No new external call, adapter or package.

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET, `IlDependencies` over Mono.Cecil, NUnit, Stryker.NET, bash scripts, the existing EXAMINE capture pieces.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `ModuleRules` + `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | module rules and falsifiers | **REUSE**; no rule copied; add a `PromisesModule` constant |
| `ForecastingRulesSeeTheModuleArchUnitTest` / `TrackerIntegrationRulesSeeTheModuleArchUnitTest` fixtures | made-up offender under a module | **REUSE** the approach for A1 |
| `TrackerIntegrationMembershipArchUnitTest`, `WorkItemsMembershipArchUnitTest` | membership list | **REUSE** shape for A2 |
| `ForecastingUnitTestPlacementTest` | placement | **EXTEND** (parameterise) or sibling, decided in C3 |
| `ModuleBoundariesArchUnitTest.PortfolioDeliveryPattern` | update-code pattern | **REUSE** as is (E1); **CREATE_NEW** `PromisesPattern` |
| `forecast-answers-compare` and its shared capture pieces | Delivery likelihoods | **REUSE** for likelihoods; `deliveries-answers-compare` **CREATE_NEW** thin command on the same pieces (it needs other endpoints and a different "nothing compared" message) |
| `ef-model-unchanged`, `architecture-rules`, `backend-tests` | behaviour-free proof | **REUSE** |
| `IWorkTrackingConnectorFactory` + capabilities (S03a) | Promises' route to the tracker | **REUSE**; a query wrapper for Promises rejected (pass-through) |
| CREATE_NEW | `Modules/Promises` tree, 9 oracle files, `IDescriptionBlockMerger` | no existing owner of the module's layout or rules; the merge port has no owner because the renderer's `MergeInto` sits behind an interface Promises owns |

Semantic duplicates checked: none found. `DeliveryRuleService` (which Features match a Delivery's rules) and the
Refinement stage rule matcher both use the Work Items rule evaluator but decide different questions; both stay separate.

## Wave: DESIGN / [REF] Impact closure

| Locator | Kind | Decision | Reason |
|---|---|---|---|
| 13 production files of D-1 (+ `DeliveryForecastBlock`) | source | CHANGED (moved) | D-1, D-2 |
| `Program.cs` | composition root | CHANGED (`using`s, names, one line in C2) | D-10; forces full CI suite |
| `API/DeliveriesController`, `DeliveryRulesController`, `DeliverySourcesController` | consumers | CHANGED (`using`s) | |
| `PortfolioUpdater` | consumer | CHANGED (`using`s only; generic `GetRequiredService<T>()` names the new namespaces) | candidate 4 stays |
| `JiraWorkTrackingConnector`, `Modules/TrackerIntegration/Application` | consumer / new port | CHANGED (C2) | D-7 |
| 9 moving unit tests, 13 Jira tests, `JiraConnectorTestSetup`, 29 tests `using` the `DeliverySources` namespaces | tests | MIGRATED (moved) / CHANGED (`using`s) | D-9 |
| Rule files of E1 to E4 | tests | CHANGED | A1, B1, C1 |
| `Scripts/examine/build-examine-commands`, `.nwave/examine.json`, `test-path-classifier.sh` | scripts | CHANGED | A3 |
| `.github/workflows/*`, `path-classifier.sh` regexes, `test-time-budget.json`, Stryker configs | CI / timings / mutation | UNCHANGED_COMPATIBLE | E8: nothing names a Promises path; A4 adds test cases only. The Stryker config for the mutation run is written at close-out |
| `Lighthouse.Backend.csproj`, `.Tests.csproj` | build | UNCHANGED_COMPATIBLE | SDK globbing; to confirm no item names a moved path (A3) |
| `appsettings*.json` | config | UNCHANGED_COMPATIBLE | no key names a moved type (text search) |
| EF snapshots, migrations | persistence | UNCHANGED | D-0 |
| Frontend, Lighthouse-Clients, website | | UNCHANGED | no wire change |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, UI unchanged; no schema change (D-0). C2 changes a constructor type, not the written text. |
| RBAC | N/A, because no endpoint, guard or authorisation call changes. |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing a user does changes (see DEVOPS). |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. "Delivery" stays the code word; the folder is `Modules/Promises`. |
| Log categories | Change with the namespace for every moved logger; no configuration key names them. |
| `Program.cs` | Edited in B1, B2 and C2: those pushes run the full CI backend suite and every live suite (E8, D-11). |
| Epic 6474 | No shape reversed: `PortfolioUpdater` stays and keeps its two Promises calls as listed edges. When slice 12 turns them into events, R-P4 loses its entry. |

## Wave: DESIGN / [REF] Residuality

Stressors from negated assumptions, walked against the finished design (walkthrough, not execution).

| Assumption negated | What happens | Preserved / forbidden | Residue, owner |
|---|---|---|---|
| Handler order of `PortfolioForecastsUpdated` is free | Unpinned today (E5); a registration reorder in `Program.cs` could change which runs first | Same order as today, **pinned by B2** | A2 measures whether the handlers depend on each other |
| Live suites are selected by path | Holds for connectors; no regex names Promises (E8). But `Program.cs` and the Tracker integration `Application` are SHARED paths: B1, B2 and C2 select all live suites | A connector change runs its suite; no live suite is skipped | the 429 risk on the shared Linear key (D-11) |
| DI resolves by type, not namespace | Holds; `GetRequiredService<T>()` in `PortfolioUpdater` is generic, so the compiler follows the move | — | non-generic or string lookups: none found by search; the full suite would catch one |
| Moving the renderer changes written text | `Render` and `MergeInto` stay in one class; C2 moves no logic | Same bytes written to Jira | existing renderer and Jira write-back tests, unchanged |
| The old S03a lists hold the renderer's old name | They fail on rename until edited (both-way comparison) | Never loosened | B1 and C1 edit them in the same change |
| S03a is not closed when S03d starts | Files and lists are in flux | Nothing starts | start condition |
| Epic 6474 slice 12 lands first | `PortfolioUpdater` is replaced; R-P4's entry goes away | List only shrinks | owner Epic 6474 |
| EF keys on namespaces | Entities stay in `Models` (D-0) | No migration | `ef-model-unchanged` |
| The EXAMINE demo data holds no Delivery | The new command compares nothing and fails loudly | No false green | A3 |

Second set, not used to derive the design: a worktree without the premium licence fixture (2 Licensing failures,
environmental); a parallel S03i moving Portfolio parts (it owns `Portfolio`, repositories and ordering; no file here);
the Standalone build packaging the backend (a namespace change does not change the assembly name).

## Wave: DESIGN / [REF] Formal verification

**Not applicable, with the reason.** The formal-verification skill was read (procedure and checker selection). The
claims are: (1) "the same types exist in other namespaces and the edges are exactly the listed ones", decided exactly by
the compiled IL with a made-up offender per rule as falsifier; (2) "behaviour is unchanged", decided by the existing
suites and the answer comparisons, since both sides run the same code; (3) C2: "the merge function is the same", a pure
string function whose existing example tests run unchanged. No state, ordering, concurrency, recovery or liveness claim
changes (no handler, queue or schedule moves; D-10). A Quint or TLA+ model would restate the namespace map and could not
fail. **No checker (Quint, TLC, Agda) was run.** A probe was attempted in this session and the shell refused it
(`agda` is not on the shell allowlist), so tool availability is **not established**. If the maintainer wants a model, the
claim is **INDETERMINATE** until a checker is probed and run.

## Wave: DESIGN / [REF] Test substrate

- C# .NET 10, NUnit 4.6, Mono.Cecil via `IlDependencies`; bash for the classifier tests and EXAMINE commands. No new package.
- Oracles are architecture tests (`Lighthouse.Backend.Tests/Architecture/*`), the classifier script and one EXAMINE command;
  the claims are about types, edges and selection, not behaviour. Behaviour stays covered by the existing scenarios at the
  application-service port (`DeliverySources` acceptance tests, `DeliveriesController` integration tests), which do not move.
- Timing: each new architecture test reads the cached IL module like the existing ones; inside the default budgets.
  Formal laws: none, so no executable projection is owed. No derogation requested.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature C.

- **ADR-270**: Promises row: "**Packaged** in `Modules/Promises` by Story 6362 (date). Entities, the kernel Delivery types and wire DTOs stay in `Models` and `API/DTO`. Delivery persistence stays with the platform repositories until S05d; the Portfolio refresh calls stay in the update code until Epic 6474 slice 12." Decision 6 folder counts. "Every edge": Tracker integration → Promises row: removed by C2 (or kept, one entry); ratchet candidate 3 answered by S03a, 4 unchanged. "Left open": Feature ranking (not Promises; Organisation set-up, placed by S03i), write-back (not Promises; owner proposed, no ADO item filed yet), update queue (not Promises). New block "Edges measured by packaging Promises" (R-P1 to R-P8).
- **ARCHITECTURE.md** §4: Promises row with `Modules.Promises.*` anchors; the diagram node; the Portfolio/Delivery wording now means "update code".
- **brief.md**: new section `## Application Architecture — story-6362 s03d package promises`.
- **Epic catalogue**: Improvement log; KPI 1/2/3; slice-06 brief ACs and IN list corrected; S03i note (ordering and ranking, Portfolio); S05d note (Delivery repositories wait); the proposed write-back item (not filed until the maintainer confirms).

## Wave: DESIGN / [REF] Architecture summary

**Shape in hexagonal terms.** Everything below is MOVED unless marked; one new in-process port in C2; no new external call.

```
 driving                    Modules/Promises                                   neighbours
 ───────                    ──────────────────────────────────────────         ──────────────────
 Delivery controllers  ──►  Application: rule service, source resolver,        Work Items Application
 (unchanged)                  source sync, forecast publishing, projector ──►   (rule evaluator)   [legal]
 PortfolioUpdater      ──►    (listed caller, Epic 6474)                       Tracker integration
 PortfolioForecasts-   ──►  Application/Policies: snapshot handler,   ──►        Application: factory +
 Updated (Forecasting)        publishing handler                                 connector port    [legal]
                            Application: forecast block renderer ──implements──►  IDescriptionBlockMerger (C2)
                            Domain: DeliveryForecastBlock (value)
                                        │                                       used by the Jira connector
                                        ▼
                            Models kernel (Delivery entities, source types: unchanged)
                            Platform: Delivery repositories, clock, dispatcher (unchanged, listed)
```

- Driving adapters: controllers (unchanged), the Portfolio refresh (unchanged, listed), two event handlers (moved).
- Application services and ports: rule service, source resolver, source sync, forecast publishing, projector (moved).
- Domain: the forecast block value (moved). Rendering and merging stay together in `Application` (D-7).
- Driven adapters: none of Promises' own; the Jira connector stays in Tracker integration.

**Patterns weighed.**

| Pattern | Verdict | Why |
|---|---|---|
| Dependency inversion for the Jira merge (`IDescriptionBlockMerger`) | chosen (C2) | the ACL owns the port, Promises implements it; removes the one upward edge, bytes unchanged |
| Ports next to their use case | chosen | ADR-270 Decision 6 |
| Domain event reaction as a policy (`Application/Policies`) | chosen (kept) | the two handlers already are events; only their folder moves |
| Query service wrapping the connector factory for Promises | rejected | pass-through layer; the factory and capabilities are the ACL's port (S03a D-7) |
| Hand merged text to the connector as data | rejected | the connector alone holds the current description; needs a new read capability |
| Move the whole renderer into Tracker integration | rejected | splits markers from composition; they must live together |
| Move update queue and updaters into Promises | rejected | wrong owner; Epic 6474 slice 12 replaces them |
| Move write-back round into Promises | rejected | reverse edge (Forecasting → Promises) |
| Move repositories to `Adapters/Persistence` | rejected for now | S05d owns the Portfolio-side ports |
| Events for the Portfolio refresh's two Promises calls | rejected for now | not behaviour-free; ADR-276 / Epic 6474 owns it (S03b D-3) |
| Rename `PortfolioDeliveryPattern` | rejected | three readers, no behaviour gain; add `PromisesPattern` |

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-17)

- Platform lens: N/A, because nothing deploys, configures or runs differently. The one CI concern (path-based live-suite
  selection) was measured (E8) and needs one scenario (A3), not a platform change.
- Usage-data event: **N/A, because nothing a user sees or does changes.**

## Wave: DESIGN / [REF] Open questions for the maintainer

None blocks DELIVER. Confirmations, each with the AFK default taken:
1. **Start order**: S03d starts only after S03a is closed (A, B and C). Agree?
2. **D-5 and D-6**: Feature ordering and ranking stay for Organisation set-up (placed by S03i); the write-back round stays in
   `Services` as a named exception. Both leave the brief's IN items. Shall I file one ADO item "Place the write-back round
   (candidate: Forecasting `Application/WriteBack`)", owner candidate the Forecasting module, under Epic #6510? **Nothing is
   filed yet.** Recommendation: yes.
3. **C2 (the only production-surface change; it changes a closed module's public surface and costs one full live run)**: keep
   it in S03d, or make it its own Story and keep the S03a R-T7 entry until then? Recommendation: **keep it in S03d as the
   last slice of feature C**: S03a D-7 gave this edge to S03d, a Story for one day of work costs more in ADO and rule-file
   churn than it saves, and C2 can be dropped at any point without touching B. If you prefer the split, C2 and A4's C2 cases
   leave this Story and S03a's R-T7 keeps one entry.
4. **Push cadence (D-11)**: push B1 and B2 together and C1 to C3 together (2 full live runs, recommended), or per slice (3)?
5. **Size**: M, about 6.5 days, 9 slices, 3 features, not L (ADR-270 moved the update code, write-back, ranking and
   persistence out). Agree?
6. **Rigor profile** per feature is proposed at the start of DELIVER (as S03a did); UI sketch: N/A.

Not verified at DESIGN (each named with the slice that measures it): IL edges (all inbound/outbound lists above are
`noisy` text search; A2/C1); D-2 admissions (A2); whether the two `PortfolioForecastsUpdated` handlers depend on each
other (A2); demo data holds a Delivery (A3); csproj item paths (A3); the exact content of the 29 test files that `using`
the `DeliverySources` namespaces (B1); whether Promises reaches Forecasting or Flow metrics `Application` (A2, none found by text search).
