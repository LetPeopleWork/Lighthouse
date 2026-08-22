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

            return new WhenForecast(simulationResult) { TeamId = team.Id, HasSufficientData = hasSufficientData };
        }
    }
}
