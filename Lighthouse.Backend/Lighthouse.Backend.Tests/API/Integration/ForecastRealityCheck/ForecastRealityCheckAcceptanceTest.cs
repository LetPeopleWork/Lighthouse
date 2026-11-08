using System.Text;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Feature-wide acceptance harness for the Forecast Reality Check: one press replays a Team's own
    /// forecast against its own finished work at every sampling window and every horizon, and answers
    /// with a region of windows that behaved alike rather than a winner.
    ///
    /// The app is the production app: the real ASP.NET host over a real SQLite file, real EF, real DI,
    /// the real metrics service reading real finished Work Items. One host serves the whole fixture; every
    /// scenario gets its own database, clock reading, scripted forecast and log capture inside it, and ends
    /// by leaving the host idle. The clock is pinned, because every
    /// check ends today and a drifting today would move every date these scenarios name. The licence is
    /// granted, because it is external.
    ///
    /// The forecast is the one thing a scenario usually scripts, and it is scripted per check rather than
    /// replaced wholesale. A scenario about which levels held has to choose the forecast a check produced,
    /// or it is asserting sampling noise; and the simulation is also the one cost that grows several times
    /// over when the suite runs under coverage. Where a scenario is about the check running end to end, it asks
    /// for the shipped engine instead, with its starting number pinned and fewer runs than production uses.
    ///
    /// Nothing here names a type of the check's own code. Every scenario talks to the check over the wire
    /// and reads the answer as JSON, so the scenarios pin what a caller receives, and a change that breaks
    /// that fails on the answer rather than on the build.
    /// </summary>
    public abstract class ForecastRealityCheckAcceptanceTest : IntegrationFixture
    {
        protected const string LatestRoute = "/api/latest/forecast/reality-check/";

        protected const string VersionedRoute = "/api/v1/forecast/reality-check/";

        protected const string NoOptions = "{}";

        /// <summary>The Team has no rolling window for the check to test, so its setting sits neither inside nor outside anything.</summary>
        protected const string NotTested = "NotTested";

        /// <summary>A level's reading when no check could be evaluated at all.</summary>
        protected const string NotEvaluated = "NotEvaluated";

        /// <summary>Why a setting was not tested: the Team forecasts from a fixed pair of dates. Wins when both reasons apply.</summary>
        protected const string UsesFixedDates = "UsesFixedDates";

        /// <summary>Why a setting was not tested: the stored window is zero days or fewer.</summary>
        protected const string NotAPositiveLength = "NotAPositiveLength";

        /// <summary>
        /// The sampling windows every Team is checked against, whatever its own setting is.
        /// </summary>
        protected static readonly int[] StandardWindowDays = RealityCheckForecastScript.StandardWindowDays;

        /// <summary>One, two, four and eight weeks, each ending today.</summary>
        protected static readonly int[] HorizonDays = RealityCheckForecastScript.HorizonDays;

        protected static readonly int[] ConfidenceLevels = [50, 70, 85, 95];

        /// <summary>
        /// The day the instance believes it is, for every scenario. Arbitrary; that it never moves is the
        /// point, since every check this feature makes is measured back from it.
        /// </summary>
        protected static readonly DateTimeOffset Today = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        /// <summary>Far fewer runs than production's ten thousand; a scenario needs a spread, not precision.</summary>
        private const int SimulatedRunsWhenTheEngineRuns = 1_000;

        private const long PinnedStartingNumber = 4172;

        private TestWebApplicationFactory<Program> rootFactory = null!;

        /// <summary>
        /// The host's forecast service reads this one script for the life of the host, so each scenario
        /// starts by putting it back to what a new one says.
        /// </summary>
        protected RealityCheckForecastScript Forecasts { get; } = new();

        /// <summary>
        /// What the host logged at the level an operator sees by default, and above. The host's logger
        /// factory holds this sink for the life of the host, so each scenario empties it rather than
        /// replacing it.
        /// </summary>
        protected CapturedLogMessages CapturedLogs { get; } = new();

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected static DateOnly TodayDay => DateOnly.FromDateTime(Today.UtcDateTime);

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
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Today));

                        var license = new Mock<ILicenseService>();
                        license.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
                        services.RemoveAll<ILicenseService>();
                        services.AddSingleton(license.Object);

                        services.RemoveAll<IDrawStreamFactory>();
                        services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(PinnedStartingNumber));
                        services.RemoveAll<ForecastSimulationLimits>();
                        services.AddSingleton(new ForecastSimulationLimits(
                            SimulatedRunsWhenTheEngineRuns,
                            ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

                        services.RemoveAll<IForecastService>();
                        services.AddScoped<ForecastService>();
                        services.AddScoped<IForecastService>(provider =>
                            script.Over(provider.GetRequiredService<ForecastService>()));

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

            Seed = new RealityCheckSeedSteps(Factory.Services, Forecasts, TodayDay);
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

        protected int GivenATeamForecastingFromFixedDates(string name, int inertSamplingWindowDays)
            => Seed.GivenATeamForecastingFromFixedDates(name, inertSamplingWindowDays);

        protected int GivenAnotherTeam(string name) => Seed.GivenAnotherTeam(name);

        protected void GivenTheTeamFinishedAWorkItemEveryDay(int teamId) => Seed.GivenTheTeamFinishedAWorkItemEveryDay(teamId);

        protected void GivenTheTeamFinishedAWorkItemEvery(int teamId, int days) => Seed.GivenTheTeamFinishedAWorkItemEvery(teamId, days);

        protected void GivenTheTeamFinishedWorkItemsOn(int teamId, List<DateOnly> days) => Seed.GivenTheTeamFinishedWorkItemsOn(teamId, days);

        // --- Asking ---

        protected async Task<HttpResponseMessage> WhenTheCheckIsRunBy(Func<HttpClient, HttpClient> identity, int teamId, string body = NoOptions, string route = LatestRoute)
        {
            identity(Client);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"{route}{teamId}", content);
        }

        protected Task<HttpResponseMessage> WhenSomebodyWhoCanReadTheTeamRunsTheCheck(int teamId, string body = NoOptions, string route = LatestRoute)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(teamId, "maria-santos"), teamId, body, route);

        protected async Task<RealityCheckAnswer> TheAnswerTo(int teamId)
        {
            using var response = await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(teamId);
            return await ReadTheAnswer(response);
        }

        protected Task<RealityCheckAnswer> ReadTheAnswer(HttpResponseMessage response) => RealityCheckAnswer.ReadFrom(response, Forecasts);

        // --- Reading what is stored ---

        protected RealityCheckSeedSteps.TeamAsStored WhatIsStoredFor(int teamId) => Seed.WhatIsStoredFor(teamId);
    }
}
