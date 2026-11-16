# Story 6360 — S03b Package Work Items: the third context in its own module

**Finished:** 2026-11-16 · ADO User Story #6360 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Work Items was spread over `Services/{Interfaces,Implementation}/{WorkItems, TeamData, WorkItemRules, Dependencies,
Parents}`, five event handlers in `Services/Implementation/DomainEvents`, one in the update folder and a few loose
files. No namespace rule named it whole: the one pattern that did (`WorkItemsSyncPattern`) covered only three of those
folders. It also commanded Flow metrics directly: after a Team refresh, `TeamDataService` called
`ITeamMetricsService.UpdateTeamMetrics` (ADR-270 ratchet candidate 1), while the Portfolio side already reacted to its
refresh event. With that call in place Work Items cannot have its own rules, and Epic 6474 slices 04, 10 and 12 have
no clean module to write into.

## What changed

Three nwdes features on `main`. Everything is behaviour-free apart from one accepted change of the failure path in
feature C.

- **Feature A, rules first** (`docs/feature/story-6360-s03b-a-rules-first/`, 3 slices). No production code changed.
  - Every architecture rule keyed on Work Items' namespaces, folders, paths or types also sees `Modules/WorkItems`,
    each proven with a made-up type (`WorkItemsRulesSeeTheModuleArchUnitTest`). The dependency rules name their own
    sub-folders in the module, and the oracle refuses a key that names a whole layer.
  - The membership list was measured from the compiled IL and fixed in `WorkItemsMembershipArchUnitTest`. The refresh
    log service and its port move; the orphan clean-up pair stays (it reaches the database context and EF Core
    directly), and an interface whose only implementation stays, stays with it.
  - Safety net: `RefreshesStayTheSameTest` refreshes a CSV-backed Team and Portfolio at the service port and compares
    items, transitions, blocked periods, parent placements and events with values written in the test. Public
    commands `refresh-answers-compare` and `refresh-reactions-show` (later `refresh-reactions-compare`, which also
    counts parent placements) capture the refresh answers and the reactions on demo data.
- **Feature B, the move** (`docs/feature/story-6360-s03b-b-move/`, 3 slices). 35 production files moved by `git mv`,
  namespace lines only, then separate edit commits for `using` lines, rule-list strings, one doc `cref`, the
  `Program.cs` registrations and the CI test selection that follows `FetchFingerprint`.
  - `Domain` (2): `SyncModeResolver`, `FetchFingerprint`; `Domain/Dependencies` (4): the dependency honour policy,
    the cycle detector and the dependency facts.
  - `Application` (14): `WorkItemService` (whole), `TeamDataService`, `BlockedItemService`, the refresh log service,
    the rule evaluator with its field providers, parent placement, and their ports. `Application/Dependencies` (9):
    the dependency decision, reconciler, refresh reporter, rendering and `WhatTheForecastWaitsFor`.
  - `Application/Policies` (6): the blocked-transition capture and close handlers for Work Items and Features, the
    refresh-log clean-up on Team deletion, and the update trigger on a changed fetch shape. Handler order unchanged.
  - The rule evaluator went to `Application`, not `Domain`: its public `Match` signature takes `IRuleFieldProvider<T>`,
    an Application port, and the Domain rule rejects it. No rule was loosened.
  - Stays: the three tracker selectors (`ParentSourceSelector`, `DependencySourceSelector`, `DependencyFieldReferences`;
    connectors call them, and connectors may not reach Work Items), `TeamUpdater` and `PortfolioUpdater` (Epic 6474
    slice 12 replaces them), and every entity, rule type, event and DTO in `Models` and `API/DTO`.
- **Feature C, edge and rules** (`docs/feature/story-6360-s03b-c-edge-and-rules/`, 3 slices).
  - C1: Work Items no longer commands Flow metrics. `TeamDataService` saves the items and publishes `TeamDataRefreshed`;
    the new Flow metrics reaction `TeamDataRefreshedTeamMetricsUpdateHandler`
    (`Modules/FlowMetrics/Application/Policies`) loads the Team and updates its metrics, registered first among the
    `TeamDataRefreshed` handlers so every later reaction reads fresh metrics. `DomainEventHandlerOrderPinTest` pins
    that order; `TeamMetricsFollowTheTeamRefreshTest` pins the success path and the accepted failure path at the port.
  - C2: Work Items has its own module rules (`WorkItemsModuleBoundariesArchUnitTest`, on the shared `ModuleRules`):
    each type in its layer and none left in `Services`; Domain reaches only the kernel and the BCL; inline inbound and
    outbound lists compared both ways; Policies hold only domain-event handlers; no connector reaches a Work Items type.
    Flow metrics' R4 list 5 → 4, without `TeamDataService`.
  - C3: 29 Work Items unit-test files moved to `Lighthouse.Backend.Tests/Modules/WorkItems` (`Domain` 8, `Application`
    17, `Application/Policies` 4), and `WorkItemsUnitTestPlacementTest` keeps them there. Three of the moved classes
    now drive a `ServicePortHarness` instead of the host, so they left `HostUsersStillToMove`.
  - The public command `workitems-edge-show` runs and names, in one call, the Team-refresh metrics scenarios, the
    order pin, the module rules, the Flow metrics inbound list and the placement test.
- **Docs, at finalize:**
  - ADR-270 (`docs/product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md`) and
    `ARCHITECTURE.md` §4 and §5.
  - A new section "Application Architecture — story-6360 s03b package work items" in
    `docs/product/architecture/brief.md`: layout, the edge to Flow metrics with both paths, rule shapes, proof.
  - The Epic catalogue (`docs/feature/epic-6264-modular-monolith/feature-delta.md`): the S03b entry in the Improvement
    log, and the S03* notes (S03b done; listing types of a context with no namespace yet; selectors stay outside).
  - The slice-04 brief (`docs/feature/epic-6264-modular-monolith/slices/slice-04-s03b-package-workitems-sync.md`):
    the criterion on `Models` types rewritten (D-7: no `Models` type moves; the list goes to S04b), and a close-out
    note on what stayed in `Services`.
  - The 6474 delta (`docs/feature/epic-6474-one-structure/feature-delta.md`): "Amendment after S03b", `UnitUpdater`
    in `Modules/WorkItems` (D-4) with the seams slices 03, 04, 08, 10, 12 and 15 write into, and the `UnitUpdater` row
    of its component table marked.

## Key commits

| Commit | What |
|---|---|
| `93f10d9d1`, `c6dcc2f9b`, `f96d97692` | A: oracles, then every Work Items-keyed rule sees `Modules/WorkItems` |
| `2be1d6205` | A: the membership list, measured from the compiled code |
| `4c472bc5a`, `ec625a4a4` | A: `refresh-answers-compare` and `refresh-reactions-show` |
| `fe3652721`, `eea18a37c`, `298773ccc` | B: Domain move, then callers and the CI test selection |
| `44a61c553`, `92029ee30` | B: Application move, then callers, registrations and rule lists |
| `f7b64f405`, `85dc9a27f` | B: Policies move, then callers |
| `e2dbc016a` | B: `refresh-reactions-compare`; parent placements counted in the refresh compare |
| `4dad90e40`, `294045516` | C1: oracle, then the Team metrics update as a Flow metrics reaction |
| `73eb27a8e` | C2: Work Items module rules (no production change, `9390cb583`) |
| `ecf1e1f26`, `8b6f9eb01`, `7f3dfe1ad` | C3: placement oracle, the 29 test moves, three classes to the service port |
| `6ae3aca53` | C: unit tests for the new reaction; mutation record |
| `db1cb9512` | C: `workitems-edge-show` |

## What it achieved

- **Behaviour unchanged on the success path.** Refresh answers: all 297 identical over 9 scenarios. Metrics answers:
  366 responses and 3 snapshot tables identical. Forecast answers: all 128 identical. Work Items reactions: identical
  before and after (`refresh-reactions-compare`). No behavioural assertion changed.
- **Failure path changed, accepted by the maintainer (2026-11-16).** Before, a failing save of a Team's metrics failed
  the refresh and no `TeamDataRefreshed` reaction ran. Now the dispatcher catches and logs the handler's error: the
  refresh is reported and logged as succeeded, the Team's `UpdateTime` and `FeatureWIP` keep their old values (so the
  Team looks due for a refresh again), and the other reactions run in order. The metrics cache was dropped before the
  save, so readers never see stale metrics.
- **Ratchet candidate 1 closed.** Work Items no longer reaches `ITeamMetricsService`. Flow metrics' R4 list 5 → 4.
  The two query edges (`WorkItemService` → `IPortfolioMetricsService`, `PercentileCalculator`) stay, named in Work
  Items' own outbound list and in Flow metrics' R3 and R4 lists.
- **Contexts packaged with namespace rules:** 2 → 3 of 11. Files under `Services`: 456 → 421; `Modules/WorkItems`:
  35 files.

## Quality

| Gate | Result |
|---|---|
| Feature A | Every oracle approved with its RED observed (S1 tightened once); slice reviews S1 and S3 accepted (S2 changed no target). Candidate review accepted, no findings (786 selected tests, refresh answers 297 identical). EXAMINE accepted, E1–E4 met |
| Feature B | Oracle reviews rejected twice on the way (S1 placed the rule evaluator in Domain; S2 missed untracked Stryker configs and a relative `cref`), then approved. Slice reviews accepted. Candidate review accepted with two low design findings, taken as deviations (below). Architecture 826/826, refresh 297, metrics 366 + 3 tables and forecasts 128 identical. EXAMINE E1–E4 met, E3 judged on the reactions a user can observe |
| Feature C | Every oracle approved with its RED observed (C2, a rule-only slice, by breaking the oracle on purpose: 6 and 5 failures, then restored byte for byte). Slice reviews S1 (one low finding, fixed in `1b88b2e3e`) and S3 accepted; S2 changed no production code. Candidate review accepted with one non-blocking finding. EXAMINE **not accepted: indeterminate twice, on observation gaps only**. The first run (all four unmet, observation gap) had no probe that printed the order, the failing save or the rule rows within its 5-minute budget; `workitems-edge-show` and a day-cached base for `metrics-answers-compare` were added (`db1cb9512`). The second run met E3 and E4 (metrics 366 + 3 tables, forecasts 128, reactions 3 identical; placement 95/95) and left E1 and E2 unmet on an observation gap: no observation contradicted a promise. The host re-ran the same probe through `nwdes examine probe`: its 109 lines open with E1 (the failing-save, deleted-Team and success scenarios passed, the order pin passed, and the handler order printed with `TeamDataRefreshedTeamMetricsUpdateHandler` first) and E2 (the module rules, both-ways lists and R4 rows passed). The maintainer chose to record this and finalize, with no third run (2026-11-16) |
| Full backend suite | Green before each push: A 13,524, B 13,578, C 13,661 passed, 0 failed. CI: C1 green; C2 backend green, its test-time budget step red on a live ServiceNow test (connectors untouched; `Program.cs` forces the full suite); C3 CI not seen at review |
| Mutation | Feature C: 4 of 4 mutants killed, 100 % (first run 25 %; two unit tests added for the new reaction). See `docs/feature/story-6360-s03b-c-edge-and-rules/mutation/results.md` |

No frontend file changed, so no frontend gate ran.

## Deviations from the design, accepted

- **D-8 (registration order), feature B.** The `Program.cs` registration lines kept their place, order and lifetimes,
  but the moved types' qualifiers were shortened to the short type name, since the `using` lines carry the namespace.
  The order pin and all three compares stayed identical: text only.
- **D-9 (unit-test placement), feature B.** `DeliveryRulesControllerTest` and `FeaturesControllerTest` named
  `IBlockedItemService` by its full name inside `Mock.Of<…>`. A `using` line cannot fix a full name, so both were
  retargeted to the new one. No expectation changed.
- **R-W6 (Work Items Application outbound), feature C.** The design expected 4 edges into Access, Licensing,
  Organisation set-up and the update queue; the list holds 6. Organisation set-up has no namespace of its own yet:
  `IFeatureOrdering` sits in the top-level `Services` namespaces with `IAppSettingService` and `ILighthouseClock`, so
  `RefreshLogService` → `IAppSettingService` and `WorkItemService` → `ILighthouseClock` are listed too. The rule is
  stricter than designed, never looser.
- **R-W5 (Work Items Application inbound).** Promises has no namespace yet either, so `DeliveryRuleService` is a
  listed caller rather than a legal one by namespace. The list holds 7.
- **D-2 a with its failure path**, as above: decided by the maintainer on 2026-11-16 after feature C's design and its
  Quint model (`docs/feature/story-6360-s03b-c-edge-and-rules/models/team_refresh_reactions.qnt`) showed the
  difference. The fallback to ratchet only (D-2 e) was withdrawn.

## Decisions taken without the maintainer, open to revisit

- **Admission rule for the membership list (A2).** A port whose only implementation stays, stays with it; otherwise a
  `Services` class would implement a Work Items port. That kept the orphan clean-up interface out of the module.
- **EXAMINE E3 of feature B reworded** to the reactions a user can observe. Four of the six reactions open and close
  blocked periods, which no public answer shows on the day they open; the order pin and the candidate review cover them.
- **Three moved integration test classes moved down to the service port** in C3 (improve the tests you touch); the two
  seam tests that resolve real tracker and metrics services stay on the host.
- **The new reaction got unit tests** during the mutation pass, beside its sibling
  `PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest`, and is listed in `FlowMetricsUnitTestPlacementTest`.

## Knowingly left open

- **`WorkItemService`'s two Flow metrics queries** for the default-size guess, listed until Epic 6474 slice 10 builds
  `DefaultSizeGuess` or S07a splits `WorkItemService`.
- **`UpdateTeamMetrics` writes through Work Items' `IWorkItemRepository.Save`**: a Flow metrics write through Work
  Items' repository, left to S07b (maintainer, hold 1).
- **The refresh pipelines and the Portfolio refresh's calls into Promises and Forecasting** (ratchet candidate 4) stay
  in the update code; Epic 6474 slice 12 builds `UnitUpdater` in `Modules/WorkItems` (maintainer, hold 1; recorded in
  the 6474 delta at this finalize).
- **Work Items' entities, rule types, events and DTOs** stay in `Models` and `API/DTO` (S04b, S12c). The tracker
  selectors go with Tracker integration (S03a). No `AddWorkItems` registration extension (S09a).
- **Candidate-review note, feature C:** the three harness-backed test classes repeat the same 15-line set-up and
  tear-down, and the scope from `harness.NewScope()` is not disposed. Extract one helper when they are next touched.
- **`IlDependencies` still misses catch-clause types, events and named attribute arguments**; no current code hits it.
- **Maintainer's production-like comparison:** one refresh-answers comparison on the production-like database, by hand.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because no page under `docs/` describes what changed. `teams/detail.md` ("Update Team Data") says an update refreshes the Team's data including its Metrics, which still holds on the success path. No page describes what a refresh does when saving a Team's metrics fails, and the Team header's last-updated time keeps its old value in that case, which matches the accepted behaviour. `settings/refreshhistory.md` does not describe failed runs. The architecture docs are under "Context map" below. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed and nothing user-visible is new. |
| Clients (CLI, MCP) | N/A, because no route, DTO or enum changed (297 refresh, 366 metrics and 128 forecast answers compared identical); no client release is due. |
| Context map, `ARCHITECTURE.md` | Done in this finalize: ADR-270 (Work Items packaged, the metrics update as a Flow metrics reaction, measured edges, layout, ratchet candidates 1, 4 and 7, gate change 5) and `ARCHITECTURE.md` §4 and §5. With them: the new section in `docs/product/architecture/brief.md`, the Improvement log and S03* notes in `docs/feature/epic-6264-modular-monolith/feature-delta.md`, the slice-04 brief's `Models` criterion (`docs/feature/epic-6264-modular-monolith/slices/slice-04-s03b-package-workitems-sync.md`), and the `UnitUpdater` amendment in `docs/feature/epic-6474-one-structure/feature-delta.md`. |
| Usage-data event | N/A, because nothing became usable or is used differently (DEVOPS answered the same). |
| Mutation | Done for feature C: 4 of 4, 100 %, no survivor (`6ae3aca53`, `docs/feature/story-6360-s03b-c-edge-and-rules/mutation/results.md`). N/A for feature A, because it changed no production code (tests and `Scripts/examine` only). N/A for feature B, because its production edits are namespace and `using` lines, one doc `cref` and shortened `Program.cs` registration qualifiers: no statement changed, so no mutant can be added in the moved code (bar: "no survivor or no-coverage mutant added in the moved code"). |
| Release notes | N/A; written at /release. |
