using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// Bug #5567 - the two <c>ForecastController</c> projection endpoints anchor "today" on two
    /// different calendar days. <c>RunItemCreationPrediction</c> reads <c>DateTime.Today</c>
    /// (server local) while <c>RunManualForecastAsync</c> reads <c>DateTime.UtcNow.Date</c>, in the
    /// same file and the same request lifetime. They only agree while the process runs in UTC - the
    /// container does, the standalone distribution inherits the host zone and does not.
    /// </summary>
    [TestFixture]
    public class ForecastControllerCalendarDayAnchorTest
    {
        private const string InstanceTimeZoneId = "Europe/Zurich";

        private const int WorkingDaysToCompletion = 10;

        /// <summary>
        /// 23:30 UTC is already 00:30 of the next day in Europe/Zurich (CET, UTC+1). This is the
        /// nightly window in which the instance's calendar day and the UTC calendar day disagree.
        /// The instant is supplied by the test, never read from the wall clock, so the expected
        /// anchor day below is a fixed literal and the assertion is deterministic. It is also a day
        /// in the past, so it can never coincide with the wall-clock day a run happens to fall on -
        /// on HEAD this test fails on every date, not only inside the nightly window.
        /// </summary>
        private static readonly DateTimeOffset NightlyInstantWhereZurichIsAlreadyTomorrow =
            new(2026, 1, 15, 23, 30, 0, TimeSpan.Zero);

        private static readonly DateOnly ExpectedAnchorDay = new(2026, 1, 16);

        private static readonly DateTime ItemCreationTargetDate =
            new(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] PredictedWorkItemTypes = ["Bug"];

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private WebApplicationFactory<Program> factory = null!;

        private HttpClient client = null!;

        private int seededTeamId;

        [OneTimeSetUp]
        public void StartApplicationAtTheNightlyInstant()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var forecastServiceMock = new Mock<IForecastService>();
            forecastServiceMock
                .Setup(s => s.When(It.IsAny<Team>(), It.IsAny<int>(), It.IsAny<ThroughputFilterMode>()))
                .ReturnsAsync(ForecastCompletingInWorkingDays(WorkingDaysToCompletion));
            forecastServiceMock
                .Setup(s => s.PredictWorkItemCreation(
                    It.IsAny<Team>(), It.IsAny<string[]>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .Returns((Team _, string[] _, DateTime _, DateTime _, int daysToForecast) =>
                    ForecastEchoingTheDayCount(daysToForecast));

            var teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamMetricsServiceMock
                .Setup(s => s.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(), false, null));

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<TimeProvider>();
                        services.AddSingleton<TimeProvider>(
                            new FakeTimeProvider(NightlyInstantWhereZurichIsAlreadyTomorrow));
                        services.RemoveAll<IForecastService>();
                        services.AddScoped(_ => forecastServiceMock.Object);
                        services.RemoveAll<ITeamMetricsService>();
                        services.AddScoped(_ => teamMetricsServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            seededTeamId = SeedTeam();
        }

        [OneTimeTearDown]
        public void StopApplication()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        /// <summary>
        /// Proves the lighthouse.runsettings pin actually took effect on this test host. .NET caches
        /// TimeZoneInfo.Local on first use, so a pin applied too late is silently inert - which reads
        /// as covered and is worse than no pin at all. Never ignored.
        /// </summary>
        [Test]
        public void TestHost_RunsUnderThePinnedInstanceTimeZone()
        {
            Assert.That(TimeZoneInfo.Local.Id, Is.EqualTo(InstanceTimeZoneId));
        }

        [Test]
        [Ignore("Bug #5567 - ForecastController anchors its two projection endpoints on different calendar days. Un-ignored by step 02-04 as the proof its cluster landed.")]
        public async Task RunItemCreationPrediction_AndRunManualForecast_AnchorOnTheSameCalendarDay()
        {
            var itemCreationPredictionAnchor = await AnchorDayUsedByItemCreationPrediction();
            var manualForecastAnchor = await AnchorDayUsedByManualForecast();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(itemCreationPredictionAnchor, Is.EqualTo(manualForecastAnchor),
                    "the two ForecastController endpoints projected from different calendar days");
                Assert.That(itemCreationPredictionAnchor, Is.EqualTo(ExpectedAnchorDay));
                Assert.That(manualForecastAnchor, Is.EqualTo(ExpectedAnchorDay));
            }
        }

        /// <summary>
        /// The stubbed forecast echoes the day count the controller derived, and every returned
        /// percentile carries it as its value. Subtracting it from the target date the caller sent
        /// recovers the calendar day the endpoint anchored its projection on.
        /// </summary>
        private async Task<DateOnly> AnchorDayUsedByItemCreationPrediction()
        {
            client.AsTeamViewer(seededTeamId);

            var input = new
            {
                StartDate = ItemCreationTargetDate.AddDays(-60),
                EndDate = ItemCreationTargetDate.AddDays(-30),
                TargetDate = ItemCreationTargetDate,
                WorkItemTypes = PredictedWorkItemTypes,
            };
            var response = await client.PostAsJsonAsync($"/api/latest/forecast/itemprediction/{seededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var daysToTargetDate = document.RootElement.GetProperty("howManyForecasts")
                .EnumerateArray()
                .First()
                .GetProperty("value").GetInt32();

            return DateOnly.FromDateTime(ItemCreationTargetDate.AddDays(-daysToTargetDate));
        }

        /// <summary>
        /// With no blackout periods configured, the returned percentile date is the anchor plus the
        /// forecast's working days, so subtracting them recovers the anchor.
        /// </summary>
        private async Task<DateOnly> AnchorDayUsedByManualForecast()
        {
            client.AsTeamViewer(seededTeamId);

            var input = new { RemainingItems = 5 };
            var response = await client.PostAsJsonAsync($"/api/latest/forecast/manual/{seededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var expectedDate = document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == 85)
                .GetProperty("expectedDate").GetDateTime();

            return DateOnly.FromDateTime(expectedDate.AddDays(-WorkingDaysToCompletion));
        }

        private static WhenForecast ForecastCompletingInWorkingDays(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private static HowManyForecast ForecastEchoingTheDayCount(int daysToForecast)
        {
            var simulation = new Dictionary<int, int> { [daysToForecast] = 100 };
            return new HowManyForecast(simulation, daysToForecast);
        }

        private int SeedTeam()
        {
            using var scope = factory.Services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }
    }
}
