# Architecture brief: story\-6382\-s09a\-c\-contracts

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
No behaviour, schema, route, DTO, registration or usage-data change: only namespace lines, using lines, string literals naming a moved type, file splits and the R-C1 ratchet list change (D-0, scope out).
```

#### D2

```text
Each published type moves into Modules/<X>/Contracts with namespace Lighthouse.Backend.Modules.<X>.Contracts, flat, no sub-folders; the set per context is ContextContractsMembership.Entries with Seed or Closure admission at HEAD, not the feature-delta's expected list (D-1, D-2).
```

#### D3

```text
Slice order is by publisher: S1 Licensing and Access, S2 Flow metrics and Forecasting, S3 Work Items, S4 Tracker integration, S5 Organisation set-up; no moving type names another context's module type in its signature (measured from the moving files' using lines), so R-C2 stays empty after every slice whatever the order.
```

#### D4

```text
Each slice has its own new oracle under Tests/Architecture (<Publisher>PublishesItsContracts or PublishTheirContracts ArchUnitTest), RED before the move: every non-event ContextContractsMembership entry of the slice's publishers lives in Modules.<X>.Contracts and no type names it through its old namespace. No later slice edits an earlier oracle. ContextContractsArchUnitTest (the R-C1 list), and every shared rule file a slice changes, is an Extend target of that slice: the crafter deletes the slice's R-C1 entries, the entries whose target is one of its moved types; edges onto ReachedButNotPublished types stay (D-8).
```

#### D5

```text
Domain event types stay in Models.Events in feature C; their Event entries in ContextContractsMembership are feature D's (scope out).
```

#### D6

```text
CsvWorkTrackingOptionNames moves to TrackerIntegration.Contracts in S4 per P4: a static class of constants that Flow metrics' two demo backfill handlers name at source level; C# inlines constants, so the IL shows no edge and neither ContextContractsMembership nor R-C1 can see it. It is not added to the membership (which is compared both ways with the IL closure); S4's oracle observes its namespace by reflection instead.
```

#### D7

```text
Files that hold several top-level types and move are split first, in a preserving commit before the move (File layout): ITeamMetricsService.cs, IPortfolioMetricsService.cs, CycleTimeOutlierDefinition.cs, XmRCalculator.cs in S2. A consumer file whose only change is a using line or a renamed literal is not restructured (maintainer default on open question 7).
```

#### D8

```text
Tests move with production only where a test class tests a contract type alone; every other test changes using lines and literals only (D-10). Literal old names in tests follow ContextContractsStringsTheMoveRenames, whose entries hold both forms and are not edited.
```

#### D9

```text
S5 narrows Organisation set-up's open host from Application and Domain by right to Contracts in the open-host oracle and the downstream modules' set-up patterns; the Refinement edge onto the unpublished WeeklyRecurrence is then listed in Refinement's own outbound list until feature D re-homes module-to-module entries into R-C1 (D-8, E10).
```

#### D10

```text
Cross-layer algebra, compile-time only: state is the compiled type graph of Lighthouse.Backend (types, namespaces, IL edges); operations are a namespace move of a type set and a list deletion; observations are the NUnit verdicts of the Architecture tests and the build. Domain, application, adapter and infrastructure runtime states are unchanged; no new carrier, error or per-process state.
```

#### D11

```text
No formal model: R-C1, R-C2, R-C3 and the membership closure are relations over the finite compiled assembly and inline lists with no shared mutable state, no multi-step protocol, no concurrency or crash window; NUnit evaluates them exhaustively on every run. No checker selected, so no probe, install or waiver (D-16).
```

#### D12

```text
No property tests: every law ranges over a closed finite set (compiled types, inline edge list), checked exhaustively; planted fixture types already falsify each rule (D-16; the test-writing skill wins over nWave PBT density).
```

#### D13

```text
No liveness claim: no process must eventually progress; every check is a synchronous evaluation of a compiled assembly.
```

#### D14

```text
Paradigm object_oriented preserved; no domain decision changes owner: every moved type keeps its members and implementations; the namespace is the single owner of what a context publishes, the membership its measured projection.
```

#### D15

```text
Commits per slice: the split commit (S2), then refactor(contracts): move commits apart from list edits; S4 changes SHARED_REGEX in the same commit that moves the connector port (D-12, E8).
```

#### D16

```text
S5 open-host narrowing keeps the property route (option A), with one owner: ModuleRules owns the set-up boundary. ModuleRules holds SetUpContracts, the positive pattern of Organisation set-up's Contracts namespace built from ContextContractsMembership.ContractsOf("OrganisationSetup"), and SetUpBehindItsContracts, the negative pattern derived from the same name; both are properties, not constants, so the namespace-move law does not read them as rule patterns. Every downstream rule reuses them: WorkItems' SetUpOpenToTheContextsBuildingOnIt becomes ModuleRules.SetUpContracts (or is removed and its readers use it), the composites in Promises (TheNamespacedUpstreamCodeAndSetUpBehindItsContracts) and Refinement (TheForbiddenCodeAndSetUpBehindItsContracts) and the Access, Licensing, Reports and WorkItems layer patterns compose ModuleRules.SetUpBehindItsContracts, and OrganisationSetupIsAnOpenHostArchUnitTest's open list uses ContextContractsMembership.ContractsOf("OrganisationSetup") instead of SetUpModule + ".Contracts". The declared pattern roles (ContextContractsPatternRoles, OrganisationSetupPatternRoles) stay unchanged: they describe what the namespace-move law sees, not the open host, and moving the narrowing there would put a boundary owner inside the move law. Option B rejected for that reason.
```
Governed slices: S5

### Constraints

#### K1

```text
No input bounds apply: no stored or validated input changes; the inline edge list only shrinks and is compared both ways.
```

#### K2

```text
R-C1 ratchet entries are deleted only for the slice's own publishers; no entry is added; the residue after S5 is the edges onto ReachedButNotPublished types (CurrentUserProfileService, PercentileCalculator x2, WeeklyRecurrence, ParentSourceSelector, FeatureFieldProvider, RuleEvaluator`1, FetchFingerprint), each with its owner comment unchanged.
```

#### K3

```text
Do not run the full backend suite or any live connector category inside a slice; the host runs the filtered full gate before push.
```

#### K4

```text
Rigor: S4 thorough; S1, S2, S3, S5 standard.
```

### Reuse

ContextContractsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembership\.cs\:44

Reuse

```text
Owns the measured published set per context; each slice moves exactly its entries; unchanged.
```

ContextContractsStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsStringsTheMoveRenames\.cs\:11

Reuse

```text
Fixed rename list for test literals naming a moved type; both forms admitted, never edited.
```

SHARED\_REGEX

Scripts\/test\-selection\/path\-classifier\.sh\:29

Extend

```text
Existing CI shared-path classifier; S4 adds Contracts to the TrackerIntegration (Application|Domain) alternation.
```

CrossContextEdgesOutsideContracts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:51

Extend

```text
The R-C1 ratchet list from feature A; each slice deletes its publishers' entries; the rule code is reused unchanged.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No wire, schema, config or persisted contract changes: types change namespace only inside one deployable, producers and consumers change in the same commit, no type is serialised by its full name (D-0).
```

contract_tests

```text
Single deployable; every producer and consumer of a moved type compiles in the same commit; no persisted or wire data names a moved type.
```

### Boundaries

```text
The compiled Lighthouse.Backend assembly read in-process through IlDependencies by the Architecture tests; nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

