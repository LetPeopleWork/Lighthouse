using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A Team or Portfolio Admin asks which rules for every Team and Portfolio also reach their unit. They read the
    /// rules as a role and a group, once each, with no stored line to change and no System Admin group; a person who
    /// only reads the unit is not let in to ask. The rules of a unit are the distinct role and group
    /// pairs of every rule that reaches it, other than a System Admin group.
    /// </summary>
    [TestFixture]
    public class UnitSystemWideRulesAtThePortTest
    {
        private static readonly string[] RoleAndHolder = ["Role", "Holder"];

        private const string TeamReaders = "team-readers";

        private const string FinanceStaff = "finance-staff";

        private const string SystemAdminsGroup = "lh-system-admins";

        private const int TeamZenith = 11;

        private const int TeamApollo = 12;

        private const int PortfolioOceanExplorer = 21;

        private UnitRulesScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new UnitRulesScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [Test]
        public async Task One_Viewer_rule_reaching_every_Team_is_read_as_a_role_and_a_group_and_nothing_else()
        {
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rules.Lines, Is.EqualTo(new[] { (UserRole.Viewer, TeamReaders) }), "WHAT: Team Zenith's rules are not exactly Viewer for team-readers. WHY: a Team Admin needs to see who reaches their Team through a rule for all Teams and Portfolios. HOW: read the rules whose reach covers the unit and answer each as its role and group.");
                Assert.That(rules.Members, Is.EquivalentTo(RoleAndHolder), "WHAT: a rule carries more or less than its role and group. WHY: a stored line's number and scope are for the System Admin who edits it, and the Access tab must not offer a way to change it. HOW: answer a read model with Role and Holder only.");
            }
        }

        [Test]
        public async Task Twin_rows_of_one_rule_make_one_line_while_another_group_keeps_its_own_line()
        {
            scenario.TwoEqualRowsAreStored(TeamReaders, UserRole.Viewer);
            scenario.ARuleForEveryUnitIsStored(FinanceStaff, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(rules.Lines, Is.EquivalentTo(new[] { (UserRole.Viewer, TeamReaders), (UserRole.Viewer, FinanceStaff) }), "WHAT: the rules are not one line for team-readers and one for finance-staff. WHY: two equal rows are one rule to a person, and a different group is a different rule. HOW: answer the distinct role and group pairs.");
        }

        [Test]
        public async Task A_System_Admin_group_is_left_out_of_a_units_rules()
        {
            scenario.ASystemAdminGroupIsStored(SystemAdminsGroup);
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(rules.Lines.Select(line => line.Holder), Is.EqualTo(new[] { TeamReaders }), "WHAT: the System Admin group is named in a Team's rules. WHY: a System Admin group is no role on units, and its name stays away from Team Admins. HOW: leave group mappings that give System Admin out of the answer.");
        }

        [Test]
        public async Task With_no_rule_reaching_the_unit_nothing_is_listed_not_even_the_rules_of_single_units()
        {
            scenario.ASystemAdminGroupIsStored(SystemAdminsGroup);
            scenario.AMappingIsStored("zenith-readers", UserRole.Viewer, PermissionScopeType.Team, TeamZenith);
            scenario.AMappingIsStored("apollo-readers", UserRole.Viewer, PermissionScopeType.Team, TeamApollo);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(rules.Lines, Is.Empty, "WHAT: Team Zenith lists rules though no rule for all Teams and Portfolios exists. WHY: the notice shows only what the Team's own group list below does not already show. HOW: answer only the rules whose reach is every unit.");
        }

        [Test]
        public async Task A_Portfolio_reads_the_same_rules_the_same_way()
        {
            scenario.ASystemAdminGroupIsStored(SystemAdminsGroup);
            scenario.TwoEqualRowsAreStored(TeamReaders, UserRole.Viewer);
            scenario.AMappingIsStored("ocean-readers", UserRole.Viewer, PermissionScopeType.Portfolio, PortfolioOceanExplorer);

            var rules = await scenario.TheRulesReachingPortfolio(PortfolioOceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rules.Lines, Is.EqualTo(new[] { (UserRole.Viewer, TeamReaders) }), "WHAT: the Portfolio's rules are not exactly one line Viewer for team-readers. WHY: a Portfolio Admin needs the same view as a Team Admin. HOW: read the Portfolio's rules through the same reach, deduped, without System Admin groups.");
                Assert.That(rules.Members, Is.EquivalentTo(RoleAndHolder), "WHAT: the Portfolio's rule carries more than its role and group. WHY: a stored line's number and scope are not for a unit's admin. HOW: answer Role and Holder only.");
            }
        }

        [Test]
        public async Task A_units_rules_are_listed_by_group_and_then_by_role()
        {
            scenario.ARuleForEveryUnitIsStored(FinanceStaff, UserRole.Viewer);
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.UnitAdmin);
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(
                rules.Lines,
                Is.EqualTo(new[] { (UserRole.Viewer, FinanceStaff), (UserRole.Viewer, TeamReaders), (UserRole.UnitAdmin, TeamReaders) }),
                "WHAT: a unit's rules are not in a stable order. WHY: the Access tab shows the same list on every reload, a group's rules together. HOW: order the rules by group and then by role.");
        }

        [Test]
        public async Task A_Team_or_Portfolio_is_known_only_when_it_is_stored()
        {
            var (team, portfolio) = scenario.ATeamAndAPortfolioAreStored();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await scenario.TheTeamExists(team), Is.True, "WHAT: a stored Team is said not to exist. WHY: its rules are asked for by its number, and a missing Team answers not found. HOW: look the Team up by its number.");
                Assert.That(await scenario.TheTeamExists(team + 1000), Is.False, "WHAT: a Team that is not stored is said to exist. WHY: as above. HOW: as above.");
                Assert.That(await scenario.ThePortfolioExists(portfolio), Is.True, "WHAT: a stored Portfolio is said not to exist. WHY: as for a Team. HOW: look the Portfolio up by its number.");
                Assert.That(await scenario.ThePortfolioExists(portfolio + 1000), Is.False, "WHAT: a Portfolio that is not stored is said to exist. WHY: as above. HOW: as above.");
            }
        }

        [TestCase(UnitRulesScenario.Zoe, true, TestName = "A_Team_Admin_of_the_Team_may_ask_for_its_rules")]
        [TestCase(UnitRulesScenario.Alex, true, TestName = "A_System_Admin_may_ask_for_a_Teams_rules")]
        [TestCase(UnitRulesScenario.Noor, false, TestName = "A_member_who_only_reads_through_the_rule_is_refused_a_Teams_rules")]
        [TestCase(UnitRulesScenario.Ari, false, TestName = "A_Team_Admin_of_another_Team_is_refused_a_Teams_rules")]
        [TestCase(UnitRulesScenario.Sam, false, TestName = "A_person_with_no_role_is_refused_a_Teams_rules")]
        public async Task Only_those_who_manage_a_Teams_members_may_ask_for_its_rules(string who, bool allowed)
        {
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var mayAsk = await scenario.MayManageTheMembersOfTeam(who, TeamZenith);

            Assert.That(mayAsk, Is.EqualTo(allowed), $"WHAT: {who} {(allowed ? "is refused" : "is let in")} to Team Zenith's rules. WHY: the rules name groups, which only those who manage the Team's members, and System Admins, may see. HOW: guard the read with the right to manage the Team's members.");
        }

        [TestCase(UnitRulesScenario.Olive, true, TestName = "A_Portfolio_Admin_of_the_Portfolio_may_ask_for_its_rules")]
        [TestCase(UnitRulesScenario.Alex, true, TestName = "A_System_Admin_may_ask_for_a_Portfolios_rules")]
        [TestCase(UnitRulesScenario.Noor, false, TestName = "A_member_who_only_reads_through_the_rule_is_refused_a_Portfolios_rules")]
        [TestCase(UnitRulesScenario.Zoe, false, TestName = "A_Team_Admin_is_refused_a_Portfolios_rules")]
        public async Task Only_those_who_manage_a_Portfolios_members_may_ask_for_its_rules(string who, bool allowed)
        {
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var mayAsk = await scenario.MayManageTheMembersOfPortfolio(who, PortfolioOceanExplorer);

            Assert.That(mayAsk, Is.EqualTo(allowed), $"WHAT: {who} {(allowed ? "is refused" : "is let in")} to the Portfolio's rules. WHY: the rules name groups, which only those who manage the Portfolio's members, and System Admins, may see. HOW: guard the read with the right to manage the Portfolio's members.");
        }

        /// <summary>The rules a unit reports, as the port answers them, and the members each rule carries.</summary>
        public sealed record TheRules(IReadOnlyList<(UserRole Role, string Holder)> Lines, IReadOnlyList<string> Members);

        /// <summary>
        /// A database of the scenario's own, access control on, a Premium licence, one System Admin, a Team Admin of Zenith
        /// and of Apollo's neighbour, a Portfolio Admin, a reader through the group rule and a person with no role.
        /// </summary>
        public sealed class UnitRulesScenario : IDisposable
        {
            public const string Alex = "oidc|alex-chen";

            public const string Zoe = "oidc|zoe-zenith";

            public const string Ari = "oidc|ari-apollo";

            public const string Olive = "oidc|olive-ocean";

            public const string Noor = "oidc|noor-haddad";

            public const string Sam = "oidc|sam-weber";

            private const string GroupClaim = "groups";

            private readonly ServicePortHarness harness;

            public UnitRulesScenario()
            {
                harness = new ServicePortHarness(services =>
                {
                    services.AddSingleton(Options.Create(new AuthorizationConfiguration { Enabled = true, GroupClaimName = GroupClaim }));
                    services.AddSingleton(AValidPremiumLicence());
                    services.AddSingleton<TimeProvider>(new Microsoft.Extensions.Time.Testing.FakeTimeProvider(TestToday.FrozenNow));
                    services.AddScoped<ICurrentUserProfileService, PeopleWhoSignedInBefore>();
                    services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
                });

                using var scope = harness.NewScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                APersonWith(context, Alex, "Alex Chen", UserRole.SystemAdmin, PermissionScopeType.System, null);
                APersonWith(context, Zoe, "Zoe Zenith", UserRole.TeamAdmin, PermissionScopeType.Team, 11);
                APersonWith(context, Ari, "Ari Apollo", UserRole.TeamAdmin, PermissionScopeType.Team, 12);
                APersonWith(context, Olive, "Olive Ocean", UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, 21);
                APersonWith(context, Noor, "Noor Haddad", null, null, null, "team-readers");
                APersonWith(context, Sam, "Sam Weber", null, null, null);
                context.SaveChanges();
            }

            public void ARuleForEveryUnitIsStored(string group, UserRole role)
                => AMappingIsStored(group, role, PermissionScopeType.AllUnits, null);

            public void TwoEqualRowsAreStored(string group, UserRole role)
            {
                ARuleForEveryUnitIsStored(group, role);
                ARuleForEveryUnitIsStored(group, role);
            }

            public void ASystemAdminGroupIsStored(string group)
                => AMappingIsStored(group, UserRole.SystemAdmin, PermissionScopeType.System, null);

            public void AMappingIsStored(string group, UserRole role, PermissionScopeType scope, int? scopeId)
            {
                using var scopeOfWork = harness.NewScope();
                var context = scopeOfWork.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = group, Role = role, ScopeType = scope, ScopeId = scopeId });
                context.SaveChanges();
            }

            public Task<TheRules> TheRulesReachingTeam(int teamId) => ReadRules(port => port.GetTeamSystemWideRulesAsync(teamId));

            public Task<TheRules> TheRulesReachingPortfolio(int portfolioId) => ReadRules(port => port.GetPortfolioSystemWideRulesAsync(portfolioId));

            public (int Team, int Portfolio) ATeamAndAPortfolioAreStored()
            {
                using var scopeOfWork = harness.NewScope();
                var context = scopeOfWork.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var team = new Team { Name = "Team Zenith", WorkTrackingSystemConnection = AConnection("Team Zenith"), DataRetrievalValue = "project = ZENITH" };
                var portfolio = new Portfolio { Name = "Ocean Explorer", WorkTrackingSystemConnection = AConnection("Ocean Explorer") };
                context.Teams.Add(team);
                context.Portfolios.Add(portfolio);
                context.SaveChanges();
                return (team.Id, portfolio.Id);
            }

            public Task<bool> TheTeamExists(int teamId) => Port(port => port.TeamExistsAsync(teamId));

            public Task<bool> ThePortfolioExists(int portfolioId) => Port(port => port.PortfolioExistsAsync(portfolioId));

            public Task<bool> MayManageTheMembersOfTeam(string who, int teamId)
                => Port(port => port.CanManageTeamMembershipAsync(SignedIn(who), teamId));

            public Task<bool> MayManageTheMembersOfPortfolio(string who, int portfolioId)
                => Port(port => port.CanManagePortfolioMembershipAsync(SignedIn(who), portfolioId));

            public void Dispose() => harness.Dispose();

            private static WorkTrackingSystemConnection AConnection(string name)
                => new() { Name = $"{name} connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            private static ClaimsPrincipal SignedIn(string subject)
            {
                var groups = subject == Noor ? new[] { "team-readers" } : [];
                var claims = new List<Claim> { new("sub", subject) };
                claims.AddRange(groups.Select(group => new Claim(GroupClaim, group)));
                return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
            }

            private async Task<TheRules> ReadRules(Func<IRbacAdministrationService, Task<IReadOnlyList<SystemWideRule>>> read)
            {
                var answer = await Port(read);
                var members = typeof(SystemWideRule).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name).ToList();
                return new TheRules([.. answer.Select(rule => (rule.Role, rule.Holder))], members);
            }

            private async Task<T> Port<T>(Func<IRbacAdministrationService, Task<T>> step)
            {
                using var scope = harness.NewScope();
                return await step(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>());
            }

            private static void APersonWith(LighthouseAppContext context, string subject, string name, UserRole? role, PermissionScopeType? scope, int? scopeId, params string[] groups)
            {
                var profile = new UserProfile
                {
                    Subject = subject,
                    SubjectClaimType = "sub",
                    DisplayName = name,
                    LastKnownGroupClaimValues = JsonSerializer.Serialize(groups),
                };
                context.UserProfiles.Add(profile);
                context.SaveChanges();
                if (role is not null)
                {
                    context.UserPermissions.Add(new UserPermission { UserProfileId = profile.Id, Role = role.Value, ScopeType = scope!.Value, ScopeId = scopeId });
                }
            }

            private static ILicenseService AValidPremiumLicence()
            {
                var licence = new Mock<ILicenseService>();
                licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
                return licence.Object;
            }

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
}
