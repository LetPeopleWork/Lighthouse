using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts.Authorization
{
    /// <summary>
    /// What a browser reads when it asks which rules for all Teams and Portfolios also reach one Team or Portfolio:
    /// both routes under both versions, the names of what comes back, and the answer for a unit that does not exist.
    /// What the rules are is asserted at the port.
    /// </summary>
    public class SystemWideRulesWireContractTest : WireContractTest
    {
        private const int ANumberNoUnitWasEverGiven = 987_654;

        private static readonly string[] RoleAndHolder = ["role", "holder"];

        private static readonly PermissionScopeType AllUnits = Enum.Parse<PermissionScopeType>("AllUnits");

        [TestCase("/api/latest/authorization/teams/{0}/system-wide-rules", true)]
        [TestCase("/api/v1/authorization/teams/{0}/system-wide-rules", true)]
        [TestCase("/api/latest/authorization/portfolios/{0}/system-wide-rules", false)]
        [TestCase("/api/v1/authorization/portfolios/{0}/system-wide-rules", false)]
        public async Task A_unit_answers_its_rules_as_a_role_and_a_holder_and_nothing_else(string routeOfAnyUnit, bool isTeam)
        {
            var (teamId, portfolioId) = TheseUnitsAndRulesAreStored();
            var route = string.Format(routeOfAnyUnit, isTeam ? teamId : portfolioId);

            using var response = await Client.GetAsync(route);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: {route} answered {(int)response.StatusCode}, not 200. WHY: the Access tab reads its notice from this route. HOW: add the GET route on the authorization controller under both versions.");
            var rules = JsonDocument.Parse(body).RootElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rules.GetArrayLength(), Is.EqualTo(1), $"WHAT: the answer was {body}. WHY: one rule for all Teams and Portfolios exists besides a System Admin group, which stays out. HOW: answer one line per distinct rule.");
                var line = rules[0];
                Assert.That(line.EnumerateObject().Select(member => member.Name), Is.EquivalentTo(RoleAndHolder), $"WHAT: the line was {line}. WHY: the browser reads exactly the role and the holder, and a stored line's number or scope has no place on a unit's Access tab. HOW: serialise a read model whose only members are Role and Holder.");
                Assert.That(line.GetProperty("role").GetString(), Is.EqualTo("Viewer"), "WHAT: the role is not the word Viewer. WHY: the browser shows roles by name, as the other access routes name them. HOW: serialise the role as its name.");
                Assert.That(line.GetProperty("holder").GetString(), Is.EqualTo("team-readers"), "WHAT: the holder is not the group's name. WHY: the notice shows which group holds the rule. HOW: answer the group value as the holder.");
            }
        }

        [TestCase("/api/latest/authorization/teams/{0}/system-wide-rules", true)]
        [TestCase("/api/v1/authorization/teams/{0}/system-wide-rules", true)]
        [TestCase("/api/latest/authorization/portfolios/{0}/system-wide-rules", false)]
        [TestCase("/api/v1/authorization/portfolios/{0}/system-wide-rules", false)]
        public async Task A_unit_that_does_not_exist_is_not_found_where_an_existing_one_answers_its_rules(string routeOfAnyUnit, bool isTeam)
        {
            var (teamId, portfolioId) = TheseUnitsAndRulesAreStored();

            using var known = await Client.GetAsync(string.Format(routeOfAnyUnit, isTeam ? teamId : portfolioId));
            using var unknown = await Client.GetAsync(string.Format(routeOfAnyUnit, ANumberNoUnitWasEverGiven));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(known.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: an existing unit was answered {(int)known.StatusCode}. WHY: the route exists for units that exist. HOW: add the GET route.");
                Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"WHAT: a unit that does not exist was answered {(int)unknown.StatusCode}. WHY: an empty list would read as no rule reaching a unit that is not there. HOW: answer 404 when the Team or Portfolio does not exist, as the other unit routes do.");
            }
        }

        private (int TeamId, int PortfolioId) TheseUnitsAndRulesAreStored()
        {
            using var scope = NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = new Team
            {
                Name = "Team Zenith",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira },
                DataRetrievalValue = "project = LIGHTHOUSE",
            };
            var portfolio = new Portfolio
            {
                Name = "Ocean Explorer",
                WorkTrackingSystemConnection = team.WorkTrackingSystemConnection,
            };
            context.Teams.Add(team);
            context.Portfolios.Add(portfolio);
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "team-readers", Role = UserRole.Viewer, ScopeType = AllUnits, ScopeId = null });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "lh-system-admins", Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null });
            context.SaveChanges();
            return (team.Id, portfolio.Id);
        }
    }

    /// <summary>
    /// Who is turned away on the wire from a unit's rules: a signed-in person who only reads, and an admin of another unit.
    /// </summary>
    public class SystemWideRulesRefusalWireContractTest : WireContractTest
    {
        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged);

        [TestCase("/api/latest/authorization/teams/11/system-wide-rules")]
        [TestCase("/api/v1/authorization/teams/11/system-wide-rules")]
        public async Task A_person_who_only_reads_or_administers_another_Team_is_refused_a_Teams_rules(string route)
        {
            using var asViewer = await GetAs(route, client => client.AsTeamViewer(11));
            using var asAnotherTeamsAdmin = await GetAs(route, client => client.AsTeamAdmin(12));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asViewer.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a Viewer of the Team was answered {(int)asViewer.StatusCode} on {route}. WHY: the rules name groups, which only those who manage the Team's members may see. HOW: guard the route with the right to manage the Team's members and answer 403.");
                Assert.That(asAnotherTeamsAdmin.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: an admin of another Team was answered {(int)asAnotherTeamsAdmin.StatusCode} on {route}. WHY: the right to manage members is the right on this Team. HOW: check the unit named in the route.");
            }
        }

        [TestCase("/api/latest/authorization/portfolios/21/system-wide-rules")]
        [TestCase("/api/v1/authorization/portfolios/21/system-wide-rules")]
        public async Task A_person_who_only_reads_or_administers_another_Portfolio_is_refused_a_Portfolios_rules(string route)
        {
            using var asViewer = await GetAs(route, client => client.AsPortfolioViewer(21));
            using var asAnotherPortfoliosAdmin = await GetAs(route, client => client.AsPortfolioAdmin(22));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asViewer.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a Viewer of the Portfolio was answered {(int)asViewer.StatusCode} on {route}. WHY: the rules name groups, which only those who manage the Portfolio's members may see. HOW: guard the route with the right to manage the Portfolio's members and answer 403.");
                Assert.That(asAnotherPortfoliosAdmin.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: an admin of another Portfolio was answered {(int)asAnotherPortfoliosAdmin.StatusCode} on {route}. WHY: the right to manage members is the right on this Portfolio. HOW: check the unit named in the route.");
            }
        }

        private async Task<HttpResponseMessage> GetAs(string route, Func<HttpClient, HttpClient> person)
        {
            return await person(Client).GetAsync(route);
        }
    }
}
