using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Filling in a past day of percentiles: what is already stored for a day stays, what is missing is
    /// worked out over the window that ends on that day, and a day with nothing to report gets no row.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class PercentileSnapshotWriterTest
    {
        private const int OwnerId = 2;

        private static readonly DateOnly Day = new(2026, 7, 15);

        private static readonly DateTime DayAtMidnight = new(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

        private static readonly int?[] EveryCycleTimeHorizon = [30, 60, 90];

        private static readonly (DateTime Start, DateTime End)[] AWindowPerHorizonEndingOnTheDay =
        [
            (DayAtMidnight.AddDays(-30), DayAtMidnight),
            (DayAtMidnight.AddDays(-60), DayAtMidnight),
            (DayAtMidnight.AddDays(-90), DayAtMidnight),
        ];

        private LighthouseAppContext context = null!;

        private PercentilesOverTimeSnapshotRepository repository = null!;

        private PercentileSnapshotWriter subject = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
            repository = new PercentilesOverTimeSnapshotRepository(context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>());
            subject = new PercentileSnapshotWriter(repository, Clock());
        }

        [TearDown]
        public void TearDown()
        {
            context.Dispose();
        }

        [Test]
        public async Task ADayWithoutRows_IsWorkedOutAtEveryHorizon_OverAWindowEndingOnThatDay()
        {
            var windowsRead = new List<(DateTime Start, DateTime End)>();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.CycleTime, Day, (start, end) =>
            {
                windowsRead.Add((start, end));
                return Percentiles(1, 2, 3, 4);
            });
            await repository.Save();

            var rows = StoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(windowsRead, Is.EqualTo(AWindowPerHorizonEndingOnTheDay));
                Assert.That(rows.Select(row => row.Horizon), Is.EqualTo(EveryCycleTimeHorizon));
                Assert.That(rows.TrueForAll(row => row.RecordedAt == Day && row.P50 == 1 && row.P70 == 2 && row.P85 == 3 && row.P95 == 4), Is.True);
            }
        }

        [Test]
        public async Task AHorizonThatAlreadyHasARow_KeepsWhatWasObserved_WhileTheOtherHorizonsAreFilled()
        {
            context.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = OwnerId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                RecordedAt = Day,
                P50 = 99,
            });
            await context.SaveChangesAsync();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.CycleTime, Day, (_, _) => Percentiles(1, 2, 3, 4));
            await repository.Save();

            var rows = StoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows.Select(row => row.Horizon), Is.EqualTo(EveryCycleTimeHorizon));
                Assert.That(rows[0].P50, Is.EqualTo(99));
            }
        }

        [Test]
        public async Task ADayOnWhichNothingFinished_GetsNoRow()
        {
            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.CycleTime, Day, (_, _) => []);
            await repository.Save();

            Assert.That(StoredRows(), Is.Empty);
        }

        [TestCase(5, 0, 0, 0)]
        [TestCase(0, 5, 0, 0)]
        [TestCase(0, 0, 5, 0)]
        [TestCase(0, 0, 0, 5)]
        public async Task AnyOnePercentileAboveZero_IsAReadingWorthARow(int p50, int p70, int p85, int p95)
        {
            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.CycleTime, Day, (_, _) => Percentiles(p50, p70, p85, p95));
            await repository.Save();

            Assert.That(StoredRows(), Has.Count.EqualTo(EveryCycleTimeHorizon.Length));
        }

        [Test]
        public void ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed()
        {
            var racing = WriterRacingAnotherCopyThatStored(horizon: 30, refusedHorizon: 30);

            Assert.DoesNotThrowAsync(racing.SaveFilledDay);
        }

        /// <summary>
        /// The refused row is matched on every part of its natural key, horizon included: another
        /// horizon of the same day being stored does not make this one present.
        /// </summary>
        [Test]
        public void ARowRefusedWhileOnlyAnotherHorizonOfThatDayIsStored_IsThrownOn()
        {
            var racing = WriterRacingAnotherCopyThatStored(horizon: 60, refusedHorizon: 30);

            Assert.ThrowsAsync<DbUpdateException>(racing.SaveFilledDay);
        }

        private PercentileSnapshotWriter WriterRacingAnotherCopyThatStored(int horizon, int refusedHorizon)
        {
            var storedByTheOtherCopy = new List<PercentilesOverTimeSnapshot>
            {
                new() { OwnerId = OwnerId, OwnerType = OwnerType.Team, MetricType = MetricType.CycleTime, Horizon = horizon, RecordedAt = Day },
            };
            var refused = context.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = OwnerId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = refusedHorizon,
                RecordedAt = Day,
            });

            var saves = 0;
            var racingRepository = new Mock<IPercentilesOverTimeSnapshotRepository>();
            racingRepository
                .Setup(store => store.GetByPredicate(It.IsAny<Func<PercentilesOverTimeSnapshot, bool>>()))
                .Returns((Func<PercentilesOverTimeSnapshot, bool> predicate) => storedByTheOtherCopy.Find(row => predicate(row)));
            racingRepository
                .Setup(store => store.Save())
                .Returns(() => ++saves == 1
                    ? Task.FromException(new DbUpdateException("the natural key is already taken", new List<EntityEntry> { refused }))
                    : Task.CompletedTask);

            return new PercentileSnapshotWriter(racingRepository.Object, Clock());
        }

        private List<PercentilesOverTimeSnapshot> StoredRows()
            => [.. context.PercentilesOverTimeSnapshots.AsNoTracking().Where(row => row.OwnerId == OwnerId).OrderBy(row => row.Horizon)];

        private static List<PercentileValue> Percentiles(int p50, int p70, int p85, int p95)
            => [new(50, p50), new(70, p70), new(85, p85), new(95, p95)];

        private static FakeLighthouseClock Clock() => new(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero));
    }
}
