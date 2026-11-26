# Architecture brief: story\-6382\-s09a\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
No behaviour or schema change in feature A: only Architecture test files change; no entity, DbSet, migration, route, DTO, usage-data event or configuration key, and no production file is touched (D-0).
```

#### D2

```text
Contracts is the namespace a context publishes: Modules/<X>/Contracts, namespace equals folder, holds what other contexts may name; a facade per context, an inline type list and one shared contracts root were rejected (D-1).
```

#### D3

```text
Published set is a measured IL closure: seeds are X types another context reaches in the IL that are interfaces, enums, dependency-free records or structs, constant classes or X events; closure adds X types named in their public signatures, minus kernel Models and BCL; behaviour classes stay and their edges are listed with an owner (D-2).
```

#### D4

```text
ContextContractsMembership holds the measured published set: one entry per top-level type with context, old namespace, new namespace Modules.<X>.Contracts and admission Seed or Closure, carried as the existing MovingType record plus a closed ContractAdmission enum, owned by the Architecture test namespace; it is the measured projection of the future namespace, not a second owner (D-2, reuse of the Membership shape).
```

#### D5

```text
FrontDoor is Context scoped to a layer: a pattern that must see a moving type under old and new name is declared FrontDoor and widened permanently from Modules.X.Application to Modules.X.(Application|Contracts); a layer-location pattern is Place and must not see the new name; the declaration is added to PatternDeclaration in NamespaceMoveLaw beside Context and Place (D-7).
```

#### D6

```text
Move law runs over the contracts map: NamespaceMoveLaw is extended so one law call covers the ten possible Contracts places, every regex field of the test assembly is evaluated by IsMatch on old and new full names, alternations and interpolated readers included; earlier maps keep their verdicts (D-7).
```

#### D7

```text
One shared contracts rule over every pair: ContextContractsArchUnitTest holds R-C1 surface (Modules.A reaches Modules.B only through Modules.B.Contracts except the inline CrossContextEdgesOutsideContracts list), R-C2 contracts point inward only (kernel, BCL, Microsoft.Extensions abstractions, any Contracts), R-C3 enumerator (contexts read from the assembly equal the inline ten, both ways); direction stays with the per-module inbound rules (D-3).
```

#### D8

```text
Ratchet list lands complete in S2: R-C1 lists every measured cross-context edge by full type names Source -> Target with an owner comment, green on writing, compared both ways through InlineBaseline, only shrinks; feature C deletes entries per slice (D-8).
```

#### D9

```text
Kernel and platform lane stay legal targets: every Models type counts as kernel for R-C1 and R-C2; Lighthouse.Backend.Platform, Services, Data and API are not contexts and R-C1 does not govern edges into or out of them (D-4).
```

#### D10

```text
R-C3 enumerator is its own craft target: PackagedContexts reads every Lighthouse.Backend.Modules.<X> namespace root of the production IL; the oracle compares it with the inline ten names, so a new or vanished module root fails until named (gate predicate-needs-its-own-enumerator).
```

#### D11

```text
Refusals state what, why and how: each rule failure names the source, the target and its layer (what), that the target context may change it without telling anyone (why), and to name the type through Modules.B.Contracts, move a contract type there or react to an event (how), worded through ModuleWording (D-3).
```

#### D12

```text
Cross-layer algebra, test layer only: states are the IL type graph and the regex fields of the test assembly; operations are IsMatch on full names and edge enumeration; observations are the NUnit verdict per test and its failure text; no domain, application, adapter or infrastructure layer of production changes in feature A.
```

#### D13

```text
No formal model, laws run exhaustively on IL: No model for R-C1 to R-C3, the move law or the D-2 closure: they name no shared mutable state, no multi-step protocol, no concurrency and no crash window; they are relations over finite closed sets (compiled types, contexts, regex fields) evaluated exhaustively by NUnit; no checker selected, so no probe, install or waiver (D-16).
```

#### D14

```text
No property tests, finite domains only: every law ranges over the finite compiled assembly and the inline lists, so each is checked exhaustively as an example over the whole set; planted fixture types are the falsifiers (D-16, test-writing skill wins over nWave PBT density).
```

#### D15

```text
No liveness claim: feature A has no process that must eventually progress; every check is a synchronous evaluation over a compiled assembly.
```

#### D16

```text
Paradigm object oriented, preserved: the Architecture test namespace is OO C# (static rule owners, sealed records, verdict types); new types follow it: records for entries, a static owner for the membership and the enumerator, verdict records from ModuleRules.
```

#### D17

```text
Behaviour-free proof uses admitted probes only: architecture-rules (one line per test) observes every new and existing rule; no new probe; test names carry the observed facts (type, context, admission; field and role) so the probe output shows them (D-14).
```

### Constraints

#### K1

```text
Start condition is S1s first action: per Story 6365, 6366, 6468, 6657 the ADO state is Closed, the final rules commit is an ancestor of HEAD, and git ls-files shows Modules/Refinement, Modules/Reports, Modules/OrganisationSetup and no Services/Implementation/WriteBack*.cs; a failed check stops S1 and is reported with Story, check and output; results are recorded as craft commands.
```

#### K2

```text
Every count in the design was measured as text at 952040471 and is to be re-measured at A1 from the IL: the 38 edges, about 25 targets, the published-set table, the event owners of D-5 and the command list of D-6; a difference is recorded, not argued with.
```

#### K3

```text
Hypothesis threshold, fixed before S1: if after the published set is drawn the listed non-contract cross-context edges exceed 15, or one context pair carries more than 5, S1 stops and the finding goes to the ADR-270 owner as a wrong cut.
```

#### K4

```text
The contexts are exactly the ten of ADR-270 Decision 1: OrganisationSetup, TrackerIntegration, WorkItems, FlowMetrics, Forecasting, Promises, Refinement, Reports, Access, Licensing; the platform lane is not a context. Promises, Refinement and Reports are expected to publish nothing (no Contracts folder; open question 3 default).
```

#### K5

```text
No rule or list is loosened: every existing Architecture test keeps its verdict before and after each slice; widening a FrontDoor pattern admits only Contracts of the same context; the whole Architecture namespace runs before each close.
```

#### K6

```text
IL facts only: IlDependencies.TypesIn returns nested types, so holdings and exact sets compare top-level names (the part before +); the IL misses event declarations and named attribute arguments and XML-doc crefs produce no edge.
```

#### K7

```text
Lists inline by full type name, compared both ways (InlineBaseline), each R-C1 entry with its owner in a trailing comment; a rule exemption is exact per type and nested types, never a namespace or layer.
```

#### K8

```text
Planted offenders live in fixture namespaces such as Lighthouse.Backend.Modules.<X>Fixtures in the oracle file, checked against every existing lookalike guard; each rule asserts its subject set is not empty; each list position has a planted offender; rule tests assert by the failure naming the offender, never by counts.
```

#### K9

```text
Old-name sweep reads grouped alternations (Services\.(Implementation|Interfaces)\.(A|B) and Lighthouse\.Backend\.(X|Y)\.(A|B)) with a made-up file in each form; the sweep and any fix live in the slice oracle file, never in a shared support an approved oracle uses.
```

#### K10

```text
One oracle file per slice; an approved oracle or support file of this feature is never edited, not even a using; oracles name types as strings; a craft target is never an oracle support.
```

#### K11

```text
Test rules: the Lighthouse test-writing skill and its backend half win over nWave defaults (per-test timing, PBT density); no nWave scenario tags in code; layout oracles are IL architecture tests and start no host; default per-test budgets apply, no derogation, no infrastructure suite.
```

#### K12

```text
Commands are recorded without the flock prefix; every dotnet and pwsh run is made under flock -o on the scratchpad lock; long suites run as ctx_shell background jobs.
```

#### K13

```text
No stored or validated user input is involved: the inputs are the compiled assemblies and inline lists, with no range or size bound in the authority and none added by the design.
```

#### K14

```text
Earlier Stories module-boundary rule files are rule files, not frozen oracles of this feature: S1 edits only the FrontDoor field values in them (as Story 6657 S1 did), adding no rows and removing no list entries.
```

### Reuse

NamespaceMoveLaw

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

Extend

```text
Owns proving that rules keep seeing a moving type; extended with the contracts map (ten possible new places) and the FrontDoor declaration; earlier maps unchanged.
```

PatternDeclaration

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:31

Extend

```text
Owns the role of a rule pattern; FrontDoor added beside Context and Place as the Context role scoped to a layer.
```

MovingType

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:41

Reuse

```text
Already carries name, area, old and new namespace; used for each membership entry with the context as area.
```

ApplicationIsReachedFromOutsideOnlyBy

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:87

Reuse

```text
Owns per-module direction; only its application argument values widen; no second direction owner.
```

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
Owns shared edge rules with verdicts; gains ReachesOtherContextsOnlyThroughContracts for R-C1 and R-C2, since no per-pair form exists (OutboundEdges is per module and target).
```

