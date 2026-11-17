# Architecture brief: story\-6362\-s03d\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Placement (feature-delta D-1, D-2, D-3): the 15 types of PromisesMembership.Moves and the two admitted Models records (DeliveryForecastBlock, DeliveryForecastBlockPercentile, one file) move by git mv into Lighthouse.Backend/Modules/Promises, namespace equals folder. Domain: DeliveryForecastBlock.cs. Application (flat): rule service, source resolver, source sync, forecast publishing, forecast block renderer, their five interfaces and the metric-values projector. Application/Policies: the two PortfolioForecastsUpdated handlers. Slice order S1 then S2.
```

#### D2

```text
Behaviour-free (D-0, D-4, D-10): no class is created, split, merged or renamed, no signature changes, no entity, DbSet, migration, route or DTO changes. Repositories, entities, update code, write-back round, ordering and ranking stay. Program.cs lines and order stay; only usings and full type names change, and the two PortfolioForecastsUpdated registrations keep their order (snapshot recording, then publishing).
```

#### D3

```text
Tests move with production (D-9): unit tests of the moved files move in a separate commit to Lighthouse.Backend.Tests/Modules/Promises/<same sub-folder>; integration, acceptance and controller tests stay. The 12 Jira test files that construct the renderer, JiraDeliverySourceProviderTest, WorkTrackingConnectorFactoryTest and JiraConnectorTestSetup get a using line only; the improve-the-tests rule applies to DeliveryMetricSnapshotRecordingHandlerTest in S2 and is waived for move-only and using-only edits.
```

#### D4

```text
Rule lists follow the move and are never loosened: full-name strings in TrackerIntegrationModuleBoundariesArchUnitTest, WorkItemsModuleBoundariesArchUnitTest and DeliveryRuleServiceApiPreservationTest are rewritten to the new names and stay compared both ways; a rule exemption for a moved type is exact (type and nested types by full name), proved by a throwaway probe that is reverted. Old classifier alternatives stay until feature C. PromisesMembership and PromisesMembershipArchUnitTest are namespace-agnostic and stay unedited.
```

#### D5

```text
Oracles are IL architecture tests through ModuleRules (TypesLiveInTheirLayer, DomainReachesOnlyTheKernelAndTheBaseLibrary), modelled on ForecastingDomainLayoutArchUnitTest and WorkItemsApplicationLayoutArchUnitTest. Each looks every moved type up by name in the compiled assembly and fails when it is not found (never vacuous); top-level types are compared (nested names reduced to the part before the plus sign). Made-up offenders live in a fixture namespace such as Lighthouse.Backend.Modules.PromisesFixtures. They start no host.
```

#### D6

```text
Claim-to-checker: namespace placement, Domain reach and handler order are finite facts read from the compiled IL and the composition root, checked by the two ArchUnit oracles (Finite laws) and by EXAMINE probes (architecture-rules, backend-tests, ef-model-unchanged, deliveries-answers-compare, deliveries-answers-selftest, refresh-reactions-show, test-selection-show, ci-live-suites-show). No Quint, TLA+ or Agda model: nothing is a state machine, queue or proof obligation (no handler, schedule or queue moves) and a model would restate the namespace map. Liveness not applicable: no progress property exists. Type strength: native C# namespaces, no new types; invalid placement is excluded by the IL rules, not by construction.
```

#### D7

```text
Push and cost (D-11, maintainer 2026-11-17): B1 and B2 edit Program.cs, a shared path, so each push selects every live suite; push per slice with CI watched; no review window; mutation once at the end of S03d on frozen code. Standard rigor: architect, acceptance designer and crafter on sonnet, reviewers and EXAMINE on haiku. EXAMINE expectations name only what an admitted probe shows.
```

### Constraints

#### K1

```text
No schema, route, DTO, DI lifetime, forecast or Delivery answer changes; the EF model stays the same (ef-model-unchanged) and every existing test stays green unmodified apart from moves, usings and rule-file strings.
```

#### K2

```text
Approved oracle files are never edited, not even a using line; a crafter target is never an oracle support. The pure-move commit contains git mv and the namespace line only and need not build; GREEN and every review run on a clean committed tree.
```

#### K3

```text
WorkTrackingConnectorFactory, WorkTrackingSystemFactory and AuthenticationMethodSchema stay in Application and get no namespace-wide exemption. Update code, repositories, the write-back round and the Jira port inversion are out of scope. Input bounds: no stored or validated input changes; none is introduced.
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
Producer and consumer ship in one deployable and change in the same commit; the move changes no wire or persisted shape, so the unchanged existing tests and the layout oracles cover it
```

### Boundaries

```text
ArchUnit layout oracles reading the compiled Lighthouse.Backend assembly (IlDependencies and ModuleRules) and the DI composition root for handler order; existing HTTP and application-service ports unchanged
```

#### Driven ports

```text
none changed (repositories and tracker ports stay behind their existing interfaces)
```

```text
Promises Domain reaches only the shared kernel and the base library; Promises Application reaches what it reaches today (Domain, kernel, Models entities, repositories, the Tracker integration port); no edge is added or removed, only namespaces change
```

#### Failures

None.

### Observations

```text
Layout verdict: the full names of moved types per layer namespace, and the offending type named by a failing rule; registration order of the two PortfolioForecastsUpdated handlers
```

IReadOnlyList of full type names with a verdict naming each offender\; ordered list of handler type names

```text
Behaviour: a placement check cannot see a changed answer, publication or snapshot; the unchanged tests and EXAMINE probes carry that. Nested types are compared at top level
```

### Equality

layout observation

```text
Two layouts are equal when every moved top-level type has the same simple name and lives in the same designed namespace, and the registration order of the two handlers is the same list
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
After the move every Promises type lives in its designed layer and Domain reaches only the kernel
```

```text
A type left behind or a Domain edge hides Promises from namespace rules in feature C
```

PromisesLayoutArchUnitTest and PromisesPoliciesLayoutArchUnitTest

```text
The compiled assembly after git mv plus namespace edits
```

git mv plus namespace line\, then using and full\-name edits

ModuleRules\.TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary

```text
Rule verdict naming each offender by full type name
```

```text
Reflection and string-named types are not seen by IL rules; DI strings and Program.cs full names are covered by compilation and the unchanged test suite
```

### States

Admitted

```text
Moved types under Modules.Promises.Domain, Modules.Promises.Application and Modules.Promises.Application.Policies; unmoved neighbours (repositories, update code, entities, DTOs) in their old namespaces
```

crafter

Excluded

```text
A moved type left in an old namespace, a Domain type reaching anything beyond the kernel and base library, a missing type that would make a list vacuous, handlers registered in the other order
```

layout oracles

Unknown

```text
Behaviour against a live Jira release (publishing, syncing) is not observable locally; it stays with the existing acceptance tests and the Jira live suite in CI
```

maintainer

### Models

None.

### Closure

```text
Promises layout move
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states are failed by the IL rules over the whole governed scope; the finite ArchUnit oracles are the checker
```

##### Evidence

None.

```text
Promises layout move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Admitted input is the existing 15 types and 2 records; the constructor is git mv plus namespace line, reaching the oracles through the real build
```

##### Evidence

None.

```text
Promises layout move
```

PublicReachability

Pass

DesignedNotBuilt

```text
The oracles read the compiled assembly and the composition root; HTTP answers are read by EXAMINE probes
```

##### Evidence

None.

```text
Promises layout move
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
IL rules miss catch-clause, event and attribute-argument references
```

```text
Domain reaches only the kernel on every path the IL shows
```

```text
A reference kind the IL cannot see exists
```

```text
A visible edge from Promises Domain beyond the kernel
```

```text
Unseen reference kinds
```

acceptance designer and reviewer

#### Y2

SinglePointOfFailure

```text
Program.cs is a shared path and the composition root: a wrong using or a swapped order is invisible to unit tests
```

```text
Every route and DI registration; handler order
```

```text
A longer CI run (all live suites) on each push
```

```text
A changed registration order or lifetime
```

```text
Linear key rate limit (429) across repeated live runs
```

host

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Domain\/DeliveryForecastBlock\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Models/DeliverySources/DeliveryForecastBlock.cs (holds DeliveryForecastBlock and DeliveryForecastBlockPercentile); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryRuleService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryRuleService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IDeliveryRuleService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryMetricValuesProjector\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryMetricValuesProjector.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliverySources/DeliveryForecastBlockRenderer.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastBlockRenderer\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DeliverySources/IDeliveryForecastBlockRenderer.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliverySources/DeliveryForecastPublishingService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastPublishingService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DeliverySources/IDeliveryForecastPublishingService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceResolver\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliverySources/DeliverySourceResolver.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliverySourceResolver\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DeliverySources/IDeliverySourceResolver.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceSyncService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliverySources/DeliverySourceSyncService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliverySourceSyncService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DeliverySources/IDeliverySourceSyncService.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryRuleServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliveryRuleServiceTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliveryMetricValuesProjectorTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliverySources/DeliveryForecastBlockRendererTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererBeyondTheHorizonTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliverySources/DeliveryForecastBlockRendererBeyondTheHorizonTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliverySources/DeliveryForecastPublishingServiceTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceResolverTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliverySources/DeliverySourceResolverTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceSyncServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DeliverySources/DeliverySourceSyncServiceTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
usings and full type names only; order and lines stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/BlockedItemService\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DeliveryRuleServiceApiPreservationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
full-name strings and fixture types follow the new namespaces; lists stay compared both ways
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
DeliveryRuleService full name follows the new namespace; list stays compared both ways
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBatchedWriteBackTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDependencyDogfoodTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraLinkTypeDogfoodTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesLiveTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDirectionDogfoodTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDogfoodTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraQuietWriteBackTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraScopedTokenIntegrationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSuppressionRetryIntegrationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorAuthDelegationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWriteBackTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactoryTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JiraConnectorTestSetup\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesLayoutArchUnitTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run PromisesLayoutArchUnitTest over the compiled Lighthouse.Backend assembly: look each of the 11 Application and Domain types (and the Models record DeliveryForecastBlockPercentile) up by name and read its namespace; run the shared Domain-reaches-only-the-kernel rule over Modules.Promises.Domain
```

