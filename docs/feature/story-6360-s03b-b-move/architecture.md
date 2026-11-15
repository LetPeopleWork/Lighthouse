# Architecture brief: story\-6360\-s03b\-b\-move

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Pure relocation of 43 Work Items types by git mv from Services into Lighthouse.Backend/Modules/WorkItems, namespace = folder (Lighthouse.Backend.Modules.WorkItems.{Domain,Domain.Dependencies,Application,Application.Dependencies,Application.Policies}), type and file names unchanged, the move list is the Moves list of WorkItemsMembershipArchUnitTest. Stays: DependencySourceSelector, DependencyFieldReferences, ParentSourceSelector, OrphanedFeatureCleanupService, IOrphanedFeatureCleanupService. Each slice is a pure-move commit (host commits after a move-ready message), then a separate edit commit for using lines, rule-list strings, scripts and Stryker paths.
```

#### D2

```text
Layer placement. Domain: SyncModeResolver, FetchFingerprint; Domain/Dependencies: DependencyHonourPolicy with IDependencyHonourPolicy, DependencyHonourInput and FeatureDependencyFacts (declared in the interface file), DependencyCycleDetector with DependencyLoops and DependencyLoop, DependencyFacts. RuleEvaluator<T> and IRuleEvaluator<T> go to Application with IRuleFieldProvider<T> and the field providers: rejected by the Domain rule, because their public Match signature takes IRuleFieldProvider<T>, an Application port; no rule is loosened. Final Domain membership is what the IL-reading Domain rule admits: a candidate that reaches anything beyond Models and the BCL goes to Application, recorded in the oracle list. Application: WorkItemService, IWorkItemService, BlockedItemService, IBlockedItemService, TeamDataService, ITeamDataService, RuleEvaluator, IRuleEvaluator, IRuleFieldProvider, WorkItemFieldProvider, FeatureFieldProvider, ParentPlacementReport with RecordPlacement, RefreshLogService, IRefreshLogService; Application/Dependencies: DependencyDecision, DependencyReconciler, DependencyRefreshReporter, FeatureDependencyRendering with DependenciesAsRead, WhatTheForecastWaitsFor and IDependencyDecision, IDependencyReconciler, IDependencyRefreshReporter, IWhatTheForecastWaitsFor. Application/Policies: the six handlers. Dependency types never sit flat in a layer: the sub-folders are the ones DependencySingleDecisionArchUnitTest's folder keys name.
```

#### D3

```text
Checker mapping: no state machine, temporal or fairness claim exists (behaviour-free relocation), so Quint, TLA+/TLC and Agda are not applicable and liveness is not applicable (no progress or scheduling change). Dependency-direction and placement claims are checked by IL-reading ArchUnit rules (ModuleRules.TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary) in the three slice oracles; behaviour preservation by RefreshesStayTheSameTest and bash Scripts/examine/bin/refresh-answers-compare (base 8a785450c vs HEAD), plus metrics-answers-compare, forecast-answers-compare, ef-model-unchanged and architecture-rules. No property-based law: the finite relocation has no broad input domain.
```

#### D4

```text
Paradigm object_oriented, smell catalogue nw-code-design-oo: every moved type keeps its owner and responsibility; no domain decision moves into a service, no class is created, split or merged. Carriers unchanged: decisions in DependencyHonourPolicy, DependencyCycleDetector, RuleEvaluator, SyncModeResolver; effects in WorkItemService, TeamDataService, RefreshLogService and the handlers; no per-process state is added.
```

#### D5

```text
Program.cs registrations and order stay; only using lines change, DomainEventHandlerOrderPinTest stays green and unmodified. Unit tests of moved types stay in place and gain using lines only; they move in feature C. Feature A's oracle files may take a using-line edit forced by the move, recorded as a target, never a semantic change. TheNamespaceThisEpicOwns already covers Modules.WorkItems; if WorkItemService's move makes it reach the forecast, the crafter returns that to the architect and never loosens the rule.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTOs, serialized responses, refresh results, metrics answers, forecasts, DI lifetimes, handler order and the schema stay the same. No bound on stored or validated inputs changes. Existing API, E2E and response tests stay green and unmodified apart from using lines.
```

#### K2

```text
One oracle file per slice; approved oracle and support files of this feature are never edited. Rules are never loosened: full-name rule lists are rewritten to the new names and still compared in both directions. Made-up offenders live in fixture namespaces under Lighthouse.Backend.Modules.WorkItemsFixtures. Slice dependencies: S2 after S1, S3 after S2. Per-test and property budgets are the defaults; no derogation.
```

#### K3

```text
Every dotnet command runs under flock -o on the shared lock file; declared commands omit the prefix. Code-fact was not consulted: file lists come from the directory listing and the IL membership test (stated limit). Rule strings, ILogger categories, perf harness, Stryker configs and test-selection scripts are grepped for moved full type names, not only using lines. Jev unauthorized: typesafe unavailable by refusal.
```

#### K4

```text
P4 is a completion obligation on the candidate: RefreshesStayTheSameTest and refresh-answers-compare are run by the final reviewer and EXAMINE; they are not an S1 support. Feature A's oracle files are not frozen supports here and may take using-line edits forced by the move.
```

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Reuse

```text
Layout oracles reuse TypesLiveInTheirLayer and DomainReachesOnlyTheKernelAndTheBaseLibrary as the Forecasting layout oracles do; CreateNew would duplicate the layer rules.
```

WorkItemsMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

Reuse

```text
Its Moves and Stays lists are the move list; it compares by short name and stays green through the move.
```

WorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/WorkItemService\.cs\:1

Reuse

```text
Existing owners of every moved responsibility keep their code; only folder and namespace change. CreateNew would duplicate refresh, blocked, dependency and rule decisions.
```

ForecastingDomainLayoutArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs\:1

Reuse

```text
Model for the three layout oracles: types named as strings, read from the compiled assembly, with made-up offenders in a fixture namespace.
```

ForecastingDomainLayoutArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingDomainLayoutArchUnitTest\.cs\:1

CreateNew

```text
Slice layout oracle for Work Items; the model oracle is Forecasting's, ModuleRules supplies the rules, no existing test owns this list.
```

SyncModeResolver

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/SyncModeResolver\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FetchFingerprint

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/FetchFingerprint\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

RuleEvaluator

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/RuleEvaluator\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IRuleEvaluator

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItemRules\/IRuleEvaluator\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyHonourPolicy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyHonourPolicy\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IDependencyHonourPolicy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyHonourPolicy\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyCycleDetector

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyCycleDetector\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyFacts

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyFacts\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

JIRA\_REGEX

Scripts\/test\-selection\/path\-classifier\.sh\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

FetchFingerprint

Scripts\/test\-selection\/test\-path\-classifier\.sh\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

RuleEvaluatorPurityTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RuleEvaluatorPurityTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

FetchShapingPropertyGuardTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FetchShapingPropertyGuardTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

ConnectionSaveArchitectureTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionSaveArchitectureTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

DependencySingleDecisionArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

BlockedItemSinglePathArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

mutate

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5687\-slice\-05\.json\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

mutate

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-4365\-slice\-02\.json\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

mutate

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

WorkItemsMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

WorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/WorkItemService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IWorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItems\/IWorkItemService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

BlockedItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/BlockedItemService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IBlockedItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItems\/IBlockedItemService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

TeamDataService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/TeamData\/TeamDataService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

ITeamDataService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/TeamData\/ITeamDataService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IRuleFieldProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItemRules\/IRuleFieldProvider\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

WorkItemFieldProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/WorkItemFieldProvider\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FeatureFieldProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/FeatureFieldProvider\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

ParentPlacementReport

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Parents\/ParentPlacementReport\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

RefreshLogService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RefreshLogService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IRefreshLogService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IRefreshLogService\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyDecision

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyDecision\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyReconciler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyReconciler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

DependencyRefreshReporter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyRefreshReporter\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FeatureDependencyRendering

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/FeatureDependencyRendering\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

WhatTheForecastWaitsFor

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/WhatTheForecastWaitsFor\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IDependencyDecision

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyDecision\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IDependencyReconciler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyReconciler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IDependencyRefreshReporter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyRefreshReporter\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

IWhatTheForecastWaitsFor

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IWhatTheForecastWaitsFor\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FlowMetricsInboundApplicationArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

FlowMetricsInboundDomainAndAdaptersArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

RepositoryLeakRatchetArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

HostUsersStillToMove

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

TimeInStateSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TimeInStateSeamArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

CalendarDayAnchorSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

WorkItemsRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

ForecastingRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

ForecastingAdaptersAndPoliciesLayoutArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

ForecastingUnitTestPlacementTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Extend

```text
Existing owner of the string, using line or path; extended in place.
```

WorkItemBlockedTransitionCaptureHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/WorkItemBlockedTransitionCaptureHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

WorkItemBlockedTransitionCloseHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/WorkItemBlockedTransitionCloseHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FeatureBlockedTransitionCaptureHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/FeatureBlockedTransitionCaptureHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

FeatureBlockedTransitionCloseHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/FeatureBlockedTransitionCloseHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

TeamDeletedRefreshLogCleanupHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/TeamDeletedRefreshLogCleanupHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

ConnectionFetchShapeChangedUpdateTriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs\:1

Reuse

```text
Moved unchanged; owner and responsibility stay.
```

### Impact

test\-file

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

UnchangedCompatible

```text
Membership lists stay; the new oracle only adds layout checks.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/SyncModeResolver\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/FetchFingerprint\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/RuleEvaluator\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItemRules\/IRuleEvaluator\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyHonourPolicy\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyHonourPolicy\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyCycleDetector\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyFacts\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

