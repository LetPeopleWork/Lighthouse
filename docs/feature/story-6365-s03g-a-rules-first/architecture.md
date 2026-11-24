# Architecture brief: story\-6365\-s03g\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Carriers and owners, all test side in Lighthouse.Backend.Tests.Architecture (object_oriented): RefinementMembership (S1 support) holds the moving Refinement types, each with its short name, its layer from the closed choice RefinementLayer {Domain, Application}, its old namespace and its new namespace Lighthouse.Backend.Modules.Refinement.<Layer>; the Refinement types and files that stay, each with namespace or path, owner and reason; and the targets found naming a moving type, each with file, target and kind (suppression, analyser, CI selection, test budget, host user, Stryker). NamespaceMoveLaw (S2 target, created by the S2 prefactoring) owns the preservation law over any move map and pattern-role map, and owns PatternSubject, PatternRole and PatternDeclaration. RefinementPatternRoles and RefinementStringsTheMoveRenames are S2 supports. No production carrier, effect, error type or per-process state changes; the only effects are reading the compiled production and test assemblies and repository text files; every failure is an NUnit assertion message stating WHAT, WHY and HOW.
```

#### D2

```text
Claim-to-checker: the membership and the preservation law are relations over a finite, closed domain (the compiled types of the production assembly, the const and static readonly string fields of the test assembly, two subjects per type, the string literals of the test project). They are executed exhaustively by native NUnit over the real IL, which is stronger than a model of them. No Quint, TLA+ or Agda model: no state, transition, ordering, concurrency, time or unbounded input is claimed. Property-based testing does not apply: closed finite domain, every case enumerated. Layers affected: only the test-side architecture rules (Infrastructure layer of the test project); Domain, Application and Adapter layers of production are unchanged.
```

#### D3

```text
Shared law: LIFT. PlatformLaneRulesSeeTheMoveArchUnitTest is platform-specific: it reads PlatformLaneMembership.MovingTypes and StringsTheMoveRenames directly and hard-codes ThePlatformNamespace, the Platform alternative token and MovingAreasOfServices, so no map can be passed in. S2 therefore starts with a refactor(architecture) commit that lifts the law into NamespaceMoveLaw, a class constructed from a move map (moving types with area, old and new namespace and full name; the strings the move renames; the new root namespace and its alternative token) and a pattern-role map; PatternSubject, PatternRole and PatternDeclaration move from PlatformLanePatternRoles into it (one owner). The platform test keeps its test methods, made-up fixtures and verdicts and calls NamespaceMoveLaw with the platform map; the Refinement test calls it with the Refinement map. Rejected: a Refinement copy of the law (two owners deciding the same rule) and one fixture type per pattern (covers only the patterns someone remembered).
```

#### D4

```text
P2 same answer is the role law of the lifted S03f law: every regex field that sees a moving Refinement type under its old subject is declared in RefinementPatternRoles with its subject (Namespace or FullName) and role. A Context pattern (a part of the code a rule protects or forbids, such as RefinementPattern, RefinementReportsAccessAndLicensing, the Refinement per-type patterns and ServicesImplementationPattern read as a service) must give the same answer under the new subject, widened by one anchored alternative for Modules.Refinement or its layer namespaces. A Place pattern, or a pattern that saw a Refinement type only through a Services folder and is declared folder-only, must not see the type in Modules.Refinement. An undeclared pattern that sees a moving type fails. Candidates measured by text (noisy): ModuleBoundariesArchUnitTest.RefinementPattern, TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing, RefinementModuleArchUnitTest constants, KernelRules.TheServices, ThePlatform and RefinementAndReports of Access, Licensing and Promises boundaries, and the WhereTheTypesLivedBefore old-location patterns; the IL-backed law names the definitive set.
```

#### D5

```text
Exact-namespace uses rewritten over the membership map in RefinementModuleArchUnitTest: ReadPath selects every type whose namespace is a Refinement Domain or Application namespace of the map (old or new) minus the write path; the stage-rule-matcher rule excludes Refinement types by RefinementPattern instead of DoNotResideInNamespace(RefinementImplementationNamespace); the need-calculator assertion accepts the old or new namespace the map gives RefinementNeedCalculator; reaches-no-service is ServicesImplementationPattern union the Refinement Domain and Application namespaces minus the type itself. RefinementImplementationNamespace is then removed, not renamed. BackgroundUpdatesPattern keeps its name and value (read by member name through reflection).
```

#### D6

```text
Re-measurement on the worktree HEAD d35488500 by text (noisy): the old-path literals in the test project are AccessModuleBoundariesArchUnitTest line 107 (VoterIdentityResolver full name), CategoryListOwnershipGuardTest line 20 and RepositoryLeakRatchetArchUnitTest line 46 (RefinementList.cs path), TrackerIntegrationModuleBoundariesArchUnitTest line 445 (IRefinementViewQuery full name), RefinementModuleArchUnitTest lines 31, 33, 37, 39, and, new against the DESIGN record, 16 rows of PlatformLaneUnitTestPlacementTest.ProductionFilesThatStay (lines 65-77 and 159-161) naming every moving Refinement file with owner Refinement. Those 16 rows go on the rename list as removed by feature B. The global:: reference at TrackerIntegrationModuleBoundariesArchUnitTest line 870 is compile-checked, not a literal. code-fact answered through textsearch at noisy confidence with no C# atoms; facts here come from git grep and reading; the IL measurement of A1 decides.
```

#### D7

```text
Per-file layer rule (constraint K4) replaces the per-type reading of D-1. Rejected: a per-type rule, which would put the 3 ports, their 21 records and enums, RefinementResolution, StageMatches and the Voter* types in Domain and split port files and other files in feature B; and a one-way-only rule, which lets a kernel-only pure type sit in Application unnoticed. If only one Domain file results, the lookalike-or-namesake falsifier uses a made-up Domain fixture instead of a second real Domain type.
```

### Constraints

#### K1

```text
P4: no production file, migration, Data, Models, frontend or E2E file changes in this feature; the existing backend suite stays green.
```

#### K2

```text
Each slice has its own oracle file; a later slice never edits an earlier slice oracle or support. RefinementMembership.cs is the S1 support and is read, never edited, by S2.
```

#### K3

```text
Membership domain is closed and finite: every top-level type the IL finds in Lighthouse.Backend.Services.Implementation.Refinement, Lighthouse.Backend.Services.Interfaces.Refinement and Lighthouse.Backend.Modules.Refinement (nested types follow their outermost type). Measured from the IL at 93fddc5c9: 41 moving top-level types, 17 in Services.Implementation.Refinement (13 files, plus StageMatches, VoterResolution, VoterKeyResolution and VoterRefusal) and 24 in Services.Interfaces.Refinement (3 ports plus 21 records and enums); 31 staying types, 16 of them in Models/Refinement across 9 files (8 in RefinementSettings.cs); no suppression, analyser, CI, budget, host-user or Stryker target names a moving type. No count bound is fixed by authority; a later difference is recorded, not argued with.
```

#### K4

```text
Layer rule, decided per file (Confirmed, AFK recommended option in line with D-1): a move never splits a file. A file is Domain if and only if it is not a port file and every top-level type it declares has IL edges only to the kernel (Lighthouse.Backend.Models), the BCL and types of other Domain files. A port file declares an I-prefixed port interface; it stays in Application with every record and enum it declares, recorded as a declared fact on the membership map, not inferred. Every other file is Application. The Domain rule is one-way (R-R2): a Domain type reaches only the kernel, the BCL and other Domain types. The oracle also checks the reverse per non-port file: a kernel-only non-port file listed as Application fails. Whatever the IL gives as Domain is the answer (open question 4); SizingRefusal reaches Microsoft.Extensions.Logging, so it is Application; NeedBand is expected Domain, with any other kernel-only non-port file.
```

#### K5

```text
The rename list is fixed at the end of S2 from the pre-move search and only shrinks: feature B removes an entry when it renames or deletes its literal, feature C removes the widened old-namespace alternatives. Feature B and C work (move, renames, narrowing, R-R1 to R-R6, placement test, architecture documents) is out of scope.
```

#### K6

```text
Test-writing guidance in .claude/skills/test-writing/SKILL.md and references/backend.md wins over nWave timing and PBT density rules. No nWave scenario tags in code. Comments are rare, plain, and never cite internal ids as the explanation.
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
Compiled production and test assemblies read in process through LighthouseArchitecture.Production (ArchUnitNET) and IlDependencies.ReadModule (Mono.Cecil); nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Architecture
```

#### Driven ports

```text
Repository text files read from the repository root: test project .cs sources, GlobalSuppressions.cs, both .editorconfig, any .globalconfig, Directory.Build.props, BannedSymbols files, Scripts/test-selection, Scripts/test-timings/test-time-budget.json, TestHostUseGuardTest.HostUsersStillToMove.cs, stryker*.json
```

```text
Architecture tests depend on the production assembly and on repository files; nothing in production depends on them; S2 oracle and rule files depend on the S1 support RefinementMembership and on NamespaceMoveLaw.
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
Each moving Refinement type found exactly once, in its old or its new namespace
```

RefinementMembershipArchUnitTest

Excluded

```text
A rule pattern that sees a moving type only under its old namespace (rule goes blind after the move)
```

RefinementRulesSeeTheMoveArchUnitTest

Excluded

```text
An old-path literal in the test project that is not on the rename list, or a listed literal that no longer exists
```

RefinementRulesSeeTheMoveArchUnitTest

Unknown

```text
Strings built at run time from fragments that never appear whole as a literal or constant
```

RefinementRulesSeeTheMoveArchUnitTest

### Models

None.

### Closure

```text
Refinement rules see the move
```

Safety

Pass

DesignedNotBuilt

```text
A rule that stops seeing a moving type, an undeclared pattern, an alternative reaching outside Modules.Refinement, an unlisted old-path literal or a wrong membership entry each fails an exhaustive NUnit check over the IL.
```

##### Evidence

None.

```text
Refinement rules see the move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today the Refinement types exist in the old namespaces, so the map and every Context pattern have at least one type to answer for; the read path and reaches-no-service selections stay non-empty.
```

##### Evidence

None.

```text
Refinement rules see the move
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly -> RefinementMembership and NamespaceMoveLaw -> NUnit architecture tests -> dotnet test exit code and assertion message.
```

##### Evidence

None.

```text
Refinement rules see the move
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
Assumption: rules read constants. Negated: exact-namespace equality, DoNotResideInNamespace and an exact-namespace assertion use a plain string
```

```text
Read path, matcher rule and need-calculator verdicts
```

```text
Rewrite over the membership map
```

```text
A rule silently checking an empty or partial set after the move
```

```text
Literal sweep fails while RefinementImplementationNamespace exists
```

RefinementRulesSeeTheMoveArchUnitTest

#### Y2

Stressor

```text
Assumption: the DESIGN text counts still hold after S03e and S03f. Negated: counts moved
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

