# Architecture brief: story\-6374\-s05f\-a\-team\-reads

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemRepository\.cs

Extend

```text
Prefactoring: W1-W5 declared, no caller yet.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemRepository\.cs

Extend

```text
Prefactoring: W1-W5 over a private helper with Include(Team), tracked, materialised once; W4 server-side Count().
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemStateTransitionRepository\.cs

Extend

```text
Prefactoring: T1 declared.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemStateTransitionRepository\.cs

Extend

```text
Prefactoring: T1 as Where(ids.Contains(WorkItemId)).ToList().
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs

Extend

```text
Prefactoring: F1 declared.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs

Extend

```text
Prefactoring: F1 as featureOrdering.Order(GetFeatures().Where(today's predicate).AsEnumerable()).ToList(); never the ListRead.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedWorkItemWrites\.cs

Extend

```text
Compile-forced: WriteRecordingWorkItemRepository implements IWorkItemRepository, so W1-W5 are added as plain delegations to inner; no recording or assertion change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Value slice: the 14 GetAllByPredicate calls switch to the seven named reads; constructor unchanged; the size chart's in-memory filter stays in the service.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Arrange stubs retarget through one helper; Asserts byte-identical; the Times.Never Verify on Features retargets to F1.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Arrange stubs retarget only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Arrange stubs retarget only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Arrange stubs retarget only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Arrange stubs retarget only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Arrange stubs retarget only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Arrange stubs retarget, keeping today's per-read filtering of the in-memory list; Asserts unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

#### Verification

```text
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter 'FullyQualifiedName~RepositoryLeakRatchetArchUnitTest|FullyQualifiedName~ParityOnBothProviders|FullyQualifiedName~RealityCheckQueryCountTest'
dotnet build Lighthouse.Backend/Lighthouse.sln
dotnet test Lighthouse.Backend/Lighthouse.sln --filter 'TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration'
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Release --filter FullyQualifiedName~Architecture
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs

#### Public oracle

```text
Run RepositoryLeakRatchetArchUnitTest.GetAllByPredicate_IsCalledOnlyFromTheFilesOnTheList with Modules/FlowMetrics/Application/TeamMetricsService.cs removed from FilesThatCallGetAllByPredicate and Has.Length set to 14; with it, the parity support on SQLite and Postgres and RealityCheckQueryCountTest.
```

```text
RED on the base: the guard fails naming TeamMetricsService.cs as a caller not on the list. GREEN on the candidate: the guard passes with 14 entries, every named Team read returns the same id sequence, graph and tracking as today's predicate on both providers, W4 equals the length of W3, the statement counts stay 20 and 24, and every existing metrics Assert passes unchanged.
```

```text
Put one GetAllByPredicate call back in TeamMetricsService: the guard goes red and names the file. Drop Doing from W2 or Include(Team) from a work-item read: the parity support goes red on both providers. Make W1 read twice: RealityCheckQueryCountTest goes red.
```

##### Promises

P1

P2

P3

#### Prefactoring

```text
Add the seven named reads W1-W5, T1 and F1 to the three owner repository interfaces and classes, each keeping today's predicate, include, tracking and ordering, with no caller yet; add the five plain delegations to WriteRecordingWorkItemRepository; land the parity support TeamMetricsReadsParityOnBothProvidersTests, proven by a deliberate break. This makes the value slice's switch of TeamMetricsService a call-site-only edit.
```

```text
Every existing test stays green unchanged before and after the move; no production caller changes; GetAllByPredicate and the S05a list (15) unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

##### Verification

```text
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.sln --filter 'TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration'
```

#### Records

##### Decisions

```text
Read model: TeamMetricsService reads through seven named reads on the owner repositories, not a query object per capability and not Flow-metrics ports in the module. W1 DoneItemsOfTeam(teamId), W2 DoingOrDoneItemsOfTeam(teamId), W3 ToDoOrDoingItemsOfTeam(teamId), W4 CountToDoOrDoingItemsOfTeam(teamId), W5 ItemsOfTeam(teamId) on IWorkItemRepository; T1 TransitionsOfWorkItems(IReadOnlyCollection<int> ids) on IWorkItemStateTransitionRepository; F1 FeaturesWithRealWorkForTeam(teamId, StateCategories? onlyIn) on IFeatureRepository. Reading goes through the owner's contract, so the owner keeps its one definition of how a Feature is read whole; no new dependency edge, no Program.cs edit, TeamMetricsService keeps its constructor. Names are working names; the crafter may improve them, but no new read may be called FeaturesOfTeam or FeaturesOfPortfolio.
```

```text
Shape of a read: one named read per state-category set, each keeping today's predicate literally (Done; Doing or Done; ToDo or Doing; team only), with one private helper per repository holding the shared include and ordering code. A category set passed as a parameter or a StateCategorySet value object is rejected: it turns OR into IN over a parameter collection, and the row order of work-item reads is unspecified yet serialised per day in RunChartDataDto.WorkItemsPerUnitOfTime, so a new plan may reorder visible answers. W2 serves call sites written Done-or-Doing and Doing-or-Done.
```

```text
Carriers and owners. Work-item reads (owner WorkItemRepository): Context.WorkItems.Include(Team).Where(today's predicate).ToList(), tracked, returned as IReadOnlyList<WorkItem>; W4 is a server-side Count() on the same query, returned as int, never load-and-count. Transition read (owner WorkItemStateTransitionRepository): Where(ids.Contains(WorkItemId)).ToList(), IReadOnlyList<WorkItemStateTransition>. Feature read (owner FeatureRepository): featureOrdering.Order(GetFeatures().Where(today's predicate).AsEnumerable()).ToList(), tracked, all five includes of GetFeatures(), IReadOnlyList<Feature>; F1 with onlyIn null serves the size chart, whose Done/ToDo/Doing filter stays in the service, in memory. No IQueryable, Expression or Func in a new signature. Tracking stays on, so identity and the Save in UpdateTeamMetrics see the same instances. Errors: a provider exception propagates unchanged; no new error type, retry or per-process state.
```

```text
Cross-layer algebra. Domain: unchanged (WorkItem, Feature, WorkItemStateTransition; no new state or transition). Application (TeamMetricsService): operations are the existing ITeamMetricsService members; observation is each metrics answer; the change replaces each GetAllByPredicate composition by the named read with the same predicate. Adapter (the three EF repositories): operation = named read; observation = returned id sequence, loaded navigations and tracking state, statement count. Infrastructure: SQLite and Postgres through EF Core 10.0.14 / Npgsql 10.0.3; no timeout, retry, recovery or new failure path.
```

```text
Behaviour-free oracle: the value slice's semantic RED is the S05a allow-list edit (remove Modules/FlowMetrics/Application/TeamMetricsService.cs from FilesThatCallGetAllByPredicate and set Has.Length to 14) while the service still calls GetAllByPredicate. Existing metrics tests keep every Assert, Is. and Verify line byte-identical; only Arrange lines that stubbed GetAllByPredicate retarget to the named reads through one test helper per service ('every read of this Team returns this list'). One stated exception: the Verify in TeamMetricsServiceTests that GetAllByPredicate was never called on Features retargets to F1 with the same Times.Never. RealityCheckQueryCountTest stays green with its assertion unchanged.
```

```text
Prefactoring before the value slice: the seven named reads land first on the owner repositories with no caller, together with the provider parity support TeamMetricsReadsParityOnBothProvidersTests (SQLite file and SharedPostgresContainer on IntegrationFixture, shaped like FeatureListParityOnPostgresTests), proven by a deliberate break. The slice commit then switches the callers. Moves separate from edits.
```

```text
Formal verification: claim-to-checker mapping. Claims: (1) each named read returns the same rows, in the same order, with the same loaded graph as today's predicate on SQLite and Postgres; (2) no extra statements; (3) the S05a list shrinks. None is a state-transition, concurrency, liveness or recovery claim, so Quint and TLA+/TLC have nothing to model but a restatement, and an Agda proof would be about a model of LINQ, not EF's translation. Checker availability is INDETERMINATE: quint, java and agda were refused by the session shell allowlist (exit 126). Human waiver recorded by the maintainer at the DESIGN hold (2026-11-10): formal verification waived for claims (1)-(3); they stay INDETERMINATE as proofs; the executable evidence is the provider parity tests, RealityCheckQueryCountTest, the S05a guard and the metrics answer comparison. Install route if wanted: npm install -g @informalsystems/quint after allowing quint in the lean-ctx allowlist.
```

```text
Left as is: the Save call in UpdateTeamMetrics through IWorkItemRepository (a unit-of-work write, decided by S07b or S03b), Team's featureRepository.GetAll() (in memory, not a GetAllByPredicate call), InProgressFeatureResolution, GetAllByPredicate itself (public until S05g), and the other allow-list callers (BlockedCountSeriesBuilder, OverTimeFillTarget, the demo backfill handlers, TeamMetricsController).
```

```text
Mutation bar at the end of the nwdes feature, on frozen code: TeamMetricsService not lower than 30.58 percent; the new read methods at 80 percent or more, killed by the parity class, so the Stryker test-case-filter names TeamMetricsReadsParityOnBothProviders and the Team metrics unit tests. Killing the service's old survivors is a separate improvement item.
```

##### Constraints

```text
Behaviour-free: routes, ITeamMetricsService, DTO shapes, serialized answers, row order, loaded graphs, tracking and the number of database statements per answer do not change.
```

```text
Ratchet: FilesThatCallGetAllByPredicate goes from 15 to 14 entries in the same commit that makes TeamMetricsService pass; inline list; no rule loosened; ApiTypesThatDependOnARepository (23) and the Flow metrics module rules R2-R6 unchanged; no baseline grows.
```

```text
Input bounds: teamId is an int Team id and onlyIn a nullable StateCategories, both as today; the transition id collection is unbounded, exactly as today's captured HashSet<int>; the design adds no bound and no validation.
```

```text
Program.cs, EF model, migrations, BaseMetricsService, controllers and DTOs are not touched; no new package.
```

```text
Test placement: the parity class is integration tier (Docker, Testcontainers Postgres plus a SQLite file), not in Test-Fast.ps1, within the backend per-class budget of 34 s, starts no web host, no allow-list entry. The Lighthouse test-writing skill governs where nWave's per-test timing and PBT density rules disagree. No nWave scenario tags or internal ids in test names or comments.
```

##### Reuse

IWorkItemRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemRepository\.cs\:5

Extend

```text
Owner contract of work items; gains W1-W5. Reading goes through the owner's contract.
```

GetAllByPredicate

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemRepository\.cs\:18

Extend

```text
WorkItemRepository's override: its Include(Team) plus tracked Where is exactly the base of W1-W5; the class gains the five reads over one private helper holding that include.
```

IWorkItemStateTransitionRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemStateTransitionRepository\.cs\:5

Extend

```text
Owner contract of work-item transitions, empty today; gains T1.
```

WorkItemStateTransitionRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemStateTransitionRepository\.cs\:7

Extend

```text
Adds nothing to RepositoryBase today (no include, no order); T1 is a plain Where(ids.Contains) materialised once.
```

IFeatureRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs\:5

Extend

```text
Owner contract of Features; gains F1.
```

GetFeatures

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:153

Reuse

```text
FeatureRepository.GetFeatures is the one definition of how a Feature is read whole (five includes, tracked); with IFeatureOrdering it makes F1 Order(GetFeatures().Where(p)), the same as today's GetAllByPredicate override.
```

FeatureRepository\.FeaturesOfTeam

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:106

CreateNew

```text
Rejected for F1: a list read with no tracking and only the Feature's own start forecast; reusing it changes start dates in answers. Coincidental similarity, not the same fact, so F1 is a new read.
```

FeatureRepository\.FeaturesWorkedOnBy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:86

CreateNew

```text
Rejected for F1: no TotalWorkItems above zero, no default-size exclusion, no simulation results.
```

TeamMetricsService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Extend

```text
Its 14 GetAllByPredicate calls switch to the seven named reads; constructor and ITeamMetricsService unchanged.
```

FilesThatCallGetAllByPredicate

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:35

Extend

```text
The S05a guard already scans every caller; the slice oracle shrinks its inline list by one entry and the count to 14.
```

FeatureListParityOnPostgresTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/FeatureListParityOnPostgresTests\.cs\:24

Reuse

```text
Provider parity shape (SQLite file plus SharedPostgresContainer on IntegrationFixture) reused by the new parity support.
```

RealityCheckQueryCountTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:42

Reuse

```text
Real SQLite, real WorkItemRepository, counts statements; pins that materialising inside the repository adds none. Unmodified.
```

WriteRecordingWorkItemRepository

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedWorkItemWrites\.cs\:59

Extend

```text
Decorator implementing IWorkItemRepository; gains five plain delegations to inner so it still compiles.
```

TeamMetricsServiceTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs\:17

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged. The Times.Never Verify on Features retargets to F1.
```

TeamMetricsServiceSnapshotTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs\:18

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged.
```

TeamWorkItemAgeAsOfDateTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:27

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged. The Portfolio class in the same file is feature B's.
```

MetricsAsOfGroupNameTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:18

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged.
```

InstanceDayItemBucketingTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:19

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged.
```

ClosedInRangeCharacterisationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:31

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged.
```

ForecastFilterFeatureForecastIntegrationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastFilterFeatureForecastIntegrationTest\.cs\:21

Extend

```text
Existing test of TeamMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through one per-service helper; Asserts unchanged. Its stub compiles the predicate over an in-memory list, so the retargeted stubs keep the same per-read filtering.
```

##### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemRepository\.cs\:5

Changed

```text
Five new read members, additive.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemRepository\.cs\:18

Changed

```text
Implements W1-W5 over a private helper; GetAllByPredicate unchanged.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IWorkItemStateTransitionRepository\.cs\:5

Changed

```text
One new read member, additive.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemStateTransitionRepository\.cs\:7

Changed

```text
Implements T1.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs\:5

Changed

```text
One new read member, additive.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:153

Changed

```text
Implements F1 over GetFeatures() and featureOrdering.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Changed

```text
14 call sites switch to named reads.
```

test\-helper

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedWorkItemWrites\.cs\:59

Migrated

```text
WriteRecordingWorkItemRepository implements IWorkItemRepository, so the five new members must be added as plain delegations to inner, or the test project does not compile. No recording, no assertion change.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs\:61

Migrated

```text
Arrange stubs retarget to the named reads; Asserts unchanged; the Times.Never Verify at line 194 retargets to F1.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs\:1

Migrated

```text
Arrange stubs retarget; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:1

Migrated

```text
Arrange stubs retarget; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:1

Migrated

```text
Arrange stubs retarget; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:1

Migrated

```text
Arrange stubs retarget; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:1

Migrated

```text
Arrange stubs retarget; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastFilterFeatureForecastIntegrationTest\.cs\:66

Migrated

```text
Builds TeamMetricsService with new and stubs GetAllByPredicate by compiling the predicate over an in-memory list; the retargeted stubs must keep the same per-read filtering so its Asserts stay unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:42

UnchangedCompatible

```text
Runs the real repositories on SQLite; its statement-count assertion is unchanged and must stay green.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:1

UnchangedCompatible

```text
Builds TeamMetricsService with new but stubs no GetAllByPredicate; constructor unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:1966

UnchangedCompatible

```text
Its GetAllByPredicate stubs serve TeamMetricsController's own call and the snapshot repository, both out of scope; ITeamMetricsService unchanged.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
Registrations unchanged: no new type, constructor unchanged.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs\:11

UnchangedCompatible

```text
Driving port unchanged; its consumers (controllers, refresh policies, hosted filler, Forecasting) are unaffected.
```

##### Agreement

IWorkItemRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemRepository\.cs\:9

Producer

Migrated

```text
Implements W1-W5 in the same commit as the interface.
```

IWorkItemRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedWorkItemWrites\.cs\:59

Producer

Migrated

```text
Decorator implementing the interface; delegates the new members to inner.
```

IWorkItemRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Consumer

Migrated

```text
Switches to the named reads in the value slice.
```

IWorkItemRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementList\.cs\:7

Consumer

UnchangedCompatible

```text
Additive members only; existing members unchanged.
```

IWorkItemStateTransitionRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/WorkItemStateTransitionRepository\.cs\:7

Producer

Migrated

```text
Implements T1 in the same commit as the interface.
```

IWorkItemStateTransitionRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Consumer

Migrated

```text
Switches its two transition reads to T1.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:11

Producer

Migrated

```text
Implements F1 in the same commit as the interface; sole production implementer.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Consumer

Migrated

```text
Switches its Feature read to F1.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/InProgressFeatureResolution\.cs\:13

Consumer

UnchangedCompatible

```text
Additive member only.
```

##### Not applicable

contract_tests

```text
No parties that change apart: the repositories, their interfaces and TeamMetricsService ship in one deployable and change in the same commit; no wire or persisted format changes. The primary oracle and the parity support cover the agreement.
```

##### Boundaries

```text
ITeamMetricsService (unchanged), reached by the metrics controllers, refresh-event policies, the hosted filler and Forecasting
```

###### Driven ports

```text
IWorkItemRepository (+W1-W5)
```

```text
IWorkItemStateTransitionRepository (+T1)
```

```text
IFeatureRepository (+F1)
```

```text
Flow metrics Application depends on the owner repository interfaces in Services/Interfaces/Repositories, implemented by the EF repositories over LighthouseAppContext; no edge added or removed.
```

###### Failures

None.

##### Observations

```text
The S05a guard's set of production files that call GetAllByPredicate, compared with the inline list and its length
```

set of file paths

```text
A call written through reflection or a delegate; a named read that internally builds a different predicate.
```

```text
Per named read and per provider (SQLite file, Postgres container): the returned id sequence, the loaded navigations (WorkItem.Team; Feature Portfolios, FeatureWork with Team, Forecasts and StartForecasts with SimulationResults, DependsOnReferences) and that entities are tracked; W4 compared with the length of W3
```

ordered id list plus loaded\-graph facts

```text
Plan-dependent row order on data much larger than the fixture; W4 loading rows and counting in memory (same number, a review item).
```

```text
Number of SQL statements per Reality Check answer on real SQLite with the real WorkItemRepository
```

int

```text
Which statement changed shape, as long as the count is equal.
```

```text
Every metrics answer of the instance backup, serialized, base revision against candidate (metrics-answers-compare)
```

identical or a byte diff

```text
Behaviour on data shapes absent from the backup.
```

##### Equality

named read vs today\'s predicate

```text
Equal when, on the same provider and data, both return the same ids in the same sequence, with the same navigations loaded and the entities tracked; for W4, the same integer.
```

##### Constructors

DoneItemsOfTeam

IReadOnlyList\<WorkItem\> DoneItemsOfTeam\(int teamId\)

DoingOrDoneItemsOfTeam

IReadOnlyList\<WorkItem\> DoingOrDoneItemsOfTeam\(int teamId\)

ToDoOrDoingItemsOfTeam

IReadOnlyList\<WorkItem\> ToDoOrDoingItemsOfTeam\(int teamId\)

CountToDoOrDoingItemsOfTeam

int CountToDoOrDoingItemsOfTeam\(int teamId\)

ItemsOfTeam

IReadOnlyList\<WorkItem\> ItemsOfTeam\(int teamId\)

TransitionsOfWorkItems

IReadOnlyList\<WorkItemStateTransition\> TransitionsOfWorkItems\(IReadOnlyCollection\<int\> workItemIds\)

FeaturesWithRealWorkForTeam

IReadOnlyList\<Feature\> FeaturesWithRealWorkForTeam\(int teamId\, StateCategories\? onlyIn\)

##### Laws

###### L1

```text
TeamMetricsService.cs is absent from the production callers of GetAllByPredicate, and the allow-list has exactly 14 unique entries equal to the scanned caller set.
```

Invariant

Application

RepositoryLeakRatchetArchUnitTest

###### Treatment

Finite

###### Cases

```text
Candidate source: guard green with 14 entries
```

```text
One GetAllByPredicate call put back in TeamMetricsService: guard red, naming the file
```

```text
A source scan over a finite file set; no generated input domain.
```

###### Evidence

None.

###### L2

```text
Each named read equals today's predicate run through the same base query (id sequence, loaded graph, tracking) on SQLite and on Postgres; W4 equals the length of W3; Done-or-Doing and Doing-or-Done spellings return the same sequence.
```

Refinement

Adapter

TeamMetricsReadsParityOnBothProvidersTests

###### Treatment

Finite

###### Cases

```text
Two Teams with items in all four categories, Unknown included
```

```text
Transitions for several item ids, read through T1
```

```text
Features with a default size, FeatureWork total 0 and above 0, a Feature in two Portfolios, F1 with onlyIn null and with a category
```

```text
Both OR spellings of W2
```

```text
Closed fixture over the four state categories and the F1 filter branches; the law is an equivalence across two engines on a fixed data set, not a broad generated domain. The Lighthouse test-writing skill governs density.
```

###### Evidence

None.

###### L3

```text
The number of SQL statements per Reality Check answer is unchanged (20 and 24) after the switch to named reads.
```

Invariant

Application

RealityCheckQueryCountTest

###### Treatment

Finite

###### Cases

```text
Existing RealityCheckQueryCountTest rows, assertion unchanged
```

```text
Existing finite statement-count test.
```

###### Evidence

None.

###### L4

```text
Every Team metrics answer serialized from the instance backup is identical between the base revision a86e3f8b4 and the candidate.
```

Refinement

Application

metrics\-answers\-compare

###### Treatment

Finite

###### Cases

```text
Scripts/examine/bin/metrics-answers-compare a86e3f8b4 <candidate>, exit 0, identical
```

```text
Captured comparison over one real data set, run at EXAMINE.
```

###### Evidence

None.

##### Refuted

```text
A category set passed as a parameter (one read per owner) is behaviour-free.
```

```text
It changes OR to IN over a parameter collection; row order is unspecified and visible in per-day item lists.
```

```text
Per-day WorkItemsPerUnitOfTime list serialised in a different order under a new plan.
```

```text
IFeatureListReader.FeaturesOfTeam can serve F1.
```

```text
No tracking and only the Feature's own start forecast loaded.
```

```text
Per-Team start forecasts missing, so start dates in Feature answers change.
```

```text
A work-item read without Include(Team) is equivalent because the mocked unit tests stay green.
```

```text
Mocks never see the missing navigation.
```

```text
AssociateSyncedTransitions builds new WorkItem(item, item.Team) with a null Team; the parity support asserts the loaded Team.
```

##### Claims

###### C1

```text
TeamMetricsService cannot compose a further query on a read: every new read returns a materialised IReadOnlyList or int, never IQueryable, Expression or Func.
```

```text
A chained operator would run where the provider cannot translate it, or in LINQ to Objects, unseen by EF InMemory tests.
```

IWorkItemRepository\, IWorkItemStateTransitionRepository\, IFeatureRepository

```text
int teamId, StateCategories? onlyIn, IReadOnlyCollection<int> ids
```

the seven named reads

TeamMetricsService

```text
ITeamMetricsService answers through the metrics controllers
```

```text
GetAllByPredicate stays public on IRepository until S05g; the S05a guard is the remaining check. C# static types, no proof of the predicate's meaning.
```

##### States

Admitted

```text
TeamMetricsService reads work items, transitions and Features only through the seven named reads
```

TeamMetricsService

Excluded

```text
TeamMetricsService calls GetAllByPredicate (excluded by the S05a guard, extrinsic)
```

RepositoryLeakRatchetArchUnitTest

Unknown

```text
SQLite or Postgres plan order for OR spellings on data much larger than the fixture
```

host

##### Models

None.

##### Closure

```text
TeamMetricsService off GetAllByPredicate
```

Safety

Pass

DesignedNotBuilt

```text
S05a guard scans every production file; list edit to 14 is the oracle.
```

###### Evidence

None.

```text
named Team reads
```

Inhabitation

Pass

DesignedNotBuilt

```text
int teamId and an id collection, already held by the service, construct every read; the parity fixture builds each one on both providers.
```

###### Evidence

None.

```text
Team metrics answers
```

PublicReachability

Pass

DesignedNotBuilt

```text
metrics controllers, policies and hosted filler -> ITeamMetricsService -> TeamMetricsService -> named reads -> EF repositories; no new seam, so the existing wiring reaches them.
```

###### Evidence

None.

```text
named Team reads
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous, request-scoped reads with no wait, retry, queue or progress claim.
```

###### Evidence

None.

##### Risks

###### Y1

Stressor

```text
Assumption 'same predicate gives same plan and order' negated: the plan order of OR spellings differs on large data
```

```text
Same rows per read
```

```text
None in the fixture
```

```text
A changed order in a serialized per-day item list
```

```text
Shown on the fixture, not proven for large SQLite data; the backup comparison covers one real data set
```

host

###### Y2

Stressor

```text
Assumption 'Docker is present' negated
```

```text
Fast loop and unit tests
```

```text
Parity support skipped locally
```

```text
Merging without the parity class green in CI
```

```text
A developer without Docker relies on CI
```

host

###### Y3

Stressor

```text
Assumption 'the instance backup exists in the examiner environment' negated
```

```text
Parity, guard and statement-count evidence
```

```text
metrics-answers-compare INDETERMINATE
```

```text
Reporting that comparison as met
```

```text
That EXAMINE expectation stays INDETERMINATE
```

host

###### Y4

Bypass

```text
GetAllByPredicate remains public on IRepository
```

```text
TeamMetricsService off the allow-list
```

```text
Other listed callers keep it
```

```text
A new unlisted caller
```

```text
S05a guard catches any new caller; S05g makes it protected
```

S05g

###### Y5

Bypass

```text
Loose Moq returns an empty list for a named read nobody set up
```

```text
Unchanged Asserts
```

```text
Arrange retargeting
```

```text
A test green because a read returns nothing
```

```text
A missed set-up turns the test red, not falsely green; review checks the diff shows no changed Assert, Is. or Verify line
```

reviewer

##### Architectural tests

###### T1

```text
Only the files on the inline list call GetAllByPredicate; TeamMetricsService is no longer on it (15 to 14), scanned over every production source file, including later ones.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RepositoryLeakRatchetArchUnitTest
```

###### T2

```text
Flow metrics module rules R2-R6 hold with unchanged baselines: no edge from the module's Application to EF Core or Data, no newcomer on the inbound lists, in CI's Release configuration.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Release --filter FullyQualifiedName~Architecture
```

##### Contract tests

None.

## ADRs

None.
