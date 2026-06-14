using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public abstract class RecurringBlackoutRulesTestBase
    {
        protected const int WorkingDaysAtAllPercentiles = 10;

        private TestWebApplicationFactory<Program> rootFactory = null!;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected Mock<IForecastService> ForecastServiceMock { get; private set; } = null!;

        protected Mock<ITeamMetricsService> TeamMetricsServiceMock { get; private set; } = null!;

        protected Mock<IPortfolioMetricsService> PortfolioMetricsServiceMock { get; private set; } = null!;

        protected Mock<ILicenseService> LicenseServiceMock { get; private set; } = null!;

        protected int SeededTeamId { get; private set; }

        protected static DateTime Today => DateTime.UtcNow.Date;

        protected void StartApplicationWithDeterministicForecast()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            ForecastServiceMock = new Mock<IForecastService>();
            ForecastServiceMock
                .Setup(s => s.When(It.IsAny<Team>(), It.IsAny<int>(), It.IsAny<ThroughputFilterMode>()))
                .ReturnsAsync(ForecastCompletingInWorkingDays(WorkingDaysAtAllPercentiles));

            TeamMetricsServiceMock = new Mock<ITeamMetricsService>();
            TeamMetricsServiceMock
                .Setup(s => s.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(), false, null));

            PortfolioMetricsServiceMock = new Mock<IPortfolioMetricsService>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IForecastService>();
                        services.AddScoped(_ => ForecastServiceMock.Object);
                        services.RemoveAll<ITeamMetricsService>();
                        services.AddScoped(_ => TeamMetricsServiceMock.Object);
                        services.RemoveAll<IPortfolioMetricsService>();
                        services.AddScoped(_ => PortfolioMetricsServiceMock.Object);
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeededTeamId = SeedTeam();
        }

        protected void StopApplication()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
        }

        protected void ConfigureOneOffBlackoutPeriod(DateOnly start, DateOnly end)
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
            return new WhenForecast(simulation) { HasSufficientData = true };
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