#### Driven ports

```text
None changed: repositories, tracker connectors and the domain-event dispatcher keep their ports and implementations
```

```text
Consumers in other contexts depend on Modules.<X>.Contracts; Contracts depend only on the kernel, BCL, Microsoft.Extensions and other Contracts; Domain, Application and Adapters of X stay private to X except listed residue.
```

#### Failures

##### B1

```text
An edge outside Contracts not on the list, or a listed edge no longer made
```

Refusal

```text
R-C1 test fails naming the edge, why, and how to fix
```

### Observations

```text
Architecture test verdicts and failure texts naming unlisted or gone edges, from the dotnet test Architecture filter
```

NUnit\-verdict

```text
Runtime behaviour; const-only references inlined by the compiler; references from Services, Data, API (not contexts)
```

### Equality

behaviour

```text
Before and after a slice the same types exist with the same members and implementations and the same DI registrations; only full names differ by the Application/Domain to Contracts segment.
```

### Constructors

None.

### Laws

#### L1

```text
After slice Sn, every IL edge from a context type into another context targets that context's Contracts or is in the shrunk list, and every listed edge is still made.
```

Invariant

Application

ContextContractsArchUnitTest

##### Treatment

Finite

###### Cases

```text
Every edge of the compiled assembly (exhaustive)
```

```text
Planted fixture types in the test assembly
```

```text
Closed finite domain: the compiled assembly, evaluated exhaustively
```

##### Evidence

None.

#### L2

```text
A namespace move preserves behaviour: no member, implementation or registration changes; the registration order pin and every unchanged acceptance test stay green.
```

Refinement

Application

DomainEventHandlerOrderPinTest

##### Treatment

NoTest

```text
Observed by the existing unchanged suite and the order pin; no new test adds evidence over a namespace-only diff
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
After S5 no type of one context names another context's Application, Domain or Adapters except the listed residue.
```

```text
A context changes an internal type and silently breaks another; S09b cannot cut assemblies on Contracts.
```

ContextContractsArchUnitTest

```text
The compiled production assembly
```

ModuleRules\.ReachesOtherContextsOnlyThroughContracts

R\-C1 test and S09b

```text
dotnet test Architecture filter verdict
```

```text
Constants inlined by the compiler (CsvWorkTrackingOptionNames), reflection by string name, code in Services/Data/API outside any context; the build and the S4 reflection check cover the first
```

### States

Admitted

```text
Cross-context edge into the target's Contracts
```

None.

Admitted

```text
Listed residue edge onto a ReachedButNotPublished type
```

owner named in its list comment

Excluded

```text
Unlisted edge into another context's Application, Domain or Adapters
```

None.

Unknown

