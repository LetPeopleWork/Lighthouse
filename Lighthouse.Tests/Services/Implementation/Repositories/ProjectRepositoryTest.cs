using Lighthouse.Models;
using Lighthouse.Services.Implementation.Repositories;
using Lighthouse.Tests.TestHelpers;
using Lighthouse.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Tests.Services.Implementation.Repositories
{
    public class ProjectRepositoryTest : IntegrationTestBase
    {
        public ProjectRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task GetProjectById_ProjectHasInvolvedTeams_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var project = new Project { Name = "Name" };
            var team = new Team { Name = "Team1", WorkTrackingSystemOptions = new List<WorkTrackingSystemOption<Team>> { new WorkTrackingSystemOption<Team> { Key = "key", Value = "Value " } } };

            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            var feature = new Feature { Name = "Feature", Order = "12" };
            feature.RemainingWork.Add(new RemainingWork(team, 12, feature));

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);
            
            Assert.Multiple(() =>
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.InvolvedTeams.ToList(), Has.Count.EqualTo(1));
                Assert.That(foundProject.InvolvedTeams.Single().WorkTrackingSystemOptions, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task GetProjectById_ProjectHasFeatures_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var project = new Project { Name = "Name",  };
            var feature = new Feature { Name = "MyFeature", Order = "12" };

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.Features, Has.Count.EqualTo(1));
            });
        }

        private ProjectRepository CreateSubject()
        {
            return new ProjectRepository(DatabaseContext, Mock.Of<ILogger<ProjectRepository>>());
        }
    }
}
