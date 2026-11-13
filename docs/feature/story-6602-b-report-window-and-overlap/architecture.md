# Architecture brief: story\-6602\-b\-report\-window\-and\-overlap

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
S1 creates a new Reports port base, ReportsAtThePortTest, under API/Integration/Reports, over ServicePortHarness: no candidate owns IReportQueries or IReportCommands at the port (ServicePortHarness users are the Reality Check, Refinement and System-wide-access bases only). It registers the Reports services as Program.RegisterReports does (the 17 registrations) plus what they read: the team and portfolio repositories, the team metrics service, a TimeProvider and an ILicenseService stand-in that answers no premium licence, as the host fixture does. The clock is a FakeLighthouseClock set to nine in the morning of Friday 2 Oct 2026 in Europe/Zurich, the host fixture instant, because the port harness starts on TestToday.FrozenNow. It exposes WhatItRegisters for the port-fixture lifetime check.
```
Governed slices: S1

#### D2

```text
ReportWindowBoundaryScenarios moves to ReportsAtThePortTest keeping its namespace (Lighthouse.Backend.Tests.API.Integration.Reports), class name, method name and TestCaseSource names, so its 15 test ids stay. It drives IReportQueries.Templates and IReportCommands.Create with ReportOwner.Team and a Models.Reports.NewReport. It keeps TheTable, BoundaryTableFile, BoundaryBounds and BoundaryRow internal or public as now, because ReportWindowPolicyTest reads them.
```
Governed slices: S1

#### D3

```text
Port observation of each window row: ReportTemplatesRead.Today and EarliestStart written as yyyy-MM-dd equal the table bounds (EarliestStart null for a Team keeping every finished Work Item), MinimumLengthDays equals 14; a row without a refusal answers ReportOutcomeStatus.Done with a report; a row with a refusal answers ReportOutcomeStatus.Refused with Code equal to the table refusal. HTTP 400 and the JSON text are not observable at the port and need no new wire row: today, earliestStart and minimumLengthDays on the wire are pinned by the hosted Slice01TeamReportScenarios and CreateForSeveralTeamsScenarios, a null earliestStart by Slice02ThenInThePastScenarios, and the 400 with each of the seven window refusal codes by ThenItIsRefusedFor in Slice01 (then-too-long, now-too-long), Slice02 (then-too-short, then-ends-in-the-future, then-before-cutoff) and Slice07NowLengthScenarios (now-too-short, now-before-cutoff), all of which stay on the host. The role check of the POST stays pinned by the hosted classes; the port has none.
```
Governed slices: S1

#### D4

```text
No builder and no PortGivensGuardTest-like guard for the Reports port base: its only Team set-up is one ATeam step in the base itself, used by one class, so no builder rule exists to guard. A shared Reports builder is built when a second Reports class moves to the port.
```
Governed slices: S1

#### D5

```text
The repository-root lookup that anchors the shared table (Lighthouse.sln, then its parent) is needed at the port. ReportsAcceptanceTest.RepositoryRoot stays unchanged and is not reachable from the port base, so ReportsAtThePortTest or the moved class owns its own lookup. This repeats a lookup that already exists in several test classes; folding them is outside this Story.
```
Governed slices: S1

#### D6

```text
Slice05NeedAndVerdictTest rows dropped, each mapped to the NeedOverOneCycleTest row with the same Givens in the same order and an assertion superset: (1) Below_range_says_how_many_are_ready_against_the_range_the_Team_is_likely_to_pull maps to The_need_covers_the_working_days_from_the_next_Refinement_to_the_one_after (Gravity with GR-058 and GR-059 tagged ready under the Ready rule, one finished a day, refines on Thursdays, today 2 Oct 2026, the 3/5/6/8/10 script over 7 working days; the twin asserts the same need facts Below, 5, 8, 50, 85, 7, next Refinement 8 Oct not today and 2 ready, plus the cycle 8 to 15 Oct). (2) Too_little_Throughput_history_gives_the_familiar_guard_instead_of_a_number maps to Without_a_number_the_need_names_no_cycle(ThroughputHistory,InsufficientData) (today 7 Oct, Gravity with GR-058 tagged ready, 3 finished a day on 4 days, refines on Thursdays; the twin asserts InsufficientData and no verdict, low or high, plus no cycle). (3) A_Team_nobody_set_up_has_no_number_because_it_has_no_refinement_states maps to Without_a_number_the_need_names_no_cycle(RefinementStates,NoRefinementStates) (today 7 Oct, Team Zenith with Backlog, Implementation, Done and 30 days of history; the twin asserts NoRefinementStates and no verdict, low or high, plus no cycle). Slice05NeedAndVerdictTest goes from 17 rows to 14.
```
Governed slices: S2

#### D7

```text
Slice05NeedAndVerdictTest rows kept because no NeedOverOneCycleTest row has the same inputs: On_a_Refinement_day_the_number_is_for_the_following_Refinement (the twin On_a_Refinement_day_the_cycle_runs_from_today_to_the_next_Refinement has the same Team, cadence, today 8 Oct and 7-day horizon, but a different scripted forecast, 5 to 8 against 6 to 9); Blackout_days_inside_the_cycle_are_not_counted (two blackout days 13 and 14 Oct, horizon 5; no twin blacks out those days); Without_a_cadence_there_is_no_number_and_the_list_stays (two Work Items tagged ready against one in the twin, and it also asserts the six Work Items are still listed); the four verdict rows, the two vote rows, the three high-end rows, the range of nothing and the guardrail, which NeedOverOneCycleTest does not cover.
```
Governed slices: S2

#### D8

```text
Hoist, one owner per member: Team set-up goes to RefinementPortGivens (the Gravity ready-rule builder GravityWithItsReadyRule and its ready tag), because the builder is the only writer of Team set-up; everything else goes to RefinementAtThePortTest as protected members: the need-text constants (Below, NoCadence, InsufficientData, NoRefinementStates and, if still used, In and Above), GravitysCycleWorkingDays, the 3/5/6/8/10 five-to-eight script, the identical Gravity-two-ready-refining-on-Thursdays-on-Friday-2-October Given, and the coach's tab reader. NeedOverOneCycleSpecifications drops NeedIn and NeedReading and uses the base TheNeedOn and NeedFacts; CycleIn and CycleReading stay private to it, because only it reads the cycle. Steps that the three dropped rows alone used are deleted. The base TheTabAsSeenBy gains an overload taking a browser key, and Slice10SleYardstickSpecifications drops TheTabAsReadBy and calls it. The comment above GivenTodayIsWednesdayTheSeventhOfOctober in Slice10SleYardstickSpecifications (and the same sentence in Slice05NeedAndVerdictSpecifications, if it survives the hoist) is reworded so it no longer narrates the old host design.
```
Governed slices: S2

#### D9

```text
S2 commit order: first refactor(test-infra) hoisting the duplicated members, with every test id of Slice05NeedAndVerdictTest, NeedOverOneCycleTest and Slice10SleYardstickTest unchanged; then test(refinement) dropping the three mapped rows and the steps only they used. A scenario file may change only the names of the steps it calls, never a test name.
```
Governed slices: S2

#### D10

```text
Carriers, all test-side, no domain class added: per-scenario state lives in the fixture instance of the port base (ServicePortHarness, FakeLighthouseClock, the licence stand-in for Reports; the RefinementAtThePortTest members as in feature story-6602-a for Refinement); Team set-up decisions live in the base ATeam step for Reports and in RefinementPortGivens for Refinement; outcomes are ReportTemplatesRead and ReportOutcome<ReportRead> from the Reports ports and RefinementViewDto from IRefinementViewQuery. Layers: application/port rows observe those records; HTTP status and JSON text are observed only by the hosted Reports classes and RefinementWithoutSignInWireContractTest, which do not change; no infrastructure or recovery layer is touched.
```
Governed slices: S1, S2

#### D11

```text
Formal verification not applicable: no production code, state, transition, protocol or concurrency changes; the change is test structure only. The laws are finite set equalities of test ids, finite twin mappings and finite deliberate breaks, checked by running code. Quint, TLA+/TLC and Agda would model nothing new, so no model is written and no checker run or claimed. Liveness not applicable: no progress or temporal property exists in a synchronous test move. Property-based testing not applicable: no broad input domain, every row is a named finite case read from a table or written out. Code facts: code-fact answered only through its textsearch provider (noisy) and missed TheTabAsReadBy, so every locator was confirmed by reading the file. Jev: unavailable (unauthorized in this project).
```
Governed slices: S1, S2

#### D12

```text
No ADR change. ADR-237 (test time budget guard) names no port base, so the second port base needs no line there; ADR-270 gains no concept, context or edge (Reports and Refinement contexts only).
```
Governed slices: S1, S2

### Constraints

#### K1

```text
Behaviour-free: no production code change, test code only. Every other API, E2E and response test stays green unmodified.
```
Governed slices: S1, S2

#### K2

```text
Moves are separate from edits: a refactor(test-infra) commit holds only a move or a hoist that changes no test id, and a separate test(...) commit holds an edit. Dropping rows is an edit and is never folded into a move.
```
Governed slices: S1, S2

#### K3

```text
One oracle file per slice, and S1 and S2 share no oracle or support file. S2 edits files that were feature story-6602-a oracles (Slice05NeedAndVerdictSpecifications, NeedOverOneCycleSpecifications, Slice10SleYardstickSpecifications, RefinementAtThePortTest); feature story-6602-a is closed, so that is allowed.
```
Governed slices: S1, S2

#### K4

```text
Every dotnet test run unsets the seven tracker variables with env -u: AzureDevOpsLighthouseIntegrationTestToken, JiraLighthouseIntegrationTestToken, JiraLighthouseRestrictedIntegrationTestToken, JiraScopedTokenIntegrationTestToken, ServiceNowLighthouseIntegrationTestToken, LinearAPIKey, AzureDevOpsLighthouseE2ETestToken. The unfiltered suite never runs inside a slice. Fast loop: pwsh Lighthouse.Backend/Test-Fast.ps1 plus the slice oracle class by --filter FullyQualifiedName~<Class>.
```
Governed slices: S1, S2

#### K5

```text
No nWave scenario tags in code. Comments are rare, in plain language, and never cite an internal reference (a decision id, a finding id, an ADR number) as the explanation.
```
Governed slices: S1, S2

#### K6

```text
New backend acceptance scenarios drive the application-service port. TestHostUseGuardTest.HostUsersStillToMove loses ReportWindowBoundaryScenarios in the S1 move commit and never gains a line.
```
Governed slices: S1, S2

#### K7

```text
Input bounds: none change. No stored or validated input changes; the window bounds the rows pin (shortest window 14 days, a Team keeping finished Work Items for 365 days or for ever, today 2 Oct 2026) are production values that are only observed.
```
Governed slices: S1, S2

#### K8

```text
Test-writing guidance: .claude/skills/test-writing/SKILL.md and references/backend.md win where they disagree with nWave timing or PBT density rules. Default test budgets apply; no derogation is requested.
```
Governed slices: S1, S2

#### K9

```text
Out of scope: moving any other Reports or Refinement class off the host; folding the host NeedForecastScript into ScriptedForecasts; changing ReportsAcceptanceTest or the host Refinement bases; any ADR change. Mutation testing not applicable: no production file changes; the deliberate breaks prove each kept row can fail.
```
Governed slices: S1, S2

### Reuse

ReportsAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:35

Reuse

```text
Host base, unchanged: 17 other Reports classes still derive from it; the moved class stops deriving from it
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:20

