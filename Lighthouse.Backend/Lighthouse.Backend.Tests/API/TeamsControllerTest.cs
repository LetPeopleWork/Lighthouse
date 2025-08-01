﻿using Lighthouse.Backend.API;
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
    public class TeamsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepoMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepositoryMock;

        private Mock<ITeamUpdater> teamUpdateServiceMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workItemRepoMock = new Mock<IWorkItemRepository>();
            workTrackingSystemConnectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();

            teamUpdateServiceMock = new Mock<ITeamUpdater>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
        }

        [Test]
        public void GetTeams_SingleTeam_NoProjects()
        {
            var team = CreateTeam(1, "Numero Uno");

            var subject = CreateSubject([team]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects, Has.Count.EqualTo(0));
                Assert.That(result.Features, Has.Count.EqualTo(0));
            };
        }

        [Test]
        public void GetTeams_SingleTeam_SingleProject_SingleFeature()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project = CreateProject(42, "My Project");
            project.UpdateTeams([team]);

            var feature = CreateFeature(project, team, 12);

            var subject = CreateSubject([team], [project], [feature]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects, Has.Count.EqualTo(1));
                Assert.That(result.Features, Has.Count.EqualTo(1));
            };
        }

        [Test]
        public void GetTeams_SingleTeam_SingleProject_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project = CreateProject(42, "My Project");
            project.UpdateTeams([team]);

            var feature1 = CreateFeature(project, team, 12);
            var feature2 = CreateFeature(project, team, 42);

            var subject = CreateSubject([team], [project], [feature1, feature2]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects, Has.Count.EqualTo(1));
                Assert.That(result.Features, Has.Count.EqualTo(2));
            };
        }

        [Test]
        public void GetTeams_SingleTeam_MultipleProjects_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project1 = CreateProject(42, "My Project");
            project1.UpdateTeams([team]);

            var feature1 = CreateFeature(project1, team, 12);
            var feature2 = CreateFeature(project1, team, 42);

            var project2 = CreateProject(13, "My Other Project");
            project2.UpdateTeams([team]);

            var feature3 = CreateFeature(project2, team, 5);

            var subject = CreateSubject([team], [project1, project2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects, Has.Count.EqualTo(2));
                Assert.That(result.Features, Has.Count.EqualTo(3));
            };
        }

        [Test]
        public void GetTeams_MultipleTeams_MultipleProjects_MultipleFeatures()
        {
            var team1 = CreateTeam(1, "Numero Uno");
            var project1 = CreateProject(42, "My Project");
            project1.UpdateTeams([team1]);

            var feature1 = CreateFeature(project1, team1, 12);
            var feature2 = CreateFeature(project1, team1, 42);

            var team2 = CreateTeam(2, "Una Mas");
            var project2 = CreateProject(13, "My Other Project");
            project2.UpdateTeams([team2]);

            var feature3 = CreateFeature(project2, team2, 5);

            var subject = CreateSubject([team1, team2], [project1, project2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var team1Results = results[0];
            var team2Results = results[results.Count - 1];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(team1Results.Id, Is.EqualTo(1));
                Assert.That(team2Results.Id, Is.EqualTo(2));

                Assert.That(team1Results.Name, Is.EqualTo("Numero Uno"));
                Assert.That(team2Results.Name, Is.EqualTo("Una Mas"));

                Assert.That(team1Results.Projects, Has.Count.EqualTo(1));
                Assert.That(team2Results.Projects, Has.Count.EqualTo(1));

                Assert.That(team1Results.Features, Has.Count.EqualTo(2));
                Assert.That(team2Results.Features, Has.Count.EqualTo(1));
            };
        }

        [Test]
        public async Task Delete_RemovesTeamAndSaves()
        {
            var teamId = 12;

            var subject = CreateSubject();

            await subject.DeleteTeam(teamId);

            teamRepositoryMock.Verify(x => x.Remove(teamId));
            teamRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public void GetTeam_TeamExists_ReturnsTeam()
        {
            var team = CreateTeam(1, "Numero Uno");

            var project1 = CreateProject(42, "My Project");
            project1.UpdateTeams([team]);

            var feature1 = CreateFeature(project1, team, 12);
            var feature2 = CreateFeature(project1, team, 42);

            var project2 = CreateProject(13, "My Other Project");
            project2.UpdateTeams([team]);


            var feature3 = CreateFeature(project2, team, 5);

            teamRepositoryMock.Setup(x => x.GetById(1)).Returns(team);

            var subject = CreateSubject([team], [project1, project2], [feature1, feature2, feature3]);

            var result = subject.GetTeam(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var returnedTeamDto = okResult.Value as TeamDto;

                Assert.That(returnedTeamDto.Id, Is.EqualTo(1));
                Assert.That(returnedTeamDto.Name, Is.EqualTo("Numero Uno"));
                Assert.That(returnedTeamDto.Projects, Has.Count.EqualTo(2));
                Assert.That(returnedTeamDto.Features, Has.Count.EqualTo(3));
            };
        }

        [Test]
        public void GetTeam_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.GetTeam(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetTeamSettings_TeamExists_ReturnsSettings()
        {
            var team = CreateTeam(12, "El Teamo");
            team.ThroughputHistory = 42;
            team.FeatureWIP = 3;
            team.WorkItemQuery = "SELECT * FROM *";
            team.WorkTrackingSystemConnectionId = 37;
            team.ParentOverrideField = "Custom.RelatedItem";
            team.ServiceLevelExpectationProbability = 75;
            team.ServiceLevelExpectationRange = 10;
            team.SystemWIPLimit = 5;

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var result = subject.GetTeamSettings(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Id, Is.EqualTo(team.Id));
                Assert.That(teamSettingDto.Name, Is.EqualTo(team.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(team.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(team.FeatureWIP));
                Assert.That(teamSettingDto.WorkItemQuery, Is.EqualTo(team.WorkItemQuery));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(team.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(team.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideField, Is.EqualTo(team.ParentOverrideField));
                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(team.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(team.ServiceLevelExpectationRange));
                Assert.That(teamSettingDto.SystemWIPLimit, Is.EqualTo(team.SystemWIPLimit));
            };
        }

        [Test]
        public void GetTeamSettings_TeamNotFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = subject.GetTeamSettings(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public async Task CreateTeam_GivenNewTeamSettings_CreatesTeamAsync()
        {
            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "project = MyProject",
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                ToDoStates = new List<string> { " To Do" },
                DoingStates = new List<string> { "Doing" },
                DoneStates = new List<string> { "Done " },
                ServiceLevelExpectationProbability = 50,
                ServiceLevelExpectationRange = 2,
                SystemWIPLimit = 3,
                BlockedStates = new List<string> { "Blocked" },
                BlockedTags = new List<string> { "Waiting", "Customer Input Needed" },
            };

            var subject = CreateSubject();

            var result = await subject.CreateTeam(newTeamSettings);

            teamRepositoryMock.Verify(x => x.Add(It.IsAny<Team>()));
            teamRepositoryMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Name, Is.EqualTo(newTeamSettings.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(newTeamSettings.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(newTeamSettings.FeatureWIP));
                Assert.That(teamSettingDto.WorkItemQuery, Is.EqualTo(newTeamSettings.WorkItemQuery));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(newTeamSettings.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(newTeamSettings.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideField, Is.EqualTo(newTeamSettings.ParentOverrideField));

                Assert.That(teamSettingDto.ToDoStates, Contains.Item("To Do"));
                Assert.That(teamSettingDto.DoingStates, Contains.Item("Doing"));
                Assert.That(teamSettingDto.DoneStates, Contains.Item("Done"));

                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(newTeamSettings.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(newTeamSettings.ServiceLevelExpectationRange));

                Assert.That(teamSettingDto.SystemWIPLimit, Is.EqualTo(newTeamSettings.SystemWIPLimit));

                Assert.That(teamSettingDto.BlockedStates, Contains.Item("Blocked"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Waiting"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Customer Input Needed"));
            }
            ;
        }

        [Test]
        public async Task UpdateTeam_GivenNewTeamSettings_UpdatesTeamAsync()
        {
            var existingTeam = new Team { Id = 132 };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "project = MyProject",
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
                ServiceLevelExpectationRange = 18,
                ServiceLevelExpectationProbability = 86,
                BlockedStates = new List<string> { "Waiting for Peter" },
                BlockedTags = new List<string> { "Customer Input Needed" },
            };

            var subject = CreateSubject();

            var result = await subject.UpdateTeam(132, updatedTeamSettings);

            teamRepositoryMock.Verify(x => x.Update(existingTeam));
            teamRepositoryMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Name, Is.EqualTo(updatedTeamSettings.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(updatedTeamSettings.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(updatedTeamSettings.FeatureWIP));
                Assert.That(teamSettingDto.WorkItemQuery, Is.EqualTo(updatedTeamSettings.WorkItemQuery));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(updatedTeamSettings.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(updatedTeamSettings.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideField, Is.EqualTo(updatedTeamSettings.ParentOverrideField));
                Assert.That(teamSettingDto.AutomaticallyAdjustFeatureWIP, Is.EqualTo(updatedTeamSettings.AutomaticallyAdjustFeatureWIP));
                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(updatedTeamSettings.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(updatedTeamSettings.ServiceLevelExpectationRange));
                Assert.That(teamSettingDto.BlockedStates, Contains.Item("Waiting for Peter"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Customer Input Needed"));
            }
            ;
        }

        [Test]
        public async Task UpdateTeam_GivenNewTeamSettings_ResetsLastUpdateTime()
        {
            var existingTeam = new Team { Id = 132, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "project = MyProject",
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            Assert.That(existingTeam.UpdateTime, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        [TestCase("New Query", true)]
        [TestCase("Existing Query", false)]
        public async Task UpdateTeam_GivenNewQuery_DeletesExistingWorkItems(string workItemQuery, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, WorkItemQuery = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = workItemQuery,
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(new string[] { "User Story", "Bug" }, false)]
        [TestCase(new string[] { "Bug", "User Story" }, false)]
        [TestCase(new string[] { "Story", "Bug" }, true)]
        [TestCase(new string[] { "User Story" }, true)]
        [TestCase(new string[] { "All New Type" }, true)]
        [TestCase(new string[] { "User Story", "Bug", "Task" }, true)]
        public async Task UpdateTeam_GivenWorkItemTypes_DeletesExistingWorkItems(string[] workItemTypes, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, WorkItemQuery = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "Existing Query",
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = [.. workItemTypes],
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(new string[] { "To Do" }, new string[] { "Doing" }, new string[] { "Done" }, false)]
        [TestCase(new string[] { "ToDo" }, new string[] { "Doing" }, new string[] { "Done" }, true)]
        [TestCase(new string[] { "To Do" }, new string[] { "Boing" }, new string[] { "Done" }, true)]
        [TestCase(new string[] { "To Do" }, new string[] { "Doing" }, new string[] { "Donny" }, true)]
        [TestCase(new string[] { "To Do", "New" }, new string[] { "Doing" }, new string[] { "Done" }, true)]
        [TestCase(new string[] { "To Do" }, new string[] { "Doing", "In Progress" }, new string[] { "Done" }, true)]
        [TestCase(new string[] { "To Do" }, new string[] { "Doing" }, new string[] { "Done", "Closed" }, true)]
        public async Task UpdateTeam_GivenChangedStates_DeletesExistingWorkItems(string[] toDoStates, string[] doingStates, string[] doneStates, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, WorkItemQuery = "Existing Query", ToDoStates = ["To Do"], DoingStates = ["Doing"], DoneStates = ["Done"], WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "Existing Query",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = toDoStates.ToList(),
                DoingStates = doingStates.ToList(),
                DoneStates = doneStates.ToList(),
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(2, false)]
        [TestCase(1, true)]
        public async Task UpdateTeam_GivenWorkTrackingSystemConnectionId_DeletesExistingWorkItems(int workTrackingSystemConnectionId, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, WorkItemQuery = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                WorkItemQuery = "Existing Query",
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = new List<string> { "User Story", "Bug" },
                WorkTrackingSystemConnectionId = workTrackingSystemConnectionId,
                ParentOverrideField = "CUSTOM.AdditionalField",
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        public async Task UpdateTeam_TeamNotFound_ReturnsNotFoundResultAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateTeam(1, new TeamSettingDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void UpdateTeamData_GivenTeamId_TriggersTeamUpdate()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            var subject = CreateSubject();

            var response = subject.UpdateTeamData(12);

            teamUpdateServiceMock.Verify(x => x.TriggerUpdate(expectedTeam.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());
                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            };
        }

        [Test]
        public void UpdateTeamData_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.UpdateTeamData(12);

            Assert.That(response, Is.InstanceOf<NotFoundObjectResult>());
            var notFoundObjectResult = response as NotFoundObjectResult;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
                var value = notFoundObjectResult.Value;
                Assert.That(value, Is.Null);
            };
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ValidateTeamSettings_GivenTeamSettings_ReturnsResultFromWorkItemService(bool expectedResult)
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetById(1886)).Returns(workTrackingSystemConnection);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystemConnection.WorkTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);            
            workTrackingConnectorServiceMock.Setup(x => x.ValidateTeamSettings(It.IsAny<Team>())).ReturnsAsync(expectedResult);

            var subject = CreateSubject();

            var response = await subject.ValidateTeamSettings(teamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                
                var okObjectResult = response.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                var value = okObjectResult.Value;
                Assert.That(value, Is.EqualTo(expectedResult));
            };
        }

        [Test]
        public async Task ValidateTeamSettings_WorkTrackingSystemNotFound_ReturnsNotFound()
        {
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };

            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetById(1886)).Returns((WorkTrackingSystemConnection)null);            

            var subject = CreateSubject();

            var response = await subject.ValidateTeamSettings(teamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
                
                var notFoundObjectResult = response.Result as NotFoundResult;
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
            };
        }

        private Team CreateTeam(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        private Project CreateProject(int id, string name)
        {
            return new Project { Id = id, Name = name };
        }

        private static Feature CreateFeature(Project project, Team team, int remainingWork)
        {
            var feature = new Feature(team, remainingWork);
            project.Features.Add(feature);

            return feature;
        }

        private TeamsController CreateSubject(Team[]? teams = null, Project[]? projects = null, Feature[]? features = null)
        {
            teams ??= [];
            projects ??= [];
            features ??= [];

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            return new TeamsController(teamRepositoryMock.Object, projectRepositoryMock.Object, featureRepositoryMock.Object, workTrackingSystemConnectionRepositoryMock.Object, workItemRepoMock.Object, teamUpdateServiceMock.Object, workTrackingConnectorFactoryMock.Object);
        }
    }
}
