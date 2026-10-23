using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class ForecastInputsForgottenAtStartSeederTests
    {
        private Mock<IPortfolioRepository> portfolioRepositoryMock;

        [SetUp]
        public void Setup()
        {
            portfolioRepositoryMock = new Mock<IPortfolioRepository>();
            portfolioRepositoryMock.Setup(r => r.ForgetAllForecastInputs(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        [Test]
        public async Task Seed_ForgetsWhatEveryPortfolioRecordedOnce_AndTouchesNothingElse()
        {
            var subject = new ForecastInputsForgottenAtStartSeeder(portfolioRepositoryMock.Object);

            await subject.Seed();

            portfolioRepositoryMock.Verify(r => r.ForgetAllForecastInputs(It.IsAny<CancellationToken>()), Times.Once);
            portfolioRepositoryMock.VerifyNoOtherCalls();
        }
    }
}