```text
DeliveryForecastBlock and DeliveryForecastBlockPercentile live in Lighthouse.Backend.Modules.Promises.Domain; the rule service, source resolver, source sync, forecast publishing, forecast block renderer, their five interfaces, PortfolioSourcePreview, DeliveryMetricValues and the projector live in Lighthouse.Backend.Modules.Promises.Application; none is left in its old namespace; Domain reaches only the kernel and the base library; the existing unit, integration and controller tests stay green
```

```text
A moved type left in Services.Implementation, Services.Interfaces or Models.DeliverySources, a list name that no type in the assembly matches (vacuous list), or a made-up Domain type under Modules.PromisesFixtures that reaches a repository: the rule fails naming that type
```

##### Promises

P1

P2

P4

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

production file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs\:1

Migrated

```text
git mv plus namespace line for the 11 Application files (rule service and interface, projector, renderer and interface, resolver and interface, sync and interface, publishing service and interface) and Domain/DeliveryForecastBlock.cs
```

unit test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryRuleServiceTest\.cs\:1

Migrated

```text
7 unit tests move with their files, separate commit: DeliveryRuleServiceTest, DeliveryMetricValuesProjectorTest, DeliveryForecastBlockRendererTest, DeliveryForecastBlockRendererBeyondTheHorizonTest, DeliveryForecastPublishingServiceTest, DeliverySourceResolverTest, DeliverySourceSyncServiceTest
```

