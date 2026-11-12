# Architecture brief: story\-6602\-a\-refinement\-at\-the\-port

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Order: Slice10SleYardstickTest (S1), then Slice05NeedAndVerdictTest (S2), then Slice12CommentsTest (S3), then a stop-and-ask checkpoint before any report-window work. Smallest harness change first (Slice10 casts no vote); Slice12 needs the most new wire rows, so it comes last and the checkpoint reads all three classes.
```
Governed slices: S1, S2, S3

#### D2

```text
Each moved class derives from RefinementAtThePortTest over ServicePortHarness and drives the application-service driving ports IRefinementViewQuery and ISizingLogCommands over the real repositories and a database per scenario; no web host. A moved class keeps its namespace, class name and method names, so test ids stay.
```
Governed slices: S1, S2, S3

#### D3

```text
The 7 rows that are about a route stay behind HTTP as wire-contract rows in the existing RefinementWithoutSignInWireContractTest, one partial file per slice (Yardstick, NeedParity, Log). No new host class and no new host build.
```
Governed slices: S1, S2, S3

#### D4

```text
Seeders: keep the IAppSettingService stand-in at the port, no real seeders (maintainer decision 2026-11-08, not reopened). The wire test that compares port fixtures with a seeded database holds the stand-in to it.
```
Governed slices: S1, S2, S3

#### D5

```text
RefinementPortGivens is the only writer of Team set-up: Work Item and finished-work Givens (TheTrackerHolds, TheTeamFinishedWorkEveryDay), the 5-Work-Item Gravity Given and GravitysStates move into it from RefinementAtThePortTest and the wire class. The port base gets ISizingLogCommands (scoped, as on the host) and voter, tab and log readers.
```
Governed slices: S1, S2, S3

#### D6

```text
A moved class keeps its data: Slice10 and Slice12 use a new port Given for the 5-Work-Item Gravity of the vote host (GR-040, GR-058, GR-051, GR-054, GR-073, GR-074), not the 7-item base Given, which adds GR-059.
```
Governed slices: S1, S3

#### D7

```text
Port steps obtain a voter by VoterIdentityResolver.ForWriteAsync(name, key, none) and then call ISizingLogCommands, as RefinementVotesController does; writing the raw key would let ForTeam miss the voter own vote.
```
Governed slices: S2, S3

#### D8

```text
Port rows read RefinementViewDto and the sizing log built as the controller builds them, as NeedOverOneCycleTest does, so the wire text of enums and dates is the DTO text and string assertions stay. New DTO readers go on the port base as protected members; NeedOverOneCycleSpecifications keeps its private readers.
```
Governed slices: S1, S2, S3

#### D9

```text
Guard gaps close in the slice whose class needs them: S1 tightens PortGivensGuardTest and the Team-fields parity rows; S2 makes the settings stand-in strict and compares SignInOff; S3 adds the all-inputs parity case. Every tightened guard ships a planted case that turns it red.
```
Governed slices: S1, S2, S3

#### D10

```text
No dedup of rows that overlap NeedOverOneCycleTest in this feature: ids must survive a behaviour-free move. Dropping them is a separate later slice in feature story-6602-b.
```
Governed slices: S1, S2, S3

#### D11

```text
Today is explicit in every moved Given: TodayIs(2026, 10, 7) unless the row sets another day, because the host clock starts on 7 October 2026 09:00 UTC while the port base starts on TestToday.FrozenNow.
```
Governed slices: S1, S2, S3

#### D12

```text
ServicePortHarness.NewScope() stays public (maintainer, DELIVER start). PortGivensGuardTest, not visibility inside one test assembly, stops a port class from writing a Team itself.
```
Governed slices: S1

#### D13

```text
ScriptedForecasts (port) stamps forecasts from the port clock instead of DateTime.UtcNow. ScriptedForecasts and the host NeedForecastScript are both kept; the host script still serves classes that stay hosted.
```
Governed slices: S2

#### D14

```text
Formal verification: not applicable. No production code, state, transition, protocol or concurrency changes; the change is test structure only. The only laws are already unit-tested rules the rows pin and the id-preservation rule, a finite set equality checked by comparing id lists. Quint, TLA+/TLC and Agda would model nothing new, so no model is written and no checker run or claimed. Liveness is not applicable: no progress or temporal property exists in a synchronous test move. Property-based testing not applicable: no broad input domain; every law is a finite enumeration of named rows, ids and planted cases.
```
Governed slices: S1, S2, S3

#### D15

```text
Carriers, all test-side (no domain class added): per-scenario state lives in the fixture instance of RefinementAtThePortTest (ServicePortHarness, FakeLighthouseClock, ScriptedForecasts, the IAppSettingService stand-in); Team set-up decisions live in RefinementPortGivens; outcomes are RefinementViewDto, the sizing log DTO and SizingOutcome from the production ports; HTTP status and refusal codes are observed only in RefinementWithoutSignInWireContractTest. Layers: application/port rows observe DTO and SizingOutcome; adapter/wire rows observe status, code and JSON text; no infrastructure or recovery layer is touched.
```
Governed slices: S1, S2, S3

### Constraints

#### K1

```text
Behaviour-free: no production code change. Test code only. Existing API, E2E and response tests stay green unmodified, apart from the classes that move.
```
Governed slices: S1, S2, S3

#### K2

```text
Every test id of the three classes survives: 49 rows at the port (10, 17, 22) and 7 route rows in RefinementWithoutSignInWireContractTest (2, 1, 4). The only id differences are the 7 mapped host rows and the new wire rows (up to 9 in total).
```
Governed slices: S1, S2, S3

#### K3

```text
TestHostUseGuardTest.HostUsersStillToMove loses the moved class line in the commit that moves it (S1 Slice10SleYardstickTest, S2 Slice05NeedAndVerdictTest, S3 Slice12CommentsTest); the list never gains a line.
```
Governed slices: S1, S2, S3

#### K4

```text
Moves are separate from edits: a pure refactor(test-infra) move commit and a separate test(refinement) edit commit, as each slice names them.
```
Governed slices: S1, S2, S3

#### K5

```text
One oracle file per slice; no slice edits an earlier slice oracle or support files.
```
Governed slices: S1, S2, S3

#### K6

```text
No nWave scenario tags in code; comments are rare and never cite an internal reference as the explanation.
```
Governed slices: S1, S2, S3

#### K7

```text
Every dotnet test run unsets the seven tracker variables (AzureDevOpsLighthouseIntegrationTestToken, JiraLighthouseIntegrationTestToken, JiraLighthouseRestrictedIntegrationTestToken, JiraScopedTokenIntegrationTestToken, ServiceNowLighthouseIntegrationTestToken, LinearAPIKey, AzureDevOpsLighthouseE2ETestToken) with env -u; the unfiltered suite is never run inside a slice.
```
Governed slices: S1, S2, S3

#### K8

```text
Input bounds: none. No stored or validated input changes; the existing production bounds the rows pin (for example the 2000-character comment limit) are unchanged and only observed.
```
Governed slices: S1, S2, S3

#### K9

```text
Out of scope: ReportWindowBoundaryScenarios and a Reports port base, and dropping rows that repeat NeedOverOneCycleTest (both feature story-6602-b, after the checkpoint). Mutation testing not applicable (maintainer, 2026-11-12): no production code changes; the deliberate-break table proves each moved rule can fail.
```
Governed slices: S1, S2, S3

#### K10

```text
Test-writing guidance: .claude/skills/test-writing/SKILL.md and references/backend.md win where they disagree with nWave timing or PBT density rules. Default test budgets apply; no derogation is requested.
```
Governed slices: S1, S2, S3

### Reuse

RefinementAtThePortTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:34

Extend

```text
Already owns the port database, clock, forecast script and blackout doors; extend with ISizingLogCommands, voter, tab and log steps and DTO readers.
```

RefinementPortGivens

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:22

Extend

```text
The one builder for Team set-up; takes TheTrackerHolds, TheTeamFinishedWorkEveryDay, the 5-item Gravity Given and GravitysStates (two copies today).
```

NeedOverOneCycleTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:23

Reuse

```text
Unchanged; must stay green through every prefactor commit; the existing GREEN oracle for the builder move.
```

PortGivensGuardTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs\:22

Extend

```text
Owns the rule that only the port builder writes Team set-up; extend for the gaps found in the earlier review.
```

RefinementWithoutSignInWireContractTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:22

Extend

```text
Already has one host and per-scenario seeders and the parity rows; extended by partial files per slice for the moved and new wire rows.
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:20

