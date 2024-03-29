﻿using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Services.Implementation.BackgroundServices
{
    public class ThroughputUpdateService : UpdateBackgroundServiceBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public ThroughputUpdateService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<ThroughputUpdateService> logger) : base(configuration, "Throughput", logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

                foreach (var team in teamRepository.GetAll().ToList())
                {
                    await UpdateThroughputForTeam(scope, teamRepository, team);
                }
            }
        }

        private async Task UpdateThroughputForTeam(IServiceScope scope, IRepository<Team> teamRepository, Team team)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - team.ThroughputUpdateTime).TotalMinutes;

            if (minutesSinceLastUpdate >= RefreshAfter)
            {
                var throughputService = scope.ServiceProvider.GetRequiredService<IThroughputService>();
                await throughputService.UpdateThroughput(team).ConfigureAwait(true);
                await teamRepository.Save();
            }
        }
    }
}
