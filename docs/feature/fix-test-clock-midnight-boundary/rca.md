# RCA: backend tests fail nightly between 22:00 and 24:00 UTC

CI run 39873168742 ran at about 2026-10-22T23:43Z, which was 2026-10-23 01:43 in Zurich (CEST, UTC+2).
Result: 23 failed, 11,234 passed. Every failure is an off-by-one calendar day.

## 0. Problem statement and scope

- **Window.** The test host runs with `TZ=Europe/Zurich` (`Lighthouse.Backend.Tests/lighthouse.runsettings:23`).
  - Under summer time (UTC+2) the Zurich day runs ahead of the UTC day from 22:00Z to 24:00Z.
  - Under winter time (UTC+1) the window is 23:00Z to 24:00Z.
- **In scope:**
  - the backend NUnit suite;
  - the integration hosts (`TestWebApplicationFactory<Program>`, `IntegrationTestBase`);
  - `TestToday` / `FakeLighthouseClock`;
  - production code that reads the ambient clock.
- **Out of scope:** the frontend and E2E suites. They already go through `localDate.ts` and have their own pins.

### Experiment evidence (not in CI)

I ran these in a scratch copy of the backend, not in the repository. The filter excluded the connector categories and `requires-docker`. The run was at 05:30Z, well outside the window.

| Variant | Change | Failures |
|---|---|---|
| A ("utc") | `TestToday.Clock` frozen at `<UTC date of host start> 23:30Z`, zone left at UTC. `TestWebApplicationFactory` registers `FakeTimeProvider(FrozenNow)` as `TimeProvider`. | **54** = 38 embed fixtures (one cause, see R4) + **all 16 integration-test failures from the nightly run, reproduced deterministically at 05:30Z** |
| B ("local") | Same as A, plus `TestToday.Clock` zone set to `TimeZoneInfo.Local` (Zurich) | **84** = A's 54, minus the ones fixed by the zone change, plus 3 nightly `ForecastControllerTest` failures reproduced, plus about 27 unit tests that build fixtures from real `DateTime.UtcNow` / `DateTime.Now` against a Zurich-zoned frozen clock (TeamMetricsServiceTests ×18, DeliveryWithLikelihoodDtoTest ×3, DeliveriesControllerTest ×2, TeamController/TeamsControllerTest ×2, WorkItemBaseTest ×1, LicenseServiceTest.ExpiresToday ×1) |

What the two variants show:

- Freezing the instant alone (A) turns every **integration** failure in this class into a deterministic failure.
- The 7 **unit-test** failures are not reproduced by A at 05:30Z. They compare `DateTime.Now`/`Today` (local) against a UTC-zoned `TestToday`.
- Only a ban on `DateTime.Now`/`Today` in tests (step 5), or the zone change (B), catches them.

## 1. Root cause chains (5 Whys)

### Branch A: integration tests derive "today" from UTC while the host derives it from Zurich (16 of 23)

- **WHY 1. Expected date is one day earlier than actual, or a query for "today" lands on yesterday.**
  - Evidence (log): `Expected: 2026-11-03 But was: 2026-11-04` (BlackoutForecastShift…:39, RecurringBlackoutRules…:53).
  - Evidence (log): `blockedItemsAtDate … Body: []` (Slice04…:58, Slice08, WorkItemEstimateApiIntegrationTest:164).
  - Evidence (log): likelihood `0 vs 25` (MultiTeamJointForecastDeliveryIntegrationTest:47).
- **WHY 2. The test computes today as `DateTime.UtcNow.Date`; the host computes it as `ILighthouseClock.Today` in `TimeZoneInfo.Local` = Zurich.**
  - Test side:
    - `BlackoutForecastShiftDeliveryIntegrationTest.cs:27`
    - `RecurringBlackoutRulesTestBase.cs:39`
    - `RecurringBlackoutRulesWriteBackIntegrationTest.cs:34`
    - `MultiTeamJointForecastDeliveryIntegrationTest.cs:31`
    - `Slice04PortfolioBlockedDrillThroughScenarios.cs:43`
    - `Slice08BlockedDrilldownScenarios.cs:61`
    - `WorkItemEstimateApiIntegrationTest.cs:77`
    - `Slice02FeatureBlockedCaptureSpecifications.cs:116,126`, which sends the instant `DateTime.UtcNow` as `asOfDate`
    - `PercentilesOverTimeAcceptanceTest.cs:199`, which seeds `StartedDate` from `UtcNow.Date`
  - Host side:
    - `Program.cs:1502-1507` registers `TimeProvider.System` and `LighthouseClock(ResolveInstanceTimeZone(config), …)`.
    - `LighthouseClock.cs:20`: `Today => ToInstanceDay(timeProvider.GetUtcNow())`.
    - Consumers: `PortfolioMetricsController.cs:611` (`targetDate >= clock.Today`) and `PortfolioMetricsController.cs:137` (`isHistoricRange = asOfDay < clock.Today`).
