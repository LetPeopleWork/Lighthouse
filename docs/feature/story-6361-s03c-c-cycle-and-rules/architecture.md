# Architecture brief: story\-6361\-s03c\-c\-cycle\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented, ports and adapters. Owners stay: the throughput filter rule is ForecastFilterRuleService's, the sufficiency rule is ForecastDataSufficiencyPolicy's (Forecasting Domain), the How Many forecast is ForecastService's. Flow metrics gains three ports in Modules/FlowMetrics/Application that name what it asks, in its own words; Forecasting answers them. No rule is copied, no new decision is created; the only new class is a delegating adapter in Forecasting's Application. Carriers: decisions stay in ForecastDataSufficiencyPolicy and ForecastFilterRuleService; effects (forecast round, DI) stay in ForecastService, ForecastUpdater and Program.cs; per-process state stays in ForecastLane and the metrics cache. Bound contexts touched: Flow metrics and Forecasting (ADR-270); no new term, no domain event, no UI surface.
```

#### D2

```text
Checker mapping: no state, transition, protocol, concurrency, recovery or scheduling behaviour changes, so Quint, TLA+/TLC and Agda cover no claim here (Quint is installed; nothing is modelled). Liveness: not applicable, no progress claim is added or changed. Dependency-direction, layer and placement claims are decided exactly by the IL-reading rules (IlDependencies) in the S2, S3 and S4 oracles, each with a made-up offender as its falsifier. Answer preservation is observed by SeededForecastsStayTheSameTest, forecast-answers-compare and metrics-answers-compare; trigger and seeding firing by the S1 public command. A property test over seeds would compare the code with itself (both sides run the same code), so pinned values and before/after captures are the falsifiers. Types: C# nominal interfaces under the compiler; the claim is that no Flow metrics type names a Forecasting type, checked on the IL, not a proof.
```

#### D3

```text
ForecastUpdater edge into the shared update infrastructure (UpdateServiceBase, UpdateKey in Services.Implementation.BackgroundServices.Update): the shared update code stays where it is, because it also serves TeamUpdater and PortfolioUpdater; moving it is S09a's. S3 adds one Forecasting Adapters outbound rule: Modules.Forecasting.Adapters reaches Services.Implementation.BackgroundServices.Update only through an inline list, compared both ways, whose entries are the ForecastUpdater edges read from the IL in S3 (expected UpdateServiceBase and UpdateKey). The list is a ratchet candidate for S09a and only shrinks. The edges into Services.Interfaces.Update, DomainEvents and Repositories are platform-lane contracts and stay legal by namespace; ForecastUpdater's licence read stays named in prose (R-F6).
```

#### D4

```text
Behaviour-free: same objects answer the same calls through another interface name. Program.cs keeps every existing line and position; the new registrations resolve the existing scoped instances (IThroughputFilter resolves the scoped ForecastFilterRuleService instance already registered for IForecastFilterRuleService; the Forecasting adapter answering IForecastDataSufficiency and IHowManyForecaster is scoped, the lifetime of IForecastService it delegates to). Handler order, routes, DTOs, responses and schema unchanged; BaseMetricsService keeps resolving through the service provider (S07b removes that), now asking for IHowManyForecaster.
```

#### D5

```text
Moves and renames search full type names inside strings (ILogger<T> categories, perf harness, Stryker configs, reflection by name, test-name lists in guards), not only using lines. Checked at design for the S4 test move: no guard list, Stryker config, script or workflow names a Forecasting test class by namespace (stryker.6094 filters by class name, which survives the move); the crafter repeats the search at the move.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTO shapes, serialized responses, metrics answers, forecast results, DI lifetimes of existing registrations, domain-event handler order and the schema stay the same. DomainEventHandlerOrderPinTest, SeededForecastsStayTheSameTest and the existing API, E2E and response tests stay green and unmodified. No bound on stored or validated input changes (none is added, widened or narrowed).
```

#### K2

```text
One oracle file per slice; never edit an earlier slice's or earlier feature's oracle or support bytes. Rules read the compiled IL, lists are inline and compared both ways, never loosened; made-up offenders live in fixture namespaces under Lighthouse.Backend.Modules.ForecastingFixtures.* (they may not reach API, Data or EF Core, nor be domain-event handlers). Rule tests assert by the failure naming the offender, not by counts. No nWave scenario tags in code. Default per-test budgets; no derogation. Test-writing skill wins over nWave timing and PBT density rules.
```

#### K3

```text
Every dotnet command runs under flock -o on the shared lock file. Fast loop Test-Fast.ps1 plus the slice oracle; full gates (dotnet build zero warnings, dotnet test with the CLAUDE.md connector filter) at slice end. Pure moves (git mv) are their own commit; edits follow in separate commits. Comments only for a why, no internal ids.
```

#### K4

```text
Code-fact: code-fact query.callers-of answered from textsearch at noisy confidence with an empty site list for C# symbols, so it is unknown, not zero callers. Callers and consumers come from git grep over source, limited to textual names (reflection-by-string and generated code may be missed); the IL rules are the authoritative check. Jev: unauthorized, not called.
```

### Reuse

ForecastFilterRuleService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Extend

```text
Already holds GetEffectiveRuleSet and Filter with the signatures TeamMetricsService calls; it also implements IThroughputFilter by declaration only, so there is one filter, no second implementation.
```

ForecastDataSufficiencyPolicy

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Domain\/ForecastDataSufficiencyPolicy\.cs\:5

Reuse

```text
The sufficiency rule stays Forecasting's single owner (the reality check reads it too); the adapter calls it, nothing copies the limit.
```

HowMany

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastService\.cs\:12

Reuse

```text
The How Many forecast stays ForecastService's; IHowManyForecaster's answer delegates to it.
```

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
The one shared rules class; its rule wording gains the module name and the module's legal callers as parameters, and Forecasting's rules call it with Forecasting's namespaces. No second rules class.
```

FlowMetricsUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsUnitTestPlacementTest\.cs\:15

Reuse

```text
Its placement method (files on disk plus classes read from the test assembly with symbols) is the model for ForecastingUnitTestPlacementTest. Generalising it would edit an earlier feature's approved oracle, so S4 adds a sibling fixture; folding the two is a later improvement item.
```

forecast\-answers\-capture\.cs

Scripts\/examine\/build\-examine\-commands\:75

Reuse

```text
Its in-process WebApplicationFactory start on the seeded demo data, scenario loading and wait-until-idle loop are the model for the S1 probe; no existing command shows a trigger or the seeder firing.
```

RefreshLogDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/RefreshLogDto\.cs\:12

Reuse

```text
GET /api/v1/systeminfo/refreshlog already returns per run the Type (Forecast), EntityId, ExecutedAt and Skipped; the S1 probe observes the triggers and the seeder through it, no new endpoint.
```

### Impact

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1476

Changed

```text
S2: three port registrations added next to the filter-rule registration; existing lines, order and lifetimes unchanged
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:14

Changed

```text
S2: constructor parameters swap to IThroughputFilter and IForecastDataSufficiency (S107 suppression already present); behaviour identical
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:21

Changed

```text
S2: resolves IHowManyForecaster through the same service-locator call
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Changed

```text
S2: declares IThroughputFilter, no new method
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/IForecastFilterRuleService\.cs\:7

UnchangedCompatible

