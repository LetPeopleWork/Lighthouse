# DISTILL — RED classification (Story #5569, slice-01)

Run: `dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~JointCompletionDistributionTest|FullyQualifiedName~AggregatedWhenForecastTest|FullyQualifiedName~MultiTeamJointForecast"`
Result 2026-07-28: **28 tests — 18 failed, 10 passed, 0 broken.** Build: 0 warnings.

Every failure was classified before hand-off. `MISSING_FUNCTIONALITY` is the only correct RED;
zero tests failed on import, fixture or setup errors.

## Failing — 18 (all MISSING_FUNCTIONALITY)

| Test | Classification | Evidence |
|---|---|---|
| `Combine_*` (11 tests, `JointCompletionDistributionTest`) | MISSING_FUNCTIONALITY | `InvalidOperationException: __SCAFFOLD__ JointCompletionDistribution.Combine is not implemented yet` — the scaffold is reached, so the seam and the signature are right |
| `GetProbability_TwoTeamsWithMassAtTheSelectedTeamsDate_IsStrictlyLaterThanThatTeam` | MISSING_FUNCTIONALITY | expected p50 `2`, but was `1` — the worst-team copy in full view |
| `Provenance_AggregateOfMultipleTeams_CarriesNoTeamIdentity` | MISSING_FUNCTIONALITY | `Team`/`TeamId` still carry the selected team (ADR-111 not applied) |
| `Provenance_NumberOfItems_IsTheSumOfAllContributors` | MISSING_FUNCTIONALITY | expected `5`, but was `3` — copied, not summed |
| `Provenance_CreationTime_IsTheOldestContributor` | MISSING_FUNCTIONALITY | expected `2026-07-20`, but was `2026-07-27` — newest wins today |
| `ContributorWithoutTrials_IsExcludedFromTheMathsButStillCountsForProvenance` | MISSING_FUNCTIONALITY | histogram half already correct (`MaxBy` discards zero-trial); `NumberOfItems` expected `5`, but was `3` |
| `FeatureForecast_TwoTeamsWithTwoValueThroughput_IsLaterThanEveryContributingTeam` | MISSING_FUNCTIONALITY | joint p70 expected `3`, but was `2` — real Monte Carlo, real aggregation, wrong maths |
| `GetDelivery_MultiTeamFeature_LikelihoodIsTheJointProbabilityNotTheWorstTeams` | MISSING_FUNCTIONALITY | joint likelihood expected `25`, but was `50` — reached the HTTP port, so the wiring is proven and only the maths is missing |

## Passing — 10 (regression guards, must stay green)

`HasSufficientData_*` (2), `FilterApplied_*`, `ExcludedSummary_*` (2) — AC-01.7, unchanged behaviour.
`SingleContributor_HistogramAndPercentilesAreIdenticalToThatContributor` — AC-01.4, the guard that
matters most; it is green today and must stay green after the change.
`GetProbability_CrossingContributors_IsAtLeastEveryContributorsSamePercentile` — AC-01.2, green today
because `MaxBy` picks by p85; it constrains the new maths, it does not distinguish it.
`InputOrder_CrossingContributors_DoesNotChangeTheResult` — AC-01.6.
`NoContributors_ProducesAnEmptyForecast` — the empty-input guard.
`FeatureForecast_ConstantThroughputTeams_MatchesTheSlowestTeam` — Tier-4 plumbing anchor; it passes
under the old code **and** the new one by construction, and says so in a comment.

## State at hand-off

The 18 RED tests carry `[Ignore("RED until Story #5569 ...")]` so the committed tree stays green
(`JointCompletionDistributionTest` is ignored at fixture level, the other six per test). **DELIVER's
first act is to remove those attributes** — the RED they encode is the entry gate, not a backlog item.
