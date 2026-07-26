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
        private static readonly DateOnly[] ThreeDaysFromTargetDay = [TargetDay, TargetDay.AddDays(1), TargetDay.AddDays(2)];
        private static readonly DateOnly[] TargetDayTo1 = [TargetDay, TargetDay.AddDays(1)];
        private static readonly DateOnly[] TargetDayPlus1To3 = [TargetDay.AddDays(1), TargetDay.AddDays(2), TargetDay.AddDays(3)];
        private static readonly DateOnly[] TargetDayPlus3To4 = [TargetDay.AddDays(3), TargetDay.AddDays(4)];

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

        [Test]
        public async Task GetSeries_ReturnsTheOwnersFamilyOrderedByRecordedAt()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            subject.Add(Snapshot(1, OwnerType.Team, TargetDay.AddDays(2), 15, 9, 3));
            subject.Add(Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3));
            subject.Add(Snapshot(1, OwnerType.Team, TargetDay.AddDays(1), 14, 8, 2));
            await subject.Save();

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, from: null, to: null);

            Assert.That(series.Select(s => s.RecordedAt), Is.EqualTo(ThreeDaysFromTargetDay).AsCollection);
        }

        [Test]
        public async Task GetSeries_NeverReturnsAnotherOwnerOrScope()
        {
            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Metric-family discrimination is not assertable yet — ProcessBehaviorMetricType has a single
            // member until slice 04 adds the remaining families. The predicate carries MetricType already.
            subject.Add(Snapshot(1, OwnerType.Team, TargetDay, 13, 8, 3));
            subject.Add(Snapshot(2, OwnerType.Team, TargetDay, 99, 98, 97));
            subject.Add(Snapshot(1, OwnerType.Portfolio, TargetDay, 88, 87, 86));
            await subject.Save();

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, from: null, to: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(1));
                Assert.That(series[0].Unpl, Is.EqualTo(13));
            }
        }

        [Test]
        public async Task GetSeries_WindowIsInclusiveAtBothEnds()
        {
            using var context = CreateContext();
            var subject = await GivenFiveConsecutiveDays(context);

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, TargetDay.AddDays(1), TargetDay.AddDays(3));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series.Select(s => s.RecordedAt), Is.EqualTo(TargetDayPlus1To3).AsCollection,
                    "a snapshot recorded exactly on the lower or upper bound is inside the window");
                Assert.That(series, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public async Task GetSeries_LowerBoundOnly_ReturnsEveryDayOnOrAfterIt()
        {
            using var context = CreateContext();
            var subject = await GivenFiveConsecutiveDays(context);

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, TargetDay.AddDays(3), to: null);

            Assert.That(series.Select(s => s.RecordedAt), Is.EqualTo(TargetDayPlus3To4).AsCollection);
        }

        [Test]
        public async Task GetSeries_UpperBoundOnly_ReturnsEveryDayOnOrBeforeIt()
        {
            using var context = CreateContext();
            var subject = await GivenFiveConsecutiveDays(context);

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, from: null, to: TargetDay.AddDays(1));

            Assert.That(series.Select(s => s.RecordedAt), Is.EqualTo(TargetDayTo1).AsCollection);
        }

        [Test]
        public async Task GetSeries_NoBounds_ReturnsTheFullHistory()
        {
            using var context = CreateContext();
            var subject = await GivenFiveConsecutiveDays(context);

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, from: null, to: null);

            Assert.That(series, Has.Count.EqualTo(5), "omitting both bounds must not filter anything out");
        }

        [Test]
        public async Task GetSeries_WindowWithNothingRecordedInside_ReturnsEmpty()
        {
            using var context = CreateContext();
            var subject = await GivenFiveConsecutiveDays(context);

            var series = subject.GetSeries(1, OwnerType.Team, ProcessBehaviorMetricType.Throughput, TargetDay.AddDays(-40), TargetDay.AddDays(-30));

            Assert.That(series, Is.Empty);
        }

        private async Task<ProcessBehaviorSnapshotRepository> GivenFiveConsecutiveDays(LighthouseAppContext context)
        {
            var subject = CreateSubject(context);

            for (var dayOffset = 0; dayOffset < 5; dayOffset++)
            {
                subject.Add(Snapshot(1, OwnerType.Team, TargetDay.AddDays(dayOffset), 13, 8, 3));
            }

            await subject.Save();
            return subject;
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
