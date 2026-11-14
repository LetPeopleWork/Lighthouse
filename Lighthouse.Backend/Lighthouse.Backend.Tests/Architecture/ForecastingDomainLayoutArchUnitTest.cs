using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Clean;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingAdapters;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingApplication;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingMicrosoftExtensions;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingServices;
using Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingServicesOnlyInsideAnAsyncMethod;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The pure part of Forecasting, the simulation, its draw streams, its run plan and the two forecast policies, lives
    /// in the module's Domain and reaches only the shared kernel and the base library. The types are named as strings and
    /// read from the compiled production assembly, so this class compiles before and after the move, and a type that has
    /// not moved yet is a failure that names it. The Domain rule also runs on made-up Domain types: each one that reaches a
    /// forbidden layer must be reported, and a compliant twin next to it must not be.
    /// </summary>
    [TestFixture]
    public class ForecastingDomainLayoutArchUnitTest
    {
        private const string ForecastingDomainNamespace = "Lighthouse.Backend.Modules.Forecasting.Domain";

        private const string ForecastingDomain = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Domain(\.|$)";

        private const string ForecastingDomainFolder = "Modules/Forecasting/Domain";

        private const string FixtureDomains = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.DomainLayout\.";

        private const string ADomainThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.DomainLayout\.NothingLivesHere(\.|$)";

        // Each file declares exactly one top-level type, named like the file: the simulation and what it draws from,
        // the plan and the record of a run, and the two policies that decide from handed values.
        private static readonly string[] DomainTypes =
        [
            "AddressableDrawStream",
            "DayCounts",
            "DrawStreamFactory",
            "ForecastDataSufficiencyPolicy",
            "ForecastRunPlan",
            "ForecastSimulation",
            "HowTheRunEnded",
            "OneWorkersShareOfTheRuns",
            "RealityCheckVerdictPolicy",
            "SimulatedRun",
            "TrialRecordings",
            "TrialState",
            "WhatTheRunsCouldNotFinish",
            "IDrawStream",
            "IDrawStreamFactory",
            "ForecastRunOutcome",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(ForecastingDomainLayoutArchUnitTest).Assembly));

        public enum ForbiddenReach
        {
            Services,
            MicrosoftExtensions,
            ModuleApplication,
            ModuleAdapters,
            ServicesOnlyInsideAnAsyncMethod,
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
        public void TheForecastSimulationAndItsPureParts_LiveInTheModuleDomain_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ForecastingDomainNamespace, DomainTypes).Check();
        }

        [Test]
        public void TheForecastSimulationAndItsPurePartsFiles_SitInTheModuleDomainFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ForecastingDomainFolder, [.. DomainTypes.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheForecastingDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, ForecastingDomain).Check();
        }

        [TestCase(ForbiddenReach.Services)]
        [TestCase(ForbiddenReach.MicrosoftExtensions)]
        [TestCase(ForbiddenReach.ModuleApplication)]
        [TestCase(ForbiddenReach.ModuleAdapters)]
        [TestCase(ForbiddenReach.ServicesOnlyInsideAnAsyncMethod)]
        public void AForecastingDomainTypeThatReachesAForbiddenLayer_IsReportedAndItsCompliantTwinIsNot(ForbiddenReach reach)
        {
            var (domain, offender, forbiddenTarget) = MadeUpDomainReaching(reach);

            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, domain);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, forbiddenTarget, reach);
        }

        [Test]
        public void AForecastingDomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
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
            ForbiddenReach.Services => (FixtureDomain("ReachingServices"), typeof(ADomainTypeReachingServices).FullName!, typeof(global::Lighthouse.Backend.Services.ForecastingFixtures.AServiceType).FullName!),
            ForbiddenReach.MicrosoftExtensions => (FixtureDomain("ReachingMicrosoftExtensions"), typeof(ADomainTypeReachingMicrosoftExtensions).FullName!, typeof(global::Microsoft.Extensions.Logging.ILogger).FullName!),
            ForbiddenReach.ModuleApplication => (FixtureDomain("ReachingApplication"), typeof(ADomainTypeReachingTheModuleApplication).FullName!, typeof(global::Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Application.AnApplicationType).FullName!),
            ForbiddenReach.ModuleAdapters => (FixtureDomain("ReachingAdapters"), typeof(ADomainTypeReachingTheModuleAdapters).FullName!, typeof(global::Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType).FullName!),
            ForbiddenReach.ServicesOnlyInsideAnAsyncMethod => (FixtureDomain("ReachingServicesOnlyInsideAnAsyncMethod"), typeof(ADomainTypeReachingServicesOnlyInsideAnAsyncMethod).FullName!, typeof(global::Lighthouse.Backend.Services.ForecastingFixtures.AServiceType).FullName!),
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
                    "layer must be caught, async bodies included, and a twin that reaches only its own Domain, the Models, the base " +
                    "library and the compiler's own helper types must not be, or the rule flags everything. How to fix: the rule " +
                    "allows exactly the Domain itself, the shared kernel, System.* and compiler-made types.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, $"The Domain rule passed with {offender} in it ({reach}).");

            Assert.That(
                failure!.Message,
                Does.Contain(offender).And.Contain(forbiddenTarget),
                "The failure must name both the offending Domain type and the forbidden type it reached, so the reader knows what to undo.");
        }

        private static void ThenEachFileSitsOnlyIn(string folder, IReadOnlyCollection<string> fileNames)
        {
            var paths = ProductionSource.Files().Select(file => file.RelativePath).ToList();

            var misplaced = fileNames
                .SelectMany(fileName => paths.Where(path => Path.GetFileName(path) == fileName && path != $"{folder}/{fileName}"))
                .ToList();
            var missing = fileNames.Where(fileName => !paths.Contains($"{folder}/{fileName}", StringComparer.Ordinal)).ToList();

            Assert.That(
                missing.Concat(misplaced),
                Is.Empty,
                $"Expected each of {string.Join(", ", fileNames)} to sit in the production project's {folder} folder and nowhere else; " +
                $"missing there: [{string.Join(", ", missing)}], found elsewhere: [{string.Join(", ", misplaced)}]. Why it matters: a " +
                "module shows its layers only when the folder matches the namespace. How to fix: move the file with git mv into " +
                $"{folder}, leaving no copy behind.");
        }
    }
}

