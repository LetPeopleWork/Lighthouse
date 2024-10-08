﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class MonteCarloServiceTest
    {
        private NotSoRandomNumberService randomNumberService;

        private Mock<IRepository<Feature>> featureRepositoryMock;

        private Mock<IFeatureHistoryService> featureHistoryServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            featureHistoryServiceMock = new Mock<IFeatureHistoryService>();
        }

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
                Assert.That(forecast.GetProbability(50), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(70), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(85), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(timespan));
            });
        }

        [Test]
        public async Task When_ReturnsWhenForecastAsync()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = await subject.When(CreateTeam(1, [1]), 12);

            Assert.That(forecast, Is.InstanceOf(typeof(WhenForecast)));
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public async Task When_ThroughputOfOne_AllPercentilesEqualTimespan(int remainingItems)
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = await subject.When(CreateTeam(1, [1]), remainingItems);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(70), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(85), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(remainingItems));
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
                Assert.That(forecast.GetProbability(50), Is.InRange(9, 11));
                Assert.That(forecast.GetProbability(70), Is.InRange(7, 9));
                Assert.That(forecast.GetProbability(85), Is.InRange(5, 7));
                Assert.That(forecast.GetProbability(95), Is.InRange(3, 5));
            });
        }

        [Test]
        public async Task When_FixedThroughputAndRemainingDays_ReturnsCorrectForecastAsync()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput =  [ 2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(1, throughput);

            var forecast = await subject.When(team, 35);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(32, 34));
                Assert.That(forecast.GetProbability(70), Is.InRange(36, 39));
                Assert.That(forecast.GetProbability(85), Is.InRange(40, 43));
                Assert.That(forecast.GetProbability(95), Is.InRange(46, 49));
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
                Assert.That(forecast.GetProbability(50), Is.InRange(30, 33));
                Assert.That(forecast.GetProbability(70), Is.InRange(27, 29));
                Assert.That(forecast.GetProbability(85), Is.InRange(23, 25));
                Assert.That(forecast.GetProbability(95), Is.InRange(19, 21));
            });
        }

        [Test]
        public async Task When_RealData_RunRealForecast_ExpectCorrectResultsAsync()
        {
            var subject = CreateSubjectWithRealThroughput();

            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var forecast = await subject.When(CreateTeam(1, throughput), 28);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(26, 28));
                Assert.That(forecast.GetProbability(70), Is.InRange(29, 31));
                Assert.That(forecast.GetProbability(85), Is.InRange(33, 36));
                Assert.That(forecast.GetProbability(95), Is.InRange(38, 41));

                Assert.That(forecast.GetLikelihood(30), Is.InRange(65, 73));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_OneFeature_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = new Feature(team, 35);
            SetupFeatures([feature]);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(35));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPOneAsync()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);

            SetupFeatures(feature1, feature2);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(35));
                Assert.That(feature2.Forecast.GetProbability(50), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(70), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(85), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(95), Is.EqualTo(55));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPTwoAsync()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];

            var team = CreateTeam(2, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 15);

            SetupFeatures(feature1, feature2);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPTwoAsync()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(2, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);
            var feature3 = new Feature(team, 20);

            SetupFeatures(feature1, feature2, feature3);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));

                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature3.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature3.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature3.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature3.Forecast.GetProbability(95)));

                Assert.That(feature1.Forecast.GetProbability(50), Is.LessThan(feature3.Forecast.GetProbability(50)));
                Assert.That(feature1.Forecast.GetProbability(70), Is.LessThan(feature3.Forecast.GetProbability(70)));
                Assert.That(feature1.Forecast.GetProbability(85), Is.LessThan(feature3.Forecast.GetProbability(85)));
                Assert.That(feature1.Forecast.GetProbability(95), Is.LessThan(feature3.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPThreeAsync()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(3, throughput);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);
            var feature3 = new Feature(team, 5);

            SetupFeatures(feature1, feature2, feature3);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));

                Assert.That(feature3.Forecast.GetProbability(50), Is.LessThan(feature2.Forecast.GetProbability(50)));
                Assert.That(feature3.Forecast.GetProbability(70), Is.LessThan(feature2.Forecast.GetProbability(70)));
                Assert.That(feature3.Forecast.GetProbability(85), Is.LessThan(feature2.Forecast.GetProbability(85)));
                Assert.That(feature3.Forecast.GetProbability(95), Is.LessThan(feature2.Forecast.GetProbability(95)));

                Assert.That(feature3.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature3.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature3.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature3.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_TwoFeatures_FeatureWIPOneAsync()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = new Feature(team1, 35);
            var feature2 = new Feature(team2, 20);

            SetupFeatures(feature1, feature2);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_ThreeFeatures_FeatureWIPOneAsync()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = new Feature(team1, 50);
            var feature2 = new Feature(team2, 20);
            var feature3 = new Feature(team2, 7);

            SetupFeatures(feature1, feature2, feature3);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));

                Assert.That(feature3.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature3.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature3.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature3.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_ThreeFeatures_FeatureWIPTwoAsync()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(2, [1]);
            var team2 = CreateTeam(2, [1]);

            var feature1 = new Feature(team1, 30);
            var feature2 = new Feature(team2, 20);
            var feature3 = new Feature(team2, 20);

            SetupFeatures(feature1, feature2, feature3);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.LessThan(feature2.Forecast.GetProbability(50)));
                Assert.That(feature1.Forecast.GetProbability(70), Is.LessThan(feature2.Forecast.GetProbability(70)));
                Assert.That(feature1.Forecast.GetProbability(85), Is.LessThan(feature2.Forecast.GetProbability(85)));
                Assert.That(feature1.Forecast.GetProbability(95), Is.LessThan(feature2.Forecast.GetProbability(95)));

                Assert.That(feature1.Forecast.GetProbability(50), Is.LessThan(feature3.Forecast.GetProbability(50)));
                Assert.That(feature1.Forecast.GetProbability(70), Is.LessThan(feature3.Forecast.GetProbability(70)));
                Assert.That(feature1.Forecast.GetProbability(85), Is.LessThan(feature3.Forecast.GetProbability(85)));
                Assert.That(feature1.Forecast.GetProbability(95), Is.LessThan(feature3.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_SingleFeatures_FeatureWIPOneAsync()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = new Feature([(team1, 20, 37), (team2, 15, 17)]);

            SetupFeatures(feature1);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(20));
            });
        }

        [Test]
        public async Task FeatureForecast_TeamHasNoThroughput_WillIgnoreThisTeamAsync()
        {
            var subject = CreateSubjectWithRealThroughput();
            var team = CreateTeam(1, [0, 0, 0 , 0]);

            var feature = new Feature([(team, 20, 37)]);

            SetupFeatures(feature);

            await subject.ForecastAllFeatures();

            Assert.That(feature.Forecast.NumberOfItems, Is.EqualTo(20));
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_OneTeamHasNoThroughput_UsesTeamWithThroughputsForecastAsync()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team1 = CreateTeam(1, [0]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = new Feature([(team1, 20, 42), (team2, 15, 17)]);

            SetupFeatures(feature1);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(15));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(15));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(15));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(15));
            });
        }

        [Test]
        public async Task FeatureForecast_NoWorkRemaining_SetsDefaultForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = new Feature();
            SetupFeatures([feature]);

            await subject.ForecastAllFeatures();

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task ForecastAllFeatures_ArchivesFeatures()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);

            SetupFeatures(feature1, feature2);

            await subject.ForecastAllFeatures();

            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature1));
            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature2));
        }

        [Test]
        public async Task UpdateForecastsForProject_ArchivesFeatures()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = new Feature(team, 35);
            var feature2 = new Feature(team, 20);
            var project = new Project();

            project.UpdateFeatures([feature1, feature2]);

            SetupFeatures(feature1, feature2);

            await subject.UpdateForecastsForProject(project);

            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature1));
            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature2));
        }

        private void SetupFeatures(params Feature[] features)
        {
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);
        }

        private MonteCarloService CreateSubjectWithPersistentThroughput()
        {
            randomNumberService = new NotSoRandomNumberService();

            return new MonteCarloService(new NotSoRandomNumberService(), featureRepositoryMock.Object, featureHistoryServiceMock.Object, Mock.Of<ILogger<MonteCarloService>>());
        }

        private MonteCarloService CreateSubjectWithRealThroughput()
        {
            return new MonteCarloService(new RandomNumberService(), featureRepositoryMock.Object, featureHistoryServiceMock.Object, Mock.Of<ILogger<MonteCarloService>>(), 10000);
        }

        private Team CreateTeam(int featureWip, int[] throughput)
        {
            var team = new Team
            {
                Name = "Team",
                FeatureWIP = featureWip,
            };

            team.UpdateThroughput(throughput);

            return team;
        }
    }
}
