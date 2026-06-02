using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DeliveryMetricsHistoryDtoTest
    {
        [Test]
        public void From_OrdersPointsByDateAscending_RegardlessOfInputOrder()
        {
            var deliveryDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var snapshots = new[]
            {
                new DeliveryMetricSnapshot { RecordedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
            };

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, snapshots);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Points, Has.Count.EqualTo(3));
                Assert.That(dto.Points[0].Date, Is.EqualTo(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(dto.Points[1].Date, Is.EqualTo(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(dto.Points[2].Date, Is.EqualTo(new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public void From_MapsActualWorkCounts_AndPassesNullableForwardFieldsThroughAsIs()
        {
            var deliveryDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var snapshots = new[]
            {
                new DeliveryMetricSnapshot
                {
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    TotalWork = 20,
                    DoneWork = 8,
                    RemainingWork = 12,
                    EstimatedTotalWork = null,
                    ForecastHowMany = null,
                    LikelihoodPercentage = null,
                    WhenDistributionJson = null,
                },
            };

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, snapshots);

            var point = dto.Points[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(point.TotalWork, Is.EqualTo(20));
                Assert.That(point.DoneWork, Is.EqualTo(8));
                Assert.That(point.RemainingWork, Is.EqualTo(12));
                Assert.That(point.EstimatedTotalWork, Is.Null);
                Assert.That(point.ForecastHowMany, Is.Null);
                Assert.That(point.LikelihoodPercentage, Is.Null);
                Assert.That(point.WhenDistribution, Is.Null);
            }
        }

        [Test]
        public void From_SetsFirstSnapshotDateToEarliestRecordedAt_AndCarriesDeliveryDate()
        {
            var deliveryDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var snapshots = new[]
            {
                new DeliveryMetricSnapshot { RecordedAt = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
            };

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, snapshots);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.DeliveryDate, Is.EqualTo(deliveryDate));
                Assert.That(dto.FirstSnapshotDate, Is.EqualTo(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public void From_LeavesFirstSnapshotDateNull_AndPointsEmpty_WhenNoSnapshotsRecorded()
        {
            var deliveryDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.FirstSnapshotDate, Is.Null);
                Assert.That(dto.Points, Is.Empty);
                Assert.That(dto.DeliveryDate, Is.EqualTo(deliveryDate));
            }
        }
    }
}