namespace Lighthouse.Backend.Services.ForecastingFixtures
{
    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Models.ForecastingFixtures
{
    internal sealed record AKernelValue(int Days);
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Application
{
    internal sealed class AnApplicationType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Adapters
{
    internal sealed class AnAdapterType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingServices
{
    internal static class ADomainTypeReachingServices
    {
        public static int Answer(global::Lighthouse.Backend.Services.ForecastingFixtures.AServiceType service) => service.Answer;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingMicrosoftExtensions
{
    internal static class ADomainTypeReachingMicrosoftExtensions
    {
        public static bool IsQuiet(global::Microsoft.Extensions.Logging.ILogger logger) => !logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingApplication
{
    internal static class ADomainTypeReachingTheModuleApplication
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Application.AnApplicationType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingAdapters
{
    internal static class ADomainTypeReachingTheModuleAdapters
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Neighbours.Adapters.AnAdapterType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.ReachingServicesOnlyInsideAnAsyncMethod
{
    internal sealed class ADomainTypeReachingServicesOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new global::Lighthouse.Backend.Services.ForecastingFixtures.AServiceType().Answer;
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.DomainLayout.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. It also makes
    /// the compiler write its own helper types (a lambda's closure, an iterator, an array written out in code) and
    /// uses a generic parameter, so a rule that mistook those for forbidden types would report it.
    /// </summary>
    internal static class ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary
    {
        private static readonly int[] DaysShown = [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597];

        public static int LongestAbove(IEnumerable<global::Lighthouse.Backend.Models.ForecastingFixtures.AKernelValue> values, int threshold)
            => values.Where(value => value.Days > threshold).Select(value => value.Days).DefaultIfEmpty(ASiblingInTheSameDomain.Nothing).Max();

        public static T FirstOr<T>(IReadOnlyList<T> items, T fallback) => items.Count > 0 ? items[0] : fallback;

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