composition root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
usings and full type names in the registrations of the moved services change; lines and order stay (D-10)
```

API controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
usings for IDeliveryRuleService, IDeliverySourceResolver, PortfolioSourcePreview and the projector; same for DeliveryRulesController and DeliverySourcesController
```

consumer service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
usings for IDeliveryRuleService and IDeliverySourceSyncService; BlockedItemService (Work Items) uses DeliveryRuleService and JiraWorkTrackingConnector uses IDeliveryForecastBlockRenderer: usings only
```

rule list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:103

Changed

```text
full-name strings and made-up-offender types that name DeliveryForecastPublishingService, DeliverySourceResolver, IDeliveryForecastBlockRenderer and IDeliveryForecastPublishingService follow the new namespaces; PromisesAndTheUpdateCode regex already holds Modules.Promises; lists stay compared both ways
```

rule list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:129

Changed

```text
the DeliveryRuleService full name follows the new namespace; DeliveryRuleServiceApiPreservationTest changes its using for the same type
```

rule list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:195

UnchangedCompatible

```text
names IDeliveryForecastBlockRenderer by simple name only; PromisesMembership and PromisesMembershipArchUnitTest are namespace-agnostic and stay unedited
```

test using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWriteBackTest\.cs\:1

Changed

```text
using line only (no row touched): the 12 Jira tests that construct the renderer (JiraBatchedWriteBack, JiraDependencyDogfood, JiraLinkTypeDogfood, JiraOriginalNamesLive, JiraParentLinkDirectionDogfood, JiraParentLinkDogfood, JiraQuietWriteBack, JiraScopedTokenIntegration, JiraSuppressionRetryIntegration, JiraWorkTrackingConnectorAuthDelegation, JiraWorkTrackingConnector, JiraWriteBack), JiraDeliverySourceProviderTest, WorkTrackingConnectorFactoryTest and TestHelpers/JiraConnectorTestSetup
```

test using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
using lines only: DeliveriesControllerTest, DeliveriesControllerArchiveTest, DeliveriesControllerUtcTest, DeliveryRulesControllerTest, DeliverySourcesControllerTest, ArchivedDeliveryStaleAggregateRaceIntegrationTest and PortfolioUpdaterTest; any further file the old namespaces' removal breaks is a using-only edit found by dotnet build
```