Reuse

```text
Database per scenario and container; unchanged
```

RegisterReports

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1326

Reuse

```text
The list of Reports registrations the port base copies with the same lifetimes; production unchanged
```

PortFixtures

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:341

Extend

```text
Compares a port fixture's registrations with the host's; gains the ReportsAtThePortTest case
```

HostUsersStillToMove

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:183

Extend

```text
The ratchet list; loses the ReportWindowBoundaryScenarios line in the S1 move commit
```

ReportWindowPolicyTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs\:58

Reuse

```text
Unit test reading TheTable and BoundaryRow of the moved class; unchanged, so those members keep their names and visibility
```

RefinementAtThePortTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:38

Extend

```text
Already owns TheNeedOn, NeedFacts, TheTabAsSeenBy and the forecast script; takes the hoisted constants, script, shared Given, coach tab reader and a browser-key overload of TheTabAsSeenBy
```

RefinementPortGivens

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:23

Extend

```text
The one writer of Team set-up; takes the Gravity ready-rule builder duplicated in the two need classes
```

NeedOverOneCycleTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:23

Reuse

```text
Holds the twins of the dropped rows; its ids and assertions are unchanged; its specifications use the base readers
```

Slice05NeedAndVerdictTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictScenarios\.cs\:28

Extend

