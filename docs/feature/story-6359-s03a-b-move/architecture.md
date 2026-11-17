# Architecture brief: story\-6359\-s03a\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D13

```text
Placement: every type of TrackerIntegrationMembership.Moves and the four admitted Models OAuth value types moves by git mv to Lighthouse.Backend/Modules/TrackerIntegration, namespace equals folder. Layers: Domain, Application, Application/OAuth, Application/ConnectionHealth, Adapters/Connectors/{AzureDevOps,Csv,Jira,Linear,ServiceNow,Auth,OAuth}, Adapters/OAuth, Adapters/Background. Unit tests move with their files to Lighthouse.Backend.Tests/Modules/TrackerIntegration/<same sub-folder> in the same slice. Slice order S1 then S2 then S3 then S4.
```

#### D14

```text
Domain exception, measured: WorkItemCategoryCrossing calls EnumerableExtensions.IsItemInList and DependencySourceSelector calls Extensions.WorkItemExtensions.GetAdditionalFieldValue, both in namespace Lighthouse.Backend.Extensions, which ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary rejects (kernel is Lighthouse.Backend.Models plus the base library). Both therefore go to Application in S1, no code edit to avoid it. WorkItemStateTransitionMapper, ParentSourceSelector and DependencyFieldReferences use only Models and go to Domain, flat folder.
```

#### D15

```text
Stay unchanged: repositories, connection save and usage, write-back round, Encryption, demo data (Factories keeps only demo data), Health, controllers, API DTOs, entities, all other Models types (OAuthCompleteResult, OAuthCredential, ConnectionHealthVerdict and the rest).
```

#### D16

```text
Commit shape per slice: oracle commit, pure-move commit (git mv plus namespace line only, tests included), edits (using lines, full-name strings in rule lists, scripts, configs, csproj, budgets) uncommitted for the host, then review record. Program.cs registrations and order stay, only using lines change. No class is created, split or merged; no signature changes; no EF model change.
```

#### D17

```text
Layer algebra. Domain: states are pure values; operations are the five translation rules over handed-in data; observation is the set of Domain-namespace types and their IL dependency targets; equality is type full name. Application: operations are the connector port, factories, OAuth and health use cases; observation is the namespace of each type and the IL edges to Adapters, which must be empty; factory interface and class share one namespace; Board and BoardInformation sit beside IBoardInformationProvider. Adapters: connectors, strategies, providers, prober; observation is namespace and that connectors reach no repository. Infrastructure and recovery: unchanged, no timeout, retry, replay or loss behaviour is touched; DI lifetimes and handler order identical.
```

#### D18

```text
Checker mapping: placement and dependency-edge claims are decided exactly by the compiled IL through IlDependencies and ModuleRules, each oracle carrying a made-up offender as falsifier; behaviour-free is decided by the existing suites, the live connector suites in CI and the EXAMINE answer comparisons. No Quint, TLA+ or Agda model: there is no state, ordering, concurrency, recovery or liveness claim, and a model would restate the namespace map and could not fail. Quint 0.33.1 is installed (probed with quint --version) and was not run; no waiver is recorded, the formal-model claim set is empty by reason, not by omission. Liveness: not applicable, nothing progresses.
```

#### D19

```text
Type and property assessment (object oriented, nw-code-design-oo read): no class is added or changed, so no new anemic type, no Object Calisthenics break and no new invariant owner; each existing rule keeps its cohesive owner. Construction safety: a type cannot silently stay in an old namespace because each layout oracle looks the type up by name in the compiled assembly and fails when it is missing. Property testing: not applicable, every law is a finite list of named types compared both ways, native examples.
```

#### D20

```text
Oracle design rule for all four slices: a layout oracle asserts through ModuleRules.TypesLiveInTheirLayer for its layer (and DomainReachesOnlyTheKernelAndTheBaseLibrary in S1), reads types by name from the compiled assembly, fails naming the offender when a type is missing or in the wrong place, and fails when the layer holds no type. An existing rule that finds its subject by namespace or path (SecretCustodySeamArchUnitTest auth pattern, ConnectorPort key, ConnectorRatchetFixtures, ConnectionHealthSingleWriterArchUnitTest, ServiceNow purity tests) proves in the slice that moves its subject that it still names a made-up offender in the new place, or cites TrackerIntegrationRulesSeeTheModuleArchUnitTest. Lists are rewritten to the new names and compared both ways, never loosened. No nWave scenario tags in code.
```

#### D21

