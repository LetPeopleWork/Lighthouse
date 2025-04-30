using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class FeatureTest
    {
        [Test]
        public void GetLikelihoodForFeature_FeatureHasNoRemainingWork_Returns100()
        {
            var subject = new Feature();

            var likelihood = subject.GetLikelhoodForDate(DateTime.Today.AddDays(17));

            Assert.That(likelihood, Is.EqualTo(100));
        }

        [Test]
        public void Update_SetsEstimatedSize()
        {
            var otherItem = new Feature
            {
                EstimatedSize = 42
            };

            var subject = new Feature();

            subject.Update(otherItem);
            
            Assert.That(subject.EstimatedSize, Is.EqualTo(otherItem.EstimatedSize));
        }

        [Test]
        public void Update_SetsOwningTeam()
        {
            var otherItem = new Feature
            {
                OwningTeam = "Team B"
            };
            
            var subject = new Feature();

            subject.Update(otherItem);

            Assert.That(subject.OwningTeam, Is.EqualTo(otherItem.OwningTeam));
        }
    }
}
