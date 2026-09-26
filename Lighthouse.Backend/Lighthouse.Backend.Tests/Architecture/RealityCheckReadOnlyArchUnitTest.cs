using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Forecast Reality Check reads and never writes, and that is held here as a property of the
    /// code rather than as a promise in review. The check once had a button that wrote one sampling
    /// window back to the Team, and it was removed because a button has to name one number inside a
    /// range the check had just said was undifferentiated. A sweep that could reach anything with a
    /// Save on it is one small step from bringing that button back.
    ///
    /// The rules find the feature's types by name. ArchUnitNET refuses a rule that finds nothing, so
    /// until the types exist every rule here fails for that reason alone, and all of them stay pending
    /// until the sweep and its verdict policy are written. The first test also pins that the policy is
    /// a static class, which no dependency rule can express.
    /// </summary>
    [TestFixture]
    [Category("epic-4172-forecast-backtest-sweep")]
    public class RealityCheckReadOnlyArchUnitTest
    {
        private const string Pending = "Pending: the Forecast Reality Check is not built yet (epic 4172, slice 01, story 6072).";

        private const string FeatureWord = "RealityCheck";

        private const string ForecastNamespace = "Lighthouse.Backend.Services.Implementation.Forecast";

        private const string VerdictPolicy = ForecastNamespace + ".RealityCheckVerdictPolicy";

        private const string RepositoriesPattern = @"^Lighthouse\.Backend\.Services\.Interfaces\.Repositories($|\..*)";

        private const string PersistencePattern = @"^Lighthouse\.Backend\.Data($|\..*)";

        private const string ImplementationPattern = @"^Lighthouse\.Backend\.Services\.Implementation($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        // @us-01 @kpi-OUT-4172-read-only @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void The_sweep_and_its_verdict_rules_exist_where_these_rules_look_for_them()
        {
            var production = typeof(Lighthouse.Backend.Services.Implementation.Forecast.ForecastDataSufficiencyPolicy).Assembly;
            var policy = production.GetType(VerdictPolicy);
            var sweep = production.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.Name == "ForecastRealityCheckService")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(policy, Is.Not.Null,
                    "the verdict rules live in one pure policy beside the shipped sufficiency policy, so every rule is a value in and a value out");
                Assert.That(policy, Has.Property(nameof(Type.IsAbstract)).True.And.Property(nameof(Type.IsSealed)).True,
                    "the verdict policy is a static class: no state, no dependencies, today handed in as an argument");
                Assert.That(sweep, Has.Count.EqualTo(1),
                    "the sweep is one service these rules can find by name; without it the rules below guard nothing");
            }
        }

        // @us-01 @kpi-OUT-4172-read-only @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void Nothing_in_the_reality_check_can_reach_a_repository()
        {
            Types().That().HaveNameContaining(FeatureWord)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(RepositoriesPattern))
                .Because(
                    "the sweep is handed the Team it checks and reads everything else through the metrics service. " +
                    "A repository is a Save away from writing a Team setting, which the check must never do.")
                .Check(Architecture);
        }

        // @us-01 @kpi-OUT-4172-read-only @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void Nothing_in_the_reality_check_can_reach_the_database_directly()
        {
            Types().That().HaveNameContaining(FeatureWord)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistencePattern))
                .Because("the check writes nothing, and reading around the metrics service would also skip its cache")
                .Check(Architecture);
        }

        // @us-01 @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public void The_verdict_rules_depend_on_no_service()
        {
            Types().That().HaveFullName(VerdictPolicy)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ImplementationPattern)
                    .And().DoNotHaveFullName("Lighthouse.Backend.Services.Implementation.Forecast.ForecastDataSufficiencyPolicy"))
                .Because("the verdict is decided from values alone, so every rule can be checked without a database or a simulation")
                .Check(Architecture);
        }
    }
}
