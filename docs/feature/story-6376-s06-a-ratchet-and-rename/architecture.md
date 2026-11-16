# Architecture brief: story\-6376\-s06\-a\-ratchet\-and\-rename

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented (CLAUDE.md). Both slices are behaviour-free: S1 adds an architecture rule over compiled IL in the test project, S2 renames identifiers on the connector port and its five connectors. No domain decision moves, so no new domain owner, value object or aggregate is introduced; the rule core is a test-side static rule class like IlDependencies and ModuleRules.
```

#### D2

```text
Confirmed from the DESIGN authority (feature-delta.md, AFK decisions 2026-11-16): D-0 behaviour-free scope, D-1 option a (two nwdes features, this is A), D-6 rename, D-7 ratchet from the compiled IL with an inline two-way list, D-10 mechanical test edits, D-11 EXAMINE adds connector-throws-count. Removing throws and the capability interfaces stay in feature B.
```

#### D3

```text
Formal verification: quint 0.33.1 is installed, but no claim selects a checker. The S1 claim (no population method creates a NotSupportedException outside the list) is decided exactly by an executed scan of the finite compiled IL; the S2 claim (no Project name for a Portfolio on the port and its implementers) by reflection over the finite built types; behaviour preservation of a pure rename by the compiler plus the unchanged existing suite. No state, transition, ordering, concurrency or recovery is added, so no Quint, TLA+ or Agda model is written. Liveness: not applicable, both slices add no process that must make progress; every check is a single synchronous pass over a loaded assembly.
```

#### D4

```text
Property-based testing: not applicable. Both populations are closed and finite (types of one compiled assembly), so the laws are checked exhaustively over the real assembly plus named made-up falsifier types, not sampled.
```

#### D5

```text
Cross-layer algebra. Adapter layer only (connector adapters and their driven port): states are the set of methods of port-implementing types and, per method, whether its IL (with compiler-made nested types folded in) creates a NotSupportedException; operation is the scan; observation is the set of entries Type.Method(ParameterTypes) compared with the inline list. Domain, application and infrastructure layers are untouched: no model, service outcome, timeout or recovery path changes.
```

#### D6

```text
Second stressor walk over the finished design (walkthrough, not executed): a Release build compiles async bodies differently, so S1 also runs its oracle in Release; a connector throwing from a static helper class that implements no port interface is outside the population and is named as an accepted gap; a stale test build reads an old Lighthouse.Backend.dll, but dotnet test builds first; the rename would change S1 list entries, so S2 is delivered before S1.
```

### Constraints

#### K1

```text
Behaviour-free: no route, DTO, serialized response, schema, registration, event, sync path, validation verdict or refresh result changes. Program.cs is not edited. Existing tests stay green; they change only by the mechanical rename of S2.
```

#### K2

```text
No nWave scenario tags in test code; comments are rare and never cite internal ids such as D-7, A1 or ADR numbers as the explanation.
```

#### K3

```text
dotnet build ends with zero warnings (TreatWarningsAsErrors, SonarAnalyzer). Never run the live connector categories (JiraIntegration, LinearIntegration, AdoIntegration, ServiceNowIntegration, GithubIntegration) inside a slice.
```

#### K4

```text
Out of scope: removing any NotSupportedException throw, splitting the port into capability interfaces (feature B), moving connectors into Modules (S03a), rewriting doc-comment prose that cites internal ids (the edit commits of feature B do that).
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
The connector port, its five connectors and every caller ship in one deployable (Lighthouse.Backend) and change in the same commit; the parties cannot change apart, so the primary oracles and the compiler cover the agreement.
```

### Boundaries

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

None.

### Risks

None.

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs

Create

```text
Rule core reached by the oracle through its full type name Lighthouse.Backend.Tests.Architecture.ConnectorUnsupportedMembers; RED is the assertion that it is missing
```

Scripts\/examine\/bin\/connector\-throws\-count

Create

```text
EXAMINE command printing the KPI 7 count per folder Csv Linear ServiceNow
```

\.nwave\/examine\.json

Extend

```text
Register connector-throws-count
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj -c Release --filter FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorRatchetFixtures\.cs

#### Public oracle

