# Architecture brief: story\-6468\-s03i\-package\-organisation\-set\-up

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
D-0 No schema and no behaviour change: no entity, DbSet, migration, route, DTO, setting key, seeded value, terminology default, usage-data event or configuration key changes; observed by ef-model-unchanged and an empty diff under the migrations projects, Lighthouse.Frontend, Lighthouse.EndToEndTests and Models.
```
Governed slices: S1, S2, S3, S4, S5

#### D2

```text
D-1 Layout: Modules/OrganisationSetup/{Domain, Application, Application/Connections, Application/OptionalFeatures}, namespace = folder. A type goes to Domain only if its IL edges reach only the kernel, the base library and other set-up Domain types (expected WeeklyRecurrence and RecurringBlackoutRuleExtensions; the IL measured at S1 decides). Services and ports go to Application; the connection save, ChangedConnectionDefault, the connection usage and their two ports to Application/Connections; the three appliers, the registry and IOptionalFeatureApplier to Application/OptionalFeatures. No Application/Policies and no Adapters. Rejected: sub-folders per concern (Epic 6474 decides inner folders), one flat Application (drops the two existing groupings).
```
Governed slices: S2, S3

#### D3

```text
D-2 Membership: the 27 files of the five moving groups move; every staying set-up file is named in OrganisationSetupMembership with owner and reason: the four context seeders, FeaturePositionMap and IFeaturePositionMap (feature-list read side), the repositories, the Models types, API controllers, helpers and DTOs, TeamUpdater and PortfolioUpdater (Epic 6474), the write-back round (#6657). The optional-features mechanism moves with ordering.
```
Governed slices: S1, S2, S3

#### D4

```text
D-3 Models: every set-up Models type stays in the kernel (EF entities, types reached by entities, wire types, events handled by other contexts); ManualRankComparer stays by default for S04b.
```
Governed slices: S2, S3

#### D5

```text
D-4 Repositories stay in Services/*/Repositories, named in the membership with owner (S05c Team, S05d Portfolio, S05e connections, Organisation set-up for settings, terminology and blackouts) and target Modules/OrganisationSetup/Adapters/Persistence when S05 or S10 reshape repositories.
```
Governed slices: S1

#### D6

```text
D-5 The four context seeders (AppSettingSeeder, TerminologySeeder, OptionalFeatureSeeder, CategoryListNormalisationSeeder) stay in Services/Implementation/Seeding, owner Organisation set-up, target module start-up (S09a). Rejected: moving them to Adapters/Seeding (a second seeding decision in the Epic).
```
Governed slices: S1

#### D7

```text
D-6 Nothing of Team and Portfolio moves; no unit, level or event change. Their settings services move (AppSettingService, ConnectionSaveService, WorkTrackingSystemConnectionUsage). The brief IN line narrows accordingly (amended in S5).
```
Governed slices: S3, S5

#### D8

```text
D-7 Rules first: EXTEND the shared NamespaceMoveLaw (it already carries PatternDeclaration Context, Place and Composite roles) with an Organisation set-up map and a per-pattern declaration. For every regex-valued const or static readonly string field and every moved type, the law evaluates the field against its declared subject (Namespace or FullName; an undeclared field is evaluated on both and a match on either fails until declared). A field whose old subject matches carries a role: Context must also match the new subject (S1 widens Namespace fields with the alternative for Modules.OrganisationSetup, FullName fields with the new full name), Place must not match the new subject. Composite fields are declared CompositeOf their component and follow it by reference. String fields equal to an old namespace or old full name take roles by equality. Inline method-body literals are caught only by the literal sweep, whose rename list is fixed at S1 from the pre-move grep. Rejected: a text sweep alone (misses the loose-namespace patterns, which name no type) and plain widening of every match (wrong for Place patterns).
```
Governed slices: S1, S5

#### D9

```text
D-8 Open host by namespace, amended 2026-11-25 after the S2 slice review: S4 makes the set-up Application and Domain namespaces a legal outbound target in the Work Items, Flow metrics, Forecasting, Promises, Refinement and Reports rules, with a planted falsifier per module (a made-up downstream type reaching a made-up non-set-up foreign type is still named). Downstream modules whose outbound rules read set-up only through the Services/Platform folder (Refinement, Promises; in S3 the same for Access, Licensing and Reports via IAppSettingService) stop listing their set-up edges in the slice that moves the set-up type, not in S4: a renamed entry is an edge the rule does not read, and the lists are compared both ways, so it cannot be renamed. S1's pattern roles stay folder-only (set-up is a context, not part of the platform) and are not widened. Edges dropped in S2: Refinement RefinementCadenceCalendar to WeeklyRecurrence, RefinementCalendar and RefinementNeedCalculator to IBlackoutPeriodService; Promises DeliveryMetricValuesProjector to IBlackoutPeriodService. Between that move and S4 those edges are legal by omission, not by rule; the five slices are pushed together (D-13), so no pushed revision carries the window. S4's RED keeps the Work Items entries it already names (RefreshLogService to IAppSettingService, WorkItemService to IFeatureOrdering, renamed by S2/S3) and the Tracker integration list holds exactly the one ConnectionHealthCadence to IAppSettingService edge, owner S09a / Epic 6474. Rejected: legal by namespace for Tracker integration (contradicts the acknowledged-cycle decision); keeping every edge listed; widening S1's folder-only patterns.
```
Governed slices: S2, S3, S4

#### D10

```text
D-9 New rules: OrganisationSetupModuleBoundariesArchUnitTest on the shared ModuleRules (R-OS1 layer membership, R-OS2 Domain reaches kernel and base library only, R-OS3 Application reaches no API, module adapter or downstream context except listed edges, EF Core only along listed edges, R-OS4 Tracker integration reaches set-up only along its listed edge, R-OS5 set-up events constructed only in set-up Application except the listed TeamController to TeamDeleted edge, R-OS6 downstream allowance by namespace), lists inline by full type name, compared both ways, only shrink, ModuleWording for Organisation set-up. FeatureOrderingSingleSource, RecurringBlackoutEventsSeam, ConnectionFetchShapeChangedSinglePublisher, OptionalFeatureApplierSeam, BlackoutForecastShiftSeam and CategoryListOwnershipGuard keep every assertion and change only through classified patterns and renamed literals.
```
Governed slices: S4

#### D11

```text
D-10 The 9 unit tests move with production to Lighthouse.Backend.Tests/Modules/OrganisationSetup/{Domain, Application, Application/Connections} (namespace = folder), including the misplaced API/TerminologyServiceTest and Services/BlackoutPeriodServiceTest; OrganisationSetupUnitTestPlacementTest keeps them there from S4. API, integration, ManualSorting, container and seeder tests stay and change only using lines. The test-improvement rule is waived for move-only and using-only edits; a mock-heavy moved test is noted for the reviewer, not rewritten.
```
Governed slices: S2, S3, S4

#### D12

```text
D-11 DI: same registrations, order and lifetimes. Program.cs gains up to three using lines for the set-up namespaces and rewrites only the namespace text of the four fully qualified registrations (the three optional-feature lines and the ConnectionSaveService line); the loose usings stay. No AddOrganisationSetup (S09a).
```
Governed slices: S2, S3

#### D13

```text
D-12 The ConnectionSaveService log category changes with its namespace; no override, filter or harness reads it.
```
Governed slices: S3

#### D14

```text
D-13 Commit per slice, moves in refactor(organisation-setup) commits apart from edits; push S1-S5 together once all five are green, so the Program.cs change triggers one live connector run.
```
Governed slices: S2, S3, S5

#### D15

```text
D-14 Behaviour-free proof reuses admitted probes: architecture-rules (all rules green, set-up rules green by namespace), ef-model-unchanged, backend-tests on the unchanged-assertion API, integration, ManualSorting, BlackoutForecastShift and RecurringBlackoutRules classes, test-selection-show for Modules/OrganisationSetup paths and Program.cs. No new compare command.
```
Governed slices: S2, S3, S4, S5

#### D16

```text
D-15 Mutation bar: last, on frozen code, Stryker.NET over Modules/OrganisationSetup with the x64 runner, compared with the same files on the pre-move revision: no survivor or no-coverage mutant added in the moved code; the 80 percent project bar reported.
```
Governed slices: S5

#### D17

```text
D-16 Formal verification: no Quint, TLA+ or Agda model selected. Claims C1 membership (each listed type in exactly its listed namespace), C2 preservation (Context implies the new subject matches whenever the old one does; Place implies the new subject never matches) and C3 the boundary rules are relations over a finite closed set (compiled types x membership map x rule fields) with no state, transition, concurrency, time, fairness or unbounded input; a model over a hand-copied abstraction would be weaker than deciding the relation on the compiled assembly. Each is decided exhaustively by NUnit over the IL in the default suite. PBT not applicable (closed finite domain). Formal result NOT_SELECTED, DESIGN PASS, runtime DESIGNED_NOT_BUILT; no install or waiver needed. Default taken by the maintainer (open question 10).
```
Governed slices: S1, S4, S5

#### D18

```text
Oracle files: each slice has its own oracle file in Lighthouse.Backend.Tests/Architecture; no slice edits an earlier slice oracle or support of this feature. Therefore the S1 membership test accepts a listed type in its old or new namespace, the S1 law states preservation as an implication (it stays green after S5 narrows), and each S1 rename-list entry is an old and new literal pair that the S1 sweep accepts in either form; S2 and S3 tighten through their own oracles (only the new namespace for their group), S5 asserts the old forms are gone through its own oracle reading the S1 map read-only.
```
Governed slices: S1, S2, S3, S5

#### D19

```text
Carriers and owners, all test-side in Lighthouse.Backend.Tests/Architecture: OrganisationSetupMembership (moving types with layer, old and new namespace; staying files with owner and reason; implicit IL edges), OrganisationSetupPatternRoles (PatternDeclaration per field), OrganisationSetupStringsTheMoveRenames (old and new literal pairs), the shared NamespaceMoveLaw (decision), KernelRuleVerdict from ModuleRules (verdict and offender message), ModuleWording (failure wording). Production carriers are unchanged: the moved services, ports and the kernel Models keep their types; only namespaces change. Cross-layer algebra: domain, application, adapter and infrastructure states and operations are unchanged; the only changed observation is the type full name and namespace, observed through the rules.
```
Governed slices: S1, S2, S3, S4, S5

### Constraints

#### K1

```text
Behaviour-free: production edits are namespace lines, using lines and the namespace text of the four fully qualified Program.cs registrations only; registration order, lifetimes and pairs unchanged. Any other production edit disproves the hypothesis and returns to DESIGN.
```
Governed slices: S2, S3

#### K2

```text
No rule is loosened and no list grows except the one Tracker integration edge that the move makes cross a module boundary; lists are inline, compared both ways, and only shrink.
```
Governed slices: S1, S2, S3, S4, S5

#### K3

```text
Widen then narrow: S1 widens every Context pattern to old or new; S5 narrows it to Modules.OrganisationSetup only. Place patterns are never edited and are asserted to match no moved type.
```
Governed slices: S1, S5

#### K4

```text
Every regex or string field that matches a moving type old name carries an explicit Context or Place classification with a reason; an unclassified match fails.
```
Governed slices: S1

#### K5

```text
One oracle file per slice; an approved oracle or its support file is never edited, not even a using line; oracles name types as strings.
```
Governed slices: S1, S2, S3, S4, S5

#### K6

```text
Start condition (S1 first action): S03e, S03f, S03g and S03h merged by the three checks, the content grep deciding; Epic 6474 slice 03 not landed (no UnitCreated or UnitDeleted, TeamController still publishes TeamDeleted). A failed content or 6474 check stops S1; every count in the design is re-measured at S1 and differences are recorded.
```
Governed slices: S1

#### K7

```text
No schema, route, DTO, setting key, configuration key or usage-data event changes; no bound or range of any stored or validated input changes (none is introduced or narrowed).
```
Governed slices: S1, S2, S3, S4, S5

#### K8

```text
Test rules: the Lighthouse test-writing skill wins over nWave timing and PBT density; no nWave scenario tags in code; arch rule tests assert by the failure naming the offender, not by counts; each module rule asserts its subject set is not empty; each list position has a made-up offender in a fixture namespace such as Lighthouse.Backend.Modules.OrganisationSetupFixtures. Default per-test budgets; no derogation, no infrastructure suite.
```
Governed slices: S1, S2, S3, S4, S5

#### K9

```text
Exemptions for a moved type are exact (that type and its nested types by full name), never a whole namespace or layer; every rule edit is proved by a reverted throwaway probe recorded as a craft command.
```
Governed slices: S1, S4, S5

### Reuse

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

Extend

```text
One owner of the preservation law across module moves; set-up adds its map and pattern declarations rather than a second law.
```

PatternDeclaration

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:33

Reuse

```text
Context, Place and Composite roles already exist from S03h; set-up declares its fields with them.
```

TypesLiveInTheirLayer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:150

Reuse

```text
Layer membership rule with former-home check, used by the Forecasting and Work Items layout oracles.
```

DomainReachesOnlyTheKernelAndTheBaseLibrary

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:176

Reuse

```text
R-OS2 and the S1 Domain-candidate check.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
Rules read edges from the compiled IL; TypesIn returns nested types, so exact-set checks reduce to top-level names.
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:7

Reuse

```text
Failure wording for set-up rules.
```

ReportsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs\:1

Reuse

```text
Shape copied for OrganisationSetupMembership (namespace-agnostic membership with staying files).
```

ReportsPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Extend

```text
The module map gains one OrganisationSetupPattern beside ReportsPattern, the single owner of which namespaces are Organisation set-up; WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp reads it instead of its own loose-namespace copy.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes (D-0); the HTTP API the clients read is unchanged and every producer and consumer of the moved types is in the one deployable and changes in the same commit.
```

contract_tests

```text
Single deployable; callers of the moved types change in the same commit and are covered by the slice oracles and the unchanged acceptance tests; no persisted or wire data names a moved type.
```

### Boundaries

```text
Compiled production assembly read through LighthouseArchitecture.Production and IlDependencies (in-process); nearest executable argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture. Behaviour observed through the unchanged application-service acceptance tests and host tests.
```

#### Driven ports

```text
Production driven ports unchanged: repositories in Services/*/Repositories, LighthouseAppContext, the domain-event dispatcher, Work Items fetch fingerprint (listed edge), Tracker integration query port, the clock.
```

```text
Organisation set-up depends down only on the kernel, platform infrastructure, the repositories and Tracker integration query port; downstream contexts reach set-up Application and Domain by namespace; Tracker integration reaches set-up only along one listed edge.
```

#### Failures

None.

### Observations

```text
Architecture rule verdict over the compiled assembly: pass, or failure naming each offender type, edge or field with WHAT, WHY and HOW.
```

NUnit outcome with failure message

```text
Behaviour of the moved code (covered by the unchanged acceptance tests); references the IL does not record (event declarations, named attribute arguments).
```

```text
Namespace and full name of each listed set-up type found in the compiled assembly.
```

set of \(type\, namespace\)

```text
Source file path of a type (checked by the placement tests through the test assembly symbols).
```

### Equality

rule list comparison

```text
Two edge or type lists are equal when they hold the same full-name strings as sets, compared both ways; nested types reduce to their top-level type.
```

### Constructors

None.

### Laws

#### L1

```text
Preservation: for every declared rule field and every moved type, if the field matches the old subject then a Context field also matches the new subject and a Place field does not; an undeclared field matching either subject fails.
```

Invariant

Infrastructure

OrganisationSetupMembershipArchUnitTest

##### Treatment

Finite

###### Cases

```text
WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp, Namespace, Context: old Services.Interfaces matches, new set-up Application must match
```

```text
RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace, Namespace, Context: undeclared today, fails until declared
```

```text
RefinementModuleArchUnitTest.WeeklyRecurrenceAndItsNestedTypesPattern, FullName, Context
```

```text
RefinementModuleArchUnitTest.ServicesImplementationPattern, Namespace, role measured at S1
```

```text
TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace, Namespace, Context
```

```text
Promises place patterns, Namespace, Place: never match a new set-up namespace
```

```text
WorkItemsApplicationLayoutArchUnitTest.WhereTheTypesLivedBefore, source text, Place
```

```text
AccessLicensingOrganisationSetUpAndTheUpdateQueue, Composite of OrganisationSetUp
```

```text
Closed finite domain: compiled rule fields x 27 moved types, decided exhaustively on the IL.
```

##### Evidence

None.

### Refuted

```text
Every pattern that matches a moved type old namespace must also match its new namespace (plain widening, as in S03h).
```

```text
Carrier distinction missing: the old home is the loose Services namespace shared with types that stay, so a pattern on it may stand for the place, not the context.
```

```text
PromisesLayoutArchUnitTest.WhereTheServicesLivedBefore stands for nothing of Promises left in Services; widening it to Modules.OrganisationSetup would make it start following set-up.
```

### Claims

#### C1

```text
No existing architecture rule silently stops seeing an Organisation set-up type when it moves.
```

```text
A rule passes over an empty or shrunken subject set and a forbidden edge ships unseen.
```

OrganisationSetupMembershipArchUnitTest

```text
Compiled test assembly rule fields and the set-up membership map
```

OrganisationSetupPatternRoles declarations plus NamespaceMoveLaw

Every ArchUnit rule reading a declared field

```text
dotnet test --filter FullyQualifiedName~OrganisationSetupMembershipArchUnitTest fails naming an undeclared or unpreserved field
```

```text
Inline method-body literals (reflection cannot see them; covered only by the literal sweep); patterns built at run time from segments; rules that select types by short name.
```

### States

Admitted

```text
Moved type in its old namespace (S1) or in its new namespace (S2/S3 per group); Context field matching old and new (S1-S4) or new only (S5).
```

None.

Excluded

```text
Listed moved type in neither namespace; Context field matching old but not new; Place field matching a new set-up namespace; undeclared field matching a moved type.
```

OrganisationSetupMembershipArchUnitTest

Unknown

```text
Dependencies the IL reader does not record (event declarations, named attribute arguments) between moved types and downstream modules.
```

Epic 6264 rules owner

### Models

None.

### Closure

```text
Organisation set-up rule preservation and membership
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states fail the S1 law and membership tests by construction of the exhaustive check over the finite domain.
```

##### Evidence

None.

```text
Organisation set-up module
```

Inhabitation

Pass

DesignedNotBuilt

```text
The 27 existing files are the admitted input; the move constructs each type in its new namespace; the membership map names each one.
```

##### Evidence

None.

```text
Set-up rules
```

PublicReachability

Pass

DesignedNotBuilt

```text
Compiled assembly to membership and law to ArchUnit rules to dotnet test Architecture exit and message.
```

##### Evidence

None.

```text
Set-up rules
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no state, time, concurrency or eventual-progress claim; each rule is a synchronous finite decision.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negated: a pattern on the old home stands for the context
```

```text
Every rule verdict for every moved type
```

```text
Place patterns stop matching moved types after the move
```

```text
A Place pattern following set-up, or a Context pattern losing it
```

```text
None once every matching field is declared
```

OrganisationSetupMembershipArchUnitTest

#### Y2

Stressor

```text
Negated: a text sweep finds every pattern; patterns are regexes; registrations use short names; only usings move
```

```text
Rule subjects and DI wiring
```

```text
Behavioural law evaluation of fields, equality branch for string constants, four fully qualified Program.cs lines rewritten, usings added for implicit same-namespace resolutions
```

```text
A field with no type name escaping the law; a registration left on the old namespace
```

```text
Run-time-built names escape the sweep
```

S1 crafter and reviewer

#### Y3

Stressor

```text
Negated: S03e-S03h left the counts as measured; 6474 has not started; narrowing OrganisationSetUp keeps Work Items as strict
```

```text
Design premises
```

```text
Counts re-measured and recorded at S1; stray loose types moved to a separate StrayLooseServices constant with their own list in S5
```

```text
S1 starting on a partial merge or after 6474 slice 03; Work Items losing a forbidden edge in S5
```

```text
None
```

S1 and S5 crafters

#### Y4

Bypass

```text
IL reader misses event declarations and named attribute arguments
```

```text
Edges visible in method bodies, fields, signatures and catch clauses
```

```text
Unrecorded edges stay unguarded
```

```text
Claiming those edges are checked
```

```text
Unknown edges through event declarations or attribute arguments
```

Epic 6264 rules owner

#### Y5

SinglePointOfFailure

```text
OrganisationSetupPattern in the module map is the one owner of which namespaces are set-up
```

```text
All rules agree on set-up
```

```text
One edit narrows every reader
```

```text
A rule keeping its own loose-namespace copy
```

```text
None after S5
```

S1 and S5 crafters

#### Y6

Stressor

```text
Second set (walkthrough, not execution): reflection by name string, DI by open generic over Services.Implementation, registry discovery by namespace, persisted type names
```

```text
Runtime behaviour
```

```text
None needed
```

```text
A runtime lookup by old namespace
```

```text
None found: short-name reflection is rename-safe, no open-generic registration, registry uses DI IEnumerable, none of the 27 is persisted
```

Candidate reviewer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Context field widened to old or new, or read from the single OrganisationSetupPattern (D-7)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Context field widened to old or new, or read from the single OrganisationSetupPattern (D-7)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Context field widened to old or new, or read from the single OrganisationSetupPattern (D-7)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

Extend

```text
Context field widened to old or new, or read from the single OrganisationSetupPattern (D-7)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs

Extend

```text
Context field widened to old or new, or read from the single OrganisationSetupPattern (D-7)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupMembershipArchUnitTest
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupStringsTheMoveRenames\.cs

#### Public oracle

```text
Run the Architecture tests on the compiled assembly before any file moves: OrganisationSetupMembershipArchUnitTest evaluates the membership map, the preservation law over every declared and undeclared rule field, and the literal sweep with its fixed rename list.
```

```text
RED before the S1 rule edits: WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp is named as a Context field whose old subject (Lighthouse.Backend.Services.Interfaces) matches and whose new subject (Lighthouse.Backend.Modules.OrganisationSetup.Application) does not, and RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace (plus any other matching field) is named as undeclared. GREEN after classifying and widening: every listed type found in its old or new namespace, every Domain candidate reaching only kernel and base library, every Context field matching old and new, every Place field matching no new namespace, every rename-list literal present in old or new form and no unlisted old literal.
```

```text
A planted type with a listed name left in neither namespace, a Place field widened to Modules.OrganisationSetup, an undeclared field matching an old namespace, or an unlisted old full-name literal each fails naming the offender.
```

##### Promises

P1

#### Prefactoring

```text
Widen and classify the Context patterns (old or new) and add OrganisationSetupPattern to the module map before any production file moves; makes the S2 and S3 moves visible to every rule.
```

```text
Every existing Architecture rule verdict; production untouched.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

Extend

```text
Set-up map and pattern declarations feed the shared law.
```

OrganisationSetUp

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Extend

```text
Existing Context field widened, then read from OrganisationSetupPattern.
```

WeeklyRecurrenceAndItsNestedTypesPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:34

Extend

```text
Existing full-name Context field widened with the new full name.
```

AnImplementationNamespace

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:26

Extend

```text
Existing Context field widened with Modules.OrganisationSetup.
```

ServiceImplementationNamespace

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:12

Extend

```text
Existing Context field widened to old or set-up Application.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Changed

```text
OrganisationSetUp reads OrganisationSetupPattern (old or new); composite at 163 follows by reference; as a forbidden-unless-listed target it gets stricter.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Changed

```text
Module map gains OrganisationSetupPattern.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:34

Changed

```text
WeeklyRecurrence full-name pattern widened to the new Domain full name; ServicesImplementationPattern line 24 classified at S1.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:26

Changed

```text
AnImplementationNamespace widened with Modules.OrganisationSetup.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:12

Changed

```text
ServiceImplementationNamespace widened to old or set-up Application; already applied with ResideInNamespaceMatching.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:23

UnchangedCompatible

```text
S1 is test-side only.
```

##### Agreement

None.

##### Not applicable

None.

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
Membership: every type of the map exists in its old or new namespace; a Domain candidate reaches only the kernel and the base library; every staying file is named with owner and reason and holds a file.
```

Invariant

Infrastructure

OrganisationSetupMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
27 moving types, old or new namespace
```

```text
Domain candidates WeeklyRecurrence, RecurringBlackoutRuleExtensions
```

```text
Staying: four seeders, FeaturePositionMap pair, repositories, Models, controllers, updaters, write-back
```

```text
Implicit IL edges of the movers and of RefinementCadenceCalendar
```

```text
Closed finite set read from the IL.
```

###### Evidence

None.

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
Rules see the move before it happens: membership, preservation law with Context and Place roles, literal sweep.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupMembershipArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/WeeklyRecurrence\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WeeklyRecurrence\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/RecurringBlackoutRuleExtensions\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RecurringBlackoutRuleExtensions\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BlackoutPeriodService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RecurringBlackoutRuleService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureOrdering\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureOrdering\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureOrderingPolicyProvider\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureOrderingPolicyProvider\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureRankingService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankSeeder\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureRankSeeder\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IBlackoutPeriodService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IBlackoutPeriodService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IRecurringBlackoutRuleService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IRecurringBlackoutRuleService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureOrdering\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureOrdering\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureOrderingPolicyProvider\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureOrderingPolicyProvider\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureRankingService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureRankingService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureRankSeeder\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IFeatureRankSeeder\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/FeatureOrderingApplier\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OptionalFeatures\/FeatureOrderingApplier\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/DefaultOptionalFeatureApplier\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OptionalFeatures\/DefaultOptionalFeatureApplier\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/OptionalFeatureApplierRegistry\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OptionalFeatures\/OptionalFeatureApplierRegistry\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/IOptionalFeatureApplier\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/OptionalFeatures\/IOptionalFeatureApplier\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodServiceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/BlackoutPeriodServiceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleServiceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/RecurringBlackoutRuleServiceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/FeatureRankSeederTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureRankSeederTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/WeeklyRecurrenceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WeeklyRecurrenceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/RecurringBlackoutRuleExtensionsTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/RecurringBlackoutRuleExtensionsTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Set-up usings added; the three fully qualified optional-feature registrations rewritten (D-11)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs

Extend

```text
Nine SourceFilesThatStay entries deleted in the move commit; FeatureOrdering, IFeatureOrdering, FeatureRank, IFeatureRank prefixes dropped
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListRowsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterBacktestIntegrationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamForecastIntegrationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RecurringBlackoutRulesControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OptionalFeatureApplierSeamArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OverTimeReconstructionSeamArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/OptionalFeatureToggledEventTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/AppSettingServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureGraphReaderRelationalTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureListReaderRelationalTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeaturesWithReferenceIdsRelationalTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastConfirmationRepositoryRelationalTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastGatherReaderRelationalTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureOrderingTestHelper\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/PortfolioDto\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RecurringBlackoutRulesController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/AppSettings\/AppSettingKeys\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/ManualRankComparer\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OptionalFeatures\/OptionalFeatureKeys\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryMetricValuesProjector\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCadenceCalendar\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/AppSettingService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeaturePositionMap\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Blackout|FullyQualifiedName~Terminology|FullyQualifiedName~ManualSorting|FullyQualifiedName~OptionalFeature|FullyQualifiedName~AppSetting|FullyQualifiedName~ConnectionSave|FullyQualifiedName~ConnectionUsage|FullyQualifiedName~FeatureRank|FullyQualifiedName~WeeklyRecurrence"
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

#### Public oracle

```text
Run CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest on the compiled assembly.
```

```text
RED before the move: each of the 18 calendar, ordering and ranking and optional-feature types is named as still in Services.Implementation or Services.Interfaces. GREEN after: WeeklyRecurrence and RecurringBlackoutRuleExtensions in Modules.OrganisationSetup.Domain (as the IL decided at S1), the 12 services and ports in Modules.OrganisationSetup.Application, the 4 optional-feature types in Application.OptionalFeatures, none left in the old namespaces, and every other Architecture rule plus the unchanged behaviour tests green.
```

```text
Leaving one of the 18 in its old namespace, or placing a Domain candidate in Application, fails naming that type and its expected namespace.
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
S2 row amended: the four Refinement and Promises set-up edges leave their outbound lists in the S2 move commit (see D-8), the entries are not renamed. PromisesTypeNamePrefixes in PromisesUnitTestPlacementTest drops FeatureOrdering, IFeatureOrdering, FeatureRank and IFeatureRank in S2 as the S2 row says: the scan reads only Services paths, where no file with those prefixes is left (slice review finding 3 misread the unchanged-line note at feature-delta line 323, which concerns SourceFilesThatStay deletion in the same commit).
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
The slice is itself a preserving move; S1 widened the rules as its prefactoring.
```

reuse

```text
Every S2 target is an existing type, test or consumer moved or re-pointed as it is; no new responsibility, so nothing to reuse or create; the shared reuse rows cover the rule machinery.
```

impact

```text
Closure (per-path rows cannot cite not-yet-existing paths): 18 production files Migrated to Modules/OrganisationSetup/{Domain, Application, Application/OptionalFeatures}; 5 unit tests Migrated to Tests/Modules/OrganisationSetup; Program.cs Changed (usings, namespace text of the three optional-feature registrations at 1447-1449; order and lifetimes unchanged); PromisesUnitTestPlacementTest Changed (entries 82-90 and four prefixes at 97 leave in the move commit); every consumer target Changed in using lines or renamed old-name literals only; csproj, suppressions, editorconfig, appsettings and CI targets UnchangedCompatible.
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
Each of the 18 group types lives only in its new set-up namespace.
```

Invariant

Infrastructure

CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest

###### Treatment

Finite

###### Cases

```text
2 Domain types
```

```text
12 Application services and ports
```

```text
4 Application.OptionalFeatures types
```

```text
Closed set of 18 type names read from the IL.
```

###### Evidence

None.

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
Calendar, ordering and ranking and optional features live in Modules/OrganisationSetup in their layer.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/AppSettingService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/AppSettingService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/TerminologyService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/TerminologyService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IAppSettingService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IAppSettingService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/ITerminologyService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/ITerminologyService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Connections\/ConnectionSaveService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/ChangedConnectionDefault\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Connections\/ChangedConnectionDefault\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/WorkTrackingSystemConnectionUsage\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingSystemConnectionUsage\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/IConnectionSaveService\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IConnectionSaveService\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/IWorkTrackingSystemConnectionUsage\.cs

Create

```text
Moved as it is, namespace = folder (D-1)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWorkTrackingSystemConnectionUsage\.cs

Extend

```text
Old file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/AppSettingServiceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/AppSettingServiceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/TerminologyServiceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyServiceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveServiceTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Connections\/ConnectionSaveServiceTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/Connections\/WorkTrackingSystemConnectionUsageTest\.cs

Create

```text
Unit test moves beside its subject (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingSystemConnectionUsageTest\.cs

Extend

```text
Old test file removed by the move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Set-up usings added; the fully qualified ConnectionSaveService registration rewritten (D-11)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
EdgesOfTheModuleIntoOtherContexts gets the one ConnectionHealthCadence to IAppSettingService entry (D-8)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/OwnerSettingRefusalsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SurveyNudgeControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TerminologyControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionFetchShapeChangedSinglePublisherArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RefreshLogServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/SystemInfoServiceTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/OwnerSettingRefusals\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TerminologyController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthCadence\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/SystemInfoService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/InstallTimestampSeeder\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
Consumer: using line or renamed old-name literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Blackout|FullyQualifiedName~Terminology|FullyQualifiedName~ManualSorting|FullyQualifiedName~OptionalFeature|FullyQualifiedName~AppSetting|FullyQualifiedName~ConnectionSave|FullyQualifiedName~ConnectionUsage|FullyQualifiedName~FeatureRank|FullyQualifiedName~WeeklyRecurrence"
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

#### Public oracle

```text
Run SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest on the compiled assembly.
```

```text
RED before the move: each of the 9 settings, terminology and connection types is named as still in the old namespaces. GREEN after: AppSettingService, TerminologyService and their two ports in Modules.OrganisationSetup.Application; ConnectionSaveService, ChangedConnectionDefault, WorkTrackingSystemConnectionUsage and their two ports in Application.Connections; none left in the old namespaces; TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts holds the ConnectionHealthCadence to IAppSettingService edge; every other Architecture rule and the unchanged behaviour tests green.
```

```text
Leaving one of the 9 in its old namespace fails naming it; omitting the Tracker integration edge fails the Tracker integration rule naming ConnectionHealthCadence.
```

##### Promises

P2

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

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
The slice is itself a preserving move; S1 widened the rules as its prefactoring.
```

reuse

```text
Every S3 target is an existing type, test or consumer moved or re-pointed as it is (TypesLiveInTheirLayer and the shared rule machinery are reused by the oracle); no new responsibility to reuse or create.
```

impact

```text
Closure (per-path rows cannot cite not-yet-existing paths): 9 production files Migrated to Modules/OrganisationSetup/{Application, Application/Connections}; 4 unit tests Migrated (including API/TerminologyServiceTest); Program.cs Changed (usings, namespace text of the ConnectionSaveService registration at 1464); TrackerIntegrationModuleBoundariesArchUnitTest Changed (one ConnectionHealthCadence to IAppSettingService entry at 142); ConnectionSaveService log category Changed with its namespace; every consumer target Changed in using lines or renamed old-name literals only.
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
Each of the 9 group types lives only in its new set-up namespace.
```

Invariant

Infrastructure

SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest

###### Treatment

Finite

###### Cases

```text
4 Application types
```

```text
5 Application.Connections types
```

```text
Closed set of 9 type names read from the IL.
```

###### Evidence

None.

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
Settings, terminology and connections live in Modules/OrganisationSetup in their layer; Tracker integration reaches set-up only along its listed edge.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

Create

```text
R-OS1..R-OS6 on ModuleRules with IL baselines (D-9)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupUnitTestPlacementTest\.cs

Create

```text
Moved unit tests stay beside the module (D-10)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs

Extend

```text
Planted offenders for the set-up rules
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Set-up Application and Domain legal by namespace; listed set-up edges leave (D-8); Tracker integration keeps its one edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupIsAnOpenHostArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupModuleBoundariesArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupUnitTestPlacementTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run OrganisationSetupIsAnOpenHostArchUnitTest on the compiled test and production assemblies.
```

```text
RED while WorkItemsModuleBoundariesArchUnitTest.EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue holds the RefreshLogService to IAppSettingService and WorkItemService to IFeatureOrdering edges or Promises outbound list holds DeliveryMetricValuesProjector to IBlackoutPeriodService, naming each set-up edge still listed. GREEN after both modules admit set-up Application and Domain by namespace and the three entries leave, and TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts holds exactly the ConnectionHealthCadence to IAppSettingService edge.
```

```text
A planted Tracker integration type calling IAppSettingService fails the Tracker integration rule; a planted controller constructing BlackoutConfigurationChanged fails R-OS5; a planted Work Items type calling a set-up adapter fails; a second Tracker integration edge or an emptied list fails the exact-list check.
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
S4 row amended: S4's RED no longer names the Refinement edges or the Promises DeliveryMetricValuesProjector to IBlackoutPeriodService edge (already dropped in S2 per D-8); it names the Work Items entries it already names and the exact Tracker integration list. S4 gives Refinement, Promises, Access, Licensing and Reports the set-up namespace allowance, each guarded by a planted falsifier.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
Test-side rule additions and list shrinks only; no preserving move precedes them.
```

reuse

```text
New rule files copy existing shapes (ModuleRulesCatchANewOffenderArchUnitTest falsifiers, TrackerIntegrationUnitTestPlacementTest placement, ModuleRules rules, recorded in the shared reuse); extended rule files keep their rules and only gain the set-up allowance or lose listed edges.
```

impact

```text
Closure (two targets are new files): OrganisationSetupModuleBoundariesArchUnitTest and OrganisationSetupUnitTestPlacementTest created; WorkItemsModuleBoundariesArchUnitTest (list at 148 loses two set-up edges), PromisesModuleBoundariesArchUnitTest (145 loses one; planted fixture 410/755 follows), Flow metrics, Forecasting, Refinement and Reports rules (set-up admitted by namespace), ModuleBoundariesArchUnitTest, ModuleRulesCatchANewOffenderArchUnitTest (planted offenders) Changed; TrackerIntegrationModuleBoundariesArchUnitTest Changed only to assert its exact one edge; production TeamController:117 UnchangedCompatible (TeamDeleted listed by R-OS5).
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

###### L5

```text
Downstream modules reach set-up Application and Domain without a list entry; Tracker integration reaches set-up only along exactly one listed edge; set-up events are constructed only in set-up Application except the listed TeamController edge.
```

Invariant

Application

OrganisationSetupIsAnOpenHostArchUnitTest

###### Treatment

Finite

###### Cases

```text
Work Items list holds no set-up edge
```

```text
Promises list holds no set-up edge
```

```text
Tracker integration list equals the ConnectionHealthCadence edge
```

```text
Planted Tracker integration caller fails
```

```text
Planted controller publishing BlackoutConfigurationChanged fails
```

```text
Finite edge sets read from the IL and inline lists compared both ways.
```

###### Evidence

None.

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
Organisation set-up is an open host: downstream by namespace, Tracker integration along one listed edge.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupIsAnOpenHostArchUnitTest
```

###### T5

```text
R-OS1..R-OS5 set-up boundary rules and unit-test placement.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupModuleBoundariesArchUnitTest
```

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Context field narrowed to Modules.OrganisationSetup; stray loose types to their own constant and list where still reached
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Context field narrowed to Modules.OrganisationSetup; stray loose types to their own constant and list where still reached
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Context field narrowed to Modules.OrganisationSetup; stray loose types to their own constant and list where still reached
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

Extend

```text
Context field narrowed to Modules.OrganisationSetup; stray loose types to their own constant and list where still reached
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs

Extend

```text
Context field narrowed to Modules.OrganisationSetup; stray loose types to their own constant and list where still reached
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Architecture documents show Organisation set-up packaged (prose, reviewed, no test)
```

ARCHITECTURE\.md

Extend

```text
Architecture documents show Organisation set-up packaged (prose, reviewed, no test)
```

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md

Extend

```text
Architecture documents show Organisation set-up packaged (prose, reviewed, no test)
```

docs\/feature\/epic\-6264\-modular\-monolith\/slices\/slice\-45\-s03i\-package\-organisation\-set\-up\.md

Extend

```text
Architecture documents show Organisation set-up packaged (prose, reviewed, no test)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupRulesNameOnlyTheModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run OrganisationSetupRulesNameOnlyTheModuleArchUnitTest: the S1 law and sweep evaluated with an empty rename and allowance list over the test project, excluding by exact path only itself, OrganisationSetupMembership.cs, OrganisationSetupMembershipArchUnitTest.cs, OrganisationSetupPatternRoles.cs, OrganisationSetupStringsTheMoveRenames.cs and the planted-offender fixtures.
```

```text
RED at S5 start, naming each Context field still matching an old namespace or full name of a moved type and each literal still naming an old full name or path. GREEN once every Context field names Modules.OrganisationSetup only and every old literal is renamed; the exclusion list equals exactly those paths and each exists; Place fields still match no moved type.
```

```text
Re-adding an old Services alternative to a Context field, an old full-name literal in a rule file, or an extra path in the exclusion list fails naming it.
```

##### Promises

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

Reuse

```text
The oracle reuses the S1 law and map read-only with an empty list.
```

WeeklyRecurrenceAndItsNestedTypesPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:34

Extend

```text
Context field narrowed to Modules.OrganisationSetup only.
```

AnImplementationNamespace

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:26

Extend

```text
Context field narrowed to Modules.OrganisationSetup only.
```

ServiceImplementationNamespace

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:12

Extend

```text
Context field narrowed to Modules.OrganisationSetup only.
```

ReportsPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Extend

```text
Context field narrowed to Modules.OrganisationSetup only.
```

OrganisationSetUp

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Extend

```text
Narrowed; stray loose types still reached get their own StrayLooseServices constant and list.
```

Organisation set\-up

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:63

Extend

```text
Existing architecture document brought to current: set-up packaged in Modules/OrganisationSetup.
```

Organisation set\-up

ARCHITECTURE\.md\:90

Extend

```text
Existing architecture document brought to current: set-up packaged in Modules/OrganisationSetup.
```

Organisation set\-up

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md\:197

Extend

```text
Existing architecture document brought to current: set-up packaged in Modules/OrganisationSetup.
```

Organisation set\-up

docs\/feature\/epic\-6264\-modular\-monolith\/slices\/slice\-45\-s03i\-package\-organisation\-set\-up\.md\:1

Extend

```text
Existing architecture document brought to current: set-up packaged in Modules/OrganisationSetup.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Changed

```text
OrganisationSetUp narrowed; stray loose types still reached move to StrayLooseServices with their own list; no loosening.
```

docs

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:62

Changed

```text
Row 62 packaged, row 63 note, line 289, Left open 845-851 brought to current; ARCHITECTURE.md section 4 row and rules bullet; catalogue and brief.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:34

Changed

```text
Context field narrowed; S1 widening removed.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:26

Changed

```text
Context field narrowed; S1 widening removed.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:12

Changed

```text
Context field narrowed; S1 widening removed.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Changed

```text
Context field narrowed; S1 widening removed.
```

docs

ARCHITECTURE\.md\:90

Changed

```text
Prose updated to show set-up packaged; reviewed, no test.
```

docs

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md\:197

Changed

```text
Prose updated to show set-up packaged; reviewed, no test.
```

docs

docs\/feature\/epic\-6264\-modular\-monolith\/slices\/slice\-45\-s03i\-package\-organisation\-set\-up\.md\:1

Changed

```text
Prose updated to show set-up packaged; reviewed, no test.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Narrowing rule fields and updating documents; no preserving move precedes them.
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

###### L6

```text
No Context field matches an old subject of a moved type and no test-project literal names an old full name or path of the 27, outside the exact self-exclusion list.
```

Invariant

Infrastructure

OrganisationSetupRulesNameOnlyTheModuleArchUnitTest

###### Treatment

Finite

###### Cases

```text
Each Context field of the S1 declarations
```

```text
Each S1 rename-list literal
```

```text
Exclusion list equals the named paths and each exists
```

```text
Finite fields and files of the test project.
```

###### Evidence

None.

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

###### T6

```text
Rules name Organisation set-up by Modules.OrganisationSetup only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OrganisationSetupRulesNameOnlyTheModuleArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
