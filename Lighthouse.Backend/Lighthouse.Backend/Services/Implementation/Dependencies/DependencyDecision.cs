using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Reads the instance's licence and asks the one decision. Nothing else in this product reads the
    /// licence to work out what a dependency does: a screen that read it for itself and a forecast that read
    /// it for itself would be two answers to one question, and the day they differ a reader is told a
    /// purchase would move a date that would not move.
    /// </summary>
    public class DependencyDecision(
        IDependencyHonourPolicy dependencyHonourPolicy,
        ILicenseService licenseService) : IDependencyDecision
    {
        public HonouredDependencies About(
            IReadOnlyCollection<Feature> features,
            IReadOnlyDictionary<int, int>? placeOfEachFeature = null)
        {
            var facts = DependencyFacts.About(features, licenseService.CanUsePremiumFeatures(), placeOfEachFeature);

            return dependencyHonourPolicy.Evaluate(facts);
        }
    }
}
