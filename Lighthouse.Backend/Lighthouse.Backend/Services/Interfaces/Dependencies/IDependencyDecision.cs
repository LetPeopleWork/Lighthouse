using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Services.Interfaces.Dependencies
{
    /// <summary>
    /// The one place that asks. It turns Features a caller already holds into the facts the decision reads,
    /// hands over the instance's licence among them, and gives back what was decided. It exists so that the
    /// licence is read once in the product rather than at every screen and every run that wants an answer:
    /// two readings are two chances to disagree, and the disagreement would show up as a warning promising
    /// a date that a purchase would not actually move.
    /// </summary>
    public interface IDependencyDecision
    {
        /// <param name="placeOfEachFeature">
        /// Where each Feature sits, for callers that number them. A caller that does not leaves this out and
        /// nothing is claimed about the order.
        /// </param>
        HonouredDependencies About(
            IReadOnlyCollection<Feature> features,
            IReadOnlyDictionary<int, int>? placeOfEachFeature = null);
    }
}
