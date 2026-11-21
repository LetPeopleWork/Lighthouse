# Architecture brief: story\-6363\-s03e\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Placement (feature-delta D-1, D-2, D-3; lists of AccessAndLicensingMembership as measured in feature A): every listed type moves by git mv into Lighthouse.Backend/Modules, namespace equals folder. S1: Licensing/Application holds ILicenseService, ILicenseVerifier, LicenseService, LicenseVerifier and public_key.pem; Licensing/Adapters/Http holds LicenseGuardAttribute. S2: Access/Domain holds ApiKeyPrincipalFactory, EmbedHandshakeOutcome, FeatureReadability, GroupClaimParser and the four admitted Models types ApiKeyOwnerResolutionState, ApiKeyValidationResult, AuthConfigurationValidationResult, EffectiveAccess; Access/Application (flat) holds the 16 AccessApplication types, the eight interfaces next to their services. S3: Access/Adapters/Http holds the 8 AccessAdaptersHttp types. S4 moves the Licensing test data after S1. PermissionRule and every other type in Models/Auth, Models/Authorization and Models/LicenseInformation stays.
```
Governed slices: S1, S2, S3, S4

#### D2

```text
EmbedHandshakeOutcome is declared at the end of Services/Interfaces/Auth/IEmbedSessionTokenService.cs but measured as Domain (it reaches only the BCL), while the interface is Application. In S2 the interface file moves to Access/Application and the record declaration is cut, unchanged apart from its namespace, into its own new file Modules/Access/Domain/EmbedHandshakeOutcome.cs. No member, name or signature changes. Rejected: keeping both types in one file under Application (contradicts the measured layer and would need the approved membership lists edited) and a second namespace block in one file (namespace no longer equals folder).
```
Governed slices: S2

#### D3

```text
Behaviour-free (D-0, D-4, D-5, D-10): no class is created, merged or renamed and no signature changes; the only new file is the EmbedHandshakeOutcome split of S2. Entities, wire types, the six repositories, the two seeders, controllers, DTOs, API helpers, EmbedConfiguration and AuthPostureBanner stay. The three direct LighthouseAppContext readers keep their edge (listed in feature C). Program.cs lines, order and lifetimes stay; only usings and full type names change. Scheme names, cookie names, the API-key header, the emergency-admin key and the auth-off one-subject behaviour are constants or configuration, not namespaces, and stay; the only configuration change is the development log key in S3.
```
Governed slices: S1, S2, S3, S4

#### D4

```text
Tests move with production (D-9 as amended by the maintainer on 2026-11-17): each slice moves the unit tests of its moved types with git mv to Lighthouse.Backend.Tests/Modules/<Access or Licensing>/<same layer>, namespace equals folder. S1: LicenseServiceTest and LicenseVerifierTest to Licensing/Application, LicenseGuardAttributeTest to Licensing/Adapters/Http. S2: ApiKeyPrincipalFactoryTest and Models/Authorization/EffectiveAccessTest to Access/Domain; ApiKeyServiceTest, AuthConfigurationValidatorTest, AuthModeResolverTest, CurrentUserProfileServiceTest, EmbedSessionTokenServiceTest, OidcGroupSnapshotWriterTest, RbacAdministrationServiceTest and CreateRightsAcceptanceTest to Access/Application. S3: ApiKeyAuthenticationHandlerTest, BlockedModeFilterTest, SmartAuthSchemeSelectorTest and RbacGuardAttributeTest to Access/Adapters/Http. PermissionRuleTest stays. S4 moves TestLicenseData, invalid_license.json, valid_expired_license.json and LicensingIntegrationTest to Tests/Modules/Licensing. None of the moved unit tests starts a host; the improve-the-tests rule is waived for move-only and using-only edits; the HostUsersStillToMove line of LicensingIntegrationTest follows its new full name.
```
Governed slices: S1, S2, S3, S4

#### D5

```text
Rule lists follow the move and are never loosened (P5): full-name strings, typeof and global:: references to a moved type in ForecastingModuleBoundariesArchUnitTest, WorkItemsModuleBoundariesArchUnitTest, TrackerIntegrationModuleBoundariesArchUnitTest, PromisesModuleBoundariesArchUnitTest, AccessTablesSingleReaderArchUnitTest, EveryActionDeclaresItsAccessArchitectureTest, ReportsModuleArchUnitTest, RefinementModuleArchUnitTest and TestHostUseGuardTest.HostUsersStillToMove are rewritten in the slice that moves the type and stay compared both ways. The old-namespace alternatives feature A widened (RbacIdentityPattern and the neighbour patterns) keep both alternatives until feature C. AccessAndLicensingMembership and AccessAndLicensingRulesSeeTheModulesArchUnitTest are namespace-agnostic or declare their own fixture namespaces and stay unedited. One compile-forced exception: AccessAndLicensingMembershipArchUnitTest names IRbacAdministrationService by its old full name in a made-up Domain fixture (line 695) and in an expected failure text (line 270); S2 rewrites those two full names to Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService and nothing else in that file.
```
Governed slices: S1, S2, S3

#### D6

```text
Oracles (one new file per slice, D-12): S1-S3 are IL architecture tests over the compiled Lighthouse.Backend assembly through ModuleRules.TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary, reading the type names from AccessAndLicensingMembership so a list name with no type fails (never vacuous), modelled on PromisesLayoutArchUnitTest and ForecastingDomainLayoutArchUnitTest; nested types are compared at top level. The preservation pins (IRbacAdministrationService public members in S2; RbacGuardAttribute constructors and public members, the AttributeUsage of RbacGuardAttribute, AccessMarkerAttribute, SignedInOnlyAttribute and CheckedInHandlerAttribute, the guarded-action count and the development log key in S3) are GREEN before the move and stay GREEN; the layout part is the RED. S4 is a placement test over the test assembly, its output folder and the repository files that name the licence fixture paths. Repository files are read from the folder holding Lighthouse.sln, found by walking up from the test assembly directory. No oracle starts a host.
```
Governed slices: S1, S2, S3, S4

#### D7

```text
Claim-to-checker: namespace placement, Domain reach, member lists, attribute usage, the guarded-action count and fixture paths are finite facts read from the compiled IL, the test assembly and repository files, checked by the four oracles (Finite laws) and by the EXAMINE probes architecture-rules Access, Licensing and Rbac, ef-model-unchanged, access-answers-compare, ci-auth-suites-show, test-selection-show, ci-live-suites-show and backend-tests. No Quint, TLA+ or Agda model is selected: no state machine, ordering, concurrency, recovery or proof obligation changes (no handler, queue, schedule or registration order moves), and a model would restate the namespace map. Liveness is not applicable: there is no progress property. Type strength: native C# namespaces, no new types; a misplaced type is representable and is excluded by the IL rules, not by construction.
```
Governed slices: S1, S2, S3, S4

#### D8

```text
Push, cost and rigor (D-11, maintainer 2026-11-17): one commit per slice, made by the root; B1-B4 are pushed together once all four are green locally (fast loop and full filtered gate), so one full live run covers the move (Program.cs and the backend csproj are shared paths that select every live suite; verifyauth runs on every push and shows the S4 CI path edits). Rigor: S2 thorough (opus crafter and acceptance designer, sonnet reviewers); S1, S3 and S4 standard (sonnet, haiku reviewers); EXAMINE on the reviewer model. EXAMINE expectations name only what an admitted probe shows. Mutation and the finalize checklist run after feature C.
```
Governed slices: S1, S2, S3, S4

### Constraints

#### K1

```text
No schema, route, DTO, DI lifetime, registration order, scheme, cookie or configuration-key change apart from the development log key; the EF model stays the same (ef-model-unchanged); IRbacAdministrationService and RbacGuardAttribute keep every public member; every existing test stays green, unmodified apart from moves, usings, full-name strings and fixture paths.
```
Governed slices: S1, S2, S3, S4

#### K2

```text
Namespace equals folder for every moved file. Approved oracle files of this feature are never edited, not even a using line; a crafter target is never an oracle support. One commit per slice; GREEN and every review run on a clean committed tree.
```
Governed slices: S1, S2, S3, S4

#### K3

```text
Input bounds: no stored or validated input changes and none is introduced; the licence JSON fixtures keep their bytes. Out of scope: module rules and the unit-test placement rule (feature C), repositories, seeders, entities, wire types, Access persistence ports, licence events.
```
Governed slices: S1, S2, S3, S4

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Producer and consumer ship in one deployable and change in the same commit; the move changes no wire or persisted shape, so the unchanged existing tests, access-answers-compare and the layout oracles cover it
```

