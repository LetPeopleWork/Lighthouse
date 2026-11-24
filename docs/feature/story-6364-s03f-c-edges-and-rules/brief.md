# Product brief: story\-6364\-s03f\-c\-edges\-and\-rules

## Request

```text
ADO Story #6364 S03f Platform composition root, feature C (edges and rules): the platform lane gets its own rules measured from the IL, R-PL1 to R-PL4, with falsifiers per rule and list position (C1); every platform unit test sits under Tests/Platform, no platform file is left in Services/{Interfaces,Implementation}, and the files that stay are named per file with owner (C2). Builds on feature A's membership map and preservation law and feature B's moves (all pushed). Behaviour unchanged. DESIGN and DEVOPS: docs/feature/story-6364-s03f-platform-composition-root/feature-delta.md.
```

## Work

M

```text
The platform lane is ruled like a module: its direction below the contexts is written down and checked, and nothing platform-like is left in the old Services folders
```

### Rework

None.

## Promises

### P1

Outcome

```text
R-PL1, read from the compiled IL: every type that feature A's membership map (PlatformLaneMembership.MovingTypes) lists lives in its Platform/<Area> namespace (Encryption, DatabaseManagement, DemoData, Seeding, Persistence, UsageData, Logging, Releases, DomainEvents, Host, UpdateQueue), namespace equal to folder, and no listed type is left under Lighthouse.Backend.Services; a made-up listed type placed under Services, or in another area than the map names, makes the rule fail and the failure names the type and its expected area.
```

### P2

Outcome

```text
R-PL2 and R-PL3, read from the compiled IL with an empty baseline: no type under Lighthouse.Backend.Platform reaches an API type, and none reaches a module's Adapters; a made-up platform type that reaches an API type, or one that reaches a module adapter, makes the matching rule fail and the failure names the edge in the platform lane's own wording (not a module name).
```

### P3

Outcome

```text
R-PL4, read from the compiled IL: the platform lane reaches code still in Services (update pipelines, write-back, Organisation set-up files, repositories, Reports, Refinement) only through one inline list of edges by full type name, measured in C1 and compared both ways, never loosened; the design expects at least the repository edges of UsageDataGate, UsageDataMasterSwitch, UsageDataConsentService, UsageDataConsentPruningService, DemoDataService and UpdateTaskNaming, and UpdateQueueService to the update pipelines and IAppSettingService. A made-up new edge into Services that is not on the list fails, and a listed edge that no longer exists fails, for every list position (the ModuleRulesCatchANewOffender falsifier shape).
```

### P4

Outcome

```text
Every unit test of a platform type sits under Lighthouse.Backend.Tests/Platform/<Area> (namespace equal to folder); a made-up test file of a platform type placed in an old test folder (Tests/Services/...) is caught by the placement rule. The tests that stay by design (update-pipeline tests, the three context gold tests, seeder and repository tests, TestData/*.zip host fixtures) are not flagged.
```

### P5

Outcome

```text
No platform file is left in Services/Interfaces or Services/Implementation: every production file that stays there is named per file, each with its owner (for example the repositories and their base types, the update pipelines TeamUpdater, PortfolioUpdater and UpdateServiceBase<T> with their interfaces, the context seeders, the Organisation set-up files such as AppSettingService); a made-up new file in one of those folders that is not on the list makes the rule fail, and a listed file that no longer exists fails too.
```

