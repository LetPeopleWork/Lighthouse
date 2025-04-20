using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.TeamData
{
    public class TeamDataServiceTest
    {
        private Mock<IWorkTrackingConnector> workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
        private Mock<IWorkItemRepository> workItemRepoMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workItemRepoMock = new Mock<IWorkItemRepository>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task ExecuteAsync_InvalidatesMetricForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            teamMetricsServiceMock.Verify(x => x.InvalidateTeamMetrics(team), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_RefreshesUpdateTimeForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

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

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

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

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            Assert.That(team.FeatureWIP, Is.EqualTo(2));
        }

        [Test]
        public async Task ExecuteAsync_TeamHasAutomaticallyAdjustFeatureWIPSetting_NewFeatureWIPIsInvalide_DoesNotChangeFeatureWIP()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = true;

            teamMetricsServiceMock.Setup(x => x.GetCurrentFeaturesInProgressForTeam(team)).Returns([]);

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            Assert.That(team.FeatureWIP, Is.EqualTo(2));
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

        private TeamDataService CreateSubject()
        {
            var workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorServiceMock.Object);

            return new TeamDataService(Mock.Of<ILogger<TeamDataService>>(), workTrackingConnectorFactoryMock.Object, workItemRepoMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
