using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.TeamData
{ 
    public class TeamDataService : ITeamDataService
    {
        private readonly ILogger<TeamDataService> logger;
        private readonly ITeamMetricsService teamMetricsService;
        private readonly IWorkItemService workItemService;
        private readonly IForecastUpdater forecastUpdater;

        public TeamDataService(
            ILogger<TeamDataService> logger, ITeamMetricsService teamMetricsService, IWorkItemService workItemService, IForecastUpdater forecastUpdater)
        {
            this.logger = logger;
            this.teamMetricsService = teamMetricsService;
            this.workItemService = workItemService;
            this.forecastUpdater = forecastUpdater;
        }

        public async Task UpdateTeamData(Team team)
        {
            logger.LogInformation("Updating Team Data for {TeamName}", team.Name);

            await workItemService.UpdateWorkItemsForTeam(team);
            await teamMetricsService.UpdateTeamMetrics(team);

            foreach (var project in team.Projects)
            {
                forecastUpdater.TriggerUpdate(project.Id);
            }

            logger.LogInformation("Finished updating Team Data for {TeamName}", team.Name);
        }
    }
}
