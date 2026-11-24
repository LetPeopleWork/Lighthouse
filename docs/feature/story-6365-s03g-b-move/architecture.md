# Architecture brief: story\-6365\-s03g\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Layer per file: NeedBand.cs goes to Modules/Refinement/Domain (namespace Lighthouse.Backend.Modules.Refinement.Domain); the other 15 files, with the three port files whole, go to Modules/Refinement/Application. The membership map RefinementMembership is the single owner of which type moves where; the new oracles read it and add no second list.
```
Governed slices: S1

#### D2

```text
Carriers: decisions stay in the existing owners (NeedBand value, RefinementList, StageRuleMatcher, VoterIdentityResolver and the others keep their data and behaviour); only the namespace and folder change. Errors stay the existing SizingRefusal, VoterRefusal and NeedUnavailableReason; effects stay behind the unchanged ports and Program.cs registrations. No per-process state is added.
```
Governed slices: S1

#### D3

```text
Log categories: ILogger<T> categories of moved types change with the namespace. No appsettings, analyser, suppression, CI, budget, host-user or Stryker target keys on a moving type (feature A measured this), so no log override is affected.
```
Governed slices: S1

#### D4

```text
Rename list RefinementStringsTheMoveRenames is feature A's support and B1 edits it: the 4 renamed entries are applied in their files and the 16 PlatformLaneUnitTestPlacementTest rows are removed in B1; the list ends empty. Feature A's rules and preservation law are not narrowed here (feature C).
```
Governed slices: S1

#### D5

```text
Paradigm object_oriented, behaviour-free namespace move. Formal models (Quint, TLA+, Agda) not applicable: a finite closed set of types with no state, ordering or concurrency claim; liveness not applicable for the same reason. The checker for each claim is the compiled-IL architecture test (namespace and folder of every moving type) and the existing preservation law, run as NUnit tests; no property-based generation, because the domain is the finite map of 41 types in 16 files, covered by native finite cases.
```
Governed slices: S1, S2

#### D6

```text
Rigor profile: architect, acceptance designer and crafter on sonnet, reviewers and EXAMINE on haiku. Moves go in refactor(refinement): move commits, production move first (B1), then the unit-test move (B2). Program.cs registrations keep order, lifetimes and service/implementation pairs and change only their full names.
```
Governed slices: S1, S2

### Constraints

#### K1

```text
Behaviour-free: no route, DTO, header, wire format, EF model or migration change; stays in place: sizing-log repository pair, Models/Refinement, API controllers, validator and DTOs, WeeklyRecurrence, and the acceptance, wire-contract, model and usage-data tests (only their using lines change). Input bounds: none, the move takes no input. Comments never cite internal ids.
```
Governed slices: S1, S2

#### K2

```text
Declared commands stay under the 120 s shell limit: oracle by name fragment, the Architecture namespace, dotnet build Lighthouse.Backend, and the filtered full suite as a background job. Program.cs changes select every live connector suite in CI; no live suite runs locally. Per-test budget default; no derogation requested.
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
Producer and consumer change in one deployable and one commit; no persisted or wire data changes; the acceptance and wire-contract tests are the primary oracle for unchanged behaviour.
```

agreement

```text
No shared schema or wire format is touched: DTOs, routes, headers and the EF model are unchanged; the only shared names are C# namespaces inside one deployable, covered by impact records.
```

### Boundaries

```text
RefinementController and RefinementVotesController over IRefinementViewQuery and ISizingLogCommands (unchanged), observed through the Refinement acceptance tests at the service port and the wire-contract tests
```

#### Driven ports

```text
ISizingLogRepository (stays), IRefinementCalendar (moves with the module)
```

```text
API and Program.cs depend on Modules.Refinement.Application; Application depends on Modules.Refinement.Domain and the shared kernel Models.Refinement; Domain depends on nothing of Services. Direction unchanged, only namespaces move.
```

#### Failures

None.

### Observations

```text
Namespace and declaring-file folder of each type of the membership map, read from the compiled IL and the source tree
```

per type\: \(namespace\, folder\) pair

```text
behaviour of the type; a type renamed without its behaviour preserved is caught only by the existing acceptance and wire-contract tests
```

### Equality

namespace\-and\-folder observation

```text
A type is in place when its full name equals the map's new full name, its namespace equals its folder path, and its old full name no longer exists; equality of behaviour is observed by the unchanged acceptance and wire-contract tests.
```

### Constructors

None.

### Laws

#### L1

```text
For every moving type of the membership map the assembly holds exactly the new full name Lighthouse.Backend.Modules.Refinement.Domain|Application.Name, not the old one, and its file sits in the folder of that namespace
```

Invariant

Domain

RefinementMembership

##### Treatment

Finite

###### Cases

```text
NeedBand in Domain
```

```text
a type of each of the 13 implementation files in Application
```

```text
a type of each of the 3 port files in Application
```

```text
The domain is the finite map of 41 types; a native finite-case test over the map covers it and shrinking adds nothing.
```

##### Evidence

None.

#### L2

```text
The rename list holds no entry for a moved type and no file of the moving set remains under Services/Implementation/Refinement or Services/Interfaces/Refinement
```

Invariant

Infrastructure

RefinementStringsTheMoveRenames

##### Treatment

Finite

###### Cases

```text
list is empty
```

```text
both old folders hold no moving file
```

```text
Finite list and finite folders; native finite cases.
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
Every moving type is in its new namespace only
```

```text
A type left or duplicated in the old namespace splits the module and a rule or registration resolves the wrong one
```

RefinementMembership

```text
the membership map of 41 types
```

move of each declaring file whole

RefinementLivesInItsModuleArchUnitTest

```text
IL namespace and folder per type
```

```text
reflection by string type name and a stale using line; the compiler catches the using, no reflection names a moving type (feature A measured)
```

### States

Admitted

```text
all 41 types in the new namespace, old folders gone
```

RefinementMembership

Excluded

```text
a moving type in its old namespace, a file under the old folders, an entry left in the rename list for a moved type
```

RefinementMembership

Unknown

```text
string-built type names read at runtime by reflection
```

crafter\, checked by the full filtered suite

### Models

None.

### Closure

```text
the 41 moving types of the membership map in their new namespace and folder
```

Safety

Pass

DesignedNotBuilt

```text
Safety is exclusion of an old-namespace type: the B1 oracle reads the compiled IL and refuses any moving type in its old namespace or any file left under the old folders.
```

##### Evidence

None.

```text
each moving type from the membership map to its new namespace
```

Inhabitation

Pass

DesignedNotBuilt

```text
Every type of the map has an acyclic construction path: the crafter moves each declaring file whole, the compiler resolves it, and the oracle finds it in its new place.
```

##### Evidence

None.

```text
Refinement acceptance and wire-contract tests reach the moved types through the unchanged service port
```

PublicReachability

Pass

DesignedNotBuilt

```text
The public ports (service port, controllers, Program.cs registrations) are unchanged; only full names change, so the existing acceptance and wire-contract tests observe the same behaviour.
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
Not applicable: no progress, scheduling or fairness claim, the move adds no state or concurrency.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
assumption: only the 4 listed strings and Program.cs name a moving type by old path
```

```text
all Refinement behaviour and every architecture rule stay green
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
assumption: log categories are not keyed by a moving type; negated: a log override targets the old namespace
```

```text
log output content
```

```text
ILogger category string changes to the new namespace
```

```text
a lost log override
```

```text
none: no appsettings or analyser names a moving type
```

crafter

#### Y3

Bypass

```text
second stressor set: a half-applied move or a late rebase adds a using of the old namespace
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

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Domain\/NeedBand\.cs

