﻿using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.AzureDevOps;

namespace CMFTAspNet.Services.ThroughputService
{
    public class AzureDevOpsThroughputService : IThroughputService
    {
        private readonly Team team;
        private readonly IAzureDevOpsWorkItemService azureDevOpsWorkItemService;

        public AzureDevOpsThroughputService(Team team, IAzureDevOpsWorkItemService azureDevOpsWorkItemService)
        {
            this.team = team;
            this.azureDevOpsWorkItemService = azureDevOpsWorkItemService;
        }

        public async Task UpdateThroughput(int historyInDays)
        {
            var throughput = await azureDevOpsWorkItemService.GetClosedWorkItemsForTeam((AzureDevOpsTeamConfiguration)team.TeamConfiguration, historyInDays);
            team.UpdateThroughput(new Throughput(throughput));
        }
    }
}