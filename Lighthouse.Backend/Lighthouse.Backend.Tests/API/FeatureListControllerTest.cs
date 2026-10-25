using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListControllerTest
    {
        private Mock<IFeatureListService> serviceMock = null!;
        private FeatureListController subject = null!;

        [SetUp]
        public void SetUp()
        {
            serviceMock = new Mock<IFeatureListService>();
            var rows = new FeatureListRows(Mock.Of<IBlackoutPeriodService>(), Mock.Of<IBlockedItemService>(), new FakeLighthouseClock(DateTimeOffset.UtcNow));
            subject = new FeatureListController(serviceMock.Object, rows);
        }

        [Test]
        public async Task A_Portfolio_that_does_not_exist_has_no_list_and_none_is_read()
        {
            serviceMock.Setup(service => service.PortfolioExists(7)).Returns(false);

            var result = await subject.ForPortfolio(7);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                serviceMock.Verify(service => service.ForPortfolioAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task A_Team_that_does_not_exist_has_no_list_and_none_is_read()
        {
            serviceMock.Setup(service => service.TeamExists(7)).Returns(false);

            var result = await subject.ForTeam(7);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                serviceMock.Verify(service => service.ForTeamAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }
    }
}
