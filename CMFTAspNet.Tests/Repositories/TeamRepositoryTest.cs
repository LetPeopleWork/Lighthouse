using CMFTAspNet.Models.Teams;
using CMFTAspNet.Repositories;

namespace CMFTAspNet.Tests.Repositories
{
    public class TeamRepositoryTest
    {
        [Test]
        public void GetTeams_NoTeams_ReturnsEmptyList()
        {
            var subject = new TeamRepository();

            var teams = subject.GetTeams();

            Assert.That(teams, Is.Empty);
        }

        [Test]
        public void AddTeam_StoresTeam()
        {
            var subject = new TeamRepository();
            var team = new Team("MyTeam");

            subject.AddTeam(team);

            var teams = subject.GetTeams();

            Assert.That(teams, Contains.Item(team));
        }

        [Test]
        public void GivenExistingTeam_RemoveTeam_RemovesFromList()
        {
            var subject = new TeamRepository();
            var team = new Team("MyTeam");

            subject.AddTeam(team);

            // Act
            subject.RemoveTeam(team);

            var teams = subject.GetTeams();
            CollectionAssert.DoesNotContain(teams, team);
        }

        [Test]
        public void UpdateTeam_GivenExistingTeam_PersistsChange()
        {
            var subject = new TeamRepository();
            var team = new Team("MyTeam");

            subject.AddTeam(team);

            // Act
            team.FeatureWIP = 2;
            subject.UpdateTeam(team);

            // Assert
            var teams = subject.GetTeams();
            Assert.That(teams.Single().FeatureWIP, Is.EqualTo(2));
        }
    }
}
