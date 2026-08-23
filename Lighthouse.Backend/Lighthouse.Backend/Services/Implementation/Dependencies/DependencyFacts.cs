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
        /// <param name="hasPremiumLicence">
        /// Whether this instance is licensed for the paid behaviour a dependency has. It is an argument
        /// rather than something read here so that exactly one component in the product reads the licence
        /// and everything else is told the same answer.
        /// </param>
        public static DependencyHonourInput About(
            IReadOnlyCollection<Feature> features,
            bool hasPremiumLicence,
            IReadOnlyDictionary<int, int>? placeOfEachFeature = null)
        {
            var facts = features
                .Select(feature => new FeatureDependencyFacts(
                    feature.ReferenceId,
                    feature.Portfolios.Select(portfolio => portfolio.Id).ToList(),
                    feature.FeatureWork.Select(work => work.TeamId).Distinct().ToList(),
                    PlaceOf(feature, placeOfEachFeature),
                    feature.CanBeForecast,
                    feature.DependsOnReferences.Select(reference => reference.ReferenceId).ToList()))
                .ToList();

            return new DependencyHonourInput(facts, hasPremiumLicence, PortfoliosSettingTheirDependenciesAside(features));
        }

        /// <summary>
        /// Read off the Portfolios the caller already loaded rather than fetched, so this stays a projection
        /// of what is in hand and the decision keeps costing no queries.
        /// </summary>
        private static List<int> PortfoliosSettingTheirDependenciesAside(IReadOnlyCollection<Feature> features)
            => features
                .SelectMany(feature => feature.Portfolios)
                .Where(portfolio => portfolio.IgnoreDependencies)
                .Select(portfolio => portfolio.Id)
                .Distinct()
                .ToList();

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