RefinementMembershipArchUnitTest

#### Y3

Stressor

```text
Assumption: a suppression, analyser, CI, budget, host-user or Stryker target follows a move. Negated: it names a type by its old name
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

RefinementMembershipArchUnitTest

#### Y4

Bypass

```text
Reflection reads a rule member by name (ForecastingRulesSeeTheModuleArchUnitTest reads BackgroundUpdatesPattern)
```

```text
The member name and value
```

```text
None
```

```text
Renaming BackgroundUpdatesPattern
```

```text
Guarded by the reflecting test itself, which fails on a missing member
```

RefinementModuleArchUnitTest

#### Y5

SinglePointOfFailure

```text
NamespaceMoveLaw is the one owner of the preservation law for Platform and Refinement
```

```text
Platform verdicts unchanged by the lift
```

```text
Shared fixture of made-up patterns
```

```text
A defect in the law hiding a break for both maps
```

```text
The made-up pattern fixtures of the platform test falsify each break kind; the Refinement test adds its own made-up breaks
```

NamespaceMoveLaw

#### Y6

Stressor

```text
Second set, walked over the finished design (walkthrough, not execution): a test class name listed in the budget or host-user files; a composite pattern built by string interpolation from RefinementPattern; a Refinement nested type under the ArchUnit + separator
```

```text
Budget and host-user entries name classes that stay; composites are declared Composite of their component; nested types follow their outermost type on the Namespace subject and the per-type patterns keep ($|\+.*)
```

```text
Empty target list
```

```text
A composite read on a different subject than its component
```

```text
None found
```

NamespaceMoveLaw

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~RefinementMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

#### Public oracle

```text
The architecture tests run the Refinement membership check over the compiled production IL: each type listed in RefinementMembership with its file, its layer, its port-file flag, old namespace and new Lighthouse.Backend.Modules.Refinement.<Layer> namespace, compared with the top-level types the IL finds in Services.Implementation.Refinement, Services.Interfaces.Refinement and Modules.Refinement; each listed staying type or file looked up by namespace or path with its owner and reason; the IL edges of every type of every non-port file; and the suppression, analyser, CI-selection, test-budget, host-user and Stryker files read for targets naming a moving type.
```

```text
On today code the check passes: the 41 listed moving types equal the IL set both ways, each found exactly once in its old or its new namespace; the 31 staying entries exist where listed (sizing-log repository pair, the Models/Refinement types, the API controllers, validator and DTOs, WeeklyRecurrence); every type of a Domain-listed file reaches only the kernel, the BCL and types of other Domain files; no kernel-only non-port file is listed as Application; port files and all their types are Application; the listed targets equal those found (none). A failure names each missing listed type, each listed name that does not exist, each unlisted type, each Domain edge beyond the kernel, each kernel-only non-port file listed as Application and each unlisted target, with WHAT, WHY and HOW.
```

```text
Written before the list is filled the check fails naming the unlisted Refinement types (RED). One entry removed, a made-up entry added, a staying name that does not exist, SizingRefusal listed as Domain (reaches Microsoft.Extensions.Logging), NeedBand listed as Application (kernel-only non-port file), a port file split across layers, a made-up Domain fixture type lookalike or namesake of a Domain type, or a made-up target naming a moving type each fails and is named. A check that compares one way only, judges layers per type, counts entries, reads only the old or only the new namespace, or matches a short name without its namespace passes those cases and is rejected.
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

PlatformLaneMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:19

Reuse

```text
Same shape (map compared both ways with the IL, old-or-new lookup, staying entries with owner, targets naming moving types); copied as a pattern, not shared code, because each module map is its own fact set.
```

ReadModule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:21

Reuse

```text
Existing IL reader for type enumeration and edges; no new reader.
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
The listed Refinement membership equals the set of top-level types the IL finds in the old Refinement namespaces and in Lighthouse.Backend.Modules.Refinement, both ways, each listed name found exactly once in its old or its new namespace; layers are per file: a file is Domain if and only if it is not a declared port file and every top-level type it declares has IL edges only to Lighthouse.Backend.Models, the BCL and types of other Domain files, every other file (port files with all their records and enums included) is Application; each staying entry exists at its listed place; the listed targets naming a moving type equal those found.
```

Invariant

Infrastructure

RefinementMembershipArchUnitTest

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
An IL type in a Refinement namespace not listed
```

```text
A Domain-listed file with a type reaching beyond the kernel (SizingRefusal to Microsoft.Extensions.Logging)
```

```text
A kernel-only non-port file listed as Application (NeedBand)
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
The Refinement membership is measured from the IL, namespace-agnostic, with layer, old and new namespace, staying entries with owner and reason, the kernel-only Domain rule and the recorded targets.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~RefinementMembershipArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs

Create

```text
The lifted shared preservation law and its pattern subject, role and declaration types (prefactoring).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

Extend

```text
Calls NamespaceMoveLaw with the platform map after the lift (prefactoring).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs

Extend

```text
Keeps only the platform declarations; the shared types move to NamespaceMoveLaw (prefactoring).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementPattern widened by an anchored Modules.Refinement alternative; every reader follows.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementReportsAccessAndLicensing widened by an anchored Modules.Refinement alternative.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Per-type nested patterns widened to the new full names; ServicesImplementationPattern as a service widened; exact-namespace uses rewritten over RefinementMembership; RefinementImplementationNamespace removed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Extend

```text
Only if the law declares TheServices a Context pattern that must keep seeing Refinement application services.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Only for a Context pattern (ThePlatform, RefinementAndReports) the law names; the VoterIdentityResolver full name stays on the rename list for feature B.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Only for a Context pattern (ThePlatform, RefinementAndReports) the law names.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Only for a Context pattern (ThePlatform) the law names.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter "FullyQualifiedName~Lighthouse.Backend.Tests.Architecture&FullyQualifiedName~Refinement"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs

#### Public oracle

```text
The architecture tests ask NamespaceMoveLaw, built from RefinementMembership and RefinementPatternRoles, about every const and static readonly string field of the test assembly that parses as a regex and names the backend: each is asked on its declared subject (namespace of the outermost type, or full name) under the old and the new name of every moving Refinement type; each Modules.Refinement alternative is asked about every production namespace and full name outside Modules.Refinement; and the test project .cs sources are read as text for string literals matching Services.Implementation.Refinement, Services.Interfaces.Refinement or Services/(Implementation|Interfaces)/Refinement, compared with RefinementStringsTheMoveRenames.
```

