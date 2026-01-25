using Lighthouse.Backend.Data;
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
            await AddOrUpdateCurrentFeatures();

            await context.SaveChangesAsync();

            logger.LogInformation("OptionalFeatures seeded successfully");
        }

        private async Task RemoveDeprecatedFeatures()
        {
            var deprecatedKeys = new[]
            {
                OptionalFeatureKeys.LighthouseChartKey,
                OptionalFeatureKeys.CycleTimeScatterPlotKey
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

        private async Task AddOrUpdateCurrentFeatures()
        {
            var features = new[]
            {
                new OptionalFeature
                {
                    Id = 2,
                    Key = OptionalFeatureKeys.McpServerKey,
                    Name = "MCP Server",
                    Description = "Enables MCP Server to integrate with AI Agents (requires restart). [Premium Only]",
                    Enabled = false,
                    IsPreview = false
                },
                new OptionalFeature
                {
                    Id = 3,
                    Key = OptionalFeatureKeys.LinearIntegrationKey,
                    Name = "Linear Integration",
                    Description = "Enables Experimental Support for Linear.app",
                    Enabled = false,
                    IsPreview = true
                }
            };

            foreach (var feature in features)
            {
                var existing = await context.OptionalFeatures
                    .FirstOrDefaultAsync(f => f.Key == feature.Key);

                if (existing == null)
                {
                    context.OptionalFeatures.Add(feature);
                    logger.LogDebug("Adding OptionalFeature: {Key}", feature.Key);
                }
                else
                {
                    // Update IsPreview flag if it changed
                    existing.IsPreview = feature.IsPreview;
                    logger.LogDebug("Updating OptionalFeature: {Key}", feature.Key);
                }
            }
        }
    }
}