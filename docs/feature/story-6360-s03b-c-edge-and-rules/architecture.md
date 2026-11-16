# Architecture brief: story\-6360\-s03b\-c\-edge\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented, settled by CLAUDE.md and the existing C# hexagonal modules. Every change keeps an existing owner: the Team metrics update stays in Flow metrics' TeamMetricsService; Work Items' TeamDataService keeps the refresh use case and only publishes; module and placement rules stay in the shared ModuleRules class and test-side rule fixtures. No domain decision moves, so no entity, value object or domain service changes.
```

#### D2

```text
D-2 a confirmed as approved: TeamDataService.UpdateTeamData saves the items through IWorkItemService and publishes TeamDataRefreshed(TeamId); it no longer takes ITeamMetricsService. The new Flow metrics reaction TeamDataRefreshedTeamMetricsUpdateHandler in Modules/FlowMetrics/Application/Policies loads the Team by id through ITeamRepository in the dispatcher's scope (the pattern of BlockedCountSnapshotRecordingHandler) and calls ITeamMetricsService.UpdateTeamMetrics; it returns when the Team is gone and holds no rule of its own. It is registered in Program.cs on its own new line immediately before the ReconstructionMemo TeamDataRefreshed registration, which makes it first among the TeamDataRefreshed handlers; every other Program.cs line stays (D-8). Carriers: event TeamDataRefreshed (kernel, Models/Events); effect = metrics cache invalidation plus the Team's UpdateTime and FeatureWIP save, owned by Flow metrics TeamMetricsService; errors = handler exceptions, caught and logged by DomainEventDispatcher.InvokeHandlerSafely; per-process state = the singleton Cache<string,object> metrics cache and the singleton ReconstructionMemo.
```
Governed slices: S1

#### D3

```text
Cross-layer algebra. Application (Work Items): refresh-pass states itemsSaved, published, done; operation UpdateTeamData; observations the returned SyncOutcome and the published event. Application (Flow metrics): states metrics invalidated, metrics saved; operation HandleAsync(TeamDataRefreshed) then UpdateTeamMetrics; observations the metrics answers and the Team's UpdateTime and FeatureWIP. Adapter/infrastructure: DomainEventDispatcher opens one DI scope per publish and runs handlers sequentially in registration order, catching each failure; observation the handler order the container hands out and the refresh log Success flag. Domain: unchanged, no new state.
```

#### D4

```text
C2 rules reuse ModuleRules with Work Items' namespaces and wording: TypesLiveInTheirLayer (R-W1), DomainReachesOnlyTheKernelAndTheBaseLibrary (R-W2), DomainIsReachedFromOutsideOnlyBy (R-W3), ApplicationIsReachedFromOutsideOnlyBy (R-W5), OutboundEdges per target (R-W6), a Policies rule that every type in Modules.WorkItems.Application.Policies implements IDomainEventHandler<T>, and the leaf rule (connectors reach no Modules.WorkItems type). Baselines are inline lists measured from the IL in S2 and compared both ways; the two WorkItemService query edges (IPortfolioMetricsService, PercentileCalculator) stay named in R-W6 and in R3/R4.
```
Governed slices: S2

#### D5

```text
C3 adds a sibling WorkItemsUnitTestPlacementTest modelled on ForecastingUnitTestPlacementTest (no edit to the frozen Forecasting or Flow metrics placement tests). Layer folders are D-9's three: Domain, Application, Application/Policies; tests of subjects in the Domain/Dependencies and Application/Dependencies sub-namespaces go to their layer folder. Measured: 29 Work Items unit-test files, 8 Domain, 17 Application, 4 Application/Policies. Tests of the selectors that stay in Tracker integration (DependencySourceSelector, DependencyFieldReferences, ParentSourceSelector), RuleEvaluatorPurityTest, WorkItemDomainEventsGoldTest (subject in Models), WorkItemServiceTestBuilder (shared helper) and every API/Integration test stay.
```
Governed slices: S3

#### D6

```text
Removing the inline call deletes TeamDataService's only IL edge to Flow metrics' ITeamMetricsService, and FlowMetricsInboundApplicationArchUnitTest compares its list both ways, so the R4 entry for TeamDataService must leave in S1's edit commit or S1 turns that rule red. S2's oracle re-observes the R4 list (4 entries) and fails if the TeamDataService entry is put back.
```
Governed slices: S1, S2

#### D7

```text
Accepted failure path under D-2 a: when the metrics save in TeamDataRefreshedTeamMetricsUpdateHandler throws, DomainEventDispatcher.InvokeHandlerSafely catches and logs the error and goes on; UpdateTeamData returns its SyncOutcome, so TeamUpdater treats the refresh as succeeded (refresh log Success true, write-back staged as for any successful refresh); the Team's UpdateTime and FeatureWIP keep their values from before the refresh, so the Team stays due for the next refresh, which retries the update; every other TeamDataRefreshed reaction runs in registration order; the metrics cache was dropped before the save, so no reader sees stale metrics. There is no fallback to D-2 e: S1 removes the inline call and S2 keeps R4 at four entries. The success path is unchanged.
```
Governed slices: S1

#### D8

```text
Claim-to-checker: the handler-order, success-path and accepted failure-path claims of one Team refresh pass are a finite state model, checked with Quint (quint run with a fixed seed for mutant counterexamples, quint verify with the Apalache backend at 12 steps, which covers a whole pass). No Agda, because no proof obligation over an unbounded domain exists. No TLC, because no liveness claim exists: a refresh pass is a finite sequence of awaited calls with no waiting, retry or scheduling, so eventual progress is not a claim of this change; refresh scheduling belongs to Epic 6474 slice 12. The dependency and placement claims of S1 to S3 are decided exactly by the compiled IL (IlDependencies over Mono.Cecil) and by test-file locations, each rule with a made-up offender as falsifier; they are not state claims. IlDependencies does not see catch-clause, event or attribute named-argument references; no edge of this change is of those kinds.
```

### Constraints

#### K1

```text
Behaviour-free for the user: routes, DTO shapes, serialized responses, refresh results, metrics answers, forecast results, DI lifetimes and the schema stay the same; only who triggers the Team metrics update changes, with its order pinned.
```

#### K2

```text
Program.cs registration lines stay; S1 adds one registration line; no AddWorkItems extension (S09a). Registrations use short type names.
```

#### K3

```text
Architecture rules read the compiled IL, keep inline baselines compared both ways and are never loosened; made-up offender types live in fixture namespaces; assertions name the offender, not counts.
```

#### K4

```text
A pure move (git mv plus the namespace line) is its own commit, apart from edits; feature A and B oracle and support files take only forced using or path edits, recorded as targets.
```

#### K5

```text
Out of scope: the two WorkItemService query edges to Flow metrics (6474 slice 10, S07a), UpdateTeamMetrics writing through IWorkItemRepository (S07b), an AddWorkItems extension (S09a), ADR-270 and ARCHITECTURE.md edits (finalize).
```

#### K6

```text
No stored or validated input changes in feature C, so no input bound, range or absence of a bound is carried or changed.
```

#### K7

```text
Lighthouse test rules win over nWave defaults: new backend scenarios drive the application-service port (ServicePortHarness); no nWave scenario tags in code; the per-test time rule is the test-writing skill's, not the nine-run rule.
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
ITeamDataService.UpdateTeamData(Team), called by TeamUpdater; in tests through ServicePortHarness
```

#### Driven ports

```text
IDomainEventDispatcher.PublishAsync<TeamDataRefreshed>
```

```text
ITeamRepository.GetById (in the handler's scope)
```

```text
ITeamMetricsService.UpdateTeamMetrics (Flow metrics)
```

```text
Work Items publishes a kernel event; Flow metrics, its downstream, reacts. After S1 Work Items Application references no Flow metrics command; the two WorkItemService query edges remain listed.
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

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs

Extend

```text
Drop the ITeamMetricsService dependency and the UpdateTeamMetrics call; keep save then publish.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs

Create

```text
Flow metrics reaction to TeamDataRefreshed: load the Team by id, call UpdateTeamMetrics.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
One AddScoped<IDomainEventHandler<TeamDataRefreshed>, TeamDataRefreshedTeamMetricsUpdateHandler> line placed immediately before the ReconstructionMemo TeamDataRefreshed registration (Program.cs:1423).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/TeamData\/TeamDataServiceTest\.cs

Extend

```text
Its subject's constructor loses ITeamMetricsService; the mock and its argument go.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
R4 list loses the TeamDataService entry (5 to 4), forced by the both-ways comparison once the edge is gone.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --no-build --filter "FullyQualifiedName~WorkItemsDoesNotCommandFlowMetricsArchUnitTest|FullyQualifiedName~TeamMetricsFollowTheTeamRefreshTest|FullyQualifiedName~DomainEventHandlerOrderPinTest|FullyQualifiedName~FlowMetricsInboundApplicationArchUnitTest|FullyQualifiedName~TeamDataServiceTest|FullyQualifiedName~RefreshesStayTheSameTest" --logger "console;verbosity=normal"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

#### Public oracle

```text
At the service port (ServicePortHarness with the product TeamDataRefreshed registrations taken from Program.cs, FakeLighthouseClock), refresh a CSV-backed Team through TeamUpdater.TriggerUpdate(teamId) built on the harness scope factory, which calls ITeamDataService.UpdateTeamData and writes the refresh log row; read the Team metrics answers through ITeamMetricsService, its UpdateTime and FeatureWIP, the refresh log row, and the handler order the container hands out for TeamDataRefreshed. Read TeamDataService IL edges. Then, from a written-out starting UpdateTime and FeatureWIP, refresh again with a save that throws injected into the Team metrics update, recording the reactions that run and the logged errors.
```

```text
Success path: metrics answers, UpdateTime and FeatureWIP equal the values written out in the test (today answers); the handler order is TeamDataRefreshedTeamMetricsUpdateHandler, ReconstructionMemo, TeamDataRefreshedForecastTriggerHandler, BlockedCountSnapshotRecordingHandler, PercentilesOverTimeRecordingHandler, ProcessBehaviorRecordingHandler, DemoBlockedHistoryBackfillHandler, DemoPercentilesBackfillHandler; TeamDataService has no IL edge to ITeamMetricsService or any Flow metrics Application type. Failing save: the refresh returns normally and its refresh log Success flag is true; one error is logged naming TeamDataRefreshedTeamMetricsUpdateHandler; UpdateTime and FeatureWIP equal their written-out values from before the refresh; the seven other reactions ran, in the order above; a metrics read after the refresh is computed from the refreshed items, not served from the cache of before.
```

```text
A dispatcher or handler that rethrows the save failure fails the refresh (log Success false) and skips the later reactions; a dispatcher that stops after the failing handler, or a refresh that skips the reactions, leaves reactions missing; registering the metrics handler after BlockedCountSnapshotRecordingHandler fails the order pin and lets a snapshot recorder read stale metrics; a metrics answer read from the cache of before the failed save differs from the written-out refreshed value; putting the inline call back fails the IL rule naming TeamDataService; leaving the registration out leaves UpdateTime and FeatureWIP at their old values on the success path.
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

TeamDataService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:8

Extend

```text
Owns the Team refresh use case; it keeps save-then-publish and only loses the command to Flow metrics.
```

BlockedCountSnapshotRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:47

Reuse

```text
Pattern for a Flow metrics TeamDataRefreshed handler: load the Team by id through ITeamRepository in the dispatcher scope, return when absent.
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1423

Extend

```text
Composition root holds the handler order; one line added before the ReconstructionMemo registration.
```

DomainEventHandlerOrderPinTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:26

Extend

```text
Existing order pin for TeamDataRefreshed; the new handler is pinned first there, proven red on a swapped registration.
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:111

Reuse

```text
Refresh at the port with the product's registrations, as feature A's safety net does.
```

UpdateTeamMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:868

Reuse

```text
The Team metrics update stays owned by Flow metrics TeamMetricsService; the new handler only calls it.
```

TeamDataRefreshedTeamMetricsUpdateHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:1

CreateNew

```text
No existing TeamDataRefreshed handler updates Team metrics; the six existing reactions each own another fact. Modelled on BlockedCountSnapshotRecordingHandler (load the Team by id in the dispatcher scope).
```

TeamDataServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/TeamData\/TeamDataServiceTest\.cs\:11

Extend

```text
Existing unit test of TeamDataService; only the ITeamMetricsService mock and constructor argument go.
```

FlowMetricsInboundApplicationArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:21

Extend

```text
Existing R4 rule owns the inbound list; its TeamDataService entry leaves because the edge is gone, the rule itself is not loosened.
```

##### Impact

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:21

Changed

```text
The inline UpdateTeamMetrics call is removed; code-fact (textsearch, noisy) finds it the only caller of UpdateTeamMetrics.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:1

Changed

```text
New reaction.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1423

Changed

```text
One registration added first among TeamDataRefreshed handlers; DI lifetime scoped like its siblings.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:56

UnchangedCompatible

```text
Calls UpdateTeamData as before, code unchanged. Its in-memory Team keeps the pre-update UpdateTime and FeatureWIP because the handler saves its own copy in the dispatcher scope; the Team concurrency token changes only on an explicit edit (LighthouseAppContext.ApplyConcurrencyTokenForEdit), so no later save conflicts. Whether anything after UpdateTeamData reads those two values from the tracked Team is UNKNOWN; the S1 port scenario measures it. Under a failed metrics save UpdateTeamData now returns normally, so TeamUpdater logs the refresh as succeeded and stages write-back, as the accepted failure path says.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:9

UnchangedCompatible

```text
Sequential, registration order, one scope per publish, catches and logs each handler failure; unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:58

UnchangedCompatible

```text
Reads team metrics through ITeamMetricsService (cached); the metrics handler runs before it, so it reads after the cache is dropped, as today.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:45

UnchangedCompatible

```text
Reads team metrics; runs after the metrics handler.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:42

UnchangedCompatible

```text
Reads team metrics; runs after the metrics handler.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:70

UnchangedCompatible

```text
Reads team metrics; runs after the metrics handler.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:64

UnchangedCompatible

```text
Runs after the metrics handler; no metrics read seen in its handle method.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs\:13

UnchangedCompatible

```text
Only enqueues forecast updates for the Team's Portfolios; reads no metrics in the handler; the queued forecast runs later, after the pass.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:74

UnchangedCompatible

```text
Forgets the Team's notes on TeamDataRefreshed; today it runs after the inline update, and the metrics handler is registered before it, so the relative order holds.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/TeamData\/TeamDataServiceTest\.cs\:13

Changed

```text
Constructor argument removed.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:56

Changed

```text
R4 entry removed.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:111

UnchangedCompatible

```text
Feature A oracle compares items, transitions, blocks and published events; TeamDataRefreshed still published once per refresh. If its registration helper builds TeamDataService with ITeamMetricsService the forced edit is recorded by the crafter as a target.
```

##### Agreement

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:23

Producer

UnchangedCompatible

```text
Still published once per Team refresh after the items are saved.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:1

Consumer

Migrated

```text
New consumer, registered first.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:74

Consumer

UnchangedCompatible

```text
Second in order now; same relative order to the metrics update.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs\:13

Consumer

UnchangedCompatible

```text
Order unchanged relative to the others.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:47

Consumer

UnchangedCompatible

```text
Reads after the metrics update, as today.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:45

Consumer

UnchangedCompatible

```text
Reads after the metrics update, as today.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:42

Consumer

UnchangedCompatible

```text
Reads after the metrics update, as today.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:70

Consumer

UnchangedCompatible

```text
Reads after the metrics update, as today.
```

TeamDataRefreshed\(TeamId\) in\-process domain event

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:64

Consumer

UnchangedCompatible

```text
Last, as today.
```

##### Not applicable

prefactoring

```text
The change is one removed call, one new class and one registration; no preserving move makes it easier, and the moves of feature B already placed TeamDataService and the Flow metrics Policies folder.
```

contract_tests

```text
Producer and consumers of TeamDataRefreshed are in one deployable, change in the same commit and exchange no persisted or wire data; the port scenario and the order pin cover them.
```

##### Boundaries

```text
TeamUpdater.TriggerUpdate(teamId) -> ITeamDataService.UpdateTeamData(Team), in tests built on the ServicePortHarness scope factory
```

###### Driven ports

```text
IDomainEventDispatcher.PublishAsync<TeamDataRefreshed>
```

```text
ITeamRepository.GetById (in the handler scope)
```

```text
ITeamMetricsService.UpdateTeamMetrics (Flow metrics)
```

```text
IRefreshLogService.LogRefreshAsync
```

```text
Work Items publishes the kernel event TeamDataRefreshed; Flow metrics, downstream, reacts through its Policies handler; Work Items Application holds no Flow metrics command.
```

###### Failures

####### B1

```text
The Team was deleted between the save and the handler
```

Refusal

```text
Handler returns without updating; no exception.
```

####### B2

```text
The metrics save in TeamDataRefreshedTeamMetricsUpdateHandler throws
```

Retry

```text
The refresh returns its SyncOutcome and its refresh log Success flag is true; the handler error is logged by DomainEventDispatcher; UpdateTime and FeatureWIP equal their values before the refresh, so the Team is due again and the next refresh retries the update; every other reaction ran in registration order.
```

##### Observations

```text
Team metrics answers, UpdateTime and FeatureWIP after a refresh at the port
```

ITeamMetricsService answers and Team fields

```text
Which component triggered the update; a stale cached answer equal by coincidence (the fixture changes items so the answer moves).
```

```text
Order in which the container hands out IDomainEventHandler<TeamDataRefreshed>
```

List of handler type names

```text
A dispatcher that stops honouring registration order; the dispatcher itself is unchanged.
```

```text
IL edges from Modules.WorkItems types to Flow metrics Application
```

Set of offender type names

```text
References through catch clauses, events or attribute named arguments.
```

```text
Under a failing metrics save: refresh outcome, refresh log Success flag, the logged handler error, the Team UpdateTime and FeatureWIP against their values before the refresh, the reactions run and their order, and a metrics read after the refresh
```

SyncOutcome or exception\, RefreshLog\.Success\, log entries\, Team fields\, recorded handler calls\, ITeamMetricsService answer

```text
Failures other than the injected save failure; a failure inside another reaction.
```

##### Equality

metrics answers

```text
Equal when every metrics answer of the refreshed Team, its UpdateTime and FeatureWIP equal the values written out in the test for the same CSV data and FakeLighthouseClock instant.
```

##### Constructors

None.

##### Laws

###### L2

```text
No TeamDataRefreshed reaction reads the Team's metrics cache before the refresh has dropped it.
```

Invariant

Application

TeamDataRefreshedTeamMetricsUpdateHandler

###### Treatment

Finite

###### Cases

```text
today's registrations with the inline call
```

```text
C1 registrations with the handler first
```

```text
mutant: handler after BlockedCountSnapshotRecordingHandler
```

```text
The handler list is finite and fixed at start-up.
```

###### Evidence

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

###### L3

```text
A Team refresh gives the same metrics answers, UpdateTime and FeatureWIP as before C1.
```

Refinement

Application

TeamDataRefreshedTeamMetricsUpdateHandler

###### Treatment

Finite

###### Cases

```text
CSV Team with in-progress Features and automatic Feature WIP
```

```text
CSV Team with done items in the throughput window
```

```text
Both sides run the same UpdateTeamMetrics; a generated comparison would compare the code with itself, so written-out values are the oracle.
```

###### Evidence

None.

###### L4

```text
No Modules.WorkItems type has an IL edge to ITeamMetricsService.
```

Invariant

Adapter

WorkItemsDoesNotCommandFlowMetricsArchUnitTest

###### Treatment

Finite

###### Cases

```text
product assembly
```

```text
made-up Work Items fixture type calling UpdateTeamMetrics
```

```text
Decided exactly over the compiled IL.
```

###### Evidence

None.

###### L5

```text
A failed metrics save in the handler leaves the refresh succeeded (SyncOutcome returned, refresh log Success true, error logged), UpdateTime and FeatureWIP at their values before the refresh, and every other TeamDataRefreshed reaction run in registration order.
```

Invariant

Application

DomainEventDispatcher

###### Treatment

Finite

###### Cases

```text
CSV Team refresh with a failing metrics save
```

```text
mutant: handler failure rethrown past the dispatcher
```

```text
mutant: dispatcher stops after the failing handler
```

```text
One injected failure point over a finite, fixed handler list.
```

###### Evidence

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/verify\_c1\_MetricsFailureLeavesRefreshOkAndSaveUndone\.txt

##### Refuted

```text
Under C1, a failed metrics save fails the refresh and no TeamDataRefreshed reaction runs, as today.
```

```text
Boundary distinction: DomainEventDispatcher.InvokeHandlerSafely catches and logs every handler exception and continues, so a handler cannot fail the publisher; the inline call could.
```

```text
Quint module c1, invariant MetricsFailureFailsTheRefresh: metricsFails = true, the pass ends with outcome ok and all eight handlers ran (quint run seed 7, violation).
```

```text
Under C1, a refresh that ends ok has saved the Team's UpdateTime and FeatureWIP.
```

```text
Same boundary: the swallowed handler failure leaves the save undone while the refresh succeeds.
```

```text
Quint module c1, invariant OkRefreshHasSavedMetrics: metricsFails = true, outcome ok, metricsSaved false.
```

##### Claims

###### C1

```text
Work Items Application holds no command to Flow metrics' Team metrics update.
```

```text
Work Items would depend on its downstream Flow metrics, a cycle ADR-270 sends through events.
```

WorkItemsDoesNotCommandFlowMetricsArchUnitTest

```text
Compiled Lighthouse.Backend assembly
```

TeamDataService\(ILogger\, IWorkItemService\, IDomainEventDispatcher\)

TeamUpdater

```text
IL offender list names no Modules.WorkItems type
```

```text
Reflection or service-locator resolution of ITeamMetricsService inside Work Items is invisible to the IL type-reference check; none exists today.
```

##### States

Admitted

```text
Success path: items saved, TeamDataRefreshed published, metrics handler first, then the seven existing reactions in their order.
```

None.

Excluded

```text
A snapshot recorder reading the Team's metrics before the cache is dropped (order pin).
```

None.

Unknown

```text
Whether anything in TeamUpdater's scope after UpdateTeamData reads the Team's UpdateTime or FeatureWIP from the tracked entity, now stale. Falsifier: the S1 port scenario reads them after the refresh.
```

S1\-oracle

Unknown

```text
Whether every Team change of UpdateWorkItemsForTeam is saved before the publish, so the handler's new scope reads them (after-commit). Falsifier: the port scenario's FeatureWIP for a Team whose in-progress Features changed in this refresh.
```

S1\-oracle

Admitted

```text
Failure path (accepted): the metrics save throws inside the handler; the dispatcher logs the error and runs the remaining reactions in order; UpdateTeamData returns its SyncOutcome; the refresh log Success flag is true; the Team keeps its old UpdateTime and FeatureWIP; the metrics cache is dropped.
```

None.

Excluded

```text
A failed metrics save that fails the refresh, skips later TeamDataRefreshed reactions, or leaves a reader with metrics cached from before the refresh.
```

None.

##### Models

Quint

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

###### Properties

ReadersNeverSeeStaleMetrics

Invariant

```text
No snapshot recorder runs before the Team's metrics cache is dropped.
```

DomainEventDispatcher\.PublishAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:9

SuccessfulSaveIsKept

Invariant

```text
A refresh whose metrics save succeeds ends ok with UpdateTime and FeatureWIP saved.
```

TeamMetricsService\.UpdateTeamMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:868

MetricsFailureLeavesRefreshOkAndSaveUndone

Invariant

```text
A failed metrics save leaves the refresh ok, the cache dropped, UpdateTime and FeatureWIP unsaved, and every TeamDataRefreshed reaction run.
```

DomainEventDispatcher\.InvokeHandlerSafely

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:20

###### Atoms

Action

inlineMetrics

TeamDataService\.UpdateTeamData

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:21

None.

Action

dispatchNext

DomainEventDispatcher\.InvokeHandlerSafely

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:20

None.

State

invalidated

BaseMetricsService\.InvalidateMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:954

None.

State

metricsSaved

TeamMetricsService\.UpdateTeamMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:875

None.

Constant

ORDER

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1423

None.

Constant

FAILURE\_POLICY

DomainEventDispatcher\.InvokeHandlerSafely

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/DomainEventDispatcher\.cs\:20

```text
The catch around one awaited HandleAsync; swallow is the product, rethrow and stop are mutants
```

###### Assumptions

```text
One Team refresh pass; handler list fixed at start-up; a metrics failure happens at the save, after the cache is dropped
```

###### Mutants

######## N1

```text
c1Swapped: metrics handler registered after BlockedCountSnapshotRecordingHandler
```

###### Kills

ReadersNeverSeeStaleMetrics

######## N2

```text
c1Dropped: inline call removed and no handler registered
```

###### Kills

ReadersNeverSeeStaleMetrics

SuccessfulSaveIsKept

MetricsFailureLeavesRefreshOkAndSaveUndone

######## N3

```text
c1Rethrow: the dispatcher lets the metrics handler failure leave the publish
```

###### Kills

MetricsFailureLeavesRefreshOkAndSaveUndone

######## N4

```text
c1StopsOnFailure: the dispatcher skips the remaining reactions after a failing handler
```

###### Kills

MetricsFailureLeavesRefreshOkAndSaveUndone

##### Closure

```text
Metrics handler first among TeamDataRefreshed reactions
```

Safety

Pass

DesignedNotBuilt

```text
quint verify c1 ReadersNeverSeeStaleMetrics at 12 steps exit 0; mutant c1Swapped violated under quint run
```

###### Evidence

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

```text
Refresh that updates the metrics
```

Inhabitation

Pass

DesignedNotBuilt

```text
c1 reaches outcome ok with metricsSaved when metricsFails is false (quint run traces); constructor path TeamDataRefreshed(TeamId) from TeamDataService to the handler
```

###### Evidence

None.

```text
Metrics update reachable from the public refresh
```

PublicReachability

Pass

DesignedNotBuilt

```text
TeamUpdater -> ITeamDataService.UpdateTeamData -> IDomainEventDispatcher.PublishAsync -> TeamDataRefreshedTeamMetricsUpdateHandler -> ITeamMetricsService answers read at the port
```

###### Evidence

None.

```text
Accepted failure path: refresh ok, save undone, all reactions run, no stale read
```

Safety

Pass

DesignedNotBuilt

```text
quint verify c1 MetricsFailureLeavesRefreshOkAndSaveUndone and ReadersNeverSeeStaleMetrics at 12 steps exit 0; mutants c1Rethrow, c1StopsOnFailure and c1Dropped violated under quint run seed 7
```

###### Evidence

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/verify\_c1\_MetricsFailureLeavesRefreshOkAndSaveUndone\.txt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/run\_c1Rethrow\_MetricsFailureLeavesRefreshOkAndSaveUndone\.txt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/run\_c1StopsOnFailure\_MetricsFailureLeavesRefreshOkAndSaveUndone\.txt

```text
Success path: refresh ok with UpdateTime and FeatureWIP saved
```

Safety

Pass

DesignedNotBuilt

```text
quint verify c1 SuccessfulSaveIsKept at 12 steps exit 0, also holds for today; mutant c1Dropped violated under quint run seed 7
```

###### Evidence

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/team\_refresh\_reactions\.qnt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/verify\_c1\_SuccessfulSaveIsKept\.txt

docs\/feature\/story\-6360\-s03b\-c\-edge\-and\-rules\/models\/receipts\/run\_c1Dropped\_SuccessfulSaveIsKept\.txt

##### Risks

###### Y1

Stressor

```text
Negates: the metrics handler is registered first
```

```text
Order of the seven existing reactions
```

```text
None
```

```text
A snapshot recorder reading metrics before invalidation
```

```text
None; the order pin fails on a swapped registration
```

DomainEventHandlerOrderPinTest

###### Y2

SinglePointOfFailure

```text
DomainEventDispatcher is now the only path that updates a Team's metrics after a refresh
```

```text
Metrics updated on every successful refresh
```

```text
None
```

```text
A refresh with no metrics update and no error
```

```text
A missing registration is caught by the order pin and the port scenario
```

S1\-oracle

###### Y3

Bypass

```text
Another publisher or caller of UpdateTeamMetrics, or service-locator resolution, outside the IL rule
```

```text
One metrics update path
```

```text
Flow metrics' own callers
```

```text
Work Items commanding Flow metrics again
```

```text
Reflection and service-locator use are not seen by the IL rule; none exists today
```

WorkItemsDoesNotCommandFlowMetricsArchUnitTest

###### Y4

Stressor

```text
Second stressor walk (not used to derive the design): the handler runs in a new DI scope with its own DbContext
```

```text
Metrics answers and saved UpdateTime and FeatureWIP
```

```text
TeamUpdater's in-memory Team keeps old UpdateTime and FeatureWIP
```

```text
A concurrency failure on a later save
```

```text
Team's concurrency token changes only on explicit edits, so no conflict; stale in-memory read is the UNKNOWN the port scenario measures
```

S1\-oracle

###### Y5

Stressor

```text
Second stressor walk: cancellation during the pass
```

```text
Cancellation still ends the refresh before publish
```

```text
Handler runs with the dispatcher's default token, as UpdateTeamMetrics took none
```

```text
A metrics update for a cancelled refresh
```

```text
None new: publish happens only after UpdateWorkItemsForTeam returns
```

S1\-oracle

###### Y6

Stressor

```text
Negates: UpdateTeamMetrics succeeds
```

```text
Items, transitions and events of the refresh; the refresh outcome and its log Success flag; every other TeamDataRefreshed reaction, in order; no stale metrics for readers
```

```text
The handler error in the log; UpdateTime and FeatureWIP not saved until the next refresh
```

```text
A refresh failed or reactions skipped because of the metrics handler; a reader served metrics cached from before the refresh
```

```text
The Team shows its previous UpdateTime until a later refresh succeeds; accepted by the maintainer
```

TeamMetricsFollowTheTeamRefreshTest

##### Architectural tests

###### T3

```text
Work Items Application (TeamDataService and every Modules.WorkItems type) has no IL edge to ITeamMetricsService or UpdateTeamMetrics; a made-up Work Items fixture type calling UpdateTeamMetrics is named.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~WorkItemsDoesNotCommandFlowMetricsArchUnitTest
```

###### T4

```text
TeamDataRefreshedTeamMetricsUpdateHandler is the first TeamDataRefreshed handler the container hands out; red on a swapped registration.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~DomainEventHandlerOrderPinTest
```

##### Contract tests

None.

### S2

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --no-build --filter "FullyQualifiedName~WorkItemsModuleBoundariesArchUnitTest|FullyQualifiedName~FlowMetricsInboundApplicationArchUnitTest|FullyQualifiedName~FlowMetricsInboundDomainAndAdaptersArchUnitTest|FullyQualifiedName~ModuleRulesCatchANewOffenderArchUnitTest" --logger "console;verbosity=normal"
```

#### Supports

None.

#### Public oracle

```text
Run the Work Items module rules over the compiled product assembly and over made-up offender types in the test assembly's fixture namespaces: a Work Items Domain type reaching a Services type, an outside type reaching Domain or Application off the list, an Application type reaching EF Core, Data, API or an unlisted Flow metrics type, a non-handler class in Modules.WorkItems.Application.Policies, a connector type reaching Modules.WorkItems.
```

```text
On the product: every rule passes with its inline baseline (R-W1 the D-1 lists; R-W2 empty; R-W3 and R-W5 the lists measured from the IL in this slice; R-W6 Flow metrics IPortfolioMetricsService and PercentileCalculator plus one entry each for IRbacAdministrationService, ILicenseService, IFeatureOrdering, UpdateCancellationContext; leaf rule empty), each compared both ways, and Flow metrics R4 has 4 entries with no TeamDataService. On each made-up offender: the rule's failure names that offender type.
```

```text
Deleting any baseline entry, or adding an entry with no IL edge, fails the both-ways comparison; a rule that ignores the offender namespace passes a made-up offender and fails its row; putting TeamDataService back into R4 fails R4.
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

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

reuse

```text
S2 creates only its oracle file; no production or existing test target. The rules reuse ModuleRules (Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ModuleRules.cs:15) unchanged, recorded as a shared decision.
```

impact

```text
No code target: the new rule file only reads the compiled IL; no production, export or build file changes.
```

prefactoring

```text
No code target to prepare; the module folders were placed by feature B.
```

agreement

```text
No shared contract changes: the rules read IL and add no producer or consumer of a schema, wire format or shared configuration.
```

contract_tests

```text
No parties that change apart; the rules are tests over one assembly.
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
Every Work Items module rule holds on the product with its inline baseline and names each made-up offender.
```

Invariant

Adapter

WorkItemsModuleBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
product assembly
```

```text
one made-up offender per rule in a fixture namespace
```

```text
Decided exactly over the finite compiled IL.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

Unknown

```text
R-W3 and R-W5 baselines (outside callers of Domain and Application): measured from the IL in S2; expected per the Ratchet design. Falsifier: the both-ways comparison.
```

S2\-oracle

##### Models

None.

##### Closure

```text
Work Items module rules
```

Safety

Pass

DesignedNotBuilt

```text
IL rules with made-up offenders; baselines measured in S2
```

###### Evidence

None.

```text
Work Items module rules
```

PublicReachability

Pass

DesignedNotBuilt

```text
Rules run in the default backend suite through dotnet test
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
Work Items module rules R-W1, R-W2, R-W3, R-W5, R-W6, the Policies-only-handlers rule and the leaf rule hold over every current and future type of Modules.WorkItems and of the connectors, with inline baselines compared both ways, each catching a made-up offender by name.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~WorkItemsModuleBoundariesArchUnitTest
```

###### T2

```text
Flow metrics R4 lists 4 Application callers outside the module, without TeamDataService; R3 keeps its 6 entries.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter "FullyQualifiedName~FlowMetricsInboundApplicationArchUnitTest|FullyQualifiedName~FlowMetricsInboundDomainAndAdaptersArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintParentSourcesTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintParentSourcesTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintPinnedDigestTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintPinnedDigestTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/JiraReadingVersionFingerprintTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/JiraReadingVersionFingerprintTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/SyncModeResolverTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/SyncModeResolverTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/DependencyCycleDetectorTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyCycleDetectorTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/DependencyHonourPolicyTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyHonourPolicyTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/BlockedItemServiceTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/BlockedItemServiceTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceCsvStateEnteredSeamIntegrationTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceFeatureTransitionIntegrationTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionFallbackIntegrationTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionSyncIntegrationTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/DemoDataStateEnteredSeamIntegrationTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyReconcilerTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyReconcilerTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyRefreshReporterTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/FeatureDependencyRenderingTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyVerdictIdentityTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureFieldProviderTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/FeatureFieldProviderTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RuleEvaluatorTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/RuleEvaluatorTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemFieldProviderTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/WorkItemFieldProviderTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamData/TeamDataServiceTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ParentPlacementReportTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Parents/ParentPlacementReportTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RefreshLogServiceTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/RefreshLogServiceTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureTests\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemBlockedTransitionCaptureTests.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionHandlerTests\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/FeatureBlockedTransitionHandlerTests.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/TeamDeletedRefreshLogCleanupHandlerTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs

Create

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/ConnectionFetchShapeChangedUpdateTriggerHandlerTest.cs (pure move plus namespace line).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
The five moved Work Items host users (DemoDataStateEnteredSeamIntegrationTest and the four WorkItemService*IntegrationTest classes) get their new full names, or lose their line where a moved test stops starting a host.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --no-build --filter "FullyQualifiedName~WorkItemsUnitTestPlacementTest|FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~Tests.Modules.WorkItems|FullyQualifiedName~ForecastingUnitTestPlacementTest|FullyQualifiedName~FlowMetricsUnitTestPlacementTest" --logger "console;verbosity=normal"
```

#### Supports

None.

#### Public oracle

```text
Read the test project's source files on disk and the compiled test assembly with its symbols; run the placement rule on the real tree and on made-up trees: a Work Items test left in its old folder, a copy left behind, a file in the wrong layer, a newcomer in a folder the Work Items tests left, a class with a stale namespace, an API/Integration test.
```

```text
Each of the 29 listed Work Items unit-test files sits under Lighthouse.Backend.Tests/Modules/WorkItems/<layer> (8 Domain, 17 Application, 4 Application/Policies) with the namespace of that folder and its class name kept; each made-up misplacement is named; the compliant twin, the shared helpers and the API/Integration test are not. Every moved test passes unchanged in behaviour.
```

```text
Leaving one listed file in Services/Implementation, placing one in the wrong layer, or keeping a stale namespace fails the row naming it; a rule that accepts any folder passes a made-up misplacement and fails its row.
```

##### Promises

P3

P4

#### Prefactoring

```text
Pure git mv of the 29 Work Items unit-test files into Lighthouse.Backend.Tests/Modules/WorkItems/{Domain, Application, Application/Policies} with only the namespace line changed, committed apart; makes the placement rule's baseline the real tree.
```

```text
Each moved test's outcome and count, same ids
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkItems\/WorkItemServiceTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter "FullyQualifiedName~WorkItemServiceTest|FullyQualifiedName~BlockedItemServiceTest|FullyQualifiedName~FetchFingerprint|FullyQualifiedName~SyncModeResolverTest|FullyQualifiedName~Dependency|FullyQualifiedName~FieldProviderTest|FullyQualifiedName~RuleEvaluatorTest|FullyQualifiedName~TeamDataServiceTest|FullyQualifiedName~ParentPlacementReportTest|FullyQualifiedName~RefreshLogServiceTest|FullyQualifiedName~BlockedTransition|FullyQualifiedName~TeamDeletedRefreshLogCleanupHandlerTest|FullyQualifiedName~ConnectionFetchShapeChangedUpdateTriggerHandlerTest|FullyQualifiedName~StateEnteredSeam|FullyQualifiedName~JiraReadingVersionFingerprintTest"
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkItems\/WorkItemServiceTest\.cs\:1

Migrated

```text
29 unit-test files move by layer; ids and outcomes kept.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:321

Changed

```text
Lines 321 to 325 renamed or removed.
```

build\-script

Scripts\/test\-selection\/path\-classifier\.sh\:13

UnchangedCompatible

```text
Its Jira regex names test paths only under Services/Implementation/WorkTrackingConnectors/Jira and production paths; no moved test path. test-path-classifier.sh fixtures name production files and ParentSourceSelectorTest, which stays.
```

test\-helper

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

UnchangedCompatible

```text
Shared helper stays; its namespace is unchanged, moved tests add a using if needed.
```

mutation\-config

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json\:1

UnchangedCompatible

```text
Historical per-feature configs name old test classes by name only; the D-11 mutation run at finalize writes its own config naming Tests.Modules.WorkItems.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintParentSourcesTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintParentSourcesTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintPinnedDigestTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintPinnedDigestTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/JiraReadingVersionFingerprintTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/JiraReadingVersionFingerprintTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/SyncModeResolverTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/SyncModeResolverTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/DependencyCycleDetectorTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyCycleDetectorTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Domain\/DependencyHonourPolicyTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyHonourPolicyTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/BlockedItemServiceTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/BlockedItemServiceTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceCsvStateEnteredSeamIntegrationTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceCsvStateEnteredSeamIntegrationTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceFeatureTransitionIntegrationTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceFeatureTransitionIntegrationTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionFallbackIntegrationTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionFallbackIntegrationTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTransitionSyncIntegrationTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionSyncIntegrationTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DemoDataStateEnteredSeamIntegrationTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/DemoDataStateEnteredSeamIntegrationTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyReconcilerTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyReconcilerTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyRefreshReporterTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyRefreshReporterTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureDependencyRenderingTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/FeatureDependencyRenderingTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Dependencies/DependencyVerdictIdentityTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/FeatureFieldProviderTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/FeatureFieldProviderTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RuleEvaluatorTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/RuleEvaluatorTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemFieldProviderTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItemRules/WorkItemFieldProviderTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/TeamData/TeamDataServiceTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ParentPlacementReportTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Parents/ParentPlacementReportTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/RefreshLogServiceTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/RefreshLogServiceTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureTests\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemBlockedTransitionCaptureTests.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionHandlerTests\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/FeatureBlockedTransitionHandlerTests.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/TeamDeletedRefreshLogCleanupHandlerTest.cs; test ids and outcomes kept; using lines follow the move.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs\:1

Migrated

```text
Moved from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/ConnectionFetchShapeChangedUpdateTriggerHandlerTest.cs; test ids and outcomes kept; using lines follow the move.
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract between parties that change apart: the test list files and the guard live in the same test assembly and change in the same commit.
```

contract_tests

```text
Test placement only; no producer and consumer that change apart.
```

reuse

```text
No owner question arises in S3. The 29 Create targets are existing Work Items unit-test classes moved unchanged (git mv plus the namespace line), so the existing classes are reused as they are. The only edited target, TestHostUseGuardTest.HostUsersStillToMove.cs, extends the existing guard list (five moved host users renamed or removed). The oracle WorkItemsUnitTestPlacementTest is new because no placement test covers Work Items and parameterising ForecastingUnitTestPlacementTest would edit a frozen oracle; it follows that test's pattern (disk files plus compiled classes, made-up trees).
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

###### L6

```text
Each listed Work Items unit-test file is placed in its subject's layer folder with that folder's namespace.
```

Invariant

Adapter

WorkItemsUnitTestPlacementTest

###### Treatment

Finite

###### Cases

```text
29 listed files on the real tree
```

```text
made-up trees: left behind, copy left, wrong layer, stale namespace, newcomer in a left folder, integration test
```

```text
Finite file list on disk and in the compiled assembly.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

Unknown

```text
JiraReadingVersionFingerprintTest's subject: its digests pin FetchFingerprint per connector reading version, so Domain; the oracle owner confirms.
```

S3\-oracle

##### Models

None.

##### Closure

```text
Work Items unit-test placement
```

Safety

Pass

DesignedNotBuilt

```text
Placement rule over disk and compiled classes with made-up trees
```

###### Evidence

None.

```text
Work Items unit-test placement
```

PublicReachability

Pass

DesignedNotBuilt

```text
Rule runs in the default backend suite through dotnet test
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T5

```text
Every Work Items unit-test file sits under Tests/Modules/WorkItems in its subject's layer folder with that folder's namespace; any Work Items unit test placed elsewhere, now or later, is named; API/Integration tests and shared helpers are not.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~WorkItemsUnitTestPlacementTest
```

##### Contract tests

None.

## ADRs

None.
