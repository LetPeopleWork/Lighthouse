using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using static Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear.LinearWorkTrackingConnector.LinearResponses;

namespace Lighthouse.Backend.Tests.API
{
    public class TagsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Project>> projectRepositoryMock;

        private List<Team> teams;
        private List<Project> projects;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            projectRepositoryMock = new Mock<IRepository<Project>>();

            teams = new List<Team>();
            projects = new List<Project>();

            teamRepositoryMock.Setup(repo => repo.GetAll()).Returns(teams);
            projectRepositoryMock.Setup(repo => repo.GetAll()).Returns(projects);
        }

        [Test]
        public void GetTags_NoTeams_NoProjects_ReturnsEmptyList()
        {
            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();
            
            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void GetTags_TeamWithTag_ReturnsTag()
        {
            var tag = "FirstClubInTown";
            var team = CreateTeam(tag);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(1));
                Assert.That(tags, Has.Member(tag));
            });
        }

        [Test]
        public void GetTags_TeamWithMultipleTags_ReturnsAllTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team = CreateTeam(tag1, tag2);
            var tagsController = CreateSubject();
            
            var response = tagsController.GetAllTags();
            
            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_TeamsWithSimilarTags_ReturnsUniqueTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team1 = CreateTeam(tag1, tag2);
            var team2 = CreateTeam(tag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_TeamsWithDifferentTags_ReturnsAllUniqueTags()
        {
            var tag1 = "Blue";
            var tag2 = "White";

            var team1 = CreateTeam(tag1);
            var team2 = CreateTeam(tag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_NoProjects_ReturnsEmptyList()
        {
            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void GetTags_ProjectWithTag_ReturnsTag()
        {
            var tag = "ProjectAlpha";
            var project = CreateProject(tag);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(1));
                Assert.That(tags, Has.Member(tag));
            });
        }

        [Test]
        public void GetTags_ProjectWithMultipleTags_ReturnsAllTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project = CreateProject(tag1, tag2);
            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_ProjectsWithSimilarTags_ReturnsUniqueTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project1 = CreateProject(tag1, tag2);
            var project2 = CreateProject(tag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_ProjectsWitDifferentTags_ReturnsUniqueTags()
        {
            var tag1 = "Red";
            var tag2 = "Green";

            var project1 = CreateProject(tag1);
            var project2 = CreateProject(tag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Member(tag1));
                Assert.That(tags, Has.Member(tag2));
            });
        }

        [Test]
        public void GetTags_TeamsAndProjectsWithOverlappingTags_ReturnsUniqueCombinedTags()
        {
            var teamTag1 = "TeamTag1";
            var teamTag2 = "SharedTag";
            var projectTag1 = "ProjectTag1";
            var projectTag2 = "SharedTag";

            var team = CreateTeam(teamTag1, teamTag2);
            var project = CreateProject(projectTag1, projectTag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(3));
                Assert.That(tags, Has.Member(teamTag1));
                Assert.That(tags, Has.Member(projectTag1));
                Assert.That(tags, Has.Member(projectTag2));
            });
        }

        [Test]
        public void GetTags_TeamsAndProjectsWithDistinctTags_ReturnsAllUniqueTags()
        {
            var teamTag1 = "TeamTag1";
            var teamTag2 = "TeamTag2";
            var projectTag1 = "ProjectTag1";
            var projectTag2 = "ProjectTag2";

            var team = CreateTeam(teamTag1, teamTag2);
            var project = CreateProject(projectTag1, projectTag2);

            var tagsController = CreateSubject();

            var response = tagsController.GetAllTags();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.Not.Null);
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var tags = (List<string>)((OkObjectResult)response.Result).Value;
                Assert.That(tags, Has.Count.EqualTo(4));
                Assert.That(tags, Has.Member(teamTag1));
                Assert.That(tags, Has.Member(teamTag2));
                Assert.That(tags, Has.Member(projectTag1));
                Assert.That(tags, Has.Member(projectTag2));
            });
        }

        private Project CreateProject(params string[] tags)
        {
            var project = new Project
            {
                Id = 1,
                Name = "Test Project",
                Tags = tags.ToList()
            };

            projects.Add(project);

            return project;
        }

        private Team CreateTeam(params string[] tags)
        {
            var team = new Team
            {
                Id = 1,
                Name = "Test Team",
                Tags = tags.ToList()
            };

            teams.Add(team);

            return team;
        }

        private TagsController CreateSubject()
        {
            return new TagsController(
                Mock.Of<ILogger<TagsController>>(),
                teamRepositoryMock.Object,
                projectRepositoryMock.Object);
        }
    }
}
