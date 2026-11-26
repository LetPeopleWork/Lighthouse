# Architecture brief: story\-6382\-s09a\-b\-module\-start\-up

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Module start-up holder (D-9, option a): one file Modules/<X>/<X>Module.cs per context, public static class <X>Module in namespace Lighthouse.Backend.Modules.<X>, with Add<X>(this IServiceCollection services, IConfiguration configuration) for FlowMetrics, Forecasting, Promises, Licensing, Access (S1) and WorkItems, TrackerIntegration, Refinement, Reports, OrganisationSetup (S2). No such holder exists today: the only extension-method registrar in the backend is Platform/UsageData AddUsageDataPublishing, which is not a context. Lines move verbatim, in today relative order, comments with them. A static extension class breaks calisthenics rule 10 (stateless class); composition code is exempt, and it is the .NET registration idiom. A context whose moved lines read no configuration still takes the parameter only if the build stays clean; otherwise it takes IServiceCollection alone, stated in the craft record.
```

#### D2

```text
Call sites: every Add<X> is called from inside Program.RegisterServices(WebApplicationBuilder), which keeps its name and signature because the order pin reflects on it. Each call sits where the context relative order inside every multi-registered service type is kept: IHostedService (today OverTimeHistoryFiller, TeamUpdater, PortfolioUpdater, ConnectionHealthProber, the two usage-data services), ISeeder (ForecastInputsForgottenAtStartSeeder is seventh of nine), IReportMetric and every other service type registered more than once. A registration whose order no call position can keep stays in Program and goes on the composition-root rule allowed list, exactly as the handlers do; measured at B1 and B2.
```

#### D3

```text
Handler block: every IDomainEventHandler<> registration, including the two ReconstructionMemo forwards and its singleton, stays in Program in one method Program.RegisterDomainEventHandlers(IServiceCollection), ordered by event across contexts, called from RegisterServices. S1 leaves handler lines where they are; S2 gathers them (today lines 1424-1426 and 1632-1657 at 49552e47a) into that method without reordering any line within an event. RegisterReports folds into ReportsModule.AddReports in S2, and Main stops calling it.
```

#### D4

```text
Preservation pin carrier (D-9, E9): StartUpRegistrationsPreservationTest in Tests/Architecture, a craft target, not an oracle or support: nwdes freezes oracle and support bytes at approval, and S2 must change its seam line (RegisterReports gone) and S3 deletes it. The S1 crafter writes it as the first commit on the pre-B1 revision, baseline captured there and observed green before any line moves; the slice and candidate reviews judge it. Seam: WebApplication.CreateBuilder with EnvironmentName Testing, then RegisterServices(builder) and RegisterReports(builder.Services) by reflection as Main calls them; only the descriptors those calls add (the suffix after the builder own) are projected. Projection per service type: the ordered list of (lifetime, service key, implementation), where implementation is ImplementationType, else the instance type, else for a factory the delegate declared return type plus the ordered service types it asks of a recording IServiceProvider that answers nothing (the closure class and method name are left out: they change when a line moves). The baseline is written out inline by full type name, never computed at test time.
```

#### D5

```text
Composition-root rule, one oracle file per slice (nwdes approval freezes each slice bytes, so no slice edits an earlier one): S1 CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest lists FlowMetrics, Forecasting, Promises, Licensing, Access; S2 CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest lists WorkItems, TrackerIntegration, Refinement, Reports, OrganisationSetup; together they are the ten-context rule. Each asserts, per listed context: its <X>Module exists in Lighthouse.Backend.Modules.<X> with Add<X>; only Program names it; Program registers no type of that context, where registers means the type is a generic argument of a call to an IServiceCollection registration extension (Add*, TryAdd*, Configure*, AddHostedService) or of GetService/GetRequiredService, read from the IL of Program and its compiler-made nested types; allowed, by full name and compared both ways, in four declared categories: (1) the handler block entries of that context; (2) the order-kept registrations of that context; (3) the host set-up lookups Program makes with GetRequiredService (Forecasting ForecastLane in PrintSystemInfo, Access IOidcGroupSnapshotWriter in the OIDC token-validated event), compared apart from registrations; (4) cross-context wiring kept in Program: the context types named by a registration statement that names types of two contexts. Measured at 1c51ec97e, the only such statements outside the handler block are Program.cs:1475 AddScoped<IDescriptionBlockMerger, DeliveryForecastBlockRenderer> (TrackerIntegration + Promises), :1481 AddScoped<IThroughputFilter>(sp => sp.GetRequiredService<ForecastFilterRuleService>()), :1483 IForecastDataSufficiency and :1484 IHowManyForecaster, both from ForecastingAnswersToFlowMetrics (FlowMetrics + Forecasting). S1 category 4 entries: FlowMetrics registered IThroughputFilter, IForecastDataSufficiency, IHowManyForecaster (FlowMetrics.Application); Forecasting looked up ForecastFilterRuleService, ForecastingAnswersToFlowMetrics (Forecasting.Application); Promises registered DeliveryForecastBlockRenderer (Promises.Application). S2 category 4 entry: TrackerIntegration registered IDescriptionBlockMerger (TrackerIntegration.Application). Method for S2 and any later context: before writing its list, list every registration statement of Program (statements joined across lines, handler block excluded) whose type names resolve to declarations under two different Modules/<X> folders, Contracts included; then confirm by a trial move whose Architecture run names no <X>Module in R-C1 or in another context's boundary rule. The approved S1 oracle CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest lacks category 4: its revision by the acceptance designer, with a made-up cross-context line in CompositionRootFixtures that is reported once taken off the list, is the next step before S1 craft; the S2 oracle is written with all four categories. The call reading is a new support CompositionRootRegistrations written with the S1 oracle and only read afterwards. S3 ProgramNamesModulesOnlyArchUnitTest: every type of the ten contexts that Program names at all (IlDependencies.DependentsIn over Program) is a <X>Module, a handler of the block, an order-kept registration, or a measured host set-up type; RED until the strays move.
```

#### D6

```text
Host set-up type versus stray (B3 list): a host set-up type is a module type Program needs outside the container registrations to set the host up: authentication scheme handlers and selectors in ConfigureAuthentication, MVC filters in AddControllers, the HTTP client names of ConfigureServices, and reads in PrintSystemInfo. A stray is any other module-internal name Program keeps, such as the Tracker integration OAuth provider check EnsureOAuthProvidersRegistered names (text measure at 49552e47a, noisy); a stray moves behind a <X>Module method called at the same point. The B1 acceptance designer measures the names from the IL; the S3 acceptance designer writes the list from that measure. If the measure leaves no stray after S2, S3 has no RED and goes back to the host.
```

#### D7

```text
Formal verification (D-16 confirmed): No model for container equivalence, handler order or the composition-root rule: there is no shared mutable state between actors, no multi-step protocol, no concurrency or crash window; each claim is a relation over closed finite sets (descriptors per service type, IL references of one type) executed exhaustively by the pin, the order pin and the rules. No checker is selected, so none is probed. No liveness claim: start-up registration is one synchronous pass with nothing that must eventually happen. PBT: not applicable, the domains are closed and finite and are enumerated whole.
```

#### D8

```text
Existing rules that name the composition root for one context (measured by a reverted trial move of the five S1 contexts, 88 Architecture failures, every one resolved below or by keeping the cross-context lines in Program): a both-ways adapters rule switches its root from Lighthouse.Backend.Program to the exact <X>Module and its set-aside namespaces to the context's namespaces below its root; an outside-types list whose Lighthouse.Backend.Program entry goes stale loses that entry, not replaced, because <X>Module is inside its context and reaching its own layers is legal by right; a reacher list inside one context gains the exact <X>Module full name; a file-path allow list gains the exact Modules/<X>/<X>Module.cs path beside Program.cs. AccessModuleBoundariesArchUnitTest's Program entry was not stale in the trial (Program keeps the Access HTTP set-up types), so it stays unless the craft run names it. Every other existing rule is run whole (the Architecture namespace) at each slice; an approved oracle of this feature is never edited except by its acceptance designer's revision.
```

#### D9

```text
Own-context root (maintainer decision 2026-11-26): a context's own <X>Module (Lighthouse.Backend.Modules.<X>.<X>Module) counts as the composition root, but only for the rules about its own context: adapters reached only by the composition root, only the composition root creates the forecast threads, and the membership lists, which gain the <X>Module entry. It is never a composition root for another context: a registration statement that names types of two contexts (one context's type built from, or registered as, another context's service) stays in Program as cross-context wiring, so no new cross-context edge appears and feature A's 73-edge R-C1 list and its surface rows stay unchanged. Rule edits are the narrowest that express this: an exact <X>Module full name or file path, never a namespace-wide exemption, and no rule or list is loosened across contexts; each edit is proven by a reverted probe recorded as a craft command. The preservation pin StartUpRegistrationsPreservationTest.cs names types by today's full names on purpose and is exempt, by its exact file name, from the old-name sweep of ContextContractsMembershipArchUnitTest; the exemption lands in the pin's own first commit and leaves with the pin at S3. Feature A's oracle files and the boundary and membership tests these edits touch are declared targets of the slice whose move makes them fail; ModuleRules.cs and ContextContractsArchUnitTest.cs need no edit, because every production call site passes the root and the set-aside namespaces as arguments and the cross-context lines stay in Program. Pattern used for both-ways adapter rules: composition root = the exact <X>Module full name, set-aside namespaces = the context's namespaces below its root (^Lighthouse\.Backend\.Modules\.<X>\.), so <X>Module is the one outside reacher and Program reaching the adapters fails.
```

### Constraints

#### K1

```text
Behaviour-free (D-0, scope out): per service type, the ordered implementations, lifetimes and keys and the set of service types are unchanged; no route, DTO, schema, migration, configuration key or log category changes; ConfigureServices, ConfigureDatabase, ConfigureHttps, ConfigureApp and Main ordering are unchanged except the S2 removal of the RegisterReports call and the S3 stray moves. Registrations under branches not taken in the Testing environment (stub mode off, Redis set) move inside their unchanged if.
```

#### K2

```text
Counts are to be re-measured at B1: the design figures (283 pinned entries, the line numbers of Program.cs) date from 952040471 or 49552e47a; B1 records the measured descriptor count, hosted-service and seeder orders, and the Program IL names per context.
```

#### K3

```text
Program.cs is in SHARED_REGEX, so each slice end runs the full filtered backend suite (Integration included) and the push runs every live connector suite once; locally the live connector categories are never run. Tests: no nWave tags; the oracles are IL architecture tests and the pin reads a ServiceCollection, none starts a host; made-up offenders live in a fixture namespace such as Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures that no module pattern matches; rules assert by the failure naming the offender, each rule asserts its subject set is not empty.
```

#### K4

```text
No input bound applies: no stored or validated input is added or changed; the ten contexts are the inline ten of ADR-270 Decision 1 (R-C3).
```

### Reuse

StartUpDomainEventHandlers

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:175

Reuse

```text
Its seam (CreateBuilder Testing, RegisterServices by reflection) is the pin seam; the pin copies the seam into its own file instead of editing the approved order pin, whose file stays unchanged through B.
```

DomainEventHandlerOrderPinTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:18

Reuse

```text
Stays the permanent handler-order pin, unchanged; green after every slice.
```

RegisterServices

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1349

Extend

```text
Keeps name and signature (the seam both pins reflect on); its context lines move out and it calls Add<X> and RegisterDomainEventHandlers instead.
```

RegisterReports

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1327

Replace

```text
The only per-context registrar today; folded into ReportsModule.AddReports in S2 (D-9).
```

AddUsageDataPublishing

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UsageData\/PostHogUsageDataPublisher\.cs\:299

Reuse

```text
Precedent for an IServiceCollection extension; Platform is not a context, so it stays called from Program unchanged.
```

\<X\>Module

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules

CreateNew

```text
No type owns a context registrations today (only Program.RegisterServices and RegisterReports); one holder per context at the module root (D-9).
```

DependentsIn

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:36

Reuse

```text
Reads every type Program names, nested closures included; the S3 rule reads with it unchanged.
```

TheCompositionRoot

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:35

Reuse

```text
The one name of the composition root, read by the three rules; not edited.
```

InTheAssembly

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PackagedContexts\.cs\:12

Reuse

```text
The enumerator of the ten contexts (R-C3); the S3 rule checks its ten listed contexts equal it, so a new context cannot escape.
```

CompositionRootRegistrations

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootRegistrations\.cs

CreateNew

```text
IlDependencies reads which types a type names, not which types it passes to a registration call; the per-call reading the S1 and S2 rules need does not exist. Written with the S1 oracle as its support.
```

### Impact

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1349

Changed

```text
Registration lines leave for Add<X>; handler block gathered; RegisterReports folded; strays moved (S3).
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules

