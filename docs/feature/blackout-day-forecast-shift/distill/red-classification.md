# RED Classification — blackout-day-forecast-shift (Epic 4974)

Pre-DELIVER fail-for-the-right-reason gate. Every story test was run with its `[Ignore]` temporarily lifted and classified. All RED failures are `MISSING_FUNCTIONALITY` (assertion fires because the forward day↔date shift is not yet implemented) — **none** are import / fixture / compile / setup failures. The no-blackout / past-target regression guards and the two ArchUnit guards are GREEN today and must stay GREEN through DELIVER.

Test stack: NUnit 4.6 + Moq + EF-InMemory(Sqlite) + `WebApplicationFactory<Program>`. Skip marker: `[Ignore("pending DELIVER — Epic 4974 US-0X ...")]` on each story test class.

Determinism: the real Monte Carlo is replaced with a deterministic `IForecastService` (and, for the When/HowMany controller path, a stub `ITeamMetricsService.GetForecastThroughputStatus`) wired through the production composition root via `RemoveAll` + `AddScoped`. A `WhenForecast` whose simulation mass sits entirely on day 10 makes `GetProbability(p) == 10` at every percentile, so a 10-working-day forecast over a 2-day blackout must land on calendar day 12.

Run date: 2026-06-05. Worked example throughout: **10 working days + 2 future blackout days ⇒ 12 calendar days**.

## US-01 — `BlackoutForecastShiftTeamForecastIntegrationTest` (HTTP `POST /forecast/manual/{id}`, When)

| Test | Expected (after) | Observed (today) | Classification |
|---|---|---|---|
| `RunManualWhenForecast_TeamWithTwoBlackoutDaysInWindow_PercentileDateStepsOverTheBlackoutSpan` | `Today+12` | `Today+10` | RED — MISSING_FUNCTIONALITY |
| `RunManualWhenForecast_PercentileDateLandsOnBlackoutDay_RollsForwardToNextWorkingDay` | `Today+11` (rolled forward off the blackout landing day, D3) | `Today+10` | RED — MISSING_FUNCTIONALITY |
| `RunManualWhenForecast_PercentileDateLandsOnFirstDayOfConsecutiveBlackoutSpan_RollsForwardPastTheWholeSpan` | `Today+14` (rolled forward past a 4-day consecutive blackout `Today+10..Today+13` whose first day is the landing day — multi-day roll-forward, +4 not +1, D3) | `Today+10` (`2026-06-15`; blackout-blind `AddDays`) | RED — MISSING_FUNCTIONALITY |
| `RunManualWhenForecast_TeamWithNoBlackoutPeriods_PercentileDateEqualsTodayPlusDays` | `Today+10` | `Today+10` | GREEN regression guard (D6) — passes today and after |

## US-02 — `BlackoutForecastShiftItemPredictionIntegrationTest` (HTTP `POST /forecast/manual/{id}`, by-date)

`IForecastService.HowMany` is stubbed to echo the day-count the controller passes (`HowMany(_, days) → value=days`), so the observed value reveals whether the controller counted calendar days (12) or working days (10).

| Test | Expected (after) | Observed (today) | Classification |
|---|---|---|---|
| `RunManualForecast_TargetDateSpanningTwoBlackoutDays_ScoresHowManyOnTheWorkingDayCount` | `value=10` | `value=12` | RED — MISSING_FUNCTIONALITY |
| `RunManualForecast_TargetDateSpanningTwoBlackoutDays_ScoresLikelihoodOnTheWorkingDayCount` | `likelihood ≥ 100` | `likelihood ≥ 100` | GREEN guard (likelihood saturates either way; documents the by-date likelihood path) |
| `RunManualForecast_NoBlackoutDaysInWindow_WorkingDayCountEqualsCalendarDayCount` | `value=12` | `value=12` | GREEN regression guard (D6) |
| `RunManualForecast_TargetDateInThePast_KeepsExistingGuardAndReturnsNoHowMany` | empty how-many list | empty how-many list | GREEN guard (existing `timeToTargetDate<=0` guard, AC3 US-02) |

## US-03 — `BlackoutForecastShiftDeliveryIntegrationTest` (HTTP `GET /deliveries/portfolio/{id}`)

