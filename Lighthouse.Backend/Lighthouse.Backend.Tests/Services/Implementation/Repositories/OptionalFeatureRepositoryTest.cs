using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class OptionalFeatureRepositoryTest : IntegrationTestBase
    {
        public OptionalFeatureRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [TestCase(OptionalFeatureKeys.LighthouseChartKey)]
        public void RemoveOldOptionalFeatureOnStartIfExisting(string key)
        {
            var subject = CreateSubject();

            var optionalFeature = subject.GetByPredicate(s => s.Key == key);

            Assert.Multiple(() =>
            {
                Assert.That(optionalFeature, Is.Null);
            });
        }

        [TestCase(OptionalFeatureKeys.McpServerKey)]
        [TestCase(OptionalFeatureKeys.LinearIntegrationKey)]
        public void AddOptionalFeatureOnStartIfNotExisting(string key)
        {
            var subject = CreateSubject();

            var optionalFeature = subject.GetByPredicate(s => s.Key == key);

            Assert.Multiple(() =>
            {
                Assert.That(optionalFeature, Is.Not.Null);
            });
        }

        private OptionalFeatureRepository CreateSubject()
        {
            return new OptionalFeatureRepository(DatabaseContext, Mock.Of<ILogger<OptionalFeatureRepository>>());
        }
    }
}
