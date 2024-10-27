using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    public class TeamUpdateBackgroundServiceTest
    {
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<ITeamUpdateService> throughputServiceMock;
        private Mock<IAppSettingService> appSettingServiceMock;

        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<TeamUpdateBackgroundService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            teamRepoMock = new Mock<IRepository<Team>>();
            throughputServiceMock = new Mock<ITeamUpdateService>();
            appSettingServiceMock = new Mock<IAppSettingService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<TeamUpdateBackgroundService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IRepository<Team>))).Returns(teamRepoMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(ITeamUpdateService))).Returns(throughputServiceMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IAppSettingService))).Returns(appSettingServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeamsThroughputAsync()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            throughputServiceMock.Verify(x => x.UpdateTeam(team));
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

            throughputServiceMock.Verify(x => x.UpdateTeam(team1));
            throughputServiceMock.Verify(x => x.UpdateTeam(team2));
            teamRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesOnlyTeamsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now);

            SetupRefreshSettings(10, 360);

            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            throughputServiceMock.Verify(x => x.UpdateTeam(team1));
            throughputServiceMock.Verify(x => x.UpdateTeam(team2), Times.Never);
            teamRepoMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        private void SetupTeams(IEnumerable<Team> teams)
        {
            teamRepoMock.Setup(x => x.GetAll()).Returns(teams);
        }

        private Team CreateTeam(DateTime lastThroughputUpdateTime)
        {
            return new Team { TeamUpdateTime = lastThroughputUpdateTime };
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetThroughputRefreshSettings()).Returns(refreshSettings);
        }

        private TeamUpdateBackgroundService CreateSubject()
        {
            return new TeamUpdateBackgroundService(serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