OutboundEdges

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:140

Reuse

```text
Shape and listed-edge comparison reused by the new helper; not changed.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
The only IL reader (generic arguments, attributes, catch types, GetRequiredService); all measurements come from it.
```

AssertMatches

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InlineBaseline\.cs\:11

Reuse

```text
Owns both-ways inline list comparison (ADR-268) for the membership and the R-C1 list.
```

ModuleWording

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleWording\.cs\:7

Reuse

```text
Owns per-context refusal wording.
```

WriteBackMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembership\.cs\:12

Reuse

```text
Shape of a membership map (static owner of inline entries) copied for ContextContractsMembership; the type itself is not changed.
```

WriteBackPatternRoles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackPatternRoles\.cs\:8

Reuse

```text
Shape of a pattern-role map keyed by class and field copied for ContextContractsPatternRoles.
```

ModuleRulesCatchANewOffenderArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:27

Reuse

```text
Planted-offender falsifier style reused for R-C1 to R-C3.
```

LighthouseArchitecture

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LighthouseArchitecture\.cs\:7

Reuse

```text
Loads the production assembly once per test assembly (shared read-only world).
```

ContextContractsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembership\.cs\:1

CreateNew

```text
No type owns what a context publishes: no Contracts namespace exists yet and the existing Membership maps each describe one move, not the published set of ten contexts.
```

ContextContractsArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:1

CreateNew

```text
No rule governs edges between every pair of contexts; per-module rules govern direction only.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes: only test-assembly rule code changes and production bytes are untouched (D-0).
```

contract_tests

```text
Single deployable; producers and consumers of every touched type change in the same commit and no persisted or wire data outlives a version.
```

### Boundaries

```text
Compiled production and test assemblies read through IlDependencies and LighthouseArchitecture.Production by NUnit architecture tests (in-process; nearest argv: dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture)
```

#### Driven ports

```text
None at runtime: production is unchanged; the tests read the IL with Mono.Cecil and the test project source text for the literal sweep
```

```text
Architecture tests depend on the production assembly; production depends on nothing new; the new rule and membership types depend on IlDependencies, InlineBaseline, ModuleRules, ModuleWording and NamespaceMoveLaw only
```

#### Failures

##### B1

```text
A cross-context edge outside the target Contracts that is not listed
```

Refusal

```text
R-C1 fails naming source, target and layer with WHAT, WHY, HOW
```

##### B2

```text
A listed R-C1 edge no longer in the IL
```

Refusal

```text
InlineBaseline fails naming the stale entry and asks to delete it
```

##### B3

```text
The IL cannot be loaded or a rule subject set is empty
```

Indeterminate

```text
The test fails at set-up or on the not-empty assertion; never a pass
```

### Observations

