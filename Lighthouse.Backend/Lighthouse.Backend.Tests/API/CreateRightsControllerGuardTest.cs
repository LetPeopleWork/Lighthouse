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
        public void ValidateTeamSettings_HasNoRbacGuardAttribute()
        {
            var attribute = GetRbacGuardAttribute(typeof(TeamsController), nameof(TeamsController.ValidateTeamSettings));

            Assert.That(
                attribute,
                Is.Null,
                "Under v1, ValidateTeamSettings is reachable by any authenticated user. The endpoint is shared by the System-Admin-only Create flow and by the Team-Admin / Portfolio-Admin Edit-and-Save flow; gating it on CanCreateTeam (= sysadmin-only) breaks save-edits for scoped admins. The subsequent write endpoints (POST /teams for create, PUT /teams/{id} for edit) enforce the proper scope.");
        }

        [Test]
        public void ValidatePortfolioSettings_HasNoRbacGuardAttribute()
        {
            var attribute = GetRbacGuardAttribute(typeof(PortfoliosController), nameof(PortfoliosController.ValidatePortfolioSettings));

            Assert.That(
                attribute,
                Is.Null,
                "Under v1, ValidatePortfolioSettings is reachable by any authenticated user. Same reasoning as ValidateTeamSettings — the validate endpoint is shared between Create (sysadmin) and Edit-and-Save (scoped admin) flows; the write endpoints downstream enforce scope.");
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
