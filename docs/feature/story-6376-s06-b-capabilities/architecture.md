# Architecture brief: story\-6376\-s06\-b\-capabilities

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Capability set (D-1 b, confirmed; supersedes the by-level split and the names IIncrementalWorkItemSource and IIncrementalFeatureSource). Base IWorkTrackingConnector keeps SupportsTransitionHistory, GetPredefinedAdditionalFields, GetWorkItemsForTeam(team, ct), ValidateConnection, ValidateTeamSettings, ValidatePortfolioSettings (all five connectors). IWriteBackTarget: WriteFieldsToWorkItems (Jira, Azure DevOps). IFeatureSource: GetFeaturesForPortfolio(portfolio, ct), GetParentFeaturesDetails (Jira, Azure DevOps, Linear, CSV). IIncrementalSource: SupportsIncrementalSync(connection), SweepWorkItemsForTeam, GetWorkItemsForTeam(team, ids, ct), SweepFeaturesForPortfolio, GetFeaturesForPortfolio(portfolio, ids, ct), SweepParentFeatures (Jira, Azure DevOps). One file per interface in Services/Interfaces/WorkTrackingConnectors, namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors. IIncrementalSource does not inherit IFeatureSource: the Portfolio refresh already holds its IFeatureSource and tests IIncrementalSource on the same connector, so no level or second capability is baked into the port Epic 6474 will make unit-generic. Markers: IJiraWorkTrackingConnector and IAzureDevOpsWorkTrackingConnector list all three; ILinearWorkTrackingConnector lists IFeatureSource; IServiceNowWorkTrackingConnector lists none; CsvWorkTrackingConnector declares IFeatureSource itself. Jira and Azure DevOps connector classes are unchanged; Program.cs registrations unchanged.
```
Governed slices: S1, S2, S3

#### D2

```text
Static versus runtime (D-2, confirmed): the type says what a connector can ever do; the probe SupportsIncrementalSync, moved from the base onto IIncrementalSource with the same name and meaning, says what it can do for this connection now. Jira answers false while its deployment is not yet known in this process (its process-wide deployment cache, unchanged), Azure DevOps answers true. SupportsTransitionHistory stays on the base: it gates no member. Jira SweepIdentities keeps its NotSupportedException as the assertion that a caller honoured the probe and keeps its ratchet entry.
```
Governed slices: S3

#### D3

```text
Caller side (D-5, confirmed): IWorkTrackingConnectorFactory.GetWorkTrackingConnector keeps returning IWorkTrackingConnector; callers test the type with is, the idiom DeliverySourceResolver and DeliveryForecastPublishingService already use; no generic capability lookup on the factory. A missing capability fails where the member threw: WriteBackService throws NotSupportedException(Write-back is not supported for {system}.) inside the existing per-batch try, so each update becomes a failed WriteBackItemResult as today; WorkItemService throws NotSupportedException naming the connection's system at the Portfolio Feature read, before any write, so the refresh outcome is the same. The message text is log-only today and is outside the preserved observation. Scans use connector is IIncrementalSource source and source.SupportsIncrementalSync(connection), else the same IdentityScan fallback, and their by-id fetches go through that same source.
```
Governed slices: S1, S2, S3

#### D4

```text
Carriers and owners: no new data carrier, record or state. Decision carrier = the connector's compiled type (capability interfaces) plus the bool probe, owned by Services/Interfaces/WorkTrackingConnectors (Tracker integration). Effect carriers unchanged: WriteBackResult and WriteBackItemResult (Models.WriteBack) for write-back, IdentityScan (private record in WorkItemService) for the sync path. Error carrier: NotSupportedException, log-only on both paths. Per-process state: Jira's deployment cache in JiraWorkTrackingConnector, unchanged. Models (domain) untouched.
```
Governed slices: S1, S2, S3

#### D5

```text
Cross-layer algebra. Domain: none affected. Application: WorkItemService route per (connector type, request, deployment known) in {SweepPath, FullPath} for Team, Portfolio and parent scans and {Served, FailsNotSupported} for Feature reads; WriteBackService per-update result in {Served, FailsNotSupported}. Adapter: the set of capability interfaces each of the five connector types implements, and the probe on IIncrementalSource. Infrastructure and recovery: none affected (no persistence, retry or timeout change).
```
Governed slices: S1, S2, S3

#### D6

```text
Formal verification: Quint for the route-equivalence state model (connectors, requests and Jira's unknown-to-known deployment transition as state), verified with quint verify on Apalache and discriminated by four behavioural mutants; the executable projection is each slice's reflection oracle plus the existing WorkItemServiceTest, FasterUpdatesAcceptanceTest and WriteBackServiceTest. No Agda: no proof obligation beyond the finite 5 x 6 x 2 table. No TLA+/TLC: no temporal claim. Liveness is not applicable: every changed decision is a synchronous pure choice inside one call; the only state transition (deployment unknown to known) is unchanged code with no progress promise added. No property-based test: the domain is closed and finite, so finite reflection cases pin it row by row.
```
Governed slices: S1, S2, S3

#### D7

```text
Ratchet: Lighthouse.Backend.Tests/Architecture/ConnectorUnsupportedMembers.cs (Listed) is a crafter target in every slice, never an oracle or support; the approved ConnectorsDeclareNoUnsupportedMembersArchUnitTest checks it both ways, so each slice removes exactly the entries its deleted throws leave: 22 to 19 (S1), 17 (S2), 2 (S3: Jira SweepIdentities and Azure DevOps ConvertAuthorizationHeaderToVssCredentials).
```
Governed slices: S1, S2, S3

#### D8

```text
Test migration (D-10, confirmed): Moq set-ups of moved members go to mock.As<Capability>() before .Object is first read; compile errors find every one. Tests asserting a removed throw are deleted in the slice that removes the member (ServiceNowWorkTrackingConnectorTest write-back case in S1; its Feature-read cases and ConnectorsWithoutDependenciesTest GetFeaturesForPortfolio_ServiceNowStillRefusesToReturnFeaturesAtAll in S2). As<>-only and deletion-only test edits are waived from improve the tests you touch. Out of scope, confirmed: other Supports* flags (D-4), rename (delivered in feature A), ADR-285, ADR-139 note, ADR-270 glossary and ARCHITECTURE.md written at finalize (D-9), packaging (S03a).
```
Governed slices: S1, S2, S3

#### D9

```text
OO smell targeted: Refused Bequest (RPP L6, Liskov and interface segregation): CSV, Linear and ServiceNow claim members they refuse. The cure is segregated capability interfaces, not a guard. No prefactoring move is needed: the rename that makes the split mechanical shipped in feature A.
```
Governed slices: S1, S2, S3

### Constraints

#### K1

```text
Behaviour-free: no schema, route, DTO, serialized response, UI, event, registration or Program.cs change; the same sync path per connector and connection, the same validation verdicts (ServiceNow keeps portfolio_not_supported), the same per-update write-back results and the same Portfolio refresh outcome.
```
Governed slices: S1, S2, S3

#### K2

```text
No stored or validated input changes, so no input bound, range or size applies; the closed domain is the five connector types, six request kinds and Jira deployment known or not.
```
Governed slices: S1, S2, S3

#### K3

```text
Connectors stay in Services/Implementation/WorkTrackingConnectors; interfaces stay in Services/Interfaces/WorkTrackingConnectors; the connector leaf rule and WorkTrackingConnectors_DoNotDependOnAnyRepository keep holding; no new package; the new port signatures use only Models kernel types.
```
Governed slices: S1, S2, S3

#### K4

```text
Each slice has one new oracle file whose RED is an assertion: the capability interface, found by full name through reflection on the built Lighthouse.Backend assembly, is absent or implemented by the wrong connector types; never a compile error. No nWave tags in test code. Never edit an earlier slice's oracle.
```
Governed slices: S1, S2, S3

### Reuse

IDeliveryForecastPublisher

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IDeliveryForecastPublisher\.cs\:11

Reuse

```text
Existing capability-by-type idiom with a probe on the capability and is-tests at callers; the new interfaces copy its shape.
```

IWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:12

Extend

```text
The port stays and is reduced to the members all five connectors serve; replacing or renaming it would touch 9 production and 40 test files for no glossary gain.
```

GetWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/IWorkTrackingConnectorFactory\.cs\:8

Reuse

```text
Returns the base; callers type-test. A generic capability lookup would be a second way to do what is does.
```

Listed

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:20

Extend

```text
The ratchet list feature A shipped; each slice shrinks it, never loosens it.
```

WorkTrackingReadException

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/WorkTrackingReadException\.cs\:1

Reuse

```text
Considered for the absent Feature read and not used there: it would show a refresh reason the user does not see today. Left unchanged.
```

### Impact

factory

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/WorkTrackingConnectorFactory\.cs\:17

UnchangedCompatible

```text
Returns the concrete connector as the base type; capabilities stay visible to is-tests.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1491

UnchangedCompatible

```text
Registrations by marker; the markers carry the capabilities.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DeliverySources\/DeliverySourceResolver\.cs\:46

UnchangedCompatible

```text
Type-tests an unchanged capability interface; DeliverySourcesController and DeliveryForecastPublishingService likewise.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

UnchangedCompatible

```text
Validation and predefined-field callers (TeamsController, PortfoliosController, the two connection controllers, ConnectionHealthService) use base members only.
```

architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:105

UnchangedCompatible

```text
New interfaces sit in the same namespace; the leaf rule keeps holding.
```

architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:17

UnchangedCompatible

```text
Approved oracle of feature A; its inline list file changes, its bytes do not.
```

client\-surface

Lighthouse\.Frontend\/package\.json\:1

UnchangedCompatible

```text
No wire change: frontend, E2E, Lighthouse-Clients and package manifests untouched.
```

### Agreement

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs\:24

Producer

Migrated

```text
Implements every capability through its marker; members unchanged.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs\:32

Producer

Migrated

```text
Implements every capability through its marker; members unchanged.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:18

Producer

Migrated

```text
Implements IFeatureSource only; throwing members deleted.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:15

Producer

Migrated

```text
Declares IFeatureSource; throwing members deleted.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:18

Producer

Migrated

```text
Implements the base only; throwing members deleted.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:327

Consumer

Migrated

```text
Type-tests IFeatureSource and IIncrementalSource in the same commits as the producers.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:157

Consumer

Migrated

```text
Type-tests IWriteBackTarget in the same commit as the producers.
```

Connector port\: IWorkTrackingConnector plus IWriteBackTarget\, IFeatureSource\, IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DeliverySources\/DeliverySourceResolver\.cs\:46

Consumer

UnchangedCompatible

```text
Uses base members and unchanged capability interfaces only; so do the validation callers.
```

### Not applicable

contract_tests

```text
Producers and consumers of the connector port ship in one deployable and change in the same commit; the slice oracles and existing service tests cover them.
```

### Boundaries

```text
Application services IWorkItemService (Team and Portfolio refresh) and IWriteBackService (write-back); capability facts are observed on compiled types through reflection
```

#### Driven ports

```text
IWorkTrackingConnectorFactory returning IWorkTrackingConnector
```

```text
Capability interfaces IWriteBackTarget, IFeatureSource, IIncrementalSource in Services/Interfaces/WorkTrackingConnectors
```

```text
Application services depend on the port namespace; connectors implement it and stay a leaf: no connector reaches Modules.WorkItems or a repository
```

#### Failures

None.

### Observations

```text
Reflection over the built Lighthouse.Backend assembly: which concrete connector types implement a capability interface found by full name, and which members the base and the capability declare
```

set of type full names and member signatures

```text
whether a member body throws (the ratchet covers it) or whether a caller tests the type (behaviour tests cover it)
```

```text
Ratchet: connector methods whose IL creates a NotSupportedException, compared both ways with the inline list
```

set of Type\.Method\(ParameterTypes\)

```text
an exception constructed in another type and thrown from a connector
```

```text
Behaviour through IWorkItemService and IWriteBackService: sweep or full path chosen per connector and connection; per-update write-back result; Portfolio refresh failing before any write
```

path choice\, WriteBackResult\, refresh exception

```text
the log-only exception sentence
```

### Equality

route

```text
Two implementations agree when, for every connector type, request kind and Jira deployment-known value, they choose the same outcome among Served, FailsNotSupported, SweepPath and FullPath; exception text is not observed.
```

### Constructors

None.

### Laws

#### L1

```text
For every connector type, request kind and deployment-known value, the type-tested route after the split equals the flag-and-throw route before it
```

Refinement

Application

WorkItemService

##### Treatment

Finite

###### Cases

```text
write-back: Jira, Azure DevOps served; CSV, Linear, ServiceNow fail each update
```

```text
Portfolio and parent Feature read: Jira, Azure DevOps, Linear, CSV served; ServiceNow fails
```

```text
Team, Portfolio, parent scan: Azure DevOps sweep; Jira sweep only when deployment known; CSV, Linear, ServiceNow full path
```

```text
Closed 5 x 6 x 2 domain; quint verify covers it exhaustively and slice oracles pin it row by row
```

##### Evidence

docs\/feature\/story\-6376\-s06\-b\-capabilities\/models\/capability\_routing\.qnt

#### L2

```text
No connector type creates a NotSupportedException from a member of an interface it implements, apart from the two listed genuine cases
```

Invariant

Adapter

ConnectorUnsupportedMembers

##### Treatment

Finite

###### Cases

```text
after S1: 19 listed
```

```text
after S2: 17 listed
```

```text
after S3: 2 listed
```

```text
Finite set of compiled connector methods, decided exactly by the IL ratchet
```

##### Evidence

None.

### Refuted

```text
Jira Data Center cannot sweep; incremental sync varies by Cloud versus Data Center
```

```text
Law scope: the probe varies by whether the deployment is known in this process, not by deployment kind
```

```text
A Data Center connection whose deployment was learned earlier answers SupportsIncrementalSync true (JiraIncrementalSyncTest SupportsIncrementalSync_IsTrueForDataCenterOnceTheDeploymentIsKnown)
```

### Claims

#### C1

```text
A connector type that implements a capability interface serves every member of it without NotSupportedException; a connector that cannot do something does not implement the interface
```

```text
A refresh or write-back routed to a member that only refuses, behind a type that claims it
```

Lighthouse\.Backend\.Services\.Interfaces\.WorkTrackingConnectors

```text
WorkTrackingSystems value of a connection
```

WorkTrackingConnectorFactory\.GetWorkTrackingConnector

WorkItemService\, WriteBackService

```text
Reflection oracle per slice plus the IL ratchet
```

```text
A decorator or proxy wrapping a connector hides its capabilities (safe: falls back to full path or failure); Moq mocks (compile errors force As<>); a NotSupportedException constructed elsewhere and thrown here (ratchet blind; review and the KPI 7 grep cover it); reflection or casts by other code
```

### States

Admitted

```text
Jira and Azure DevOps implement IWriteBackTarget, IFeatureSource, IIncrementalSource; Linear and CSV implement IFeatureSource only; ServiceNow implements the base only
```

None.

Admitted

```text
Jira implements IIncrementalSource and its probe answers false while the deployment is unknown
```

None.

Excluded

```text
CSV, Linear or ServiceNow implementing IWriteBackTarget or IIncrementalSource; ServiceNow implementing IFeatureSource; any capability member that throws NotSupportedException outside the two listed cases
```

None.

Unknown

```text
A future wrapper around a connector that does not forward capability interfaces
```

S03a\-S09a

### Models

Quint

docs\/feature\/story\-6376\-s06\-b\-capabilities\/models\/capability\_routing\.qnt

##### Properties

sameRoute

Invariant

```text
newRoute equals oldRoute in every reachable state
```

WorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:327

sweepOnlyThroughCapability

Invariant

```text
A sweep path is chosen only for an IIncrementalSource
```

IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IIncrementalSource\.cs\:1

noThrowFromClaimedMember

Invariant

```text
Write-back and Feature reads are served whenever the connector implements their capability
```

WriteBackService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:157

##### Atoms

State

jiraDeploymentKnown

JiraWorkTrackingConnector\.SupportsIncrementalSync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs\:131

```text
Process-wide cache read inside one probe call; unchanged code
```

Action

step

WorkItemService\.UpdateFeaturesForPortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1139

```text
One refresh or write-back call asks one connector; the route is a pure choice inside it
```

Constant

isIncrementalSource

IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IIncrementalSource\.cs\:1

None.

##### Assumptions

```text
Connector types and capability sets are closed at compile time
```

```text
Jira deployment knowledge only grows within a process
```

```text
Exception text is not observed
```

##### Mutants

###### N1

```text
Linear made an IIncrementalSource
```

###### Kills

sameRoute

###### N2

```text
Scan routes on the type alone, dropping the per-connection probe
```

###### Kills

sameRoute

###### N3

```text
ServiceNow made an IFeatureSource
```

###### Kills

sameRoute

###### N4

```text
CSV made an IWriteBackTarget
```

###### Kills

sameRoute

### Closure

```text
route equivalence and capability set
```

Safety

Pass

DesignedNotBuilt

```text
quint verify of sameRoute, sweepOnlyThroughCapability, noThrowFromClaimedMember at max-steps 4, exit 0; four mutants killed by quint run
```

##### Evidence

docs\/feature\/story\-6376\-s06\-b\-capabilities\/models\/capability\_routing\.receipts\.txt

```text
sweep path through IIncrementalSource
```

Inhabitation

Pass

DesignedNotBuilt

```text
quint run found a reachable SweepPath state (Azure DevOps, or Jira once known); the real Jira and Azure DevOps connectors already serve every member
```

##### Evidence

docs\/feature\/story\-6376\-s06\-b\-capabilities\/models\/capability\_routing\.receipts\.txt

```text
capability reaches the application service
```

PublicReachability

Pass

DesignedNotBuilt

```text
WorkTrackingConnectorFactory.GetWorkTrackingConnector -> is-test in WorkItemService and WriteBackService -> IWorkItemService and IWriteBackService; acyclic, no new constructor
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negates: capability is static per type (a connector can do X only for some connections)
```

```text
No member throws for a claimed capability
```

```text
The capability gains a Supports probe as IIncrementalSource has
```

```text
A throw from a claimed member
```

```text
Pattern documented in ADR-285 at finalize; the ratchet catches a throw
```

whoever adds the capability

#### Y2

Stressor

```text
Negates: Jira's deployment is known
```

```text
Full path as today
```

```text
Probe false
```

```text
A half sweep
```

```text
Jira SweepIdentities stays listed with its reason
```

JiraWorkTrackingConnector

#### Y3

SinglePointOfFailure

```text
Negates: the factory hands out the concrete connector
```

```text
Correct results
```

```text
A wrapper hides capabilities: Teams fall back to the full path, write-back fails
```

```text
A wrong sweep result
```

```text
A future wrapper must forward the capability interfaces
```

S03a\-S09a

#### Y4

Bypass

```text
A NotSupportedException constructed in another type and thrown from a connector
```

```text
Ratchet still reads construction sites
```

```text
Not reported by the ratchet
```

```text
Silent acceptance in review
```

```text
Accepted gap; review and the connector-throws-count EXAMINE command cover it
```

reviewer

#### Y5

Stressor

```text
Negates: every ServiceNow Portfolio is validated before refresh (direct API save)
```

```text
Refresh fails as today
```

```text
Success false with no reason
```

```text
A partial write
```

```text
A read refusal with a user sentence is a later behaviour change
```

maintainer

#### Y6

Stressor

```text
Second set, walkthrough only: two refreshes of one connection at once; a Jira instance switching Data Center to Cloud while running; a stale test build
```

```text
No new shared state; probe reads the same cache; IL rules read the built assembly
```

```text
Same answers as today
```

```text
A route differing from today
```

```text
none found
```

architect

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWriteBackTarget\.cs

Create

```text
New capability interface
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs

Extend

```text
WriteFieldsToWorkItems leaves the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs

Extend

```text
Marker lists IWriteBackTarget
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs

Extend

```text
Marker lists IWriteBackTarget
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
Throwing WriteFieldsToWorkItems deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
Throwing WriteFieldsToWorkItems deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Extend

```text
Throwing WriteFieldsToWorkItems deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

Extend

```text
Type test for IWriteBackTarget inside the existing try
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs

Extend

```text
Remove the three WriteFieldsToWorkItems entries (22 to 19)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs

Extend

```text
Delete the case asserting the removed write-back throw
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Move WriteFieldsToWorkItems mock set-ups to As<IWriteBackTarget>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~WriteBackTargetCapabilityArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest|FullyQualifiedName~WriteBackServiceTest|FullyQualifiedName~WriteBackSuppressionWarningTest|FullyQualifiedName~QuietWriteBackAcceptanceTest|FullyQualifiedName~ServiceNowWorkTrackingConnectorTest"
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.sln --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Load the built Lighthouse.Backend assembly, resolve Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.IWriteBackTarget by full name and enumerate every concrete class implementing IWorkTrackingConnector; then drive IWriteBackService.WriteFieldsToWorkItems for a CSV connection whose connector factory returns the real CsvWorkTrackingConnector, with two field updates
```

```text
The interface exists and declares Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection, IReadOnlyList<WriteBackFieldUpdate>); IWorkTrackingConnector no longer declares it; the concrete connectors implementing it are exactly JiraWorkTrackingConnector and AzureDevOpsWorkTrackingConnector; the CSV write-back returns one failed item result per update and stores no written value
```

```text
Today the full-name lookup finds no type, so the oracle fails on an assertion; a set still holding CSV, Linear or ServiceNow, or missing Jira or Azure DevOps, fails; a WriteBackService that skips or succeeds the CSV updates instead of failing each one fails the behaviour case
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

