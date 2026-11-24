# Feature: story-6382-s09a-context-contracts
Story #6382 · S09a Context contracts + contracts-only cross-context rule · Epic #6510 (Modules: package the bounded
contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S09a row line 347, notes from S02 lines 356-359, G4 line 72, dependency graph line 93, D-6264-7, D-6264-16).
- Brief: [`slices/slice-26-s09a-context-contracts.md`](../epic-6264-modular-monolith/slices/slice-26-s09a-context-contracts.md)
  (Size M; "Update after S03b" and "no early start" blocks).
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  (Decision 2 context map and "Every edge", Decision 3 events, Decision 6 layout, Decision 7 kernel, "Ratchet
  candidates", "Left open"), ADR-027 D7 (dispatcher is transport only), ADR-268 (inline ratchet lists),
  `ARCHITECTURE.md` §4 and §5.
- Precedents copied, same shape: [S03h](../story-6366-s03h-package-reports/feature-delta.md) (format, preservation
  law, sweep), [S03i](../story-6468-s03i-package-organisation-set-up/feature-delta.md) (D-8 open host, pattern
  classification `Context` / `Place`), S03g (#6365, worktree `wise-sleeping-firefly`: the split into
  `a-rules-first`, `b-move`, `c-edges-and-rules`, and the shared `NamespaceMoveLaw.cs`).

DESIGN and DEVOPS run 2026-11-24, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a
user sees changes. Nothing was built or run with `dotnet` or `pnpm`; every count is a **text measurement on `main` at
`952040471`** (`git grep`, `find`, a scratch shell loop over `using` lines and declared type names), confidence
`noisy` (misses same-namespace and fully qualified references, matches XML-doc `cref`s and comments). The compiled IL
measurement in slice A1 decides. Measurements this DESIGN could not finish because their subject does not exist yet
are marked **"to re-measure at DoR re-run"**.

**Start condition.** S09a has a hard dependency on **all S03*** (maintainer, 2026-11-16, not cut down). In chain order:
S03g Refinement (#6365, being delivered in `.claude/worktrees/wise-sleeping-firefly`, features A and B accepted, C
designed), S03h Reports (#6366, designed), S03i Organisation set-up (#6468, designed), then #6657 "Place the write-back
round" (designed in parallel into `docs/feature/story-6657-write-back-round/`, not present on any checkout at
`952040471`; candidate home `Modules/Forecasting/Application/WriteBack`). All four are **merged into `main` before A1
starts**; "merged" is checked per Story by the three checks of S03h's start condition (ADO `Closed`; the final rules
commit found by `git log main --grep="story <id>" -- Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture` and
`git merge-base --is-ancestor <hash> HEAD` exit 0; the content check). Content checks for this Story: `git ls-files`
shows `Modules/Refinement/**`, `Modules/Reports/**`, `Modules/OrganisationSetup/**`, and no
`Services/Implementation/WriteBack*.cs` (#6657). A failed check stops A1 and is reported with the Story, the check and
its output. After S09a come S04b (#6368, reduce `Models` to the kernel) and S09b (#6383, one assembly per context).

## Wave: DESIGN / [REF] Inherited
- ADR-270 Decision 1: ten packaged contexts after S03i (Organisation set-up, Tracker integration, Work Items, Flow
  metrics, Forecasting, Promises, Refinement, Reports, Access, Licensing); Signals target only; the platform lane is
  composition-root infrastructure, **not a context** (D-6264-16), so the contracts rule does not govern it.
- ADR-270 Decision 2: the context map and its relationship types; the "Legal today" column is the direction authority.
  The per-module inbound rules (`ApplicationIsReachedFromOutsideOnlyBy` with each module's legal callers) encode it.
- ADR-270 Decision 3: domain events are the default way contexts react to each other; direct cross-context calls are
  for **queries** through the upstream's contract; each event type moves to the context that publishes it; the kernel
  keeps only the base type.
- ADR-270 Decision 6: `Modules/<Context>/{Domain, Application, Adapters}`, namespace = folder, ports co-located, rules
  read from the IL (`IlDependencies`), lists inline, compared both ways, only shrink. **It names no `Contracts` layer:
  this Story adds it** (amendment in slice D3).
- ADR-270 Decision 7: the kernel holds only cross-context identifiers, shared value objects and the event base type.
  `Models` is still far wider; S04b narrows it after this Story.
- S03i D-8: downstream modules reach Organisation set-up's `Application` and `Domain` **by right**; Tracker integration
  only along one listed edge (`ConnectionHealthCadence -> IAppSettingService`, owner S09a / Epic 6474). This Story
  narrows "by right" to `Contracts`.
- Catalogue notes from S02 (lines 356-359): registration extensions (`AddFlowMetrics` and the like) land here; the
  handler-order pin is the safety net; the inline edge lists (R3, R4, R5 and their successors) are the starting list of
  direct cross-context calls this Story sorts into contracts and events.
- Brief "Update after S03b": Work Items' `Program.cs` registrations stayed in place; `TeamDataRefreshedTeamMetricsUpdateHandler`
  must stay **first** among the `TeamDataRefreshed` handlers; `DomainEventHandlerOrderPinTest` fails on any other order;
  move registrations as a block and keep cross-module handler order explicit.
- Owners recorded by earlier Stories that name S09a: S03a D-14 (Tracker integration `Application` types that name
  connectors, factories' split namespaces, `AddTrackerIntegration`), S03b (`AddWorkItems`), S03c (`ForecastUpdater` →
  update code, ADR-270 line 254 "a candidate for S09a"), S03i D-8 (`ConnectionHealthCadence`).

## Wave: DESIGN / [REF] DoR re-run (2026-11-24, at DESIGN)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Contexts reach each other's `Application`, `Domain` and even `Adapters` by any public type: 27 cross-module `using`/name references in 19 files on `main` (E1), including a Flow metrics handler naming a Tracker integration **CSV adapter** constant (E3). Nothing separates "what a context publishes" from "how it works inside". |
| 2 Users / value | PASS | Maintainers: one rule says where another context may touch a context; S09b can weigh assemblies on clean seams; CI selection by module path becomes sound. No user-visible change. |
| 3 Hypothesis | PASS on today's measure, **re-measure** | Brief: "After S03*, cross-context references are few and concentrated; disproved if the baseline is large." Today: 38 type edges onto **~25 distinct target types**, of which only **5 are not interfaces, enums or constants** (E2) and would stay listed. After S03g/h/i and #6657 the set-up edges are added (S03i inbound: `IBlackoutPeriodService` 19 files, `IAppSettingService` 13, `IFeatureOrdering` 10, most from outside modules). **Threshold, decided here so A1 cannot argue with it:** the hypothesis is disproved if, after the published set is drawn (D-2), the **listed** (non-contract) cross-context edges exceed **15**, or any one context pair carries more than **5**. Then A1 stops and the finding goes back to S01 (ADR-270) as a wrong cut, per the brief. |
| 4 Size | **FAIL as M, re-sized** | **L, about 9.5 days, 4 nwdes features, 13 slices** (Slices). The M estimate predates two things the Story absorbed: the module start-up extensions (brief update after S03b) and the domain events moving into `Contracts` (D-5). Open question 1 offers splitting the module start-up into its own Story. |
| 5 AC from UAT | PASS, made precise | (a) "Every context has `Contracts`" restated: every context that **another context reaches** exposes exactly its `Contracts`; a context nobody reaches has no `Contracts` folder, and the rule records it as "reached by no context" (an empty C# namespace cannot exist; a placeholder type is ceremony) — open question 3. (b) "Contracts-only rule green, or ratcheted with a named-file baseline": green with an inline list of named **type edges** (finer than files), each with owner (D-3, D-8). (c) "Reaction-style calls moved where listed. Behaviour-free": the list is E6; one moves (D-6). (d) `ARCHITECTURE.md` §4 describes contracts (D3). (e) The payoff note on CI test selection (D-13). |
| 6 Dependencies | PASS (after the start condition) | Hard: all S03* incl. S03i; #6657 inserted before. Soft: S04b now comes **after** (chain order), not before as the catalogue's "S04b (soft)" assumed: this Story therefore defines `Contracts` on top of a wide kernel, and treats every `Models` type as kernel (D-4). |
| 7 Technical notes | PASS, with two CI targets | `Scripts/test-selection/path-classifier.sh` `SHARED_REGEX` names `Modules/TrackerIntegration/(Application\|Domain)/` (E8): the connector port moving to `Contracts` must be added there, or a connector-port change stops running the live suites. `Program.cs` is in `SHARED_REGEX` too: every slice that edits it runs every live connector suite once on push (D-12). |

## Wave: DESIGN / [REF] Baseline (`main` at `952040471`, text search, `noisy`)
Modules today: `Access` (32 files), `FlowMetrics` (47), `Forecasting` (32), `Licensing` (5), `Promises` (14),
`TrackerIntegration` (107), `WorkItems` (36). After the start condition: `Refinement` (16 production files in the S03g
worktree), `Reports` (31, S03h D-2), `OrganisationSetup` (27, S03i), and Forecasting's `Application/WriteBack` (#6657,
8 files expected) — **to re-measure at DoR re-run**.

Cross-module type edges (source context → target type), from `using` lines plus a name match of the target
namespace's declared types:

| Source | Target context · layer | Target types | Edges |
|---|---|---|---|
| Access `Application` | Licensing `Application` | `ILicenseService` (`AuthModeResolver`, `RbacAdministrationService`) | 2 |
| Work Items `Application` | Licensing `Application` | `ILicenseService` (`DependencyDecision`) | 1 |
| Forecasting `Application`, `Adapters` | Licensing `Application` | `ILicenseService` (`ForecastFilterRuleService`, `ForecastUpdater`) | 2 |
| Work Items `Application` | Access `Application` | `IRbacAdministrationService` (`FeatureDependencyRendering`) | 1 |
| Forecasting `Application` | Flow metrics `Application` | `ITeamMetricsService` (2), `ForecastThroughputStatus` (2), `IThroughputFilter`, `IForecastDataSufficiency`, `IHowManyForecaster` (implemented by Forecasting) | 7 |
| Work Items `Application` | Flow metrics `Application` / `Domain` | `IPortfolioMetricsService`; **`PercentileCalculator` (static, `Domain`)** | 2 |
| Flow metrics `Application/Policies` | Work Items `Application` | `IBlockedItemService` (2 handlers); `WorkItemBlockedTransitionCaptureHandler` (**`cref` only**, no IL edge expected) | 2 (+1 text-only) |
| Forecasting `Application`, `Adapters` | Work Items `Application` | `IRuleEvaluator`, `IRuleFieldProvider`, `IWhatTheForecastWaitsFor`, `IRefreshLogService` | 4 |
| Promises `Application` | Work Items `Application` | `IRuleEvaluator`, `IRuleFieldProvider`; **`RuleEvaluator<Feature>`, `FeatureFieldProvider` (concrete, `new` in `DeliveryRuleService`'s default constructor, line 15)** | 4 |
| Promises `Application` | Tracker integration `Application` | `IWorkTrackingConnectorFactory` (2), `IDeliverySourceProvider`, `IDeliveryForecastPublisher`, `IDescriptionBlockMerger` (implemented by Promises) | 5 |
| Work Items `Application` | Tracker integration `Application` / `Domain` | `IWorkTrackingConnectorFactory`, `IWorkTrackingConnector`, `IFeatureSource`, `IIncrementalSource`; **`ParentSourceSelector` (static, `Domain`)** | 5 |
| Flow metrics `Application/Policies` | Tracker integration **`Adapters/Connectors/Csv`** | `CsvWorkTrackingOptionNames` (constants; 2 demo backfill handlers) | 2 |
| Tracker integration | anything | none (S03a/S03d rules) | 0 |
| Refinement (worktree) | Forecasting `Application` (3 files), Flow metrics `Application` (2), Work Items `Application` (1), Access `Application` (1), Organisation set-up `WeeklyRecurrence` | measured by S03g; **to re-measure at DoR re-run** | ~8 |
| Reports (S03h baseline) | Flow metrics `Application` (`ITeamMetricsService`, `IPortfolioMetricsService`) and `Domain` (`DailySeriesSummary`), Licensing (`ILicenseService`) | **to re-measure** | ~5 |
| every downstream context | Organisation set-up (S03i D-8) | `IBlackoutPeriodService`, `IAppSettingService`, `IFeatureOrdering`, `ITerminologyService`, `WeeklyRecurrence`, … | **to re-measure** (S03i lists 7 module edges) |
| Organisation set-up | Work Items `Domain` (fetch fingerprint), Tracker integration `Application` (query port) | S03i R-OS3 | **to re-measure** |

Total today: **38 type edges, 19 source files, ~25 distinct targets**; 5 targets are concrete classes or statics.

Reference counts of the main published candidates (files, production / tests): `ILicenseService` 26 / 128,
`IRbacAdministrationService` 24 / 47, `ITeamMetricsService` 23 / 68, `IPortfolioMetricsService` 15 / 33,
`IWorkTrackingConnectorFactory` 13 / 39, `IWorkTrackingConnector` 9 / 44, `IBlockedItemService` 18 / 21,
`IRefreshLogService` 11 / 24, `CsvWorkTrackingOptionNames` 6 / 14, others ≤ 10 / ≤ 27. A move to `Contracts` changes
`using` lines in every one of them (about **120 production and 450 test files**, mostly the same two dozen namespaces;
**to re-measure** after S03i, which adds the set-up ports).

Domain events: 15 types in `Models/Events` plus `IDomainEvent`. Publishers (`new <Event>` sites): Work Items
`WorkItemService` (`FeatureBlocked`, `FeatureUnblocked`, `WorkItemBecameStale`, `WorkItemBlocked`,
`WorkItemTransitioned`, `WorkItemUnblocked`), Work Items `TeamDataService` (`TeamDataRefreshed`), the update code
`PortfolioUpdater` (`PortfolioFeaturesRefreshed`), Forecasting `ForecastUpdater` (`PortfolioForecastsUpdated`), the
set-up services moving in S03i (`BlackoutConfigurationChanged` ×2, `FeatureOrderingPolicyChanged`,
`FeatureRankChanged`, `ConnectionFetchShapeChanged`), and two API controllers (`TeamDeleted`, `PortfolioDeleted`).

`Program.cs`: 1,849 lines, 283 `Add*` registration lines, `RegisterServices` (from line 1346) and `RegisterReports`
(1324); the domain-event handler registrations sit at 1421-1423 (the Flow metrics update handler and the
`ReconstructionMemo` pair) and 1629-1654 (the rest). `TeamDataRefreshed` is handled, in this order, by Flow metrics,
Flow metrics, **Forecasting**, then five Flow metrics handlers: the order interleaves contexts.

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | The cross-context surface is small and concentrated on interfaces: of ~25 distinct targets, 20 are interfaces, 1 an enum (`ForecastThroughputStatus`), 1 a constants class (`CsvWorkTrackingOptionNames`), 3 concrete or static (`RuleEvaluator<T>`, `FeatureFieldProvider`, `PercentileCalculator`, `ParentSourceSelector`). | The published set can be drawn by a mechanical criterion (D-2), and the listed residue is small (hypothesis holds today). |
| E2 | Three published interfaces are **implemented** by another context (`IThroughputFilter`, `IForecastDataSufficiency`, `IHowManyForecaster` by Forecasting; `IDescriptionBlockMerger` by Promises). | `Contracts` holds both the queries a context answers and the ports it asks others to fill (dependency inversion already in place since S03c/S03d). |
| E3 | `DemoBlockedHistoryBackfillHandler` and `DemoPercentilesBackfillHandler` (Flow metrics) name `TrackerIntegration.Adapters.Connectors.Csv.CsvWorkTrackingOptionNames`, an **adapter** type, today. | Moves to `TrackerIntegration.Contracts`: the option key is published language for the CSV connection, read by another context. |
| E4 | `DemoBlockedHistoryBackfillHandler` names `WorkItemBlockedTransitionCaptureHandler` only in a `<see cref>` (line 18). | Text over-reports; the IL decides (no edge expected). A1's measurement is from the IL only. |
| E5 | Every existing inbound rule targets a **layer pattern**: `ApplicationIsReachedFromOutsideOnlyBy(… application: …Modules\.X\.Application …)`, `DomainIsReachedFromOutsideOnlyBy(… domain …)`, R-L3, R-A4, Flow metrics R3/R4, Work Items, Forecasting, Promises, Tracker integration, and the S03g/h/i rules to come. A type moved from `Modules.X.Application` to `Modules.X.Contracts` **leaves every such rule silently**: R-L3's 11 listed callers of `ILicenseService` would fail as stale (loud), but a **new** caller of the moved type would no longer be seen (silent). | Rules first (D-7): every pattern that matches a moving type's old name is classified `FrontDoor` (widened to `Application\|Contracts`, **permanently**: `Contracts` is part of the module's front door) or `Place` (a layer location that must stop seeing it). The shared `NamespaceMoveLaw` (S03g worktree) already carries the `Context` / `Place` roles; `FrontDoor` is its `Context` role scoped to a layer. |
| E6 | Direct cross-context **commands** (calls that change another context's state), as opposed to queries: only `ForecastUpdater.LogForecastRefresh` → Work Items `IRefreshLogService.LogRefreshAsync` (`ForecastUpdater.cs:380-399`) among the modules today. Other commands cross from code that is not a module: `PortfolioUpdater` → Promises and Forecasting (ratchet candidate 4, owner Epic 6474 slice 12), the API controllers publishing `TeamDeleted` / `PortfolioDeleted` (candidate 5), the licence reads (candidate 2, queries, owner Epic 6474 and #6657). Writes to tracker systems through Tracker integration's ports (`IDeliveryForecastPublisher`, `IWriteBackTarget`) are a customer using its supplier's port, legal by ADR-270. **To re-measure** after #6657 (the write-back round's calls). | The reaction list of the AC has one entry from a module and three owned elsewhere (D-6). |
| E7 | `DomainEventDispatcher.PublishAsync` resolves handlers in a **new DI scope** and **swallows** each handler's exception (logs it, continues). `IRefreshLogService.LogRefreshAsync` today runs in the updater's scope and its exception propagates to the updater. | Converting the refresh-log write to an event changes the failure path: a failed log write no longer reaches the forecast round. Recorded as an allowed degradation (D-6, residuality). |
| E8 | `path-classifier.sh` line 29 `SHARED_REGEX`: `^Lighthouse\.Backend/Lighthouse\.Backend/Modules/TrackerIntegration/(Application\|Domain)/` makes a change to the connector port run every live suite; `Program.cs$` is in the same regex. Users: `ci_changes.yml`, `dev-test.sh`, `test-path-classifier*.sh`, `Scripts/examine/bin/test-selection-show`. | C4 adds `Contracts` to that alternation in the same commit that moves the connector port (and `test-path-classifier.sh` gains the case). Without it, the move silently drops live-suite selection for the port. |
| E9 | `DomainEventHandlerOrderPinTest` reads the start-up registrations **without a host**, through the start-up code's own registration method filling a `ServiceCollection` (`StartUpDomainEventHandlers.TheStartUpRegistrations`). | The same seam can read **every** registration: the module start-up feature gets a preservation oracle at the composition-root port without starting a host (D-9). S03g feature B's EXAMINE rejection ("no public probe sees `Program.cs` order") is the lesson. |
| E10 | The S03i design adds `OrganisationSetupIsAnOpenHostArchUnitTest` and makes set-up's `Application` and `Domain` legal targets by namespace in six modules' rules. | Those six patterns are `FrontDoor` patterns for the set-up move to `Contracts` (C5) and narrow to `Contracts` at D3; the open-host oracle is rewritten to `Contracts` there. |
| E11 | `IlDependencies` reads generic arguments, attributes and `GetRequiredService<T>()` (ADR-270 candidate 7 correction); XML-doc `cref`s produce no IL. | The published-set closure (D-2) is computed from the IL; text measurements here are only estimates. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-24).

### D-0 No schema change, no behaviour change a user can observe — RECOMMENDED
No entity, `DbSet`, migration, route, DTO, payload member, usage-data event or configuration key changes. Event types
change namespace, not shape (no event is serialised: the dispatcher is in-process and the usage-data names are a
separate vocabulary, ADR-270 Decision 3). One allowed degradation on an infrastructure failure path (D-6). Checked by
`ef-model-unchanged` and an empty diff under `Lighthouse.Migrations.*`, `Lighthouse.Frontend`, `Lighthouse.EndToEndTests`.

### D-1 What a `Contracts` namespace is — RECOMMENDED: a fourth layer, `Modules/<Context>/Contracts`, namespace = folder, holding what other contexts may name
| Option | Verdict |
|---|---|
| **a (recommended)** Move the types other contexts reference into `Modules/<X>/Contracts` (`Lighthouse.Backend.Modules.<X>.Contracts`) | The namespace is the contract: a rule can key on it (GDP-8 property, not designation), S09b can turn it into an assembly boundary, and CI selection can key on the path. Mechanical move (`namespace` and `using` lines). |
| b A new facade per context in `Contracts` delegating to today's services | Rejected: every facade method is a pass-through over an existing port (simplest-design smell "pass-through layer"), doubles every query and adds a behaviour risk for no rule gain. |
| c Keep types in place and declare the published set as an inline list of type names per context | Rejected: a designation, not a property; the brief and catalogue AC require a namespace; S09b needs a folder. |
| d One shared `Lighthouse.Backend.Contracts` root for all contexts | Rejected: re-creates `Services/Interfaces` under another name; ownership is lost. |
`Contracts` may contain sub-folders only when one context publishes two unrelated groups (expected: none; Tracker
integration's connector port and its OAuth types are measured in A1).

### D-2 What goes into `Contracts` — RECOMMENDED: a measured closure, by a mechanical criterion
For each context X, the published set `P(X)` is the least set such that:
1. **Seeds:** every X type that a type of another context reaches in the IL **and** that is an interface, an enum, a
   record or struct without injected dependencies, a static class of constants, or a domain event type of X (D-5);
2. **Closure:** every X type named in the public signature of a type in `P(X)` (parameters, return types, base types,
   implemented interfaces, generic arguments, public fields and properties), unless it is kernel (`Models`) or BCL.

A cross-context reference to an X type that fails the seed criterion (a class with behaviour: a service, an
evaluator, a calculator, a handler, a static algorithm) is **not** moved: the edge is listed (D-3, D-8) with an owner.
Rationale: a contract promises a shape, not an implementation; publishing an algorithm would freeze it for every
caller. A1 computes `P(X)` from the IL and records it in `ContextContractsMembership` with, per type, old namespace,
new namespace and the reason (seed / closure). Expected from today's text (IL decides):

| Context | Expected `Contracts` (seeds; closure re-measured) |
|---|---|
| Licensing | `ILicenseService` |
| Access | `IRbacAdministrationService` (+ closure of its signature) |
| Flow metrics | `ITeamMetricsService`, `IPortfolioMetricsService`, `ForecastThroughputStatus`, `IThroughputFilter`, `IForecastDataSufficiency`, `IHowManyForecaster` |
| Work Items | `IBlockedItemService`, `IRuleEvaluator<T>`, `IRuleFieldProvider<T>`, `IRefreshLogService`, `IWhatTheForecastWaitsFor`; events (D-5) |
| Tracker integration | `IWorkTrackingConnectorFactory`, `IWorkTrackingConnector` and its capability interfaces in the closure (`IFeatureSource`, `IIncrementalSource`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher`, …), `IDescriptionBlockMerger`, `CsvWorkTrackingOptionNames`; after #6657 `IWriteBackTarget` |
| Forecasting | what Refinement and Promises read (S03g: `IForecastService`, `IForecastFilterRuleService`), `PortfolioForecastsUpdated` |
| Organisation set-up | `IBlackoutPeriodService`, `IAppSettingService`, `IFeatureOrdering`, `IFeatureOrderingPolicyProvider`, `ITerminologyService`, value types such as `WeeklyRecurrence` if they pass the seed criterion; its events |
| Promises, Refinement, Reports | none expected (nobody reaches them but `API` and the root): **no `Contracts` folder** (open question 3) |

### D-3 The contracts-only rule — RECOMMENDED: one shared rule over every context pair, its own enumerator, an inline list of type edges
`ContextContractsArchUnitTest` (new), built on `ModuleRules`:
- **R-C1 Surface:** a type in `Modules.A.*` reaches a type in `Modules.B.*` (B ≠ A) only if the target is in
  `Modules.B.Contracts`, except along `CrossContextEdgesOutsideContracts` (inline, full type names
  `Source -> Target`, compared both ways, only shrinks; each entry carries its owner in a trailing comment).
- **R-C2 Contracts point inward only:** a type in `Modules.X.Contracts` reaches only the kernel (`Models`), the BCL,
  `Microsoft.Extensions.*` abstractions, its own `Contracts` and other contexts' `Contracts` — never its own `Domain`,
  `Application` or `Adapters`, never `Data`, `API`, `Services` or the platform lane. Expected baseline: empty (the
  closure of D-2 makes it so by construction; a non-empty measure in A1 means the closure was not followed).
- **R-C3 The enumerator:** the set of contexts is read from the assembly (every `Lighthouse.Backend.Modules.<X>`
  namespace root) **and** compared both ways with an inline list of the ten packaged context names, so a new module
  folder fails until it is named, and a context that disappears fails too
  (`gate:predicate-needs-its-own-enumerator`).
- **Direction is not this rule's job.** Which context may reach which stays with the existing per-module inbound rules
  (legal callers per ADR-270), widened to `Application|Contracts` (D-7). Two orthogonal laws, one owner each: surface
  (this rule) and direction (per-module rules).
- **One owner per edge:** today some context-to-context edges are listed in a module's Domain or Adapters inbound list
  (e.g. `WorkItemService -> PercentileCalculator` in Flow metrics' R3, `ConnectionHealthCadence -> IAppSettingService`
  in Tracker integration). D3 moves the **module-to-module** entries into R-C1's list and narrows those per-module
  lists to outsiders that are not modules (`API`, `Services`, `Data`, the platform lane). Law: every edge listed
  anywhere before D3 is listed somewhere after D3 or has left the IL.

Every refusal names WHAT (the source, the target and its layer), WHY (the target's context may change it without
telling anyone) and HOW (name the type through `Modules.B.Contracts`, or move the type there if it is a contract, or
react to B's event) — `ModuleWording` per context.

### D-4 The kernel and the platform lane — RECOMMENDED: legal targets everywhere, unchanged
`Models` is the kernel until S04b, and every `Models` type is treated as kernel by R-C1 and R-C2 (even those ADR-270
Decision 7 will move out). The platform lane is infrastructure, not a context: R-C1 does not govern edges into
`Lighthouse.Backend.Platform.*`; the lane's own rules (R-PL1-R-PL4) stay. Code still in `Services` and `Data` belongs
to no packaged context yet: R-C1 does not see it; the per-module inbound rules keep governing it. S04b and S05*/S10
bring those types under R-C1 automatically when they move into a module.

### D-5 Domain events — RECOMMENDED: each event type moves into its publisher's `Contracts`; `IDomainEvent` stays in the kernel
ADR-270 Decision 3 says each event type moves to the context that publishes it, and the brief puts events in
`Contracts`. The 15 event types are small, not EF-mapped, not on any wire. Owner by ADR-270 Decision 3, which names
the owner even where today's publisher is not a module:

| Event | `Contracts` of | Today's publisher |
|---|---|---|
| `WorkItemTransitioned`, `WorkItemBlocked`, `WorkItemUnblocked`, `WorkItemBecameStale`, `FeatureBlocked`, `FeatureUnblocked`, `TeamDataRefreshed`, `PortfolioFeaturesRefreshed` | Work Items | `WorkItemService`, `TeamDataService`, `PortfolioUpdater` (update code, Epic 6474 slice 12) |
| `PortfolioForecastsUpdated` | Forecasting | `ForecastUpdater` |
| `TeamDeleted`, `PortfolioDeleted`, `BlackoutConfigurationChanged`, `FeatureOrderingPolicyChanged`, `FeatureRankChanged` | Organisation set-up | API controllers (candidate 5), set-up services |
| `ConnectionFetchShapeChanged` | **Organisation set-up** (its publisher, `ConnectionSaveService`, after S03i) | ADR-270 Decision 3 says Tracker integration; open question 5 |
Rejected: leave events in `Models` for S04b (the `Contracts` of Work Items, Forecasting and set-up would then lack
their most important member, and S04b would re-touch every handler); move only events whose publisher is a module
(splits one rule by today's accident of placement).

### D-6 Reactions to events — RECOMMENDED: list every cross-context command; convert the one inside the modules
| # | Call | Kind | Disposition |
|---|---|---|---|
| RX1 | Forecasting `ForecastUpdater` → Work Items `IRefreshLogService.LogRefreshAsync` (a forecast round's entry in the refresh log) | command, reaction to "a forecast round finished" | **Converted (D2):** Forecasting publishes `ForecastRoundFinished` (in `Forecasting.Contracts`: portfolio id and name, item count, duration, finished-at instant, outcome `Succeeded \| Failed \| Cancelled \| Skipped`); Work Items' `ForecastRoundFinishedRefreshLogHandler` (`Application/Policies`) writes the same `RefreshLog` row. Glossary term "forecast round" (ADR-270 Forecasting). Allowed degradation: a failed log write is logged by the dispatcher and no longer reaches the round (E7). |
| RX2 | `PortfolioUpdater` → Promises (`IDeliveryRuleService`, `IDeliverySourceSyncService`) and Forecasting (`IForecastUpdater`) | commands | Stays: not a module; candidate 4, owner Epic 6474 slice 12 |
| RX3 | `API.TeamController` / `PortfolioController` publishing set-up events | publisher placement | Stays: candidate 5, owner Epic 6474 slice 03 (S03i R-OS5) |
| RX4 | Refresh paths reading the licence (`TeamUpdater`, `PortfolioUpdater`, `ForecastUpdater`) | queries today; target: reactions to Licensing's facts | Stays listed (R-L3), owner Epic 6474 and #6657 |
| RX5 | Write-back round (after #6657) → Tracker integration `IWriteBackTarget` | customer writing through its supplier's port | Legal; **to re-measure** for any other command from the round |
A1 re-measures this list from the IL (every cross-context call to a member returning `Task` without a result or
`void`, read as a command candidate, then sorted by hand); a new module-to-module command found there is converted in
D2 if it is order-independent within its event, else listed with owner and an open question.

### D-7 Rules first — RECOMMENDED: EXTEND `NamespaceMoveLaw` with a contracts map and a `FrontDoor` classification
For every regex-valued field (const and static readonly) in the test assembly and every type in
`ContextContractsMembership` (moving into `Contracts`) and every event (D-5): declared `FrontDoor` (must see the type
before and after: the per-module `Application` inbound patterns, the open-host patterns of S03i) or `Place` (must not
see it after: layer-location patterns such as `TypesLiveInTheirLayer` inputs, `Models\.Events`, unit-test placement
roots). `IsMatch` evaluated by behaviour on old and new full names, catching alternations and interpolated readers
(S03h E1/E2 lessons). RED in A1: every `FrontDoor` pattern of the form `…Modules\.X\.Application(\.|$)` matches the
old and not the new name; GREEN once widened to `…Modules\.X\.(Application|Contracts)(\.|$)`. The widening is
**permanent** (no S3-style narrowing: `Contracts` stays part of the front door). The literal sweep (S03g/S03h) is
reused with its fixed rename list for string literals naming moving types and paths.

### D-8 The ratchet list — RECOMMENDED: lands complete in A2, shrinks in every C slice
A2 writes R-C1 with **every** measured cross-context edge outside `Contracts` listed (at A2 no `Contracts` exists, so
that is every edge: about 38 today, more after S03g/h/i; **to re-measure**). It is green on writing; its falsifiers
(planted types, `ModuleRulesCatchANewOffenderArchUnitTest` style) prove it bites. Each C slice's **RED oracle is the
same list with that slice's entries deleted**, observed RED before the move and GREEN after. After C5 the list holds
only the non-contract residue, each with owner: expected `DeliveryRuleService -> RuleEvaluator<Feature>`,
`DeliveryRuleService -> FeatureFieldProvider` (owner: Work Items, inject through the constructor instead of `new`, open
question 6), `WorkItemService -> PercentileCalculator` (owner: Epic 6474 slice 10 / S07a, already named by S03b),
`WorkItemService -> ParentSourceSelector` (owner: S03a's parent-source decision; Tracker integration keeps the
selectors because the connectors call them), and set-up value types that fail the seed criterion (**to re-measure**).

### D-9 Module start-up — RECOMMENDED: one `Add<Context>` per context at the module root; domain-event handlers stay in one ordered block in the composition root
| Option | Verdict |
|---|---|
| **a (recommended)** `Modules/<X>/<X>Module.cs`, `public static class <X>Module` in `Lighthouse.Backend.Modules.<X>` with `Add<X>(this IServiceCollection services, IConfiguration configuration)` registering the context's services, adapters and hosted services **in today's relative order**; `Program.RegisterDomainEventHandlers` keeps every `IDomainEventHandler<>` registration in one block, ordered by event, across contexts | The interleaved `TeamDataRefreshed` order (Flow metrics, Flow metrics, Forecasting, Flow metrics ×5) cannot be expressed by per-module blocks; keeping handlers in one explicit list keeps the cross-module order visible in one place, as the brief asks. Hosted services keep their start order because the `Add<X>` calls are made in the order their first hosted service is registered today (measured in B1). |
| b Handlers inside each `Add<X>`, order re-established by call order | Rejected: impossible for `TeamDataRefreshed` without splitting Flow metrics' extension in two. |
| c An ordering attribute on handlers, sorted by the dispatcher | Rejected: a behaviour change of the dispatcher (ADR-027 D7 transport only) for a problem a list solves. |
| d `Adapters/Composition/<X>Registration` | Rejected: registration is composition, not an adapter to the outside; a module-root type is the convention of modular monoliths and visible at a glance. |
Rules: only the composition root (`Lighthouse.Backend.Program`) names a `<X>Module` type; after B2, `Program` names no
type in a module's `Application` or `Adapters` other than the domain-event handlers it registers and the types its
host set-up needs (listed, measured in B1). `RegisterReports` folds into `AddReports`.

**Preservation oracle (B1):** `StartUpRegistrationsPreservationTest` reads the registrations through the same seam as
the order pin (E9) and compares, **per service type**, the ordered list of (implementation type or factory-produced
type, lifetime, key) with an inline baseline captured from the pre-move revision. Equality on that projection is
exactly container equivalence (multi-registrations resolve in order, single ones last-wins; order **between** service
types does not matter, except `IHostedService`, which is one service type and so covered). The baseline is temporary:
B3 deletes it once `Program` holds only `Add<X>` calls and the handler block (a 283-entry pin left forever would make
every later DI change a list edit). `DomainEventHandlerOrderPinTest` stays as the permanent pin.

### D-10 Tests move with production — RECOMMENDED: only `using` lines change; tests touched get better
Unit tests of moved types follow their type to `Tests/Modules/<X>/Contracts` only where a test class tests a contract
type alone (expected: `CsvWorkTrackingOptionNames` none, events none; measured in A1); every other test changes
`using` lines only. The unit-test placement rules of each module admit `Contracts` (`…UnitTestPlacementTest`
patterns are `Place` patterns: widened by layer list, not by narrowing). "Improve the tests you touch": the slices
touch hundreds of test files for one `using` line each; a `using` edit alone is not a touch that obliges a rewrite —
open question 7. Test doubles that spell a moved interface fully qualified are found by the literal sweep and the
build (ci-learnings "Widening a cross-cutting interface").

### D-11 Docs — RECOMMENDED: amend ADR-270 and `ARCHITECTURE.md` §4 in D3; no new ADR
ADR-270 Decision 6 gains `Contracts` as the fourth layer with the criterion of D-2 and the rule of D-3; Decision 3's
event table records where each event lives (and the `ConnectionFetchShapeChanged` owner, open question 5); "Every edge"
rows that say "through the kernel today" or "by right" are brought to `Contracts`; the "Ratchet candidates" list gains
R-C1's residue with owners. `ARCHITECTURE.md` §4 gains a "Context contracts" bullet (layer, rule, enumerator,
residue) and §5 names `ForecastRoundFinished`. Catalogue S09a row and the brief's AC are amended to D-5/AC (a).
Rejected: a new ADR (the layout decision is ADR-270's; a second document would compete).

### D-12 CI and pushes — RECOMMENDED: commit per slice, push per nwdes feature
Every feature edits `Program.cs` (`using` lines at least), so every push runs every live connector suite once. One push
per feature (4 live runs), only with the feature green locally (fast loop and the filtered full gate). C4 edits
`path-classifier.sh` and its test in the same commit as the connector port move (E8).

### D-13 Payoff note — RECOMMENDED: written into `ARCHITECTURE.md` §4, nothing implemented
A change only under `Modules/<X>/(Domain|Application|Adapters)` can, once R-C1's residue is empty, run only `X`'s tests,
the contract consumers' tests and the architecture tests; a change under `Modules/<X>/Contracts`, the kernel, the
platform lane, `Program.cs` or the migrations runs everything. Sound only as far as R-C1 is enforced and its list is
empty. Implementing it is Story #6302's.

### D-14 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse admitted probes, add none
| Expectation | Probe |
|---|---|
| X1 Same types, new namespaces; all rules green; R-C1/R-C2/R-C3 green with the residue list | `architecture-rules` (all) |
| X2 No EF model or migration change | `ef-model-unchanged` |
| X3 Same container: registrations per service type, handler order | `architecture-rules` (`StartUpRegistrationsPreservationTest` until B3, `DomainEventHandlerOrderPinTest`) |
| X4 Same answers at the application-service port; a forecast round still writes its refresh-log entry | `backend-tests` on unchanged-assertion acceptance classes, both revisions; the RX1 oracle (D2) |
| X5 CI selects the live suites for `Modules/TrackerIntegration/Contracts` and `Program.cs` | `test-selection-show` |

### D-15 Mutation bar — RECOMMENDED
Last, on frozen code: Stryker.NET over the files with a non-`using` change (the `<X>Module` files, the RX1 handler and
publisher, `ForecastRoundFinished`), compared with the pre-Story revision where a counterpart exists: no survivor or
no-coverage mutant added; the 80 % project bar reported. Pure namespace moves are not re-mutated (no logic changed).

### D-16 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the laws are executed exhaustively on the IL
The claims are structural relations over finite, closed sets (compiled types × contexts × regex fields × registrations):
R-C1-R-C3, the preservation law, the closure of D-2, and container equivalence as a per-service-type sequence
equality. No unbounded input, no concurrency, no temporal property. The one state change (RX1) is a single
publish-and-handle in an in-process dispatcher whose order and failure policy are already pinned (E7, the order pin);
its observation is an example at the service port (the entry exists with the same fields) plus the failure example
(a throwing log write is logged and the round completes). No checker is selected, so no tool probe, install or waiver
is needed; PBT: N/A (closed finite domains).

## Wave: DESIGN / [REF] Slices
**Split into four nwdes features** (13 slices; the nwdes maximum is 5 per feature), in this order, each depending on
the previous one being merged. Moves in commits apart from edits (`refactor(contracts): …`). Size **L, about 9.5 days**.

### Feature A — `story-6382-s09a-a-rules-first`
| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Est. | Rigor |
|---|---|---|---|---|---|
| A1 | **First action: the start-condition checks for #6365, #6366, #6468, #6657; then re-measure every count of this file (the DoR re-run section).** The published set measured from the IL (`ContextContractsMembership`: type, context, old and new namespace, seed or closure; the non-contract residue; the event owners of D-5; the command list of D-6); every regex field classified `FrontDoor` / `Place`; `FrontDoor` patterns widened to `Application\|Contracts`; hypothesis threshold of DoR 3 evaluated | `ContextContractsMembershipArchUnitTest` (every listed type exists in its old or new namespace; RED if a listed seed fails the criterion or the closure is incomplete) and the `NamespaceMoveLaw` run over the contracts map — **RED today** on every `…Modules\.X\.Application(\.\|$)` front-door pattern, GREEN after widening; the literal sweep with its fixed list | none | 1.25 d | thorough |
| A2 | The contracts rule exists and bites, with the complete baseline | **RED oracle**: `ContextContractsArchUnitTest` R-C3 (the inline list of ten contexts) written against an enumerator stub that returns nothing — observed RED, then the enumerator reads the assembly; R-C1 with every measured edge listed (green on writing), R-C2 (empty), planted-type falsifiers per rule and per list position | none | 0.75 d | thorough |

### Feature B — `story-6382-s09a-b-module-start-up`
| # | Delivers | Oracle | Production targets | Est. | Rigor |
|---|---|---|---|---|---|
| B1 | The container is pinned; Flow metrics, Forecasting, Promises, Licensing, Access register through `Add<X>` | `StartUpRegistrationsPreservationTest` (baseline captured on the pre-B1 revision, green on writing) and **RED oracle** `CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest` with these five contexts listed as "registered by their module" — RED until their registrations leave `Program` | `Modules/{FlowMetrics,Forecasting,Promises,Licensing,Access}/<X>Module.cs`, `Program.cs` | 1 d | thorough |
| B2 | Work Items, Tracker integration, Refinement, Reports, Organisation set-up register through `Add<X>`; the handler block stays in `Program.RegisterDomainEventHandlers` | the same rule with all ten contexts listed (RED), the preservation pin and the order pin unchanged | five `<X>Module.cs`, `Program.cs` (`RegisterReports` folded) | 1 d | thorough |
| B3 | `Program` keeps only `Add<X>` calls, host set-up and the handler block; the temporary pin goes | **RED oracle**: the composition-root rule's list of `Program`'s remaining module-internal names emptied to the handlers and the measured host types — RED until the strays move; then `StartUpRegistrationsPreservationTest` deleted in the same commit as its last green run is recorded | `Program.cs` | 0.5 d | standard |

### Feature C — `story-6382-s09a-c-contracts`
Each slice's RED oracle is R-C1's list with that slice's entries deleted (D-8); all other rules stay green.

| # | Delivers | Production targets | Est. | Rigor |
|---|---|---|---|---|
| C1 | Licensing and Access publish `Contracts` (`ILicenseService`, `IRbacAdministrationService` and closure) | 2 folders; `using`s in ~50 production and ~175 test files | 0.75 d | standard |
| C2 | Flow metrics and Forecasting publish `Contracts` (metrics queries, `ForecastThroughputStatus`, the three ports Forecasting fills; what Refinement reads of Forecasting) | 2 folders; `using`s | 0.75 d | standard |
| C3 | Work Items publishes `Contracts` (blocked items, rule evaluation ports, refresh log, what the forecast waits for) | 1 folder; `using`s | 0.5 d | standard |
| C4 | Tracker integration publishes `Contracts` (connector port and capability interfaces, the factory, `IDescriptionBlockMerger`, `CsvWorkTrackingOptionNames`, `IWriteBackTarget`); `SHARED_REGEX` and its test gain `Contracts` | 1 folder; `using`s; `Scripts/test-selection/path-classifier.sh`, `test-path-classifier.sh` | 0.75 d | thorough |
| C5 | Organisation set-up publishes `Contracts`; its open host narrows from `Application\|Domain` by right to `Contracts` | 1 folder; `using`s | 0.75 d | standard |

### Feature D — `story-6382-s09a-d-events-and-rules`
| # | Delivers | Oracle | Production targets | Est. | Rigor |
|---|---|---|---|---|---|
| D1 | The 15 event types live in their publishers' `Contracts`; `IDomainEvent` stays in the kernel | **RED oracle**: `DomainEventsLiveWithTheirPublisherArchUnitTest` (each event in the `Contracts` of its D-5 owner; no event type left in `Models.Events` but `IDomainEvent`) | 15 event files, handlers' and publishers' `using`s, `Program.cs` handler block `using`s | 0.5 d | standard |
| D2 | A forecast round's refresh-log entry is written by Work Items reacting to `ForecastRoundFinished` (RX1) | **RED oracle** at the application-service port: after a forecast round (success, failure, cancelled, skipped), the refresh log holds the same entry as today **and** `ForecastUpdater` names no `IRefreshLogService` (R-C1 entry deleted); failure example: a throwing log write is logged and the round completes; the order pin gains `ForecastRoundFinished` | `ForecastRoundFinished`, `ForecastUpdater`, `ForecastRoundFinishedRefreshLogHandler`, `WorkItemsModule` / handler block | 0.75 d | thorough |
| D3 | Rules by contract: per-module lists narrowed to non-module outsiders, module-to-module entries owned by R-C1 only; docs (D-11, D-13) | **RED oracle**: the "one owner per edge" law of D-3 (every module-to-module edge in a per-module list fails) — RED until those entries move to R-C1; S03i's open-host oracle rewritten to `Contracts` | none (test side and docs) | 0.75 d | standard |

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~RefreshLog|FullyQualifiedName~ForecastUpdater"   # D2
bash Scripts/test-selection/test-path-classifier.sh                                                                        # C4
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```
Driving port for the rule oracles: the compiled production assembly read through `LighthouseArchitecture.Production`
/ `IlDependencies` (in-process; nearest executable argv: the `dotnet test … ~Architecture` line). For B: the start-up
code's own registration method filling a `ServiceCollection` (in-process, no host; same argv). For D2: the
application-service port of the forecast round and the refresh log (the existing acceptance seam). Behaviour of
everything else is observed through the unchanged acceptance tests and the host tests.

Test time: the new tests reuse the IL loaded once per assembly and the registrations read once per fixture (shared
read-only worlds); each within the per-test budget; no infrastructure suite, no derogation.

Rigor (CLAUDE.md profiles, proposed): `thorough` (opus / sonnet) for A1, A2, B1, B2, C4, D2 (the law's
classification, the enumerator, the container pin, the connector port with CI selection, the one behaviour-bearing
conversion); `standard` (sonnet / haiku) for B3, C1, C2, C3, C5, D1, D3. Open question 2.

## Wave: DESIGN / [REF] Ratchet design
### A1 widening (permanent), D3 re-homing
| Owner | Pattern / list | A1 | D3 |
|---|---|---|---|
| Each module's `…ModuleBoundariesArchUnitTest` (Flow metrics, Forecasting, Work Items, Promises, Tracker integration, Access, Licensing, Refinement, Reports, Organisation set-up) | the `application` argument of `ApplicationIsReachedFromOutsideOnlyBy` and every alias of it (`FrontDoor`) | `…Modules\.X\.Application(\.\|$)` → `…Modules\.X\.(Application\|Contracts)(\.\|$)` | unchanged; module-to-module entries of their lists move to R-C1 |
| `ModuleRules` | `FlowMetricsDomain` default and other layer constants (`Place`) | unchanged; asserted to match no moving type | unchanged |
| S03i open-host patterns in six modules (`FrontDoor`, E10) | `…OrganisationSetup\.(Application\|Domain)` | `+ \|Contracts` | narrowed to `Contracts` (C5 made it true) |
| Unit-test placement tests (`Place`) | layer folder lists | `+ Contracts` admitted | unchanged |
| `DomainEventHandlerOrderPinTest`, seam tests naming `Models.Events` | `using` / literals | on the rename list | renamed in D1 |

### New rules, baselines from the IL
| # | Rule | Expected baseline (text; IL decides) |
|---|---|---|
| R-C1 | Module → other module only through its `Contracts` | A2: every edge (~38 + S03g/h/i, re-measured); after C5: the residue of D-8 (expected 4 + set-up value types) |
| R-C2 | `Contracts` → kernel, BCL, abstractions, `Contracts` only | empty |
| R-C3 | Contexts read from the assembly equal the inline ten | the ten of ADR-270 Decision 1 |
| R-C4 | Only `Program` names a `<X>Module`; `Program` names no module-internal type but the handlers and measured host types | B3 list (measured in B1) |
| R-C5 | Each event type lives in its D-5 owner's `Contracts` | the 15 of D-5 |
Lists inline, by full type name, compared both ways, only shrink. `ModuleWording` per context.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| Published interfaces, enums, constants and their closure (D-2) | `Modules/<X>/{Application,Domain,Adapters}` | `Modules/<X>/Contracts` | MOVE | C1-C5 |
| 15 event types | `Models/Events` | `Modules/<Publisher>/Contracts` | MOVE | D1 |
| `IDomainEvent` | `Models/Events` | — | UNCHANGED (kernel) | — |
| `<X>Module` ×10 | `Program.cs` registration lines | `Modules/<X>/<X>Module.cs` | CREATE_NEW (lines moved) | B1, B2 |
| `Program.RegisterDomainEventHandlers` | `Program.cs` 1421-1423, 1629-1654 | same file, one method | EXTEND (gathered) | B2 |
| `ForecastRoundFinished` | — | `Modules/Forecasting/Contracts` | CREATE_NEW | D2 |
| `ForecastRoundFinishedRefreshLogHandler` | the write in `ForecastUpdater.LogForecastRefresh` | `Modules/WorkItems/Application/Policies` | CREATE_NEW (behaviour moved) | D2 |
| Rules, membership, preservation pin | `Tests/Architecture` | same | EXTEND / CREATE_NEW | A, B, D |
| `path-classifier.sh` `SHARED_REGEX` + test | `Scripts/test-selection` | same | CHANGED | C4 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged — every controller, the hosted services, the update queue, the clients through the HTTP API. The
cross-context ports (now in `Contracts`) are the same interfaces with the same implementations; Forecasting still fills
Flow metrics' three ports and Promises still fills Tracker integration's `IDescriptionBlockMerger`. One new event path:
`ForecastUpdater` → `IDomainEventDispatcher` → `ForecastRoundFinishedRefreshLogHandler` → `IRefreshLogService` (inside
Work Items). No new external call.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| `NamespaceMoveLaw` (S03g; `Context` / `Place` roles, alternation-safe evaluation) | proving rules see a move | **EXTEND** with the contracts map and the `FrontDoor` reading of `Context` |
| S03g/S03h literal sweep | old-namespace literals | **REUSE** with a new fixed list |
| `ModuleRules` (`ApplicationIsReachedFromOutsideOnlyBy`, `OutboundEdges`, `TypesLiveInTheirLayer`), `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | edge rules with lists and falsifiers | **REUSE** for R-C1-R-C5; one new helper `ReachesOtherContextsOnlyThroughContracts` added to `ModuleRules` (the per-pair form does not exist) |
| `*Membership` classes | namespace-agnostic membership | **REUSE** shape for `ContextContractsMembership` |
| `StartUpDomainEventHandlers` (order pin seam) | reading registrations without a host | **EXTEND**: read every descriptor, not only handlers |
| `DomainEventDispatcher`, `IDomainEventHandler<T>` | cross-context reaction | **REUSE** for RX1 (ADR-027 D7) |
| `IRefreshLogService.LogRefreshAsync`, `RefreshLog` | the write RX1 performs | **REUSE** unchanged, called from the handler |
| `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
Semantic-duplicate check: no second owner of "what a context publishes" — the namespace is the owner, the membership
map its measured projection; no second owner of "who may call whom" — direction stays in the per-module rules, surface
in R-C1. `CREATE_NEW` only for the ten `<X>Module` holders (no type owns a context's registrations today) and RX1's
event and handler (no event carries "forecast round finished").

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| Published types of ten contexts (D-2; ~35 files + closure, re-measured) | production | MIGRATED | D-1, D-2 |
| `Models/Events/*` (15) | production | MIGRATED | D-5 |
| `Program.cs` | composition root | CHANGED | D-9, D1 `using`s |
| `Modules/<X>/<X>Module.cs` ×10 | production | CREATED | D-9 |
| `ForecastUpdater`, new event and handler | production | CHANGED / CREATED | D-6 RX1 |
| Consumers naming moved types: API controllers, `Services`, `Platform`, repositories (~120 files) | consumers | CHANGED (`using`s) | |
| Tests naming moved types (~450 files) | tests | CHANGED (`using`s) | D-10 |
| Every `…ModuleBoundariesArchUnitTest`, membership and placement tests, order pin, seam tests naming `Models.Events` | architecture tests | CHANGED | D-7, D3 |
| `Scripts/test-selection/path-classifier.sh`, `test-path-classifier.sh` | CI | CHANGED | E8 |
| `Scripts/test-timings/test-time-budget.json`, `HostUsersStillToMove`, `TestHostPerFixtureGuardTest`, `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, Stryker configs, `appsettings*.json` Serilog overrides | build / CI / runtime config | UNCHANGED_COMPATIBLE unless A1's re-grep finds a moved type or namespace (log categories of moved **interfaces** do not exist; implementations do not move) | S03h E5 lesson |
| `Data`, migrations, frontend, E2E, Lighthouse-Clients | — | UNCHANGED | D-0 |
| ADR-270 (Decision 3, 6, "Every edge", candidates), `ARCHITECTURE.md` §4 and §5, catalogue S09a row, brief AC | docs | CHANGED in D3 | D-11 |
| `docs/product/architecture/brief.md`, older ADRs | docs | UNCHANGED: history | |

Agreement analysis: no wire, schema or config contract changes (D-0); `not-applicable` for contract tests (single
deployable, producers and consumers change in the same commit). Architectural tests: applicable (R-C1-R-C5, the law,
the preservation pin). Prefactoring: feature A is the prefactoring (rules widened and the full baseline written,
production untouched, the suite GREEN before and after A).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly (input) → `ContextContractsMembership` + R-C3 enumerator (producer) → R-C1/R-C2 (consumer)
  → `dotnet test … ~Architecture` exit and message (public observation). Falsifiers: a planted type in
  `Modules.Refinement` naming `Modules.Forecasting.Domain.X` fails R-C1 with WHAT/WHY/HOW; a planted `Contracts` type
  naming its own `Application` fails R-C2; a planted `Modules.Signals` namespace fails R-C3. Registrations: start-up
  registration method (input) → descriptor projection (producer) → preservation pin and order pin (consumers) → same
  argv. RX1: forecast round (input) → `ForecastRoundFinished` (producer) → handler (consumer) → refresh log read at
  the service port (observation). DESIGN `PASS`, runtime `DESIGNED_NOT_BUILT`, proof N/A (D-16).
- Stressors (negated assumptions): (1) "moving a type keeps it under its module's rules" — false for every layer-keyed
  pattern (E5) → `FrontDoor` widening first; (2) "a text measure finds every edge" — false for `cref`s and fully
  qualified names (E4, E11) → IL only; (3) "per-module `Add<X>` keeps handler order" — false for `TeamDataRefreshed`
  → handlers stay in one root block; (4) "CI selection follows a folder move" — false for `SHARED_REGEX` (E8) → C4
  edits it; (5) "an event publish behaves like the call it replaces" — false on the failure path and the DI scope (E7)
  → allowed degradation recorded, failure example in D2's oracle; (6) "the predecessors left the counts as measured" —
  false by construction → A1 re-measures, the hypothesis threshold is fixed beforehand.
- Walked afterwards with a second set (walkthrough, not execution): a new module folder added by a later Story without
  a `Contracts` (R-C3 fails until named; survives); a `Contracts` interface whose signature drags an `Application`
  record (R-C2 fails; the closure of D-2 prevents it); a hosted service started in a different order after B (the
  preservation pin compares `IHostedService` as one ordered list; survives); a test double implementing a moved
  interface fully qualified (compile error, the sweep lists it; survives); `PortfolioFeaturesRefreshed` owned by Work
  Items while published by the update code (R-C1 does not govern `Services`; survives, owner Epic 6474 slice 12).
- Residue, with owner: R-C1's list after C5 (D-8 owners); RX2-RX4 (Epic 6474, #6657); the kernel still wide (S04b);
  code in `Services` invisible to R-C1 (S04b, S05*/S10).

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: **all ten packaged contexts** (each gains `Contracts` or is recorded as reached by none). Edges:
  none added or removed; every edge now passes through the upstream's `Contracts`, the residue listed with owners.
  Organisation set-up's open host narrows from "`Application` and `Domain` by right" to its `Contracts`.
- New terms: **context contract** (`Contracts`: what a context publishes for other contexts: the queries it answers,
  the ports it asks others to fill, and its events) — added to ADR-270 Decision 5's shared terms. New event
  **`ForecastRoundFinished`** (published by Forecasting, handled by Work Items). New concept per context: the module
  start-up extension (`<X>Module.Add<X>`).
- Domain events: published — `ForecastRoundFinished` (new); every existing event now lives with its publisher (D-5);
  reacted to — Work Items reacts to `ForecastRoundFinished`.
- UI surface: none.
- ADR-270 / `ARCHITECTURE.md` §4 updates: D3 tasks in Impact closure.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] #6365, #6366, #6468, #6657 merged by the checks of the start condition; every count re-measured in A1.
- [ ] Hypothesis threshold (DoR 3) evaluated in A1 and the result recorded; if disproved, stop and return to S01.
- [ ] Behaviour-free: routes, DTOs, schema, usage-data events unchanged; frontend and E2E untouched; RX1's degradation recorded.
- [ ] Moves in `refactor(contracts): move …` commits apart from edits.
- [ ] `TeamDataRefreshedTeamMetricsUpdateHandler` first among `TeamDataRefreshed` handlers (order pin green after every B and D slice).
- [ ] `SHARED_REGEX` covers `Modules/TrackerIntegration/Contracts` in the commit that moves the connector port.
- [ ] Strings, suppressions, log overrides, CI targets, budget and host-user lists re-grepped in A1.
- [ ] `StartUpRegistrationsPreservationTest` deleted in B3 after its last green run is recorded.
- [ ] Architecture docs updated in D3; finalize checklist run before the last push.

## Wave: DEVOPS
- **Platform lens**: none material — no deployment, configuration, persistence or runtime topology change. Two CI
  touches: `SHARED_REGEX` gains `Modules/TrackerIntegration/Contracts` (C4), and each of the four pushes runs the live
  connector suites once because `Program.cs` changes (D-12). Log categories: no moving **implementation** class, so no
  category changes, except the new `ForecastRoundFinishedRefreshLogHandler` (new category) and the dispatcher's error
  log on a failed refresh-log write (RX1); no shipped override or perf helper keys on either.
- **Usage data**: **N/A, because** the Story changes no behaviour a user can use and adds no outcome to count; every
  existing event keeps firing unchanged. (`ForecastRoundFinished` is a domain event, not a usage-data event; the two
  vocabularies stay apart, ADR-270 Decision 3.)
- KPI: the count of cross-context edges outside `Contracts` (R-C1's list length) — measured in A2 and after C5.

## Open questions (default taken)
1. Size: the Story is **L (about 9.5 days, 4 features, 13 slices)**, not M. Split the module start-up (feature B) into
   its own ADO Story under Epic 6510? **Default: no; it stays in #6382 as feature B (the brief assigns it here), and
   the Story's size in ADO is updated to L.**
2. Rigor: A1, A2, B1, B2, C4, D2 `thorough` (opus / sonnet); B3, C1, C2, C3, C5, D1, D3 `standard` (sonnet / haiku)?
   **Default: yes.**
3. AC "every context has `Contracts`": Promises, Refinement and Reports are reached by no other context; give them no
   `Contracts` folder, recorded by R-C3 as "reached by none", rather than a placeholder type? **Default: no folder.**
4. Move the 15 domain event types into their publishers' `Contracts` in this Story (D-5), rather than leaving them to
   S04b? **Default: move them here.**
5. `ConnectionFetchShapeChanged`: ADR-270 Decision 3 assigns it to Tracker integration, but its publisher is
   Organisation set-up's connection save. Follow Decision 3's own rule ("moves to the context that publishes it") and
   amend the line? **Default: Organisation set-up; ADR line amended in D3.**
6. `DeliveryRuleService` builds `new RuleEvaluator<Feature>()` and `new FeatureFieldProvider()` in a default
   constructor. Remove that constructor (inject through DI) in C3 so two R-C1 entries disappear, or list them? **Default:
   list them (owner Work Items / S07a); removing a constructor is an edit outside a namespace move.**
7. "Improve the tests you touch": a test file whose only change is a `using` line is not counted as touched for that
   rule (hundreds of files)? **Default: yes; the moved-type test classes and the new oracles follow the test-writing
   skill in full.**
8. RX1 changes one failure path: a failed refresh-log write for a forecast round is logged by the dispatcher instead
   of failing the round. Accept as behaviour-free (no user-visible difference, the next round logs again)? **Default:
   yes; if not, RX1 is listed with owner instead of converted, and D2 drops.**
9. Keep `Add<X>` on the module root (`Modules/<X>/<X>Module.cs`, namespace `Modules.<X>`) rather than in an
   `Adapters` sub-folder? **Default: module root.**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built for users; every context gets a published front door, and the
composition root delegates registration to each context.
```
 Composition root (Program)            Context X  (Modules/X)                         Context Y (Modules/Y)
  AddFlowMetrics() ... AddReports()  ┌──────────────────────────────────────┐      ┌───────────────────────┐
  RegisterDomainEventHandlers()  ──► │ XModule.AddX()      (NEW, registration)│      │ Application (uses Y's │
  (one ordered list, all contexts)   │ Contracts  (NEW): ports X answers,    │◄─────┤   ports through       │
                                     │   ports X asks others to fill, events │      │   X.Contracts only)   │
                                     │ Application (MOVED OUT: published types)│     └───────────────────────┘
                                     │ Domain      (unchanged, private)       │
                                     │ Adapters    (unchanged, private)       │
                                     └──────────────────────────────────────┘
   Driven: repositories, trackers (via Tracker integration's Contracts), dispatcher — unchanged
   One new event path: ForecastUpdater ─ForecastRoundFinished─► Work Items handler ─► refresh log
```
- Driving adapters: controllers and hosted services — unchanged. Application services and ports: the ones another
  context uses move to `Contracts` (MODIFIED namespace); the rest stay private. Domain: unchanged; domain events move
  from the shared `Models` into their publisher's `Contracts`. Driven adapters: unchanged; no new external call.

**Patterns weighed.**
- Published contract per context (chosen) vs a facade layer (rejected: pass-through, duplicate ports) vs a type list
  (rejected: designation, not namespace) vs one shared contracts root (rejected: `Services/Interfaces` again).
- Dependency inversion across contexts (kept): Flow metrics and Tracker integration publish ports that Forecasting and
  Promises implement; the ports live in the owner's `Contracts`.
- Domain events instead of a cross-context command (chosen for the one command inside the modules) vs keeping the call
  (rejected: ADR-270 Decision 3); outbox (rejected: ADR-027 D7, a lost log entry is repaired by the next round).
- Rules first, ratchet list shrinking per slice (chosen) vs moving first (rejected: rules keyed on `Application` would
  pass over an empty set).
- Two orthogonal rules — surface (`Contracts` only) and direction (context map) — each with one owner (chosen) vs one
  matrix rule (rejected: re-states the per-module legal callers a second time).
- Module start-up extension per context with the handler order in one root list (chosen) vs handlers inside each
  extension (rejected: the `TeamDataRefreshed` order interleaves contexts) vs a dispatcher ordering attribute
  (rejected: changes the dispatcher).
- A temporary whole-container pin during the start-up move (chosen) vs a permanent one (rejected: freezes every later
  DI change) vs review only (rejected: S03g's EXAMINE found no probe sees `Program.cs` order).
- Single responsibility / interface segregation: contracts hold only what is consumed; algorithms stay private and the
  few cross-context algorithm calls are listed with owners.