```text
Run the oracle: it reaches ConnectorUnsupportedMembers.FindIn by full type name, runs it over Lighthouse.Backend.dll with the port namespace and over the made-up fixture namespace, and compares each answer with its inline list through InlineBaseline
```

```text
Real assembly: exactly the 22 listed entries. Fixtures: the direct throw, the private helper Refuse, the async member and the PlatformNotSupportedException member are reported with their exact entries; the non-port type and the InvalidOperationException member are not; a list with one extra entry fails naming it as stale and a list missing one fails naming the newcomer with the fix hint
```

```text
A rule that scans only top-level method bodies misses the async case; one that matches only the exact type misses PlatformNotSupportedException; one that reports the helper against its caller names the wrong method; a one-way comparison lets a stale entry pass; a rule over all types reports the negative control
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
S1 is delivered after S2 is GREEN: its inline list names the renamed GetFeaturesForPortfolio entries from the start.
```

##### Constraints

None.

##### Reuse

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
Already reads compiled IL with Mono.Cecil (ReadModule, ResidesInNamespaceMatching, NamespaceOf); the ratchet reuses its reader rather than adding a second one. Unchanged: its folding is per type, the ratchet folds per method in its own core.
```

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:27

Reuse

```text
Pattern reused, file unchanged: made-up offender types in a test-only namespace, and a rule core reached by its full type name through reflection so that a missing core is an assertion failure, not a compile error.
```

ConnectorUnsupportedMembers

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:1

CreateNew

```text
No rule reports per method whether its IL creates an exception; IlDependencies answers type-to-type references only and ArchUnitNET drops async edges in Release. One static rule class owns the population, the instruction scan and the folding of compiler-made nested types into the method that wrote them.
```

InlineBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs\:9

Reuse

```text
Owns the two-way inline-list comparison with the fix hint (newcomer fails named, stale entry fails named, never loosened); the ratchet passes its entries and fix hint to AssertMatches unchanged.
```

connector\-throws\-count

Scripts\/examine\/bin\/connector\-throws\-count\:1

CreateNew

```text
No EXAMINE command prints the KPI 7 count per connector folder; architecture-rules runs tests and cannot print it. The new command follows the shape of architecture-rules (bare environment, compact lines).
```

commands

\.nwave\/examine\.json\:2

Extend

```text
The one EXAMINE command registry; gains one entry.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:1

Changed

```text
New rule core: FindIn(module, typeNamespacePattern, portNamespacePattern).
```

examine\-command

Scripts\/examine\/bin\/connector\-throws\-count\:1

Changed

```text
New EXAMINE command printing the NotSupportedException count for the Csv, Linear and ServiceNow connector folders (6 6 8 today).
```

verify\-metadata

\.nwave\/examine\.json\:1

Changed

```text
Registers connector-throws-count beside the existing commands.
```

production\-assembly

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:12

UnchangedCompatible

```text
Read only by the rule; no production byte changes in S1.
```

sibling\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:13

UnchangedCompatible

```text
Connector leaf rule and repository rule keep holding; no rule is loosened.
```

##### Agreement

None.

##### Not applicable

agreement

```text
S1 changes no shared contract: it adds a test-side rule and an EXAMINE command; the connector port and every caller are untouched.
```

prefactoring

```text
S1 adds a rule and edits no production code, so there is no move to prepare.
```

##### Boundaries

```text
NUnit dotnet test over the compiled Lighthouse.Backend.dll (in-process architecture-test boundary; no web host): the oracle loads the module with IlDependencies.ReadModule and calls the rule core
```

###### Driven ports

None.

```text
Test project reads the production assembly; production code does not know the rule
```

###### Failures

None.

##### Observations

```text
Set of entries Type.Method(ParameterTypes) for population methods whose IL creates System.NotSupportedException or a derived type, compared two ways with the inline list; the failure names each newcomer with the fix hint and each stale entry
```

NUnit outcome plus InlineBaseline failure message

```text
An exception constructed in a type outside the population and thrown inside a connector; a caught exception rethrown; an exception created through reflection or Activator; whether the throw is reachable at run time
```

##### Equality

ordinal string equality\, as sets

