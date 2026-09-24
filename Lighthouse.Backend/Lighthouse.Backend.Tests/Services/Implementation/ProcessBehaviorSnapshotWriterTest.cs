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
    /// Filling in a past day of limits: a day already carrying limits keeps them, and a missing day is
    /// read over the family's span ending on that day, as of that day.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class ProcessBehaviorSnapshotWriterTest
    {
        private const int OwnerId = 2;

        private const int LookbackDays = 20;

        private static readonly DateOnly Day = new(2026, 7, 15);

        private static readonly DateTime DayAtMidnight = new(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

        private static readonly ProcessBehaviourChart Limits = new()
        {
            Status = BaselineStatus.Ready,
            Average = 3,
            UpperNaturalProcessLimit = 8,
            LowerNaturalProcessLimit = 1,
        };

        private LighthouseAppContext context = null!;

        private ProcessBehaviorSnapshotRepository repository = null!;

        private ProcessBehaviorSnapshotWriter subject = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
            repository = new ProcessBehaviorSnapshotRepository(context, Mock.Of<ILogger<ProcessBehaviorSnapshotRepository>>());
            subject = WriterOver(repository);
        }

        [TearDown]
        public void TearDown()
        {
            context.Dispose();
        }

        [Test]
        public async Task ADayWithoutLimits_IsReadOverTheSpanEndingOnThatDay_AsOfThatDay()
        {
            (DateTime Start, DateTime End, DateOnly? AsOf)? asked = null;
            var family = new ProcessBehaviorFamilyReader(ProcessBehaviorMetricType.Throughput, LookbackDays, (start, end, asOf) =>
            {
                asked = (start, end, asOf);
                return Limits;
            });

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, family, Day);
            await repository.Save();

            var rows = StoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(asked, Is.EqualTo((DayAtMidnight.AddDays(-LookbackDays), DayAtMidnight, (DateOnly?)Day)));
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows.TrueForAll(row => row.RecordedAt == Day && row.Average == 3 && row.Unpl == 8 && row.Lnpl == 1), Is.True);
            }
        }

        [Test]
        public async Task ADayThatAlreadyCarriesLimits_KeepsWhatWasObserved()
        {
            context.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
            {
                OwnerId = OwnerId,
                OwnerType = OwnerType.Team,
                MetricType = ProcessBehaviorMetricType.Throughput,
                RecordedAt = Day,
                Average = 42,
            });
            await context.SaveChangesAsync();

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, FamilyReading(), Day);
            await repository.Save();

            var rows = StoredRows();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].Average, Is.EqualTo(42));
            }
        }

        [Test]
        public void ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed()
        {
            var racing = WriterRacingAnotherCopyThatStored(ProcessBehaviorMetricType.Throughput);

            Assert.DoesNotThrowAsync(racing.SaveFilledDay);
        }

        [Test]
        public void ARowRefusedWhileOnlyAnotherFamilyOfThatDayIsStored_IsThrownOn()
        {
            var racing = WriterRacingAnotherCopyThatStored(ProcessBehaviorMetricType.Wip);

            Assert.ThrowsAsync<DbUpdateException>(racing.SaveFilledDay);
        }

        private ProcessBehaviorSnapshotWriter WriterRacingAnotherCopyThatStored(ProcessBehaviorMetricType storedFamily)
        {
            var storedByTheOtherCopy = new List<ProcessBehaviorSnapshot>
            {
                new() { OwnerId = OwnerId, OwnerType = OwnerType.Team, MetricType = storedFamily, RecordedAt = Day },
            };
            var refused = context.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
            {
                OwnerId = OwnerId,
                OwnerType = OwnerType.Team,
                MetricType = ProcessBehaviorMetricType.Throughput,
                RecordedAt = Day,
            });

            var saves = 0;
            var racingRepository = new Mock<IProcessBehaviorSnapshotRepository>();
            racingRepository
                .Setup(store => store.GetByPredicate(It.IsAny<Func<ProcessBehaviorSnapshot, bool>>()))
                .Returns((Func<ProcessBehaviorSnapshot, bool> predicate) => storedByTheOtherCopy.Find(row => predicate(row)));
            racingRepository
                .Setup(store => store.Save())
                .Returns(() => ++saves == 1
                    ? Task.FromException(new DbUpdateException("the natural key is already taken", new List<EntityEntry> { refused }))
                    : Task.CompletedTask);

            return WriterOver(racingRepository.Object);
        }

        private List<ProcessBehaviorSnapshot> StoredRows()
            => [.. context.ProcessBehaviorSnapshots.AsNoTracking().Where(row => row.OwnerId == OwnerId)];

        private static ProcessBehaviorFamilyReader FamilyReading()
            => new(ProcessBehaviorMetricType.Throughput, LookbackDays, (_, _, _) => Limits);

        private static ProcessBehaviorSnapshotWriter WriterOver(IProcessBehaviorSnapshotRepository snapshots)
            => new(
                Mock.Of<ITeamMetricsService>(),
                Mock.Of<IPortfolioMetricsService>(),
                snapshots,
                new FakeLighthouseClock(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero)));
    }
}
