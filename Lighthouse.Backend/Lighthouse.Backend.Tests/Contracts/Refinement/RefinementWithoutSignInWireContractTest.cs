using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// What a browser or a client sees on the wire when it reads the Refinement tab of an instance without
    /// sign-in. What the tab works out is asserted at the port; this class holds only the names and shapes the
    /// callers read.
    /// </summary>
    public class RefinementWithoutSignInWireContractTest : WireContractTest
    {
        private static readonly TeamStates GravitysStates = new(["Backlog"], ["Next", "Analysing", "Implementation"], ["Done"]);

        [SetUp]
        public void StartFromTheSeededDatabase()
        {
            using var scope = NewScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        // The browser and the clients read both ends of the cycle and expect null when there is none; a member
        // left out of the answer would read as missing, not as "no cycle".
        [Test]
        public async Task A_Team_with_no_number_states_its_need_as_nulls_and_names_no_date()
        {
            var givens = new RefinementPortGivens(Services);
            var teamId = givens.ATeam("Team Gravity", GravitysStates, throughputHistoryDays: 30);
            givens.TheTeamRefines(teamId, RefinementPortGivens.RefinesInBacklogAnalysingAndNext, RefinementPortGivens.ReadyWhenTaggedReady);

            var need = await TheNeedOnTheRefinementTabOf(teamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.GetProperty("unavailableReason").GetString(), Is.EqualTo("NoCadence"), "a Team that has set no cadence has no number");
                Assert.That(KindOf(need, "cycleStart"), Is.EqualTo(JsonValueKind.Null));
                Assert.That(KindOf(need, "cycleEnd"), Is.EqualTo(JsonValueKind.Null));
            }
        }

        private async Task<JsonElement> TheNeedOnTheRefinementTabOf(int teamId)
        {
            using var response = await Client.GetAsync($"/api/latest/teams/{teamId}/refinement");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.GetProperty("need").Clone();
        }

        private static JsonValueKind KindOf(JsonElement owner, string member)
            => owner.TryGetProperty(member, out var value) ? value.ValueKind : JsonValueKind.Undefined;
    }
}
