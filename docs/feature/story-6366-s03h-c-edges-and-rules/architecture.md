# Architecture brief: story\-6366\-s03h\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Test side only, behaviour-free. No production file, no Program.cs, no configuration key changes. Carrier owners: rule constants stay on their existing rule classes (ModuleBoundariesArchUnitTest, TrackerIntegrationModuleBoundariesArchUnitTest, Access and Licensing module boundaries tests); the module rule shapes stay on ModuleRules; Reports module boundaries get one new NUnit fixture ReportsModuleBoundariesArchUnitTest that holds its inline lists by full type name. Procedural-style static rule helpers are the repository convention for architecture tests; no new class is invented beyond the fixture and its made-up offender types.
```

#### D2

```text
Claim-to-checker: every claim here is a finite set comparison over the compiled IL (listed types equal measured types, listed edges equal measured edges, both ways) or a text sweep. Quint, TLA+ and Agda are not applicable: there is no state machine, no transition, no temporal or fairness claim and no unbounded input, so a model would restate the set comparison. The executable checker is the NUnit oracle itself on the compiled IL, with a made-up offender per rule and per list position as falsifier. Liveness: not applicable, nothing runs over time. Types: Mono.Cecil IL reading gives string full names, so a list entry is a string key, not a type; the made-up offenders prove each key position is live. Property testing: not applicable, the domains are finite and enumerated.
```

#### D3

```text
Measured baselines, re-measured from today's compiled IL (Release-equivalent IlDependencies read): Domain 3 types, Application 25 top-level types, Application.Metrics 8 types, 36 in all, ReportRepository the only Reports type left under Services. Domain reaches only Models (kernel) and System: R-RP2 exception list empty. Outside reaches Domain: 0 types, R-RP3 list empty. Outside reaches Application: API controllers and Program (allowed by role) plus ReportRepository: R-RP4 list holds 1. Application reaches Data, API, module Adapters, Tracker integration, RBAC, update code: none. EF Core: ReportBulkCommands -> DbUpdateException, ReportBulkRemover -> DbUpdateException: R-RP5 list holds 2. R-RP6 list holds 12 edges: DailySeriesSummary -> FlowMetrics.Domain.PercentileCalculator; ReportCommands and ReportQueries -> Licensing.Application.ILicenseService; ReportOwnerLookup, TeamReportMetricSeries and PortfolioReportMetricSeries -> Services.Interfaces.Repositories.IRepository1; six types (PortfolioReportMetricSeries, ReportBulkCommands, ReportCommands, ReportQueries, TeamReportMetricSeries, ThenAndNowTemplate) -> Platform.Host.ILighthouseClock. Flow metrics Application edges (TeamReportMetricSeries -> ITeamMetricsService, PortfolioReportMetricSeries -> IPortfolioMetricsService) are legal conformist edges and not listed.
```

#### D4

```text
Difference from the DESIGN: the IL read yields 2 EF Core edges, not 3. ReportBulkRemover reaches DbUpdateConcurrencyException only in a catch clause, which the IL reader does not see (known reader limit); its DbUpdateException edge is seen. The list holds the 2 measured edges and the rule is compared both ways on them. The Application type count is 25 types (DESIGN counted 20 files). The oracle must re-measure and the IL decides; the lists only shrink.
```

#### D5

```text
Old-path sweep law (S1): no rule constant and no string literal in the test project matches the old Reports namespaces or paths in any form (dotted, regex alternative, group member, slash path), with an EMPTY allowance list. Exact-path exclusions only: feature A measurement files that must spell old names (ReportsMembership.cs, ReportsMembershipArchUnitTest.cs, and any fixture they use), the sweep oracle itself (builds old names from segments), and RefinementRulesNameOnlyTheModuleArchUnitTest.cs, whose frozen made-up offender names Reports as a group member of the old Services alternation. Feature A's three helper files ReportsRulesSeeTheMoveArchUnitTest.cs, ReportsPatternRoles.cs and ReportsStringsTheMoveRenames.cs assert old-or-new sight and an old-path rename list; after the move they are settled and removed by the crafter, as feature C of the previous package did, and ReportsLivesInItsModuleArchUnitTest drops its rename-list tests. Every other Reports rule keeps its verdict.
```

### Constraints

#### K1

```text
No rule or list is loosened: lists are inline by full type name, compared both ways, only shrink. Rules read the compiled IL, never an empty set: each rule asserts its subject set is not empty. Outer types only for set comparison (strip the part after plus). A rule exemption is exact per type, never a namespace or layer. Failures use Reports own wording (ModuleWording Reports, the API). No nWave scenario tags in code. No edit to an approved oracle of this feature after approval; one oracle file per slice.
```

#### K2

```text
Input bounds: no stored or validated input is introduced. The rule input is the compiled production assembly; its size is the 36 Reports types and the edges measured above, with no declared upper bound, so the lists are exact sets and a new edge fails by design rather than being clipped. Test budgets: default per-test budget, the IL read is shared per fixture (Lazy ModuleDefinition disposed in OneTimeTearDown); no derogation requested.
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
Single deployable and a single test assembly; producers and consumers of the rule constants change in the same commit, so the primary oracle covers them.
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow the Reports rule constant to Modules.Reports only (and drop the dead Auth, Authorization and Licensing alternatives in the joint Tracker integration pattern)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow the Reports rule constant to Modules.Reports only (and drop the dead Auth, Authorization and Licensing alternatives in the joint Tracker integration pattern)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow the Reports rule constant to Modules.Reports only (and drop the dead Auth, Authorization and Licensing alternatives in the joint Tracker integration pattern)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow the Reports rule constant to Modules.Reports only (and drop the dead Auth, Authorization and Licensing alternatives in the joint Tracker integration pattern)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs

Extend

```text
Settled by the move: the file asserts old-or-new sight or an old-path rename list and is removed, as the previous package did
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsPatternRoles\.cs

Extend

```text
Settled by the move: the file asserts old-or-new sight or an old-path rename list and is removed, as the previous package did
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs

Extend

```text
Settled by the move: the file asserts old-or-new sight or an old-path rename list and is removed, as the previous package did
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

Extend

```text
Drop the rename-list tests that read the removed rename list; keep the layout assertions
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
Only if it names a removed support type; every assertion stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesNameOnlyTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsRulesNameOnlyTheModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run ReportsRulesNameOnlyTheModuleArchUnitTest through dotnet test: it sweeps every rule constant of the test assembly and every string literal of the test sources for the old Reports forms (dotted name, regex alternative, group member, slash folder path) with an empty allowance list and exact-path exclusions, matches each narrowed Reports alternative against the types of the module, and runs the sweep over made-up offenders in each form.
```

```text
No constant and no literal outside the exact exclusions names Reports by a former Services namespace or path; each narrowed alternative still matches a Reports module type; the joint Tracker integration pattern no longer names Reports, Auth, Authorization or Licensing under Services; each made-up former form is named and a Modules-only twin is not.
```

```text
Today the old-namespace alternatives in ReportsPattern, RefinementReportsAccessAndLicensing and the Access and Licensing RefinementAndReports patterns still match, so the sweep fails and names each; a rule narrowed in its constant but kept as a literal, or a narrowed alternative anchored to no type, is named by the made-up offenders.
```

##### Promises

P1

P3

#### Prefactoring

```text
Narrow the Reports rule constants and settle feature A helper files (behaviour and production untouched); makes the new module rule set of S2 the only Reports layout guard
```

```text
Every Reports rule keeps its verdict on today's code; ReportsModuleArchUnitTest keeps every assertion
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleArchUnitTest
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

RefinementRulesNameOnlyTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs\:1

Reuse

```text
Same law shape (constants plus literals sweep, both-ways exclusion list, narrowed-alternative liveness, made-up offenders); modelled on it, not shared, because the module name and exclusions differ
```

ReportsPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Extend

```text
Single owner of the Reports namespace pattern read by ReportsModuleArchUnitTest and three interpolated readers; narrowing here reaches every reader
```

RefinementReportsAccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Extend

```text
Joint pattern owner for Tracker integration; drop Reports and the dead Auth, Authorization, Licensing alternatives
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Access pattern names Reports by the old alternative too; same constant also in Licensing tests, narrowed in each owner
```

ReportsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs\:1

Reuse

```text
Measured map of the 36 Reports types by layer; stays as the measurement owner and old-name exclusion
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Owner of its Reports pattern constant; narrowed here
```

ReportsRulesSeeTheMoveArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:1

Replace

