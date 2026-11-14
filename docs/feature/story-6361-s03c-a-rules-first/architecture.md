# Architecture brief: story\-6361\-s03c\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Behaviour-free, test-side and script-side only: no production type, route, DTO, response, schema or forecast result changes, and no Forecasting type moves. Every rule reads the compiled IL through IlDependencies or the ArchUnitNET production architecture as today; inline lists stay two-way (InlineBaseline) and no rule is loosened.
```
Governed slices: S1, S2, S3

#### D2

```text
Shared rules owner: FlowMetricsModuleRules is renamed to ModuleRules (Architecture/ModuleRules.cs), module namespaces stay parameters. The two private Application shapes move into it: ApplicationIsReachedFromOutsideOnlyBy (from FlowMetricsInboundApplicationArchUnitTest, returns InboundVerdict) and OutboundEdges with the OutboundVerdict record (from FlowMetricsApplicationOutboundArchUnitTest, made internal). Flow metrics tests call the lifted methods with their own constants; their made-up-offender tests keep their meaning. Rename is a move-only commit, the lift an edit commit. No second rules class.
```
Governed slices: S1, S2

#### D3

```text
Made-up types that prove a widened Forecasting pattern live in the test assembly under namespaces the real widened pattern matches, i.e. below Lighthouse.Backend.Modules.Forecasting, with a last segment naming them as fixtures (for example Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastingFixtures). Deviation from the example namespace Lighthouse.Backend.Modules.ForecastingFixtures in the story design: the widened patterns end in (\.|$), so a type in Modules.ForecastingFixtures lies outside them and would prove only a rule shape with a substituted pattern, not the real constant. Rule-shape cases that pass their namespaces as parameters may keep the Modules.ForecastingFixtures namespaces the S02 way.
```
Governed slices: S2

#### D4

```text
Widening per rule (Ratchet design, Rules first): ModuleBoundariesArchUnitTest.ForecastingPattern gains |^Lighthouse\.Backend\.Modules\.Forecasting(\.|$) and Flow metrics outbound rule keeps reading it; SimulatedRunIsolationArchUnitTest uses an old-or-module namespace pattern; RealityCheckReadOnlyArchUnitTest matches the two policies by typeof and forbids ApplicationCoreRules.ApplicationCore minus the two policies; DependencyAwareForecastSeamArchUnitTest subject is the update components or Modules.Forecasting.Adapters.Background, exemption by typeof(ForecastUpdater); FastWithManyFeaturesArchUnitTest locates ForecastService.cs and ForecastUpdater.cs by file name under the production source as it does for ForecastLane.cs; RefinementModuleArchUnitTest forbidden background set adds Modules.Forecasting.Adapters. UpdateHoldsAreForecastOnly, ForecastFilterSeam and DomainEventHandlerOrderPin already key on typeof or short names and are left unchanged. RecurringBlackoutEventsSeam and StalenessPolicyPurity: the crafter checks whether a Forecasting type is in their subject or target set and widens to ApplicationCore only if so. Flow metrics full-name strings for Forecasting types are not touched here (feature B rewrites them).
```
Governed slices: S2

#### D5

```text
Safety net: a port-level seeded check under Tests/Acceptance/Forecasting runs ForecastSimulation and ForecastService (How Many and When) with DrawsFromAPinnedStartingNumber over ServicePortHarness and compares percentiles to literal values written in the test. A new Scripts/examine/bin/forecast-answers-compare, built like metrics-answers-compare (usage: forecast-answers-compare [base-revision [candidate-revision]]), captures the bodies of the GET forecast endpoints on seeded demo data with a fixed seed and diffs them; declared in .nwave/examine.json. The production-like database comparison is a maintainer-run step, not an EXAMINE expectation.
```
Governed slices: S3

#### D6

```text
Formal verification: no checker selected. No state, transition, protocol, concurrency or recovery behaviour changes. Rule claims are finite and decided exactly by the compiled IL with a made-up offender per rule as falsifier; the forecast-preservation claim compares code with itself, so a pinned seed with literal expected percentiles is the honest falsifier. Quint, TLA+/TLC and Agda would model nothing; no model written, no checker run or claimed. Liveness not applicable: synchronous rule evaluation and a one-shot capture have no progress property. Property-based testing not applicable: every law is a finite enumeration of rules, made-up types and named endpoints.
```
Governed slices: S1, S2, S3

#### D7

```text
Carriers, all test-side: rule verdicts are KernelRuleVerdict, InboundVerdict and OutboundVerdict owned by ModuleRules and KernelRules (Architecture folder); dependency facts are IlDependent from IlDependencies; per-fixture state is the lazily read Mono.Cecil ModuleDefinition disposed at one-time teardown; the seeded check holds ServicePortHarness per scenario; the compare script holds captured bodies in a temp directory. Layers: application rules only; no adapter, infrastructure or recovery layer changes.
```
Governed slices: S1, S2, S3

### Constraints

#### K1

```text
One oracle file per slice; a later slice never edits an earlier slice oracle or support files. No nWave scenario tags in code. Arch rule tests assert the rule failure names the offender, not counts. Comments carry no internal ids.
```
Governed slices: S1, S2, S3

#### K2

```text
Fixture types placed under Lighthouse.Backend.Modules in the test assembly must not reach the API, Data or EF Core, nor be domain-event handlers, because ApplicationCoreRulesReachModulesArchUnitTest runs the core rules over the test assembly with the Services-or-Modules pattern.
```
Governed slices: S2

#### K3

```text
Seeded check: FakeLighthouseClock, own database per scenario through ServicePortHarness, expected percentiles written out as literals; its mutation config names Tests.Acceptance.Forecasting.
```
Governed slices: S3

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
Renamed to ModuleRules (S1 target ModuleRules.cs); module namespaces already parameters; no second rules class
```

