using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    public class HowManyForecastTest
    {
        [Test]
        [TestCase(10, 9)]
        [TestCase(30, 7)]
        [TestCase(50, 5)]
        [TestCase(70, 5)]
        [TestCase(85, 4)]
        [TestCase(92, 3)]
        public void GetPercentile_ReturnsCorrectValue(int percentile, int expectedResult)
        {
            var simulationResult = new Dictionary<int, int>
            {
                {9, 1 },
                {3, 1 },
                {7, 2 },
                {5, 4 },
                {4, 2 },
            };

            var subject = new HowManyForecast(simulationResult, 1);

            var forecast = subject.GetProbability(percentile);

            Assert.That(forecast, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(3, 100)]
        [TestCase(4, 90)]
        [TestCase(5, 90)]
        [TestCase(9, 0)]
        public void GetLikelihood_ReportsShareOfTrialsDeliveringAtLeastTheTargetItemCount(int targetItems, int expectedLikelihood)
        {
            var simulationResult = new Dictionary<int, int>
            {
                {3, 10 },
                {5, 20 },
                {8, 70 },
            };

            var subject = new HowManyForecast(simulationResult, 1);

            var forecast = subject.GetLikelihood(targetItems);

            Assert.That(forecast, Is.EqualTo(expectedLikelihood));
        }
    }
}
