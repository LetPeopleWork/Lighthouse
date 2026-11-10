# Architecture brief: story\-6374\-s05f\-b\-portfolio\-reads

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs

Extend

```text
Prefactoring: F2-F6 declared, no caller yet.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs

Extend

```text
Prefactoring: F2-F6 as featureOrdering.Order(GetFeatures().Where(today's predicate).AsEnumerable()).ToList() over one private helper; never the list read.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureStateTransitionRepository\.cs

Extend

```text
Prefactoring: T2 declared.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureStateTransitionRepository\.cs

Extend

```text
Prefactoring: T2 as Where(ids.Contains(FeatureId)).ToList().
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
Value slice: the 13 GetAllByPredicate calls switch to the six named reads; IRepository<Feature> becomes IFeatureRepository.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureRepositoryDoubles\.cs

Extend

```text
Portfolio read answers with each read's own predicate over a list read at each call; a T2 answer for the transition mock.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Extend

```text
Mock type and Arrange stubs retarget; transition mock answers empty by default; Asserts byte-identical.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Portfolio class: Arrange stubs retarget, explicit empty T2 answer.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Transition stub retargets to T2; Mock.Of<IRepository<Feature>> becomes Mock.Of<IFeatureRepository>.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Portfolio builder: Arrange stubs retarget, explicit empty T2 answer.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Portfolio builder: Arrange stubs retarget, explicit empty T2 answer.
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PortfolioMetricsReadsParityOnBothProvidersTests\.cs

#### Public oracle

```text
Run RepositoryLeakRatchetArchUnitTest.GetAllByPredicate_IsCalledOnlyFromTheFilesOnTheList with Modules/FlowMetrics/Application/PortfolioMetricsService.cs removed from FilesThatCallGetAllByPredicate and Has.Length set to 13; with it, the parity support on SQLite and Postgres and the existing Portfolio metrics tests.
```

```text
RED on the base: the guard fails naming PortfolioMetricsService.cs as a caller not on the list. GREEN on the candidate: the guard passes with 13 entries, every named Portfolio read returns the same id sequence, graph and tracking as today's predicate on both providers, and every existing Portfolio metrics Assert passes unchanged.
```

```text
Put one GetAllByPredicate call back in PortfolioMetricsService: the guard goes red and names the file. Drop Doing from F3 or the StartForecasts include from the Feature helper: the parity support goes red on both providers. Leave a transition mock unanswered: the Portfolio metrics test goes red, not falsely green.
```

##### Promises

P1

P2

P3

#### Prefactoring

```text
Add the six named reads F2-F6 and T2 to IFeatureRepository, FeatureRepository, IFeatureStateTransitionRepository and FeatureStateTransitionRepository, each keeping today's predicate, includes, tracking and ordering, with no caller yet; land the parity support PortfolioMetricsReadsParityOnBothProvidersTests, proven by a deliberate break. This makes the value slice's switch of PortfolioMetricsService a call-site and constructor-type edit.
```

```text
Every existing test stays green unchanged before and after the move; no production caller changes; GetAllByPredicate and the S05a list (14) unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

##### Verification

```text
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.sln --filter 'TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration'
```

#### Records

##### Decisions

```text
Read model: PortfolioMetricsService reads through six named reads on the owner repositories, not a query object per capability and not Flow-metrics ports in the module. F2 DoneFeaturesOfPortfolio(portfolioId), F3 DoingOrDoneFeaturesOfPortfolio(portfolioId), F4 ToDoOrDoingFeaturesOfPortfolio(portfolioId), F5 ToDoDoingOrDoneFeaturesOfPortfolio(portfolioId), F6 EveryFeatureOfPortfolio(portfolioId) on IFeatureRepository; T2 TransitionsOfFeatures(IReadOnlyCollection<int> featureIds) on IFeatureStateTransitionRepository. Reading goes through the owner's contract, so FeatureRepository keeps its one definition of how a Feature is read whole; no new dependency edge, no Program.cs edit. Call sites: F2 at 28 and 755; F3 at 143, 161, 182 (written Done or Doing), 216, 599; F4 at 264; F5 at 564; F6 at 434; T2 at 241, 447, 504. Names are working names; the crafter may improve them, but no new read may be called FeaturesOfPortfolio or FeaturesOfTeam.
```

```text
Constructor: PortfolioMetricsService takes IFeatureRepository instead of IRepository<Feature>. DI already resolves IRepository<Feature> by forwarding to the scoped IFeatureRepository, so the service receives the same instance and Program.cs is untouched. The five test sites that build the service with new change the mock type to IFeatureRepository.
```

```text
Shape of a read: one named read per state-category set, each keeping today's predicate literally (Portfolios.Any(p => p.Id == portfolioId) and Done; Doing or Done; ToDo or Doing; Done or ToDo or Doing; Portfolio only), with one private helper in FeatureRepository holding Order(GetFeatures().Where(predicate).AsEnumerable()).ToList(). A category set passed as a parameter or a value object is rejected: it turns OR into IN over a parameter collection. F3 serves both OR spellings.
```

```text
Carriers and owners. Feature reads (owner FeatureRepository): featureOrdering.Order(GetFeatures().Where(today's predicate).AsEnumerable()).ToList(), tracked, all five includes of GetFeatures(), returned as IReadOnlyList<Feature>; never the untracked list read. Transition read (owner FeatureStateTransitionRepository): Where(ids.Contains(FeatureId)).ToList(), IReadOnlyList<FeatureStateTransition>; the service passes its HashSet<int>. No IQueryable, Expression or Func in a new signature. Tracking stays on. Errors: a provider exception propagates unchanged; no new error type, retry or per-process state.
```

```text
Cross-layer algebra. Domain: unchanged (Feature, FeatureStateTransition; no new state or transition). Application (PortfolioMetricsService): operations are the existing IPortfolioMetricsService members; observation is each metrics answer; the change replaces each GetAllByPredicate composition by the named read with the same predicate. Adapter (FeatureRepository, FeatureStateTransitionRepository): operation = named read; observation = returned id sequence, loaded navigations and tracking state. Infrastructure: SQLite and Postgres through EF Core 10.0.14 / Npgsql 10.0.3; no timeout, retry, recovery or new failure path.
```

```text
Behaviour-free oracle: the value slice's semantic RED is the S05a allow-list edit (remove Modules/FlowMetrics/Application/PortfolioMetricsService.cs from FilesThatCallGetAllByPredicate and set Has.Length to 13) while the service still calls GetAllByPredicate. Existing Portfolio metrics tests keep every Assert, Is. and Verify line byte-identical; only Arrange lines that stubbed GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles. Those stubs compile today's predicate over the test's list, so the doubles answer each read with its own predicate, reading the list at each call.
```

```text
Prefactoring before the value slice: the six named reads land first on the owner repositories with no caller, together with the provider parity support PortfolioMetricsReadsParityOnBothProvidersTests (SQLite file and SharedPostgresContainer on IntegrationFixture, shaped like TeamMetricsReadsParityOnBothProvidersTests), proven by a deliberate break. The slice commit then switches the callers and the constructor. Moves separate from edits.
```

```text
Formal verification: claim-to-checker mapping. Claims: (1) each named Portfolio read returns the same rows, in the same order, with the same loaded graph as today's predicate on SQLite and Postgres; (2) no extra statements; (3) the S05a list shrinks to 13. None is a state-transition, concurrency, liveness or recovery claim, so Quint and TLA+/TLC have nothing to model but a restatement, and an Agda proof would be about a model of LINQ, not EF's translation. Checker availability is INDETERMINATE: quint, java and agda are refused by the session shell allowlist (exit 126). Human waiver recorded by the maintainer at the DESIGN hold (2026-11-10): formal verification waived for claims (1)-(3); they stay INDETERMINATE as proofs; the executable evidence is the provider parity tests, the S05a guard, the unchanged Portfolio metrics tests and the metrics answer comparison. Install route if wanted: npm install -g @informalsystems/quint after allowing quint in the lean-ctx allowlist.
```

```text
Left as is: TeamMetricsService (feature A), GetAllByPredicate itself (public until S05g), the other allow-list callers (BlockedCountSeriesBuilder, OverTimeFillTarget, the demo backfill handlers, TeamMetricsController), the untracked FeaturesOfPortfolio list read, and the IRepository<Feature> DI forward used by other consumers.
```

```text
Mutation bar at the end of the nwdes feature, on frozen code: PortfolioMetricsService not lower than 27.60 percent; the new read methods at 80 percent or more, killed by the parity class, so the Stryker test-case-filter names PortfolioMetricsReadsParityOnBothProviders and the Portfolio metrics unit tests. Killing the service's old survivors is a separate improvement item.
```

##### Constraints

```text
Behaviour-free: routes, IPortfolioMetricsService, DTO shapes, serialized answers, row order, loaded graphs, tracking and the number of database statements per answer do not change.
```

```text
Ratchet: FilesThatCallGetAllByPredicate goes from 14 to 13 entries in the same commit that makes PortfolioMetricsService pass; inline list; no rule loosened; ApiTypesThatDependOnARepository (23) and the Flow metrics module rules R2-R6 unchanged; no baseline grows.
```

```text
Input bounds: portfolioId is an int Portfolio id, as today; the transition id collection is unbounded, exactly as today's captured HashSet<int>; the design adds no bound and no validation.
```

```text
Program.cs, EF model, migrations, BaseMetricsService, controllers and DTOs are not touched; no new package.
```

```text
Test doubles: Moq returns null, not an empty list, for an unstubbed IReadOnlyList read, where the old IQueryable stub returned an empty sequence. Every test that builds PortfolioMetricsService with Mock.Of<IFeatureStateTransitionRepository>() or a bare transition mock gets an explicit empty TransitionsOfFeatures answer, and every retargeted Feature mock answers all five Feature reads. The Portfolio doubles read the test's list at each call, because PortfolioMetricsServiceTests assigns its list in SetupTestData after the stub is set up and tests add Features afterwards; the snapshotting FeatureRepositoryDoubles.Holds cannot serve them.
```

```text
Test placement: the parity class is integration tier (Docker, Testcontainers Postgres plus a SQLite file), not in Test-Fast.ps1, within the backend per-class budget of 34 s, starts no web host, no allow-list entry. The Lighthouse test-writing skill governs where nWave's per-test timing and PBT density rules disagree. No nWave scenario tags or internal ids in test names or comments.
```

##### Reuse

IFeatureRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs\:5

Extend

```text
Owner contract of Features; gains F2-F6. Reading goes through the owner's contract.
```

GetAllByPredicate

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:40

Extend

```text
FeatureRepository's override, Order(GetFeatures().Where(p)) executed at once and tracked, is exactly the base of F2-F6; the class gains the five reads over one private helper holding that shape.
```

GetFeatures

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:163

Reuse

```text
The one definition of how a Feature is read whole (five includes, tracked); with IFeatureOrdering it makes each of F2-F6.
```

FeaturesOfPortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:113

CreateNew

```text
Rejected for F6: an untracked list read with only the Feature's own start forecast; reusing it changes start dates in answers. Coincidental similarity, not the same fact, so F6 is a new read.
```

IFeatureStateTransitionRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureStateTransitionRepository\.cs\:5

Extend

```text
Owner contract of Feature transitions, empty today; gains T2.
```

FeatureStateTransitionRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureStateTransitionRepository\.cs\:7

Extend

```text
Adds nothing to RepositoryBase today (no include, no order); T2 is a plain Where(ids.Contains(FeatureId)) materialised once.
```

PortfolioMetricsService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:10

Extend

```text
Its 13 GetAllByPredicate calls switch to the six named reads; its feature parameter becomes IFeatureRepository; IPortfolioMetricsService unchanged.
```

FilesThatCallGetAllByPredicate

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:35

Extend

```text
The S05a guard already scans every caller; the slice oracle shrinks its inline list by one entry and the count to 13.
```

TeamMetricsReadsParityOnBothProvidersTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs\:24

Reuse

```text
Feature A's provider parity shape (SQLite file plus SharedPostgresContainer on IntegrationFixture) reused by the Portfolio parity support.
```

FeatureRepositoryDoubles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureRepositoryDoubles\.cs\:13

Extend

```text
The Feature repository double; gains answers for F2-F6 that apply each read's own predicate over a list read at each call, and an answer for T2 on the transition mock. Holds snapshots its list, so it is not reused for the Portfolio tests.
```

PortfolioMetricsServiceTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs\:18

Extend

```text
Existing test of PortfolioMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles, and the mock type becomes IFeatureRepository where it builds the service; Asserts unchanged.
```

PortfolioWorkItemAgeAsOfDateTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:230

Extend

```text
Existing test of PortfolioMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles, and the mock type becomes IFeatureRepository where it builds the service; Asserts unchanged.
```

MetricsAsOfGroupNameTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:19

Extend

```text
Existing test of PortfolioMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles, and the mock type becomes IFeatureRepository where it builds the service; Asserts unchanged. Its Mock.Of<IRepository<Feature>> becomes Mock.Of<IFeatureRepository>.
```

InstanceDayItemBucketingTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:20

Extend

```text
Existing test of PortfolioMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles, and the mock type becomes IFeatureRepository where it builds the service; Asserts unchanged.
```

ClosedInRangeCharacterisationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:32

Extend

```text
Existing test of PortfolioMetricsService; only the Arrange stubs of GetAllByPredicate retarget to the named reads through FeatureRepositoryDoubles, and the mock type becomes IFeatureRepository where it builds the service; Asserts unchanged.
```

##### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureRepository\.cs\:5

Changed

```text
Five new read members, additive.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:40

Changed

```text
Implements F2-F6 over a private helper; GetAllByPredicate unchanged.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Repositories\/IFeatureStateTransitionRepository\.cs\:5

Changed

```text
One new read member, additive.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureStateTransitionRepository\.cs\:7

Changed

```text
Implements T2.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:10

Changed

```text
13 call sites switch to named reads; constructor parameter IRepository<Feature> becomes IFeatureRepository.
```

test\-helper

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureRepositoryDoubles\.cs\:13

Migrated

```text
Gains the Portfolio read answers and the T2 answer; Holds unchanged for its other users.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs\:21

Migrated

```text
Mock<IRepository<Feature>> becomes Mock<IFeatureRepository>; 8 Feature and 4 transition stubs retarget; transition mock gets an empty default answer; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs\:230

Migrated

```text
Portfolio class: Feature stub retargets, explicit empty T2 answer; Asserts unchanged. The Team class was feature A's.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs\:91

Migrated

```text
Transition stub retargets to T2; Mock.Of<IRepository<Feature>> becomes Mock.Of<IFeatureRepository>; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs\:259

Migrated

```text
Portfolio builder: Feature stub retargets, explicit empty T2 answer; Asserts unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs\:120

Migrated

```text
Portfolio builder: Feature stub retargets, explicit empty T2 answer; Asserts unchanged.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1355

UnchangedCompatible

```text
IRepository<Feature> forwards to the scoped IFeatureRepository (1354) for other consumers; PortfolioMetricsService (1401) now resolves IFeatureRepository directly, the same instance.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs\:9

UnchangedCompatible

```text
Driving port unchanged; controllers, policies, report series, OverTimeFillTarget and WorkItemService are unaffected.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs\:138

UnchangedCompatible

```text
Its IRepository<Feature> mock serves OverTimeFillTarget, out of scope; it builds no PortfolioMetricsService.
```

##### Agreement

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureRepository\.cs\:11

Producer

Migrated

```text
Implements F2-F6 in the same commit as the interface; sole production implementer.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FeatureRepositoryDoubles\.cs\:13

Producer

Migrated

```text
Test double answering F2-F6 with each read's own predicate.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:10

Consumer

Migrated

```text
Switches its ten Feature reads to F2-F6 in the value slice.
```

IFeatureRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Consumer

UnchangedCompatible

```text
Uses F1 only; additive members.
```

IFeatureStateTransitionRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/FeatureStateTransitionRepository\.cs\:7

Producer

Migrated

```text
Implements T2 in the same commit as the interface.
```

IFeatureStateTransitionRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:15

Consumer

Migrated

```text
Switches its three transition reads to T2.
```

IFeatureStateTransitionRepository read members

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/WorkItemService\.cs\:29

Consumer

UnchangedCompatible

```text
Additive member only; existing members unchanged.
```

##### Not applicable

contract_tests

```text
No parties that change apart: the repositories, their interfaces and PortfolioMetricsService ship in one deployable and change in the same commit; no wire or persisted format changes. The primary oracle and the parity support cover the agreement.
```

##### Boundaries

```text
IPortfolioMetricsService (unchanged), reached by PortfolioMetricsController, the refresh-event and snapshot policies, OverTimeFillTarget, PortfolioReportMetricSeries and WorkItemService
```

###### Driven ports

```text
IFeatureRepository (+F2-F6)
```

```text
IFeatureStateTransitionRepository (+T2)
```

```text
Flow metrics Application depends on the owner repository interfaces in Services/Interfaces/Repositories, implemented by the EF repositories over LighthouseAppContext; no edge added or removed (PortfolioMetricsService already depended on that namespace).
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
Per named Portfolio read and per provider (SQLite file, Postgres container): the returned id sequence, the loaded navigations (Feature Portfolios, FeatureWork with Team, Forecasts and StartForecasts with SimulationResults, DependsOnReferences; none for transitions) and that entities are tracked
```

ordered id list plus loaded\-graph facts

```text
Ordering-policy ties on data much larger than the fixture; a transition read whose row order differs on large data.
```

```text
Every Portfolio metrics answer of the existing PortfolioMetricsService unit tests, through IPortfolioMetricsService with Asserts unchanged
```

NUnit pass or fail per test

```text
Provider translation, tracking and loaded navigations: the doubles run in memory.
```

```text
Every metrics answer of the instance backup, serialized, base revision against candidate (metrics-answers-compare)
```

identical or a byte diff

```text
Behaviour on data shapes absent from the backup; the number of statements.
```

##### Equality

named Portfolio read vs today\'s predicate

```text
Equal when, on the same provider and data, both return the same ids in the same sequence, with the same navigations loaded and the entities tracked.
```

##### Constructors

DoneFeaturesOfPortfolio

IReadOnlyList\<Feature\> DoneFeaturesOfPortfolio\(int portfolioId\)

DoingOrDoneFeaturesOfPortfolio

IReadOnlyList\<Feature\> DoingOrDoneFeaturesOfPortfolio\(int portfolioId\)

ToDoOrDoingFeaturesOfPortfolio

IReadOnlyList\<Feature\> ToDoOrDoingFeaturesOfPortfolio\(int portfolioId\)

ToDoDoingOrDoneFeaturesOfPortfolio

IReadOnlyList\<Feature\> ToDoDoingOrDoneFeaturesOfPortfolio\(int portfolioId\)

EveryFeatureOfPortfolio

IReadOnlyList\<Feature\> EveryFeatureOfPortfolio\(int portfolioId\)

TransitionsOfFeatures

IReadOnlyList\<FeatureStateTransition\> TransitionsOfFeatures\(IReadOnlyCollection\<int\> featureIds\)

##### Laws

###### L1

```text
PortfolioMetricsService.cs is absent from the production callers of GetAllByPredicate, and the allow-list has exactly 13 unique entries equal to the scanned caller set.
```

Invariant

Application

RepositoryLeakRatchetArchUnitTest

###### Treatment

Finite

###### Cases

```text
Candidate source: guard green with 13 entries
```

```text
One GetAllByPredicate call put back in PortfolioMetricsService: guard red, naming the file
```

```text
A source scan over a finite file set; no generated input domain.
```

###### Evidence

None.

###### L2

```text
Each named Portfolio read equals today's predicate run through the same base query (id sequence, loaded graph, tracking) on SQLite and on Postgres; Done-or-Doing and Doing-or-Done spellings return the same sequence through F3.
```

Refinement

Adapter

PortfolioMetricsReadsParityOnBothProvidersTests

###### Treatment

Finite

###### Cases

```text
Two Portfolios with Features in all four categories, Unknown included
```

```text
A Feature in two Portfolios, a Feature in no Portfolio
```

```text
Transitions for several Feature ids and for a Feature not asked about, read through T2
```

```text
Both OR spellings of F3
```

```text
Closed fixture over the four state categories and Portfolio membership; the law is an equivalence across two engines on a fixed data set, not a broad generated domain. The Lighthouse test-writing skill governs density.
```

###### Evidence

None.

###### L3

```text
The number of SQL statements per Portfolio metrics answer is unchanged after the switch to named reads.
```

Invariant

Application

PortfolioMetricsService

###### Treatment

NoTest

```text
No Portfolio statement-count test exists. Today's Feature override already executes at once and every transition query is enumerated once by its caller, so materialising inside the repository adds no statement; a review item, and the answer comparison shows no changed answer.
```

###### Evidence

None.

###### L4

```text
Every Portfolio metrics answer serialized from the instance backup is identical between the base revision a86e3f8b4 and the candidate.
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
IFeatureListReader.FeaturesOfPortfolio can serve F6.
```

```text
No tracking and only the Feature's own start forecast loaded.
```

```text
Per-Team start forecasts missing, so start dates in Feature answers change.
```

```text
A category set passed as a parameter (one read per owner) is behaviour-free.
```

```text
It changes OR to IN over a parameter collection, a different SQL shape.
```

```text
A plan change reorders tied Features before the in-memory ordering, or transition rows, in an answer.
```

```text
An unstubbed named read behaves like the old unstubbed GetAllByPredicate in a Moq test.
```

```text
Moq's empty default covers IEnumerable and IQueryable, not IReadOnlyList.
```

```text
Mock.Of<IFeatureStateTransitionRepository>() answers TransitionsOfFeatures with null, and the service throws where it used to see no transitions.
```

##### Claims

###### C1

```text
PortfolioMetricsService cannot compose a further query on a read: every new read returns a materialised IReadOnlyList, never IQueryable, Expression or Func.
```

```text
A chained operator would run where the provider cannot translate it, or in LINQ to Objects, unseen by EF InMemory tests.
```

IFeatureRepository\, IFeatureStateTransitionRepository

```text
int portfolioId, IReadOnlyCollection<int> featureIds
```

the six named reads

PortfolioMetricsService

```text
IPortfolioMetricsService answers through PortfolioMetricsController
```

```text
GetAllByPredicate stays public on IRepository until S05g; the S05a guard is the remaining check. C# static types, no proof of the predicate's meaning.
```

##### States

Admitted

```text
PortfolioMetricsService reads Features and Feature transitions only through the six named reads
```

PortfolioMetricsService

Excluded

```text
PortfolioMetricsService calls GetAllByPredicate (excluded by the S05a guard, extrinsic)
```

RepositoryLeakRatchetArchUnitTest

Unknown

```text
Postgres or SQLite row order of transition reads on data much larger than the fixture
```

host

##### Models

None.

##### Closure

```text
PortfolioMetricsService off GetAllByPredicate
```

Safety

Pass

DesignedNotBuilt

```text
S05a guard scans every production file; list edit to 13 is the oracle.
```

###### Evidence

None.

```text
named Portfolio reads
```

Inhabitation

Pass

DesignedNotBuilt

```text
portfolio.Id and an id set, already held by the service, construct every read; the parity fixture builds each one on both providers.
```

###### Evidence

None.

```text
Portfolio metrics answers
```

PublicReachability

Pass

DesignedNotBuilt

```text
PortfolioMetricsController, policies, report series -> IPortfolioMetricsService -> PortfolioMetricsService -> named reads -> EF repositories; DI already resolves IFeatureRepository, so the existing wiring reaches them.
```

###### Evidence

None.

```text
named Portfolio reads
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
Assumption 'same predicate gives same plan and order' negated: the plan order of OR spellings or of transition rows differs on large data
```

```text
Same rows per read
```

```text
None in the fixture
```

```text
A changed order in a serialized answer
```

```text
Shown on the fixture, not proven for large data; the backup comparison covers one real data set
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
Parity, guard and unit-test evidence
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
PortfolioMetricsService off the allow-list
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
Loose Moq returns null for a named read nobody set up
```

```text
Unchanged Asserts
```

```text
Arrange retargeting with explicit answers
```

```text
A test green because a read returns nothing
```

```text
A missed set-up throws, so the test goes red, not falsely green; review checks the diff shows no changed Assert, Is. or Verify line
```

reviewer

##### Architectural tests

###### T1

```text
Only the files on the inline list call GetAllByPredicate; PortfolioMetricsService is no longer on it (14 to 13), scanned over every production source file, including later ones.
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
