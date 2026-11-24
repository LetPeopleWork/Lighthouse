# Architecture brief: story\-6364\-s03f\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Placement (feature-delta D-1, D-2, D-3; confirms the Proposed layout decision): every type that PlatformLaneMembership.MovingTypes tags for a slice's PlatformMove moves by git mv to Lighthouse.Backend/Lighthouse.Backend/Platform/<Area>, namespace Lighthouse.Backend.Platform.<Area>, no Domain/Application/Adapters layers; each port sits next to its implementation in its area. S1 KeyRing: Encryption. S2 DatabaseAndSeeding: DatabaseManagement, DemoData, Seeding (ISeeder, InstallTimestampSeeder), Persistence (LostRaceTolerantSave). S3 UsageDataLoggingAndReleases: UsageData, Logging, Releases. S4 DomainEventsAndHost: DomainEvents (DomainEventDispatcher, IDomainEventDispatcher, IDomainEventHandler<T>), Host. S5 UpdateQueue: UpdateQueue. The map's Models judgement admits no Models type (MovingTypes lists none with a Models namespace), so Models/Encryption and Models/Logging stay; P1's admitted Models/Encryption types are the empty set.
```
Governed slices: S1, S2, S3, S4, S5

#### D2

```text
What stays (D-0, D-4, D-5, D-6, D-7, D-11; confirms the Proposed AFK defaults of open questions 2, 3, 5 and 6): Data and its migrations, every repository with IRepository<T> and RepositoryBase<T>, the seven context seeders (they reach ISeeder in its new place by a using), TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their three interfaces, the domain-event handlers, the host band (Startup, Health, Configuration, Standalone, Cache, Extensions, Factories), TestData zips and every Models entity and wire type. The dispatcher contracts and the clock stay listed by full name in each module's outbound list, renamed; making them legal by namespace is its own Story. Program.cs lines, order and lifetimes stay; only usings change.
```
Governed slices: S1, S2, S3, S4, S5

#### D3

```text
Tests move with production (D-10; confirms the Proposed B4 host-test decision): each slice moves the unit tests of its moved types with git mv to Lighthouse.Backend/Lighthouse.Backend.Tests/Platform/<Area>, namespace equals folder, in its own refactor commit. Stay: tests whose subject stays (EncryptionKeyRingTests and SecretReadabilityReportTests test Models types; TeamUpdaterTest, PortfolioUpdaterTest, UpdateServiceBaseTest, RoundWriteBackDuringDatabaseMaintenanceTest and Slice01SkippedEntityLogTest test the pipelines; the three context gold tests under DomainEvents; the seeder and repository tests) and the container tests under Integration/Containers (host fixtures, selected by path). S4 also tries, in a separate edit commit, to take DomainEventDispatcherGoldTest and LighthouseClockTest off a host; if a day is not enough it records why and their HostUsersStillToMove rows stay, renamed.
```
Governed slices: S1, S2, S3, S4, S5

#### D4

```text
Rule lists and strings follow the move and are never loosened (P6): each slice rewrites, in its move commit, exactly the PlatformLaneMembership.StringsTheMoveRenames rows and SuppressionTargets of its PlatformMove (GlobalSuppressions.cs targets in S2, S3 and S4; test-time-budget.json and test_check_budget_time_budgets.py in S1; HostUsersStillToMove, CalendarDayAnchorSeamArchUnitTest and TestHostPerFixtureGuardTest in S4), plus the typeof and global:: references the build names. Feature A's membership map, PlatformLanePatternRoles and its widened rule patterns stay as delivered (confirmed by the maintainer 2026-11-22: Context patterns widen by whole Platform area, Place patterns gain nothing); the subject-aware preservation law must stay green after every slice.
```
Governed slices: S1, S2, S3, S4, S5

#### D5

```text
CryptoServiceFullName and the other renamed rule constants (a finding against feature A, decided here): measured, feature A's PlatformLaneRulesSeeTheMoveArchUnitTest is not move-stable. Its test ThePreservationCheck_LeavesAFullNameTheMoveRenames_ToTheMoveThatRenamesIt reads the live constant SecretCustodySeamArchUnitTest.CryptoServiceFullName by reflection and requires it to match the old full name and not the new one, and RulePatternsOf skips a renamed constant only when its value equals the map row's OldText. B1 must rewrite CryptoServiceFullName and EncryptionNamespacePrefix to their NewText (the map lists both as renamed by KeyRing, and the seam test looks CryptoService up by exact full name), so after B1 the first assertion fails and both constants are taken up as undeclared patterns (NotDeclared breaks). Decision: in S1 the acceptance designer, before craft, amends that one support file in two places and nothing else: (1) RulePatternsOf skips a constant whose value equals the OldText or the NewText of a StringsTheMoveRenames row of its own file; (2) the example test takes its example from the map row (file SecretCustodySeamArchUnitTest, OldText equal to CryptoService's old full name), asserts that row's OldText matches the old full name and not the new one, asserts the live constant equals that row's OldText or NewText, and keeps the not-taken-up and KernelRules.TheUpdateQueue assertions. The amended file passes before and after the move. PlatformLaneMembership and PlatformLanePatternRoles stay unedited. Rejected: keeping the constant at its old value (a stale designation pinned only for a reflection read, unused once the seam lookup uses the new name, so S1144 fails the build); replacing it by typeof (GetRawConstantValue throws on a non-literal field and the map's renamed row no longer finds its text); declaring roles for the renamed constants (edits PlatformLanePatternRoles, which stays).
```
Governed slices: S1

#### D6

```text
Oracles (one new file per slice, D-13): each is an IL architecture test over the compiled Lighthouse.Backend assembly, modelled on the S03e layout oracles (LicensingLayoutArchUnitTest). It reads its type names from PlatformLaneMembership.MovingTypes filtered by the slice's areas (never a copied list), checks each area through ModuleRules.TypesLiveInTheirLayer with the type's old namespace as former home (a name with no type fails, so the list is never vacuous; generic types by metadata name, nested types at top level), and reads the test assembly to check that the test classes the slice moves (named in the oracle, from the slice's test targets) live in Lighthouse.Backend.Tests.Platform.<Area> and none is left in its old namespace. S5 also pins that TeamUpdater, PortfolioUpdater, UpdateServiceBase`1 and their three interfaces stay in their old namespaces (from PlatformLaneMembership.TypesThatStay). RED: every listed type sits in its old namespace today.
```
Governed slices: S1, S2, S3, S4, S5

#### D7

```text
Claim-to-checker: placement, test placement, rename completeness and the preservation law are finite facts read from the compiled IL, the test assembly and repository files, checked exhaustively by the five layout oracles (Finite laws), feature A's PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest, and the EXAMINE probes architecture-rules Platform and all, ef-model-unchanged, platform-answers-compare, refresh-reactions-compare, test-selection-show, ci-live-suites-show and backend-tests. No Quint, TLA+ or Agda model is selected: no state, transition, ordering, concurrency, recovery or proof obligation changes; the update queue with its locks and notifiers, the dispatcher and the key ring move unchanged, so a model would restate the namespace map and could not fail. Liveness: not applicable, no progress claim changes.
```
Governed slices: S1, S2, S3, S4, S5

#### D8

```text
Carriers and layers (pass 6): no decision, effect, error or per-process-state carrier changes; each keeps its type, members and DI lifetime and changes only its namespace. Per-process state stays with its owner: EncryptionKeyRingHolder (key ring), UpdateQueueService and the InProcess update stores, locks and notifiers (queue state), RecentProblemsSink (recent problems), UsageDataEventQueue (pending events), DatabaseOperationTracker and DatabaseMaintenanceGate (operation state). Domain layer: none touched. Application/ports: the same success, refusal and retry outcomes (ISecretCustodyService, IDatabaseManagementService, IUpdateQueueService, IDomainEventDispatcher). Adapter/integration: Redis channel and key literals, the PostHog address, key-store directory names and the SignalR hub route are literals, not type names, so they stay. Infrastructure/recovery: the Postgres advisory lock, the Redis lock and the key-ring file watcher behave the same. Log categories change with the namespace (D-11).
```
Governed slices: S1, S2, S3, S4, S5

#### D9

```text
Push, cost and rigor (D-12, D-14; confirms the Proposed AFK defaults, maintainer not yet asked on open question 4): one commit per slice made by the root, the tests in their own refactor commit; B1-B5 pushed together once all five are green locally (fast loop and full filtered gate), so one full live run covers the move (Program.cs selects every live suite; feature A's GitHub classifier selects the GitHub suite for Platform/Releases). Rigor: S1 and S5 thorough (opus acceptance designer and crafter, sonnet reviewers); S2, S3 and S4 standard (sonnet, haiku reviewers); EXAMINE on the reviewer model. Mutation (no survivor or no-coverage mutant added, 80 percent reported per area) and the finalize checklist run after feature C, not as nwdes slices.
```
Governed slices: S1, S2, S3, S4, S5

#### D10

```text
F3-S3 (engineering choice taken in AFK mode, open to the maintainer; the same kind of exception as F2): PlatformLaneMembership.cs stays unedited. A map row of a later move (S4, S5) names a test file at its old path, and an earlier move of this feature (S3) moves that file under Tests/Platform/<Area>, so feature A's EachStringTheMoveRenames_IsFoundInItsFile_UnderItsOldOrItsNewText fails between the two moves. The S3 acceptance designer amends the support PlatformLaneMembershipArchUnitTest before craft, changing only the file lookup in WhenTheStringsTheMoveRenamesAreLookedFor: it reads the row's file at its listed path; when that path does not exist it reads the single file of the same name under Tests/Platform/<Area> (any area); it names the row as missing when there is no such file or more than one, and it never reads any other file. Every assertion and test stays, so the row holds before and after its file moves, and the made-up string row still fails. Rejected: editing the map row's path (breaks the check before the move and edits feature A's map); a craft-time exemption (loosens the rule). Applies to S3, S4 and S5.
```
Governed slices: S3, S4, S5

### Constraints

#### K1

```text
Behaviour-free: no schema, entity, migration, route, DTO, hub route, DI lifetime, registration order, configuration key, key-store directory, Redis channel or key, or UsageDataEventName change; the EF model stays the same; no type is created, merged, split or renamed and no member or signature changes; existing tests stay green, unmodified apart from moves, namespace lines, usings, full-name strings and paths; Program.cs changes only usings.
```
Governed slices: S1, S2, S3, S4, S5

#### K2

```text
Namespace equals folder for every moved file. Approved oracles of this feature are never edited, not even a using line; feature A's oracles and supports are never edited, with the S1 exceptions only: the S1 support amendments of PlatformLaneRulesSeeTheMoveArchUnitTest (CryptoServiceFullName decision, and honouring F4's per-type exception), PlatformLaneMembershipArchUnitTest (F2) and PlatformLanePatternRoles (F4's per-type exception for PromisesAndTheUpdateCode and KeyRingFileWatcher), made by the S1 acceptance designer before craft; and the S3 support amendment of PlatformLaneMembershipArchUnitTest (F3-S3: where a listed string's file has moved, the check reads the one file of that name under Tests/Platform), made by the S3 acceptance designer before craft; and the S4 support amendments of PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest (F5-S4: only the choice of example in two tests, so each builds the failure it asserts before and after every move), made by the S4 acceptance designer before the re-craft. The S1 maintainer decisions F1 (named log exemption in SecretCustodySeamArchUnitTest), F3 (2 listed ICryptoService Application edges and the fixture offender set in TrackerIntegrationModuleBoundariesArchUnitTest) and F4 (no Platform.Encryption alternative in PromisesAndTheUpdateCode or its composites there) are the only edits of existing tests beyond K1's moves, namespace lines, usings, full-name strings and paths, made by the S1 crafter. A crafter target is never an oracle support. One commit per slice plus its tests refactor commit; GREEN and every review run on a clean committed tree; never git stash; never loosen a rule.
```
Governed slices: S1, S2, S3, S4, S5

#### K3

```text
Input bounds: no stored or validated input changes and none is introduced; key-ring files, the licence fixture and TestData zips keep their bytes. Out of scope: the platform lane's own rules and the unit-test placement rule (feature C), feature A's map, law, classifier and probe, Data, repositories, context seeders, update pipelines, domain-event handlers, the host band, Models entity and wire types, any schema, configuration, signature or behaviour change.
```
Governed slices: S1, S2, S3, S4, S5

### Reuse

TypesLiveInTheirLayer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:150

Reuse

```text
the S03e layout check (names expected in a namespace, a missing name fails, a type left in the former home is named); every slice oracle reuses it per area with the old namespace as former home
```

MovingTypes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Reuse

```text
feature A's measured membership map, namespace-agnostic: every slice oracle filters it by area instead of copying the names, so the map stays the single list
```

TypesThatStay

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:262

Reuse

```text
names the pipeline types that stay, so the S5 oracle pins them without a second list
```

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Producer and consumer ship in one deployable and change in the same commit; the move changes no wire, persisted or Redis shape, so the unchanged existing tests, platform-answers-compare and the layout oracles cover it
```

