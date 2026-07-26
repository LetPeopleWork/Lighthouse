using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class ProcessBehaviorSnapshotRepositoryTests
    {
        private static readonly DateOnly TargetDay = new(2026, 7, 1);

        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();
        }

        [Test]
        public async Task Add_ThenSave_RoundTripsEveryFieldThroughThePort()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            subject.Add(Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3));
            await subject.Save();

            var stored = subject.GetByPredicate(s => s.OwnerId == 1);

            Assert.That(stored, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.OwnerId, Is.EqualTo(1));
                Assert.That(stored.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(stored.RecordedAt, Is.EqualTo(TargetDay));
                Assert.That(stored.MetricType, Is.EqualTo(ProcessBehaviorMetricType.Throughput));
                Assert.That(stored.Unpl, Is.EqualTo(13));
                Assert.That(stored.Average, Is.EqualTo(8));
                Assert.That(stored.Lnpl, Is.EqualTo(3));
                Assert.That(stored.Id, Is.Not.Zero, "the store must assign an identity");
            }
        }

        [TestCase(2, OwnerType.Team, 0, TestName = "NaturalKey_DiscriminatesByOwnerId")]
        [TestCase(1, OwnerType.Portfolio, 0, TestName = "NaturalKey_DiscriminatesByOwnerType")]
        [TestCase(1, OwnerType.Team, 1, TestName = "NaturalKey_DiscriminatesByRecordedDay")]
        public async Task NaturalKeyQuery_ReturnsOnlyTheRowMatchingEveryKeyPart(
            int neighbourOwnerId, OwnerType neighbourOwnerType, int neighbourDayOffset)
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            subject.Add(Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3));
            subject.Add(Snapshot(neighbourOwnerId, neighbourOwnerType, TargetDay.AddDays(neighbourDayOffset), 99, 98, 97));
            await subject.Save();

            var matches = QueryNaturalKey(subject, 1, OwnerType.Team, TargetDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matches, Has.Count.EqualTo(1),
                    "a row differing in exactly one natural-key part is a different snapshot, not the same one");
                Assert.That(matches[0].Unpl, Is.EqualTo(13));
                Assert.That(matches[0].Average, Is.EqualTo(8));
                Assert.That(matches[0].Lnpl, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task NaturalKeyQuery_NoMatchingRow_ReturnsEmpty()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            subject.Add(Snapshot(2, OwnerType.Team, TargetDay, 13, 8, 3));
            await subject.Save();

            var matches = QueryNaturalKey(subject, 1, OwnerType.Team, TargetDay);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public async Task Update_OnTheSameNaturalKey_OverwritesTheLimitsInPlace()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            var snapshot = Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3);
            subject.Add(snapshot);
            await subject.Save();

            snapshot.Unpl = 20;
            snapshot.Average = 12;
            snapshot.Lnpl = 4;
            subject.Update(snapshot);
            await subject.Save();

            var matches = QueryNaturalKey(subject, 1, OwnerType.Team, TargetDay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matches, Has.Count.EqualTo(1), "one row per natural key per calendar day");
                Assert.That(matches[0].Unpl, Is.EqualTo(20));
                Assert.That(matches[0].Average, Is.EqualTo(12));
                Assert.That(matches[0].Lnpl, Is.EqualTo(4));
            }
        }

        [Test]
        public async Task Remove_DeletesTheSnapshotFromTheStore()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            var snapshot = Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3);
            subject.Add(snapshot);
            await subject.Save();

            subject.Remove(snapshot);
            await subject.Save();

            Assert.That(subject.GetAll(), Is.Empty);
        }

        private static List<ProcessBehaviorSnapshot> QueryNaturalKey(
            ProcessBehaviorSnapshotRepository repository, int ownerId, OwnerType ownerType, DateOnly recordedAt)
        {
            return repository.GetAllByPredicate(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.MetricType == ProcessBehaviorMetricType.Throughput &&
                    s.RecordedAt == recordedAt)
                .ToList();
        }

        private static ProcessBehaviorSnapshot Snapshot(
            int ownerId, OwnerType ownerType, DateOnly recordedAt, int unpl, int average, int lnpl)
        {
            return new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                MetricType = ProcessBehaviorMetricType.Throughput,
                Unpl = unpl,
                Average = average,
                Lnpl = lnpl,
            };
        }

        private ProcessBehaviorSnapshotRepository CreateSubject(LighthouseAppContext context)
        {
            return new ProcessBehaviorSnapshotRepository(context, Mock.Of<ILogger<ProcessBehaviorSnapshotRepository>>());
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }
    }
}
