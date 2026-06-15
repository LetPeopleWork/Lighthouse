# Slice-01 Triage — backend-test-speed (#5258)

Date: 2026-06-15 — classification only, no code changed.

Inventory of every backend test fixture that runs serially under `[assembly: Parallelizable(ParallelScope.Fixtures)]`: it either carries `[NonParallelizable]` directly or derives from `TestHelpers/IntegrationTestBase` (which is itself `[NonParallelizable]`, so derived fixtures serialize by inheritance without their own tag). `Architecture/BackendTestParallelizationGuardTest.cs` carries the literal string in its scan/allowlist code but is the guard itself — excluded from the offender inventory.

## Inventory summary

- Total serial fixtures: 71 (35 carry their own `[NonParallelizable]`; 36 serialize purely by inheriting `IntegrationTestBase`).
- Plus 2 self-built WAF bases that are not fixtures themselves but force their derivers serial: `BlackoutForecastShiftTestBase`, `RecurringBlackoutRulesTestBase`. And `UpdateServiceTestBase` (shared-mock base, no own tag).

Counts per cluster:

| cluster | count |
|---|---|
| WAF-integration-base (derives `IntegrationTestBase`, shared static WAF + global DB reset) | 36 |
| WAF-integration-selfbuilt (self-builds its own `TestWebApplicationFactory` + `EnsureDeleted`/`EnsureCreated`) | 25 |
| service-mock (shared Moq / static dispatcher probe / static metrics cache) | 7 |
| inherently-serial (deliberate process-global / concurrency) | 3 |
| **total** | **71** |

Counts per verdict:

| verdict | count |
|---|---|
| fix | 67 |
| keep | 4 |

(The `keep` count is 4: the 3 genuine process-global security fixtures plus `LighthouseReleaseServiceIntegrationTest`, whose static is a GitHub rate-limit workaround. See the allowlist discrepancy note below — the current guard allowlist of 6 includes three fixtures this triage would `fix`.)

## Triage table

`carries_own_tag` = the file has `[NonParallelizable]` directly (vs inheriting it from `IntegrationTestBase`).

### WAF-integration-base (shared static WAF + global DB reset, bucket 1)

These all use the default `IntegrationTestBase()` ctor → the single `static SharedFactoryLazy` factory and the base `[SetUp]`/`[TearDown]` that `EnsureDeleted()`/`EnsureCreated()` the whole DB. They serialize by inheritance.

