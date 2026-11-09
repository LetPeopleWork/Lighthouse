# Architecture brief: epic\-6577\-system\-wide\-access

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented, ports and adapters in the modular monolith. Domain owners: AppliesTo (value object, Models/Authorization) alone answers Reaches(unit); EffectiveAccess (value object, Models/Authorization) owns role resolution, split into the System role and a lazily built unit reach, and answers RoleOn(unit), CanRead, CanWrite and SystemRole; EveryoneSignedInSetting (entity with ConcurrencyToken) owns the switch; SystemWideRule (read model) carries holder, role and applies-to for the Access tab. RbacAdministrationService is the application service: it loads rows, builds these values and asks them; it never compares roles itself.
```

#### D2

```text
Carriers and owners. Decisions: EffectiveAccess and AppliesTo in Models/Authorization. Effects: rows through LighthouseAppContext (mappings, permissions, keys, the switch row), one Information line per rule add, rule remove and switch change through ILogger in AuthorizationController, and the name-only usage-data event SystemWideAccessGranted = 26 through the existing usage-data sender. Errors: RbacOperationResult for refusals (invalid pair, duplicate, not System Admin), DbUpdateConcurrencyException mapped to 409 by ConcurrencyConflictExceptionFilter, and any failed read of rules or the switch surfaces as an error, never a grant. Per-process state: none; no cache, so every request re-reads.
```

#### D3

```text
Cross-layer algebra. Domain: roles SystemAdmin, TeamAdmin, PortfolioAdmin, Viewer and UnitAdmin (new, 4); scopes System, Team(id), Portfolio(id) and AllUnits (new, 3, null ScopeId); operations Reaches, RoleOn, SystemRole, CanRead, CanWrite; observation is the read or write answer for a principal on a unit. Application: Success, Refusal (invalid pair, duplicate equal row, caller not System Admin), Conflict (stale or missing switch token) and Error (failed read). Adapter: EF optimistic concurrency on the switch row; a null ScopeId is not unique in the index on SQLite or PostgreSQL, so twins are representable. Infrastructure: a restore without the switch row reads as off and the idempotent seeder adds it again; enums stored as ints, so a rollback meets unknown values and fails closed; no retry, replay or queue.
```

#### D4

```text
Liveness is not applicable: every operation is one synchronous request that reads rows and answers; nothing propagates in the background, so a removed rule or a switch turned off takes effect on the next request by construction (no cache), and there is no eventual-progress obligation to model.
```

#### D5

```text
Test rules. The Lighthouse test-writing skill wins over nWave own test rules where they differ (nine-run per-test timing, PBT density). Read in full and fresh; last lines quoted: SKILL.md ends with End of SKILL.md; references/backend.md ends with End of backend.md; references/frontend.md ends with End of frontend.md. Backend scenarios drive the application-service port (IRbacAdministrationService built with ServicePortHarness) under Lighthouse.Backend.Tests/Acceptance/SystemWideAccess/, namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess, which the feature mutation config must name; a host only as a wire contract in Tests/Contracts. Frontend tests are Vitest plus RTL at the lowest layer. No nWave scenario tags in test code; comments rare and never citing internal references. Each slice has its own oracle files and never edits an earlier slice oracle; a shared builder lives in a support file owned by the slice that creates it. The slice oracle is the backend port class or the lead frontend test; the slice further test files are listed as its supports.
```

#### D6

```text
Open items taken with the recommended option. R-3: S4 first states the removed-person-with-embed-cookie case; if GREEN on the original source the guard is not widened and the test stays as a pin; only if RED is the CurrentUserProfileService guard widened, and then LastSeenAt for embed viewers is kept. FE5 #6393 stays a separate improvement item. E2E: DoD item 4 asks one thin step per flow in RoleBasedAccessControl.spec.ts through page objects against the Keycloak realm; it is owed at feature end and is not a slice oracle command because it needs Keycloak. The DESIGN copy questions are settled by M-S1 to M-S4.
```

#### D7

```text
Verification output: the host reads runner-derived counts from the dotnet test summary (Passed, Failed, Skipped, and a build or set-up failure as no tests run) and from the Vitest summary (Tests passed and failed, and a failed file import as set-up failure); exit status alone is not a RED. Fast loop inside a slice: pwsh Lighthouse.Backend/Test-Fast.ps1 and pnpm test --changed.
```

#### D8

```text
Code facts: code-fact query.reads-of PermissionScopeKey answered from textsearch at noisy confidence over live source (AST provider did not answer), 22 sites, all inside RbacAdministrationService.cs; it may miss indirect uses, so ADR-280 list of 13 or more readers plus deletion of the type (a missed reader cannot compile) carry the closure. typesafe: unavailable, Jev is unauthorized in ~/.nwave/config.json.
```

#### D10

```text
Authority: the DESIGN of feature-delta.md (DDD-1 to DDD-18, Reuse Analysis, Driving and Driven Ports, Loading and Failure Looks), ADR-280 and ADR-281, with the DELIVER sections Sketch Review (M-S1 to M-S4, binding UI copy), Rigor Profile and Architecture Review R-1 to R-6. Where R-1 to R-6 differ from the earlier DESIGN text they win: the seven readers are 13 or more; the key coverage change and the key-limit tests both live in S1, which now covers P1 and P2 (the product merged the former key-limit slice into S1); DDD-15 is conditional on R-3. All maintainer decisions M1 to M4 are confirmed unchanged.
```

#### D11

```text
Claim-to-checker. EffectiveAccess, AppliesTo, the valid-pairs table and the key intersection are total functions over the closed enums UserRole and PermissionScopeType: totality is checked by the C# compiler (a switch over the enum without a default fails the build under TreatWarningsAsErrors) and the laws by exhaustive NUnit tables over every pair; no formal checker is selected for them because the domain is finite and exhaustively enumerated. The two concurrent write protocols, noLostSwitchWrite (switch PUT under its concurrency token including the no-row upsert) and removalLeavesNoTwin (rule creation racing with removal of all twins), are state-model safety claims checked with Quint (simulation, then quint verify with the TLC backend) on docs/feature/epic-6577-system-wide-access/models/system_wide_access_writes.qnt; the check is bounded to two writers and four accepted writes, and runtime still rests on the S4 stale-write tests on SQLite and PostgreSQL and the S1 twin-removal tests. Agda is not selected (no proof beyond finite tables). TLA+ written by hand is not needed: no liveness or fairness claim exists.
```

#### D12

```text
The temporary maintainer waiver of 2026-11-09 for noLostSwitchWrite and removalLeavesNoTwin is lifted the same day: the Quint model typechecks and both properties pass quint run and exhaustive quint verify with the TLC backend on two writers with at most four accepted switch writes, and each recorded mutant is killed by a quint run counterexample. Receipts under docs/feature/epic-6577-system-wide-access/models/receipts/, re-run with models/run_checks.sh.
```

### Constraints

#### K1

```text
Rules are RbacGroupMapping rows with scope AllUnits and a null ScopeId; no column changes. Enum values are appended only, AllUnits = 3 and UnitAdmin = 4, never renumbered; usage-data SystemWideAccessGranted = 26, appended.
```

#### K2

```text
Valid pairs: System only with SystemAdmin; Team with TeamAdmin or Viewer; Portfolio with PortfolioAdmin or Viewer; AllUnits only with Viewer or UnitAdmin; UnitAdmin only with AllUnits. An API key can never be limited to AllUnits.
```

#### K3

```text
Bounds: the group value keeps the existing mapping request validation, no new bound; the number of rules and of units is unbounded by authority (none stated); NFR-1 bounds cost instead: permission lookups for a list do not grow with the number of units, checked with 200 Teams, my-summary included.
```

#### K4

```text
A rule only adds: the role on a unit is the higher of the unit role and any reach; the explicit-replaces-group merge on unit keys runs first, unchanged. The System role never depends on rules or the switch.
```

#### K5

```text
Fail closed and no cache: a failed read of rules or the switch is an error, never a grant; a change applies on the next request.
```

#### K6

```text
Private by default: the switch is off by default and a missing row is off; it can only grant reading; it applies only to a person with a stable subject and a profile.
```

#### K7

```text
All RBAC logic flows through IRbacAdministrationService; no component fetches my-summary directly; UI gating derives from useRbac(); my-summary keeps its contract.
```

#### K8

```text
UI copy fixed by M-S1 to M-S4; Team and Portfolio render through Terminology. Every new or changed view has its loading look and its failure look; failures say reload, no Retry button.
```

#### K9

```text
One additive migration only, in S4, made with the CreateMigration PowerShell script, expand-only.
```

### Reuse

RbacAdministrationService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:16

Extend

```text
The single inbound port implementation for access; every new read and write goes through it
```

GetVirtualPermissionsAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1146

Reuse

```text
Already returns group mapping rows for the principal, including AllUnits rows once they exist
```

ConcurrencyConflictExceptionFilter

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/Filters\/ConcurrencyConflictExceptionFilter\.cs\:7

Reuse

```text
Maps a stale switch write to 409 as for other configuration
```

AppSetting and OptionalFeature

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/OptionalFeatureSeeder\.cs\:1

CreateNew

```text
Rejected as the switch carrier: no concurrency token and not owned by Access (ADR-281); EveryoneSignedInSetting is created instead
```

### Impact

None.

### Agreement

group\-mappings payload\: scopeType and role enums \(strings out\, ints in\)

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Producer

Migrated

```text
Accepts and returns AllUnits and UnitAdmin
```

group\-mappings payload\: scopeType and role enums \(strings out\, ints in\)

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:1

Consumer

Migrated

```text
Unions gain AllUnits and UnitAdmin in the same commit
```

group\-mappings payload\: scopeType and role enums \(strings out\, ints in\)

\/storage\/repos\/lighthouse\-clients

Consumer

UnchangedCompatible

```text
Clients neither list nor create mappings (DISCUSS house checklist)
```

my\-summary payload

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:418

Producer

UnchangedCompatible

```text
Same fields; adminTeamIds and adminPortfolioIds are fuller for an admin reach
```

my\-summary payload

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:33

Consumer

UnchangedCompatible

```text
Reads the same lists
```

### Not applicable

contract_tests

```text
Every producer and consumer of the changed contracts ships in one build of one deployable (backend plus bundled frontend in the same commit); lighthouse-clients consumes none of them. The JSON names of the new endpoints are pinned by a Lighthouse wire contract inside the slice that adds them.
```

### Boundaries

```text
IRbacAdministrationService, reached through AuthorizationController routes under api/latest and api/v1 and through RbacGuard for every unit route
```

#### Driven ports

```text
LighthouseAppContext on SQLite and PostgreSQL
```

```text
ILogger
```

```text
usage-data event sender
```

```text
ILicenseService enforcement gate, unchanged
```

```text
Controllers and RbacGuard depend on IRbacAdministrationService; RbacAdministrationService depends on Models/Authorization values and LighthouseAppContext; AppliesTo and EffectiveAccess depend on nothing outside Models
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
Rule rows (Viewer, AllUnits, null) and (UnitAdmin, AllUnits, null); existing System, Team and Portfolio pairs
```

RbacAdministrationService

Excluded

```text
SystemAdmin on AllUnits; UnitAdmin on System, Team or Portfolio; an API key on AllUnits; the switch granting more than reading
```

RbacAdministrationService

Unknown

```text
Role or scope ints written by a newer release and read by an older one
```

EffectiveAccess

Admitted

```text
Twin rule rows from concurrent creates
```

RbacAdministrationService

### Models

Quint

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/system\_wide\_access\_writes\.qnt

##### Properties

noLostSwitchWrite

Invariant

```text
No two accepted switch writes were based on the same token
```

EveryoneSignedInSetting\.ConcurrencyToken

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:876

removalLeavesNoTwin

Invariant

```text
Right after a rule removal no row with that group, role and scope remains
```

RbacAdministrationService\.RemoveGroupMappingAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:963

##### Atoms

Action

putSwitch

RbacAdministrationService switch update \(designed\, not built\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:16

```text
One SaveChanges under the token; insert when no row exists
```

Action

removeRule

RbacAdministrationService\.RemoveGroupMappingAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:963

```text
One ExecuteDelete over every row with the same group, role and scope
```

Action

checkRule

RbacAdministrationService\.CreateGroupMappingAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:916

```text
Check and insert are separate statements; the race is modelled
```

Constant

MAX\_WRITES

bound of the checked instance

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/system\_wide\_access\_writes\.qnt\:7

None.

##### Assumptions

```text
The database applies a conditional update or insert atomically (EF concurrency token check, primary key on the one switch row)
```

```text
Two concurrent writers bound the race; more writers add no new interleaving class
```

```text
MAX_WRITES = 4: at most four accepted switch writes, which bounds the token counter so the state space is finite for exhaustive checking
```

##### Mutants

###### N3

```text
putSwitch accepts without comparing the token read with the stored token (models/mutants/mutant_put_ignores_token.qnt, sha256 58de44dd8a45f9dfd7e19a6adb329d0361a0c2036522e95d90f71ebd0c231c7a); killed by quint run, receipt models/receipts/mutant_put_ignores_token.txt
```

###### Kills

noLostSwitchWrite

###### N4

```text
removeRule deletes one row instead of every twin (models/mutants/mutant_remove_deletes_one_row.qnt, sha256 3933163bad2cfa8baedbe4c8884a1d8fb460bdb2137003e213868eeb8d149b4b); killed by quint run, receipt models/receipts/mutant_remove_deletes_one_row.txt
```

###### Kills

removalLeavesNoTwin

### Closure

```text
Switch write under its token and rule removal with twins
```

Safety

Pass

DesignedNotBuilt

```text
quint 0.33.1: noLostSwitchWrite and removalLeavesNoTwin hold under quint run (seed 0x2a6577, 15 steps, 20000 samples) and exhaustive quint verify --backend=tlc (972 distinct states, depth 14) on two writers with MAX_WRITES = 4; each mutant violates its property
```

##### Evidence

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/run\_noLostSwitchWrite\.txt

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/run\_removalLeavesNoTwin\.txt

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/verify\_tlc\_noLostSwitchWrite\.txt

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/verify\_tlc\_removalLeavesNoTwin\.txt

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/mutant\_put\_ignores\_token\.txt

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/receipts\/mutant\_remove\_deletes\_one\_row\.txt

### Risks

#### Y1

Stressor

```text
Assumption: the group claim is present and current
```

```text
Rules give nothing to a person without the group; the switch still works
```

```text
A key snapshot stays stale until its owner signs in again
```

```text
A grant from a missing or malformed claim
```

```text
Stale key snapshot accepted by P11
```

RbacAdministrationService

#### Y2

Stressor

```text
Assumption: reading rules and the switch succeeds
```

```text
System Admin keeps System Admin
```

```text
Unit reads answer an error
```

```text
A grant after a failed read
```

```text
None beyond the error page
```

EffectiveAccess

#### Y3

Stressor

```text
Assumption: the switch row exists
```

```text
Behaviour as switch off
```

```text
Seeder adds the row on next start; PUT upserts
```

```text
Switch read as on
```

```text
None
```

EveryoneSignedInSetting

#### Y4

Stressor

```text
Assumption: the unique index prevents equal rules
```

```text
The rule list shows one line per group, role and scope
```

```text
Twin rows stored by a race
```

```text
A removed rule still reaching through a twin
```

```text
Twin rows until removal; harmless because a rule only adds
```

RbacAdministrationService

#### Y5

Stressor

```text
Assumption: stored enum values are known (negated by a rollback to an older release)
```

```text
Older release ignores AllUnits and UnitAdmin rows
```

```text
Those rows grant nothing on the older release
```

```text
An unknown value read as a grant
```

```text
None
```

EffectiveAccess

#### Y6

SinglePointOfFailure

```text
EffectiveAccess is the one resolver for every read and write answer
```

```text
One rule for 13 or more readers
```

```text
none
```

```text
A wrong resolver granting broadly
```

```text
Covered by exhaustive tables and a rule-only-person scenario per consumer
```

EffectiveAccess

#### Y7

Bypass

```text
Code outside the Access service querying RbacGroupMappings or UserPermissions directly
```

```text
All checks through IRbacAdministrationService
```

```text
none
```

```text
A second reader that ignores rules
```

```text
Architecture test reading compiled IL
```

RbacAdministrationService

#### Y8

Bypass

```text
Deserialization of an out-of-range role or scope int on a group-mappings request
```

```text
Valid pairs only
```

```text
Refusal
```

```text
A stored row with an unknown value
```

```text
Valid-pairs table refuses every pair not listed
```

RbacAdministrationService

#### Y9

Bypass

```text
An embed session of a person removed during the session
```

```text
No reads for a person without a profile
```

```text
none
```

```text
An embed reader keeping the switch reach
```

```text
Program.cs RejectEmbedPrincipalWhoseIdentityIsGone; S4 pins it and widens the guard only if RED
```

CurrentUserProfileService

#### Y10

Stressor

```text
Second stressor walk (walkthrough, not execution): a Team is deleted, Premium lapses, RBAC is turned off, a list of 200 Teams
```

```text
A rule names no unit so nothing dangles; the licence gate refuses first; RBAC off answers before rules; lookups stay constant per list
```

```text
none
```

```text
A rule surviving as a grant without RBAC or Premium
```

```text
NFR-1 query-count test in S1
```

RbacAdministrationService

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/PermissionScopeType\.cs

Extend

```text
Append AllUnits = 3
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/AppliesTo\.cs