- **WHY 3. The integration host's clock is the real system clock, so the two days differ only for 1 to 2 hours per night.**
  - Evidence: `TestWebApplicationFactory.ConfigureWebHost` (TestHelpers/TestWebApplicationFactory.cs:117-133) replaces the DbContext and hosted services, but **not** `TimeProvider` or `ILighthouseClock`.
  - Only 16 of 161 integration files replace the clock themselves (`RemoveAll<TimeProvider|ILighthouseClock>`).
  - Outside the window both expressions agree, so the mismatch is invisible to every PR build that runs in daytime.
- **WHY 4. When the instance-day seam was introduced, production and unit tests were migrated, but the integration tests' private `Today` helpers were not. Their oracle re-derives the old, wrong expression.**
  - The helpers date from June: 55d8794f8 (2026-06-05) and 62eb3e7b6 (2026-06-06).
  - The seam and the TZ pin landed later: f8d075d30 and 8c00e49b2 (2026-07-27).
  - `docs/ci-learnings.md:700` names this exact pattern (`private static DateTime Today => DateTime.UtcNow.Date;` "appeared verbatim in three test files"). It is still present in 4 files.
  - `CalendarDayAnchorSeamArchUnitTest` scans **production only**, so the tests were never in reach of the guard.
- **WHY 5 (root cause R1). Tests have no frozen, boundary-placed test time.**
  - The integration host runs on real wall-clock time.
  - The Bug #5567 defect class therefore only shows when CI happens to run inside the nightly window.
  - The TZ pin made the class *possible* to detect, but not *certain*. **Detection depends on the hour the build runs.**

### Branch B: unit tests mix the local ambient day with a UTC-zoned `TestToday` (6 of 23)

- **WHY 1. Symptoms (log):**
  - NRE in `DtoExtensions.CreateForecastDtos:26`: the mock `HowMany(_, 3)` was not matched because the controller computed 4 days.
  - License `ValidFromToday` returned False (`LicenseServiceTest.cs:275`).
  - `GetCycleTimeDataForProject…` returned 0 items.
  - `GetClosedItemsForTeam…` returned 10 instead of 11.
- **WHY 2. The subject receives `TestToday.Clock`, while fixtures and inputs use `DateTime.Now` / `DateTime.Today` (Zurich).**
  - `ForecastControllerTest.cs:215,279,313` use `DateTime.Now.AddDays(3)`; the subject is built at `:711` with `TestToday.Clock`.
  - `LicenseServiceTest.cs:266` uses `ValidFrom = DateTime.Today`; the subject is built at `:413` with `TestToday.Clock`.
  - `PortfolioMetricsServiceTests.cs:366-371` and `TeamMetricsServiceTests.cs:800-804` use `DateTime.Now` / `DateTime.Today` against `TestToday.Clock` (`:53`, `:54`).
- **WHY 3. `TestToday.Clock` is zoned **UTC** (`new FakeLighthouseClock(DateTimeOffset.UtcNow)`; zone defaults to UTC, `FakeLighthouseClock.cs:20`), but the process zone is Zurich.**
  - So `TestToday.Today` is the UTC day and `DateTime.Today` is the Zurich day.
  - Production reduces local-kind instants via `InstanceCalendar.DayOf` (`.ToUniversalTime()` first). A local 01:43 therefore falls on the previous UTC day.
- **WHY 4. TestToday was designed to keep pre-seam unit-test behaviour unchanged (its own doc comment in `TestToday.cs:5-10`).**
  - It did not ban the local ambient APIs that disagree with it.
  - Nothing in the build stops `DateTime.Now`/`Today` in tests: 330 occurrences in 27 files. `BannedApiAnalyzers` is not referenced anywhere.