| path | fixture | cluster | carries_own_tag | root_cause | verdict | fix_hint |
|---|---|---|---|---|---|---|
| API/Integration/ApiKeyControllerHttpSmokeTests.cs | ApiKeyControllerHttpSmokeTests | WAF-integration-base | no | bucket1: shared static `SharedFactory` + base global DB `EnsureDeleted`/`EnsureCreated` per test | fix | per-fixture WAF with SQLite pooling defeated |
| API/Integration/ApiVersioningRoutingTests.cs | ApiVersioningRoutingTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/BlackoutForecastShiftDeliveryIntegrationTest.cs | BlackoutForecastShiftDeliveryIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/BlackoutPeriodsControllerAuthorizationTests.cs | BlackoutPeriodsControllerAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/DeliveriesControllerIntegrationTest.cs | DeliveriesControllerIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/DeliveryMetricSnapshotCascadeDeleteIntegrationTest.cs | DeliveryMetricSnapshotCascadeDeleteIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/ForecastControllerAuthorizationTests.cs | ForecastControllerAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/ProjectsControllerAuthorizationTests.cs | ProjectsControllerAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/RbacExceptionEndpointsAuthorizationTests.cs | RbacExceptionEndpointsAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/RecurringBlackoutRulesAuthorizationTests.cs | RecurringBlackoutRulesAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/TeamDeletionIntegrationTest.cs | TeamDeletionIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/TeamsControllerAuthorizationTests.cs | TeamsControllerAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Integration/TerminologyControllerAuthorizationTests.cs | TerminologyControllerAuthorizationTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| API/Security/S3_UpdateNotificationHubAuthorizeTests.cs | S3_UpdateNotificationHubAuthorizeTests | WAF-integration-base | no | bucket1: derives base but passes a self-built factory into base ctor (owns factory); still base global DB reset → SQLite race | fix | per-fixture WAF, pooling defeated |
| API/Security/S4_DeliveriesDeleteGuardInversionTests.cs | S4_DeliveriesDeleteGuardInversionTests | WAF-integration-base | no | bucket1: derives base, self-built factory via base ctor; base global DB reset → SQLite race | fix | per-fixture WAF, pooling defeated |
| Models/OAuth/OAuthCredentialTest.cs | OAuthCredentialTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Repository/DeliveryRepositoryTest.cs | DeliveryRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Licensing/LicensingIntegrationTest.cs | LicensingIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/DeletionTests.cs | DeletionTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/DeliveryMetricSnapshotRepositoryTest.cs | DeliveryMetricSnapshotRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/LicenseInformationRepositoryTest.cs | LicenseInformationRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/PortfolioRepositoryTest.cs | PortfolioRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/RepositoryBaseTest.cs | RepositoryBaseTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/TeamRepositoryTest.cs | TeamRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/WorkItemRepositoryTest.cs | WorkItemRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/WorkItemStateTransitionRepositoryTest.cs | WorkItemStateTransitionRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Repositories/WorkTrackingSystemConnectionRepositoryTest.cs | WorkTrackingSystemConnectionRepositoryTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Seeding/AppSettingSeederTests.cs | AppSettingSeederTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Seeding/OptionalFeatureSeederTests.cs | OptionalFeatureSeederTests | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/Seeding/TerminologySeederTests.cs | TerminologySeederTests | WAF-integration-base | yes | bucket1: derives base (already serial); own `[NonParallelizable]` is REDUNDANT | fix | per-fixture WAF; drop redundant own tag |
| Services/Implementation/WorkItems/DemoDataStateEnteredSeamIntegrationTest.cs | DemoDataStateEnteredSeamIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/WorkItems/WorkItemServiceCsvStateEnteredSeamIntegrationTest.cs | WorkItemServiceCsvStateEnteredSeamIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/WorkItems/WorkItemServiceFeatureTransitionIntegrationTest.cs | WorkItemServiceFeatureTransitionIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/WorkItems/WorkItemServiceTransitionFallbackIntegrationTest.cs | WorkItemServiceTransitionFallbackIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/WorkItems/WorkItemServiceTransitionSyncIntegrationTest.cs | WorkItemServiceTransitionSyncIntegrationTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |
| Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnectorTest.cs | CsvWorkTrackingConnectorTest | WAF-integration-base | no | bucket1: shared static WAF + global DB reset | fix | per-fixture WAF, pooling defeated |

### WAF-integration-selfbuilt (own factory + `EnsureDeleted`/`EnsureCreated`, bucket 2)

Each self-builds `new TestWebApplicationFactory<Program>()` (in `[SetUp]`, per-test, unless noted) and calls `EnsureDeleted`/`EnsureCreated`. Per ci-learnings 2026-05-18 the SQLite connection pool is **process-wide**, so even unique file names race under parallel `EnsureDeleted`/`EnsureCreated`. The `static int testDateOffset` seen in many of these is `Interlocked.Increment` — thread-safe, NOT the blocker; the `static readonly` arrays are immutable, also not the blocker. The pool race is the real reason.

