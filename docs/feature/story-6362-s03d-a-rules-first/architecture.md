# Architecture brief: story\-6362\-s03d\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (D-0, D-12, P5): feature A changes no production code, schema, DI registration or Program.cs. Only test-side architecture rule files, ModuleRules, the EXAMINE command builder, .nwave/examine.json and the classifier test script change; path-classifier.sh only if an A4 case is red. Nothing moves in feature A; the moves are feature B (story-6362-s03d-b-move), the module rules, C2 port and placement are feature C.
```

#### D2

```text
Confirmed (D-1): the Promises membership is the 13 Services files that move (DeliveryForecastBlockRenderer, IDeliveryForecastBlockRenderer, DeliveryRuleService, IDeliveryRuleService, DeliverySourceResolver, DeliverySourceSyncService, DeliveryForecastPublishingService and their 3 interfaces, DeliveryMetricValuesProjector, DeliveryMetricSnapshotRecordingHandler, DeliveryForecastPublishingHandler) plus the admitted Models value DeliveryForecastBlock. Update code, write-back round, ordering and ranking, repositories, entities, controllers and DTOs stay with a named owner.
```
Governed slices: S2

#### D3

```text
Confirmed (D-2): a Models type is admitted into the module only if it is not an EF entity, not on the wire, not reached by a kernel type and not used by code that stays outside. Expected admitted: DeliveryForecastBlock only (E11). A2 measures every candidate from the IL and pins the answer; if DeliveryForecastBlock fails a condition, A2 records it and the Domain folder is dropped (D-3).
```
Governed slices: S2

#### D4

```text
Confirmed (D-3, D-8, Ratchet design: Rules first): the target namespaces are Lighthouse.Backend.Modules.Promises.Domain, .Application and .Application.Policies. A1 adds a PromisesModule constant to ModuleRules and a PromisesPattern ^Lighthouse\.Backend\.Modules\.Promises(\.|$) beside the old Delivery namespaces in the module map; every rule keyed on a Promises namespace or folder also matches it. PortfolioDeliveryPattern keeps its update-code meaning and is not renamed (E1).
```
Governed slices: S1

#### D5

```text
Confirmed (D-4, D-5, D-6): Delivery repositories, Feature ordering and ranking and the write-back round (#6657) stay outside Promises. A Modules.Promises type that reaches a repository outside the listed platform ports, Forecasting adapters or the update code is an offender for the widened rules.
```
Governed slices: S1

#### D6

```text
Confirmed (D-7): Promises Application reaching Modules.TrackerIntegration.Application is legal by namespace (written in C1); the Jira connector to renderer edge stays listed until C2 inverts it. Feature A widens the Tracker integration rule that names the Promises namespaces so it also sees Modules.Promises, without making the existing Jira to renderer entry red.
```
Governed slices: S1

#### D7

```text
Confirmed (D-9, D-10): tests move with production in feature B; Program.cs lines and handler order stay. Using-only and list-string renames in DeliveryRuleServiceApiPreservationTest, TestHostUseGuardTest.HostUsersStillToMove, WorkItemsModuleBoundariesArchUnitTest and TrackerIntegrationMembershipArchUnitTest belong to B1, not feature A.
```

#### D8

```text
Confirmed (D-11): no classifier widening. A4 adds cases to test-path-classifier.sh: a Modules/Promises source path and test path select the backend suite only; Modules/TrackerIntegration/Application/IDescriptionBlockMerger.cs and Program.cs select every live connector suite. path-classifier.sh changes only if one of those cases is red.
```

#### D9

```text
Confirmed (D-12): a new EXAMINE command deliveries-answers-compare (CREATE_NEW, thin, on the shared capture pieces of build-examine-commands like metrics-, forecast- and refresh-answers-compare) reads per demo scenario and Portfolio the Delivery list, deliveries/{id}/metrics-history, portfolios/{id}/delivery-rules/schema and the delivery sources on two revisions and compares the bodies: IDENTICAL exit 0, DIFFERENT exit 1, and it fails when nothing was compared. Its compare stage takes two capture folders (split out in A3 if it cannot today). Declared in .nwave/examine.json before the first EXAMINE.
```
Governed slices: S3

#### D10

```text
Confirmed (D-13 and the start of DELIVER): rigor thorough for feature A; push per slice with CI watched; no review window; mutation once at the end of S03d on frozen code, 80 percent kill rate and no new survivor in moved code.
```

#### D11

```text
Carriers: rule baselines are inline string lists in their test classes, keyed by full type name and compared both ways (ADR-268); the IL is read through IlDependencies over Mono.Cecil; the membership and admissions list is a static class PromisesMembership in the Architecture folder (shape of TrackerIntegrationMembership), owner the A2 oracle; made-up offenders live in a fixture namespace such as Lighthouse.Backend.Modules.PromisesFixtures; the EXAMINE command is a generated bash script under Scripts/examine/bin, owner build-examine-commands. No production carrier, effect or per-process state changes.
```

#### D12

```text
Formal verification not applicable (feature-delta Formal verification): the claims are decided exactly by the compiled IL with a made-up offender per rule, by two fixture capture pairs for the compare command and by finite classifier cases. No state, ordering, concurrency, recovery or liveness claim changes; a Quint, TLA+ or Agda model would restate the namespace map and could not fail. No checker was run. Property tests: not applicable, every domain here is finite and enumerated.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTOs, responses, Delivery answers, forecasts, DI, handler order and the EF model stay the same; every existing test stays green.
```

#### K2

```text
Never loosen a rule: a widening adds the Modules.Promises namespace to what a rule sees; exemptions are exact per type (that type and its nested types by full name), never a namespace or layer. Every rule edit is proved by a reverted throwaway probe.
```

#### K3

```text
IlDependencies.TypesIn also returns nested types; an exact-set check compares top-level types (the part before +). IlDependencies misses catch-clause, event and attribute named-argument references.
```

#### K4

```text
One oracle file per slice; an approved oracle or support file of this feature is never edited, not even a using line; oracles name production types as strings. No nWave scenario tags in code; Lighthouse test-writing skill wins over nWave timing and PBT defaults.
```

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
Owns the module namespace constants and shared rules; gains a PromisesModule constant beside FlowMetricsModule and ForecastingModule; no rule copied.
```

ForecastingRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:25

Reuse

```text
Its made-up offender under a module namespace, asserted by the rule naming that offender, is the approach A1 follows.
```

TrackerIntegrationMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembership\.cs\:1

Reuse

```text
Shape for the Promises membership list and its both-ways IL comparison in A2.
```

PortfolioDeliveryPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:18

Reuse

```text
Names the update code only (E1); kept as is; a new PromisesPattern sits beside it.
```

refresh\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:586

Reuse

```text
Its shared capture and compare pieces carry the new deliveries-answers-compare; the new command needs other endpoints and its own nothing-compared message.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes shape: path-classifier.sh output is unchanged (A4 adds test cases only), .nwave/examine.json gains one command entry within its existing format, and no wire, DTO or schema changes.
```

contract_tests

```text
No producer and consumer change apart: one deployable, no wire or persisted format changes.
```

### Boundaries

```text
Architecture rule tests over the compiled production IL (dotnet test, Architecture namespace); the classifier test script; the EXAMINE command builder and its generated deliveries-answers-compare
```

#### Driven ports

```text
IlDependencies module reader over Mono.Cecil
```

```text
git revisions and the backend HTTP API on demo data for the answers capture
```

```text
Tests and scripts read production; production never references a test, fixture or script.
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
Production unchanged; every rule naming a Promises namespace also sees Modules.Promises; membership and admissions pinned by full type name.
```

story\-6362\-s03d\-a\-rules\-first

Excluded

```text
A type under Modules.Promises that a rule keyed on the old Promises namespaces would miss; a loosened rule or a namespace-wide exemption.
```

story\-6362\-s03d\-a\-rules\-first

Unknown

```text
Whether demo data holds a Delivery (A3 reports it); whether the two PortfolioForecastsUpdated handlers depend on each other and whether Promises reaches Forecasting or Flow metrics Application (A2 measures).
```

story\-6362\-s03d\-a\-rules\-first

### Models

None.

### Closure

```text
Feature A guards for the Promises move
```

Safety

Pass

DesignedNotBuilt

```text
A made-up offender under Modules.Promises is named by each widened rule; a list check fails on a missing and on an extra entry; the compare command cannot answer IDENTICAL for a changed answer.
```

##### Evidence

None.

```text
Feature A guards for the Promises move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today code passes every widened rule and the pinned membership list; two identical capture folders answer IDENTICAL.
```

##### Evidence

None.

```text
Feature A guards for the Promises move
```

PublicReachability

Pass

DesignedNotBuilt

```text
Each guard is reached through its public driver: dotnet test on the Architecture namespace, bash on the classifier and compare test scripts, and the examiner probes architecture-rules, test-selection-show and deliveries-answers-compare.
```

##### Evidence

None.

```text
Feature A guards for the Promises move
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no queue, schedule, retry or eventual progress claim; every guard is a bounded synchronous run.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: rules see Promises wherever it lives. A rule keyed on Services...DeliverySources misses a moved type.
```

```text
Every rule naming Promises names the made-up Modules.Promises offender
```

```text
Rule files gain the Modules.Promises key
```

```text
A moved type escaping a rule; widening that turns the listed Jira to renderer edge red
```

```text
Rules keyed on single type names follow in B1
```

story\-6362\-s03d\-a\-rules\-first

#### Y2

Bypass

```text
IlDependencies misses catch-clause, event and attribute named-argument references
```

```text
Edges through signatures, fields, calls and constructors
```

```text
Those three reference kinds stay unseen
```

```text
Claiming the rules see them
```

```text
Reviewer reads moved files for those kinds in feature B
```

story\-6362\-s03d\-b\-move

#### Y3

Stressor

```text
Assumption negated: the EXAMINE demo data holds a Delivery
```

```text
No false IDENTICAL
```

```text
The command fails loudly when nothing was compared
```

```text
IDENTICAL on an empty comparison
```

```text
A3 reports whether demo data holds a Delivery
```

story\-6362\-s03d\-a\-rules\-first

#### Y4

Stressor

```text
Assumption negated: a path under Scripts is not shared; a Scripts edit could select live suites
```

```text
A4 pins the selection for Promises, IDescriptionBlockMerger and Program.cs paths
```

```text
A3 and A4 pushes run what the classifier selects
```

```text
A Promises path silently skipping or forcing live suites
```

```text
Linear key 429 risk on pushes that select live suites (D-11)
```

story\-6362\-s03d\-a\-rules\-first

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Add the PromisesModule namespace constant.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
Add PromisesPattern (Modules.Promises plus the old Delivery namespaces) to the module map; PortfolioDeliveryPattern unchanged.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
PromisesAndTheUpdateCode (line 53) names only the old DeliverySources namespaces; it also matches Modules.Promises. Listed entries keep their names until B1.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
Only if the oracle shows a rule here keyed on a Promises namespace misses Modules.Promises; its list string for DeliveryRuleService is renamed in B1, not here.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Only if the oracle shows the repository rule misses a Modules.Promises type reaching a repository; the Delivery controller entries stay.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesRulesSeeTheModuleArchUnitTest"
bash Scripts/test-selection/test-path-classifier.sh
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
The architecture rule tests run (dotnet test, Architecture namespace) with a made-up Delivery service under Lighthouse.Backend.Modules.Promises (fixture namespace, compiled into the IL the rules read) that reaches a repository, Forecasting adapters or the update code, beside its twin under the old Services DeliverySources namespace.
```

```text
Each rule that names a Promises type, namespace or folder names the made-up Modules.Promises offender exactly as it names the same type under the old namespace; every existing architecture test stays green on today code.
```

```text
Today the made-up offender under Modules.Promises passes the rules (RED). A widening that misses Modules.Promises in ModuleRules, the module map or the Tracker integration rule leaves the offender unreported; a widening that loosens a rule or turns the listed Jira to renderer edge red fails the existing tests.
```

##### Promises

P1

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

PromisesAndTheUpdateCode

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:53

Extend

```text
The one namespace key in the Tracker integration rules that names Promises; widened, never narrowed.
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:129

Reuse

```text
Lists DeliveryRuleService by full name; an unlisted caller under Modules.Promises is already named by the both-ways comparison.
```

RefinementModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleArchUnitTest\.cs\:41

Reuse

```text
Keys on PortfolioDeliveryPattern, the update code only; unchanged meaning.
```

RepositoryLeakRatchetArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:124

Extend

```text
Owns the repository-dependency ratchet; extended only if the oracle shows a Modules.Promises type reaching a repository goes unreported.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Changed

```text
New PromisesModule constant.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:18

Changed

```text
New PromisesPattern beside PortfolioDeliveryPattern.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:53

Changed

```text
Promises key also sees Modules.Promises.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DeliveryRuleServiceApiPreservationTest\.cs\:44

UnchangedCompatible

```text
typeof reference; using-only edit in B1.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

UnchangedCompatible

```text
Test class names; the moving handler test line follows in B2 per D-9.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:197

UnchangedCompatible

```text
Short type names; unchanged by a namespace move.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production edit in feature A.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:129

UnchangedCompatible

```text
Expected unchanged: its full-name list already names an unlisted Modules.Promises caller; edited only if the oracle shows a namespace key missing Modules.Promises.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:124

UnchangedCompatible

```text
Expected unchanged; edited only if the oracle shows a Modules.Promises type reaching a repository goes unreported.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The rule files are widened in place; no preserving move makes the widening easier.
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
For each widened rule, a made-up offender under Modules.Promises is named exactly when its twin under the old Promises namespace is named.
```

Invariant

Infrastructure

PromisesRulesSeeTheModuleArchUnitTest

###### Treatment

Finite

###### Cases

```text
offender reaching a repository
```

```text
offender reaching Forecasting adapters
```

```text
offender reaching the update code
```

```text
today code: no offender named
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
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
Every architecture rule that names a Promises type, namespace or folder also sees Lighthouse.Backend.Modules.Promises, proved by a made-up offender there.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesRulesSeeTheModuleArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesMembershipArchUnitTest"
bash Scripts/test-selection/test-path-classifier.sh
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs

#### Public oracle

```text
The architecture rule tests run the membership check over the compiled production IL: the 13 moving types and the admitted Models types listed by full type name in PromisesMembership, compared with the top-level types the IL finds in the Promises namespaces and the Models candidates evaluated against the four admission conditions.
```

```text
The check passes on today code and its failure names each missing listed type and each unlisted type that appears; the admissions list holds DeliveryForecastBlock, or A2 records why not.
```

```text
A list with one entry removed, one made-up entry added, or a type that does not exist fails and names it; a check that compares one way only, counts entries or matches short names passes those cases and is rejected.
```

##### Promises

P2

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

TrackerIntegrationMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:1

Reuse

```text
Shape of the both-ways membership comparison by full type name.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:116

Reuse

```text
TypesIn reads the namespaces from the IL; nested types reduced to the top-level part.
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
No crafter target: the acceptance designer authors the oracle and its membership list; no production or rule file changes.
```

prefactoring

```text
No code target.
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
The listed membership equals the set of top-level types the IL finds, both ways, keyed by full type name.
```

Invariant

Infrastructure

PromisesMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
list equals IL: pass
```

```text
listed type missing: named
```

```text
unlisted type present: named
```

```text
nested type: reduced to its outer type
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
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
The Promises membership and Models admissions are measured from the IL and pinned both ways by full type name.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~PromisesMembershipArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Scripts\/examine\/build\-examine\-commands

Extend

```text
Generate deliveries-answers-compare on the shared capture and compare pieces; split the compare stage so it takes two capture folders if it cannot today.
```

\.nwave\/examine\.json

Extend

```text
Declare deliveries-answers-compare as an admitted EXAMINE probe.
```

Scripts\/examine\/test\-deliveries\-answers\-compare\.sh

#### Verification

```text
bash Scripts/examine/test-deliveries-answers-compare.sh
bash Scripts/examine/build-examine-commands
bash Scripts/test-selection/test-path-classifier.sh
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
bash Scripts/examine/test-deliveries-answers-compare.sh builds the EXAMINE commands and runs the deliveries-answers-compare compare stage on two fixture capture folders that differ in one Delivery answer, then on two identical capture folders.
```

```text
The first pair prints DIFFERENT naming the changed answer and exits 1; the identical pair prints IDENTICAL and exits 0; an empty pair fails because nothing was compared.
```

```text
Today the command does not exist (RED). A command that always prints IDENTICAL, always prints DIFFERENT, or answers IDENTICAL when nothing was compared fails the script.
```

##### Promises

P3

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

metrics\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:52

Reuse

```text
Model for a thin answers-compare command on the shared pieces.
```

forecast\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:477

Reuse

```text
Already compares Delivery likelihoods; stays the likelihood probe, the new command reads the other Delivery endpoints.
```

build\-examine\-commands

Scripts\/examine\/build\-examine\-commands\:99

Extend

```text
Owns the shared pieces and every generated command; one more command, no copy of the pieces.
```

commands

\.nwave\/examine\.json\:2

Extend

```text
The commands list owns the admitted EXAMINE probes; one entry added for deliveries-answers-compare.
```

##### Impact

script

Scripts\/examine\/build\-examine\-commands\:586

Changed

```text
New generated command beside refresh-answers-compare.
```

config

\.nwave\/examine\.json\:1

Changed

```text
New probe entry.
```

script

Scripts\/examine\/build\-examine\-commands\:477

UnchangedCompatible

```text
Existing compare commands keep their behaviour; shared pieces only gain a folder-input entry if A3 splits the compare stage.
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No dependency, boundary or owner law; the obligation is behavioural and covered by the script oracle.
```

prefactoring

```text
If the compare stage cannot take capture folders, A3 splits it inside the slice under the oracle; no separate preserving move.
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
compare(X, X) is IDENTICAL with exit 0; compare(X, Y) with one Delivery answer changed is DIFFERENT with exit 1; compare of empty captures fails.
```

Algebraic

Adapter

deliveries\-answers\-compare

###### Treatment

Finite

###### Cases

```text
identical pair
```

```text
one answer changed
```

```text
nothing compared
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
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

None.

##### Contract tests

None.

## ADRs

None.