EF model\, scripts\, Stryker\, test\-time budget

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
Entities, migrations, test-selection scripts, EXAMINE config and Stryker config name none of the moved types (git grep finds none); the three Delivery entities and DTOs stay
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Domain\/DeliveryForecastBlock\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryRuleService\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryMetricValuesProjector\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastBlockRenderer\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliveryForecastPublishingService\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceResolver\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliverySourceResolver\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceSyncService\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/IDeliverySourceSyncService\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererBeyondTheHorizonTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceResolverTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceSyncServiceTest\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/BlockedItemService\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DeliveryRuleServiceApiPreservationTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraBatchedWriteBackTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDependencyDogfoodTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraLinkTypeDogfoodTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraOriginalNamesLiveTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDirectionDogfoodTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraParentLinkDogfoodTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraQuietWriteBackTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraScopedTokenIntegrationTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraSuppressionRetryIntegrationTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorAuthDelegationTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactoryTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JiraConnectorTestSetup\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

##### Agreement

Namespaces of the Promises Application and Domain types

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Consumer

Migrated

```text
The composition root registers the moved types under their new namespaces; registrations and lifetimes are unchanged
```

Namespaces of the Promises Application and Domain types

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:1

Consumer

Migrated

```text
The Jira connector takes IDeliveryForecastBlockRenderer under its new namespace by using only; the edge itself is retired later in feature C
```

Namespaces of the Promises Application and Domain types

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs\:1

Producer

Migrated

```text
Producer of the moved types; names change, members do not
```

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move done as separate pure-move commits (git mv plus namespace line, then tests, then edits); no further preserving move is needed to make it easier
```

contract_tests

```text
No wire or persisted shape changes and every caller changes in the same commit inside one deployable; the layout oracle and the unchanged suite cover the consumers
```

reuse

```text
Every target is an existing owner relocated by git mv with a namespace line, or an existing consumer edited by using lines; no new type or responsibility is created. Candidates consulted and reused unchanged: ModuleRules (shared layer and Domain rules, PromisesModule already declared), PromisesMembership (lists the 15 types and 2 records), ForecastingDomainLayoutArchUnitTest (oracle shape), DeliveryForecastBlock, DeliveryRuleService, DeliveryMetricValuesProjector, DeliveryForecastBlockRenderer, DeliverySourceResolver, DeliverySourceSyncService, DeliveryForecastPublishingService with their interfaces. Only the oracle PromisesLayoutArchUnitTest is new: no existing oracle states the Promises Domain and Application layout. Future paths do not resolve as locators until the pure move.
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
Every Promises type measured in PromisesMembership (Domain and Application sets) lives in its designed layer namespace, every list name resolves to a type, and Promises Domain reaches only the kernel and base library
```

Invariant

Domain

PromisesLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
all 13 production files' types in the old namespaces fail the layer rule naming each
```

```text
a list name with no matching type fails as missing
```

```text
a made-up Domain type reaching a repository fails the Domain rule
```