| path | fixture | cluster | carries_own_tag | root_cause | verdict | fix_hint |
|---|---|---|---|---|---|---|
| API/Integration/AgeInStatePercentilesReadApiIntegrationTest.cs | AgeInStatePercentilesReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-builds WAF in [SetUp] + EnsureDeleted/Created → process-wide SQLite pool race (testDateOffset is Interlocked, not the blocker) | fix | disable SQLite pooling per fixture (Pooling=False / held-open in-memory) |
| API/Integration/AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest.cs | AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/AgeInStatePercentilesPortfolioReadApiIntegrationTest.cs | AgeInStatePercentilesPortfolioReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/CumulativeStateTimeReadApiIntegrationTest.cs | CumulativeStateTimeReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/CumulativeStateTimePortfolioReadApiIntegrationTest.cs | CumulativeStateTimePortfolioReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/CycleTimeDefinitionSettingsIntegrationTest.cs | CycleTimeDefinitionSettingsIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/CycleTimeDefinitionValidityIntegrationTest.cs | CycleTimeDefinitionValidityIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/DeliveryMetricsHistoryReadApiIntegrationTest.cs | DeliveryMetricsHistoryReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race (carries own `[NonParallelizable]` at line 17) | fix | disable SQLite pooling per fixture |
| API/Integration/FlowEfficiencyReadApiIntegrationTest.cs | FlowEfficiencyReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/FlowEfficiencyPortfolioReadApiIntegrationTest.cs | FlowEfficiencyPortfolioReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs | ForecastFilterTeamSettingsIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/ForecastFilterThroughputChartIntegrationTest.cs | ForecastFilterThroughputChartIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/NamedCycleTimeReadApiIntegrationTest.cs | NamedCycleTimeReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race (carries own `[NonParallelizable]` at line 15) | fix | disable SQLite pooling per fixture |
| API/Integration/NamedCycleTimeCumulativeScopeIntegrationTest.cs | NamedCycleTimeCumulativeScopeIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/NamedCycleTimePortfolioIntegrationTest.cs | NamedCycleTimePortfolioIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/PortfolioConcurrencyTokenIntegrationTest.cs | PortfolioConcurrencyTokenIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/PortfolioDeleteSerialisationTests.cs | PortfolioDeleteSerialisationTests | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/PortfolioStalenessThresholdSettingsIntegrationTest.cs | PortfolioStalenessThresholdSettingsIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/PortfolioTimeInStateReadApiIntegrationTest.cs | PortfolioTimeInStateReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race (testDateOffset Interlocked, not blocker) | fix | disable SQLite pooling per fixture |
| API/Integration/RbacGroupMappingConcurrencyTokenIntegrationTest.cs | RbacGroupMappingConcurrencyTokenIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/RecurringBlackoutRulesChartOverlayIntegrationTest.cs | RecurringBlackoutRulesChartOverlayIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/RecurringBlackoutRulesWriteBackIntegrationTest.cs | RecurringBlackoutRulesWriteBackIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/RecurringBlackoutRulesByDateForecastIntegrationTest.cs | RecurringBlackoutRulesByDateForecastIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` (self-builds WAF + EnsureDeleted/Created) → SQLite pool race | fix | fix the shared base: pooling defeated per fixture |
| API/Integration/RecurringBlackoutRulesDeliveryIntegrationTest.cs | RecurringBlackoutRulesDeliveryIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/RecurringBlackoutRulesDownstreamParityIntegrationTest.cs | RecurringBlackoutRulesDownstreamParityIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/RecurringBlackoutRulesIntervalRuleIntegrationTest.cs | RecurringBlackoutRulesIntervalRuleIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/RecurringBlackoutRulesLifecycleIntegrationTest.cs | RecurringBlackoutRulesLifecycleIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/RecurringBlackoutRulesWeekendsForeverIntegrationTest.cs | RecurringBlackoutRulesWeekendsForeverIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `RecurringBlackoutRulesTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/BlackoutForecastShiftItemPredictionIntegrationTest.cs | BlackoutForecastShiftItemPredictionIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `BlackoutForecastShiftTestBase` (self-builds WAF + EnsureDeleted/Created in [SetUp]) → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/BlackoutForecastShiftTeamForecastIntegrationTest.cs | BlackoutForecastShiftTeamForecastIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: via `BlackoutForecastShiftTestBase` → SQLite pool race | fix | fix the shared base: pooling defeated |
| API/Integration/TeamConcurrencyTokenIntegrationTest.cs | TeamConcurrencyTokenIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/TeamDeleteSerialisationTests.cs | TeamDeleteSerialisationTests | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/TeamStalenessThresholdSettingsIntegrationTest.cs | TeamStalenessThresholdSettingsIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/TimeInStateReadApiIntegrationTest.cs | TimeInStateReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race (testDateOffset Interlocked) | fix | disable SQLite pooling per fixture |
| API/Integration/WorkItemAgePercentilesReadApiIntegrationTest.cs | WorkItemAgePercentilesReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/WorkItemAgePercentilesPortfolioReadApiIntegrationTest.cs | WorkItemAgePercentilesPortfolioReadApiIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| API/Integration/WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest.cs | WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race | fix | disable SQLite pooling per fixture |
| Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandlerTest.cs | DeliveryMetricSnapshotRecordingHandlerTest | WAF-integration-selfbuilt | yes | bucket2: self-built WAF [SetUp] + EnsureDeleted/Created → SQLite pool race (static fields are immutable arrays/JsonSerializerOptions) | fix | disable SQLite pooling per fixture |

