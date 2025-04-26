using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Preview;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class PreviewFeatureRepository : RepositoryBase<PreviewFeature>
    {
        public PreviewFeatureRepository(LighthouseAppContext context, ILogger<PreviewFeatureRepository> logger) : base(context, (LighthouseAppContext context) => context.PreviewFeatures, logger)
        {
            SeedAppSettings();
        }

        private void SeedAppSettings()
        {
            RemoveIfExists(new PreviewFeature { Id = 0, Key = PreviewFeatureKeys.LighthouseChartKey, Name = "Lighthouse Chart", Description = "Shows Burndown Chart with Forecasts for each Feature in a Project", Enabled = false });
            RemoveIfExists(new PreviewFeature { Id = 1, Key = PreviewFeatureKeys.CycleTimeScatterPlotKey, Name = "Cycle Time Scatter Plot", Description = "Shows Cycle Time Scatterplot for a team", Enabled = false });

            AddIfNotExists(new PreviewFeature { Id = 2, Key = PreviewFeatureKeys.McpServerKey, Name = "MCP Server", Description = "Enables MCP Server to integrate with AI Agents (requires restart)", Enabled = false });

            AddIfNotExists(new PreviewFeature { Id = 3, Key = PreviewFeatureKeys.LinearIntegrationKey, Name = "Linear Integration", Description = "Enables Experimental Support for Linear.app", Enabled = false });

            SaveSync();
        }

        private void AddIfNotExists(PreviewFeature previewFeature)
        {
            PreviewFeature? existingDefault = GetFeatureByName(previewFeature);
            if (existingDefault == null)
            {
                Add(previewFeature);
            }
        }

        private void RemoveIfExists(PreviewFeature previewFeature)
        {
            PreviewFeature? existingDefault = GetFeatureByName(previewFeature);
            if (existingDefault != null)
            {
                Remove(existingDefault);
            }
        }

        private PreviewFeature? GetFeatureByName(PreviewFeature previewFeature)
        {
            return GetByPredicate((feature) => feature.Key == previewFeature.Key);
        }
    }
}
