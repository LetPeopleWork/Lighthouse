using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    /// <summary>
    /// Blackout days are read for a window sized by the latest percentile a screen shows. When that
    /// percentile is beyond the horizon it has no day of its own, and a window sized as though it were
    /// "no days" would shrink below the earlier percentiles, which would then be projected over a calendar
    /// with their blackouts missing.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-03")]
    [Category("us-03")]
    public class FeatureForecastWindowBeyondTheHorizonTest
    {
        private const int Runs = 100;

        private const int TheDayTheFinishingRunsFinish = 40;

        private const int FiveYearsOfSimulatedDays = 5 * 365 + 2;

        private static readonly DateTime Today = new(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Feature_whose_95th_percentile_is_beyond_the_horizon_reads_blackouts_at_least_as_far_as_one_whose_95th_has_a_day()
        {
            var sixInTenFinish = AFeatureWhoseRunsFinishOnDay40(finished: 60);
            var everyRunFinishes = AFeatureWhoseRunsFinishOnDay40(finished: Runs);

            var beyond = FeatureForecastWindow.EndFor(Today, [sixInTenFinish]);
            var withADay = FeatureForecastWindow.EndFor(Today, [everyRunFinishes]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beyond, Is.GreaterThanOrEqualTo(withADay),
                    $"The window for a Feature beyond the horizon ended {beyond:yyyy-MM-dd}, before the one whose every run finished on day 40 ({withADay:yyyy-MM-dd}).");
                Assert.That(beyond, Is.GreaterThanOrEqualTo(Today.AddDays(FiveYearsOfSimulatedDays)),
                    $"The window for a Feature beyond the horizon ended {beyond:yyyy-MM-dd}, short of the five years its 95% stands for.");
            }
        }

        /// <summary>
        /// Every run is counted whether or not it finished: the runs that did not finish are the gap between
        /// the total and the days recorded.
        /// </summary>
        private static Feature AFeatureWhoseRunsFinishOnDay40(int finished)
        {
            var team = new Team { Id = 1, Name = "Payments" };
            var feature = new Feature(team, 480) { Name = "Billing Migration", ReferenceId = "PAY-1" };

            var forecast = new WhenForecast(new Dictionary<int, int> { [TheDayTheFinishingRunsFinish] = finished })
            {
                TotalTrials = Runs,
                Team = team,
                TeamId = team.Id,
            };

            feature.Forecasts.Clear();
            feature.Forecasts.Add(forecast);

            return feature;
        }
    }
}
