using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Publishes the forecast of every eligible source-bound Delivery back onto its remote source.
    ///
    /// Eligibility is deliberately narrow: switched on, bound, not retired, and heard from. A retired
    /// Delivery would push a frozen closure forecast into a live source forever - which is why the
    /// Deliveries arrive as the set a background pass may write to rather than as a plain list - and a
    /// Delivery whose source is finished, or which nothing has ever resolved, points at a reference that
    /// may not be there.
    /// </summary>
    public interface IDeliveryForecastPublishingService
    {
        Task PublishForPortfolio(Portfolio portfolio, RecordableDeliveries deliveries);
    }
}
