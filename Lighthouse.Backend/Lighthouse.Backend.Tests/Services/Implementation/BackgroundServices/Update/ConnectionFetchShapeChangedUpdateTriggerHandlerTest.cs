using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class ConnectionFetchShapeChangedUpdateTriggerHandlerTest
    {
        private static readonly string[] EveryTeamThenEveryPortfolio = ["Team 11", "Team 12", "Portfolio 21", "Portfolio 22"];

        private Mock<ITeamUpdater> teamUpdaterMock;
        private Mock<IPortfolioUpdater> portfolioUpdaterMock;
        private List<string> triggered;

        [SetUp]
        public void Setup()
        {
            triggered = [];
            teamUpdaterMock = new Mock<ITeamUpdater>();
            teamUpdaterMock.Setup(x => x.TriggerUpdate(It.IsAny<int>())).Callback<int>(id => triggered.Add($"Team {id}"));
            portfolioUpdaterMock = new Mock<IPortfolioUpdater>();
            portfolioUpdaterMock.Setup(x => x.TriggerUpdate(It.IsAny<int>())).Callback<int>(id => triggered.Add($"Portfolio {id}"));
        }

        [Test]
        public async Task HandleAsync_TriggersEveryTeamThenEveryPortfolioOnce()
        {
            var subject = CreateSubject();

            await subject.HandleAsync(new ConnectionFetchShapeChanged(5, [11, 12], [21, 22]), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(triggered, Is.EqualTo(EveryTeamThenEveryPortfolio));
                teamUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
            }

            teamUpdaterMock.VerifyNoOtherCalls();
            portfolioUpdaterMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task HandleAsync_OnlyPortfolios_TriggersNoTeam()
        {
            var subject = CreateSubject();

            await subject.HandleAsync(new ConnectionFetchShapeChanged(5, [], [21]), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                teamUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(21), Times.Once);
            }
        }

        private ConnectionFetchShapeChangedUpdateTriggerHandler CreateSubject()
        {
            return new ConnectionFetchShapeChangedUpdateTriggerHandler(
                teamUpdaterMock.Object,
                portfolioUpdaterMock.Object,
                Mock.Of<ILogger<ConnectionFetchShapeChangedUpdateTriggerHandler>>());
        }
    }
}