Create

```text
Value object answering Reaches(unit)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs

Create

```text
Value object: System role plus lazily built unit reach; replaces the private dictionary
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs

Extend

```text
Builds EffectiveAccess, moves every reader, deletes PermissionScopeKey, valid pairs, removal of all twins, deduped list, key coverage by owner reach, my-summary through Reaches
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
Actor log line on rule add and remove; usage-data event on a Viewer-on-all add
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs

Extend

```text
Append SystemWideAccessGranted = 26
```

docs\/settings\/usagedata\.md

Extend

```text
List the event
```

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts

Extend

```text
AllUnits scope in the unions
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx

Extend

```text
SSO Groups heading, Role choice with Viewer on all, Applies to column, loading and failure looks of the rules table
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs

#### Verification

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Acceptance.SystemWideAccess.ViewerOnAll|FullyQualifiedName~Acceptance.SystemWideAccess.LimitedKeyUnderRule|FullyQualifiedName~EffectiveAccessTest|FullyQualifiedName~AccessTablesSingleReaderArchUnitTest"
pnpm --dir /storage/repos/Lighthouse/Lighthouse.Frontend exec vitest run src/pages/Settings/Rbac/RbacSettingsViewerOnAll.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/SystemWideAccessScenario\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllListCostTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx

#### Public oracle

```text
Through IRbacAdministrationService built with ServicePortHarness: a System Admin creates the mapping team-readers, Viewer, AllUnits; then a person whose only access is the group team-readers asks to read an existing Team, an existing Portfolio and a Team created after the rule, lists Teams, and asks to write one; an outsider asks the same; the member unlimited key asks to read; the member creates a key limited to Viewer on Team Zenith and it asks to read Team Zenith, Team Apollo and a Portfolio; the member asks for a key with TeamAdmin on Team Zenith; the rule is removed and the member and the limited key ask again
```

```text
Member: read allowed on every unit including the later Team, the list holds every Team, write refused; outsider refused; unlimited key reads what the member reads; the limited key reads Team Zenith only; the TeamAdmin key is refused with the existing message; after removal the member and the key are refused on their next request; the rules list shows one line Viewer on all for team-readers
```

```text
An exact-key implementation refuses the member or leaves the limited key reading nothing; a later Team is missing; Viewer on all is allowed to write; the outsider reads; the limited key reads Team Apollo or the Portfolio; the refusal message changes; a removed rule still grants
```

##### Promises

P1

P2

#### Prefactoring

```text
Replace the private Dictionary of PermissionScopeKey to UserRole and the Has*Permission helpers with EffectiveAccess holding today exact-key semantics, and delete PermissionScopeKey, before any rule reach is added; makes adding reach a change in one value instead of 13 or more readers
```

```text
Every existing read, write, summary and key-intersection answer
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Authorization\/RbacAdministrationServiceTest\.cs

