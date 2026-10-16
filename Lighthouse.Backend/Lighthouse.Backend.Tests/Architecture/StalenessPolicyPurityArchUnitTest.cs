using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // The staleness rule is pure arithmetic over values the reads already hold. Kept free of services,
    // controllers and storage, every boundary of it stays reachable from a plain unit test, and nothing
    // can grow a second, slightly different rule by pulling it through a service.
    [TestFixture]
    [Category("story-6247")]
    public class StalenessPolicyPurityArchUnitTest
    {
        private const string StalenessNamespace = "Lighthouse.Backend.Models.Staleness";

        private const string ServicesPattern = @"^Lighthouse\.Backend\.Services($|\..*)";
        private const string ApiPattern = @"^Lighthouse\.Backend\.API($|\..*)";
        private const string DataPattern = @"^Lighthouse\.Backend\.Data($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void The_staleness_rule_exists_as_its_own_policy()
        {
            var production = typeof(Lighthouse.Backend.Models.Staleness.StalenessPolicy).Assembly;

            Assert.That(production.GetType(StalenessNamespace + ".StalenessPolicy"), Is.Not.Null,
                "Every read that judges staleness asks this one policy, so it has to exist under its own name.");
        }

        [Test]
        public void The_staleness_rule_depends_on_nothing_in_services_the_API_or_storage()
        {
            Types().That().ResideInNamespace(StalenessNamespace)
                .Should().NotDependOnAny(Types().That()
                    .ResideInNamespaceMatching(ServicesPattern)
                    .Or().ResideInNamespaceMatching(ApiPattern)
                    .Or().ResideInNamespaceMatching(DataPattern))
                .Because("the rule takes the day and the time zone as values, so it never needs a clock, a repository or a controller")
                .Check(Architecture);
        }
    }
}