### service-mock (shared Moq / static probe / static metrics cache)

| path | fixture | cluster | carries_own_tag | root_cause | verdict | fix_hint |
|---|---|---|---|---|---|---|
| Services/Implementation/TeamMetricsServiceTests.cs | TeamMetricsServiceTests | service-mock | yes | bucket3: asserts the process-wide `static BaseMetricsService.MetricsCache` (keyed by entityId via `GetCacheKey`); `*CachesValue`/`*InvalidateCache` tests + Team Id=1 collide under parallel same-id fixtures (mocks themselves are fresh per [SetUp], not the blocker) | fix | make MetricsCache injectable per fixture, or keep serial |
| Services/Implementation/PortfolioMetricsServiceTests.cs | PortfolioMetricsServiceTests | service-mock | yes | bucket3: asserts `static BaseMetricsService.MetricsCache` keyed by entityId; `InvalidatePortfolioMetrics` in [TearDown] + same Portfolio id collide under parallel | fix | make MetricsCache injectable per fixture, or keep serial |
| Services/Implementation/BackgroundServices/Update/PortfolioUpdaterTest.cs | PortfolioUpdaterTest | service-mock | yes | bucket5: shared Moqs from `UpdateServiceTestBase` (`private readonly Mock<...>` built in base ctor, mutated across [Test]); static array `ForecastThenEventDispatchOrder` is immutable, not the blocker | fix | rebuild mocks in [SetUp] (per-test) |
| Services/Implementation/BackgroundServices/Update/TeamUpdaterTest.cs | TeamUpdaterTest | service-mock | yes | bucket5: shared Moqs from `UpdateServiceTestBase` ctor, mutated across [Test] methods | fix | rebuild mocks in [SetUp] (per-test) |
| Services/Implementation/DomainEvents/DomainEventDispatcherGoldTest.cs | DomainEventDispatcherGoldTest | service-mock | yes | bucket5: derives `IntegrationTestBase` (already serial) + own `static ProbeState` reset in [SetUp]; parallel fixtures would contaminate the shared static probe | fix | per-fixture probe (non-static) + per-fixture WAF; own tag distinct from base |
| Services/Implementation/DomainEvents/TeamDataRefreshedGoldTest.cs | TeamDataRefreshedGoldTest | service-mock | yes | bucket5: derives `IntegrationTestBase` + own `static Recorder` reset in [SetUp]; shared static recorder contaminates under parallel | fix | per-fixture recorder (non-static) + per-fixture WAF |
| Services/Implementation/DomainEvents/WorkItemDomainEventsGoldTest.cs | WorkItemDomainEventsGoldTest | service-mock | yes | bucket5: derives `IntegrationTestBase` + own `static ProbeState` reset in [SetUp]; shared static probe contaminates under parallel | fix | per-fixture probe (non-static) + per-fixture WAF |

### inherently-serial (deliberate process-global / concurrency, bucket 6)

| path | fixture | cluster | carries_own_tag | root_cause | verdict | fix_hint |
|---|---|---|---|---|---|---|
| API/Security/S1_AllowedOriginsEnvVarBindingTests.cs | S1_AllowedOriginsEnvVarBindingTests | inherently-serial | yes | bucket6: `Environment.SetEnvironmentVariable` for `Authentication__AllowedOrigins*` in [SetUp]/[TearDown] — process-global env binding | keep | stays on allowlist |
| API/Security/S1_CorsFailClosedTests.cs | S1_CorsFailClosedTests | inherently-serial | yes | bucket6: `Environment.SetEnvironmentVariable` for CORS/auth env keys — process-global env binding | keep | stays on allowlist |
| API/Security/S6_RateLimitingTests.cs | S6_RateLimitingTests | inherently-serial | yes | bucket6: per-IP rate-limiter window/partition is process-wide; parallel requests would cross-trip the limiter | keep | stays on allowlist |

