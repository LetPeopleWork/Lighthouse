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
        public async Task ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed()
        {
            var racing = WriterRacingAnotherCopyThatStored(ProcessBehaviorMetricType.Throughput);

            await Assert.DoesNotThrowAsync(racing.SaveFilledDay);
        }

        [Test]
        public async Task ARowRefusedWhileOnlyAnotherFamilyOfThatDayIsStored_IsThrownOn()
        {
            var racing = WriterRacingAnotherCopyThatStored(ProcessBehaviorMetricType.Wip);

            await Assert.ThrowsAsync<DbUpdateException>(racing.SaveFilledDay);
        }

        [Test]
        public void ATeam_ReportsExactlyThroughputWorkItemAgeWipCycleTimeAndArrivals()
        {
            var families = subject.FamiliesFor(new Team { ThroughputHistory = 30 });

            Assert.That(
                families.Select(family => family.MetricType),
                Is.EquivalentTo(new[]
                {
                    ProcessBehaviorMetricType.Throughput,
                    ProcessBehaviorMetricType.WorkItemAge,
                    ProcessBehaviorMetricType.Wip,
                    ProcessBehaviorMetricType.CycleTime,
                    ProcessBehaviorMetricType.Arrivals,
                }));
        }

        [Test]
        public void APortfolio_ReportsExactlyThroughputWorkItemAgeWipCycleTimeArrivalsAndFeatureSize()
        {
            var families = subject.FamiliesFor(new Portfolio());

            Assert.That(
                families.Select(family => family.MetricType),
                Is.EquivalentTo(new[]
                {
                    ProcessBehaviorMetricType.Throughput,
                    ProcessBehaviorMetricType.WorkItemAge,
                    ProcessBehaviorMetricType.Wip,
                    ProcessBehaviorMetricType.CycleTime,
                    ProcessBehaviorMetricType.Arrivals,
                    ProcessBehaviorMetricType.FeatureSize,
                }));
        }

        [Test]
        public void HowBigDeliveriesAreGetting_IsAFamilyOfAPortfolioAndNeverOfATeam()
        {
            var ofATeam = subject.FamiliesFor(new Team { ThroughputHistory = 30 }).Select(family => family.MetricType);
            var ofAPortfolio = subject.FamiliesFor(new Portfolio()).Select(family => family.MetricType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ofATeam, Does.Not.Contain(ProcessBehaviorMetricType.FeatureSize));
                Assert.That(ofAPortfolio, Does.Contain(ProcessBehaviorMetricType.FeatureSize));
            }
        }

        [Test]
        public async Task ADayWhoseLimitsAreNotReady_GetsNoRow_EvenWhenTheChartCarriesABand()
        {
            var notReady = new ProcessBehaviourChart
            {
                Status = BaselineStatus.BaselineInvalid,
                Average = 3,
                UpperNaturalProcessLimit = 8,
                LowerNaturalProcessLimit = 1,
            };

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, FamilyReading(notReady), Day);
            await repository.Save();

            Assert.That(StoredRows(), Is.Empty);
        }

        [Test]
        public async Task ADayWhoseReadyLimitsAreAFlatZeroBand_GetsNoRow()
        {
            var flatZero = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                Average = 0,
                UpperNaturalProcessLimit = 0,
                LowerNaturalProcessLimit = 0,
            };

            subject.FillDayIfAbsent(OwnerId, OwnerType.Team, FamilyReading(flatZero), Day);
            await repository.Save();

            Assert.That(StoredRows(), Is.Empty);
        }

        /// <summary>
        /// The watched day is worked out in a second writer whose own today is later, as it is when a
        /// day is filled in afterwards. Its answer is a function of the window and the day it is judged
        /// as of, so any other stretch, or judging as of today, reads differently.
        /// </summary>
        [Test]
        public async Task ADayFilledInAfterwards_ReadsTheSameAsWhenItWasWatched()
        {
            var laterToday = Day.AddDays(25);
            var watchedOnTheDay = WriterOver(repository, ClockOn(Day));
            watchedOnTheDay.RecordToday(OwnerId, OwnerType.Team, LimitsAsOf(Day));
            await repository.Save();
            var asWatched = StoredRows().Single();
            context.ProcessBehaviorSnapshots.Remove(context.ProcessBehaviorSnapshots.Single(row => row.OwnerId == OwnerId));
            await context.SaveChangesAsync();

            var filledLater = WriterOver(repository, ClockOn(laterToday));
            filledLater.FillDayIfAbsent(OwnerId, OwnerType.Team, LimitsAsOf(laterToday), Day);
            await repository.Save();
            var filled = StoredRows().Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(filled.Average, Is.EqualTo(asWatched.Average));
                Assert.That(filled.Unpl, Is.EqualTo(asWatched.Unpl));
                Assert.That(filled.Lnpl, Is.EqualTo(asWatched.Lnpl));
            }
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

        private static ProcessBehaviorFamilyReader FamilyReading(ProcessBehaviourChart chart)
            => new(ProcessBehaviorMetricType.Throughput, LookbackDays, (_, _, _) => chart);

        // Stands in for the metrics service: a null day means the service's own today, and the triple
        // is worked out from the window and that day alone.
        private static ProcessBehaviorFamilyReader LimitsAsOf(DateOnly serviceToday)
            => new(ProcessBehaviorMetricType.Throughput, LookbackDays, (start, end, asOf) =>
            {
                var judgedOn = asOf ?? serviceToday;
                return new ProcessBehaviourChart
                {
                    Status = BaselineStatus.Ready,
                    Average = DayNumber(end) + DayNumber(judgedOn),
                    UpperNaturalProcessLimit = DayNumber(start),
                    LowerNaturalProcessLimit = DayNumber(end),
                };
            });

        private static int DayNumber(DateTime day) => DateOnly.FromDateTime(day).DayNumber;

        private static int DayNumber(DateOnly day) => day.DayNumber;

        private static FakeLighthouseClock ClockOn(DateOnly day)
            => new(new DateTimeOffset(day.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero));

        private static ProcessBehaviorSnapshotWriter WriterOver(IProcessBehaviorSnapshotRepository snapshots)
            => WriterOver(snapshots, new FakeLighthouseClock(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero)));

        private static ProcessBehaviorSnapshotWriter WriterOver(IProcessBehaviorSnapshotRepository snapshots, FakeLighthouseClock clock)
            => new(
                Mock.Of<ITeamMetricsService>(),
                Mock.Of<IPortfolioMetricsService>(),
                snapshots,
                clock);
    }
}
