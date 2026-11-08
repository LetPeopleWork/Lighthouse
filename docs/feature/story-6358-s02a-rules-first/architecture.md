# Architecture brief: story\-6358\-s02a\-rules\-first

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ArchivedDeliveryReadPathArchUnitTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventDispatcherSeamArchUnitTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Run each application-core rule (ModuleBoundariesArchUnitTest ServiceLayerPattern and ParentPlacementKernel, KernelRules.TheServices, ArchivedDeliveryReadPathArchUnitTest ServiceNamespacePattern, DomainEventDispatcherSeamArchUnitTest handler rules) over made-up offender types placed under Lighthouse.Backend.Modules
```

```text
Each rule reports the Modules offender as it reports the same offender under Lighthouse.Backend.Services; domain-event handlers are found by implementing IDomainEventHandler<>, not by namespace; each rule's examined set on the real assembly does not shrink
```

```text
A rule passes while a made-up offender sits under Lighthouse.Backend.Modules, or a handler outside .Services.Implementation.DomainEvents escapes the handler rules
```

##### Promises

P1

P3

#### Prefactoring

```text
Lift the five application-core rules (core-not-on-API, parent-placement kernel, archived read path, handler service-locator, handler awaits saves) into Architecture/ApplicationCoreRules.cs, KernelRules shape, read through IlDependencies, subjects as parameters, and make the four existing rule test classes call them on the real production module; defaults stay Services-only and namespace-fragment handler selection
```

```text
Every existing Architecture test stays green with the same examined types; no production file changes
```

Lighthouse\.Backend\.Tests\.Architecture namespace

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Records

##### Decisions

```text
Rule surface for the application-core rules (declared by the design owner after the acceptance designer's BLOCKED-BY design, 2026-11-08).

New file Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ApplicationCoreRules.cs: `internal static class ApplicationCoreRules`, in the KernelRules shape. Every rule takes a Mono.Cecil `ModuleDefinition`, reads dependencies through `IlDependencies` (not ArchUnitNET), and returns `KernelRuleVerdict` (Examined, Offenders, Rule) whose `Check()` the real-assembly tests call.

Constants:
- `ApplicationCoreRules.ApplicationCore = @"^Lighthouse\.Backend\.(Services|Modules)(\.|$)"` (introduced by the implementation phase, not the prefactoring).
- The API, Data and other forbidden-target patterns the current rules use, lifted as constants of the same class.

Rules (subject types that are fixed today by full name become parameters, so a made-up type can stand in):
1. `CoreDoesNotDependOnApi(ModuleDefinition module, string core = <core pattern>)` — from ModuleBoundariesArchUnitTest's service-layer-does-not-depend-on-API rule.
2. `ParentPlacementKernelDependsOnNoCoreOrData(ModuleDefinition module, IReadOnlyCollection<string> kernelTypes, string core = <core pattern>)` — from ModuleBoundariesArchUnitTest's ParentPlacementKernel rule; the real test passes today's four kernel type names.
3. `ArchivedReadPathReachesNoRecalculation(ModuleDefinition module, IReadOnlyCollection<string> projectionTypes, string core = <core pattern>)` — from ArchivedDeliveryReadPathArchUnitTest; the real test passes today's two projection type names.
4. `HandlersUseNoServiceLocator(ModuleDefinition module)` and 5. `HandlersAwaitRepositorySaves(ModuleDefinition module)` — from DomainEventDispatcherSeamArchUnitTest. They select the handlers themselves.
6. `KernelRules.ModelsDoNotDependOnServices` stays where it is; only its default `services` argument changes in the implementation phase.

Prefactoring (crafter, phase before-move): create ApplicationCoreRules with rules 1 to 5 and make the four existing test classes call them on the real production module, with behaviour preserved: the core pattern default is `KernelRules.TheServices`, and the handler rules select handlers by the `.Services.Implementation.DomainEvents` namespace fragment, exactly as today. Moving rules 1 to 3 from ArchUnitNET NotDependOnAny to IlDependencies may reveal real offenders in async bodies that ArchUnitNET missed in Release: if it does, do not baseline them, reject and report them (they are a finding for the maintainer). Preserved: every existing Architecture test stays green and no production file changes.

Implementation (crafter, after the oracle is approved and its RED observed): the core-pattern defaults become `ApplicationCore` (rules 1 to 3 and KernelRules.ModelsDoNotDependOnServices), and rules 4 and 5 select handlers as every non-abstract type implementing `IDomainEventHandler<>`, wherever its namespace. Six real handlers outside `.Services.Implementation.DomainEvents` (ReconstructionMemo, and the five trigger handlers in Services.Implementation.BackgroundServices.Update) come under rules 4 and 5 for the first time; any of them that breaks a rule goes on an inline two-way baseline in that rule, named one per line with the reason, and is reported for the gate lessons. No rule is loosened otherwise.

"Examined set does not shrink": on the real production module, each rule's Examined with the widened default is a superset of its Examined when called with `KernelRules.TheServices` (rules 1 to 3) or with today's namespace-fragment selection (rules 4 and 5, exposed as an explicit selector overload or parameter the crafter adds in the prefactoring, e.g. `HandlersUseNoServiceLocator(module, HandlerSelection.ByDomainEventsNamespace)`). The oracle asserts that superset on the real module, and asserts on a fixture module that a made-up offender under `Lighthouse.Backend.Modules.*` (made-up handler implementing IDomainEventHandler<> outside a DomainEvents namespace, for rules 4 and 5) is reported by the default call, just as the same offender under `Lighthouse.Backend.Services.*` is.

Order: prefactoring crafter, then acceptance designer (oracle against this surface, semantic RED = the Modules offenders are not reported), oracle approval, crafter implementation.
```

##### Constraints

None.

##### Reuse

ApplicationCoreRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs

CreateNew

```text
KernelRules is specific to Models; the five application-core rules need the same rule-defined-once shape with subject and pattern parameters so made-up types can be checked, and later module Stories reuse it
```

ModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:11

Extend

```text
Its rule(s) move into ApplicationCoreRules (shared, parameterised) and it calls them on the real production module; its test names stay
```

KernelRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs\:14

Extend

```text
Its rule(s) move into ApplicationCoreRules (shared, parameterised) and it calls them on the real production module; its test names stay
```

ArchivedDeliveryReadPathArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ArchivedDeliveryReadPathArchUnitTest\.cs\:8

Extend

```text
Its rule(s) move into ApplicationCoreRules (shared, parameterised) and it calls them on the real production module; its test names stay
```

DomainEventDispatcherSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventDispatcherSeamArchUnitTest\.cs\:11

Extend

```text
Its rule(s) move into ApplicationCoreRules (shared, parameterised) and it calls them on the real production module; its test names stay
```

##### Impact

test\-only architecture rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRules\.cs

Changed

```text
Test project only; production code and its behaviour are unchanged; the rule examines at least the types it examined before
```

test\-only architecture rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Changed

```text
Test project only; production code and its behaviour are unchanged; the rule examines at least the types it examined before
```

test\-only architecture rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelRules\.cs

Changed

```text
Test project only; production code and its behaviour are unchanged; the rule examines at least the types it examined before
```

test\-only architecture rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ArchivedDeliveryReadPathArchUnitTest\.cs

Changed

```text
Test project only; production code and its behaviour are unchanged; the rule examines at least the types it examined before
```

test\-only architecture rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventDispatcherSeamArchUnitTest\.cs

Changed

```text
Test project only; production code and its behaviour are unchanged; the rule examines at least the types it examined before
```

##### Agreement

Architecture rules over the production assembly

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

Consumer

UnchangedCompatible

```text
The rules read the compiled production assembly; no production contract changes
```

##### Not applicable

contract_tests

```text
No wire, DTO, route or cross-process contract is touched; the slice changes test-project rules only
```

architectural_tests

```text
The slice's oracle is itself the architectural test (rules over the compiled assembly); no further architectural test is designed
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

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.DomainEventHandlerOrderPinTest
```

#### Supports

None.

#### Public oracle

```text
Build the production service provider as Program.cs registers it and resolve IEnumerable<IDomainEventHandler<TeamDataRefreshed>> and IEnumerable<IDomainEventHandler<PortfolioFeaturesRefreshed>>
```

```text
The handler types come back in today's registration order, written out in the test as two literal lists (PortfolioFeaturesRefreshedMetricsInvalidationHandler before the three recorders; ReconstructionMemo and TeamDataRefreshedForecastTriggerHandler at their current positions)
```

```text
Two registrations in Program.cs are swapped and the pin still passes
```

##### Promises

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

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
The order pin adds one test and changes no code
```

reuse

```text
No component is reused or changed; one new test
```

impact

```text
No code target
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