### Boundaries

```text
In-process: the layout ArchUnit oracles read the compiled Lighthouse.Backend assembly, the test assembly (IlDependencies, ModuleRules) and repository files, run by dotnet test; the existing HTTP endpoints, SignalR hub and application-service ports (ISecretCustodyService, IDatabaseManagementService, IUpdateQueueService, IDomainEventDispatcher, ILighthouseClock) are unchanged and observed by EXAMINE's platform-answers-compare and refresh-reactions-compare
```

#### Driven ports

```text
none changed: LighthouseAppContext, the repositories, Redis, Postgres, PostHog, GitHub, the file system and the key store stay behind their existing adapters, which move with their area
```

```text
Platform sits below the contexts: Program.cs, API and the contexts reach Platform/<Area>; a platform type reaches what it reaches today (Models, Data, repositories, the listed context code still in Services); no edge is added or removed, only namespaces change
```

#### Failures

None.

### Observations

```text
Namespace of each PlatformLaneMembership.MovingTypes entry in the compiled Lighthouse.Backend assembly, and of each moved test class in the test assembly
```

namespace per type name

```text
a behaviour change inside a moved type, or a member change that keeps the name
```

```text
EXAMINE answers before and after: platform-answers-compare (system info, version, encryption and secrets state, database-management status, usage-data state, log level and problems, update status), refresh-reactions-compare (domain-event reaction order), ef-model-unchanged, test-selection-show, ci-live-suites-show, backend-tests
```

probe verdict per expectation

```text
Redis and Postgres update locks under a real cluster (container tests, unchanged), a key ring on a mounted file (unit tests), the GitHub calls of the version check (GithubIntegration on the push)
```

### Equality

layout

```text
two candidates are layout-equal when every moving type of the slice's PlatformMove resolves to exactly one type whose namespace is Lighthouse.Backend.Platform.<Area> and no type of that name remains in its old namespace
```

behaviour

```text
two revisions are behaviour-equal when platform-answers-compare reports identical bodies after dropping the fields it names as volatile, refresh-reactions-compare reports the same reactions in the same order, the EF model is unchanged and the full filtered backend suite and every live suite pass
```

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
a moving type in Lighthouse.Backend.Platform.<Area> after its slice, in its old namespace before it
```

None.

Excluded

```text
a moving type left in its old namespace after its slice, present in both namespaces, placed in a Platform area other than its map area, or a stay type (pipelines, handlers, context seeders, repositories) moved
```

None.

Unknown

```text
an operator's own log-level override that names an old platform namespace loses effect after the move (none documented)
```

maintainer

### Models

None.

### Closure

```text
platform lane move
```

Safety

Pass

DesignedNotBuilt

```text
each layout oracle fails naming the type when a moving type of its slice is in its old namespace, in both or in the wrong area
```

##### Evidence

None.

```text
platform lane move
```

Inhabitation

Pass

DesignedNotBuilt

```text
each oracle resolves every listed name to a type (a name with no type fails), so the admitted layout has a concrete witness per type
```

##### Evidence

None.

```text
platform lane move
```

PublicReachability

Pass

DesignedNotBuilt

```text
the oracles run by dotnet test over the compiled assembly; behaviour through HTTP, the hub and the application-service ports is observed by EXAMINE's platform-answers-compare and refresh-reactions-compare
```

##### Evidence

None.

```text
platform lane move
```

Liveness

Pass

DesignedNotBuilt

```text
not applicable: no progress, scheduling or fairness claim changes; the update queue, locks and dispatcher move unchanged
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: rules name the lane by area. They name it as Services; a move would drop types from about 20 patterns
```

```text
every rule keeps its verdict for every moved type
```

```text
a rule pattern gains a Platform alternative only through feature A's roles
```

```text
a rule goes blind or sees a type outside Platform
```

```text
a pattern built at run time from pieces is outside the reflection scan; both-way list comparisons fail loudly on rename
```

acceptance designer

#### Y2

Stressor

```text
Assumption negated: renamed rule constants keep their old value. The map renames CryptoServiceFullName and EncryptionNamespacePrefix in B1
```

```text
the preservation law stays green after B1
```

```text
the S1 support amendment of feature A's test
```

```text
the law reports NotDeclared or the example test fails after a correct rename
```

```text
another renamed constant outside StringsTheMoveRenames: the law names it and the slice's acceptance designer takes it to the architect
```

acceptance designer

#### Y3

Stressor

```text
Assumption negated: test budgets are keyed by file. They are keyed by class full name
```

```text
the budget check finds the four Encryption test classes
```

```text
S1 renames the keys
```

```text
a moved test class with no budget entry passes unseen
```

```text
a missed entry: the budget check fails loudly
```

crafter

#### Y4

Stressor

```text
Assumption negated: CI selects suites by content. GitHub is selected by path
```

```text
the GitHub suite runs when Platform/Releases changes
```

```text
feature A widened the classifier first
```

```text
the release move pushes without the GitHub suite
```

```text
another path-keyed selector: test-selection-show in EXAMINE
```

maintainer

#### Y5

Stressor

```text
Assumption negated: each B push is local. Every B slice edits Program.cs usings, which select every live suite
```

```text
every live suite passes on the move push
```

```text
one push for B1-B5
```

```text
a skipped suite
```

```text
429 risk on the shared Linear key, one live run only
```

maintainer

#### Y6

Bypass

```text
Reflection, configuration and serialized type names: a type found by its full name at run time (Serilog categories, SuppressMessage targets, reflection in tests)
```

```text
the same log levels and suppressions
```

```text
log categories change with the namespace
```

```text
a suppression target or test string left on the old name
```

```text
operators' own overrides on old namespaces; suppression targets are on the map and the build fails if one is missed
```

maintainer

#### Y7

SinglePointOfFailure

```text
Consumer target lists are measured by whole-word name search (noisy): a file that reaches a moved type only through an extension method or a sibling namespace may be missing
```

```text
the build names every file that needs a using
```

```text
the crafter adds a using to a file the build names and reports it to the architect
```

```text
a silent edit outside the targets
```

```text
the build is the completeness check; a file outside the targets goes to the architect to amend the targets
```

architect

#### Y8

Stressor

```text
Second set, walkthrough only: a worktree without the premium licence fixture; the Standalone build; a Redis cluster deployment
```

```text
Licensing failures are environmental; namespaces do not change the assembly name; channel literals unchanged
```

```text
none
```

```text
none
```

```text
none found by the walkthrough
```

maintainer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyRingFileWatcher\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/KeyRingFileWatcher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/CryptoService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/CryptoService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/AKeyOnlyItsOwnerCanReplace\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/AKeyOnlyItsOwnerCanReplace.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ConfiguredKeyRingSource\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ConfiguredKeyRingSource.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ConnectionSecrets\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ConnectionSecrets.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/DatabaseSecretPresenceProbe\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/DatabaseSecretPresenceProbe.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/EncryptionKeyRingBootstrapper\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/EncryptionKeyRingBootstrapper.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/EncryptionKeyRingHolder\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/EncryptionKeyRingHolder.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/GeneratedKeyRingMinter\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/GeneratedKeyRingMinter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/GeneratedKeyRingStore\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/GeneratedKeyRingStore.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyRingSerializer\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyRingSerializer.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreFile\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreFile.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreMigration\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreMigration.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreResolver\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/LegacyDefaultEncryptionKey\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/MintingNotPermittedException\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/MintingNotPermittedException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/MountedFileKeyRingSource\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/MountedFileKeyRingSource.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/OneSecretPassAtATime\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/OneSecretPassAtATime.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/PublishedKeySecretCount\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/PublishedKeySecretCount.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ReadableSecretsNotOnTheActiveKey\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ReadableSecretsNotOnTheActiveKey.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ReferencedKeyIds\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ReferencedKeyIds.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretCustodyService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretCustodyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretEnvelope\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretEnvelope.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretStateClassifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretStateClassifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretLocations\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretLocations.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretReadabilityProbe\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretReadabilityProbe.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretSummaryReader\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretSummaryReader.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/UnreadableSecretException\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/UnreadableSecretException.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/UnreadableStoredSecrets\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/UnreadableStoredSecrets.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/WhereTheKeyCameFrom\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/WhereTheKeyCameFrom.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IEncryptionKeyRingHolder\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IEncryptionKeyRingHolder.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IKeyRingMinter\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IKeyRingMinter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IPublishedKeySecretCount\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IPublishedKeySecretCount.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IReadableSecretsNotOnTheActiveKey\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IReadableSecretsNotOnTheActiveKey.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IReferencedKeyIds\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IReferencedKeyIds.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ISecretCustodyService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/ISecretCustodyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IStoredSecretSummary\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IStoredSecretSummary.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ICryptoService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ICryptoService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyRingFileWatcherTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/KeyRingFileWatcherTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/CryptoServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/CryptoServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ConfiguredKeyRingSourceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ConfiguredKeyRingSourceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ConnectionSecretsTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ConnectionSecretsTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/CryptoServiceNamedKeyTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/CryptoServiceNamedKeyTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/EncryptionKeyRingBootstrapperTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/EncryptionKeyRingBootstrapperTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/EncryptionKeyRingHolderTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/EncryptionKeyRingHolderTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/GeneratedKeyRingStoreTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/GeneratedKeyRingStoreTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyRingSerializerTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyRingSerializerTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyStoreFileTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyStoreFileTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyStoreResolverTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyStoreResolverTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/LegacyDefaultEncryptionKeyTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/LegacyDefaultEncryptionKeyTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/PublishedKeySecretCountTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/PublishedKeySecretCountTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ReadableSecretsNotOnTheActiveKeyTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ReadableSecretsNotOnTheActiveKeyTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ReferencedKeyIdsTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ReferencedKeyIdsTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretCustodyServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretCustodyServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretEnvelopeTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretEnvelopeTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretStateClassifierTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretStateClassifierTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/StoredSecretReadabilityProbeTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/StoredSecretReadabilityProbeTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/StoredSecretSummaryReaderTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/StoredSecretSummaryReaderTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/WhereTheKeyCameFromTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/WhereTheKeyCameFromTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretCustodySeamArchUnitTest\.cs

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs; F1: NothingThatResolvesOrKeepsAKey_CanWriteToALogAtAll exempts exactly CryptoService and KeyRingFileWatcher by name, with the reason beside it, so its subjects stay today's set
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretRotationWriteSurfaceTest\.cs

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs; F3: list the 2 ICryptoService edges ConnectionHealthService and OAuthService in ApplicationEdgesBelowItIntoAdaptersOrThePlatform with the reason "existed before the move: ICryptoService was a loose platform interface the rule did not see", add no entry to ModuleEdgesIntoOtherContexts, and make ATrackerIntegrationApplicationTypeReadingTheKeyRingOffTheList_IsNamed and its EveryEntryDeleted case expect the fixture's new offender set; F4: PromisesAndTheUpdateCode and every composite built from it carry no Platform.Encryption alternative
```