Reuse

```text
Shared by the Reality Check, Refinement and System-wide-access ports; unchanged.
```

PortFixtures

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:341

Extend

```text
Compares registrations of port fixtures with the host; the ISizingLogCommands lifetime is compared for Refinement from S2.
```

SizingVotesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs\:35

Reuse

```text
Host base; unchanged, other classes still derive from it, as do RefinementAcceptanceTest and RefinementNeedAcceptanceTest.
```

HostUsersStillToMove

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:10

Extend

```text
The ratchet list of host users; each slice removes its moved class line, never adds one
```

ScriptedForecasts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:196

Extend

```text
F49
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes: production routes, DTOs and port interfaces are unchanged; the wire rows only observe the existing HTTP contract.
```

contract_tests

```text
No parties change apart: producers (controllers, ports) and consumers (tests) live in one deployable and one commit; the wire-contract rows are part of each slice oracle, not an independent contract obligation.
```

### Boundaries

```text
Application-service ports IRefinementViewQuery and ISizingLogCommands, reached from RefinementAtThePortTest; route rows reach RefinementVotesController and TeamController over HTTP in RefinementWithoutSignInWireContractTest
```

#### Driven ports

```text
Real repositories over a database per scenario
```

```text
IAppSettingService stand-in (strict from S2)
```

