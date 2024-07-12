using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class ThroughputUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<ThroughputUpdateService> logger;

        public ThroughputUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ThroughputUpdateService> logger) : base(configuration, "Throughput", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting Update of Throughput for all Teams");

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

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

            logger.LogInformation("Last Refresh of Throughput for team {TeamName} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", team.Name, minutesSinceLastUpdate, RefreshAfter);

            if (minutesSinceLastUpdate >= RefreshAfter)
            {
                var throughputService = scope.ServiceProvider.GetRequiredService<IThroughputService>();
                await throughputService.UpdateThroughputForTeam(team).ConfigureAwait(true);
                await teamRepository.Save();
            }
        }
    }
}