```text
Two rule answers are equal when they hold the same entries Type.Method(ParameterTypes) under ordinal comparison, ignoring order and repeats. Type is the declaring type simple name; parameter types are Cecil TypeReference.Name values joined by comma and space (generic arity as Cecil gives it, for example IReadOnlyCollection`1).
```

##### Constructors

ConnectorUnsupportedMembers\.FindIn

static IReadOnlyList\<string\> FindIn\(ModuleDefinition module\, string typeNamespacePattern\, string portNamespacePattern\)

##### Laws

###### L1

```text
For every type written in typeNamespacePattern that implements at least one interface whose namespace matches portNamespacePattern, FindIn reports exactly the methods whose IL, with compiler-made nested types (async state machines, lambdas, display classes) folded into the method that wrote them, contains newobj of System.NotSupportedException or a type derived from it; types implementing no such interface are never reported.
```

Invariant

Adapter

ConnectorUnsupportedMembers

###### Treatment

Finite

###### Cases

```text
port member throws NotSupportedException directly: reported against that member
```

```text
port member calls a private helper that throws it: reported against the helper
```

```text
async port member throws it after an await: reported against the async member (state machine folded)
```

```text
port member throws PlatformNotSupportedException: reported (derived type)
```

```text
type implementing no port interface throws it: not reported
```

```text
port member throws InvalidOperationException: not reported
```

```text
Closed finite population: six made-up falsifier types plus the real compiled assembly
```

###### Evidence

None.

###### L2

```text
Over Lighthouse.Backend.dll with portNamespacePattern Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors, the rule answer equals the inline list of 22 entries: Csv 6, Linear 6 and ServiceNow 8 member methods, JiraWorkTrackingConnector.SweepIdentities and AzureDevOpsWorkTrackingConnector.ConvertAuthorizationHeaderToVssCredentials, each listed with its reason; an unlisted offender fails named with the fix hint, a listed entry that stops offending fails named.
```

Invariant

Adapter

InlineBaseline

###### Treatment

Finite

###### Cases

```text
current main: answer equals the 22-entry list
```

```text
a listed entry removed from the code: stale entry fails named
```

```text
One finite compiled assembly; two-way comparison through InlineBaseline
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
Every connector-population method that creates a NotSupportedException is on the inline list, by name
```

```text
Feature B or a new connector could add a member that says it cannot do what its interface asks, unseen, and KPI 7 would silently grow
```

ConnectorUnsupportedMembers

```text
Compiled Lighthouse.Backend.dll built by dotnet test (Debug and Release)
```

ConnectorUnsupportedMembers\.FindIn

InlineBaseline\.AssertMatches

```text
dotnet test outcome of ConnectorsDeclareNoUnsupportedMembersArchUnitTest and its failure message
```

```text
A static helper type outside the population that creates and throws the exception; reflection or Activator creation; a rethrown caught exception; editing the inline list in review
```

##### States

Admitted

```text
Population method creating NotSupportedException that is on the inline list with its reason
```

InlineBaseline

Excluded

```text
Population method creating NotSupportedException that is not on the list, or a listed entry that no longer creates one
```

ConnectorUnsupportedMembers

Unknown

```text
Exception created in a non-population helper type and thrown from a connector
```

reviewer\-and\-kpi7\-grep

##### Models

None.

##### Closure

```text
connector ratchet
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states are refused by the two-way inline comparison over the real assembly; observed only when the oracle runs at GREEN
```

###### Evidence

None.

```text
connector ratchet
```

Inhabitation

Pass

DesignedNotBuilt

```text
The made-up fixture types construct each admitted and excluded case; the real assembly inhabits the 22-entry list today
```

###### Evidence

None.

```text
connector ratchet
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly -> IlDependencies.ReadModule -> ConnectorUnsupportedMembers.FindIn -> InlineBaseline.AssertMatches -> dotnet test outcome and message; also reachable through Scripts/examine/bin/architecture-rules ConnectorsDeclareNoUnsupportedMembers
```

###### Evidence

None.

##### Risks

###### Y1

Bypass

```text
Assumption: every throw is created inside a population type
```

```text
Throws created in connectors, auth strategies and their nested compiler types are caught
```

```text
A helper type outside the population creates and throws the exception unseen
```

```text
A population method creating it unlisted passes
```

```text
Accepted gap, stated in the rule summary; the KPI 7 grep (connector-throws-count) and review cover it
```

ConnectorUnsupportedMembers

###### Y2

Stressor

```text
Assumption: Debug and Release compile async bodies alike
```

```text
Cecil walks MoveNext of state machines in both configurations
```

```text
none
```

```text
A Release-only miss of an async throw
```

```text
Verified by running the oracle in Release as a second verification command
```

acceptance\-designer

##### Architectural tests

###### T1

```text
No type implementing a connector-port interface creates a NotSupportedException outside the inline list, over every such type including ones added later; falsified by six made-up types
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs

Extend

```text
Rename on the port
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
Rename on the implementation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs

Extend

```text
Rename on the implementation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
Rename on the implementation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
Rename on the implementation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Extend

```text
Rename on the implementation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Call sites
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice02ParentFromLinkSpecifications\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataReferenceIdUniquenessTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsDependencyRelationTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsFetchRefusalTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsHistoryReadTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsNamedRelationEndTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnectorTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWriteBackTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvCancellationGranularityTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnectorTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyDogfoodTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyLinkTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraIncrementalSyncTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraNamedDependencyLinkTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraOriginalNamesRefusedListTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraParentReadingsTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraRefreshRefusalTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnectorTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWriteBackTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearCancellationGranularityTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyDogfoodTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyRelationTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearFetchRefusalTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorHistoryParsingTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TrackerPayloadSources\.cs

Extend

```text
Mechanical rename of GetFeaturesForProject
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ConnectorPortSpeaksPortfolioArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

None.

#### Public oracle

```text
Run the oracle: reflect over IWorkTrackingConnector and every concrete Lighthouse.Backend type implementing it, collecting port and implementing members whose name contains Project and Portfolio-typed parameters of their declared methods whose name contains project
```

```text
Empty answer after the rename; on current main it fails naming GetFeaturesForProject on the port and the five connectors and each Portfolio project parameter, including CsvWorkTrackingConnector.TheDependenciesOf
```

```text
A rename of the interface only leaves implementation parameters named project; a check over port members only misses connector private Portfolio parameters; a check on every Project name flags Linear tracker projects
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
Rename GetFeaturesForProject to GetFeaturesForPortfolio (both overloads) on IWorkTrackingConnector and the five connectors, every Portfolio-typed parameter named project on the port and on any method of the five connector types to portfolio (Sonar S927 wants implementation names to match), the Azure DevOps private GetFeaturesForProjectByQuery to GetFeaturesForPortfolioByQuery, and the cref in the port doc comment. One commit: refactor(connectors): rename GetFeaturesForProject to GetFeaturesForPortfolio.
```

```text
Delivery order: S2 (rename) is delivered first and depends on nothing; S1 (ratchet) is delivered after S2 is GREEN, so the S1 inline list names GetFeaturesForPortfolio from the start and no S2 edit touches the S1 oracle.
```

##### Constraints

```text
Pure rename: identifiers and crefs only; no logic, order, comment prose, signature shape or file move changes. Names where Project means a tracker project stay (Linear CreateFeatureFromProject, Jira and Azure DevOps project reads).
```

```text
Callers outside the port and the five connectors change only at the call sites; WorkItemService private parameters, PortfolioUpdater and API helpers keep their local names (outside the promise).
```

##### Reuse

IWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:12

Extend

```text
The port owns the member names; renamed in place, not replaced (D-1 d keeps the interface name).
```

WorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:972

Extend

```text
Only consumer of the renamed member (two call sites).
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs\:344

Extend

```text
Implements the port member; renamed in place with its Portfolio parameters.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs\:155

Extend

```text
Implements the port member; renamed in place with its Portfolio parameters.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:117

Extend

```text
Implements the port member; renamed in place with its Portfolio parameters.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:68

Extend