Scripts\/test\-timings\/test\-time\-budget\.json

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Scripts\/test\-timings\/tests\/test\_check\_budget\_time\_budgets\.py

Extend

```text
rewrite the KeyRing rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EncryptionRecordTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/Story6084HistoryFillAlwaysOnSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/EncryptionControllerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthCallbackCsrfIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NoPendingModelChangesTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/RecurringBlackoutRulePersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/RefreshLogFindingsPersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportsTableConstraintsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/SecretEncryptionOnSaveTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/DataProtectionKeyRingTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/DatabaseConnectivityHealthCheckTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/MigrationsAppliedHealthCheckTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedCountSnapshotMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedDurationMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedRuleSetMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedStalenessThresholdMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CategoryListNormalisationSeederIsolationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CategoryListNormalisationSeederOnBothProvidersTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ConcurrentStartupMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ConnectionDefaultsUpgradeMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/DeliveryMetricSnapshotDayKeyMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/FeatureDependencyReferenceMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/FeatureListParityOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputFingerprintAcrossProvidersTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputsCompareAndSetOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastOnTheLaneIdentityOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LongSecretRoundTripTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ParentSourcesUpgradeMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PercentilesOverTimeSnapshotMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PortfolioMetricsReadsParityOnBothProvidersTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PredefinedAdditionalFieldMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ProcessBehaviorSnapshotMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ReEncryptionCompareAndSwapProbeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/RefreshLogSkippedColumnMigrationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ViewerEmbedStorageGuaranteeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/KeyRingFileReloadTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/LighthouseAppContextUtcTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/DeliverySourceBindingPersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/OidcGroupSnapshotWriterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepositoryTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepositoryTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepositoryTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilderTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategyTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearFetchRefusalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/Bug5023JiraRefreshRotationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshSingleFlightTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBaseTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DatabaseManagementServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Encryption\/EncryptionKeyRingTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/LostRaceTolerantSaveTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureGraphReaderRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureListReaderRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeaturesWithReferenceIdsRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastConfirmationRepositoryRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastGatherReaderRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/PortfolioCatalogueReaderRelationalTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/PortfolioRepositoryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ReportRepositoryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SystemInfoServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StalenessThresholdPersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/EncryptionBootstrapOrderTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/KeyStoreDirectoryResolutionTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StartupBannerEncryptionKeyLineTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StartupBannerForecastLaneLineTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FakeCryptoService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestAuthStrategyFactory\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/WorkItemStateTransitionPersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/EncryptionStateDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionOptionDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContextFactory\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/JiraCloudBasicAuthStrategy\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/LinearApiKeyAuthStrategy\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/PatAuthStrategy\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategy\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingConnector\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/SystemInfoService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Startup\/StartupBanner\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformEncryptionLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformEncryptionLayoutArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLane"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
python -m pytest Scripts/test-timings/tests/test_check_budget_time_budgets.py
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLanePatternRoles\.cs

#### Public oracle

```text
Run PlatformEncryptionLayoutArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly: take every PlatformLaneMembership.MovingTypes entry of the KeyRing move (areas Encryption), look its name up in its new namespace and in its old one, and look each test class the slice moves up in Lighthouse.Backend.Tests.Platform.<Area> and in its old namespace
```

```text
Every KeyRing type resolves to exactly one type in Lighthouse.Backend.Platform.<its area> and none is left in its old namespace (CryptoService, ICryptoService and KeyRingFileWatcher included, so every KeyRing type live in Lighthouse.Backend.Platform.Encryption; test-time-budget.json and test_check_budget_time_budgets.py name SecretCustodyServiceTests, PublishedKeySecretCountTests, ReferencedKeyIdsTests and ReadableSecretsNotOnTheActiveKeyTests under Lighthouse.Backend.Tests.Platform.Encryption and the budget check passes); each moved test class lives in Lighthouse.Backend.Tests.Platform.<Area>; PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest stay green and the existing suite stays green
```

```text
A KeyRing type left in its old namespace, present in both, placed in another area, a listed name matching no type (vacuous list), or a moved test class left in its old namespace: the test fails naming the type. Today every listed type sits in its old namespace, so the test is RED
```

##### Promises

P1

P6

P7

#### Prefactoring

None.

#### Records

##### Decisions

```text
F1 (maintainer, 2026-11-22): SecretCustodySeamArchUnitTest.NothingThatResolvesOrKeepsAKey_CanWriteToALogAtAll gains a named exemption of exactly CryptoService and KeyRingFileWatcher, the two key-handling types that log and that the rule kept outside its namespace for that reason; once EncryptionNamespacePrefix becomes Lighthouse.Backend.Platform.Encryption. the rule's subjects stay exactly today's set. The reason is written beside the exemption in plain language. Owner: maintainer. S1 crafter target. Rejected: a separate Platform area or sub-namespace for the two types (contradicts the one-area placement of the membership map); a narrower subject pattern (changes the rule's reach for future types).
```

```text
F2 (maintainer, 2026-11-22): feature A's PlatformLaneMembershipArchUnitTest tests ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice and ATypeMovedIntoTheWrongArea_IsNamedAsNotInTheCode_AndAsNotOnTheMap take their example from the membership map (its row's OldFullName and NewFullName, using whichever full name the compiled assembly holds), so they build the failure they assert before and after the move. A recorded exception of the same kind as the PlatformLaneRulesSeeTheMove amendment: made by the S1 acceptance designer before craft, as an S1 support, changing only the choice of example; every assertion stays. PlatformLaneMembership stays unedited; PlatformLanePatternRoles changes only by F4's per-type exception. Owner: maintainer. Rejected: an example type of a later move (breaks again at that move).
```

```text
F3 (maintainer, 2026-11-22, narrowed by F4): the whole-area Context pattern ^Lighthouse.Backend.Platform.Encryption(.|$) makes 2 existing ICryptoService edges visible. TrackerIntegrationModuleBoundariesArchUnitTest lists them in ApplicationEdgesBelowItIntoAdaptersOrThePlatform, ConnectionHealthService and OAuthService to Lighthouse.Backend.Platform.Encryption.ICryptoService, with the reason "existed before the move: ICryptoService was a loose platform interface the rule did not see". ModuleEdgesIntoOtherContexts gains no entry: under F4 the other-contexts pattern does not widen, so the connector edges stay outside it as before. The fixture test ATrackerIntegrationApplicationTypeReadingTheKeyRingOffTheList_IsNamed and the matching EveryEntryDeletedFromATrackerIntegrationList case expect the new offender set of the AConnectionServiceReadingSecrets fixture (ConnectionSecrets and ICryptoService in Platform.Encryption). No other entry changes, so the rule stays as strict as before. Owner: maintainer. S1 crafter targets.
```

```text
F4 (maintainer, 2026-11-22, "the pattern does not widen"): KeyRingFileWatcher matched TrackerIntegrationModuleBoundariesArchUnitTest.PromisesAndTheUpdateCode only through its old BackgroundServices folder: a Place match of that one type, not the Promises or update context. So PromisesAndTheUpdateCode, and every composite built from it, gets no Platform.Encryption alternative, and TrackerIntegration_ReachesNoOtherContext stays as it is. Carrier: feature A's PlatformLanePatternRoles gains a per-type exception ("this field's match of type T was folder-only; T's area is not widened for this field") naming PromisesAndTheUpdateCode and KeyRingFileWatcher, and PlatformLaneRulesSeeTheMoveArchUnitTest honours it, so the preservation law does not demand the Encryption widening for that field only; every other role and widening stays. Both are S1 supports, edited by the S1 acceptance designer before craft, a recorded exception of the same kind as F2. The S1 crafter keeps the Encryption area out of PromisesAndTheUpdateCode and every composite built from it (removing any alternative the rejected craft added). Owner: maintainer. Rejected: widening the pattern by whole area (the other-contexts pattern would take in all of Platform.Encryption, which it never covered).
```

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyRingFileWatcher\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/KeyRingFileWatcher.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/CryptoService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/CryptoService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/AKeyOnlyItsOwnerCanReplace\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/AKeyOnlyItsOwnerCanReplace.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ConfiguredKeyRingSource\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ConfiguredKeyRingSource.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ConnectionSecrets\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ConnectionSecrets.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/DatabaseSecretPresenceProbe\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/DatabaseSecretPresenceProbe.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/EncryptionKeyRingBootstrapper\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/EncryptionKeyRingBootstrapper.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/EncryptionKeyRingHolder\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/EncryptionKeyRingHolder.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/GeneratedKeyRingMinter\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/GeneratedKeyRingMinter.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/GeneratedKeyRingStore\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/GeneratedKeyRingStore.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyRingSerializer\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyRingSerializer.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreFile\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreFile.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreMigration\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreMigration.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/KeyStoreResolver\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/KeyStoreResolver.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/LegacyDefaultEncryptionKey\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/MintingNotPermittedException\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/MintingNotPermittedException.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/MountedFileKeyRingSource\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/MountedFileKeyRingSource.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/OneSecretPassAtATime\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/OneSecretPassAtATime.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/PublishedKeySecretCount\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/PublishedKeySecretCount.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ReadableSecretsNotOnTheActiveKey\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ReadableSecretsNotOnTheActiveKey.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ReferencedKeyIds\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/ReferencedKeyIds.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretCustodyService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretCustodyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretEnvelope\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretEnvelope.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/SecretStateClassifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/SecretStateClassifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretLocations\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretLocations.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretReadabilityProbe\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretReadabilityProbe.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/StoredSecretSummaryReader\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/StoredSecretSummaryReader.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/UnreadableSecretException\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/UnreadableSecretException.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/UnreadableStoredSecrets\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/UnreadableStoredSecrets.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/WhereTheKeyCameFrom\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/WhereTheKeyCameFrom.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IEncryptionKeyRingHolder\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IEncryptionKeyRingHolder.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IKeyRingMinter\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IKeyRingMinter.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IPublishedKeySecretCount\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IPublishedKeySecretCount.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IReadableSecretsNotOnTheActiveKey\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IReadableSecretsNotOnTheActiveKey.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IReferencedKeyIds\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IReferencedKeyIds.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ISecretCustodyService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/ISecretCustodyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/IStoredSecretSummary\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Encryption/IStoredSecretSummary.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Encryption\/ICryptoService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ICryptoService.cs; no member changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyRingFileWatcherTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/KeyRingFileWatcherTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/CryptoServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/CryptoServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ConfiguredKeyRingSourceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ConfiguredKeyRingSourceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ConnectionSecretsTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ConnectionSecretsTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/CryptoServiceNamedKeyTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/CryptoServiceNamedKeyTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/EncryptionKeyRingBootstrapperTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/EncryptionKeyRingBootstrapperTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/EncryptionKeyRingHolderTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/EncryptionKeyRingHolderTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/GeneratedKeyRingStoreTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/GeneratedKeyRingStoreTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyRingSerializerTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyRingSerializerTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyStoreFileTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyStoreFileTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/KeyStoreResolverTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/KeyStoreResolverTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/LegacyDefaultEncryptionKeyTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/LegacyDefaultEncryptionKeyTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/PublishedKeySecretCountTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/PublishedKeySecretCountTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ReadableSecretsNotOnTheActiveKeyTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ReadableSecretsNotOnTheActiveKeyTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/ReferencedKeyIdsTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/ReferencedKeyIdsTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretCustodyServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretCustodyServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretEnvelopeTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretEnvelopeTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/SecretStateClassifierTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/SecretStateClassifierTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/StoredSecretReadabilityProbeTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/StoredSecretReadabilityProbeTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/StoredSecretSummaryReaderTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/StoredSecretSummaryReaderTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Encryption\/WhereTheKeyCameFromTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Encryption/WhereTheKeyCameFromTests.cs; no test changes
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretCustodySeamArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretRotationWriteSurfaceTest\.cs\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Scripts\/test\-timings\/test\-time\-budget\.json\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Scripts\/test\-timings\/tests\/test\_check\_budget\_time\_budgets\.py\:1

Changed

```text
the strings the map lists for the KeyRing move name the new place; no rule loosened
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/EncryptionRecordTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/Story6084HistoryFillAlwaysOnSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionValidationUnreadableSecretTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/EncryptionControllerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthCallbackCsrfIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthControllerIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WriteBackLicenseGateTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NoPendingModelChangesTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/RecurringBlackoutRulePersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/RefreshLogFindingsPersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportsTableConstraintsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/SecretEncryptionOnSaveTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/DataProtectionKeyRingTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/DatabaseConnectivityHealthCheckTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/MigrationsAppliedHealthCheckTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedCountSnapshotMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedDurationMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedRuleSetMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BlockedStalenessThresholdMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CategoryListNormalisationSeederIsolationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CategoryListNormalisationSeederOnBothProvidersTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ConcurrentStartupMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ConnectionDefaultsUpgradeMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/DeliveryMetricSnapshotDayKeyMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/FeatureDependencyReferenceMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/FeatureListParityOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputFingerprintAcrossProvidersTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastInputsCompareAndSetOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ForecastOnTheLaneIdentityOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LongSecretRoundTripTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ParentSourcesUpgradeMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PercentilesOverTimeSnapshotMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PortfolioMetricsReadsParityOnBothProvidersTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PredefinedAdditionalFieldMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ProcessBehaviorSnapshotMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ReEncryptionCompareAndSwapProbeTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/RefreshLogSkippedColumnMigrationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ViewerEmbedStorageGuaranteeTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/KeyRingFileReloadTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/LighthouseAppContextUtcTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/DeliverySourceBindingPersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CreateRightsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/CurrentUserProfileServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/OidcGroupSnapshotWriterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/RbacAdministrationServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/BlockedCountSnapshotRepositoryTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/PercentilesOverTimeSnapshotRepositoryTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Persistence\/ProcessBehaviorSnapshotRepositoryTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/BlockedCountSeriesBuilderTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategyTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/WorkTrackingAuthStrategyTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearFetchRefusalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/Bug5023JiraRefreshRotationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshSingleFlightTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DependencyVerdictIdentityTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBaseTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DatabaseManagementServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Encryption\/EncryptionKeyRingTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/LostRaceTolerantSaveTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureGraphReaderRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeatureListReaderRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/FeaturesWithReferenceIdsRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastConfirmationRepositoryRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ForecastGatherReaderRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/PortfolioCatalogueReaderRelationalTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/PortfolioRepositoryTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Repositories\/ReportRepositoryTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SystemInfoServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StalenessThresholdPersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/EncryptionBootstrapOrderTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/KeyStoreDirectoryResolutionTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StartupBannerEncryptionKeyLineTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/StartupBannerForecastLaneLineTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/FakeCryptoService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/TestAuthStrategyFactory\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/WorkItemStateTransitionPersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/EncryptionStateDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionOptionDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/EncryptionController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContextFactory\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/JiraCloudBasicAuthStrategy\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/LinearApiKeyAuthStrategy\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/PatAuthStrategy\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Auth\/ServiceNowBasicAuthStrategy\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Linear\/LinearWorkTrackingConnector\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/SystemInfoService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Startup\/StartupBanner\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

