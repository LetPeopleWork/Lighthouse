# Feature: story-6359-s03a-package-tracker-integration

Story #6359 · S03a Package Tracker integration (ACL) · Epic #6510 (Modules: package the bounded contexts)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
- Brief: [`slices/slice-03-s03a-package-worktracking-integration.md`](../epic-6264-modular-monolith/slices/slice-03-s03a-package-worktracking-integration.md)
  (all three "Update" sections read: retitled after S01; selectors handed over by S03b; S06 ran first)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md),
  [ADR-285](../../product/architecture/adr-285-connector-capabilities-are-interfaces-and-per-connection-variance-is-a-probe-on-the-capability.md)
- Precedents, same shape: [S03b Work Items](../story-6360-s03b-package-work-items/feature-delta.md),
  [S03c Forecasting](../story-6361-s03c-package-forecasting/feature-delta.md),
  [S06 connector capabilities](../story-6376-s06-connector-capability-interfaces/feature-delta.md) (its D-8).

DISCUSS: covered at Epic level (D-6264-3, D-6264-4, D-6264-16); waves start at DESIGN.

DESIGN run 2026-11-16, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is recorded
as such. No choice changes what a user sees, so no decision is held for the maintainer; the open questions at the end
are confirmations, not blockers.

## Wave: DESIGN / [REF] Inherited

Linked, not copied.

- **ADR-270**: Decision 1, Tracker integration row (connections and their credentials, OAuth, connection health,
  translation of each record during a fetch, applying the mapping it is given; "nobody outside it sees a tracker's
  model"); Decision 2, the acknowledged Organisation set-up ↔ Tracker integration cycle (set-up is a customer of the
  ACL's query port; the mapping is handed in as data); "Every edge" rows for Tracker integration; ratchet candidate 3
  (Deliveries take `IWorkTrackingConnectorFactory`); Decision 5 glossary (Connection, Translation, Parent source,
  Connection stopped working, Read refusal, Capability); Decision 6 layout (`Modules/TrackerIntegration`,
  `Domain/Application/Adapters`, `Adapters/Connectors` for a context that talks to an outside system,
  `Adapters/Background`, `Adapters/Persistence`, ports next to their use case, tests mirror the layout, rules from the
  IL); Decision 7 (shared kernel); the platform lane row (the encryption key ring and demo data are platform, not a
  context); "Left open": where write-back lives (decided here, D-6).
- **ADR-285**: capabilities are interfaces; per-connection variance is a probe on the capability. The port S03a
  moves is S06's final port: `IWorkTrackingConnector`, the capability interfaces, the markers,
  `IWorkTrackingConnectorFactory`, `RemoteRecordStamp`, the read-refusal exceptions (S06 D-8).
- **S03b/S03c packaging rules** (catalogue "Notes from S02" and the two Stories): rules first, widened before any type
  moves and proven with a made-up type under `Modules`; edges from the compiled IL (`IlDependencies`), inline lists
  compared both ways, never loosened (ADR-268); one oracle file per slice; moves separate from edits; `Program.cs`
  edits force the full CI suite; logger categories change with the namespace; search full type names in strings, not
  only `using` lines; `IlDependencies` misses catch-clause types, events and named attribute arguments; reuse
  `ModuleRules` + `ModuleWording`; EXAMINE commands declared up front; mutation bar "no survivor or no-coverage
  mutant added in the moved code".
- **Work Items leaf rule (S03b, ADR-270 "Edges measured by packaging Work Items")**: no connector reaches a
  `Modules.WorkItems` type. That is why `ParentSourceSelector`, `DependencySourceSelector` and
  `DependencyFieldReferences` stayed outside `Modules/WorkItems`; they belong to this Story (brief update after S03b).
- **S06 D-8 leaks** handed to S03a/S09a: (1) `IBoardInformationProvider` takes `Board`/`BoardInformation` from the
  implementation namespace; (2) `IWorkTrackingConnectorFactory` sits in `Lighthouse.Backend.Services.Factories`,
  its class in `Lighthouse.Backend.Factories`.

## Wave: DESIGN / [REF] DoR re-run (2026-11-16)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Tracker integration is spread over `Services/{Interfaces,Implementation}/{WorkTrackingConnectors, OAuth, ConnectionHealth, Parents, Dependencies}`, `Factories`, `BackgroundServices` and `Models`; the leaf rule's pattern names only `WorkTrackingConnectors` and `Factories` and misses the factory interface's namespace (E1, E4). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files and edges below. |
| 4 UAT | PASS (catalogue) | Brief AC, CI-observable; the acceptance designer writes the oracles in DELIVER. |
| 5 AC from UAT | PASS, one AC corrected | "Context-owned `Models` types have moved" holds only for non-entity, non-wire, non-kernel-reached types (D-2, as S03b D-7). |
| 6 Right-sized | PASS by waiver | Brief says L; DESIGN confirms L (about 7.5 days, 9 slices, 3 nwdes features). |
| 7 Technical notes | PASS | Live connector suites and the Linear rate limit answered in D-11 and the CI section. |
| 8 Dependencies | PASS | Hard: S01 Closed, S02 gate go-with-changes. Soft: S06 #6376 done (finalized at `175daeeec`); S04a, S05e #6373 not taken (D-4); S12c not done, so wire DTOs stay. |
| 9 Outcome KPIs | PASS | KPIs 1 (files under `Services`), 2 (contexts packaged), 3 (`Models` files). |

## Wave: DESIGN / [REF] Baseline re-count (2026-11-16, `main` at `175daeeec`)

Counted with `git ls-files` (tracked files). Namespaces from each file's `namespace` line. Edges from `using`
directives with a throw-away scanner in the session scratchpad (`scan.py`): **text-search fallback, `noisy`** — it
misses same-namespace and fully qualified references and catch-clause types; the IL measurement is slice A3's (E-list
marks which facts wait for it). `code-fact` was not used (C# callers; the stable CLI answers at `noisy` text level
here too, so the scanner was the same tier).

| Measure | Today |
|---|---|
| Files under `Services` (KPI 1) | **424** |
| Loose files at the top of `Services/Interfaces` + `Services/Implementation` | 76 in total |
| `Models` files (KPI 3) | unchanged by S06; exact count measured in A3 |
| Contexts packaged (KPI 2) | **3 of 11** (`FlowMetrics`, `Forecasting`, `WorkItems`) |

### The Tracker integration set by ADR-270 (production)

