# Feature: story-6363-s03e-package-access-licensing

Story #6363 · S03e Package Access and Licensing · Epic #6510 (Modules: package the bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03e row, "RBAC" cross-cutting default, D-6264-16, "Notes from S02")
- Brief: [`slices/slice-07-s03e-package-rbac-identity.md`](../epic-6264-modular-monolith/slices/slice-07-s03e-package-rbac-identity.md)
  (body and "Update after S01": retitled Access and Licensing, two folders; ratchet candidate 2)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) (Access and
  Licensing rows, "Every edge" rows Access → every context and Licensing → every context, ratchet candidate 2, glossary
  "Refinement, Reports, Access, Licensing", layout), [ADR-271](../../product/architecture/adr-271-ui-surfaces-follow-units-and-levels-and-settings-split-by-audience.md)
  (no UI surface here).
- Precedents, same shape: [S03a Tracker integration](../story-6359-s03a-package-tracker-integration/feature-delta.md)
  (D-2 amended, D-14, "D-1 outcome", "Feature B EXAMINE waived", "C1 lists as measured from the IL"; evolution record
  `docs/evolution/2026-11-17-story-6359-s03a-package-tracker-integration/evolution.md`),
  [S03d Promises](../story-6362-s03d-package-promises-deliveries/feature-delta.md) (in DELIVER while this was written),
  S03b and S03c (`docs/evolution/*6360*`, `*6361*`).

DISCUSS: covered at Epic level (D-6264-3, D-6264-4, D-6264-16); waves start at DESIGN.

DESIGN run 2026-11-17, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is recorded as
such. No choice changes what a user sees or is told, so no decision is held; the open questions at the end are
confirmations, not blockers. Nothing was built or run with `dotnet` (another delivery held the build); every count
is a text measurement on `main` at `abe4f1304`.

**Start condition.** This Story starts only after S03d is closed (features A, B and C, mutation, finalize, CI green).
S03d edits the same rule files (`ModuleBoundariesArchUnitTest`, the Work Items and Tracker integration boundaries
files) and writes a Promises rule (R-P7) that names Access and Licensing by their old namespaces. Every count below is
**re-measured at the Story's start (A2)**.

## Wave: DESIGN / [REF] Inherited

Linked, not copied.

- **ADR-270 Access row**: users, roles, unit memberships, IdP group mappings, API keys, embed sessions. "RBAC /
  Identity (minus licensing)". **Licensing row**: the licence, its verification, the free-tier limits; the three
  licence events (loaded, last 30 days, expired) are **target**, derived on read today.
- **ADR-270 "Every edge"**: Access → every context except Organisation set-up ("guards at each context's API adapter;
  owner-scoped listing through `IRbacAdministrationService`"); OIDC identity provider → Access (a small ACL: claims
  parsed into roles and groups); Licensing → every context except Organisation set-up; Licensing → Work Items and
  Organisation set-up → Licensing (derived). Access is downstream of Licensing (Access reads "is Premium").
- **Ratchet candidate 2** ("the refresh paths read the licence"): `TeamUpdater`, `PortfolioUpdater`, `ForecastUpdater`
  resolve `ILicenseService`. The S03c note: `ForecastUpdater`'s read is **in no list** today. This Story lists it; it
  does not turn the reads into licence events (behaviour change, target).
- **Catalogue RBAC default**: no change to authorization behaviour; `IRbacAdministrationService` stays the single
  entry for RBAC business logic; `RbacGuard` unchanged on every action; `useRbac()` stays the only source of UI
  gating; the RBAC ArchUnit rules are re-pointed in the same change as each move.
- **Brief notes**: highest-stakes move, run the full authorization and API-key suites; the emergency-admin config key
  and the auth-off "one subject" behaviour must not change.
- **Packaging rules (catalogue "Notes from S02") and today's lessons (S03a, S03d)**: rules first, proven with a made-up
  type under `Modules`; edges from the compiled IL (`IlDependencies`), inline lists compared both ways, never loosened
  (ADR-268); lists keyed by **full type name** where a short name is shared; exemptions **exact per type**, never per
  namespace or layer; one oracle file per slice, an approved oracle is never edited; moves apart from edits;
  `Program.cs` registrations unchanged (lines and order), only `using`s change; every EXAMINE expectation observable
  through an admitted probe, and a missing probe is added at DESIGN; mutation bar "no survivor or no-coverage mutant
  added in the moved code"; no review window (behaviour-free).

## Wave: DESIGN / [REF] DoR re-run (2026-11-17)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Access and Licensing sit in `Services/{Implementation,Interfaces}/{Auth,Authorization,Licensing}`; the only rule naming them is `ModuleBoundariesArchUnitTest.RbacIdentityPattern` (one "not empty" assertion), plus two neighbour patterns (E1). |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files and edges below. |
| 4 UAT | PASS (catalogue) | Brief AC, CI-observable; the acceptance designer writes the oracles in DELIVER. |
| 5 AC from UAT | PASS, two ACs corrected | (a) brief IN `Models/Auth/**`, `Models/Authorization/**`: only types passing S03a's four conditions move (D-2); the 9 EF entities and the wire types stay. (b) "No file of this context remains in `Services/Interfaces` or `Services/Implementation`": holds, with named exceptions outside those two folders only (repositories, seeders, Licensing test fixtures: D-4, D-5, D-9). |
| 6 Right-sized | PASS | Brief says M. **32 `.cs` files + 1 embedded resource** move (27 Access, 6 Licensing), **16 unit-test files** move (+2 conditional `Models` tests, D-2). DESIGN sizes it **M, about 7 days, 10 slices, 3 nwdes features**. |
| 7 Technical notes | PASS | Emergency-admin key and auth-off "one subject": configuration keys and scheme names are string constants on the moved types, not namespaces (E7); unchanged by a move, proved by the unchanged tests and `access-answers-compare` (A3). |
| 8 Dependencies | PASS | Hard: S01 Closed, S02 gate go-with-changes. **Added: S03d closed first** (shared rule files). Soft: none of S05 covers Access or Licensing persistence (D-4). |
| 9 Outcome KPIs | PASS | KPI 1 (files under `Services`), KPI 2 (contexts packaged), KPI 3 (`Models` files). |

