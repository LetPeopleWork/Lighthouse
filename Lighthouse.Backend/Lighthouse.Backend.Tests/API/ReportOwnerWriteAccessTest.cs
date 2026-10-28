using System.Security.Claims;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class ReportOwnerWriteAccessTest
    {
        private readonly ClaimsPrincipal caller = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "coach")], "test"));

        private Mock<IRbacAdministrationService> rbac = null!;

        [SetUp]
        public void SetUp()
        {
            rbac = new Mock<IRbacAdministrationService>(MockBehavior.Strict);
            rbac.Setup(service => service.CanWriteTeamAsync(caller, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            rbac.Setup(service => service.CanWritePortfolioAsync(caller, 7, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        }

        [Test]
        public async Task A_Team_is_answered_by_the_callers_right_to_write_that_Team()
        {
            var mayWrite = await new ReportOwnerWriteAccess(rbac.Object, caller).MayWriteAsync(ReportOwner.Team(7));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mayWrite, Is.True);
                rbac.Verify(service => service.CanWriteTeamAsync(caller, 7, It.IsAny<CancellationToken>()), Times.Once);
                rbac.VerifyNoOtherCalls();
            }
        }

        [Test]
        public async Task A_Portfolio_is_answered_by_the_callers_right_to_write_that_Portfolio()
        {
            var mayWrite = await new ReportOwnerWriteAccess(rbac.Object, caller).MayWriteAsync(ReportOwner.Portfolio(7));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mayWrite, Is.False);
                rbac.Verify(service => service.CanWritePortfolioAsync(caller, 7, It.IsAny<CancellationToken>()), Times.Once);
                rbac.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void An_owner_of_no_known_kind_is_never_answered_as_a_Portfolio()
        {
            var access = new ReportOwnerWriteAccess(rbac.Object, caller);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => access.MayWriteAsync(new ReportOwner((ReportOwnerKind)99, 7)), Throws.InstanceOf<ArgumentOutOfRangeException>());
                rbac.VerifyNoOtherCalls();
            }
        }
    }
}
