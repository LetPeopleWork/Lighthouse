using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
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

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            teamRepoMock = new Mock<IRepository<Team>>();
            appSettingServiceMock = new Mock<IAppSettingService>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);
            SetupServiceProviderMock(workItemServiceFactoryMock.Object);

            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public void UpdateTeam_GetsClosedItemsFromWorkItemService_UpdatesThroughput()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.ThroughputHistory = 7;
            SetupTeams([team]);

            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];
            workItemServiceMock.Setup(x => x.GetThroughputForTeam(team)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

            Assert.Multiple(() =>
            {
                Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
                for (var index = 0; index < closedItemsPerDay.Length; index++)
                {
                    Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
                }
            });
        }

        [Test]
        [TestCase(new[] { "12" }, new[] { "12" })]
        [TestCase(new[] { "" }, new[] { "" })]
        [TestCase(new[] { "12", "12" }, new[] { "12" })]
        [TestCase(new[] { "12", "" }, new[] { "12", "" })]
        [TestCase(new[] { "12", "123", "34", "231324" }, new[] { "12", "123", "34", "231324" })]
        [TestCase(new string[0], new string[0])]
        public void UpdateTeam_GetsOpenItemsFromWorkItemService_SetsActualWip(string[] inProgressFeatures, string[] expectedFeaturesInProgress)
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            workItemServiceMock.Setup(x => x.GetFeaturesInProgressForTeam(team)).ReturnsAsync(inProgressFeatures);

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

            Assert.That(team.FeaturesInProgress, Is.EquivalentTo(expectedFeaturesInProgress));
        }

        [Test]
        [TestCase(true, new[] { "13", "37", "42" }, 3)]
        [TestCase(false, new[] { "13", "37", "42" }, 2)]
        public void UpdateTeam_UpdatesFeatureWIP_DependingOnSetting(bool automaticallyAdjustFeatureWIP, string[] inProgressFeatures, int expectedFeatureWIP)
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            workItemServiceMock.Setup(x => x.GetFeaturesInProgressForTeam(team)).ReturnsAsync(inProgressFeatures);
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = automaticallyAdjustFeatureWIP;

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

            Assert.That(team.FeatureWIP, Is.EqualTo(expectedFeatureWIP));
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeamsThroughputAsync()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Team, team.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeamsThroughputAsync()
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
