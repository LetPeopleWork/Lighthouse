# Architecture brief: story\-6364\-s03f\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (D-0, D-11, D-12, P6): feature A changes no production code, schema, DI registration, configuration, GlobalSuppressions.cs, analyser config or Program.cs. Only test-side architecture rule files, Scripts/test-selection/path-classifier.sh, the EXAMINE command builder, its test script and .nwave/examine.json change. Nothing moves in feature A; the moves, the list and path strings that name a moved type and the suppression targets are feature B (story-6364-s03f-b-move); the platform lane rules R-PL1 to R-PL4 and unit-test placement are feature C. Data, migrations, every repository, the host band, context seeders and update pipelines stay (D-0, D-4, D-5, D-6).
```

#### D2

```text
Confirmed (D-1, D-2, D-3, D-5, D-6, D-7, E9, E13): the platform lane lives in Lighthouse.Backend.Platform.<Area>, namespace = folder, no layers; areas Encryption, DatabaseManagement, UsageData, Logging, DomainEvents, UpdateQueue, Releases, Host, DemoData, Seeding, Persistence. Membership is the ADR-270 platform set (about 137 files: key ring and secret custody, database management, demo data, ISeeder and InstallTimestampSeeder, LostRaceTolerantSave, usage data, logging, releases, the dispatcher with IDomainEventDispatcher and IDomainEventHandler, the clock and host facilities, the update queue mechanics). A Models type joins only under the four S03a conditions (not an EF entity, not on the wire, not reached by a kernel type, not used outside the lane). Stay with a named owner: Data, every repository with IRepository and RepositoryBase, the seven context seeders, TeamUpdater, PortfolioUpdater, UpdateServiceBase and their interfaces, AppSettingService. A1 measures the map from the IL (each type, area, old and new namespace; the files that stay with owner; the suppression and analyser-config targets naming a moving type; the list and path strings each B slice renames) and pins it; the text counts are not trusted.
```
Governed slices: S1

#### D3

```text
Confirmed (D-9, E1, Ratchet design): every const string field of the test assembly that parses as a regex is enumerated by reflection, so a pattern added later is covered. For every pattern P and every moving type T of the A1 map, Regex.IsMatch(old(T), P) == Regex.IsMatch(new(T), P). Patterns only gain alternatives of the form ^Lighthouse\.Backend\.Platform\.<Area>(\.|$) for the areas of the types the pattern matched before (ApplicationCore gains Platform; KernelRules TheServices and TheUpdateQueue; the Promises, Access and Licensing ThePlatform and TheUpdateCode; Tracker integration TheKeyRing and PromisesAndTheUpdateCode; Work Items OrganisationSetUp by the areas of the loose types; Forecasting, DependencyAwareForecastSeam, ModuleBoundaries line 19 and Refinement update code; the Promises (\.DomainEvents)? patterns). No added alternative matches a production type outside Platform. Old alternatives stay until feature C. The full list of failing patterns is measured at the A2 RED.
```
Governed slices: S2

#### D4

```text
Confirmed (E1, P2): HandlerSelection.ByDomainEventsNamespace in ApplicationCoreRules (line 182) stops matching the inline .Services.Implementation.DomainEvents fragment and selects the dispatcher by its exact type (typeof(DomainEventDispatcher), the only type in that namespace), so the selection follows the move by compilation; its readers in ApplicationCoreRulesReachModulesArchUnitTest (lines 159, 168) keep their verdicts.
```
Governed slices: S2

#### D5

```text
Confirmed (P3, Ratchet design): non-constant fragments escape reflection, so a second assertion reads the rule files as text: no string literal names a moving area (Encryption, DatabaseManagement, UsageData, Logging, DomainEvents, BackgroundServices.Update, Interfaces.Update) or a moving loose type by its old Services.Implementation, Services.Interfaces or Services/Implementation path, except the entries the A1 map lists as renamed by a B slice (E4, E5 list and path strings). Known today: ApplicationCoreRules.cs:182.
```
Governed slices: S2

#### D6

```text
Confirmed (D-12, E3): GITHUB_REGEX in Scripts/test-selection/path-classifier.sh (line 17) gains the Platform/Releases path of the production and test projects, so a change to Platform/Releases/GitHubService.cs, LighthouseReleaseService or their tests selects the GitHub live suite. A3 lands before B3 so the move push runs the GitHub suite. Every case already in test-path-classifier.sh keeps its answer.
```
Governed slices: S3

#### D7

```text
Confirmed (D-13, X3, E10): a new EXAMINE command platform-answers-compare (CREATE_NEW, thin, on the shared capture and compare pieces of build-examine-commands in the shape of access-answers-compare) starts each revision in-process with authentication off and reads the platform answers that need no network: GET systeminfo, version current, updateSupported and distribution (not hasupdate or new, which call GitHub), encryption and encryption/secrets, database-management/status, usagedata/state, logs level, level/supported and problems, update status. Volatile fields (uptime, memory, clock values) are blanked at any depth by its masked keys; which endpoints answer and which fields are volatile are measured in A4. IDENTICAL exit 0, DIFFERENT exit 1 naming the changed answer, and it fails when nothing was compared. Its compare stage is reachable on two capture folders (--compare-captures BASE CANDIDATE). Declared in .nwave/examine.json with a platform-answers-selftest probe before any file moves.
```
Governed slices: S4

#### D8

```text
Confirmed (D-13, D-14, brief decisions): rigor thorough for feature A (architect, acceptance designer, crafter opus; reviewers and EXAMINE sonnet); no review window; EXAMINE observes through architecture-rules, ef-model-unchanged, platform-answers-compare once A4 adds it, refresh-reactions-compare, test-selection-show, ci-live-suites-show and backend-tests. The mutation bar applies once at the end of S03f to the moved production code; feature A changes no production code. Usage-data event, UI sketch, loading and failure looks: not applicable.
```

#### D9

```text
Carriers: the platform lane map is a static class PlatformLaneMembership in the Architecture folder (shape of PromisesMembership and AccessAndLicensingMembership: per moving type its short or full name, area, old namespace and new namespace; the files that stay with owner; the suppression and analyser-config targets naming a moving type; the strings each B slice renames), owner the A1 oracle; rule patterns stay const strings in their test classes and ModuleRules, KernelRules and ApplicationCoreRules, owner each rule class; the IL is read through IlDependencies over Mono.Cecil; the dispatcher selection is HandlerSelection in ApplicationCoreRules, selecting the dispatcher and its nested (compiler-generated) types; the GitHub selection is GITHUB_REGEX in path-classifier.sh, owner that script; platform-answers-compare and platform-answers-selftest are generated bash scripts under Scripts/examine/bin, owner build-examine-commands. No production carrier, effect, error type or per-process state changes.
```

#### D10

```text
Formal verification not applicable (feature-delta Formal verification; quint 0.33.1 and OpenJDK 21 probed, no checker selected): no state, ordering, concurrency, recovery or liveness claim changes; the update queue, the locks and the dispatcher move unchanged in feature B. The preservation law of A2 is decided exhaustively over the finite domain (moving types x const patterns) by an executable check in the default suite; the membership is decided by the IL; the classifier and the compare command by enumerated cases. A Quint, TLA+ or Agda model would restate the namespace map and could not fail. Property tests not applicable: every domain is finite and enumerated exhaustively, so a generator adds no coverage. Paradigm object_oriented is kept; the targets are test-side rules and bash scripts outside the OO domain core, so no domain owner, value object or Object Calisthenics rule is in play.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTOs, responses, configuration keys, key-store names, Redis channels, the hub route, DI and Program.cs, log categories, the EF model and every suppression target stay the same in feature A; every existing test stays green.
```

#### K2

```text
Never loosen a rule: a widening adds only Lighthouse.Backend.Platform.<Area> alternatives for the areas of types the pattern matched before; no alternative may match a production type outside Platform; no existing assertion, listed entry or alternative is removed in feature A. Every rule edit is proved by a reverted throwaway probe.
```

#### K3

```text
IlDependencies.TypesIn also returns nested types; an exact-set check compares top-level types (the part before +). IlDependencies misses catch-clause, event and attribute named-argument references. Regex evaluation uses the existing pattern timeout.
```

#### K4

```text
One oracle file per slice; an approved oracle or support file of this feature is never edited, not even a using line; oracles name production types as strings. No nWave scenario tags in code; the Lighthouse test-writing skill wins over nWave timing and PBT defaults.
```

#### K5

```text
The EXAMINE capture runs with authentication off and no network: the GitHub calls (version hasupdate and new) are left to the GithubIntegration suite on the move push; Redis and Postgres update locks under a real cluster and a key ring on a mounted file are left to container and unit tests. No stored or validated input bound changes; none is introduced.
```

### Reuse

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
Reads types and edges from the compiled IL over Mono.Cecil; the A1 membership check and the A2 outside-Platform check read through it; no second IL reader.
```

AccessAndLicensingMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs\:11

Reuse

```text
Shape for PlatformLaneMembership with PromisesMembership: names without namespace so one list holds before and after the move, a full name only where a short name is shared. Platform is a lane, not a module, so a new list owns its own facts; no second owner of the platform membership exists.
```

AccessAndLicensingRulesSeeTheModulesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:1

Extend

```text
The rules-see-the-move idea is extended: instead of one made-up offender per known rule key, A2 checks every const pattern at once by a law over the A1 map, so a key the re-grep misses is still covered.
```

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Reuse

```text
Its module constants are among the patterns the law enumerates; Platform is not a module, so no PlatformModule constant is added and no rule is copied.
```

access\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:811

Reuse

```text
Closest precedent: its generated driver, masked keys and the shared compare-captures piece with --compare-captures carry platform-answers-compare; the new command needs other endpoints, its volatile-field mask and its own subject wording.
```

build\-examine\-commands

Scripts\/examine\/build\-examine\-commands\:1

Extend

```text
Owns every generated EXAMINE command and the shared pieces; gains platform-answers-compare and platform-answers-selftest, no copy of the pieces.
```

GITHUB\_REGEX

Scripts\/test\-selection\/path\-classifier\.sh\:17

Extend

```text
Sole owner of the GitHub live-suite selection by path, read by ci_changes.yml and dev-test.sh; widened in place, no second selector.
```