Hypothesis in the brief ("RBAC can be packaged without touching a single authorization decision; disproved if a
guard's or service's public surface has to change"): **kept**. No public member changes; only namespaces do.

## Wave: DESIGN / [REF] Baseline re-count (2026-11-17, `main` at `abe4f1304`)

Counted with `git ls-files`; namespaces from each file's `namespace` line; edges from `using` lines and `git grep -w`
of type names. **Text search, `noisy`**: it misses same-namespace and fully qualified references and `var`-typed
returns, and matches names in comments. `IlDependencies` reads attribute types and constructor arguments (it sees
`[RbacGuard(RbacGuardRequirement.X)]`), but misses catch-clause types, events and named attribute arguments (S03a
evolution). The IL measurement is A2's (membership) and C1/C2's (lists).

| Measure | Today |
|---|---|
| Files under `Services` (KPI 1, all tracked) | **333** (S03d will take 13 before this Story starts; re-measure at start) |
| Loose files at the top of `Services/Interfaces` + `Services/Implementation` | 33 + 43 = **76** (this Story moves none of them; all its files sit in sub-folders) |
| `Models` files (KPI 3) | **263** |
| Contexts packaged (KPI 2) | **4 of 11** (`FlowMetrics`, `Forecasting`, `WorkItems`, `TrackerIntegration`); `Promises` in DELIVER |

### The Access set by ADR-270 (production, 27 files)

| Group | Files |
|---|---|
| RBAC administration | `Services/Implementation/Authorization/RbacAdministrationService` (1478 lines), `Services/Interfaces/Authorization/IRbacAdministrationService` (136 lines) |
| Feature-level decisions | `FeatureMoveAuthorization` + `IFeatureMoveAuthorization`, `FeatureReadability` (reaches `Models` only) |
| OIDC claims ACL | `GroupClaimParser` (BCL only), `OidcGroupSnapshotWriter` + interface |
| Users, API keys, embed sessions | `CurrentUserProfileService`, `ApiKeyService`, `ApiKeyPrincipalFactory`, `EmbedSessionTokenService` + 3 interfaces |
| Auth mode and configuration | `AuthModeResolver`, `AuthConfigurationValidator` + 2 interfaces |
| ASP.NET pipeline pieces | `ApiKeyAuthenticationHandler`, `DisabledAuthenticationHandler`, `SmartAuthSchemeSelector`, `BlockedModeFilter`, `RbacGuardAttribute`, `AccessMarkerAttribute`, `SignedInOnlyAttribute`, `CheckedInHandlerAttribute` |

Namespaces today: `Services.Implementation.Auth` 11, `Services.Interfaces.Auth` 6, `Services.Implementation.Authorization` 8,
`Services.Interfaces.Authorization` 2.

### The Licensing set (production, 5 files + 1 resource)

`Services/Implementation/Licensing/{LicenseService, LicenseVerifier, LicenseGuardAttribute, public_key.pem}`,
`Services/Interfaces/Licensing/{ILicenseService, ILicenseVerifier}`. `public_key.pem` is an `EmbeddedResource`
(`Lighthouse.Backend.csproj` lines 29 and 85); `LicenseVerifier` finds it with
`GetManifestResourceNames().First(x => x.EndsWith("public_key.pem"))` (lines 105-108), so the resource name's
namespace prefix does not matter (E3).

### Touching the sets but not moving (D-1 says why)

| Group | Files | Owner / reason |
|---|---|---|
| EF entities | `Models/Auth/{UserProfile, ApiKey, EmbedSessionToken}`, `Models/Authorization/{UserPermission, RbacGroupMapping, EveryoneSignedInSetting, ApiKeyPermission}`, `Models/LicenseInformation` (DbSets at `LighthouseAppContext` lines 52-80) | D-0, D-2 |
| Wire and request types | `Models/Auth/*` and `Models/Authorization/*` read by controllers (`ApiKeyInfo`, `RbacStatus`, `UserAuthorizationSummary`, `*Request`, `*Summary`, …) | D-2; S12c |
| Repositories | `ApiKeyRepository`, `ApiKeyPermissionRepository`, `EmbedSessionTokenRepository`, `LicenseInformationRepository`, `IApiKeyRepository`, `IEmbedSessionTokenRepository` | D-4 |
| Seeders | `Seeding/ApiKeyOwnerReconciliationSeeder`, `Seeding/EveryoneSignedInSeeder` | D-5 |
| Controllers, DTOs, helpers | `ApiKeyController`, `AuthController`, `AuthorizationController`, `LicenseController`, 3 Embed controllers, `API/DTO/{LicenseStatusDto, EmbedHandshakeResponse}`, `API/Helpers/RbacAdministrationServiceExtensions` | S12 (API band) |
| Composition root and config | `Program.cs`, `Startup/AuthPostureBanner`, `Configuration/EmbedConfiguration` | composition root |
| Refinement's own | `Refinement/VoterIdentityResolver` | Refinement (S03g) |
| Tracker integration's OAuth and auth strategies | `Modules/TrackerIntegration/**/Auth`, `**/OAuth`, `Models/OAuth/*` | already packaged (S03a); not Access (connection credentials, not people) |

### Tests of the sets (path count)

Unit tests that move (17): `Services/Implementation/Auth/*` 10 (`ApiKeyAuthenticationHandlerTest`,
`ApiKeyPrincipalFactoryTest`, `ApiKeyServiceTest`, `AuthConfigurationValidatorTest`, `AuthModeResolverTest`,
`BlockedModeFilterTest`, `CurrentUserProfileServiceTest`, `EmbedSessionTokenServiceTest`, `OidcGroupSnapshotWriterTest`,
`SmartAuthSchemeSelectorTest`), `Authorization/*` 3 (`RbacAdministrationServiceTest` 3684 lines,
`RbacGuardAttributeTest`, `CreateRightsAcceptanceTest`), `Licensing/*` 3 (`LicenseServiceTest`, `LicenseVerifierTest`,
`LicenseGuardAttributeTest`), plus `Models/Authorization/{EffectiveAccessTest, PermissionRuleTest}` only if A2 admits
their types (D-2). None references a host (0 matches for `TestWebApplicationFactory|WebApplicationFactory|IntegrationFixture|TestHost`);
none carries a live-connector category (`SmartAuthSchemeSelectorTest` carries `epic-5305-k8s-readiness` only).

**Stay** in `Tests/Services/Implementation/Licensing` (D-9): `TestLicenseData`, `invalid_license.json`,
`valid_expired_license.json`, the gitignored `valid_not_expired_license.json`, and `LicensingIntegrationTest`
(derives from `IntegrationTestBase`; line 301 of `TestHostUseGuardTest.HostUsersStillToMove`). Their path is named
in the test `.csproj` (lines 72, 75, 123), `TestLicenseData` (lines 5-9), `LicenseControllerTest` (lines 97, 136),
`.gitignore` line 427, `ci_backend.yml` line 69, `ci_verifyauth.yml` line 101 and `Scripts/examine/build-examine-commands`
(lines 368, 618, 1031).

Authorization and API-key behaviour is covered at the HTTP and service port by tests that **stay**: about 40 files under
`API/`, `API/Integration`, `API/Security`, `Acceptance/SystemWideAccess`, `Integration/Containers`, `Contracts/Authorization`
(`ApiKeyControllerHttpSmokeTests`, `JwtBearerAuthIntegrationTest`, `McpInboundAuthIntegrationTest`, the `S5`/`S8`-`S18`
security tests, `ViewerEmbedSessionJourneyTests`, `EndpointAuthorizationPolicyTest`, the `*AuthorizationTests`), plus
the live Keycloak Playwright runs `@auth`, `@rbac`, `@proxyauth` in `ci_verifyauth.yml`, which CI runs on **every** push
(`ci.yml` lines 151-161).

### Inbound (who calls the sets; text search, `noisy`)

| Caller | Reaches | ADR-270 reading |
|---|---|---|
| `API/*` (43 files `using` `Implementation.Authorization`, 18 `Interfaces.Authorization`, 14 `Implementation.Licensing`, 7 `Interfaces.Licensing`, 10 `Auth`) | guards, markers, `IRbacAdministrationService`, `ILicenseService`, user/API-key/embed services | API band: legal by namespace |
| `API/Helpers/{RbacAdministrationServiceExtensions, AdditionalFieldsHelper, WriteBackMappingsHelper}`, `API/DTO/DependenciesAsReadExtensions` | `IRbacAdministrationService`, `ILicenseService`, `FeatureReadability` | API band: legal |
| `Program.cs` | registrations (lines 1391-1392, 1450, 1461, 1465, 1656-1661), authentication schemes (826-941), `BlockedModeFilter` (310), `GroupClaimParser` (1012), constants (428, 1296-1298) | composition root |
| `Modules/WorkItems/Application/Dependencies/{DependencyDecision → ILicenseService, FeatureDependencyRendering → IRbacAdministrationService}` | | already in Work Items' outbound list by full name |
| `Modules/Forecasting/Application/ForecastFilterRuleService → ILicenseService` | | in Forecasting's outbound list (candidate 2) |
| `Modules/Forecasting/Adapters/Background/ForecastUpdater → ILicenseService` | | **in no list** (candidate 2) |
| `BackgroundServices/Update/{TeamUpdater, PortfolioUpdater}`, `BackgroundServices/UsageDataConsentPruningService` | `ILicenseService` | candidate 2; update code; usage data |
| `Services/Implementation/{FeatureListService, FeatureListActiveWork}`, `Services/Interfaces/IFeatureListService` | `IRbacAdministrationService`, `FeatureReadability` | owner-scoped listing (ADR-270: legal intent; listed by name, no namespace yet) |
| `Reports/{ReportCommands, ReportQueries}` | `ILicenseService`, `IRbacAdministrationService` (Reports depends on RBAC) | listed by name |
| `UsageData/{UsageDataConsentService, UsageDataInstanceProperties}` | `ILicenseService`, `Interfaces.Auth` | listed by name |
| `Refinement/VoterIdentityResolver` | `Implementation.Auth`, `Interfaces.Auth` | listed by name |
| `WriteBackTriggerService` | `ILicenseService` | write-back round (#6657) |
| `appsettings.Development.json` line 12 | log category `Lighthouse.Backend.Services.Implementation.Auth.DisabledAuthenticationHandler` | **configuration by namespace** (E4) |

### Outbound (what the sets reach beyond `Models`)

| From | To | Reading |
|---|---|---|
| `RbacAdministrationService`, `CurrentUserProfileService`, `OidcGroupSnapshotWriter` | `Data.LighthouseAppContext` (constructor) | Application → `Data`: forbidden by the module rule; **listed by exact type** (D-4) |
| `ApiKeyService`, `EmbedSessionTokenService` | `Services.Interfaces.Repositories` | platform persistence, listed |
| `EmbedSessionTokenService` | `Configuration.EmbedConfiguration` | configuration, listed |
| `AuthModeResolver` | `Services.Interfaces` (top level) | platform, listed (type measured in C1) |
| `AuthModeResolver`, `RbacAdministrationService` | `ILicenseService` | **Access → Licensing**: legal (Licensing is upstream) |
| `LicenseService`, `LicenseGuardAttribute` | `Services.Interfaces`, `Services.Interfaces.Repositories` (the guard counts Teams and Portfolios for the free tier) | platform, listed |
| Licensing → Access | nothing found | Licensing is a leaf toward Access |
| Either set → Work Items, Forecasting, Flow metrics, Tracker integration, Promises | nothing found | leaf rule, to confirm from the IL (C1, C2) |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. Which files form Access and which Licensing (brief IN, ADR-270 split)? → D-1.
2. Which `Models` types move? → D-2 (S03a's amended four conditions).
3. Target paths, layers, sub-folders; where do ASP.NET guards and handlers go? → D-3.
4. Repositories and the three direct `LighthouseAppContext` users? → D-4.
5. Seeders, controllers, config, the embedded licence key? → D-5, D-6.
6. Rule set, and how the existing RBAC rules are re-pointed. → D-8, Ratchet design.
7. Tests (and the Licensing fixtures), DI, logging config, CI selection, behaviour-free proof with probes, mutation. → D-9 to D-13.
8. Does the packaging touch `IRbacAdministrationService` or `useRbac()`, or the frontend? → Cross-cutting.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **S03d is closed** before the first slice starts.
- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, auth schemes, cookie names, configuration keys,
  `RbacGuard` on every action, UI unchanged; existing tests green unmodified apart from moved tests, `using`s and rule files.
- [ ] **`IRbacAdministrationService` public surface unchanged** (members, signatures); only its namespace changes.
- [ ] **Rules first**: every rule keyed on the sets' namespaces, types or paths widened before a type moves, proven with
  a made-up type under `Modules/Access` and `Modules/Licensing`.
- [ ] **Moves separate from edits**: `refactor(access): move …` / `refactor(licensing): move …` commits apart from edit commits.
- [ ] **Ratchet in the same change**; inline lists keyed by full type name; exemptions per exact type; no rule loosened.
- [ ] **No Access or Licensing file left in `Services/Interfaces|Implementation`** apart from the named exceptions (D-4, D-5).
- [ ] **Strings and config checked**: `appsettings.Development.json` log category, rule-list strings, `global::` fixture
  references, `csproj` resource paths, Stryker globs.
- [ ] **`Program.cs`**: only `using`s change; registration lines and order unchanged; every push of a move slice runs
  the full CI backend suite and every live suite (D-11); `verifyauth` runs on every push anyway.
- [ ] **Frontend untouched** (`useRbac` unchanged): no file under `Lighthouse.Frontend` or `Lighthouse.EndToEndTests` changes.
- [ ] **Architecture docs in the same Story** (finalize): `ARCHITECTURE.md` §4, ADR-270, brief ACs.
- [ ] **Cross-cutting re-check** answered (section below).

## Wave: DESIGN / [REF] Context-map placement (2026-11-17)

- **Contexts touched**: Access, packaged into `Modules/Access`; Licensing, packaged into `Modules/Licensing`.
- **Neighbours whose edge becomes visible** (no behaviour change): Access → Licensing (legal); every context → Access
  through guards (API band) and owner-scoped listing (`FeatureListService`, Work Items' `FeatureDependencyRendering`,
  Reports); every context → Licensing (candidate 2: the updaters, `ForecastFilterRuleService`, `DependencyDecision`,
  Reports, usage data, write-back); OIDC identity provider → Access (`GroupClaimParser`, `OidcGroupSnapshotWriter`);
  the platform lane (repositories, `LighthouseAppContext`, seeders).
- **New terms / concepts**: none. Folders `Modules/Access` and `Modules/Licensing` are the ADR-270 glossary names;
  code keeps `License*` (American spelling) and `Rbac*` identifiers.
- **Domain events**: none published or newly reacted to. The three licence events stay **target**.
- **UI surface**: none (ADR-271). **Improvement backlog check**: S03e is itself a #6510 item; N/A, stated.

## Wave: DESIGN / [REF] Evidence (2026-11-17, `main` at `abe4f1304`)

| # | Finding | Consequence |
|---|---|---|
| E1 | Four rule patterns key on the old namespaces: `ModuleBoundariesArchUnitTest.RbacIdentityPattern` (line 20-21, read by the "module is not empty" assertion at line 118), `TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing` (line 55, its leaf rule), `WorkItemsModuleBoundariesArchUnitTest.AccessAndLicensing` (line 52, outbound target), `ForecastingModuleBoundariesArchUnitTest.TheLicence` (line 59, Forecasting's outbound licence selector, used at lines 124-128). S03d's Promises R-P7 ("no Access or Licensing") will be a fifth. | A1 widens all of them to also match `Modules.Access` and `Modules.Licensing`. Without it, line 118 fails on an empty module after B, the two leaf rules silently stop seeing Access and Licensing, and Forecasting's licence selector stops matching `ILicenseService` (each a loosening). **A1 re-greps every `Architecture/*.cs` for `Auth`, `Authorization` and `Licensing` namespace regexes at its start** and widens what it finds; this list is not trusted as complete. |
| E2 | Full-name strings: Forecasting outbound list (line 112, `ILicenseService`), Work Items outbound list (lines 146-147), Tracker integration fixtures (lines 435-436, `global::` references at 612, 884, 897), Work Items fixtures (`global::` at 597, 750, 752, 363), Forecasting fixtures (832, 857-858), `AccessTablesSingleReaderArchUnitTest` (Models strings line 18, `typeof(RbacAdministrationService)`), `EveryActionDeclaresItsAccessArchitectureTest`, `ReportsModuleArchUnitTest`, `RefinementModuleArchUnitTest`, `LicenseGateSingleSourceArchUnitTest` (`using`s, `typeof`). | `typeof` and `using` follow the move; `global::` references and strings are edited in the B slice that moves the type (compile errors or both-way comparison failures otherwise). |
| E3 | `public_key.pem` is an embedded resource found by `EndsWith("public_key.pem")`. | Moving it needs two `csproj` path edits (lines 29, 85) in the move commit; the lookup still finds it. `LicenseVerifierTest` verifies real signatures and proves it. |
| E4 | `appsettings.Development.json` line 12 sets `Lighthouse.Backend.Services.Implementation.Auth.DisabledAuthenticationHandler` to `Warning`. No other appsettings or logging code names a moved type (search over `*.json`, `Startup/`). | Moving the handler without the key would make development logs noisier (a behaviour change, development only). B3 renames the key in the move commit and its oracle pins that the key equals the handler's full type name. |
| E5 | `IlDependencies` reads attribute types and constructor arguments (lines 73-103). | API → guard edges (`[RbacGuard(...)]`, `[LicenseGuard]`, markers) are visible to the rules; the C falsifiers include one attribute-only offender. |
| E6 | Three Access services take `LighthouseAppContext` directly; `AccessTablesSingleReaderArchUnitTest` pins `RbacAdministrationService` as the single reader of the RBAC tables. No S05 Story covers Access or Licensing persistence. | D-4: keep and list the three edges by exact type; the single-reader rule keeps its assertions (`typeof` follows the move). |
| E7 | Scheme names, cookie names, the API-key header and the emergency-admin and auth-off behaviour are constants or configuration read by `Program.cs` and the moved types (lines 826-941, 1296-1298); none is derived from a namespace. | Unchanged by a move; proved by the unchanged tests, `access-answers-compare` and the `verifyauth` runs. |
| E8 | `path-classifier.sh`: no regex names an Access or Licensing path; `SHARED_REGEX` names `Program.cs`, so every move slice selects all live suites. `ci_verifyauth.yml` runs on every push. No Stryker config or `test-time-budget.json` entry names the sets (the one Stryker JSON naming `RbacAdministrationService` is a closed Bug's under `docs/feature/bug-6575…`). | D-11. `BACKEND_REGEX` already classifies every `Lighthouse.Backend/` path as backend-only, so new classifier cases for the module paths would pass on arrival (no RED): **no classifier slice** (design review round 1). The selection is shown by the existing `test-selection-show` probe (X5). |
| E9 | No EXAMINE probe reads an auth, authorization, API-key or licence endpoint, and none shows the `Verify Authentication` job (`ci-live-suites-show` reads only the backend job and the five tracker categories). | A3 and A4 add the two missing probes before any file moves (lesson of S03a feature B). |
| E10 | The 9 entities are mapped from `Models`; none moves. | No migration; `ef-model-unchanged` proves it. |
| E11 | `Models/Auth` + `Models/Authorization` (36 files): types named only inside the set (text search) are `ApiKeyOwnerResolutionState`, `ApiKeyValidationResult`, `AuthConfigurationValidationResult`, `EmbedSessionTokenRedemption`, `EffectiveAccess`, `PermissionRule`, `RbacOperationResult`. Whether they are reached by a kernel type or returned on the wire through `var`: **not measured, A2 measures it**. | Candidates for D-2 only. |
| E12 | No frontend, E2E, client or website file names a moved type or folder. | Frontend untouched; no pnpm gate needed. |

## Wave: DESIGN / [REF] Decisions

All taken as recommended (AFK, 2026-11-17). Option tables are the record of what was weighed.

### D-0 No schema change — RECOMMENDED

No entity, `DbSet`, migration or table name changes. Entities stay in `Models` (as S03a to S03d). Checked by
`ef-model-unchanged` in EXAMINE.

### D-1 Membership — RECOMMENDED: the 27 Access files and the 6 Licensing files move; the rest stays with a named owner

"Move" = a pure-move commit to `Lighthouse.Backend.Modules.<Access|Licensing>.<Layer>[.<Sub>]`, namespace = folder.

| Candidate | Decision | Why |
|---|---|---|
| The 27 Access files (table above) | **Move** to `Modules/Access` | ADR-270 Access row. |
| The 5 Licensing files + `public_key.pem` | **Move** to `Modules/Licensing` | ADR-270 Licensing row; the key belongs with its verifier (D-6). |
| Entities and wire types in `Models/Auth`, `Models/Authorization`, `Models/LicenseInformation` | **Stay** | D-0, D-2. |
| Repositories (6 files) | **Stay** | D-4. |
| Seeders (2) | **Stay** | D-5. |
| Controllers, DTOs, `API/Helpers/RbacAdministrationServiceExtensions` | **Stay** in `API` | S12; guards sit in the API band (ADR-270). |
| `Startup/AuthPostureBanner`, `Configuration/EmbedConfiguration`, `Program.cs` | **Stay** | composition root and configuration. |
| `Refinement/VoterIdentityResolver` | **Stay** | Refinement's (S03g). |
| Tracker integration's auth strategies and OAuth | already packaged | connection credentials, not people (S03a). |

Totals: **32 `.cs` + 1 resource move** (all 33 from `Services`). KPI 1: **−33** against the tree at the time S03e
starts (333 today; about 320 after S03d). KPI 2: **+2** (7 of 11 once S03d has closed). KPI 3: 263 minus the admitted
`Models` types (0 to 7; A2). Loose top-level `Services` files: unchanged (76 minus S03d's).

### D-2 Context-owned `Models` types — RECOMMENDED: S03a's amended rule, four conditions

A `Models` type moves only if it is (a) not an EF entity, (b) not on the wire, (c) not reached by a kernel type and
(d) not used by any code that stays outside the module. A2 lists every candidate from the IL and fixes the answer in a
test, by full type name.

| Option | Verdict |
|---|---|
| **a (recommended)** Rule-based, four conditions | Candidates: the seven of E11. Stay: the 9 entities (a); every type a controller reads or returns (b); `AuthMode`, `AuthenticationConfiguration`, `AuthorizationConfiguration`, `UserRole`, `RbacGuardRequirement`, `AppliesTo`, `PermissionScopeType` (d: read outside). |
| b Move all `Models/Auth*` | Rejected: changes the EF model and the wire, waits for S12c. |
| c Move none | Rejected as a default; pure values used only inside are a free, honest move. |

The brief AC "context-owned `Models` types have moved" is rewritten at close-out as for S03a/S03d. If a moved type's
test exists (`EffectiveAccessTest`, `PermissionRuleTest`), it moves with it (D-9).

### D-3 Target paths, layers and sub-folders — RECOMMENDED

```
Modules/Access/
  Domain/         FeatureReadability, GroupClaimParser (+ admitted Models values); reach only the kernel and the BCL
  Application/    RBAC administration, Feature move authorization, current user profile, API keys, API-key principal
                  factory, embed sessions, OIDC group snapshot writer, auth mode resolver, auth configuration validator
                  (+ their 8 interfaces, ports next to their use case)
  Adapters/Http/  ApiKeyAuthenticationHandler, DisabledAuthenticationHandler, SmartAuthSchemeSelector, BlockedModeFilter,
                  RbacGuardAttribute, AccessMarkerAttribute, SignedInOnlyAttribute, CheckedInHandlerAttribute
Modules/Licensing/
  Application/    LicenseService, LicenseVerifier, ILicenseService, ILicenseVerifier, public_key.pem
  Adapters/Http/  LicenseGuardAttribute
```

| Option for the ASP.NET guards and handlers | Verdict |
|---|---|
| **a (recommended)** `Adapters/Http` in each module | They are driving adapters of the ASP.NET pipeline (filters, attributes, authentication handlers); ADR-270 puts the guards "at each context's API adapter". The API band may reach them by namespace (R-A5, R-L4). `Http` is a new sub-folder name beside S03a's `Adapters/Connectors` and `Adapters/OAuth`; recorded in ADR-270's layout at close-out. |
| b Keep them in `Application` | Rejected: puts `Microsoft.AspNetCore.Mvc` types in the use-case layer; the Application rule would need a framework exception. |
| c Move them to `API/` | Rejected: they are Access's rules of entry, not one controller's; S12 owns `API`. |

`ApiKeyPrincipalFactory` (reaches `Models.Auth` and the BCL) and `FeatureReadability` / `GroupClaimParser` are the
Domain/Application borderline: A2 measures each from the IL and fixes the layer (Domain if it reaches only the
kernel and the BCL, as S03a "D-1 outcome"). No Licensing `Domain`: the free-tier limits live in `LicenseService` and
`LicenseGuardAttribute` today; extracting them is an edit (target, with the licence events). Tests mirror the layout
under `Lighthouse.Backend.Tests/Modules/{Access,Licensing}/…`.

### D-4 Persistence and the direct `LighthouseAppContext` users — RECOMMENDED: stay, listed by exact type

| Option | Verdict |
|---|---|
| **a (recommended)** The 6 repositories stay in `Services/*/Repositories`; the three Application → `LighthouseAppContext` edges (`RbacAdministrationService`, `CurrentUserProfileService`, `OidcGroupSnapshotWriter`) are listed in R-A6 by exact type, owner proposed: Epic #6507 (repositories by role), no item filed | Keeps S03e a move. `AccessTablesSingleReaderArchUnitTest` keeps the single-reader rule unchanged. |
| b Introduce Access persistence ports now | Rejected: new ports and rewired consumers in a move Story, on the highest-stakes service. |
| c Move the repositories to `Adapters/Persistence` | Rejected now: `ApiKey` is also read by Tracker integration and usage data; a split is a repository-by-role decision. |

### D-5 Seeders, controllers and configuration — RECOMMENDED: stay

`ApiKeyOwnerReconciliationSeeder` and `EveryoneSignedInSeeder` stay in `Seeding` (startup lane with every other seeder;
S09a decides module start-up). Controllers and DTOs stay (S12). `EmbedConfiguration` stays in `Configuration`
(options bound by `Program.cs`), listed in R-A6.

### D-6 The licence public key — RECOMMENDED: moves with `LicenseVerifier`, `csproj` paths edited in the move commit

| Option | Verdict |
|---|---|
| **a (recommended)** `git mv` to `Modules/Licensing/Application/public_key.pem`; the two `csproj` lines follow in the same commit | The verifier's lookup is suffix-based (E3), so the resource is still found; `LicenseVerifierTest` proves it on real signatures. A path edit needed to compile is part of the move (like a `using`). |
| b Leave the key in `Services/Implementation/Licensing` | Rejected: leaves a folder with one non-code file; "no file left" would need an exception for a file that belongs to the module. |

### D-7 Access → Licensing — RECOMMENDED: legal by namespace

`AuthModeResolver` and `RbacAdministrationService` read `ILicenseService`. Licensing is upstream of every context
(ADR-270), so Access `Application` may reach Licensing `Application` by namespace, never its `Adapters`. Licensing
reaches no Access type (R-L2, expected empty).

### D-8 The rule set — RECOMMENDED: by namespace, through `ModuleRules`, two new boundaries files

`Architecture/AccessModuleBoundariesArchUnitTest.cs` and `Architecture/LicensingModuleBoundariesArchUnitTest.cs` on
`ModuleRules` with a `ModuleWording` each (REUSE; `AccessModule`, `LicensingModule` namespace constants). The old
`RbacIdentityPattern` keeps its meaning widened in A1 and is retired in C3 when nothing old matches (its one assertion
moves to the new files). The existing RBAC rules (`EveryActionDeclaresItsAccess`, `AccessTablesSingleReader`,
`LicenseGateSingleSource`, the Reports and Refinement guard rules) keep every assertion; they only follow the types.
Where a neighbour has no namespace yet (Organisation set-up, update code, Reports, usage data, Refinement), its types
are listed by full name.

### D-9 Tests move with production — RECOMMENDED (S03d D-9); the Licensing fixtures stay

Each B slice moves the unit tests of the files it moves, in a separate `refactor(…): move … tests` commit in the same
slice, to `Lighthouse.Backend.Tests/Modules/{Access,Licensing}/<same layer>`. Integration, acceptance-at-HTTP,
security and controller tests stay.

| Test class / file | Host references | In `HostUsersStillToMove` | Action |
|---|---|---|---|
| The 16 unit-test files of the sets (B1 3, B2 9 incl. `CreateRightsAcceptanceTest`, which starts no host, B3 4), +2 conditional `Models` tests in B2 (D-2) | 0 | no | Move only. The B crafter reads each against the test-writing skill and fixes any row it touches; none expected. |
| `TestLicenseData`, the 3 licence JSON fixtures (one gitignored), `LicensingIntegrationTest` | `LicensingIntegrationTest`: `IntegrationTestBase` | `LicensingIntegrationTest`: yes (line 301) | **Stay**, untouched. The fixture path is named in 10 places across `csproj`, `.gitignore`, two CI workflows, the examine builder and `CLAUDE.md`; moving it is churn with no gain. Named exceptions in C3's placement rule, **per exact file**. |
| Rule files of E1, E2 | n/a | no | A1 (patterns), B (strings, `global::`), C (new lists). |

### D-10 DI registration and configuration — RECOMMENDED

`Program.cs` lines and order stay; only `using`s change (registrations at 1391-1392, 1450, 1461, 1465, 1656-1661;
schemes 826-941). No `AddAccess`/`AddLicensing` extension (S09a). `appsettings.Development.json` line 12 is renamed in
B3's move commit (E4). Log categories of moved loggers change with the namespace; no other configuration names them.

### D-11 CI selection and live suites — RECOMMENDED: no classifier widening; pushes priced

Every B slice edits `Program.cs` `using`s, which `SHARED_REGEX` selects: each B push runs every live suite (Jira,
ADO, Linear, ServiceNow, GitHub) and `verifyauth` runs on every push anyway.

| Option | Live runs | Verdict |
|---|---|---|
| a push every slice | B1, B2, B3 = 3 | S03a/S03d maintainer choice; highest 429 risk on the shared Linear key |
| **b (recommended)** commits per slice; push B1-B3 together once all three are green locally | **1** | one full live run for the whole move; A and C slices touch no production or `Program.cs` file |

Push B1-B3 together **only with all three green locally** (fast loop and full filtered gate). B1's `csproj` edit is
in the backend project, and `Program.cs` `using`s are SHARED: each of B1-B3 alone already selects all live suites. No
classifier cases are added (E8: they would pass on arrival). Push cadence is open question 4.

### D-12 Behaviour-free proof and EXAMINE — RECOMMENDED: every expectation has an admitted probe

| EXAMINE expectation | Probe |
|---|---|
| X1 Same types in new namespaces; Access and Licensing rules green; old RBAC rules green | `architecture-rules Access`, `architecture-rules Licensing`, `architecture-rules Rbac` |
| X2 No schema change | `ef-model-unchanged` |
| X3 Same answers from auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys, licence status (before and after a licence import) | **new `access-answers-compare`** (A3) |
| X4 Authentication, RBAC and proxy-auth journeys pass live (Keycloak) on the candidate | **new `ci-auth-suites-show`** (A4) |
| X5 CI selects backend for module paths, all live suites for `Program.cs` | `test-selection-show` |
| X6 Every tracker's live suite still passes on the move push | `ci-live-suites-show` |
| X7 Full filtered backend suite green | `backend-tests` |

`access-answers-compare` (CREATE_NEW on the shared capture pieces of `build-examine-commands`): starts each revision
in-process with authentication off (one subject, E7), reads the listed endpoints, imports the premium licence fixture,
reads the licence status again, and compares the bodies; it fails when nothing was compared. `ci-auth-suites-show`
(CREATE_NEW beside `ci-live-suites-show`): for the newest finished CI run at or before a revision, prints the
`Verify Authentication` job conclusion and the conclusion of its `@auth`, `@rbac`, `@proxyauth` steps. Exact step
names: **not measured, A4 measures them**. Not observable by any probe: OIDC sign-in with a real identity provider
outside CI, and embed handshakes from a foreign host; they stay with the Keycloak runs (X4), the `S8`-`S15` security
tests and the unit tests. Both probes are declared in `.nwave/examine.json` before the first EXAMINE.

### D-13 Mutation bar — RECOMMENDED

Last, on frozen code: **minimum kill rate 80 %** of the moved code **and** "no survivor or no-coverage mutant added in
the moved code" (the stricter holds). Whole-file `mutate` entries for the 32 files; same filter on old and new paths;
acceptance suite excluded. `RbacAdministrationService` is large (1478 lines, 3684-line test): if the run is long,
compare survivors by file against the old path rather than re-running whole (memory: kill survivors, skip long runs).

## Wave: DESIGN / [REF] Slices

Every slice ≤ 1 day, its own oracle file, moves apart from edits. Three nwdes features, 10 slices. Size **M, about 7 days**.

| Id | nwdes feature | Delivers, in plain words | Oracle (new file; how it is RED first) | Production targets | Size |
|---|---|---|---|---|---|
| A1 | A `story-6363-s03e-a-rules-first` | Every rule that names Access or Licensing by namespace also sees `Modules/Access` and `Modules/Licensing`; a made-up Access or Licensing type there that reaches Work Items, Tracker integration, Promises or a repository is caught | `Architecture/AccessAndLicensingRulesSeeTheModulesArchUnitTest.cs` (reuses the `…RulesSeeTheModule` fixture approach). RED: the made-up offenders under `Modules.Access`/`Modules.Licensing` pass today's patterns (E1), including a made-up Forecasting `Application` type reaching a made-up `Modules.Licensing` type, which Forecasting's `TheLicence` selector does not see before A1. A1 starts by re-grepping `Architecture/*.cs` for old-namespace regexes | none (test-side patterns in 4-5 rule files) | 1 d |
| A2 | A | Membership lists of both modules, the layer of each type (Domain or Application) and the `Models` admissions (D-2) are measured from the IL and fixed | `Architecture/AccessAndLicensingMembershipArchUnitTest.cs`: **namespace-agnostic** (a listed type is found in its old or its new namespace), short-name lists with a full-name map only for shared short names, as S03a's membership test, so the approved oracle needs no edit after B (RED: fails when a listed type is missing or a name does not exist) | none | 0.5 d |
| A3 | A | An answers probe for Access and Licensing exists before any file moves and reports a changed answer | `Scripts/examine/test-access-answers-compare.sh` (**behavioural RED**): compare stage on two capture folders differing in one answer → `DIFFERENT` exit 1; identical → `IDENTICAL` exit 0. RED: the command does not exist | `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | 1 d |
| A4 | A | A CI probe shows the live authentication, RBAC and proxy-auth runs for a revision | `Scripts/examine/test-ci-auth-suites-show.sh` (RED: the command does not exist; GREEN on a recorded `gh` fixture with one failed step prints that step as failed) | `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | 0.5 d |
| B1 | B `story-6363-s03e-b-move` | Licensing lives in `Modules/Licensing/{Application, Adapters/Http}`, with its key; neighbour list strings follow | `Architecture/LicensingLayoutArchUnitTest.cs` (RED: types in old namespaces; the resource name ends with `public_key.pem` under the new folder) | 5 files + pem, `csproj` 2 lines, 3 unit-test files, `Program.cs` usings, list strings, `global::` fixtures and Forecasting's `TheLicence` users | 0.5 d |
| B2 | B | Access Domain and Application types live in `Modules/Access/{Domain, Application}` (+ admitted `Models`); `IRbacAdministrationService` surface unchanged | `Architecture/AccessApplicationLayoutArchUnitTest.cs` (RED: types in old namespaces; Domain reaches only kernel and BCL; the public members of `IRbacAdministrationService` equal a list fixed from today's IL) | 19 files (+ admitted `Models`) + 9 unit-test files (+2 conditional `Models` tests), `Program.cs` usings, list strings | 1 d |
| B3 | B | The guards, markers, filters and authentication handlers live in `Modules/Access/Adapters/Http`; the development log key follows | `Architecture/AccessHttpAdaptersLayoutArchUnitTest.cs`. RED is the **layout** part: types in old namespaces. Preservation pins in the same file (GREEN today, kept GREEN): `RbacGuardAttribute`'s constructors and public members, and the `AttributeUsage` of the four attributes, equal a list fixed from today's IL; `EveryActionDeclaresItsAccess`'s count of guarded actions equals today's number (measured at B3's start); the `Logging:LogLevel` key in `appsettings.Development.json` equals `typeof(DisabledAuthenticationHandler).FullName` (it does today; it fails after the move until the key is renamed). Substrate: the JSON file is read from the repository root, resolved by walking up from the test assembly's directory to the folder holding `Lighthouse.sln` (no copy-to-output entry) | 8 files + 4 unit-test files, `appsettings.Development.json`, 43+ API `using`s | 0.5 d |
| C1 | C `story-6363-s03e-c-edges-and-rules` | Access has its own module rules with inline lists measured from the IL | `Architecture/AccessModuleBoundariesArchUnitTest.cs` (RED: a made-up offender per list position, including one reaching only through an attribute; falsified with `ModuleRulesCatchANewOffender`) | rule files | 1 d |
| C2 | C | Licensing has its own module rules; candidate 2's readers are all listed, `ForecastUpdater` included | `Architecture/LicensingModuleBoundariesArchUnitTest.cs` (RED as C1) | rule files; neighbour lists re-keyed | 0.5 d |
| C3 | C | Every Access and Licensing unit test sits under `Tests/Modules/…`; no file of either context left in `Services/{Interfaces,Implementation}`; `RbacIdentityPattern` retired | `Architecture/AccessAndLicensingUnitTestPlacementTest.cs` + "no file left" path check, exceptions per exact file (D-9) (RED: a made-up file in an old folder is caught) | rule files | 0.5 d |

A2's and B's REDs are structural (types in the old place), the right RED for a layout move; behaviour is proved by the
unchanged tests, the Keycloak runs and EXAMINE. A3 and A4 have behavioural REDs.

Verification commands per slice (fast loop, then the full gate at the end of the slice):

```
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~<OracleClass>"
bash Scripts/examine/test-access-answers-compare.sh                       # A3
bash Scripts/examine/test-ci-auth-suites-show.sh                          # A4
dotnet build Lighthouse.Backend                                           # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

Why three features (A 4 slices, B 3, C 3): rules and probes only; pure moves; module rules and placement. B1 may run before or after B2
(Access → Licensing is legal both ways round). Finalize (not nwdes slices): mutation on frozen code (D-13); ADR-270,
`ARCHITECTURE.md` §4, brief ACs, catalogue; the Lighthouse finalize checklist.

## Wave: DESIGN / [REF] Ratchet design

All rules read the compiled IL (`IlDependencies`); every list inline, by full type name, compared both ways, never loosened.

### Rules first (A1): what is widened

| File | Keys on | Widening |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | `RbacIdentityPattern` (+ "not empty", line 118) | also `^Lighthouse\.Backend\.Modules\.(Access|Licensing)(\.|$)`; retired in C3 |
| `TrackerIntegrationModuleBoundariesArchUnitTest` | `RefinementReportsAccessAndLicensing` (leaf rule) | same alternative added |
| `WorkItemsModuleBoundariesArchUnitTest` | `AccessAndLicensing` (outbound target) | same alternative added |
| `ForecastingModuleBoundariesArchUnitTest` | `TheLicence` (line 59, outbound licence selector, lines 124-128) | also `^Lighthouse\.Backend\.Modules\.Licensing(\.|$)` |
| every other `Architecture/*.cs` | old-namespace regexes found by A1's re-grep at its start | same alternative added |
| S03d's Promises boundaries file | R-P7 (no Access or Licensing) | same alternative added (as written by S03d; measured at start) |
| Forecasting, Work Items, Tracker integration list strings and `global::` fixtures; `AccessTablesSingleReader` | full names | renamed in the B slice that moves the type |

### New rules (C1, C2), baselines measured from the IL in A2 and C1/C2

| # | Rule | Expected baseline (text search; the IL decides) |
|---|---|---|
| R-A1 | Each listed type lives in its layer; none left under `Services.*.(Auth|Authorization)` | D-3 lists |
| R-A2 | `Domain` reaches only `Models` and the BCL | empty |
| R-A3 | Outside → `Domain` from `API` (legal) plus the list | `FeatureListService` (`FeatureReadability`), `Program` (`GroupClaimParser`) |
| R-A4 | Outside → `Application` from `API`, the composition root (legal) plus the list | `FeatureDependencyRendering`, `FeatureListService`, `FeatureListActiveWork`, `IFeatureListService`, `ReportCommands`, `ReportQueries`, `UsageDataConsentService`, `UsageDataInstanceProperties`, `VoterIdentityResolver`; anything else only with a reason |
| R-A5 | Outside → `Adapters/Http` only from `API` and the composition root | empty |
| R-A6 | `Application` → no `API`, no module `Adapters`; Licensing `Application` legal; `Data` and platform only through the list | `LighthouseAppContext` ×3 (D-4), repository ports, `EmbedConfiguration`, the top-level `Services.Interfaces` type `AuthModeResolver` reads |
| R-A7 | Access reaches no other context (Work Items, Forecasting, Flow metrics, Tracker integration, Promises, update code, Refinement, Reports) | empty |
| R-L1 | Layout, none left under `Services.*.Licensing` | D-3 |
| R-L2 | Licensing reaches no Access type and no other context | empty |
| R-L3 | Outside → Licensing `Application` from `API`, the composition root, Access `Application` (legal) plus the list | `TeamUpdater`, `PortfolioUpdater`, `ForecastUpdater` (newly listed), `ForecastFilterRuleService`, `DependencyDecision`, `ReportCommands`, `ReportQueries`, `UsageDataConsentService`, `UsageDataInstanceProperties`, `UsageDataConsentPruningService`, `WriteBackTriggerService` (candidate 2, owners Epic 6474 and #6657) |
| R-L4 | Outside → Licensing `Adapters/Http` only from `API` | empty |
| R-L5 | Licensing `Application` → platform only through the list | repository ports, top-level `Services.Interfaces` (C2 measures which) |

Neighbour lists that name Access or Licensing by full name (Forecasting, Work Items) keep their entries under the new
names: they are those modules' outbound edges and only shrink with candidate 2.

## Wave: DESIGN / [REF] Component decomposition

| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `LicenseService`, `LicenseVerifier`, 2 interfaces, `public_key.pem` | `Services/*/Licensing` | `Licensing/Application/` | MOVE (+ 2 `csproj` path lines) | B1 |
| `LicenseGuardAttribute` | `Services/Implementation/Licensing` | `Licensing/Adapters/Http/` | MOVE | B1 |
| `FeatureReadability`, `GroupClaimParser` (+ admitted `Models`) | `Services/Implementation/Authorization`, `Models/*` | `Access/Domain/` | MOVE | B2 |
| RBAC administration, Feature move, user profile, API keys, principal factory, embed sessions, OIDC snapshot, auth mode, auth config (+ 8 interfaces) | `Services/*/Auth`, `Services/*/Authorization` | `Access/Application/` | MOVE | B2 |
| 3 authentication pieces, `BlockedModeFilter`, 4 attributes | `Services/Implementation/Auth`, `…/Authorization` | `Access/Adapters/Http/` | MOVE (+ 1 config key) | B3 |
| Rule files, scripts | tests, `Scripts` | same | EDIT / CREATE_NEW | A, C |
| Entities, wire types, repositories, seeders, controllers, config, Licensing fixtures | | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. HTTP: `AuthController`, `AuthorizationController`, `ApiKeyController`, `LicenseController`, the three Embed
controllers, and every controller's `RbacGuard` / marker / `LicenseGuard` (routes, guards, responses untouched). ASP.NET
pipeline: authentication schemes (disabled, API key, JWT bearer, cookie, embed cookie) and `BlockedModeFilter`, all
registered in `Program.cs`. In-process: other contexts call `IRbacAdministrationService` and `ILicenseService`.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IApiKeyRepository`, `IEmbedSessionTokenRepository`, repository base for `ApiKeyPermission`, `LicenseInformation` | platform repositories | stay (D-4) |
| `LighthouseAppContext` (RBAC tables, user profiles, group snapshots) | EF Core | stays; 3 listed edges (D-4) |
| OIDC identity provider | JWT bearer / cookie handlers in `Program.cs`, `GroupClaimParser` | unchanged |
| Licence public key | embedded resource | moved with the verifier (D-6) |

No new external call, adapter or package.

## Wave: DESIGN / [REF] Technology choices

None new: ArchUnitNET, `IlDependencies` over Mono.Cecil, NUnit, Stryker.NET, bash, `gh`, the existing EXAMINE capture pieces.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `ModuleRules` + `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | module rules and falsifiers | **REUSE**; add `AccessModule`, `LicensingModule` constants |
| `…RulesSeeTheModuleArchUnitTest` fixtures (Forecasting, Tracker integration, Promises) | made-up offender under a module | **REUSE** approach for A1 |
| `TrackerIntegrationMembershipArchUnitTest` / `TrackerIntegrationMembership` | membership by full name | **REUSE** shape for A2 |
| `ForecastingUnitTestPlacementTest`, S03a's "no file left" check | placement | **EXTEND** or sibling, decided in C3 |
| `EveryActionDeclaresItsAccessArchitectureTest`, `AccessTablesSingleReaderArchUnitTest`, `LicenseGateSingleSourceArchUnitTest` | RBAC and licence rules | **REUSE** unchanged (they follow `typeof`) |
| `forecast-answers-compare` capture pieces | in-process host, licence import | **REUSE**; `access-answers-compare` **CREATE_NEW** thin command (other endpoints, own "nothing compared" message) |
| `ci-live-suites-show` | CI run lookup by revision | **REUSE** its lookup; `ci-auth-suites-show` **CREATE_NEW** (different job and steps) |
| `ef-model-unchanged`, `architecture-rules`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
| CREATE_NEW | `Modules/Access`, `Modules/Licensing` trees, 11 oracle files, 2 probes | no existing owner of the modules' layout or rules |

Semantic duplicates checked: `RbacAdministrationServiceExtensions` (API helper) wraps the service and decides nothing
of its own (text read of its one method `ConfirmsSystemAdminAsync`); `FeatureMoveAuthorization` asks the service rather
than deciding roles itself. No second owner of an RBAC decision found; the catalogue rule "all RBAC logic through
`IRbacAdministrationService`" holds before and after.

## Wave: DESIGN / [REF] Impact closure

| Locator | Kind | Decision | Reason |
|---|---|---|---|
| 32 production files + `public_key.pem` | source | CHANGED (moved) | D-1, D-3, D-6 |
| `Lighthouse.Backend.csproj` lines 29, 85 | build | CHANGED (path) | D-6 |
| `Program.cs` | composition root | CHANGED (`using`s only) | D-10; forces full CI suite |
| `appsettings.Development.json` line 12 | config | CHANGED (key) | E4 |
| API controllers, `API/Helpers`, `API/DTO` (about 60 files) | consumers | CHANGED (`using`s) | |
| Work Items, Forecasting, update code, Reports, usage data, Refinement, write-back, Feature list consumers | consumers | CHANGED (`using`s) | |
| 16 unit-test files (+2 conditional `Models` tests); tests `using` the old namespaces (about 50 files, `noisy`) | tests | MIGRATED / CHANGED (`using`s) | D-9 |
| Licensing fixtures, `TestLicenseData`, `LicensingIntegrationTest`, `.gitignore`, `ci_backend.yml`, `ci_verifyauth.yml`, test `csproj` | tests / CI | UNCHANGED | D-9 |
| Rule files of E1, E2 | tests | CHANGED | A1, B, C |
| `Scripts/examine/build-examine-commands`, `.nwave/examine.json` | scripts | CHANGED | A3, A4 |
| `path-classifier.sh` regexes, `test-path-classifier.sh`, `test-time-budget.json`, Stryker configs | CI / timings / mutation | UNCHANGED_COMPATIBLE | E8; the mutation config is written at close-out |
| `IRbacAdministrationService` members, `RbacGuardAttribute` members, scheme and cookie names | public surface | UNCHANGED | brief AC; B2 pins `IRbacAdministrationService`'s members, B3 pins `RbacGuardAttribute`'s constructors and members, the four attributes' usage and the guarded-action count; scheme and cookie names are pinned by the unchanged tests and the Keycloak runs |
| EF snapshots, migrations | persistence | UNCHANGED | D-0 |
| Frontend (`useRbac`), E2E, Lighthouse-Clients, website | | UNCHANGED | no wire change (E12) |

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | Routes, DTOs, responses, schemes, cookies, configuration keys (one development log key renamed to keep its effect), UI unchanged; no schema change. |
| RBAC | **Touched in place, not in behaviour.** `IRbacAdministrationService` and `RbacGuardAttribute` move namespace; members and every guard on every action stay (B2 pins the interface members; B3 pins the guard's constructors and members, the attributes' usage and the number of guarded actions; `EveryActionDeclaresItsAccess` keeps its assertions). All RBAC logic still flows through `IRbacAdministrationService`. |
| `useRbac()` / frontend | **Not touched.** No file under `Lighthouse.Frontend` or `Lighthouse.EndToEndTests` moves or changes; no pnpm gate runs. |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing a user does changes (see DEVOPS). |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. Code keeps `License*` and `Rbac*`; the folders use the glossary names. |
| Emergency admin, auth-off "one subject" | Configuration and constants, not namespaces (E7); proved by unchanged tests, `access-answers-compare`, Keycloak runs. |
| `Program.cs` | `using`s only in B1-B3: those pushes run the full CI backend suite and every live suite (D-11). |
| Epic 6474 / #6657 | Candidate 2's readers stay and are listed; when they become licence-event reactions, R-L3 shrinks. |

## Wave: DESIGN / [REF] Residuality

Stressors from negated assumptions, walked against the finished design (walkthrough, not execution).

| Assumption negated | What happens | Preserved / forbidden | Residue, owner |
|---|---|---|---|
| Configuration never names a type | `appsettings.Development.json` does (E4) | Same development log level; B3 renames the key and pins it | other environments' config outside the repository: none known; operators' own `appsettings.*.json` overrides naming the handler would lose effect (development-only key; release notes not needed) |
| Embedded resources are found by full name | The verifier matches a suffix (E3) | Licence still verified | if someone changes the lookup to a full name later, `LicenseVerifierTest` fails |
| Rules keyed on old namespaces keep seeing the context | They would match nothing after B (E1), including Forecasting's `TheLicence` selector, which would stop seeing `ILicenseService` and silently drop `ForecastFilterRuleService`'s edge from its list check | A1 widens before any move, re-grepping all rule files first; "not empty" pin moves to the new files | a pattern the re-grep misses (built from string concatenation): the both-way list comparisons fail loudly on rename |
| The IL sees every edge | Attributes yes (E5); catch types, events, named attribute arguments no | Lists may miss such an edge (looser, never a false failure) | C1 falsifier with an attribute; residual named-argument blind spot recorded |
| A guard's behaviour depends on its namespace | ASP.NET resolves filters by type; the guard resolves `IRbacAdministrationService` by type | Same decisions | `RbacGuardAttributeTest`, `EveryActionDeclaresItsAccess`, Keycloak runs |
| Test fixtures are found by test namespace | They are found by output path (`csproj`) | Fixtures stay (D-9) | none |
| S03d is not closed when S03e starts | Shared rule files in flux | Nothing starts | start condition |
| Live suites are selected by path | Every B push is SHARED (`Program.cs`) | No live suite skipped | 429 risk on the shared Linear key (D-11) |
| The examine host runs with auth on | It runs auth-off; auth-on paths are not compared | X3 covers auth-off answers only | X4 (Keycloak) covers auth-on; OIDC outside CI: not observable, stated |
| EF keys on namespaces | Entities stay in `Models` | No migration | `ef-model-unchanged` |

Second set, not used to derive the design: a worktree without the premium licence fixture (2 Licensing failures,
environmental; the fixture path is unchanged); the Standalone build (namespace changes do not change the assembly);
an S12 controller split landing in parallel (it keeps the same guards and only adds `using`s).

## Wave: DESIGN / [REF] Formal verification

**Not applicable, with the reason.** The formal-verification skill was read. The claims are: (1) "the same types exist
in other namespaces and the edges are exactly the listed ones", decided exactly by the compiled IL with a made-up
offender per rule as falsifier; (2) "authorization behaviour is unchanged", decided by the existing suites, the
Keycloak runs and the answer comparison, since both sides run the same code; (3) "the public surface is unchanged",
decided by B2's member list from the IL. No state, ordering, concurrency, recovery or liveness claim changes (no
handler order, queue or schedule moves). A Quint or TLA+ model would restate the namespace map and could not fail.
**No checker (Quint, TLC, Agda) was probed or run** in this session, so tool availability is not established; if the
maintainer wants a model, the claim is **INDETERMINATE** until a checker is probed and run.

## Wave: DESIGN / [REF] Test substrate

- C# .NET 10, NUnit 4.6, Mono.Cecil via `IlDependencies`; bash and `gh` for the classifier tests and EXAMINE probes. No new package.
- Oracles are architecture tests (`Lighthouse.Backend.Tests/Architecture/*`), the classifier script and two probe tests;
  the claims are about types, edges, selection and observability, not behaviour. Behaviour stays covered by the
  existing HTTP/service-port tests and the Keycloak Playwright runs, which do not move.
- Timing: each new architecture test reads the cached IL module like the existing ones; inside the default budgets.
  Formal laws: none, so no executable projection is owed. No derogation requested.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature C.

- **ADR-270**: Access row "**Packaged** in `Modules/Access` by Story 6363 (date); entities, wire types, repositories and
  seeders stay"; Licensing row likewise; layout gains `Adapters/Http`; ratchet candidate 2 note: all licence readers
  listed in R-L3, `ForecastUpdater` included; new block "Edges measured by packaging Access and Licensing" (R-A1 to R-L5).
- **ARCHITECTURE.md** §4: Access and Licensing rows with `Modules.Access.*` / `Modules.Licensing.*` anchors; the diagram
  node "RBAC/Identity" split into two.
- **brief.md**: new section `## Application Architecture — story-6363 s03e package access and licensing`.
- **Epic catalogue**: Improvement log; KPI 1/2/3; slice-07 brief ACs and IN list corrected (D-2, DoR 5); note for
  #6507 (Access persistence ports, the three `LighthouseAppContext` edges), no item filed.
- **CLAUDE.md** "Architecture": the `IRbacAdministrationService` rule unchanged in wording; no edit needed.

## Wave: DESIGN / [REF] Architecture summary

**Shape in hexagonal terms.** Everything below is MOVED unless marked; no new port, no new external call.

```
 driving                        Modules/Access                                    Modules/Licensing
 ───────                        ─────────────────────────────────                 ──────────────────────────
 Controllers ── [RbacGuard] ──► Adapters/Http: guards, markers,                   Adapters/Http: LicenseGuard
  (unchanged)   [LicenseGuard]   BlockedModeFilter, auth handlers,
 ASP.NET auth ─────────────────► scheme selector
  pipeline                          │                                                    │
 Other contexts ───────────────► Application: IRbacAdministrationService ──legal──► Application: ILicenseService,
  (listed: Work Items, Reports,    (single RBAC entry), user profile,                 LicenseService, LicenseVerifier
   Feature list, usage data,       API keys, embed sessions, OIDC snapshot,          (+ public_key.pem)
   Refinement)                     auth mode / config, Feature move                       ▲
                                    │                                                    │ listed: updaters,
                                 Domain: FeatureReadability, GroupClaimParser            │ Forecasting, Work Items,
                                    │                                                    │ Reports, usage data, write-back
                                    ▼
                    Models kernel (RBAC entities, ApiKey, UserProfile, LicenseInformation: unchanged)
                    Platform: repositories, LighthouseAppContext (3 listed edges), seeders, config (unchanged)
```

- Driving adapters: controllers (unchanged), guards and authentication handlers (moved to `Adapters/Http`).
- Application services and ports: `IRbacAdministrationService` and the other Access services; `ILicenseService` (moved).
- Domain: Feature readability rule, OIDC group-claim parsing (moved; A2 confirms).
- Driven adapters: none of their own; the platform repositories and EF context stay.

**Patterns weighed.**

| Pattern | Verdict | Why |
|---|---|---|
| Two modules for two generic contexts (ADR-270) | chosen | permissions and licensing change for different reasons; Access → Licensing is one-way |
| Guards as driving adapters (`Adapters/Http`) | chosen | ASP.NET filters and handlers are entry adapters; keeps framework types out of `Application` |
| Single entry for RBAC (`IRbacAdministrationService`) | kept | project rule; only its namespace changes |
| Ports next to their use case | chosen | ADR-270 Decision 6 |
| Access persistence ports (repositories by role) | rejected for now | a rewiring of the highest-stakes service inside a move; Epic #6507 |
| Licence events (loaded, last 30 days, expired) instead of reads | rejected for now | behaviour change; target in ADR-270, candidate 2 |
| Licensing `Domain` for the free-tier limits | rejected for now | an extraction edit with no second consumer |
| Move the Licensing fixtures with the tests | rejected | 10 path references, gitignored secret file, CI generators; no gain |
| One "RBAC/Identity" module | rejected | ADR-270 split it |

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-17)

- Platform lens: N/A, because nothing deploys, configures or runs differently. Two CI concerns were measured: path
  selection (E8; already backend-only, shown by `test-selection-show`, no slice) and the live authentication runs (`verifyauth` on every push; one probe, A4).
  The embedded licence key is still embedded (D-6). No Docker, workflow or environment change.
- Usage-data event: **N/A, because nothing a user sees or does changes.**

## Wave: DESIGN / [REF] Open questions for the maintainer

None blocks DELIVER. Confirmations, each with the AFK default taken:

1. **Start order**: S03e starts only after S03d is closed (shared rule files). Default: yes.
2. **Licensing fixtures stay** in `Tests/Services/Implementation/Licensing` (with `TestLicenseData` and
   `LicensingIntegrationTest`), as named exceptions per file. Default: yes.
3. **Licence key moves** with its verifier, two `csproj` lines edited in the move commit (D-6). Default: move.
4. **Push cadence (D-11)**: push B1-B3 together (1 full live run, recommended) or per slice (3)? Default: together.
5. **Rigor profile**: `thorough` for features A and C (opus; reviewers and EXAMINE sonnet). For feature B, the
   recommended default is **`thorough` for B2 at least** (it moves `RbacAdministrationService`, 1478 lines, the single
   RBAC entry; a clean haiku review is not verification), `standard` for B1 and B3 (sonnet; reviewers and EXAMINE
   haiku). Your choice: B2 only, or all of B on `thorough`. Default taken: B2 `thorough`, B1 and B3 `standard`.
6. **Size**: M, about 7 days, 10 slices in 3 features (A 4, B 3, C 3). Default: agreed.
7. **Access persistence**: the three direct `LighthouseAppContext` edges are listed, owner proposed Epic #6507, **no ADO
   item filed**. Default: catalogue note only.
8. **Review window**: none (behaviour-free). UI sketch, loading and failure looks: N/A.

Not measured at DESIGN (each named with the slice that measures it): IL edges and every list above (text search,
`noisy`; A2, C1, C2); D-2 admissions and the Domain/Application split of `ApiKeyPrincipalFactory`,
`FeatureReadability`, `GroupClaimParser` (A2); S03d's final rule files and counts (A1, A2 at start); whether the compare
stage of the examine pieces can take capture folders, and which endpoints answer auth-off (A3); the step names of the
`Verify Authentication` job (A4); the exact list of test files that `using`
the old namespaces (B1-B3); the number of guarded actions `EveryActionDeclaresItsAccess` finds today (B3); the mutation run time on `RbacAdministrationService` (finalize). Nothing was built or run.

## Wave: DESIGN / [REF] Design review round 1 (2026-11-17)

Independent design review: NEEDS_REVISION, no blocker. Each finding was resolved in place (above), once:

| # | Severity | Finding | Resolution |
|---|---|---|---|
| 1 | major | A fourth old-namespace pattern was missing: `ForecastingModuleBoundariesArchUnitTest.TheLicence` (line 59, used at 124-128) | Added to E1, the A1 widening table, A1's oracle (a made-up Forecasting `Application` type reaching a made-up `Modules.Licensing` type is caught before B1) and Residuality. A1 re-greps every `Architecture/*.cs` for `Auth`/`Authorization`/`Licensing` regexes at its start instead of trusting E1. B1 edits its users. |
| 2 | major | A5 had no real RED: `BACKEND_REGEX` already classifies every `Lighthouse.Backend/` path as backend-only | A5 dropped. E8, D-11, the slice table, the verification commands, the impact closure and DEVOPS say why; the selection is shown by the existing `test-selection-show` probe (X5). Feature A is 4 slices; the Story is 10 slices, about 7 days. |
| 3 | minor | A2's key form | A2 is namespace-agnostic (old or new), short-name lists with a full-name map only for shared short names, as S03a's membership test, so the approved oracle needs no edit after B. |
| 4 | minor | The claim that B2 pins `RbacGuardAttribute` members, scheme and cookie names | B3's oracle pins `RbacGuardAttribute`'s constructors and members, the four attributes' `AttributeUsage`, and the number of guarded actions `EveryActionDeclaresItsAccess` finds (measured at B3's start). Scheme and cookie names: reworded to "pinned by the unchanged tests and the Keycloak runs". Impact closure and Cross-cutting updated. |
| 5 | minor | Test counts disagreed | One number: 16 unit-test files (+2 conditional `Models` tests). B1 3, B2 9 (+2 conditional), B3 4. DoR 6, D-9, impact closure and the B rows fixed. |
| 6 | minor | B3 reads `appsettings.Development.json` at test time without a named substrate | Substrate named: read from the repository root, found by walking up from the test assembly's directory to the folder holding `Lighthouse.sln`; no copy-to-output entry. The key already equals the full type name today, so B3's RED is the layout part; the key check is a preservation pin. |
| 7 | minor | Rigor for B | Open question 5: recommended default `thorough` for B2 at least (`RbacAdministrationService`, 1478 lines; a clean haiku review is not verification), `standard` for B1 and B3; still the maintainer's choice. |
| — | note | D-11 push cadence | Kept: push B1-B3 together only with all three green locally; B1's `csproj` edit and the `Program.cs` `using`s each select all live suites through `SHARED_REGEX`. |

Design review round 1 delta re-assessed: **APPROVED** (2026-11-17), no remaining finding. Note for A1: its re-grep also covers the Promises boundaries file S03d adds.

## Maintainer answers to the open questions (2026-11-17)

- 5 Rigor: **thorough for B2 only** (opus crafter and designer, sonnet reviewers); B1 and B3 standard; A and C thorough.
- 4 Push cadence: **B1-B3 pushed together**, one commit per slice, all three green locally first.
- 3 Licence key: **moves with its verifier** (two csproj lines in B1's move commit).
- 1, 6, 7, 8: agreed (start after S03d closes; size M, about 7 days, 10 slices; Access persistence a catalogue note only; no review window).
- 2 Licensing test data: **move it too, as its own slice after B1** (B4 in feature B, which grows to 4 slices; size about 7.5 days, 11 slices). Reason (maintainer): the test tree then matches the module one to one, which also prepares a later test assembly per module. Everything under `Tests/Services/Implementation/Licensing` goes to `Tests/Modules/Licensing`: `TestLicenseData`, the two committed licence files, `LicensingIntegrationTest`, and the generated `valid_not_expired_license.json`. Paths follow in `.gitignore`, `ci_backend.yml`, `ci_verifyauth.yml`, the test `csproj`, `LicenseControllerTest`, the examine builder and the `HostUsersStillToMove` entry if it names a path. The host moves the maintainer's local gitignored licence file with a plain file move (not git) when the slice lands, and updates the project memory note that names the path. C3 then has no per-file exception for the old Licensing folder. The CI path edits are checked on the push by `ci-auth-suites-show`.