IWriteBackTarget

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:76

CreateNew

```text
No existing capability owns write-back; the member moves off the base into its own interface, shaped like IDeliveryForecastPublisher
```

WriteFieldsToWorkItems

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:157

Extend

```text
The one write-back caller; its try already turns a connector failure into failed item results
```

IWriteBackTarget

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWriteBackTarget\.cs\:1

CreateNew

```text
New capability interface; no existing interface owns this capability
```

IJiraWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

IAzureDevOpsWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

CsvWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

LinearWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ServiceNowWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ServiceNowWorkTrackingConnectorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

WriteBackServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

WriteBackSuppressionWarningTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

QuietWriteBackAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

FasterUpdatesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

Slice00OneForecastPerBatchScenarios

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

TaskManagerHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectionFieldChangeAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

##### Impact

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWriteBackTarget\.cs\:1

Changed

```text
New interface
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:76

Changed

```text
Member removed
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:6

Changed

```text
Marker base list
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:3

Changed

```text
Marker base list
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:551

Changed

```text
Member deleted
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:902

Changed

```text
Member deleted
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:979

Changed

```text
Member deleted
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:157

Migrated

```text
Type test
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:20

Changed

```text
Three entries removed
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs\:26

Migrated

```text
Mocks move to As<IWriteBackTarget>(); same for the other listed mock files
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No preserving move precedes the behaviour: the rename that makes the split mechanical shipped in feature A, and the slice itself is a behaviour-free type move proven by the existing service tests.
```

##### Boundaries

```text
Application services IWorkItemService and IWriteBackService; capability facts observed on compiled types through reflection
```

###### Driven ports

```text
IWorkTrackingConnectorFactory returning IWorkTrackingConnector, tested for the slice capability
```

```text
Application services depend on the port namespace; connectors implement it and stay a leaf
```

###### Failures

####### B1

```text
Write-back to a connection whose connector is not an IWriteBackTarget
```

Refusal

```text
One failed WriteBackItemResult per update, logged; nothing stored
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
The set of concrete connector types implementing IWriteBackTarget equals {Jira, Azure DevOps}, and a write-back to any other connection fails each update
```

Invariant

Adapter

IWriteBackTarget

###### Treatment

Finite

###### Cases

```text
Jira, Azure DevOps implement it
```

```text
CSV, Linear, ServiceNow do not, and their write-back fails each update
```

```text
Five closed connector types
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
Every concrete type, present or added later, implementing IWriteBackTarget is JiraWorkTrackingConnector or AzureDevOpsWorkTrackingConnector, and both implement it
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~WriteBackTargetCapabilityArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IFeatureSource\.cs

