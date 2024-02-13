using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;
using CMFTAspNet.Services;
using CMFTAspNet.Tests.TestDoubles;

namespace CMFTAspNet.Tests.Services
{
    public class MonteCarloServiceTest
    {
        private NotSoRandomNumberService randomNumberService;

        [Test]
        public void HowMany_ReturnsHowManyForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.HowMany(new Throughput([]), TimeSpan.FromHours(0).Days);

            Assert.That(forecast, Is.InstanceOf(typeof(HowManyForecast)));
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public void HowMany_ThroughputOfOne_AllPercentilesEqualTimespan(int timespan)
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.HowMany(new Throughput([1]), TimeSpan.FromDays(timespan).Days);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.EqualTo(timespan));
                Assert.That(forecast.GetPercentile(70), Is.EqualTo(timespan));
                Assert.That(forecast.GetPercentile(85), Is.EqualTo(timespan));
                Assert.That(forecast.GetPercentile(95), Is.EqualTo(timespan));
            });
        }

        [Test]
        public void When_ReturnsWhenForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.When(new Throughput([1]), 12);

            Assert.That(forecast, Is.InstanceOf(typeof(WhenForecast)));
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public void When_ThroughputOfOne_AllPercentilesEqualTimespan(int remainingItems)
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.When(new Throughput([1]), remainingItems);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetPercentile(70), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetPercentile(85), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetPercentile(95), Is.EqualTo(remainingItems));
            });
        }

        [Test]
        public void HowMany_FixedThroughputAndSimulatedDays_ReturnsCorrectForecast()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);

            var forecast = subject.HowMany(throughput, 10);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.InRange(9, 11));
                Assert.That(forecast.GetPercentile(70), Is.InRange(7, 9));
                Assert.That(forecast.GetPercentile(85), Is.InRange(5, 7));
                Assert.That(forecast.GetPercentile(95), Is.InRange(3, 5));
            });
        }

        [Test]
        public void When_FixedThroughputAndRemainingDays_ReturnsCorrectForecast()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);

            var forecast = subject.When(throughput, 35);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.InRange(32, 34));
                Assert.That(forecast.GetPercentile(70), Is.InRange(36, 38));
                Assert.That(forecast.GetPercentile(85), Is.InRange(40, 42));
                Assert.That(forecast.GetPercentile(95), Is.InRange(46, 48));
            });
        }

        [Test]
        public void HowMany_RealData_RunRealForecast_ExpectCorrectResults()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);

            var forecast = subject.HowMany(throughput, 30);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.InRange(31, 33));
                Assert.That(forecast.GetPercentile(70), Is.InRange(27, 29));
                Assert.That(forecast.GetPercentile(85), Is.InRange(23, 25));
                Assert.That(forecast.GetPercentile(95), Is.InRange(19, 21));
            });
        }

        [Test]
        public void When_RealData_RunRealForecast_ExpectCorrectResults()
        {
            var subject = CreateSubjectWithRealThroughput();

            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);
            var forecast = subject.When(throughput, 28);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetPercentile(50), Is.InRange(26, 28));
                Assert.That(forecast.GetPercentile(70), Is.InRange(29, 31));
                Assert.That(forecast.GetPercentile(85), Is.InRange(33, 35));
                Assert.That(forecast.GetPercentile(95), Is.InRange(38, 40));

                Assert.That(forecast.GetLikelihood(30), Is.InRange(70, 73));
            });
        }

        [Test]
        public void FeatureForecast_SingleTeam_OneFeature_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = new Team(1, new Throughput([1]));

            var feature = new Feature(team, 35);

            subject.ForecastFeatures(1, feature);

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetPercentile(50), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetPercentile(70), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetPercentile(85), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetPercentile(95), Is.EqualTo(35));
            });
        }

        [Test]
        public void FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            
            var team = new Team(1, new Throughput([1]));

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);

            subject.ForecastFeatures(1, feature1, feature2);

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetPercentile(50), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetPercentile(70), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetPercentile(85), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetPercentile(95), Is.EqualTo(35));
                Assert.That(feature2.Forecast.GetPercentile(50), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetPercentile(70), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetPercentile(85), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetPercentile(95), Is.EqualTo(55));
            });
        }

        [Test]
        public void FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPTwo()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);

            var team = new Team(1, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 15);

            subject.ForecastFeatures(2, feature1, feature2);

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetPercentile(50), Is.LessThan(feature1.Forecast.GetPercentile(50)));
                Assert.That(feature2.Forecast.GetPercentile(70), Is.LessThan(feature1.Forecast.GetPercentile(70)));
                Assert.That(feature2.Forecast.GetPercentile(85), Is.LessThan(feature1.Forecast.GetPercentile(85)));
                Assert.That(feature2.Forecast.GetPercentile(95), Is.LessThan(feature1.Forecast.GetPercentile(95)));
            });
        }

        [Test]
        public void FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPTwo()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);
            var team = new Team(1, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);
            var feature3 = new Feature(team, 20);

            subject.ForecastFeatures(2, feature1, feature2, feature3);

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetPercentile(50), Is.LessThan(feature1.Forecast.GetPercentile(50)));
                Assert.That(feature2.Forecast.GetPercentile(70), Is.LessThan(feature1.Forecast.GetPercentile(70)));
                Assert.That(feature2.Forecast.GetPercentile(85), Is.LessThan(feature1.Forecast.GetPercentile(85)));
                Assert.That(feature2.Forecast.GetPercentile(95), Is.LessThan(feature1.Forecast.GetPercentile(95)));

                Assert.That(feature2.Forecast.GetPercentile(50), Is.LessThan(feature3.Forecast.GetPercentile(50)));
                Assert.That(feature2.Forecast.GetPercentile(70), Is.LessThan(feature3.Forecast.GetPercentile(70)));
                Assert.That(feature2.Forecast.GetPercentile(85), Is.LessThan(feature3.Forecast.GetPercentile(85)));
                Assert.That(feature2.Forecast.GetPercentile(95), Is.LessThan(feature3.Forecast.GetPercentile(95)));

                Assert.That(feature1.Forecast.GetPercentile(50), Is.LessThan(feature3.Forecast.GetPercentile(50)));
                Assert.That(feature1.Forecast.GetPercentile(70), Is.LessThan(feature3.Forecast.GetPercentile(70)));
                Assert.That(feature1.Forecast.GetPercentile(85), Is.LessThan(feature3.Forecast.GetPercentile(85)));
                Assert.That(feature1.Forecast.GetPercentile(95), Is.LessThan(feature3.Forecast.GetPercentile(95)));
            });
        }

        [Test]
        public void FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPThree()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new Throughput([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]);
            var team = new Team(1, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);
            var feature3 = new Feature(team, 5);

            subject.ForecastFeatures(3, feature1, feature2, feature3);

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetPercentile(50), Is.LessThan(feature1.Forecast.GetPercentile(50)));
                Assert.That(feature2.Forecast.GetPercentile(70), Is.LessThan(feature1.Forecast.GetPercentile(70)));
                Assert.That(feature2.Forecast.GetPercentile(85), Is.LessThan(feature1.Forecast.GetPercentile(85)));
                Assert.That(feature2.Forecast.GetPercentile(95), Is.LessThan(feature1.Forecast.GetPercentile(95)));

                Assert.That(feature3.Forecast.GetPercentile(50), Is.LessThan(feature2.Forecast.GetPercentile(50)));
                Assert.That(feature3.Forecast.GetPercentile(70), Is.LessThan(feature2.Forecast.GetPercentile(70)));
                Assert.That(feature3.Forecast.GetPercentile(85), Is.LessThan(feature2.Forecast.GetPercentile(85)));
                Assert.That(feature3.Forecast.GetPercentile(95), Is.LessThan(feature2.Forecast.GetPercentile(95)));

                Assert.That(feature3.Forecast.GetPercentile(50), Is.LessThan(feature1.Forecast.GetPercentile(50)));
                Assert.That(feature3.Forecast.GetPercentile(70), Is.LessThan(feature1.Forecast.GetPercentile(70)));
                Assert.That(feature3.Forecast.GetPercentile(85), Is.LessThan(feature1.Forecast.GetPercentile(85)));
                Assert.That(feature3.Forecast.GetPercentile(95), Is.LessThan(feature1.Forecast.GetPercentile(95)));
            });
        }

        private MonteCarloService CreateSubjectWithPersistentThroughput()
        {
            randomNumberService = new NotSoRandomNumberService();

            return new MonteCarloService(new NotSoRandomNumberService());
        }

        private MonteCarloService CreateSubjectWithRealThroughput()
        {
            return new MonteCarloService(new RandomNumberService(), 10000);
        }
    }
}