Changed

```text
Ten new <X>Module.cs files at the module roots, holding moved lines only.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:175

UnchangedCompatible

```text
Seam RegisterServices(WebApplicationBuilder) is kept and still registers every handler, in order.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:98

Changed

```text
Only if the move leaves its Program entry stale: that entry becomes ForecastingModule, nothing else.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:115

Changed

```text
Only if the move leaves its Program entry stale: that entry becomes AccessModule, nothing else.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:131

Changed

```text
Only if the move leaves its Program entry stale: that entry becomes WorkItemsModule, nothing else.
```

host\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests

UnchangedCompatible

```text
WebApplicationFactory hosts call Main, which still registers the same container; the full filtered suite at each slice end observes it.
```

ci

Scripts\/test\-selection\/path\-classifier\.sh

UnchangedCompatible

```text
Program.cs already in SHARED_REGEX; Modules/<X>/<X>Module.cs fall under each module existing folder classification.
```

### Agreement

None.

### Not applicable

agreement

```text
No shared wire, schema or configuration contract changes: Add<X> has one caller, Program, in the same deployable and the same commit; configuration keys are read unchanged.
```

contract_tests

```text
Single deployable; producer (Add<X>) and consumer (Program) change in the same commit; nothing persisted or on the wire outlives a version.
```

