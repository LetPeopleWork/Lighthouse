using CMFTAspNet.Models;
using CMFTAspNet.Services;

namespace CMFTAspNet.Tests.Services
{
    public class MonteCarloServiceTest
    {
        [Test]
        public void HowMany_ReturnsHowManyForecast()
        {
            var subject = new MonteCarloService();

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
            var subject = new MonteCarloService();
            var throughput = new Throughput([1, 1, 1, 1]);

            var forecast = subject.HowMany(throughput, TimeSpan.FromDays(timespan).Days);

            Assert.That(timespan, Is.EqualTo(forecast.GetPercentile(50)));
            Assert.That(timespan, Is.EqualTo(forecast.GetPercentile(70)));
            Assert.That(timespan, Is.EqualTo(forecast.GetPercentile(85)));
            Assert.That(timespan, Is.EqualTo(forecast.GetPercentile(95)));
        }
    }
}