Create

```text
New capability interface
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs

Extend

```text
GetFeaturesForPortfolio(portfolio, ct) and GetParentFeaturesDetails leave the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs

Extend

```text
Marker lists IFeatureSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs

Extend

```text
Marker lists IFeatureSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/ILinearWorkTrackingConnector\.cs

Extend

```text
Marker lists IFeatureSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
Class declares IFeatureSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Extend

```text
Two throwing Feature-read members deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Resolve IFeatureSource at the Portfolio Feature read and parent reads; absent fails at the same point
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs

Extend

```text
Remove the two ServiceNow Feature-read entries (19 to 17)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs

Extend

```text
Delete the cases asserting the removed Feature-read throws
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs

Extend

```text
Delete GetFeaturesForPortfolio_ServiceNowStillRefusesToReturnFeaturesAtAll
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Extend

```text
Move Feature-read mock set-ups to As<IFeatureSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~FeatureSourceCapabilityArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest|FullyQualifiedName~WorkItemServiceTest|FullyQualifiedName~FasterUpdatesAcceptanceTest|FullyQualifiedName~ServiceNowWorkTrackingConnectorTest|FullyQualifiedName~ConnectorsWithoutDependenciesTest"
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.sln --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Load the built Lighthouse.Backend assembly, resolve Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.IFeatureSource by full name and enumerate every concrete class implementing IWorkTrackingConnector; then drive IWorkItemService.UpdateFeaturesForPortfolio for a Portfolio on a ServiceNow connection whose connector factory returns the real ServiceNowWorkTrackingConnector
```

```text
The interface exists and declares GetFeaturesForPortfolio(Portfolio, CancellationToken) and GetParentFeaturesDetails(Portfolio, IEnumerable<string>, CancellationToken); IWorkTrackingConnector no longer declares either; the concrete connectors implementing it are exactly Jira, Azure DevOps, Linear and CSV; the ServiceNow Portfolio refresh ends in a NotSupportedException and leaves the Portfolio's Features unchanged
```

```text
Today the full-name lookup finds no type, so the oracle fails on an assertion; ServiceNow in the set, or any of the four missing, fails; a refresh that completes with an empty Feature list for ServiceNow instead of failing fails the behaviour case
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

IFeatureSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:40

CreateNew

```text
No existing capability owns Portfolio Feature reading; IDeliverySourceProvider reads Delivery sources, a different question
```

FetchEveryFeature

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:970

Extend

```text
The Portfolio Feature read site; resolving the capability there keeps the failure point and outcome
```

IFeatureSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IFeatureSource\.cs\:1

CreateNew

```text
New capability interface; no existing interface owns this capability
```

IJiraWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

IAzureDevOpsWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ILinearWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/ILinearWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

CsvWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ServiceNowWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ServiceNowWorkTrackingConnectorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectorsWithoutDependenciesTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

PortfolioBlockedHistoryAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ManualSortingAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

FasterUpdatesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

DependenciesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

Slice00OneForecastPerBatchScenarios

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

BehaviourSettingsAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

TaskManagerHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectionFieldChangeAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectionDefaultsAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

FastWithManyFeaturesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

WorkItemServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

WorkItemServiceFeatureTransitionIntegrationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

##### Impact

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IFeatureSource\.cs\:1

Changed

```text
New interface
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:40

Changed

```text
Two members removed
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/ILinearWorkTrackingConnector\.cs\:3

Changed

```text
Marker base list; Jira and Azure DevOps markers likewise
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:15

Changed

```text
Declares IFeatureSource
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:758