### Boundaries

```text
The composition-root port: Program.RegisterServices(WebApplicationBuilder) (and, until S2 folds it, Program.RegisterReports(IServiceCollection)) filling the builder service collection, read without a host the way StartUpDomainEventHandlers in DomainEventHandlerOrderPinTest reads it; and the compiled IL of Lighthouse.Backend.Program read with IlDependencies.
```

#### Driven ports

```text
None new: the moved registrations name the same implementations, adapters and hosted services.
```

```text
Program -> <X>Module.Add<X> -> the context own Application, Domain and Adapters types. Only Program names a <X>Module; no module names another module start-up type; Program names no module-internal type but the domain-event handlers of its one ordered handler block and the measured host set-up types.
```

#### Failures

##### B1

```text
A rule or the pin finds a difference
```

Refusal

```text
Assertion failure naming the service type or the offending type full name, why it matters and how to fix (WHAT/WHY/HOW).
```

##### B2

```text
The seam method RegisterServices(WebApplicationBuilder) is missing
```

Refusal

```text
Set-up failure naming the method to point the pin at.
```

### Observations

```text
Container projection: per service type, the ordered list of (lifetime, key, implementation) of the descriptors RegisterServices and RegisterReports add, read without a host
```

map of service type to ordered list of triples

```text
Two factories asking the same services but differing in constants or captured locals; options values bound from configuration; registrations under branches not taken in the Testing environment; ConfigureServices and ConfigureDatabase registrations (not read).
```

