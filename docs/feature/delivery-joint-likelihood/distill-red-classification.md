# DISTILL — RED classification (Story #5587, slice-01)

Run:

```
dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~ComonotonicCompletionDistributionTest|FullyQualifiedName~DeliveryCompletionForecastTest|FullyQualifiedName~DeliveryJointForecastTest|FullyQualifiedName~FeatureMissingForecastRowTest|FullyQualifiedName~DeliveryJointForecastIntegrationTest|FullyQualifiedName~DeliveryUnknownForecastDtoTest|FullyQualifiedName~DeliveryGrainSeamArchUnitTest"
```

Result 2026-07-29, with every `[Ignore]` temporarily stripped: **54 tests — 33 failed, 21 passed,
0 broken.** Build: 0 warnings. Full suite with the ignores in place: **3918 passed, 0 failed,
33 skipped**.

Every failure was classified before hand-off. `MISSING_FUNCTIONALITY` is the only correct RED; zero
tests failed on import, fixture or setup errors, and every scaffold failure reaches the scaffold body
(so the seam and the signature are right) rather than failing to resolve.

## Failing — 33 (all MISSING_FUNCTIONALITY)

### `ComonotonicCompletionDistributionTest` — 11, fixture-level `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `Min_*` (all 11) | MISSING_FUNCTIONALITY | `InvalidOperationException: __SCAFFOLD__ ComonotonicCompletionDistribution.Min is not implemented yet` — the scaffold is reached on every one |

The fixture that matters: `Min_TwoIdenticalContributors_ReturnsThatHistogramUnchanged`. Minimum leaves
an identical pair alone; `JointCompletionDistribution.Combine` on the same input squares it. A test
that passes for both operators is not coverage of `Min`.

### `DeliveryCompletionForecastTest` — 10, fixture-level `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `ContributingRows_*` (5) | MISSING_FUNCTIONALITY | `__SCAFFOLD__ DeliveryCompletionForecast.ContributingRows is not implemented yet` |
| `Build_*` (5) | MISSING_FUNCTIONALITY | `__SCAFFOLD__ DeliveryCompletionForecast.Build is not implemented yet` |

### `DeliveryJointForecastTest` — 6 of 12, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `CalculateMetrics_EveryFeatureCannotBeForecast_ReportsUnknownRatherThanZeroPercent` | MISSING_FUNCTIONALITY | expected `null`, but was `0.0` — DDD-6 in full view: `likelihood >= 0` rejects every candidate and the "no governing feature" branch reports 0 % |
| `CalculateMetrics_DeliveryFinishedBetweenForecastRuns_MovesEveryPercentileDateToToday` | MISSING_FUNCTIONALITY | expected all dates `2026-07-29`, but was `2026-09-17` ×3 — DDD-9: the persisted rows still carry their full trials |
| `CalculateMetrics_TwoFeaturesOnSeparateTeams_HeadlineAndPercentileDatesComeFromTheJointHistogram` | MISSING_FUNCTIONALITY | expected `81.0 ± 0.001`, but was `90.0`; expected 85th `2026-08-18`, but was `2026-08-08` — the governing feature answering for the delivery, badge **and** chips |
| `CalculateMetrics_ContributingPairHasNoForecastRow_ReportsUnknownRatherThanASilentCertainty` | MISSING_FUNCTIONALITY | expected `null`, but was `90.0` with three percentile dates — C1/DDD-7: Beta's work silently assumed done |
| `CalculateMetrics_ContributingPairHasNoForecastRowAndNoTeamNavigation_StillReportsUnknown` | MISSING_FUNCTIONALITY | expected `null`, but was `90.0` — guard 4, the pair-grain backstop |
| `CalculateMetrics_DeliveryWithoutADate_KeepsReportingHundredPercentAndPublishesTheJointDates` | MISSING_FUNCTIONALITY | likelihood half already `100`; expected 85th `2026-08-18`, but was `2026-08-08` — the dates half is what is missing |

### `FeatureMissingForecastRowTest` — 3 of 6, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `TeamsWithoutForecast_ContributingPairHasNoForecastRow_NamesThatTeam` | MISSING_FUNCTIONALITY | expected `["Beta"]`, actual sequence has 0 elements — the second DDD-8 clause does not exist yet |
| `CanBeForecast_ContributingPairHasNoForecastRow_IsFalse` | MISSING_FUNCTIONALITY | expected `False`, but was `True` |
| `GetLikelhoodForDate_ContributingPairHasNoForecastRow_IsUnknownRatherThanAlphasNumberAlone` | MISSING_FUNCTIONALITY | expected `null`, but was `100.0` — the feature answers with Alpha's distribution alone |

### `DeliveryUnknownForecastDtoTest` — 1 of 10 (the one added this wave), per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `FromDelivery_ContributingPairHasNoForecastRow_ReportsUnknownAndNamesThatTeam` | MISSING_FUNCTIONALITY | expected `null`, but was `100.0`; `TeamsWithoutForecast` expected `["Team Pulsar"]`, actual list has 0 elements |

### `DeliveryJointForecastIntegrationTest` — 2 of 4, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `GetDelivery_TwoFeaturesOnSeparateTeams_LikelihoodIsTheJointAcrossEveryFeature` | MISSING_FUNCTIONALITY | expected `81.0 ± 0.01`, but was `90.0` — **reached the HTTP port and deserialised the real DTO, so the wiring is proven and only the maths is missing** |
| `GetDelivery_TwoFeaturesOnSeparateTeams_PercentileDatesComeFromTheJointHistogram` | MISSING_FUNCTIONALITY | expected `2026-08-18`, but was `2026-08-08` |

## Passing — 21 (regression guards and anchors, must stay green)

