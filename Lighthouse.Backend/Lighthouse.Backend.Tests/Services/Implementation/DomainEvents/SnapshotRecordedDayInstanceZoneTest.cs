using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Bug #5567 - proves the four snapshot recorders key their rows on the INSTANCE calendar day,
    /// and proves it against the DATABASE rather than the change tracker.
    ///
    /// The read-back matters. The global EF value converter
    /// (<c>Data/Converters/UtcDateTimeConverter.cs</c>, applied by convention over
    /// <c>Properties&lt;DateTime&gt;()</c>) is applied to query PARAMETERS as well as to stored
    /// values, so a day that left the handler with <see cref="DateTimeKind.Local"/> would have both
    /// its stored value and its comparison bounds shifted by the same offset. Reads would stay
    /// self-consistent and every in-memory assertion would pass while the stored data was wrong for
    /// every other consumer. Only a fresh context - a real SELECT, no identity-map hit - can see it.
    ///
    /// Every day here is a LITERAL. Re-deriving the production expression would make these
    /// assertions hold for every possible value of "today", which is root cause D of this bug.
    /// </summary>
    [TestFixture]
    [Category("bug-5567-instance-day-anchor")]
    public class SnapshotRecordedDayInstanceZoneTest
    {
        /// <summary>
        /// 23:30 UTC. Europe/Zurich is UTC+1 on this date (DST starts 2026-03-29), so the instance
        /// is already on the NEXT calendar day while UTC is still on the previous one. That two-hour
        /// nightly window is where the whole bug lives.
        /// </summary>
        private static readonly DateTimeOffset BoundaryInstant = new(2026, 3, 17, 23, 30, 0, TimeSpan.Zero);

        private static readonly DateOnly InstanceDay = new(2026, 3, 18);

        private static readonly DateOnly UtcDay = new(2026, 3, 17);

        private const int TeamId = 7;

        private const int FirstRunBlockedCount = 2;

        private const int SecondRunBlockedCount = 1;

        private const int PreUpgradeBlockedCount = 9;

        private static readonly int[] AllHorizons = [30, 60, 90];

        private FakeLighthouseClock clock = null!;

        private string databaseFileName = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock = null!;
        private Mock<IRepository<Team>> teamRepositoryMock = null!;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<IBlockedItemService> blockedItemServiceMock = null!;

        private Team team = null!;

        [SetUp]
        public void SetUp()
        {
            var instanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");
            clock = new FakeLighthouseClock(BoundaryInstant, instanceZone);

            databaseFileName = $"Bug5567_{Path.GetRandomFileName().Replace(".", string.Empty)}.db";
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite($"DataSource={databaseFileName};Pooling=False")
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();

            using var context = CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            portfolioMetricsServiceMock = new Mock<IPortfolioMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            blockedItemServiceMock = new Mock<IBlockedItemService>();

            team = CreateTeam();
            teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);
        }

        [TearDown]
        public void TearDown()
        {
            using (var context = CreateContext())
            {
                context.Database.EnsureDeleted();
            }

            SqliteConnection.ClearAllPools();

            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }
        }

        [Test]
        public async Task RecordedDay_AtAZoneBoundary_IsPersistedAsTheInstanceDay()
        {
            await RecordBlockedCount(FirstRunBlockedCount);
            await RecordPercentiles(p50: 3, p70: 5, p85: 7, p95: 9);
            await RecordProcessBehaviour(unpl: 14, average: 9, lnpl: 4);
            var deliveryDay = await RecordDeliveryMetricsAndReadBackTheDay();

            using var context = CreateContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    context.BlockedCountSnapshots.Single().RecordedAt,
                    Is.EqualTo(InstanceDay),
                    "Blocked count: the recorded day must be the instance day, not the UTC day.");
                Assert.That(
                    context.PercentilesOverTimeSnapshots.Select(snapshot => snapshot.RecordedAt).Distinct().Single(),
                    Is.EqualTo(InstanceDay),
                    "Percentiles over time: the recorded day must be the instance day, not the UTC day.");
                Assert.That(
                    context.ProcessBehaviorSnapshots.Select(snapshot => snapshot.RecordedAt).Distinct().Single(),
                    Is.EqualTo(InstanceDay),
                    "Process behaviour: the recorded day must be the instance day, not the UTC day.");
                Assert.That(
                    deliveryDay,
                    Is.EqualTo(InstanceDay),
                    "Delivery metrics: the recorded day must be the instance day, not the UTC day.");
                Assert.That(
                    InstanceDay,
                    Is.Not.EqualTo(UtcDay),
                    "If the two days were equal this fixture would pass on the unfixed code and prove nothing.");
            }
        }

        [Test]
        public async Task PersistedRecordedDay_ReadBackThroughEf_IsNotShiftedByTheGlobalConverter()
        {
            await RecordBlockedCount(FirstRunBlockedCount);
            await RecordPercentiles(p50: 3, p70: 5, p85: 7, p95: 9);
            await RecordProcessBehaviour(unpl: 14, average: 9, lnpl: 4);

            using var context = CreateContext();

            // Querying BY the day key is the half the change tracker cannot fake: EF applies value
            // converters to query parameters too, so a shifted parameter simply would not match.
            var blockedRows = await context.BlockedCountSnapshots
                .CountAsync(snapshot => snapshot.RecordedAt == InstanceDay);
            var percentileRows = await context.PercentilesOverTimeSnapshots
                .CountAsync(snapshot => snapshot.RecordedAt == InstanceDay);
            var processBehaviourRows = await context.ProcessBehaviorSnapshots
                .CountAsync(snapshot => snapshot.RecordedAt == InstanceDay);

            var legacyInstant = await DeliveryLegacyRecordedInstant();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blockedRows, Is.EqualTo(1));
                Assert.That(percentileRows, Is.EqualTo(AllHorizons.Length + 1), "three cycle-time horizons plus the horizon-less work item age row");
                Assert.That(processBehaviourRows, Is.EqualTo(1));

                // The one DateTime day column left on this path. Kind is load-bearing: a Local kind
                // here is exactly the leak the converter would silently shift on the next write.
                Assert.That(legacyInstant.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(DateOnly.FromDateTime(legacyInstant), Is.EqualTo(InstanceDay));
                Assert.That(legacyInstant.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
            }
        }

        [Test]
        public async Task SameDayRerun_UpsertsInPlace_AndDoesNotCreateASecondRow()
        {
            await RecordBlockedCount(FirstRunBlockedCount);
            await RecordPercentiles(p50: 3, p70: 5, p85: 7, p95: 9);
            await RecordProcessBehaviour(unpl: 14, average: 9, lnpl: 4);

            await RecordBlockedCount(SecondRunBlockedCount);
            await RecordPercentiles(p50: 4, p70: 6, p85: 8, p95: 10);
            await RecordProcessBehaviour(unpl: 21, average: 13, lnpl: 5);

            using var context = CreateContext();
            var blocked = await context.BlockedCountSnapshots.ToListAsync();
            var percentiles = await context.PercentilesOverTimeSnapshots
                .Where(snapshot => snapshot.MetricType == MetricType.CycleTime && snapshot.Horizon == 30)
                .ToListAsync();
            var processBehaviour = await context.ProcessBehaviorSnapshots
                .Where(snapshot => snapshot.MetricType == ProcessBehaviorMetricType.Throughput)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blocked, Has.Count.EqualTo(1), "Blocked count: the rerun must upsert in place.");
                Assert.That(blocked[0].BlockedCount, Is.EqualTo(SecondRunBlockedCount));

                Assert.That(percentiles, Has.Count.EqualTo(1), "Percentiles over time: the rerun must upsert in place.");
                Assert.That(percentiles[0].P50, Is.EqualTo(4));

                Assert.That(processBehaviour, Has.Count.EqualTo(1), "Process behaviour: the rerun must upsert in place.");
                Assert.That(processBehaviour[0].Average, Is.EqualTo(13));
            }
        }

        /// <summary>
        /// The upgrade artifact, pinned. An instance that recorded under the old UTC anchor already
        /// holds a row on the UTC day. The first post-upgrade run inside the boundary window writes
        /// the NEXT day's key, so the reading taken on that evening lands one day further along the
        /// series and the pre-upgrade row is left exactly as it was.
        ///
        /// The artifact is therefore a one-day SHIFT, not a skipped or lost row: the series stays
        /// contiguous, no row is overwritten, no row disappears, and the evening reading is filed
        /// under the day the instance was actually on when it was taken. That is the release-note line.
        /// </summary>
        [Test]
        public async Task UpgradeFromTheUtcAnchor_InsideTheBoundaryWindow_ShiftsTheDayForwardAndKeepsThePreUpgradeRow()
        {
            using (var seedContext = CreateContext())
            {
                seedContext.BlockedCountSnapshots.Add(new BlockedCountSnapshot
                {
                    OwnerId = TeamId,
                    OwnerType = OwnerType.Team,
                    RecordedAt = UtcDay,
                    BlockedCount = PreUpgradeBlockedCount,
                });
                await seedContext.SaveChangesAsync();
            }

            await RecordBlockedCount(FirstRunBlockedCount);

            using var context = CreateContext();
            var series = await context.BlockedCountSnapshots
                .OrderBy(snapshot => snapshot.RecordedAt)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(series, Has.Count.EqualTo(2), "No row is lost and none is duplicated at upgrade.");
                Assert.That(series[0].RecordedAt, Is.EqualTo(UtcDay));
                Assert.That(series[0].BlockedCount, Is.EqualTo(PreUpgradeBlockedCount), "The pre-upgrade row keeps its value.");
                Assert.That(series[1].RecordedAt, Is.EqualTo(InstanceDay), "The post-upgrade reading is filed one day further along.");
                Assert.That(series[1].BlockedCount, Is.EqualTo(FirstRunBlockedCount));
            }
        }

        private async Task RecordBlockedCount(int blockedCount)
        {
            var items = Enumerable.Range(0, blockedCount)
                .Select(index => new WorkItem { ReferenceId = $"ITEM-{index}" })
                .ToList();

            teamMetricsServiceMock
                .Setup(service => service.GetBlockedEligibleItemsForTeam(team))
                .Returns(items);
            blockedItemServiceMock
                .Setup(service => service.IsBlocked(It.IsAny<WorkItem>(), It.IsAny<Team>()))
                .Returns(true);

            using var context = CreateContext();
            var handler = new BlockedCountSnapshotRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                blockedItemServiceMock.Object,
                new BlockedCountSnapshotRepository(context, Mock.Of<ILogger<BlockedCountSnapshotRepository>>()),
                clock,
                Mock.Of<ILogger<BlockedCountSnapshotRecordingHandler>>());

            await handler.HandleAsync(new TeamDataRefreshed(TeamId), CancellationToken.None);
        }

        private async Task RecordPercentiles(int p50, int p70, int p85, int p95)
        {
            List<PercentileValue> percentiles =
            [
                new PercentileValue(50, p50),
                new PercentileValue(70, p70),
                new PercentileValue(85, p85),
                new PercentileValue(95, p95),
            ];

            teamMetricsServiceMock
                .Setup(service => service.GetCycleTimePercentilesForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(percentiles);
            teamMetricsServiceMock
                .Setup(service => service.GetWorkItemAgePercentilesForTeam(team, It.IsAny<DateTime>()))
                .Returns(percentiles);

            using var context = CreateContext();
            var handler = new PercentilesOverTimeRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                new PercentilesOverTimeSnapshotRepository(context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>()),
                clock,
                Mock.Of<ILogger<PercentilesOverTimeRecordingHandler>>());

            await handler.HandleAsync(new TeamDataRefreshed(TeamId), CancellationToken.None);
        }

        private async Task RecordProcessBehaviour(int unpl, int average, int lnpl)
        {
            var chart = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = average,
                UpperNaturalProcessLimit = unpl,
                LowerNaturalProcessLimit = lnpl,
            };

            teamMetricsServiceMock
                .Setup(service => service.GetThroughputProcessBehaviourChart(team, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(chart);

            using var context = CreateContext();
            var handler = new ProcessBehaviorRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                new ProcessBehaviorSnapshotRepository(context, Mock.Of<ILogger<ProcessBehaviorSnapshotRepository>>()),
                clock,
                Mock.Of<ILogger<ProcessBehaviorRecordingHandler>>());

            await handler.HandleAsync(new TeamDataRefreshed(TeamId), CancellationToken.None);
        }

        private Task<DateOnly> RecordDeliveryMetricsAndReadBackTheDay()
        {
            return RecordDeliveryMetricsAndReadBack(snapshot => snapshot.RecordedDay);
        }

        private Task<DateTime> DeliveryLegacyRecordedInstant()
        {
            return RecordDeliveryMetricsAndReadBack(snapshot => snapshot.RecordedAt);
        }

        /// <summary>
        /// The delivery family runs through the real DI graph because its snapshot row carries a
        /// foreign key to a delivery. The projection is taken from a context resolved AFTER the
        /// handler completed, in a second scope, so the value comes from a SELECT rather than from
        /// the writing context's identity map.
        /// </summary>
        private async Task<T> RecordDeliveryMetricsAndReadBack<T>(Func<DeliveryMetricSnapshot, T> readColumn)
        {
            using var factory = new TestWebApplicationFactory<Program>();
            using var clockedFactory = factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILighthouseClock>();
                    services.AddSingleton<ILighthouseClock>(clock);
                }));

            using var writeScope = clockedFactory.Services.CreateScope();
            var writeContext = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            writeContext.Database.EnsureDeleted();
            writeContext.Database.EnsureCreated();

            var portfolioId = await SeedPortfolioWithDelivery(writeScope);

            var handler = writeScope.ServiceProvider.GetRequiredService<IDomainEventHandler<PortfolioForecastsUpdated>>();
            await handler.HandleAsync(new PortfolioForecastsUpdated(portfolioId), CancellationToken.None);

            using var readScope = clockedFactory.Services.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var value = readColumn(await readContext.DeliveryMetricSnapshots.SingleAsync());

            writeContext.Database.EnsureDeleted();
            return value;
        }

        private async Task<int> SeedPortfolioWithDelivery(IServiceScope serviceScope)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var seededTeam = new Team { Name = "Test Team", WorkTrackingSystemConnection = connection };
            var teamRepository = serviceScope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(seededTeam);
            await teamRepository.Save();

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            var portfolioRepository = serviceScope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var feature = new Feature([(seededTeam, 6, 10)]) { Name = "Feature", Order = "1" };
            var featureRepository = serviceScope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            // Delivery's constructor refuses a target date in the past against the real wall clock,
            // so the target date is deliberately NOT clock-relative here. It is irrelevant to the
            // subject of this fixture, which is the recorded DAY KEY, not the delivery date.
            var delivery = new Delivery("Release 1", DateTime.UtcNow.AddDays(30), portfolio.Id, TestToday.Ambient);
            delivery.Features.Add(feature);
            var deliveryRepository = serviceScope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return portfolio.Id;
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private static Team CreateTeam()
        {
            return new Team
            {
                Id = TeamId,
                Name = "Test Team",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };
        }
    }
}