```text
Closed finite set of 17 named types read from the compiled assembly; a native example table with both-ways comparison is exact, a generative property adds nothing
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs

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
Promises types live in Modules/Promises Domain and Application by namespace, Domain reaches only the kernel, lists compared both ways and never vacuous
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesLayoutArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/DeliveryForecastPublishingHandler.cs; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DeliveryForecastPublishingHandlerTest.cs; namespace line only, plus usings
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandlerTest.cs; then host-starting rows move down to the mock-built handler (improve-the-tests rule)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
usings and full names of the two registrations; order and lines stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
remove or rename the DeliveryMetricSnapshotRecordingHandlerTest line
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
using line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesPoliciesLayoutArchUnitTest|FullyQualifiedName~DomainEventHandlerOrderPinTest"
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

#### Public oracle

```text
Run PromisesPoliciesLayoutArchUnitTest over the compiled assembly (look DeliveryMetricSnapshotRecordingHandler and DeliveryForecastPublishingHandler up by name and read their namespace) and run the new PortfolioForecastsUpdated entry of DomainEventHandlerOrderPinTest, which reads the handler registration order from the composition root
```

```text
Both handlers live in Lighthouse.Backend.Modules.Promises.Application.Policies and none is left under Services.Implementation.DomainEvents; the registered handlers of PortfolioForecastsUpdated are, in order, DeliveryMetricSnapshotRecordingHandler then DeliveryForecastPublishingHandler; no existing test changes outcome
```

```text
A handler left in the old namespace or a list name matching no type fails by name; the two registrations swapped in Program.cs, or either one missing, fails the order entry naming the position; today the entry does not exist so the order pin is RED until added
```

##### Promises

P1

P2

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

None.

##### Impact

production file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs\:1

Migrated

```text
git mv plus namespace line for both handlers
```

unit test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandlerTest\.cs\:1

Migrated

```text
DeliveryForecastPublishingHandlerTest moves with its handler; no host reference, move only
```

unit test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs\:1

Changed

```text
Moves with its handler; derives from IntegrationFixture and builds a TestWebApplicationFactory in some rows. Each row that starts the host moves down to the mock-built handler the class already uses; if no host reference remains the HostUsersStillToMove line comes off, otherwise it is renamed to the new namespace (D-9)
```

host\-user list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:295

Changed

```text
The line naming Services.Implementation.DomainEvents.DeliveryMetricSnapshotRecordingHandlerTest comes off when no host reference remains, else follows the new namespace; the list stays compared both ways
```

composition root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1633

Changed

```text
usings and full names of the two PortfolioForecastsUpdated registrations only; lines, order and scoped lifetimes stay
```

test using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:484

Changed

```text
using line only: resolves DeliveryMetricSnapshotRecordingHandler through OfType; same for SnapshotRecordedDayInstanceZoneTest
```

test comment

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/DeliveryJointForecastTest\.cs\:298

UnchangedCompatible

```text
Names the handler only in a comment; no reference to change
```

EF model\, scripts\, Stryker

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1633

UnchangedCompatible

```text
No entity, migration, script or Stryker config names either handler (git grep finds none outside the files listed)
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs\:1

Migrated

```text
git mv plus namespace line (and usings for the tests); no member changes
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:1

Changed

```text
Using lines or full-name strings follow the moved types; no behaviour change
```

##### Agreement

PortfolioForecastsUpdated handler registration order

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1633

Producer

UnchangedCompatible

```text
The two registrations keep their order; only namespaces in the lines change
```

PortfolioForecastsUpdated handler registration order

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:1

Consumer

UnchangedCompatible

```text
The dispatcher runs handlers in registration order as today; nothing in it changes
```

##### Not applicable

prefactoring

```text
Behaviour-free move done as separate pure-move commits; the order pin is added by the oracle owner before the move, so no further preserving move is needed
```

contract_tests

```text
No wire or persisted shape changes; the handlers and their event ship in one deployable and the order pin plus the unchanged suite cover the consumers
```

reuse

```text
Every target is an existing owner relocated by git mv with a namespace line, or an existing consumer edited by using lines; no new type or responsibility is created. Candidates consulted and reused unchanged: DomainEventHandlerOrderPinTest (extended with one entry by the oracle owner; a second pin class would duplicate the mechanism), DeliveryMetricSnapshotRecordingHandler, DeliveryForecastPublishingHandler, ModuleRules (as the Work Items and Forecasting Policies layout oracles use it). Only the oracle PromisesPoliciesLayoutArchUnitTest is new: the S1 oracle is frozen by approval and governs other layers. Future paths do not resolve as locators until the pure move.
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
The two PortfolioForecastsUpdated handlers live in Modules.Promises.Application.Policies and are registered in the order snapshot recording, then publishing
```

Invariant

Application

PromisesPoliciesLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
each handler in the old namespace fails the layer rule naming it
```

```text
registration order reversed fails the order pin
```

```text
a missing registration fails the order pin
```

```text
Two named types and one ordered pair of registrations: a closed finite domain; native examples are exact and no generative property adds coverage
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

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
Both forecasts-updated handlers live in Modules/Promises/Application/Policies by namespace, lists compared both ways and never vacuous
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesPoliciesLayoutArchUnitTest"
```

###### T3

```text
PortfolioForecastsUpdated handlers are registered snapshot recording first, then publishing
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~DomainEventHandlerOrderPinTest"
```

##### Contract tests

None.

## ADRs

None.
