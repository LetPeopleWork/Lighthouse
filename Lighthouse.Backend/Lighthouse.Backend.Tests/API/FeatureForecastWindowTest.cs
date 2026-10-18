using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// The window blackout days are read for runs from today to twice the working days of the latest
    /// percentile a screen shows, plus two weeks of headroom, so the calendar always reaches past the
    /// last date the screen projects.
    /// </summary>
    [TestFixture]
    public class FeatureForecastWindowTest
    {
        private const int Runs = 100;

        private const int TwoWeeksOfHeadroom = 14;

        private static readonly DateTime Today = new(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void A_forecast_whose_95th_percentile_falls_on_day_40_reads_twice_that_plus_the_headroom()
        {
            var end = FeatureForecastWindow.EndFor(Today, AForecastFinishingOn(40, finished: Runs));

            Assert.That(end, Is.EqualTo(Today.AddDays((40 * 2) + TwoWeeksOfHeadroom)));
        }

        [Test]
        public void A_forecast_whose_95th_percentile_is_beyond_the_horizon_reads_twice_the_horizon_plus_the_headroom()
        {
            var end = FeatureForecastWindow.EndFor(Today, AForecastFinishingOn(40, finished: 60));

            Assert.That(end, Is.EqualTo(Today.AddDays((ForecastHorizon.Days * 2) + TwoWeeksOfHeadroom)));
        }

        [Test]
        public void Without_a_forecast_only_the_headroom_is_read()
        {
            var end = FeatureForecastWindow.EndFor(Today, (WhenForecast)null!);

            Assert.That(end, Is.EqualTo(Today.AddDays(TwoWeeksOfHeadroom)));
        }

        [Test]
        public void Several_Features_read_as_far_as_the_one_that_finishes_last()
        {
            var early = AFeatureFinishingOn(10, "PAY-1");
            var late = AFeatureFinishingOn(40, "PAY-2");

            var end = FeatureForecastWindow.EndFor(Today, [early, late]);

            Assert.That(end, Is.EqualTo(Today.AddDays((40 * 2) + TwoWeeksOfHeadroom)));
        }

        [Test]
        public void No_Features_read_only_the_headroom()
        {
            var end = FeatureForecastWindow.EndFor(Today, Array.Empty<Feature>());

            Assert.That(end, Is.EqualTo(Today.AddDays(TwoWeeksOfHeadroom)));
        }

        private static WhenForecast AForecastFinishingOn(int day, int finished)
        {
            return new WhenForecast(new Dictionary<int, int> { [day] = finished }) { TotalTrials = Runs };
        }

        private static Feature AFeatureFinishingOn(int day, string referenceId)
        {
            var team = new Team { Id = 1, Name = "Payments" };
            var feature = new Feature(team, 480) { Name = $"Feature {referenceId}", ReferenceId = referenceId };

            var forecast = AForecastFinishingOn(day, finished: Runs);
            forecast.Team = team;
            forecast.TeamId = team.Id;

            feature.Forecasts.Clear();
            feature.Forecasts.Add(forecast);

            return feature;
        }
    }
}
