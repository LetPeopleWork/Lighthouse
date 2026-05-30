using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class TeamDeletedForecastRetriggerHandlerTest
    {
        private Mock<IPortfolioUpdater> portfolioUpdaterMock;

        [SetUp]
        public void Setup()
        {
            portfolioUpdaterMock = new Mock<IPortfolioUpdater>();
        }

        [Test]
        public async Task HandleAsync_TriggersUpdateForEachAffectedPortfolio()
        {
            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDeleted(42, [3, 7]), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(3), Times.Once);
                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(7), Times.Once);
                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
            }
        }

        [Test]
        public async Task HandleAsync_NoAffectedPortfolios_TriggersNoUpdate()
        {
            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDeleted(42, []), CancellationToken.None);

            portfolioUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private TeamDeletedForecastRetriggerHandler CreateSubject()
        {
            return new TeamDeletedForecastRetriggerHandler(portfolioUpdaterMock.Object);
        }
    }
}
