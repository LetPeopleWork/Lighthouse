using System.Collections;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    public class ForecastServiceTest
    {
        /// <summary>
        /// The two real-data tests below check that a known throughput forecasts into a known
        /// neighbourhood, and they say so in windows two or three days wide. A seedless simulation
        /// lands outside such a window every so often — three times on record for the 70th percentile
        /// alone, always 32 against a (29,31) window — and each time it reported a regression that was
        /// not one. Pinning the draw makes the same claim once instead of re-rolling it every build.
        /// The windows are unchanged, so what those tests assert is unchanged; only the dice are gone.
        /// </summary>
        private const int TheSeedTheRealDataForecastsShare = 20260916;

        private static readonly DateOnly TheInstanceDay = new(2026, 10, 23);

        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<ILogger<ForecastService>> loggerMock;

        private int idCounter;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            loggerMock = new Mock<ILogger<ForecastService>>();
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(70), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(85), Is.EqualTo(timespan));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(timespan));
            }
        }

        [Test]
        [TestCase(7, 2026, 3, 21)]
        [TestCase(30, 2026, 4, 13)]
        public void HowMany_IsStampedWithTheClockInstant_AndItsTargetDateCountsFromThere(int days, int targetYear, int targetMonth, int targetDay)
        {
            var subject = CreateSubjectWithPersistentThroughput(new FakeTimeProvider(new DateTimeOffset(2026, 3, 14, 23, 30, 0, TimeSpan.Zero)));

            var forecast = subject.HowMany(new RunChartData(RunChartDataGenerator.GenerateRunChartData([1])), days);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.CreationTime, Is.EqualTo(new DateTime(2026, 3, 14, 23, 30, 0, DateTimeKind.Utc)));
                Assert.That(forecast.TargetDate, Is.EqualTo(new DateTime(targetYear, targetMonth, targetDay, 23, 30, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task When_IsStampedWithTheClockInstant()
        {
            var subject = CreateSubjectWithPersistentThroughput(new FakeTimeProvider(new DateTimeOffset(2026, 3, 14, 23, 30, 0, TimeSpan.Zero)));

            var forecast = await subject.When(CreateTeam(1, [1]), 12);

            Assert.That(forecast.CreationTime, Is.EqualTo(new DateTime(2026, 3, 14, 23, 30, 0, DateTimeKind.Utc)));
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(70), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(85), Is.EqualTo(remainingItems));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(remainingItems));
            }
        }

        [Test]
        public void HowMany_FixedThroughputAndSimulatedDays_ReturnsCorrectForecast()
        {
            var subject = CreateSubjectWithRealThroughput();
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));

            var forecast = subject.HowMany(throughput, 10);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(9, 11));
                Assert.That(forecast.GetProbability(70), Is.InRange(7, 9));
                Assert.That(forecast.GetProbability(85), Is.InRange(5, 7));
                Assert.That(forecast.GetProbability(95), Is.InRange(3, 5));
            }
        }

        [Test]
        public void PredictWorkItemCreation_GivenWorkItemTypes_CreatesHowManyForecast()
        {
            var startDate = TestToday.AmbientAsUtcMidnight.AddDays(-30);
            var endDate = TestToday.AmbientAsUtcMidnight;

            const int daysToForecast = 10;
            var workItemTypes = new[] { "User Story" };

            var itemCreationRunChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));
            var team = CreateTeam(1, [1]);

            teamMetricsServiceMock.Setup(x => x.GetCreatedItemsForTeam(team, workItemTypes, startDate, endDate)).Returns(itemCreationRunChart);

            var subject = CreateSubjectWithRealThroughput();
            var itemCreationForecast = subject.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(itemCreationForecast.GetProbability(50), Is.InRange(9, 11));
                Assert.That(itemCreationForecast.GetProbability(70), Is.InRange(7, 9));
                Assert.That(itemCreationForecast.GetProbability(85), Is.InRange(5, 7));
                Assert.That(itemCreationForecast.GetProbability(95), Is.InRange(3, 5));
            }
        }

        [Test]
        public async Task When_FixedThroughputAndRemainingDays_ReturnsCorrectForecast()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(1, throughput);

            var forecast = await subject.When(team, 35);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(32, 34));
                Assert.That(forecast.GetProbability(70), Is.InRange(36, 39));
                Assert.That(forecast.GetProbability(85), Is.InRange(40, 43));
                Assert.That(forecast.GetProbability(95), Is.InRange(46, 49));
            }
        }

        [Test]
        public async Task When_FixedThroughput_TargetDateInPast_ReturnZeroLikelihood()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(1, throughput);

            var forecast = await subject.When(team, 35);

            Assert.That(forecast.GetLikelihood(-1), Is.Zero);
        }

        [Test]
        public async Task When_FixedThroughput_TargetDateToday_ReturnZeroLikelihood()
        {
            var subject = CreateSubjectWithRealThroughput();
            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var team = CreateTeam(1, throughput);

            var forecast = await subject.When(team, 35);

            Assert.That(forecast.GetLikelihood(0), Is.Zero);
        }

        [Test]
        public void HowMany_RealData_RunRealForecast_ExpectCorrectResults()
        {
            var subject = CreateSubjectWithPinnedDraws();
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0]));

            var forecast = subject.HowMany(throughput, 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(30, 33));
                Assert.That(forecast.GetProbability(70), Is.InRange(27, 29));
                Assert.That(forecast.GetProbability(85), Is.InRange(23, 25));
                Assert.That(forecast.GetProbability(95), Is.InRange(19, 21));
            }
        }

        [Test]
        public async Task When_RealData_RunRealForecast_ExpectCorrectResults()
        {
            var subject = CreateSubjectWithPinnedDraws();

            int[] throughput = [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];
            var forecast = await subject.When(CreateTeam(1, throughput), 28);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.InRange(26, 28));
                Assert.That(forecast.GetProbability(70), Is.InRange(29, 31));
                Assert.That(forecast.GetProbability(85), Is.InRange(33, 36));
                Assert.That(forecast.GetProbability(95), Is.InRange(38, 41));

                Assert.That(forecast.GetLikelihood(30), Is.InRange(65, 73));
            }
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_OneFeature_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = SetupFeature(team, 35);
            SetupFeatures(feature);

            var project = CreatePortfolio(feature);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(35));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(35));
            }
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_AnswersComputed_AndSavesTheForecasts()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = SetupFeature(team, 35);
            SetupFeatures(feature);

            var project = CreatePortfolio(feature);

            var outcome = await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.InstanceOf<ForecastRunOutcome.Computed>());
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                featureRepositoryMock.Verify(x => x.Save(), Times.Once);
            }
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_RecordedInputsEqualTheGatheredOnes_ConfirmsTheForecastsAndWritesNothing()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var feature = SetupFeature(CreateTeam(1, [1]), 35);
            SetupFeatures(feature);
            var portfolio = CreatePortfolio(feature);

            var firstRun = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);
            var recorded = ((ForecastRunOutcome.Computed)firstRun).FreshFingerprint;
            portfolio.ForecastInputFingerprint = recorded;
            var forecastsBefore = feature.Forecasts.ToList();
            var startForecastsBefore = feature.StartForecasts.ToList();
            featureRepositoryMock.Invocations.Clear();

            var outcome = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            Assert.That(outcome, Is.TypeOf<ForecastRunOutcome.ConfirmedUnchanged>());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(((ForecastRunOutcome.ConfirmedUnchanged)outcome).FeatureIds, Is.EquivalentTo(new[] { feature.Id }));
                Assert.That(feature.Forecasts, Is.EqualTo(forecastsBefore).Using((IEqualityComparer)ReferenceEqualityComparer.Instance));
                Assert.That(feature.StartForecasts, Is.EqualTo(startForecastsBefore).Using((IEqualityComparer)ReferenceEqualityComparer.Instance));
                Assert.That(portfolio.ForecastInputFingerprint, Is.EqualTo(recorded));
                featureRepositoryMock.Verify(x => x.Save(), Times.Never);
            }
        }

        [TestCase(null)]
        [TestCase("forgotten:6b1f0e2c9d8a4b7e9f3c5a1d2e4b6c8a")]
        public async Task UpdateForecastsForPortfolio_NothingOrAForgetRecorded_ComputesAndSavesWithoutRecording(string? recorded)
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var feature = SetupFeature(CreateTeam(1, [1]), 35);
            SetupFeatures(feature);
            var portfolio = CreatePortfolio(feature);
            portfolio.ForecastInputFingerprint = recorded;

            var outcome = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            AssertComputedAndSavedWithoutRecording(outcome, portfolio, feature, recorded);
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_InputsRecordedOnAnEarlierDay_ComputesAndSavesWithoutRecording()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var feature = SetupFeature(CreateTeam(1, [1]), 35);
            SetupFeatures(feature);
            var portfolio = CreatePortfolio(feature);

            var yesterday = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay.AddDays(-1));
            var recorded = ((ForecastRunOutcome.Computed)yesterday).FreshFingerprint;
            portfolio.ForecastInputFingerprint = recorded;
            featureRepositoryMock.Invocations.Clear();

            var outcome = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            Assert.That(recorded, Is.Not.Null);
            AssertComputedAndSavedWithoutRecording(outcome, portfolio, feature, recorded);
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_InputsCannotBeFingerprinted_ComputesWithNothingToRecordAndWarns()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var feature = SetupFeature(CreateTeam(1, [1]), 35);
            typeof(Feature).GetProperty(nameof(Feature.ReferenceId))?.SetValue(feature, null);
            SetupFeatures(feature);
            var portfolio = CreatePortfolio(feature);
            portfolio.ForecastInputFingerprint = "0123456789abcdef0123456789abcdef";

            var outcome = await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(new ForecastRunOutcome.Computed("0123456789abcdef0123456789abcdef", null)));
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                featureRepositoryMock.Verify(x => x.Save(), Times.Once);
                loggerMock.Verify(
                    logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_ReadsOnlyTheFeaturesOfThePortfoliosTeams()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var feature = SetupFeature(CreateTeam(1, [1]), 35);
            var ofAnotherTeam = SetupFeature(CreateTeam(1, [1]), 10);
            SetupFeatures(feature, ofAnotherTeam);
            var portfolio = CreatePortfolio(feature);

            await subject.UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(ofAnotherTeam.Forecasts, Is.Empty);
            }
        }

        [Test]
        public async Task FeatureForecast_SingleTeam_TwoFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature1 = SetupFeature(team, 35);
            var feature2 = SetupFeature(team, 20);

            SetupFeatures(feature1, feature2);
            var project = CreatePortfolio(feature1, feature2);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(35));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(35));
                Assert.That(feature2.Forecast.GetProbability(50), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(70), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(85), Is.EqualTo(55));
                Assert.That(feature2.Forecast.GetProbability(95), Is.EqualTo(55));
            }
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
            var project = CreatePortfolio(feature1, feature2);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            }
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
            var project = CreatePortfolio(feature1, feature2, feature3);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
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
            }
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
            var project = CreatePortfolio(feature1, feature2, feature3);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
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
            }
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
            var project = CreatePortfolio(feature1, feature2);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            }
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
            var project = CreatePortfolio(feature1, feature2, feature3);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature2.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature2.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature2.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature2.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));

                Assert.That(feature3.Forecast.GetProbability(50), Is.LessThan(feature1.Forecast.GetProbability(50)));
                Assert.That(feature3.Forecast.GetProbability(70), Is.LessThan(feature1.Forecast.GetProbability(70)));
                Assert.That(feature3.Forecast.GetProbability(85), Is.LessThan(feature1.Forecast.GetProbability(85)));
                Assert.That(feature3.Forecast.GetProbability(95), Is.LessThan(feature1.Forecast.GetProbability(95)));
            }
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
            var project = CreatePortfolio(feature1, feature2, feature3);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.LessThan(feature2.Forecast.GetProbability(50)));
                Assert.That(feature1.Forecast.GetProbability(70), Is.LessThan(feature2.Forecast.GetProbability(70)));
                Assert.That(feature1.Forecast.GetProbability(85), Is.LessThan(feature2.Forecast.GetProbability(85)));
                Assert.That(feature1.Forecast.GetProbability(95), Is.LessThan(feature2.Forecast.GetProbability(95)));

                Assert.That(feature1.Forecast.GetProbability(50), Is.LessThan(feature3.Forecast.GetProbability(50)));
                Assert.That(feature1.Forecast.GetProbability(70), Is.LessThan(feature3.Forecast.GetProbability(70)));
                Assert.That(feature1.Forecast.GetProbability(85), Is.LessThan(feature3.Forecast.GetProbability(85)));
                Assert.That(feature1.Forecast.GetProbability(95), Is.LessThan(feature3.Forecast.GetProbability(95)));
            }
        }

        [Test]
        public async Task FeatureForecast_MultiTeam_SingleFeatures_FeatureWIPOne()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team1 = CreateTeam(1, [1]);
            var team2 = CreateTeam(1, [1]);

            var feature1 = SetupFeature([(team1, 20, 37), (team2, 15, 17)]);

            SetupFeatures(feature1);
            var project = CreatePortfolio(feature1);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature1.Forecast.GetProbability(50), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(70), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(85), Is.EqualTo(20));
                Assert.That(feature1.Forecast.GetProbability(95), Is.EqualTo(20));
            }
        }

        [Test]
        public async Task FeatureForecast_TeamHasNoThroughput_WillIgnoreThisTeam()
        {
            var subject = CreateSubjectWithRealThroughput();
            var team = CreateTeam(1, [0, 0, 0, 0]);

            var feature = SetupFeature([(team, 20, 37)]);
            SetupFeatures(feature);
            var project = CreatePortfolio(feature);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

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
            var project = CreatePortfolio(feature);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(15));
                Assert.That(feature.Forecast.GetProbability(95), Is.EqualTo(15));
            }
        }

        [Test]
        public async Task FeatureForecast_NoWorkRemaining_SetsDefaultForecast()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var team = CreateTeam(1, [1]);

            var feature = SetupFeature(team, 0);
            SetupFeatures(feature);
            var project = CreatePortfolio(feature);

            await subject.UpdateForecastsForPortfolio(project, DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.GetProbability(50), Is.Zero);
                Assert.That(feature.Forecast.GetProbability(70), Is.Zero);
                Assert.That(feature.Forecast.GetProbability(85), Is.Zero);
                Assert.That(feature.Forecast.GetProbability(95), Is.Zero);
            }
        }

        [Test]
        public async Task When_TeamHasInsufficientData_FlagsForecastAsInsufficient()
        {
            var team = CreateTeam(1, [1, 0, 1, 0, 1, 0, 1]);
            var thinThroughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 0, 1, 0, 1, 0, 1]));
            teamMetricsServiceMock
                .Setup(x => x.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(thinThroughput, false, null, HasSufficientData: false));

            var subject = CreateSubjectWithRealThroughput();

            var forecast = await subject.When(team, 5);

            Assert.That(forecast.HasSufficientData, Is.False);
        }

        private void AssertComputedAndSavedWithoutRecording(ForecastRunOutcome outcome, Portfolio portfolio, Feature feature, string? recorded)
        {
            Assert.That(outcome, Is.TypeOf<ForecastRunOutcome.Computed>());
            var computed = (ForecastRunOutcome.Computed)outcome;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(computed.ReadFingerprint, Is.EqualTo(recorded));
                Assert.That(computed.FreshFingerprint, Is.Not.Null.And.Not.EqualTo(recorded));
                Assert.That(feature.Forecast.GetProbability(50), Is.EqualTo(35));
                Assert.That(portfolio.ForecastInputFingerprint, Is.EqualTo(recorded));
                featureRepositoryMock.Verify(x => x.Save(), Times.Once);
            }
        }

        [Test]
        public void HowMany_SimulatesExactlyTheConfiguredNumberOfRuns()
        {
            var subject = CreateSubjectWithPersistentThroughput();

            var forecast = subject.HowMany(new RunChartData(RunChartDataGenerator.GenerateRunChartData([1])), 7);

            Assert.That(forecast.TotalTrials, Is.EqualTo(ForecastSimulationLimits.Default.Trials));
        }

        [Test]
        public async Task UpdateForecastsForPortfolio_ForecastsAFeatureSharedWithATeamOutsideThePortfolio_AndSavesIt()
        {
            var subject = CreateSubjectWithPersistentThroughput();
            var portfolioTeam = CreateTeam(1, [1]);
            var otherTeam = CreateTeam(1, [1]);
            var ownFeature = SetupFeature(portfolioTeam, 5);
            var sharedFeature = SetupFeature([(portfolioTeam, 3, 3), (otherTeam, 4, 4)]);
            SetupFeatures(ownFeature, sharedFeature);

            await subject.UpdateForecastsForPortfolio(CreatePortfolio(ownFeature), DateOnly.MinValue);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sharedFeature.Forecasts, Is.Not.Empty);
                featureRepositoryMock.Verify(x => x.Save(), Times.Once);
            }
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
            featureRepositoryMock.Holds(features);
        }

        private ForecastService CreateSubjectWithPersistentThroughput() => CreateSubjectWithPersistentThroughput(TimeProvider.System);

        private ForecastService CreateSubjectWithPersistentThroughput(TimeProvider timeProvider)
        {
            return new ForecastService(new NotSoRandomNumberService(), loggerMock.Object, teamMetricsServiceMock.Object, featureRepositoryMock.Object, new NothingWaitsForAnything(), new ForecastSimulation(new DrawsTheSameNumberEveryTime(), ForecastSimulationLimits.Default), timeProvider);
        }

        private ForecastService CreateSubjectWithRealThroughput()
        {
            return new ForecastService(new RandomNumberService(), Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object, new NothingWaitsForAnything(), new ForecastSimulation(new DrawsAfreshEveryTime(), ForecastSimulationLimits.Default), TimeProvider.System);
        }

        /// <summary>
        /// Both draw sources are pinned, because either one left loose puts the dice back.
        /// Deliberately not the factory above: most of its callers forecast several Teams at once, and
        /// one seeded sequence handed to concurrent draws is not reproducible either — see
        /// <see cref="SeededRandomNumberService"/>. Only the two single-stream real-data tests use this.
        /// </summary>
        private ForecastService CreateSubjectWithPinnedDraws()
        {
            return new ForecastService(
                new SeededRandomNumberService(TheSeedTheRealDataForecastsShare),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetricsServiceMock.Object,
                featureRepositoryMock.Object,
                new NothingWaitsForAnything(),
                new ForecastSimulation(new DrawsFromAPinnedStartingNumber(TheSeedTheRealDataForecastsShare), ForecastSimulationLimits.Default),
                TimeProvider.System);
        }

        private Team CreateTeam(int featureWip, int[] throughput)
        {
            var team = new Team
            {
                Name = "Team",
                FeatureWIP = featureWip,
                Id = idCounter++,
            };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock.Setup(x => x.GetCurrentThroughputForTeamForecast(team, ThroughputFilterMode.RespectTeamSetting)).Returns(runChart);
            teamMetricsServiceMock.Setup(x => x.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting)).Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private Portfolio CreatePortfolio(params Feature[] features)
        {
            var portfolio = CreatePortfolio(DateTime.UtcNow, features);

            return portfolio;
        }

        private Portfolio CreatePortfolio(DateTime lastUpdatedTime, params Feature[] features)
        {
            var portfolio = new Portfolio
            {
                Name = "Project",
                Id = idCounter++,
                UpdateTime = lastUpdatedTime,
            };
            
            portfolio.UpdateFeatures(features);
            
            return portfolio;
        }
    }
}
