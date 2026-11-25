# Architecture brief: story\-6657\-write\-back\-round

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
D-0 No schema or behaviour change: no entity, DbSet, migration, route, DTO, WriteBackValueSource ordinal, usage-data event, configuration key, DI lifetime or registration order changes; what is written to Jira and Azure DevOps, when and how often per round, and the quiet, suppression, licence and consent behaviour stay as they are. Observed by ef-model-unchanged, an empty diff under migrations, frontend and E2E, and the unchanged write-back acceptance and host tests.
```

#### D2

```text
D-1 Owner Forecasting: the 5 classes (WriteBackCollector, WriteBackRound, WriteBackRoundContext, WriteBackService, WriteBackTriggerService) and 3 ports (IWriteBackCollector, IWriteBackService, IWriteBackTriggerService) move to Modules/Forecasting/Application/WriteBack, namespace Lighthouse.Backend.Modules.Forecasting.Application.WriteBack, ports co-located. The sub-namespace gives the round a name rules key on. Rejected: flat Application (8-name lists return), split into the platform lane (staging is context knowledge), Tracker integration, Promises, Work Items, staying in Services. No Domain member: the round is application orchestration.
```

#### D3

```text
D-2 Membership: WriteBackMembership lists the 8 moving types with old and new namespace, area Forecasting, and the staying neighbours with reason: Models/WriteBack (kernel), IWriteBackTarget and connectors (Tracker integration), the API mapping helpers and DTO (driving adapter), the updaters and UpdateQueueService (callers). Membership is checked against the compiled IL, top-level types only (nested names reduced before the plus sign).
```

#### D4

```text
D-3 Models/WriteBack stays in the kernel (all 9 types): the mapping definition is an EF entity, the value source is stored by ordinal, and the result and field-update types sit on IWriteBackTarget's signature in Tracker integration.
```

#### D5

```text
D-4 Tracker integration edge stays listed by name, one caller: WriteBackService is the only outside type of Tracker integration's write-back port (R-T4 entry renamed); Forecasting is not made a legal caller by namespace. ADR-270 gets the row Tracker integration to Forecasting, customer of the ACL's write-back capability, one listed type.
```

#### D6

```text
D-5 Rules first (S1): one owner ModuleRules.TheWriteBackRound, matched against the full name, equal to the 8 old full names union the new namespace; the Access, Licensing and Promises predicates take the round pattern as an argument (production value for the real rule, a made-up value naming the fixture namespace for the falsifier) instead of reading their 8-name lists. The shared NamespaceMoveLaw is reused unchanged with a write-back map; the literal sweep WriteBackStringsTheMoveRenames is fixed from the pre-move grep and only shrinks in meaning.
```

#### D7

```text
D-6 Tests move with production: the 8 unit-test files move in S2 to Tests/Modules/Forecasting/Application/WriteBack, namespace equals folder; RoundWriteBackDuringDatabaseMaintenanceTest stays with the update code unless S1 finds it constructs only round types. Acceptance, host and container tests stay; only their using lines change. Improve-the-tests-you-touch is waived for move-only and using-only edits.
```

#### D8

```text
D-7 Platform lane to Forecasting edges (UpdateQueueService and its nested HeldUpdate creating, handing over and flushing the round) are listed in Forecasting's inbound list, not in the platform lane's list, as DatabaseMaintenanceGate is in Flow metrics. Inverting the flush behind a queue-owned port is noted for S09a, not done here.
```

#### D9

```text
D-8 Update code to Forecasting edges are listed (TeamUpdater, UpdateServiceBase with its generic arity; PortfolioUpdater already listed); owner Epic 6474 slice 12.
```

#### D10

```text
D-9 Order: after Story 6468, before Story 6382 (S09a); the move does not wait for S09a. S1 first runs the three merged checks for 6365, 6366 and 6468 and stops if any fails.
```

#### D11

```text
D-10 Commit per slice, moves in refactor(forecasting) commits apart from edits; push S1 to S3 together once all three are green, so the Program.cs change triggers one live connector run.
```

#### D12

```text
D-11 Behaviour-free proof reuses the admitted probes: architecture-rules (all), ef-model-unchanged, backend-tests on the unchanged write-back acceptance and host test classes on both revisions, test-selection-show for Modules/Forecasting paths and Program.cs. No new compare command.
```

#### D13

```text
D-12 Mutation bar last, on frozen code: Stryker.NET over Modules/Forecasting/Application/WriteBack compared with the same files on the pre-move revision; no survivor or no-coverage mutant added; the 80 percent project bar reported.
```

#### D14

```text
D-13 Formal verification: no Quint, TLA+ or Agda model selected (maintainer default taken). Claims C1 preservation, membership and the sweep are relations over a finite closed set (compiled types x membership map x rule fields x test-project literals) with no state, transition, time, fairness or unbounded input; a model over a hand-copied abstraction would be weaker than deciding the relation on the compiled assembly. Each is decided exhaustively by NUnit over the IL in the default suite. The round's concurrency (AsyncLocal context, last-out flush) is unchanged (D-0) and stays observed by the TaskManager, ResponsiveDuringUpdates and CancelAllWhileWorkIsAdmitted tests. PBT not applicable (closed finite domain). Formal result NOT_SELECTED; no install or waiver needed.
```

#### D15

```text
D-14 Re-read against Story 6468 as shipped (main up to 6d531d0d1). E2 holds: PromisesModuleBoundariesArchUnitTest IsUpstreamCode still recognises the round only by its 8-name list, and TheNamespacedUpstreamCode (module adapters, update code, Access and Licensing) does not name Forecasting. E4 holds: Forecasting inbound list 1 (PortfolioUpdater), licence-edge list 1, platform lane 5 UpdateQueueService write-back edges, Flow metrics R4, Licensing R-L3 and Tracker integration R-T4 each still carry their one write-back entry; the 3 Access, Licensing and Promises lists still hold the same 8 names. IBlackoutPeriodService now lives in Modules/OrganisationSetup/Application (M15). Line numbers moved (PlatformLaneMembership 293-301, PlatformLaneUnitTestPlacementTest 88-98, PromisesUnitTestPlacementTest prefixes now only WriteBack and IWriteBack); S1 re-measures them.
```

#### D16

```text
D-15 New since the design, from Story 6468: (a) WorkItemsModuleBoundariesArchUnitTest.StrayLooseImplementation and StrayLooseInterfaces (and the composite StrayLooseServices, also read by RecurringBlackoutEventsSeamArchUnitTest) name the 8 round types by old full name as regex alternatives. S1 declares them Place on the full name in WriteBackPatternRoles and puts them on the sweep; S3 removes the 8 alternatives. S1 also proves with a planted Work Items Application type reaching a made-up round type in the new place that a Work Items rule still refuses the edge; if none does, the 8 alternatives are replaced by the new full names instead of removed. (b) OrganisationSetupMembership.cs lists 3 round files by old path as staying neighbours; S2 renames those paths. (c) NamespaceMoveLaw has no exception for a move into an existing area: admissions live per move in its PatternRoles class (6468's WholeParentPatterns and NamedByTheModuleOnly), so S1 creates WriteBackPatternRoles and leaves the law unchanged (M3 answered).
```

#### D17

```text
D-16 Narrowing planned from S1 so no frozen file is edited later: WriteBackPatternRoles holds one admission set, SeeTheRoundInItsNewPlace, for every Context pattern that must see the round in Forecasting (ModuleRules.TheWriteBackRound, ModuleRules.ForecastingModule and Forecasting Application patterns the role check reports). For those patterns the S1 check requires the new subject of every moved type to match and leaves the old subject free, so the same S1 oracle holds after the S1 widening, the S2 move and the S3 narrowing. A declared Place pattern may stop matching its old subject. S3's own oracle then asserts that every entry of that set and every Place entry sees no moved type under its old subject, which is 6468's NamedByTheModuleOnly check; the narrowing cannot hide a pattern that still reads the old home.
```

#### D18

```text
D-17 One oracle file per slice, approved bytes frozen: S1 WriteBackMembershipArchUnitTest (membership in old or new place, the law with the write-back map, the Promises falsifier); S2 WriteBackRoundLivesInForecastingArchUnitTest (each listed type only in its new namespace; deviation from the design text, which tightened S1's test, because an approved oracle is never edited); S3 WriteBackRulesNameOnlyForecastingArchUnitTest (sweep with an empty list, round pattern new-only). Each WriteBackStringsTheMoveRenames entry holds while its file carries exactly one of its old or new text, so S2 renames without editing the S1 support. R-WB1 to R-WB4 in WriteBackRoundBoundariesArchUnitTest are an S3 crafter target with planted falsifiers, green on writing.
```

#### D19

```text
D-18 Carriers: decisions are NUnit verdicts over IlDependencies reads (owner: each ArchUnitTest class); the moved production types keep their carriers unchanged (WriteBackRoundContext per-execution AsyncLocal state, WriteBackRound staging per round, results in Models/WriteBack); the map is WriteBackMembership (MovingType records), the roles WriteBackPatternRoles (PatternDeclaration records), the literal list WriteBackStringsTheMoveRenames (RenamedString records), the round pattern ModuleRules.TheWriteBackRound. Effects: none beyond git mv. Paradigm object_oriented: rule classes own their lists; no anemic split.
```

### Constraints

#### K1

```text
Behaviour-free: production changes only by git mv plus namespace and using lines (S2); no registration line, order or lifetime in Program.cs changes; one using line added there.
```
Governed slices: S1, S2, S3

#### K2

```text
No rule or list is loosened. Lists are inline, by full type name, compared both ways and only shrink, except the measured growth of Forecasting's inbound and licence-edge lists (E4), which moves existing edges from other lists and is recorded in ADR-270 with owner.
```
Governed slices: S1, S2, S3

#### K3

```text
Rule exemptions for a moved type are exact (that type and its nested types by full name), never a namespace or layer; every rule edit is proved by a throwaway probe whose offender is caught, then reverted, recorded as craft commands.
```
Governed slices: S1, S2, S3

#### K4

```text
Approved oracle and support files of this feature are never edited, not even a using line; oracles name production types as strings. After S1's sweep lands, a test literal naming a moving type by its old Services path is built from segments.
```
Governed slices: S1, S2, S3

#### K5

```text
No nWave scenario tags in code. Live-connector categories (JiraIntegration, LinearIntegration, AdoIntegration, ServiceNowIntegration, GithubIntegration) are never run; any filter selecting WriteBack adds TestCategory exclusions for JiraIntegration and AdoIntegration.
```
Governed slices: S1, S2, S3

#### K6

```text
No input bound applies: no stored or validated input changes; the rule inputs are the compiled assemblies and the test project's source text.
```
Governed slices: S1, S2, S3

### Reuse

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:118

Reuse

```text
Shared preservation law; takes a NamespaceMove and a PatternRoleMap; no change needed, admissions live in the move's roles class.
```

OrganisationSetupPatternRoles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs\:20

Reuse

```text
Shape copied for WriteBackPatternRoles (PatternRoles, FolderOnlyMatches, admission set); the class itself is not edited.
```

RefinementMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs\:1

Reuse

```text
Membership-map shape reused for WriteBackMembership; no existing map owns the round.
```

TheWriteBackRound

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:171

Replace

```text
Three lists (Access, Licensing, Promises) answer the same fact; replaced by one ModuleRules.TheWriteBackRound pattern; semantic duplicate removed.
```

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:25

Extend

```text
Owner of module patterns and shared rules; gains TheWriteBackRound; R-WB1 to R-WB4 reuse OutboundEdges and ModuleWording.
```

StrayLooseImplementation

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:56

Extend

```text
6468's full-name list of loose services; the 8 round alternatives leave in S3 (Place role).
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:103

Extend

```text
Forecasting's inbound and licence-edge lists take the measured round edges.
```

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:1

Reuse

```text
Planted-offender style for R-WB falsifiers.
```

QuietWriteBackSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/QuietWriteBackSeamArchUnitTest\.cs\:19

Extend

```text
HaveFullName strings become typeof full names so the rule cannot select an empty set after the move.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes (D-0); the moved types are in-process, one deployable, all consumers change in the same commit.
```

contract_tests

```text
Single deployable; producer and consumer of every touched interface change together; the tracker receives the same writes, observed by the unchanged acceptance tests.
```

### Boundaries

```text
Compiled production and test assemblies read through IlDependencies by NUnit architecture tests (in-process; nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture)
```

#### Driven ports

```text
Production runtime unchanged: IWriteBackTarget via IWorkTrackingConnectorFactory (Tracker integration), ITeamMetricsService (Flow metrics), ILicenseService (Licensing), IBlackoutPeriodService (Organisation set-up), repositories, ILighthouseClock
```

```text
Platform UpdateQueueService, the update code and ForecastUpdater call into Forecasting.Application.WriteBack (listed edges); the round calls out to Tracker integration (one listed type), Flow metrics, Licensing (listed), Organisation set-up (legal), kernel Models and repositories; Promises, Access and Licensing never call the round.
```

#### Failures

None.

### Observations

```text
NUnit verdict of an architecture rule, failing with a message that names each offending type, edge, pattern or literal
```

NUnit outcome with failure message

```text
Edges the IL reader misses (event declarations, named attribute arguments) and patterns declared as properties rather than fields, which the reflection over rule fields does not read
```

```text
Placement of the moving types measured from the compiled IL
```

set of \(top\-level type full name\, namespace\)

```text
Nested types unless reduced to their top-level type; a same short name in another namespace unless matched by full name
```

### Equality

Membership

```text
Two placements are equal when every listed top-level type resolves to the same namespace and no listed type exists in both its old and new namespace.
```

### Constructors

None.

### Laws

#### L1

```text
Move preservation: for every moved type T and every declared rule pattern, a Context pattern that matches the old subject of T matches its new subject; a Place pattern never matches the new subject; a pattern in SeeTheRoundInItsNewPlace matches the new subject of every T; an undeclared pattern matches neither subject.
```

Invariant

Infrastructure

WriteBackMembershipArchUnitTest

##### Treatment

Finite

###### Cases

```text
Context field seeing only the old place: StopsSeeingTheType
```

```text
Place field widened into the new place: SeesTheTypeInItsNewPlace
```

```text
Undeclared field matching an old full name: NotDeclared
```

```text
Forecasting module pattern sees the round only after the move: admitted by SeeTheRoundInItsNewPlace
```

```text
TheWriteBackRound old union new, then new only: admitted at every slice
```

```text
StrayLooseImplementation without the 8 alternatives: Place may stop matching
```

```text
Finite closed set: compiled rule fields x 8 moving types; decided exhaustively over the IL, with made-up keys proving each break kind.
```

##### Evidence

None.

### Refuted

```text
The module rules cover what the three 8-name round lists did, so the lists can simply be dropped after the move.
```

```text
Carrier: the lists named the round because it had no namespace; Promises reaches Forecasting Application by right, so the module rule cannot tell the round from the rest of Forecasting.
```

```text
A made-up Promises Application type reaching a made-up round type in the new WriteBack namespace passes Promises' upstream rule while the predicate reads only the old 8 names.
```

### Claims

#### C1

```text
No existing architecture rule silently stops seeing a write-back round type when it moves, and no Promises, Access or Licensing type can join the round unseen.
```

```text
A rule passes over an empty or shrunken subject set and a forbidden edge ships unseen; Promises reaches Forecasting by right, so a dropped list would admit it into the round.
```

WriteBackMembershipArchUnitTest

```text
Compiled test assembly rule fields, the write-back membership map and the test project's source literals
```

WriteBackPatternRoles declarations plus NamespaceMoveLaw plus ModuleRules\.TheWriteBackRound

Every ArchUnit rule reading a declared field\; the Access\, Licensing and Promises outbound predicates

```text
dotnet test --filter FullyQualifiedName~WriteBackMembershipArchUnitTest fails naming an undeclared or unpreserved field or a Promises type reaching the round
```

```text
Inline method-body literals (covered only by the sweep); patterns built at run time or declared as properties; rules selecting types by short name
```

### States

Admitted

```text
Before S2 every listed type is in its old namespace; after S2 only in its new one; TheWriteBackRound matches old and new during the move and new only after S3.
```

None.

Excluded

```text
A listed type in both or neither namespace; a Context pattern seeing a moved type only in the old place; any regex field or test literal naming an old full name or path after S3 outside the exact exclusion list.
```

WriteBackMembershipArchUnitTest

Unknown

```text
Whether a Work Items rule other than the StrayLoose list refuses a Work Items Application type reaching the round once it lives in Forecasting; S1 decides by a planted falsifier (D-15a).
```

S1 acceptance designer and crafter

### Models

None.

### Closure

```text
write-back round placement rules
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states refused by S1 law and S3 sweep, each with a planted made-up offender; not built yet.
```

##### Evidence

None.

```text
write-back round placement rules
```

Inhabitation

Pass

DesignedNotBuilt

```text
The 8 listed types exist today in Services.Implementation and Services.Interfaces (git ls-files); the map is constructible from them; S2 inhabits the new place.
```

##### Evidence

None.

```text
write-back round placement rules
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly, WriteBackMembership plus law plus TheWriteBackRound, module rules, dotnet test exit and message; acyclic, no bootstrap needed.
```

##### Evidence

None.

```text
write-back round placement rules
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no progress claim; each rule is a terminating evaluation over a finite assembly; the round's runtime progress is unchanged (D-0) and observed by existing tests.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negated: a pattern sees the round (false, it has no namespace)
```

```text
Every old literal is renamed or deleted
```

```text
Literal list fixed at S1, entries hold in old or new state
```

```text
An unlisted old literal surviving S3
```

```text
Literals built at run time
```

WriteBackStringsTheMoveRenames

#### Y2

Stressor

```text
Negated: module rules cover what the lists did (false for Promises)
```

```text
Promises, Access, Licensing never reach the round
```

```text
Lists kept until S3, predicates read the pattern from S1
```

```text
A Promises type joining the round unseen
```

```text
None
```

PromisesModuleBoundariesArchUnitTest

#### Y3

Stressor

```text
Negated: HaveFullName fails on an empty set (unknown)
```

```text
QuietWriteBackSeam keeps selecting WriteBackTriggerService
```

```text
typeof full names
```

```text
A rule passing over an empty selection
```

```text
None
```

S2 crafter

#### Y4

Stressor

```text
Negated: sibling Stories left counts as measured (false by construction; 6468 added StrayLoose and staying paths)
```

```text
Every list compared both ways
```

```text
Re-measure M1-M17 at S1, differences recorded
```

```text
Arguing a count instead of recording it
```

```text
None
```

S1 crafter

#### Y5

Stressor

```text
Negated: the shared law admits a move into an existing area (false: no such exception in NamespaceMoveLaw)
```

```text
Forecasting patterns must see the round
```

```text
Admission set SeeTheRoundInItsNewPlace in WriteBackPatternRoles
```

```text
Editing the shared law or S1 supports later
```

```text
None
```

WriteBackMembershipArchUnitTest

#### Y6

Bypass

```text
Rule pattern declared as a C# property (6468 used this to escape the law) or built in a method body
```

```text
The S3 sweep reads source text of the test project, not only fields
```

```text
Properties for deliberate exclusions with a reason
```

```text
A property pattern still naming an old full name after S3
```

```text
Short-name selections stay unseen by both
```

S3 crafter and reviewer

#### Y7

SinglePointOfFailure

```text
WriteBackMembership is hand-kept
```

```text
Map equals the IL measurement
```

```text
Map edited only by S1
```

```text
A missing or extra listed type
```

```text
None: membership test compares both ways
```

WriteBackMembershipArchUnitTest

#### Y8

Stressor

```text
Second set, walked not executed: host-test row naming a moved class; WriteBackValueSource ordinals; queue flushing from another scope; ADR-270 drawing Forecasting to Tracker integration as new coupling
```

```text
All survive: M13 re-measure, D-3 enum untouched, DI lifetimes unchanged, the edge existed under Services
```

```text
Walkthrough only
```

```text
None found
```

```text
Platform to Forecasting (S09a) and update code to Forecasting (Epic 6474 slice 12) edges stay listed
```

Host

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Adds TheWriteBackRound, full-name pattern: the 8 old full names union the new WriteBack namespace.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembership\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackStringsTheMoveRenames\.cs

#### Public oracle

```text
Run WriteBackMembershipArchUnitTest: (1) each type of WriteBackMembership exists in its old or its new namespace in the compiled IL, top-level names only; (2) NamespaceMoveLaw with the write-back map and WriteBackPatternRoles over every regex field of the test assembly; (3) a made-up Promises Application type reaching a made-up round type, judged by Promises' upstream predicate given a round pattern naming the fixture namespace; (4) a made-up Work Items Application type reaching a made-up round type in the new place, judged by Work Items' rules; (5) the literal sweep with WriteBackStringsTheMoveRenames.
```

```text
RED today on (3): the predicate reads only the 8 old full names, so the made-up Promises type passes. GREEN once Access, Licensing and Promises predicates read the round pattern; (1), (2), (5) green before and after S2 and S3 (entries hold in old or new state; admission set SeeTheRoundInItsNewPlace requires the new subject only); (4) records which Work Items rule refuses the edge.
```

```text
Dropping a listed type, an undeclared regex field matching an old or new full name, a Context field losing an old subject, a Place field matching the new namespace, or a predicate reading the list again fails naming the field, type or offender.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

LicensingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Changed

```text
Adds TheWriteBackRound, full-name pattern: the 8 old full names union the new WriteBack namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Outbound predicate takes the round pattern as an argument (ModuleRules.TheWriteBackRound in production); the 8-name list stays until S3, no loosening.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
S1 is itself the rules-first step of the move: rule changes only, production untouched; the existing Architecture suite is GREEN before and after it.
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
Round guard: Promises, Access and Licensing types reach no type whose full name matches ModuleRules.TheWriteBackRound outside their listed edges, whatever namespace the round sits in.
```

Invariant

Infrastructure

WriteBackMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
Made-up Promises type reaching a made-up round type in the new namespace: refused
```

```text
Existing made-up Access and Promises fixtures reaching IWriteBackTriggerService: refused
```

```text
No production Promises, Access or Licensing type reaches the round: empty
```

```text
Finite set of compiled types; decided over the IL with planted offenders.
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
No rule loses a write-back edge while the round moves; Promises, Access and Licensing refuse any round type by the one round pattern.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackMembershipArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackCollector\.cs

Create

```text
git mv from Services/Implementation; namespace and using lines only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackCollector\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRound\.cs

Create

```text
git mv from Services/Implementation; namespace and using lines only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRound\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRoundContext\.cs

Create

```text
git mv from Services/Implementation; namespace and using lines only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRoundContext\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackService\.cs

Create

```text
git mv from Services/Implementation; namespace and using lines only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs

Create

```text
git mv from Services/Implementation; namespace and using lines only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackCollector\.cs

Create

```text
git mv from Services/Interfaces; namespace line only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackCollector\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackService\.cs

Create

```text
git mv from Services/Interfaces; namespace line only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackService\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackTriggerService\.cs

Create

```text
git mv from Services/Interfaces; namespace line only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackTriggerService\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateQueueService\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackCollectorTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackCollectorTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRoundTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackRoundTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackServiceTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackSuppressionWarningTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.BeyondTheHorizon\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.BeyondTheHorizon\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.StartDates\.cs

Create

```text
git mv of the unit test beside its subject; namespace equals folder.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.StartDates\.cs

Extend

```text
Removed by the git mv.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice01WriteBackCollectionSpecifications\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/Slice01SleRiskReadSpecifications\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/FlushRecordingWriteBackCollector\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemBaseAgeOnDayTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/CancelAllWhileWorkIsAdmittedTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RefreshRoundSummaryLineSkippedForecastTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateQueueServiceTests\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/UpdateServiceTestBase\.cs

Extend

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/QuietWriteBackSeamArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundLivesInForecastingArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackRoundLivesInForecastingArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "(FullyQualifiedName~WriteBack|FullyQualifiedName~TaskManager|FullyQualifiedName~AdoOAuthWriteConsent)&TestCategory!=JiraIntegration&TestCategory!=AdoIntegration"
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembership\.cs

#### Public oracle

```text
Run WriteBackRoundLivesInForecastingArchUnitTest: each type of WriteBackMembership, read from the compiled IL by top-level full name, exists in its new namespace Lighthouse.Backend.Modules.Forecasting.Application.WriteBack and not in its old Services namespace; no top-level type named WriteBack* or IWriteBack* remains under Lighthouse.Backend.Services; the 8 unit-test files sit under Tests/Modules/Forecasting/Application/WriteBack.
```

```text
RED before the move, naming the 8 types still in Services.Implementation and Services.Interfaces; GREEN after the git mv with every existing Architecture rule green under the measured list changes.
```

```text
Leaving one type or test file in Services, or moving a type to flat Forecasting Application, fails naming that type or file.
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

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackCollector\.cs\:1

Migrated

```text
git mv from Services/Implementation; namespace and using lines only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackCollector\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRound\.cs\:1

Migrated

```text
git mv from Services/Implementation; namespace and using lines only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRound\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRoundContext\.cs\:1

Migrated

```text
git mv from Services/Implementation; namespace and using lines only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRoundContext\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackService\.cs\:1

Migrated

```text
git mv from Services/Implementation; namespace and using lines only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerService\.cs\:1

Migrated

```text
git mv from Services/Implementation; namespace and using lines only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackCollector\.cs\:1

Migrated

```text
git mv from Services/Interfaces; namespace line only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackCollector\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackService\.cs\:1

Migrated

```text
git mv from Services/Interfaces; namespace line only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackService\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/WriteBack\/IWriteBackTriggerService\.cs\:1

Migrated

```text
git mv from Services/Interfaces; namespace line only.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IWriteBackTriggerService\.cs\:1

Migrated

```text
Removed by the git mv.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateQueueService\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
One using line for Modules.Forecasting.Application.WriteBack; nothing else.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/BlackoutForecastShiftWriteBackTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackCollectorTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackCollectorTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackRoundTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackRoundTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackServiceTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackServiceTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackSuppressionWarningTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackSuppressionWarningTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.BeyondTheHorizon\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.BeyondTheHorizon\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/WriteBack\/WriteBackTriggerServiceTest\.StartDates\.cs\:1

Migrated

```text
git mv of the unit test beside its subject; namespace equals folder.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackTriggerServiceTest\.StartDates\.cs\:1

Migrated

```text
Removed by the git mv.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice01WriteBackCollectionSpecifications\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/Slice01SleRiskReadSpecifications\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/FlushRecordingWriteBackCollector\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/WorkItemBaseAgeOnDayTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/CancelAllWhileWorkIsAdmittedTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RefreshRoundSummaryLineSkippedForecastTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateQueueServiceTests\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/UpdateServiceTestBase\.cs\:1

Changed

```text
using lines (and the cref full name in QuietWriteBackAcceptanceTest) follow the move; no assertion changes.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/QuietWriteBackSeamArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Lists compared both ways updated from the IL (Forecasting inbound plus TeamUpdater, UpdateServiceBase, UpdateQueueService and HeldUpdate as measured; licence edges plus WriteBackTriggerService; Flow metrics R4 entry leaves; R-L3 and R-T4 entries renamed; platform lane 5 edges and still-in-Services entries leave; Promises placement exceptions leave), old literals renamed, HaveFullName to typeof, staying paths renamed, fixture references to moved types renamed; using lines.
```

##### Agreement

None.

##### Not applicable

reuse

```text
S2 creates no new type and extends no owner's responsibility: every production and unit-test target is an existing type or test moved unchanged by git mv (its new path does not exist before the move), and every other target only follows the move in using lines, literals or compared-both-ways lists. The owners consulted are recorded in the shared reuse rows (NamespaceMoveLaw, ModuleRules, ForecastingModuleBoundariesArchUnitTest, QuietWriteBackSeamArchUnitTest).
```

prefactoring

```text
The move is itself the preserving change; S1 already made the rules see both places, and the unchanged write-back acceptance and host tests observe behaviour before and after.
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
Placement: every listed round type resolves to exactly its new namespace, and the set of top-level types of the new namespace equals the listed 8.
```

Invariant

Infrastructure

WriteBackRoundLivesInForecastingArchUnitTest

###### Treatment

Finite

###### Cases

```text
Type still in old namespace: refused
```

```text
Type in both namespaces: refused
```

```text
Extra type in the WriteBack namespace: refused
```

```text
Eight named types, exhaustively checked over the IL.
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

###### T4

```text
The round lives in Forecasting Application WriteBack and nowhere else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundLivesInForecastingArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackRoundLivesInForecastingArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
TheWriteBackRound narrowed to ^Lighthouse.Backend.Modules.Forecasting.Application.WriteBack namespace only; no old alternative.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
The 8 round alternatives leave StrayLooseImplementation and StrayLooseInterfaces (or are replaced by new full names if S1 found no other Work Items rule refusing the edge).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs

Extend

```text
WriteBack and IWriteBack prefixes and the write-back newcomer case leave; R-WB1 takes the newcomer.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

Extend

```text
Placement covers the WriteBack sub-folder if S2 did not need it already.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs

Create

```text
R-WB1 to R-WB4 with planted falsifiers, lists inline by full name, compared both ways, ModuleWording for the round.
```

ARCHITECTURE\.md

Extend

```text
Section 4 Forecasting row gains Application.WriteBack; Forecasting rules bullet (inbound count, licence edges, Tracker integration edge, R-WB); Flow metrics count; section 3 namespace list. Prose, reviewed, no test.
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Left open write-back answered (Forecasting Application/WriteBack); edge rows Tracker integration to Forecasting (one listed type) and platform lane to Forecasting (listed, S09a note); layout line; glossary term write-back round if absent. Prose, reviewed.
```

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md

Extend

```text
Catalogue note: S03d's last named exception closed; S09a note on inverting the queue flush.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRulesNameOnlyForecastingArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackRulesNameOnlyForecastingArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

None.

#### Public oracle

```text
Run WriteBackRulesNameOnlyForecastingArchUnitTest: the S1 sweep with an empty rename list over every regex field and string literal of the test project, excluding by exact path only itself, WriteBackMembership.cs, WriteBackPatternRoles.cs, WriteBackStringsTheMoveRenames.cs and WriteBackMembershipArchUnitTest.cs (that exclusion list asserted equal to exactly those paths); and the role check asserting that every SeeTheRoundInItsNewPlace and Place entry sees no moved type under its old subject.
```

```text
RED at S3 start, naming TheWriteBackRound's old alternatives, the three 8-name lists, the StrayLoose round alternatives and the Promises prefixes; GREEN once each names the round by its Forecasting namespace only.
```

```text
Re-adding an old full name to any regex field or literal, an old alternative to TheWriteBackRound, or an extra path to the exclusion list fails naming it.
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

LicensingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

PromisesUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

ForecastingUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Extend

```text
Existing owner of this rule, consumer or test; only its lists, literals or using lines follow the move.
```

WriteBackRoundBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs\:1

CreateNew

```text
No existing rule class owns the round's own boundaries; built from ModuleRules.OutboundEdges and ModuleWording.
```

Forecasting

ARCHITECTURE\.md\:1

Extend

```text
Existing owner; only using lines, literals or prose follow the move.
```

write\-back

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1

Extend

```text
Existing owner; only using lines, literals or prose follow the move.
```

feature\-delta

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md\:1

Extend

```text
The current authority document for this fact; extended in place, no second document.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Changed

```text
TheWriteBackRound narrowed to ^Lighthouse.Backend.Modules.Forecasting.Application.WriteBack namespace only; no old alternative.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
8-name TheWriteBackRound list and its no-namespace comment deleted; predicate reads only ModuleRules.TheWriteBackRound.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
The 8 round alternatives leave StrayLooseImplementation and StrayLooseInterfaces (or are replaced by new full names if S1 found no other Work Items rule refusing the edge).
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:1

Changed

```text
WriteBack and IWriteBack prefixes and the write-back newcomer case leave; R-WB1 takes the newcomer.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Changed

```text
Placement covers the WriteBack sub-folder if S2 did not need it already.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs\:1

Changed

```text
R-WB1 to R-WB4 with planted falsifiers, lists inline by full name, compared both ways, ModuleWording for the round.
```

docs

ARCHITECTURE\.md\:1

Changed

```text
Section 4 Forecasting row gains Application.WriteBack; Forecasting rules bullet (inbound count, licence edges, Tracker integration edge, R-WB); Flow metrics count; section 3 namespace list. Prose, reviewed, no test.
```

docs

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1

Changed

```text
Left open write-back answered (Forecasting Application/WriteBack); edge rows Tracker integration to Forecasting (one listed type) and platform lane to Forecasting (listed, S09a note); layout line; glossary term write-back round if absent. Prose, reviewed.
```

docs

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md\:1

Changed

```text
Catalogue note: S03d's last named exception closed; S09a note on inverting the queue flush.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Narrowing and new rules over an already moved round; no preparatory move; S1 and S2 oracles stay green across it by the admission set.
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

###### L3

```text
Namespace only: after S3 no regex field or string literal of the test project outside the exact exclusion list matches an old full name or old path of a round type, and TheWriteBackRound matches exactly the new namespace.
```

Invariant

Infrastructure

WriteBackRulesNameOnlyForecastingArchUnitTest

###### Treatment

Finite

###### Cases

```text
Old alternative left in TheWriteBackRound: refused
```

```text
8-name list left in a boundaries test: refused
```

```text
StrayLoose round alternative left: refused
```

```text
Extra exclusion path: refused
```

```text
Finite source text and field set of one test project.
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
Rules name the write-back round by its Forecasting namespace only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRulesNameOnlyForecastingArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackRulesNameOnlyForecastingArchUnitTest
```

###### T3

```text
R-WB1 each round type lives in Forecasting Application WriteBack, no WriteBack or IWriteBack type under Services; R-WB2 inside Forecasting only WriteBackService reaches Tracker integration, along 2 listed edges; R-WB3 Promises, Access and Licensing reach no round type; R-WB4 inside Forecasting only ForecastUpdater and the round reach the round.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackRoundBoundariesArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
