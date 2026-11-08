using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.Clean;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingAdapters;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingApplication;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingData;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingEntityFrameworkCore;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingMicrosoftExtensions;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingServices;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingServicesOnlyInsideAnAsyncMethod;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Flow metrics context lives in one module folder with its layers visible. The types are named as strings
    /// and read from the compiled production assembly, so this class compiles before and after each move, and a
    /// type that has not moved yet is a failure that names it. The Domain rule runs on the real module and on
    /// made-up Domain types: each one that reaches a forbidden layer must be reported, and a compliant twin next
    /// to it must not be, so a rule that flags everything fails as surely as one that flags nothing.
    /// </summary>
    [TestFixture]
    public class FlowMetricsModuleLayoutArchUnitTest
    {
        private const string FixtureDomains = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.Domain\.";

        private const string ADomainThatHoldsNothing = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.Domain\.NothingLivesHere(\.|$)";

        private const string ModuleDomainFolder = "Modules/FlowMetrics/Domain";

        private const string ModuleApplicationNamespace = "Lighthouse.Backend.Modules.FlowMetrics.Application";

        private const string ModuleApplicationFolder = "Modules/FlowMetrics/Application";

        private const string ModulePoliciesNamespace = "Lighthouse.Backend.Modules.FlowMetrics.Application.Policies";

        private const string ModulePoliciesFolder = "Modules/FlowMetrics/Application/Policies";

        private static readonly string[] PureCalculations =
        [
            "PercentileCalculator",
            "XmRCalculator",
            "XmRResult",
            "SleRiskCalculator",
            "BaselineValidationService",
            "BaselineValidationResult",
        ];

        private static readonly string[] PureCalculationFiles =
        [
            "PercentileCalculator.cs",
            "XmRCalculator.cs",
            "SleRiskCalculator.cs",
            "BaselineValidationService.cs",
        ];

        // Every type the contract files declare, the records and companion interfaces that travel with them included.
        private static readonly string[] ContractTypes =
        [
            "ITeamMetricsService",
            "ForecastThroughputStatus",
            "CycleTimeWorkItem",
            "IPortfolioMetricsService",
            "CycleTimeFeature",
            "IPercentileSnapshotWriter",
            "PercentileFamilyReader",
            "IProcessBehaviorSnapshotWriter",
            "ProcessBehaviorFamilyReader",
            "IPercentilesOverTimeSeriesQuery",
            "IProcessBehaviorSeriesQuery",
            "IOverTimeGapReconciler",
            "IOverTimeHistoryFiller",
            "IOverTimeHistoryFillActivity",
            "OverTimeFillRequest",
            "IBlockedCountSnapshotRepository",
            "IPercentilesOverTimeSnapshotRepository",
            "IProcessBehaviorSnapshotRepository",
        ];

        private static readonly string[] ContractFiles =
        [
            "ITeamMetricsService.cs",
            "IPortfolioMetricsService.cs",
            "IPercentileSnapshotWriter.cs",
            "IProcessBehaviorSnapshotWriter.cs",
            "IPercentilesOverTimeSeriesQuery.cs",
            "IProcessBehaviorSeriesQuery.cs",
            "IOverTimeGapReconciler.cs",
            "IOverTimeHistoryFiller.cs",
            "IBlockedCountSnapshotRepository.cs",
            "IPercentilesOverTimeSnapshotRepository.cs",
            "IProcessBehaviorSnapshotRepository.cs",
        ];

        // Each implementation file declares exactly one type, named like the file.
        private static readonly string[] Implementations =
        [
            "BaseMetricsService",
            "TeamMetricsService",
            "PortfolioMetricsService",
            "MetricsCacheKey",
            "PercentileFamilies",
            "BlockedCountSeriesBuilder",
            "PercentileSnapshotWriter",
            "ProcessBehaviorSnapshotWriter",
            "PercentilesOverTimeSeriesQuery",
            "GapAskingPercentilesOverTimeSeriesQuery",
            "ProcessBehaviorSeriesQuery",
            "GapAskingProcessBehaviorSeriesQuery",
            "OverTimeGapReconciler",
            "ReconstructionMemo",
            "RetentionEdge",
            "OverTimeFillTarget",
        ];

        // The module's reactions to domain events. Each file declares exactly one handler, named like the file.
        private static readonly string[] Policies =
        [
            "BlockedCountSnapshotRecordingHandler",
            "PercentilesOverTimeRecordingHandler",
            "ProcessBehaviorRecordingHandler",
            "PortfolioFeaturesRefreshedMetricsInvalidationHandler",
            "BlackoutConfigurationChangedMetricsInvalidationHandler",
            "DemoBlockedHistoryBackfillHandler",
            "DemoPercentilesBackfillHandler",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsModuleLayoutArchUnitTest).Assembly));

        public enum ForbiddenReach
        {
            Services,
            DataLayer,
            EntityFrameworkCore,
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
        public void ThePureFlowMetricsCalculations_LiveInTheModuleDomain_AndNoneIsLeftUnderServices()
        {
            FlowMetricsModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, FlowMetricsModuleRules.FlowMetricsDomainNamespace, PureCalculations).Check();
        }

        [Test]
        public void ThePureFlowMetricsCalculationFiles_SitInTheModuleDomainFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ModuleDomainFolder, PureCalculationFiles);
        }

        [Test]
        public void TheFlowMetricsContract_LivesInTheModuleApplication_AndNoneOfItIsLeftUnderServices()
        {
            FlowMetricsModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ModuleApplicationNamespace, ContractTypes).Check();
        }

        [Test]
        public void TheFlowMetricsContractFiles_SitInTheModuleApplicationFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ModuleApplicationFolder, ContractFiles);
        }

        [Test]
        public void TheFlowMetricsImplementations_LiveInTheModuleApplication_AndNoneIsLeftUnderServices()
        {
            FlowMetricsModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ModuleApplicationNamespace, Implementations).Check();
        }

        [Test]
        public void TheFlowMetricsImplementationFiles_SitInTheModuleApplicationFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ModuleApplicationFolder, [.. Implementations.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheFlowMetricsEventHandlers_LiveInTheModulePolicies_AndNoneIsLeftUnderServices()
        {
            FlowMetricsModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ModulePoliciesNamespace, Policies).Check();
        }

        [Test]
        public void TheFlowMetricsEventHandlerFiles_SitInTheModulePoliciesFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ModulePoliciesFolder, [.. Policies.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheModuleDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            FlowMetricsModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value).Check();
        }

        [TestCase(ForbiddenReach.Services)]
        [TestCase(ForbiddenReach.DataLayer)]
        [TestCase(ForbiddenReach.EntityFrameworkCore)]
        [TestCase(ForbiddenReach.MicrosoftExtensions)]
        [TestCase(ForbiddenReach.ModuleApplication)]
        [TestCase(ForbiddenReach.ModuleAdapters)]
        [TestCase(ForbiddenReach.ServicesOnlyInsideAnAsyncMethod)]
        public void ADomainTypeThatReachesAForbiddenLayer_IsReportedAndItsCompliantTwinIsNot(ForbiddenReach reach)
        {
            var (domain, offender, forbiddenTarget) = MadeUpDomainReaching(reach);

            var verdict = FlowMetricsModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, domain);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, forbiddenTarget, reach);
        }

        [Test]
        public void ADomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var verdict = FlowMetricsModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, ADomainThatHoldsNothing);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A Domain rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing " +
                "stays green forever and guards nothing. How to fix: the rule must fail when the Domain it was given holds no type.");

            Assert.That(failure!.Message, Does.Contain(ADomainThatHoldsNothing), "The failure must name the Domain that held nothing, so the reader knows which pattern to correct.");
        }

        private static (string Domain, string Offender, string ForbiddenTarget) MadeUpDomainReaching(ForbiddenReach reach) => reach switch
        {
            ForbiddenReach.Services => (FixtureDomain("ReachingServices"), typeof(ADomainTypeReachingServices).FullName!, typeof(global::Lighthouse.Backend.Services.FlowMetricsFixtures.AServiceType).FullName!),
            ForbiddenReach.DataLayer => (FixtureDomain("ReachingData"), typeof(ADomainTypeReachingTheDataLayer).FullName!, typeof(global::Lighthouse.Backend.Data.FlowMetricsFixtures.ADataLayerType).FullName!),
            ForbiddenReach.EntityFrameworkCore => (FixtureDomain("ReachingEntityFrameworkCore"), typeof(ADomainTypeReachingEntityFrameworkCore).FullName!, typeof(global::Microsoft.EntityFrameworkCore.DbContext).FullName!),
            ForbiddenReach.MicrosoftExtensions => (FixtureDomain("ReachingMicrosoftExtensions"), typeof(ADomainTypeReachingMicrosoftExtensions).FullName!, typeof(global::Microsoft.Extensions.Logging.ILogger).FullName!),
            ForbiddenReach.ModuleApplication => (FixtureDomain("ReachingApplication"), typeof(ADomainTypeReachingTheModuleApplication).FullName!, typeof(global::Lighthouse.Backend.Modules.FlowMetricsFixtures.Application.AnApplicationType).FullName!),
            ForbiddenReach.ModuleAdapters => (FixtureDomain("ReachingAdapters"), typeof(ADomainTypeReachingTheModuleAdapters).FullName!, typeof(global::Lighthouse.Backend.Modules.FlowMetricsFixtures.Adapters.AnAdapterType).FullName!),
            ForbiddenReach.ServicesOnlyInsideAnAsyncMethod => (FixtureDomain("ReachingServicesOnlyInsideAnAsyncMethod"), typeof(ADomainTypeReachingServicesOnlyInsideAnAsyncMethod).FullName!, typeof(global::Lighthouse.Backend.Services.FlowMetricsFixtures.AServiceType).FullName!),
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

namespace Lighthouse.Backend.Services.FlowMetricsFixtures
{
    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Data.FlowMetricsFixtures
{
    internal sealed class ADataLayerType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Application
{
    internal sealed class AnApplicationType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Adapters
{
    internal sealed class AnAdapterType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Models.FlowMetricsFixtures
{
    internal sealed record AKernelValue(int Days);
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingServices
{
    internal static class ADomainTypeReachingServices
    {
        public static int Answer(global::Lighthouse.Backend.Services.FlowMetricsFixtures.AServiceType service) => service.Answer;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingData
{
    internal static class ADomainTypeReachingTheDataLayer
    {
        public static int Answer(global::Lighthouse.Backend.Data.FlowMetricsFixtures.ADataLayerType data) => data.Answer;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingEntityFrameworkCore
{
    internal static class ADomainTypeReachingEntityFrameworkCore
    {
        public static bool HasChanges(global::Microsoft.EntityFrameworkCore.DbContext context) => context.ChangeTracker.HasChanges();
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingMicrosoftExtensions
{
    internal static class ADomainTypeReachingMicrosoftExtensions
    {
        public static bool IsQuiet(global::Microsoft.Extensions.Logging.ILogger logger) => !logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information);
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingApplication
{
    internal static class ADomainTypeReachingTheModuleApplication
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.Application.AnApplicationType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingAdapters
{
    internal static class ADomainTypeReachingTheModuleAdapters
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.Adapters.AnAdapterType().Answer;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.ReachingServicesOnlyInsideAnAsyncMethod
{
    internal sealed class ADomainTypeReachingServicesOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new global::Lighthouse.Backend.Services.FlowMetricsFixtures.AServiceType().Answer;
        }
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.Domain.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. It also makes
    /// the compiler write its own helper types (a lambda's closure, an iterator, an array written out in code) and
    /// uses a generic parameter, so a rule that mistook those for forbidden types would report it.
    /// </summary>
    internal static class ADomainTypeReachingOnlyTheKernelAndTheBaseLibrary
    {
        private static readonly int[] PercentilesShown = [50, 70, 85, 95, 98, 99, 100, 12, 13, 14, 15, 16, 17, 18, 19, 20];

        public static int LongestAbove(IEnumerable<global::Lighthouse.Backend.Models.FlowMetricsFixtures.AKernelValue> values, int threshold)
            => values.Where(value => value.Days > threshold).Select(value => value.Days).DefaultIfEmpty(ASiblingInTheSameDomain.Nothing).Max();

        public static T FirstOr<T>(IReadOnlyList<T> items, T fallback) => items.Count > 0 ? items[0] : fallback;

        public static IEnumerable<int> Shown()
        {
            foreach (var percentile in PercentilesShown)
            {
                yield return Math.Min(percentile, 100);
            }
        }
    }

    internal static class ASiblingInTheSameDomain
    {
        public const int Nothing = 0;
    }
}
