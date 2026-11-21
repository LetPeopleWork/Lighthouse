# Feature: story-6364-s03f-platform-composition-root

Story #6364 · S03f Platform composition-root infrastructure (no module) · Epic #6510 (Modules: package the bounded
contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03f row, "Notes from S02", the S03a/S03d notes, line "S03f (Platform): `DatabaseMaintenanceGate` reaches the Flow
  metrics Application (R4 list)", risk 3 "EF Core migrations and the two migration assemblies")
- Brief: [`slices/slice-08-s03f-package-platform-persistence.md`](../epic-6264-modular-monolith/slices/slice-08-s03f-package-platform-persistence.md)
  (body, hypothesis, "Update after S01": retitled, Platform is not a context)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) ("*(Platform
  lane)*" row, "Every edge" rows Flow metrics → Platform lane, Platform lane → PostHog, GitHub Releases → Platform lane,
  Decision 6 "No folder for … Platform (composition root)", "Answers to the brief's open questions", "Left open: the
  update queue", lessons 1-9 of the Flow metrics pilot), [ADR-271](../../product/architecture/adr-271-ui-surfaces-follow-units-and-levels-and-settings-split-by-audience.md)
  (no UI surface here).
- Precedents, same shape: [S03e Access and Licensing](../story-6363-s03e-package-access-licensing/feature-delta.md)
  (designed 2026-11-17; **not yet delivered**, it is delivered before this Story starts), S03d record
  `docs/evolution/2026-11-17-story-6362-s03d-package-promises-deliveries/evolution.md`, S03a, S03b, S03c records.

DISCUSS: covered at Epic level (D-6264-3, D-6264-4, D-6264-16); waves start at DESIGN.

DESIGN run 2026-11-21, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is recorded as
such. Nothing in this Story changes what a user sees or is told, so no decision is held; the open questions at the end
are confirmations, not blockers. Nothing was built or run with `dotnet` or `pnpm` (another delivery held the build);
every count is a **text measurement on `main` at `d2d46f9fc`** (`git ls-files`, `git grep`), `noisy`.

**Start condition.** This Story starts only after **S03e (Story 6363) is closed** (features A, B and C, mutation,
finalize, CI green). S03e edits the same rule files (`ModuleBoundariesArchUnitTest`, the Tracker integration, Work
Items, Forecasting and Promises boundaries files), adds `AccessModuleBoundariesArchUnitTest` and
`LicensingModuleBoundariesArchUnitTest` whose lists name platform types by their old full names (R-A6, R-L3, R-L5:
repository ports, top-level `Services.Interfaces` types, the usage-data readers), and moves the Licensing test data.
Every count below is **re-measured at the Story's start (A1)**; where a count was not finished at DESIGN it says so.

## Wave: DESIGN / [REF] Inherited

Linked, not copied.

- **ADR-270 Platform lane row**: "Composition-root infrastructure below the domain: the encryption key ring, backups
  and restore, demo data, version update, log level, usage-data consent and forwarding, start-up housekeeping, the
  `DbContext` and database management, the domain-event dispatcher, the update queue's mechanics, health and graceful
  shutdown. … Stays in the composition root." **Not a context**; no `Modules/Platform` folder (Decision 6).
- **ADR-270 answers**: "Each context owns its persistence adapters; the `DbContext`, database management and the
  dispatcher stay in the platform lane." "Left open: the update queue" → "the queue mechanics go with the platform lane
  (S03f) and Epic 6474 slice 12 replaces the updaters."
- **ADR-270 "Every edge"**: Flow metrics → Platform lane (`DatabaseMaintenanceGate` reaches Flow metrics' Application,
  "ratcheted in the R4 list until S03f decides"); Platform lane → PostHog; GitHub Releases → Platform lane.
- **ARCHITECTURE.md §4**: row 7 "Platform / Persistence" (`Data`, `Services.*.Repositories`, `.DatabaseManagement`,
  `.DomainEvents`, `.Seeding`); "The composition root & host band — `Program.cs`, `Startup/`, `Configuration/`,
  `Health/`, `Standalone/`. Not a module and not under the boundary rules."
- **Brief hypothesis**: "Platform code can leave the layer folders without touching EF migrations. Disproved if moving
  `LighthouseAppContext` or the configurations forces migration-assembly changes. In that case stop at the namespace
  move and leave the rest to S10." **Brief OUT**: entity configuration per context and table ownership (S10);
  persistence ports per aggregate (S05c–e).
- **Brief notes**: read the key-ring and dev-server notes in `CLAUDE.md` before touching `Encryption`; never point a
  key store at `data-protection-keys`; `Program.cs` edits force the full CI suite.
- **Packaging rules** (catalogue "Notes from S02", S03a/S03d/S03e lessons): rules first, proven before any type moves;
  edges from the compiled IL (`IlDependencies`); inline lists compared both ways, never loosened (ADR-268); lists keyed
  by full type name; exemptions exact per type; one oracle file per slice, an approved oracle is never edited; moves
  apart from edits; `Program.cs` registrations unchanged (lines and order), only `using`s change; every EXAMINE
  expectation observable through an admitted probe; mutation bar "no survivor or no-coverage mutant added in the moved
  code"; documentation edits in the finalize checklist, not in a slice; no review window (behaviour-free).

## Wave: DESIGN / [REF] DoR re-run (2026-11-21)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | The platform lane is spread over `Services/{Interfaces,Implementation}/{Encryption, DatabaseManagement, UsageData, Logging, DomainEvents, Seeding, Update, BackgroundServices}` and 34 loose top-level files. No rule names the lane; about 20 rule patterns name it indirectly as "Services" (E1). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files and edges below. |
| 4 UAT | PASS (catalogue) | Brief AC, CI-observable; the acceptance designer writes the oracles in DELIVER. |
| 5 AC from UAT | PASS, three ACs corrected | (a) "No platform file remains in `Services/Interfaces` or `Services/Implementation`": holds for the 137 files of D-2; the repositories (D-4), the update pipelines (D-6) and the context seeders (D-5) are **not** platform files by ADR-270 and stay with named owners. (b) "Migrations untouched": holds **only because `Data` stays** (E2, D-0); the brief's own stop rule applies to `LighthouseAppContext`. (c) "Domain-event handlers live with the context that reacts": **already true** (E9); the dispatcher and its two contracts move. |
| 6 Right-sized | PASS, re-sized | Brief says L. **137 production files** move, **about 65 unit-test files** move (+2 decided in A1). DESIGN sizes it **L, about 10-11 days, 11 slices, 3 nwdes features** (A 4, B 5, C 2). |
| 7 Technical notes | PASS | Key-store directory names, Redis channel names, the SignalR hub route, the data-protection application name and the Recent problems source column are string constants or short type names, not namespaces (E6). Log categories of moved loggers change (E7). |
| 8 Dependencies | PASS | Hard: S01 Closed, S02 gate go-with-changes. **Added: S03e closed first** (shared rule files). Soft: S05c-e and S10 own the repositories and `Data`; S03i owns the Organisation set-up files left in `Services`; Epic 6474 slice 12 owns the updaters; #6657 the write-back round. |
| 9 Outcome KPIs | PASS | KPI 1 (files under `Services`), KPI 3 (`Models` files). KPI 2 unchanged (Platform is not a context; the catalogue target "8 + Platform as composition root" is met for the Platform part). |