```text
Test substrate: NUnit 4.6 architecture tests reading compiled IL through Mono.Cecil, no host started. Commands run from the repository root under flock (flock -o <lockfile> prefix, not recorded in the command text). Default per-test budgets and the 300 s suite ceiling apply, no derogation requested; each oracle reads the cached IL module like the existing ones. Live connector categories are never run locally; CI runs them on each push (push per slice, maintainer 2026-11-16 and 2026-11-17), hold after B4 until review and EXAMINE are accepted.
```

#### D22

```text
Risk walk. Stressor: a rule keyed on a path or namespace string goes blind after a move; allowed none, forbidden a vacuous rule; residue owner the slice that moves the subject. Stressor: CI selects live suites by path; A2 already matches both old and new paths, old alternatives stay until feature C. Stressor second set: a Serilog filter or reflection string naming an old category; none found in the repository, a host-side configuration outside it is unknown and accepted. Single point of failure: Program.cs using lines, compile error is immediate. Bypass: reflection or string type names, none found; IL misses catch-clause, event and attribute-named-argument references, covered by the full suite.
```

#### D23

```text
Design gap recorded for the host: product promise P2 and slice S1 say all five translation rules live in Domain, but two are rejected by the Domain rule (see Domain exception) and the approved design already sends such types to Application. The S1 oracle therefore asserts three types in Domain and two in Application; if the maintainer wants all five in Domain, the Extensions calls must first be edited, which is a behaviour-free but non-move change outside this feature.
```

#### D24

```text
Reuse verdicts: ModuleRules.TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary (Architecture/ModuleRules.cs:127,153) REUSE; TrackerIntegrationMembership REUSE; the four layout oracles CREATE_NEW (Forecasting and Work Items layout oracles are the model); moved types REUSE unchanged by git mv.
```

### Constraints

#### K5

```text
Behaviour-free: no route, DTO, serialized response, refresh answer, forecast, DI lifetime, handler order or EF model changes; no schema change. No stored or validated input is introduced and no bound is added or removed; the only inputs are source files and the compiled assembly.
```

#### K6

```text
Domain reaches only Lighthouse.Backend.Models and the base library. Application reaches no Adapters of its own module. The factory interface and class share Modules.TrackerIntegration.Application. The board port names no implementation namespace.
```

#### K7

```text
Feature A files (TrackerIntegrationMembership.cs and its test, TrackerIntegrationRulesSeeTheModuleArchUnitTest) may take a using or full-name string edit forced by the move, recorded as a target, never a semantic change. Earlier slices oracle files of this feature are never edited.
```

#### K8

```text
UI, loading and failure looks, RBAC, clients, website and usage-data event are not applicable: nothing a user sees or does changes. Jev is unauthorized (typesafe unavailable by refusal).
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
Producer and consumers sit in one deployable and change in the same commit; no wire or persisted data changes; the architecture oracle and existing integration tests cover them.
```

prefactoring

```text
The slices are themselves behaviour-free moves; no later behaviour change in this feature needs a preparatory move.
```

### Boundaries

```text
IL architecture tests over the compiled Lighthouse.Backend assembly via IlDependencies and ModuleRules (no host); real consumers unchanged: API controllers, Work Items refresh, Promises delivery sources, write-back round, ConnectionHealthProber
```

#### Driven ports

```text
IWorkTrackingConnector and capabilities (moved, unchanged)
```

```text
IWorkTrackingAuthStrategy (moved, unchanged)
```

```text
IOAuthProvider (moved, unchanged)
```

```text
IRepository and ConnectionHealthVerdictRepository (stay)
```

```text
Domain to Models only; Application to Domain and Models and the listed platform edges; Adapters to Application; composition root to Adapters; no Adapters to repositories in Adapters/Connectors
```

#### Failures

None.

### Observations

```text
Namespace and IL dependency targets of each listed type in the compiled assembly
```

IReadOnlyList of full type names and verdict naming each offender

```text
runtime behaviour, DI lifetimes, catch-clause, event and attribute-named-argument references
```

### Equality

layout observation

```text
Two layouts are equal when every listed type has the same full name and the IL edges between listed layers are the same lists; short name equality holds before and after (membership compares short names).
```

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

#### C2

```text
Every Moves type lives in its named layer and none is left under Services or Factories
```

```text
a type left behind or in the wrong layer breaks the module boundary feature C builds on
```

TrackerIntegrationMembership and the four layout oracles

```text
the compiled Lighthouse.Backend assembly
```

git mv plus namespace line

ModuleRules\.TypesLiveInTheirLayer

```text
oracle verdict naming offenders
```

```text
a type renamed or deleted: oracle looks up by name and fails; reflection strings: none found
```

### States

Admitted