```text
Registered-by-Program names: types of a listed context passed as generic arguments to registration or GetService calls in the IL of Program and its nested types
```

set of full type names per context

```text
Which method of Program made the call; a type registered through a non-generic overload (typeof argument) unless the support reads it.
```

```text
Names of Program: every type of the ten contexts Program or its nested types reference in IL
```

set of full type names

```text
XML-doc crefs, event declarations and named attribute arguments (not in IL references read by IlDependencies).
```

### Equality

container projection

```text
Two start-ups are equal when they have the same set of service types and, per service type, the same ordered list of (lifetime, key, implementation); order between different service types is ignored (container resolution does not depend on it).
```

### Constructors

None.

### Laws

#### L1

```text
Moving registrations into Add<X> leaves the container projection equal to the pre-B1 baseline.
```

Invariant

Infrastructure

StartUpRegistrationsPreservationTest

##### Treatment

Finite

###### Cases

```text
Every service type of the baseline, compared both ways
```

```text
Planted swap of two IHostedService lines: fails naming IHostedService
```

```text
Planted lifetime change of one line: fails naming its service type
```

```text
Planted dropped line: fails naming its service type
```

```text
Closed finite set of descriptors, enumerated whole.
```

##### Evidence

None.

#### L2

```text
Handlers of each event come in the written-out order.
```

Invariant

Infrastructure

DomainEventHandlerOrderPinTest

##### Treatment

Finite

###### Cases

```text
The seven pinned events, unchanged file
```

```text
Existing permanent pin, closed set.
```

##### Evidence

None.

#### L3