assert

Scripts\/test\-selection\/test\-path\-classifier\.sh\:14

Reuse

```text
The source-the-classifier and assert harness of test-path-classifier.sh is the shape of the new A3 oracle file (one oracle per slice); its existing cases are the preservation check of A3.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes shape: path-classifier.sh keeps its output keys and every existing selection, .nwave/examine.json gains entries within its existing format, the generated commands are read by the examiner only, and no wire, DTO, schema or persisted format changes.
```

contract_tests

```text
No producer and consumer change apart: one deployable; the classifier and its CI consumers change in the same commit and are covered by the classifier oracles.
```

### Boundaries

```text
Architecture rule tests over the compiled production IL and the test assembly (dotnet test, Architecture namespace); the path classifier driven by its bash test scripts; the EXAMINE command builder and its generated platform-answers-compare, driven by its bash test script
```

#### Driven ports

```text
IlDependencies module reader over Mono.Cecil, and reflection over the test assembly for const patterns
```

```text
git revisions and the backend HTTP API, run in-process with authentication off and no network, for the answers capture
```

```text
Tests and scripts read production; production never references a test, fixture or script.
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
Production unchanged; the platform lane map pinned from the IL; every const pattern gives the same answer for each moving type under its old and its new namespace; the dispatcher selected by its exact type; no old-path literal left except the listed B renames; Platform/Releases selects the GitHub suite; platform-answers-compare and its selftest declared.
```

story\-6364\-s03f\-a\-rules\-first

Excluded

```text
A moving type whose new namespace a pattern answers differently from its old one; an added alternative matching a production type outside Platform; a loosened rule or removed alternative; a classifier case changing its selection; an IDENTICAL verdict on a changed or empty comparison.
```

story\-6364\-s03f\-a\-rules\-first

Unknown

```text
The exact membership, the admitted Models types and the suppression and analyser-config targets (A1 measures); the full list of failing patterns (A2 measures at its RED); which platform endpoints answer with authentication off and no network on demo data, and which fields are volatile (A4 measures).
```

