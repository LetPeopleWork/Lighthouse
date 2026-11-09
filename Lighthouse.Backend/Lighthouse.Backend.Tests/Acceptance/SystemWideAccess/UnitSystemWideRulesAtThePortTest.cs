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
    /// only reads the unit is not let in to ask. L9 is the law: the rules of a unit are the distinct role and group
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
        public async Task L9_one_Viewer_rule_reaching_every_Team_is_read_as_a_role_and_a_group_and_nothing_else()
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
        public async Task L9_twin_rows_of_one_rule_make_one_line_while_another_group_keeps_its_own_line()
        {
            scenario.TwoEqualRowsAreStored(TeamReaders, UserRole.Viewer);
            scenario.ARuleForEveryUnitIsStored(FinanceStaff, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(rules.Lines, Is.EquivalentTo(new[] { (UserRole.Viewer, TeamReaders), (UserRole.Viewer, FinanceStaff) }), "WHAT: the rules are not one line for team-readers and one for finance-staff. WHY: two equal rows are one rule to a person, and a different group is a different rule. HOW: answer the distinct role and group pairs.");
        }

        [Test]
        public async Task L9_a_System_Admin_group_is_left_out_of_a_units_rules()
        {
            scenario.ASystemAdminGroupIsStored(SystemAdminsGroup);
            scenario.ARuleForEveryUnitIsStored(TeamReaders, UserRole.Viewer);

            var rules = await scenario.TheRulesReachingTeam(TeamZenith);

            Assert.That(rules.Lines.Select(line => line.Holder), Is.EqualTo(new[] { TeamReaders }), "WHAT: the System Admin group is named in a Team's rules. WHY: a System Admin group is no role on units, and its name stays away from Team Admins. HOW: leave group mappings that give System Admin out of the answer.");
        }

        [Test]
        public async Task L9_with_no_rule_reaching_the_unit_nothing_is_listed_not_even_the_rules_of_single_units()
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
                    services.AddSingleton(TimeProvider.System);
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
                => AMappingIsStored(group, role, AllUnits(), null);

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

            public Task<TheRules> TheRulesReachingTeam(int teamId) => ReadRules("GetTeamSystemWideRulesAsync", teamId);

            public Task<TheRules> TheRulesReachingPortfolio(int portfolioId) => ReadRules("GetPortfolioSystemWideRulesAsync", portfolioId);

            public Task<bool> MayManageTheMembersOfTeam(string who, int teamId)
                => Port(port => port.CanManageTeamMembershipAsync(SignedIn(who), teamId));

            public Task<bool> MayManageTheMembersOfPortfolio(string who, int portfolioId)
                => Port(port => port.CanManagePortfolioMembershipAsync(SignedIn(who), portfolioId));

            public void Dispose() => harness.Dispose();

            private static PermissionScopeType AllUnits()
            {
                Assert.That(
                    Enum.TryParse<PermissionScopeType>("AllUnits", out var allUnits),
                    Is.True,
                    "WHAT: there is no scope named AllUnits. WHY: a rule for every Team and Portfolio is a group mapping on that scope. HOW: append AllUnits to the scopes a permission can have.");
                return allUnits;
            }

            private static ClaimsPrincipal SignedIn(string subject)
            {
                var groups = subject == Noor ? new[] { "team-readers" } : [];
                var claims = new List<Claim> { new("sub", subject) };
                claims.AddRange(groups.Select(group => new Claim(GroupClaim, group)));
                return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
            }

            private async Task<TheRules> ReadRules(string member, int unitId)
            {
                using var scope = harness.NewScope();
                var port = scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();
                var read = typeof(IRbacAdministrationService).GetMethod(member);
                if (read is null)
                {
                    Assert.Fail($"WHAT: the access service has no {member}. WHY: a Team or Portfolio Admin must be able to ask which rules for all Teams and Portfolios also reach their unit. HOW: add {member}(int unitId, CancellationToken) to IRbacAdministrationService, answering a list of rules with a Role and a Holder.");
                }

                var task = (Task)read!.Invoke(port, [unitId, CancellationToken.None])!;
                await task;
                var answer = ((System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!).Cast<object>().ToList();
                var members = answer.Count == 0
                    ? (IReadOnlyList<string>)(new[] { "Role", "Holder" })
                    : [.. answer[0].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name)];
                var lines = answer.Select(rule => (
                    (UserRole)rule.GetType().GetProperty("Role")!.GetValue(rule)!,
                    (string)rule.GetType().GetProperty("Holder")!.GetValue(rule)!)).ToList();
                return new TheRules(lines, members);
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
