using Lighthouse.Backend.Models.Preview;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class PreviewFeatureRepositoryTest : IntegrationTestBase
    {
        public PreviewFeatureRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [TestCase(PreviewFeatureKeys.LighthouseChartKey)]
        public void RemoveOldPreviewSettingOnStartIfExisting(string key)
        {
            var subject = CreateSubject();

            var previewFeature = subject.GetByPredicate(s => s.Key == key);

            Assert.Multiple(() =>
            {
                Assert.That(previewFeature, Is.Null);
            });
        }

        private PreviewFeatureRepository CreateSubject()
        {
            return new PreviewFeatureRepository(DatabaseContext, Mock.Of<ILogger<PreviewFeatureRepository>>());
        }
    }
}
