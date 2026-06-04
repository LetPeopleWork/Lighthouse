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
    [NonParallelizable]
    public class DeliveryMetricsHistoryReadApiIntegrationTest
    {
        private static readonly JsonSerializerOptions CaseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

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
                new { Probability = 85.0, ExpectedDate = DateTime.UtcNow.Date.AddDays(28) },
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

        private static bool EveryPointCarriesBacklogTotal(string body)
        {
            var points = Points(body);
            return points.Count > 0 && points.All(point => point.TotalWork > 0);
        }

        private static bool NoPointCarriesEstimatedItemCount(string body)
            => Points(body).All(point => point.EstimatedItemCount is null);

        private static bool BodyCarriesEverySeries(string body)
        {
            var point = Points(body).Single();
            return point.EstimatedItemCount.HasValue
                && point.ForecastHowMany.HasValue
                && point.LikelihoodPercentage.HasValue
                && point.WhenDistribution is { Count: > 0 };
        }

        private static IReadOnlyList<HistoryPointView> Points(string body)
        {
            var dto = JsonSerializer.Deserialize<HistoryView>(body, CaseInsensitiveJson);
            return dto?.Points ?? [];
        }

        private sealed record HistoryView(DateTime? FirstSnapshotDate, IReadOnlyList<HistoryPointView> Points);

        private sealed record HistoryPointView(
            DateTime Date,
            int TotalWork,
            int DoneWork,
            int RemainingWork,
            int? EstimatedItemCount,
            int? ForecastHowMany,
            double? LikelihoodPercentage,
            IReadOnlyList<WhenDistributionView>? WhenDistribution);

        private sealed record WhenDistributionView(double Probability, DateTime ExpectedDate);
    }
}
