using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class ThroughputUpdateService : UpdateBackgroundServiceBase
    {
        private readonly ILogger<ThroughputUpdateService> logger;

        public ThroughputUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<ThroughputUpdateService> logger) : base(serviceScopeFactory, logger)
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
            logger.LogInformation($"Starting Update of Throughput for all Teams");

            using (var scope = CreateServiceScope())
            {
                var teamRepository = GetServiceFromServiceScope<IRepository<Team>>(scope);

                foreach (var team in teamRepository.GetAll().ToList())
                {
                    logger.LogInformation("Checking Throughput for team {TeamName}", team.Name);
                    await UpdateThroughputForTeam(scope, teamRepository, team);
                }
            }

            logger.LogInformation("Done Updating of Throughput for all Teams");
        }

        private async Task UpdateThroughputForTeam(IServiceScope scope, IRepository<Team> teamRepository, Team team)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - team.ThroughputUpdateTime).TotalMinutes;

            var refreshSettings = GetRefreshSettings();

            logger.LogInformation("Last Refresh of Throughput for team {TeamName} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", team.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            if (minutesSinceLastUpdate >= refreshSettings.RefreshAfter)
            {
                var throughputService = GetServiceFromServiceScope<IThroughputService>(scope);
                await throughputService.UpdateThroughputForTeam(team).ConfigureAwait(true);
                await teamRepository.Save();
            }
        }
    }
}