- **WHY 5 (root cause R2). Tests have two un-reconciled notions of "today":**
  - the ambient local clock;
  - a UTC-zoned fake.

  No mechanism forces a test to use one of them.

### Branch C: production writes a calendar-day label from UTC (1 of 23)

- **WHY 1.** `Expected Lighthouse_Log_2026.10.23.txt But was …10.22.txt` (`LogsControllerTest.cs:242`).
- **WHY 2.** `LogsController.cs:103` uses `$"Lighthouse_Log_{DateTime.UtcNow:yyyy.MM.dd}.txt"`. That is the UTC day. The test expects `DateTime.Now`.
- **WHY 3.** The arch guard (`CalendarDayAnchorSeamArchUnitTest`, `AnchorPatterns` ≈ line 78) matches `UtcNow.Date`, `DateTime.Today` and `DateOnly.FromDateTime(DateTime.`. It does not match a day reached through a **format string** (`{DateTime.UtcNow:yyyy.MM.dd}`).
- **WHY 4.** The guard is a text scan of known spellings, not a ban on the ambient API. Any new spelling escapes it.
- **WHY 5 (root cause R3).** Production still has 39 direct ambient-clock reads that bypass `TimeProvider` / `ILighthouseClock` (list in §5). Neither the compiler nor the arch test forbids them. A frozen test clock cannot reach them.

### Branch D (found by experiment, not in the nightly 23): test fixtures that forge time-bound tokens from real time

- **WHY 1.** With the host frozen, 38 embed tests fail. Symptoms:
  - `handshake 200 {}`
  - `isAuthenticated false`
- **WHY 2.** `ViewerEmbedTestHost.cs:258-259` forges the interactive cookie ticket with `IssuedUtc = DateTimeOffset.UtcNow.AddMinutes(-1)` and `ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)`. The cookie handler validates it against the host `TimeProvider`, which in the experiment was up to 18h later, so the ticket reads as expired.
- **WHY 3–5 (root cause R4).** Test helpers that create time-bound artefacts read real time instead of the host's `TimeProvider`. This is the same "two clocks" defect as R2, at the instant level. It is latent today and becomes active the moment the host clock is frozen.

### Backwards validation

- R1 + 22:00–24:00Z leads to `UtcNow.Date` ≠ Zurich `clock.Today`, which explains all 16 integration failures. Confirmed: experiment A reproduces exactly those 16 at 05:30Z.
- R2 + window leads to local `DateTime.Now` ≠ UTC-zoned `TestToday`, which explains the 6 unit failures. Confirmed: experiment B reproduces the ForecastControllerTest three.
- R3 explains LogsController.
- The branches do not contradict each other. All 23 failures are explained.

## 2. Contributing factors

1. **Some read endpoints take a `DateTime` where they mean a calendar day, and reduce it with `DateOnly.FromDateTime(date)`.**
   - Affected: `wip?asOfDate` (`PortfolioMetricsController.cs:136`) and `blockedItemsAtDate?date` (`:610`).
   - An instant sent as `…T23:43Z` is therefore read as the UTC day.
   - The frontend sends local `yyyy-MM-dd` (`MetricsService.ts:377,843`), so users are not affected. Tests that send `DateTime.UtcNow:O` are.
   - This is an API ambiguity, not a bug in this incident.
2. **`TestToday.Clock` is created at host start from real `UtcNow`.** A run crossing UTC midnight already gets a mixed result.
3. **The integration hosts that do pin a clock each do it differently.** There are 16 ad-hoc overrides; some use `ILighthouseClock`, some use `TimeProvider`.
4. **Production instants still come from `DateTime.UtcNow`**, so a frozen host clock and real-time instants mix:
   - blocked-transition `EnteredAt` / `LeftAt`, in the four DomainEvents handlers;
   - `ForecastBase.CreationTime`;
   - `UpdateTime` vs `minutesSinceLastUpdate` in the Team/Portfolio updaters.
5. **The suite only ever runs at whatever hour CI happens to start.** The nightly window was the only thing that exercised this class.

## 3. Per-failure classification (all 23)

"Production bug" means the code under test uses a different day than the instance clock.