##### Verification

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~RbacAdministrationServiceTest|FullyQualifiedName~CreateRightsAcceptanceTest|FullyQualifiedName~RbacGuardAttributeTest"
```

#### Records

##### Decisions

```text
S1 covers P1 and P2 (the former key-limit slice is merged in): LimitedKeyUnderRuleAtThePortTest is S1 own oracle file, written with the Viewer-on-all scenarios and observed RED with them on the original source.
```

```text
The equal-row refusal is new behaviour, not an existing message: today CreateGroupMappingAsync answers Success for an equal row and the update path has no such check. Creating or updating a mapping onto an equal existing row (same group, role, scope and unit) returns a validation failure with the message This group already has this role here. and leaves the rows unchanged. Concurrent creates can still store twins, which removal deletes together. Root decision taken autonomously, logged for the maintainer.
```

##### Constraints

None.

##### Reuse

PermissionScopeType

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/PermissionScopeType\.cs\:5

Extend

```text
One appended value carries the applies-to part; no column
```

AppliesTo

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacGroupMapping\.cs\:1

CreateNew

```text
No existing type answers whether a rule reaches a unit; RbacGroupMapping stays a row
```

IsValidGroupMappingScope

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1289

Extend

```text
Serves create and update; gains the AllUnits pairs
```

GetAuthorizationSummaryAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:418

Extend

```text
Admin lists filled through Reaches over existing unit ids
```

AuthorizationController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Extend

```text
Existing access endpoints accept the new scope
```

UsageDataEventName

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:90

Extend

```text
Next integer appended
```

RbacSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Extend

```text
Existing section; layout of ScopedGroupMappingManager reused
```

GetTeamGroupMappingsAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:882

Reuse

```text
Not reused for rules (rule rows would land beside a Remove button); left unchanged
```

UserAuthorizationSummary

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:23

Extend

```text
Same RBAC unions gain AllUnits
```

Parent sources were saved

docs\/settings\/usagedata\.md\:68

Extend

```text
One table row for the new event
```

AppliesTo

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/AppliesTo\.cs\:1

CreateNew

```text
No existing type answers whether a rule reaches a unit; RbacGroupMapping stays a row
```

EffectiveAccess

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

CreateNew

```text
Replaces the private exact-key dictionary; one owner for the higher-role rule
```

UnitKey

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:115

Replace

```text
The S1 prefactoring deleted PermissionScopeKey; unit keys are now private to EffectiveAccess, so a missed reader cannot compile
```

GetOwnerEffectiveAccessAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1039

Extend

```text
Keeps the explicit-replaces-group merge on unit keys, then resolves EffectiveAccess; S1 adds rule reach
```

CanRead

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:40

Extend

```text
The prefactoring replaced the Has*Permission helpers with CanRead and CanWrite; S1 adds the higher-role rule over reach here
```

LimitedTo

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:60

Extend

```text
Key intersection: coverage asks the owner role on the key unit; the result stays the key exact rows
```

##### Impact

domain enum

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/PermissionScopeType\.cs\:7

Changed

```text
AllUnits appended; stored as int
```

domain value

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/AppliesTo\.cs\:1

Changed

```text
New
```

domain value

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

Changed

```text
New
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1028

