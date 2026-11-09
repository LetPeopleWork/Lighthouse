using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.Acceptance.SystemWideAccess;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts.Authorization
{
    /// <summary>
    /// What a browser reads and sends for "everyone signed in can view all Teams and Portfolios": the routes under both
    /// versions, the names on the wire, the answer to a stale or tokenless save, and the start-up seeders storing it off.
    /// What the switch gives is asserted at the port.
    /// </summary>
    public class EveryoneSignedInWireContractTest : WireContractTest
    {
        private static readonly bool[] OnlyOff = [false];

        private static readonly string[] SwitchNames = ["everyoneCanView", "concurrencyToken"];

        [TestCase("/api/latest/authorization/everyone-signed-in")]
        [TestCase("/api/v1/authorization/everyone-signed-in")]
        public async Task The_switch_reads_off_as_everyoneCanView_and_a_concurrencyToken_and_nothing_else(string route)
        {
            using var response = await Client.GetAsync(route);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {route} answered {(int)response.StatusCode}. WHY: Settings > Access reads the switch from this route. HOW: add the GET route on the authorization controller under both versions.");
            var theSwitch = JsonDocument.Parse(body).RootElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theSwitch.EnumerateObject().Select(member => member.Name), Is.EquivalentTo(SwitchNames), $"WHAT: the answer was {body}. WHY: the browser reads exactly everyoneCanView and concurrencyToken. HOW: serialise those two members.");
                Assert.That(theSwitch.GetProperty("everyoneCanView").GetBoolean(), Is.False, "WHAT: a new instance reads the switch on. WHY: it is off unless a System Admin turns it on. HOW: read a missing row as off.");
            }
        }

        [TestCase("/api/latest/authorization/everyone-signed-in")]
        [TestCase("/api/v1/authorization/everyone-signed-in")]
        public async Task A_save_on_the_token_read_is_kept_and_a_later_save_on_that_old_token_is_a_409_conflict(string route)
        {
            var firstRead = await ReadTheSwitch(route);

            using var kept = await Client.PutAsJsonAsync(route, new { everyoneCanView = true, concurrencyToken = firstRead.Token });
            var afterTheSave = await ReadTheSwitch(route);
            using var stale = await Client.PutAsJsonAsync(route, new { everyoneCanView = false, concurrencyToken = firstRead.Token });
            var staleBody = await stale.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kept.IsSuccessStatusCode, Is.True, $"WHAT: PUT {route} on the token read answered {(int)kept.StatusCode}. WHY: a System Admin turns the switch on with the token the read gave, also before any row is stored. HOW: add the PUT route, upserting under the token.");
                Assert.That(afterTheSave.On, Is.True, "WHAT: the switch reads off after it was saved on. WHY: the save must be kept. HOW: store everyoneCanView.");
                Assert.That(stale.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), $"WHAT: a save on the old token answered {(int)stale.StatusCode}. WHY: the browser reloads the switch and shows the conflict message on a 409. HOW: let the stale save throw DbUpdateConcurrencyException for the existing filter.");
                Assert.That(staleBody, Does.Contain("concurrency-conflict"), $"WHAT: the 409 answered {staleBody}. WHY: the browser tells a conflict from other refusals by its code. HOW: let the existing conflict filter answer it.");
            }
        }

        [TestCase("/api/latest/authorization/everyone-signed-in")]
        [TestCase("/api/v1/authorization/everyone-signed-in")]
        public async Task A_save_with_no_token_is_a_400_and_changes_nothing(string route)
        {
            using var refused = await Client.PutAsJsonAsync(route, new { everyoneCanView = true });
            var afterwards = await ReadTheSwitch(route);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"WHAT: a save with no token answered {(int)refused.StatusCode}. WHY: this route has required its token since it first shipped. HOW: refuse a missing token with 400 before writing.");
                Assert.That(afterwards.On, Is.False, "WHAT: the switch is on after a refused save. WHY: a refused save changes nothing. HOW: write nothing when the token is missing.");
            }
        }

        [Test]
        public async Task The_start_up_seeders_store_the_switch_off()
        {
            using var scope = NewScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }

            var stored = TheSwitch.StoredRows(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());

            Assert.That(stored.Select(row => row.On), Is.EqualTo(OnlyOff), $"WHAT: after the start-up seeders ran the stored switches are {string.Join(", ", stored)}. WHY: every install and upgrade starts with the switch stored off. HOW: register the switch's seeder with the other ISeeders in Program.cs.");
        }

        private async Task<SwitchState> ReadTheSwitch(string route)
        {
            using var response = await Client.GetAsync(route);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {route} answered {(int)response.StatusCode}. WHY: a save is based on what the read answers. HOW: add the GET route.");
            var theSwitch = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            return new SwitchState(theSwitch.GetProperty("everyoneCanView").GetBoolean(), theSwitch.GetProperty("concurrencyToken").GetGuid());
        }
    }

    /// <summary>Who is turned away on the wire from the switch: anyone who is not a System Admin.</summary>
    public class EveryoneSignedInRefusalWireContractTest : WireContractTest
    {
        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged);

        [TestCase("/api/latest/authorization/everyone-signed-in")]
        [TestCase("/api/v1/authorization/everyone-signed-in")]
        public async Task A_Team_Admin_may_neither_read_nor_save_the_switch(string route)
        {
            using var read = await Client.AsTeamAdmin(11).GetAsync(route);
            using var save = await Client.AsTeamAdmin(11).PutAsJsonAsync(route, new { everyoneCanView = true, concurrencyToken = Guid.NewGuid() });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a Team Admin's read of {route} was answered {(int)read.StatusCode}. WHY: only a System Admin manages the switch; a unit's admin learns it from the unit's rules. HOW: guard the route with the right to manage access and answer 403.");
                Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a Team Admin's save on {route} was answered {(int)save.StatusCode}. WHY: only a System Admin opens every Team and Portfolio to everyone. HOW: guard the PUT the same way and answer 403.");
            }
        }
    }
}
