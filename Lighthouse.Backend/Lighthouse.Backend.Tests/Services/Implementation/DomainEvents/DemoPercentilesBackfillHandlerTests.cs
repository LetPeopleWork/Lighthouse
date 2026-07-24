using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class DemoPercentilesBackfillHandlerTests
    {
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>> teamRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock = null!;

        private static readonly int[] ExpectedHorizons = [30, 60, 90];

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();

            teamRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>>();
            portfolioRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>>();
            connectionRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>>();
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private DemoPercentilesBackfillHandler CreateSubject(LighthouseAppContext context)
        {
            var snapshotRepo = new PercentilesOverTimeSnapshotRepository(
                context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>());

            return new DemoPercentilesBackfillHandler(
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                connectionRepositoryMock.Object,
                snapshotRepo,
                Mock.Of<ILogger<DemoPercentilesBackfillHandler>>());
        }

        private void ArrangeConnection(int connectionId, bool isDemo)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionId,
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };

            if (isDemo)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo,
                    Value = bool.TrueString,
                });
            }

            connectionRepositoryMock.Setup(x => x.GetById(connectionId)).Returns(connection);
        }

        private Team ArrangeTeam(int teamId, int connectionId)
        {
            var team = new Team { Id = teamId, Name = $"Team {teamId}", WorkTrackingSystemConnectionId = connectionId };
            teamRepositoryMock.Setup(x => x.GetById(teamId)).Returns(team);
            return team;
        }

        private Portfolio ArrangePortfolio(int portfolioId, int connectionId)
        {
            var portfolio = new Portfolio { Id = portfolioId, Name = $"Portfolio {portfolioId}", WorkTrackingSystemConnectionId = connectionId };
            portfolioRepositoryMock.Setup(x => x.GetById(portfolioId)).Returns(portfolio);
            return portfolio;
        }

        [Test]
        public async Task HandleTeamRefreshed_DemoOwner_BackdatesCycleTimePercentileHistoryForAllHorizons()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            var snapshots = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshots, Has.Count.EqualTo(42), "14-day window x 3 horizons");
                Assert.That(snapshots.All(s => s.MetricType == MetricType.CycleTime), Is.True, "demo backfill is cycle-time only");
                Assert.That(snapshots.All(s => s.RecordedAt < todayDate), Is.True, "backdated rows only; today stays forward-only");
                Assert.That(snapshots.Select(s => s.Horizon).Distinct().OrderBy(h => h), Is.EqualTo(ExpectedHorizons));
                Assert.That(snapshots.Select(s => s.RecordedAt).Distinct().Count(), Is.EqualTo(14), "one row per horizon per day across the window");
                Assert.That(snapshots.All(s => s.P50 <= s.P70 && s.P70 <= s.P85 && s.P85 <= s.P95), Is.True, "percentiles are monotone non-decreasing");
                Assert.That(snapshots.All(s => s.P50 > 0), Is.True, "demo values are plausible, non-zero cycle times");
            }
        }

        [Test]
        public async Task HandlePortfolioRefreshed_DemoOwner_BackdatesCycleTimePercentileHistoryForAllHorizons()
        {
            const int portfolioId = 12;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangePortfolio(portfolioId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolioId), CancellationToken.None);

            var snapshots = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == portfolioId && s.OwnerType == OwnerType.Portfolio)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshots, Has.Count.EqualTo(42), "14-day window x 3 horizons");
                Assert.That(snapshots.All(s => s.MetricType == MetricType.CycleTime), Is.True);
                Assert.That(snapshots.Select(s => s.Horizon).Distinct().OrderBy(h => h), Is.EqualTo(ExpectedHorizons));
            }
        }

        [Test]
        public async Task HandleTeamRefreshed_NonDemoOwner_DoesNothing()
        {
            const int teamId = 7;
            const int connectionId = 55;
            ArrangeConnection(connectionId, isDemo: false);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            Assert.That(context.PercentilesOverTimeSnapshots.ToList(), Is.Empty, "real tenants stay forward-only and are never backdated");
        }

        [Test]
        public async Task HandleTeamRefreshed_AlreadyBackfilled_IsIdempotent()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var seedContext = CreateContext();
            // A prior backdated row means this demo owner's percentile history was already backfilled.
            seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = teamId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                RecordedAt = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                P50 = 5,
                P70 = 7,
                P85 = 9,
                P95 = 11,
            });
            await seedContext.SaveChangesAsync();

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var backdated = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();

            Assert.That(backdated, Has.Count.EqualTo(1), "second run must not add further backdated rows");
        }
    }
}