```text
Const-only and reflection-by-name references invisible to the IL
```

S09b

### Models

None.

### Closure

```text
context contracts
```

Safety

Pass

DesignedNotBuilt

```text
R-C1 compares edges both ways over the whole compiled assembly; an unlisted edge fails
```

##### Evidence

None.

```text
context contracts
```

Inhabitation

Pass

DesignedNotBuilt

```text
Every moved type keeps its implementations and DI registrations; the build and the suite construct them
```

##### Evidence

None.

```text
context contracts
```

PublicReachability

Pass

DesignedNotBuilt

```text
Compiled assembly -> IlDependencies -> R-C1/R-C2/R-C3 -> dotnet test Architecture exit and message
```

##### Evidence

None.

```text
context contracts
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous evaluation of a compiled assembly; no process must progress
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: the IL shows every cross-context reference. Negated: constants inline, so CsvWorkTrackingOptionNames is invisible
```

```text
Every IL edge is checked
```

```text
A const-only reference stays outside the rule
```

```text
A const-only cross-context reference reported as covered
```

```text
Const-only references are caught only by the build once the type moves and by S09b's assembly split
```

TrackerIntegration

#### Y2

Stressor

```text
Assumption: moving a type keeps every rule seeing it. Negated: layer-keyed patterns stop seeing it
```

```text
Front-door patterns widened permanently in feature A
```

```text
None
```

```text
A rule silently passing over a moved type
```

```text
The pattern-role test of feature A covers declared patterns; an undeclared new pattern fails that test
```

Architecture tests

#### Y3

Stressor

```text
Assumption: CI selection follows a folder move. Negated: SHARED_REGEX names Application|Domain only
```

```text
A connector-port change runs every live suite
```

```text
None
```

```text
A Contracts change to the connector port selecting no live suite
```

```text
None after S4 adds Contracts in the same commit
```

S4

#### Y4

Bypass

```text
Test doubles or literals spelling a moved type by its old full name
```

```text
The build fails on an old using; the rename-list test names old literals
```

```text
None
```

```text
A stale string silently passing
```

```text
Strings outside the test project are not swept; measured: only test-path-classifier.sh outside .cs names an old path
```

S4

#### Y5

SinglePointOfFailure

```text
The single inline R-C1 ratchet list in ContextContractsArchUnitTest, edited by every slice
```

```text
Compared both ways; it is a craft target, never an oracle, so editing it in a later slice stales no approval
```

```text
None
```

```text
An entry deleted without its move landing, or an earlier slice's oracle edited later
```

```text
Each slice's own oracle checks its types moved; R-C1 checks the list both ways
```

Crafter per slice

#### Y6

Stressor

```text
Second set (walkthrough, not execution): a later Story adds a new caller of a moved type through the old name
```

```text
Compile error: the old namespace no longer holds the type
```

```text
None
```

```text
A silent new edge
```

```text
None
```

Build

### Architectural tests

#### T1

```text
R-C1: a type of one context reaches another context only through its Contracts or a listed edge, both ways; R-C2: Contracts reach only kernel, BCL, Microsoft.Extensions and Contracts; R-C3: contexts equal the ten; membership closure and pattern roles hold.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Contracts\/IAuthModeResolver\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Contracts\/IRbacAdministrationService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Contracts\/ILicenseService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ApiKeyControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthorizationControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/AdditionalFieldsHelperTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/WriteBackMappingsHelperTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedStalenessThresholdValidationTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldEnforcementTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionSettingsIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamSettingsIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OrderedParentSources\/ParentSourcesSettingsApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioConcurrencyTokenIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioOwningTeamSaveIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioStalenessThresholdSettingsIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RbacGroupMappingConcurrencyTokenIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice06TheWarningsWithoutTheLogSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHostPerFixtureAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamConcurrencyTokenIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamStalenessThresholdSettingsIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WriteBackMappingForNewAdditionalFieldIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportOwnerWriteAccessTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S4\_DeliveriesDeleteGuardInversionTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SuggestionsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DefaultFeatureSizing\/NewPortfolioStartingValueTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRulesNameOnlyForecastingArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/BlockedModeFilterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/RbacGuardAttributeTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/FeatureMoveAuthorizationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterRuleServiceIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttributeTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/LicensingIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/VoterIdentityResolverTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportCommandsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportLoggingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportQueriesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentPruningServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataInstancePropertiesTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForwardedHeaders\/ForwardedHeadersOidcTestHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestWebApplicationFactory\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/AdditionalFieldsHelper\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/WriteBackMappingsHelper\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportNotesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportNotesControllerBase\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportNotesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/AccessModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/BlockedModeFilter\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/RbacGuardAttribute\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/FeatureMoveAuthorization\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthModeResolver\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IRbacAdministrationService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttribute\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/LicensingModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/VoterIdentityResolver\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportCommands\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportQueries\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyDecision\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/FeatureDependencyRendering\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentPruningService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataInstanceProperties\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet build Lighthouse.Backend
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run LicensingAndAccessPublishTheirContractsArchUnitTest.cs (Architecture filter) over the compiled production and test assemblies: take every ContextContractsMembership entry of Licensing and Access that is not an event, look it up in its Contracts namespace and its old namespace, and scan the IL for any type outside Licensing and Access naming it through its old namespace.
```

