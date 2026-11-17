using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.PromisesFixtures.DomainLayout.Clean;
using Lighthouse.Backend.Modules.PromisesFixtures.DomainLayout.ReachingRepositories;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Delivery forecast block records live in the Promises module's Domain, which reaches only the shared kernel and the base
    /// library. The rule service, the source resolver, the source sync, the forecast publishing and the forecast block renderer,
    /// their interfaces and the metric values projector live in the module's Application, and none is left under Services or
    /// under the Models folder the records came from. The seven unit-test classes of the moved services sit in the matching test
    /// folder. Every type is named as a string and read from the compiled assembly, so a type that is missing, renamed or left
    /// behind is a failure that names it. The Domain rule and the left-behind rule also run on made-up types: an offender must be
    /// reported and its compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class PromisesLayoutArchUnitTest
    {
        private const string DomainNamespace = "Lighthouse.Backend.Modules.Promises.Domain";

        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.Promises.Application";

        private const string TestApplicationNamespace = "Lighthouse.Backend.Tests.Modules.Promises.Application";

        private const string PromisesDomain = @"^Lighthouse\.Backend\.Modules\.Promises\.Domain(\.|$)";

        private const string WhereTheRecordsLivedBefore = @"^Lighthouse\.Backend\.Models\.DeliverySources(\.|$)";

        private const string WhereTheServicesLivedBefore = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)(\.|$)";

        private const string WhereTheTestsLivedBefore = @"^Lighthouse\.Backend\.Tests\.Services(\.|$)";

        private const string TheWholeAssembly = @"^Lighthouse\.Backend\.";

        private const string FixtureDomains = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.DomainLayout\.";

        private const string ADomainThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.DomainLayout\.NothingLivesHere(\.|$)";

        private const string FixtureApplicationNamespace = "Lighthouse.Backend.Modules.PromisesFixtures.ApplicationLayout.Application";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.ApplicationLayout\.Services(\.|$)";

        private static readonly string[] ForecastBlockRecords =
        [
            "DeliveryForecastBlock",
            "DeliveryForecastBlockPercentile",
        ];

        private static readonly string[] RuleServiceTypes =
        [
            "DeliveryRuleService",
            "IDeliveryRuleService",
        ];

        private static readonly string[] MetricValuesTypes =
        [
            "DeliveryMetricValuesProjector",
            "DeliveryMetricValues",
        ];

        private static readonly string[] ForecastBlockRendererTypes =
        [
            "DeliveryForecastBlockRenderer",
            "IDeliveryForecastBlockRenderer",
        ];

        private static readonly string[] ForecastPublishingTypes =
        [
            "DeliveryForecastPublishingService",
            "IDeliveryForecastPublishingService",
        ];

        private static readonly string[] SourceTypes =
        [
            "DeliverySourceResolver",
            "IDeliverySourceResolver",
            "PortfolioSourcePreview",
            "DeliverySourceSyncService",
            "IDeliverySourceSyncService",
        ];

        private static readonly string[] MovedUnitTests =
        [
            "DeliveryRuleServiceTest",
            "DeliveryMetricValuesProjectorTest",
            "DeliveryForecastBlockRendererTest",
            "DeliveryForecastBlockRendererBeyondTheHorizonTest",
            "DeliveryForecastPublishingServiceTest",
            "DeliverySourceResolverTest",
            "DeliverySourceSyncServiceTest",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PromisesLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheForecastBlockRecords_LiveInThePromisesDomain_AndNoneIsLeftUnderModelsDeliverySources()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, DomainNamespace, ForecastBlockRecords, WhereTheRecordsLivedBefore).Check();
        }

        [Test]
        public void ThePromisesDomain_HoldsExactlyTheForecastBlockRecords()
        {
            // A nested type belongs to the type that declares it, so only the outermost types count.
            var held = IlDependencies.TypesIn(ProductionModule.Value, PromisesDomain)
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var expected = ForecastBlockRecords.Select(name => $"{DomainNamespace}.{name}").ToList();

            Assert.That(
                held,
                Is.EquivalentTo(expected),
                $"The Promises Domain holds [{string.Join(", ", held)}] but should hold exactly [{string.Join(", ", expected)}]. " +
                "Why it matters: a record that is missing has not moved out of the Models, and a type that does not belong keeps the Domain from being " +
                "the short list of pure Delivery records. How to fix: move the forecast block file into the module's Domain folder, and put a type " +
                "that needs services or repositories in the module's Application instead.");
        }

        [Test]
        public void ThePromisesDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, PromisesDomain).Check();
        }

        [Test]
        public void TheDeliveryRuleServiceAndItsInterface_LiveInThePromisesApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, RuleServiceTypes, WhereTheServicesLivedBefore).Check();
        }

        [Test]
        public void TheMetricValuesProjectorAndItsValues_LiveInThePromisesApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, MetricValuesTypes, WhereTheServicesLivedBefore).Check();
        }

        [Test]
        public void TheForecastBlockRendererAndItsInterface_LiveInThePromisesApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, ForecastBlockRendererTypes, WhereTheServicesLivedBefore).Check();
        }

        [Test]
        public void TheForecastPublishingServiceAndItsInterface_LiveInThePromisesApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, ForecastPublishingTypes, WhereTheServicesLivedBefore).Check();
        }

        [Test]
        public void TheSourceResolverTheSourcePreviewAndTheSourceSync_LiveInThePromisesApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, SourceTypes, WhereTheServicesLivedBefore).Check();
        }

        [Test]
        public void TheUnitTestsOfTheMovedServices_LiveInThePromisesApplicationTestFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(TestModule.Value, TestApplicationNamespace, MovedUnitTests, WhereTheTestsLivedBefore).Check();
        }

        [Test]
        public void ANameThatNoTypeMatches_FailsTheLayoutRuleAsMissing_InsteadOfPassingUnseen()
        {
            const string anInventedName = "DeliveryRuleServiceThatDoesNotExist";

            var verdict = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, [anInventedName], WhereTheServicesLivedBefore);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A list name that no type in the assembly matches passed the layout rule. Why it matters: a list that has drifted from the code " +
                "stays green forever and guards nothing. How to fix: the rule must fail for a name whose type is not in the layer.");

            Assert.That(failure!.Message, Does.Contain($"{ApplicationNamespace}.{anInventedName}"), "The failure must name the missing type, so the reader knows which list entry to correct.");
        }

        [Test]
        public void ATypeLeftUnderItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, FixtureApplicationNamespace, ["AWellPlacedService"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, FixtureApplicationNamespace, ["ALeftBehindService"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in the Application and has no copy in its former home was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left in its former home passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindService"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void APromisesDomainTypeThatReachesARepository_IsReported_AndItsCompliantTwinIsNot()
        {
            var offender = typeof(ADomainTypeReachingARepository).FullName!;
            var forbiddenTarget = typeof(global::Lighthouse.Backend.Services.PromisesFixtures.ARepositoryType).FullName!;
            var compliantTwin = typeof(ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary).FullName!;

            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, $"{FixtureDomains}(ReachingRepositories|Clean)$");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(compliantTwin).And.Contain(offender),
                    "The rule did not look at both made-up Domain types. Why it matters: a twin the rule never looked at proves nothing by passing. " +
                    "How to fix: the rule must examine every type in the Domain namespace it is given.");

                Assert.That(
                    verdict.Offenders.Select(dependent => dependent.TypeName),
                    Is.EqualTo(new[] { offender }),
                    $"Expected exactly {offender} to be reported. Why it matters: a Domain type that reaches a repository must be caught, and a twin that " +
                    "reaches only its own Domain, the Models and the base library must not be, or the rule flags everything. How to fix: the rule allows " +
                    "exactly the Domain itself, the shared kernel, System.* and compiler-made types.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, $"The Domain rule passed with {offender} in it.");

            Assert.That(
                failure!.Message,
                Does.Contain(offender).And.Contain(forbiddenTarget),
                "The failure must name both the offending Domain type and the repository it reached, so the reader knows what to undo.");
        }

        [Test]
        public void APromisesDomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, ADomainThatHoldsNothing);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A Domain rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing " +
                "stays green forever and guards nothing. How to fix: the rule must fail when the Domain it was given holds no type.");

            Assert.That(failure!.Message, Does.Contain(ADomainThatHoldsNothing), "The failure must name the Domain that held nothing, so the reader knows which pattern to correct.");
        }

        [Test]
        public void EveryNameInTheLists_ResolvesToATypeInTheAssembly()
        {
            var inTheAssembly = IlDependencies.TypesIn(ProductionModule.Value, TheWholeAssembly)
                .Select(fullName => fullName.Split('+')[0][(fullName.Split('+')[0].LastIndexOf('.') + 1)..])
                .ToHashSet(StringComparer.Ordinal);
            var listed = ForecastBlockRecords.Concat(RuleServiceTypes).Concat(MetricValuesTypes).Concat(ForecastBlockRendererTypes)
                .Concat(ForecastPublishingTypes).Concat(SourceTypes).ToList();

            Assert.That(
                listed.Where(name => !inTheAssembly.Contains(name)),
                Is.Empty,
                "These listed names match no type in the compiled assembly. Why it matters: a name that matches nothing makes its layout check look at nothing. " +
                "How to fix: correct the name in the list, or restore the type it names.");
        }
    }
}