```text
S2: kept as is for API, Refinement and Forecasting callers
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs\:1

Migrated

```text
S2: with the other ten listed consumer tests, construction and provider stubs move to the ports; assertions unchanged
```

arch\-rule\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:56

Changed

```text
S2: Forecasting edge list emptied, kept as an empty two-way list
```

arch\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastFilterSeamArchUnitTest\.cs\:15

Changed

```text
S2: filter seam keyed on the port and the old interface
```

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastServiceTest\.cs\:1

Migrated

```text
S4: with every moved Forecasting unit-test file, git mv, namespace follows the folder, test names unchanged
```

build\-config

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\.6094\.backend\.json\:11

UnchangedCompatible

```text
S4: filters by class name, which survives the move
```

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/UpdateServiceTestBase\.cs\:1

UnchangedCompatible

```text
S4: stays shared with Team and Portfolio updater tests; moved tests gain a using
```

### Agreement

None.

### Not applicable

contract_tests

```text
Producer and consumer of every touched contract are in one deployable and change in the same commit (the three ports and their Forecasting answers); the primary oracles plus metrics-answers-compare and forecast-answers-compare observe the unchanged answers. The examine.json entry is read by the nWave examiner, which reads it only by name and path.
```

### Boundaries

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

None.

### Models

None.

### Closure

```text
Flow metrics to Forecasting cycle removed
```

Safety

Pass

DesignedNotBuilt

```text
S2 IL rule excludes any Flow metrics edge into Forecasting namespaces; a made-up Flow metrics type reaching a Forecasting type fails and is named
```

##### Evidence

None.

```text
Flow metrics to Forecasting cycle removed
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each port has an existing answerer: ForecastFilterRuleService (filter), ForecastDataSufficiencyPolicy and ForecastService.HowMany through the new adapter
```

##### Evidence

None.

```text
Flow metrics to Forecasting cycle removed
```

PublicReachability

Pass

DesignedNotBuilt

```text
Team and Portfolio metrics endpoints and the forecast endpoints reach the ports through Program.cs registrations; observed by metrics-answers-compare and forecast-answers-compare on demo data
```

##### Evidence

None.

```text
Flow metrics to Forecasting cycle removed
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no progress, scheduling or waiting behaviour is added or changed
```

##### Evidence

None.

```text
Forecast triggers and start-up seeding observable
```

PublicReachability

Pass

DesignedNotBuilt

```text
S1 command drives POST /api/v1/teams/{id} and a restart, and reads GET /api/v1/systeminfo/refreshlog
```

##### Evidence

None.

```text
Forecast triggers and start-up seeding observable
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable as a design claim: the probe waits on GET /api/v1/update/status with a bounded timeout and reports a timeout as not observed; no temporal property is claimed
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
Assumption: every Flow metrics reach into Forecasting is a compiled reference. Negated: a resolve by string, reflection or a catch/event/attribute-only reference that IlDependencies does not read
```

```text
No Flow metrics type names a Forecasting type
```

```text
The S2 rule stays green while such a hidden reference exists
```

```text
A new edge added through a normal call or signature unseen
```

```text
IlDependencies does not read catch-clause types, events or named attribute arguments; no current code hits it (improvement candidate)
```

host \(improvement item\)\; reviewer

#### Y2

Stressor

```text
Assumption: the same instance answers both interfaces. Negated: IThroughputFilter registered as a second ForecastFilterRuleService
```

```text
Filter rules and throughput answers
```

```text
A second scoped instance with the same stateless answers
```

```text
Changed lifetime or a different answer
```

```text
ForecastFilterRuleService holds no per-request state; metrics-answers-compare observes answers
```

crafter

#### Y3

Stressor

```text
Assumption: BaseMetricsService's provider can resolve IHowManyForecaster wherever it resolved IForecastService. Negated: a test or host provider that registered only IForecastService
```

```text
Predictability score
```

```text
Tests that stub the service provider are migrated to stub the port
```

```text
A runtime resolve failure in a metrics endpoint
```

```text
Program.cs registers the port; full backend suite and metrics-answers-compare run on the candidate
```

crafter

#### Y4

SinglePointOfFailure

```text
Second stressor walk: the S1 probe runs on demo data whose demo connector and free licence limits decide which Teams refresh
```

```text
One Team refresh and one restart are observable
```

```text
The probe picks a Team inside the free limits, or uses the licence file like forecast-answers-compare
```

```text
A probe that reports OBSERVED without a Forecast row after the refresh
```

```text
A host without the demo data files cannot run it; it says so and exits non-zero
```

acceptance designer

#### Y5

Stressor

```text
Second stressor walk: Forecasting test move. Negated assumption: no string names a moved test class by its namespace
```

```text
Every moved test still runs in the fast loop, Stryker and CI filters
```

```text
Filters by class name keep matching
```

```text
A test silently dropped from a filter or guard list
```

```text
Checked at design: none found; the crafter repeats the search
```

crafter

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

None.

Scripts\/examine\/forecast\-triggers\-show\.cs

#### Verification

```text
bash Scripts/examine/build-examine-commands
bash Scripts/examine/bin/forecast-triggers-show
```

#### Supports

Scripts\/examine\/build\-examine\-commands

\.nwave\/examine\.json

#### Public oracle

```text
Run the public examine command forecast-triggers-show (declared in .nwave/examine.json, written into Scripts/examine/bin by build-examine-commands from the committed driver Scripts/examine/forecast-triggers-show.cs). It starts the checkout's backend in-process on a fresh SQLite database, loads one demo scenario through POST /api/v1/demo/scenarios/{id}/load, waits until GET /api/v1/update/status is idle, and reads GET /api/v1/systeminfo/refreshlog. (1) Team refresh: it picks a Team that serves at least one Portfolio, records the time, sends POST /api/v1/teams/{teamId}, waits until idle and reads the refresh log again. (2) Start-up seeding: it asks one Portfolio's forecast again with unchanged data (POST /api/v1/portfolios/refresh-all or the Team refresh again) and reads that round's Skipped flag, then disposes the host, starts a new host on the same database file, triggers the same round and reads the refresh log.
```

```text
One line per observation and a last VERDICT line; exit 0 and OBSERVED only when (1) after the Team refresh the log holds a Forecast row (Type Forecast) for every Portfolio the Team serves with ExecutedAt later than the refresh request, and (2) the round on unchanged data before the restart is Skipped=true while the first round after the restart is Skipped=false, because the seeder forgot the stored forecast inputs at start. Each line names the Team, Portfolio ids and ExecutedAt values it read. A timeout or a missing row prints NOT OBSERVED with what was missing, why it matters and how to rerun, and exits non-zero.
```

```text
With the TeamDataRefreshed forecast trigger registration removed from Program.cs the command prints NOT OBSERVED for (1); with the ForecastInputsForgottenAtStartSeeder registration removed the first round after restart is Skipped=true and (2) is NOT OBSERVED. A command that reads only producer test results, or reports OBSERVED with no Forecast row read, is wrong.
```

##### Promises

P1

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
The probe is the slice oracle and is authored by the acceptance designer: a committed C# file-based driver Scripts/examine/forecast-triggers-show.cs (modelled on forecast-answers-capture.cs: WebApplicationFactory in-process, sqlite in a temp work folder, demo scenario load, wait on update status, optional licence file), copied by build-examine-commands into Scripts/examine/bin with the checkout path filled in, plus a bash wrapper bin/forecast-triggers-show. bin/ is git-ignored and generated. It runs on the checkout's HEAD only; it compares nothing across revisions. No production code changes in S1: the triggers, updater and seeder already exist, so the command is green on main and stays green through S2 to S4.
```

