using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A System Admin gives one SSO group Admin on every Team and Portfolio with a single rule. Its members change the
    /// settings, reports and members of every unit, including ones created after the rule, through every check that
    /// lets an admin change a unit; a key they limit to one Team stays on that Team; removing the rule ends it on the
    /// next request; and neither the everyone-signed-in switch nor a stored rule of another role gives anyone admin.
    /// </summary>
    [TestFixture]
    public class AdminOnAllAtThePortTest
    {
        private AdminOnAllScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new AdminOnAllScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [Test]
        public async Task A_System_Admin_adds_Admin_on_all_for_a_group_the_rules_list_shows_it_once_and_adding_it_again_is_refused()
        {
            var added = await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var addedAgain = await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);

            var lines = await scenario.TheRulesList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.Succeeded, Is.True, $"WHAT: adding Admin on all for team-admins was refused ({added.Message}). WHY: a System Admin gives a group admin on every Team and Portfolio with this one rule. HOW: accept the Admin-on-all role with the AllUnits scope and no unit.");
                Assert.That(
                    lines.Select(line => (line.GroupValue, line.Role, line.ScopeType, line.ScopeId)),
                    Is.EqualTo(new[] { (AdminOnAllScenario.TeamAdmins, AdminOnAllScenario.AdminOnAll, AdminOnAllScenario.AllUnits, (int?)null) }),
                    "WHAT: the rules list does not show exactly one line Admin on all for team-admins. WHY: the System Admin must see the rule they added, once. HOW: store the rule as a group mapping on AllUnits and list it.");
                Assert.That(addedAgain.Succeeded, Is.False, "WHAT: adding the same Admin-on-all rule twice was accepted. WHY: the System Admin must learn the group already has that role. HOW: refuse an equal row on create.");
                Assert.That(addedAgain.Message, Is.EqualTo(AdminOnAllScenario.DuplicateMessage), "WHAT: the refusal does not say the group already has the role. WHY: this is the message the page shows. HOW: answer the existing duplicate message.");
            }
        }

        [TestCaseSource(nameof(PairsAnAdminOnAllRuleMayNotTake))]
        public async Task Admin_on_all_applies_only_to_every_Team_and_Portfolio_and_no_other_role_applies_to_all_of_them_but_Viewer(string role, PermissionScopeType scope, int? unit)
        {
            var result = await scenario.AlexAddsAMapping("any-group", AdminOnAllScenario.RoleNamed(role), scope, unit);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False, $"WHAT: {role} on {scope} with unit {unit?.ToString(CultureInfo.InvariantCulture) ?? "none"} was accepted. WHY: Admin on all reaches every unit and nothing else, and a rule for all units gives Viewer or Admin on all only. HOW: accept Admin on all only with AllUnits and no unit, and AllUnits only with Viewer or Admin on all.");
                Assert.That(scenario.StoredRowsFor("any-group"), Is.Zero, "WHAT: a refused rule was stored. WHY: a refusal writes no row. HOW: refuse before saving.");
            }
        }

        private static IEnumerable<TestCaseData> PairsAnAdminOnAllRuleMayNotTake()
        {
            yield return new TestCaseData("UnitAdmin", PermissionScopeType.System, null).SetName("Admin on all on the System is refused");
            yield return new TestCaseData("UnitAdmin", PermissionScopeType.Team, AdminOnAllScenario.AnyUnitNumber).SetName("Admin on all on one Team is refused");
            yield return new TestCaseData("UnitAdmin", PermissionScopeType.Portfolio, AdminOnAllScenario.AnyUnitNumber).SetName("Admin on all on one Portfolio is refused");
            yield return new TestCaseData("UnitAdmin", PermissionScopeType.AllUnits, AdminOnAllScenario.AnyUnitNumber).SetName("Admin on all naming a unit is refused");
            yield return new TestCaseData("SystemAdmin", PermissionScopeType.AllUnits, null).SetName("System Admin on all units is refused");
            yield return new TestCaseData("TeamAdmin", PermissionScopeType.AllUnits, null).SetName("Team Admin on all units is refused");
            yield return new TestCaseData("PortfolioAdmin", PermissionScopeType.AllUnits, null).SetName("Portfolio Admin on all units is refused");
        }

        [Test]
        public async Task A_member_changes_every_Team_and_Portfolio_including_a_Team_created_after_the_rule()
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var meridian = scenario.ATeamIsCreated("Team Meridian");

            var reach = await scenario.WhatInesMayDo(meridian);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reach.WritesZenith, Is.True, "WHAT: a member of team-admins, with no role on any unit, cannot change Team Zenith. WHY: Admin on all is Team Admin on every Team. HOW: read the Admin-on-all reach as Team Admin on a Team.");
                Assert.That(reach.WritesLaterTeam, Is.True, "WHAT: the member cannot change a Team created after the rule. WHY: the rule names no unit, so later units are covered with no further grant. HOW: decide reach from the rule's scope, never from units copied when it was added.");
                Assert.That(reach.WritesPortfolio, Is.True, "WHAT: the member cannot change Portfolio Ocean Explorer. WHY: Admin on all is Portfolio Admin on every Portfolio. HOW: read the Admin-on-all reach as Portfolio Admin on a Portfolio.");
                Assert.That(reach.ReadsZenith && reach.ReadsPortfolio, Is.True, "WHAT: the member cannot read the units she administers. WHY: an admin of a unit reads it. HOW: let the read check accept the admin reach.");
            }
        }

        [Test]
        public async Task A_member_adds_someone_as_Viewer_on_a_Team_and_that_person_then_reads_it()
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var mayManageTeam = await scenario.MayManageTheMembersOfTeam(AdminOnAllScenario.Ines_SignedIn(), scenario.TeamZenith);
            var mayManagePortfolio = await scenario.MayManageTheMembersOfPortfolio(AdminOnAllScenario.Ines_SignedIn(), scenario.PortfolioOceanExplorer);

            var added = await scenario.InesMakesMorganAViewerOfTeamZenith();

            var morganReads = await scenario.CanReadTeam(AdminOnAllScenario.Morgan_SignedIn(), scenario.TeamZenith);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(mayManageTeam, Is.True, "WHAT: the member may not manage Team Zenith's members. WHY: Admin on all lets her manage a unit's members on its Access tab, as a Team Admin can. HOW: let the membership check accept the Admin-on-all reach.");
                Assert.That(mayManagePortfolio, Is.True, "WHAT: the member may not manage the Portfolio's members. WHY: Admin on all is Portfolio Admin there. HOW: as above, for Portfolios.");
                Assert.That(added.Succeeded, Is.True, $"WHAT: adding Morgan as Viewer on Team Zenith failed ({added.Message}). WHY: a unit's admin adds members. HOW: keep the member writer as it is.");
                Assert.That(morganReads, Is.True, "WHAT: Morgan cannot read Team Zenith after being added. WHY: the member she was given must work. HOW: keep the member writer as it is.");
            }
        }

        [Test]
        public async Task A_members_summary_lists_every_Team_and_Portfolio_as_administered_and_offers_nothing_a_System_Admin_does()
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var meridian = scenario.ATeamIsCreated("Team Meridian");

            var summary = await scenario.TheSummaryOf(AdminOnAllScenario.Ines_SignedIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.AdminTeamIds, Is.EquivalentTo(new[] { scenario.TeamZenith, scenario.TeamApollo, meridian }), "WHAT: the summary does not list every stored Team as administered. WHY: the browser shows a Team's Settings and Access tabs from this list, the later Team included. HOW: fill the admin lists from the existing units the Admin-on-all reach covers.");
                Assert.That(summary.AdminPortfolioIds, Is.EquivalentTo(new[] { scenario.PortfolioOceanExplorer }), "WHAT: the summary does not list the Portfolio as administered. WHY: the browser shows its Settings tab from this list. HOW: as above, for Portfolios.");
                Assert.That(summary.IsSystemAdmin, Is.False, "WHAT: the summary says System Admin. WHY: Admin on all never gives the System role. HOW: keep the System role apart from rules.");
                Assert.That(summary.CanCreateTeam || summary.CanCreatePortfolio, Is.False, "WHAT: the summary offers creating a Team or Portfolio. WHY: creating stays with System Admins. HOW: keep creation on the System role alone.");
            }
        }

        [TestCaseSource(nameof(EveryCheckThatLetsAnAdminChangeAUnit))]
        public async Task Every_check_that_lets_an_admin_change_a_unit_lets_a_member_through(AdminCheck check)
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var meridian = scenario.ATeamIsCreated("Team Meridian");

            var allowed = await scenario.WhatTheCheckAnswers(check, AdminOnAllScenario.Ines_SignedIn(), meridian);

            Assert.That(allowed, Is.True, $"WHAT: {check} refuses a member of team-admins whose only access is Admin on all. WHY: every check that lets a Team or Portfolio Admin change a unit must agree, or one page or report works and the next refuses. HOW: let {check} read the Admin-on-all reach as the unit's admin role.");
        }

        [Test]
        public async Task An_explicit_Viewer_role_on_a_Team_and_a_Viewer_rule_do_not_narrow_Admin_on_all_there()
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            await scenario.AlexAddsAMapping(AdminOnAllScenario.TeamReaders, UserRole.Viewer, AdminOnAllScenario.AllUnits, null);
            scenario.InesHoldsDirectlyViewerOn(scenario.TeamZenith);

            var writesZenith = await scenario.CanWriteTeam(AdminOnAllScenario.Ines_InBothGroups(), scenario.TeamZenith);
            var summary = await scenario.TheSummaryOf(AdminOnAllScenario.Ines_InBothGroups());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writesZenith, Is.True, "WHAT: a Viewer role held on Team Zenith, or a Viewer rule, took away Admin on all there. WHY: a rule only adds and the higher role wins. HOW: take the higher of the unit role and every reach.");
                Assert.That(summary.AdminTeamIds, Does.Contain(scenario.TeamZenith), "WHAT: the summary does not list Team Zenith as administered. WHY: the browser would hide its Settings tab although she may change it. HOW: fill the admin lists from the same higher role.");
            }
        }

        [Test]
        public async Task A_key_limited_to_Team_Admin_on_Team_Zenith_writes_Team_Zenith_and_nothing_else()
        {
            await scenario.AlexAddsAdminOnAllFor(AdminOnAllScenario.TeamAdmins);
            var zenithKey = scenario.InesHasAKeyLimitedToTeamAdminOn(scenario.TeamZenith);

            var mayIssue = await scenario.SatisfiesTheRequirement(AdminOnAllScenario.Ines_SignedIn(), RbacGuardRequirement.TeamWrite, scenario.TeamZenith);
            var writesZenith = await scenario.CanWriteTeam(zenithKey, scenario.TeamZenith);
            var writesApollo = await scenario.CanWriteTeam(zenithKey, scenario.TeamApollo);
            var readsApollo = await scenario.CanReadTeam(zenithKey, scenario.TeamApollo);
            var writesPortfolio = await scenario.CanWritePortfolio(zenithKey, scenario.PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mayIssue, Is.True, "WHAT: a member of team-admins may not issue a key limited to Team Admin on Team Zenith. WHY: she is Team Admin there through the rule, so the key is no broader than she is. HOW: let the issuing check use the owner's reach, Admin on all included.");
                Assert.That(writesZenith, Is.True, "WHAT: the key limited to Team Admin on Team Zenith cannot change it. WHY: its owner administers Team Zenith through the rule, so the limit is covered. HOW: cover a key row with the owner's role on that unit, the rule included.");
                Assert.That(writesApollo, Is.False, "WHAT: the key changes Team Apollo. WHY: a limited key keeps its exact scope even when its owner administers every Team. HOW: give the key its own rows only, never the owner's reach.");
                Assert.That(readsApollo, Is.False, "WHAT: the key reads Team Apollo. WHY: it is limited to Team Zenith. HOW: give the key its own rows only.");
                Assert.That(writesPortfolio, Is.False, "WHAT: the key changes a Portfolio. WHY: it is limited to one Team. HOW: give the key its own rows only.");
            }
        }

        [Test]
        public async Task Removing_the_rule_ends_the_members_admin_reach_on_the_next_request_twins_included()
        {
            scenario.TwoEqualAdminOnAllRowsWereStoredFor(AdminOnAllScenario.TeamAdmins);
            var writeBeforeRemoval = await scenario.CanWriteTeam(AdminOnAllScenario.Ines_SignedIn(), scenario.TeamZenith);
            var lines = (await scenario.TheRulesList()).Where(line => line.GroupValue == AdminOnAllScenario.TeamAdmins).ToList();

            await scenario.AlexRemovesOneLine(lines[0].Id);

            var writeAfterRemoval = await scenario.CanWriteTeam(AdminOnAllScenario.Ines_SignedIn(), scenario.TeamZenith);
            var summaryAfterRemoval = await scenario.TheSummaryOf(AdminOnAllScenario.Ines_SignedIn());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(writeBeforeRemoval, Is.True, "WHAT: the member could not change Team Zenith while the rule existed. WHY: without the reach first, the removal proves nothing. HOW: make Admin on all reach every Team.");
                Assert.That(lines, Has.Count.EqualTo(1), "WHAT: a rule stored twice is listed twice. WHY: the System Admin sees one rule and one Remove. HOW: list one line per group, role and scope.");
                Assert.That(writeAfterRemoval, Is.False, "WHAT: the member still changes Team Zenith after the rule was removed. WHY: removing a rule ends what it gave on the next request, and a twin left behind would keep granting it. HOW: delete every equal row and read rules on every request.");
                Assert.That(summaryAfterRemoval.AdminTeamIds.Concat(summaryAfterRemoval.AdminPortfolioIds), Is.Empty, "WHAT: the summary still lists administered units after the removal. WHY: the browser would keep showing Settings tabs. HOW: fill the lists from the rules read on this request.");
            }
        }

        [TestCaseSource(nameof(EveryCheckThatLetsAnAdminChangeAUnit))]
        public async Task With_everyone_signed_in_on_a_person_with_no_role_still_passes_no_check_that_changes_a_unit(AdminCheck check)
        {
            await scenario.AlexTurnsEveryoneSignedInOn();
            var meridian = scenario.ATeamIsCreated("Team Meridian");
            var morganReads = await scenario.CanReadTeam(AdminOnAllScenario.Morgan_SignedIn(), scenario.TeamZenith);

            var allowed = await scenario.WhatTheCheckAnswers(check, AdminOnAllScenario.Morgan_SignedIn(), meridian);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(morganReads, Is.True, "WHAT: with everyone signed in on, Morgan cannot read Team Zenith. WHY: without the switch's reach the refusal below proves nothing. HOW: keep the switch giving Viewer on every unit.");
                Assert.That(allowed, Is.False, $"WHAT: {check} lets Morgan, who holds no role and is in no group, through while everyone signed in can view. WHY: the switch gives reading only and can never make anyone an admin. HOW: keep the switch's reach Viewer, and read Admin on all only from a group's rule.");
            }
        }

        [TestCase("TeamAdmin")]
        [TestCase("PortfolioAdmin")]
        [TestCase("SystemAdmin")]
        public async Task A_stored_rule_for_all_units_with_a_role_other_than_Viewer_or_Admin_on_all_gives_nothing(string storedRole)
        {
            scenario.ARuleForAllUnitsWasStored(AdminOnAllScenario.TeamAdmins, AdminOnAllScenario.RoleNamed(storedRole));
            var meridian = scenario.ATeamIsCreated("Team Meridian");

            var reach = await scenario.WhatInesMayDo(meridian);
            var summary = await scenario.TheSummaryOf(AdminOnAllScenario.Ines_SignedIn());
            var managesAccess = await scenario.CanManageAccess(AdminOnAllScenario.Ines_SignedIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reach, Is.EqualTo(UnitReach.Nothing), $"WHAT: a stored {storedRole} rule for all units gave the member {reach}. WHY: a rule for all units gives Viewer or Admin on all and nothing else, so a row written by hand or by a newer release must grant nothing. HOW: read only Viewer and Admin-on-all rows as a reach.");
                Assert.That(summary.AdminTeamIds.Concat(summary.AdminPortfolioIds), Is.Empty, $"WHAT: a stored {storedRole} rule for all units lists administered units. WHY: it gives nothing. HOW: as above.");
                Assert.That(managesAccess || summary.IsSystemAdmin, Is.False, $"WHAT: a stored {storedRole} rule for all units made the member a System Admin. WHY: only a System row gives the System role. HOW: resolve the System role from the System row alone.");
            }
        }

        private static IEnumerable<TestCaseData> EveryCheckThatLetsAnAdminChangeAUnit()
            => Enum.GetValues<AdminCheck>().Select(check => new TestCaseData(check).SetArgDisplayNames(check.ToString()));
    }

    /// <summary>Each check the server makes before someone changes a Team or Portfolio, as the routes and pages ask it.</summary>
    public enum AdminCheck
    {
        ChangeATeamCreatedLater,
        ChangeAPortfolio,
        ChangeATeamThroughItsRoute,
        ChangeAPortfolioThroughItsRoute,
        WritablePortfoliosOfAFeatureList,
        MoveAFeatureOfThePortfolio,
        WriteAReportOfATeam,
        WriteAReportOfAPortfolio,
        ReadConnectionNamesForSettings,
        ManageTheMembersOfATeam,
        ManageTheMembersOfAPortfolio,
        AdministeredUnitsInTheSummary,
    }

    /// <summary>What one person may do on Team Zenith, on a Team created later and on Portfolio Ocean Explorer.</summary>
    public sealed record UnitReach(bool ReadsZenith, bool WritesZenith, bool WritesLaterTeam, bool ReadsPortfolio, bool WritesPortfolio)
    {
        public static UnitReach Nothing { get; } = new(false, false, false, false, false);
    }

    /// <summary>
    /// Drives the access service the way a signed-in person, their key and a System Admin reach it, with no web host: a
    /// database of the scenario's own with Team Zenith, Team Apollo and Portfolio Ocean Explorer stored, access control
    /// switched on and a Premium licence. Alex is the one System Admin; Ines is in the group team-admins, and with a
    /// second sign-in also in team-readers, and holds no role; Morgan has a profile, no role and no group. The routes
    /// and pages that let an admin change a unit reach the service through the same consumers the server builds.
    /// </summary>
    public sealed class AdminOnAllScenario : IDisposable
    {
        public const string GroupClaim = "groups";

        public const string TeamAdmins = "team-admins";

        public const string TeamReaders = "team-readers";

        public const string Alex = "oidc|alex-chen";

        public const string Ines = "oidc|ines-moreau";

        public const string Morgan = "oidc|morgan-davies";

        public const string DuplicateMessage = "This group already has this role here.";

        /// <summary>A unit number for a rule that must be refused before any unit is looked up.</summary>
        public const int AnyUnitNumber = 11;

        private readonly ServicePortHarness harness;

        public AdminOnAllScenario()
        {
            harness = new ServicePortHarness(services =>
            {
                services.AddSingleton(Options.Create(new AuthorizationConfiguration { Enabled = true, GroupClaimName = GroupClaim }));
                services.AddSingleton(AValidPremiumLicence());
                services.AddSingleton(TimeProvider.System);
                services.AddScoped<ICurrentUserProfileService, PeopleWhoSignedInBefore>();
                services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
            });

            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            ThePeopleAreKnown(context);
            var zenith = ATeam("Team Zenith");
            var apollo = ATeam("Team Apollo");
            var oceanExplorer = new Portfolio { Name = "Ocean Explorer", WorkTrackingSystemConnection = AConnection("Ocean Explorer") };
            context.Teams.AddRange(zenith, apollo);
            context.Portfolios.Add(oceanExplorer);
            context.SaveChanges();
            TeamZenith = zenith.Id;
            TeamApollo = apollo.Id;
            PortfolioOceanExplorer = oceanExplorer.Id;
        }

        public int TeamZenith { get; }

        public int TeamApollo { get; }

        public int PortfolioOceanExplorer { get; }

        /// <summary>The role a rule gives to be admin on every Team and Portfolio, as the server names it.</summary>
        public static UserRole AdminOnAll => RoleNamed("UnitAdmin");

        /// <summary>The scope a rule for every Team and Portfolio is stored under, as the server names it.</summary>
        public static PermissionScopeType AllUnits => PermissionScopeType.AllUnits;

        /// <summary>A role by the name the browser sends, so a role the server does not have yet is a failed expectation, not a build error.</summary>
        public static UserRole RoleNamed(string name)
        {
            UserRole role;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Enum.TryParse(name, out role), Is.True, $"WHAT: there is no role named {name}. WHY: Admin on all is a group rule with the role UnitAdmin on AllUnits, and the browser sends it by that name. HOW: append UnitAdmin to the roles.");
                if (name == "UnitAdmin")
                {
                    Assert.That((int)role, Is.EqualTo(4), "WHAT: UnitAdmin is not stored as 4. WHY: roles are stored as numbers, and an older release reading the rule must never mistake it for another role. HOW: append UnitAdmin after Viewer, never renumber.");
                }
            }

            return role;
        }

        public static ClaimsPrincipal SignedIn(string subject, params string[] groups)
        {
            var claims = new List<Claim> { new("sub", subject) };
            claims.AddRange(groups.Select(group => new Claim(GroupClaim, group)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
        }

        public static ClaimsPrincipal Ines_SignedIn() => SignedIn(Ines, TeamAdmins);

        public static ClaimsPrincipal Ines_InBothGroups() => SignedIn(Ines, TeamAdmins, TeamReaders);

        public static ClaimsPrincipal Morgan_SignedIn() => SignedIn(Morgan);

        public static ClaimsPrincipal Alex_SignedIn() => SignedIn(Alex);

        public Task<RbacOperationResult> AlexAddsAdminOnAllFor(string group)
            => Port(port => port.CreateGroupMappingAsync(group, AdminOnAll, AllUnits, null));

        public Task<RbacOperationResult> AlexAddsAMapping(string group, UserRole role, PermissionScopeType scope, int? scopeId)
            => Port(port => port.CreateGroupMappingAsync(group, role, scope, scopeId));

        public Task<IReadOnlyList<RbacGroupMappingSummary>> TheRulesList() => Port(port => port.GetGroupMappingsAsync());

        public Task<RbacOperationResult> AlexRemovesOneLine(int mappingId) => Port(port => port.RemoveGroupMappingAsync(mappingId));

        public async Task AlexTurnsEveryoneSignedInOn()
        {
            var read = await Port(port => port.GetEveryoneSignedInAsync());
            var saved = await Port(port => port.SetEveryoneSignedInAsync(Alex_SignedIn(), true, read.ConcurrencyToken));
            Assert.That(saved.Succeeded, Is.True, $"WHAT: Alex could not turn everyone signed in on ({saved.Message}). WHY: the scenario needs the switch on. HOW: keep the switch save working for a System Admin.");
        }

        public Task<RbacOperationResult> SomeoneTurnsEveryoneSignedIn(ClaimsPrincipal who, bool on)
            => Port(async port => await port.SetEveryoneSignedInAsync(who, on, (await port.GetEveryoneSignedInAsync()).ConcurrencyToken));

        public Task<bool> EveryoneSignedInIsOn() => Port(async port => (await port.GetEveryoneSignedInAsync()).EveryoneCanView);

        /// <summary>Two equal Admin-on-all rows, as two System Admins adding the same rule at the same moment can leave behind.</summary>
        public void TwoEqualAdminOnAllRowsWereStoredFor(string group)
        {
            var role = AdminOnAll;
            ARowIsStored(group, role);
            ARowIsStored(group, role);
        }

        /// <summary>A rule for all units with a role the page never offers for it, as a row written by hand or by a newer release.</summary>
        public void ARuleForAllUnitsWasStored(string group, UserRole role) => ARowIsStored(group, role);

        public int StoredRowsFor(string group)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().RbacGroupMappings.Count(mapping => mapping.GroupValue == group);
        }

        public int ATeamIsCreated(string name)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = ATeam(name);
            context.Teams.Add(team);
            context.SaveChanges();
            return team.Id;
        }

        public void InesHoldsDirectlyViewerOn(int teamId)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.UserPermissions.Add(new UserPermission { UserProfileId = ProfileIdOf(context, Ines), Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = teamId });
            context.SaveChanges();
        }

        /// <summary>The key the server stores once its creation was admitted, limited to Team Admin on one Team, signed in as that key.</summary>
        public ClaimsPrincipal InesHasAKeyLimitedToTeamAdminOn(int teamId)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var key = new ApiKey
            {
                Name = "zenith-settings",
                KeyHash = "hash",
                Salt = "salt",
                CreatedByUser = "Ines Moreau",
                OwnerUserProfileId = ProfileIdOf(context, Ines),
                OwnerSubject = Ines,
                CreatedAt = TestToday.FrozenNow.UtcDateTime,
            };
            context.ApiKeys.Add(key);
            context.SaveChanges();
            context.ApiKeyPermissions.Add(new ApiKeyPermission { ApiKeyId = key.Id, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = teamId, GrantedAt = TestToday.FrozenNow.UtcDateTime });
            context.SaveChanges();

            return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "api-key-user"),
                new Claim("auth_method", "api-key"),
                new Claim("api_key_id", key.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("sub", Ines),
            ], "ApiKey"));
        }

        public async Task<RbacOperationResult> InesMakesMorganAViewerOfTeamZenith()
        {
            var morgan = ProfileIdOf(Morgan);
            return await Port(port => port.SetTeamMemberRoleAsync(morgan, TeamZenith, UserRole.Viewer));
        }

        public async Task<RbacOperationResult> SomeoneMakesMorgan(UserRole role, int teamId)
        {
            var morgan = ProfileIdOf(Morgan);
            return await Port(port => port.SetTeamMemberRoleAsync(morgan, teamId, role));
        }

        /// <summary>What the server answers a person asking for a key limited to one role on one scope.</summary>
        public async Task<ObjectResult?> AsksForAKey(ClaimsPrincipal person, UserRole role, PermissionScopeType scopeType, int? scopeId)
        {
            using var scope = harness.NewScope();
            var keys = new Mock<IApiKeyService>(MockBehavior.Strict);
            var controller = new ApiKeyController(keys.Object, scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = person } },
            };

            var answer = await controller.CreateApiKey(new CreateApiKeyRequest
            {
                Name = "every-unit",
                Scope = [new ApiKeyScopeDto { Role = role, ScopeType = scopeType, ScopeId = scopeId }],
            });
            return answer as ObjectResult;
        }

        public async Task<UnitReach> WhatInesMayDo(int teamCreatedLater)
        {
            var ines = Ines_SignedIn();
            return new UnitReach(
                await CanReadTeam(ines, TeamZenith),
                await CanWriteTeam(ines, TeamZenith),
                await CanWriteTeam(ines, teamCreatedLater),
                await CanReadPortfolio(ines, PortfolioOceanExplorer),
                await CanWritePortfolio(ines, PortfolioOceanExplorer));
        }

        public Task<bool> WhatTheCheckAnswers(AdminCheck check, ClaimsPrincipal who, int teamCreatedLater) => check switch
        {
            AdminCheck.ChangeATeamCreatedLater => CanWriteTeam(who, teamCreatedLater),
            AdminCheck.ChangeAPortfolio => CanWritePortfolio(who, PortfolioOceanExplorer),
            AdminCheck.ChangeATeamThroughItsRoute => SatisfiesTheRequirement(who, RbacGuardRequirement.TeamWrite, teamCreatedLater),
            AdminCheck.ChangeAPortfolioThroughItsRoute => SatisfiesTheRequirement(who, RbacGuardRequirement.PortfolioWrite, PortfolioOceanExplorer),
            AdminCheck.WritablePortfoliosOfAFeatureList => Port(async port => (await port.GetWritablePortfolioIdsAsync(who, [PortfolioOceanExplorer])).Contains(PortfolioOceanExplorer)),
            AdminCheck.MoveAFeatureOfThePortfolio => MayMoveAFeatureOfThePortfolio(who),
            AdminCheck.WriteAReportOfATeam => Port(port => new ReportOwnerWriteAccess(port, who).MayWriteAsync(new ReportOwner(ReportOwnerKind.Team, teamCreatedLater))),
            AdminCheck.WriteAReportOfAPortfolio => Port(port => new ReportOwnerWriteAccess(port, who).MayWriteAsync(new ReportOwner(ReportOwnerKind.Portfolio, PortfolioOceanExplorer))),
            AdminCheck.ReadConnectionNamesForSettings => SatisfiesTheRequirement(who, RbacGuardRequirement.AnyScopedAdmin, null),
            AdminCheck.ManageTheMembersOfATeam => MayManageTheMembersOfTeam(who, teamCreatedLater),
            AdminCheck.ManageTheMembersOfAPortfolio => MayManageTheMembersOfPortfolio(who, PortfolioOceanExplorer),
            AdminCheck.AdministeredUnitsInTheSummary => Port(async port =>
            {
                var summary = await port.GetAuthorizationSummaryAsync(who);
                return summary.AdminTeamIds.Contains(teamCreatedLater) && summary.AdminPortfolioIds.Contains(PortfolioOceanExplorer);
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check, "Every check is asked."),
        };

        public Task<bool> SatisfiesTheRequirement(ClaimsPrincipal person, RbacGuardRequirement requirement, int? unitId)
            => Port(port => port.CanSatisfyRequirementAsync(person, requirement, unitId));

        public Task<bool> MayManageTheMembersOfTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanManageTeamMembershipAsync(person, teamId));

        public Task<bool> MayManageTheMembersOfPortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanManagePortfolioMembershipAsync(person, portfolioId));

        public Task<bool> CanReadTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanReadTeamAsync(person, teamId));

        public Task<bool> CanWriteTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanWriteTeamAsync(person, teamId));

        public Task<bool> CanReadPortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanReadPortfolioAsync(person, portfolioId));

        public Task<bool> CanWritePortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanWritePortfolioAsync(person, portfolioId));

        public Task<bool> CanManageAccess(ClaimsPrincipal person) => Port(port => port.CanManageRbacAsync(person));

        public Task<bool> SeesTheLicenceOwner(ClaimsPrincipal person) => Port(port => port.ConfirmsSystemAdminAsync(person, NullLogger.Instance, CancellationToken.None));

        public Task<UserAuthorizationSummary> TheSummaryOf(ClaimsPrincipal person) => Port(port => port.GetAuthorizationSummaryAsync(person));

        public void Dispose() => harness.Dispose();

        private async Task<bool> MayMoveAFeatureOfThePortfolio(ClaimsPrincipal who)
        {
            var feature = new Feature { Id = 501, Name = "Deep-sea sonar" };
            feature.Portfolios.Add(new Portfolio { Id = PortfolioOceanExplorer, Name = "Ocean Explorer" });
            var verdicts = await Port(port => new FeatureMoveAuthorization(port).GetVerdictsAsync(who, [feature], new HashSet<int> { PortfolioOceanExplorer }));
            return verdicts[feature.Id].CanMove;
        }

        private void ARowIsStored(string group, UserRole role)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = group, Role = role, ScopeType = AllUnits, ScopeId = null });
            context.SaveChanges();
        }

        private int ProfileIdOf(string subject)
        {
            using var scope = harness.NewScope();
            return ProfileIdOf(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>(), subject);
        }

        private static int ProfileIdOf(LighthouseAppContext context, string subject) => context.UserProfiles.Single(profile => profile.Subject == subject).Id;

        private async Task<T> Port<T>(Func<IRbacAdministrationService, Task<T>> step)
        {
            using var scope = harness.NewScope();
            return await step(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>());
        }

        private static void ThePeopleAreKnown(LighthouseAppContext context)
        {
            var alex = new UserProfile { Subject = Alex, SubjectClaimType = "sub", DisplayName = "Alex Chen" };
            var ines = new UserProfile { Subject = Ines, SubjectClaimType = "sub", DisplayName = "Ines Moreau", LastKnownGroupClaimValues = JsonSerializer.Serialize(new[] { TeamAdmins }) };
            var morgan = new UserProfile { Subject = Morgan, SubjectClaimType = "sub", DisplayName = "Morgan Davies", LastKnownGroupClaimValues = JsonSerializer.Serialize(Array.Empty<string>()) };
            context.UserProfiles.AddRange(alex, ines, morgan);
            context.SaveChanges();
            context.UserPermissions.Add(new UserPermission { UserProfileId = alex.Id, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null });
            context.SaveChanges();
        }

        private static Team ATeam(string name)
            => new() { Name = name, WorkTrackingSystemConnection = AConnection(name), DataRetrievalValue = "project = LIGHTHOUSE" };

        private static WorkTrackingSystemConnection AConnection(string name)
            => new() { Name = $"{name} connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

        private static ILicenseService AValidPremiumLicence()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
            return licence.Object;
        }

        /// <summary>Finds the person a principal speaks for among those who signed in before; a stranger is nobody.</summary>
        private sealed class PeopleWhoSignedInBefore(LighthouseAppContext context) : ICurrentUserProfileService
        {
            public async Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(principal);
                var subject = principal.FindFirst("sub")?.Value;
                return string.IsNullOrWhiteSpace(subject)
                    ? null
                    : await context.UserProfiles.SingleOrDefaultAsync(profile => profile.Subject == subject, cancellationToken);
            }
        }
    }
}
