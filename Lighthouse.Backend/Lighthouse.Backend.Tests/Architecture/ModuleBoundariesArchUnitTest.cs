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
        internal const string WorkTrackingIntegrationPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.WorkTrackingConnectors($|\..*)|^Lighthouse\.Backend\.Factories($|\..*)";
        private const string WorkItemsSyncPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(WorkItems|TeamData|WorkItemRules)($|\..*)";
        internal const string ForecastingPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Forecast($|\..*)";
        internal const string PortfolioDeliveryPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices|Interfaces\.Update)($|\..*)";
        private const string RbacIdentityPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)($|\..*)";
        internal const string RefinementPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Refinement($|\..*)";
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

        private static readonly GivenTypesConjunctionWithDescription WorkTrackingIntegrationModule =
            Types().That().ResideInNamespaceMatching(WorkTrackingIntegrationPattern).As("WorkTracking-Integration");

        private static readonly GivenTypesConjunctionWithDescription WorkItemsSyncModule =
            Types().That().ResideInNamespaceMatching(WorkItemsSyncPattern).As("WorkItems/Sync");

        private static readonly GivenTypesConjunctionWithDescription ForecastingModule =
            Types().That().ResideInNamespaceMatching(ForecastingPattern).As("Forecasting");

        private static readonly GivenTypesConjunctionWithDescription PortfolioDeliveryModule =
            Types().That().ResideInNamespaceMatching(PortfolioDeliveryPattern).As("Portfolio/Delivery");

        private static readonly GivenTypesConjunctionWithDescription RbacIdentityModule =
            Types().That().ResideInNamespaceMatching(RbacIdentityPattern).As("RBAC/Identity");

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
        public void WorkTrackingIntegration_IsALeafInputAdapter()
        {
            Types().That().Are(WorkTrackingIntegrationModule)
                .Should().NotDependOnAny(
                    Types().That().Are(ForecastingModule)
                        .Or().Are(PortfolioDeliveryModule)
                        .Or().Are(WorkItemsSyncModule))
                .Because(
                    "WorkTracking-Integration is a leaf input adapter that only depends downward on " +
                    "Platform/Persistence and the Models shared kernel; it must not reach sideways or upward " +
                    "into Forecasting, Portfolio/Delivery, or WorkItems/Sync.")
                .Check(Architecture);
        }

        [Test]
        public void WorkTrackingConnectors_DoNotDependOnAnyRepository()
        {
            Types().That().Are(WorkTrackingIntegrationModule)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(RepositoryRecognition.StoredRecordsPattern))
                .Because(
                    "a connector only translates what the tracker sent, so it must not touch the database context " +
                    "or any repository, interface or implementation. Whatever it needs to know about stored " +
                    "records, such as which Features this instance holds, is handed to it by the caller. Other " +
                    "platform services, such as the OAuth token service, stay allowed.")
                .Check(Architecture);
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
                Assert.That(WorkTrackingIntegrationModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(WorkItemsSyncModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(ForecastingModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(PortfolioDeliveryModule.GetObjects(Architecture), Is.Not.Empty);
                Assert.That(RbacIdentityModule.GetObjects(Architecture), Is.Not.Empty);
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
