# Mutation testing — 6424 (Delivery forecasts don't match with blackout rules)

Run 2026-10-27 against the worktree @ `b65dba5a6` (plus the kill-round tests). The gate is an 80 % kill rate. Only the backend
changed; the frontend is N/A because this fix touched no frontend file.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET), first full run | **86.24 %** | 107 | 94 | 13 | 0 | 5 m 22 s |
| Backend, after the kill round | **93.46 %** | 107 | 100 | 7 | 0 | — |

The figure after the kill round is the first run's 94 kills plus the 6 that the narrowed re-run
(`stryker.6424.backend.killround.json`: projector only, 31 mutants, 26 killed, 2 m 10 s) moved from survived
to killed. Nothing else changed between the two runs.

Configs: `stryker.6424.backend.json` (full), `stryker.6424.backend.killround.json` (narrowed).

## Backend

| file | tested | survived (first run) | survived (after the kill round) |
| --- | --- | --- | --- |
| `Models/Forecast/FeatureForecastWindow.cs` | 9 | 0 | 0 |
| `Services/Implementation/DeliveryMetricValuesProjector.cs` | 31 | 11 | 5 |
| `Services/Implementation/WriteBackTriggerService.cs` | 67 | 2 | 2 |

### Closed by this pass

New tests in `DeliveryMetricValuesProjectorTest`, each asserting the exact window end rather than a lower bound:

- `BlackoutPeriodsFor_DeliveriesWithoutForecasts_ReadsTwoWeeksPastTheLatestTargetDate`: two Deliveries due in
  30 and 60 days with no forecast, so the window ends at the later target date + 14 days. This kills the
  survivors that took `today` instead of the latest date (the conditionals and `<` at lines 111 and 115) and
  `Max`→`Min` over the target dates (line 113).
- `BlackoutPeriodsFor_NoDeliveries_ReadsTwoWeeksFromToday`: an empty list reads today + 14 days. This kills the
  `false` conditional at line 111, where `Max` over an empty list would throw.
- `BlackoutPeriodsFor_TwoTeamsDeliveryAlongsideOneWithoutAForecast_ReadsAsFarAsTheFurthestDelivery`: two
  Deliveries, one with a two-Team long tail. This kills `Max`→`Min` over the Deliveries' own reach
  (line 125).

### Accepted survivors

- `DeliveryMetricValuesProjector.cs:115`, `latestDeliveryDate >= today`: an equivalent mutant. When the two
  are equal, both branches give the same date.
- `DeliveryMetricValuesProjector.cs:115`, conditional `true` (always the latest target date, even when it is
  in the past): equivalent in its effect on the result. A past target date gives an end earlier than
  today + 14, but the Features' and the Deliveries' reach never end before today + 14, so the window ends
  at the same date either way.
- `DeliveryMetricValuesProjector.cs:90` and `:96` (three mutants): the null checks on the likelihood and
  the feature-breakdown snapshot fields. This fix did not touch those lines; they lie outside its scope.
- `WriteBackTriggerService.cs:188-189` (two mutants): the statement and message of the error log in the
  per-mapping `catch`. This fix did not touch them; a log message cannot be observed through a test.

### Not mutated

- `API/DeliveriesController.cs`: 769 lines, of which this fix changed about 15. The change removes the
  controller's own copy of the window sizing and calls `DeliveryMetricValuesProjector.BlackoutPeriodsFor`
  instead. `RecurringBlackoutRulesDeliveryIntegrationTest` (left out of the filter, because the integration
  suites are slow under `perTestInIsolation`) covers that wiring, and the projector's mutants cover the
  sizing itself.