### Boundaries

```text
ArchUnit layout oracles reading the compiled Lighthouse.Backend assembly (IlDependencies and ModuleRules), the test assembly and its output folder; existing HTTP ports, ASP.NET authentication schemes and the application-service ports IRbacAdministrationService and ILicenseService unchanged
```

#### Driven ports

```text
none changed (repositories, LighthouseAppContext and the embedded licence key stay behind their existing access paths)
```

```text
Access Domain reaches only the shared kernel and the base library; Access Application reaches what it reaches today (Domain, kernel, Models entities, repositories, LighthouseAppContext, EmbedConfiguration, Licensing Application); Access and Licensing Adapters/Http are reached from API and the composition root; Licensing reaches no Access type; no edge is added or removed, only namespaces change
```

#### Failures

None.

### Observations

```text
Layout verdict: the full names of the moved types per layer namespace and the offending type named by a failing rule; the public member list of IRbacAdministrationService; RbacGuardAttribute constructors and members, the AttributeUsage of four attributes, the guarded-action count; the development log key; the licence key resource name; the licence fixture paths
```

IReadOnlyList of full type names or member signatures with a verdict naming each offender

```text
Behaviour: a placement check cannot see a changed authorization answer, licence status or log level at runtime; the unchanged tests, the Keycloak runs and the EXAMINE probes carry that. Nested types are compared at top level
```

### Equality

layout observation

```text
Two layouts are equal when every listed top-level type has the same simple name and lives in the same designed namespace, and the pinned member lists, attribute usages, guarded-action count, log key and fixture paths are the same values
```

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

#### C1

```text
After the move every Access and Licensing type measured in feature A lives in its designed layer, Access Domain reaches only the kernel, and the pinned public surfaces are unchanged
```

```text
A type left behind or a Domain edge hides Access or Licensing from the namespace rules of feature C; a changed member breaks the single RBAC entry
```

the four layout oracles of S1\-S4

```text
The compiled assembly after git mv plus namespace edits
```

