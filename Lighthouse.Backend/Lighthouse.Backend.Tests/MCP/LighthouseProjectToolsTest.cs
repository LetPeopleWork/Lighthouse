using Lighthouse.Backend.MCP;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Moq;
using Newtonsoft.Json;
using NuGet.Protocol;

namespace Lighthouse.Backend.Tests.MCP
{
    public class LighthouseProjectToolsTest : LighthosueToolsBaseTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            SetupServiceProviderMock(projectRepositoryMock.Object);
        }

        [Test]
        public void GetAllProjects_ReturnsIdAndNameAndCounts()
        {
            var project = CreateProject();

            projectRepositoryMock.Setup(x => x.GetAll()).Returns(new List<Project> { project });

            var subject = CreateSubject();
            var result = subject.GetAllProjects();

            using (Assert.EnterMultipleScope())
            {
                var projects = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(projects, Has.Count.EqualTo(1));

                var projectToVerify = projects.Single();
                int projectId = Convert.ToInt32(projectToVerify.Id);
                string projectName = Convert.ToString(projectToVerify.Name);
                int teamCount = Convert.ToInt32(projectToVerify.TeamCount);
                int featureCount = Convert.ToInt32(projectToVerify.FeatureCount);
                int milestoneCount = Convert.ToInt32(projectToVerify.MilestoneCount);

                Assert.That(projectId, Is.EqualTo(project.Id));
                Assert.That(projectName, Is.EqualTo(project.Name));
                Assert.That(teamCount, Is.EqualTo(project.Teams.Count));
                Assert.That(featureCount, Is.EqualTo(project.Features.Count));
                Assert.That(milestoneCount, Is.EqualTo(project.Milestones.Count));
            }
        }

        [Test]
        public void GetProjectByName_WithExistingProject_ReturnsProjectDetails()
        {
            var project = CreateProject();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectByName(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var projectData = result.FromJson<dynamic>();

                int projectId = Convert.ToInt32(projectData.Id);
                string projectName = Convert.ToString(projectData.Name);

                Assert.That(projectId, Is.EqualTo(project.Id));
                Assert.That(projectName, Is.EqualTo(project.Name));
            }
        }

        [Test]
        public void GetProjectByName_WithNonExistingProject_ReturnsNotFoundMessage()
        {
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns((Project?)null);

            var subject = CreateSubject();
            var result = subject.GetProjectByName("NonExistentProject");

            Assert.That(result, Is.EqualTo("No project found with name NonExistentProject"));
        }

        [Test]
        public void GetProjectFeatures_WithExistingProject_ReturnsFeatures()
        {
            var project = CreateProjectWithFeatures();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectFeatures(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var features = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(features, Has.Count.EqualTo(1));

                var featureToVerify = features.Single();
                int featureId = Convert.ToInt32(featureToVerify.Id);
                string featureName = Convert.ToString(featureToVerify.Name);

                Assert.That(featureId, Is.EqualTo(project.Features[0].Id));
                Assert.That(featureName, Is.EqualTo(project.Features[0].Name));
            }
        }

        [Test]
        public void GetProjectTeams_WithExistingProject_ReturnsTeams()
        {
            var project = CreateProjectWithTeams();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectTeams(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var teams = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(teams, Has.Count.EqualTo(1));

                var teamToVerify = teams.Single();
                int teamId = Convert.ToInt32(teamToVerify.Id);
                string teamName = Convert.ToString(teamToVerify.Name);

                Assert.That(teamId, Is.EqualTo(project.Teams[0].Id));
                Assert.That(teamName, Is.EqualTo(project.Teams[0].Name));
            }
        }

        private LighthouseProjectTools CreateSubject()
        {
            return new LighthouseProjectTools(ServiceScopeFactory);
        }

        private Project CreateProject()
        {
            return new Project
            {
                Id = 1,
                Name = "Test Project"
            };
        }

        private Project CreateProjectWithFeatures()
        {
            var project = CreateProject();
            var feature = new Feature
            {
                Id = 1,
                Name = "Test Feature",
                ReferenceId = "TEST-1",
                State = "Active",
                StateCategory = StateCategories.Doing,
                OwningTeam = "Development Team",
                Url = "https://example.com/feature/1"
            };
            project.Features.Add(feature);
            return project;
        }

        private Project CreateProjectWithTeams()
        {
            var project = CreateProject();
            var team = new Team
            {
                Id = 1,
                Name = "Development Team",
                WorkTrackingSystemConnectionId = 1
            };
            project.Teams.Add(team);
            return project;
        }
    }
}