Create

```text
new home of the Domain file, namespace Lighthouse.Backend.Modules.Refinement.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCadenceCalendar\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementList\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementResolution\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementViewQuery\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingLogCommands\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingRefusal\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SleYardstickResolver\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleHealing\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/VoterIdentityResolver\.cs

Create

```text
new home, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementCalendar\.cs

Create

```text
new home of the port file, whole, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementViewQuery\.cs

Create

```text
new home of the port file, whole, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/ISizingLogCommands\.cs

Create

```text
new home of the port file, whole, namespace Lighthouse.Backend.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
the eight Refinement registrations change only their full names; order, lifetimes and service/implementation pairs stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RefinementViewDto\.cs

Extend

```text
using line follows the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/SizingLogDto\.cs

Extend

```text
using line follows the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs

Extend

```text
using line follows the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs

Extend

```text
using line follows the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using line follows the moved types
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsScenarios\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsSpecifications\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
using line or full-name literal follows the moved types; assertions unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
the one rename-list literal in this guard test is renamed to the new place
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
the one rename-list literal in this guard test is renamed to the new place
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs

Extend

```text
the one rename-list literal in this guard test is renamed to the new place
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
the one rename-list literal in this guard test is renamed to the new place
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Extend

```text
the 16 rows that only existed for the old Refinement folders are removed
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs

Extend

```text
the rename list shrinks as entries are renamed or removed and ends empty; it is the prior feature's support, which this slice may edit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs

Extend

```text
using lines follow the moved types so the build stays green until the unit tests move
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs

Extend

```text
five made-up-case tests are reshaped to build from the namespace the map gives each type today, no assertion weakened
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~RefinementLivesInItsModule
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementPatternRoles\.cs

#### Public oracle

```text
Run RefinementLivesInItsModuleArchUnitTest through the NUnit runner (the architecture test over the compiled Lighthouse.Backend assembly and the source tree), reading RefinementMembership and RefinementStringsTheMoveRenames.
```

```text
Each of the 41 moving types exists as Lighthouse.Backend.Modules.Refinement.Domain (NeedBand) or .Application (all others) only, its declaring file sits in Modules/Refinement/Domain or /Application, no moving file remains under Services/Implementation/Refinement or Services/Interfaces/Refinement, the 31 staying types are untouched, and the rename list holds no entry; derived independently from the membership map, not from the production code.
```

```text
Before the move every type is still in its old namespace and the test fails; a type left or copied in an old namespace, a file left in an old folder, a staying type moved, or a leftover rename-list entry fails it.
```

##### Promises

P1

P2

P4

#### Prefactoring

```text
L1 move: relocate the 16 files whole into Modules/Refinement, change only namespace, using lines and Program.cs full names; makes feature C's narrowed Refinement rules and R-R1 to R-R6 possible
```

```text
all Refinement behaviour, the EF model, routes, DTOs, and every architecture rule and feature A's preservation law
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

```text
RefinementMembershipArchUnitTest is a B1 target: the five made-up-case tests (AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches, AMapWithOneTypeLeftOff_NamesThatTypeAsNotOnTheMap, AMapWithOneTypeSwappedForAMadeUpOne_NamesBoth_ThoughItListsAsManyTypesAsTheCodeHolds, ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice, ATypeMovedIntoTheOtherLayer_IsNamedAsNotInTheCode_AndAsNotOnTheMap) build their cases from the namespace the map gives each type today (new once moved, old before). Each keeps its name, intent and subject; no assertion is weakened. RefinementMembership.cs stays unchanged.
```

##### Constraints

None.

##### Reuse

RefinementMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs\:1

Reuse

```text
The single owner of which type moves where and to which layer; the B1 oracle reads it and adds no second list.
```

RefinementStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs\:1

Extend

```text
The owner of the strings the move renames; it shrinks to empty as the strings are renamed or removed.
```

Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

NeedBandTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementCadenceCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementListTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementNeedCalculatorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementResolutionTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

RefinementViewQueryTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

SizingLogCommandsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

SleYardstickResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

StageRuleMatcherTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

VoterIdentityResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs\:1

Extend

```text
Existing unit test; only using lines change here, it moves in the next slice.
```

NeedBand

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Domain\/NeedBand\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same value object moved whole, the old file is deleted.
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Extend

```text
Existing composition root; only the full names of the Refinement registrations change.
```

RefinementCadenceCalendar

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCadenceCalendar\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementCalendar

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementList

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementList\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementNeedCalculator

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementResolution

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementResolution\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementViewQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementViewQuery\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

SizingLogCommands

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingLogCommands\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

SizingRefusal

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingRefusal\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

SleYardstickResolver

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SleYardstickResolver\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

StageRuleHealing

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleHealing\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

StageRuleMatcher

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

VoterIdentityResolver

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/VoterIdentityResolver\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

IRefinementCalendar

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementCalendar\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

IRefinementViewQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementViewQuery\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

ISizingLogCommands

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/ISizingLogCommands\.cs\:1

CreateNew

```text
New path of the moved file; the type is the same one moved whole, the old file is deleted.
```

RefinementViewDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RefinementViewDto\.cs\:1

Extend

```text
Existing consumer; only using lines change.
```

SizingLogDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/SizingLogDto\.cs\:1

Extend

```text
Existing consumer; only using lines change.
```

RefinementController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs\:1

Extend

```text
Existing consumer; only using lines change.
```

RefinementVotesController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs\:1

Extend

```text
Existing consumer; only using lines change.
```

TeamController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Extend

```text
Existing consumer; only using lines change.
```

RefinementAtThePortTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

Slice12CommentsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsScenarios\.cs\:1

Extend

```text
Existing test; only a using line changes.
```

Slice12CommentsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsSpecifications\.cs\:1

Extend

```text
Existing test; only a using line changes.
```

RefinementVotesControllerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

CurrentUserProfileServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

FeatureOrderingSingleSourceArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

RefinementModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

TrackerIntegrationModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

CategoryListOwnershipGuardTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

RepositoryLeakRatchetArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Extend

```text
Existing test or guard; only a using line or rename-list literal changes.
```

RefinementMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:1

Extend

```text
Existing test of the map; only its made-up cases change.
```

##### Impact

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementList\.cs\:1

Migrated

```text
The 16 files move to Modules/Refinement; the old folders end empty.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1429