```text
Per architecture test: its name and outcome as printed by the architecture-rules probe and dotnet test (passed, failed, setup failed)
```

NUnit test outcome line

```text
Why a passing rule passes over an empty subject set; therefore each rule asserts its subject set is not empty
```

```text
Rule failure message naming each offender: source type, target type and layer, or field, type and role break, with WHAT, WHY and HOW
```

NUnit assertion message

```text
Edges the IL does not carry (event declarations, named attribute arguments, reflection, string type names)
```

### Equality

membership and lists

```text
Two published sets are equal when their top-level full names, contexts and admissions agree as sets; two edge lists are equal when their Source -> Target full-name strings agree as sets; order and comments do not count
```

### Constructors

ContextContractsMembership\.Entries

IReadOnlyList\<\(MovingType Type\, ContractAdmission Admission\)\>

PatternDeclaration\.FrontDoor

PatternDeclaration FrontDoor\(PatternSubject subject\, string reason\)

ModuleRules\.ReachesOtherContextsOnlyThroughContracts

OutboundVerdict ReachesOtherContextsOnlyThroughContracts\(ModuleDefinition module\, IReadOnlyCollection\<string\> listed\, string modules\, string contracts\, string kernel\, ModuleWording\? wording \= null\)

PackagedContexts\.InTheAssembly

IReadOnlyList\<string\> InTheAssembly\(ModuleDefinition module\)

### Laws

#### L1

```text
Every legal edge between two contexts goes through the target Contracts or is listed: the set of Modules.A to Modules.B edges (A not B) whose target is outside Modules.B.Contracts equals CrossContextEdgesOutsideContracts
```

Invariant

Adapter

ContextContractsArchUnitTest

##### Treatment

Finite

###### Cases

```text
Production IL: equal to the inline list, both ways
```

```text
Planted Modules.<X>Fixtures type naming another fixture context Domain type: named as offender
```

```text
Each list position removed: that edge named as unlisted
```

```text
Finite closed set: every compiled type and every list entry is evaluated
```

##### Evidence

None.

#### L2

```text
Contracts point inward only: no type in Modules.X.Contracts reaches its own Domain, Application or Adapters, Data, API, Services or the platform lane
```

Invariant

Adapter

ContextContractsArchUnitTest

##### Treatment

Finite

###### Cases

```text
Production IL: no offender
```

```text
Planted Contracts type naming its own Application: named as offender
```

```text
Finite closed set evaluated exhaustively
```

##### Evidence

None.

#### L3

```text
The contexts read from the assembly equal the inline ten, both ways
```

Invariant

Adapter

ContextContractsArchUnitTest

##### Treatment

Finite

###### Cases

```text
Enumerator returning nothing: all ten named missing
```

```text
Production IL: equal
```

```text
A planted extra module root: named as unknown
```

```text
Finite closed set
```

##### Evidence

None.

#### L4

```text
Each membership entry exists as a top-level type in exactly its old or its new namespace, and the membership equals the D-2 closure computed from the IL (seeds by criterion plus public-signature closure, minus kernel and BCL)
```

Invariant

Adapter

ContextContractsMembershipArchUnitTest

##### Treatment

Finite

###### Cases

```text
Production IL: every entry found and the closure equal both ways
```

```text
A listed seed that is a behaviour class: named
```

```text
A closure member missing from the map: named
```

```text
Finite closed set
```

##### Evidence

None.

#### L5

```text
For every regex field of the test assembly and every membership type: a FrontDoor field matches old and new full name, a Place field does not match the new name, and every field matching a moving type is declared
```

Refinement

Adapter

ContextContractsMembershipArchUnitTest

##### Treatment

Finite

###### Cases

```text
Before widening: each Modules\.X\.Application(\.|$) FrontDoor field named as StopsSeeingTheType
```

```text
After widening to (Application|Contracts): no break
```

```text
An undeclared field seeing a moving type: named
```

```text
Finite closed set of fields times types
```

##### Evidence