```text
FakeLighthouseClock
```

```text
ScriptedForecasts forecast script
```

```text
Tests depend on production ports; production code depends on nothing in tests; port classes set up Teams only through RefinementPortGivens
```

#### Failures

##### B1

```text
A port scenario reads a setting the stand-in was not given
```

Refusal

```text
The strict stand-in throws at the port, so the row fails loud instead of reading a default
```

### Observations

```text
NUnit outcome of each moved row at the port, read from TRX
```

TRX outcome per test id

```text
An HTTP status, refusal code or JSON member text; those are observed only on the wire
```

```text
Sorted test ids before (evidence ids lists) against after
```

Set difference of ids

```text
Whether an assertion inside a kept id was weakened; the deliberate breaks cover that
```

```text
Planted case of each tightened guard turns its guard red
```

Guard row outcome

```text
Writes that no plant shapes
```

### Equality

None.

### Constructors

None.

### Laws

#### L1

```text
Ids after the move equal ids before, except the 7 mapped host rows and the new wire rows
```

Invariant

Application

RefinementAtThePortTest

##### Treatment

Finite

###### Cases

```text
Slice10SleYardstickTest: 12 before; 10 at the port, 2 in the wire class
```

```text
Slice05NeedAndVerdictTest: 18 before; 17 at the port, 1 in the wire class
```

```text
Slice12CommentsTest: 26 before; 22 at the port, 4 in the wire class
```

```text
Finite named id lists
```

##### Evidence

None.

#### L2

```text
Every moved row can fail: a named break in the working tree turns it red
```

Invariant

Application

RefinementAtThePortTest

##### Treatment

Finite

###### Cases

```text
S1 SleYardstickResolver fallback window one day wider
```

```text
S2 RefinementNeedCalculator verdict > for >=
```

```text
S3 SizingLogEntry.LongestComment 2000 to 1999
```

```text
Finite deliberate-break table
```

##### Evidence

None.

#### L3

```text
Only RefinementPortGivens writes Team set-up in a port class
```

Invariant

Application

PortGivensGuardTest

##### Treatment

Finite

###### Cases

```text
set_ on a type reachable from Team
```

```text
Add/Insert/Remove/Clear on a Team collection getter
```

```text
LighthouseAppContext.Set<T> for guarded tables, Add/Update with a guarded argument
```

```text
Finite planted cases
```

##### Evidence

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
Moved Refinement classes at the port
```

Inhabitation

Pass

DesignedNotBuilt

```text
NeedOverOneCycleTest already runs at the same port base, so a port class is constructible; the three moves are not built yet
```

##### Evidence

None.

```text
Moved Refinement classes at the port
```

PublicReachability

Pass

DesignedNotBuilt

```text
Rows call IRefinementViewQuery and ISizingLogCommands, the production driving ports; route rows reach the controllers over HTTP
```

##### Evidence

None.

```text
Moved Refinement classes at the port
```

Safety

Pass

DesignedNotBuilt

```text
Id comparison, planted guard cases and deliberate breaks exclude lost rows and silent writers; executed per slice by the host
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: the port clock equals the host clock
```

```text
Window-edge rows and recordedAt check
```

```text
none
```

```text
A row passing on a shifted window
```

```text
Explicit TodayIs(2026, 10, 7) in every moved Given; edge rows are the proof
```

RefinementAtThePortTest

#### Y2

Stressor

```text
Assumption negated: every host assertion survives at the port
```

```text
Status, code and route comparisons
```

```text
Moving the assertion to a wire row
```

```text
A port row that compares the engine with itself or drops a status/code
```

```text
Route rows stay wire rows; E6/E7 carry status and code
```

RefinementWithoutSignInWireContractTest

#### Y3

Bypass

```text
A port class writes a Team directly through the context or a collection
```

```text
Single writer of Team set-up
```

```text
none
```

```text
A Given that stores what no settings save produces
```

```text
Tightened PortGivensGuardTest plus Team-fields parity rows; reflection-based writes stay outside the guard
```

PortGivensGuardTest

#### Y4

SinglePointOfFailure

```text
The shared wire host grows to at most 23 rows
```

```text
Per-class time budget
```

```text
About 4 s plus one build
```

```text
Exceeding the per-class budget unnoticed
```

```text
Re-read at the checkpoint after S3
```

RefinementWithoutSignInWireContractTest

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs

Extend

```text
Takes Work Item and finished-work Givens, GravitysStates and the 5-item Gravity Given; only writer of Team set-up
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Gives up the moved Givens; yardstick reader for the DTO
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

