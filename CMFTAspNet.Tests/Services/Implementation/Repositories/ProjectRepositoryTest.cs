using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;
using CMFTAspNet.Tests.TestHelpers;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
{
    public class ProjectRepositoryTest : IntegrationTestBase
    {
        public ProjectRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public void GetProjects_NoProjects_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var projects = subject.GetAll();

            Assert.That(projects, Is.Empty);
        }

        [Test]
        public async Task AddProject_StoresAsync()
        {
            var subject = CreateSubject();
            var project = new Project { Name = "Name", SearchTerm = "Search" };

            subject.Add(project);
            await subject.Save();

            var projects = subject.GetAll();

            Assert.That(projects, Contains.Item(project));
        }

        [Test]
        public async Task GetProjectById_ExistingId_RetunrsCorrectProject()
        {
            var subject = CreateSubject();

            var project = new Project { Name = "Name", SearchTerm = "Search" };

            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            Assert.That(foundProject, Is.EqualTo(project));
        }

        [Test]
        public async Task GetProjectById_ProjectHasInvolvedTeams_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var project = new Project { Name = "Name", SearchTerm = "Search" };
            var team = new Team { Name = "Team1", ProjectName = "Project", WorkTrackingSystemOptions = new List<WorkTrackingSystemOption> { new WorkTrackingSystemOption { Key = "key", Value = "Value " } } };

            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            project.InvolvedTeams.Add(team);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            Assert.That(foundProject, Is.EqualTo(project));
            Assert.That(foundProject.InvolvedTeams, Has.Count.EqualTo(1));
            Assert.That(foundProject.InvolvedTeams.Single().WorkTrackingSystemOptions, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetProjectById_ProjectHasFeatures_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var project = new Project { Name = "Name", SearchTerm = "Search" };
            var feature = new Feature { Name = "MyFeature" };

            DatabaseContext.Features.Add(feature);
            await DatabaseContext.SaveChangesAsync();

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            Assert.That(foundProject, Is.EqualTo(project));
            Assert.That(foundProject.Features, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GivenExistingProject_RemoveProject_RemovesFromList()
        {
            var subject = CreateSubject();
            var project = new Project { Name = "Name", SearchTerm = "Search" };

            subject.Add(project);
            await subject.Save();

            // Act
            subject.Remove(project.Id);
            await subject.Save();

            var projects = subject.GetAll();
            CollectionAssert.DoesNotContain(projects, project);
        }

        [Test]
        public async Task UpdateProject_GivenExistingProject_PersistsChange()
        {
            var subject = CreateSubject();
            var project = new Project { Name = "Name", SearchTerm = "Search" };

            subject.Add(project);
            await subject.Save();

            // Act
            project.SearchTerm = "Yellooo?";
            subject.Update(project);
            await subject.Save();

            // Assert
            var projects = subject.GetAll();
            Assert.That(projects.Single().SearchTerm, Is.EqualTo(project.SearchTerm));
        }

        private ProjectRepository CreateSubject()
        {
            return new ProjectRepository(DatabaseContext);
        }
    }
}
