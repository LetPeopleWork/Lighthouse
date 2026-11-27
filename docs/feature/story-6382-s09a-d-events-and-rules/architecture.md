# Architecture brief: story\-6382\-s09a\-d\-events\-and\-rules

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Feature D adopts the Story DESIGN unchanged (docs/feature/story-6382-s09a-context-contracts/feature-delta.md): D-0 no schema, route, DTO or user-visible change except the RX1 failure path; D-3 R-C1 owns surface, the per-module rules own direction, one owner per edge; D-4 Models is the kernel; D-5 event owners; D-6 RX1 converted, RX2-RX5 stay with their owners; D-9 domain-event handlers stay in the one ordered block in Program; D-11 ADR-270 and ARCHITECTURE.md section 4 amended, no new ADR; D-12 commit per slice, one push for the feature; D-13 payoff note written, nothing implemented; D-15 mutation last, at the end of the Story; D-16 no formal model. Maintainer decision of 2026-11-26 (open question 8): RX1's changed failure path is accepted as behaviour-free.
```

#### D2

```text
Formal verification (D-16, confirmed): no Quint, TLA+ or Agda model. No model for R-C1, R-C5 and the one-owner law: they are relations over finite closed sets (compiled types, contexts, inline lists) executed exhaustively on the IL by the Architecture tests, with no state, protocol, concurrency or crash window. No model for RX1: one in-process publish and handle inside the round's finally, the dispatcher awaiting its handlers one after the other in the same call; the refresh log is append-only, one row per round, no other actor reads or changes that row, and the crash window (process dies after the round, before the row) is the same as today's direct call. The outcome mapping is a total function over a closed four-value domain, checked exhaustively by the S2 oracle's four rows. No liveness claim: nothing is eventual, the handler completes before PublishAsync returns. PBT: not applicable, every domain is closed and finite and enumerated.
```

#### D3

```text
Cross-layer algebra. Domain: ForecastRoundOutcome is a closed choice Succeeded | Failed | Cancelled | Skipped (the four flag combinations today's round can produce). Application: Forecasting decides the round's outcome and publishes ForecastRoundFinished; Work Items decides how its refresh log records a forecast round. Adapter: ForecastUpdater (Forecasting.Adapters.Background) publishes through IDomainEventDispatcher; DomainEventDispatcher (platform lane) runs handlers in a new scope and logs and swallows a handler's exception. Infrastructure: RefreshLogService writes the RefreshLog row through its repository, unchanged. Observations: the refresh-log rows read through IRefreshLogService.GetRefreshLogs, the dispatcher's error log, the Architecture test verdicts.
```

#### D4

```text
Carriers and owners. Event types: one sealed record per file in Modules/<Owner>/Contracts, namespace = folder, implementing the kernel's IDomainEvent (Models.Events). ForecastRoundFinished and ForecastRoundOutcome: Forecasting.Contracts, one type per file. The refresh-log entry: kernel RefreshLog, built by ForecastRoundFinishedRefreshLogHandler (Work Items, Application/Policies) and written through Work Items' IRefreshLogService. Errors: a throwing handler is an exception caught and logged by DomainEventDispatcher (platform lane); no new error type. Per-process state: none added; the handler is Scoped like every other handler.
```

#### D5

```text
Ports. Driving: unchanged (controllers, hosted services, the update queue). The forecast round is driven at IForecastUpdater.TriggerImmediateUpdate with the harness's scope factory, no host (test-writing skill rule 1, background job at the port). Driven: IDomainEventDispatcher (platform lane) and IRefreshLogService (Work Items' Contracts), both reused unchanged. One new in-process event path: ForecastUpdater -> IDomainEventDispatcher -> ForecastRoundFinishedRefreshLogHandler -> IRefreshLogService. No new external call.
```

#### D6

```text
Already done by feature C, so not repeated here: Organisation set-up's open host was narrowed to its Contracts in feature C S5 (feature C decision D16: ModuleRules owns SetUpContracts and SetUpBehindItsContracts; OrganisationSetupIsAnOpenHostArchUnitTest reads ContextContractsMembership.ContractsOf("OrganisationSetup")). The Story's D3 line "S03i's open-host oracle rewritten to Contracts" is therefore met; S3 changes only the two Tracker integration list assertions of that test, which P3 contradicts.
```

#### D7

```text
R-C1's list (ContextContractsArchUnitTest.CrossContextEdgesOutsideContracts) holds eight edges at HEAD and none of them is ForecastUpdater -> IRefreshLogService: IRefreshLogService moved to Work Items' Contracts in feature C S3, so that edge is legal under R-C1. The Story's "R-C1 entry deleted" for RX1 has nothing to delete; S2's oracle asserts instead, on the IL, that ForecastUpdater names no IRefreshLogService.
```

#### D8

```text
Context map: Work Items reacting to ForecastRoundFinished makes Work Items' Application.Policies depend on Forecasting's Contracts, the reverse of the ADR-270 edge Work Items -> Forecasting (customer/supplier, by events). The edge is by event only, through Contracts, in one deployable; S3 records it on ADR-270's map as Forecasting -> Work Items, by events (ForecastRoundFinished).
```

### Constraints

#### K1

```text
Behaviour unchanged except who writes a forecast round's refresh-log entry and that a failing write no longer fails the round (P1-P3, D-0). No schema, migration, route, DTO, usage-data event or registration-lifetime change; frontend, E2E and Lighthouse-Clients untouched.
```

#### K2

```text
IDomainEvent stays in Lighthouse.Backend.Models.Events; the 15 event types keep their names, shapes and positional parameters; only namespace and file path change.
```

#### K3

```text
Out of scope: any other cross-context command (RX2-RX5 keep their owners), any fix of an R-C1 residue edge, mutation testing (end of the Story).
```

#### K4

```text
Lists by full type name, compared both ways, only shrink, with one exception: an S1 rename may restate a pre-existing listed edge from code that is not a module (API, Services, Data, platform lane) under the event's new name. No module-to-module entry is ever added to a per-module list.
```

#### K5

```text
Tests: no nWave scenario tags; backend acceptance scenarios drive the application-service port; the test-writing skill and its backend half win over nWave's timing and PBT density rules. Each new test within the default per-test budget, no infrastructure suite, no derogation; the Architecture tests reuse the IL loaded once per assembly.
```

#### K6

```text
Do not run the full backend suite under parallel load or any live connector category; the filtered full gate (TestCategory exclusions of CLAUDE.md) runs once before the push, by the host.
```

#### K7

```text
Program.cs changes in S1 and S2, so the feature's single push runs every live connector suite once in CI (D-12).
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Every producer and consumer of the touched contracts (the 15 event types, ForecastRoundFinished, IRefreshLogService, RefreshLog) lives in the one backend deployable and changes in the same commit; nothing is serialised, persisted in a new shape or sent on a wire. The S2 oracle runs the real producer through the real consumer at the service port.
```

### Boundaries

```text
Architecture rules: the compiled production assembly read through LighthouseArchitecture.Production / IlDependencies (in-process; nearest argv: dotnet test ... --filter FullyQualifiedName~Architecture). RX1: IForecastUpdater.TriggerImmediateUpdate over a ServicePortHarness scope factory, no host.
```

#### Driven ports

```text
IDomainEventDispatcher (platform lane, unchanged)
```

```text
IRefreshLogService (Work Items Contracts, unchanged)
```

```text
Module types reach another module only through its Contracts (R-C1) and only where the target's per-module direction rule admits the caller's namespace; Contracts reach only the kernel, the BCL, abstractions and other Contracts (R-C2); the kernel and the platform lane reach no module.
```

#### Failures

None.

### Observations

```text
The refresh-log rows after a forecast round, read through Work Items' IRefreshLogService.GetRefreshLogs in a fresh harness scope
```

IEnumerable\<RefreshLog\>

```text
which type wrote the row; that is observed separately on the IL (ForecastUpdater names no IRefreshLogService)
```

```text
Architecture test verdict: exit code and the failure message naming WHAT (source, target, list), WHY and HOW
```

NUnit outcome \+ message

```text
code reached only by reflection or by string (no IL edge)
```

```text
The dispatcher's error log entry when a handler throws (handler type name, event type name)
```

captured ILogger\<DomainEventDispatcher\> entry

```text
a handler that threw before writing from one that threw after a partial write
```

### Equality

refresh\-log entry

```text
Two RefreshLog rows are equal when Type, EntityId, EntityName, ItemCount, Mode, RecordsScanned, RecordsFetched, Success, Cancelled and Skipped are equal and ExecutedAt equals the round's finish instant on the scenario's TimeProvider; Id is ignored and DurationMs is compared as non-negative and taken from the round.
```

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
Round outcomes Succeeded, Failed, Cancelled, Skipped
```

