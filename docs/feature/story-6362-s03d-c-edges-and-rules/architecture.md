# Architecture brief: story\-6362\-s03d\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (feature-delta D-8, Ratchet design New rules C1): Promises gets its own module rules in Architecture/PromisesModuleBoundariesArchUnitTest.cs, R-P1..R-P7, written with the shared ModuleRules (its PromisesModule namespace constant already exists, ModuleRules.cs:27) and a Promises ModuleWording built in the oracle from two strings, as ForecastingModuleBoundariesArchUnitTest does (ModuleWording.cs:7 already takes the name and the legal callers), so ModuleRules.cs and ModuleWording.cs stay unchanged. Every list is inline, measured from the compiled IL and compared both ways; designed baselines: R-P1 the D-1 lists (types in their layer, none left under Services); R-P2 Domain reaches only Models and the base library, empty; R-P3 outside to Domain, empty; R-P4 outside to Application besides the API and the composition root: PortfolioUpdater; R-P5 outside to Application/Policies besides the composition root: empty; R-P6 Application reaches no EF Core, Data, API or any module's Adapters, Work Items and Tracker integration Application legal by namespace, Forecasting and Flow metrics Application legal if found, platform only through the list ILighthouseClock, IDeliveryRepository, IDeliveryMetricSnapshotRepository, RecordableDeliveries and the dispatcher contract, by name; R-P7 no other module's Adapters, update code, write-back round, Access or Licensing, empty. A neighbour without a namespace is listed by type name. Anything else the IL finds is added only with a reason.
```
Governed slices: S1

#### D2

```text
Confirmed (Ratchet design, D-7 edge Promises to Tracker integration option b): Promises' Application is legal by namespace as an inbound caller in the Work Items and Tracker integration rules, so their lists lose exactly the three Promises entries: WorkItemsModuleBoundariesArchUnitTest.OtherOutsideTypesReachingTheApplication loses Modules.Promises.Application.DeliveryRuleService (line 129); TrackerIntegrationModuleBoundariesArchUnitTest.OtherOutsideTypesReachingTheApplication loses Modules.Promises.Application.DeliveryForecastPublishingService and DeliverySourceResolver (lines 103-104). Promises may reach Tracker integration Application, never its Adapters. No other entry or pattern of those files changes in C1; the R-T7 edge list stays one entry until C2.
```
Governed slices: S1, S2

#### D3

```text
Confirmed (D-7 option b, D-10): the Tracker integration to Promises edge is inverted. New Modules/TrackerIntegration/Application/IDescriptionBlockMerger.cs declares one method with the renderer's measured signature kept exactly: string MergeInto(string? existingDescription, string blockText) (IDeliveryForecastBlockRenderer.cs:25; the authority's shorthand parameter name existing is not used). MergeInto leaves IDeliveryForecastBlockRenderer, which keeps Render only; DeliveryForecastBlockRenderer implements both interfaces and keeps Render and MergeInto in one class with unchanged bodies. JiraWorkTrackingConnector's fifth constructor parameter (JiraWorkTrackingConnector.cs:26) changes type from IDeliveryForecastBlockRenderer to IDescriptionBlockMerger, its call at line 3968 and its parameter name stay, and its using of Modules.Promises.Application goes. Program.cs keeps line 1474 and the order of every line and adds one registration AddScoped<IDescriptionBlockMerger, DeliveryForecastBlockRenderer>() beside it; the connector is built only by DI (Program.cs:1493 via WorkTrackingConnectorFactory). The class is stateless and pure, so two scoped instances behave the same. No AddPromises extension. Carrier: the port is a Tracker integration Application interface owned by that module; the implementation is owned by Promises Application; no per-process state.
```
Governed slices: S2

#### D4

```text
Confirmed (D-7, Ratchet design R-P8): after C2 Tracker integration reaches no Promises type. Test-side lists that hold the edge follow it in C2: TrackerIntegrationModuleBoundariesArchUnitTest.EdgesOfTheModuleIntoOtherContexts (line 135) goes to empty, and TheJiraConnectorMergingTheForecastBlock (line 140) with the test that pins that one edge (line 229) follow; TrackerIntegrationMembershipArchUnitTest's case (JiraWorkTrackingConnector, IDeliveryForecastBlockRenderer, true) at line 195 follows; TrackerIntegrationMembership.Moves gains IDescriptionBlockMerger, because the membership test names any unlisted type under Modules.TrackerIntegration. Made-up offender fixtures in that file that name IDeliveryForecastBlockRenderer still compile and stay. The test constructions that pass new DeliveryForecastBlockRenderer() to the connector compile unchanged (the class implements the port); only JiraDeliverySourceProviderTest's service registration (line 1137) changes so DI can build the connector (D-9).
```
Governed slices: S2

#### D5

```text
Confirmed (D-9, D-1, D-4, D-5, D-6): Architecture/PromisesUnitTestPlacementTest.cs checks that every unit test of a Promises type (the PromisesMembership types) lives under Tests/Modules/Promises in the sub-folder of its subject, and that no Promises source file is left under Services apart from the named exceptions with their owners: the Delivery repositories (DeliveryRepository, DeliveryMetricSnapshotRepository, IDeliveryRepository, IDeliveryMetricSnapshotRepository, RecordableDeliveries; repository split owner S05d), the write-back round (owner #6657), the update code (S03f, Epic 6474) and feature ordering and ranking (S03i). Integration, acceptance and controller tests stay where they are. Modelled on TrackerIntegrationUnitTestPlacementTest and ForecastingUnitTestPlacementTest. Measured today: the nine Promises unit tests already sit under Tests/Modules/Promises; DeliverySourceBindabilityTest tests a kernel Models type and is not a Promises unit test; no file moves in C3.
```

#### D6

```text
Confirmed (D-0, D-12, P5): behaviour-free. No schema, entity, migration, route, DTO, forecast, Delivery answer, handler order or DI lifetime changes, and the bytes written to Jira stay the same. Production changes only in C2. Behaviour-preservation evidence for C2, unchanged and green: the renderer merge tests DeliveryForecastBlockRendererTest and DeliveryForecastBlockRendererBeyondTheHorizonTest, the Jira write-back unit tests JiraBatchedWriteBackTest and JiraQuietWriteBackTest, and the connector publishing tests JiraDeliverySourceProviderTest and JiraDeliveryForecastPublisherTest; the live JiraWriteBackTest and JiraSuppressionRetryIntegrationTest run in CI on the push. EXAMINE observes through admitted probes only (architecture-rules, test-selection-show, ci-live-suites-show, ef-model-unchanged, the answers-compare probes) and names up front the four blocked-period reactions refresh-reactions-compare cannot observe.
```
Governed slices: S1, S2

#### D7

```text
Claim-to-checker: every claim of feature C is a finite fact of the compiled IL or of unchanged pure code: namespace placement, the exact set of edges per rule, the location of each test file, and that MergeInto's body is unchanged. Each is decided exactly by its IL architecture oracle (Finite laws) with a made-up offender per rule as falsifier, and by the existing example tests for the merge. No Quint, TLA+ or Agda model: no state machine, ordering, concurrency, recovery or progress claim changes (no handler, queue or schedule moves), and a model would restate the namespace map and could not fail. Liveness is not applicable: nothing waits for progress. Types: IDescriptionBlockMerger narrows what the connector can call to the one merge it uses, checked by the C# compiler; property tests add nothing over a finite edge set.
```
Governed slices: S1, S2

#### D8

```text
Confirmed (maintainer 2026-11-17): rigor thorough for feature C (architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet); C2 stays as the last edit slice of feature C; push per slice with CI watched, rebase before push allowed; no review window; mutation once at the end of S03d on frozen code (D-13), then finalize. C2 adds a file under Modules/TrackerIntegration/Application and edits Program.cs, so its push runs every live suite (D-11).
```
Governed slices: S1, S2

### Constraints

#### K1

```text
Rule lists only shrink and are never loosened: inline full-name lists compared both ways against the IL (IlDependencies, which misses catch-clause, event and attribute-named-argument references); exemptions are exact per type and its nested types, never a namespace or layer; holdings compare top-level types (nested names reduced to the part before the plus sign). Each rule edit is proved by a throwaway probe that is reverted.
```
Governed slices: S1, S2

#### K2

```text
One oracle file per slice; an approved oracle file is never edited, not even a using line; a crafter target is never an oracle support; oracles name types as strings. Made-up offenders live in a fixture namespace such as Lighthouse.Backend.Modules.PromisesFixtures. Rule tests assert by the failure naming the offender, not by counts. No nWave scenario tags. The oracles are IL architecture tests and start no host.
```
Governed slices: S1, S2

#### K3

```text
Input bounds unchanged: MergeInto takes a nullable description of unbounded length and a block text of unbounded length, as today; the authority states no bound and C2 adds none. Scope out: no rename or signature change beyond the extracted port; repositories, update code, write-back round, ordering and ranking stay.
```
Governed slices: S2

#### K4

```text
Order: S2 and S3 run after S1. S1's Tracker integration rule file holds the one edge C2 empties, and the placement check relies on the Promises rule set being in place.
```
Governed slices: S2

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Every producer and consumer of IDescriptionBlockMerger and of the rule lists is in one deployable and changes in the same commit; the compiler and the primary oracles cover them, and no persisted or wire data changes.
```

### Boundaries

```text
The compiled Lighthouse.Backend assembly read through IlDependencies by the NUnit architecture oracles (dotnet test on Lighthouse.Backend.Tests); for C2 behaviour, JiraWorkTrackingConnector's description publishing driven by the existing connector tests through a fake HTTP handler
```

#### Driven ports

```text
IDescriptionBlockMerger (Tracker integration Application) implemented by DeliveryForecastBlockRenderer (Promises Application)
```

```text
Promises Application may reach Tracker integration Application and Work Items Application by namespace; Tracker integration reaches no Promises type; Promises Domain reaches only Models and the base library; nothing outside reaches Promises Application/Policies except the composition root
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
Promises module boundaries and the inverted merge edge
```

Safety

Pass

DesignedNotBuilt

```text
Each rule's inline list is compared both ways with the IL and a made-up offender per rule must be named; R-P8 empty excludes any Tracker integration to Promises edge
```

##### Evidence

None.

```text
Promises module boundaries and the inverted merge edge
```

Inhabitation

Pass

DesignedNotBuilt

```text
DeliveryForecastBlockRenderer is a concrete IDescriptionBlockMerger constructible with no arguments; DI builds the connector from Program.cs registrations
```

##### Evidence

None.

```text
Promises module boundaries and the inverted merge edge
```

PublicReachability

Pass

DesignedNotBuilt

```text
The connector's publish path calls MergeInto at JiraWorkTrackingConnector.cs:3968 through the port, reached from WorkTrackingConnectorFactory and exercised by the existing connector publishing tests
```

##### Evidence

None.

```text
Promises module boundaries and the inverted merge edge
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no waiting, queue, schedule or progress behaviour changes; every check is a finite synchronous read of the IL
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: the extracted port keeps the bytes written to Jira
```

```text
MergeInto body and markers unchanged in one class with Render
```

```text
Only the declaring interface and the connector's parameter type change
```

```text
Any change to merged description text
```

```text
Renderer merge tests and Jira publishing tests unchanged and green; the Jira live suite in CI
```

crafter

#### Y2

Bypass

```text
Assumption negated: IL sees every reference
```

```text
Rules over IL method bodies, signatures and fields
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

#### Y3

Stressor

```text
Assumption negated: DI resolves the connector after the parameter type change
```

```text
Program.cs registers IDescriptionBlockMerger and every existing line in order
```

```text
One added registration
```

```text
A missing registration that fails only at runtime
```

```text
Host-starting tests and the full CI suite resolve the connector; JiraDeliverySourceProviderTest builds it from DI
```

crafter

#### Y4

Stressor

```text
Second set, walkthrough: a new type added under Modules.TrackerIntegration fails the membership list
```

```text
Membership lists name every module type
```

```text
IDescriptionBlockMerger added to TrackerIntegrationMembership.Moves in C2
```

```text
A namespace-wide exemption
```

```text
None found beyond that list
```

crafter

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
Its inbound list loses DeliveryRuleService (line 129) and its legal callers take Promises Application by namespace
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Its inbound list loses DeliveryForecastPublishingService and DeliverySourceResolver (lines 103-104) and its legal callers take Promises Application by namespace; the edge list R-T7 stays for C2
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesModuleBoundariesArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run PromisesModuleBoundariesArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly's made-up offenders: R-P1..R-P7 through ModuleRules with a Promises ModuleWording, and the inbound lists of the Work Items and Tracker integration rule files read by field name
```

```text
Every Promises type lives in its layer and none is left under Services; each rule's measured inline list equals the IL both ways (R-P4 holds PortfolioUpdater, R-P6 the five platform types by name, the others empty); the Work Items and Tracker integration inbound lists name no Promises type, Promises Application being legal there by namespace; each rule names its made-up offender in the Promises fixture namespace; no other test changes outcome
```

```text
Today the Work Items list names DeliveryRuleService and the Tracker integration list names DeliveryForecastPublishingService and DeliverySourceResolver, so the oracle is RED naming them; a list entry the IL does not hold, or an edge the list misses, fails naming it; a rule that does not name its made-up offender fails
```

##### Promises

P1

P2

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:129

Reuse

```text
Shared rule set (TypesLiveInTheirLayer, DomainReachesOnlyTheKernelAndTheBaseLibrary, the inbound and adapter rules) with the PromisesModule constant at line 27 already present; the Forecasting and Work Items oracles use it unchanged
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:7

Reuse

```text
Already built from a module name and its legal callers in words; the oracle builds the Promises wording as ForecastingModuleBoundariesArchUnitTest does (line 63), so no new wording type or static member is needed
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:1

Reuse

```text
Reads the compiled IL for every module rule; one reader keeps the measured lists comparable with the other modules
```

OtherOutsideTypesReachingTheApplication

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:124

Extend

```text
The existing inbound list of Work Items shrinks by one entry; a second list would duplicate the inbound rule
```

OtherOutsideTypesReachingTheApplication

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:97

Extend

```text
The existing inbound list of Tracker integration shrinks by two entries; a second list would duplicate the inbound rule
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:129

Changed

```text
One Promises entry removed; Promises legal by namespace
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:103

Changed

```text
Two Promises entries removed; Promises legal by namespace
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:27

UnchangedCompatible

```text
PromisesModule constant and the rules are used as they are
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:7

UnchangedCompatible

```text
Constructed by the oracle from two strings
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:20

UnchangedCompatible

```text
PromisesPattern already sees Modules.Promises since feature A
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs\:1

UnchangedCompatible

```text
No production file changes in C1; the rules only read the IL
```

##### Agreement

None.

##### Not applicable

agreement

```text
No schema, wire format or shared configuration changes; the inbound lists are private to their test classes and keep their field names, which the new oracle may read by name
```

prefactoring

```text
Test-side list edits and a new oracle file; no production structure changes and no preserving move makes C1 easier
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
For each rule R-P1..R-P7 the set of offending edges the IL shows equals the rule's inline list, and the Work Items and Tracker integration inbound lists hold no Promises type
```

Invariant

Application

PromisesModuleBoundariesArchUnitTest

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
a made-up offender per rule in the Promises fixture namespace is named
```

```text
a Promises type left on a Work Items or Tracker integration list is named
```

```text
The compiled assembly holds a finite set of types and edges; exact comparison decides each rule and generation adds no coverage
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

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
Promises keeps its shape: each type in its layer, Domain reaches only the kernel and the base library, outsiders reach Domain, Application and Policies only as listed, Application reaches no EF Core, Data, API, other Adapters, update code, write-back round, Access or Licensing except as listed; Work Items and Tracker integration take Promises by namespace and list no Promises type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesModuleBoundariesArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDescriptionBlockMerger\.cs

Create

```text
The port Tracker integration defines for merging a block into a tracker description
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastBlockRenderer\.cs

Extend

```text
MergeInto leaves it; Render stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs

Extend

```text
Implements IDescriptionBlockMerger as well; bodies unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
Constructor parameter type becomes IDescriptionBlockMerger; Promises using removed
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
One added registration of the renderer as IDescriptionBlockMerger
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
The edge list into other contexts goes to empty and the test pinning the one edge follows
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

Extend

```text
The design-edge case for the connector and the renderer no longer holds
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs

Extend

```text
Moves gains IDescriptionBlockMerger, a new type under Modules.TrackerIntegration
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs

Extend

```text
Its service registration (line 1137) must offer IDescriptionBlockMerger so DI builds the connector
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationReachesNoPromisesArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run TrackerIntegrationReachesNoPromisesArchUnitTest over the compiled assembly: collect every IL edge from a Tracker integration type into Modules.Promises, and read where IDescriptionBlockMerger lives, its one method, who implements it and the Jira connector's constructor parameter types
```

```text
No Tracker integration type reaches a Promises type; IDescriptionBlockMerger lives in Modules.TrackerIntegration.Application with string MergeInto(string?, string); DeliveryForecastBlockRenderer implements it; JiraWorkTrackingConnector takes IDescriptionBlockMerger; IDeliveryForecastBlockRenderer no longer declares MergeInto; the renderer merge tests and Jira publishing and write-back unit tests pass unchanged
```

```text
Today JiraWorkTrackingConnector reaches IDeliveryForecastBlockRenderer, so the oracle is RED naming that edge and the missing port; a made-up Tracker integration type reaching a Promises type is named; any change to the merged text fails the unchanged renderer merge tests
```

##### Promises

P3

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

DeliveryForecastBlockRenderer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs\:9

Extend

```text
The one owner of the block's markers keeps Render and MergeInto together and implements the port
```

IDeliveryForecastBlockRenderer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastBlockRenderer\.cs\:10

Extend

```text
Keeps Render for Promises' publishing service; MergeInto moves to the port so no client depends on an operation it does not use
```

JiraWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:21

Extend

```text
Its constructor parameter type changes; the merge call at line 3968 is unchanged
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1474

Extend

```text
The composition root registers the renderer once more for the port
```

IDescriptionBlockMerger

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDescriptionBlockMerger\.cs\:1

CreateNew

```text
No Tracker integration port owns merging a block into a description: IWriteBackTarget, IDeliveryForecastPublisher and IDeliverySourceProvider were consulted and name other jobs; handing merged text in needs a new connector read of the current description, and moving the renderer splits Render from MergeInto, which share the markers
```

EdgesOfTheModuleIntoOtherContexts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:135

Extend

```text
The existing edge list of Tracker integration goes to empty; the oracle checks the IL itself, so no second list is made
```

AnEdgeNamedAtDesign\_IsInTheCompiledCode

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:200

Extend

```text
The existing design-edge cases follow the inversion
```

TrackerIntegrationMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:11

Extend

```text
The one membership list of Tracker integration gains the new port
```

AddScoped

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs\:1137

Extend

```text
The existing test service registration offers the port so DI builds the connector
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:135

Changed

```text
Edge list into other contexts emptied
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:195

Changed

```text
Design-edge case follows the inversion
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:36

Changed

```text
New port listed
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDescriptionBlockMerger\.cs\:1

Changed

```text
New port
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastBlockRenderer\.cs\:25

Changed

```text
MergeInto removed
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs\:9

Changed

```text
Implements the port
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:26

Changed

```text
Parameter type
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1474

Changed

```text
One added registration; order kept
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs\:1137

Changed

```text
DI registration offers the port
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs\:12

UnchangedCompatible

```text
Calls Render only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactory\.cs\:30

UnchangedCompatible

```text
Resolves the connector from DI
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs\:44

UnchangedCompatible

```text
Mocks IDeliveryForecastBlockRenderer without MergeInto
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs\:45

UnchangedCompatible

```text
Calls MergeInto on the concrete class; behaviour evidence
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererBeyondTheHorizonTest\.cs\:71

UnchangedCompatible

```text
Calls MergeInto on the concrete class; behaviour evidence
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:38

UnchangedCompatible

```text
TypesLiveInTheirLayer checks listed types only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JiraConnectorTestSetup\.cs\:61

UnchangedCompatible

```text
Passes new DeliveryForecastBlockRenderer(), which converts to the port
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactoryTest\.cs\:27

UnchangedCompatible

```text
Passes new DeliveryForecastBlockRenderer(), which converts to the port
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBatchedWriteBackTest\.cs\:318

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs\:1482

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDependencyDogfoodTest\.cs\:75

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraLinkTypeDogfoodTest\.cs\:132

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesLiveTest\.cs\:246

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDirectionDogfoodTest\.cs\:167

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDogfoodTest\.cs\:178

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraQuietWriteBackTest\.cs\:337

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraScopedTokenIntegrationTest\.cs\:178

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSuppressionRetryIntegrationTest\.cs\:173

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorAuthDelegationTest\.cs\:172

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs\:916

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWriteBackTest\.cs\:651

UnchangedCompatible

```text
Constructs the connector with new DeliveryForecastBlockRenderer(), which converts to the port; using line still needed
```

##### Agreement

MergeInto\(string\? existingDescription\, string blockText\) returning the description to write

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs\:90

Producer

Migrated

```text
Same body, now declared by IDescriptionBlockMerger
```

MergeInto\(string\? existingDescription\, string blockText\) returning the description to write

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:3968

Consumer

Migrated

```text
Calls the same method through the port
```

MergeInto\(string\? existingDescription\, string blockText\) returning the description to write

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs\:45

Consumer

UnchangedCompatible

```text
Calls the concrete class; unchanged
```

IDeliveryForecastBlockRenderer\.Render

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs\:12

Consumer

UnchangedCompatible

```text
Uses Render only
```

##### Not applicable

prefactoring

```text
The extraction is the change itself and is small; the unchanged renderer merge tests and Jira tests preserve behaviour across it, so no separate preserving move helps
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
The set of IL edges from Tracker integration types into Promises types is empty, and the text MergeInto returns is the same for every input as before the extraction
```

Invariant

Adapter

TrackerIntegrationReachesNoPromisesArchUnitTest

###### Treatment

Finite

###### Cases

```text
the Jira connector reaching IDeliveryForecastBlockRenderer is named
```

```text
a made-up Tracker integration type reaching a Promises type is named
```

```text
the renderer merge examples (replace in place, append on broken markers, idempotent rewrite) return the same text
```

```text
Edges are a finite set read exactly from the IL; the merge body does not change, so its existing example tests are the preservation check and a generator would test the old code against itself
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs

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
Tracker integration reaches no Promises type; the merge port lives in Tracker integration Application and the renderer implements it
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationReachesNoPromisesArchUnitTest"
```

##### Contract tests

None.

## ADRs

None.