story\-6364\-s03f\-a\-rules\-first

### Models

None.

### Closure

```text
Feature A guards for the platform lane move
```

Safety

Pass

DesignedNotBuilt

```text
A list check fails on a missing listed type and on a name that does not exist; the law fails for any pattern whose answer differs between old and new namespace and for an alternative matching outside Platform; the literal check names any old-path literal not listed; the classifier test fails on a GitHub path not selected or a changed existing case; the compare command cannot answer IDENTICAL for a changed or empty comparison.
```

##### Evidence

None.

```text
Feature A guards for the platform lane move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today code passes the pinned map; the widened patterns satisfy the law on today and on the post-move names; Platform/Releases paths select github=true with every old case unchanged; two identical capture folders answer IDENTICAL.
```

##### Evidence

None.

```text
Feature A guards for the platform lane move
```

PublicReachability

Pass

DesignedNotBuilt

```text
Each guard is reached through its public driver: dotnet test on the Architecture namespace, bash on test-path-classifier-platform.sh and test-path-classifier.sh, bash on test-platform-answers-compare.sh, and the examiner probes architecture-rules, test-selection-show, platform-answers-compare and platform-answers-selftest declared in .nwave/examine.json.
```

##### Evidence

None.

```text
Feature A guards for the platform lane move
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no queue, schedule, retry or eventual progress claim; every guard is a bounded synchronous run.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: rules name the platform lane by area. They name it as Services (E1); a move to Platform drops types from about 20 patterns, so Models could reach the key ring and Promises the clock unseen.
```

```text
Every pattern answers the same for each moving type under old and new namespace
```

```text
Patterns gain Platform.<Area> alternatives
```

```text
A moved type escaping a rule; an alternative matching outside Platform
```

```text
A pattern built at run time from pieces or an inline fragment escapes reflection: the A2 literal check and the both-ways list comparisons fail loudly on rename in B
```

story\-6364\-s03f\-a\-rules\-first

#### Y2

Bypass

```text
IlDependencies misses catch-clause, event and attribute named-argument references; reflection sees only const fields
```

```text
Edges through signatures, fields, calls, constructors and attribute types; every const pattern
```

```text
Those reference kinds and static readonly or interpolated patterns stay unseen by the law
```

```text
Claiming the law covers them
```

```text
Interpolated patterns composed from consts (TheOtherContexts) inherit the widening of their parts; the C1 falsifiers cover the lane rules
```

story\-6364\-s03f\-c\-edges\-and\-rules

#### Y3

Stressor

```text
Assumption negated: CI selects live suites by content. GitHub is selected by path (E3).
```

```text
The GitHub suite runs on the push that moves the release code
```

```text
GITHUB_REGEX gains the Platform/Releases path
```

```text
A release-code change with github=false; an existing case changing selection
```

```text
Another future path-keyed selector: test-selection-show in EXAMINE
```

story\-6364\-s03f\-a\-rules\-first

#### Y4

Stressor

```text
Assumption negated: every listed platform endpoint answers with authentication off and no network on demo data, with stable fields
```

```text
No false IDENTICAL
```

```text
The command fails loudly when nothing was compared or an answer is not 2xx; volatile fields blanked
```

```text
IDENTICAL on an empty or failing comparison; a stable field masked away
```

```text
A4 reports which endpoints answer and which fields are volatile; GitHub calls stay with the GithubIntegration suite on the move push
```

story\-6364\-s03f\-a\-rules\-first

#### Y5

Stressor

```text
Assumption negated: no parallel Story touches the same loose files (S03i, #6657, Epic 6474 slice 12). Second set walked against the finished design: a worktree without the premium licence fixture, the Standalone build, a Redis cluster deployment.
```

```text
The map measured from the IL at the start of each B slice
```

```text
Their files stay in Services here; Licensing fixture failures are environmental; namespaces do not change the assembly; channel literals unchanged
```

```text
A stale map silently passing
```

```text
The later Story rebases; the map check fails on a missing or unknown name
```

story\-6364\-s03f\-b\-move

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneMembershipArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs

#### Public oracle

