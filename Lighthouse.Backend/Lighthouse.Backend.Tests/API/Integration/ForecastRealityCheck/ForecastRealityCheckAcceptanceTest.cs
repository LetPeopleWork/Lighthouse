using System.Text;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Acceptance harness for the Forecast Reality Check over the web host, for what only the running host
    /// shows: the line the check writes to the log an operator reads. What the check works out is pinned at
    /// its service (<see cref="RealityCheckAtThePortTest"/>), and what a caller receives on the wire by
    /// RealityCheckWireContractTest.
    ///
    /// The app is the production app: the real ASP.NET host over a real SQLite file, real EF, real DI,
    /// the real metrics service reading real finished Work Items. One host serves the whole fixture; every
    /// scenario gets its own database, clock reading, scripted forecast and log capture inside it, and ends
    /// by leaving the host idle. The day, the licence, the engine and the forecast script are replaced as on
    /// every web host that runs the check (<see cref="RealityCheckOnAWebHost"/>).
    ///
    /// Nothing here names a type of the check's own code. Every scenario talks to the check over the wire
    /// and reads the answer as JSON, so a change that breaks what a caller receives fails on the answer
    /// rather than on the build.
    /// </summary>
    public abstract class ForecastRealityCheckAcceptanceTest : IntegrationFixture
    {
        protected const string LatestRoute = "/api/latest/forecast/reality-check/";

        protected const string VersionedRoute = "/api/v1/forecast/reality-check/";

        protected const string NoOptions = "{}";

        private TestWebApplicationFactory<Program> rootFactory = null!;

        /// <summary>
        /// The host's forecast service reads this one script for the life of the host, so each scenario
        /// starts by putting it back to what a new one says.
        /// </summary>
        private RealityCheckForecastScript Forecasts { get; } = new();

        /// <summary>
        /// What the host logged at the level an operator sees by default, and above. The host's logger
        /// factory holds this sink for the life of the host, so each scenario empties it rather than
        /// replacing it.
        /// </summary>
        protected CapturedLogMessages CapturedLogs { get; } = new();

        private WebApplicationFactory<Program> Factory { get; set; } = null!;

        private HttpClient Client { get; set; } = null!;

        private RealityCheckSeedSteps Seed { get; set; } = null!;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();
            var script = Forecasts;
            var capturedLogs = CapturedLogs;

            Factory = TestWebApplicationFactory<Program>
                .WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.PinWhatTheCheckDependsOn(script);

                        // Serilog is the host's logging pipeline, so a logging provider added here would never
                        // see a line; replacing the factory is what makes the host's own log readable. The
                        // framework overrides mirror appsettings.json, so the capture holds what an operator reads.
                        services.RemoveAll<ILoggerFactory>();
                        services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                            new LoggerConfiguration()
                                .MinimumLevel.Information()
                                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                                .WriteTo.Sink(capturedLogs)
                                .CreateLogger(),
                            dispose: true));
                    });
                });

            Seed = new RealityCheckSeedSteps(Factory.Services, Forecasts, RealityCheckForecastScript.TodayDay);
        }

        [SetUp]
        public void GiveTheScenarioItsOwnState()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            CapturedLogs.Clear();
            Forecasts.Reset();
            Seed.StartAfresh();

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            Factory.ForgetTheCachedMetrics();

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                await SharedHostCleanup.DrainTheHistoryFillerOrFail(
                    Factory.Services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
                await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(Factory.Services, TimeSpan.FromSeconds(30));
            }
            finally
            {
                // A failed wait above must not leave the memo's notes or the database behind for the next
                // scenario on this host; the failure itself still propagates.
                await SharedHostCleanup.ForgetOwners(Factory.Services, Seed.OwnersSeeded);

                using (var scope = Factory.Services.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
                }

                Client.Dispose();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            Factory?.Dispose();
            rootFactory?.Dispose();
        }

        // --- Teams and their finished work ---

        protected int GivenATeam(string name, int samplingWindowDays) => Seed.GivenATeam(name, samplingWindowDays);

        protected int GivenAnotherTeam(string name) => Seed.GivenAnotherTeam(name);

        protected void GivenTheTeamFinishedAWorkItemEveryDay(int teamId) => Seed.GivenTheTeamFinishedAWorkItemEveryDay(teamId);

        // --- Asking ---

        protected async Task<HttpResponseMessage> WhenTheCheckIsRunBy(Func<HttpClient, HttpClient> identity, int teamId, string body = NoOptions, string route = LatestRoute)
        {
            identity(Client);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"{route}{teamId}", content);
        }

        protected Task<RealityCheckAnswer> ReadTheAnswer(HttpResponseMessage response) => RealityCheckAnswer.ReadFrom(response, Forecasts);
    }
}
