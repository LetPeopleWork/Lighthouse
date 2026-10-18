using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// A percentile beyond the simulated horizon has no date anybody forecast, so write-back sends the
    /// tracker nothing for it: no answer means no write, not an invented one. The percentiles that have a
    /// day are still written.
    ///
    /// The Feature is worked by two Teams, so the forecast write-back reads is the one combined across them.
    /// Each Team finished only some of its runs; the combination has to carry the runs that did not finish,
    /// or the 85% would be read off the finished ones and get a date. The subject's clock is the fixture's
    /// fixed instant, 2026-03-10, so day 12 is 2026-03-22.
    /// </summary>
    public partial class WriteBackTriggerServiceTest
    {
        private const int RunsEachTeamSimulated = 100;

        private const int TheDayTheFinishingRunsFinish = 12;

        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("epic-6311-responsive-during-updates")]
        [Category("slice-03")]
        [Category("ac-03.7")]
        [Category("ac-03.6")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public void ResolveForecastWriteBackForPortfolio_PercentileBeyondTheHorizon_WritesNothingAndKeepsThePercentileThatHasADay()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile50, WriteBackAppliesTo.Portfolio, "Custom.Forecast50", WriteBackTargetValueType.Date));

            portfolio.Features.Add(AFeatureTwoTeamsFinishInSixOfTenRuns());

            var plan = CreateSubject().ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                updates.Count == 1 &&
                updates[0].TargetFieldReference == "Custom.Forecast50" &&
                updates[0].Value == "2026-03-22");
        }

        /// <summary>
        /// Payments finished eight runs in ten and Platform three in four, each on day 12, so the Feature
        /// finished in six of every ten runs: its 50% has a day and its 85% is beyond the horizon.
        /// </summary>
        private static Feature AFeatureTwoTeamsFinishInSixOfTenRuns()
        {
            var payments = new Team { Id = 1, Name = "Payments" };
            var platform = new Team { Id = 2, Name = "Platform" };

            var feature = CreateFeature("PAY-480", StateCategories.Doing, payments, remainingItems: 240, totalItems: 250);
            feature.AddOrUpdateWorkForTeam(platform, 240, 250);

            feature.Forecasts.Add(ATeamsForecast(feature, payments, finished: 80));
            feature.Forecasts.Add(ATeamsForecast(feature, platform, finished: 75));

            return feature;
        }

        private static WhenForecast ATeamsForecast(Feature feature, Team team, int finished)
        {
            var forecast = new WhenForecast
            {
                TeamId = team.Id,
                Team = team,
                NumberOfItems = 240,
                FeatureId = feature.Id,
                Feature = feature,
                TotalTrials = RunsEachTeamSimulated,
            };
            forecast.SimulationResults.Add(new IndividualSimulationResult { Key = TheDayTheFinishingRunsFinish, Value = finished });

            return forecast;
        }
    }
}
