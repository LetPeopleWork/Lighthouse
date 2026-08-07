using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureOrderingPolicyProvider(IRepository<AppSetting> repository) : IFeatureOrderingPolicyProvider
    {
        public FeatureOrderingPolicy GetPolicy()
        {
            var setting = repository.GetByPredicate(s => s.Key == AppSettingKeys.FeatureOrderingPolicy);

            // An absent row - a fresh install, or an instance downgraded from a build that had one - reads
            // as the tracker owning the order rather than throwing.
            if (setting == null || !Enum.TryParse<FeatureOrderingPolicy>(setting.Value, out var policy))
            {
                return FeatureOrderingPolicy.SourceOrder;
            }

            return policy;
        }

        public async Task SetPolicy(FeatureOrderingPolicy policy)
        {
            var existing = repository.GetByPredicate(s => s.Key == AppSettingKeys.FeatureOrderingPolicy);

            if (existing == null)
            {
                repository.Add(new AppSetting { Key = AppSettingKeys.FeatureOrderingPolicy, Value = policy.ToString() });
            }
            else
            {
                existing.Value = policy.ToString();
                repository.Update(existing);
            }

            await repository.Save();
        }
    }
}
