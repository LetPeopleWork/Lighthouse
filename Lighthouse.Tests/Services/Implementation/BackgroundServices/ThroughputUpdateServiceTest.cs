using Lighthouse.Models;
using Lighthouse.Services.Implementation.BackgroundServices;
using Lighthouse.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Tests.Services.Implementation.BackgroundServices
{
    public class ThroughputUpdateServiceTest
    {
        private IConfiguration configuration;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IThroughputService> throughputServiceMock;
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<ThroughputUpdateService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            teamRepoMock = new Mock<IRepository<Team>>();
            throughputServiceMock = new Mock<IThroughputService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<ThroughputUpdateService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IRepository<Team>))).Returns(teamRepoMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IThroughputService))).Returns(throughputServiceMock.Object);

            SetupConfiguration(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeamsThroughputAsync()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            throughputServiceMock.Verify(x => x.UpdateThroughput(team));
            teamRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeamsThroughputAsync()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            throughputServiceMock.Verify(x => x.UpdateThroughput(team1));
            throughputServiceMock.Verify(x => x.UpdateThroughput(team2));
            teamRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesOnlyTeamsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now);

            SetupConfiguration(10, 360);

            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            throughputServiceMock.Verify(x => x.UpdateThroughput(team1));
            throughputServiceMock.Verify(x => x.UpdateThroughput(team2), Times.Never);
            teamRepoMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        private void SetupTeams(IEnumerable<Team> teams)
        {
            teamRepoMock.Setup(x => x.GetAll()).Returns(teams);
        }

        private Team CreateTeam(DateTime lastThroughputUpdateTime)
        {
            return new Team { ThroughputUpdateTime = lastThroughputUpdateTime };
        }

        private void SetupConfiguration(int interval, int refreshAfter)
        {
            var inMemorySettings = new Dictionary<string, string?> 
            {
                { "PeriodicRefresh:Throughput:Interval", interval.ToString() },
                { "PeriodicRefresh:Throughput:RefreshAfter", refreshAfter.ToString() },
                { "PeriodicRefresh:Forecast:StartDelay", 0.ToString() },
            };

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private ThroughputUpdateService CreateSubject()
        {
            return new ThroughputUpdateService(configuration, serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
