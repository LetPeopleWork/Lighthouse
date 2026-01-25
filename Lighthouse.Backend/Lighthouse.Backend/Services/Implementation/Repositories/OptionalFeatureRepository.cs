using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class OptionalFeatureRepository(LighthouseAppContext context, ILogger<OptionalFeatureRepository> logger)
        : RepositoryBase<OptionalFeature>(context, lighthouseAppContext => lighthouseAppContext.OptionalFeatures, logger);
}
