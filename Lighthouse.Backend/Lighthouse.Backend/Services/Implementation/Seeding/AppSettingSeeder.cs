using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class AppSettingSeeder(LighthouseAppContext context, ILogger<AppSettingSeeder> logger)
        : ISeeder
    {
        public async Task Seed()
        {
            logger.LogInformation("Seeding AppSettings");

            await AddDefaultSettings();
            await RemoveObsoleteSettings();
            
            await context.SaveChangesAsync();
            
            logger.LogInformation("AppSettings seeded successfully");
        }

        private async Task AddDefaultSettings()
        {
            var defaultSettings = new[]
            {
                new AppSetting { Id = 0, Key = AppSettingKeys.TeamDataRefreshInterval, Value = "60" },
                new AppSetting { Id = 1, Key = AppSettingKeys.TeamDataRefreshAfter, Value = "180" },
                new AppSetting { Id = 2, Key = AppSettingKeys.TeamDataRefreshStartDelay, Value = "10" },
                new AppSetting { Id = 3, Key = AppSettingKeys.FeaturesRefreshInterval, Value = "60" },
                new AppSetting { Id = 4, Key = AppSettingKeys.FeaturesRefreshAfter, Value = "180" },
                new AppSetting { Id = 5, Key = AppSettingKeys.FeaturesRefreshStartDelay, Value = "15" }
            };

            foreach (var setting in defaultSettings)
            {
                var exists = await context.AppSettings
                    .AnyAsync(s => s.Key == setting.Key);

                if (!exists)
                {
                    context.AppSettings.Add(setting);
                    logger.LogDebug("Adding AppSetting: {Key} = {Value}", setting.Key, setting.Value);
                }
            }
        }

        private async Task RemoveObsoleteSettings()
        {
            var obsoleteIds = new[] 
            { 
                9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20,
                21, 22, 23, 24, 25, 27, 28, 29, 30, 31, 32,
                33, 34, 35, 36, 38, 39, 40, 41, 42
            };

            var toRemove = await context.AppSettings
                .Where(s => obsoleteIds.Contains(s.Id))
                .ToListAsync();

            if (toRemove.Count > 0)
            {
                context.AppSettings.RemoveRange(toRemove);
                logger.LogInformation("Removing {Count} obsolete AppSettings", toRemove.Count);
            }
        }
    }
}