Changed

```text
Two members deleted
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:972

Migrated

```text
Type test at the Feature read and the parent reads
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:20

Changed

```text
Two entries removed
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Migrated

```text
Mocks move to As<IFeatureSource>(); same for the other listed mock files
```

port\-or\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:1

Changed

```text
Capability list or members change in this slice
```

port\-or\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Changed

```text
Capability list or members change in this slice
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No preserving move precedes the behaviour: the rename that makes the split mechanical shipped in feature A, and the slice itself is a behaviour-free type move proven by the existing service tests.
```

##### Boundaries

```text
Application services IWorkItemService and IWriteBackService; capability facts observed on compiled types through reflection
```

###### Driven ports

```text
IWorkTrackingConnectorFactory returning IWorkTrackingConnector, tested for the slice capability
```

```text
Application services depend on the port namespace; connectors implement it and stay a leaf
```

###### Failures

####### B2

```text
Portfolio refresh on a connection whose connector is not an IFeatureSource
```

Refusal

```text
UpdateFeaturesForPortfolio ends in NotSupportedException before any write; the updater records Success false with no reason, as today
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
The set of concrete connector types implementing IFeatureSource equals {Jira, Azure DevOps, Linear, CSV}, and a ServiceNow Portfolio refresh fails before any write
```

Invariant

Adapter

IFeatureSource

###### Treatment

Finite

###### Cases

```text
Jira, Azure DevOps, Linear, CSV implement it
```

```text
ServiceNow does not; its Portfolio refresh fails and its validation still answers portfolio_not_supported
```

```text
Five closed connector types
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
Every concrete type, present or added later, implementing IFeatureSource is Jira, Azure DevOps, Linear or CSV connector, all four implement it, and ServiceNow does not
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~FeatureSourceCapabilityArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IIncrementalSource\.cs

