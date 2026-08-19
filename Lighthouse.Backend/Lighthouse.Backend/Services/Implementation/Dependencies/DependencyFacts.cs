using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Turns Features a caller already loaded into the plain facts the decision reads. It exists so the
    /// screen and the refresh describe a Feature the same way: two projections would be two chances to
    /// leave something out, and the decision would answer them differently through no fault of its own.
    /// </summary>
    public static class DependencyFacts
    {
        /// <param name="placeOfEachFeature">
        /// Where each Feature sits, for callers that number them. A caller that does not leaves this out
        /// and nothing is claimed about the order - which is not the same as claiming things are in it.
        /// </param>
        /// <remarks>
        /// The licence answer is left false because nothing may read it yet: no dependency changes a
        /// forecast until that behaviour ships, and until it does an instance's licence has no bearing on
        /// anything decided here. Whoever turns it on has to hand the real answer in from here.
        /// </remarks>
        public static DependencyHonourInput About(
            IReadOnlyCollection<Feature> features,
            IReadOnlyDictionary<int, int>? placeOfEachFeature = null)
        {
            var facts = features
                .Select(feature => new FeatureDependencyFacts(
                    feature.ReferenceId,
                    feature.Portfolios.Select(portfolio => portfolio.Id).ToList(),
                    PlaceOf(feature, placeOfEachFeature),
                    feature.CanBeForecast,
                    feature.DependsOnReferences.Select(reference => reference.ReferenceId).ToList()))
                .ToList();

            return new DependencyHonourInput(facts, HasPremiumLicence: false);
        }

        private static int? PlaceOf(Feature feature, IReadOnlyDictionary<int, int>? placeOfEachFeature)
        {
            if (placeOfEachFeature is not null && placeOfEachFeature.TryGetValue(feature.Id, out var place))
            {
                return place;
            }

            return null;
        }
    }
}
