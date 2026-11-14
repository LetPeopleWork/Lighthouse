using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
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

        [Test]
        public async Task AWorkItemAgeDay_IsOneRowUnderNoHorizon_ReadAsOfThatDayAlone()
        {
            var windowsRead = new List<(DateTime Start, DateTime End)>();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.WorkItemAge, Day, (start, end) =>
            {
                windowsRead.Add((start, end));
                return Percentiles(1, 2, 3, 4);
            });
            await repository.Save();

            var rows = StoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(windowsRead, Has.Count.EqualTo(1));
                Assert.That(windowsRead[0].End, Is.EqualTo(DayAtMidnight));
                Assert.That(rows.Select(row => row.Horizon), Is.EqualTo(new int?[] { PercentilesOverTimeSnapshot.NoHorizon }));
                Assert.That(rows[0].RecordedAt, Is.EqualTo(Day));
            }
        }

        [Test]
        public async Task AnAgeDayWithNothingInFlight_GetsNoRow_WhileTheNextDayWithWorkInFlightDoes()
        {
            var nextDay = Day.AddDays(1);

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.WorkItemAge, Day, (_, _) => Percentiles(0, 0, 0, 0));
            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.WorkItemAge, nextDay, (_, _) => Percentiles(3, 3, 3, 3));
            await repository.Save();

            Assert.That(StoredRows().Select(row => row.RecordedAt), Is.EqualTo(new[] { nextDay }));
        }

        /// <summary>
        /// The reader answers from the end of the window it is handed, as the age reader does, so a day
        /// filled in afterwards can only read the same as the day recorded live if it is read as of
        /// that day and not as of today.
        /// </summary>
        [Test]
        public async Task AnAgeDayFilledInAfterwards_ReadsTheSameAsWhenItWasRecordedOnTheDay()
        {
            static List<PercentileValue> AgesAsOf(DateTime end) => Percentiles(end.Day, end.Day + 1, end.Day + 2, end.Day + 3);
            var recordedOnTheDay = new PercentileSnapshotWriter(repository, new FakeLighthouseClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero)));
            recordedOnTheDay.RecordToday(OwnerId, OwnerType.Team, MetricType.WorkItemAge, (_, end) => AgesAsOf(end));
            await repository.Save();
            var asRecorded = StoredRows().Single();
            context.PercentilesOverTimeSnapshots.Remove(context.PercentilesOverTimeSnapshots.Single());
            await context.SaveChangesAsync();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.WorkItemAge, Day, (_, end) => AgesAsOf(end));
            await repository.Save();

            var filledAfterwards = StoredRows().Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(filledAfterwards.RecordedAt, Is.EqualTo(asRecorded.RecordedAt));
                Assert.That(filledAfterwards.Horizon, Is.EqualTo(asRecorded.Horizon));
                Assert.That(
                    new[] { filledAfterwards.P50, filledAfterwards.P70, filledAfterwards.P85, filledAfterwards.P95 },
                    Is.EqualTo(new[] { asRecorded.P50, asRecorded.P70, asRecorded.P85, asRecorded.P95 }));
            }
        }

        /// <summary>
        /// The rule that a quiet day is left blank holds on the path that records today as well as on the
        /// one that fills a past day, or the first of the two to reach a day would settle it for good.
        /// </summary>
        [Test]
        public async Task ATodayOnWhichNothingFinished_GetsNoRowFromTheDailyRecording()
        {
            subject.RecordToday(OwnerId, OwnerType.Team, MetricType.CycleTime, (_, _) => []);
            await repository.Save();

            Assert.That(StoredRows(), Is.Empty,
                "The daily recording wrote a row for a day on which nothing finished. A row of zeros draws a floor the team never stood on, and because a stored day is never rewritten it would stay on the chart.");
        }

        /// <summary>
        /// Every horizon of a day worked out afterwards reads what the recording wrote on that day. Each
        /// reading is built from the start and the end of the window it is asked for, so a day read over
        /// any other window, or as of any other day, comes out different.
        /// </summary>
        [Test]
        public async Task ACycleTimeDayFilledInAfterwards_ReadsTheSameAtEveryHorizonAsWhenItWasRecordedOnTheDay()
        {
            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            List<PercentileValue> ReadingOf(DateTime start, DateTime end)
                => Percentiles((int)(end - epoch).TotalDays, (int)(start - epoch).TotalDays, (end - start).Days, start.Day + end.Day);
            var recordedOnTheDay = new PercentileSnapshotWriter(repository, new FakeLighthouseClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero)));
            recordedOnTheDay.RecordToday(OwnerId, OwnerType.Team, MetricType.CycleTime, ReadingOf);
            await repository.Save();
            var asRecorded = ReadingsOf(StoredRows());
            context.PercentilesOverTimeSnapshots.RemoveRange(context.PercentilesOverTimeSnapshots);
            await context.SaveChangesAsync();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, MetricType.CycleTime, Day, ReadingOf);
            await repository.Save();

            var filledAfterwards = ReadingsOf(StoredRows());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(asRecorded, Has.Count.EqualTo(EveryCycleTimeHorizon.Length),
                    "The recording did not write the three horizons of the day, so there is nothing to hold the filled day to.");
                Assert.That(filledAfterwards, Is.EqualTo(asRecorded),
                    "A day worked out afterwards must read exactly as the day recorded live at every horizon, or the chart draws two different readings as one line.");
            }
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
        public async Task ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed()
        {
            var racing = WriterRacingAnotherCopyThatStored(horizon: 30, refusedHorizon: 30);

            await Assert.DoesNotThrowAsync(racing.SaveFilledDay);
        }

        /// <summary>
        /// The refused row is matched on every part of its natural key, horizon included: another
        /// horizon of the same day being stored does not make this one present.
        /// </summary>
        [Test]
        public async Task ARowRefusedWhileOnlyAnotherHorizonOfThatDayIsStored_IsThrownOn()
        {
            var racing = WriterRacingAnotherCopyThatStored(horizon: 60, refusedHorizon: 30);

            await Assert.ThrowsAsync<DbUpdateException>(racing.SaveFilledDay);
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

        private static List<(int? Horizon, DateOnly RecordedAt, int P50, int P70, int P85, int P95)> ReadingsOf(List<PercentilesOverTimeSnapshot> rows)
            => [.. rows.Select(row => (row.Horizon, row.RecordedAt, row.P50, row.P70, row.P85, row.P95))];

        private static List<PercentileValue> Percentiles(int p50, int p70, int p85, int p95)
            => [new(50, p50), new(70, p70), new(85, p85), new(95, p95)];

        private static FakeLighthouseClock Clock() => new(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero));
    }
}
