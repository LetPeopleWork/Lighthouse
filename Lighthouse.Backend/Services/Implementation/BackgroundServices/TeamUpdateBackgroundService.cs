using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class TeamUpdateBackgroundService : UpdateBackgroundServiceBase
    {
        private readonly ILogger<TeamUpdateBackgroundService> logger;

        public TeamUpdateBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<TeamUpdateBackgroundService> logger) : base(serviceScopeFactory, logger)
        {
            this.logger = logger;
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetThroughputRefreshSettings();
            }
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting Update for all Teams");

            using (var scope = CreateServiceScope())
            {
                var teamRepository = GetServiceFromServiceScope<IRepository<Team>>(scope);

                foreach (var team in teamRepository.GetAll().ToList())
                {
                    logger.LogInformation("Checking last update for team {TeamName}", team.Name);
                    await UpdateTeam(scope, teamRepository, team);
                }
            }

            logger.LogInformation("Done Updating all Teams");
        }

        private async Task UpdateTeam(IServiceScope scope, IRepository<Team> teamRepository, Team team)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - team.TeamUpdateTime).TotalMinutes;

            var refreshSettings = GetRefreshSettings();

            logger.LogInformation("Last Refresh of team {TeamName} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", team.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            if (minutesSinceLastUpdate >= refreshSettings.RefreshAfter)
            {
                var teamUpdateService = GetServiceFromServiceScope<ITeamUpdateService>(scope);
                await teamUpdateService.UpdateTeam(team).ConfigureAwait(true);
                await teamRepository.Save();
            }
        }
    }
}
