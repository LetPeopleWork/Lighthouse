using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Update
{
    public class TeamUpdateServiceTest : UpdateServiceTestBase
    {
        private Mock<IWorkItemService> workItemServiceMock = new Mock<IWorkItemService>();
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IWorkItemRepository> workItemRepoMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            teamRepoMock = new Mock<IRepository<Team>>();
            workItemRepoMock = new Mock<IWorkItemRepository>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);
            SetupServiceProviderMock(workItemServiceFactoryMock.Object);

            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(workItemRepoMock.Object);
            SetupServiceProviderMock(teamMetricsServiceMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeams()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeams()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team2.Id, It.IsAny<Func<IServiceProvider, Task>>()));
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

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team2.Id, It.IsAny<Func<IServiceProvider, Task>>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_InvalidatesMetricForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamMetricsServiceMock.Verify(x => x.InvalidateTeamMetrics(team), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_RefreshesUpdateTimeForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Assert.That(team.TeamUpdateTime, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public async Task ExecuteAsync_TeamHasAutomaticallyAdjustFeatureWIPSetting_SetsFeatureWIPToRealWIP()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = true;

            var featuresInProgress = new List<Feature>
            {
                new Feature(team, 1),
                new Feature(team, 1),
                new Feature(team, 1),
            };

            teamMetricsServiceMock.Setup(x => x.GetCurrentFeaturesInProgressForTeam(team)).Returns(featuresInProgress);

            SetupRefreshSettings(10, 360);
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Assert.That(team.FeatureWIP, Is.EqualTo(3));
        }

        [Test]
        public async Task ExecuteAsync_TeamHasNoAutomaticallyAdjustFeatureWIPSetting_DoesNotChangeWIP()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = false;

            var featuresInProgress = new List<Feature>
            {
                new Feature(team, 1),
                new Feature(team, 1),
                new Feature(team, 1),
            };

            teamMetricsServiceMock.Setup(x => x.GetCurrentFeaturesInProgressForTeam(team)).Returns(featuresInProgress);

            SetupRefreshSettings(10, 360);
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Assert.That(team.FeatureWIP, Is.EqualTo(2));
        }

        [Test]
        public async Task ExecuteAsync_TeamHasAutomaticallyAdjustFeatureWIPSetting_NewFeatureWIPIsInvalide_DoesNotChangeFeatureWIP()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = true;

            teamMetricsServiceMock.Setup(x => x.GetCurrentFeaturesInProgressForTeam(team)).Returns(Enumerable.Empty<Feature>());

            SetupRefreshSettings(10, 360);
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Assert.That(team.FeatureWIP, Is.EqualTo(2));
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetThroughputRefreshSettings()).Returns(refreshSettings);
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
                TeamUpdateTime = lastThroughputUpdateTime
            };
        }

        private TeamUpdateService CreateSubject()
        {
            return new TeamUpdateService(Mock.Of<ILogger<TeamUpdateService>>(), ServiceScopeFactory, UpdateQueueService);
        }
    }
}