| # | Test | Class | Why |
|---|---|---|---|
| 1-3 | ForecastControllerTest.RunManualForecast_HasRemainingItemsAndTargetDate_IncludesLikelihood / _HasTargetDate_RunsHowManyForecast / _WhenForecastHasNoTrials_ReportsNoLikelihood | **Test** | `DateTime.Now.AddDays(3)` (Zurich day) vs subject clock `TestToday.Clock` (UTC day). The controller correctly uses `clock.TodayAsUtcMidnight` (ForecastController.cs:104), computes 4 days, and the mock `HowMany(_,3)` returns null. |
| 4-6 | BlackoutForecastShiftDeliveryIntegrationTest ×3 | **Test** | `Today => DateTime.UtcNow.Date` (:27). The host forecasts from the instance day. |
| 7-8 | RecurringBlackoutRulesDeliveryIntegrationTest ×2 (date step, likelihood) | **Test** | `RecurringBlackoutRulesTestBase.cs:39`. |
| 9-10 | RecurringBlackoutRulesDownstreamParityIntegrationTest ×2 (WhenForecast_…) | **Test** | Same base class. |
| 11-12 | RecurringBlackoutRulesWriteBackIntegrationTest ×2 (TriggerForecastWriteBack_…) | **Test** | `:34`. |
| 13 | MultiTeamJointForecastDeliveryIntegrationTest.GetDelivery_MultiTeamFeature_… | **Test** | The delivery date is `UtcNow.Date + Median`. The instance day is one later, so it lands one working day short and the likelihood is 0. |
| 14 | Slice02FeatureBlockedCaptureTest.A_feature_that_stops_matching_… | **Test** (plus contributing factor 1) | Sends the instant `DateTime.UtcNow` as `asOfDate`. Production reduces it to the UTC day, which is less than `clock.Today`, so it takes the historic branch and reads the old spell. |
| 15 | Slice04PortfolioBlockedDrillThroughTest.The_latest_bar_… | **Test** | Drills at `UtcNow.Date`, which is less than `clock.Today`. It takes the reconstruction branch, where there is no history. |
| 16 | Slice08BlockedDrilldownTest.The_latest_date_lists_… | **Test** | Same. The comment at `:59` even says "real today (UtcNow.Date)". |
| 17-18 | WorkItemEstimateApiIntegrationTest Portfolio_/Team_("blockedItemsAtDate") | **Test** | `windowEnd = DateTime.UtcNow.Date.AddHours(12)` (:77). |
| 19 | Slice02WorkItemAgePercentilesTest.The_flow_coach_reads_back_… | **Test** | Seeds `StartedDate` from `UtcNow.Date` (PercentilesOverTimeAcceptanceTest.cs:199) but expects `DateTime.Today`. The instance-day age is +1. |
| 20 | LogsControllerTest.DownloadAllLogs_ReturnsFileContentResult | **Production and test** | Production labels the file with the UTC day (`LogsController.cs:103`) instead of `clock.Today`. The test uses `DateTime.Now`. |
| 21 | LicenseServiceTest.CanUsePremiumFeature_ValidLicense_ValidFromToday_ReturnsTrue | **Test** | `ValidFrom = DateTime.Today` vs UTC-zoned `TestToday.Clock`. Production correctly uses `clock.Today` (LicenseService.cs:57). |
| 22 | PortfolioMetricsServiceTests.GetCycleTimeDataForProject_FeatureClosedAtStartDate_ReturnsFeature | **Test** | Local-kind `ClosedDate` reduced in the UTC zone of `TestToday.Clock`. Production correctly uses `Clock.ToInstanceDay` (BaseMetricsService.cs:885). |
| 23 | TeamMetricsServiceTests.GetClosedItemsForTeam_IgnoresItemsOutOfDateRange | **Test** | Same, at `:800-804`. |

Totals:

- **22 test bugs and 1 production bug** (LogsController, cosmetic).
- None of the forecast, metrics or licence production paths uses a different day from the instance clock. The Bug #5567 seam holds in production.

## 4. Proposed fix, in commit-sized steps

Each step is green on its own. The suite stays green throughout, per the never-push-red rule. The regression guard comes first, and the freeze lands only after the tests it would turn red are fixed.

1. **`test(clock): pin the boundary instant as a named, asserted test fixture`.**
   - Add to `TestToday`:
     ```csharp
     internal static DateTimeOffset FrozenNow { get; } =
         new(DateTime.UtcNow.Date.AddHours(23).AddMinutes(30), TimeSpan.Zero);
     ```
   - Add a guard test `TestClockBoundaryTest`. It asserts that `ToInstanceDay(FrozenNow.UtcDateTime)` in `TimeZoneInfo.Local` ≠ `DateOnly.FromDateTime(FrozenNow.UtcDateTime)`, meaning the frozen instant sits where the UTC day and the instance day disagree. If someone moves the instant or un-pins TZ, this fails loudly. It is green now.
