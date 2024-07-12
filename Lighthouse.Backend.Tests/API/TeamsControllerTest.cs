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
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>(), "Result should be NotFoundResult");

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404), "Status code should be 404");
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
