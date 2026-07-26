using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class ProcessBehaviorSnapshotDtoTests
    {
        [Test]
        public void Constructor_MapsSnapshot_FormattingRecordedAtAsIsoDate()
        {
            var snapshot = new ProcessBehaviorSnapshot
            {
                RecordedAt = new DateOnly(2026, 3, 5),
                MetricType = ProcessBehaviorMetricType.Throughput,
                Unpl = 14,
                Average = 9,
                Lnpl = 4,
            };

            var dto = new ProcessBehaviorSnapshotDto(snapshot);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.RecordedAt, Is.EqualTo("2026-03-05"), "RecordedAt is serialized as an ISO yyyy-MM-dd date string");
                Assert.That(dto.Unpl, Is.EqualTo(14));
                Assert.That(dto.Average, Is.EqualTo(9));
                Assert.That(dto.Lnpl, Is.EqualTo(4));
            }
        }

        [Test]
        public void Constructor_MapsALowerLimitClampedToZero_WithoutInventingPrecision()
        {
            var snapshot = new ProcessBehaviorSnapshot
            {
                RecordedAt = new DateOnly(2026, 3, 6),
                MetricType = ProcessBehaviorMetricType.Throughput,
                Unpl = 6,
                Average = 3,
                Lnpl = 0,
            };

            var dto = new ProcessBehaviorSnapshotDto(snapshot);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Lnpl, Is.Zero, "A zero lower natural process limit is a real value, not a missing one");
                Assert.That(dto.Average, Is.EqualTo(3));
                Assert.That(dto.Unpl, Is.EqualTo(6));
            }
        }
    }
}