2. **`test(embed): forged session tickets take their time from the host clock`.**
   - In `ViewerEmbedTestHost.cs:258-259`, read `factory.Services.GetRequiredService<TimeProvider>().GetUtcNow()` instead of `DateTimeOffset.UtcNow`. (R4)
   - Grep for other forged-ticket or token helpers: `IssuedUtc|ExpiresUtc|ExpiresAt =` in TestHelpers.
3. **`test(integration): read today from the host's instance clock, not from UTC`.**
   - Add to `IntegrationTestBase`: `protected ILighthouseClock InstanceClock => WebApplicationFactory.Services.GetRequiredService<ILighthouseClock>();` and `protected DateTime InstanceToday => InstanceClock.TodayAsUtcMidnight;`.
   - Replace the private `Today => DateTime.UtcNow.Date` in these files: BlackoutForecastShiftDeliveryIntegrationTest, RecurringBlackoutRulesTestBase, RecurringBlackoutRulesWriteBackIntegrationTest, MultiTeamJointForecastDeliveryIntegrationTest.
   - Make the drill and as-of dates send the instance day as `yyyy-MM-dd`, the way the frontend does: Slice04 scenarios :43, Slice08 :61, WorkItemEstimateApiIntegrationTest:77, Slice02FeatureBlockedCaptureSpecifications:116,126.
   - Seed ages from `InstanceToday` in PercentilesOverTimeAcceptanceTest:199, and expect `InstanceClock.Today` in Slice02WorkItemAgePercentilesScenarios:32.
   - Sweep the other `UtcNow.Date` users in integration tests (30 files / 140 occurrences overall, §5). They passed nightly, but they share the pattern.
4. **`test(clock): freeze the integration host and TestToday at 23:30Z`.**
   - In `TestWebApplicationFactory.ConfigureWebHost`, add `services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new FakeTimeProvider(TestToday.FrozenNow));`. `ILighthouseClock` follows, because Program.cs:1506 resolves it from DI.
   - Set `TestToday.Clock = new(FrozenNow)`, keeping UTC for now. See step 7.
   - Per-test `WithWebHostBuilder` overrides keep working because they run later. 16 files already do this.
   - Experiment A shows that after steps 2-3 this leaves 0 failures from this class.
   - Note: the hosts not built on `TestWebApplicationFactory` (`HealthCheckTestHost`, `TelemetryTestHost`, `ClusterSubstrateProbeRegistrationTest`, `ServiceProviderValidationTest`, `EncryptionBootstrapOrderTests`, `PostgresEmbedFactory`) stay on real time. They are startup/infra tests, so that is acceptable.
5. **`test(clock): ban the local ambient clock in tests`.**
   - Reference `Microsoft.CodeAnalysis.BannedApiAnalyzers` in `Lighthouse.Backend.Tests.csproj`, with `BannedSymbols.Tests.txt` containing:
     ```
     P:System.DateTime.Now; use TestToday / the host clock
     P:System.DateTime.Today; use TestToday.Ambient
     P:System.DateTimeOffset.Now; use TestToday.FrozenNow
     ```
   - RS0030 is a warning, and `TreatWarningsAsErrors=true` (Directory.Build.props:24) makes it a build error.
   - Migrate the 27 files / 330 occurrences in §5. This fixes failures 1-3 and 20-23 for good.
6. **`fix(logs): the log download is named for the instance day`.**
   - In `LogsController`, inject `ILighthouseClock` and use `clock.Today:yyyy.MM.dd`. The test asserts against an explicit `FakeLighthouseClock` at 23:30Z with zone Zurich, using the literal day (red before the fix, green after).
   - Then **`refactor(time): production reads instants through TimeProvider`**: migrate the class (a) sites in §5 to `TimeProvider.GetUtcNow()` / `clock.Now`.
   - Cache/CacheItem measure elapsed time. Use the injected `TimeProvider`, defaulting to `TimeProvider.System`.
   - Remove the entity default initialisers (`ApiKey`, `UserProfile`, `*Permission.GrantedAt`, `ForecastBase`), so the services set them.
   - Update `CalendarDayAnchorSeamArchUnitTest`: its 4 pinned tracker-cutoff strings and `ExemptedTrackerCutoffCount=4` must change in the same commit.
   - Then ban in production with `BannedSymbols.txt`: `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`.
   - Class (c) sites get a narrow `#pragma warning disable RS0030` with a plain-language reason. `TimeProvider.System` itself is not banned, so Program.cs:476/494/538 need nothing.
