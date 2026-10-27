using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class DeliveryMetricValuesProjectorTest
    {
        private const int PortfolioId = 4;

        private const int ForecastDays = 12;

        private static readonly DateTimeOffset FixedInstant = new(2026, 3, 17, 9, 30, 0, TimeSpan.Zero);

        private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

        private static readonly string[] TeamNames = ["Team Alpha"];

        private FakeLighthouseClock clock = null!;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock = null!;
        private DeliveryMetricValuesProjector subject = null!;

        [SetUp]
        public void Init()
        {
            clock = new FakeLighthouseClock(FixedInstant, TimeZoneInfo.Utc);

            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);

            subject = new DeliveryMetricValuesProjector(blackoutPeriodServiceMock.Object);
        }

        /// <summary>
        /// The reason this component exists. A Delivery closed today must be pinned with the same
        /// numbers the daily recorder would have written for it today, or the record it keeps and the
        /// history it sits on top of tell two different stories.
        /// </summary>
        [Test]
        public async Task Project_AtArchiveTime_AgreesWithEveryValueTheDailyRecorderWrites()
        {
            var delivery = ForecastableDelivery();

            var recorded = await WhatTheDailyRecorderWrites(delivery);
            var projected = subject.Project(delivery, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projected.TargetDate, Is.EqualTo(recorded.TargetDateAtSnapshot));
                Assert.That(projected.TotalWork, Is.EqualTo(recorded.TotalWork));
                Assert.That(projected.DoneWork, Is.EqualTo(recorded.DoneWork));
                Assert.That(projected.RemainingWork, Is.EqualTo(recorded.RemainingWork));
                Assert.That(projected.EstimatedItemCount, Is.EqualTo(recorded.EstimatedItemCount));
                Assert.That(projected.LikelihoodPercentage, Is.EqualTo(recorded.LikelihoodPercentage));
                Assert.That(projected.WhenDistributionJson, Is.EqualTo(recorded.WhenDistributionJson));
                Assert.That(projected.FeatureBreakdownJson, Is.EqualTo(recorded.FeatureBreakdownJson));
            }
        }

        /// <summary>
        /// Guards the equality above against passing because both sides are empty.
        /// </summary>
        [Test]
        public async Task Project_AtArchiveTime_AgreesOnNumbersThatAreActuallyThere()
        {
            var delivery = ForecastableDelivery();

            var recorded = await WhatTheDailyRecorderWrites(delivery);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.TotalWork, Is.EqualTo(10));
                Assert.That(recorded.DoneWork, Is.EqualTo(6));
                Assert.That(recorded.LikelihoodPercentage, Is.Not.Null);
                Assert.That(recorded.WhenDistributionJson, Is.Not.Null);
                Assert.That(recorded.FeatureBreakdownJson, Is.Not.Null);
            }
        }

        [Test]
        public void Project_DeliveryTheRecorderHasNeverRunFor_StillProducesTheWholeRecord()
        {
            var neverRecorded = ForecastableDelivery();

            var projected = subject.Project(neverRecorded, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projected.TotalWork, Is.EqualTo(10));
                Assert.That(projected.RemainingWork, Is.EqualTo(4));
                Assert.That(projected.LikelihoodPercentage, Is.Not.Null);
                Assert.That(projected.WhenDistributionJson, Is.Not.Null);
                Assert.That(projected.FeatureBreakdownJson, Is.Not.Null);
            }
        }

        [Test]
        public void Project_DeliveryRestingOnTooLittleHistory_SaysSo()
        {
            var delivery = ForecastableDelivery(hasSufficientData: false);

            var projected = subject.Project(delivery, clock.Today);

            Assert.That(projected.HasSufficientData, Is.False);
        }

        [Test]
        public void Project_DeliveryWithAForecastableTeam_NamesNobodyAsMissingOne()
        {
            var delivery = ForecastableDelivery();

            var projected = subject.Project(delivery, clock.Today);

            Assert.That(projected.TeamsWithoutForecastJson, Is.Null);
        }

        [Test]
        public void Project_DeliveryWaitingOnATeamWithNoThroughput_NamesThatTeam()
        {
            var delivery = DeliveryWithoutThroughput();

            var projected = subject.Project(delivery, clock.Today);

            var teams = JsonSerializer.Deserialize<List<string>>(projected.TeamsWithoutForecastJson!, ReadOptions);
            Assert.That(teams, Is.EquivalentTo(TeamNames));
        }

        [Test]
        public void Project_RuleBasedDelivery_CarriesTheRuleItWasMatchedBy()
        {
            var delivery = ForecastableDelivery();
            delivery.SelectFeaturesByRule("{\"mode\":\"and\"}", 1);

            var projected = subject.Project(delivery, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projected.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
                Assert.That(projected.RuleDefinitionJson, Is.EqualTo("{\"mode\":\"and\"}"));
                Assert.That(projected.RuleSchemaVersion, Is.EqualTo(1));
            }
        }

        [Test]
        public void Project_DeliveryNobodyCouldForecast_RecordsNoLikelihoodAndNoDistribution()
        {
            var delivery = DeliveryWithoutThroughput();

            var projected = subject.Project(delivery, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projected.LikelihoodPercentage, Is.Null);
                Assert.That(projected.WhenDistributionJson, Is.Null);
            }
        }

        [Test]
        public void Project_DeliveryWhoseFeaturesAreAllSized_RecordsNoEstimatedPortion()
        {
            var projected = subject.Project(ForecastableDelivery(), clock.Today);

            Assert.That(projected.EstimatedItemCount, Is.Null);
        }

        [Test]
        public void Project_DeliveryWithAFeatureOnTheDefaultSize_RecordsHowMuchOfTheWorkIsEstimated()
        {
            var team = new Team { Id = 7, Name = TeamNames[0] };
            var sized = new Feature([(team, 4, 10)]) { Id = 11, ReferenceId = "FTR-11", Name = "Checkout", Order = "1" };
            var estimated = new Feature([(team, 3, 5)]) { Id = 12, ReferenceId = "FTR-12", Name = "Refunds", Order = "2", IsUsingDefaultFeatureSize = true };
            var delivery = new Delivery { Id = 3, Name = "Q1 Release", PortfolioId = PortfolioId, Date = clock.TodayAsUtcMidnight.AddDays(ForecastDays * 2) };
            delivery.ReplaceFeatures([sized, estimated]);

            var projected = subject.Project(delivery, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projected.TotalWork, Is.EqualTo(15));
                Assert.That(projected.RemainingWork, Is.EqualTo(7));
                Assert.That(projected.EstimatedItemCount, Is.EqualTo(5));
            }
        }

        [Test]
        public void BlackoutPeriodsFor_ForecastRunningPastTheTargetDate_ReadsBlackoutDaysAsFarAsTheFeaturesForecastReaches()
        {
            var team = new Team { Id = 7, Name = TeamNames[0] };
            var feature = new Feature([(team, 4, 10)]) { Id = 11, ReferenceId = "FTR-11", Name = "Checkout", Order = "1" };
            var simulationResult = new SimulationResult();
            simulationResult.SimulationResults[40] = 100;
            feature.SetFeatureForecasts([new WhenForecast(simulationResult, DateTime.UtcNow) { TeamId = team.Id, HasSufficientData = true }]);
            var delivery = new Delivery { Id = 3, Name = "Q1 Release", PortfolioId = PortfolioId, Date = clock.TodayAsUtcMidnight.AddDays(1) };
            delivery.ReplaceFeatures([feature]);

            subject.BlackoutPeriodsFor([delivery], clock.Today);

            // Day 40, doubled for weekends and the like, plus two weeks: 94 days after 17 March.
            var forecastReach = new DateTime(2026, 6, 19, 0, 0, 0, DateTimeKind.Utc);
            blackoutPeriodServiceMock.Verify(
                service => service.GetEffectiveBlackoutDays(clock.TodayAsUtcMidnight, It.Is<DateTime>(end => end >= forecastReach)),
                Times.Once);
        }

        [Test]
        public void BlackoutPeriodsFor_TwoTeamsWhoseTailsPushTheDeliveryPastEveryFeature_ReadsBlackoutDaysAsFarAsTheDeliveryReaches()
        {
            var first = FeatureOfATeamWithALongTail(teamId: 7, referenceId: "FTR-11");
            var second = FeatureOfATeamWithALongTail(teamId: 8, referenceId: "FTR-12");
            var delivery = new Delivery { Id = 3, Name = "Q1 Release", PortfolioId = PortfolioId, Date = clock.TodayAsUtcMidnight.AddDays(1) };
            delivery.ReplaceFeatures([first, second]);

            subject.BlackoutPeriodsFor([delivery], clock.Today);

            // Each Feature is done by day 20 in 95% of runs, but both Teams must finish: the Delivery's
            // 95th is day 100, which reads 214 days after 17 March (each Feature alone would read 54).
            var deliveryReach = new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc);
            blackoutPeriodServiceMock.Verify(
                service => service.GetEffectiveBlackoutDays(clock.TodayAsUtcMidnight, It.Is<DateTime>(end => end >= deliveryReach)),
                Times.Once);
        }

        [Test]
        public void BlackoutPeriodsFor_TwoTeamsDeliveryAlongsideOneWithoutAForecast_ReadsAsFarAsTheFurthestDelivery()
        {
            var longTailed = new Delivery { Id = 3, Name = "Q1 Release", PortfolioId = PortfolioId, Date = clock.TodayAsUtcMidnight.AddDays(1) };
            longTailed.ReplaceFeatures([FeatureOfATeamWithALongTail(teamId: 7, referenceId: "FTR-11"), FeatureOfATeamWithALongTail(teamId: 8, referenceId: "FTR-12")]);
            var unforecast = UnforecastDeliveryDue(daysOut: 1, id: 4);

            subject.BlackoutPeriodsFor([unforecast, longTailed], clock.Today);

            var deliveryReach = new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc);
            blackoutPeriodServiceMock.Verify(
                service => service.GetEffectiveBlackoutDays(clock.TodayAsUtcMidnight, deliveryReach),
                Times.Once);
        }

        [Test]
        public void BlackoutPeriodsFor_DeliveriesWithoutForecasts_ReadsTwoWeeksPastTheLatestTargetDate()
        {
            var sooner = UnforecastDeliveryDue(daysOut: 30, id: 3);
            var later = UnforecastDeliveryDue(daysOut: 60, id: 4);

            subject.BlackoutPeriodsFor([later, sooner], clock.Today);

            // The later target date, 60 days after 17 March, plus two weeks.
            var targetReach = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
            blackoutPeriodServiceMock.Verify(
                service => service.GetEffectiveBlackoutDays(clock.TodayAsUtcMidnight, targetReach),
                Times.Once);
        }

        [Test]
        public void BlackoutPeriodsFor_NoDeliveries_ReadsTwoWeeksFromToday()
        {
            subject.BlackoutPeriodsFor([], clock.Today);

            var twoWeeksOut = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
            blackoutPeriodServiceMock.Verify(
                service => service.GetEffectiveBlackoutDays(clock.TodayAsUtcMidnight, twoWeeksOut),
                Times.Once);
        }

        private Delivery UnforecastDeliveryDue(int daysOut, int id)
        {
            var team = new Team { Id = 9, Name = TeamNames[0] };
            var feature = new Feature([(team, 4, 10)]) { Id = 20 + id, ReferenceId = $"FTR-{20 + id}", Name = "Unforecast", Order = "9" };
            var delivery = new Delivery { Id = id, Name = $"Release {id}", PortfolioId = PortfolioId, Date = clock.TodayAsUtcMidnight.AddDays(daysOut) };
            delivery.ReplaceFeatures([feature]);

            return delivery;
        }

        private static Feature FeatureOfATeamWithALongTail(int teamId, string referenceId)
        {
            var team = new Team { Id = teamId, Name = $"Team {teamId}" };
            var feature = new Feature([(team, 4, 10)]) { Id = teamId, ReferenceId = referenceId, Name = referenceId, Order = teamId.ToString() };
            var simulationResult = new SimulationResult();
            simulationResult.SimulationResults[20] = 95;
            simulationResult.SimulationResults[100] = 5;
            feature.SetFeatureForecasts([new WhenForecast(simulationResult, DateTime.UtcNow) { TeamId = team.Id, HasSufficientData = true }]);

            return feature;
        }

        private async Task<DeliveryMetricSnapshot> WhatTheDailyRecorderWrites(Delivery delivery)
        {
            var snapshot = new DeliveryMetricSnapshot { DeliveryId = delivery.Id, RecordedDay = clock.Today };

            var deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            deliveryRepositoryMock.Setup(repository => repository.GetRecordableByPortfolio(PortfolioId)).Returns(new RecordableDeliveries([delivery]));

            var snapshotRepositoryMock = new Mock<IDeliveryMetricSnapshotRepository>();
            snapshotRepositoryMock.Setup(repository => repository.GetOrCreateForDay(delivery.Id, clock.Today)).Returns(snapshot);

            var recorder = new DeliveryMetricSnapshotRecordingHandler(
                deliveryRepositoryMock.Object,
                snapshotRepositoryMock.Object,
                subject,
                clock,
                Mock.Of<ILogger<DeliveryMetricSnapshotRecordingHandler>>());

            await recorder.HandleAsync(new PortfolioForecastsUpdated(PortfolioId), CancellationToken.None);

            return snapshot;
        }

        private Delivery ForecastableDelivery(bool hasSufficientData = true)
        {
            var team = new Team { Id = 7, Name = TeamNames[0] };
            var feature = new Feature([(team, 4, 10)]) { Id = 11, ReferenceId = "FTR-11", Name = "Checkout", Order = "1" };
            feature.SetFeatureForecasts([ForecastOf(team, hasSufficientData)]);

            return DeliveryWith(feature);
        }

        private Delivery DeliveryWithoutThroughput()
        {
            var team = new Team { Id = 7, Name = TeamNames[0] };
            var feature = new Feature([(team, 4, 10)]) { Id = 11, ReferenceId = "FTR-11", Name = "Checkout", Order = "1" };

            return DeliveryWith(feature);
        }

        private Delivery DeliveryWith(Feature feature)
        {
            var delivery = new Delivery
            {
                Id = 3,
                Name = "Q1 Release",
                PortfolioId = PortfolioId,
                Date = clock.TodayAsUtcMidnight.AddDays(ForecastDays * 2),
            };

            delivery.ReplaceFeatures([feature]);

            return delivery;
        }

        private static WhenForecast ForecastOf(Team team, bool hasSufficientData)
        {
            var simulationResult = new SimulationResult();
            simulationResult.SimulationResults[ForecastDays] = 100;

            return new WhenForecast(simulationResult, DateTime.UtcNow) { TeamId = team.Id, HasSufficientData = hasSufficientData };
        }
    }
}
