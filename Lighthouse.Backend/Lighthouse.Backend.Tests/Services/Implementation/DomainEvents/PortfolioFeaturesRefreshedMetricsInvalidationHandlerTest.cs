using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    public class PortfolioFeaturesRefreshedMetricsInvalidationHandlerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock;

        [SetUp]
        public void SetUp()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            portfolioMetricsServiceMock = new Mock<IPortfolioMetricsService>();
        }

        [Test]
        public async Task HandleAsync_InvalidatesMetricsForThePortfolioCarriedByTheEvent()
        {
            var portfolio = new Portfolio { Id = 17, Name = "Release 1" };
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject();
            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            portfolioMetricsServiceMock.Verify(x => x.InvalidatePortfolioMetrics(portfolio), Times.Once);
        }

        [Test]
        public async Task HandleAsync_PortfolioNoLongerExists_DoesNotInvalidate()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio?)null);

            var subject = CreateSubject();
            await subject.HandleAsync(new PortfolioFeaturesRefreshed(99), CancellationToken.None);

            portfolioMetricsServiceMock.Verify(x => x.InvalidatePortfolioMetrics(It.IsAny<Portfolio>()), Times.Never);
        }

        private PortfolioFeaturesRefreshedMetricsInvalidationHandler CreateSubject()
        {
            return new PortfolioFeaturesRefreshedMetricsInvalidationHandler(portfolioRepositoryMock.Object, portfolioMetricsServiceMock.Object);
        }
    }
}