7. **Optional, separate story: `test(clock): TestToday reads the day in the instance zone`.**
   - Set the `TestToday.Clock` zone to `TimeZoneInfo.Local`.
   - Experiment B: about 30 more unit tests then need their real-`UtcNow` fixtures moved to `TestToday`. Notably all of TeamMetricsServiceTests, DeliveryWithLikelihoodDtoTest, DeliveriesControllerTest, Team(s)ControllerTest, and the controllers built with `new FakeLighthouseClock(DateTimeOffset.UtcNow)`.
   - `WorkItemBaseTest…MinutesAwayOnDifferentDays_Returns1` deliberately asserts UTC-zone semantics, so it should take an explicit UTC clock.
   - This closes the remaining blind spot: unit tests can then catch a production UTC-day bug.

Dependency: BannedApiAnalyzers is a new package. Check the pnpm/NuGet min-release-age rules, and pin a released version.

## 5. Files affected

**Production direct ambient reads: 39 sites in 25 files** (`Lighthouse.Backend/Lighthouse.Backend`).

Classes: (a) instant, should go through TimeProvider · (b) calendar day, should go through ILighthouseClock · (c) legitimately real time.

| Class | Site |
|---|---|
| **(b)** | `API/LogsController.cs:103`: the day label. **The one production bug.** |
| (a) | `API/DeliveryNotesController.cs:69,110` · `Models/Auth/ApiKey.cs:25` · `Models/Auth/UserProfile.cs:19,21` · `Models/Authorization/ApiKeyPermission.cs:17` · `Models/Authorization/UserPermission.cs:19` · `Models/Forecast/ForecastBase.cs:19` · `Models/WorkTrackingSystemOptionsOwner.cs:256` · `Services/Implementation/Auth/ApiKeyService.cs:48,66,164` · `Auth/CurrentUserProfileService.cs:42,43,53` · `Authorization/RbacAdministrationService.cs:525,748,794,1426` · `BackgroundServices/Update/ForecastUpdater.cs:220` · `PortfolioUpdater.cs:36,170` · `TeamUpdater.cs:107,123` · `DomainEvents/FeatureBlockedTransitionCaptureHandler.cs:30` · `FeatureBlockedTransitionCloseHandler.cs:25` · `WorkItemBlockedTransitionCaptureHandler.cs:29` · `WorkItemBlockedTransitionCloseHandler.cs:25` · `WorkItems/WorkItemService.cs:136` · `Cache/Cache.cs:29` + `Cache/CacheItem.cs:13` (elapsed TTL) |
| (a), exempt in the arch test today | `WorkItems/WorkItemService.cs:837,840` · `AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1631` · `Jira/JiraWorkTrackingConnector.cs:2751`: UTC history cutoffs sent to trackers |
| (c) | `API/DatabaseManagementController.cs:47`: backup file name with a timestamp. `Services/Implementation/DatabaseManagement/DatabaseManagementService.cs:231`: `createdAt` in the backup manifest. Either could move to `clock.Now`; keep it in UTC on purpose. Also Program.cs:476/494/538 `TimeProvider.System` (key-ring bootstrap before DI). That one is not banned, and correctly so. |

**Already on TimeProvider:** 18 files, 53 files use `ILighthouseClock`. Examples: EmbedSessionTokenService, OAuth*, UsageDataConsent*, AppSettingService, Reports*, DatabaseMaintenanceGate.

**Tests** (`Lighthouse.Backend.Tests`):

