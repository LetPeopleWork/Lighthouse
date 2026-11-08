using System.Net;
using System.Reflection;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a fixture on <see cref="WireContractTest"/> can rely on. The fixtures nested here sit on the base and
    /// are run by NUnit itself, so the base's one-time set-up and hook run the way they do for a real contract
    /// fixture; each fixture's scenarios read what its own hook or scenarios recorded. This class only reads
    /// one of them by reflection and starts no host.
    /// </summary>
    [TestFixture]
    public class WireContractTestTest
    {
        [Test]
        public void A_contract_fixture_is_in_the_hosted_tier_without_saying_so()
        {
            var ownCategories = typeof(OneHostForThreeScenarios).GetCustomAttributes<CategoryAttribute>(inherit: false);
            var categories = typeof(OneHostForThreeScenarios).GetCustomAttributes<CategoryAttribute>(inherit: true).Select(category => category.Name);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ownCategories, Is.Empty, "the fixture must carry no category of its own for this to show anything");
                Assert.That(categories, Has.Exactly(1).EqualTo("Integration"));
            }
        }

        public class OneHostForThreeScenarios : WireContractTest
        {
            private int hostBuilds;

            protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
                => unchanged.WithWebHostBuilder(builder => builder.ConfigureServices(_ => hostBuilds++));

            [TestCase(1)]
            [TestCase(2)]
            [TestCase(3)]
            public void A_contract_fixture_starts_one_host_however_many_scenarios_it_holds(int scenario)
            {
                Assert.That(hostBuilds, Is.EqualTo(1), $"host builds by scenario {scenario}");
            }
        }

        public class EveryScenarioStartsAfresh : WireContractTest
        {
            [TestCase(1)]
            [TestCase(2)]
            public void Each_contract_scenario_starts_from_a_fresh_database_on_the_same_day(int scenario)
            {
                using (var scope = NewScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(context.Teams.Count(), Is.Zero, $"Teams found at the start of scenario {scenario}");
                        Assert.That(Clock.GetUtcNow(), Is.EqualTo(TestToday.FrozenNow), $"the day at the start of scenario {scenario}");
                    }

                    ATeamIsStoredIn(context);
                }

                Clock.Advance(TimeSpan.FromDays(3));
            }

            private static void ATeamIsStoredIn(LighthouseAppContext context)
            {
                var connection = new WorkTrackingSystemConnection { Name = "Contract", WorkTrackingSystem = WorkTrackingSystems.Csv };
                context.WorkTrackingSystemConnections.Add(connection);
                context.SaveChanges();

                context.Teams.Add(new Team { Name = "Ocean Explorer", WorkTrackingSystemConnectionId = connection.Id });
                context.SaveChanges();
            }
        }

        public class SignInTurnedOnThroughTheHook : WireContractTest
        {
            protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
                => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged);

            [Test]
            public async Task A_fixture_changes_the_host_through_its_one_hook()
            {
                using var response = await Client.AsViewer().GetAsync("/api/latest/encryption");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        public class WorkLeftOnTheQueue : WireContractTest
        {
            private const int QueuedTeam = 4711;

            [Test]
            public async Task A_contract_scenario_that_leaves_work_in_the_queue_fails_at_tear_down()
            {
                var refreshMayEnd = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Services.GetRequiredService<IUpdateQueueService>().EnqueueUpdate(UpdateType.Team, QueuedTeam, _ => refreshMayEnd.Task);

                var failure = await Assert.ThrowsAsync<AssertionException>(LeaveTheHostIdle);

                // Letting the refresh end is what lets the tear-down NUnit runs after this scenario find the host
                // idle, so the failure is seen here and nowhere else.
                refreshMayEnd.SetResult();

                Assert.That(failure?.Message, Does.Contain("update queue did not go idle"));
            }
        }
    }
}