```text
For every listed context, Program registers no type of it outside the allowed list, the context module exists with Add<X>, and only Program names that module.
```

Invariant

Infrastructure

CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest

##### Treatment

Finite

###### Cases

```text
Each listed context, real Program
```

```text
Made-up composition root in CompositionRootFixtures registering a type of a listed context: reported by name
```

```text
Made-up type outside Program naming a <X>Module: reported
```

```text
Compliant twin registering only through a made-up module: not reported
```

```text
Closed finite IL of one type and ten contexts.
```

##### Evidence

None.

#### L4

```text
Program names no type of the ten contexts but <X>Module types, handler-block entries, order-kept registrations and the measured host set-up types.
```

Invariant

Infrastructure

ProgramNamesModulesOnlyArchUnitTest

##### Treatment

Finite

###### Cases

```text
Real Program after S3
```

```text
Made-up composition root naming a module-internal type off the list: reported
```

```text
Listed contexts equal PackagedContexts.InTheAssembly
```

```text
Closed finite IL.
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
Only Program composes the modules, and the container stays what it was.
```

```text
A dropped, duplicated or reordered registration changes start-up, hosted-service or handler order silently.
```

CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest

```text
Compiled Lighthouse.Backend assembly
```

\<X\>Module\.Add\<X\>

Program\.RegisterServices

```text
dotnet test exit and the failing rule naming the offender
```

```text
Reflection or a non-generic registration overload (typeof argument) escapes the registration reading; branches not taken under Testing escape the pin; the review reads the diff for both.
```

### States

Admitted

```text
A context registers through its own Add<X>, called once from RegisterServices.
```

None.

Excluded

```text
Program registering a type of a listed context outside the allowed list; a module naming another <X>Module.
```

CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest

Unknown

```text
Registrations under branches not taken in the Testing environment (stub mode off, Redis set): not pinned.
```

slice review

### Models

None.

### Closure

```text
module start-up
```

Safety

Pass

DesignedNotBuilt

```text
The three rules and the pin exclude stray registration, a wrong caller and a changed container over closed finite sets.
```

##### Evidence

None.

```text
module start-up
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each <X>Module.Add<X> is built from moved lines; the pin shows the composed container equals the baseline.
```

##### Evidence

None.

```text
module start-up
```

PublicReachability

Pass

DesignedNotBuilt

```text
Main -> RegisterServices -> Add<X> -> container; observed through the seam and the full filtered suite hosts.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negated: per-module blocks keep handler order (false for TeamDataRefreshed)
```

```text
Handler order per event
```

```text
Handlers stay in Program
```

```text
A handler inside an Add<X>
```

```text
None: order pin
```

DomainEventHandlerOrderPinTest

#### Y2

Stressor

```text
Negated: hosted services and seeders keep their order when a context block moves
```

```text
Order within IHostedService, ISeeder and every multi-registered type
```

```text
Add<X> placed at the kept position, or the line stays in Program, listed
```

```text
A reordered multi-registration
```

```text
None under Testing: the pin
```

StartUpRegistrationsPreservationTest

#### Y3

Stressor

```text
Negated: a factory can be observed without its dependencies
```

```text
Factory identity by requested services
```

```text
Recording provider answers nothing
```

```text
Running a factory against the database or the key ring
```

```text
A factory asking for nothing runs for real; none is registered under Testing (Redis multiplexer is behind a set connection string)
```

StartUpRegistrationsPreservationTest

#### Y4

Bypass

```text
Branches not taken under Testing; reflection or typeof registrations
```

```text
The same lines inside the same if
```

```text
Review of the diff
```

```text
Editing a moved line
```

```text
Unpinned branches
```

slice review

#### Y5

SinglePointOfFailure

```text
The pin is temporary and deleted in S3
```

```text
Order pin and the three rules stay
```

```text
Later DI changes need no pin edit
```

```text
Deleting the pin before its last green run is recorded on the S3 candidate
```

```text
After S3 container equivalence is not re-checked; walked second set: a hosted service reordered later is caught only by tests of its behaviour
```

S3 crafter and reviewer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Create

```text
AddFlowMetrics with the context registration lines moved from Program.RegisterServices.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/ForecastingModule\.cs

Create

```text
AddForecasting with the context registration lines moved from Program.RegisterServices.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/PromisesModule\.cs

Create

