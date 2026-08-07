using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The forecast draws from the order, so a Feature that changed places changed every date its
    /// Portfolios show (ADR-133). No debounce is added: the update queue already parks a single coalesced
    /// follow-up, so a run of Move Ups collapses to at most two runs per Portfolio.
    /// </summary>
    public class FeatureRankChangedForecastTriggerHandler(
        IRepository<Feature> featureRepository,
        IForecastUpdater forecastUpdater) : IDomainEventHandler<FeatureRankChanged>
    {
        public Task HandleAsync(FeatureRankChanged domainEvent, CancellationToken cancellationToken)
        {
            var feature = featureRepository.GetById(domainEvent.FeatureId);

            foreach (var portfolio in feature?.Portfolios ?? [])
            {
                forecastUpdater.TriggerUpdate(portfolio.Id);
            }

            return Task.CompletedTask;
        }
    }
}
