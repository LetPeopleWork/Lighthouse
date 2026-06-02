using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class DeliveryMetricSnapshotRepositoryTest : IntegrationTestBase
    {
        [Test]
        public async Task GetOrCreateForDay_NewDeliveryAndDate_InsertsSingleSnapshot()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var recordedAt = new DateTime(2026, 5, 25, 9, 30, 0, DateTimeKind.Utc);
            var snapshot = subject.GetOrCreateForDay(deliveryId, recordedAt);
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.DeliveryId, Is.EqualTo(deliveryId));
                Assert.That(subject.GetByDelivery(deliveryId).Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetOrCreateForDay_SameDeliveryAndDate_ReturnsExistingRowWithoutDuplicating()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var morning = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc);
            var firstSnapshot = subject.GetOrCreateForDay(deliveryId, morning);
            await subject.Save();

            var evening = new DateTime(2026, 5, 25, 21, 0, 0, DateTimeKind.Utc);
            var secondSnapshot = subject.GetOrCreateForDay(deliveryId, evening);
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondSnapshot.Id, Is.EqualTo(firstSnapshot.Id));
                Assert.That(subject.GetByDelivery(deliveryId).Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetByDelivery_MultipleDays_ReturnsSnapshotsOrderedByRecordedAtAscending()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var dayTwo = new DateTime(2026, 5, 26, 8, 0, 0, DateTimeKind.Utc);
            var dayOne = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc);
            subject.GetOrCreateForDay(deliveryId, dayTwo);
            await subject.Save();
            subject.GetOrCreateForDay(deliveryId, dayOne);
            await subject.Save();

            var orderedDates = subject.GetByDelivery(deliveryId).Select(s => s.RecordedAt).ToList();

            Assert.That(orderedDates, Is.Ordered.Ascending);
        }

        private async Task<int> GivenPersistedDelivery()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id);
            DatabaseContext.Deliveries.Add(delivery);
            await DatabaseContext.SaveChangesAsync();

            return delivery.Id;
        }

        private DeliveryMetricSnapshotRepository CreateSubject()
        {
            return new DeliveryMetricSnapshotRepository(DatabaseContext, Mock.Of<ILogger<DeliveryMetricSnapshotRepository>>());
        }
    }
}
