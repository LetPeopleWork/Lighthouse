using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Preview;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class PreviewFeatureRepository : RepositoryBase<PreviewFeature>
    {
        public PreviewFeatureRepository(LighthouseAppContext context, ILogger<RepositoryBase<PreviewFeature>> logger) : base(context, (LighthouseAppContext context) => context.PreviewFeatures, logger)
        {
            SeedAppSettings();
        }

        private void SeedAppSettings()
        {
            AddIfNotExists(new PreviewFeature { Key = PreviewFeatureKeys.LighthouseChartKey, Name = "Lighthouse Chart", Description = "Shows Burndown Chart with Forecasts for each Feature in a Project", Enabled = false });

            SaveSync();
        }

        private void AddIfNotExists(PreviewFeature previewFeature)
        {
            var existingDefault = GetByPredicate((feature) => feature.Name == previewFeature.Name);
            if (existingDefault == null)
            {
                Add(previewFeature);
            }
        }
    }
}
