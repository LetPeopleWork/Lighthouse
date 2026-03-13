using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class PortfolioTest
    {
        [Test]
        public void RefreshUpdateTime_RefreshesLastUpdateTime()
        {
            var subject = CreateSubject();

            var initialUpdateTime = subject.UpdateTime;

            subject.RefreshUpdateTime();

            Assert.That(subject.UpdateTime, Is.Not.EqualTo(initialUpdateTime));
        }

        [Test]
        public void Teams_NoFeatures_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            Assert.That(subject.Teams, Is.Empty);
        }

        [Test]
        public void Teams_FeaturesWithNoFeatureWork_ReturnsEmptyList()
        {
            var subject = CreateSubject();
            subject.UpdateFeatures([new Feature(), new Feature()]);

            Assert.That(subject.Teams, Is.Empty);
        }

        [Test]
        public void Teams_SingleFeatureWithSingleTeam_ReturnsThatTeam()
        {
            var team = new Team { Id = 1 };
            var feature = new Feature(team, remainingItems: 3);

            var subject = CreateSubject();
            subject.UpdateFeatures([feature]);

            Assert.That(subject.Teams, Is.EqualTo([team]));
        }

        [Test]
        public void Teams_MultipleFeaturesDifferentTeams_ReturnsAllDistinctTeams()
        {
            var teamA = new Team { Id = 1 };
            var teamB = new Team { Id = 2 };

            var subject = CreateSubject();
            subject.UpdateFeatures([
                new Feature(teamA, remainingItems: 1),
                new Feature(teamB, remainingItems: 1),
            ]);

            Assert.That(subject.Teams, Is.EquivalentTo(new[] { teamA, teamB }));
        }

        [Test]
        public void Teams_SameTeamAcrossMultipleFeatures_IsReturnedOnlyOnce()
        {
            var team = new Team { Id = 1 };

            var subject = CreateSubject();
            subject.UpdateFeatures([
                new Feature(team, remainingItems: 1),
                new Feature(team, remainingItems: 2),
            ]);

            Assert.That(subject.Teams.ToList(), Has.Count.EqualTo(1));
            Assert.That(subject.Teams, Contains.Item(team));
        }

        [Test]
        public void Teams_SameTeamIdOnDifferentInstances_IsDeduplicatedById()
        {
            var teamA1 = new Team { Id = 1 };
            var teamA2 = new Team { Id = 1 };

            var subject = CreateSubject();
            subject.UpdateFeatures([
                new Feature(teamA1, remainingItems: 1),
                new Feature(teamA2, remainingItems: 1),
            ]);

            Assert.That(subject.Teams.ToList(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Teams_SingleFeatureMultipleTeams_ReturnsAllTeams()
        {
            var teamA = new Team { Id = 1 };
            var teamB = new Team { Id = 2 };
            var feature = new Feature([(teamA, 1, 5), (teamB, 2, 5)]);

            var subject = CreateSubject();
            subject.UpdateFeatures([feature]);

            Assert.That(subject.Teams, Is.EquivalentTo([teamA, teamB]));
        }

        private static Portfolio CreateSubject()
        {
            return new Portfolio();
        }
    }
}