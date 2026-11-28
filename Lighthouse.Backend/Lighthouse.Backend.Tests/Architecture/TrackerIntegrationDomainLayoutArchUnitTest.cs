using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.Clean;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingAdapters;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingExtensions;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingServices;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The translation rules that apply a connection's mapping, one answer for every tracker, live in the Tracker integration
    /// module: the state-transition mapper, the parent source selector and the dependency field references in its Domain, which
    /// reaches only the shared kernel and the base library, and the category crossing and the dependency source selector in its
    /// Application, because they use the shared extension methods a Domain may not reach. The types are named as strings and read
    /// from the compiled production assembly, so a type that is missing, renamed or left under Services is a failure that names it.
    /// The Domain rule also runs on made-up Domain types: each one that reaches a forbidden layer must be reported, and a compliant
    /// twin next to it must not be.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationDomainLayoutArchUnitTest
    {
        private const string DomainNamespace = "Lighthouse.Backend.Modules.TrackerIntegration.Domain";

        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.TrackerIntegration.Application";

        private const string TrackerIntegrationDomain = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration\.Domain(\.|$)";

        private const string FixtureDomains = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.DomainLayout\.";

        private const string ADomainThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.DomainLayout\.NothingLivesHere(\.|$)";

        private static readonly string[] DomainTypes =
        [
            "WorkItemStateTransitionMapper",
            "ParentSourceSelector",
            "DependencyFieldReferences",
            "DeliverySourceBindability",
        ];

        private static readonly string[] ApplicationTypes =
        [
            "WorkItemCategoryCrossing",
            "DependencySourceSelector",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationDomainLayoutArchUnitTest).Assembly));

        public enum ForbiddenReach
        {
            Services,
            ModuleAdapters,
            Extensions,
        }

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheStateTransitionMapperTheParentSourceSelectorAndTheDependencyFieldReferences_LiveInTheTrackerIntegrationDomain_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, DomainNamespace, DomainTypes).Check();
        }

        [Test]
        public void TheCategoryCrossingAndTheDependencySourceSelector_LiveInTheTrackerIntegrationApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, ApplicationTypes).Check();
        }

        [Test]
        public void TheTrackerIntegrationDomain_HoldsExactlyTheFourTranslationRulesThatReachOnlyTheKernel()
        {
            // A nested type belongs to the type that declares it, so only the outermost types count.
            var held = IlDependencies.TypesIn(ProductionModule.Value, TrackerIntegrationDomain)
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var expected = DomainTypes.Select(name => $"{DomainNamespace}.{name}").ToList();

            Assert.That(
                held,
                Is.EquivalentTo(expected),
                $"The Tracker integration Domain holds [{string.Join(", ", held)}] but should hold exactly [{string.Join(", ", expected)}]. " +
                "Why it matters: a type that is missing has not moved out of Services, and a type that does not belong keeps the Domain from being " +
                "the short list of pure translation rules. How to fix: move each missing file into the module's Domain folder, and put a type that " +
                "reaches the shared extensions in the module's Application instead.");
        }

        [Test]
        public void TheTrackerIntegrationDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, TrackerIntegrationDomain).Check();
        }

        [TestCase(ForbiddenReach.Services)]
        [TestCase(ForbiddenReach.ModuleAdapters)]
        [TestCase(ForbiddenReach.Extensions)]
        public void ATrackerIntegrationDomainTypeThatReachesAForbiddenLayer_IsReportedAndItsCompliantTwinIsNot(ForbiddenReach reach)
        {
            var (domain, offender, forbiddenTarget) = MadeUpDomainReaching(reach);

            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, domain);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, forbiddenTarget, reach);
        }

        [Test]
        public void ATrackerIntegrationDomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, ADomainThatHoldsNothing);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A Domain rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing " +
                "stays green forever and guards nothing. How to fix: the rule must fail when the Domain it was given holds no type.");

            Assert.That(failure!.Message, Does.Contain(ADomainThatHoldsNothing), "The failure must name the Domain that held nothing, so the reader knows which pattern to correct.");
        }

        private static (string Domain, string Offender, string ForbiddenTarget) MadeUpDomainReaching(ForbiddenReach reach) => reach switch
        {
            ForbiddenReach.Services => (FixtureDomain("ReachingServices"), typeof(ADomainTypeReachingServices).FullName!, typeof(global::Lighthouse.Backend.Services.TrackerIntegrationFixtures.AServiceType).FullName!),
            ForbiddenReach.ModuleAdapters => (FixtureDomain("ReachingAdapters"), typeof(ADomainTypeReachingTheModuleAdapters).FullName!, typeof(global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType).FullName!),
            ForbiddenReach.Extensions => (FixtureDomain("ReachingExtensions"), typeof(ADomainTypeReachingTheExtensions).FullName!, typeof(global::Lighthouse.Backend.Extensions.TrackerIntegrationFixtures.AnExtensionType).FullName!),
            _ => throw new ArgumentOutOfRangeException(nameof(reach)),
        };

        // Each made-up Domain is the offender's namespace together with the compliant twin's.
        private static string FixtureDomain(string offenderNamespace) => $"{FixtureDomains}({offenderNamespace}|Clean)$";

        private static void ThenTheRuleReportsOnlyTheOffender(KernelRuleVerdict verdict, string offender, string forbiddenTarget, ForbiddenReach reach)
        {
            var compliantTwin = typeof(ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary).FullName!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(compliantTwin).And.Contain(offender),
                    $"The rule did not look at both made-up Domain types ({reach}). Why it matters: a twin the rule never looked at " +
                    "proves nothing by passing. How to fix: the rule must examine every type in the Domain namespace it is given.");

                Assert.That(
                    verdict.Offenders.Select(dependent => dependent.TypeName),
                    Is.EqualTo(new[] { offender }),
                    $"Expected exactly {offender} to be reported ({reach}). Why it matters: a Domain type that reaches a forbidden " +
                    "layer must be caught, and a twin that reaches only its own Domain, the Models and the base library must not be, " +
                    "or the rule flags everything. How to fix: the rule allows exactly the Domain itself, the shared kernel, System.* and compiler-made types.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, $"The Domain rule passed with {offender} in it ({reach}).");

            Assert.That(
                failure!.Message,
                Does.Contain(offender).And.Contain(forbiddenTarget),
                "The failure must name both the offending Domain type and the forbidden type it reached, so the reader knows what to undo.");
        }
    }
}

