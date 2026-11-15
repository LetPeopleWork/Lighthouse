# Architecture brief: story\-6360\-s03b\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Behaviour-free, test-side and script-side only: no production type moves, no route, DTO, response, schema, refresh result, metrics answer, DI lifetime or handler order changes. Every rule reads the compiled IL through IlDependencies or the ArchUnitNET production architecture; inline lists stay two-way (InlineBaseline); no rule is loosened. DomainEventHandlerOrderPinTest stays green unmodified.
```
Governed slices: S1, S2, S3

#### D2

```text
Shared rules owner: ModuleRules and ModuleWording (Architecture folder) are reused; no rule is copied and no second rules class is created. KernelRules and ApplicationCoreRules already treat Modules as application core and need no change.
```
Governed slices: S1, S2

#### D3

```text
Made-up types live in the test assembly below Lighthouse.Backend.Modules.WorkItems (for example Lighthouse.Backend.Modules.WorkItems.Application.WorkItemsFixtures), with a compliant twin each and a namesake just outside the module (Lighthouse.Backend.Modules.WorkItemsFixtures), the S03c way: the widened patterns end in (\.|$), so only a type below Modules.WorkItems proves the real widened constant.
```
Governed slices: S1, S3

#### D4

```text
Formal verification: no checker selected, no model written or run. No state, transition, protocol, concurrency or recovery behaviour changes. Rule and membership claims are finite and decided exactly by the compiled IL with a made-up offender as falsifier; the refresh-preservation claim compares the code with itself, so literal expected values at the service port are the honest falsifier. Liveness not applicable: synchronous rule evaluation and a one-shot capture have no progress property. Property-based testing not applicable: every law is a finite enumeration of rules, types, refresh scenarios and named endpoints; the test-writing skill decides test density.
```
Governed slices: S1, S2, S3

#### D5

```text
Carriers, all test-side: rule verdicts are KernelRuleVerdict, InboundVerdict and OutboundVerdict owned by ModuleRules and KernelRules; dependency facts are IlDependent from IlDependencies; per-fixture state is the lazily read Mono.Cecil ModuleDefinition disposed at one-time teardown; the refresh oracle holds a ServicePortHarness per scenario; the compare script holds captured bodies in a temp directory. Layers: application rules only; no adapter, infrastructure or recovery layer changes.
```
Governed slices: S1, S2, S3

### Constraints

#### K1

```text
Fixture types placed under Lighthouse.Backend.Modules in the test assembly must not reach the API, Data or EF Core, nor be domain-event handlers, because ApplicationCoreRulesReachModulesArchUnitTest runs the core rules over the test assembly with the Services-or-Modules pattern.
```
Governed slices: S1

#### K2

```text
Examiner budget about 5 minutes wall and 3 probing calls reading only the output tail: each public command prints compact per-line results and a last VERDICT line, never raw dotnet output.
```
Governed slices: S3

#### K3

```text
One oracle file per slice; a later slice never edits an earlier slice's or an earlier feature's oracle or support files. No nWave scenario tags in code. Arch rule tests assert the rule failure names the offender, not counts. Comments carry no internal ids. New plain fixtures never go under Tests/Contracts.
```
Governed slices: S1, S2, S3

#### K4

```text
IlDependencies misses catch-clause types, event references and named attribute arguments; a rule or membership measurement that depends on one of those says so in its failure text.
```
Governed slices: S1, S2

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Reuse

```text
Shared module rules owner from S03c; no second rules class
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:20

Reuse

```text
Application-service port harness for the refresh oracle
```

forecast\-answers\-capture\.cs

Scripts\/examine\/build\-examine\-commands\:75

Reuse

```text
Template for refresh-answers-compare and its in-process capture driver (2xx and zero-answer guards)
```

forecast\-triggers\-show\.cs

Scripts\/examine\/build\-examine\-commands\:437

Reuse

```text
Template for refresh-reactions-show (user-level observation through the public API on demo data)
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes: rules, membership test, refresh oracle and scripts only observe the production assembly and existing HTTP responses, in one deployable and one commit range.
```

contract_tests

```text
No parties change apart: no wire, DTO or route changes; the compare script observes existing endpoints only.
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
Widened Work Items rules
```

Safety

Pass

DesignedNotBuilt

```text
Each widened rule has a made-up offender below Modules.WorkItems that the rule failure must name; host executes per slice
```

##### Evidence

None.