edit\-target

Scripts\/test\-selection\/path\-classifier\.sh\:1

Changed

```text
Line 13: the FetchFingerprint path follows the file to Modules/WorkItems/Domain, or the Jira tests stop being selected
```

edit\-target

Scripts\/test\-selection\/test\-path\-classifier\.sh\:1

Changed

```text
Lines 135-146 pin the FetchFingerprint path; follow the file
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Using lines only
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RuleEvaluatorPurityTest\.cs\:1

Changed

```text
Full-name strings follow RuleEvaluator and IRuleEvaluator
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FetchShapingPropertyGuardTest\.cs\:1

Changed

```text
Full-name strings follow FetchFingerprint
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionSaveArchitectureTest\.cs\:1

Changed

```text
Full-name strings follow FetchFingerprint
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
Folder keys and full names follow the Domain/Dependencies types; the rule's folder list stays exact
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs\:1

Changed

```text
Full-name strings follow RuleEvaluator
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5687\-slice\-05\.json\:1

Changed

```text
Mutate paths of FetchFingerprint and SyncModeResolver follow the files
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-4365\-slice\-02\.json\:1

Changed

```text
Mutate paths of DependencyHonourPolicy, DependencyCycleDetector, DependencyFacts follow the files
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json\:1

Changed

```text
Mutate path of DependencyHonourPolicy follows the file
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:1

