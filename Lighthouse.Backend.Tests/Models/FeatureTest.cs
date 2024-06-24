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
    }
}