| Test | Why it is green today and must stay green |
|---|---|
| `CalculateMetrics_DeliveryWithoutFeatures_ReportsZeroPercentAndNoDates` | AC-01.11. Same values, narrower reason after the change — guard 1 no longer swallows the all-un-forecastable case |
| `CalculateMetrics_OneFeatureCannotBeForecast_ReportsUnknownAndNoDates` | AC-01.10 / ADR-112 D8. The short-circuit is preserved exactly |
| `CalculateMetrics_EveryFeatureWasAlreadyFinishedAtTheLastForecastRun_ReportsHundredPercentForToday` | Guard 3 on the `{0: 0}` sentinel shape; today's path already agrees here |
| `CalculateMetrics_LateAndEarlyFeatureOnSeparateTeams_PercentileDatesAreNeverEarlierThanTheLatestFeature` | **AC-01.9**, the ADO #5435 regression re-asserted directly rather than through the deleted tie-break. The single most important guard in this set |
| `CalculateMetrics_OverdueDelivery_ReportsZeroPercentAndStillSaysWhenItWillLand` | A 0 % delivery must still publish dates, or `DeliveryMetricSnapshotRecordingHandler` reads the empty distribution as "no forecast" and the deliveries most in trouble silently stop reporting |
| `CalculateMetrics_ThreeWayFixture_HeadlineIsSeventyTwoAndEqualsTheGoverningBreakdownRow` | **GRAIN ANCHOR** — see the note below. Green under old code and new code by construction |
| `TeamsWithoutForecast_PairWithNoRemainingWorkAndNoForecastRow_IsNotNamed` | The exemption keys off remaining work, not off the absence of a forecast (AC-01.7) |
| `TeamsWithoutForecast_ContributingPairWithNoForecastRowAndNoTeamNavigation_IsNotNamed` | The dangling-name guard: DDD-8's new clause must drop unnameable teams, which is exactly why guard 4 is retained |
| `TeamsWithoutForecast_EveryContributingPairHasARow_StaysEmpty` | The negative case for both DDD-8 clauses |
| `Delivery_DoesNotReachForACompletionCombinatorDirectly` | ArchUnitNET. Green today because `Delivery` has no such dependency; it must still be green after DELIVER moves the combination into `DeliveryCompletionForecast` |
| `GetDelivery_JointRollup_LeavesTheDeliveryPayloadShapeUnchanged` | **AC-01.12**, the contract guard. 18 keys, byte-compatible with the CLI/MCP clients |
| `FromDelivery_JointRollup_AttachesNothingToTheChangeTracker` | The delivery read path is read-only over the entity graph; the carriers leave `Team`/`Feature` unset |
| `DeliveryUnknownForecastDtoTest` (9 pre-existing) | ADR-112 D8 at delivery grain, unchanged by this slice |

### The grain anchor, stated plainly

`CalculateMetrics_ThreeWayFixture_HeadlineIsSeventyTwoAndEqualsTheGoverningBreakdownRow` passes under
today's code **and** under the new maths. That is not an oversight: on the DISCUSS fixture Checkout
governs entirely and Reporting carries slack, so the correct joint (.720) **coincides** with today's
governing-feature answer — the AC-01.4 equality corner, and the reason slice 03's copy must not promise
"always lower". It is kept because it still fails loudly on a wrong *grain* (68.4 for a feature-CDF
product, 51.84 for a team term taken off `feature.Forecast`). The old-vs-new discrimination lives in
`CalculateMetrics_TwoFeaturesOnSeparateTeams_...` and in the two `DeliveryCompletionForecastTest`
fixtures, not here. Same standing as Story #5569's
`FeatureForecast_ConstantThroughputTeams_MatchesTheSlowestTeam`.

## Regression guards that DELIVER must REPAIR, not treat as a wrong-reason RED

Two pre-existing tests in `Lighthouse.Backend.Tests/Models/DeliveryTest.cs` build `FeatureWork` with no
`Team` and forecasts with no `TeamId`:

- `CalculateMetrics_MultipleFeaturesTiedOnLikelihood_WhenDistributionReflectsLatestCompletingFeature`
- `CalculateMetrics_FeatureWithZeroLikelihood_StillGovernsAndReportsForecastDates`

Under the new enumeration (`FeatureWork.Where(RemainingWorkItems > 0)` LEFT JOIN `Forecasts`, matched
on `f.Team?.Id ?? f.TeamId`) those pairs join to nothing, so guard 4 fires and both tests will go red
with `LikelihoodPercentage = null`. **That is a fixture defect, not a behaviour regression** — the
repair is to wire a real `Team` on the `FeatureWork` and the matching `TeamId` on the forecast
(`CreateForecastCompletingInDays` also needs the team). Both are already covered by direct replacements
in `DeliveryJointForecastTest` (`..._PercentileDatesAreNeverEarlierThanTheLatestFeature` and
`..._OverdueDelivery_ReportsZeroPercentAndStillSaysWhenItWillLand`), so the first one — which asserts
the *governing feature's* dates and is the behaviour AC-01.9 deletes — can also simply be removed once
its replacement is green. DISTILL deliberately did not touch them: they pass today, and separating the
fixture repair from the change that forces it would make the DELIVER diff harder to read.

## State at hand-off

The 33 RED tests carry `[Ignore("RED until Story #5587 ...")]` so the committed tree stays green —
`ComonotonicCompletionDistributionTest` and `DeliveryCompletionForecastTest` at fixture level (every
test in them is RED), the other 13 per test. **DELIVER's first act is to remove those attributes.** The
RED they encode is the entry gate, not a backlog item.

Scaffold detection: `grep -rn "__SCAFFOLD__" Lighthouse.Backend/Lighthouse.Backend/` returns 9 hits
across three files today and must return **zero** when DELIVER is done.
