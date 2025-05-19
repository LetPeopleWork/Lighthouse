using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class ProjectTests
    {
        [Test]
        public void UpdateFeatures_RefreshesLastUpdateTime()
        {
            var subject = CreateSubject();
            
            var initialUpdateTime = subject.UpdateTime;

            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Feature 1" },
                new Feature { Id = 2, Name = "Feature 2" }
            };

            subject.UpdateFeatures(features);

            Assert.That(subject.UpdateTime, Is.Not.EqualTo(initialUpdateTime));
        }

        [Test]
        public void UpdateTeams_RefreshesLastUpdateTime()
        {
            var subject = CreateSubject();
            
            var initialUpdateTime = subject.UpdateTime;
            var teams = new List<Team>
            {
                new Team { Id = 1, Name = "Team 1" },
                new Team { Id = 2, Name = "Team 2" }
            };
            
            subject.UpdateTeams(teams);

            Assert.That(subject.UpdateTime, Is.Not.EqualTo(initialUpdateTime));
        }

        private Project CreateSubject()
        {
            return new Project();
        }
    }
}
