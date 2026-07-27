using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
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

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Bug #5567 - the manual-forecast endpoint must project from the INSTANCE calendar day. The
    /// instant is supplied by the test, so both candidate days are fixed literals and the assertion
    /// discriminates between them on every date a run happens to fall on.
    /// </summary>
    [TestFixture]
    public class InstanceDayForecastWindowIntegrationTest
    {
        private const string InstanceTimeZoneId = "Europe/Zurich";

        private const int WorkingDaysToCompletion = 10;

        private const int PercentileUnderTest = 85;

        private static readonly DateTimeOffset NightlyInstantWhereZurichIsAlreadyTomorrow =
            new(2026, 1, 15, 23, 30, 0, TimeSpan.Zero);

        private static readonly DateTime ProjectionFromTheInstanceDay =
            new(2026, 1, 26, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime ProjectionFromTheUtcDay =
            new(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc);

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
        /// A pin applied too late is silently inert, which reads as covered and is worse than no pin.
        /// </summary>
        [Test]
        public void TestHost_RunsUnderThePinnedInstanceTimeZone()
        {
            Assert.That(TimeZoneInfo.Local.Id, Is.EqualTo(InstanceTimeZoneId));
        }

        [Test]
        public void InstanceClock_AtTheNightlyInstant_IsAlreadyOnTheNextDay()
        {
            var clock = factory.Services.GetRequiredService<ILighthouseClock>();

            Assert.That(clock.TodayAsUtcMidnight, Is.EqualTo(ProjectionFromTheInstanceDay.AddDays(-WorkingDaysToCompletion)));
        }

        [Test]
        public async Task RunManualForecast_AtTheNightlyInstant_ProjectsFromTheInstanceDayNotTheUtcDay()
        {
            client.AsTeamViewer(seededTeamId);

            var response = await client.PostAsJsonAsync(
                $"/api/latest/forecast/manual/{seededTeamId}", new { RemainingItems = 5 });

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var expectedDate = document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == PercentileUnderTest)
                .GetProperty("expectedDate").GetDateTime();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedDate.Date, Is.EqualTo(ProjectionFromTheInstanceDay),
                    "the manual forecast projected from a calendar day other than the instance day");
                Assert.That(expectedDate.Date, Is.Not.EqualTo(ProjectionFromTheUtcDay),
                    "the manual forecast projected from the UTC day");
            }
        }

        private static WhenForecast ForecastCompletingInWorkingDays(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation) { HasSufficientData = true };
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
