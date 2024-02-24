using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;
using CMFTAspNet.Tests.TestHelpers;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
{
    public class TeamRepositoryTest : IntegrationTestBase
    {
        public TeamRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public void GetTeams_NoTeams_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var teams = subject.GetAll();

            Assert.That(teams, Is.Empty);
        }

        [Test]
        public async Task AddTeam_StoresTeamAsync()
        {
            var subject = CreateSubject();
            var team = new Team {  Name = "Name", ProjectName = "Project" };

            subject.Add(team);
            await subject.Save();

            var teams = subject.GetAll();

            Assert.That(teams, Contains.Item(team));
        }

        [Test]
        public async Task GetTeamById_ExistingId_RetunrsCorrectTeam()
        {
            var subject = CreateSubject();
            var workTrackingOptions = new List<WorkTrackingSystemOption> { new WorkTrackingSystemOption("key", "value") };
            var team = new Team {  Name = "Name", ProjectName = "Project", WorkTrackingSystemOptions = workTrackingOptions };

            subject.Add(team);
            await subject.Save();

            var foundTeam = subject.GetById(team.Id);

            Assert.That(foundTeam, Is.EqualTo(team));
        }

        [Test]
        public async Task GivenExistingTeam_RemoveTeam_RemovesFromList()
        {
            var subject = CreateSubject();
            var team = new Team { Name = "Name", ProjectName = "Project" };

            subject.Add(team);
            await subject.Save();

            // Act
            subject.Remove(team.Id);
            await subject.Save();

            var teams = subject.GetAll();
            CollectionAssert.DoesNotContain(teams, team);
        }

        [Test]
        public async Task UpdateTeam_GivenExistingTeam_PersistsChange()
        {
            var subject = CreateSubject();
            var team = new Team { Name = "Name", ProjectName = "Project" };

            subject.Add(team);
            await subject.Save();

            // Act
            team.FeatureWIP = 2;
            subject.Update(team);
            await subject.Save();

            // Assert
            var teams = subject.GetAll();
            Assert.That(teams.Single().FeatureWIP, Is.EqualTo(2));
        }

        private TeamRepository CreateSubject()
        {
            return new TeamRepository(DatabaseContext);
        }
    }
}
