# Architecture brief: story\-6364\-s03f\-c\-edges\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (F11, D-8 option a): the platform lane is infrastructure below the contexts, not a module. It stays under every rule it is under today (ApplicationCoreRules.ApplicationCore and the kernel rules widened in feature A) and gets one direction rule set of its own, R-PL1 to R-PL4, in Architecture/PlatformLaneBoundariesArchUnitTest. The lane subject is every type whose namespace is under Lighthouse.Backend.Platform. R-PL1: each PlatformLaneMembership.MovingTypes row lives in Lighthouse.Backend.Platform.<its Area> and no type of that name is left under Lighthouse.Backend.Services. R-PL2: the lane reaches no type under Lighthouse.Backend.API, empty list. R-PL3: the lane reaches no type under any Lighthouse.Backend.Modules.<Module>.Adapters, empty list. R-PL4: the lane reaches types under Lighthouse.Backend.Services only through one inline list of "Dependent -> Target" full names. Rejected: a composition-root exemption (loosens rules that see these types today); no rule of its own (new platform code could reach the API, an adapter or Services unseen).
```
Governed slices: S1

#### D2

```text
Confirmed (F12, D-15): the lane's edges into modules (DatabaseMaintenanceGate -> FlowMetrics.Application.IOverTimeHistoryFillActivity, UpdateNotificationHub -> WorkItems.Application.IRefreshLogService, UsageDataConsentService, UsageDataConsentPruningService and UsageDataInstanceProperties -> Licensing.Application.ILicenseService, UsageDataInstanceProperties -> Access.Application.IAuthModeResolver) stay governed by those modules' inbound rules and are not listed a second time; R-PL4 targets only Lighthouse.Backend.Services, so no R-PL rule sees a module Application. Models -> Platform stays forbidden by the widened kernel rule (feature A), not a new rule. DatabaseMaintenanceGate -> Flow metrics stays listed in Flow metrics' rule; its inversion is a catalogue note only, no ADO item. Feature B's listed edges stay as recorded and are not touched: F3 (ConnectionHealthService and OAuthService -> Platform.Encryption.ICryptoService in TrackerIntegrationModuleBoundariesArchUnitTest) and F6-S5 (ForecastUpdater -> Platform.UpdateQueue.IUpdateQueueService and IUpdateStatusStore in ForecastingModuleBoundariesArchUnitTest). F3-S3 (PlatformLaneMembershipArchUnitTest reads a renamed string's file under Tests/Platform/<Area> once moved) is why C2's lists name files by their current place and never re-list a path feature A's map holds.
```
Governed slices: S1, S2

#### D3

```text
Confirmed (F13): every list is inline in its oracle, by full type name or by repository path, compared both ways through InlineBaseline.AssertMatches (an unlisted offender fails as not on the list, a listed entry no longer found fails as no longer offending) and never loosened. R-PL2, R-PL3 and R-PL4 call ModuleRules.OutboundEdges(module, lane, target, listed, rule) with the lane wording passed as its rule text: the text says "the platform lane", names no module and is not a ModuleWording. R-PL1 calls ModuleRules.TypesLiveInTheirLayer once per area of PlatformLaneMembership.MovingTypes with formerHome = the Services pattern ^Lighthouse\.Backend\.Services(\.|$); a missing expected full name names the type and its expected Platform.<Area>, and a type left under Services names its expected place. The R-PL4 list is measured from the IL by C1's acceptance designer at RED; the DESIGN probe (the IlDependencies.DependentsIn algorithm, generic parameters excluded as ModuleRules.IsATypeIn does) found 19 edges, recorded in the S1 constraint as the expected list. The probe refutes part of the product expectation (S1 refuted record); the oracle lists what the IL shows, never the expectation.
```
Governed slices: S1, S2

#### D4

```text
Pattern placement against feature A's preservation law (PlatformLaneRulesSeeTheMoveArchUnitTest scans every class-level const and static readonly string of the test assembly that names the backend and parses as a regex): PlatformLanePatternRoles does not change in C1 or C2. The lane pattern ^Lighthouse\.Backend\.Platform(\.|$) names only the new place, which no role of the law can carry (as Context every moving type breaks as SeesAnAreaItNeverSaw; as Place it breaks as KeepsAPlatformAlternativeAsAnOldLocation), and the Services pattern ^Lighthouse\.Backend\.Services(\.|$) is used only by these rules. Both are written as method-local constants or inline arguments where the rules are called, not as class-level fields, the same choice as feature B's F5-S4 made-up name. Their own guard: R-PL1 over the real IL names every moved type as left behind if the Services pattern saw Platform, and OutboundVerdict.Check fails when the lane pattern examines no type. C2 holds no class-level string matching a moving type's namespace or full name: the production project folder is read through ProductionSource or held method-local; test namespaces start with Lighthouse.Backend.Tests and match no moving type. Every made-up path or name in C1 and C2 names a made-up type, never a moving type or a moving Services area by its old path, so feature A's old-path literal check stays green. Rejected: declaring the Services pattern as a Place in PlatformLanePatternRoles (edits feature A's support for a pattern the R-PL1 run already guards).
```
Governed slices: S1, S2

#### D5

```text
Confirmed (F14, F15, D-13, D-14): C1's RED is assertion-level, observed with the oracle written before its R-PL4 list is filled (the real-IL R-PL4 test fails naming every unlisted edge) and with each falsifier asserting that the verdict names its made-up offender; C2's RED is structural, observed with the stay lists written empty (the real-tree rule names the unlisted staying files) and the made-up file in an old folder named. Behaviour is proved by the unchanged suites and EXAMINE through the admitted probes (architecture-rules, backend-tests); feature C changes no production code. The mutation bar (D-14) applies once after feature C to the moved production code; feature C adds none. One commit per slice; C1 and C2 are pushed together after the whole-candidate review. Rigor thorough: architect, acceptance designer and crafter opus; reviewers and EXAMINE sonnet. Usage-data event, UI sketch, loading and failure looks: not applicable.
```
Governed slices: S1, S2

#### D6

```text
Carriers and owners: the rules and their inline lists live in their oracle classes (Architecture/PlatformLaneBoundariesArchUnitTest for R-PL1 to R-PL4; Architecture/PlatformLaneUnitTestPlacementTest for the placement rule and the per-file, per-owner list of production files that stay in Services/Interfaces and Services/Implementation, as (Path, Owner) tuples in the PromisesUnitTestPlacementTest shape). The IL is read through IlDependencies over Mono.Cecil, reached via ModuleRules.OutboundEdges and ModuleRules.TypesLiveInTheirLayer; verdicts are OutboundVerdict and KernelRuleVerdict compared through InlineBaseline. The type list is PlatformLaneMembership.MovingTypes, read only. Made-up offenders live in support fixture files in namespaces that do not start with Lighthouse.Backend.Platform, Lighthouse.Backend.Services or Lighthouse.Backend.Tests.Platform, so no real rule sees them. No production carrier, effect, error type or per-process state exists or changes. Paradigm object_oriented is kept; the targets are test-side rules outside the OO domain core, so no domain owner, value object or Object Calisthenics rule is in play.
```

#### D7

```text
Formal verification not applicable (quint --version 0.33.1 and java -version OpenJDK 21.0.13.1 probed; no checker selected): feature C has no state, ordering, concurrency, recovery or liveness claim. Each claim is a set comparison over a finite domain decided exhaustively by the compiled IL or the file tree: R-PL1 (map rows against types found), R-PL2 and R-PL3 (edge set empty), R-PL4 (edge set equals the inline list both ways), placement (files found against the stay lists both ways). A Quint, TLA+ or Agda model would restate the list and could not fail. Property tests not applicable: every domain is finite and enumerated, and each list position has its own falsifier case. Cross-layer algebra: only the test-side rule layer is affected; states are the edge or file sets found, operations are the rule runs, observations are the verdict messages; no production layer changes.
```

### Constraints

#### K1

```text
Test-side rule files only: no production file, Program.cs, DI registration, configuration key, schema, GlobalSuppressions.cs, analyser config or script changes; nothing a user, an API client, a tracker or the database sees changes; the full filtered backend suite stays green and dotnet build has zero warnings. Known environmental failures, never regressions: the path-classifier case ACodeChangeToTheFetchFingerprint_... and the DiValidation lock signature, plus the worktree-only Licensing fixture and the excluded live-connector and release-update categories named in CLAUDE.md.
```

#### K2

```text
Never loosen a rule: no existing assertion, list entry, pattern alternative or message is removed or widened; the lane's edges into modules stay under the modules' inbound rules and are not listed again; no other module's list changes (feature B's F3 and F6-S5 entries included). Feature A's oracles and supports (PlatformLaneMembership, PlatformLaneMembershipArchUnitTest, PlatformLaneRulesSeeTheMoveArchUnitTest, PlatformLanePatternRoles, the classifier and EXAMINE scripts) and feature B's approved layout oracles (Platform*LayoutArchUnitTest) are never edited; feature C's approved oracles and supports are never edited by craft. One oracle file per slice. No nWave scenario tags in code; the Lighthouse test-writing skill wins over nWave timing and PBT defaults. Never git stash.
```

#### K3

```text
IlDependencies limits carry over: a hand-written nested type (UpdateQueueService+HeldUpdate) is its own dependent in an edge list; catch-clause, event and attribute named-argument references are missed; generic parameters are excluded by ModuleRules.IsATypeIn. Regex evaluation uses the existing one-second pattern timeout. No stored or validated input bound exists or changes.
```

### Reuse

OutboundEdges

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:126

Reuse

```text
ModuleRules.OutboundEdges: R-PL2, R-PL3 and R-PL4 are outbound edge sets of a namespace set compared with an inline list both ways; it does exactly that over the IL, takes subject, target, list and rule text as parameters, and is the rule ModuleRulesCatchANewOffender already falsifies. The lane passes its own rule text, so no module wording appears.
```

TypesLiveInTheirLayer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:150

Reuse

```text
ModuleRules.TypesLiveInTheirLayer: R-PL1 is the layout check feature B's layout oracles already run per area: expected full names present, none left in the former home; formerHome takes the Services-only pattern.
```

AssertMatches

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs\:11

Reuse

```text
InlineBaseline.AssertMatches: the both-way list comparison with not-on-the-list and no-longer-offending messages; used by OutboundVerdict and by the C2 stay lists.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:25

Reuse

```text
The one IL reader every architecture rule uses (Release-safe, unlike the ArchUnitNET graph); no second reader.
```

MovingTypes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Reuse

```text
PlatformLaneMembership.MovingTypes: feature A's map is the one source of the lane's types and areas; R-PL1 and C2's subject lookup read it, never a copy.
```

PatternRoles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:51

Reuse

```text
PlatformLanePatternRoles.PatternRoles, unchanged: the new rule files hold no class-level string the preservation law must declare (the lane and Services patterns are method-local), so the existing declarations stay the whole set.
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:1

Reuse

```text
Deliberately not used by the lane rules: the lane is not a module, so a ModuleWording module name would mislabel its failures; the lane wording is the rule text passed to OutboundEdges.
```

PromisesUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:17

Reuse

```text
Shape of C2: files on disk plus the compiled test assembly and its symbols, roots not judged, a (Path, Owner) stay list compared both ways, rules also run on made-up trees. Copied as a shape; the Promises class is not edited.
```

TypesThatStay

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:262

Reuse

```text
PlatformLaneMembership.TypesThatStay: the types that stay with their owner seed C2's per-file owner list where a file holds a listed type; C2 names every other staying file and its owner itself.
```

Files

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProductionSource\.cs\:19

Reuse

```text
ProductionSource.Files: reads the production source tree without a class-level project-folder string the preservation law would have to declare.
```

PlatformLaneUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

CreateNew

```text
Each placement test owns one module; feature B's layout oracles pin the named moved test classes, not every future platform test; no rule owns the per-file stay list of Services/Interfaces and Services/Implementation.
```

PlatformPlacementFixtures

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/PlacementFixtures\/Platform\/PlatformPlacementFixtures\.cs\:1

CreateNew

```text
A made-up platform type and its made-up test class, compiled so the subject lookup by compiled class and symbols runs on made-up trees, as PlacementFixtures/Modules/Promises does for Promises.
```

### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:57

UnchangedCompatible

```text
OrganisationSetUp is not renamed in C1: it still names the loose Organisation set-up files of Services.Implementation and Services.Interfaces that Work Items reaches only through listed edges, and its PlatformLanePatternRoles declaration already says the platform types among them stay reached. A rename would change two feature A keys (OrganisationSetUp and its composite) and LicensingModuleBoundariesArchUnitTest's list-name string, with no verdict change.
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs\:51

UnchangedCompatible

```text
No new class-level pattern sees a moving type (lane and Services patterns method-local), so the preservation law's declarations stay complete.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:88

UnchangedCompatible

```text
Scans the new files as rule files and their fields as patterns; they add no undeclared pattern and no old-path literal of a moving type or area, so its verdicts stay; run in each slice's verification.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production file, configuration, schema or DI change in feature C.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

UnchangedCompatible

```text
Module lists stay as they are, including feature B's F6-S5 ForecastUpdater edges and F3 ICryptoService edges in TrackerIntegrationModuleBoundariesArchUnitTest; the lane's module edges are not listed again.
```

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes: feature C adds test-side rule files only; no wire, DTO, schema, configuration, persisted or script output format is produced or consumed differently.
```

contract_tests

```text
No producer and consumer can change apart: one deployable, and the rules read the compiled assembly of the same commit.
```

### Boundaries

```text
Architecture rule tests run by dotnet test over the compiled production IL (IlDependencies over Mono.Cecil), the compiled test assembly with its symbols, and the file trees of the production and test projects
```

#### Driven ports

```text
IlDependencies module reader and the file system, read only
```

```text
Tests read production; production never references a test, fixture or rule. Within production the rules hold: platform lane below the contexts, never toward API, a module's Adapters, or unlisted Services code; Models never toward Platform.
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
Production unchanged; every map type in its Platform area; the lane reaches no API type and no module adapter; its Services edges equal the measured list; every platform unit test under Tests/Platform; every file left in Services/Interfaces and Services/Implementation listed with its owner.
```

story\-6364\-s03f\-c\-edges\-and\-rules

Excluded

```text
An unlisted lane edge into Services, API or a module adapter; a listed edge or file that no longer exists; a map type outside its area or left under Services; a platform unit test outside Tests/Platform; an unlisted file in Services/Interfaces or Services/Implementation; a loosened list or rule; a lane pattern that examines nothing.
```

story\-6364\-s03f\-c\-edges\-and\-rules

Unknown

```text
The exact R-PL4 list and the per-file owners until the acceptance designers re-measure them at RED; lane reach the IL does not carry (DI-only resolution, reflection, catch clauses, events, attribute named arguments).
```

story\-6364\-s03f\-c\-edges\-and\-rules

### Models

None.

### Closure

```text
Platform lane rules and placement
```

Safety

Pass

DesignedNotBuilt

```text
Each excluded state is named by a rule run: unlisted or gone edge, misplaced type, misplaced test, unlisted or gone stay file, lane pattern examining nothing; every list compared both ways with a falsifier per position.
```

##### Evidence

None.

```text
Platform lane rules and placement
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today's code passes with the measured lists: the DESIGN IL probe found 19 Services edges and no API or adapter edge, and the file tree holds the staying files named in the S2 constraint.
```

##### Evidence

None.

```text
Platform lane rules and placement
```

PublicReachability

Pass

DesignedNotBuilt

```text
Both oracles are reached through dotnet test on the Architecture namespace and the examiner probe architecture-rules.
```

##### Evidence

None.

```text
Platform lane rules and placement
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no queue, schedule, retry or eventual progress claim; every rule is a bounded synchronous run.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negated assumption: the lane reaches Services only through what the IL shows (the update pipelines are reached through DI and never appear as an edge)
```

```text
Every IL-visible lane edge into Services is listed and checked both ways
```

```text
DI-only reach stays outside R-PL4
```

```text
Listing an expected edge the IL does not show, or loosening the list to absorb one
```

```text
A lane type that resolves a Services type by DI or reflection is not seen; owner: the S03f finalize note and Epic 6474 (update pipelines)
```

story\-6364\-s03f\-c\-edges\-and\-rules

#### Y2

Bypass

```text
Negated assumption: every rule pattern is a class-level field the preservation law scans; the lane and Services patterns are method-local
```

```text
R-PL1 fails if the Services pattern sees Platform; OutboundVerdict fails if the lane pattern examines nothing
```

```text
The two patterns sit outside the preservation law
```

```text
Using a method-local pattern to hide one that sees a moving type under its old name
```

```text
A later move of a type into a new lane area is seen by the lane pattern without a declaration; owner: the next platform move's rules-first slice
```

story\-6364\-s03f\-c\-edges\-and\-rules

#### Y3

Stressor

```text
Negated assumption: no parallel Story adds files to Services or to the lane before C lands (S03i, #6657, Epic 6474 slice 12)
```

```text
Every new file or edge is named loudly by the both-way lists
```

```text
The later Story lists its own file or edge with an owner, or places it
```

```text
A silent pass on a new Services file or lane edge
```

```text
Merge friction on the inline lists; owner: the later Story, which rebases
```

story\-6364\-s03f\-c\-edges\-and\-rules

#### Y4

Stressor

```text
Second set, walkthrough not execution: Release build in CI (IL reader is Release-safe; same edges), a worktree without the licence fixture (2 Licensing failures, environmental), the path-classifier ACodeChangeToTheFetchFingerprint case and the DiValidation lock (environmental)
```

```text
Rule verdicts identical in Debug and Release
```

```text
Named environmental failures outside the rules
```

```text
Treating an environmental failure as a rule regression, or a rule regression as environmental
```

```text
None beyond the named environmental signatures; owner: the host at each run
```

story\-6364\-s03f\-c\-edges\-and\-rules

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneBoundariesArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest|FullyQualifiedName~PlatformLaneMembershipArchUnitTest|FullyQualifiedName~ModuleRulesCatchANewOffenderArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundaryFixtures\.cs

#### Public oracle

```text
dotnet test runs PlatformLaneBoundariesArchUnitTest over the compiled production IL (IlDependencies.ReadModule of the Lighthouse assembly): R-PL1 per area of PlatformLaneMembership.MovingTypes through ModuleRules.TypesLiveInTheirLayer with formerHome ^Lighthouse\.Backend\.Services(\.|$); R-PL2, R-PL3 and R-PL4 through ModuleRules.OutboundEdges with lane ^Lighthouse\.Backend\.Platform(\.|$) and targets ^Lighthouse\.Backend\.API(\.|$), ^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$) and ^Lighthouse\.Backend\.Services(\.|$), with the inline lists (R-PL2 and R-PL3 empty, R-PL4 the measured edges). The same rule calls run over the test assembly IL with made-up lane, API, adapter and Services namespaces from the support fixture file.
```

```text
On the real IL every rule passes: each listed type in its Platform.<Area>, none under Services; no API edge; no adapter edge; the R-PL4 edges equal the list both ways. Each made-up case fails and its message names the case in the lane wording ("the platform lane", no module name): a made-up listed type left under the made-up Services namespace names the type and its expected area; one in another area is named as missing from its expected Platform.<Area>; a made-up lane type reaching a made-up API type, and one reaching a made-up module adapter, are named as the edge; a made-up lane type reaching an unlisted made-up Services type is named as not on the list; and for every position of the R-PL4 list, the list with that entry removed names it as not on the list and the list with a made-up gone entry added names it as no longer offending.
```

```text
Written before the R-PL4 list is filled, the real-IL R-PL4 test fails naming every measured edge (RED). A rule that compares one way only, counts entries, reads only top-level types, matches short names without namespace, examines no lane type, or names a module instead of the lane passes some made-up case and is rejected. A per-position falsifier that drops or fakes only one fixed entry leaves the other positions unguarded and is rejected.
```

##### Promises

P1

P2

P3

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

```text
Expected R-PL4 list (DESIGN IL probe over the Debug build of HEAD; the acceptance designer re-measures at RED and the oracle lists exactly what the IL shows): DemoDataService -> IDeliveryMetricSnapshotRepository, IDeliveryRepository, IRepository`1, ISizingLogRepository; SystemInfoService -> IAppSettingService; InstallTimestampSeeder -> IAppSettingService; UpdateQueueService -> WriteBackCollector, WriteBackRound, WriteBackRoundContext, IWriteBackService; UpdateQueueService+HeldUpdate -> WriteBackRound; UpdateTaskNaming -> IPortfolioRepository, IRepository`1; UsageDataConsentPruningService -> IUsageDataConsentRepository; UsageDataConsentService -> IAppSettingService, IUsageDataConsentRepository; UsageDataGate -> IRepository`1, IUsageDataConsentRepository; UsageDataMasterSwitch -> IRepository`1. Dependents are under Lighthouse.Backend.Platform.<Area>; repository interfaces under Lighthouse.Backend.Services.Interfaces.Repositories; IAppSettingService and IWriteBackService under Lighthouse.Backend.Services.Interfaces; the WriteBack types under Lighthouse.Backend.Services.Implementation. 19 edges. R-PL2 and R-PL3 measured empty.
```

##### Reuse

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:27

Reuse

```text
Shape of the falsifiers: run the same shared rule over made-up namespaces in the test assembly, assert the verdict names the newcomer, and drop or fake one list entry per position. Copied as a shape; the class itself is not edited.
```

PlatformLaneBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:1

CreateNew

```text
No existing rule owns the lane's direction: each module rule governs one module's edges, the kernel and core rules govern Models and the application core; none has the lane as subject.
```

PlatformLaneBoundaryFixtures

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundaryFixtures\.cs\:1

CreateNew

```text
The made-up lane, API, module-adapter and Services types the falsifiers need; the Forecasting fixtures model a module, not the lane, and editing them would change ModuleRulesCatchANewOffender's cases.
```

##### Impact

test\-oracle

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:1

Changed

```text
New oracle holding R-PL1 to R-PL4, the inline lists and the falsifiers; written by the acceptance designer.
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundaryFixtures\.cs\:1

Changed

```text
New made-up offender types in namespaces no real rule reads.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No move eases the change: the reused rules (OutboundEdges, TypesLiveInTheirLayer, InlineBaseline) already take subject, target, list and rule text as parameters, so the new oracle calls them unchanged.
```

##### Boundaries

None.

##### Observations

```text
The rule verdict: pass, or a failure message listing each offending edge as Dependent -> Target or each misplaced type with its expected Platform.<Area>, worded for the platform lane
```

NUnit outcome with Assert\.Fail message

```text
References the IL does not carry (catch clauses, events, attribute named arguments) and reflection or DI-only reach, such as the update pipelines resolved through DI; a hand-written nested type is reported as its own dependent.
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
For the platform lane L (types under Lighthouse.Backend.Platform) and the compiled IL: R-PL1 every MovingTypes row T has exactly Platform.<Area(T)>.Name(T) and no type named Name(T) under Services; R-PL2 edges(L, API) is empty; R-PL3 edges(L, Modules.*.Adapters) is empty; R-PL4 edges(L, Services) equals the inline list as sets (both differences empty). Each list position i is load-bearing: list minus i names i, list plus a made-up gone edge names it.
```

Invariant

Infrastructure

PlatformLaneBoundariesArchUnitTest

###### Treatment

Finite

###### Cases

```text
real IL: every rule passes with the measured list
```

```text
made-up listed type left under the made-up Services namespace: named with its expected area
```

```text
made-up listed type in another area: named missing from its expected Platform area
```

```text
made-up lane type reaching a made-up API type: edge named, lane wording
```

```text
made-up lane type reaching a made-up module adapter: edge named, lane wording
```

```text
made-up lane type reaching an unlisted made-up Services type: named not on the list
```

```text
for each R-PL4 list position: list minus that entry names it as not on the list
```

```text
for each R-PL4 list position: list plus a made-up gone edge names it as no longer offending
```

```text
lane pattern examining no type: fails as checked nothing
```

```text
Finite domain: the compiled types and edges and the list positions, enumerated exhaustively; a generator adds no coverage.
```

###### Evidence

None.

##### Refuted

```text
The product expectation that UpdateQueueService reaches the update pipelines and IAppSettingService (P3 "at least").
```

```text
Text search counted the update pipelines and IAppSettingService as UpdateQueueService dependencies; the IL shows the pipelines are not reached by any lane type (they are resolved through DI outside the lane) and IAppSettingService is reached by SystemInfoService, InstallTimestampSeeder and UsageDataConsentService, not by UpdateQueueService. The expectation was a designation of the text search, not a measured edge; the list follows the IL.
```

```text
DESIGN IL probe over the HEAD Debug build: UpdateQueueService -> WriteBackCollector, WriteBackRound, WriteBackRoundContext, IWriteBackService (Lighthouse.Backend.Services); no edge UpdateQueueService -> TeamUpdater, PortfolioUpdater, UpdateServiceBase`1, ITeamUpdater, IPortfolioUpdater, IUpdateService or IAppSettingService.
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

###### T1

```text
The platform lane's direction law over its whole scope, including types added later under Lighthouse.Backend.Platform: R-PL1 layout from the map, R-PL2 no API, R-PL3 no module Adapters, R-PL4 Services only through the inline list, each with a falsifier per rule and per list position
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneBoundariesArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/GithubServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/GithubServiceTest.cs; namespace Lighthouse.Backend.Tests.Platform.Releases, only the usings the compiler needs; body, Category GithubIntegration and IntegrationFixture base unchanged (F7-S2).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/PlatformExtensionTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PlatformExtensionTest.cs; namespace Lighthouse.Backend.Tests.Platform.Host, only the usings the compiler needs; body unchanged (F7-S2).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneUnitTestPlacementTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneRulesSeeTheMoveArchUnitTest|FullyQualifiedName~PlatformLaneMembershipArchUnitTest|FullyQualifiedName~UnitTestPlacementTest|FullyQualifiedName~Platform"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/PlacementFixtures\/Platform\/PlatformPlacementFixtures\.cs

#### Public oracle

```text
dotnet test runs PlatformLaneUnitTestPlacementTest over the test project's files on disk and the compiled test assembly with its symbols (which file each test class was compiled from), and over the production project's files under Services/Interfaces and Services/Implementation, in the PromisesUnitTestPlacementTest shape: every unit test whose subject is a platform type (a PlatformLaneMembership.MovingTypes row or any type under Lighthouse.Backend.Platform) sits under Lighthouse.Backend.Tests/Platform/<its Area>, namespace equal to folder; the API, Acceptance, Architecture, Contracts and Integration roots are not judged; the test files that stay by design are named per file with their reason; every production file left in Services/Interfaces and Services/Implementation is named per file with its owner. The same rules run on made-up trees.
```

```text
On today's tree the rules pass: no platform unit test outside Tests/Platform/<Area>; the staying tests (update-pipeline tests, the three context gold tests, seeder and repository tests, EncryptionKeyRingTests, SecretReadabilityReportTests, TestData/*.zip) not flagged; the production files found in Services/Interfaces and Services/Implementation equal the (Path, Owner) list both ways and no entry is a platform type. On made-up trees: a made-up test of a made-up platform type in Tests/Services/Implementation is named with its expected Platform folder; a made-up file in Services/Interfaces or Services/Implementation that is not listed is named; a listed file that no longer exists is named; a made-up test in its Platform folder, and a file that only looks alike, are not named.
```

```text
Written with the stay lists empty, the real-tree rule fails naming every staying production file (RED), and the made-up test in an old folder is named. A rule that reads one folder only, compares one way, judges by class name without the compiled file path, flags the staying tests, or accepts a list entry without an owner passes some made-up case and is rejected.
```

##### Promises

P4

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

```text
F7-S2 (AFK engineering choice, open to the maintainer): the two unit tests of platform types that feature B left in Tests/Services/Implementation move into the lane's test tree, as the same kind of pure test move feature B made. GithubServiceTest (subject GitHubService, Platform/Releases; the live GitHub suite, Category GithubIntegration) moves with git mv to Lighthouse.Backend.Tests/Platform/Releases/GithubServiceTest.cs; PlatformExtensionTest (subject PlatformExtensions, Platform/Host) moves with git mv to Lighthouse.Backend.Tests/Platform/Host/PlatformExtensionTest.cs. Each file's namespace becomes its folder (Lighthouse.Backend.Tests.Platform.Releases, Lighthouse.Backend.Tests.Platform.Host) and its usings are only those the compiler needs; test bodies, class names, categories and fixtures are unchanged. The C2 oracle's staying-test list stays empty for them. No production file changes. Test selection needs no script change: Scripts/test-selection/path-classifier.sh GITHUB_REGEX matches both the old Services path and Platform/Releases, and test-path-classifier-platform.sh already expects Tests/Platform/Releases/GithubServiceTest.cs; the crafter renames only a time-budget or list entry that names either test by full name, and the DESIGN search found none. Rejected: listing both as staying tests (neither has a reason to stay; their subjects moved in feature B), and a craft-time exemption in the oracle (loosens the rule).
```

##### Constraints

```text
Placement facts measured at DESIGN (file tree of HEAD): 151 production .cs files stay in Lighthouse.Backend/Services/Interfaces and Services/Implementation (loose 39, Repositories 46, Reports 31, Refinement 16, Seeding 7, OptionalFeatures 4, Update interfaces 3, BackgroundServices/Update 3, Connections 2). Test files under Lighthouse.Backend.Tests/Services that stay by design: update-pipeline tests (Implementation/BackgroundServices/Update), the three context gold tests (Implementation/DomainEvents: SnapshotRecordedDayInstanceZoneTest, TeamDataRefreshedGoldTest, WorkItemDomainEventsGoldTest), seeder and repository tests, Implementation/Encryption EncryptionKeyRingTests and SecretReadabilityReportTests (their subjects stay outside the lane, per feature B), and TestData/*.zip (2 host fixtures). C2's acceptance designer measures each file and its owner at RED.
```

##### Reuse

None.

##### Impact

test\-oracle

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs\:1

Changed

```text
New oracle holding the placement rule, the staying-test list and the per-file, per-owner production stay list.
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/PlacementFixtures\/Platform\/PlatformPlacementFixtures\.cs\:1

Changed

```text
New made-up platform type and test class; namespace Lighthouse.Backend.Tests.PlacementFixtures.Platform, outside Tests.Platform, so B's layout oracles and C2's real-tree rule do not judge it.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/GithubServiceTest\.cs\:1

Migrated

```text
Moved with git mv to Tests/Platform/Releases/GithubServiceTest.cs, namespace Lighthouse.Backend.Tests.Platform.Releases; still selected by Category GithubIntegration and excluded by the filtered suite as before.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PlatformExtensionTest\.cs\:1

Migrated

```text
Moved with git mv to Tests/Platform/Host/PlatformExtensionTest.cs, namespace Lighthouse.Backend.Tests.Platform.Host; same tests run in the filtered suite.
```

test\-selection\-script

Scripts\/test\-selection\/path\-classifier\.sh\:17

UnchangedCompatible

```text
GITHUB_REGEX matches GithubServiceTest under both Services/Implementation and Platform/Releases; test-path-classifier-platform.sh:52 already expects the Platform/Releases path and test-path-classifier.sh:94 keeps the old path as a classifier input. No change.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/GithubServiceTest\.cs\:1

Changed

```text
New location of the moved live GitHub suite; C2's real-tree rule finds it under Tests/Platform/Releases with namespace equal to folder. Feature B's Releases layout oracle pins named classes only and is unaffected.
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/PlatformExtensionTest\.cs\:1

Changed

```text
New location of the moved PlatformExtensions test; C2's real-tree rule finds it under Tests/Platform/Host with namespace equal to folder. Feature B's Host layout oracle pins named classes only and is unaffected.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No move eases the change: the placement test is a new file in the PromisesUnitTestPlacementTest shape and edits no existing test; extracting a shared placement helper would edit approved module oracles for no verdict change.
```

reuse

```text
S2's two targets (Tests/Platform/Releases/GithubServiceTest.cs and Tests/Platform/Host/PlatformExtensionTest.cs) are existing test files moved by git mv with only their namespace and usings changed (F7-S2); no responsibility is created, so each reuses its own current class (GithubServiceTest, PlatformExtensionTest, today under Tests/Services/Implementation) unchanged. The other S2 reuse rows cover the oracle and its support, which are not crafter targets.
```

##### Boundaries

None.

##### Observations

```text
The placement verdict: pass, or a failure naming each misplaced platform test file with its expected Tests/Platform/<Area> folder, each unlisted file in Services/Interfaces or Services/Implementation, and each listed file that no longer exists
```

NUnit outcome with Assert\.Fail message

```text
Whether a listed owner is the right owner (judged by review, not the rule); a test whose subject is reached only indirectly (integration roots are not judged).
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
For the test tree and compiled test assembly: every unit test whose subject is a platform type sits under Tests/Platform/<Area(subject)>, namespace equal to folder, except the named staying files; the production .cs files found under Services/Interfaces and Services/Implementation equal the (Path, Owner) list as sets, and no listed entry is a platform type. Each list entry is load-bearing: a listed file that no longer exists is named; an unlisted file is named.
```

Invariant

Infrastructure

PlatformLaneUnitTestPlacementTest

###### Treatment

Finite

###### Cases

```text
real tree: passes with the measured stay lists
```

```text
made-up test of a made-up platform type in Tests/Services/Implementation: named with its expected Tests/Platform folder
```

```text
made-up test in its Tests/Platform folder: not named
```

```text
staying tests (update pipelines, three gold tests, seeder and repository tests, the two Encryption tests, TestData zips): not named
```

```text
made-up unlisted file in Services/Interfaces and one in Services/Implementation: named
```

```text
listed file that no longer exists: named
```

```text
file that only looks alike: not named
```

```text
Finite domain: the files on disk and the list entries, enumerated exhaustively.
```

###### Evidence

None.

##### Refuted

```text
On the tree before S2's craft, C2's rule passes with the measured stay lists.
```

```text
Feature B moved GitHubService to Platform/Releases and PlatformExtensions to Platform/Host but left their unit tests in Tests/Services/Implementation; the DESIGN staying-test inventory listed Services test folders by kind, not by the subject of each test, so it missed them.
```

```text
HEAD 7d4883cfe: Lighthouse.Backend.Tests/Services/Implementation/GithubServiceTest.cs (subject GitHubService) and Lighthouse.Backend.Tests/Services/Implementation/PlatformExtensionTest.cs (subject PlatformExtensions) are named by the real-tree placement rule as platform tests outside Tests/Platform/<Area>. Resolved by F7-S2's move, not by a stay-list entry.
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
Platform unit tests under Tests/Platform/<Area> and the per-file, per-owner stay list of Services/Interfaces and Services/Implementation, over every file present now or added later, with falsifiers on made-up trees
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneUnitTestPlacementTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLaneUnitTestPlacementTest"
```

##### Contract tests

None.

## ADRs

None.