```text
AddPromises with the context registration lines moved from Program.RegisterServices.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/LicensingModule\.cs

Create

```text
AddLicensing with the context registration lines moved from Program.RegisterServices.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/AccessModule\.cs

Create

```text
AddAccess with the context registration lines moved from Program.RegisterServices.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
The five contexts lines leave RegisterServices, replaced by the Add<X> calls at the order-keeping positions.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Create

```text
The temporary container pin, first commit on the pre-B1 revision with the baseline captured there, green before any line moves.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
TheForecastingAdapters rule: root becomes "Lighthouse.Backend.Modules.Forecasting.ForecastingModule", set-aside namespaces ^Lighthouse\.Backend\.Modules\.Forecasting\.; the stale "Lighthouse.Backend.Program" entry of OutsideTypesReachingTheDomain is deleted, not replaced.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
Only if its Lighthouse.Backend.Program entry goes stale: replaced by AccessModule, nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

Extend

```text
Gains two one-entry lists, AccessModuleRoot ["AccessModule"] and LicensingModuleRoot ["LicensingModule"]; the layer lists stay unchanged because AccessApplication and LicensingApplication feed TypesLiveInTheirLayer and the unit-test placement rules.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

Extend

```text
AccessLists gains AccessModuleRoot and LicensingLists gains LicensingModuleRoot, so AccessModule and LicensingModule are listed members; the fix hints name the new lists.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs

Extend

```text
Moves gains "PromisesModule" (it lives in Modules/Promises).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

Extend

```text
The old-name sweep skips the preservation pin by its exact file name "StartUpRegistrationsPreservationTest.cs" (a one-entry exemption beside TheMeasurementFiles, a string because S3 deletes the class); written in the pin's own first commit.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs

Extend

```text
The allowed set gains the path of Modules/Forecasting/ForecastingModule.cs, which now creates the forecast lane; Program.cs stays allowed for its PrintSystemInfo lookup.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs

Extend

```text
Gains "Lighthouse.Backend.Modules.Forecasting.ForecastingModule", which registers the round services.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Extend

```text
The production call passes compositionRoot "Lighthouse.Backend.Modules.FlowMetrics.FlowMetricsModule" and moduleNamespaces ^Lighthouse\.Backend\.Modules\.FlowMetrics\. instead of the defaults; the fixture rows are unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~StartUpRegistrationsPreservationTest|FullyQualifiedName~DomainEventHandlerOrderPinTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootRegistrations\.cs

None.

None.

#### Public oracle

```text
Run the S1 oracle class through dotnet test over the compiled assembly: it reads the IL of Lighthouse.Backend.Program for FlowMetrics, Forecasting, Promises, Licensing and Access.
```

```text
Before craft: RED by assertion, naming for each of the five contexts the missing <X>Module and the context types Program still registers. After craft: green; StartUpRegistrationsPreservationTest and DomainEventHandlerOrderPinTest green; the Architecture namespace green.
```

```text
A made-up composition root in CompositionRootFixtures that registers a type of a listed context is reported by its full name; a made-up type outside Program naming a <X>Module is reported; a compliant twin is not; the pin goes red on a swapped IHostedService line, a changed lifetime and a dropped line (reverted probes recorded as craft commands).
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

FlowMetricsModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

ForecastingModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/ForecastingModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

PromisesModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/PromisesModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

LicensingModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/LicensingModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

AccessModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/AccessModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

StartUpRegistrationsPreservationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

CreateNew

```text
The order pin reads only handler registrations; no test pins every descriptor. Its seam copies StartUpDomainEventHandlers rather than editing the approved order pin.
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:98

Extend

```text
Existing approved rule of a closed Story; only its Lighthouse.Backend.Program entry is replaced by ForecastingModule, and only if the move leaves it stale.
```

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:115

Extend

```text
Existing approved rule of a closed Story; only its Lighthouse.Backend.Program entry is replaced by AccessModule, and only if the move leaves it stale.
```

AccessAndLicensingMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs\:21

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

AccessLists

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs\:73

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

Moves

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs\:11

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

TheMeasurementFiles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs\:35

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

Only\_the\_forecast\_updater\_and\_the\_composition\_root\_reach\_the\_forecast\_threads

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs\:71

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

TheForecastingTypesReachingTheRound

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs\:45

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

TheModuleAdapters\_AreReachedFromOutsideOnlyByTheCompositionRoot

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:82

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

##### Impact

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/ForecastingModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/PromisesModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Licensing\/LicensingModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/AccessModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Changed

```text
New temporary pin, baseline captured on the pre-B1 revision.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FastWithManyFeaturesArchUnitTest\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackRoundBoundariesArchUnitTest\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Changed

