using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class AppSettingRepository : RepositoryBase<AppSetting>
    {
        private static readonly object seedLock = new();
        
        private static bool hasSeeded = false;

        public AppSettingRepository(LighthouseAppContext context, ILogger<AppSettingRepository> logger) : base(context, lighthouseAppContext => lighthouseAppContext.AppSettings, logger)
        {
            SeedIfNecessary();
        }

        private void SeedIfNecessary()
        {
            lock (seedLock)
            {
                if (hasSeeded)
                {
                    return;
                }

                SeedAppSettings();
                hasSeeded = true;
            }
        }

        private void SeedAppSettings()
        {
            AddIfNotExists(new AppSetting { Id = 0, Key = AppSettingKeys.TeamDataRefreshInterval, Value = "60" });
            AddIfNotExists(new AppSetting { Id = 1, Key = AppSettingKeys.TeamDataRefreshAfter, Value = "180" });
            AddIfNotExists(new AppSetting { Id = 2, Key = AppSettingKeys.TeamDataRefreshStartDelay, Value = "10" });
            AddIfNotExists(new AppSetting { Id = 3, Key = AppSettingKeys.FeaturesRefreshInterval, Value = "60" });
            AddIfNotExists(new AppSetting { Id = 4, Key = AppSettingKeys.FeaturesRefreshAfter, Value = "180" });
            AddIfNotExists(new AppSetting { Id = 5, Key = AppSettingKeys.FeaturesRefreshStartDelay, Value = "15" });
            
            // Remove not existing App Settings (remove this later once DB is cleaned - latest with #3930)
            RemoveObsoleteAppSettings();

            SaveSync();
        }

        private void RemoveObsoleteAppSettings()
        {
            RemoveIfExists(9);
            RemoveIfExists(10);
            RemoveIfExists(11);
            RemoveIfExists(12);
            RemoveIfExists(13);
            RemoveIfExists(14);
            RemoveIfExists(15);
            RemoveIfExists(16);
            RemoveIfExists(17);
            RemoveIfExists(19);
            RemoveIfExists(20);
            RemoveIfExists(21);
            RemoveIfExists(22);
            RemoveIfExists(23);
            RemoveIfExists(24);
            RemoveIfExists(25);
            RemoveIfExists(27);
            RemoveIfExists(28);
            RemoveIfExists(29);
            RemoveIfExists(30);
            RemoveIfExists(31);
            RemoveIfExists(32);
            RemoveIfExists(33);
            RemoveIfExists(34);
            RemoveIfExists(35);
            RemoveIfExists(36);
            RemoveIfExists(38);
            RemoveIfExists(39);
            RemoveIfExists(40);
            RemoveIfExists(41);
            RemoveIfExists(42);
        }

        private void RemoveIfExists(int id)
        {
            var allElementsById = GetAllByPredicate(s => s.Id == id);

            foreach (var item in allElementsById)
            {
                Remove(item);
            }
        }

        private void AddIfNotExists(AppSetting defaultAppSetting)
        {
            var existingDefault = GetByPredicate((appSetting) => appSetting.Key == defaultAppSetting.Key);
            if (existingDefault == null)
            {
                Add(defaultAppSetting);
            }
        }
    }
}