Forecasting

Excluded

```text
Refresh-log flag combinations a round cannot produce (Success and Cancelled, Skipped without Success): unrepresentable in ForecastRoundOutcome
```

Forecasting

Unknown

```text
Process dies after the round's save and before the handler's write: no row, as today
```

Forecasting

### Models

None.

### Closure

None.

### Risks

#### Y1

Stressor

```text
Negated assumption: an event publish behaves like the call it replaces (E7)
```

```text
the refresh-log row of every completed, failed, cancelled or skipped round
```

```text
a throwing write is logged by the dispatcher and the round completes without its row; the next round logs again
```

```text
a log failure failing or stopping a round; a round finishing without publishing
```

```text
a round whose log write failed has no row (accepted by the maintainer 2026-11-26)
```

Forecasting \/ Work Items

#### Y2

Stressor

```text
Negated assumption: a cancelled round still reaches the handler (the operator's token is cancelled)
```

```text
the Cancelled row
```

```text
none
```

```text
publishing with the round's cancelled token so the handler or the write is skipped
```

```text
none: ForecastUpdater publishes with CancellationToken.None, pinned by the oracle's cancelled row
```

Forecasting

#### Y3

Stressor

```text
Negated assumption: moving an event leaves every boundary rule seeing its readers (E5)
```

```text
every rule keeps reading the event's edges
```

```text
direction patterns of the publisher admit the reacting contexts' Application.Policies
```

```text
a rule silently passing over a moved type; a module-to-module list entry added
```

```text
none known; the Architecture run after S1 names each failing rule
```

S1 crafter

#### Y4

SinglePointOfFailure

```text
DomainEventDispatcher resolves the handler; a missing registration loses every forecast refresh-log row silently
```

```text
one handler of ForecastRoundFinished registered
```

```text
none
```

```text
a round with no row and no error
```

```text
none: the order pin names ForecastRoundFinished's handler list and the S2 oracle builds the handlers through Program's registration
```

S2

#### Y5

Bypass

```text
Another Forecasting type calling IRefreshLogService directly (legal under R-C1, Contracts edge)
```

```text
one writer per forecast round's row
```

```text
none
```

```text
two rows for one round
```

```text
only ForecastUpdater is checked on the IL; a new Forecasting writer would pass R-C1 and Forecasting's rules
```

Forecasting \(Epic 6474 refresh paths\)

#### Y6

Stressor

```text
Second set, walkthrough only (not executed): an undefined ForecastRoundOutcome value cast from an int; a later Story adding a 16th event published by a module; a per-module list entry built by string concatenation rather than a literal; PortfolioFeaturesRefreshed published by Services code
```

```text
S1 and S3 laws hold for future members
```

```text
the undefined outcome throws in the handler's total switch and the dispatcher logs it
```

```text
a new event or list entry passing unseen
```

```text
S1's law is generic over every IDomainEvent implementer (a 16th event needs no oracle edit); S3's law reads list values from the compiled test assembly, not source text, so concatenated entries are seen; Services publishers stay outside R-C1 (D-4, owner S04b and Epic 6474 slice 12)
```

Story 6382

### Architectural tests

#### T1

```text
R-C1-R-C3 stay green with the residue list; every Architecture rule passes after each slice
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

#### T2

```text
Domain-event handler order is pinned per event, ForecastRoundFinished included after S2
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~DomainEventHandlerOrderPinTest"
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemTransitioned\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBlocked\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemUnblocked\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBecameStale\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureBlocked\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureUnblocked\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/TeamDataRefreshed\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/PortfolioFeaturesRefreshed\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/PortfolioForecastsUpdated\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/TeamDeleted\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/PortfolioDeleted\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/BlackoutConfigurationChanged\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureOrderingPolicyChanged\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureRankChanged\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/ConnectionFetchShapeChanged\.cs

Create

```text
git mv from Models/Events; namespace line only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/FeatureOrderingApplier\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice02JiraCloudTeamDeltaSpecifications\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice03JiraCloudPortfolioDeltaSpecifications\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/Slice01AnnouncedAndDailyScenarios\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/Slice01StaleSignalSpecifications\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionFetchShapeChangedSinglePublisherArchUnitTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsStringsTheMoveRenames\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ReconstructionMemoTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/AppSettingServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionHandlerTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureTests\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherGoldTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDataRefreshedGoldTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs

Extend

```text
using line or old event name/path literal only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
direction pattern admits the Application.Policies of contexts reacting to its event
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

