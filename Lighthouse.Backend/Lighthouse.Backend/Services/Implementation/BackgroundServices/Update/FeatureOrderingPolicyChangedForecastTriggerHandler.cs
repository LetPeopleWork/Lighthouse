using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The forecast draws from the order, so changing who owns it changes every date (S5).
    /// <para>
    /// ADR-134 SA-16 offered to skip the fan-out when the order is handed over, on the grounds that
    /// INV-A3 seeds from the sequence already on screen and so nothing moved. That optimisation is
    /// declined: it only holds the *first* time, and taking the order over again after the tracker has
    /// re-ranked (AC-5.3) moves plenty. Triggering always is one coalesced run per Portfolio on a rare
    /// administrative action, where the alternative is silently stale dates.
    /// </para>
    /// </summary>
    public class FeatureOrderingPolicyChangedForecastTriggerHandler(
        IRepository<Portfolio> portfolioRepository,
        IForecastUpdater forecastUpdater) : IDomainEventHandler<FeatureOrderingPolicyChanged>
    {
        public Task HandleAsync(FeatureOrderingPolicyChanged domainEvent, CancellationToken cancellationToken)
        {
            foreach (var portfolio in portfolioRepository.GetAll())
            {
                forecastUpdater.TriggerUpdate(portfolio.Id);
            }

            return Task.CompletedTask;
        }
    }
}
