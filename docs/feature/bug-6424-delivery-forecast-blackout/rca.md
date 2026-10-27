# RCA: Bug #6424, Delivery forecasts do not match under blackout rules

Status: investigated, no code changed. Paths are relative to `Lighthouse.Backend/Lighthouse.Backend/` unless stated otherwise.

## Problem statement

Setup: one Team, one Portfolio, and a Delivery whose latest Feature drives its forecast. With no blackout days, the Delivery's completion dates (70/85/95) match that Feature's dates. Add a recurring Sat+Sun weekly rule with no end date and re-forecast. The Feature's dates move later, as expected. The Delivery's dates move later too, but by less, so they no longer match the Feature.

**What is wrong: the dates, not the likelihood.** The Delivery's likelihood % is still right. Its percentile dates come out *too early*, and so do the per-Feature completion dates shown *inside* the Delivery row (`FeatureLikelihoods[].CompletionDates`). This only happens when a percentile lands more than 14 calendar days after the latest active Delivery's target date, which is the usual case for a Delivery that is at risk.

The frontend is not involved. `Lighthouse.Frontend/src/models/Delivery.ts:107` maps `completionDates` straight from the DTO and does no date arithmetic.

## Mechanism (one sentence)

The list of blackout days is a *window-truncated materialisation* of the recurring rules. The Delivery read path sizes that window from the Delivery **target date** (+14 days), not from the **forecast**. So `ProjectWorkingDays` runs past the end of the window and counts every later Saturday and Sunday as a working day.

Worked example: today is a Monday, the Delivery is due in 7 days, and the 85th percentile is 30 working days.
- Feature list, window = 30×2+14 = 74 days: Monday + 30 working days = **day 42**.
- Delivery, window = 7+14 = 21 days: 15 working days fall inside the window, then 15 more are counted as plain calendar days = **day 36**.

## 5 Whys

### Branch A: the Delivery's dates are earlier than the Feature's

- **WHY 1A.** The Delivery's percentile dates differ from the Feature's after a weekend rule is added.
  [Evidence: `Models/Delivery.cs:674-678` builds each date as `reading.ExpectedDateFrom(today, blackoutPeriods)`, and `Models/Forecast/ForecastPercentile.cs:38` does `blackoutPeriods.ProjectWorkingDays(today, Day)`. This is the same function the Feature uses (`API/DTO/WhenForecastDto.cs:25`). So the algorithm is identical and only the `blackoutPeriods` input can differ.]
- **WHY 2A.** The Delivery is handed a shorter blackout list than the Feature list.
  [Evidence: `API/DeliveriesController.cs:60-64` calls `GetEffectiveBlackoutDays(today, ForecastWindowEnd(deliveries, today))`, and `ForecastWindowEnd` at `:756-767` is `max(latest Delivery.Date, today) + 14`. The Feature endpoints use `FeatureForecastWindow.EndFor(...)` instead (`API/FeaturesController.cs:214-215`, `API/FeatureListRows.cs:29-30`), and `API/FeatureForecastWindow.cs:41-46` sets the end at `today + (95th-percentile working days × 2) + 14`.]
- **WHY 3A.** Recurring days exist only inside the window they were expanded for, and anything beyond it counts as a working day.
  [Evidence: `Services/Implementation/BlackoutPeriodService.cs:24-35` expands `rule.ExpandToBlackoutDays(windowStartDay, windowEndDay)`. `Services/Implementation/BlackoutDaysExtensions.cs:50-71` (`ProjectWorkingDays`) keeps stepping day by day with `IsBlackoutDay` and has no knowledge of where the window ends. A no-end weekly rule therefore stops at the window edge. Before the rule existed the list was empty, so window size made no difference, which is why the dates matched.]
- **WHY 4A.** The Delivery window was sized for the *likelihood* question (working days up to the target date), not for the *date* question (working days up to each percentile).
  [Evidence: commit `48a94d8ae` ("union recurring days into the delivery read path") says *"The horizon covers the latest delivery date plus a 14-day calendar headroom so the percentile projection that steps over blackout days stays inside the expanded recurring-rule window."* That assumes the percentiles fall within 14 days of the target date. `Delivery.cs:642` (likelihood, `CountWorkingDays(today, Date)`) really is bounded by `Date`. The percentile dates are not.]
- **WHY 5A, root cause A.** Window sizing is not part of the blackout contract. Every caller works out its own window size, and the delivery copy was written before, and separately from, the forecast-driven `FeatureForecastWindow`.
  [Evidence: there are three independent sizing rules: `DeliveriesController.ForecastWindowEnd` (`:756`), `DeliveryMetricValuesProjector.ForecastWindowEnd` (`Services/Implementation/DeliveryMetricValuesProjector.cs:108-117`, a verbatim copy), and `FeatureForecastWindow` (`API/FeatureForecastWindow.cs`). Commit `2974d42c2` fixed the Feature sizing for beyond-horizon percentiles but did not touch the delivery copies. `IReadOnlyList<BlackoutPeriod>` carries no window bound, so a consumer that projects past the end of its window cannot notice.]