```text
Loses the three rows its twins prove and the steps only they used
```

TheTabAsReadBy

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickSpecifications\.cs\:93

Replace

```text
Repeats the base TheTabAsSeenBy; replaced by a browser-key overload on the base
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes: production routes, DTOs, port interfaces and registrations are unchanged; BoundaryRow and TheTable keep their names for ReportWindowPolicyTest, the only other reader, which compiles in the same commit.
```

contract_tests

```text
No parties change apart: producers (controllers, ports) and consumers (tests) live in one deployable and one commit; the port-fixture lifetime check is part of S1's verification, not an independent contract obligation.
```

### Boundaries

```text
Application-service ports: IReportQueries.Templates and IReportCommands.Create reached from ReportsAtThePortTest (S1); IRefinementViewQuery and ISizingLogCommands reached from RefinementAtThePortTest (S2)
```

#### Driven ports

```text
Real repositories over a database per scenario (ServicePortHarness)
```

```text
FakeLighthouseClock set by the port base
```

```text
ILicenseService stand-in answering no premium licence (Reports)
```

```text
IAppSettingService strict stand-in and ScriptedForecasts (Refinement, unchanged)
```

```text
Tests depend on production ports; production depends on nothing in tests; a port class sets up a Team only through its base step (Reports) or RefinementPortGivens (Refinement)
```

