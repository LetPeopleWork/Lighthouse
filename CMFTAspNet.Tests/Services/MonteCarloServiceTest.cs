using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;
using CMFTAspNet.Services;
using CMFTAspNet.Tests.TestDoubles;

namespace CMFTAspNet.Tests.Services
{
    public class MonteCarloServiceTest
    {
        private NotSoRandomNumberService randomNumberService;

        private MonteCarloService subject;

        [SetUp]
        public void SetUp()
        {
            randomNumberService = new NotSoRandomNumberService();

            subject = new MonteCarloService(randomNumberService);
        }

        [Test]
        public void HowMany_ReturnsHowManyForecast()
        {
            var forecast = subject.HowMany(new Throughput([]), TimeSpan.FromHours(0).Days);

            Assert.IsInstanceOf<HowManyForecast>(forecast);
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public void HowMany_ThroughputOfOne_AllPercentilesEqualTimespan(int timespan)
        {
            var forecast = subject.HowMany(new Throughput([1]), TimeSpan.FromDays(timespan).Days);

            Assert.That(forecast.GetPercentile(50), Is.EqualTo(timespan));
            Assert.That(forecast.GetPercentile(70), Is.EqualTo(timespan));
            Assert.That(forecast.GetPercentile(85), Is.EqualTo(timespan));
            Assert.That(forecast.GetPercentile(95), Is.EqualTo(timespan));
        }

        [Test]
        public void When_ReturnsWhenForecast()
        {
            var forecast = subject.When(new Throughput([1]), 12);

            Assert.IsInstanceOf<WhenForecast>(forecast);
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public void When_ThroughputOfOne_AllPercentilesEqualTimespan(int remainingItems)
        {
            var forecast = subject.When(new Throughput([1]), remainingItems);

            Assert.That(forecast.GetPercentile(50), Is.EqualTo(remainingItems));
            Assert.That(forecast.GetPercentile(70), Is.EqualTo(remainingItems));
            Assert.That(forecast.GetPercentile(85), Is.EqualTo(remainingItems));
            Assert.That(forecast.GetPercentile(95), Is.EqualTo(remainingItems));
        }

        [Test]
        public void HowMany_FixedThroughputAndSimulatedDays_ReturnsCorrectForecast()
        {
            var throughput = new Throughput([0, 1, 0, 0, 2, 1, 0, 0, 2, 0, 0, 3, 1]);

            randomNumberService.InitializeRandomNumbers([2, 4, 7, 1, 8, 5, 3, 0, 6, 9, 3, 6, 8, 0, 5, 1, 2, 7, 9, 4, 9, 7, 3, 0, 6, 1, 2, 5, 8]);

            var forecast = subject.HowMany(throughput, 10);

            Assert.That(forecast.GetPercentile(50), Is.EqualTo(6));
            Assert.That(forecast.GetPercentile(70), Is.EqualTo(4));
            Assert.That(forecast.GetPercentile(85), Is.EqualTo(4));
            Assert.That(forecast.GetPercentile(95), Is.EqualTo(3));
        }

        [Test]
        public void When_FixedThroughputAndRemainingDays_ReturnsCorrectForecast()
        {
            var throughput = new Throughput([0, 1, 0, 0, 1, 1, 2, 0, 0, 1, 0, 2, 1]);

            randomNumberService.InitializeRandomNumbers([5, 2, 4, 1, 6, 8, 0, 9, 7, 3, 1, 7, 3, 0, 6, 5, 4, 9, 8, 2, 4, 9, 3, 8, 7, 2, 6, 5, 0]);

            var forecast = subject.When(throughput, 35);

            Assert.That(forecast.GetPercentile(50), Is.EqualTo(59));
            Assert.That(forecast.GetPercentile(70), Is.EqualTo(61));
            Assert.That(forecast.GetPercentile(85), Is.EqualTo(61));
            Assert.That(forecast.GetPercentile(95), Is.EqualTo(63));
        }
    }
}
