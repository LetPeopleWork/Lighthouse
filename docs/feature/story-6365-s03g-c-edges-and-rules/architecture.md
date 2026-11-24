# Architecture brief: story\-6365\-s03g\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Question 1 (preservation law): retire feature A's Refinement call of the move law. Delete RefinementRulesSeeTheMoveArchUnitTest, RefinementPatternRoles and RefinementStringsTheMoveRenames, and drop the rename-list use in RefinementLivesInItsModuleArchUnitTest. Reason: the move is complete and feature B pins every type to its new namespace, so a law that compares old and new sight has no old side left, and a Refinement call kept as a move-complete check would be a second owner next to the C1 oracle. NamespaceMoveLaw, PatternDeclaration and PatternRoleMap stay: PlatformLaneRulesSeeTheMoveArchUnitTest still uses them.
```

#### D2

```text
Question 2 (C1 oracle): one new file RefinementRulesNameOnlyTheModuleArchUnitTest owns the whole check. It reads every rule constant of the test assembly by reflection and sweeps every string literal of the test project sources for the four old forms (the old Implementation and Interfaces Refinement namespaces, as a plain name, a regex alternative or a group member, and the two old folder paths). A hit is allowed only in a declared measurement file (RefinementMembership, RefinementMembershipArchUnitTest, RefinementLivesInItsModuleArchUnitTest, RefinementUnitTestsBesideTheModuleArchUnitTest, NamespaceMoveLaw and the oracle itself); that list is compared both ways, so a listed file with no hit fails until it is removed. It also asserts that each Refinement rule pattern still matches a Refinement type in its new namespace, so narrowing cannot make a rule blind. Made-up offenders are fixtures declared to the oracle.
```

#### D3

```text
Question 3 (C2): RefinementModuleBoundariesArchUnitTest on ModuleRules, modelled on PromisesModuleBoundariesArchUnitTest and LicensingModuleBoundariesArchUnitTest, with ModuleWording Refinement and the API. R-R1 TypesLiveInTheirLayer per layer (Domain, Application) with the former home set to the whole Services pattern; R-R2 DomainReachesOnlyTheKernelAndTheBaseLibrary; R-R3 DomainIsReachedFromOutsideOnlyBy with an empty list and legal callers by namespace (API, composition root, Refinement Application); R-R4 ApplicationIsReachedFromOutsideOnlyBy with an empty list and legal callers API and the composition root; R-R5 Application outbound forbidden targets (EF Core, Data, API, module Adapters, Tracker integration, update code) with an empty list; R-R6 OutboundEdges into Access, Organisation set-up, the Platform lane and repositories along an inline list measured from the IL by the acceptance designer. Expected list from the design record: VoterIdentityResolver to Access, RefinementCalendar to the blackout service and the clock, SleYardstickResolver to the clock, RefinementCadenceCalendar to WeeklyRecurrence, RefinementList to IWorkItemRepository, RefinementViewQuery and SizingLogCommands to ISizingLogRepository; the IL measurement wins over this expectation. Every list holds full type names, is compared both ways and only shrinks. Falsifiers: per rule and per list position a made-up type in a fixture namespace, in the style of ModuleRulesCatchANewOffenderArchUnitTest; each failure names the edge in Refinement wording.
```

#### D4

```text
Question 4 (RefinementModuleArchUnitTest): it stays, with its PortFileTypes exclusion. The new set covers layers and edges only. It does not hold the static-ness of the settings validator, resolution, cadence arithmetic and need band, their Services-Implementation reach, the read path calling no writing repository member, the append-only sizing log, the votes controller or the migration cascades. C1 narrows only its three per-type patterns (resolution, cadence calendar, need band) to Modules.Refinement. The exclusion stays because the types under test and the port types share one Application namespace.
```

#### D5

```text
Claim to checker: every claim is a finite set comparison over compiled IL or test sources, checked by native NUnit architecture tests through the real ModuleRules port. No state machine, temporal, fairness or proof claim exists, so Quint, TLA+/TLC and Agda are not applicable and no model file is delivered. Liveness is not applicable: the checks are synchronous and terminate. Carriers: the existing InboundVerdict, OutboundVerdict, KernelRuleVerdict and IlDependent records; no new carrier. Paradigm object_oriented with the repository's static rule classes; no domain object is added. Property testing: the lists are finite, so native table cases are used, not generators. code-fact was not used; callers were found by text search and the impact is closed by reading the test project.
```

### Constraints

#### K1

```text
Test side only: no production file, no Program.cs, no configuration key changes. No rule or list is loosened; every rule keeps its verdict on today's code. Lists are inline full type names, measured from the compiled IL, compared both ways. New files carry no constant or literal spelling the old Refinement paths, and no rule-pattern constant over Refinement types that the C1 oracle does not declare. Comments cite no internal ids. Default test budgets apply, no derogation is requested; the filtered full backend suite runs as a background job.
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
NUnit architecture tests in Lighthouse.Backend.Tests/Architecture that call ModuleRules and IlDependencies over the compiled Lighthouse.Backend assembly and read the test project sources
```

#### Driven ports

None.

```text
Test code reads production IL only; production never references the test project; the new rule file depends on ModuleRules, ModuleWording and IlDependencies
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