Changed

```text
Every reader moves to EffectiveAccess
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Changed

```text
Log line and event
```

usage\-data enum

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:90

Changed

```text
Value 26
```

docs

docs\/settings\/usagedata\.md\:68

Changed

```text
Row added
```

frontend model

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:1

Changed

```text
Unions
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Changed

```text
Role choice and column
```

port interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs\:1

UnchangedCompatible

```text
No new member in this slice
```

test double

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs\:1

UnchangedCompatible

```text
Interface unchanged in this slice
```

existing tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Authorization\/RbacAdministrationServiceTest\.cs\:1

UnchangedCompatible

```text
Behaviour without rules unchanged; it is the prefactoring oracle
```

unit routes and RbacGuard

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacGuardAttribute\.cs\:1

UnchangedCompatible

```text
Calls the same service checks
```

##### Agreement

None.

##### Not applicable

agreement

```text
The changed contracts (group-mappings enums, my-summary) are recorded once in the shared agreement analysis
```

contract_tests

```text
One deployable; see the shared reason
```

##### Boundaries

```text
IRbacAdministrationService (application-service port), reached through AuthorizationController and RbacGuard
```

###### Driven ports

```text
LighthouseAppContext
```

```text
Controllers and RbacGuard -> IRbacAdministrationService -> Models/Authorization values and LighthouseAppContext; frontend through RbacService.ts and useRbac()
```

###### Failures

####### B1

```text
Invalid role and scope pair
```

Refusal

```text
RbacOperationResult failure, no row written
```

####### B2

```text
Creating or updating a mapping onto an equal existing row (same group, role, scope and unit)
```

Refusal

```text
New validation failure with the message: This group already has this role here. Rows unchanged
```

####### B3

```text
Caller is not System Admin
```

Refusal

```text
Refused as today
```

####### B4

```text
Reading rule rows throws
```

Indeterminate

```text
Error answer, never a grant; System Admin unaffected
```

##### Observations

```text
Read or write answer of IRbacAdministrationService for a principal on a Team or Portfolio
```

bool

```text
Whether the reach came from a unit role, a rule or the switch
```

##### Equality

EffectiveAccess

```text
Two values are equal when their SystemRole is equal and RoleOn is equal for every unit
```

##### Constructors

AppliesTo\.AllUnits

static AppliesTo AllUnits

EffectiveAccess\.Resolve

static EffectiveAccess Resolve\(IEnumerable\<PermissionRule\> ownerRows\, IEnumerable\<PermissionRule\> ruleRows\)

##### Laws

###### L1

```text
RoleOn(unit) is the higher of the unit role and the role of every rule whose AppliesTo reaches the unit; Viewer is below TeamAdmin and PortfolioAdmin
```

Algebraic

Domain

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
no row: none
```

```text
Viewer rule only: Viewer on every Team and Portfolio
```

```text
Viewer rule plus TeamAdmin on Team A: TeamAdmin on A, Viewer elsewhere
```

```text
Viewer on Team A plus Viewer rule: Viewer
```

```text
explicit role replacing a group role on a unit key, then a rule: the higher of both
```

```text
Closed enums; every pair enumerated
```

###### Evidence

None.

###### L2

```text
A rule only adds: adding a rule never lowers any read or write answer
```

Invariant

Domain

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
every role on Team and Portfolio with and without a Viewer rule
```

```text
Finite role set
```

###### Evidence

None.

###### L3

```text
SystemRole depends only on the System row; rule rows never change it
```

Invariant

Domain

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
System Admin with and without rules
```

```text
rule-only person: no System role
```

```text
Finite
```

###### Evidence

None.

###### L4

```text
IsValidGroupMappingScope accepts exactly the valid pairs of the shared constraint
```

Invariant

Application

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
every UserRole value times every PermissionScopeType value, with null and non-null ScopeId
```

```text
Enumerated from Enum.GetValues, so a later value fails the table
```

###### Evidence

None.

###### L5

```text
A limited key yields exactly its own rows, each at the lower of the key role and the owner RoleOn for that unit, and nothing when the owner does not reach the unit
```

Invariant

Application

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
owner reaches unit through a rule
```

```text
owner reaches nothing
```

```text
owner Viewer through rule, key asks TeamAdmin: refused
```

```text
Finite
```

###### Evidence

None.

###### L6

```text
Removing a rule deletes every row with the same group, role and scope; the list shows one line per group, role and scope; creating or updating a mapping onto an equal existing row is refused with the new message This group already has this role here. and leaves the rows unchanged
```

Invariant

Application

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
two twin rows then remove: none left
```

```text
two twin rows listed: one line
```

```text
create a mapping equal to an existing row: refused with This group already has this role here. rows unchanged
```

```text
update a mapping role onto an existing equal row: refused with This group already has this role here. rows unchanged
```

```text
Finite
```

###### Evidence

None.

###### L7

```text
Permission lookups for a list do not grow with the number of units, my-summary included
```

Invariant

Infrastructure

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
200 Teams with one rule: query count equals the count for 2 Teams
```

```text
Cost law observed by counting commands on the harness SQLite database
```

###### Evidence

None.

###### L8

```text
A key never exceeds the lower of its limit and its owner role, and never reaches beyond its own rows
```

Invariant

Application

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
limit Viewer Team, owner Viewer on all
```