Create

```text
New capability interface with the per-connection probe
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs

Extend

```text
Probe, three sweeps and two by-id fetches leave the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs

Extend

```text
Marker lists IIncrementalSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs

Extend

```text
Marker lists IIncrementalSource
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
Five throwing members and the probe deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
Five throwing members and the probe deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Extend

```text
Five throwing members, the probe and the misnamed unavailable-reading message deleted
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Three scan sites type-test IIncrementalSource plus its probe; by-id fetches use the same source
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs

Extend

```text
Remove the fifteen sweep and by-id entries (17 to 2)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
Move probe, sweep and by-id mock set-ups to As<IIncrementalSource>() where the mock is of the base
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~IncrementalSourceCapabilityTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~ConnectorsDeclareNoUnsupportedMembersArchUnitTest|FullyQualifiedName~WorkItemServiceTest|FullyQualifiedName~FasterUpdatesAcceptanceTest|FullyQualifiedName~JiraIncrementalSyncTest"
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.sln --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Load the built Lighthouse.Backend assembly and resolve Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.IIncrementalSource by full name; enumerate every concrete class implementing IWorkTrackingConnector; through the interface's SupportsIncrementalSync method (invoked by reflection), ask a real Jira connector built with a stubbed HTTP handler as JiraIncrementalSyncTest does, once for a site this process already reached and once for a site address no test has reached, and a real Azure DevOps connector
```

```text
The interface exists and declares SupportsIncrementalSync(WorkTrackingSystemConnection), SweepWorkItemsForTeam, GetWorkItemsForTeam(Team, IReadOnlyCollection<string>, CancellationToken), SweepFeaturesForPortfolio, GetFeaturesForPortfolio(Portfolio, IReadOnlyCollection<string>, CancellationToken) and SweepParentFeatures; IWorkTrackingConnector declares exactly SupportsTransitionHistory, GetPredefinedAdditionalFields, GetWorkItemsForTeam(Team, CancellationToken), ValidateConnection, ValidateTeamSettings, ValidatePortfolioSettings; the implementing concrete connectors are exactly Jira and Azure DevOps; CSV, Linear and ServiceNow are not of the type; the probe answers true for the reached Jira site, false for the unreached one, true for Azure DevOps
```

```text
Today the full-name lookup finds no type, so the oracle fails on an assertion; any of CSV, Linear, ServiceNow in the set, or Jira or Azure DevOps missing, fails; a probe dropped or made constant true fails the unreached-Jira case; a base still declaring a sweep or by-id member fails
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

