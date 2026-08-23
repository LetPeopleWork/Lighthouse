using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// A forecast run in which no Feature waits on another, which is what every forecast looked like before
    /// dependencies could change a date. Fixtures that say nothing about waiting use this so they keep
    /// asserting what they were written to assert.
    /// </summary>
    public sealed class NothingWaitsForAnything : IWhatTheForecastWaitsFor
    {
        public ForecastWaits Of(IReadOnlyCollection<Feature> featuresBeingForecast) => ForecastWaits.Nothing;
    }
}
