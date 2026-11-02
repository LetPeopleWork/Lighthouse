using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public abstract class BlackoutForecastShiftTestBase
    {
        protected const int WorkingDaysAtAllPercentiles = 10;

        private TestWebApplicationFactory<Program> rootFactory = null!;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected Mock<IForecastService> ForecastServiceMock { get; private set; } = null!;

        protected Mock<ITeamMetricsService> TeamMetricsServiceMock { get; private set; } = null!;

        protected int SeededTeamId { get; private set; }

        protected DateTime Today => Factory.Services.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IForecastService>();
                        services.AddScoped(_ => ForecastServiceMock.Object);
                        services.RemoveAll<ITeamMetricsService>();
                        services.AddScoped(_ => TeamMetricsServiceMock.Object);
                    });
                });
        }

        [SetUp]
        public void PrepareTheHostForTheTest()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            ForecastServiceMock = ForecastServiceDoubles.AnsweringComputed();
            ForecastServiceMock
                .Setup(s => s.When(It.IsAny<Team>(), It.IsAny<int>(), It.IsAny<ThroughputFilterMode>()))
                .ReturnsAsync(ForecastCompletingInWorkingDays(WorkingDaysAtAllPercentiles));

            TeamMetricsServiceMock = new Mock<ITeamMetricsService>();
            TeamMetricsServiceMock
                .Setup(s => s.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(), false, null));

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            Factory.ForgetTheCachedMetrics();

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureCreated();

            SeededTeamId = SeedTeam();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                await SharedHostTearDown.LeaveTheHostIdleOrFail(Factory.Services);
            }
            finally
            {
                await SharedHostCleanup.ForgetOwners(Factory.Services, [(SeededTeamId, OwnerType.Team)]);

                using (var teardownScope = Factory.Services.CreateScope())
                {
                    var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                    await dbContext.Database.EnsureDeletedAsync();
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

        protected void ConfigureBlackoutPeriod(DateOnly start, DateOnly end)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            repository.Add(new BlackoutPeriod { Start = start, End = end, Description = "Company shutdown" });
            repository.Save().GetAwaiter().GetResult();
        }

        protected static WhenForecast ForecastCompletingInWorkingDays(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation, DateTime.UtcNow) { HasSufficientData = true };
        }

        private int SeedTeam()
        {
            using var scope = Factory.Services.CreateScope();
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
