using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ProjectsControllerTest
    {
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;

        private Mock<IProjectUpdater> workItemCollectorServiceMock;
        
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepoMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            workItemCollectorServiceMock = new Mock<IProjectUpdater>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingSystemConnectionRepoMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
        }

        [Test]
        public void GetProjects_ReturnsAllProjectsFromRepository()
        {
            var testProjects = GetTestProjects();
            projectRepoMock.Setup(x => x.GetAll()).Returns(testProjects);

            var subject = CreateSubject();

            var result = subject.GetProjects().ToList();

            Assert.That(result, Has.Count.EqualTo(testProjects.Count));
        }

        [Test]
        public void GetProject_ReturnsSpecificProject()
        {
            var testProjects = GetTestProjects();
            var testProject = testProjects[testProjects.Count - 1];
            projectRepoMock.Setup(x => x.GetById(42)).Returns(testProject);

            var subject = CreateSubject();

            var result = subject.Get(42);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var projectDto = okResult.Value as ProjectDto;

                Assert.That(projectDto.Id, Is.EqualTo(testProject.Id));
                Assert.That(projectDto.Name, Is.EqualTo(testProject.Name));
            });
        }

        [Test]
        public void GetProject_ProjectNotFound_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.Get(1337);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_ProjectExists_UpdatesAndRefreshesForecasts()
        {
            var testProjects = GetTestProjects();
            var testProject = testProjects[testProjects.Count - 1];
            projectRepoMock.Setup(x => x.GetById(42)).Returns(testProject);

            var subject = CreateSubject();

            var result = subject.UpdateFeaturesForProject(42);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkResult>());

                var okResult = result as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                workItemCollectorServiceMock.Verify(x => x.TriggerUpdate(testProject.Id));
            });
        }

        [Test]
        public async Task Delete_RemovesTeamAndSaves()
        {
            var projectId = 12;

            var subject = CreateSubject();

            await subject.DeleteProject(projectId);

            projectRepoMock.Verify(x => x.Remove(projectId));
            projectRepoMock.Verify(x => x.Save());
        }

        [Test]
        public void GetProjectSettings_ProjectExists_ReturnsSettings()
        {
            var project = new Project
            {
                Id = 12,
                Name = "El Projecto",
                WorkItemTypes = new List<string> { "Bug", "Feature" },
                WorkItemQuery = "SELECT * FROM WorkItems",
                UnparentedItemsQuery = "SELECT * FROM UnparentedItems",
                DefaultAmountOfWorkItemsPerFeature = 5,
                WorkTrackingSystemConnectionId = 101
            };
            project.Milestones.AddRange([
                new Milestone { Id = 1, Name = "Milestone 1" },
                new Milestone { Id = 2, Name = "Milestone 2" },
            ]);

            projectRepoMock.Setup(x => x.GetById(12)).Returns(project);

            var subject = CreateSubject();

            var result = subject.GetProjectSettings(12);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<ProjectSettingDto>());
                var projectSettingDto = okObjectResult.Value as ProjectSettingDto;

                Assert.That(projectSettingDto.Id, Is.EqualTo(project.Id));
                Assert.That(projectSettingDto.Name, Is.EqualTo(project.Name));
                Assert.That(projectSettingDto.WorkItemTypes, Is.EqualTo(project.WorkItemTypes));
                Assert.That(projectSettingDto.Milestones, Has.Count.EqualTo(project.Milestones.Count));
                for (int i = 0; i < project.Milestones.Count; i++)
                {
                    Assert.That(projectSettingDto.Milestones[i].Id, Is.EqualTo(project.Milestones[i].Id));
                    Assert.That(projectSettingDto.Milestones[i].Name, Is.EqualTo(project.Milestones[i].Name));
                }
                Assert.That(projectSettingDto.WorkItemQuery, Is.EqualTo(project.WorkItemQuery));
                Assert.That(projectSettingDto.UnparentedItemsQuery, Is.EqualTo(project.UnparentedItemsQuery));
                Assert.That(projectSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(project.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(projectSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(project.WorkTrackingSystemConnectionId));
            });
        }


        [Test]
        public void GetProjectSettings_ProjectNotFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = subject.GetProjectSettings(1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public async Task CreateProject_GivenNewProjectSettings_CreatesProjectAsync()
        {
            var newProjectSettings = new ProjectSettingDto
            {
                Name = "New Project",
                WorkItemTypes = new List<string> { "Bug", "Feature" },
                Milestones = new List<MilestoneDto>
                {
                    new MilestoneDto { Id = 1, Name = "Milestone 1" },
                    new MilestoneDto { Id = 2, Name = "Milestone 2" }
                },
                WorkItemQuery = "SELECT * FROM WorkItems",
                UnparentedItemsQuery = "SELECT * FROM UnparentedItems",
                DefaultAmountOfWorkItemsPerFeature = 5,
                WorkTrackingSystemConnectionId = 101,
                ToDoStates = new List<string> { "To Do " },
                DoingStates = new List<string> { " In Progress" },
                DoneStates = new List<string> { "Done" },
            };

            var subject = CreateSubject();

            var result = await subject.CreateProject(newProjectSettings);

            projectRepoMock.Verify(x => x.Add(It.IsAny<Project>()));
            projectRepoMock.Verify(x => x.Save());

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<ProjectSettingDto>());
                var projectSettingDto = okObjectResult.Value as ProjectSettingDto;

                Assert.That(projectSettingDto.Name, Is.EqualTo(newProjectSettings.Name));
                Assert.That(projectSettingDto.WorkItemTypes, Is.EqualTo(newProjectSettings.WorkItemTypes));
                Assert.That(projectSettingDto.Milestones, Has.Count.EqualTo(newProjectSettings.Milestones.Count));

                for (int i = 0; i < newProjectSettings.Milestones.Count; i++)
                {
                    Assert.That(projectSettingDto.Milestones[i].Id, Is.EqualTo(newProjectSettings.Milestones[i].Id));
                    Assert.That(projectSettingDto.Milestones[i].Name, Is.EqualTo(newProjectSettings.Milestones[i].Name));
                }

                Assert.That(projectSettingDto.WorkItemQuery, Is.EqualTo(newProjectSettings.WorkItemQuery));
                Assert.That(projectSettingDto.UnparentedItemsQuery, Is.EqualTo(newProjectSettings.UnparentedItemsQuery));
                Assert.That(projectSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(newProjectSettings.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(projectSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(newProjectSettings.WorkTrackingSystemConnectionId));

                Assert.That(projectSettingDto.ToDoStates, Contains.Item("To Do"));
                Assert.That(projectSettingDto.DoingStates, Contains.Item("In Progress"));
                Assert.That(projectSettingDto.DoneStates, Contains.Item("Done"));
            });
        }

        [Test]
        public async Task UpdateProject_GivenNewProjectSettings_UpdatesProjectAsync()
        {
            var existingProject = new Project { Id = 132 };
            var existingTeam = new Team { Id = 42, Name = "My Team" };

            projectRepoMock.Setup(x => x.GetById(132)).Returns(existingProject);
            teamRepoMock.Setup(x => x.GetById(42)).Returns(existingTeam);

            var updatedProjectSettings = new ProjectSettingDto
            {
                Id = 132,
                Name = "Updated Project",
                WorkItemTypes = new List<string> { "Feature", "Bug" },
                Milestones = new List<MilestoneDto>
                {
                    new MilestoneDto { Id = 1, Name = "Updated Milestone 1" },
                    new MilestoneDto { Id = 2, Name = "Updated Milestone 2" }
                },
                WorkItemQuery = "SELECT * FROM UpdatedWorkItems",
                UnparentedItemsQuery = "SELECT * FROM UpdatedUnparentedItems",
                DefaultAmountOfWorkItemsPerFeature = 10,
                WorkTrackingSystemConnectionId = 202,
                SizeEstimateField = "NewField",
                InvolvedTeams = new List<TeamDto>
                {
                    new TeamDto(existingTeam)
                },
                OwningTeam = new TeamDto(existingTeam),
                FeatureOwnerField = "OwnerField",
            };

            var subject = CreateSubject();

            var result = await subject.UpdateProject(132, updatedProjectSettings);

            projectRepoMock.Verify(x => x.Update(existingProject));
            projectRepoMock.Verify(x => x.Save());

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<ProjectSettingDto>());
                var projectSettingDto = okObjectResult.Value as ProjectSettingDto;

                Assert.That(projectSettingDto.Id, Is.EqualTo(updatedProjectSettings.Id));
                Assert.That(projectSettingDto.Name, Is.EqualTo(updatedProjectSettings.Name));
                Assert.That(projectSettingDto.WorkItemTypes, Is.EqualTo(updatedProjectSettings.WorkItemTypes));
                Assert.That(projectSettingDto.Milestones, Has.Count.EqualTo(updatedProjectSettings.Milestones.Count));
                
                for (int i = 0; i < updatedProjectSettings.Milestones.Count; i++)
                {
                    Assert.That(projectSettingDto.Milestones[i].Id, Is.EqualTo(updatedProjectSettings.Milestones[i].Id));
                    Assert.That(projectSettingDto.Milestones[i].Name, Is.EqualTo(updatedProjectSettings.Milestones[i].Name));
                }

                Assert.That(projectSettingDto.WorkItemQuery, Is.EqualTo(updatedProjectSettings.WorkItemQuery));
                Assert.That(projectSettingDto.UnparentedItemsQuery, Is.EqualTo(updatedProjectSettings.UnparentedItemsQuery));
                Assert.That(projectSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(updatedProjectSettings.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(projectSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(updatedProjectSettings.WorkTrackingSystemConnectionId));
                Assert.That(projectSettingDto.SizeEstimateField, Is.EqualTo(updatedProjectSettings.SizeEstimateField));

                Assert.That(projectSettingDto.InvolvedTeams, Has.Count.EqualTo(1));
                var teamDto = projectSettingDto.InvolvedTeams.Single();
                Assert.That(teamDto.Id, Is.EqualTo(existingTeam.Id));
                Assert.That(teamDto.Name, Is.EqualTo(existingTeam.Name));

                Assert.That(projectSettingDto.OwningTeam.Id, Is.EqualTo(existingTeam.Id));
                Assert.That(projectSettingDto.OwningTeam.Name, Is.EqualTo(existingTeam.Name));
                Assert.That(projectSettingDto.FeatureOwnerField, Is.EqualTo(updatedProjectSettings.FeatureOwnerField));
            });
        }

        [Test]
        public async Task UpdateProject_ProjectNotFound_ReturnsNotFoundResultAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateProject(1, new ProjectSettingDto());

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ValidateTeamSettings_GivenTeamSettings_ReturnsResultFromWorkItemService(bool expectedResult)
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var projectSettings = new ProjectSettingDto { WorkTrackingSystemConnectionId = 1886 };

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(1886)).Returns(workTrackingSystemConnection);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystemConnection.WorkTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);
            workTrackingConnectorServiceMock.Setup(x => x.ValidateProjectSettings(It.IsAny<Project>())).ReturnsAsync(expectedResult);

            var subject = CreateSubject();

            var response = await subject.ValidateProjectSettings(projectSettings);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = response.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                var value = okObjectResult.Value;
                Assert.That(value, Is.EqualTo(expectedResult));
            });
        }

        [Test]
        public async Task ValidateTeamSettings_WorkTrackingSystemNotFound_ReturnsNotFound()
        {
            var projectSettings = new ProjectSettingDto { WorkTrackingSystemConnectionId = 1886 };

            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(1886)).Returns((WorkTrackingSystemConnection)null);

            var subject = CreateSubject();

            var response = await subject.ValidateProjectSettings(projectSettings);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundObjectResult = response.Result as NotFoundResult;
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
            });
        }

        private ProjectsController CreateSubject()
        {
            return new ProjectsController(
                projectRepoMock.Object,
                teamRepoMock.Object,
                workItemCollectorServiceMock.Object,
                workTrackingConnectorFactoryMock.Object,
                workTrackingSystemConnectionRepoMock.Object
            );
        }

        private List<Project> GetTestProjects()
        {
            return new List<Project>
            {
                new Project { Id = 12, Name = "Foo" },
                new Project { Id = 42, Name = "Bar" }
            };
        }
    }
}
