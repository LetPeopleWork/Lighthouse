# Architecture brief: story\-6366\-s03h\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Layer per file: ReportCapPolicy, ReportSeriesReading and ReportWindowPolicy go to Modules/Reports/Domain (namespace Lighthouse.Backend.Modules.Reports.Domain); the nine other implementation files go to Modules/Reports/Application; the eight metric files go to Modules/Reports/Application/Metrics; the eleven port files go whole to Modules/Reports/Application, each with the records and enums it declares. ReportsMembership.MovingTypes is the single owner of which type moves where; the new oracles read it and add no second list.
```
Governed slices: S1

#### D2

```text
Carriers: decisions stay in their existing owners (ReportCapPolicy, ReportWindowPolicy, ReportSeriesReading, the metric classes, ReportCommands, ReportQueries and the others keep their data and behaviour); only namespace and folder change. Errors, effects and per-process state stay as they are: the ports and the Program.cs registrations are unchanged, and no state is added. No class is created, split or merged and no signature changes.
```
Governed slices: S1

#### D3

```text
Staying: ReportRepository (Services/Implementation/Repositories), Models/Reports, the API controllers, ReportsControllerBase, the DTOs and ReportOwnerWriteAccess stay where they are; only their using lines change. Program.cs: the three Reports using lines are replaced by the three module namespaces; RegisterReports keeps its text, order, lifetimes and service and implementation pairs, and there is no AddReports extension. Reports log categories change with the namespace; no appsettings, analyser, suppression, CI, budget, host-user or Stryker target names a moving type (measured: no non-C# file in the repository names the old Reports folders).
```
Governed slices: S1

#### D4

```text
Rename list ReportsStringsTheMoveRenames is a craft target, not a support: its five literals are renamed in their files (FlowMetricsInboundDomainAndAdaptersArchUnitTest, LicensingModuleBoundariesArchUnitTest twice, RepositoryLeakRatchetArchUnitTest, TrackerIntegrationModuleBoundariesArchUnitTest) and its 31 PlatformLaneUnitTestPlacementTest rows are removed, each entry leaving the list in the change that renames its string; the list ends empty. TrackerIntegrationModuleBoundariesArchUnitTest also holds a second old-path reference (a global-qualified IReportQueries in a made-up case) that follows the move. Frozen feature A oracles (ReportsMembership, ReportsMembershipArchUnitTest, ReportsRulesSeeTheMoveArchUnitTest) are not edited; rule patterns are not narrowed here.
```
Governed slices: S1

#### D5

```text
Unit tests follow the module: ReportWindowPolicyTest and ReportCapPolicyTest go to Tests/Modules/Reports/Domain; ArrivalsReportMetricTest, CycleTimeReportMetricTest, DailySeriesSummaryTest, ThroughputReportMetricTest and WorkItemAgeReportMetricTest go to Tests/Modules/Reports/Application/Metrics; the other 11 (PortfolioReportMetricSeriesTest, ReportBulkCommandsTest, ReportBulkRemoverTest, ReportCommandsTest, ReportLoggingTest, ReportMetricSeriesByOwnerTest, ReportOwnerLookupTest, ReportQueriesTest, TeamReportMetricSeriesTest, ThenAndNowTemplateEditTest, ThenAndNowTemplateTest) go to Tests/Modules/Reports/Application, namespace equal to folder. ReportWindowPolicyTest keeps its using of Tests.API.Integration.Reports. The port, host, wire-contract tests and ReportRepositoryTest stay; only their using lines change. Move-only: no assertion is edited.
```
Governed slices: S2

#### D6

```text
Checkers: the claims are a finite closed namespace-and-folder placement over 31 production types and 18 test classes, with no state, ordering or concurrency, so no Quint, TLA+ or Agda model is applicable (confirmed in the DESIGN); each claim is checked by the compiled-IL architecture oracle (production) and the source-tree check (folders) run as NUnit tests, plus feature A's preservation law, and behaviour by the unchanged acceptance, integration and wire-contract tests. Property-based generation is not applicable: finite maps, native finite cases. Liveness is not applicable. Types: the C# compiler is the repository checker; a stale namespace or using fails dotnet build; reflection or string-built names are the bypass, checked by the full filtered suite. Paradigm object_oriented: no new owner, entity or service is created, each rule keeps its cohesive existing owner.
```
Governed slices: S1, S2

#### D7

```text
Rigor standard: architect, acceptance designer and crafter on sonnet, reviewers and EXAMINE on haiku. Moves go in refactor(reports) commits apart from edits: production move first, then the test move. Program.cs order is observed only by the candidate review; no public probe sees it.
```
Governed slices: S1, S2

### Constraints

#### K1

```text
Behaviour-free: no route, DTO, header, wire format, EF model or migration change. Input bounds: none, the move takes no input and has no stored or validated input. Comments never cite internal ids. Narrowing the Reports rule patterns, the new Reports boundary rules, the unit-test placement test and the architecture documents are feature C, not here.
```
Governed slices: S1, S2

#### K2

```text
Declared commands stay under the 120 s shell limit: oracle by name fragment, the Architecture namespace, dotnet build Lighthouse.Backend, and the filtered full suite as a background job. A Program.cs change selects every live connector suite in CI; no live suite runs locally. Per-test budget default; no derogation requested.
```
Governed slices: S1, S2

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Producer and consumer change in one deployable and one commit set; no persisted or wire data changes; the acceptance, integration and wire-contract tests are the primary oracle for unchanged behaviour.
```

agreement

```text
No shared schema or wire format is touched: DTOs, routes, headers and the EF model are unchanged; the only shared names are C# namespaces inside one deployable, covered by impact records.
```

### Boundaries

```text
ReportsController, TeamReportsController and PortfolioReportsController over IReportQueries, IReportCommands, IReportBulkCommands and IReportBulkRemover (unchanged), observed through the Reports acceptance, integration and wire-contract tests
```

#### Driven ports

```text
IReportReader and IReportWriter (implemented by the staying ReportRepository), IReportOwnerLookup, IReportMetricSeries, IReportMetric and IReportTemplate (move with the module)
```

```text
API and Program.cs depend on Modules.Reports.Application; Application depends on Modules.Reports.Domain and the shared kernel Models.Reports; Domain depends on nothing of Services. Direction unchanged, only namespaces move.
```

#### Failures

None.

### Observations

```text
Namespace and declaring-file folder of each type of ReportsMembership, read from the compiled IL and the source tree; and the namespace and folder of each of the 18 Reports unit-test classes
```

per type\: \(namespace\, folder\) pair

```text
behaviour of the type; a type moved without its behaviour preserved is caught only by the existing acceptance, integration and wire-contract tests; Program.cs registration order is not visible to any public probe
```

### Equality

namespace\-and\-folder observation

```text
A type is in place when its full name equals the map's new full name, its namespace equals its folder path, and its old full name no longer exists; behaviour equality is observed by the unchanged acceptance and wire-contract tests.
```

### Constructors

None.

### Laws

#### L1

```text
For every moving type of ReportsMembership the assembly holds exactly the new full name Lighthouse.Backend.Modules.Reports.Domain, .Application or .Application.Metrics plus the name, not the old one, and its file sits in the folder of that namespace with the types the map lists for it
```

Invariant

Domain

ReportsMembership

##### Treatment

Finite

###### Cases

```text
ReportCapPolicy in Domain
```

```text
a type of each of the nine Application implementation files
```

```text
a type of each of the eight Metrics files
```

```text
each of the eleven port files whole in Application
```

```text
The domain is the finite map of 31 files; a native finite-case test over the map covers it and shrinking adds nothing.
```

##### Evidence

None.

#### L2

```text
The rename list holds no entry and no file of the moving set remains under Services/Implementation/Reports or Services/Interfaces/Reports; the 18 unit-test classes sit in Tests/Modules/Reports/Domain, /Application or /Application/Metrics in the namespace of their folder
```

Invariant

Infrastructure

ReportsStringsTheMoveRenames

##### Treatment

Finite

###### Cases

```text
list Entries is empty
```

```text
both old production folders hold no moving file
```

```text
the old unit-test folder holds none of the 18 classes
```

```text
Finite list, finite folders and 18 named classes; native finite cases.
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
Every moving type is in its new namespace only, and every moved unit test is in its new test namespace only
```

```text
A type left or duplicated in the old namespace splits the module, and a rule or registration resolves the wrong one
```

ReportsMembership

```text
the membership map of 31 files and the fixed list of 18 test classes
```

move of each declaring file whole

ReportsLivesInItsModuleArchUnitTest and ReportsUnitTestsBesideTheModuleArchUnitTest

```text
IL namespace and folder per type; source folder per test class
```

```text
reflection by string type name and a stale using line; the compiler catches the using, and no non-C# file or reflection string names a moving type (measured)
```

### States

Admitted

```text
all 31 production files in the new folders with namespace equal to folder, old folders without a moving file, the 18 unit tests in their new folders, the rename list empty
```

ReportsMembership

Excluded

```text
a moving type in its old namespace, a moving file under the old folders, a moved type declared twice, an entry left in the rename list, a unit test left in Tests/Services/Implementation/Reports
```

ReportsMembership

Unknown

```text
string-built or reflection-read type names that no search finds; Program.cs registration order, seen only by the candidate review
```

crafter\, checked by the full filtered suite

### Models

None.

### Closure

```text
the 31 moving types and 18 unit tests in their new namespace and folder
```

Safety

Pass

DesignedNotBuilt

```text
Safety is exclusion of an old-namespace type: the B1 oracle reads the compiled IL and refuses any moving type in its old namespace or any file left under the old folders; the B2 oracle does the same for the test classes.
```

##### Evidence

None.

```text
each moving type and test class from the map to its new place
```

Inhabitation

Pass

DesignedNotBuilt

```text
Every file has an acyclic construction path: the crafter moves each declaring file whole, the compiler resolves it, and the oracle finds it in its new place.
```

##### Evidence

None.

```text
Reports acceptance, integration and wire-contract tests reach the moved types through the unchanged controllers and ports
```

PublicReachability

Pass

DesignedNotBuilt

```text
The public ports, controllers and Program.cs registrations are unchanged apart from using lines; the existing tests observe the same behaviour.
```

##### Evidence

None.

```text
namespace move
```

Liveness

Pass

Indeterminate

```text
Not applicable: no progress, scheduling or fairness claim; the move adds no state or concurrency.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
assumption: only the five listed literals, the 31 placement rows and the using lines name a moving type by old path; negated: another string names one
```

```text
all Reports behaviour and every architecture rule stay green
```

```text
none
```

```text
an unlisted string or rule left naming the old place silently
```

```text
an unlisted literal fails the full filtered suite or the Architecture namespace run, not a silent break
```

crafter

#### Y2

Stressor

```text
assumption: log categories are not keyed by a moving type; negated: a log override or ReportLoggingTest targets the old namespace
```

```text
log output content
```

```text
ILogger category string changes to the new namespace
```

```text
a lost log override or a log assertion that no longer finds its category
```

```text
none: no appsettings names a moving type; ReportLoggingTest moves with its types and runs in the full suite
```

crafter

#### Y3

Bypass

```text
second stressor set: a half-applied move, a late rebase, or a using of the old namespace added by another change
```

```text
build stays green
```

```text
none
```

```text
old namespace still compiling
```

```text
old namespaces no longer exist after the move, so a stale using fails the build
```

crafter

#### Y4

SinglePointOfFailure

```text
second stressor set: Program.cs is the one composition root and its registration order is seen by no public probe
```

```text
registration order, lifetimes and pairs
```

```text
using lines change
```

```text
reordered, re-scoped or re-paired registrations
```

```text
only the candidate review of the Program.cs diff observes order; the acceptance tests observe the resulting behaviour
```

candidate review

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportCapPolicy\.cs

Create

```text
new home of the Domain file, namespace Lighthouse.Backend.Modules.Reports.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportSeriesReading\.cs

Create

```text
new home of the Domain file, namespace Lighthouse.Backend.Modules.Reports.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportWindowPolicy\.cs

Create

```text
new home of the Domain file, namespace Lighthouse.Backend.Modules.Reports.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/PortfolioReportMetricSeries\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportBulkCommands\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportBulkRemover\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportCommands\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportMetricSeriesByOwner\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportOwnerLookup\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportQueries\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/TeamReportMetricSeries\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ThenAndNowTemplate\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportBulkCommands\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportBulkRemover\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportCommands\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportMetric\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportMetricSeries\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportOwnerLookup\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportOwnerWriteAccess\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportQueries\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportReader\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportTemplate\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportWriter\.cs

Create

```text
new home, whole, namespace Lighthouse.Backend.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ArrivalsReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/CycleTimeReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/DailySeriesSummary\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ReportMetricCatalog\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ThroughputReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WindowedReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WipReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WorkItemAgeReportMetric\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
the three Reports using lines follow the new namespaces; RegisterReports keeps its text, order, lifetimes and service and implementation pairs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/NewReportRequest\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs

Extend

```text
using line follows the moved types; behaviour, routes and DTOs unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamReportsControllerTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortFixtureLifetimesTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

Extend

```text
using line follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Extend

```text
the one rename-list literal is renamed to the new full name
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
the two rename-list literals (ReportCommands, ReportQueries) are renamed to the new full names
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
the rename-list literal and the global-qualified IReportQueries reference in a made-up case follow the new namespace
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
the one rename-list file-path literal is renamed to the new place
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Extend

```text
the 31 rows that only existed for the old Reports folders are removed
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs

Extend

```text
the rename list shrinks as entries are renamed or removed and ends empty; it is feature A's measurement made to shrink, so it is a target and not a support of the oracle
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCapPolicyTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/CycleTimeReportMetricTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/DailySeriesSummaryTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThroughputReportMetricTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/WorkItemAgeReportMetricTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/PortfolioReportMetricSeriesTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkRemoverTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwnerTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportOwnerLookupTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/TeamReportMetricSeriesTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateEditTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~ReportsLivesInItsModule
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

#### Public oracle

```text
Run ReportsLivesInItsModuleArchUnitTest through the NUnit runner (an architecture test over the compiled Lighthouse.Backend assembly and the source tree), reading ReportsMembership and ReportsStringsTheMoveRenames.Entries.
```

```text
Each of the 31 moving files declares its types in Lighthouse.Backend.Modules.Reports.Domain (3 files), .Application (9 implementation files and 11 port files) or .Application.Metrics (8 files) only, the file sits in Modules/Reports/<same layer folder>, no moving file remains under Services/Implementation/Reports or Services/Interfaces/Reports, the staying types (ReportRepository, Models/Reports, controllers, DTOs, ReportOwnerWriteAccess) are where the map lists them, and the rename list holds no entry; derived from the membership map, not from production code. Each type is looked up by name in the compiled assembly and its absence fails the test.
```

```text
Before the move every type is still in its old namespace and the list holds entries, so the test fails; a type left or copied in an old namespace, a file left in an old folder, a staying type moved, a moving type renamed or missing, or a leftover rename-list entry fails it.
```

##### Promises

P1

P2

P4

#### Prefactoring

```text
L1 move: relocate the 31 files whole into Modules/Reports, changing only the namespace line, using lines and Program.cs usings; makes feature C's narrowed Reports rules and boundary rules possible
```

```text
all Reports behaviour, the EF model, routes, DTOs, and every architecture rule and feature A's preservation law
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs\:1

Migrated

```text
The 12 implementation files and 8 metric files move whole to Modules/Reports/{Domain,Application,Application/Metrics}; the old folders end without a moving file.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportQueries\.cs\:1

Migrated

```text
The 11 port files move whole to Modules/Reports/Application; the old folder ends empty.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:32

Changed

```text
Three using lines change; RegisterReports (18 registrations) keeps text, order, lifetimes and pairs.
```

api\-consumers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsController\.cs\:1

Changed

```text
Using lines in the three controllers, ReportsControllerBase, ReportOwnerWriteAccess and NewReportRequest follow the new namespaces; routes, DTOs and headers are unchanged.
```

persistence\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs\:1

Changed

```text
ReportRepository stays; only its using lines follow the moved ports.
```

persistence

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/Report\.cs\:1

UnchangedCompatible

```text
Models/Reports stays; the EF model and migrations are unchanged.
```

tests\-and\-guards

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs\:1

Changed

```text
Five literals renamed, 31 PlatformLaneUnitTestPlacementTest rows removed, the list emptied; guard and acceptance tests change only using lines or the listed literals.
```

test\-consumers

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
Using lines of the 10 consuming test and fixture files and of the 18 unit tests follow the moved types; assertions unchanged.
```

frozen\-oracles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Feature A's membership test and preservation law are namespace-agnostic (old or new) and stay green without edits.
```

build\-and\-ci

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:1

UnchangedCompatible

```text
SDK-style globbing picks up the new folder; no suppression, analyser, budget, host-user, Stryker or script names a moving type (measured); the backend path filter already covers Modules.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportCapPolicy\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportSeriesReading\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportWindowPolicy\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/PortfolioReportMetricSeries\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportBulkCommands\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportBulkRemover\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportCommands\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportMetricSeriesByOwner\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportOwnerLookup\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportQueries\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/TeamReportMetricSeries\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ThenAndNowTemplate\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportBulkCommands\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportBulkRemover\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportCommands\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportMetricSeries\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportOwnerLookup\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportOwnerWriteAccess\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportQueries\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportReader\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportTemplate\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportWriter\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ArrivalsReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/CycleTimeReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/DailySeriesSummary\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ReportMetricCatalog\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/ThroughputReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WindowedReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WipReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/Metrics\/WorkItemAgeReportMetric\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/NewReportRequest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportsController\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportsController\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/ChangesDuringABulkRun\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamReportsControllerTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportsControllerTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortFixtureLifetimesTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

Changed

```text
Using line or listed literal follows the moved types; behaviour and assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCapPolicyTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/CycleTimeReportMetricTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/DailySeriesSummaryTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThroughputReportMetricTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/WorkItemAgeReportMetricTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/PortfolioReportMetricSeriesTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkRemoverTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwnerTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportOwnerLookupTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/TeamReportMetricSeriesTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateEditTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

##### Agreement

None.

##### Not applicable

reuse

```text
The 31 Create targets are whole-file moves of existing types; each is reused unchanged from its old path (reuse records name the old files that exist today), and no new owner or class is created. A reuse record cannot locate a file that does not exist yet.
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

###### T1

```text
Every moving type of ReportsMembership is in its new Modules.Reports namespace and folder only; no moving file under the old Services folders; staying types where the map lists them; rename list empty. Falsifies the placement law over the whole map, not a sample.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~ReportsLivesInItsModule
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Domain\/ReportWindowPolicyTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Domain\/ReportCapPolicyTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCapPolicyTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/ArrivalsReportMetricTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/CycleTimeReportMetricTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/CycleTimeReportMetricTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/DailySeriesSummaryTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/DailySeriesSummaryTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/ThroughputReportMetricTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThroughputReportMetricTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/WorkItemAgeReportMetricTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application.Metrics
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/WorkItemAgeReportMetricTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/PortfolioReportMetricSeriesTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/PortfolioReportMetricSeriesTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportBulkCommandsTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportBulkRemoverTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkRemoverTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportCommandsTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportLoggingTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportMetricSeriesByOwnerTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwnerTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportOwnerLookupTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportOwnerLookupTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportQueriesTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/TeamReportMetricSeriesTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/TeamReportMetricSeriesTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ThenAndNowTemplateEditTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateEditTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ThenAndNowTemplateTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Reports.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~ReportsUnitTestsBesideTheModule
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Modules.Reports
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

#### Public oracle

```text
Run ReportsUnitTestsBesideTheModuleArchUnitTest through the NUnit runner (an architecture test over the compiled test assembly and the source tree).
```

```text
ReportWindowPolicyTest and ReportCapPolicyTest are in Lighthouse.Backend.Tests.Modules.Reports.Domain; the 5 metric and series-summary tests in ...Reports.Application.Metrics; the other 11 (log-assertion, bulk and the rest) in ...Reports.Application; each class sits in the folder of its namespace, none is left under Tests/Services/Implementation/Reports, and the Reports port, host and wire-contract tests and ReportRepositoryTest are where they were. Each class is looked up by name; absence fails the test.
```

```text
Before the move all 18 classes are in Tests.Services.Implementation.Reports and the test fails; a class left in the old folder, put in the wrong layer, duplicated, renamed or missing, or a staying test moved, fails it.
```

##### Promises

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

test\-files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCommandsTest\.cs\:1

Migrated

```text
The 18 unit-test files move whole to Tests/Modules/Reports/{Domain,Application,Application/Metrics}; the old folder ends empty.
```

test\-project\-discovery

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:1

UnchangedCompatible

```text
SDK-style globbing and NUnit discovery pick up the new folders; no budget, Stryker or script names a moved test file (measured).
```

staying\-reports\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ReportRepositoryTest\.cs\:1

UnchangedCompatible

```text
ReportRepositoryTest and the port, host and wire-contract tests stay; their using lines already changed in the production move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportLoggingTest\.cs\:1

Migrated

```text
New home of the log-assertion test with the other Application tests, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Domain\/ReportWindowPolicyTest\.cs\:1

Migrated

```text
New home in Domain; keeps its using of Tests.API.Integration.Reports.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Domain\/ReportCapPolicyTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportCapPolicyTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/ArrivalsReportMetricTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/CycleTimeReportMetricTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/CycleTimeReportMetricTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/DailySeriesSummaryTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/DailySeriesSummaryTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/ThroughputReportMetricTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThroughputReportMetricTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/Metrics\/WorkItemAgeReportMetricTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/WorkItemAgeReportMetricTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/PortfolioReportMetricSeriesTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/PortfolioReportMetricSeriesTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportBulkCommandsTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportBulkRemoverTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkRemoverTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportCommandsTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportMetricSeriesByOwnerTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwnerTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportOwnerLookupTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportOwnerLookupTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ReportQueriesTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/TeamReportMetricSeriesTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/TeamReportMetricSeriesTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ThenAndNowTemplateEditTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateEditTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

moved\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Reports\/Application\/ThenAndNowTemplateTest\.cs\:1

Migrated

```text
New home of a moved file, namespace equal to folder.
```

moved\-test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs\:1

Migrated

```text
Old location of a unit test, deleted by the move.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The slice is itself a behaviour-free move of test files; no move precedes it and no behaviour changes, so there is no separate preservation step.
```

reuse

```text
The 18 Create targets are whole-file moves of existing test classes, reused unchanged from their old paths (reuse records name the old files); a reuse record cannot locate a file that does not exist yet.
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

###### T2

```text
Each of the 18 Reports unit-test classes is in its Tests.Modules.Reports layer namespace and the folder of that namespace; none under the old test folder; the staying Reports tests are where they were.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~ReportsUnitTestsBesideTheModule
```

##### Contract tests

None.

## ADRs

None.
