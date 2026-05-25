using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureStateTransitionRepository(LighthouseAppContext context, ILogger<FeatureStateTransitionRepository> logger)
        : RepositoryBase<FeatureStateTransition>(context, (lighthouseAppContext) => lighthouseAppContext.FeatureStateTransitions, logger), IFeatureStateTransitionRepository
    {
    }
}
