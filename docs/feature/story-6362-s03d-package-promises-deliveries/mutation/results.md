# Mutation testing — 6362 (S03d Package Promises)

Run 2026-11-17 on `main` at `dd8d8f94d` (feature C closed, code frozen). Bar (D-13, approved at DESIGN): at least 80 %
of the moved code, **and** no survivor or no-coverage mutant added by the move. The stricter of the two holds.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, first run (Stryker.NET 5.0) | **89.09 %** | 163 | 145 | 16 | 2 | 8 m |
| Backend, after one kill round | **90.80 %** | 163 | 148 | 15 | 0 | narrowed re-run 1 m |
| Frontend | N/A — no frontend file changed | | | | | |

Configs: `stryker.6362.backend.json` (the 14 files under `Modules/Promises/Domain` and `Modules/Promises/Application`),
`stryker.6362.backend.killround.json` (the two files the kill round touched, with their two test classes). Test
filter: the Promises unit tests, the three Delivery controllers' tests, `PortfolioUpdaterTest` and the Jira tests that
build the renderer without a real Jira; no `API.Integration`, no acceptance suite, no dogfood suites, no live connector
category. The after-kill-round row combines the first run's 12 untouched files with the narrowed re-run of the two
touched files (35 mutants there: 30 killed, 5 survived).

## Per file (files with a survivor or a no-coverage mutant in the first run)

| File | killed | survived | no coverage | after the kill round |
| --- | --- | --- | --- | --- |
| Application/DeliveryForecastBlockRenderer.cs | 55 | 2 | 0 | unchanged (2 timeouts counted as killed) |
| Application/DeliveryForecastPublishingService.cs | 19 | 2 | 0 | unchanged |
| Application/DeliveryMetricValuesProjector.cs | 29 | 2 | 0 | unchanged |
| Application/DeliveryRuleService.cs | 12 | 2 | 2 | 14 killed, 2 survived, 0 no coverage |
| Application/DeliverySourceSyncService.cs | 15 | 4 | 0 | 16 killed, 3 survived |
| Application/Policies/DeliveryForecastPublishingHandler.cs | 5 | 1 | 0 | unchanged |
| Application/Policies/DeliveryMetricSnapshotRecordingHandler.cs | 3 | 3 | 0 | unchanged |

`DeliverySourceResolver.cs` and the Domain file have no survivor and no uncovered mutant.

### Closed by the kill round

Each new test was seen failing against its mutant, applied by hand, before the file was restored.

- `DeliveryRuleService.cs:35` (`IsMultiValue = true`): `GetRuleSchema_AdditionalField_HoldsASingleValue`.
- `DeliveryRuleService.cs:69` (no coverage: stored rule text that reads as no rule):
  `RecomputeRuleBasedDeliveries_StoredRuleThatReadsAsNoRule_LeavesTheFeaturesAsTheyAre("")` and `("null")`.
- `DeliveryRuleService.cs:74` (the healing step): `RecomputeRuleBasedDeliveries_RuleNamingOnlyADeletedAdditionalField_LeavesTheFeaturesAsTheyAre`,
  which also covers the line-77 branch (it was a no-coverage mutant; now an equivalent survivor, below).
- `DeliverySourceSyncService.cs:205` (which unavailable reasons are permanent): two new rows on
  `What_a_Delivery_ends_up_saying_about_its_source_follows_from_the_verdict`, for a source that is gone and a source
  with no date.

### Accepted survivors

- **Equivalent:** `DeliveryMetricValuesProjector.cs:115` (both mutants): the window end is the latest of three dates,
  and the Feature forecast window is always at least today plus 14 days, so a past `latestDeliveryDate` never wins.
  `DeliveryRuleService.cs:63`: the deserializer already returns null for empty text, so the line-67 guard skips the
  same Deliveries. `DeliveryRuleService.cs:77`: a rule set with no conditions matches no Feature, and no match leaves
  the Delivery as it is. `DeliveryForecastBlockRenderer.cs:83`: `ToString("")` and `ToString("0")` give the same text
  for a whole number that was already rounded.
- **Diagnostic text, not behaviour:** `DeliveryForecastBlockRenderer.cs:60` (exception message);
  `DeliveryMetricSnapshotRecordingHandler.cs:47` and `:54` (a stopwatch that only feeds a log line) and `:57` (log
  text); the catch bodies of `DeliveryForecastPublishingService.cs:102` and `:189`, `DeliverySourceSyncService.cs:108`
  and `DeliveryForecastPublishingHandler.cs:70`. These only log, under the existing Stryker disable comments; what each
  branch does is asserted.
- **Argument guards:** `DeliverySourceSyncService.cs:15-16`, `ThrowIfNull` on parameters that are not nullable.

## Against the D-13 bar: met

Feature B moved these files with only their `namespace` and `using` lines changed (proved line by line by its slice
and whole-candidate reviews). Feature C changed two of them without logic: the renderer implements one more
interface, and the Jira connector (outside this scope) takes the port. The tests that cover them moved unchanged. So
no mutant and no outcome was added by the move; the kill round only removed survivors. The project gate of 80 % is met
on the first run and after the kill round.