Changed

```text
Eight registrations change full names only; order, lifetimes and pairs stay.
```

api\-consumers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs\:1

Changed

```text
Using lines in the controllers, two DTOs and TeamController follow the new namespaces; routes, DTOs and headers are unchanged.
```

tests\-and\-guards

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs\:1

Changed

```text
Four literals renamed, 16 PlatformLaneUnitTestPlacementTest rows removed, the rename list emptied; the acceptance, wire-contract, model and usage-data tests change only using lines where they have one.
```

build\-and\-ci

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:1

UnchangedCompatible

```text
SDK-style project globbing picks up the new folder; no suppression, analyser, CI selection, budget, host-user or Stryker target names a moving type; the backend path filter already covers Modules.
```

persistence

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Refinement\/SizingLogEntry\.cs\:1

UnchangedCompatible

```text
Models/Refinement stays; the EF model and migrations are unchanged.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Domain\/NeedBand\.cs\:1

Migrated

```text
New home in the Domain layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/NeedBand\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCadenceCalendar\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementCadenceCalendar\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementCalendar\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementCalendar\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementList\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementList\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementNeedCalculator\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementNeedCalculator\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementResolution\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementResolution\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/RefinementViewQuery\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementViewQuery\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingLogCommands\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SizingLogCommands\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SizingRefusal\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SizingRefusal\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/SleYardstickResolver\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SleYardstickResolver\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleHealing\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/StageRuleHealing\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/StageRuleMatcher\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/StageRuleMatcher\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/VoterIdentityResolver\.cs\:1

Migrated

```text
New home in the Application layer, namespace equal to folder.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/VoterIdentityResolver\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementCalendar\.cs\:1

Migrated

```text
New home of the port file, whole, in the Application layer.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Refinement\/IRefinementCalendar\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/IRefinementViewQuery\.cs\:1

Migrated

```text
New home of the port file, whole, in the Application layer.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Refinement\/IRefinementViewQuery\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/Application\/ISizingLogCommands\.cs\:1

Migrated

```text
New home of the port file, whole, in the Application layer.
```

port\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Refinement\/ISizingLogCommands\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RefinementViewDto\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/SizingLogDto\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementController\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/RefinementVotesController\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

production\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
Using lines or full names follow the moved types; behaviour, routes and DTOs unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsScenarios\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsSpecifications\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/RefinementVotesControllerTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementStringsTheMoveRenames\.cs\:1

Changed

```text
Using line or rename-list literal follows the moved types; assertions unchanged.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs\:1

Changed

```text
Using lines follow the moved types until the test moves in the next slice.
```

