# Architecture brief: story\-6366\-s03h\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Carriers and owners, all test side in Lighthouse.Backend.Tests.Architecture (object_oriented): ReportsMembership (S1 support) holds the moving Reports types, each with its short name, its file, its layer from the closed choice ReportsLayer {Domain, Application, ApplicationMetrics}, its port-file flag, its old namespace and its new namespace Lighthouse.Backend.Modules.Reports.Domain, .Application or .Application.Metrics; the Reports types and files that stay (ReportRepository, the Models/Reports types, the three Reports controllers, ReportsControllerBase, ReportOwnerWriteAccess and the four API/DTO/Reports requests), each with namespace or path, owner and reason; and the targets found naming a moving type, each with file, target and kind (suppression, analyser, CI selection, test budget, host user, Stryker, log override). ReportsPatternRoles and ReportsStringsTheMoveRenames are S2 supports that feed the existing shared NamespaceMoveLaw (NamespaceMove, PatternRoleMap, PatternDeclaration, RoleBreak). No production carrier, effect, error type or per-process state changes; the only effects are reading the compiled production and test assemblies and repository text files; every failure is an NUnit assertion message stating WHAT, WHY and HOW.
```

#### D2

```text
Claim-to-checker: the membership and the preservation law are relations over a finite, closed domain (the compiled types of the production assembly, the const and static readonly string fields of the test assembly, two subjects per type, the string literals of the test project). They are executed exhaustively by native NUnit over the real IL. No Quint, TLA+ or Agda model: no state, transition, ordering, concurrency, time or unbounded input is claimed, so no checker install or waiver is needed. Property-based testing does not apply: closed finite domain, every case enumerated. Layers affected: only the test-side architecture rules (Infrastructure layer of the test project); production Domain, Application and Adapter layers are unchanged.
```

#### D3

```text
Shared law: REUSE. NamespaceMoveLaw already takes a move map and a pattern-role map (lifted by the Refinement packaging), so Reports adds its own map and calls the same law; no second law and no edit to NamespaceMoveLaw. The open renames of RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation and the PlatformLanePatternRoles reason text stay open: this feature does not edit those files. Rejected: a Reports copy of the law (two owners of one rule) and one fixture per pattern (covers only remembered patterns).
```

#### D4

```text
Layer rule, decided per file, never per type (a move never splits a file): a file is Domain if and only if it is not a port file and every top-level type it declares has IL edges only to the kernel (Lighthouse.Backend.Models and its sub-namespaces), the BCL and types of other Reports Domain files. A port file declares an I-prefixed port interface and stays in Application with every record and enum it declares; the flag is a declared fact on the map. A non-Domain file under Services/Implementation/Reports/Metrics is ApplicationMetrics (the grouping is kept); every other non-Domain file is Application. The check runs both ways: a Domain-listed file reaching beyond the kernel fails, and a kernel-only non-port file listed as Application or ApplicationMetrics fails. Expected Domain: ReportWindowPolicy, ReportCapPolicy, ReportSeriesReading; DailySeriesSummary reaches the Flow metrics Domain and stays ApplicationMetrics. If the IL makes a Metrics file kernel-only, the crafter returns it to the architect instead of choosing a layer.
```

#### D5

```text
Same answer (P2) is the role law of NamespaceMoveLaw: every regex field that sees a moving Reports type under its old subject is declared in ReportsPatternRoles with its subject (Namespace or FullName) and role. A Context pattern must give the same answer under the new subject, widened by one anchored alternative ^Lighthouse\.Backend\.Modules\.Reports(\.|$); a Place or folder-only pattern must not see the type in Modules.Reports; an undeclared pattern that sees a moving type fails; no Modules.Reports alternative matches a production type outside Modules.Reports. Candidates measured by text on HEAD e5bbe3fed (noisy; the IL-backed law names the definitive set): ModuleBoundariesArchUnitTest.ReportsPattern (line 25; read by ReportsModuleArchUnitTest, ModuleBoundariesArchUnitTest line 67 and the interpolated readers FlowMetricsInboundApplicationArchUnitTest 51, ForecastingModuleBoundariesArchUnitTest 139, WorkItemsModuleBoundariesArchUnitTest 169), TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing (line 60, alternation form), and, new against the DESIGN text, AccessModuleBoundariesArchUnitTest.RefinementAndReports and LicensingModuleBoundariesArchUnitTest.RefinementAndReports (line 65 each). ReportsModuleArchUnitTest ReportModelsPattern, RepositoryPortPattern, ApiLayerPattern and CompositionRootPattern name staying code and stay unchanged.
```

#### D6

```text
Re-measurement on HEAD e5bbe3fed by text (noisy; the A1 IL measurement decides): the 31 moving files hold as designed (12 in Services/Implementation/Reports, 8 in its Metrics, 11 in Services/Interfaces/Reports) with 36 top-level type declarations (port files IReportCommands, IReportMetricSeries and IReportTemplate declare 2 types, IReportOwnerLookup 3); 18 unit tests under Tests/Services/Implementation/Reports; 7 Models/Reports files; 9 API files. Old-path literals in the test project, all on the S2 rename list: RepositoryLeakRatchetArchUnitTest 47, FlowMetricsInboundDomainAndAdaptersArchUnitTest 47, TrackerIntegrationModuleBoundariesArchUnitTest 446 (DESIGN said 427), and, new against the DESIGN text, LicensingModuleBoundariesArchUnitTest 111 and 112 (ReportCommands, ReportQueries full names) and 31 rows of PlatformLaneUnitTestPlacementTest (lines 65 to 84 and 146 to 156) naming every moving Reports file with owner Reports. The global:: reference at TrackerIntegrationModuleBoundariesArchUnitTest 883 is compile-checked, not a literal. No backend suppression, analyser, build, Stryker, CI-selection, test-budget or log-override target names a Reports type. code-fact was not used; facts come from git grep and reading.
```

### Constraints

#### K1

```text
P4: no production file, migration, Data, Models, frontend or E2E file changes in this feature; the existing backend suite stays green.
```

#### K2

```text
Each slice has its own oracle file; a later slice never edits an earlier slice oracle or support. ReportsMembership.cs is the S1 support and is read, never edited, by S2.
```

#### K3

```text
Membership domain is closed and finite: every top-level type the IL finds in Lighthouse.Backend.Services.Implementation.Reports (and .Metrics), Lighthouse.Backend.Services.Interfaces.Reports and Lighthouse.Backend.Modules.Reports; nested types follow their outermost type (IlDependencies.TypesIn returns Outer+Inner, reduced to the part before +). No count bound is fixed by authority; a measured difference is recorded, not argued with.
```

#### K4

```text
The rename list is fixed at the end of S2 from the pre-move search and only shrinks: feature B removes an entry when it renames or deletes its literal, feature C removes the widened old-namespace alternatives. Feature B and C work (move, renames, narrowing, R-RP1 to R-RP6, placement test, architecture documents) is out of scope; ReportRepository and Models/Reports stay where they are.
```

#### K5

```text
Test-writing guidance in .claude/skills/test-writing/SKILL.md and references/backend.md wins over nWave timing and PBT density rules. No nWave scenario tags in code. Comments are rare, plain, and never cite internal ids. Architecture rules assert by the failure naming the offender, not by counts; made-up offenders live in a fixture namespace such as Lighthouse.Backend.Modules.ReportsFixtures.
```

#### K6

```text
Start condition as measured on HEAD e5bbe3fed (host records S03e and S03f Closed, S03g Resolved and pushed as bd31ac293, started on the maintainer word). The DESIGN git check (git log --grep story <id> on Tests/Architecture) finds no commit for any of the three, because rules commits carry a scope, not the story id; the last rules commits by scope are cc3d7def2 test(access), 7d4883cfe test(platform) and d8391783c test(refinement), each an ancestor of HEAD. Content: RefinementPattern names only Modules.Refinement; no Services/Interfaces/ILighthouseClock.cs; no file left under Services/(Implementation|Interfaces)/(Auth|Authorization|Licensing), but RefinementReportsAccessAndLicensing still carries the Auth, Authorization and Licensing alternatives under Services next to Reports (they match no type). That leftover is not changed by this feature.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes; only test-side rule code in one test project changes.
```

contract_tests

```text
Single deployable; producer and consumer of every touched constant live in the same test assembly and change in the same commit.
```

### Boundaries

```text
Compiled production and test assemblies read in process through LighthouseArchitecture.Production (ArchUnitNET) and IlDependencies.ReadModule (Mono.Cecil); nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Driven ports