git mv plus namespace line\, then using\, full\-name and path edits

ModuleRules\.TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary

```text
Rule verdict naming each offender by full type name or member
```

```text
Reflection, configuration strings and string-named types are not seen by IL rules; the log key is pinned by S3, DI and Program.cs full names are covered by compilation and the unchanged suite
```

### States

Admitted

```text
Moved types under Modules.Access.Domain, Modules.Access.Application, Modules.Access.Adapters.Http, Modules.Licensing.Application and Modules.Licensing.Adapters.Http; the licence key embedded as Modules.Licensing.Application.public_key.pem; the Licensing test data under Tests/Modules/Licensing; unmoved neighbours (entities, wire types, repositories, seeders, controllers) in their old namespaces
```

crafter

Excluded

```text
A moved type left in an old namespace, a Domain type reaching anything beyond the kernel and base library, a list name matching no type, a changed IRbacAdministrationService or RbacGuardAttribute member, a guarded action lost, a log key naming a type that no longer exists, a fixture path naming the old folder
```

layout oracles

Unknown

```text
OIDC sign-in with a real identity provider outside CI and embed handshakes from a foreign host are not observable locally; they stay with the Keycloak runs, the security tests and the unit tests
```

maintainer

### Models

None.

### Closure

```text
Access and Licensing layout move
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states are failed by the IL and path rules over the whole governed scope; the finite oracles are the checker
```

##### Evidence

None.

```text
Access and Licensing layout move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Admitted input is the existing types, the key and the test data; the constructor is git mv plus namespace line, reaching the oracles through the real build
```

##### Evidence

None.

```text
Access and Licensing layout move
```

PublicReachability

Pass

DesignedNotBuilt

```text
The oracles read the compiled assembly and repository files; HTTP answers are read by access-answers-compare and the Keycloak runs through ci-auth-suites-show
```

##### Evidence

None.

```text
Access and Licensing layout move
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no queue, schedule or handler behaviour changes, so there is no progress property to check
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
IL rules miss catch-clause, event and named attribute-argument references
```

```text
Domain reaches only the kernel on every path the IL shows
```

```text
A reference kind the IL cannot see exists
```

```text
A visible edge from Access Domain beyond the kernel
```

```text
Unseen reference kinds
```

acceptance designer and reviewer

#### Y2

SinglePointOfFailure

```text
Program.cs is a shared path and the composition root of every authentication scheme and RBAC registration
```

```text
Every scheme, filter, route and DI registration and their order
```

```text
A full CI run with every live suite on the one push of B1-B4
```

```text
A changed registration, scheme or lifetime
```

```text
Linear key rate limit (429) on the shared live run
```

host

#### Y3

Stressor

```text
Assumption negated: configuration never names a type. appsettings.Development.json names DisabledAuthenticationHandler by full name
```

```text
The development log level of the disabled authentication handler
```

```text
The key text changes with the namespace
```

```text
A key that names a type no longer in the assembly
```

```text
Operator appsettings overrides outside the repository that name the old handler lose effect (development-only key)
```

maintainer

#### Y4

Stressor

```text
Assumption negated: test fixtures are found by namespace. They are found by output path, and the gitignored premium fixture is generated by CI and copied by hand locally
```

```text
The licensing tests and the live authentication runs find the licence files
```

```text
Every path naming the fixture changes in S4
```

```text
A CI generator or examine builder writing to the old folder
```

```text
A worktree or machine without the moved local premium file sees the 2 known environmental Licensing failures; the host moves the file and updates the memory note
```

host

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Licensing/ILicenseService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseVerifier\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Licensing/ILicenseVerifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseVerifier\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseVerifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/public\_key\.pem

Create

```text
git mv; bytes unchanged, embedded beside its verifier, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/public_key.pem
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttribute\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseGuardAttribute.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs

Create

```text
git mv; namespace line plus usings (TestLicenseData keeps its old namespace until S4), from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseVerifierTest\.cs

Create

```text
git mv; namespace line plus usings (TestLicenseData keeps its old namespace until S4), from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseVerifierTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttributeTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseGuardAttributeTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj

Extend

```text
the two public_key.pem lines (None Remove and EmbeddedResource Include) name Modules\Licensing\Application\public_key.pem
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/AdditionalFieldsHelperTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/WriteBackMappingsHelperTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedStalenessThresholdValidationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldEnforcementTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OrderedParentSources\/ParentSourcesSettingsApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioConcurrencyTokenIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioOwningTeamSaveIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioStalenessThresholdSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamConcurrencyTokenIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamStalenessThresholdSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WriteBackMappingForNewAdditionalFieldIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterRuleServiceIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/UsageDataConsentPruningServiceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataConsentServiceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataInstancePropertiesTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForwardedHeaders\/ForwardedHeadersOidcTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/AdditionalFieldsHelper\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/WriteBackMappingsHelper\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyDecision\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/UsageDataConsentPruningService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportQueries\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataConsentService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataInstanceProperties\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~LicensingLayoutArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run LicensingLayoutArchUnitTest over the compiled Lighthouse.Backend assembly: look each name of AccessAndLicensingMembership.LicensingApplication and LicensingAdaptersHttp up and read its namespace; read the manifest resource names of the assembly
```

```text
ILicenseService, ILicenseVerifier, LicenseService and LicenseVerifier live in Lighthouse.Backend.Modules.Licensing.Application; LicenseGuardAttribute lives in Lighthouse.Backend.Modules.Licensing.Adapters.Http; none is left under Services.Interfaces.Licensing or Services.Implementation.Licensing; exactly one resource ends with public_key.pem and its name is Lighthouse.Backend.Modules.Licensing.Application.public_key.pem; LicenseVerifierTest still verifies real signatures and the existing suite stays green
```

```text
A Licensing type left in its old namespace, a list name matching no type (vacuous list), the key left under Services.Implementation.Licensing or embedded twice: the test fails naming the type or resource. Today every type and the key sit in the old place, so the test is RED
```

##### Promises

P1

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Licensing/ILicenseService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseVerifier\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Licensing/ILicenseVerifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseVerifier\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseVerifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/public\_key\.pem\:1

Migrated

```text
git mv; bytes unchanged, embedded beside its verifier, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/public_key.pem; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttribute\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseGuardAttribute.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings (TestLicenseData keeps its old namespace until S4), from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseServiceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseVerifierTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings (TestLicenseData keeps its old namespace until S4), from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseVerifierTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttributeTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicenseGuardAttributeTest.cs; no member changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:1

