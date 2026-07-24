using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class PercentilesOverTimeSnapshotTests
    {
        [Test]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            var snapshot = new PercentilesOverTimeSnapshot();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Id, Is.Zero);
                Assert.That(snapshot.OwnerId, Is.Zero);
                Assert.That(snapshot.OwnerType, Is.Default);
                Assert.That(snapshot.RecordedAt, Is.Default);
                Assert.That(snapshot.MetricType, Is.Default);
                Assert.That(snapshot.Horizon, Is.Null);
                Assert.That(snapshot.P50, Is.Zero);
                Assert.That(snapshot.P70, Is.Zero);
                Assert.That(snapshot.P85, Is.Zero);
                Assert.That(snapshot.P95, Is.Zero);
            }
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var snapshot = new PercentilesOverTimeSnapshot
            {
                Id = 42,
                OwnerId = 7,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 1),
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                P50 = 3,
                P70 = 5,
                P85 = 8,
                P95 = 13,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Id, Is.EqualTo(42));
                Assert.That(snapshot.OwnerId, Is.EqualTo(7));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(snapshot.MetricType, Is.EqualTo(MetricType.CycleTime));
                Assert.That(snapshot.Horizon, Is.EqualTo(30));
                Assert.That(snapshot.P50, Is.EqualTo(3));
                Assert.That(snapshot.P70, Is.EqualTo(5));
                Assert.That(snapshot.P85, Is.EqualTo(8));
                Assert.That(snapshot.P95, Is.EqualTo(13));
            }
        }

        [Test]
        public void Horizon_IsNullable_ForWorkItemAgeReadiness()
        {
            var snapshot = new PercentilesOverTimeSnapshot
            {
                OwnerId = 1,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = null,
            };

            Assert.That(snapshot.Horizon, Is.Null);
        }

        [TestCase(OwnerType.Team)]
        [TestCase(OwnerType.Portfolio)]
        public void OwnerType_DiscriminatesBetweenTeamAndPortfolio(OwnerType ownerType)
        {
            var snapshot = new PercentilesOverTimeSnapshot
            {
                OwnerId = 1,
                OwnerType = ownerType,
            };

            Assert.That(snapshot.OwnerType, Is.EqualTo(ownerType));
        }

        [Test]
        public void Snapshot_ImplementsIEntity()
        {
            var snapshot = new PercentilesOverTimeSnapshot();
            Assert.That(snapshot, Is.InstanceOf<IEntity>());
        }
    }
}