```text
RED before the move: each of ILicenseService, IAuthModeResolver, IRbacAdministrationService is found in its old namespace and not in Lighthouse.Backend.Modules.Licensing.Contracts and Lighthouse.Backend.Modules.Access.Contracts. GREEN after: each resolves to exactly one top-level type in Lighthouse.Backend.Modules.Licensing.Contracts and Lighthouse.Backend.Modules.Access.Contracts, none is left in the old namespace, no type names one through the old name, and with R-C1's entries for these targets deleted every Architecture rule passes.
```

```text
A Licensing and Access published type left in its old namespace, present in both, moved to another context's Contracts, or a membership entry matching no type (vacuous list): the oracle fails naming the type. A stale R-C1 entry or a residue entry deleted fails R-C1 as gone or unlisted.
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

None.

##### Impact

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Contracts\/IAuthModeResolver\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Contracts\/IRbacAdministrationService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Contracts\/ILicenseService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ApiKeyControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AuthorizationControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EmbedStartControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/AdditionalFieldsHelperTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/WriteBackMappingsHelperTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedStalenessThresholdValidationTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldEnforcementTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionSettingsIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamSettingsIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/Slice01FeaturesViewSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OrderedParentSources\/ParentSourcesSettingsApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioOwningTeamSaveIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioStalenessThresholdSettingsIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RbacGroupMappingConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowConnectionAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice06TheWarningsWithoutTheLogSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHostPerFixtureAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamStalenessThresholdSettingsIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WriteBackMappingForNewAdditionalFieldIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LicenseControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/OptionalFeaturesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportOwnerWriteAccessTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/DeleteUnitRightsTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/F\_BE\_1\_GroupSnapshotInheritanceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S17\_LicenseDetailsTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S18\_ApiKeyCreationScopeTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S4\_DeliveriesDeleteGuardInversionTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S5\_ApiKeyScopesTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SuggestionsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DefaultFeatureSizing\/NewPortfolioStartingValueTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRulesNameOnlyForecastingArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/EmbedSessionSingleUseConcurrencyTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/BlockedModeFilterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Adapters\/Http\/RbacGuardAttributeTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/FeatureMoveAuthorizationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterRuleServiceIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttributeTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/LicensingIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/VoterIdentityResolverTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportCommandsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportLoggingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportQueriesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentPruningServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataInstancePropertiesTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForwardedHeaders\/ForwardedHeadersOidcTestHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/JwtBearerAuth\/JwtBearerTestHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/McpInboundAuth\/McpInboundAuthTestHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestWebApplicationFactory\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ViewerEmbedTestHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedEntryController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedHandshakeController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EmbedStartController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/AdditionalFieldsHelper\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RbacAdministrationServiceExtensions\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/WriteBackMappingsHelper\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LicenseController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/OptionalFeaturesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportNotesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportNotesControllerBase\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SuggestionsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportNotesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/AccessModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/BlockedModeFilter\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Adapters\/Http\/RbacGuardAttribute\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/FeatureMoveAuthorization\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IAuthModeResolver\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/IRbacAdministrationService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/RbacAdministrationService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Adapters\/Http\/LicenseGuardAttribute\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/ILicenseService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/LicensingModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/VoterIdentityResolver\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportCommands\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportQueries\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyDecision\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/FeatureDependencyRendering\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentPruningService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataInstanceProperties\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

Changed

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: every moving file of this slice holds one top-level type (measured); new Contracts files hold one type each; consumer files change only a using line or a renamed literal, no design step, so they are left as they are. Nothing to split.
```

reuse

```text
No new owner in this slice: each moved type is reused as is (same members, implementations and registrations, only its namespace becomes Modules.<X>.Contracts; owner is the shared ContextContractsMembership entry); every other target only changes a using line or an old full-name literal.
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

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/ITeamMetricsService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IPortfolioMetricsService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IForecastDataSufficiency\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IHowManyForecaster\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IThroughputFilter\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/IForecastFilterRuleService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/IForecastService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRunOutcome\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/ForecastThroughputStatus\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/CycleTimeWorkItem\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/CycleTimeFeature\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/OutlierVerdict\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/UpperNaturalProcessLimit\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/CycleTimeObservation\.cs

Create

```text
Split out of a multi-type file before the move; content unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ICycleTimeOutlierDefinition\.cs

Create

```text
Split out of a multi-type file before the move; content unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/UpperNaturalProcessLimitOutlierDefinition\.cs

Create

```text
Split out of a multi-type file before the move; content unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRResult\.cs

Create