#### Failures

None.

### Observations

```text
NUnit outcome of each row at the port, read from TRX
```

TRX outcome per test id

```text
An HTTP status, a JSON member name or the date text on the wire; those stay pinned by the hosted Reports classes and the Refinement wire contract
```

```text
Sorted test ids before against after the slice
```

Set difference of test ids

```text
Whether an assertion inside a kept id was weakened; the deliberate breaks cover that
```

```text
A named deliberate break in the working tree turns the named row red, then is reverted with git diff showing the file as it was
```

Row outcome under the break

```text
Rules the break does not touch
```

### Equality

None.

### Constructors

None.

### Laws

#### L1

```text
Ids of ReportWindowBoundaryScenarios after the move equal the ids before: 15 rows, no new wire row
```

Invariant

Application

ReportsAtThePortTest

##### Treatment

Finite

###### Cases

```text
15 ids A_window_at_an_edge_is_answered_as_the_table_says(<case>), one per row of reportWindowBoundaries.json
```

```text
Finite named id list
```

##### Evidence

None.

#### L2

```text
Ids of Slice05NeedAndVerdictTest after S2 equal the ids before minus exactly the three mapped ids; ids of NeedOverOneCycleTest and Slice10SleYardstickTest are unchanged
```

Invariant

Application

RefinementAtThePortTest

##### Treatment

Finite

###### Cases

```text
Slice05NeedAndVerdictTest: 17 before, 14 after
```

```text
NeedOverOneCycleTest: unchanged
```

