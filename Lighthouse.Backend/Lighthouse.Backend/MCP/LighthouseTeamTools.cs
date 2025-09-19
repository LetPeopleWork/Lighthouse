using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using ModelContextProtocol.Server;
using NuGet.Protocol;
using System.ComponentModel;

namespace Lighthouse.Backend.MCP
{
    [McpServerToolType]
    public sealed class LighthouseTeamTools : LighthouseToolsBase
    {
        public LighthouseTeamTools(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
        }

        [McpServerTool, Description("Get a list of all teams configured in Lighthouse")]
        public string GetAllTeams()
        {
            using (var scope = CreateServiceScope())
            {
                var teamRepo = GetServiceFromServiceScope<IRepository<Team>>(scope);

                return teamRepo.GetAll()
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                    })
                    .ToJson();
            }
        }

        [McpServerTool, Description("Get information for a specific team.")]
        public string GetTeamByName(string name)
        {
            using (var scope = CreateServiceScope())
            {
                var teamRepo = GetServiceFromServiceScope<IRepository<Team>>(scope);

                var team = GetTeamByName(name, teamRepo);

                if (team == null)
                {
                    return $"No team found with name {name}";
                }

                return ToJson(team);
            }
        }

        [McpServerTool, Description("Run a How Many Forecast for a Team")]
        public string RunHowManyForecast(string teamName, DateTime untilWhen)
        {
            using (var scope = CreateServiceScope())
            {
                var teamRepo = GetServiceFromServiceScope<IRepository<Team>>(scope);
                var forecastService = GetServiceFromServiceScope<IForecastService>(scope);
                var teamMetricsService = GetServiceFromServiceScope<ITeamMetricsService>(scope);

                var team = GetTeamByName(teamName, teamRepo);
                if (team == null)
                {
                    return $"No team found with name {teamName}";
                }

                var throughput = teamMetricsService.GetCurrentThroughputForTeam(team);
                var timeToTargetDate = (untilWhen - DateTime.Today).Days;
                var howManyForecast = forecastService.HowMany(throughput, timeToTargetDate);

                return ToJson(howManyForecast);
            }
        }

        [McpServerTool, Description("Run a When Forecast for a Team")]
        public string RunWhenForecast(string teamName, int remainingItems)
        {
            using (var scope = CreateServiceScope())
            {
                var teamRepo = GetServiceFromServiceScope<IRepository<Team>>(scope);
                var forecastService = GetServiceFromServiceScope<IForecastService>(scope);

                var team = GetTeamByName(teamName, teamRepo);
                if (team == null)
                {
                    return $"No team found with name {teamName}";
                }

                var whenForecast = forecastService.When(team, remainingItems);

                return ToJson(whenForecast);
            }
        }

        [McpServerTool, Description("Get Flow Metrics of the specified Team in a given time range")]
        public string GetFlowMetricsForTeam(string teamName, DateTime? startDate, DateTime? endDate)
        {
            var rangeStart = startDate ?? DateTime.Now.AddDays(-30);
            var rangeEnd = endDate ?? DateTime.Now;

            using (var scope = CreateServiceScope())
            {
                var teamRepo = GetServiceFromServiceScope<IRepository<Team>>(scope);
                var teamMetricsService = GetServiceFromServiceScope<ITeamMetricsService>(scope);

                var team = GetTeamByName(teamName, teamRepo);
                if (team == null)
                {
                    return $"No team found with name {teamName}";
                }

                var cycleTimePercentiles = teamMetricsService.GetCycleTimePercentilesForTeam(team, rangeStart, rangeEnd);
                var cycleTimes = teamMetricsService.GetClosedItemsForTeam(team, rangeStart, rangeEnd).Select(wi => wi.CycleTime);
                var wip = teamMetricsService.GetWorkInProgressOverTimeForTeam(team, rangeStart, rangeEnd);
                var throughput = teamMetricsService.GetThroughputForTeam(team, rangeStart, rangeEnd);

                return ToJson(new
                {
                    teamName,
                    cycleTimePercentiles,
                    cycleTimes,
                    wip,
                    throughput,
                });
            }
        }

        private static Team? GetTeamByName(string name, IRepository<Team> teamRepo)
        {
            return teamRepo.GetByPredicate(t => t.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
