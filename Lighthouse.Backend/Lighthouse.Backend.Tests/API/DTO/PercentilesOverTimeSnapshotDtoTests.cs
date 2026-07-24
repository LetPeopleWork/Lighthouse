using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class PercentilesOverTimeSnapshotDtoTests
    {
        [Test]
        public void Constructor_MapsSnapshot_FormattingRecordedAtAsIsoDate()
        {
            var snapshot = new PercentilesOverTimeSnapshot
            {
                RecordedAt = new DateOnly(2026, 3, 5),
                MetricType = MetricType.CycleTime,
                P50 = 4,
                P70 = 7,
                P85 = 11,
                P95 = 16,
            };

            var dto = new PercentilesOverTimeSnapshotDto(snapshot);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.RecordedAt, Is.EqualTo("2026-03-05"), "RecordedAt is serialized as an ISO yyyy-MM-dd date string");
                Assert.That(dto.MetricType, Is.EqualTo(MetricType.CycleTime));
                Assert.That(dto.P50, Is.EqualTo(4));
                Assert.That(dto.P70, Is.EqualTo(7));
                Assert.That(dto.P85, Is.EqualTo(11));
                Assert.That(dto.P95, Is.EqualTo(16));
            }
        }
    }
}