```text
Widened Work Items rules and membership test
```

Inhabitation

Pass

DesignedNotBuilt

```text
Compliant made-up twins and the real production assembly pass each rule; the real assembly passes the membership lists
```

##### Evidence

None.

```text
Refresh oracle and public commands
```

PublicReachability

Pass

DesignedNotBuilt

```text
Oracle drives ITeamDataService and IWorkItemService at the application-service port; commands call the real refresh and read endpoints on demo data
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: a widened pattern also matches the fixture namespace
```

```text
Each widened rule sees Modules.WorkItems
```

```text
none
```

```text
A rule green because the fixture sits outside the pattern
```

```text
Fixtures below Modules.WorkItems with a compliant twin and an outside namesake each
```

WorkItemsRulesSeeTheModuleArchUnitTest

#### Y2

Stressor

```text
Assumption negated: a refresh is deterministic for the same tracker data and clock
```

```text
Literal items, transitions, blocked periods and events
```

```text
Time-stamped fields masked or pinned by FakeLighthouseClock
```

```text
A comparison of the code with itself or a capture that compares zero answers or non-2xx answers
```

```text
Literal expected values in the oracle; script fails on zero answers and on any non-2xx
```

RefreshesStayTheSameTest

#### Y3

Stressor

```text
Assumption negated: widening the Dependencies namespace key to Modules.WorkItems keeps the forecast-isolation rule true after the move
```

```text
Forecast asks only IWhatTheForecastWaitsFor
```

```text
none
```

```text
Loosening the rule when feature B moves a type ForecastService reaches
```

```text
If B finds ForecastService reaches a moved non-dependency type, B returns it to the architect instead of loosening
```

feature\-b\-crafter

#### Y4

SinglePointOfFailure

```text
Demo data only on the examiner host
```

```text
Refresh answer equality on demo data
```

```text
Production-like comparison run by the maintainer
```

```text
Calling EXAMINE indeterminate for a missing production backup
```

```text
Maintainer-run comparison on the production-like database
```

maintainer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
WorkItemsSyncPattern gains |^Lighthouse\.Backend\.Modules\.WorkItems(\.|$); Dependencies and Parents stay out of it because the connectors call the selectors that stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Files located by name under the production source instead of fixed Services paths (reconciler, honour policy, decision); the one-folder keys become old-or-module folder; the selector path stays; TheNamespaceThisEpicOwns gains Modules.WorkItems
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs

Extend

```text
The two WorkItemService.cs cutoff entries locate the file by name under the production source, as FastWithManyFeaturesArchUnitTest does
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~WorkItemsRulesSeeTheModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run each widened rule, with its own Work Items key read from the rule (pattern constant, file locator or namespace key), over made-up types placed below Lighthouse.Backend.Modules.WorkItems in the test assembly, as subject and as target, and run the path-keyed rules over a made-up relative path under Modules/WorkItems
```

```text
For each rule the made-up offender below Modules.WorkItems is named in the rule failure, its compliant twin is not, and a namesake in Modules.WorkItemsFixtures is not treated as Work Items; path-keyed rules find the listed file by name wherever it lives; real-assembly rules stay green
```

```text
Any listed rule keeps its old Services-only key or fixed Services path, so the made-up type or file under Modules/WorkItems passes unseen or the rule reports the file as moved or deleted
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
Measured rule census (text search plus reading, 2026-11-15): widening needed only in ModuleBoundariesArchUnitTest (WorkItemsSyncPattern, read by the connector leaf rule), DependencySingleDecisionArchUnitTest (fixed Services/Implementation/Dependencies paths, the WhatThisEpicAddedToTheBackend folders and TheNamespaceThisEpicOwns) and CalendarDayAnchorSeamArchUnitTest (two WorkItemService.cs paths). Checked and left unchanged because they key on typeof or short names and see a type wherever it lives (feature B adds only a using): BlockedItemSinglePath, FetchShapingPropertyGuard, InstantsUnaffectedByZone, TimeInStateSeam, RuleEvaluatorPurity, ConnectionFetchShapeChangedSinglePublisher, ConnectionSaveArchitecture, DeliveryRuleServiceApiPreservation, DomainEventHandlerOrderPin, UpdateHoldsAreForecastOnly, UpdateCancellationContextWriter, DependencyAwareForecastSeam, PortGivensGuard, KernelRules (already sees Modules). Full-name baselines rewritten by the move, not widened here: FlowMetricsInboundApplication (R4) and FlowMetricsInboundDomainAndAdapters (R3) in B2, RepositoryLeakRatchet file paths in the move slice that moves WorkItemService.cs or RefreshLogService.cs, TestHostUseGuardTest.HostUsersStillToMove and the Forecasting placement lists in C3 or B2. Configs: only Scripts/test-selection path-classifier.sh and test-path-classifier.sh name a Work Items path (FetchFingerprint.cs); B1 updates them.
```