```text
Old-or-new sight law is settled by the move; removed
```

ReportsPatternRoles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsPatternRoles\.cs\:1

Replace

```text
Role map for the old-or-new sight law; removed with it
```

ReportsStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs\:1

Replace

```text
Old-path rename list ends empty after the move; removed
```

ReportsLivesInItsModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs\:1

Extend

```text
Layout assertions stay; rename-list tests dropped
```

ReportsModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Extend

```text
Edited only if it names a removed support type; every assertion stays
```

##### Impact

architecture\-rule\-constant

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Changed

```text
ReportsPattern narrowed to Modules.Reports
```

architecture\-rule\-constant

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Changed

```text
Reports and dead alternatives removed
```

architecture\-rule\-constant

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
Reports named by Modules.Reports only
```

architecture\-rule\-constant

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
Reports named by Modules.Reports only
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:1

Changed

```text
Removed once settled by the move, together with ReportsPatternRoles.cs and ReportsStringsTheMoveRenames.cs
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs\:75

Changed

```text
Rename-list tests dropped with the list
```

architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

UnchangedCompatible

```text
Reads ReportsPattern only; every assertion keeps its verdict on the narrowed pattern
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production file, DI registration or configuration key changes
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsPatternRoles\.cs\:1

Changed

```text
Removed with the old-or-new sight law
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs\:1

Changed

```text
Removed once the rename list is empty
```

##### Agreement

Reports namespace pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Producer

Migrated

```text
Narrowed to Modules.Reports
```

Reports namespace pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Reads the constant; verdict unchanged on today's code because every Reports type is already under Modules.Reports
```

Reports namespace pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Reads the constant; verdict unchanged on today's code because every Reports type is already under Modules.Reports
```

Reports namespace pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Reads the constant; verdict unchanged on today's code because every Reports type is already under Modules.Reports
```

Reports namespace pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Reads the constant; verdict unchanged on today's code because every Reports type is already under Modules.Reports
```

##### Not applicable

None.

##### Boundaries

```text
dotnet test on the NUnit fixture ReportsRulesNameOnlyTheModuleArchUnitTest; reads the test assembly constants by reflection and test sources as text
```

###### Driven ports

None.

```text
tests read production IL and test sources; no production code is touched
```

###### Failures

None.

##### Observations

```text
Names of rule constants and literals that still match an old Reports form, plus narrowed alternatives that match no module type
```

string list

```text
A form written in a way the four forms do not cover; the oracle names the covered forms
```

##### Equality

exclusion list

```text
Listed exclusion paths equal the files that must spell an old form, both ways; a listed file with no old form is named stale
```

##### Constructors

None.

##### Laws

###### L2

```text
For every rule constant and string literal outside the exact exclusions, no old Reports form matches; every Modules.Reports alternative matches a module type
```

Invariant

Infrastructure

ReportsRulesNameOnlyTheModuleArchUnitTest

###### Treatment

Finite

###### Cases

```text
dotted name offender
```

```text
regex alternative offender
```

```text
group member offender
```

```text
slash path offender
```

```text
Modules-only twin passes
```

```text
closed finite domain of four forms
```

###### Evidence

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

```text
Old-path sweep
```

Safety

Pass

DesignedNotBuilt

```text
Sweep with empty allowance list over constants and literals; made-up offenders per form
```

###### Evidence

None.

```text
Narrowed Reports alternatives
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each Modules.Reports alternative matches a measured type of the 36
```

###### Evidence

None.

```text
Sweep through dotnet test
```

PublicReachability

Pass

DesignedNotBuilt

```text
NUnit class drives the real rule constants and test sources: input constants -> sweep -> failure naming the offender
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Rule constants and literals name Reports by Modules.Reports only; no Reports rule is dead
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesNameOnlyTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsRulesNameOnlyTheModuleArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleBoundariesArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run ReportsModuleBoundariesArchUnitTest through dotnet test: R-RP1 to R-RP6 read the compiled IL with ModuleRules and compare the measured types and edges both ways with the inline lists, then run the same rules over made-up offender types in a fixture namespace (Lighthouse.Backend.Modules.ReportsFixtures).
```