```text
Refinement rule set and narrowed rule constants
```

Safety

Pass

DesignedNotBuilt

```text
A made-up offender per rule and list position is refused by the same ModuleRules verdicts the real code passes; baselines come from the IL
```

##### Evidence

None.

```text
Refinement rule set and narrowed rule constants
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today's Refinement code passes every rule: Domain holds NeedBand, Application holds the 15 files, and the only outside caller of Domain is the votes controller in API
```

##### Evidence

None.

```text
Refinement rule set and narrowed rule constants
```

PublicReachability

Pass

DesignedNotBuilt

```text
dotnet test over the Architecture namespace reaches both oracles through NUnit discovery; the rules read the compiled assembly through ModuleRules
```

##### Evidence

None.

```text
Refinement rule set and narrowed rule constants
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: the checks are synchronous finite comparisons with no progress or fairness claim
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
A type is added to Refinement under a layer folder the rules do not name, or a new edge is hidden behind a lookalike namespace
```

```text
Every Refinement type is listed in a layer and every edge is on a measured list
```

```text
A list entry deleted when its edge disappears
```

```text
A rule or list loosened; a Refinement rule that matches nothing
```

```text
A type outside the listed layers is caught by the membership check of feature B, not by C; reflection and runtime-loaded types are invisible to IL reading
```

Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

#### Y2

Stressor

```text
Assumption: narrowing a pattern to Modules.Refinement keeps it seeing every Refinement type. Negated: a narrowed pattern is anchored wrong and matches nothing
```

```text
Each rule keeps its verdict on today's code
```

```text
none
```

```text
A rule that passes because its pattern matches no type
```

```text
The C1 oracle asserts every narrowed Refinement pattern still matches a Refinement type in its new namespace
```

Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs

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
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs

Extend

```text
Narrow its Refinement pattern or literal to Modules.Refinement only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs

Extend

```text
Retire: the file is deleted, its law has no old side left
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementPatternRoles\.cs

Extend

```text
Retire: the file is deleted, its law has no old side left
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs

Extend

```text
Retire: the file is deleted, its law has no old side left
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefinementRulesNameOnlyTheModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run the C1 oracle through dotnet test: it reads every rule constant of the test assembly and every string literal of the test project sources
```

```text
No rule constant or literal outside the declared measurement files matches an old Refinement namespace or folder path; every Refinement rule pattern still matches a Refinement type in Modules.Refinement; the Architecture namespace stays green
```

```text
Made-up fixtures in the oracle: a rule constant with an old-namespace alternative, a literal with an old folder path, a pattern anchored so it matches no Refinement type, and a listed measurement file with no hit are each named; the old alternatives in today's rule files fail it before the crafter narrows them
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

RefinementPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:23

Extend

```text
The one Refinement context pattern; Flow metrics, Forecasting and Work Items callers are built from it, so narrowing it once reaches them
```

RefinementModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:32

Extend

```text
Owns the three per-type patterns and the static-ness and read-path guards; no other owner decides them
```

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:1

Reuse

```text
Stays for the Platform lane law; the Refinement law is retired, not rewritten
```

RefinementRulesNameOnlyTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:81

CreateNew

```text
Feature A's law compares old and new sight and cannot hold once the old side is gone; the sweep over constants and literals is a different question with one owner
```

RefinementPatternRoles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementPatternRoles\.cs\:12

Replace

```text
Declares roles for the old and new sight of each pattern; with the old side gone it has no reader, so it is deleted
```

RefinementStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs\:15

Replace

```text
Its rename list is empty and only the retired law and one placement test read it; deleted
```

RefinementReportsAccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Extend

```text
Owns the contexts Tracker integration must not reach; the Refinement part is narrowed in place
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Owns the contexts Access must not reach; the Refinement part is narrowed in place
```

RefinementAndReports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Extend

