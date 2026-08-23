using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// Waits written out by hand, so a fixture says which Features wait on which and nothing else has to be
    /// true for it to run. What decides them has its own tests; a fixture using this is about what the
    /// simulation does once it has been told.
    /// </summary>
    public sealed class WaitsHandedStraightToTheForecast(ForecastWaits waits) : IWhatTheForecastWaitsFor
    {
        public ForecastWaits Of(IReadOnlyCollection<Feature> featuresBeingForecast) => waits;
    }
}