```text
Observation surface: the refresh log (RefreshLogDto: Type, EntityId, ExecutedAt, Skipped) through GET /api/v1/systeminfo/refreshlog, and update idleness through GET /api/v1/update/status, both existing public routes used with auth off in the examine host. Skipped distinguishes a recomputed round from a confirmed-unchanged one, which is what the start-up seeder changes.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

reuse

```text
No production or test code target: the slice adds only the acceptance designer's examine probe and its declaration; reuse owners are in the shared rows (forecast-answers-capture.cs, RefreshLogDto).
```

impact

```text
No production target; build-examine-commands gains one generated command and examine.json one entry, both acceptance supports, and every existing generated command is unchanged.
```

agreement

```text
The examine.json entry and the generated command are read only by the nWave examiner by name and path; no product contract changes.
```

architectural_tests

```text
No dependency, boundary or owner law changes in this slice.
```

prefactoring

```text
No production code target to prepare.
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

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IThroughputFilter\.cs

Create

```text
Port owned by Flow metrics, in Flow metrics' words
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IForecastDataSufficiency\.cs

Create

```text
Port owned by Flow metrics, in Flow metrics' words
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IHowManyForecaster\.cs

Create

```text
Port owned by Flow metrics, in Flow metrics' words
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastingAnswersToFlowMetrics\.cs

Create

```text
Delegating adapter: implements IForecastDataSufficiency (calls ForecastDataSufficiencyPolicy.HasEnoughData) and IHowManyForecaster (calls IForecastService.HowMany)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs

Extend

```text
Also implements IThroughputFilter; no new method
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

```text
Constructor takes IThroughputFilter and IForecastDataSufficiency instead of IForecastFilterRuleService and the static policy; no Forecasting using left
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

```text
Resolves IHowManyForecaster instead of IForecastService through the same service-locator call
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Three added registrations next to the IForecastFilterRuleService line; no existing line moves
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
Forecasting edge list emptied (kept as an empty two-way list); its made-up-type tests keep their Forecasting stand-ins
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastFilterSeamArchUnitTest\.cs

Extend

```text
Seam reworded to the port: only TeamMetricsService and ForecastFilterRuleService call Filter on IThroughputFilter or IForecastFilterRuleService; internal-id wording removed
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ClosedInRangeCharacterisationTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/InstanceDayItemBucketingTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/MetricsAsOfGroupNameTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceSnapshotTests\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WorkItemAgeAsOfDateTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckWallClockProbe\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastFilterFeatureForecastIntegrationTest\.cs

Extend

```text
Consumer migration only: constructs TeamMetricsService with the ports, or stubs IHowManyForecaster where it stubbed IForecastService in the service provider; no assertion changes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~FlowMetricsDoesNotReachForecastingArchUnitTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter 'FullyQualifiedName~Lighthouse.Backend.Tests.Modules.FlowMetrics|FullyQualifiedName~SeededForecastsStayTheSameTest|FullyQualifiedName~ForecastFilterFeatureForecastIntegrationTest'
bash Scripts/examine/bin/metrics-answers-compare
bash Scripts/examine/bin/forecast-answers-compare
bash Scripts/examine/bin/forecast-triggers-show
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly with IlDependencies and apply the rule: no type in a namespace matching Lighthouse.Backend.Modules.FlowMetrics reaches a type in a namespace matching Lighthouse.Backend.Modules.Forecasting (or the old Services Forecast namespaces); and the three ports IThroughputFilter, IForecastDataSufficiency and IHowManyForecaster live in Lighthouse.Backend.Modules.FlowMetrics.Application and are implemented by types in Lighthouse.Backend.Modules.Forecasting. Run the same rule over made-up types under Lighthouse.Backend.Modules.ForecastingFixtures and a made-up Flow metrics module.
```

```text
The rule passes on production with no list (no Flow metrics to Forecasting edge exists); a made-up Flow metrics type reaching a made-up Forecasting type fails and is named with the type it reaches; a made-up Flow metrics type reaching only its own port passes. Metrics and forecast answers stay identical (metrics-answers-compare, forecast-answers-compare, SeededForecastsStayTheSameTest).
```

```text
Leaving TeamMetricsService on IForecastFilterRuleService, the static ForecastDataSufficiencyPolicy call, or BaseMetricsService on IForecastService makes the rule fail naming that edge; a port declared outside Flow metrics' Application, or one with no Forecasting implementation, fails.
```

##### Promises