```text
Split out of a multi-type file before the move; content unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/CycleTimeOutliersDtoTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/ProcessBehaviourChartTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerCalendarDayAnchorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/RuleSetValidationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlackoutForecastShiftTestBase\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/BenchmarkPortfolio\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice03FeaturesListReadsScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterBacktestIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamForecastIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckForecastScript\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckPortAnswerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/StartForecastWallClockProbe\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/InstanceDayForecastWindowIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementNeedAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice02StopEndsAForecastScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice03BeyondTheHorizonScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastFilterSeamArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OverTimeReconstructionSeamArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputFingerprintAcrossProvidersTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastOnTheLaneIdentityOnPostgresTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/ProcessBehaviorSnapshotTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/CycleTimeOutlierDefinitionTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileFamiliesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/RetentionEdgeTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/XmRCalculatorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastDrawCoordinatesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceCrossTeamDependencyTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceDependencyTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceSchedulerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceStartDayTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/MultiTeamJointForecastTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/ForecastRunThatCannotEndTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SizingLogCommandsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SleYardstickResolverTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/StageRuleMatcherTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/PortfolioReportMetricSeriesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportMetricSeriesByOwnerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/TeamReportMetricSeriesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForecastServiceDoubles\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RuleSetValidation\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/ProcessBehaviourChart\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/CycleTimeOutlierDefinition\.cs

Extend

```text
Split: published type leaves, the rest stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IForecastDataSufficiency\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IHowManyForecaster\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IThroughputFilter\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileFamilies\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs

Extend

```text
Split: published type leaves, the rest stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastInputFingerprint\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastingAnswersToFlowMetrics\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastFilterRuleService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastRunOutcome\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/ForecastingModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SleYardstickResolver\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleHealing\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/PortfolioReportMetricSeries\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/TeamReportMetricSeries\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet build Lighthouse.Backend
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run FlowMetricsAndForecastingPublishTheirContractsArchUnitTest.cs (Architecture filter) over the compiled production and test assemblies: take every ContextContractsMembership entry of Flow metrics and Forecasting that is not an event, look it up in its Contracts namespace and its old namespace, and scan the IL for any type outside Flow metrics and Forecasting naming it through its old namespace.
```

```text
RED before the move: each of ITeamMetricsService, IPortfolioMetricsService, ForecastThroughputStatus, IThroughputFilter, IForecastDataSufficiency, IHowManyForecaster, CycleTimeFeature, CycleTimeWorkItem, OutlierVerdict, UpperNaturalProcessLimit, IForecastService, IForecastFilterRuleService, ForecastRunOutcome is found in its old namespace and not in Lighthouse.Backend.Modules.FlowMetrics.Contracts and Lighthouse.Backend.Modules.Forecasting.Contracts. GREEN after: each resolves to exactly one top-level type in Lighthouse.Backend.Modules.FlowMetrics.Contracts and Lighthouse.Backend.Modules.Forecasting.Contracts, none is left in the old namespace, no type names one through the old name, and with R-C1's entries for these targets deleted every Architecture rule passes.
```

```text
A Flow metrics and Forecasting published type left in its old namespace, present in both, moved to another context's Contracts, or a membership entry matching no type (vacuous list): the oracle fails naming the type. A stale R-C1 entry or a residue entry deleted fails R-C1 as gone or unlisted.
```

##### Promises

P2

#### Prefactoring

```text
L1 split, own commit before the move: ITeamMetricsService.cs -> ForecastThroughputStatus.cs, CycleTimeWorkItem.cs; IPortfolioMetricsService.cs -> CycleTimeFeature.cs; CycleTimeOutlierDefinition.cs -> OutlierVerdict.cs (sum type with Limited, NoLimits), CycleTimeObservation.cs, ICycleTimeOutlierDefinition.cs, UpperNaturalProcessLimitOutlierDefinition.cs; XmRCalculator.cs -> UpperNaturalProcessLimit.cs, XmRResult.cs; namespaces unchanged. Makes the Contracts move a pure per-file git mv. ForecastRunOutcome.cs is one sum type and stays whole; every other touched file holds one type or changes only a using line.
```

```text
Same IL: types, namespaces and edges unchanged, so the membership and every Architecture rule stay green
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~ContextContractsMembershipArchUnitTest"
dotnet build Lighthouse.Backend
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/ITeamMetricsService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IPortfolioMetricsService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IForecastDataSufficiency\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IHowManyForecaster\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/IThroughputFilter\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/IForecastFilterRuleService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/IForecastService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRunOutcome\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/ForecastThroughputStatus\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/CycleTimeWorkItem\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/CycleTimeFeature\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/OutlierVerdict\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Contracts\/UpperNaturalProcessLimit\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/CycleTimeObservation\.cs\:1

Migrated

```text
Split out of a multi-type file before the move; content unchanged
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ICycleTimeOutlierDefinition\.cs\:1

Migrated

```text
Split out of a multi-type file before the move; content unchanged
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/UpperNaturalProcessLimitOutlierDefinition\.cs\:1

Migrated

```text
Split out of a multi-type file before the move; content unchanged
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRResult\.cs\:1

Migrated

