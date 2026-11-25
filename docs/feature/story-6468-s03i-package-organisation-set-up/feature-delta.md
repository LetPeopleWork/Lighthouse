# Feature: story-6468-s03i-package-organisation-set-up
Story #6468 · S03i Package Organisation set-up · Epic #6510 (Modules: package the bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (#6468 row line 792, D-6264-16, the S03a and S03d "done" notes naming S03i at lines 199-200, lines 929/936/941).
- Brief: [`slices/slice-45-s03i-package-organisation-set-up.md`](../epic-6264-modular-monolith/slices/slice-45-s03i-package-organisation-set-up.md)
  (Size M-L; the 2026-11-16 update: S03i runs **before** Epic 6474, creates `Modules/OrganisationSetup/`, "move the
  code as it is").
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  (Organisation set-up row line 62, Tracker integration row line 63 "the connection save and the connection usage stay
  for Organisation set-up (S03i)", the acknowledged cycle lines 162-167, edge rows 182-209, Work Items list line 289,
  glossary lines 563-579, Left open lines 845-851 "Feature ranking ... placed by S03i"); `ARCHITECTURE.md` §4 row 4.
- Condition from Epic 6474 ([feature-delta](../epic-6474-one-structure/feature-delta.md) lines 419, 826-833): units,
  levels, `UnitCreated` / `UnitDeleted` and the Team/Portfolio collapse are 6474's (its slice 03); this Story builds none
  of them and fixes no shape 6474 reverses.
- Precedents copied, same shape: [S03h](../story-6366-s03h-package-reports/feature-delta.md) (closest; its folded review
  notes) and [S03g](../story-6365-s03g-package-refinement/feature-delta.md). S03f D-2/D-4/D-5
  ([feature-delta](../story-6364-s03f-platform-composition-root/feature-delta.md) lines 115-123, 270-301) already
  assigned this Story's file set.

DESIGN and DEVOPS run 2026-11-21, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. **Nothing a
user sees or is told changes; no decision is held.** Nothing was built or run with `dotnet` or `pnpm`; every count is a
**text measurement on `main` at `b9fb0ae41`** (`git ls-files`, `git grep`), confidence `noisy` (misses fully qualified
and extension-method references, matches comments). The compiled IL measurement in slice S1 decides.

**Start condition.** S03e (#6363), S03f (#6364), S03g (#6365) and S03h (#6366) are delivered and merged into `main`
before S1 starts. At `b9fb0ae41` all four are designed only: `Modules/` holds `FlowMetrics`, `Forecasting`,
`Promises`, `TrackerIntegration`, `WorkItems`; `Services/{Implementation,Interfaces}/{Refinement,Reports,Auth,
Authorization,Licensing}` and `Services/Interfaces/ILighthouseClock.cs` still exist. All four edit files this Story
edits: `ModuleBoundariesArchUnitTest` (module map), `TrackerIntegrationModuleBoundariesArchUnitTest`,
`RefinementModuleArchUnitTest` (its `ServicesImplementationPattern` and `WeeklyRecurrenceAndItsNestedTypesPattern`
name this Story's `WeeklyRecurrence`), the shared preservation law and literal sweep, and `Program.cs`; S03f moves the
clock and `ISeeder` out of the loose namespaces this Story also empties, and writes R-PL4 ("the lane reaches
Organisation set-up files still in `Services` only through the list"). **Every count below is re-measured at S1**; a
difference is recorded, not argued with.

**What "merged" means, checked by S1 for each of #6363, #6364, #6365, #6366; all three checks must hold:**
1. ADO: the Story's state is `Closed` (`az boards work-item show --id <id> --query 'fields."System.State"'`).
2. Git: `git log main -1 --format="%h %s" --grep="story <id>" -- Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture`
   returns that Story's final rules commit (its last slice, "rules by namespace only"); S1 records the hash and runs
   `git merge-base --is-ancestor <hash> HEAD` (exit 0) against the revision S1 starts from. No hit is "not merged".
3. Content (the property, not the message), on `main`: S03h — `git ls-files` shows nothing under
   `Services/*/Reports/` and `ReportsPattern` names only `Modules\.Reports`; S03g — the same for `Refinement` /
   `RefinementPattern`; S03e — nothing under `Services/*/(Auth|Authorization|Licensing)/` other than the two Access
   seeders S03e D-5 keeps, and no such alternative in `RefinementReportsAccessAndLicensing` (or its successor); S03f —
   no `Services/Interfaces/ILighthouseClock.cs`, no `Services/Interfaces/Seeding/ISeeder.cs`, and a `Platform/` folder.
4. **And not yet Epic 6474 slice 03**: `git grep -l -E "\b(UnitCreated|UnitDeleted)\b" -- Lighthouse.Backend` is empty
   and `TeamController.cs` still publishes `TeamDeleted`. If 6474 slice 03 has landed, S1 stops: this design assumed
   Team and Portfolio as they are today (brief update, 2026-11-16) and returns to DESIGN.
When checks 1-2 find nothing (no `Closed` state yet, or the final rules commit carries another message), **check 3, the
content grep, decides**: the property holds or it does not, whatever the ADO state or commit message says; a check 1-2
miss with check 3 holding is recorded, not a stop. A failed check 3 (or 4) stops S1 and is reported with the Story, the
check and its output; S1 never starts on a partial merge.

## Wave: DESIGN / [REF] Inherited
- ADR-270: Organisation set-up is a **supporting** context, **upstream of nearly all** (open host: "everyone conforms to
  its structure"); "structure and defaults only, no behaviour"; packaged from today's settings, Teams and Portfolios;
  folder `Modules/OrganisationSetup` (settled spelling, line 684); namespace = folder
  (`Lighthouse.Backend.Modules.<Context>.<Layer>[.<Sub>]`); `Domain`, `Application` (ports co-located),
  `Application/Policies`, `Adapters/*`; tests mirror under `Lighthouse.Backend.Tests/Modules/OrganisationSetup`; rules
  read from the IL (`IlDependencies`); lists inline, compared both ways, only shrink.
- ADR-270 acknowledged cycle: set-up is the **customer** of Tracker integration's query port; the mapping reaches the
  translation as data; **Tracker integration never references Organisation set-up**.
- ADR-270 Left open, answered by Story 6362: Feature ordering policy **and** ranking go to Organisation set-up, placed
  here; the nine files are named in `PromisesUnitTestPlacementTest` lines 82-90 with owner "Organisation set-up" (those
  entries are deleted in S2, with the move).
- S03f D-2: `AppSettingService` is Organisation set-up's; S03f D-4: every repository stays in `Services/*/Repositories`
  (owner "S03i settings and terminology", S05c Teams, S05d Portfolios, S05e connections); S03f D-5 and S03e D-5:
  context seeders stay in `Services/Implementation/Seeding`, S09a decides module start-up.
- S03a D-3 (via S03f, S03g, S03h): a `Models` type moves only if it is not an EF entity, not on the wire, not reached
  by a kernel type and not used outside the context.
- Epic 6474 (brief update 2026-11-16): move the code as it is; Team, Portfolio, `TeamDeleted` and Portfolio deletion
  stay as they are; `UnitCreated` / `UnitDeleted` replace them in 6474 slice 03.

## Wave: DESIGN / [REF] DoR re-run (2026-11-21)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Organisation set-up has no namespace: its 27 files sit in the **loose** `Services.Implementation` / `Services.Interfaces` namespaces (shared with platform and stray types) and three sub-folders. Rules stand for it by the loose namespace (`WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp = ^…Services\.(Implementation\|Interfaces)$`, line 57) and by full-name lists (E1). |
| 2 Users / value | PASS | Maintainers and Epic 6474: the context has its ADR-270 folder, so 6474 builds units and levels inside it and creates no module; the open-host edges become legal by namespace (ADR-270 line 289 expected this). No user-visible change. |
| 3 Hypothesis | PASS, sharpened | "Packages with namespace changes only." Holds as "namespace and `using` changes, plus the **fully qualified** registrations in `Program.cs` lines 1450-1452 and 1467" (E3). Disproved only if a production edit beyond `namespace` / `using` lines and those four registration lines is needed. |
| 4 Size | PASS | **M, about 5 days**, one nwdes feature, 5 slices (brief M-L; 6474's "→ S" assumed 6474 absorbed Team/Portfolio, which the 2026-11-16 update reversed, but nothing of Team/Portfolio moves either, D-6). |
| 5 AC from UAT | PASS, made precise | The brief has no written AC; S3 of the precedents is used: (a) no Organisation set-up file remains in `Services/Interfaces` or `Services/Implementation` **other than** the 4 context seeders (D-5) and the repositories (D-4) — open question 7; (b) Organisation set-up rules green by namespace; (c) downstream modules reach set-up's `Application` by right, Tracker integration only along a listed edge; (d) ADR-270 row 62 and `ARCHITECTURE.md` §4 updated. |
| 6 Dependencies | PASS | Hard: S02 gate; **added: S03e, S03f, S03g, S03h merged first** (shared rule files, loose namespaces); **and before Epic 6474 slice 03**. Soft: S05c/S05d/S05e own the repositories; S09a module start-up and seeders; #6657 the write-back round (stays loose). |
| 7 Technical notes | PASS | E5: no `GlobalSuppressions.cs` / `.editorconfig` / `.globalconfig` / `.csproj` / `Directory.Build.props` / `appsettings*.json` / `perf/helpers/collect.ts` / CI classifier target names a moving type; Stryker globs exist only in **archived** feature mutation configs (history). No schema change (D-0). |

## Wave: DESIGN / [REF] Baseline (`main` at `b9fb0ae41`, text search, `noisy`)
| Group | Files | Moves? |
|---|---|---|
| Calendar: `BlackoutPeriodService`, `RecurringBlackoutRuleService`, `RecurringBlackoutRuleExtensions`, `WeeklyRecurrence` + `IBlackoutPeriodService`, `IRecurringBlackoutRuleService` | 6 (loose) | yes, S2 |
| Feature ordering and ranking: `FeatureOrdering`, `FeatureOrderingPolicyProvider`, `FeatureRankingService`, `FeatureRankSeeder`, `OptionalFeatures/FeatureOrderingApplier` + `IFeatureOrdering`, `IFeatureOrderingPolicyProvider`, `IFeatureRankingService`, `IFeatureRankSeeder` | 9 (the list of `PromisesUnitTestPlacementTest` 82-90) | yes, S2 |
| Optional features mechanism: `OptionalFeatures/{DefaultOptionalFeatureApplier, OptionalFeatureApplierRegistry}`, `Interfaces/OptionalFeatures/IOptionalFeatureApplier` | 3 | yes, S2 (D-2: installation-wide switches are org-wide settings; `FeatureOrderingApplier` implements the port; S03f named `OptionalFeatures/*` "their contexts; not platform") |
| Settings and terminology: `AppSettingService`, `TerminologyService` + `IAppSettingService`, `ITerminologyService` | 4 (loose) | yes, S3 |
| Connection defaults and usage: `Connections/{ConnectionSaveService, ChangedConnectionDefault}`, `WorkTrackingSystemConnectionUsage` + `IConnectionSaveService`, `IWorkTrackingSystemConnectionUsage` | 5 | yes, S3 → `Application/Connections` (D-1) |
| Context seeders: `Seeding/{AppSettingSeeder, TerminologySeeder, OptionalFeatureSeeder, CategoryListNormalisationSeeder}` | 4 | **no** (D-5) |
| `FeaturePositionMap` + `IFeaturePositionMap` (numbers the Features table by `IFeatureOrdering`; used by `FeatureListService`, `FeaturesController`) | 2 | **no**: feature-list read side with `FeatureListService` (S03f "strays"); open question 6 |
| Repositories `Team`, `Portfolio`, `AppSetting`, `Terminology`, `BlackoutPeriod`, `RecurringBlackoutRule`, `WorkTrackingSystemConnection` (+ ports) | ~12 | **no** (D-4) |
| `Models`: `Team`, `Portfolio`, `WorkTrackingSystemOptionsOwner`, `SettingSource`, `EffectiveSetting`, `IncomingSetting`, `AppSetting`, `AppSettings/*`, `TerminologyEntry`, `BlackoutPeriod`, `RecurringBlackoutRule`, `*Dto`, `BlackoutDaysExtensions`, `FeatureOrderingPolicy`, `ManualRankComparer`, `ThroughputSettings`, `PortfolioSize`, events `TeamDeleted`, `BlackoutConfigurationChanged`, `FeatureOrderingPolicyChanged`, `FeatureRankChanged` | ~25 | **no** (D-3) |
| API: `TeamController`, `PortfolioController`, settings / terminology / blackout / optional-feature / connection controllers, `OwnerSettingRefusals`, `WorkTrackingSystemOptionsOwnerExtensions`, DTOs | many | no (driving adapters) |
| Update pipelines `TeamUpdater`, `PortfolioUpdater` | 2 | no (Epic 6474 slice 12) |
| Unit tests: `Services/BlackoutPeriodServiceTest`, `Services/Implementation/{AppSettingServiceTest, Connections/ConnectionSaveServiceTest, FeatureRankSeederTest, RecurringBlackoutRuleExtensionsTest, RecurringBlackoutRuleServiceTest, WeeklyRecurrenceTest, WorkTrackingSystemConnectionUsageTest}`, `API/TerminologyServiceTest` (misplaced today) | 9 | yes → `Tests/Modules/OrganisationSetup/…` (D-10) |
| Seeder tests (4), `BlackoutDaysExtensionsTest`, `BlackoutForecastShiftWriteBackTest` (write-back, #6657), `Models/ManualRankComparerTest`, API / integration / ManualSorting / container tests, `TestHelpers/FeatureOrderingTestHelper` | ~45 | no (`using` lines only) |

Moving: **27 production files** (16 implementations, 11 ports), about 1,900 lines (`wc -l`), **9 unit tests**.

Inbound (production, `noisy`, file counts excluding the type's own file): `IBlackoutPeriodService` 19, `IAppSettingService`
13, `IFeatureOrdering` 10, `ITerminologyService` 5, `IFeatureOrderingPolicyProvider` 5, `IOptionalFeatureApplier` 4,
others ≤ 3; tests: `IBlackoutPeriodService` 46, `IAppSettingService` 37, `IFeatureOrdering` 16, `FeatureOrdering` 15.
Module edges into the set (by `using` / name): **Flow metrics** `TeamMetricsService` (`IAppSettingService`,
`IBlackoutPeriodService`), `PortfolioMetricsService` (`IAppSettingService`); **Forecasting**
`ForecastRealityCheckService` (`IBlackoutPeriodService`); **Promises** `DeliveryMetricValuesProjector`
(`IBlackoutPeriodService`); **Work Items** `RefreshLogService` (`IAppSettingService`), `WorkItemService`
(`IFeatureOrdering`); **Tracker integration** `ConnectionHealthCadence` (`IAppSettingService`) — against ADR-270's
"Tracker integration never references Organisation set-up" (D-8). Refinement (after S03g) `RefinementCadenceCalendar`
→ `WeeklyRecurrence`.

Outbound (from the 27 files): kernel `Models` (entities, settings, events), the repositories (`IRepository<T>`,
`IPortfolioRepository`, …), **EF Core** (`ConnectionSaveService` `using Microsoft.EntityFrameworkCore`; other direct
`LighthouseAppContext` use measured in S1), **Work Items `Domain`** (`ConnectionSaveService` `using
Lighthouse.Backend.Modules.WorkItems.Domain`: the fetch fingerprint it compares before publishing
`ConnectionFetchShapeChanged`, per `TrackerIntegrationMembershipArchUnitTest` 197-198 — an edge from the upstream
context to a downstream one, listed in R-OS3), the domain-event dispatcher, the clock (platform after S03f),
`ILogger<ConnectionSaveService>`.

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | **The old home is the loose namespace, shared with types that stay.** Patterns that match it today: `WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp` `^…Services\.(Implementation\|Interfaces)$` (line 57, stands for **the context**); `RefinementModuleArchUnitTest.ServicesImplementationPattern` (line 24) and `WeeklyRecurrenceAndItsNestedTypesPattern` `^…Services\.Implementation\.WeeklyRecurrence($\|\+.*)` (line 34, a **type** pattern); `TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace` (line 26); `PromisesMembershipArchUnitTest` line 25 `^…Services\.(Implementation\|Interfaces)(\.DomainEvents)?$`; `PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore` (line 30) and `PromisesPoliciesLayoutArchUnitTest.WhereTheHandlersLivedBefore` (line 19), which stand for **the place** ("nothing of Promises left in `Services`"); `RepositoryRecognition` line 35 (`…\.(Repositories\|DatabaseManagement\|DomainEvents\|Seeding\|OAuth)`: seeders stay, D-5); `DependencySingleDecisionArchUnitTest` line 94 (`.Dependencies` only, unaffected); `WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore` (line 47, a **source-text** pattern `(?<![\w.])(Lighthouse\.Backend\.)?Services\.(Implementation\|Interfaces)\.((WorkItems\|…)\.)?` whose optional group also matches `Services.Interfaces.IAppSettingService` as text: Place, it stands for "where Work Items lived"; measured in S1 against the files that name a mover). **Subjects differ**: `OrganisationSetUp`, `PromisesMembershipArchUnitTest:25` and `PromisesPoliciesLayoutArchUnitTest:19` end in `$` or `(\.\|$)` right after the loose namespace and are applied to **namespaces** (they match `Lighthouse.Backend.Services.Interfaces`, never `…Interfaces.IFeatureOrdering`); `WeeklyRecurrenceAndItsNestedTypesPattern` is applied to **full type names**; `WhereTheTypesLivedBefore` to **source text**. Composite `static readonly` fields interpolate them (`WorkItemsModuleBoundariesArchUnitTest.AccessLicensingOrganisationSetUpAndTheUpdateQueue`, lines 159-160). | A pattern on a shared namespace is **ambiguous**: "context" patterns must follow the move (widen in S1, narrow in S5), "place" patterns must not. The law (D-7) therefore requires every regex field that matches a moving type's old name to carry an explicit classification, `Context` or `Place`, with a reason; an unclassified match fails. Plain widening, as in S03h, would be wrong for the `Place` patterns. |
| E2 | **Three non-regex forms** name moving types: exact-namespace equality `RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace = "Lighthouse.Backend.Services.Implementation"` used in `ResideInNamespace(...)` (line 12, 34: "the recurring-rule entity must not depend on the `BlackoutPeriodService`"); full-name string constants `RecurringBlackoutEventsSeamArchUnitTest.BlackoutPeriodServiceInterfaceFullName` (line 14), `WorkItemsMembershipArchUnitTest` line 230 (`outermost.FullName == "…Services.Interfaces.IAppSettingService"`); full-name list entries `WorkItemsModuleBoundariesArchUnitTest` lines 122 (`…Connections.ConnectionSaveService`), 150, 152, `PromisesModuleBoundariesArchUnitTest` line 144 and its planted fixture line 409/754 (`global::…IAppSettingService`, compile-checked). Path literals: `PromisesUnitTestPlacementTest` 82-90, `RepositoryLeakRatchetArchUnitTest` 45, 48, `TrackerIntegrationUnitTestPlacementTest` 236, `CategoryListOwnershipGuardTest` 30 and `OverTimeReconstructionSeamArchUnitTest` 104 (seeders, stay). Prefix lists: `PromisesUnitTestPlacementTest.PromisesTypeNamePrefixes` (`FeatureOrdering`, `FeatureRank`, line 97, type-name words, not namespaces). | The law evaluates **regex fields by behaviour** (`IsMatch` on old and new full names), **string fields equal to an old namespace or old full name** by equality, and the literal sweep lists every full-name / path literal by file and line with a fixed rename list. `nameof`/`typeof` users (`FeatureOrderingSingleSourceArchUnitTest`, `ConnectionFetchShapeChangedSinglePublisherArchUnitTest`, `OptionalFeatureApplierSeamArchUnitTest`, `TrackerIntegrationMembershipArchUnitTest` 197-198 by short name) are rename-safe. |
| E3 | `Program.cs` names the movers by **short name** behind `using Lighthouse.Backend.Services.Implementation;` / `.Interfaces;` (lines 18, 41; registrations 1397-1398, 1443, 1462-1464) **and fully qualified** at 1450-1452 (`…Implementation.OptionalFeatures.*`, `…Interfaces.OptionalFeatures.IOptionalFeatureApplier`) and 1467 (`…Implementation.Connections.ConnectionSaveService`). Seeder registrations 1500-1507 stay. | S2/S3 add `using Lighthouse.Backend.Modules.OrganisationSetup.Application[.Connections\|.OptionalFeatures];` and **rewrite the four fully qualified lines** (the loose `using`s stay: other loose types remain). Registration order, lifetimes and pairs unchanged (reviewer diff check). `Program.cs` is in `SHARED_REGEX` of `Scripts/test-selection/path-classifier.sh`, so a push carrying S2/S3 runs the live connector suites once (D-13). |
| E4 | **Implicit resolution** (measured with a text script; the IL decides): `Services/Implementation/Refinement/RefinementCadenceCalendar.cs` names `WeeklyRecurrence` with **no `using`** — it resolves through the **ancestor** namespace `Services.Implementation` (re-measured after S03g moves Refinement). The movers themselves name **no** non-moving loose type (`LostRaceTolerantSave`, `FeaturePositionMap`, `LighthouseClock`, `RefreshRoundSummary`, `EstimateNormalizer`, `FeatureListService`: none) and reach their sub-folders by explicit `using` (`AppSettingService` → `…Implementation.OptionalFeatures`; `FeatureOrderingApplier`, `ChangedConnectionDefault`, `ConnectionSaveService` → `Services.Interfaces`). But the loose movers resolve **each other** by same namespace (e.g. `FeatureOrdering` and `FeatureOrderingPolicyProvider`), so the `Domain` / `Application` split loses resolutions for `Application` files naming `WeeklyRecurrence` or the recurring-rule extensions. Domain candidates by `using`: `WeeklyRecurrence` (none), `RecurringBlackoutRuleExtensions` (`Models` only); `FeatureOrdering` names its two ports, so `Application`. Test side: tests under `Lighthouse.Backend.Tests.Services.Implementation[…]` reach production by `using`; `FeatureOrderingTestHelper` by `using …Tests.TestHelpers`. | S1 lists every implicit edge (origin, target) from the IL in the membership data; S2 adds `using …Modules.OrganisationSetup.Domain;` where a moved `Application` file or `RefinementCadenceCalendar` needs it (extension methods resolve only by `using`). A moved test that resolved a helper through its old **test** ancestor namespace is listed the same way. |
| E5 | Lessons from the earlier reviews, checked: `GlobalSuppressions.cs`, both `.editorconfig`, any `.globalconfig`, `.csproj`, `Directory.Build.props`: **no** target names a moving type. `appsettings*.json` Serilog overrides: only `…Auth.DisabledAuthenticationHandler`; `Lighthouse.EndToEndTests/perf/helpers/collect.ts`: only Forecasting's `ForecastUpdater`. **Log categories**: 1 moving class takes `ILogger<T>`, `ConnectionSaveService` (category becomes `Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService`); the four seeders with `ILogger<T>` stay. `Scripts/test-selection/*`: no path of the set (backend selected by prefix; `Program.cs` shared). `Scripts/test-timings/test-time-budget.json` line 134 and its Python test line 49 name `Integration.Containers.CategoryListNormalisationSeederOnBothProvidersTests`, which **stays** (seeder stays, D-5). `TestHostUseGuardTest.HostUsersStillToMove` / `TestHostPerFixtureGuardTest`: no moving test class. Stryker: no repo-level config names the set; 11 archived feature configs (`docs/feature/*/mutation/stryker.*.json`) glob old paths — history, not edited. `Scripts/examine/forecast-triggers-show.cs` uses the route `/api/v1/optionalfeatures/FeatureOrdering`, not a namespace. | S1's re-grep repeats this on the post-S03h `main` and records the result in the membership data. D-15's mutation config is written new for `Modules/OrganisationSetup/**`. |
| E6 | Domain-event publishing of set-up's own events: `TeamController.cs:115` publishes `TeamDeleted`; `PortfolioController.DeletePortfolio` (line ~70) removes the Portfolio and its refresh logs inline, publishing nothing (brief IN ratchet candidates). | Epic 6474 slice 03 replaces both with `UnitDeleted`; this Story **does not fix** them (6474's condition) but writes the ratchet with one listed edge (R-OS5), so a second controller-published set-up event cannot appear. |
| E7 | `ManualRankComparer` (Models) is used in production only by `FeatureOrdering`; `FeatureOrderingPolicy` is on the wire (`FeatureOrderingDto`) and in `AppSettingKeys`. | `FeatureOrderingPolicy` stays (wire). `ManualRankComparer` may pass S03a's four conditions; it stays by default with the kernel (open question 8), like S03h's `ReportOwnerGoneException`. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-21).

### D-0 No schema change, no behaviour change — RECOMMENDED
No entity, `DbSet`, migration, route, DTO, setting key, seeded value, terminology default, usage-data event or
configuration key changes. Checked by `ef-model-unchanged` and an empty diff under `Lighthouse.Migrations.*`,
`Lighthouse.Frontend`, `Lighthouse.EndToEndTests`, `Lighthouse.Backend/Lighthouse.Backend/Models`.

### D-1 Layout — RECOMMENDED: `Modules/OrganisationSetup/{Domain, Application, Application/Connections, Application/OptionalFeatures}`; a type goes to `Domain` iff its IL edges reach only the kernel, the BCL and other set-up `Domain` types
| Option | Verdict |
|---|---|
| **a (recommended)** `Domain` for kernel-only pure types (expected `WeeklyRecurrence`, `RecurringBlackoutRuleExtensions`; IL decides); `Application` for the services and their ports; `Application/Connections` for the connection save, `ChangedConnectionDefault`, the usage and their two ports; `Application/OptionalFeatures` for the three appliers, the registry and `IOptionalFeatureApplier` | Keeps the two existing groupings (`Connections`, `OptionalFeatures`) as sub-namespaces, so the move stays a move; `<Layer>.<Sub>` is used by `Modules/WorkItems/Application/Dependencies` and S03h's `Application/Metrics`. The connection usage and its port join `Connections` because ADR-270 names "the connection save and the connection usage" as one set-up concern. |
| b Sub-folders per concern (`Calendar`, `Ordering`, `Settings`, `Terminology`) | Rejected now: renames groupings the code does not have; Epic 6474 reshapes the module (levels, units) and decides its inner folders. |
| c One flat `Application` | Rejected: drops the two groupings that exist today. |
No `Application/Policies` (no mover implements `IDomainEventHandler`; measured in S1), no `Adapters/*` (D-4, D-5).

### D-2 Membership — RECOMMENDED: the 27 files move; the rest is named with owner
Moving: the five groups of the Baseline. Staying, each named in `OrganisationSetupMembership` with owner and reason:
4 context seeders (D-5), `FeaturePositionMap` / `IFeaturePositionMap` (feature-list read side, open question 6), the
repositories (D-4), the `Models` types (D-3), API controllers / helpers / DTOs (driving adapters), `TeamUpdater` /
`PortfolioUpdater` (6474 slice 12), the write-back round (#6657). The optional-features mechanism moves with ordering:
installation-wide switches are org-wide settings ("how it works by default"), `FeatureOrderingApplier` implements its
port, and S03f placed `OptionalFeatures/*` with "their contexts"; open question 5.

### D-3 `Models` — RECOMMENDED: all stay
Team, Portfolio, `AppSetting`, `TerminologyEntry`, `BlackoutPeriod`, `RecurringBlackoutRule` are EF entities;
`SettingSource`, `EffectiveSetting`, `IncomingSetting`, `WorkTrackingSystemOptionsOwner`, `ThroughputSettings` are
reached by the entities; the `*Dto` types and `FeatureOrderingPolicy` are on the wire; the events are published to and
handled by other contexts. `ManualRankComparer` (E7) stays by default; S04b reduces `Models` to the kernel.

### D-4 Repositories — RECOMMENDED: all stay in `Services/*/Repositories` (S03f D-4)
Named in the membership with owner (S05c Team, S05d Portfolio, S05e connections, "Organisation set-up" for settings,
terminology and blackouts) and target `Modules/OrganisationSetup/Adapters/Persistence` "when S05*/S10 reshape
repositories". `RepositoryRecognition` and `RepositoryLeakRatchetArchUnitTest` stay valid (paths on the rename list).

### D-5 Context seeders — RECOMMENDED: stay in `Services/Implementation/Seeding`
| Option | Verdict |
|---|---|
| **a (recommended)** The four set-up seeders stay, owner Organisation set-up, target "module start-up, S09a" | Same as S03e D-5 (Access seeders stayed although Access got a module) and S03f D-5; `ISeeder` registration order in `Program.cs` 1500-1507 is start-up ordering, S09a's concern. Keeps the container test in `test-time-budget.json`, `CategoryListOwnershipGuardTest` and `OverTimeReconstructionSeamArchUnitTest` untouched. |
| b Move to `Modules/OrganisationSetup/Adapters/Seeding` | Rejected by default: a second seeding decision in the Epic, and `RepositoryRecognition` line 35 would need a new alternative. Open question 7. |

### D-6 Team and Portfolio — RECOMMENDED: nothing of them moves; no unit, level or event change
The brief's first IN line ("Move Team and Portfolio (as units) with their settings") narrows: the entities stay in the
kernel (D-3), their repositories with the platform (D-4), their controllers in `API`; no Team- or Portfolio-specific
service exists in `Services` today (measured: only the repositories and the updaters). Their **settings** services move
(`AppSettingService`, `ConnectionSaveService`, `WorkTrackingSystemConnectionUsage`). 6474's condition holds: no shape
6474 reverses is touched. Open question 9 states the narrowing.

### D-7 Rules first — RECOMMENDED: EXTEND the shared preservation law with an Organisation set-up map **and a per-pattern classification**
For every regex-valued field `P` in the test assembly (const **and** static readonly strings, by reflection) and every
moved type `T`, the law evaluates `P` against **the subject the field is applied to**, declared per field in
`OrganisationSetupMembership.PatternRoles`: `Namespace` (`IsMatch(P, oldNamespace(T))` against `IsMatch(P, newNamespace(T))`)
or `FullName` (`IsMatch(P, oldFullName(T))` against `IsMatch(P, newFullName(T))`). An undeclared field is evaluated on
**both** subjects, and a match on either fails until the field is declared. If the old subject matches, `P` carries a
role: `Context` (must also match the new subject from S1 on: S1 widens it with
`|^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)` for a `Namespace` field, or with the type's new full name for a
`FullName` field) or `Place` (must **not** match the new subject: it stands for "the `Services` folder"), with a reason;
an unclassified match fails. **Composite fields** (`static readonly` strings built by interpolation, e.g.
`AccessLicensingOrganisationSetUpAndTheUpdateQueue`, WIMB 159-160) are read by the same reflection, inherit the subject
of the rule that applies them and are declared `CompositeOf(<field>)`: they follow their component by reference, so
widening the component widens them, and the law re-evaluates them to prove it. **Effect of widening, both intended**:
a `Context` pattern used as a **forbidden target** (`OrganisationSetUp` inside Work Items' "only through the list"
target set) gets **stricter** — the moved types stay forbidden-unless-listed after the move; a `Context` pattern used as
an **allowed** namespace gets **looser** by exactly the moved types, which were allowed before the move. String fields
equal to an old namespace or old full name (E2) take the same roles by equality. **Inline literals** in method bodies
(`WorkItemsMembershipArchUnitTest` 229-230, `outermost.FullName == "…Services.Interfaces.IAppSettingService"`) are not
fields and reflection cannot see them: only the literal sweep catches them, so each one is on its rename list. A second assertion is the **literal sweep** over the test project's source:
the moving types' old full names (`Lighthouse.Backend.Services.(Implementation|Interfaces)[.Connections|.OptionalFeatures].<T>`),
their old paths (`Services/(Implementation|Interfaces)/[Connections/|OptionalFeatures/]<T>.cs`), and
`Services\.(Implementation|Interfaces)\$`-style context patterns; its rename list is **fixed at S1 from the pre-move
grep and only shrinks**; each entry is a literal S2/S3 rename (removed when renamed; the sweep fails if a listed literal
no longer exists or an unlisted one appears). If S03g/S03h left `NamespaceMoveLaw` shared (taking a map), set-up adds
its map and the `PatternRoles` extension; if not, S1 lifts it first (one owner). **RED in S1** (measured today): `WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp` (subject `Namespace`, role
`Context`): the namespace `Lighthouse.Backend.Services.Interfaces` (old home of `IFeatureOrdering`) matches and
`Lighthouse.Backend.Modules.OrganisationSetup.Application` does not. This is **the real RED**: an existing rule that would
silently stop seeing the move. `RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace` fails only because
it is **not yet declared** in `PatternRoles`. That is a **RED by absence** (the map is new), not evidence of a rule defect. GREEN after S1 widens / classifies. Rejected: a text sweep alone (misses the loose-namespace
`$` patterns, which contain no type name) and plain widening of every match (wrong for `Place` patterns, E1).

### D-8 Open host by namespace — RECOMMENDED: downstream modules reach set-up's `Application` and `Domain` by right; Tracker integration only along a list
ADR-270: "Organisation set-up → every other context: open host … queries through its contract"; line 289: Work Items
listed the set-up edges only because "Organisation set-up has no namespace of its own yet … stricter than the 4 the
design expected". S4 makes `^Lighthouse\.Backend\.Modules\.OrganisationSetup\.(Application|Domain)(\.|$)` a legal
outbound target in the Work Items, Flow metrics, Forecasting, Promises, Refinement and Reports rules, and their listed
set-up edges leave the lists (lists shrink: Work Items 2, lines 150 and 152; Promises 1, line 144; others measured in S1). **Tracker integration**
keeps set-up forbidden (ADR-270 "never references Organisation set-up") with one listed edge, held in
`TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts`: `ConnectionHealthCadence
→ IAppSettingService` (refresh cadence), owner S09a / Epic 6474 (hand the cadence in as data). Rejected: legal by
namespace for Tracker integration (contradicts the acknowledged-cycle decision); keep every edge listed (ADR-270 line
289 calls that stricter than designed, and every new consumer of a setting would be a list edit).

### D-9 New rules — RECOMMENDED: `OrganisationSetupModuleBoundariesArchUnitTest` on `ModuleRules`, existing guards kept
See Ratchet design. `FeatureOrderingSingleSourceArchUnitTest`, `RecurringBlackoutEventsSeamArchUnitTest`,
`ConnectionFetchShapeChangedSinglePublisherArchUnitTest`, `OptionalFeatureApplierSeamArchUnitTest`,
`BlackoutForecastShiftSeamArchUnitTest`, `CategoryListOwnershipGuardTest` keep every assertion; they change only
through the classified patterns / literals of D-7.

### D-10 Tests move with production — RECOMMENDED; acceptance and wire tests stay
The 9 unit tests move in S2/S3's `refactor(organisation-setup): move … tests` commits to `Tests/Modules/OrganisationSetup/
{Domain, Application, Application/Connections}` (namespace = folder), including the misplaced `API/TerminologyServiceTest`
and `Services/BlackoutPeriodServiceTest`; kept there by `OrganisationSetupUnitTestPlacementTest` (S4). API, integration,
ManualSorting, container and seeder tests stay; only `using`s change. "Improve the tests you touch": each moved class is
read against the test-writing skill's backend half; none is a host user (E5), so no `HostUsersStillToMove` row moves;
a moved test that drives a service through mocks of the service's own collaborators is noted for the reviewer, not
rewritten (behaviour-free Story).

### D-11 DI — RECOMMENDED: same registrations; new `using`s and the four fully qualified lines rewritten
`Program.cs` gains up to three `using` lines; lines 1450-1452 and 1467 change their namespace text only. No
`AddOrganisationSetup()` (S09a).

### D-12 Log category — RECOMMENDED: accept the namespace change of `ConnectionSaveService`'s category
No override, filter or harness reads it (E5). Recorded in the DEVOPS note.

### D-13 CI and pushes — RECOMMENDED: commit per slice, push S1-S5 together
S2 and S3 touch `Program.cs`, which selects every live connector suite; one push means one live run. Push only with all
five green locally (fast loop and the filtered full gate).

### D-14 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse admitted probes, no new compare command
| Expectation | Probe |
|---|---|
| X1 Same types, new namespaces; all rules green; set-up rules green by namespace | `architecture-rules` (all) |
| X2 No EF model or migration change | `ef-model-unchanged` |
| X3 Same answers (settings read/write, terminology, blackouts and recurring rules, Feature ordering policy and manual rank, optional features toggle, connection save with default inheritance and usage counts) | `backend-tests` on the unchanged-assertion API / integration / ManualSorting / BlackoutForecastShift / RecurringBlackoutRules classes, both revisions |
| X4 CI selects backend for `Modules/OrganisationSetup` paths; live suites for `Program.cs` | `test-selection-show` |

### D-15 Mutation bar — RECOMMENDED
Last, on frozen code, Stryker.NET over `Modules/OrganisationSetup/**` (x64 SDK, `run-backend-x64.ps1`), compared with
the same files' run on the pre-move revision: **no survivor or no-coverage mutant added in the moved code**; the 80 %
project bar reported.

### D-16 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the law is executed exhaustively on the real IL
Claims: (C1) membership — every listed type is in exactly its listed namespace; (C2) preservation — for every pattern
and moved type, classification `Context` ⇒ match(old) = match(new) during S1-S4, `Place` ⇒ ¬match(new); (C3) the
boundary rules R-OS1..R-OS6. All are relations over a **finite, closed** set (compiled types × membership map × regex /
string fields): no state, transition, concurrency, time, fairness or unbounded input, so no state-model or temporal
checker applies, and a proof over a hand-copied abstraction would be weaker than deciding the relation on the compiled
assembly. Each is decided **exhaustively** by NUnit over the IL — the executable projection the default suite runs.
PBT: N/A, closed finite domain. Formal result: `NOT_SELECTED` (reason above), DESIGN `PASS`, runtime
`DESIGNED_NOT_BUILT`; no install or waiver needed. Open question 10 offers a model anyway.

## Wave: DESIGN / [REF] Slices
One nwdes feature `story-6468-s03i-package-organisation-set-up`, 5 slices (the nwdes maximum; if S1's re-measure needs a
sixth slice, **S5 is the split point**: S1-S4 stay in this feature and S5 becomes a second nwdes feature that depends on
it), each ≤ 1.25 days, moves in commits apart
from edits (`refactor(organisation-setup): …`). Size **M, about 5 days**.

| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Est. |
|---|---|---|---|---|
| S1 | **First action: the four "merged" checks for #6363-#6366 and the 6474-slice-03 check under "Start condition"; then re-measure every count of this file.** Rules see the move before it happens: membership measured from the IL (each type, layer, old and new namespace; staying files with owner and reason; implicit edges of E4); every pattern matching an old name classified `Context` / `Place` and the `Context` ones widened (`WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp`, `RefinementModuleArchUnitTest.WeeklyRecurrence…Pattern` or its S03g successor, `TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace`, `RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace` turned into a namespace pattern of old ∪ new); E5 re-grep recorded | `OrganisationSetupMembership.cs` + `OrganisationSetupMembershipArchUnitTest.cs` (a listed type is found in its old **or** new namespace; RED when a listed name does not exist or a `Domain` candidate's IL edges leave the kernel); the law of D-7 over the set-up map — **RED today** on `OrganisationSetUp` (Context: old matches, new does not) and on the unclassified `ServiceImplementationNamespace`; GREEN after classifying and widening; the literal sweep with its fixed list | none | 1.25 d |
| S2 | Calendar, Feature ordering and ranking, optional features live in `Modules/OrganisationSetup/{Domain, Application, Application/OptionalFeatures}`; their 5 unit tests in `Tests/Modules/OrganisationSetup/…`; `using`s, `Program.cs` (1443, 1450-1452, 1463-1464), the E2 literals of this group renamed. **In the commit that moves the nine ordering and ranking files**, their nine `SourceFilesThatStay` entries in `PromisesUnitTestPlacementTest` (lines 82-90) are **deleted** (otherwise they fail "named as staying … but holds no file", line 404), and `PromisesTypeNamePrefixes` (line 97) drops `FeatureOrdering`, `IFeatureOrdering`, `FeatureRank`, `IFeatureRank`: the scan reads only `Services/` paths (line 396), where no file with those prefixes is left (`FeaturePositionMap` does not carry them); `WriteBack` / `IWriteBack` stay for #6657 | S1's membership test tightened to accept **only the new namespace** for the 18 types of this group — RED before the move, GREEN after; all existing rules green | 18 files moved; `Program.cs`; consumers' `using`s (Flow metrics, Forecasting, Promises, Work Items, Refinement, `FeaturePositionMap`, `FeatureListService`, controllers, updaters) | 1 d |
| S3 | Settings, terminology, connection defaults and usage live in `Modules/OrganisationSetup/{Application, Application/Connections}`; their 4 unit tests moved; `using`s, `Program.cs` (1397-1398, 1462, 1467), E2 literals of this group. `TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts` (line 136, empty today) gets its one entry `…TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence -> …Modules.OrganisationSetup.Application.IAppSettingService` in the same edit commit, because the edge now crosses into a module | the same tightening for the 9 types of this group — RED before, GREEN after | 9 files moved; `Program.cs`; consumers' `using`s (incl. Tracker integration's `ConnectionHealthCadence`) | 1 d |
| S4 | Downstream modules reach set-up by right (D-8); set-up has its own boundary rules and test placement; the `TeamDeleted` ratchet | **RED oracle**: `OrganisationSetupIsAnOpenHostArchUnitTest`. `WorkItemsModuleBoundariesArchUnitTest.EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue` holds no edge into `Modules.OrganisationSetup` (after S2/S3's renames today's line 150 `RefreshLogService -> …IAppSettingService` and line 152 `WorkItemService -> …IFeatureOrdering`); Promises' outbound list holds none (`DeliveryMetricValuesProjector -> …IBlackoutPeriodService`, `PromisesModuleBoundariesArchUnitTest` line 144); and `TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts` holds **exactly** `ConnectionHealthCadence -> …IAppSettingService`. RED while the three Work Items / Promises entries exist, GREEN after they leave and both modules admit set-up by namespace. Flow metrics, Forecasting, Refinement and Reports reach set-up today without a list entry (S1 measures how their rules classify the loose namespace); they get the namespace allowance in the same slice, guarded by the planted falsifiers, not by this RED. Alongside (green on writing, guards later regressions): `OrganisationSetupModuleBoundariesArchUnitTest.cs` (R-OS1..R-OS5), `OrganisationSetupUnitTestPlacementTest.cs`, planted-type falsifiers in the `ModuleRulesCatchANewOffenderArchUnitTest` style (a planted Tracker integration type calling `IAppSettingService` fails; a planted controller publishing `BlackoutConfigurationChanged` fails) | none (test side) | 1 d |
| S5 | Rules by namespace only: every `Context` pattern narrowed to `Modules.OrganisationSetup`; docs: ADR-270 row 62 → "**Packaged** in `Modules/OrganisationSetup` by Story 6468 …", row 63's "stay for Organisation set-up (S03i)" and line 289 and Left open 845-851 brought to current, `ARCHITECTURE.md` §4 (a new Organisation set-up row or row 4 split, plus a "Organisation set-up module rules" bullet), catalogue #6468 row, brief IN / namespace rule (open question 9) | **RED oracle**: S1's sweep run with an **empty** rename / allowance list — no regex field classified `Context` may still match an old name of a moved type, and no string literal in the test project may still name an old full name / path of the 27. The sweep **excludes itself and its own fixtures by exact path** — the sweep test file, `OrganisationSetupMembership.cs`, `OrganisationSetupMembershipArchUnitTest.cs`, the set-up map passed to the preservation law, and the planted-type falsifier fixtures — because they must spell the old names to do their job; the exclusion list is itself asserted equal to exactly those paths (each must exist), so a real rule file cannot hide behind it. RED at S5's start (S1's widened old alternatives still match), GREEN once narrowed. Written first, observed RED, then narrowed. `Place` patterns are untouched and asserted still to match no moved type | none (test side and docs) | 0.75 d |

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Blackout|FullyQualifiedName~Terminology|FullyQualifiedName~ManualSorting|FullyQualifiedName~OptionalFeature|FullyQualifiedName~AppSetting|FullyQualifiedName~ConnectionSave|FullyQualifiedName~ConnectionUsage|FullyQualifiedName~FeatureRank|FullyQualifiedName~WeeklyRecurrence"
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```
Driving port for every oracle: the compiled production assembly read through `LighthouseArchitecture.Production` /
`IlDependencies` (in-process; the nearest executable argv is the `dotnet test … ~Architecture` line). Behaviour is
observed through the unchanged acceptance tests at the application-service port and the host tests.

Test time: the new tests reuse the IL loaded once per assembly (`LighthouseArchitecture`), a shared read-only world;
each stays within the repository's per-test budget; no infrastructure suite, no derogation.

Rigor (CLAUDE.md profiles, proposed): S1 and S4 `thorough` (opus / sonnet: the law's `Context`/`Place` classification and
the open-host rule change are where a miss is silent); S2, S3, S5 `standard` (sonnet / haiku). Open question 1.

## Wave: DESIGN / [REF] Ratchet design
### S1 classification and widening (old ∪ new), S5 narrowing (new only)
Subject: `NS` = applied to namespaces, `FN` = applied to full type names, `TXT` = applied to source text.

| Owner | Field | Role (subject) | S1 | S5 |
|---|---|---|---|---|
| `WorkItemsModuleBoundariesArchUnitTest` | `OrganisationSetUp` `^…Services\.(Implementation\|Interfaces)$`; composite `AccessLicensingOrganisationSetUpAndTheUpdateQueue` (159-160) follows by reference | Context (NS); a forbidden-unless-listed target, so widening tightens | `+ ^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.\|$)` | set-up only; the stray loose types still reached (e.g. `FeatureListService` users) move to a separate `StrayLooseServices` constant with their own list (measured) — no loosening |
| `RefinementModuleArchUnitTest` (or S03g successor) | `WeeklyRecurrenceAndItsNestedTypesPattern` | Context (FN) | `+ …Modules\.OrganisationSetup\.Domain\.WeeklyRecurrence(\$\|\+.*)` (layer by IL) | new only |
| `RefinementModuleArchUnitTest` | `ServicesImplementationPattern` | measured at S1 (S03g may have removed it) | — | — |
| `TrackerIntegrationApplicationLayoutArchUnitTest` | `AnImplementationNamespace` | Context (NS) | `+ Modules\.OrganisationSetup` | kept with set-up alternative (Tracker integration `Application` still reaches no set-up implementation) |
| `RecurringBlackoutEventsSeamArchUnitTest` | `ServiceImplementationNamespace` (equality) | Context (NS); S1 RED by absence only | becomes a pattern of old ∪ set-up `Application` (`ResideInNamespaceMatching`) | set-up only |
| `PromisesLayoutArchUnitTest`, `PromisesPoliciesLayoutArchUnitTest`, `PromisesMembershipArchUnitTest:25` | "where Promises lived" | Place (NS; line 25 ends in `$`, line 19 in `(\.\|$)`) | unchanged | unchanged; asserted to match no moved type |
| `RepositoryRecognition` | line 31 / 35 | Place (NS; repositories, seeding) |
| `WorkItemsApplicationLayoutArchUnitTest` | `WhereTheTypesLivedBefore` (47) | Place (TXT) | unchanged | unchanged | unchanged | unchanged |
| `WorkItemsModuleBoundariesArchUnitTest` 122/150/152, `PromisesModuleBoundariesArchUnitTest` 144/409, `WorkItemsMembershipArchUnitTest` 229-230 (inline, text sweep only), `RecurringBlackoutEventsSeamArchUnitTest` 14, `RepositoryLeakRatchetArchUnitTest` 45/48, `TrackerIntegrationUnitTestPlacementTest` 236 | literals | on the S1 rename list | renamed in S2/S3 (lists' entries leave in S4 where D-8 makes them legal) | — |
| `PromisesUnitTestPlacementTest` | `SourceFilesThatStay` 82-90, `PromisesTypeNamePrefixes` 97 (paths and type-name words) | not a pattern | unchanged | — (**deleted in S2's move commit**, see S2) |

### New rules (S4), baselines from the IL
| # | Rule | Expected baseline (text; IL decides) |
|---|---|---|
| R-OS1 | Each listed set-up type lives in its layer; none left under `Services` other than the named stays | membership map |
| R-OS2 | `Domain` → kernel and BCL only | empty |
| R-OS3 | `Application` → no `API`, no module `Adapters`, no downstream context (Work Items, Flow metrics, Forecasting, Promises, Refinement, Reports) except along a listed edge; Tracker integration `Application` legal (customer of the ACL); `Data` / EF Core only along a listed edge | listed (S1 measures): `ConnectionSaveService` → Work Items `Domain` (fetch fingerprint) and → EF Core; Tracker integration `Application` legal (customer of the query port) |
| R-OS4 | Tracker integration → set-up only along a listed edge | 1: `ConnectionHealthCadence` → `IAppSettingService` |
| R-OS5 | Set-up's events (`TeamDeleted`, `BlackoutConfigurationChanged`, `FeatureOrderingPolicyChanged`, `FeatureRankChanged`) are constructed only in set-up `Application`, except along a listed edge | 1: `API.TeamController` → `TeamDeleted` (Epic 6474 slice 03 empties it); others measured |
| R-OS6 | Downstream modules → set-up `Application` / `Domain` legal by namespace (D-8) | n/a (allowance; the S4 oracle proves the lists emptied) |
Lists inline, by full type name, compared both ways, only shrink. `ModuleWording("Organisation set-up", …)`.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `WeeklyRecurrence`, `RecurringBlackoutRuleExtensions` (if IL kernel-only) | `Services/Implementation` | `Modules/OrganisationSetup/Domain` | MOVE | S2 |
| Blackout and recurring-rule services + 2 ports | `Services/{Implementation,Interfaces}` | `…/Application` | MOVE | S2 |
| Feature ordering and ranking (9) | `Services/{Implementation,Interfaces}[/OptionalFeatures]` | `…/Application` (`FeatureOrderingApplier` → `Application/OptionalFeatures`) | MOVE | S2 |
| Optional-features mechanism (3) | `Services/{Implementation,Interfaces}/OptionalFeatures` | `…/Application/OptionalFeatures` | MOVE | S2 |
| `AppSettingService`, `TerminologyService` + 2 ports | `Services/{Implementation,Interfaces}` | `…/Application` | MOVE | S3 |
| Connection save, `ChangedConnectionDefault`, usage + 2 ports | `Services/{Implementation,Interfaces}[/Connections]` | `…/Application/Connections` | MOVE | S3 |
| Seeders, repositories, `Models`, `FeaturePositionMap`, controllers, updaters | — | — | UNCHANGED (D-3..D-6) | — |
| Set-up rules, membership, placement, open-host oracle | `Tests/Architecture` | same | EXTEND / CREATE_NEW | S1, S4, S5 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged — `TeamController`, `PortfolioController`, the settings, terminology, blackout-period,
recurring-rule, optional-feature, Feature-ordering / rank (`FeaturesController`) and connection controllers; CLI / MCP
through the same endpoints. Driven: unchanged — the repositories (`Services/*/Repositories`), `LighthouseAppContext`
where measured, the domain-event dispatcher, Work Items' fetch fingerprint (listed edge), Tracker integration's query port (customer edge, via the API band today), the clock.
No new external call.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| Shared preservation law (`NamespaceMoveLaw` if S03g/S03h lifted it) | proving rules see a move | **EXTEND** with the set-up map and the `Context`/`Place` pattern roles (new: needed because the old home is shared, E1) |
| S03g/S03h literal sweep (alternation-aware) | old-namespace literals | **EXTEND** with full-name equality constants and the loose `$` patterns |
| `TrackerIntegrationMembership` / `PromisesMembership` / `RefinementMembership` / `ReportsMembership` | namespace-agnostic membership | **REUSE** shape for `OrganisationSetupMembership` |
| `ModuleRules`, `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | layer and edge rules with lists, falsifiers | **REUSE** for R-OS1..R-OS5 |
| `…UnitTestPlacementTest` (six modules) | tests mirror layout | **REUSE** shape |
| `FeatureOrderingSingleSourceArchUnitTest`, `RecurringBlackoutEventsSeamArchUnitTest`, `ConnectionFetchShapeChangedSinglePublisherArchUnitTest`, `OptionalFeatureApplierSeamArchUnitTest` | set-up's existing guards | **REUSE** unchanged except D-7 fields |
| S03d C1 "calls by right" oracle (`03639b027`) | legal-by-namespace edges, lists shrink | **REUSE** shape for the S4 open-host oracle |
| `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
No CREATE_NEW production type. Semantic-duplicate check: "which namespaces are Organisation set-up" gets one owner, a
new `OrganisationSetupPattern` in `ModuleBoundariesArchUnitTest`'s module map, which `WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp`
then reads (S1) instead of its own loose-namespace copy; the membership map is its measured projection.

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| The 27 files of the Baseline | production | MIGRATED | D-1, D-2 |
| `Program.cs` `using`s + lines 1450-1452, 1467 | composition root | CHANGED | D-11, E3 |
| Consumers: Flow metrics (2), Forecasting (1), Promises (1), Work Items (2), Tracker integration (1), Refinement (1), controllers, `FeaturePositionMap`, `FeatureListService`, updaters, seeders, repositories (by `using`) | consumers | CHANGED (`using`s) | |
| 9 unit tests; ~45 test files naming the moved types | tests | MIGRATED / CHANGED | D-10, E2 |
| Rule files of E1, E2 | architecture tests | CHANGED | S1, S2, S3, S4, S5 |
| `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, `Directory.Build.props` | build / analyser | UNCHANGED_COMPATIBLE unless S1's re-grep finds a target | E5 |
| `path-classifier.sh`, `dev-test.*`, `test-time-budget.json` + Python test, `HostUsersStillToMove`, `TestHostPerFixtureGuardTest` | CI / test infra | UNCHANGED_COMPATIBLE | E5 (named classes stay) |
| Archived `docs/feature/*/mutation/stryker.*.json` (11 files glob old paths) | history | UNCHANGED (dated) | E5 |
| Log category of `ConnectionSaveService` | runtime log | CHANGED (namespace) | D-12 |
| `Models`, `Data`, migrations, repositories, seeders, frontend, E2E, Lighthouse-Clients | — | UNCHANGED | D-0, D-3..D-5 |
| ADR-270 row 62 → "**Packaged** in `Modules/OrganisationSetup` by Story 6468: settings, Terminology, blackouts and recurring rules, Feature ordering and ranking, optional features, connection defaults and usage. Team, Portfolio and the mapping stay in the kernel and the repositories until Epic 6474 and S05c-e; levels and units are target"; row 63's S03i note, line 289 (set-up edges legal by namespace), Left open 845-851 ("placed by Story 6468"), an "Organisation set-up today" layout line; `ARCHITECTURE.md` §4 row and rules bullet; catalogue #6468 row and the "S03a-S03h" enumeration note (line 971); brief IN / namespace rule | docs | CHANGED in S5 | context-map rule |
| `docs/product/architecture/brief.md`, older ADRs (060, 134, 135, …) naming `Services.*` types | docs | UNCHANGED: history (dated records); ADR-270 and `ARCHITECTURE.md` §4 are the current authority | |

Agreement analysis: no shared wire / schema / config contract changes (D-0); `not-applicable` for agreement and contract
tests (single deployable, same commit; the clients read the unchanged HTTP API). Architectural tests: applicable
(R-OS1..R-OS6, the law, the open-host oracle). Prefactoring: S1 is the prefactoring (rules widened and classified,
production untouched; the existing suite GREEN before and after S1, then S2/S3 move).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly (input) → `OrganisationSetupMembership` + law (producer) → ArchUnit rules (consumer) →
  `dotnet test … ~Architecture` exit and message (public observation). Falsifiers: a planted type left in
  `Services.Interfaces` with a listed name; a planted Tracker integration type calling `IAppSettingService`; a planted
  controller constructing `BlackoutConfigurationChanged` — each fails with WHAT / WHY / HOW. DESIGN `PASS`, runtime
  `DESIGNED_NOT_BUILT`, proof `NOT_SELECTED` (D-16).
- Stressors (negated assumptions): (1) "a pattern that matches the old home stands for the context" — false for the
  shared loose namespace (E1) → `Context`/`Place` roles, unclassified fails; (2) "a text sweep finds every pattern" —
  false for `^…Services\.(Implementation|Interfaces)$` (no type name in it) → behavioural law; (3) "patterns are
  regexes" — false for the equality constant and `ResideInNamespace` (E2) → equality branch; (4) "registrations use
  short names" — false for 4 lines (E3); (5) "only `using`s move with files" — false for ancestor / same-namespace
  resolution (E4) → IL list, `using`s added; (6) "S03e-S03h left the counts as measured" — false by construction →
  S1 re-measures; (7) "6474 has not started" — checked (Start condition 4); (8) "narrowing `OrganisationSetUp` keeps
  Work Items as strict" — false if Work Items still reaches stray loose types → separate stray constant with its list.
  Walked afterwards with a second set (a moved type found by reflection by **name string** — `TrackerIntegrationMembershipArchUnitTest`
  197-198 uses short names, rename-safe; a DI resolution by open generic over `Services.Implementation` — none found
  in `Program.cs`; `OptionalFeatureApplierRegistry` discovering appliers by namespace — it uses DI `IEnumerable`,
  `OptionalFeatureApplierSeamArchUnitTest` covers it; Data Protection / JSON payloads storing a type name — none of
  the 27 is persisted): all survive. Walkthrough, not execution.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: **Organisation set-up** (packaged, no behaviour). Edges: Organisation set-up → every downstream
  context (open host) becomes legal by namespace (D-8); Organisation set-up → Tracker integration (customer of the
  query port) unchanged; Tracker integration → Organisation set-up stays forbidden with one listed edge (refresh
  cadence); Access and Licensing edges unchanged (guards in `API`).
- New terms / concepts: none. ADR-270 glossary terms used as they are (Unit, Portfolio, Mapping, Terminology).
  Domain events: none added; `TeamDeleted` and Portfolio deletion stay as they are for Epic 6474 slice 03 (R-OS5).
- UI surface: none (nothing a user sees changes).
- ADR-270 / `ARCHITECTURE.md` §4 updates: S5 tasks listed in Impact closure.
- Continuous improvement: `/continuous-improvement before 6468` was not run in this AFK DESIGN (it presents items to
  the maintainer); open question 11.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] S03e, S03f, S03g, S03h merged by the three checks; Epic 6474 slice 03 not landed; counts re-measured in S1.
- [ ] Behaviour-free: routes, DTOs, setting keys, seeded values, usage-data events unchanged; frontend and E2E untouched.
- [ ] No migration, `Data` or `Models` file changes; repositories and seeders stay in place and change only `using` lines.
- [ ] Moves in `refactor(organisation-setup): move …` commits apart from edits.
- [ ] `Program.cs`: only `using` lines and lines 1450-1452, 1467 change.
- [ ] Every pattern matching an old name classified `Context` / `Place`; `Place` patterns untouched.
- [ ] Strings, suppressions, log overrides and CI targets re-grepped (E5) on the post-S03h `main`.
- [ ] Open-host lists shrunk; Tracker integration list holds exactly one set-up edge.
- [ ] Architecture docs updated in S5 (ADR-270 row 62, 63, line 289, Left open; `ARCHITECTURE.md` §4; brief); finalize checklist run before the push.

## Wave: DEVOPS
- **Platform lens**: none — no deployment, configuration, persistence, pipeline or runtime change; one push runs the live
  connector suites once because `Program.cs` changes (D-13). One log category changes namespace
  (`ConnectionSaveService`); no shipped override or harness reads it (E5).
- **Usage data**: **N/A, because** the Story changes no behaviour a user can use; the existing settings, terminology,
  optional-feature (`OptionalFeatureToggledEventTests`) and connection events keep firing unchanged, and no new outcome
  exists to count. `UsageDataEventName` is not touched.
- KPI: KPI 1 of the catalogue (files under `Services`) drops by 27; measured at S5.

## Open questions (default taken)
1. Rigor: S1 and S4 `thorough` (opus / sonnet), S2, S3, S5 `standard` (sonnet / haiku)? **Default: yes.**
2. Two move slices (S2 calendar / ordering / optional features, S3 settings / terminology / connections) instead of one
   (S03h moved 31 files in one)? **Default: two — `IBlackoutPeriodService` and `IAppSettingService` alone touch about
   98 distinct files (32 production, 66 tests), and each slice keeps a reviewable diff and its own RED.**
3. Make set-up's `Application` and `Domain` legal by namespace for every downstream module (D-8), shrinking their lists,
   rather than keeping every edge listed? **Default: yes (ADR-270 line 289; S03d C1 precedent).**
4. Keep Tracker integration's `ConnectionHealthCadence → IAppSettingService` as the one listed edge (owner S09a / Epic
   6474) instead of fixing it here? **Default: list it; fixing it is a behaviour-shaped edit.**
5. The optional-features mechanism (`IOptionalFeatureApplier`, the registry, the default applier) moves with
   Organisation set-up (D-2)? **Default: yes.**
6. `FeaturePositionMap` / `IFeaturePositionMap` stay with the feature-list read side (`FeatureListService`), not
   ordering? **Default: stay; it reads `Data` directly and serves the feature list.**
7. The four context seeders stay in `Services/Implementation/Seeding` until S09a (D-5); AC (a) therefore reads "no
   Organisation set-up file remains in `Services` other than the four seeders and the repositories"? **Default: yes.**
8. `ManualRankComparer` (Models, used only by `FeatureOrdering`) stays in the kernel for S04b? **Default: stay.**
9. The brief's IN narrows: Team and Portfolio "as units" do not move (entities kernel, repositories platform, no
   service exists); the ratchet candidates become a listed rule (R-OS5) instead of a fix; the namespace rule is
   restated as "set-up depends down only on the kernel, the platform infrastructure, the repositories and Tracker
   integration's query port; downstream contexts reach it by right; Tracker integration does not". **Default: S5
   amends the brief so.**
10. No formal model (D-16): the claims are finite relations decided exhaustively on the IL. **Default: no model.**
11. `/continuous-improvement before 6468` (CLAUDE.md, at DISCUSS / DESIGN at the latest) was not run AFK. **Default:
    run it at the start of DELIVER with the maintainer, before S1; a chosen item runs as its own item.**
12. Push S1-S5 together (one live connector run) rather than per slice? **Default: together.**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built; files move and rules are restated.
```
 Driving (unchanged)            Modules/OrganisationSetup (NEW folder, MOVED code)        Driven (unchanged)
 TeamController        ─┐   Application: AppSettingService, TerminologyService,      ┌─ repositories (stay in
 PortfolioController   ─┤     Blackout + recurring-rule services, FeatureOrdering,   │   Services/Repositories)
 Settings / Terminology─┼─►   FeatureRanking, rank seeder, policy provider (+ ports) ├─ LighthouseAppContext
 Blackout / Rules ctrls─┤   Application/OptionalFeatures: appliers, registry          ├─ domain-event dispatcher
 Features (rank, order)─┤   Application/Connections: connection save, default         └─ Tracker integration query
 Connection controllers─┘     change, usage (+ ports)                                     port (customer edge)
                            Domain: WeeklyRecurrence, recurring-rule extensions
                            Kernel Models (UNCHANGED): Team, Portfolio, settings, events
   Downstream (Work Items, Flow metrics, Forecasting, Promises, Refinement, Reports) ──► Application: by right
   Tracker integration ──► Application: one listed edge only (refresh cadence)
```
- Driving adapters: the controllers — unchanged. Application services and ports: moved, ports beside their use cases,
  two existing sub-groups kept. Domain: two pure calendar types. Kernel: Team, Portfolio, settings entities and events
  stay. Driven adapters: repositories and seeders stay; no new external call.

**Patterns weighed.**
- Rules first, then move (chosen): the rules must see the move, or they pass over an empty set.
- Preservation law with **`Context` / `Place` roles** (chosen) vs plain widening (rejected: the old home is a shared
  folder, and "nothing of Promises in `Services`" must not start following set-up) vs a text sweep alone (rejected:
  `^…Services\.(Implementation|Interfaces)$` names no type).
- Open host service (chosen, ADR-270): downstream contexts conform and call set-up's ports by namespace; the
  anti-corruption direction stays strict, so Tracker integration keeps one listed edge.
- Domain events from the application layer (kept as a ratchet, not fixed): `TeamDeleted` from a controller stays for
  Epic 6474's `UnitDeleted`; fixing it now would build a shape 6474 replaces.
- Domain / Application split by measured dependencies (chosen) vs all in Application (rejected: loses the kernel-only
  guarantee for the pure calendar types).
- Context-owned repositories and seeders in `Adapters/*` (rejected for now): one repository decision (S03f D-4) and one
  seeding decision (S03e/S03f D-5) for the Epic; S05* and S09a own them.
- Moving kernel models / Team-Portfolio as one `Unit` (rejected): EF snapshots and wire shapes would change, and the
  `Unit` aggregate is Epic 6474's.
- Single responsibility / dependency inversion / interface segregation: ports stay with their use cases; set-up depends
  down only; consumers depend on its ports, not its implementations.

## Host decisions during DELIVER (AFK, 2026-11-25)
- **S2: downstream set-up edges leave with the move.** Refinement's and Promises' outbound rules read set-up only through the Services/Platform folder, and S1's roles keep them folder-only, so a renamed list entry would be an edge no rule reads. Their entries leave in the slice that moves the set-up type; until S4 the edges are legal by omission. The five slices are pushed together, so no pushed revision carries that window. S4 adds the by-namespace allowance with a planted falsifier per module (design D9, amended).
- **S3: the platform lane keeps its three settings edges guarded.** No rule guarded a platform type reaching a context module (only their Adapters), so the platform lane's Services-only rule also reads set-up's module rather than dropping the three edges into `IAppSettingService`. Follow-up, outside this Story: no rule guards Platform → other `Modules.*`.
- **S5: the move-preservation laws end for the narrowed fields.** The preservation claim holds only during S1-S4. `OrganisationSetupPatternRoles.NamedByTheModuleOnly` (five fields) and `PlatformLanePatternRoles.NoLongerReadTheLooseServicesNamespaces` (three fields) declare the only fields that may stop seeing the old place; each must see set-up only in its module, and the platform list holds only while none of the platform lane's moved types is left in the loose namespaces (checked on every run). The open-host check's Work Items witness changed from the loose Services namespace to `Modules.Licensing.Application`, because the rule now matches the loose namespaces' staying types by full name, not by namespace (slice review S5 findings F1, F2).
- **S5: the stray at the board-port call site.** `TrackerIntegrationApplicationLayoutArchUnitTest` passes `StrayLooseImplementation` at its one call site instead of inside the namespace pattern, because the pattern matches namespaces and the strays are full names; equivalent while there is one caller (finding F3).
- **S5: the brief keeps its history.** `brief.md`'s Recurring blackout section (line ~2029) names the Services paths that existed when that feature was designed; the brief is a per-Story log, so it stays as dated history (finding F4).
- **Candidate review: Access and Licensing are open-host consumers of set-up.** The design text said "Access and Licensing edges unchanged", but their outbound rules read set-up only through the Services/Platform folder, so after the move they reach set-up's Application and Domain by right, like every other downstream context. Accepted: ADR-270's context map already has both depend on set-up (Access is conformist to the units that exist; Licensing counts units against the free-tier limit), and set-up is the open host for every context. No compiled Access or Licensing edge into set-up exists today. The open-host oracle checks both rules still read a foreign context that is not set-up (finding 1).
- **Candidate review: fixture names.** `AnAccessServiceAskingTheAppSettings`, `AReportBandAskingTheSettings` and their siblings now ask `IFeatureListService`; rename them to name the loose service they ask when those files are next touched (finding 2, non-blocking).
