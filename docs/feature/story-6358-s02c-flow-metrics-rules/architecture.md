# Architecture brief: story\-6358\-s02c\-flow\-metrics\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Design source: docs/feature/story-6358-s02-metrics-pilot/feature-delta.md (Ratchet design R1 to R7, slices 6, 7, 9), recorded, not re-decided. Paradigm object_oriented. Every rule reads the compiled production assembly (Lighthouse.dll) through the existing IlDependencies (Mono.Cecil), never ArchUnitNET's graph, because that graph drops edges written inside async bodies in Release. The module is found by namespace: Lighthouse.Backend.Modules.FlowMetrics.{Domain, Application, Application.Policies, Adapters.Background, Adapters.Persistence}; outside = any type whose namespace does not match ^Lighthouse\.Backend\.Modules\.FlowMetrics(\.|$); composition root = Lighthouse.Backend.Program. Baselines are inline lists of full type names (R3, R4: outside dependent; R5: 'Dependent -> Target' pairs) compared both ways.
```

#### D2

```text
Rule owners (test supports): the module rules are added as new static methods on the existing Lighthouse.Backend.Tests/Architecture/FlowMetricsModuleRules.cs, each taking its namespaces as parameters (same shape as KernelRules: one definition run on real and made-up types), returning the existing KernelRuleVerdict carrier where no baseline is involved. The two-way comparison is lifted, not copied, from the private RepositoryLeakRatchetArchUnitTest.AssertMatchesBaseline into a new shared support Lighthouse.Backend.Tests/Architecture/InlineBaseline.cs (internal static, same message: newcomers named with the fix hint, stale entries named with 'delete'); RepositoryLeakRatchetArchUnitTest calls it and keeps its own six comparison tests. S1 creates InlineBaseline.cs and owns it; S2 and S3 only call it.
```

#### D3

```text
One oracle file per slice (lesson from feature B, where a shared oracle made earlier approvals stale): S1 FlowMetricsInboundDomainAndAdaptersArchUnitTest.cs, S2 FlowMetricsInboundApplicationArchUnitTest.cs, S3 FlowMetricsApplicationOutboundArchUnitTest.cs, S4 FlowMetricsUnitTestPlacementTest.cs, all under Lighthouse.Backend.Tests/Architecture. A later slice adds files; it never edits an earlier slice's oracle. Each oracle holds its rule on the real module with its inline baseline, plus made-up-types fixtures (types declared in the oracle file under Lighthouse.Backend.Modules.FlowMetricsFixtures.* and outside fixture namespaces, read from the test assembly) for: a newcomer fails and is named, a stale baseline entry fails and is named, a compliant twin passes. S2 adds an outside fixture that reaches the Application only inside an async method, run also with -c Release.
```

#### D4

```text
Measured baselines (compiled Debug and Release builds of HEAD a40ba3230, identical): R3 Domain inbound = 6 types, same as feature-delta: API.PortfolioController, API.PortfoliosController, API.TeamController, API.TeamsController, Services.Implementation.Reports.Metrics.DailySeriesSummary, Services.Implementation.WorkItems.WorkItemService. R3 Adapters inbound = only Lighthouse.Backend.Program, as designed. R4 Application inbound outside API/Program/Reports/Refinement = 10 entries, feature-delta said 8: adds Services.Implementation.Forecast.ForecastInputs and the nested type Services.Implementation.Forecast.ForecastService+GatheredInputs (IlDependencies reports a hand-written nested type under its own name); the other 8 match (ForecastService, ForecastInputFingerprint, ForecastRealityCheckService, TeamData.TeamDataService, WorkItems.WorkItemService, WriteBackTriggerService, FeatureListActiveWork, DatabaseManagement.DatabaseMaintenanceGate). Legal R4 dependents seen: API ForecastController, ForecastRealityCheckController, PortfolioMetricsController, TeamFeatureSizeMetricsController, TeamMetricsController; Program; Reports PortfolioReportMetricSeries, TeamReportMetricSeries; Refinement RefinementNeedCalculator, SleYardstickResolver. R5 Application outbound to EF Core, Data, module Adapters, API = empty. R5 Forecasting edges = 3 pairs, feature-delta said 1 plus 1 invisible: BaseMetricsService -> Services.Interfaces.Forecast.IForecastService (the service-locator call is visible in IL as the generic argument of GetRequiredService), TeamMetricsService -> Services.Interfaces.Forecast.IForecastFilterRuleService, TeamMetricsService -> Services.Implementation.Forecast.ForecastDataSufficiencyPolicy (not in feature-delta). Recorded lists are the measured ones; feature-delta's open question 5 is answered: no source scan needed.
```

#### D5

```text
Open points taken with the recommended option: (a) Forecasting is matched by the existing ForecastingPattern of ModuleBoundariesArchUnitTest (made internal), Reports and Refinement by its ReportsPattern and RefinementPattern, API and Data by ApplicationCoreRules.TheApi and TheData, EF Core by ^Microsoft\.EntityFrameworkCore(\.|$); (b) R4 lists nested hand-written types as IlDependencies reports them, no folding; (c) the path-string tests (OverTimeReconstructionSeamArchUnitTest, RepositoryLeakRatchetArchUnitTest allow-list, CategoryListOwnershipGuardTest) already name Modules/FlowMetrics paths since feature B moved the files, so S3 has nothing to edit there; (d) R1 = FlowMetricsModuleLayoutArchUnitTest from feature B plus the module namespaces added to ModuleBoundariesArchUnitTest.NamedModules_AreNonEmpty; the Metrics type-set rule is MetricsArchitectureTests.MetricsServices_ReadTransitions_OnlyViaRepositoryPorts_NeverViaDbSetDirectly with its opcode scanner, deleted in S3; the two name rules of that file stay (S07b). (e) S4 membership is the 34 unit-test classes whose subject is a module type (list in S4 decision); none is on test-time-budget.json or HostUsersStillToMove, so neither list changes.
```

#### D6

```text
Formal verification: no checker selected. The claims are finite and decidable over the compiled assembly (set equality of a measured dependent list against an inline list; namespace membership); there is no state machine, concurrency, temporal or liveness claim, and no representation change. Their checker is the native NUnit rule run on the real assembly plus exhaustive made-up-type examples (newcomer, stale, both, compliant, async-only, empty). Probe: quint and agda are not installed (which: not found), java present; neither is needed by any selected claim. Liveness: not applicable, the rules are synchronous single-pass reads with no progress obligation.
```

#### D7

```text
Lighthouse test rules (root context; the test-writing skill wins over nWave's on timing and PBT density): rules run in the fast suite without a host; no nWave scenario tags; comments rare and never cite internal references (R3, D-6, S05f, ADR numbers). Last lines quoted: .claude/skills/test-writing/SKILL.md ends 'End of SKILL.md'; references/backend.md ends 'End of backend.md'. Verification per slice: pwsh Lighthouse.Backend/Test-Fast.ps1 (fast loop) and the Architecture-filtered dotnet test (declared); never live tracker categories or a bare TestCategory=Integration.
```

### Constraints

#### K1

```text
No behaviour change: no production file changes in this feature; EF model unchanged; routes, DTOs, responses unchanged. Baselines are never loosened: an entry leaves the list only when the edge is gone. Old and new layouts coexist.
```

#### K2

```text
Scope out: registration extension for the module; read ports for GetAllByPredicate (S05f); moving entities or DTOs out of Models; renaming *Service roles; moving the Metrics controllers out of API.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

```text
dotnet test over Lighthouse.Backend.Tests, Architecture namespace: the rule reads the compiled production assembly Lighthouse.dll through IlDependencies
```

#### Driven ports

```text
Mono.Cecil ModuleDefinition of Lighthouse.dll and of the test assembly (made-up fixtures)
```

```text
Test rules depend on the production assembly's IL; production code depends on nothing in the tests
```

#### Failures

None.

### Observations

```text
Rule verdict: NUnit pass, or failure message naming each newcomer with the fix hint and each stale entry with 'delete'
```

NUnit outcome \+ message

```text
Edges invisible in IL: reflection by string name, DI resolution by non-generic Type argument created at runtime
```

### Equality

None.

### Constructors

None.

### Laws

#### L1

```text
A baseline check passes iff the set of measured entries equals the set of listed entries; otherwise it fails naming every newcomer and every stale entry in one message; order and duplicates do not matter
```

Invariant

Application

InlineBaseline

##### Treatment

Finite

###### Cases

```text
equal lists pass
```

```text
newcomer fails, named
```

```text
stale entry fails, named with delete
```

```text
newcomer and stale at once, both named
```

```text
reordered and repeated entries pass
```

```text
both empty pass
```

```text
Closed finite case set over a pure set comparison; the existing RepositoryLeakRatchet comparison tests already pin it
```

##### Evidence

None.

#### L2

```text
IlDependencies reports an edge written only inside an async method or lambda against the type that wrote it, in Debug and Release
```

Invariant

Adapter

IlDependencies

##### Treatment

Finite

###### Cases

```text
outside fixture reaching Application only inside an async method is reported
```

```text
One discriminating fixture per rule; finite
```

##### Evidence

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
namespace rules R1, R3, R4, R5
```

Safety

Pass

DesignedNotBuilt

```text
Measured with a Cecil probe mirroring IlDependencies on Debug and Release builds; rules not yet written
```

##### Evidence

None.

```text
namespace rules R1, R3, R4, R5
```

Inhabitation

Pass

Observed

```text
Module namespaces are non-empty in the compiled assembly (Domain 6, Application 34, Policies 7, Background 1, Persistence 3 top-level types)
```

##### Evidence

None.

```text
namespace rules R1, R3, R4, R5
```

PublicReachability

Pass

DesignedNotBuilt

```text
Reached through the declared dotnet test Architecture filter
```

##### Evidence

None.

```text
namespace rules R1, R3, R4, R5
```

Liveness

Pass

Observed

```text
Not applicable: synchronous single-pass reads, no progress obligation
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
Assumption: every edge is visible in IL. Negation: a type resolved by string name or a runtime-built Type
```

```text
Every compile-time edge is named
```

```text
A reflection-only edge stays unseen
```

```text
Claiming the rule covers reflection edges
```

```text
Only BaseMetricsService uses a service locator, and its generic call is visible
```

architect

#### Y2

Stressor

```text
Assumption: Debug and Release IL agree. Negation: Release drops or changes an edge
```

```text
Same lists in both
```

```text
none
```

```text
A green Debug run with a red Release CI
```

```text
Measured identical today; S2 runs the Architecture filter with -c Release
```

acceptance\-designer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Calls the lifted InlineBaseline instead of its private copy
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs

#### Public oracle

```text
Read the compiled production assembly and list every type outside Lighthouse.Backend.Modules.FlowMetrics that references a type in its Domain, and every one that references a type in its Adapters; run the same rules on made-up types in the test assembly
```

```text
Domain dependents equal the inline list of 6 (API.PortfolioController, API.PortfoliosController, API.TeamController, API.TeamsController, Services.Implementation.Reports.Metrics.DailySeriesSummary, Services.Implementation.WorkItems.WorkItemService); Adapters dependents are only Lighthouse.Backend.Program; a made-up outside newcomer fails and is named; a made-up stale list entry fails and is named with 'delete'; a compliant twin passes
```

```text
A new outside type reaches the Domain or Adapters and the rule stays green, or a listed type stops reaching it and the rule stays green
```

##### Promises

P1

#### Prefactoring

```text
Lift RepositoryLeakRatchetArchUnitTest.AssertMatchesBaseline into InlineBaseline.cs, behaviour-free, before the new rules use it
```

```text
RepositoryLeakRatchetArchUnitTest comparison and ratchet tests green with unchanged messages
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~RepositoryLeakRatchetArchUnitTest
```

#### Records

##### Decisions

```text
S1 adds DomainIsReachedFromOutsideOnlyBy and AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot (names indicative) to FlowMetricsModuleRules, parameterised by module, outside and target patterns; creates InlineBaseline.cs by lifting AssertMatchesBaseline out of RepositoryLeakRatchetArchUnitTest, which then calls it with unchanged behaviour (its six comparison tests stay green).
```

##### Constraints

None.

##### Reuse

DependentsIn

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:35

Reuse

```text
IlDependencies.DependentsIn answers every dependency question from the IL, async bodies included; no change
```

FlowMetricsModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs\:14

Extend

```text
Owner of the module's layer rules since feature B; same parameterised shape
```

AssertMatchesBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:351

Extend

```text
The one two-way comparison; lifted into InlineBaseline so every ratchet shares it, not copied
```

##### Impact

test support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:351

Changed

```text
Private helper replaced by a call to InlineBaseline; messages and its tests unchanged
```

test support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs\:14

Changed

```text
New rule methods added; existing methods unchanged
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract between deployables changes; test-only rules
```

contract_tests

```text
No independently changing producer and consumer; one test assembly
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
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

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj -c Release --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly (Debug and Release) and list every type outside Lighthouse.Backend.Modules.FlowMetrics, outside API, the composition root, Reports and Refinement, that references a type in the module's Application; run the same rule on made-up types including one that reaches the Application only inside an async method
```

```text
The dependents equal the inline list of 10: Services.Implementation.Forecast.ForecastService, ForecastService+GatheredInputs, ForecastInputs, ForecastInputFingerprint, ForecastRealityCheckService, TeamData.TeamDataService, WorkItems.WorkItemService, WriteBackTriggerService, FeatureListActiveWork, DatabaseManagement.DatabaseMaintenanceGate (all under Lighthouse.Backend.Services.Implementation); a made-up newcomer fails and is named; the async-only made-up newcomer fails and is named in Debug and Release; a stale entry fails and is named with 'delete'
```

```text
A new undeclared caller of the Application, or one written only inside an async method, leaves the rule green
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
S2 adds ApplicationIsReachedFromOutsideOnlyBy (name indicative) to FlowMetricsModuleRules; legal callers are matched by ApplicationCoreRules.TheApi, the composition root pattern ^Lighthouse\.Backend\.Program(\+|$), ModuleBoundariesArchUnitTest.ReportsPattern and RefinementPattern. Baseline is the measured 10, not feature-delta's 8.
```

##### Constraints

None.

##### Reuse

AssertMatchesBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:351

Reuse

```text
Lifted by S1 into InlineBaseline.cs; S2 calls it, does not edit it
```

FlowMetricsModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs\:14

Extend

```text
Adds the Application inbound rule beside the others
```

##### Impact

test support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs\:14

Changed

```text
One new rule method
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract between deployables changes; test-only rules
```

contract_tests

```text
No independently changing producer and consumer
```

architectural_tests

```text
The slice oracle is itself the architectural test
```

prefactoring

```text
Builds on S1's lifted baseline helper; nothing else to move first
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/MetricsArchitectureTests\.cs

Extend

```text
Delete the opcode scanner rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Canary of module namespaces; internal ForecastingPattern
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj -c Release --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and list every type in Lighthouse.Backend.Modules.FlowMetrics.Application that references EF Core, Lighthouse.Backend.Data, the module's Adapters, the API, or Forecasting; check the module's namespaces are non-empty; run the same rules on made-up types
```

```text
No Application type references EF Core, Data, Adapters or API; the Forecasting edges equal the inline list of 3 pairs (BaseMetricsService -> IForecastService, TeamMetricsService -> IForecastFilterRuleService, TeamMetricsService -> ForecastDataSufficiencyPolicy); made-up newcomer and stale entry each fail and are named; MetricsServices_ReadTransitions_OnlyViaRepositoryPorts_NeverViaDbSetDirectly and its opcode scanner no longer exist; NamedModules_AreNonEmpty names the five module namespaces
```

```text
An Application type reaches DbSet, LighthouseAppContext, an adapter, the API or a new Forecasting type and the rule stays green
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
S3 adds ApplicationReachesNothingBelowIt and ApplicationReachesForecastingOnlyThrough (names indicative) to FlowMetricsModuleRules; deletes the opcode scanner test and its helpers from MetricsArchitectureTests.cs (the two name rules stay); makes ModuleBoundariesArchUnitTest.ForecastingPattern internal and adds the five FlowMetrics namespaces to NamedModules_AreNonEmpty (R1). The no-EF rule over the whole Application covers what the scanner checked (DbSet of transitions in two services), so nothing is lost. Path-string tests need no edit (already updated in feature B).
```

##### Constraints

None.

##### Reuse

MetricsServices\_ReadTransitions\_OnlyViaRepositoryPorts\_NeverViaDbSetDirectly

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/MetricsArchitectureTests\.cs\:72

Replace

```text
Hand-written opcode scanner over a type set; the IL rule over the whole Application namespace is stricter and simpler
```

NamedModules\_AreNonEmpty

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:130

Extend

```text
Existing non-empty canary takes the module's namespaces
```

TheApi

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs\:22

Reuse

```text
ApplicationCoreRules.TheApi and TheData are the existing API and Data patterns
```

##### Impact

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/MetricsArchitectureTests\.cs\:72

Changed

```text
Scanner test and helpers deleted; name rules unchanged
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:130

Changed

```text
Canary extended; ForecastingPattern made internal
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract between deployables changes; test-only rules
```

contract_tests

```text
No independently changing producer and consumer
```

architectural_tests

```text
The slice oracle is itself the architectural test
```

prefactoring

```text
Deletion and canary extension need no preparatory move
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/BaselineValidationServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaselineValidationServiceTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/PercentileCalculatorTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PercentileCalculatorTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/SleRiskCalculatorTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SleRiskCalculatorTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/XmRCalculatorTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/XmRCalculatorTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BaseMetricsServiceNamedCycleTimeTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceNamedCycleTimeTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BaseMetricsServiceTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilderTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BlockedCountSeriesBuilderTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/GapAskingSeriesQueryTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/GapAskingSeriesQueryTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsCacheKeyTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/MetricsCacheKeyTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeGapReconcilerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/OverTimeGapReconcilerTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriterTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PercentileSnapshotWriterTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PortfolioMetricsServiceTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ProcessBehaviorSnapshotWriterTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamMetricsServiceSnapshotTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamMetricsServiceTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ClosedInRangeCharacterisationTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/InstanceDayItemBucketingTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemAgeAsOfDateTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/OverTimeFillTargetTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ReconstructionMemoTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/ReconstructionMemoTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/RetentionEdgeTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/RetentionEdgeTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/BlackoutConfigurationChangedMetricsInvalidationHandlerTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/BlockedCountSnapshotRecordingHandlerTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandlerTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DemoPercentilesBackfillHandlerTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/PercentilesOverTimeRecordingHandlerTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/ProcessBehaviorRecordingHandlerTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/OverTimeHistoryFillerTest.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepositoryTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/BlockedCountSnapshotRepositoryTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepositoryTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/PercentilesOverTimeSnapshotRepositoryTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepositoryTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/ProcessBehaviorSnapshotRepositoryTests.cs; namespace and using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsUnitTestPlacementTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Read the compiled test assembly and the test project's source tree for the 33 listed Flow metrics unit-test classes
```

```text
Each class lives in Lighthouse.Backend.Tests.Modules.FlowMetrics.<Domain|Application|Application.Policies|Adapters.Background|Adapters.Persistence>, its file sits in the matching folder under Lighthouse.Backend.Tests/Modules/FlowMetrics, no class of that name is left under Lighthouse.Backend.Tests.Services, and the whole filtered suite stays green with the same test count
```

```text
A listed class stays under Tests/Services, or its folder and namespace disagree, and the oracle stays green
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
S4 is a pure move (git mv; namespace and using lines only; class names unchanged so Stryker FullyQualifiedName~ filters keep matching). Membership, 33 classes. Domain: BaselineValidationServiceTest, PercentileCalculatorTest, SleRiskCalculatorTest, XmRCalculatorTest. Application: BaseMetricsServiceNamedCycleTimeTest, BaseMetricsServiceTests, BlockedCountSeriesBuilderTests, GapAskingSeriesQueryTest, MetricsAsOfGroupNameTest, MetricsCacheKeyTest, OverTimeGapReconcilerTest, PercentileSnapshotWriterTest, PortfolioMetricsServiceTests, ProcessBehaviorSnapshotWriterTest, TeamMetricsServiceSnapshotTests, TeamMetricsServiceTests, ClosedInRangeCharacterisationTest, InstanceDayItemBucketingTest, WorkItemAgeAsOfDateTest, OverTimeFillTargetTest, ReconstructionMemoTest, RetentionEdgeTest. Application/Policies: BlackoutConfigurationChangedMetricsInvalidationHandlerTest, BlockedCountSnapshotRecordingHandlerTests, DemoBlockedHistoryBackfillHandlerTests, DemoPercentilesBackfillHandlerTests, PercentilesOverTimeRecordingHandlerTests, PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest, ProcessBehaviorRecordingHandlerTests. Adapters/Background: OverTimeHistoryFillerTest. Adapters/Persistence: BlockedCount, PercentilesOverTime and ProcessBehavior SnapshotRepositoryTests. Excluded: SnapshotRecordedDayInstanceZoneTest starts a host (IntegrationFixture, on HostUsersStillToMove and TestHostPerFixtureGuardTest), so it is not a unit test and stays. None of the 33 is keyed in test-time-budget.json, HostUsersStillToMove or TestHostPerFixtureGuardTest (git grep), so no list moves.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/BaselineValidationServiceTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaselineValidationServiceTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/PercentileCalculatorTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PercentileCalculatorTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/SleRiskCalculatorTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SleRiskCalculatorTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/XmRCalculatorTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/XmRCalculatorTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BaseMetricsServiceNamedCycleTimeTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceNamedCycleTimeTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BaseMetricsServiceTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BaseMetricsServiceTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilderTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BlockedCountSeriesBuilderTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/GapAskingSeriesQueryTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/GapAskingSeriesQueryTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsCacheKeyTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/MetricsCacheKeyTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeGapReconcilerTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/OverTimeGapReconcilerTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriterTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PercentileSnapshotWriterTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PortfolioMetricsServiceTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ProcessBehaviorSnapshotWriterTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamMetricsServiceSnapshotTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamMetricsServiceTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ClosedInRangeCharacterisationTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/InstanceDayItemBucketingTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemAgeAsOfDateTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/OverTimeFillTargetTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ReconstructionMemoTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/ReconstructionMemoTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/RetentionEdgeTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/RetentionEdgeTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/BlackoutConfigurationChangedMetricsInvalidationHandlerTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/BlockedCountSnapshotRecordingHandlerTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandlerTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DemoPercentilesBackfillHandlerTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/PercentilesOverTimeRecordingHandlerTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/ProcessBehaviorRecordingHandlerTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/OverTimeHistoryFillerTest.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepositoryTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/BlockedCountSnapshotRepositoryTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepositoryTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/PercentilesOverTimeSnapshotRepositoryTests.cs
```

test move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepositoryTests\.cs

Migrated

```text
Pure move from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Repositories/ProcessBehaviorSnapshotRepositoryTests.cs
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract; test files only
```

contract_tests

```text
No independently changing producer and consumer
```

architectural_tests

```text
The slice oracle is itself the placement check
```

prefactoring

```text
The slice is itself a pure move
```

reuse

```text
Pure move: each target is an existing test file moved with git mv, namespace and using lines only; nothing is reused, extended or replaced
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