| Group | Folder / namespace | Files |
|---|---|---|
| Connectors and translation | `Services/Implementation/WorkTrackingConnectors` root (`AuthenticationMethodKeys`, `AuthenticationMethodSchema`, `WorkItemCategoryCrossing`, `WorkItemStateTransitionMapper`) | 4 |
| | `…/Auth` (5 auth strategies, `NoOpAuthStrategy`, `WorkTrackingAuthStrategyFactory`, its not-found exception) | 8 |
| | `…/AzureDevOps` | 6 |
| | `…/Boards` (`Board`, `BoardInformation`: S06 leak 1) | 2 |
| | `…/Csv` | 2 |
| | `…/Jira` | 15 |
| | `…/Linear` | 3 |
| | `…/OAuth` (`OAuthSchemaExtensions`, `OAuthWorkTrackingOptionNames`) | 2 |
| | `…/ServiceNow` (connector, mappers, verdicts, read scope) | 14 |
| Connector port (S06's final port) | `Services/Interfaces/WorkTrackingConnectors` (base port, 4 capability interfaces, 4 per-tracker interfaces, `IBoardInformationProvider`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher`, `IWriteBackTarget`, auth strategy ports, `WorkTrackingReadException`, `WorkTrackingRefusedException`) | 15 |
| Factories | `Factories`: `IWorkTrackingConnectorFactory` (namespace `Services.Factories`, S06 leak 2), `WorkTrackingConnectorFactory`, `IWorkTrackingSystemFactory`, `WorkTrackingSystemFactory`, `IIssueFactory`, `IssueFactory` | 6 |
| OAuth | `Services/Implementation/OAuth` (service, registry, state-token issuer, refresh options, 7 exceptions) 11, `…/OAuth/Providers` (Ado, Jira, Stub) 3, `Services/Interfaces/OAuth` 4 | 18 |
| Connection health | `Services/Implementation/ConnectionHealth` (`ConnectionHealthService`, `ConnectionHealthCadence`) 2, `Services/Interfaces/ConnectionHealth` 1, `Services/Implementation/BackgroundServices/ConnectionHealthProber` 1 | 4 |
| Source selectors (from S03b) | `Services/Implementation/Parents/ParentSourceSelector`, `Services/Implementation/Dependencies/{DependencySourceSelector, DependencyFieldReferences}` | 3 |
| **Total set, production** | | **102** (96 of them under `Services`) |

Touching the set but **not** moving (D-1 says why): `Factories/DemoDataFactory`, `IDemoDataFactory`, `DemoData/*.csv`
(platform lane: demo data); `Services/Implementation/Connections/{ConnectionSaveService, ChangedConnectionDefault}`,
`Services/Interfaces/IConnectionSaveService`, `WorkTrackingSystemConnectionUsage` + interface (Organisation set-up
candidates); `WriteBackService`, `WriteBackCollector`, `WriteBackRound`, `WriteBackRoundContext`,
`WriteBackTriggerService` + 3 interfaces (D-6); `Services/*/Encryption` 33 files incl. `ConnectionSecrets` (platform
lane: key ring); repositories `WorkTrackingSystemConnectionRepository`, `OAuthCredentialRepository`,
`ConnectionHealthVerdictRepository` (D-4); `Health/*` (7, host readiness, not connection health); controllers and
DTOs.

`Models` types of the context: `WorkTrackingSystemConnection`, `WorkTrackingSystemConnectionOption`,
`WorkTrackingSystems`, `WorkTrackingSystemOptionsOwner`, `IWorkTrackingSystemOptionsOwner`, `ConnectionFieldDefaults`,
`SupportedConnectionDefaults`, `AdditionalFieldDefinition`, `RemoteRecordStamp`, `Models/OAuth/*` (7, incl. the
entity `OAuthCredential`), `Models/ConnectionHealth/*` (3, incl. the entity `ConnectionHealthVerdict` and the wire
`ConnectionHealthDto`), `Models/Validation/ConnectionValidationResult`, `Models/WriteBack/*` (9),
`Models/Events/ConnectionFetchShapeChanged`. `LighthouseAppContext` uses `Models.OAuth` and `Models.ConnectionHealth`
(DbSets).

### Tests of the set (path count)

`Lighthouse.Backend.Tests`: `Services/Implementation/WorkTrackingConnectors/**` 93 (Jira 32, Csv 18, ServiceNow 17,
AzureDevOps 12, Linear 6, Auth 2, root 6), `…/OAuth/**` 10, `…/Parents` 3, `…/Dependencies` 2 (the selector tests),
`Factories` 6 (of 8; the two demo-data tests stay) = **114**. Connection-health tests elsewhere: unknown — measured in
A3. 192 test files `using` a namespace of the set.

### Inbound (who calls the set; text search, `noisy`)

| Caller | Reaches | ADR-270 reading |
|---|---|---|
| `Modules/WorkItems/Application/WorkItemService` | `IWorkTrackingConnectorFactory`, the connector port | legal (Work Items is the ACL's customer) |
| `Services/Implementation/DeliverySources/DeliverySourceResolver`, `DeliveryForecastPublishingService` | factory + `IDeliverySourceProvider` / `IDeliveryForecastPublisher` | ratchet candidate 3 (D-7) |
| `WriteBackService` | factory + `IWriteBackTarget` | write-back round (D-6) |
| `BackgroundServices/Update/{TeamUpdater, PortfolioUpdater, UpdateServiceBase}` | connector port, `IConnectionHealthService` | refresh pipelines (6474 slice 12) |
| `ConnectionHealthProber` (`BackgroundServices`) | `IConnectionHealthService` | moves into the module (D-1) |
| `Modules/FlowMetrics/Application/Policies/{DemoBlockedHistoryBackfillHandler, DemoPercentilesBackfillHandler}` | `…WorkTrackingConnectors.Csv` (CSV option names) | **Flow metrics → a connector's internals**: listed edge (E5) |
| `API`: `DeliverySourcesController`, `PortfoliosController`, `TeamsController`, `WizardsController` (Boards), `WorkTrackingSystemConnection(s)Controller` (factory, `…Jira`), `OAuthController`, `ConnectionHealthController`, DTOs `AuthenticationMethodDto`, `WorkTrackingSystemConnection(Summary)Dto` | port, factory, OAuth, health, Jira types | `API` legal by namespace for Application; `API` → `Adapters` (Jira types) listed |
| `Services/Implementation/Encryption/{PublishedKeySecretCount, ReadableSecretsNotOnTheActiveKey, ReferencedKeyIds, SecretCustodyService}` | `Models.OAuth` (kernel, stays) | no module edge |
| `Services/Implementation/DemoDataService` | `Factories` (demo data, stays) | none after the move |
| `Data/LighthouseAppContext`, repositories | `Models.OAuth`, `Models.ConnectionHealth` | kernel, stays |
| `Program` | everything | composition root |

### Outbound (what the set reaches beyond `Models`, `Extensions`, `Cache`)

| From | To | Reading |
|---|---|---|
| ADO, CSV, Jira, Linear connectors | `Services.Implementation.Dependencies` / `Parents` (the selectors) | moves with the set: becomes internal |
| `JiraWorkTrackingConnector` | `Services.Interfaces.DeliverySources.IDeliveryForecastBlockRenderer` (merges the forecast block into a Jira description, line 3971) | **Tracker integration → Promises**: an upstream ACL reaching a downstream context. The current leaf rule misses it (its Portfolio/Delivery pattern names only `BackgroundServices` and `Interfaces.Update`). D-7 |
| Jira, Linear, PAT, Jira Cloud basic, ServiceNow basic, Linear key auth | `Services.Interfaces` (`ICryptoService`, platform) | platform lane, listed |
| `ConnectionHealthService` | `Services.Implementation.Encryption` (`UnreadableSecretException`), `Services.Implementation.Repositories.ConnectionHealthVerdictRepository` (a **concrete** repository), `IRepository<>` | listed (D-4) |
| `OAuthService` | `Services.Implementation.Encryption`, `IRepository<OAuthCredential>` | listed |
| `WorkTrackingConnectorFactory` | `Cache` | platform, legal |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. Target module path and sub-folders. → D-3.
2. Membership: what moves, what stays and to which Story (connection persistence, OAuth, connection health, the
   connection entity, write-back, encryption, demo data, the selectors). → D-1, D-4, D-6.
3. Context-owned `Models` types. → D-2.
4. S05e (connection ports) taken or not. → D-4.
5. The Deliveries → connector edge (ratchet candidate 3) and the Jira connector → Promises edge. → D-7.
6. The two S06 leaks. → D-5.
7. The rule set and how it is written. → D-8, Ratchet design.
8. Tests moving with production; DI registration; `ARCHITECTURE.md` §4; EF impact. → D-9, D-10, D-0, SSOT edits.
9. CI path filters, scripts and Stryker configs that name connector folders. → D-11, Impact closure.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels unchanged; existing tests green
      unmodified apart from moved tests, `using`s and rule files.
- [ ] **Rules first**: every rule, script and config keyed on the set's namespaces or paths widened before a type
      moves, proven with a made-up type or path under `Modules/TrackerIntegration`.
- [ ] **The live connector suites still select by path** after the move (D-11): proven before the move, not after.
- [ ] **Moves separate from edits**: `refactor(connectors): move …` commits apart from edit commits.
- [ ] **Ratchet in the same change**; inline lists; no rule loosened.
- [ ] **No file of the context left in `Services/Interfaces|Implementation`** (the brief's first AC), with the
      named exceptions of D-1 and their owners.
- [ ] **Configuration and strings checked for namespace and full type names** (log categories, `test-time-budget.json`,
      Stryker globs, `connector-throws-count`).
- [ ] **`Program.cs` edits**: the push runs the full CI backend suite, live tracker tests included.
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4 row 1, anchors, diagram, leaf-rule wording; ADR-270.
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered.

## Wave: DESIGN / [REF] Context-map placement (2026-11-16)

- **Context touched**: Tracker integration, packaged into `Modules/TrackerIntegration`.
- **Neighbours whose edge becomes visible** (no behaviour change): Work Items (customer of the connector port, legal);
  Promises (customer of the port through `DeliverySourceResolver`, `DeliveryForecastPublishingService`; and the reverse
  Jira → renderer edge, D-7); Flow metrics (two demo back-fill handlers read CSV option names, E5); Organisation set-up
  (connection save, connection usage, the API's validation and suggestions); platform lane (crypto, key ring,
  repositories, cache, demo data).
- **New terms / concepts**: none. Folder `Modules/TrackerIntegration` is the ADR-270 glossary name. Code keeps "mapper"
  for translation (glossary drift column, unchanged).
- **Domain events**: none published or reacted to newly. `ConnectionFetchShapeChanged` stays in `Models/Events`
  with its publisher (`ConnectionSaveService`, which stays). "Connection stopped working" stays target.
- **UI surface**: none.
- **Improvement backlog check**: S03a is itself a #6510 item; N/A, stated.

## Wave: DESIGN / [REF] Evidence (2026-11-16, `main` at `175daeeec`)

| # | Finding | Consequence |
|---|---|---|
| E1 | The leaf rule's pattern (`ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern`) covers `Services.(Implementation\|Interfaces).WorkTrackingConnectors` and `Lighthouse.Backend.Factories`, not `Services.Factories` (the factory interface), OAuth or connection health. | The restated rule covers the whole module namespace (D-8). |
| E2 | `RepositoryRecognition.PlatformPersistencePattern` lists `Services.(Implementation\|Interfaces).OAuth` as persistence platform; `StoredRecordsPattern` already admits `Modules.\w+.Adapters.Persistence`. | A1 widens `PlatformPersistencePattern` to the module's OAuth namespaces, or the connector "no repository" rule silently changes meaning after B4. |
| E3 | `Scripts/test-selection/path-classifier.sh` selects the live suites by **path**: `JIRA_REGEX`, `ADO_REGEX`, `LINEAR_REGEX`, `SERVICENOW_REGEX` name `Services/Implementation/WorkTrackingConnectors/<Tracker>/` (production and tests), Jira also `Services/Implementation/Parents/`; `SHARED_REGEX` names `…/WorkTrackingConnectors/(Auth\|OAuth)/`, top-level `…/WorkTrackingConnectors/*.cs`, `Services/Interfaces/WorkTrackingConnectors/I*.cs`, `Models/(WorkTrackingSystemConnection\|OAuth/)`, `Models/WorkTrackingSystems.cs`, `Program.cs`, csproj. `test-path-classifier.sh` runs in `ci_changes.yml` before every job. `dev-test.sh`/`.ps1` use the same classifier. | **After the move, a connector change would stop selecting its live suite: silent-wrong.** A2 widens every regex to the module paths first, with classifier scenarios as oracle (D-11). The move pushes themselves hit `SHARED_REGEX` via the old paths (deletions) and `Program.cs`, so they run all live suites. |
| E4 | `Scripts/examine/build-examine-commands:1222` and the generated `bin/connector-throws-count` hard-code `…/Services/Implementation/WorkTrackingConnectors` and fail with "folder not found" for Csv/Linear/ServiceNow after the move. | A2: the command locates the connector folders wherever they live. |
| E5 | `DemoBlockedHistoryBackfillHandler`, `DemoPercentilesBackfillHandler` (Flow metrics) use `…WorkTrackingConnectors.Csv` (CSV option names). | After B3 they reach `Modules.TrackerIntegration.Adapters.Connectors.Csv`: Flow metrics → a module's Adapters. Listed (2 entries) in C2; removing it (option names into `Application`) is an edit left to S09a's published contract. |
| E6 | `JiraWorkTrackingConnector` takes `IDeliveryForecastBlockRenderer` (Promises) and calls `MergeInto(described, publication.BlockText)`. | Hypothesis **partly disproved**: one ACL → Promises edge the old rule missed. D-7. |
| E7 | `ConnectionHealthService` takes the concrete `ConnectionHealthVerdictRepository` from `Services.Implementation.Repositories`. | Application → a concrete repository; listed (D-4). |
| E8 | `ConnectionSaveService` uses `Modules.WorkItems.Domain` (`FetchFingerprint`) and publishes `ConnectionFetchShapeChanged`. | Moving it into Tracker integration would make the ACL reach Work Items, against the leaf rule. Stays (D-1). |
| E9 | The three selectors `using` only `Models`, `Models.Dependencies` and `Extensions`. `WorkItemCategoryCrossing` and `WorkItemStateTransitionMapper` the same. | Domain candidates (D-3); confirmed by the Domain rule in B1. |
| E10 | Stryker: `Lighthouse.Backend/stryker-config.json` names `**/WorkTrackingConnectors/ServiceNow/…` (2 globs); `Lighthouse.Backend.Tests/stryker-config.{bug-5756-fetch-refusal, bug-5973-jira-board-query, epic-5074-blocked-items-slice05, servicenow-record-classes}.json` name `**/Services/Implementation/WorkTrackingConnectors/…`. `docs/feature/*/mutation/*.json` are history. | The live configs are rewritten in the move slice of their files (impact closure). |
| E11 | `Scripts/test-timings/test-time-budget.json` names 5 test classes by full name under `Tests.Services.Implementation.WorkTrackingConnectors`; `Scripts/test-timings/tests/test_connector_classes.py`, `test_trx_to_csv.py`, `test_check_budget_time_budgets.py` mention the namespace. | Rewritten with the test moves; contents of the three Python tests: unknown — checked in A2. |
| E12 | `appsettings*.json` log levels name only `Services.Implementation.Auth.DisabledAuthenticationHandler` (not in the set). No full type-name string of the set in production `.cs` found by the text search. Tests: `ServiceNowHistoryPurityArchUnitTest`, `ServiceNowValidationVerdictPurityArchUnitTest` hold full names as strings. | Log categories of moved loggers change (no config keys on them); the two purity tests are rewritten in A1. |
| E13 | Rule files keyed on the set (text search over `Tests/Architecture`, hit counts): `DependencySingleDecisionArchUnitTest` 24, `ModuleBoundariesArchUnitTest` 10, `TestHostUseGuardTest.HostUsersStillToMove` 9, `TestHostIsReleasedAfterDisposeTest` 9, `ConnectionHealthSingleWriterArchUnitTest` 9, `WriteBackTargetCapabilityArchUnitTest` 5, `TestHostPerFixtureGuardTest` 4, `JiraRouteLiteralsTest` 4, `FeatureSourceCapabilityArchUnitTest` 4 (full-name constant, path in a message), `WorkItemsMembershipArchUnitTest` 3, `WorkItemsUnitTestPlacementTest`, `TestTierGuardTest`, `ServiceNowValidationVerdictPurityArchUnitTest`, `ConnectorsDeclareNoUnsupportedMembersArchUnitTest`, `ConnectorPortSpeaksPortfolioArchUnitTest`, `CalendarDayAnchorSeamArchUnitTest` 2 each, `WorkItemsModuleBoundariesArchUnitTest`, `WorkItemsApplicationLayoutArchUnitTest`, `ServiceNowHistoryPurityArchUnitTest`, `SecretCustodySeamArchUnitTest`, `RefinementModuleArchUnitTest`, `ConnectorRatchetFixtures`, `CategoryListOwnershipGuardTest` 1 each = **23 files**. | A1 (Ratchet design). Which hits are `using`-only and which key on a path or name: unknown per hit, checked in A1. |
| E14 | EF: `LighthouseAppContext` maps `WorkTrackingSystemConnection`, `OAuthCredential`, `ConnectionHealthVerdict` from `Models`; none of them moves (D-0, D-2). | No migration; `ef-model-unchanged` proves it. |

## Wave: DESIGN / [REF] Decisions

All taken as recommended (AFK, 2026-11-16). Option tables are the record of what was weighed.

### D-0 No schema change — RECOMMENDED

No entity, `DbSet`, migration or table name changes. Entities stay in `Models` (the EF snapshots of both providers carry
their type names; same reasoning as S03b/S03c). Checked by `ef-model-unchanged` in EXAMINE.

### D-1 Membership — RECOMMENDED

"Move" = a pure-move commit to `Lighthouse.Backend.Modules.TrackerIntegration.<Layer>[.<Sub>]`, namespace = folder.

| Candidate | Decision | Why |
|---|---|---|
| Connectors, per-tracker helpers, mappers, verdicts, read scopes (`WorkTrackingConnectors/{AzureDevOps, Csv, Jira, Linear, ServiceNow}`) | **Move**, `Adapters/Connectors/<Tracker>` | ADR-270 Decision 6: `Adapters/Connectors` is where a context talks to an outside system. |
| Auth strategies + their factory (`…/Auth`), `OAuthSchemaExtensions`, `OAuthWorkTrackingOptionNames` (`…/OAuth`) | **Move**, `Adapters/Connectors/Auth`, `Adapters/Connectors/OAuth` | They build HTTP authentication for a connector. |
| The connector port (`Services/Interfaces/WorkTrackingConnectors`, 15) | **Move**, `Application` | The ports Work Items and Promises use; ports next to their use case. |
| `IWorkTrackingConnectorFactory` + `WorkTrackingConnectorFactory`, `IWorkTrackingSystemFactory` + `WorkTrackingSystemFactory` | **Move**, `Application` | The factory is part of the port (S06 D-8); fixes leak 2 by placement (D-5). |
| `IIssueFactory`, `IssueFactory` | **Move**, `Adapters/Connectors/Jira` | Jira issue hydration; only Jira code uses it. |
| `AuthenticationMethodKeys`, `AuthenticationMethodSchema` | **Move**, `Application` | Connection auth schema read by `API` DTOs; `API` → `Application` is legal by namespace, `API` → `Adapters` is not. |
| `Board`, `BoardInformation` | **Move**, `Application` (beside `IBoardInformationProvider`) | Fixes leak 1 by placement (D-5). |
| `WorkItemCategoryCrossing`, `WorkItemStateTransitionMapper`, `ParentSourceSelector`, `DependencySourceSelector`, `DependencyFieldReferences` | **Move**, `Domain` (rejects of the B1 Domain rule go to `Application`, recorded) | Translation rules applied with the mapping handed in; they reach only the kernel (E9). The selectors complete S03b's hand-over. |
| OAuth (`OAuthService`, registry, state-token issuer, refresh options, 7 exceptions, 4 interfaces) | **Move**, `Application/OAuth` | ADR-270 gives OAuth to Tracker integration; use case + its ports. |
| OAuth providers (`AdoOAuthProvider`, `JiraOAuthProvider`, `StubOAuthProvider`) | **Move**, `Adapters/OAuth` | They call the trackers' token endpoints. |
| `ConnectionHealthService`, `ConnectionHealthCadence`, `IConnectionHealthService` | **Move**, `Application/ConnectionHealth` | ADR-270: connection health is Tracker integration's. |
| `ConnectionHealthProber` | **Move**, `Adapters/Background` | Hosted service; ADR-270 sub-folder name. |
| `WorkTrackingSystemConnectionRepository`, `OAuthCredentialRepository`, `ConnectionHealthVerdictRepository` | **Stay** in `Services/Implementation/Repositories` | D-4. Named exception to "no file left", owner S05e/S03f. |
| `ConnectionSaveService`, `ChangedConnectionDefault`, `IConnectionSaveService`, `WorkTrackingSystemConnectionUsage` + interface | **Stay** | E8: the save compares Work Items' fetch fingerprint and publishes the fetch-shape event; usage asks Team/Portfolio repositories. Both are Organisation set-up's side of the acknowledged cycle; owner S03i (creates `OrganisationSetup`). |
| Write-back round (`WriteBackService`, `WriteBackCollector`, `WriteBackRound`, `WriteBackRoundContext`, `WriteBackTriggerService`, 3 interfaces) | **Stay** | D-6. |
| `Services/*/Encryption` (incl. `ConnectionSecrets`, `UnreadableSecretException`) | **Stay** | ADR-270 platform lane: the key ring. Owner S03f. |
| `DemoDataFactory`, `IDemoDataFactory`, `Factories/DemoData` | **Stay** in `Factories` | Platform lane: demo data. `Factories` then holds only demo data. |
| `Health/*` | **Stay** | Host readiness and liveness, not connection health. |
| Controllers, API DTOs, `API/Helpers` | **Stay** in `API` | As S03b/S03c: S12a/S12c. |
| `Models` types | D-2 | |

Totals: **102 production files move** (96 from `Services`). KPI 1 **424 → 328**. KPI 2 **4 of 11**. KPI 3 changes
only by D-2's admitted types.

### D-2 Context-owned `Models` types — RECOMMENDED: move only what is neither an entity, nor on the wire, nor reached by a kernel type

| Option | What | Verdict |
|---|---|---|
| **a (recommended)** Rule-based | A3 lists, from the IL, each candidate (`Models/OAuth/{OAuthCompleteResult, OAuthFlowContext, OAuthRefreshContext, OAuthStateClaims, OAuthTokens, OAuthCredentialStatus}`, `Models/ConnectionHealth/{ConnectionHealthState, ConnectionHealthVerdict, ConnectionHealthDto}`, `Models/Validation/ConnectionValidationResult`, `Models/WriteBack/*`, the connection option types) and admits it only if it is not an EF entity, not serialized by a controller, and not reached by any `Models` type. Admitted types move in B4 to `Application/OAuth` or `Application/ConnectionHealth`. | Honest: the kernel may not reach a module (`KernelRules.ModelsDoNotDependOnServices` covers `Modules`). |
| b Move all | — | Rejected: entities change the EF model; wire types wait for S12c; the connector port's signatures use `Models.Validation` and `Models.WriteBack` by design (S06 D-8). |
| c Move none | — | Rejected as a default; it is what a fails everywhere degrades to. |

Expected outcome (to confirm in A3): the OAuth flow/state/token value types are admitted; `OAuthCredential`,
`ConnectionHealthVerdict` (entities), `ConnectionHealthDto` (wire), `ConnectionValidationResult` and `Models.WriteBack`
(port signatures, wire) stay. The brief AC is rewritten at close-out to "context-owned `Models` types that are not
entities, wire types or port vocabulary have moved; the rest is listed for S04b/S12c" (as S03b D-7).

### D-3 Target path and sub-folders — RECOMMENDED

`Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/`:

```
Domain/                      translation rules: category crossing, state-transition mapper, parent and dependency source selectors
Application/                 the connector port, capabilities, read refusals, factories, auth schema, Board types
Application/OAuth/           OAuth service, registry, state tokens, exceptions, provider port (+ admitted Models value types)
Application/ConnectionHealth/ health service, cadence, its port (+ admitted Models value types)
Adapters/Connectors/{AzureDevOps, Csv, Jira, Linear, ServiceNow, Auth, OAuth}/
Adapters/OAuth/              the Jira, ADO and stub OAuth providers
Adapters/Background/         ConnectionHealthProber
```

No `Adapters/Persistence` (D-4). Sub-folders under `Application` follow Work Items' `Application/Dependencies`
precedent. Tests mirror it under `Lighthouse.Backend.Tests/Modules/TrackerIntegration/…`.

### D-4 S05e and connection persistence — RECOMMENDED: S05e not taken; repositories stay

| Option | Verdict |
|---|---|
| **a (recommended)** Repositories stay in `Services/Implementation/Repositories`; `ConnectionHealthService` → concrete `ConnectionHealthVerdictRepository` and `OAuthService`/health → `IRepository<>` listed in the Application outbound list | Keeps S03a a pure move; S05e replaces `IRepository<WorkTrackingSystemConnection>` with role ports and is the Story that decides where those ports and stores live. |
| b Move the three repositories to `Adapters/Persistence` (Flow metrics precedent) | Rejected now: `ConnectionHealthService` takes the concrete class, so the module's `Application` would reach its own `Adapters` (forbidden by the shared rule) until an edit; S05e would move them again. |
| c Take S05e inside S03a | Rejected: an edit Story (new ports, consumers rewired, encrypted-save path) inside a move Story; S05e's hard dependency S05a. |

### D-5 The two S06 leaks — RECOMMENDED: fixed by placement in B2, no signature change

1. `Board`, `BoardInformation` move into `Application` beside `IBoardInformationProvider`: the port no longer names an
   implementation namespace.
2. `IWorkTrackingConnectorFactory` and `WorkTrackingConnectorFactory` both land in `Modules.TrackerIntegration.Application`.
Both are namespace moves; `B2`'s oracle asserts them.

### D-6 Where write-back lives (ADR-270 "Left open") — RECOMMENDED

Tracker integration owns the **translation into tracker fields**: `IWriteBackTarget` (port, moves with the port) and
each connector's implementation (moves with the connector). The **round** (`WriteBackService`, collector, round,
context, trigger) decides *what* is written and *when*, from Forecasting's dates and Work Items' fields, and reaches
Flow metrics and Licensing: it stays where it is and is placed by the Story that packages its publisher (S03d
Promises/Delivery or a later Story). Recorded in ADR-270 "Left open" as answered. Alternative "move the round into
Tracker integration": rejected, it would make the ACL reach Flow metrics, Licensing and Forecasting.

### D-7 The Promises edges — RECOMMENDED: the port edge is legal and listed; the Jira → renderer edge is listed

| Edge | Options | Recommended | Why |
|---|---|---|---|
| Promises → Tracker integration (`DeliverySourceResolver`, `DeliveryForecastPublishingService` take the factory and type-test for `IDeliverySourceProvider` / `IDeliveryForecastPublisher`) — ratchet candidate 3 | a route through a new ACL query service; **b the factory + capability interfaces now *are* the ACL's port (ADR-285), so the edge is a customer of the port; list the two callers by name in the Application inbound list until Promises has a namespace (S03d)**; c leave unrecorded | **b** | After the move both reach only `Modules.TrackerIntegration.Application`, never `Adapters`. A wrapper service (a) would re-state the factory without hiding anything (pass-through layer). Candidate 3 is answered: "through the ACL's port since S03a". |
| Tracker integration → Promises (`JiraWorkTrackingConnector` → `IDeliveryForecastBlockRenderer.MergeInto`, E6) | a hand the merged text in as data (publisher merges, connector writes): an edit to the publication shape; **b list it as the one entry of "the module reaches no other context", owner S03d**; c move the renderer into Tracker integration | **b** (a is the target) | a changes the publication contract that S03d designs; c splits the renderer between rendering (Promises) and merging into a description (translation) — an edit, not a move. The list only shrinks. |

### D-8 The rule set — RECOMMENDED: by namespace, through `ModuleRules`, one new boundaries file

Written in `Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs` with `ModuleRules` and a
`ModuleWording` of its own (REUSE); the old `WorkTrackingIntegration_IsALeafInputAdapter` and
`WorkTrackingConnectors_DoNotDependOnAnyRepository` are restated on the module namespace and the old pattern retired
in C2. Rules and baselines in "Ratchet design".

### D-9 Tests move with production — RECOMMENDED (deviation from S03b D-9)

Each B slice moves the unit tests of the files it moves, in a separate `refactor(connectors): move … tests` commit in
the same slice, to `Lighthouse.Backend.Tests/Modules/TrackerIntegration/<same sub-folder>`. Why not a later C3 as S03b
did: the test paths are in the live-suite classifier (E3), so a separate test move would trigger every live suite a
second time on the shared Linear key. Integration tests under `API/Integration` stay. `TrackerIntegrationUnitTestPlacementTest`
lands in C2. The test-writing skill's "improve the tests you touch" is waived for move-only and `using`-only edits, as
S03c and S03b recorded; rows that move down to the service port are not part of a move.

### D-10 DI registration — RECOMMENDED

`Program.cs` lines and their order stay; only `using`s change (every B slice). No `AddTrackerIntegration` extension
(S09a). `DomainEventHandlerOrderPinTest` unaffected (no handler moves).

### D-11 CI selection, live suites and the Linear key — RECOMMENDED

- A2 widens `path-classifier.sh` so each tracker regex matches both `Services/Implementation/WorkTrackingConnectors/<T>/`
  and `Modules/TrackerIntegration/Adapters/Connectors/<T>/` (production and tests); Jira also
  `Modules/TrackerIntegration/Domain/ParentSourceSelector.cs`; `SHARED_REGEX` also `Adapters/Connectors/(Auth|OAuth)/`,
  `Modules/TrackerIntegration/Application/` and `Domain/` (the port, factories, translation rules, OAuth, health) and
  `Adapters/OAuth/`. Old alternatives are removed in C2 once nothing lives there.
- B pushes: every B slice touches `Program.cs` and old shared paths, so each runs all live suites in CI. Four B
  pushes = four live runs. To spare the shared Linear key, the B slices are **pushed together once B is complete and
  green locally** (commits stay per slice), unless the maintainer wants a push per slice.
- Locally, the live categories are not run (root context); CI runs them on the push.

### D-12 Behaviour-free proof and EXAMINE — RECOMMENDED

1. Existing tests green unmodified apart from moves, `using`s and rule files; live connector suites green in CI.
2. Reuse EXAMINE commands: `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `connector-throws-count`
   (after A2's fix), `refresh-answers-compare` (a refresh on demo CSV data goes through the factory, the CSV connector,
   the crossing rule and the selectors), `forecast-answers-compare`. No new command: no answer of the module is
   reachable without a tracker other than through the refresh (subtraction: a connection-answers command would
   compare endpoints that need live credentials).
3. Declared in `.nwave/examine.json` before the first EXAMINE.

### D-13 Mutation bar — RECOMMENDED

Last, on frozen code: "no survivor or no-coverage mutant added in the moved code"; whole-file `mutate` entries; the
same filter on old and new paths; the acceptance suite excluded (memory: Stryker must skip it).

## Wave: DESIGN / [REF] Slices

Every slice ≤ 1 day, its own oracle file, moves apart from edits. Three nwdes features. Size **L, about 7.5 days**.

| Id | nwdes feature | Delivers, in plain words | Oracle (new file; how it is RED first) | Production targets | Size |
|---|---|---|---|---|---|
| A1 | A `story-6359-s03a-a-rules-first` | Every architecture rule that names the context's namespaces, folders, paths or types also sees the future `Modules/TrackerIntegration`; a made-up connector there that reaches Work Items, Forecasting, the update code or a repository is caught | `Architecture/TrackerIntegrationRulesSeeTheModuleArchUnitTest.cs` (reuses the `WorkItemsRulesSeeTheModule` fixture approach). RED: the made-up offender under the module passes today's rules | none (test-side rule files, `RepositoryRecognition`) | 1 d |
| A2 | A | The CI test selection, the examine commands, test-time budgets and Stryker configs find connector code under the new paths too; a change to a moved connector still selects its live suite | `Scripts/test-selection/test-path-classifier.sh` gains one scenario per tracker plus shared, on module paths (RED: classified `false` today); `connector-throws-count` run against a module path (RED: "folder not found") | `path-classifier.sh`, `build-examine-commands`, `dev-test.ps1` | 1 d |
| A3 | A | The membership list and the `Models` admissions (D-2) are measured from the IL and fixed in a test; the IL edges of E5–E8 confirmed | `Architecture/TrackerIntegrationMembershipArchUnitTest.cs` (RED: fails when a listed type is missing from the list or the list names a type that does not exist) | none | 0.5 d |
| B1 | B `story-6359-s03a-b-move` | The translation rules (category crossing, state-transition mapper, parent and dependency source selectors) live in `Modules/TrackerIntegration/Domain`, which reaches only the kernel; Work Items' outbound list loses the selectors | `Architecture/TrackerIntegrationDomainLayoutArchUnitTest.cs` (RED: types not in `Domain`) | 5 files + tests, `Program.cs` usings | 0.5 d |
| B2 | B | The connector port, the factories, the auth schema and the board types live in `Application`; the factory interface and class share a namespace; the board port names no implementation type | `Architecture/TrackerIntegrationApplicationLayoutArchUnitTest.cs` (RED: types in old namespaces; leak assertions fail) | 15 + 4 factories + 2 schema + 2 boards + tests | 1 d |
| B3 | B | All five connectors, their helpers, the auth strategies and Jira's issue factory live in `Adapters/Connectors/<Tracker>` | `Architecture/TrackerIntegrationConnectorsLayoutArchUnitTest.cs` (RED: types under `Services`) | 52 files (50 + `IssueFactory` pair) + 93 tests, Stryker configs, `test-time-budget.json` | 1 d |
| B4 | B | OAuth and connection health live in `Application/OAuth`, `Application/ConnectionHealth`, `Adapters/OAuth`, `Adapters/Background`; admitted `Models` value types with them | `Architecture/TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.cs` (RED: types under `Services`) | 22 files (+ D-2 admissions) + tests | 1 d |
| C1 | C `story-6359-s03a-c-edges-and-rules` | Tracker integration has its own module rules with inline lists: Domain, Application inbound and outbound, Adapters reached only by the composition root (plus list), the module reaches no other context (one listed entry, D-7); the old leaf pattern is retired | `Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs` (RED: a made-up offender in each list position; before C1 the file does not exist, falsified per rule with `ModuleRulesCatchANewOffender`) | rule files; old `WorkTrackingIntegrationPattern` removed; classifier old alternatives removed | 1 d |
| C2 | C | Every unit test of the module sits under `Tests/Modules/TrackerIntegration`, and no file of the context is left in `Services/Interfaces` or `Services/Implementation` apart from the named exceptions | `Architecture/TrackerIntegrationUnitTestPlacementTest.cs` + a "no file left" path check (RED: a made-up test or source file in an old folder is caught) | none | 0.5 d |

Verification commands per slice (fast loop, then the full gate at the end of the slice):

```
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~<OracleClass>"
bash Scripts/test-selection/test-path-classifier.sh                      # A2, C1
dotnet build Lighthouse.Backend                                          # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

Why three features: rules only; pure moves; edits. B4 and C1 can swap order without touching A. Finalize (not nwdes
slices): mutation on frozen code (D-13); ADR-270, `ARCHITECTURE.md` §4, brief AC (D-2), catalogue; the Lighthouse
finalize checklist.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list inline, compared both ways, never loosened (ADR-268).

### Rules first (A1): what is widened

| File | Keys on | Widening |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | `WorkTrackingIntegrationPattern` | add `^Lighthouse\.Backend\.Services\.Factories$` and `^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.\|$)`; the Portfolio/Delivery side gains `Services.(Implementation\|Interfaces).DeliverySources` **only after** D-7's entry is listed (C1), otherwise A1 goes red on E6 |
| `RepositoryRecognition.PlatformPersistencePattern` | `Services.*.OAuth` | add `Modules.TrackerIntegration.(Application\|Adapters).OAuth` (E2) |
| `DependencySingleDecisionArchUnitTest`, `CalendarDayAnchorSeamArchUnitTest`, `JiraRouteLiteralsTest`, `FeatureSourceCapabilityArchUnitTest`, `WriteBackTargetCapabilityArchUnitTest`, `ConnectionHealthSingleWriterArchUnitTest`, `ConnectorsDeclareNoUnsupportedMembersArchUnitTest`, `ConnectorPortSpeaksPortfolioArchUnitTest`, `ConnectorRatchetFixtures`, `ServiceNowHistoryPurityArchUnitTest`, `ServiceNowValidationVerdictPurityArchUnitTest`, `CategoryListOwnershipGuardTest`, `SecretCustodySeamArchUnitTest` | source paths, full-name strings, `typeof` | locate files by name under the production source; full names read from `typeof(...)` instead of strings; `using`-only otherwise |
| `WorkItemsMembershipArchUnitTest`, `WorkItemsModuleBoundariesArchUnitTest`, `WorkItemsApplicationLayoutArchUnitTest`, `WorkItemsUnitTestPlacementTest`, `RefinementModuleArchUnitTest` | the selectors' names, connector namespace | rewritten in B1 (the selectors leave Work Items' notes and outbound list) |
| `TestHostUseGuardTest.HostUsersStillToMove`, `TestHostIsReleasedAfterDisposeTest`, `TestHostPerFixtureGuardTest`, `TestTierGuardTest` | test class names / namespaces | names rewritten where a listed test moves (B3/B4) |

### New rules (C1), baselines measured from the IL in C1

| # | Rule | Expected baseline |
|---|---|---|
| R-T1 | Each listed type lives in its layer; none left under `Services` or `Factories` (`ModuleRules.TypesLiveInTheirLayer`) | D-1 lists |
| R-T2 | `Domain` reaches only `Models` and the BCL | empty |
| R-T3 | Outside → `Domain` only from the list | `Program`; Work Items' `WorkItemService` if it still calls `ParentSourceSelector` (S03b E3) |
| R-T4 | Outside → `Application` from `API`, the composition root and Work Items by namespace, plus the list | `DeliverySourceResolver`, `DeliveryForecastPublishingService` (D-7 b), `WriteBackService` (D-6), `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase` (6474 slice 12), `ConnectionSaveService`? (unknown), Encryption/demo-data readers (unknown) |
| R-T5 | Outside → `Adapters` only from the composition root, plus the list | `DemoBlockedHistoryBackfillHandler`, `DemoPercentilesBackfillHandler` (E5); `API` controllers that read Jira types (`WorkTrackingSystemConnection(s)Controller`) |
| R-T6 | `Application` → no EF Core, `Data`, `API`, other modules' `Adapters`; platform only through the list | `ConnectionHealthService` → `ConnectionHealthVerdictRepository`, `UnreadableSecretException`; `OAuthService` → Encryption type(s) |
| R-T7 (leaf) | The module reaches no other context (Work Items, Forecasting, Flow metrics, Promises/update code, Refinement, Reports, Access, Licensing) | 1: `JiraWorkTrackingConnector` → `IDeliveryForecastBlockRenderer` (D-7) |
| R-T8 | No module type reaches a repository except the `Application` entries of R-T6 (restates "connectors do not depend on any repository" for `Adapters/Connectors`) | empty for `Adapters/Connectors` |
| Work Items | Outbound to Tracker integration | legal by namespace; its "connectors" rule now reads the module namespace |

## Wave: DESIGN / [REF] Component decomposition

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| Crossing, transition mapper, 3 selectors | `Services/Implementation/{WorkTrackingConnectors, Parents, Dependencies}` | `Domain/` | MOVE | B1 |
| Port (15), factories (4), auth schema (2), boards (2) | `Services/Interfaces/WorkTrackingConnectors`, `Factories`, `…/WorkTrackingConnectors`, `…/Boards` | `Application/` | MOVE | B2 |
| 5 connectors and helpers, auth strategies, OAuth schema helpers, `IssueFactory` | `…/WorkTrackingConnectors/*`, `Factories` | `Adapters/Connectors/<T>/` | MOVE | B3 |
| OAuth service etc., providers, health, prober | `Services/*/OAuth`, `…/ConnectionHealth`, `…/BackgroundServices` | `Application/OAuth`, `Adapters/OAuth`, `Application/ConnectionHealth`, `Adapters/Background` | MOVE | B4 |
| Rule files, scripts, configs | tests, `Scripts` | same | EDIT | A1, A2, C1 |
| Repositories, connection save/usage, write-back round, encryption, demo data, controllers, DTOs, entities, events | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: connection, OAuth, connection-health, wizard, delivery-source, Team and Portfolio controllers (routes,
guards, responses untouched). Host: `ConnectionHealthProber`, the refresh updaters. In-process: Work Items →
connector port; Promises → connector port (D-7); write-back round → `IWriteBackTarget`.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IWorkTrackingConnector` + capabilities | Jira, ADO, Linear, CSV, ServiceNow connectors | moved, unchanged |
| `IWorkTrackingAuthStrategy` | 6 auth strategies | moved, unchanged |
| `IOAuthProvider` | Jira, ADO, stub providers | moved, unchanged |
| `IRepository<WorkTrackingSystemConnection>`, `IRepository<OAuthCredential>`, `ConnectionHealthVerdictRepository` | platform repositories | stay (D-4) |
| `ICryptoService`, key ring | platform lane | unchanged, listed |
| No new external call, adapter or package. | | |

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET, `IlDependencies` over Mono.Cecil, NUnit, Stryker.NET, bash scripts.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `ModuleRules` + `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | module rules and their falsifiers | **REUSE**; no rule copied |
| `WorkItemsRulesSeeTheModuleArchUnitTest` / `ForecastingRulesSeeTheModuleArchUnitTest` | made-up offender under a module | **REUSE** the fixture approach for A1 |
| `WorkItemsMembershipArchUnitTest` | membership list | **REUSE** shape for A3 |
| `WorkItemsUnitTestPlacementTest` / `ForecastingUnitTestPlacementTest` | placement | **EXTEND** (parameterise) or sibling, decided in C2 |
| `ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern` and its two rules | the leaf rule | **EXTEND** in A1, **REPLACE** by R-T7/R-T8 in C1 |
| `RepositoryRecognition` | stored-records and platform patterns | **EXTEND** (E2) |
| `ConnectorRatchetFixtures` (S06) | connector port fixtures | **REUSE** |
| `Scripts/test-selection/path-classifier.sh` + `test-path-classifier.sh` | live-suite selection and its tests | **EXTEND** |
| EXAMINE commands (`architecture-rules`, `ef-model-unchanged`, `backend-tests`, `connector-throws-count`, `refresh-answers-compare`, `forecast-answers-compare`) | behaviour-free proof | **REUSE**; `connector-throws-count` **EXTEND** (path) |
| CREATE_NEW | `Modules/TrackerIntegration` folder tree; the 9 oracle files | no existing owner of the module's layout or rules |

Semantic duplicates checked: none found. The connection auth schema (`AuthenticationMethodSchema`) and the auth
strategy factory decide different things (what a connection asks for vs how a request is signed); both move, not merged.

## Wave: DESIGN / [REF] Impact closure

| Locator | Kind | Decision | Reason |
|---|---|---|---|
| 102 production files of D-1 | source | CHANGED (moved) | D-1 |
| `Program.cs` | composition root | CHANGED (`using`s) | D-10; forces full CI suite |
| `API/*` controllers, DTOs (E-list inbound) | consumers | CHANGED (`using`s) | |
| `Modules/WorkItems/Application/WorkItemService`, Flow metrics back-fill handlers, updaters, Delivery services, `WriteBackService` | consumers | CHANGED (`using`s) | |
| 114 test files of the set + 192 test files `using` the set | tests | MIGRATED (moved) / CHANGED (`using`s) | D-9 |
| 23 architecture rule files (E13), `RepositoryRecognition` | tests | CHANGED | A1, C1 |
| `Scripts/test-selection/path-classifier.sh`, `test-path-classifier.sh`, `dev-test.sh`, `dev-test.ps1` | CI selection | CHANGED | E3, A2 |
| `.github/workflows/ci_changes.yml`, `ci_backend.yml` | workflows | UNCHANGED_COMPATIBLE | they call the classifier; categories are by `TestCategory`, not path |
| `Scripts/examine/build-examine-commands`, `bin/connector-throws-count` | examine | CHANGED | E4 |
| `Scripts/test-timings/test-time-budget.json`, its 3 Python tests | test timings | CHANGED (names) / to check | E11 |
| `Lighthouse.Backend/stryker-config.json`, 4 `Tests/stryker-config.*.json` | mutation | CHANGED | E10 |
| `docs/feature/*/mutation/*.json` | history | UNCHANGED | archived |
| `Lighthouse.Backend.csproj`, `Lighthouse.Backend.Tests.csproj` | build | UNCHANGED_COMPATIBLE | SDK globbing; `Factories/DemoData` stays — to confirm no `<None Include>` names a moved path in A2 |
| `appsettings*.json` | config | UNCHANGED_COMPATIBLE | no key names a moved type (E12) |
| EF model snapshots, migrations | persistence | UNCHANGED | D-0 |
| Frontend, Lighthouse-Clients | | UNCHANGED | no wire change |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, UI unchanged; no schema change (D-0). |
| RBAC | N/A, because no endpoint, guard or authorisation call changes. |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing a user does changes. |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. Code: `Modules/TrackerIntegration`. |
| Log categories | Change with the namespace for every moved logger; no configuration keys on them (E12). |
| `Program.cs` | Edited in every B slice: the push runs the full CI backend suite and the live tracker suites. |
| Epic 6474 | No shape reversed: updaters stay (they are listed callers of the port); the mapping still arrives as data. |

## Wave: DESIGN / [REF] Residuality

Stressors from negated assumptions, walked against the finished design (walkthrough, not execution).

| Assumption negated | What happens | Preserved / forbidden | Residue, owner |
|---|---|---|---|
| CI selects live suites by test category only | False: by path (E3). Without A2 a later Jira change would skip its live suite | Forbidden: a connector change that runs no live suite | A2 before any move; C1 removes old alternatives |
| DI resolves by type, not by namespace | Holds: registrations are by type; scanning by namespace: none found by text search | — | an assembly scan by namespace string would be caught by the full suite; unknown → none found |
| No JSON payload carries a .NET type name | No `TypeNameHandling` / `$type` found in the set by text search; OAuth state tokens carry claims, not type names | Same serialized bytes | checked again in A3 from strings in the IL |
| EF does not key on namespaces | Entities stay in `Models` (D-0) | No migration | `ef-model-unchanged` |
| No configuration filters logs by these namespaces | appsettings checked (E12); Serilog filters by namespace: none found | Log levels unchanged | a host-side config outside the repo could name a category: accepted, unknown |
| Tests do not hold namespaces as strings | Two purity tests do (E12), `test-time-budget.json` does | rewritten in A1/B3 | |
| Stryker globs are path-free | False (E10) | rewritten with the move | |
| The ACL depends on no downstream context | False for one edge (E6) | listed, only shrinks | S03d |
| The shared Linear key absorbs four live runs | Possibly not (429) | one push for B (D-11) | maintainer may ask for per-slice pushes |
| A connector type is referenced through `nameof`/reflection in a test helper | Compiles after the move, same name | — | none |

Second set, not used to derive the design: a worktree without the premium licence fixture (2 Licensing failures,
environmental, unrelated to the move); a parallel S03d moving `DeliverySources` at the same time (its rules would see
R-T4/R-T7 entries renamed: coordinate through the lists, each only shrinks); the Standalone build packaging the
backend (namespace changes do not change the published assembly name).

## Wave: DESIGN / [REF] Formal verification

**Not applicable, with the reason.** The formal-verification skill was read (procedure and checker selection). S03a's
claims are: (1) "the same types exist in other namespaces and the dependency edges are exactly the listed ones",
decided exactly by the compiled IL with a made-up offender per rule as falsifier; (2) "behaviour is unchanged",
decided by the existing suites, the live connector suites and the answer comparisons, since both sides run the same
code. There is no state, ordering, concurrency, recovery or liveness claim: no handler, queue, schedule or transition
changes (D-10). A Quint or TLA+ model would restate the namespace map and could not fail. **No checker (Quint, TLC,
Agda) was probed or run.** The skill states routine designs are not exempt; if the maintainer wants a model, the
claim is **INDETERMINATE** until a checker is probed and run.

## Wave: DESIGN / [REF] Test substrate

- C# .NET 10, NUnit 4.6, Mono.Cecil via `IlDependencies`; bash for the classifier tests. No new package.
- Oracles are architecture tests (`Lighthouse.Backend.Tests/Architecture/*`) and the classifier test script; the claims
  are about types, paths and selection, not behaviour. Behaviour stays covered by the existing scenarios at the
  application-service port and the connector tests, which move unchanged.
- Timing: each new architecture test reads the cached IL module, as the existing ones do; within the default budgets.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature C.

- **ADR-270**: Decision 1 Tracker integration row: "**Packaged** in `Modules/TrackerIntegration` by Story 6359 (date).
  Entities, the connection option types, `Models.WriteBack`, `ConnectionValidationResult` and wire DTOs stay in
  `Models` and `API/DTO`. Connection persistence stays with the platform repositories until S05e; the connection save
  and usage stay for Organisation set-up (S03i); the write-back round stays with its publisher." Decision 6: Tracker
  integration folder counts. "Every edge": Promises → Tracker integration through the port (D-7); the Jira → renderer
  entry. Ratchet candidate 3: answered (D-7). New block "Edges measured by packaging Tracker integration" (R-T1–R-T8).
  "Left open": write-back answered (D-6). Glossary: Tracker integration rows point at `Modules/TrackerIntegration`.
- **ARCHITECTURE.md** §4: row 1 renamed to Tracker integration with `Modules.TrackerIntegration.*` anchors; the
  diagram node `WT` relabelled; the leaf-rule bullet (line 150) restated by namespace with its one listed entry.
- **brief.md**: new section `## Application Architecture — story-6359 s03a package tracker integration`.
- **Epic catalogue**: Improvement log entry; KPI 1/2; slice-03 brief AC on `Models` corrected (D-2); S05e note that
  the repositories wait for it; S03d note on D-7's two edges; S03i note on the connection save and usage.

## Wave: DESIGN / [REF] Architecture summary

**Shape in hexagonal terms.** Everything below is MOVED unless marked; no new external call.

```
 driving                         Modules/TrackerIntegration                              driven (external)
 ───────                         ─────────────────────────────────────────────────       ─────────────────
 API controllers (unchanged) ─►  Application: connector port + capabilities,             Adapters/Connectors:
 Work Items refresh ─────────►     factories, auth schema, Board types   ◄── implements ── Jira, ADO, Linear,
 Promises delivery sources ──►   Application/OAuth ───────────────────── ◄── implements ── CSV, ServiceNow,
 write-back round (stays) ───►   Application/ConnectionHealth                              Auth strategies
 Adapters/Background:            Domain: category crossing, state mapper,                Adapters/OAuth:
   ConnectionHealthProber ──►      parent/dependency source selectors                      Jira, ADO providers
                                         │                                                 │
                                         ▼                                                 ▼
                                 Models kernel (connection, OAuth credential,       trackers' REST APIs
                                 health verdict entities: unchanged)
                                 Platform: repositories, key ring, crypto (unchanged)
```

- Driving adapters: controllers (unchanged), `ConnectionHealthProber` (moved to `Adapters/Background`).
- Application services and ports: connector port and factories (moved, two S06 leaks closed by placement), OAuth
  service, connection health service (moved).
- Domain: translation rules (moved, selectors included).
- Driven adapters: five connectors, auth strategies, OAuth providers (moved); repositories (unchanged, S05e).

**Patterns weighed.**

| Pattern | Verdict | Why |
|---|---|---|
| Anti-corruption layer | chosen (restated) | Trackers' models stay inside; the leaf rule becomes a namespace rule with one listed exception |
| Ports next to their use case (DIP) | chosen | ADR-270 Decision 6; the port moves into `Application`, not a global interfaces folder |
| Capability interfaces (ISP, ADR-285) | kept | S06 shaped the port; S03a moves it as is |
| A query service wrapping the factory for Promises | rejected | a pass-through layer; the factory + capabilities already are the port (D-7) |
| Domain event for "connection stopped working" | rejected for now | a behaviour change; stays target in ADR-270 |
| Repositories into `Adapters/Persistence` | rejected for now | `Application` takes a concrete repository; S05e owns the ports (D-4) |
| One slice per connector folder | rejected | each push runs every live suite on a shared key; one connector slice (D-11) |
| Moving `Models` entities | rejected | changes the EF model (D-0) |

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-16)