Extend

```text
direction pattern or non-module outsider entry restated under the event's new name
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

None.

None.

None.

#### Public oracle

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Architecture over the compiled assembly
```

```text
RED before the move: the oracle names each of the 15 events still in Models.Events. GREEN after: each event resolves to exactly one type in its owner's Contracts, Models.Events holds only IDomainEvent, and every other Architecture rule passes.
```

```text
An event left in Models.Events, or placed in a context's Contracts other than its publisher's (planted fixture), fails with source, owner and the HOW.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

```text
The S1 oracle's law is generic so no later slice edits it: every type implementing IDomainEvent, except IDomainEvent itself, lives in Lighthouse.Backend.Modules.<C>.Contracts where C is the context whose code constructs it; for an event constructed only outside the modules (TeamDeleted by API.TeamController, PortfolioFeaturesRefreshed by PortfolioUpdater) C is the owner named inline by ADR-270 Decision 3 (D-5); the 15 events of D-5 are named inline with their owner and compared one way (each found at its owner); no type but IDomainEvent is left in Models.Events. ConnectionFetchShapeChanged belongs to Organisation set-up (open question 5 default, its publisher ConnectionSaveService). ContextContractsMembership's event rows (A1) are reused as the old/new namespace map.
```

```text
Direction after the move: a handler in context Y naming an event now in X.Contracts is a downstream reaction, legal by ADR-270 (upstream publishes, downstream reacts). Where X's per-module direction rule does not admit Y (measured at HEAD: Forecasting admits only the API and Refinement, so Promises' PortfolioForecastsUpdated handlers), the crafter widens X's legal-callers pattern by Y's Application.Policies namespace only, never by a list entry. Non-module publishers (API, Services) keep or restate their existing outsider entries under the new name.
```

##### Constraints

None.

##### Reuse

ContextContractsMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembership\.cs\:74

Reuse

```text
already maps each of the 15 events to its owner's Contracts (A1); the oracle reads it instead of a second map
```

ContextContractsStringsTheMoveRenames

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsStringsTheMoveRenames\.cs\:12

Reuse

```text
already holds the old/new event literals of the open-host test and OrganisationSetupMembership; never edited
```

IDomainEvent

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Events\/IDomainEvent\.cs\:1

Reuse

```text
stays in the kernel (D-5); every event keeps implementing it
```

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:1

Reuse

```text
existing wording and IL edge helpers for the oracle's refusal message
```

WorkItemTransitioned

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemTransitioned\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/WorkItemTransitioned.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

WorkItemBlocked

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBlocked\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/WorkItemBlocked.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

WorkItemUnblocked

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemUnblocked\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/WorkItemUnblocked.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

WorkItemBecameStale

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBecameStale\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/WorkItemBecameStale.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

FeatureBlocked

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureBlocked\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/FeatureBlocked.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

FeatureUnblocked

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureUnblocked\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/FeatureUnblocked.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

TeamDataRefreshed

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/TeamDataRefreshed\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/TeamDataRefreshed.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

PortfolioFeaturesRefreshed

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/PortfolioFeaturesRefreshed\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/PortfolioFeaturesRefreshed.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

PortfolioForecastsUpdated

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/PortfolioForecastsUpdated\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/PortfolioForecastsUpdated.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

TeamDeleted

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/TeamDeleted\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/TeamDeleted.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

PortfolioDeleted

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/PortfolioDeleted\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/PortfolioDeleted.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

BlackoutConfigurationChanged

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/BlackoutConfigurationChanged\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/BlackoutConfigurationChanged.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

FeatureOrderingPolicyChanged

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureOrderingPolicyChanged\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/FeatureOrderingPolicyChanged.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

FeatureRankChanged

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureRankChanged\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/FeatureRankChanged.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

ConnectionFetchShapeChanged

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/ConnectionFetchShapeChanged\.cs\:1

CreateNew

```text
New file at the owner's Contracts home, made by git mv of Models/Events/ConnectionFetchShapeChanged.cs; the record itself is reused unchanged (namespace line only), no other owner exists
```

PortfolioController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:33

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:32

Extend

```text
existing owner edited in place: using line or event literal only
```

BlackoutConfigurationChangedMetricsInvalidationHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

BlockedCountSnapshotRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:13

Extend

```text
existing owner edited in place: using line or event literal only
```

DemoBlockedHistoryBackfillHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:25

Extend

```text
existing owner edited in place: using line or event literal only
```

DemoPercentilesBackfillHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:28

Extend

```text
existing owner edited in place: using line or event literal only
```

PercentilesOverTimeRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioFeaturesRefreshedMetricsInvalidationHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

ProcessBehaviorRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataRefreshedTeamMetricsUpdateHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

ReconstructionMemo

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:21

Extend

```text
existing owner edited in place: using line or event literal only
```

ForecastUpdater

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:22

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureOrderingPolicyChangedForecastTriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs\:21

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureRankChangedForecastTriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs\:16

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataRefreshedForecastTriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs\:8

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDeletedForecastRetriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

BlackoutPeriodService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

ConnectionSaveService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveService\.cs\:12

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureRankingService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureOrderingApplier

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/FeatureOrderingApplier\.cs\:15

Extend

```text
existing owner edited in place: using line or event literal only
```

RecurringBlackoutRuleService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleService\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryForecastPublishingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs\:21

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryMetricSnapshotRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

ConnectionFetchShapeChangedUpdateTriggerHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureBlockedTransitionCaptureHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs\:8

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureBlockedTransitionCloseHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioDeletedOrphanedFeatureCleanupHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandler\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDeletedRefreshLogCleanupHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:8

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemBlockedTransitionCaptureHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs\:8

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemBlockedTransitionCloseHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:7

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:23

Extend

```text
existing owner edited in place: using line or event literal only
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:79

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioUpdater

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:23

Extend

```text
existing owner edited in place: using line or event literal only
```

RefreshesStayTheSameTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:54

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamMetricsFollowTheTeamRefreshTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:60

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioBlockedHistoryAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:36

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryArchiveClosurePinIntegrationTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:26

Extend

```text
existing owner edited in place: using line or event literal only
```

Slice00OneForecastPerBatchScenarios

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:49

Extend

```text
existing owner edited in place: using line or event literal only
```

FasterUpdatesAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:56

Extend

```text
existing owner edited in place: using line or event literal only
```

using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice02JiraCloudTeamDeltaSpecifications\.cs\:1

