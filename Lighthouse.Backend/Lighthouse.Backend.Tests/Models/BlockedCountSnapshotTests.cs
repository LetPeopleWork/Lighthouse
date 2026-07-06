using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedCountSnapshotTests
    {
        [Test]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            var snapshot = new BlockedCountSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Id, Is.EqualTo(0));
                Assert.That(snapshot.OwnerId, Is.EqualTo(0));
                Assert.That(snapshot.OwnerType, Is.EqualTo(default(OwnerType)));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(default(DateOnly)));
                Assert.That(snapshot.BlockedCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var snapshot = new BlockedCountSnapshot
            {
                Id = 42,
                OwnerId = 7,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 1),
                BlockedCount = 5,
            };

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Id, Is.EqualTo(42));
                Assert.That(snapshot.OwnerId, Is.EqualTo(7));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(snapshot.BlockedCount, Is.EqualTo(5));
            });
        }

        [TestCase(OwnerType.Team)]
        [TestCase(OwnerType.Portfolio)]
        public void OwnerType_DiscriminatesBetweenTeamAndPortfolio(OwnerType ownerType)
        {
            var snapshot = new BlockedCountSnapshot
            {
                OwnerId = 1,
                OwnerType = ownerType,
            };

            Assert.That(snapshot.OwnerType, Is.EqualTo(ownerType));
        }

        [Test]
        public void TwoSnapshots_SameOwnerSameDay_AreDistinctById()
        {
            var a = new BlockedCountSnapshot
            {
                Id = 1,
                OwnerId = 5,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 1),
                BlockedCount = 3,
            };

            var b = new BlockedCountSnapshot
            {
                Id = 2,
                OwnerId = 5,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 1),
                BlockedCount = 3,
            };

            Assert.That(a.Equals(b), Is.False, "IDs differ — unique index (OwnerId, OwnerType, RecordedAt) is the planned DB backstop, not entity equality");
            Assert.That(a.OwnerId, Is.EqualTo(b.OwnerId));
            Assert.That(a.OwnerType, Is.EqualTo(b.OwnerType));
            Assert.That(a.RecordedAt, Is.EqualTo(b.RecordedAt));
        }

        [Test]
        public void Snapshot_ImplementsIEntity()
        {
            var snapshot = new BlockedCountSnapshot();
            Assert.That(snapshot, Is.InstanceOf<IEntity>());
        }
    }
}