None.

#### L6

```text
No string literal in the test project names a moving type by its old full name, grouped alternations included, unless the fixed rename list renames it
```

Invariant

Adapter

ContextContractsMembershipArchUnitTest

##### Treatment

Finite

###### Cases

```text
A made-up file with a flat old name: named
```

```text
A made-up file with a grouped alternation naming the old name: named
```

```text
Finite set of files and literals
```

##### Evidence

None.

#### L7

```text
Every existing architecture rule keeps its verdict across each slice
```

Invariant

Adapter

ArchitectureNamespace

##### Treatment

Finite

###### Cases

```text
dotnet test filtered to Lighthouse.Backend.Tests.Architecture green before and after each slice
```

```text
Finite suite
```

##### Evidence

None.

### Refuted

```text
A text search over using lines and type names finds every cross-context edge
```

```text
Text sees crefs and comments and misses same-namespace and fully qualified references
```

```text
DemoBlockedHistoryBackfillHandler names WorkItemBlockedTransitionCaptureHandler only in a see cref, which yields no IL edge
```

```text
A type moved from Modules.X.Application to Modules.X.Contracts stays under its module rules
```

```text
Every inbound rule keys on a layer pattern Modules\.X\.Application(\.|$)
```

```text
ILicenseService moved to Modules.Licensing.Contracts is no longer matched by LicensingApplication, so a new caller would pass unseen
```

### Claims

#### C1

```text
No architecture rule silently stops seeing a type when it later moves into its context Contracts
```

```text
A rule passes over an empty subject set and a forbidden caller of a published type ships unseen
```

ContextContractsMembershipArchUnitTest

```text
Compiled test assembly regex fields, ContextContractsMembership and ContextContractsPatternRoles
```

PatternDeclaration\.FrontDoor plus NamespaceMoveLaw over the contracts map

Every per\-module inbound rule and placement rule reading a declared field

```text
dotnet test --filter FullyQualifiedName~ContextContractsMembershipArchUnitTest fails naming the field, the type and the role break
```

```text
Patterns built at run time or held in properties; rules selecting by short name; inline method-body literals (only the sweep sees them)
```

#### C2

```text
No new cross-context edge outside the target Contracts ships unseen, and no context appears or vanishes without being named
```

```text
Contexts couple to each other internals and features B to D move types under a rule that does not bite
```

ContextContractsArchUnitTest

```text
Compiled production IL and the inline lists
```

ModuleRules\.ReachesOtherContextsOnlyThroughContracts plus PackagedContexts\.InTheAssembly

R\-C1\, R\-C2\, R\-C3 tests and feature C ratchet slices

```text
dotnet test --filter FullyQualifiedName~ContextContractsArchUnitTest fails naming source, target and layer, or the missing or unknown context
```

```text
Code in Services, Data, API and the platform lane is outside R-C1; IL-invisible edges (event declarations, named attribute arguments, reflection)
```

### States

Admitted

```text
An edge from Modules.A to a type in Modules.B.Contracts, to the kernel, the BCL or the platform lane
```

ContextContractsArchUnitTest

Admitted

```text
An edge from Modules.A to a Modules.B type outside Contracts that is on CrossContextEdgesOutsideContracts
```

ContextContractsArchUnitTest

Excluded

```text
An unlisted edge from Modules.A to a Modules.B type outside Contracts
```

ContextContractsArchUnitTest

Excluded

```text
A Contracts type reaching its own Domain, Application or Adapters, or Data, API, Services or the platform lane
```

ContextContractsArchUnitTest

Excluded

```text
A Lighthouse.Backend.Modules root not on the inline ten, or one of the ten missing
```

ContextContractsArchUnitTest

Excluded

```text
A FrontDoor field that stops seeing a published type, or a Place field that sees its new name, or an undeclared field that sees it
```

ContextContractsMembershipArchUnitTest

Unknown

```text
Edges carried only by event declarations, named attribute arguments, reflection or type names in strings
```