Extend

```text
Nested setters on types reachable from Team, Team collection mutators, LighthouseAppContext.Set<T>/Add/Update on guarded tables; five more plants
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
Parity rows compare SLE probability/range, states and ThroughputHistory by public-property dump; GravitysStates copy removed
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Remove the Slice10SleYardstickTest line in the move commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickScenarios\.cs

#### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~Slice10SleYardstickTest
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~PortGivensGuardTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest|FullyQualifiedName~RealityCheckWireContractTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Yardstick\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickSpecifications\.cs

#### Public oracle

```text
Run Slice10SleYardstickTest, now deriving from RefinementAtThePortTest, through IRefinementViewQuery with Givens from RefinementPortGivens and TodayIs(2026, 10, 7); run the wire class rows Setting_an_SLE and Clearing_the_SLE moved into RefinementWithoutSignInWireContractTest.Yardstick.cs plus the new yardstick-member and yardstick-source-text wire rows; run the guards
```

```text
10 port rows of Slice10SleYardstickTest pass with unchanged ids; the 2 moved host rows and up to 2 new wire rows pass in the wire class; sorted ids differ from the before list only by the mapped host rows and new wire rows; HostUsersStillToMove no longer names Slice10SleYardstickTest; PortGivensGuardTest and the parity rows pass
```

```text
SleYardstickResolver fallback window one day wider turns Work_Items_finished_the_day_before_the_Throughput_window_opens_do_not_move_the_fallback red; a planted port class calling team.ToDoStates.Add turns the PortGivensGuardTest planted row red; any id lost or renamed fails the id comparison
```

##### Promises

P1

P2

P3

#### Prefactoring

```text
Move TheTrackerHolds, TheTeamFinishedWorkEveryDay and GravitysStates from RefinementAtThePortTest and the wire class into RefinementPortGivens, behaviour-free; makes the yardstick class move and the stricter single-writer guard easier
```

```text
NeedOverOneCycleTest and RefinementWithoutSignInWireContractTest outcomes and ids unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs

##### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~NeedOverOneCycleTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest'
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

test\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:34

Changed

```text
Givens move out; NeedOverOneCycleTest must stay green
```

test\-builder

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:22

Changed

```text
Gains Work Item, finished-work and 5-item Gravity Givens
```

architecture\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs\:22

Changed

```text
Stricter rule and five more plants
```

wire\-contract\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:22

Changed

```text
Parity rows widened; new Yardstick partial
```

consumer\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:23

UnchangedCompatible

```text
Uses the base and builder; file not edited
```

host\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/SizingVotesAcceptanceTest\.cs\:35

UnchangedCompatible

```text
Still serves other hosted classes
```

test\-ratchet\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
Loses the moved class line in the move commit; never gains one
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
Only RefinementPortGivens writes Team set-up in any port class, including nested setters, Team collection mutators and direct context writes; each gap has a plant
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~PortGivensGuardTest
```

###### T2

```text
No class outside HostUsersStillToMove or a named wire-contract reason starts a web host; the moved class leaves the list
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~TestHostUseGuardTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Registers ISizingLogCommands scoped; voter steps via VoterIdentityResolver.ForWriteAsync; strict IAppSettingService stand-in; ScriptedForecasts stamps from the port clock; need readers
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
Port-fixture registration comparison covers the ISizingLogCommands lifetime for Refinement
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
Seeded-database comparison under a strict stand-in; adds SignInOff against the host AuthModeResolver answer on an instance without sign-in
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Remove the Slice05NeedAndVerdictTest line in the move commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs

Extend