```text
Slice10SleYardstickTest: unchanged
```

```text
Finite named id lists
```

##### Evidence

None.

#### L3

```text
Every dropped row has a kept twin with the same Givens in the same order whose assertions include every assertion of the dropped row
```

Refinement

Application

NeedOverOneCycleTest

##### Treatment

Finite

###### Cases

```text
Below_range_says_how_many_are_ready_against_the_range_the_Team_is_likely_to_pull -> The_need_covers_the_working_days_from_the_next_Refinement_to_the_one_after
```

```text
Too_little_Throughput_history_gives_the_familiar_guard_instead_of_a_number -> Without_a_number_the_need_names_no_cycle(ThroughputHistory,InsufficientData)
```

```text
A_Team_nobody_set_up_has_no_number_because_it_has_no_refinement_states -> Without_a_number_the_need_names_no_cycle(RefinementStates,NoRefinementStates)
```

```text
Finite mapping of three rows
```

##### Evidence

None.

#### L4

```text
Every kept row can fail: a named break turns it red
```

Invariant

Application

ReportsAtThePortTest

##### Treatment

Finite

###### Cases

```text
S1: ReportWindowPolicy.MinimumLengthDays 14 to 15 turns A_window_at_an_edge_is_answered_as_the_table_says(a Then of 14 days) red
```

```text
S2: RefinementSettings.DefaultHighPercentile 85 to 70 turns The_need_covers_the_working_days_from_the_next_Refinement_to_the_one_after red
```

```text
S2: ForecastDataSufficiencyPolicy.MinimumActiveDays 5 to 4 turns Without_a_number_the_need_names_no_cycle(ThroughputHistory,InsufficientData) red
```

```text
S2: RefinementNeedCalculator no longer answering NoRefinementStates turns Without_a_number_the_need_names_no_cycle(RefinementStates,NoRefinementStates) red
```

```text
Finite deliberate-break table
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
Report-window rows at the Reports port
```

Inhabitation

Pass

DesignedNotBuilt

```text
Templates and Create are public methods of IReportQueries and IReportCommands with production implementations registered by RegisterReports; the base is not built yet
```

##### Evidence

None.

```text
Report-window rows at the Reports port
```

PublicReachability

Pass

DesignedNotBuilt

```text
Rows call IReportQueries and IReportCommands, the ports TeamReportsController calls
```

##### Evidence

None.

```text
Report-window and need rows
```

Safety

Pass

DesignedNotBuilt

```text
Id comparison, the twin mapping and the deliberate breaks exclude lost rows and lost assertions; executed per slice by the host
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
The today, earliest-start and edge rows
```

```text
none
```

```text
A row passing on a shifted window
```

```text
The base sets the host instant, 2 Oct 2026 09:00 Europe/Zurich; the today bound row fails on any other day
```

ReportsAtThePortTest

#### Y2

Stressor

```text
Assumption negated: every host assertion survives at the port
```

```text
Status 400, refusal text and JSON names
```

```text
Pinning them on the hosted classes that stay
```

```text
A refusal code or wire name no test pins after the move
```

```text
Seven window codes, today, earliestStart and minimumLengthDays stay pinned by Slice01, Slice02, Slice07 and CreateForSeveralTeamsScenarios; a later move of those classes must re-check
```

ReportsAcceptanceTest

#### Y3

Stressor

```text
Assumption negated: the twin proves the same rule with the same inputs
```

```text
Each need rule proven once
```

```text
Keeping a row that is only a near-twin
```

```text
Dropping a row whose Givens or assertions differ from its twin
```

```text
Three rows mapped with identical Givens; near-twins kept, named in the decisions
```

NeedOverOneCycleTest

#### Y4

Bypass

```text
A port class writes a Team directly through the context or a repository
```

```text
One Team writer per port base
```

```text
The Reports base ATeam step, the only writer there
```

```text
A second Team writer in a Reports port class
```