```text
The architecture rule tests run the platform lane membership check over the compiled production IL: each moving type listed in PlatformLaneMembership with its area, old namespace and new Lighthouse.Backend.Platform.<Area> namespace, the files that stay listed with their owner, compared with the top-level types the IL finds in the old platform namespaces and in Lighthouse.Backend.Platform; the domain-event handler implementors found in the IL; and the GlobalSuppressions.cs, .editorconfig and .globalconfig files of both projects read for targets naming a moving type.
```

```text
The check passes on today code and finds each listed type in its old or its new namespace; no IDomainEventHandler implementor lives outside Lighthouse.Backend.Modules; the listed suppression and analyser-config targets (today RandomNumberService.GetRandomNumber, PostgresDatabaseManagementProvider and LighthouseReleaseService in GlobalSuppressions.cs) equal the targets found naming a moving type, each with the B slice that rewrites it. A failure names each missing listed type, each listed name that does not exist and each unlisted type that appears.
```

```text
Written before the list is filled, the check fails naming the unlisted platform types (RED). A list with one entry removed, one made-up entry added, or a name that does not exist fails and names it; a made-up handler outside Modules is named; a suppression target naming a moving type that is not listed is named. A check that compares one way only, counts entries, reads only the old or only the new namespace, or matches a shared short name without its namespace passes those cases and is rejected.
```

##### Promises

P1

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

AccessAndLicensingMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs\:21

Reuse

```text
Shape of the namespace-agnostic both-ways membership comparison and the four-condition Models admission check.
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
No crafter target: the acceptance designer authors the oracle and its membership list; no production, rule or analyser file changes.
```

prefactoring

```text
No code target.
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
The listed platform membership equals the set of top-level types the IL finds in the old platform namespaces and in Lighthouse.Backend.Platform, both ways, each listed name found exactly once in its old or its new namespace; no IDomainEventHandler implementor lies outside Modules; the listed suppression and analyser-config targets equal those found naming a moving type.
```

Invariant

Infrastructure

PlatformLaneMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
list equals IL: pass
```

```text
listed type missing: named
```

```text
listed name that does not exist: named
```

```text
unlisted type present: named
```

```text
type found in the new namespace instead of the old: pass
```

```text
nested type: reduced to its outer type
```

```text
handler outside Modules: named
```

```text
suppression target naming a moving type not listed: named
```

```text
Models candidate failing a condition: left out with the condition named
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
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
The platform lane membership, its areas and old and new namespaces, the files that stay, the handler placement and the suppression targets naming moving types are measured from the IL and pinned both ways, namespace-agnostic.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneMembershipArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs

Extend

```text
ApplicationCore (line 20) gains Platform; HandlerSelection.ByDomainEventsNamespace (line 182) selects the dispatcher by its exact type and its nested types; descriptions updated.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Extend

```text
TheServices (line 18) gains Platform; TheUpdateQueue (line 20) gains Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
ThePlatform (line 56) and TheUpdateCode (line 58) gain Platform and Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
ThePlatform (line 58) and TheUpdateCode (line 62) gain Platform and Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
ThePlatform (line 58) and TheUpdateCode (line 62) gain Platform and Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
TheKeyRing (line 50) gains Platform.Encryption; PromisesAndTheUpdateCode (line 56) gains Platform.UpdateQueue. An edge newly visible through the widening is listed only if it exists on the pre-move IL, with a reason.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
OrganisationSetUp (line 57) gains the areas of the loose types that move (Host, Encryption, Logging, Releases, DemoData, Persistence, UpdateQueue) as the law requires; list strings are renamed in B.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
The shared update-code pattern gains Platform.UpdateQueue as the law requires.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs

Extend

```text
TheUpdateComponents (line 18) gains Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
The update pattern at line 19 gains Platform.UpdateQueue.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
ServicesImplementationPattern (line 24) gains Platform as the law requires.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs

Extend

```text
The (\.DomainEvents)? pattern gains Platform.DomainEvents.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs

Extend

