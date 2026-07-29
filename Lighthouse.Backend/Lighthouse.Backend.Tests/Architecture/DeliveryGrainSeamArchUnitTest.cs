using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // Story #5587 / ADR-113. The grain rule - "min only WITHIN a team's bucket, product only ACROSS
    // buckets" - is a property of the CALL SITE, not of the combinators. The weaker "neither combinator
    // depends on the other" would forbid only what nobody would write; this rule forbids exactly the
    // mistake, a Delivery reaching for Min over cross-team rows.
    [TestFixture]
    public class DeliveryGrainSeamArchUnitTest
    {
        private const string DeliveryEntity = "Lighthouse.Backend.Models.Delivery";
        private const string ComonotonicCombinator = "Lighthouse.Backend.Models.Forecast.ComonotonicCompletionDistribution";
        private const string JointCombinator = "Lighthouse.Backend.Models.Forecast.JointCompletionDistribution";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void Delivery_DoesNotReachForACompletionCombinatorDirectly()
        {
            Classes().That().HaveFullName(DeliveryEntity)
                .Should().NotDependOnAny(Types().That().HaveFullName(ComonotonicCombinator).Or().HaveFullName(JointCombinator))
                .Because(
                    "ADR-113: only DeliveryCompletionForecast may compose the two combinators. Delivery keeps the guards - " +
                    "empty / cannot forecast / all done are delivery policy - and delegates the combination, so the entity " +
                    "cannot apply the elementwise minimum at the wrong grain (across teams instead of within a team).")
                .Check(Architecture);
        }
    }
}