```text
Type in its layer namespace with the old using lines rewritten
```

crafter

Excluded

```text
A listed type under Services or Factories, or Domain reaching Extensions, Services or Adapters
```

layout oracles

Unknown

```text
Host-side log filters or external configuration naming an old category
```

maintainer

### Models

None.

### Closure

```text
layer placement
```

Safety

Pass

DesignedNotBuilt

```text
oracle per slice excludes each listed type outside its layer and Domain edges outside the kernel; IL decided, not yet built
```

##### Evidence

None.

```text
layer placement
```

Inhabitation

Pass

DesignedNotBuilt

```text
each type has a concrete target path in the slice targets; git mv produces it
```

##### Evidence

None.

```text
layer placement
```

PublicReachability

Pass

DesignedNotBuilt

```text
oracle reads the compiled assembly produced by dotnet build, the same assembly the host runs; consumers unchanged
```

##### Evidence

None.

```text
layer placement
```

Liveness

Pass

DesignedNotBuilt

```text
not applicable: no progress, queue, schedule or handler changes, no temporal claim
```

##### Evidence

None.

### Risks

#### Y3

Bypass

```text
string or reflection type names and path-keyed rules
```

```text
same rule coverage after the move
```

```text
old alternatives stay until feature C
```

```text
a rule that passes with its subject gone
```

```text
IL misses catch-clause, event and attribute-named-argument references
```

acceptance designer and reviewer

#### Y4

SinglePointOfFailure

```text
Program.cs using lines and shared Linear key on push
```

```text
build and live suites green
```

```text
a push may rerun on a 429
```

```text
red push
```

```text
live suites only in CI
```

host

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Domain\/WorkItemStateTransitionMapper\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/WorkItemStateTransitionMapper.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Domain\/ParentSourceSelector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/Parents/ParentSourceSelector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Domain\/DependencyFieldReferences\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/Dependencies/DependencyFieldReferences.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkItemCategoryCrossing\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/WorkItemCategoryCrossing.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/DependencySourceSelector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/Dependencies/DependencySourceSelector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Domain\/WorkItemStateTransitionMapperTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/WorkItemStateTransitionMapperTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Domain\/ParentSourceSelectorOrderTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/Parents/ParentSourceSelectorOrderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Domain\/ParentSourceSelectorOwnParentTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/Parents/ParentSourceSelectorOwnParentTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Domain\/ParentSourceSelectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/Parents/ParentSourceSelectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Domain\/DependencyFieldReferencesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyFieldReferencesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkItemCategoryCrossingTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/WorkItemCategoryCrossingTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/DependencySourceSelectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencySourceSelectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/ParentSourcesOwnerWriteThroughTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WhereAPortfoliosWorkItemHangsTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WhereATeamsWorkItemHangsTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/IssueFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowStateSpanMapper\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationDomainLayoutArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run the compiled-assembly layout test for Tracker integration Domain: look up WorkItemStateTransitionMapper, ParentSourceSelector and DependencyFieldReferences in Modules.TrackerIntegration.Domain and WorkItemCategoryCrossing and DependencySourceSelector in Modules.TrackerIntegration.Application, and run the Domain-reaches-only-kernel rule; Work Items membership test lists no selector.
```

```text
Each of the five types is found in its namespace, none under Services, Domain reaches only Models and the base library, and made-up Domain offenders reaching Services, Adapters or Extensions are each reported by name.
```

```text
Leave a type under Services.Implementation, rename or delete one, or let a Domain type call EnumerableExtensions: the oracle fails naming it.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
Walking skeleton of the feature: it creates Modules/TrackerIntegration/Domain and Application. Tests: three to Tests/Modules/TrackerIntegration/Domain, two to Application. Work Items rule files stop treating the selectors as their own (membership Stays entries removed, placement fixtures use real new paths, outbound list measured by running the rules).
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

Translation rules called by connectors and Work Items

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Consumer

Migrated

```text
using lines only
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move.
```

contract_tests

```text
One deployable, same commit; see shared record.
```

reuse

```text
Moved targets keep their existing owner unchanged (git mv, namespace line only); using-line targets change no responsibility. ModuleRules.TypesLiveInTheirLayer is reused by the oracle; the oracle file is CreateNew.
```

impact

```text
Impact closed by the target list: every Create target is a git mv, every Extend target is a consumer, rule list, script, config, csproj or budget naming a moved type. Program.cs registrations and order, path-classifier, examine locations and SecretCustody pattern stay compatible; consumer agreements recorded.
```

##### Boundaries

```text
TrackerIntegrationDomainLayoutArchUnitTest reading the compiled assembly
```