```text
Repository text files read from the repository root: test project .cs sources, GlobalSuppressions.cs, both .editorconfig, any .globalconfig, Directory.Build.props, BannedSymbols files, Scripts/test-selection, Scripts/test-timings/test-time-budget.json, TestHostUseGuardTest.HostUsersStillToMove, backend stryker*.json, appsettings*.json and the E2E perf log overrides
```

```text
Architecture tests depend on the production assembly and on repository files; nothing in production depends on them; the S2 oracle and rule files depend on the S1 support ReportsMembership and on NamespaceMoveLaw.
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

Admitted

```text
Each moving Reports type found exactly once, in its old or its new namespace
```

ReportsMembershipArchUnitTest

Excluded

```text
A rule pattern that sees a moving Reports type only under its old namespace (rule goes blind after the move)
```

ReportsRulesSeeTheMoveArchUnitTest

Excluded

```text
An old-path Reports literal in the test project that is not on the rename list, or a listed literal that no longer exists
```

ReportsRulesSeeTheMoveArchUnitTest

Unknown

```text
Strings built at run time from fragments that never appear whole as a literal or constant
```

ReportsRulesSeeTheMoveArchUnitTest

### Models

None.

### Closure

```text
Reports rules see the move
```

Safety

Pass

DesignedNotBuilt

```text
A rule that stops seeing a moving type, an undeclared pattern, an alternative reaching outside Modules.Reports, an unlisted old-path literal or a wrong membership entry each fails an exhaustive NUnit check over the IL.
```

##### Evidence

None.

```text
Reports rules see the move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today the Reports types exist in the old namespaces, so the map and every Context pattern have at least one type to answer for.
```

##### Evidence

None.

```text
Reports rules see the move
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly -> ReportsMembership and NamespaceMoveLaw -> NUnit architecture tests -> dotnet test exit code and assertion message.
```

##### Evidence

None.

```text
Reports rules see the move
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no eventual-progress claim; each check is a terminating finite enumeration with a 1 s regex timeout per match.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: a text sweep finds every pattern. Negated: the alternation form (Reports inside (Refinement|Reports|...)) and the interpolated non-const readers
```

```text
Every regex field is asked by behaviour (IsMatch on old and new subjects), not by text
```

```text
Interpolated readers follow the widened owner with no edit
```

```text
A rule silently checking an empty or partial set after the move
```

```text
Strings composed only at run time (Unknown state)
```

ReportsRulesSeeTheMoveArchUnitTest

#### Y2

Stressor

```text
Assumption: the DESIGN text counts still hold after S03e, S03f and S03g. Negated: counts moved
```

```text
Membership equals the IL set both ways
```

```text
Recorded difference
```

```text
Map copied from the DESIGN text
```

```text
None: the IL decides
```

ReportsMembershipArchUnitTest

#### Y3

Stressor

```text
Assumption: a suppression, analyser, CI, budget, host-user, Stryker or log-override target follows a move. Negated: it names a type by its old name
```

```text
Targets naming a moving type are listed
```

```text
Empty list
```

```text
An unlisted target naming a moving type
```

```text
Files outside the read set (archived docs) are not searched
```

ReportsMembershipArchUnitTest

#### Y4

Stressor

```text
Assumption: only using lines move with files. Negated: same-namespace resolution breaks on the Domain split
```

```text
Each Application file naming a Domain type is visible as an IL edge in the membership data
```

```text
Feature B adds using lines
```

```text
An edge read from using lines instead of the IL
```

```text
IL misses catch-clause, event and attribute-named-argument references
```

ReportsMembershipArchUnitTest

#### Y5

SinglePointOfFailure

```text
NamespaceMoveLaw is the one owner of the preservation law for Platform and Reports
```

```text
Platform verdicts unchanged
```

```text
Shared fixture of made-up patterns
```

```text
A defect in the law hiding a break for both maps
```

```text
The platform made-up pattern fixtures falsify each break kind; the Reports test adds its own made-up breaks
```

NamespaceMoveLaw

#### Y6

Stressor

```text
Second set, walked over the finished design (walkthrough, not execution): a test class name in the budget or host-user lists; ReportOwnerWriteAccess drifting into the module with its port; ThenAndNowPayload moving and the stored JSON changing; a Reports nested type under the ArchUnit + separator
```

```text
Budget and host-user entries name classes that stay; ReportOwnerWriteAccess is pinned to API by an existing rule; Models/Reports stays and System.Text.Json stores no type name; nested types follow their outermost type
```

```text
Empty target list
```

```text
A staying type listed as moving
```

```text
None found
```

ReportsRulesSeeTheMoveArchUnitTest

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ReportsMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

#### Public oracle

```text
The architecture tests run the Reports membership check over the compiled production IL: each type listed in ReportsMembership with its file, its layer, its port-file flag, old namespace and new Lighthouse.Backend.Modules.Reports.<Layer> namespace, compared with the top-level types the IL finds in Services.Implementation.Reports (and .Metrics), Services.Interfaces.Reports and Modules.Reports; each listed staying type or file looked up by namespace or path with its owner and reason; the IL edges of every type of every non-port file; and the suppression, analyser, CI-selection, test-budget, host-user, Stryker and log-override files read for targets naming a moving type.
```

```text
On today code the check passes: the listed moving types equal the IL set both ways, each found exactly once in its old or its new namespace; the staying entries exist where listed (ReportRepository, the Models/Reports types, the three controllers, ReportsControllerBase, ReportOwnerWriteAccess, the four DTOs); every type of a Domain-listed file reaches only the kernel, the BCL and types of other Domain files; no kernel-only non-port file is listed as Application or ApplicationMetrics; port files and all their types are Application; the listed targets equal those found. A failure names each missing listed type, each listed name that does not exist, each unlisted type, each Domain edge beyond the kernel, each kernel-only non-port file listed outside Domain and each unlisted target, with WHAT, WHY and HOW.
```

```text
Written before the list is filled the check fails naming the unlisted Reports types (RED). One entry removed, a made-up entry added, a staying name that does not exist, DailySeriesSummary listed as Domain (reaches the Flow metrics Domain), ReportCapPolicy listed as Application (kernel-only non-port file), a port file split across layers, a made-up Domain fixture type lookalike or namesake of a Domain type, or a made-up target naming a moving type each fails and is named. A check that compares one way only, judges layers per type, counts entries, reads only the old or only the new namespace, or matches a short name without its namespace passes those cases and is rejected.
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

RefinementMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:18

Reuse

```text
Same shape (map compared both ways with the IL, old-or-new lookup, per-file layers, staying entries with owner, targets naming moving types); copied as a pattern, not shared code, because each module map is its own fact set.
```

ReadModule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:21

Reuse

```text
Existing IL reader for type enumeration and edges; no new reader.
```

DomainReachesOnlyTheKernelAndTheBaseLibrary

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:176

Reuse

```text
Existing kernel-only edge rule; the membership check reuses its notion of kernel and base library.
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
No crafter target: the acceptance designer authors the oracle and its membership support; no production, rule or configuration file changes.
```

prefactoring

```text
No code target; nothing to move.
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
The listed Reports membership equals the set of top-level types the IL finds in the old Reports namespaces and in Lighthouse.Backend.Modules.Reports, both ways, each listed name found exactly once in its old or its new namespace; layers are per file: a file is Domain if and only if it is not a declared port file and every top-level type it declares has IL edges only to Lighthouse.Backend.Models, the BCL and types of other Domain files; other Metrics files are ApplicationMetrics and every other file (port files with all their records included) is Application; each staying entry exists at its listed place; the listed targets naming a moving type equal those found.
```

Invariant

Infrastructure

ReportsMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
Every moving type listed and found in the old namespace
```

```text
A listed type missing from the IL
```

```text
A listed name that exists in neither namespace
```

```text
An IL type in a Reports namespace not listed
```

```text
A Domain-listed file with a type reaching beyond the kernel (DailySeriesSummary to the Flow metrics Domain)
```

```text
A kernel-only non-port file listed outside Domain (ReportCapPolicy)
```

```text
A port file or its records split across layers
```

```text
A made-up Domain fixture lookalike or namesake of a Domain type
```

```text
A staying entry not found at its listed place
```

```text
A target naming a moving type not listed
```

```text
Closed finite set of compiled types and files; each case is enumerated, no generator adds coverage.
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

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
The Reports membership is measured from the IL, namespace-agnostic, with layer, old and new namespace, staying entries with owner and reason, the kernel-only Domain rule both ways and the recorded targets.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ReportsMembershipArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
ReportsPattern widened by an anchored Modules.Reports alternative; every reader follows.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementReportsAccessAndLicensing widened by an anchored Modules.Reports alternative.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementAndReports widened by an anchored Modules.Reports alternative, if the law names it Context.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementAndReports widened by an anchored Modules.Reports alternative, if the law names it Context; the ReportCommands and ReportQueries literals stay on the rename list for feature B.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ReportsRulesSeeTheMoveArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsStringsTheMoveRenames\.cs

