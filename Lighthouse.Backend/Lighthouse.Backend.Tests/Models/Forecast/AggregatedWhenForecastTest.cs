using System.Reflection;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    public class AggregatedWhenForecastTest
    {
        [Test]
        public void HasSufficientData_OneContributingForecastInsufficient_AggregatesToFalse()
        {
            var sufficient = CreateForecast(hasSufficientData: true);
            var insufficient = CreateForecast(hasSufficientData: false);

            var aggregate = new AggregatedWhenForecast([sufficient, insufficient]);

            Assert.That(aggregate.HasSufficientData, Is.False);
        }

        [Test]
        public void HasSufficientData_AllContributingForecastsSufficient_AggregatesToTrue()
        {
            var first = CreateForecast(hasSufficientData: true);
            var second = CreateForecast(hasSufficientData: true);

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.HasSufficientData, Is.True);
        }

        [Test]
        public void FilterApplied_OneContributingForecastFiltered_AggregatesToTrue()
        {
            var filtered = CreateForecast(filterApplied: true);
            var unfiltered = CreateForecast(filterApplied: false);

            var aggregate = new AggregatedWhenForecast([filtered, unfiltered]);

            Assert.That(aggregate.FilterApplied, Is.True);
        }

        [Test]
        public void ExcludedSummary_DistinctSummariesAcrossForecasts_AreJoinedAndDeduplicated()
        {
            var first = CreateForecast(excludedSummary: "excluded 2 items");
            var second = CreateForecast(excludedSummary: "excluded 5 items");
            var duplicate = CreateForecast(excludedSummary: "excluded 2 items");
            var none = CreateForecast(excludedSummary: null);

            var aggregate = new AggregatedWhenForecast([first, second, duplicate, none]);

            Assert.That(aggregate.ExcludedSummary, Is.EqualTo("excluded 2 items; excluded 5 items"));
        }

        [Test]
        public void ExcludedSummary_NoForecastReportsExclusions_IsNull()
        {
            var first = CreateForecast(excludedSummary: null);
            var second = CreateForecast(excludedSummary: "   ");

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.ExcludedSummary, Is.Null);
        }

        private static WhenForecast CreateForecast(
            bool hasSufficientData = true,
            bool filterApplied = false,
            string? excludedSummary = null)
        {
            var forecast = new WhenForecast
            {
                HasSufficientData = hasSufficientData,
                FilterApplied = filterApplied,
                ExcludedSummary = excludedSummary,
            };

            typeof(WhenForecast)
                .GetMethod("SetSimulationResult", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Dictionary<int, int>)], null)?
                .Invoke(forecast, [new Dictionary<int, int> { { 10, 100 } }]);

            return forecast;
        }
    }
}