Changed

```text
the two public_key.pem lines (None Remove and EmbeddedResource Include) name Modules\Licensing\Application\public_key.pem; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/AdditionalFieldsHelperTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/WriteBackMappingsHelperTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedStalenessThresholdValidationTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldEnforcementTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OrderedParentSources\/ParentSourcesSettingsApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioOwningTeamSaveIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioStalenessThresholdSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamStalenessThresholdSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WriteBackMappingForNewAdditionalFieldIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterRuleServiceIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/UsageDataConsentPruningServiceTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataConsentServiceTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataInstancePropertiesTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForwardedHeaders\/ForwardedHeadersOidcTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/AdditionalFieldsHelper\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/WriteBackMappingsHelper\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyDecision\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/UsageDataConsentPruningService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportQueries\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataConsentService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataInstanceProperties\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

##### Agreement

Namespaces of the Licensing Application and Adapters\/Http types

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs\:1

Producer

Migrated

```text
Producer of the moved types; names change, members do not
```

Namespaces of the Licensing Application and Adapters\/Http types

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Consumer

Migrated

```text
The composition root registers the moved types under their new namespaces; registrations and lifetimes unchanged
```

Embedded licence key resource

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:85

Producer

Migrated

```text
The EmbeddedResource item names the new path, so the manifest name gains the Modules.Licensing.Application prefix
```

Embedded licence key resource

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseVerifier\.cs\:106

Consumer

UnchangedCompatible

```text
The verifier picks the resource whose name ends with public_key.pem, so the prefix change is invisible to it
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move (git mv plus namespace line, then usings, full names and paths); no further preserving move makes it easier
```

contract_tests

```text
No wire or persisted shape changes and every caller changes in the same commit inside one deployable; the layout oracle, the unchanged suite and access-answers-compare cover the consumers
```

reuse

```text
Every target is an existing owner relocated by git mv with a namespace line, or an existing consumer edited by using lines, full names or csproj paths; no new type or responsibility is created. Candidates consulted and reused unchanged: ModuleRules.TypesLiveInTheirLayer, AccessAndLicensingMembership (Licensing lists), PromisesLayoutArchUnitTest (oracle shape), LicenseService, LicenseVerifier and its suffix lookup of public_key.pem, LicenseGuardAttribute. Only the oracle LicensingLayoutArchUnitTest is new: no existing oracle states the Licensing layout. Future paths do not resolve as locators until the move
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
Every Licensing type measured in AccessAndLicensingMembership lives in its designed Licensing layer namespace, every list name resolves to a type, and the licence key is embedded once from Modules/Licensing/Application
```

Invariant

Application

LicensingLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
the five Licensing types in their old namespaces fail the layer rule naming each
```

```text
a list name with no matching type fails as missing
```

```text
the key resource named under Services.Implementation.Licensing fails naming the resource
```

```text
Closed finite set of named types, members, keys and paths read from the compiled assembly and repository files; a native example table compared both ways is exact, a generative property adds nothing
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
Licensing types live in Modules/Licensing Application and Adapters/Http by namespace and the licence key is embedded from the new folder; lists compared both ways and never vacuous
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~LicensingLayoutArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/ApiKeyService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthConfigurationValidator\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/AuthConfigurationValidator.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/AuthModeResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/CurrentUserProfileService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/CurrentUserProfileService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/EmbedSessionTokenService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/EmbedSessionTokenService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/OidcGroupSnapshotWriter\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/OidcGroupSnapshotWriter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/FeatureMoveAuthorization\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/FeatureMoveAuthorization.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IApiKeyService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IApiKeyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthConfigurationValidator\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IAuthConfigurationValidator.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthModeResolver\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IAuthModeResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/ICurrentUserProfileService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/ICurrentUserProfileService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IEmbedSessionTokenService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IEmbedSessionTokenService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IOidcGroupSnapshotWriter\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IOidcGroupSnapshotWriter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IFeatureMoveAuthorization\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Authorization/IFeatureMoveAuthorization.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IRbacAdministrationService\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Authorization/IRbacAdministrationService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyPrincipalFactory\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyPrincipalFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/FeatureReadability\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/FeatureReadability.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/GroupClaimParser\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/GroupClaimParser.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyOwnerResolutionState\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/ApiKeyOwnerResolutionState.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyValidationResult\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/ApiKeyValidationResult.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/AuthConfigurationValidationResult\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/AuthConfigurationValidationResult.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EffectiveAccess\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Authorization/EffectiveAccess.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EmbedHandshakeOutcome\.cs

Create

```text
new file holding only the EmbedHandshakeOutcome record cut unchanged from the end of IEmbedSessionTokenService.cs, namespace Lighthouse.Backend.Modules.Access.Domain (decision on the split)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/ApiKeyServiceTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthConfigurationValidatorTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/AuthConfigurationValidatorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/AuthModeResolverTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/CurrentUserProfileServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/EmbedSessionTokenServiceTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/EmbedSessionTokenServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/OidcGroupSnapshotWriterTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/OidcGroupSnapshotWriterTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/RbacAdministrationServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Domain\/ApiKeyPrincipalFactoryTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyPrincipalFactoryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Domain\/EffectiveAccessTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Models/Authorization/EffectiveAccessTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