Hypothesis in the brief: **kept, narrowed**. Platform code leaves the layer folders without touching a migration. It
is disproved for `LighthouseAppContext` alone: 280 files in the two migration projects name `Lighthouse.Backend.Data`
(E2), so `Data` stays where it is and S10 owns it (the brief's own stop rule).

## Wave: DESIGN / [REF] Baseline re-count (2026-11-21, `main` at `d2d46f9fc`)

Counted with `git ls-files`; edges from `using` lines and `git grep -w` of type names. **Text search, `noisy`**: it
misses same-namespace and fully qualified references and matches names in comments. The IL measurement is A1's
(membership) and C1's (lists).

| Measure | Today | After S03e (forecast, not measured) | After this Story (forecast) |
|---|---|---|---|
| Files under `Services` (KPI 1) | **321** | about 288 (−33) | about **151** (−137); re-measured at the Story's start (A1) |
| Loose files at the top of `Services/Interfaces` + `Services/Implementation` | 32 + 41 = **73** | 73 | 16 + 23 = **39** (−34) |
| `Models` files (KPI 3) | **262** | 262 minus S03e's admissions | minus this Story's admissions (0 to a few; A1) |
| Contexts packaged (KPI 2) | 5 of 11 | 7 of 11 | 7 of 11 (Platform is not a context) |
| `Program.cs` | 1851 lines, 27 `using Lighthouse.Backend.Services…` lines | | `using`s only |

### The platform set by ADR-270 (production, 137 files)

| Area (target folder) | Files today | Count |
|---|---|---|
| `Platform/Encryption` | `Services/Implementation/Encryption/*` (28), `Services/Interfaces/Encryption/*` (7), `CryptoService` + `ICryptoService`, `BackgroundServices/KeyRingFileWatcher` | 38 |
| `Platform/DatabaseManagement` | `Services/Implementation/DatabaseManagement/*` (6: command runner, maintenance gate, management service, operation tracker, Postgres and SQLite providers), `Services/Interfaces/DatabaseManagement/*` (10) | 16 |
| `Platform/UsageData` | `Services/Implementation/UsageData/*` (8), `Services/Interfaces/UsageData/*` (7), `BackgroundServices/UsageDataConsentPruningService`, `BackgroundServices/UsageDataForwardingService` | 17 |
| `Platform/Logging` | `Services/Implementation/Logging/{RecentProblemsReport, RecentProblemsSink}`, `IRecentProblems`, `IRecentProblemsReport`, `SerilogLogConfiguration` + `ILogConfiguration`, `ConfigFileUpdater` + `IConfigFileUpdater` | 8 |
| `Platform/DomainEvents` | `DomainEventDispatcher`, `IDomainEventDispatcher`, `IDomainEventHandler<T>` | 3 |
| `Platform/UpdateQueue` | `BackgroundServices/Update/*` minus `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase<T>` (21: admitted work and cancellations, in-process / Redis / Postgres locks, notifiers and status stores, cancellation context and policy, `UpdateKey`, `UpdateMoments`, `UpdateProgress`, `UpdateStatus`, `UpdateSubstrate`, `UpdateEntityKinds`, `UpdateTaskNaming`, `UpdateQueueService`, `UpdateNotificationHub`); `Interfaces/Update/*` minus `ITeamUpdater`, `IPortfolioUpdater`, `IUpdateService` (6); `RefreshRoundSummary`, `RefreshRoundSummaryLine` | 29 |
| `Platform/Releases` | `GitHubService`, `LighthouseReleaseService`, `AssemblyService` + 3 interfaces | 6 |
| `Platform/Host` | `PlatformService` + interface + `PlatformExtensions`, `SystemInfoService`, `FileSystemService`, `ProcessService`, `ServiceConfig`, `LighthouseClock`, `RandomNumberService` + their interfaces | 15 |
| `Platform/DemoData` | `DemoDataService` + `IDemoDataService` | 2 |
| `Platform/Seeding` | `ISeeder`, `InstallTimestampSeeder` | 2 |
| `Platform/Persistence` | `LostRaceTolerantSave` (the save that tolerates a lost insert race; Flow metrics' two snapshot writers use it) | 1 |
| **Total** | | **137** |

Exact split of the update code (which types only the updaters use) and the `Models` admissions: **re-measured at the
Story's start (A1)** from the IL.

### Touching the set but not moving (D-0, D-4 to D-6 say why)

| Group | Files | Owner / reason |
|---|---|---|
| `Data/*` (`LighthouseAppContext`, factory, configurator, converters, interceptors) | 7 | S10; 280 migration files name its namespace (E2) |
| Repositories | `Services/Implementation/Repositories/*` (29), `Services/Interfaces/Repositories/*` (17), incl. `IRepository<T>`, `RepositoryBase<T>`, the usage-data consent repository | S05c-e, S10; each context's persistence adapters (D-4) |
| Update pipelines | `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase<T>`, `ITeamUpdater`, `IPortfolioUpdater`, `IUpdateService` | Epic 6474 slice 12 (D-6) |
| Context seeders | `AppSettingSeeder`, `TerminologySeeder`, `OptionalFeatureSeeder`, `CategoryListNormalisationSeeder` (Organisation set-up, S03i), `RefreshLogSeeder` (Work Items), `ApiKeyOwnerReconciliationSeeder`, `EveryoneSignedInSeeder` (Access, S03e D-5) | D-5 |
| Context files among the loose ones | `AppSettingService`, blackout and recurring-rule services, `WeeklyRecurrence`, `TerminologyService`, Feature ordering and ranking (9), `ConnectionSaveService`, `ChangedConnectionDefault`, `WorkTrackingSystemConnectionUsage` (S03i); `FeatureListService`, `FeatureListActiveWork`, `FeatureListOwners`; `EstimateNormalizer`, `InProgressFeatureResolution` (Flow metrics / Work Items strays); `OrphanedFeatureCleanupService` (Work Items, called by `PortfolioUpdater`); the write-back round (8, #6657); `OptionalFeatures/*` | their contexts; not platform |
| Composition root and host band | `Program.cs`, `Startup/`, `Configuration/`, `Health/`, `Standalone/`, `Cache/`, `Extensions/`, `Factories/` (demo data CSVs and `DemoDataFactory`) | already outside `Services`; composition root (D-1) |
| Platform-related `Models` | `Models/Encryption/*` (6), `Models/UsageData/*` (18), `Models/Logging/*` (2), `SystemInfo`, `LighthouseRelease`, `LighthouseReleaseAsset`, `UpdateType`, `AppSetting*` | D-3 (four conditions; `UsageDataEventName` is used by every context, `UpdateType` was put in `Models` on purpose by Stories 6367-6369) |

### Tests of the set (path count)

About **65 unit-test files** move to `Lighthouse.Backend.Tests/Platform/<Area>`: Encryption 21 + `KeyRingFileWatcherTests` +
`CryptoServiceTests` (23); Database management 6 (`DatabaseMaintenanceGateTest`, `DatabaseManagementCompatibilityTest`,
`DatabaseManagementServiceTest`, `DatabaseOperationTrackerTest`, the SQLite and Postgres provider tests) + `DemoDataServiceTest`
+ `LostRaceTolerantSaveTest` (8); Usage data 9 + the two background-service tests, Logging (`RecentProblemsSinkTest`,
`SerilogLogConfigurationTest`, `ConfigFileUpdaterTest`), Releases (`GithubServiceTest`, `LighthouseReleaseServiceTest`,
`LighthouseReleaseServiceIntegrationTest`) (17); `DomainEventDispatcherTest`, `DomainEventDispatcherGoldTest`,
`PlatformExtensionTest`, `LighthouseClockTest`, `SystemInfoServiceTest` (5); Update queue 11 (`AdmittedCancellationsTest`,
`CancelAllWhileWorkIsAdmittedTest`, `InProcessUpdateAdaptersTest`, `InProcessUpdateCancellationNotifierTest`,
`RedisUpdateStatusScriptFreezeTest`, `RedisUpdateStatusStoreBestEffortMomentsTest`, `UpdateMomentsTest`,
`UpdateNotificationHubTest`, `UpdateProgressOrdinalFreezeTest`, `UpdateQueueServiceTests`, `UpdateStatusStoreTest`) +
`RefreshRoundSummaryLineSkippedForecastTest` (12). `Slice01SkippedEntityLogTest` and
`RoundWriteBackDuringDatabaseMaintenanceTest` go with the subject they test: **decided in A1**.

**Stay**: `PortfolioUpdaterTest`, `TeamUpdaterTest`, `UpdateServiceBaseTest` (update pipelines); the three context gold
tests in `Tests/Services/Implementation/DomainEvents` (`SnapshotRecordedDayInstanceZoneTest`, `TeamDataRefreshedGoldTest`,
`WorkItemDomainEventsGoldTest`; they test contexts' events through a host, not the dispatcher); the six seeder tests
(their seeders stay); the repository tests; `Tests/Services/Implementation/TestData/*.zip` (shared by
`TestDatabaseTemplate`, `TestWebApplicationFactory`, `ServicePortHarness`; named in the test `csproj` lines 126, 129).

Host users among the moving tests (`TestHostUseGuardTest.HostUsersStillToMove`): `DomainEventDispatcherGoldTest`
(line 296) and `LighthouseClockTest` (line 302). Live category: `GithubServiceTest` and
`LighthouseReleaseServiceIntegrationTest` carry `GithubIntegration`. Test-time budget:
`Scripts/test-timings/test-time-budget.json` names four Encryption test classes by full name (lines 49, 69, 74, 84), and
`Scripts/test-timings/tests/test_check_budget_time_budgets.py` names the same four (lines 32-39).

### Inbound (who reaches the set; text search, `noisy`)

| Target | Prod files / test files (`using` or `-w` name) | Reading |
|---|---|---|
| Encryption namespaces | 23 / 49 | API, `Data` (`LighthouseAppContext` takes `ICryptoService`), Tracker integration (`ConnectionSecrets`, `UnreadableSecretException`: R-T6 list) |
| `ICryptoService` | 21 / 95 | connectors' auth strategies, connection DTOs, `Data` |
| `ILighthouseClock` | 59 / 93 | every context and the API; Promises R-P6 lists it |
| `IDomainEventHandler<T>` / `IDomainEventDispatcher` | 25 / 18, 13 / 34 | every context's `Policies`; R-P6 and Work Items' `TheDomainEventHandlerPort` name it |
| Update namespaces (`BackgroundServices.Update`, `Interfaces.Update`) | 44 / 134 | updaters, Forecasting's `ForecastUpdater` (listed: `UpdateCancellationContext`, `UpdateKey`, `UpdateServiceBase<T>`), API, Work Items inbound list (`UpdateNotificationHub`) |
| `DatabaseManagement` | 11 / 18 | API, `UpdateQueueService` |
| `UsageData` | 10 / 16 | API, contexts emitting events |
| `Seeding` (`ISeeder`) | 11 / 68 | the seven context seeders, `Program.cs` |
| `Program.cs` | registrations, hosted services, `MapHub<UpdateNotificationHub>("api/updateNotificationHub")` (line 228), `SetApplicationName("Lighthouse")` (line 1086), `LegacyKeyStoreDirectoryName` (line 406) | composition root |

### Outbound (what the set reaches beyond `Models`; `using` lines)

| From | To | Reading |
|---|---|---|
| `DatabaseMaintenanceGate` | Flow metrics `Application` (`IOverTimeHistoryFillActivity`, an "is the history fill running" query) | wrong way (platform → context); in Flow metrics R4 (D-12) |
| `UpdateNotificationHub` | Work Items `Application` | wrong way; listed |
| `UsageDataInstanceProperties` | Access, Licensing | listed by S03e (R-A4, R-L3); renamed |
| `UsageDataConsentService`, `UsageDataConsentPruningService` | Licensing, repositories | listed |
| `UsageDataGate`, `UsageDataMasterSwitch`, `DemoDataService`, `UpdateTaskNaming` | repositories | platform → persistence lane |
| `RecentProblemsReport` | update code (`Interfaces.Update`) | inside the lane after the move |
| Encryption, database management | `Data`, EF Core | infrastructure, legal for the lane |
| The set → `API` | nothing found | expected empty (C1) |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. Where does composition-root infrastructure live, given "no module"? → D-1.
2. Which files form the platform lane, and which stay? → D-2, D-4, D-5, D-6.
3. `Data` and the migrations (brief hypothesis)? → D-0.
4. `Models` types? → D-3.
5. The dispatcher, its contracts and the handlers? → D-7.
6. How do the existing rules keep seeing what moves, when they name the lane as "Services"? → D-8, D-9, Ratchet design.
7. Which rule does the lane get of its own ("namespace rule, or the composition-root exemption")? → D-8, C1.
8. Tests, fixtures, budgets, host users; DI and configuration; CI selection; behaviour-free proof; mutation. → D-10 to D-14.
9. `DatabaseMaintenanceGate` → Flow metrics (left to S03f by ADR-270)? → D-15.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **S03e is closed** before the first slice starts; every count re-measured in A1.
- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, configuration keys, key-store directory names, Redis
  channel names, the hub route, health endpoints, UI unchanged; existing tests green unmodified apart from moved tests,
  `using`s and rule files.
- [ ] **No migration file and no `Data` file changes**; both migration assemblies untouched.
- [ ] **Rules first**: every rule pattern that matched a moved type's old namespace matches its new one (A2's law),
  before a type moves.
- [ ] **Moves separate from edits**: `refactor(platform): move …` commits apart from edit commits.
- [ ] **Ratchet in the same change**; inline lists keyed by full type name; exemptions per exact type; no rule loosened.
- [ ] **No platform file left in `Services/Interfaces|Implementation`**; the files that stay are named per file with owner.
- [ ] **Suppression targets follow the move**: `Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs` names three
  moving types by full name (S2245 on `RandomNumberService.GetRandomNumber`, B4; S4036 on
  `DatabaseManagement.PostgresDatabaseManagementProvider`, B2; S4036 on `LighthouseReleaseService`, B3). Each `Target`
  is edited in the **same commit** as the move, or `dotnet build` fails under `TreatWarningsAsErrors` (E13).
- [ ] **Strings and config checked**: `GlobalSuppressions.cs` targets, `.editorconfig` entries, `path-classifier.sh` (GitHub), `test-time-budget.json` and its Python test,
  `HostUsersStillToMove`, rule-list strings, path strings in rule files, `csproj`, Stryker globs.
- [ ] **Key ring**: no key-store constant, directory or file name changes (`CLAUDE.md` "Running Lighthouse Locally").
- [ ] **`Program.cs`**: only `using`s change; registration lines and order unchanged; the move pushes run the full CI
  backend suite and every live suite (D-12).
- [ ] **Frontend untouched**: no file under `Lighthouse.Frontend` or `Lighthouse.EndToEndTests` changes.
- [ ] **Architecture docs in the same Story** (finalize): `ARCHITECTURE.md` §4, ADR-270, brief ACs, catalogue.
- [ ] **Cross-cutting re-check** answered (section below).

## Wave: DESIGN / [REF] Context-map placement (2026-11-21)

- **Contexts touched**: none as a context. The Platform lane ("not a context", ADR-270) gets a folder of its own,
  `Platform/`, beside `Modules/` and the host band.
- **Edges that become visible** (no behaviour change): every context → Platform lane (clock, dispatcher contracts, key
  ring, update queue contracts, usage data), already listed by full name in each module's outbound list and renamed;
  Platform lane → Flow metrics (`DatabaseMaintenanceGate`, D-15) and → Work Items (`UpdateNotificationHub`), both wrong
  way and listed; Platform lane → Access and Licensing (usage data), listed; Platform lane → PostHog and GitHub Releases
  (unchanged adapters, now in `Platform/UsageData` and `Platform/Releases`).
- **New terms / concepts**: none. "Platform lane" is ADR-270's word; the folder is `Platform`.
- **Domain events**: none published or newly reacted to. The dispatcher moves; ADR-027 transport unchanged.
- **UI surface**: none (ADR-271). **Improvement backlog check**: S03f is itself a #6510 item; N/A, stated.

## Wave: DESIGN / [REF] Evidence (2026-11-21, `main` at `d2d46f9fc`)

| # | Finding | Consequence |
|---|---|---|
| E1 | The rules name the platform lane **as "Services"**, not by area. Generic patterns: `ApplicationCoreRules.ApplicationCore` `^…\.(Services\|Modules)(\.\|$)` (used by `CoreDoesNotDependOnApi`, `StalenessPolicyPurity`, `RealityCheckReadOnly`, the parent-placement rule), `KernelRules.TheServices` (Models ↛ Services), `KernelRules.TheUpdateQueue`, `PromisesModuleBoundariesArchUnitTest.ThePlatform` `^…\.Services(\.\|$)` and `TheUpdateCode`, `TrackerIntegrationModuleBoundariesArchUnitTest.TheKeyRing` (`Services.*.Encryption`) and `PromisesAndTheUpdateCode`, `WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp` (`Services.(Implementation\|Interfaces)$`, which today also holds the clock and the other loose platform types) and `TheDomainEventHandlerPort`, `ForecastingModuleBoundariesArchUnitTest` shared update code, `ModuleBoundariesArchUnitTest` line 19 (`BackgroundServices\|Interfaces.Update`), `DependencyAwareForecastSeamArchUnitTest` update components, `RefinementModuleArchUnitTest.ServicesImplementationPattern`, `PromisesMembershipArchUnitTest` / `PromisesPoliciesLayoutArchUnitTest` (`(\.DomainEvents)?`), and one inline fragment `.Services.Implementation.DomainEvents` in `ApplicationCoreRules.ByDomainEventsNamespace` (lines 182-191). | Moving a type to `Lighthouse.Backend.Platform.*` would silently drop it from each of them (Models could reach the key ring unseen; Promises could reach the clock off its list unseen). **A2 widens every pattern by the new namespaces of exactly the types it matched**, and proves it with an exhaustive law over the membership map (Ratchet design). The inline fragment becomes the dispatcher's exact type. |
| E2 | 280 files in `Lighthouse.Migrations.Sqlite` and `Lighthouse.Migrations.Postgres` name `Lighthouse.Backend.Data` (`[DbContext(typeof(…LighthouseAppContext))]` and snapshots); the backend takes both as `HintPath` DLLs (`Lighthouse.Backend.csproj` lines 91, 95). | `Data` stays (D-0). The brief's hypothesis is disproved for the `DbContext` alone; every other platform file moves with no migration touched. |
| E3 | `path-classifier.sh` `GITHUB_REGEX` (line 17) selects the GitHub live suite by the path `Services/(Implementation\|Interfaces)/…(GitHubService\|LighthouseReleaseService)…`. | After the move a change to the release code would no longer select the GitHub suite: a CI behaviour change. **A3 widens it, with a real RED** (`Platform/Releases/GitHubService.cs` → `github=false` today). |
| E4 | Path strings in rule files name moved files: `CalendarDayAnchorSeamArchUnitTest` (`Services/Implementation/LighthouseClock.cs`, line 60); the Promises placement test's negative cases name `Services/Implementation/DomainEvents/…` and `BackgroundServices/Update/PortfolioUpdater.cs`; `ForecastingUnitTestPlacementTest` names `Services/Implementation/BackgroundServices/Update/…Test.cs`. | Edited in the B slice that moves the file (a missing file fails loudly). Negative cases keep their meaning (a made-up newcomer in an old folder). |
| E5 | Full-name strings name moved types: Flow metrics R4 (`DatabaseMaintenanceGate`), Forecasting adapters list (`UpdateCancellationContext`, `UpdateKey`), Promises R-P6 (`ILighthouseClock`, `IDomainEventHandler`1`), Tracker integration R-T6 (`ConnectionSecrets`, `UnreadableSecretException`), Work Items inbound (`UpdateNotificationHub`), S03e's Access and Licensing lists (usage data; re-measured at start). | Renamed in the B slice that moves the type; counts unchanged. Where a widened pattern makes an existing edge newly visible to a rule (for example a Tracker integration `Application` type reaching `ICryptoService`, which was a loose `Services.Interfaces` type outside `TheKeyRing`), the entry is added **only** if the edge exists on the pre-move revision (measured from the IL) and with a reason. |
| E6 | Strings that look namespace-bound are not: key-store directories (`KeyStoreResolver` lines 25-27, `Program.cs` line 406) are constants; Redis channels are `RedisChannel.Literal("lighthouse:update-…")`; the hub route is literal; `SetApplicationName("Lighthouse")`; `UpdateServiceBase` uses `typeof(TEntity).Name`; `RecentProblemsSink` shows the **short** source name (line 192). No `GetManifestResource`, `AssemblyQualifiedName` or type-name serialisation in the set. | Unchanged by a move; proven by the unchanged tests and `platform-answers-compare` (A4). |
| E7 | Log categories are full type names. `appsettings*.json` name no platform namespace (the one entry is S03e's). `docs/product/kpi-contracts.yaml` line 2727 names `ForecastUpdater`'s old namespace (already stale since S03c, not this Story's type). | Log lines of moved types read `Lighthouse.Backend.Platform.*`; stated in the record and release notes not needed (ADR-270 lesson 5). |
| E8 | `test-time-budget.json` keys four Encryption test classes by full name; its Python test names the same four. | B1 renames both in its move commit. |
| E9 | No `IDomainEventHandler<>` implementor outside `Modules/` (text search: only `Program.cs` registrations and the contract itself). | The brief AC already holds; A1 pins it from the IL. |
| E10 | No EXAMINE probe reads a platform endpoint (system info, version, encryption, database management, usage-data state, log level and problems, update status). `refresh-reactions-compare` covers dispatcher order; `ci-live-suites-show` includes `GithubIntegration`. | A4 adds `platform-answers-compare` before any file moves. |
| E11 | `HostUsersStillToMove` names two moving tests (lines 296, 302). | B4 renames them in the move commit; the test-writing rule "improve the tests you touch" applies in a separate edit commit of B4 (D-10). |
| E12 | No frontend, E2E, client or website file names a moved type or folder. | Frontend untouched; no pnpm gate. |
| E13 | `Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs` names three moving types by full name: `~M:Lighthouse.Backend.Services.Implementation.RandomNumberService.GetRandomNumber(System.Int32)~System.Int32` (S2245, B4), `~T:Lighthouse.Backend.Services.Implementation.DatabaseManagement.PostgresDatabaseManagementProvider` (S4036, B2), `~T:Lighthouse.Backend.Services.Implementation.LighthouseReleaseService` (S4036, B3). A suppression target does not follow a namespace move. Found by the independent design review, missed by DESIGN's text search. | The move commit of B2, B3 and B4 rewrites the matching `Target` to the new full name; without it the Sonar hotspots come back and `dotnet build` fails (`TreatWarningsAsErrors`). **A1 re-greps** every `GlobalSuppressions.cs`, `.editorconfig` (`Lighthouse.Backend/.editorconfig`, `Lighthouse.Backend/Lighthouse.Backend.Tests/.editorconfig`) and any `.globalconfig` for targets that name a moving type, and records them in its map, so no other target is left. |

## Wave: DESIGN / [REF] Decisions

All taken as recommended (AFK, 2026-11-21). Option tables are the record of what was weighed.

### D-0 No schema change; `Data` stays — RECOMMENDED

No entity, `DbSet`, migration or table changes. `Data/*` stays in `Lighthouse.Backend.Data` (E2): moving it edits 280
migration files and needs both migration DLLs rebuilt, which the brief's hypothesis names as the stop condition. S10
owns it. Checked by `ef-model-unchanged` in EXAMINE and by an empty diff under `Lighthouse.Migrations.*` (reviewer).

### D-1 Where the lane lives — RECOMMENDED: a top-level `Platform/` folder, areas by capability, no layers

| Option | Verdict |
|---|---|
| **a (recommended)** `Lighthouse.Backend/Platform/<Area>`, namespace `Lighthouse.Backend.Platform.<Area>` (namespace = folder) | A reader asking "where is the key ring / the update queue / the dispatcher" finds one folder named after ADR-270's word, beside `Modules/` and the host band. No `Domain/Application/Adapters`: the lane is not a context and has no domain; each port sits next to its implementation in its area (Decision 6's co-location). No existing `Platform` namespace or type collides (`git grep`). |
| b `Modules/Platform` | Rejected: ADR-270 Decision 6, "no folder for Platform"; `ModuleRules` would treat it as a context. |
| c Spread into `Startup/`, `Health/`, `Configuration/` | Rejected: those are the host band (wiring, endpoints); key custody and the update queue are services, not wiring. |
| d `CompositionRoot/` or `Infrastructure/` | Rejected: not ADR-270's word; "composition root" names `Program.cs`'s wiring role. |

Areas: `Encryption`, `DatabaseManagement`, `UsageData`, `Logging`, `DomainEvents`, `UpdateQueue`, `Releases`, `Host`,
`DemoData`, `Seeding`, `Persistence`. The host band (`Startup/`, `Health/`, `Configuration/`, `Standalone/`, `Cache/`,
`Extensions/`, `Factories/`) stays where it is: already outside `Services`, no AC needs it; folding it in is open
question 6.

### D-2 Membership — RECOMMENDED: the 137 files of the platform set move; the rest stays with a named owner

Membership by ADR-270's Platform lane row, file by file (Baseline table). Ambiguous loose files were placed by owner,
not by name: `AppSettingService` is Organisation set-up's (ADR-270 "packaged from today's settings"); `RandomNumberService`
is a host facility like the clock (Forecasting is its only caller today, open to move later); `LostRaceTolerantSave`
is persistence plumbing; `RefreshRoundSummary*` is the update queue's round log. A1 fixes the list from the IL.

### D-3 Context-owned `Models` types — RECOMMENDED: S03a's four conditions

A `Models` type moves only if it is (a) not an EF entity, (b) not on the wire, (c) not reached by a kernel type and
(d) not used outside the lane. Candidates: `Models/Encryption/*`, `Models/Logging/*`. Stay: `UsageDataConsent`,
`AppSetting` (entities), `UsageDataEventName` and the other usage-data event types (used by every context), `SystemInfo`,
`LighthouseRelease*` (wire), `UpdateType` (put in `Models` on purpose). A1 lists the admissions by full name; most likely
none or a few.

### D-4 Repositories — RECOMMENDED: all stay in `Services/*/Repositories`, base types included

| Option | Verdict |
|---|---|
| **a (recommended)** All 46 repository files stay, `IRepository<T>` and `RepositoryBase<T>` included; named per file with owner (S05c Teams, S05d Portfolios and Deliveries, S05e connections, S03e Access, S03i settings and terminology, S03g sizing log, S03h reports, Work Items, usage data) | ADR-270: "each context owns its persistence adapters". `RepositoryRecognition` and every repository rule stay valid unchanged. |
| b Move `IRepository<T>`, `RepositoryBase<T>` to `Platform/Persistence` | Rejected now: `IRepository` is named by 83 production and 241 test files (`noisy`); S05* reshapes the ports by role (Epic #6507) and S05g changes the base. Churn for a type about to change. |
| c Move every repository to `Platform/Persistence` | Rejected: contradicts ADR-270; S05* would move them a second time. |

### D-5 Seeding — RECOMMENDED: the mechanism and the platform's own seeder move; context seeders stay

`ISeeder` and `InstallTimestampSeeder` move to `Platform/Seeding`. The seven context seeders stay in
`Services/Implementation/Seeding` (S03e D-5 kept the two Access seeders there; S09a decides module start-up), each
named with owner. Rejected: move all nine as "start-up housekeeping" (puts Organisation set-up and Access data into the
lane); split them into modules now (Organisation set-up has no module; another context's move).

### D-6 The update code — RECOMMENDED: queue mechanics move; the pipelines stay

ADR-270: "the queue mechanics go with the platform lane (S03f) and Epic 6474 slice 12 replaces the updaters". Move
the 29 mechanics files (Baseline). Stay: `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase<T>` (the refresh pipeline
base, which reaches Tracker integration's connection health and factory, listed in R-T4) and their three interfaces.
Forecasting's adapters list keeps `UpdateServiceBase`1` and renames `UpdateCancellationContext` and `UpdateKey`.

### D-7 Domain events — RECOMMENDED: dispatcher and both contracts to `Platform/DomainEvents`; lists renamed

The handlers already live with their contexts (E9). `IDomainEventHandler<T>` and `IDomainEventDispatcher` stay listed
by full name in each module's outbound list (renamed), as today. Making them legal by namespace for every module is a
rule change, not a move: open question 5, default "not in this Story". The kernel keeps only the event base type
(Decision 7); the handler contract does not move into `Models`.

### D-8 The lane's rules — RECOMMENDED: keep it under every rule it is under today, plus one direction rule

| Option | Verdict |
|---|---|
| **a (recommended)** The lane stays inside `ApplicationCore` and the kernel rules (A2's widening), and gets `PlatformLaneBoundariesArchUnitTest` (C1): layout and membership; the lane reaches no `API` type and no module `Adapters`; it reaches context code still in `Services` (updaters, write-back, Organisation set-up files, repositories) only through a measured list. Its edges into modules stay governed by each module's inbound rules (Flow metrics R4, Work Items inbound), so they are not listed twice. | Same verdicts as today, plus the lane's direction ("below the contexts") written down. |
| b "Composition-root exemption": treat `Platform` like `Program` (legal caller of every module, outside the rules) | Rejected: a loosening of rules that see these types today; ADR-270 puts the lane below the domain, not above it. |
| c No rule of its own | Rejected: the brief IN asks for one; new platform code could reach any context unseen. |

### D-9 Rules first — RECOMMENDED: one preservation law over the membership map

See Ratchet design. Every `const string` regex in the test assembly is enumerated by reflection; for every moved type
the pattern's answer for its new full name must equal its answer for its old one. Patterns only gain
`Lighthouse.Backend.Platform.<Area>` alternatives, which match no type outside `Platform` (checked).

### D-10 Tests move with production — RECOMMENDED; fixtures and context tests stay

Each B slice moves the unit tests of the files it moves, in its own `refactor(platform): move … tests` commit, to
`Lighthouse.Backend.Tests/Platform/<Area>` (namespace = folder). Stay: the update-pipeline tests, the three context gold
tests, the seeder and repository tests, `TestData/*.zip` (shared host fixtures). Renamed in the moving slice:
`HostUsersStillToMove` lines 296 and 302 (B4), `test-time-budget.json` and its Python test (B1). B4 also tries, in a
separate edit commit, to move `DomainEventDispatcherGoldTest` and `LighthouseClockTest` off a host (test-writing skill,
"improve the tests you touch"); if a day is not enough, it records why and the rows stay.

### D-11 DI and configuration — RECOMMENDED: `using`s only

`Program.cs` lines and order stay; only `using`s change. No `AddPlatform()` extension (S09a). No configuration key,
key-store name, Redis channel, hub route or application name changes (E6). Log categories change with the namespace (E7).

### D-12 CI selection and live suites — RECOMMENDED: widen the GitHub selection first; push B together

Every B slice edits `Program.cs` `using`s, which `SHARED_REGEX` selects, so each B push runs every live suite.

| Option | Live runs | Verdict |
|---|---|---|
| a push every B slice | 5 | highest 429 risk on the shared Linear key |
| **b (recommended)** commit per slice; push B1-B5 together, only with all five green locally (fast loop and full filtered gate) | **1** | S03e choice; A and C touch no production file or `Program.cs` |
| c push after B2 and after B5 | 2 | fallback if B runs over several days (open question 4) |

A3 lands first so the GitHub suite is selected on the B3 push (where `GitHubService` moves).

### D-13 Behaviour-free proof and EXAMINE — RECOMMENDED: every expectation has an admitted probe

| EXAMINE expectation | Probe |
|---|---|
| X1 Same types in new namespaces; all rules green; the platform lane rules green | `architecture-rules Platform`, `architecture-rules` (all) |
| X2 No schema change | `ef-model-unchanged` |
| X3 Same answers from system info, version (current, update supported, distribution; not the GitHub calls), encryption and secrets state, database-management status, usage-data state, log level and problems, update status | **new `platform-answers-compare`** (A4) |
| X4 Domain-event reactions fire in the same order | `refresh-reactions-compare` |
| X5 CI selects backend for `Platform/` paths, all live suites for `Program.cs`, the GitHub suite for `Platform/Releases` | `test-selection-show` |
| X6 Every live suite, GitHub included, passes on the move push | `ci-live-suites-show` |
| X7 Full filtered backend suite green | `backend-tests` |

`platform-answers-compare` (CREATE_NEW on the shared capture pieces of `build-examine-commands`): starts each revision
in-process with authentication off, reads the listed endpoints, drops fields named as volatile (uptime, memory, clock),
compares the bodies, and fails when nothing was compared. Which endpoints answer without network and which fields are
volatile: **measured in A4**. Not observable by a probe: Redis and Postgres update locks under a real cluster (covered
by the container tests, unchanged); a key ring on a mounted file (unit tests). Declared in `.nwave/examine.json` before
the first EXAMINE.

### D-14 Mutation bar — RECOMMENDED

Last, on frozen code, per area, same filter on old and new paths, acceptance suite excluded: **"no survivor or
no-coverage mutant added in the moved code"** (maintainer, 2026-11-14). The 80 % project bar is reported per area; an
area below it before the move is existing debt, named as an improvement item, not fixed in a move (ADR-270 lesson 9).
Run under the x64 SDK script on the laptop (Stryker forces X64). 137 files: if a run is long, compare survivors by file.

### D-15 `DatabaseMaintenanceGate` → Flow metrics — RECOMMENDED: stays listed, renamed

The gate asks Flow metrics' `IOverTimeHistoryFillActivity` whether the chart history fill runs, a platform → context
query, the wrong way. It stays in Flow metrics' R4 list under its new name. The fix is an inversion (a platform port
"activity that blocks maintenance" which Flow metrics implements): proposed as an improvement item, not filed (open
question 7).

## Wave: DESIGN / [REF] Slices

Every slice ≤ 1 day, its own oracle file, moves apart from edits. Three nwdes features, 11 slices. Size **L, about
10-11 days** (A 3.5, B 5, C 1.5, finalize and mutation 1-1.5).

| Id | nwdes feature | Delivers, in plain words | Oracle (new file; how it is RED first) | Production targets | Size |
|---|---|---|---|---|---|
| A1 | A `story-6364-s03f-a-rules-first` | The platform lane's membership is measured from the IL and fixed: each type, its area, its old and new namespace; the files that stay, each with owner; no domain-event handler outside `Modules` | `Architecture/PlatformLaneMembershipArchUnitTest.cs` + `PlatformLaneMembership.cs` (namespace-agnostic: a listed type is found in its old or new namespace, as S03a's membership test). RED: fails when a listed type is missing or a name does not exist. Also records every suppression or analyser-config target that names a moving type (E13; re-grep of `GlobalSuppressions.cs`, `.editorconfig`, `.globalconfig` in both projects) | none | 1 d |
| A2 | A | Every rule that sees a moved type today still sees it after the move | `Architecture/PlatformLaneRulesSeeTheMoveArchUnitTest.cs`: reflects every `const string` regex in the test assembly and checks, over A1's map, `IsMatch(P, old(T)) == IsMatch(P, new(T))` for every moved type T, and that no added alternative matches a production type outside `Platform`. RED: about 20 patterns fail today (E1). A2 also replaces the inline `.Services.Implementation.DomainEvents` fragment with the dispatcher's exact type. Second assertion in the same file: no string literal of the form `Services.Implementation.<area>` / `Services.Interfaces.<area>` / `Services/Implementation/<area>` naming a moving area (`Encryption`, `DatabaseManagement`, `UsageData`, `Logging`, `DomainEvents`, `BackgroundServices.Update`, `Interfaces.Update`) or a moving loose type is left in the rule files, except entries in A1's list of strings a B slice renames; RED today on `ApplicationCoreRules.cs:182` and the path literals | none (test-side patterns) | 1.5 d |
| A3 | A | A change to the release code under `Platform/Releases` still selects the GitHub live suite | `Scripts/test-selection/test-path-classifier-platform.sh` (RED: `Platform/Releases/GitHubService.cs` and its test path give `github=false` today) | `Scripts/test-selection/path-classifier.sh` | 0.5 d |
| A4 | A | An answers probe for the platform's endpoints exists before any file moves and reports a changed answer | `Scripts/examine/test-platform-answers-compare.sh` (**behavioural RED**: two capture folders differing in one answer → `DIFFERENT` exit 1; identical → `IDENTICAL` exit 0; the command does not exist today) | `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | 1 d |
| B1 | B `story-6364-s03f-b-move` | The key ring and secret custody live in `Platform/Encryption` | `Architecture/PlatformEncryptionLayoutArchUnitTest.cs` (RED: types in old namespaces) | 38 files, 23 test files, `Program.cs` usings, R-T6 strings, `test-time-budget.json` + Python test | 1 d |
| B2 | B | Database management, demo data, the platform's seeder port and the race-tolerant save live in `Platform/` | `Architecture/PlatformDatabaseAndSeedingLayoutArchUnitTest.cs` (RED as B1) | 21 files, 8 test files, Flow metrics R4 string, the seven context seeders' `using`s, `GlobalSuppressions.cs` target of `PostgresDatabaseManagementProvider` (same commit) | 1 d |
| B3 | B | Usage data, logging and releases live in `Platform/` | `Architecture/PlatformUsageLoggingReleasesLayoutArchUnitTest.cs` (RED as B1) | 31 files, 17 test files, S03e list strings, `GlobalSuppressions.cs` target of `LighthouseReleaseService` (same commit) | 1 d |
| B4 | B | The dispatcher and its contracts, the clock and the host facilities live in `Platform/` | `Architecture/PlatformDomainEventsAndHostLayoutArchUnitTest.cs` (RED as B1) | 18 files, 5 test files; `using`s in about 150 clock and 80 dispatcher users (`noisy`); R-P6 and Work Items strings; `CalendarDayAnchorSeam` path; `HostUsersStillToMove`; `GlobalSuppressions.cs` target of `RandomNumberService.GetRandomNumber` (same commit) | 1 d |
| B5 | B | The update queue's mechanics live in `Platform/UpdateQueue`; the pipelines stay | `Architecture/PlatformUpdateQueueLayoutArchUnitTest.cs` (RED as B1; also pins that `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase<T>` did not move) | 29 files, 12 test files, Forecasting and Work Items strings, placement-test paths | 1 d |
| C1 | C `story-6364-s03f-c-edges-and-rules` | The platform lane has its own rules, measured from the IL | `Architecture/PlatformLaneBoundariesArchUnitTest.cs` (R-PL1 to R-PL4; RED: a made-up offender per rule and list position, falsified as `ModuleRulesCatchANewOffender`) | rule files | 1 d |
| C2 | C | Every platform unit test sits under `Tests/Platform/…`; no platform file left in `Services/{Interfaces,Implementation}`; the files that stay are named per file with owner | `Architecture/PlatformLaneUnitTestPlacementTest.cs` (RED: a made-up file in an old folder is caught) | rule files | 0.5 d |

A1's, B's and C2's REDs are structural (the right RED for a layout move); behaviour is proved by the unchanged tests and
EXAMINE. A2's RED is the preservation law failing; A3 and A4 have behavioural REDs.

Order: A1 before A2 (the law runs over A1's map); A3 before B3; B1-B5 in any order, sequential (each edits `Program.cs`).

Verification commands per slice (fast loop, then the full gate at the end of the slice):

```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~<OracleClass>"
bash Scripts/test-selection/test-path-classifier-platform.sh               # A3
bash Scripts/test-selection/test-path-classifier.sh                        # A3, unchanged cases
bash Scripts/examine/test-platform-answers-compare.sh                      # A4
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

Why three features: rules and probes only (A, 4); pure moves (B, 5); the lane's rules and placement (C, 2). Finalize
(not nwdes slices): mutation (D-14); ADR-270, `ARCHITECTURE.md` §4, brief ACs, catalogue; the Lighthouse finalize checklist.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list inline, by full type name, compared both ways, never loosened.

### Rules first (A2): the preservation law

Let `f` map each moved type's old full name to its new one (A1). For every regex constant `P` in the test assembly
(enumerated by reflection, so a pattern added later is covered) and every moved type `T`:
`IsMatch(P, T) == IsMatch(P', f(T))`, where `P'` is the widened pattern. Added alternatives are of the form
`^Lighthouse\.Backend\.Platform\.<Area>(\.|$)` and match no production type outside `Platform`, so no unmoved type's
answer changes. The domain is finite (137 types × the constants), so the check is exhaustive, not generated. Patterns
anchored on one type name are unaffected. Inline non-constant fragments are not constants, so reflection misses them; A2's second assertion fails on any literal left that names a moving area (known today: `ApplicationCoreRules.cs:182`; `Services/...` path literals in `CalendarDayAnchorSeamArchUnitTest`, `CategoryListOwnershipGuardTest`, `OverTimeReconstructionSeamArchUnitTest`, `DependencySingleDecisionArchUnitTest`, `FlowMetricsUnitTestPlacementTest`, `ForecastingUnitTestPlacementTest`, `PromisesUnitTestPlacementTest`, `RepositoryLeakRatchetArchUnitTest`, `JiraRouteLiteralsTest`; only those naming a moving area or type count). Also found by A2's text re-grep at its start
(known: one, E1).

| File | Keys on | Widening (by the areas of the types it matched) |
|---|---|---|
| `ApplicationCoreRules` | `ApplicationCore` | `Services\|Modules\|Platform` |
| `ApplicationCoreRules` | `.Services.Implementation.DomainEvents` fragment | the dispatcher's exact type |
| `KernelRules` | `TheServices`, `TheUpdateQueue` | `+ Platform`, `+ Platform.UpdateQueue` |
| `PromisesModuleBoundariesArchUnitTest` | `ThePlatform`, `TheUpdateCode` | `+ Platform`, `+ Platform.UpdateQueue` |
| `TrackerIntegrationModuleBoundariesArchUnitTest` | `TheKeyRing`, `PromisesAndTheUpdateCode` | `+ Platform.Encryption`, `+ Platform.UpdateQueue` |
| `WorkItemsModuleBoundariesArchUnitTest` | `OrganisationSetUp` (loose `Services`) | `+` the areas of the loose types that move (`Host`, `Encryption`, `Logging`, `Releases`, `DemoData`, `Persistence`, `UpdateQueue`); renamed in C1 if the name misleads |
| `ForecastingModuleBoundariesArchUnitTest`, `DependencyAwareForecastSeamArchUnitTest`, `ModuleBoundariesArchUnitTest` line 19, `RefinementModuleArchUnitTest` | update code, `Services.Implementation` | `+ Platform.UpdateQueue`, `+ Platform` |
| `PromisesMembershipArchUnitTest`, `PromisesPoliciesLayoutArchUnitTest` | `(\.DomainEvents)?` | `+ Platform.DomainEvents` |
| every other constant the law finds | | as the law requires |
| list strings, path strings | full names, paths | renamed in the B slice that moves the type (E4, E5) |

### New rules (C1), baselines measured from the IL

| # | Rule | Expected baseline (text search; the IL decides) |
|---|---|---|
| R-PL1 | Each listed type lives in its `Platform/<Area>`; none left under `Services` | A1's map |
| R-PL2 | The lane reaches no `API` type | empty |
| R-PL3 | The lane reaches no module `Adapters` | empty |
| R-PL4 | The lane reaches code still in `Services` (update pipelines, write-back, Organisation set-up files, repositories, Reports, Refinement) only through the list | repositories from `UsageDataGate`, `UsageDataMasterSwitch`, `UsageDataConsentService`, `UsageDataConsentPruningService`, `DemoDataService`, `UpdateTaskNaming`; `UpdateQueueService` → update pipelines and `IAppSettingService`; others **measured in C1** |

The lane's edges into modules (`DatabaseMaintenanceGate` → Flow metrics, `UpdateNotificationHub` → Work Items, usage
data → Access and Licensing) stay governed by those modules' inbound rules, renamed, not listed twice. `Models ↛
Platform` comes from the widened kernel rule (A2), not a new rule.

## Wave: DESIGN / [REF] Component decomposition

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| Key ring, secret custody, `CryptoService`, key-ring file watcher | `Services/*/Encryption`, loose, `BackgroundServices` | `Platform/Encryption` | MOVE | B1 |
| Database management (16), demo data (2), `ISeeder` + `InstallTimestampSeeder`, `LostRaceTolerantSave` | `Services/*` | `Platform/{DatabaseManagement, DemoData, Seeding, Persistence}` | MOVE | B2 |
| Usage data (17), logging (8), releases (6) | `Services/*` | `Platform/{UsageData, Logging, Releases}` | MOVE | B3 |
| Dispatcher + 2 contracts, host facilities (15) | `Services/*` | `Platform/{DomainEvents, Host}` | MOVE | B4 |
| Update queue mechanics (29) | `Services/*/BackgroundServices/Update`, `Interfaces/Update`, loose | `Platform/UpdateQueue` | MOVE | B5 |
| Rule files, scripts | tests, `Scripts` | same | EDIT / CREATE_NEW | A, C |
| `Data`, repositories, update pipelines, context seeders and loose context files, host band | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: `SystemInfoController`, `VersionController`, `EncryptionController`, `DatabaseManagementController`,
`UsageDataController`, `LogsController`, `UpdateController`, `DemoController`, the SignalR hub at
`api/updateNotificationHub`. Hosted services: key-ring file watcher, usage-data forwarding and consent pruning, the
update queue. In-process: every context calls the clock, the dispatcher and the update queue contracts.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IDatabaseManagementProvider` | SQLite and Postgres providers | moved (B2) |
| `IUpdateExecutionLock`, `IUpdateStatusStore`, `IUpdateCancellationNotifier`, `IUpdateCompletionNotifier` | in-process, Redis, Postgres | moved (B5) |
| `IUsageDataPublisher` | PostHog | moved (B3) |
| `IGitHubService` | GitHub Releases | moved (B3) |
| `IFileSystemService`, `IProcessService`, `IPlatformService` | OS | moved (B4) |
| `LighthouseAppContext` | EF Core | stays (D-0) |

No new external call, adapter or package.

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET, `IlDependencies` over Mono.Cecil, NUnit, Stryker.NET, bash, `gh`, the existing EXAMINE capture pieces.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `…RulesSeeTheModuleArchUnitTest` approach (Forecasting, Tracker integration, Promises) | proving rules see a move | **EXTEND** the idea: A2 checks every pattern at once by a law over the map instead of one made-up type per pattern |
| `TrackerIntegrationMembership` / `PromisesMembership` + their tests | membership by name, namespace-agnostic | **REUSE** shape for A1 |
| `ModuleRules`, `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | outbound edges with a list, falsifiers | **REUSE** `OutboundEdges` and the falsifier for C1; the lane is not a module, so no `ModuleWording` module name, a lane wording instead |
| `PromisesUnitTestPlacementTest` | placement with named exceptions | **REUSE** shape for C2 |
| `test-path-classifier.sh` | classifier cases | **REUSE** harness; new file for A3 (one oracle per slice) |
| `deliveries-answers-compare` capture and compare pieces | in-process host, compare stage, "nothing compared" | **REUSE**; `platform-answers-compare` **CREATE_NEW** thin command |
| `ef-model-unchanged`, `architecture-rules`, `refresh-reactions-compare`, `backend-tests`, `test-selection-show`, `ci-live-suites-show` | behaviour-free proof | **REUSE** |
| CREATE_NEW | `Platform/` tree, 11 oracle files, 1 probe, 1 classifier test file | no existing owner of the lane's layout or rules |

Semantic duplicates checked: `CryptoService` and the key-ring classes do not decide the same thing (`CryptoService`
encrypts with the active key; custody decides which key is active); `RecentProblemsReport` and the log configuration do
not overlap. No second owner of a platform fact found.

## Wave: DESIGN / [REF] Impact closure

| Locator | Kind | Decision | Reason |
|---|---|---|---|
| 137 production files | source | CHANGED (moved) | D-1, D-2 |
| `Program.cs` | composition root | CHANGED (`using`s only) | D-11; forces full CI suite |
| API controllers, `Data`, every context using the clock, dispatcher, key ring, update contracts | consumers | CHANGED (`using`s) | |
| about 65 unit-test files; tests `using` the old namespaces (hundreds, `noisy`) | tests | MIGRATED / CHANGED | D-10 |
| Rule files of E1, E4, E5 | tests | CHANGED | A2, B, C |
| `path-classifier.sh` | CI | CHANGED | A3 (E3) |
| `Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs` (3 targets) | build / analyser | CHANGED (same commit as the move: B2, B3, B4) | E13 |
| `.editorconfig` (both projects), any `.globalconfig` | build / analyser | UNCHANGED unless A1's re-grep finds a target naming a moving type | E13 |
| `test-time-budget.json`, `test_check_budget_time_budgets.py` | CI timings | CHANGED (keys) | E8 |
| `HostUsersStillToMove` | tests | CHANGED (2 names) | E11 |
| `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | scripts | CHANGED | A4 |
| `Data/*`, `Lighthouse.Migrations.*`, EF snapshots | persistence | UNCHANGED | D-0 |
| Repositories, update pipelines, context seeders, `TestData/*.zip`, test `csproj` | | UNCHANGED (`using`s only where a moved type is used) | D-4 to D-6, D-10 |
| `appsettings*.json`, key-store names, Redis channels, hub route | config / wire | UNCHANGED | E6, E7 |
| Frontend, E2E, Lighthouse-Clients, website, Docker, workflows | | UNCHANGED | E12 |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, configuration keys, key-store names, Redis channels, hub route, UI unchanged; no schema change; log categories of moved types change. |
| RBAC | Not touched. `IRbacAdministrationService` unchanged; usage data's edges to Access are renamed only. |
| `useRbac()` / frontend | Not touched; no pnpm gate. |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing a user does changes (see DEVOPS). The usage-data pipe moves but sends the same events. |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. |
| Key ring (`CLAUDE.md`) | No key store, directory or legacy name changes; `KeyStoreResolver` and `Program.cs` constants move with their types or stay. |
| `Program.cs` | `using`s only in B1-B5: one full CI and live-suite run (D-12). |

## Wave: DESIGN / [REF] Residuality

Stressors from negated assumptions, walked against the finished design (walkthrough, not execution).

| Assumption negated | What happens | Preserved / forbidden | Residue, owner |
|---|---|---|---|
| Rules name the lane by area | They name it as "Services" (E1); a move drops types from about 20 patterns | A2's law before any move; enumerator over all constants | a pattern built at run time from pieces, or an inline fragment: A2 re-greps; both-way list comparisons fail loudly on rename |
| CI selects suites by content | GitHub is selected by path (E3) | A3 widens with a real RED | another future path-keyed selector: `test-selection-show` in EXAMINE |
| The `DbContext` can move like the rest | 280 migration files name it (E2) | `Data` stays | S10 |
| Configuration never names a type | Log levels can (E7); none name a platform type in the repo | development log unchanged | operators' own overrides naming an old platform namespace lose effect; release notes not needed (none documented) |
| Persisted keys or channels derive from type names | They are literals or short names (E6) | same Redis and key-store behaviour | none known |
| Test budgets are keyed by file | They are keyed by class full name (E8) | B1 renames | a budget entry missed: the budget check fails loudly |
| S03e is not closed when S03f starts | Shared rule files in flux | Nothing starts | start condition |
| Every B push is SHARED | All live suites run | No suite skipped | 429 risk on the shared Linear key (D-12) |
| The examine host reaches the network | Version "has update" and "new" call GitHub | X3 excludes them | covered by `GithubIntegration` on the push (X6) |
| A parallel Story (S03i, #6657, Epic 6474 slice 12) touches the same loose files | Merge pain on `using`s | its files stay in `Services` here | the later Story rebases; A1's map is re-measured at start |

Second set, not used to derive the design: a worktree without the premium licence fixture (Licensing failures,
environmental); the Standalone build (namespaces do not change the assembly); a cluster deployment with Redis (channel
literals unchanged; container tests unchanged).

## Wave: DESIGN / [REF] Formal verification

The formal-verification skill was read. Tools probed on the laptop: `quint --version` → `0.33.1` (exit 0),
`java -version` → OpenJDK 21.0.13.1, `QUINT_HOME` unset. **No checker was run, and none is selected**, because no
state, ordering, concurrency, recovery or liveness claim changes: the update queue, the locks and the dispatcher move
unchanged. The Story's claims are (1) the preservation law of A2 over a finite domain, decided **exhaustively** by an
executable check in the default suite; (2) "the edges are exactly the listed ones", decided by the IL with a made-up
offender per rule; (3) "answers unchanged", decided by the unchanged suites and `platform-answers-compare`. A Quint
model would restate the namespace map and could not fail. If the maintainer wants a model, the claim stays
**INDETERMINATE** until one is written and run.

## Wave: DESIGN / [REF] Test substrate

- C# .NET 10, NUnit 4.6, Mono.Cecil via `IlDependencies`; bash for the classifier and EXAMINE probe tests. No new package.
- Oracles are architecture tests (`Lighthouse.Backend.Tests/Architecture/*`), one classifier test file and one probe
  test; the claims are about types, edges, selection and observability. Behaviour stays covered by the existing tests,
  which move or stay but do not change.
- Timing: each architecture test reads the cached IL module; A2's law is about 137 types × a few hundred constants of
  regex matching with the existing timeout, well inside the default per-test budget. No derogation requested.

## Wave: DESIGN / [REF] SSOT edits pending

Not made in this wave (docs only). Written in the finalize checklist after feature C.

- **ADR-270**: Platform lane row "**Placed** in `Platform/<Area>` by Story 6364 (date); `Data`, the repositories, the
  update pipelines and the context seeders stay"; Decision 6 layout note "`Platform/` is the lane's folder, not a module";
  "Left open: the update queue" answered (mechanics in `Platform/UpdateQueue`); "Every edge" Flow metrics → Platform
  lane answered (D-15, listed); new block "Edges measured by placing the platform lane" (R-PL1 to R-PL4).
- **ARCHITECTURE.md** §4: row 7 becomes "Platform lane (composition-root infrastructure)" with `Platform.*` anchors;
  the host band note names `Platform/`; the diagram node renamed.
- **brief.md**: new section `## Application Architecture — story-6364 s03f platform composition root`.
- **Epic catalogue**: Improvement log; KPI 1 and 3; slice-08 brief title and goal (stale "Platform/Persistence"
  wording), IN list and ACs corrected (DoR 5); notes for S05*, S10, S03i, Epic 6474 slice 12.
- **`CLAUDE.md`**: no edit needed (its key-ring notes name directories, not namespaces).

## Wave: DESIGN / [REF] Architecture summary

**Shape in hexagonal terms.** Everything under `Platform/` is MOVED; nothing new, no new port, no new external call.

```
 driving (unchanged)                 Modules/* (contexts, unchanged)          Platform/ (NEW folder, moved code)
 ─────────────────                   ───────────────────────────────          ──────────────────────────────────
 Controllers: SystemInfo, Version, ─────────────────────────────────────────► Host, Releases ──► GitHub Releases
  Encryption, DatabaseManagement,                                             Encryption (key ring, custody)
  UsageData, Logs, Update, Demo                                               DatabaseManagement ──► SQLite/Postgres
 SignalR hub, hosted services ──────────────────────────────────────────────► UpdateQueue ──► Redis / Postgres locks
                                     Policies ──IDomainEventHandler──────────► DomainEvents (dispatcher)
                                     Application ──clock, queue, key ring────► (listed edges, renamed)
                                     Flow metrics ◄──listed, wrong way──────── DatabaseMaintenanceGate
                                                                              UsageData ──► PostHog
                                                                              Logging, DemoData, Seeding, Persistence
 Program.cs (composition root, usings only)
 Data/LighthouseAppContext (stays: 280 migration files) · Services/*/Repositories (stay: S05*) · updaters (stay: 6474)
```

- Driving adapters: platform controllers and the update hub (unchanged in place; the hub moves with the queue).
- Application services and ports: none new; the lane is infrastructure below the contexts, its ports next to their
  implementations.
- Domain: none (the lane has no domain).
- Driven adapters: database providers, Redis and Postgres locks, PostHog publisher, GitHub client, OS facades (moved).

**Patterns weighed.**

| Pattern | Verdict | Why |
|---|---|---|
| Platform lane as composition-root infrastructure, not a module (ADR-270) | chosen | no user tells Lighthouse's story through it; it sits below the contexts |
| One `Platform/` folder with areas by capability | chosen | one place to find the lane; no fake `Domain/Application/Adapters` layers |
| Ports co-located with their implementation | chosen | ADR-270 Decision 6 |
| Preservation law over the namespace map (rules first) | chosen | one exhaustive check replaces one made-up type per pattern; catches patterns nobody listed |
| Composition-root exemption for the lane | rejected | a loosening of rules that see these types today |
| Move `Data` / `LighthouseAppContext` | rejected | 280 migration files; the brief's stop rule; S10 |
| Move the repositories or their base types | rejected now | ADR-270 gives repositories to contexts; S05* reshapes the ports |
| Dispatcher contracts into the kernel, or legal by namespace | rejected now | Decision 7 keeps the kernel to the event base type; legalising is a rule change (open question 5) |
| Invert `DatabaseMaintenanceGate` → Flow metrics through a platform port | rejected now | an edit in a move Story; improvement item |
| Fold the host band (`Startup/`, `Health/`, …) into `Platform/` | rejected now | already outside `Services`; no AC needs it (open question 6) |

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-21)

- Platform lens: N/A for deployment, because nothing deploys, configures or runs differently: same binary, same
  configuration keys, same key-store directories, same Redis channels, no Docker, workflow or environment change. Two CI
  concerns were measured: the GitHub live-suite selection by path (E3; A3 widens it with a real RED) and the test-time
  budget keys (E8; renamed in B1). Log categories of moved types change (E7).
- Usage-data event: **N/A, because nothing a user sees or does changes.** The usage-data pipe itself moves to
  `Platform/UsageData` and sends the same events; no `UsageDataEventName` is added.

## Wave: DESIGN / [REF] Open questions for the maintainer

None blocks DELIVER. Confirmations, each with the AFK default taken:

1. **Start order**: S03f starts only after S03e is closed (shared rule files). Default: yes.
2. **Folder name and shape**: `Platform/<Area>` with 11 areas, no layers (D-1). Default: yes.
3. **`Data` stays** (280 migration files; S10) and **all repositories stay**, base types included (D-0, D-4). Default: yes.
4. **Push cadence (D-12)**: push B1-B5 together (1 full live run, recommended) or after B2 and B5 (2)? Default: together.
5. **Dispatcher contracts and the clock**: keep them listed by full name in each module's list (renamed), or make them
   legal by namespace for every module? Default: keep listed; legalising is its own small Story.
6. **Host band**: leave `Startup/`, `Health/`, `Configuration/`, `Standalone/`, `Cache/`, `Extensions/`, `Factories/`
   where they are? Default: leave them.
7. **`DatabaseMaintenanceGate` → Flow metrics**: stays listed; the inversion is proposed as an improvement item, **no
   ADO item filed**. Default: catalogue note only.
8. **Rigor profile**: feature A `thorough` (opus; reviewers and EXAMINE sonnet: the rule widening is where a silent
   loosening would hide); feature B `thorough` for **B1** (key ring and secret custody) and **B5** (update queue locks,
   Redis, Postgres), `standard` for B2, B3, B4 (sonnet; reviewers and EXAMINE haiku); feature C `thorough`. Default
   taken as stated.
9. **Size**: L, about 10-11 days, 11 slices in 3 features (A 4, B 5, C 2). Default: agreed; no split of the ADO Story.
10. **Mutation bar (D-14)**: "no survivor or no-coverage mutant added", 80 % reported per area, debt named not fixed.
    Default: yes.
11. **Test data**: `Tests/Services/Implementation/TestData/*.zip` stays (shared host fixtures), unlike S03e's licence
    files. Default: stays.
12. **Review window**: none (behaviour-free). UI sketch, loading and failure looks: N/A.

Not measured at DESIGN (each named with the slice that measures it): every count, after S03e lands (A1); the IL
membership, the update-code split, `Models` admissions and the subject of the two uncertain update tests (A1); the full
list of patterns the law finds (A2); the endpoints that answer auth-off without network and their volatile fields (A4);
the exact `using` sites (B1-B5); the lane's lists R-PL4 (C1); the mutation run time (finalize). Nothing was built or run.