```text
Adds the Tuesday cadence and the Backlog-only refinement input (both in EveryInput) and a Team whose Backlog holds many Work Items, which the need-and-verdict class sets up; the builder is the only writer of Team set-up
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictScenarios\.cs

#### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~Slice05NeedAndVerdictTest
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~PortGivensGuardTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest|FullyQualifiedName~RealityCheckWireContractTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.NeedParity\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictSpecifications\.cs

#### Public oracle

```text
Run Slice05NeedAndVerdictTest at the port, casting votes through ISizingLogCommands with voters from VoterIdentityResolver.ForWriteAsync; run the moved row The_range_uses_the_same_horizon_as_the_manual_forecast in RefinementWithoutSignInWireContractTest.NeedParity.cs plus the need-reason text wire row; run the guards
```

```text
17 port rows pass with unchanged ids; the moved row compares the tab range with GET of the manual-forecast route and passes; ids differ only by the mapped and new rows; HostUsersStillToMove no longer names Slice05NeedAndVerdictTest; an unstubbed settings read throws at the port
```

```text
RefinementNeedCalculator verdict comparing > for >= at the range ends turns The_verdict_compares_the_ready_count_with_both_ends_of_the_range red; stand-in GetTeamDataRefreshSettings stubbed to 61 turns the seeded-database comparison red, and a strict stand-in throws on an unstubbed read; horizon 6 in the tab turns the moved wire row red
```

##### Promises

P1

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

None.

##### Impact

test\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:34

Changed

```text
Voting, strict stand-in, clock-stamped forecasts; NeedOverOneCycleTest stays green
```

wire\-contract\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:341

Changed

```text
Compares the new ISizingLogCommands registration lifetime
```

wire\-contract\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:84

Changed

```text
Strict stand-in comparison and SignInOff
```

host\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementNeedAcceptanceTest\.cs\:33

UnchangedCompatible

```text
Still serves other hosted classes
```

test\-ratchet\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
Loses the moved class line in the move commit; never gains one
```

TestSupport

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:23

Changed

```text
Gains RefinesOnTuesdays and RefinesInBacklog (both added to EveryInput) and ATeamWhoseBacklogHolds; existing members unchanged, so S1 port classes and the wire class compile and behave as before; the settings-save parity row now also covers the two new inputs
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
S2 first step is an edit (port-base voting and strict stand-in), not a behaviour-preserving move; the builder move happened in S1
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

###### T3

```text
No class outside HostUsersStillToMove or a named wire-contract reason starts a web host; Slice05NeedAndVerdictTest leaves the list
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~TestHostUseGuardTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs

Extend

```text
Tab and sizing-log readers built as the controller builds them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

Extend

```text
All-inputs parity case: every port builder input together against a settings save
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Remove the Slice12CommentsTest line in the move commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs

Extend

```text
Adds a Gravity Given with one more Backlog Work Item whose reference holds a slash (REQ/12, REQ/12#3), which the two slash-reference log rows moved to the wire class set up; the wire class has no SeedWorkItem and the builder is the only writer of Team set-up
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsScenarios\.cs

#### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~Slice12CommentsTest
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~PortGivensGuardTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest|FullyQualifiedName~RealityCheckWireContractTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsSpecifications\.cs

#### Public oracle

```text
Run Slice12CommentsTest at the port through ISizingLogCommands and the log reader; run the 4 moved rows (log 404, 2 slash references, question without a name) and the new wire rows for recordedAt format, isOpenQuestion and log members, 409 work-item-not-in-refinement, 400 comment-required and comment-too-long, and the answer row in RefinementWithoutSignInWireContractTest.Log.cs; run the guards; measure the three classes and the wire class as the earlier timing run did
```

```text
22 port rows pass with unchanged ids; 4 moved and the new wire rows pass; ids differ only by the mapped and new rows; HostUsersStillToMove no longer names Slice12CommentsTest; the summed time of the three classes after the move is lower than the 19.06 s before
```

```text
SizingLogEntry.LongestComment 2000 to 1999 turns A_comment_of_two_thousand_characters_is_kept_whole red at the port and the comment-too-long wire row red; [JsonIgnore] on SizingLogLine.IsOpenQuestion turns the isOpenQuestion wire row red; a summed time not lower than before fails P4
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

None.

##### Impact

test\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:34

Changed

```text
Tab and log readers
```

wire\-contract\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs\:62

Changed

```text
All-inputs case
```

test\-ratchet\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
Loses the moved class line in the move commit; never gains one
```

TestSupport

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:23

Changed

```text
Gains a Gravity Given with one extra Backlog Work Item whose reference holds a slash, for the two slash-reference log rows in RefinementWithoutSignInWireContractTest.Log.cs; existing members unchanged
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
S3 first step is an edit (readers and new wire rows), not a behaviour-preserving move
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

###### T4

```text
No class outside HostUsersStillToMove or a named wire-contract reason starts a web host; Slice12CommentsTest leaves the list
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~TestHostUseGuardTest
```

##### Contract tests

None.

## ADRs

None.
