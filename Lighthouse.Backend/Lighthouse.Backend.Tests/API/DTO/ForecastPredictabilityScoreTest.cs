using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ForecastPredictabilityScoreTest
    {
        [Test]
        public void Constructor_GivenValidForecast_SetsPercentiles()
        {
            var forecast = CreateTestForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Percentiles, Has.Count.EqualTo(4));
                Assert.That(subject.Percentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(subject.Percentiles[1].Percentile, Is.EqualTo(70));
                Assert.That(subject.Percentiles[2].Percentile, Is.EqualTo(85));
                Assert.That(subject.Percentiles[3].Percentile, Is.EqualTo(95));
            }
        }

        [Test]
        public void Constructor_GivenValidForecast_CopiesForecastResults()
        {
            var forecast = CreateTestForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ForecastResults, Is.Not.Null);
                Assert.That(subject.ForecastResults, Has.Count.GreaterThan(0));
                Assert.That(subject.ForecastResults, Is.EqualTo(forecast.SimulationResult));
            }
        }

        [Test]
        public void PredictabilityScore_GivenNormalForecast_CalculatesCorrectly()
        {
            var forecast = CreateTestForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            // Expected calculation: 95th percentile (3) / 50th percentile (5) = 0.6
            var expectedScore = 3.0 / 5.0;
            Assert.That(subject.PredictabilityScore, Is.EqualTo(expectedScore).Within(0.001));
        }

        [Test]
        public void PredictabilityScore_GivenForecastWithZero50thPercentile_ReturnsZero()
        {
            var forecast = CreateForecastWithZero50thPercentile();

            var subject = new ForecastPredictabilityScore(forecast);

            Assert.That(subject.PredictabilityScore, Is.Zero);
        }

        [Test]
        public void PredictabilityScore_GivenForecastWithSamePercentiles_ReturnsOne()
        {
            var forecast = CreateForecastWithSamePercentiles();

            var subject = new ForecastPredictabilityScore(forecast);

            Assert.That(subject.PredictabilityScore, Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void PredictabilityScore_GivenHighPredictabilityForecast_ReturnsLowScore()
        {
            var forecast = CreateHighPredictabilityForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            // With descending sort: 50th percentile (10), 95th percentile (1)
            // Lower score indicates higher predictability (less variance between percentiles)
            // Expected calculation: 95th percentile (1) / 50th percentile (10) = 0.1
            var expectedScore = 1.0 / 10.0;
            Assert.That(subject.PredictabilityScore, Is.EqualTo(expectedScore).Within(0.001));
        }

        [Test]
        public void Percentiles_GivenValidForecast_ContainsCorrectValues()
        {
            var forecast = CreateTestForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            using (Assert.EnterMultipleScope())
            {
                var percentile50 = subject.Percentiles.First(p => p.Percentile == 50);
                var percentile70 = subject.Percentiles.First(p => p.Percentile == 70);
                var percentile85 = subject.Percentiles.First(p => p.Percentile == 85);
                var percentile95 = subject.Percentiles.First(p => p.Percentile == 95);

                Assert.That(percentile50.Value, Is.EqualTo(5));
                Assert.That(percentile70.Value, Is.EqualTo(4));
                Assert.That(percentile85.Value, Is.EqualTo(3));
                Assert.That(percentile95.Value, Is.EqualTo(3));
            }
        }

        [Test]
        public void ForecastResults_GivenValidForecast_IsIndependentCopy()
        {
            var forecast = CreateTestForecast();

            var subject = new ForecastPredictabilityScore(forecast);

            // Modify the original forecast results
            forecast.SimulationResult.Add(999, 1);

            // The subject's copy should not be affected
            Assert.That(subject.ForecastResults.ContainsKey(999), Is.False);
        }

        private static HowManyForecast CreateTestForecast()
        {
            var simulationResult = new Dictionary<int, int>
            {
                { 9, 1 },  // Highest value - lowest percentile
                { 7, 1 },  // Above 50th percentile
                { 5, 4 },  // 50th percentile (cumulative: 6 out of 10 = 60%)
                { 4, 2 },  // 70th percentile (cumulative: 8 out of 10 = 80%)
                { 3, 2 }   // 85th and 95th percentile (cumulative: 10 out of 10 = 100%)
            };

            return new HowManyForecast(simulationResult, 30);
        }

        private static HowManyForecast CreateForecastWithZero50thPercentile()
        {
            var simulationResult = new Dictionary<int, int>
            {
                { 0, 5 },  // 50th percentile is 0
                { 1, 3 },
                { 2, 2 }
            };

            return new HowManyForecast(simulationResult, 30);
        }

        private static HowManyForecast CreateForecastWithSamePercentiles()
        {
            var simulationResult = new Dictionary<int, int>
            {
                { 5, 10 }  // All simulations result in 5, so all percentiles are the same
            };

            return new HowManyForecast(simulationResult, 30);
        }

        private static HowManyForecast CreateHighPredictabilityForecast()
        {
            var simulationResult = new Dictionary<int, int>
            {
                { 20, 1 },  // Highest value - lowest percentile  
                { 15, 1 },
                { 10, 3 },  // 50th percentile (cumulative: 5 out of 10 = 50%)
                { 5, 2 },
                { 2, 2 },
                { 1, 1 }    // 95th percentile - lowest value, highest confidence
            };

            return new HowManyForecast(simulationResult, 30);
        }
    }
}