test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:1

Changed

```text
Five cases name the old namespaces and cannot hold after the move.
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
Every moving type of the membership map is in its new Modules.Refinement namespace and folder only; no moving file under the old Services folders; rename list empty
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~RefinementLivesInItsModule
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCadenceCalendarTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementListTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementResolutionTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SizingLogCommandsTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SleYardstickResolverTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/StageRuleMatcherTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/VoterIdentityResolverTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Domain\/NeedBandTest\.cs

Create

```text
new home, namespace Lighthouse.Backend.Tests.Modules.Refinement.Domain
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs

Extend

```text
file moves away: deleted from the old folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementUnitTestsBesideTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~RefinementUnitTestsBesideTheModule
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter FullyQualifiedName~Lighthouse.Backend.Tests.Modules.Refinement
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

#### Public oracle

```text
Run RefinementUnitTestsBesideTheModuleArchUnitTest through the NUnit runner over the compiled test assembly and the test source tree.
```

```text
NeedBandTest is in Lighthouse.Backend.Tests.Modules.Refinement.Domain and the other 10 Refinement unit tests are in Lighthouse.Backend.Tests.Modules.Refinement.Application, each in the folder of its namespace; the old Services/Implementation/Refinement test folder is empty; the acceptance, wire-contract, model and usage-data tests and SizingLogRepositoryTest stay in their namespaces.
```

```text
Before the move all 11 sit in the old folder and the test fails; an unit test left behind, a moved acceptance, wire-contract, model, usage-data or repository test, or a namespace that differs from its folder fails it.
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

RefinementCadenceCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementListTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementNeedCalculatorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementResolutionTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementViewQueryTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

SizingLogCommandsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

SleYardstickResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

StageRuleMatcherTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

VoterIdentityResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

NeedBandTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs\:1

Reuse

```text
Old location, deleted by the move.
```

RefinementCadenceCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCadenceCalendarTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

RefinementCalendarTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

RefinementListTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementListTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

RefinementNeedCalculatorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

RefinementResolutionTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementResolutionTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

RefinementViewQueryTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

SizingLogCommandsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SizingLogCommandsTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

SleYardstickResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SleYardstickResolverTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

StageRuleMatcherTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/StageRuleMatcherTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

VoterIdentityResolverTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/VoterIdentityResolverTest\.cs\:1

CreateNew

```text
New path of the moved test; same test class moved whole, assertions unchanged, the old file is deleted.
```

NeedBandTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Domain\/NeedBandTest\.cs\:1

CreateNew

```text
New path of the moved test; same class moved whole, assertions unchanged.
```

##### Impact

test\-files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs\:1

Migrated

```text
11 unit tests move; the old test folder ends empty; assertions unchanged.
```

test\-project\-discovery

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj\:1

UnchangedCompatible

```text
SDK-style project and NUnit discovery find tests by assembly, not by folder.
```

staying\-refinement\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/SizingLogRepositoryTest\.cs\:1

UnchangedCompatible

```text
Acceptance, wire-contract, model, usage-data tests and SizingLogRepositoryTest stay where they are.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCadenceCalendarTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCadenceCalendarTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementCalendarTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementListTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementListTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementNeedCalculatorTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementNeedCalculatorTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementResolutionTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementResolutionTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/RefinementViewQueryTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SizingLogCommandsTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/SleYardstickResolverTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/StageRuleMatcherTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/StageRuleMatcherTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Application\/VoterIdentityResolverTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/VoterIdentityResolverTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Refinement\/Domain\/NeedBandTest\.cs\:1

Migrated

```text
New home, namespace equal to folder.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/NeedBandTest\.cs\:1

Migrated

```text
Old location, deleted by the move.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Moving the unit tests is the whole slice; no separate preservation move precedes it, and the moved tests are the existing GREEN oracle for the production move.
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
The 11 Refinement unit tests live in Tests/Modules/Refinement/Domain and /Application with namespace equal to folder; the staying Refinement tests are untouched
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementUnitTestsBesideTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend --filter FullyQualifiedName~RefinementUnitTestsBesideTheModule
```

##### Contract tests

None.

## ADRs

None.