```text
WhereTheHandlersLivedBefore (line 19) gains Platform.DomainEvents.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
The architecture rule tests run PlatformLaneRulesSeeTheMoveArchUnitTest: it enumerates by reflection every const string field of the test assembly that parses as a regex and is not a full-name or path string listed in the A1 map as renamed by a B slice, and for each moving type of the A1 map (PlatformLaneMembership) evaluates the pattern on the old full name and on the new Lighthouse.Backend.Platform.<Area> full name; it evaluates every Platform alternative against the production types the IL finds outside Lighthouse.Backend.Platform; it reads the dispatcher selection of ApplicationCoreRules on the IL; and it reads the rule files as text for string literals naming a moving area or a moving loose type by its old Services.Implementation, Services.Interfaces or Services/Implementation path.
```

```text
For every pattern and every moving type the two answers are equal; no Platform alternative matches a production type outside Platform; the dispatcher selection selects DomainEventDispatcher and its nested types by type, not by namespace fragment; no old-path literal is left except the entries the A1 map lists as renamed by a B slice; every existing architecture test stays green on today code. A failure names the pattern (class and field), the moving type and both answers, or the file, line and literal.
```

```text
Today about 20 patterns answer true on the old name and false on the new (RED), and ApplicationCoreRules.cs:182 names .Services.Implementation.DomainEvents (RED). A widening that misses one pattern, widens to all of Lighthouse.Backend (matching outside Platform), removes an old alternative, or leaves the fragment fails the oracle or an existing test. A check that reads only a hand list of patterns, compares only the old answer, or skips nested classes passes those cases and is rejected.
```

##### Promises

P2

P3

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ByDomainEventsNamespace

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs\:181

Extend

```text
Owner of the dispatcher selection read by ApplicationCoreRulesReachModulesArchUnitTest lines 159 and 168; its predicate becomes the exact type, not a second selection.
```

KernelRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:18

Extend

```text
Owner of TheServices and TheUpdateQueue, consumed by the kernel rules and by Forecasting as its shared update code default; widened in place.
```

ThePlatform

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:56

Extend

```text
Sole owner of the Promises platform and update-code keys; widened in place.
```

ThePlatform

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:58

Extend

```text
Sole owner of the Access platform and update-code keys; widened in place.
```

ThePlatform

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:58

Extend

```text
Sole owner of the Licensing platform and update-code keys; widened in place.
```

TheKeyRing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:50

Extend

```text
Sole owner of the Tracker integration key-ring and update-code keys; widened in place.
```

OrganisationSetUp

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Extend

```text
Sole owner of the Work Items loose-Services key; widened in place.
```

ForecastingsAdaptersOutboundRule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:434

Extend

```text
Owner of the Forecasting shared update-code selection; widened through KernelRules or in place.
```

TheUpdateComponents

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:18

Extend

```text
Sole owner of the seam update-components key; widened in place.
```

ModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:19

Extend

```text
Owner of the module-map update pattern at line 19; widened in place.
```

ServicesImplementationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:24

Extend

```text
Sole owner of the Refinement Services.Implementation key; widened in place.
```

PromisesMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:18

Extend

```text
Owner of the Promises membership DomainEvents pattern; widened in place.
```

WhereTheHandlersLivedBefore

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs\:19

Extend

```text
Sole owner of the Promises policies old-place key; widened in place.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs\:20

Changed

```text
ApplicationCore gains Platform; the dispatcher selection at line 182 becomes the exact type.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:18

Changed

```text
TheServices and TheUpdateQueue gain Platform alternatives.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:56

Changed

```text
ThePlatform and TheUpdateCode widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:58

Changed

```text
ThePlatform and TheUpdateCode widened; the composed TheOtherContexts inherits the widening.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:58

Changed

```text
ThePlatform and TheUpdateCode widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:50

Changed

```text
TheKeyRing and PromisesAndTheUpdateCode widened; newly visible pre-move edges listed with a reason.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

Changed

```text
OrganisationSetUp widened by the areas of the moving loose types.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:434

Changed

```text
Shared update code widened through KernelRules.TheUpdateQueue or in place, as the law requires.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:18

Changed

```text
TheUpdateComponents widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:19

Changed

```text
Update pattern widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:24

Changed

```text
ServicesImplementationPattern widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:18

Changed

```text
DomainEvents pattern widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs\:19

Changed

```text
WhereTheHandlersLivedBefore widened.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:159

