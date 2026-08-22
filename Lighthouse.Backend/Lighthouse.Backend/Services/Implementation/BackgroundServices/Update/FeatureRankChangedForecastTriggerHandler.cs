using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The simulation draws each day's throughput from the first few Features in order, so a Feature that
    /// changed places changed every date its Portfolios show. The only way a Feature changes places is a
    /// person dragging it, and that person is watching for the dates to move, which is why each Portfolio
    /// forecasts immediately instead of taking the ordinary route that waits for a refresh they never
    /// asked for.
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
                forecastUpdater.TriggerImmediateUpdate(portfolio.Id);
            }

            return Task.CompletedTask;
        }
    }
}