### inherently-serial vs SQLite-race — allowlisted fixtures that this triage would actually `fix`

These three are on the current guard allowlist but have NO genuine process-global state; their serialization reason is the SQLite-pool race (bucket 2) or a fixable external workaround. Listed here for the discrepancy note; verdict `fix` (or `keep` with a narrower justification — see below).

| path | fixture | carries_own_tag | root_cause | verdict | fix_hint |
|---|---|---|---|---|---|
| API/Security/S5_ApiKeyScopesTests.cs | S5_ApiKeyScopesTests | yes | bucket2: self-builds WAF + EnsureDeleted/Created → SQLite pool race; no mutable static in `ApiKeyService` (all statics are pure helpers) | fix | per-fixture WAF, pooling defeated — remove from allowlist |
| API/Security/F_BE_1_GroupSnapshotInheritanceTests.cs | F_BE_1_GroupSnapshotInheritanceTests | yes | bucket2: self-builds WAF + EnsureCreated; seeds a shared system-admin profile; no mutable static in the RBAC/group-snapshot services | fix | per-fixture WAF, pooling defeated; self-contained seed — remove from allowlist |
| LighthouseAppContextConcurrencyTest.cs | LighthouseAppContextConcurrencyTest | yes | bucket2: name says "concurrency" but the test is EF optimistic-concurrency-TOKEN semantics across separate scopes, run serially — NOT in-process Task.Run concurrency; the actual serial reason is self-built WAF + EnsureDeleted/Created → SQLite pool race | fix | per-fixture WAF, pooling defeated — remove from allowlist |
| Services/Implementation/LighthouseReleaseServiceIntegrationTest.cs | LighthouseReleaseServiceIntegrationTest | yes | bucket6-ish: `static readonly IGitHubService GitHubService = new GitHubService()` shared explicitly "to work around rate limits"; HTTP is mocked, so the static is the only shared state | keep | genuinely serial external-rate-limit workaround — ADD to allowlist (currently absent) |

## Provisional allowlist (keep verdicts)

The genuinely-serial set this triage endorses (4 fixtures):

- **S1_AllowedOriginsEnvVarBindingTests** — mutates process-global `Authentication__AllowedOrigins*` env vars in `[SetUp]`/`[TearDown]`; parallel fixtures would read each other's env state.
- **S1_CorsFailClosedTests** — mutates process-global CORS/auth env vars; same reason.
- **S6_RateLimitingTests** — exercises the process-wide per-IP rate limiter; parallel requests would cross-trip windows.
- **LighthouseReleaseServiceIntegrationTest** — shares a `static IGitHubService` deliberately to avoid GitHub rate limits across methods; the only shared state and an external-resource constraint.

Discrepancy vs the current guard allowlist (`Architecture/BackendTestParallelizationGuardTest.cs`, 6 names):

Current allowlist: `S6_RateLimitingTests`, `S1_AllowedOriginsEnvVarBindingTests`, `S1_CorsFailClosedTests`, `S5_ApiKeyScopesTests`, `F_BE_1_GroupSnapshotInheritanceTests`, `LighthouseAppContextConcurrencyTest`.

- **3 endorsed (agree)**: `S6_RateLimitingTests`, `S1_AllowedOriginsEnvVarBindingTests`, `S1_CorsFailClosedTests`.
- **3 allowlisted but this triage would `fix` (remove from allowlist after isolation)**: `S5_ApiKeyScopesTests`, `F_BE_1_GroupSnapshotInheritanceTests`, `LighthouseAppContextConcurrencyTest` — all are SQLite-pool-race (bucket 2), not genuine process-global. `LighthouseAppContextConcurrencyTest`'s name is misleading: it tests EF concurrency-token semantics serially via separate scopes, not in-process parallelism.
- **1 `keep` NOT currently on the allowlist (add)**: `LighthouseReleaseServiceIntegrationTest` — shared `static IGitHubService` rate-limit workaround.

