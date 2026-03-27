using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using System.Diagnostics;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class TeamUpdater(
        ILogger<TeamUpdater> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService)
        : UpdateServiceBase<Team>(logger, serviceScopeFactory, updateQueueService, UpdateType.Team), ITeamUpdater
    {
        protected override RefreshSettings GetRefreshSettings()
        {
            using var scope = CreateServiceScope();
            return GetServiceFromServiceScope<IAppSettingService>(scope).GetTeamDataRefreshSettings();
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();

            var licenseService = serviceProvider.GetRequiredService<ILicenseService>();
            var teamCount = teamRepository.GetAll().Count();

            if (!licenseService.CanUsePremiumFeatures() && teamCount > 3)
            {
                Logger.LogError("Skipped Refreshing team {TeamId} because the no Premium License was found and there are already {TeamCount} teams", id, teamCount);
                return;
            }

            var team = teamRepository.GetById(id);
            if (team == null)
            {
                return;
            }

            var refreshLogService = serviceProvider.GetRequiredService<IRefreshLogService>();
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            var itemCount = 0;

            try
            {
                var teamDataService = serviceProvider.GetRequiredService<ITeamDataService>();
                await teamDataService.UpdateTeamData(team);

                var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                await writeBackTriggerService.TriggerWriteBackForTeam(team);

                itemCount = team.WorkItems.Count;
                success = true;
            }
            finally
            {
                stopwatch.Stop();
                await refreshLogService.LogRefreshAsync(new RefreshLog
                {
                    Type = RefreshType.Team,
                    EntityId = team.Id,
                    EntityName = team.Name,
                    ItemCount = itemCount,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ExecutedAt = DateTime.UtcNow,
                    Success = success
                });
            }
        }

        protected override bool ShouldUpdateEntity(Team entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.UpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of team {TeamName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }
    }
}
