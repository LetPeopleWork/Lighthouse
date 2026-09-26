using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Tests.Models.Metrics
{
    [TestFixture]
    public class ThroughputFilterOverrideTest
    {
        [TestCase(true, ThroughputFilterMode.ApplyFilter)]
        [TestCase(false, ThroughputFilterMode.SkipFilter)]
        [TestCase(null, ThroughputFilterMode.RespectTeamSetting)]
        public void ToFilterMode_YesAppliesTheFilterNoSkipsItAndLeavingItOutKeepsTheTeamsSetting(bool? applyFilterOverride, ThroughputFilterMode expected)
        {
            Assert.That(ThroughputFilterOverride.ToFilterMode(applyFilterOverride), Is.EqualTo(expected));
        }
    }
}
