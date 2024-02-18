using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class FeatureRepository
    {
        private readonly List<Feature> features = new List<Feature>();

        public void AddFeature(Feature feature)
        {
            features.Add(feature);
        }

        public IEnumerable<Feature> GetFeatures()
        {
            return features;
        }

        public void RemoveFeature(Feature feature)
        {
            var featureToRemove = features.SingleOrDefault(t => t.Id == feature.Id);
            features.Remove(featureToRemove);
        }

        public void UpdateFeature(Feature feature)
        {
            RemoveFeature(feature);
            AddFeature(feature);
        }
    }
}