```text
The moved dependency files keep their own sub-folder and namespace inside the module: Modules/WorkItems/Domain/Dependencies/ and Modules/WorkItems/Application/Dependencies/ (whichever layer D-1 and B1 place each in; namespaces Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies and .Application.Dependencies). In DependencySingleDecisionArchUnitTest, WhatThisEpicAddedToTheBackend and TheOnlyPlaceThatMayRecordTheSource name exactly those two sub-folders beside the old Services folders, never a whole module layer, so WorkItemService stays outside the exempt folder and outside the blocked-word scan after the move. Feature B moves the dependency types into these sub-folders, not into the bare layer folder.
```

##### Constraints

None.

##### Reuse

DependencySingleDecisionArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:40

Extend

```text
Existing owner of the one-decision rules; keys widened in place
```

WorkItemsSyncPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:15

Extend

```text
Existing owner of the Work Items pattern; widened in place
```

TrackerHistoryCutoffs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:149

Extend

```text
Existing cutoff list; file located by name
```

TheOnlyFileNamed

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs\:173

Reuse

```text
Pattern for locating a production file by name; no second rule
```

##### Impact

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:15

Changed

```text
Test project only; pattern widened, real assembly unchanged
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:40

Changed

```text
Test project only; same rules, keys see Modules/WorkItems
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:149

Changed

```text
Test project only; same cutoffs, located by name
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
Widening is the change itself; no preserving move makes it easier
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~WorkItemsMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly with IlDependencies and collect every top-level type in the Work Items namespaces (Services.(Implementation|Interfaces).(WorkItems|TeamData|WorkItemRules|Dependencies|Parents) or Modules.WorkItems) plus the six handlers and admitted candidates by name, compiler-made nested types folded into their writer
```

```text
The collected set equals the inline Moves list plus the inline Stays list (each Stays entry names its owner), compared both ways by short type name so it holds before and after the move; a failure names each missing listed type and each unlisted Work Items type
```

```text
A made-up type added to a Work Items namespace, or a listed type removed or renamed, and the test stays green
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
Membership from D-1: Moves = the 27 core files' types (WorkItemService, IWorkItemService, TeamDataService, ITeamDataService, BlockedItemService, IBlockedItemService, SyncModeResolver, FetchFingerprint, RuleEvaluator<T>, IRuleEvaluator<T>, IRuleFieldProvider<T>, WorkItemFieldProvider, FeatureFieldProvider, DependencyDecision, DependencyHonourPolicy, DependencyReconciler, DependencyRefreshReporter, DependencyCycleDetector, DependencyFacts, FeatureDependencyRendering, WhatTheForecastWaitsFor, the five dependency interfaces, ParentPlacementReport and any further top-level type the IL finds in those files) plus WorkItemBlockedTransitionCaptureHandler, WorkItemBlockedTransitionCloseHandler, FeatureBlockedTransitionCaptureHandler, FeatureBlockedTransitionCloseHandler, TeamDeletedRefreshLogCleanupHandler, ConnectionFetchShapeChangedUpdateTriggerHandler. Stays = DependencySourceSelector, DependencyFieldReferences, ParentSourceSelector (Tracker integration, S03a). Candidate rule: a candidate is admitted when its IL outbound edges reach only Models, repositories, platform settings/logging and Work Items types, no context downstream of Work Items and no EF Core or Data. Source reading (not yet IL): RefreshLogService and IRefreshLogService reach Models, IAppSettingService, repositories and logging only, so admitted; OrphanedFeatureCleanupService reaches Lighthouse.Backend.Data and EF Core directly, so it and IOrphanedFeatureCleanupService stay (owner: the Work Items context, after a repository port exists; not this Story). The S2 crafter confirms from the IL; if the IL disagrees, the IL answer is recorded in the oracle and returned to the architect before feature B.
```