- Any ambient read: **158 files**. `DateTime.UtcNow` ×707, `DateTimeOffset.UtcNow` ×53 in 143 files.
- Would be hit by the test ban (Now/Today/DTO.Now): **27 files, 330 occurrences**:
  - TeamMetricsControllerTest 66 · PortfolioMetricsControllerTests 65 · PortfolioUpdaterTest 23 · TeamUpdaterTest 20
  - CalendarDayAnchorSeamArchUnitTest 8 (mostly string literals, which the analyzer does not flag) · ForecastControllerTest 6 · TeamMetricsServiceTests 5
  - LicenseInformationRepositoryTest 4 · PortfolioMetricsServiceTests 4 · RunChartDataGenerator 3
  - TeamDataServiceTest 2 · LicenseServiceTest 2 · ForecastServiceTest 2
  - 1 each: AzureDevOpsWorkTrackingConnectorTest, WorkItemServiceTest, ProcessBehaviorRecordingHandlerTests, PercentilesOverTimeRecordingHandlerTests, DemoPercentilesBackfillHandlerTests, DemoBlockedHistoryBackfillHandlerTests, BlockedCountSnapshotRecordingHandlerTests, LogsControllerTest, Slice02WorkItemAgePercentilesScenarios, NamedDependencyLinkAcceptanceTest, ForecastControllerCalendarDayAnchorTest, TeamDtoTest, PortfolioDtoTest, FeatureDtoTest
- `UtcNow.Date` (day-from-UTC, not catchable by BannedApi): **30 files, 140 occurrences**. The heaviest are PortfolioMetricsServiceTests 27, DeliveryMetricsHistoryReadApiIntegrationTest 20 and BaselineValidationServiceTest 18. The 16 integration-test files among them are covered by the freeze, which makes any wrong use fail deterministically.
- Step 2 fixture: ViewerEmbedTestHost.cs:258-259. It unblocks 38 tests.
- Integration hosts: `TestWebApplicationFactory` serves 161 files. 16 files override the clock themselves.

## 6. Risk assessment

- **Frozen instant: "today's UTC date at 23:30Z" vs a fixed literal date.**
  - Choose 23:30Z of the host-start UTC date. There are 760 real-`UtcNow` fixtures across 143 files, and they stay within −23.5h…+0.5h of frozen now.
  - Experiment A shows that only fixtures which compare a **day** across the two clocks break: 16, all of them true positives. One instant fixture (the embed cookie) also breaks, and it hides 38 tests.
  - A fixed literal (e.g. 2026-06-15T23:30Z) would put every real-`UtcNow` fixture months away from "now". That is likely hundreds of breaks, and also the "a literal passes until the calendar reaches it" trap that `TestToday.AFutureDate` warns about.
  - 23:30Z is the next day in Zurich under both CEST (01:30) and CET (00:30). The step-1 guard asserts it.
- **A run that starts between 23:30Z and 24:00Z.**
  - Real now is up to 30 min *after* frozen now. "Created just now" fixtures are then up to 30 min in the future. This is low risk; nothing in experiment A depended on it.
  - A run that crosses UTC midnight has frozen D at 23:30 and real D+1. `UtcNow.Date` then equals `clock.Today`, and a wrong test passes for that run only. That is acceptable.
- **FakeTimeProvider never advances.**
  - `GetTimestamp`-based durations read 0 (ReportCommands/ReportQueries logging only).
  - `DatabaseMaintenanceGate`'s 1-minute window never elapses inside a host.
  - Timers created from the DI `TimeProvider` never fire. Hosted services are removed, and `KeyRingFileWatcher` is a hosted service.
  - Experiment A showed no failure from any of these.
  - Risk: once Cache moves to the DI `TimeProvider` (step 6), cached entries never expire inside a frozen host. A test that relies on expiry would need `Advance`. None was found. The existing helpers clear the cache explicitly.
- **Mixed clocks in production during tests.** Until step 6 lands, production instants written with `DateTime.UtcNow` (blocked spells, `UpdateTime`, `ExecutedAt`) are real-time while `clock.Now`/`Today` are frozen. Experiment A showed this causes no failures today, but a new test that compares those instants with the clock could flake. Step 6 removes it.
- **Ban scope.** The test ban misses `UtcNow.Date`, which caused 13 of the 23. That gap is covered by the freeze, not by the analyzer. Do not rely on the ban alone.
- **Blast radius of step 7 (zone change):** about 30 unit tests. Run it as its own commit or story, after the ban.
- **Sonar/CI:**
  - The new package needs a released version older than the min-release-age.
  - The `#pragma warning disable RS0030` lines need inline plain-language reasons, per CLAUDE.md.
  - Editing `CalendarDayAnchorSeamArchUnitTest` constants must happen in the same commit as the connector edits.
