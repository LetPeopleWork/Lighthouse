using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ArchivedDeliveryReadPathArchUnitTest
    {
        private const string ArchivedProjection = "Lighthouse.Backend.API.DTO.Archived.ArchivedDeliveryProjection";
        private const string ArchivedIdentity = "Lighthouse.Backend.API.DTO.Archived.ArchivedDeliveryIdentity";

        private const string FeatureEntity = "Lighthouse.Backend.Models.Feature";
        private const string DeliveryEntity = "Lighthouse.Backend.Models.Delivery";
        private const string BlackoutPeriodEntity = "Lighthouse.Backend.Models.BlackoutPeriod";
        private const string ForecastNamespacePattern = @"^Lighthouse\.Backend\.Models\.Forecast($|\..*)";
        private const string ServiceNamespacePattern = @"^Lighthouse\.Backend\.Services($|\..*)";
        private const string DataNamespacePattern = @"^Lighthouse\.Backend\.Data($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string Reason =
            "A retired Delivery's numbers were written down once and must read the same every time. The only way to be " +
            "sure of that is for the code that builds the view to have nothing it could recalculate from: no Feature, " +
            "no Delivery, no calendar of non-working days, no forecast and no service. Handing it any of those makes " +
            "the guarantee a matter of nobody choosing to use them.";

        [Test]
        public void ArchivedReadPath_CannotReachAnythingItCouldRecalculateFrom()
        {
            Classes().That().HaveFullName(ArchivedProjection).Or().HaveFullName(ArchivedIdentity)
                .Should().NotDependOnAny(Types().That()
                    .HaveFullName(FeatureEntity)
                    .Or().HaveFullName(DeliveryEntity)
                    .Or().HaveFullName(BlackoutPeriodEntity)
                    .Or().ResideInNamespaceMatching(ForecastNamespacePattern)
                    .Or().ResideInNamespaceMatching(ServiceNamespacePattern)
                    .Or().ResideInNamespaceMatching(DataNamespacePattern))
                .Because(Reason)
                .Check(Architecture);
        }
    }
}