```text
Per-type kernel reach of the Work Items Models types (E5) is measured with IlDependencies during S2 and reported in the slice hand-back for the host to record in the feature workspace as the S04b list; it is not a test, because no Models type moves in S03b.
```

```text
Candidate admission, measured from the IL by S2: RefreshLogService and IRefreshLogService move (Application). OrphanedFeatureCleanupService stays: it reaches Data.LighthouseAppContext and EF Core directly; owner Work Items once it reads Features through a repository port. Amended admission rule: an interface whose only implementation stays, stays with it, because a port does not move ahead of the class that implements it (otherwise a Services class implements a Work Items module port, and PortfolioUpdater and Program reach into the module for a service that is not yet the module's). So IOrphanedFeatureCleanupService is on Stays with the same owner as its implementation, although its own IL edges reach only System. This replaces the provisional candidate reading in the earlier S2 membership decision.
```

```text
The IL found six top-level types in the moving files that D-1 does not name: DependenciesAsRead, DependencyLoop, DependencyLoops, RecordPlacement, DependencyHonourInput, FeatureDependencyFacts. All six are on Moves with the file that declares them.
```

##### Constraints

None.

##### Reuse

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:1

Reuse

```text
IL reader for type collection and edges
```

InlineBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs\:1

Reuse

```text
Two-way inline list comparison
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
New oracle file only; no existing code, consumer or build metadata changes
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

prefactoring

```text
Adds one new test; no existing code to prepare
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

Scripts\/examine\/build\-examine\-commands

Extend

```text
Committed generator: writes Scripts/examine/bin/refresh-answers-compare with its in-process capture driver, and bin/refresh-reactions-show with its probe program, both inline as forecast-answers-capture.cs is; bin is git-ignored
```

\.nwave\/examine\.json

Extend

```text
Declares refresh-answers-compare and refresh-reactions-show before the first EXAMINE
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~RefreshesStayTheSameTest
bash Scripts/examine/build-examine-commands
bash Scripts/examine/bin/refresh-answers-compare
bash Scripts/examine/bin/refresh-reactions-show
```

#### Supports

None.

#### Public oracle

```text
Through ServicePortHarness with FakeLighthouseClock and a CSV-backed tracker, refresh a Team through ITeamDataService and a Portfolio through IWorkItemService, then read stored items, state transitions, blocked periods, parent placements and the published domain events
```

```text
Items, transitions, blocked periods, parent placements and published events equal literal values written in the test; refresh-answers-compare with no argument prints one line per answer kind and VERDICT IDENTICAL comparing the pinned base revision with HEAD on demo data, failing on zero answers or any non-2xx; refresh-reactions-show prints one line per observed Work Items reaction after a Team refresh and VERDICT OBSERVED
```

```text
A changed transition, a dropped blocked period, a missing or reordered event, or a refresh that writes a different item, and the test or compare stays green
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
refresh-answers-compare is built like forecast-answers-compare: an in-process capture driver per revision on seeded demo data, forces a refresh of every Team and Portfolio, captures the bodies of the Team and Portfolio work item, Feature list, dependency and blocked endpoints, masks time-stamped keys, diffs base and candidate. No-argument base is pinned to the main revision the S3 commit is made on (the last revision before any Work Items type moves); feature B and C do not change it. refresh-reactions-show is built like forecast-triggers-show: starts this checkout's backend in-process on a fresh database, loads the smallest demo scenario with Teams, refreshes a Team through the public API and reads public answers (refresh history and blocked history) showing the Work Items reactions ran. Which reactions are observable through the public API without a premium licence: unknown, measured by the S3 crafter; any reaction with no public observation is named in the command output as not observable rather than claimed.
```

##### Constraints

None.

##### Reuse

forecast\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:314

Reuse

```text
Template; a new command because no refresh answer comparison exists
```

forecast\-triggers\-show

Scripts\/examine\/build\-examine\-commands\:436

Reuse

```text
Template for the reactions probe
```

commands

\.nwave\/examine\.json\:2

Extend

```text
Gains two entries beside the existing commands
```

##### Impact

examine\-generator

Scripts\/examine\/build\-examine\-commands\:1

Changed

```text
Writes two more commands; existing commands unchanged
```

examine\-declaration

\.nwave\/examine\.json\:1

Changed

```text
Two entries added; existing entries unchanged
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No dependency or owner law changes; the slice adds a behaviour oracle and examine commands
```

prefactoring

```text
New oracle and scripts only; no existing code to prepare
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
