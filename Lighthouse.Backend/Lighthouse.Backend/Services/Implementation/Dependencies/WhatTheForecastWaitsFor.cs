using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    public class WhatTheForecastWaitsFor(IDependencyDecision dependencyDecision) : IWhatTheForecastWaitsFor
    {
        public ForecastWaits Of(IReadOnlyCollection<Feature> featuresBeingForecast)
        {
            // Nothing here is a judgement about a dependency - there are none to judge. Asking anyway would
            // put a licence read on every forecast of every instance that has never linked two Features.
            if (!featuresBeingForecast.Any(feature => feature.DependsOnReferences.Count > 0))
            {
                return ForecastWaits.Nothing;
            }

            return ForecastWaits.From(dependencyDecision.About(featuresBeingForecast));
        }
    }
}
