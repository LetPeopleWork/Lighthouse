# RED Classification — recurring-blackout-events (DISTILL)

Pre-DELIVER fail-for-the-right-reason gate. Every acceptance test was authored as a
port-to-port NUnit integration test driving the NEW `recurring-blackout-rules` endpoint
family over real HTTP through `WebApplicationFactory<Program>`. The new endpoints do not
exist yet, so each test fails because the route is unmatched and the request falls through
to the SPA fallback middleware (`The SPA default page middleware could not return the
default page '/index.html'`) — the canonical "route missing = MISSING_FUNCTIONALITY" signal
in this repo. The app starts, auth runs, the DB seeds, and the HTTP call executes
end-to-end; only the absent controller causes the failure. This is RED, not BROKEN
(no compile error, no fixture/setup error, no import error).

Verification method: the Tests project builds clean (0 warnings, 0 errors). Three
representative tests — one per base type — were temporarily un-`[Ignore]`d and run; all
three failed identically with the SPA-fallback (route-missing) signal. They were then
re-`[Ignore]`d. `dotnet test --filter Category=recurring-blackout-events` reports
17 skipped / 0 failed, keeping the planning-wave tree green (skip-to-push convention).

| # | Test | Story | Tag | Classification |
|---|------|-------|-----|----------------|
| 1 | `RecurringBlackoutRulesWeekendsForeverIntegrationTest.CreateWeekendsForeverRule_AsPremiumSystemAdmin_RuleIsAcceptedAndListedWithHumanReadableSummary` | US-01 | @walking_skeleton @real-io | MISSING_FUNCTIONALITY (route missing — verified RED) |
| 2 | `RecurringBlackoutRulesWeekendsForeverIntegrationTest.WhenForecast_WithWeekendsForeverRule_NoPercentileDateLandsOnAWeekend` | US-01 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 3 | `RecurringBlackoutRulesIntervalRuleIntegrationTest.CreateEveryFourthFridayRule_AsPremiumSystemAdmin_RuleIsAcceptedAndListed` | US-02 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 4 | `RecurringBlackoutRulesIntervalRuleIntegrationTest.WhenForecast_WithEveryFourthFridayRule_NoPercentileDateLandsOnAFriday` | US-02 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 5 | `RecurringBlackoutRulesIntervalRuleIntegrationTest.WhenForecast_IntervalOneWeekRuleReproducesPlainWeeklyBehaviour_NoPercentileDateLandsOnAWeekend` | US-02 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 6 | `RecurringBlackoutRulesDownstreamParityIntegrationTest.WhenForecast_RecurringRuleDay_ProducesSamePercentileDateAsAnEquivalentOneOffPeriod` | US-03 | @real-io | MISSING_FUNCTIONALITY (route missing — POST step unimplemented) |
| 7 | `RecurringBlackoutRulesDownstreamParityIntegrationTest.WhenForecast_NoRecurringRulesAndNoOneOffPeriods_PercentileDateIsUnshifted` | US-03 | @real-io @regression | MISSING_FUNCTIONALITY (no `GetEffectiveBlackoutDays` union path yet) |
| 8 | `RecurringBlackoutRulesAuthorizationTests.GetAll_AsNonPremiumUser_DoesNotReturn403` | US-04 | @error @auth | MISSING_FUNCTIONALITY (route missing) |
| 9 | `RecurringBlackoutRulesAuthorizationTests.Create_AsNonPremiumUser_Returns403` | US-04 | @error @auth | MISSING_FUNCTIONALITY (route missing — verified RED) |
| 10 | `RecurringBlackoutRulesAuthorizationTests.Update_AsNonPremiumUser_Returns403` | US-04 | @error @auth | MISSING_FUNCTIONALITY (route missing) |
| 11 | `RecurringBlackoutRulesAuthorizationTests.Delete_AsNonPremiumUser_Returns403` | US-04 | @error @auth | MISSING_FUNCTIONALITY (route missing) |
| 12 | `RecurringBlackoutRulesLifecycleIntegrationTest.EditRule_ChangeWeekendsToFridayOnly_ListReflectsTheNewWeekday` | US-04 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 13 | `RecurringBlackoutRulesLifecycleIntegrationTest.DeleteRule_RemovesItFromTheList` | US-04 | @real-io | MISSING_FUNCTIONALITY (route missing) |
| 14 | `RecurringBlackoutRulesLifecycleIntegrationTest.DeleteRule_UnknownId_Returns404` | US-04 | @error | MISSING_FUNCTIONALITY (route missing) |
| 15 | `RecurringBlackoutRulesLifecycleIntegrationTest.CreateRule_WithNoWeekdays_RejectedWithWeekdayRequiredMessage` | US-04 | @error | MISSING_FUNCTIONALITY (route + validation unimplemented — verified RED) |
| 16 | `RecurringBlackoutRulesLifecycleIntegrationTest.CreateRule_WithIntervalBelowOne_RejectedWithIntervalMessage` | US-04 | @error | MISSING_FUNCTIONALITY (route + validation unimplemented) |
| 17 | `RecurringBlackoutRulesLifecycleIntegrationTest.CreateRule_WithEndBeforeStart_RejectedWithDateRangeMessage` | US-04 | @error | MISSING_FUNCTIONALITY (route + validation unimplemented) |

All 17 = MISSING_FUNCTIONALITY. Zero BROKEN. Gate PASSED.

## Addendum (Sentinel HIGH findings — US-03 AC2/AC3/AC4 per-surface coverage)

