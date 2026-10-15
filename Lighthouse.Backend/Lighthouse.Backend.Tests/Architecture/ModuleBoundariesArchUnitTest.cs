using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ModuleBoundariesArchUnitTest
    {
        private const string ApiLayerPattern = @"^Lighthouse\.Backend\.API($|\..*)";
        private const string ServiceLayerPattern = @"^Lighthouse\.Backend\.Services($|\..*)";
        private const string SharedKernelPattern = @"^Lighthouse\.Backend\.Models($|\..*)";

        internal const string WorkTrackingIntegrationPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.WorkTrackingConnectors($|\..*)|^Lighthouse\.Backend\.Factories($|\..*)";
        private const string WorkItemsSyncPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(WorkItems|TeamData|WorkItemRules)($|\..*)";
        private const string ForecastingPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Forecast($|\..*)";
        internal const string PortfolioDeliveryPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices|Interfaces\.Update)($|\..*)";
        private const string RbacIdentityPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)($|\..*)";
        internal const string RefinementPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Refinement($|\..*)";
        internal const string ReportsPattern =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports($|\..*)";
        private const string PlatformPersistencePattern =
            @"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.(Repositories|DatabaseManagement|DomainEvents|Seeding|OAuth))($|\..*)";

        private const string StoredRecordsPattern =
            @"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.Repositories)($|\..*)";

        private const string DataLayerPattern = @"^Lighthouse\.Backend\.Data($|\..*)";

        private static readonly string[] ParentPlacementKernelTypes =
        [
            "Lighthouse.Backend.Models.ParentResolution",
            "Lighthouse.Backend.Models.ParentCandidate",
            "Lighthouse.Backend.Models.ParentPlacement",
            "Lighthouse.Backend.Models.StoredFeatures",
        ];

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

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
            Types().That().ResideInNamespaceMatching(PlatformPersistencePattern).As("Platform/Persistence");

        [Test]
        public void ServiceLayer_DoesNotDependOnApiLayer()
        {
            Types().That().ResideInNamespaceMatching(ServiceLayerPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ApiLayerPattern))
                .Because(
                    "ADR-027 D3/D5 hexagonal seam: the application core (Services.Implementation + Interfaces) " +
                    "must not depend on the API driving adapter; DTOs the core returns live in Models.* not API.DTO.")
                .Check(Architecture);
        }

        [Test]
        public void SharedKernel_DoesNotDependUpwardOnApi()
        {
            Types().That().ResideInNamespaceMatching(SharedKernelPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ApiLayerPattern))
                .Because(
                    "Models is the shared kernel at the bottom of the modular monolith; it must not depend " +
                    "upward on the API driving adapter. The stronger Models-must-not-depend-on-Services half is " +
                    "NOT yet enforceable: every entity implements Services.Interfaces.IEntity and " +
                    "Models.WorkTrackingSystemConnection references the Services.Implementation.WorkTrackingConnectors." +
                    "WorkTrackingSystems enum, so those contracts would need to move into Models first.")
                .Check(Architecture);
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
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(StoredRecordsPattern))
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
            Types().That().HaveFullName(ParentPlacementKernelTypes[0])
                .Or().HaveFullName(ParentPlacementKernelTypes[1])
                .Or().HaveFullName(ParentPlacementKernelTypes[2])
                .Or().HaveFullName(ParentPlacementKernelTypes[3])
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(ServiceLayerPattern)
                        .Or().ResideInNamespaceMatching(DataLayerPattern))
                .Because(
                    "deciding under which issue a record hangs must be checkable from what it is handed alone. " +
                    "If the pick could reach a repository, the database context, a connector or a logging service, " +
                    "its answer could change with what it happened to read, and a wrongly placed parent looks " +
                    "exactly like correct data.")
                .Check(Architecture);
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
                Assert.That(Types().That().ResideInNamespaceMatching(StoredRecordsPattern).GetObjects(Architecture), Is.Not.Empty);
                foreach (var kernelType in ParentPlacementKernelTypes)
                {
                    Assert.That(Types().That().HaveFullName(kernelType).GetObjects(Architecture), Is.Not.Empty, kernelType);
                }
            }
        }
    }
}