- Platform lens: N/A, because nothing deploys, configures or runs differently; the one CI concern (path-based live
  suite selection) is an A2 slice, not platform risk.
- Usage-data event: N/A, because nothing a user does changes.

## Wave: DESIGN / [REF] Open questions for the maintainer

None blocks DELIVER. Confirmations, each with the AFK default taken:

1. **D-11 push cadence**: push B once when complete (default), or per slice (four live runs on the shared Linear key)?
2. **D-7**: the Jira connector's merge through Promises' renderer is listed for S03d rather than reshaped now. Agree?
3. **D-1**: the connection save and usage stay for Organisation set-up (S03i); write-back round stays with its
   publisher (D-6). Agree?
4. **Size**: L, about 7.5 days, 9 slices, 3 nwdes features.

Not verified at DESIGN (each named with the slice that measures it): IL edges (all inbound/outbound lists are
`noisy` text search; A3/C1); D-2 admissions (A3); connection-health test files and `Models` file count (A3); the three
`Scripts/test-timings` Python tests' contents (A2); per-hit content of the 23 rule files (A1); csproj item paths (A2).

## Wave: DELIVER / [REF] Start of DELIVER (maintainer, 2026-11-16)

- **Rigor**: `thorough` for features A and C (crafter, acceptance designer, architect on opus; reviewers sonnet),
  `standard` for feature B (sonnet; reviewers haiku). EXAMINE on the reviewer model of its feature.