UnchangedCompatible

```text
Reads ByDomainEventsNamespace; same verdicts with the exact-type selection.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production edit in feature A.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The patterns are widened in place; no preserving move makes the widening easier.
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
For every const regex pattern P of the test assembly outside the A1 rename list and every moving type T of the A1 map, IsMatch(old(T), P) == IsMatch(new(T), P); every Platform alternative matches no production type outside Lighthouse.Backend.Platform; no rule-file literal names a moving area or moving loose type by its old path outside the A1 rename list.
```

Invariant

Infrastructure

PlatformLaneRulesSeeTheMoveArchUnitTest

###### Treatment

Finite

###### Cases

```text
pattern matching the old name and not the new: named with both answers
```

```text
pattern matching neither or both: pass
```

```text
Platform alternative matching a production type outside Platform: named
```

```text
const pattern added later in any test class: enumerated
```

```text
const in a nested test class: enumerated
```

```text
full-name or path string on the A1 rename list: skipped
```

```text
inline .Services.Implementation.DomainEvents literal: named
```

```text
today code: every existing architecture test green
```

```text
Exhaustive over the finite domain (moving types x const patterns, about 137 x a few hundred); no generator adds coverage.
```

###### Evidence

None.

##### Refuted

```text
Every const string of the test assembly gives the same IsMatch answer for each moving type under its old and new full name.
```

```text
Law scope: a const full-name or path string (a designation of one type or file, renamed in its B slice) parses as a regex but is not a namespace rule; widening it would make it name two types.
```

```text
SecretCustodySeamArchUnitTest.CryptoServiceFullName = Lighthouse.Backend.Services.Implementation.CryptoService matches the old name and not Lighthouse.Backend.Platform.Encryption.CryptoService; it is renamed in B1 and carried on the A1 rename list instead.
```

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
Every const regex rule pattern of the test assembly answers the same for each moving platform type under its old and new namespace, no added alternative reaches outside Platform, and no rule string names a moving area by its old path except the listed B renames.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Scripts\/test\-selection\/path\-classifier\.sh

Extend

```text
GITHUB_REGEX (line 17) also selects GitHubService and LighthouseReleaseService files under Platform/Releases of the production and test projects.
```

Scripts\/test\-selection\/test\-path\-classifier\-platform\.sh

#### Verification

```text
bash Scripts/test-selection/test-path-classifier-platform.sh
bash Scripts/test-selection/test-path-classifier.sh
```

#### Supports

None.

#### Public oracle

```text
bash Scripts/test-selection/test-path-classifier-platform.sh sources path-classifier.sh and classifies diff lists: Lighthouse.Backend/Lighthouse.Backend/Platform/Releases/GitHubService.cs alone, Lighthouse.Backend/Lighthouse.Backend.Tests/Platform/Releases/GitHubServiceTest.cs alone, a LighthouseReleaseService file under Platform/Releases, and a file under another Platform area (Platform/Encryption/CryptoService.cs).
```

```text
The Platform/Releases GitHub and release-service paths, production and test, give github=true and backend=true; the other Platform area gives github=false and backend=true; the script prints its pass and fail counts and exits 0; the existing test-path-classifier.sh still passes every case unchanged.
```

```text
Today Platform/Releases/GitHubService.cs and its test path give github=false (RED). A widening that selects github for every Platform path, drops the old Services path, or changes another classifier output fails this script or the existing one.
```

##### Promises

P4

P6

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

ci

Scripts\/test\-selection\/path\-classifier\.sh\:17

Changed

```text
GITHUB_REGEX gains the Platform/Releases path.
```

ci

Scripts\/test\-selection\/test\-path\-classifier\.sh\:1

UnchangedCompatible

```text
Existing cases keep their expected selection; the file is not edited.
```

ci

\.github\/workflows\/ci\_changes\.yml\:1

UnchangedCompatible

```text
Consumes the classifier outputs by the same keys; one more path selects github.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
One regex widened in place; no preserving move makes it easier.
```

architectural_tests

