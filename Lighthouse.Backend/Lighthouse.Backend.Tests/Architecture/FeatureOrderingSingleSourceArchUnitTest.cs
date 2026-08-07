using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class FeatureOrderingSingleSourceArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string Because =
            "ADR-134 SA-2: FeatureOrdering is the single selection point, and the only production type " +
            "allowed to know an ordering comparer exists. Four read paths draw from it - the Features " +
            "view, the Portfolio detail, the forecast queue and the position map - and the premise check " +
            "run on 2026-08-07 showed what a second sort site costs: two of them sorted a subset with no " +
            "Id tie-break, so Features the tracker had ranked alike came back in whatever sequence the " +
            "store happened to hand over. Reaching for a comparer directly reopens that. If a new path " +
            "genuinely needs one, take IFeatureOrdering instead; if it truly cannot, amend ADR-134 first " +
            "and then this test.";

        [Test]
        public void NoProductionTypeButTheOrderingSeamDependsOnTheSourceOrderComparer()
        {
            Classes().That().AreNot(typeof(FeatureOrdering)).And().AreNot(typeof(FeatureComparer))
                .Should().NotDependOnAny(typeof(FeatureComparer))
                .Because(Because)
                .Check(Architecture);
        }

        [Test]
        public void NoProductionTypeButTheOrderingSeamDependsOnTheManualRankComparer()
        {
            Classes().That().AreNot(typeof(FeatureOrdering)).And().AreNot(typeof(ManualRankComparer))
                .Should().NotDependOnAny(typeof(ManualRankComparer))
                .Because(Because)
                .Check(Architecture);
        }
    }
}
