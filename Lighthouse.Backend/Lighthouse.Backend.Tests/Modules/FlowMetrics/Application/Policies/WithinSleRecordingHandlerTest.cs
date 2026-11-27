using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Application.Policies;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application.Policies
{
    [TestFixture]
    public class WithinSleRecordingHandlerTest
    {
        private readonly InvalidOperationException writerFailure = new("the record could not be written");

        private Mock<ITeamMetricsService> teamMetrics = null!;
        private Mock<IPortfolioMetricsService> portfolioMetrics = null!;
        private Mock<IRepository<Team>> teams = null!;
        private Mock<IRepository<Portfolio>> portfolios = null!;
        private Mock<IWithinSleSnapshotWriter> writer = null!;
        private Mock<ILogger<WithinSleRecordingHandler>> logger = null!;

        [SetUp]
        public void SetUp()
        {
            teamMetrics = new Mock<ITeamMetricsService>();
            portfolioMetrics = new Mock<IPortfolioMetricsService>();
            teams = new Mock<IRepository<Team>>();
            portfolios = new Mock<IRepository<Portfolio>>();
            writer = new Mock<IWithinSleSnapshotWriter>();
            logger = new Mock<ILogger<WithinSleRecordingHandler>>();

            writer
                .Setup(w => w.RecordToday(It.IsAny<int>(), It.IsAny<OwnerType>(), It.IsAny<WithinSleFamilyReader>()))
                .ThrowsAsync(writerFailure);
        }

        [Test]
        public async Task TeamRefresh_WhenTheWriterFails_LogsAnErrorDropsTheReadCacheAndLetsTheRefreshFinish()
        {
            var team = new Team { Id = 7, Name = "Team" };
            teams.Setup(repository => repository.GetById(7)).Returns(team);

            await HandlingIsQuiet(() => CreateSubject().HandleAsync(new TeamDataRefreshed(7), CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                ThenTheFailureWasLoggedAsAnError(OwnerType.Team, 7);
                teamMetrics.Verify(metrics => metrics.InvalidateTeamMetrics(team), Times.Once);
            }
        }

        [Test]
        public async Task PortfolioRefresh_WhenTheWriterFails_LogsAnErrorDropsTheReadCacheAndLetsTheRefreshFinish()
        {
            var portfolio = new Portfolio { Id = 9, Name = "Portfolio" };
            portfolios.Setup(repository => repository.GetById(9)).Returns(portfolio);

            await HandlingIsQuiet(() => CreateSubject().HandleAsync(new PortfolioFeaturesRefreshed(9), CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                ThenTheFailureWasLoggedAsAnError(OwnerType.Portfolio, 9);
                portfolioMetrics.Verify(metrics => metrics.InvalidatePortfolioMetrics(portfolio), Times.Once);
            }
        }

        private WithinSleRecordingHandler CreateSubject()
        {
            return new WithinSleRecordingHandler(
                teamMetrics.Object,
                portfolioMetrics.Object,
                teams.Object,
                portfolios.Object,
                writer.Object,
                new FakeLighthouseClock(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero)),
                logger.Object);
        }

        private static async Task HandlingIsQuiet(Func<Task> handle)
        {
            Exception? escaped = null;
            try
            {
                await handle();
            }
            catch (Exception exception)
            {
                escaped = exception;
            }

            Assert.That(escaped, Is.Null, "a failed Within SLE record must not break the refresh that raised the event");
        }

        private void ThenTheFailureWasLoggedAsAnError(OwnerType ownerType, int ownerId)
        {
            logger.Verify(
                log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        ValueOf(state, "MetricFamily") == "WithinSle"
                        && ValueOf(state, "OwnerType") == ownerType.ToString()
                        && ValueOf(state, "OwnerId") == ownerId.ToString()),
                    writerFailure,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static string? ValueOf(object? state, string name)
        {
            return state is IReadOnlyList<KeyValuePair<string, object?>> values
                ? values.FirstOrDefault(pair => pair.Key == name).Value?.ToString()
                : null;
        }
    }
}