##### Agreement

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Producer

UnchangedCompatible

```text
the map lists each type by old namespace and finds it in the old or the new one, so it holds before, during and after the move; not edited
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:297

Consumer

UnchangedCompatible

```text
accepts the old or the new text of each renamed string and the old or new namespace of each type
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:472

Consumer

Migrated

```text
skips a renamed constant only under its OldText and its example test pins the pre-move value; the S1 support amendment makes it accept OldText or NewText, before and after the move
```

test\-time\-budget\.json keys \(test class full names\)

Scripts\/test\-timings\/test\-time\-budget\.json\:1

Consumer

Migrated

```text
the four Encryption budget keys and their Python test are renamed in the S1 move commit
```

##### Not applicable

prefactoring

```text
the slice is itself a behaviour-free move; no behaviour change follows it in this feature that a preparatory move would make easier
```

reuse

```text
every target is an existing file moved by git mv or an existing file whose using lines and strings follow the move; no responsibility is created, so each target reuses its own current owner unchanged
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
For each KeyRing entry t of PlatformLaneMembership.MovingTypes: exactly one type named t.Name exists in t.NewNamespace and none in t.OldNamespace; for each test class the slice moves, it exists in Lighthouse.Backend.Tests.Platform.<Area> and not in its old namespace
```

Invariant

Infrastructure

PlatformEncryptionLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
every listed type in its new namespace: pass
```

```text
one listed type left in its old namespace: fails naming it
```

```text
a listed name with no type anywhere: fails naming it
```

```text
a moved test class left in its old namespace: fails naming it
```

```text
the domain is the finite measured map; every entry is checked, so generation adds nothing
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
every KeyRing type of PlatformLaneMembership lives in Lighthouse.Backend.Platform.<Area> and none in its old namespace; the slice's moved test classes live in Lighthouse.Backend.Tests.Platform.<Area>
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformEncryptionLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformEncryptionLayoutArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/CommandRunner\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/CommandRunner.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseMaintenanceGate\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseMaintenanceGate.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseManagementService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseManagementService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationTracker\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseOperationTracker.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/PostgresDatabaseManagementProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/SqliteDatabaseManagementProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/DemoDataService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DemoDataService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Persistence\/LostRaceTolerantSave\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LostRaceTolerantSave.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/InstallTimestampSeeder\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Seeding/InstallTimestampSeeder.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseCapabilityStatus\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseCapabilityStatus.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationFailure\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationFailure.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationState\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationState.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationStatus\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationStatus.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationType\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationType.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/ICommandRunner\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/ICommandRunner.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IDatabaseManagementProvider\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IDatabaseManagementProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IDatabaseManagementService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IDatabaseManagementService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IStagedBackup\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IStagedBackup.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/UnreadableCredentials\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/UnreadableCredentials.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/IDemoDataService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IDemoDataService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/ISeeder\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Seeding/ISeeder.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseMaintenanceGateTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseMaintenanceGateTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseManagementServiceTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseManagementServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseOperationTrackerTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseOperationTrackerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DemoData\/DemoDataServiceTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DemoDataServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Persistence\/LostRaceTolerantSaveTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LostRaceTolerantSaveTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/PostgresDatabaseManagementProviderTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PostgresDatabaseManagementProviderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/SqliteDatabaseManagementProviderTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SqliteDatabaseManagementProviderTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
rewrite the DatabaseAndSeeding rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs

Extend

```text
rewrite the DatabaseAndSeeding rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs

Extend

```text
rewrite the DatabaseAndSeeding rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesPortfolioReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/Story6084HistoryFillAlwaysOnSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CumulativeStateTimePortfolioReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CumulativeStateTimeReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionOverAMappingIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FlowEfficiencyPortfolioReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FlowEfficiencyReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/ForecastRealityCheckAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeCumulativeScopeIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimePortfolioIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeTrendInfoApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioTimeInStateReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TimeInStateReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OptionalFeatureApplierSeamArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/EveryoneSignedInWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersNoLimitsWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BulkReportOwnerDeletedMidRunOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CatalogueBodiesOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/CancelAllWhileWorkIsAdmittedTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateQueueServiceTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/IntegrationTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/DatabaseConfigurator\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastInputsForgottenAtStartSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateQueueService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/ApiKeyOwnerReconciliationSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/AppSettingSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/CategoryListNormalisationSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/EveryoneSignedInSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/OptionalFeatureSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/RefreshLogSeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/TerminologySeeder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformDatabaseAndSeedingLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformDatabaseAndSeedingLayoutArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLane"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run PlatformDatabaseAndSeedingLayoutArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly: take every PlatformLaneMembership.MovingTypes entry of the DatabaseAndSeeding move (areas DatabaseManagement, DemoData, Seeding and Persistence), look its name up in its new namespace and in its old one, and look each test class the slice moves up in Lighthouse.Backend.Tests.Platform.<Area> and in its old namespace
```

```text
Every DatabaseAndSeeding type resolves to exactly one type in Lighthouse.Backend.Platform.<its area> and none is left in its old namespace (every DatabaseAndSeeding type live in Platform.DatabaseManagement, Platform.DemoData, Platform.Seeding (ISeeder, InstallTimestampSeeder) and Platform.Persistence (LostRaceTolerantSave); the seven context seeders stay in Services.Implementation.Seeding; the GlobalSuppressions.cs target of PostgresDatabaseManagementProvider names its new full name and dotnet build is clean); each moved test class lives in Lighthouse.Backend.Tests.Platform.<Area>; PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest stay green and the existing suite stays green
```

```text
A DatabaseAndSeeding type left in its old namespace, present in both, placed in another area, a listed name matching no type (vacuous list), or a moved test class left in its old namespace: the test fails naming the type. Today every listed type sits in its old namespace, so the test is RED
```

##### Promises

P2

P6

P7

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

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/CommandRunner\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/CommandRunner.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseMaintenanceGate\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseMaintenanceGate.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseManagementService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseManagementService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationTracker\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseOperationTracker.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/PostgresDatabaseManagementProvider.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/SqliteDatabaseManagementProvider.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/DemoDataService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DemoDataService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Persistence\/LostRaceTolerantSave\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LostRaceTolerantSave.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/InstallTimestampSeeder\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Seeding/InstallTimestampSeeder.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseCapabilityStatus\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseCapabilityStatus.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationFailure\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationFailure.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationState\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationState.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationStatus\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationStatus.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseOperationType\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/DatabaseOperationType.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/ICommandRunner\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/ICommandRunner.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IDatabaseManagementProvider\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IDatabaseManagementProvider.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IDatabaseManagementService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IDatabaseManagementService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/IStagedBackup\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/IStagedBackup.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/UnreadableCredentials\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DatabaseManagement/UnreadableCredentials.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/IDemoDataService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IDemoDataService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Seeding\/ISeeder\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Seeding/ISeeder.cs; no member changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseMaintenanceGateTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseMaintenanceGateTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseManagementServiceTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseManagementServiceTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseOperationTrackerTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DatabaseOperationTrackerTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DemoData\/DemoDataServiceTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DemoDataServiceTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Persistence\/LostRaceTolerantSaveTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LostRaceTolerantSaveTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/PostgresDatabaseManagementProviderTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/PostgresDatabaseManagementProviderTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/SqliteDatabaseManagementProviderTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SqliteDatabaseManagementProviderTest.cs; no test changes
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DatabaseAndSeeding move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs\:1

Changed

```text
the strings the map lists for the DatabaseAndSeeding move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs\:1

Changed

```text
the strings the map lists for the DatabaseAndSeeding move name the new place; no rule loosened
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DemoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesPortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/AgeInStatePercentilesReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/BehaviourSettingsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/Story6084HistoryFillAlwaysOnSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CategoryListsHoldRawStatesSettingsIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CumulativeStateTimePortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CumulativeStateTimeReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionOverAMappingIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/CycleTimeDefinitionValidityIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliverySources\/DeliverySourcesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Dependencies\/DependenciesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FeatureMoveAuthOffIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FlowEfficiencyPortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FlowEfficiencyReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/ForecastRealityCheckAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeCumulativeScopeIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimePortfolioIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/NamedCycleTimeTrendInfoApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OnlyFeaturesAsParentCandidates\/ParentPlacementAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ParentFromIssueLinks\/Slice03AmbiguityRefusalSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioTimeInStateReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TimeInStateReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OptionalFeatureApplierSeamArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/EveryoneSignedInWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersNoLimitsWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/BulkReportOwnerDeletedMidRunOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CatalogueBodiesOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/LighterRoundsOnPostgresTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/TeamSizingUsageEventsTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/CancelAllWhileWorkIsAdmittedTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateQueueServiceTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/IntegrationTestBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DemoController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/DatabaseConfigurator\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastInputsForgottenAtStartSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateQueueService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/ApiKeyOwnerReconciliationSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/AppSettingSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/CategoryListNormalisationSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/EveryoneSignedInSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/OptionalFeatureSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/RefreshLogSeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/TerminologySeeder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

##### Agreement

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Producer

UnchangedCompatible

```text
the map lists each type by old namespace and finds it in the old or the new one, so it holds before, during and after the move; not edited
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:297

Consumer

UnchangedCompatible

```text
accepts the old or the new text of each renamed string and the old or new namespace of each type
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:472

Consumer

UnchangedCompatible

```text
after the S1 amendment it skips a renamed constant under its OldText or NewText, so this move needs no change to it
```

##### Not applicable

prefactoring

```text
the slice is itself a behaviour-free move; no behaviour change follows it in this feature that a preparatory move would make easier
```

reuse

```text
every target is an existing file moved by git mv or an existing file whose using lines and strings follow the move; no responsibility is created, so each target reuses its own current owner unchanged
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
For each DatabaseAndSeeding entry t of PlatformLaneMembership.MovingTypes: exactly one type named t.Name exists in t.NewNamespace and none in t.OldNamespace; for each test class the slice moves, it exists in Lighthouse.Backend.Tests.Platform.<Area> and not in its old namespace
```

Invariant

Infrastructure

PlatformDatabaseAndSeedingLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
every listed type in its new namespace: pass
```

```text
one listed type left in its old namespace: fails naming it
```

```text
a listed name with no type anywhere: fails naming it
```

```text
a moved test class left in its old namespace: fails naming it
```

```text
the domain is the finite measured map; every entry is checked, so generation adds nothing
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
every DatabaseAndSeeding type of PlatformLaneMembership lives in Lighthouse.Backend.Platform.<Area> and none in its old namespace; the slice's moved test classes live in Lighthouse.Backend.Tests.Platform.<Area>
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformDatabaseAndSeedingLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformDatabaseAndSeedingLayoutArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/AssemblyService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/AssemblyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentPruningService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/UsageDataConsentPruningService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataForwardingService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/UsageDataForwardingService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ConfigFileUpdater\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ConfigFileUpdater.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/GitHubService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/GitHubService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LighthouseReleaseService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsReport\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Logging/RecentProblemsReport.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsSink\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Logging/RecentProblemsSink.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/SerilogLogConfiguration\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SerilogLogConfiguration.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/PostHogUsageDataPublisher\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/PostHogUsageDataPublisher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataConsentService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentToken\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataConsentToken.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataDeploymentModeResolver\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataDeploymentModeResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataEventQueue\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataEventQueue.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataGate\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataGate.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataInstanceProperties\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataInstanceProperties.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataMasterSwitch\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataMasterSwitch.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/IAssemblyService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IAssemblyService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IConfigFileUpdater\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IConfigFileUpdater.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/IGitHubService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IGitHubService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/ILighthouseReleaseService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILighthouseReleaseService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ILogConfiguration\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILogConfiguration.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IRecentProblems\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRecentProblems.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IRecentProblemsReport\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRecentProblemsReport.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataConsentService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataConsentService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataDeploymentModeResolver\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataDeploymentModeResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataEventQueue\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataEventQueue.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataGate\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataGate.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataInstanceProperties\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataInstanceProperties.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataMasterSwitch\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataMasterSwitch.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataPublisher\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataPublisher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentPruningServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/UsageDataConsentPruningServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataForwardingServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/UsageDataForwardingServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/ConfigFileUpdaterTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ConfigFileUpdaterTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceIntegrationTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseReleaseServiceIntegrationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseReleaseServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/RecentProblemsSinkTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Logging/RecentProblemsSinkTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/SerilogLogConfigurationTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SerilogLogConfigurationTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataConsentServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataDeploymentModeResolverTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataDeploymentModeResolverTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataEventQueueTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataEventQueueTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateBudgetTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateBudgetTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateConsentTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateConsentTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateReportingTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateReportingTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataInstancePropertiesTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataInstancePropertiesTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataMistypedCollectorAddressTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataPublishedMessageTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataPublishedMessageTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UsageDataLoggingAndReleases rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UsageDataLoggingAndReleases rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs

Extend

```text
rewrite the UsageDataLoggingAndReleases rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/UsageDataVeto\/Slice03VetoSettingSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LogsControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/UsageDataControllerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/VersionControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BackendTestParallelizationGuardTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UsageDataEmitSeamArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataAskEndpointsTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataCollectorAddressTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataCollectorObservationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataEventCeilingTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataVetoEmitTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/GithubServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataPublishedMessageTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SystemInfoServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/LoggingConfiguratorTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/RecentProblemsCapacityConfigurationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UsageDataController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/SystemInfoService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformUsageLoggingReleasesLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformUsageLoggingReleasesLayoutArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLane"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

