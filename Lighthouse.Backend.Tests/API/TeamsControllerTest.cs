using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class TeamsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
        }

        [Test]
        public void GetTeams_SingleTeam_NoProjects()
        {
            var team = CreateTeam(1, "Numero Uno");

            var subject = CreateSubject([team]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects.Count, Is.EqualTo(0));
                Assert.That(result.Features.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void GetTeams_SingleTeam_SingleProject_SingleFeature()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project = CreateProject(42, "My Project");
            var feature = CreateFeature(project, team, 12);

            var subject = CreateSubject([team], [project], [feature]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects.Count, Is.EqualTo(1));
                Assert.That(result.Features.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void GetTeams_SingleTeam_SingleProject_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project = CreateProject(42, "My Project");
            var feature1 = CreateFeature(project, team, 12);
            var feature2 = CreateFeature(project, team, 42);

            var subject = CreateSubject([team], [project], [feature1, feature2]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects.Count, Is.EqualTo(1));
                Assert.That(result.Features.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void GetTeams_SingleTeam_MultipleProjects_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project1 = CreateProject(42, "My Project");
            var feature1 = CreateFeature(project1, team, 12);
            var feature2 = CreateFeature(project1, team, 42);

            var project2 = CreateProject(13, "My Other Project");
            var feature3 = CreateFeature(project2, team, 5);

            var subject = CreateSubject([team], [project1, project2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Projects.Count, Is.EqualTo(2));
                Assert.That(result.Features.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void GetTeams_MultipleTeams_MultipleProjects_MultipleFeatures()
        {
            var team1 = CreateTeam(1, "Numero Uno");
            var project1 = CreateProject(42, "My Project");
            var feature1 = CreateFeature(project1, team1, 12);
            var feature2 = CreateFeature(project1, team1, 42);

            var team2 = CreateTeam(2, "Una Mas");
            var project2 = CreateProject(13, "My Other Project");
            var feature3 = CreateFeature(project2, team2, 5);

            var subject = CreateSubject([team1, team2], [project1, project2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var team1Results = results.First();
            var team2Results = results.Last();
            Assert.Multiple(() =>
            {
                Assert.That(team1Results.Id, Is.EqualTo(1));
                Assert.That(team2Results.Id, Is.EqualTo(2));

                Assert.That(team1Results.Name, Is.EqualTo("Numero Uno"));
                Assert.That(team2Results.Name, Is.EqualTo("Una Mas"));

                Assert.That(team1Results.Projects.Count, Is.EqualTo(1));
                Assert.That(team2Results.Projects.Count, Is.EqualTo(1));

                Assert.That(team1Results.Features.Count, Is.EqualTo(2));
                Assert.That(team2Results.Features.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void Delete_RemovesTeamAndSaves()
        {
            var teamId = 12;

            var subject = CreateSubject();

            subject.DeleteTeam(teamId);

            teamRepositoryMock.Verify(x => x.Remove(teamId));
            teamRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public void GetTeam_TeamExists_ReturnsTeam()
        {
            var team = CreateTeam(1, "Numero Uno");
            var project1 = CreateProject(42, "My Project");
            var feature1 = CreateFeature(project1, team, 12);
            var feature2 = CreateFeature(project1, team, 42);

            var project2 = CreateProject(13, "My Other Project");
            var feature3 = CreateFeature(project2, team, 5);

            teamRepositoryMock.Setup(x => x.GetById(1)).Returns(team);

            var subject = CreateSubject([team], [project1, project2], [feature1, feature2, feature3]);

            var result = subject.GetTeam(1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var returnedTeamDto = okResult.Value as TeamDto;

                Assert.That(returnedTeamDto.Id, Is.EqualTo(1));
                Assert.That(returnedTeamDto.Name, Is.EqualTo("Numero Uno"));
                Assert.That(returnedTeamDto.Projects.Count, Is.EqualTo(2));
                Assert.That(returnedTeamDto.Features.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void GetTeam_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.GetTeam(1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetTeamSettings_TeamExists_ReturnsSettings()
        {
            var team = CreateTeam(12, "El Teamo");
            team.ThroughputHistory = 42;
            team.FeatureWIP = 3;
            team.WorkItemQuery = "SELECT * FROM *";
            team.WorkTrackingSystemConnectionId = 37;
            team.AdditionalRelatedField = "Custom.RelatedItem";

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var result = subject.GetTeamSettings(12);

            Assert.Multiple(() =>
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
                Assert.That(teamSettingDto.RelationCustomField, Is.EqualTo(team.AdditionalRelatedField));
            });
        }

        [Test]
        public void GetTeamSettings_TeamNotFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = subject.GetTeamSettings(1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
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
                RelationCustomField = "CUSTOM.AdditionalField"
            };

            var subject = CreateSubject();

            var result = await subject.CreateTeamAsync(newTeamSettings);

            teamRepositoryMock.Verify(x => x.Add(It.IsAny<Team>()));
            teamRepositoryMock.Verify(x => x.Save());

            Assert.Multiple(() =>
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
                Assert.That(teamSettingDto.RelationCustomField, Is.EqualTo(newTeamSettings.RelationCustomField));
            });
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
                RelationCustomField = "CUSTOM.AdditionalField"
            };

            var subject = CreateSubject();

            var result = await subject.UpdateTeam(132, updatedTeamSettings);

            teamRepositoryMock.Verify(x => x.Update(existingTeam));
            teamRepositoryMock.Verify(x => x.Save());

            Assert.Multiple(() =>
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
                Assert.That(teamSettingDto.RelationCustomField, Is.EqualTo(updatedTeamSettings.RelationCustomField));
            });
        }


        [Test]
        public async Task UpdateTeam_TeamNotFound_ReturnsNotFoundResultAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateTeam(1, new TeamSettingDto());

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        private Team CreateTeam(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        private Project CreateProject(int id, string name)
        {
            return new Project { Id = id, Name = name };
        }

        private Feature CreateFeature(Project project, Team team, int remainingWork)
        {
            var feature = new Feature(team, remainingWork);
            project.Features.Add(feature);

            return feature;
        }

        private TeamsController CreateSubject(Team[]? teams = null, Project[]? projects = null, Feature[]? features = null)
        {
            teams ??= Array.Empty<Team>();
            projects ??= Array.Empty<Project>();
            features ??= Array.Empty<Feature>();

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            return new TeamsController(teamRepositoryMock.Object, projectRepositoryMock.Object, featureRepositoryMock.Object);
        }
    }
}