ApplicationIsReachedFromOutsideOnlyBy

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:157

Extend

```text
Lifted unchanged into ModuleRules
```

OutboundEdges

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:218

Extend

```text
Lifted with OutboundVerdict into ModuleRules
```

InboundVerdict

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:186

Reuse

```text
Verdict of the lifted inbound shape
```

metrics\-answers\-compare

Scripts\/examine\/bin\/metrics\-answers\-compare\:1

Reuse

```text
Template for forecast-answers-compare (S3 target, new: no forecast answer comparison exists)
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:1

Reuse

```text
Port harness for the seeded check
```

DrawsFromAPinnedStartingNumber

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestDoubles\/DrawsFromAPinnedStartingNumber\.cs\:1

Reuse

```text
Pinned seed for the draws
```

metrics\-answers\-compare

\.nwave\/examine\.json\:1

Extend

```text
Gains a forecast-answers-compare entry beside the metrics one
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes: rules and the compare script only observe the production assembly and existing HTTP responses, in one deployable and one commit range.
```

contract_tests

```text
No parties change apart: no wire, DTO or route changes; the compare script observes existing endpoints only.
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
Widened rules and shared ModuleRules
```

Safety

Pass

DesignedNotBuilt

```text
Each widened rule has a made-up offender in the new place that must be named by the rule failure; host executes per slice
```

##### Evidence

None.

```text
Widened rules and shared ModuleRules
```

Inhabitation

Pass

DesignedNotBuilt

```text
Compliant made-up twins and the real production assembly must pass each rule
```

##### Evidence

None.

```text
Seeded forecast check and forecast-answers-compare
```

PublicReachability

Pass

DesignedNotBuilt

```text
Seeded check drives ForecastService at the application-service port; compare script calls the real GET forecast endpoints on demo data
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: a widened pattern also matches the fixture namespace
```

```text
Each widened rule sees the new place
```

```text
none
```

```text
A rule green because the fixture sits outside the pattern
```

```text
Fixtures under Modules.Forecasting with a compliant twin each
```

ForecastingRulesSeeTheModuleArchUnitTest

#### Y2

Stressor

```text
Assumption negated: forecast draws are deterministic for a pinned seed
```

```text
Literal percentiles
```

```text
none
```

```text
A comparison of the code with itself
```

```text
Literal expected values; compare script pins the seed
```

SeededForecastsStayTheSameTest

#### Y3

SinglePointOfFailure

```text
Demo data only on the examiner host
```

```text
Answer equality on demo data
```

```text
Production-like comparison run by the maintainer
```

```text
Calling EXAMINE indeterminate for the missing DB_Backup
```

```text
Maintainer-run comparison on the production-like database
```

maintainer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Create

```text
Renamed from FlowMetricsModuleRules.cs plus the two lifted Application shapes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs

Extend

```text
Removed by the rename
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
Calls ModuleRules instead of its own private shape or FlowMetricsModuleRules
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
Calls ModuleRules instead of its own private shape or FlowMetricsModuleRules
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Extend

```text
Calls ModuleRules instead of its own private shape or FlowMetricsModuleRules
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

Extend

```text
Calls ModuleRules instead of its own private shape or FlowMetricsModuleRules
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ModuleRulesCatchANewOffenderArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetrics
```

#### Supports

None.

#### Public oracle

```text
Call ModuleRules.ApplicationIsReachedFromOutsideOnlyBy and ModuleRules outbound edges rule with a made-up module namespace other than Flow metrics over made-up types, and run the Flow metrics rule tests through ModuleRules
```

```text
The made-up newcomer, async-only caller and stale listed entry are each named in the failure; compliant made-up types pass; Flow metrics rules on the real assembly stay green
```

```text
ModuleRules does not exist or does not take the module namespaces as parameters, or a made-up offender in the other module passes
```

##### Promises

P1

#### Prefactoring

```text
Rename FlowMetricsModuleRules to ModuleRules (file and class), move-only commit, before lifting the two Application shapes; makes the lift and every later module rule land in one owner
```

```text
Every Architecture test stays green with the same examined types and offenders
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Changed

```text
Test project only; same rule, same made-up offenders, called through ModuleRules
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:1

Changed

```text
Test project only; same rule, same made-up offenders, called through ModuleRules
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:1

Changed

```text
Test project only; same rule, same made-up offenders, called through ModuleRules
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs\:1

Changed

```text
Test project only; same rule, same made-up offenders, called through ModuleRules
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Changed

```text
New shared owner of the module rules
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleRules\.cs\:14

Migrated

```text
Renamed to ModuleRules.cs; every consumer migrated in the same commit
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
```

reuse

```text
Reuse is decided in the shared rows: ModuleRules.cs is FlowMetricsModuleRules renamed and extended with ApplicationIsReachedFromOutsideOnlyBy and OutboundEdges; the four Flow metrics rule tests are Extended to call it
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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SimulatedRunIsolationArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RealityCheckReadOnlyArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs

Extend

```text
Forecasting namespace, folder or file key widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StalenessPolicyPurityArchUnitTest\.cs

Extend

```text
Its target ServicesPattern matches Services.Implementation.Forecast today, so a moved Forecasting type would leave its forbidden set; widened to also see Modules/Forecasting
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~ForecastingRulesSeeTheModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run each widened rule, with its real Forecasting pattern or key, over made-up types in the test assembly placed below Lighthouse.Backend.Modules.Forecasting, as subject and as target
```

```text
For each rule, the made-up offender in the new place is named in the rule failure and its compliant twin is not; for subject-exempt rules the made-up Forecasting type is treated as inside Forecasting
```