```text
After the crafter widens the Refinement rule constants and rewrites the exact-namespace uses, no pattern breaks its role (Context patterns answer the same under old and new, Place and folder-only patterns do not see the type in Modules.Refinement, no undeclared pattern sees a moving type), no Modules.Refinement alternative matches anything outside Modules.Refinement, the old-path literals equal the rename list both ways, and the whole Architecture namespace and the backend suite stay green. Made-up patterns and literals in the oracle fixture are each named with their break kind.
```

```text
Written before the widening, the law fails naming RefinementPattern, RefinementReportsAccessAndLicensing and the per-type patterns as stopping to see the type, and the sweep fails naming RefinementImplementationNamespace as an unlisted literal (RED). A made-up pattern that sees only the old place, an unanchored Modules.Refinement alternative reaching a lookalike namespace, a listed literal removed from the sources, or an unlisted literal added each fails and is named. A law that asks a namespace pattern about full names, skips static readonly or nested-class fields, or compares the literal list one way only passes those cases and is rejected.
```

##### Promises

P2

P3

P4

#### Prefactoring

```text
Lift the preservation law out of PlatformLaneRulesSeeTheMoveArchUnitTest into a new NamespaceMoveLaw class constructed from a move map and a pattern-role map; move PatternSubject, PatternRole and PatternDeclaration from PlatformLanePatternRoles into NamespaceMoveLaw.cs; the platform test calls the lifted law with the platform map. One refactor(architecture) commit before the S2 oracle is written. It makes the Refinement law a second call of the same owner instead of a copy.
```

```text
Every test of PlatformLaneRulesSeeTheMoveArchUnitTest keeps its name, made-up fixtures and verdict (all pass before and after), and the Architecture namespace stays green. The platform test file bytes change because the law lived inside it; its assertions and expected break lists do not.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

```text
Whole-parent patterns (Confirmed, AFK root decision): RefinementPatternRoles carries a short declared list of patterns that cover the whole destination parent. Today the list holds only PlatformLaneMembershipArchUnitTest.ModuleNamespaces, which means every module where a domain-event handler may live. The S2 oracle accepts new sight of the moved types for a listed pattern only when it matches the parent namespace Lighthouse.Backend.Modules itself and the new namespace of every moving type. An unlisted pattern that newly sees the moved types still breaks, and a listed pattern that matches less than the whole parent fails. NamespaceMoveLaw and PlatformLaneMembershipArchUnitTest stay unchanged and are not targets. Rejected: a new role and API in the shared law for one pattern; making PlatformLaneMembershipArchUnitTest a target, because its meaning is right; an unchecked exclusion, because it could hide a real break.
```

##### Constraints

None.

##### Reuse

PlatformLaneRulesSeeTheMoveArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:35

Extend

```text
Owner of the preservation law; platform-specific, so its law is lifted into NamespaceMoveLaw and both maps call it.
```

PatternDeclaration

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:28

Extend

```text
Subject, role and composite declaration reused unchanged; moved to NamespaceMoveLaw as their one owner.
```

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:280

CreateNew

```text
No class owns the law apart from the platform test; created by lifting WhenEachPatternIsAskedOnItsSubjectAboutEveryMovingType and the alternative and literal checks, not by writing a second law.
```

RefinementModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:22

Extend

```text
Refinement own guards keep every assertion; only constants and exact-namespace uses change.
```

RefinementPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:23

Extend

```text
One owner of the Refinement module pattern; widening it widens every reader (Flow metrics, Forecasting, Work Items boundaries, RefinementModuleArchUnitTest, non-empty guard).
```

RefinementReportsAccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Extend

```text
Its OtherModules part names no Refinement module, so this constant alone keeps Tracker integration away from Refinement; widened by one anchored Modules.Refinement alternative.
```

TheServices

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:18

Extend

```text
Existing kernel rule pattern; widened only if the law declares it Context for Refinement application services.
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Context pattern for the Refinement and Reports contexts Access must not reach; widened by an anchored Modules.Refinement alternative. ThePlatform at line 58 is declared folder-only or Context by the law.
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Same pattern for Licensing; widened the same way. ThePlatform at line 58 is declared by the law.
```

ThePlatform

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:56

Extend

```text
Sees Refinement only through the Services folder; the law declares it folder-only (must not follow) or Context, and the crafter changes it only if declared Context.
```

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:1

CreateNew

```text
No class owns the preservation law apart from the platform test (PlatformLaneRulesSeeTheMoveArchUnitTest.cs line 280, WhenEachPatternIsAskedOnItsSubjectAboutEveryMovingType); created by lifting that law, not by writing a second one.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:23