### P6

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: feature C edits only test-side rule files (no production file, no Program.cs, no configuration key); every existing architecture rule keeps its verdicts and no rule or list is loosened, including the lane's edges into modules (DatabaseMaintenanceGate to Flow metrics, UpdateNotificationHub to Work Items, usage data to Access and Licensing), which stay governed by those modules' inbound rules and are not listed a second time; the full filtered backend suite stays green and dotnet build has zero warnings.
```

## Scope

```text
The platform lane's own rules R-PL1 to R-PL4 in a new Architecture/PlatformLaneBoundariesArchUnitTest (layout and membership from feature A's map; no API; no module Adapters; edges into Services only through a list measured from the IL), with a falsifier per rule and per list position (C1); the platform unit-test placement rule and the per-file, per-owner list of production files that stay in Services/Interfaces and Services/Implementation in a new Architecture/PlatformLaneUnitTestPlacementTest (C2); renaming WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp in C1 only if its name misleads after the move. Test-side rule files only.
```

```text
Any production, Program.cs, configuration, schema or behaviour change; any further move of types (feature B, delivered) and the membership map, preservation law, GitHub classifier and platform-answers-compare (feature A, delivered); listing the lane's edges into modules a second time (they stay under the modules' inbound rules) and changing other modules' lists, including B's F6-S5 ForecastUpdater edges; inverting DatabaseMaintenanceGate -> Flow metrics (improvement item, not filed); legalising the dispatcher contracts and the clock by namespace (own Story); moving Data, the repositories, the update pipelines, the context seeders or the host band; mutation, ADR-270, ARCHITECTURE.md section 4 and the finalize checklist (after feature C, not nwdes slices). Nothing a user sees changes, so no UI sketch, no loading or failure look and no usage-data event.
```

## Decisions

```text
The platform lane is infrastructure below the contexts, not a module: it stays under every rule it is under today (ApplicationCore and the kernel rules widened in feature A) and gets one direction rule set of its own, R-PL1 to R-PL4 (D-8 option a). Rejected: a composition-root exemption (loosens rules that see these types today) and no rule of its own (new platform code could reach any context unseen). AFK default, maintainer not yet asked.
```

#### Status

Proposed


```text
The lane's edges into modules (DatabaseMaintenanceGate -> Flow metrics, UpdateNotificationHub -> Work Items, usage data -> Access and Licensing) stay governed by those modules' inbound rules, renamed, not listed twice; Models -> Platform is forbidden by the widened kernel rule (A2), not a new rule. DatabaseMaintenanceGate -> Flow metrics stays listed; its inversion is a catalogue note only, no ADO item filed (D-15, open question 7 AFK default).
```

#### Status

Proposed


```text
The R-PL4 list is measured from the IL in C1 (the DESIGN baseline is a text search only); every list is inline, by full type name, compared both ways and never loosened, and its falsifiers reuse the ModuleRules OutboundEdges and ModuleRulesCatchANewOffender shape with a lane wording instead of a ModuleWording module name. C2 reuses the PromisesUnitTestPlacementTest shape with named exceptions.
```

#### Status

Proposed


```text
Each slice's RED: C1 fails on a made-up offender per rule and per list position; C2 is structural, failing on a made-up file in an old folder. Behaviour is proved by the unchanged tests and EXAMINE through admitted probes (D-13).
```

#### Status

Proposed


```text
One commit per slice; C1 and C2 are pushed together after the whole-candidate review. Rigor thorough for feature C: product owner, architect, acceptance designer and crafter opus, reviewers and EXAMINE sonnet (open question 8 AFK default).
```

#### Status

Proposed


## Journey

None.

## Slices

### S1

```text
C1: the platform lane has its own rules, measured from the IL: R-PL1 (each listed type in its Platform/<Area>, none under Services), R-PL2 (no API), R-PL3 (no module Adapters) and R-PL4 (edges into code still in Services only through a measured list), each caught failing on a made-up offender per rule and per list position.
```

#### Promises

P1

P2

P3

P6

#### Depends on

None.

#### Slice work

M

```text
The platform lane's direction below the contexts is written down and checked, so new platform code cannot reach the API, a module adapter or unlisted Services code unseen
```

##### Rework

None.

### S2

```text
C2: every platform unit test sits under Tests/Platform/<Area>, no platform file is left in Services/Interfaces or Services/Implementation, and the production files that stay there are named per file with their owner; a made-up file in an old folder is caught.
```

#### Promises

P4

P5

P6

#### Depends on

S1

#### Slice work

S

```text
Nothing platform-like can be left in, or later added to, the old Services folders unseen, and every file that stays has a named owner
```

##### Rework

None.
