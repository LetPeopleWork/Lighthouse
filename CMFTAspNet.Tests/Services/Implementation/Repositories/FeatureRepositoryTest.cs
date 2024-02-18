using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
{
    public class FeatureRepositoryTest
    {
        [Test]
        public void GetFeatures_NoFeature_ReturnsEmptyList()
        {
            var subject = new FeatureRepository();

            var features = subject.GetFeatures();

            Assert.That(features, Is.Empty);
        }

        [Test]
        public void AddFeature_StoresFeature()
        {
            var subject = new FeatureRepository();
            var feature = new Feature();

            subject.AddFeature(feature);

            var features = subject.GetFeatures();

            Assert.That(features, Contains.Item(feature));
        }

        [Test]
        public void GivenExistingFeature_RemoveFeature_RemovesFromList()
        {
            var subject = new FeatureRepository();
            var feature = new Feature();

            subject.AddFeature(feature);

            // Act
            subject.RemoveFeature(feature);

            var features = subject.GetFeatures();
            CollectionAssert.DoesNotContain(features, feature);
        }

        [Test]
        public void UpdateFeature_GivenExistingFeature_PersistsChange()
        {
            var subject = new FeatureRepository();
            var feature = new Feature();

            subject.AddFeature(feature);

            // Act
            feature.Name = "New Feature";
            subject.UpdateFeature(feature);

            // Assert
            var features = subject.GetFeatures();
            Assert.That(features.Single().Name, Is.EqualTo("New Feature"));
        }
    }
}
