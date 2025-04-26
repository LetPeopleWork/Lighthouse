using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class OptionalFeatureRepository : RepositoryBase<OptionalFeature>
    {
        public OptionalFeatureRepository(LighthouseAppContext context, ILogger<OptionalFeatureRepository> logger) : base(context, (LighthouseAppContext context) => context.OptionalFeatures, logger)
        {
            SeedAppSettings();
        }

        private void SeedAppSettings()
        {
            RemoveIfExists(new OptionalFeature { Id = 0, Key = OptionalFeatureKeys.LighthouseChartKey, Name = "Lighthouse Chart", Description = "Shows Burndown Chart with Forecasts for each Feature in a Project", Enabled = false });
            RemoveIfExists(new OptionalFeature { Id = 1, Key = OptionalFeatureKeys.CycleTimeScatterPlotKey, Name = "Cycle Time Scatter Plot", Description = "Shows Cycle Time Scatterplot for a team", Enabled = false });

            AddIfNotExists(new OptionalFeature { Id = 2, Key = OptionalFeatureKeys.McpServerKey, Name = "MCP Server", Description = "Enables MCP Server to integrate with AI Agents (requires restart)", Enabled = false, IsPreview = true });

            AddIfNotExists(new OptionalFeature { Id = 3, Key = OptionalFeatureKeys.LinearIntegrationKey, Name = "Linear Integration", Description = "Enables Experimental Support for Linear.app", Enabled = false, IsPreview = true });

            SaveSync();
        }

        private void AddIfNotExists(OptionalFeature optionalFeature)
        {
            OptionalFeature? existingDefault = GetFeatureByName(optionalFeature);
            if (existingDefault == null)
            {
                Add(optionalFeature);
            }
            else
            {
                existingDefault.IsPreview = optionalFeature.IsPreview;
                Update(existingDefault);
            }
        }

        private void RemoveIfExists(OptionalFeature optionalFeature)
        {
            OptionalFeature? existingDefault = GetFeatureByName(optionalFeature);
            if (existingDefault != null)
            {
                Remove(existingDefault);
            }
        }

        private OptionalFeature? GetFeatureByName(OptionalFeature optionalFeature)
        {
            return GetByPredicate((feature) => feature.Key == optionalFeature.Key);
        }
    }
}
