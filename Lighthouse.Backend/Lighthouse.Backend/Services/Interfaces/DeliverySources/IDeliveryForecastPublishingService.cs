using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Publishes the forecast of every eligible source-bound Delivery back onto its remote source, and
    /// records or clears the refusal state on the Portfolio.
    ///
    /// Eligibility is deliberately narrow: bound, not archived, and not broken. An archived Delivery
    /// would push a frozen closure forecast into a live source forever, and a broken one points at a
    /// reference that no longer resolves.
    /// </summary>
    public interface IDeliveryForecastPublishingService
    {
        Task PublishForPortfolio(Portfolio portfolio, IReadOnlyList<Delivery> deliveries);
    }
}