Extend

```text
only the two old full names of IRbacAdministrationService (made-up Domain fixture parameter and expected failure text) become Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService; compile-forced, lists untouched
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ApiKeyControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthorizationControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/CreateRightsControllerGuardTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/JwtBearerStandaloneGateTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RbacGroupMappingConcurrencyTokenIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04StopARefreshSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice06TheWarningsWithoutTheLogSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHostPerFixtureAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportOwnerWriteAccessTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S4\_DeliveriesDeleteGuardInversionTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SuggestionsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WizardsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataInstancePropertiesTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestConfiguration\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestWebApplicationFactory\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DependenciesAsReadExtensions\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WizardsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/FeatureDependencyRendering\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/VoterIdentityResolver\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataInstanceProperties\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureListService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessApplicationLayoutArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run AccessApplicationLayoutArchUnitTest over the compiled Lighthouse.Backend assembly: look each name of AccessAndLicensingMembership.AccessDomain, ModelsTypesThatMoveIntoAccess and AccessApplication up and read its namespace; run the shared Domain-reaches-only-the-kernel rule over Modules.Access.Domain; read the public members of IRbacAdministrationService (name, return type, parameter types) from the IL
```

```text
ApiKeyPrincipalFactory, EmbedHandshakeOutcome, FeatureReadability, GroupClaimParser, ApiKeyOwnerResolutionState, ApiKeyValidationResult, AuthConfigurationValidationResult and EffectiveAccess live in Lighthouse.Backend.Modules.Access.Domain; the 16 AccessApplication types live in Lighthouse.Backend.Modules.Access.Application; none is left under Services.*.Auth, Services.*.Authorization, Models.Auth or Models.Authorization; Access Domain reaches only the kernel and the base library; the public members of IRbacAdministrationService equal the list fixed from the IL before the move; the existing suite stays green
```

```text
A type left in its old namespace, a list name matching no type, a made-up type under a fixture namespace of Modules.Access.Domain that reaches a repository, or an added, removed or changed IRbacAdministrationService member: the test fails naming it. The member pin is GREEN before the move; the layout part is RED today because every type sits in the old place
```

##### Promises

P2

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/ApiKeyService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthConfigurationValidator\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/AuthConfigurationValidator.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/AuthModeResolver.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/CurrentUserProfileService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/CurrentUserProfileService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/EmbedSessionTokenService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/EmbedSessionTokenService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/OidcGroupSnapshotWriter\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/OidcGroupSnapshotWriter.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/FeatureMoveAuthorization\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/FeatureMoveAuthorization.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IApiKeyService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IApiKeyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthConfigurationValidator\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IAuthConfigurationValidator.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthModeResolver\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IAuthModeResolver.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/ICurrentUserProfileService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/ICurrentUserProfileService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IEmbedSessionTokenService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IEmbedSessionTokenService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IOidcGroupSnapshotWriter\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IOidcGroupSnapshotWriter.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IFeatureMoveAuthorization\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Authorization/IFeatureMoveAuthorization.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IRbacAdministrationService\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Authorization/IRbacAdministrationService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyPrincipalFactory\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyPrincipalFactory.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/FeatureReadability\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/FeatureReadability.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/GroupClaimParser\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/GroupClaimParser.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyOwnerResolutionState\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/ApiKeyOwnerResolutionState.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/ApiKeyValidationResult\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/ApiKeyValidationResult.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/AuthConfigurationValidationResult\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Auth/AuthConfigurationValidationResult.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EffectiveAccess\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Models/Authorization/EffectiveAccess.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EmbedHandshakeOutcome\.cs\:1

Migrated