namespace Lighthouse.Backend.Services.PromisesFixtures
{
    internal sealed class ARepositoryType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Models.PromisesFixtures
{
    internal sealed record AKernelValue(int Days);
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.ApplicationLayout.Application
{
    internal sealed class AWellPlacedService
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.ApplicationLayout.Services
{
    internal sealed class ALeftBehindService
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.DomainLayout.ReachingRepositories
{
    internal static class ADomainTypeReachingARepository
    {
        public static int Answer(global::Lighthouse.Backend.Services.PromisesFixtures.ARepositoryType repository) => repository.Answer;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.DomainLayout.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. It also makes
    /// the compiler write its own helper types, so a rule that mistook those for forbidden types would report it.
    /// </summary>
    internal static class ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary
    {
        private static readonly int[] DaysShown = [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597];

        public static int LongestAbove(IEnumerable<global::Lighthouse.Backend.Models.PromisesFixtures.AKernelValue> values, int threshold)
            => values.Where(value => value.Days > threshold).Select(value => value.Days).DefaultIfEmpty(ASiblingInTheSameDomain.Nothing).Max();

        public static IEnumerable<int> Shown()
        {
            foreach (var day in DaysShown)
            {
                yield return Math.Min(day, 1000);
            }
        }
    }

    internal static class ASiblingInTheSameDomain
    {
        public const int Nothing = 0;
    }
}