namespace Lighthouse.Backend.Services.TrackerIntegrationFixtures
{
    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Extensions.TrackerIntegrationFixtures
{
    internal static class AnExtensionType
    {
        public static int Doubled(this int value) => value * 2;
    }
}

namespace Lighthouse.Backend.Models.TrackerIntegrationFixtures
{
    internal sealed record AKernelValue(int Days);
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.Neighbours.Adapters
{
    internal sealed class AnAdapterType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingServices
{
    internal static class ADomainTypeReachingServices
    {
        public static int Answer(global::Lighthouse.Backend.Services.TrackerIntegrationFixtures.AServiceType service) => service.Answer;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingAdapters
{
    internal static class ADomainTypeReachingTheModuleAdapters
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.ReachingExtensions
{
    internal static class ADomainTypeReachingTheExtensions
    {
        public static int Answer() => global::Lighthouse.Backend.Extensions.TrackerIntegrationFixtures.AnExtensionType.Doubled(21);
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.DomainLayout.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. It also makes
    /// the compiler write its own helper types, so a rule that mistook those for forbidden types would report it.
    /// </summary>
    internal static class ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary
    {
        private static readonly int[] DaysShown = [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597];

        public static int LongestAbove(IEnumerable<global::Lighthouse.Backend.Models.TrackerIntegrationFixtures.AKernelValue> values, int threshold)
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
