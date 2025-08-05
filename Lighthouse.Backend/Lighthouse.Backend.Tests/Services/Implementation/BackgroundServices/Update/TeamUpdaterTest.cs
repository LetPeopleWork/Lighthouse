using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class TeamUpdaterTest : UpdateServiceTestBase
    {
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<ITeamDataService> teamDataServiceMock;
        private Mock<ILicenseService> licenseServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            teamRepoMock = new Mock<IRepository<Team>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            licenseServiceMock = new Mock<ILicenseService>();
            teamDataServiceMock = new Mock<ITeamDataService>();

            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(licenseServiceMock.Object);
            SetupServiceProviderMock(teamDataServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeams()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeams()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team1), Times.Once);
            teamDataServiceMock.Verify(x => x.UpdateTeamData(team2), Times.Once);
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

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team1), Times.Once);
            teamDataServiceMock.Verify(x => x.UpdateTeamData(team2), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_NoPremiumLicense_MoreThanThreeTeams_DoesNotRefresh()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);

            SetupTeams([team, CreateTeam(DateTime.Now), CreateTeam(DateTime.Now), CreateTeam(DateTime.Now)]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanThreeTeams_Refreshes()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupRefreshSettings(10, 360);
            SetupTeams([team, CreateTeam(DateTime.Now), CreateTeam(DateTime.Now), CreateTeam(DateTime.Now)]);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once);
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetTeamDataRefreshSettings()).Returns(refreshSettings);
        }

        private void SetupTeams(IEnumerable<Team> teams)
        {
            teamRepoMock.Setup(x => x.GetAll()).Returns(teams);

            foreach (var team in teams)
            {
                teamRepoMock.Setup(x => x.GetById(team.Id)).Returns(team);
            }
        }
        private Team CreateTeam(DateTime lastThroughputUpdateTime)
        {
            return new Team
            {
                Id = idCounter++,
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
                },
                UpdateTime = lastThroughputUpdateTime
            };
        }

        private TeamUpdater CreateSubject()
        {
            return new TeamUpdater(Mock.Of<ILogger<TeamUpdater>>(), ServiceScopeFactory, UpdateQueueService);
        }
    }
}