SupportsIncrementalSync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:21

Extend

```text
Same name and meaning, moved onto the capability that it qualifies
```

IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:38

CreateNew

```text
No existing capability owns sweeping; one interface for all levels because Epic 6474 makes fetching unit-generic
```

IdentityScan

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:327

Extend

```text
The three scan sites keep their fallback; only the condition changes
```

IIncrementalSource

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IIncrementalSource\.cs\:1

CreateNew

```text
New capability interface; no existing interface owns this capability
```

IJiraWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

IAzureDevOpsWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

CsvWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

LinearWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

ServiceNowWorkTrackingConnector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:1

Extend

```text
Existing owner of this capability membership; extended rather than replaced
```

FasterUpdatesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

Slice00OneForecastPerBatchScenarios

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

DeliverySourcesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

TaskManagerHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectionFieldChangeAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

ConnectionDefaultsAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

WorkItemServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Extend

```text
Existing test owner of the moved mock set-ups or removed-throw assertions; edited, not replaced
```

##### Impact

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IIncrementalSource\.cs\:1

Changed

```text
New interface
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:21

Changed

```text
Six members removed; six remain
```

port

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IJiraWorkTrackingConnector\.cs\:6

Changed

```text
Marker base list; Azure DevOps likewise
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs\:30

Changed