```text
No guard on the Reports base until a builder exists; PortGivensGuardTest keeps guarding Refinement
```

ReportsAtThePortTest

#### Y5

Stressor

```text
Assumption negated: the port registrations match the host
```

```text
Service lifetimes as on the host
```

```text
none
```

```text
A scoped service registered as singleton at the port, or the reverse
```

```text
The PortFixtures case for ReportsAtThePortTest compares every registration
```

RealityCheckWireContractTest

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Create

```text
The Reports port base: ServicePortHarness with the Reports registrations, the host clock instant, the no-premium licence stand-in, an ATeam step, the Templates and Create steps and WhatItRegisters
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs

Extend

```text
PortFixtures gains the ReportsAtThePortTest case, so its registrations are compared with the host's
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Remove the ReportWindowBoundaryScenarios line (line 183) in the move commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs

Extend

```text
The port-fixture lifetime check learns the two host registration shapes the Reports port base meets, a factory forwarding to another registration and several registrations of one service type, each matched only under the narrow rule of the S1 decision, so a lookalike registration at the port is still reported
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs

#### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~ReportWindowBoundaryScenarios
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~RealityCheckWireContractTest|FullyQualifiedName~ReportWindowPolicyTest|FullyQualifiedName~PortFixtureLifetimesTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortFixtureLifetimesTest\.cs

#### Public oracle

```text
Run ReportWindowBoundaryScenarios at the port: for each of the 15 rows of reportWindowBoundaries.json, set up Team Lightspeed keeping finished Work Items for 365 days (or for ever), read IReportQueries.Templates and call IReportCommands.Create with the row's window; run the port-fixture lifetime check and the host-use guard; run PortFixtureLifetimesTest, which hands PortFixtureLifetimes host registrations shaped as Program registers them (IPortfolioRepository by type, IRepository<Portfolio> as a factory, ReportRepository by type, IReportReader and IReportWriter as factories, four IReportMetric types) and port registrations that differ in one planted way per row
```

```text
15 rows pass with the ids they had on the host; Templates gives today 2026-10-02, the earliest start 2025-10-02 (none for a Team keeping every finished Work Item) and a shortest window of 14 days; rows without a refusal are Done, rows with one are Refused with the table's code; the sorted id lists before and after are equal; HostUsersStillToMove no longer names the class; the ReportsAtThePortTest port-fixture case passes; in PortFixtureLifetimesTest the port registering as the host does gives no difference, and each planted row gives exactly one difference naming the planted service
```

```text
ReportWindowPolicy.MinimumLengthDays 14 to 15 turns A_window_at_an_edge_is_answered_as_the_table_says(a Then of 14 days) red; a Reports registration given another lifetime than on the host turns the ReportsAtThePortTest port-fixture case red; putting the line back on HostUsersStillToMove, or the class starting a host, turns TestHostUseGuardTest red; planted lookalikes each give a difference naming the service: IRepository<Portfolio> registered as the type PortfolioRepository, IReportReader registered as the type ReportRepository, IRepository<Portfolio> as a factory with IPortfolioRepository missing at the port, and an IReportMetric type the host does not register; restoring the crafter's unconditional forwarder skip turns the IRepository<Portfolio> lookalike row red
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
PortFixtureLifetimes matches a service the host builds with a factory forwarding to another registration only through a declared forwarder list naming each service and its forward target: IRepository<Portfolio> to IPortfolioRepository, IReportReader and IReportWriter to ReportRepository. Such a service matches only when the port registers it with a factory too (no implementation type, no instance), with the host's lifetime, and the port registers the forward target with the host's lifetime; the forward target is then compared by what is built like any other registration. A forwarder registered at the port by type or by instance, or one whose forward target the port does not register, is reported. A service type the host registers more than once (every IReportMetric) matches only when the port's implementation type is among the host's registrations of that type; with one host registration the last-registration comparison applies unchanged. What a port factory returns is not observed. The rules live in PortFixtureLifetimes, the one owner of the comparison; the forwarder list is a static member beside StandInsBuiltOnPurpose; the Reality Check and Refinement fixtures register no forwarder and no repeated service type, so all four fixture cases stay green.
```

```text
The planted check PortFixtureLifetimesTest lives under Tests/Architecture (namespace Lighthouse.Backend.Tests.Architecture), beside the other architecture guards, because Tests/Contracts holds only wire contracts (fixtures on WireContractTest) and the check starts no host. PortFixtureLifetimes.cs stays in Contracts/PortFixtures.
```

##### Constraints

None.

##### Reuse

ReportsAtThePortTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

CreateNew

```text
No Reports port base exists; ServicePortHarness users are the Reality Check, Refinement and System-wide-access bases, none registers IReportQueries or IReportCommands
```

PortFixtureLifetimes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:20

Extend

```text
The one owner of the comparison of a port fixture's registrations with the host's; every port fixture case reads it, so the two new registration shapes belong here and nowhere else
```

##### Impact

test\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:1

Changed

```text
New file; no existing consumer
```

acceptance\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:20

Migrated

```text
Base class and steps move from the host to the port; namespace, class and method names unchanged, so the 15 ids stay
```

unit\-test\-consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs\:58

UnchangedCompatible

```text
Reads TheTable and BoundaryRow, which keep their names and visibility
```

wire\-contract\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:341

Changed

```text
One more port-fixture case
```

architecture\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:183

Changed

```text
The ratchet list loses one line
```

host\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:35

UnchangedCompatible

```text
Loses one derived class; its 17 other users and its members are unchanged
```

test\-helper

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/PortFixtures\/PortFixtureLifetimes\.cs\:20

Changed

```text
Gains the forwarder and multi-registration rules; its consumers are RealityCheckWireContractTest (four port fixture cases plus the before-the-swaps test reading OnTheHost and WhatIsBuilt, both unchanged in signature) and the new planted-registration support; the Reality Check and Refinement fixtures register no forwarder and no repeated service type, so their outcome is unchanged
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
No preserving move precedes the behaviour: the base is a new file, not a restructuring of existing code, and the move commit is itself the behaviour-free change, observed by the id comparison before and after.
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