Changed

```text
Only a using or fixture edit if the move forces it; the lists stay
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/WorkItemService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItems\/IWorkItemService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItems\/BlockedItemService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItems\/IBlockedItemService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/TeamData\/TeamDataService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/TeamData\/ITeamDataService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/WorkItemRules\/IRuleFieldProvider\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/WorkItemFieldProvider\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WorkItemRules\/FeatureFieldProvider\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Parents\/ParentPlacementReport\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/RefreshLogService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/IRefreshLogService\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyDecision\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyReconciler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/DependencyRefreshReporter\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/FeatureDependencyRendering\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Dependencies\/WhatTheForecastWaitsFor\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyDecision\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyReconciler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IDependencyRefreshReporter\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Dependencies\/IWhatTheForecastWaitsFor\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TimeInStateSeamArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs\:1

Changed

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5687\-slice\-05\.json\:1

Changed

```text
Mutate path of WorkItemService follows the file
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-4365\-slice\-02\.json\:1

Changed

```text
Mutate path of DependencyRefreshReporter follows the file
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json\:1

Changed

```text
Mutate path of DependencyRefreshReporter follows the file
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/WorkItemBlockedTransitionCaptureHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/WorkItemBlockedTransitionCloseHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/FeatureBlockedTransitionCaptureHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/FeatureBlockedTransitionCloseHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DomainEvents\/TeamDeletedRefreshLogCleanupHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