```text
Members and message deleted; CSV and Linear likewise
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1139

Migrated

```text
Type test plus probe at lines 327, 1139, 1423; by-id fetches at 381, 1186
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:20

Changed

```text
Fifteen entries removed; two genuine remain
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Migrated

```text
Mocks move to As<IIncrementalSource>(); same for the other listed mock files
```

examine\-metadata

\.nwave\/examine\.json\:1

UnchangedCompatible

```text
connector-throws-count prints 0 0 0 after this slice; the command itself is owned by the host EXAMINE set-up
```

port\-or\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Changed

```text
Capability list or members change in this slice
```

port\-or\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Changed

```text
Capability list or members change in this slice
```

port\-or\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Changed

```text
Capability list or members change in this slice
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Migrated

```text
Mock set-ups move to As<Capability>() or a removed-throw assertion is deleted
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No preserving move precedes the behaviour: the rename that makes the split mechanical shipped in feature A, and the slice itself is a behaviour-free type move proven by the existing service tests.
```

##### Boundaries

```text
Application services IWorkItemService and IWriteBackService; capability facts observed on compiled types through reflection
```

###### Driven ports

```text
IWorkTrackingConnectorFactory returning IWorkTrackingConnector, tested for the slice capability
```

```text
Application services depend on the port namespace; connectors implement it and stay a leaf
```

###### Failures

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
A Team, Portfolio or parent-Feature scan takes the sweep path exactly when the connector is an IIncrementalSource and its probe answers true for the connection; otherwise the full path, as today
```

Refinement

Application

WorkItemService

###### Treatment

Finite

###### Cases

```text
Azure DevOps: sweep
```

```text
Jira reached: sweep
```

```text
Jira not yet reached: full path
```

```text
CSV, Linear, ServiceNow: full path, not of the type
```

```text
Closed table; WorkItemServiceTest and FasterUpdatesAcceptanceTest pin probe to path, the oracle pins type and probe
```

###### Evidence

docs\/feature\/story\-6376\-s06\-b\-capabilities\/models\/capability\_routing\.qnt

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
Every concrete type, present or added later, implementing IIncrementalSource is Jira or Azure DevOps connector, both implement it, and the base IWorkTrackingConnector declares exactly the six members all five connectors serve
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~IncrementalSourceCapabilityTest"
```

##### Contract tests

None.

## ADRs

None.