### Branch B: the dates for the same Feature differ between the Feature view and the Delivery row

- **WHY 1B.** `FeatureLikelihoods[].CompletionDates` in the Delivery DTO also disagrees with the Features page.
  [Evidence: `API/DTO/DeliveryWithLikelihoodDto.cs:166-168` calls `feature.Forecast.CreateForecastDtos(today, blackoutPeriods, …)` with the *delivery's* list.]
- **WHY 2B to 5B.** Same chain as Branch A from WHY 2A on: the same truncated list is reused. Root cause A.

### Branch C: the same wrong dates are persisted and published

- **WHY 1C.** Daily Delivery snapshots and the forecast block published to the Jira Release carry the same too-early dates.
  [Evidence: `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs:27` and `Services/Implementation/DeliverySources/DeliveryForecastPublishingService.cs:52` both call `projector.BlackoutPeriodsFor(deliveries, today)`, which uses the copied `ForecastWindowEnd` (`DeliveryMetricValuesProjector.cs:57-62,108-117`).]
- **WHY 2C to 5C.** Same chain. Root cause A. The copy exists so that "the screen and the day's recorded history read against the same non-working days" (`DeliveriesController.cs:52-57`), and that is why the bug was copied along with it.

### Branch D (sibling defect, same family, not reported): Feature write-back dates

- **WHY 1D.** A write-back of a forecast date to a tracker field can be too early in the same way.
  [Evidence: `Services/Implementation/WriteBackTriggerService.cs:333-337` sizes the window as `windowStart.AddDays(daysAway)`, which counts *working* days as calendar days, and then calls `ProjectWorkingDays(windowStart, daysAway)`. Under a weekend rule, roughly the last 28% of the span falls outside the window.]
- **WHY 2D to 5D.** Root cause A: a fourth caller-local sizing rule.

### Ruled out

- **Stale cache or missing reforecast:** ruled out. Both views compute dates on every read from the same stored `WhenForecast` rows (`Delivery.cs:588`, `DeliveryCompletionForecast.Build`), and the reporter sees the Feature dates change.
- **Different maths (calendar versus working days):** ruled out. Both paths use `ProjectWorkingDays` and `CountWorkingDays`.
- **Likelihood mismatch:** ruled out. `CountWorkingDays(today, Date)` (`Delivery.cs:642`, `Feature.cs:239`) only reads days up to `Date`, which the delivery window always covers (window end ≥ `Date + 14`).
- **Frontend date arithmetic:** ruled out (see above).

### Backwards validation

If the window ends at `Date + 14` and a no-end weekly rule exists, then every percentile whose working-day projection passes `Date + 14` loses every weekend after that point. Its date comes out earlier than the Feature's, and only Delivery-sourced reads are affected, which matches the report. With no rule the list is empty, so the window does not matter and the dates match, which also matches. One-off periods are not truncated (`repository.GetAll()` takes all of them), so the bug needs a *recurring* rule. That matches the reporter's setup and explains why the existing one-off tests never saw it.

## Contributing factors

1. **No test where a percentile goes past the target date.** `Lighthouse.Backend.Tests/API/Integration/RecurringBlackoutRulesDeliveryIntegrationTest.cs` always uses a target date 30 days out (`CreateDelivery` default `dueDateDaysOut = 30`) against a 10-working-day forecast. Its rule also *ends* on the second blackout day (`ConfigureRecurringRuleCovering`), so truncation can never show. No test uses a no-end rule together with a late Delivery.
2. **Duplicated sizing logic.** The two `ForecastWindowEnd` copies plus `FeatureForecastWindow` plus the write-back mean any fix has to be repeated by hand. `2974d42c2` fixed one of the four.
3. **A silent failure mode.** `ProjectWorkingDays` cannot tell "no blackout here" from "outside the expanded window".
4. **The commit-message assumption** in `48a94d8ae` (percentiles within 14 days of the target date) was never written down as an invariant or tested.

## Proposed fix (minimal)

Size the delivery window to cover the forecasts as well as the target date, reusing `FeatureForecastWindow`. The latest Feature's 95th percentile bounds the Delivery's own percentiles: per Team it is the latest Feature, and across Teams it is the maximum.

1. `API/DeliveriesController.cs:756-767`, `ForecastWindowEnd`:
   ```csharp
   var targetEnd = horizon.AddDays(CalendarHeadroomDays);
   var forecastEnd = FeatureForecastWindow.EndFor(today, deliveries.SelectMany(d => d.Features));
   return targetEnd > forecastEnd ? targetEnd : forecastEnd;
   ```
2. `Services/Implementation/DeliveryMetricValuesProjector.cs:108-117`: the same change. `FeatureForecastWindow` is `internal` in `Lighthouse.Backend.API`, which is the same assembly, so it compiles. If the ArchUnit layering rules forbid Services → API, move `FeatureForecastWindow` to `Models/Forecast` (or `Services/Implementation`) in a separate `refactor:` commit first. Better still, have the controller call `projector.BlackoutPeriodsFor(...)` so that only one delivery sizing rule remains, which also guarantees "screen = recorded history".
3. Sibling defect, separate commit (or a separate Bug if preferred): `Services/Implementation/WriteBackTriggerService.cs:334-335` should use `FeatureForecastWindow.EndFor(windowStart, forecast)` (it needs a `ForecastBase`/`WhenForecast` overload) instead of `windowStart.AddDays(daysAway)`.

**Permanent prevention (P2):** move the window into the blackout service's contract. One option is a `GetEffectiveBlackoutDaysFor(today, IEnumerable<Feature>, DateTime? atLeastUntil)` that owns the sizing rule. A stronger option is a calendar object that remembers its window and expands lazily (or throws) when `ProjectWorkingDays` goes past it. Either one removes the per-caller sizing that caused this bug.

## Files affected

- `Lighthouse.Backend/Lighthouse.Backend/API/DeliveriesController.cs` (`ForecastWindowEnd`, lines 756-767)
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryMetricValuesProjector.cs` (`ForecastWindowEnd`, lines 108-117)
- possibly `Lighthouse.Backend/Lighthouse.Backend/API/FeatureForecastWindow.cs` (move, or add an overload)
- sibling: `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WriteBackTriggerService.cs` (lines 333-335)
- tests: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/RecurringBlackoutRulesDeliveryIntegrationTest.cs`, plus `DeliveryMetricValuesProjector` unit tests

## Risk

- **Low functional risk.** The window only grows, and a larger window can only add blackout days the projection should already have skipped. Dates that were right stay the same. Dates that were too early move later, to match the Feature view.
- **Visible change.** At-risk Deliveries will show later dates, and the Jira Release block will publish later dates (they will be correct). Daily snapshot history will jump on the day of deploy, since past rows were recorded with the truncated calendar. Worth a release-notes line.
- **Performance.** The expansion grows from about `target+14` days to about `2×p95 + 14` days per request. With a beyond-horizon 95th it reaches `ForecastHorizon.Days×2` (several years of weekend days, around 1000 periods), and `IsBlackoutDay` is a linear scan per projected day. The Feature list already pays this cost (since `2974d42c2`), so it is not a new class of cost, but the Delivery list and the snapshot handler now pay it too. Watch it on Epic 6400-sized portfolios.
- **Layering.** If ArchUnit forbids Services → API, the shared helper has to move first (a refactor commit).
- **Window density.** A density multiplier of 2 covers weekends (7/5 = 1.4) but not weekends combined with long one-off closures that together block more than half the days. That gap already exists in `FeatureForecastWindow` and is out of scope here.

## Regression test (fails today, passes after the fix)

In `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/RecurringBlackoutRulesDeliveryIntegrationTest.cs`, add:

`GetDelivery_WeekendRuleWithoutEnd_DeliveryAndFeatureDatesMatchWhenForecastRunsPastTheTargetDate`
- Arrange: post a recurring rule with weekdays `Saturday`, `Sunday`, `intervalWeeks = 1`, `start = today`, **no `end`**. Seed one Team and one Feature with `ForecastCompletingInWorkingDays(30)`. Create the Delivery with `dueDateDaysOut: 1`.
- Act: `GetSingleDelivery(portfolioId)`.
- Assert, for the 85th percentile:
  - `delivery.CompletionDates[85].ExpectedDate` equals an independent oracle: step forward from `Today`, skipping Saturdays and Sundays, until 30 working days have been counted.
  - `delivery.FeatureLikelihoods.Single().CompletionDates[85].ExpectedDate` equals the same oracle.
  - Optionally, the same Feature read through `/api/latest/features` (the `FeatureForecastWindow` path) gives the same date. This pins the reporter's invariant "Delivery = latest Feature" directly.
- Today: the Delivery window ends at about today+15, so everything after that counts as a working day. The date comes out at day 34 instead of day 42 (8 days early, if today is a Monday) and the test fails. After the fix the window reaches about today+74, and the test passes.

Add a unit-level twin in the `DeliveryMetricValuesProjector` tests (`BlackoutPeriodsFor` with a stubbed `IBlackoutPeriodService` asserting `windowEnd >= today + 2×p95`). That covers the snapshot and publish path as well.