```text
Split out of a multi-type file before the move; content unchanged
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/CycleTimeOutliersDtoTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/ProcessBehaviourChartTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerCalendarDayAnchorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Helpers\/RuleSetValidationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlackoutForecastShiftTestBase\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/BenchmarkPortfolio\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice03FeaturesListReadsScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterBacktestIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamForecastIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckForecastScript\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckPortAnswerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/StartForecastWallClockProbe\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/InstanceDayForecastWindowIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesChartOverlayIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementNeedAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice02StopEndsAForecastScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice03BeyondTheHorizonScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastFilterSeamArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicenseGateSingleSourceArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OverTimeReconstructionSeamArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputFingerprintAcrossProvidersTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastOnTheLaneIdentityOnPostgresTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/ProcessBehaviorSnapshotTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/CycleTimeOutlierDefinitionTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileFamiliesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/RetentionEdgeTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/XmRCalculatorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastDrawCoordinatesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceCrossTeamDependencyTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceDependencyTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceSchedulerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceStartDayTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/MultiTeamJointForecastTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/ForecastRunThatCannotEndTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SizingLogCommandsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SleYardstickResolverTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/StageRuleMatcherTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/PortfolioReportMetricSeriesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportMetricSeriesByOwnerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/TeamReportMetricSeriesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/FeatureListServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ForecastServiceDoubles\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarnessTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastRealityCheckController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RuleSetValidation\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/ProcessBehaviourChart\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/CycleTimeOutlierDefinition\.cs\:1

Changed

```text
Split: published type leaves, the rest stays
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IForecastDataSufficiency\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IHowManyForecaster\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IThroughputFilter\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileFamilies\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs\:1

Changed

```text
Split: published type leaves, the rest stays
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastInputFingerprint\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastingAnswersToFlowMetrics\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastFilterRuleService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastRunOutcome\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/ForecastingModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SleYardstickResolver\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleHealing\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/PortfolioReportMetricSeries\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/TeamReportMetricSeries\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

Changed

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

##### Agreement

None.

##### Not applicable

reuse

```text
No new owner in this slice: each moved type is reused as is (same members, implementations and registrations, only its namespace becomes Modules.<X>.Contracts; owner is the shared ContextContractsMembership entry); split-out files keep their types unchanged; every other target only changes a using line or an old full-name literal.
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

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IBlockedItemService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRefreshLogService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRuleEvaluator\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRuleFieldProvider\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IWhatTheForecastWaitsFor\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListRowsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourceRefreshAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/SharedClockBaselineFixture\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice05LighterRoundsScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/FeatureTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemRules\/RuleEngineReuseCanaryTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RuleEvaluatorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateNotificationHubTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Seeding\/RefreshLogSeederTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/NothingWaitsForAnything\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/WaitsHandedStraightToTheForecast\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RuleSetValidation\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/BlockedItemService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IWhatTheForecastWaitsFor\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/WhatTheForecastWaitsFor\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/FeatureFieldProvider\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IBlockedItemService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRefreshLogService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleEvaluator\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleFieldProvider\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RuleEvaluator\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemFieldProvider\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/WorkItemsModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/RefreshLogSeeder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPublishesItsContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet build Lighthouse.Backend
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run WorkItemsPublishesItsContractsArchUnitTest.cs (Architecture filter) over the compiled production and test assemblies: take every ContextContractsMembership entry of Work Items that is not an event, look it up in its Contracts namespace and its old namespace, and scan the IL for any type outside Work Items naming it through its old namespace.
```

```text
RED before the move: each of IBlockedItemService, IRefreshLogService, IRuleEvaluator`1, IRuleFieldProvider`1, IWhatTheForecastWaitsFor is found in its old namespace and not in Lighthouse.Backend.Modules.WorkItems.Contracts. GREEN after: each resolves to exactly one top-level type in Lighthouse.Backend.Modules.WorkItems.Contracts, none is left in the old namespace, no type names one through the old name, and with R-C1's entries for these targets deleted every Architecture rule passes.
```

```text
A Work Items published type left in its old namespace, present in both, moved to another context's Contracts, or a membership entry matching no type (vacuous list): the oracle fails naming the type. A stale R-C1 entry or a residue entry deleted fails R-C1 as gone or unlisted.
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

None.

##### Impact

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IBlockedItemService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRefreshLogService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRuleEvaluator\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRuleFieldProvider\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IWhatTheForecastWaitsFor\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListRowsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourceRefreshAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/SharedClockBaselineFixture\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice05LighterRoundsScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/ForecastedStartDateAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/FeatureTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemRules\/RuleEngineReuseCanaryTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnectorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RuleEvaluatorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateNotificationHubTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Seeding\/RefreshLogSeederTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/NothingWaitsForAnything\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/WaitsHandedStraightToTheForecast\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Helpers\/RuleSetValidation\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryRuleService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/BlockedItemService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IWhatTheForecastWaitsFor\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/WhatTheForecastWaitsFor\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/FeatureFieldProvider\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IBlockedItemService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRefreshLogService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleEvaluator\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleFieldProvider\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RuleEvaluator\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemFieldProvider\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/WorkItemsModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/RefreshLogSeeder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