Extend

```text
existing owner edited in place: using line or event literal only
```

using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice03JiraCloudPortfolioDeltaSpecifications\.cs\:1

Extend

```text
existing owner edited in place: using line or event literal only
```

PercentilesOverTimeAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:27

Extend

```text
existing owner edited in place: using line or event literal only
```

ReconstructOverTimeHistoryAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:48

Extend

```text
existing owner edited in place: using line or event literal only
```

SkipUnchangedForecastsAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:45

Extend

```text
existing owner edited in place: using line or event literal only
```

Slice01AnnouncedAndDailyScenarios

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/Slice01AnnouncedAndDailyScenarios\.cs\:15

Extend

```text
existing owner edited in place: using line or event literal only
```

using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/Slice01StaleSignalSpecifications\.cs\:1

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamFeatureSizeAcceptanceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs\:33

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioControllerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:25

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamControllerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:30

Extend

```text
existing owner edited in place: using line or event literal only
```

ConnectionFetchShapeChangedSinglePublisherArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionFetchShapeChangedSinglePublisherArchUnitTest\.cs\:12

Extend

```text
existing owner edited in place: using line or event literal only
```

DomainEventHandlerOrderPinTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:18

Extend

```text
existing owner edited in place: using line or event literal only
```

InstantsUnaffectedByZoneTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:35

Extend

```text
existing owner edited in place: using line or event literal only
```

OrganisationSetupIsAnOpenHostArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs\:23

Extend

```text
existing owner edited in place: using line or event literal only
```

OrganisationSetupMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:53

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemsDoesNotCommandFlowMetricsArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs\:16

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
existing owner edited in place: using line or event literal only
```

BlackoutConfigurationChangedMetricsInvalidationHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

BlockedCountSnapshotRecordingHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:23

Extend

```text
existing owner edited in place: using line or event literal only
```

DemoBlockedHistoryBackfillHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:22

Extend

```text
existing owner edited in place: using line or event literal only
```

DemoPercentilesBackfillHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs\:19

Extend

```text
existing owner edited in place: using line or event literal only
```

PercentilesOverTimeRecordingHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs\:22

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

ProcessBehaviorRecordingHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs\:23

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataRefreshedTeamMetricsUpdateHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandlerTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

ReconstructionMemoTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ReconstructionMemoTest\.cs\:8

Extend

```text
existing owner edited in place: using line or event literal only
```

ForecastUpdaterTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:32

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureOrderingPolicyChangedForecastTriggerHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:29

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureRankChangedForecastTriggerHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:28

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataRefreshedForecastTriggerHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandlerTest\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDeletedForecastRetriggerHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandlerTest\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

using

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/AppSettingServiceTest\.cs\:1

Extend

```text
existing owner edited in place: using line or event literal only
```

BlackoutPeriodServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodServiceTest\.cs\:12

Extend

```text
existing owner edited in place: using line or event literal only
```

ConnectionSaveServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveServiceTest\.cs\:17

Extend

```text
existing owner edited in place: using line or event literal only
```

RecurringBlackoutRuleServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleServiceTest\.cs\:12

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryMetricValuesProjectorTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs\:18

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryForecastPublishingHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandlerTest\.cs\:22

Extend

```text
existing owner edited in place: using line or event literal only
```

DeliveryMetricSnapshotRecordingHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs\:29

Extend

```text
existing owner edited in place: using line or event literal only
```

ConnectionFetchShapeChangedUpdateTriggerHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

FeatureBlockedTransitionHandlerTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionHandlerTests\.cs\:14

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioDeletedOrphanedFeatureCleanupHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs\:17

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDeletedRefreshLogCleanupHandlerTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemBlockedTransitionCaptureTests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureTests\.cs\:16

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs\:10

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemServiceTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:22

Extend

```text
existing owner edited in place: using line or event literal only
```

DomainEventDispatcherGoldTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherGoldTest\.cs\:11

Extend

```text
existing owner edited in place: using line or event literal only
```

DomainEventDispatcherTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherTest\.cs\:9

Extend

```text
existing owner edited in place: using line or event literal only
```

PortfolioUpdaterTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:32

Extend

```text
existing owner edited in place: using line or event literal only
```

SnapshotRecordedDayInstanceZoneTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:45

Extend

```text
existing owner edited in place: using line or event literal only
```

TeamDataRefreshedGoldTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDataRefreshedGoldTest\.cs\:12

Extend

```text
existing owner edited in place: using line or event literal only
```

WorkItemDomainEventsGoldTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs\:18

Extend

```text
existing owner edited in place: using line or event literal only
```

SharedHostCleanup

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs\:18

Extend

```text
existing owner edited in place: using line or event literal only
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:44

Extend

```text
existing owner edited in place: using line or event literal only
```

OrganisationSetupModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs\:16

Extend

```text
existing owner edited in place: using line or event literal only
```

##### Impact

production\-event\-types

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Events\/TeamDeleted\.cs\:1

Migrated

```text
15 event files move to their owners' Contracts; namespace line only
```

production\-and\-test\-consumers

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1418

Changed

```text
93 files naming an event: using line or literal only (Program.cs handler block, handlers, publishers, tests); measured by text search, the build and the Architecture run decide
```

kernel\-dispatch

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/DomainEventDispatcher\.cs\:1

UnchangedCompatible

```text
names only IDomainEvent
```

schema\-wire

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:1

UnchangedCompatible

```text
events are not EF-mapped and not serialised (D-0)
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemTransitioned\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBlocked\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemUnblocked\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/WorkItemBecameStale\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureBlocked\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/FeatureUnblocked\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/TeamDataRefreshed\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/PortfolioFeaturesRefreshed\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/PortfolioForecastsUpdated\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/TeamDeleted\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/PortfolioDeleted\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/BlackoutConfigurationChanged\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureOrderingPolicyChanged\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/FeatureRankChanged\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Contracts\/ConnectionFetchShapeChanged\.cs\:1

Migrated

```text
event record moved, namespace line only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioController\.cs\:33

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamController\.cs\:32

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandler\.cs\:9

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandler\.cs\:13

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandler\.cs\:25

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandler\.cs\:28

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:11

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandler\.cs\:9

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandler\.cs\:11

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandler\.cs\:9

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:21

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:22

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandler\.cs\:21

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandler\.cs\:16

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandler\.cs\:8

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandler\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodService\.cs\:11

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveService\.cs\:12

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/FeatureRankingService\.cs\:11

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/OptionalFeatures\/FeatureOrderingApplier\.cs\:15

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleService\.cs\:9

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandler\.cs\:21

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandler\.cs\:10

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandler\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCaptureHandler\.cs\:8

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionCloseHandler\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandler\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:8

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureHandler\.cs\:8

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCloseHandler\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/TeamDataService\.cs\:7

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs\:23

