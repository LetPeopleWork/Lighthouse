using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The simulation draws each day's throughput from the first few Features in order, so changing who
    /// owns that order changes every date. A person just made that change and is watching for the dates
    /// to move, which is why each Portfolio forecasts immediately instead of taking the ordinary route
    /// that waits for a refresh they never asked for.
    /// <para>
    /// Skipping the fan-out when the order is handed over looks safe, because seeding the ranks from the
    /// sequence already on screen cannot move anybody. That only holds the first time - taking the order
    /// over again, after the work tracking system has re-ranked everything underneath, moves plenty. So
    /// this triggers always: one run per Portfolio on a rare administrative action, where the
    /// alternative is silently stale dates.
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
                forecastUpdater.TriggerImmediateUpdate(portfolio.Id);
            }

            return Task.CompletedTask;
        }
    }
}