Feature forecast persisted deterministically via `feature.SetFeatureForecasts([...])`; the delivery is created through the API, then read back. The feature percentile date surfaces in `featureLikelihoods[].completionDates[].expectedDate`.

| Test | Expected (after) | Observed (today) | Classification |
|---|---|---|---|
| `GetDelivery_FeatureWithFutureBlackoutDays_FeaturePercentileDateStepsOverTheBlackoutSpan` | `Today+12` | `Today+10` (`2026-06-15`) | RED — MISSING_FUNCTIONALITY |
| `GetDelivery_FeatureWithNoBlackoutPeriods_FeaturePercentileDateUnchanged` | `Today+10` | `Today+10` | GREEN regression guard (D6) |
| `GetDelivery_MultiTeamFeatureWithBlackoutDays_StillForecastsPerTeamThenStepsTheWorstCaseDate` | `Today+12` (worst-case team still forecast per-team, then shifted, D9) | `Today+10` | RED — MISSING_FUNCTIONALITY |

## US-04 — `BlackoutForecastShiftWriteBackTest` (service-level)

**Level chosen: service test (`WriteBackTriggerService` constructed directly with Moq doubles), NOT port-to-port HTTP.** Rationale: the only honest port-to-port observation of a write-back value is a real Jira/ADO field write; that is an external non-deterministic boundary (per the Architecture-of-Reference table → fake/capture). The written value is captured at the `IWriteBackService.WriteFieldsToWorkItems` seam — the lowest level that observes the written date honestly. Feature forecast is seeded deterministically; the forecast write-back mapping (`ForecastPercentile85`, `TargetValueType=Date`) drives `ResolveForecastValue`, whose date string is captured.

| Test | Expected (after) | Observed (today) | Classification |
|---|---|---|---|
| `TriggerForecastWriteBack_FeatureWithFutureBlackoutDays_WritesTheShiftedDate` | `"2026-06-17"` (`Today+12`) | `"2026-06-15"` (`Today+10`) | RED — MISSING_FUNCTIONALITY |
| `TriggerForecastWriteBack_FeatureWithNoBlackoutPeriods_WritesTheUnchangedDate` | `Today+10` | `Today+10` | GREEN regression guard (D6, AC2) |
| `TriggerForecastWriteBack_HistoricalAndFutureBlackoutBothConfigured_DaysValueUnchangedAndDateShiftedExactlyOnce` | days value `== 10` AND written date `Today+12` (shifted once, no double-count) | days value `== 10` (already holds), written date `Today+10` | RED — MISSING_FUNCTIONALITY (compose guard, AC3) |

**Service-level seeding caveat (DELIVER input):** `WriteBackTriggerService` does not yet take `IRepository<BlackoutPeriod>`. The current ctor (4 args) cannot be handed a blackout source without a compile break, so the future-blackout tests document the blackout in their NAME and assert the shifted value — they are RED purely because the date is not shifted. In DELIVER the crafter (a) injects `IRepository<BlackoutPeriod>` into `WriteBackTriggerService` (DDD-2 fetch-once), (b) adds a `ConfigureGlobalBlackoutPeriod`-style mock seam to this test fixture, (c) `[Ignore]` is removed. Until then the test correctly fails on the value, not on construction.

## ArchUnit guards — `BlackoutForecastShiftSeamArchUnitTest` (GREEN now, stays GREEN)

| Test | Classification |
|---|---|
| `ForecastModels_DoNotDependOnRepositories` (`Models.Forecast.*` ↛ `Services.Interfaces.Repositories`) | GREEN guard — ADR-058 A1 (no repo dependency in forecast models) |
| `FeatureAndDeliveryModels_DoNotDependOnRepositories` (`Feature`/`Delivery` ↛ repositories) | GREEN guard — upholds brief's Models ↛ Repositories invariant |

These are guards, not RED feature tests — they reference only existing symbols and pass immediately. They must remain GREEN after DELIVER threads `IReadOnlyList<BlackoutPeriod>` as a parameter (A1) rather than a repository dependency.

## Verification command

```
dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~BlackoutForecastShift"
# main (Ignored): Passed: 2 (ArchUnit), Skipped: 14, Failed: 0
# un-ignored:     7 RED (MISSING_FUNCTIONALITY) + 7 GREEN guards + 2 ArchUnit GREEN
```