Changed

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: every moving file of this slice holds one top-level type (measured); new Contracts files hold one type each; consumer files change only a using line or a renamed literal, no design step, so they are left as they are. Nothing to split.
```

reuse

```text
No new owner in this slice: each moved type is reused as is (same members, implementations and registrations, only its namespace becomes Modules.<X>.Contracts; owner is the shared ContextContractsMembership entry); every other target only changes a using line or an old full-name literal.
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

None.

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDeliveryForecastPublisher\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDeliverySourceProvider\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDescriptionBlockMerger\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IFeatureSource\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IIncrementalSource\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWorkTrackingConnector\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWorkTrackingConnectorFactory\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWriteBackTarget\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/CsvWorkTrackingOptionNames\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/FeatureHasNoChildWorkItemsYetTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice05LighterRoundsScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice04QuietWriteBackScenarios\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowTeamSyncAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DefaultFeatureSizing\/NewPortfolioStartingValueTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationConnectorsLayoutArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembership\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackSuppressionWarningTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceResolverTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsBatchedWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ConnectorsWithoutDependenciesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvDemoJourneyWithStateMappingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnectorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraIncrementalSyncTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/IncrementalSourceCapabilityTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactoryTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceResolver\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingOptionNames\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IAzureDevOpsWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliveryForecastPublisher\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliverySourceProvider\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDescriptionBlockMerger\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IFeatureSource\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IIncrementalSource\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IJiraWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ILinearWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IServiceNowWorkTrackingConnector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnector\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnectorFactory\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWriteBackTarget\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactory\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactory\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Scripts\/test\-selection\/path\-classifier\.sh

Extend

```text
SHARED_REGEX gains Contracts for TrackerIntegration (E8)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

Scripts\/test\-selection\/test\-path\-classifier\.sh

Extend

```text
Classifier test gains the TrackerIntegration Contracts case beside the SHARED_REGEX change
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
bash Scripts/test-selection/test-path-classifier.sh
dotnet build Lighthouse.Backend
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run TrackerIntegrationPublishesItsContractsArchUnitTest.cs (Architecture filter) over the compiled production and test assemblies: take every ContextContractsMembership entry of Tracker integration that is not an event, look it up in its Contracts namespace and its old namespace, and scan the IL for any type outside Tracker integration naming it through its old namespace. Also run test-path-classifier.sh.
```

```text
RED before the move: each of IWorkTrackingConnector, IWorkTrackingConnectorFactory, IFeatureSource, IIncrementalSource, IDeliverySourceProvider, IDeliveryForecastPublisher, IDescriptionBlockMerger, IWriteBackTarget is found in its old namespace and not in Lighthouse.Backend.Modules.TrackerIntegration.Contracts. GREEN after: each resolves to exactly one top-level type in Lighthouse.Backend.Modules.TrackerIntegration.Contracts, none is left in the old namespace, no type names one through the old name, and with R-C1's entries for these targets deleted every Architecture rule passes. It also asserts by reflection that CsvWorkTrackingOptionNames lives in Lighthouse.Backend.Modules.TrackerIntegration.Contracts and not in Adapters.Connectors.Csv (a constants class the IL cannot show as an edge). test-path-classifier.sh classifies Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Contracts/IWorkTrackingConnector.cs as shared.
```

```text
A Tracker integration published type left in its old namespace, present in both, moved to another context's Contracts, or a membership entry matching no type (vacuous list): the oracle fails naming the type. A stale R-C1 entry or a residue entry deleted fails R-C1 as gone or unlisted.
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

None.

##### Impact

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDeliveryForecastPublisher\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDeliverySourceProvider\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IDescriptionBlockMerger\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IFeatureSource\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IIncrementalSource\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWorkTrackingConnector\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWorkTrackingConnectorFactory\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/IWriteBackTarget\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Contracts\/CsvWorkTrackingOptionNames\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/FeatureHasNoChildWorkItemsYetTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/Slice05PredefinedFieldScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice05LighterRoundsScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/Slice06SizeWarningInTheLogScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice04QuietWriteBackScenarios\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ServiceNowTeamSyncAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfoliosControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/S16\_ValidateEndpointAccessTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DefaultFeatureSizing\/NewPortfolioStartingValueTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorUnsupportedMembers\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationConnectorsLayoutArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembership\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackSuppressionWarningTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliverySourceResolverTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsBatchedWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/AzureDevOps\/AzureDevOpsIncrementalSyncTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/ConnectorsWithoutDependenciesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvDemoJourneyWithStateMappingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnectorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraDeliverySourceProviderTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraIncrementalSyncTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/IncrementalSourceCapabilityTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactoryTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastBlockRenderer\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceResolver\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Csv\/CsvWorkTrackingOptionNames\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IAzureDevOpsWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliveryForecastPublisher\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDeliverySourceProvider\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IDescriptionBlockMerger\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IFeatureSource\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IIncrementalSource\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IJiraWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ILinearWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IServiceNowWorkTrackingConnector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnector\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWorkTrackingConnectorFactory\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/IWriteBackTarget\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingConnectorFactory\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/WorkTrackingSystemFactory\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Scripts\/test\-selection\/path\-classifier\.sh\:1

Changed

