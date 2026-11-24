using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ModuleBoundariesArchUnitTest
    {
        internal const string TrackerIntegrationPattern = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";
        private const string WorkItemsSyncPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(WorkItems|TeamData|WorkItemRules)($|\..*)|^Lighthouse\.Backend\.Modules\.WorkItems(\.|$)";
        internal const string ForecastingPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Forecast($|\..*)|^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";
        internal const string PortfolioDeliveryPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices|Interfaces\.Update)($|\..*)"
            + @"|^Lighthouse\.Backend\.Platform\.(Encryption|UsageData|UpdateQueue)(\.|$)";
        internal const string PromisesPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DeliverySources(\.|$)|^Lighthouse\.Backend\.Modules\.Promises(\.|$)";
        internal const string RefinementPattern =
            @"^Lighthouse\.Backend\.Modules\.Refinement(\.|$)";
        internal const string ReportsPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports($|\..*)";
        private static readonly string[] FlowMetricsNamespaces =
        [
            @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Domain$",
            @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Application$",
            @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Application\.Policies$",
            @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Adapters\.Background$",
            @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Adapters\.Persistence$",
        ];

        private static readonly string[] ParentPlacementKernelTypes =
        [
            "Lighthouse.Backend.Models.ParentResolution",
            "Lighthouse.Backend.Models.ParentCandidate",
            "Lighthouse.Backend.Models.ParentPlacement",
            "Lighthouse.Backend.Models.StoredFeatures",
        ];

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly GivenTypesConjunctionWithDescription TrackerIntegrationModule =
            Types().That().ResideInNamespaceMatching(TrackerIntegrationPattern).As("Tracker integration");

        private static readonly GivenTypesConjunctionWithDescription WorkItemsSyncModule =
            Types().That().ResideInNamespaceMatching(WorkItemsSyncPattern).As("WorkItems/Sync");

        private static readonly GivenTypesConjunctionWithDescription ForecastingModule =
            Types().That().ResideInNamespaceMatching(ForecastingPattern).As("Forecasting");

        private static readonly GivenTypesConjunctionWithDescription PortfolioDeliveryModule =
            Types().That().ResideInNamespaceMatching(PortfolioDeliveryPattern).As("Portfolio/Delivery");

        private static readonly GivenTypesConjunctionWithDescription PromisesModule =
            Types().That().ResideInNamespaceMatching(PromisesPattern).As("Promises");

        private static readonly GivenTypesConjunctionWithDescription RefinementModule =
            Types().That().ResideInNamespaceMatching(RefinementPattern).As("Refinement");

        private static readonly GivenTypesConjunctionWithDescription ReportsModule =
            Types().That().ResideInNamespaceMatching(ReportsPattern).As("Reports");

        private static readonly GivenTypesConjunctionWithDescription PlatformPersistenceModule =
            Types().That().ResideInNamespaceMatching(RepositoryRecognition.PlatformPersistencePattern).As("Platform/Persistence");

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void ServiceLayer_DoesNotDependOnApiLayer()
        {
            ApplicationCoreRules.CoreDoesNotDependOnApi(ProductionModule.Value).Check();
        }

        [Test]
        public void SharedKernel_DoesNotDependUpwardOnApi()
        {
            Types().That().ResideInNamespaceMatching(KernelRules.TheModels)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ApplicationCoreRules.TheApi))
                .Because(
                    "Models is the shared kernel at the bottom of the modular monolith; it must not depend " +
                    "upward on the API driving adapter.")
                .Check(Architecture);
        }

        [Test]
        public void SharedKernel_DoesNotDependOnServices()
        {
            KernelRules.ModelsDoNotDependOnServices(ProductionModule.Value).Check();
        }

        [Test]
        public void ParentPlacementKernel_DependsOnNoServiceOrData()
        {
            ApplicationCoreRules.ParentPlacementKernelDependsOnNoCoreOrData(ProductionModule.Value, ParentPlacementKernelTypes).Check();
        }

        [Test]
        public void NamedModules_AreNonEmpty()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TrackerIntegrationModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(WorkItemsSyncModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(ForecastingModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(PortfolioDeliveryModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(PromisesModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(PlatformPersistenceModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(RefinementModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(ReportsModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(Types().That().ResideInNamespaceMatching(RepositoryRecognition.StoredRecordsPattern).GetObjects(Architecture), Is.Not.Empty);
                foreach (var namespacePattern in new[] { KernelRules.TheModels, KernelRules.TheServices, KernelRules.TheUpdateQueue }.Concat(FlowMetricsNamespaces))
                {
                    Assert.That(IlDependencies.TypesIn(ProductionModule.Value, namespacePattern), Is.Not.Empty, namespacePattern);
                }

                foreach (var kernelType in ParentPlacementKernelTypes)
                {
                    Assert.That(Types().That().HaveFullName(kernelType).GetObjects(Architecture), Is.Not.Empty, kernelType);
                }
            }
        }
    }
}