```text
new file holding only the EmbedHandshakeOutcome record cut unchanged from the end of IEmbedSessionTokenService.cs, namespace Lighthouse.Backend.Modules.Access.Domain (decision on the split); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/ApiKeyServiceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyServiceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthConfigurationValidatorTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/AuthConfigurationValidatorTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/AuthModeResolverTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/CurrentUserProfileServiceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/EmbedSessionTokenServiceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/EmbedSessionTokenServiceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/OidcGroupSnapshotWriterTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/OidcGroupSnapshotWriterTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/RbacAdministrationServiceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Domain\/ApiKeyPrincipalFactoryTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyPrincipalFactoryTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Domain\/EffectiveAccessTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Models/Authorization/EffectiveAccessTest.cs; no member changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs\:1

Changed

```text
only the two old full names of IRbacAdministrationService (made-up Domain fixture parameter and expected failure text) become Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService; compile-forced, lists untouched; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ApiKeyControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthorizationControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/CreateRightsControllerGuardTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/JwtBearerStandaloneGateTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RbacGroupMappingConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04StopARefreshSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice06TheWarningsWithoutTheLogSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHostPerFixtureAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportOwnerWriteAccessTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S4\_DeliveriesDeleteGuardInversionTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SuggestionsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WizardsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataInstancePropertiesTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestConfiguration\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestWebApplicationFactory\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DependenciesAsReadExtensions\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WizardsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/FeatureDependencyRendering\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/VoterIdentityResolver\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/UsageData\/UsageDataInstanceProperties\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureListService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

##### Agreement

Namespaces of the Access Domain and Application types

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs\:1

Producer

Migrated

```text
Producer of the moved types; names change, members do not
```

Namespaces of the Access Domain and Application types

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Consumer

Migrated

```text
The composition root registers the moved services under their new namespaces; registrations, order and lifetimes unchanged
```

IRbacAdministrationService public surface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IRbacAdministrationService\.cs\:1

Producer

UnchangedCompatible

```text
Only the namespace changes; the member pin holds the members
```

IRbacAdministrationService public surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs\:1

Consumer

UnchangedCompatible

```text
Calls the same members; a using line follows
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move (git mv plus namespace line, then usings, full names and paths); no further preserving move makes it easier
```

contract_tests

```text
No wire or persisted shape changes and every caller changes in the same commit inside one deployable; the layout oracle, the unchanged suite and access-answers-compare cover the consumers
```

reuse

```text
Every target is an existing owner relocated by git mv with a namespace line, an existing consumer edited by using lines or full names, or the EmbedHandshakeOutcome declaration moved unchanged into its own file; no new type or responsibility is created. Candidates consulted and reused unchanged: ModuleRules (TypesLiveInTheirLayer with the Models former home for the admitted types, DomainReachesOnlyTheKernelAndTheBaseLibrary), AccessAndLicensingMembership (Access lists and admissions), ForecastingDomainLayoutArchUnitTest and PromisesLayoutArchUnitTest (oracle shape), RbacAdministrationService and IRbacAdministrationService (still the single RBAC entry), AccessTablesSingleReaderArchUnitTest (typeof follows). Only the oracle AccessApplicationLayoutArchUnitTest is new: no existing oracle states the Access layout or pins the interface members. Future paths do not resolve as locators until the move
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
Every Access Domain and Application type measured in AccessAndLicensingMembership, with the admitted Models types, lives in its designed layer namespace, Access Domain reaches only the kernel and base library, and the IRbacAdministrationService public members equal the fixed list
```

Invariant

Domain

AccessApplicationLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
the Domain and Application types in their old namespaces fail the layer rule naming each
```

```text
a list name with no matching type fails as missing
```

```text
a made-up Domain type reaching a repository fails the Domain rule
```

```text
an extra, missing or re-typed IRbacAdministrationService member fails the member pin
```

```text
Closed finite set of named types, members, keys and paths read from the compiled assembly and repository files; a native example table compared both ways is exact, a generative property adds nothing
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Access Domain and Application types live in Modules/Access by namespace, Domain reaches only the kernel, the IRbacAdministrationService surface is unchanged; lists compared both ways and never vacuous
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessApplicationLayoutArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/ApiKeyAuthenticationHandler\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/BlockedModeFilter\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/BlockedModeFilter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/DisabledAuthenticationHandler\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/DisabledAuthenticationHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/SmartAuthSchemeSelector\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/SmartAuthSchemeSelector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/AccessMarkerAttribute\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/AccessMarkerAttribute.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/CheckedInHandlerAttribute\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/CheckedInHandlerAttribute.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/RbacGuardAttribute\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/RbacGuardAttribute.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/SignedInOnlyAttribute\.cs

Create

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/SignedInOnlyAttribute.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/ApiKeyAuthenticationHandlerTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyAuthenticationHandlerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/BlockedModeFilterTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/BlockedModeFilterTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/SmartAuthSchemeSelectorTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/SmartAuthSchemeSelectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/RbacGuardAttributeTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/RbacGuardAttributeTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/appsettings\.Development\.json

Extend

```text
the Logging:LogLevel key becomes Lighthouse.Backend.Modules.Access.Adapters.Http.DisabledAuthenticationHandler, value Warning unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/CreateRightsControllerGuardTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/JwtBearerStandaloneGateTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WizardsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestConfiguration\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DependenciesAsReadExtensions\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WizardsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/VoterIdentityResolver\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessHttpAdaptersLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessHttpAdaptersLayoutArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run AccessHttpAdaptersLayoutArchUnitTest: look each name of AccessAndLicensingMembership.AccessAdaptersHttp up in the compiled assembly and read its namespace; read RbacGuardAttribute constructors and public members and the AttributeUsage of RbacGuardAttribute, AccessMarkerAttribute, SignedInOnlyAttribute and CheckedInHandlerAttribute from the IL; count the guarded actions with the rule EveryActionDeclaresItsAccessArchitectureTest uses; read appsettings.Development.json from the repository root
```

