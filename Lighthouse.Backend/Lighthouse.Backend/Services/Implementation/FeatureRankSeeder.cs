using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureRankSeeder(IRepository<Feature> featureRepository) : IFeatureRankSeeder
    {
        public async Task SeedMissingRanks()
        {
            // Read while the tracker still owns the order, so the sequence this reads is the one the user
            // is looking at - which is the whole of D6's "nothing moves". The caller writes the policy
            // afterwards, never before.
            var features = featureRepository.GetAll().ToList();

            var lastPlace = features.Max(feature => feature.ManualRank) ?? 0;

            foreach (var feature in features.Where(feature => feature.ManualRank is null))
            {
                feature.ManualRank = ++lastPlace;
                featureRepository.Update(feature);
            }

            await featureRepository.Save();
        }
    }
}
