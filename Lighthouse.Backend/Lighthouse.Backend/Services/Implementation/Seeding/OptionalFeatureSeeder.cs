using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class OptionalFeatureSeeder(LighthouseAppContext context, ILogger<OptionalFeatureSeeder> logger)
        : ISeeder
    {
        public async Task Seed()
        {
            logger.LogInformation("Seeding OptionalFeatures");

            await RemoveDeprecatedFeatures();

            var features = GetOptionalFeatures();

            await AddOrUpdateCurrentFeatures(features);

            await context.SaveChangesAsync();

            logger.LogInformation("OptionalFeatures seeded successfully");
        }

        private async Task RemoveDeprecatedFeatures()
        {
            var deprecatedKeys = new[]
            {
                OptionalFeatureKeys.LighthouseChartKey,
                OptionalFeatureKeys.CycleTimeScatterPlotKey,
                OptionalFeatureKeys.LinearIntegrationKey,
                OptionalFeatureKeys.McpServerKey,
            };

            var toRemove = await context.OptionalFeatures
                .Where(f => deprecatedKeys.Contains(f.Key))
                .ToListAsync();

            if (toRemove.Count > 0)
            {
                context.OptionalFeatures.RemoveRange(toRemove);
                logger.LogInformation("Removing {Count} deprecated OptionalFeatures", toRemove.Count);
            }
        }

        private static List<OptionalFeature> GetOptionalFeatures()
        {
            return
            [
                new OptionalFeature
                {
                    Id = 0,
                    Key = OptionalFeatureKeys.DeltaSyncKey,
                    Name = "Faster Updates",
                    Description = "Fetch only the {{workItems}} that changed since the last update instead of the whole query.",
                    Enabled = true,
                    IsPreview = false,
                },
                new OptionalFeature
                {
                    Id = 0,
                    Key = OptionalFeatureKeys.FeatureOrderingKey,
                    Name = "Let Lighthouse own the order of your {{features}}",
                    Description = "While this is on, Lighthouse forecasts your {{features}} in the order you gave them, and a refresh from your work tracking system no longer re-sequences it. Turning it off hands the order straight back to your work tracking system — the places you chose are kept, so turning it on again restores them.",
                    Enabled = false,
                    IsPreview = false,
                    IsPremium = true,
                },
            ];
        }

        private async Task AddOrUpdateCurrentFeatures(List<OptionalFeature> features)
        {
            foreach (var feature in features)
            {
                var existing = await context.OptionalFeatures
                    .FirstOrDefaultAsync(f => f.Key == feature.Key);

                if (existing == null)
                {
                    // Whether a setting is on is the operator's answer, and the branch below never
                    // overwrites it. So adding the row is the one and only moment at which a choice this
                    // instance made before the setting existed can be carried across, and getting it wrong
                    // in a shipped release cannot be put right by seeding again.
                    if (feature.Key == OptionalFeatureKeys.FeatureOrderingKey)
                    {
                        feature.Enabled = await ThisInstanceAlreadyOwnedTheFeatureOrder();
                    }

                    context.OptionalFeatures.Add(feature);
                    logger.LogDebug("Adding OptionalFeature: {Key}", feature.Key);
                }
                else
                {
                    // How a feature presents itself is ours and is refreshed on every upgrade; whether it
                    // is on is the operator's and is never overwritten.
                    existing.Name = feature.Name;
                    existing.Description = feature.Description;
                    existing.IsPreview = feature.IsPreview;
                    existing.IsPremium = feature.IsPremium;
                    logger.LogDebug("Updating OptionalFeature: {Key}", feature.Key);
                }
            }
        }

        /// <summary>
        /// What the instance had chosen while the order lived in its own app setting. Anything other than
        /// the exact stored word for this instance owning the order - the tracker's, an absent row, or a
        /// value nothing recognises - means it did not, which is what an absent row has always meant.
        /// </summary>
        private async Task<bool> ThisInstanceAlreadyOwnedTheFeatureOrder()
        {
            var storedPolicy = await context.AppSettings
                .Where(setting => setting.Key == AppSettingKeys.FeatureOrderingPolicy)
                .Select(setting => setting.Value)
                .FirstOrDefaultAsync();

            return string.Equals(storedPolicy, nameof(FeatureOrderingPolicy.ManualOrder), StringComparison.Ordinal);
        }
    }
}