```text
Owns the contexts Licensing must not reach; the Refinement part is narrowed in place
```

RefinementStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:1

Extend

```text
Its placement test reads the retired rename list; it drops that read and keeps its membership checks
```

##### Impact

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:23

Changed

```text
RefinementPattern drops its old Services alternative
```

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:60

Changed

```text
The Refinement part leaves the Services group; Reports, Auth, Authorization and Licensing keep theirs
```

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
The Refinement part leaves the Services group; Reports keeps its alternative
```

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:65

Changed

```text
The Refinement part leaves the Services group; Reports keeps its alternative
```

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:32

Changed

```text
Three per-type patterns drop the old Implementation namespace
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:1

Changed

```text
Stops reading the retired empty rename list
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:25

Changed

```text
Deleted with RefinementPatternRoles and RefinementStringsTheMoveRenames; only RefinementLivesInItsModuleArchUnitTest read the rename list
```

test\-measurement

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs\:57

UnchangedCompatible

```text
Measurement record of the move keeps the old namespaces on purpose; the C1 oracle lists it as a declared measurement file
```

test\-rule\-pattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:20

UnchangedCompatible

```text
Measures the moving types in old and new namespaces; a declared measurement file
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementPatternRoles\.cs\:12

Changed

```text
Deleted with the retired law; no other file reads it
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs\:15

Changed

```text
Deleted; RefinementLivesInItsModuleArchUnitTest stops reading it in the same change
```

##### Agreement

Refinement context pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:23

Producer

Migrated

```text
RefinementPattern narrows to Modules.Refinement
```

Refinement context pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Builds its legal callers from RefinementPattern and follows it
```

Refinement context pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Builds its legal callers from RefinementPattern and follows it
```

Refinement context pattern shared by rule files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Consumer

UnchangedCompatible

```text
Builds its legal caller namespaces from RefinementPattern and follows it
```

##### Not applicable

contract_tests

```text
Test-side change in one deployable and one commit; no producer or consumer outlives the other
```

prefactoring

```text
No production code moves; the existing rules are narrowed in place and the existing Architecture suite is the preserved verdict
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
Every Refinement rule names Refinement by Modules.Refinement only and no rule constant or literal outside declared measurement files names an old Refinement namespace or path
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefinementRulesNameOnlyTheModuleArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefinementModuleBoundariesArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run the Refinement boundaries oracle through dotnet test: ModuleRules reads the compiled Lighthouse.Backend assembly for R-R1 to R-R6
```

```text
Each rule passes on today's code with its inline list equal to the IL measurement in both directions; a made-up offender per rule and per list position fails the same rule and the failure names the edge in Refinement wording
```

```text
Fixture types in a made-up namespace: a Domain type reaching a service, an outside type reaching Domain or Application, an Application type reaching EF Core, Data, API, an Adapter, Tracker integration or update code, an unlisted Access, set-up, Platform or repository edge, each list entry deleted, and a listed entry with no edge; a rule that lets any of them through fails the oracle
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:45

Reuse

```text
Owns the inbound, outbound, layer and kernel verdicts with their namespaces as parameters; Refinement passes its own, so no rule is written twice
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:1

Reuse

```text
Gives the failure text its Refinement and the API wording
```

PromisesModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Reuse

```text
Closest precedent, a context module with Domain and Application; copied in shape, not shared in code, because lists and made-up fixtures are per module
```

RefinementModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

CreateNew

```text
No Refinement rule on ModuleRules exists; RefinementModuleArchUnitTest holds other guards and keeps them
```

##### Impact

test\-rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
New oracle file; ModuleRules, ModuleWording and IlDependencies are not edited
```

test\-rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:1

UnchangedCompatible

```text
Covers the shared rules; Refinement lists and fixtures live in the new file
```

test\-rule\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:185

UnchangedCompatible

```text
Its only-API-and-composition-root rule stays beside the new R-R3 and R-R4; both agree on today's code
```

##### Agreement

None.

##### Not applicable

agreement

```text
The new file shares no contract; ModuleRules is read, not changed
```

contract_tests

```text
Test-side change in one deployable and one commit; no producer or consumer outlives the other
```

prefactoring

```text
No production code or existing rule is moved; the slice adds one new oracle file
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
R-R1 to R-R6: each listed type in its layer and none under Services; Domain reaches only the kernel and the BCL; outside reaches Domain and Application only from the legal callers; Application reaches no forbidden code and reaches Access, set-up, Platform and repositories only along the measured list
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefinementModuleBoundariesArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
