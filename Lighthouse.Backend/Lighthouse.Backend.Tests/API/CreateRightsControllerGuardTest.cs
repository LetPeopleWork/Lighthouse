using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class CreateRightsControllerGuardTest
    {
        [Test]
        public void CreateTeam_IsGuardedOnCanCreateTeamRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(TeamsController), nameof(TeamsController.CreateTeam));

            Assert.That(attribute, Is.Not.Null, "TeamsController.CreateTeam must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.CanCreateTeam),
                "TeamsController.CreateTeam must be gated on the create-team requirement so users with at least one Team Admin role (direct or group-derived) are admitted.");
        }

        [Test]
        public void CreatePortfolio_IsGuardedOnCanCreatePortfolioRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(PortfoliosController), nameof(PortfoliosController.CreatePortfolio));

            Assert.That(attribute, Is.Not.Null, "PortfoliosController.CreatePortfolio must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.CanCreatePortfolio),
                "PortfoliosController.CreatePortfolio must be gated on the create-portfolio requirement so users with at least one Portfolio Admin role (direct or group-derived) are admitted.");
        }

        [Test]
        public void ValidateTeamSettings_IsGuardedOnCanCreateTeamRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(TeamsController), nameof(TeamsController.ValidateTeamSettings));

            Assert.That(attribute, Is.Not.Null, "TeamsController.ValidateTeamSettings must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.CanCreateTeam),
                "ValidateTeamSettings is the pre-flight for CreateTeam; it must accept the same callers as CreateTeam itself.");
        }

        [Test]
        public void ValidatePortfolioSettings_IsGuardedOnCanCreatePortfolioRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(PortfoliosController), nameof(PortfoliosController.ValidatePortfolioSettings));

            Assert.That(attribute, Is.Not.Null, "PortfoliosController.ValidatePortfolioSettings must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.CanCreatePortfolio),
                "ValidatePortfolioSettings is the pre-flight for CreatePortfolio; it must accept the same callers as CreatePortfolio itself.");
        }

        private static RbacGuardAttribute? GetRbacGuardAttribute(Type controllerType, string methodName)
        {
            return controllerType
                .GetMethod(methodName)?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();
        }
    }
}
