using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ProjectsControllerTest
    {
        private Mock<IRepository<Project>> projectRepoMock;

        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;

        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public void GetProjects_ReturnsAllProjectsFromRepository()
        {
            var testProjects = GetTestProjects();
            projectRepoMock.Setup(x => x.GetAll()).Returns(testProjects);

            var subject = CreateSubject();

            var result = subject.GetProjects().ToList();

            Assert.That(result, Has.Count.EqualTo(testProjects.Count()));
        }

        [Test]
        public void GetProject_ReturnsSpecificProject()
        {
            var testProject = GetTestProjects().Last();
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
        public async Task UpdateFeaturesForProject_ProjectExists_UpdatesAndRefreshesForecasts()
        {
            var testProject = GetTestProjects().Last();
            projectRepoMock.Setup(x => x.GetById(42)).Returns(testProject);

            var subject = CreateSubject();

            var result = await subject.UpdateFeaturesForProject(42);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());

                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(testProject));
                monteCarloServiceMock.Verify(x => x.UpdateForecastsForProject(testProject));
            });
        }

        [Test]
        public async Task UpdateFeaturesForProject_ProjectNotFound_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateFeaturesForProject(1337);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void Delete_RemovesTeamAndSaves()
        {
            var projectId = 12;

            var subject = CreateSubject();

            subject.DeleteProject(projectId);

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
                WorkTrackingSystemConnectionId = 101
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
            });
        }

        [Test]
        public async Task UpdateProject_GivenNewProjectSettings_UpdatesProjectAsync()
        {
            var existingProject = new Project { Id = 132 };

            projectRepoMock.Setup(x => x.GetById(132)).Returns(existingProject);

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

        private ProjectsController CreateSubject()
        {
            return new ProjectsController(projectRepoMock.Object, workItemCollectorServiceMock.Object, monteCarloServiceMock.Object);
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
