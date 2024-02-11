using CMFTAspNet.Models;

namespace CMFTAspNet.Tests.Models
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
                {3, 1 },
                {4, 2 },
                {5, 4 },
                {7, 2 },
                {9, 1 }
            };

            var subject = new HowManyForecast(simulationResult);

            var forecast = subject.GetPercentile(percentile);

            Assert.That(forecast, Is.EqualTo(expectedResult));
        }
    }
}