source\-file

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs\:1

Migrated

```text
Moves by git mv, namespace line only; consumers gain using lines.
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Changed

```text
Using lines only; registration order stays
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:1

Changed

```text
Full-name strings naming a moved handler follow it; no change if none
```

edit\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs\:1

Changed

```text
Full names naming a moved handler's tests follow it; no change if none
```

### Agreement

Lighthouse CLR namespaces of Work Items types

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

Consumer

Migrated

```text
DI registrations gain using lines only; order and lifetimes stay.
```

### Not applicable

contract_tests

```text
Producers and consumers are in one deployable and change in one commit; RefreshesStayTheSameTest and the examine answer-compare scripts observe unchanged answers.
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
Relocation of 43 Work Items types
```

Safety

Pass

DesignedNotBuilt

```text
IL-reading layout rules in each slice oracle exclude a Work Items type left in Services and a Domain type reaching beyond the kernel and the BCL
```

##### Evidence

None.

```text
Relocation of 43 Work Items types
```

Inhabitation

Pass

DesignedNotBuilt

```text
Each type has an existing source file and a named destination folder
```

##### Evidence

None.

```text
Relocation of 43 Work Items types
```

PublicReachability

Pass

DesignedNotBuilt

```text
Observation is the compiled assembly read by ArchUnit plus unchanged refresh and metrics answers through RefreshesStayTheSameTest and the examine compare scripts
```

##### Evidence

None.

```text
Relocation of 43 Work Items types
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no progress or scheduling behaviour is added or changed; files move byte for byte apart from the namespace line
```

##### Evidence

None.

### Risks

#### Y1

Bypass

```text
A file moves but a namespace, using line or string-named type (logger category, perf harness, Stryker path, reflection, test-selection script) is missed
```

```text
Refresh answers, metrics answers, forecasts and handler order
```

```text
Build errors until usings are fixed
```

```text
Silent change in results, logger categories or handler order; Jira tests no longer selected
```

```text
Reflection or string lookups of moved types not found by grep
```

crafter\; DomainEventHandlerOrderPinTest and RefreshesStayTheSameTest

#### Y2

Stressor

```text
Assumption: rule lists, scripts and Stryker configs name no other moved path
```

```text
Rule lists compare both ways
```

```text
A stale string fails a rule or test loudly
```

```text
Loosened rule or silently stale list or script
```

```text
Stryker configs of old features that name moved files
```

crafter and reviewer

#### Y3

Stressor

```text
Second walk: a Domain candidate turns out to reach Application, or WorkItemService's move makes the forecast-isolation rule reach the forecast
```

```text
Rules unloosened
```

```text
Candidate goes to Application, recorded in the oracle list; forecast reach returns to the architect
```

```text
Rule weakened or a Domain type reaching Application
```

```text
None known; the Domain rule names the offender
```

crafter\, architect

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

Create

```text
Oracle: Domain list, layout and kernel-only reach
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/SyncModeResolver\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/SyncModeResolver.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/FetchFingerprint\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/FetchFingerprint.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/Dependencies\/DependencyHonourPolicy\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyHonourPolicy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/Dependencies\/IDependencyHonourPolicy\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Dependencies/IDependencyHonourPolicy.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/Dependencies\/DependencyCycleDetector\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyCycleDetector.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Domain\/Dependencies\/DependencyFacts\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyFacts.cs
```

Scripts\/test\-selection\/path\-classifier\.sh

Extend

```text
Line 13: the FetchFingerprint path follows the file to Modules/WorkItems/Domain, or the Jira tests stop being selected
```

Scripts\/test\-selection\/test\-path\-classifier\.sh

Extend

```text
Lines 135-146 pin the FetchFingerprint path; follow the file
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RuleEvaluatorPurityTest\.cs

Extend

```text
Full-name strings follow RuleEvaluator and IRuleEvaluator
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FetchShapingPropertyGuardTest\.cs

Extend

```text
Full-name strings follow FetchFingerprint
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionSaveArchitectureTest\.cs

Extend

```text
Full-name strings follow FetchFingerprint
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Folder keys and full names follow the Domain/Dependencies types; the rule's folder list stays exact
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs

Extend

```text
Full-name strings follow RuleEvaluator
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5687\-slice\-05\.json

Extend

```text
Mutate paths of FetchFingerprint and SyncModeResolver follow the files
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-4365\-slice\-02\.json

Extend

```text
Mutate paths of DependencyHonourPolicy, DependencyCycleDetector, DependencyFacts follow the files
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json

Extend

```text
Mutate path of DependencyHonourPolicy follows the file
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

Extend

```text
Only a using or fixture edit if the move forces it; the lists stay
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsDomainLayoutArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule: every type on the Domain list lives in Lighthouse.Backend.Modules.WorkItems.Domain (Dependencies types in Domain.Dependencies), none remains in Services, and the Domain namespace reaches only the kernel and the BCL; any candidate the rule rejects is on the recorded Application list instead
```

```text
The rule passes on the real assembly; a made-up offender in Lighthouse.Backend.Modules.WorkItemsFixtures reaching an Application type fails the Domain rule and is named, and its compliant twin is not
```

```text
A Domain candidate left under Services.Implementation, a dependency type placed flat in a layer, or a Domain type referencing an Application or adapter type fails and names the type; FetchFingerprint's path in the test-selection scripts pointing at the old file fails test-path-classifier.sh
```

##### Promises

P1

P4

#### Prefactoring

```text
L1 pure move: git mv the Domain files, changing only the namespace line the move requires; the host commits it, then a separate edit commit for using lines, rule strings, the two test-selection scripts and Stryker paths. Makes the Application move in the next slice and feature C's module rules possible
```

```text
Refresh answers, metrics answers, forecasts, routes, DTOs and handler order; compile state restored by the edit commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefreshesStayTheSameTest
```

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

agreement

```text
No wire, schema or persisted contract is touched; CLR namespaces change inside one deployable in one commit.
```

reuse

```text
Pure move of existing files and one new layout oracle: each moved type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every target is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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
Domain types sit in Modules/WorkItems/Domain(/Dependencies), reach only the kernel and the BCL, none remains in Services; a made-up offender is named
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsDomainLayoutArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

Create

```text
Oracle: Application list and layout, full-name lists name the new places
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IWorkItemService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItems/IWorkItemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/BlockedItemService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/BlockedItemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IBlockedItemService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItems/IBlockedItemService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamData/TeamDataService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/ITeamDataService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/TeamData/ITeamDataService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleFieldProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItemRules/IRuleFieldProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemFieldProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItemRules/WorkItemFieldProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/FeatureFieldProvider\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItemRules/FeatureFieldProvider.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/ParentPlacementReport\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Parents/ParentPlacementReport.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/RefreshLogService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRefreshLogService\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IRefreshLogService.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyDecision\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyDecision.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyReconciler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyReconciler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/DependencyRefreshReporter\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/DependencyRefreshReporter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/FeatureDependencyRendering\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/FeatureDependencyRendering.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/WhatTheForecastWaitsFor\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Dependencies/WhatTheForecastWaitsFor.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IDependencyDecision\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Dependencies/IDependencyDecision.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IDependencyReconciler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Dependencies/IDependencyReconciler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IDependencyRefreshReporter\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Dependencies/IDependencyRefreshReporter.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Dependencies\/IWhatTheForecastWaitsFor\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Dependencies/IWhatTheForecastWaitsFor.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Using lines only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TimeInStateSeamArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/BlockedItemSinglePathArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingUnitTestPlacementTest\.cs

Extend

```text
Full-name strings and typeof usings naming a moved type are rewritten to the new place; lists still compared in both directions, no rule loosened. If the file names no moved type, no change.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5687\-slice\-05\.json

Extend

```text
Mutate path of WorkItemService follows the file
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-4365\-slice\-02\.json

Extend