```text
Own-context root: the narrowest edit that keeps the rule green after the five S1 contexts move; no list loosened.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: Program.cs holds one type (Program); the five <X>Module.cs and StartUpRegistrationsPreservationTest.cs are new, one type each; AccessAndLicensingMembership.cs and PromisesMembership.cs hold one type each. AccessAndLicensingMembershipArchUnitTest.cs, ContextContractsMembershipArchUnitTest.cs, WriteBackRoundBoundariesArchUnitTest.cs, FlowMetricsInboundDomainAndAdaptersArchUnitTest.cs, ForecastingModuleBoundariesArchUnitTest.cs and AccessModuleBoundariesArchUnitTest.cs hold their fixture class plus the made-up offenders the rule reads from the test assembly IL: not variants of one sum type, but no move is possible in this slice because the maintainer allows only the narrowest edit to these approved oracles of closed Stories and feature A, and a split would change their approved bytes. FastWithManyFeaturesArchUnitTest.cs holds one type.
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
Five contexts are registered only through their own module, only Program names a module, container and handler order unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/WorkItemsModule\.cs

Create

```text
AddWorkItems with the context registration lines moved from Program.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

Create

```text
AddTrackerIntegration with the context registration lines moved from Program.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/RefinementModule\.cs

Create

```text
AddRefinement with the context registration lines moved from Program.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/ReportsModule\.cs

Create

```text
AddReports with the context registration lines moved from Program.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/OrganisationSetupModule\.cs

Create

```text
AddOrganisationSetup with the context registration lines moved from Program.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
The other five contexts lines leave; RegisterReports folds into AddReports and Main stops calling it; every handler line gathered into RegisterDomainEventHandlers, called from RegisterServices, no line reordered within an event.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Extend

```text
Seam line only: the RegisterReports invocation goes with the method; the baseline bytes stay unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
If its "Lighthouse.Backend.Program" entry of OutsideTypesReachingTheDomain goes stale, it is deleted, not replaced; nothing else.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs

Extend

```text
Moves gains "TrackerIntegrationModule", if the S2 run names it unlisted.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
Moves gains "WorkItemsModule", if the S2 run names it unlisted.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

Extend

```text
Gains RefinementModule at the module root in the file's existing shape, if the S2 run names it; a shape with no place for a root type returns to the architect.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

Extend

```text
Gains ReportsModule at the module root in the file's existing shape, if the S2 run names it; a shape with no place for a root type returns to the architect.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Extend

```text
Gains OrganisationSetupModule at the module root in the file's existing shape, if the S2 run names it; a shape with no place for a root type returns to the architect.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
The production adapters rule takes root "Lighthouse.Backend.Modules.TrackerIntegration.TrackerIntegrationModule" and set-aside namespaces ^Lighthouse\.Backend\.Modules\.TrackerIntegration\. instead of ModuleRules.TheCompositionRoot; its OtherOutsideTypesReachingTheAdapters list is unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest|FullyQualifiedName~CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~StartUpRegistrationsPreservationTest|FullyQualifiedName~DomainEventHandlerOrderPinTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootRegistrations\.cs

None.

None.

#### Public oracle

```text
Run the S2 oracle class with the S1 class: together they list the ten contexts; the S2 class reads the IL of Program for WorkItems, TrackerIntegration, Refinement, Reports and OrganisationSetup.
```

```text
Before craft: RED by assertion naming each missing <X>Module and the context types Program still registers. After craft: both classes green; StartUpRegistrationsPreservationTest baseline unchanged and green; DomainEventHandlerOrderPinTest unchanged and green; Program holds every IDomainEventHandler<> line in RegisterDomainEventHandlers.
```

```text
A made-up composition root registering a Reports type is reported; dropping the AddReports call turns the pin red on the Reports service types; moving a TeamDataRefreshed handler into an Add<X> turns the order pin or the rule red (reverted probes recorded).
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

WorkItemsModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/WorkItemsModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

TrackerIntegrationModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

RefinementModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/RefinementModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

ReportsModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/ReportsModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

OrganisationSetupModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/OrganisationSetupModule\.cs

CreateNew

```text
No type owns this context registrations today; Program.RegisterServices holds them (D-9). Lines move verbatim.
```

StartUpRegistrationsPreservationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

CreateNew

```text
Created by S1 of this feature, not yet on main; here only its seam line changes (RegisterReports invocation removed), baseline unchanged.
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:131

Extend

```text
Existing approved rule of a closed Story; only its Lighthouse.Backend.Program entry is replaced by WorkItemsModule, and only if the move leaves it stale.
```

Moves

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:13

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

Moves

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:22

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

MovingTypes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs\:69

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

MovingTypes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs\:77

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

MovingTypes

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:59

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

AdaptersInboundRule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:474

Extend

```text
Existing rule or list owns this own-context fact; extended by one exact entry or argument, no new rule.
```

##### Impact

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/WorkItemsModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Refinement\/RefinementModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/ReportsModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/OrganisationSetupModule\.cs

Changed

```text
New module start-up holder; its only caller is Program.RegisterServices.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Changed

```text
Seam line only; the baseline bytes do not change.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembership\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Changed

```text
Own-context root for the S2 contexts, by the S1 treatment; edited only if the S2 Architecture run names the <X>Module or a stale Program entry.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: Program.cs holds one type (Program); the five <X>Module.cs are new, one type each; StartUpRegistrationsPreservationTest.cs holds one test class, one line changes; TrackerIntegrationMembership.cs holds one type; RefinementMembership.cs, ReportsMembership.cs and OrganisationSetupMembership.cs hold the membership class with the record and enum types its lists are made of, which change together; WorkItemsMembershipArchUnitTest.cs, TrackerIntegrationModuleBoundariesArchUnitTest.cs and WorkItemsModuleBoundariesArchUnitTest.cs hold their fixture class and the made-up offenders the rule reads from the IL; no move is possible in this slice because only the narrowest edit to these approved oracles of closed Stories is allowed.
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
The other five contexts register only through their module; handlers stay in one ordered block; container unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
The strays leave Program behind a <X>Module method called at the same point; Program keeps Add<X> calls, host set-up and the handler block.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

Extend

```text
Receives the Tracker integration strays (the OAuth provider start check), if the B1 measure classifies them as strays.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Extend

```text
Deleted, in the same commit as its last green run on the S3 craft is recorded as a craft command.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

Extend

```text
The pin's file-name exemption from the old-name sweep is removed in the commit that deletes the pin.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ProgramNamesModulesOnlyArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~StartUpRegistrationsPreservationTest|FullyQualifiedName~DomainEventHandlerOrderPinTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Run the S3 oracle class over the compiled assembly: it reads every type of the ten contexts that Program and its nested types name.
```

```text
Before craft: RED by assertion naming each stray module-internal type Program still names. After craft: green with the list of handlers, order-kept registrations and measured host set-up types; the S1 and S2 rules and the order pin green; StartUpRegistrationsPreservationTest absent, its last green run recorded on the craft revision just before deletion.
```

```text
A made-up composition root naming a module-internal type off the list is reported by full name; the listed ten equal PackagedContexts.InTheAssembly, so an eleventh context fails; a list entry Program no longer names fails as stale.
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

StartUpRegistrationsPreservationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

CreateNew

```text
Created by S1 of this feature, not yet on main; deleted here after its last recorded green run (P3).
```

TrackerIntegrationModule

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

CreateNew

```text
Created by S2 of this feature, not yet on main; receives the Tracker integration strays behind a method Program calls at the same point.
```

TheMeasurementFiles

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs\:35

Extend

```text
The sweep owns its exemptions; the temporary one leaves with the pin.
```

##### Impact

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

Changed

```text
Deleted; DomainEventHandlerOrderPinTest stays the permanent pin.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/TrackerIntegrationModule\.cs

Changed

```text
Gains the stray start check, same call point and timing.
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

Changed

```text
Back to its feature A bytes for the file list; the sweep reads every test file again.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Per file: Program.cs holds one type (Program); TrackerIntegrationModule.cs holds one type; StartUpRegistrationsPreservationTest.cs is deleted. Nothing to split.
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
Program names modules only, apart from the handler block and the measured host set-up types.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ProgramNamesModulesOnlyArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ProgramNamesModulesOnlyArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