```text
Each listed type lives in its layer with none under Services; Domain reaches only the kernel and System; outside reaches Domain only from API, the composition root and Reports Application (empty list) and Application only from API, the composition root and ReportRepository; Application reaches no Data, API, module Adapters, Tracker integration, RBAC or update code and EF Core only along the 2 listed edges; Application reaches Flow metrics Domain, Licensing, Organisation set-up data and platform infrastructure only along the 12 listed edges. Compliant twins pass.
```

```text
A made-up misplaced type, unlisted layer type, Domain type reaching a service, outside type reaching Domain or Application, Application type reaching forbidden code or an unlisted EF Core, Flow metrics Domain, Licensing, set-up or platform type, an entry deleted from a list and a listed edge that no longer exists are each named in Reports wording; an empty subject set fails.
```

##### Promises

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

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:150

Reuse

```text
TypesLiveInTheirLayer, DomainReachesOnlyTheKernelAndTheBaseLibrary, DomainIsReachedFromOutsideOnlyBy, ApplicationIsReachedFromOutsideOnlyBy and OutboundEdges already carry the six rule shapes with parameterised namespaces and wording
```

RefinementModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:40

Reuse

```text
Same module rule set shape; modelled on it with Reports lists, no shared base class because lists and wording are per module
```

ReportsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs\:1

Reuse

```text
Measured layer map of the 36 Reports types for cross-checking; the new oracle still carries its own inline lists by full type name
```

ReportsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

CreateNew

```text
No existing fixture owns Reports module layer and edge lists; ReportsModuleArchUnitTest holds design-law guards by type identity, a different responsibility
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
S2 adds one test file only; no production or existing test file changes (oracle is the new rule file, no targets)
```

agreement

```text
No shared contract is touched; the new fixture reads ModuleRules without changing it
```

prefactoring

```text
No code target: S1 already narrowed the constants and nothing is moved in S2
```

##### Boundaries

```text
dotnet test on the NUnit fixture ReportsModuleBoundariesArchUnitTest reading the production and test assemblies IL through ModuleRules
```

###### Driven ports

None.

```text
test reads production IL; no production change
```

###### Failures

None.

##### Observations

```text
Offending types and edges named in Reports wording, or the full list entries to delete
```

string list

```text
Catch-clause, event and attribute-named-argument references, which the IL reader misses (ReportBulkRemover catch of DbUpdateConcurrencyException)
```

##### Equality

list comparison

```text
A listed set equals the measured set of full outer type names or Dependent -> Target edges, both ways; nested types reduce to the outer type
```

##### Constructors

None.

##### Laws

###### L1

```text
Each of R-RP1 to R-RP6 passes on today's IL with the inline lists and fails on a made-up offender per rule and per list position
```

Invariant

Application

ReportsModuleBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
layer membership offender
```

```text
Domain reaching a service
```

```text
outside type reaching Domain
```

```text
outside type reaching Application off the list
```

```text
Application reaching forbidden code
```

```text
unlisted EF Core, Flow metrics Domain, Licensing, set-up or platform edge
```

```text
deleted and stale list entries
```

```text
closed finite domain enumerated from the IL
```

###### Evidence

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

```text
R-RP1 to R-RP6 rules
```

Safety

Pass

DesignedNotBuilt

```text
ModuleRules on the IL; both-ways lists; non-empty subject asserted; made-up offender per rule
```

###### Evidence

None.

```text
Measured lists
```

Inhabitation

Pass

DesignedNotBuilt

```text
Lists measured from today's IL: 36 types, 1 outside Application caller, 2 EF edges, 12 edges
```

###### Evidence

None.

```text
Rules through dotnet test
```

PublicReachability

Pass

DesignedNotBuilt

```text
NUnit fixture drives ModuleRules over the real assembly and made-up fixtures: IL -> rule -> verdict naming the edge
```

###### Evidence

None.

##### Risks

###### Y1

Bypass

```text
IL reader misses catch-clause, event and attribute-argument references
```

```text
Listed edges equal IL-visible edges
```

```text
A catch-only reference such as DbUpdateConcurrencyException stays unlisted
```

```text
Claiming a rule sees catch-only references
```

```text
Catch-only edges invisible to R-RP5
```

ReportsModuleBoundariesArchUnitTest

##### Architectural tests

###### T1

```text
R-RP1 to R-RP6: Reports layers, kernel-only Domain, inbound and outbound edge lists compared both ways with made-up offenders
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleBoundariesArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