###### Y6

Bypass

```text
Assumption negated: a port factory for a declared forwarder returns the forward target
```

```text
A forwarder registered at the port by type or instance, or without its forward target, is reported
```

```text
A port factory for a forwarder passes on lifetime and the presence of its forward target
```

```text
A lookalike type registration of a forwarder passing the check
```

```text
What a port factory returns is not observed; a factory returning a lookalike would pass. Observing it needs resolving the port container, which needs a database per check; not built
```

acceptance\-designer

##### Architectural tests

###### T1

```text
No class outside HostUsersStillToMove or a named reason starts a web host; ReportWindowBoundaryScenarios leaves the list and no longer starts one
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
Takes the need-text constants, GravitysCycleWorkingDays, the five-to-eight script, the shared Gravity-on-Friday-2-October Given and the coach tab reader; TheTabAsSeenBy gains a browser-key overload
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs

Extend

```text
Takes the Gravity ready-rule builder and its ready tag, the one writer of Team set-up
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleSpecifications\.cs

Extend

```text
Drops its copies of the hoisted members and NeedIn/NeedReading; uses TheNeedOn and NeedFacts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs

Extend

```text
Only if a hoisted step takes another name: the calls change, no test name or assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickSpecifications\.cs

Extend

```text
Drops TheTabAsReadBy for the base overload; rewords the stale host-design comment above GivenTodayIsWednesdayTheSeventhOfOctober
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictScenarios\.cs

#### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~Slice05NeedAndVerdictTest|FullyQualifiedName~NeedOverOneCycleTest|FullyQualifiedName~Slice10SleYardstickTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~PortGivensGuardTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest|FullyQualifiedName~RealityCheckWireContractTest'
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictSpecifications\.cs

#### Public oracle

```text
Run Slice05NeedAndVerdictTest, NeedOverOneCycleTest and Slice10SleYardstickTest at the port after the hoist and after the drop, compare sorted ids with the ids before, and apply the deliberate breaks to the twins of the dropped rows
```

```text
After the hoist every id of the three classes is unchanged and passes; after the drop the id difference is exactly Below_range_says_how_many_are_ready_against_the_range_the_Team_is_likely_to_pull, Too_little_Throughput_history_gives_the_familiar_guard_instead_of_a_number and A_Team_nobody_set_up_has_no_number_because_it_has_no_refinement_states, Slice05NeedAndVerdictTest has 14 rows and all pass; the Gravity ready-rule builder, tab and need readers, shared constants, five-to-eight script and the tab-by-browser-key reader each exist once, in RefinementAtThePortTest or RefinementPortGivens
```

```text
RefinementSettings.DefaultHighPercentile 85 to 70 turns The_need_covers_the_working_days_from_the_next_Refinement_to_the_one_after red; ForecastDataSufficiencyPolicy.MinimumActiveDays 5 to 4 turns Without_a_number_the_need_names_no_cycle(ThroughputHistory,InsufficientData) red; RefinementNeedCalculator no longer answering NoRefinementStates turns Without_a_number_the_need_names_no_cycle(RefinementStates,NoRefinementStates) red; any id missing beyond the three, or an extra id, fails the id comparison
```

##### Promises

P3

P4

#### Prefactoring

```text
Hoist the members duplicated across Slice05NeedAndVerdictSpecifications, NeedOverOneCycleSpecifications and Slice10SleYardstickSpecifications into RefinementAtThePortTest and RefinementPortGivens, changing no test id or assertion; this makes the row drop a deletion of three rows and their now-unused steps
```

```text
Every test id and outcome of Slice05NeedAndVerdictTest (17), NeedOverOneCycleTest and Slice10SleYardstickTest
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs

