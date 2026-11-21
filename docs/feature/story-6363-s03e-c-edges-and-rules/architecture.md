# Architecture brief: story\-6363\-s03e\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (Ratchet design R-A1..R-A7): Access gets its own module rules in Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessModuleBoundariesArchUnitTest.cs, written with the shared ModuleRules (AccessModule constant already at ModuleRules.cs:29) and an Access ModuleWording built in the oracle from two strings, as ForecastingModuleBoundariesArchUnitTest does. Lists inline by full type name, measured from the IL of the current compiled tree (main at ca13aa58b, after feature B) and compared both ways. R-A1: each type of AccessAndLicensingMembership lives in its layer (Domain = AccessDomain plus ModelsTypesThatMoveIntoAccess, 8 types; Application 16; Adapters.Http 8), none left under ^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization)(\.|$) (measured 0 left). R-A2: Domain reaches only Models and the base library, empty. R-A3: outside Domain readers besides the API namespace and the composition root: Lighthouse.Backend.Services.Implementation.FeatureListService. R-A4: outside Application readers besides the API namespace and the composition root: Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering, Lighthouse.Backend.Services.Implementation.FeatureListActiveWork, Lighthouse.Backend.Services.Implementation.FeatureListService, Lighthouse.Backend.Services.Implementation.Refinement.VoterIdentityResolver, Lighthouse.Backend.Services.Implementation.UsageData.UsageDataInstanceProperties; the DESIGN record's text-search entries IFeatureListService, ReportCommands, ReportQueries and UsageDataConsentService hold no Access edge in the IL and are not listed (the IL decides). R-A5: Adapters.Http reached only from the API namespace and the composition root, with the list [Lighthouse.Backend.Program] (it registers ApiKeyAuthenticationHandler, BlockedModeFilter, DisabledAuthenticationHandler, SmartAuthSchemeSelector); nothing else. R-A6: Application edges into Lighthouse.Backend.Data, Lighthouse.Backend.Services, Lighthouse.Backend.API, any Lighthouse.Backend.Modules.*.Adapters and Microsoft.EntityFrameworkCore, as Dependent -> Target, exactly the 21 measured edges: ApiKeyService -> Services.Interfaces.Repositories.IApiKeyRepository and IRepository`1; AuthModeResolver -> Services.Interfaces.IPlatformService; EmbedSessionTokenService -> Services.Interfaces.Repositories.IEmbedSessionTokenRepository; CurrentUserProfileService and OidcGroupSnapshotWriter each -> Data.LighthouseAppContext and EF Core ChangeTracking.EntityEntry`1, DbContext, DbSet`1, EntityFrameworkQueryableExtensions, Query.UpdateSettersBuilder`1; RbacAdministrationService -> Data.LighthouseAppContext, EntityEntry`1, DbContext, DbSet`1, EntityFrameworkQueryableExtensions. Licensing Application is legal by namespace. R-A7: Access (every layer) reaches no other context (Modules.WorkItems, Forecasting, FlowMetrics, TrackerIntegration, Promises, the update code under Services BackgroundServices and Interfaces.Update, Refinement, Reports, the write-back round), empty; its two Licensing edges (AuthModeResolver and RbacAdministrationService -> Modules.Licensing.Application.ILicenseService) are legal. A made-up offender per list position lives in a fixture namespace beside the module (for example Lighthouse.Backend.Modules.AccessFixtures), including one outside the API whose only reference is an applied attribute ([RbacGuard] or [SignedInOnly]), which R-A5 must name.
```
Governed slices: S1

#### D2

```text
Confirmed (Ratchet design R-L1..R-L5): Licensing gets its own module rules in Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/LicensingModuleBoundariesArchUnitTest.cs with ModuleRules (LicensingModule constant at ModuleRules.cs:31) and a Licensing ModuleWording built in the oracle; lists measured from the current IL and compared both ways. R-L1: LicensingApplication (4) and LicensingAdaptersHttp (1) of AccessAndLicensingMembership live in their layer, none left under ^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Licensing(\.|$) (measured 0). R-L2: Licensing reaches no Access type and no other context, empty. R-L3: outside Application readers besides the API namespace, the composition root and Modules.Access.Application (legal callers), exactly 11: Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater, Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService, Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision, Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater, Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater, Lighthouse.Backend.Services.Implementation.BackgroundServices.UsageDataConsentPruningService, Lighthouse.Backend.Services.Implementation.Reports.ReportCommands, Lighthouse.Backend.Services.Implementation.Reports.ReportQueries, Lighthouse.Backend.Services.Implementation.UsageData.UsageDataConsentService, Lighthouse.Backend.Services.Implementation.UsageData.UsageDataInstanceProperties, Lighthouse.Backend.Services.Implementation.WriteBackTriggerService (candidate 2 readers, owners Epic 6474 and #6657, stay listed). R-L4: Adapters.Http reached only from the API namespace, through HttpAdaptersAreReachedFromOutsideOnlyBy with an empty list (the composition root does not reach it, measured). R-L5: Application edges into Data, Services, API, module Adapters and EF Core, exactly: Lighthouse.Backend.Modules.Licensing.Application.LicenseService -> Lighthouse.Backend.Services.Interfaces.ILighthouseClock and -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1. A made-up offender per rule lives in a fixture namespace such as Lighthouse.Backend.Modules.LicensingFixtures.
```

#### D3

```text
Confirmed (DESIGN record, neighbour lists): the Forecasting and Work Items lists that name Access or Licensing types already hold them under the new names since feature B, measured: WorkItemsModuleBoundariesArchUnitTest.EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue (lines 148-149: DependencyDecision -> Modules.Licensing.Application.ILicenseService, FeatureDependencyRendering -> Modules.Access.Application.IRbacAdministrationService) and the Forecasting edge list entry at ForecastingModuleBoundariesArchUnitTest.cs:112 (ForecastFilterRuleService -> Modules.Licensing.Application.ILicenseService). No list entry moves or shrinks in C2; the S2 oracle reads those lists by field name and requires every Access or Licensing entry to name a type under Modules.Access or Modules.Licensing that the IL holds. No other test file names an old Access or Licensing full name except the deliberate old-place alternatives of the A1 and A2 oracles.
```

#### D4

```text
Confirmed (DESIGN record, RbacIdentityPattern retired in C3): ModuleBoundariesArchUnitTest.cs loses RbacIdentityPattern (lines 22-23), RbacIdentityModule (lines 64-65) and its one assertion, the not-empty check at line 124; no other member of that file changes. The not-empty claim is carried by the new module rules: TypesLiveInTheirLayer fails on a listed Access or Licensing type that is absent (MissingSubjects) and DomainReachesOnlyTheKernelAndTheBaseLibrary fails on an empty Access Domain. The key is also read by name in feature A's oracle AccessAndLicensingRulesSeeTheModulesArchUnitTest: the two tests that read it (AServiceUnderTheModuleReachingARepository_IsNamedByTheModuleMapsAccessAndLicensingNamespaces_AsItsTwinInTheOldPlaceIs, lines 63-81, and AnAccessServiceAskingTheLicenceUnderTheModules_StaysInsideOneModuleOnTheModuleMap_AsItsTwinInTheOldPlaceDoes, lines 155-175) and the private helpers and fixtures only they use are removed in the same slice, because the module map they guard no longer exists; its Access-asks-Licensing legality is carried by R-A7 (Licensing not forbidden) and R-L3 (Access Application a legal caller). Every other test of that file, and the old-place alternatives in the Work Items, Tracker integration, Promises and Forecasting keys, stay unchanged.
```
Governed slices: S3

#### D5

```text
Confirmed (D-9, maintainer answer 2026-11-17): Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessAndLicensingUnitTestPlacementTest.cs, modelled on PromisesUnitTestPlacementTest, checks that every unit test of an Access or Licensing type (the AccessAndLicensingMembership types) lives under Tests/Modules/Access or Tests/Modules/Licensing in the folder of its subject's layer (Domain, Application, Adapters/Http), and that no file is left in Services/Implementation or Services/Interfaces under Auth, Authorization or Licensing, in production or tests, with no per-file exception (B4 moved the licence data; LicensingTestDataPlacementTest stays as it is). Integration, acceptance, controller, contract and architecture test roots are not judged; tests of other contexts that mock an Access or Licensing port are not Access unit tests. Each rule also runs on a made-up tree (PlacementFixtures/Modules/Access, PlacementFixtures/Modules/Licensing and a made-up file in an old folder), which must be reported. Measured: those old folders hold 0 files today.
```
Governed slices: S3

#### D6

```text
Resolved open item (does C3 have a RED): C3 stays its own slice with an observable RED. Its oracle asks whether any namespace key of ModuleBoundariesArchUnitTest (each constant or static string field read as a pattern) still takes in a made-up type name under the old Services Auth, Authorization or Licensing namespaces; today RbacIdentityPattern does, so the oracle is RED naming it, and it turns GREEN only when the key is retired. The placement and no-file-left checks are GREEN on arrival over the real tree (the old folders are empty) and are discriminated by their made-up trees, which must be reported: they are the regression guard. Folding C3 into C2 was rejected: the retirement is a real edit with its own RED, and product slices are not merged in DESIGN. S1's RED is the missing shared HTTP-adapter rule, asked for by name. S2's lists and rules are GREEN on arrival once S1's rule exists, because feature B already re-keyed the neighbour lists; S2's discriminating evidence is its made-up offender per rule, and it has no crafter target.
```
Governed slices: S1, S3

#### D7

```text
Claim-to-checker: every claim of feature C is a finite fact of the compiled IL or of the file tree: namespace placement, the exact set of edges per rule, the location of each test file, and the absence of a key that sees the old namespaces. Each is decided exactly by its NUnit architecture oracle (Finite laws) with a made-up offender per rule as falsifier. No Quint, TLA+ or Agda model is selected: no state machine, ordering, concurrency, recovery or progress claim exists (no production code changes), and a model would restate the namespace map and could not fail. Liveness is not applicable: nothing waits for progress. Types: the rule lists are string sets compared both ways by InlineBaseline; the C# compiler checks the new rule's signature; property tests add nothing over a finite edge set. Paradigm object_oriented (repository authority); the work is test-side rule code; nw-code-design-oo smells applied: duplicated knowledge (one shared rule, not a copy per module) and a false reason in a rule text; namespace patterns stay strings, as in every existing rule file.
```
Governed slices: S1, S3

#### D8

```text
Confirmed (maintainer 2026-11-17): rigor thorough for all of feature C (architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet). Order S1, S2, S3. Behaviour-free: no production file changes; EveryActionDeclaresItsAccess, AccessTablesSingleReader, LicenseGateSingleSource and the Reports and Refinement guard rules keep every assertion; every existing test except the two A1 module-map tests retired in S3 stays and stays green. Finalize work (mutation, ADR-270, ARCHITECTURE.md section 4, brief ACs, catalogue, Lighthouse finalize checklist) is not an nwdes slice.
```
Governed slices: S1, S3

#### D9

```text
Confirmed (GDP-3, counterexample): the shared rules have no inbound rule for an HTTP adapter layer. AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot (ModuleRules.cs:59) lists exactly [compositionRoot] compared both ways and explains adapters as storage and background code: for Licensing Adapters.Http, which the composition root does not reach (measured), it would name Lighthouse.Backend.Program for deletion, and for Access it would give a false reason. S1 extends ModuleRules with one rule, HttpAdaptersAreReachedFromOutsideOnlyBy(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string httpAdapters, string legalCallerNamespaces, string outside = AnyNamespace, ModuleWording? wording = null) returning InboundVerdict: the module's own types and the types in the legal callers' namespaces (the API) are set aside, every other dependent of the Http adapters is compared both ways with the list, and the rule text says the Http adapters are the attributes, filters and authentication handlers the API applies and the composition root registers, so any other caller ties its code to how requests are authenticated and guarded, and says how to fix it. Access calls it with list [Lighthouse.Backend.Program]; Licensing with an empty list. Carrier: InboundVerdict (existing record, ModuleRules.cs:204); owner the test-side ModuleRules class; no per-process state. ModuleWording and every existing ModuleRules member stay unchanged.
```
Governed slices: S1

### Constraints

#### K1

```text
Rule lists only shrink and are never loosened: inline full-name lists compared both ways against the IL (IlDependencies, which sees base types, interfaces, members, signatures, method bodies, applied attributes and their constructor arguments, and misses catch-clause, event and attribute-named-argument references); exemptions are exact per type and its nested types, never a namespace or layer; holdings compare top-level types. A legal caller namespace is the API (Lighthouse.Backend.API) or a named module layer; the composition root is Lighthouse.Backend.Program.
```
Governed slices: S1, S3

#### K2

```text
One oracle file per slice; an approved oracle file is never edited within this feature; a crafter target is never an oracle support; oracles name production types as strings. Made-up offenders live in fixture namespaces that do not match the module constant (Modules.AccessFixtures, Modules.LicensingFixtures, PlacementFixtures). Rule tests assert by the failure naming the offender, not by counts. No nWave scenario tags. The oracles are IL and file-tree architecture tests and start no host. A new shared rule is invoked by the oracle by name and parameter names, as ModuleRulesCatchANewOffenderArchUnitTest does, so the oracle compiles before the rule exists and its RED is an assertion, not a build error.
```
Governed slices: S1, S3

#### K3

```text
Input bounds: none stored or validated; the rules read a finite compiled assembly and a finite file tree, and the authority states no bound. Scope out: any production change, Access persistence (entities, repositories, seeders), candidate 2 readers stay listed, the old-place alternatives of other rule keys stay.
```
Governed slices: S1, S3

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Every producer and consumer of the rule code and the rule keys is in one test project and changes in the same commit; no persisted or wire data changes.
```

### Boundaries

```text
The compiled Lighthouse assembly (Lighthouse.dll) and the test assembly read through IlDependencies, and the repository file tree, by the NUnit architecture oracles run with dotnet test on Lighthouse.Backend.Tests
```

#### Driven ports

None.

```text
Access Domain reaches only Models and the base library; Access Application may reach Licensing Application; Licensing reaches no Access type and no other context; outsiders reach Access and Licensing Domain and Application only from the API, the composition root, Access Application (for Licensing) or as listed; Http adapters only from the API and, for Access, the composition root
```

#### Failures

None.

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

None.

### Models

None.

### Closure

```text
Access and Licensing module rules and test placement
```

Safety

Pass

DesignedNotBuilt

```text
Each rule's inline list is compared both ways with the IL and a made-up offender per rule must be named; the placement check names a made-up file in an old folder
```

##### Evidence

None.

```text
Access and Licensing module rules and test placement
```

Inhabitation

Pass

DesignedNotBuilt

```text
The measured lists are non-empty where the IL holds edges (R-A3, R-A4, R-A5, R-A6, R-L3, R-L5) and every listed type exists in the current compiled tree
```

##### Evidence

None.

```text
Access and Licensing module rules and test placement
```

PublicReachability

Pass

DesignedNotBuilt

```text
The oracles run under dotnet test over the real compiled assembly and the real tree; the full filtered suite runs them in CI
```

##### Evidence

None.

```text
Access and Licensing module rules and test placement
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no waiting, queue, schedule or progress behaviour; every check is a finite synchronous read
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
Assumption negated: IL sees every reference
```

```text
Rules over base types, members, signatures, bodies and applied attributes
```

```text
Catch-clause, event and attribute-named-argument references are invisible to IlDependencies
```

```text
Claiming those edges are checked
```

```text
Stated limit of the rule set; reviewers read the diff
```

acceptance\-designer

#### Y2

Stressor

```text
Assumption negated: the DESIGN record's text-search baselines are right
```

```text
Lists equal the IL both ways
```

```text
Lists differ from the text-search baseline (R-A4 drops IFeatureListService, ReportCommands, ReportQueries, UsageDataConsentService; R-A3 drops Program, which is legal)
```

```text
Listing an entry the IL does not hold
```

```text
The oracle measures at authoring; a wrong entry fails naming it
```

acceptance\-designer

#### Y3

Stressor

```text
Assumption negated: retiring RbacIdentityPattern touches one file
```

```text
Every remaining test green
```

```text
Feature A's oracle loses its two module-map tests and their fixtures
```

```text
Leaving a test that reads a removed key, or removing any other A1 test
```

```text
The full filtered suite after S3 shows no failing reflective read
```

crafter

#### Y4

SinglePointOfFailure

```text
AccessAndLicensingMembership feeds R-A1, R-L1, the A2 membership test and the placement check
```

```text
One list per layer
```

```text
A wrong list fails all of them at once
```

```text
A second copy of the lists
```

```text
The A2 membership test compares the lists with the IL both ways
```

crafter

#### Y5

Stressor

```text
Second set, walkthrough: a new type is added under Modules.Access or Modules.Licensing, or a new test file lands in an old folder
```

```text
Membership, layout and placement rules name it
```

```text
The author adds it to the list or moves it
```

```text
A namespace-wide exemption
```

```text
None found beyond the lists
```

crafter

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Gains HttpAdaptersAreReachedFromOutsideOnlyBy, the inbound rule for an HTTP adapter layer with legal caller namespaces and a list compared both ways; every existing member unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessModuleBoundariesArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run AccessModuleBoundariesArchUnitTest over the compiled Lighthouse assembly and the test assembly's made-up offenders: R-A1..R-A7 through ModuleRules with an Access ModuleWording, the Http rule asked of ModuleRules by name and parameter names
```

```text
Every Access type lives in its layer and none is left under Services Auth or Authorization; each rule's measured inline list equals the IL both ways (R-A3 FeatureListService; R-A4 the five readers; R-A5 the composition root only; R-A6 the 21 Data, platform and EF Core edges; R-A2 and R-A7 empty); each rule names its made-up offender in the Access fixture namespace, the attribute-only offender outside the API included, and passes its compliant twin; no other test changes outcome
```

```text
Today ModuleRules has no inbound rule for an HTTP adapter layer, so the oracle is RED naming the missing rule and the attribute-only offender goes unnamed; a list entry the IL does not hold, or an edge the list misses, fails naming it; a rule that does not name its made-up offender fails
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
The shared rule set already holds AccessModule (line 29) and the Domain, Application and outbound rules; only an HTTP adapter inbound rule is missing, and a second rules class would duplicate the shared rule knowledge
```

AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:59

Reuse

```text
Consulted and kept as it is for storage and background adapters; it cannot take the API as a legal caller nor an empty list, and its reason speaks of storage, so it is not bent to Http adapters
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:7

Reuse

```text
Built in the oracle from the module name and its legal callers, as the Forecasting and Promises oracles do
```

AccessAndLicensingMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs\:11

Reuse

```text
Measured layer lists from feature A; R-A1 reads them rather than a second copy
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:1

Reuse

```text
The one IL reader behind every module rule, which also sees applied attributes
```

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:171

Reuse

```text
Its by-name invocation of a shared rule with a parameter dictionary is the model for asking ModuleRules for the new rule before it exists
```

##### Impact

test\-rule\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:204

Changed

```text
One new public static rule returning the existing InboundVerdict
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:59

UnchangedCompatible

```text
Consumers of ModuleRules (Forecasting, Work Items, Tracker integration, Promises, Flow metrics rules, ModuleRulesCatchANewOffender) call existing members only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs\:1

UnchangedCompatible

```text
No production file changes; the rules only read the IL
```

##### Agreement

None.

##### Not applicable

agreement

```text
No schema, wire format or shared configuration changes; ModuleRules is test code whose callers compile with it in the same commit
```

prefactoring

```text
One added test-side rule and a new oracle file; no preserving move makes the addition easier
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
For each rule R-A1..R-A7 the set of offending edges the IL shows equals the rule's inline list, and the Http rule names every outside dependent of Access Adapters.Http that is neither in the API nor listed
```

Invariant

Application

AccessModuleBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
a listed entry the IL does not hold is named as no longer offending
```

```text
an edge the list misses is named as offending
```

```text
a made-up offender per list position in the Access fixture namespace is named
```

```text
a made-up type outside the API reaching Access only through an applied attribute is named by R-A5
```

```text
a compliant twin and the API are not named
```

```text
The compiled assembly holds a finite set of types and edges; exact comparison decides each rule and generation adds no coverage
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

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
Access keeps its shape: each type in its layer and none in the old Services namespaces, Domain reaches only the kernel and the base library, outsiders reach Domain, Application and Http adapters only from the API, the composition root or as listed, Application reaches Data, platform and EF Core only as listed, Access reaches no other context
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessModuleBoundariesArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
RbacIdentityPattern, RbacIdentityModule and the not-empty assertion at line 124 are removed; nothing else changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs

Extend

```text
The two tests that read RbacIdentityPattern by name and the helpers and fixtures only they use are removed; every other test stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingUnitTestPlacementTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/PlacementFixtures\/Modules\/Access

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/PlacementFixtures\/Modules\/Licensing

#### Public oracle

```text
Run AccessAndLicensingUnitTestPlacementTest over the repository tree, the compiled test assembly and its symbols, the made-up placement trees, and the namespace keys of ModuleBoundariesArchUnitTest read as patterns against made-up names in the old Services Auth, Authorization and Licensing namespaces
```

```text
Every Access and Licensing unit test sits under Tests/Modules/Access or Tests/Modules/Licensing in its subject's layer folder; no file is left under Services/Implementation or Services/Interfaces Auth, Authorization or Licensing in production or tests, with no per-file exception; the made-up misplaced test and the made-up file in an old folder are reported and their well-placed twins are not; no key of ModuleBoundariesArchUnitTest takes in a name under the old namespaces
```

```text
Today RbacIdentityPattern takes in the old Services Auth, Authorization and Licensing names, so the oracle is RED naming it until it is retired; a unit test or source file placed in an old folder, made-up or real, is named
```

##### Promises

P3

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

PromisesUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:17

Reuse

```text
Model for the placement and no-file-left check with made-up trees and symbols; a sibling file per context, as Forecasting, Tracker integration and Promises have
```

LicensingTestDataPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingTestDataPlacementTest\.cs\:15

Reuse

```text
Already guards the licence data under Tests/Modules/Licensing; kept, so the new check needs no Licensing exception
```

RbacIdentityPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:22

Replace

```text
Its one assertion (the module is not empty) is carried by TypesLiveInTheirLayer and the Domain rule of the new module rules
```

AccessAndLicensingRulesSeeTheModulesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:29

Extend

```text
Feature A's oracle keeps its ModuleRules-constant, neighbour-key and Forecasting-selector tests; only the two tests that read the retired key by name, and the fixtures only they use, go
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:124

Changed

```text
Key, selector and not-empty assertion removed
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:63

Changed

```text
Two module-map tests and their unused fixtures removed with the key they read
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:86

UnchangedCompatible

```text
The ModuleRules-constant, neighbour-key and Forecasting-selector tests keep every assertion
```

##### Agreement

RbacIdentityPattern\, the module map\'s Access and Licensing namespace key read by field name

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:22

Producer

Migrated

```text
Retired
```

RbacIdentityPattern\, the module map\'s Access and Licensing namespace key read by field name

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:67

Consumer

Migrated

```text
Test removed with the key
```

RbacIdentityPattern\, the module map\'s Access and Licensing namespace key read by field name

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:158

Consumer

Migrated

```text
Test removed with the key
```

##### Not applicable

prefactoring

```text
Removals in two test files and a new oracle; no preserving move makes them easier
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
Every Access or Licensing unit test file lies under Tests/Modules/Access or Tests/Modules/Licensing in its subject's layer folder, no file lies under Services/(Implementation|Interfaces)/(Auth|Authorization|Licensing), and no namespace key of ModuleBoundariesArchUnitTest matches a name in those old namespaces
```

Invariant

Infrastructure

AccessAndLicensingUnitTestPlacementTest

###### Treatment

Finite

###### Cases

```text
a made-up unit test outside its module folder is named
```

```text
a made-up file in an old folder is named
```

```text
a well-placed twin and a lookalike name are not named
```

```text
a key matching an old-namespace name is named
```

```text
The tree and the key set are finite; exact enumeration decides the law
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

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
Access and Licensing unit tests sit with their modules, no file of either context is left in the old Services folders, and no module-map key still sees the old namespaces
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingUnitTestPlacementTest"
```

##### Contract tests

None.

## ADRs

None.
