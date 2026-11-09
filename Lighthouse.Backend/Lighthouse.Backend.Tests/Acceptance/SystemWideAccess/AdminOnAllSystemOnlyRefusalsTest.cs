using Lighthouse.Backend.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A member of a group with Admin on all changes every Team and Portfolio, and still cannot do anything that stays
    /// with System Admins: administer access, create, clone or delete a unit, edit connections or system settings,
    /// install an update, update all Teams, see who the licence belongs to, or turn everyone signed in on. Each refusal
    /// is asked only once her admin reach is shown to be in force, so a refusal cannot pass because the rule is missing.
    /// </summary>
    [TestFixture]
    public class AdminOnAllSystemOnlyRefusalsTest
    {
        private AdminOnAllScenario scenario = null!;

        [SetUp]
        public async Task GiveTheScenarioItsOwnDatabaseAndTheRule()
        {
            scenario = new AdminOnAllScenario();
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
        }

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [TestCase(RbacGuardRequirement.SystemAdmin, "edit connections, system settings and terminology, install an update or update all Teams")]
        [TestCase(RbacGuardRequirement.CanCreateTeam, "create or clone a Team")]
        [TestCase(RbacGuardRequirement.CanCreatePortfolio, "create or clone a Portfolio")]
        [TestCase(RbacGuardRequirement.CanDeleteUnit, "delete a Team or Portfolio")]
        [TestCase(RbacGuardRequirement.SystemAdminOrBootstrap, "import or clear the licence")]
        public async Task A_member_with_Admin_on_all_is_refused_what_stays_with_System_Admins(RbacGuardRequirement requirement, string whatSheAsks)
        {
            await HerAdminReachIsInForce();

            var allowed = await scenario.SatisfiesTheRequirement(AdminOnAllScenario.Ines_SignedIn(), requirement, null);

            Assert.That(allowed, Is.False, $"WHAT: a member whose only access is Admin on all may {whatSheAsks}. WHY: Admin on all is Team and Portfolio Admin on every unit and never the System role, so this stays with System Admins. HOW: answer this check from the System role alone, which no rule gives.");
        }

        [Test]
        public async Task A_member_with_Admin_on_all_does_not_administer_access_or_see_the_licence_owner()
        {
            await HerAdminReachIsInForce();

            var managesAccess = await scenario.CanManageAccess(AdminOnAllScenario.Ines_SignedIn());
            var seesLicenceOwner = await scenario.SeesTheLicenceOwner(AdminOnAllScenario.Ines_SignedIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(managesAccess, Is.False, "WHAT: a member with Admin on all may manage Settings > Access, its users and all mappings. WHY: instance users and all mappings stay with System Admins. HOW: resolve the System role from the System row alone.");
                Assert.That(seesLicenceOwner, Is.False, "WHAT: a member with Admin on all is shown who the licence belongs to. WHY: the licensee's details stay with System Admins. HOW: as above.");
            }
        }

        [Test]
        public async Task A_member_with_Admin_on_all_cannot_turn_everyone_signed_in_on()
        {
            await HerAdminReachIsInForce();

            var saved = await scenario.SomeoneTurnsEveryoneSignedIn(AdminOnAllScenario.Ines_SignedIn(), true);

            var isOn = await scenario.EveryoneSignedInIsOn();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.Succeeded, Is.False, "WHAT: a member with Admin on all turned everyone signed in on. WHY: only a System Admin changes system-wide rules or the switch. HOW: refuse the save unless the caller is a System Admin.");
                Assert.That(isOn, Is.False, "WHAT: the switch is on after a refused save. WHY: a refusal changes nothing. HOW: refuse before saving.");
            }
        }

        [Test]
        public async Task Admin_on_all_is_neither_a_member_role_on_a_Team_nor_a_key_limit()
        {
            await HerAdminReachIsInForce();

            var madeMember = await scenario.SomeoneMakesMorgan(AdminOnAllScenario.AdminOnAll, scenario.TeamZenith);
            var keyOnAllUnits = await scenario.AsksForAKey(AdminOnAllScenario.Ines_SignedIn(), AdminOnAllScenario.AdminOnAll, AdminOnAllScenario.AllUnits, null);
            var keyOnATeam = await scenario.AsksForAKey(AdminOnAllScenario.Ines_SignedIn(), AdminOnAllScenario.AdminOnAll, PermissionScopeType.Team, scenario.TeamZenith);
            var morganReads = await scenario.CanReadTeam(AdminOnAllScenario.Morgan_SignedIn(), scenario.TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(madeMember.Succeeded, Is.False, "WHAT: Admin on all was given to someone as a member of Team Zenith. WHY: a member holds Team Admin or Viewer on one unit; Admin on all is only a group rule a System Admin adds. HOW: keep the member writers' allow-list of roles.");
                Assert.That(morganReads, Is.False, "WHAT: the refused member role still let Morgan read Team Zenith. WHY: a refusal writes nothing. HOW: refuse before saving.");
                Assert.That(keyOnAllUnits?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest), "WHAT: a key limited to Admin on all was not refused as invalid. WHY: a key is limited per unit or not at all, never to all units. HOW: keep the key's allow-list of role and scope pairs.");
                Assert.That(keyOnATeam?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest), "WHAT: a key limited to Admin on all on one Team was not refused as invalid. WHY: Admin on all names no unit. HOW: as above.");
            }
        }

        private async Task HerAdminReachIsInForce()
        {
            var writesZenith = await scenario.CanWriteTeam(AdminOnAllScenario.Ines_SignedIn(), scenario.TeamZenith);
            Assert.That(writesZenith, Is.True, "WHAT: the member of team-admins cannot change Team Zenith, so the refusal below would prove nothing. WHY: Admin on all is Team Admin on every Team. HOW: read the Admin-on-all reach as the unit's admin role.");
        }
    }
}
