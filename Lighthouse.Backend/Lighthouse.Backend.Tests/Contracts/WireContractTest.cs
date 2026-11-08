using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using static Lighthouse.Backend.Tests.TestHelpers.FixtureSetupTimer;

namespace Lighthouse.Backend.Tests.Contracts
{
    /// <summary>
    /// The base for a test that pins what a caller sees on the wire: route, status, JSON names, headers, sign-in.
    /// It starts one host for the whole fixture, because a host costs more to start than most scenarios take,
    /// and gives every scenario a fresh database, the starting day, an empty metrics cache and a new client.
    /// A scenario must leave the host idle; one that leaves work on the update queue fails at its tear-down
    /// rather than letting that work run against the next scenario's database. What the database holds and
    /// which routes are asked is the fixture's business, not this class's.
    /// </summary>
    public abstract class WireContractTest : IntegrationFixture
    {
        private TestWebApplicationFactory<Program> root = null!;

        private WebApplicationFactory<Program> host = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected IServiceProvider Services => host.Services;

        protected ResettableFakeTimeProvider Clock => root.Clock;

        protected IServiceScope NewScope() => host.Services.CreateScope();

        /// <summary>
        /// The one place a fixture changes the host, such as turning on test sign-in or replacing the licence.
        /// It runs once, before the host starts, so whatever it returns serves every scenario of the fixture.
        /// </summary>
        protected virtual WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged) => unchanged;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = Measure(GetType().Name, MeasurementKind.OneTimeSetUp);

            root = new TestWebApplicationFactory<Program> { UseSchemaTemplate = true };
            host = AdjustTheHost(root);
            host.StartServer();
        }

        [SetUp]
        public void GiveTheScenarioItsOwnState()
        {
            using var _ = Measure(GetType().Name, MeasurementKind.SetUp);

            root.Clock.Reset();
            root.UseFreshDatabase();
            host.ForgetTheCachedMetrics();
            Client = host.CreateClient();
        }

        [TearDown]
        public Task LeaveTheHostIdle() => LeaveTheHostIdleWithin(null);

        /// <summary>
        /// The tear-down with its own wait for the update queue, for a scenario that leaves work on the queue on
        /// purpose and expects the tear-down to fail: it need not sit through the full wait to see that.
        /// </summary>
        protected async Task LeaveTheHostIdleWithin(TimeSpan? queuePatience)
        {
            using var _ = Measure(GetType().Name, MeasurementKind.TearDown);

            try
            {
                await SharedHostTearDown.LeaveTheHostIdleOrFail(host.Services, queuePatience);
            }
            finally
            {
                // A failed wait must not leave the database behind for the next scenario; the failure itself
                // still propagates.
                Client.Dispose();
                using var scope = host.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = Measure(GetType().Name, MeasurementKind.OneTimeTearDown);

            if (!ReferenceEquals(host, root))
            {
                host?.Dispose();
            }

            root?.Dispose();
        }
    }
}
