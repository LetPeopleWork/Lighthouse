# Architecture brief: story\-6358\-s02b\-flow\-metrics\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Flow metrics move, oracle and craft shape (design owner, 2026-11-08).

Placement oracle: one test class, Lighthouse.Backend.Tests/Architecture/FlowMetricsModuleLayoutArchUnitTest.cs, extended by each slice's acceptance designer. It reads the compiled production assembly through IlDependencies (Mono.Cecil) and names the moved types as full-name strings (never typeof, so the test compiles before and after a move). Per slice it asserts, for each named type: it exists under its declared namespace (Lighthouse.Backend.Modules.FlowMetrics.Domain, .Application, .Application.Policies, .Adapters.Background or .Adapters.Persistence) and no type of that name is left under Lighthouse.Backend.Services. A missing type is a failure that names it. This is the placement canary the DESIGN calls R1; feature C replaces the old Metrics type-set rule with it.

Slice S1 also adds the Domain rule (R2): Lighthouse.Backend.Modules.FlowMetrics.Domain depends only on Lighthouse.Backend.Models and the base library (System.*), not on Application, Adapters, Services, Data, Microsoft.EntityFrameworkCore or Microsoft.Extensions.*; rule defined once in the KernelRules shape and run on the real module and on a made-up offender in the test assembly (an async-only edge included), with a compliant twin as negative control.

