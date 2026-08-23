using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Services.Interfaces.Dependencies
{
    /// <summary>
    /// The one thing a forecast asks about dependencies. It exists so the simulation can be handed a plain
    /// list of what has to finish first without ever meeting the decision that produced it: a loop with a
    /// licence check or a circle walk inside it is a loop that decides, and then a warning a reader sees and
    /// a date they are given come from two different answers to the same question.
    /// </summary>
    public interface IWhatTheForecastWaitsFor
    {
        ForecastWaits Of(IReadOnlyCollection<Feature> featuresBeingForecast);
    }
}
