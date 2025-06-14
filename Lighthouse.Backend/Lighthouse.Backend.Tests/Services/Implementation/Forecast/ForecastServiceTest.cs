using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    public class ForecastServiceTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IFeatureHistoryService> featureHistoryServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            featureHistoryServiceMock = new Mock<IFeatureHistoryService>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public void HowMany_ReturnsHowManyForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.HowMany(new RunChartData([]), TimeSpan.FromHours(0).Days);

            Assert.That(forecast, Is.InstanceOf<HowManyForecast>());
        }

        [Test]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(90)]
        public void HowMany_ThroughputOfOne_AllPercentilesEqualTimespan(int timespan)
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.HowMany(new RunChartData(RunChartDataGenerator.GenerateRunChartData([1])), TimeSpan.FromDays(timespan).Days);

            Assert.Multiple(() =>
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(70), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(85), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(timespan));
            });
        }

        [Test]
        public async Task When_ReturnsWhenForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = await subject.When(CreateTeam(1, [1]), 12);

            Assert.That(forecast, Is.InstanceOf<WhenForecast>());
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
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));

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
        public void PredictWorkItemCreation_GivenWorkItemTypes_CreatesHowManyForecast()
        {
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            var daysToForecast = 10;
            var workItemTypes = new string[] { "User Story" };

            var itemCreationRunChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));
            var team = CreateTeam(1, [1]);

            teamMetricsServiceMock.Setup(x => x.GetCreatedItemsForTeam(team, workItemTypes, startDate, endDate)).Returns(itemCreationRunChart);

            var subject = CreateSubjectWithRealThroughput();
            var itemCreationForecast = subject.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            Assert.Multiple(() =>
            {
                Assert.That(itemCreationForecast.GetProbability(50), Is.InRange(9, 11));
                Assert.That(itemCreationForecast.GetProbability(70), Is.InRange(7, 9));
                Assert.That(itemCreationForecast.GetProbability(85), Is.InRange(5, 7));
                Assert.That(itemCreationForecast.GetProbability(95), Is.InRange(3, 5));
            });
        }

        [Test]
        public async Task When_FixedThroughputAndRemainingDays_ReturnsCorrectForecast()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
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
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));

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
        public async Task When_RealData_RunRealForecast_ExpectCorrectResults()
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

            var feature = SetupFeature(team, 35);
            SetupFeatures(feature);

            var project = CreateProject(feature);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(35));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 20);

            SetupFeatures(feature1, feature2);
            var project = CreateProject(feature1, feature2);

            await subject.UpdateForecastsForProject(project);

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
        public async Task FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPTwo()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];

            var team = CreateTeam(2, throughput);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 15);

            SetupFeatures(feature1, feature2);
            var project = CreateProject(feature1, feature2);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPTwo()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(2, throughput);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 20);
            var feature3 = SetupFeature(team, 20);

            SetupFeatures(feature1, feature2, feature3);
            var project = CreateProject(feature1, feature2, feature3);

            await subject.UpdateForecastsForProject(project);

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
        public async Task FeatureForecast_SingleTeam_ThreeFeatures_FeatureWIPThree()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(3, throughput);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 20);
            var feature3 = SetupFeature(team, 5);

            SetupFeatures(feature1, feature2, feature3);
            var project = CreateProject(feature1, feature2, feature3);

            await subject.UpdateForecastsForProject(project);

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
        public async Task FeatureForecast_MultiTeam_TwoFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = SetupFeature(team1, 35);
            var feature2 = SetupFeature(team2, 20);

            SetupFeatures(feature1, feature2);
            var project = CreateProject(feature1, feature2);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            });
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_ThreeFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = SetupFeature(team1, 50);
            var feature2 = SetupFeature(team2, 20);
            var feature3 = SetupFeature(team2, 7);

            SetupFeatures(feature1, feature2, feature3);
            var project = CreateProject(feature1, feature2, feature3);

            await subject.UpdateForecastsForProject(project);

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
        public async Task FeatureForecast_MultiTeam_ThreeFeatures_FeatureWIPTwo()
        {
            var subject = CreateSubjectWithRealThroughput();

            var team1 = CreateTeam(2, [1]);
            var team2 = CreateTeam(2, [1]);

            var feature1 = SetupFeature(team1, 30);
            var feature2 = SetupFeature(team2, 20);
            var feature3 = SetupFeature(team2, 20);

            SetupFeatures(feature1, feature2, feature3);
            var project = CreateProject(feature1, feature2, feature3);

            await subject.UpdateForecastsForProject(project);

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
        public async Task FeatureForecast_MultiTeam_SingleFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = SetupFeature([(team1, 20, 37), (team2, 15, 17)]);

            SetupFeatures(feature1);
            var project = CreateProject(feature1);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(20));
            });
        }

        [Test]
        public async Task FeatureForecast_TeamHasNoThroughput_WillIgnoreThisTeam()
        {
            var subject = CreateSubjectWithRealThroughput();
            var team = CreateTeam(1, [0, 0, 0, 0]);

            var feature = SetupFeature([(team, 20, 37)]);
            SetupFeatures(feature);
            var project = CreateProject(feature);

            await subject.UpdateForecastsForProject(project);

            Assert.That(feature.Forecast.NumberOfItems, Is.EqualTo(20));
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_OneTeamHasNoThroughput_UsesTeamWithThroughputsForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team1 = CreateTeam(1, [0]);
            var team2 = CreateTeam(1, [1]);

            var feature = SetupFeature([(team1, 20, 42), (team2, 15, 17)]);
            SetupFeatures(feature);
            var project = CreateProject(feature);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(15));
            });
        }

        [Test]
        public async Task FeatureForecast_NoWorkRemaining_SetsDefaultForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = SetupFeature(team, 0);
            SetupFeatures(feature);
            var project = CreateProject(feature);

            await subject.UpdateForecastsForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(0));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task UpdateForecastsForProject_ArchivesFeatures()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 20);

            SetupFeatures(feature1, feature2);

            var project = CreateProject(feature1, feature2);

            await subject.UpdateForecastsForProject(project);

            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature1));
            featureHistoryServiceMock.Verify(x => x.ArchiveFeature(feature2));
        }

        private Feature SetupFeature(Team team, int remainingItems)
        {
            return SetupFeature([(team, remainingItems, remainingItems)]);
        }

        private Feature SetupFeature(IEnumerable<(Team team, int remainingItems, int totalItems)> remainingWork)
        {
            var feature = new Feature(remainingWork)
            {
                Id = idCounter,
                ReferenceId = $"{idCounter++}"
            };
            
            return feature;
        }

        private void SetupFeatures(params Feature[] features)
        {
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);
        }

        private ForecastService CreateSubjectWithPersistentThroughput()
        {
            return new ForecastService(new NotSoRandomNumberService(), Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object, featureHistoryServiceMock.Object);
        }

        private ForecastService CreateSubjectWithRealThroughput()
        {
            return new ForecastService(new RandomNumberService(), Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object, featureHistoryServiceMock.Object);
        }

        private Team CreateTeam(int featureWip, int[] throughput)
        {
            var team = new Team
            {
                Name = "Team",
                FeatureWIP = featureWip,
                Id = idCounter++,
            };

            teamMetricsServiceMock.Setup(x => x.GetCurrentThroughputForTeam(team)).Returns(new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput)));

            return team;
        }

        private Project CreateProject(params Feature[] features)
        {
            var project = CreateProject(DateTime.UtcNow, features);
            project.Teams.AddRange(features.SelectMany(f => f.Teams).Distinct());

            return project;
        }

        private Project CreateProject(DateTime lastUpdatedTime, params Feature[] features)
        {
            var project = new Project
            {
                Name = "Project",
                Id = idCounter++,
                UpdateTime = lastUpdatedTime,
            };
            project.UpdateFeatures(features);
            return project;
        }
    }
}
