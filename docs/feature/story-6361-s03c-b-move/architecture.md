# Architecture brief: story\-6361\-s03c\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Pure moves only: 31 Forecasting files git-mv from Services into Modules/Forecasting, type and file names unchanged, namespace = folder (Lighthouse.Backend.Modules.Forecasting.{Domain,Application,Application.Policies,Adapters.Background}). Each slice is a pure-move commit, then a separate edit commit for using lines, Flow metrics list strings and the Stryker path. Layers per D-4: Domain = 16 types reaching only kernel and BCL; Application = services, ports, ForecastLane, IForecastUpdater; Policies = four triggers; Adapters/Background = ForecastUpdater and ForecastInputsForgottenAtStartSeeder.
```

#### D2

```text
Checker mapping: no state machine or temporal claim exists (behaviour-free relocation), so Quint, TLA+/TLC and Agda are not applicable; liveness is not applicable (no new progress claim, no scheduling change). The dependency-direction and placement claims are checked by the IL-reading ArchUnit rules in the three slice oracles; behaviour preservation is checked by SeededForecastsStayTheSameTest and bash Scripts/examine/bin/forecast-answers-compare (base 39a7f598f vs HEAD). No property-based law: the finite relocation has no broad input domain.
```

#### D3

```text
Paradigm object_oriented: owners are unchanged; every moved type keeps its owner and responsibility. No domain decision moves into a service, no new class is created, no type is split or merged. Carriers: decisions stay in Domain policies and ForecastSimulation; effects stay in ForecastUpdater, ForecastLane and the seeder; per-process state stays in ForecastLane.
```

#### D4

```text
Handler order and registrations stay: Program.cs lines are untouched apart from using lines (DomainEventHandlerOrderPinTest stays green and unmodified). Tests covering moved types stay where they are; only their using lines change. Stryker file path in stryker.6094.backend.json follows ForecastRealityCheckService in B2.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTOs, serialized responses, forecast results, schema, handler order unchanged. Existing API, E2E and response tests stay green and unmodified apart from using lines. No bound on stored or validated inputs changes.
```

#### K2

```text
One oracle file per slice; earlier slices' oracle files are never edited. Rules are never loosened; Flow metrics list strings are rewritten to the new full names, both directions still compared. Made-up offenders live in fixture namespaces under Lighthouse.Backend.Modules.ForecastingFixtures.*. Per-test and property budgets are the defaults; no derogation.
```

#### K3

```text
Every dotnet command runs under flock -o on the shared lock file. Code-fact tool not consulted: file lists come from directory listing of the old folders (stated limit).
```

### Reuse

ForecastSimulation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Forecast\/ForecastSimulation\.cs\:1

Reuse

```text
The 16 Domain types keep their one owner each; the move changes only folder and namespace. CreateNew would duplicate the simulation rules.
```

ForecastService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Forecast\/ForecastService\.cs\:1

Reuse

```text
Application services and ports (ForecastService, ForecastRealityCheckService, ForecastFilterRuleService, ForecastLane, ForecastInputFingerprint, IForecastUpdater and ports) move unchanged into Modules/Forecasting/Application.
```

ForecastUpdater

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/ForecastUpdater\.cs\:1

Reuse

```text
ForecastUpdater and the start-up seeder move unchanged into Adapters/Background, like the history filler of the earlier module; the four triggers move into Application/Policies.
```

ForecastingRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:1

Reuse

```text
Existing module map and rules already see Modules/Forecasting; the layout oracles reuse the IL helpers and inline-list style.
```

### Impact

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Forecast\/ForecastService\.cs\:1

Migrated

```text
Files move by git mv; all consumers (controllers, PortfolioUpdater, Flow metrics services, DI in Program.cs, tests) only gain using lines.
```

arch\-rule\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:58

Changed

```text
Inbound list strings name moved types by full name; rewritten to the new namespaces in B2, still compared in both directions.
```

arch\-rule\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:56

Changed

```text
Outbound edge strings and typeof usings name moved types; rewritten to the new namespaces in B2.
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\.6094\.backend\.json\:7

Changed

```text
Mutate path of ForecastRealityCheckService.cs follows the file to Modules/Forecasting/Application in B2.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Using lines only; registrations and order stay.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastServiceTest\.cs\:1

Migrated

```text
Unit tests of moved types stay in place; only using lines change.
```

### Agreement

None.

### Not applicable

agreement

```text
No wire, schema or persisted contract is touched: DTOs, routes, entities and the database schema stay; the move changes only CLR namespaces inside one deployable that change in one commit.
```

contract_tests

```text
Producer and consumer are in one deployable and change in the same commit; SeededForecastsStayTheSameTest and forecast-answers-compare observe unchanged answers.
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

```text
Relocation of 31 Forecasting files
```

Safety

Pass

DesignedNotBuilt

```text
IL-reading layout rules in each slice oracle exclude a Forecasting type left in Services and a Domain type reaching beyond the kernel
```

##### Evidence

None.

```text
Relocation of 31 Forecasting files
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each type has an existing source file and a named destination folder
```

##### Evidence

None.

```text
Relocation of 31 Forecasting files
```

PublicReachability

Pass

DesignedNotBuilt

```text
Public observation is the compiled assembly read by ArchUnit plus the unchanged forecast answers through SeededForecastsStayTheSameTest and forecast-answers-compare
```

##### Evidence

None.

```text
Relocation of 31 Forecasting files
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no progress or scheduling behaviour is added or changed; ForecastLane and the updater are moved byte for byte
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
A file moves but its namespace or a DI using line is missed
```