P2

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
Ports (Flow metrics' Application, namespace Lighthouse.Backend.Modules.FlowMetrics.Application): IThroughputFilter { WorkItemRuleSet? GetEffectiveRuleSet(Team team); IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet); } - two members, because TeamMetricsService asks for the Team's effective rule set before it filters; one member would leave the GetEffectiveRuleSet edge in place. IForecastDataSufficiency { bool HasEnoughData(RunChartData throughput); }. IHowManyForecaster { HowManyForecast HowMany(RunChartData throughput, int days); }. All parameter and return types stay in Models (kernel), so the ports name no Forecasting type.
```

```text
Answers (Forecasting): ForecastFilterRuleService declares IThroughputFilter beside IForecastFilterRuleService (same methods, no new code; IForecastFilterRuleService is unchanged). ForecastingAnswersToFlowMetrics in Modules/Forecasting/Application takes IForecastService and implements IForecastDataSufficiency by calling ForecastDataSufficiencyPolicy.HasEnoughData and IHowManyForecaster by calling IForecastService.HowMany; it decides nothing itself. Program.cs: AddScoped<IThroughputFilter>(sp => (ForecastFilterRuleService)sp.GetRequiredService<IForecastFilterRuleService>()) so the one scoped instance answers both; AddScoped<ForecastingAnswersToFlowMetrics>() with AddScoped<IForecastDataSufficiency> and AddScoped<IHowManyForecaster> resolving it. Forecasting keeps depending on Flow metrics (ITeamMetricsService, ForecastThroughputStatus), which is now the only direction. The Flow metrics outbound Forecasting list goes 3 to 0 here, because the rule compares both ways and the gone edges would fail it.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

IThroughputFilter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastFilterRuleService\.cs\:1

Producer

Migrated

```text
Implements the port with its existing GetEffectiveRuleSet and Filter
```

IThroughputFilter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:912

Consumer

Migrated

```text
Calls the same two methods through the port, same instance
```

IForecastDataSufficiency

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastingAnswersToFlowMetrics\.cs\:1

Producer

Migrated

```text
New adapter delegating to ForecastDataSufficiencyPolicy.HasEnoughData
```

IForecastDataSufficiency

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:115

Consumer

Migrated

```text
Same answer, asked through the injected port
```

IHowManyForecaster

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/ForecastingAnswersToFlowMetrics\.cs\:1

Producer

Migrated

```text
Delegates to IForecastService.HowMany
```

IHowManyForecaster

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:21

Consumer

Migrated

```text
Same service-locator resolve, now of the port
```

##### Not applicable

prefactoring

```text
The edit itself is the inversion; no separate preserving move makes it easier (the module move was feature B).
```

reuse

```text
Reuse owners for every S2 target are the shared rows (ForecastFilterRuleService, ForecastDataSufficiencyPolicy, IForecastService HowMany): the three ports and the adapter are new because no Flow metrics-owned contract for these questions exists, and they delegate to those owners; the metrics services, Program.cs, the two rule files and the consumer tests are edited in place, not replaced.
```

impact

```text
Impact of every S2 target is recorded once in the shared impact rows marked S2 (edited production files, Program.cs, the two rule files, the consumer tests); the four new files have no consumer before this slice and their consumers are among those rows.
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
No Flow metrics type reaches a Forecasting type; the three ports live in Flow metrics' Application and are answered by Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~FlowMetricsDoesNotReachForecastingArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Rule wording takes the module's name and its legal callers in words as a parameter (default: Flow metrics wording, so earlier callers compile unchanged); TypesLiveInTheirLayer advice says to move the file; OutboundEdges empty-subject text names the layer it checks
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
Five Forecasting entries removed (10 to 5); Forecasting module namespaces join the legal callers
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ForecastingModuleBoundariesArchUnitTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly with IlDependencies and apply Forecasting's module rules through ModuleRules with Forecasting's namespaces and wording: outside to Modules.Forecasting.Domain only from the inline list; outside to Modules.Forecasting.Adapters only from the composition root; outside to Modules.Forecasting.Application only from the API, the composition root, Refinement plus the inline list; Application outbound (no EF Core, Data, API or module Adapters; Licensing only through the list); Adapters outbound into Services.Implementation.BackgroundServices.Update only through the list. Apply Flow metrics' inbound Application rule with its list. Run every rule over made-up types under Lighthouse.Backend.Modules.ForecastingFixtures.
```

```text
Every rule passes with its inline list, compared both ways, baselines read from the IL in this slice: Domain inbound {Lighthouse.Backend.Program} (expected), Adapters inbound composition root only, Application inbound {PortfolioUpdater} (expected), Application outbound Licensing {ForecastFilterRuleService -> ILicenseService} (expected), Adapters outbound update infrastructure {ForecastUpdater edges to UpdateServiceBase and UpdateKey} (expected). Flow metrics' inbound list holds five entries and no Forecasting type. A failing rule's message names the module it checks (Forecasting or Flow metrics). Made-up offenders fail and are named; compliant twins pass.
```

```text
A made-up outside type reaching the Forecasting Domain, Adapters or Application not on the list fails naming it; a listed edge that is gone fails naming it for deletion; a Forecasting Application type reaching EF Core or a module Adapter fails; a new Adapters edge into the update implementation namespace fails; a Forecasting entry left on Flow metrics' inbound list fails as gone; a rule message saying Flow metrics while checking Forecasting is wrong.
```

##### Promises

P3

P5

#### Prefactoring

```text
L2 parameterise the rule wording in ModuleRules (module name and legal callers in words, Flow metrics default) before Forecasting's rules call it; makes adding Forecasting's rules a call with its own wording
```

```text
Every Flow metrics rule verdict and every earlier made-up-offender outcome
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs

##### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter 'FullyQualifiedName~ModuleRulesCatchANewOffenderArchUnitTest|FullyQualifiedName~FlowMetrics'
```

#### Records

##### Decisions

```text
Rule set (R-F1..R-F6 plus one): R-F1 and R-F2 stay held by feature B's layout oracles, unchanged. New in this slice, all through ModuleRules: R-F3 Domain inbound, R-F4 Adapters inbound (composition root), R-F5 Application inbound (legal by namespace: API, the composition root, Refinement; list), R-F6 Application outbound, and the Adapters outbound list into the shared update implementation namespace (decided in the shared ForecastUpdater decision). Baselines R-F3, R-F5, R-F6 and the Adapters list are read from the IL in this slice, not copied from the design table; an IL answer that differs from the expected entries is recorded as the baseline and reported, never loosened. Flow metrics: inbound list 10 to 5; outbound Forecasting list already empty since S2.
```

```text
Shared wording: a ModuleWording value (the module's name in words and its legal callers in words) is a parameter of each ModuleRules rule whose text names a module or its callers, with Flow metrics' wording as the default, so the earlier feature oracles that call ModuleRules compile and keep their meaning without an edit. Forecasting's rules pass Forecasting's wording. If an earlier oracle asserts the old wording text, the crafter stops and returns it to the architect instead of editing that oracle.
```

##### Constraints

None.

##### Reuse

ApplicationIsReachedFromOutsideOnlyBy

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:65

Extend

```text
Forecasting's Application inbound rule reuses it with its own namespaces and wording
```

OutboundEdges

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:98

Extend

```text
Forecasting's Application outbound and Adapters outbound lists reuse it
```

OtherOutsideTypesReachingTheApplication

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:54

Extend

```text
Flow metrics' inbound list is edited in place: five Forecasting entries off, Forecasting legal by namespace; no second list
```

##### Impact

test\-support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Changed

```text
Wording parameter with Flow metrics default; verdict logic unchanged
```

arch\-rule\-list

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:54

Changed

```text
Five Forecasting entries off; Forecasting legal by namespace
```

arch\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier oracle; compiles and keeps meaning through the default wording
```

##### Agreement

None.

##### Not applicable

agreement

```text
Only test-side rule code changes; no shared product contract is touched.
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

###### T1

```text
Forecasting's Domain, Adapters and Application are reached from outside only from their inline lists or legal namespaces; its Application and Adapters reach outward only as listed; Flow metrics' inbound list names no Forecasting type; rule messages name the module they check
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ForecastingModuleBoundariesArchUnitTest
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/AddressableDrawStreamTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/AddressableDrawStreamTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/ForecastRunThatCannotEndTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastRunThatCannotEndTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Domain\/RealityCheckVerdictPolicyTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/RealityCheckVerdictPolicyTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastBeyondTheHorizonTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastBeyondTheHorizonTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastDrawCoordinatesTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastDrawCoordinatesTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterFeatureForecastIntegrationTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastFilterRuleServiceIntegrationTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterRuleServiceIntegrationTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastInputFingerprintTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastInputFingerprintTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastLaneTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastLaneTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastRealityCheckServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastRealityCheckServiceTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceCrossTeamDependencyTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastServiceCrossTeamDependencyTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceDependencyTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastServiceDependencyTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceSchedulerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastServiceSchedulerTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceStartDayTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastServiceStartDayTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastServiceTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastServiceTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/ForecastWorkerBoundTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastWorkerBoundTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/MultiTeamJointForecastTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/MultiTeamJointForecastTest.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/OnePortfolioForecast\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/OnePortfolioForecast.cs; namespace follows the folder
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/FeatureOrderingPolicyChangedForecastTriggerHandlerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/FeatureRankChangedForecastTriggerHandlerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandlerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandlerTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/TeamDeletedForecastRetriggerHandlerTest.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/ForecastUpdaterTest.cs; keeps UpdateServiceTestBase, which stays shared
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastInputsForgottenAtStartSeederTests\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Seeding/ForecastInputsForgottenAtStartSeederTests.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

#### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ForecastingUnitTestPlacementTest
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Modules.Forecasting
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

None.

#### Public oracle

```text
Find the test source files on disk and read the compiled test assembly with its symbols; for each listed Forecasting unit-test file with its layer (Domain, Application, Application/Policies, Adapters/Background), check it sits only under Lighthouse.Backend.Tests/Modules/Forecasting/<layer>, nowhere else, and its classes live in the namespace of that folder; run the same placement rule over made-up test trees.
```

```text
Every listed file sits only in its layer folder with matching namespaces; the folder holds at least one compiled class; made-up trees report a file left in its old place, a copy left behind, a file in the wrong layer and a namespace not matching its folder, and do not report a compliant twin. The moved tests pass with the same names and results as before the move.
```

```text
A test file left under Services/Implementation/Forecast or BackgroundServices/Update, copied, put in the wrong layer, or with a stale namespace is reported by name; an empty module test folder fails instead of passing vacuously.
```

##### Promises

P4

P5

#### Prefactoring

```text
L1 pure git mv of the Forecasting unit-test files into Tests/Modules/Forecasting/<layer> before improving them; makes the improvements reviewable as edits
```

```text
Every moved test's name and result
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Forecast\/ForecastServiceTest\.cs

##### Verification

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock pwsh Lighthouse.Backend/Test-Fast.ps1
```

#### Records

##### Decisions

```text
Moves: the 18 files of Tests/Services/Implementation/Forecast (helper OnePortfolioForecast included), the four trigger handler tests, ForecastUpdaterTest and the seeder test go to Tests/Modules/Forecasting/{Domain,Application,Application/Policies,Adapters/Background} by the layer of their subject (Domain: draw stream, run that cannot end, verdict policy; Application: ForecastService, lane, fingerprint, reality check, filter rules and their helpers). Stay: API/Integration and Integration/Containers tests, Acceptance/Forecasting, TestDoubles, TestHelpers, UpdateServiceTestBase and PortfolioUpdaterTest (shared or not Forecasting's). A pure git mv commit first (namespace follows the folder only as far as the move needs), then separate edit commits that improve the touched classes under the test-writing skill (rows move down to the port or unit level; names drop internal ids; no assertion loses meaning). Placement fixture is a sibling of FlowMetricsUnitTestPlacementTest, not a generalisation, because that file is an earlier feature's approved oracle.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

reuse

```text
Pure relocation of existing test files; each keeps its subject and owner. The placement fixture's reuse decision is in the shared rows.
```

agreement

```text
Test files only; no product contract.
```

impact

```text
Every S4 target is a git mv of an existing test file; impact is recorded once in the shared impact rows marked S4.
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
Forecasting's unit tests sit under Tests/Modules/Forecasting in their layer folder with the folder's namespace, and nowhere else
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

```text
flock -o /tmp/claude-1000/-storage-repos-Lighthouse/e10a069d-75fb-4139-b81b-0d9e268a63ec/scratchpad/dotnet.lock dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ForecastingUnitTestPlacementTest
```

##### Contract tests

None.

## ADRs

None.