#### Public oracle

```text
Run PlatformUsageLoggingReleasesLayoutArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly: take every PlatformLaneMembership.MovingTypes entry of the UsageDataLoggingAndReleases move (areas UsageData, Logging and Releases), look its name up in its new namespace and in its old one, and look each test class the slice moves up in Lighthouse.Backend.Tests.Platform.<Area> and in its old namespace
```

```text
Every UsageDataLoggingAndReleases type resolves to exactly one type in Lighthouse.Backend.Platform.<its area> and none is left in its old namespace (every UsageDataLoggingAndReleases type live in Platform.UsageData, Platform.Logging and Platform.Releases; UsageDataEventName gains no member; the GlobalSuppressions.cs target of LighthouseReleaseService names its new full name; test-selection-show selects the GitHub live suite for a change under Platform/Releases); each moved test class lives in Lighthouse.Backend.Tests.Platform.<Area>; PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest stay green and the existing suite stays green
```

```text
A UsageDataLoggingAndReleases type left in its old namespace, present in both, placed in another area, a listed name matching no type (vacuous list), or a moved test class left in its old namespace: the test fails naming the type. Today every listed type sits in its old namespace, so the test is RED
```

##### Promises

P3

P6

P7

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

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/AssemblyService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/AssemblyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentPruningService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/UsageDataConsentPruningService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataForwardingService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/UsageDataForwardingService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ConfigFileUpdater\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ConfigFileUpdater.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/GitHubService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/GitHubService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LighthouseReleaseService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsReport\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Logging/RecentProblemsReport.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsSink\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Logging/RecentProblemsSink.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/SerilogLogConfiguration\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SerilogLogConfiguration.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/PostHogUsageDataPublisher\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/PostHogUsageDataPublisher.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataConsentService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataConsentToken\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataConsentToken.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataDeploymentModeResolver\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataDeploymentModeResolver.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataEventQueue\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataEventQueue.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataGate\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataGate.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataInstanceProperties\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataInstanceProperties.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataMasterSwitch\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/UsageData/UsageDataMasterSwitch.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/IAssemblyService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IAssemblyService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IConfigFileUpdater\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IConfigFileUpdater.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/IGitHubService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IGitHubService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/ILighthouseReleaseService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILighthouseReleaseService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ILogConfiguration\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILogConfiguration.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IRecentProblems\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRecentProblems.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/IRecentProblemsReport\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRecentProblemsReport.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataConsentService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataConsentService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataDeploymentModeResolver\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataDeploymentModeResolver.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataEventQueue\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataEventQueue.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataGate\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataGate.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataInstanceProperties\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataInstanceProperties.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataMasterSwitch\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataMasterSwitch.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/IUsageDataPublisher\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/UsageData/IUsageDataPublisher.cs; no member changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentPruningServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/UsageDataConsentPruningServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataForwardingServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/UsageDataForwardingServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/ConfigFileUpdaterTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/ConfigFileUpdaterTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceIntegrationTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseReleaseServiceIntegrationTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseReleaseServiceTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/RecentProblemsSinkTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Logging/RecentProblemsSinkTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/SerilogLogConfigurationTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/SerilogLogConfigurationTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataConsentServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataConsentServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataDeploymentModeResolverTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataDeploymentModeResolverTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataEventQueueTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataEventQueueTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateBudgetTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateBudgetTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateConsentTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateConsentTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateReportingTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataGateReportingTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataInstancePropertiesTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataInstancePropertiesTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataMistypedCollectorAddressTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataPublishedMessageTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/UsageData/UsageDataPublishedMessageTests.cs; no test changes
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UsageDataLoggingAndReleases move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UsageDataLoggingAndReleases move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs\:1

Changed

```text
the strings the map lists for the UsageDataLoggingAndReleases move name the new place; no rule loosened
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/UsageDataVeto\/Slice03VetoSettingSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/LogsControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Security\/W1\_VersionEndpointAuthorizationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/UsageDataControllerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/VersionControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BackendTestParallelizationGuardTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UsageDataEmitSeamArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataAskEndpointsTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataCollectorAddressTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataCollectorObservationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataEventCeilingTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/UsageData\/UsageDataVetoEmitTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/GithubServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/UsageData\/UsageDataPublishedMessageTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SystemInfoServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/LoggingConfiguratorTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Startup\/RecentProblemsCapacityConfigurationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UsageDataController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/VersionController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/SystemInfoService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

##### Agreement

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Producer

UnchangedCompatible

```text
the map lists each type by old namespace and finds it in the old or the new one, so it holds before, during and after the move; not edited
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:297

Consumer

UnchangedCompatible

```text
accepts the old or the new text of each renamed string and the old or new namespace of each type
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:472

Consumer

UnchangedCompatible

```text
after the S1 amendment it skips a renamed constant under its OldText or NewText, so this move needs no change to it
```

##### Not applicable

prefactoring

```text
the slice is itself a behaviour-free move; no behaviour change follows it in this feature that a preparatory move would make easier
```

reuse

```text
every target is an existing file moved by git mv or an existing file whose using lines and strings follow the move; no responsibility is created, so each target reuses its own current owner unchanged
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
For each UsageDataLoggingAndReleases entry t of PlatformLaneMembership.MovingTypes: exactly one type named t.Name exists in t.NewNamespace and none in t.OldNamespace; for each test class the slice moves, it exists in Lighthouse.Backend.Tests.Platform.<Area> and not in its old namespace
```

Invariant

Infrastructure

PlatformUsageLoggingReleasesLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
every listed type in its new namespace: pass
```

```text
one listed type left in its old namespace: fails naming it
```

```text
a listed name with no type anywhere: fails naming it
```

```text
a moved test class left in its old namespace: fails naming it
```

```text
the domain is the finite measured map; every entry is checked, so generation adds nothing
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
every UsageDataLoggingAndReleases type of PlatformLaneMembership lives in Lighthouse.Backend.Platform.<Area> and none in its old namespace; the slice's moved test classes live in Lighthouse.Backend.Tests.Platform.<Area>
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformUsageLoggingReleasesLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformUsageLoggingReleasesLayoutArchUnitTest"
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/DomainEventDispatcher\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/DomainEventDispatcher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/FileSystemService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/FileSystemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/LighthouseClock\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LighthouseClock.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/PlatformExtensions\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PlatformExtensions.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/PlatformService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PlatformService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ProcessService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ProcessService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/RandomNumberService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RandomNumberService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ServiceConfig\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ServiceConfig.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/SystemInfoService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SystemInfoService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/IDomainEventDispatcher\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DomainEvents/IDomainEventDispatcher.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/IDomainEventHandler\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DomainEvents/IDomainEventHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IFileSystemService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IFileSystemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ILighthouseClock\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILighthouseClock.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IPlatformService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IPlatformService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IProcessService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IProcessService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IRandomNumberService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRandomNumberService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IServiceConfig\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IServiceConfig.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ISystemInfoService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ISystemInfoService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherGoldTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DomainEventDispatcherGoldTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DomainEventDispatcherTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/LighthouseClockTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseClockTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/SystemInfoServiceTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/SystemInfoServiceTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostPerFixtureGuardTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs

Extend

```text
rewrite the DomainEventsAndHost rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlackoutForecastShiftTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/ForecastWallClockProbe\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/SharedClockBaselineFixture\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/StartForecastWallClockProbe\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/InstanceDayForecastWindowIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice01WriteBackCollectionSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/Slice01StaleSignalSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice03HowLongHasItBeenGoingSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BackendTestParallelizationGuardTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionSaveArchitectureTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UsageDataEmitSeamArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Yardstick\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastDrawCoordinatesTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceCrossTeamDependencyTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceStartDayTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/MultiTeamJointForecastTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/ForecastRunThatCannotEndTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/Bug5023JiraRefreshRotationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshSingleFlightTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateSecretPersistenceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/BlackoutPeriodServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/AppSettingServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/ConfigFileUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Connections\/ConnectionSaveServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DemoData\/DemoDataServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDataRefreshedGoldTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceIntegrationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PlatformExtensionTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/RecurringBlackoutRuleServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/SerilogLogConfigurationTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataDeploymentModeResolverTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateBudgetTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateConsentTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateReportingTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataPublishedMessageTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestClockBoundaryTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/CountingStubOAuthProvider\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/FakeLighthouseClock\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/NotSoRandomNumberService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/SeededRandomNumberService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedDomainEvents\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/Clocks\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/IntegrationTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/FeatureDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/FeatureListDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RunChartDataDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkItemDto\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/InstanceCalendar\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeGapReconciler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceSyncService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuer\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/InProcessUpdateStatusStore\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/RedisUpdateStatusStore\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BlackoutPeriodService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ConfigFileUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Connections\/ConnectionSaveService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/DemoDataService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureRankingService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OptionalFeatures\/FeatureOrderingApplier\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RecurringBlackoutRuleService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementCalendar\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SizingLogCommands\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SleYardstickResolver\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/PortfolioReportMetricSeries\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportBulkCommands\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportQueries\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/TeamReportMetricSeries\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/SerilogLogConfiguration\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/PostHogUsageDataPublisher\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataDeploymentModeResolver\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataGate\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformDomainEventsAndHostLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformDomainEventsAndHostLayoutArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLane"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

#### Public oracle

```text
Run PlatformDomainEventsAndHostLayoutArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly: take every PlatformLaneMembership.MovingTypes entry of the DomainEventsAndHost move (areas DomainEvents and Host), look its name up in its new namespace and in its old one, and look each test class the slice moves up in Lighthouse.Backend.Tests.Platform.<Area> and in its old namespace
```