```text
limit TeamAdmin Team, owner Viewer on all: refused
```

```text
rule removed: key refused next request
```

```text
Finite
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
No reader decides a unit answer from exact unit keys alone
```

```text
A rule-only person is refused on a missed path
```

EffectiveAccess

```text
Owner permission rows and group rule rows
```

EffectiveAccess\.Resolve

The 13 or more read\, write and summary readers in RbacAdministrationService

```text
A rule-only person reads every unit on every consumer
```

```text
Direct queries on UserPermissions or RbacGroupMappings outside the service (architecture test); reflection
```

##### States

None.

##### Models

None.

##### Closure

```text
Viewer on all
```

Safety

Pass

DesignedNotBuilt

```text
Valid-pairs table over closed enums and compiler totality; PermissionScopeKey deleted
```

###### Evidence

None.

```text
Viewer on all
```

Inhabitation

Pass

DesignedNotBuilt

```text
CreateGroupMappingAsync admits (Viewer, AllUnits, null)
```

###### Evidence

None.

```text
Viewer on all
```

PublicReachability

Pass

DesignedNotBuilt

```text
CreateGroupMappingAsync -> RbacGroupMappings -> GetVirtualPermissionsAsync -> EffectiveAccess.Resolve -> read check -> RbacGuard on unit routes; acyclic
```

###### Evidence

None.

```text
Limited key under a rule
```

PublicReachability

Pass

DesignedNotBuilt

```text
Key creation -> IntersectWithApiKeyScope -> read check on the key route
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
Only the Access service reads RbacGroupMappings and UserPermissions, over every production type including types added later, read from compiled IL
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~AccessTablesSingleReaderArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs

Extend

```text
GetTeamSystemWideRulesAsync and GetPortfolioSystemWideRulesAsync
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs

Extend

```text
Unit rules read through Reaches, deduped, System Admin groups left out, no mapping id; access for those who manage the unit members
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/SystemWideRule\.cs

Create

```text
Read model: role and holder (group name)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
Two GET actions with declared access
```

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts

Extend

```text
SystemWideRule type
```

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts

Extend

```text
Two calls
```

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx

Create

```text
Read-only notice, keyed TanStack query, loading, failure and loaded looks per M-S1 and M-S3
```

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManager\.tsx

Extend

```text
Read-only mode: no add form, no Actions column
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx

Extend

```text
Mount notice; pass isSystemAdmin && !isLoading && !loadFailed from useRbac()
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx

Extend

```text
Same as TeamDetail
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx

Extend

```text
No direct role column, chip and filter copy per M-S2
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.test\.tsx

Extend

```text
Migrated at GREEN: its non-System-Admin createGroupMapping assertion pins the old Bug 6576 behaviour
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.test\.tsx

Extend

```text
Migrated at GREEN: its non-System-Admin createGroupMapping assertion pins the old Bug 6576 behaviour
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.test\.tsx

Extend

```text
Migrated at GREEN: it clicks Show unassigned users only, which M-S2 renames
```

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.test\.tsx

#### Verification

```text
pnpm --dir /storage/repos/Lighthouse/Lighthouse.Frontend exec vitest run src/components/Common/Authorization/SystemWideRulesNotice.test.tsx src/components/Common/Authorization/ScopedGroupMappingManagerReadOnly.test.tsx src/pages/Settings/Rbac/RbacSettingsNoDirectRole.test.tsx src/pages/Teams/Detail/TeamDetailAccessTab.test.tsx src/pages/Portfolios/Detail/PortfolioDetailAccessTab.test.tsx
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Acceptance.SystemWideAccess.UnitSystemWideRules|FullyQualifiedName~SystemWideRulesWireContractTest|FullyQualifiedName~SystemWideRulesRefusalWireContractTest|FullyQualifiedName~EveryActionDeclaresItsAccessArchitectureTest"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/SystemWideRulesWireContractTest\.cs

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManagerReadOnly\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsNoDirectRole\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetailAccessTab\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetailAccessTab\.test\.tsx

#### Public oracle

```text
Render the Access tab notice for Team Zenith while the rules call is pending, after it fails, and after it answers one Viewer on all rule for team-readers and nothing else; render ScopedGroupMappingManager read-only; through IRbacAdministrationService a Team Admin of Zenith reads Zenith rules, a Viewer asks the same, and a System Admin group exists
```

```text
Loading: spinner and Checking access to all Teams and Portfolios...; failure: warning Could not check access to all Teams and Portfolios. More people may reach this Team than listed below. Reload the page to try again.; loaded: heading Also reachable through access to all Teams and Portfolios with Viewer and team-readers; no rule: nothing, only after the answer; read-only manager shows the list with no add form and no Actions column; the port answers role and group, no mapping id and no System Admin group; the Viewer is refused; Settings shows No direct role
```

```text
Notice hidden or empty while loading or after a failure; a late answer for another unit lands; System Admin group listed; Viewer allowed; add form shown to a Team Admin; Unassigned still shown
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
New members on IRbacAdministrationService force the existing test double ClaimsDrivenRbacAdministrationService to gain them. That file is no slice oracle or support, so S3 and S4 acceptance designers each add throwing members there without staling an earlier approval.
```

```text
Bug 6576 page wiring is Tested: TeamDetailAccessTab.test.tsx and PortfolioDetailAccessTab.test.tsx render the page as a non-System-Admin unit Admin (notice shown, no add-mapping form) and as a System Admin (form shown). The existing TeamDetail.test.tsx, PortfolioDetail.test.tsx and RbacSettings.test.tsx are S3 targets the crafter migrates in the GREEN commit, because they pin the old form and the Unassigned copy.
```

```text
Where refusal lives: GetTeamSystemWideRulesAsync and GetPortfolioSystemWideRulesAsync take the unit id and no principal, so the refusal is enforced at the controller guard (RbacGuard with the same access as managing that unit members, through CanManageTeamMembershipAsync and its Portfolio twin). The refusal obligation (403 for a Viewer and for an Admin of another unit, on both route versions) is observed on the wire class SystemWideRulesRefusalWireContractTest; port rows that call CanManageTeamMembershipAsync are predicate-guard rows, not the read.
```

```text
An unknown Team or Portfolio id answers 404 Not Found on the unit rules read (recommended option, consistent with the other unit routes), never an empty list that would read as no rules. The guard answers first: a caller without access to manage that unit gets 403 for an existing unit; for an id that does not exist a System Admin gets 404.
```

```text
S3 lists group rules only; the everyone signed in line on the notice and in the unit rules read belongs to S4. The unknown-unit 404 is owned by TeamExistsAsync and PortfolioExistsAsync on IRbacAdministrationService: the controller asks the port whether the unit exists, because the API layer may not reach a repository.
```

##### Constraints

None.

##### Reuse

IRbacAdministrationService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs\:1

Extend

```text
The single inbound port
```

SystemWideRule

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/RbacGroupMappingSummary\.cs\:1

CreateNew

```text
RbacGroupMappingSummary carries a mapping id and scope for administration; the Access tab line must carry neither
```

AuthorizationController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Extend

```text
Access endpoints live here
```

