using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class DeliveryMetricsHistoryReadApiIntegrationTest
    {
        private static readonly JsonSerializerOptions CaseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

        private static readonly int[] RecordedWhenPercentiles = [50, 70, 85, 95];

        private static readonly string[] ExpectedBreakdownReferenceIds = ["FEAT-1", "FEAT-2"];

        private const double LowerSpreadPercentile = 50.0;
        private const double UpperSpreadPercentile = 95.0;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetMetricsHistory_DeliveryWithThreeRecordedDays_ReturnsOrderedBacklogAndDoneSeries()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithSnapshotSeries();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PointDatesInOrder(body), Is.True, $"Points must be in date order. Body: {body}");
                Assert.That(PointCount(body), Is.EqualTo(3), body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_NoSnapshotsRecordedYet_ReturnsEmptySeriesNotError()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithoutSnapshots();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PointCount(body), Is.Zero, body);
                Assert.That(FirstSnapshotDateIsAbsent(body), Is.True, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_NonPremiumInstance_StillReadsTheHistory()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            var (portfolioId, deliveryId) = SeedDeliveryWithSnapshotSeries();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PointCount(body), Is.EqualTo(3), body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_PortfolioViewer_CanReadTheRecordedSeries()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithSnapshotSeries();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetMetricsHistory_SnapshotsRecordedWithTargetDates_CarryTargetPerPoint()
        {
            var (portfolioId, deliveryId, earlierTarget, laterTarget) = SeedDeliveryWithVaryingRecordedTarget();

            client.AsPortfolioViewer(portfolioId);
            var body = await (await client.GetAsync(MetricsHistoryUrl(deliveryId))).Content.ReadAsStringAsync();

            var points = Points(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(EveryPointCarriesTargetDate(body), Is.True, body);
                Assert.That(points[0].TargetDateAtSnapshot, Is.EqualTo(earlierTarget), body);
                Assert.That(points[^1].TargetDateAtSnapshot, Is.EqualTo(laterTarget), body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_SnapshotsRecordedBeforeTargetCapture_CarryNoTargetButKeepDeliveryDate()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithSnapshotSeries();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NoPointCarriesTargetDate(body), Is.True, body);
                Assert.That(DeliveryDateIsPresent(body), Is.True, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_ConsolidatedEndpoint_CarriesEverySeriesInOneResponse()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithEverySeriesRecorded();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(BodyCarriesEverySeries(body), Is.True, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_SnapshotsRecordedBeforeEstimatedPortion_CarryBacklogTotalButNoEstimatedItemCount()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithOnlyBacklogTotals();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(EveryPointCarriesBacklogTotal(body), Is.True, body);
                Assert.That(NoPointCarriesEstimatedItemCount(body), Is.True, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_SnapshotsRecordedBeforePredictability_CarryNoLikelihoodAndNoWhenDistribution()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithOnlyBacklogTotals();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PointDatesInOrder(body), Is.True, body);
                Assert.That(NoPointCarriesLikelihood(body), Is.True, body);
                Assert.That(NoPointCarriesWhenDistribution(body), Is.True, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_ImprovingWhenSpreadNarrows_DegradingWhenSpreadWidens()
        {
            var (improvingPortfolioId, improvingDeliveryId, _) = SeedDeliveryWithNarrowingWhenSpread();
            var (degradingPortfolioId, degradingDeliveryId, _) = SeedDeliveryWithWideningWhenSpread();

            client.AsPortfolioViewer(improvingPortfolioId);
            var improvingBody = await (await client.GetAsync(MetricsHistoryUrl(improvingDeliveryId))).Content.ReadAsStringAsync();

            client.AsPortfolioViewer(degradingPortfolioId);
            var degradingBody = await (await client.GetAsync(MetricsHistoryUrl(degradingDeliveryId))).Content.ReadAsStringAsync();

            var improvingPoints = Points(improvingBody);
            var degradingPoints = Points(degradingBody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LastDaySpread(improvingPoints), Is.LessThan(FirstDaySpread(improvingPoints)), improvingBody);
                Assert.That(LastDaySpread(degradingPoints), Is.GreaterThan(FirstDaySpread(degradingPoints)), degradingBody);
            }
        }

        [Test]
        public async Task GetMetricsHistory_WithRecordedFeatureBreakdown_ReturnsPerFeatureCompletionAndLikelihood()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithFeatureBreakdown();

            client.AsPortfolioViewer(portfolioId);
            var body = await (await client.GetAsync(MetricsHistoryUrl(deliveryId))).Content.ReadAsStringAsync();

            var nullableBreakdown = Points(body).Single().FeatureBreakdown;
            Assert.That(nullableBreakdown, Is.Not.Null, body);
            var breakdown = nullableBreakdown!;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(breakdown.Select(entry => entry.ReferenceId), Is.EquivalentTo(ExpectedBreakdownReferenceIds), body);
                Assert.That(breakdown.Single(entry => entry.ReferenceId == "FEAT-1").Name, Is.EqualTo("Checkout"), body);
                Assert.That(breakdown.Single(entry => entry.ReferenceId == "FEAT-1").Completion, Is.EqualTo(40.0), body);
                Assert.That(breakdown.Single(entry => entry.ReferenceId == "FEAT-2").Likelihood, Is.EqualTo(88.0), body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_FeatureBreakdownJsonIsLiteralNull_ReturnsEmptyBreakdown()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithLiteralNullFeatureBreakdown();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(MetricsHistoryUrl(deliveryId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(Points(body).Single().FeatureBreakdown, Is.Not.Null, body);
                Assert.That(Points(body).Single().FeatureBreakdown, Is.Empty, body);
            }
        }

        [Test]
        public async Task GetMetricsHistory_SnapshotsWithoutFeatureBreakdown_ReturnEmptyBreakdown()
        {
            var (portfolioId, deliveryId) = SeedDeliveryWithOnlyBacklogTotals();

            client.AsPortfolioViewer(portfolioId);
            var body = await (await client.GetAsync(MetricsHistoryUrl(deliveryId))).Content.ReadAsStringAsync();

            Assert.That(Points(body).All(point => point.FeatureBreakdown is null or { Count: 0 }), Is.True, body);
        }

        [Test]
        public async Task GetMetricsHistory_ForTheWhenView_ReturnsTheDeliveryTargetDate()
        {
            var (portfolioId, deliveryId, targetDate) = SeedDeliveryWithNarrowingWhenSpread();

            client.AsPortfolioViewer(portfolioId);
            var body = await (await client.GetAsync(MetricsHistoryUrl(deliveryId))).Content.ReadAsStringAsync();

            var historyView = JsonSerializer.Deserialize<HistoryView>(body, CaseInsensitiveJson);
            Assert.That(historyView!.DeliveryDate, Is.EqualTo(targetDate), body);
        }

        private static string MetricsHistoryUrl(int deliveryId)
            => $"/api/latest/deliveries/{deliveryId}/metrics-history";

        private (int portfolioId, int deliveryId) SeedDeliveryWithSnapshotSeries()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var firstDay = DateTime.UtcNow.Date.AddDays(-2);
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay,
                TotalWork = 10,
                DoneWork = 2,
                RemainingWork = 8,
            });
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay.AddDays(1),
                TotalWork = 10,
                DoneWork = 5,
                RemainingWork = 5,
            });
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay.AddDays(2),
                TotalWork = 10,
                DoneWork = 9,
                RemainingWork = 1,
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId, DateTime earlierTarget, DateTime laterTarget) SeedDeliveryWithVaryingRecordedTarget()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var laterTarget = DateTime.UtcNow.Date.AddDays(30);
            var earlierTarget = laterTarget.AddDays(-14);
            var delivery = new Delivery("Release 6", laterTarget, portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var firstDay = DateTime.UtcNow.Date.AddDays(-2);
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay,
                TargetDateAtSnapshot = earlierTarget,
                TotalWork = 10,
                DoneWork = 2,
                RemainingWork = 8,
            });
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay.AddDays(1),
                TargetDateAtSnapshot = earlierTarget,
                TotalWork = 10,
                DoneWork = 5,
                RemainingWork = 5,
            });
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay.AddDays(2),
                TargetDateAtSnapshot = laterTarget,
                TotalWork = 10,
                DoneWork = 9,
                RemainingWork = 1,
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id, earlierTarget, laterTarget);
        }

        private (int portfolioId, int deliveryId) SeedDeliveryWithoutSnapshots()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 3", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId) SeedDeliveryWithOnlyBacklogTotals()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 4", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var firstDay = DateTime.UtcNow.Date.AddDays(-1);
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay,
                TotalWork = 14,
                DoneWork = 3,
                RemainingWork = 11,
            });
            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = firstDay.AddDays(1),
                TotalWork = 14,
                DoneWork = 6,
                RemainingWork = 8,
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId) SeedDeliveryWithEverySeriesRecorded()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 2", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var whenDistributionJson = JsonSerializer.Serialize(new[]
            {
                new { Probability = 50.0, ExpectedDate = DateTime.UtcNow.Date.AddDays(20) },
                new { Probability = 70.0, ExpectedDate = DateTime.UtcNow.Date.AddDays(24) },
                new { Probability = 85.0, ExpectedDate = DateTime.UtcNow.Date.AddDays(28) },
                new { Probability = 95.0, ExpectedDate = DateTime.UtcNow.Date.AddDays(30) },
            });

            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = DateTime.UtcNow.Date,
                TotalWork = 12,
                DoneWork = 4,
                RemainingWork = 8,
                EstimatedItemCount = 15,
                ForecastHowMany = 6,
                LikelihoodPercentage = 72.5,
                WhenDistributionJson = whenDistributionJson,
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId) SeedDeliveryWithFeatureBreakdown()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 5", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var featureBreakdownJson = JsonSerializer.Serialize(new[]
            {
                new { ReferenceId = "FEAT-1", Name = "Checkout", Completion = 40.0, Likelihood = 100.0 },
                new { ReferenceId = "FEAT-2", Name = "Search", Completion = 75.0, Likelihood = 88.0 },
            });

            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = DateTime.UtcNow.Date,
                TotalWork = 20,
                DoneWork = 11,
                RemainingWork = 9,
                FeatureBreakdownJson = featureBreakdownJson,
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId) SeedDeliveryWithLiteralNullFeatureBreakdown()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var delivery = new Delivery("Release 6", DateTime.UtcNow.AddDays(30), portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = DateTime.UtcNow.Date,
                TotalWork = 20,
                DoneWork = 11,
                RemainingWork = 9,
                FeatureBreakdownJson = "null",
            });
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id);
        }

        private (int portfolioId, int deliveryId, DateTime targetDate) SeedDeliveryWithNarrowingWhenSpread()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var targetDate = DateTime.UtcNow.Date.AddDays(40);
            var delivery = new Delivery("Improving Release", targetDate, portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var firstDay = DateTime.UtcNow.Date.AddDays(-2);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay, 50.0, lowerOffset: 10, upperOffset: 38);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay.AddDays(1), 65.0, lowerOffset: 14, upperOffset: 32);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay.AddDays(2), 80.0, lowerOffset: 18, upperOffset: 24);
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id, targetDate);
        }

        private (int portfolioId, int deliveryId, DateTime targetDate) SeedDeliveryWithWideningWhenSpread()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();

            var portfolio = AddPortfolio(scope.ServiceProvider);
            var targetDate = DateTime.UtcNow.Date.AddDays(40);
            var delivery = new Delivery("Degrading Release", targetDate, portfolio.Id);
            dbContext.Deliveries.Add(delivery);
            dbContext.SaveChanges();

            var firstDay = DateTime.UtcNow.Date.AddDays(-2);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay, 80.0, lowerOffset: 18, upperOffset: 24);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay.AddDays(1), 65.0, lowerOffset: 14, upperOffset: 32);
            AddWhenDistributionSnapshot(dbContext, delivery.Id, firstDay.AddDays(2), 50.0, lowerOffset: 10, upperOffset: 38);
            dbContext.SaveChanges();

            return (portfolio.Id, delivery.Id, targetDate);
        }

        private static void AddWhenDistributionSnapshot(
            Lighthouse.Backend.Data.LighthouseAppContext dbContext,
            int deliveryId,
            DateTime recordedAt,
            double likelihood,
            int lowerOffset,
            int upperOffset)
        {
            var whenDistributionJson = JsonSerializer.Serialize(new[]
            {
                new { Probability = LowerSpreadPercentile, ExpectedDate = DateTime.UtcNow.Date.AddDays(lowerOffset) },
                new { Probability = UpperSpreadPercentile, ExpectedDate = DateTime.UtcNow.Date.AddDays(upperOffset) },
            });

            dbContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = deliveryId,
                RecordedAt = recordedAt,
                TotalWork = 20,
                DoneWork = 5,
                RemainingWork = 15,
                LikelihoodPercentage = likelihood,
                WhenDistributionJson = whenDistributionJson,
            });
        }

        private static Portfolio AddPortfolio(IServiceProvider serviceProvider)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var portfolioRepository = serviceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        private static bool PointDatesInOrder(string body)
        {
            var dates = Points(body).Select(point => point.Date).ToList();
            return dates.SequenceEqual(dates.OrderBy(date => date));
        }

        private static int PointCount(string body)
            => Points(body).Count;

        private static bool FirstSnapshotDateIsAbsent(string body)
        {
            var dto = JsonSerializer.Deserialize<HistoryView>(body, CaseInsensitiveJson);
            return dto?.FirstSnapshotDate is null;
        }

        private static bool DeliveryDateIsPresent(string body)
        {
            var dto = JsonSerializer.Deserialize<HistoryView>(body, CaseInsensitiveJson);
            return dto is not null && dto.DeliveryDate != default;
        }

        private static bool EveryPointCarriesBacklogTotal(string body)
        {
            var points = Points(body);
            return points.Count > 0 && points.All(point => point.TotalWork > 0);
        }

        private static bool EveryPointCarriesTargetDate(string body)
        {
            var points = Points(body);
            return points.Count > 0 && points.All(point => point.TargetDateAtSnapshot is not null);
        }

        private static bool NoPointCarriesTargetDate(string body)
            => Points(body).All(point => point.TargetDateAtSnapshot is null);

        private static bool NoPointCarriesEstimatedItemCount(string body)
            => Points(body).All(point => point.EstimatedItemCount is null);

        private static bool NoPointCarriesLikelihood(string body)
            => Points(body).All(point => point.LikelihoodPercentage is null);

        private static bool NoPointCarriesWhenDistribution(string body)
            => Points(body).All(point => point.WhenDistribution is null or { Count: 0 });

        private static bool BodyCarriesEverySeries(string body)
        {
            var point = Points(body).Single();
            var whenPercentiles = point.WhenDistribution?
                .Select(entry => (int)entry.Probability)
                .ToList();

            return point.EstimatedItemCount.HasValue
                && point.ForecastHowMany.HasValue
                && point.LikelihoodPercentage.HasValue
                && whenPercentiles is not null
                && whenPercentiles.OrderBy(percentile => percentile).SequenceEqual(RecordedWhenPercentiles);
        }

        private static TimeSpan FirstDaySpread(IReadOnlyList<HistoryPointView> points)
            => WhenSpread(points[0]);

        private static TimeSpan LastDaySpread(IReadOnlyList<HistoryPointView> points)
            => WhenSpread(points[^1]);

        private static TimeSpan WhenSpread(HistoryPointView point)
            => ExpectedDateAt(point, UpperSpreadPercentile) - ExpectedDateAt(point, LowerSpreadPercentile);

        private static DateTime ExpectedDateAt(HistoryPointView point, double percentile)
            => point.WhenDistribution!.Single(entry => entry.Probability == percentile).ExpectedDate;

        private static IReadOnlyList<HistoryPointView> Points(string body)
        {
            var dto = JsonSerializer.Deserialize<HistoryView>(body, CaseInsensitiveJson);
            return dto?.Points ?? [];
        }

        private sealed record HistoryView(DateTime DeliveryDate, DateTime? FirstSnapshotDate, IReadOnlyList<HistoryPointView> Points);

        private sealed record HistoryPointView(
            DateTime Date,
            DateTime? TargetDateAtSnapshot,
            int TotalWork,
            int DoneWork,
            int RemainingWork,
            int? EstimatedItemCount,
            int? ForecastHowMany,
            double? LikelihoodPercentage,
            IReadOnlyList<WhenDistributionView>? WhenDistribution,
            IReadOnlyList<FeatureBreakdownView>? FeatureBreakdown);

        private sealed record WhenDistributionView(double Probability, DateTime ExpectedDate);

        private sealed record FeatureBreakdownView(string ReferenceId, string Name, double Completion, double Likelihood);
    }
}
