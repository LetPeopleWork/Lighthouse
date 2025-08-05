﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class ProjectUpdaterTest : UpdateServiceTestBase
    {
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IProjectMetricsService> projectMetricsServiceMock;
        private Mock<ILicenseService> licenseServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void SetUp()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = new Mock<IForecastService>();
            workItemServiceMock = new Mock<IWorkItemService>();
            projectMetricsServiceMock = new Mock<IProjectMetricsService>();
            licenseServiceMock = new Mock<ILicenseService>();

            SetupServiceProviderMock(projectRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(workItemServiceMock.Object);
            SetupServiceProviderMock(projectMetricsServiceMock.Object);
            SetupServiceProviderMock(licenseServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public void UpdateProject_TriggersFeatureUpdateForProject()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            workItemServiceMock.Verify(x => x.UpdateFeaturesForProject(project));
        }

        [Test]
        public void UpdateProject_TriggersReforecast()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForProject(project));
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllProjectsAsync()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_InvalidatesProjectMetricsAfterUpdate()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            projectMetricsServiceMock.Verify(x => x.InvalidateProjectMetrics(project));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesAllProjectsAsync()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project1, project2);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesOnlyProjectsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now);

            SetupRefreshSettings(10, 360);

            SetupProjects(project1, project2);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_NoPremiumLicense_MoreThanOneProject_DoesNotRefresh()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);

            SetupProjects([project, CreateProject(DateTime.Now)]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemServiceMock.Verify(x => x.UpdateFeaturesForProject(project), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanOneProject_Refreshes()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupRefreshSettings(10, 360);
            SetupProjects([project, CreateProject(DateTime.Now)]);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemServiceMock.Verify(x => x.UpdateFeaturesForProject(project), Times.Once);
        }

        private void SetupProjects(params Project[] projects)
        {
            projectRepoMock.Setup(x => x.GetAll()).Returns(projects);

            foreach (var project in projects)
            {
                projectRepoMock.Setup(x => x.GetById(project.Id)).Returns(project);
            }
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetFeaturRefreshSettings()).Returns(refreshSettings);
        }


        private Team CreateTeam()
        {
            var team = new Team { Name = "Team", Id = idCounter++ };

            team.WorkItemTypes.Add("User Story");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            return team;
        }

        private Project CreateProject(params Team[] teams)
        {
            return CreateProject(DateTime.Now, teams);
        }

        private Project CreateProject(DateTime lastUpdateTime, params Team[] teams)
        {
            var project = new Project
            {
                Id = idCounter++,
                Name = "Release 1",
            };

            project.WorkItemTypes.Add("Feature");
            project.UpdateTeams(teams);

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            project.WorkTrackingSystemConnection = workTrackingConnection;

            project.UpdateTime = lastUpdateTime;

            return project;
        }

        private ProjectUpdater CreateSubject()
        {
            return new ProjectUpdater(Mock.Of<ILogger<ProjectUpdater>>(), ServiceScopeFactory, UpdateQueueService);
        }
    }
}