```text
The eight Http adapter types live in Lighthouse.Backend.Modules.Access.Adapters.Http and none is left under Services.Implementation.Auth or Services.Implementation.Authorization; RbacGuardAttribute constructors and public members and the four AttributeUsage values equal the list fixed from the IL before the move; the guarded-action count equals the number measured at the start of the slice; the Logging:LogLevel section holds a key equal to typeof(DisabledAuthenticationHandler).FullName with value Warning; the existing suite stays green
```

```text
A type left in its old namespace, a list name matching no type, a changed guard constructor or member, a changed AttributeUsage, a lost guarded action, or the log key left naming the old full name: the test fails naming it. The pins are GREEN before the move and the key pin fails after the move until the key is renamed; the layout part is RED today
```

##### Promises

P3

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/ApiKeyAuthenticationHandler\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/BlockedModeFilter\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/BlockedModeFilter.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/DisabledAuthenticationHandler\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/DisabledAuthenticationHandler.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/SmartAuthSchemeSelector\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/SmartAuthSchemeSelector.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/AccessMarkerAttribute\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/AccessMarkerAttribute.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/CheckedInHandlerAttribute\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/CheckedInHandlerAttribute.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/RbacGuardAttribute\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/RbacGuardAttribute.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/SignedInOnlyAttribute\.cs\:1

Migrated

```text
git mv; namespace line only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/SignedInOnlyAttribute.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/ApiKeyAuthenticationHandlerTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyAuthenticationHandlerTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/BlockedModeFilterTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/BlockedModeFilterTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/SmartAuthSchemeSelectorTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/SmartAuthSchemeSelectorTest.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/RbacGuardAttributeTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Authorization/RbacGuardAttributeTest.cs; no member changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/appsettings\.Development\.json\:1

Changed

```text
the Logging:LogLevel key becomes Lighthouse.Backend.Modules.Access.Adapters.Http.DisabledAuthenticationHandler, value Warning unchanged; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/CreateRightsControllerGuardTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/JwtBearerStandaloneGateTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WizardsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestConfiguration\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DependenciesAsReadExtensions\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WizardsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/VoterIdentityResolver\.cs\:1

Changed

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them; no behaviour change
```

##### Agreement

Namespaces of the Access Http adapter types

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/RbacGuardAttribute\.cs\:1

Producer

Migrated

```text
Producer of the moved types; names change, members do not
```

Namespaces of the Access Http adapter types

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Consumer

Migrated

```text
Schemes, BlockedModeFilter and handlers are registered under the new namespaces; lines and order unchanged
```

Development log category of DisabledAuthenticationHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/appsettings\.Development\.json\:12

Consumer

Migrated

```text
The key follows the full type name so the Warning level keeps its effect
```

Development log category of DisabledAuthenticationHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/DisabledAuthenticationHandler\.cs\:1

Producer

Migrated

```text
ILogger of the handler logs under its full type name, which changes with the namespace
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move (git mv plus namespace line, then usings, full names and paths); no further preserving move makes it easier
```

contract_tests

```text
No wire or persisted shape changes and every caller changes in the same commit inside one deployable; the layout oracle, the unchanged suite and access-answers-compare cover the consumers
```

reuse

```text
Every target is an existing owner relocated by git mv with a namespace line, or an existing consumer edited by using lines, full names or the one log key; no new type or responsibility is created. Candidates consulted and reused unchanged: ModuleRules.TypesLiveInTheirLayer, AccessAndLicensingMembership (AccessAdaptersHttp), EveryActionDeclaresItsAccessArchitectureTest and its AccessRule scan (counts guarded actions, keeps its assertions), RbacGuardAttribute, the four attributes. Only the oracle AccessHttpAdaptersLayoutArchUnitTest is new: no existing oracle states the Http adapter layout or pins the guard surface and the log key. Future paths do not resolve as locators until the move
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
The Access Http adapters live in Modules.Access.Adapters.Http, RbacGuardAttribute and the four attribute usages are unchanged, the guarded-action count is unchanged, and the development log key names the disabled authentication handler by its full type name
```

Invariant

Adapter

AccessHttpAdaptersLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
the eight types in their old namespaces fail the layer rule naming each
```

```text
a list name with no matching type fails as missing
```

```text
a guard constructor removed or an AttributeUsage changed fails the pin
```

```text
a guarded-action count different from the measured number fails
```

```text
the log key naming the old namespace fails the key pin
```

```text
Closed finite set of named types, members, keys and paths read from the compiled assembly and repository files; a native example table compared both ways is exact, a generative property adds nothing
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T3

```text
Access Http adapters live in Modules/Access/Adapters/Http by namespace; guard surface, attribute usage, guarded-action count and the development log key preserved; lists compared both ways and never vacuous
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessHttpAdaptersLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessHttpAdaptersLayoutArchUnitTest"
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/TestLicenseData\.cs

Create

```text
git mv; namespace line plus the three relative file paths, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/TestLicenseData.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/invalid\_license\.json

Create

```text
git mv; bytes unchanged, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/invalid_license.json
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/valid\_expired\_license\.json

Create

```text
git mv; bytes unchanged, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/valid_expired_license.json
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/LicensingIntegrationTest\.cs

Create

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicensingIntegrationTest.cs
```

\.github\/workflows\/ci\_backend\.yml

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