```text
Implements the port member; renamed in place with its Portfolio parameters.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:758

Extend

```text
Implements the port member; renamed in place with its Portfolio parameters.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:106

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:97

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:757

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:600

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:130

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:112

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:183

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:184

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs\:129

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:100

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice02ParentFromLinkSpecifications\.cs\:256

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:137

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs\:102

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:147

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:236

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:87

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs\:310

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataReferenceIdUniquenessTest\.cs\:37

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:170

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:99

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsDependencyRelationTest\.cs\:176

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsFetchRefusalTest\.cs\:134

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsHistoryReadTest\.cs\:37

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs\:82

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsNamedRelationEndTest\.cs\:222

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnectorTest\.cs\:83

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWriteBackTest\.cs\:549

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs\:23

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvCancellationGranularityTest\.cs\:60

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnectorTest\.cs\:278

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyDogfoodTest\.cs\:32

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyLinkTest\.cs\:209

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraIncrementalSyncTest\.cs\:182

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraNamedDependencyLinkTest\.cs\:53

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraOriginalNamesRefusedListTest\.cs\:60

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraParentReadingsTest\.cs\:258

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraRefreshRefusalTest\.cs\:26

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnectorTest\.cs\:157

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWriteBackTest\.cs\:543

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearCancellationGranularityTest\.cs\:40

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyDogfoodTest\.cs\:26

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyRelationTest\.cs\:27

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearFetchRefusalTest\.cs\:56

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorHistoryParsingTest\.cs\:50

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorTest\.cs\:183

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:163

Extend

```text
Existing test that names the renamed member; renamed only.
```

GetFeaturesForProject

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TrackerPayloadSources\.cs\:71

Extend

```text
Existing test that names the renamed member; renamed only.
```

##### Impact

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:40

Changed

```text
Two overloads renamed; five Portfolio parameters renamed; cref updated
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs\:1

Changed

```text
Implementing members and Portfolio parameters renamed
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs\:1

Changed

```text
Implementing members and Portfolio parameters renamed
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Changed

```text
Implementing members and Portfolio parameters renamed
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Changed

```text
Implementing members and Portfolio parameters renamed
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:1

Changed

```text
Implementing members and Portfolio parameters renamed
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:972

Migrated

```text
Call sites follow the rename
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Migrated

```text
47 test files set up or verify GetFeaturesForProject on Mock<IWorkTrackingConnector> or call it on real connectors; renamed only
```

factory

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/WorkTrackingConnectorFactory\.cs\:1

UnchangedCompatible

```text
Returns the port; no renamed member used
```

wire

Lighthouse\.Frontend\/src\:1

UnchangedCompatible

```text
No route, DTO or response changes; the frontend name with the same spelling is a different symbol
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:106

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:97

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:757

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:600

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:130

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:112

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:183

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:184

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs\:129

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:100

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice02ParentFromLinkSpecifications\.cs\:256

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:137

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs\:102

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:147

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:236

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:87

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs\:310

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataReferenceIdUniquenessTest\.cs\:37

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:170

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsDependencyRelationTest\.cs\:176

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsFetchRefusalTest\.cs\:134

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsHistoryReadTest\.cs\:37

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs\:82

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsNamedRelationEndTest\.cs\:222

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnectorTest\.cs\:83

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWriteBackTest\.cs\:549

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs\:23

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvCancellationGranularityTest\.cs\:60

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnectorTest\.cs\:278

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyDogfoodTest\.cs\:32

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyLinkTest\.cs\:209

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraIncrementalSyncTest\.cs\:182

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraNamedDependencyLinkTest\.cs\:53

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraOriginalNamesRefusedListTest\.cs\:60

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraParentReadingsTest\.cs\:258

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraRefreshRefusalTest\.cs\:26

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnectorTest\.cs\:157

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWriteBackTest\.cs\:543

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearCancellationGranularityTest\.cs\:40

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyDogfoodTest\.cs\:26

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearDependencyRelationTest\.cs\:27

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearFetchRefusalTest\.cs\:56

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorHistoryParsingTest\.cs\:50

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnectorTest\.cs\:183

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:163

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TrackerPayloadSources\.cs\:71

Migrated

```text
Mock set-up, verification or call of the renamed member; renamed only
```

##### Agreement

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:40

Producer

Migrated