###### Driven ports

None.

```text
as the shared boundaries
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
Domain holds exactly WorkItemStateTransitionMapper, ParentSourceSelector, DependencyFieldReferences and reaches only Models and the base library; Application holds WorkItemCategoryCrossing and DependencySourceSelector; Work Items membership lists no selector
```

Invariant

Domain

TrackerIntegrationDomainLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
three Domain types
```

```text
two Application types
```

```text
made-up Domain type reaching Services, Adapters, Extensions
```

```text
closed list of five types
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

###### T5

```text
S1: listed types live in their layer, none under Services or Factories, layer edges as the shared boundaries; falsified by made-up offenders in a fixture namespace Modules.TrackerIntegrationFixtures
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationDomainLayoutArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IAzureDevOpsWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IAzureDevOpsWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IBoardInformationProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IBoardInformationProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliveryForecastPublisher\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IDeliveryForecastPublisher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliverySourceProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IDeliverySourceProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IFeatureSource\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IFeatureSource.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IIncrementalSource\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IIncrementalSource.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IJiraWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IJiraWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ILinearWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/ILinearWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IServiceNowWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IServiceNowWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingAuthStrategyFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingAuthStrategyFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWriteBackTarget\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWriteBackTarget.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingReadException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/WorkTrackingReadException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingRefusedException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/WorkTrackingRefusedException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnectorFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/IWorkTrackingConnectorFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingSystemFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/IWorkTrackingSystemFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/WorkTrackingConnectorFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/WorkTrackingSystemFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodKeys\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodKeys.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodSchema\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/Board\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Boards/Board.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/BoardInformation\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Boards/BoardInformation.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactoryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/WorkTrackingConnectorFactoryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactoryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/WorkTrackingSystemFactoryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodKeysTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodKeysTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodSchemaTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchemaTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/IncrementalSourceCapabilityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/IncrementalSourceCapabilityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/AuthenticationMethodDtoTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkTrackingSystemConnectionDtoTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldEnforcementTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionSecretStatePayloadTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice05LighterRoundsScenarios\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthCallbackCsrfIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowTeamSyncAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WriteBackMappingForNewAdditionalFieldIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WizardsControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorRatchetFixtures\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataReferenceIdUniquenessTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintPinnedDigestTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/JiraReadingVersionFingerprintTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBaseTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DeliverySources\/DeliveryForecastPublishingServiceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DeliverySources\/DeliverySourceResolverTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/Bug5023JiraRefreshRotationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/Providers\/JiraOAuthProviderTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/ServiceNowBasicAuthStrategyTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/WorkTrackingAuthStrategyTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsBatchedWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsBoardReadFailureTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsDependencyRelationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsNamedRelationEndTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsSuppressionOutcomeTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnectorTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ConnectorsWithoutDependenciesTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvCancellationGranularityTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvDemoJourneyWithStateMappingTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnectorTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraBatchedWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraBoardOriginalNamesTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraBoardQueryAssemblyTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraBoardReadFailureTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDeliverySourceProviderTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraDependencyDogfoodTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraLinkTypeDogfoodTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraOriginalNamesLiveTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraParentLinkDirectionDogfoodTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraParentLinkDogfoodTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraQuietWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraRefreshRefusalTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraReleaseSweepLineTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraScopedTokenIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraSuppressionRetryIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnectorAuthDelegationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnectorTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWriteBackTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowBoardPickerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowCancellationGranularityTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowConnectionConfigurationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowRecordClassTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowTeamSyncTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowTransitionHistoryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/AzureDevOpsOrganisation\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FakeJiraDataCenter\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JiraConnectorTestSetup\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestAuthStrategyFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/AuthenticationMethodDto\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WizardsController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/ConnectionHealth\/ConnectionHealthService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DeliverySources\/DeliveryForecastPublishingService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DeliverySources\/DeliverySourceResolver\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OAuth\/Providers\/AdoOAuthProvider\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OAuth\/Providers\/JiraOAuthProvider\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OAuth\/Providers\/StubOAuthProvider\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/JiraCloudBasicAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/LinearApiKeyAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/NoOpAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/OAuthBearerAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/PatAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/ServiceNowBasicAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Auth\/WorkTrackingAuthStrategyFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsReadException\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraQueryRejectedException\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraReadException\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowBoardMapper\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowReadException\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationApplicationLayoutArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run the layout test for Application: the 15 port types, four factory types, AuthenticationMethodKeys, AuthenticationMethodSchema (with AuthenticationMethod and AuthenticationMethodOption), Board and BoardInformation looked up by name.
```