Net: the final allowlist should converge to 4 (`S6`, both `S1`, `LighthouseReleaseServiceIntegrationTest`) once Slice-04 finalizes — a 3-out, 1-in swing from the current 6. Confirm `LighthouseAppContextConcurrencyTest` truly has no in-process parallelism before un-allowlisting it (the verdict here rests on it using separate scopes, not `Task.Run`).

## Per-cluster fix strategy

### WAF-integration-base (bucket 1) — per-fixture WAF ownership, pooling defeated

Replace the single shared static `SharedFactoryLazy` + whole-DB `EnsureDeleted`/`EnsureCreated` per test with per-fixture WAF/DB ownership: each fixture gets its own database whose SQLite **connection pooling is defeated** (either `Pooling=False` in the connection string or in-memory SQLite with a single connection held open for the fixture's lifetime). Per ci-learnings 2026-05-18, unique file names alone do NOT defeat the process-wide pool race — pooling must be turned off so concurrent `EnsureDeleted`/`EnsureCreated` cannot collide on "table AppSettings already exists". Once each fixture owns an isolated, pool-defeated DB, the base `[NonParallelizable]` is removable and all 36 derivers parallelize for free. The 36-fixture base is the highest-leverage single fix.

### WAF-integration-selfbuilt (bucket 2) — defeat the SQLite pool, keep per-fixture WAF

Same isolation primitive as bucket 1, applied to the self-built factories (and the two intermediate bases `BlackoutForecastShiftTestBase` / `RecurringBlackoutRulesTestBase`): set `Pooling=False` (or hold-open in-memory SQLite) per fixture so parallel `EnsureDeleted`/`EnsureCreated` stop racing. The `Interlocked.Increment` `testDateOffset` and the `static readonly` arrays are already parallel-safe and need no change. Best done by extracting the same pool-defeated DB-bootstrap helper used for bucket 1 so the two clusters converge on one mechanism.

### service-mock — injectable cache, fresh mocks, non-static probes

Three sub-fixes: (1) **metrics-cache** (`TeamMetricsServiceTests`, `PortfolioMetricsServiceTests`) — the process-wide `static BaseMetricsService.MetricsCache` keyed by entityId is the blocker; either make the cache injectable so each fixture gets its own instance, or accept these two as serial. (2) **shared-Moq** (`PortfolioUpdaterTest`, `TeamUpdaterTest`) — move the `UpdateServiceTestBase` mock construction out of the ctor into a `[SetUp]` so each test gets fresh mocks, then the `[NonParallelizable]` drops. (3) **Gold-test probes** — make the `static ProbeState`/`Recorder` per-fixture (instance, registered into that fixture's WAF) so parallel fixtures don't share the recorder; this rides on the same per-fixture WAF isolation as bucket 1.

## Spike: WAF setup cost + re-baseline

Measured 2026-06-15 on a 12-core local machine; CI numbers from the last 8 `Build And Deploy Lighthouse` main runs.

### CI re-baseline (AC-01.2)

| Metric | Value |
|---|---|
| Backend job total (build + test + license + upload) | ~765s mean / 12.75 min (715–820s over 8 runs) |
| `Test Backend` step alone | ~580s mean / 9.7 min (523–620s) |
| `Build Backend` step | ~80s |

Caveat on the timing CSV (`test-timings-backend`): its `category=Integration` rows are the **real-external-API** Jira/ADO write-back tests (30 tests, ~528s summed — e.g. `JiraWriteBackTest`, `BackTest`), which are **out of scope** (#5020 CS-H) and only run when connector/shared paths change. The in-process `WebApplicationFactory` fixtures this feature parallelizes are classified `Unit` in that CSV (3167 rows, ~311s summed). So a chunk of the `Test Backend` step variance is real-API noise, not our lever.

### Local serial baseline (AC-01.2)

`dotnet test --filter "Category!=Integration"` (the in-process WAF + unit + arch suite, current `[NonParallelizable]`-serial state), Release, `--no-build`:

| Run | Wall-clock | Result |
|---|---|---|
| 1 | 341s (5m39s) | 2969 passed, **1 failed**, 3 skipped |
| 2 | 332s (5m29s) | 2970 passed, 0 failed, 3 skipped |

Local serial baseline ≈ **336s (~5.5 min)**. The run-1 single failure was **non-reproducible** (green on the identical re-run) — a scheduling-dependent flake consistent with the static-cache contamination root cause (the `*MetricsServiceTests` / static `BaseMetricsService.MetricsCache` class identified above). It reinforces that those fixtures need a real per-fixture-isolation fix, not just an opt-out tag. Even "serial" here is only fixture-serial: the ~2900 un-tagged unit tests already run parallel, so a latent same-id cache collision can surface.

### WAF construction cost (AC-01.3) — throwaway microbenchmark, since deleted (no commit)

Built `new TestWebApplicationFactory<Program>()` → `CreateClient()` → `EnsureDeleted()`/`EnsureCreated()` 12× in one process (first 2 discarded as warmup):

| Phase | Cold (1st) | Warm mean | Warm range |
|---|---|---|---|
| WAF object construction | 1ms | 0ms | 0ms (host is lazy — built on first `CreateClient`) |
| `CreateClient` (real host build) | 1666ms | **110ms** | 89–139ms |
| DB `EnsureDeleted`+`EnsureCreated` | 184ms | 77ms | 67–114ms |
| **Full per-fixture warm setup** | — | **187ms** | — |

### Break-even + strategy recommendation (AC-01.4)

The first host build in the process costs ~1.7s (one-time JIT/host warmup, paid once regardless of strategy); every subsequent per-fixture WAF build is **~110ms**, plus ~77ms DB bootstrap ≈ **187ms per fixture warm**.

At the real scale (61 WAF fixtures): per-fixture WAF ownership adds ~61 × 187ms ≈ **11.4s of setup work in aggregate**, but that work parallelizes across cores — at 12 cores it is ~1s of wall-clock. Against a ~336s serial baseline that the parallelization collapses toward the un-tagged-suite floor, **the WAF-per-fixture setup cost never approaches the break-even** where shared-WAF would win. There is effectively no fixture count at this scale where WAF-per-fixture loses on cost.

**Therefore the cost premise behind ADR-074 (DDD-7: ship Strategy A plain, add a mitigation only if measured cost erodes the gain) holds: no mitigation is needed for cost.** The binding constraint is **correctness**, not cost — the process-wide SQLite connection-pool race (ci-learnings 2026-05-18). 

**Recommended strategy (confirms DESIGN Strategy A, sharpened):**
- **Per-fixture `WebApplicationFactory` ownership** (drop the shared static `SharedFactoryLazy` and the base `[NonParallelizable]`), AND
- **Defeat the SQLite connection pool per fixture** — set `Pooling=False` in the SQLite `DataSource` connection string (cheapest; keeps the existing file-per-fixture model) or switch to held-open in-memory SQLite. Unique file names alone do NOT defeat the process-wide pool, so this is the load-bearing change, not WAF ownership per se.
- Apply the same pool-defeat primitive to the 25 self-built fixtures + the two intermediate bases (`BlackoutForecastShiftTestBase`, `RecurringBlackoutRulesTestBase`) — extract one shared pool-defeated DB-bootstrap helper so both clusters converge on a single mechanism.
- The service-mock cluster is independent (Slice-03): injectable/per-fixture `MetricsCache`, fresh mocks in `[SetUp]`, non-static gold-probes.

### Open item for Slice-02 (cheapest-first validation)

Before the full base refactor, validate the pool-defeat primitive in isolation: pick ~3 self-built fixtures, set `Pooling=False`, remove their `[NonParallelizable]`, and run the integration subset 3× under `ParallelScope.Fixtures`. If green 3×, the primitive is proven and the base refactor proceeds; if a hidden process-global survives (à la the 2026-05-17 `VssConnection` cache), isolate that singleton per-host — do not re-blanket the base.
