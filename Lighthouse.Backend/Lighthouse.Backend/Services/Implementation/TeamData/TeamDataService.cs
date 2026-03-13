using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.TeamData
{ 
    public class TeamDataService(
        ILogger<TeamDataService> logger,
        ITeamMetricsService teamMetricsService,
        IWorkItemService workItemService,
        IForecastUpdater forecastUpdater)
        : ITeamDataService
    {
        public async Task UpdateTeamData(Team team)
        {
            logger.LogInformation("Updating Team Data for {TeamName}", team.Name);

            await workItemService.UpdateWorkItemsForTeam(team);
            await teamMetricsService.UpdateTeamMetrics(team);

            foreach (var portfolio in team.Portfolios)
            {
                forecastUpdater.TriggerUpdate(portfolio.Id);
            }

            logger.LogInformation("Finished updating Team Data for {TeamName}", team.Name);
        }
    }
}