```text
All live in Modules.TrackerIntegration.Application; IWorkTrackingConnectorFactory and WorkTrackingConnectorFactory share one namespace; IBoardInformationProvider names no implementation namespace.
```

```text
Leave one in Services.Interfaces or Factories, split the factory pair across namespaces, or let the board port name a connector namespace: the oracle names it.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
IncrementalSourceCapabilityTest moves with the port to Tests/Modules/TrackerIntegration/Application; ConnectorsWithoutDependenciesTest moves in S3 with the connectors. Both leaks fixed by placement only.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

Connector port IWorkTrackingConnector and capabilities

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkTrackingConnectors\/IWorkTrackingConnector\.cs\:1

Producer

Migrated

```text
moved to Application, signature unchanged
```

Connector port IWorkTrackingConnector and capabilities

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Consumer

Migrated

```text
using lines only
```

Connector port IWorkTrackingConnector and capabilities

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DeliverySources\/DeliverySourceResolver\.cs\:1

Consumer

Migrated

```text
using lines only; edge is legal and listed in feature C
```

Authentication schema read by API DTOs

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Consumer

Migrated

```text
API to Application is legal by namespace
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move.
```

contract_tests

```text
One deployable, same commit; see shared record.
```

reuse

```text
Moved targets keep their existing owner unchanged (git mv, namespace line only); using-line targets change no responsibility. ModuleRules.TypesLiveInTheirLayer is reused by the oracle; the oracle file is CreateNew.
```

impact

```text
Impact closed by the target list: every Create target is a git mv, every Extend target is a consumer, rule list, script, config, csproj or budget naming a moved type. Program.cs registrations and order, path-classifier, examine locations and SecretCustody pattern stay compatible; consumer agreements recorded.
```

##### Boundaries

```text
TrackerIntegrationApplicationLayoutArchUnitTest reading the compiled assembly
```

###### Driven ports

None.

```text
as the shared boundaries
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

###### L6

```text
The port, factories, auth schema and board types live in Application; factory pair shares a namespace; the board port names no implementation namespace
```

Invariant

Application

TrackerIntegrationApplicationLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
25 listed types
```

```text
factory pair namespace
```

```text
IBoardInformationProvider signature namespaces
```

```text
closed list
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
S2: listed types live in their layer, none under Services or Factories, layer edges as the shared boundaries; falsified by made-up offenders in a fixture namespace Modules.TrackerIntegrationFixtures
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationApplicationLayoutArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsFieldNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsFieldNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsReadException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsReadException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/RelationTypeListing\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/RelationTypeListing.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/WorkItemExtensions\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/WorkItemExtensions.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsBatchedWriteBackTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsBatchedWriteBackTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsBoardReadFailureTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsBoardReadFailureTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsCancellationGranularityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsCancellationGranularityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsDependencyRelationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsDependencyRelationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsFetchRefusalTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsFetchRefusalTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsHistoryReadTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsHistoryReadTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsIncrementalSyncTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsNamedRelationEndTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsNamedRelationEndTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsRawListsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsRawListsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsSuppressionOutcomeTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsSuppressionOutcomeTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsWorkTrackingConnectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsWriteBackTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWriteBackTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvCancellationGranularityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvCancellationGranularityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvDemoJourneyWithStateMappingTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvDemoJourneyWithStateMappingTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/adodata\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/adodata.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/empty\-file\.txt

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/empty-file.txt
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/invalid\-missing\-required\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/invalid-missing-required.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/invalid\-not\-csv\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/invalid-not-csv.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/invalid\-wrong\-csv\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/invalid-wrong-csv.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/project\-valid\-all\-optional\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/project-valid-all-optional.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/project\-valid\-depends\-on\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/project-valid-depends-on.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/project\-valid\-required\-only\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/project-valid-required-only.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/project\-valid\-with\-optional\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/project-valid-with-optional.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-additional\-fields\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-additional-fields.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-all\-optional\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-all-optional.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-demo\-journey\-synthesis\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-demo-journey-synthesis.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-required\-only\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-required-only.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-state\-since\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-state-since.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/team\-valid\-with\-optional\.csv

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-with-optional.csv
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/Issue\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/Issue.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueExtensions\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/IssueExtensions.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraFieldNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraFieldNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraOriginalNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraQueryRejectedException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraQueryRejectedException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReadException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraReadException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReleaseSweepCost\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraReleaseSweepCost.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReleaseSweepTally\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraReleaseSweepTally.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReleaseVersionReader\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraReleaseVersionReader.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraRestRoutes\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraRestRoutes.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSiteAddress\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraSiteAddress.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/LinkDirection\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/LinkDirection.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/StandingLink\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/StandingLink.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueExtensionsParentLinkTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/IssueExtensionsParentLinkTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBatchedWriteBackTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraBatchedWriteBackTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBoardOriginalNamesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraBoardOriginalNamesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBoardQueryAssemblyTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraBoardQueryAssemblyTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBoardReadFailureTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraBoardReadFailureTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraCancellationGranularityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraCancellationGranularityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliveryForecastPublisherTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDeliveryForecastPublisherTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDeliverySourceProviderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDependencyDogfoodTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDependencyDogfoodTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDependencyLinkTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraDependencyLinkTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraFieldLookupTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraFieldLookupTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraIncrementalSyncTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraIncrementalSyncTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraIssuesPerRequestTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraIssuesPerRequestTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraLinkTypeDogfoodTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraLinkTypeDogfoodTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraNamedDependencyLinkTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraNamedDependencyLinkTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesLiveTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraOriginalNamesLiveTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesRefusedListTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraOriginalNamesRefusedListTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraOriginalNamesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDirectionDogfoodTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraParentLinkDirectionDogfoodTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDogfoodTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraParentLinkDogfoodTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentReadingsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraParentReadingsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraQuietWriteBackTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraQuietWriteBackTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraRefreshOriginalNamesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraRefreshOriginalNamesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraRefreshRefusalTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraRefreshRefusalTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReleaseSweepCostTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraReleaseSweepCostTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraReleaseSweepLineTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraReleaseSweepLineTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraScopedTokenIntegrationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraScopedTokenIntegrationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSiteAddressTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraSiteAddressTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSuppressionRetryIntegrationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraSuppressionRetryIntegrationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorAuthDelegationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnectorAuthDelegationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWriteBackTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWriteBackTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearResponses\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearResponses.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearCancellationGranularityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearCancellationGranularityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearDependencyDogfoodTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearDependencyDogfoodTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearDependencyRelationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearDependencyRelationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearFetchRefusalTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearFetchRefusalTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingConnectorHistoryParsingTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorHistoryParsingTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingConnectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowBoardMapper\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowBoardMapper.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowBoardVerdict\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowBoardVerdict.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowClassLabels\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowClassLabels.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowHistoryQuery\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowHistoryQuery.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowHistoryVerdict\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowHistoryVerdict.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowReadException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowReadException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowReadScope\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowReadScope.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowStateSpan\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpan.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowStateSpanMapper\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapper.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowTeamQueryVerdict\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowTeamQueryVerdict.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowValidationVerdict\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowValidationVerdict.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkItemMapper\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkItemMapper.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkTrackingConnector\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingConnector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowBoardPickerTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowBoardPickerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowBoardVerdictTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowBoardVerdictTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowCancellationGranularityTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowCancellationGranularityTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowClassLabelsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowClassLabelsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowConnectionConfigurationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowConnectionConfigurationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowHistoryQueryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowHistoryQueryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowHistoryVerdictTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowHistoryVerdictTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowRecordClassTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowRecordClassTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowStateSpanMapperRawListsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowStateSpanMapperTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowTeamQueryVerdictTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowTeamQueryVerdictTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowTeamSyncTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowTeamSyncTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowTransitionHistoryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowTransitionHistoryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowValidationVerdictTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowValidationVerdictTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkItemMapperTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkItemMapperTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkTrackingConnectorIntegrationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingConnectorIntegrationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingConnectorTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/JiraCloudBasicAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/JiraCloudBasicAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/LinearApiKeyAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/LinearApiKeyAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/NoOpAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/NoOpAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/OAuthBearerAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/OAuthBearerAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/PatAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/PatAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategy\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/ServiceNowBasicAuthStrategy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/WorkTrackingAuthStrategyFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyNotFoundException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/WorkTrackingAuthStrategyNotFoundException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategyTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Auth/ServiceNowBasicAuthStrategyTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Auth/WorkTrackingAuthStrategyTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/OAuth\/OAuthSchemaExtensions\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/OAuth/OAuthSchemaExtensions.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/OAuth\/OAuthWorkTrackingOptionNames\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/OAuth/OAuthWorkTrackingOptionNames.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IIssueFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/IIssueFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Factories/IssueFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueFactoryLinkDatesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/IssueFactoryLinkDatesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueFactoryOriginalNamesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/IssueFactoryOriginalNamesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueFactoryRawListsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/IssueFactoryRawListsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/IssueFactoryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Factories/IssueFactoryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ConnectorsWithoutDependenciesTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/ConnectorsWithoutDependenciesTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/OAuth\/OAuthSchemaExtensionsTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthSchemaExtensionsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05SyntheticLabelRemovalTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthCallbackCsrfIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice01LinkTypeReferenceScenarios\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice02ParentFromLinkScenarios\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice02ParentFromLinkSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowTeamSyncAcceptanceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowHistoryPurityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowValidationVerdictPurityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataReferenceIdUniquenessTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactoryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactoryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/Bug5023JiraRefreshRotationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/OAuthProviderRegistryTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/OAuthRefreshSingleFlightTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OAuth\/OAuthServiceTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodSchemaTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/IncrementalSourceCapabilityTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/AzureDevOpsOrganisation\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JiraConnectorTestSetup\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestAuthStrategyFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TrackerPayloadSources\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OAuth\/OAuthService\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/AuthenticationMethodSchema\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/stryker\-config\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.servicenow\-record\-classes\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.bug\-5973\-jira\-board\-query\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.bug\-5756\-fetch\-refusal\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5074\-blocked\-items\-slice05\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Scripts\/test\-timings\/test\-time\-budget\.json

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Scripts\/test\-timings\/tests\/test\_check\_budget\_time\_budgets\.py

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Scripts\/test\-timings\/tests\/test\_connector\_classes\.py

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestTierGuardTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationConnectorsLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationConnectorsLayoutArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run the layout test for the connectors: every connector, helper, mapper, verdict, read scope, option-name class, auth strategy and factory, OAuthSchemaExtensions, OAuthWorkTrackingOptionNames, IIssueFactory and IssueFactory looked up by name.
```

```text
Each lives in Modules.TrackerIntegration.Adapters.Connectors.<Tracker|Auth|OAuth>, none under Services, the mirrored tests sit in the mirrored test folder, the CSV test data resolves at its new path, and the path-keyed rules still name a made-up offender there.
```

```text
Leave one type or test folder behind, forget a csproj data entry (CSV test fails to find its file), or a rule whose subject pattern no longer matches: the oracle or the rule falsifier fails.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
The 15 CSV test-data entries in Lighthouse.Backend.Tests.csproj, the Stryker mutate paths, the test-time-budget keys and their Python pins, the connector-class discovery test and the guard lists of test class names follow the moved tests. Stryker test-case-filters match by class name and need no edit.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