#### Public oracle

```text
The architecture tests ask NamespaceMoveLaw, built from ReportsMembership and ReportsPatternRoles, about every const and static readonly string field of the test assembly that parses as a regex and names the backend: each is asked on its declared subject (namespace of the outermost type, or full name) under the old and the new name of every moving Reports type; each Modules.Reports alternative is asked about every production namespace and full name outside Modules.Reports; and the test project .cs sources are read as text for string literals matching Services.Implementation.Reports, Services.Interfaces.Reports or Services/(Implementation|Interfaces)/Reports, or an alternation group naming Reports under Services.(Implementation|Interfaces), compared with ReportsStringsTheMoveRenames.
```

```text
After the crafter widens the Reports rule constants, no pattern breaks its role (Context patterns answer the same under old and new, Place and folder-only patterns do not see the type in Modules.Reports, no undeclared pattern sees a moving type), no Modules.Reports alternative matches anything outside Modules.Reports, the old-path literals equal the rename list both ways, and the whole Architecture namespace and the backend suite stay green. Made-up patterns and literals in the oracle fixture are each named with their break kind.
```

```text
Written before the widening, the law fails naming ReportsPattern and RefinementReportsAccessAndLicensing (and any other Context pattern the IL finds, such as RefinementAndReports) as stopping to see the type (RED). A made-up pattern that sees only the old place, an unanchored Modules.Reports alternative reaching a lookalike namespace, a listed literal removed from the sources, or an unlisted literal added each fails and is named. A law that asks a namespace pattern about full names, skips static readonly or nested-class fields, misses the alternation form, or compares the literal list one way only passes those cases and is rejected.
```