IlDependencies

### Models

None.

### Closure

```text
contracts rules and move law
```

Safety

Pass

DesignedNotBuilt

```text
Excluded states are refused by R-C1 to R-C3 and the move law, each with a planted offender per rule and list position; not built yet
```

##### Evidence

None.

```text
contracts rules and move law
```

Inhabitation

Pass

DesignedNotBuilt

```text
Every listed type exists today in its old namespace (re-measured in S1); the ten module roots exist on main (git ls-files); the R-C1 list is built from the measured edges
```

##### Evidence

None.

```text
contracts rules and move law
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly -> ContextContractsMembership and PackagedContexts -> NamespaceMoveLaw and ModuleRules helper -> NUnit test outcome through dotnet test and the architecture-rules probe
```

##### Evidence

None.

```text
contracts rules and move law
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: synchronous evaluation over a compiled assembly, nothing must eventually happen
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negates: moving a type keeps it under its module rules
```

```text
Every per-module rule keeps seeing a published type
```

```text
FrontDoor fields widen to Application|Contracts permanently
```

```text
A field silently stops matching a moved type
```

```text
Fields built at run time or held in properties are not read by the law
```

ContextContractsMembershipArchUnitTest

#### Y2

Stressor

```text
Negates: a text measure finds every edge
```

```text
Edges and the published set come from the IL only
```

```text
Design counts differ from S1 counts
```

```text
A text-only edge listed, or an IL edge missed by the list
```

```text
IL-invisible edges (event declarations, named attribute arguments)
```

IlDependencies

#### Y3

Stressor

```text
Negates: the earlier Stories left the counts as measured at 952040471
```

```text
The hypothesis threshold is fixed before measuring
```

```text
Counts change and are recorded
```

```text
The threshold moved after measuring
```

```text
None
```

ContextContractsMembershipArchUnitTest

#### Y4

Bypass

```text
Code outside Modules (Services, Data, API, platform lane) and reflection or string type names
```

```text
Per-module inbound rules keep governing outsiders
```

```text
R-C1 does not see them
```

```text
Claiming R-C1 covers them
```

```text
Kernel width (S04b) and Services code (S05, S10) remain
```

Epic 6510

#### Y5

SinglePointOfFailure

```text
IlDependencies is the one IL reader for every rule
```

```text
All rules read the same edge set
```

```text
A reader defect affects every rule equally
```

```text
A reader change that drops edges silently
```

```text
Each rule asserts its subject set is not empty and plants an offender
```

IlDependencies

#### Y6

Stressor

```text
Second set, walkthrough not execution: a later Story adds a module folder without naming it; a Contracts interface signature drags an Application record; a nested type +Inner shares a short name; a widened FrontDoor field also matches another context Contracts
```

```text
R-C3 fails until named; R-C2 fails; top-level reduction; widening is per context so other contexts Contracts stay unmatched
```

```text
One more list edit per new context
```

```text
A green rule over a silently unmatched type
```

```text
None found by the walkthrough
```

ContextContractsArchUnitTest

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

Extend

```text
Widen each field the law declares FrontDoor from Modules\.X\.Application to Modules\.X\.(Application|Contracts), same context only; no row, list entry or planted type changes.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementUnitTestsBesideTheModuleArchUnitTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs

Extend

```text
Admit Contracts in the layer set of the placement rule (a Place field, widened by layer list, not narrowed); per-class rows unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ContextContractsMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembership\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsPatternRoles\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsStringsTheMoveRenames\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs

None.

None.

#### Public oracle

```text
Run ContextContractsMembershipArchUnitTest: (1) each ContextContractsMembership entry exists as a top-level type in its old or its new namespace; (2) the membership equals the D-2 closure computed from the production IL, both ways; (3) NamespaceMoveLaw with the contracts map and ContextContractsPatternRoles over every regex field of the test assembly; (4) the literal sweep with its fixed rename list, grouped alternations included; then the whole Architecture namespace
```

