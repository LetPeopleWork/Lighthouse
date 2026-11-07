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
        public void ValidateTeamSettings_IsGuardedOnAnyScopedAdminRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(TeamsController), nameof(TeamsController.ValidateTeamSettings));

            Assert.That(attribute, Is.Not.Null, "TeamsController.ValidateTeamSettings must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.AnyScopedAdmin),
                "Validating runs the settings' query against the tracker with the stored credential, so a Viewer must not reach it. The route carries no scope, and both the create flow and a scoped admin's Save call it, so every kind of admin has to be admitted.");
        }

        [Test]
        public void ValidatePortfolioSettings_IsGuardedOnAnyScopedAdminRequirement()
        {
            var attribute = GetRbacGuardAttribute(typeof(PortfoliosController), nameof(PortfoliosController.ValidatePortfolioSettings));

            Assert.That(attribute, Is.Not.Null, "PortfoliosController.ValidatePortfolioSettings must carry an RbacGuard attribute.");
            Assert.That(
                attribute!.Requirement,
                Is.EqualTo(RbacGuardRequirement.AnyScopedAdmin),
                "Validating runs the settings' query against the tracker with the stored credential, so a Viewer must not reach it. The route carries no scope, and both the create flow and a scoped admin's Save call it, so every kind of admin has to be admitted.");
        }

        [Test]
        public void DeleteTeam_IsGuardedOnCanDeleteUnitWithNoScope()
        {
            var attribute = GetRbacGuardAttribute(typeof(TeamController), nameof(TeamController.DeleteTeam));

            AssertGuardedOnCanDeleteUnitWithNoScope(attribute);
        }

        [Test]
        public void DeletePortfolio_IsGuardedOnCanDeleteUnitWithNoScope()
        {
            var attribute = GetRbacGuardAttribute(typeof(PortfolioController), nameof(PortfolioController.DeletePortfolio));

            AssertGuardedOnCanDeleteUnitWithNoScope(attribute);
        }

        private static void AssertGuardedOnCanDeleteUnitWithNoScope(RbacGuardAttribute? attribute)
        {
            Assert.That(attribute, Is.Not.Null, "A delete action must carry an RbacGuard attribute.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    attribute!.Requirement,
                    Is.EqualTo(RbacGuardRequirement.CanDeleteUnit),
                    "Only a System Admin deletes a Team or Portfolio, so the write requirement scoped admins hold must not guard it.");
                Assert.That(
                    attribute.ScopeIdRouteKey,
                    Is.Null,
                    "The right to delete does not depend on which Team or Portfolio it is.");
            }
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