```text
No dependency, boundary or owner law; the obligation is a CI selection, covered by the script oracle.
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
classify_github(path) is true for a GitHubService or LighthouseReleaseService file under Services/(Implementation|Interfaces) or Platform/Releases of the production or test project, and false otherwise; every other classifier output is unchanged for every path.
```

Invariant

Infrastructure

path\-classifier\.sh

###### Treatment

Finite

###### Cases

```text
Platform/Releases/GitHubService.cs: github=true
```

```text
Tests Platform/Releases/GitHubServiceTest.cs: github=true
```

```text
Platform/Releases LighthouseReleaseService: github=true
```

```text
Platform/Encryption file: github=false, backend=true
```

```text
old Services paths: github=true as before
```

```text
every existing test-path-classifier.sh case: unchanged
```

```text
Closed finite set of representative paths; each case is enumerated, no generator adds coverage.
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

None.

##### Contract tests

None.

### S4

#### Targets

Scripts\/examine\/build\-examine\-commands

Extend

```text
Generate platform-answers-compare on the shared capture and compare pieces with a --compare-captures stage and its masked volatile keys, and platform-answers-selftest that runs the slice test script.
```

\.nwave\/examine\.json

Extend

```text
Declare platform-answers-compare and platform-answers-selftest as admitted EXAMINE probes.
```

Scripts\/examine\/test\-platform\-answers\-compare\.sh

#### Verification

```text
bash Scripts/examine/test-platform-answers-compare.sh
bash Scripts/examine/build-examine-commands
bash Scripts/examine/test-access-answers-compare.sh
```

#### Supports

None.

#### Public oracle

```text
bash Scripts/examine/test-platform-answers-compare.sh builds the EXAMINE commands and runs platform-answers-compare --compare-captures on fixture capture folders it writes: one pair that differs in one platform answer (for example the log level), one identical pair, one pair that differs only in a field named volatile, and one empty pair; it also reads .nwave/examine.json.
```

```text
The differing pair prints DIFFERENT naming the changed answer and exits 1; the identical pair and the volatile-only pair print IDENTICAL and exit 0; the empty pair fails because nothing was compared; the command usage names the captured answers (system info, version without the GitHub calls, encryption and secrets state, database-management status, usage-data state, log level and problems, update status); .nwave/examine.json declares platform-answers-compare and platform-answers-selftest.
```

```text
Today the command does not exist (RED). A command that always prints IDENTICAL, always prints DIFFERENT, answers IDENTICAL when nothing was compared, or masks a stable answer field fails the script.
```

##### Promises

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

compare\_captures

Scripts\/examine\/build\-examine\-commands\:364

Reuse

```text
Shared compare stage over two capture folders; fails when a side is not a capture folder or an answer is not 2xx.
```

access\-answers\-selftest

Scripts\/examine\/build\-examine\-commands\:1730

Reuse

```text
Model for the selftest probe that runs the slice test script for the examiner.
```

commands

\.nwave\/examine\.json\:2

Extend

```text
The commands list owns the admitted EXAMINE probes; two entries added.
```

##### Impact

script

Scripts\/examine\/build\-examine\-commands\:811

Changed

```text
New generated platform-answers-compare beside access-answers-compare, and platform-answers-selftest.
```

config

\.nwave\/examine\.json\:2

Changed

```text
Two new probe entries.
```

script

Scripts\/examine\/build\-examine\-commands\:364

UnchangedCompatible

```text
Existing compare commands keep their behaviour; the shared pieces are reused, not changed; test-access-answers-compare.sh stays green.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The compare stage already takes capture folders (access-answers-compare --compare-captures); no preserving move is needed.
```

architectural_tests

```text
No dependency, boundary or owner law; the obligation is behavioural and covered by the script oracle.
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
compare(X, X) is IDENTICAL with exit 0; compare(X, Y) with one platform answer changed is DIFFERENT with exit 1 naming it; compare(X, Y) differing only in masked volatile fields is IDENTICAL; a comparison of empty captures fails.
```

Algebraic

Adapter

platform\-answers\-compare

###### Treatment

Finite

###### Cases

```text
identical pair
```

```text
one answer changed
```

```text
only a volatile field changed
```

```text
nothing compared
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
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

None.

##### Contract tests

None.

## ADRs

None.