```text
RED today on (3): every FrontDoor field Modules\.X\.Application(\.|$) stops seeing X published types under Modules.X.Contracts. GREEN once those fields read Modules\.X\.(Application|Contracts); (1), (2), (4) and every existing Architecture test green before and after; the probe lists each entry with context, namespaces and admission, and each field with its role
```

```text
A listed seed that is a behaviour class, a closure member missing from the map, an undeclared field seeing a moving type, a FrontDoor field not widened, a Place field matching a new name, or a literal (flat or grouped) naming an old full name not on the rename list each fails naming the entry, field or file
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
S1 RED is the move law, not a stub: before widening, the law over the contracts map names every FrontDoor field Modules\.X\.Application(\.|$) as StopsSeeingTheType for X published types; the crafter widens the fields to GREEN.
```

```text
Oracle-side files written with the oracle: ContextContractsMembership, ContextContractsPatternRoles, ContextContractsStringsTheMoveRenames and the NamespaceMoveLaw extension are authored by the acceptance designer before RED and frozen at approval; the S1 oracle and supports do not name ModuleRules.
```

```text
Membership and roles visible in probe output: one test case per membership entry (type, context, old and new namespace, Seed or Closure) and one per classified field (field, FrontDoor or Place), so architecture-rules lists them.
```

##### Constraints

```text
Start-condition checks, the re-measured counts (edges, targets, published set, D-5 event owners, D-6 command list from the IL) and the DoR-3 threshold result are recorded as S1 craft commands with their output; the commit check for 6365 and 6468 finds no commit by --grep="story <id>" (their commits use scope names), so the host decides the equivalent check.
```

##### Reuse

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

FlowMetricsInboundApplicationArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:21

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:43

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

LicensingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

PromisesModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

RefinementModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:41

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

ReportsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:42

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

TrackerIntegrationModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:29

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:35

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

ConnectorsDeclareNoUnsupportedMembersArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:17

Extend

```text
Owns this module rule and its FrontDoor field; extended in place by widening the field value; no second rule owner.
```

FlowMetricsUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsUnitTestPlacementTest\.cs\:15

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

ForecastingUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:16

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

OrganisationSetupUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupUnitTestPlacementTest\.cs\:10

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

PromisesUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:17

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

TrackerIntegrationUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:18

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

WorkItemsUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs\:17

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

RefinementUnitTestsBesideTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementUnitTestsBesideTheModuleArchUnitTest\.cs\:17

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

ReportsUnitTestsBesideTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs\:13

Extend

```text
Owns this module unit-test placement rule; extended in place by admitting Contracts in its layer set.
```

##### Impact

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:1

Changed

```text
FrontDoor field values widen; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsUnitTestPlacementTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementUnitTestsBesideTheModuleArchUnitTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs\:1

Changed

```text
Layer set admits Contracts; verdicts unchanged.
```

architecture test consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier move laws read every regex field; a widened FrontDoor value keeps their declared roles; confirmed by the whole Architecture run.
```

architecture test consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier move laws read every regex field; a widened FrontDoor value keeps their declared roles; confirmed by the whole Architecture run.
```

architecture test consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier move laws read every regex field; a widened FrontDoor value keeps their declared roles; confirmed by the whole Architecture run.
```

architecture test consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier move laws read every regex field; a widened FrontDoor value keeps their declared roles; confirmed by the whole Architecture run.
```

architecture test consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Earlier move laws read every regex field; a widened FrontDoor value keeps their declared roles; confirmed by the whole Architecture run.
```

architecture test support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/NamespaceMoveLaw\.cs\:105

UnchangedCompatible

