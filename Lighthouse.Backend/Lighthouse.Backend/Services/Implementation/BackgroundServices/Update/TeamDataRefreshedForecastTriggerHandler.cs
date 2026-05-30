using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class TeamDataRefreshedForecastTriggerHandler(
        IRepository<Team> teamRepository,
        IForecastUpdater forecastUpdater) : IDomainEventHandler<TeamDataRefreshed>
    {
        public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null)
            {
                return Task.CompletedTask;
            }

            foreach (var portfolio in team.Portfolios)
            {
                forecastUpdater.TriggerUpdate(portfolio.Id);
            }

            return Task.CompletedTask;
        }
    }
}