```text
SHARED_REGEX gains Contracts for TrackerIntegration (E8)
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

Changed

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

rule\-file

Scripts\/test\-selection\/test\-path\-classifier\.sh\:1

Changed

```text
Classifier test gains the TrackerIntegration Contracts case beside the SHARED_REGEX change
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: every moving file of this slice holds one top-level type (measured); new Contracts files hold one type each; consumer files change only a using line or a renamed literal, no design step, so they are left as they are. Nothing to split.
```

reuse

```text
No new owner in this slice: each moved type is reused as is (same members, implementations and registrations, only its namespace becomes Modules.<X>.Contracts; owner is the shared ContextContractsMembership entry); every other target only changes a using line or an old full-name literal.
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

None.

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IAppSettingService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IBlackoutPeriodService\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IFeatureOrdering\.cs

Create

```text
New Contracts home (git mv); namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListRowsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterBacktestIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamForecastIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SurveyNudgeControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupStringsTheMoveRenames\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RefreshLogServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/SystemInfoServiceTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureGraphReaderRelationalTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureListReaderRelationalTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeaturesWithReferenceIdsRelationalTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastConfirmationRepositoryRelationalTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastGatherReaderRelationalTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureOrderingTestHelper\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/PortfolioDto\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/AppSettingService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureOrdering\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankSeeder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IAppSettingService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IBlackoutPeriodService\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureOrdering\.cs

Extend

```text
Moved away to Contracts (git mv source)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/OrganisationSetupModule\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryMetricValuesProjector\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthCadence\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/SystemInfoService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/InstallTimestampSeeder\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeaturePositionMap\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs

Extend

```text
Names a moved type: using line or old full-name literal changes only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

Extend

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet build Lighthouse.Backend
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run OrganisationSetupPublishesItsContractsArchUnitTest.cs (Architecture filter) over the compiled production and test assemblies: take every ContextContractsMembership entry of Organisation set-up that is not an event, look it up in its Contracts namespace and its old namespace, and scan the IL for any type outside Organisation set-up naming it through its old namespace.
```

```text
RED before the move: each of IAppSettingService, IBlackoutPeriodService, IFeatureOrdering is found in its old namespace and not in Lighthouse.Backend.Modules.OrganisationSetup.Contracts. GREEN after: each resolves to exactly one top-level type in Lighthouse.Backend.Modules.OrganisationSetup.Contracts, none is left in the old namespace, no type names one through the old name, and with R-C1's entries for these targets deleted every Architecture rule passes. It also asserts the open host is narrowed: no downstream module's set-up pattern or SetUpNamespacesOpenToDownstreamModules admits Organisation set-up's Application or Domain, only its Contracts.
```

```text
A Organisation set-up published type left in its old namespace, present in both, moved to another context's Contracts, or a membership entry matching no type (vacuous list): the oracle fails naming the type. A stale R-C1 entry or a residue entry deleted fails R-C1 as gone or unlisted.
```

##### Promises

P5

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

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IAppSettingService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IBlackoutPeriodService\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/IFeatureOrdering\.cs\:1

Migrated

```text
New Contracts home (git mv); namespace line only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/AppSettingsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/BlackoutPeriodsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerArchiveTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerUtcTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryRulesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliverySourcesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeatureListRowsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/FeaturesControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ForecastControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterBacktestIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterTeamForecastIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SurveyNudgeControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamsControllerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupStringsTheMoveRenames\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/NamedCycleTimeAnswerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastPublishingServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RefreshLogServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/SystemInfoServiceTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureGraphReaderRelationalTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureListReaderRelationalTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeaturesWithReferenceIdsRelationalTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastConfirmationRepositoryRelationalTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastGatherReaderRelationalTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureOrderingTestHelper\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/PortDoubles\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AppSettingsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/BlackoutPeriodsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/PortfolioDto\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SurveyNudgeController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/AppSettingService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureOrdering\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankSeeder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IAppSettingService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IBlackoutPeriodService\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/IFeatureOrdering\.cs\:1

Migrated

```text
Moved away to Contracts (git mv source)
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/OrganisationSetupModule\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryMetricValuesProjector\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthCadence\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/SystemInfoService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/InstallTimestampSeeder\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeaturePositionMap\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:1

Changed

```text
Names a moved type: using line or old full-name literal changes only
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

Changed

```text
Rule list the crafter edits: delete this slice's R-C1 entries (targets among its moved types)
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: set-up patterns admit only Contracts; Refinement lists its WeeklyRecurrence edge until feature D
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Changed

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Open-host narrowing: holds or composes the one set-up boundary owner ModuleRules.SetUpBehindItsContracts / SetUpContracts
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: every moving file of this slice holds one top-level type (measured); new Contracts files hold one type each; consumer files change only a using line or a renamed literal, no design step, so they are left as they are. Nothing to split.
```

reuse

```text
No new owner in this slice: each moved type is reused as is (same members, implementations and registrations, only its namespace becomes Modules.<X>.Contracts; owner is the shared ContextContractsMembership entry); every other target only changes a using line or an old full-name literal.
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

None.

##### Contract tests

None.

## ADRs

None.
