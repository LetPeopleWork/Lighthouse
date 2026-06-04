using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [NonParallelizable]
    public class DeliveryMetricSnapshotRecordingHandlerTest
    {
        private static readonly int[] ExpectedWhenPercentiles = [50, 70, 85, 95];

        private const int KnownForecastDays = 30;

        private const int SingleBucketTrials = 100;

        private const double CertainSingleBucketLikelihood = 100.0;

        private TestWebApplicationFactory<Program> factory = null!;
        private IServiceScope scope = null!;

        [SetUp]
        public void Init()
        {
            factory = new TestWebApplicationFactory<Program>();
            scope = factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            scope.Dispose();
            factory.Dispose();
        }

        [Test]
        public async Task HandleAsync_DeliveryWithKnownCounts_RecordsTodaysBacklogDoneAndRemaining()
        {
            var fixture = await SeedDeliveryWithKnownCounts();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.TotalWork, Is.EqualTo(fixture.ExpectedTotalWork));
                Assert.That(snapshot.DoneWork, Is.EqualTo(fixture.ExpectedDoneWork));
                Assert.That(snapshot.RemainingWork, Is.EqualTo(fixture.ExpectedTotalWork - fixture.ExpectedDoneWork));
            }
        }

        [Test]
        public async Task HandleAsync_RunTwiceSameDay_IsIdempotentOnDeliveryAndDate()
        {
            var fixture = await SeedDeliveryWithKnownCounts();

            await HandlePortfolioForecastsUpdated(fixture);
            await HandlePortfolioForecastsUpdated(fixture);

            Assert.That(await TodaysSnapshotRowCount(fixture), Is.EqualTo(1));
        }

        [Test]
        public async Task HandleAsync_DeliverySpanningMultipleFeatures_RecordsSummedWorkAcrossThemNotASingleFeature()
        {
            var fixture = await SeedDeliveryWithTwoFeatures();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.TotalWork, Is.EqualTo(fixture.ExpectedTotalWork));
                Assert.That(snapshot.RemainingWork, Is.EqualTo(fixture.ExpectedTotalWork - fixture.ExpectedDoneWork));
                Assert.That(snapshot.DoneWork, Is.EqualTo(fixture.ExpectedDoneWork));
            }
        }

        [Test]
        public async Task HandleAsync_WhenSnapshotPersistenceFails_SwallowsAndLogsTheError()
        {
            var fixture = await SeedDeliveryWithKnownCounts();
            var persistenceFailure = new InvalidOperationException("snapshot store unavailable");
            var snapshotRepository = new Mock<IDeliveryMetricSnapshotRepository>();
            snapshotRepository
                .Setup(repository => repository.GetOrCreateForDay(It.IsAny<int>(), It.IsAny<DateTime>()))
                .Returns(new DeliveryMetricSnapshot { DeliveryId = fixture.DeliveryId });
            snapshotRepository
                .Setup(repository => repository.Save())
                .ThrowsAsync(persistenceFailure);

            var logger = new Mock<ILogger<DeliveryMetricSnapshotRecordingHandler>>();
            var handler = new DeliveryMetricSnapshotRecordingHandler(
                scope.ServiceProvider.GetRequiredService<IDeliveryRepository>(),
                snapshotRepository.Object,
                logger.Object);

            await handler.HandleAsync(new PortfolioForecastsUpdated(fixture.PortfolioId), CancellationToken.None);

            logger.Verify(
                log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    persistenceFailure,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task HandleAsync_ItemReopenedSinceYesterday_LowersTodaysRecordedDone()
        {
            var fixture = await SeedDeliveryRecordedYesterdayThenReopenedAnItem();

            await HandlePortfolioForecastsUpdated(fixture);

            var (yesterday, today) = await YesterdayAndTodaySnapshots(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(today.DoneWork, Is.LessThan(yesterday.DoneWork));
                Assert.That(today.TotalWork, Is.EqualTo(yesterday.TotalWork));
            }
        }

        [Test]
        public async Task HandleAsync_NotBrokenDownFeature_RecordsExtrapolatedItemsAsEstimatedPortion()
        {
            var fixture = await SeedDeliveryWithNotBrokenDownFeature();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.EstimatedItemCount, Is.EqualTo(fixture.ExpectedEstimatedItemCount));
                Assert.That(snapshot.EstimatedItemCount, Is.LessThan(snapshot.TotalWork));
            }
        }

        [Test]
        public async Task HandleAsync_FullyBrokenDownDelivery_RecordsNoEstimatedPortion()
        {
            var fixture = await SeedDeliveryWithKnownCounts();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            Assert.That(snapshot.EstimatedItemCount, Is.Null);
        }

        [Test]
        public async Task HandleAsync_NotBrokenDownFeatureRunTwiceSameDay_KeepsSingleRowWithSameEstimatedPortion()
        {
            var fixture = await SeedDeliveryWithNotBrokenDownFeature();

            await HandlePortfolioForecastsUpdated(fixture);
            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TodaysSnapshotRowCount(fixture), Is.EqualTo(1));
                Assert.That(snapshot.EstimatedItemCount, Is.EqualTo(fixture.ExpectedEstimatedItemCount));
            }
        }

        [Test]
        public async Task HandleAsync_DeliveryWithForecastedFeature_RecordsLikelihoodAndWhenDistributionForToday()
        {
            var fixture = await SeedDeliveryWithForecastedFeature();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            var whenPoints = DeserializeWhenDistribution(snapshot.WhenDistributionJson);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.LikelihoodPercentage, Is.EqualTo(fixture.ExpectedLikelihoodPercentage));
                Assert.That(whenPoints.Select(point => (int)point.Probability), Is.EquivalentTo(ExpectedWhenPercentiles));
                Assert.That(whenPoints.Select(point => point.ExpectedDate), Has.All.EqualTo(fixture.ExpectedWhenDate));
            }
        }

        [Test]
        public async Task HandleAsync_DeliveryWithoutUsableForecast_RecordsNullLikelihoodAndNullWhenDistribution()
        {
            var fixture = await SeedDeliveryWithoutUsableForecast();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.LikelihoodPercentage, Is.Null);
                Assert.That(snapshot.WhenDistributionJson, Is.Null);
            }
        }

        [Test]
        public async Task HandleAsync_ForecastedDeliveryRunTwiceSameDay_OverwritesLikelihoodInPlace()
        {
            var fixture = await SeedDeliveryWithForecastedFeature();

            await HandlePortfolioForecastsUpdated(fixture);
            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TodaysSnapshotRowCount(fixture), Is.EqualTo(1));
                Assert.That(snapshot.LikelihoodPercentage, Is.EqualTo(fixture.ExpectedLikelihoodPercentage));
            }
        }

        [Test]
        public async Task HandleAsync_DeliveryWithMultipleFeatures_RecordsPerFeatureCompletionAndLikelihoodBreakdown()
        {
            var fixture = await SeedDeliveryWithMixedFeatureBreakdown();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            var breakdown = DeserializeFeatureBreakdown(snapshot.FeatureBreakdownJson);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(breakdown.Select(entry => entry.ReferenceId), Is.EquivalentTo(fixture.ExpectedBreakdown.Select(entry => entry.ReferenceId)));
                Assert.That(breakdown, Is.EquivalentTo(fixture.ExpectedBreakdown));
            }
        }

        [Test]
        public async Task HandleAsync_FeatureWithoutAnyItems_IsExcludedFromTheBreakdown()
        {
            var fixture = await SeedDeliveryWithMixedFeatureBreakdown();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            var breakdown = DeserializeFeatureBreakdown(snapshot.FeatureBreakdownJson);
            Assert.That(breakdown.Select(entry => entry.ReferenceId), Has.None.EqualTo(fixture.EmptyFeatureReferenceId));
        }

        [Test]
        public async Task HandleAsync_DeliveryWithNoPlottableFeatures_RecordsNullFeatureBreakdown()
        {
            var fixture = await SeedDeliveryWithNoPlottableFeatures();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            Assert.That(snapshot.FeatureBreakdownJson, Is.Null);
        }

        [Test]
        public async Task HandleAsync_BreakdownDeliveryRunTwiceSameDay_OverwritesBreakdownInPlace()
        {
            var fixture = await SeedDeliveryWithMixedFeatureBreakdown();

            await HandlePortfolioForecastsUpdated(fixture);
            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            var breakdown = DeserializeFeatureBreakdown(snapshot.FeatureBreakdownJson);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TodaysSnapshotRowCount(fixture), Is.EqualTo(1));
                Assert.That(breakdown, Is.EquivalentTo(fixture.ExpectedBreakdown));
            }
        }

        [Test]
        [Ignore("Slice 3 — US-03 forecast freshness (ForecastHowMany)")]
        public async Task HandleAsync_AfterForecastUpdate_RecordsFreshPostForecastFiguresNotStaleOnes()
        {
            var fixture = await SeedDeliveryWhoseForecastChangesOnUpdate();

            await HandlePortfolioForecastsUpdated(fixture);

            var snapshot = await TodaysSnapshot(fixture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.ForecastHowMany, Is.EqualTo(fixture.FreshForecastHowMany));
                Assert.That(snapshot.ForecastHowMany, Is.Not.EqualTo(fixture.StalePreForecastHowMany));
            }
        }

        private async Task<RecorderFixture> SeedDeliveryWithKnownCounts()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();
            var delivery = await SeedDeliveryWithWork(portfolio, team, remainingWork: 6, totalWork: 10);

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                ExpectedTotalWork = 10,
                ExpectedDoneWork = 4,
            };
        }

        private async Task<RecorderFixture> SeedDeliveryWithTwoFeatures()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();
            var delivery = await SeedDeliveryWithFeatures(
                portfolio,
                team,
                (remainingWork: 6, totalWork: 10),
                (remainingWork: 3, totalWork: 5));

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                ExpectedTotalWork = 15,
                ExpectedDoneWork = 6,
            };
        }

        private async Task<RecorderFixture> SeedDeliveryRecordedYesterdayThenReopenedAnItem()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();
            var delivery = await SeedDeliveryWithWork(portfolio, team, remainingWork: 6, totalWork: 10);

            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = DateTime.UtcNow.Date.AddDays(-1),
                TotalWork = 10,
                DoneWork = 6,
                RemainingWork = 4,
            });
            await dbContext.SaveChangesAsync();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                ExpectedTotalWork = 10,
                ExpectedDoneWork = 4,
            };
        }

        private Task<RecorderFixture> SeedDeliveryWhoseForecastChangesOnUpdate()
            => throw new AssertionException("pending — DELIVER seeds a delivery whose forecast changes when the update runs");

        private async Task<RecorderFixture> SeedDeliveryWithNotBrokenDownFeature()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();

            var brokenDownFeature = new Feature([(team, 4, 12)])
            {
                Name = "Broken Down Feature",
                Order = "1",
                IsUsingDefaultFeatureSize = false,
            };

            var extrapolatedFeature = new Feature([(team, 8, 8)])
            {
                Name = "Not Broken Down Feature",
                Order = "2",
                IsUsingDefaultFeatureSize = true,
            };

            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(brokenDownFeature);
            featureRepository.Add(extrapolatedFeature);
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);
            delivery.Features.Add(brokenDownFeature);
            delivery.Features.Add(extrapolatedFeature);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                ExpectedTotalWork = 20,
                ExpectedDoneWork = 8,
                ExpectedEstimatedItemCount = 8,
            };
        }

        private async Task<RecorderFixture> SeedDeliveryWithForecastedFeature()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();

            var feature = new Feature([(team, 12, 12)])
            {
                Name = "Forecasted Feature",
                Order = "1",
            };

            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            feature.SetFeatureForecasts([SingleOutcomeForecast(KnownForecastDays)]);
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(KnownForecastDays), portfolio.Id);
            delivery.Features.Add(feature);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                ExpectedTotalWork = 12,
                ExpectedDoneWork = 0,
                ExpectedLikelihoodPercentage = CertainSingleBucketLikelihood,
                ExpectedWhenDate = DateTime.UtcNow.Date.AddDays(KnownForecastDays),
            };
        }

        private async Task<RecorderFixture> SeedDeliveryWithMixedFeatureBreakdown()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();

            var inProgressFeature = new Feature([(team, 6, 10)])
            {
                Name = "In Progress Feature",
                ReferenceId = "FEAT-1",
                Order = "1",
            };

            var completedFeature = new Feature([(team, 0, 5)])
            {
                Name = "Completed Feature",
                ReferenceId = "FEAT-2",
                Order = "2",
            };

            var emptyFeature = new Feature([(team, 0, 0)])
            {
                Name = "Empty Feature",
                ReferenceId = "FEAT-3",
                Order = "3",
            };

            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(inProgressFeature);
            featureRepository.Add(completedFeature);
            featureRepository.Add(emptyFeature);
            await featureRepository.Save();

            inProgressFeature.SetFeatureForecasts([SingleOutcomeForecast(KnownForecastDays)]);
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(KnownForecastDays), portfolio.Id);
            delivery.Features.Add(inProgressFeature);
            delivery.Features.Add(completedFeature);
            delivery.Features.Add(emptyFeature);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
                EmptyFeatureReferenceId = "FEAT-3",
                ExpectedBreakdown =
                [
                    new DeliveryFeatureMetric("FEAT-1", "In Progress Feature", 40.0, CertainSingleBucketLikelihood),
                    new DeliveryFeatureMetric("FEAT-2", "Completed Feature", 100.0, CertainSingleBucketLikelihood),
                ],
            };
        }

        private async Task<RecorderFixture> SeedDeliveryWithNoPlottableFeatures()
        {
            var (portfolio, team) = await SeedPortfolioWithTeam();

            var emptyFeature = new Feature([(team, 0, 0)])
            {
                Name = "Empty Feature",
                ReferenceId = "FEAT-EMPTY",
                Order = "1",
            };

            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(emptyFeature);
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(KnownForecastDays), portfolio.Id);
            delivery.Features.Add(emptyFeature);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
            };
        }

        private async Task<RecorderFixture> SeedDeliveryWithoutUsableForecast()
        {
            var (portfolio, _) = await SeedPortfolioWithTeam();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(KnownForecastDays), portfolio.Id);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new RecorderFixture
            {
                PortfolioId = portfolio.Id,
                DeliveryId = delivery.Id,
            };
        }

        private static WhenForecast SingleOutcomeForecast(int days)
        {
            var simulationResult = new SimulationResult();
            simulationResult.SimulationResults[days] = SingleBucketTrials;

            return new WhenForecast(simulationResult);
        }

        private static readonly JsonSerializerOptions WhenDistributionReadOptions = new() { PropertyNameCaseInsensitive = true };

        private static List<WhenDistributionPointDto> DeserializeWhenDistribution(string? whenDistributionJson)
        {
            Assert.That(whenDistributionJson, Is.Not.Null);
            return JsonSerializer.Deserialize<List<WhenDistributionPointDto>>(
                whenDistributionJson!,
                WhenDistributionReadOptions)!;
        }

        private static List<DeliveryFeatureMetric> DeserializeFeatureBreakdown(string? featureBreakdownJson)
        {
            Assert.That(featureBreakdownJson, Is.Not.Null);
            return JsonSerializer.Deserialize<List<DeliveryFeatureMetric>>(
                featureBreakdownJson!,
                WhenDistributionReadOptions)!;
        }

        private async Task HandlePortfolioForecastsUpdated(RecorderFixture fixture)
        {
            var handler = scope.ServiceProvider.GetRequiredService<IDomainEventHandler<PortfolioForecastsUpdated>>();
            await handler.HandleAsync(new PortfolioForecastsUpdated(fixture.PortfolioId), CancellationToken.None);
        }

        private async Task<SnapshotView> TodaysSnapshot(RecorderFixture fixture)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var today = DateTime.UtcNow.Date;
            var snapshot = await dbContext.DeliveryMetricSnapshots
                .SingleAsync(s => s.DeliveryId == fixture.DeliveryId && s.RecordedAt >= today && s.RecordedAt < today.AddDays(1));

            return ToView(snapshot);
        }

        private async Task<int> TodaysSnapshotRowCount(RecorderFixture fixture)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var today = DateTime.UtcNow.Date;
            return await dbContext.DeliveryMetricSnapshots
                .CountAsync(s => s.DeliveryId == fixture.DeliveryId && s.RecordedAt >= today && s.RecordedAt < today.AddDays(1));
        }

        private async Task<(SnapshotView Yesterday, SnapshotView Today)> YesterdayAndTodaySnapshots(RecorderFixture fixture)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var today = DateTime.UtcNow.Date;

            var yesterday = await dbContext.DeliveryMetricSnapshots
                .SingleAsync(s => s.DeliveryId == fixture.DeliveryId && s.RecordedAt >= today.AddDays(-1) && s.RecordedAt < today);
            var todaySnapshot = await dbContext.DeliveryMetricSnapshots
                .SingleAsync(s => s.DeliveryId == fixture.DeliveryId && s.RecordedAt >= today && s.RecordedAt < today.AddDays(1));

            return (ToView(yesterday), ToView(todaySnapshot));
        }

        private static SnapshotView ToView(DeliveryMetricSnapshot snapshot)
            => new()
            {
                TotalWork = snapshot.TotalWork,
                DoneWork = snapshot.DoneWork,
                RemainingWork = snapshot.RemainingWork,
                EstimatedItemCount = snapshot.EstimatedItemCount,
                ForecastHowMany = snapshot.ForecastHowMany,
                LikelihoodPercentage = snapshot.LikelihoodPercentage,
                WhenDistributionJson = snapshot.WhenDistributionJson,
                FeatureBreakdownJson = snapshot.FeatureBreakdownJson,
            };

        private async Task<(Portfolio Portfolio, Team Team)> SeedPortfolioWithTeam()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            var team = new Team
            {
                Name = "Test Team",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio
            {
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };

            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return (portfolio, team);
        }

        private async Task<Delivery> SeedDeliveryWithWork(Portfolio portfolio, Team team, int remainingWork, int totalWork)
        {
            var feature = new Feature([(team, remainingWork, totalWork)])
            {
                Name = "Feature",
                Order = "1",
            };

            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);
            delivery.Features.Add(feature);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return delivery;
        }

        private async Task<Delivery> SeedDeliveryWithFeatures(Portfolio portfolio, Team team, params (int remainingWork, int totalWork)[] featureWork)
        {
            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var features = featureWork
                .Select((work, index) => new Feature([(team, work.remainingWork, work.totalWork)])
                {
                    Name = $"Feature {index}",
                    Order = index.ToString(),
                })
                .ToList();

            foreach (var feature in features)
            {
                featureRepository.Add(feature);
            }
            await featureRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);
            delivery.Features.AddRange(features);

            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return delivery;
        }

        private sealed record RecorderFixture
        {
            public int PortfolioId { get; init; }
            public int DeliveryId { get; init; }
            public int ExpectedTotalWork { get; init; }
            public int ExpectedDoneWork { get; init; }
            public int? ExpectedEstimatedItemCount { get; init; }
            public double? ExpectedLikelihoodPercentage { get; init; }
            public DateTime ExpectedWhenDate { get; init; }
            public int FreshForecastHowMany { get; init; }
            public int StalePreForecastHowMany { get; init; }
            public string EmptyFeatureReferenceId { get; init; } = string.Empty;
            public IReadOnlyList<DeliveryFeatureMetric> ExpectedBreakdown { get; init; } = [];
        }

        private sealed record SnapshotView
        {
            public int TotalWork { get; init; }
            public int DoneWork { get; init; }
            public int RemainingWork { get; init; }
            public int? EstimatedItemCount { get; init; }
            public int? ForecastHowMany { get; init; }
            public double? LikelihoodPercentage { get; init; }
            public string? WhenDistributionJson { get; init; }
            public string? FeatureBreakdownJson { get; init; }
        }
    }
}