RbacService

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Extend

```text
Existing RBAC client
```

SystemWideRulesNotice

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManager\.tsx\:1

CreateNew

```text
No existing read-only list of rules; reuses CircularProgress and Alert
```

ScopedGroupMappingManager

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManager\.tsx\:1

Extend

```text
Read-only prop, no hook in the child
```

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:33

Reuse

```text
Unchanged; parent derives the prop
```

RbacSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Extend

```text
Copy only
```

GetTeamGroupMappingsAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:882

Reuse

```text
Not reused for the unit rules read, which carries no mapping id
```

TeamDetail

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:90

Extend

```text
Hosts the Access tab
```

PortfolioDetail

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:60

Extend

```text
Hosts the Access tab
```

UserAuthorizationSummary

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:23

Extend

```text
Gains the SystemWideRule type
```

SystemWideRule

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/SystemWideRule\.cs\:1

CreateNew

```text
RbacGroupMappingSummary carries a mapping id and scope for administration; the Access tab line must carry neither
```

SystemWideRulesNotice

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

CreateNew

```text
No existing read-only list of rules; reuses CircularProgress, Alert and a keyed TanStack query
```

creates team\-scoped group mappings on access tab

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.test\.tsx\:904

Extend

```text
Existing page test; its non-System-Admin form assertion moves to the System Admin case
```

creates portfolio\-scoped group mappings on access tab

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.test\.tsx\:676

Extend

```text
Existing page test; its non-System-Admin form assertion moves to the System Admin case
```

Show users with no direct role only

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.test\.tsx\:169

Extend

```text
Existing settings test; its label was Show unassigned users only and becomes Show users with no direct role only
```

##### Impact

port interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs\:1

Changed

```text
Four members: GetTeamSystemWideRulesAsync, GetPortfolioSystemWideRulesAsync, and TeamExistsAsync and PortfolioExistsAsync for the unknown-unit 404
```

test double

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs\:1

Migrated

```text
Gains the two members, throwing
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:882

Changed

```text
New read beside the unit mappings read
```

read model

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/SystemWideRule\.cs\:1

Changed

```text
New
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Changed

```text
Two routes
```

frontend model

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:1

Changed

```text
Type
```

frontend api client

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Changed

```text
Calls
```

frontend component

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

Changed

```text
New
```

frontend component

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManager\.tsx\:1

Changed

```text
Read-only mode
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:1

Changed

```text
Notice and prop
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:1

Changed