##### Verification

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter 'FullyQualifiedName~Slice05NeedAndVerdictTest|FullyQualifiedName~NeedOverOneCycleTest|FullyQualifiedName~Slice10SleYardstickTest'
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

NeedOverOneCycleTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleSpecifications\.cs\:13

Extend

```text
Its step definitions lose the copies of the hoisted members and NeedIn/NeedReading and use the base TheNeedOn and NeedFacts; its rows are the twins of the dropped rows
```

##### Impact

test\-base

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementAtThePortTest\.cs\:38

Changed

```text
Gains hoisted members and an overload; every derived port class and PortGivensGuardTest plants still compile
```

test\-builder

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/RefinementPortGivens\.cs\:23

Changed

```text
Gains the Gravity ready-rule builder; the wire contract and other users unchanged
```

acceptance\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictScenarios\.cs\:28

Changed

```text
Three rows dropped, 17 to 14
```

acceptance\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:23

UnchangedCompatible

```text
Ids and assertions unchanged; only step owners move
```

acceptance\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickSpecifications\.cs\:93

Changed

```text
Uses the base overload; ids unchanged
```

architecture\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs\:27

UnchangedCompatible

```text
Keeps guarding Refinement port classes; the hoisted builder lives in RefinementPortGivens, which it allows
```

acceptance\-test\-steps

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleSpecifications\.cs\:13

Changed

```text
Copies of hoisted members removed; base readers used; ids and assertions unchanged
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

###### T2

```text
Only RefinementPortGivens writes Team set-up in a Refinement port class, including the hoisted Gravity ready-rule builder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
env -u AzureDevOpsLighthouseIntegrationTestToken -u JiraLighthouseIntegrationTestToken -u JiraLighthouseRestrictedIntegrationTestToken -u JiraScopedTokenIntegrationTestToken -u ServiceNowLighthouseIntegrationTestToken -u LinearAPIKey -u AzureDevOpsLighthouseE2ETestToken dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --logger trx --filter FullyQualifiedName~PortGivensGuardTest
```

##### Contract tests

None.

## ADRs

None.