Changed

```text
RefinementPattern old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Changed

```text
RefinementReportsAccessAndLicensing old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:31

Changed

```text
Constants widened; exact-namespace uses rewritten over the map.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:35

Changed

```text
Delegates to NamespaceMoveLaw; verdicts unchanged.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:1

Changed

```text
Shared types moved out; platform declarations unchanged.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:51

UnchangedCompatible

```text
Reads RefinementPattern; follows the widening with no edit.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:136

UnchangedCompatible

```text
Reads RefinementPattern; follows the widening.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:163

UnchangedCompatible

```text
Reads RefinementPattern; follows the widening.
```

test\-rule\-reader

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:130

UnchangedCompatible

```text
Reads BackgroundUpdatesPattern by member name; name and value kept.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:65

UnchangedCompatible

```text
Old-path rows go on the rename list; feature B removes them.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs\:20

UnchangedCompatible

```text
On the rename list; feature B renames.
```

test\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:46

UnchangedCompatible

```text
On the rename list; feature B renames.
```

build

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs\:1

UnchangedCompatible

```text
No Refinement target today; S1 records the re-search.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:18

Changed

```text
Changed only if declared Context for Refinement; else unchanged with a folder-only declaration.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
RefinementAndReports old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
RefinementAndReports old union new.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:56

Changed

```text
Changed only if ThePlatform is declared Context for Refinement; else unchanged with a folder-only declaration.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:1

Changed

```text
New shared law; consumed by the platform and Refinement preservation tests.
```

##### Agreement

None.

##### Not applicable

None.

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
For every const or static readonly string field P of the test assembly that parses as a regex and is not the text of a rename-list entry, and every moving Refinement type T of RefinementMembership, with s the subject RefinementPatternRoles declares for P: if IsMatch(P, s_old(T)) then P is declared, a Context P requires IsMatch(P, s_new(T)) and a Place or folder-only P requires not IsMatch(P, s_new(T)); if not IsMatch(P, s_old(T)) then IsMatch(P, s_new(T)) is admitted only for a Context P that saw another moving type under its old subject; an undeclared P matching either subject of any T fails; every Modules.Refinement alternative of P matches no production namespace (Namespace) or full name (FullName) outside Lighthouse.Backend.Modules.Refinement; the set of (file, literal) pairs in the test project naming the old Refinement path equals RefinementStringsTheMoveRenames both ways.
```

Invariant

Infrastructure

RefinementRulesSeeTheMoveArchUnitTest

###### Treatment

Finite

###### Cases

```text
Context pattern seeing only the old place
```

```text
Context pattern widened by an anchored Modules.Refinement alternative
```

```text
Place pattern that would follow the type into Modules.Refinement
```

```text
Folder-only match that follows the type
```

```text
Undeclared pattern seeing a moving type
```

```text
Composite pattern read through its component
```

```text
Unanchored Modules.Refinement alternative reaching a lookalike namespace
```

```text
Listed literal that no longer exists
```

```text
Unlisted old-path literal (RefinementImplementationNamespace)
```

```text
Exact-namespace read path still selecting NeedBand after a Domain split
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
Every rule pattern keeps its role for each moving Refinement type through the move, no added alternative reaches outside Modules.Refinement, and no old-path literal exists outside the fixed rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~RefinementRulesSeeTheMoveArchUnitTest
```

###### T3

```text
The lifted law keeps every platform verdict.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