Changed

```text
using line or event literal only
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdater\.cs\:23

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:54

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs\:60

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BlockedItems\/PortfolioBlockedHistoryAcceptanceTest\.cs\:36

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DeliveryArchiveClosurePinIntegrationTest\.cs\:26

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/DependencyAwareForecasting\/Slice00OneForecastPerBatchScenarios\.cs\:49

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/FasterUpdatesAcceptanceTest\.cs\:56

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice02JiraCloudTeamDeltaSpecifications\.cs\:1

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/FasterUpdates\/Slice03JiraCloudPortfolioDeltaSpecifications\.cs\:1

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/PercentilesOverTimeAcceptanceTest\.cs\:27

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/ReconstructOverTimeHistoryAcceptanceTest\.cs\:48

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/SkipUnchangedForecastsAcceptanceTest\.cs\:45

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/SkipUnchangedForecasts\/Slice01AnnouncedAndDailyScenarios\.cs\:15

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Staleness\/Slice01StaleSignalSpecifications\.cs\:1

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TeamFeatureSize\/TeamFeatureSizeAcceptanceTest\.cs\:33

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:25

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamControllerTest\.cs\:30

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionFetchShapeChangedSinglePublisherArchUnitTest\.cs\:12

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsStringsTheMoveRenames\.cs\:8

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:18

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/InstantsUnaffectedByZoneTest\.cs\:35

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs\:23

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembership\.cs\:53

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs\:16

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:36

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlackoutConfigurationChangedMetricsInvalidationHandlerTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/BlockedCountSnapshotRecordingHandlerTests\.cs\:23

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoBlockedHistoryBackfillHandlerTests\.cs\:22

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/DemoPercentilesBackfillHandlerTests\.cs\:19

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandlerTests\.cs\:22

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/ProcessBehaviorRecordingHandlerTests\.cs\:23

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/Policies\/TeamDataRefreshedTeamMetricsUpdateHandlerTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/ReconstructionMemoTest\.cs\:8

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:32

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureOrderingPolicyChangedForecastTriggerHandlerTest\.cs\:29

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/FeatureRankChangedForecastTriggerHandlerTest\.cs\:28

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDataRefreshedForecastTriggerHandlerTest\.cs\:11

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Application\/Policies\/TeamDeletedForecastRetriggerHandlerTest\.cs\:9

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/AppSettingServiceTest\.cs\:1

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/BlackoutPeriodServiceTest\.cs\:12

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/Connections\/ConnectionSaveServiceTest\.cs\:17

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/OrganisationSetup\/Application\/RecurringBlackoutRuleServiceTest\.cs\:12

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryMetricValuesProjectorTest\.cs\:18

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryForecastPublishingHandlerTest\.cs\:22

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/Policies\/DeliveryMetricSnapshotRecordingHandlerTest\.cs\:29

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/FeatureBlockedTransitionHandlerTests\.cs\:14

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs\:17

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandlerTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/WorkItemBlockedTransitionCaptureTests\.cs\:16

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/TeamDataServiceTest\.cs\:10

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/WorkItemServiceTest\.cs\:22

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherGoldTest\.cs\:11

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Platform\/DomainEvents\/DomainEventDispatcherTest\.cs\:9

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/PortfolioUpdaterTest\.cs\:32

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/SnapshotRecordedDayInstanceZoneTest\.cs\:45

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDataRefreshedGoldTest\.cs\:12

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/WorkItemDomainEventsGoldTest\.cs\:18

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/SharedHostCleanup\.cs\:18

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:44

Changed

```text
using line or event literal only
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs\:16

Changed

```text
using line or event literal only
```

##### Agreement

None.

##### Not applicable

agreement

```text
Namespace-only move of in-process record types in one deployable; no wire, persisted or config form; every producer and consumer changes in the same commit and the compiler rejects a missed one.
```

prefactoring