```text
Every DomainEventsAndHost type resolves to exactly one type in Lighthouse.Backend.Platform.<its area> and none is left in its old namespace (DomainEventDispatcher, IDomainEventDispatcher and IDomainEventHandler`1 live in Platform.DomainEvents and every host facility in Platform.Host; no domain-event handler moved; HostUsersStillToMove names DomainEventDispatcherGoldTest and LighthouseClockTest under Tests.Platform (or the rows are gone after the separate edit commit), CalendarDayAnchorSeamArchUnitTest reads Platform/Host/LighthouseClock.cs and the RandomNumberService.GetRandomNumber suppression names its new full name); each moved test class lives in Lighthouse.Backend.Tests.Platform.<Area>; PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest stay green and the existing suite stays green
```

```text
A DomainEventsAndHost type left in its old namespace, present in both, placed in another area, a listed name matching no type (vacuous list), or a moved test class left in its old namespace: the test fails naming the type. Today every listed type sits in its old namespace, so the test is RED
```

##### Promises

P4

P6

P7

#### Prefactoring

None.

#### Records

##### Decisions

```text
F5-S4 (engineering choice taken in AFK mode, open to the maintainer; the same kind of exception as F2): two feature A support tests take a type that moves in S4 as their example, so they hold before the move and fail after it. PlatformLaneMembershipArchUnitTest.AListedTargetRewrittenToTheNewNamespace_IsStillFound_AndOneTakenOutOfItsFileIsNamed takes SuppressionTargets[0], which S4 rewrites (the RandomNumberService suppression); its removed variant replaces the type's old full name, which the file no longer holds. PlatformLaneRulesSeeTheMoveArchUnitTest.ThePlatformAlternativeCheck_NamesAMadeUpAlternativeThatReachesOutsidePlatform_AndPassesAnAnchoredOne builds its unanchored Platform alternative to match the platform service's full name, which after S4 sits in Platform.Host and no longer matches outside Platform. The S4 acceptance designer amends both supports before the re-craft, changing only the choice of example; every assertion and test stays. For the first, the removed variant replaces whichever full name of the type the file holds, old or new, as F2 did. For the second, the example is a production type that no move of this feature touches and that stays outside Lighthouse.Backend.Platform, chosen from the compiled assembly, not hard-coded to a moving type. PlatformLaneMembership.cs stays unedited. Owner: maintainer. Rejected: editing the map (edits feature A's map and breaks the check before the move); a craft-time exemption (loosens the rule); dropping the tests (loses the law they hold).
```

##### Constraints

None.

##### Reuse

None.

##### Impact

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/DomainEventDispatcher\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/DomainEventDispatcher.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/FileSystemService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/FileSystemService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/LighthouseClock\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LighthouseClock.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/PlatformExtensions\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PlatformExtensions.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/PlatformService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/PlatformService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ProcessService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ProcessService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/RandomNumberService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RandomNumberService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ServiceConfig\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ServiceConfig.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/SystemInfoService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SystemInfoService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/IDomainEventDispatcher\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DomainEvents/IDomainEventDispatcher.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/IDomainEventHandler\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/DomainEvents/IDomainEventHandler.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IFileSystemService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IFileSystemService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ILighthouseClock\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ILighthouseClock.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IPlatformService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IPlatformService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IProcessService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IProcessService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IRandomNumberService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRandomNumberService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/IServiceConfig\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IServiceConfig.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host\/ISystemInfoService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ISystemInfoService.cs; no member changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherGoldTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DomainEventDispatcherGoldTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/DomainEvents/DomainEventDispatcherTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/LighthouseClockTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/LighthouseClockTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Host\/SystemInfoServiceTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/SystemInfoServiceTest.cs; no test changes
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostPerFixtureGuardTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\/GlobalSuppressions\.cs\:1

Changed

```text
the strings the map lists for the DomainEventsAndHost move name the new place; no rule loosened
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveriesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNotesControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryReadStabilityIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryStaleAggregateRaceIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ArchivedDeliveryWriteRefusalIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlackoutForecastShiftTestBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/BlockedItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/ConnectionDefaultsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/NamedDependencyLinkAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryFeatureSelectionScopeIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryMetricsHistoryReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/ForecastWallClockProbe\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/SharedClockBaselineFixture\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FastWithManyFeatures\/FastWithManyFeaturesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastFilterThroughputChartIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckOnAWebHost\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastedStartDates\/StartForecastWallClockProbe\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/InstanceDayForecastWindowIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ManualSorting\/ManualSortingAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/OAuthProviderAbstractionIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/Slice01WriteBackCollectionSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesTestBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RecurringBlackoutRulesWriteBackIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/RunChartPayloadContractIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SleRisk\/SleRiskAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/Slice01StaleSignalSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/StaleWorkItemsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice03HowLongHasItBeenGoingSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamThroughputHistoryValidationIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesPortfolioReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemAgePercentilesReadApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/WorkItemEstimateApiIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/SystemInfoControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WorkTrackingSystemConnectionControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/WriteBack\/WorkTrackingSystemConnectionControllerWriteBackTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BackendTestParallelizationGuardTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionSaveArchitectureTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UsageDataEmitSeamArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Yardstick\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Factories\/DemoDataFactoryTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Access\/Application\/AuthModeResolverTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastDrawCoordinatesTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceCrossTeamDependencyTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceStartDayTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/MultiTeamJointForecastTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/ForecastRunThatCannotEndTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Licensing\/Application\/LicenseServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/Bug5023JiraRefreshRotationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthCredentialConcurrentLoadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthRefreshSingleFlightTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateSecretPersistenceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/BlackoutPeriodServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/AppSettingServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BlackoutForecastShiftWriteBackTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/ConfigFileUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Connections\/ConnectionSaveServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DemoData\/DemoDataServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDataRefreshedGoldTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceIntegrationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Releases\/LighthouseReleaseServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PlatformExtensionTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/RecurringBlackoutRuleServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementCalendarTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/RefinementViewQueryTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SizingLogCommandsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Refinement\/SleYardstickResolverTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportBulkCommandsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/SerilogLogConfigurationTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataDeploymentModeResolverTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateBudgetTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateConsentTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataGateReportingTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataMistypedCollectorAddressTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UsageData\/UsageDataPublishedMessageTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/SurveyNudgeSettingsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestClockBoundaryTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/CountingStubOAuthProvider\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/FakeLighthouseClock\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/NotSoRandomNumberService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/SeededRandomNumberService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/CapturedDomainEvents\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/Clocks\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/IntegrationTestBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/FeatureDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/FeatureListDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RunChartDataDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkItemDto\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveriesController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryRulesController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliverySourcesController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeatureListRows\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/FeaturesController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ForecastController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/LogsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfoliosController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamFeatureSizeMetricsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamsController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoDataFactory\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/InstanceCalendar\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/AuthModeResolver\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeGapReconciler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ProcessBehaviorSnapshotWriter\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastRealityCheckService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/Application\/LicenseService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliveryForecastPublishingService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/DeliverySourceSyncService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/ConnectionHealth\/ConnectionHealthService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthStateTokenIssuer\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/InProcessUpdateStatusStore\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/RedisUpdateStatusStore\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BlackoutPeriodService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/ConfigFileUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Connections\/ConnectionSaveService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DemoData\/DemoDataService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureListActiveWork\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/FeatureRankingService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Releases\/LighthouseReleaseService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/OptionalFeatures\/FeatureOrderingApplier\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RecurringBlackoutRuleService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/RefinementCalendar\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SizingLogCommands\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Refinement\/SleYardstickResolver\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/PortfolioReportMetricSeries\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportBulkCommands\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportQueries\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/TeamReportMetricSeries\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/SerilogLogConfiguration\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/PostHogUsageDataPublisher\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataDeploymentModeResolver\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/UsageDataGate\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackTriggerService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

##### Agreement

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Producer

UnchangedCompatible

```text
the map lists each type by old namespace and finds it in the old or the new one, so it holds before, during and after the move; not edited
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:297

Consumer

UnchangedCompatible

```text
accepts the old or the new text of each renamed string and the old or new namespace of each type
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:472

Consumer

UnchangedCompatible

```text
after the S1 amendment it skips a renamed constant under its OldText or NewText, so this move needs no change to it
```

TestHostUseGuardTest\.HostUsersStillToMove \(test class full names\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:296

Consumer

Migrated

```text
the two rows name DomainEventDispatcherGoldTest and LighthouseClockTest under Tests.Platform in the move commit, or go away after the separate edit commit
```

##### Not applicable

prefactoring

```text
the slice is itself a behaviour-free move; no behaviour change follows it in this feature that a preparatory move would make easier
```

reuse

```text
every target is an existing file moved by git mv or an existing file whose using lines and strings follow the move; no responsibility is created, so each target reuses its own current owner unchanged
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
For each DomainEventsAndHost entry t of PlatformLaneMembership.MovingTypes: exactly one type named t.Name exists in t.NewNamespace and none in t.OldNamespace; for each test class the slice moves, it exists in Lighthouse.Backend.Tests.Platform.<Area> and not in its old namespace
```

Invariant

Infrastructure

PlatformDomainEventsAndHostLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
every listed type in its new namespace: pass
```

```text
one listed type left in its old namespace: fails naming it
```

```text
a listed name with no type anywhere: fails naming it
```

```text
a moved test class left in its old namespace: fails naming it
```

```text
the domain is the finite measured map; every entry is checked, so generation adds nothing
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
every DomainEventsAndHost type of PlatformLaneMembership lives in Lighthouse.Backend.Platform.<Area> and none in its old namespace; the slice's moved test classes live in Lighthouse.Backend.Tests.Platform.<Area>
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformDomainEventsAndHostLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformDomainEventsAndHostLayoutArchUnitTest"
```

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/AdmittedCancellations\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/AdmittedCancellations.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/AdmittedWorkOrdering\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/AdmittedWorkOrdering.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateCancellationNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateCancellationNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateCompletionNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateCompletionNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateExecutionLock\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateExecutionLock.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateStatusStore\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateStatusStore.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/PostgresUpdateExecutionLock\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/PostgresUpdateExecutionLock.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateCancellationNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateCancellationNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateCompletionNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateCompletionNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateStatusStore\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusStore.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateCancellationContext\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateCancellationContext.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateCancellationPolicy\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateCancellationPolicy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateEntityKinds\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateEntityKinds.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateKey\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateKey.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateMoments\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateMoments.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateNotificationHub.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateProgress\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateProgress.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateQueueService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateStatus\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateStatus.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateSubstrate\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateSubstrate.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateTaskNaming\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateTaskNaming.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RefreshRoundSummary\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RefreshRoundSummary.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RefreshRoundSummaryLine\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RefreshRoundSummaryLine.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateCancellationNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateCancellationNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateCompletionNotifier\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateCompletionNotifier.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateExecutionLock\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateExecutionLock.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateQueueService\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateQueueService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateStatusStore\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateStatusStore.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateTaskNaming\.cs

Create

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateTaskNaming.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/AdmittedCancellationsTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/AdmittedCancellationsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/CancelAllWhileWorkIsAdmittedTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/CancelAllWhileWorkIsAdmittedTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/InProcessUpdateAdaptersTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/InProcessUpdateAdaptersTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/InProcessUpdateCancellationNotifierTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/InProcessUpdateCancellationNotifierTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RedisUpdateStatusScriptFreezeTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusScriptFreezeTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RedisUpdateStatusStoreBestEffortMomentsTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusStoreBestEffortMomentsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateMomentsTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateMomentsTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateNotificationHubTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateNotificationHubTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateProgressOrdinalFreezeTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateProgressOrdinalFreezeTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateQueueServiceTests\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateQueueServiceTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateStatusStoreTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateStatusStoreTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RefreshRoundSummaryLineSkippedForecastTest\.cs

Create

```text
git mv in the slice tests refactor commit; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/RefreshRoundSummaryLineSkippedForecastTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UpdateCancellationContextWriterArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/RecentProblemsSinkTest\.cs

Extend

```text
rewrite the UpdateQueue rows of PlatformLaneMembership.StringsTheMoveRenames and SuppressionTargets for this file from old text to new text, plus usings the build needs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice02StopEndsAForecastScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/CapturedUpdateNotifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice01FailedRefreshSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice03HowLongHasItBeenGoingSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04StopARefreshSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueScenarios\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Story6055ActivityNamesTheWorkSpecifications\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/UpdateControllerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ScalabilitySubstrateSeamArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SharedHostsDrainTheUpdateQueueGuardTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UpdateHoldsAreForecastOnlyArchUnitTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/GracefulShutdownServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/SubstrateProbeTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TaskManagerMultiReplicaTests\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/CancelAllWhileWorkIsAdmittedTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/InProcessUpdateAdaptersTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RedisUpdateStatusScriptFreezeTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBaseTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseMaintenanceGateTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseManagementServiceTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackRoundTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/UpdateServiceTestBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WireContractTestTest\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Health\/GracefulShutdownService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseMaintenanceGate\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsReport\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRound\.cs

Extend

```text
using lines and full-name strings follow the moved types, only where the build or a both-ways list needs them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformUpdateQueueLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformUpdateQueueLayoutArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformLane"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
Run PlatformUpdateQueueLayoutArchUnitTest over the compiled Lighthouse.Backend assembly and the test assembly: take every PlatformLaneMembership.MovingTypes entry of the UpdateQueue move (areas UpdateQueue), look its name up in its new namespace and in its old one, and look each test class the slice moves up in Lighthouse.Backend.Tests.Platform.<Area> and in its old namespace; look the six pipeline types of PlatformLaneMembership.TypesThatStay up in their namespaces
```

```text
Every UpdateQueue type resolves to exactly one type in Lighthouse.Backend.Platform.<its area> and none is left in its old namespace (every UpdateQueue mechanics type live in Platform.UpdateQueue; TeamUpdater, PortfolioUpdater, UpdateServiceBase`1, ITeamUpdater, IPortfolioUpdater and IUpdateService are still in Services.Implementation.BackgroundServices.Update and Services.Interfaces.Update; the Forecasting and Work Items list strings and the placement-test paths name the new place); each moved test class lives in Lighthouse.Backend.Tests.Platform.<Area>; PlatformLaneMembershipArchUnitTest and PlatformLaneRulesSeeTheMoveArchUnitTest stay green and the existing suite stays green
```

```text
A UpdateQueue type left in its old namespace, present in both, placed in another area, a listed name matching no type (vacuous list), or a moved test class left in its old namespace: the test fails naming the type; a pipeline type moved into Platform: the test fails naming it. Today every listed type sits in its old namespace, so the test is RED
```

##### Promises

P5

P6

P7

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

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/AdmittedCancellations\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/AdmittedCancellations.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/AdmittedWorkOrdering\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/AdmittedWorkOrdering.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateCancellationNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateCancellationNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateCompletionNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateCompletionNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateExecutionLock\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateExecutionLock.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/InProcessUpdateStatusStore\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/InProcessUpdateStatusStore.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/PostgresUpdateExecutionLock\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/PostgresUpdateExecutionLock.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateCancellationNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateCancellationNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateCompletionNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateCompletionNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RedisUpdateStatusStore\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusStore.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateCancellationContext\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateCancellationContext.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateCancellationPolicy\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateCancellationPolicy.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateEntityKinds\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateEntityKinds.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateKey\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateKey.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateMoments\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateMoments.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateNotificationHub.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateProgress\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateProgress.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateQueueService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateStatus\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateStatus.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateSubstrate\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateSubstrate.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateTaskNaming\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateTaskNaming.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RefreshRoundSummary\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RefreshRoundSummary.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/RefreshRoundSummaryLine\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RefreshRoundSummaryLine.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateCancellationNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateCancellationNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateCompletionNotifier\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateCompletionNotifier.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateExecutionLock\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateExecutionLock.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateQueueService\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateQueueService.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateStatusStore\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateStatusStore.cs; no member changes
```

moved file

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/IUpdateTaskNaming\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Update/IUpdateTaskNaming.cs; no member changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/AdmittedCancellationsTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/AdmittedCancellationsTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/CancelAllWhileWorkIsAdmittedTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/CancelAllWhileWorkIsAdmittedTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/InProcessUpdateAdaptersTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/InProcessUpdateAdaptersTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/InProcessUpdateCancellationNotifierTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/InProcessUpdateCancellationNotifierTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RedisUpdateStatusScriptFreezeTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusScriptFreezeTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RedisUpdateStatusStoreBestEffortMomentsTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/RedisUpdateStatusStoreBestEffortMomentsTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateMomentsTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateMomentsTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateNotificationHubTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateNotificationHubTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateProgressOrdinalFreezeTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateProgressOrdinalFreezeTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateQueueServiceTests\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateQueueServiceTests.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/UpdateStatusStoreTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateStatusStoreTest.cs; no test changes
```

moved test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/UpdateQueue\/RefreshRoundSummaryLineSkippedForecastTest\.cs\:1

Migrated

```text
git mv; namespace line and usings only, from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/RefreshRoundSummaryLineSkippedForecastTest.cs; no test changes
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UpdateCancellationContextWriterArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

rule\, list\, budget or suppression string

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/Logging\/RecentProblemsSinkTest\.cs\:1

Changed

```text
the strings the map lists for the UpdateQueue move name the new place; no rule loosened
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConnectionFieldChange\/ConnectionFieldChangeAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PortfolioDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/QuietWriteBack\/QuietWriteBackAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/ForecastWhileItUpdatesAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice02StopEndsAForecastScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ResponsiveDuringUpdates\/Slice05CancelAllScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/CapturedUpdateNotifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice01FailedRefreshSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice02SeeWhatIsRunningSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice03HowLongHasItBeenGoingSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04CancellationReachProbe\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice04StopARefreshSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice05AnyBrokenCredentialSaysSoSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueScenarios\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice07TheQueueReadsLikeAQueueSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Slice08HealthThatChecksItselfSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/Story6055ActivityNamesTheWorkSpecifications\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TaskManager\/TaskManagerHarness\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamDeleteSerialisationTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/UpdateControllerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ScalabilitySubstrateSeamArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SharedHostsDrainTheUpdateQueueGuardTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UpdateHoldsAreForecastOnlyArchUnitTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Health\/GracefulShutdownServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/CancelAllMultiReplicaTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ScalabilityTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/SubstrateProbeTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TaskManagerMultiReplicaTests\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFillerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/CancelAllWhileWorkIsAdmittedTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/InProcessUpdateAdaptersTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RedisUpdateStatusScriptFreezeTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/RoundWriteBackDuringDatabaseMaintenanceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/Slice01SkippedEntityLogTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdaterTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBaseTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseMaintenanceGateTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DatabaseManagement\/DatabaseManagementServiceTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WriteBackRoundTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/UpdateServiceTestBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WireContractTestTest\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/WorkItemServiceTestBuilder\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/UpdateController\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Health\/GracefulShutdownService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/TeamUpdater\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceBase\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DatabaseManagement\/DatabaseMaintenanceGate\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Logging\/RecentProblemsReport\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackRound\.cs\:1

Changed

```text
using lines and full-name strings only; no logic change
```

##### Agreement

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembership\.cs\:66

Producer

UnchangedCompatible

```text
the map lists each type by old namespace and finds it in the old or the new one, so it holds before, during and after the move; not edited
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:297

Consumer

UnchangedCompatible

```text
accepts the old or the new text of each renamed string and the old or new namespace of each type
```

PlatformLaneMembership \(MovingTypes\, StringsTheMoveRenames\, SuppressionTargets\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:472

Consumer

UnchangedCompatible

```text
after the S1 amendment it skips a renamed constant under its OldText or NewText, so this move needs no change to it
```

##### Not applicable

prefactoring

```text
the slice is itself a behaviour-free move; no behaviour change follows it in this feature that a preparatory move would make easier
```

reuse

```text
every target is an existing file moved by git mv or an existing file whose using lines and strings follow the move; no responsibility is created, so each target reuses its own current owner unchanged
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

###### L5

```text
For each UpdateQueue entry t of PlatformLaneMembership.MovingTypes: exactly one type named t.Name exists in t.NewNamespace and none in t.OldNamespace; for each test class the slice moves, it exists in Lighthouse.Backend.Tests.Platform.<Area> and not in its old namespace; each pipeline type of TypesThatStay exists in its namespace and not in Lighthouse.Backend.Platform
```

Invariant

Infrastructure

PlatformUpdateQueueLayoutArchUnitTest

###### Treatment

Finite

###### Cases

```text
every listed type in its new namespace: pass
```

```text
one listed type left in its old namespace: fails naming it
```

```text
a listed name with no type anywhere: fails naming it
```

```text
a moved test class left in its old namespace: fails naming it
```

```text
the domain is the finite measured map; every entry is checked, so generation adds nothing
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

###### T5

```text
every UpdateQueue type of PlatformLaneMembership lives in Lighthouse.Backend.Platform.<Area> and none in its old namespace; the slice's moved test classes live in Lighthouse.Backend.Tests.Platform.<Area>; the six pipeline types stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformUpdateQueueLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PlatformUpdateQueueLayoutArchUnitTest"
```

##### Contract tests

None.

## ADRs

None.