CI live\-suite selection by path

Scripts\/test\-selection\/path\-classifier\.sh\:13

Consumer

UnchangedCompatible

```text
already matches old and new paths (feature A)
```

Test\-time budget keys by test class full name

Scripts\/test\-timings\/test\-time\-budget\.json\:59

Consumer

Migrated

```text
five keys renamed to the new test namespaces
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move.
```

contract_tests

```text
One deployable, same commit; see shared record.
```

reuse

```text
Moved targets keep their existing owner unchanged (git mv, namespace line only); using-line targets change no responsibility. ModuleRules.TypesLiveInTheirLayer is reused by the oracle; the oracle file is CreateNew.
```

impact

```text
Impact closed by the target list: every Create target is a git mv, every Extend target is a consumer, rule list, script, config, csproj or budget naming a moved type. Program.cs registrations and order, path-classifier, examine locations and SecretCustody pattern stay compatible; consumer agreements recorded.
```

##### Boundaries

```text
TrackerIntegrationConnectorsLayoutArchUnitTest reading the compiled assembly
```

###### Driven ports

None.

```text
as the shared boundaries
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

###### L7

```text
Every connector type, helper, auth strategy, OAuth schema helper and issue factory lives in Adapters.Connectors.<Tracker> and reaches no repository
```

Invariant

Adapter

TrackerIntegrationConnectorsLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
per tracker folder
```

```text
Auth and OAuth folders
```

```text
closed list
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

###### T7

```text
S3: listed types live in their layer, none under Services or Factories, layer edges as the shared boundaries; falsified by made-up offenders in a fixture namespace Modules.TrackerIntegrationFixtures
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationConnectorsLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationConnectorsLayoutArchUnitTest
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialNotValidException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthCredentialNotValidException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthProviderNotFoundException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthProviderNotFoundException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthProviderRegistry\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthProviderRegistry.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthProviderResponseException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthProviderResponseException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshFailedException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthRefreshFailedException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshOptions\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthRefreshOptions.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshTimeoutException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthRefreshTimeoutException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenExpiredException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthStateTokenExpiredException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenInvalidException\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthStateTokenInvalidException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuer\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/OAuthStateTokenIssuer.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/Providers/AdoOAuthProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/Providers/JiraOAuthProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/OAuth/Providers/StubOAuthProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/OAuth/IOAuthProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthProviderRegistry\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/OAuth/IOAuthProviderRegistry.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthService\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/OAuth/IOAuthService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthStateTokenIssuer\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/OAuth/IOAuthStateTokenIssuer.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthFlowContext\.cs