```text
Per touched file: each of the 15 event files holds one record (not-applicable, moved whole); every Extend file changes a using line or a literal only, no structural edit, so no layout move is due; Program.cs holds one type. The move is itself the refactoring, in a refactor(contracts) commit.
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

###### L6

```text
For every type implementing IDomainEvent other than IDomainEvent: its namespace is Lighthouse.Backend.Modules.<C>.Contracts, C being the context whose types construct it (or ADR-270 Decision 3's owner when only non-module code constructs it); Models.Events holds IDomainEvent only.
```

Invariant

Domain

DomainEventsLiveWithTheirPublisherArchUnitTest

###### Treatment

Finite

###### Cases

```text
each of the 15 D-5 events at its owner
```

```text
Models.Events holds only IDomainEvent
```

```text
planted falsifier: a made-up event constructed in one fixture context but declared in another context's Contracts is named WHAT/WHY/HOW
```

```text
closed finite set read from the compiled assembly
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
event placement
```

Safety

Pass

DesignedNotBuilt

```text
the oracle's law excludes an event outside its publisher's Contracts, executed on the IL
```

###### Evidence

None.

```text
event placement
```

Inhabitation

Pass

DesignedNotBuilt

```text
each of the 15 owners' Contracts folders exists (feature C) and receives its files by git mv
```

###### Evidence

None.

```text
event placement
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled assembly -> IlDependencies -> oracle -> dotnet test exit and message
```

###### Evidence

None.

```text
event placement
```

Liveness

Pass

DesignedNotBuilt

```text
no liveness claim: a static placement relation, nothing eventual
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T4

```text
R-C5: each domain event type lives in the Contracts of the context that publishes it; none but IDomainEvent in Models.Events
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundFinished\.cs

Create

```text
the event: PortfolioId, PortfolioName, ItemCount, DurationMs, FinishedAt, Outcome
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundOutcome\.cs

Create

```text
closed choice Succeeded | Failed | Cancelled | Skipped, one type per file
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ForecastRoundFinishedRefreshLogHandler\.cs

Create

```text
writes the same RefreshLog row through IRefreshLogService
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs

Extend

```text
publishes ForecastRoundFinished where LogForecastRefresh was called; drops IRefreshLogService
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
registers the handler Scoped in the one ordered handler block
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
Forecasting's legal callers admit Work Items' Application.Policies
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs

Extend

```text
unit rows capture the published ForecastRoundFinished instead of the refresh-log mock
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

Extend

```text
only if its placement list names the Work Items handlers
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~ForecastRoundsKeepTheirRefreshLogEntryTest|FullyQualifiedName~DomainEventHandlerOrderPinTest|FullyQualifiedName~ForecastUpdaterTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

None.

None.

#### Public oracle

```text
IForecastUpdater.TriggerImmediateUpdate(portfolioId) on a ServicePortHarness built as Program builds the round, once per outcome (succeeded, failed, cancelled, skipped) and once with a throwing refresh-log write; plus the IL of ForecastUpdater
```

```text
Each round leaves exactly one Forecast row equal to today's row for its outcome, written by the handler; the throwing write is logged by the dispatcher and the round completes; ForecastUpdater names no IRefreshLogService; the order pin lists ForecastRoundFinishedRefreshLogHandler. RED before craft: ForecastRoundFinished does not exist / ForecastUpdater still names IRefreshLogService.
```

```text
Publishing with the round's token (cancelled row missing), a wrong flag mapping (skipped row with Success false), a missing registration (no row), or ForecastUpdater still writing the row directly each fails a named row.
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

```text
ForecastUpdater keeps the round's flags and, in the same finally and at the same point, publishes ForecastRoundFinished(portfolio.Id, portfolio.Name, itemCount, stopwatch ms, CurrentInstant, outcome) through IDomainEventDispatcher with CancellationToken.None, so a cancelled round still writes its row. Outcome: Cancelled when cancelled, Failed when not successful, Skipped when successful and skipped, else Succeeded; Forecasting owns that decision: a static factory ForecastRoundFinished.For(portfolio, itemCount, durationMs, finishedAt, success, cancelled, skipped) on the event record derives the outcome, replacing ForecastUpdater's private LogForecastRefresh.
```

```text
ForecastRoundFinishedRefreshLogHandler (Work Items, Application/Policies, IDomainEventHandler<ForecastRoundFinished>) translates the event into today's RefreshLog row: Type Forecast, EntityId, EntityName, ItemCount, SyncOutcome.None's Mode/RecordsScanned/RecordsFetched, DurationMs, ExecutedAt = FinishedAt, Success = Succeeded or Skipped, Cancelled = Cancelled, Skipped = Skipped; a total switch over ForecastRoundOutcome (CS8524 pragma per the repository idiom). It is the anti-corruption translation of Work Items' own record and holds no other rule. Registered Scoped in Program's handler block; it is the only handler of the event.
```

```text
Forecasting's direction rule (ForecastingsLegalCallers: the API and Refinement at HEAD) admits Lighthouse.Backend.Modules.WorkItems.Application.Policies, so the handler may name Forecasting.Contracts.ForecastRoundFinished. A namespace pattern, not a list entry (S3's law forbids module-to-module list entries).
```

```text
The S2 oracle is one file at the port: a ServicePortHarness registering the forecast round as Program builds it (real ForecastUpdater, DomainEventDispatcher, RefreshLogService, the handler through the start-up registration), doubles only for IForecastService outcomes (computed, confirmed unchanged, throws) and the operator stop; four rows (succeeded, failed, cancelled, skipped) read the row through IRefreshLogService.GetRefreshLogs against today's expected fields; one row with a throwing IRefreshLogService observes the dispatcher's error log and the round's completion; one IL row asserts ForecastUpdater names no IRefreshLogService. The order pin (support) reads ForecastRoundFinished and pins [ForecastRoundFinishedRefreshLogHandler].
```

##### Constraints

None.

##### Reuse

DomainEventDispatcher

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/DomainEvents\/DomainEventDispatcher\.cs\:6

Reuse

```text
existing transport (ADR-027 D7); its log-and-continue policy is the accepted degradation
```

ForecastUpdater

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:22

Extend

```text
owns the round and its outcome flags; LogForecastRefresh becomes the publish
```

ForecastRoundFinished

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRunOutcome\.cs\:1

CreateNew

```text
no event carries a forecast round's end; ForecastRunOutcome is the forecast computation's result (computed or confirmed unchanged), not the round's four-way outcome, so it is not extended
```

ForecastRoundFinishedRefreshLogHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/TeamDeletedRefreshLogCleanupHandler\.cs\:8

CreateNew

```text
follows the existing Work Items refresh-log policy handler shape; no handler of a forecast round exists
```

ServicePortHarness

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/TestHelpers\/ServicePortHarness\.cs\:21

Reuse

```text
the port seam for background jobs without a host
```

LogRefreshAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Contracts\/IRefreshLogService\.cs\:7

Reuse

```text
the write RX1 performs, unchanged, now called by Work Items' own handler
```

ForecastRoundFinished

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundFinished\.cs\:1

CreateNew

```text
new type, no existing owner (see the slice's CreateNew rows)
```

ForecastRoundOutcome

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundOutcome\.cs\:1

CreateNew

```text
new type, no existing owner (see the slice's CreateNew rows)
```

ForecastRoundFinishedRefreshLogHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ForecastRoundFinishedRefreshLogHandler\.cs\:1

CreateNew

```text
new type, no existing owner (see the slice's CreateNew rows)
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:79

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:44

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

ForecastUpdaterTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:32

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

WorkItemsPoliciesLayoutArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs\:20

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

##### Impact

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:383

Changed

```text
direct refresh-log write replaced by the event publish
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1433

Changed

```text
one handler registration added to the handler block
```

refresh\-log\-readers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:22

UnchangedCompatible

```text
reads the same rows with the same fields
```

refresh\-log\-readers

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/UpdateQueue\/UpdateNotificationHub\.cs\:14

UnchangedCompatible

```text
reads the same rows with the same fields
```

unit\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdaterTest\.cs\:1253

Changed

```text
CaptureRefreshLog rows move to the published event
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:145

Changed

```text
legal callers widened by Work Items' Application.Policies
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundFinished\.cs\:1

Changed

```text
new file of the RX1 path
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Contracts\/ForecastRoundOutcome\.cs\:1

Changed

```text
new file of the RX1 path
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ForecastRoundFinishedRefreshLogHandler\.cs\:1

Changed

```text
new file of the RX1 path
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs\:20

Changed

```text
edited for this slice's law
```

##### Agreement

ForecastRoundFinished \(Forecasting\.Contracts\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Forecasting\/Adapters\/Background\/ForecastUpdater\.cs\:226

Producer

Migrated

```text
new producer replacing the direct LogRefreshAsync call
```

ForecastRoundFinished \(Forecasting\.Contracts\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/Policies\/ForecastRoundFinishedRefreshLogHandler\.cs\:1

Consumer

Migrated

```text
sole consumer, same commit
```

RefreshLog row of RefreshType\.Forecast

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/RefreshLogService\.cs\:17

Consumer

UnchangedCompatible

```text
receives the same entry fields from the handler
```

RefreshLog row of RefreshType\.Forecast

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/SystemInfoController\.cs\:22

Consumer

UnchangedCompatible

```text
reads rows equal under the refresh-log entry equality
```

##### Not applicable

prefactoring

```text
Per touched file: ForecastUpdater.cs holds one type (not-applicable); Program.cs one type (not-applicable); ForecastUpdaterTest.cs one type (not-applicable); ForecastingModuleBoundariesArchUnitTest.cs and WorkItemsPoliciesLayoutArchUnitTest.cs hold the rule class plus planted fixture types in made-up namespaces that the falsifiers read from the compiled test assembly; the edit is one pattern, and splitting every rule's fixtures is a whole-folder layout decision for the test-writing skill's owner, not this slice (not-applicable, flagged to the maintainer). The three new files hold one type each.
```

##### Boundaries

```text
IForecastUpdater.TriggerImmediateUpdate over a ServicePortHarness scope factory, no host
```

###### Driven ports

```text
IDomainEventDispatcher (platform lane, unchanged)
```

```text
IRefreshLogService (Work Items Contracts, unchanged)
```

```text
Forecasting publishes ForecastRoundFinished from its Contracts; Work Items' Application.Policies handler depends on Forecasting.Contracts and writes through its own IRefreshLogService; ForecastUpdater names no Work Items type for the log
```

###### Failures

####### B1

```text
IRefreshLogService.LogRefreshAsync throws inside the handler
```

Indeterminate

```text
dispatcher error log naming the handler and event; no row for that round; the round completes
```

##### Observations

None.

##### Equality

None.

##### Constructors

ForecastRoundFinished

sealed record ForecastRoundFinished\(int PortfolioId\, string PortfolioName\, int ItemCount\, long DurationMs\, DateTime FinishedAt\, ForecastRoundOutcome Outcome\) \: IDomainEvent

ForecastRoundOutcome

enum ForecastRoundOutcome \{ Succeeded\, Failed\, Cancelled\, Skipped \}

##### Laws

###### L1

```text
For each round outcome, the refresh-log row written after the round equals today's row under the refresh-log entry equality.
```

Refinement

Application

ForecastRoundsKeepTheirRefreshLogEntryTest

###### Treatment

Finite

###### Cases

```text
succeeded: Success true, Cancelled false, Skipped false, ItemCount = feature count
```

```text
failed (forecast throws): Success false, Cancelled false, Skipped false, ItemCount 0
```

```text
cancelled (operator stop): Success false, Cancelled true, Skipped false
```

```text
skipped (confirmed unchanged): Success true, Cancelled false, Skipped true
```

```text
closed four-value domain, every member a row
```

###### Evidence

None.

###### L2

```text
A throwing refresh-log write never fails the round: the round completes, the dispatcher logs one error naming ForecastRoundFinishedRefreshLogHandler and ForecastRoundFinished, and no refresh-log row exists for it.
```

Invariant

Adapter

ForecastRoundsKeepTheirRefreshLogEntryTest

###### Treatment

Finite

###### Cases

```text
succeeded round with an IRefreshLogService whose LogRefreshAsync throws
```

```text
one failure point, one example (D-16)
```

###### Evidence

None.

###### L3

```text
ForecastUpdater has no IL dependency on IRefreshLogService.
```

Invariant

Adapter

ForecastRoundsKeepTheirRefreshLogEntryTest

###### Treatment

Finite

###### Cases

```text
IlDependencies of ForecastUpdater and its nested types exclude Modules.WorkItems.Contracts.IRefreshLogService
```

```text
single type, read once
```

###### Evidence

None.

###### L4

```text
ForecastRoundFinished's handlers, in registration order, are exactly [ForecastRoundFinishedRefreshLogHandler].
```

Invariant

Infrastructure

DomainEventHandlerOrderPinTest

###### Treatment

Finite

###### Cases

```text
start-up registrations read without a host
```

```text
one event, one list
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
A forecast round's refresh-log flags come from one of four outcomes, so Success with Cancelled, or Skipped without Success, cannot be written for a forecast round
```

```text
a row the System info page shows as both succeeded and stopped
```

Forecasting\.Contracts\.ForecastRoundOutcome

```text
the round's success, cancelled and skipped flags in ForecastUpdater
```

ForecastRoundFinished

ForecastRoundFinishedRefreshLogHandler

```text
the refresh-log row read through IRefreshLogService.GetRefreshLogs
```

```text
an undefined enum value cast from an int: the handler's total switch throws and the dispatcher logs it (C# enums are open; construction centralised, not proved)
```

##### States

None.

##### Models

None.

##### Closure

```text
RX1
```

Safety

Pass

DesignedNotBuilt

```text
four-way closed outcome excludes unreachable flag rows; IL row excludes the direct write
```

###### Evidence

None.

```text
RX1
```

Inhabitation

Pass

DesignedNotBuilt

```text
each outcome is constructed by ForecastUpdater from flags the round already computes
```

###### Evidence

None.

```text
RX1
```

PublicReachability

Pass

DesignedNotBuilt

```text
round -> ForecastRoundFinished -> DomainEventDispatcher -> handler -> IRefreshLogService -> GetRefreshLogs at the port
```

###### Evidence

None.

```text
RX1
```

Liveness

Pass

DesignedNotBuilt

```text
no liveness claim: the handler runs to completion inside the awaited PublishAsync of the round's finally
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs

Extend

```text
module-to-module entries removed; patterns stop reading other modules as forbidden where R-C1 now owns the edge
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Extend

```text
R-C1's list: gains any module-to-module non-Contracts edge found only in a per-module list
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
prose: Contracts as fourth layer (D-2 criterion, R-C1-R-C5), event table with owners, Every-edge rows to Contracts, Forecasting -> Work Items by events, ratchet candidates = R-C1 residue with owners, ConnectionFetchShapeChanged owner, context contract term
```

ARCHITECTURE\.md

Extend

```text
prose: section 4 Context contracts bullet (layer, rule, enumerator, residue) and the CI test-selection payoff note (D-13); section 5 names ForecastRoundFinished
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EachCrossContextEdgeHasOneOwningRuleArchUnitTest\.cs

#### Verification

```text
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

None.

None.

#### Public oracle

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Architecture over the compiled test and production assemblies
```

```text
RED before craft: the law names each module-to-module entry of the per-module lists (e.g. Licensing's ForecastFilterRuleService -> ILicenseService, Tracker integration's ConnectionHealthCadence -> IAppSettingService, Work Items' WorkItemService -> PercentileCalculator). GREEN after: no such entry remains, R-C1 and every other Architecture rule pass.
```

```text
Re-adding any module-to-module entry to a per-module list, or a planted fixture list with one, fails the law naming the list, the entry and R-C1 as the owner.
```

##### Promises

P3

#### Prefactoring

None.

#### Records

##### Decisions

```text
The one-owner law's population is read, not named: every static string collection member of the test assembly's Architecture types whose entries have the edge shape "Source -> Target", read by value from the compiled test assembly (concatenated entries included). It excludes R-C1's own list and an inline, both-ways-compared list of members that record compiled edges rather than allow them (for example the open host's edges-that-became-legal list). Law: no included entry has a source in Lighthouse.Backend.Modules.A and a target in Lighthouse.Backend.Modules.B with A different from B (fixture roots such as *Fixtures excluded as not packaged contexts, per R-C3). Preservation: every module-to-module edge listed anywhere before S3 is in R-C1's list after S3, or targets a Contracts type, or has left the IL.
```

```text
A module-to-module edge into another context's Contracts leaves every list: R-C1 makes it legal and the target's direction pattern governs it at context level. This includes Licensing R-L3's module-sourced licence reads (Forecasting ForecastFilterRuleService and WriteBackTriggerService, Work Items DependencyDecision, Reports ReportCommands and ReportQueries) and Work Items' and Access's Contracts edges; R-L3 keeps its non-module readers (the Services updaters, API). The per-edge ratchet on module licence reads is given up for a context-level direction rule, as P3 requires.
```

```text
OrganisationSetupIsAnOpenHostArchUnitTest (support, acceptance designer): TrackerIntegrationsList_HoldsExactlyTheCadenceEdgeIntoTheSettings becomes "Tracker integration's list names no edge into set-up", and AMadeUpTrackerIntegrationTypeAskingTheSettings_IsNamedByTrackerIntegrationsRule becomes "a made-up Tracker integration type asking set-up's Contracts passes Tracker integration's rule" (set-up is the open host). Nothing else in that file changes; its Contracts narrowing is feature C's.
```

##### Constraints

None.

##### Reuse

GivenTheEntriesOfTheList

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs\:237

Reuse

```text
reading a list's entries by reflection already exists; the oracle uses the same reading over every member instead of named ones
```

CrossContextEdgesOutsideContracts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:51

Reuse

```text
R-C1 is the one owner; holds all eight non-Contracts edges at HEAD
```

TheTenPackagedContexts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:36

Reuse

```text
the R-C3 enumerator decides which namespace roots are contexts
```

ForecastingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:44

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

LicensingModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

OrganisationSetupModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs\:16

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

RefinementModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:41

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

ReportsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:42

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

TrackerIntegrationModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:29

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

WorkItemsModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

AccessModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

PromisesModuleBoundariesArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:36

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

FlowMetricsApplicationOutboundArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:29

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

FlowMetricsInboundApplicationArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:21

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

FlowMetricsInboundDomainAndAdaptersArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:23

Extend

```text
existing owner edited in place: list entries, a pattern or a registration only
```

Bounded

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1

Extend

```text
the existing authority document is amended (D-11), no second document
```

Lighthouse

ARCHITECTURE\.md\:1

Extend

```text
the existing authority document is amended (D-11), no second document
```

##### Impact

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:136

Changed

```text
module-to-module entries removed (measured at HEAD by literal search: Forecasting, Licensing, Organisation set-up, Refinement, Reports, Tracker integration, Work Items lists; concatenated entries found by the oracle)
```

architecture\-tests

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs\:51

UnchangedCompatible

```text
expected unchanged: every non-Contracts module edge of the per-module lists is already in R-C1
```

docs

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1

Changed

```text
D-11 prose
```

docs

ARCHITECTURE\.md\:1

Changed

```text
D-11, D-13 prose
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:44

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs\:16

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs\:41

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\:42

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:29

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:36

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:36

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:36

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:29

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:21

Changed

```text
edited for this slice's law
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:23

Changed

```text
edited for this slice's law
```

##### Agreement

None.

##### Not applicable

agreement

```text
Test-side rule lists and prose only; no production contract changes.
```

prefactoring

```text
Per touched file: OrganisationSetupModuleBoundariesArchUnitTest.cs, ContextContractsArchUnitTest.cs (rule plus fixtures), and the per-module rule files (Forecasting, Licensing, Refinement, Reports, Tracker integration, Work Items, Access, Promises, the three Flow metrics rules) each hold one rule class plus planted fixture types in made-up namespaces that the falsifiers read from the compiled test assembly; the slice edits list entries and patterns only. Splitting every rule's fixtures is a whole-folder layout decision for the test-writing skill's owner (not-applicable per file, flagged to the maintainer). ADR-270 and ARCHITECTURE.md are prose (not-applicable).
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

###### L5

```text
No edge-shaped entry of any rule list other than R-C1's names a source in one packaged context and a target in another; every module-to-module edge listed before is in R-C1, targets a Contracts type, or has left the IL.
```

Invariant

Adapter

EachCrossContextEdgeHasOneOwningRuleArchUnitTest

###### Treatment

Finite

###### Cases

```text
every per-module list read by value
```

```text
planted falsifier: a fixture list naming Modules.Refinement.X -> Modules.Access.Application.Y is named WHAT/WHY/HOW
```

```text
the exclusion list compared both ways with the members it names
```

```text
closed finite set of compiled members
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
one owner per edge
```

Safety

Pass

DesignedNotBuilt

```text
law executed over every edge-shaped list member by value
```

###### Evidence

None.

```text
one owner per edge
```

Inhabitation

Pass

DesignedNotBuilt

```text
R-C1's list already holds the eight non-Contracts edges; the per-module lists can be emptied of module edges without losing one
```

###### Evidence

None.

```text
one owner per edge
```

PublicReachability

Pass

DesignedNotBuilt

```text
compiled test assembly -> oracle -> dotnet test exit and message
```

###### Evidence

None.

```text
one owner per edge
```

Liveness

Pass

DesignedNotBuilt

```text
no liveness claim: a static relation over lists
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T3

```text
One owner per edge: no per-module list names a module-to-module edge; every such edge is owned by R-C1 or is legal through Contracts
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EachCrossContextEdgeHasOneOwningRuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
```

##### Contract tests

None.

## ADRs

None.