- **Push**: per slice, B included (maintainer chose per-slice pushes on entry and kept them over D-11's "B once");
  CI watched per push. Trunk moved: rebase allowed (maintainer, 2026-11-16).
- **D-7 and D-1 confirmed** as recommended: the Jira → Promises renderer edge is listed and left for S03d; connection
  save and usage stay for S03i; the write-back round stays with its publisher.
- **UI sketch**: N/A, nothing a user sees changes. Loading and failure looks: N/A, no view.
- **Slice wrap-up**: refactor + review per slice; mutation once at the end on frozen code.

## Wave: DESIGN / [REF] D-2 amended (A3 oracle review, 2026-11-16)

The A3 oracle review found D-2's admission rule too wide. Amended, recommended and taken in AFK mode: a `Models`
type moves to Tracker integration only if it is (a) not an EF entity, (b) not on the wire, (c) not reached by a kernel
type, **and (d) not used by any code that stays outside the module** (staying services, controllers, other contexts,
events consumed elsewhere). Consequences:

- `ConnectionFetchShapeChanged` stays in `Models/Events`: the staying save service publishes it, Work Items handles it.
- The write-back vocabulary (`WriteBackFieldUpdate`, `WriteBackItemResult`, `WriteBackResult`, `WriteBackValueSources`)
  stays in the kernel as the connector port's shared vocabulary (port signatures use only kernel types, S06 D-8).
- `OAuthCompleteResult` stays: `OAuthController` (which stays) reads it from `IOAuthService.CompleteAsync`.

A3's oracle and its membership rule check all four conditions.

## Wave: DESIGN / [REF] D-14 Application types that name the connectors (B2 slice review, 2026-11-17)

The B2 slice review found that three `Application` types name connector types: `WorkTrackingConnectorFactory` and
`WorkTrackingSystemFactory` construct the connectors, and `AuthenticationMethodSchema` reads each tracker's option
names. After B3 moves the connectors to `Adapters/Connectors`, these are `Application` → own `Adapters` edges, which the
module rules of C1 forbid by default.

| Option | Verdict |
|---|---|
| **a (recommended, taken in AFK mode)** Keep the three in `Application` and list each edge by exact type name in C1's `Application` outbound list, owner S09a (the composition root takes over connector construction when `AddTrackerIntegration` arrives) | Keeps B a pure move and keeps D-5's "factory interface and class share one namespace", which B2's oracle pins. The list only shrinks. |
| b Move the two factory classes to `Adapters/Connectors` | Rejected now: breaks D-5's leak fix and B2's approved oracle; S06 D-8 made the factory part of the port. |
| c Give `Application` its own option-name source for the schema | Rejected now: an edit inside a move Story; the option names are each connector's vocabulary. |

Exemptions are exact per type, never per namespace or layer (S1 learning). C1 owns the list entries.

## Wave: DESIGN / [REF] D-1 outcome of the B1 Domain rule (candidate review, 2026-11-17)

The D-1 row "WorkItemCategoryCrossing … DependencyFieldReferences → Domain (rejects go to Application,
recorded)" resolved in B1 as follows, and this record is that recording:

| Type | Layer | Why |
|---|---|---|
| `WorkItemStateTransitionMapper`, `ParentSourceSelector`, `DependencyFieldReferences` | `Domain` | They reach only the kernel and the base library. |
| `WorkItemCategoryCrossing`, `DependencySourceSelector` | `Application` | They call helpers in `Lighthouse.Backend.Extensions`, which the Domain rule rejects. |

Feature C keys the module lists by full name where a short name is shared: the moved AzureDevOps
`WorkItemExtensions` and the unmoved `Lighthouse.Backend.Extensions.WorkItemExtensions` have the same
short name, and the name-only lists in `TrackerIntegrationMembership` cannot tell them apart.
