using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation.BackgroundServices
{
    public class ThroughputUpdateService : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<ThroughputUpdateService> logger;

        public ThroughputUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ThroughputUpdateService> logger)
        {
            this.configuration = configuration;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdatingThroughput(stoppingToken);
            }
        }

        private async Task TryUpdatingThroughput(CancellationToken stoppingToken)
        {
            try
            {
                await UpdateThroughputForAllTeams(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private async Task UpdateThroughputForAllTeams(CancellationToken stoppingToken)
        {
            var throughputInterval = configuration.GetValue<int>("PeriodicRefresh:Throughput:Interval");

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

                foreach (var team in teamRepository.GetAll().ToList())
                {
                    await UpdateThroughputForTeam(scope, teamRepository, team);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(throughputInterval), stoppingToken);
        }

        private async Task UpdateThroughputForTeam(IServiceScope scope, IRepository<Team> teamRepository, Team team)
        {
            var throughputRefreshAfter = configuration.GetValue<int>("PeriodicRefresh:Throughput:RefreshAfter");

            var minutesSinceLastUpdate = (DateTime.Now - team.ThroughputUpdateTime).TotalMinutes;

            if (minutesSinceLastUpdate >= throughputRefreshAfter)
            {
                var throughputService = scope.ServiceProvider.GetRequiredService<IThroughputService>();
                await throughputService.UpdateThroughput(team).ConfigureAwait(true);
                await teamRepository.Save();
            }
        }
    }
}