```text
Port declares the new name
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
Implements the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
Implements the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
Implements the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
Implements the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
Implements the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:972

Consumer

Migrated

```text
Calls the renamed member in the same commit
```

IWorkTrackingConnector\.GetFeaturesForPortfolio and Portfolio parameter names

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Consumer

Migrated

```text
Test mocks and connector tests renamed in the same commit
```

##### Not applicable

prefactoring

```text
The slice is itself the behaviour-preserving rename; every existing test that names the member changes with it, so no unchanged existing oracle can witness a separate move. Preservation is witnessed by the compiler, the full gate and the S1 oracle staying green. The rename makes feature B easier: its IFeatureSource names GetFeaturesForPortfolio.
```

##### Boundaries

```text
NUnit dotnet test over the built Lighthouse.Backend assembly through reflection (in-process architecture-test boundary); behaviour preservation through the existing suite driving IWorkItemService and the connectors
```

###### Driven ports

```text
IWorkTrackingConnector (connector port), implemented by the Jira, Azure DevOps, Linear, CSV and ServiceNow connectors
```

```text
WorkItemService depends on the port; connectors implement it; the port names only kernel Models types
```

###### Failures

None.

##### Observations

```text
Names of the port members, of the members implementing them on the five connectors, and of every Portfolio-typed parameter on any method declared by the port or those connectors (compiler-generated excluded), read by reflection; the failure lists each offending member or parameter
```

NUnit outcome plus failure message

```text
A tracker project legitimately named Project in a method without a Portfolio parameter; local variable names; comment prose
```

##### Equality

set of offending names

```text
Two answers are equal when they list the same Type.Member or Type.Member(parameter) strings, order ignored; GREEN is the empty set.
```

##### Constructors

None.

##### Laws

###### L3

```text
For IWorkTrackingConnector and every concrete type in Lighthouse.Backend that implements it: no port member and no member implementing a port member has Project in its name, and no Portfolio-typed parameter of any method declared on those types (compiler-generated excluded) has a name containing project, ordinal ignoring case.
```

Invariant

Adapter

IWorkTrackingConnector

###### Treatment

Finite

###### Cases

```text
current main: fails naming GetFeaturesForProject on the port and five connectors and the project parameters
```

```text
after the rename: empty answer
```

```text
a connector added later with a Portfolio project parameter: fails naming it
```

```text
Closed set of types in one built assembly, reflected exhaustively
```

###### Evidence

None.

###### L4

```text
The rename preserves behaviour: every existing test outside the live connector categories has the same outcome before and after.
```

Refinement

Adapter

IWorkTrackingConnector

###### Treatment

Finite

###### Cases

```text
filtered full dotnet test green before and after
```

```text
Test-Fast green after
```

```text
A pure rename is checked by the compiler and the unchanged existing suite
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C2

```text
The connector port and its implementations name a Portfolio as a Portfolio
```

```text
Feature B and S03a would publish the glossary drift Project for Portfolio into the capability interfaces and the module contract
```

IWorkTrackingConnector

```text
Built Lighthouse.Backend assembly
```

reflection over typeof\(IWorkTrackingConnector\) and its implementers

ConnectorPortSpeaksPortfolioArchUnitTest

```text
dotnet test outcome and failure message naming offending members and parameters
```

```text
Explicit interface implementations are still reflected by interface map; dynamic proxies or test doubles outside Lighthouse.Backend are not in the population
```

##### States

Admitted

```text
Port and connector members named ForPortfolio, Portfolio parameters named portfolio; tracker-project names on methods without a Portfolio parameter
```

IWorkTrackingConnector

Excluded

```text
A port or implementing member with Project in its name, or a Portfolio parameter named project on the port or a connector
```

ConnectorPortSpeaksPortfolioArchUnitTest

##### Models

None.

##### Closure

```text
Portfolio naming on the connector port
```

Safety

Pass

DesignedNotBuilt

```text
Excluded names are refused by the reflection oracle over every implementer, including later ones
```

###### Evidence

None.

```text
Portfolio naming on the connector port
```

Inhabitation

Pass

DesignedNotBuilt

```text
After the rename the port, five connectors and WorkItemService compile and the existing suite constructs and calls them
```

###### Evidence

None.

```text
Portfolio naming on the connector port
```

PublicReachability

Pass

DesignedNotBuilt

```text
built assembly -> reflection over IWorkTrackingConnector implementers -> dotnet test outcome; behaviour through IWorkItemService refresh paths in the existing suite
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
No member or Portfolio parameter of the connector port or of any type implementing it says Project for a Portfolio
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ConnectorPortSpeaksPortfolioArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