```text
Forecast answers and handler order
```

```text
Build errors until usings are fixed
```

```text
Silent change in forecast results or handler order
```

```text
Reflection or string-named type lookups of moved types
```

crafter\; DomainEventHandlerOrderPinTest and SeededForecastsStayTheSameTest

#### Y2

Stressor

```text
Assumption: stryker, rule lists and tests name no other moved path
```

```text
Mutation run and rule lists compare both ways
```

```text
Stale string fails a rule or test loudly
```

```text
Loosened rule or silently stale list
```

```text
None known beyond the one stryker path
```

crafter and reviewer

### Architectural tests

#### T1

```text
Forecasting types sit in their layer folders and namespaces; Domain reaches only the kernel and the BCL; Flow metrics lists name the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/AddressableDrawStream\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/AddressableDrawStream.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/DayCounts\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/DayCounts.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/DrawStreamFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/DrawStreamFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastDataSufficiencyPolicy\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastDataSufficiencyPolicy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastRunPlan\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastRunPlan.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastSimulation\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastSimulation.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/HowTheRunEnded\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/HowTheRunEnded.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/OneWorkersShareOfTheRuns\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/OneWorkersShareOfTheRuns.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/RealityCheckVerdictPolicy\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/RealityCheckVerdictPolicy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/SimulatedRun\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/SimulatedRun.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/TrialRecordings\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/TrialRecordings.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/TrialState\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/TrialState.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/WhatTheRunsCouldNotFinish\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/WhatTheRunsCouldNotFinish.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/IDrawStream\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IDrawStream.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/IDrawStreamFactory\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IDrawStreamFactory.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastRunOutcome\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/ForecastRunOutcome.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs

Create

```text
slice oracle, written by the acceptance designer
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ForecastingDomainLayoutArchUnitTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SeededForecastsStayTheSameTest
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule: every type named in the Domain list lives in Lighthouse.Backend.Modules.Forecasting.Domain, none remains in Services, and the Domain namespace reaches only the kernel and the BCL
```

```text
The rule passes; a made-up offender in the fixture namespace Lighthouse.Backend.Modules.ForecastingFixtures reaching an Application type fails the rule and is named
```

```text
A Domain type left under Services.Implementation.Forecast or a Domain type referencing an Application or adapter type fails and names the type
```

##### Promises

P1

P4

#### Prefactoring

```text
L1 pure move: git mv the 16 Domain files, only the namespace line changes as far as the move requires; then a separate edit commit for using lines
```

```text
Forecast results, handler order, routes and DTOs; compile state restored by the edit commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

##### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SeededForecastsStayTheSameTest
```

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

reuse

```text
Pure move of existing files: each type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every moved file is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastFilterRuleService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastInputFingerprint\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastInputFingerprint.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastLane\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastLane.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastRealityCheckService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastFilterRuleService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IForecastFilterRuleService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastRealityCheckService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IForecastRealityCheckService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IForecastService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastUpdater\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IForecastUpdater.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
inbound list strings renamed to the new full names
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
outbound edge strings and usings renamed to the new full names
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\.6094\.backend\.json

Extend

```text
mutate path of ForecastRealityCheckService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

Create

```text
slice oracle, written by the acceptance designer
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ForecastingApplicationLayoutArchUnitTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SeededForecastsStayTheSameTest
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule plus the two Flow metrics rule lists: the nine Application types live in Lighthouse.Backend.Modules.Forecasting.Application and the lists name them by their new full names
```

```text
The layout rule passes and both Flow metrics rules pass with the rewritten lists compared in both directions
```

```text
An Application type left under Services, or a list string still naming a Services namespace, fails and names the type or edge
```

##### Promises

P2

P4

#### Prefactoring

```text
L1 pure move: git mv the nine Application files; then a separate edit commit for using lines, Flow metrics list strings and the Stryker path
```

```text
Forecast answers, DI registrations and handler order
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

##### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SeededForecastsStayTheSameTest
```

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

reuse

```text
Pure move of existing files: each type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every moved file is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/TeamDeletedForecastRetriggerHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/FeatureRankChangedForecastTriggerHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/FeatureOrderingPolicyChangedForecastTriggerHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastInputsForgottenAtStartSeeder\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Seeding/ForecastInputsForgottenAtStartSeeder.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines only; registration order unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.cs

Create

```text
slice oracle, written by the acceptance designer
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ForecastingAdaptersAndPoliciesLayoutArchUnitTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SeededForecastsStayTheSameTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DomainEventHandlerOrderPinTest
bash Scripts/examine/bin/forecast-answers-compare
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule, then run DomainEventHandlerOrderPinTest: ForecastUpdater and ForecastInputsForgottenAtStartSeeder live in Modules.Forecasting.Adapters.Background and the four trigger handlers in Modules.Forecasting.Application.Policies
```

```text
The layout rule passes and the handler order pin stays green unmodified
```

```text
A trigger left in BackgroundServices.Update or the updater outside Adapters.Background fails and names the type; a reordered registration fails the order pin
```

##### Promises

P3

P4

#### Prefactoring

```text
L1 pure move: git mv the six files; then a separate edit commit for using lines
```

```text
Handler order, hosted-service and seeder registration order, forecast answers
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

##### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/d78ed3b3-d5ed-4805-8941-b20488c04f2f/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DomainEventHandlerOrderPinTest
```

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

reuse

```text
Pure move of existing files: each type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every moved file is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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