Sentinel raised that US-03 previously tested only the manual "When" forecast path
(scenarios #6/#7). The three remaining US-03 surfaces — item/feature & delivery percentile
dates + `Feature.GetLikelhoodForDate` (AC2), forecast write-back (AC3), and chart blackout
overlays (AC4) — had no dedicated test. Each is now mirrored from its shipped #4974 sibling,
seeding a recurring rule via `POST /api/latest/recurring-blackout-rules` instead of a one-off
`BlackoutPeriod`, and (where practical) asserting parity with an equivalent one-off period.

Verification method (identical to the original pass): the Tests project builds clean
(0 warnings, 0 errors); the 9 new tests were temporarily un-`[Ignore]`d and run. The 6
recurring-rule tests fail with the SPA-fallback (route-missing) signal at the rule-creation
POST — the canonical MISSING_FUNCTIONALITY RED in this repo. The 3 per-surface no-rule
guards pass GREEN today (they assert pre-feature parity and need no controller) — they are
regression guards, mirroring #4974's own `…NoBlackoutPeriods…` tests, not scaffolds. All
9 were then re-`[Ignore]`d. `dotnet test --filter Category=recurring-blackout-events`
reports 26 skipped / 0 failed.

| # | Test | Mirrors #4974 file | Story | Tag | Classification |
|---|------|--------------------|-------|-----|----------------|
| 18 | `RecurringBlackoutRulesDeliveryIntegrationTest.GetDelivery_FeatureWithRecurringRuleDays_FeaturePercentileDateStepsOverTheRecurringSpan` | `BlackoutForecastShiftDeliveryIntegrationTest` | US-03 AC2 (AC1) | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 19 | `RecurringBlackoutRulesDeliveryIntegrationTest.GetDelivery_RecurringRuleDays_FeaturePercentileDateIdenticalToEquivalentOneOffPeriod` | `BlackoutForecastShiftDeliveryIntegrationTest` | US-03 AC2 (parity) | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 20 | `RecurringBlackoutRulesDeliveryIntegrationTest.GetDelivery_FeatureWithRecurringRuleDays_LikelihoodComputedOnTheWorkingDayCount` | `BlackoutForecastShiftDeliveryIntegrationTest` | US-03 AC2 (`GetLikelhoodForDate`) | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 21 | `RecurringBlackoutRulesWriteBackIntegrationTest.TriggerForecastWriteBack_FeatureWithRecurringRuleDays_WritesTheShiftedDate` | `BlackoutForecastShiftWriteBackTest` | US-03 AC3 | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 22 | `RecurringBlackoutRulesWriteBackIntegrationTest.TriggerForecastWriteBack_RecurringRuleDays_WritesSameDateAsEquivalentOneOffPeriod` | `BlackoutForecastShiftWriteBackTest` | US-03 AC3 (parity) | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 23 | `RecurringBlackoutRulesWriteBackIntegrationTest.TriggerForecastWriteBack_NoRecurringRulesAndNoOneOffPeriods_WritesTheUnchangedDate` | `BlackoutForecastShiftWriteBackTest` | US-03 AC5 (regression) | @real-io @regression | REGRESSION_GUARD (green today; mirrors #4974 no-period guard) |
| 24 | `RecurringBlackoutRulesChartOverlayIntegrationTest.GetThroughputPbc_WithRecurringRuleDay_AnnotatesTheMatchingDataPointAsBlackout` | `TeamMetricsControllerTest.GetThroughputPbc_WithBlackoutPeriods_…` | US-03 AC4 | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 25 | `RecurringBlackoutRulesChartOverlayIntegrationTest.GetThroughputPbc_RecurringRuleDay_AnnotatesIdenticallyToEquivalentOneOffPeriod` | `TeamMetricsControllerTest.GetThroughputPbc_WithBlackoutPeriods_…` | US-03 AC4 (parity) | @real-io | MISSING_FUNCTIONALITY (route missing at rule POST) |
| 26 | `RecurringBlackoutRulesChartOverlayIntegrationTest.GetThroughputPbc_NoRecurringRulesAndNoOneOffPeriods_NoDataPointMarkedAsBlackout` | `TeamMetricsControllerTest.GetThroughputPbc_NoBlackoutPeriods_…` | US-03 AC5 (regression) | @real-io @regression | REGRESSION_GUARD (green today; mirrors #4974 no-period guard) |

6 new = MISSING_FUNCTIONALITY (route missing at rule POST). 3 new = REGRESSION_GUARD
(green today). Zero BROKEN. Gate PASSED. US-03 now traces a dedicated test to every surface:
AC1/AC2 (feature/delivery dates + `GetLikelhoodForDate`) via the Delivery file + the existing
"When" parity (#6); AC3 (write-back) via the WriteBack file; AC4 (chart overlays) via the
ChartOverlay file; AC5 (no-rule regression) via #7 + the three per-surface no-rule guards.

## Note on auth/validation tests asserting 400/403 currently failing with route-missing

Per the C#/statically-typed RED-readiness strategy, the eventual 400 (validation) and 403
(premium/RBAC guard) assertions currently fail because the route is absent (the request
falls through to SPA fallback rather than reaching a controller that returns 400/403). This
is acceptable RED — the behaviour is wholly unimplemented. DELIVER, once the controller +
guards + validation land, will see these flip GREEN as the route begins returning the real
400/403/404. The expected terminal status of each is recorded in its assertion.
