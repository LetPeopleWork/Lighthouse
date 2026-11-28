# Architecture brief: story\-6368\-s04b\-models\-to\-kernel

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
No schema or behaviour change: the Story only moves types; no entity, DbSet, migration, model snapshot, route, DTO shape, payload, usage-data event or configuration key changes, so Lighthouse.Migrations.*, Lighthouse.Frontend and Lighthouse.EndToEndTests stay byte-identical.
```
Governed slices: S1, S2, S3, S4

#### D2

```text
Kernel is the three base types: ADR-270 Decision 7 allows shared ids, truly shared value objects and the event base type; Lighthouse has no shared id or value object type (ids are int), so the Kernel rows are IDomainEvent, IEntity and IConcurrencyTokenEntity only. Widely used types (StateCategories, InstanceCalendar, WorkTrackingSystems) are not promoted: each has an owner in ADR-270's context table, and calling them kernel would hide that owner's published language.
```
Governed slices: S1, S4

#### D3

```text
One catalogue row per Models type: KernelCatalogue (test side, Tests/Architecture) holds one row per top-level Models type, standing Kernel or Owned(owner context or Platform lane, blockers, freeing Story). Blockers are a closed set, each a property measured from the IL or the EF model, never a name: Entity (in LighthouseAppContext's model), Wire (reachable from a controller action signature or an API.DTO public property, transitively), HeldBy(type) (a staying Models type references it), Shared (IL users in two or more Modules.<X> contexts), InfraReached (named by Data, a repository or the composition root), PathPinned(consumer) (a non-test consumer reads its file by path), Event (implements IDomainEvent). A row with no blocker is a mover and is allowed only while it is on the shrink-only movers list.
```
Governed slices: S1, S2, S3, S4

#### D4

```text
Move whole files only: a file moves only if all its top-level types are free and share one owner; no file split and no rename, so each move commit is a pure move (refactor(kernel): move ...). Files of mixed standing wait for S12c.
```
Governed slices: S2, S3

#### D5

```text
Destination follows measured IL edges: a context-owned mover goes to Modules/<Owner>/Domain when its IL edges reach only the three kernel types, the BCL and that module's Domain, otherwise to Modules/<Owner>/Application; a Platform-owned mover goes to Platform/<Area>. No Contracts folder receives a mover and no new module folder is created; an owner without a module keeps its row Owned with its blocker.
```
Governed slices: S2, S3

#### D6

```text
Rules see the move before it happens: NamespaceMoveLaw is already generic (NamespaceMove plus PatternRoleMap), so it is reused unchanged and fed a new Models pattern-role map (Models.* old namespaces in role Place for KernelRules.TheModels, Context for ReportsModuleArchUnitTest.ReportModelsPattern and every module pattern) plus a rename list fixed in S1 from the pre-move literal sweep; the list only shrinks and is empty after S4.
```
Governed slices: S1, S4

#### D7

```text
Drop the event-move slice: Story 6382 already moved every domain event type into its publisher's Modules/<Publisher>/Contracts; only IDomainEvent stays in Models/Events, as a Kernel row. The design's former S5 (rules by namespace only and docs) is this feature's S4.
```
Governed slices: S1, S4

#### D8

```text
Wire types and entities stay owned: wire types wait for S12c or S09a's contracts and entities for S12c or S13, because both EF snapshots carry entity CLR names; their rows carry the measured blocker and the freeing Story.
```
Governed slices: S1

#### D9

```text
Kernel-size rule on existing ratchet parts: KernelCatalogueArchUnitTest compares the catalogue with the IL both ways through InlineBaseline, and KernelRules gains the rule that every Kernel row is used by two or more contexts; failures name the type and the fix.
```
Governed slices: S1, S4

#### D10

```text
Unit tests move with their production type: unit tests of moved types (Tests/Models/**, Tests/Services/Implementation/DeliverySources/DeliverySourceBindabilityTest.cs) move to Tests/Modules/<Owner>/<Layer> with namespace equal to folder, kept there by each module's existing unit-test placement rule; port, host and wire tests stay and change only using lines.
```
Governed slices: S3

#### D11

```text
Commit per slice and push once: each GREEN slice is a plain git commit with hooks; moves commit apart from edits; no slice touches Program.cs; S1 to S4 are pushed together after the candidate review and EXAMINE, so one CI run covers the Story.
```
Governed slices: S1, S2, S3, S4

#### D12

```text
Behaviour-free proof reuses existing probes: architecture rules (all), ef-model-unchanged, the unchanged backend suites on both revisions, and test-selection-show for the moved OAuth file; no new compare command.
```
Governed slices: S2, S3, S4

#### D13

```text
Mutation bar on moved files: Stryker.NET runs last on frozen code over the moved production files; the bar is no new survivor or no-coverage mutant against the same files on the pre-move revision. Enums and constant classes yield no meaningful mutants, stated as not applicable.
```
Governed slices: S2, S3

#### D14

```text
No formal model, exhaustive checks instead: every claim is a relation over a closed finite set (the Models types of one compiled assembly times the catalogue times measured IL and EF properties) with no shared mutable state, multi-step protocol, concurrency, retry, crash window or progress claim. Claim-to-checker: K1 to K5 and the move law are checked by native NUnit tests that evaluate the real assembly exhaustively (the executable projection is the check itself, in the default suite); no Quint, TLA+/TLC or Agda model, so no checker probe, install or waiver is needed. Property-based testing is not applicable: the domain is closed and enumerated in full.
```
Governed slices: S1, S2, S3, S4

#### D15

```text
Algebra lives only on the test side: runtime layers (domain, application, adapters, infrastructure) keep their states, operations and observations unchanged. The test-side carriers are KernelCatalogue rows (records owned by KernelCatalogue in Tests/Architecture), KernelRuleVerdict for rule outcomes (owned by KernelRules), NamespaceMoveLaw's PatternBreak for move-law breaks, and NUnit failure messages as the error surface; there is no per-process state. Operations: measure a type's blockers from IL and EF model, compare catalogue and IL both ways, evaluate a pattern map over a move.
```
Governed slices: S1, S4

#### D16

```text
Keep the object-oriented paradigm: the repository is object-oriented (CLAUDE.md); the catalogue is a first-class collection of row records with the decision on each row (is its claim true), measured through IlDependencies; no new production type is created.
```
Governed slices: S1

#### D17

```text
Rigor per slice as proposed: S1 thorough (opus for acceptance designer and crafter, sonnet for reviewers) because the measured blockers and the move law are where earlier reviews found misses; S2, S3 and S4 standard (sonnet, reviewers haiku). Pass model explicitly on every dispatch.
```
Governed slices: S1, S2, S3, S4

#### D18

```text
Confirm the open-question defaults: AC 1 restated as Kernel or owned with a measured blocker and freeing Story (S4 amends the slice-12 brief); kernel is the three base types; files of mixed standing are not split; a later Story that frees a row fails K2 until it updates or moves that row; ConnectionFetchShapeChanged and Models/WriteBack ownership follow where Stories 6382 and 6657 put them, as S1 measures.
```
Governed slices: S1, S4

#### D19

```text
Revise S2 and S3 targets after S1 measures: the recorded movers are the design's expected list; S1's IL measurement is the authority on which files are free and on each destination layer. Before the S2 and S3 acceptance-designer dispatches, the host reopens this design (design open --from-current) with S1's measured movers so the architect removes blocked files, corrects destination layers, and adds every consumer file whose using lines change.
```
Governed slices: S2, S3

### Constraints

#### K1

```text
S1 starts only after the predecessors are merged: for each of Stories 6365, 6366, 6468, 6657 and 6382, the ADO state is Closed, git log main --grep finds the Story's final commit and git merge-base --is-ancestor holds, and the content check holds (Modules/Refinement, Modules/Reports, Modules/OrganisationSetup present; no Services/{Implementation,Interfaces}/{Refinement,Reports}; Modules/*/Contracts with its contracts-only rule; no loose Services/Implementation/WriteBack*.cs). A failed check stops S1 and is reported with the Story, the check and its output.
```
Governed slices: S1

#### K2

```text
S1 re-measures R1 to R11 from the IL and the EF model (every Models type, entity names in both snapshots, wire reachability, held-by closure, users per context, Contracts placement, the write-back owner, rule-file literals, NamespaceMoveLaw shape, non-test path consumers, S12c and S13 state); every count in the feature-delta is a noisy text measurement and a difference is recorded, not argued.
```
Governed slices: S1

#### K3

```text
Contexts are read from Modules.<X> namespaces only; API, Data, Platform.*, repositories, the composition root and loose Services are not contexts.
```
Governed slices: S1, S4

#### K4

```text
No bound on catalogue size is set by any authority; the catalogue starts at the IL count S1 measures and may only shrink (rows leave when their type leaves Models).
```
Governed slices: S1, S4

#### K5

```text
At most four slices in one nwdes feature, each about one day or less; slices run in order S1, S2, S3, S4.
```
Governed slices: S1, S2, S3, S4

#### K6

```text
Tests follow .claude/skills/test-writing/SKILL.md and references/backend.md, which win over nWave's own timing and PBT-density rules; architecture rules are native NUnit plus ArchUnitNET or IL tests under Tests/Architecture; behaviour is observed through the unchanged acceptance tests at the application-service port; no nWave scenario tags in code, so an untagged test is correct here.
```
Governed slices: S1, S2, S3, S4

#### K7

```text
Inside a slice the fast loop is Lighthouse.Backend/Test-Fast.ps1 (builds first) plus the slice oracle; the slice's verification commands run once at its end. Live connector categories are never run.
```
Governed slices: S1, S2, S3, S4

#### K8

```text
Non-test path consumers stay working: Scripts/test-selection/path-classifier.sh (SHARED_REGEX, JIRA_REGEX) and Lighthouse.Frontend/src/services/UsageData/usageDataRouteKeys.test.ts, which reads Models/UsageData/UsageDataRouteKey.cs and UsageDataRoutePatterns.cs by path; those two files stay (PathPinned).
```
Governed slices: S2, S3

#### K9

```text
Comments are rare and written for a stranger; no internal id (decision, slice or ADR number) is the only reason in a comment.
```
Governed slices: S1, S2, S3, S4

### Reuse

KernelRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:14

Extend

```text
Owns the kernel rules (Models does not depend on Services, Modules, Platform or the update queue) run on real and planted types; it gains the rule that every Kernel row is used by two or more contexts.
```

KernelRuleVerdict

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:55

Reuse

```text
Already carries examined types, offenders and the rule text for kernel-rule failures; the new rule returns the same verdict.
```

InlineBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs\:9

Reuse

```text
Owns the both-ways compare of an inline list with a measured set; K1 and the shrink-only movers list use it rather than a second compare.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
Owns IL edge reading through Mono.Cecil, async bodies included; HeldBy, Shared, Wire and InfraReached are measured with it.
```

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:114

Reuse

```text
Already generic over a NamespaceMove and a PatternRoleMap and used by several moves; the Models move adds a role map and a rename list, not a second law.
```

PlatformLaneMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:47

Reuse

```text
Namespace-agnostic membership shape copied for the catalogue; its lane rules keep the Platform movers in the lane.
```

ContextContractsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembership\.cs\:20

Reuse

```text
Decides cross-context references through Contracts; the catalogue decides only Models standing, so they do not decide the same fact; S1 checks the actual scope and records any overlap.
```

### Impact

migrations

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\:1

UnchangedCompatible

```text
No entity moves, so the model snapshot and migrations stay byte-identical.
```

migrations

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\:1

UnchangedCompatible

```text
No entity moves, so the model snapshot and migrations stay byte-identical.
```

frontend\-test

Lighthouse\.Frontend\/src\/services\/UsageData\/usageDataRouteKeys\.test\.ts\:21

UnchangedCompatible

```text
Reads Models/UsageData/UsageDataRouteKey.cs and UsageDataRoutePatterns.cs by path; both files stay.
```

ci\-script

Scripts\/test\-selection\/path\-classifier\.sh\:1

UnchangedCompatible

```text
Its Models regexes name files that stay; the only mover it can see (Models/OAuth/OAuthCompleteResult.cs) lands in Modules/TrackerIntegration/Application, already in SHARED_REGEX; S3 checks with test-selection-show.
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs\:1

UnchangedCompatible

```text
Names no mover; S1 re-greps it with .editorconfig, the Stryker config and appsettings files.
```

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes: System.Text.Json writes no type names, Swashbuckle uses short schema ids, no type-name reflection exists, and no entity moves, so no producer or consumer outside the compiled assembly sees a namespace.
```

contract_tests

```text
Every caller of a moved type is inside the one backend deployable and changes in the same commit; no persisted or wire data names a namespace, so the primary oracle and the unchanged suites cover them.
```

### Boundaries

```text
Unchanged at runtime (controllers, the update queue, the event dispatcher). The oracles' driving port is the compiled Lighthouse.Backend assembly read through IlDependencies and the EF model of LighthouseAppContext, in-process; nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Driven ports

```text
Unchanged: LighthouseAppContext, repositories and tracker connectors; no new external call.
```

```text
Models stays the bottom layer and depends on nothing above it (Models does not depend on Services, Modules or Platform); moved types land in Modules/<Owner>/{Domain,Application} or Platform/<Area> and may depend downward on Models.
```

#### Failures

##### B1

```text
A Models type has no catalogue row.
```

Refusal

```text
Failure names the type: add its row with its owner and measured blocker, or move it to Modules/<Owner> or Platform/<Area>; why: ADR-270 Decision 7 lets the kernel hold only shared ids, value objects and the event base type.
```

##### B2

```text
A row's blocker is false when measured.
```

Refusal

```text
Failure names the type and the blocker: give it its true blocker or move it to its owner.
```

##### B3

```text
A free row is not on the movers list, or the movers list is not empty after S4.
```

Refusal

```text
Failure names the type and its destination: move it there.
```

##### B4

```text
A blocker cannot be measured (IL body unreadable).
```

Indeterminate

```text
Failure says could not verify, names the type and the unreadable member; never counted as true.
```

### Observations

```text
Architecture suite outcome per Models type: pass, or a failure naming the type, its false claim and the fix.
```

NUnit outcome with message

```text
Runtime behaviour of the type; a blocker that holds for a reason the closed blocker set does not name.
```

```text
Unchanged backend suites (application-service port acceptance tests, unit tests) pass on the revision before S1 and after S4 with unchanged assertions.
```

NUnit outcomes per test

```text
Behaviour no existing test observes; that is why the change is restricted to moves with unchanged bodies.
```

### Equality

catalogue

```text
Two catalogues are equal when they hold the same set of rows (type full name, standing, owner, blocker kinds, freeing Story); row order and file layout are ignored.
```

### Constructors

KernelCatalogue\.Kernel

Kernel\(string fullName\) \-\> CatalogueRow

KernelCatalogue\.Owned

Owned\(string fullName\, Owner owner\, Blocker first\, params Blocker\[\] more\, FreeingStory by\) \-\> CatalogueRow

KernelCatalogue\.Mover

Mover\(string oldFullName\, string newFullName\, Owner owner\) \-\> MoverRow

### Laws

#### L1

```text
The unchanged backend suites give the same outcome on the revision before S1 and after S4, with no assertion changed (only using lines in tests of moved types).
```

Refinement

Application

unchanged backend suites

##### Treatment

Finite

###### Cases

```text
full filtered dotnet test on both revisions
```

```text
ef-model-unchanged on both revisions
```

```text
empty diff under Lighthouse.Migrations.*, Lighthouse.Frontend and Lighthouse.EndToEndTests
```

```text
Two revisions and three fixed checks: the domain is the finite set of existing suites, so each is run once per revision.
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
A Models type without a catalogue row, or a row whose blocker is false, cannot pass the Architecture suite.
```

```text
A new context type lands in the shared kernel unseen, or a stale reason keeps a free type in Models.
```

KernelCatalogueArchUnitTest

```text
The compiled Lighthouse.Backend assembly and LighthouseAppContext's EF model.
```

KernelCatalogue rows measured through IlDependencies

KernelCatalogueArchUnitTest and KernelRules

```text
dotnet test on the Architecture namespace fails naming the type, the claim and the fix.
```

```text
A type compiled into another assembly under the Models namespace, or generated at runtime, is not scanned; none exists today (no source generators emit Models types, no Reflection.Emit). Remaining check: K1 enumerates the one production assembly only, stated in its message.
```

### States

Admitted

```text
Kernel row for one of the three base types.
```

None.

Admitted

```text
Owned row whose every blocker is measured true.
```

None.

Admitted

```text
Mover row on the shrink-only movers list, until S4 empties it.
```

None.

Excluded

```text
A Models type with no row.
```

None.

Excluded

```text
A row naming a type not in Models.
```

None.

Excluded

```text
A row with a false blocker.
```

None.

Excluded

```text
A free row not on the movers list, or any mover after S4.
```

None.

Unknown

```text
A blocker IlDependencies cannot measure (unreadable body): reported as could-not-verify, never as true.
```

S1 acceptance designer

### Models

None.

### Closure

```text
catalogue
```

Safety

Pass

DesignedNotBuilt

```text
K1 and K2 exclude an off-catalogue type and a false blocker; planted-type falsifiers in S1 and S4 show the exclusion fails loudly.
```

##### Evidence

None.

```text
catalogue
```

Inhabitation

Pass

DesignedNotBuilt

```text
The real assembly yields a valid catalogue: three Kernel rows and Owned rows with measured blockers, built by KernelCatalogue's constructors from the IL.
```

##### Evidence

None.

```text
catalogue
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly + EF model -> KernelCatalogue measured by IlDependencies -> K1 to K5 -> dotnet test on the Architecture namespace, exit and message.
```

##### Evidence

None.

```text
catalogue
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: each check is one synchronous evaluation over a finite set with no state, scheduling or eventual progress.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: a mention in API/DTO means on the wire. Controllers return Models types directly.
```

```text
Wire rows are true.
```

```text
More rows marked Wire than the text scan found.
```

```text
A wire type moving without its contract.
```

```text
None: R3 measures reachability from controller signatures and API.DTO properties.
```

S1 acceptance designer

#### Y2

Stressor

```text
Assumption negated: a single-context type is free. A staying type may hold it, also through extension-method calls the text scan misses (BlackoutDaysExtensions).
```

```text
HeldBy rows are true.
```

```text
Fewer movers.
```

```text
Moving a held type, which breaks Models does not depend on Modules.
```

```text
None: the IL closure measures it.
```

S1 acceptance designer

#### Y3

Stressor

```text
Assumption negated: only tests read Models paths. The frontend test and the CI classifier read them.
```

```text
Path consumers keep working.
```

```text
PathPinned rows.
```

```text
A pinned file moving and breaking the frontend test or test selection.
```

```text
None: PathPinned plus the classifier test.
```

S1 acceptance designer

#### Y4

Stressor

```text
Assumption negated: predecessors left Models as measured on 2026-11-24. Story 6382 moved the event types; 6657 placed the write-back round.
```

```text
Catalogue matches the IL at S1.
```

```text
Different movers and owners than expected.
```

```text
Catalogue built from the feature-delta's text counts.
```

```text
S2 and S3 targets revised from S1's measurement.
```

host and architect

#### Y5

Stressor

```text
Assumption negated: a blocker stays true. S12c or S13 will free rows.
```

```text
K2 names the freed type.
```

```text
A later Story fails until it updates or moves the row.
```

```text
A stale blocker passing.
```

```text
Accepted friction, stated in the failure message.
```

owner of the freeing Story

#### Y6

SinglePointOfFailure

```text
IlDependencies is the one reader for HeldBy, Shared, Wire and InfraReached.
```

```text
Blockers measured from IL, async bodies included.
```

```text
Could-not-verify reaches the summary.
```

```text
An unreadable body counted as no edge.
```

```text
Its existing planted-type tests; a could-not-verify result fails K2.
```

S1 acceptance designer

#### Y7

Bypass

```text
Reflection, Type.GetType, serializer type names or another assembly could reach or define Models types outside the IL scan.
```

```text
Namespace moves change no JSON or API document.
```

```text
None.
```

```text
A runtime lookup by full name breaking after a move.
```

```text
Unreachable today: no JsonDerivedType, TypeNameHandling, Type.GetType or CustomSchemaIds; S1 re-greps (R10).
```

S1 acceptance designer

#### Y8

Stressor

```text
Second set, walked over the finished design (walkthrough, not execution): a moved enum persisted as int; a Swagger schema-name clash; DI registration by namespace scan.
```

```text
Stored values, API document and registrations unchanged.
```

```text
None.
```

```text
Changed persisted value, schema id or registration.
```

```text
None found: no entity moves and enum values are unchanged; schema ids are short names; registrations name types, not namespaces.
```

S4 reviewer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Extend

```text
Its Models pattern is widened where the move law requires, and KernelRuleVerdict leaves the file in the prefactoring commit.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRuleVerdict\.cs

Create

```text
Prefactoring: one type per file before KernelRules changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
ReportModelsPattern treats Models.Reports as part of Reports; widened so it keeps seeing the Reports movers.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~KernelCatalogueArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogue\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModelsPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModelsStringsTheMoveRenames\.cs

None.

None.

#### Public oracle

```text
Run the Architecture tests on the compiled assembly before any file moves: KernelCatalogueArchUnitTest compares the catalogue with the IL and the EF model both ways, measures every row's blockers, reads the shrink-only movers list, and evaluates NamespaceMoveLaw over the Models pattern-role map and rename list.
```

```text
Every top-level Models type has exactly one row and every row names a Models type; every Owned row's blockers are true by measurement; every type with no blocker is on the movers list with its old and new namespace and owner. RED first on the movers' rows and on the move law for KernelRules.TheModels and ReportModelsPattern; GREEN once the widened patterns are in; the unchanged backend suites pass.
```

```text
A planted Lighthouse.Backend.Models fixture type used by one module and absent from the catalogue fails naming itself; a row claiming Wire for a type no controller reaches fails naming the type and the blocker; a free type left off the movers list fails naming its destination.
```

##### Promises

P1

P4

#### Prefactoring

```text
Per touched file. KernelRules.cs holds KernelRules and KernelRuleVerdict, not variants of one sum type: split-now, KernelRuleVerdict moves to Tests/Architecture/KernelRuleVerdict.cs in its own refactor commit before the catalogue work; this makes the S4 kernel rule addition touch a one-type file. ReportsModuleArchUnitTest.cs holds one type: not-applicable, nothing moves.
```

```text
Every kernel rule's outcome and message; KernelRulesCatchANewOffenderArchUnitTest unchanged and green before and after.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRulesCatchANewOffenderArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~KernelRulesCatchANewOffenderArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:16

Changed

```text
TheModels widened per the move law until S4 narrows it.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:55

Migrated

```text
KernelRuleVerdict moves to KernelRuleVerdict.cs unchanged.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:43

Changed

```text
ReportModelsPattern widened per the move law until S4 narrows it.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRuleVerdict\.cs\:1

Migrated

```text
Receives KernelRuleVerdict unchanged from KernelRules.cs.
```

##### Agreement

None.

##### Not applicable

reuse

```text
The S1 targets extend existing owners recorded in the shared reuse analysis: KernelRules (Extend, owner of TheModels) and ReportsModuleArchUnitTest's ReportModelsPattern (owner of the Reports view of Models.Reports, widened for the movers); KernelRuleVerdict.cs only receives the existing KernelRuleVerdict record unchanged in the prefactoring split. No new responsibility is created.
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
K1: every top-level type in Lighthouse.Backend.Models has exactly one catalogue row and every row names an existing Models type.
```

Invariant

Infrastructure

KernelCatalogueArchUnitTest

###### Treatment

Finite

###### Cases

```text
real assembly: every type matched
```

```text
planted fixture type absent from the catalogue: fails naming itself
```

```text
row for a type that does not exist: fails naming the row
```

```text
The domain is the closed set of Models types in one compiled assembly; the test enumerates all of them.
```

###### Evidence

None.

###### L5

```text
K2: every Owned row's blockers are true when measured from the IL and the EF model (Entity, Wire, HeldBy, Shared, InfraReached, PathPinned, Event); a type with no true blocker is on the movers list.
```

Invariant

Infrastructure

KernelCatalogueArchUnitTest

###### Treatment

Finite

###### Cases

```text
real rows: all true
```

```text
row claiming Entity for a non-entity: fails
```

```text
row claiming Wire for a type no controller reaches: fails
```

```text
free type off the movers list: fails naming its destination
```

```text
Each blocker is a finite measurement over the same closed set of types.
```

###### Evidence

None.

###### L6

```text
Move law: every governed rule pattern keeps (role Context) or drops (role Place) each mover under its new namespace, as the Models pattern-role map declares; the rename list only shrinks.
```

Invariant

Infrastructure

KernelCatalogueArchUnitTest

###### Treatment

Finite

###### Cases

```text
TheModels and ReportModelsPattern before widening: RED
```

```text
after widening: GREEN
```

```text
an undeclared pattern naming a mover: fails naming the field
```

```text
The domain is the finite set of rule-pattern fields in the test assembly times the movers.
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

###### T3

```text
The catalogue is closed both ways with measured blockers (K1, K2) and the rules see the move (move law).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~KernelCatalogueArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataEmitPermit\.cs

Create

```text
Expected Platform mover; S1's measurement confirms it or the design revision removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEmitPermit\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Platform/UsageData/UsageDataEmitPermit.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataSuppressionReason\.cs

Create

```text
Expected Platform mover; S1's measurement confirms it or the design revision removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataSuppressionReason\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Platform/UsageData/UsageDataSuppressionReason.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretReadResult\.cs

Create

```text
Expected Platform mover; S1's measurement confirms it or the design revision removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Encryption\/SecretReadResult\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Platform/Encryption/SecretReadResult.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RefreshSubject\.cs

Create

```text
Expected Platform mover; S1's measurement confirms it or the design revision removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Logging\/RefreshSubject\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Platform/Logging/RefreshSubject.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs

Extend

```text
Names a Platform mover by its Models full name; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs

Extend

```text
Names a Platform mover by its Models full name; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformOwnedModelsTypesLiveInPlatformArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~PlatformOwnedModelsTypesLiveInPlatformArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogue\.cs

None.

None.

#### Public oracle

```text
Run the Architecture tests after the Platform-owned movers moved: the oracle reads each Platform mover's catalogue row and looks the type up in the compiled assembly.
```

```text
Each Platform mover exists only under Lighthouse.Backend.Platform.<Area> with the same public members, its row names that namespace, and the Platform lane rules pass. RED before the move (the type is still under Models); GREEN after; the unchanged backend suites pass.
```

```text
Leaving a mover under Models, or moving it into a module instead of Platform/<Area>, fails naming the type and its expected namespace.
```

##### Promises

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

None.

##### Impact

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEmitPermit\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Platform/UsageData/UsageDataEmitPermit.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataSuppressionReason\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Platform/UsageData/UsageDataSuppressionReason.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Encryption\/SecretReadResult\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Platform/Encryption/SecretReadResult.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Logging\/RefreshSubject\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Platform/Logging/RefreshSubject.cs; consumers change only using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:47

Changed

```text
Lane membership names the movers under Platform.<Area>.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:1

Changed

```text
Pattern-role literal for a mover renamed (S2) or its widened Models alternative removed (S4).
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataEmitPermit\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataSuppressionReason\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretReadResult\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RefreshSubject\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

##### Agreement

None.

##### Not applicable

reuse

```text
A pure move keeps each existing type unchanged; its destination folders Platform/UsageData, Platform/Encryption and Platform/Logging already exist and are reused; no new responsibility is created.
```

prefactoring

```text
Per touched file. UsageDataEmitPermit.cs, UsageDataSuppressionReason.cs, SecretReadResult.cs, RefreshSubject.cs: one type each, not-applicable, they move whole. PlatformLanePatternRoles.cs: one type, not-applicable. PlatformLaneMembership.cs: holds several declarations of the lane map; not-applicable in this slice because the slice changes only full-name strings in it, and the accepted design keeps this Story to pure moves plus literal renames; a split is offered to the improvement backlog.
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
Each Platform mover exists only under its Platform.<Area> namespace and its catalogue row names that namespace.
```

Invariant

Infrastructure

PlatformOwnedModelsTypesLiveInPlatformArchUnitTest

###### Treatment

Finite

###### Cases

```text
each mover found at Platform.<Area>
```

```text
a mover still under Models: fails
```

```text
a mover under Modules: fails
```

```text
The domain is the finite list of Platform movers S1 measured.
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
The Platform lane keeps holding its types after the move.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~PlatformLaneMembershipArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EmbedSessionTokenRedemption\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Auth\/EmbedSessionTokenRedemption\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/EmbedSessionTokenRedemption.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/RbacGuardRequirement\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacGuardRequirement\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/RbacGuardRequirement.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/RbacOperationResult\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacOperationResult\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/RbacOperationResult.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Domain\/DeliverySourceDescriptor\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceDescriptor\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Domain/DeliverySourceDescriptor.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/DeliverySourceBindability\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceBindability\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/DeliverySourceBindability.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/DeliverySourceOption\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceOption\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/DeliverySourceOption.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCompleteResult\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCompleteResult\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/OAuth/OAuthCompleteResult.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/RealityCheckVerdictTypes\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Forecast\/RealityCheckVerdictTypes\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Forecasting/Domain/RealityCheckVerdictTypes.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/ManualRankComparer\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/ManualRankComparer\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Application/ManualRankComparer.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/OwnersWhoseFetchMoved\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OwnersWhoseFetchMoved\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Domain/OwnersWhoseFetchMoved.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/AdditionalFieldInUse\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/AdditionalFieldInUse\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Domain/AdditionalFieldInUse.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportOwnerGoneException\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportOwnerGoneException\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Reports/Application/ReportOwnerGoneException.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportRefusalCodes\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/Reports/Domain/ReportRefusalCodes.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/RuleSetSemantics\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkItemRules\/RuleSetSemantics\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/Domain/RuleSetSemantics.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/SpecialCauseType\.cs

Create

```text
Expected context mover; S1's measurement confirms it and its layer (Domain or Application by IL edges), or the design revision corrects or removes it.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/SpecialCauseType\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/Domain/SpecialCauseType.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/ManualRankComparerTest\.cs

Create

```text
Unit test moves beside its production type, namespace equal to folder, kept there by the module's unit-test placement rule.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/ManualRankComparerTest\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Application/ManualRankComparerTest.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/OwnersWhoseFetchMovedTest\.cs

Create

```text
Unit test moves beside its production type, namespace equal to folder, kept there by the module's unit-test placement rule.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/OwnersWhoseFetchMovedTest\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Domain/OwnersWhoseFetchMovedTest.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/AdditionalFieldInUseTest\.cs

Create

```text
Unit test moves beside its production type, namespace equal to folder, kept there by the module's unit-test placement rule.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/AdditionalFieldInUseTest\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Domain/AdditionalFieldInUseTest.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/DeliverySourceBindabilityTest\.cs

Create

```text
Unit test moves beside its production type, namespace equal to folder, kept there by the module's unit-test placement rule.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DeliverySources\/DeliverySourceBindabilityTest\.cs

Extend

```text
Removed by the move; the file's bytes reappear at Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration/Application/DeliverySourceBindabilityTest.cs with only namespace and using lines changed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AdditionalFieldInUseRulePurityArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationOAuthAndHealthLayoutArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

Extend

```text
Names a context mover or its Models namespace by literal; renamed per S1's rename list.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextOwnedModelsTypesLiveInTheirModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ContextOwnedModelsTypesLiveInTheirModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
bash Scripts/test-selection/test-path-classifier.sh
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogue\.cs

None.

None.

#### Public oracle

```text
Run the Architecture tests after the context-owned movers and their unit tests moved: the oracle reads each context mover's catalogue row and looks the type up in the compiled assembly.
```

```text
Each context mover exists only under Lighthouse.Backend.Modules.<Owner>.Domain or .Application as its row names, its unit tests sit under Tests/Modules/<Owner> in the matching layer, and every module's boundary and placement rules pass. RED before the move; GREEN after; the unchanged backend suites and the path-classifier test pass.
```

```text
Leaving a mover under Models, putting it in another module or the wrong layer, or leaving its unit test under Tests/Models, fails naming the type and its expected place.
```

##### Promises

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

None.

##### Impact

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Auth\/EmbedSessionTokenRedemption\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/EmbedSessionTokenRedemption.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacGuardRequirement\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/RbacGuardRequirement.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacOperationResult\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Access/Domain/RbacOperationResult.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceDescriptor\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Domain/DeliverySourceDescriptor.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceBindability\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/DeliverySourceBindability.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliverySources\/DeliverySourceOption\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/DeliverySourceOption.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCompleteResult\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/OAuth/OAuthCompleteResult.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Forecast\/RealityCheckVerdictTypes\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Forecasting/Domain/RealityCheckVerdictTypes.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/ManualRankComparer\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Application/ManualRankComparer.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OwnersWhoseFetchMoved\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Domain/OwnersWhoseFetchMoved.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/AdditionalFieldInUse\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/OrganisationSetup/Domain/AdditionalFieldInUse.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportOwnerGoneException\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Reports/Application/ReportOwnerGoneException.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/Reports/Domain/ReportRefusalCodes.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkItemRules\/RuleSetSemantics\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/Domain/RuleSetSemantics.cs; consumers change only using lines.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/SpecialCauseType\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/Domain/SpecialCauseType.cs; consumers change only using lines.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/ManualRankComparerTest\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Application/ManualRankComparerTest.cs; consumers change only using lines.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/OwnersWhoseFetchMovedTest\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Domain/OwnersWhoseFetchMovedTest.cs; consumers change only using lines.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/AdditionalFieldInUseTest\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/OrganisationSetup/Domain/AdditionalFieldInUseTest.cs; consumers change only using lines.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DeliverySources\/DeliverySourceBindabilityTest\.cs\:1

Migrated

```text
Pure move to Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration/Application/DeliverySourceBindabilityTest.cs; consumers change only using lines.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AdditionalFieldInUseRulePurityArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationOAuthAndHealthLayoutArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureOrderingSingleSourceArchUnitTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs\:1

Changed

```text
Literal for a mover renamed to its module namespace.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/EmbedSessionTokenRedemption\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/RbacGuardRequirement\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Domain\/RbacOperationResult\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Domain\/DeliverySourceDescriptor\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/DeliverySourceBindability\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/DeliverySourceOption\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCompleteResult\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/RealityCheckVerdictTypes\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/ManualRankComparer\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/OwnersWhoseFetchMoved\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Domain\/AdditionalFieldInUse\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportOwnerGoneException\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportRefusalCodes\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/RuleSetSemantics\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

production\-move

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/SpecialCauseType\.cs\:1

Migrated

```text
Receives the moved file; only namespace and using lines differ from the Models original.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/ManualRankComparerTest\.cs\:1

Migrated

```text
Receives the moved unit test beside its production type; assertions unchanged.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/OwnersWhoseFetchMovedTest\.cs\:1

Migrated

```text
Receives the moved unit test beside its production type; assertions unchanged.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Domain\/AdditionalFieldInUseTest\.cs\:1

Migrated

```text
Receives the moved unit test beside its production type; assertions unchanged.
```

test\-move

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/DeliverySourceBindabilityTest\.cs\:1

Migrated

```text
Receives the moved unit test beside its production type; assertions unchanged.
```

##### Agreement

None.

##### Not applicable

reuse

```text
A pure move keeps each existing type unchanged; the destination modules and their Domain and Application folders already exist and are reused; no new responsibility is created.
```

prefactoring

```text
Per touched file. Movers with one type each (EmbedSessionTokenRedemption.cs, RbacGuardRequirement.cs, DeliverySourceDescriptor.cs, DeliverySourceBindability.cs, OAuthCompleteResult.cs, ManualRankComparer.cs, OwnersWhoseFetchMoved.cs, ReportOwnerGoneException.cs, ReportRefusalCodes.cs, RuleSetSemantics.cs, SpecialCauseType.cs) and the four moved unit tests: not-applicable, they move whole. Movers holding several types (RbacOperationResult.cs, DeliverySourceOption.cs, RealityCheckVerdictTypes.cs, AdditionalFieldInUse.cs): not-applicable in this slice, because the accepted design moves a file whole and keeps every commit a pure move; their split waits for S12c. Rule files holding one type (AdditionalFieldInUseRulePurityArchUnitTest.cs, OrganisationSetupPatternRoles.cs, FeatureOrderingSingleSourceArchUnitTest.cs): not-applicable. Rule files holding several types (AccessApplicationLayoutArchUnitTest.cs, AccessAndLicensingMembershipArchUnitTest.cs, TrackerIntegrationMembershipArchUnitTest.cs, TrackerIntegrationOAuthAndHealthLayoutArchUnitTest.cs, OrganisationSetupMembership.cs, ReportsMembership.cs, EveryActionDeclaresItsAccessArchitectureTest.cs): not-applicable in this slice, because the slice changes only literal strings in them and their extra types are planted fixtures of the rule; a split is offered to the improvement backlog.
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
Each context mover exists only under Modules.<Owner>.Domain or .Application as its catalogue row names, and its unit tests sit in the matching Tests/Modules/<Owner> layer.
```

Invariant

Infrastructure

ContextOwnedModelsTypesLiveInTheirModuleArchUnitTest

###### Treatment

Finite

###### Cases

```text
each mover found at its row's namespace
```

```text
a mover still under Models: fails
```

```text
a mover in the wrong module or layer: fails
```

```text
a unit test left under Tests/Models: fails
```

```text
The domain is the finite list of context movers S1 measured.
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
Every module's boundary, layout and unit-test placement rules stay green with the moved types inside.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Extend

```text
Gains the rule that every Kernel row is used by two or more contexts; TheModels narrowed back to Models only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
ReportModelsPattern narrowed: Reports' types are named by module namespace only.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs

Extend

```text
Any widened Models alternative removed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs

Extend

```text
Any widened Models alternative removed.
```

ARCHITECTURE\.md

Extend

```text
Section 4 line 109 and the diagram node describe the kernel as the three base types and point to the catalogue.
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Decision 7 gains a status line: today three base types; every other Models type is listed with owner and blocker by the kernel catalogue.
```

docs\/feature\/epic\-6264\-modular\-monolith\/slices\/slice\-12\-s04b\-reduce\-models\-to\-kernel\.md

Extend

```text
AC 1 and the hypothesis restated as measured.
```

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md

Extend

```text
KPI 3 (Models files) re-measured.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModelsRulesNameOnlyTheOwnersArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ModelsRulesNameOnlyTheOwnersArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~KernelRulesCatchANewOffenderArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogue\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModelsStringsTheMoveRenames\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRulesCatchANewOffenderArchUnitTest\.cs

None.

None.

#### Public oracle

```text
Run the Architecture tests with the movers list and the rename list empty: the oracle sweeps every rule-pattern field and literal in Tests/Architecture for a Models name of a moved type (its own files and fixtures excluded by exact path, that exclusion list asserted equal to those paths), and plants a made-up Models type off the catalogue.
```

```text
No rule names a moved type by its old Models name and no widened Models alternative remains; the planted type fails naming itself and the fix; every Kernel row is used by two or more contexts; Models does not depend on Services, Modules, Platform or the update queue. RED while S1's widened alternatives remain; GREEN once narrowed; the unchanged backend suites pass. ARCHITECTURE.md and ADR-270 Decision 7 are prose, verified by review and EXAMINE, not pinned by a test.
```

```text
A rule keeping a Models alternative for a moved type, a non-empty movers or rename list, a planted Models type that passes, or a Kernel row used by one context, fails naming it.
```

##### Promises

P3

P5

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

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:16

Changed

```text
TheModels narrowed; new Kernel-row usage rule.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:43

Changed

```text
ReportModelsPattern narrowed.
```

docs

ARCHITECTURE\.md\:109

Changed

```text
Kernel described as it is after the Story.
```

docs

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1062

Changed

```text
Decision 7 status line.
```

docs

docs\/feature\/epic\-6264\-modular\-monolith\/slices\/slice\-12\-s04b\-reduce\-models\-to\-kernel\.md\:22

Changed

```text
AC 1 restated.
```

docs

docs\/feature\/epic\-6264\-modular\-monolith\/feature\-delta\.md\:1

Changed

```text
KPI 3 re-measured.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:1

Changed

```text
Pattern-role literal for a mover renamed (S2) or its widened Models alternative removed (S4).
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPatternRoles\.cs\:1

Changed

```text
Pattern-role literal for a mover renamed (S2) or its widened Models alternative removed (S4).
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per touched file. KernelRules.cs holds one type after S1's split: not-applicable. ReportsModuleArchUnitTest.cs, PlatformLanePatternRoles.cs and OrganisationSetupPatternRoles.cs hold one type each: not-applicable. ARCHITECTURE.md, the ADR-270 file, the slice-12 brief and the epic feature-delta are documents, not code: not-applicable.
```

reuse

```text
The S4 targets are the existing owners of the facts they change: KernelRules (Extend, shared reuse analysis) gains the Kernel-row usage rule; ReportsModuleArchUnitTest and the pattern-role files narrow their own patterns; NamespaceMoveLaw's existing old-path sweep is reused with an empty rename list; ARCHITECTURE.md, ADR-270, the slice-12 brief and the epic feature-delta describe the kernel. No new responsibility is created.
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

###### L7

```text
K3: after S4 the movers list and the rename list are empty and no rule pattern or literal names a moved type by its old Models name.
```

Invariant

Infrastructure

ModelsRulesNameOnlyTheOwnersArchUnitTest

###### Treatment

Finite

###### Cases

```text
real rules after narrowing: GREEN
```

```text
a Models alternative left for a mover: fails naming the field
```

```text
a non-empty movers list: fails naming the type
```

```text
The domain is the finite set of rule fields and literals in the test assembly.
```

###### Evidence

None.

###### L8

```text
K4: every Kernel row is an interface or base type used by two or more contexts in the IL.
```

Invariant

Infrastructure

KernelRules

###### Treatment

Finite

###### Cases

```text
IDomainEvent, IEntity, IConcurrencyTokenEntity: each used by two or more contexts
```

```text
planted kernel type used by one context: fails
```

```text
The domain is the three Kernel rows.
```

###### Evidence

None.

###### L9

```text
K5 (kept): Models does not depend on Services, Modules, Platform or the update queue.
```

Invariant

Infrastructure

KernelRules

###### Treatment

Finite

###### Cases

```text
real assembly: no offender
```

```text
planted offender in KernelRulesCatchANewOffenderArchUnitTest: fails naming it
```

```text
The domain is the finite set of Models types and their IL edges.
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
Rules name moved types by namespace only and a new Models type off the catalogue fails naming itself (K3, K1 planted).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModelsRulesNameOnlyTheOwnersArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ModelsRulesNameOnlyTheOwnersArchUnitTest
```

###### T5

```text
Kernel rows are shared by two or more contexts and Models depends on nothing above it (K4, K5), each with a planted offender.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRulesCatchANewOffenderArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~KernelRulesCatchANewOffenderArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
