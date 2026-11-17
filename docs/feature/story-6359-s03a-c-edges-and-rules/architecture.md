# Architecture brief: story\-6359\-s03a\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed D-8: Tracker integration gets one new boundaries file, Architecture/TrackerIntegrationModuleBoundariesArchUnitTest.cs, with rules R-T2..R-T8 written through the shared ModuleRules class and a ModuleWording of its own, as the Forecasting and Work Items boundaries files do; no rule body is copied. R-T1 (each listed type lives in its layer, none left under Services or Factories) is already pinned by the four closed layout oracles TrackerIntegrationDomainLayoutArchUnitTest, TrackerIntegrationApplicationLayoutArchUnitTest, TrackerIntegrationConnectorsLayoutArchUnitTest and TrackerIntegrationOAuthAndHealthLayoutArchUnitTest and is cited, not restated.
```
Governed slices: S1

#### D2

```text
Confirmed Ratchet design: the compiled IL decides every list entry. Each list is inline, measured from the IL during C1, keyed by full type name (nested types reduced to their top-level type), and compared both ways (a missing entry and an extra entry both fail). Where a short name is shared, the key is the full name (D-1 outcome record: the moved AzureDevOps WorkItemExtensions versus Lighthouse.Backend.Extensions.WorkItemExtensions). The Expected baseline column of the Ratchet design is a guess: an entry the design did not foresee is recorded in the list and reported to the host with the IL edge that forced it, never silently accepted; an expected entry the IL does not show is left out and reported the same way. Lists only shrink after C.
```
Governed slices: S1, S2

#### D3

```text
Confirmed D-7: the Promises port edge is legal; R-T4 lists DeliverySourceResolver and DeliveryForecastPublishingService by full name as outside callers of Application until Promises has a namespace (S03d). R-T7 (the module reaches no other context: Work Items, Forecasting, Flow metrics, Promises and update code, Refinement, Reports, Access, Licensing) carries exactly one listed edge, JiraWorkTrackingConnector to IDeliveryForecastBlockRenderer, owner S03d.
```
Governed slices: S1

#### D4

```text
Confirmed D-14: WorkTrackingConnectorFactory, WorkTrackingSystemFactory and AuthenticationMethodSchema stay in Application. Their Application to own-Adapters edges are listed by exact source type full name in the R-T6 Application outbound list, owner S09a. No namespace-wide or layer-wide exemption is added for them or for any other type; every exemption is exact per type.
```
Governed slices: S1

#### D5

```text
Confirmed D-1, D-4 and D-6 (stays): the three repositories (WorkTrackingSystemConnectionRepository, OAuthCredentialRepository, ConnectionHealthVerdictRepository), the connection save service and its types, connection usage, the write-back round, Encryption, demo data and Health stay where they are. R-T6 and R-T8 list the Application repository edges of ConnectionHealthService and OAuthService by full name; these entries replace the OAuthService and ConnectionHealthService exemptions of ModuleBoundariesArchUnitTest. The same staying types are the named exceptions of the C2 no-file-left check, read from TrackerIntegrationMembership.Stays rather than restated.
```
Governed slices: S1, S2

#### D6

```text
Confirmed D-2 as amended: Models types that do not meet all four admission conditions stay in the kernel (entities, wire types, the write-back vocabulary, ConnectionFetchShapeChanged, OAuthCompleteResult). Domain and Application reaching Models is therefore legal by namespace and needs no list entry; nothing moves in feature C.
```
Governed slices: S1

#### D7

```text
Confirmed Reuse Analysis (REPLACE row): ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern and its two rules WorkTrackingIntegration_IsALeafInputAdapter and WorkTrackingConnectors_DoNotDependOnAnyRepository are retired; R-T7 and R-T8 replace them. Every consumer of the pattern migrates to the module namespace pattern ^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$) in the same slice: DependencySingleDecisionArchUnitTest (TheConnectors), WorkItemsModuleBoundariesArchUnitTest, RefinementModuleArchUnitTest, and the TrackerIntegrationRulesSeeTheModuleArchUnitTest rows that key on the pattern by name. Feature B exact exemptions (ParentSourceSelector, IWorkTrackingConnectorFactory, IWorkTrackingSystemFactory, OAuthService, ConnectionHealthService) are kept or replaced by C1 list entries, never widened.
```
Governed slices: S1

#### D8

```text
Confirmed D-11: C1 removes the old alternatives from Scripts/test-selection/path-classifier.sh and dev-test.ps1 (Services/Implementation/WorkTrackingConnectors/<Tracker>/, Services/Implementation/WorkTrackingConnectors/(Auth|OAuth)/, top-level Services/Implementation/WorkTrackingConnectors/*.cs, Services/Interfaces/WorkTrackingConnectors/I*.cs, Factories); module-path alternatives and alternatives outside the context (Parents, Models, FetchFingerprint, Program.cs, the solution and project files) stay. test-path-classifier.sh scenarios that exist only for old paths are removed; every module-path scenario stays green, so a change to a connector under the module still selects that tracker live suite.
```
Governed slices: S1

#### D9

```text
Confirmed D-9, decided here: C2 writes a sibling TrackerIntegrationUnitTestPlacementTest modelled on WorkItemsUnitTestPlacementTest, as ForecastingUnitTestPlacementTest already is, instead of parameterising the existing placement tests: those are approved oracles of other features and would become craft targets. Test files are found on disk and classes read from the compiled test assembly with symbols. The no-file-left check lists the files under Services/Interfaces, Services/Implementation (WorkTrackingConnectors, OAuth, ConnectionHealth) and Factories that belong to the context and fails on any that is not a named exception.
```
Governed slices: S2

#### D10

```text
Layer algebra, test side only (production unchanged, D-0, D-10, D-12): state = the finite set of top-level type-to-type IL edges of the production assembly and the finite set of source and test file paths; operation = evaluate a rule over that state with an inline list; observation = a verdict naming each offender, each missing list entry and each extra list entry. Carriers: InboundVerdict, OutboundVerdict and KernelRuleVerdict owned by Lighthouse.Backend.Tests/Architecture/ModuleRules.cs; the module wording is a ModuleWording value owned by the new boundaries file; membership facts are owned by TrackerIntegrationMembership. No per-process state.
```
Governed slices: S1, S2

#### D11

```text
Formal verification not applicable (feature-delta Formal verification section, Confirmed): every claim is an exact equality over finite sets read from the compiled IL and the file tree, decided exhaustively by the rule itself with a made-up offender per position as falsifier. There is no state transition, ordering, concurrency, recovery or liveness claim, so no Quint, TLA+/TLC or Agda model is selected; a model would restate the namespace map and could not fail. Liveness is not applicable: nothing runs over time. Treatment of each law is Finite; property-based testing is not applicable to a closed finite domain.
```
Governed slices: S1, S2

### Constraints

#### K1

```text
Production code does not change: routes, DTOs, refresh answers, forecasts, DI registrations and handler order, and the EF model stay the same (D-0, D-10, D-12, P4).
```
Governed slices: S1, S2

#### K2

```text
Rules read the compiled IL through IlDependencies, which does not see catch-clause, event or attribute named-argument references; that blind spot is accepted and not compensated by text search.
```
Governed slices: S1, S2

#### K3

```text
Lists are never loosened and never widened to a namespace or layer; each entry names a full type name. Out of scope: moving repositories, the connection save service, connection usage, the write-back round, encryption or demo data, and shrinking any listed edge (S03d renderer edge, S09a connector construction, S03i connection save).
```
Governed slices: S1, S2

#### K4

```text
One oracle file per slice; an approved oracle file of this feature is not edited by a later slice. No nWave scenario tags in code. Made-up offenders live in a fixture namespace such as Lighthouse.Backend.Modules.TrackerIntegrationFixtures. Tests assert by the failure message naming the offender, not by counts.
```
Governed slices: S1, S2

#### K5

```text
No input bound applies: no stored or validated input is added or changed.
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
Every producer and consumer of the touched contracts (the rule files, the classifier variables and their sourcing scripts and workflow) lives in one repository and changes in the same commit; no party changes apart, so the primary oracles cover them.
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Create

```text
The oracle: rules R-T2..R-T8 with inline lists measured from the IL and a made-up offender per list position.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Retire WorkTrackingIntegrationPattern, WorkTrackingIntegration_IsALeafInputAdapter and WorkTrackingConnectors_DoNotDependOnAnyRepository with their OAuthService and ConnectionHealthService exemptions; R-T7 and R-T8 replace them.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
TheConnectors reads the module namespace instead of the retired pattern; the feature B exemptions stay exact.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Its connectors rule reads the module namespace instead of the retired pattern.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Its rule reads the module namespace instead of the retired pattern.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs

Extend

```text
Rows that key on WorkTrackingIntegrationPattern by name follow the retirement; the made-up offender under the module stays caught by the module rules.
```

Scripts\/test\-selection\/path\-classifier\.sh

Extend

```text
Remove the old Services/WorkTrackingConnectors and Factories alternatives; keep module paths.
```

Scripts\/test\-selection\/dev\-test\.ps1

Extend

```text
Same removal as path-classifier.sh so local and CI selection agree.
```

Scripts\/test\-selection\/test\-path\-classifier\.sh

Extend

```text
Remove scenarios that exist only for old paths; module-path scenarios stay.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationModuleBoundariesArchUnitTest
bash Scripts/test-selection/test-path-classifier.sh
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run TrackerIntegrationModuleBoundariesArchUnitTest (architecture-rules probe with fragment TrackerIntegrationModuleBoundaries) and bash Scripts/test-selection/test-path-classifier.sh on the candidate.
```

```text
Every rule R-T2..R-T8 passes on the production IL with its measured list and reports each made-up offender by name; the D-7 and D-14 entries appear by exact type name; every module-path classifier scenario passes and no old-path alternative remains; the filtered suite stays green.
```

```text
Before C1 the oracle class does not exist (RED). Removing a measured list entry, adding an entry with no IL edge, or a fixture type that reaches another context, a repository or the module Adapters from outside makes the named rule fail.
```

##### Promises

P1

P2

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Reuse

```text
Owns the inbound, outbound, layer and Domain rules and their verdicts; the new file calls it with the module namespaces and its own ModuleWording.
```

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:27

Reuse

```text
Made-up offenders in a fixture namespace are the falsifier approach; the new file uses its own TrackerIntegrationFixtures namespace.
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:34

Reuse

```text
Model for a module boundaries file with ModuleWording and inline lists; no rule body is copied.
```

TrackerIntegrationMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:11

Reuse

```text
Owns Moves and Stays; the lists derive layer subjects and staying types from it rather than restating them.
```

JIRA\_REGEX

Scripts\/test\-selection\/path\-classifier\.sh\:13

Extend

```text
The classifier owns live-suite selection; only old alternatives are removed.
```

JiraRegex

Scripts\/test\-selection\/dev\-test\.ps1\:22

Extend

```text
Local mirror of the classifier; same removal.
```

WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:15

Replace

```text
The leaf rule on old and new namespaces is replaced by R-T7 and R-T8 on the module namespace once nothing lives in the old ones.
```

TrackerIntegrationModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

CreateNew

```text
No existing file owns the Tracker integration module rules; it calls ModuleRules and copies no rule body.
```

TheConnectors

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:152

Extend

```text
Its single-decision rules keep their owner; only the connector namespace it reads changes to the module namespace.
```

WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:185

Extend

```text
The Refinement rule keeps its owner; it reads the module namespace instead of the retired pattern.
```

WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:68

Extend

```text
The feature A oracle keeps its other rows; rows keyed on the retired pattern follow its retirement.
```

throws\_count\_in\_checkout\_with\_connectors\_under

Scripts\/test\-selection\/test\-path\-classifier\.sh\:208

Extend

```text
The classifier test owns the selection scenarios; old-path-only scenarios are removed, module-path ones kept.
```

##### Impact

test\-oracle

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
New file.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:15

Migrated

```text
Pattern and its two rules removed; their subject moves to the new file.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:152

Migrated

```text
Consumer of the pattern reads the module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:258

Migrated

```text
Consumer of the pattern reads the module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:185

Migrated

```text
Consumer of the pattern reads the module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:152

Migrated

```text
Rows naming the pattern follow its retirement.
```

build\-script

Scripts\/test\-selection\/path\-classifier\.sh\:13

Changed

```text
Old alternatives removed; variable names and output unchanged.
```

build\-script

Scripts\/test\-selection\/dev\-test\.ps1\:22

Changed

```text
Old alternatives removed.
```

test\-script

Scripts\/test\-selection\/test\-path\-classifier\.sh\:232

Changed

```text
Old-path-only scenarios removed.
```

ci\-workflow

\.github\/workflows\/ci\_changes\.yml\:194

UnchangedCompatible

```text
Sources path-classifier.sh; same variables and output.
```

dev\-script

Scripts\/test\-selection\/dev\-test\.sh\:19

UnchangedCompatible

```text
Sources path-classifier.sh; same variables and output.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\:1

UnchangedCompatible

```text
No production edit; rules only read it.
```

##### Agreement

path\-classifier\.sh tracker and shared regex variables and their true\/false classification

Scripts\/test\-selection\/path\-classifier\.sh\:13

Producer

Migrated

```text
Old alternatives removed; names and output shape kept.
```

path\-classifier\.sh tracker and shared regex variables and their true\/false classification

\.github\/workflows\/ci\_changes\.yml\:194

Consumer

UnchangedCompatible

```text
Reads the same variables and output.
```

path\-classifier\.sh tracker and shared regex variables and their true\/false classification

Scripts\/test\-selection\/dev\-test\.sh\:19

Consumer

UnchangedCompatible

```text
Reads the same variables and output.
```

path\-classifier\.sh tracker and shared regex variables and their true\/false classification

Scripts\/test\-selection\/test\-path\-classifier\.sh\:232

Consumer

Migrated

```text
Old-path-only scenarios removed in the same commit.
```

ModuleBoundariesArchUnitTest\.WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:15

Producer

Migrated

```text
Retired; consumers move to the module namespace in the same commit.
```

ModuleBoundariesArchUnitTest\.WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:152

Consumer

Migrated

```text
Reads the module namespace.
```

ModuleBoundariesArchUnitTest\.WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:258

Consumer

Migrated

```text
Reads the module namespace.
```

ModuleBoundariesArchUnitTest\.WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:185

Consumer

Migrated

```text
Reads the module namespace.
```

ModuleBoundariesArchUnitTest\.WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:152

Consumer

Migrated

```text
Rows naming the pattern follow its retirement.
```

##### Not applicable

prefactoring

```text
Nothing moves in feature C and production is unchanged; the rule-file edits are the slice behaviour itself, preserved by the existing architecture tests staying green.
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
Each R-T list equals the set of IL edges its rule finds on the production assembly: a missing entry and an extra entry both fail, naming the entry.
```

Invariant

Application

TrackerIntegrationModuleBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
production IL with the measured lists passes
```

```text
a measured entry removed from a list fails naming it
```

```text
an entry with no IL edge added to a list fails naming it
```

```text
A closed finite domain: the IL edges and file paths of one build are enumerated exhaustively by the rule, so examples per position are complete and property-based generation adds nothing.
```

###### Evidence

None.

###### L2

```text
A made-up type in the TrackerIntegrationFixtures namespace in each rule position (Domain reaching beyond the kernel, an unlisted outside caller of Domain, Application or Adapters, Application reaching EF Core, Data, API or another module Adapters, the module reaching another context, a module type reaching a repository) makes that rule fail naming the type.
```

Invariant

Application

TrackerIntegrationModuleBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
one offender per rule R-T2..R-T8
```

```text
A closed finite domain: the IL edges and file paths of one build are enumerated exhaustively by the rule, so examples per position are complete and property-based generation adds nothing.
```

###### Evidence

None.

###### L3

```text
A changed file under Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ selects that tracker live suite; shared module paths select every live suite; no old Services/WorkTrackingConnectors or Factories alternative remains in path-classifier.sh or dev-test.ps1.
```

Invariant

Infrastructure

path\-classifier\.sh

###### Treatment

Finite

###### Cases

```text
one module-path scenario per tracker and shared stays green
```

```text
old-path-only scenarios removed
```

```text
A closed finite domain: the IL edges and file paths of one build are enumerated exhaustively by the rule, so examples per position are complete and property-based generation adds nothing.
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
Tracker integration module rules
```

Safety

Pass

DesignedNotBuilt

```text
Each rule fails on a made-up offender in each position and on a missing or extra list entry.
```

###### Evidence

None.

```text
Tracker integration module rules
```

Inhabitation

Pass

DesignedNotBuilt

```text
The lists are measured from the current IL, so the production assembly satisfies every rule.
```

###### Evidence

None.

```text
Tracker integration module rules
```

PublicReachability

Pass

DesignedNotBuilt

```text
The rules run in the architecture suite, observable through the architecture-rules probe with the class-name fragment and through the filtered dotnet test gate.
```

###### Evidence

None.

##### Risks

###### Y1

Stressor

```text
Assumption negated: the Expected baseline column is correct
```

```text
Every list equals the IL
```

```text
An unforeseen entry is added with the edge that forced it
```

```text
An entry accepted silently or a list widened to a namespace
```

```text
Reported to the host for the design owner
```

host

###### Y2

Bypass

```text
Assumption negated: the IL shows every dependency
```

```text
Rules over IL type references
```

```text
Catch-clause, event and attribute named-argument references and reflection by string go unseen
```

```text
Text search presented as an IL fact
```

```text
Accepted blind spot; the full suite and review cover behaviour
```

maintainer

##### Architectural tests

###### T1

```text
R-T2 Domain reaches only Models and the base library; R-T3 outside to Domain only from its list; R-T4 outside to Application only from API, the composition root and Work Items by namespace plus its list; R-T5 outside to Adapters only from the composition root plus its list; R-T6 Application reaches no EF Core, Data, API or other module Adapters and platform only through its list (including the D-14 entries); R-T7 the module reaches no other context except the one D-7 entry; R-T8 no module type reaches a repository except the R-T6 Application entries. Each list compared both ways; a made-up offender in each position caught.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationModuleBoundariesArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs

Create

```text
The oracle: unit-test placement under Tests/Modules/TrackerIntegration and the no-file-left check for the old folders.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationUnitTestPlacementTest
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run TrackerIntegrationUnitTestPlacementTest (architecture-rules probe with fragment TrackerIntegrationUnitTestPlacement) on the candidate.
```

```text
Every module unit test sits under Tests/Modules/TrackerIntegration with its folder namespace; no context file remains in Services/Interfaces, Services/Implementation or Factories apart from the named exceptions; made-up offenders are reported by path.
```

```text
Before C2 the oracle class does not exist (RED). A made-up test or source file of the context in an old folder, or an empty or missing listed folder, makes the check fail naming the path.
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

WorkItemsUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs\:17

Reuse

```text
Model for finding test files on disk and classes from the compiled assembly with symbols; a sibling, not a parameterisation, because it is another feature approved oracle.
```

TrackerIntegrationMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:123

Reuse

```text
Stays names the exceptions of the no-file-left check.
```

TrackerIntegrationUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:1

CreateNew

```text
No placement test owns the Tracker integration module; a sibling of WorkItemsUnitTestPlacementTest and ForecastingUnitTestPlacementTest, which are other features approved oracles.
```

##### Impact

test\-oracle

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:1

Changed

```text
New file.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\:1

UnchangedCompatible

```text
No file moves; the check reads paths only.
```

##### Agreement

None.

##### Not applicable

agreement

```text
A new test file only; it touches no shared contract.
```

prefactoring

```text
A new test file only; no production or existing test code moves.
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

###### L4

```text
A test or source file of the context outside its module folder, other than a named exception, is reported by path; a file in its module folder is not.
```

Invariant

Infrastructure

TrackerIntegrationUnitTestPlacementTest

###### Treatment

Finite

###### Cases

```text
a made-up test file under Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors is reported
```

```text
a made-up source file under Services/Interfaces/WorkTrackingConnectors is reported
```

```text
a named exception such as a staying repository is not reported
```

```text
a made-up test file in its module layer folder is not reported
```

```text
A closed finite domain: the IL edges and file paths of one build are enumerated exhaustively by the rule, so examples per position are complete and property-based generation adds nothing.
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
Tracker integration test placement and no-file-left
```

Safety

Pass

DesignedNotBuilt

```text
Made-up files in old folders are reported by path.
```

###### Evidence

None.

```text
Tracker integration test placement and no-file-left
```

Inhabitation

Pass

DesignedNotBuilt

```text
Feature B mirrored every test under the module, so the current tree passes.
```

###### Evidence

None.

```text
Tracker integration test placement and no-file-left
```

PublicReachability

Pass

DesignedNotBuilt

```text
Runs in the architecture suite; observable through the architecture-rules probe.
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Every unit test file of the module sits under Lighthouse.Backend.Tests/Modules/TrackerIntegration in the folder of its layer with the namespace of its folder; no file of the context is left under Services/Interfaces, Services/Implementation (WorkTrackingConnectors, OAuth, ConnectionHealth) or Factories apart from the named exceptions; a listed folder that is empty or missing fails.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~TrackerIntegrationUnitTestPlacementTest
```

##### Contract tests

None.

## ADRs

None.
