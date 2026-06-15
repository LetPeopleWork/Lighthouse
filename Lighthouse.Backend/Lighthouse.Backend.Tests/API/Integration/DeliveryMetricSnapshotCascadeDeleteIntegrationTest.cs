using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class DeliveryMetricSnapshotCascadeDeleteIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task DeleteDelivery_WithRecordedSnapshots_CascadeDeletesSnapshotRows()
        {
            var (deliveryId, _) = await SeedDeliveryWithSnapshotRows();

            await DeleteDelivery(deliveryId);

            Assert.That(await SnapshotRowCountForDelivery(deliveryId), Is.Zero);
        }

        private async Task<(int DeliveryId, int SnapshotCount)> SeedDeliveryWithSnapshotRows()
        {
            var portfolio = await AddPortfolio();
            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);

            DatabaseContext.Deliveries.Add(delivery);
            await DatabaseContext.SaveChangesAsync();

            var recordedDay = DateTime.UtcNow.Date;
            DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = recordedDay,
                TotalWork = 10,
                DoneWork = 4,
                RemainingWork = 6,
            });
            DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = delivery.Id,
                RecordedAt = recordedDay.AddDays(1),
                TotalWork = 10,
                DoneWork = 7,
                RemainingWork = 3,
            });
            await DatabaseContext.SaveChangesAsync();

            return (delivery.Id, await SnapshotRowCountForDelivery(delivery.Id));
        }

        private async Task DeleteDelivery(int deliveryId)
        {
            var deleteResponse = await Client.DeleteAsync($"/api/latest/deliveries/{deliveryId}");
            deleteResponse.EnsureSuccessStatusCode();
        }

        private async Task<int> SnapshotRowCountForDelivery(int deliveryId)
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Data.LighthouseAppContext>();
            return await context.DeliveryMetricSnapshots.CountAsync(s => s.DeliveryId == deliveryId);
        }

        private async Task<Portfolio> AddPortfolio()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            var team = new Team
            {
                Name = "Test Team",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };

            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio
            {
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };

            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }
    }
}
