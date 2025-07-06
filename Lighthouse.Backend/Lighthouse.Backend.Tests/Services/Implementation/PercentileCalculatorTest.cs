using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class PercentileCalculatorTest
    {
        [Test]
        [TestCase(0, 1)]
        [TestCase(10, 1)]
        [TestCase(20, 1)]
        [TestCase(30, 1)]
        [TestCase(40, 2)]
        [TestCase(50, 2)]
        [TestCase(60, 3)]
        [TestCase(65, 3)]
        [TestCase(69, 3)]
        [TestCase(70, 3)]
        [TestCase(75, 3)]
        [TestCase(79, 3)]
        [TestCase(80, 4)]
        [TestCase(90, 4)]
        [TestCase(100, 5)]
        public void CalculatePercentile_ShouldReturnCorrectValue_ForGivenPercentile(int percentile, int expectedValue)
        {
            var items = new List<int> { 1, 2, 3, 4, 5 };

            var result = PercentileCalculator.CalculatePercentile(items, percentile);

            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void CalculatePercentile_ShouldHandleEmptyList()
        {
            var items = new List<int>();
            var percentile = 50;

            var result = PercentileCalculator.CalculatePercentile(items, percentile);

            Assert.That(result, Is.Zero);
        }
    }
}