```text
Extended by the acceptance designer with FrontDoor and the contracts map before RED; earlier maps keep their results.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
AccessModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
FlowMetricsInboundApplicationArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
ForecastingModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
LicensingModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
PromisesModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
RefinementModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
ReportsModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
TrackerIntegrationModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
WorkItemsModuleBoundariesArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
ConnectorsDeclareNoUnsupportedMembersArchUnitTest.cs holds its rule test class plus planted fixture types in fixture namespaces, deliberately beside the rule that plants them; S1 changes only field values, so no type moves in this slice.
```

prefactoring

```text
FlowMetricsUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
ForecastingUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
OrganisationSetupUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
PromisesUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
TrackerIntegrationUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
WorkItemsUnitTestPlacementTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
RefinementUnitTestsBesideTheModuleArchUnitTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
```

prefactoring

```text
ReportsUnitTestsBesideTheModuleArchUnitTest.cs holds its placement test class and at most its own planted twins; S1 only admits a layer name, so no type moves in this slice.
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
Every rule pattern that sees a published type keeps its role when the type moves into its context Contracts; the published set equals the IL closure; no literal names a moving type by its old name
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ContextContractsMembershipArchUnitTest
```

###### T2

```text
Every existing architecture rule keeps its verdict
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PackagedContexts\.cs

Create

```text
R-C3 enumerator: every Lighthouse.Backend.Modules.<X> namespace root of the production IL, top-level, sorted.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ContextContractsArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

None.

None.

#### Public oracle

```text
Run ContextContractsArchUnitTest over the production IL and planted Modules.<X>Fixtures types: R-C1 with CrossContextEdgesOutsideContracts, R-C2, R-C3 with the inline ten and PackagedContexts, plus one planted offender per rule and per list position; then the whole Architecture namespace
```

```text
RED today on R-C3: the enumerator returns nothing, so the ten contexts are named missing. GREEN once PackagedContexts reads the module roots from the IL; R-C1 (every measured edge listed) and R-C2 (empty) green on writing; each planted offender fails its rule naming source, target and layer or the context; every existing Architecture test stays green
```

```text
A planted type in one fixture context naming another fixture context Domain type, a planted Contracts type naming its own Application, a planted extra module root, a removed list entry, or a stale list entry each fails naming the offender; an enumerator that returns nothing or a fixed list fails R-C3
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
R-C3 RED against an empty enumerator: the acceptance designer creates PackagedContexts.InTheAssembly as a declaration returning no contexts so the oracle compiles and R-C3 fails by assertion naming the ten missing contexts; the crafter makes it read the IL (design A2). R-C1 and R-C2 are green on writing through the ModuleRules helper the acceptance designer adds.
```

```text
R-C1 baseline is every measured edge: at S2 no Contracts exists, so CrossContextEdgesOutsideContracts lists every module-to-module edge in the IL by full names Source -> Target with an owner comment; R-C2 baseline is empty.
```

##### Constraints

None.

##### Reuse

PackagedContexts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PackagedContexts\.cs\:1

CreateNew

```text
No enumerator of module roots exists; R-C3 needs its own enumerator beside the predicate.
```

##### Impact

architecture test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PackagedContexts\.cs\:1

Changed

```text
New enumerator read only by R-C3.
```

architecture test support

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

UnchangedCompatible

```text
Additive helper ReachesOtherContextsOnlyThroughContracts written with the oracle; existing members and their callers unchanged.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
PackagedContexts.cs is a new file holding one type; nothing moves.
```

prefactoring

```text
ModuleRules.cs holds ModuleRules and its two verdict records InboundVerdict and OutboundVerdict and is extended additively by the acceptance designer, not by craft; splitting the verdict records would touch every approved rule oracle that names them and is outside this slice.
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
R-C1: a type in Modules.A reaches Modules.B (B not A) only through Modules.B.Contracts or a listed edge; R-C2: Contracts reach only kernel, BCL, Microsoft.Extensions abstractions and Contracts; R-C3: contexts read from the assembly equal the inline ten
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ContextContractsArchUnitTest
```

###### T4

```text
Every existing architecture rule keeps its verdict
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

##### Contract tests

None.

## ADRs

None.