Craft: each move is a pure-move commit (`git mv`, namespace line, using lines in consumers, test using lines) separate from any edit commit. Files land in the folder matching their namespace (Modules/FlowMetrics/Domain, /Application, /Application/Policies, /Adapters/Background, /Adapters/Persistence). Records nested in a moved file move with it. Source-path strings in tests (OverTimeReconstructionSeamArchUnitTest, RepositoryLeakRatchetArchUnitTest allow-list, CategoryListOwnershipGuardTest) follow the move in the slice that moves the file. OverTimeFillTarget's two qualified references to Implementation.PercentileFamilies are an edit commit in S3. In S5, the repository rules (the GetAllByPredicate guard's folder exemption, repository detection, StoredRecordsPattern and PlatformPersistencePattern) widen to "Repositories folder or a module's Adapters/Persistence"; that is an edit commit, and `dotnet ef migrations has-pending-model-changes` (after a build, for each provider's migrations project) must report no changes. Program.cs gets using lines only; registration lines keep their place.

Behaviour-free proof per slice: the Architecture namespace (oracle, handler-order pin, application-core rules) is the declared command; the fast suite and dotnet build run at the end of each slice; the full filtered dotnet test and the behaviour capture (capture-metrics.sh on the feature base and on the candidate, back to back the same day, compare.sh exit 0) run once at the end of the feature.
```

### Constraints

None.

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/PercentileCalculator\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/SleRiskCalculator\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/BaselineValidationService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly: the namespaces of the five calculators, and every type the Lighthouse.Backend.Modules.FlowMetrics.Domain namespace depends on, async bodies included
```

```text
Each of the five calculators sits in Lighthouse.Backend.Modules.FlowMetrics.Domain, none is left under Lighthouse.Backend.Services, and Domain reaches only Lighthouse.Backend.Models types and the base library; a made-up Domain type that reaches a Services, Data, EF Core or Microsoft.Extensions type is reported
```

```text
A calculator still sits under Services, or Domain reaches a forbidden type and the rule stays green
```

##### Promises

P1

P2

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

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

UnchangedCompatible

```text
Program.cs gets using lines only; registrations keep their place and order (pinned by the handler-order test)
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/PercentileCalculator\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/SleRiskCalculator\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/BaselineValidationService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

##### Agreement

HTTP routes\, DTOs and serialized responses of the metrics surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API

Producer

UnchangedCompatible

```text
Controllers stay in API and are unchanged; the 366-request capture compares the answers before and after
```

##### Not applicable

reuse

```text
Pure move: every target is an existing file moved with git mv to its module folder unchanged (namespace and using lines only), and Program.cs gets using lines only; nothing is reused, extended or replaced
```

contract_tests

```text
No wire contract changes; the behaviour capture (docs/feature/story-6358-s02-metrics-pilot/capture) compares every metrics answer
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
The slice is itself a pure move; consumer using lines change in the same move commit
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentileSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentilesOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IOverTimeGapReconciler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IOverTimeHistoryFiller\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IBlockedCountSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentilesOverTimeSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly: the namespaces of the eleven contract types
```

```text
Each of the eleven contract types sits in Lighthouse.Backend.Modules.FlowMetrics.Application and none is left under Lighthouse.Backend.Services; the earlier layer checks stay green
```

```text
A contract type still sits under Services, or sits in the module but under the wrong layer
```

##### Promises

P1

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

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

UnchangedCompatible

```text
Program.cs gets using lines only; registrations keep their place and order (pinned by the handler-order test)
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentileSnapshotWriter\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSnapshotWriter\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentilesOverTimeSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IOverTimeGapReconciler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IOverTimeHistoryFiller\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IBlockedCountSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentilesOverTimeSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IProcessBehaviorSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

##### Agreement

HTTP routes\, DTOs and serialized responses of the metrics surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API

Producer

UnchangedCompatible

```text
Controllers stay in API and are unchanged; the 366-request capture compares the answers before and after
```

##### Not applicable

reuse

```text
Pure move: every target is an existing file moved with git mv to its module folder unchanged (namespace and using lines only), and Program.cs gets using lines only; nothing is reused, extended or replaced
```

contract_tests

```text
No wire contract changes; the behaviour capture (docs/feature/story-6358-s02-metrics-pilot/capture) compares every metrics answer
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
The slice is itself a pure move; consumer using lines change in the same move commit
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/MetricsCacheKey\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileFamilies\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilder\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentilesOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingPercentilesOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingProcessBehaviorSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeGapReconciler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/RetentionEdge\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly: the namespaces of the sixteen implementation types
```

```text
Each of the sixteen implementation types sits in Lighthouse.Backend.Modules.FlowMetrics.Application and none is left under Lighthouse.Backend.Services; the earlier layer checks stay green
```

```text
An implementation type still sits under Services, or sits under the wrong layer
```

##### Promises

P1

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

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

UnchangedCompatible

```text
Program.cs gets using lines only; registrations keep their place and order (pinned by the handler-order test)
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/MetricsCacheKey\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileFamilies\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilder\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentilesOverTimeSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingPercentilesOverTimeSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingProcessBehaviorSeriesQuery\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeGapReconciler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/RetentionEdge\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

##### Agreement

HTTP routes\, DTOs and serialized responses of the metrics surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API

Producer

UnchangedCompatible

```text
Controllers stay in API and are unchanged; the 366-request capture compares the answers before and after
```

##### Not applicable

reuse

```text
Pure move: every target is an existing file moved with git mv to its module folder unchanged (namespace and using lines only), and Program.cs gets using lines only; nothing is reused, extended or replaced
```

contract_tests

```text
No wire contract changes; the behaviour capture (docs/feature/story-6358-s02-metrics-pilot/capture) compares every metrics answer
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
The slice is itself a pure move; consumer using lines change in the same move commit
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly: the namespaces of the seven handlers; run the handler-order pin
```

```text
Each of the seven handlers sits in Lighthouse.Backend.Modules.FlowMetrics.Application.Policies and none is left under Lighthouse.Backend.Services; the handler-order pin and the handler rules stay green
```

```text
A handler still sits under Services, or the pinned handler order changes
```

##### Promises

P1

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

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

UnchangedCompatible

```text
Program.cs gets using lines only; registrations keep their place and order (pinned by the handler-order test)
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

##### Agreement

HTTP routes\, DTOs and serialized responses of the metrics surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API

Producer

UnchangedCompatible

```text
Controllers stay in API and are unchanged; the 366-request capture compares the answers before and after
```

##### Not applicable

reuse

```text
Pure move: every target is an existing file moved with git mv to its module folder unchanged (namespace and using lines only), and Program.cs gets using lines only; nothing is reused, extended or replaced
```

contract_tests

```text
No wire contract changes; the behaviour capture (docs/feature/story-6358-s02-metrics-pilot/capture) compares every metrics answer
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
The slice is itself a pure move; consumer using lines change in the same move commit
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly: the namespaces of the filler and the three repositories; run the repository rules
```

```text
The filler sits in Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background and the three repositories in Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence, none left under Lighthouse.Backend.Services; the repository-detection and GetAllByPredicate rules still treat the three as repositories
```

```text
A type still sits under Services, or a moved repository is no longer seen by a repository rule
```

##### Promises

P1

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

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

UnchangedCompatible

```text
Program.cs gets using lines only; registrations keep their place and order (pinned by the handler-order test)
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

namespace move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepository\.cs

Migrated

```text
Moved here unchanged from Lighthouse.Backend.Services; namespace changes to its module layer, every consumer gets a using line, behaviour unchanged
```

##### Agreement

HTTP routes\, DTOs and serialized responses of the metrics surface

Lighthouse\.Backend\/Lighthouse\.Backend\/API

Producer

UnchangedCompatible

```text
Controllers stay in API and are unchanged; the 366-request capture compares the answers before and after
```

##### Not applicable

reuse

```text
Pure move: every target is an existing file moved with git mv to its module folder unchanged (namespace and using lines only), and Program.cs gets using lines only; nothing is reused, extended or replaced
```

contract_tests

```text
No wire contract changes; the behaviour capture (docs/feature/story-6358-s02-metrics-pilot/capture) compares every metrics answer
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
The slice is itself a pure move; consumer using lines change in the same move commit
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