\.github\/workflows\/ci\_verifyauth\.yml

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

\.gitignore

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

Scripts\/examine\/build\-examine\-commands

Extend

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs

Extend

```text
the using of TestLicenseData follows its new namespace
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseVerifierTest\.cs

Extend

```text
the using of TestLicenseData follows its new namespace
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingTestDataPlacementTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~LicensingTestDataPlacementTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run LicensingTestDataPlacementTest: read the namespaces of TestLicenseData and LicensingIntegrationTest from the test assembly; look for invalid_license.json and valid_expired_license.json in the test output folder; read .gitignore, .github/workflows/ci_backend.yml, .github/workflows/ci_verifyauth.yml, the test csproj and Scripts/examine/build-examine-commands from the repository root and collect the paths they give for the licence fixtures
```

```text
TestLicenseData and LicensingIntegrationTest live in Lighthouse.Backend.Tests.Modules.Licensing; the two committed licence files are copied to Modules/Licensing in the test output and not to Services/Implementation/Licensing; every one of the five files names the licence fixtures under Lighthouse.Backend.Tests/Modules/Licensing and none names Services/Implementation/Licensing; LicenseControllerTest, LicenseServiceTest, LicenseVerifierTest and LicensingIntegrationTest stay green, and the pushed CI run shows Verify Authentication passing through ci-auth-suites-show
```

```text
TestLicenseData or LicensingIntegrationTest left in the old namespace, a licence file copied to the old output folder, or any of the five files still naming the old folder (for example the CI generator writing valid_not_expired_license.json there): the test fails naming the file and the path. Today everything names the old folder, so the test is RED
```

##### Promises

P4

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/TestLicenseData\.cs\:1

Migrated

```text
git mv; namespace line plus the three relative file paths, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/TestLicenseData.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/invalid\_license\.json\:1

Migrated

```text
git mv; bytes unchanged, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/invalid_license.json; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/valid\_expired\_license\.json\:1

Migrated

```text
git mv; bytes unchanged, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/valid_expired_license.json; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/LicensingIntegrationTest\.cs\:1

Migrated

```text
git mv; namespace line plus usings, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/LicensingIntegrationTest.cs; no member changes
```

consumer

\.github\/workflows\/ci\_backend\.yml\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

\.github\/workflows\/ci\_verifyauth\.yml\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

\.gitignore\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

Scripts\/examine\/build\-examine\-commands\:1

Changed

```text
paths, namespaces and the HostUsersStillToMove full name follow to Tests/Modules/Licensing; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs\:1

Changed

```text
the using of TestLicenseData follows its new namespace; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseVerifierTest\.cs\:1

Changed

```text
the using of TestLicenseData follows its new namespace; no behaviour change
```

##### Agreement

Licence fixture path

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:72

Producer

Migrated

```text
CopyToOutputDirectory items name Modules\Licensing, so the output folder changes
```

Licence fixture path

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/TestLicenseData\.cs\:5

Consumer

Migrated

```text
Reads the files from Modules/Licensing relative to the output folder
```

Licence fixture path

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:97

Consumer

Migrated

```text
Reads the two committed files from the new relative path
```

Licence fixture path

\.github\/workflows\/ci\_backend\.yml\:69

Producer

Migrated

```text
Generates valid_not_expired_license.json into the new folder
```

Licence fixture path

\.github\/workflows\/ci\_verifyauth\.yml\:101

Producer

Migrated

```text
Generates valid_not_expired_license.json into the new folder for the Keycloak runs
```

Licence fixture path

Scripts\/examine\/build\-examine\-commands\:441

Consumer

Migrated

```text
The examine probes import the premium fixture from the new folder
```

Licence fixture path

\.gitignore\:427

Consumer

Migrated

```text
Ignores the generated premium fixture in the new folder
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move (git mv plus namespace line, then usings, full names and paths); no further preserving move makes it easier
```

contract_tests

```text
No wire or persisted shape changes and every caller changes in the same commit inside one deployable; the layout oracle, the unchanged suite and access-answers-compare cover the consumers
```

reuse

```text
Every target is existing test data relocated by git mv, or an existing path or using edited to follow it; no new type or responsibility is created. Candidates consulted and reused unchanged: TestLicenseData (still the single reader of the fixture files), the CI generator steps of ci_backend.yml and ci_verifyauth.yml, the licence capture of build-examine-commands, the CopyToOutputDirectory items of the test csproj. Only the oracle LicensingTestDataPlacementTest is new: no existing test states where the fixtures live. Future paths do not resolve as locators until the move
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
The Licensing test data lives in Tests/Modules/Licensing and every file that names the licence fixtures names that folder
```

Invariant

Infrastructure

LicensingTestDataPlacementTest

###### Treatment

Finite

###### Cases

```text
TestLicenseData in the old namespace fails naming it
```

```text
a licence file in the old output folder fails naming it
```

```text
a workflow, gitignore, csproj or examine builder line naming the old folder fails naming the file and line
```

```text
Closed finite set of named types, members, keys and paths read from the compiled assembly and repository files; a native example table compared both ways is exact, a generative property adds nothing
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T4

```text
Licensing test data and every path naming it live under Tests/Modules/Licensing; no file names the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingTestDataPlacementTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~LicensingTestDataPlacementTest"
```

##### Contract tests

None.

## ADRs

None.