```text
Notice and prop
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Changed

```text
Copy
```

existing component test

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManager\.test\.tsx\:1

UnchangedCompatible

```text
Default prop keeps today behaviour
```

existing page test

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.test\.tsx\:904

Migrated

```text
Updated by the crafter in the GREEN commit that changes the wiring
```

existing page test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.test\.tsx\:676

Migrated

```text
Updated by the crafter in the GREEN commit that changes the wiring
```

existing page test

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.test\.tsx\:166

Migrated

```text
Updated by the crafter in the GREEN commit that changes the copy
```

##### Agreement

GET authorization\/teams\/\{teamId\}\/system\-wide\-rules and portfolios twin

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Producer

Migrated

```text
New route; JSON names pinned by the wire contract
```

GET authorization\/teams\/\{teamId\}\/system\-wide\-rules and portfolios twin

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Consumer

Migrated

```text
New call in the same commit
```

##### Not applicable

contract_tests

```text
One deployable; the wire contract support pins route, status and JSON names
```

prefactoring

```text
A new read and component beside existing ones; no preserving move makes them easier (FE5 #6393 stays a separate improvement item)
```

##### Boundaries

```text
IRbacAdministrationService (application-service port), reached through AuthorizationController and RbacGuard
```

###### Driven ports

```text
LighthouseAppContext
```

```text
Controllers and RbacGuard -> IRbacAdministrationService -> Models/Authorization values and LighthouseAppContext; frontend through RbacService.ts and useRbac()
```

###### Failures

####### B5

```text
Caller does not manage the unit members (Viewer, or Admin of another unit)
```

Refusal

```text
403 at the controller guard on api/latest and api/v1, observed by SystemWideRulesRefusalWireContractTest
```

####### B6

```text
Rules read fails
```

Indeterminate

```text
Warning notice per M-S3, never hidden
```

####### B11

```text
Unknown Team or Portfolio id
```

Refusal

```text
404 Not Found, not an empty list
```

##### Observations

```text
Rules listed for a unit
```

IReadOnlyList\<SystemWideRule\>

```text
Twin rows; which people a rule reaches
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L9

```text
In S3 the rules of a unit are the distinct role and holder pairs of every non System Admin group rule whose AppliesTo reaches the unit; S3 covers group rules only, and S4 adds the everyone signed in line when the switch is on
```

Invariant

Application

RbacAdministrationService

###### Treatment

Finite

###### Cases

```text
one Viewer rule
```

```text
twin rows: one line
```

```text
System Admin group: absent
```

```text
no rule: empty
```

```text
Finite
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
Unit rules notice
```

PublicReachability

Pass

DesignedNotBuilt

```text
RbacGroupMappings -> unit rules read -> GET route -> RbacService -> SystemWideRulesNotice on TeamDetail and PortfolioDetail
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Every controller action, including the two new ones, declares its access
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~EveryActionDeclaresItsAccessArchitectureTest
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EveryoneSignedInSetting\.cs

Create

```text
Entity: EveryoneCanView, ConcurrencyToken, IConcurrencyTokenEntity
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/EveryoneSignedInSeeder\.cs

Create

```text
Idempotent ISeeder; missing row is off
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs

Extend

```text
One DbSet, token configured
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Register the seeder
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite

Extend

```text
Additive table, generated by the CreateMigration script
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres

Extend

```text
Additive table, generated by the CreateMigration script
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs

Extend

```text
Viewer reach from the switch for a resolved profile
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs

Extend

```text
Get and set of the switch
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs

Extend

```text
Reads the switch once a profile is resolved; PUT upserts under the token; System role never reads it; unit rules read names everyone signed in
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
GET and PUT everyone-signed-in, actor log line, usage-data event when turned on
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Auth\/CurrentUserProfileService\.cs

Extend

```text
Only if the embed pin is RED on the original source
```

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts

Extend

```text
Switch type
```

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts

Extend

```text
Get and put
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx

Extend

```text
Switch, confirm dialog, loading and failure looks per M-S4
```

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx

Extend

```text
Names everyone signed in
```

docs\/settings\/usagedata\.md

Extend

```text
Event row extended to the switch
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

#### Verification

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Acceptance.SystemWideAccess.EveryoneSignedIn|FullyQualifiedName~Acceptance.SystemWideAccess.RemovedPersonEmbed|FullyQualifiedName~EveryoneSignedInWireContractTest|FullyQualifiedName~ExpandOnlyMigrationGuardTest"
pnpm --dir /storage/repos/Lighthouse/Lighthouse.Frontend exec vitest run src/pages/Settings/Rbac/RbacSettingsEveryoneSignedIn.test.tsx src/components/Common/Authorization/SystemWideRulesNoticeEveryone.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInSeederTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/EveryoneSignedInWireContractTest\.cs

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsEveryoneSignedIn\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNoticeEveryone\.test\.tsx

#### Public oracle

```text
Through IRbacAdministrationService on a fresh harness database after the seeder ran: GET the switch; a person with a profile and no group asks to read a Team and a Portfolio; a System Admin PUTs on with the token read; the person asks again, asks to write, and their key reads; a principal with no profile asks; a second PUT with the old token and one with no token; a non System Admin PUTs; the switch read is made to throw while a System Admin asks to manage RBAC and the person asks to read; the switch is turned off and the person asks again
```

```text
Off with a token by default; refused before; read allowed on every Team and Portfolio after, write refused, key reads; no-profile principal refused; stale token 409, missing token refused, row unchanged; non System Admin refused; with the read throwing System Admin still manages RBAC and the unit read is an error, not a grant; after off the person is refused and real roles are unchanged
```

```text
Switch on by default or on with a missing row; a stale write accepted; the switch grants write; System Admin lost or a unit read granted when the switch read throws; a no-profile principal reads
```

##### Promises

P4

#### Prefactoring

None.

#### Records

##### Decisions

```text
R-3 taken: RemovedPersonEmbedSkeletonTest is written first as a named skeleton on the embed flow (it needs the host cookie validation in Program.cs) and run on the original source; if GREEN, CurrentUserProfileService is not changed and the test stays as a pin; if RED, the guard widens to embed principals and LastSeenAt for embed viewers is kept. The stale-write test runs on SQLite and on PostgreSQL through SharedPostgresContainer because EF InMemory does not enforce the token. Program.cs gains the seeder registration, so the full backend integration suite runs before push.
```

##### Constraints

None.

##### Reuse

EveryoneSignedInSetting

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/IConcurrencyTokenEntity\.cs\:3

CreateNew

```text
No existing record has a token and belongs to Access; implements IConcurrencyTokenEntity
```

ApplyConcurrencyTokenForEdit

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:876

Reuse

```text
Stale edit rule as for other configuration
```

ISeeder

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/OptionalFeatureSeeder\.cs\:1

Reuse

```text
Idempotent start-up rows, also after a restore
```

CurrentUserProfileService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Auth\/CurrentUserProfileService\.cs\:10

Extend

```text
Conditional on R-3
```

RejectEmbedPrincipalWhoseIdentityIsGone

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:414

Reuse

```text
Already rejects an embed principal whose profile is gone
```

RbacSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Extend

```text
Same section
```

IRbacAdministrationService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs\:6

Extend

```text
The single inbound port gains get and set of the switch
```

AuthorizationController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Extend

```text
Access endpoints live here
```

UserAuthorizationSummary

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:23

Extend

```text
Gains the switch type
```

RbacService

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:45

Extend

```text
Existing RBAC client
```

Parent sources were saved

docs\/settings\/usagedata\.md\:68

Extend

```text
Event row extended
```

EveryoneSignedInSetting

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EveryoneSignedInSetting\.cs\:1

CreateNew

```text
No existing record has a token and belongs to Access; implements IConcurrencyTokenEntity
```

EveryoneSignedInSeeder

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/EveryoneSignedInSeeder\.cs\:1

CreateNew

```text
Follows the existing ISeeder pattern; idempotent
```

Sqlite migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\:1

CreateNew

```text
New additive migration files generated by the CreateMigration script
```

Postgres migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\:1

CreateNew

```text
New additive migration files generated by the CreateMigration script
```

EffectiveAccess

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

CreateNew

```text
Created by S1 within this feature; S4 extends it with the switch reach
```

SystemWideRulesNotice

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

CreateNew

```text
Created by S3 within this feature; S4 extends it with the everyone line
```

GetOwnerEffectiveAccessAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1039

Extend

```text
Adds the switch reach after a profile is resolved
```

##### Impact

entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EveryoneSignedInSetting\.cs\:1

Changed

```text
New
```

seeder

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Seeding\/EveryoneSignedInSeeder\.cs\:1

Changed

```text
New
```

persistence context

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:22

Changed

```text
DbSet and token
```

composition root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1492

Changed

```text
Seeder registered
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\:1

Changed

```text
Expand-only
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\:1

Changed

```text
Expand-only
```

domain value

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

Changed

```text
Switch reach
```

port interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Authorization\/IRbacAdministrationService\.cs\:1

Changed

```text
Two members
```

test double

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ClaimsDrivenRbacAdministrationService\.cs\:1

Migrated

```text
Gains the two members, throwing
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1053

Changed

```text
Switch read and upsert
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Changed

```text
Two routes
```

auth service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Auth\/CurrentUserProfileService\.cs\:30

UnchangedCompatible

```text
Unchanged unless the embed pin is RED
```

frontend model

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:1

Changed

```text
Type
```

frontend api client

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Changed

```text
Calls
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Changed

```text
Switch
```

frontend component

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

Changed

```text
Everyone line
```

docs

docs\/settings\/usagedata\.md\:68

Changed

```text
Row extended
```

##### Agreement

GET and PUT authorization\/everyone\-signed\-in \(everyoneCanView\, concurrencyToken\)

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Producer

Migrated

```text
New route; 409 on stale token, refusal on missing token
```

GET and PUT authorization\/everyone\-signed\-in \(everyoneCanView\, concurrencyToken\)

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Consumer

Migrated

```text
New calls in the same commit
```

##### Not applicable

contract_tests

```text
One deployable; the wire contract support pins route, status codes and JSON names
```

prefactoring

```text
New record, seeder and routes; no preserving move eases them
```

##### Boundaries

```text
IRbacAdministrationService (application-service port), reached through AuthorizationController and RbacGuard
```

###### Driven ports

```text
LighthouseAppContext
```

```text
Controllers and RbacGuard -> IRbacAdministrationService -> Models/Authorization values and LighthouseAppContext; frontend through RbacService.ts and useRbac()
```

###### Failures

####### B7

```text
PUT with a stale token
```

Refusal

```text
409 and the existing conflict message; the UI reloads the state
```

####### B8

```text
PUT with no token
```

Refusal

```text
Refused, row unchanged
```

####### B9

```text
Switch read fails
```

Indeterminate

```text
Unit reads error, System Admin unaffected; UI shows Could not load this setting. Reload the page to try again.
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L10

```text
The switch adds Viewer reach only, only for a resolved profile, and an absent row equals off
```

Invariant

Domain

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
on, profile, no group: Viewer on all
```

```text
on, no profile: nothing
```

```text
off: no change
```

```text
absent row: as off
```

```text
on, TeamAdmin on A: TeamAdmin on A
```

```text
Finite
```

###### Evidence

None.

###### L11

```text
A switch write based on a token that is not the stored token is refused and leaves the row unchanged; a write with no row inserts it
```

Invariant

Infrastructure

EveryoneSignedInSetting

###### Treatment

Finite

###### Cases

```text
two writers, same token: one accepted, one 409, on SQLite and PostgreSQL
```

```text
no row, PUT: row inserted
```

```text
Finite; the race itself is the Quint model claim
```

###### Evidence

docs\/feature\/epic\-6577\-system\-wide\-access\/models\/system\_wide\_access\_writes\.qnt

###### L12

```text
The seeder is idempotent: running it twice leaves one row and never changes its value
```

Invariant

Infrastructure

EveryoneSignedInSeeder

###### Treatment

Finite

###### Cases

```text
no row
```

```text
row on
```

```text
row off
```

```text
Finite
```

###### Evidence

None.

###### L13

```text
SystemRole never reads the switch: a failing switch read leaves System Admin true and makes unit reads errors
```

Invariant

Application

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
switch read throws, System Admin manages RBAC
```

```text
switch read throws, person read: error
```

```text
Finite
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C2

```text
The switch can only grant reading
```

```text
Everyone signed in becomes an admin
```

EveryoneSignedInSetting

```text
One boolean EveryoneCanView
```

EveryoneSignedInSeeder

EffectiveAccess\.Resolve

```text
Write refused for a switch-only person
```

```text
Direct row edits in the database; a later field added to the record
```

##### States

None.

##### Models

None.

##### Closure

```text
Everyone signed in
```

Inhabitation

Pass

DesignedNotBuilt

```text
Seeder inserts the off row; PUT turns it on
```

###### Evidence

None.

```text
Everyone signed in
```

PublicReachability

Pass

DesignedNotBuilt

```text
PUT route -> EveryoneSignedInSetting -> GetOwnerEffectivePermissionsAsync -> EffectiveAccess -> read check on unit routes
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T3

```text
Every migration, including the new one, is expand-only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ExpandOnlyMigrationGuardTest\.cs

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ExpandOnlyMigrationGuardTest
```

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/UserRole\.cs

Extend

```text
Append UnitAdmin = 4
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs

Extend

```text
UnitAdmin reads as TeamAdmin on a Team and PortfolioAdmin on a Portfolio; no System role
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs

Extend

```text
Valid pair (UnitAdmin, AllUnits); write-side readers and member management; my-summary admin lists through Reaches
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs

Extend

```text
Usage-data event on an Admin-on-all add
```

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts

Extend

```text
UnitAdmin in the union
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx

Extend

```text
Admin on all in the Role choice
```

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx

Extend

```text
Says Admin
```

docs\/settings\/usagedata\.md

Extend

```text
Event row extended to Admin on all
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllAtThePortTest\.cs

#### Verification

```text
dotnet test /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Acceptance.SystemWideAccess.AdminOnAll
pnpm --dir /storage/repos/Lighthouse/Lighthouse.Frontend exec vitest run src/pages/Settings/Rbac/RbacSettingsAdminOnAll.test.tsx src/components/Common/Authorization/SystemWideRulesNoticeAdmin.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/AdminOnAllSystemOnlyRefusalsTest\.cs

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsAdminOnAll\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNoticeAdmin\.test\.tsx

#### Public oracle

```text
Through IRbacAdministrationService: a System Admin creates team-admins, UnitAdmin, AllUnits; a member whose only access is that group asks to write an existing Team, an existing Portfolio and a Team created later, to manage their members, and reads my-summary; the same member asks to manage RBAC, create, clone or delete a Team or Portfolio, manage connections, change system settings, read licence details and update all Teams; a key limited to Team Zenith under that owner asks to write Zenith and Apollo; the rule is removed and the member asks again; the switch is on and a switch-only person asks to write
```

```text
Writes and member management allowed on every unit including the later one; my-summary adminTeamIds and adminPortfolioIds list every unit; every System-Admin-only action refused; the key writes Zenith only; after removal refused; the switch-only person is refused write
```

```text
UnitAdmin read as System Admin; a write refused; admin lists empty; the key reaches Apollo; the switch yields admin
```

##### Promises

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
The Access tab notice renders the role through getRoleLabel; for an Admin-on-all rule S5 must render Admin: group <value> as sketched, not Team Admin or Portfolio Admin. The S5 oracle pins this line.
```

##### Constraints

None.

##### Reuse

UserRole

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/UserRole\.cs\:5

Extend

```text
One generic role (M2) instead of two
```

CanManageRbacAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:604

Reuse

```text
Reads only the System role, so Admin on all cannot reach System-Admin-only actions
```

GetAuthorizationSummaryAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:418

Extend

```text
Admin lists from existing unit ids through Reaches
```

RbacSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Extend

```text
Same Role choice
```

AuthorizationController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Extend

```text
Usage-data event on an Admin-on-all add
```

UserAuthorizationSummary

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:23

Extend

```text
UnitAdmin in the union
```

Parent sources were saved

docs\/settings\/usagedata\.md\:68

Extend

```text
Event row extended
```

EffectiveAccess

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

CreateNew

```text
Created by S1 within this feature; S5 extends it with UnitAdmin
```

SystemWideRulesNotice

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

CreateNew

```text
Created by S3 within this feature; S5 extends it with Admin wording
```

##### Impact

domain enum

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/UserRole\.cs\:8

Changed

```text
UnitAdmin appended; stored as int
```

domain value

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs\:1

Changed

```text
UnitAdmin resolution
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Authorization\/RbacAdministrationService\.cs\:1289

Changed

```text
Valid pair and write readers
```

controller

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/AuthorizationController\.cs\:15

Changed

```text
Event
```

frontend model

Lighthouse\.Frontend\/src\/models\/Authorization\/RbacModels\.ts\:1

Changed

```text
Union
```

frontend page

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettings\.tsx\:1

Changed

```text
Option
```

frontend component

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.tsx\:1

Changed

```text
Admin wording
```

docs

docs\/settings\/usagedata\.md\:68

Changed

```text
Row extended
```

frontend hooks

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:33

UnchangedCompatible

```text
Fed by fuller my-summary lists
```

member and API\-key writers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ApiKeyController\.cs\:1

UnchangedCompatible

```text
Allow-list their roles; UnitAdmin not admitted as a key or member role
```

##### Agreement

None.

##### Not applicable

agreement

```text
The UnitAdmin enum value and fuller my-summary lists are covered by the shared agreement analysis
```

contract_tests

```text
One deployable
```

prefactoring

```text
S1 prefactoring already gives one resolver; no further move
```

architectural_tests

```text
The access-table reader law is checked in S1 over all types; no new dependency law
```

##### Boundaries

```text
IRbacAdministrationService (application-service port), reached through AuthorizationController and RbacGuard
```

###### Driven ports

```text
LighthouseAppContext
```

```text
Controllers and RbacGuard -> IRbacAdministrationService -> Models/Authorization values and LighthouseAppContext; frontend through RbacService.ts and useRbac()
```

###### Failures

####### B10

```text
Admin on all asks a System-Admin-only action
```

Refusal

```text
Refused as for a TeamAdmin today
```

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L14

```text
UnitAdmin on a unit equals TeamAdmin on a Team and PortfolioAdmin on a Portfolio, and gives no System role
```

Algebraic

Domain

EffectiveAccess

###### Treatment

Finite

###### Cases

```text
UnitAdmin rule, Team
```

```text
UnitAdmin rule, Portfolio
```

```text
UnitAdmin rule, CanManageRbac false
```

```text
UnitAdmin plus Viewer rule: UnitAdmin
```

```text
Finite
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
Admin on all
```

PublicReachability

Pass

DesignedNotBuilt

```text
CreateGroupMappingAsync -> EffectiveAccess UnitAdmin -> write checks and my-summary -> useRbac tabs
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
