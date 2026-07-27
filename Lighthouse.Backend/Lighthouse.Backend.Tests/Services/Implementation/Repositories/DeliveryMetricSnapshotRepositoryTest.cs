using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class DeliveryMetricSnapshotRepositoryTest : IntegrationTestBase
    {
        [Test]
        public async Task GetOrCreateForDay_NewDeliveryAndDay_InsertsSingleSnapshot()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var day = new DateOnly(2026, 5, 25);
            var snapshot = subject.GetOrCreateForDay(deliveryId, day);
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.DeliveryId, Is.EqualTo(deliveryId));
                Assert.That(snapshot.RecordedDay, Is.EqualTo(day));
                Assert.That(subject.GetByDelivery(deliveryId).Count(), Is.EqualTo(1));
            }
        }

        /// <summary>
        /// The legacy instant column keeps being written during the expand phase so a rollback to the
        /// previous release still reads correct data.
        /// </summary>
        [Test]
        public async Task GetOrCreateForDay_NewDeliveryAndDay_AlsoWritesTheLegacyInstantAtMidnightUtc()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var snapshot = subject.GetOrCreateForDay(deliveryId, new DateOnly(2026, 5, 25));
            await subject.Save();

            Assert.That(snapshot.RecordedAt, Is.EqualTo(new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public async Task GetOrCreateForDay_SameDeliveryAndDay_ReturnsExistingRowWithoutDuplicating()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            var day = new DateOnly(2026, 5, 25);
            var firstSnapshot = subject.GetOrCreateForDay(deliveryId, day);
            await subject.Save();

            var secondSnapshot = subject.GetOrCreateForDay(deliveryId, day);
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondSnapshot.Id, Is.EqualTo(firstSnapshot.Id));
                Assert.That(subject.GetByDelivery(deliveryId).Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetByDelivery_MultipleDays_ReturnsSnapshotsOrderedByRecordedDayAscending()
        {
            var deliveryId = await GivenPersistedDelivery();
            var subject = CreateSubject();

            subject.GetOrCreateForDay(deliveryId, new DateOnly(2026, 5, 26));
            await subject.Save();
            subject.GetOrCreateForDay(deliveryId, new DateOnly(2026, 5, 25));
            await subject.Save();

            var orderedDays = subject.GetByDelivery(deliveryId).Select(s => s.RecordedDay).ToList();

            Assert.That(orderedDays, Is.Ordered.Ascending);
        }

        [Test]
        public async Task GetOrCreateForDay_SnapshotOnTheAdjacentDay_IsNotMatchedSoANewRowIsCreated()
        {
            var deliveryId = await GivenPersistedDelivery();

            var nextDay = new DateOnly(2026, 5, 26);
            DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = deliveryId,
                RecordedDay = nextDay,
                RecordedAt = nextDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();
            var day = new DateOnly(2026, 5, 25);
            var snapshot = subject.GetOrCreateForDay(deliveryId, day);
            await subject.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.RecordedDay, Is.EqualTo(day));
                Assert.That(subject.GetByDelivery(deliveryId).Count(), Is.EqualTo(2));
            }
        }

        /// <summary>
        /// The pre-DateOnly repository had to tie-break between several instants inside one day. That
        /// state is now unrepresentable: the unique (DeliveryId, RecordedDay) index rejects a second
        /// row, which is exactly what this asserts. A colliding LEGACY population is refused outright
        /// by DeliveryMetricSnapshotDayCollisionGuard rather than de-duplicated.
        /// </summary>
        [Test]
        public async Task DayKey_ASecondRowForTheSameDeliveryDay_IsRejectedByTheDatabase()
        {
            var deliveryId = await GivenPersistedDelivery();

            var day = new DateOnly(2026, 5, 25);
            DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = deliveryId,
                RecordedDay = day,
                RecordedAt = new DateTime(2026, 5, 25, 6, 0, 0, DateTimeKind.Utc),
            });
            await DatabaseContext.SaveChangesAsync();

            DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
            {
                DeliveryId = deliveryId,
                RecordedDay = day,
                RecordedAt = new DateTime(2026, 5, 25, 18, 0, 0, DateTimeKind.Utc),
            });

            var exception = Assert.ThrowsAsync<DbUpdateException>(async () => await DatabaseContext.SaveChangesAsync());
            Assert.That(exception!.InnerException!.Message, Does.Contain("UNIQUE").IgnoreCase);
        }

        [Test]
        public async Task GetSnapshotCountsByDelivery_MultipleDeliveries_ReturnsCountPerDeliveryInOneQuery()
        {
            var deliveryWithSnapshots = await GivenPersistedDelivery();
            var deliveryWithoutSnapshots = await GivenPersistedDelivery();
            var subject = CreateSubject();

            foreach (var dayOfMonth in new[] { 25, 26, 27, 28 })
            {
                var recordedDay = new DateOnly(2026, 5, dayOfMonth);
                DatabaseContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
                {
                    DeliveryId = deliveryWithSnapshots,
                    RecordedDay = recordedDay,
                    RecordedAt = recordedDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                });
            }
            await DatabaseContext.SaveChangesAsync();

            var counts = subject.GetSnapshotCountsByDelivery([deliveryWithSnapshots, deliveryWithoutSnapshots]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counts[deliveryWithSnapshots], Is.EqualTo(4));
                Assert.That(counts.TryGetValue(deliveryWithoutSnapshots, out var zero) ? zero : 0, Is.Zero);
            }
        }

        private async Task<int> GivenPersistedDelivery()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id, TestToday.Ambient);
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
