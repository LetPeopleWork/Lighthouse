using Lighthouse.Backend.API.DTO;
using NUnit.Framework;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedCountSnapshotDtoTests
    {
        private static readonly JsonSerializerOptions CamelCaseOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        [Test]
        public void DefaultConstructor_HasEmptyRecordedAt()
        {
            var dto = new BlockedCountSnapshotDto();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.RecordedAt, Is.EqualTo(string.Empty));
                Assert.That(dto.BlockedCount, Is.Zero);
            }
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var dto = new BlockedCountSnapshotDto
            {
                RecordedAt = "2026-07-01",
                BlockedCount = 5,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.RecordedAt, Is.EqualTo("2026-07-01"));
                Assert.That(dto.BlockedCount, Is.EqualTo(5));
            }
        }

        [Test]
        public void Serialize_ProducesCamelCaseJson()
        {
            var dto = new BlockedCountSnapshotDto
            {
                RecordedAt = "2026-07-01",
                BlockedCount = 3,
            };

            var json = JsonSerializer.Serialize(dto, CamelCaseOptions);

            Assert.That(json, Does.Contain("\"recordedAt\""));
            Assert.That(json, Does.Contain("\"blockedCount\""));
            Assert.That(json, Does.Contain("\"2026-07-01\""));
            Assert.That(json, Does.Contain("3"));
            Assert.That(json, Does.Not.Contain("\"RecordedAt\""));
            Assert.That(json, Does.Not.Contain("\"BlockedCount\""));
        }

        [Test]
        public void Deserialize_FromCamelCaseJson_RoundTrips()
        {
            var json = """{"recordedAt":"2026-07-01","blockedCount":9}""";

            var dto = JsonSerializer.Deserialize<BlockedCountSnapshotDto>(json, CaseInsensitiveOptions);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto, Is.Not.Null);
                Assert.That(dto!.RecordedAt, Is.EqualTo("2026-07-01"));
                Assert.That(dto.BlockedCount, Is.EqualTo(9));
            }
        }

        [Test]
        public void Deserialize_FromPascalCaseJson_RoundTrips()
        {
            var json = """{"RecordedAt":"2026-07-01","BlockedCount":9}""";
            var dto = JsonSerializer.Deserialize<BlockedCountSnapshotDto>(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto, Is.Not.Null);
                Assert.That(dto!.RecordedAt, Is.EqualTo("2026-07-01"));
                Assert.That(dto.BlockedCount, Is.EqualTo(9));
            }
        }

        [Test]
        public void Serialize_ZeroBlockedCount_ProducesZeroNotNegative()
        {
            var dto = new BlockedCountSnapshotDto
            {
                RecordedAt = "2026-07-01",
                BlockedCount = 0,
            };

            var json = JsonSerializer.Serialize(dto, CamelCaseOptions);

            Assert.That(json, Does.Contain("\"blockedCount\":0"));
        }
    }
}