Create

```text
git mv from Lighthouse.Backend/Models/OAuth/OAuthFlowContext.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshContext\.cs

Create

```text
git mv from Lighthouse.Backend/Models/OAuth/OAuthRefreshContext.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateClaims\.cs

Create

```text
git mv from Lighthouse.Backend/Models/OAuth/OAuthStateClaims.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthTokens\.cs

Create

```text
git mv from Lighthouse.Backend/Models/OAuth/OAuthTokens.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthCadence\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/ConnectionHealth/ConnectionHealthCadence.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/ConnectionHealth/ConnectionHealthService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/IConnectionHealthService\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Interfaces/ConnectionHealth/IConnectionHealthService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Background\/ConnectionHealthProber\.cs

Create

```text
git mv from Lighthouse.Backend/Services/Implementation/BackgroundServices/ConnectionHealthProber.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/Bug5023JiraRefreshRotationTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/Bug5023JiraRefreshRotationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthCredentialConcurrentLoadTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthProviderRegistryTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthProviderRegistryTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshSingleFlightTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthRefreshSingleFlightTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateSecretPersistenceTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthStateSecretPersistenceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuerTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthStateTokenIssuerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProviderTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/Providers/AdoOAuthProviderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProviderTest\.cs

Create

```text
git mv from Lighthouse.Backend.Tests/Services/Implementation/OAuth/Providers/JiraOAuthProviderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthCallbackCsrfIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionHealthSingleWriterArchUnitTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostIsReleasedAfterDisposeTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/CountingStubOAuthProvider\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestAuthStrategyFactory\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/OAuthBearerAuthStrategy\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostPerFixtureGuardTest\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.EndToEndTests\/tests\/specs\/oauth\/OAuthConnection\.feature

Extend

```text
using lines, full-name strings or paths naming a moved type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationOAuthAndHealthLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationOAuthAndHealthLayoutArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run the layout test for OAuth and health: OAuth service, registry, issuer, refresh options, 7 exceptions, 4 interfaces and the four Models value types in Application.OAuth; health service, cadence and interface in Application.ConnectionHealth; the three providers in Adapters.OAuth; ConnectionHealthProber in Adapters.Background; and no Moves type under Services or Factories.
```

```text
Every type found in its namespace and the TrackerIntegrationMembership Moves list has no member left under Services or Factories; ConnectionHealthSingleWriter still names a made-up offender.
```

```text
Leave any Moves type under Services, move an entity or wire type, or leave OAuthFlowContext in Models: the oracle fails.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
The four admitted Models value types move to Application/OAuth; OAuthCompleteResult stays (OAuthController reads it). OAuthSchemaExtensionsTest moved in S3. Connection health has no unit test of its own to move.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

OAuth service and provider ports

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OAuthController\.cs\:1

Consumer

Migrated

```text
using lines only
```

Connection health port

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ConnectionHealthController\.cs\:1

Consumer

Migrated

```text
using lines only
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move.
```

contract_tests

```text
One deployable, same commit; see shared record.
```

reuse

```text
Moved targets keep their existing owner unchanged (git mv, namespace line only); using-line targets change no responsibility. ModuleRules.TypesLiveInTheirLayer is reused by the oracle; the oracle file is CreateNew.
```

impact

```text
Impact closed by the target list: every Create target is a git mv, every Extend target is a consumer, rule list, script, config, csproj or budget naming a moved type. Program.cs registrations and order, path-classifier, examine locations and SecretCustody pattern stay compatible; consumer agreements recorded.
```

##### Boundaries

```text
TrackerIntegrationOAuthAndHealthLayoutArchUnitTest reading the compiled assembly
```

###### Driven ports

None.

```text
as the shared boundaries
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

###### L8

```text
OAuth and health live in Application.OAuth, Application.ConnectionHealth, Adapters.OAuth, Adapters.Background; no Moves type is left under Services or Factories
```

Invariant

Application

TrackerIntegrationOAuthAndHealthLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
four layer folders
```

```text
no leftover Moves type
```

```text
closed list
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

###### T8

```text
S4: listed types live in their layer, none under Services or Factories, layer edges as the shared boundaries; falsified by made-up offenders in a fixture namespace Modules.TrackerIntegrationFixtures
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationOAuthAndHealthLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationOAuthAndHealthLayoutArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