```text
Mutate path of DependencyRefreshReporter follows the file
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\-config\.epic\-5792\-slice\-02\.json

Extend

```text
Mutate path of DependencyRefreshReporter follows the file
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RuleEvaluator\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItemRules/RuleEvaluator.cs; rejected by the Domain rule: its public Match signature takes IRuleFieldProvider<T>, an Application port
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/IRuleEvaluator\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItemRules/IRuleEvaluator.cs; rejected by the Domain rule: its public Match signature takes IRuleFieldProvider<T>, an Application port
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsApplicationLayoutArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefreshesStayTheSameTest
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule: every type on the Application list lives in Lighthouse.Backend.Modules.WorkItems.Application (dependency services and ports in Application.Dependencies), none remains in Services, and every rule list that names a moved type by full name names it in its new place and is still compared in both directions
```

```text
The layout rule passes on the real assembly; the forecast-isolation rule over Modules.WorkItems still passes with WorkItemService inside it; a made-up Application type left under Services.Implementation.WorkItems in the fixture namespace fails and is named
```

```text
A Work Items service or port left under Services, a dependency type placed flat in Application, a full-name list string naming the old namespace, or a loosened rule fails and names the offender; RefreshesStayTheSameTest or refresh-answers-compare answers differently from base
```

##### Promises

P2

P4

#### Prefactoring

```text
L1 pure move: git mv the Application files, changing only the namespace line; the host commits it, then a separate edit commit for using lines, full-name rule strings and Stryker paths. Makes feature C's Work Items module rules and the metrics reaction possible
```

```text
Refresh answers, metrics answers, forecasts, DI lifetimes and handler order; compile state restored by the edit commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefreshesStayTheSameTest
```

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

agreement

```text
No wire, schema or persisted contract is touched; CLR namespaces change inside one deployable in one commit.
```

reuse

```text
Pure move of existing files and one new layout oracle: each moved type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every target is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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
Application types sit in Modules/WorkItems/Application(/Dependencies), none remains in Services, full-name rule lists name the new places, forecast isolation holds over Modules.WorkItems
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsApplicationLayoutArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

Create

```text
Oracle: Policies list and layout, handler order unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/WorkItemBlockedTransitionCaptureHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/WorkItemBlockedTransitionCloseHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/FeatureBlockedTransitionCaptureHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/FeatureBlockedTransitionCloseHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DomainEvents/TeamDeletedRefreshLogCleanupHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs

Create

```text
git mv from Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/ConnectionFetchShapeChangedUpdateTriggerHandler.cs
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
Using lines only; registration order stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Full-name strings naming a moved handler follow it; no change if none
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.HostUsersStillToMove\.cs

Extend

```text
Full names naming a moved handler's tests follow it; no change if none
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsPoliciesLayoutArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~RefreshesStayTheSameTest
```

#### Supports

None.

#### Public oracle

```text
Read the compiled production assembly and apply the layout rule: the six handlers live in Lighthouse.Backend.Modules.WorkItems.Application.Policies and none remains in Services; run DomainEventHandlerOrderPinTest unmodified
```

```text
The layout rule passes and the order pin stays green, so the handlers still run in the registered order; a made-up handler left under Services in the fixture namespace fails and is named
```

```text
A handler left under Services.Implementation.DomainEvents or BackgroundServices.Update fails and is named; reordering a registration in Program.cs fails DomainEventHandlerOrderPinTest
```

##### Promises

P3

P4

#### Prefactoring

```text
L1 pure move: git mv the six handlers, changing only the namespace line; the host commits it, then a separate edit commit for using lines. Registration lines in Program.cs are not touched. Makes feature C's reaction to TeamDataRefreshed a Work Items policy
```

```text
Handler order, refresh log clean-up, blocked-transition capture and close, update trigger on fetch-shape change
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DomainEventHandlerOrderPinTest
```

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

agreement

```text
No wire, schema or persisted contract is touched; CLR namespaces change inside one deployable in one commit.
```

reuse

```text
Pure move of existing files and one new layout oracle: each moved type keeps its one owner and only folder and namespace change; reuse owners are recorded in the shared reuse rows (the destination files do not exist yet).
```

impact

```text
Consumer and test impact for every target is recorded once in the shared impact rows: consumers only gain using lines, tests stay in place, Program.cs gets using lines only.
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
The six handlers sit in Modules/WorkItems/Application/Policies, none remains in Services; handler order pin unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemsPoliciesLayoutArchUnitTest
```

##### Contract tests

None.

## ADRs

None.
