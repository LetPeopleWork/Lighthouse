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
                new DeliveryMetricSnapshot { RecordedDay = new DateOnly(2026, 2, 3), RecordedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedDay = new DateOnly(2026, 2, 1), RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedDay = new DateOnly(2026, 2, 2), RecordedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
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
                    RecordedDay = DateOnly.FromDateTime(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
                    TotalWork = 20,
                    DoneWork = 8,
                    RemainingWork = 12,
                    EstimatedItemCount = null,
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
                Assert.That(point.EstimatedItemCount, Is.Null);
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
                new DeliveryMetricSnapshot { RecordedDay = new DateOnly(2026, 2, 5), RecordedAt = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc) },
                new DeliveryMetricSnapshot { RecordedDay = new DateOnly(2026, 2, 2), RecordedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
            };

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, snapshots);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.DeliveryDate, Is.EqualTo(deliveryDate));
                Assert.That(dto.FirstSnapshotDate, Is.EqualTo(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public void From_ParsesCamelCaseWhenDistributionJson_IntoProbabilityAndExpectedDatePoints()
        {
            var deliveryDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var snapshots = new[]
            {
                new DeliveryMetricSnapshot
                {
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    RecordedDay = DateOnly.FromDateTime(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
                    WhenDistributionJson = "[{\"probability\":0.85,\"expectedDate\":\"2026-04-10T00:00:00Z\"}]",
                },
            };

            var dto = DeliveryMetricsHistoryDto.From(deliveryDate, snapshots);

            var distribution = dto.Points[0].WhenDistribution;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(distribution, Has.Count.EqualTo(1));
                Assert.That(distribution![0].Probability, Is.EqualTo(0.85));
                Assert.That(distribution[0].ExpectedDate, Is.EqualTo(new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public void From_ReadsTheSizeAndEstimateFlagRecordedForEachFeature()
        {
            var dto = DeliveryMetricsHistoryDto.From(
                DeliveryDate,
                [SnapshotWithBreakdown(
                    "[{\"referenceId\":\"EPIC-1\",\"name\":\"Checkout\",\"completion\":40,\"likelihood\":80,\"totalItems\":8,\"isUsingDefaultSize\":true}]")]);

            var entry = dto.Points[0].FeatureBreakdown[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.TotalItems, Is.EqualTo(8));
                Assert.That(entry.IsUsingDefaultSize, Is.True);
            }
        }

        [Test]
        public void From_StillReadsASnapshotRecordedBeforeSizesWereEverWritten()
        {
            // The four-field shape every row carried before Epic #5585 slice 02. It must keep loading,
            // with the two new fields simply absent - there is no backfill (D5).
            var dto = DeliveryMetricsHistoryDto.From(
                DeliveryDate,
                [SnapshotWithBreakdown(
                    "[{\"referenceId\":\"EPIC-1\",\"name\":\"Checkout\",\"completion\":40,\"likelihood\":80}]")]);

            var entry = dto.Points[0].FeatureBreakdown[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.ReferenceId, Is.EqualTo("EPIC-1"));
                Assert.That(entry.TotalItems, Is.Null);
                Assert.That(entry.IsUsingDefaultSize, Is.Null);
            }
        }

        [Test]
        public void From_StillReadsASnapshotWhoseFeatureCouldNotBeForecast()
        {
            // ADR-120 / DDD-6. The recorder serialises DeliveryFeatureMetric verbatim and its Likelihood
            // is double? (ADR-112: a feature whose contributing team has no throughput reports unknown),
            // so "likelihood": null reaches the column - but the DTO declared it non-nullable, and
            // System.Text.Json throws on null -> double, 500-ing the whole delivery's metrics-history.
            // Predates #5585; repaired here because this is the read path slice 02 widens.
            // Asserted as "does not throw" rather than "Likelihood is null" on purpose: while the DTO
            // declares double, NUnit2023 rejects an Is.Null assertion at COMPILE time, which would make
            // this test un-runnable instead of red. Reading the whole delivery's history is the
            // behaviour that matters.
            var snapshot = SnapshotWithBreakdown(
                "[{\"referenceId\":\"EPIC-1\",\"name\":\"Checkout\",\"completion\":40,\"likelihood\":null}]");

            Assert.That(
                () => DeliveryMetricsHistoryDto.From(DeliveryDate, [snapshot]),
                Throws.Nothing);
        }

        private static readonly DateTime DeliveryDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        private static DeliveryMetricSnapshot SnapshotWithBreakdown(string featureBreakdownJson)
        {
            return new DeliveryMetricSnapshot
            {
                RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                RecordedDay = new DateOnly(2026, 2, 1),
                FeatureBreakdownJson = featureBreakdownJson,
            };
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