```text
Any listed rule keeps its old Services-only key, so the made-up type under Modules.Forecasting passes unseen or is wrongly flagged as an outsider
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
The S2 oracle reads two rule members by name: StalenessPolicyPurityArchUnitTest.ServicesPattern (its widened target pattern) and a new member RefinementModuleArchUnitTest.BackgroundUpdatesPattern (the background-updates key that rule widens). Both stay members of their owning rule class with those exact names.
```

##### Constraints

None.

##### Reuse

ModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

SimulatedRunIsolationArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SimulatedRunIsolationArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

RealityCheckReadOnlyArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RealityCheckReadOnlyArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

DependencyAwareForecastSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

FastWithManyFeaturesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

RefinementModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

ApplicationCore

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs\:20

Reuse

```text
Already Services or Modules; forbidden target for RealityCheckReadOnly
```

StalenessPolicyPurityArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StalenessPolicyPurityArchUnitTest\.cs\:16

Extend

```text
Existing owner of the rule; widened in place, no second rule
```

##### Impact

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SimulatedRunIsolationArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RealityCheckReadOnlyArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencyAwareForecastSeamArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:1

Changed

```text
Test project only; examines at least the types it examined before
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RecurringBlackoutEventsSeamArchUnitTest\.cs\:12

UnchangedCompatible

```text
Widened to ApplicationCore only if the crafter finds a Forecasting type in its subject or target set
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/UpdateHoldsAreForecastOnlyArchUnitTest\.cs\:32

UnchangedCompatible

```text
Keys on typeof; sees the type wherever it lives
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastFilterSeamArchUnitTest\.cs\:21

UnchangedCompatible

```text
Keys on typeof
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:247

UnchangedCompatible

```text
Reads ForecastingPattern by reflection, so widened through it
```

test\-architecture\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StalenessPolicyPurityArchUnitTest\.cs\:16

Changed

```text
Test project only; ServicesPattern widened so the forbidden set examines at least the types it examined before plus Modules/Forecasting
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Widening edits each rule in place; S1 already made the shared owner
```

architectural_tests

```text
The slice oracle is itself the architectural test over the compiled assembly
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

### S3

#### Targets

\.nwave\/examine\.json

Extend

```text
Declares forecast-answers-compare before the first EXAMINE
```

Scripts\/examine\/build\-examine\-commands

Extend

```text
Committed generator: writes Scripts/examine/bin/forecast-answers-compare and its C# driver forecast-answers-capture.cs beside metrics-answers-compare
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~SeededForecastsStayTheSameTest
bash Scripts/examine/bin/forecast-answers-compare HEAD
```

#### Supports

None.

#### Public oracle

```text
Through ServicePortHarness with FakeLighthouseClock and DrawsFromAPinnedStartingNumber, ask ForecastService for a How Many and a When forecast and run ForecastSimulation over fixed inputs
```

```text
The returned percentiles equal literal values written in the test
```

```text
A changed draw, percentile or input (for example a percentile index shifted by one) and the test stays green
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
Scripts/examine/bin/ is git-ignored and generated: the committed target is Scripts/examine/build-examine-commands, which writes forecast-answers-compare and its C# driver forecast-answers-capture.cs. The verification command still runs the generated bash Scripts/examine/bin/forecast-answers-compare HEAD after the generator has run.
```

##### Constraints

None.

##### Reuse

None.

##### Impact

examine\-config

\.nwave\/examine\.json\:1

Changed

```text
One added entry
```

examine\-script\-generator

Scripts\/examine\/build\-examine\-commands\:51

Changed

```text
Generator gains forecast-answers-compare and forecast-answers-capture.cs; existing generated commands unchanged; generated scripts read only
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Adds a test and a script; no existing code moves
```

architectural_tests

```text
No dependency or owner law is reachable from a test and a read-only script
```

reuse

```text
Reuse is decided in the shared rows: forecast-answers-compare is new, shaped like metrics-answers-compare; examine.json is Extended; ServicePortHarness and DrawsFromAPinnedStartingNumber are Reused
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

## ADRs

None.
