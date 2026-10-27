using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class PortfolioSizeTest
    {
        [Test]
        [TestCase(0, false)]
        [TestCase(100, false)]
        [TestCase(101, true)]
        public void IsOverThreshold_IsTrueOnlyAboveOneHundredFeatures(int featureCount, bool expected)
        {
            Assert.That(new PortfolioSize(featureCount).IsOverThreshold, Is.EqualTo(expected));
        }
    }
}