##### Promises

P2

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

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

Reuse

```text
Shared owner of the preservation law, constructed from a NamespaceMove and a PatternRoleMap; Reports passes its own map; no edit.
```

ReportsPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Extend

```text
One owner of the Reports module pattern; widening it widens every reader (ReportsModuleArchUnitTest, the Flow metrics, Forecasting and Work Items interpolated readers, the non-empty guard).
```

RefinementReportsAccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Extend

```text
Names Reports inside an alternation; widened by one anchored Modules.Reports alternative.
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Names the old Reports namespaces; widened if the law declares it Context.
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Names the old Reports namespaces; widened if the law declares it Context.
```

ReportsModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:68

Reuse

```text
Keys on typeof and ReportsPattern; follows the widening with no edit.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:25

Changed

```text
ReportsPattern old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Changed

```text
RefinementReportsAccessAndLicensing old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
RefinementAndReports old union new, if declared Context.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
RefinementAndReports old union new, if declared Context.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:51

UnchangedCompatible

```text
Reads ReportsPattern; follows the widening.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:139

UnchangedCompatible

```text
Reads ReportsPattern; follows the widening.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:169

UnchangedCompatible

```text
Reads ReportsPattern; follows the widening.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:68

UnchangedCompatible

```text
Reads ReportsPattern; follows the widening.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:65

UnchangedCompatible

```text
The 31 Reports rows go on the rename list; feature B renames them.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:111

UnchangedCompatible

```text
On the rename list; feature B renames.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:47

UnchangedCompatible

```text
On the rename list; feature B renames.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:47

UnchangedCompatible

```text
On the rename list; feature B renames.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:446

UnchangedCompatible

```text
On the rename list; feature B renames.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

UnchangedCompatible

```text
Called with the Reports map; not edited.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The shared law already takes a map, so no lift or move precedes the oracle; the widening itself is the behaviour-preserving step before feature B.
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

###### L2

```text
For every const or static readonly string field P of the test assembly that parses as a regex and is not the text of a rename-list entry, and every moving Reports type T of ReportsMembership, with s the subject ReportsPatternRoles declares for P: if IsMatch(P, s_old(T)) then P is declared, a Context P requires IsMatch(P, s_new(T)) and a Place or folder-only P requires not IsMatch(P, s_new(T)); an undeclared P matching either subject of any T fails; every Modules.Reports alternative of P matches no production namespace or full name outside Lighthouse.Backend.Modules.Reports; the set of (file, literal) pairs in the test project naming the old Reports path, including an alternation group naming Reports under Services, equals ReportsStringsTheMoveRenames both ways.
```

Invariant

Infrastructure

ReportsRulesSeeTheMoveArchUnitTest

###### Treatment

Finite

###### Cases

```text
Context pattern seeing only the old place (ReportsPattern)
```

```text
Alternation-form Context pattern (RefinementReportsAccessAndLicensing)
```

```text
Context pattern widened by an anchored Modules.Reports alternative
```

```text
Place pattern that would follow the type into Modules.Reports
```

```text
Folder-only match that follows the type
```

```text
Undeclared pattern seeing a moving type
```

```text
Interpolated reader composed from ReportsPattern
```

```text
Unanchored Modules.Reports alternative reaching a lookalike namespace
```

```text
Listed literal that no longer exists
```

```text
Unlisted old-path literal
```

```text
Exhaustive over the finite domain (moving types x regex fields x two subjects, plus the test project literals); no generator adds coverage.
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

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Every rule pattern keeps its role for each moving Reports type through the move, no added alternative reaches outside Modules.Reports, and no old-path literal exists outside the fixed rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ReportsRulesSeeTheMoveArchUnitTest
```

###### T3

```text
The shared law keeps every platform verdict.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
